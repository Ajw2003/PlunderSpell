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


def build_bronze_foundry(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeFoundry.svg"""
    h, fz = room_shell(bm, uv, "OuterBailey")
    # Soot over the furnace corner: the top of the north and west walls.
    for side, along in (("north", -3.4), ("west", 3.4)):
        ek.wall_panel(bm, uv, SOOT, side, along, fz + h - 1.8, 4.1, 1.8 - 0.45)
    # NW: the shaft furnace, its glowing mouth to the south, fire at its top.
    mk.paint(bm, mk.add_cylinder(bm, 0.8, 1.4, loc=(-4.2, 4.2, fz + 0.7), segments=12, radius2=0.35), GRAIN_JAR, uv)
    cb._box(bm, uv, RED, (-4.2, 3.47, fz + 0.275), (0.5, 0.12, 0.35))
    mk.paint(bm, mk.add_cylinder(bm, 0.36, 0.05, loc=(-4.2, 4.2, fz + 1.425), segments=12), SOOT, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.14, 0.40, loc=(-4.2, 4.2, fz + 1.45 + 0.2), segments=6, radius2=0.02), RED, uv)
    # Two bag bellows on the floor east of it, a clay nozzle from each toward the furnace.
    for y in (4.6, 3.8):
        mk.paint(bm, mk.add_sphere(bm, 0.35, loc=(-3.0, y, fz + 0.175), segments=8, rings=6, scale=(1.0, 0.7, 0.5)),
                 "leather", uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.04, 0.35, loc=(-3.45, y, fz + 0.22), rot=Euler((0, math.radians(90), 0)),
                                     segments=6), GRAIN_JAR, uv)
    # NE: the stone mould bench against the north wall, moulds and crucibles on it.
    cb._box(bm, uv, "vellum_faint", (3.6, 5.15, fz + 0.4), (2.6, 0.7, 0.8))
    for x in (2.6, 3.1):
        cb._box(bm, uv, "line", (x, 5.15, fz + 0.86), (0.4, 0.3, 0.12))
    for x in (4.2, 4.6):
        mk.paint(bm, mk.add_cylinder(bm, 0.12, 0.15, loc=(x, 5.15, fz + 0.875), segments=8, radius2=0.09), SOOT, uv)
    cb._anchor(3.6, 5.15, fz + 0.8)
    # East wall: the tool shelf, tongs and ladles on it.
    cb._shelf(bm, uv, 5.2, 2.9, fz, 1.6, levels=2, along_x=False)
    for y in (2.5, 3.3):
        cb._box(bm, uv, METAL, (5.2, y, fz + 0.41), (0.3, 0.05, 0.04))
    # SE: a stack of cast ingots on its pallet.
    ingot_stack(bm, uv, 4.0, -4.3, fz, 1.8, 1.2, 4)
    # SW: the charcoal heap, the water trough, the stone anvil.
    mk.paint(bm, mk.add_cylinder(bm, 0.9, 0.6, loc=(-4.2, -4.3, fz + 0.3), segments=10, radius2=0.25), SOOT, uv)
    cb._box(bm, uv, "vellum_faint", (-2.6, -5.0, fz + 0.25), (1.4, 0.6, 0.5))
    cb._box(bm, uv, FRESCO, (-2.6, -5.0, fz + 0.51), (1.2, 0.4, 0.02))
    cb._box(bm, uv, "vellum_faint", (-2.6, -3.0, fz + 0.25), (0.6, 0.6, 0.5))
    cb._anchor(-2.6, -3.0, fz + 0.5)
