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
- neither                       the part may blend with its own bone, that bone's
                                parent and its children (never Root, unless the part
                                is on Root). Good for single-bone parts.

A vertex left with no weight after filtering falls back to rigid on its part's bone
(EnemyForge's fallback, applied a second time), so every vertex is always weighted.
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
        top = sorted(weights.items(), key=lambda kv: -kv[1])[:MAX_INFLUENCES]
        total = sum(w for _n, w in top)
        if total < 1e-4:
            new_weights.append({bone: 1.0})
            fallback += 1
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
