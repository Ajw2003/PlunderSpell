"""Gate every model on EnemyForge's geometry checks plus the art bible's sizes.

EnemyForge's `validate()` is run unchanged through a small adapter (it reads name,
tri_budget, height and grounded). It was written for rigged enemies, so on the
static path its four rig checks are expected to fire and are removed by exact
message; nothing else it reports is filtered. Then, per kind:

- items: bounding box within ±10 % of the JSON `dimensions` on each axis.
  W is X, D is Y, H is Z; the model stands on z = 0 and faces -Y.
- structures: footprint inside the 12 × 12 m cell (|x|, |y| ≤ 6 m).
- enemies: body height (parts marked extras["prop"] excluded, so a glaive does not
  count) within ±5 % of the JSON height_m; centred; facing -Y (Blueprint.
  forward_bones point -Y); one Armature modifier; <= 4 influences per vertex;
  rig and skinned bone counts reported. EnemyForge's own checks cover every
  vertex weighted and normalised, grounded, budget and the rest.
"""

from __future__ import annotations

from dataclasses import dataclass

import bpy

from enemy_forge.validate import Report, format_report, validate as ef_validate  # noqa: F401

from .blueprint import Blueprint

DIM_TOLERANCE = 0.10
ENEMY_HEIGHT_TOLERANCE = 0.05
CELL_HALF = 6.0
AXES = ("X", "Y", "Z")
AXIS_LABEL = {"X": "W", "Y": "D", "Z": "H"}

# EnemyForge's rig checks. On a static mesh these are expected, not failures.
_RIG_FAILURE_PREFIXES_STATIC = (
    "vertices are not assigned to any bone",
    "mesh has no bound armature modifier",
)


@dataclass
class _Adapter:
    """What enemy_forge.validate.validate reads from an Archetype."""

    name: str
    tri_budget: int
    height: float
    grounded: bool


def bbox(obj: bpy.types.Object) -> tuple[tuple[float, ...], tuple[float, ...]]:
    xs, ys, zs = [], [], []
    for v in obj.data.vertices:
        co = obj.matrix_world @ v.co
        xs.append(co.x), ys.append(co.y), zs.append(co.z)
    return (min(xs), min(ys), min(zs)), (max(xs), max(ys), max(zs))


def expected_height(bp: Blueprint) -> float:
    if bp.kind == "items":
        return bp.entry.dims[2]
    return float(bp.entry.height_m or 0.0)


def validate(obj: bpy.types.Object, bp: Blueprint) -> Report:
    adapter = _Adapter(bp.name, bp.budget, expected_height(bp), bp.grounded)
    report = ef_validate(obj, adapter)
    report.stats["budget"] = bp.budget

    if not bp.rigged:
        kept = []
        for failure in report.failures:
            if any(failure.endswith(p) for p in _RIG_FAILURE_PREFIXES_STATIC):
                continue
            kept.append(failure)
        report.failures = kept
        # The static path must really be static, since the filter above trusts that.
        if obj.vertex_groups:
            report.failures.append(f"static mesh has {len(obj.vertex_groups)} vertex groups")
        if any(m.type == "ARMATURE" for m in obj.modifiers):
            report.failures.append("static mesh has an armature modifier")
        report.stats.pop("bones", None)
        # EnemyForge's ±0.12 m height warning is meant for people; sizes are checked below.
        report.warnings = [w for w in report.warnings if not w.startswith("height ")]

    lo, hi = bbox(obj)
    size = tuple(round(hi[i] - lo[i], 3) for i in range(3))
    report.stats["bbox_m"] = f"{size[0]:.3f}x{size[1]:.3f}x{size[2]:.3f}"

    if bp.kind == "items":
        _check_item_dims(report, bp, size)
        for i, axis in enumerate(("X", "Y")):
            centre = (lo[i] + hi[i]) / 2.0
            if abs(centre) > 0.1 * max(size[i], 1e-6) + 0.01:
                report.warnings.append(f"bbox centre is off the origin in {axis} by {centre:.3f} m")
    elif bp.kind == "structures":
        over = [f"{axis} {lo[i]:.2f}..{hi[i]:.2f}" for i, axis in enumerate(("X", "Y"))
                if lo[i] < -CELL_HALF - 1e-3 or hi[i] > CELL_HALF + 1e-3]
        if over:
            report.failures.append(f"footprint leaves the 12 × 12 m cell: {', '.join(over)}")
        if bp.entry.height_m and size[2] > bp.entry.height_m * (1 + DIM_TOLERANCE):
            report.warnings.append(f"height {size[2]:.2f} m exceeds the spec's "
                                   f"{bp.entry.height_m:.2f} m by more than 10 %")
    elif bp.kind == "enemies":
        _check_enemy(report, obj, bp, lo, hi)
    return report


