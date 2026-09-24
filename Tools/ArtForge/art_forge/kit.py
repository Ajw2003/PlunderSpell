"""Geometry kit: EnemyForge's parts plus the shapes plunder and architecture need.

Every EnemyForge part kind (box, cyl, cone, sphere, ico, torus, shard) works here
unchanged — they are built by `enemy_forge.parts._primitive`. Three kinds are new:

`lathe`  revolve a (radius, z) profile about local Z into a closed solid.
         extras["profile"] = [(r, z), ...] in metres, bottom to top. An end point
         with r == 0 becomes a pole; an end with r > 0 is capped flat. Interior
         points must have r > 0. The profile may turn back down (a cup's inside).
         `segments` = number of sides. `size` scales the result (use (1, 1, 1)
         for true metres; (1, 0.8, 1) squashes it oval).

`prism`  extrude a 2D outline along local Z.
         extras["outline"] = [(x, y), ...] in metres, any winding, may be concave
         (caps are triangulated by polygon fill, not fanned). Depth is size[2]
         (the unit prism spans z = -0.5..0.5); size[0], size[1] scale the outline
         (use 1.0 for true metres).

`tube`   sweep a cross-section along a 3D polyline: handles, cords, chains, drips.
         extras["path"] = [(x, y, z), ...] in metres; extras["section"] = (rn, rb)
         half-extents of the elliptical section (rn along the frame normal, rb
         along the binormal); extras["up"] = hint vector for the frame normal
         (default: whichever world axis is least parallel to the first segment);
         extras["closed"] = True joins the ends into a ring (no caps).
         `segments` = sides of the section.

Any part may set extras["smooth"] = True: every edge inside that part shades
smooth whatever its angle. EnemyForge's auto-smooth is 34°, so without this a
cord or strap with fewer than 11 sides renders visibly faceted.

Any part may set extras["bevel"] = False to keep the asset's edge bevel off it.
Worth it on small discs, low-sided tubes and hidden relief, where the bevel
roughly triples the triangles for no visible gain.

Any part may also carry extras["paint"]: a list of regions that restamp faces with
another family, so a band of paint is part of the same closed shell (no floating
decal geometry). Each region is {"mat": family, "min": (x, y, z), "max": (x, y, z)}
in the part's LOCAL coordinates (the same metres as a lathe profile or prism
outline), tested against each face's centroid. Put profile points or outline
vertices on the band edges if you want them crisp.

Invariants are EnemyForge's: every part is its own closed, manifold island with
outward normals; material index and bone id are stamped inline while the op's
references are live (see "bmesh references and index order both go stale" in
docs/systems/enemy-asset-pipeline.md).
"""

from __future__ import annotations

import math

import bpy  # noqa: F401  (importing bpy is what makes bmesh importable)
import bmesh
from mathutils import Vector
from mathutils.geometry import tessellate_polygon

from enemy_forge.parts import BONE_LAYER, Part, _bone_name, _place, _primitive

__all__ = ["Part", "build_bmesh", "families_used", "ring_of", "arc_path", "spline",
           "rounded_rect", "gable_outline", "BONE_LAYER", "SMOOTH_LAYER", "NOBEVEL_LAYER"]

# Face layer: 1 where the owning part asked for extras["smooth"]. An integer face
# layer, like material_index, survives the bevel intact.
SMOOTH_LAYER = "af_smooth"
# Face layer: 1 where the owning part asked for extras["bevel"] = False.
NOBEVEL_LAYER = "af_nobevel"

NEW_KINDS = {"lathe", "prism", "tube"}


# --------------------------------------------------------------------------------
# New primitives. Each returns (verts, faces) created in `bm`, in local metres.
# --------------------------------------------------------------------------------

def _faces_volume(faces) -> float:
    total = 0.0
    for face in faces:
        verts = face.verts
        origin = verts[0].co
        for i in range(1, len(verts) - 1):
            total += origin.cross(verts[i].co).dot(verts[i + 1].co)
    return total / 6.0


def _orient_outward(bm, faces: list) -> None:
    """Flip a freshly built closed island if it came out inside-out."""
    if _faces_volume(faces) < 0.0:
        bmesh.ops.reverse_faces(bm, faces=faces)


