"""LateOubliette: the dungeon (stands in for TombCorridor).

Four cells, one in each quadrant, against the east and west walls and running back
to the north or south wall: a sandstone partition 0.30 m thick on each cell's inner
side (x ±3.50 … ±3.80), and a barred front facing the crossing at |y| 2.20, iron
bars at 0.18 m under an iron lintel, a 0.70 m doorway in it, its door gone. Inside,
a plank shelf against the outer wall with the prisoners' goods, and straw. Stocks
stand before the NW cell, shackles hang on the north wall by the archway, and in the
SE a flat iron grate covers the oubliette. Section A-A east-west at y = 0, looking
north: the north cells' fronts face-on, the partitions end-on, the stocks.
"""
from _late import *

CELL_X, PART_T, CELL_Y0 = 3.5, 0.3, 2.2
BAR_H = 2.3
DOOR = (4.25, 4.95)                                # |x| span of each front's doorway
SHELF = (IN - 0.225, 3.9, 0.45, 1.2, 0.55)         # |x|, |y|, depth, length, height
STOCKS = (-2.6, 3.4, 1.6)                          # x, y, width
SHACKLES = [(2.0, 1.4), (2.6, 1.3), (-2.2, 1.4)]
GRATE = (2.6, -3.4, 1.2)
ANCHORS = [(STOCKS[0], STOCKS[1] - 0.35, FZ + 0.44), (SHELF[0], SHELF[1], FZ + SHELF[4]),
           (-SHELF[0], -SHELF[1], FZ + SHELF[4])]


def bar_xs(sgn):
    """Bar positions along a front from the partition to the wall, skipping the doorway."""
    xs, x = [], CELL_X + PART_T + 0.12
    while x < IN - 0.05:
        if not (DOOR[0] - 0.02 < x < DOOR[1] + 0.02):
            xs.append(sgn * x)
        x += 0.18
    return xs


