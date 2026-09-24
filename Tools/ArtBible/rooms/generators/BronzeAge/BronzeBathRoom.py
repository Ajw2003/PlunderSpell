"""BronzeBathRoom: the bath (stands in for LordsSolar), after the bathroom at Pylos.

Section A-A east-west at y = 0, looking north: the lustral basin on its pedestal
in the NW, the painted clay tub on its plinth in the NE with water jars either
side, a band of painted lilies behind. The bench, flask shelf and oil stand in the
south half show in the plan.
"""
from _bronze import *

ANCHORS = [(3.8, 4.8, 1.05), (-4.2, 4.2, 1.30), (3.6, -5.25, 0.70), (-3.7, -5.2, 1.78), (-4.8, -2.6, 1.00)]


def lily(sh, x, y, s, col):
    """A painted lily: a stem and three curled petals, base at (x, y) px."""
    sh.line(x, y, x, y - 26 * s, darken(col, .3), 1.2)
    for dx, rot in ((-7, -1), (0, 0), (7, 1)):
        sh.path(f"M{f(x)} {f(y - 24 * s)} q{f(dx * s)} {f(-10 * s)} {f((dx + rot * 4) * s)} {f(-16 * s)}",
                "none", col, 2, op=.9)


def build():
    mats = [("plaster", PLAST), ("fresco", BLUE), ("haematite", RED), ("clay", TERRA),
            ("gypsum", GYP), ("cypress", CYP), ("water", "#5E8A96")]
    sh = room_sheet("BronzeAge", "Keep", "The Bath", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE ROOM, LOOKING NORTH")
    kit_glow(sh, 3.8, FZ + 1.2, "#6A8A8A", rx=280, ry=200, strength=.28)

    # ---- section ----
    kit_slab(sh, FLOOR)
    kit_back_wall(sh, "Keep", PLAST, trim=RED)
    # A band of painted lilies, lower than the halls' frescoes: this room is sat in, not walked through.
    for x0, x1 in ((-IN, -1.3), (1.3, IN)):
        kerect(sh, x0, FZ + 1.20, x1, FZ + 2.40, mix("#E0C8A0", PLAST, .4), darken(PLAST, .5), .8)
        spiral_band(sh, KE(x0, 0)[0], KE(x1, 0)[0], KE(0, FZ + 2.40)[1], 0.16 * SK, BLUE, bg=RED)
        for k in range(int((x1 - x0) / 0.45)):
            lily(sh, *KE(x0 + 0.25 + k * 0.45, FZ + 1.30), 1.4, RED if k % 2 else BLUE)
    # NW: the lustral basin, a stone bowl on a pedestal.
    kerect(sh, -4.45, FZ, -3.95, FZ + 0.10, GYP, darken(GYP, .5), .7)
    kerect(sh, -4.45 + 0.2, FZ + 0.10, -3.95 - 0.2, FZ + 0.80, f"url(#{sh.lin(GYP, 'h', .25, .5)})", darken(GYP, .6), .7)
    bowl = [KE(-4.75, FZ + 1.0), KE(-4.65, FZ + 0.80), KE(-3.75, FZ + 0.80), KE(-3.65, FZ + 1.0)]
    sh.path(poly_path(bowl), f"url(#{sh.lin(GYP, 'h', .3, .5)})", darken(GYP, .6), 1)
    sh.ellipse(*KE(-4.2, FZ + 1.0), 0.5 * SK, 4, "#5E8A96", darken("#5E8A96", .5), .6)
    # NE: the painted clay tub on its plinth, a hydria either side.
    kerect(sh, 2.8, FZ, 4.8, FZ + 0.15, f"url(#{sh.lin(GYP, 'v', .2, .45)})", darken(GYP, .6), .8)
    tub = [KE(3.0, FZ + 0.15), KE(4.6, FZ + 0.15), KE(4.62, FZ + 0.75), KE(2.98, FZ + 0.75)]
    d = poly_path(tub)
    sh.path(d, f"url(#{sh.lin(TERRA, 'v', .25, .5)})", darken(TERRA, .6), 1)
    spiral_band(sh, KE(3.05, 0)[0], KE(4.55, 0)[0], KE(0, FZ + 0.55)[1], 0.16 * SK, "#E0C8A0", bg=RED)
    sh.line(*KE(2.98, FZ + 0.75), *KE(4.62, FZ + 0.75), darken(TERRA, .5), 2)
    for x in (2.45, 5.1):
        jar(sh, x, FZ, 0.62, 0.40, TERRA, bands=(0.25,))
    kit_cut_walls(sh, "Keep")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(3.8, FZ + 0.5), "PAINTED CLAY TUB", "1.60 × 0.70 m, spiral band"),
        (*KE(5.1, FZ + 0.4), "HYDRIA", "water jar, 0.62 m"),
        (*KE(2.2, FZ + 1.8), "LILY FRESCO", "1.20–2.40 m, low band"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-4.2, FZ + 0.95), "LUSTRAL BASIN", "stone bowl on a pedestal"),
    ], 330, 260, 260, anchor="end")
    kit_clear_note(sh, "Keep", x=1.2)

    # ---- plan ----
    kit_plan(sh, "Keep", floor="#2E2719", wall="#3A332A", trim=RED)
    # A water channel cut in the floor from the tub towards the south archway (flat).
    sh.path(poly_path([KP(3.05, 4.25), KP(3.05, 0.0), KP(0.0, 0.0), KP(0.0, -IN)], closed=False),
            "none", "#5E8A96", 3, op=.7)
    sh.circle(*KP(-4.2, 4.2), 0.55 * PK, GYP, "#0E0C09", .8)
    sh.circle(*KP(-4.2, 4.2), 0.42 * PK, "#5E8A96")
    plan_box(sh, 3.8, 4.8, 2.0, 1.1, GYP)
    plan_box(sh, 3.8, 4.8, 1.6, 0.7, TERRA)
    plan_box(sh, 3.8, 4.8, 1.4, 0.5, "#5E8A96")
    for x, y in ((2.45, 5.0), (5.1, 3.6)):
        plan_jar(sh, x, y, 0.40, TERRA)
    plan_box(sh, 3.6, -5.25, 3.2, 0.5, GYP)
    for x in (2.6, 3.2):
        plan_box(sh, x, -5.25, 0.45, 0.35, LINEN)
    plan_box(sh, -3.7, -5.2, 2.6, 0.45, darken(CYP, .15))
    for k in range(4):
        sh.circle(*KP(-4.7 + k * 0.6, -5.2), 0.08 * PK, TERRA)
    plan_box(sh, -4.8, -2.6, 0.6, 0.6, FLOOR)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Keep")
    socket_label(sh, *KP(3.8, 3.75), "TUB")
    socket_label(sh, *KP(-4.2, 3.2), "BASIN")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 3.31 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("TUB", "NE, on a gypsum plinth · water channel (flat)"),
                    ("OIL", "flask shelf and stand, SW · towels on the bench"),
                    ("LOOT", "L1–L5: tub, basin, bench, shelf, oil stand")])
    return sh
