from lib import *

TERRA, SEALC, CORDC, RED, WINE = "#B88A62", "#8A5A3C", "#9C8660", "#8E3F2C", "#4A2A2A"

PROF = [(0.0, 0.012), (0.03, 0.022), (0.06, 0.032), (0.12, 0.07), (0.22, 0.123), (0.32, 0.156), (0.40, 0.17),
        (0.43, 0.166), (0.47, 0.125), (0.495, 0.075), (0.515, 0.052), (0.56, 0.049), (0.572, 0.058), (0.585, 0.06)]


def jar(sh, cx, gy, k, e=16, detail=True, handles="3q"):
    ce, se = math.cos(math.radians(e)), math.sin(math.radians(e))

    def H(phi, z0, z1, rin, rout):
        """handle loop at azimuth phi (deg, 0 = toward viewer)."""
        s = math.sin(math.radians(phi))
        Y = lambda z: gy - z * k * ce
        p = [(cx + s * rin * k, Y(z0)), (cx + s * rout * k, Y((z0 + z1) / 2 + 0.03)), (cx + s * rin * k, Y(z1))]
        dd = f"M{f(p[0][0])} {f(p[0][1])} Q{f(p[1][0] + s * 30)} {f(p[1][1] - 10)} {f(p[2][0])} {f(p[2][1])}"
        sh.path(dd, "none", darken(TERRA, .6), 0.035 * k * .55 + 2)
        sh.path(dd, "none", TERRA, 0.035 * k * .55)
        sh.path(dd, "none", lighten(TERRA, .35), 1.4, op=.7)
        return p[1]

    back = []
    if handles == "3q":
        back = H(-110, 0.44, 0.34, 0.14, 0.235)
    elif handles == "front":
        back = H(-90, 0.44, 0.34, 0.15, 0.245)
    d, Y, _ = revolve(sh, cx, gy, k, PROF, TERRA, e, ridges=[0.08 + i * 0.03 for i in range(12)] if detail else None)
    bb = (cx - 0.17 * k, Y(0.585), cx + 0.17 * k, gy + 5)
    # fire-cloud near the toe
    gid = sh.rad([(0, darken(TERRA, .5), .7), (1, darken(TERRA, .5), 0)])
    sh.clipped(d, f'<ellipse cx="{f(cx + 0.03 * k)}" cy="{f(Y(0.08))}" rx="{f(0.12 * k)}" ry="{f(0.1 * k)}" fill="url(#{gid})"/>')
    # shoulder band + potter's mark
    r0, r1 = interp(PROF, 0.40), interp(PROF, 0.425)
    sh.clipped(d, f'<path d="M{f(cx - r0 * k)} {f(Y(0.40))} A{f(r0 * k)} {f(r0 * k * se)} 0 0 0 {f(cx + r0 * k)} {f(Y(0.40))} '
                  f'L{f(cx + r1 * k)} {f(Y(0.425))} A{f(r1 * k)} {f(r1 * k * se)} 0 0 1 {f(cx - r1 * k)} {f(Y(0.425))} Z" fill="{RED}" opacity=".85"/>')
    if detail:
        mx, my = cx - 0.04 * k, Y(0.37)
        sh.path(f"M{f(mx)} {f(my)} l{f(0.02 * k)} {f(0.03 * k)} l{f(0.02 * k)} {f(-0.03 * k)} M{f(mx + 0.02 * k)} {f(my + 0.03 * k)} v{f(0.02 * k)}",
                "none", RED, 2)
        # wine stain run from seal down one side
        sh.clipped(d, f'<path d="M{f(cx + 0.03 * k)} {f(Y(0.55))} q{f(0.03 * k)} {f(0.05 * k)} {f(0.01 * k)} {f(0.10 * k)} t{f(0.015 * k)} {f(0.09 * k)} t{f(-0.01 * k)} {f(0.07 * k)}" stroke="{WINE}" stroke-width="{f(0.009 * k)}" fill="none" opacity=".5" stroke-linecap="round"/>')
        sh.flecks(d, bb, 90, darken(TERRA, .35), .5, 1.5, .45)
        sh.flecks(d, bb, 40, lighten(TERRA, .4), .4, 1.1, .5)
    # rim ellipse
    rr = 0.06
    sh.ellipse(cx, Y(0.585), rr * k, rr * k * se, lighten(TERRA, .15), darken(TERRA, .6), 1.2)
    # seal lump
    seal = [(cx - 0.047 * k, Y(0.585)), (cx - 0.04 * k, Y(0.605)), (cx - 0.015 * k, Y(0.62)), (cx + 0.02 * k, Y(0.618)),
            (cx + 0.045 * k, Y(0.60)), (cx + 0.048 * k, Y(0.585)), (cx, Y(0.585) + 0.01 * k)]
    sh.shape(seal, SEALC, light=.3, dark=.5)
    if detail:
        sh.ellipse(cx, Y(0.603), 0.016 * k, 0.012 * k, "none", darken(SEALC, .5), 1.2)
        sh.path(f"M{f(cx - 0.006 * k)} {f(Y(0.597))} v{f(-0.012 * k)} h{f(0.01 * k)}", "none", darken(SEALC, .5), 1.1)
    # cord round the neck (2 turns + knot)
    for z in (0.53, 0.545):
        r = 0.051
        sh.path(f"M{f(cx - r * k)} {f(Y(z))} A{f(r * k)} {f(r * k * se)} 0 0 0 {f(cx + r * k)} {f(Y(z))}", "none", CORDC, 3)
    sh.path(f"M{f(cx + 0.02 * k)} {f(Y(0.535))} l{f(0.012 * k)} {f(0.04 * k)} m{f(-0.006 * k)} {f(-0.04 * k)} l{f(-0.004 * k)} {f(0.045 * k)}", "none", CORDC, 2.4)
    front = None
    if handles == "3q":
        front = H(70, 0.44, 0.34, 0.15, 0.24)
    elif handles == "front":
        front = H(90, 0.44, 0.34, 0.15, 0.245)
    elif handles == "side":
        # handle facing the viewer: a ring seen face-on
        sh.ellipse(cx, Y(0.40), 0.035 * k, 0.06 * k, "none", darken(TERRA, .6), 0.035 * k * .55 + 2)
        sh.ellipse(cx, Y(0.40), 0.035 * k, 0.06 * k, "none", TERRA, 0.035 * k * .55)
    return d, Y, front, back


