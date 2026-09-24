from lib import *

EBONY = "#1A1714"; WAL = "#3B2A1E"; GILT = "#C9A227"; PANEL = "#6E7A62"; BONE = "#DCD2BA"; BRZ = "#8A6A3E"
W, D = 0.95, 0.55
ZS, ZC, ZK = 0.42, 1.14, 1.20  # stand top, carcass top, cornice top


def quad(q, u, v):
    (x0, y0), (x1, y1), (x2, y2), (x3, y3) = q  # bl, br, tr, tl
    bx, by = x0 + (x1 - x0) * u, y0 + (y1 - y0) * u
    tx, ty = x3 + (x2 - x3) * u, y3 + (y2 - y3) * u
    return (bx + (tx - bx) * v, by + (ty - by) * v)


def sub(q, u0, v0, u1, v1):
    return [quad(q, u0, v0), quad(q, u1, v0), quad(q, u1, v1), quad(q, u0, v1)]


def landscape(sh, q):
    sh.path(poly_path(q), f"url(#{sh.lin([(0, lit(PANEL, 0.45)), (1, PANEL)], 0, 0, 0, 1, key='sky')})", INK, 0.8)
    hills = [quad(q, 0, 0)] + [quad(q, u / 8, 0.35 + 0.12 * math.sin(u * 1.3)) for u in range(9)] + [quad(q, 1, 0)]
    sh.path(poly_path(hills), dk(PANEL, 0.3))
    hills2 = [quad(q, 0, 0)] + [quad(q, u / 8, 0.18 + 0.08 * math.cos(u * 1.9)) for u in range(9)] + [quad(q, 1, 0)]
    sh.path(poly_path(hills2), dk(PANEL, 0.5))
    for u in (0.25, 0.7):  # trees
        x, y = quad(q, u, 0.3)
        sh.circle(x, y - 3, 3, dk(PANEL, 0.6))
    sh.path(poly_path(q), "none", GILT, 1.0)


def drawers(sh, q, cols=5, rows=8):
    for r in range(rows):
        for c in range(cols):
            u0, u1 = 0.03 + c * 0.194, 0.03 + c * 0.194 + 0.18
            v0, v1 = 0.03 + r * 0.121, 0.03 + r * 0.121 + 0.105
            f = sub(q, u0, v0, u1, v1)
            sh.path(poly_path(f), f"url(#{sh.form(EBONY, 'v', 0.25, 0.4)})", INK, 0.7)
            sh.path(poly_path(sub(q, u0 + 0.012, v0 + 0.015, u1 - 0.012, v1 - 0.015)), "none", BONE, 0.6, op=0.55)
            cx, cy = quad(q, (u0 + u1) / 2, (v0 + v1) / 2)
            sh.circle(cx, cy, 2.2, "none", GILT, 1.0)


def turned_leg(sh, x, ybot, ytop, s, shade=0.0):
    h = ytop - ybot  # negative in screen space
    L = abs(h)
    prof = [(0.035, 0), (0.03, 0.10), (0.06, 0.35), (0.065, 0.55), (0.04, 0.75), (0.025, 0.82), (0.035, 0.92), (0.035, 1.0)]
    k = s
    right = [(x + w * k, ybot - t * L) for w, t in prof]
    left = [(x - w * k, ybot - t * L) for w, t in reversed(prof)]
    sh.path(smooth_path(right + left), f"url(#{sh.form(dk(WAL, shade), 'h', 0.35)})", INK, 0.9)


