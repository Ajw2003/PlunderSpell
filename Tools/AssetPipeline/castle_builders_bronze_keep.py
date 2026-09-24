"""
Bronze Age Keep pieces: BronzeMegaron, BronzeTreasury, BronzeQueensHall, BronzeBathRoom, BronzeMegaronStair.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/bronze/<Key>.json,
drawing: docs/art/rooms/concept/bronze/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_bronze.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_bronze import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


# ── Keep: the wanax's quarters ──────────────────────────────────────────

def build_bronze_megaron(bm, uv):
    h, fz = room_shell(bm, uv, "Keep")
    # The hearth: a flat painted ring at the crossing (the art bible's raised
    # rim would sit across the walkway, so it is inlaid instead).
    ek.disc(bm, uv, FRESCO, 0, 0, fz, 1.8, thickness=0.04, segments=16)
    ek.disc(bm, uv, RED, 0, 0, fz, 1.5, thickness=0.07, segments=16)
    ek.disc(bm, uv, SOOT, 0, 0, fz, 1.05, thickness=0.1, segments=16)
    # Four red columns on the 5 m square round the hearth, tapering downward.
    for sx in (-1, 1):
        for sy in (-1, 1):
            ek.tapered_column(bm, uv, RED, SOOT, sx * 2.5, sy * 2.5, fz, h)
    # The throne against the east wall in the NE quadrant, facing west, with
    # painted griffins either side of it.
    tx, ty = IN - 0.45, 3.7
    cb._box(bm, uv, LINEN, (tx, ty, fz + 0.23), (0.7, 1.0, 0.46))
    cb._box(bm, uv, LINEN, (IN - 0.1, ty, fz + 0.9), (0.25, 1.0, 1.8))
    for k, dy in enumerate((-0.35, 0.0, 0.35)):
        mk.paint(bm, mk.add_cylinder(bm, 0.14, 0.25, loc=(IN - 0.1, ty + dy, fz + 1.8 + (0.12 if k == 1 else 0.06)),
                                     rot=Euler((0, math.radians(90), 0)), segments=8), LINEN, uv)
    cb._anchor(tx, ty, fz + 0.46)
    cb._box(bm, uv, "vellum_faint", (tx - 0.9, ty, fz + 0.06), (1.2, 1.6, 0.12))   # footstool step
    cb._anchor(tx - 0.9, ty, fz + 0.12)
    for dy in (-1.5, 1.4):
        ek.wall_panel(bm, uv, FRESCO, "east", ty + dy, fz + 0.9, 1.4, 1.1)
    # Clay benches with fleeces along the north and south walls, in the quadrants.
    for sx in (-1, 1):
        clay_bench(bm, uv, sx * 3.1, IN - 0.25, fz, 2.4)
        clay_bench(bm, uv, sx * 3.6, -IN + 0.25, fz, 3.2)
    # Two tripods by the hearth, three offering tables.
    tripod(bm, uv, -3.5, 2.0, fz)
    tripod(bm, uv, 3.3, -2.0, fz)
    cb._table(bm, uv, "vellum_faint", -4.6, -3.6, fz, 0.6, 0.6, h=0.7)
    cb._table(bm, uv, "vellum_faint", -2.4, -4.3, fz, 0.6, 0.6, h=0.7)
    cb._table(bm, uv, "vellum_faint", 4.6, -3.6, fz, 0.6, 0.6, h=0.7)
    fresco_band(bm, uv, fz, sides=("north", "south", "west"))


def build_bronze_treasury(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeTreasury.svg"""
    h, fz = room_shell(bm, uv, "Keep")
    # A haematite dado on every wall section: the only paint in the room.
    for side in rk.SIDES:
        for along in (-3.4, 3.4):
            ek.wall_panel(bm, uv, RED, side, along, fz, 4.1, 0.40)
    # NE: the stepped gypsum plinth, the gold death-mask on top.
    cb._box(bm, uv, LINEN, (4.3, 4.3, fz + 0.25), (1.4, 1.4, 0.50))
    cb._box(bm, uv, LINEN, (4.3, 4.3, fz + 0.70), (0.8, 0.8, 0.40))
    mk.paint(bm, mk.add_sphere(bm, 0.14, loc=(4.3, 4.3, fz + 0.90 + 0.1), segments=8, rings=6,
                               scale=(1.0, 0.45, 1.4)), GOLD, uv)
    cb._anchor(4.3, 4.3, fz + 0.90)
    # Two ingot stacks on timber pallets, crosswise layers of 0.60 × 0.40 × 0.06 slabs.
    for x, y, w, d, layers in ((4.0, -4.3, 1.8, 1.2, 4), (-3.9, -2.9, 1.2, 0.9, 3)):
        ingot_stack(bm, uv, x, y, fz, w, d, layers)
    # Two bronze-bound timber chests along the south wall, west of the archway.
    for x in (-4.6, -2.9):
        cb._chest(bm, uv, x, -4.9, fz, w=1.0, d=0.6, h=0.55, trim=METAL)
    # NW: the faience shelf against the north wall, with blue pieces on its boards.
    cb._shelf(bm, uv, -3.7, IN - 0.3, fz, 3.0, levels=3)
    for i, z in enumerate((0.35, 0.90, 1.45)):
        for k in range(5):
            px = -5.2 + 0.35 + k * 0.55 + (i % 2) * 0.2
            if px > -2.4:
                continue
            mk.paint(bm, mk.add_cylinder(bm, 0.08, 0.26, loc=(px, IN - 0.3, fz + z + 0.03 + 0.13), segments=6,
                                         radius2=0.04), FRESCO, uv)     # a faience flask
    # A bronze tripod each side (the east one by the plinth), clear of the walkway.
    tripod(bm, uv, -2.4, 2.6, fz)
    tripod(bm, uv, 2.6, 4.6, fz)
    # SE: two bronze cauldrons on the floor by the east wall, something in each.
    for x in (4.2, 5.0):
        mk.paint(bm, mk.add_cylinder(bm, 0.34, 0.45, loc=(x, -2.3, fz + 0.225), segments=10, radius2=0.26),
                 METAL, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.25, 0.04, loc=(x, -2.3, fz + 0.43), segments=10), SOOT, uv)
        cb._anchor(x, -2.3, fz + 0.45)


