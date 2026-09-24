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


def build_bronze_levy_barracks(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeLevyBarracks.svg"""
    h, fz = room_shell(bm, uv, "OuterBailey")
    # Four low clay sleeping benches along the east and west walls, reed mats on them;
    # the two northern ones have kit bags rolled at their north ends.
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * 5.0, sy * 3.5
            cb._box(bm, uv, GRAIN_JAR, (x, y, fz + 0.175), (0.9, 3.0, 0.35))
            cb._box(bm, uv, "vellum_dim", (x, y, fz + 0.365), (0.8, 2.9, 0.03))
            if sy > 0:
                cb._box(bm, uv, "leather", (x, y + 1.1, fz + 0.45), (0.6, 0.5, 0.14))
                cb._anchor(x, y, fz + 0.38)
    # Spear racks on the north wall either side of the archway.
    for sx in (-1, 1):
        x0 = sx * 3.4
        for dx in (-1.0, 1.0):
            cb._box(bm, uv, TIMBER, (x0 + dx, IN - 0.25, fz + 0.75), (0.1, 0.1, 1.5))
        cb._box(bm, uv, TIMBER, (x0, IN - 0.25, fz + 1.41), (2.1, 0.1, 0.1))
        for k in range(5):
            x = x0 - 0.8 + k * 0.4
            mk.paint(bm, mk.add_cylinder(bm, 0.02, 2.1, loc=(x, IN - 0.25, fz + 1.05), segments=6), TIMBER, uv)
            mk.paint(bm, mk.add_cylinder(bm, 0.05, 0.3, loc=(x, IN - 0.25, fz + 2.25), segments=6, radius2=0.005),
                     METAL, uv)
    # Figure-of-eight oxhide shields hung on the south wall, a water jar below each pair.
    for sx in (-1, 1):
        for x in (sx * 3.0, sx * 4.2):
            for z, r in ((1.3, 0.35), (1.9, 0.30)):
                mk.paint(bm, mk.add_cylinder(bm, r, 0.06, loc=(x, -IN + 0.04, fz + z), rot=Euler((math.radians(90), 0, 0)),
                                             segments=10), "leather", uv)
        ek.jar(bm, uv, GRAIN_JAR, sx * 2.2, -5.0, fz, 0.6, 0.36, mouth=0.14, segments=6)
    # SE: a low gaming table; SW: a kit chest.
    cb._table(bm, uv, TIMBER, 3.2, -3.0, fz, 0.9, 0.6, h=0.5)
    cb._chest(bm, uv, -3.2, -3.2, fz, w=1.0, d=0.6, h=0.55, trim=METAL)


def build_bronze_cistern(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeCistern.svg"""
    h, fz = room_shell(bm, uv, "OuterBailey")
    # SE: the cistern head, water below its rim, the lid half drawn, a winch beam on posts over it.
    mk.paint(bm, mk.add_cylinder(bm, 1.1, 0.8, loc=(3.6, -3.6, fz + 0.4), segments=14), "vellum_faint", uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.88, 0.02, loc=(3.6, -3.6, fz + 0.81), segments=14), FRESCO, uv)
    for k in range(12):                                    # a stone lip round the water, so it reads recessed
        a = (k + 0.5) * 2 * math.pi / 12
        mk.paint(bm, mk.add_box(bm, (0.2, 0.55, 0.12), loc=(3.6 + math.cos(a) * 0.99, -3.6 + math.sin(a) * 0.99,
                                                            fz + 0.86), rot=Euler((0, 0, a))), "vellum_faint", uv)
    cb._box(bm, uv, "vellum_dim", (3.6, -3.0, fz + 0.98), (1.6, 0.8, 0.12))
    cb._anchor(3.6, -2.9, fz + 1.04)
    for x in (2.6, 4.6):
        cb._box(bm, uv, TIMBER, (x, -3.6, fz + 0.92 + 0.72), (0.14, 0.14, 1.44))
    cb._box(bm, uv, TIMBER, (3.6, -3.6, fz + 2.35), (2.1, 0.12, 0.12))
    mk.paint(bm, mk.add_cylinder(bm, 0.2, 0.35, loc=(2.2, -4.8, fz + 0.175), segments=8), TIMBER, uv)   # bucket
    cb._anchor(2.2, -4.8, fz + 0.35)
    # NW: the hydria stand against the north wall, a water jar at each end.
    cb._table(bm, uv, TIMBER, -3.8, 4.9, fz, 1.6, 0.5, h=0.5)
    for dx in (-0.55, 0.55):
        ek.jar(bm, uv, GRAIN_JAR, -3.8 + dx, 4.9, fz + 0.5, 0.62, 0.40, mouth=0.16, segments=8)
    # NE: a clay basin on its pedestal.
    mk.paint(bm, mk.add_cylinder(bm, 0.25, 0.10, loc=(3.8, 4.2, fz + 0.05), segments=10), LINEN, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.08, 0.72, loc=(3.8, 4.2, fz + 0.44), segments=8), GRAIN_JAR, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.45, 0.20, loc=(3.8, 4.2, fz + 0.90), segments=12, radius2=0.55), GRAIN_JAR, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.42, 0.03, loc=(3.8, 4.2, fz + 0.99), segments=12), FRESCO, uv)
    cb._anchor(3.8, 4.2, fz + 1.0)
    # SW: a stone trough against the south wall.
    cb._box(bm, uv, "vellum_faint", (-3.8, -5.0, fz + 0.3), (2.2, 0.7, 0.6))
    cb._box(bm, uv, FRESCO, (-3.8, -5.0, fz + 0.61), (2.0, 0.5, 0.02))


