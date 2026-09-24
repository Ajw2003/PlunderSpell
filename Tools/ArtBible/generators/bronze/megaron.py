from lib import *

PLAST, BLUE, RED, MADDER_, CYP, SOOT, GYP = "#C9A77A", "#3F6F86", "#8E3F2C", "#C4542E", "#5A3E28", "#2B231B", "#D8CDB2"
GROUND = (40, 770)
F0 = 0.30


def spiral_band(sh, x0, x1, y, h, col, bg=None):
    """Running spiral band between px x0..x1 at top y, height h px."""
    out = []
    if bg:
        out.append(f'<rect x="{f(x0)}" y="{f(y)}" width="{f(x1 - x0)}" height="{f(h)}" fill="{bg}"/>')
    r = h * .32
    n = int((x1 - x0) / (h * 1.1))
    for i in range(n):
        cx = x0 + (i + .5) * (x1 - x0) / n
        cy = y + h / 2
        out.append(f'<path d="M{f(cx - r * 1.6)} {f(cy + r * .9)} C{f(cx - r)} {f(cy + r * 1.2)} {f(cx - r * 1.1)} {f(cy - r)} '
                   f'{f(cx)} {f(cy - r)} A{f(r)} {f(r)} 0 1 1 {f(cx - r * .2)} {f(cy + r * .6)} A{f(r * .5)} {f(r * .5)} 0 1 1 {f(cx + r * .3)} {f(cy)}" '
                   f'fill="none" stroke="{col}" stroke-width="{f(max(1, h * .07))}"/>')
    sh.add("".join(out))


def griffin(sh, x, y, s, col, facing=1):
    """Couchant griffin silhouette, lower-left at (x, y), ~120 x 60 units."""
    pts = [(0, 0), (10, -18), (30, -24), (58, -22), (78, -30), (84, -52), (96, -60), (106, -56), (104, -46), (114, -44),
           (104, -38), (94, -34), (92, -18), (100, 0)]
    pp = [(x + facing * px * s, y + py * s) for px, py in pts]
    sh.path(smooth_path(pp, tension=.35), col, darken(col, .5), 1, op=.9)
    # wing
    w = [(40, -24), (46, -52), (64, -64), (70, -46), (62, -28)]
    sh.path(smooth_path([(x + facing * px * s, y + py * s) for px, py in w], tension=.35), BLUE, darken(BLUE, .5), 1, op=.9)
    for k in range(4):
        sh.line(x + facing * (48 + k * 5) * s, y - 30 * s, x + facing * (52 + k * 5) * s, y - (52 - k * 3) * s, lighten(BLUE, .4), .8, op=.8)
    sh.circle(x + facing * 100 * s, y - 52 * s, 1.6 * s + .4, "#14120E")


def column(sh, x, h0=F0, beam=True):
    top = h0 + 4.60
    # base disc
    erect(sh, x - 0.30, h0, x + 0.30, h0 + 0.10, SOOT, darken(SOOT, .5))
    shaft = [E(x - 0.17, h0 + 0.10), E(x + 0.17, h0 + 0.10), E(x + 0.23, h0 + 4.14), E(x - 0.23, h0 + 4.14)]
    d = poly_path(shaft)
    sh.path(d, f"url(#{sh.lin(RED, 'h', .3, .55)})", darken(RED, .6), 1.2)
    # soot on the hearth-facing side
    side = 1 if x < 6 else -1
    a = E(x + side * 0.05, h0 + 0.1)
    sh.clipped(d, f'<rect x="{f(min(a[0], a[0] + side * 30))}" y="{f(E(0, h0 + 4.14)[1])}" width="30" height="{f(4.04 * SK)}" fill="{SOOT}" opacity=".45"/>')
    sh.flecks(d, (E(x - .25, 0)[0], E(0, h0 + 4.14)[1], E(x + .25, 0)[0], E(0, h0 + .1)[1]), 30, SOOT, .5, 1.3, .5)
    # capital (cushion) + abacus
    cap = [E(x - 0.23, h0 + 4.14), E(x - 0.35, h0 + 4.30), E(x - 0.30, h0 + 4.50), E(x + 0.30, h0 + 4.50), E(x + 0.35, h0 + 4.30), E(x + 0.23, h0 + 4.14)]
    sh.path(smooth_path(cap, tension=.35), f"url(#{sh.lin(SOOT, 'h', .35, .5)})", "#0E0C09", 1)
    erect(sh, x - 0.35, h0 + 4.50, x + 0.35, h0 + 4.60, lighten(SOOT, .15), "#0E0C09")
    return top


