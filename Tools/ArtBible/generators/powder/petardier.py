from lib import *

BRONZE = "#8A6A3E"; PATINA = "#4A3A26"; OAK = "#6A5034"; CANVAS = "#8A7E62"; LEATH = "#4A3628"; IRON = "#2F2E2C"
SKIN = "#8E6A52"; EMBER = "#C4542E"; POWDER = "#1E1C1A"; SHIRT = lit(CANVAS, 0.25); HAIR = dk(SKIN, 0.6)


def ember(sh, x, y, r=24):
    sh.glow(x, y, r, EMBER, 0.5)
    sh.circle(x, y, 2.6, EMBER)
    sh.circle(x, y, 1.1, lit(EMBER, 0.7))
    sh.line([(x, y - 3), (x + 3, y - 14), (x - 2, y - 26), (x + 3, y - 38)], "#8A857A", 1.2, op=0.35)


def grenado(sh, x, y, r, lit_fuse=False, shade=0.0):
    sh.circle(x, y, r, f"url(#{sh.form(dk(IRON, shade), 'sphere', 0.45)})", INK, 1)
    sh.flecks(x - r * 0.7, y - r * 0.7, x + r * 0.7, y + r * 0.7, 4, lit(IRON, 0.3), 0.5, 1.2, (0.3, 0.6))
    sh.rect(x - 2.2, y - r - 4, 4.4, 5, OAK, INK, 0.6)
    sh.line([(x, y - r - 4), (x + 2, y - r - 9), (x - 1, y - r - 13)], dk(CANVAS, 0.3), 1.3)
    if lit_fuse:
        ember(sh, x - 1, y - r - 13, 16)


