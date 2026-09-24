from lib import *

MUR = "#5B3036"; LIN = "#8A5A5E"; STEEL = "#34404E"; LINEN = "#D2C7AC"; LEATH = "#221C18"
ASH = "#8A7254"; HORN = "#B09A62"; SKIN = "#B88E6E"; BRIGHT = "#9AA3A8"; STEEL_HI = "#6E7E92"
HAIR = dk(SKIN, 0.7)


def interp(table, y):
    for (y0, v0), (y1, v1) in zip(table, table[1:]):
        if y0 >= y >= y1:
            t = (y - y0) / (y1 - y0)
            return v0 + (v1 - v0) * t
    return table[-1][1]


def partisan(sh, F, x, y0=0.02, lean=0.0):
    """Upright partisan; x is haft centre (m)."""
    top = y0 + 2.20
    # haft
    sh.shape([F.p(x - 0.017, y0 + 0.08), F.p(x - 0.017, top), F.p(x + 0.017, top), F.p(x + 0.017, y0 + 0.08)], ASH, smooth=False, direction="h", sw=0.9)
    for yy in (0.9, 1.4):  # polished grip bands
        sh.rect(F.p(x - 0.017, 0)[0], F.p(0, yy + 0.08)[1], 0.034 * F.s, 0.16 * F.s, dk(ASH, 0.25), op=0.55)
    for k in range(9):  # grain
        yy = y0 + 0.2 + k * 0.22
        sh.line([F.p(x - 0.006, yy), F.p(x - 0.004, yy + 0.12)], lit(ASH, 0.3), 0.7, op=0.5, smooth=False)
    # ferrule
    sh.shape([F.p(x - 0.019, y0), F.p(x - 0.019, y0 + 0.09), F.p(x + 0.019, y0 + 0.09), F.p(x + 0.019, y0)], STEEL, smooth=False)
    # langets
    for dx in (-0.012, 0.012):
        sh.line([F.p(x + dx, top - 0.30), F.p(x + dx, top)], STEEL_HI, 1.4, smooth=False)
        for yy in (top - 0.25, top - 0.12):
            sh.circle(*F.p(x + dx, yy), 1.2, BRIGHT)
    # tassels
    for dx, c in ((-0.03, MUR), (0.03, dk(MUR, 0.2))):
        sh.shape([F.p(x + dx * 0.4, top - 0.01), F.p(x + dx - 0.02, top - 0.10), F.p(x + dx, top - 0.13), F.p(x + dx + 0.02, top - 0.10)], c, sw=0.7)
        for j in range(4):
            sh.line([F.p(x + dx - 0.012 + j * 0.008, top - 0.10), F.p(x + dx - 0.014 + j * 0.009, top - 0.15)], lit(c, 0.2), 0.8, smooth=False)
    # socket
    sh.shape([F.p(x - 0.022, top - 0.02), F.p(x - 0.02, top + 0.09), F.p(x + 0.02, top + 0.09), F.p(x + 0.022, top - 0.02)], STEEL, smooth=False)
    sh.ellipse(*F.p(x, top + 0.02), 0.024 * F.s, 3, STEEL_HI, INK, 0.6)
    # flukes + blade
    b = top + 0.09
    head = [F.p(x, b), F.p(x - 0.05, b + 0.02), F.p(x - 0.10, b + 0.03), F.p(x - 0.11, b + 0.12), F.p(x - 0.085, b + 0.07),
            F.p(x - 0.045, b + 0.06), F.p(x - 0.035, b + 0.10), F.p(x - 0.03, b + 0.22), F.p(x, b + 0.33),
            F.p(x + 0.03, b + 0.22), F.p(x + 0.035, b + 0.10), F.p(x + 0.045, b + 0.06), F.p(x + 0.085, b + 0.07),
            F.p(x + 0.11, b + 0.12), F.p(x + 0.10, b + 0.03), F.p(x + 0.05, b + 0.02)]
    sh.shape(head, STEEL, smooth=False, direction="h", light=0.4)
    sh.line([F.p(x, b + 0.03), F.p(x, b + 0.31)], BRIGHT, 1.0, op=0.8, smooth=False)  # midrib
    sh.line([F.p(x - 0.028, b + 0.12), F.p(x - 0.004, b + 0.30)], BRIGHT, 0.8, op=0.6, smooth=False)
    # etching
    for k in range(3):
        sh.line([F.p(x - 0.015, b + 0.08 + k * 0.035), F.p(x, b + 0.095 + k * 0.035), F.p(x + 0.015, b + 0.08 + k * 0.035)], dk(STEEL, 0.3), 0.7, smooth=False)
    sh.line([F.p(x - 0.10, b + 0.035), F.p(x - 0.108, b + 0.115)], BRIGHT, 1.0, op=0.7, smooth=False)
    sh.line([F.p(x + 0.10, b + 0.035), F.p(x + 0.108, b + 0.115)], BRIGHT, 1.0, op=0.7, smooth=False)