def hero(sh):
    P = Proj(232, 575, 310, b=22, yk=0.7)
    # floor shadow
    sh.path(smooth_path(P.pts([(0.0, -0.1, 0), (W + 0.08, -0.02, 0), (W + 0.05, D + 0.06, 0), (-0.02, D + 0.03, 0)])), "#000000", op=0.4)
    # stand: back legs, top, front legs, stretcher
    for X in (0.06, 0.475, 0.89):
        x, y = P.p(X, 0.49, 0)
        turned_leg(sh, x, y, P.p(X, 0.49, ZS - 0.04)[1], 310, 0.35)
    for Y in (0.49,):
        P.box(sh, 0.06, Y - 0.02, 0.04, W - 0.12, 0.04, 0.035, WAL)  # back stretcher
    P.box(sh, 0.455, 0.08, 0.04, 0.04, D - 0.16, 0.035, WAL)  # cross stretcher
    for X in (0.06, 0.475, 0.89):
        x, y = P.p(X, 0.06, 0)
        turned_leg(sh, x, y, P.p(X, 0.06, ZS - 0.04)[1], 310)
    P.box(sh, 0.06, 0.04, 0.04, W - 0.12, 0.04, 0.035, WAL)  # front stretcher
    P.box(sh, -0.01, -0.01, ZS - 0.04, W + 0.02, D + 0.02, 0.04, WAL)
    # carcass
    f = P.box(sh, 0, 0, ZS, W, D, ZC - ZS, EBONY, light=0.25)
    front_q = f["front"]
    drawers(sh, sub(front_q, 0.04, 0.03, 0.96, 0.97))
    # right side panel moulding + drop handle
    rq = f["right"]
    sh.path(poly_path(sub(rq, 0.1, 0.1, 0.9, 0.9)), "none", dk(BONE, 0.6), 0.9, op=0.6)
    hx, hy = quad(rq, 0.5, (1.00 - ZS) / (ZC - ZS))
    sh.path(smooth_path([(hx - 16, hy - 4), (hx - 18, hy + 8), (hx, hy + 16), (hx + 18, hy + 4), (hx + 16, hy - 8)], closed=False), "none", GILT, 2.4)
    sh.rect(hx - 20, hy - 9, 8, 6, GILT, INK, 0.6)
    sh.rect(hx + 12, hy - 14, 8, 6, GILT, INK, 0.6)
    grab(sh, hx, hy + 12, "GRAB · CARRIER 2", 12, 22)
    lx, ly = quad(front_q, 0.0, (1.00 - ZS) / (ZC - ZS))
    # cornice
    P.box(sh, -0.02, -0.02, ZC, W + 0.04, D + 0.04, 0.025, EBONY, light=0.35)
    P.box(sh, -0.035, -0.035, ZC + 0.025, W + 0.07, D + 0.07, 0.035, EBONY, light=0.4)
    sh.line([P.p(-0.035, -0.035, ZC + 0.06), P.p(W + 0.035, -0.035, ZC + 0.06), P.p(W + 0.035, D + 0.035, ZC + 0.06)], GILT, 1.0, op=0.5, smooth=False)
    # pediment + finial
    ped = P.pts([(0.255, -0.035, ZK), (0.695, -0.035, ZK), (0.475, -0.035, ZK + 0.07)])
    sh.path(poly_path(ped), f"url(#{sh.form(EBONY, 'v', 0.4)})", INK, 0.9)
    sh.path(poly_path(ped), "none", GILT, 0.9, op=0.7)
    fx, fy = P.p(0.475, -0.035, ZK + 0.095)
    sh.circle(fx, fy, 0.025 * 310, f"url(#{sh.form(GILT, 'sphere', 0.5)})", INK, 0.8)
    # gilt corner mounts
    for X, Z in ((0, ZS), (W, ZS), (0, ZC), (W, ZC)):
        x, y = P.p(X, 0, Z)
        sh.path(poly_path([(x - 6, y), (x, y - 7 if Z == ZS else y + 7), (x + 6, y)]), GILT, INK, 0.6)
    # doors open ~90°: left door plane at X=0 swinging toward viewer (inside face shows), right door at X=W
    for X, inside in ((0.0, True), (W, False)):
        dq = P.pts([(X, 0, ZS + 0.01), (X, -0.45, ZS + 0.01), (X, -0.45, ZC - 0.01), (X, 0, ZC - 0.01)])
        dq = [dq[1], dq[0], dq[3], dq[2]] if X == 0 else [dq[0], dq[1], dq[2], dq[3]]
        edge = P.pts([(X, -0.45, ZS + 0.01), (X - 0.02, -0.45, ZS + 0.01), (X - 0.02, -0.45, ZC - 0.01), (X, -0.45, ZC - 0.01)])
        sh.path(poly_path(edge), dk(EBONY, 0.2), INK, 0.8)
        sh.path(poly_path(dq), f"url(#{sh.form(EBONY, 'h', 0.3 if inside else 0.2)})", INK, 1.0)
        sh.path(poly_path(sub(dq, 0.07, 0.06, 0.93, 0.94)), "none", dk(BONE, 0.5), 1.0, op=0.6)
        landscape(sh, sub(dq, 0.16, 0.18, 0.84, 0.80))
        sh.flecks(*dq[3], *dq[1], 6, dk(BONE, 0.7), 0.6, 1.2, (0.15, 0.35))
    grab(sh, lx - 6, ly, "GRAB · CARRIER 1 (far side)", -14, -14, "end", hidden=True)
    # escutcheon on right door edge
    ex, ey = P.p(W, -0.43, (ZS + ZC) / 2)
    sh.rect(ex - 4, ey - 7, 8, 14, GILT, INK, 0.6)
    # fracture lines: carcass/stand, top, side
    fracture(sh, [P.p(-0.01, -0.01, ZS + 0.005), P.p(W + 0.01, -0.01, ZS + 0.005), P.p(W + 0.01, D + 0.01, ZS + 0.005)])
    fracture(sh, [P.p(0, 0, ZC - 0.02), P.p(W, 0, ZC - 0.02), P.p(W, D, ZC - 0.02)])
    fracture(sh, [P.p(W, 0.0, ZS + 0.02), P.p(W, 0.0, ZC - 0.03)])
    fracture(sh, [P.p(0.475, 0.06, 0.12), P.p(0.475, 0.06, 0.28)])


