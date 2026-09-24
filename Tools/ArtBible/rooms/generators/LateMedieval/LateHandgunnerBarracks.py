"""LateHandgunnerBarracks: the handgunners' barracks (stands in for BarracksBunk).

Two straw pallets on low oak frames in every quadrant, long axis north-south, against
the east and west walls. NW: a handgun rack on the north wall, and a footlocker at
the head of the outer pallet; SW: another footlocker. NE: the dice table between the
pallets and the crossing, two stools, dice, cups and a stake. SE: three pavises propped
against the south wall. Section A-A east-west at y = 0, looking north: the pallets
end-on, the rack, the table.
"""
from _late import *

BED_X = (3.9, 5.0)
BED_Y = (2.6, 4.6)
BED_W, BED_H = 0.9, 0.3
STRAW = "#B98A34"
RACK = (-3.3, 2.2)                                 # centre x, width on the north wall
GUNS = [-4.1, -3.6, -3.1, -2.6]
LOCKERS = [(-5.0, 5.05), (-5.0, -5.05)]
TABLE = (2.5, 3.6, 1.0, 0.7, 0.75)
STOOLS = [(2.5, 2.95), (2.5, 4.25)]
PAVISES = [2.4, 3.1, 3.8]
ANCHORS = [(TABLE[0], TABLE[1], FZ + TABLE[4]), (LOCKERS[0][0], LOCKERS[0][1], FZ + 0.55),
           (LOCKERS[1][0], LOCKERS[1][1], FZ + 0.55)]


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("straw", STRAW), ("blanket", WOOL), ("iron", IRON),
            ("madder", ESTATE), ("coin", GOLD)]
    sh = room_sheet("LateMedieval", "OuterBailey", "The Handgunners' Barracks", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE BARRACKS, LOOKING NORTH")
    kit_glow(sh, 2.5, FZ + 1.2, FIRE, rx=200, ry=150, strength=.2)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "OuterBailey", SAND, soot=SOOT, trim=BRICK, soot_depth=0.6)
    ashlar_courses(sh, "OuterBailey")
    rx, rw = RACK
    kerect(sh, rx - rw / 2, FZ + 1.5, rx + rw / 2, FZ + 1.6, OAK, darken(OAK, .6), .6)
    kerect(sh, rx - rw / 2, FZ + 0.25, rx + rw / 2, FZ + 0.33, OAK, darken(OAK, .6), .6)
    for x in GUNS:
        kerect(sh, x - 0.05, FZ, x + 0.05, FZ + 0.95, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .6)
        kerect(sh, x - 0.03, FZ + 0.95, x + 0.03, FZ + 1.8, f"url(#{sh.lin(IRON, 'h', .3, .5)})", "#0E0C09", .6)
    chest(sh, LOCKERS[0][0], 0.8, 0.5)
    for sgn in (-1, 1):
        for bx in BED_X:
            x = sgn * bx
            kerect(sh, x - BED_W / 2, FZ, x + BED_W / 2, FZ + 0.2, f"url(#{sh.lin(OAK, 'v', .25, .5)})",
                   darken(OAK, .6), .8)
            kerect(sh, x - BED_W / 2 + 0.03, FZ + 0.2, x + BED_W / 2 - 0.03, FZ + BED_H + 0.05, STRAW,
                   darken(STRAW, .5), .6)
            kerect(sh, x - BED_W / 2 + 0.02, FZ + BED_H + 0.02, x + BED_W / 2 - 0.02, FZ + BED_H + 0.08, WOOL,
                   darken(WOOL, .5), .5)
    tx, ty, tw, td, th = TABLE
    table(sh, tx, tw, th)
    for dx in (-0.15, 0.1):
        kerect(sh, tx + dx - 0.04, FZ + th, tx + dx + 0.04, FZ + th + 0.1, PEWTER, darken(PEWTER, .5), .5)
    sh.circle(*KE(tx - 0.25, FZ + th + 0.02), 2, GOLD)
    kit_cut_walls(sh, "OuterBailey")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(tx, FZ + th + 0.05), "DICE TABLE", "dice, cups, a stake · loot"),
        (*KE(5.0, FZ + 0.3), "STRAW PALLETS", "0.90 × 2.00 m, two per quadrant"),
    ], 610, 230, 310, slope=1.0)
    sh.callouts([
        (*KE(-2.6, FZ + 1.6), "HANDGUN RACK", "four hand-cannon, north wall"),
        (*KE(LOCKERS[0][0], FZ + 0.4), "FOOTLOCKER", "NW and SW · loot"),
    ], 330, 230, 310, anchor="end")
    kit_clear_note(sh, "OuterBailey", x=0.0, text="3.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "OuterBailey", floor="#302A20", wall="#3A332A", trim=BRICK)
    for sx in (-1, 1):
        for sy in (-1, 1):
            for bx in BED_X:
                y0, y1 = sorted((sy * BED_Y[0], sy * BED_Y[1]))
                kprect(sh, sx * bx - BED_W / 2, y0, sx * bx + BED_W / 2, y1, OAK, "#0E0C09", .6)
                kprect(sh, sx * bx - BED_W / 2 + 0.05, y0 + 0.05, sx * bx + BED_W / 2 - 0.05, y1 - 0.05, STRAW)
                yb0, yb1 = (y0 + 0.05, y0 + 1.0) if sy > 0 else (y1 - 1.0, y1 - 0.05)
                kprect(sh, sx * bx - BED_W / 2 + 0.03, yb0, sx * bx + BED_W / 2 - 0.03, yb1, WOOL)
    kprect(sh, rx - rw / 2, IN - 0.25, rx + rw / 2, IN, OAK, "#0E0C09", .5)
    for x in GUNS:
        plan_disc(sh, x, IN - 0.12, 0.05, IRON)
    for x, y in LOCKERS:
        plan_box(sh, x, y, 0.8, 0.5, darken(OAK, .1))
    plan_box(sh, tx, ty, tw, td, OAK)
    for x, y in STOOLS:
        plan_disc(sh, x, y, 0.18, OAK)
    for x in PAVISES:
        kprect(sh, x - 0.3, -IN, x + 0.3, -IN + 0.1, ESTATE, "#0E0C09", .5)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "OuterBailey")
    socket_label(sh, *KP(3.7, -4.95), "PAVISES")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.59 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("PALLETS", "8, two per quadrant against the E and W walls"),
                    ("KIT", "handgun rack N wall · pavises S wall · lockers"),
                    ("LOOT", "L1–L3: dice table, two footlockers")])
    return sh