def build():
    sh = item_sheet("Sealed Amphora", 120, 3, "0.34 × 0.34 × 0.62 m", "≤ 1.5k tris · 1024²",
                    "hero 1 m = 680 px · orthos 1 m = 320 px",
                    [("buff terracotta", TERRA), ("sealing clay", SEALC), ("flax cord", CORDC), ("haematite", RED),
                     ("wine stain", WINE)], seed=61)
    k, cx, gy = 680, 360, 650
    gid = sh.rad([(0, "#2A2012", .9), (1, "#14120E", 0)])
    sh.back.append(f'<ellipse cx="{cx}" cy="480" rx="300" ry="260" fill="url(#{gid})"/>')
    sh.ellipse(cx + 20, gy + 20, 170, 16, "#0E0C09", op=.6)
    # reed ring stand
    se = math.sin(math.radians(16))
    for i, c in enumerate((darken(CORDC, .3), CORDC)):
        sh.ellipse(cx, gy + 14 - i * 6, 0.10 * k, 0.10 * k * se + 4, "none", c, 9 - i * 2)
    for t in range(14):
        a = t * math.pi / 7
        x = cx + 0.10 * k * math.cos(a)
        y = gy + 11 + 0.10 * k * se * math.sin(a)
        if math.sin(a) > 0:
            sh.line(x - 3, y - 4, x + 3, y + 4, darken(CORDC, .5), 1)
    d, Y, hf, hb = jar(sh, cx, gy - 12, k)
    # fracture lines (5-7 sherds; neck + seal stays one piece)
    crack(sh, [(cx - 0.155 * k, Y(0.33)), (cx - 0.08 * k, Y(0.30)), (cx - 0.02 * k, Y(0.34)), (cx + 0.07 * k, Y(0.29)), (cx + 0.165 * k, Y(0.32))])
    crack(sh, [(cx - 0.02 * k, Y(0.34)), (cx - 0.04 * k, Y(0.22)), (cx + 0.01 * k, Y(0.14)), (cx - 0.01 * k, Y(0.06))])
    crack(sh, [(cx + 0.07 * k, Y(0.29)), (cx + 0.10 * k, Y(0.18)), (cx + 0.12 * k, Y(0.12))])
    crack(sh, [(cx - 0.08 * k, Y(0.30)), (cx - 0.12 * k, Y(0.20))])
    crack(sh, [(cx - 0.05 * k, Y(0.505)), (cx, Y(0.495)), (cx + 0.05 * k, Y(0.505))])
    sh.text(cx - 0.30 * k, Y(0.505) + 4, "neck + seal: one piece", 10, "#C4542E", "start")
    # grabs
    grab(sh, *hf, hf[0] + 40, hf[1] - 30)
    grab(sh, *hb, hb[0] - 30, hb[1] - 30)
    grab(sh, cx, Y(0.02) + 4, cx - 70, Y(0.02) + 20, "GRAB (TOE)")
    # orthos
    k2, g2 = 320, 690
    jar(sh, 830, g2, k2, e=0.001, detail=False, handles="front")
    jar(sh, 1060, g2, k2, e=0.001, detail=False, handles="side")
    for x0 in (830, 1060):
        sh.line(x0 - 0.17 * k2, 694, x0 + 0.17 * k2, 694, "#635C4C", .8)
    sh.line(1150, 690, 1150, 690 - 0.62 * k2, "#635C4C", .8)
    sh.text(1156, 690 - 0.31 * k2, "0.62", 10, "#635C4C")
    view_label(sh, 360, "THREE-QUARTER")
    view_label(sh, 830, "FRONT")
    view_label(sh, 1060, "SIDE")
    scale_bar(sh, 120, 706, 68, "0.1 m = 68 px (hero)")
    scale_bar(sh, 700, 706, 32, "0.1 m = 32 px (orthos)", ticks=2)
    sh.callouts([
        (cx + 0.01 * k, Y(0.615), "CLAY SEAL", "signet impression, 0.09 m"),
        (cx - 0.04 * k, Y(0.54), "FLAX CORD", "two turns + knot"),
        (hf[0], hf[1] + 16, "LOOP HANDLE ×2", "strap 0.035 × 0.02 m"),
        (cx - 0.10 * k, Y(0.415), "SHOULDER BAND", "haematite red, potter's mark"),
        (cx + 0.12 * k, Y(0.22), "WHEEL RIDGES", "every 0.01 m, normal map"),
        (cx + 0.01 * k, Y(0.04), "KNOB TOE", "won't stand: ring stand")], 700, 150, 400, slope=0.5)
    sh.text(1060, 300, "BREAKS > 4 m/s", 12, MADDER, "middle", ls="2")
    sh.text(1060, 316, "5–7 sherds · spills 20 L wine", 10, "#9A9078", "middle")
    return sh
