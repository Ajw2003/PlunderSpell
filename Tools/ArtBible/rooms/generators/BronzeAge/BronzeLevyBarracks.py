"""BronzeLevyBarracks: the palace levy's barracks (stands in for BarracksBunk).

Section A-A east-west at y = 0, looking north: spear racks on the north wall either
side of the archway, the low sleeping benches along the east and west walls seen
end-on. The figure-of-eight shields hang on the south wall (plan, and noted).
"""
from _bronze import *

ANCHORS = [(5.0, 3.5, 0.68), (-5.0, 3.5, 0.68), (3.2, -3.0, 0.80), (-3.2, -3.2, 0.90)]
REED = "#B8A070"
HIDE = "#8A6A4A"


def spear(sh, x, base, h=2.4):
    """A spear standing in its rack: ash shaft, bronze leaf-shaped head."""
    kerect(sh, x - 0.02, base, x + 0.02, base + h - 0.3, "#7A5E3A")
    tip = [KE(x, base + h), KE(x - 0.05, base + h - 0.18), KE(x, base + h - 0.32), KE(x + 0.05, base + h - 0.18)]
    sh.path(poly_path(tip), f"url(#{sh.lin(BRONZE, 'h', .4, .45)})", darken(BRONZE, .5), .7)


def build():
    mats = [("plaster", OCHRE), ("mud-brick", MUD), ("reed mat", REED), ("oxhide", HIDE),
            ("ash wood", "#7A5E3A"), ("bronze", BRONZE), ("clay", TERRA)]
    sh = room_sheet("BronzeAge", "OuterBailey", "The Levy Barracks", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE BARRACKS, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 1.6, "#5A4A30", rx=380, ry=200, strength=.3)

    # ---- section ----
    kit_slab(sh, "#6A4E36")
    kit_back_wall(sh, "OuterBailey", OCHRE, trim=MUD, soot_depth=0.6)
    # Spear racks on the north wall, either side of the archway: two uprights, a rail, five spears each.
    for sx in (-1, 1):
        x0 = sx * 3.4
        for dx in (-1.0, 1.0):
            kerect(sh, x0 + dx - 0.05, FZ, x0 + dx + 0.05, FZ + 1.50, CYP, darken(CYP, .6), .6)
        kerect(sh, x0 - 1.05, FZ + 1.36, x0 + 1.05, FZ + 1.46, CYP, darken(CYP, .6), .6)
        for k in range(5):
            spear(sh, x0 - 0.8 + k * 0.4, FZ)
    # The sleeping benches along the east and west walls, end-on, reed mats on them.
    for x0, x1 in ((-IN, -IN + 0.9), (IN - 0.9, IN)):
        kerect(sh, x0, FZ, x1, FZ + 0.35, f"url(#{sh.lin(TERRA, 'v', .25, .5)})", darken(TERRA, .6), .8)
        kerect(sh, x0 + 0.05, FZ + 0.35, x1 - 0.05, FZ + 0.38, REED, darken(REED, .5), .6)
        kerect(sh, x0 + 0.1, FZ + 0.38, x1 - 0.2, FZ + 0.52, HIDE, darken(HIDE, .5), .6)       # a rolled kit bag
    kit_cut_walls(sh, "OuterBailey")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(3.8, FZ + 2.1), "SPEAR RACK", "5 spears, bronze heads, 2.40 m"),
        (*KE(5.0, FZ + 0.45), "SLEEPING BENCH", "clay, reed mat, kit bag · loot"),
    ], 610, 230, 310, slope=1.0)
    sh.callouts([
        (*KE(-3.4, FZ + 1.4), "RACK RAIL", "cypress, at 1.70 m"),
        (*KE(-5.0, FZ + 0.2), "BENCH", "0.90 × 3.00 × 0.35 m"),
    ], 330, 230, 300, anchor="end")
    kit_clear_note(sh, "OuterBailey", x=0.0, text="3.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "OuterBailey", floor="#2E2519", wall="#3A332A", trim=MUD)
    for sx in (-1, 1):
        for sy in (-1, 1):
            plan_box(sh, sx * 5.0, sy * 3.5, 0.9, 3.0, TERRA)
            plan_box(sh, sx * 5.0, sy * 3.5, 0.8, 2.9, REED)
        plan_box(sh, sx * 3.4, 5.25, 2.1, 0.12, CYP)
        for k in range(5):
            sh.circle(*KP(sx * 3.4 - 0.8 + k * 0.4, 5.25), 0.04 * PK, BRONZE)
        for x in (sx * 3.0, sx * 4.2):                            # figure-of-eight shields on the south wall
            kprect(sh, x - 0.35, -IN, x + 0.35, -IN + 0.08, HIDE, "#0E0C09", .5)
        plan_jar(sh, sx * 2.2, -5.0, 0.36, TERRA)
    plan_box(sh, 3.2, -3.0, 0.9, 0.6, CYP)
    for dx in (-0.2, 0.0, 0.2):
        sh.circle(*KP(3.2 + dx, -3.0), 0.03 * PK, "#DCD2BA")
    plan_box(sh, -3.2, -3.2, 1.0, 0.6, CYP)
    kprect(sh, -3.7, -3.23, -2.7, -3.17, BRONZE)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "OuterBailey")
    socket_label(sh, *KP(3.6, -4.55), "SHIELDS")
    socket_label(sh, *KP(-3.6, -4.55), "SHIELDS")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.59 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("BENCHES", "4, E and W walls, sleeping 8 levymen"),
                    ("ARMS", "spear racks N wall · oxhide shields S wall"),
                    ("LOOT", "L1–L4: two benches, gaming table, kit chest")])
    return sh
