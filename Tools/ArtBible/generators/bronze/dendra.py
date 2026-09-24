from lib import *

BRZ, TUSK, FELT, LINEN, BONE, HAIR = "#8E5E32", "#D6CCB0", "#5E4230", "#CFC3A2", "#E0D6BE", "#2B231B"
RUB = "#B08050"   # rubbed bronze edge (from the plate note)
SKIN = "#9C6E4E"


def rivets(F, pts, r=1.6):
    for x, h in pts:
        cx, cy = F.p(x, h)
        F.sh.circle(cx, cy, r, RUB)
        F.sh.circle(cx + .5, cy + .5, r * .5, darken(BRZ, .5))


def band(F, top, bot, wt, wb, x0=0.0, base=BRZ, lip=True, ridges=0, rv=True):
    pts = [(x0 - wt, top), (x0 + wt, top), (x0 + wb, bot), (x0 + wb * .5, bot - 0.012), (x0, bot - 0.016),
           (x0 - wb * .5, bot - 0.012), (x0 - wb, bot)]
    d = F.shape(pts, base, light=.42, dark=.55, sw=1.4, smooth=False)
    # rolled lip highlight + soot in overlap
    F.curve([(x0 - wb, bot + 0.004), (x0, bot - 0.012), (x0 + wb, bot + 0.004)], RUB, 2.2, op=.9)
    F.curve([(x0 - wt, top - 0.004), (x0 + wt, top - 0.004)], darken(base, .7), 2.5, op=.8)
    for k in range(ridges):
        h = top - (top - bot) * (k + 1) / (ridges + 1)
        w = wt + (wb - wt) * (k + 1) / (ridges + 1)
        F.curve([(x0 - w, h), (x0, h - 0.01), (x0 + w, h)], darken(base, .45), 1.1)
        F.curve([(x0 - w, h - 0.006), (x0, h - 0.016), (x0 + w, h - 0.006)], lighten(base, .35), .7, op=.7)
    if rv:
        n = int(wb * 2 / 0.05)
        rivets(F, [(x0 - wb + (i + .5) * (2 * wb / n), bot + 0.02) for i in range(n)], 1.4)
    F.sh.flecks(d, F.bbox(pts), 30, darken(base, .75), .6, 1.6, .5)
    # dent highlights
    for _ in range(3):
        x = F.sh.rng.uniform(x0 - wb * .6, x0 + wb * .6); h = F.sh.rng.uniform(bot + .02, top - .02)
        F.dot(x, h, 2.4, lighten(base, .45), op=.5)
    return d


def tusk_helmet(F, x0, crown, side=False):
    """Conical boar's-tusk helmet; rim at crown-0.20, top at crown."""
    rim = crown - 0.21
    w = 0.13 if not side else 0.14
    cone = [(x0 - w, rim), (x0 - w * .95, rim + 0.06), (x0 - w * .72, rim + 0.13), (x0 - w * .35, rim + 0.185),
            (x0, crown - 0.02), (x0 + w * .35, rim + 0.185), (x0 + w * .72, rim + 0.13), (x0 + w * .95, rim + 0.06),
            (x0 + w, rim)]
    d = F.shape(cone, FELT, light=.3)
    rows = 4
    for r in range(rows):
        h0 = rim + 0.01 + r * 0.043
        h1 = h0 + 0.036
        frac0 = 1 - (h0 - rim) / 0.21 * .78
        frac1 = 1 - (h1 - rim) / 0.21 * .78
        n = 12 - r * 2
        for i in range(n):
            t0 = -1 + 2 * i / n
            t1 = -1 + 2 * (i + 1) / n
            slant = 0.012 if r % 2 == 0 else -0.012
            pts = [(x0 + w * frac0 * t0 + .003, h0), (x0 + w * frac0 * t1 - .003, h0),
                   (x0 + w * frac1 * t1 - .003 + slant, h1), (x0 + w * frac1 * t0 + .003 + slant, h1)]
            shade = .0 + .35 * max(0, (t0 + 1) / 2) ** 1.5
            F.sh.path(poly_path(F.pts(pts)), darken(TUSK, shade), darken(TUSK, .55), .7)
            a, b = pts[0], pts[3]
            F.line(a, b, darken(TUSK, .45), .8, op=.6)
    # felt bands between rows
    for r in range(rows + 1):
        h = rim + 0.008 + r * 0.043
        fr = 1 - (h - rim) / 0.21 * .78
        F.curve([(x0 - w * fr, h), (x0 + w * fr, h)], darken(FELT, .3), 1.6)
    # knob + tuft
    F.ell(x0, crown - 0.012, 0.018, 0.012, BRZ, light=.5)
    F.curve([(x0, crown - 0.01), (x0 - 0.05, crown - 0.02), (x0 - 0.10, crown - 0.08)], HAIR, 4)
    F.curve([(x0, crown - 0.01), (x0 - 0.04, crown - 0.04), (x0 - 0.075, crown - 0.11)], HAIR, 3)
    F.curve([(x0, crown - 0.005), (x0 - 0.05, crown - 0.015), (x0 - 0.095, crown - 0.07)], lighten(HAIR, .35), .9, op=.8)
    return rim


