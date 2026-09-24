"""LateChantryChapel: the chantry chapel (stands in for ChapelRoom).

NE: a sandstone altar against the north wall under a linen cloth and a madder
frontal, a gilded triptych standing open on it between two candlesticks. NW: the
founder's tomb chest, his recumbent effigy on its lid, head on a pillow, feet on a
lion. SE: a parclose screen of oak closing off the corner as a private chantry, a
solid dado under open mullions, its entrance facing the crossing, a lectern inside.
SW: a prie-dieu before a devotional panel on the west wall between two candle
stands. Section A-A east-west at y = 0, looking north.
"""
from _late import *

ALTAR = (3.6, 5.1, 2.0, 0.8, 1.0)
TRIP = (3.6, 0.9, 1.1)                          # triptych centre x, centre panel width, height
TOMB = (-3.6, 4.6, 2.1, 0.9, 0.8)
SCREEN_Y, SCREEN_X = -2.3, 2.0                  # the parclose's north run and west run
DOOR = (2.0, 2.8)                               # its entrance, on the north run
PRIE = (-4.6, -3.6)
PANEL_Y = -3.6
STANDS = [(-4.8, -2.6), (-4.8, -4.6)]
ANCHORS = [(ALTAR[0], ALTAR[1] - 0.2, FZ + ALTAR[4]), (TOMB[0] + 0.85, TOMB[1], FZ + TOMB[4])]


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("gilt", GOLD), ("madder", ESTATE), ("linen", LINEN),
            ("alabaster", "#D8D0BE"), ("iron", IRON)]
    sh = room_sheet("LateMedieval", "InnerWard", "The Chantry Chapel", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE CHAPEL, LOOKING NORTH")
    kit_glow(sh, 3.6, FZ + 1.6, "#8A6A30", rx=260, ry=200, strength=.35)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "InnerWard", SAND, soot=SOOT, trim=WOOL, soot_depth=0.4)
    ashlar_courses(sh, "InnerWard")
    # NE: altar, frontal, cloth, triptych, candlesticks.
    ax, ay, aw, ad, ah = ALTAR
    kerect(sh, ax - aw / 2, FZ, ax + aw / 2, FZ + ah, f"url(#{sh.lin(SAND, 'v', .25, .5)})", darken(SAND, .6), 1)
    kerect(sh, ax - aw / 2 + 0.1, FZ + 0.1, ax + aw / 2 - 0.1, FZ + ah - 0.12, f"url(#{sh.lin(ESTATE, 'h', .3, .5)})",
           darken(ESTATE, .5), .7)
    kerect(sh, ax - aw / 2 - 0.03, FZ + ah - 0.12, ax + aw / 2 + 0.03, FZ + ah, LINEN, darken(LINEN, .5), .6)
    tx, tw, th = TRIP
    t0 = FZ + ah
    for x0, x1, top in ((tx - tw / 2 - tw / 2, tx - tw / 2, th * .8), (tx - tw / 2, tx + tw / 2, th),
                        (tx + tw / 2, tx + tw, th * .8)):
        kerect(sh, x0, t0, x1, t0 + top, GOLD, darken(GOLD, .5), .8)
        kerect(sh, x0 + 0.05, t0 + 0.05, x1 - 0.05, t0 + top - 0.05, f"url(#{sh.lin(darken(WOOL, .1), 'v', .3, .5)})")
    cx = tx
    sh.circle(*KE(cx, t0 + 0.72), 0.13 * SK, GOLD, op=.8)                           # a halo
    kerect(sh, cx - 0.1, t0 + 0.1, cx + 0.1, t0 + 0.62, ESTATE, op=.9)
    for dx in (-0.85, 0.85):
        kerect(sh, ax + dx - 0.06, t0, ax + dx + 0.06, t0 + 0.04, GOLD)
        kerect(sh, ax + dx - 0.02, t0 + 0.04, ax + dx + 0.02, t0 + 0.35, GOLD)
        kerect(sh, ax + dx - 0.025, t0 + 0.35, ax + dx + 0.025, t0 + 0.55, LINEN)
        sh.ellipse(*KE(ax + dx, t0 + 0.59), 2.2, 4, FIRE)
    # NW: the tomb chest side-on, the effigy on it.
    mx, my, mw, md, mh = TOMB
    kerect(sh, mx - mw / 2, FZ, mx + mw / 2, FZ + mh, f"url(#{sh.lin(SAND, 'v', .25, .5)})", darken(SAND, .6), 1)
    for k in range(4):
        x0 = mx - mw / 2 + 0.15 + k * 0.47
        kerect(sh, x0, FZ + 0.15, x0 + 0.35, FZ + mh - 0.15, "none", darken(SAND, .35), .8)
        sh.path(f"M{f(KE(x0, FZ + mh - 0.3)[0])} {f(KE(0, FZ + mh - 0.3)[1])} q{f(0.175 * SK)} -12 {f(0.35 * SK)} 0",
                "none", darken(SAND, .35), .8)
    alab = "#D8D0BE"
    kerect(sh, mx - 0.85, FZ + mh, mx + 0.75, FZ + mh + 0.2, f"url(#{sh.lin(alab, 'v', .3, .5)})", darken(alab, .5), .8)
    kerect(sh, mx + 0.75, FZ + mh, mx + 0.95, FZ + mh + 0.12, alab, darken(alab, .5), .7)          # pillow
    sh.circle(*KE(mx + 0.85, FZ + mh + 0.22), 0.11 * SK, alab, darken(alab, .5), .8)
    sh.path(poly_path([KE(mx - 0.2, FZ + mh + 0.2), KE(mx - 0.1, FZ + mh + 0.36), KE(mx, FZ + mh + 0.2)]), alab,
            darken(alab, .5), .7)                                                            # the hands at prayer
    sh.ellipse(*KE(mx - 0.95, FZ + mh + 0.12), 0.12 * SK, 0.1 * SK, darken(alab, .15), darken(alab, .5), .7)
    kit_cut_walls(sh, "InnerWard")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(tx + 0.3, t0 + 0.9), "GILDED TRIPTYCH", "standing open, wings 0.45 m"),
        (*KE(ax + 0.6, FZ + 0.6), "ALTAR", "sandstone, madder frontal · loot"),
        (*KE(ax + 0.85, t0 + 0.3), "CANDLESTICKS", "gilt, one each side"),
    ], 610, 190, 330, slope=1.0)
    sh.callouts([
        (*KE(mx + 0.1, FZ + mh + 0.2), "RECUMBENT EFFIGY", "alabaster, hands at prayer"),
        (*KE(mx - 0.4, FZ + 0.4), "TOMB CHEST", "2.10 × 0.90 × 0.80 m, arcaded · loot"),
    ], 330, 220, 300, anchor="end")
    kit_clear_note(sh, "InnerWard", x=0.0, text="4.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "InnerWard", floor="#3A3226", wall="#3A332A", trim=WOOL)
    plan_box(sh, ax, ay, aw, ad, SAND)
    plan_box(sh, ax, IN - 0.1, tw * 2, 0.1, GOLD)
    plan_box(sh, mx, my, mw, md, SAND)
    plan_box(sh, mx - 0.05, my, 1.6, 0.45, "#D8D0BE")
    kprect(sh, DOOR[1], SCREEN_Y - 0.04, IN, SCREEN_Y + 0.04, OAK)
    kprect(sh, SCREEN_X - 0.04, -IN, SCREEN_X + 0.04, SCREEN_Y + 0.04, OAK)
    for k in range(9):
        plan_disc(sh, DOOR[1] + 0.15 + k * 0.3, SCREEN_Y, 0.03, darken(OAK, .4))
    for k in range(10):
        plan_disc(sh, SCREEN_X, SCREEN_Y - 0.15 - k * 0.3, 0.03, darken(OAK, .4))
    plan_box(sh, 3.9, -4.0, 0.5, 0.4, OAK)
    plan_box(sh, *PRIE, 0.35, 0.6, OAK)
    kprect(sh, -IN, PANEL_Y - 0.5, -IN + 0.08, PANEL_Y + 0.5, GOLD)
    for x, y in STANDS:
        plan_disc(sh, x, y, 0.2, IRON)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "InnerWard")
    socket_label(sh, *KP(3.9, -3.1), "PARCLOSE")
    socket_label(sh, *KP(-3.6, -2.2), "PRIE-DIEU")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.88 · all open"),
                    ("CLEAR CROSS", "dashed · the parclose entrance faces it"),
                    ("NE / NW", "altar and triptych · founder's tomb and effigy"),
                    ("SE / SW", "parclose chantry, lectern · prie-dieu, panel"),
                    ("LOOT", "L1–L2: altar, tomb chest")])
    return sh