def _lathe(bm, part: Part):
    profile = [(float(r), float(z)) for r, z in part.extras["profile"]]
    if len(profile) < 2:
        raise ValueError("lathe profile needs at least two points")
    segments = max(3, part.segments)
    eps = 1e-6
    for i, (r, _z) in enumerate(profile):
        if r < -eps:
            raise ValueError(f"lathe profile point {i} has negative radius {r}")
        if r <= eps and 0 < i < len(profile) - 1:
            raise ValueError(f"lathe profile point {i} has zero radius mid-profile; "
                             "only the first and last points may be poles")

    rows = []
    verts = []
    for r, z in profile:
        if r <= eps:
            pole = bm.verts.new((0.0, 0.0, z))
            rows.append([pole])
            verts.append(pole)
        else:
            row = []
            for j in range(segments):
                theta = 2.0 * math.pi * j / segments
                row.append(bm.verts.new((math.cos(theta) * r, math.sin(theta) * r, z)))
            rows.append(row)
            verts.extend(row)

    faces = []
    for a, b in zip(rows, rows[1:]):
        for j in range(segments):
            k = (j + 1) % segments
            if len(a) == 1 and len(b) == 1:
                raise ValueError("lathe profile has two consecutive poles")
            if len(a) == 1:
                quad = (a[0], b[k], b[j])
            elif len(b) == 1:
                quad = (a[j], a[k], b[0])
            else:
                quad = (a[j], a[k], b[k], b[j])
            faces.append(bm.faces.new(quad))
    if len(rows[0]) > 1:
        faces.append(bm.faces.new(list(reversed(rows[0]))))
    if len(rows[-1]) > 1:
        faces.append(bm.faces.new(rows[-1]))

    _orient_outward(bm, faces)
    return verts, faces


def _signed_area(points) -> float:
    return 0.5 * sum(points[i][0] * points[(i + 1) % len(points)][1]
                     - points[(i + 1) % len(points)][0] * points[i][1]
                     for i in range(len(points)))


def _prism(bm, part: Part):
    outline = [(float(x), float(y)) for x, y in part.extras["outline"]]
    if len(outline) >= 2 and outline[0] == outline[-1]:
        outline = outline[:-1]
    if len(outline) < 3:
        raise ValueError("prism outline needs at least three points")
    if abs(_signed_area(outline)) < 1e-10:
        raise ValueError("prism outline has zero area")
    if _signed_area(outline) < 0.0:
        outline.reverse()   # counter-clockwise from +Z

    n = len(outline)
    bottom = [bm.verts.new((x, y, -0.5)) for x, y in outline]
    top = [bm.verts.new((x, y, 0.5)) for x, y in outline]

    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces.append(bm.faces.new((bottom[i], bottom[j], top[j], top[i])))

    # Polygon fill copes with concave outlines; a fan or a single n-gon would not
    # triangulate the same way in every importer.
    triangles = tessellate_polygon([[Vector((x, y, 0.0)) for x, y in outline]])
    for tri in triangles:
        a, b, c = tri
        pa, pb, pc = outline[a], outline[b], outline[c]
        ccw = ((pb[0] - pa[0]) * (pc[1] - pa[1]) - (pb[1] - pa[1]) * (pc[0] - pa[0])) > 0.0
        if not ccw:
            b, c = c, b
        faces.append(bm.faces.new((top[a], top[b], top[c])))
        faces.append(bm.faces.new((bottom[a], bottom[c], bottom[b])))

    _orient_outward(bm, faces)
    return bottom + top, faces