def cheek(F, x, rim, flip=1):
    pts = [(x, rim + 0.005), (x + 0.045 * flip, rim + 0.005), (x + 0.05 * flip, rim - 0.08), (x + 0.02 * flip, rim - 0.14),
           (x - 0.005 * flip, rim - 0.12)]
    F.shape(pts, TUSK, light=.2, dark=.5, sw=1)
    for k in range(3):
        F.line((x + 0.003 * flip, rim - 0.03 - k * 0.035), (x + 0.045 * flip, rim - 0.025 - k * 0.035), darken(TUSK, .45), .8)


def rapier(F, hand, tip):
    (hx, hh), (tx, th) = hand, tip
    dx, dh = tx - hx, th - hh
    L = math.hypot(dx, dh)
    ux, uh = dx / L, dh / L
    nx, nh = -uh, ux
    g = (hx + ux * 0.05, hh + uh * 0.05)
    pts = [(g[0] + nx * 0.02, g[1] + nh * 0.02), (tx, th), (g[0] - nx * 0.02, g[1] - nh * 0.02)]
    F.shape(pts, BRZ, smooth=False, light=.5, sw=1)
    F.line(g, (tx - ux * 0.05, th - uh * 0.05), RUB, 1.2)
    # horned guard
    F.curve([(g[0] + nx * 0.07 - ux * .03, g[1] + nh * 0.07 - uh * .03), g, (g[0] - nx * 0.07 - ux * .03, g[1] - nh * 0.07 - uh * .03)], BRZ, 4)
    # grip + pommel
    F.line((hx - ux * 0.02, hh - uh * 0.02), (hx - ux * 0.11, hh - uh * 0.11), BONE, 5)
    F.ell(hx - ux * 0.14, hh - uh * 0.14, 0.025, 0.025, BONE, light=.3)


