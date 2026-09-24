"""LateWallStraight: the machicolated curtain (stands in for WallStraight).

A dressed-sandstone wall 1.60 m thick on the south edge, its outer face 0.45 m in
from the cell line, 4.00 m to the stone walk. A brick frieze runs under a row of
two-stage corbels that carry the parapet 0.40 m out over the face, leaving a 0.12 m
machicolation slot between each pair of corbels; the parapet's breastwork rises to
4.60 m with merlons to 5.20 m. Three keyhole gun-loops pierce the face. On the inner
face a timber walk 1.00 m wide on stone corbels widens the stone walk. South
elevation left; plan right; a small N-S section above.
"""
from _late_wall import *

LOOPS = [-3.6, 0.0, 3.6]
ANCHORS = []


def build():
    mats = [("sandstone", SAND), ("brick", BRICK), ("oak", OAK), ("soot", SOOT), ("rubble", RUBBLE)]
    sh = room_sheet("LateMedieval", "CurtainWall", "The Machicolated Wall", mats, "≤ 1.2k tris (kit)",
                    "SOUTH ELEVATION · OUTSIDE FACE, LOOKING NORTH")

    # ---- elevation ----
    ground(sh)
    elev_ashlar(sh, -KH, KH, FRIEZE[0])
    elev_crown(sh, -KH, KH, loops=LOOPS)
    khuman(sh, -1.6, floor=0.0)
    ms = centres(-KH, KH, MERLON_PITCH)
    cs = centres(-KH, KH, CORBEL_PITCH)
    sh.callouts([
        (*KE(ms[-2], 4.9), "MERLONS", "0.80 m at 1.30 m pitch, to 5.20 m"),
        (*KE(cs[-3], 3.75), "MACHICOLATION CORBELS", "two courses, 0.40 m out, 0.90 m pitch"),
        (*KE(4.5, 3.2), "BRICK FRIEZE", "0.40 m band under the corbels"),
        (*KE(3.6, 1.6), "KEYHOLE GUN-LOOP", "0.20 m port under a 0.90 m slit"),
    ], 610, 170, 350, slope=1.0)
    sh.callouts([
        (*KE(-4.0, 1.4), "DRESSED SANDSTONE", "0.45 m courses, 1.60 m thick"),
    ], 540, 365, 365, anchor="end")
    mini_section(sh, 70, 330, wall_section_rects(), "SECTION B–B", width=TIMBER_WALK[1], scale=24, label_above=True)

    # ---- plan ----
    kit_plan(sh, "CurtainWall", floor="#3A3226", wall="#3A332A", shell=False)
    plan_run(sh, -KH, KH)
    for u in LOOPS:
        plan_strip(sh, u - 0.08, u + 0.08, FACE - 0.03, FACE + 0.3, "south", "#0E0C09")
    socket_label(sh, *KP(0, -4.6), "STONE WALK 4.00 · TIMBER WALK N")
    socket_label(sh, *KP(0, 4.0), "BAILEY (NORTH) · OUTSIDE BEYOND THE SOUTH EDGE")
    kit_legend(sh, [("WALL", "sandstone 1.60 m, face 0.45 m inside the cell"),
                    ("CROWN", "brick frieze, corbels, parapet out to the cell line"),
                    ("WALK", "stone 4.00 m + timber 1.00 m on the inner face"),
                    ("LOOPS", "3 keyhole gun-loops in the face"),
                    ("LOOT", "none: a wall piece carries no loot")])
    return sh