def front(sh):
    F = Fig(470)
    sh.ellipse(470, 690, 105, 7, "#000000", op=0.45)
    # madrier behind, visible around the shoulders
    pl = F.pts([(-0.30, 1.05), (0.30, 1.05), (0.30, 1.65), (-0.30, 1.65)])
    sh.shape(pl, OAK, smooth=False, direction="h", light=0.25)
    for k in range(6):
        sh.line([F.p(-0.29, 1.10 + k * 0.1), F.p(0.29, 1.12 + k * 0.1)], dk(OAK, 0.35), 0.9, op=0.5, smooth=False)
    for y in (1.18, 1.52):
        sh.shape(F.pts([(-0.30, y + 0.03), (0.30, y + 0.03), (0.30, y), (-0.30, y)]), IRON, smooth=False, sw=0.8)
        sh.rivets([F.p(x, y + 0.015) for x in (-0.27, 0.27)], 1.5, lit(IRON, 0.4))
    sh.shape(F.pts([(-0.02, 1.65), (-0.03, 1.70), (0.0, 1.73), (0.03, 1.70), (0.02, 1.65)]), IRON)  # hook
    sh.flat(F.pts([(-0.30, 1.05), (0.30, 1.05), (0.30, 1.10), (-0.30, 1.10)]), POWDER, smooth=False, op=0.5)  # scorched edge
    # legs: gaiters, breeches
    for s in (-1, 1):
        sh.shape(F.pts([(s * 0.02, 0.78), (s * 0.03, 0.46), (s * 0.17, 0.46), (s * 0.20, 0.78)]), dk(CANVAS, 0.15), smooth=False, direction="h")
        g = F.pts([(s * 0.04, 0.46), (s * 0.04, 0.10), (s * 0.05, 0.07), (s * 0.155, 0.07), (s * 0.16, 0.12), (s * 0.165, 0.46)])
        sh.shape(g, CANVAS, direction="h", light=0.3)
        for k in range(5):
            sh.circle(*F.p(s * 0.15, 0.13 + k * 0.07), 1.6, IRON)
        sh.shape(F.pts([(s * 0.035, 0.08), (s * 0.175, 0.08), (s * 0.195, 0.02), (s * 0.18, 0.0), (s * 0.03, 0.0), (s * 0.025, 0.03)]), LEATH, direction="h", light=0.25)
        sh.shape(F.pts([(s * 0.03, 0.49), (s * 0.18, 0.49), (s * 0.18, 0.45), (s * 0.03, 0.45)]), LEATH, smooth=False)
    # jerkin (quilted)
    body = F.pts([(-0.21, 0.86), (0.21, 0.86), (0.20, 1.10), (0.215, 1.35), (0.20, 1.46), (0.10, 1.50), (-0.10, 1.50), (-0.20, 1.46), (-0.215, 1.35), (-0.20, 1.10)])
    d = sh.shape(body, CANVAS, direction="h", light=0.25)
    sh.clip_open(d)
    for k in range(12):
        x = -0.20 + k * 0.036
        sh.line([F.p(x, 1.50), F.p(x + 0.004, 1.2), F.p(x, 0.86)], dk(CANVAS, 0.35), 1.1, op=0.7)
    sh.flat(F.pts([(-0.22, 0.86), (0.22, 0.86), (0.22, 0.95), (-0.22, 0.97)]), POWDER, op=0.45)
    sh.flat(F.pts([(0.08, 0.86), (0.22, 0.86), (0.22, 1.5), (0.12, 1.5)]), "#000000", op=0.22)
    sh.flecks(*F.p(-0.2, 1.45), *F.p(0.2, 0.88), 30, POWDER, 0.8, 2.2, (0.2, 0.5))
    sh.close()
    # leather apron
    ap = F.pts([(-0.20, 1.02), (0.20, 1.02), (0.22, 0.52), (0.12, 0.49), (-0.12, 0.49), (-0.22, 0.52)])
    d = sh.shape(ap, LEATH, direction="h", light=0.3)
    sh.clip_open(d)
    sh.flecks(*F.p(-0.2, 1.0), *F.p(0.2, 0.5), 40, POWDER, 0.8, 2.4, (0.3, 0.8))
    sh.flat(F.pts([(-0.08, 0.80), (0.05, 0.78), (0.07, 0.62), (-0.1, 0.6)]), POWDER, op=0.35)  # powder spill
    sh.line(F.pts([(-0.05, 1.0), (-0.07, 0.75), (-0.06, 0.52)]), dk(LEATH, 0.5), 1.2, op=0.6)
    sh.line(F.pts([(0.09, 1.0), (0.10, 0.75), (0.11, 0.52)]), dk(LEATH, 0.5), 1.2, op=0.6)
    sh.close()
    # shoulder straps
    for s in (-1, 1):
        sh.shape(F.pts([(s * 0.07, 1.50), (s * 0.13, 1.50), (s * 0.15, 1.10), (s * 0.10, 1.10)]), LEATH, smooth=False, direction="h")
        sh.rect(*F.p(s * 0.125 - 0.025, 1.25), 0.05 * F.s, 0.035 * F.s, "none", IRON, 1.5)
    # belt with grenados
    sh.shape(F.pts([(-0.22, 1.06), (0.22, 1.06), (0.22, 1.01), (-0.22, 1.01)]), LEATH, smooth=False, direction="h")
    sh.rect(*F.p(-0.03, 1.065), 0.06 * F.s, 0.06 * F.s, "none", IRON, 1.6)
    for i, x in enumerate((-0.18, -0.09, 0.09, 0.18)):
        cx, cy = F.p(x, 0.97)
        sh.shape([(cx - 11, cy - 10), (cx + 11, cy - 10), (cx + 9, cy + 6), (cx - 9, cy + 6)], LEATH, smooth=False, sw=0.8)
        grenado(sh, cx, cy - 8, 0.045 * F.s)
    # linstock tucked at left hip (viewer right), lit end up
    sh.line([F.p(0.24, 0.80), F.p(0.30, 1.38)], OAK, 3.2, smooth=False)
    for k in range(6):
        y = 0.95 + k * 0.06
        x = 0.24 + (y - 0.80) / 0.58 * 0.06
        sh.ellipse(*F.p(x, y), 4.5, 2, dk(CANVAS, 0.2), INK, 0.5)
    ember(sh, *F.p(0.30, 1.39))
    # mallet at right hip (viewer left)
    sh.line([F.p(-0.24, 1.02), F.p(-0.27, 0.78)], OAK, 3, smooth=False)
    sh.shape(F.pts([(-0.31, 0.80), (-0.22, 0.79), (-0.22, 0.73), (-0.31, 0.74)]), OAK, smooth=False, direction="h")
    # arms: shirt sleeves rolled, sooted forearms
    def arm(s, el, hand):
        sh.shape(limb(F.p(s * 0.21, 1.44), 0.12 * F.s, F.p(*el), 0.11 * F.s, bulge=0.15), SHIRT, direction="h", light=0.2)
        sh.shape(limb(F.p(el[0], el[1] + 0.02), 0.12 * F.s, F.p(el[0], el[1] - 0.03), 0.11 * F.s), dk(SHIRT, 0.1), direction="h")
        sh.shape(limb(F.p(el[0], el[1] - 0.02), 0.085 * F.s, F.p(*hand), 0.07 * F.s), SKIN, direction="h")
        sh.flecks(*F.p(min(el[0], hand[0]) - 0.03, el[1]), *F.p(max(el[0], hand[0]) + 0.03, hand[1]), 10, POWDER, 0.8, 2, (0.3, 0.6))
        hx_, hy_ = hand
        sh.shape(F.pts([(hx_ - 0.04, hy_ + 0.02), (hx_ + 0.04, hy_ + 0.02), (hx_ + 0.035, hy_ - 0.07), (hx_ - 0.035, hy_ - 0.07)]), SKIN, direction="h", sw=0.9)
    arm(-1, (-0.29, 1.18), (-0.30, 0.95))
    arm(1, (0.29, 1.18), (0.28, 0.96))
    sh.flat(F.pts([(0.24, 0.98), (0.32, 0.98), (0.32, 0.9), (0.24, 0.9)]), POWDER, op=0.3)
    # neck + head (lowered, stooped)
    sh.shape(F.pts([(-0.055, 1.56), (0.055, 1.56), (0.06, 1.47), (-0.06, 1.47)]), dk(SKIN, 0.1), smooth=False, direction="h")
    face = F.pts([(0, 1.515), (0.055, 1.535), (0.08, 1.58), (0.083, 1.64), (0.07, 1.68), (0, 1.695), (-0.07, 1.68), (-0.083, 1.64), (-0.08, 1.58), (-0.055, 1.535)])
    sh.shape(face, SKIN, direction="h", light=0.25)
    sh.flecks(*F.p(-0.07, 1.68), *F.p(0.07, 1.54), 25, POWDER, 0.6, 1.8, (0.3, 0.7))
    for s in (-1, 1):
        sh.ellipse(*F.p(s * 0.032, 1.615), 0.013 * F.s, 0.006 * F.s, "#0E0C0A")
        sh.circle(*F.p(s * 0.030, 1.616), 1.1, "#DCD2BA", op=0.8)
    sh.line(F.pts([(-0.004, 1.625), (-0.012, 1.58), (0.006, 1.575)]), dk(SKIN, 0.45), 1.1)
    beard = F.pts([(-0.07, 1.585), (-0.05, 1.53), (0, 1.50), (0.05, 1.53), (0.07, 1.585), (0.03, 1.56), (0, 1.565), (-0.03, 1.56)])
    sh.shape(beard, HAIR, sw=0.6)
    sh.flecks(*F.p(-0.06, 1.58), *F.p(0.06, 1.51), 8, EMBER, 0.5, 1.0, (0.3, 0.5))  # singed
    # cap
    cap = F.pts([(-0.10, 1.655), (-0.095, 1.71), (-0.06, 1.75), (0, 1.76), (0.06, 1.75), (0.095, 1.71), (0.10, 1.655)])
    sh.shape(cap, LEATH, direction="h", light=0.3)
    sh.shape(F.pts([(-0.105, 1.68), (0.105, 1.68), (0.105, 1.645), (-0.105, 1.645)]), dk(LEATH, 0.1), smooth=False, direction="h")
    for s in (-1, 1):
        sh.shape(F.pts([(s * 0.10, 1.70), (s * 0.13, 1.72), (s * 0.12, 1.66), (s * 0.10, 1.65)]), LEATH, sw=0.8)
        sh.line(F.pts([(s * 0.12, 1.72), (s * 0.08, 1.75), (s * 0.02, 1.76)]), dk(LEATH, 0.3), 1.0)
    sh.flecks(*F.p(-0.09, 1.75), *F.p(0.09, 1.66), 10, POWDER, 0.6, 1.4)


