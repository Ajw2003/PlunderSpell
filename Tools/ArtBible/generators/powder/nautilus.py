from lib import *

NACRE = "#D8CFC0"; GILT = "#C9A227"; SIL = "#B8B4A8"; TARN = "#6A665C"; STRIPE = "#8A6A4E"
K = math.log(3) / (2 * math.pi)


def foot_and_stem(sh, cx, gy, s, narrow=False):
    fw = 0.055 * s
    foot = [(cx - fw, gy), (cx - fw * 0.95, gy - 0.012 * s), (cx - fw * 0.5, gy - 0.028 * s), (cx - 0.012 * s, gy - 0.034 * s),
            (cx + 0.012 * s, gy - 0.034 * s), (cx + fw * 0.5, gy - 0.028 * s), (cx + fw * 0.95, gy - 0.012 * s), (cx + fw, gy)]
    sh.path(smooth_path(foot), f"url(#{sh.form(GILT, 'h', 0.45)})", INK, 1)
    for k in range(9):  # embossed waves
        x = cx - fw * 0.8 + k * fw * 0.2
        sh.path(f"M{fmt(x)} {fmt(gy - 0.01 * s)} q {fmt(fw * 0.05)} {fmt(-0.008 * s)} {fmt(fw * 0.1)} 0", "none", TARN, 0.9, op=0.7)
    sh.line([(cx - fw * 0.98, gy - 0.004 * s), (cx + fw * 0.98, gy - 0.004 * s)], SIL, 1.2, op=0.8, smooth=False)  # rubbed edge
    # triton on sea-monster (stem) 0.03 - 0.12
    b, t = gy - 0.034 * s, gy - 0.12 * s
    w = 0.018 * s
    monster = [(cx - w * 1.4, b), (cx - w * 1.8, b - (b - t) * 0.25), (cx - w * 0.6, b - (b - t) * 0.45), (cx - w * 1.5, b - (b - t) * 0.7),
               (cx - w * 0.5, t), (cx + w * 0.6, t), (cx + w * 1.2, b - (b - t) * 0.6), (cx + w * 0.4, b - (b - t) * 0.35), (cx + w * 1.6, b - (b - t) * 0.12), (cx + w * 1.2, b)]
    sh.path(smooth_path(monster), f"url(#{sh.form(SIL, 'h', 0.45)})", INK, 1)
    sh.path(smooth_path([(cx - w * 1.2, b - (b - t) * 0.3), (cx - w * 0.2, b - (b - t) * 0.5), (cx - w * 0.9, b - (b - t) * 0.72)], closed=False), "none", GILT, 1.6)
    sh.circle(cx + w * 0.3, b - (b - t) * 0.82, w * 0.35, f"url(#{sh.form(SIL, 'sphere', 0.5)})", INK, 0.7)  # triton head
    for k in range(4):
        sh.circle(cx - w * 0.9 + k * w * 0.5, b - (b - t) * (0.2 + 0.1 * k), 1.1, TARN)
    # foot-cup under the shell
    cup = [(cx - 0.03 * s, t - 0.02 * s), (cx + 0.03 * s, t - 0.02 * s), (cx + 0.012 * s, t + 0.004 * s), (cx - 0.012 * s, t + 0.004 * s)]
    sh.path(smooth_path(cup), f"url(#{sh.form(GILT, 'h', 0.45)})", INK, 1)
    return t - 0.02 * s


