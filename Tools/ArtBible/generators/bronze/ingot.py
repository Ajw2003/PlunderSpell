from lib import *

CU, OX, GRIT, SOOT = "#9E5A36", "#5A3524", "#7A6A58", "#2B231B"
HI = "#C07A4E"   # bright copper on high points (from the copper note)

OUT = [(0.30, 0.20), (0.22, 0.165), (0, 0.142), (-0.22, 0.165), (-0.30, 0.20), (-0.255, 0.12), (-0.24, 0),
       (-0.255, -0.12), (-0.30, -0.20), (-0.22, -0.165), (0, -0.142), (0.22, -0.165), (0.30, -0.20), (0.255, -0.12),
       (0.24, 0), (0.255, 0.12)]


def lug_z(x, y, top):
    """lugs droop 5 degrees; centre is domed."""
    r = max(abs(x) / 0.30, abs(y) / 0.20)
    return (0.045 if top else 0.0) + (0.006 if top else 0) * (1 - r) - 0.012 * max(0, r - 0.8) / 0.2


def build():
    sh = item_sheet("Oxhide Ingot", 180, 4, "0.60 × 0.40 × 0.05 m", "≤ 1.5k tris · 1024²",
                    "hero 1 m = 680 px · orthos 1 m = 400 px",
                    [("raw copper", CU), ("copper oxide", OX), ("casting grit", GRIT), ("hearth soot", SOOT)], seed=51)
    P = Proj(370, 520, 680, a=28, e=34)
    # firelight pool
    gid = sh.rad([(0, "#2A2012", .9), (1, "#14120E", 0)])
    sh.back.append(f'<ellipse cx="380" cy="540" rx="330" ry="170" fill="url(#{gid})"/>')
    sh.ellipse(380, 630, 250, 24, "#0E0C09", op=.6)
    bot = P.pts([(x, y, lug_z(x, y, True) - 0.035) for x, y in OUT])
    top = P.pts([(x, y, lug_z(x, y, True)) for x, y in OUT])
    sh.path(smooth_path(bot, tension=.35), f"url(#{sh.lin(OX, 'v', .2, .5)})", darken(OX, .6), 1.4)
    # side band gradient (visible front faces)
    dt = sh.shape(top, CU, direction="d", light=.35, dark=.45, sw=1.4)
    bb = (min(p[0] for p in top), min(p[1] for p in top), max(p[0] for p in top), max(p[1] for p in top))
    # shrinkage skin wrinkles
    for i in range(7):
        y0 = -0.10 + i * 0.033
        pts = [P(x, y0 + 0.01 * math.sin(x * 40 + i), 0.05) for x in [-0.18 + j * 0.06 for j in range(7)]]
        sh.path(smooth_path(pts, closed=False), "none", darken(CU, .35), 1, op=.45)
    # blisters
    rng = sh.rng
    blis = []
    for _ in range(26):
        x, y = rng.uniform(-0.2, 0.2), rng.uniform(-0.12, 0.12)
        r = rng.uniform(0.006, 0.018)
        cx, cy = P(x, y, 0.051)
        blis.append((cx, cy, r))
        sh.ellipse(cx, cy, r * P.k, r * P.k * P.se, OX, op=.9)
        sh.ellipse(cx - r * P.k * .25, cy - r * P.k * P.se * .3, r * P.k * .55, r * P.k * P.se * .45, HI, op=.75)
    sh.flecks(dt, bb, 160, OX, .5, 1.6, .55)
    sh.flecks(dt, bb, 60, HI, .4, 1.1, .6)
    # lug rub highlights
    for x, y in ((0.30, 0.20), (-0.30, 0.20), (-0.30, -0.20), (0.30, -0.20)):
        cx, cy = P(x * .93, y * .93, 0.036)
        sh.ellipse(cx, cy, 16, 6, HI, op=.7)
    # rim edge highlight on the near edges
    near = P.pts([(x, y, lug_z(x, y, True)) for x, y in OUT[8:13]])
    sh.path(smooth_path(near, closed=False, tension=.35), "none", HI, 1.6, op=.8)
    # stamp: trident-like sign near the short edge
    s0 = P(0.17, 0.0, 0.052)
    sc = 0.65
    sh.path(f"M{f(s0[0])} {f(s0[1] + 16 * sc)} L{f(s0[0])} {f(s0[1] - 20 * sc)} M{f(s0[0] - 14 * sc)} {f(s0[1] - 18 * sc)} "
            f"Q{f(s0[0] - 14 * sc)} {f(s0[1] - 2 * sc)} {f(s0[0])} {f(s0[1] - 2 * sc)} Q{f(s0[0] + 14 * sc)} {f(s0[1] - 2 * sc)} "
            f"{f(s0[0] + 14 * sc)} {f(s0[1] - 18 * sc)} M{f(s0[0] - 8 * sc)} {f(s0[1] + 14 * sc)} L{f(s0[0] + 8 * sc)} {f(s0[1] + 14 * sc)}",
            "none", darken(OX, .3), 2.4)
    # soot smudge on underside edge
    sh.path(smooth_path(P.pts([(-0.2, -0.16, 0.0), (0.1, -0.15, 0.0), (0.2, -0.17, 0.005), (-0.1, -0.18, 0.005)])),
            SOOT, op=.35)

    # ---- orthos ----
    k = 400
    # FRONT (x across, z up), centred 830
    fx, gy = 830, 690
    fr = [(-0.30, 0.0), (-0.30, 0.02), (-0.24, 0.035), (0, 0.05), (0.24, 0.035), (0.30, 0.02), (0.30, 0.0),
          (0.24, 0.012), (0, 0.012), (-0.24, 0.012)]
    fr = [(fx + x * k, gy - 6 - z * k) for x, z in fr]
    sh.shape(fr, CU, direction="v", light=.35, dark=.5, sw=1.2)
    sh.line(fx - 0.30 * k, gy - 6, fx + 0.30 * k, gy - 6, darken(OX, .3), .8, op=.6)
    # SIDE (y across), centred 1080
    sx = 1080
    sd = [(-0.20, 0.0), (-0.20, 0.02), (-0.16, 0.035), (0, 0.05), (0.16, 0.035), (0.20, 0.02), (0.20, 0.0),
          (0.16, 0.012), (0, 0.012), (-0.16, 0.012)]
    sd = [(sx + y * k, gy - 6 - z * k) for y, z in sd]
    sh.shape(sd, CU, direction="v", light=.35, dark=.5, sw=1.2)
    # TOP (plan) above them, same scale
    tx, ty = 830, 520
    tp = [(tx + x * k, ty + y * k) for x, y in OUT]
    dtp = sh.shape(tp, CU, direction="d", light=.3, dark=.45, sw=1.2)
    sh.flecks(dtp, (tx - 120, ty - 80, tx + 120, ty + 80), 80, OX, .5, 1.5, .55)
    sh.line(tx - 0.30 * k, ty + 92, tx + 0.30 * k, ty + 92, "#635C4C", .8)
    sh.text(tx, ty + 106, "0.60 m", 10, "#635C4C", "middle")
    sh.line(tx + 0.30 * k + 14, ty - 0.20 * k, tx + 0.30 * k + 14, ty + 0.20 * k, "#635C4C", .8)
    sh.text(tx + 0.30 * k + 20, ty + 4, "0.40 m", 10, "#635C4C")
    sh.extra_frame.append(f'<text x="{tx}" y="{ty - 0.20 * k - 12:.0f}" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">TOP</text>')
    view_label(sh, 380, "THREE-QUARTER")
    view_label(sh, 830, "FRONT")
    view_label(sh, 1080, "SIDE")
    scale_bar(sh, 120, 706, 68, "0.1 m = 68 px (hero)")
    scale_bar(sh, 700, 706, 40, "0.1 m = 40 px (orthos)", ticks=2)
    # grabs: two lugs on the near short side
    g1 = P(-0.30, -0.20, 0.03); g2 = P(-0.30, 0.20, 0.03)
    grab(sh, *g1, g1[0] - 50, g1[1] + 40)
    grab(sh, *g2, g2[0] - 40, g2[1] - 40)
    # callouts
    sh.callouts([
        (*P(0.30, 0.20, 0.035), "HORNED LUG ×4", "flares 0.07 m, droops 5°"),
        (*P(0.0, 0.142, 0.045), "CONCAVE SIDE", "0.06 m inward curve"),
        (*P(0.05, 0.02, 0.051), "BLISTERED TOP", "open-mould face, pocked"),
        (*s0, "STAMPED SIGN", "incised mark, 0.06 m"),
        (*P(0.0, -0.142, 0.02), "CAST EDGE", "0.035 m, flash line one side")], 700, 150, 330, slope=0.9)
    sh.text(1085, 600, "UNBREAKABLE · 999", 12, MADDER, "middle", ls="2")
    sh.text(1085, 616, "rings, slides, never splits", 10, "#9A9078", "middle")
    return sh
