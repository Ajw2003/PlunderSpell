"""BronzeLionGate: the Lion Gate (stands in for GatehouseModule), the art bible's
structure (docs/art/bronze.md, "The Lion Gate") built to the kit.

Two cyclopean masses 3.20 m deep on the south edge pinch the way in to a 2.60 m
passage between conglomerate jambs, 3.74 m to a lintel 4.40 × 3.20 × 0.50 m. Over
the lintel three corbelled courses leave the relieving triangle, filled at its
outer face by the lion slab: a Minoan column on an altar between two rampant,
headless lions. Mud-brick breastworks on the masses; inside, two oak leaves stand
folded back against the north faces, the bar lies by the west mass, bronze torch
rings flank the passage and a stone stair climbs the east mass to the walk. A
paved road with cart ruts runs through the cell. Kit override: the art bible sets
the masses 0.30 m in from the E/W cell edges; the kit runs them to the edges so a
neighbouring wall piece meets them without a gap. Blocks are cyclopean.blocks():
west mass seed 9, east mass seed 10, inner faces flat for the plaster.
"""
from _curtain import *

DEPTH = 3.2
PASS, JO = 1.3, 2.2                        # passage half-width, jamb outer edge
LINTEL = (3.74, 4.24)
CORBEL_H = 0.32
GAPS = (1.2, 0.8, 0.4)                     # the triangle's half-width at each corbel course
SLAB_D = (0.10, 0.60)                      # the lion slab, set back from the corbels' face
WEST = cy.blocks(-KH, -JO, WALK_Z, seed=9)
EAST = cy.blocks(JO, KH, WALK_Z, seed=10)
STAIR_X, STEPS, RISE = 2.75, 14, 0.30
LEAF = (PASS, PASS + 1.30)
TORCH_X, TORCH_Z = 2.9, 2.2
ROAD = 1.6
ANCHORS = []


def lion_slab(sh, cx, by, s, col=STONE, relief=None):
    """The relief slab at s px per metre, base centre (cx, by) in px: the triangle, the
    altar plinth and down-tapering column, two rampant lions whose heads are missing
    (dowel holes where they were fixed)."""
    relief = relief or lighten(col, .12)
    tri = [(cx - 1.2 * s, by), (cx + 1.2 * s, by), (cx, by - 0.96 * s)]
    sh.path(poly_path(tri), f"url(#{sh.lin(col, 'v', .2, .5)})", "#2A251D", 1.2)
    sh.add(f'<rect x="{f(cx - 0.25 * s)}" y="{f(by - 0.14 * s)}" width="{f(0.5 * s)}" height="{f(0.14 * s)}" '
           f'fill="{relief}" stroke="#2A251D" stroke-width=".8"/>')
    col_pts = [(cx - 0.055 * s, by - 0.14 * s), (cx + 0.055 * s, by - 0.14 * s),
               (cx + 0.08 * s, by - 0.76 * s), (cx - 0.08 * s, by - 0.76 * s)]
    sh.path(poly_path(col_pts), relief, "#2A251D", .8)
    sh.add(f'<rect x="{f(cx - 0.11 * s)}" y="{f(by - 0.82 * s)}" width="{f(0.22 * s)}" height="{f(0.06 * s)}" '
           f'fill="{relief}" stroke="#2A251D" stroke-width=".8"/>')
    lion = [(0.86, 0.02), (0.66, 0.02), (0.62, 0.12), (0.55, 0.30), (0.33, 0.14), (0.27, 0.15), (0.30, 0.58),
            (0.38, 0.66), (0.50, 0.62), (0.70, 0.45), (0.90, 0.26)]
    for sgn in (-1, 1):
        sh.path(smooth_path([(cx + sgn * u * s, by - z * s) for u, z in lion], tension=.3), relief, "#2A251D", .8)
        sh.circle(cx + sgn * 0.35 * s, by - 0.64 * s, max(1.2, 0.03 * s), "#14100C")


