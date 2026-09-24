"""LateEffigyCrypt: the founders' crypt (stands in for CryptChamberFinal).

NE: the founder's tomb chest, arcaded sandstone, his gilded effigy on the lid, under a
four-post oak canopy hung with madder; before it a candle hearse, a triangular iron
frame of candles. NW, SE, SW: lesser tomb chests, plain slabs with a brass plate let
into each lid. Section A-A east-west at y = 0, looking north: the canopy and tomb
side-on, the hearse in front, the NW tomb.
"""
from _late import *

ALAB = "#D8D0BE"
FOUNDER = (3.6, 4.2, 2.2, 1.0, 0.9)                # x, y, length (E–W), width, height
CANOPY_H = 2.6
POSTS = [(dx, dy) for dx in (-1.3, 1.3) for dy in (-0.7, 0.7)]
HEARSE = (3.6, 2.5, 1.2, 1.1)                      # x, y, width, height
LESSER = [(-3.6, 4.6), (3.6, -4.6), (-3.6, -4.6)]
LESSER_SIZE = (2.0, 0.8, 0.8)
ANCHORS = [(FOUNDER[0], FOUNDER[1] + 0.36, FZ + FOUNDER[4])] + [(x, y, FZ + LESSER_SIZE[2]) for x, y in LESSER]


def tomb_side(sh, x, l, h, base=FZ, col=SAND, arcades=4):
    """A tomb chest side-on: plinth, arcaded panel, lid."""
    kerect(sh, x - l / 2, base, x + l / 2, base + h, f"url(#{sh.lin(col, 'v', .25, .5)})", darken(col, .6), 1)
    w = (l - 0.3) / max(arcades, 1)
    for k in range(arcades):
        x0 = x - l / 2 + 0.15 + k * w
        kerect(sh, x0 + 0.04, base + 0.12, x0 + w - 0.04, base + h - 0.15, "none", darken(col, .35), .8)
        sh.path(f"M{f(KE(x0 + 0.04, base + h - 0.3)[0])} {f(KE(0, base + h - 0.3)[1])} "
                f"q{f((w - 0.08) * SK / 2)} -12 {f((w - 0.08) * SK)} 0", "none", darken(col, .35), .8)
    kerect(sh, x - l / 2 - 0.04, base + h - 0.08, x + l / 2 + 0.04, base + h, lighten(col, .1), darken(col, .6), .8)


