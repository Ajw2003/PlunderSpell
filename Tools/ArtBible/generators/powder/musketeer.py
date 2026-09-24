from lib import *

BUFF = "#A48A60"; MUR = "#5B3036"; FELT = "#221E1A"; WAL = "#3B2A1E"; STEEL = "#34404E"; SKIN = "#B88E6E"
EMBER = "#C4542E"; BRIGHT = "#9AA3A8"; STEEL_HI = "#6E7E92"; POWDER = dk(FELT, 0.2)
HAIR = dk(SKIN, 0.7); BOOT = dk(BUFF, 0.45)


def ember(sh, x, y):
    sh.glow(x, y, 26, EMBER, 0.45)
    sh.circle(x, y, 2.6, EMBER)
    sh.circle(x, y, 1.1, lit(EMBER, 0.7))
    sh.line([(x, y - 3), (x - 4, y - 14), (x + 2, y - 26), (x - 3, y - 40)], "#8A857A", 1.2, op=0.35)


def musket_upright(sh, F, x):
    # butt / stock
    stock = F.pts([(x - 0.055, 0.0), (x + 0.055, 0.0), (x + 0.04, 0.12), (x + 0.028, 0.36), (x + 0.03, 0.62),
                   (x + 0.024, 1.25), (x - 0.024, 1.25), (x - 0.03, 0.62), (x - 0.028, 0.36), (x - 0.04, 0.12)])
    sh.shape(stock, WAL, smooth=False, direction="h", light=0.35)
    sh.flat(F.pts([(x - 0.055, 0.0), (x + 0.055, 0.0), (x + 0.05, 0.03), (x - 0.05, 0.03)]), dk(WAL, 0.4), smooth=False)
    sh.line([F.p(x - 0.02, 0.1), F.p(x - 0.015, 0.34)], lit(WAL, 0.3), 1.0, op=0.5, smooth=False)
    # lockplate + serpentine
    sh.shape(F.pts([(x + 0.02, 0.44), (x + 0.045, 0.44), (x + 0.045, 0.62), (x + 0.02, 0.62)]), STEEL, smooth=False)
    sh.line(F.pts([(x + 0.045, 0.52), (x + 0.075, 0.55), (x + 0.07, 0.60), (x + 0.085, 0.64)]), STEEL_HI, 2.2)
    sh.flecks(*F.p(x - 0.02, 0.66), *F.p(x + 0.05, 0.44), 10, POWDER, 0.6, 1.6)
    # barrel
    sh.shape(F.pts([(x - 0.014, 0.6), (x - 0.013, 1.55), (x + 0.013, 1.55), (x + 0.014, 0.6)]), STEEL, smooth=False, direction="h", light=0.45)
    sh.line([F.p(x - 0.005, 0.62), F.p(x - 0.005, 1.53)], BRIGHT, 0.8, op=0.6, smooth=False)
    sh.rect(F.p(x - 0.014, 0)[0], F.p(0, 1.55)[1], 0.028 * F.s, 0.10 * F.s, POWDER, op=0.6)
    for yy in (0.8, 1.05, 1.2):
        sh.rect(F.p(x - 0.027, 0)[0], F.p(0, yy + 0.01)[1], 0.054 * F.s, 0.012 * F.s, STEEL, INK, 0.6)
    sh.line([F.p(x + 0.02, 0.62), F.p(x + 0.02, 1.47)], dk(STEEL, 0.3), 1.6, smooth=False)  # ramrod


def charger(sh, x, y, s=1.0, shade=0.0):
    w, h = 0.035 * SCALE * s, 0.10 * SCALE * s
    sh.line([(x, y), (x, y + 6)], dk(FELT, 0.2), 0.9, smooth=False)
    sh.shape([(x - w / 2, y + 6), (x + w / 2, y + 6), (x + w / 2 * 0.9, y + 6 + h), (x - w / 2 * 0.9, y + 6 + h)], dk(WAL, shade), smooth=False, direction="h", light=0.4, sw=0.8)
    sh.shape([(x - w / 2 - 0.5, y + 4), (x + w / 2 + 0.5, y + 4), (x + w / 2, y + 10), (x - w / 2, y + 10)], FELT, smooth=False, sw=0.7)
    sh.line([(x - w / 2 + 1, y + 6 + h * 0.5), (x + w / 2 - 1, y + 6 + h * 0.5)], dk(WAL, 0.5), 0.8, smooth=False)