def dendra_front(sh, cx=470):
    F = Fig(sh, cx)
    # legs with greaves
    for s in (-1, 1):
        F.limb((0.10 * s, 0.62), (0.095 * s, 0.10), 0.13, 0.08, SKIN)
        F.limb((0.097 * s, 0.40), (0.095 * s, 0.10), 0.12, 0.09, BRZ, light=.45, cap=False)
        F.curve([(0.06 * s, 0.40), (0.1 * s, 0.395), (0.14 * s, 0.40)], RUB, 2)
        for k in range(4):
            F.line((0.06 * s, 0.35 - k * 0.07), (0.13 * s, 0.35 - k * 0.07), darken(BRZ, .5), .8, op=.6)
        F.ell(0.10 * s, 0.035, 0.07, 0.035, FELT)
    # linen tunic below rings
    F.shape([(-0.30, 0.66), (0.30, 0.66), (0.26, 0.50), (0.0, 0.48), (-0.26, 0.50)], LINEN, light=.2, dark=.5)
    for x in (-0.15, -0.05, 0.08, 0.18):
        F.line((x, 0.64), (x * 1.05, 0.50), darken(LINEN, .3), 1.1, op=.6)
    # arms (under pauldrons) with upper arm guards
    for s in (-1, 1):
        F.limb((0.29 * s, 1.30), (0.34 * s, 1.05), 0.11, 0.09, LINEN)
        F.limb((0.29 * s, 1.30), (0.33 * s, 1.14), 0.13, 0.12, BRZ, light=.45, cap=False)
    F.limb((-0.34, 1.05), (-0.33, 0.87), 0.085, 0.07, SKIN)
    F.limb((0.34, 1.05), (0.35, 0.87), 0.085, 0.07, SKIN)
    # skirt rings (3, flaring)
    band(F, 0.80, 0.63, 0.33, 0.38, ridges=1)
    band(F, 0.94, 0.78, 0.31, 0.35, ridges=1)
    band(F, 1.06, 0.92, 0.29, 0.32, ridges=1)
    # cuirass bell
    cu = [(-0.12, 1.47), (-0.25, 1.43), (-0.27, 1.30), (-0.28, 1.14), (-0.30, 1.04), (0.30, 1.04), (0.28, 1.14),
          (0.27, 1.30), (0.25, 1.43), (0.12, 1.47), (0, 1.455)]
    d = F.shape(cu, BRZ, light=.45, dark=.55, sw=1.5)
    for h in (1.34, 1.22, 1.12):
        F.curve([(-0.26, h), (0, h - 0.02), (0.26, h)], darken(BRZ, .5), 1.2)
        F.curve([(-0.26, h - 0.008), (0, h - 0.028), (0.26, h - 0.008)], lighten(BRZ, .35), .8, op=.7)
    F.curve([(-0.24, 1.42), (-0.26, 1.25), (-0.27, 1.10)], lighten(BRZ, .5), 2, op=.6)   # rim light
    sh.flecks(d, F.bbox(cu), 45, darken(BRZ, .75), .6, 1.6, .5)
    rivets(F, [(-0.27 + i * 0.054, 1.065) for i in range(11)], 1.5)
    # shoulder guards
    for s in (-1, 1):
        p = [(0.12 * s, 1.47), (0.24 * s, 1.49), (0.34 * s, 1.44), (0.38 * s, 1.34), (0.36 * s, 1.26),
             (0.30 * s, 1.28), (0.22 * s, 1.36), (0.14 * s, 1.40)]
        dp = F.shape(p, BRZ, light=.5, dark=.5, sw=1.4)
        F.curve([(0.16 * s, 1.44), (0.28 * s, 1.44), (0.35 * s, 1.35)], RUB, 1.8, op=.8)
        rivets(F, [(0.18 * s, 1.43), (0.26 * s, 1.45), (0.33 * s, 1.40), (0.36 * s, 1.32)], 1.5)
        sh.flecks(dp, F.bbox(p), 12, darken(BRZ, .75), .6, 1.4, .5)
    # neck guard
    ng = [(-0.13, 1.47), (0.13, 1.47), (0.12, 1.58), (0.13, 1.60), (-0.13, 1.60), (-0.12, 1.58)]
    F.shape(ng, BRZ, smooth=False, light=.5, dark=.55, sw=1.4)
    F.curve([(-0.13, 1.598), (0.13, 1.598)], RUB, 2.4)
    F.curve([(-0.12, 1.52), (0.12, 1.52)], darken(BRZ, .5), 1)
    # face
    F.ell(0, 1.665, 0.085, 0.10, SKIN, light=.3)
    for s in (-1, 1):
        F.ell(0.033 * s, 1.672, 0.013, 0.006, "#1E1A16")
        F.curve([(0.055 * s, 1.69), (0.03 * s, 1.698), (0.012 * s, 1.692)], HAIR, 2)
    F.curve([(0, 1.685), (0.008, 1.64), (-0.006, 1.632)], darken(SKIN, .45), 1.2)
    F.curve([(-0.03, 1.615), (0, 1.61), (0.03, 1.615)], darken(SKIN, .6), 1.4)
    rim = tusk_helmet(F, 0, 1.92)
    cheek(F, -0.125, rim, 1)
    cheek(F, 0.125, rim, -1)
    # hands + rapier held low
    rapier(F, (-0.33, 0.86), (-0.40, -0.02 + 0.03))
    F.ell(-0.33, 0.855, 0.042, 0.05, SKIN, light=.35)
    F.ell(0.35, 0.855, 0.042, 0.05, SKIN, light=.35)


