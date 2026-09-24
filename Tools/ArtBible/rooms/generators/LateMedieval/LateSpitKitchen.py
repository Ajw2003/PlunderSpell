"""LateSpitKitchen: the spit kitchen (stands in for KitchenRoom).

NW: a great hooded hearth on the west wall, 2.40 m wide, a long iron spit on two
tall firedogs across its mouth with a boar on it, the fire under. NE: a dresser
against the north wall, a cupboard base under two open shelves of pewter. SE: a
chopping block and the work table with a cleaver and loaves. SW: a flour bin with a
sloped lid against the west wall and two hooped barrels. Section A-A east-west at
y = 0, looking north: the hearth in profile with the spit end-on, the dresser.
"""
from _late import *

HEARTH = (3.6, 2.4, 1.0, 1.5, 3.5)                 # centre y, width, depth, mouth, hood top
SPIT_Z = 0.7
DRESSER = (3.6, 2.0, 0.5, 0.9)                     # x, width, depth, base height
SHELVES = (1.4, 1.8)
BLOCK = (4.6, -4.3, 0.35, 0.8)
TABLE = (3.0, -3.6, 1.8, 0.8, 0.85)
BIN = (-5.1, -4.0, 0.7, 1.2, 0.8)
BARRELS = [(-3.3, -4.9), (-2.4, -4.95)]
ANCHORS = [(TABLE[0], TABLE[1], FZ + TABLE[4]), (DRESSER[0], IN - 0.35, FZ + DRESSER[3])]


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("iron", IRON), ("pewter", PEWTER), ("fire", FIRE),
            ("flour", LINEN), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "InnerWard", "The Spit Kitchen", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE KITCHEN, LOOKING NORTH")
    kit_glow(sh, -4.6, FZ + 0.8, FIRE, rx=320, ry=220, strength=.5)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "InnerWard", SAND, soot=SOOT, trim=WOOL, soot_depth=1.4)
    ashlar_courses(sh, "InnerWard")
    # NW: the hearth in profile, the spit end-on on its firedog, the boar, the fire.
    hy, hw, hd, mouth, hood = HEARTH
    x_face = -IN + hd
    prof = [KE(-IN, FZ), KE(x_face, FZ), KE(x_face, FZ + mouth), KE(x_face + 0.1, FZ + mouth),
            KE(x_face + 0.1, FZ + mouth + 0.25), KE(-IN + 0.3, FZ + hood), KE(-IN, FZ + hood)]
    sh.path(poly_path(prof), f"url(#{sh.lin(SAND, 'h', .25, .5)})", darken(SAND, .6), 1)
    sh.path(poly_path([KE(x_face + 0.1, FZ + mouth + 0.25), KE(-IN + 0.3, FZ + hood), KE(-IN + 0.15, FZ + hood),
                       KE(x_face - 0.1, FZ + mouth + 0.3)]), SOOT, op=.5)
    sx = -IN + 0.5
    kerect(sh, sx - 0.03, FZ, sx + 0.03, FZ + SPIT_Z + 0.08, IRON)
    sh.ellipse(*KE(sx, FZ + SPIT_Z), 0.2 * SK, 0.2 * SK, "#6A3A22", darken("#6A3A22", .5), .8)
    sh.circle(*KE(sx, FZ + SPIT_Z), 2.5, IRON)
    fx, fy = KE(sx, FZ)
    sh.path(f"M{f(fx - 18)} {f(fy)} q5 -22 18 -26 q-2 12 8 16 q4 -9 2 -14 q10 12 4 24 z", FIRE, op=.9)
    # NE: the dresser.
    dx, dw, dd, dh = DRESSER
    kerect(sh, dx - dw / 2, FZ, dx + dw / 2, FZ + dh, f"url(#{sh.lin(OAK, 'v', .25, .5)})", darken(OAK, .6), 1)
    sh.line(*KE(dx, FZ + 0.05), *KE(dx, FZ + dh - 0.05), darken(OAK, .5), 1)
    kerect(sh, dx - dw / 2, FZ + dh, dx + dw / 2, FZ + 2.1, darken(OAK, .25), darken(OAK, .6), .8)
    for z in SHELVES:
        kerect(sh, dx - dw / 2, FZ + z - 0.03, dx + dw / 2, FZ + z, lighten(OAK, .1), darken(OAK, .6), .6)
        for k in range(5):
            cx = dx - 0.8 + k * 0.4
            sh.circle(*KE(cx, FZ + z + 0.15), 0.14 * SK, PEWTER, darken(PEWTER, .5), .7)
    for k in range(3):
        cx = dx - 0.6 + k * 0.6
        jar(sh, cx, FZ + dh, 0.26, 0.16, PEWTER, rim=0.08, lugs=False)
    kit_cut_walls(sh, "InnerWard")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(dx + 0.6, FZ + 1.55), "DRESSER", "pewter plates on two shelves"),
        (*KE(dx - 0.4, FZ + dh - 0.2), "CUPBOARD BASE", "0.90 m, jugs on top · loot"),
    ], 610, 230, 310, slope=1.0)
    sh.callouts([
        (*KE(-5.0, FZ + 2.8), "HEARTH HOOD", "2.40 m wide, 1.00 m deep"),
        (*KE(sx + 0.15, FZ + SPIT_Z + 0.1), "SPIT AND BOAR", "iron spit on tall firedogs"),
    ], 330, 220, 300, anchor="end")
    kit_clear_note(sh, "InnerWard", x=0.0, text="4.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "InnerWard", floor="#3A3226", wall="#3A332A", trim=WOOL)
    plan_box(sh, -IN + hd / 2, hy, hd, hw + 0.4, SAND)
    plan_box(sh, -IN + 0.4, hy, 0.6, hw - 0.4, "#0E0C09")
    kprect(sh, sx - 0.02, hy - 1.0, sx + 0.02, hy + 1.0, IRON)
    plan_box(sh, sx, hy, 0.36, 0.9, "#6A3A22")
    for dy in (-0.9, 0.9):
        plan_disc(sh, sx, hy + dy, 0.05, IRON)
    kprect(sh, dx - dw / 2, IN - dd, dx + dw / 2, IN, OAK, "#0E0C09", .7)
    plan_disc(sh, BLOCK[0], BLOCK[1], BLOCK[2], "#7A5A3A")
    plan_box(sh, *TABLE[:4], OAK)
    plan_box(sh, TABLE[0] - 0.5, TABLE[1], 0.3, 0.2, "#C8A870")
    plan_box(sh, *BIN[:4], OAK)
    for x, y in BARRELS:
        plan_disc(sh, x, y, 0.4, OAK)
        plan_disc(sh, x, y, 0.32, None, IRON)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "InnerWard")
    socket_label(sh, *KP(-3.9, 2.0), "HEARTH + SPIT")
    socket_label(sh, *KP(3.6, -2.6), "WORK TABLE")
    socket_label(sh, *KP(-3.6, -3.6), "FLOUR + BARRELS")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.88 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("NW / NE", "hearth with spit and boar · pewter dresser"),
                    ("SE / SW", "chopping block, table · flour bin, barrels"),
                    ("LOOT", "L1–L2: work table, dresser")])
    return sh
