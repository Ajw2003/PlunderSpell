"""LateJewelHouse: the jewel house (stands in for TreasuryVault).

The household's plate, where it can be counted. NE: a stepped buffet of three oak
tiers against the north wall under a verdure dosser, gilt cups, dishes and a ewer
set out on the steps. NW: a tall oak strong cupboard behind a lattice of iron
straps. SE and SW: iron-bound chests against brick-lined walls. An iron candle
stand by the buffet. Section A-A east-west at y = 0, looking north: the buffet,
the cupboard and the candle stand; the chests are south of the cut (plan).
"""
from _late import *

STEPS = [(0.9, 0.9), (0.6, 0.45), (0.3, 0.45)]             # (depth from the wall, height) per tier
BUF_X, BUF_W = 3.6, 2.4
CUP = (-3.6, 1.6, 0.6, 2.2)                               # cupboard: x, w, d, h
CHESTS = [(3.4, -5.0), (-3.4, -5.0)]
JTABLE = (4.4, -2.9, 0.9, 0.6, 0.8)                      # the jeweller's table, SE
SACKS = [(-4.8, -3.6), (-4.4, -3.2), (-5.0, -3.1)]
ANCHORS = [(BUF_X, IN - 0.75, FZ + 0.9), (BUF_X, IN - 0.45, FZ + 1.35), (3.4, -5.0, FZ + 0.75), (-3.4, -5.0, FZ + 0.75),
           (JTABLE[0], JTABLE[1], FZ + JTABLE[4])]


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("iron", IRON), ("gilt", GOLD), ("brick", BRICK),
            ("tapestry", WOOL), ("linen", LINEN)]
    sh = room_sheet("LateMedieval", "Keep", "The Jewel House", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE HOUSE, LOOKING NORTH")
    kit_glow(sh, 2.0, FZ + 1.8, "#8A6A30", rx=300, ry=200, strength=.35)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "Keep", SAND, soot=SOOT, trim=ESTATE, soot_depth=0.5)
    ashlar_courses(sh, "Keep")
    # NE: the dosser above the buffet, then the three tiers with their plate.
    top = FZ + sum(h for _, h in STEPS)
    tapestry(sh, BUF_X - 1.3, BUF_X + 1.3, top, FZ + 3.4)
    z = FZ
    for k, (_, h) in enumerate(STEPS):
        kerect(sh, BUF_X - BUF_W / 2, z, BUF_X + BUF_W / 2, z + h, f"url(#{sh.lin(OAK, 'v', .2 + .05 * k, .5)})",
               darken(OAK, .6), 1)
        kerect(sh, BUF_X - BUF_W / 2, z + h - 0.04, BUF_X + BUF_W / 2, z + h, lighten(OAK, .15))
        z += h
    z1, z2, z3 = FZ + 0.9, FZ + 1.35, FZ + 1.8
    for dx in (-0.8, -0.2, 0.5):                                          # cups on the first tier
        kerect(sh, BUF_X + dx - 0.05, z1, BUF_X + dx + 0.05, z1 + 0.16, GOLD, darken(GOLD, .5), .6)
    for dx in (-0.6, 0.6):                                                # dishes standing on the second
        sh.circle(*KE(BUF_X + dx, z2 + 0.15), 0.15 * SK, GOLD, darken(GOLD, .5), .8)
        sh.circle(*KE(BUF_X + dx, z2 + 0.15), 0.08 * SK, "none", darken(GOLD, .35), .8)
    ewer = [KE(BUF_X - 0.06, z3), KE(BUF_X + 0.06, z3), KE(BUF_X + 0.1, z3 + 0.16), KE(BUF_X + 0.04, z3 + 0.3),
            KE(BUF_X + 0.12, z3 + 0.34), KE(BUF_X - 0.06, z3 + 0.34), KE(BUF_X - 0.1, z3 + 0.16)]
    sh.path(poly_path(ewer), GOLD, darken(GOLD, .5), .8)
    for dx in (-0.7, 0.7):
        kerect(sh, BUF_X + dx - 0.05, z3, BUF_X + dx + 0.05, z3 + 0.14, GOLD, darken(GOLD, .5), .6)
    candle_stand(sh, 1.95)
    # NW: the strong cupboard behind its iron lattice.
    cx, w, d, h = CUP
    kerect(sh, cx - w / 2, FZ, cx + w / 2, FZ + h, f"url(#{sh.lin(OAK, 'h', .25, .5)})", darken(OAK, .6), 1)
    sh.line(*KE(cx, FZ + 0.1), *KE(cx, FZ + h - 0.1), darken(OAK, .5), 1.2)
    for dx in (-0.6, -0.2, 0.2, 0.6):
        kerect(sh, cx + dx - 0.02, FZ + 0.1, cx + dx + 0.02, FZ + h - 0.1, IRON)
    for dz in (0.4, 0.9, 1.4, 1.9):
        kerect(sh, cx - 0.75, FZ + dz - 0.02, cx + 0.75, FZ + dz + 0.02, IRON)
    kerect(sh, cx + 0.05, FZ + 1.05, cx + 0.2, FZ + 1.25, lighten(IRON, .3), "#0E0C09", .5)
    kit_cut_walls(sh, "Keep")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(BUF_X + 0.9, FZ + 1.1), "STEPPED BUFFET", "3 oak tiers, 2.40 m · loot"),
        (*KE(BUF_X, z3 + 0.25), "GILT PLATE", "cups, standing dishes, a ewer"),
        (*KE(BUF_X - 1.0, FZ + 2.8), "VERDURE DOSSER", "2.60 × 1.60 m over the buffet"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(cx - 0.4, FZ + 1.6), "STRONG CUPBOARD", "1.60 × 0.60 × 2.20 m oak"),
        (*KE(cx + 0.6, FZ + 0.9), "IRON LATTICE", "straps 0.04 m, a lock plate"),
    ], 330, 220, 300, anchor="end")
    kit_clear_note(sh, "Keep", x=0.0, text="4.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Keep", floor="#2A2016", wall="#3A332A", trim=ESTATE)
    for k, (dep, _) in enumerate(STEPS):
        plan_box(sh, BUF_X, IN - dep / 2, BUF_W, dep, lighten(OAK, .08 * k))
    for dx in (-0.8, -0.2, 0.5):
        plan_disc(sh, BUF_X + dx, IN - 0.75, 0.05, GOLD)
    kprect(sh, BUF_X - 1.3, IN - 0.06, BUF_X + 1.3, IN, WOOL)
    plan_box(sh, cx, IN - d / 2, w, d, OAK)
    for dx in (-0.6, -0.2, 0.2, 0.6):
        kprect(sh, cx + dx - 0.02, IN - d - 0.02, cx + dx + 0.02, IN - d, IRON)
    plan_box(sh, *JTABLE[:4], OAK)
    plan_box(sh, JTABLE[0] + 0.25, JTABLE[1], 0.3, 0.2, GOLD)
    for x, y in SACKS:
        plan_disc(sh, x, y, 0.2, "#5A4630")
    for x, y in CHESTS:
        kprect(sh, x - 1.7, -IN, x + 1.7, -IN + 0.06, BRICK)
        plan_box(sh, x, y, 1.2, 0.7, OAK)
        for dx in (-0.45, 0, 0.45):
            kprect(sh, x + dx - 0.03, y - 0.35, x + dx + 0.03, y + 0.35, IRON)
    plan_disc(sh, 1.95, 4.0, 0.2, IRON)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Keep")
    socket_label(sh, *KP(3.4, -4.2), "BRICK-LINED")
    socket_label(sh, *KP(-3.4, -4.2), "BRICK-LINED")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 3.31 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("PLATE", "NE stepped buffet, gilt on every tier"),
                    ("LOCKED", "NW strong cupboard · SE, SW iron-bound chests"),
                    ("SE / SW", "jeweller's table and casket · coin sacks"),
                    ("LOOT", "L1–L5: buffet tiers, chests, the table")])
    return sh