def build():
    mats = [("sandstone", SAND), ("gilt", GOLD), ("oak", OAK), ("madder", ESTATE), ("iron", IRON),
            ("linen", LINEN), ("brass", "#B98A34")]
    sh = room_sheet("LateMedieval", "Crypt", "The Effigy Crypt", mats, "≤ 1.6k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE CRYPT, LOOKING NORTH")
    kit_glow(sh, 3.6, FZ + 1.2, "#8A6A30", rx=280, ry=180, strength=.45)

    # ---- section ----
    kit_slab(sh, "#2A241C")
    kit_back_wall(sh, "Crypt", SAND, soot=SOOT, trim="#3A342A", soot_depth=0.7)
    ashlar_courses(sh, "Crypt")
    fx, fy, fl, fw, fh = FOUNDER
    # The canopy: far posts, hangings behind, the tester, the near posts.
    kerect(sh, fx - 1.3, FZ + fh + 0.3, fx + 1.3, FZ + CANOPY_H, f"url(#{sh.lin(ESTATE, 'h', .3, .5)})",
           darken(ESTATE, .5), .8)
    tomb_side(sh, fx, fl, fh)
    ef = poly_path([KE(fx - 0.85, FZ + fh), KE(fx + 0.85, FZ + fh), KE(fx + 0.85, FZ + fh + 0.14),
                    KE(fx - 0.6, FZ + fh + 0.2), KE(fx - 0.85, FZ + fh + 0.14)])
    sh.path(ef, f"url(#{sh.lin(GOLD, 'v', .35, .6)})", darken(GOLD, .5), .9)
    sh.circle(*KE(fx + 0.75, FZ + fh + 0.22), 0.1 * SK, GOLD, darken(GOLD, .5), .8)
    sh.path(poly_path([KE(fx + 0.05, FZ + fh + 0.18), KE(fx + 0.15, FZ + fh + 0.34), KE(fx + 0.25, FZ + fh + 0.18)]),
            GOLD, darken(GOLD, .5), .7)
    for dx in (-1.3, 1.3):
        kerect(sh, fx + dx - 0.07, FZ, fx + dx + 0.07, FZ + CANOPY_H, f"url(#{sh.lin(OAK, 'h', .3, .5)})",
               darken(OAK, .6), .8)
    kerect(sh, fx - 1.4, FZ + CANOPY_H, fx + 1.4, FZ + CANOPY_H + 0.15, darken(ESTATE, .1), darken(ESTATE, .5), .9)
    for k in range(10):
        x = fx - 1.4 + k * 2.8 / 9
        sh.line(*KE(x, FZ + CANOPY_H), *KE(x, FZ + CANOPY_H - 0.1), GOLD, 1.2)
    # The hearse in front: a triangle of iron with candles up its edges.
    hx, hy, hw, hh = HEARSE
    sh.path(poly_path([KE(hx - hw / 2, FZ + 0.4), KE(hx + hw / 2, FZ + 0.4), KE(hx, FZ + 0.4 + hh)]), "none", IRON, 2.4)
    for dx in (-hw / 2 + 0.1, hw / 2 - 0.1):
        sh.line(*KE(hx + dx, FZ), *KE(hx + dx, FZ + 0.4), IRON, 2.4)
    for k in range(7):
        t = k / 6
        x = hx - hw / 2 + t * hw
        z = FZ + 0.4 + hh * (1 - abs(2 * t - 1))
        kerect(sh, x - 0.015, z, x + 0.015, z + 0.14, LINEN)
        sh.ellipse(*KE(x, z + 0.18), 2, 3.5, FIRE)
    # NW: a lesser tomb.
    tomb_side(sh, -3.6, LESSER_SIZE[0], LESSER_SIZE[2], arcades=0)
    kerect(sh, -4.4, FZ + LESSER_SIZE[2], -3.9, FZ + LESSER_SIZE[2] + 0.02, "#B98A34")
    kit_cut_walls(sh, "Crypt")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(fx - 0.2, FZ + fh + 0.15), "GILDED EFFIGY", "the founder, hands at prayer"),
        (*KE(fx + 0.5, FZ + CANOPY_H + 0.08), "CANOPY", "four oak posts, madder, gilt fringe"),
        (*KE(fx - 0.6, FZ + 0.5), "FOUNDER'S TOMB", "2.20 × 1.00 × 0.90 m · loot"),
        (*KE(hx, FZ + 0.4 + hh), "CANDLE HEARSE", "iron, seven candles"),
    ], 610, 170, 350, slope=1.0)
    sh.callouts([
        (*KE(-3.6, FZ + 0.4), "LESSER TOMB", "NW, SE, SW, a brass plate · loot"),
    ], 330, 260, 260, anchor="end")
    kit_clear_note(sh, "Crypt", x=0.0, text="3.00 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "Crypt", floor="#1E1B16", wall="#3A332A", trim="#3A342A")
    kprect(sh, fx - 1.4, fy - 0.8, fx + 1.4, fy + 0.8, ESTATE, op=.35)
    plan_box(sh, fx, fy, fl, fw, SAND)
    plan_box(sh, fx, fy, 1.7, 0.45, GOLD)
    for dx, dy in POSTS:
        plan_box(sh, fx + dx, fy + dy, 0.14, 0.14, OAK)
    kprect(sh, fx - 1.4, fy - 0.8, fx + 1.4, fy + 0.8, "none", GOLD, .8)
    plan_box(sh, hx, hy, hw, 0.12, "#6A6E74")
    for k in range(7):
        plan_disc(sh, hx - hw / 2 + k * hw / 6, hy, 0.03, LINEN)
    for x, y in LESSER:
        plan_box(sh, x, y, LESSER_SIZE[0], LESSER_SIZE[1], SAND)
        plan_box(sh, x - 0.55, y, 0.5, 0.35, "#B98A34")
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "Crypt")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.16 · all open"),
                    ("CLEAR CROSS", "dashed · the hearse stops at y 2.44"),
                    ("FOUNDER", "NE tomb, gilded effigy, canopy, hearse"),
                    ("LESSER", "3 plain tombs with brass plates, NW SE SW"),
                    ("LOOT", "L1–L4: founder's lid, three lesser tombs")])
    return sh
