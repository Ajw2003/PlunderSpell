"""
Late Medieval Keep pieces: LateGreatHall, LateJewelHouse, LateStateBedchamber, LateTapestrySolar, LateTurretStair.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/LateMedieval/<Key>.json,
drawing: docs/art/rooms/concept/LateMedieval/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_late.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_late import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


def _candle_stand(bm, uv, x, y, fz, h=1.5):
    """A standing iron pricket: foot, stem, drip pan, a candle and its flame."""
    mk.paint(bm, mk.add_cylinder(bm, 0.2, 0.05, loc=(x, y, fz + 0.025), segments=8), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.03, h, loc=(x, y, fz + h / 2), segments=6), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.12, 0.03, loc=(x, y, fz + h + 0.015), segments=8), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.035, 0.18, loc=(x, y, fz + h + 0.12), segments=6), LINEN, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.025, 0.08, loc=(x, y, fz + h + 0.25), segments=6, radius2=0.005), CLOTH, uv)


def _wall_hearth(bm, uv, side_x, y, fz, width=1.3, depth=0.9, mouth=1.3, hood_top=4.1):
    """A hooded hearth against the west wall (side_x = -IN) centred at y: jambs, a
    mantel, a sandstone hood tapering back to the wall, a sooted back, a fire."""
    x_face = side_x + depth
    for o in (-1, 1):
        cb._box(bm, uv, WALL, ((side_x + x_face) / 2, y + o * (width / 2 - 0.09), fz + mouth / 2), (depth, 0.18, mouth))
    cb._box(bm, uv, WALL, ((side_x + x_face + 0.1) / 2, y, fz + mouth + 0.125), (depth + 0.1, width + 0.2, 0.25))
    ek.prism(bm, uv, WALL, [(side_x, mouth + 0.25), (x_face + 0.1, mouth + 0.25), (side_x + 0.3, hood_top),
                            (side_x, hood_top)], width, loc=(0, y, fz), along="y")
    ek.wall_panel(bm, uv, SOOT, "west", y, fz, width - 0.36, mouth, depth=0.04)
    for o in (-1, 1):
        cb._box(bm, uv, IRON, (side_x + 0.45, y + o * 0.25, fz + 0.12), (0.5, 0.06, 0.24))
    mk.paint(bm, mk.add_cylinder(bm, 0.25, 0.55, loc=(side_x + 0.4, y, fz + 0.275), segments=6, radius2=0.03), CLOTH, uv)


def build_late_great_hall(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateGreatHall.svg"""
    h, fz = room_shell(bm, uv, "Keep")
    d = fz + 0.35
    # The two daises, and the dorsal tapestry in halves either side of the north archway.
    for sgn in (-1, 1):
        x0, x1 = sorted((sgn * 1.8, sgn * IN))
        cb._box(bm, uv, TIMBER, ((x0 + x1) / 2, (3.3 + IN) / 2, fz + 0.175), (x1 - x0, IN - 3.3, 0.35))
        ek.wall_panel(bm, uv, TAPESTRY, "north", sgn * 3.4, fz + 1.5, 3.9, 2.55, depth=0.04)
    # NE: the high table under linen, the lord's chair, the cloth of estate and its canopy.
    cb._table(bm, uv, TIMBER, 3.6, 4.2, d, 3.0, 0.8, h=0.75)
    cb._box(bm, uv, LINEN, (3.6, 4.2, d + 0.76), (3.06, 0.86, 0.02))
    for dx, r in ((-1.0, 0.05), (-0.3, 0.055), (0.9, 0.045)):
        mk.paint(bm, mk.add_cylinder(bm, r, 0.14, loc=(3.6 + dx, 4.2, d + 0.84), segments=6, radius2=r * 0.8), STEEL, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.1, 0.12, loc=(3.9, 4.2, d + 0.83), segments=8, radius2=0.06), GOLD, uv)
    cb._box(bm, uv, TIMBER, (3.6, 5.05, d + 0.225), (0.6, 0.5, 0.45))
    cb._box(bm, uv, TIMBER, (3.6, 5.3, d + 0.8), (0.6, 0.1, 1.6))
    ek.wall_panel(bm, uv, CLOTH, "north", 3.6, d + 0.9, 1.4, fz + 3.7 - (d + 0.9), depth=0.04, proud=0.04)
    cb._box(bm, uv, CLOTH, (3.6, IN - 0.45, fz + 3.76), (1.6, 0.9, 0.12))
    cb._box(bm, uv, GOLD, (3.6, IN - 0.9, fz + 3.66), (1.6, 0.03, 0.08))
    # NW: the side table with the gilded nef.
    cb._table(bm, uv, TIMBER, -3.6, 4.5, d, 1.2, 0.6, h=0.8)
    top = d + 0.8
    ek.prism(bm, uv, GOLD, [(-0.1, 0.0), (0.1, 0.0), (0.14, 0.13), (-0.14, 0.13)], 0.6, loc=(-3.6, 4.5, top), along="x")
    mk.paint(bm, mk.add_cylinder(bm, 0.015, 0.4, loc=(-3.6, 4.5, top + 0.33), segments=4), GOLD, uv)
    cb._box(bm, uv, LINEN, (-3.5, 4.5, top + 0.4), (0.18, 0.02, 0.2))
    # West: the hooded hearth south of the NW dais. East: two window embrasures.
    _wall_hearth(bm, uv, -IN, 2.5, fz)
    for y in (3.6, -3.6):
        ek.framed_panel(bm, uv, "vellum_faint", "line", "east", y, fz + 1.6, 1.4, 1.6)
    # South: two benches and a standing candle stand.
    cb._bench(bm, uv, 3.4, -3.0, fz, 2.6, 0.35, True, TIMBER)
    cb._bench(bm, uv, -3.2, -3.4, fz, 2.6, 0.35, True, TIMBER)
    _candle_stand(bm, uv, 4.8, -4.8, fz)


