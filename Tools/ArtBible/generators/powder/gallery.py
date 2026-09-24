from lib import *

PLASTER = "#D2C7AC"; WAL = "#3B2A1E"; OAK = "#6B4E32"; GLASS = "#7E9377"; SILV = "#A8ABA2"; GILT = "#C9A227"; MUR = "#5B3036"
CX, FLOOR, S = 110, 663, 90
E = lambda x, z: (CX + x * S, FLOOR - z * S)
H = 4.6


def label(sh, tx, ty, lx, ly, text, sub=None, anchor="start"):
    callouts(sh, [(tx, ty, text, sub or "", lx, ly, anchor)])


def arch_opening(sh, half=True):
    # round-headed kit archway 2.60 x 3.31: jambs to 2.01, semicircle r 1.30
    pts = [E(0, 0), E(1.3, 0), E(1.3, 2.01)] + [E(1.3 * math.cos(a), 2.01 + 1.3 * math.sin(a)) for a in [i * math.pi / 2 / 12 for i in range(1, 13)]]
    g = sh.lin([(0, "#0E0C0A"), (1, "#1E1A14")], 0, 0, 0, 1)
    sh.path(poly_path(pts), f"url(#{g})", INK, 1)
    # enfilade glimpse: next cell's archways shrinking
    for k, sc in enumerate((0.55, 0.32)):
        ip = [E(0, 0.6 - k * 0.2)] + [E(1.3 * sc, 0.6 - k * 0.2), E(1.3 * sc, 0.6 - k * 0.2 + 2.01 * sc)] + [E(1.3 * sc * math.cos(a), 0.6 - k * 0.2 + (2.01 + 1.3 * math.sin(a)) * sc) for a in [i * math.pi / 2 / 8 for i in range(1, 9)]]
        sh.path(poly_path(ip, closed=False), "none", "#3A3226", 1.2, op=0.8)
    return pts


