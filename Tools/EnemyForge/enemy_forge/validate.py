"""Geometry validation for exported enemy meshes.

Checks the things that actually break a game import: inverted normals, holes,
unwelded transforms, a pivot in the wrong place, degenerate faces, dead UVs and
unskinned vertices. Every check returns human-readable failures rather than
asserting, so one run reports every problem across the whole roster.
"""

from __future__ import annotations

from dataclasses import dataclass, field

import bmesh
import bpy
from mathutils import Vector

from .archetypes import Archetype


@dataclass
class Report:
    name: str
    stats: dict = field(default_factory=dict)
    failures: list[str] = field(default_factory=list)
    warnings: list[str] = field(default_factory=list)

    @property
    def passed(self) -> bool:
        return not self.failures


def _islands(bm) -> list[list]:
    """Group faces into connected components so each closed shell can be checked alone."""
    parent = list(range(len(bm.verts)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i

    for edge in bm.edges:
        a, b = find(edge.verts[0].index), find(edge.verts[1].index)
        if a != b:
            parent[a] = b

    buckets: dict[int, list] = {}
    for face in bm.faces:
        buckets.setdefault(find(face.verts[0].index), []).append(face)
    return list(buckets.values())


def _signed_volume(faces) -> float:
    """Divergence-theorem volume. Negative means the shell is inside-out."""
    total = 0.0
    for face in faces:
        verts = face.verts
        origin = verts[0].co
        for i in range(1, len(verts) - 1):
            total += origin.cross(verts[i].co).dot(verts[i + 1].co)
    return total / 6.0


def validate(obj: bpy.types.Object, arch: Archetype) -> Report:
    report = Report(arch.name)

    if tuple(round(v, 6) for v in obj.location) != (0.0, 0.0, 0.0):
        report.failures.append(f"origin is not at the world origin: {tuple(obj.location)}")
    if tuple(round(v, 6) for v in obj.scale) != (1.0, 1.0, 1.0):
        report.failures.append(f"object scale is not applied: {tuple(obj.scale)}")
    if any(round(v, 6) for v in obj.rotation_euler):
        report.failures.append(f"object rotation is not applied: {tuple(obj.rotation_euler)}")

    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.index_update()
    bm.faces.index_update()

    tris = sum(len(f.verts) - 2 for f in bm.faces)
    report.stats.update(
        vertices=len(bm.verts), faces=len(bm.faces), triangles=tris,
        islands=len(_islands(bm)), bones=0,
    )

    if tris > arch.tri_budget:
        report.failures.append(f"{tris} triangles exceeds the {arch.tri_budget} budget")

    loose_verts = [v.index for v in bm.verts if not v.link_faces]
    if loose_verts:
        report.failures.append(f"{len(loose_verts)} loose vertices with no faces")
    loose_edges = [e.index for e in bm.edges if not e.link_faces]
    if loose_edges:
        report.failures.append(f"{len(loose_edges)} wire edges with no faces")

    open_edges = [e for e in bm.edges if len(e.link_faces) != 2]
    if open_edges:
        report.failures.append(
            f"{len(open_edges)} non-manifold edges (holes or internal faces)")

    inconsistent = [e for e in bm.edges
                    if len(e.link_faces) == 2 and not e.is_contiguous]
    if inconsistent:
        report.failures.append(f"{len(inconsistent)} edges with inconsistent winding")

    inverted = [shell for shell in _islands(bm) if _signed_volume(shell) <= 0.0]
    if inverted:
        report.failures.append(f"{len(inverted)} shells have inverted normals")

    degenerate = [f.index for f in bm.faces if f.calc_area() < 1e-9]
    if degenerate:
        report.failures.append(f"{len(degenerate)} degenerate (zero-area) faces")

    uv_layer = bm.loops.layers.uv.active
    if uv_layer is None:
        report.failures.append("no UV map")
    else:
        flat_uvs, out_of_range = 0, 0
        for face in bm.faces:
            coords = [loop[uv_layer].uv for loop in face.loops]
            area = 0.0
            for i in range(1, len(coords) - 1):
                a, b = coords[i] - coords[0], coords[i + 1] - coords[0]
                area += abs(a.x * b.y - a.y * b.x) * 0.5
            if area < 1e-10:
                flat_uvs += 1
            if any(u < -0.001 or u > 1.001 for uv in coords for u in uv):
                out_of_range += 1
        if flat_uvs:
            report.failures.append(f"{flat_uvs} faces have zero-area UVs")
        if out_of_range:
            report.failures.append(f"{out_of_range} faces have UVs outside 0..1")

    slots = len(obj.data.materials)
    if slots != 1:
        report.failures.append(f"expected exactly one baked material slot, found {slots}")
    stray = {f.material_index for f in bm.faces} - {0}
    if stray:
        report.failures.append(f"faces reference missing material slots: {sorted(stray)}")

    lowest = min(v.co.z for v in bm.verts)
    highest = max(v.co.z for v in bm.verts)
    width_x = max(v.co.x for v in bm.verts) - min(v.co.x for v in bm.verts)
    centre_x = (max(v.co.x for v in bm.verts) + min(v.co.x for v in bm.verts)) / 2.0
    report.stats.update(lowest_z=round(lowest, 4), height=round(highest, 3),
                        width=round(width_x, 3))

    if arch.grounded:
        if abs(lowest) > 0.02:
            report.failures.append(f"feet are at z={lowest:.3f}, not on the ground plane")
    elif lowest < 0.04:
        report.failures.append(
            f"flier geometry reaches z={lowest:.3f}; it should hover clear of the ground")

    if abs(highest - arch.height) > 0.12:
        report.warnings.append(
            f"height {highest:.2f} m differs from the declared {arch.height:.2f} m")
    if abs(centre_x) > 0.35:
        report.warnings.append(f"silhouette is off-centre in X by {centre_x:.2f} m")

    # Every vertex must be fully skinned, or it will be left behind when the rig moves.
    group_count = len(obj.vertex_groups)
    report.stats["bones"] = group_count
    unweighted = 0
    underweighted = 0
    for vert in mesh.vertices:
        if not vert.groups:
            unweighted += 1
        elif abs(sum(g.weight for g in vert.groups) - 1.0) > 1e-3:
            underweighted += 1
    if unweighted:
        report.failures.append(f"{unweighted} vertices are not assigned to any bone")
    if underweighted:
        report.failures.append(f"{underweighted} vertices have weights that do not sum to 1")

    armature = next((m for m in obj.modifiers if m.type == "ARMATURE"), None)
    if armature is None or armature.object is None:
        report.failures.append("mesh has no bound armature modifier")
    else:
        bone_names = {b.name for b in armature.object.data.bones}
        missing = {g.name for g in obj.vertex_groups} - bone_names
        if missing:
            report.failures.append(f"vertex groups without bones: {sorted(missing)}")

    bm.free()
    return report


def format_report(report: Report) -> str:
    status = "PASS" if report.passed else "FAIL"
    stats = "  ".join(f"{k}={v}" for k, v in report.stats.items())
    lines = [f"[{status}] {report.name}", f"        {stats}"]
    lines += [f"        ! {f}" for f in report.failures]
    lines += [f"        ~ {w}" for w in report.warnings]
    return "\n".join(lines)