def dendra_side(sh, cx=820):
    F = Fig(sh, cx)
    # tower shield on back (variant)
    sp = [(-0.30, 1.72), (-0.20, 1.74), (-0.17, 1.40), (-0.17, 0.60), (-0.20, 0.34), (-0.30, 0.32), (-0.33, 0.60),
          (-0.34, 1.40)]
    ds = F.shape(sp, "#B79B78", direction="h", light=.3, dark=.55, sw=1.3)
    sh.flecks(ds, F.bbox(sp), 18, "#4A3526", 3, 8, .75)
    F.curve([(-0.26, 1.72), (-0.27, 1.0), (-0.26, 0.33)], darken("#B79B78", .5), 1.2)
    # far leg
    F.limb((-0.03, 0.62), (-0.06, 0.10), 0.13, 0.08, darken(SKIN, .25))
    F.limb((-0.05, 0.40), (-0.06, 0.10), 0.12, 0.09, darken(BRZ, .25), cap=False)
    F.shape([(-0.12, 0.0), (0.08, 0.0), (0.085, 0.035), (-0.02, 0.08), (-0.12, 0.06)], darken(FELT, .2))
    # near leg
    F.limb((0.03, 0.62), (0.05, 0.10), 0.135, 0.08, SKIN)
    F.limb((0.045, 0.40), (0.05, 0.10), 0.125, 0.09, BRZ, light=.45, cap=False)
    F.curve([(-0.01, 0.40), (0.1, 0.40)], RUB, 2)
    for k in range(3):
        F.line((0.0, 0.32 - k * 0.08), (-0.02, 0.30 - k * 0.08), darken(BRZ, .6), 1.5)    # wire ties behind
    F.shape([(-0.02, 0.0), (0.18, 0.0), (0.185, 0.035), (0.08, 0.085), (-0.02, 0.065)], FELT)
    F.shape([(-0.20, 0.66), (0.22, 0.66), (0.19, 0.50), (-0.17, 0.50)], LINEN, light=.2, dark=.5)
    # rings (profile: flare front and back)
    for top, bot, wt, wb in ((0.80, 0.63, 0.21, 0.26), (0.94, 0.78, 0.20, 0.24), (1.06, 0.92, 0.19, 0.22)):
        band(F, top, bot, wt, wb, x0=0.01, ridges=1)
    # cuirass profile: chest forward
    cu = [(-0.02, 1.48), (-0.14, 1.45), (-0.17, 1.30), (-0.18, 1.14), (-0.19, 1.04), (0.21, 1.04), (0.21, 1.14),
          (0.23, 1.28), (0.20, 1.42), (0.10, 1.48)]
    d = F.shape(cu, BRZ, direction="hr", light=.45, dark=.55, sw=1.5)
    for h in (1.34, 1.22, 1.12):
        F.curve([(-0.17, h), (0.02, h - 0.012), (0.21, h)], darken(BRZ, .5), 1.2)
        F.curve([(-0.17, h - .008), (0.02, h - 0.02), (0.21, h - .008)], lighten(BRZ, .35), .8, op=.7)
    F.line((0.01, 1.46), (0.01, 1.06), darken(BRZ, .6), 1.2)            # side lacing seam
    for k in range(8):
        F.line((0.0, 1.43 - k * 0.05), (0.02, 1.41 - k * 0.05), FELT, 1.6)
    sh.flecks(d, F.bbox(cu), 40, darken(BRZ, .75), .6, 1.6, .5)
    rivets(F, [(-0.17 + i * 0.05, 1.065) for i in range(8)], 1.5)
    # neck guard
    F.shape([(-0.11, 1.46), (0.13, 1.46), (0.13, 1.60), (-0.11, 1.60)], BRZ, smooth=False, light=.5, dark=.55, sw=1.4)
    F.curve([(-0.11, 1.598), (0.13, 1.598)], RUB, 2.4)
    # head
    F.ell(0.02, 1.665, 0.10, 0.10, SKIN, light=.3)
    F.shape([(0.118, 1.68), (0.14, 1.645), (0.118, 1.635)], SKIN, smooth=False)
    F.ell(0.08, 1.675, 0.011, 0.006, "#1E1A16")
    F.curve([(0.06, 1.694), (0.09, 1.70), (0.11, 1.694)], HAIR, 2)
    rim = tusk_helmet(F, 0.02, 1.92, side=True)
    cheek(F, 0.06, rim, 1)
    # near arm + pauldron
    F.limb((0.02, 1.32), (0.08, 1.06), 0.12, 0.10, LINEN)
    F.limb((0.08, 1.06), (0.14, 0.90), 0.09, 0.075, SKIN)
    p = [(-0.12, 1.47), (0.0, 1.50), (0.14, 1.46), (0.16, 1.36), (0.11, 1.26), (0.0, 1.24), (-0.10, 1.28), (-0.14, 1.38)]
    dp = F.shape(p, BRZ, light=.5, dark=.5, sw=1.4)
    F.curve([(-0.10, 1.45), (0.02, 1.48), (0.13, 1.44)], RUB, 1.8, op=.8)
    rivets(F, [(-0.10 + i * 0.05, 1.29) for i in range(5)], 1.5)
    sh.flecks(dp, F.bbox(p), 12, darken(BRZ, .75), .6, 1.4, .5)
    rapier(F, (0.15, 0.88), (0.56, 0.08))
    F.ell(0.15, 0.885, 0.045, 0.05, SKIN, light=.35)


