"""BronzeWallStraight: the curtain wall (stands in for WallStraight).

A cyclopean mass 2.40 m thick on the south edge of the cell, three courses of
huge irregular limestone blocks to 4.10 m, the plastered wall-walk on top and a
1.10 m mud-brick breastwork with rounded merlons on the outer edge, sling slots
in every other merlon, to the shared 5.20 m CurtainWall height. The blocks are
cyclopean.blocks(-6.05, 6.05, seed=0), the builder's own layout. South elevation
(outside face, looking north) left; plan right; a small N–S section above.
"""
from _curtain import *

DEPTH = 2.4
BLOCKS = cy.blocks(-KH, KH, WALK_Z, seed=0)
ANCHORS = []


def build():
    mats = [("limestone", STONE), ("mud-brick", MUD), ("plaster", PLASTER), ("soot", SOOT),
            ("earth", "#4A3C2C")]
    sh = room_sheet("BronzeAge", "CurtainWall", "The Cyclopean Wall", mats, "≤ 1.2k tris (kit)",
                    "SOUTH ELEVATION · OUTSIDE FACE, LOOKING NORTH")

    # ---- elevation ----
    ground(sh)
    elev_blocks(sh, BLOCKS)
    elev_parapet(sh, -KH, KH)
    khuman(sh, -0.6, floor=0.0)
    ms = merlon_centres(-KH, KH)
    sh.callouts([
        (*KE(ms[-2], 5.0), "ROUNDED MERLONS", "0.60 m, 1.20 m pitch, to 5.20 m"),
        (*KE(ms[-1], WALK_Z + 0.25), "MUD-BRICK BREASTWORK", "0.40 m thick, 1.10 m tall"),
        (*KE(3.5, 2.2), "CYCLOPEAN BLOCKS", "3 courses to 4.10 m, 1.1–2.4 m long"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(ms[2], WALK_Z + 0.6), "SLING SLOT", "0.12 × 0.50 m, every other merlon"),
        (*KE(-4.0, 0.8), "SET-BACK FACES", "stones recessed 0–0.07 m"),
    ], 470, 200, 270, anchor="end")
    mini_section(sh, 80, 330, [
        (0, 0, DEPTH, WALK_Z, STONE),
        (PARAPET_IN + PARAPET_T, WALK_Z, DEPTH - 0.2, WALK_Z + 0.1, PLASTER),
        (PARAPET_IN, WALK_Z, PARAPET_IN + PARAPET_T, 5.2, MUD),
    ], "SECTION B–B", width=DEPTH)

    # ---- plan ----
    kit_plan(sh, "CurtainWall", floor="#3A3226", wall="#3A332A", shell=False)
    plan_run(sh, BLOCKS, DEPTH)
    plan_walk(sh, -KH, KH, DEPTH)
    plan_parapet(sh, -KH, KH)
    for k, u in enumerate(ms):
        if k % 2 == 0:
            plan_strip(sh, u - 0.06, u + 0.06, PARAPET_IN - 0.02, PARAPET_IN + 0.04, "south", "#14100C")
    wall_labels(sh)
    socket_label(sh, *KP(0, -4.45), "WALL-WALK 4.10 · 1.80 m WIDE")
    kit_legend(sh, [("WALL", "cyclopean, 2.40 m thick on the south edge"),
                    ("WALK", "plastered, 4.10 m, 1.80 m wide, open both ends"),
                    ("PARAPET", "mud-brick, 1.10 m, rounded merlons, outer edge"),
                    ("JOINS", "runs to both cell edges; any neighbour meets it"),
                    ("LOOT", "none: a wall piece carries no loot")])
    return sh
