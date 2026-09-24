"""BronzeTholos: the beehive tomb's chamber (stands in for CryptChamberFinal), after
the Treasury of Atreus.

Four corbelled courses of ashlar, each ring stepping further in as it rises, built
as four arcs (one per quadrant, 28°–62° from each axis) so the archways and the
clear cross pass through the gaps; behind the rings the corners are packed solid
to the walls, as the earth mound packs a real tholos. The dome would close far
overhead; the roof is open. The king on his bier in the NE under a gold mask,
gold cups on an offering table NW, a bronze-bound chest SW, braziers NE and SE.
Section A-A east-west at y = 0, looking north: the north arcs in elevation, the
bier, the tripod behind it and the offering table in front of them.
"""
import math

from _bronze import *

ASHLAR = "#9A8C72"
A0, A1 = 28.0, 62.0                                   # each arc's span, degrees from the x axis
COURSES = [(4.90, 0.00), (4.55, 0.65), (4.20, 1.30), (3.85, 1.95)]   # (inner radius, bottom)
HC = 0.65                                             # course height; the top course ends at 2.60
W = IN - 0.02                                         # the fill stops just short of the wall face
ANCHORS = [(2.7, 2.35, FZ + 0.56), (2.0, 3.35, 1.44), (-2.6, 2.6, FZ + 0.70), (-2.8, -2.3, FZ + 0.60)]


def arc_angles():
    """The arc's sample angles, 45° included so the corner fill reaches the corner."""
    return [A0 + k * (A1 - A0) / 6 for k in range(7)]          # 45° is k = 3


def course_outline(r, qx, qy):
    """One course's footprint in quadrant (qx, qy): the arc at radius r, then out to the walls."""
    inner, outer = [], []
    for a in arc_angles():
        t = math.radians(a)
        c, s = math.cos(t), math.sin(t)
        inner.append((qx * r * c, qy * r * s))
        d = W / max(c, s)
        outer.append((qx * d * c, qy * d * s))
    return inner + list(reversed(outer))              # the 45° ray lands on the corner itself


