from lib import *

GLAZE, BLACK, CORE, HIGH = "#3E86A8", "#1E1A16", "#E6DDC8", "#8CC0D0"
POOL = "#2E6680"


def lotus(sh, x, y, s, rot=0, bud=False):
    g = [f'<g transform="translate({f(x)} {f(y)}) rotate({f(rot)}) scale({f(s)})" fill="none" stroke="{BLACK}" stroke-width="{f(1.6 / s)}" stroke-linecap="round">']
    if bud:
        g.append('<path d="M0 0 C-5 -8 -4 -16 0 -22 C4 -16 5 -8 0 0 Z"/><path d="M0 0 C0 10 -2 20 -6 30"/>')
    else:
        g.append('<path d="M0 0 C-10 -6 -14 -16 -12 -24 C-6 -18 -3 -10 0 0 Z"/>'
                 '<path d="M0 0 C-3 -10 -3 -20 0 -28 C3 -20 3 -10 0 0 Z"/>'
                 '<path d="M0 0 C10 -6 14 -16 12 -24 C6 -18 3 -10 0 0 Z"/>'
                 '<path d="M-6 2 C-2 5 2 5 6 2"/><path d="M0 4 C1 14 -1 24 -4 34"/>')
    g.append("</g>")
    return "".join(g)


def hippo_hero(sh, ox, oy, k=1.0):
    def P(x, y):
        return (ox + x * k * 0.85, oy + y * k * 1.05)

    def pts(lst):
        return [P(x, y) for x, y in lst]

    shade = lambda b: f"url(#{sh.lin(b, 'v', .3, .5)})"
    # far legs
    for x in (-55, 165):
        d = smooth_path(pts([(x - 20, -70), (x + 20, -70), (x + 22, -4), (x - 22, -4)]))
        sh.path(d, darken(GLAZE, .35), darken(GLAZE, .7), 1.2)
    # body
    body = pts([(-120, -175), (-40, -205), (60, -208), (150, -190), (200, -150), (205, -100), (170, -60), (60, -45),
                (-60, -48), (-130, -70), (-150, -120)])
    db = smooth_path(body)
    sh.path(db, f"url(#{sh.lin(GLAZE, 'v', .35, .55)})", darken(GLAZE, .7), 1.6)
    # pooled darker glaze on belly
    gid = sh.rad([(0, POOL, .0), (0.7, POOL, .3), (1, POOL, .9)], cy=.35, r=.65)
    sh.clipped(db, f'<ellipse cx="{f(ox + 30 * k)}" cy="{f(oy - 140 * k)}" rx="{f(200 * k)}" ry="{f(100 * k)}" fill="url(#{gid})"/>')
    # lotus decoration on flank (clipped to body)
    deco = [lotus(sh, *P(-20, -110), 1.3 * k, -8), lotus(sh, *P(70, -120), 1.4 * k, 6), lotus(sh, *P(150, -105), 1.1 * k, 18, True),
            lotus(sh, *P(20, -175), 1.0 * k, -2, True), lotus(sh, *P(110, -175), 0.9 * k, 10)]
    # reeds
    for x in (-90, 0, 100, 175):
        deco.append(f'<path d="M{f(P(x, -50)[0])} {f(P(x, -50)[1])} q{f(6 * k)} {f(-40 * k)} {f(-2 * k)} {f(-85 * k)}" stroke="{BLACK}" stroke-width="1.3" fill="none"/>')
    # butterfly on hip
    bx, by = P(160, -150)
    deco.append(f'<path d="M{f(bx)} {f(by)} l-9 -8 l-2 10 z M{f(bx)} {f(by)} l9 -8 l2 10 z" fill="{BLACK}"/>')
    sh.clipped(db, "".join(deco))
    # glaze highlights on the back
    sh.path(smooth_path(pts([(-40, -196), (40, -202), (120, -192)]), closed=False), "none", HIGH, 5, op=.55)
    sh.path(smooth_path(pts([(-20, -192), (40, -196)]), closed=False), "none", lighten(HIGH, .5), 2, op=.8)
    sh.flecks(db, (ox - 150 * k, oy - 210 * k, ox + 210 * k, oy - 40 * k), 40, HIGH, .5, 1.2, .5)
    # tail
    sh.path(smooth_path(pts([(200, -140), (214, -132), (206, -122)])), GLAZE, darken(GLAZE, .7), 1.2)
    # near legs
    for x in (-95, 125):
        d = smooth_path(pts([(x - 24, -80), (x + 24, -80), (x + 26, -2), (x - 26, -2)]))
        sh.path(d, f"url(#{sh.lin(GLAZE, 'h', .3, .5)})", darken(GLAZE, .7), 1.4)
        sh.ellipse(*P(x, -2), 26 * k, 6 * k, POOL, darken(GLAZE, .7), 1)
        sh.circle(*P(x + 14, -6), 3.5 * k, CORE)          # chip showing white core
        sh.path(smooth_path(pts([(x - 18, -60), (x - 14, -10)]), closed=False), "none", BLACK, 1.2)   # reed on leg
    # head
    head = pts([(-120, -172), (-150, -190), (-185, -178), (-230, -160), (-245, -120), (-235, -85), (-190, -70),
                (-140, -75), (-120, -110)])
    dh = smooth_path(head)
    sh.path(dh, f"url(#{sh.lin(GLAZE, 'h', .4, .5)})", darken(GLAZE, .7), 1.6)
    sh.clipped(dh, f'<ellipse cx="{f(P(-180, -70)[0])}" cy="{f(P(-180, -70)[1])}" rx="{f(80 * k)}" ry="{f(30 * k)}" fill="{POOL}" opacity=".6"/>')
    sh.path(smooth_path(pts([(-240, -110), (-205, -100), (-160, -104)]), closed=False), "none", BLACK, 1.6)   # mouth
    for x, y in ((-228, -150), (-210, -156)):
        sh.ellipse(*P(x, y), 6 * k, 4 * k, POOL, BLACK, 1.1)          # nostrils
    for x, y in ((-165, -188), (-138, -194)):
        sh.circle(*P(x, y), 9 * k, GLAZE, darken(GLAZE, .7), 1.2)     # eye knobs
        sh.circle(*P(x, y + 1), 4 * k, BLACK)
    for x, y in ((-128, -206), (-112, -208)):
        sh.ellipse(*P(x, y), 5 * k, 8 * k, GLAZE, darken(GLAZE, .7), 1)   # ears
    sh.path(smooth_path(pts([(-200, -168), (-160, -176)]), closed=False), "none", HIGH, 3, op=.6)
    return P, db


