"""Skinning rules and test poses for the rigged (enemy) path.

EnemyForge's `apply_smooth_weights` does the heavy lifting: Blender's heat weighting
over the whole mesh, a rigid fallback for every vertex it misses, one smoothing
pass, a 4-influence limit and normalisation. Heat weighting sees geometry, not
intent, so on a mesh of disjoint islands it hands out weights nobody asked for: a
glaive haft beside the leg picks up UpperLeg, a hat brim picks up Shoulder, a skirt
picks up Root. `apply_bind_rules` runs after it and enforces what each Part says:

- extras["rigid"] = True       every vertex of the part is bound 1.0 to `part.bone`
                                (props, hats, buckles, lanterns: things that do not
                                bend). extras["prop"] = True implies rigid.
- extras["bones"] = [names]     the only bones the part may be weighted to. Heat
                                weights on any other bone are dropped and the rest
                                renormalised. `.L` names swap to `.R` on the
                                mirrored copy, exactly as `part.bone` does.
- extras["skirt"] = {...}      the part below a height is handed to the thighs
                                (a gambeson or surcoat skirt): see _skirt_weights.
- neither                       the part may blend with its own bone, that bone's
                                parent and its children (never Root, unless the part
                                is on Root). Good for single-bone parts.

A vertex left with no allowed weight after filtering is blended between the two
nearest allowed bones by distance (reported as fallback_vertices); only if that
somehow yields nothing does it go rigid on its part's bone (EnemyForge's fallback).
Every vertex is always weighted.
The part of each vertex is read from kit.PART_LAYER, an integer face layer written
by build_bmesh that survives the bevel and unwrap.
"""

from __future__ import annotations

import json
import math

import bpy
from mathutils import Matrix, Vector

from enemy_forge.assemble import _expand_bones

from . import kit
from .blueprint import Blueprint

MAX_INFLUENCES = 4
MIN_WEIGHT = 0.02   # influences below this are dropped before renormalising


def _mirror_name(name: str, mirrored: bool) -> str:
    if mirrored and name.endswith(".L"):
        return name[:-2] + ".R"
    return name


def vertex_parts(obj: bpy.types.Object) -> list[int]:
    """Part id (part_number * 2 + mirrored) for every vertex, from its faces."""
    mesh = obj.data
    layer = mesh.attributes.get(kit.PART_LAYER)
    if layer is None:
        raise RuntimeError(f"{obj.name}: the {kit.PART_LAYER} face layer is missing; "
                           f"the rigged path cannot tell which part a vertex came from")
    ids = [-1] * len(mesh.vertices)
    values = [a.value for a in layer.data]
    for poly in mesh.polygons:
        pid = values[poly.index]
        for v in poly.vertices:
            ids[v] = pid
    missing = sum(1 for i in ids if i < 0)
    if missing:
        raise RuntimeError(f"{obj.name}: {missing} vertices belong to no face")
    return ids


def _hierarchy(bp: Blueprint) -> tuple[dict[str, str | None], dict[str, list[str]]]:
    parent, children = {}, {}
    for spec in _expand_bones(bp.bones):
        parent[spec["name"]] = spec.get("parent")
        children.setdefault(spec["name"], [])
    for name, par in parent.items():
        if par:
            children.setdefault(par, []).append(name)
    return parent, children


def allowed_bones(part: kit.Part, mirrored: bool, parent: dict, children: dict) -> set[str]:
    bone = _mirror_name(part.bone, mirrored)
    if part.extras.get("rigid") or part.extras.get("prop"):
        return {bone}
    if "bones" in part.extras:
        names = {_mirror_name(n, mirrored) for n in part.extras["bones"]}
        names.add(bone)
    else:
        names = {bone, *(children.get(bone, []))}
        if parent.get(bone):
            names.add(parent[bone])
        if bone != "Root":
            names.discard("Root")
    unknown = names - set(parent)
    if unknown:
        raise ValueError(f"part on {bone!r} allows bones the rig does not have: "
                         f"{sorted(unknown)}")
    return names


