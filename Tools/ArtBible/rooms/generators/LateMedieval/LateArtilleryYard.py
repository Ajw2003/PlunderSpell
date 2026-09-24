"""LateArtilleryYard: the artillery yard (stands in for StableBlock).

The first guns, and everything they eat. NW: a bombard, a wrought-iron barrel bound
in hoops with its narrower powder chamber behind, lashed into a timber sledge. NE: a
pyramid of stone gunstones and a few loose. SE: powder kegs, five on the floor and
one on top. SW: a two-wheeled powder cart, a spare wheel against the west wall, and
a rack on the south wall with a rammer and a sponge. Section A-A east-west at y = 0,
looking north: the bombard side-on on its sledge, the gunstone pyramid.
"""
import math

from _late import *

SLEDGE = (-3.6, 4.2, 3.0, 0.9, 0.35)
BORE_R, CHAMBER_R = 0.36, 0.25
PYR = (3.6, 4.2, 0.22)                                 # pyramid centre and ball radius
KEGS = [(3.2, -4.8), (3.75, -4.8), (4.3, -4.8), (3.45, -4.25), (4.0, -4.25)]
KEG_TOP = (3.75, -4.52)                                # the keg lying on top of the others
KEG_R, KEG_H = 0.25, 0.5
CART = (-3.2, -3.8, 1.6, 0.9)
ANCHORS = [(KEGS[0][0], KEGS[0][1], FZ + KEG_H), (KEGS[4][0], KEGS[4][1], FZ + KEG_H),
           (CART[0], CART[1], FZ + 0.85)]


def pyramid_balls(cx, cy, r):
    """Ball centres of a 3-2-1 square pyramid, each layer settled into the one below
    (and 0.02 m lower still, so the balls overlap rather than just touch)."""
    out = []
    step = 2 * r - 0.02
    for n, layer in ((3, 0), (2, 1), (1, 2)):
        z = r - 0.01 + layer * (step / math.sqrt(2) - 0.02)
        off = (n - 1) * step / 2
        for i in range(n):
            for j in range(n):
                out.append((cx - off + i * step, cy - off + j * step, z))
    return out


