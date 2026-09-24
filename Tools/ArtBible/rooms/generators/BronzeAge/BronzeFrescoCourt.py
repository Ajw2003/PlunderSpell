"""BronzeFrescoCourt: the colonnaded court of the palace (stands in for GreatHallMain).

Four red down-tapering columns round the crossing, a procession fresco on every
wall, plastered benches along all four walls, a round fire altar in the NE and
two offering tables to the south. Section A-A east-west at y = 0, looking north.
"""
from _bronze import *

ANCHORS = [(-3.6, 5.25, 0.70), (3.6, 5.25, 0.70), (-3.6, -5.25, 0.70), (3.6, -5.25, 0.70),
           (-3.2, -3.4, 1.00), (3.2, -3.4, 1.00), (3.6, 3.6, 1.20)]


def build():
    mats = [("plaster", PLAST), ("fresco", BLUE), ("haematite", RED), ("gypsum", GYP),
            ("soot", SOOT), ("fire", MADDER_), ("floor", FLOOR)]
    sh = room_sheet("BronzeAge", "InnerWard", "The Fresco Court", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE COURT, LOOKING NORTH")
    kit_glow(sh, 3.6, FZ + 1.2, MADDER_, rx=300, ry=220, strength=.35)
    top = FZ + ZONE_CLEAR["InnerWard"]

    # ---- section ----
    kit_slab(sh, FLOOR)
    kit_back_wall(sh, "InnerWard", PLAST, trim=BLUE, soot_depth=0.8)
    fresco_band(sh, "InnerWard", bottom=1.4, top=3.0, figures=True)
    for x in (-3.6, 3.6):
        bench(sh, x - 1.6, x + 1.6)
    # East and west benches seen end-on against their walls.
    for x0, x1 in ((-IN, -IN + 0.5), (IN - 0.5, IN)):
        kerect(sh, x0, FZ, x1, FZ + 0.40, darken(GYP, .15), darken(GYP, .6), .7)
    # The round fire altar in the NE, fire on it.
    kerect(sh, 3.0, FZ, 4.2, FZ + 0.90, f"url(#{sh.lin(GYP, 'h', .3, .5)})", darken(GYP, .6), 1)
    for k in range(3):
        kerect(sh, 3.0, FZ + 0.15 + k * 0.3, 4.2, FZ + 0.2 + k * 0.3, RED, op=.7)
    for x, hh in ((3.2, .35), (3.35, .55), (3.5, .4)):             # the fire burns at the altar's west side
        a, tip = KE(x, FZ + 0.9), KE(x, FZ + 0.9 + hh)
        sh.path(f"M{f(a[0] - 8)} {f(a[1])} Q{f(a[0] - 6)} {f((a[1] + tip[1]) / 2)} {f(tip[0])} {f(tip[1])} "
                f"Q{f(a[0] + 8)} {f((a[1] + tip[1]) / 2)} {f(a[0] + 8)} {f(a[1])} Z", MADDER_, op=.9)
    for x in (-2.4, 2.4):
        column(sh, x, top)
    kit_cut_walls(sh, "InnerWard")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(2.4, FZ + 2.2), "RED COLUMNS", "4 at ±2.40, 0.34 → 0.46 m"),
        (*KE(3.6, FZ + 1.3), "FIRE ALTAR", "gypsum drum, 1.20 × 0.90 m"),
        (*KE(4.3, FZ + 0.3), "BENCHES", "all four walls · loot"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-4.1, FZ + 2.2), "PROCESSION FRESCO", "1.40–3.00 m, every wall"),
    ], 330, 250, 250, anchor="end")
    kit_clear_note(sh, "InnerWard", x=0.0, text="4.00 clear · open to the sky")

    # ---- plan ----
    kit_plan(sh, "InnerWard", floor="#2E2719", wall="#3A332A", trim=BLUE)
    # A painted grid on the court floor (albedo only), the paving of a court.
    for i in range(-5, 6):
        sh.line(*KP(i, -IN), *KP(i, IN), "#3A3124", .5, op=.6)
        sh.line(*KP(-IN, i), *KP(IN, i), "#3A3124", .5, op=.6)
    for x in (-3.6, 3.6):
        for y in (5.25, -5.25):
            plan_box(sh, x, y, 3.2, 0.5, darken(GYP, .2))
    for x in (-5.25, 5.25):
        for y in (-3.4, 3.4):
            plan_box(sh, x, y, 0.5, 2.4, darken(GYP, .2))
    for x in (-2.4, 2.4):
        for y in (-2.4, 2.4):
            sh.circle(*KP(x, y), 0.35 * PK, SOOT, "#0E0C09", .8)
            sh.circle(*KP(x, y), 0.23 * PK, RED, "#0E0C09", .8)
    sh.circle(*KP(3.6, 3.6), 0.6 * PK, GYP, "#0E0C09", .8)
    sh.circle(*KP(3.35, 3.6), 0.18 * PK, MADDER_, op=.9)
    for x in (-3.2, 3.2):
        plan_box(sh, x, -3.4, 0.6, 0.6, FLOOR)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "InnerWard")
    socket_label(sh, *KP(3.6, 2.6), "ALTAR")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.88 · all open"),
                    ("CLEAR CROSS", "dashed · the columns stand just outside it"),
                    ("COLUMNS", "4 at (±2.40, ±2.40), 4.00 m to the wall top"),
                    ("ALTAR", "NE, a fire on it (IGNIS source)"),
                    ("LOOT", "L1–L7: benches, offering tables, altar")])
    return sh
