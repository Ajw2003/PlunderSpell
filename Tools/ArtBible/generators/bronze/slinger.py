from lib import *

LINEN, RAWHIDE, CORD, STONE, SKIN, RED = "#CFC3A2", "#8C6A48", "#6E5A44", "#8E8778", "#9C6E4E", "#8E3F2C"
HAIR = "#3A2A1E"
SOOT = "#2B231B"


def muscles_front(F):
    c = darken(SKIN, 0.35)
    F.curve([(-0.13, 1.27), (-0.07, 1.22), (-0.01, 1.235)], c, 1.2, op=.7)
    F.curve([(0.13, 1.27), (0.07, 1.22), (0.01, 1.235)], c, 1.2, op=.7)
    F.curve([(0, 1.22), (0.003, 1.10), (0, 1.02)], c, 1.0, op=.5)
    for h in (1.16, 1.10):
        F.curve([(-0.05, h), (0, h - 0.008), (0.05, h)], c, 0.9, op=.4)
    F.curve([(-0.17, 1.36), (-0.19, 1.30)], lighten(SKIN, 0.45), 1.6, op=.7)


def pouch(sh, F, x, h, w, hgt, base):
    pts = [(x - w * .45, h + hgt * .5), (x + w * .45, h + hgt * .5), (x + w * .55, h), (x + w * .4, h - hgt * .45),
           (x, h - hgt * .52), (x - w * .4, h - hgt * .45), (x - w * .55, h)]
    d = F.shape(pts, base, light=0.35)
    F.curve([(x - w * .45, h + hgt * .38), (x, h + hgt * .30), (x + w * .45, h + hgt * .38)], darken(base, .5), 1.2)
    for k in range(3):
        F.ell(x - w * .2 + k * w * .2, h + hgt * .5, 0.018, 0.012, STONE, light=0.4)
    sh.flecks(d, F.bbox(pts), 14, lighten(STONE, .5), .5, 1.2, .6)   # stone dust


