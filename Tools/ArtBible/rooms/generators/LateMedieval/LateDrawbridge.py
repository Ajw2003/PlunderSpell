"""LateDrawbridge: the bascule bridge (stands in for Drawbridge).

The machicolated wall on the south edge with a gate 3.00 m wide and 3.40 m high
between sandstone jambs under a lintel; the walk, frieze, corbels and parapet run on
unbroken over it. The oak deck lies lowered, running 5.00 m inward from the gate like
the kit's own drawbridge. Two oak counterweight arms (flèches) pivot on posts on
the walk above the gate: their south ends, weighted, stand up over the parapet,
their north ends hang over the deck's inner end on chains. Let the counterweights
drop and the deck's inner end rises, closing the gate from inside. South elevation
left; plan right; a N-S section through the gate and an arm above.
"""
import math

from _late_wall import *

GATE_W, GATE_H = 3.0, 3.4
JAMB = 0.4
DECK = (3.0, 0.2, 5.2, 0.22)                 # width, d from, d to, thickness
ARM_X = 1.3
ARM_S = (0.15, 5.7)                          # the south end: d, z
ARM_N = (5.2, 2.4)                           # the north end: d, z
PIVOT_D = 1.2
LOOPS = [-4.2, 4.2]
ANCHORS = []


def pivot_z():
    (d0, z0), (d1, z1) = ARM_S, ARM_N
    return z0 + (z1 - z0) * (PIVOT_D - d0) / (d1 - d0)


def build():
    mats = [("sandstone", SAND), ("brick", BRICK), ("oak", OAK), ("iron", IRON), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "CurtainWall", "The Bascule Bridge", mats, "≤ 1.6k tris (kit)",
                    "SOUTH ELEVATION · OUTSIDE FACE, LOOKING NORTH")

    # ---- elevation ----
    ground(sh)
    g = GATE_W / 2
    for u0, u1 in ((-KH, -g - JAMB), (g + JAMB, KH)):
        elev_ashlar(sh, u0, u1, FRIEZE[0])
    for sgn in (-1, 1):
        a, b = sorted((sgn * g, sgn * (g + JAMB)))
        kerect(sh, a, 0, b, GATE_H, f"url(#{sh.lin(SAND, 'h', .3, .5)})", darken(SAND, .6), 1)
    kerect(sh, -g, 0, g, GATE_H, "#14120E")
    kerect(sh, -g, 0, g, DECK[3], OAK, darken(OAK, .6), .6)
    kerect(sh, -g - JAMB, GATE_H, g + JAMB, FRIEZE[0], f"url(#{sh.lin(SAND, 'v', .25, .5)})", darken(SAND, .6), 1)
    for k in range(7):
        x = -g - JAMB + k * (2 * g + 2 * JAMB) / 6
        sh.line(*KE(x, GATE_H), *KE(x, FRIEZE[0]), darken(SAND, .4), .7)
    elev_crown(sh, -KH, KH, loops=LOOPS)
    for sgn in (-1, 1):
        x = sgn * ARM_X
        kerect(sh, x - 0.1, ARM_S[1] - 0.1, x + 0.1, ARM_S[1] + 0.1, OAK, darken(OAK, .6), .7)
        kerect(sh, x - 0.225, ARM_S[1] + 0.1, x + 0.225, ARM_S[1] + 0.55, f"url(#{sh.lin(IRON, 'h', .35, .55)})",
               "#0E0C09", .8)
    khuman(sh, -3.0, floor=0.0)
    sh.callouts([
        (*KE(ARM_X, ARM_S[1] + 0.35), "COUNTERWEIGHT", "iron-bound, on each arm's south end"),
        (*KE(3.8, 4.9), "WALL CROWN", "runs on unbroken over the gate"),
        (*KE(1.0, 3.7), "LINTEL", "sandstone, the walk carried over it"),
        (*KE(0.4, 0.1), "DECK", "oak, lowered, 5.00 m inward"),
    ], 610, 170, 350, slope=0.0)
    sh.callouts([
        (*KE(-g - 0.2, 2.0), "GATE", "3.00 × 3.40 m between jambs"),
    ], 540, 330, 330, anchor="end")
    # The N-S section through an arm.
    ox, oy, sc = 70, 360, 20
    rects = wall_section_rects()
    rects[0] = (FACE, GATE_H, FACE + MASS, WALK_Z, SAND)
    rects.append((DECK[1], 0, DECK[2], DECK[3], OAK))
    rects.append((PIVOT_D - 0.1, WALK_Z, PIVOT_D + 0.1, pivot_z(), OAK))
    mini_section(sh, ox, oy, rects, "SECTION B–B · THROUGH AN ARM", width=DECK[2] + 0.2, scale=sc)
    (d0, z0), (d1, z1) = ARM_S, ARM_N
    sh.line(ox + d0 * sc, oy - z0 * sc, ox + d1 * sc, oy - z1 * sc, OAK, 4)
    sh.add(f'<rect x="{f(ox + (d0 - 0.075) * sc)}" y="{f(oy - (z0 + 0.55) * sc)}" width="{f(0.45 * sc)}" '
           f'height="{f(0.45 * sc)}" fill="{IRON}" stroke="#0E0C09" stroke-width=".8"/>')
    sh.line(ox + d1 * sc, oy - z1 * sc, ox + d1 * sc, oy - DECK[3] * sc, IRON, 1.4)
    sh.circle(ox + PIVOT_D * sc, oy - pivot_z() * sc, 3, IRON)

    # ---- plan ----
    kit_plan(sh, "CurtainWall", floor="#3A3226", wall="#3A332A", shell=False)
    for u0, u1 in ((-KH, -g), (g, KH)):
        plan_strip(sh, u0, u1, FACE, FACE + MASS, "south", lighten(SAND, .05), "#2A251D", .6)
    plan_strip(sh, -g, g, FACE, FACE + MASS, "south", SAND, "#2A251D", .6, op=.5)
    plan_run(sh, -KH, KH, "south")
    kprect(sh, -g, -KH + DECK[1], g, -KH + DECK[2], OAK, "#0E0C09", .8)
    for k in range(1, 10):
        x = -g + k * GATE_W / 10
        sh.line(*KP(x, -KH + FACE + MASS + 1.0), *KP(x, -KH + DECK[2]), darken(OAK, .4), .6)
    for sgn in (-1, 1):
        x = sgn * ARM_X
        kprect(sh, x - 0.1, -KH + ARM_S[0], x + 0.1, -KH + ARM_N[0], lighten(OAK, .15), "#0E0C09", .6)
        plan_box(sh, x, -KH + ARM_S[0] + 0.1, 0.45, 0.45, IRON)
        for dx in (-0.2, 0.2):
            plan_box(sh, x + dx, -KH + PIVOT_D, 0.14, 0.14, darken(OAK, .3))
        plan_disc(sh, x, -KH + ARM_N[0], 0.08, IRON)
    socket_label(sh, *KP(0, -KH + DECK[2] + 0.4), "DECK · LOWERED")
    socket_label(sh, *KP(0, 3.4), "BAILEY (NORTH)")
    kit_legend(sh, [("GATE", "3.00 × 3.40 m, deck lowered, open"),
                    ("ARMS", "2 oak flèches pivoting on the walk, chains"),
                    ("WALK", "stone + timber, unbroken over the gate"),
                    ("LOOPS", "2 keyhole gun-loops beside the gate"),
                    ("LOOT", "none: a wall piece carries no loot")])
    return sh
