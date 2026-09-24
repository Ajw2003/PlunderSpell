"""
Bronze Age InnerWard pieces: BronzePithosMagazine, BronzeFrescoCourt, BronzeShrine, BronzePalaceKitchen, BronzeTabletArchive.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/bronze/<Key>.json,
drawing: docs/art/rooms/concept/bronze/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_bronze.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_bronze import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


# ── InnerWard: the palace ───────────────────────────────────────────────

def build_bronze_pithos_magazine(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzePithosMagazine.svg"""
    h, fz = room_shell(bm, uv, "InnerWard")
    jar_h, jar_b, bench_h = 1.70, 0.90, 0.30
    rows_x, rows_y = (2.5, 3.8), (2.4, 3.5, 4.6)
    # One clay bench per quadrant, two rows of three pithoi on it: grain west, oil east (lidded).
    for sx in (-1, 1):
        for sy in (-1, 1):
            xs = [sx * x for x in rows_x]
            ys = [sy * y for y in rows_y]
            cb._box(bm, uv, "leather", ((min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2, fz + bench_h / 2),
                    (max(xs) - min(xs) + 1.2, max(ys) - min(ys) + 1.0, bench_h))
            for x in xs:
                for y in ys:
                    pithos(bm, uv, x, y, fz + bench_h, height=jar_h, belly=jar_b,
                           pigment=OIL_JAR if sx > 0 else GRAIN_JAR, lid=sx > 0)
    # Oil pools on the floor by the east benches: flat, and they burn.
    for x, y, r in ((3.2, 1.9, 0.55), (4.8, -2.9, 0.40)):
        mk.paint(bm, mk.add_cylinder(bm, r, 0.02, loc=(x, y, fz + 0.01), segments=10, scale=(1.0, 0.6, 1.0)), SOOT, uv)
    # A step-ladder leant on the NW front jar from the west aisle.
    length, lean = math.hypot(0.85, 1.95), math.atan2(0.85, 1.95)
    for y in (3.05, 3.30):
        mk.paint(bm, mk.add_box(bm, (0.06, 0.06, length), loc=(-5.2 + 0.425, y, fz + 0.975),
                                rot=Euler((0, lean, 0))), TIMBER, uv)
    for k in range(1, 7):
        t = k / 7
        cb._box(bm, uv, TIMBER, (-5.2 + 0.85 * t, 3.175, fz + 1.95 * t), (0.05, 0.30, 0.04))
    # The scribe's bench in the SW wall aisle, tablets on it; a ladle table NE; a grain basket NW.
    cb._box(bm, uv, "leather", (-5.2, -3.5, fz + 0.4), (0.6, 1.8, 0.8))
    for k in range(6):
        cb._box(bm, uv, "vellum_dim", (-5.2, -4.2 + k * 0.28, fz + 0.81), (0.12, 0.07, 0.02))
    cb._anchor(-5.2, -3.5, fz + 0.8)
    offering_table(bm, uv, 5.1, 2.3, fz, w=0.5, d=0.5)
    mk.paint(bm, mk.add_cylinder(bm, 0.28, 0.40, loc=(-5.0, 2.3, fz + 0.2), segments=8, radius2=0.32),
             "vellum_dim", uv)
    cb._anchor(-5.0, 2.3, fz + 0.4)


def build_bronze_fresco_court(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeFrescoCourt.svg"""
    h, fz = room_shell(bm, uv, "InnerWard")
    fresco_band(bm, uv, fz, sides=rk.SIDES, bottom=1.4, top=3.0)
    # Four red columns round the crossing, up to the wall top.
    for sx in (-1, 1):
        for sy in (-1, 1):
            ek.tapered_column(bm, uv, RED, SOOT, sx * 2.4, sy * 2.4, fz, h)
    # Benches: along the north and south walls (with fleeces, loot), and short ones on the east and west walls.
    for x in (-3.6, 3.6):
        clay_bench(bm, uv, x, IN - 0.25, fz, 3.2)
        clay_bench(bm, uv, x, -IN + 0.25, fz, 3.2)
    for x in (-IN + 0.25, IN - 0.25):
        for y in (-3.4, 3.4):
            cb._box(bm, uv, LINEN, (x, y, fz + 0.2), (0.5, 2.4, 0.4))
    # NE: the round fire altar, three painted bands, the fire at its west side.
    mk.paint(bm, mk.add_cylinder(bm, 0.6, 0.9, loc=(3.6, 3.6, fz + 0.45), segments=12), LINEN, uv)
    for k in range(3):
        mk.paint(bm, mk.add_cylinder(bm, 0.61, 0.05, loc=(3.6, 3.6, fz + 0.175 + k * 0.3), segments=12), RED, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.16, 0.45, loc=(3.35, 3.6, fz + 0.9 + 0.225), segments=6, radius2=0.02),
             RED, uv)
    cb._anchor(3.6, 3.6, fz + 0.9)
    # Two offering tables to the south.
    for x in (-3.2, 3.2):
        offering_table(bm, uv, x, -3.4, fz)


def _idol(bm, uv, x, y, z, h=0.45):
    """A clay idol with raised arms, standing on z."""
    mk.paint(bm, mk.add_cylinder(bm, 0.10, h * 0.72, loc=(x, y, z + h * 0.36), segments=6, radius2=0.03),
             "vellum_dim", uv)
    ek.sphere(bm, uv, "vellum_dim", x, y, z + h * 0.72 - 0.01, 0.06, segments=6, rings=4)
    for s in (-1, 1):
        mk.paint(bm, mk.add_box(bm, (0.03, 0.03, 0.16), loc=(x + s * 0.07, y, z + h * 0.72 + 0.02),
                                rot=Euler((0, s * 0.5, 0))), "vellum_dim", uv)


def build_bronze_shrine(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeShrine.svg"""
    h, fz = room_shell(bm, uv, "InnerWard")
    for side in ("north", "east", "west"):
        for along in (-3.4, 3.4):
            ek.wall_panel(bm, uv, FRESCO, side, along, fz + 1.6, 4.1, 1.4)
            ek.wall_panel(bm, uv, RED, side, along, fz + 3.0, 4.1, 0.18)
    # NE: the stepped bench altar, the horns on its top step, idols along the lower one.
    cb._box(bm, uv, LINEN, (3.8, 5.05, fz + 0.225), (2.6, 0.9, 0.45))
    cb._box(bm, uv, RED, (3.8, 4.6, fz + 0.40), (2.61, 0.04, 0.08))          # band on the step's front edge
    cb._box(bm, uv, LINEN, (3.8, 5.27, fz + 0.625), (1.8, 0.45, 0.35))
    horns(bm, uv, 3.8, 5.27, fz + 0.8, size=0.8, along="x")
    for x in (3.0, 3.35, 4.25):
        _idol(bm, uv, x, 4.75, fz + 0.45)
    cb._anchor(4.6, 4.75, fz + 0.45)
    offering_table(bm, uv, 3.6, 3.3, fz)
    # NW: the double axe on a bronze pole, on its stepped stand.
    cb._box(bm, uv, LINEN, (-3.8, 4.2, fz + 0.15), (0.8, 0.8, 0.30))
    cb._box(bm, uv, LINEN, (-3.8, 4.2, fz + 0.45), (0.5, 0.5, 0.30))
    mk.paint(bm, mk.add_cylinder(bm, 0.03, 1.4, loc=(-3.8, 4.2, fz + 1.3), segments=6), METAL, uv)
    ek.prism(bm, uv, METAL, [(-0.5, -0.25), (-0.05, -0.06), (-0.05, 0.06), (-0.5, 0.25)], 0.04,
             loc=(-3.8, 4.2, fz + 2.0), along="y")
    ek.prism(bm, uv, METAL, [(0.05, -0.06), (0.5, -0.25), (0.5, 0.25), (0.05, 0.06)], 0.04,
             loc=(-3.8, 4.2, fz + 2.0), along="y")
    # Worshippers' benches: three low rows each side in the south quadrants, facing north.
    for sx in (-1, 1):
        for y in (-2.4, -3.6, -4.8):
            cb._box(bm, uv, LINEN, (sx * 3.6, y, fz + 0.2), (3.0, 0.4, 0.4))
        cb._anchor(sx * 3.6, -2.4, fz + 0.4)


def build_bronze_palace_kitchen(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzePalaceKitchen.svg"""
    h, fz = room_shell(bm, uv, "InnerWard")
    # Soot: the upper walls blackened 2.2 m down from the top by the hearth and oven.
    for side in rk.SIDES:
        for along in (-3.4, 3.4):
            ek.wall_panel(bm, uv, SOOT, side, along, fz + h - 2.2, 4.1, 2.2 - 0.45)
    # NW: the round hearth, fire on it, the tripod cauldron standing over the fire.
    mk.paint(bm, mk.add_cylinder(bm, 0.8, 0.35, loc=(-3.8, 3.8, fz + 0.175), segments=12), GRAIN_JAR, uv)
    for dx, hh in ((-0.25, 0.35), (0.0, 0.5), (0.25, 0.4)):
        mk.paint(bm, mk.add_cylinder(bm, 0.12, hh, loc=(-3.8 + dx, 3.8, fz + 0.35 + hh / 2), segments=6,
                                     radius2=0.02), RED, uv)
    tripod(bm, uv, -3.8, 3.8, fz + 0.35)
    # NE: the domed bread oven with its mouth to the south, and the loaf table.
    mk.paint(bm, mk.add_cylinder(bm, 0.9, 1.2, loc=(4.2, 4.2, fz + 0.6), segments=12, radius2=0.25), GRAIN_JAR, uv)
    cb._box(bm, uv, SOOT, (4.2, 3.47, fz + 0.275), (0.5, 0.12, 0.35))
    cb._table(bm, uv, TIMBER, 2.4, 4.9, fz, 1.0, 0.6, h=0.8)
    for dx in (-0.3, 0.3):
        mk.paint(bm, mk.add_sphere(bm, 0.16, loc=(2.4 + dx, 4.9, fz + 0.86), segments=6, rings=4,
                                   scale=(1.0, 1.0, 0.4)), "vellum_dim", uv)
    # SE: the quern bench along the south wall, a saddle quern at each end.
    cb._box(bm, uv, "vellum_faint", (3.6, -5.1, fz + 0.25), (3.0, 0.7, 0.5))
    for x in (2.5, 4.7):
        cb._box(bm, uv, "vellum_dim", (x, -5.1, fz + 0.55), (0.5, 0.3, 0.1))
    cb._anchor(3.6, -5.1, fz + 0.5)
    # SW: the pot bench along the west wall, pots on it; two amphorae on the floor.
    cb._box(bm, uv, GRAIN_JAR, (-5.1, -3.6, fz + 0.25), (0.7, 3.0, 0.5))
    for y in (-4.7, -4.1, -3.1, -2.5):
        mk.paint(bm, mk.add_cylinder(bm, 0.2, 0.3, loc=(-5.1, y, fz + 0.5 + 0.15), segments=8, radius2=0.15),
                 SOOT, uv)
    cb._anchor(-5.1, -3.6, fz + 0.5)
    for x, y in ((-2.6, -4.8), (-3.3, -4.9)):
        amphora(bm, uv, x, y, fz)


def build_bronze_tablet_archive(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeTabletArchive.svg"""
    h, fz = room_shell(bm, uv, "InnerWard")
    for side in rk.SIDES:
        for along in (-3.4, 3.4):
            ek.wall_panel(bm, uv, RED, side, along, fz, 4.1, 0.40)
    # North wall: clay benches with baskets of tablets, a clear space in the middle of each.
    for sx in (-1, 1):
        x = sx * 3.6
        cb._box(bm, uv, GRAIN_JAR, (x, IN - 0.3, fz + 0.3), (3.2, 0.6, 0.6))
        for dx in (-1.0, 1.0):
            mk.paint(bm, mk.add_cylinder(bm, 0.22, 0.30, loc=(x + dx, IN - 0.3, fz + 0.75), segments=8), TIMBER, uv)
            for k in range(4):
                cb._box(bm, uv, "vellum_dim", (x + dx - 0.15 + k * 0.1, IN - 0.3, fz + 0.92), (0.07, 0.12, 0.06))
        cb._anchor(x, IN - 0.3, fz + 0.6)
    # East wall: the tablet shelf, tablets laid along every board.
    cb._shelf(bm, uv, 5.2, 3.2, fz, 2.2, levels=3, along_x=False)
    for z in (0.35, 0.90, 1.45):
        cb._box(bm, uv, "vellum_dim", (5.2, 2.7, fz + z + 0.075), (0.35, 0.9, 0.09))
    # SE: the scribe's table, tablets on it, and his stool.
    cb._table(bm, uv, TIMBER, 3.6, -3.4, fz, 1.4, 0.8, h=0.75)
    for k in range(4):
        cb._box(bm, uv, "vellum_dim", (3.2 + k * 0.25, -3.1, fz + 0.785), (0.12, 0.07, 0.02))
    mk.paint(bm, mk.add_cylinder(bm, 0.2, 0.45, loc=(3.6, -4.2, fz + 0.225), segments=8), TIMBER, uv)
    # SW: the archive guard's post: a spear rack on the west wall, his stool, a row of sealed jars.
    for y in (-4.4, -2.8):
        cb._box(bm, uv, TIMBER, (-5.3, y, fz + 0.75), (0.1, 0.1, 1.5))
    cb._box(bm, uv, TIMBER, (-5.3, -3.6, fz + 1.4), (0.15, 1.8, 0.12))
    for y in (-4.2, -3.8, -3.4, -3.0):
        mk.paint(bm, mk.add_cylinder(bm, 0.025, 2.4, loc=(-5.3, y, fz + 1.2), segments=6), METAL, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.22, 0.45, loc=(-4.2, -3.2, fz + 0.225), segments=8), TIMBER, uv)
    cb._anchor(-4.2, -3.2, fz + 0.45)
    for x in (-4.8, -4.1, -3.4, -2.7):
        ek.jar(bm, uv, GRAIN_JAR, x, -5.1, fz, 0.60, 0.36, mouth=0.14, segments=6)
        mk.paint(bm, mk.add_cylinder(bm, 0.09, 0.03, loc=(x, -5.1, fz + 0.615), segments=6), LINEN, uv)
