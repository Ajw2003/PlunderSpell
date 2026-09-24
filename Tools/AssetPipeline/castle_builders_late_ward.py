"""
Late Medieval InnerWard pieces: LateCountingHouse, LateArmouryHall, LateSpitKitchen, LateChantryChapel, LateLibrary.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/LateMedieval/<Key>.json,
drawing: docs/art/rooms/concept/LateMedieval/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_late.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_late import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


def _coin_sacks(bm, uv, spots, fz):
    for (x, y), r in zip(spots, (0.2, 0.18, 0.16, 0.19)):
        mk.paint(bm, mk.add_cylinder(bm, r, r * 2.2, loc=(x, y, fz + r * 1.1), segments=6, radius2=r * 0.55), BRICK, uv)


def build_late_counting_house(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateCountingHouse.svg"""
    h, fz = room_shell(bm, uv, "InnerWard")
    # NW: the brick strong room in the corner, its doorway, vault cap and open lattice door.
    t, wh = 0.3, 2.6
    for x0, x1 in ((-IN, -4.3), (-3.4, -2.2)):
        cb._box(bm, uv, BRICK, ((x0 + x1) / 2, 2.2 + t / 2, fz + wh / 2), (x1 - x0, t, wh))
    cb._box(bm, uv, BRICK, (-3.85, 2.2 + t / 2, fz + 2.3), (0.9, t, 0.6))
    cb._box(bm, uv, BRICK, (-2.2 - t / 2, (2.5 + IN) / 2, fz + wh / 2), (t, IN - 2.5, wh))
    half = (IN - 2.2) / 2
    arc = [(half * (1 - k / 4), 0.6 * (1 - (1 - k / 4) ** 2)) for k in range(1, 8)]
    ek.prism(bm, uv, BRICK, [(-half, 0.0), (half, 0.0)] + arc, IN - 2.2, loc=(-3.85, (2.2 + IN) / 2, fz + wh),
             along="y")
    for k in range(5):
        cb._box(bm, uv, IRON, (-3.4 + k * 0.225, 2.18, fz + 1.0), (0.04, 0.03, 2.0))
    for k in range(6):
        cb._box(bm, uv, IRON, (-2.95, 2.15, fz + 0.02 + k * 0.39), (0.9, 0.03, 0.04))
    for x in (-4.55, -3.3):
        iron_chest(bm, uv, x, 5.1, fz, w=1.0, d=0.6, h=0.6)
    # NE: the ledger case: back, sides, top, four shelves, ledgers on them.
    cb._box(bm, uv, TIMBER, (3.6, IN - 0.02, fz + 1.17), (2.28, 0.04, 2.34))
    for x in (2.43, 4.77):
        cb._box(bm, uv, TIMBER, (x, IN - 0.225, fz + 1.17), (0.06, 0.45, 2.34))
    cb._box(bm, uv, TIMBER, (3.6, IN - 0.235, fz + 2.37), (2.52, 0.47, 0.06))
    for k, z in enumerate((0.1, 0.66, 1.21, 1.79)):
        cb._box(bm, uv, TIMBER, (3.6, IN - 0.225, fz + z), (2.34, 0.41, 0.04))
        for j, x in enumerate((2.62, 2.8, 3.0, 4.2, 4.45)):
            bh = 0.34 + ((j + k) % 3) * 0.04
            cb._box(bm, uv, (CLOTH, TIMBER, TAPESTRY, LINEN)[(j + k) % 4], (x, IN - 0.25, fz + z + 0.02 + bh / 2),
                    (0.1, 0.3, bh))
    cb._anchor(3.6, IN - 0.25, fz + 1.23)
    # SE: the counting table under its chequered cloth, balance, coin, ledger, candle; a bench; the barred window.
    cb._table(bm, uv, TIMBER, 3.6, -3.4, fz, 2.0, 1.0, h=0.8)
    cb._box(bm, uv, TAPESTRY, (3.6, -3.4, fz + 0.81), (2.06, 1.06, 0.02))
    for i in range(8):
        for j in range(4):
            if (i + j) % 2 == 0:
                cb._box(bm, uv, "line", (2.725 + i * 0.25, -3.775 + j * 0.25, fz + 0.826), (0.24, 0.24, 0.006))
    top = fz + 0.826
    mk.paint(bm, mk.add_cylinder(bm, 0.08, 0.03, loc=(3.1, -3.25, top + 0.015), segments=8), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.015, 0.45, loc=(3.1, -3.25, top + 0.25), segments=4), IRON, uv)
    cb._box(bm, uv, IRON, (3.1, -3.25, top + 0.47), (0.45, 0.02, 0.02))
    for dx in (-0.2, 0.2):
        mk.paint(bm, mk.add_cylinder(bm, 0.006, 0.34, loc=(3.1 + dx, -3.25, top + 0.29), segments=4), IRON, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.07, 0.01, loc=(3.1 + dx, -3.25, top + 0.115), segments=8), STEEL, uv)
    for (x, y), hh in zip(((2.95, -3.6), (3.1, -3.65), (3.25, -3.6)), (0.06, 0.09, 0.05)):
        mk.paint(bm, mk.add_cylinder(bm, 0.03, hh, loc=(x, y, top + hh / 2), segments=6), GOLD, uv)
    cb._box(bm, uv, LINEN, (4.1, -3.4, top + 0.015), (0.4, 0.3, 0.03))
    mk.paint(bm, mk.add_cylinder(bm, 0.05, 0.03, loc=(4.4, -3.15, top + 0.015), segments=6), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.02, 0.16, loc=(4.4, -3.15, top + 0.11), segments=6), LINEN, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.015, 0.05, loc=(4.4, -3.15, top + 0.215), segments=4, radius2=0.003), CLOTH, uv)
    cb._bench(bm, uv, 3.6, -4.25, fz, 2.0, 0.35, True, TIMBER)
    ek.framed_panel(bm, uv, "vellum_faint", "line", "east", -3.4, fz + 1.3, 1.2, 1.3)
    for k in range(4):
        cb._box(bm, uv, IRON, (IN - 0.13, -3.775 + k * 0.25, fz + 1.95), (0.03, 0.03, 1.1))
    # SW: the floor strongbox's flagstone lid and iron ring, coin sacks.
    cb._box(bm, uv, "vellum_faint", (-3.6, -3.6, fz + 0.02), (0.8, 0.6, 0.04))
    mk.paint(bm, mk.add_cylinder(bm, 0.08, 0.02, loc=(-3.6, -3.6, fz + 0.05), segments=8), IRON, uv)
    _coin_sacks(bm, uv, ((-4.8, -4.6), (-4.4, -4.9), (-5.0, -4.1)), fz)
