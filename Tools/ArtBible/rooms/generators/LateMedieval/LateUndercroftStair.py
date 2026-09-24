"""LateUndercroftStair: the cellar stair (stands in for CryptStairwell).

The kit's stair-to-dais (castle_builders._stair_to_dais) in sandstone: four steps up
the west wall of the NW quadrant to a 1.00 m dais; on the dais the great tun, a cask
r 0.55 m lying north-south on cradles with a spigot, a pewter jug set beside it. NE:
two casks on cradles along the north wall. SE: the butler's table with a lantern,
jugs and tally sticks, a stool. SW: two upright barrels. Section A-A east-west at
y = 0, looking north: the steps face-on before the dais, the tun end-on, the NE casks.
"""
from _late import *

DAIS = 1.00
STEPS = 4
TUN = (-4.6, 4.4, 0.55, 1.4)                        # x, y, r, length (N–S)
JUG = (-3.7, 4.9)
NE_CASKS = [2.8, 4.4]
NE_Y, NE_R, NE_L = 5.0, 0.4, 1.0
TABLE = (3.6, -3.6, 1.2, 0.7, 0.8)
BARRELS = [(-4.8, -4.8), (-3.9, -4.9)]
ANCHORS = [(JUG[0], JUG[1], FZ + DAIS), (TABLE[0], TABLE[1], FZ + TABLE[4])]


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("iron", IRON), ("pewter", PEWTER), ("linen", LINEN),
            ("fire", FIRE), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "Crypt", "The Undercroft Stair", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE CELLAR, LOOKING NORTH")
    kit_glow(sh, 3.6, FZ + 1.0, FIRE, rx=220, ry=150, strength=.25)

    # ---- section ----
    kit_slab(sh, "#2A241C")
    kit_back_wall(sh, "Crypt", SAND, soot=SOOT, trim="#3A342A", soot_depth=0.8)
    ashlar_courses(sh, "Crypt")
    for x in NE_CASKS:
        cask_side(sh, x - NE_L / 2, x + NE_L / 2, FZ, r=NE_R)
    kerect(sh, -IN, FZ, -3.3, FZ + DAIS, f"url(#{sh.lin(SAND, 'v', .25, .5)})", darken(SAND, .6), 1)
    tx, ty, tr, tl = TUN
    cask_end(sh, tx, FZ + DAIS, r=tr)
    kerect(sh, tx - 0.04, FZ + DAIS + 0.12 + tr * 0.4, tx + 0.04, FZ + DAIS + 0.12 + tr * 0.6, IRON)
    jar(sh, JUG[0], FZ + DAIS, 0.28, 0.16, PEWTER, rim=0.08, lugs=False)
    for i in reversed(range(STEPS)):
        top = FZ + DAIS * (i + 1) / STEPS
        kerect(sh, -IN, FZ, -3.3, top, f"url(#{sh.lin(SAND, 'v', .2 + .05 * i, .5)})", darken(SAND, .6), .9)
        kerect(sh, -IN, top - 0.03, -3.3, top, lighten(SAND, .2))
    kit_cut_walls(sh, "Crypt")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(3.6, FZ + 0.7), "CASKS", "two on cradles along the north wall"),
    ], 610, 260, 260, slope=1.0)
    sh.callouts([
        (*KE(tx, FZ + DAIS + 0.9), "THE GREAT TUN", "r 0.55 × 1.40 m, a spigot"),
        (*KE(JUG[0], FZ + DAIS + 0.2), "PEWTER JUG", "set on the dais · loot"),
        (*KE(-4.6, FZ + 0.5), "FOUR STEPS", "0.25 m rise, north up the west wall"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "Crypt", x=4.1, text="3.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Crypt", floor="#1E1B16", wall="#3A332A", trim="#3A342A")
    run = 1.4 / STEPS
    for i in range(STEPS):
        y0 = 1.9 + i * run
        kprect(sh, -IN, y0, -3.3, y0 + run, lighten(SAND, .05 * i - .1), "#0E0C09", .6)
    kprect(sh, -IN, 3.3, -3.3, IN, lighten(SAND, .12), "#0E0C09", .8)
    plan_cask(sh, tx, ty, tl, tr, along_x=False)
    plan_disc(sh, JUG[0], JUG[1], 0.08, PEWTER)
    sh.text(*KP(-4.4, 2.2), "UP", 8, "#1E1B16", "middle")
    for x in NE_CASKS:
        plan_cask(sh, x, NE_Y, NE_L, NE_R)
    plan_box(sh, *TABLE[:4], OAK)
    plan_box(sh, TABLE[0] + 0.35, TABLE[1], 0.16, 0.16, "#6A6E74")
    plan_disc(sh, TABLE[0], -2.95, 0.18, OAK)
    for x, y in BARRELS:
        plan_disc(sh, x, y, 0.4, OAK)
        plan_disc(sh, x, y, 0.32, None, IRON)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Crypt")
    socket_label(sh, *KP(3.6, -2.4), "BUTLER'S TABLE")
    socket_label(sh, *KP(-4.35, -3.9), "BARRELS")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.16 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("STAIR", "NW, 4 steps north up the west wall to the dais"),
                    ("CELLAR", "great tun on the dais · casks NE · barrels SW"),
                    ("LOOT", "L1–L2: the dais jug, the butler's table")])
    return sh