def build():
    mats = [("sandstone", SAND), ("iron", IRON), ("oak", OAK), ("straw", "#B98A34"), ("soot", SOOT),
            ("damp", "#3A4A50")]
    sh = room_sheet("LateMedieval", "Crypt", "The Oubliette", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE DUNGEON, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 1.0, "#3A3A30", rx=300, ry=150, strength=.3)

    # ---- section ----
    kit_slab(sh, "#2A241C")
    kit_back_wall(sh, "Crypt", SAND, soot=SOOT, trim="#3A342A", soot_depth=1.0)
    ashlar_courses(sh, "Crypt")
    kerect(sh, -IN, FZ, IN, FZ + 0.5, "#3A4A50", op=.25)                       # damp tide mark
    # The north cells' fronts face-on: dark interior, the shelf and straw seen through the bars,
    # the lintel, the door gap; the partitions end-on.
    for sgn in (-1, 1):
        a, b = sorted((sgn * (CELL_X + PART_T), sgn * IN))
        kerect(sh, a, FZ, b, FZ + BAR_H, "#14120E", op=.55)
        s0, s1 = sorted((sgn * (IN - SHELF[2]), sgn * IN))
        kerect(sh, s0, FZ + SHELF[4] - 0.05, s1, FZ + SHELF[4], OAK, darken(OAK, .6), .6)
        kerect(sh, a + 0.1, FZ, b - 0.1, FZ + 0.12, "#B98A34", op=.7)
        for x in bar_xs(sgn):
            kerect(sh, x - 0.02, FZ, x + 0.02, FZ + BAR_H, IRON)
        for z in (0.3, 1.2, 2.0):
            for u0, u1 in ((CELL_X + PART_T, DOOR[0]), (DOOR[1], IN)):
                c0, c1 = sorted((sgn * u0, sgn * u1))
                kerect(sh, c0, FZ + z - 0.02, c1, FZ + z + 0.02, IRON)
        kerect(sh, a - 0.05, FZ + BAR_H, b + 0.05, FZ + BAR_H + 0.15, IRON, "#0E0C09", .6)
        p0, p1 = sorted((sgn * CELL_X, sgn * (CELL_X + PART_T)))
        kerect(sh, p0, FZ, p1, FZ + BAR_H + 0.15, f"url(#{sh.lin(SAND, 'h', .25, .5)})", darken(SAND, .6), .9)
    # The stocks, face-on in front of the NW cell.
    sx, sy, sw = STOCKS
    for dx in (-sw / 2 + 0.06, sw / 2 - 0.06):
        kerect(sh, sx + dx - 0.06, FZ, sx + dx + 0.06, FZ + 0.75, OAK, darken(OAK, .6), .7)
    kerect(sh, sx - sw / 2, FZ + 0.42, sx + sw / 2, FZ + 0.62, f"url(#{sh.lin(OAK, 'v', .25, .5)})", darken(OAK, .6), .8)
    for dx in (-0.25, 0.25):
        sh.circle(*KE(sx + dx, FZ + 0.52), 0.06 * SK, "#14120E")
    kerect(sh, sx - sw / 2 + 0.1, FZ, sx + sw / 2 - 0.1, FZ + 0.42, "none", darken(OAK, .3), .6)
    # Shackles on the north wall.
    for x, z in SHACKLES:
        sh.circle(*KE(x, FZ + z + 0.3), 3, IRON)
        sh.path(f"M{f(KE(x, FZ + z + 0.3)[0])} {f(KE(0, FZ + z + 0.3)[1])} q-6 14 0 22 q6 -8 0 -22", "none", IRON, 1.4)
    kit_cut_walls(sh, "Crypt")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(5.2, FZ + 1.6), "BARRED CELL", "1.70 × 3.30 m, bars at 0.18 m"),
        (*KE(4.6, FZ + 0.9), "DOORWAY", "0.70 m, the door gone"),
        (*KE(4.9, FZ + SHELF[2]), "PLANK SHELF", "inside, prisoners' goods · loot"),
        (*KE(2.3, FZ + 1.7), "SHACKLES", "iron rings on the north wall"),
    ], 610, 180, 340, slope=1.0)
    sh.callouts([
        (*KE(sx, FZ + 0.55), "STOCKS", "oak, 1.60 m, two holes · loot"),
        (*KE(-4.5, FZ + BAR_H + 0.08), "IRON LINTEL", "over each cell front"),
    ], 330, 220, 300, anchor="end")
    kit_clear_note(sh, "Crypt", x=0.0, text="3.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Crypt", floor="#1E1B16", wall="#3A332A", trim="#3A342A")
    for sxn in (-1, 1):
        for syn in (-1, 1):
            a, b = sorted((sxn * (CELL_X + PART_T), sxn * IN))
            y0, y1 = sorted((syn * CELL_Y0, syn * IN))
            kprect(sh, a, y0, b, y1, "#161310")
            p0, p1 = sorted((sxn * CELL_X, sxn * (CELL_X + PART_T)))
            kprect(sh, p0, y0, p1, y1, SAND, "#0E0C09", .5)
            fy = syn * CELL_Y0
            for u0, u1 in ((CELL_X + PART_T, DOOR[0]), (DOOR[1], IN)):
                c0, c1 = sorted((sxn * u0, sxn * u1))
                kprect(sh, c0, fy - 0.04, c1, fy + 0.04, IRON)
            s0, s1 = sorted((sxn * (IN - SHELF[2]), sxn * IN))
            kprect(sh, s0, syn * SHELF[1] - SHELF[3] / 2, s1, syn * SHELF[1] + SHELF[3] / 2, OAK)
    plan_box(sh, sx, sy, sw, 0.3, OAK)
    plan_box(sh, sx, sy - 0.35, sw - 0.2, 0.3, darken(OAK, .2))
    for x, _ in SHACKLES:
        kprect(sh, x - 0.05, IN - 0.08, x + 0.05, IN, IRON)
    gx, gy, gs = GRATE
    plan_box(sh, gx, gy, gs, gs, "#14120E")
    for k in range(1, 6):
        kprect(sh, gx - gs / 2 + k * gs / 6 - 0.02, gy - gs / 2, gx - gs / 2 + k * gs / 6 + 0.02, gy + gs / 2, IRON)
    kprect(sh, gx - gs / 2, gy - gs / 2, gx + gs / 2, gy + gs / 2, "none", IRON, 1.5)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Crypt")
    socket_label(sh, *KP(3.0, -2.1), "OUBLIETTE GRATE")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.16 · all open"),
                    ("CLEAR CROSS", "dashed · the cells stop at x ±3.50, y ±2.20"),
                    ("CELLS", "4, stone partitions, barred fronts facing the cross"),
                    ("ROOM", "stocks NW · shackles N wall · grate SE (flat)"),
                    ("LOOT", "L1–L3: stocks bench, two cell shelves")])
    return sh