def elevation(sh):
    # ceiling slab cut + floor slab cut + south wall cut
    poche(sh, *E(0, H + 0.3), 6.0 * S, 0.3 * S)
    poche(sh, *E(0, 0), 6.0 * S, 0.3 * S)
    poche(sh, *E(5.4, H), 0.6 * S, H * S)
    # wall face
    g = sh.lin([(0, dk(PLASTER, 0.45)), (0.25, dk(PLASTER, 0.2)), (1, PLASTER)], 0, 0, 0, 1)
    sh.rect(*E(0, H), 5.4 * S, H * S, f"url(#{g})")
    sh.flecks(*E(0.1, H - 0.1), *E(5.3, 0.2), 60, dk(PLASTER, 0.3), 1, 3, (0.05, 0.15))
    # cove + entablature
    cove = sh.lin([(0, dk(PLASTER, 0.55)), (1, dk(PLASTER, 0.15))], 0, 0, 0, 1)
    sh.rect(*E(0, H), 5.4 * S, 0.5 * S, f"url(#{cove})", INK, 0.8)
    sh.rect(*E(0, 4.10), 5.4 * S, 0.30 * S, f"url(#{sh.form(PLASTER, 'v', 0.2, 0.35)})", INK, 0.8)
    for z in (4.02, 3.92, 3.86):
        sh.line([E(0, z), E(5.4, z)], dk(PLASTER, 0.45), 1, smooth=False)
    # soot fans on the cove above the chandelier
    sh.glow(*E(0.0, 4.5), 150, "#1E1C1A", 0.55)
    # archway + architrave with keystone
    ap = arch_opening(sh)
    sh.line([E(1.3, 0), E(1.3, 2.01)] + [E(1.3 * math.cos(a), 2.01 + 1.3 * math.sin(a)) for a in [i * math.pi / 2 / 12 for i in range(1, 13)]], WAL, 0.18 * S * 0.9)
    sh.line([E(1.39, 0), E(1.39, 2.01)] + [E(1.39 * math.cos(a), 2.01 + 1.39 * math.sin(a)) for a in [i * math.pi / 2 / 12 for i in range(1, 13)]], lit(WAL, 0.35), 1.2, op=0.8)
    sh.path(poly_path([E(0, 3.28), E(0.14, 3.28), E(0.18, 3.62), E(0, 3.62)]), f"url(#{sh.form(WAL, 'h', 0.4)})", INK, 0.8)
    # pilasters
    for xc in (2.15, 4.95):
        sh.rect(*E(xc - 0.25, 3.80), 0.5 * S, 3.80 * S, f"url(#{sh.form(PLASTER, 'h', 0.25, 0.3)})", INK, 0.8)
        sh.rect(*E(xc - 0.30, 3.80), 0.6 * S, 0.12 * S, f"url(#{sh.form(PLASTER, 'v', 0.3)})", INK, 0.8)
        sh.rect(*E(xc - 0.30, 0.30), 0.6 * S, 0.30 * S, f"url(#{sh.form(WAL, 'h', 0.3)})", INK, 0.8)
        # wainscot on piers
        sh.rect(*E(xc - 0.25, 1.10), 0.5 * S, 0.80 * S, f"url(#{sh.form(WAL, 'h', 0.3)})", INK, 0.8)
        sh.rect(*E(xc - 0.18, 1.02), 0.36 * S, 0.62 * S, "none", lit(WAL, 0.3), 1, op=0.7)
        sh.line([E(xc - 0.25, 1.10), E(xc + 0.25, 1.10)], lit(WAL, 0.45), 2, smooth=False)
    # window 1.50 x 3.20, sill 0.60
    wx0, wx1, s0, s1 = 2.85 - 0.25 + 0.25, 4.35, 0.60, 3.80
    wx0 = 2.85
    sh.rect(*E(wx0 - 0.12, s1 + 0.05), (wx1 - wx0 + 0.24) * S, (s1 - s0 + 0.1) * S, dk(PLASTER, 0.35), INK, 1)  # splayed reveal
    gl = sh.lin([(0, dk(GLASS, 0.55)), (0.6, dk(GLASS, 0.35)), (1, dk(GLASS, 0.6))], 0, 0, 1, 1)
    sh.rect(*E(wx0, s1), (wx1 - wx0) * S, (s1 - s0) * S, f"url(#{gl})", INK, 1)
    # moonlight on the panes
    sh.path(poly_path([E(wx0, 2.6), E(wx0 + 0.6, s1), E(wx0 + 1.0, s1), E(wx0, 1.9)]), lit(GLASS, 0.4), op=0.18)
    # leaded quarries
    sh.clip_open(poly_path([E(wx0, s1), E(wx1, s1), E(wx1, s0), E(wx0, s0)]))
    for k in range(-30, 30):
        x = wx0 + k * 0.12
        sh.line([E(x, s0), E(x + 1.6, s1 + 0.3)], "#1E1C1A", 0.8, op=0.8, smooth=False)
        sh.line([E(x, s1), E(x + 1.6, s0 - 0.3)], "#1E1C1A", 0.8, op=0.8, smooth=False)
    sh.close()
    sh.rect(*E((wx0 + wx1) / 2 - 0.06, s1), 0.12 * S, (s1 - s0) * S, f"url(#{sh.form(PLASTER, 'h', 0.2, 0.4)})", INK, 0.8)  # mullion
    sh.rect(*E(wx0, 3.16), (wx1 - wx0) * S, 0.12 * S, f"url(#{sh.form(PLASTER, 'v', 0.2, 0.4)})", INK, 0.8)  # transom
    sh.rect(*E(wx0 - 0.15, s0), (wx1 - wx0 + 0.3) * S, 0.08 * S, f"url(#{sh.form(WAL, 'v', 0.4)})", INK, 0.8)  # seat lid
    # curtain (murrey drape) gathered at the right
    cur = [E(wx1 - 0.05, 3.9), E(wx1 + 0.30, 3.9), E(wx1 + 0.35, 2.5), E(wx1 + 0.28, 0.6), E(wx1 + 0.0, 0.6), E(wx1 + 0.08, 2.0)]
    sh.path(smooth_path(cur), f"url(#{sh.form(MUR, 'h', 0.3)})", INK, 1)
    for k in range(4):
        sh.line([E(wx1 + 0.04 + k * 0.07, 3.85), E(wx1 + 0.06 + k * 0.065, 2.2), E(wx1 + 0.03 + k * 0.07, 0.65)], dk(MUR, 0.45), 1.2, op=0.7)
    sh.line([E(wx0 - 0.3, 3.92), E(wx1 + 0.45, 3.92)], WAL, 3, smooth=False)  # rod
    # floor line / parquet edge
    sh.rect(*E(0, 0.03), 5.4 * S, 0.03 * S, OAK, INK, 0.6)
    # chandelier (key light) hanging at centreline
    sh.clip_open(poly_path([E(0, H), E(5.4, H), E(5.4, 0), E(0, 0)]))
    sh.glow(*E(0.0, 3.0), 260, "#F2D9A8", 0.22)
    sh.glow(*E(0.0, 3.0), 120, "#C4542E", 0.12)
    sh.line([E(0, H), E(0, 3.45)], dk(GILT, 0.4), 1.6, smooth=False, dash="3 2")
    body = [E(-0.08, 3.45), E(0.08, 3.45), E(0.12, 3.1), E(0.45, 2.95), E(0.42, 2.85), E(0.06, 2.72), E(0.0, 2.62), E(-0.06, 2.72), E(-0.42, 2.85), E(-0.45, 2.95), E(-0.12, 3.1)]
    sh.path(smooth_path(body), f"url(#{sh.form(GILT, 'v', 0.5)})", INK, 1)
    for x in (-0.42, -0.28, -0.14, 0.14, 0.28, 0.42):
        cx, cy = E(x, 2.96 + (0.02 if abs(x) < 0.2 else 0))
        sh.rect(cx - 2, cy - 12, 4, 12, PLASTER, INK, 0.5)
        sh.glow(cx, cy - 16, 14, "#F2D9A8", 0.9)
        sh.ellipse(cx, cy - 16, 2, 4, "#F2D9A8")
        for d in (-5, 5):
            sh.ellipse(cx + d, cy + 8, 1.6, 3.5, lit(GLASS, 0.5), op=0.8)
    sh.close()
    # human for scale
    human(sh, E(3.6, 0)[0] - 110, FLOOR, S)
    # socket labels
    label(sh, *E(0.7, 1.2), 150, 150, "DOOR (ENFILADE ARCHWAY)", "2.60 × 3.31 m, round head")
    label(sh, *E(3.3, 2.2), 430, 150, "WINDOW 1.50 × 3.20 m", "sill 0.60 · leaded quarries · exit")
    label(sh, *E(3.1, 0.64), 430, 200, "WINDOW SEAT (HIDE)", "walnut lid 1.40 × 0.45 m")
    label(sh, *E(0.35, 2.8), 150, 200, "CHANDELIER (KEY LIGHT)", "0.90 m · droppable at 3.4 m")


