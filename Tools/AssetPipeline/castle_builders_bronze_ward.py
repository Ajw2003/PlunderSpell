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
