"""LateLibrary: the library (stands in for GreatHallMain, the household hall).

Four tall oak book presses, two against the north wall and two against the south,
either side of the archways, each four shelves of books. Before each press a sloped
lectern desk with a book chained to its rail, one in every quadrant. NE: a reading
table against the east wall with a candle and a book laid open. Section A-A
east-west at y = 0, looking north: the two north presses face-on, the lecterns in
front of them side-on, the reading table end-on at the east wall.
"""
from _late import *

PRESS_X = (2.0, 5.3)                               # each press spans |x| from..to
PRESS_D, PRESS_H = 0.45, 2.6
LECTERN_X, LECTERN_Y, LECTERN_W, LECTERN_D = 3.2, 3.5, 1.2, 0.6
READ = (4.9, 2.9, 0.8, 1.6, 0.8)                   # x, y, w (E–W), l (N–S), h
ANCHORS = [(LECTERN_X, LECTERN_Y, FZ + 0.95), (-LECTERN_X, LECTERN_Y, FZ + 0.95),
           (LECTERN_X, -LECTERN_Y, FZ + 0.95), (-LECTERN_X, -LECTERN_Y, FZ + 0.95), (READ[0], READ[1], FZ + READ[4])]


def lectern_side(sh, x, w, base=FZ, col=OAK):
    """A sloped lectern desk seen side-on from the south: a carcass, the sloped top, a
    chained book on it."""
    kerect(sh, x - w / 2, base, x + w / 2, base + 0.85, f"url(#{sh.lin(col, 'v', .25, .5)})", darken(col, .6), .9)
    kerect(sh, x - w / 2 - 0.03, base + 0.85, x + w / 2 + 0.03, base + 1.0, lighten(col, .08), darken(col, .6), .7)
    kerect(sh, x - 0.2, base + 1.0, x + 0.2, base + 1.05, ESTATE, darken(ESTATE, .5), .5)
    sh.path(f"M{f(KE(x + 0.2, base + 1.02)[0])} {f(KE(0, base + 1.02)[1])} q8 8 18 2", "none", IRON, 1.2)


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("bindings", ESTATE), ("vellum", LINEN), ("iron", IRON),
            ("tapestry", WOOL), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "InnerWard", "The Library", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE LIBRARY, LOOKING NORTH")
    kit_glow(sh, 4.9, FZ + 1.3, "#8A6A30", rx=220, ry=160, strength=.3)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "InnerWard", SAND, soot=SOOT, trim=WOOL, soot_depth=0.4)
    ashlar_courses(sh, "InnerWard")
    for sgn in (-1, 1):
        a, b = sorted((sgn * PRESS_X[0], sgn * PRESS_X[1]))
        press(sh, a, b, h=PRESS_H, tiers=4, seed=5 if sgn > 0 else 11)
    for sgn in (-1, 1):
        lectern_side(sh, sgn * LECTERN_X, LECTERN_W)
    rx, ry, rw, rl, rh = READ
    table(sh, rx, rw, rh)
    kerect(sh, rx - 0.3, FZ + rh, rx - 0.05, FZ + rh + 0.03, LINEN)
    candle_stand(sh, rx + 0.2, base=FZ + rh, h=0.2)
    kit_cut_walls(sh, "InnerWard")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(4.2, FZ + 2.3), "BOOK PRESS", "oak, 3.30 × 0.45 × 2.60 m, 4 shelves"),
        (*KE(LECTERN_X + 0.3, FZ + 1.03), "CHAINED BOOK", "on a sloped lectern · loot"),
        (*KE(rx, FZ + rh - 0.05), "READING TABLE", "east wall, candle · loot"),
    ], 610, 190, 330, slope=1.0)
    sh.callouts([
        (*KE(-LECTERN_X, FZ + 0.5), "LECTERN DESK", "1.20 × 0.60 m, one per quadrant"),
        (*KE(-3.0, FZ + 1.4), "BINDINGS", "madder, green and vellum"),
    ], 330, 230, 300, anchor="end")
    kit_clear_note(sh, "InnerWard", x=0.0, text="4.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "InnerWard", floor="#3A3226", wall="#3A332A", trim=WOOL)
    for sx in (-1, 1):
        for sy in (-1, 1):
            a, b = sorted((sx * PRESS_X[0], sx * PRESS_X[1]))
            y0, y1 = sorted((sy * IN, sy * (IN - PRESS_D)))
            kprect(sh, a, y0, b, y1, OAK, "#0E0C09", .7)
            plan_box(sh, sx * LECTERN_X, sy * LECTERN_Y, LECTERN_W, LECTERN_D, lighten(OAK, .08))
            plan_box(sh, sx * LECTERN_X, sy * LECTERN_Y, 0.4, 0.3, ESTATE)
    plan_box(sh, rx, ry, rw, rl, OAK)
    plan_box(sh, rx, ry + 0.3, 0.3, 0.25, LINEN)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "InnerWard")
    socket_label(sh, *KP(3.6, 4.55), "PRESS")
    socket_label(sh, *KP(-3.6, 4.55), "PRESS")
    socket_label(sh, *KP(3.6, -4.6), "PRESS")
    socket_label(sh, *KP(-3.6, -4.6), "PRESS")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.88 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("PRESSES", "four, N and S walls either side of the archways"),
                    ("LECTERNS", "one per quadrant, a chained book on each"),
                    ("LOOT", "L1–L5: four lecterns, the reading table")])
    return sh
