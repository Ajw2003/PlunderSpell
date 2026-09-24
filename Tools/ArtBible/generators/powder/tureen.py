from lib import *

SIL = "#B8B4A8"; TARN = "#6A665C"; POL = "#DAD7CE"; SOUP = "#5A4A30"


def silver_grad(sh, key="sil"):
    return sh.lin([(0, TARN), (0.12, SIL), (0.3, POL), (0.45, SIL), (0.75, dk(SIL, 0.25)), (0.92, TARN), (1, dk(TARN, 0.3))], key=key)


def paw(sh, x, y, s, shade=0.0):
    w = 0.03 * s
    h = 0.05 * s
    pts = [(x - w, y), (x - w * 1.1, y - h * 0.3), (x - w * 0.6, y - h), (x + w * 0.6, y - h), (x + w * 1.1, y - h * 0.3), (x + w, y)]
    sh.path(smooth_path(pts), f"url(#{sh.form(dk(SIL, shade), 'h', 0.4)})", INK, 0.9)
    for k in (-0.5, 0, 0.5):
        sh.line([(x + k * w, y), (x + k * w * 1.1, y - h * 0.3)], TARN, 1.0, smooth=False)


def vessel(sh, cx, gy, s, rx, rim_ry, handles="loop", lid_off=0.0, ladle=True, fracture_on=False, view="hero"):
    """Oval tureen in elevation / slight top view. rx = half-width in metres of this view (bowl)."""
    Z = lambda z: gy - z * s
    X = lambda x: cx + x * s
    # feet
    for fx, sh_ in ((-0.72, 0.3), (0.72, 0.0)) if view != "hero" else ((-0.75, 0.25), (-0.25, 0.4), (0.3, 0.35), (0.72, 0.0)):
        paw(sh, X(fx * rx), gy, s, sh_)
    # skirt
    sk = [(X(-rx * 0.82), Z(0.045)), (X(rx * 0.82), Z(0.045)), (X(rx * 0.72), Z(0.075)), (X(-rx * 0.72), Z(0.075))]
    sh.path(smooth_path(sk), f"url(#{silver_grad(sh)})", INK, 1)
    # bowl body
    body = [(X(-rx * 0.72), Z(0.07)), (X(-rx * 0.97), Z(0.13)), (X(-rx * 1.02), Z(0.19)), (X(-rx * 0.98), Z(0.23)),
            (X(rx * 0.98), Z(0.23)), (X(rx * 1.02), Z(0.19)), (X(rx * 0.97), Z(0.13)), (X(rx * 0.72), Z(0.07))]
    d = smooth_path(body)
    sh.path(d, f"url(#{silver_grad(sh)})", INK, 1.2)
    sh.clip_open(d)
    # hammer facets
    rnd = sh.rnd
    for _ in range(60):
        x = rnd.uniform(-rx, rx)
        z = rnd.uniform(0.08, 0.22)
        sh.ellipse(X(x), Z(z), 0.009 * s, 0.006 * s, rnd.choice([POL, TARN]), op=round(rnd.uniform(0.06, 0.18), 2))
    sh.path(smooth_path([(X(-rx), Z(0.205)), (X(rx), Z(0.205)), (X(rx), Z(0.24)), (X(-rx), Z(0.24))]), TARN, op=0.35)
    sh.close()
    # engraved arms lozenge
    lx, ly = X(-rx * 0.05), Z(0.15)
    lw = 0.04 * s
    sh.path(poly_path([(lx, ly - lw), (lx + lw * 0.8, ly), (lx, ly + lw), (lx - lw * 0.8, ly)]), "none", TARN, 1.2)
    sh.path(poly_path([(lx, ly - lw * 0.6), (lx + lw * 0.45, ly), (lx, ly + lw * 0.6), (lx - lw * 0.45, ly)]), "none", TARN, 0.8)
    sh.line([(lx - lw * 0.3, ly - lw * 0.1), (lx + lw * 0.3, ly - lw * 0.1)], TARN, 0.8, smooth=False)
    # rim (ellipse)
    rim_y = Z(0.23)
    sh.ellipse(cx, rim_y, rx * s * 1.0, rim_ry * s, SIL, INK, 1.2)
    sh.ellipse(cx, rim_y, rx * s * 0.985, rim_ry * s * 0.8, POL, op=0.5)
    # handles
    if handles == "loop":
        for sgn in (-1, 1):
            hx = X(sgn * rx * 0.98)
            hy = Z(0.19)
            pts = [(hx, hy - 0.03 * s), (hx + sgn * 0.05 * s, hy - 0.045 * s), (hx + sgn * 0.075 * s, hy - 0.01 * s), (hx + sgn * 0.06 * s, hy + 0.03 * s), (hx, hy + 0.02 * s)]
            sh.line(pts, INK, 0.014 * s + 2)
            sh.line(pts, f"url(#{sh.form(SIL, 'v', 0.4)})", 0.014 * s)
            sh.line(pts[:3], POL, 1.2, op=0.7)
            sh.path(smooth_path([(hx + sgn * 0.04 * s, hy - 0.045 * s), (hx + sgn * 0.055 * s, hy - 0.07 * s), (hx + sgn * 0.065 * s, hy - 0.048 * s)]), SIL, INK, 0.8)
    else:  # end-on: loop seen as a ring in front
        hx, hy = cx, Z(0.18)
        sh.ellipse(hx, hy, 0.045 * s, 0.03 * s, "none", INK, 0.014 * s + 2)
        sh.ellipse(hx, hy, 0.045 * s, 0.03 * s, "none", SIL, 0.014 * s)
    # lid
    ly0 = Z(0.235 + lid_off)
    lid = [(X(-rx * 1.0), ly0), (X(-rx * 0.85), ly0 - 0.035 * s), (X(-rx * 0.45), ly0 - 0.07 * s), (cx, ly0 - 0.08 * s),
           (X(rx * 0.45), ly0 - 0.07 * s), (X(rx * 0.85), ly0 - 0.035 * s), (X(rx * 1.0), ly0)]
    dl = smooth_path(lid)
    sh.path(dl, f"url(#{silver_grad(sh)})", INK, 1.2)
    sh.clip_open(dl)
    sh.path(smooth_path([(X(-rx * 0.6), ly0 - 0.055 * s), (X(-rx * 0.2), ly0 - 0.075 * s), (X(0.0), ly0 - 0.07 * s), (X(-rx * 0.5), ly0 - 0.045 * s)]), POL, op=0.6)
    for k in range(3):
        sh.path(smooth_path([(X(-rx * 0.95), ly0 - 0.006 * s * (k + 1)), (cx, ly0 - 0.006 * s * (k + 1) + 0.004 * s), (X(rx * 0.95), ly0 - 0.006 * s * (k + 1))], closed=False), "none", TARN, 0.8, op=0.5)
    sh.close()
    sh.ellipse(cx, ly0, rx * s * 1.01, rim_ry * s * 0.35, "none", dk(SIL, 0.3), 1.2)
    # finial: pomegranate
    fy = ly0 - 0.08 * s
    sh.rect(cx - 0.008 * s, fy - 0.015 * s, 0.016 * s, 0.017 * s, SIL, INK, 0.8)
    sh.circle(cx, fy - 0.032 * s, 0.02 * s, f"url(#{sh.form(SIL, 'sphere', 0.5)})", INK, 0.9)
    for k in (-1, 0, 1):
        sh.path(poly_path([(cx + k * 0.008 * s - 3, fy - 0.05 * s), (cx + k * 0.008 * s, fy - 0.06 * s), (cx + k * 0.008 * s + 3, fy - 0.05 * s)]), SIL, INK, 0.7)
    # ladle through notch
    if ladle:
        nx, ny = X(rx * 0.8), ly0 - 0.02 * s
        sh.path(poly_path([(nx - 0.015 * s, ly0 + 2), (nx + 0.015 * s, ly0 + 2), (nx + 0.012 * s, ny), (nx - 0.012 * s, ny)]), dk(TARN, 0.4), INK, 0.8)
        stem = [(nx, ny + 0.01 * s), (nx + 0.05 * s, ny - 0.05 * s), (nx + 0.10 * s, ny - 0.11 * s), (nx + 0.12 * s, ny - 0.14 * s)]
        sh.line(stem, INK, 0.008 * s + 2)
        sh.line(stem, SIL, 0.008 * s)
        sh.line(stem, POL, 1, op=0.7)
        sh.ellipse(nx + 0.125 * s, ny - 0.15 * s, 0.012 * s, 0.018 * s, f"url(#{sh.form(SIL, 'sphere', 0.5)})", INK, 0.8, rot=-40)
    if fracture_on:
        fracture(sh, [(X(-rx * 1.03), ly0 + 3), (cx, ly0 + rim_ry * s * 0.35 + 3), (X(rx * 1.03), ly0 + 3)], smooth=True)
        if ladle:
            fracture(sh, [(X(rx * 0.8) - 14, ly0 - 0.03 * s), (X(rx * 0.8) + 14, ly0 - 0.03 * s)])
        fracture(sh, [(X(0.72 * rx) - 0.035 * s, Z(0.055)), (X(0.72 * rx) + 0.035 * s, Z(0.055))])
    return ly0


