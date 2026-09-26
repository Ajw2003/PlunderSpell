"""LateWallCorner: the corner tower (stands in for WallCorner).

The machicolated wall on the south and west edges meeting at an octagonal sandstone
tower in the SW corner, its south and west flats on the walls' face line (0.45 m in
from the cell edge). The drum rises to 4.60 m; a brick frieze, corbels on its five
outward flats and a parapet ring carry the crown out to 6.20 m, and a conical tile
roof rises to 8.60 m. Doorways from both walks lead in through the north and east
flats; keyhole gun-loops in the south and west flats. South elevation left; plan
right; a small N-S section through the tower above.
"""
import math

from _late_wall import *

R = 2.2                                    # drum circumradius (flats face the axes)
A = R * math.cos(math.radians(22.5))       # apothem, 2.03
C = -KH + FACE + A                         # tower centre (x and y), -3.57
FLAT = R * math.sin(math.radians(22.5))    # half a flat, 0.84
LIFT = 1.6                                 # the tower's crown sits this much above the walls'
RP = 2.6                                   # parapet ring circumradius
RR = 2.65                                  # roof base circumradius
ROOF_TOP = LIFT + LOW_TOP + 2.4
RUN0 = C + FLAT                            # the runs start at the south / west flat's corner
LOOPS = [2.1]
ANCHORS = []
TILE = BRICK


def build():
    mats = [("sandstone", SAND), ("brick", BRICK), ("roof tile", TILE), ("oak", OAK), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "CurtainWall", "The Corner Tower", mats, "≤ 2k tris (kit)",
                    "SOUTH ELEVATION · OUTSIDE FACE, LOOKING NORTH")

    # ---- elevation ----
    ground(sh)
    ap = RP * math.cos(math.radians(22.5))
    # The drum: the south flat face-on between two foreshortened diagonal flats.
    for x0, x1, shade in ((C - A, C - FLAT, .18), (C - FLAT, C + FLAT, 0.0), (C + FLAT, C + A, .18)):
        elev_ashlar(sh, x0, x1, LIFT + FRIEZE[0], col=darken(SAND, shade))
    kerect(sh, C - A - 0.02, LIFT + FRIEZE[0], C + A + 0.02, LIFT + FRIEZE[1], f"url(#{sh.lin(BRICK, 'v', .25, .5)})",
           darken(BRICK, .6), .8)
    kerect(sh, C - A, LIFT + CORBEL[0], C + A, LIFT + CORBEL[2], "#14120E", op=.55)
    for u in (C - A + 0.3, C - FLAT + 0.2, C, C + FLAT - 0.2, C + A - 0.3):
        kerect(sh, u - 0.15, LIFT + CORBEL[0], u + 0.15, LIFT + CORBEL[1], darken(SAND, .08), darken(SAND, .6), .6)
        kerect(sh, u - 0.18, LIFT + CORBEL[1], u + 0.18, LIFT + CORBEL[2], SAND, darken(SAND, .6), .6)
    kerect(sh, C - ap, LIFT + CORBEL[2], C + ap, LIFT + LOW_TOP, f"url(#{sh.lin(SAND, 'v', .2, .5)})", darken(SAND, .6), 1)
    ar = RR * math.cos(math.radians(22.5))
    roof = poly_path([KE(C - ar, LIFT + LOW_TOP), KE(C + ar, LIFT + LOW_TOP), KE(C, ROOF_TOP)])
    sh.path(roof, f"url(#{sh.lin(TILE, 'h', .3, .55)})", darken(TILE, .6), 1.2)
    for k in range(1, 5):
        z = LIFT + LOW_TOP + k * 0.45
        w = ar * (1 - (z - LIFT - LOW_TOP) / (ROOF_TOP - LIFT - LOW_TOP))
        sh.line(*KE(C - w, z), *KE(C + w, z), darken(TILE, .35), .8, op=.7)
    sh.line(*KE(C, ROOF_TOP), *KE(C, ROOF_TOP + 0.3), IRON, 1.6)
    keyhole(sh, C, LOOP_Z)
    # The run's face stands in front of the tower's receding east flat, so it is drawn after.
    elev_ashlar(sh, RUN0, KH, FRIEZE[0])
    elev_crown(sh, RUN0, KH, loops=LOOPS)
    khuman(sh, 0.4, floor=0.0)
    sh.callouts([
        (*KE(C + 0.4, ROOF_TOP - 1.0), "CONICAL ROOF", "tile, 8 facets, to 8.60 m"),
        (*KE(C + 1.2, LIFT + 4.4), "TOWER CROWN", "frieze, corbels, parapet ring to 6.20 m"),
        (*KE(4.0, 4.9), "WALL CROWN", "corbelled parapet, merlons to 5.20 m"),
        (*KE(C + 0.6, 2.4), "OCTAGONAL DRUM", "r 2.20 m, sandstone"),
        (*KE(2.1, 1.6), "KEYHOLE GUN-LOOP", "one per run, one per outer flat"),
    ], 610, 160, 360, slope=0.0)

    # ---- plan ----
    kit_plan(sh, "CurtainWall", floor="#3A3226", wall="#3A332A", shell=False)
    plan_run(sh, RUN0, KH, "south")
    plan_run(sh, RUN0, KH, "west")
    pts = [KP(C + RR * math.cos(math.radians(22.5 + 45 * k)), C + RR * math.sin(math.radians(22.5 + 45 * k)))
           for k in range(8)]
    sh.path(poly_path(pts), TILE, "#2A251D", .8)
    for p in pts:
        sh.line(*p, *KP(C, C), darken(TILE, .45), .8)
    for u in LOOPS:
        plan_strip(sh, u - 0.08, u + 0.08, FACE - 0.03, FACE + 0.3, "south", "#0E0C09")
        plan_strip(sh, u - 0.08, u + 0.08, FACE - 0.03, FACE + 0.3, "west", "#0E0C09")
    for x, y, w, d in ((C + A, C, 0.12, 0.9), (C, C + A, 0.9, 0.12)):
        plan_box(sh, x, y, w, d, "#0E0C09")
    socket_label(sh, *KP(C, C - 0.2), "ROOF")
    socket_label(sh, *KP(2.2, -4.6), "WALK 4.00")
    socket_label(sh, *KP(-4.6, 2.2), "WALK 4.00")
    socket_label(sh, *KP(2.6, 2.6), "BAILEY (NORTH-EAST)")
    kit_legend(sh, [("WALLS", "machicolated runs on the south and west edges"),
                    ("TOWER", "octagonal, r 2.20 m, crown 6.20 m, roof 8.60 m"),
                    ("DOORS", "from both walks into the N and E flats"),
                    ("LOOPS", "keyholes: one per run, S and W flats"),
                    ("LOOT", "none: a wall piece carries no loot")])
    return sh
