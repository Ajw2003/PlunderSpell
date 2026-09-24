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


def _harness(bm, uv, x, y, fz):
    """A harness of plate on its stand: base, legs, faulds, breastplate, pauldrons,
    arms and a sallet, each part touching the next."""
    cb._box(bm, uv, TIMBER, (x, y, fz + 0.03), (0.6, 0.45, 0.06))
    for dx in (-0.1, 0.1):
        mk.paint(bm, mk.add_cylinder(bm, 0.07, 0.86, loc=(x + dx, y, fz + 0.49), segments=6), STEEL, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.2, 0.18, loc=(x, y, fz + 1.01), segments=8), STEEL, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.21, 0.52, loc=(x, y, fz + 1.36), segments=8, radius2=0.18), STEEL, uv)
    for s in (-1, 1):
        mk.paint(bm, mk.add_sphere(bm, 0.1, loc=(x + s * 0.24, y, fz + 1.53), segments=6, rings=3), STEEL, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.05, 0.47, loc=(x + s * 0.28, y, fz + 1.285), segments=6), STEEL, uv)
    mk.paint(bm, mk.add_sphere(bm, 0.13, loc=(x, y, fz + 1.75), segments=6, rings=4), STEEL, uv)
    cb._box(bm, uv, "line", (x, y - 0.12, fz + 1.77), (0.16, 0.04, 0.03))