def front(sh):
    F = Fig(470)
    sh.ellipse(470, 690, 100, 7, "#000000", op=0.45)
    # rest (viewer's right) — staff
    x = 0.37
    sh.shape(F.pts([(x - 0.012, 0.02), (x - 0.013, 1.25), (x + 0.013, 1.25), (x + 0.012, 0.02)]), dk(BUFF, 0.35), smooth=False, direction="h")
    sh.shape(F.pts([(x - 0.008, 0.0), (x + 0.008, 0.0), (x + 0.012, 0.06), (x - 0.012, 0.06)]), STEEL, smooth=False)
    fork = F.pts([(x - 0.05, 1.33), (x - 0.045, 1.26), (x, 1.235), (x + 0.045, 1.26), (x + 0.05, 1.33), (x + 0.036, 1.33), (x + 0.03, 1.27), (x, 1.255), (x - 0.03, 1.27), (x - 0.036, 1.33)])
    sh.shape(fork, STEEL, direction="h", light=0.4)
    musket_upright(sh, F, -0.36)
    # legs: breeches + bucket boots
    for s in (-1, 1):
        sh.shape(F.pts([(s * 0.02, 0.76), (s * 0.02, 0.55), (s * 0.17, 0.55), (s * 0.19, 0.76)]), MUR, smooth=False, direction="h")
        boot = F.pts([(s * 0.035, 0.52), (s * 0.04, 0.12), (s * 0.05, 0.06), (s * 0.05, 0.0), (s * 0.15, 0.0), (s * 0.155, 0.07), (s * 0.145, 0.14), (s * 0.15, 0.52)])
        sh.shape(boot, BOOT, direction="h", light=0.3)
        for k in range(3):
            sh.line(F.pts([(s * 0.05, 0.16 + k * 0.035), (s * 0.095, 0.15 + k * 0.035), (s * 0.14, 0.165 + k * 0.035)]), dk(BOOT, 0.4), 1.0, op=0.6)
        top = F.pts([(s * 0.0, 0.62), (s * 0.03, 0.50), (s * 0.095, 0.48), (s * 0.17, 0.50), (s * 0.205, 0.62), (s * 0.10, 0.60)])
        sh.shape(top, lit(BOOT, 0.08), direction="h", light=0.35)
        sh.line(F.pts([(s * 0.01, 0.605), (s * 0.10, 0.59), (s * 0.20, 0.61)]), lit(BOOT, 0.4), 1.0, op=0.6)
        sh.shape(F.pts([(s * 0.04, 0.06), (s * 0.16, 0.06), (s * 0.18, 0.04), (s * 0.03, 0.04)]), dk(BOOT, 0.4), smooth=False)  # spur leather
    # buff coat body with skirts
    coat = F.pts([(0, 0.70), (0.13, 0.69), (0.26, 0.70), (0.24, 0.90), (0.18, 1.05), (0.20, 1.30), (0.235, 1.45), (0.12, 1.52),
                  (-0.12, 1.52), (-0.235, 1.45), (-0.20, 1.30), (-0.18, 1.05), (-0.24, 0.90), (-0.26, 0.70), (-0.13, 0.69)])
    d = sh.shape(coat, BUFF, direction="h", light=0.25)
    sh.clip_open(d)
    for sx in (-0.13, 0.0, 0.13):  # skirt panel splits
        sh.line(F.pts([(sx, 1.02), (sx * 1.15, 0.85), (sx * 1.3, 0.69)]), dk(BUFF, 0.45), 1.3, op=0.8)
    sh.flat(F.pts([(-0.26, 1.50), (0.26, 1.50), (0.26, 1.34), (-0.26, 1.40)]), dk(BUFF, 0.3), op=0.35)  # rain-dark shoulders
    sh.flat(F.pts([(0.08, 0.69), (0.27, 0.69), (0.27, 1.5), (0.12, 1.5), (0.1, 1.1)]), "#000000", op=0.2)
    sh.flecks(*F.p(-0.24, 1.45), *F.p(0.24, 0.70), 40, dk(BUFF, 0.4), 0.8, 2.0, (0.15, 0.4))
    sh.close()
    sh.line(F.pts([(0, 1.50), (0, 1.05)]), dk(BUFF, 0.5), 1.2)
    for i in range(7):
        sh.line([F.p(-0.012, 1.46 - i * 0.06), F.p(0.012, 1.46 - i * 0.06)], dk(STEEL, 0.1), 1.6, smooth=False)  # hooks
    # sword belt
    sh.shape(F.pts([(-0.185, 1.07), (0.185, 1.07), (0.18, 1.03), (-0.18, 1.03)]), FELT, smooth=False, direction="h")
    sh.rect(*F.p(-0.03, 1.075), 0.06 * F.s, 0.05 * F.s, "none", BRIGHT, 1.3)
    # sword hilt left hip
    sh.shape(limb(F.p(0.19, 1.04), 0.03 * F.s, F.p(0.29, 0.62), 0.025 * F.s), FELT)
    sh.line([F.p(0.13, 1.07), F.p(0.24, 1.10)], STEEL, 2.6, smooth=False)
    sh.line([F.p(0.16, 1.08), F.p(0.13, 1.15)], dk(WAL, 0.1), 3.5, smooth=False)
    # bandolier: left shoulder (viewer right) -> right hip (viewer left)
    a, b = (0.17, 1.47), (-0.20, 0.99)
    band = F.pts([(a[0] + 0.03, a[1] + 0.02), (b[0] + 0.025, b[1] - 0.02), (b[0] - 0.025, b[1] + 0.03), (a[0] - 0.035, a[1] - 0.01)])
    sh.shape(band, FELT, smooth=False, direction="d", light=0.3)
    for i in range(8):
        t = 0.12 + i * 0.105
        px, py = a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t
        charger(sh, *F.p(px, py - 0.015), 1.0)
    # bullet bag + priming flask at hip end
    sh.shape(F.pts([(-0.27, 0.98), (-0.17, 0.98), (-0.16, 0.86), (-0.22, 0.83), (-0.28, 0.87)]), dk(BUFF, 0.35), direction="h")
    sh.line(F.pts([(-0.26, 0.96), (-0.18, 0.96)]), FELT, 1.4)
    sh.shape(F.pts([(-0.14, 0.97), (-0.10, 0.97), (-0.105, 0.83), (-0.125, 0.81), (-0.145, 0.83)]), lit(BUFF, 0.2), direction="h")
    # arms (murrey sleeves from the elbow, buff wings)
    def arm(s, sh_pt, el, hand):
        sh.shape(limb(F.p(*sh_pt), 0.13 * F.s, F.p(*el), 0.11 * F.s, bulge=0.1), BUFF, direction="h", light=0.25)
        sh.shape(limb(F.p(*el), 0.105 * F.s, F.p(*hand), 0.085 * F.s, bulge=0.1), MUR, direction="h")
        sh.shape(limb(F.p(sh_pt[0] - s * 0.01, sh_pt[1] + 0.02), 0.09 * F.s, F.p(sh_pt[0] + s * 0.04, sh_pt[1] - 0.05), 0.08 * F.s), dk(BUFF, 0.15), direction="h", light=0.3)
    arm(-1, (-0.215, 1.44), (-0.30, 1.20), (-0.35, 1.08))
    arm(1, (0.215, 1.44), (0.31, 1.20), (0.36, 1.08))
    for s, x in ((-1, -0.36), (1, 0.37)):
        sh.shape(F.pts([(x - 0.035, 1.12), (x + 0.035, 1.12), (x + 0.035, 1.03), (x - 0.035, 1.03)]), SKIN, direction="h", sw=0.9)
        for k in range(3):
            sh.line([F.p(x - 0.033, 1.10 - k * 0.022), F.p(x + 0.03, 1.10 - k * 0.022)], dk(SKIN, 0.35), 0.8, smooth=False)
    sh.flecks(*F.p(-0.39, 1.12), *F.p(-0.33, 1.03), 8, POWDER, 0.6, 1.4, (0.4, 0.8))
    # slow match looped over left hand, lit both ends
    sh.line(F.pts([(0.34, 1.06), (0.30, 0.95), (0.33, 0.86), (0.40, 0.90), (0.40, 1.03)]), dk(BUFF, 0.3), 2.2)
    sh.line(F.pts([(0.40, 1.08), (0.44, 1.02), (0.45, 0.92), (0.43, 0.84)]), dk(BUFF, 0.3), 2.2)
    ember(sh, *F.p(0.43, 0.84))
    sh.line(F.pts([(0.34, 1.11), (0.30, 1.17), (0.28, 1.2)]), dk(BUFF, 0.3), 2.2)
    ember(sh, *F.p(0.28, 1.2))
    # neck + collar + head + hat
    sh.shape(F.pts([(-0.05, 1.60), (0.05, 1.60), (0.055, 1.50), (-0.055, 1.50)]), dk(SKIN, 0.1), smooth=False, direction="h")
    sh.shape(F.pts([(-0.12, 1.52), (-0.07, 1.555), (0.07, 1.555), (0.12, 1.52), (0.08, 1.49), (-0.08, 1.49)]), dk(BUFF, 0.15), direction="h")
    face = F.pts([(0, 1.555), (0.055, 1.575), (0.078, 1.62), (0.082, 1.69), (0.07, 1.73), (0, 1.745), (-0.07, 1.73), (-0.082, 1.69), (-0.078, 1.62), (-0.055, 1.575)])
    sh.shape(face, SKIN, direction="h", light=0.3)
    for s in (-1, 1):
        sh.ellipse(*F.p(s * 0.085, 1.66), 0.014 * F.s, 0.03 * F.s, dk(SKIN, 0.1), INK, 0.7)
        sh.ellipse(*F.p(s * 0.032, 1.655), 0.013 * F.s, 0.006 * F.s, dk(SKIN, 0.55))
        sh.line([F.p(s * 0.05, 1.675), F.p(s * 0.015, 1.68)], HAIR, 1.8, smooth=False)
        # long hair to the collar
        sh.shape(F.pts([(s * 0.07, 1.72), (s * 0.09, 1.64), (s * 0.095, 1.54), (s * 0.075, 1.53), (s * 0.075, 1.62)]), HAIR, sw=0.6)
    sh.line(F.pts([(-0.004, 1.665), (-0.012, 1.62), (0.006, 1.615)]), dk(SKIN, 0.4), 1.1)
    sh.shape(F.pts([(-0.05, 1.59), (0, 1.607), (0.05, 1.59), (0.03, 1.60), (0, 1.598), (-0.03, 1.60)]), HAIR, sw=0.6)
    sh.shape(F.pts([(-0.013, 1.582), (0.013, 1.582), (0.0, 1.545)]), HAIR, sw=0.6)
    sh.flecks(*F.p(-0.07, 1.64), *F.p(-0.03, 1.58), 9, POWDER, 0.7, 1.8, (0.35, 0.7))  # powder smudge right cheek
    sh.flat(F.pts([(0.03, 1.57), (0.08, 1.64), (0.078, 1.72), (0.05, 1.72), (0.06, 1.62)]), "#000000", op=0.2)
    hat_front(sh, F)