def build():
    mats = [("plaster", PLAST), ("fresco blue", BLUE), ("haematite", RED), ("madder", MADDER_), ("cypress", CYP),
            ("soot", SOOT), ("gypsum", GYP)]
    sh = struct_sheet("Keep", "The Megaron", 4.60, mats, seed=111)
    sh.extra_frame.append(f'<text x="{f(E(6, 0)[0])}" y="730" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">SECTION A–A · E–W THROUGH THE HEARTH, LOOKING NORTH</text>')
    # hearth glow (behind everything)
    hx, hy = E(6, F0 + 0.6)
    g1 = sh.rad([(0, MADDER_, .55), (0.35, "#6A3418", .45), (1, "#14120E", 0)])
    sh.back.append(f'<ellipse cx="{f(hx)}" cy="{f(hy - 40)}" rx="330" ry="240" fill="url(#{g1})"/>')
    # slab
    erect(sh, 0, 0, 12, F0, f"url(#{sh.lin('#7A6A58', 'v', .1, .4)})", darken(PLAST, .7))
    # north wall inner face (x 0.8..11.2, 0..4.6)
    wall = poly_path([E(0.8, F0), E(11.2, F0), E(11.2, F0 + 4.6), E(0.8, F0 + 4.6)])
    sh.path(wall, f"url(#{sh.lin(PLAST, 'v', .1, .5)})", darken(PLAST, .6), 1)
    # soot gradient from the ceiling down
    gs = sh.lin(SOOT, "v", 0, 0, stops=[(0, SOOT), (0.45, SOOT), (1, PLAST)])
    sh.clipped(wall, f'<rect x="{f(E(0.8, 0)[0])}" y="{f(E(0, F0 + 4.6)[1])}" width="{f(10.4 * SK)}" height="{f(1.4 * SK)}" fill="url(#{gs})" opacity=".75"/>')
    # dado (imitation veined stone)
    erect(sh, 0.8, F0 + 0.8, 11.2, F0 + 1.2, mix(PLAST, GYP, .5), darken(PLAST, .5), .8)
    for i in range(10):
        a = E(0.9 + i * 1.05, F0 + 0.85)
        sh.path(f"M{f(a[0])} {f(a[1])} q14 10 28 2 t30 12", "none", darken(PLAST, .45), .8, op=.7)
    # fresco band 1.2..3.2 with spirals top and bottom
    erect(sh, 0.8, F0 + 1.2, 11.2, F0 + 3.2, mix(PLAST, "#E0C8A0", .3), darken(PLAST, .5), .8)
    spiral_band(sh, E(0.8, 0)[0], E(11.2, 0)[0], E(0, F0 + 3.2)[1], 0.25 * SK, RED, bg=BLUE)
    spiral_band(sh, E(0.8, 0)[0], E(11.2, 0)[0], E(0, F0 + 1.45)[1], 0.25 * SK, BLUE, bg=RED)
    # procession figures + griffins in the band
    for i, x in enumerate((1.2, 2.0, 2.8, 8.2, 9.0)):
        bx, by = E(x, F0 + 1.45)
        col = RED if i % 2 else BLUE
        sh.path(f"M{f(bx)} {f(by)} l6 -30 l-3 -8 a6 6 0 1 1 8 0 l-3 8 l6 30 z", col, darken(col, .5), .8, op=.9)
    griffin(sh, *E(9.7, F0 + 1.5), 0.62, "#E0C8A0" if False else lighten(PLAST, .25), 1)
    # timber lacing beams
    for hh in (1.2, 3.4):
        erect(sh, 0.8, F0 + hh - 0.1, 11.2, F0 + hh + 0.1, f"url(#{sh.lin(CYP, 'v', .2, .5)})", darken(CYP, .6), .8)
    # north archway with wool curtain
    ax0, ax1 = 4.7, 7.3
    erect(sh, ax0, F0, ax1, F0 + 3.31, "#14120E")
    for i in range(9):
        x = ax0 + i * (ax1 - ax0) / 9
        a, b = E(x, F0 + 3.28), E(x + (ax1 - ax0) / 9, F0 + 0.05)
        sh.path(f"M{f(a[0])} {f(a[1])} L{f(b[0])} {f(a[1])} Q{f(b[0] + 3)} {f((a[1] + b[1]) / 2)} {f(b[0] - 2)} {f(b[1])} L{f(a[0] + 2)} {f(b[1])} Q{f(a[0] - 3)} {f((a[1] + b[1]) / 2)} {f(a[0])} {f(a[1])} Z",
                f"url(#{sh.lin(RED, 'h', .25, .6)})", darken(RED, .6), .6)
    sh.line(*E(ax0 - .1, F0 + 3.31), *E(ax1 + .1, F0 + 3.31), CYP, 4)
    # clerestory slot
    erect(sh, 8.8, F0 + 3.7, 10.0, F0 + 4.1, "#14120E", darken(PLAST, .6))
    # bench along the north wall
    erect(sh, 2.6, F0, 8.6, F0 + 0.40, f"url(#{sh.lin(PLAST, 'v', .25, .45)})", darken(PLAST, .6))
    # north row columns
    for x in (3.5, 8.5):
        column(sh, x)
    # roof: E-W beam band, cut joists, reed/clay, lantern
    erect(sh, 0, F0 + 4.6, 12, F0 + 5.0, f"url(#{sh.lin(CYP, 'v', .15, .5)})", "#0E0C09")
    sh.flecks(poly_path([E(0, F0 + 4.6), E(12, F0 + 4.6), E(12, F0 + 5.0), E(0, F0 + 5.0)]),
              (E(0, 0)[0], E(0, F0 + 5)[1], E(12, 0)[0], E(0, F0 + 4.6)[1]), 60, SOOT, .6, 1.8, .6)
    for i in range(30):
        x = 0.2 + i * 0.4
        if 5.0 < x < 7.0:
            continue
        sh.circle(*E(x, F0 + 5.07), 0.06 * SK, darken(CYP, .2), "#0E0C09", .8)
    for x0, x1 in ((0, 5.0), (7.0, 12)):
        erect(sh, x0, F0 + 5.14, x1, F0 + 5.40, darken("#7A6A58", .1), "#0E0C09", .8)
    # lantern (clerestory box above the smoke-hole) — beyond the cell, dashed
    lx0, lx1 = 5.0, 7.0
    sh.path(poly_path([E(lx0 - .15, F0 + 5.0), E(lx0 - .15, F0 + 5.9), E(lx1 + .15, F0 + 5.9), E(lx1 + .15, F0 + 5.0)], closed=False),
            "none", CYP, 3)
    for i in range(5):
        a, b = E(lx0 + .1, F0 + 5.15 + i * .15), E(lx1 - .1, F0 + 5.25 + i * .15)
        sh.line(a[0], a[1], b[0], b[1], darken(CYP, .2), 2)
    sh.path(poly_path([E(lx0 - .3, F0 + 5.9), E(6.0, F0 + 6.25), E(lx1 + .3, F0 + 5.9)], closed=False), "none", CYP, 3)
    # smoke rising through the lantern
    for dx in (-0.4, 0.1, 0.5):
        pts = [E(6 + dx, F0 + 0.9), E(6 + dx * 1.4 + .2, F0 + 2.3), E(6 + dx * .6 - .2, F0 + 3.8), E(6 + dx * .3 + .1, F0 + 5.3), E(6 + dx * .2, F0 + 6.3)]
        sh.path(smooth_path(pts, closed=False), "none", lighten(SOOT, .35), 10, op=.18)
    # cut walls (section): west with archway, east with blind recess + niche
    for x0, x1 in ((0, 0.8), (11.2, 12)):
        d = poly_path([E(x0, F0), E(x1, F0), E(x1, F0 + 5.0), E(x0, F0 + 5.0)])
        sh.path(d, "#3A332A", "#0E0C09", 1.2)
        a, b = E(x0, F0 + 5.0), E(x1, F0)
        hatch(sh, d, (a[0], a[1], b[0], b[1]), "#635C4C", 5, .7)
    erect(sh, 0, F0, 0.8, F0 + 3.31, "#14120E", "#0E0C09")   # west archway (cut)
    erect(sh, 11.2, F0 + 1.0, 11.6, F0 + 2.2, "#14120E", "#0E0C09")   # secret niche behind throne
    # throne in profile against the east wall
    th = [E(10.55, F0), E(10.55, F0 + 0.46), E(10.95, F0 + 0.46), E(10.95, F0 + 1.30), E(11.0, F0 + 1.40), E(11.08, F0 + 1.34),
          E(11.14, F0 + 1.42), E(11.2, F0 + 1.40), E(11.2, F0)]
    sh.path(smooth_path(th, tension=.15), f"url(#{sh.lin(GYP, 'h', .2, .45)})", darken(GYP, .6), 1.2)
    # hearth: raised painted rim, ash, logs and fire
    rim = poly_path([E(4.2, F0), E(7.8, F0), E(7.8, F0 + 0.2), E(4.2, F0 + 0.2)])
    sh.path(rim, "#B7803E", darken("#B7803E", .6), 1)
    tongues = []
    for i in range(14):
        a = E(4.25 + i * 0.25, F0 + 0.02)
        tongues.append(f'<path d="M{f(a[0])} {f(a[1])} q4 -6 1 -10 q6 4 7 10 z" fill="{MADDER_}"/>')
    sh.clipped(rim, "".join(tongues))
    spiral_band(sh, E(4.2, 0)[0], E(7.8, 0)[0], E(0, F0 + 0.2)[1], 3.5, BLUE)
    for x0 in (5.2, 6.1):
        sh.path(poly_path([E(x0, F0 + 0.2), E(x0 + 0.8, F0 + 0.32), E(x0 + 0.8, F0 + 0.40), E(x0, F0 + 0.28)]), CYP, "#0E0C09", 1)
    flames = [(5.3, .9), (5.7, 1.3), (6.0, 1.05), (6.35, 1.2), (6.7, .8)]
    for x, h in flames:
        a = E(x, F0 + 0.25)
        tip = E(x + .05, F0 + 0.25 + h)
        sh.path(f"M{f(a[0] - 12)} {f(a[1])} Q{f(a[0] - 10)} {f((a[1] + tip[1]) / 2)} {f(tip[0])} {f(tip[1])} Q{f(a[0] + 12)} {f((a[1] + tip[1]) / 2)} {f(a[0] + 12)} {f(a[1])} Z",
                MADDER_, op=.9)
        sh.path(f"M{f(a[0] - 5)} {f(a[1])} Q{f(a[0] - 4)} {f((a[1] + tip[1]) / 2 + 10)} {f(tip[0])} {f(tip[1] + 18)} Q{f(a[0] + 5)} {f((a[1] + tip[1]) / 2 + 10)} {f(a[0] + 5)} {f(a[1])} Z",
                "#E6C75A" if False else lighten(MADDER_, .55), op=.9)
    # tripod stands by the hearth
    for x in (3.9, 8.1):
        bx_, by_ = E(x, F0 + 0.85)
        sh.path(f"M{f(bx_ - 12)} {f(by_)} a12 7 0 0 0 24 0 z", "#9B6A38", darken("#9B6A38", .6), 1)
        for dx in (-10, 0, 10):
            sh.line(bx_ + dx * .6, by_ + 3, bx_ + dx * 1.2, E(0, F0)[1], "#9B6A38", 1.6)
    human(sh, 2.1)
    # callouts
    sh.callouts([
        (*E(8.5, F0 + 2.4), "TAPERED COLUMN", "0.34 → 0.46 m, haematite"),
        (*E(6.0, F0 + 5.6), "SMOKE LANTERN", "2.0 × 2.0 m, louvred"),
        (*E(10.9, F0 + 1.1), "GYPSUM THRONE", "wavy crest, niche behind"),
        (*E(4.4, F0 + 0.12), "PAINTED HEARTH", "3.60 m dia., flame band"),
        (*E(6.9, F0 + 2.0), "WOOL CURTAIN", "north archway, burns 3 s")], 600, 160, 330, slope=1.0)
    sh.callout(*E(1.8, F0 + 2.3), 250, 180, "FRESCO BAND", "1.20–3.20 m, spirals", anchor="end")
    # clear-height note under the beams
    sh.text(E(10.0, 0)[0], E(0, F0 + 4.38)[1], "4.60 clear", 9, "#DCD2BA", "middle")
    # ---- plan ----
    plan_frame(sh)
    wallc = "#3A332A"
    # hall walls (outer 0..12 x 1.6..12, 0.8 thick) as filled L's
    for x0, y0, x1, y1 in ((0, 0, 0.8, 12), (11.2, 0, 12, 12), (0.8, 11.2, 11.2, 12), (0.8, 1.6, 11.2, 2.4)):
        prect(sh, x0, y0, x1, y1, wallc, "#635C4C", .6)
    # openings (cut out)
    prect(sh, 4.7, 11.2, 7.3, 12, "#1B1813")           # north archway
    prect(sh, 4.7, 1.6, 7.3, 2.4, "#1B1813")           # south door
    prect(sh, 0, 5.5, 0.8, 8.1, "#1B1813")             # west archway
    prect(sh, 11.2, 5.5, 11.6, 8.1, "#1B1813", "#635C4C", .6)    # east blind recess
    prect(sh, 11.2, 6.4, 11.8, 7.2, "#14120E", "#C4542E", .8)   # secret niche
    # curtain (dashed) and door leaves
    sh.line(*PL(4.7, 11.6), *PL(7.3, 11.6), RED, 2.5, dash="3 2")
    for s in (-1, 1):
        sh.line(*PL(6 + s * 1.3, 2.4), *PL(6 + s * 1.3 - s * 0.9, 3.3), CYP, 3)
    # porch columns in antis
    for x in (4.0, 8.0):
        sh.circle(*PL(x, 0.8), 0.23 * PK, RED, "#0E0C09", 1)
    # benches
    prect(sh, 2.6, 10.7, 8.6, 11.2, darken(PLAST, .3))
    prect(sh, 2.6, 2.4, 8.6, 2.9, darken(PLAST, .3))
    # hearth
    hc = PL(6, 6.8)
    g2 = sh.rad([(0, MADDER_, .6), (1, MADDER_, 0)])
    sh.circle(*hc, 2.6 * PK, f"url(#{g2})")
    sh.circle(*hc, 1.8 * PK, "#B7803E", "#0E0C09", 1.2)
    sh.circle(*hc, 1.5 * PK, SOOT, BLUE, 2)
    sh.circle(*hc, 0.6 * PK, MADDER_, op=.8)
    # libation channel to the throne
    sh.line(*PL(7.8, 6.8), *PL(10.55, 6.8), "#9A9078", 1, dash="2 2")
    # columns
    for x, y in ((3.5, 4.3), (8.5, 4.3), (3.5, 9.3), (8.5, 9.3)):
        sh.circle(*PL(x, y), 0.23 * PK, RED, "#0E0C09", 1)
    # smoke-hole
    a, b = PL(5.0, 7.8), PL(7.0, 5.8)
    sh.add(f'<rect x="{f(a[0])}" y="{f(a[1])}" width="{f(b[0] - a[0])}" height="{f(b[1] - a[1])}" fill="none" stroke="#DCD2BA" stroke-width=".8" stroke-dasharray="3 3"/>')
    # throne
    prect(sh, 10.55, 6.35, 11.2, 7.25, GYP, "#0E0C09", .8)
    # clerestory slots
    prect(sh, 8.8, 11.4, 10.0, 11.8, "#14120E", "#DCD2BA", .6)
    prect(sh, 2.6, 1.8, 3.8, 2.2, "#14120E", "#DCD2BA", .6)
    # section line A-A
    sh.line(*PL(-0.6, 6.8), *PL(12.6, 6.8), "#9A9078", .8, dash="8 3 2 3")
    for x in (-0.6, 12.6):
        p = PL(x, 6.8)
        sh.path(f"M{f(p[0])} {f(p[1])} l0 -10 l-4 5 m4 -5 l4 5", "none", "#9A9078", 1)
        sh.text(p[0], p[1] + 12, "A", 9, "#9A9078", "middle")
    socket_label(sh, *PL(6.0, 10.15), "DOOR N (CURTAIN) 2.60 × 3.31")
    socket_label(sh, *PL(6.0, 0.25), "DOOR 2.60 × 3.31 · PORCH")
    socket_label(sh, *PL(2.4, 7.4), "DOOR W")
    socket_label(sh, *PL(9.9, 7.9), "THRONE + NICHE")
    socket_label(sh, *PL(10.3, 10.6), "WINDOW")
    socket_label(sh, *PL(6.0, 8.2), "SMOKE-HOLE")
    socket_label(sh, *PL(6.0, 6.7), "HEARTH", col="#DCD2BA")
    legend(sh, [("DOOR", "S (porch) · N (curtain) · W — 2.60 × 3.31"),
                ("WINDOW", "2 clerestory slots 1.20 × 0.40 at 3.90"),
                ("SMOKE-HOLE", "2.00 × 2.00 m roof opening + lantern"),
                ("NICHE", "E blind recess, secret niche (artifact)"),
                ("COLUMNS", "4 on a 5 m square · 2 in the porch")])
    return sh
