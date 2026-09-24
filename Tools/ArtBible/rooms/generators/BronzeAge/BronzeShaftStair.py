"""BronzeShaftStair: the shaft grave and its stair (stands in for CryptStairwell).

The kit's stair-to-dais (castle_builders._stair_to_dais) in ashlar: four steps up
the west wall of the NW quadrant to a 1.00 m dais with a painted larnax on it. The
open mouth of a shaft grave in the SE, kerbed in stone, sheerlegs over it and the
digger's basket still hanging on its rope; storage jars below the north wall in the
NE, an offering table and amphorae SW, bronze braziers either side of the north
archway. Section A-A east-west at y = 0, looking north: the steps face-on in front
of the dais, the larnax end-on on top, the jars and the NE brazier; the sheerlegs
stand in front of the cut (dashed).
"""
from _bronze import *

ASHLAR = "#9A8C72"
DAIS = 1.00
STEPS = 4
SHAFT = (3.7, -3.4)                                   # the kerb's centre
ANCHORS = [(-4.4, 4.55, FZ + DAIS + 0.87), (-3.4, -3.4, FZ + 0.70), (3.7, -3.4, FZ + 0.80)]
JARS = [(2.4, 4.9), (3.5, 4.9), (4.6, 4.9)]


def brazier(sh, x, base=FZ):
    """The kit's standing brazier in elevation: a post, a flared bowl, the fire bed."""
    kerect(sh, x - 0.08, base, x + 0.08, base + 1.0, f"url(#{sh.lin(BRONZE, 'h', .3, .5)})", darken(BRONZE, .6), .7)
    bowl = [KE(x - 0.35, base + 1.25), KE(x - 0.20, base + 1.0), KE(x + 0.20, base + 1.0), KE(x + 0.35, base + 1.25)]
    sh.path(poly_path(bowl), f"url(#{sh.lin(BRONZE, 'h', .3, .5)})", darken(BRONZE, .6), .9)
    kerect(sh, x - 0.25, base + 1.23, x + 0.25, base + 1.31, MADDER_, darken(MADDER_, .5), .6)


