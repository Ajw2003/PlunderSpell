from lib import *

GILT = "#C9A227"; BRASS = "#A07E3E"; GRIME = "#3A2E18"; PIN = "#34404E"
R = 0.11


def face(sh, cx, cy, s, fracture_on=False, seed_off=0):
    r = R * s
    # mater + limb
    sh.circle(cx, cy, r, f"url(#{sh.form(GILT, 'sphere', 0.35, 0.45)})", INK, 1.4)
    sh.circle(cx, cy, r - 0.015 * s, f"url(#{sh.form(dk(GILT, 0.15), 'd', 0.3, 0.45)})", INK, 1)
    # worn brass on the rim (top-left wear arc)
    sh.path(f"M{fmt(cx - r * 0.95)} {fmt(cy - r * 0.3)} A{fmt(r * 0.99)} {fmt(r * 0.99)} 0 0 1 {fmt(cx + r * 0.2)} {fmt(cy - r * 0.97)}", "none", BRASS, 0.012 * s, op=0.7)
    for k in range(72):
        a = k * math.tau / 72
        l = 0.012 if k % 6 == 0 else 0.006
        sh.line([(cx + math.cos(a) * (r - 0.0015 * s), cy + math.sin(a) * (r - 0.0015 * s)),
                 (cx + math.cos(a) * (r - l * s), cy + math.sin(a) * (r - l * s))], GRIME, 0.9 if k % 6 == 0 else 0.6, smooth=False)
    for k in range(24):  # hour letters as small glyph marks
        a = k * math.tau / 24 + 0.13
        x, y = cx + math.cos(a) * (r - 0.0095 * s), cy + math.sin(a) * (r - 0.0095 * s)
        sh.circle(x, y, max(1.0, 0.0013 * s), GRIME)
    # plate: almucantars + azimuths
    pr = r - 0.015 * s
    for k in range(7):
        rr = pr * (0.18 + k * 0.12)
        sh.circle(cx, cy - pr * 0.28 + k * pr * 0.02, rr, "none", GRIME, 0.7, op=0.75)
    for k in range(-3, 4):
        sh.path(f"M{fmt(cx)} {fmt(cy - pr * 0.4)} Q{fmt(cx + k * pr * 0.35)} {fmt(cy + pr * 0.1)} {fmt(cx + k * pr * 0.25)} {fmt(cy + pr * 0.95)}", "none", GRIME, 0.6, op=0.6)
    sh.circle(cx, cy, pr * 0.92, "none", GRIME, 0.8)
    sh.line([(cx - pr, cy), (cx + pr, cy)], GRIME, 0.8, smooth=False)
    sh.line([(cx, cy - pr), (cx, cy + pr)], GRIME, 0.8, smooth=False)
    # rete: tropic ring, zodiac ring offset, star pointers, bars
    rg = sh.form(GILT, "d", 0.5, 0.4)
    rr = 0.095 * s
    sh.circle(cx, cy, rr, "none", INK, 0.012 * s * 0.55 + 1.6)
    sh.circle(cx, cy, rr, "none", f"url(#{rg})", 0.012 * s * 0.55)
    zc = (cx, cy - 0.028 * s)
    zr = 0.062 * s
    sh.circle(*zc, zr, "none", INK, 0.01 * s + 1.6)
    sh.circle(*zc, zr, "none", f"url(#{rg})", 0.01 * s)
    for k in range(36):
        a = k * math.tau / 36
        sh.line([(zc[0] + math.cos(a) * (zr - 0.004 * s), zc[1] + math.sin(a) * (zr - 0.004 * s)), (zc[0] + math.cos(a) * (zr + 0.004 * s), zc[1] + math.sin(a) * (zr + 0.004 * s))], GRIME, 0.5, smooth=False)
    for a in (0.3, 1.9, 3.5, 4.9):
        sh.line([(cx + math.cos(a) * rr, cy + math.sin(a) * rr), (cx + math.cos(a) * 0.012 * s, cy + math.sin(a) * 0.012 * s)], INK, 0.006 * s + 1.2, smooth=False)
        sh.line([(cx + math.cos(a) * rr, cy + math.sin(a) * rr), (cx + math.cos(a) * 0.012 * s, cy + math.sin(a) * 0.012 * s)], GILT, 0.006 * s, smooth=False)
    rnd = random.Random(7 + seed_off)
    for k in range(20):  # flame-shaped star pointers
        a = rnd.uniform(0, math.tau)
        d0 = rnd.uniform(0.035, 0.088) * s
        bx, by = cx + math.cos(a) * d0, cy + math.sin(a) * d0
        tip = (bx + math.cos(a + 2.4) * 0.012 * s, by + math.sin(a + 2.4) * 0.012 * s)
        nrm = (math.cos(a + 2.4 + 1.57) * 0.003 * s, math.sin(a + 2.4 + 1.57) * 0.003 * s)
        sh.path(smooth_path([(bx - nrm[0], by - nrm[1]), (bx + nrm[0], by + nrm[1]), tip]), GILT, INK, 0.6)
    # rule across
    ang = -0.55
    L = 0.095 * s
    ux, uy = math.cos(ang), math.sin(ang)
    w = 0.006 * s
    rule = [(cx - ux * L - uy * w, cy - uy * L + ux * w), (cx + ux * L - uy * w, cy + uy * L + ux * w), (cx + ux * L + uy * w, cy + uy * L - ux * w), (cx - ux * L + uy * w, cy - uy * L - ux * w)]
    sh.path(poly_path(rule), f"url(#{sh.form(BRASS, 'd', 0.5)})", INK, 0.9)
    for k in range(10):
        t = -0.9 + k * 0.2
        sh.line([(cx + ux * L * t, cy + uy * L * t), (cx + ux * L * t - uy * w * 0.8, cy + uy * L * t + ux * w * 0.8)], GRIME, 0.6, smooth=False)
    # pin + horse
    sh.circle(cx, cy, 0.006 * s, f"url(#{sh.form(PIN, 'sphere', 0.5)})", INK, 0.8)
    sh.path(smooth_path([(cx + 0.004 * s, cy - 0.002 * s), (cx + 0.02 * s, cy - 0.008 * s), (cx + 0.03 * s, cy - 0.002 * s), (cx + 0.02 * s, cy + 0.004 * s)]), PIN, INK, 0.7)
    # dents on the limb
    for a in (2.3, 4.0):
        sh.circle(cx + math.cos(a) * (r - 0.004 * s), cy + math.sin(a) * (r - 0.004 * s), 0.004 * s, dk(GILT, 0.35), op=0.6)
    if fracture_on:
        fracture(sh, [(cx + math.cos(t / 10 * math.tau) * (rr + 0.006 * s), cy + math.sin(t / 10 * math.tau) * (rr + 0.006 * s)) for t in range(11)], smooth=True)
        fracture(sh, [rule[0], rule[1]])