def _tube(bm, part: Part):
    path = [Vector(p) for p in part.extras["path"]]
    closed = bool(part.extras.get("closed", False))
    if closed and (path[0] - path[-1]).length < 1e-9:
        path = path[:-1]
    if len(path) < 2:
        raise ValueError("tube path needs at least two points")
    rn, rb = part.extras.get("section", (0.01, 0.01))
    sides = max(3, part.segments)
    count = len(path)

    def tangent(i):
        if closed:
            t = path[(i + 1) % count] - path[(i - 1) % count]
        elif i == 0:
            t = path[1] - path[0]
        elif i == count - 1:
            t = path[-1] - path[-2]
        else:
            t = (path[i + 1] - path[i]).normalized() + (path[i] - path[i - 1]).normalized()
        if t.length < 1e-9:
            raise ValueError(f"tube path doubles back on itself at point {i}")
        return t.normalized()

    t0 = tangent(0)
    hint = part.extras.get("up")
    if hint is None:
        axes = [Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))]
        hint = min(axes, key=lambda a: abs(a.dot(t0)))
    normal = Vector(hint) - Vector(hint).dot(t0) * t0
    if normal.length < 1e-6:
        raise ValueError("tube 'up' hint is parallel to the path")
    normal.normalize()

    rings = []
    verts = []
    for i in range(count):
        t = tangent(i)
        # Parallel transport: remove the tangential component, keep the twist minimal.
        normal = (normal - normal.dot(t) * t)
        if normal.length < 1e-6:
            raise ValueError(f"tube frame collapsed at point {i}")
        normal.normalize()
        binormal = t.cross(normal)
        # A mitred joint: widen the section where the path bends so the wall
        # keeps its thickness through the corner.
        scale, bend = 1.0, None
        if 0 < i < count - 1 or closed:
            prev = (path[i] - path[(i - 1) % count]).normalized()
            bend = prev - prev.dot(t) * t
            if bend.length > 1e-6:
                bend.normalize()
                scale = 1.0 / max(0.35, prev.dot(t))
            else:
                bend = None
        ring = []
        for j in range(sides):
            phi = 2.0 * math.pi * j / sides
            offset = normal * (math.cos(phi) * rn) + binormal * (math.sin(phi) * rb)
            if bend is not None:
                # Only the component in the bend plane needs the mitre stretch.
                offset = offset + bend * (offset.dot(bend) * (scale - 1.0))
            ring.append(bm.verts.new(path[i] + offset))
        rings.append(ring)
        verts.extend(ring)

    faces = []
    spans = count if closed else count - 1
    for i in range(spans):
        a, b = rings[i], rings[(i + 1) % count]
        for j in range(sides):
            k = (j + 1) % sides
            faces.append(bm.faces.new((a[j], a[k], b[k], b[j])))
    if not closed:
        faces.append(bm.faces.new(list(reversed(rings[0]))))
        faces.append(bm.faces.new(rings[-1]))

    _orient_outward(bm, faces)
    return verts, faces


_NEW_BUILDERS = {"lathe": _lathe, "prism": _prism, "tube": _tube}


# --------------------------------------------------------------------------------
# Assembly
# --------------------------------------------------------------------------------

def _paint_regions(part: Part) -> list[dict]:
    regions = part.extras.get("paint", [])
    for region in regions:
        missing = {"mat", "min", "max"} - set(region)
        if missing:
            raise ValueError(f"paint region {region} is missing {sorted(missing)}")
    return regions


def families_used(parts: list[Part]) -> set[str]:
    """Every family a part list will put on the mesh, including painted regions."""
    used = set()
    for part in parts:
        used.add(part.mat)
        used.update(region["mat"] for region in _paint_regions(part))
    return used


def _face_family(face, regions, default: int, family_index: dict[str, int]) -> int:
    centre = face.calc_center_median()
    for region in reversed(regions):   # later regions paint over earlier ones
        lo, hi = region["min"], region["max"]
        if all(lo[a] <= centre[a] <= hi[a] for a in range(3)):
            return family_index[region["mat"]]
    return default


def build_bmesh(parts: list[Part], family_index: dict[str, int]):
    """Assemble parts into one bmesh; return it with its bone-name table.

    `family_index` maps a family slug to the material slot it will occupy. Faces
    carry that slot as `material_index`, verts carry an index into the returned
    bone table in the `bone_id` layer, both stamped inline.
    """
    bm = bmesh.new()
    bone_layer = bm.verts.layers.int.new(BONE_LAYER)
    smooth_layer = bm.faces.layers.int.new(SMOOTH_LAYER)
    nobevel_layer = bm.faces.layers.int.new(NOBEVEL_LAYER)
    bone_names: list[str] = []
    bone_lookup: dict[str, int] = {}

    for number, part in enumerate(parts):
        for name in {part.mat} | {r["mat"] for r in _paint_regions(part)}:
            if name not in family_index:
                raise ValueError(f"part {number} ({part.kind}) uses family {name!r}, "
                                 f"which is not in this asset's families "
                                 f"{sorted(family_index)}")
        if any(abs(s) < 1e-9 for s in part.size):
            raise ValueError(f"part {number} ({part.kind}) has a zero size component")
        regions = _paint_regions(part)
        default = family_index[part.mat]

        for mirrored in ((False, True) if part.mirror else (False,)):
            bone = _bone_name(part, mirrored)
            if bone not in bone_lookup:
                bone_lookup[bone] = len(bone_names)
                bone_names.append(bone)
            bone_id = bone_lookup[bone]

            if part.kind in _NEW_BUILDERS:
                verts, faces = _NEW_BUILDERS[part.kind](bm, part)
            else:
                verts = _primitive(bm, part)
                faces = list({f for v in verts for f in v.link_faces})

            # Families are decided in local space, before placement moves the verts.
            stamps = [_face_family(f, regions, default, family_index) if regions else default
                      for f in faces]
            for region in regions:
                if family_index[region["mat"]] not in stamps and region["mat"] != part.mat:
                    raise ValueError(
                        f"part {number} ({part.kind}): paint region {region} contains no "
                        f"face centroid — add profile/outline points on the band edges")
            _place(bm, part, verts, mirrored)
            # A negative size is a reflection too, and would turn the island inside out.
            negative = sum(1 for s in part.size if s < 0.0) % 2 == 1
            if negative:
                bmesh.ops.reverse_faces(bm, faces=faces)
            for vert in verts:
                vert[bone_layer] = bone_id
            smooth = 1 if part.extras.get("smooth") else 0
            nobevel = 0 if part.extras.get("bevel", True) else 1
            for face, family in zip(faces, stamps):
                face.material_index = family
                face[smooth_layer] = smooth
                face[nobevel_layer] = nobevel

    bm.verts.index_update()
    bm.faces.index_update()
    return bm, bone_names


