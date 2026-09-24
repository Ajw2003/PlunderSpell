"""LateBarbican: the crooked barbican (stands in for GatehouseModule), the art bible's
Crooked Barbican (docs/art/late.md) built to the kit.

The machicolated south wall with a gate 2.60 × 3.00 m between two octagonal towers
(r 1.80 m, conical tile roofs), their south flats on the wall's face line. Inside,
the passage runs north for only 1.7 m before a baffle wall across its end forces it
west, between the west tower and the baffle: nobody gets a straight run in. An east
side wall closes the first leg's east side and carries, with the wall and the west
tower, an oak deck over the first leg at walk level, broken by two murder-hole slots;
the hot-sand cauldron and an iron fire basket stand on the walk above the gate. The
oak leaves stand folded back inside. Kit overrides: the art bible's enclosed L with its
guardroom and vault becomes this open dog-leg (the cell is open to the sky and the
bailey), and its passage turns west rather than east. South elevation left; plan right.
"""
import math

from _late_wall import *

TR = 1.8                                           # tower circumradius
TAPO = TR * math.cos(math.radians(22.5))           # 1.66
TFLAT = TR * math.sin(math.radians(22.5))          # 0.69
TX = 3.2                                           # towers at x ±3.20
TY = -KH + FACE + TAPO                             # -3.94
GATE_W, GATE_H = 2.6, 3.0
INNER = -KH + FACE + MASS                          # the wall's inner face, y -4.00
EAST_WALL = (1.5, 1.9, INNER, 0.9, 3.8)            # x0, x1, y0, y1, height
BAFFLE = (-3.5, 1.9, 0.5, 0.9, 3.0)                # x0, x1, y0, y1, height
DECK = [(-4.0, -3.3), (-3.0, -2.6), (-2.3, -2.0)]  # strips (y0, y1): the gaps are the murder-holes
DECK_X = (-1.5, 1.9)
DECK_Z = (3.8, 4.0)
CAULDRON = (0.9, -4.4)
BASKET = (-0.8, -4.4)
LEAF = (1.35, 1.47, -3.95, -2.65, 3.0)             # |x| from..to, y from..to, height
ANCHORS = []