def build():
    mats = [("sandstone", SAND), ("iron", IRON), ("oak", OAK), ("gunstone", "#8A8474"), ("rope", "#9C8A60"),
            ("madder", ESTATE), ("soot", SOOT)]
    sh = room_sheet("LateMedieval", "OuterBailey", "The Artillery Yard", mats, "≤ 2.4k tris (kit)",
                    "SECTION A–A · E–W THROUGH THE YARD, LOOKING NORTH")
    kit_glow(sh, 0, FZ + 1.6, "#5A5448", rx=380, ry=200, strength=.2)

    # ---- section ----
    kit_slab(sh, FLAG)
    kit_back_wall(sh, "OuterBailey", SAND, soot=SOOT, trim=BRICK, soot_depth=0.6)
    ashlar_courses(sh, "OuterBailey")
    # NE: the pyramid face-on (the front row of each layer).
    px, py, r = PYR
    for x, y, z in sorted(pyramid_balls(px, py, r), key=lambda b: -b[1]):
        sh.circle(*KE(x, FZ + z), r * SK, f"url(#{sh.lin('#8A8474', 'h', .35, .55)})", darken("#8A8474", .6), .8)
    for x in (2.3, 2.75):
        sh.circle(*KE(x, FZ + r), r * SK, "#8A8474", darken("#8A8474", .6), .8)
    # NW: the sledge and the bombard on it, side-on.
    sx, sy, sw, sd, sh_ = SLEDGE
    kerect(sh, sx - sw / 2, FZ, sx + sw / 2, FZ + sh_, f"url(#{sh.lin(OAK, 'v', .25, .5)})", darken(OAK, .6), 1)
    for k in range(3):
        xx = sx - sw / 2 + 0.3 + k * 1.2
        kerect(sh, xx, FZ + sh_ - 0.05, xx + 0.12, FZ + sh_ + 0.45, darken(OAK, .15), darken(OAK, .6), .6)
    bz = FZ + sh_ + BORE_R - 0.06
    kerect(sh, sx - 1.5, bz - CHAMBER_R, sx - 0.5, bz + CHAMBER_R, f"url(#{sh.lin(IRON, 'v', .4, .6)})", "#0E0C09", .9)
    kerect(sh, sx - 0.5, bz - BORE_R, sx + 1.4, bz + BORE_R, f"url(#{sh.lin(IRON, 'v', .4, .6)})", "#0E0C09", .9)
    for k in range(7):
        xx = sx - 0.45 + k * 0.3
        kerect(sh, xx, bz - BORE_R - 0.02, xx + 0.06, bz + BORE_R + 0.02, lighten(IRON, .2), "#0E0C09", .5)
    sh.ellipse(*KE(sx + 1.42, bz), 0.05 * SK, BORE_R * SK * .75, "#0E0C09")
    for xx in (sx - 1.0, sx + 0.9):
        sh.line(*KE(xx, FZ + sh_), *KE(xx + 0.1, bz + BORE_R + 0.04), "#9C8A60", 2)
    kit_cut_walls(sh, "OuterBailey")
    khuman(sh, -0.9)
    sh.callouts([
        (*KE(px, FZ + 0.9), "GUNSTONES", "0.44 m, a 3-2-1 pyramid"),
    ], 610, 260, 260, slope=1.0)
    sh.callouts([
        (*KE(sx + 0.6, bz + 0.2), "BOMBARD", "hooped iron, 0.72 m bore"),
        (*KE(sx - 1.2, bz), "POWDER CHAMBER", "0.50 m, behind the bore"),
        (*KE(sx + 0.5, FZ + 0.15), "TIMBER SLEDGE", "3.00 × 0.90 m, roped"),
    ], 330, 190, 330, anchor="end")
    kit_clear_note(sh, "OuterBailey", x=0.0, text="3.60 clear · open roof")

    # ---- plan ----
    kit_plan(sh, "OuterBailey", floor="#302A20", wall="#3A332A", trim=BRICK)
    plan_box(sh, sx, sy, sw, sd, OAK)
    plan_box(sh, sx - 1.0, sy, 1.0, 2 * CHAMBER_R, IRON)
    plan_box(sh, sx + 0.45, sy, 1.9, 2 * BORE_R, IRON)
    for x, y, z in sorted(pyramid_balls(px, py, r), key=lambda b: b[2]):
        plan_disc(sh, x, y, r, lighten("#8A8474", z * .2))
    for x in (2.3, 2.75):
        plan_disc(sh, x, py - 1.0, r, "#8A8474")
    for x, y in KEGS:
        plan_disc(sh, x, y, KEG_R, OAK)
        plan_disc(sh, x, y, KEG_R * .8, None, IRON)
    plan_cask(sh, KEG_TOP[0], KEG_TOP[1], KEG_H, KEG_R)
    cx, cy, cw, cd = CART
    plan_box(sh, cx, cy, cw, cd, OAK)
    for dy in (-0.55, 0.55):
        plan_box(sh, cx, cy + dy, 0.9, 0.08, darken(OAK, .2))
    for dy in (-0.3, 0.3):
        kprect(sh, cx + cw / 2, cy + dy - 0.03, cx + cw / 2 + 1.0, cy + dy + 0.03, OAK)
    kprect(sh, -IN, -3.9, -IN + 0.1, -2.9, darken(OAK, .2))
    kprect(sh, -5.2, -IN + 0.05, -2.2, -IN + 0.25, OAK, "#0E0C09", .5)
    kit_loot(sh, [(x, y) for x, y, _ in ANCHORS])
    kit_section_line(sh, 0)
    kit_arch_labels(sh, "OuterBailey")
    socket_label(sh, *KP(3.75, -3.5), "POWDER KEGS")
    socket_label(sh, *KP(-3.2, -2.7), "POWDER CART")
    socket_label(sh, *KP(-3.7, -4.85), "RAMMER + SPONGE")
    kit_legend(sh, [("ARCHWAYS", "4, centred · 2.60 × 2.59 · all open"),
                    ("CLEAR CROSS", "dashed · |x|,|y| < 1.6 m kept free to 2 m"),
                    ("NW / NE", "bombard on its sledge · gunstone pyramid"),
                    ("SE / SW", "powder kegs · cart, spare wheel, rammer rack"),
                    ("LOOT", "L1–L3: two keg tops, the cart")])
    return sh
