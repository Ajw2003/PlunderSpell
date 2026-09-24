"""Gate every model on EnemyForge's geometry checks plus the art bible's sizes.

EnemyForge's `validate()` is run unchanged through a small adapter (it reads name,
tri_budget, height and grounded). It was written for rigged enemies, so on the
static path its four rig checks are expected to fire and are removed by exact
message; nothing else it reports is filtered. Then, per kind:

- items: bounding box within ±10 % of the JSON `dimensions` on each axis.
  W is X, D is Y, H is Z; the model stands on z = 0 and faces -Y.
- structures: footprint inside the 12 × 12 m cell (|x|, |y| ≤ 6 m).
- enemies: height within ±5 % of the JSON height_m.
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
        target = float(bp.entry.height_m)
        if abs(size[2] - target) > target * ENEMY_HEIGHT_TOLERANCE:
            report.failures.append(f"height {size[2]:.3f} m is not within ±5 % of the "
                                   f"spec's {target:.2f} m")
    return report


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