def build():
    mats = [("limestone", STONE), ("ashlar", ASHLAR), ("larnax clay", TERRA), ("haematite", RED),
            ("timber", CYP), ("bronze", BRONZE), ("gold", GOLD)]
    sh = room_sheet("BronzeAge", "Crypt", "The Shaft Stair", mats, "≤ 2k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE ROOM, LOOKING NORTH")
    kit_glow(sh, -2.4, FZ + 1.3, MADDER_, rx=240, ry=170, strength=.3)
    kit_glow(sh, 4.4, FZ + 1.3, MADDER_, rx=240, ry=170, strength=.3)

    # ---- section ----
    kit_slab(sh, "#3A332A")
    kit_back_wall(sh, "Crypt", STONE, trim=ASHLAR, soot_depth=0.6)
    for x0, x1 in ((-IN, -1.3), (1.3, IN)):
        for k in range(1, 5):
            sh.line(*KE(x0, FZ + k * 0.55), *KE(x1, FZ + k * 0.55), darken(STONE, .35), .8, op=.6)
    # NE: the storage jars along the north wall, then the brazier in front of them.
    for x, _ in JARS:
        jar(sh, x, FZ, 1.20, 0.70, darken(TERRA, .1), bands=(0.62,), lid=STONE)
    brazier(sh, 4.4)
    # NW: the dais against the north wall, the larnax end-on on it, the steps face-on in front,
    # the highest (rearmost) first so each lower step overlaps the one behind it.
    kerect(sh, -IN, FZ, -3.3, FZ + DAIS, f"url(#{sh.lin(ASHLAR, 'v', .25, .5)})", darken(ASHLAR, .6), 1)
    larnax(sh, -4.4, 0.6, base=FZ + DAIS)
    for i in reversed(range(STEPS)):
        top = FZ + DAIS * (i + 1) / STEPS
        kerect(sh, -IN, FZ, -3.3, top, f"url(#{sh.lin(ASHLAR, 'v', .2 + .05 * i, .5)})", darken(ASHLAR, .6), .9)
        kerect(sh, -IN, top - 0.03, -3.3, top, lighten(ASHLAR, .2))
    brazier(sh, -2.4)
    # SE, in front of the cut: the sheerlegs over the shaft, dashed.
    for x in (2.4, 5.0):
        sh.line(*KE(x, FZ), *KE(x, FZ + 2.2), CYP, 2, dash="4 3", op=.6)
    sh.line(*KE(2.3, FZ + 2.15), *KE(5.1, FZ + 2.15), CYP, 3, dash="4 3", op=.6)
    sh.line(*KE(3.7, FZ + 2.15), *KE(3.7, FZ + 0.80), "#9C8A60", 1.2, dash="3 2", op=.7)
    kerect(sh, 3.48, FZ + 0.50, 3.92, FZ + 0.80, "none", CYP, 1.4, op=.7)
    kit_cut_walls(sh, "Crypt")
    khuman(sh, -1.0)
    sh.callouts([
        (*KE(4.6, FZ + 1.0), "STORAGE JARS", "three, 1.20 m, stone lids"),
        (*KE(3.7, FZ + 0.65), "DIGGER'S BASKET", "on its rope over the shaft · loot"),
        (*KE(4.4, FZ + 1.27), "BRONZE BRAZIER", "one each side of the N archway"),
        (*KE(2.9, FZ + 2.15), "SHEERLEGS (SE)", "in front of the cut, dashed · 2.20 m"),
    ], 610, 170, 380, slope=1.0)
    sh.callouts([
        (*KE(-4.4, FZ + DAIS + 0.5), "DAIS LARNAX", "painted, on the 1.00 m dais · loot"),
        (*KE(-4.6, FZ + 0.5), "FOUR STEPS", "0.25 m rise, 0.35 m tread, north"),
        (*KE(-3.4, FZ + 0.9), "DAIS", "2.20 × 2.20 m ashlar"),
    ], 330, 190, 340, anchor="end")
    kit_clear_note(sh, "Crypt", x=-2.9, text="3.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Crypt", floor="#1E1B16", wall="#3A332A", trim=ASHLAR)
    # NW: the steps (south to north) and the dais, the larnax on it.
    run = 1.4 / STEPS
    for i in range(STEPS):
        y0 = 1.9 + i * run
        kprect(sh, -IN, y0, -3.3, y0 + run, lighten(ASHLAR, .05 * i - .1), "#0E0C09", .6)
    kprect(sh, -IN, 3.3, -3.3, IN, lighten(ASHLAR, .12), "#0E0C09", .8)
    plan_box(sh, -4.4, 4.55, 0.66, 1.66, TERRA)
    kprect(sh, -4.42, 3.72, -4.38, 5.38, darken(TERRA, .45))
    sh.text(*KP(-4.4, 2.2), "UP", 8, "#1E1B16", "middle")
    sh.path(f"M{f(KP(-4.4, 2.35)[0])} {f(KP(-4.4, 2.35)[1])} l0 -26 l-4 6 m4 -6 l4 6", "none", "#1E1B16", 1.2)
    # NE: the jars and the brazier; NW brazier.
    for x, y in JARS:
        plan_jar(sh, x, y, 0.70, darken(TERRA, .1), lid=STONE)
    for x, y in ((4.4, 2.6), (-2.4, 2.8)):
        sh.circle(*KP(x, y), 0.35 * PK, BRONZE, "#0E0C09", .7)
        sh.circle(*KP(x, y), 0.25 * PK, MADDER_)
    # SE: the kerbed shaft, the sheerlegs' feet and crossbar, the basket.
    sx, sy = SHAFT
    kprect(sh, sx - 1.1, sy - 0.7, sx + 1.1, sy + 0.7, STONE, "#0E0C09", .8)
    kprect(sh, sx - 0.9, sy - 0.5, sx + 0.9, sy + 0.5, "#050403")
    for x in (2.4, 5.0):
        sh.line(*KP(x, sy - 0.6), *KP(x, sy + 0.6), CYP, 3)
        for y in (sy - 0.6, sy + 0.6):
            sh.circle(*KP(x, y), 0.06 * PK, CYP)
    sh.line(*KP(2.3, sy), *KP(5.1, sy), CYP, 2.4)
    sh.circle(*KP(sx, sy), 0.22 * PK, CYP, "#0E0C09", .7)
    # SW: the offering table, gold cups, amphorae.
    plan_box(sh, -3.4, -3.4, 0.6, 0.6, ASHLAR)
    for dx in (-0.15, 0.15):
        sh.circle(*KP(-3.4 + dx, -3.4), 0.05 * PK, GOLD)
    for x, y in ((-4.6, -4.6), (-4.8, -3.8), (-3.8, -4.8)):
        plan_jar(sh, x, y, 0.34, TERRA)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Crypt")
    socket_label(sh, *KP(sx, sy - 1.05), "SHAFT GRAVE")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.16 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("STAIR", "NW, 4 steps north up the west wall to the dais"),
                    ("SHAFT", "SE, kerbed 2.20 × 1.40 m, sheerlegs 2.20 m"),
                    ("LOOT", "L1–L3: dais larnax, offering table, basket")])
    return sh
