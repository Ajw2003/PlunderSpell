"""BronzeWallCorner: the corner tower (stands in for WallCorner).

The 2.40 m cyclopean wall on the south and west edges, meeting at a square
cyclopean tower 4.40 × 4.40 m in the SW corner whose top, at 5.50 m, stands 1.40 m
above the walk; its mud-brick parapet runs on its two outer sides only. Three stone
steps on each walk climb the last 1.40 m onto the tower. Blocks are
cyclopean.blocks(): the tower seed 3, the south run seed 1, the west run seed 2,
the builder's own layout. South elevation left (the west run is hidden behind the
tower and the south run); plan right; a small N–S section through the tower above.
"""
from _curtain import *

DEPTH = 2.4
T = 4.4                                   # the tower's side
TOP = 5.5
TE = -KH + T                              # the tower's east (and north) face, -1.65
TOWER = cy.blocks(-KH, TE, TOP, seed=3)
SOUTH = cy.blocks(TE, KH, WALK_Z, seed=1)
WEST = cy.blocks(TE, KH, WALK_Z, seed=2)
STEP_RUN = 0.35
STEP_RISE = (TOP + 0.1 - (WALK_Z + 0.1)) / 4           # four rises, the last onto the tower
ANCHORS = []


def build():
    mats = [("limestone", STONE), ("mud-brick", MUD), ("plaster", PLASTER), ("ashlar", "#9A8C72"),
            ("soot", SOOT)]
    sh = room_sheet("BronzeAge", "CurtainWall", "The Corner Tower", mats, "≤ 2.4k tris (kit)",
                    "SOUTH ELEVATION · OUTSIDE FACE, LOOKING NORTH")

    # ---- elevation ----
    ground(sh)
    elev_blocks(sh, SOUTH)
    elev_parapet(sh, TE, KH)
    elev_blocks(sh, TOWER, lit=lighten(STONE, .06))
    elev_parapet(sh, -KH, TE, base=TOP)
    khuman(sh, 0.8, floor=0.0)
    ms = merlon_centres(TE, KH)
    sh.callouts([
        (*KE(ms[-1], 5.0), "WALL PARAPET", "1.10 m mud-brick on the 4.10 m walk"),
        (*KE(3.0, 2.2), "SOUTH RUN", "2.40 m thick, x −1.65 → 6.05"),
    ], 610, 200, 290, slope=1.0)
    sh.callouts([
        (*KE(-3.8, TOP + 0.9), "TOWER PARAPET", "south and west sides only, to 6.60 m"),
        (*KE(-4.8, 4.8), "FOURTH COURSE", "the tower's 1.40 m above the walk"),
        (*KE(-3.0, 1.2), "CORNER TOWER", "4.40 × 4.40 m, cyclopean, 5.50 m"),
    ], 470, 190, 290, anchor="end")
    mini_section(sh, 70, 300, [
        (0, 0, T, TOP, STONE),
        (PARAPET_IN + PARAPET_T, TOP, T - 0.2, TOP + 0.1, PLASTER),
        (PARAPET_IN, TOP, PARAPET_IN + PARAPET_T, TOP + 1.1, MUD),
    ], "SECTION B–B · TOWER", width=T, scale=20, label_above=True)

    # ---- plan ----
    kit_plan(sh, "CurtainWall", floor="#3A3226", wall="#3A332A", shell=False)
    plan_run(sh, SOUTH, DEPTH)
    plan_walk(sh, TE, KH, DEPTH)
    plan_parapet(sh, TE, KH)
    plan_run(sh, WEST, DEPTH, side="west")
    plan_walk(sh, TE, KH, DEPTH, side="west")
    plan_parapet(sh, TE, KH, side="west")
    plan_run(sh, TOWER, T, fill=lighten(STONE, .06))
    kprect(sh, -KH + PARAPET_IN + PARAPET_T, -KH + PARAPET_IN + PARAPET_T, TE, TE - 0.2, lighten(PLASTER, .12),
           "#2A251D", .6)
    plan_parapet(sh, -KH, TE)
    plan_parapet(sh, -KH + PARAPET_IN + PARAPET_T, TE, side="west")
    for k in range(3):                                    # the steps up from each walk, highest nearest the tower
        a, b = TE + k * STEP_RUN, TE + (k + 1) * STEP_RUN
        col = lighten("#9A8C72", .1 * (2 - k))
        plan_strip(sh, a, b, PARAPET_IN + PARAPET_T, DEPTH - 0.2, "south", col, "#2A251D", .6)
        plan_strip(sh, a, b, PARAPET_IN + PARAPET_T, DEPTH - 0.2, "west", col, "#2A251D", .6)
    sh.path(f"M{f(KP(-0.2, -4.8)[0])} {f(KP(-0.2, -4.8)[1])} l-40 0 l6 -4 m-6 4 l6 4", "none", "#1E1B16", 1.2)
    sh.path(f"M{f(KP(-4.8, -0.2)[0])} {f(KP(-4.8, -0.2)[1])} l0 40 l-4 -6 m4 6 l4 -6", "none", "#1E1B16", 1.2)
    socket_label(sh, *KP(-3.9, -3.6), "TOWER 5.50")
    socket_label(sh, *KP(2.6, -4.55), "WALK 4.10")
    socket_label(sh, *KP(-4.8, 2.6), "WALK 4.10")
    socket_label(sh, *KP(3.0, 3.0), "BAILEY (NORTH)")
    kit_legend(sh, [("WALLS", "2.40 m cyclopean on the south and west edges"),
                    ("TOWER", "SW, 4.40 m square, top 5.50 m, parapet S and W"),
                    ("STEPS", "3 per walk, 0.35 m rises, up onto the tower"),
                    ("JOINS", "both runs reach their far cell edges"),
                    ("LOOT", "none: a wall piece carries no loot")])
    return sh