def front_ortho(sh):
    F = Fig(830, 250)
    sh.ellipse(830, 690, 125, 5, "#000000", op=0.4)
    for x in (-0.415, 0.0, 0.415):
        turned_leg(sh, *F.p(x, 0), F.p(0, ZS - 0.04)[1], 250)
    sh.shape(F.pts([(-0.465, 0.03), (0.465, 0.03), (0.465, 0.065), (-0.465, 0.065)]), WAL, smooth=False, direction="v")
    sh.shape(F.pts([(-0.485, ZS - 0.04), (0.485, ZS - 0.04), (0.485, ZS), (-0.485, ZS)]), WAL, smooth=False, direction="v")
    sh.shape(F.pts([(-W / 2, ZS), (W / 2, ZS), (W / 2, ZC), (-W / 2, ZC)]), EBONY, smooth=False, direction="h", light=0.25)
    for s in (-1, 1):
        dq = F.pts([(s * 0.005, ZS + 0.01), (s * 0.455, ZS + 0.01), (s * 0.455, ZC - 0.01), (s * 0.005, ZC - 0.01)])
        sh.path(poly_path(dq), "none", INK, 1.2)
        sh.path(poly_path(sub(dq, 0.08, 0.06, 0.92, 0.94)), "none", dk(BONE, 0.5), 1.0, op=0.6)
        landscape(sh, sub(dq, 0.17, 0.19, 0.83, 0.81))
    sh.rect(*F.p(-0.012, 0.80), 6, 12, GILT, INK, 0.6)
    sh.shape(F.pts([(-0.495, ZC), (0.495, ZC), (0.51, ZC + 0.06), (-0.51, ZC + 0.06)]), EBONY, smooth=False, direction="v", light=0.4)
    sh.shape(F.pts([(-0.20, ZK), (0.20, ZK), (0.0, ZK + 0.05)]), EBONY, smooth=False, direction="v", light=0.4)
    sh.circle(*F.p(0, ZK + 0.075), 0.025 * 250, f"url(#{sh.form(GILT, 'sphere', 0.5)})", INK, 0.8)
    for x, z in ((-W / 2, ZS), (W / 2, ZS), (-W / 2, ZC), (W / 2, ZC)):
        cx, cy = F.p(x, z)
        sh.path(poly_path([(cx - 6, cy), (cx, cy - 7 if z == ZS else cy + 7), (cx + 6, cy)]), GILT, INK, 0.6)
    for s in (-1, 1):  # drop handles in profile at the sides
        x, y = F.p(s * (W / 2 + 0.01), 1.00)
        sh.line([(x, y - 6), (x + s * 7, y + 2), (x, y + 10)], GILT, 2.2)
        grab(sh, x + s * 6, y + 4, "", 0, 0)


