from lib import *

BLK = "#23282E"; EDGE = "#7E8A94"; STEEL = "#34404E"; BOOT = "#5E4430"; LEATH = "#221C18"; WAL = "#3B2A1E"; MUR = "#5B3036"
RUST = dk(BOOT, 0.2)


def plate(sh, pts, direction="h", smooth=True, edge=True, light=0.3):
    d = sh.shape(pts, BLK, smooth=smooth, direction=direction, light=light, dark=0.45)
    if edge:
        sh.path(d, "none", EDGE, 1.1, op=0.55)
    return d


def lame_lines(sh, F, x0, x1, y0, y1, n, curve=0.02, flip=1, bright_first=True):
    for k in range(1, n):
        y = y0 + (y1 - y0) * k / n
        sh.line(F.pts([(x0, y), ((x0 + x1) / 2, y - curve * flip), (x1, y)]), "#0A0B0C", 1.6, op=0.9)
        sh.line(F.pts([(x0, y + 0.006), ((x0 + x1) / 2, y - curve * flip + 0.006), (x1, y + 0.006)]), EDGE, 0.9, op=0.55)


def pistol_holster(sh, F, s, x_top=0.26, shade=0.0, facing=1):
    """Holster hung at hip, butt forward (front view: butt out to the side)."""
    hol = F.pts([(s * (x_top - 0.04), 1.00), (s * (x_top + 0.05), 1.00), (s * (x_top + 0.09), 0.64), (s * (x_top + 0.03), 0.62)])
    sh.shape(hol, dk(LEATH, shade), smooth=False, direction="h", light=0.25)
    sh.line(F.pts([(s * (x_top - 0.03), 0.97), (s * (x_top + 0.05), 0.97)]), dk(EDGE, 0.3), 1.2, smooth=False)
    # stock + ball pommel jutting
    sh.shape(limb(F.p(s * (x_top + 0.0), 1.00), 0.04 * F.s, F.p(s * (x_top + 0.05), 1.10), 0.035 * F.s), dk(WAL, shade), direction="h")
    cx, cy = F.p(s * (x_top + 0.06), 1.12)
    sh.circle(cx, cy, 0.035 * F.s, f"url(#{sh.form(dk(WAL, shade), 'sphere')})", INK, 1)
    sh.circle(cx, cy - 2, 0.02 * F.s, f"url(#{sh.form(STEEL, 'sphere')})", INK, 0.6)
    sh.circle(cx - 2, cy - 4, 1.4, EDGE, op=0.8)


def helmet_front(sh, F):
    # neck lames behind (visible at sides)
    for k in range(3):
        y = 1.60 - k * 0.035
        sh.shape(F.pts([(-0.13 - k * 0.012, y), (0.13 + k * 0.012, y), (0.14 + k * 0.012, y - 0.035), (-0.14 - k * 0.012, y - 0.035)]), BLK, smooth=False, direction="h", sw=0.9)
    skull = F.pts([(-0.12, 1.62), (-0.125, 1.76), (-0.10, 1.85), (-0.05, 1.895), (0, 1.905), (0.05, 1.895), (0.10, 1.85), (0.125, 1.76), (0.12, 1.62)])
    plate(sh, skull, light=0.35)
    comb = F.pts([(-0.013, 1.89), (-0.012, 1.95), (0.012, 1.95), (0.013, 1.89)])
    plate(sh, comb, smooth=False, light=0.4)
    sh.line([F.p(0, 1.895), F.p(0, 1.948)], EDGE, 1.0, smooth=False)
    # falling buff (lower face)
    buff = F.pts([(-0.11, 1.70), (-0.10, 1.63), (-0.06, 1.585), (0, 1.575), (0.06, 1.585), (0.10, 1.63), (0.11, 1.70)])
    plate(sh, buff, light=0.3)
    for k in range(3):
        sh.line(F.pts([(-0.09, 1.66 - k * 0.022), (0, 1.652 - k * 0.022), (0.09, 1.66 - k * 0.022)]), "#0A0B0C", 1.2, op=0.8)
    for j in range(5):  # breaths
        sh.rect(*F.p(0.03 + j * 0.012, 1.655), 3, 7, "#0A0B0C")
    # peaked visor
    visor = F.pts([(-0.118, 1.76), (-0.11, 1.70), (0, 1.69), (0.11, 1.70), (0.118, 1.76), (0.06, 1.795), (0, 1.80), (-0.06, 1.795)])
    plate(sh, visor, light=0.4)
    for k in range(3):
        sh.line([F.p(-0.085, 1.735 + k * 0.018), F.p(0.085, 1.735 + k * 0.018)], "#050506", 2.2, smooth=False)
    sh.line(F.pts([(-0.11, 1.70), (0, 1.69), (0.11, 1.70)]), EDGE, 1.2, op=0.8)
    sh.rivets([F.p(x, 1.63) for x in (-0.10, -0.05, 0.05, 0.10)] + [F.p(s * 0.118, 1.78) for s in (-1, 1)], 1.8, EDGE)
    sh.line(F.pts([(-0.10, 1.85), (-0.06, 1.88)]), EDGE, 1.3, op=0.5)


