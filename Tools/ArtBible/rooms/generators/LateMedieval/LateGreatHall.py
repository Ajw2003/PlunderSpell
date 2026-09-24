"""LateGreatHall: the great hall (stands in for ThroneRoomKeep), the art bible's Great
Hall (docs/art/late.md) built to the kit.

Oak daises 0.35 m high in the NW and NE corners, the clear cross running between
them to the north archway. On the NE dais the high table under a linen cloth, the
lord's chair behind it under a cloth of estate and its canopy; on the NW dais a side
table with the gilded nef. The dorsal tapestry hangs in two halves either side of
the north archway. A hooded hearth on the west wall just south of the NW dais,
window embrasures on the east wall, two oak benches and a standing candle stand
to the south. Kit overrides: the art bible's single dais and 8 m tapestry are split
by the clear cross; its roof trusses go (rooms are open-roofed). Section A-A
east-west at y = 0, looking north.
"""
from _late import *

DAIS_H, DAIS_Y = 0.35, 3.3
D = FZ + DAIS_H                                          # the dais top
HT = (3.6, 4.2, 3.0, 0.8, 0.75)                          # high table: x, y, w, d, h
SIDE = (-3.6, 4.5, 1.2, 0.6, 0.80)                       # the side table on the NW dais
ESTATE_X = (2.9, 4.3)
TAP = (1.45, 5.35, FZ + 1.5, FZ + 4.05)                  # each tapestry half: |x| from..to, bottom, top
HEARTH_Y, HEARTH_W, HEARTH_D = 2.5, 1.3, 0.9
ANCHORS = [(HT[0], HT[1], D + HT[4]), (SIDE[0], SIDE[1], D + SIDE[4])]


