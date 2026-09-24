from lib import *
from cabinet import quad, sub

SILV = "#A8ABA2"; FOX = "#5E5F58"; GLASS = "#7E9377"; WAL = "#3B2A1E"; GILT = "#C9A227"; PINE = "#9A8260"; IRON = "#34404E"
FW, FH, CREST = 0.80, 0.96, 0.14
RAIL, SLIP = 0.09, 0.02


def crest_pts(cx, base_y, w, h):
    """Acanthus crest outline in pixel space, centred cx, sitting on base_y, w/h in px."""
    pts = []
    for i in range(0, 41):
        t = i / 40
        x = cx - w / 2 + w * t
        env = math.sin(math.pi * t) ** 0.8
        y = base_y - h * env * (0.75 + 0.25 * math.cos(t * math.pi * 8))
        pts.append((x, y))
    return [(cx + w / 2, base_y)] + [(cx - w / 2, base_y)] + pts[1:-1]


def mirror_face(sh, q, fracture_on=False, reflect=True):
    """q = outer frame quad (bl, br, tr, tl)."""
    u_r = RAIL / FW
    v_r = RAIL / FH
    u_s = (RAIL + SLIP) / FW
    v_s = (RAIL + SLIP) / FH
    sh.path(poly_path(q), f"url(#{sh.form(WAL, 'd', 0.35)})", INK, 1.2)
    # ogee moulding highlights / grain
    inner = sub(q, u_r * 0.5, v_r * 0.5, 1 - u_r * 0.5, 1 - v_r * 0.5)
    sh.path(poly_path(inner), "none", lit(WAL, 0.35), 1.2, op=0.6)
    for k in range(14):
        u = 0.02 + k * 0.07
        sh.line([quad(q, u, 0.01), quad(q, u + 0.02, v_r * 0.9)], dk(WAL, 0.3), 0.7, op=0.5, smooth=False)
        sh.line([quad(q, u, 1 - v_r * 0.9), quad(q, u + 0.02, 0.99)], dk(WAL, 0.3), 0.7, op=0.5, smooth=False)
    # gilt slip bead-and-reel
    slip = sub(q, u_r, v_r, 1 - u_r, 1 - v_r)
    sh.path(poly_path(slip), f"url(#{sh.form(GILT, 'd', 0.4)})", INK, 0.8)
    for k in range(24):
        for (a, b) in (((k / 24, v_r + 0.008), None), ((k / 24, 1 - v_r - 0.008), None)):
            u, v = a
            if u_r < u < 1 - u_r:
                x, y = quad(q, u, v)
                sh.circle(x, y, 1.3, dk(GILT, 0.35))
    glass = sub(q, u_s, v_s, 1 - u_s, 1 - v_s)
    g = sh.lin([(0, "#C4C7BE"), (0.35, SILV), (0.7, dk(SILV, 0.25)), (1, dk(SILV, 0.4))], 0, 0, 1, 1)
    sh.path(poly_path(glass), f"url(#{g})", INK, 1.0)
    d = poly_path(glass)
    sh.clip_open(d)
    if reflect:
        # reflected room: window light and a guard silhouette (morion) far behind
        wq = sub(glass, 0.12, 0.35, 0.38, 0.92)
        sh.path(poly_path(wq), lit(GLASS, 0.3), op=0.35)
        for u in (0.33, 0.66):
            sh.line([quad(wq, u, 0), quad(wq, u, 1)], FOX, 1.2, op=0.5, smooth=False)
        sh.line([quad(wq, 0, 0.6), quad(wq, 1, 0.6)], FOX, 1.2, op=0.5, smooth=False)
        gx, gy = quad(glass, 0.68, 0.10)
        tx, ty = quad(glass, 0.68, 0.62)
        hgt = gy - ty
        sil = [(gx - hgt * 0.10, gy), (gx - hgt * 0.13, gy - hgt * 0.45), (gx - hgt * 0.16, gy - hgt * 0.72), (gx - hgt * 0.06, gy - hgt * 0.80),
               (gx - hgt * 0.05, gy - hgt * 0.88), (gx - hgt * 0.13, gy - hgt * 0.90), (gx, gy - hgt * 1.0), (gx + hgt * 0.13, gy - hgt * 0.90),
               (gx + hgt * 0.05, gy - hgt * 0.88), (gx + hgt * 0.06, gy - hgt * 0.80), (gx + hgt * 0.16, gy - hgt * 0.72), (gx + hgt * 0.13, gy - hgt * 0.45), (gx + hgt * 0.10, gy)]
        sh.path(smooth_path(sil), FOX, op=0.55)
        sh.line([(gx + hgt * 0.22, gy), (gx + hgt * 0.22, gy - hgt * 1.25)], FOX, 1.5, op=0.5, smooth=False)
        # diagonal sheen
        sh.path(poly_path([quad(glass, 0.0, 0.55), quad(glass, 0.0, 0.75), quad(glass, 0.75, 1.0), quad(glass, 0.5, 1.0)]), "#E4E4DC", op=0.18)
        sh.path(poly_path([quad(glass, 0.0, 0.3), quad(glass, 0.0, 0.36), quad(glass, 0.9, 1.0), quad(glass, 0.82, 1.0)]), "#E4E4DC", op=0.12)
    # foxing band at the edges
    fg = sh.rad([(0, FOX, 0), (0.7, FOX, 0), (1, FOX, 0.75)], 0.5, 0.5, 0.72, key="fox")
    sh.path(d, f"url(#{fg})")
    sh.flecks(*glass[3], *glass[1], 26, FOX, 0.8, 2.4, (0.2, 0.5))
    sh.close()
    # bevel line
    sh.path(poly_path(sub(glass, 0.03, 0.025, 0.97, 0.975)), "none", GLASS, 1.0, op=0.55)
    if fracture_on:
        c = quad(glass, 0.55, 0.45)
        for (u, v) in ((0, 0.1), (0.2, 1), (1, 0.8), (0.95, 0), (0.0, 0.62), (0.62, 1.0), (0.4, 0)):
            e = quad(glass, u, v)
            m = ((c[0] + e[0]) / 2 + 6, (c[1] + e[1]) / 2 - 4)
            fracture(sh, [c, m, e])
    # rosettes
    for u, v in ((u_r / 2, v_r / 2), (1 - u_r / 2, v_r / 2), (1 - u_r / 2, 1 - v_r / 2), (u_r / 2, 1 - v_r / 2)):
        x, y = quad(q, u, v)
        sh.circle(x, y, 5, f"url(#{sh.form(GILT, 'sphere', 0.5)})", INK, 0.7)
        for k in range(6):
            a = k * math.pi / 3
            sh.circle(x + 4 * math.cos(a), y + 4 * math.sin(a), 1.4, dk(GILT, 0.2))
    return glass