def throne(sh, cx, top_of_disc, s):
    # throne: pierced scroll, shackle, ring
    t = top_of_disc
    th = [(cx - 0.05 * s, t + 0.012 * s), (cx - 0.045 * s, t - 0.01 * s), (cx - 0.025 * s, t - 0.03 * s), (cx - 0.012 * s, t - 0.04 * s),
          (cx + 0.012 * s, t - 0.04 * s), (cx + 0.025 * s, t - 0.03 * s), (cx + 0.045 * s, t - 0.01 * s), (cx + 0.05 * s, t + 0.012 * s)]
    sh.path(smooth_path(th), f"url(#{sh.form(GILT, 'v', 0.4)})", INK, 1)
    for dx in (-0.025, 0.025):
        sh.ellipse(cx + dx * s, t - 0.012 * s, 0.009 * s, 0.006 * s, GRIME)
    sh.ellipse(cx, t - 0.026 * s, 0.006 * s, 0.005 * s, GRIME)
    sh.path(smooth_path([(cx - 0.035 * s, t - 0.004 * s), (cx - 0.02 * s, t - 0.02 * s), (cx, t - 0.018 * s), (cx + 0.02 * s, t - 0.02 * s), (cx + 0.035 * s, t - 0.004 * s)], closed=False), "none", BRASS, 1.2, op=0.8)
    sh.rect(cx - 0.006 * s, t - 0.05 * s, 0.012 * s, 0.011 * s, BRASS, INK, 0.8)
    ry = t - 0.05 * s - 0.02 * s
    sh.circle(cx, ry, 0.017 * s, "none", INK, 0.005 * s + 1.6)
    sh.circle(cx, ry, 0.017 * s, "none", f"url(#{sh.form(BRASS, 'd', 0.5)})", 0.005 * s)
    return ry


def hero(sh):
    s = 1250
    cx, cy = 380, 670 - R * s
    sh.ellipse(395, 684, 150, 10, "#000000", op=0.45)
    # thickness: offset back disc
    sh.add(f'<g transform="translate({cx} {cy}) matrix(0.80 0.10 0 1 0 0) translate({-cx} {-cy})">')
    sh.circle(cx + 22, cy + 2, R * s, dk(BRASS, 0.35), INK, 1.2)
    sh.close()
    sh.add(f'<g transform="translate({cx} {cy}) matrix(0.80 0.10 0 1 0 0) translate({-cx} {-cy})">')
    ry = throne(sh, cx, cy - R * s, s)
    face(sh, cx, cy, s, fracture_on=True)
    sh.close()
    return cx, cy, ry, s


