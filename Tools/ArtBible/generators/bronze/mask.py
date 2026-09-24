from lib import *

GOLD, DEEP, HIGH, DUST, LINEN = "#C9A227", "#8A6B1A", "#E6C75A", "#5A4A38", "#CFC3A2"


def mask_face(sh, cx, cy, s, turn=1.0, detail=True):
    """Mask centred at (cx, cy); s = px per 'unit' where the face is ~338 units wide x 403 tall.
    turn < 1 compresses the far (viewer-right) half for a three-quarter view."""
    def P(x, y):
        # x in [-169, 169], y in [-201, 201]; far half compressed
        xx = x * (turn if x > 0 else (2 - turn) * 0.92 if turn < 1 else 1)
        return (cx + xx * s, cy + y * s)

    def pts(lst):
        return [P(x, y) for x, y in lst]

    shift = (1 - turn) * 60   # facial midline moves toward the far side

    def Q(x, y):
        return P(x + shift * (1 - abs(y) / 260), y)

    outline = [(0, -201), (80, -190), (140, -150), (165, -80), (160, 10), (140, 90), (105, 150), (55, 190), (0, 201),
               (-55, 190), (-105, 150), (-140, 90), (-160, 10), (-165, -80), (-140, -150), (-80, -190)]
    # ears (behind the rim)
    for side in (-1, 1):
        ex = 168 * side
        e = pts([(ex, -60), (ex + 34 * side, -64), (ex + 42 * side, -20), (ex + 34 * side, 30), (ex, 26)])
        sh.path(smooth_path(e), f"url(#{sh.lin(GOLD, 'h', .3, .5)})", darken(DEEP, .4), 1.2)
        hx, hy = P(ex + 24 * side, -18)
        sh.circle(hx, hy, 4 * s, darken(DEEP, .5))
    d = smooth_path(pts(outline))
    gid = sh.rad([(0, HIGH, 1), (0.45, GOLD, 1), (1, DEEP, 1)], cx=.38, cy=.35, r=.75)
    sh.path(d, f"url(#{gid})", darken(DEEP, .45), 2)
    # rolled rim
    sh.path(d, "none", HIGH, 2.2 * s + .5, op=.6)
    if not detail:
        # simple features for the ortho
        pass
    # brows
    for side, w in ((-1, 1), (1, 1)):
        sh.path(smooth_path([Q(-10 * side * -1 + 0, -95) if False else Q(side * 12, -92), Q(side * 60, -112), Q(side * 115, -98)], closed=False),
                "none", DEEP, 9 * s)
        sh.path(smooth_path([Q(side * 12, -96), Q(side * 60, -116), Q(side * 115, -102)], closed=False), "none", HIGH, 3 * s)
        if detail:
            for t in range(9):
                a = Q(side * (20 + t * 11), -108 + abs(t - 4) * 1.8)
                sh.line(a[0], a[1], a[0] + 3 * s * side, a[1] + 7 * s, darken(DEEP, .3), 1)
    # closed eyes
    for side in (-1, 1):
        e = [Q(side * 22, -52), Q(side * 60, -70), Q(side * 105, -55), Q(side * 60, -40)]
        sh.path(smooth_path(e), DEEP, darken(DEEP, .4), 1.2)
        sh.path(smooth_path([Q(side * 22, -52), Q(side * 60, -64), Q(side * 105, -55)], closed=False), "none", HIGH, 2.5 * s)
        sh.path(smooth_path([Q(side * 24, -50), Q(side * 60, -44), Q(side * 102, -53)], closed=False), "none", darken(DEEP, .5), 1.5)
        if detail:
            for t in range(6):
                a = Q(side * (32 + t * 12), -46 + abs(t - 2.5) * 1.5)
                sh.line(a[0], a[1], a[0], a[1] + 6 * s, darken(DEEP, .5), .9)
    # nose ridge + side shadow
    nose = [Q(-8, -90), Q(8, -90), Q(16, 10), Q(22, 30), Q(0, 38), Q(-22, 30), Q(-16, 10)]
    sh.path(smooth_path(nose), f"url(#{sh.lin(GOLD, 'h', .5, .45)})", darken(DEEP, .3), 1)
    sh.path(smooth_path([Q(-4, -85), Q(-6, 0), Q(-10, 28)], closed=False), "none", HIGH, 3 * s, op=.9)
    for side in (-1, 1):
        sh.ellipse(*Q(side * 12, 32), 5 * s, 3 * s, darken(DEEP, .4))
    # moustache: two upturned curls
    for side in (-1, 1):
        m = [Q(side * 4, 50), Q(side * 40, 48), Q(side * 70, 44), Q(side * 88, 30), Q(side * 84, 20), Q(side * 74, 30),
             Q(side * 42, 62), Q(side * 4, 64)]
        sh.path(smooth_path(m), f"url(#{sh.lin(GOLD, 'v', .45, .5)})", darken(DEEP, .35), 1.2)
        sh.circle(*Q(side * 85, 24), 3 * s, HIGH)
        if detail:
            for t in range(5):
                a, b = Q(side * (12 + t * 14), 54), Q(side * (16 + t * 14), 62)
                sh.line(a[0], a[1], b[0], b[1], darken(DEEP, .3), .9)
    # lips
    sh.path(smooth_path([Q(-30, 88), Q(0, 82), Q(30, 88), Q(0, 96)]), DEEP, darken(DEEP, .4), 1)
    sh.path(smooth_path([Q(-30, 88), Q(0, 92), Q(30, 88)], closed=False), "none", darken(DEEP, .5), 1.5)
    # beard: incised parallel lines along jaw and chin
    if detail:
        beard = []
        for t in range(26):
            ang = math.radians(200 + t * 5.6)
            ox, oy = 150 * math.cos(ang) * -1, 120 + 70 * math.sin(math.radians(t * 180 / 25))
            x0 = -150 + t * 12
            y0 = 110 + 70 * math.sin(math.radians(t * 180 / 25)) - 40
            a, b = Q(x0, y0 - 30), Q(x0 * 0.95, y0 + 26)
            beard.append(f'<line x1="{f(a[0])}" y1="{f(a[1])}" x2="{f(b[0])}" y2="{f(b[1])}" stroke="{darken(DEEP, .3)}" stroke-width="1.2"/>')
        sh.clipped(d, "".join(beard))
        # tomb dust in the incisions + hollows
        sh.flecks(d, (cx - 170 * s, cy - 200 * s, cx + 170 * s, cy + 200 * s), 90, DUST, .5, 1.6, .5)
        sh.flecks(d, (cx - 170 * s, cy - 200 * s, cx + 170 * s, cy + 200 * s), 50, HIGH, .4, 1.0, .7)
        # small ancient crumple on the lower left cheek
        for a, b, c in (((-120, 60), (-95, 95), (-130, 110)), ((-95, 95), (-70, 130), (-110, 140))):
            tri = [Q(*a), Q(*b), Q(*c)]
            sh.path(poly_path(tri), DEEP, darken(DEEP, .4), .8, op=.55)
            sh.line(*tri[0], *tri[1], HIGH, 1.2, op=.8)
    # four fixing holes at temples and chin
    for x, y in ((-150, -110), (150, -110), (-30, 185), (30, 185)):
        sh.circle(*P(x, y), 2.6 * s + .6, darken(DEEP, .6))
    return P, Q, d