def build():
    sh = Sheet(seed=24)
    s = 1000
    cx, gy = 380, 672
    sh.ellipse(cx, gy + 6, 250, 16, "#000000", op=0.45)
    # hero: slight top view (rim ellipse deeper)
    ly0 = vessel(sh, cx, gy, s, 0.20, 0.06, fracture_on=True)
    # motion arcs: lid pops
    for k in range(3):
        sh.path(f"M{cx - 150 + k * 16} {ly0 - 88 - k * 10} q 40 -26 80 -8", "none", FRAME_TXT, 1, op=0.5)
    sh.text(cx - 210, ly0 - 150, "lid pops at > 1.5 m/s jolt", 10.5, FRAME_TXT)
    grab(sh, cx - 0.20 * s - 0.06 * s, gy - 0.19 * s - 0.01 * s, "GRAB", 0, 26, "middle")
    grab(sh, cx + 0.20 * s + 0.06 * s, gy - 0.19 * s - 0.01 * s, "GRAB", 0, 26, "middle")
    grab(sh, cx, ly0 - 0.08 * s - 0.032 * s, "HOLD LID", 24, -16)
    # orthos
    so = 500
    fx = 830
    sh.ellipse(fx, 690, 150, 5, "#000000", op=0.4)
    vessel(sh, fx, 690, so, 0.20, 0.012)
    for sgn in (-1, 1):
        grab(sh, fx + sgn * (0.20 + 0.06) * so, 690 - 0.19 * so - 0.01 * so, "", 0, 0)
    sx = 1075
    sh.ellipse(sx, 690, 95, 5, "#000000", op=0.4)
    vessel(sh, sx, 690, so, 0.13, 0.012, handles="end", ladle=False, view="side")
    grab(sh, sx, 690 - 0.18 * so, "", 0, 0)
    scale_bar(sh, 700, 336, 500, 0.1, "0.1 m = 50 px (orthos)")
    callouts(sh, [
        (cx, ly0 - 0.13 * s, "POMEGRANATE FINIAL", "cast silver 0.05 m", 700, 150, "start"),
        (cx - 60, ly0 - 0.06 * s, "DOMED LID", "separate physics body", 700, 205, "start"),
        (cx + 0.20 * s * 0.8 + 0.1 * s, ly0 - 0.13 * s, "LADLE IN NOTCH", "0.30 m · separate body", 700, 260, "start"),
        (cx - 20, gy - 0.16 * s, "HAMMERED OVAL BOWL", "0.40 × 0.26 m · engraved arms", 950, 150, "start"),
        (cx + 0.28 * s, gy - 0.215 * s, "SCROLL LOOP HANDLE", "leaf thumb-piece", 950, 205, "start"),
        (cx + 0.3 * 0.2 * s, gy - 0.03 * s, "LION'S-PAW FEET ×4", "0.05 m · tarnish in recesses", 950, 260, "start"),
    ])
    sh.text(700, 298, "- - -  dents above 6 m/s: lid, ladle, one foot", 10.5, "#C4542E")
    sh.text(700, 312, "       fly off (4 pieces, each stealable)", 10.5, "#C4542E")
    return sh.render("THE AGE OF POWDER · PLUNDER · 260 COIN · 3 ST", "Silver Service Tureen",
                     "0.46 × 0.30 × 0.36 m · ≤ 3k tris · 1024²", "hero 1 m ≈ 1000 px · orthos 1 m = 500 px · two hands",
                     [(SIL, "sterling silver"), (TARN, "tarnish"), (POL, "bright polish"), (SOUP, "soup residue")],
                     view_labels=[(380, "HERO ¾"), (830, "FRONT"), (1075, "SIDE")], ladder=None, human=False)