def slinger_front(sh, cx=470):
    F = Fig(sh, cx)
    # bandana tails behind
    F.curve([(0.05, 1.60), (0.12, 1.55), (0.14, 1.46)], darken(LINEN, 0.2), 5)
    # legs
    for s in (-1, 1):
        F.limb((0.08 * s, 0.66), (0.075 * s, 0.09), 0.10, 0.06, SKIN)
        F.curve([(0.05 * s, 0.49), (0.075 * s, 0.505), (0.10 * s, 0.49)], darken(SKIN, .35), 1.2, op=.7)
        F.ell(0.085 * s, 0.03, 0.055, 0.03, RAWHIDE)
        for k in range(4):
            F.line((0.045 * s, 0.05 + k * 0.05), (0.105 * s, 0.07 + k * 0.05), RAWHIDE, 1.3)
    # arms
    F.limb((-0.185, 1.34), (-0.23, 1.09), 0.085, 0.07, SKIN)
    F.limb((-0.23, 1.09), (-0.25, 0.88), 0.068, 0.055, SKIN)
    F.limb((0.185, 1.34), (0.24, 1.09), 0.085, 0.07, SKIN)
    F.limb((0.24, 1.09), (0.27, 0.89), 0.068, 0.055, SKIN)
    F.limb((0.245, 1.05), (0.265, 0.92), 0.078, 0.07, RAWHIDE, cap=False)      # bracer
    for k in range(3):
        F.line((0.225, 1.03 - k * 0.04), (0.29, 1.02 - k * 0.04), darken(RAWHIDE, .5), .9)
    F.ell(0.272, 0.855, 0.034, 0.045, SKIN, light=.35)
    # torso
    tor = [(-0.05, 1.43), (-0.19, 1.385), (-0.20, 1.31), (-0.165, 1.23), (-0.14, 1.10), (-0.13, 0.99), (0.13, 0.99),
           (0.14, 1.10), (0.165, 1.23), (0.20, 1.31), (0.19, 1.385), (0.05, 1.43)]
    d = F.shape(tor, SKIN, light=0.3)
    muscles_front(F)
    sh.flecks(d, F.bbox(tor), 20, darken(SKIN, .3), .6, 1.4, .35)
    # baldric over right shoulder to left hip
    F.curve([(-0.15, 1.39), (-0.02, 1.22), (0.12, 1.02)], LINEN, 5.5)
    F.curve([(-0.15, 1.39), (-0.02, 1.22), (0.12, 1.02)], darken(LINEN, .35), 1, op=.6)
    # kilt
    kil = [(-0.14, 1.00), (0.14, 1.00), (0.17, 0.80), (0.20, 0.58), (0.05, 0.57), (-0.06, 0.60), (-0.19, 0.58),
           (-0.17, 0.80)]
    dk = F.shape(kil, LINEN, light=0.2, dark=0.5)
    bb = F.bbox(kil)
    F.curve([(0.06, 0.99), (0.07, 0.80), (0.05, 0.575)], darken(LINEN, .4), 1.4)         # front overlap edge
    for k in range(6):
        F.line((0.055 + 0.002 * k, 0.60 + k * 0.06), (0.03, 0.59 + k * 0.06), darken(LINEN, .5), 1.2)  # tassels
    for x in (-0.12, -0.06, 0.12):
        F.curve([(x, 0.96), (x - 0.01, 0.78), (x - 0.02, 0.60)], darken(LINEN, .28), 1.2, op=.6)
    sh.clipped(dk, f'<rect x="{bb[0]}" y="{G - 0.62 * S:.1f}" width="{bb[2] - bb[0]:.1f}" height="{0.04 * S:.1f}" fill="{RED}" opacity=".9"/>')
    sh.flecks(dk, (bb[0], G - 0.9 * S, bb[0] + 40, bb[3]), 30, lighten(STONE, .5), .6, 1.6, .6)
    sh.flecks(dk, (bb[0], G - 0.7 * S, bb[2], bb[3]), 30, SOOT, .6, 1.4, .45)
    F.shape([(-0.14, 0.99), (0.14, 0.99), (0.142, 1.03), (-0.142, 1.03)], RAWHIDE, smooth=False)
    # pouches
    pouch(sh, F, -0.19, 0.86, 0.14, 0.22, RAWHIDE)
    pouch(sh, F, 0.15, 0.90, 0.10, 0.15, darken(RAWHIDE, .1))
    # sling hanging from right hand (viewer left)
    F.curve([(-0.25, 0.84), (-0.265, 0.60), (-0.275, 0.36), (-0.28, 0.20)], CORD, 1.6)
    F.curve([(-0.25, 0.84), (-0.24, 0.60), (-0.255, 0.36), (-0.27, 0.20)], CORD, 1.6)
    F.ell(-0.275, 0.18, 0.03, 0.03, RAWHIDE)
    F.ell(-0.275, 0.18, 0.018, 0.022, STONE, light=.45)
    F.ell(-0.25, 0.855, 0.035, 0.045, SKIN, light=.35)
    # neck, head
    F.limb((0, 1.40), (0, 1.47), 0.085, 0.08, SKIN)
    F.ell(0, 1.55, 0.075, 0.10, SKIN, light=.3)
    for s in (-1, 1):
        F.ell(0.03 * s, 1.555, 0.011, 0.006, "#1E1A16")
        F.curve([(0.048 * s, 1.575), (0.028 * s, 1.582), (0.012 * s, 1.577)], HAIR, 1.5)
        F.ell(0.078 * s, 1.55, 0.011, 0.022, SKIN)
    F.curve([(0.0, 1.57), (0.007, 1.525), (-0.005, 1.518)], darken(SKIN, .4), 1.1)
    F.curve([(-0.018, 1.495), (0, 1.49), (0.018, 1.495)], darken(SKIN, .55), 1.2)
    # curly hair + bandana
    hair = [(-0.08, 1.58), (-0.085, 1.63), (-0.05, 1.655), (0, 1.66), (0.05, 1.655), (0.085, 1.63), (0.08, 1.58)]
    F.shape(hair, HAIR, flat=True)
    for k in range(9):
        x = -0.075 + k * 0.019
        F.dot(x, 1.585 + 0.012 * math.sin(k), 3.2, darken(HAIR, .3))
    F.shape([(-0.083, 1.60), (0.083, 1.60), (0.078, 1.635), (-0.078, 1.635)], LINEN, light=.3)
    F.curve([(-0.08, 1.62), (0.08, 1.62)], darken(LINEN, .35), .8)


