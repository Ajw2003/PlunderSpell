"""
Shape primitives the Bronze Age, Late Medieval and Age of Powder castle sets
need and the High Medieval set never did: sloped prisms (taluses, glacis,
roof pitches), turned jars, spoked wheels, cannon, down-tapering columns,
conical roofs, flat wall panels (frescoes, windows, portraits, mirrors) and
balustrades. See docs/plans/era-castle-rooms.md.

Runs only inside Blender's Python. Same rules as castle_builders: everything
bottom-flush, painted from palette.py, and anything flush against another
part overlaps it slightly (room_kit.OVERLAP) rather than sharing an exact
vertex position, which validate_in_blender rejects.
"""
import math

import bmesh
from mathutils import Euler, Vector

import castle_builders as cb
import mesh_kit as mk
import room_kit as rk

IN = cb.IN            # 5.5: inner face of each room wall
Q0 = cb.Q0            # 1.8: a quadrant starts this far from each centre line


def prism(bm, uv, pigment, profile, length, loc=(0, 0, 0), along="x"):
    """A straight extrusion of a 2D `profile` [(u, z), ...] (counter-clockwise,
    convex) for `length` metres along `along` ("x" or "y"), centred on `loc`.
    u is the horizontal axis across the extrusion: world Y when along == "x",
    world X when along == "y". Use it for a talus or glacis (a right
    triangle), a gabled coffin lid or a roof pitch."""
    half = (length + rk.OVERLAP) / 2
    lx, ly, lz = loc
    ends = []
    for s in (-half, half):
        ring = []
        for u, z in profile:
            if along == "x":
                ring.append(bm.verts.new((lx + s, ly + u, lz + z)))
            else:
                ring.append(bm.verts.new((lx + u, ly + s, lz + z)))
        ends.append(ring)
    a, b = ends
    n = len(profile)
    faces = [bm.faces.new(list(reversed(a))), bm.faces.new(b)]
    for i in range(n):
        j = (i + 1) % n
        faces.append(bm.faces.new((a[i], a[j], b[j], b[i])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    mk.paint(bm, faces, pigment, uv)
    return faces


def jar(bm, uv, pigment, x, y, z, height, belly, mouth=None, segments=8):
    """A turned storage jar standing on (x, y, z): a narrow foot, a belly at
    about 45% of its height and a rolled rim. Pithos, amphora, hydria and
    specimen jar are all this profile at different proportions."""
    r = belly / 2
    m = (mouth if mouth is not None else belly * 0.45) / 2
    profile = [
        (0.001, 0.0),
        (r * 0.45, 0.0),
        (r * 0.85, height * 0.2),
        (r, height * 0.45),
        (r * 0.8, height * 0.75),
        (m, height * 0.92),
        (m * 1.15, height * 0.97),
        (m * 1.1, height),
        (0.001, height),
    ]
    return mk.paint(bm, mk.add_lathe(bm, profile, segments=segments, loc=(x, y, z)), pigment, uv)


def wheel(bm, uv, pigment, x, y, z_axle, radius, along="x", width=0.08, spokes=4):
    """A spoked wheel standing on the floor, its axle along `along` (the wheel
    faces that axis). Built from a thin rim of box segments plus crossed
    spokes, so it reads as a wheel rather than a disc."""
    segs = 10
    rot_axis = 0 if along == "x" else 1
    for i in range(segs):
        ang = (i + 0.5) / segs * 2 * math.pi
        chord = 2 * radius * math.sin(math.pi / segs) + 0.03
        cu, cz = math.cos(ang) * radius * 0.95, math.sin(ang) * radius * 0.95
        if rot_axis == 0:
            loc = (x, y + cu, z_axle + cz)
            rot = Euler((ang + math.pi / 2, 0, 0))
            size = (width, chord, 0.08)
        else:
            loc = (x + cu, y, z_axle + cz)
            rot = Euler((0, -(ang + math.pi / 2), 0))
            size = (chord, width, 0.08)
        mk.paint(bm, mk.add_box(bm, size, loc=loc, rot=rot), pigment, uv)
    for k in range(spokes // 2):
        ang = k * math.pi / (spokes // 2)
        if rot_axis == 0:
            mk.paint(bm, mk.add_box(bm, (width * 0.8, radius * 1.9, 0.06), loc=(x, y, z_axle),
                                    rot=Euler((ang, 0, 0))), pigment, uv)
        else:
            mk.paint(bm, mk.add_box(bm, (radius * 1.9, width * 0.8, 0.06), loc=(x, y, z_axle),
                                    rot=Euler((0, ang, 0))), pigment, uv)


def cannon(bm, uv, barrel, carriage, x, y, fz, length=2.4, bore=0.3, facing="south", wheels=True,
           wheel_pigment=None):
    """A gun on a carriage, muzzle toward `facing` (a compass side, Blender
    space). A two-wheeled field carriage when `wheels`, else a low four-truck
    garrison/sledge bed. The barrel tapers from breech to muzzle."""
    dx, dy = {"north": (0, 1), "south": (0, -1), "east": (1, 0), "west": (-1, 0)}[facing]
    along_x = dx != 0
    bed_h = 0.55 if wheels else 0.35
    # Carriage cheeks: two side boards under the barrel.
    for o in (-1, 1):
        off = o * bore * 0.9
        cx, cy = (x, y + off) if along_x else (x + off, y)
        size = (length * 0.8, 0.1, bed_h) if along_x else (0.1, length * 0.8, bed_h)
        cb._box(bm, uv, carriage, (cx - dx * length * 0.1, cy - dy * length * 0.1, fz + bed_h / 2), size)
    if wheels:
        r = bed_h + 0.1
        for o in (-1, 1):
            off = o * (bore * 0.9 + 0.12)
            wx, wy = (x, y + off) if along_x else (x + off, y)
            wheel(bm, uv, wheel_pigment or carriage, wx, wy, fz + r, r, along="y" if along_x else "x")
    rot = Euler((0, math.radians(90), 0)) if along_x else Euler((math.radians(90), 0, 0))
    mk.paint(bm, mk.add_cylinder(bm, bore * 0.75, length, loc=(x, y, fz + bed_h + bore * 0.6),
                                 rot=rot, segments=10, radius2=bore * 0.55), barrel, uv)


def tapered_column(bm, uv, shaft, cap, x, y, fz, height, foot_r=0.17, top_r=0.23):
    """A Minoan/Mycenaean column: the shaft widens toward the top, under a
    cushion capital and a square abacus (art bible, the Megaron)."""
    abacus = 0.1
    cush = 0.36
    shaft_h = height - abacus - cush
    mk.paint(bm, mk.add_cylinder(bm, foot_r + 0.13, 0.1, loc=(x, y, fz + 0.05), segments=10), cap, uv)
    mk.paint(bm, mk.add_cylinder(bm, foot_r, shaft_h, loc=(x, y, fz + shaft_h / 2), segments=10,
                                 radius2=top_r), shaft, uv)
    mk.paint(bm, mk.add_cylinder(bm, top_r * 1.1, cush, loc=(x, y, fz + shaft_h + cush / 2), segments=10,
                                 radius2=top_r * 1.5), cap, uv)
    cb._box(bm, uv, cap, (x, y, fz + height - abacus / 2), (top_r * 3, top_r * 3, abacus))


def cone_roof(bm, uv, pigment, x, y, base_z, radius, height, segments=8):
    """A conical tower roof sitting on base_z (Late Medieval towers)."""
    mk.paint(bm, mk.add_cylinder(bm, radius, height, loc=(x, y, base_z + height / 2), segments=segments,
                                 radius2=0.02), pigment, uv)


def wall_panel(bm, uv, pigment, side, along, bottom, width, height, depth=0.05, proud=0.0):
    """A flat panel against a room wall's inner face on `side`, centred at
    `along` on that wall, from `bottom` (absolute Z) up: a fresco band, a
    window, a portrait, a mirror, a tapestry. Touches the wall, so it never
    floats. `proud` pushes it further off the wall (a frame behind a canvas)."""
    off = IN - depth / 2 + 0.01 - proud
    z = bottom + height / 2
    if side == "north":
        cb._box(bm, uv, pigment, (along, off, z), (width, depth, height))
    elif side == "south":
        cb._box(bm, uv, pigment, (along, -off, z), (width, depth, height))
    elif side == "east":
        cb._box(bm, uv, pigment, (off, along, z), (depth, width, height))
    else:
        cb._box(bm, uv, pigment, (-off, along, z), (depth, width, height))


def framed_panel(bm, uv, frame, face, side, along, bottom, width, height, border=0.1):
    """A wall panel with a frame: `frame` pigment behind, `face` pigment in
    front and inset by `border` (window glazing, a portrait, a mirror)."""
    wall_panel(bm, uv, frame, side, along, bottom, width, height, depth=0.06)
    wall_panel(bm, uv, face, side, along, bottom + border, width - 2 * border, height - 2 * border,
               depth=0.04, proud=0.05)


def balustrade(bm, uv, pigment, x0, y0, x1, y1, fz, height=0.9, spacing=0.3):
    """A straight balustrade from (x0, y0) to (x1, y1): turned balusters
    under a handrail, on a plinth rail. Axis-aligned runs only."""
    along_x = abs(x1 - x0) >= abs(y1 - y0)
    length = abs(x1 - x0) if along_x else abs(y1 - y0)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    size = (length, 0.14, 0.1) if along_x else (0.14, length, 0.1)
    cb._box(bm, uv, pigment, (cx, cy, fz + 0.05), size)
    cb._box(bm, uv, pigment, (cx, cy, fz + height - 0.05), size)
    n = max(2, int(length / spacing))
    for i in range(n):
        t = (i + 0.5) / n
        bx = x0 + (x1 - x0) * t
        by = y0 + (y1 - y0) * t
        mk.paint(bm, mk.add_cylinder(bm, 0.05, height - 0.2, loc=(bx, by, fz + height / 2), segments=6,
                                     radius2=0.035), pigment, uv)


def horns_of_consecration(bm, uv, pigment, x, y, z, size=0.8, along="x"):
    """The Minoan cult sign: a plinth with two up-curving horns, standing on z.
    Used as the Bronze Age's parapet crest and on shrine altars."""
    w = size
    base_size = (w, w * 0.3, w * 0.25) if along == "x" else (w * 0.3, w, w * 0.25)
    cb._box(bm, uv, pigment, (x, y, z + w * 0.125), base_size)
    for o in (-1, 1):
        hx, hy = (x + o * w * 0.35, y) if along == "x" else (x, y + o * w * 0.35)
        tilt = math.radians(18) * o
        rot = Euler((0, tilt, 0)) if along == "x" else Euler((-tilt, 0, 0))
        mk.paint(bm, mk.add_cylinder(bm, w * 0.1, w * 0.6, loc=(hx, hy, z + w * 0.25 + w * 0.28), rot=rot,
                                     segments=6, radius2=w * 0.04), pigment, uv)


def disc(bm, uv, pigment, x, y, z, radius, thickness=0.04, segments=12):
    """A flat round slab lying on z: an inlaid hearth ring, a rug, a floor
    grate, a hatch. Thin enough to stay walkable under validate_castle_layout's
    FLAT_DECOR when `thickness` <= 0.1."""
    return mk.paint(bm, mk.add_cylinder(bm, radius, thickness, loc=(x, y, z + thickness / 2), segments=segments),
                    pigment, uv)


def sphere(bm, uv, pigment, x, y, z_bottom, radius, segments=8, rings=6):
    """A sphere resting on z_bottom (a gunstone, a shot pile, a globe, an
    orange-tree crown)."""
    return mk.paint(bm, mk.add_sphere(bm, radius, loc=(x, y, z_bottom + radius), segments=segments, rings=rings),
                    pigment, uv)


__all__ = [
    "prism", "jar", "wheel", "cannon", "tapered_column", "cone_roof", "wall_panel",
    "framed_panel", "balustrade", "horns_of_consecration", "disc", "sphere", "Vector",
]