def apply_bind_rules(obj: bpy.types.Object, bp: Blueprint) -> dict:
    """Filter the heat weights per part (see module docstring). Returns stats."""
    parent, children = _hierarchy(bp)
    part_ids = vertex_parts(obj)
    rules: dict[int, tuple[str, set[str], bool]] = {}
    for pid in set(part_ids):
        part = bp.parts[pid // 2]
        mirrored = bool(pid % 2)
        rigid = bool(part.extras.get("rigid") or part.extras.get("prop"))
        rules[pid] = (_mirror_name(part.bone, mirrored),
                      allowed_bones(part, mirrored, parent, children), rigid)

    groups = {g.index: g.name for g in obj.vertex_groups}
    by_name = {g.name: g for g in obj.vertex_groups}
    for bone, _allowed, _rigid in rules.values():
        if bone not in by_name:
            by_name[bone] = obj.vertex_groups.new(name=bone)

    segments = {spec["name"]: (Vector(spec["head"]), Vector(spec["tail"]))
                for spec in _expand_bones(bp.bones)}
    skirts = {}
    for pid in set(part_ids):
        part = bp.parts[pid // 2]
        if "skirt" in part.extras:
            skirts[pid] = part.extras["skirt"]

    rigid_verts = fallback = trimmed = 0
    new_weights: list[dict[str, float]] = []
    for vert in obj.data.vertices:
        bone, allowed, rigid = rules[part_ids[vert.index]]
        if rigid:
            new_weights.append({bone: 1.0})
            rigid_verts += 1
            continue
        weights = {groups[g.group]: g.weight for g in vert.groups
                   if g.weight > MIN_WEIGHT and groups[g.group] in allowed}
        if len(weights) < len([g for g in vert.groups if g.weight > MIN_WEIGHT]):
            trimmed += 1
        skirt = skirts.get(part_ids[vert.index])
        if skirt:
            weights = _skirt_weights(weights, vert.co, skirt)
        if not weights:
            # Heat weighting missed this vertex (typically a layer hidden inside
            # another island). Blend the nearest allowed bones by distance rather
            # than going rigid, so a coat still bends with the spine under it.
            weights = _nearest_weights(vert.co, allowed, segments)
            fallback += 1
        top = sorted(weights.items(), key=lambda kv: -kv[1])[:MAX_INFLUENCES]
        total = sum(w for _n, w in top)
        if total < 1e-4:
            new_weights.append({bone: 1.0})
            continue
        new_weights.append({n: w / total for n, w in top})

    everything = list(range(len(obj.data.vertices)))
    for group in obj.vertex_groups:
        group.remove(everything)
    for index, weights in enumerate(new_weights):
        for name, weight in weights.items():
            by_name[name].add([index], weight, "REPLACE")

    # Drop groups that ended up empty: the bone stays in the rig, it just skins nothing.
    used = {n for w in new_weights for n in w}
    for group in list(obj.vertex_groups):
        if group.name not in used:
            obj.vertex_groups.remove(group)

    influences = [len(w) for w in new_weights]
    return {"rigid_vertices": rigid_verts, "fallback_vertices": fallback,
            "filtered_vertices": trimmed, "max_influences": max(influences),
            "mean_influences": round(sum(influences) / len(influences), 2)}


def _nearest_weights(co, allowed: set[str], segments: dict) -> dict[str, float]:
    """Inverse-distance (power 4) weights to the two nearest allowed bone segments."""
    scored = []
    for name in allowed:
        head, tail = segments[name]
        axis = tail - head
        t = max(0.0, min(1.0, (co - head).dot(axis) / max(axis.length_squared, 1e-12)))
        scored.append(((co - (head + axis * t)).length, name))
    scored.sort()
    near = scored[:2]
    raw = {name: 1.0 / max(d, 1e-3) ** 4 for d, name in near}
    total = sum(raw.values())
    return {name: w / total for name, w in raw.items()}


def _smoothstep(a: float, b: float, x: float) -> float:
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)


def _skirt_weights(weights: dict[str, float], co, skirt: dict) -> dict[str, float]:
    """Hand the lower part of a coat/skirt to the thighs so it follows a stride.

    extras["skirt"] = {"top": z, "bottom": z, "left": "UpperLeg.L",
    "right": "UpperLeg.R", "strength": 0.75, "split": 0.05}. Below `top` the thigh
    share grows to `strength` at `bottom`; across the centre line it is split
    between the legs over +/- `split` metres of X. Heat weighting alone leaves a
    skirt on the Hips, and a lifted knee then goes straight through it.
    """
    top, bottom = skirt["top"], skirt["bottom"]
    if co.z >= top:
        return weights
    left, right = skirt.get("left", "UpperLeg.L"), skirt.get("right", "UpperLeg.R")
    share = skirt.get("strength", 0.75) * _smoothstep(top, bottom, co.z)
    split = skirt.get("split", 0.05)
    to_left = _smoothstep(-split, split, co.x)
    rest = {n: w for n, w in weights.items() if n not in (left, right)}
    total = sum(rest.values())
    out = {n: w / total * (1.0 - share) for n, w in rest.items()} if total > 1e-6 else {}
    if not out:
        share = 1.0
    out[left] = out.get(left, 0.0) + share * to_left
    out[right] = out.get(right, 0.0) + share * (1.0 - to_left)
    return {n: w for n, w in out.items() if w > MIN_WEIGHT}


def dedupe_armature_modifiers(obj: bpy.types.Object, rig: bpy.types.Object) -> None:
    """parent_set(ARMATURE_AUTO) adds its own Armature modifier next to the one
    build_armature made; two modifiers would deform every vertex twice."""
    mods = [m for m in obj.modifiers if m.type == "ARMATURE"]
    for extra in mods[1:]:
        obj.modifiers.remove(extra)
    if mods:
        mods[0].object = rig


# --------------------------------------------------------------------------------
# Rig facts for validation
# --------------------------------------------------------------------------------

def facing_problems(bp: Blueprint) -> list[str]:
    """Each forward bone must point toward -Y (the model's front)."""
    problems = []
    specs = {s["name"]: s for s in _expand_bones(bp.bones)}
    for name in bp.forward_bones:
        if name not in specs:
            problems.append(f"forward bone {name!r} is not in the rig")
            continue
        head, tail = Vector(specs[name]["head"]), Vector(specs[name]["tail"])
        d = tail - head
        if not (d.y < -1e-4 and abs(d.y) >= abs(d.x)):
            problems.append(f"{name} points {tuple(round(c, 3) for c in d)}, not toward "
                            f"-Y: the model does not face -Y")
    if not bp.forward_bones:
        problems.append("blueprint names no forward_bones, so facing cannot be checked")
    return problems


def prop_part_ids(bp: Blueprint) -> set[int]:
    out = set()
    for number, part in enumerate(bp.parts):
        if part.extras.get("prop"):
            out.update({number * 2, number * 2 + 1})
    return out


# --------------------------------------------------------------------------------
# Test pose (render.py's POSED view)
# --------------------------------------------------------------------------------

def store_pose(rig: bpy.types.Object, pose: dict) -> None:
    rig["artforge_pose"] = json.dumps(pose)


def apply_pose(rig: bpy.types.Object, pose: dict | None = None) -> list[str]:
    """Rotate bones about their own (posed) heads by WORLD-space XYZ degrees.

    `pose` = {bone: (rx, ry, rz)}; applied parents-first so a turned chest carries
    the arm with it. World axes are easier to author than bone-local ones: X tips a
    limb forward/back (negative X swings a hanging arm toward -Y, the front), Y
    rolls it sideways, Z turns it about the vertical. Returns the bones posed.
    """
    if pose is None:
        pose = json.loads(rig.get("artforge_pose", "{}"))
    bones = rig.pose.bones
    unknown = set(pose) - {b.name for b in bones}
    if unknown:
        raise KeyError(f"pose names bones the rig lacks: {sorted(unknown)}")

    def depth(name):
        d, b = 0, bones[name]
        while b.parent:
            d, b = d + 1, b.parent
        return d

    for name in sorted(pose, key=depth):
        bpy.context.view_layer.update()
        pb = bones[name]
        rx, ry, rz = (math.radians(a) for a in pose[name])
        rot = (Matrix.Rotation(rz, 4, "Z") @ Matrix.Rotation(ry, 4, "Y")
               @ Matrix.Rotation(rx, 4, "X"))
        head = pb.head.copy()   # armature space, posed
        pb.matrix = Matrix.Translation(head) @ rot @ Matrix.Translation(-head) @ pb.matrix
    bpy.context.view_layer.update()
    return sorted(pose, key=depth)


HEAT_TRIES = 8


def smooth_weights_with_retry(apply_smooth_weights, mesh_obj, rig, tries: int = HEAT_TRIES, **kwargs) -> dict:
    """Run EnemyForge's heat weighting, retrying when it silently collapses to one bone.

    Blender's bone heat weighting ("failed to find solution for one or more bones")
    fails at random on the same mesh, and on failure EnemyForge keeps the rigid
    per-part weights. Retrying unchanged input fails the same way, so each retry
    first restores the rigid weights, nudges mesh and rig by under a millimetre
    (different floats for the solver), then puts both back exactly. Adopted from
    the Age of Powder enemy worker, which measured failures on about half of
    Petardier builds, all recovered on the second attempt. A mesh that still fails
    after `tries` attempts is left rigid, and validate.py fails it loudly.
    """
    names = [g.name for g in mesh_obj.vertex_groups]
    rigid = [[(names[g.group], g.weight) for g in v.groups] for v in mesh_obj.data.vertices]
    stats: dict = {}
    for attempt in range(1, tries + 1):
        if attempt > 1:
            mesh_obj.vertex_groups.clear()
            groups = {n: mesh_obj.vertex_groups.new(name=n) for n in names}
            for index, weights in enumerate(rigid):
                for name, weight in weights:
                    groups[name].add([index], weight, "REPLACE")
            offset = (0.00037 * attempt, -0.00021 * attempt, 0.00013 * attempt)
            mesh_obj.location = offset
            rig.location = offset
            bpy.context.view_layer.update()
        stats = apply_smooth_weights(mesh_obj, rig, **kwargs)
        if attempt > 1:
            mesh_obj.location = (0.0, 0.0, 0.0)
            rig.location = (0.0, 0.0, 0.0)
            mesh_obj.matrix_parent_inverse.identity()
            bpy.context.view_layer.update()
        if not stats.get("auto_weights") or stats.get("max_influences", 0) > 1:
            break
        print(f"  {mesh_obj.name}: heat weighting collapsed to one bone "
              f"(attempt {attempt}/{tries}), retrying")
    stats["heat_attempts"] = attempt
    return stats