def slinger_side(sh, cx=820):
    F = Fig(sh, cx)
    # whirl arc (motion guide)
    x0, y0 = F.p(-0.12, 1.45)
    sh.ellipse(x0, y0, 0.42 * S, 0.14 * S, "none", "#9A9078", .8, op=.5, rot=-18)
    sh.path(f"M{x0 - 0.42 * S:.1f} {y0 + 12:.1f} l-6 -9 l10 1", "none", "#9A9078", .8, op=.6)
    # far arm, extended toward target (aiming)
    F.limb((0.01, 1.34), (0.17, 1.30), 0.08, 0.068, darken(SKIN, .25))
    F.limb((0.17, 1.30), (0.34, 1.33), 0.066, 0.055, darken(SKIN, .25))
    F.limb((0.20, 1.31), (0.30, 1.325), 0.078, 0.07, darken(RAWHIDE, .2), cap=False)
    F.ell(0.365, 1.335, 0.035, 0.03, darken(SKIN, .25))
    # far leg (back, pushing off)
    F.limb((-0.03, 0.66), (-0.13, 0.37), 0.10, 0.075, darken(SKIN, .25))
    F.limb((-0.13, 0.37), (-0.20, 0.09), 0.075, 0.058, darken(SKIN, .25))
    F.shape([(-0.26, 0.02), (-0.12, 0.0), (-0.11, 0.035), (-0.20, 0.09), (-0.25, 0.07)], darken(RAWHIDE, .25))
    # near leg (forward)
    F.limb((0.03, 0.66), (0.10, 0.37), 0.105, 0.078, SKIN)
    F.limb((0.10, 0.37), (0.08, 0.09), 0.078, 0.06, SKIN)
    F.shape([(0.03, 0.0), (0.21, 0.0), (0.215, 0.03), (0.11, 0.075), (0.03, 0.06)], RAWHIDE)
    for k in range(4):
        F.line((0.055, 0.06 + k * 0.05), (0.11, 0.08 + k * 0.05), darken(RAWHIDE, .35), 1.3)
    # bandana tails
    F.curve([(-0.08, 1.61), (-0.16, 1.60), (-0.22, 1.56)], darken(LINEN, .15), 5)
    F.curve([(-0.08, 1.60), (-0.15, 1.56), (-0.19, 1.50)], darken(LINEN, .25), 4.5)
    # torso (leaning slightly forward)
    tor = [(-0.02, 1.44), (-0.10, 1.40), (-0.115, 1.30), (-0.10, 1.15), (-0.09, 0.99), (0.10, 0.99), (0.10, 1.10),
           (0.13, 1.25), (0.12, 1.36), (0.06, 1.43)]
    d = F.shape(tor, SKIN, direction="hr", light=.3)
    F.curve([(0.12, 1.30), (0.09, 1.24), (0.07, 1.23)], darken(SKIN, .35), 1.1, op=.7)
    sh.flecks(d, F.bbox(tor), 14, darken(SKIN, .3), .6, 1.4, .35)
    F.curve([(0.08, 1.42), (-0.02, 1.22), (-0.09, 1.02)], LINEN, 5)
    # kilt
    kil = [(-0.095, 1.00), (0.10, 1.00), (0.13, 0.80), (0.17, 0.60), (0.02, 0.575), (-0.12, 0.59), (-0.13, 0.80)]
    dk = F.shape(kil, LINEN, direction="hr", light=.2, dark=.5)
    bb = F.bbox(kil)
    sh.clipped(dk, f'<rect x="{bb[0]}" y="{G - 0.63 * S:.1f}" width="{bb[2] - bb[0]:.1f}" height="{0.04 * S:.1f}" fill="{RED}" opacity=".9"/>')
    sh.flecks(dk, (bb[0], G - 0.7 * S, bb[2], bb[3]), 25, SOOT, .6, 1.4, .45)
    for x in (-0.06, 0.03, 0.09):
        F.curve([(x, 0.97), (x + 0.01, 0.78), (x + 0.03, 0.60)], darken(LINEN, .28), 1.2, op=.6)
    F.shape([(-0.095, 0.99), (0.10, 0.99), (0.102, 1.03), (-0.097, 1.03)], RAWHIDE, smooth=False)
    pouch(sh, F, -0.02, 0.86, 0.15, 0.23, RAWHIDE)
    # knife at back of belt
    F.shape([(-0.10, 1.02), (-0.085, 1.03), (-0.14, 0.82), (-0.155, 0.83)], darken(RAWHIDE, .2), smooth=False)
    # neck + head
    F.limb((0.0, 1.41), (0.02, 1.48), 0.09, 0.085, SKIN)
    F.ell(0.03, 1.55, 0.088, 0.10, SKIN, light=.3)
    F.shape([(0.113, 1.565), (0.133, 1.53), (0.114, 1.52)], SKIN, smooth=False)
    F.ell(0.08, 1.56, 0.01, 0.006, "#1E1A16")
    F.curve([(0.06, 1.577), (0.09, 1.583), (0.108, 1.577)], HAIR, 1.4)
    F.curve([(0.095, 1.49), (0.11, 1.488)], darken(SKIN, .55), 1.2)
    hair = [(-0.06, 1.50), (-0.075, 1.58), (-0.05, 1.645), (0.02, 1.66), (0.09, 1.64), (0.11, 1.60), (0.05, 1.59),
            (0.0, 1.55), (-0.03, 1.49)]
    F.shape(hair, HAIR, flat=True)
    for k in range(7):
        F.dot(-0.06 + k * 0.012, 1.52 + k * 0.015, 3.2, darken(HAIR, .3))
    F.ell(0.02, 1.545, 0.015, 0.025, SKIN, light=.4)
    F.shape([(-0.075, 1.60), (0.105, 1.605), (0.10, 1.64), (-0.07, 1.635)], LINEN, light=.3)
    F.dot(-0.075, 1.615, 4.5, darken(LINEN, .2))
    # near arm drawn back in wind-up, sling hanging in a loop
    F.limb((-0.01, 1.34), (-0.12, 1.16), 0.088, 0.072, SKIN)
    F.limb((-0.12, 1.16), (-0.24, 1.25), 0.07, 0.058, SKIN)
    F.ell(-0.265, 1.26, 0.035, 0.04, SKIN, light=.35)
    F.curve([(-0.27, 1.24), (-0.33, 1.00), (-0.36, 0.80), (-0.34, 0.66)], CORD, 1.6)
    F.curve([(-0.26, 1.24), (-0.29, 1.00), (-0.31, 0.80), (-0.32, 0.66)], CORD, 1.6)
    F.ell(-0.33, 0.64, 0.035, 0.028, RAWHIDE, rot=15)
    F.ell(-0.33, 0.645, 0.022, 0.018, STONE, light=.45)
    # sling bullet detail inset
    ix, iy = 640, 190
    sh.ellipse(ix, iy, 16, 11, f"url(#{sh.lin(STONE, 'h', .4, .45)})", darken(STONE, .6), 1)
    sh.ellipse(ix + 44, iy, 16, 11, f"url(#{sh.lin('#6E6A62', 'h', .4, .45)})", darken(STONE, .6), 1)
    sh.text(ix - 18, iy + 30, "SLING BULLETS 1:2", 10, "#9A9078")
    sh.text(ix - 18, iy + 43, "river stone · cast lead", 10, "#635C4C")