def build():
    mats = [("limestone", STONE), ("conglomerate", CONGLOM), ("plaster", PLASTER), ("mud-brick", MUD),
            ("oak", CYP), ("bronze", BRONZE), ("soot", SOOT)]
    sh = room_sheet("BronzeAge", "CurtainWall", "The Lion Gate", mats, "≤ 2.4k tris (kit)",
                    "SOUTH ELEVATION · OUTSIDE FACE, LOOKING NORTH")
    kit_glow(sh, 0, 2.0, MADDER_, rx=160, ry=120, strength=.18)

    # ---- elevation ----
    ground(sh)
    kerect(sh, -PASS, 0, PASS, LINTEL[0], "#14120E")
    kerect(sh, -PASS, 0, PASS, 0.06, lighten(STONE, .1), darken(STONE, .5), .6)
    for x in (-0.7, 0.7):
        kerect(sh, x - 0.06, 0.0, x + 0.06, 0.07, SOOT)
    for bl in (WEST, EAST):
        elev_blocks(sh, bl)
    for sgn in (-1, 1):
        a, b = sorted((sgn * PASS, sgn * JO))
        kerect(sh, a, 0, b, LINTEL[0], f"url(#{sh.lin(CONGLOM, 'h', .3, .5)})", darken(CONGLOM, .6), 1.2)
        kerect(sh, a + 0.05, 0.4, b - 0.05, 0.6, darken(CONGLOM, .15), op=.5)                  # cart scuff
        kerect(sh, (a if sgn > 0 else b - 0.12), 0.9, (a + 0.12 if sgn > 0 else b), 1.6, lighten(CONGLOM, .15), op=.6)
    kerect(sh, -JO, LINTEL[0], JO, LINTEL[1], f"url(#{sh.lin(CONGLOM, 'v', .25, .5)})", darken(CONGLOM, .6), 1.2)
    for c, g in enumerate(GAPS):
        z0 = LINTEL[1] + c * CORBEL_H
        for sgn in (-1, 1):
            a, b = sorted((sgn * g, sgn * JO))
            kerect(sh, a, z0, b, z0 + CORBEL_H, f"url(#{sh.lin(CONGLOM, 'v', .2, .5)})", darken(CONGLOM, .6), .9)
    bx, by = KE(0, LINTEL[1])
    lion_slab(sh, bx, by, SK, col=darken(STONE, .08))
    elev_parapet(sh, -KH, -JO)
    elev_parapet(sh, JO, KH)
    khuman(sh, 0.0, floor=0.0)
    sh.callouts([
        (*KE(0.6, LINTEL[1] + 0.3), "LION SLAB", "2.40 × 0.96 × 0.50 m, set 0.10 m back"),
        (*KE(1.5, LINTEL[1] - 0.2), "LINTEL", "4.40 × 3.20 × 0.50 m conglomerate"),
        (*KE(4.0, 2.0), "CYCLOPEAN MASS", "3.20 m deep, to the cell edge"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-1.0, LINTEL[1] + 0.8), "CORBELLED COURSES", "3 × 0.32 m, stepping 0.40 m in"),
        (*KE(-1.75, 1.2), "CONGLOMERATE JAMB", "0.90 m, worn at the shoulder"),
    ], 470, 225, 300, anchor="end")
    sh.text(160, 185, "LION SLAB DETAIL · 1 m = 80 px", 9, "#9A9078", "middle", ls=2)
    lion_slab(sh, 160, 290, 80.0, col=darken(STONE, .08))
    sh.text(160, 306, "heads missing · dowel holes", 8.5, "#9A9078", "middle")

    # ---- plan ----
    kit_plan(sh, "CurtainWall", floor="#3A3226", wall="#3A332A", shell=False)
    for k in range(12):
        y0 = -KH + k * (2 * KH / 12)
        cuts = (-ROAD, 0.0, ROAD) if k % 2 == 0 else (-ROAD, -0.8, 0.8, ROAD)
        for a, b in zip(cuts, cuts[1:]):
            kprect(sh, a + 0.02, y0 + 0.02, b - 0.02, y0 + 2 * KH / 12 - 0.02, lighten(STONE, .1), "#2A251D", .5)
    for x in (-0.7, 0.7):
        kprect(sh, x - 0.06, -KH, x + 0.06, KH, SOOT)
    for bl, u0, u1 in ((WEST, -KH, -JO), (EAST, JO, KH)):
        plan_run(sh, bl, DEPTH, ragged=False)
        plan_walk(sh, u0, u1, DEPTH)
        plan_parapet(sh, u0, u1)
    for sgn in (-1, 1):
        a, b = sorted((sgn * PASS, sgn * JO))
        plan_strip(sh, a, b, 0, DEPTH, "south", CONGLOM, "#2A251D", .7)
    kprect(sh, -JO, -KH, JO, -KH + DEPTH, "none", lighten(CONGLOM, .3), 1)
    sh.text(*KP(0, -KH + DEPTH / 2 + 0.2), "LINTEL + TRIANGLE OVER", 7.5, "#DCD2BA", "middle")
    kprect(sh, -1.2, -KH + SLAB_D[0], 1.2, -KH + SLAB_D[1], darken(STONE, .08), "#2A251D", .6)
    # Inside: plaster, the leaves folded back, the bar, the torches, the stair.
    plan_strip(sh, -KH, -JO, DEPTH, DEPTH + 0.04, "south", PLASTER)
    for sgn in (-1, 1):
        a, b = sorted((sgn * LEAF[0], sgn * LEAF[1]))
        plan_strip(sh, a, b, DEPTH + 0.05, DEPTH + 0.19, "south", CYP, "#0E0C09", .6)
        plan_strip(sh, *sorted((sgn * (LEAF[1] - 0.12), sgn * LEAF[1])), DEPTH + 0.19, DEPTH + 0.21, "south", BRONZE)
    plan_strip(sh, -5.8, -2.6, DEPTH + 0.27, DEPTH + 0.43, "south", CYP, "#0E0C09", .6)
    tread = (KH - STAIR_X) / STEPS
    for k in range(STEPS):
        x0 = STAIR_X + k * tread
        plan_strip(sh, x0, x0 + tread, DEPTH, DEPTH + 1.0, "south", lighten("#9A8C72", .12 * k / STEPS - .06),
                   "#2A251D", .4)
    for sgn in (-1, 1):                                  # the torches, over the stair's first step on the east
        sh.circle(*KP(sgn * TORCH_X, -KH + DEPTH + 0.1), 0.12 * PK, MADDER_, "#0E0C09", .6)
    p = KP(3.2, -KH + DEPTH + 0.5)
    sh.path(f"M{f(p[0])} {f(p[1])} l70 0 l-6 -4 m6 4 l-6 4", "none", "#1E1B16", 1.2)
    socket_label(sh, *KP(4.4, -1.2), "STAIR → WALK 4.10")
    socket_label(sh, *KP(-3.4, -1.9), "BAR · LEAVES FOLDED")
    socket_label(sh, *KP(0, 3.0), "BAILEY (NORTH)")
    kit_legend(sh, [("GATE", "2.60 × 3.74 m, N–S through the centre, 2 oak leaves"),
                    ("MASSES", "cyclopean 3.20 m deep, walk 4.10, parapet S"),
                    ("STAIR", "east mass, north face, 14 × 0.30 m rises"),
                    ("TORCHES", "2 bronze rings on the north face, 2.20 m"),
                    ("LOOT", "none: a wall piece carries no loot")])
    return sh