def hero(sh):
    s = 380
    P = Proj(200, 610, s, a=16, b=28, yk=0.7)
    T = 0.05
    sh.path(smooth_path(P.pts([(-0.05, -0.05, 0), (FW + 0.1, -0.05, 0), (FW + 0.1, 0.25, 0), (-0.05, 0.25, 0)])), "#000000", op=0.4)
    # thickness: right edge + top edge
    sh.path(poly_path(P.pts([(FW, 0, 0), (FW, T, 0), (FW, T, FH), (FW, 0, FH)])), dk(WAL, 0.3), INK, 1)
    sh.path(poly_path(P.pts([(0, 0, FH), (FW, 0, FH), (FW, T, FH), (0, T, FH)])), lit(WAL, 0.15), INK, 1)
    q = P.pts([(0, 0, 0), (FW, 0, 0), (FW, 0, FH), (0, 0, FH)])
    glass = mirror_face(sh, q, fracture_on=True)
    # crest
    bl = P.p(FW / 2 - 0.20, 0, FH)
    br = P.p(FW / 2 + 0.20, 0, FH)
    cx = (bl[0] + br[0]) / 2
    cy = (bl[1] + br[1]) / 2
    pts = crest_pts(cx, cy, br[0] - bl[0], CREST * s)
    ang = math.degrees(math.atan2(br[1] - bl[1], br[0] - bl[0]))
    sh.add(f'<g transform="rotate({ang:.2f} {cx:.1f} {cy:.1f})">')
    sh.path(smooth_path(pts, tension=0.35), f"url(#{sh.form(GILT, 'v', 0.45)})", INK, 1)
    for k in range(5):
        x = cx - 50 + k * 25
        sh.circle(x, cy - 18 - 8 * math.sin(k), 5, "none", dk(GILT, 0.4), 1.4)
    sh.circle(cx, cy - CREST * s * 0.72, 6, dk(GILT, 0.5))
    sh.close()
    # grabs on side rails at 0.45 and 0.65 m
    for z in (0.45, 0.65):
        x, y = P.p(0.0, 0, z)
        grab(sh, x - 2, y, "GRAB", -14, 4, "end")
        x, y = P.p(FW, T / 2, z)
        grab(sh, x + 4, y, "GRAB", 14, 4)
    return P


