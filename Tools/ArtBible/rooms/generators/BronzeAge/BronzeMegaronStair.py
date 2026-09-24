"""BronzeMegaronStair: the stair up to the roof terrace (stands in for KeepStairwell).

The kit's L-stair (castle_builders._stair_to_gallery): a flight west along the south
wall, a corner landing, a flight north along the west wall, a bridge over the
east-west walkway and a railed gallery filling the NW quadrant at 2.60 m. Section
A-A east-west at y = 0, looking north, cuts the bridge and shows the gallery with
its horns of consecration and the chest on it.
"""
from _bronze import *

TOP = 2.60
ANCHORS = [(-4.3, 4.4, FZ + TOP + 0.60), (3.8, 3.8, 1.44), (3.8, -3.8, 1.44), (4.6, -2.4, 1.00)]


def build():
    mats = [("plaster", OCHRE), ("gypsum", GYP), ("cypress", CYP), ("haematite", RED),
            ("fresco", BLUE), ("bronze", BRONZE), ("fire", MADDER_)]
    sh = room_sheet("BronzeAge", "Keep", "The Megaron Stair", mats, "≤ 1.6k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE BRIDGE, LOOKING NORTH")
    kit_glow(sh, 3.8, FZ + 1.3, MADDER_, rx=260, ry=200, strength=.35)

    # ---- section ----
    kit_slab(sh, FLOOR)
    kit_back_wall(sh, "Keep", OCHRE, trim=RED)
    fresco_band(sh, "Keep", bottom=1.4, top=3.0, figures=False, x_sections=((1.3, IN),))
    g0 = FZ + TOP
    # The gallery in the NW quadrant: its deck, posts, rail and horns along the front.
    kerect(sh, -IN, g0 - 0.20, -1.8, g0, f"url(#{sh.lin(CYP, 'v', .25, .5)})", darken(CYP, .6), .8)
    for x in (-5.3, -1.9):
        kerect(sh, x - 0.1, FZ, x + 0.1, g0 - 0.20, f"url(#{sh.lin(CYP, 'h', .25, .5)})", darken(CYP, .6), .7)
    kerect(sh, -1.89, g0, -1.81, g0 + 0.90, CYP, darken(CYP, .6), .6)
    kerect(sh, -4.0, g0 + 0.82, -1.8, g0 + 0.90, CYP, darken(CYP, .6), .6)
    for x in (-3.6, -2.4):
        horns(sh, x, g0 + 0.90, 0.5, GYP)
    chest(sh, -4.3, 1.0, 0.55, base=g0)
    # The bridge over the walkway, cut by the section at the west wall.
    d = poly_path([KE(-IN, g0 - 0.20), KE(-4.0, g0 - 0.20), KE(-4.0, g0), KE(-IN, g0)])
    sh.path(d, CYP, "#0E0C09", 1.2)
    a, b = KE(-IN, g0), KE(-4.0, g0 - 0.2)
    hatch(sh, d, (a[0], a[1], b[0], b[1]), "#635C4C", 4, .8)
    sh.text(*KE(-3.0, g0 + 0.25), f"GALLERY +{TOP:.2f}", 9, "#DCD2BA", "middle")
    # East side: a tripod with a fire in its bowl in each corner (NE seen here).
    tripod(sh, 3.8)
    for x, hh in ((3.72, .35), (3.85, .5), (3.95, .3)):
        a, tip = KE(x, FZ + 1.02), KE(x, FZ + 1.02 + hh)
        sh.path(f"M{f(a[0] - 5)} {f(a[1])} Q{f(a[0] - 4)} {f((a[1] + tip[1]) / 2)} {f(tip[0])} {f(tip[1])} "
                f"Q{f(a[0] + 5)} {f((a[1] + tip[1]) / 2)} {f(a[0] + 5)} {f(a[1])} Z", MADDER_, op=.9)
    kit_cut_walls(sh, "Keep")
    khuman(sh, 0.9)
    sh.callouts([
        (*KE(3.8, FZ + 1.2), "TRIPOD BRAZIER", "NE and SE, fire in the bowl"),
        (*KE(4.0, FZ + 2.2), "FRESCO BAND", "east half of the north wall"),
    ], 610, 230, 310, slope=1.0)
    sh.callouts([
        (*KE(-3.0, g0 + 1.1), "HORNS OF CONSECRATION", "gypsum, on the gallery rail"),
        (*KE(-4.3, g0 + 0.4), "CHEST ON THE GALLERY", "the reason to climb · loot"),
        (*KE(-4.75, g0 - 0.1), "BRIDGE", "over the walkway, 2.40 m clear"),
    ], 330, 180, 330, anchor="end")
    kit_clear_note(sh, "Keep", x=1.8)

    # ---- plan ----
    kit_plan(sh, "Keep", floor="#2E2719", wall="#3A332A", trim=RED)
    steps, rise = 4, TOP / 8
    x0, x_land = -Q0 - 0.1, -IN + 1.5
    run_x = (x0 - x_land) / steps
    for i in range(steps):
        xa = x0 - i * run_x
        kprect(sh, xa - run_x, -IN, xa, -IN + 1.5, mix(GYP, "#14120E", .45 - i * .06), "#0E0C09", .6)
    kprect(sh, -IN, -IN, -IN + 1.5, -IN + 1.5, mix(GYP, "#14120E", .15), "#0E0C09", .6)
    y0, y_top = -IN + 1.5, -Q0 - 0.2
    run_y = (y_top - y0) / steps
    for i in range(steps):
        ya = y0 + i * run_y
        kprect(sh, -IN, ya, -IN + 1.5, ya + run_y, mix(GYP, "#14120E", .1 - i * .02), "#0E0C09", .6)
    kprect(sh, -IN, -Q0 - 0.2, -IN + 1.5, Q0 + 0.2, CYP, "#0E0C09", .6)
    kprect(sh, -IN, Q0, -Q0, IN, lighten(CYP, .1), "#0E0C09", .8)
    for x, y in ((-Q0 - 0.1, Q0 + 0.1), (-Q0 - 0.1, IN - 0.2), (-IN + 0.2, Q0 + 0.1)):
        plan_box(sh, x, y, 0.2, 0.2, darken(CYP, .3))
    kprect(sh, -Q0 - 0.1, Q0, -Q0, IN, "#DCD2BA")
    kprect(sh, -IN + 1.5, Q0, -Q0, Q0 + 0.1, "#DCD2BA")
    plan_box(sh, -4.3, 4.4, 1.0, 0.6, CYP)
    sh.path(f"M{f(KP(-2.9, -4.75)[0])} {f(KP(-2.9, -4.75)[1])} L{f(KP(-4.6, -4.75)[0])} {f(KP(-4.6, -4.75)[1])} "
            f"L{f(KP(-4.75, -4.6)[0])} {f(KP(-4.75, -4.6)[1])} L{f(KP(-4.75, -2.6)[0])} {f(KP(-4.75, -2.6)[1])}",
            "none", "#DCD2BA", 1, extra='stroke-dasharray="3 2"')
    for x, y in ((3.8, 3.8), (3.8, -3.8)):
        sh.circle(*KP(x, y), 0.30 * PK, BRONZE, "#0E0C09", .8)
        sh.circle(*KP(x, y), 0.16 * PK, MADDER_)
    plan_box(sh, 4.6, -2.4, 0.6, 0.6, FLOOR)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Keep")
    socket_label(sh, *KP(-3.2, -3.6), "STAIR UP")
    socket_label(sh, *KP(-3.6, 3.0), f"GALLERY +{TOP:.2f}")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 3.31 · all open"),
                    ("CLEAR CROSS", "dashed · the bridge crosses it at 2.40 m"),
                    ("STAIR", "2 × 4 steps, foot facing open floor (SW)"),
                    ("GALLERY", "NW quadrant at 2.60 m · railed, horns"),
                    ("LOOT", "L1–L4: gallery chest, tripods, stand")])
    return sh