def front(sh):
    F = Fig(470)
    sh.ellipse(470, 690, 110, 8, "#000000", op=0.5)
    # scabbard behind (left hip, viewer right)
    sh.shape(limb(F.p(0.22, 1.02), 0.045 * F.s, F.p(0.36, 0.25), 0.035 * F.s), LEATH, direction="h")
    sh.shape(limb(F.p(0.35, 0.30), 0.04 * F.s, F.p(0.365, 0.24), 0.03 * F.s), BLK)
    # boots
    for s in (-1, 1):
        boot = F.pts([(s * 0.04, 0.58), (s * 0.035, 0.30), (s * 0.045, 0.12), (s * 0.05, 0.0), (s * 0.17, 0.0), (s * 0.165, 0.10), (s * 0.155, 0.30), (s * 0.165, 0.58)])
        sh.shape(boot, BOOT, direction="h", light=0.3)
        for k in range(4):
            sh.line(F.pts([(s * 0.05, 0.10 + k * 0.04), (s * 0.10, 0.09 + k * 0.04), (s * 0.155, 0.105 + k * 0.04)]), dk(BOOT, 0.4), 1.1, op=0.65)
        sh.flat(F.pts([(s * 0.045, 0.0), (s * 0.17, 0.0), (s * 0.168, 0.08), (s * 0.05, 0.08)]), dk(BOOT, 0.35), op=0.6)  # mud
        sh.flecks(*F.p(min(s * 0.05, s * 0.17), 0.1), *F.p(max(s * 0.05, s * 0.17), 0.0), 8, dk(BOOT, 0.5), 0.8, 1.8)
        sh.shape(F.pts([(s * 0.035, 0.07), (s * 0.18, 0.07), (s * 0.20, 0.05), (s * 0.03, 0.05)]), LEATH, smooth=False)  # spur leather
        sh.shape(F.pts([(s * 0.155, 0.075), (s * 0.20, 0.10), (s * 0.21, 0.06), (s * 0.18, 0.05)]), LEATH, sw=0.8)
        # turned-down cup
        cup = F.pts([(s * 0.0, 0.70), (s * 0.03, 0.56), (s * 0.10, 0.54), (s * 0.18, 0.56), (s * 0.22, 0.70), (s * 0.11, 0.67)])
        sh.shape(cup, lit(BOOT, 0.06), direction="h", light=0.35)
        sh.line(F.pts([(s * 0.01, 0.685), (s * 0.11, 0.66), (s * 0.21, 0.69)]), lit(BOOT, 0.4), 1.1, op=0.6)
    # tassets + poleyns
    for s in (-1, 1):
        tas = F.pts([(s * 0.015, 1.02), (s * 0.27, 1.02), (s * 0.24, 0.62), (s * 0.075, 0.62)])
        plate(sh, tas, smooth=False)
        n = 12
        for k in range(1, n):
            t = k / n
            y = 1.02 - 0.40 * t
            xa, xb = s * (0.015 + 0.06 * t), s * (0.27 - 0.03 * t)
            sh.line([F.p(xa / F.s * F.s, y), F.p((xa + xb) / 2, y - 0.008), F.p(xb, y)], "#0A0B0C", 1.5, op=0.85)
            sh.line([F.p(xa, y + 0.005), F.p((xa + xb) / 2, y - 0.003), F.p(xb, y + 0.005)], EDGE, 0.8, op=0.45)
        sh.rivets([F.p(s * 0.04, 1.02 - 0.0333 * k) for k in range(1, 12, 2)], 1.3, EDGE)
        pol = F.pts([(s * 0.06, 0.64), (s * 0.20, 0.64), (s * 0.21, 0.56), (s * 0.13, 0.52), (s * 0.05, 0.56)])
        plate(sh, pol, light=0.45)
        sh.shape(F.pts([(s * 0.19, 0.61), (s * 0.26, 0.64), (s * 0.25, 0.54), (s * 0.20, 0.56)]), BLK, direction="h", sw=0.9)  # fan
        sh.circle(*F.p(s * 0.13, 0.585), 2.2, EDGE, op=0.7)
    # fauld
    plate(sh, F.pts([(-0.22, 1.08), (0.22, 1.08), (0.24, 1.01), (-0.24, 1.01)]), smooth=False)
    sh.line([F.p(-0.23, 1.045), F.p(0.23, 1.045)], "#0A0B0C", 1.5, smooth=False)
    # breastplate
    breast = F.pts([(0, 1.03), (0.12, 1.06), (0.20, 1.08), (0.215, 1.25), (0.20, 1.42), (0.14, 1.49), (0.06, 1.47), (0, 1.48),
                    (-0.06, 1.47), (-0.14, 1.49), (-0.20, 1.42), (-0.215, 1.25), (-0.20, 1.08), (-0.12, 1.06)])
    d = plate(sh, breast, light=0.3)
    sh.clip_open(d)
    sh.flat(F.pts([(-0.19, 1.12), (-0.06, 1.14), (-0.04, 1.40), (-0.15, 1.43)]), EDGE, op=0.14)  # broad sheen
    sh.flat(F.pts([(0.06, 1.05), (0.22, 1.08), (0.22, 1.45), (0.1, 1.45)]), "#000000", op=0.3)
    sh.flecks(*F.p(-0.2, 1.45), *F.p(0.2, 1.08), 16, RUST, 0.8, 2.2, (0.3, 0.6))
    sh.close()
    sh.line(F.pts([(0, 1.47), (0, 1.25), (0, 1.04)]), EDGE, 1.4, op=0.85)  # medial ridge
    sh.circle(*F.p(-0.07, 1.30), 4.5, "none", EDGE, 0.8, op=0.6)  # proof mark
    sh.circle(*F.p(-0.07, 1.30), 2.5, "#000000", op=0.35)
    sh.line(F.pts([(-0.14, 1.48), (-0.06, 1.46), (0, 1.47), (0.06, 1.46), (0.14, 1.48)]), EDGE, 2.0, op=0.8)  # roped edge
    # sash at waist, bow at right hip (viewer left)
    sh.shape(F.pts([(-0.22, 1.10), (0.22, 1.10), (0.225, 1.04), (-0.225, 1.04)]), MUR, smooth=False, direction="h")
    for k in range(5):
        sh.line([F.p(-0.2 + k * 0.1, 1.098), F.p(-0.18 + k * 0.1, 1.045)], dk(MUR, 0.4), 1.0, op=0.6, smooth=False)
    sh.shape(F.pts([(-0.20, 1.07), (-0.29, 1.12), (-0.30, 1.03), (-0.20, 1.05)]), MUR, direction="h", light=0.35)
    sh.shape(F.pts([(-0.20, 1.06), (-0.26, 1.01), (-0.30, 0.74), (-0.25, 0.72), (-0.22, 0.95)]), dk(MUR, 0.1), direction="h")
    sh.shape(F.pts([(-0.19, 1.06), (-0.20, 0.95), (-0.19, 0.78), (-0.15, 0.78), (-0.16, 1.0)]), dk(MUR, 0.2), direction="h")
    # holsters + pistols both hips
    pistol_holster(sh, F, -1)
    pistol_holster(sh, F, 1)
    # sword hilt left hip
    sh.shape(F.pts([(0.13, 1.14), (0.21, 1.17), (0.23, 1.10), (0.16, 1.07)]), BLK, direction="h")
    sh.line([F.p(0.17, 1.12), F.p(0.15, 1.19)], LEATH, 4, smooth=False)
    sh.circle(*F.p(0.145, 1.20), 4, f"url(#{sh.form(BLK, 'sphere')})", INK, 0.8)
    # arms: rerebrace, couter, vambrace, gauntlet
    def arm(s):
        plate(sh, limb(F.p(s * 0.25, 1.40), 0.10 * F.s, F.p(s * 0.29, 1.17), 0.09 * F.s))
        plate(sh, limb(F.p(s * 0.29, 1.15), 0.085 * F.s, F.p(s * 0.30, 0.97), 0.075 * F.s))
        sh.shape(F.pts([(s * 0.25, 1.19), (s * 0.34, 1.20), (s * 0.35, 1.12), (s * 0.29, 1.10), (s * 0.24, 1.14)]), BLK, direction="h", light=0.45)
        sh.shape(F.pts([(s * 0.33, 1.20), (s * 0.39, 1.19), (s * 0.37, 1.10), (s * 0.34, 1.12)]), BLK, direction="h", sw=0.9)
        cuff = F.pts([(s * 0.25, 0.99), (s * 0.35, 0.99), (s * 0.345, 0.91), (s * 0.255, 0.91)])
        plate(sh, cuff, smooth=False, light=0.4)
        hand = F.pts([(s * 0.26, 0.915), (s * 0.34, 0.915), (s * 0.345, 0.83), (s * 0.31, 0.80), (s * 0.265, 0.83)])
        plate(sh, hand, light=0.3)
        for k in range(3):
            sh.line([F.p(s * 0.27, 0.885 - k * 0.022), F.p(s * 0.335, 0.885 - k * 0.022)], EDGE, 0.8, op=0.5, smooth=False)
    arm(-1)
    arm(1)
    sh.flecks(*F.p(-0.35, 0.92), *F.p(-0.26, 0.82), 10, "#1E1C1A", 0.8, 2.0, (0.4, 0.8))  # powder burn
    # pauldrons (6 lames)
    for s in (-1, 1):
        for k in range(6):
            y = 1.52 - k * 0.055
            xi = s * (0.13 + 0.02 * k)
            xo = s * (0.33 + 0.004 * k)
            lame = F.pts([(xi, y), (s * 0.24, y + 0.02), (xo, y - 0.02), (xo + s * 0.01, y - 0.075), (s * 0.25, y - 0.06), (xi + s * 0.02, y - 0.065)])
            d = plate(sh, lame, light=0.35 - k * 0.03)
            sh.line([F.p(xi + s * 0.01, y - 0.002), F.p(s * 0.24, y + 0.017), F.p(xo, y - 0.022)], EDGE, 1.1, op=0.8)
            sh.circle(*F.p(s * 0.28, y - 0.03), 1.4, EDGE, op=0.7)
    # gorget
    gor = F.pts([(-0.17, 1.50), (-0.12, 1.56), (-0.06, 1.585), (0.06, 1.585), (0.12, 1.56), (0.17, 1.50), (0.08, 1.475), (-0.08, 1.475)])
    plate(sh, gor, light=0.4)
    sh.line(F.pts([(-0.16, 1.51), (0, 1.49), (0.16, 1.51)]), EDGE, 1.2, op=0.7)
    helmet_front(sh, F)


