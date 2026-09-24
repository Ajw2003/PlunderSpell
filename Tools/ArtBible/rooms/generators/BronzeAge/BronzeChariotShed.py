"""BronzeChariotShed: the chariot shed and stable (stands in for StableBlock).

Section A-A east-west at y = 0, looking north: the war chariot in profile in the
NW (four-spoked wheel, car with its wicker screen, the pole running east to a
forked stand), two horse stalls with clay mangers against the north wall in the
NE. Fodder and the harness rail are in the south half (plan).
"""
import math

from _bronze import *

ANCHORS = [(-4.4, 3.8, 0.91), (4.6, 5.2, 0.95), (3.7, -4.6, 1.50), (-4.4, -4.9, 0.90)]
WICKER = "#9C7A48"
HAY = "#C8A868"


def wheel_face(sh, x, z_axle, r, col=CYP, spokes=4):
    """A spoked wheel seen face-on: tyre, felloe, hub and spokes."""
    cx, cy = KE(x, z_axle)
    R = r * SK
    sh.circle(cx, cy, R, "none", darken(col, .5), 5)
    sh.circle(cx, cy, R - 3, "none", col, 3)
    for k in range(spokes):
        a = math.pi * k / spokes * 2 + math.pi / 4
        sh.line(cx, cy, cx + math.cos(a) * (R - 3), cy + math.sin(a) * (R - 3), col, 3)
    sh.circle(cx, cy, 5, darken(col, .2), "#0E0C09", 1)


def build():
    mats = [("plaster", OCHRE), ("mud-brick", MUD), ("cypress", CYP), ("wicker", WICKER),
            ("clay", TERRA), ("hay", HAY), ("bronze", BRONZE)]
    sh = room_sheet("BronzeAge", "OuterBailey", "The Chariot Shed", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE SHED, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 1.6, "#5A4A30", rx=380, ry=200, strength=.3)

    # ---- section ----
    kit_slab(sh, "#6A4E36")
    kit_back_wall(sh, "OuterBailey", OCHRE, trim=MUD, soot_depth=0.5)
    # Mud-brick shows through where the plaster has fallen, low on the walls.
    for x0, x1 in ((-5.2, -4.1), (2.9, 3.7)):
        kerect(sh, x0, FZ + 0.1, x1, FZ + 0.6, MUD, op=.55)
    # NE: two stalls against the north wall, partitions and clay mangers with hay.
    for x in (2.2, 3.8):
        kerect(sh, x - 0.06, FZ, x + 0.06, FZ + 1.30, f"url(#{sh.lin(CYP, 'h', .25, .5)})", darken(CYP, .6), .7)
    for x in (3.0, 4.6):
        kerect(sh, x - 0.6, FZ, x + 0.6, FZ + 0.60, f"url(#{sh.lin(TERRA, 'v', .25, .5)})", darken(TERRA, .6), .8)
        kerect(sh, x - 0.5, FZ + 0.60, x + 0.5, FZ + 0.66, HAY, darken(HAY, .5), .6)
    # NW: the chariot in profile, pole east to a forked stand.
    kerect(sh, -4.85, FZ + 0.55, -3.95, FZ + 0.61, f"url(#{sh.lin(CYP, 'v', .25, .5)})", darken(CYP, .6), .8)
    screen = [KE(-3.95, FZ + 0.61), KE(-3.95, FZ + 1.10), KE(-4.2, FZ + 1.18), KE(-4.85, FZ + 0.96), KE(-4.85, FZ + 0.61)]
    sh.path(smooth_path(screen, tension=.2), f"url(#{sh.lin(WICKER, 'h', .3, .5)})", darken(WICKER, .6), 1)
    for k in range(5):
        sh.line(*KE(-4.8 + k * 0.18, FZ + 0.62), *KE(-4.8 + k * 0.18, FZ + 1.0 + k * 0.03), darken(WICKER, .3), .8, op=.7)
    kerect(sh, -3.95, FZ + 0.58, -2.0, FZ + 0.66, CYP, darken(CYP, .6), .7)
    kerect(sh, -2.14, FZ, -2.06, FZ + 0.58, CYP, darken(CYP, .6), .6)
    sh.path(f"M{f(KE(-2.25, FZ + 0.72)[0])} {f(KE(0, FZ + 0.72)[1])} L{f(KE(-2.1, FZ + 0.6)[0])} {f(KE(0, FZ + 0.6)[1])} "
            f"L{f(KE(-1.95, FZ + 0.72)[0])} {f(KE(0, FZ + 0.72)[1])}", "none", CYP, 3)
    wheel_face(sh, -4.4, FZ + 0.45, 0.45)
    kit_cut_walls(sh, "OuterBailey")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(3.0, FZ + 0.4), "CLAY MANGERS", "hay in them · loot"),
        (*KE(2.2, FZ + 1.1), "STALL PARTITIONS", "cypress, 1.30 m"),
    ], 610, 240, 320, slope=1.0)
    sh.callouts([
        (*KE(-4.4, FZ + 0.9), "WAR CHARIOT", "wicker car · loot inside"),
        (*KE(-4.4, FZ + 0.25), "FOUR-SPOKED WHEEL", "0.90 m, cypress, bronze nave"),
        (*KE(-2.1, FZ + 0.3), "POLE STAND", "a forked post at 0.60 m"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "OuterBailey", x=0.0, text="3.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "OuterBailey", floor="#2E2519", wall="#3A332A", trim=MUD)
    plan_box(sh, -4.4, 3.8, 0.9, 1.1, WICKER)
    plan_box(sh, -4.4, 3.8, 0.12, 1.4, darken(CYP, .2))
    for y in (3.15, 4.45):
        plan_box(sh, -4.4, y, 0.9, 0.08, CYP)
    plan_box(sh, -2.975, 3.8, 1.95, 0.08, CYP)
    plan_box(sh, -2.2, 3.8, 0.08, 1.0, CYP)
    for x in (2.2, 3.8):
        plan_box(sh, x, 4.4, 0.12, 2.2, CYP)
    for x in (3.0, 4.6):
        plan_box(sh, x, 5.2, 1.2, 0.5, TERRA)
        plan_box(sh, x, 5.2, 1.0, 0.36, HAY)
    for x, y in ((3.0, -4.6), (4.4, -4.6), (3.7, -4.6), (4.6, -3.2)):
        plan_box(sh, x, y, 1.2, 0.8, HAY)
    plan_box(sh, -5.4, -3.6, 0.1, 2.0, CYP)
    for y in (-4.3, -3.8, -3.3, -2.8):
        plan_box(sh, -5.3, y, 0.08, 0.1, "#6E4A3A")
    plan_box(sh, -4.4, -4.9, 1.0, 0.6, CYP)
    kprect(sh, -4.9, -4.93, -3.9, -4.87, BRONZE)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "OuterBailey")
    socket_label(sh, *KP(-3.6, 2.4), "CHARIOT")
    socket_label(sh, *KP(3.8, 3.0), "STALLS")
    socket_label(sh, *KP(3.8, -3.7), "FODDER")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.59 · all open"),
                    ("CLEAR CROSS", "dashed · the pole stops at x −1.95"),
                    ("CHARIOT", "NW, axle N–S, pole east on a stand"),
                    ("STABLE", "2 stalls NE · fodder SE · tack SW"),
                    ("LOOT", "L1–L4: chariot car, manger, fodder, tack chest")])
    return sh