def hat_front(sh, F):
    # crown
    crown = F.pts([(-0.095, 1.745), (-0.085, 1.905), (-0.05, 1.918), (0.05, 1.918), (0.085, 1.905), (0.095, 1.745)])
    sh.shape(crown, FELT, direction="h", light=0.3)
    sh.shape(F.pts([(-0.096, 1.77), (0.096, 1.77), (0.096, 1.745), (-0.096, 1.745)]), MUR, smooth=False, direction="h")  # hatband
    sh.flecks(*F.p(-0.08, 1.915), *F.p(0.08, 1.89), 10, "#8A857A", 0.6, 1.2, (0.15, 0.3))
    # brim: flat on viewer-left, cocked up on viewer-right
    brim = F.pts([(-0.235, 1.725), (-0.12, 1.735), (0.0, 1.742), (0.10, 1.755), (0.17, 1.80), (0.20, 1.88), (0.215, 1.87),
                  (0.19, 1.78), (0.12, 1.735), (0.0, 1.722), (-0.12, 1.715), (-0.23, 1.712)])
    sh.shape(brim, FELT, direction="h", light=0.35)
    sh.line(F.pts([(-0.23, 1.724), (-0.1, 1.735), (0.05, 1.745), (0.12, 1.76), (0.18, 1.81)]), lit(FELT, 0.4), 0.9, op=0.6)
    sh.circle(*F.p(0.13, 1.765), 2.2, STEEL, INK, 0.6)  # pin
    # plume
    plume = F.pts([(0.06, 1.90), (0.12, 1.93), (0.20, 1.915), (0.26, 1.86), (0.27, 1.80), (0.24, 1.79), (0.23, 1.85), (0.17, 1.89), (0.10, 1.895)])
    sh.shape(plume, MUR, direction="h", light=0.4)
    for k in range(9):
        t = k / 9
        sh.line([F.p(0.08 + t * 0.17, 1.905 - t * t * 0.08), F.p(0.09 + t * 0.19, 1.87 - t * t * 0.1)], lit(MUR, 0.35), 0.8, op=0.7, smooth=False)