def lantern(sh, F, x, ytop):
    w, h = 0.12, 0.22
    sh.line([F.p(x, ytop + 0.04), F.p(x, ytop)], STEEL, 1.2, smooth=False)
    sh.glow(*F.p(x, ytop - h * 0.5), 0.22 * F.s, "#C4542E", 0.18)
    sh.shape([F.p(x - w / 2, ytop - 0.03), F.p(x + w / 2, ytop - 0.03), F.p(x + w / 2, ytop - h), F.p(x - w / 2, ytop - h)], HORN, smooth=False, direction="h", light=0.5)
    sh.glow(*F.p(x, ytop - h * 0.55), 0.06 * F.s, "#F2D9A8", 0.6)
    sh.shape([F.p(x - w / 2 - 0.01, ytop - 0.03), F.p(x, ytop + 0.03), F.p(x + w / 2 + 0.01, ytop - 0.03)], STEEL, smooth=False)
    sh.rect(F.p(x - w / 2 - 0.01, 0)[0], F.p(0, ytop - h + 0.01)[1], (w + 0.02) * F.s, 0.02 * F.s, STEEL, INK, 0.8)
    for dx in (-w / 2, 0, w / 2):
        sh.line([F.p(x + dx, ytop - 0.03), F.p(x + dx, ytop - h)], dk(STEEL, 0.2), 1.3, smooth=False)
    sh.flecks(*F.p(x - w / 2, ytop + 0.02), *F.p(x + w / 2, ytop - 0.04), 6, "#2E2923", 0.8, 1.6)