def front_ortho(sh):
    F = Fig(850, 300)
    sh.ellipse(850, 690, 130, 5, "#000000", op=0.4)
    q = F.pts([(-FW / 2, 0), (FW / 2, 0), (FW / 2, FH), (-FW / 2, FH)])
    mirror_face(sh, q, reflect=False)
    bl, br = F.p(-0.20, FH), F.p(0.20, FH)
    pts = crest_pts((bl[0] + br[0]) / 2, bl[1], br[0] - bl[0], CREST * 300)
    sh.path(smooth_path(pts, tension=0.35), f"url(#{sh.form(GILT, 'v', 0.45)})", INK, 1)
    sh.circle(850, bl[1] - CREST * 300 * 0.72, 5, dk(GILT, 0.5))
    for z in (0.45, 0.65):
        grab(sh, *F.p(-FW / 2 - 0.005, z), "", 0, 0)
        grab(sh, *F.p(FW / 2 + 0.005, z), "", 0, 0)


def side_ortho(sh):
    F = Fig(1085, 300)
    sh.ellipse(1085, 690, 30, 4, "#000000", op=0.4)
    # frame profile: walnut rail with ogee front, pine back, ring
    prof = F.pts([(-0.04, 0), (0.02, 0), (0.04, 0.02), (0.04, FH - 0.02), (0.02, FH), (-0.04, FH)])
    sh.path(poly_path(prof), f"url(#{sh.form(WAL, 'h', 0.3)})", INK, 1)
    sh.path(poly_path(F.pts([(-0.04, 0.01), (-0.03, 0.01), (-0.03, FH - 0.01), (-0.04, FH - 0.01)])), PINE, INK, 0.7)
    sh.line([F.p(0.035, 0.03), F.p(0.035, FH - 0.03)], lit(WAL, 0.35), 1.0, op=0.6, smooth=False)
    sh.path(poly_path(F.pts([(0.015, FH), (0.03, FH), (0.03, FH + CREST), (0.015, FH + CREST * 0.9)])), f"url(#{sh.form(GILT, 'h', 0.4)})", INK, 0.8)
    x, y = F.p(-0.045, FH - 0.04)
    sh.circle(x - 6, y, 7, "none", IRON, 2)
    for z in (0.2, 0.75):
        x, y = F.p(-0.04, z)
        sh.rect(x - 5, y - 2, 5, 4, IRON)
    for z in (0.45, 0.65):
        grab(sh, *F.p(0.0, z), "", 0, 0)
    sh.text(1085 + 30, 690 - 300 * 0.5, "0.08 m", 10, FRAME_TXT)


def build():
    sh = Sheet(seed=22)
    P = hero(sh)
    front_ortho(sh)
    side_ortho(sh)
    scale_bar(sh, 700, 336, 300, 0.5, "0.5 m = 150 px (orthos)")
    callouts(sh, [
        (*P.p(FW / 2 + 0.02, 0, FH + 0.09), "PIERCED ACANTHUS CREST", "gilt · 0.40 × 0.14 m", 700, 150, "start"),
        (*P.p(0.3, 0, 0.6), "MURANO PLATE, SILVERED", "0.58 × 0.74 m · bevel 2 cm", 700, 205, "start"),
        (*P.p(0.4, 0, FH - RAIL - 0.01), "GILT BEAD-AND-REEL SLIP", "0.02 m", 700, 260, "start"),
        (*P.p(0.5, 0, 0.045), "WALNUT CUSHION FRAME", "0.09 m rails · mitred", 950, 150, "start"),
        (*P.p(FW - RAIL / 2, 0, FH - RAIL / 2), "GILT ROSETTES ×4", "0.04 m corners", 950, 205, "start"),
        (1073, 690 - 300 * FH + 12, "PINE BACK + IRON RING", "turn-buttons ×4", 950, 260, "start"),
    ])
    sh.text(700, 298, "- - -  fracture: 8–12 shards, frame survives", 10.5, "#C4542E")
    sh.text(700, 312, "       (90 coin, 3 st)", 10.5, "#C4542E")
    sh.text(120, 708, "reflection: it shows who follows you", 10.5, FRAME_TXT)
    return sh.render("THE AGE OF POWDER · PLUNDER · 700 COIN · 6 ST", "Venetian Mirror",
                     "0.80 × 0.08 × 1.10 m · ≤ 3k tris · 1024²", "hero 1 m ≈ 380 px · orthos 1 m = 300 px · two hands",
                     [(SILV, "silvering"), (GLASS, "glass edge"), (WAL, "black walnut"), (GILT, "gilt"), (PINE, "pine back")],
                     view_labels=[(380, "HERO ¾"), (850, "FRONT"), (1085, "SIDE")], ladder=None, human=False)