# --------------------------------------------------------------------------------
# Authoring helpers for blueprints
# --------------------------------------------------------------------------------

def ring_of(count: int, radius: float, z: float, start_deg: float = 0.0,
            face_out: bool = True, **part_kwargs) -> list[Part]:
    """Repeat a part evenly around the Z axis (like EnemyForge's archetypes.ring)."""
    base_loc = part_kwargs.pop("loc", (0.0, 0.0, 0.0))
    base_rot = part_kwargs.pop("rot", (0.0, 0.0, 0.0))
    out = []
    for i in range(count):
        yaw = start_deg + 360.0 * i / count
        theta = math.radians(yaw)
        out.append(Part(
            loc=(base_loc[0] + math.cos(theta) * radius,
                 base_loc[1] + math.sin(theta) * radius,
                 base_loc[2] + z),
            rot=(base_rot[0], base_rot[1], base_rot[2] + (yaw if face_out else 0.0)),
            **part_kwargs,
        ))
    return out


def arc_path(centre, radius: float, start_deg: float, end_deg: float, steps: int,
             axis_u=(1.0, 0.0, 0.0), axis_v=(0.0, 0.0, 1.0)) -> list[tuple]:
    """Points on a circular arc in the plane spanned by axis_u, axis_v (for tubes)."""
    c, u, v = Vector(centre), Vector(axis_u), Vector(axis_v)
    out = []
    for i in range(steps + 1):
        a = math.radians(start_deg + (end_deg - start_deg) * i / steps)
        out.append(tuple(c + u * (math.cos(a) * radius) + v * (math.sin(a) * radius)))
    return out


def rounded_rect(width: float, height: float, radius: float, steps: int = 3,
                 cx: float = 0.0, cy: float = 0.0) -> list[tuple]:
    """A rectangle outline with rounded corners, for prisms."""
    radius = min(radius, width / 2.0, height / 2.0)
    hw, hh = width / 2.0 - radius, height / 2.0 - radius
    out = []
    for corner, (sx, sy) in enumerate(((1, 1), (-1, 1), (-1, -1), (1, -1))):
        start = 90.0 * corner
        for i in range(steps + 1):
            a = math.radians(start + 90.0 * i / steps)
            out.append((cx + sx * hw + math.cos(a) * radius,
                        cy + sy * hh + math.sin(a) * radius))
    return out


def gable_outline(width: float, shoulder: float, apex: float,
                  base: float = 0.0) -> list[tuple]:
    """A house-shaped outline in XY: rectangle from `base` to `shoulder`, then a
    gable rising to a point at `apex`. For panels, pediments and shrine roofs."""
    hw = width / 2.0
    return [(-hw, base), (hw, base), (hw, shoulder), (0.0, apex), (-hw, shoulder)]


def spline(points, per_segment: int = 3) -> list[tuple]:
    """Catmull-Rom through `points` (2D or 3D), `per_segment` samples per span.

    For tube paths and lathe profiles that should read as curves: pass a handful of
    control points and let this add the in-betweens. Endpoints are kept exactly.
    """
    pts = [Vector(p) if len(p) == 3 else Vector((p[0], p[1], 0.0)) for p in points]
    dim = len(points[0])
    if len(pts) < 3:
        return [tuple(p) for p in points]
    ext = [pts[0] * 2 - pts[1]] + pts + [pts[-1] * 2 - pts[-2]]
    out = []
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        for step in range(per_segment):
            t = step / per_segment
            t2, t3 = t * t, t * t * t
            q = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                       + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)
            out.append(tuple(q)[:dim])
    out.append(tuple(pts[-1])[:dim])
    return out
