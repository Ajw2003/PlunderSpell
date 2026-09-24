"""LateStateBedchamber: the state bedchamber (stands in for RoyalBedchamber).

NW: a tester bed, its head to the north wall under a cloth dosser, four oak posts
carrying the tester, curtains gathered at the two posts on the room side, and an
iron-bound chest at its foot. NE: a verdure tapestry and a prie-dieu before it. SE:
a table with ewer and basin, a close-stool in the corner. SW: a small hooded hearth
on the west wall. A window on the east wall. Section A-A east-west at y = 0, looking
north: the bed end-on with its chest in front, the tapestry and prie-dieu.
"""
from _late import *

BED = (-3.8, 4.3, 1.8, 2.2)                               # x, y, w (E–W), l (N–S)
POST_H = 2.75
CHEST = (-3.8, 2.8, 1.2, 0.5, 0.55)
PRIE = (3.6, 4.8)
TABLE = (3.4, -3.4, 1.0, 0.7, 0.75)
STOOL = (4.9, -4.9)
HEARTH_Y = -3.2
ANCHORS = [(BED[0], 4.1, FZ + 0.55), (CHEST[0], CHEST[1], FZ + CHEST[4] + 0.05), (TABLE[0], TABLE[1], FZ + TABLE[4])]


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("hangings", ESTATE), ("tapestry", WOOL), ("linen", LINEN),
            ("iron", IRON), ("pewter", PEWTER)]
    sh = room_sheet("LateMedieval", "Keep", "The State Bedchamber", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE CHAMBER, LOOKING NORTH")
    kit_glow(sh, -3.8, FZ + 1.4, "#6A3A2A", rx=260, ry=200, strength=.3)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "Keep", SAND, soot=SOOT, trim=ESTATE, soot_depth=0.5)
    ashlar_courses(sh, "Keep")
    x0, x1 = BED[0] - BED[2] / 2, BED[0] + BED[2] / 2
    # NW: dosser, tester, far posts, the bed end-on, near posts and curtains, the chest in front.
    kerect(sh, x0, FZ + 0.45, x1, FZ + POST_H, f"url(#{sh.lin(ESTATE, 'h', .3, .5)})", darken(ESTATE, .5), .8)
    for k in range(5):
        x = x0 + 0.2 + k * (BED[2] - 0.4) / 4
        sh.line(*KE(x, FZ + 0.5), *KE(x, FZ + POST_H - 0.05), darken(ESTATE, .3), .8, op=.6)
    kerect(sh, x0, FZ, x1, FZ + 0.45, f"url(#{sh.lin(OAK, 'v', .25, .5)})", darken(OAK, .6), 1)
    kerect(sh, x0 - 0.02, FZ + 0.45, x1 + 0.02, FZ + 0.55, lighten(ESTATE, .1), darken(ESTATE, .5), .8)
    kerect(sh, x0 + 0.15, FZ + 0.55, x1 - 0.15, FZ + 0.67, LINEN, darken(LINEN, .5), .6)
    for x in (x0 + 0.05, x1 - 0.05):
        kerect(sh, x - 0.05, FZ, x + 0.05, FZ + POST_H, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .8)
    kerect(sh, x0 - 0.05, FZ + POST_H, x1 + 0.05, FZ + POST_H + 0.15, darken(ESTATE, .1), darken(ESTATE, .5), .9)
    for k in range(9):
        x = x0 - 0.05 + k * (BED[2] + 0.1) / 8
        sh.line(*KE(x, FZ + POST_H), *KE(x, FZ + POST_H - 0.1), GOLD, 1.2)
    curtain = [KE(x1 - 0.02, FZ + POST_H), KE(x1 + 0.2, FZ + POST_H), KE(x1 + 0.28, FZ + 1.4), KE(x1 + 0.16, FZ + 0.5),
               KE(x1 - 0.02, FZ + 0.5)]
    sh.path(smooth_path(curtain, tension=.25), f"url(#{sh.lin(ESTATE, 'h', .35, .5)})", darken(ESTATE, .5), .8)
    chest(sh, CHEST[0], CHEST[2], CHEST[4])
    # NE: tapestry and prie-dieu.
    tapestry(sh, 2.0, 5.3, FZ + 0.9, FZ + 3.6)
    px = PRIE[0]
    kerect(sh, px - 0.3, FZ, px + 0.3, FZ + 0.85, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .9)
    kerect(sh, px - 0.33, FZ + 0.85, px + 0.33, FZ + 0.97, lighten(OAK, .1), darken(OAK, .6), .7)
    kerect(sh, px - 0.12, FZ + 0.97, px + 0.12, FZ + 1.0, ESTATE)
    kerect(sh, px - 0.3, FZ, px + 0.3, FZ + 0.15, ESTATE, darken(ESTATE, .5), .6)
    kit_cut_walls(sh, "Keep")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(3.4, FZ + 3.0), "VERDURE TAPESTRY", "3.30 × 2.70 m, north wall"),
        (*KE(px, FZ + 0.9), "PRIE-DIEU", "oak desk, cushioned kneeler"),
    ], 610, 230, 310, slope=1.0)
    sh.callouts([
        (*KE(BED[0], FZ + POST_H + 0.08), "TESTER", "on four oak posts, gilt fringe"),
        (*KE(x1 + 0.2, FZ + 1.6), "BED CURTAINS", "gathered at the two room-side posts"),
        (*KE(BED[0] + 0.5, FZ + 0.5), "BED", "1.80 × 2.20 m, coverlet · loot"),
        (*KE(CHEST[0] - 0.4, FZ + 0.3), "CHEST AT THE FOOT", "iron-bound oak · loot"),
    ], 330, 180, 330, anchor="end")
    kit_clear_note(sh, "Keep", x=0.0, text="4.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Keep", floor="#2A2016", wall="#3A332A", trim=ESTATE)
    kprect(sh, x0, BED[1] - BED[3] / 2, x1, BED[1] + BED[3] / 2, OAK, "#0E0C09", .8)
    kprect(sh, x0 - 0.02, BED[1] - BED[3] / 2, x1 + 0.02, BED[1] + BED[3] / 2 - 0.2, ESTATE, "#0E0C09", .6)
    plan_box(sh, BED[0], 5.0, 1.5, 0.35, LINEN)
    kprect(sh, x0 - 0.05, BED[1] - BED[3] / 2 - 0.05, x1 + 0.05, BED[1] + BED[3] / 2 + 0.05, "none", GOLD, .9)
    for y in (3.55, 5.05):
        kprect(sh, x1 + 0.02, y - 0.3, x1 + 0.08, y + 0.3, ESTATE)
    kprect(sh, x0, IN - 0.06, x1, IN, ESTATE)
    plan_box(sh, CHEST[0], CHEST[1], CHEST[2], CHEST[3], OAK)
    kprect(sh, 2.0, IN - 0.06, 5.3, IN, WOOL)
    plan_box(sh, px, 4.95, 0.6, 0.35, OAK)
    plan_box(sh, px, 4.5, 0.6, 0.3, ESTATE)
    plan_box(sh, *TABLE[:4], OAK)
    plan_disc(sh, TABLE[0] + 0.2, TABLE[1], 0.18, PEWTER)
    plan_box(sh, *STOOL, 0.5, 0.45, OAK)
    plan_box(sh, -IN + 0.35, HEARTH_Y, 0.7, 1.7, SAND)
    plan_box(sh, -IN + 0.25, HEARTH_Y, 0.4, 0.9, "#0E0C09")
    plan_disc(sh, -IN + 0.3, HEARTH_Y, 0.15, FIRE, None)
    kprect(sh, IN - 0.1, 2.3, IN, 3.7, "#2A3238", "#0E0C09", .6)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Keep")
    socket_label(sh, *KP(-3.6, -2.1), "HEARTH")
    socket_label(sh, *KP(4.1, -4.1), "CLOSE-STOOL")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 3.31 · all open"),
                    ("CLEAR CROSS", "dashed · the bed stops at x −2.90, y 3.20"),
                    ("BED", "NW tester bed, curtains, chest at its foot"),
                    ("ROOM", "prie-dieu NE · table SE · hearth SW"),
                    ("LOOT", "L1–L3: bed, chest, table")])
    return sh