def build():
    mats = [("sandstone", SAND), ("brick", BRICK), ("roof tile", BRICK), ("oak", OAK), ("iron", IRON),
            ("fire", FIRE), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "CurtainWall", "The Crooked Barbican", mats, "≤ 2.4k tris (kit)",
                    "SOUTH ELEVATION · OUTSIDE FACE, LOOKING NORTH")
    kit_glow(sh, -0.8, 5.4, FIRE, rx=160, ry=120, strength=.3)

    # ---- elevation ----
    ground(sh)
    g = GATE_W / 2
    c = TX - TFLAT                                   # where the runs meet the towers' south flats
    # The fire basket's flame shows over the parapet (behind it, on the walk).
    fx, fy = KE(BASKET[0], 5.35)
    sh.path(f"M{f(fx - 9)} {f(fy + 14)} q2 -16 9 -24 q7 10 9 24 z", FIRE, op=.85)
    # Through the gate: the leaves folded back, then the baffle blocking the view.
    kerect(sh, -g, 0, g, GATE_H, f"url(#{sh.lin(darken(SAND, .45), 'v', .2, .5)})", darken(SAND, .7), .8)
    for sgn in (-1, 1):
        kerect(sh, sgn * g - (0.12 if sgn > 0 else 0), 0, sgn * g + (0 if sgn > 0 else 0.12), GATE_H, darken(OAK, .3))
    sh.text(*KE(0, 1.5), "BAFFLE — TURNS WEST", 8.5, "#DCD2BA", "middle")
    for sgn in (-1, 1):
        elev_oct_tower(sh, sgn * TX, TR)
    for u0, u1 in ((-KH, -TX - TFLAT), (TX + TFLAT, KH), (-c, -g), (g, c)):
        elev_ashlar(sh, u0, u1, FRIEZE[0])
    elev_ashlar(sh, -g, g, FRIEZE[0], z0=GATE_H)
    elev_crown(sh, -KH, -TX - TFLAT, loops=(-5.0,))
    elev_crown(sh, TX + TFLAT, KH, loops=(5.0,))
    elev_crown(sh, -c, c)
    khuman(sh, -1.9, floor=0.0)
    # The tall roofs leave only the band above them free for labels.
    sh.callouts([
        (*KE(TX + 0.4, 7.0), "CONICAL ROOF", "tile, to 8.60 m, on each tower"),
        (*KE(TX + 1.5, 5.9), "TOWER CROWN", "frieze, corbels, parapet ring"),
    ], 610, 125, 185, slope=0.0)
    sh.callouts([
        (*KE(-TX, 1.6), "KEYHOLE GUN-LOOP", "in each tower's south flat"),
        (*KE(-0.9, 0.6), "GATE + BAFFLE", "2.60 × 3.00 m, baffle beyond"),
        (*KE(BASKET[0], 5.4), "FIRE BASKET", "on the walk, the deck's light"),
    ], 470, 125, 215, anchor="end")

    # ---- plan ----
    kit_plan(sh, "CurtainWall", floor="#3A3226", wall="#3A332A", shell=False)
    plan_run(sh, -KH, -TX - TFLAT, "south")
    plan_run(sh, TX + TFLAT, KH, "south")
    plan_run(sh, -c, c, "south", walk=False)
    plan_strip(sh, -g, g, FACE, FACE + MASS, "south", darken(SAND, .1), "#2A251D", .6, op=.6)
    ex0, ex1, ey0, ey1, _ = EAST_WALL
    kprect(sh, ex0, ey0, ex1, ey1, SAND, "#2A251D", .7)
    bx0, bx1, by0, by1, _ = BAFFLE
    kprect(sh, bx0, by0, bx1, by1, SAND, "#2A251D", .7)
    for y0, y1 in DECK:
        kprect(sh, DECK_X[0], y0, DECK_X[1], y1, OAK, "#0E0C09", .6)
    for y0, y1 in ((-3.3, -3.0), (-2.6, -2.3)):
        kprect(sh, DECK_X[0], y0, DECK_X[1], y1, "#14120E", FIRE, .8)
    for sgn in (-1, 1):
        plan_oct_roof(sh, sgn * TX, TY, TR)
        x0, x1 = sorted((sgn * LEAF[0], sgn * LEAF[1]))
        kprect(sh, x0, LEAF[2], x1, LEAF[3], darken(OAK, .1))
    plan_disc(sh, CAULDRON[0], CAULDRON[1], 0.3, IRON)
    plan_disc(sh, BASKET[0], BASKET[1], 0.3, IRON)
    plan_disc(sh, BASKET[0], BASKET[1], 0.18, FIRE, None)
    path = [KP(0, -5.4), KP(0, -0.9), KP(-0.6, -0.9), KP(-5.2, -0.9)]
    sh.path(poly_path(path, closed=False), "none", "#DCD2BA", 1.2, extra=' stroke-dasharray="4 3"')
    a = KP(-5.2, -0.9)
    sh.path(f"M{f(a[0])} {f(a[1])} l8 -4 m-8 4 l8 4", "none", "#DCD2BA", 1.2)
    socket_label(sh, *KP(0.2, -1.55), "MURDER-HOLES (DECK)")
    socket_label(sh, *KP(-4.0, 0.0), "LEG 2 → WEST")
    socket_label(sh, *KP(2.6, 3.0), "BAILEY (NORTH)")
    kit_legend(sh, [("GATE", "2.60 × 3.00 m between two octagonal towers"),
                    ("DOG-LEG", "baffle at y 0.5: north 1.7 m, then west"),
                    ("DECK", "oak over leg 1 at walk level, 2 murder-hole slots"),
                    ("WALK", "cauldron and fire basket above the gate"),
                    ("LOOT", "none: a wall piece carries no loot")])
    return sh