def shell_side(sh, C, re, theta_e, fracture_on=False, rim=True, s=1000):
    """Draw nautilus in side view. C centre px, re outer radius px at aperture, theta_e aperture angle (rad)."""
    def P(th, frac=1.0):
        r = re * math.exp(K * (th - theta_e)) * frac
        return (C[0] + r * math.cos(th), C[1] - r * math.sin(th))
    outer = [P(theta_e - 2 * math.pi + i * 2 * math.pi / 60) for i in range(61)]
    d = smooth_path(outer, closed=True)
    g = sh.rad([(0, "#EEE8DC"), (0.35, NACRE), (0.75, mix(NACRE, SIL, 0.5)), (1, mix(NACRE, TARN, 0.55))], 0.42, 0.4, 0.7, 0.35, 0.3)
    sh.path(d, f"url(#{g})", INK, 1.3)
    sh.clip_open(d)
    # iridescent sheen (nacre + silver + gilt mixes)
    sh.ellipse(C[0] - re * 0.2, C[1] - re * 0.25, re * 0.55, re * 0.3, mix(NACRE, GILT, 0.25), op=0.18, rot=-30)
    sh.ellipse(C[0] + re * 0.1, C[1] + re * 0.35, re * 0.6, re * 0.25, mix(NACRE, SIL, 0.6), op=0.25, rot=15)
    # growth lines (sigmoid from inner whorl to outer edge)
    for i in range(22):
        th = theta_e - 2 * math.pi + 0.15 + i * (2 * math.pi - 0.3) / 22
        a, b = P(th, 1 / 3), P(th, 1.0)
        m1 = P(th + 0.10, 0.55)
        m2 = P(th - 0.06, 0.8)
        sh.line([a, m1, m2, b], mix(NACRE, TARN, 0.35), 0.8, op=0.55)
    # tiger stripe band near the aperture (outer skin left on)
    for i in range(4):
        th = theta_e - 0.25 - i * 0.22
        a, b = P(th, 0.72), P(th, 1.02)
        sh.line([a, P(th + 0.05, 0.85), b], STRIPE, 3.2, op=0.45)
    # umbilicus shadow
    sh.circle(C[0], C[1], re * 0.2, mix(NACRE, TARN, 0.6), op=0.55)
    sh.circle(C[0] - re * 0.03, C[1] - re * 0.03, re * 0.1, TARN, op=0.6)
    sh.close()
    # aperture segment + rim mount
    a_in, a_out = P(theta_e - 2 * math.pi, 1.0), P(theta_e, 1.0)
    if rim:
        sh.line([a_in, a_out], INK, 0.015 * s + 2, smooth=False)
        sh.line([a_in, a_out], f"url(#{sh.form(GILT, 'v', 0.5)})", 0.015 * s, smooth=False)
        n = 12
        ux, uy = (a_out[0] - a_in[0]) / n, (a_out[1] - a_in[1]) / n
        L = math.hypot(ux, uy) or 1
        nx, ny = uy / L, -ux / L
        if ny > 0:
            nx, ny = -nx, -ny
        for k in range(n):
            x0, y0 = a_in[0] + ux * k, a_in[1] + uy * k
            x1, y1 = x0 + ux, y0 + uy
            mx, my = (x0 + x1) / 2 + nx * (L * 0.7 + 0.008 * s), (y0 + y1) / 2 + ny * (L * 0.7 + 0.008 * s)
            sh.path(smooth_path([(x0, y0), (mx, my), (x1, y1)]), GILT, INK, 0.6)
    if fracture_on:
        for th in (theta_e - 1.2, theta_e - 2.7, theta_e - 4.2):
            fracture(sh, [P(th, 0.4), P(th + 0.2, 0.7), P(th - 0.1, 1.0)])
    return P


def straps(sh, P, theta_e, base):
    for th in (theta_e - 1.25, theta_e - 3.45):
        pts = [base, P(th + 0.12, 0.62), P(th + 0.02, 0.85), P(th - 0.02, 1.03)]
        sh.line(pts, INK, 7)
        sh.line(pts, f"url(#{sh.form(GILT, 'v', 0.5)})", 5)
        cx, cy = P(th + 0.06, 0.8)
        sh.ellipse(cx, cy, 7, 5, f"url(#{sh.form(GILT, 'sphere', 0.5)})", INK, 0.8)  # hinged cartouche


def neptune(sh, x, y, s):
    h = 0.06 * s
    body = [(x - h * 0.12, y), (x - h * 0.2, y - h * 0.4), (x - h * 0.12, y - h * 0.7), (x - h * 0.06, y - h * 0.8), (x + h * 0.08, y - h * 0.78),
            (x + h * 0.14, y - h * 0.6), (x + h * 0.2, y - h * 0.35), (x + h * 0.14, y)]
    sh.path(smooth_path(body), f"url(#{sh.form(GILT, 'h', 0.5)})", INK, 0.9)
    sh.circle(x, y - h * 0.88, h * 0.1, f"url(#{sh.form(GILT, 'sphere', 0.5)})", INK, 0.8)
    sh.line([(x + h * 0.22, y + h * 0.05), (x + h * 0.26, y - h * 1.05)], dk(GILT, 0.2), 1.6, smooth=False)
    for dx in (-0.06, 0, 0.06):
        sh.line([(x + h * 0.26 + dx * h, y - h * 1.05), (x + h * 0.26 + dx * h * 1.3, y - h * 1.15)], dk(GILT, 0.2), 1.3, smooth=False)
    sh.line([(x + h * 0.1, y - h * 0.62), (x + h * 0.25, y - h * 0.55)], dk(GILT, 0.2), 2, smooth=False)


def hero(sh):
    s = 1150
    cx, gy = 380, 672
    sh.ellipse(cx, gy + 4, 140, 10, "#000000", op=0.45)
    top_cup = foot_and_stem(sh, cx, gy, s)
    re = 0.114 * s
    theta_e = math.radians(28)
    C = (cx - re * 0.25, top_cup - re * 0.72)
    P = shell_side(sh, C, re, theta_e, fracture_on=True, s=s)
    straps(sh, P, theta_e, (cx, top_cup + 2))
    # aperture opening as a dark ellipse hint (3/4)
    a_in, a_out = P(theta_e - 2 * math.pi), P(theta_e)
    vx, vy = P(math.radians(118) - 2 * math.pi)
    neptune(sh, vx + 6, vy + 4, s)
    return cx, gy, top_cup, P, theta_e, s