def build_bronze_oil_press(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeOilPress.svg"""
    h, fz = room_shell(bm, uv, "OuterBailey")
    # NW: the lever press. An oil-dark floor under it, the stone bed with olive frails,
    # the beam socketed in the west wall and running east down to its weights.
    cb._box(bm, uv, "line", (-3.9, 4.2, fz + 0.01), (3.0, 2.4, 0.02))
    cb._box(bm, uv, "vellum_faint", (-3.4, 4.2, fz + 0.2), (1.4, 1.4, 0.4))
    for k in range(2):
        mk.paint(bm, mk.add_cylinder(bm, 0.4 - k * 0.03, 0.12, loc=(-3.6, 4.3, fz + 0.46 + k * 0.12), segments=10),
                 "leather", uv)
    cb._anchor(-2.95, 3.75, fz + 0.4)
    length, tilt = math.hypot(3.45, 0.45), math.atan2(0.45, 3.45)
    mk.paint(bm, mk.add_box(bm, (length, 0.2, 0.2), loc=(-3.725, 4.2, fz + 1.475), rot=Euler((0, tilt, 0))), TIMBER, uv)
    cb._box(bm, uv, TIMBER, (-3.6, 4.3, fz + 1.035), (0.16, 0.16, 0.79))        # post from the frails to the beam
    mk.paint(bm, mk.add_cylinder(bm, 0.02, 0.75, loc=(-2.05, 4.2, fz + 0.875), segments=6), "vellum_dim", uv)
    for k in range(2):
        mk.paint(bm, mk.add_cylinder(bm, 0.3 - k * 0.03, 0.25, loc=(-2.05, 4.2, fz + 0.125 + k * 0.25), segments=10),
                 "vellum_faint", uv)
    cb._box(bm, uv, "vellum_faint", (-3.4, 3.35, fz + 0.3), (0.12, 0.3, 0.08))  # spout to the jar
    ek.jar(bm, uv, GRAIN_JAR, -3.4, 3.1, fz, 0.55, 0.5, mouth=0.28, segments=8)
    # NE and SE: amphora racks against the north and south walls, a board over five amphorae.
    for y in (5.1, -5.1):
        for x in (2.1, 5.1):
            cb._box(bm, uv, TIMBER, (x, y, fz + 0.375), (0.08, 0.4, 0.75))
        cb._box(bm, uv, TIMBER, (3.6, y, fz + 0.75), (3.0, 0.45, 0.06))
        cb._box(bm, uv, TIMBER, (3.6, y, fz + 0.32), (3.0, 0.06, 0.04))
        for k in range(5):
            amphora(bm, uv, 2.4 + k * 0.55, y, fz)
        cb._anchor(3.6, y, fz + 0.78)
    # SW: small pithoi of oil, and a jug table.
    for x, y in ((-4.6, -4.6), (-3.5, -4.8), (-4.7, -3.4)):
        pithos(bm, uv, x, y, fz, height=1.2, belly=0.7, pigment=OIL_JAR, lid=True)
    cb._table(bm, uv, TIMBER, -2.6, -3.4, fz, 0.6, 0.6, h=0.7)
