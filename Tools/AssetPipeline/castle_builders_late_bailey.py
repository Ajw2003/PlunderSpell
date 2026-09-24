"""
Late Medieval OuterBailey pieces: LateArtilleryYard, LateGunFoundry, LateHandgunnerBarracks, LateBrewhouse, LateTreadwheelWell.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/LateMedieval/<Key>.json,
drawing: docs/art/rooms/concept/LateMedieval/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_late.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_late import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


GUNSTONE = "vellum_faint"


def keg(bm, uv, x, y, z, r=0.25, h=0.5, lying=False):
    """A powder keg with two iron hoops, standing on z, or lying along x centred at
    height z + r when `lying`."""
    rot = Euler((0, math.radians(90), 0)) if lying else None
    cz = z + (r if lying else h / 2)
    mk.paint(bm, mk.add_cylinder(bm, r, h, loc=(x, y, cz), rot=rot, segments=10), TIMBER, uv)
    for f in (-0.3, 0.3):
        loc = (x + f * h, y, cz) if lying else (x, y, cz + f * h)
        mk.paint(bm, mk.add_cylinder(bm, r + 0.015, 0.04, loc=loc, rot=rot, segments=10), IRON, uv)


def build_late_artillery_yard(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateArtilleryYard.svg"""
    h, fz = room_shell(bm, uv, "OuterBailey")
    # NW: the timber sledge, three cleats, the bombard lashed into it.
    sx, sy = -3.6, 4.2
    cb._box(bm, uv, TIMBER, (sx, sy, fz + 0.175), (3.0, 0.9, 0.35))
    for x in (sx - 1.2, sx, sx + 1.2):
        cb._box(bm, uv, TIMBER, (x + 0.06, sy, fz + 0.55), (0.12, 0.9, 0.5))
    bz = fz + 0.35 + 0.36 - 0.06
    along = Euler((0, math.radians(90), 0))
    mk.paint(bm, mk.add_cylinder(bm, 0.25, 1.0, loc=(sx - 1.0, sy, bz), rot=along, segments=10), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.36, 1.9, loc=(sx + 0.45, sy, bz), rot=along, segments=12), IRON, uv)
    for k in range(7):
        mk.paint(bm, mk.add_cylinder(bm, 0.38, 0.06, loc=(sx - 0.42 + k * 0.3, sy, bz), rot=along, segments=12),
                 "line", uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.27, 0.02, loc=(sx + 1.41, sy, bz), rot=along, segments=10), SOOT, uv)
    for x in (sx - 1.0, sx + 0.95):
        cb._box(bm, uv, "vellum_faint", (x, sy, bz - 0.02), (0.05, 0.94, 0.8))
    # NE: the gunstone pyramid, 3-2-1, each layer settled into the one below; two loose.
    r = 0.22
    step = 2 * r - 0.02
    for n, layer in ((3, 0), (2, 1), (1, 2)):
        z = r - 0.01 + layer * (step / math.sqrt(2) - 0.02)
        off = (n - 1) * step / 2
        for i in range(n):
            for j in range(n):
                ek.sphere(bm, uv, GUNSTONE, 3.6 - off + i * step, 4.2 - off + j * step, fz + z - r, r,
                          segments=6, rings=4)
    for x in (2.3, 2.75):
        ek.sphere(bm, uv, GUNSTONE, x, 3.2, fz, r, segments=6, rings=4)
    # SE: powder kegs, five standing, one lying across three of them.
    for x, y in ((3.2, -4.8), (3.75, -4.8), (4.3, -4.8), (3.45, -4.25), (4.0, -4.25)):
        keg(bm, uv, x, y, fz)
    keg(bm, uv, 3.75, -4.52, fz + 0.5, lying=True)
    cb._anchor(3.2, -4.8, fz + 0.5)
    cb._anchor(4.3, -4.8, fz + 0.5)
    # SW: the powder cart (bed, supports, axle, wheels, short shafts), a spare wheel, the rammer rack.
    cx, cy = -3.2, -3.8
    cb._box(bm, uv, TIMBER, (cx, cy, fz + 0.8), (1.6, 0.9, 0.1))
    cb._anchor(cx, cy, fz + 0.85)
    for dx in (-0.3, 0.3):
        cb._box(bm, uv, TIMBER, (cx + dx, cy, fz + 0.6), (0.1, 0.9, 0.3))
    mk.paint(bm, mk.add_cylinder(bm, 0.04, 1.2, loc=(cx, cy, fz + 0.45), rot=Euler((math.radians(90), 0, 0)),
                                 segments=6), IRON, uv)
    for dy in (-0.55, 0.55):
        ek.wheel(bm, uv, TIMBER, cx, cy + dy, fz + 0.45, 0.45, along="y")
    for dy in (-0.3, 0.3):
        cb._box(bm, uv, TIMBER, (cx + 1.05, cy + dy, fz + 0.8), (0.7, 0.06, 0.06))
    ek.wheel(bm, uv, TIMBER, -IN + 0.06, -3.4, fz + 0.45, 0.45, along="x")
    for x in (-4.8, -2.6):
        cb._box(bm, uv, IRON, (x, -IN + 0.1, fz + 1.4), (0.08, 0.2, 0.5))
    for z, head in ((1.25, TIMBER), (1.6, LINEN)):
        mk.paint(bm, mk.add_cylinder(bm, 0.035, 3.0, loc=(-3.7, -IN + 0.15, fz + z), rot=along, segments=6),
                 TIMBER, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.12, 0.25, loc=(-5.1, -IN + 0.15, fz + z), rot=along, segments=8),
                 head, uv)
