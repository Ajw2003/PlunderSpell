"""LateTapestrySolar: the lord's solar (stands in for LordsSolar).

The private room above the hall. NE: a writing desk with a sloped top, a stool, and a
shelf of books on the north wall over it. NW: a hooded hearth on the west wall and
a high-backed oak settle facing it. SE: a livery cupboard against the east wall
with pewter on it. SW: a verdure tapestry on the south wall and an iron-bound chest
before it. A window on the east wall. Section A-A east-west at y = 0, looking north:
the desk and shelf face-on, the hearth in profile on the west wall, the settle end-on.
"""
from _late import *

DESK = (3.6, 4.9, 1.2, 0.6, 0.75)
STOOL = (3.6, 4.2)
HEARTH_Y = 3.4
SETTLE = (-3.35, 3.4, 0.5, 1.6)                  # x, y, depth (E–W), length (N–S)
CUPBOARD = (5.2, -3.6, 0.6, 1.4, 1.3)            # x, y, depth, length, height
CHEST = (-3.6, -4.9, 1.2, 0.6, 0.6)
ANCHORS = [(DESK[0], DESK[1], FZ + DESK[4] + 0.08), (CHEST[0], CHEST[1], FZ + CHEST[4] + 0.05),
           (CUPBOARD[0], CUPBOARD[1], FZ + CUPBOARD[4])]


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("tapestry", WOOL), ("madder", ESTATE), ("linen", LINEN),
            ("iron", IRON), ("pewter", PEWTER)]
    sh = room_sheet("LateMedieval", "Keep", "The Tapestry Solar", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE SOLAR, LOOKING NORTH")
    kit_glow(sh, -4.8, FZ + 0.6, FIRE, rx=240, ry=180, strength=.35)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "Keep", SAND, soot=SOOT, trim=ESTATE, soot_depth=0.5)
    ashlar_courses(sh, "Keep")
    tapestry(sh, -5.0, -1.9, FZ + 1.0, FZ + 3.6)                           # the north tapestry, over the settle
    # NE: the book shelf, the desk and its sloped top, the stool in front.
    dx0, dx1 = DESK[0] - DESK[2] / 2, DESK[0] + DESK[2] / 2
    kerect(sh, dx0, FZ + 1.7, dx1, FZ + 1.74, lighten(OAK, .1), darken(OAK, .6), .6)
    xx, k = dx0 + 0.05, 0
    while k < 8:
        w = 0.07 + (k % 3) * 0.02
        hh = 0.24 + (k % 4) * 0.03
        c = (ESTATE, OAK, darken(WOOL, .1), darken(LINEN, .3))[k % 4]
        kerect(sh, xx, FZ + 1.74, xx + w, FZ + 1.74 + hh, c, darken(c, .5), .4)
        xx += w + 0.04
        k += 1
    kerect(sh, dx0, FZ, dx1, FZ + DESK[4], f"url(#{sh.lin(OAK, 'v', .25, .5)})", darken(OAK, .6), 1)
    kerect(sh, dx0 - 0.03, FZ + DESK[4], dx1 + 0.03, FZ + DESK[4] + 0.15, lighten(OAK, .08), darken(OAK, .6), .8)
    kerect(sh, DESK[0] - 0.2, FZ + DESK[4] + 0.15, DESK[0] + 0.1, FZ + DESK[4] + 0.19, LINEN)
    kerect(sh, STOOL[0] - 0.2, FZ, STOOL[0] + 0.2, FZ + 0.45, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .8)
    # NW: the hearth in profile on the west wall, the settle end-on facing it.
    x_face = -IN + 0.8
    prof = [KE(-IN, FZ), KE(x_face, FZ), KE(x_face, FZ + 1.2), KE(x_face + 0.1, FZ + 1.2), KE(x_face + 0.1, FZ + 1.45),
            KE(-IN + 0.3, FZ + 3.8), KE(-IN, FZ + 3.8)]
    sh.path(poly_path(prof), f"url(#{sh.lin(SAND, 'h', .25, .5)})", darken(SAND, .6), 1)
    fx, fy = KE(x_face - 0.15, FZ)
    sh.path(f"M{f(fx - 12)} {f(fy)} q4 -24 12 -30 q0 12 7 18 q4 -9 2 -16 q9 12 3 28 z", FIRE, op=.9)
    sx = SETTLE[0]
    kerect(sh, sx - 0.25, FZ, sx + 0.25, FZ + 0.45, f"url(#{sh.lin(OAK, 'v', .25, .5)})", darken(OAK, .6), .9)
    kerect(sh, sx + 0.21, FZ, sx + 0.29, FZ + 1.35, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .9)
    kerect(sh, sx - 0.25, FZ + 0.45, sx + 0.21, FZ + 0.52, ESTATE)                      # a cushion
    kit_cut_walls(sh, "Keep")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(DESK[0] + 0.4, FZ + 0.9), "SLOPED DESK", "1.20 × 0.60 m oak · loot"),
        (*KE(DESK[0] - 0.3, FZ + 1.85), "BOOK SHELF", "on the north wall, over the desk"),
    ], 610, 230, 300, slope=1.0)
    sh.callouts([
        (*KE(-2.6, FZ + 3.2), "NORTH TAPESTRY", "3.10 × 2.60 m verdure"),
        (*KE(-4.9, FZ + 2.6), "HEARTH HOOD", "west wall, NW quadrant"),
        (*KE(sx + 0.25, FZ + 1.1), "HIGH-BACKED SETTLE", "1.60 m, facing the fire"),
    ], 330, 200, 320, anchor="end")
    kit_clear_note(sh, "Keep", x=0.0, text="4.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Keep", floor="#2A2016", wall="#3A332A", trim=ESTATE)
    kprect(sh, -5.0, IN - 0.06, -1.9, IN, WOOL)
    plan_box(sh, *DESK[:4], OAK)
    kprect(sh, dx0, IN - 0.25, dx1, IN, lighten(OAK, .1), "#0E0C09", .5)
    plan_box(sh, *STOOL, 0.4, 0.4, OAK)
    plan_box(sh, -IN + 0.4, HEARTH_Y, 0.8, 1.7, SAND)
    plan_box(sh, -IN + 0.25, HEARTH_Y, 0.4, 0.9, "#0E0C09")
    plan_disc(sh, -IN + 0.3, HEARTH_Y, 0.15, FIRE, None)
    plan_box(sh, SETTLE[0], SETTLE[1], SETTLE[2], SETTLE[3], OAK)
    kprect(sh, SETTLE[0] + 0.21, SETTLE[1] - 0.8, SETTLE[0] + 0.29, SETTLE[1] + 0.8, darken(OAK, .3))
    plan_box(sh, CUPBOARD[0], CUPBOARD[1], CUPBOARD[2], CUPBOARD[3], OAK)
    for dy in (-0.35, 0.3):
        plan_disc(sh, CUPBOARD[0], CUPBOARD[1] + dy, 0.12, PEWTER)
    kprect(sh, -5.3, -IN, -1.8, -IN + 0.06, WOOL)
    plan_box(sh, *CHEST[:4], OAK)
    kprect(sh, IN - 0.1, 1.9, IN, 3.3, "#2A3238", "#0E0C09", .6)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Keep")
    socket_label(sh, *KP(-3.5, 2.2), "HEARTH + SETTLE")
    socket_label(sh, *KP(-3.5, -3.9), "TAPESTRY")
    socket_label(sh, *KP(4.1, -2.4), "CUPBOARD")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 3.31 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("NE / NW", "sloped desk and book shelf · hearth and settle"),
                    ("SE / SW", "livery cupboard · tapestry and chest"),
                    ("TAPESTRIES", "north wall NW, south wall SW"),
                    ("LOOT", "L1–L3: desk, chest, cupboard")])
    return sh
