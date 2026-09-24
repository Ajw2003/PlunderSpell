"""BronzeTreasury: the wanax's treasure room (stands in for TreasuryVault).

Section A-A east-west at y = 0, looking north: the faience shelf and a tripod on
the west side, the death-mask on its stepped plinth in the NE corner. The plan
shows the two ingot stacks and the bronze-bound chests to the south.
"""
from _bronze import *

ANCHORS = [(4.3, 4.3, 1.20), (4.0, -4.3, 0.66), (-3.9, -2.9, 0.60), (-4.6, -4.9, 0.90),
           (-2.9, -4.9, 0.90), (-3.7, 5.2, 1.78), (-2.4, 2.6, 1.44), (2.6, 4.6, 1.44),
           (4.2, -2.3, 0.75), (5.0, -2.3, 0.75)]


def build():
    mats = [("plaster", OCHRE), ("haematite", RED), ("bronze", BRONZE), ("cypress", CYP),
            ("faience", BLUE), ("gypsum", GYP), ("gold", GOLD)]
    sh = room_sheet("BronzeAge", "Keep", "The Treasury", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE ROOM, LOOKING NORTH")
    kit_glow(sh, 3.0, FZ + 1.4, "#8A6A2A", rx=360, ry=230, strength=.45)

    # ---- section ----
    kit_slab(sh, FLOOR)
    kit_back_wall(sh, "Keep", OCHRE, trim=RED)
    # A haematite dado round the walls: the room's only paint. Treasure needs no frescoes.
    for x0, x1 in ((-IN, -1.3), (1.3, IN)):
        kerect(sh, x0, FZ, x1, FZ + 0.40, f"url(#{sh.lin(RED, 'v', .2, .45)})", darken(RED, .6), .6)
    # NW: the faience shelf against the north wall, three boards, blue pieces on each.
    x0, x1 = -5.2, -2.2
    for x in (x0, x1 - 0.10):
        kerect(sh, x, FZ, x + 0.10, FZ + 2.15, f"url(#{sh.lin(CYP, 'h', .25, .5)})", darken(CYP, .6), .6)
    for i, z in enumerate((0.35, 0.90, 1.45)):
        kerect(sh, x0, FZ + z - 0.03, x1, FZ + z + 0.03, CYP, darken(CYP, .6), .6)
        for k in range(5):
            px = x0 + 0.35 + k * 0.55 + (i % 2) * 0.2
            if k % 2:
                jar(sh, px, FZ + z + 0.03, 0.26, 0.16, BLUE, lugs=False)
            else:
                sh.path(f"M{f(KE(px - .12, FZ + z + .03)[0])} {f(KE(0, FZ + z + .03)[1])} "
                        f"q{f(.12 * SK)} {f(-.34 * SK)} {f(.24 * SK)} 0 z", BLUE, darken(BLUE, .5), .6)   # faience figure
    # A bronze tripod on each side, clear of the walkway (the east one guards the plinth).
    tripod(sh, -2.4)
    tripod(sh, 2.6)
    # NE: the stepped gypsum plinth and the gold death-mask on it.
    kerect(sh, 3.6, FZ, 5.0, FZ + 0.50, f"url(#{sh.lin(GYP, 'v', .2, .45)})", darken(GYP, .6))
    kerect(sh, 3.9, FZ + 0.50, 4.7, FZ + 0.90, f"url(#{sh.lin(GYP, 'v', .25, .45)})", darken(GYP, .6))
    mx, my = KE(4.3, FZ + 1.10)
    sh.ellipse(mx, my, 0.14 * SK, 0.20 * SK, f"url(#{sh.lin(GOLD, 'h', .45, .4)})", darken(GOLD, .5), 1)
    for dx in (-0.05, 0.05):
        ex, ey = KE(4.3 + dx, FZ + 1.14)
        sh.path(f"M{f(ex - 3)} {f(ey)} q3 -2 6 0", "none", darken(GOLD, .6), 1)
    sh.line(*KE(4.26, FZ + 1.02), *KE(4.34, FZ + 1.02), darken(GOLD, .6), 1)
    g = sh.rad([(0, GOLD, .45), (1, GOLD, 0)])
    sh.back.append(f'<circle cx="{f(mx)}" cy="{f(my)}" r="60" fill="url(#{g})"/>')
    kit_cut_walls(sh, "Keep")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(4.3, FZ + 1.1), "GOLD DEATH-MASK", "the room's prize · loot"),
        (*KE(3.8, FZ + 0.3), "STEPPED PLINTH", "gypsum, 1.40 then 0.80 m"),
    ], 610, 220, 300, slope=1.0)
    sh.callouts([
        (*KE(-3.7, FZ + 1.5), "FAIENCE SHELF", "3 boards · figures, flasks"),
        (*KE(-2.4, FZ + 1.05), "BRONZE TRIPODS", "bowl 0.60 m, one each side · loot"),
        (*KE(-4.8, FZ + 0.2), "HAEMATITE DADO", "0.40 m, the only paint"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "Keep", x=1.2)

    # ---- plan ----
    kit_plan(sh, "Keep", floor="#2E2719", wall="#3A332A", trim=RED)
    plan_box(sh, 4.3, 4.3, 1.4, 1.4, GYP)
    plan_box(sh, 4.3, 4.3, 0.8, 0.8, lighten(GYP, .15))
    sh.ellipse(*KP(4.3, 4.3), 0.14 * PK, 0.18 * PK, GOLD, darken(GOLD, .5), .8)
    for (x, y, w, d, layers) in ((4.0, -4.3, 1.8, 1.2, 4), (-3.9, -2.9, 1.2, 0.9, 3)):
        plan_box(sh, x, y, w, d, darken(CYP, .1))
        for j in range(int(w / 0.62)):
            for k in range(int(d / 0.42)):
                plan_box(sh, x - w / 2 + 0.33 + j * 0.62, y - d / 2 + 0.24 + k * 0.42, 0.56, 0.36, BRONZE)
    for x in (-4.6, -2.9):
        plan_box(sh, x, -4.9, 1.0, 0.6, CYP)
        kprect(sh, x - 0.5, -4.93, x + 0.5, -4.87, BRONZE)
    plan_box(sh, -3.7, 5.2, 3.0, 0.45, darken(CYP, .15))
    for k in range(5):
        sh.circle(*KP(-5.0 + k * 0.6, 5.2), 0.08 * PK, BLUE)
    for x, y in ((-2.4, 2.6), (2.6, 4.6)):
        sh.circle(*KP(x, y), 0.30 * PK, BRONZE, "#0E0C09", .8)
    for x in (4.2, 5.0):                       # bronze cauldrons, SE
        sh.circle(*KP(x, -2.3), 0.34 * PK, darken(BRONZE, .15), "#0E0C09", .8)
        sh.circle(*KP(x, -2.3), 0.24 * PK, SOOT)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Keep")
    socket_label(sh, *KP(4.1, 2.9), "DEATH-MASK")
    socket_label(sh, *KP(3.6, -3.0), "INGOTS")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 3.31 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("TREASURE", "death-mask NE · ingots, cauldrons SE · chests S"),
                    ("SHELF", "faience on three boards, NW"),
                    ("LOOT", "L1–L10: plinth, stacks, chests, shelf, tripods, cauldrons")])
    return sh