def side(sh):
    S = Fig(820)
    sh.ellipse(820, 690, 80, 6, "#000000", op=0.45)
    # rest planted (far hand)
    x = 0.36
    sh.shape(S.pts([(x - 0.012, 0.02), (x - 0.013, 1.25), (x + 0.013, 1.25), (x + 0.012, 0.02)]), dk(BUFF, 0.45), smooth=False, direction="r")
    sh.shape(S.pts([(x - 0.008, 0.0), (x + 0.008, 0.0), (x + 0.012, 0.06), (x - 0.012, 0.06)]), STEEL, smooth=False)
    fork = S.pts([(x - 0.05, 1.33), (x - 0.045, 1.26), (x, 1.235), (x + 0.045, 1.26), (x + 0.05, 1.33), (x + 0.036, 1.33), (x + 0.03, 1.27), (x, 1.255), (x - 0.03, 1.27), (x - 0.036, 1.33)])
    sh.shape(fork, dk(STEEL, 0.2), direction="r")
    sh.shape(limb(S.p(0.05, 1.40), 0.10 * S.s, S.p(0.33, 1.08), 0.08 * S.s), dk(MUR, 0.35), direction="r")
    sh.shape(S.pts([(0.32, 1.12), (0.38, 1.12), (0.38, 1.03), (0.32, 1.03)]), dk(SKIN, 0.25), direction="r", sw=0.9)
    # legs
    for off, shade in ((-0.04, 0.35), (0.04, 0.0)):
        sh.shape(S.pts([(-0.08 + off, 0.76), (-0.07 + off, 0.55), (0.08 + off, 0.55), (0.09 + off, 0.76)]), dk(MUR, shade), smooth=False, direction="r")
        boot = S.pts([(-0.06 + off, 0.52), (-0.07 + off, 0.30), (-0.055 + off, 0.10), (-0.06 + off, 0.0), (0.17 + off, 0.0), (0.16 + off, 0.05), (0.06 + off, 0.08), (0.05 + off, 0.30), (0.065 + off, 0.52)])
        sh.shape(boot, dk(BOOT, shade), direction="r", light=0.3)
        sh.shape(S.pts([(-0.10 + off, 0.62), (-0.08 + off, 0.50), (0.07 + off, 0.49), (0.11 + off, 0.62), (0.0 + off, 0.60)]), dk(lit(BOOT, 0.08), shade), direction="r", light=0.35)
        sh.rect(S.p(-0.06 + off, 0)[0], S.p(0, 0.035)[1], 0.05 * S.s, 0.035 * S.s, dk(BOOT, 0.4 + shade * 0.3))
        for k in range(3):
            sh.line(S.pts([(-0.06 + off, 0.15 + k * 0.035), (0.0 + off, 0.14 + k * 0.035), (0.05 + off, 0.16 + k * 0.035)]), dk(BOOT, 0.4), 1.0, op=0.6)
    # coat profile
    coat = S.pts([(-0.22, 0.70), (0.0, 0.69), (0.22, 0.71), (0.18, 0.95), (0.15, 1.08), (0.16, 1.30), (0.12, 1.45), (0.06, 1.52),
                  (-0.07, 1.52), (-0.13, 1.42), (-0.13, 1.20), (-0.12, 1.06), (-0.18, 0.90)])
    d = sh.shape(coat, BUFF, direction="r", light=0.25)
    sh.clip_open(d)
    sh.line(S.pts([(0.0, 1.05), (0.02, 0.85), (0.03, 0.69)]), dk(BUFF, 0.45), 1.3, op=0.8)
    sh.flat(S.pts([(-0.2, 1.52), (0.2, 1.52), (0.2, 1.36), (-0.2, 1.4)]), dk(BUFF, 0.3), op=0.35)
    sh.flat(S.pts([(-0.23, 0.69), (-0.05, 0.69), (-0.08, 1.52), (-0.2, 1.52)]), "#000000", op=0.22)
    sh.flecks(*S.p(-0.2, 1.45), *S.p(0.2, 0.70), 30, dk(BUFF, 0.4), 0.8, 2.0, (0.15, 0.4))
    sh.close()
    sh.shape(S.pts([(-0.14, 1.07), (0.16, 1.07), (0.16, 1.03), (-0.14, 1.03)]), FELT, smooth=False, direction="r")
    # sword on the near hip, scabbard back
    sh.shape(limb(S.p(0.0, 1.03), 0.03 * S.s, S.p(-0.40, 0.55), 0.025 * S.s), FELT, direction="h")
    sh.line([S.p(0.05, 1.12), S.p(0.07, 0.98)], STEEL, 2.6, smooth=False)
    sh.line([S.p(0.02, 1.06), S.p(0.10, 1.10)], dk(WAL, 0.1), 3.5, smooth=False)
    # bandolier across chest with chargers
    sh.shape(S.pts([(-0.02, 1.50), (0.06, 1.50), (0.15, 1.20), (0.10, 0.98), (0.03, 1.00), (0.08, 1.20)]), FELT, direction="r", light=0.3)
    for i, (px, py) in enumerate([(0.09, 1.40), (0.12, 1.30), (0.14, 1.20), (0.125, 1.10), (0.04, 1.00), (-0.03, 0.99), (-0.10, 1.00)]):
        charger(sh, *S.p(px, py), 1.0, 0.25 if px < 0.05 else 0)
    sh.shape(S.pts([(-0.20, 0.99), (-0.12, 0.99), (-0.115, 0.86), (-0.16, 0.83), (-0.21, 0.87)]), dk(BUFF, 0.35), direction="r")
    # musket shouldered: butt low-front in near hand, barrel over the shoulder, muzzle up-back
    bx, by = 0.30, 1.16
    ux, uy = -0.8, 0.6
    L = 1.55
    def at(t, off=0.0):
        return S.p(bx + ux * t + (-uy) * off, by + uy * t + ux * off)
    stock = [at(0, -0.055), at(0, 0.05), at(0.12, 0.035), at(0.36, 0.025), at(0.62, 0.028), at(1.20, 0.022), at(1.20, -0.022), at(0.62, -0.03), at(0.36, -0.03), at(0.12, -0.045)]
    sh.shape(stock, WAL, smooth=False, direction="d", light=0.35)
    sh.shape([at(0.62, -0.013), at(L, -0.012), at(L, 0.012), at(0.62, 0.013)], STEEL, smooth=False, direction="d", light=0.45)
    sh.path(poly_path([at(L - 0.10, -0.012), at(L, -0.012), at(L, 0.012), at(L - 0.10, 0.012)]), POWDER, op=0.7)
    sh.line([at(0.64, 0.004), at(L - 0.02, 0.004)], BRIGHT, 0.8, op=0.6, smooth=False)
    for t in (0.8, 1.05):
        sh.path(poly_path([at(t, -0.026), at(t + 0.012, -0.026), at(t + 0.012, 0.026), at(t, 0.026)]), STEEL, INK, 0.6)
    sh.path(poly_path([at(0.44, -0.035), at(0.64, -0.035), at(0.64, -0.012), at(0.44, -0.012)]), STEEL, INK, 0.8)  # lockplate
    sh.line([at(0.52, -0.035), at(0.56, -0.07), at(0.61, -0.075), at(0.64, -0.10)], STEEL_HI, 2.2)  # serpentine
    sh.circle(*at(0.64, -0.10), 2, EMBER, op=0.0)
    # near arm: shoulder down to butt
    sh.shape(limb(S.p(0.0, 1.44), 0.13 * S.s, S.p(0.04, 1.20), 0.11 * S.s, bulge=0.1), BUFF, direction="r", light=0.25)
    sh.shape(limb(S.p(0.02, 1.47), 0.09 * S.s, S.p(0.04, 1.40), 0.08 * S.s), dk(BUFF, 0.1), direction="r", light=0.3)
    sh.shape(limb(S.p(0.04, 1.20), 0.105 * S.s, S.p(0.27, 1.16), 0.085 * S.s), MUR, direction="r")
    sh.shape(S.pts([(0.25, 1.21), (0.33, 1.21), (0.34, 1.12), (0.26, 1.11)]), SKIN, direction="r", sw=0.9)
    # match coiled in near hand, both ends lit
    sh.line(S.pts([(0.27, 1.13), (0.24, 1.04), (0.28, 0.98), (0.34, 1.03), (0.33, 1.13)]), dk(BUFF, 0.3), 2.2)
    sh.line(S.pts([(0.33, 1.13), (0.40, 1.08), (0.43, 1.01)]), dk(BUFF, 0.3), 2.2)
    ember(sh, *S.p(0.43, 1.01))
    sh.line(S.pts([(0.29, 1.20), (0.33, 1.26), (0.38, 1.28)]), dk(BUFF, 0.3), 2.2)
    ember(sh, *S.p(0.38, 1.28))
    # neck, collar, head
    sh.shape(S.pts([(-0.05, 1.60), (0.045, 1.58), (0.05, 1.50), (-0.055, 1.50)]), dk(SKIN, 0.1), smooth=False, direction="r")
    sh.shape(S.pts([(-0.08, 1.52), (-0.05, 1.56), (0.06, 1.56), (0.10, 1.52), (0.08, 1.49), (-0.07, 1.49)]), dk(BUFF, 0.15), direction="r")
    face = S.pts([(-0.06, 1.56), (0.0, 1.555), (0.06, 1.555), (0.09, 1.565), (0.098, 1.595), (0.105, 1.605), (0.098, 1.62), (0.125, 1.64),
                  (0.10, 1.665), (0.102, 1.69), (0.09, 1.73), (0.0, 1.75), (-0.09, 1.72), (-0.095, 1.64)])
    sh.shape(face, SKIN, direction="r", light=0.3)
    sh.shape(S.pts([(-0.095, 1.73), (-0.11, 1.62), (-0.10, 1.54), (-0.05, 1.55), (-0.06, 1.66), (-0.02, 1.73)]), HAIR, sw=0.6)
    sh.ellipse(*S.p(0.0, 1.66), 0.016 * S.s, 0.03 * S.s, dk(SKIN, 0.1), INK, 0.7)
    sh.ellipse(*S.p(0.07, 1.655), 0.012 * S.s, 0.006 * S.s, dk(SKIN, 0.55))
    sh.line([S.p(0.05, 1.68), S.p(0.095, 1.678)], HAIR, 1.8, smooth=False)
    sh.shape(S.pts([(0.06, 1.595), (0.11, 1.605), (0.085, 1.61)]), HAIR, sw=0.5)
    sh.shape(S.pts([(0.07, 1.585), (0.095, 1.575), (0.09, 1.54), (0.075, 1.56)]), HAIR, sw=0.5)
    sh.flat(S.pts([(-0.06, 1.56), (0.02, 1.57), (0.0, 1.72), (-0.09, 1.72)]), "#000000", op=0.2)
    # hat profile
    sh.shape(S.pts([(-0.09, 1.745), (-0.08, 1.905), (-0.04, 1.918), (0.05, 1.918), (0.085, 1.905), (0.095, 1.745)]), FELT, direction="r", light=0.3)
    sh.shape(S.pts([(-0.092, 1.77), (0.097, 1.77), (0.097, 1.745), (-0.092, 1.745)]), MUR, smooth=False, direction="r")
    brim = S.pts([(-0.235, 1.70), (-0.12, 1.735), (0.0, 1.745), (0.12, 1.738), (0.235, 1.715), (0.232, 1.70), (0.12, 1.72), (0.0, 1.726), (-0.12, 1.718), (-0.23, 1.685)])
    sh.shape(brim, FELT, direction="r", light=0.35)
    sh.line(S.pts([(-0.23, 1.70), (-0.1, 1.736), (0.1, 1.739), (0.23, 1.714)]), lit(FELT, 0.4), 0.9, op=0.6)
    plume = S.pts([(0.02, 1.90), (-0.06, 1.935), (-0.16, 1.92), (-0.24, 1.86), (-0.27, 1.78), (-0.24, 1.78), (-0.22, 1.84), (-0.15, 1.885), (-0.06, 1.895)])
    sh.shape(plume, MUR, direction="r", light=0.4)
    for k in range(9):
        t = k / 9
        sh.line([S.p(-0.02 - t * 0.2, 1.905 - t * t * 0.09), S.p(-0.03 - t * 0.22, 1.875 - t * t * 0.1)], lit(MUR, 0.35), 0.8, op=0.7, smooth=False)