def front(sh):
    F = Fig(470)
    # ground shadow
    sh.ellipse(470, 690, 95, 7, "#000000", op=0.45)
    # scabbard (behind)
    sh.shape(limb(F.p(0.21, 1.00), 0.035 * F.s, F.p(0.31, 0.36), 0.028 * F.s), LEATH, direction="h")
    sh.shape([F.p(0.30, 0.40), F.p(0.325, 0.40), F.p(0.318, 0.34), F.p(0.305, 0.34)], STEEL, smooth=False)
    # partisan (in right hand, viewer's left)
    partisan(sh, F, -0.335)
    # legs
    for s in (-1, 1):
        leg = [(s * 0.035, 0.48), (s * 0.03, 0.32), (s * 0.04, 0.12), (s * 0.055, 0.06), (s * 0.12, 0.06), (s * 0.135, 0.12),
               (s * 0.15, 0.32), (s * 0.145, 0.48)]
        leg = [(x if s > 0 else x, y) for x, y in leg]
        pts = F.pts(sorted(leg, key=lambda p: (p[1], p[0]))) if False else F.pts(leg)
        sh.shape(pts, dk(MUR, 0.15), direction="h")
        sh.line(F.pts([(s * 0.09, 0.44), (s * 0.08, 0.30), (s * 0.085, 0.14)]), lit(MUR, 0.2), 1.0, op=0.35)
        # shoes
        shoe = [(s * 0.04, 0.075), (s * 0.13, 0.075), (s * 0.155, 0.02), (s * 0.14, 0.0), (s * 0.04, 0.0), (s * 0.03, 0.03)]
        sh.shape(F.pts(shoe), LEATH, direction="h", light=0.25)
        sh.ellipse(*F.p(s * 0.095, 0.07), 0.035 * F.s, 0.018 * F.s, MUR, INK, 0.8)  # rosette
        sh.ellipse(*F.p(s * 0.095, 0.07), 0.012 * F.s, 0.008 * F.s, lit(MUR, 0.3))
        # canions
        can = [(s * 0.03, 0.72), (s * 0.03, 0.48), (s * 0.155, 0.48), (s * 0.16, 0.72)]
        sh.shape(F.pts(can), MUR, smooth=False, direction="h")
        sh.shape(F.pts([(s * 0.025, 0.505), (s * 0.165, 0.505), (s * 0.165, 0.475), (s * 0.025, 0.475)]), LEATH, smooth=False)
        # garter bow
        sh.shape(F.pts([(s * 0.14, 0.49), (s * 0.20, 0.52), (s * 0.19, 0.46)]), LIN, sw=0.8)
        sh.shape(F.pts([(s * 0.15, 0.49), (s * 0.18, 0.42), (s * 0.16, 0.41)]), LIN, sw=0.8)
    # trunk hose
    outer = [(1.03, 0.15), (0.96, 0.24), (0.87, 0.28), (0.78, 0.25), (0.69, 0.165)]
    inner = [(1.03, 0.0), (0.87, 0.01), (0.76, 0.02), (0.69, 0.03)]
    ys = [1.03 - i * 0.02 for i in range(18)] + [0.69]
    for s in (-1, 1):
        outline = [(s * interp(outer, y), y) for y in ys] + [(s * interp(inner, y), y) for y in reversed(ys)]
        d = sh.shape(F.pts(outline), LIN, direction="h", light=0.2)
        sh.clip_open(d)
        np_ = 5
        for k in range(np_):
            f0, f1 = k / np_ + 0.03, (k + 1) / np_ - 0.03
            pane = []
            for y in ys:
                a, b = interp(inner, y), interp(outer, y)
                pane.append((s * (a + (b - a) * f0), y))
            for y in reversed(ys):
                a, b = interp(inner, y), interp(outer, y)
                pane.append((s * (a + (b - a) * f1), y))
            base = mix(MUR, LIN, 0.1 * (k % 2))
            sh.shape(F.pts(pane), base, direction="h", light=0.28, dark=0.45, sw=0.8)
            # pane highlight
            sh.line(F.pts([(s * (interp(inner, y) + (interp(outer, y) - interp(inner, y)) * (f0 + 0.05)), y) for y in ys[2:-3]]), lit(MUR, 0.35), 0.9, op=0.4)
        # puff shadow at bottom
        sh.flat(F.pts([(s * 0.02, 0.69), (s * 0.17, 0.69), (s * 0.25, 0.76), (s * 0.02, 0.74)]), "#000000", op=0.3)
        sh.close()
        sh.path(d, "none", INK, 1.1)
    # doublet torso
    torso = F.pts([(0.0, 0.965), (0.1, 1.0), (0.155, 1.04), (0.165, 1.2), (0.18, 1.33), (0.225, 1.45), (0.12, 1.51),
                   (-0.12, 1.51), (-0.225, 1.45), (-0.18, 1.33), (-0.165, 1.2), (-0.155, 1.04), (-0.1, 1.0)])
    d = sh.shape(torso, MUR, direction="h")
    sh.clip_open(d)
    for k in range(6):  # quilted diagonal fold lines
        sh.line(F.pts([(-0.16 + k * 0.06, 1.40), (-0.14 + k * 0.06, 1.2), (-0.1 + k * 0.05, 1.02)]), dk(MUR, 0.3), 1.0, op=0.45)
    sh.flat(F.pts([(0.06, 0.96), (0.2, 1.05), (0.2, 1.5), (0.1, 1.5)]), "#000000", op=0.22)
    sh.close()
    # centre front opening + buttons
    sh.line(F.pts([(0, 1.49), (0, 1.2), (0, 0.97)]), dk(MUR, 0.5), 1.1)
    for i in range(12):
        y = 1.45 - i * 0.038
        sh.circle(*F.p(0.012, y), 2.0, STEEL, INK, 0.6)
        sh.circle(*F.p(0.009, y + 0.004), 0.8, BRIGHT)
    # belt
    sh.shape(F.pts([(-0.158, 1.045), (0.158, 1.045), (0.155, 1.005), (-0.155, 1.005)]), LEATH, smooth=False, direction="h")
    sh.rect(*F.p(-0.035, 1.05), 0.07 * F.s, 0.05 * F.s, "none", BRIGHT, 1.4)
    # lantern on right hip (viewer's left)
    lantern(sh, F, -0.21, 0.99)
    # sash (right shoulder -> left hip)
    sash = F.pts([(-0.22, 1.47), (-0.12, 1.49), (0.17, 1.10), (0.19, 0.99), (0.10, 0.97), (-0.21, 1.33)])
    d = sh.shape(sash, dk(MUR, 0.1), direction="d", light=0.35, dark=0.55)
    for k in range(4):
        t = 0.2 + k * 0.18
        sh.line([F.p(-0.2 + 0.35 * t, 1.45 - 0.45 * t), F.p(-0.13 + 0.33 * t, 1.47 - 0.42 * t)], "#1E1C1A", 1.4, op=0.5)
    # knot + tail
    sh.shape(F.pts([(0.13, 1.03), (0.21, 1.04), (0.23, 0.98), (0.16, 0.95), (0.11, 0.98)]), dk(MUR, 0.05), direction="h")
    tail = F.pts([(0.17, 0.97), (0.23, 0.97), (0.26, 0.74), (0.21, 0.72), (0.19, 0.85)])
    sh.shape(tail, dk(MUR, 0.15), direction="h")
    for j in range(7):
        sh.line([F.p(0.21 + j * 0.008, 0.735), F.p(0.206 + j * 0.009, 0.70)], lit(MUR, 0.15), 0.9, smooth=False)
    # arms
    def arm(s, sh_pt, el, hand):
        up = limb(F.p(*sh_pt), 0.12 * F.s, F.p(*el), 0.10 * F.s, bulge=0.12)
        d = sh.shape(up, MUR, direction="h")
        sh.clip_open(d)
        for k in range(4):  # slashes
            fx = -0.035 + k * 0.024
            a = (sh_pt[0] + fx, sh_pt[1] - 0.04)
            b = (el[0] + fx * 0.8, el[1] + 0.05)
            sh.line([F.p(*a), F.p((a[0] + b[0]) / 2 + 0.004, (a[1] + b[1]) / 2), F.p(*b)], LIN, 2.2, op=0.9)
        sh.close()
        # shoulder wing
        sh.shape(limb(F.p(sh_pt[0] - s * 0.01, sh_pt[1] + 0.01), 0.07 * F.s, F.p(sh_pt[0] + s * 0.035, sh_pt[1] - 0.04), 0.06 * F.s), MUR, direction="h", light=0.4)
        fa = limb(F.p(*el), 0.095 * F.s, F.p(*hand), 0.075 * F.s)
        sh.shape(fa, dk(MUR, 0.05), direction="h")
        # linen cuff
        hx_, hy_ = hand
        sh.shape(limb(F.p(hx_ + (el[0] - hx_) * 0.18, hy_ + (el[1] - hy_) * 0.18), 0.085 * F.s, F.p(hx_ + (el[0] - hx_) * 0.02, hy_ + (el[1] - hy_) * 0.02), 0.08 * F.s), LINEN, direction="h", sw=0.8)
    arm(-1, (-0.205, 1.44), (-0.30, 1.19), (-0.33, 1.04))
    arm(1, (0.205, 1.44), (0.30, 1.19), (0.23, 1.02))
    # right hand gripping haft
    sh.shape(F.pts([(-0.36, 1.07), (-0.305, 1.075), (-0.30, 0.99), (-0.35, 0.985)]), SKIN, direction="h", sw=0.9)
    for k in range(3):
        sh.line([F.p(-0.357, 1.055 - k * 0.022), F.p(-0.31, 1.055 - k * 0.022)], dk(SKIN, 0.35), 0.8, smooth=False)
    # sword hilt (left hip) + left hand on pommel
    sh.shape(limb(F.p(0.20, 1.03), 0.02 * F.s, F.p(0.16, 1.14), 0.02 * F.s), LEATH)
    sh.line([F.p(0.12, 1.02), F.p(0.26, 1.06)], STEEL, 3, smooth=False)  # quillon
    sh.line([F.p(0.13, 1.03), F.p(0.13, 1.09), F.p(0.20, 1.12), F.p(0.26, 1.06)], STEEL_HI, 1.4)  # swept guard
    sh.shape(F.pts([(0.19, 1.00), (0.27, 1.01), (0.28, 0.95), (0.20, 0.94)]), SKIN, direction="h", sw=0.9)
    # gorget, collar, neck, head
    sh.shape(F.pts([(-0.05, 1.60), (0.05, 1.60), (0.055, 1.49), (-0.055, 1.49)]), dk(SKIN, 0.1), smooth=False, direction="h")
    sh.shape(F.pts([(-0.14, 1.47), (-0.07, 1.535), (0.07, 1.535), (0.14, 1.47), (0.12, 1.45), (-0.12, 1.45)]), STEEL, direction="h", light=0.45)
    sh.line(F.pts([(-0.12, 1.49), (0, 1.515), (0.12, 1.49)]), BRIGHT, 1.0, op=0.8)
    collar = F.pts([(-0.13, 1.525), (-0.06, 1.545), (0, 1.53), (0.06, 1.545), (0.13, 1.525), (0.11, 1.48), (0.02, 1.47), (0, 1.49), (-0.02, 1.47), (-0.11, 1.48)])
    sh.shape(collar, LINEN, direction="h", light=0.15)
    head(sh, F)


