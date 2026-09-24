"""LateBastion: the artillery bulwark (stands in for Bastion).

A low, wide round tower of dressed sandstone (sixteen flats, apothem 3.34 m) whose
south flat lies on the walls' face line, projecting 6.7 m into the bailey. Its
platform is level with the walks at 4.00 m; a parapet ring 0.90 m thick stands on
its rim to 5.00 m, open in three places: a gun embrasure to the south with a low sill,
and a wide gap either side where the walks come in. A ring of gunports pierces the
base. A bombard on a timber bed on the platform points out through the embrasure,
gunstones beside it. The machicolated wall runs on from both flanks to the cell
edges. South elevation left; plan right.
"""
import math

from _late_wall import *

N = 16
R = 3.4                                        # drum circumradius
APO = R * math.cos(math.radians(180 / N))      # 3.34
CY = -KH + FACE + APO                          # centre y, -2.27
PLAT = WALK_Z                                  # platform top, level with the walks
RING = (2.5, 3.4, PLAT + 1.0)                  # parapet inner radius, outer radius, top
SILL = PLAT + 0.4
GAPS = {202.5, 225.0, 315.0, 337.5}            # walk entries
EMBRASURE = 270.0
PORTS = [225.0 + 22.5 * k for k in range(5)]  # gunports on the five outward flats clear of the runs
BED = (0.0, CY - 1.1, 0.7, 1.8, 0.3)           # bombard bed: x, y, w, l, h
RUN0 = 2.6
ANCHORS = []


def facet_angles():
    return [22.5 * k for k in range(N)]


def build():
    mats = [("sandstone", SAND), ("brick", BRICK), ("oak", OAK), ("iron", IRON), ("gunstone", "#8A8474"),
            ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "CurtainWall", "The Artillery Bulwark", mats, "≤ 2k tris (kit)",
                    "SOUTH ELEVATION · OUTSIDE FACE, LOOKING NORTH")

    # ---- elevation ----
    ground(sh)
    # The drum: its flats as vertical bands, lit at the front, darker to the flanks.
    for k in range(N):
        a0, a1 = math.radians(22.5 * k - 11.25), math.radians(22.5 * k + 11.25)
        if math.sin((a0 + a1) / 2) > 0:
            continue
        x0, x1 = sorted((R * math.cos(a0), R * math.cos(a1)))
        shade = .35 * abs(math.cos((a0 + a1) / 2))
        elev_ashlar(sh, x0, x1, PLAT, col=darken(SAND, shade))
    for ang in facet_angles():
        if ang in GAPS or math.sin(math.radians(ang)) > 0.01:
            continue
        a0, a1 = math.radians(ang - 11.25), math.radians(ang + 11.25)
        x0, x1 = sorted((R * math.cos(a0), R * math.cos(a1)))
        top = SILL if ang == EMBRASURE else RING[2]
        kerect(sh, x0, PLAT, x1, top, f"url(#{sh.lin(SAND, 'v', .2, .5)})", darken(SAND, .6), 1)
    # The bombard's muzzle in the embrasure.
    sh.circle(*KE(0, SILL + 0.3), 0.3 * SK, f"url(#{sh.lin(IRON, 'h', .4, .6)})", "#0E0C09", .8)
    sh.circle(*KE(0, SILL + 0.3), 0.2 * SK, "#0E0C09")
    for ang in PORTS:
        x = APO * math.cos(math.radians(ang))
        kerect(sh, x - 0.15, 0.6, x + 0.15, 0.9, "#0E0C09")
    # The runs' faces stand in front of the drum's receding flanks.
    for u0, u1 in ((-KH, -RUN0), (RUN0, KH)):
        elev_ashlar(sh, u0, u1, FRIEZE[0])
        elev_crown(sh, u0, u1, loops=(u0 + 1.6 if u0 < 0 else u1 - 1.6,))
    khuman(sh, -1.8, floor=0.0)
    sh.callouts([
        (*KE(0.3, SILL + 0.3), "BOMBARD", "on the platform, through the embrasure"),
        (*KE(1.2, PLAT + 0.6), "PARAPET RING", "0.90 m thick, to 5.00 m"),
        (*KE(1.9, 0.75), "GUNPORTS", "five in the base, 0.30 m"),
        (*KE(4.6, 4.9), "WALL RUNS", "machicolated, to both cell edges"),
    ], 610, 170, 350, slope=0.0)
    sh.callouts([
        (*KE(-1.2, 2.2), "ROUND BULWARK", "16 flats, apothem 3.34 m, 4.00 m"),
    ], 540, 330, 330, anchor="end")

    # ---- plan ----
    kit_plan(sh, "CurtainWall", floor="#3A3226", wall="#3A332A", shell=False)
    for u0, u1 in ((-KH, -RUN0), (RUN0, KH)):
        plan_run(sh, u0, u1, "south")
    drum = [KP(R * math.cos(math.radians(11.25 + 22.5 * k)), CY + R * math.sin(math.radians(11.25 + 22.5 * k)))
            for k in range(N)]
    sh.path(poly_path(drum), lighten(SAND, .08), "#2A251D", .8)
    for ang in facet_angles():
        if ang in GAPS:
            continue
        a0, a1 = math.radians(ang - 11.25), math.radians(ang + 11.25)
        pts = [(RING[0] * math.cos(a0), CY + RING[0] * math.sin(a0)), (R * math.cos(a0), CY + R * math.sin(a0)),
               (R * math.cos(a1), CY + R * math.sin(a1)), (RING[0] * math.cos(a1), CY + RING[0] * math.sin(a1))]
        col = darken(SAND, .05) if ang == EMBRASURE else darken(SAND, .2)
        sh.path(poly_path([KP(x, y) for x, y in pts]), col, "#2A251D", .6)
    bx, by, bw, bl, bh = BED
    plan_box(sh, bx, by, bw, bl, OAK)
    plan_box(sh, bx, by - 0.1, 0.5, 1.6, IRON)
    for x, y in ((1.2, CY - 0.4), (1.6, CY - 0.1), (1.35, CY + 0.25)):
        plan_disc(sh, x, y, 0.2, "#8A8474")
    socket_label(sh, *KP(0, CY + 1.2), "PLATFORM 4.00")
    socket_label(sh, *KP(4.2, -4.6), "WALK 4.00")
    socket_label(sh, *KP(-4.2, -4.6), "WALK 4.00")
    socket_label(sh, *KP(0, 3.4), "BAILEY (NORTH)")
    kit_legend(sh, [("BULWARK", "round, 16 flats, platform at walk level"),
                    ("PARAPET", "0.90 m ring to 5.00 m, embrasure S, walk gaps"),
                    ("GUNS", "bombard on a bed · 5 gunports at the base"),
                    ("WALLS", "machicolated runs to both cell edges"),
                    ("LOOT", "none: a wall piece carries no loot")])
    return sh
