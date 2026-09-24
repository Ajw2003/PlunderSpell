"""BronzeQueensHall: the queen's hall (stands in for RoyalBedchamber).

Section A-A east-west at y = 0, looking north: the bed against the west wall, the
warp-weighted loom against the north wall, dolphins painted between them. The
hearth, chest and dressing table in the south half show in the plan.
"""
from _bronze import *

ANCHORS = [(-4.6, 3.9, 0.92), (3.7, 4.2, 0.75), (-4.5, -4.9, 0.90), (-2.6, -4.8, 1.05), (5.25, -2.6, 0.70)]


def dolphin(sh, x, y, s, col, facing=1):
    """A leaping dolphin silhouette, nose at (x, y) px."""
    pts = [(0, 0), (-14, -8), (-40, -10), (-66, -4), (-80, 6), (-92, 0), (-88, 12), (-74, 14), (-56, 8), (-36, 8),
           (-26, 16), (-22, 8), (-10, 6)]
    sh.path(smooth_path([(x + facing * px * s, y + py * s) for px, py in pts], tension=.35), col, darken(col, .5), .8, op=.95)
    sh.path(smooth_path([(x + facing * px * s, y + py * s) for px, py in [(-6, 2), (-30, 4), (-60, 2)]], closed=False,
                        tension=.4), "none", lighten(col, .45), 1.2, op=.8)
    sh.circle(x + facing * -8 * s, y - 2 * s, 1.2, "#14120E")