def side_ortho(sh):
    F = Fig(1068, 250)
    sh.ellipse(1068, 690, 75, 5, "#000000", op=0.4)
    for x in (-0.215, 0.215):
        turned_leg(sh, *F.p(x, 0), F.p(0, ZS - 0.04)[1], 250)
    sh.shape(F.pts([(-0.26, 0.03), (0.26, 0.03), (0.26, 0.065), (-0.26, 0.065)]), WAL, smooth=False, direction="v")
    sh.shape(F.pts([(-0.285, ZS - 0.04), (0.285, ZS - 0.04), (0.285, ZS), (-0.285, ZS)]), WAL, smooth=False, direction="v")
    q = F.pts([(-D / 2, ZS), (D / 2, ZS), (D / 2, ZC), (-D / 2, ZC)])
    sh.path(poly_path(q), f"url(#{sh.form(EBONY, 'h', 0.2)})", INK, 1.1)
    sh.path(poly_path(sub(q, 0.1, 0.1, 0.9, 0.9)), "none", dk(BONE, 0.6), 0.9, op=0.6)
    sh.shape(F.pts([(-0.295, ZC), (0.295, ZC), (0.31, ZC + 0.06), (-0.31, ZC + 0.06)]), EBONY, smooth=False, direction="v", light=0.4)
    sh.shape(F.pts([(-0.31, ZK), (-0.28, ZK), (-0.28, ZK + 0.05), (-0.31, ZK + 0.05)]), EBONY, smooth=False)
    x, y = F.p(0, 1.00)
    sh.path(smooth_path([(x - 16, y - 4), (x - 18, y + 8), (x, y + 16), (x + 18, y + 8), (x + 16, y - 4)], closed=False), "none", GILT, 2.4)
    for dx in (-20, 12):
        sh.rect(x + dx, y - 8, 8, 6, GILT, INK, 0.6)
    grab(sh, x, y + 14, "GRAB", 12, 4)
    # door swing (dashed) on the front edge
    sh.line([F.p(-D / 2, ZS + 0.01), F.p(-D / 2 - 0.10, ZS + 0.01)], FRAME_TXT, 0.8, dash="3 3", smooth=False)


def build():
    sh = Sheet(seed=21)
    hero(sh)
    front_ortho(sh)
    side_ortho(sh)
    scale_bar(sh, 700, 336, 250, 0.5, "0.5 m = 125 px (orthos)")
    callouts(sh, [
        (380, 180, "PEDIMENT + GILT FINIAL", "ebony · orpiment ball 0.05 m", 700, 150, "start"),
        (330, 330, "40 DRAWERS, BONE STRINGING", "0.17 × 0.08 m faces · gilt rings", 700, 205, "start"),
        (178, 400, "PAINTED COPPER PANEL", "landscape inside each door", 700, 260, "start"),
        (555, 470, "DROP HANDLE", "gilt bail 0.14 m at 1.00 m", 950, 150, "start"),
        (420, 620, "TURNED BULB LEGS ×6", "walnut stand 0.42 m", 950, 205, "start"),
        (540, 300, "EBONY CARCASS", "0.95 × 0.55 × 0.72 m", 950, 260, "start"),
    ])
    sh.text(700, 298, "- - -  fracture: splits into 5 pieces,", 10.5, "#C4542E")
    sh.text(700, 312, "       drawers spill 6–10 curios", 10.5, "#C4542E")
    return sh.render("THE AGE OF POWDER · PLUNDER · 1200 COIN · 12 ST", "Cabinet of Curiosities",
                     "0.95 × 0.55 × 1.30 m · ≤ 5k tris · 1024²", "hero 1 m ≈ 310 px · orthos 1 m = 250 px · two carriers",
                     [(EBONY, "ebony"), (WAL, "black walnut"), (GILT, "gilt bronze"), (PANEL, "painted copper"), (BONE, "bone inlay")],
                     view_labels=[(380, "HERO ¾"), (830, "FRONT"), (1068, "SIDE")], ladder=None, human=False)