def build_late_armoury_hall(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateArmouryHall.svg"""
    h, fz = room_shell(bm, uv, "InnerWard")
    # North: a madder banner over each pair of harnesses, the harnesses on their stands.
    for x in (-3.8, 3.8):
        ek.wall_panel(bm, uv, CLOTH, "north", x, fz + 2.1, 1.0, 1.3, depth=0.03)
        ek.wall_panel(bm, uv, LINEN, "north", x, fz + 2.1, 0.15, 1.3, depth=0.02, proud=0.03)
        cb._box(bm, uv, IRON, (x, IN - 0.04, fz + 3.42), (1.1, 0.05, 0.04))
    for x in (-4.6, -3.0, 3.0, 4.6):
        _harness(bm, uv, x, 4.8, fz)
    # East and west walls: halberd and poleaxe racks, two rails and five hafts each.
    for sgn in (-1, 1):
        for z in (0.35, 1.5):
            cb._box(bm, uv, TIMBER, (sgn * (IN - 0.08), 3.1, fz + z), (0.16, 2.0, 0.1))
        for y in (2.3, 2.7, 3.1, 3.5, 3.9):
            hx = sgn * (IN - 0.1)
            mk.paint(bm, mk.add_cylinder(bm, 0.025, 2.3, loc=(hx, y, fz + 1.15), segments=6), TIMBER, uv)
            cb._box(bm, uv, STEEL, (hx - sgn * 0.1, y, fz + 2.45), (0.2, 0.03, 0.25))
            mk.paint(bm, mk.add_cylinder(bm, 0.012, 0.22, loc=(hx, y, fz + 2.66), segments=4, radius2=0.002), STEEL, uv)
    # SE: the grinding wheel on its uprights, dipping into its trough.
    wx, wy = 3.8, -3.8
    cb._box(bm, uv, TIMBER, (wx, wy, fz + 0.1), (1.0, 0.3, 0.2))
    cb._box(bm, uv, "verdigris_lo", (wx, wy, fz + 0.19), (0.9, 0.22, 0.02))
    for dy in (-0.2, 0.2):
        cb._box(bm, uv, TIMBER, (wx, wy + dy, fz + 0.375), (0.08, 0.08, 0.75))
    mk.paint(bm, mk.add_cylinder(bm, 0.45, 0.12, loc=(wx, wy, fz + 0.6), rot=Euler((math.radians(90), 0, 0)),
                                 segments=12), "vellum_faint", uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.03, 0.5, loc=(wx, wy, fz + 0.6), rot=Euler((math.radians(90), 0, 0)),
                                 segments=6), IRON, uv)
    cb._box(bm, uv, IRON, (wx, wy - 0.26, fz + 0.5), (0.02, 0.02, 0.2))
    # SW: the armourer's table with a sallet, a hammer and gauntlets; the plate chest against the south wall.
    cb._table(bm, uv, TIMBER, -3.6, -3.6, fz, 1.8, 0.8, h=0.85)
    top = fz + 0.85
    mk.paint(bm, mk.add_sphere(bm, 0.13, loc=(-4.1, -3.6, top + 0.13), segments=8, rings=5), STEEL, uv)
    cb._box(bm, uv, TIMBER, (-3.1, -3.5, top + 0.015), (0.3, 0.03, 0.03))
    cb._box(bm, uv, IRON, (-2.96, -3.5, top + 0.03), (0.04, 0.1, 0.06))
    for dx in (-0.25, -0.05):
        cb._box(bm, uv, STEEL, (-3.3 + dx, -3.8, top + 0.03), (0.12, 0.2, 0.06))
    iron_chest(bm, uv, -3.4, -5.0, fz, w=1.2, d=0.6, h=0.6)


def _hooped_barrel(bm, uv, x, y, fz, r=0.4, h=0.9):
    """An upright oak barrel with two iron hoops."""
    mk.paint(bm, mk.add_cylinder(bm, r, h, loc=(x, y, fz + h / 2), segments=10), TIMBER, uv)
    for f in (0.2, 0.8):
        mk.paint(bm, mk.add_cylinder(bm, r + 0.015, 0.05, loc=(x, y, fz + h * f), segments=10), IRON, uv)


def build_late_spit_kitchen(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateSpitKitchen.svg"""
    h, fz = room_shell(bm, uv, "InnerWard")
    # NW: the great hearth, a long spit on two tall firedogs across its mouth, a boar on the spit.
    hy = 3.6
    wall_hearth(bm, uv, -IN, hy, fz, width=2.4, depth=1.0, mouth=1.5, hood_top=3.5)
    sx, sz = -IN + 0.5, 0.7
    for dy in (-0.9, 0.9):
        cb._box(bm, uv, IRON, (sx, hy + dy, fz + (sz + 0.08) / 2), (0.06, 0.06, sz + 0.08))
    mk.paint(bm, mk.add_cylinder(bm, 0.02, 2.1, loc=(sx, hy, fz + sz), rot=Euler((math.radians(90), 0, 0)),
                                 segments=4), IRON, uv)
    mk.paint(bm, mk.add_sphere(bm, 0.2, loc=(sx, hy, fz + sz), segments=8, rings=5, scale=(1.0, 2.2, 1.0)),
             "leather", uv)
    # NE: the dresser: cupboard base, back board, two shelves of standing pewter plates, jugs on the base.
    dx = 3.6
    cb._box(bm, uv, TIMBER, (dx, IN - 0.25, fz + 0.45), (2.0, 0.5, 0.9))
    cb._anchor(dx, IN - 0.35, fz + 0.9)
    cb._box(bm, uv, TIMBER, (dx, IN - 0.03, fz + 1.5), (2.0, 0.06, 1.2))
    for z in (1.4, 1.8):
        cb._box(bm, uv, TIMBER, (dx, IN - 0.16, fz + z - 0.015), (2.0, 0.26, 0.03))
        for k in range(5):
            mk.paint(bm, mk.add_cylinder(bm, 0.14, 0.02, loc=(dx - 0.8 + k * 0.4, IN - 0.12, fz + z + 0.14),
                                         rot=Euler((math.radians(90), 0, 0)), segments=10), STEEL, uv)
    for k in range(3):
        ek.jar(bm, uv, STEEL, dx - 0.6 + k * 0.6, IN - 0.2, fz + 0.9, 0.26, 0.16, mouth=0.08, segments=8)
    # SE: the chopping block and the work table with a cleaver and loaves.
    mk.paint(bm, mk.add_cylinder(bm, 0.35, 0.8, loc=(4.6, -4.3, fz + 0.4), segments=10), TIMBER, uv)
    cb._box(bm, uv, IRON, (4.6, -4.3, fz + 0.85), (0.08, 0.3, 0.1))
    cb._table(bm, uv, TIMBER, 3.0, -3.6, fz, 1.8, 0.8, h=0.85)
    for dx2 in (-0.6, -0.35):
        mk.paint(bm, mk.add_sphere(bm, 0.1, loc=(3.0 + dx2, -3.6, fz + 0.85 + 0.07), segments=6, rings=4,
                                   scale=(1.3, 1.0, 0.7)), "bronze", uv)
    cb._box(bm, uv, IRON, (3.5, -3.7, fz + 0.86), (0.3, 0.12, 0.02))
    # SW: the flour bin against the west wall and two hooped barrels.
    cb._box(bm, uv, TIMBER, (-5.1, -4.0, fz + 0.4), (0.7, 1.2, 0.8))
    ek.prism(bm, uv, TIMBER, [(-0.35, 0.0), (0.35, 0.0), (-0.35, 0.12)], 1.2, loc=(-5.1, -4.0, fz + 0.8), along="y")
    for x, y in ((-3.3, -4.9), (-2.4, -4.95)):
        _hooped_barrel(bm, uv, x, y, fz)


ALABASTER = "vellum"


def _screen_run(bm, uv, x0, y0, x1, y1, fz, dado=0.9, top=2.2, pitch=0.3):
    """A straight run of parclose screen: a solid oak dado, open mullions above it at
    `pitch`, and a top rail. Axis-aligned; the run's ends are posts."""
    along_x = abs(x1 - x0) >= abs(y1 - y0)
    length = abs(x1 - x0) if along_x else abs(y1 - y0)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    size = (lambda l, t, h: (l, t, h)) if along_x else (lambda l, t, h: (t, l, h))
    cb._box(bm, uv, TIMBER, (cx, cy, fz + dado / 2), size(length, 0.06, dado))
    cb._box(bm, uv, TIMBER, (cx, cy, fz + top - 0.04), size(length + 0.1, 0.1, 0.08))
    n = int(round(length / pitch))
    for k in range(n + 1):
        t = k / n
        mx, my = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        w = 0.1 if k in (0, n) else 0.04
        bottom = 0.0 if k in (0, n) else dado
        cb._box(bm, uv, TIMBER, (mx, my, fz + (bottom + top - 0.08) / 2), (w, w, top - 0.08 - bottom))


def build_late_chantry_chapel(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateChantryChapel.svg"""
    h, fz = room_shell(bm, uv, "InnerWard")
    # NE: the altar, its frontal and cloth, the triptych standing open, two candlesticks.
    ax, ay = 3.6, 5.1
    cb._box(bm, uv, WALL, (ax, ay, fz + 0.5), (2.0, 0.8, 1.0))
    cb._box(bm, uv, CLOTH, (ax, ay - 0.41, fz + 0.49), (1.8, 0.02, 0.78))
    cb._box(bm, uv, LINEN, (ax, ay, fz + 0.99), (2.06, 0.86, 0.02))
    cb._anchor(ax, ay - 0.2, fz + 1.0)
    t0 = fz + 1.0
    for dx, w, th in ((-0.675, 0.45, 0.88), (0.0, 0.9, 1.1), (0.675, 0.45, 0.88)):
        cb._box(bm, uv, GOLD, (ax + dx, IN - 0.08, t0 + th / 2 + 0.01), (w, 0.06, th))
        cb._box(bm, uv, TAPESTRY, (ax + dx, IN - 0.12, t0 + th / 2 + 0.01), (w - 0.1, 0.02, th - 0.1))
    cb._box(bm, uv, CLOTH, (ax, IN - 0.14, t0 + 0.36), (0.2, 0.02, 0.52))
    for dx in (-0.85, 0.85):
        mk.paint(bm, mk.add_cylinder(bm, 0.06, 0.04, loc=(ax + dx, ay - 0.1, t0 + 0.04), segments=8), GOLD, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.02, 0.31, loc=(ax + dx, ay - 0.1, t0 + 0.215), segments=6), GOLD, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.025, 0.2, loc=(ax + dx, ay - 0.1, t0 + 0.47), segments=6), LINEN, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.018, 0.06, loc=(ax + dx, ay - 0.1, t0 + 0.6), segments=4, radius2=0.004),
                 CLOTH, uv)
    # NW: the founder's tomb chest, his effigy on its lid.
    mx, my, mh = -3.6, 4.6, 0.8
    cb._box(bm, uv, WALL, (mx, my, fz + mh / 2), (2.1, 0.9, mh))
    top = fz + mh
    cb._box(bm, uv, ALABASTER, (mx - 0.05, my, top + 0.1), (1.6, 0.45, 0.2))
    cb._box(bm, uv, ALABASTER, (mx + 0.85, my, top + 0.06), (0.2, 0.4, 0.12))
    mk.paint(bm, mk.add_sphere(bm, 0.11, loc=(mx + 0.85, my, top + 0.23), segments=6, rings=4), ALABASTER, uv)
    ek.prism(bm, uv, ALABASTER, [(-0.1, 0.0), (0.1, 0.0), (0.0, 0.16)], 0.12, loc=(mx - 0.1, my, top + 0.2), along="y")
    mk.paint(bm, mk.add_sphere(bm, 0.12, loc=(mx - 0.95, my, top + 0.1), segments=6, rings=4, scale=(1.0, 1.3, 0.8)),
             "vellum_faint", uv)
    cb._anchor(mx + 0.85, my, top)
    # SE: the parclose screen closing the corner, its entrance facing the crossing; a lectern inside.
    _screen_run(bm, uv, 2.8, -2.3, IN, -2.3, fz)
    _screen_run(bm, uv, 2.0, -2.3, 2.0, -IN, fz)
    cb._box(bm, uv, TIMBER, (3.9, -4.0, fz + 0.5), (0.3, 0.3, 1.0))
    ek.prism(bm, uv, TIMBER, [(-0.2, 0.0), (0.2, 0.0), (0.2, 0.15)], 0.5, loc=(3.9, -4.0, fz + 1.0), along="x")
    # SW: the prie-dieu facing a devotional panel on the west wall, two candle stands.
    cb._box(bm, uv, TIMBER, (-4.6, -3.6, fz + 0.425), (0.35, 0.6, 0.85))
    cb._box(bm, uv, CLOTH, (-4.2, -3.6, fz + 0.075), (0.3, 0.6, 0.15))
    ek.framed_panel(bm, uv, GOLD, TAPESTRY, "west", -3.6, fz + 1.2, 1.0, 1.3)
    for x, y in ((-4.8, -2.6), (-4.8, -4.6)):
        candle_stand(bm, uv, x, y, fz)