def side(sh):
    S = Fig(820)
    sh.ellipse(820, 690, 90, 7, "#000000", op=0.5)
    sh.shape(limb(S.p(-0.02, 1.03), 0.045 * S.s, S.p(-0.46, 0.45), 0.035 * S.s), LEATH, direction="h")
    sh.shape(limb(S.p(-0.43, 0.49), 0.042 * S.s, S.p(-0.47, 0.44), 0.035 * S.s), BLK)
    for off, shade in ((-0.05, 0.35), (0.05, 0.0)):
        boot = S.pts([(-0.08 + off, 0.60), (-0.09 + off, 0.30), (-0.06 + off, 0.10), (-0.07 + off, 0.0), (0.18 + off, 0.0), (0.18 + off, 0.06), (0.07 + off, 0.09), (0.06 + off, 0.30), (0.075 + off, 0.60)])
        sh.shape(boot, dk(BOOT, shade), direction="r", light=0.3)
        sh.flat(S.pts([(-0.07 + off, 0.0), (0.18 + off, 0.0), (0.17 + off, 0.07), (-0.065 + off, 0.08)]), dk(BOOT, 0.35), op=0.6)
        for k in range(4):
            sh.line(S.pts([(-0.07 + off, 0.12 + k * 0.04), (0.0 + off, 0.11 + k * 0.04), (0.06 + off, 0.125 + k * 0.04)]), dk(BOOT, 0.45), 1.0, op=0.6)
        sh.shape(S.pts([(-0.12 + off, 0.71), (-0.10 + off, 0.56), (0.08 + off, 0.55), (0.12 + off, 0.71), (0.0 + off, 0.68)]), dk(lit(BOOT, 0.06), shade), direction="r", light=0.35)
        sh.shape(S.pts([(-0.08 + off, 0.075), (-0.14 + off, 0.09), (-0.13 + off, 0.05), (-0.07 + off, 0.045)]), LEATH, sw=0.8)
    # tasset profile (near), front and back
    tas = S.pts([(-0.14, 1.02), (0.17, 1.02), (0.16, 0.62), (-0.02, 0.62)])
    plate(sh, tas, smooth=False, direction="r")
    for k in range(1, 12):
        t = k / 12
        y = 1.02 - 0.40 * t
        xa, xb = -0.14 + 0.12 * t, 0.17 - 0.01 * t
        sh.line([S.p(xa, y), S.p((xa + xb) / 2, y - 0.008), S.p(xb, y)], "#0A0B0C", 1.5, op=0.85)
        sh.line([S.p(xa, y + 0.005), S.p((xa + xb) / 2, y - 0.003), S.p(xb, y + 0.005)], EDGE, 0.8, op=0.45)
    pol = S.pts([(0.0, 0.64), (0.15, 0.64), (0.17, 0.57), (0.10, 0.52), (0.01, 0.55)])
    plate(sh, pol, direction="r", light=0.45)
    # torso: back-plate + breastplate profile with peascod
    torso = S.pts([(0.17, 1.03), (0.19, 1.12), (0.185, 1.30), (0.15, 1.44), (0.09, 1.52), (-0.08, 1.52), (-0.15, 1.42), (-0.155, 1.20), (-0.14, 1.03)])
    d = plate(sh, torso, direction="r", light=0.35)
    sh.clip_open(d)
    sh.flat(S.pts([(-0.16, 1.03), (-0.02, 1.03), (-0.04, 1.52), (-0.16, 1.52)]), "#000000", op=0.3)
    sh.flat(S.pts([(0.12, 1.08), (0.19, 1.12), (0.18, 1.35), (0.12, 1.40)]), EDGE, op=0.14)
    sh.flecks(*S.p(-0.15, 1.5), *S.p(0.18, 1.05), 14, RUST, 0.8, 2.2, (0.3, 0.6))
    sh.close()
    sh.line(S.pts([(0.01, 1.52), (0.0, 1.30), (0.01, 1.03)]), "#0A0B0C", 1.6, op=0.9)  # breast/back join
    sh.rivets([S.p(0.005, y) for y in (1.46, 1.36, 1.26, 1.16, 1.08)], 1.6, EDGE)
    sh.line(S.pts([(0.17, 1.04), (0.19, 1.14), (0.185, 1.30), (0.15, 1.44)]), EDGE, 1.3, op=0.85)
    plate(sh, S.pts([(-0.15, 1.08), (0.19, 1.08), (0.20, 1.01), (-0.15, 1.01)]), smooth=False, direction="r")
    sh.shape(S.pts([(-0.155, 1.10), (0.195, 1.10), (0.20, 1.04), (-0.155, 1.04)]), MUR, smooth=False, direction="r")
    # holster on near hip, butt forward
    sh.shape(S.pts([(0.02, 1.00), (0.12, 1.00), (0.02, 0.62), (-0.04, 0.64)]), LEATH, smooth=False, direction="r", light=0.25)
    sh.shape(limb(S.p(0.08, 1.00), 0.04 * S.s, S.p(0.18, 1.06), 0.035 * S.s), WAL, direction="r")
    cx, cy = S.p(0.205, 1.07)
    sh.circle(cx, cy, 0.035 * S.s, f"url(#{sh.form(WAL, 'sphere')})", INK, 1)
    sh.circle(cx + 2, cy - 2, 0.02 * S.s, f"url(#{sh.form(STEEL, 'sphere')})", INK, 0.6)
    sh.circle(*S.p(0.06, 0.985), 0.02 * S.s, STEEL, INK, 0.8)  # wheel
    sh.circle(*S.p(0.06, 0.985), 2, EDGE)
    # sword hilt
    sh.line([S.p(0.04, 1.10), S.p(0.16, 1.20)], LEATH, 4, smooth=False)
    sh.shape(S.pts([(0.0, 1.12), (0.07, 1.16), (0.08, 1.08), (0.02, 1.06)]), BLK, direction="r")
    sh.circle(*S.p(0.17, 1.21), 4, f"url(#{sh.form(BLK, 'sphere')})", INK, 0.8)
    # near arm hanging, gauntlet on pistol butt
    plate(sh, limb(S.p(0.0, 1.40), 0.10 * S.s, S.p(0.02, 1.17), 0.09 * S.s), direction="r")
    plate(sh, limb(S.p(0.03, 1.15), 0.085 * S.s, S.p(0.13, 1.02), 0.075 * S.s), direction="r")
    sh.shape(S.pts([(-0.04, 1.20), (0.05, 1.21), (0.07, 1.13), (0.0, 1.10), (-0.05, 1.14)]), BLK, direction="r", light=0.45)
    sh.shape(S.pts([(-0.05, 1.22), (-0.09, 1.16), (-0.03, 1.12)]), BLK, direction="r", sw=0.9)
    plate(sh, S.pts([(0.10, 1.07), (0.18, 1.04), (0.16, 0.97), (0.09, 0.99)]), smooth=False, direction="r", light=0.4)
    plate(sh, S.pts([(0.15, 1.05), (0.22, 1.03), (0.235, 0.98), (0.20, 0.95), (0.155, 0.98)]), direction="r")
    # pauldron profile
    for k in range(6):
        y = 1.53 - k * 0.055
        lame = S.pts([(-0.14 + k * 0.01, y - 0.01), (0.0, y + 0.025), (0.14 - k * 0.01, y - 0.01), (0.13 - k * 0.01, y - 0.07), (0.0, y - 0.05), (-0.13 + k * 0.01, y - 0.07)])
        plate(sh, lame, direction="r", light=0.35 - k * 0.03)
        sh.line(S.pts([(-0.13 + k * 0.01, y - 0.012), (0.0, y + 0.022), (0.13 - k * 0.01, y - 0.012)]), EDGE, 1.1, op=0.8)
        sh.circle(*S.p(0.0, y - 0.02), 1.5, EDGE, op=0.7)
    # gorget
    plate(sh, S.pts([(-0.10, 1.50), (-0.07, 1.585), (0.07, 1.585), (0.13, 1.50), (0.0, 1.475)]), direction="r", light=0.4)
    # helmet profile: neck lames, skull, comb, peaked visor, buff
    for k in range(3):
        y = 1.64 - k * 0.035
        plate(sh, S.pts([(-0.11 - k * 0.015, y), (-0.02, y), (-0.02, y - 0.04), (-0.13 - k * 0.018, y - 0.045)]), smooth=False, direction="r")
    skull = S.pts([(-0.12, 1.64), (-0.13, 1.76), (-0.10, 1.86), (-0.03, 1.905), (0.05, 1.90), (0.11, 1.85), (0.135, 1.76), (0.13, 1.66), (0.0, 1.62)])
    plate(sh, skull, direction="r", light=0.4)
    comb = S.pts([(-0.12, 1.80), (-0.08, 1.90), (0.0, 1.95), (0.07, 1.935), (0.12, 1.86), (0.10, 1.855), (0.05, 1.90), (-0.02, 1.905), (-0.09, 1.86)])
    plate(sh, comb, direction="r", light=0.45)
    for k in range(10):
        x = -0.09 + k * 0.02
        y = 1.90 + 0.045 * math.cos(x * 12) - 0.01
        sh.line([S.p(x, y - 0.012), S.p(x + 0.01, y + 0.002)], "#0A0B0C", 0.8, smooth=False)
    visor = S.pts([(0.04, 1.80), (0.12, 1.79), (0.19, 1.745), (0.165, 1.715), (0.13, 1.70), (0.05, 1.70)])
    plate(sh, visor, direction="r", light=0.45)
    for k in range(3):
        sh.line([S.p(0.06, 1.72 + k * 0.018), S.p(0.14, 1.715 + k * 0.018)], "#050506", 2.0, smooth=False)
    buff = S.pts([(0.03, 1.70), (0.14, 1.70), (0.15, 1.64), (0.12, 1.59), (0.04, 1.58)])
    plate(sh, buff, direction="r", light=0.35)
    for j in range(4):
        sh.rect(*S.p(0.10 + j * 0.01, 1.66), 2.5, 7, "#0A0B0C")
    sh.circle(*S.p(0.02, 1.72), 4, BLK, EDGE, 1.2)  # visor pivot
    sh.rivets([S.p(x, 1.655) for x in (-0.08, -0.04)], 1.7, EDGE)
    sh.line(S.pts([(0.05, 1.805), (0.12, 1.795), (0.19, 1.745)]), EDGE, 1.3, op=0.9)