def build():
    mats = [("limestone", STONE), ("ashlar", ASHLAR), ("gold", GOLD), ("bronze", BRONZE),
            ("linen", LINEN), ("haematite", RED), ("soot", SOOT)]
    sh = room_sheet("BronzeAge", "Crypt", "The Tholos", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE CHAMBER, LOOKING NORTH")
    kit_glow(sh, 2.4, FZ + 1.0, "#8A6A30", rx=300, ry=160, strength=.35)

    # ---- section ----
    kit_slab(sh, "#3A332A")
    kit_back_wall(sh, "Crypt", STONE, trim=ASHLAR, soot_depth=0.6)
    # The north arcs in elevation: each course runs from its near end (at 62°) to the wall,
    # the higher ones reaching further toward the centre. Joints where the blocks meet.
    for k, (r, bottom) in enumerate(COURSES):
        shade = ASHLAR if k % 2 == 0 else darken(ASHLAR, .12)
        near = r * math.cos(math.radians(A1))
        for sgn in (-1, 1):
            xa, xb = sorted((sgn * near, sgn * IN))
            kerect(sh, xa, FZ + bottom, xb, FZ + bottom + HC, f"url(#{sh.lin(shade, 'v', .25, .5)})",
                   darken(ASHLAR, .6), .9)
            for a in arc_angles()[1:]:
                x = sgn * r * math.cos(math.radians(a))
                off = 0.5 * (k % 2) * (1 if sgn > 0 else -1)
                sh.line(*KE(x + off * 0.4, FZ + bottom), *KE(x + off * 0.4, FZ + bottom + HC), darken(ASHLAR, .5), .8)
            # The end face at 62°, lit from the archway.
            kerect(sh, xa if sgn > 0 else xb - 0.12, FZ + bottom, (xa + 0.12) if sgn > 0 else xb, FZ + bottom + HC,
                   lighten(ASHLAR, .15), op=.6)
    # NE: a bronze tripod brazier behind the bier's foot, then the bier.
    tripod(sh, 2.0)
    kerect(sh, 1.8, FZ, 3.6, FZ + 0.50, f"url(#{sh.lin(ASHLAR, 'v', .25, .5)})", darken(ASHLAR, .6), 1)
    kerect(sh, 1.8, FZ + 0.38, 3.6, FZ + 0.44, RED, op=.9)                          # a painted band
    kerect(sh, 1.85, FZ + 0.50, 3.55, FZ + 0.56, LINEN, darken(LINEN, .5), .7)       # the shroud
    mx, my = KE(3.3, FZ + 0.56)
    sh.path(f"M{f(mx - 7)} {f(my)} q0 -9 7 -10 q7 1 7 10 z", GOLD, darken(GOLD, .5), .8)   # the mask, face up
    sh.circle(*KE(2.2, FZ + 0.60), 3.5, GOLD, darken(GOLD, .5), .6)                        # a cup at the feet
    # NW: the offering table with gold cups and a bull's-head rhyton.
    table(sh, -2.6, 0.9, 0.70, col=ASHLAR)
    for dx in (-0.3, 0.0, 0.25):
        kerect(sh, -2.6 + dx - 0.05, FZ + 0.70, -2.6 + dx + 0.05, FZ + 0.80, GOLD, darken(GOLD, .5), .6)
    sh.path(f"M{f(KE(-2.35, 0)[0])} {f(KE(0, FZ + 0.70)[1])} l-6 -12 l-4 -4 l6 1 l6 -1 l-4 4 z", SOOT, "#0E0C09", .6)
    kit_cut_walls(sh, "Crypt")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(4.6, FZ + 2.3), "CORBELLED COURSES", "4 × 0.65 m, each 0.35 m further in"),
        (*KE(2.0, FZ + 1.1), "TRIPOD BRAZIER", "bronze, at the bier's foot · loot"),
        (*KE(3.3, FZ + 0.6), "GOLD DEATH-MASK", "on the shroud · the bier is loot"),
        (*KE(2.7, FZ + 0.25), "BIER", "1.80 × 0.70 × 0.50 m ashlar"),
    ], 610, 170, 380, slope=1.0)
    sh.callouts([
        (*KE(-2.6, FZ + 0.76), "OFFERING TABLE", "gold cups, a rhyton · loot"),
        (*KE(-4.6, FZ + 1.6), "FOUR ARCS", "28°–62°, gapped at the axes"),
    ], 330, 230, 300, anchor="end")
    kit_clear_note(sh, "Crypt", x=4.0, text="3.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Crypt", floor="#1E1B16", wall="#3A332A", trim=ASHLAR)
    for qx in (-1, 1):
        for qy in (-1, 1):
            # The bottom course's footprint filled; the three above it overhang the floor, so
            # their inner edges are dashed and the furniture beneath them stays in view.
            sh.path(poly_path([KP(x, y) for x, y in course_outline(COURSES[0][0], qx, qy)]), ASHLAR, "#0E0C09", .6)
            for r, _ in COURSES[1:]:
                n = len(arc_angles())
                sh.path(poly_path([KP(x, y) for x, y in course_outline(r, qx, qy)[:n]], closed=False),
                        "none", lighten(ASHLAR, .2), .9, extra=' stroke-dasharray="4 3"')
    sh.circle(*KP(0, 0), COURSES[0][0] * PK, "none", darken(STONE, .4), .8, op=.35)
    # NE: the bier (head east) and the tripod at its foot.
    plan_box(sh, 2.7, 2.35, 1.8, 0.7, ASHLAR)
    plan_box(sh, 2.7, 2.35, 1.7, 0.6, LINEN)
    sh.ellipse(*KP(3.3, 2.35), 0.11 * PK, 0.09 * PK, GOLD, darken(GOLD, .5), .6)
    sh.circle(*KP(2.0, 3.35), 0.3 * PK, BRONZE, "#0E0C09", .7)
    # NW: the offering table.
    plan_box(sh, -2.6, 2.6, 0.9, 0.6, ASHLAR)
    for dx in (-0.3, 0.0, 0.25):
        sh.circle(*KP(-2.6 + dx, 2.6), 0.05 * PK, GOLD)
    # SW: the chest and two amphorae.
    plan_box(sh, -2.8, -2.3, 1.0, 0.6, CYP)
    kprect(sh, -3.3, -2.33, -2.3, -2.27, BRONZE)
    for x, y in ((-2.0, -3.2), (-2.6, -3.4)):
        plan_jar(sh, x, y, 0.34, TERRA)
    # SE: the standing brazier.
    sh.circle(*KP(2.7, -2.7), 0.35 * PK, BRONZE, "#0E0C09", .7)
    sh.circle(*KP(2.7, -2.7), 0.25 * PK, MADDER_)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Crypt")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.16 · all open"),
                    ("CLEAR CROSS", "dashed · passes through the rings' four gaps"),
                    ("COURSES", "4 rings, r 4.90 → 3.85 m, top at 2.60 m"),
                    ("FILL", "solid behind the rings, out to the walls"),
                    ("LOOT", "L1–L4: bier, tripod, offering table, chest")])
    return sh
