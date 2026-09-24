"""LateCharnelHouse: the charnel house (stands in for BurialVault).

Ossuary shelving in all four quadrants: an oak rack against the north or south wall,
3.00 m long, three shelves, each shelf a stack of long bones laid lengthwise with a
row of skulls along its front edge. A small altar with a candle in the NE corner
between the rack and the east wall; a charnel cart with a shovel in the SW. Section
A-A east-west at y = 0, looking north: the two north racks face-on.
"""
from _late import *

BONE = "#D6CDB6"
RACK_X = (2.2, 5.2)
RACK_D = 0.5
SHELVES = [0.1, 0.8, 1.5]                         # shelf-board heights above the floor
SKULL_R = 0.1
SKULLS = 7
GAP = 3                                            # the skull left out on an anchored shelf
ALTAR = (5.1, 2.9, 0.6, 0.9, 1.0)                  # against the east wall
CART = (-3.6, -3.0, 1.2, 0.7)
SKULL_Y = IN - RACK_D + 0.12
ANCHORED = {(1, 1): 1.5, (-1, 1): 0.8, (1, -1): 0.8, (-1, -1): 1.5}   # which shelf carries loot, per rack
ANCHORS = [(sx * 3.7, sy * SKULL_Y, FZ + z + 0.2) for (sx, sy), z in ANCHORED.items()]


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("bone", BONE), ("iron", IRON), ("linen", LINEN),
            ("soot", SOOT), ("madder", ESTATE)]
    sh = room_sheet("LateMedieval", "Crypt", "The Charnel House", mats, "≤ 5k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE CHARNEL, LOOKING NORTH")
    kit_glow(sh, 4.8, FZ + 1.2, "#8A6A30", rx=220, ry=150, strength=.3)

    # ---- section ----
    kit_slab(sh, "#2A241C")
    kit_back_wall(sh, "Crypt", SAND, soot=SOOT, trim="#3A342A", soot_depth=0.9)
    ashlar_courses(sh, "Crypt")
    for sgn in (-1, 1):
        a, b = sorted((sgn * RACK_X[0], sgn * RACK_X[1]))
        for x in (a, b - 0.08):
            kerect(sh, x, FZ, x + 0.08, FZ + 2.1, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .7)
        for z in SHELVES:
            kerect(sh, a, FZ + z, b, FZ + z + 0.05, OAK, darken(OAK, .6), .6)
            kerect(sh, a + 0.1, FZ + z + 0.05, b - 0.1, FZ + z + 0.2, f"url(#{sh.lin(BONE, 'v', .3, .5)})",
                   darken(BONE, .5), .6)
            for k in range(1, 12):
                xx = a + 0.1 + k * (b - a - 0.2) / 12
                sh.line(*KE(xx, FZ + z + 0.05), *KE(xx, FZ + z + 0.2), darken(BONE, .35), .6)
            for k in range(SKULLS):
                if k == GAP and ANCHORED[(sgn, 1)] == z:
                    continue
                xx = a + 0.25 + k * (b - a - 0.5) / (SKULLS - 1)
                cx, cy = KE(xx, FZ + z + 0.2 + SKULL_R)
                sh.circle(cx, cy, SKULL_R * SK, f"url(#{sh.lin(BONE, 'h', .35, .55)})", darken(BONE, .5), .6)
                sh.circle(cx - 2, cy, 1.4, "#14120E")
                sh.circle(cx + 2, cy, 1.4, "#14120E")
    ax, ay, aw, al, ah = ALTAR
    kerect(sh, IN - aw, FZ, IN, FZ + ah, f"url(#{sh.lin(SAND, 'h', .25, .5)})", darken(SAND, .6), .9)
    candle_stand(sh, ax, base=FZ + ah, h=0.25)
    kit_cut_walls(sh, "Crypt")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(3.3, FZ + 1.5 + 0.3), "SKULL ROWS", "seven to a shelf, along the front"),
        (*KE(4.5, FZ + 0.9), "LONG-BONE STACKS", "laid lengthwise behind them"),
        (*KE(IN - 0.3, FZ + 1.3), "ALTAR", "a candle for the dead"),
    ], 610, 190, 330, slope=1.0)
    sh.callouts([
        (*KE(-3.7, FZ + 0.4), "OSSUARY RACK", "oak, 3.00 m, three shelves · loot"),
    ], 330, 260, 260, anchor="end")
    kit_clear_note(sh, "Crypt", x=0.0, text="3.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Crypt", floor="#1E1B16", wall="#3A332A", trim="#3A342A")
    for sx in (-1, 1):
        for sy in (-1, 1):
            a, b = sorted((sx * RACK_X[0], sx * RACK_X[1]))
            y0, y1 = sorted((sy * IN, sy * (IN - RACK_D)))
            kprect(sh, a, y0, b, y1, OAK, "#0E0C09", .6)
            kprect(sh, a + 0.1, y0 + 0.08, b - 0.1, y1 - 0.08, BONE)
            for k in range(SKULLS):
                xx = a + 0.25 + k * (b - a - 0.5) / (SKULLS - 1)
                plan_disc(sh, xx, sy * SKULL_Y, SKULL_R, lighten(BONE, .05))
    plan_box(sh, ax, ay, aw, al, SAND)
    plan_disc(sh, ax, ay, 0.06, LINEN)
    plan_box(sh, *CART, OAK)
    plan_box(sh, CART[0], CART[1], 1.0, 0.5, BONE)
    for dy in (-0.42, 0.42):
        plan_box(sh, CART[0] + 0.2, CART[1] + dy, 0.6, 0.08, darken(OAK, .2))
    kprect(sh, CART[0] - 0.6 - 0.8, CART[1] - 0.02, CART[0] - 0.6, CART[1] + 0.02, OAK)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Crypt")
    socket_label(sh, *KP(-3.6, -2.2), "CHARNEL CART")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.16 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("RACKS", "4 ossuary racks, N and S walls, 3 shelves each"),
                    ("ROOM", "altar and candle NE · charnel cart SW"),
                    ("LOOT", "L1–L4: a gap in a skull row, each rack")])
    return sh
