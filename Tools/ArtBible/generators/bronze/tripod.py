from lib import *

BRZ, RUB, SHAD, SOOT, ASHR = "#9B6A38", "#B8844E", "#5E3E22", "#2B231B", "#6E665A"

BOWL = [(0.52, 0.02), (0.535, 0.12), (0.57, 0.20), (0.63, 0.265), (0.71, 0.31), (0.80, 0.33), (0.86, 0.332),
        (0.88, 0.335)]


def tripod(sh, cx, gy, k, e=18, yaw=25, detail=True):
    ce, se = math.cos(math.radians(e)), math.sin(math.radians(e))

    def S(phi, r, z):
        a = math.radians(phi + yaw)
        return (cx + math.sin(a) * r * k, gy - z * k * ce + math.cos(a) * r * k * se)

    def front(phi):
        return math.cos(math.radians(phi + yaw)) > -0.05

    def leg(phi):
        top, knee, foot = S(phi, 0.28, 0.70), S(phi, 0.36, 0.30), S(phi, 0.40, 0.0)
        w = 0.06 * k * (0.35 + 0.65 * abs(math.cos(math.radians(phi + yaw))))
        d = smooth_path([top, knee, foot], closed=False, tension=.3)
        sh.path(d, "none", darken(SHAD, .3), w + 2.4)
        sh.path(d, "none", BRZ if front(phi) else darken(BRZ, .3), w)
        sh.path(d, "none", RUB, max(1.2, w * .22), op=.8)
        if detail:
            # incised zigzag down the strip
            zz = []
            for i in range(14):
                t = i / 13
                x = top[0] + (foot[0] - top[0]) * t + (w * .25 if i % 2 else -w * .25)
                y = top[1] + (foot[1] - top[1]) * t
                zz.append((x, y))
            sh.path(poly_path(zz, False), "none", SHAD, 1, op=.8)
        # paw foot
        sh.ellipse(foot[0], foot[1], w * .9, 0.025 * k, BRZ, darken(SHAD, .3), 1.2)
        for j in (-1, 0, 1):
            sh.circle(foot[0] + j * w * .45, foot[1] + 0.012 * k, 0.012 * k, RUB, darken(SHAD, .3), .6)
        # attachment plate + rivets + bull protome
        px, py = S(phi, 0.30, 0.72)
        sh.path(poly_path([(px - w * .9, py - 0.07 * k), (px + w * .9, py - 0.07 * k), (px + w * .9, py + 0.07 * k),
                           (px - w * .9, py + 0.07 * k)]), f"url(#{sh.lin(BRZ, 'h', .35, .5)})", darken(SHAD, .3), 1)
        for j in range(3):
            sh.circle(px, py - 0.045 * k + j * 0.045 * k, 0.008 * k + .6, RUB, darken(SHAD, .3), .6)
        if detail and front(phi):
            hx, hy = px, py - 0.10 * k
            sh.ellipse(hx, hy, 0.025 * k, 0.02 * k, BRZ, darken(SHAD, .3), 1)
            sh.path(f"M{f(hx - 0.03 * k)} {f(hy - 0.02 * k)} q{f(-0.01 * k)} {f(-0.03 * k)} {f(0.005 * k)} {f(-0.02 * k)}"
                    f" M{f(hx + 0.03 * k)} {f(hy - 0.02 * k)} q{f(0.01 * k)} {f(-0.03 * k)} {f(-0.005 * k)} {f(-0.02 * k)}",
                    "none", RUB, 2)
        return foot

    def handle(phi):
        c = S(phi, 0.335, 0.94)
        rx = 0.12 * k * max(0.18, abs(math.cos(math.radians(phi + yaw))))
        sh.ellipse(c[0], c[1], rx, 0.12 * k, "none", darken(SHAD, .3), 0.025 * k + 2.4)
        sh.ellipse(c[0], c[1], rx, 0.12 * k, "none", BRZ, 0.025 * k)
        if detail:
            for t in range(18):
                a = t * math.pi / 9
                x, y = c[0] + rx * math.cos(a), c[1] + 0.12 * k * math.sin(a)
                sh.line(x - 2, y - 2, x + 2, y + 2, SHAD, 1)
        sh.path(f"M{f(c[0] - rx)} {f(c[1])} A{f(rx)} {f(0.12 * k)} 0 0 1 {f(c[0] + rx)} {f(c[1])}", "none", RUB, 1.4, op=.9)
        return (c[0] - rx if c[0] < cx else c[0] + rx, c[1])

    legs = [0, 120, 240]
    feet = {}
    hands = {}
    for phi in legs:
        if not front(phi):
            feet[phi] = leg(phi)
    for phi in (90, 270):
        if not front(phi):
            hands[phi] = handle(phi)
    # struts
    sp = [S(p, 0.37, 0.30) for p in legs]
    for i in range(3):
        sh.line(*sp[i], *sp[(i + 1) % 3], darken(BRZ, .2), 0.012 * k + .5)
    # bowl
    d, Y, _ = revolve(sh, cx, gy, k, BOWL, BRZ, e, light=.4, dark=.55, sw=1.6)
    gid = sh.rad([(0, SOOT, .95), (0.6, SOOT, .6), (1, SOOT, 0)], cy=1.0, r=.8)
    sh.clipped(d, f'<ellipse cx="{f(cx)}" cy="{f(Y(0.52))}" rx="{f(0.36 * k)}" ry="{f(0.22 * k)}" fill="url(#{gid})"/>')
    # rim ellipse (interior)
    rr = 0.335 * k
    sh.ellipse(cx, Y(0.88), rr, rr * se, f"url(#{sh.lin(SHAD, 'v', .1, .6)})", darken(SHAD, .4), 1.4)
    sh.ellipse(cx, Y(0.88) + rr * se * .35, rr * .7, rr * se * .45, ASHR, op=.6)
    sh.path(f"M{f(cx - rr)} {f(Y(0.88))} A{f(rr)} {f(rr * se)} 0 0 0 {f(cx + rr)} {f(Y(0.88))}", "none", RUB, 3, op=.9)
    if detail:
        # chased running-spiral band under the rim (front half)
        for i in range(22):
            t = math.pi * (i + .5) / 22
            x = cx - math.cos(t) * 0.325 * k
            y = Y(0.845) + math.sin(t) * 0.325 * k * se
            r = 0.012 * k * (0.4 + 0.6 * math.sin(t))
            sh.path(f"M{f(x - r)} {f(y)} a{f(r)} {f(r)} 0 1 1 {f(r)} {f(r * .6)} a{f(r * .5)} {f(r * .5)} 0 1 1 {f(-r * .4)} {f(-r * .6)}",
                    "none", SHAD, 1.1)
        sh.path(f"M{f(cx - rr * .97)} {f(Y(0.82))} A{f(rr * .97)} {f(rr * .97 * se)} 0 0 0 {f(cx + rr * .97)} {f(Y(0.82))}", "none", SHAD, 1)
        # dents: raised highlight + shadow pairs
        for (x, z) in ((-0.15, 0.66), (0.10, 0.60), (0.2, 0.74)):
            px, py = cx + x * k, Y(z)
            sh.ellipse(px, py, 0.03 * k, 0.018 * k, darken(BRZ, .3), op=.6)
            sh.path(f"M{f(px - 0.03 * k)} {f(py + 2)} q{f(0.03 * k)} {f(0.02 * k)} {f(0.06 * k)} 0", "none", RUB, 1.5, op=.8)
        sh.flecks(d, (cx - rr, Y(0.88), cx + rr, Y(0.52)), 90, SOOT, .5, 1.6, .5)
        sh.flecks(d, (cx - rr, Y(0.88), cx + rr, Y(0.52)), 40, RUB, .4, 1.1, .6)
    for phi in legs:
        if front(phi):
            feet[phi] = leg(phi)
    for phi in (90, 270):
        if front(phi):
            hands[phi] = handle(phi)
    return feet, hands, Y, d