def build():
    sh = Sheet(seed=13)
    front(sh)
    side(sh)
    height_mark(sh, 580, 1.95, "1.95 m (comb)", 206)
    callouts(sh, [
        (820 + 0.0 * 220, 690 - 220 * 1.94, "CLOSED BURGONET", "roped comb · 3-slit peaked visor"),
        (820 + 0.13 * 220, 690 - 220 * 1.65, "FALLING BUFF", "breaths grille · temple pivot"),
        (820 + 0.12 * 220, 690 - 220 * 1.40, "PAULDRON, 6 LAMES", "blackened · bright edges"),
        (820 + 0.18 * 220, 690 - 220 * 1.25, "PEASCOD CUIRASS", "proof dent · rolled edges"),
        (820 + 0.19 * 220, 690 - 220 * 1.07, "WHEELLOCK IN HOLSTER", "walnut ball pommel · butt fwd"),
        (820 + 0.15 * 220, 690 - 220 * 0.85, "TASSETS, 12 LAMES", "waist to knee · stiff bone chain"),
        (820 + 0.14 * 220, 690 - 220 * 0.58, "POLEYN + FAN", "over boot cup"),
        (820 + 0.1 * 220, 690 - 220 * 0.25, "RIDING BOOTS", "soft brown · mud to 0.10 m"),
        (820 - 0.10 * 220, 690 - 220 * 1.07, "FIELD SASH", "murrey · only colour on him"),
        (820 - 0.44 * 220, 690 - 220 * 0.47, "PALLASCH", "0.95 m blade · black scabbard", 725, 600, "end"),
    ], y0=150, dy=55)
    return sh.render("THE AGE OF POWDER · ENEMY · HEAVY", "Cuirassier", "H 1.95 m · ≤ 12k tris · 2048²",
                     "1 m = 220 px · ground y 690", [(BLK, "blackened steel"), (STEEL, "blued steel"),
                                                    (BOOT, "boot leather"), (LEATH, "black leather"), (WAL, "walnut"), (MUR, "murrey sash")],
                     view_labels=[(470, "FRONT"), (820, "SIDE")])
