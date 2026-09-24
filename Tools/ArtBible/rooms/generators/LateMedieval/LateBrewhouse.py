"""LateBrewhouse: the brewhouse (stands in for StorehouseRoom).

NW: a round-shouldered brick furnace in the corner, its fire mouth to the south, a
copper kettle set into its top and a short brick flue at the back. NE: the open mash
tun, a great hooped oak tub with the mash in it and a paddle standing in the mash;
a cooling trough on legs along the east wall. SE and SW: ale casks lying on their
sides on oak cradles along the south wall, three and two; malt sacks stacked in the
SW corner. Section A-A east-west at y = 0, looking north.
"""
from _late import *

COPPER = "#B87333"
FURN = (-4.5, 4.5, 1.6, 0.8)                        # x, y, side, height
KETTLE_R, KETTLE_H = 0.6, 0.7
TUN = (3.6, 4.1, 0.8, 0.9)                          # x, y, r, h
TROUGH = (5.05, 1.8 + 1.1, 0.7, 2.0, 0.35, 0.6)     # x, y, w, l, depth, legs
CASKS_SE = [2.6, 3.6, 4.6]
CASKS_SW = [-2.6, -3.6]
CASK_Y, CASK_R, CASK_L = -4.7, 0.4, 1.0
CRADLE_H = 0.2
SACKS = [(-4.9, -4.9, 0.0), (-4.9, -4.3, 0.0), (-4.9, -4.6, 0.36)]
MASH = "#5A4630"
CASK_TOP = FZ + CRADLE_H + 2 * CASK_R - 0.05
ANCHORS = [(CASKS_SE[1], CASK_Y, CASK_TOP), (CASKS_SE[2], CASK_Y, CASK_TOP), (CASKS_SW[1], CASK_Y, CASK_TOP)]


def build():
    mats = [("sandstone", SAND), ("brick", BRICK), ("copper", COPPER), ("oak", OAK), ("iron", IRON),
            ("mash", MASH), ("sacking", LINEN)]
    sh = room_sheet("LateMedieval", "OuterBailey", "The Brewhouse", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE BREWHOUSE, LOOKING NORTH")
    kit_glow(sh, -4.5, FZ + 0.5, FIRE, rx=260, ry=180, strength=.4)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "OuterBailey", SAND, soot=SOOT, trim=BRICK, soot_depth=0.8)
    ashlar_courses(sh, "OuterBailey")
    # NW: flue, furnace, kettle.
    fx, fy, fs, fh = FURN
    kerect(sh, fx - 0.3, FZ + fh, fx + 0.3, FZ + 2.4, f"url(#{sh.lin(BRICK, 'h', .25, .5)})", darken(BRICK, .6), .9)
    kerect(sh, fx - fs / 2, FZ, fx + fs / 2, FZ + fh, f"url(#{sh.lin(BRICK, 'v', .25, .5)})", darken(BRICK, .6), 1)
    for k in range(1, 4):
        sh.line(*KE(fx - fs / 2, FZ + k * 0.2), *KE(fx + fs / 2, FZ + k * 0.2), darken(BRICK, .35), .5, op=.6)
    kerect(sh, fx - 0.3, FZ + 0.1, fx + 0.3, FZ + 0.45, FIRE, darken(FIRE, .5), .7)
    kett = [KE(fx - KETTLE_R, FZ + fh + 0.45), KE(fx - KETTLE_R * .9, FZ + fh - 0.1), KE(fx + KETTLE_R * .9, FZ + fh - 0.1),
            KE(fx + KETTLE_R, FZ + fh + 0.45)]
    sh.path(poly_path(kett), f"url(#{sh.lin(COPPER, 'h', .35, .6)})", darken(COPPER, .6), 1)
    kerect(sh, fx - KETTLE_R - 0.04, FZ + fh + 0.45, fx + KETTLE_R + 0.04, FZ + fh + 0.5, lighten(COPPER, .15),
           darken(COPPER, .6), .7)
    # NE: the trough on the east wall end-on, then the tun and its paddle.
    tx, ty, tw, tl, td, tlg = TROUGH
    kerect(sh, tx - tw / 2, FZ + tlg, tx + tw / 2, FZ + tlg + td, OAK, darken(OAK, .6), .8)
    for dx in (-tw / 2 + 0.05, tw / 2 - 0.1):
        kerect(sh, tx + dx, FZ, tx + dx + 0.05, FZ + tlg, darken(OAK, .2))
    ux, uy, ur, uh = TUN
    kerect(sh, ux - ur, FZ, ux + ur, FZ + uh, f"url(#{sh.lin(OAK, 'h', .3, .55)})", darken(OAK, .6), 1)
    for z in (0.15, 0.75):
        kerect(sh, ux - ur - 0.02, FZ + z - 0.03, ux + ur + 0.02, FZ + z + 0.03, IRON)
    sh.line(*KE(ux + 0.3, FZ + uh - 0.2), *KE(ux + 0.55, FZ + uh + 0.9), OAK, 3)
    kit_cut_walls(sh, "OuterBailey")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(ux, FZ + 0.5), "MASH TUN", "open oak tub r 0.80 m, hooped"),
        (*KE(ux + 0.5, FZ + uh + 0.7), "MASH PADDLE", "standing in the mash"),
        (*KE(tx, FZ + tlg + 0.2), "COOLING TROUGH", "on legs, along the east wall"),
    ], 610, 190, 330, slope=1.0)
    sh.callouts([
        (*KE(fx, FZ + fh + 0.3), "COPPER KETTLE", "r 0.60 m, set into the furnace"),
        (*KE(fx - 0.5, FZ + 0.6), "BRICK FURNACE", "1.60 m square, fire mouth S"),
    ], 330, 220, 300, anchor="end")
    kit_clear_note(sh, "OuterBailey", x=0.0, text="3.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "OuterBailey", floor="#302A20", wall="#3A332A", trim=BRICK)
    plan_box(sh, fx, fy, fs, fs, BRICK)
    plan_disc(sh, fx, fy, KETTLE_R, COPPER)
    plan_disc(sh, fx, fy, KETTLE_R - 0.08, darken(COPPER, .4), None)
    plan_box(sh, fx - 0.4, fy + 0.5, 0.6, 0.5, darken(BRICK, .25))
    plan_box(sh, fx, fy - fs / 2 + 0.05, 0.6, 0.1, FIRE)
    plan_disc(sh, ux, uy, ur, OAK)
    plan_disc(sh, ux, uy, ur - 0.06, MASH, None)
    plan_box(sh, tx, ty, tw, tl, OAK)
    for x in CASKS_SE + CASKS_SW:
        plan_cask(sh, x, CASK_Y, CASK_L, CASK_R, along_x=False)
    for x, y, z in SACKS:
        plan_disc(sh, x, y, 0.28, lighten(LINEN, -.1 + z * .3))
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "OuterBailey")
    socket_label(sh, *KP(3.6, -3.7), "ALE CASKS")
    socket_label(sh, *KP(-3.8, -3.7), "CASKS + MALT")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.59 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("NW / NE", "copper on a brick furnace · mash tun, trough"),
                    ("SOUTH", "ale casks on cradles · malt sacks SW"),
                    ("LOOT", "L1–L3: three cask tops")])
    return sh