def head(sh, F):
    # face
    face = F.pts([(0, 1.555), (0.055, 1.575), (0.078, 1.62), (0.082, 1.69), (0.07, 1.73), (0, 1.745), (-0.07, 1.73), (-0.082, 1.69), (-0.078, 1.62), (-0.055, 1.575)])
    sh.shape(face, SKIN, direction="h", light=0.3)
    for s in (-1, 1):
        sh.ellipse(*F.p(s * 0.085, 1.66), 0.014 * F.s, 0.03 * F.s, dk(SKIN, 0.1), INK, 0.7)
    # eyes, brows, nose
    for s in (-1, 1):
        sh.ellipse(*F.p(s * 0.032, 1.655), 0.013 * F.s, 0.006 * F.s, dk(SKIN, 0.55))
        sh.line([F.p(s * 0.05, 1.675), F.p(s * 0.015, 1.68)], dk(LEATH, 0.1), 1.8, smooth=False)
    sh.line(F.pts([(-0.004, 1.665), (-0.012, 1.62), (0.006, 1.615)]), dk(SKIN, 0.4), 1.1)
    sh.flat(F.pts([(0.005, 1.66), (0.02, 1.62), (0.006, 1.615)]), "#000000", op=0.2)
    # moustache + beard (dark walnut-brown)
    sh.shape(F.pts([(-0.045, 1.595), (0, 1.607), (0.045, 1.595), (0.03, 1.603), (0, 1.598), (-0.03, 1.603)]), HAIR, sw=0.6)
    sh.shape(F.pts([(-0.015, 1.585), (0.015, 1.585), (0.0, 1.535)]), HAIR, sw=0.6)
    sh.flat(F.pts([(0.03, 1.57), (0.08, 1.64), (0.078, 1.72), (0.05, 1.72), (0.06, 1.62)]), "#000000", op=0.18)
    # cheek pieces
    for s in (-1, 1):
        sh.shape(F.pts([(s * 0.078, 1.725), (s * 0.098, 1.725), (s * 0.092, 1.60), (s * 0.072, 1.585), (s * 0.068, 1.68)]), STEEL, direction="h")
        sh.circle(*F.p(s * 0.084, 1.70), 1.6, BRIGHT)
    sh.line(F.pts([(-0.075, 1.59), (-0.02, 1.545), (0.02, 1.545), (0.075, 1.59)]), LEATH, 1.2)
    # morion (front)
    skull = F.pts([(-0.105, 1.715), (-0.10, 1.77), (-0.06, 1.805), (0, 1.815), (0.06, 1.805), (0.10, 1.77), (0.105, 1.715)])
    sh.shape(skull, STEEL, direction="h", light=0.45)
    sh.shape(F.pts([(-0.012, 1.80), (-0.01, 1.85), (0.01, 1.85), (0.012, 1.80)]), STEEL, smooth=False, direction="h", light=0.5)
    sh.line([F.p(0, 1.806), F.p(0, 1.848)], BRIGHT, 0.9, smooth=False)
    brim = F.pts([(-0.165, 1.715), (-0.10, 1.722), (-0.03, 1.75), (0, 1.79), (0.03, 1.75), (0.10, 1.722), (0.165, 1.715),
                  (0.16, 1.702), (0.10, 1.705), (0.03, 1.73), (0, 1.768), (-0.03, 1.73), (-0.10, 1.705), (-0.16, 1.702)])
    sh.shape(brim, STEEL, direction="h", light=0.5)
    sh.line(F.pts([(-0.16, 1.713), (-0.08, 1.72), (-0.02, 1.755), (0, 1.787)]), BRIGHT, 1.0, op=0.85)
    sh.rivets([F.p(x, 1.73 + 0.012 * math.cos(x * 12)) for x in (-0.085, -0.055, 0.055, 0.085)], 1.6, STEEL_HI)
    sh.line(F.pts([(-0.09, 1.78), (-0.05, 1.80)]), BRIGHT, 1.2, op=0.6)