def build():
    mats = [("sandstone", SAND), ("oak", OAK), ("tapestry", WOOL), ("estate cloth", ESTATE),
            ("linen", LINEN), ("iron", IRON), ("gilt", GOLD)]
    sh = room_sheet("LateMedieval", "Keep", "The Great Hall", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE HALL, LOOKING NORTH")
    kit_glow(sh, -4.8, FZ + 0.6, FIRE, rx=260, ry=200, strength=.4)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "Keep", SAND, soot=SOOT, trim=ESTATE, soot_depth=0.8)
    ashlar_courses(sh, "Keep")
    for sgn in (-1, 1):
        a, b = sorted((sgn * TAP[0], sgn * TAP[1]))
        tapestry(sh, a, b, TAP[2], TAP[3])
    # NE: the cloth of estate and its canopy, the chair, the dais and the high table.
    kerect(sh, ESTATE_X[0], D + 0.9, ESTATE_X[1], FZ + 3.7, f"url(#{sh.lin(ESTATE, 'h', .3, .5)})", darken(ESTATE, .5), .9)
    kerect(sh, ESTATE_X[0] - 0.1, FZ + 3.7, ESTATE_X[1] + 0.1, FZ + 3.82, darken(ESTATE, .15), darken(ESTATE, .5), .8)
    for k in range(6):
        x = ESTATE_X[0] - 0.1 + k * (ESTATE_X[1] - ESTATE_X[0] + 0.2) / 5
        sh.line(*KE(x, FZ + 3.7), *KE(x, FZ + 3.58), GOLD, 1.4)                 # the canopy's fringe
    kerect(sh, 3.3, D, 3.9, D + 1.6, f"url(#{sh.lin(OAK, 'h', .3, .5)})", darken(OAK, .6), .9)   # chair back
    for sgn in (-1, 1):
        a, b = sorted((sgn * 1.8, sgn * IN))
        kerect(sh, a, FZ, b, D, f"url(#{sh.lin(OAK, 'v', .25, .5)})", darken(OAK, .6), 1)
    table(sh, HT[0], HT[2], HT[4], base=D, cloth=LINEN)
    for dx in (-1.0, -0.3, 0.9):
        kerect(sh, HT[0] + dx - 0.05, D + HT[4], HT[0] + dx + 0.05, D + HT[4] + 0.14, PEWTER, darken(PEWTER, .5), .5)
    sh.ellipse(*KE(HT[0] + 0.3, D + HT[4] + 0.12), 10, 8, GOLD, darken(GOLD, .5), .7)        # the salt
    # NW: the side table with the nef on it.
    table(sh, SIDE[0], SIDE[2], SIDE[4], base=D)
    nb = D + SIDE[4]
    hull = [KE(SIDE[0] - 0.3, nb + 0.25), KE(SIDE[0] + 0.3, nb + 0.25), KE(SIDE[0] + 0.2, nb + 0.12),
            KE(SIDE[0] - 0.2, nb + 0.12)]
    sh.path(poly_path(hull), GOLD, darken(GOLD, .5), .8)
    kerect(sh, SIDE[0] - 0.05, nb, SIDE[0] + 0.05, nb + 0.12, GOLD)
    sh.line(*KE(SIDE[0], nb + 0.25), *KE(SIDE[0], nb + 0.62), GOLD, 1.6)
    sh.path(poly_path([KE(SIDE[0] + 0.02, nb + 0.58), KE(SIDE[0] + 0.2, nb + 0.42), KE(SIDE[0] + 0.02, nb + 0.32)]),
            LINEN, darken(LINEN, .5), .6)
    # The hearth on the west wall, in profile (it stands just behind the cut, south of the NW dais).
    x_face = -IN + HEARTH_D
    prof = [KE(-IN, FZ), KE(x_face, FZ), KE(x_face, FZ + 1.3), KE(x_face + 0.1, FZ + 1.3), KE(x_face + 0.1, FZ + 1.55),
            KE(-IN + 0.3, FZ + 4.1), KE(-IN, FZ + 4.1)]
    sh.path(poly_path(prof), f"url(#{sh.lin(SAND, 'h', .25, .5)})", darken(SAND, .6), 1)
    sh.path(poly_path([KE(x_face + 0.1, FZ + 1.55), KE(-IN + 0.3, FZ + 4.1), KE(-IN + 0.2, FZ + 4.1),
                       KE(x_face - 0.1, FZ + 1.6)]), SOOT, op=.4)
    fx, fy = KE(x_face - 0.15, FZ)
    sh.path(f"M{f(fx - 14)} {f(fy)} q4 -26 14 -34 q0 14 8 20 q4 -10 2 -18 q10 14 4 32 z", FIRE, op=.9)
    kit_cut_walls(sh, "Keep")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(3.6, FZ + 3.2), "CLOTH OF ESTATE", "and canopy over the lord's chair"),
        (*KE(4.8, D + HT[4] - 0.1), "HIGH TABLE", "3.00 × 0.80 m, linen · loot"),
        (*KE(4.9, FZ + 0.2), "OAK DAIS", "0.35 m, NE and NW corners"),
    ], 610, 200, 330, slope=1.0)
    sh.callouts([
        (*KE(-2.5, FZ + 3.0), "DORSAL TAPESTRY", "two halves, 3.90 × 2.55 m, burns"),
        (*KE(-3.6, nb + 0.3), "GILDED NEF", "on the NW side table · loot"),
        (*KE(-5.0, FZ + 2.6), "HEARTH HOOD", "sandstone, sooted, west wall"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "Keep", x=0.0, text="4.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Keep", floor="#2A2016", wall="#3A332A", trim=ESTATE)
    for sgn in (-1, 1):
        a, b = sorted((sgn * 1.8, sgn * IN))
        kprect(sh, a, DAIS_Y, b, IN, OAK, "#0E0C09", .8)
        a, b = sorted((sgn * TAP[0], sgn * TAP[1]))
        kprect(sh, a, IN - 0.06, b, IN, WOOL)
    kprect(sh, ESTATE_X[0], IN - 0.12, ESTATE_X[1], IN, ESTATE)
    kprect(sh, ESTATE_X[0] - 0.1, IN - 0.9, ESTATE_X[1] + 0.1, IN - 0.12, "none", ESTATE, 1)
    plan_box(sh, 3.6, 5.05, 0.6, 0.5, darken(OAK, .15))
    plan_box(sh, HT[0], HT[1], HT[2], HT[3], LINEN)
    plan_box(sh, SIDE[0], SIDE[1], SIDE[2], SIDE[3], darken(OAK, .1))
    plan_box(sh, SIDE[0], SIDE[1], 0.6, 0.2, GOLD)
    plan_box(sh, -IN + HEARTH_D / 2, HEARTH_Y, HEARTH_D, HEARTH_W + 0.4, SAND)
    plan_box(sh, -IN + 0.3, HEARTH_Y, 0.5, HEARTH_W - 0.2, "#0E0C09")
    sh.circle(*KP(-IN + 0.35, HEARTH_Y), 0.2 * PK, FIRE)
    for y in (3.6, -3.6):
        kprect(sh, IN - 0.1, y - 0.7, IN, y + 0.7, "#2A3238", "#0E0C09", .6)
    plan_box(sh, 3.4, -3.0, 2.6, 0.35, OAK)
    plan_box(sh, -3.2, -3.4, 2.6, 0.35, OAK)
    plan_disc(sh, 4.8, -4.8, 0.2, IRON)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Keep")
    socket_label(sh, *KP(-4.0, 1.95), "HEARTH")
    socket_label(sh, *KP(4.4, -2.1), "WINDOWS E")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 3.31 · all open"),
                    ("CLEAR CROSS", "dashed · runs between the two daises"),
                    ("DAIS", "NE: high table, chair, estate · NW: side table"),
                    ("WALLS", "tapestry N · hearth W · windows E"),
                    ("LOOT", "L1–L2: high table, the nef's side table")])
    return sh
