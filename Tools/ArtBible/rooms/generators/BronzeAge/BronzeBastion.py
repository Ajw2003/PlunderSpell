"""BronzeBastion: the horned tower (stands in for Bastion).

A rectangular cyclopean tower 7.00 m (E–W) × 4.50 m (N–S) on the south edge,
projecting into the bailey, four courses to 5.50 m; along its outer edge a low
mud-brick wall carrying five horns of consecration, the Age's skyline marker, to
about 6.70 m. The 2.40 m wall runs on from its east and west faces to the cell
edges, three steps on each walk up onto the tower. Blocks are cyclopean.blocks():
tower seed 4, west run seed 5, east run seed 6. South elevation left; plan right;
a small N–S section through the tower above.
"""
from _curtain import *

DEPTH = 2.4
TW, TD, TOP = 3.5, 4.5, 5.5               # half-width, depth, top
TOWER = cy.blocks(-TW, TW, TOP, seed=4)
WEST = cy.blocks(-KH, -TW, WALK_Z, seed=5)
EAST = cy.blocks(TW, KH, WALK_Z, seed=6)
HORNS = [-2.8, -1.4, 0.0, 1.4, 2.8]
CREST = TOP + LOW                          # the horns stand on the low wall's top
ANCHORS = []


def build():
    mats = [("limestone", STONE), ("mud-brick", MUD), ("plaster", PLASTER), ("gypsum", GYP),
            ("ashlar", "#9A8C72"), ("soot", SOOT)]
    sh = room_sheet("BronzeAge", "CurtainWall", "The Horned Bastion", mats, "≤ 2k tris (kit)",
                    "SOUTH ELEVATION · OUTSIDE FACE, LOOKING NORTH")

    # ---- elevation ----
    ground(sh)
    for bl, u0, u1 in ((WEST, -KH, -TW), (EAST, TW, KH)):
        elev_blocks(sh, bl)
        elev_parapet(sh, u0, u1)
    elev_blocks(sh, TOWER, lit=lighten(STONE, .06))
    kerect(sh, -TW, TOP, TW, CREST, f"url(#{sh.lin(MUD, 'v', .25, .5)})", darken(MUD, .6), 1)
    for k in range(1, 3):
        sh.line(*KE(-TW, TOP + k * LOW / 3), *KE(TW, TOP + k * LOW / 3), darken(MUD, .35), .7, op=.7)
    for x in HORNS:
        horns(sh, x, CREST, 0.8, GYP)
    khuman(sh, -1.4, floor=0.0)
    sh.callouts([
        (*KE(1.4, CREST + 0.5), "HORNS OF CONSECRATION", "five, 0.80 m, plastered white"),
        (*KE(2.6, TOP + 0.25), "LOW WALL", "mud-brick 0.50 m on the outer edge"),
        (*KE(4.8, 4.9), "WALL RUNS", "2.40 m wall on to both cell edges"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-2.0, 4.8), "FOURTH COURSE", "tower top 5.50 m, 1.40 m over the walk"),
        (*KE(-2.6, 1.6), "BASTION", "7.00 × 4.50 m cyclopean, projecting in"),
    ], 470, 200, 290, anchor="end")
    mini_section(sh, 70, 300, [
        (0, 0, TD, TOP, STONE),
        (PARAPET_IN + PARAPET_T, TOP, TD - 0.2, TOP + 0.1, PLASTER),
        (PARAPET_IN, TOP, PARAPET_IN + PARAPET_T, CREST, MUD),
        (PARAPET_IN + 0.08, CREST, PARAPET_IN + 0.32, CREST + 0.7, GYP),
    ], "SECTION B–B · TOWER", width=TD, scale=20, label_above=True)

    # ---- plan ----
    kit_plan(sh, "CurtainWall", floor="#3A3226", wall="#3A332A", shell=False)
    for bl, u0, u1 in ((WEST, -KH, -TW), (EAST, TW, KH)):
        plan_run(sh, bl, DEPTH)
        plan_walk(sh, u0, u1, DEPTH)
        plan_parapet(sh, u0, u1)
    plan_run(sh, TOWER, TD, fill=lighten(STONE, .06))
    plan_strip(sh, -TW, TW, PARAPET_IN + PARAPET_T, TD - 0.2, "south", lighten(PLASTER, .12), "#2A251D", .6)
    plan_strip(sh, -TW, TW, PARAPET_IN, PARAPET_IN + PARAPET_T, "south", MUD, "#2A251D", .6)
    for x in HORNS:
        plan_strip(sh, x - 0.4, x + 0.4, PARAPET_IN + 0.08, PARAPET_IN + 0.32, "south", GYP, "#2A251D", .5)
    for sgn in (-1, 1):
        for k in range(3):
            a, b = sorted((sgn * (TW + k * 0.35), sgn * (TW + (k + 1) * 0.35)))
            plan_strip(sh, a, b, PARAPET_IN + PARAPET_T, DEPTH - 0.2, "south", lighten("#9A8C72", .1 * (2 - k)),
                       "#2A251D", .6)
        p = KP(sgn * 5.3, -4.8)
        sh.path(f"M{f(p[0])} {f(p[1])} l{-sgn * 36} 0 l{sgn * 6} -4 m{-sgn * 6} 4 l{sgn * 6} 4", "none", "#1E1B16", 1.2)
    socket_label(sh, *KP(0, -3.2), "BASTION TOP 5.60")
    socket_label(sh, *KP(0, 3.0), "BAILEY (NORTH)")
    kit_legend(sh, [("BASTION", "7.00 × 4.50 m on the south edge, top 5.50 m"),
                    ("CREST", "low wall + 5 horns of consecration, to ~6.70 m"),
                    ("WALLS", "2.40 m runs east and west to the cell edges"),
                    ("STEPS", "3 per walk, 0.35 m rises, up onto the tower"),
                    ("LOOT", "none: a wall piece carries no loot")])
    return sh