def build_bronze_queens_hall(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeQueensHall.svg"""
    h, fz = room_shell(bm, uv, "Keep")
    # Dolphin frescoes: a sea band on the north, east and west walls, haematite spirals over it.
    for side in ("north", "east", "west"):
        for along in (-3.4, 3.4):
            ek.wall_panel(bm, uv, FRESCO, side, along, fz + 1.6, 4.1, 1.6)
            ek.wall_panel(bm, uv, RED, side, along, fz + 3.2, 4.1, 0.18)
    # NW: the queen's bed, head to the west wall, a rug before it.
    bx, by = -4.6, 3.9
    for ox in (-0.65, 0.65):
        for oy in (-1.05, 1.05):
            cb._box(bm, uv, TIMBER, (bx + ox, by + oy, fz + 0.25), (0.10, 0.10, 0.50))
    cb._box(bm, uv, TIMBER, (bx, by, fz + 0.425), (1.5, 2.3, 0.15))
    cb._box(bm, uv, LINEN, (bx, by, fz + 0.56), (1.4, 2.1, 0.12))
    cb._box(bm, uv, "vellum_dim", (bx, by + 0.7, fz + 0.66), (0.9, 0.5, 0.08))     # fleece
    cb._box(bm, uv, TIMBER, (-IN + 0.10, by, fz + 0.825), (0.20, 2.2, 0.95))        # headboard
    cb._anchor(bx, by, fz + 0.62)
    cb._box(bm, uv, "leather", (-2.8, 3.4, fz + 0.015), (1.6, 2.4, 0.03))           # rug (flat)
    # NE: the warp-weighted loom against the north wall, the web half woven, clay weights on the warp.
    ly = IN - 0.3
    for x in (2.6, 4.8):
        cb._box(bm, uv, TIMBER, (x, ly, fz + 1.15), (0.12, 0.12, 2.30))
    cb._box(bm, uv, TIMBER, (3.7, ly, fz + 2.24), (2.4, 0.12, 0.12))
    cb._box(bm, uv, LINEN, (3.7, ly, fz + 1.49), (1.96, 0.03, 1.38))
    cb._box(bm, uv, RED, (3.7, ly, fz + 1.95), (1.96, 0.05, 0.20))
    for k in range(7):
        mk.paint(bm, mk.add_cylinder(bm, 0.06, 0.12, loc=(2.9 + k * 0.27, ly, fz + 0.74), segments=6, radius2=0.04),
                 GRAIN_JAR, uv)
    cb._box(bm, uv, TIMBER, (3.7, 4.2, fz + 0.225), (0.5, 0.5, 0.45))              # weaver's stool
    cb._anchor(3.7, 4.2, fz + 0.45)
    mk.paint(bm, mk.add_cylinder(bm, 0.25, 0.35, loc=(2.3, 4.2, fz + 0.175), segments=8), "leather", uv)
    # SE: a small round hearth, and a clay bench along the east wall.
    mk.paint(bm, mk.add_cylinder(bm, 0.6, 0.2, loc=(4.2, -4.2, fz + 0.1), segments=12), GRAIN_JAR, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.4, 0.05, loc=(4.2, -4.2, fz + 0.22), segments=12), RED, uv)
    clay_bench(bm, uv, 5.25, -2.6, fz, 1.6, along_x=False)
    # SW: a bronze-bound chest and a dressing table with oil flasks.
    cb._chest(bm, uv, -4.5, -4.9, fz, w=1.0, d=0.6, h=0.55, trim=METAL)
    cb._table(bm, uv, "vellum_faint", -2.6, -4.8, fz, 0.9, 0.5, h=0.75)
    for dx in (-0.25, 0.0, 0.25):
        mk.paint(bm, mk.add_cylinder(bm, 0.05, 0.16, loc=(-2.6 + dx, -4.8, fz + 0.75 + 0.08), segments=6,
                                     radius2=0.03), FRESCO, uv)


def build_bronze_bath_room(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeBathRoom.svg"""
    h, fz = room_shell(bm, uv, "Keep")
    # A low band of painted lilies on the north, east and west walls.
    for side in ("north", "east", "west"):
        for along in (-3.4, 3.4):
            ek.wall_panel(bm, uv, "vellum_dim", side, along, fz + 1.2, 4.1, 1.2)
            ek.wall_panel(bm, uv, RED, side, along, fz + 2.4, 4.1, 0.16)
    # NW: the lustral basin, a stone bowl on a pedestal.
    mk.paint(bm, mk.add_cylinder(bm, 0.25, 0.10, loc=(-4.2, 4.2, fz + 0.05), segments=10), LINEN, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.06 + 0.0, 0.72, loc=(-4.2, 4.2, fz + 0.44), segments=8, radius2=0.08), LINEN, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.45, 0.20, loc=(-4.2, 4.2, fz + 0.90), segments=12, radius2=0.55), LINEN, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.42, 0.03, loc=(-4.2, 4.2, fz + 0.99), segments=12), FRESCO, uv)
    cb._anchor(-4.2, 4.2, fz + 1.0)
    # NE: the painted clay tub on a gypsum plinth, a hydria either side.
    cb._box(bm, uv, LINEN, (3.8, 4.8, fz + 0.075), (2.0, 1.1, 0.15))
    cb._box(bm, uv, GRAIN_JAR, (3.8, 4.8, fz + 0.45), (1.6, 0.7, 0.60))
    cb._box(bm, uv, RED, (3.8, 4.8, fz + 0.55), (1.61, 0.71, 0.14))
    cb._box(bm, uv, FRESCO, (3.8, 4.8, fz + 0.75), (1.4, 0.5, 0.03))              # the water
    cb._anchor(3.8, 4.8, fz + 0.75)
    for x, y in ((2.45, 5.0), (5.1, 3.6)):
        ek.jar(bm, uv, GRAIN_JAR, x, y, fz, 0.62, 0.40, mouth=0.16)
    # A water channel cut in the floor: south from the tub, along the cross, out the south archway (flat).
    for loc, size in (((3.05, 2.125, fz + 0.015), (0.10, 4.25, 0.03)),
                      ((1.5, 0.0, fz + 0.015), (3.15, 0.10, 0.03)),
                      ((0.0, -2.75, fz + 0.015), (0.10, 5.5, 0.03))):
        cb._box(bm, uv, FRESCO, loc, size)
    # SE: a plastered bench along the south wall, towels folded on it.
    clay_bench(bm, uv, 3.6, -IN + 0.25, fz, 3.2)
    for x in (2.6, 3.2):
        cb._box(bm, uv, "vellum_dim", (x, -IN + 0.25, fz + 0.45), (0.45, 0.35, 0.10))
    # SW: a shelf of oil flasks against the south wall, an oil stand by the west wall.
    cb._shelf(bm, uv, -3.7, -IN + 0.3, fz, 2.6, levels=3)
    for z in (0.35, 0.90, 1.45):
        for k in range(4):
            mk.paint(bm, mk.add_cylinder(bm, 0.07, 0.22, loc=(-4.7 + k * 0.6, -IN + 0.3, fz + z + 0.03 + 0.11),
                                         segments=6, radius2=0.03), GRAIN_JAR, uv)
    offering_table(bm, uv, -4.8, -2.6, fz)