def front_ortho(sh):
    s = 900
    cx, gy = 850, 690
    sh.ellipse(cx, gy, 60, 4, "#000000", op=0.4)
    top_cup = foot_and_stem(sh, cx, gy, s)
    re = 0.114 * s
    theta_e = math.radians(28)
    C = (cx - re * 0.25, top_cup - re * 0.72)
    P = shell_side(sh, C, re, theta_e, s=s)
    straps(sh, P, theta_e, (cx, top_cup + 2))
    vx, vy = P(math.radians(118) - 2 * math.pi)
    neptune(sh, vx + 6, vy + 4, s)
    grab(sh, cx, gy - 0.08 * s, "", 0, 0)


def side_ortho(sh):
    s = 900
    cx, gy = 1085, 690
    sh.ellipse(cx, gy, 55, 4, "#000000", op=0.4)
    top_cup = foot_and_stem(sh, cx, gy, s)
    w, h = 0.06 * s, 0.155 * s
    yb = top_cup + 4
    body = [(cx, yb), (cx - w * 0.7, yb - h * 0.15), (cx - w, yb - h * 0.5), (cx - w * 0.9, yb - h * 0.85), (cx - w * 0.5, yb - h), (cx + w * 0.5, yb - h),
            (cx + w * 0.9, yb - h * 0.85), (cx + w, yb - h * 0.5), (cx + w * 0.7, yb - h * 0.15)]
    sh.path(smooth_path(body), f"url(#{sh.form(NACRE, 'h', 0.3, 0.45)})", INK, 1.2)
    for k in range(-3, 4):
        sh.line([(cx + k * w * 0.25, yb - 4), (cx + k * w * 0.32, yb - h * 0.5), (cx + k * w * 0.25, yb - h + 4)], mix(NACRE, TARN, 0.35), 0.8, op=0.5)
    sh.ellipse(cx, yb - h * 0.93, w * 0.75, 0.012 * s, "#2E2A24", INK, 1)  # aperture seen end-on
    sh.ellipse(cx, yb - h * 0.93, w * 0.8, 0.014 * s, "none", GILT, 4)
    for dx in (-0.55, 0.0, 0.55):
        sh.line([(cx + dx * w * 0.6, yb), (cx + dx * w * 1.05, yb - h * 0.5), (cx + dx * w * 0.85, yb - h * 0.92)], INK, 6)
        sh.line([(cx + dx * w * 0.6, yb), (cx + dx * w * 1.05, yb - h * 0.5), (cx + dx * w * 0.85, yb - h * 0.92)], GILT, 4)
    neptune(sh, cx - 4, yb - h * 0.98, s)
    grab(sh, cx, gy - 0.08 * s, "", 0, 0)
    sh.text(cx + 50, gy - 0.05 * s, "0.12 m", 10, FRAME_TXT)


def build():
    sh = Sheet(seed=25)
    cx, gy, top_cup, P, th_e, s = hero(sh)
    front_ortho(sh)
    side_ortho(sh)
    grab(sh, cx + 4, gy - 0.08 * s, "GRAB · STEM ONLY", 16, 4)
    scale_bar(sh, 700, 336, 900, 0.1, "0.1 m = 90 px (orthos)")
    vx, vy = P(math.radians(118) - 2 * math.pi)
    ai, ao = P(th_e - 2 * math.pi), P(th_e)
    callouts(sh, [
        (vx + 6, vy - 50, "NEPTUNE WITH TRIDENT", "silver-gilt · 0.06 m", 700, 150, "start"),
        ((ai[0] + ao[0]) / 2, (ai[1] + ao[1]) / 2 - 6, "RIM BAND, 12 SCALLOPS", "silver-gilt 0.015 m", 700, 205, "start"),
        (cx - 90, gy - 0.20 * s, "NAUTILUS SHELL, NACRE", "0.18 × 0.10 × 0.15 m · 3 mm wall", 700, 260, "start"),
        (*P(th_e - 1.25 + 0.06, 0.8), "STRAP + CARTOUCHE ×3", "silver-gilt 0.012 m", 950, 150, "start"),
        (cx + 8, gy - 0.10 * s, "TRITON ON SEA-MONSTER", "silver, gilt highlights", 950, 205, "start"),
        (cx + 50, gy - 0.012 * s, "EMBOSSED DOMED FOOT", "0.11 m · gilt rubbed to silver", 950, 260, "start"),
    ])
    sh.text(700, 298, "- - -  shatters above 2.5 m/s: 10–14 nacre shards;", 10.5, "#C4542E")
    sh.text(700, 312, "       mount survives in 2 pieces (220 coin)", 10.5, "#C4542E")
    return sh.render("THE AGE OF POWDER · PLUNDER · 1600 COIN · 1 ST · ARTIFACT", "Nautilus Cup",
                     "0.20 × 0.12 × 0.34 m · ≤ 1.5k tris · 1024²", "hero 1 m ≈ 1150 px · orthos 1 m = 900 px · one hand",
                     [(NACRE, "nacre"), (GILT, "silver-gilt"), (SIL, "silver"), (TARN, "tarnish"), (STRIPE, "shell stripe")],
                     view_labels=[(380, "HERO ¾"), (850, "FRONT"), (1085, "SIDE")], ladder=None, human=False)
