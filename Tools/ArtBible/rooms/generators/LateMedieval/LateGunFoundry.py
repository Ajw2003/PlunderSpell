"""LateGunFoundry: the gun foundry (stands in for BlacksmithShop).

NW: a brick furnace in the corner, its fire mouth glowing on the south face, a
brick chimney stack rising from its back to the wall top, leather bellows on its
east side and a heap of charcoal before it. NE: the casting pit, a low stone kerb
round a dark fill, a clay gun mould standing upright in it under an A-frame crane,
its chain on the mould's head. SE: two gun barrels across a pair of trestles, a
tool plank beside them. SW: an anvil on its stump, a quench tub, a rack of tongs.
Section A-A east-west at y = 0, looking north.
"""
from _late import *

FURN = (-4.6, 4.7, 1.8, 1.6, 1.4)                 # x, y, w, d, h
CHIM = (-4.6, 5.15, 0.9, 0.7)
PIT = (3.4, 3.8, 1.6)                              # centre, outer side
MOULD_R, MOULD_H = 0.3, 1.8
CRANE_X, CRANE_TOP = (2.4, 4.4), 2.8
TRESTLE_X, TRESTLE_Y = (2.6, 4.4), -3.8
BARRELS = [(-4.1, 0.15), (-3.7, 0.18)]            # y, radius
PLANK = (3.5, -3.3, 2.2, 0.3)
ANVIL = (-3.4, -3.4)
TUB = (-4.7, -4.5, 0.45)
ANCHORS = [(PLANK[0], PLANK[1], FZ + 0.84), (ANVIL[0], ANVIL[1], FZ + 0.85)]
CLAY = "#8A5E3C"