def _check_enemy(report: Report, obj, bp: Blueprint, lo, hi) -> None:
    """Height (props excluded), stance, facing, rig and skin facts."""
    from . import rig as rigmod   # rig imports blueprint; keep validate importable alone

    target = float(bp.entry.height_m)
    props = rigmod.prop_part_ids(bp)
    part_ids = rigmod.vertex_parts(obj)
    body_z = [v.co.z for v in obj.data.vertices if part_ids[v.index] not in props]
    body_h = max(body_z) - min(body_z)
    report.stats["height_body_m"] = round(body_h, 3)
    report.stats["height_spec_m"] = target
    if props:
        report.stats["height_with_props_m"] = round(hi[2] - lo[2], 3)
    if abs(body_h - target) > target * ENEMY_HEIGHT_TOLERANCE:
        report.failures.append(f"body height {body_h:.3f} m (props excluded) is not within "
                               f"±5 % of the spec's {target:.2f} m")
    # EnemyForge's ±0.12 m height warning measures the props too; ours replaces it.
    report.warnings = [w for w in report.warnings if not w.startswith("height ")]

    for axis, i in (("X", 0), ("Y", 1)):
        centre = (lo[i] + hi[i]) / 2.0
        if abs(centre) > 0.25 * (hi[i] - lo[i]) + 0.05:
            report.failures.append(f"model is not centred on the origin in {axis} "
                                   f"(bbox centre {centre:.3f} m)")
    report.failures += rigmod.facing_problems(bp)

    armature = next((m.object for m in obj.modifiers if m.type == "ARMATURE"), None)
    if armature is not None:
        report.stats["rig_bones"] = len(armature.data.bones)
        report.stats["skinned_bones"] = len(obj.vertex_groups)
        report.stats.pop("bones", None)
        if sum(1 for m in obj.modifiers if m.type == "ARMATURE") != 1:
            report.failures.append("mesh has more than one Armature modifier")
    over = sum(1 for v in obj.data.vertices
               if len([g for g in v.groups if g.weight > 1e-6]) > rigmod.MAX_INFLUENCES)
    if over:
        report.failures.append(f"{over} vertices have more than {rigmod.MAX_INFLUENCES} "
                               f"bone influences")
    skin = obj.get("artforge_skin")
    if skin:
        import json
        record = json.loads(skin)
        rules, heat = record.get("rules", {}), record.get("heat", {})
        report.stats["mean_influences"] = rules.get("mean_influences")
        # Heat weighting can fail without raising: it reports success but leaves every
        # vertex on a single bone, so joints do not blend and the posed view tears (the
        # household knight's helm rivets triggered it on 3 builds in 4). Refuse that
        # binding instead of shipping it.
        if not heat.get("auto_weights", False):
            report.failures.append(f"heat weighting did not run: {heat.get('reason', 'no reason given')}")
        elif heat.get("max_influences", 0) <= 1:
            report.failures.append(
                "heat weighting silently failed: every vertex ended on a single bone, so "
                "joints will not bend smoothly (check small detached parts near joints, "
                "e.g. rivet spheres; see Tools/ArtForge/README.md, Traps)")
        if rules.get("fallback_vertices"):
            report.warnings.append(f"{rules['fallback_vertices']} vertices got no allowed "
                                   f"heat weight and were blended to the nearest allowed "
                                   f"bones by distance")


def _check_item_dims(report: Report, bp: Blueprint, size) -> None:
    spec_dims = bp.entry.dims
    parts = []
    for i, axis in enumerate(AXES):
        target, reason = spec_dims[i], None
        if axis in bp.bbox_overrides:
            target, reason = bp.bbox_overrides[axis]
            report.warnings.append(
                f"{axis} ({AXIS_LABEL[axis]}) checked against {target:.3f} m instead of "
                f"the JSON's {spec_dims[i]:.3f} m: {reason}")
        ratio = size[i] / target
        parts.append(f"{AXIS_LABEL[axis]}={size[i]:.3f}/{target:.3f}")
        if abs(ratio - 1.0) > DIM_TOLERANCE:
            report.failures.append(
                f"{axis} ({AXIS_LABEL[axis]}) extent {size[i]:.3f} m is {ratio * 100 - 100:+.0f} % "
                f"off the expected {target:.3f} m (tolerance ±10 %)")
    report.stats["vs_spec"] = " ".join(parts)