def build():
    sh = item_sheet("Tripod Cauldron", 650, 12, "0.80 × 0.80 × 1.05 m", "≤ 5k tris (dual carry) · 1024²",
                    "hero 1 m = 420 px · orthos 1 m = 200 px",
                    [("cast bronze", BRZ), ("rubbed bronze", RUB), ("bronze shadow", SHAD), ("hearth soot", SOOT),
                     ("ash residue", ASHR)], seed=91)
    cx, gy, k = 370, 640, 420
    gid = sh.rad([(0, "#2A2012", .9), (1, "#14120E", 0)])
    sh.back.append(f'<ellipse cx="{cx}" cy="440" rx="330" ry="290" fill="url(#{gid})"/>')
    sh.ellipse(cx + 10, gy + 8, 220, 26, "#0E0C09", op=.6)
    feet, hands, Y, d = tripod(sh, cx, gy, k)
    # two-carrier grabs: ring handles + nearest legs
    for phi, h in hands.items():
        if h[0] < cx:
            grab(sh, *h, h[0] - 60, h[1] - 50, "GRAB · CARRIER A")
        else:
            grab(sh, *h, h[0] + 45, h[1] + 75, "GRAB · CARRIER B")
    # leg grab (the other hand) on the nearest leg
    lp = (cx + math.sin(math.radians(25)) * 0.34 * k, gy - 0.40 * k * math.cos(math.radians(18)) + math.cos(math.radians(25)) * 0.34 * k * math.sin(math.radians(18)))
    grab(sh, *lp, lp[0] + 60, lp[1] + 30, "GRAB (LEG)")
    # orthos
    tripod(sh, 830, 690, 200, e=0.001, yaw=0, detail=False)
    tripod(sh, 1060, 690, 200, e=0.001, yaw=90, detail=False)
    sh.line(830 - 0.40 * 200, 696, 830 + 0.40 * 200, 696, "#635C4C", .8)
    sh.text(830, 708, "0.80", 10, "#635C4C", "middle")
    sh.line(1150, 690, 1150, 690 - 1.05 * 200, "#635C4C", .8)
    sh.text(1156, 690 - 0.52 * 200, "1.05", 10, "#635C4C")
    view_label(sh, 370, "THREE-QUARTER")
    view_label(sh, 830, "FRONT")
    view_label(sh, 1060, "SIDE")
    scale_bar(sh, 100, 706, 210, "0.5 m = 210 px (hero)")
    sh.callouts([
        (hands.get(270, (cx, 200))[0] + 20, hands.get(270, (cx, 200))[1] - 40, "RING HANDLE ×2", "0.24 m, twisted rope"),
        (cx + 0.1 * k, Y(0.83), "SPIRAL BAND", "chased, 0.04 m tall"),
        (cx - 0.15 * k, Y(0.66), "HAMMERED BOWL", "0.66 × 0.36 m, dent normals"),
        (cx + 0.05 * k, Y(0.55), "SOOTED BELLY", "fire-scorch, multiply 80%"),
        (*feet[0], "CAST LEG ×3", "0.06 × 0.02 strip, paw foot"),
        (cx - 0.02 * k, gy - 0.30 * k * .95, "BRACING STRUTS", "0.012 m rods at 0.30 m")], 700, 150, 410, slope=0.5)
    sh.text(1060, 300, "UNBREAKABLE · 999", 12, MADDER, "middle", ls="2")
    sh.text(1060, 316, "dents (3 states) · rings 18 m", 10, "#9A9078", "middle")
    return sh