def build():
    sh = item_sheet("Faience Hippopotamus", 420, 0.5, "0.20 × 0.08 × 0.11 m", "≤ 1.5k tris · 1024²",
                    "hero 1 m = 2000 px · orthos 1 m = 1000 px",
                    [("faience glaze", GLAZE), ("manganese black", BLACK), ("quartz core", CORE),
                     ("glaze highlight", HIGH)], seed=71)
    ox, oy = 380, 640
    gid = sh.rad([(0, "#2A2012", .9), (1, "#14120E", 0)])
    sh.back.append(f'<ellipse cx="{ox}" cy="520" rx="330" ry="220" fill="url(#{gid})"/>')
    sh.ellipse(ox, oy + 4, 250, 16, "#0E0C09", op=.6)
    P, db = hippo_hero(sh, ox, oy, 1.0)
    # fractures: head off, body split front/back, legs crumble
    crack(sh, [P(-122, -180), P(-128, -140), P(-138, -100), P(-140, -70)])
    crack(sh, [P(40, -208), P(30, -160), P(45, -110), P(35, -48)])
    for x in (-95, 125):
        crack(sh, [P(x - 24, -60), P(x, -52), P(x + 24, -62)], 1.2)
    sh.text(ox - 60, oy + 36, "4 pieces: head · body front · body back · legs (crumble)", 10, MADDER, "middle")
    grab(sh, *P(40, -60), P(40, -60)[0] - 30, P(40, -60)[1] + 55, "GRAB (BELLY)")
    grab(sh, *P(-200, -78), P(-200, -78)[0] - 30, P(-200, -78)[1] + 40, "GRAB (CHIN)")
    # orthos, k=1000 -> 0.5 of hero drawing scale (hero is 2000 px/m and drawn at 1 px = 0.5 mm)
    # SIDE: reuse the drawing at half scale
    hippo_hero(sh, 1040, 690, 0.5)
    # FRONT
    fx, gy = 800, 690
    sh.path(smooth_path([(fx - 40, gy - 30), (fx - 42, gy - 80), (fx - 20, gy - 105), (fx + 20, gy - 105), (fx + 42, gy - 80),
                         (fx + 40, gy - 30)]), f"url(#{sh.lin(GLAZE, 'h', .35, .5)})", darken(GLAZE, .7), 1.2)
    for s in (-1, 1):
        sh.path(poly_path([(fx + s * 30 - 11, gy - 40), (fx + s * 30 + 11, gy - 40), (fx + s * 30 + 12, gy), (fx + s * 30 - 12, gy)]),
                GLAZE, darken(GLAZE, .7), 1)
    sh.ellipse(fx, gy - 60, 30, 28, f"url(#{sh.lin(GLAZE, 'h', .4, .5)})", darken(GLAZE, .7), 1.2)
    for s in (-1, 1):
        sh.ellipse(fx + s * 10, gy - 76, 4, 3, POOL, BLACK, .8)
        sh.circle(fx + s * 18, gy - 96, 5, GLAZE, darken(GLAZE, .7), 1)
        sh.circle(fx + s * 18, gy - 96, 2, BLACK)
    sh.path(f"M{fx - 20} {gy - 48} Q{fx} {gy - 42} {fx + 20} {gy - 48}", "none", BLACK, 1.2)
    sh.line(fx - 40, 696, fx + 40, 696, "#635C4C", .8)
    sh.text(fx, 708, "0.08", 10, "#635C4C", "middle")
    sh.line(1040 - 104, 696, 1040 + 91, 696, "#635C4C", .8)
    sh.text(1040, 708, "0.20", 10, "#635C4C", "middle")
    view_label(sh, 380, "THREE-QUARTER")
    view_label(sh, 800, "FRONT")
    view_label(sh, 1040, "SIDE")
    scale_bar(sh, 100, 706, 200, "0.1 m = 200 px (hero)")
    sh.callouts([
        (*P(-150, -186), "EYE KNOBS + EARS", "raised, 0.012 m ears"),
        (*P(-225, -140), "BLOCKY MUZZLE", "0.06 m wide, nostril bumps"),
        (*P(70, -120), "LOTUS + REEDS", "manganese black line, 1–2 mm"),
        (*P(60, -200), "GLOSSY GLAZE", "roughness 0.15, pooled in folds"),
        (*P(139, -8), "CHIPPED FOOT", "white quartz frit core"),
        (*P(160, -150), "BUTTERFLY", "painted on the hip")], 700, 150, 410, slope=0.4)
    sh.text(1060, 520, "BREAKS > 2 m/s", 12, MADDER, "middle", ls="2")
    sh.text(1060, 536, "glass-fragile · no spill", 10, "#9A9078", "middle")
    return sh