def side(sh):
    S = Fig(820)
    sh.ellipse(810, 690, 95, 7, "#000000", op=0.45)
    lean = 0.14  # forward stoop at the shoulders
    def L(x, y):  # apply stoop: shift x forward proportional to height above hip
        return S.p(x + max(0.0, y - 0.95) / 0.8 * lean, y)
    def Lp(lst):
        return [L(x, y) for x, y in lst]
    # legs
    for off, shade in ((-0.04, 0.35), (0.04, 0.0)):
        sh.shape(S.pts([(-0.09 + off, 0.78), (-0.07 + off, 0.46), (0.08 + off, 0.46), (0.10 + off, 0.78)]), dk(dk(CANVAS, 0.15), shade), smooth=False, direction="r")
        g = S.pts([(-0.06 + off, 0.46), (-0.07 + off, 0.25), (-0.05 + off, 0.07), (0.07 + off, 0.07), (0.05 + off, 0.25), (0.065 + off, 0.46)])
        sh.shape(g, dk(CANVAS, shade), direction="r", light=0.3)
        sh.shape(S.pts([(-0.065 + off, 0.0), (-0.065 + off, 0.07), (0.07 + off, 0.085), (0.17 + off, 0.04), (0.18 + off, 0.0)]), dk(LEATH, shade * 0.5), direction="r", light=0.25)
        sh.shape(S.pts([(-0.08 + off, 0.49), (0.08 + off, 0.49), (0.08 + off, 0.45), (-0.08 + off, 0.45)]), LEATH, smooth=False)
    # madrier + petard on the back
    top, bot = 1.65, 1.05
    pl = Lp([(-0.19, bot), (-0.19, top), (-0.27, top), (-0.27, bot)])
    sh.shape(pl, OAK, smooth=False, direction="r")
    sh.path(poly_path(Lp([(-0.27, bot), (-0.19, bot), (-0.19, bot + 0.05), (-0.27, bot + 0.05)])), POWDER, op=0.5)
    sh.shape(Lp([(-0.22, top), (-0.24, top + 0.06), (-0.21, top + 0.08), (-0.20, top + 0.05)]), IRON)
    # bell: mouth on the plank, crown pointing back
    cy = 1.36
    bell = Lp([(-0.27, cy + 0.13), (-0.33, cy + 0.125), (-0.42, cy + 0.11), (-0.50, cy + 0.085), (-0.56, cy + 0.06), (-0.575, cy),
               (-0.56, cy - 0.06), (-0.50, cy - 0.085), (-0.42, cy - 0.11), (-0.33, cy - 0.125), (-0.27, cy - 0.13)])
    d = sh.shape(bell, BRONZE, direction="v", light=0.4, dark=0.55)
    sh.clip_open(d)
    for x in (-0.36, -0.47):
        sh.path(poly_path(Lp([(x, cy + 0.14), (x - 0.02, cy + 0.14), (x - 0.02, cy - 0.14), (x, cy - 0.14)])), PATINA, op=0.9)
        sh.line([L(x + 0.004, cy + 0.12), L(x + 0.004, cy - 0.12)], lit(BRONZE, 0.4), 1, op=0.7, smooth=False)
    sh.flecks(*L(-0.56, cy + 0.12), *L(-0.28, cy - 0.12), 18, PATINA, 0.8, 2.4, (0.3, 0.6))
    sh.close()
    sh.path(poly_path(Lp([(-0.27, cy + 0.135), (-0.29, cy + 0.135), (-0.29, cy - 0.135), (-0.27, cy - 0.135)])), lit(BRONZE, 0.35), INK, 0.8)  # rim
    tx, ty = L(-0.575, cy)
    sh.rect(tx - 7, ty - 3, 7, 6, BRONZE, INK, 0.6)  # touch-hole boss
    sh.line([(tx - 7, ty), (tx - 14, ty - 6), (tx - 12, ty - 14)], dk(CANVAS, 0.3), 1.4)
    for y in (cy + 0.10, cy - 0.10):  # iron straps
        sh.line([L(-0.27, y + 0.02), L(-0.40, y * 0 + cy + (y - cy) * 0.75)], IRON, 3, smooth=False)
        sh.rivets([L(-0.40, cy + (y - cy) * 0.75)], 1.8, lit(IRON, 0.4))
    for y in (1.18, 1.52):
        sh.path(poly_path(Lp([(-0.19, y + 0.03), (-0.275, y + 0.03), (-0.275, y), (-0.19, y)])), IRON, INK, 0.8)
    # torso (stooped) + jerkin
    body = Lp([(-0.15, 0.86), (0.15, 0.86), (0.14, 1.10), (0.15, 1.30), (0.12, 1.44), (0.05, 1.50), (-0.08, 1.50), (-0.17, 1.40), (-0.18, 1.15)])
    d = sh.shape(body, CANVAS, direction="r", light=0.25)
    sh.clip_open(d)
    for k in range(8):
        x = -0.16 + k * 0.04
        sh.line([L(x, 1.50), L(x, 1.2), L(x, 0.86)], dk(CANVAS, 0.35), 1.1, op=0.7)
    sh.path(poly_path(Lp([(-0.2, 0.86), (0.2, 0.86), (0.2, 0.95), (-0.2, 0.96)])), POWDER, op=0.45)
    sh.path(poly_path(Lp([(-0.2, 0.86), (-0.04, 0.86), (-0.06, 1.5), (-0.2, 1.5)])), "#000000", op=0.22)
    sh.close()
    # apron front
    ap = Lp([(0.13, 1.02), (0.17, 1.00), (0.19, 0.52), (0.10, 0.49), (0.08, 0.75)])
    d = sh.shape(ap, LEATH, direction="r", light=0.3)
    sh.clip_open(d)
    sh.flecks(*L(0.07, 1.02), *L(0.2, 0.5), 18, POWDER, 0.8, 2.2, (0.3, 0.8))
    sh.close()
    # strap over shoulder to plank
    sh.shape(Lp([(0.06, 1.50), (0.10, 1.47), (-0.02, 1.12), (-0.19, 1.10), (-0.19, 1.15), (-0.06, 1.17)]), LEATH, direction="r")
    # belt + grenados on near side
    sh.shape(Lp([(-0.17, 1.06), (0.16, 1.06), (0.16, 1.01), (-0.17, 1.01)]), LEATH, smooth=False, direction="r")
    for i, x in enumerate((-0.12, -0.03, 0.06)):
        cx, cy2 = S.p(x, 0.97)
        sh.shape([(cx - 11, cy2 - 10), (cx + 11, cy2 - 10), (cx + 9, cy2 + 6), (cx - 9, cy2 + 6)], LEATH, smooth=False, sw=0.8)
        grenado(sh, cx, cy2 - 8, 0.045 * S.s)
    # fuse coil + flask
    sh.ellipse(*S.p(-0.16, 0.90), 12, 9, "none", dk(CANVAS, 0.3), 2.2)
    sh.ellipse(*S.p(-0.16, 0.90), 7, 5, "none", dk(CANVAS, 0.3), 2.0)
    # near arm holding a lit grenado forward
    sh.shape(limb(L(0.0, 1.44), 0.12 * S.s, L(0.03, 1.18), 0.11 * S.s, bulge=0.15), SHIRT, direction="r", light=0.2)
    sh.shape(limb(L(0.03, 1.20), 0.12 * S.s, L(0.035, 1.15), 0.11 * S.s), dk(SHIRT, 0.1), direction="r")
    sh.shape(limb(L(0.035, 1.16), 0.085 * S.s, S.p(0.30, 1.13), 0.07 * S.s), SKIN, direction="r")
    sh.flecks(*L(0.03, 1.19), *S.p(0.3, 1.1), 8, POWDER, 0.8, 2, (0.3, 0.6))
    sh.shape(S.pts([(0.28, 1.17), (0.35, 1.17), (0.36, 1.09), (0.29, 1.08)]), SKIN, direction="r", sw=0.9)
    grenado(sh, *S.p(0.35, 1.20), 0.045 * S.s, lit_fuse=True)
    sh.shape(S.pts([(0.31, 1.18), (0.37, 1.16), (0.36, 1.12), (0.31, 1.13)]), SKIN, direction="r", sw=0.8)
    # linstock in belt behind hip
    sh.line([S.p(-0.08, 0.82), S.p(-0.03, 1.40)], OAK, 3.2, smooth=False)
    ember(sh, *S.p(-0.03, 1.41))
    # neck + head (forward)
    hx = lean + 0.02
    sh.shape(S.pts([(-0.05 + hx, 1.55), (0.05 + hx, 1.53), (0.05 + hx * 0.8, 1.46), (-0.06 + hx * 0.8, 1.46)]), dk(SKIN, 0.1), smooth=False, direction="r")
    face = S.pts([(-0.06 + hx, 1.52), (0.06 + hx, 1.515), (0.09 + hx, 1.525), (0.098 + hx, 1.555), (0.105 + hx, 1.565), (0.098 + hx, 1.58), (0.125 + hx, 1.60),
                  (0.10 + hx, 1.625), (0.102 + hx, 1.65), (0.09 + hx, 1.69), (0.0 + hx, 1.705), (-0.09 + hx, 1.68), (-0.095 + hx, 1.60)])
    sh.shape(face, SKIN, direction="r", light=0.3)
    sh.flecks(*S.p(hx - 0.05, 1.69), *S.p(hx + 0.12, 1.52), 18, POWDER, 0.6, 1.8, (0.3, 0.7))
    sh.ellipse(*S.p(hx + 0.07, 1.615), 0.012 * S.s, 0.006 * S.s, "#0E0C0A")
    sh.shape(S.pts([(hx + 0.03, 1.555), (hx + 0.10, 1.55), (hx + 0.095, 1.50), (hx + 0.06, 1.49), (hx + 0.02, 1.52)]), HAIR, sw=0.5)
    sh.ellipse(*S.p(hx - 0.01, 1.62), 0.016 * S.s, 0.03 * S.s, dk(SKIN, 0.1), INK, 0.7)
    sh.flat(S.pts([(hx - 0.06, 1.52), (hx + 0.02, 1.53), (hx, 1.68), (hx - 0.09, 1.68)]), "#000000", op=0.2)
    cap = S.pts([(hx - 0.105, 1.65), (hx - 0.10, 1.71), (hx - 0.05, 1.755), (hx + 0.02, 1.76), (hx + 0.08, 1.735), (hx + 0.11, 1.68), (hx + 0.105, 1.645)])
    sh.shape(cap, LEATH, direction="r", light=0.3)
    sh.shape(S.pts([(hx - 0.11, 1.675), (hx + 0.11, 1.675), (hx + 0.11, 1.64), (hx - 0.11, 1.64)]), dk(LEATH, 0.1), smooth=False, direction="r")
    sh.shape(S.pts([(hx - 0.03, 1.70), (hx + 0.01, 1.72), (hx + 0.02, 1.66), (hx - 0.02, 1.65)]), LEATH, sw=0.8)