def side(sh):
    S = Fig(820)
    sh.ellipse(820, 690, 70, 6, "#000000", op=0.45)
    # far leg (darker)
    for off, shade in ((-0.03, 0.35), (0.03, 0.0)):
        leg = [(-0.045 + off, 0.48), (-0.07 + off, 0.30), (-0.045 + off, 0.10), (-0.04 + off, 0.06), (0.04 + off, 0.06), (0.04 + off, 0.15), (0.045 + off, 0.30), (0.05 + off, 0.48)]
        sh.shape(S.pts(leg), dk(MUR, 0.15 + shade), direction="r")
        shoe = [(-0.055 + off, 0.0), (-0.055 + off, 0.06), (0.04 + off, 0.075), (0.13 + off, 0.035), (0.16 + off, 0.0)]
        sh.shape(S.pts(shoe), dk(LEATH, shade * 0.5), direction="r", light=0.3)
        sh.ellipse(*S.p(0.06 + off, 0.07), 0.03 * S.s, 0.014 * S.s, dk(MUR, shade), INK, 0.8)
        sh.rect(S.p(-0.055 + off, 0)[0], S.p(0, 0.03)[1], 0.035 * S.s, 0.03 * S.s, dk(LEATH, 0.3 + shade * 0.3))
        sh.shape(S.pts([(-0.07 + off, 0.72), (-0.065 + off, 0.48), (0.06 + off, 0.48), (0.07 + off, 0.72)]), dk(MUR, shade), smooth=False, direction="r")
        sh.shape(S.pts([(-0.07 + off, 0.505), (0.065 + off, 0.505), (0.065 + off, 0.475), (-0.07 + off, 0.475)]), LEATH, smooth=False)
    sh.shape(S.pts([(0.06, 0.49), (0.12, 0.52), (0.11, 0.46)]), LIN, sw=0.8)
    # scabbard (runs back)
    sh.shape(limb(S.p(0.02, 1.0), 0.036 * S.s, S.p(-0.34, 0.42), 0.03 * S.s), LEATH, direction="h")
    sh.shape(limb(S.p(-0.32, 0.45), 0.034 * S.s, S.p(-0.355, 0.40), 0.03 * S.s), STEEL)
    # trunk hose profile
    outer = [(1.03, 0.14), (0.96, 0.21), (0.87, 0.235), (0.78, 0.21), (0.69, 0.10)]
    ys = [1.03 - i * 0.02 for i in range(18)] + [0.69]
    outline = [(interp(outer, y) * 1.0, y) for y in ys] + [(-interp(outer, y) * 0.95, y) for y in reversed(ys)]
    d = sh.shape(S.pts(outline), LIN, direction="r", light=0.2)
    sh.clip_open(d)
    for k in range(7):
        f0, f1 = k / 7 + 0.02, (k + 1) / 7 - 0.02
        pane = [((-0.95 + 1.95 * f0) * interp(outer, y), y) for y in ys] + [((-0.95 + 1.95 * f1) * interp(outer, y), y) for y in reversed(ys)]
        sh.shape(S.pts(pane), mix(MUR, LIN, 0.1 * (k % 2)), direction="r", light=0.28, sw=0.8)
    sh.flat(S.pts([(-0.2, 0.69), (0.1, 0.69), (0.2, 0.76), (-0.2, 0.76)]), "#000000", op=0.3)
    sh.close()
    sh.path(d, "none", INK, 1.1)
    # torso profile
    torso = S.pts([(0.125, 0.975), (0.145, 1.08), (0.14, 1.25), (0.125, 1.38), (0.08, 1.48), (0.05, 1.52), (-0.06, 1.52),
                   (-0.12, 1.42), (-0.125, 1.25), (-0.105, 1.10), (-0.12, 1.02), (0.0, 1.0)])
    d = sh.shape(torso, MUR, direction="r")
    sh.clip_open(d)
    for k in range(4):
        sh.line(S.pts([(-0.08 + k * 0.05, 1.42), (-0.07 + k * 0.05, 1.2), (-0.06 + k * 0.05, 1.03)]), dk(MUR, 0.3), 1.0, op=0.45)
    sh.close()
    for i in range(10):
        y = 1.43 - i * 0.042
        x = interp([(1.48, 0.08), (1.38, 0.125), (1.25, 0.14), (1.08, 0.145), (0.975, 0.125)], y) - 0.004
        sh.circle(*S.p(x, y), 1.8, STEEL, INK, 0.5)
    # sash (seen crossing)
    sh.shape(S.pts([(-0.06, 1.48), (0.04, 1.49), (0.13, 1.10), (0.03, 1.03), (-0.05, 1.3)]), dk(MUR, 0.1), direction="r", light=0.35)
    # belt
    sh.shape(S.pts([(-0.12, 1.045), (0.15, 1.045), (0.15, 1.005), (-0.12, 1.005)]), LEATH, smooth=False, direction="r")
    # lantern at back hip
    lantern(sh, S, -0.17, 0.99)
    # sash knot tail (left hip = near side)
    sh.shape(S.pts([(0.03, 1.02), (0.10, 1.0), (0.09, 0.74), (0.04, 0.73), (0.02, 0.85)]), dk(MUR, 0.15), direction="r")
    for j in range(6):
        sh.line([S.p(0.045 + j * 0.008, 0.745), S.p(0.042 + j * 0.009, 0.71)], lit(MUR, 0.15), 0.9, smooth=False)
    # near arm to hilt
    up = limb(S.p(-0.01, 1.44), 0.12 * S.s, S.p(-0.03, 1.19), 0.10 * S.s, bulge=0.12)
    d = sh.shape(up, MUR, direction="r")
    sh.clip_open(d)
    for k in range(4):
        fx = -0.04 + k * 0.026
        sh.line(S.pts([(fx, 1.41), (fx - 0.01, 1.30), (fx - 0.018, 1.22)]), LIN, 2.2, op=0.9)
    sh.close()
    sh.shape(limb(S.p(-0.02, 1.46), 0.08 * S.s, S.p(0.0, 1.41), 0.07 * S.s), MUR, direction="r", light=0.4)
    sh.shape(limb(S.p(-0.03, 1.19), 0.095 * S.s, S.p(0.07, 1.04), 0.075 * S.s), dk(MUR, 0.05), direction="r")
    sh.shape(limb(S.p(0.055, 1.065), 0.085 * S.s, S.p(0.075, 1.035), 0.08 * S.s), LINEN, direction="r", sw=0.8)
    # hilt + hand
    sh.line([S.p(0.02, 1.04), S.p(0.10, 1.12)], LEATH, 4, smooth=False)
    sh.line([S.p(0.06, 1.10), S.p(0.10, 1.02)], STEEL, 2.5, smooth=False)
    sh.line(S.pts([(0.03, 1.05), (0.07, 1.15), (0.12, 1.11)]), STEEL_HI, 1.4)
    sh.shape(S.pts([(0.06, 1.06), (0.12, 1.08), (0.13, 1.02), (0.07, 1.00)]), SKIN, direction="r", sw=0.9)
    # partisan upright, far hand holding it in front
    partisan(sh, S, 0.25)
    sh.shape(S.pts([(0.225, 1.08), (0.27, 1.085), (0.275, 1.00), (0.23, 0.995)]), dk(SKIN, 0.2), direction="r", sw=0.9)
    sh.shape(limb(S.p(0.10, 1.25), 0.08 * S.s, S.p(0.225, 1.05), 0.07 * S.s), dk(MUR, 0.3), direction="r")
    # neck, gorget, collar
    sh.shape(S.pts([(-0.05, 1.60), (0.045, 1.58), (0.05, 1.49), (-0.055, 1.49)]), dk(SKIN, 0.1), smooth=False, direction="r")
    sh.shape(S.pts([(-0.08, 1.47), (-0.05, 1.53), (0.06, 1.53), (0.11, 1.47), (0.09, 1.45), (-0.07, 1.45)]), STEEL, direction="r", light=0.45)
    sh.shape(S.pts([(-0.07, 1.52), (0.02, 1.545), (0.11, 1.525), (0.12, 1.48), (0.03, 1.47), (-0.06, 1.49)]), LINEN, direction="r", light=0.15)
    # head profile
    face = S.pts([(-0.06, 1.56), (0.0, 1.555), (0.06, 1.555), (0.09, 1.565), (0.098, 1.595), (0.105, 1.605), (0.098, 1.62), (0.125, 1.64),
                  (0.10, 1.665), (0.102, 1.69), (0.09, 1.73), (0.0, 1.75), (-0.09, 1.72), (-0.095, 1.64)])
    sh.shape(face, SKIN, direction="r", light=0.3)
    sh.ellipse(*S.p(-0.01, 1.66), 0.018 * S.s, 0.032 * S.s, dk(SKIN, 0.1), INK, 0.7)
    sh.ellipse(*S.p(0.07, 1.655), 0.012 * S.s, 0.006 * S.s, dk(SKIN, 0.55))
    sh.line([S.p(0.05, 1.68), S.p(0.095, 1.678)], HAIR, 1.8, smooth=False)
    sh.shape(S.pts([(0.06, 1.595), (0.11, 1.605), (0.085, 1.61)]), HAIR, sw=0.5)
    sh.shape(S.pts([(0.05, 1.585), (0.095, 1.575), (0.085, 1.53), (0.06, 1.56)]), HAIR, sw=0.5)
    sh.flat(S.pts([(-0.06, 1.56), (0.02, 1.57), (0.0, 1.72), (-0.09, 1.72), (-0.095, 1.64)]), "#000000", op=0.22)
    sh.shape(S.pts([(0.015, 1.725), (0.06, 1.725), (0.07, 1.60), (0.045, 1.585), (0.02, 1.65)]), STEEL, direction="r")
    sh.circle(*S.p(0.04, 1.70), 1.6, BRIGHT)
    # morion profile: skull, comb, boat brim
    sh.shape(S.pts([(-0.115, 1.72), (-0.10, 1.78), (-0.05, 1.81), (0.05, 1.81), (0.10, 1.78), (0.115, 1.72)]), STEEL, direction="r", light=0.45)
    comb = S.pts([(-0.11, 1.76), (-0.08, 1.815), (-0.03, 1.845), (0.03, 1.85), (0.08, 1.825), (0.11, 1.77), (0.09, 1.775), (0.03, 1.805), (-0.03, 1.805), (-0.09, 1.772)])
    sh.shape(comb, STEEL, direction="r", light=0.5)
    sh.line(S.pts([(-0.08, 1.812), (-0.03, 1.842), (0.03, 1.847), (0.08, 1.822)]), BRIGHT, 1.1, op=0.9)
    for k in range(9):  # roping
        x = -0.08 + k * 0.02
        y = 1.815 + 0.035 * math.cos((x) * 14)
        sh.line([S.p(x, y - 0.012), S.p(x + 0.008, y)], dk(STEEL, 0.4), 0.8, smooth=False)
    brim = S.pts([(-0.215, 1.80), (-0.16, 1.745), (-0.08, 1.718), (0, 1.712), (0.08, 1.718), (0.16, 1.745), (0.215, 1.80),
                  (0.205, 1.80), (0.15, 1.735), (0.08, 1.703), (0, 1.697), (-0.08, 1.703), (-0.15, 1.735), (-0.205, 1.80)])
    sh.shape(brim, STEEL, direction="r", light=0.5)
    sh.line(S.pts([(-0.21, 1.80), (-0.15, 1.745), (-0.08, 1.719), (0, 1.713), (0.08, 1.719), (0.15, 1.745), (0.21, 1.80)]), BRIGHT, 1.0, op=0.8)
    sh.rivets([S.p(x, 1.735) for x in (-0.09, -0.05, -0.01, 0.03, 0.07)], 1.6, STEEL_HI)
    sh.flecks(*S.p(-0.1, 1.80), *S.p(0.1, 1.74), 8, "#2E2923", 0.5, 1.2)