def build():
    sh = Sheet(seed=12)
    front(sh)
    side(sh)
    height_mark(sh, 560, 1.92, "1.92 m (hat crown)", 206)
    height_mark(sh, 300, 1.55, "musket 1.55 m")
    callouts(sh, [
        (820 + 0.0 * 220, 690 - 220 * 1.90, "BROAD HAT + PLUME", "black felt · brim 0.46 m"),
        (820 - 0.66 * 220, 690 - 220 * 1.98, "MATCHLOCK MUSKET", "barrel 1.15 m blued · muzzle sooted", 700, 140, "end"),
        (820 + 0.36 * 220, 690 - 220 * 1.30, "FORKED REST", "ash 1.30 m · iron U-fork"),
        (820 + 0.12 * 220, 690 - 220 * 1.30, "TWELVE APOSTLES", "walnut chargers on felt band"),
        (820 + 0.38 * 220, 690 - 220 * 1.20, "SLOW MATCH, LIT BOTH ENDS", "madder ember · emissive"),
        (820 + 0.05 * 220, 690 - 220 * 1.18, "BUFF COAT", "oil-tanned leather · 8 mm"),
        (820 - 0.16 * 220, 690 - 220 * 0.92, "BULLET BAG", "buff leather · ball pouch", 725, 525, "end"),
        (820 - 0.35 * 220, 690 - 220 * 0.60, "SHORT SWORD", "felt-black scabbard", 725, 595, "end"),
        (820 + 0.12 * 220, 690 - 220 * 0.58, "BUCKET-TOP BOOTS", "dressed buff · turned down"),
    ], y0=150, dy=62)
    return sh.render("THE AGE OF POWDER · ENEMY · RANGED", "Musketeer", "H 1.92 m · ≤ 8k tris · 2048²",
                     "1 m = 220 px · ground y 690", [(BUFF, "buff leather"), (MUR, "murrey wool"), (FELT, "black felt"),
                                                    (WAL, "walnut stock"), (STEEL, "blued steel"), (SKIN, "skin"), (EMBER, "match ember")],
                     view_labels=[(470, "FRONT"), (820, "SIDE")])
