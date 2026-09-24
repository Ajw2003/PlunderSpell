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


def a_frame(bm, uv, x, y, fz, top, spread=0.8, pigment=TIMBER):
    """Two oak legs splayed north and south, meeting under a beam at `top`."""
    lean = math.atan2(spread, top)
    length = math.hypot(spread, top)
    for s in (-1, 1):
        mk.paint(bm, mk.add_box(bm, (0.12, 0.12, length), loc=(x, y + s * spread / 2, fz + top / 2),
                                rot=Euler((s * lean, 0, 0))), pigment, uv)


def build_late_gun_foundry(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateGunFoundry.svg"""
    h, fz = room_shell(bm, uv, "OuterBailey")
    # NW: the brick furnace, its glowing mouth, the chimney to the wall top, bellows, charcoal.
    cb._box(bm, uv, BRICK, (-4.6, 4.7, fz + 0.7), (1.8, 1.6, 1.4))
    cb._box(bm, uv, CLOTH, (-4.6, 3.89, fz + 0.55), (0.7, 0.02, 0.5))
    cb._box(bm, uv, BRICK, (-4.6, 5.15, fz + 2.5), (0.9, 0.7, 2.2))
    ek.prism(bm, uv, "leather", [(-0.2, 0.0), (0.2, 0.0), (0.2, 0.3)], 0.6, loc=(-3.4, 4.6, fz + 0.45), along="x")
    cb._box(bm, uv, TIMBER, (-3.4, 4.6, fz + 0.225), (0.1, 0.1, 0.45))
    mk.paint(bm, mk.add_cylinder(bm, 0.4, 0.3, loc=(-4.6, 3.3, fz + 0.15), segments=8, radius2=0.1), SOOT, uv)
    # NE: the casting pit's kerb, its dark fill, the upright mould, the A-frame crane and its chain.
    px, py = 3.4, 3.8
    for oy in (-0.7, 0.7):
        cb._box(bm, uv, WALL, (px, py + oy, fz + 0.125), (1.6, 0.2, 0.25))
    for ox in (-0.7, 0.7):
        cb._box(bm, uv, WALL, (px + ox, py, fz + 0.125), (0.2, 1.2, 0.25))
    cb._box(bm, uv, SOOT, (px, py, fz + 0.01), (1.22, 1.22, 0.02))
    mk.paint(bm, mk.add_cylinder(bm, 0.3, 1.8, loc=(px, py, fz + 0.9), segments=10), "leather", uv)
    for k in range(1, 6):
        mk.paint(bm, mk.add_cylinder(bm, 0.315, 0.04, loc=(px, py, fz + k * 0.3), segments=10), IRON, uv)
    for x in (2.4, 4.4):
        a_frame(bm, uv, x, py, fz, 2.8)
    cb._box(bm, uv, TIMBER, (px, py, fz + 2.79), (2.3, 0.18, 0.18))
    mk.paint(bm, mk.add_cylinder(bm, 0.02, 0.9, loc=(px, py, fz + 2.25), segments=4), IRON, uv)
    # SE: two trestles, two gun barrels across them, a tool plank beside them.
    for x in (2.6, 4.4):
        cb._box(bm, uv, TIMBER, (x, -3.8, fz + 0.72), (0.15, 1.2, 0.1))
        for dy in (-0.5, 0.5):
            cb._box(bm, uv, TIMBER, (x, -3.8 + dy, fz + 0.335), (0.1, 0.1, 0.67))
    along = Euler((0, math.radians(90), 0))
    for y, r in ((-4.1, 0.15), (-3.7, 0.18)):
        mk.paint(bm, mk.add_cylinder(bm, r, 2.4, loc=(3.5, y, fz + 0.77 + r), rot=along, segments=10,
                                     radius2=r * 0.8), IRON, uv)
    cb._box(bm, uv, TIMBER, (3.5, -3.3, fz + 0.805), (2.2, 0.3, 0.07))
    cb._anchor(3.5, -3.3, fz + 0.84)
    # SW: the anvil on its stump, the quench tub, a rack of tongs on the west wall.
    mk.paint(bm, mk.add_cylinder(bm, 0.3, 0.6, loc=(-3.4, -3.4, fz + 0.3), segments=10), TIMBER, uv)
    cb._box(bm, uv, IRON, (-3.4, -3.4, fz + 0.725), (0.5, 0.18, 0.25))
    cb._anchor(-3.4, -3.4, fz + 0.85)
    mk.paint(bm, mk.add_cylinder(bm, 0.45, 0.6, loc=(-4.7, -4.5, fz + 0.3), segments=12), TIMBER, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.4, 0.02, loc=(-4.7, -4.5, fz + 0.55), segments=12), "verdigris_lo", uv)
    cb._box(bm, uv, TIMBER, (-IN + 0.05, -2.5, fz + 1.4), (0.1, 1.0, 0.1))
    for k in range(4):
        cb._box(bm, uv, IRON, (-IN + 0.12, -2.85 + k * 0.23, fz + 1.05), (0.03, 0.03, 0.7))


STRAW = "bronze"


def pallet(bm, uv, x, y0, y1, fz, w=0.9, blanket_at_south=True):
    """A straw pallet on a low oak frame, long axis north-south from y0 to y1, a wool
    blanket over one half."""
    cy, l = (y0 + y1) / 2, y1 - y0
    cb._box(bm, uv, TIMBER, (x, cy, fz + 0.1), (w, l, 0.2))
    cb._box(bm, uv, STRAW, (x, cy, fz + 0.26), (w - 0.06, l - 0.06, 0.12))
    by = y0 + 0.5 if blanket_at_south else y1 - 0.5
    cb._box(bm, uv, TAPESTRY, (x, by, fz + 0.35), (w - 0.02, 0.95, 0.06))


def build_late_handgunner_barracks(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateHandgunnerBarracks.svg"""
    h, fz = room_shell(bm, uv, "OuterBailey")
    # Eight pallets, two per quadrant against the east and west walls.
    for sx in (-1, 1):
        for sy in (-1, 1):
            y0, y1 = sorted((sy * 2.6, sy * 4.6))
            for bx in (3.9, 5.0):
                pallet(bm, uv, sx * bx, y0, y1, fz, blanket_at_south=sy > 0)
    # NW: the handgun rack on the north wall; footlockers NW and SW.
    for z in (0.29, 1.55):
        cb._box(bm, uv, TIMBER, (-3.3, IN - 0.05, fz + z), (2.2, 0.1, 0.1))
    for x in (-4.1, -3.6, -3.1, -2.6):
        cb._box(bm, uv, TIMBER, (x, IN - 0.14, fz + 0.475), (0.1, 0.08, 0.95))
        mk.paint(bm, mk.add_cylinder(bm, 0.03, 0.85, loc=(x, IN - 0.14, fz + 1.375), segments=6), IRON, uv)
    for y in (5.05, -5.05):
        iron_chest(bm, uv, -5.0, y, fz, w=0.8, d=0.5, h=0.5)
    # NE: the dice table, stools, dice, cups and a stake of coin.
    cb._table(bm, uv, TIMBER, 2.5, 3.6, fz, 1.0, 0.7, h=0.75)
    for (x, y), r in zip(((2.5, 2.95), (2.5, 4.25)), (0.18, 0.18)):
        mk.paint(bm, mk.add_cylinder(bm, r, 0.45, loc=(x, y, fz + 0.225), segments=8), TIMBER, uv)
    for dy in (-0.15, 0.1):
        mk.paint(bm, mk.add_cylinder(bm, 0.04, 0.1, loc=(2.5, 3.6 + dy + 0.2, fz + 0.8), segments=6), STEEL, uv)
    for dx in (-0.1, 0.05):
        cb._box(bm, uv, LINEN, (2.5 + dx, 3.3, fz + 0.77), (0.04, 0.04, 0.04))
    mk.paint(bm, mk.add_cylinder(bm, 0.04, 0.03, loc=(2.35, 3.45, fz + 0.765), segments=6), GOLD, uv)
    # SE: three pavises propped against the south wall.
    for x in (2.4, 3.1, 3.8):
        cb._box(bm, uv, TIMBER, (x, -IN + 0.05, fz + 0.65), (0.6, 0.08, 1.3))
        cb._box(bm, uv, CLOTH, (x, -IN + 0.1, fz + 0.7), (0.5, 0.02, 1.1))


COPPER = "bronze"


def cask_on_cradle(bm, uv, x, y, fz, r=0.4, length=1.0, cradle=0.2, along_x=False):
    """An ale cask lying on its side on two oak cradle blocks, two iron hoops; registers
    nothing (the room decides which casks carry loot)."""
    rot = Euler((0, math.radians(90), 0)) if along_x else Euler((math.radians(90), 0, 0))
    cz = fz + cradle + r - 0.05
    for o in (-1, 1):
        off = o * (length / 2 - 0.15)
        loc = (x + off, y, fz + cradle / 2) if along_x else (x, y + off, fz + cradle / 2)
        size = (0.12, 2 * r - 0.1, cradle) if along_x else (2 * r - 0.1, 0.12, cradle)
        cb._box(bm, uv, TIMBER, loc, size)
    mk.paint(bm, mk.add_cylinder(bm, r, length, loc=(x, y, cz), rot=rot, segments=12), TIMBER, uv)
    for f in (-0.3, 0.3):
        loc = (x + f * length, y, cz) if along_x else (x, y + f * length, cz)
        mk.paint(bm, mk.add_cylinder(bm, r + 0.015, 0.05, loc=loc, rot=rot, segments=12), IRON, uv)
    return cz + r


def build_late_brewhouse(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateBrewhouse.svg"""
    h, fz = room_shell(bm, uv, "OuterBailey")
    # NW: the brick furnace, its fire mouth, the copper kettle set into its top, the flue.
    fx, fy = -4.5, 4.5
    cb._box(bm, uv, BRICK, (fx, fy, fz + 0.4), (1.6, 1.6, 0.8))
    cb._box(bm, uv, CLOTH, (fx, fy - 0.81, fz + 0.275), (0.6, 0.02, 0.35))
    mk.paint(bm, mk.add_cylinder(bm, 0.6, 0.6, loc=(fx, fy, fz + 0.9), segments=12, radius2=0.54), COPPER, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.64, 0.05, loc=(fx, fy, fz + 1.225), segments=12), COPPER, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.52, 0.02, loc=(fx, fy, fz + 1.19), segments=12), "leather", uv)
    cb._box(bm, uv, BRICK, (fx - 0.4, fy + 0.5, fz + 1.6), (0.6, 0.5, 1.6))
    # NE: the mash tun with its mash and paddle; the cooling trough on legs along the east wall.
    ux, uy = 3.6, 4.1
    mk.paint(bm, mk.add_cylinder(bm, 0.8, 0.9, loc=(ux, uy, fz + 0.45), segments=14), TIMBER, uv)
    for z in (0.15, 0.75):
        mk.paint(bm, mk.add_cylinder(bm, 0.815, 0.06, loc=(ux, uy, fz + z), segments=14), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.74, 0.02, loc=(ux, uy, fz + 0.91), segments=14), "leather", uv)
    mk.paint(bm, mk.add_box(bm, (0.06, 0.06, 1.3), loc=(ux + 0.42, uy, fz + 1.1), rot=Euler((0, math.radians(15), 0))),
             TIMBER, uv)
    tx, ty = 5.05, 2.9
    cb._box(bm, uv, TIMBER, (tx, ty, fz + 0.775), (0.7, 2.0, 0.35))
    cb._box(bm, uv, "leather", (tx, ty, fz + 0.955), (0.6, 1.9, 0.02))
    for dy in (-0.85, 0.85):
        for dx in (-0.28, 0.28):
            cb._box(bm, uv, TIMBER, (tx + dx, ty + dy, fz + 0.3), (0.06, 0.06, 0.6))
    # South: ale casks on cradles, three SE and two SW; malt sacks in the SW corner.
    for x in (2.6, 3.6, 4.6, -2.6, -3.6):
        top = cask_on_cradle(bm, uv, x, -4.7, fz)
    for x in (3.6, 4.6, -3.6):
        cb._anchor(x, -4.7, top)
    for (x, y, z), r in zip(((-4.9, -4.9, 0.0), (-4.9, -4.3, 0.0), (-4.9, -4.6, 0.36)), (0.28, 0.27, 0.26)):
        mk.paint(bm, mk.add_sphere(bm, r, loc=(x, y, fz + z + r * 0.65), segments=6, rings=4, scale=(1.0, 1.0, 0.65)),
                 LINEN, uv)