def build():
    sh = Sheet(seed=11)
    front(sh)
    side(sh)
    height_mark(sh, 560, 1.85, "1.85 m")
    height_mark(sh, 360, 2.62, "partisan 2.62 m")
    callouts(sh, [
        (820, 690 - 220 * 1.83, "MORION + COMB", "blued steel · brim 0.40 m"),
        (1.0 * 820 + 0.24 * 220, 690 - 220 * 2.45, "PARTISAN HEAD", "blued steel, etched · 0.42 m"),
        (820 + 0.25 * 220, 690 - 220 * 1.9, "PARTISAN HAFT", "ash 2.20 m · 32 mm"),
        (820 + 0.09 * 220, 690 - 220 * 1.50, "FALLING COLLAR", "linen over steel gorget"),
        (820 - 0.02 * 220, 690 - 220 * 1.32, "SLASHED SLEEVE", "murrey wool · 5 panes"),
        (820 + 0.07 * 220, 690 - 220 * 0.84, "SASH TAIL", "murrey silk · fringed"),
        (820 - 0.17 * 220, 690 - 220 * 0.88, "HORN LANTERN", "on belt hook · warm", 725, 420, "end"),
        (820 + 0.2 * 220, 690 - 220 * 0.95, "PANED TRUNK-HOSE", "murrey over lining"),
        (820 - 0.33 * 220, 690 - 220 * 0.43, "SCABBARD", "leather · steel chape", 725, 560, "end"),
        (820 + 0.1 * 220, 690 - 220 * 0.04, "LATCHET SHOES", "black leather · rosettes"),
    ], y0=150, dy=55)
    return sh.render("THE AGE OF POWDER · ENEMY · PATROL", "Palace Guard", "H 1.85 m · ≤ 8k tris · 2048²",
                     "1 m = 220 px · ground y 690", [(MUR, "murrey wool"), (STEEL, "blued steel"), (LINEN, "linen"),
                                                    (LEATH, "black leather"), (ASH, "ash haft"), (HORN, "horn pane"), (SKIN, "skin")],
                     view_labels=[(470, "FRONT"), (820, "SIDE")])