def build():
    sh = Sheet(seed=14)
    front(sh)
    side(sh)
    height_mark(sh, 580, 1.76, "1.76 m (stooped)", 206)
    height_mark(sh, 360, 1.65, "plank top 1.65 m", 206)
    callouts(sh, [
        (820 + 0.16 * 220, 690 - 220 * 1.73, "SAPPER'S CAP", "scorched leather · flaps tied"),
        (820 - 0.52 * 220, 690 - 220 * 1.40, "BRONZE PETARD", "bell 0.30 × 0.26 m · touch-hole", 725, 250, "end"),
        (820 - 0.24 * 220, 690 - 220 * 1.62, "MADRIER", "oak 0.60 × 0.60 × 0.08 m", 725, 190, "end"),
        (820 - 0.02 * 220, 690 - 220 * 1.41, "LINSTOCK, LIT", "ash 0.60 m · slow match"),
        (820 + 0.35 * 220, 690 - 220 * 1.25, "GRENADO, LIT", "cast iron 0.09 m · 3 s fuse"),
        (820 + 0.05 * 220, 690 - 220 * 1.30, "QUILTED JERKIN", "padded canvas · sooted hem"),
        (820 - 0.03 * 220, 690 - 220 * 0.94, "GRENADO BELT ×5", "leather cups"),
        (820 + 0.17 * 220, 690 - 220 * 0.75, "LEATHER APRON", "spark-pitted · powder spill"),
        (820 - 0.16 * 220, 690 - 220 * 0.90, "FUSE COIL", "spare cord", 725, 480, "end"),
        (820 + 0.05 * 220, 690 - 220 * 0.30, "CANVAS GAITERS", "no hob-nails (magazine rule)"),
    ], y0=150, dy=55)
    return sh.render("THE AGE OF POWDER · ENEMY · SPECIAL", "Petardier", "H 1.76 m · ≤ 8k tris · 2048²",
                     "1 m = 220 px · ground y 690", [(BRONZE, "bronze"), (OAK, "oak"), (CANVAS, "canvas"),
                                                    (LEATH, "leather"), (IRON, "cast iron"), (SKIN, "sooted skin"), (EMBER, "fuse ember")],
                     view_labels=[(470, "FRONT"), (820, "SIDE")])
