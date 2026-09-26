"""Declarative part primitives for Plunderspell enemy meshes.

An archetype is described as a flat list of `Part` records. Each part becomes one
closed, manifold island in a single joined mesh. Keeping every part disjoint (no
welding between parts) is deliberate: it lets the per-vertex `mat_id` attribute
stay crisp at part boundaries without needing UV seams or multiple materials.
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field

import bmesh
from mathutils import Euler, Matrix, Vector

# Surface families of the castle household. The index is the face's material slot,
# so this order is the order materials.build_authoring_set() must append them in.
# Deliberately contains no gold: the moodboard spends orpiment on money alone.
MATERIALS = {
    "wool": 0,     # undyed tunic, hose, surcoat, hound's coat
    "leather": 1,  # boots, belts, straps, collar
    "oak": 2,      # hafts, staves, the lantern's frame
    "mail": 3,     # riveted iron mail, dark and rough
    "steel": 4,    # helm, plate, blade — the only bright metal
    "linen": 5,    # undershirts, coifs, grave wrappings
    "flesh": 6,    # hands and faces
    "bone": 7,     # skulls, teeth, the sergeant's horn
    "madder": 8,   # the alarm's colour, worn by the man who raises it
    "tallow": 9,   # lantern flame (emissive)
    "lapis": 10,   # the voice, and whatever answers it (emissive, crypt only)
}

MIRROR = Matrix.Diagonal((-1.0, 1.0, 1.0, 1.0))


@dataclass
class Part:
    """One primitive island.

    `size` is the full extent of the primitive on each axis (not a half-extent), so
    a box with size (1, 1, 1) spans exactly one metre. `rot` is XYZ euler degrees.
    `mirror` emits a second copy reflected across the YZ plane, with its faces
    flipped so the reflected island keeps outward-facing normals.
    """

    kind: str
    loc: tuple[float, float, float]
    size: tuple[float, float, float]
    mat: str = "stone"
    bone: str = "Root"
    rot: tuple[float, float, float] = (0.0, 0.0, 0.0)
    mirror: bool = False
    segments: int = 12
    rings: int = 8
    taper: float = 1.0          # cone/cylinder: top radius as a fraction of bottom
    subdivisions: int = 2       # icosphere only
    minor: float = 0.18         # torus only: tube radius as a fraction of major radius
    extras: dict = field(default_factory=dict)


def _call_with_radius(op, bm, **kwargs):
    """Call a bmesh primitive op across Blender's radius/diameter argument renames."""
    try:
        return op(bm, **kwargs)
    except TypeError:
        swapped = {}
        for key, value in kwargs.items():
            if key == "radius":
                swapped["diameter"] = value * 2.0
            elif key.startswith("radius"):
                swapped["diameter" + key[len("radius"):]] = value * 2.0
            else:
                swapped[key] = value
        return op(bm, **swapped)


def _torus(bm, major_segments: int, minor_segments: int, minor_ratio: float):
    """Build a unit-diameter torus by hand; bmesh has no torus primitive."""
    major_r, minor_r = 0.5, 0.5 * minor_ratio
    grid = []
    for i in range(major_segments):
        theta = 2.0 * math.pi * i / major_segments
        centre = Vector((math.cos(theta) * major_r, math.sin(theta) * major_r, 0.0))
        outward = Vector((math.cos(theta), math.sin(theta), 0.0))
        ring = []
        for j in range(minor_segments):
            phi = 2.0 * math.pi * j / minor_segments
            offset = outward * (math.cos(phi) * minor_r) + Vector((0.0, 0.0, math.sin(phi) * minor_r))
            ring.append(bm.verts.new(centre + offset))
        grid.append(ring)

    for i in range(major_segments):
        ring_a, ring_b = grid[i], grid[(i + 1) % major_segments]
        for j in range(minor_segments):
            k = (j + 1) % minor_segments
            bm.faces.new((ring_a[j], ring_a[k], ring_b[k], ring_b[j]))
    return [v for ring in grid for v in ring]