def build():
    sh = Sheet("enemy", "THE BRONZE AGE · ENEMY · HEAVY", "Dendra Champion", "H 1.92 m · ≤ 12k tris · 2048²",
               "1 m = 220 px · ground y 690",
               [("hammered bronze", BRZ), ("boar's tusk", TUSK), ("felt + leather", FELT), ("linen", LINEN),
                ("bone", BONE), ("horsehair", HAIR)], seed=31)
    enemy_furniture(sh)
    height_mark(sh, 1.92, x1=880, label="1.92 m (helmet knob)")
    for cx in (470, 820):
        sh.ellipse(cx + 10, 690, 100, 8, "#0E0C09", op=.6)
    dendra_front(sh)
    dendra_side(sh)
    sh.callouts([
        (820 + 0.02 * S, G - 1.84 * S, "BOAR'S-TUSK HELMET", "4 rows, alternating slant"),
        (820 + 0.09 * S, G - 1.62 * S, "CHEEK-PIECE", "tusk-plated, hinged"),
        (820 + 0.12 * S, G - 1.53 * S, "NECK GUARD", "bronze collar 0.14 m"),
        (820 + 0.14 * S, G - 1.40 * S, "SHOULDER GUARD", "riveted dome 0.30 m"),
        (820 + 0.22 * S, G - 1.24 * S, "BELL CUIRASS", "front + back, side-laced"),
        (820 + 0.24 * S, G - 0.70 * S, "SKIRT RINGS ×3", "laced cones, swing as a bell"),
        (820 + 0.42 * S, G - 0.35 * S, "RAPIER", "0.92 m bronze, horned guard"),
        (820 + 0.07 * S, G - 0.22 * S, "GREAVES", "0.32 m, wire-tied behind")], 968, 165, 640)
    sh.callout(820 - 0.33 * S, G - 1.1 * S, 700, 330, "TOWER SHIELD", "variant · hung on back", anchor="end",
               elbow=False)
    return sh