def front_ortho(sh):
    s = 1000
    cx, cy = 840, 690 - R * s - 0.004 * s
    sh.ellipse(cx, 690, 110, 5, "#000000", op=0.4)
    ry = throne(sh, cx, cy - R * s, s)
    face(sh, cx, cy, s, seed_off=1)
    grab(sh, cx, ry, "", 0, 0)
    return ry


def side_ortho(sh):
    s = 1000
    cx = 1080
    base = 690
    top = base - 0.22 * s
    sh.ellipse(cx, 690, 30, 4, "#000000", op=0.4)
    sh.rect(cx - 0.006 * s, top, 0.012 * s, 0.22 * s, f"url(#{sh.form(GILT, 'h', 0.4)})", INK, 1, rx=3)  # mater
    sh.rect(cx + 0.006 * s, top + 2, 0.004 * s, 0.22 * s - 4, dk(GILT, 0.2), INK, 0.6)  # limb/rete
    sh.rect(cx + 0.010 * s, top + 0.03 * s, 0.003 * s, 0.16 * s, BRASS, INK, 0.6)  # rule
    sh.rect(cx - 0.009 * s, top + 0.012 * s, 0.003 * s, 0.196 * s, BRASS, INK, 0.6)  # alidade
    for y in (top + 0.02 * s, top + 0.20 * s):
        sh.rect(cx - 0.024 * s, y - 2, 0.015 * s, 4, BRASS, INK, 0.6)  # vanes
    sh.circle(cx + 0.014 * s, base - 0.11 * s, 0.004 * s, PIN, INK, 0.6)
    sh.rect(cx - 0.004 * s, top - 0.04 * s, 0.008 * s, 0.04 * s, f"url(#{sh.form(GILT, 'h', 0.4)})", INK, 0.8)
    sh.ellipse(cx, top - 0.06 * s, 0.003 * s, 0.017 * s, "none", BRASS, 3)
    grab(sh, cx, top - 0.06 * s, "", 0, 0)
    sh.text(cx + 30, base - 0.12 * s, "0.03 m", 10, FRAME_TXT)


def build():
    sh = Sheet(seed=23)
    cx, cy, ry, s = hero(sh)
    def T(x, y):
        return (cx + 0.8 * (x - cx), y + 0.1 * (x - cx))
    fry = front_ortho(sh)
    side_ortho(sh)
    grab(sh, *T(cx, ry), "GRAB · RING / THRONE", 16, -10)
    grab(sh, *T(cx - 60, cy + R * s - 20), "GRAB · PALM UNDER", -14, 20, "end")
    scale_bar(sh, 700, 336, 1000, 0.1, "0.1 m = 100 px (orthos)")
    callouts(sh, [
        (*T(cx - 17, ry), "SUSPENSION RING", "worn brass · 0.04 m", 700, 150, "start"),
        (*T(cx + 4, cy - R * s - 20), "PIERCED THRONE", "gilt, rubbed to brass", 700, 205, "start"),
        (*T(cx - 128, cy + 18), "LIMB, 360° + 24 HOURS", "engraving filled with grime", 700, 260, "start"),
        (*T(cx + 52, cy - 70), "RETE", "pierced star map · 20 pointers", 950, 150, "start"),
        (*T(cx + 20, cy - 6), "PIN + HORSE WEDGE", "blued steel", 950, 205, "start"),
        (*T(cx - 40, cy + 30), "RULE", "brass · 0.19 m", 950, 260, "start"),
    ])
    sh.text(700, 298, "- - -  snaps above 8 m/s: rete + rule free,", 10.5, "#C4542E")
    sh.text(700, 312, "       mater survives (3 pieces, 200 coin)", 10.5, "#C4542E")
    return sh.render("THE AGE OF POWDER · PLUNDER · 320 COIN · 1.5 ST", "Astrolabe",
                     "0.24 × 0.03 × 0.30 m · ≤ 1.5k tris · 1024²", "hero 1 m ≈ 1250 px · orthos 1 m = 1000 px · one hand",
                     [(GILT, "gilt brass"), (BRASS, "worn brass"), (GRIME, "engraving grime"), (PIN, "steel pin")],
                     view_labels=[(380, "HERO ¾"), (840, "FRONT"), (1080, "SIDE")], ladder=None, human=False)