def build_bronze_megaron_stair(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeMegaronStair.svg"""
    h, fz = room_shell(bm, uv, "Keep")
    top = 2.6
    # The kit's L-stair to a railed gallery in the NW quadrant: gypsum flights, cypress deck and rail.
    cb._stair_to_gallery(bm, uv, fz, top, LINEN, TIMBER, deck=TIMBER)
    for x in (-3.6, -2.4):
        horns(bm, uv, x, Q0 + 0.05, fz + top + 0.9, size=0.5, along="x")
    cb._chest(bm, uv, -4.3, 4.4, fz + top, w=1.0, d=0.6, h=0.55, trim=METAL)
    # A fresco band on the east half of the north wall and on the east wall.
    ek.wall_panel(bm, uv, FRESCO, "north", 3.4, fz + 1.4, 4.1, 1.6)
    ek.wall_panel(bm, uv, RED, "north", 3.4, fz + 3.0, 4.1, 0.18)
    for along in (-3.4, 3.4):
        ek.wall_panel(bm, uv, FRESCO, "east", along, fz + 1.4, 4.1, 1.6)
        ek.wall_panel(bm, uv, RED, "east", along, fz + 3.0, 4.1, 0.18)
    # Tripod braziers in the two east corners, fire in their bowls; a stand in the SE.
    for y in (3.8, -3.8):
        tripod(bm, uv, 3.8, y, fz)
        mk.paint(bm, mk.add_cylinder(bm, 0.15, 0.30, loc=(3.8, y, fz + 1.12 + 0.15), segments=6, radius2=0.02),
                 RED, uv)
    offering_table(bm, uv, 4.6, -2.4, fz)