def build():
    sh = item_sheet("Gold Death-Mask", 1400, 1, "0.26 × 0.08 × 0.31 m", "≤ 1.5k tris (+1k crumple) · 1024²",
                    "hero 1 m = 1300 px · orthos 1 m = 650 px",
                    [("orpiment gold", GOLD), ("deep gold", DEEP), ("gold highlight", HIGH), ("tomb dust", DUST),
                     ("linen shroud", LINEN)], seed=81)
    cx, cy = 360, 420
    gid = sh.rad([(0, "#3A2C12", .8), (1, "#14120E", 0)])
    sh.back.append(f'<ellipse cx="{cx}" cy="{cy + 20}" rx="330" ry="300" fill="url(#{gid})"/>')
    # linen shroud under it (environment prop, not part of the pick-up)
    sh.path(smooth_path([(130, 650), (260, 628), (470, 632), (600, 655), (560, 680), (330, 686), (150, 676)]),
            f"url(#{sh.lin(LINEN, 'v', .1, .6)})", darken(LINEN, .6), 1, op=.55)
    sh.ellipse(cx + 10, 662, 190, 14, "#0E0C09", op=.55)
    P, Q, d = mask_face(sh, cx, cy, 1.0, turn=0.8)
    # fold line (crumple) along the brow
    crack(sh, [P(-175, -80), Q(-60, -82), Q(20, -78), P(150, -84)], 1.6)
    sh.text(cx, 188, "CRUMPLES > 7 m/s: folds along the brow", 10, MADDER, "middle")
    sh.text(cx, 202, "1st crumple worth 700 · 2nd: gold ball 350", 10, "#9A9078", "middle")
    # grabs
    c = P(0, 199)
    grab(sh, *c, c[0] - 80, c[1] + 44, "GRAB (CHIN)")
    e1 = P(-205, -20); e2 = P(205, -20)
    grab(sh, *e1, e1[0] - 10, e1[1] - 70, "GRAB (EAR)")
    grab(sh, *e2, e2[0] + 20, e2[1] - 70, "GRAB (EAR)")
    # orthos: front at half scale, side profile
    mask_face(sh, 820, 690 - 0.155 * 650, 0.5, turn=1.0, detail=False)
    # side profile: dished sheet 0.08 deep
    sx, gy, k = 1060, 690, 650
    prof = [(0.0, 0.31), (0.03, 0.30), (0.05, 0.26), (0.06, 0.22), (0.075, 0.19), (0.068, 0.175), (0.064, 0.16),
            (0.07, 0.145), (0.062, 0.12), (0.066, 0.10), (0.05, 0.06), (0.03, 0.02), (0.0, 0.0)]
    outer = [(sx - 0.03 * k + x * k, gy - z * k) for x, z in prof]
    inner = [(sx - 0.03 * k + (x - 0.012) * k * .75, gy - z * k) for x, z in reversed(prof)]
    sh.path(smooth_path(outer + inner, tension=.3), f"url(#{sh.lin(GOLD, 'h', .15, .55)})", darken(DEEP, .45), 1.4)
    sh.path(smooth_path(outer, closed=False, tension=.3), "none", HIGH, 1.6, op=.8)
    sh.ellipse(sx - 0.03 * k + 8, gy - 0.20 * k, 0.02 * k, 0.03 * k, GOLD, darken(DEEP, .4), 1)   # ear tab edge
    sh.line(sx - 0.03 * k, 696, sx + 0.05 * k, 696, "#635C4C", .8)
    sh.text(sx + 0.01 * k, 708, "0.08", 10, "#635C4C", "middle")
    sh.line(820 - 0.13 * k, 696, 820 + 0.13 * k, 696, "#635C4C", .8)
    sh.text(820, 708, "0.26", 10, "#635C4C", "middle")
    sh.line(1140, 690, 1140, 690 - 0.31 * k, "#635C4C", .8)
    sh.text(1146, 690 - 0.155 * k + 4, "0.31", 10, "#635C4C")
    view_label(sh, 360, "THREE-QUARTER")
    view_label(sh, 820, "FRONT")
    view_label(sh, 1060, "SIDE")
    scale_bar(sh, 100, 706, 130, "0.1 m = 130 px (hero)")
    sh.callouts([
        (*Q(80, -110), "RAISED BROWS", "arcs with incised hatch"),
        (*Q(70, -55), "CLOSED EYES", "almond lids, incised lashes"),
        (*Q(12, -20), "NOSE RIDGE", "rubbed brightest"),
        (*Q(80, 10), "MOUSTACHE", "two upturned curls 0.05 m"),
        (*Q(90, 140), "BEARD", "parallel incisions, tomb dust"),
        (*P(-100, 110), "OLD CRUMPLE", "lower left cheek, keep")], 700, 150, 400, slope=0.5)
    return sh