def build():
    sh = Sheet("enemy", "THE BRONZE AGE · ENEMY · RANGED", "Wall Slinger", "H 1.66 m · ≤ 8k tris · 2048²",
               "1 m = 220 px · ground y 690",
               [("linen", LINEN), ("rawhide", RAWHIDE), ("wool cord", CORD), ("river stone", STONE), ("skin", SKIN),
                ("haematite", RED)], seed=21)
    enemy_furniture(sh)
    height_mark(sh, 1.66, x1=880, label="1.66 m (bandana)")
    for cx in (470, 820):
        sh.ellipse(cx + 10, 690, 85, 7, "#0E0C09", op=.55)
    slinger_front(sh)
    slinger_side(sh)
    sh.callouts([
        (820 + 0.03 * S, G - 1.62 * S, "LINEN BANDANA", "knot + two 0.20 m tails"),
        (820 - 0.33 * S, G - 0.645 * S, "SLING", "2 cords 0.80 m · hide cradle"),
        (820 + 0.25 * S, G - 1.32 * S, "RAWHIDE BRACER", "laced, left forearm"),
        (820 - 0.02 * S, G - 1.20 * S, "LINEN BALDRIC", "right shoulder to left hip"),
        (820 - 0.02 * S, G - 0.80 * S, "STONE POUCH", "0.22 × 0.14 × 0.24 m, bulging"),
        (820 + 0.12 * S, G - 0.61 * S, "WRAPPED KILT", "0.42 m, red border, tassels"),
        (820 + 0.12 * S, G - 0.03 * S, "SANDALS", "rawhide thongs to mid-calf")], 968, 175, 640)
    sh.callout(470 - 0.29 * S, G - 0.18 * S, 385, 560, "STONE IN CRADLE", "idle: hangs from wrist", anchor="end", elbow=False)
    return sh