def _iron_chest(bm, uv, x, y, fz, w=1.2, d=0.7, h=0.7, along_x=True):
    """An oak chest bound in blackened iron: lid band, three straps over it, a lock
    plate; registers its loot anchor on the lid."""
    sx, sy = (w, d) if along_x else (d, w)
    cb._chest(bm, uv, x, y, fz, w=sx, d=sy, h=h, trim=IRON, body=TIMBER)
    for o in (-0.375, 0.0, 0.375):
        if along_x:
            cb._box(bm, uv, IRON, (x + o * w, y, fz + h / 2 + 0.01), (0.06, d + 0.02, h + 0.02))
        else:
            cb._box(bm, uv, IRON, (x, y + o * w, fz + h / 2 + 0.01), (d + 0.02, 0.06, h + 0.02))


def build_late_jewel_house(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateJewelHouse.svg"""
    h, fz = room_shell(bm, uv, "Keep")
    # NE: the stepped buffet, each tier standing on the one below against the north wall.
    z = fz
    for dep, th in ((0.9, 0.9), (0.6, 0.45), (0.3, 0.45)):
        cb._box(bm, uv, TIMBER, (3.6, IN - dep / 2, z + th / 2), (2.4, dep, th))
        z += th
    cb._anchor(3.6, IN - 0.75, fz + 0.9)
    cb._anchor(3.6, IN - 0.45, fz + 1.35)
    ek.wall_panel(bm, uv, TAPESTRY, "north", 3.6, fz + 1.8, 2.6, 1.6, depth=0.04, proud=0.02)
    for dx in (-0.8, -0.2, 0.5):
        mk.paint(bm, mk.add_cylinder(bm, 0.05, 0.16, loc=(3.6 + dx, IN - 0.75, fz + 0.98), segments=6, radius2=0.035),
                 GOLD, uv)
    for dx in (-0.6, 0.6):
        mk.paint(bm, mk.add_cylinder(bm, 0.15, 0.02, loc=(3.6 + dx, IN - 0.4, fz + 1.5), rot=Euler((math.radians(90), 0, 0)),
                                     segments=10), GOLD, uv)
    ek.jar(bm, uv, GOLD, 3.6, IN - 0.15, fz + 1.8, 0.34, 0.2, mouth=0.08, segments=8)
    for dx in (-0.7, 0.7):
        mk.paint(bm, mk.add_cylinder(bm, 0.05, 0.14, loc=(3.6 + dx, IN - 0.15, fz + 1.87), segments=6, radius2=0.035),
                 GOLD, uv)
    _candle_stand(bm, uv, 1.95, 4.0, fz)
    # NW: the strong cupboard behind a lattice of iron straps, a lock plate on it.
    cb._box(bm, uv, TIMBER, (-3.6, IN - 0.3, fz + 1.1), (1.6, 0.6, 2.2))
    face = IN - 0.61
    for dx in (-0.6, -0.2, 0.2, 0.6):
        cb._box(bm, uv, IRON, (-3.6 + dx, face, fz + 1.1), (0.04, 0.02, 2.0))
    for dz in (0.4, 0.9, 1.4, 1.9):
        cb._box(bm, uv, IRON, (-3.6, face - 0.02, fz + dz), (1.5, 0.02, 0.04))
    cb._box(bm, uv, STEEL, (-3.475, face - 0.04, fz + 1.15), (0.15, 0.02, 0.2))
    # South: brick-lined walls with an iron-bound chest before each; the jeweller's table
    # and its casket SE, coin sacks SW.
    for sgn in (-1, 1):
        ek.wall_panel(bm, uv, BRICK, "south", sgn * 3.6, fz, 3.4, 2.4, depth=0.04)
        _iron_chest(bm, uv, sgn * 3.4, -5.0, fz)
    cb._table(bm, uv, TIMBER, 4.4, -2.9, fz, 0.9, 0.6, h=0.8)
    cb._box(bm, uv, TIMBER, (4.65, -2.9, fz + 0.9), (0.3, 0.2, 0.2))
    cb._box(bm, uv, GOLD, (4.65, -2.9, fz + 1.015), (0.32, 0.22, 0.03))
    for (x, y), r in zip(((-4.8, -3.6), (-4.4, -3.2), (-5.0, -3.1)), (0.2, 0.18, 0.16)):
        mk.paint(bm, mk.add_cylinder(bm, r, r * 2.2, loc=(x, y, fz + r * 1.1), segments=6, radius2=r * 0.55),
                 "leather", uv)


def build_late_state_bedchamber(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateStateBedchamber.svg"""
    h, fz = room_shell(bm, uv, "Keep")
    # NW: the tester bed, head to the north wall.
    bx, by, bw, bl = -3.8, 4.3, 1.8, 2.2
    x0, x1, y0, y1 = bx - bw / 2, bx + bw / 2, by - bl / 2, by + bl / 2
    post_h = 2.75
    cb._box(bm, uv, TIMBER, (bx, by, fz + 0.225), (bw, bl, 0.45))
    cb._box(bm, uv, CLOTH, (bx, by - 0.1, fz + 0.5), (bw + 0.04, bl - 0.2, 0.1))
    cb._box(bm, uv, LINEN, (bx, 5.0, fz + 0.61), (1.5, 0.35, 0.12))
    cb._anchor(bx, 4.1, fz + 0.55)
    for px in (x0 + 0.05, x1 - 0.05):
        for py in (y0 + 0.05, y1 - 0.05):
            cb._box(bm, uv, TIMBER, (px, py, fz + post_h / 2), (0.12, 0.12, post_h))
    cb._box(bm, uv, CLOTH, (bx, by, fz + post_h + 0.075), (bw + 0.1, bl + 0.1, 0.15))
    ek.wall_panel(bm, uv, CLOTH, "north", bx, fz + 0.45, bw, post_h - 0.45, depth=0.04)
    for py in (3.55, 5.05):
        cb._box(bm, uv, CLOTH, (x1 + 0.05, py, fz + 0.5 + (post_h - 0.5) / 2), (0.06, 0.6, post_h - 0.5))
    _iron_chest(bm, uv, -3.8, 2.8, fz, w=1.2, d=0.5, h=0.55)
    # NE: a verdure tapestry and the prie-dieu before it.
    ek.wall_panel(bm, uv, TAPESTRY, "north", 3.65, fz + 0.9, 3.3, 2.7, depth=0.04)
    cb._box(bm, uv, TIMBER, (3.6, 4.95, fz + 0.425), (0.6, 0.35, 0.85))
    ek.prism(bm, uv, TIMBER, [(-0.2, 0.0), (0.2, 0.0), (0.2, 0.12)], 0.66, loc=(3.6, 4.95, fz + 0.85), along="x")
    cb._box(bm, uv, CLOTH, (3.6, 4.5, fz + 0.075), (0.6, 0.3, 0.15))
    # SE: the table with ewer and basin, the close-stool in the corner.
    cb._table(bm, uv, TIMBER, 3.4, -3.4, fz, 1.0, 0.7, h=0.75)
    mk.paint(bm, mk.add_cylinder(bm, 0.18, 0.08, loc=(3.6, -3.4, fz + 0.79), segments=10, radius2=0.12), STEEL, uv)
    ek.jar(bm, uv, STEEL, 3.1, -3.4, fz + 0.75, 0.3, 0.16, mouth=0.07, segments=8)
    cb._box(bm, uv, TIMBER, (4.9, -4.9, fz + 0.225), (0.5, 0.45, 0.45))
    cb._box(bm, uv, CLOTH, (4.9, -4.9, fz + 0.47), (0.52, 0.47, 0.04))
    # SW: a small hooded hearth; east: a window.
    _wall_hearth(bm, uv, -IN, -3.2, fz, width=1.3, depth=0.7, hood_top=3.6)
    ek.framed_panel(bm, uv, "vellum_faint", "line", "east", 3.0, fz + 1.4, 1.4, 1.6)