def build():
    mats = [("plaster", PLAST), ("fresco", BLUE), ("haematite", RED), ("cypress", CYP),
            ("linen", LINEN), ("wool", "#6E4A3A"), ("clay", TERRA)]
    sh = room_sheet("BronzeAge", "Keep", "The Queen's Hall", mats, "≤ 1.8k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE ROOM, LOOKING NORTH")
    kit_glow(sh, 4.2, FZ + 0.6, MADDER_, rx=260, ry=180, strength=.35)

    # ---- section ----
    kit_slab(sh, FLOOR)
    kit_back_wall(sh, "Keep", PLAST, trim=RED)
    # Dolphin frescoes on the north wall: a sea band either side of the archway.
    for x0, x1 in ((-IN, -1.3), (1.3, IN)):
        kerect(sh, x0, FZ + 1.6, x1, FZ + 3.2, mix(BLUE, PLAST, .55), darken(PLAST, .5), .8)
        for k in range(int((x1 - x0) / 0.5)):
            a = KE(x0 + 0.25 + k * 0.5, FZ + 1.75)
            sh.path(f"M{f(a[0] - 10)} {f(a[1])} q5 -6 10 0 t10 0", "none", BLUE, 1.2, op=.7)
        spiral_band(sh, KE(x0, 0)[0], KE(x1, 0)[0], KE(0, FZ + 3.38)[1], 0.18 * SK, BLUE, bg=RED)
    for x, y, fc in ((-3.9, 2.6, 1), (-1.7, 2.9, 1), (2.2, 2.7, -1)):
        dolphin(sh, *KE(x, FZ + y), 0.75, BLUE, fc)
    # The bed against the west wall (NW), seen from its foot: frame on legs, linen, fleece.
    for x in (-5.3, -4.0):
        kerect(sh, x, FZ, x + 0.10, FZ + 0.50, CYP, darken(CYP, .6), .6)
    kerect(sh, -5.35, FZ + 0.35, -3.85, FZ + 0.50, f"url(#{sh.lin(CYP, 'v', .25, .5)})", darken(CYP, .6), .7)
    kerect(sh, -5.3, FZ + 0.50, -3.9, FZ + 0.62, LINEN, darken(LINEN, .5), .7)
    kerect(sh, -5.1, FZ + 0.62, -4.4, FZ + 0.70, "#B8AC8E", darken(LINEN, .5), .6)
    kerect(sh, -IN + 0.02, FZ + 0.35, -5.3, FZ + 1.30, CYP, darken(CYP, .6), .7)          # headboard
    # The warp-weighted loom against the north wall (NE): uprights, beam, the web, the weights.
    for x in (2.6, 4.8):
        kerect(sh, x - 0.06, FZ, x + 0.06, FZ + 2.30, f"url(#{sh.lin(CYP, 'h', .25, .5)})", darken(CYP, .6), .7)
    kerect(sh, 2.5, FZ + 2.18, 4.9, FZ + 2.30, CYP, darken(CYP, .6), .7)
    web = poly_path([KE(2.72, FZ + 2.18), KE(4.68, FZ + 2.18), KE(4.68, FZ + 0.80), KE(2.72, FZ + 0.80)])
    sh.path(web, LINEN, darken(LINEN, .5), .6, op=.9)
    for k in range(4):                                  # woven stripes at the top, warp below
        kerect(sh, 2.72, FZ + 2.10 - k * 0.14, 4.68, FZ + 2.04 - k * 0.14, RED if k % 2 else "#6E4A3A", op=.85)
    for k in range(14):
        xx = 2.8 + k * 0.135
        sh.line(*KE(xx, FZ + 1.50), *KE(xx, FZ + 0.80), darken(LINEN, .35), .6, op=.8)
        cx, cy = KE(xx, FZ + 0.74)
        sh.path(f"M{f(cx - 3)} {f(cy - 5)} l6 0 l1.5 9 l-9 0 z", TERRA, darken(TERRA, .5), .5)
    # The stool before the loom and a basket of wool.
    kerect(sh, 3.45, FZ, 3.95, FZ + 0.45, f"url(#{sh.lin(CYP, 'v', .25, .5)})", darken(CYP, .6), .7)
    kit_cut_walls(sh, "Keep")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(3.7, FZ + 1.6), "WARP-WEIGHTED LOOM", "2.30 m frame, clay weights"),
        (*KE(3.7, FZ + 0.3), "WEAVER'S STOOL", "0.45 m · loot"),
        (*KE(2.2, FZ + 2.7), "DOLPHIN FRESCO", "1.60–3.20 m, sea band"),
    ], 610, 190, 330, slope=1.0)
    sh.callouts([
        (*KE(-4.6, FZ + 0.6), "THE QUEEN'S BED", "1.40 × 2.20 m, linen, fleece"),
        (*KE(-5.45, FZ + 1.1), "HEADBOARD", "cypress, against the west wall"),
    ], 330, 220, 300, anchor="end")
    kit_clear_note(sh, "Keep", x=1.2)

    # ---- plan ----
    kit_plan(sh, "Keep", floor="#2E2719", wall="#3A332A", trim=RED)
    kprect(sh, -3.6, 2.2, -2.0, 4.6, "#6E4A3A", op=.7)                       # rug before the bed
    plan_box(sh, -4.6, 3.9, 1.4, 2.2, CYP)
    plan_box(sh, -4.6, 3.8, 1.3, 1.9, LINEN)
    plan_box(sh, -4.6, 4.6, 0.9, 0.5, "#B8AC8E")
    kprect(sh, -IN, 2.8, -5.3, 5.0, darken(CYP, .2))
    kprect(sh, 2.55, 5.05, 4.85, 5.35, LINEN, "#0E0C09", .6)                  # loom
    for x in (2.6, 4.8):
        plan_box(sh, x, 5.2, 0.14, 0.14, CYP)
    plan_box(sh, 3.7, 4.2, 0.5, 0.5, CYP)
    sh.circle(*KP(2.3, 4.2), 0.25 * PK, "#6E4A3A", "#0E0C09", .6)             # wool basket
    sh.circle(*KP(4.2, -4.2), 0.60 * PK, TERRA, "#0E0C09", .8)                # hearth
    sh.circle(*KP(4.2, -4.2), 0.40 * PK, MADDER_, op=.85)
    plan_box(sh, 5.25, -2.6, 0.5, 1.6, GYP)                                    # bench, east wall
    plan_box(sh, -4.5, -4.9, 1.0, 0.6, CYP)
    kprect(sh, -5.0, -4.93, -4.0, -4.87, BRONZE)
    plan_box(sh, -2.6, -4.8, 0.9, 0.5, FLOOR)
    for dx in (-0.25, 0, 0.25):
        sh.circle(*KP(-2.6 + dx, -4.8), 0.06 * PK, BLUE)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Keep")
    socket_label(sh, *KP(3.7, 3.4), "LOOM")
    socket_label(sh, *KP(4.2, -3.2), "HEARTH")
    socket_label(sh, *KP(-4.6, 2.35), "BED")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 3.31 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("BED", "NW, head to the west wall · rug before it"),
                    ("LOOM", "NE, against the north wall · stool, wool"),
                    ("LOOT", "L1–L5: bed, stool, chest, dressing table, bench")])
    return sh
