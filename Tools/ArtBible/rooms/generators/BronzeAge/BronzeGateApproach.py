"""BronzeGateApproach: the cart gate (stands in for Drawbridge).

The 2.40 m cyclopean wall with a 4.20 m opening in the middle, 3.80 m clear,
between two dressed conglomerate jambs; three oak beams pocketed into the jambs
carry a plank deck so the wall-walk and its breastwork run straight across. A
paved limestone road with two cart ruts runs through the opening and 5.00 m in,
and a carved stele stands on its base beside it. Bronze Age walls had no
drawbridge: this is the Age's second, humbler way in. Blocks are cyclopean.blocks():
west mass seed 7, east mass seed 8. South elevation left; plan right; a small N–S
section through the opening above.
"""
from _curtain import *

DEPTH = 2.4
OPEN = 2.1                                 # half the opening
JAMB = 0.6
BEAM_Z = 3.8
BEAMS = [0.25, 1.2, 2.0]                   # beam centres, metres in from the cell line
WEST = cy.blocks(-KH, -OPEN - JAMB, WALK_Z, seed=7)
EAST = cy.blocks(OPEN + JAMB, KH, WALK_Z, seed=8)
ROAD, ROAD_LEN = 1.8, 5.0
RUTS = (-0.7, 0.7)
STELE = (2.8, -2.6)
ANCHORS = []


def slabs():
    """The road's paving: rows 1.00 m deep, three slabs a row, joints staggered."""
    out = []
    for k in range(int(ROAD_LEN)):
        cuts = (-ROAD, -0.6, 0.6, ROAD) if k % 2 == 0 else (-ROAD, -0.9, 0.9, ROAD)
        for a, b in zip(cuts, cuts[1:]):
            out.append((a + 0.02, -KH + k + 0.02, b - 0.02, -KH + k + 0.98))
    return out


def build():
    mats = [("limestone", STONE), ("conglomerate", CONGLOM), ("oak", CYP), ("mud-brick", MUD),
            ("plaster", PLASTER), ("soot", SOOT)]
    sh = room_sheet("BronzeAge", "CurtainWall", "The Cart Gate", mats, "≤ 2k tris (kit)",
                    "SOUTH ELEVATION · OUTSIDE FACE, LOOKING NORTH")

    # ---- elevation ----
    ground(sh)
    kerect(sh, -OPEN, 0, OPEN, BEAM_Z, "#14120E")                          # through the gate: the dark bailey
    kerect(sh, -ROAD, 0, ROAD, 0.06, lighten(STONE, .1), darken(STONE, .5), .6)
    for x in RUTS:
        kerect(sh, x - 0.06, 0.0, x + 0.06, 0.07, SOOT)
    for bl in (WEST, EAST):
        elev_blocks(sh, bl)
    for sgn in (-1, 1):
        a, b = sorted((sgn * OPEN, sgn * (OPEN + JAMB)))
        kerect(sh, a, 0, b, WALK_Z, f"url(#{sh.lin(CONGLOM, 'h', .3, .5)})", darken(CONGLOM, .6), 1.2)
        kerect(sh, a + 0.04, 0.4, b - 0.04, 0.6, darken(CONGLOM, .15), op=.6)          # cart scuff
    kerect(sh, -OPEN - JAMB, BEAM_Z, OPEN + JAMB, WALK_Z, f"url(#{sh.lin(CYP, 'v', .25, .5)})", darken(CYP, .6), 1)
    for k in range(7):                                                   # the south beam's end grain of planks above
        x = -OPEN + 0.3 + k * 0.6
        sh.line(*KE(x, BEAM_Z), *KE(x, WALK_Z), darken(CYP, .4), .6, op=.6)
    elev_parapet(sh, -KH, KH)
    khuman(sh, -0.4, floor=0.0)
    ms = merlon_centres(-KH, KH)
    sh.callouts([
        (*KE(OPEN + 0.3, 2.4), "CONGLOMERATE JAMB", "0.60 m, dressed, full depth"),
        (*KE(1.2, BEAM_Z + 0.15), "OAK BEAMS + DECK", "three beams, 3.80 m clear under"),
        (*KE(ms[-2], 5.0), "BREASTWORK", "runs straight across on the deck"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-0.7, 0.05), "CART RUTS", "1.40 m apart, in paving 3.60 m wide"),
        (*KE(-4.0, 2.0), "CYCLOPEAN MASSES", "2.40 m, either side of the gate"),
    ], 470, 220, 290, anchor="end")
    mini_section(sh, 70, 320, [
        (0, 0, DEPTH, WALK_Z, "#2E2922"),                                # the east jamb, beyond the cut
        (0, 0, ROAD_LEN, 0.06, lighten(STONE, .1)),
        *[(d - 0.15, BEAM_Z, d + 0.15, WALK_Z, CYP) for d in BEAMS],
        (PARAPET_IN, WALK_Z, DEPTH - 0.2, WALK_Z + 0.1, darken(CYP, .1)),
        (PARAPET_IN, WALK_Z + 0.1, PARAPET_IN + PARAPET_T, 5.2, MUD),
    ], "SECTION B–B · GATE, JAMB BEYOND", width=ROAD_LEN, scale=24, label_above=True)

    # ---- plan ----
    kit_plan(sh, "CurtainWall", floor="#3A3226", wall="#3A332A", shell=False)
    for x0, y0, x1, y1 in slabs():
        kprect(sh, x0, y0, x1, y1, lighten(STONE, .1), "#2A251D", .5)
    for x in RUTS:
        kprect(sh, x - 0.06, -KH, x + 0.06, -KH + ROAD_LEN, SOOT)
    for bl, u0, u1 in ((WEST, -KH, -OPEN - JAMB), (EAST, OPEN + JAMB, KH)):
        plan_run(sh, bl, DEPTH)
        plan_walk(sh, u0, u1, DEPTH)
    for sgn in (-1, 1):
        a, b = sorted((sgn * OPEN, sgn * (OPEN + JAMB)))
        plan_strip(sh, a, b, 0, DEPTH, "south", CONGLOM, "#2A251D", .7)
    plan_strip(sh, -OPEN - JAMB, OPEN + JAMB, PARAPET_IN + PARAPET_T, DEPTH - 0.2, "south", CYP, "#2A251D", .6)
    for k in range(1, 8):
        x = -OPEN - JAMB + k * (2 * (OPEN + JAMB)) / 8
        plan_strip(sh, x - 0.01, x + 0.01, PARAPET_IN + PARAPET_T, DEPTH - 0.2, "south", darken(CYP, .4))
    plan_parapet(sh, -KH, KH)
    sx, sy = STELE
    plan_box(sh, sx, sy, 0.5, 0.9, CONGLOM)
    plan_box(sh, sx + 0.1, sy, 0.15, 0.6, STONE)
    socket_label(sh, *KP(0, -4.45), "DECK 4.10")
    socket_label(sh, *KP(0, -2.0), "ROAD · 5.00 m IN")
    socket_label(sh, *KP(sx + 0.3, sy - 0.8), "STELE")
    socket_label(sh, *KP(0, 3.0), "BAILEY (NORTH)")
    kit_legend(sh, [("GATE", "4.20 m wide, 3.80 m clear, open, no leaves"),
                    ("WALK", "continuous: oak deck across the opening"),
                    ("ROAD", "paved 3.60 m, cart ruts, flat, 5.00 m inward"),
                    ("STELE", "carved, on a base beside the road, NE"),
                    ("LOOT", "none: a wall piece carries no loot")])
    return sh
