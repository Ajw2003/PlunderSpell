"""
Bronze Age OuterBailey pieces: BronzeChariotShed, BronzeFoundry, BronzeLevyBarracks, BronzeCistern, BronzeOilPress.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/bronze/<Key>.json,
drawing: docs/art/rooms/concept/bronze/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_bronze.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_bronze import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


# ── OuterBailey: the lower town's workshops ─────────────────────────────

def build_bronze_chariot_shed(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeChariotShed.svg"""
    h, fz = room_shell(bm, uv, "OuterBailey")
    # NW: the war chariot, axle north-south, its pole east to a forked stand.
    for y in (3.15, 4.45):
        ek.wheel(bm, uv, TIMBER, -4.4, y, fz + 0.45, 0.45, along="y")
    mk.paint(bm, mk.add_cylinder(bm, 0.04, 1.40, loc=(-4.4, 3.8, fz + 0.45), rot=Euler((math.radians(90), 0, 0)),
                                 segments=6), METAL, uv)
    cb._box(bm, uv, TIMBER, (-4.4, 3.8, fz + 0.58), (0.9, 1.1, 0.06))
    cb._box(bm, uv, "vellum_dim", (-3.95, 3.8, fz + 0.86), (0.05, 1.1, 0.50))          # wicker screen, front
    for y in (3.28, 4.32):
        cb._box(bm, uv, "vellum_dim", (-4.4, y, fz + 0.78), (0.9, 0.05, 0.35))       # side screens
    cb._anchor(-4.4, 3.8, fz + 0.61)
    cb._box(bm, uv, TIMBER, (-2.975, 3.8, fz + 0.62), (1.95, 0.08, 0.08))             # the pole
    cb._box(bm, uv, TIMBER, (-2.1, 3.8, fz + 0.29), (0.08, 0.08, 0.58))               # its forked stand
    cb._box(bm, uv, TIMBER, (-2.2, 3.8, fz + 0.69), (0.08, 1.0, 0.06))                # the yoke
    # NE: two stalls against the north wall, clay mangers with hay.
    for x in (2.2, 3.8):
        cb._box(bm, uv, TIMBER, (x, IN - 1.1, fz + 0.65), (0.12, 2.2, 1.3))
    for x in (3.0, 4.6):
        cb._box(bm, uv, GRAIN_JAR, (x, IN - 0.3, fz + 0.3), (1.2, 0.5, 0.6))
        cb._box(bm, uv, "vellum_dim", (x, IN - 0.3, fz + 0.62), (1.0, 0.36, 0.06))
    cb._anchor(4.6, IN - 0.3, fz + 0.65)
    # SE: fodder bundles stacked by the south wall.
    for x, y, z in ((3.0, -4.6, 0.0), (4.4, -4.6, 0.0), (3.7, -4.6, 0.6), (4.6, -3.2, 0.0)):
        cb._box(bm, uv, "vellum_dim", (x, y, fz + 0.3 + z), (1.2, 0.8, 0.6))
    cb._anchor(3.7, -4.6, fz + 1.2)
    # SW: the harness rail on the west wall, straps hanging from it, and the tack chest.
    cb._box(bm, uv, TIMBER, (-IN + 0.05, -3.6, fz + 1.5), (0.1, 2.0, 0.1))
    for y in (-4.3, -3.8, -3.3, -2.8):
        cb._box(bm, uv, "leather", (-IN + 0.12, y, fz + 1.05), (0.04, 0.08, 0.9))
    cb._chest(bm, uv, -4.4, -4.9, fz, w=1.0, d=0.6, h=0.55, trim=METAL)