def _shard(bm, sides: int):
    """An elongated bipyramid: the signature silhouette element of the arcane debris."""
    belt = []
    for i in range(sides):
        theta = 2.0 * math.pi * i / sides
        belt.append(bm.verts.new((math.cos(theta) * 0.5, math.sin(theta) * 0.5, 0.0)))
    top = bm.verts.new((0.0, 0.0, 0.5))
    bottom = bm.verts.new((0.0, 0.0, -0.5))

    for i in range(sides):
        j = (i + 1) % sides
        bm.faces.new((belt[i], belt[j], top))
        bm.faces.new((belt[j], belt[i], bottom))
    return belt + [top, bottom]


def _primitive(bm, part: Part) -> list:
    """Create one primitive at unit scale, centred on the origin, and return its verts."""
    kind = part.kind
    if kind == "box":
        return _call_with_radius(bmesh.ops.create_cube, bm, size=1.0)["verts"]
    if kind in {"cyl", "cone"}:
        top = 0.0 if kind == "cone" else 0.5 * part.taper
        return _call_with_radius(
            bmesh.ops.create_cone, bm,
            cap_ends=True, cap_tris=False, segments=part.segments,
            radius1=0.5, radius2=top, depth=1.0,
        )["verts"]
    if kind == "sphere":
        return _call_with_radius(
            bmesh.ops.create_uvsphere, bm,
            u_segments=part.segments, v_segments=part.rings, radius=0.5,
        )["verts"]
    if kind == "ico":
        return _call_with_radius(
            bmesh.ops.create_icosphere, bm,
            subdivisions=part.subdivisions, radius=0.5,
        )["verts"]
    if kind == "torus":
        return _torus(bm, part.segments, max(4, part.rings), part.minor)
    if kind == "shard":
        return _shard(bm, max(3, part.segments))
    raise ValueError(f"unknown part kind: {kind!r}")


BONE_LAYER = "bone_id"


def _place(bm, part: Part, verts: list, mirrored: bool) -> None:
    """Transform one freshly created part into place, using references while valid."""
    basis = (
        Matrix.Translation(Vector(part.loc))
        @ Euler([math.radians(a) for a in part.rot], "XYZ").to_matrix().to_4x4()
        @ Matrix.Diagonal(Vector(part.size).to_4d())
    )
    if mirrored:
        basis = MIRROR @ basis
    bmesh.ops.transform(bm, matrix=basis, verts=verts)
    if mirrored:
        # Reflection flips handedness, which would leave this island inside-out.
        faces = {f for v in verts for f in v.link_faces}
        bmesh.ops.reverse_faces(bm, faces=list(faces))


def _bone_name(part: Part, mirrored: bool) -> str:
    if mirrored and part.bone.endswith(".L"):
        return part.bone[:-2] + ".R"
    return part.bone


def build_bmesh(parts: list[Part]) -> tuple[bmesh.types.BMesh, list[str]]:
    """Assemble every part into one bmesh and return it with its bone-name table.

    Faces carry their surface family as `material_index`; verts carry an index into
    the returned bone-name table in the `bone_id` int layer. Both are stamped inline,
    while the primitive op's references are still live — see "bmesh references and
    index order both go stale" in docs/4-systems/enemy-asset-pipeline.md.
    """
    bm = bmesh.new()
    bone_layer = bm.verts.layers.int.new(BONE_LAYER)
    bone_names: list[str] = []
    bone_lookup: dict[str, int] = {}

    for part in parts:
        if part.mat not in MATERIALS:
            raise ValueError(f"part references unknown material {part.mat!r}")
        family = MATERIALS[part.mat]
        for mirrored in ((False, True) if part.mirror else (False,)):
            bone = _bone_name(part, mirrored)
            if bone not in bone_lookup:
                bone_lookup[bone] = len(bone_names)
                bone_names.append(bone)
            bone_id = bone_lookup[bone]

            verts = _primitive(bm, part)
            _place(bm, part, verts, mirrored)
            for vert in verts:
                vert[bone_layer] = bone_id
            for face in {f for v in verts for f in v.link_faces}:
                face.material_index = family

    bm.verts.index_update()
    bm.faces.index_update()
    return bm, bone_names
