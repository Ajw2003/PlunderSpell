"""LateTurretStair: the turret stair (stands in for KeepStairwell).

The kit's L-stair (castle_builders._stair_to_gallery) in dressed sandstone: a flight
west along the south wall, a corner landing, a flight north along the west wall,
an oak bridge over the east-west walkway and a railed oak gallery filling the NW
quadrant at 2.60 m, an iron-bound chest on it and a keyhole gun-loop in the north
wall beside it. NE: a rack of handguns on the north wall and a pavise stood against
the east wall. SE: the guards' table with a lantern and dice, a stool. Section A-A
east-west at y = 0, looking north, cuts the bridge.
"""
from _late import *

TOP = 2.60
G0 = FZ + TOP
CHEST = (-4.3, 4.4, 1.0, 0.6, 0.55)
LOOP_X = -2.6
RACK = (3.6, 2.4)                                   # centre x, width on the north wall
GUNS = [2.7, 3.3, 3.9, 4.5]
TABLE = (3.6, -3.6, 1.2, 0.7, 0.75)
ANCHORS = [(CHEST[0], CHEST[1], G0 + CHEST[4] + 0.05), (TABLE[0], TABLE[1], FZ + TABLE[4])]


def gun_loop(sh, x, z0, col=SAND):
    """A keyhole gun-loop in elevation: a dressed surround, a 0.90 m slit, a 0.20 m round."""
    kerect(sh, x - 0.3, z0, x + 0.3, z0 + 1.3, f"url(#{sh.lin(col, 'h', .25, .5)})", darken(col, .6), .8)
    kerect(sh, x - 0.05, z0 + 0.3, x + 0.05, z0 + 1.2, "#0E0C09")
    sh.circle(*KE(x, z0 + 0.25), 0.1 * SK, "#0E0C09")


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("iron", IRON), ("steel", STEEL), ("madder", ESTATE),
            ("linen", LINEN), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "Keep", "The Turret Stair", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE BRIDGE, LOOKING NORTH")
    kit_glow(sh, 3.6, FZ + 1.2, FIRE, rx=220, ry=160, strength=.25)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "Keep", SAND, soot=SOOT, trim=ESTATE, soot_depth=0.5)
    ashlar_courses(sh, "Keep")
    gun_loop(sh, LOOP_X, G0 + 0.1)
    kit_gallery_section(sh, TOP, OAK, darken(OAK, .15), label=False)
    chest(sh, CHEST[0], CHEST[2], CHEST[4], base=G0)
    # NE: the handgun rack on the north wall, the pavise against the east wall (edge-on).
    rx, rw = RACK
    kerect(sh, rx - rw / 2, FZ + 1.55, rx + rw / 2, FZ + 1.65, OAK, darken(OAK, .6), .6)
    kerect(sh, rx - rw / 2, FZ + 0.25, rx + rw / 2, FZ + 0.33, OAK, darken(OAK, .6), .6)
    for x in GUNS:
        kerect(sh, x - 0.05, FZ, x + 0.05, FZ + 0.95, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .6)
        kerect(sh, x - 0.03, FZ + 0.95, x + 0.03, FZ + 1.85, f"url(#{sh.lin(IRON, 'h', .3, .5)})", "#0E0C09", .6)
    kerect(sh, IN - 0.1, FZ, IN - 0.02, FZ + 1.3, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .7)
    kit_cut_walls(sh, "Keep")
    khuman(sh, 0.9)
    sh.callouts([
        (*KE(3.9, FZ + 1.4), "HANDGUN RACK", "four hand-cannon on the north wall"),
        (*KE(IN - 0.06, FZ + 0.9), "PAVISE", "0.60 × 1.30 m, against the east wall"),
    ], 610, 230, 310, slope=1.0)
    sh.callouts([
        (*KE(LOOP_X, G0 + 0.9), "KEYHOLE GUN-LOOP", "0.20 m round + 0.90 m slit"),
        (*KE(CHEST[0], G0 + 0.4), "CHEST ON THE GALLERY", "gallery +2.60 · iron-bound · loot"),
        (*KE(-4.75, G0 - 0.1), "BRIDGE", "oak, over the walkway, 2.40 m clear"),
    ], 330, 180, 330, anchor="end")
    kit_clear_note(sh, "Keep", x=1.8, text="4.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Keep", floor="#2A2016", wall="#3A332A", trim=ESTATE)
    kit_l_stair_plan(sh, TOP, SAND, OAK)
    plan_box(sh, CHEST[0], CHEST[1], CHEST[2], CHEST[3], darken(OAK, .1))
    kprect(sh, LOOP_X - 0.3, IN - 0.1, LOOP_X + 0.3, IN, SAND, "#0E0C09", .5)
    kprect(sh, rx - rw / 2, IN - 0.25, rx + rw / 2, IN, OAK, "#0E0C09", .5)
    for x in GUNS:
        plan_disc(sh, x, IN - 0.12, 0.05, IRON)
    kprect(sh, IN - 0.1, 2.9, IN, 3.5, ESTATE, "#0E0C09", .5)
    plan_box(sh, *TABLE[:4], OAK)
    plan_box(sh, TABLE[0] + 0.3, TABLE[1], 0.16, 0.16, IRON)
    plan_box(sh, TABLE[0], -2.9, 0.35, 0.35, OAK)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Keep")
    socket_label(sh, *KP(3.6, 4.4), "HANDGUNS")
    socket_label(sh, *KP(3.6, -4.6), "GUARDS' TABLE")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 3.31 · all open"),
                    ("CLEAR CROSS", "dashed · the bridge crosses it at 2.40 m"),
                    ("STAIR", "sandstone, 2 × 4 steps, foot facing open floor"),
                    ("GALLERY", "oak, NW at 2.60 m · chest, gun-loop"),
                    ("LOOT", "L1–L2: gallery chest, guards' table")])
    return sh
