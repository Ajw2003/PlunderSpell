"""LateArmouryHall: the armoury hall (stands in for ArmouredCourtyard).

NW and NE: harnesses of plate on stands, two in each quadrant along the north wall.
East and west walls: halberd and poleaxe racks, five hafts each, in the north
quadrants. SE: a grinding wheel on its oak frame over a water trough. SW: the
armourer's table with a sallet in hand, tools and gauntlets, a chest of spare
plates against the south wall. Section A-A east-west at y = 0, looking north: the
four harnesses face-on, the racks edge-on at the east and west walls.
"""
from _late import *

STANDS = [-4.6, -3.0, 3.0, 4.6]
STAND_Y = 4.8
RACK_Y = (2.1, 4.1)                               # the racks' span on the E and W walls
HAFTS = [2.3, 2.7, 3.1, 3.5, 3.9]
WHEEL = (3.8, -3.8)
TABLE = (-3.6, -3.6, 1.8, 0.8, 0.85)
CHEST = (-3.4, -5.0, 1.2, 0.6, 0.6)
ANCHORS = [(TABLE[0], TABLE[1], FZ + TABLE[4]), (CHEST[0], CHEST[1], FZ + CHEST[4] + 0.05)]


def build():
    mats = [("sandstone", SAND), ("plate", STEEL), ("oak", OAK), ("iron", IRON), ("whetstone", "#8A8070"),
            ("madder", ESTATE), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "InnerWard", "The Armoury Hall", mats, "≤ 3.2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE HALL, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 1.6, "#6A6A60", rx=380, ry=200, strength=.2)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "InnerWard", SAND, soot=SOOT, trim=WOOL, soot_depth=0.5)
    ashlar_courses(sh, "InnerWard")
    for x in (-3.8, 3.8):                                                 # the banners over each pair
        kerect(sh, x - 0.5, FZ + 2.1, x + 0.5, FZ + 3.4, f"url(#{sh.lin(ESTATE, 'h', .3, .5)})", darken(ESTATE, .5), .8)
        kerect(sh, x - 0.075, FZ + 2.1, x + 0.075, FZ + 3.4, LINEN, op=.9)
        kerect(sh, x - 0.55, FZ + 3.4, x + 0.55, FZ + 3.44, IRON)
    for x in STANDS:
        armour(sh, x)
    # The racks at the east and west walls, edge-on: rails and the hafts standing in them.
    for sgn in (-1, 1):
        xw = sgn * (IN - 0.1)
        for z in (0.35, 1.5):
            kerect(sh, xw - 0.08, FZ + z - 0.05, xw + 0.08, FZ + z + 0.05, OAK, darken(OAK, .6), .5)
        kerect(sh, xw - 0.02, FZ, xw + 0.02, FZ + 2.3, OAK)
        head = [KE(xw, FZ + 2.3), KE(xw - sgn * 0.12, FZ + 2.35), KE(xw - sgn * 0.12, FZ + 2.55), KE(xw, FZ + 2.6),
                KE(xw + sgn * 0.08, FZ + 2.45)]
        sh.path(poly_path(head), STEEL, darken(STEEL, .6), .7)
        sh.line(*KE(xw, FZ + 2.6), *KE(xw, FZ + 2.8), STEEL, 1.6)
    kit_cut_walls(sh, "InnerWard")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(3.0, FZ + 1.4), "HARNESS OF PLATE", "on stands, 2 NE, 2 NW"),
        (*KE(IN - 0.15, FZ + 2.45), "HALBERD RACK", "5 hafts, east and west walls"),
    ], 610, 230, 310, slope=1.0)
    sh.callouts([
        (*KE(-3.8, FZ + 3.0), "BANNERS", "madder with a linen pale, 1.00 × 1.30 m"),
        (*KE(-4.6, FZ + 1.8), "SALLET", "with its tail, visor slit"),
        (*KE(-IN + 0.15, FZ + 1.5), "POLEAXE RACK", "rails at 0.35 and 1.50 m"),
    ], 330, 200, 320, anchor="end")
    kit_clear_note(sh, "InnerWard", x=0.0, text="4.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "InnerWard", floor="#3A3226", wall="#3A332A", trim=WOOL)
    for x in (-3.8, 3.8):
        kprect(sh, x - 0.5, IN - 0.05, x + 0.5, IN, ESTATE)
    for x in STANDS:
        plan_box(sh, x, STAND_Y, 0.6, 0.45, OAK)
        plan_disc(sh, x, STAND_Y, 0.2, STEEL)
    for sgn in (-1, 1):
        a, b = sorted((sgn * IN, sgn * (IN - 0.16)))
        kprect(sh, a, RACK_Y[0], b, RACK_Y[1], OAK, "#0E0C09", .5)
        for y in HAFTS:
            plan_disc(sh, sgn * (IN - 0.1), y, 0.04, STEEL)
    wx, wy = WHEEL
    plan_box(sh, wx, wy, 1.0, 0.3, "#3A4A50")
    plan_box(sh, wx, wy, 0.9, 0.12, "#8A8070")
    for dy in (-0.2, 0.2):
        plan_box(sh, wx, wy + dy, 0.08, 0.08, OAK)
    plan_box(sh, *TABLE[:4], OAK)
    plan_disc(sh, TABLE[0] - 0.5, TABLE[1], 0.13, STEEL)
    plan_box(sh, *CHEST[:4], darken(OAK, .1))
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "InnerWard")
    socket_label(sh, *KP(3.8, -2.6), "GRINDING WHEEL")
    socket_label(sh, *KP(-3.6, -2.6), "ARMOURER'S TABLE")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.88 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("ARMOUR", "four harnesses on stands along the north wall"),
                    ("RACKS", "halberds and poleaxes, E and W walls, north half"),
                    ("LOOT", "L1–L2: armourer's table, plate chest")])
    return sh