def build():
    mats = [("sandstone", SAND), ("brick", BRICK), ("iron", IRON), ("oak", OAK), ("clay", CLAY),
            ("fire", FIRE), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "OuterBailey", "The Gun Foundry", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE FOUNDRY, LOOKING NORTH")
    kit_glow(sh, -4.6, FZ + 0.6, FIRE, rx=320, ry=220, strength=.55)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "OuterBailey", SAND, soot=SOOT, trim=BRICK, soot_depth=1.6)
    ashlar_courses(sh, "OuterBailey")
    # NW: chimney, furnace face with its mouth, bellows, charcoal.
    fx, fy, fw, fd, fh = FURN
    cx, cy, cw, cd = CHIM
    kerect(sh, cx - cw / 2, FZ + fh, cx + cw / 2, FZ + 3.6, f"url(#{sh.lin(BRICK, 'h', .25, .5)})", darken(BRICK, .6), 1)
    kerect(sh, fx - fw / 2, FZ, fx + fw / 2, FZ + fh, f"url(#{sh.lin(BRICK, 'v', .25, .5)})", darken(BRICK, .6), 1)
    for k in range(1, 7):
        sh.line(*KE(fx - fw / 2, FZ + k * 0.2), *KE(fx + fw / 2, FZ + k * 0.2), darken(BRICK, .35), .5, op=.6)
    mouth = poly_path([KE(fx - 0.35, FZ + 0.3), KE(fx + 0.35, FZ + 0.3), KE(fx + 0.35, FZ + 0.75), KE(fx, FZ + 0.9),
                       KE(fx - 0.35, FZ + 0.75)])
    sh.path(mouth, FIRE, darken(FIRE, .5), .8)
    kerect(sh, fx - 0.25, FZ + 0.3, fx + 0.25, FZ + 0.45, "#F0A050")
    bel = poly_path([KE(-3.7, FZ + 0.55), KE(-3.1, FZ + 0.75), KE(-3.1, FZ + 0.95), KE(-3.7, FZ + 0.75)])
    sh.path(bel, f"url(#{sh.lin('#5A4630', 'v', .3, .5)})", "#0E0C09", .8)
    kerect(sh, -3.7, FZ, -3.62, FZ + 0.6, OAK)
    sh.line(*KE(-3.1, FZ + 0.95), *KE(-2.8, FZ + 1.25), OAK, 2.5)
    # NE: A-frames end-on, crossbeam, chain, the mould standing in the pit, the kerb front.
    px, py, ps = PIT
    for x in CRANE_X:
        kerect(sh, x - 0.06, FZ, x + 0.06, FZ + CRANE_TOP, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .8)
    kerect(sh, CRANE_X[0] - 0.15, FZ + CRANE_TOP - 0.1, CRANE_X[1] + 0.15, FZ + CRANE_TOP + 0.08,
           f"url(#{sh.lin(OAK, 'v', .3, .5)})", darken(OAK, .6), .8)
    sh.line(*KE(px, FZ + CRANE_TOP - 0.1), *KE(px, FZ + MOULD_H + 0.05), IRON, 1.6)
    kerect(sh, px - MOULD_R, FZ, px + MOULD_R, FZ + MOULD_H, f"url(#{sh.lin(CLAY, 'h', .35, .55)})", darken(CLAY, .6), 1)
    for k in range(1, 6):
        sh.line(*KE(px - MOULD_R, FZ + k * 0.3), *KE(px + MOULD_R, FZ + k * 0.3), IRON, 1.2)
    kerect(sh, px - ps / 2, FZ, px + ps / 2, FZ + 0.25, f"url(#{sh.lin(SAND, 'v', .25, .5)})", darken(SAND, .6), .9)
    kit_cut_walls(sh, "OuterBailey")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(4.4, FZ + 2.4), "A-FRAME CRANE", "oak, beam at 2.80 m, chain"),
        (*KE(px + 0.2, FZ + 1.2), "CLAY GUN MOULD", "1.80 m, iron-banded, upright"),
        (*KE(px + 0.6, FZ + 0.12), "CASTING PIT", "stone kerb 1.60 m, 0.25 m high"),
    ], 610, 190, 330, slope=1.0)
    sh.callouts([
        (*KE(cx, FZ + 2.8), "CHIMNEY", "brick stack to the wall top"),
        (*KE(fx + 0.2, FZ + 0.6), "FURNACE", "brick 1.80 × 1.60 × 1.40 m"),
        (*KE(-3.4, FZ + 0.8), "BELLOWS", "leather, on the furnace's east side"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "OuterBailey", x=0.0, text="3.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "OuterBailey", floor="#302A20", wall="#3A332A", trim=BRICK)
    plan_box(sh, fx, fy, fw, fd, BRICK)
    plan_box(sh, cx, cy, cw, cd, darken(BRICK, .25))
    plan_box(sh, fx, fy - fd / 2 + 0.05, 0.7, 0.1, FIRE)
    plan_box(sh, -3.4, 4.6, 0.6, 0.4, "#5A4630")
    plan_disc(sh, -4.6, 3.3, 0.4, SOOT, None)
    plan_box(sh, px, py, ps, ps, SAND)
    plan_box(sh, px, py, ps - 0.4, ps - 0.4, "#1A1612")
    plan_disc(sh, px, py, MOULD_R, CLAY)
    for x in CRANE_X:
        kprect(sh, x - 0.06, py - 0.8, x + 0.06, py + 0.8, OAK)
    kprect(sh, CRANE_X[0], py - 0.05, CRANE_X[1], py + 0.05, darken(OAK, .2))
    for x in TRESTLE_X:
        plan_box(sh, x, TRESTLE_Y, 0.15, 1.2, OAK)
    for y, r in BARRELS:
        plan_box(sh, 3.5, y, 2.4, 2 * r, IRON)
    plan_box(sh, *PLANK, lighten(OAK, .1))
    plan_disc(sh, ANVIL[0], ANVIL[1], 0.3, OAK)
    plan_box(sh, ANVIL[0], ANVIL[1], 0.5, 0.18, IRON)
    plan_disc(sh, TUB[0], TUB[1], TUB[2], OAK)
    plan_disc(sh, TUB[0], TUB[1], TUB[2] - 0.05, "#3A4A50", None)
    kprect(sh, -IN, -3.0, -IN + 0.1, -2.0, IRON)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "OuterBailey")
    socket_label(sh, *KP(3.5, -2.6), "BARRELS ON TRESTLES")
    socket_label(sh, *KP(-3.8, -2.4), "ANVIL + TUB")
    socket_label(sh, *KP(-4.4, 2.6), "CHARCOAL")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.59 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("NW / NE", "furnace, chimney, bellows · casting pit, crane"),
                    ("SE / SW", "barrels on trestles · anvil, tub, tongs"),
                    ("LOOT", "L1–L2: trestle plank, anvil")])
    return sh