def plan(sh):
    s = 32
    x0, y0 = 768, 236
    P = plan_frame(sh, x0, y0, s, 0.6, [("N", 0, 2.6), ("S", 0, 2.6), ("E", 0, 2.6), ("W", 0, 2.6)])
    # parquet basket-weave hint
    for i in range(-5, 5):
        for j in range(-5, 5):
            x, y = P(i + 0.5, j + 0.5)
            if (i + j) % 2:
                for k in range(3):
                    sh.line([(x - s * 0.3, y - s * 0.2 + k * s * 0.2), (x + s * 0.3, y - s * 0.2 + k * s * 0.2)], "#2E2820", 1, smooth=False)
            else:
                for k in range(3):
                    sh.line([(x - s * 0.2 + k * s * 0.2, y - s * 0.3), (x - s * 0.2 + k * s * 0.2, y + s * 0.3)], "#2E2820", 1, smooth=False)
    # walking line
    sh.rect(*P(-0.9, 5.4), 1.8 * s, 10.8 * s, OAK, op=0.18)
    # windows east wall, at +/-3.6 m
    for yc in (3.6, -3.6):
        xa, ya = P(5.4, yc + 0.75)
        sh.rect(xa, ya, 0.6 * s, 1.5 * s, "#1A1712", GLASS, 1.2)
        sh.line([(xa + 0.3 * s, ya), (xa + 0.3 * s, ya + 1.5 * s)], GLASS, 1.6, smooth=False)
        sh.rect(xa - 0.45 * s, ya + 0.05 * s, 0.45 * s, 1.4 * s, WAL, INK, 0.6)  # seat
        plan_label(sh, P(5.4, yc)[0] - 18, P(5.4, yc)[1] + 3, "WINDOW", "end", size=9)
    # mirrors on west piers
    for yc in (3.6, -3.6):
        xa, ya = P(-5.4, yc + 0.55)
        sh.rect(xa, ya, 0.1 * s, 1.1 * s, SILV, GILT, 1.2)
        plan_label(sh, P(-6.0, yc)[0] - 8, P(-6.0, yc)[1] + 3, "MIRROR", "end", size=9)
    xa, ya = P(-5.4, 0.3)
    # small stealable mirror between: on the pier? (west archway occupies centre) -> put on the south-west pier
    # portraits on N/S walls either side of archways
    for yc, sgn in ((5.4, -1), (-5.4, 1)):
        for xc in (-3.2, 3.2):
            xa, ya = P(xc - 0.5, yc)
            sh.rect(xa, ya + (0 if sgn < 0 else -0.1 * s), 1.0 * s, 0.1 * s, GILT)
            xt, yt = P(xc - 0.6, yc + sgn * 0.6)
            sh.rect(xt, yt - (0.5 * s if sgn < 0 else 0), 1.2 * s, 0.5 * s, WAL, INK, 0.6)  # side table
    # chandelier
    cx, cy = P(0, 0)
    sh.glow(cx, cy, 70, "#F2D9A8", 0.25)
    sh.circle(cx, cy, 0.45 * s, "none", GILT, 1.4)
    sh.circle(cx, cy, 3, GILT)
    for side, (lx, ly, a) in {"N": (P(0, 6)[0], y0 - 12, "middle"), "S": (P(0, -6)[0], y0 + 12 * s + 32, "middle"),
                              "E": (x0 + 12 * s - 0.6 * s - 6, P(0, 0)[1] + 3, "end"), "W": (x0 + 0.6 * s + 6, P(0, 0)[1] + 3, "start")}.items():
        plan_label(sh, lx, ly, f"DOOR {side} 2.60", a)
    north_arrow(sh, x0 + 12 * s - 14, y0 + 30)
    human_dot = P(2.0, -1.5)
    sh.circle(*human_dot, 0.4 * s, "none", "#9A9078", 1)
    plan_label(sh, human_dot[0] + 16, human_dot[1] + 3, "0.40 m body", "start", FRAME_TXT, 8.5)
    plan_label(sh, P(0, 1.2)[0], P(0, 1.2)[1], "CHANDELIER", "middle", FRAME_TXT, 8.5)
    plan_label(sh, P(0, -3)[0], P(0, -3)[1], "enfilade walking line", "middle", FRAME_TXT, 8.5)


def build():
    sh = Sheet(seed=31)
    elevation(sh)
    struct_ladder(sh, 70, FLOOR, S, 4.6, clear=4.6)
    sh.text(70, FLOOR + 20, "floor", 9, FRAME_MID, "middle")
    plan(sh)
    return sh.render("THE AGE OF POWDER · STRUCTURE · KEEP", "The Long Gallery",
                     "H 4.60 m clear · 12 × 12 m cell · ≤ 25k tris", "elevation 1 m = 90 px · plan 1 m = 32 px",
                     [(PLASTER, "lime plaster"), (WAL, "black walnut"), (OAK, "parquet oak"), (GLASS, "window glass"),
                      (SILV, "silvering"), (GILT, "gilt"), (MUR, "murrey drape")],
                     view_labels=[(380, "HALF ELEVATION · EAST WALL, LOOKING EAST"), (960, "PLAN")], ladder=None, human=False,
                     glow_c=("20%", "55%"))
