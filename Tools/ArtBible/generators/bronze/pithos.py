from lib import *

TERRA, OILC, PLAST, FLOOR, GRAIN, SOOT = "#A0603E", "#6A4430", "#B7803E", "#7A5A3E", "#C8A868", "#2B231B"
RED = "#8E3F2C"
GROUND = (40, 770)
F0 = 0.30
ROWS = [2.55, 4.85, 7.15, 9.45]
PITH = [(0.0, 0.22), (0.15, 0.36), (0.40, 0.48), (0.70, 0.54), (1.00, 0.55), (1.30, 0.51), (1.60, 0.39), (1.80, 0.28),
        (1.90, 0.25), (1.93, 0.29), (2.00, 0.29)]


def pithos(sh, x, oil=False, far=False):
    zs = 0.9 if oil else 1.0
    prof = [(z * zs, r) for z, r in PITH]
    base = OILC if oil else TERRA
    if far:
        base = darken(base, .25)
    cx, gy = E(x, F0)
    d, Y, se = revolve(sh, cx, gy, SK, prof, base, e=0.001, light=.35, dark=.55, sw=1.3)
    bb = (cx - 0.56 * SK, Y(2.0 * zs), cx + 0.56 * SK, gy)
    # rope bands
    for z in (0.40, 0.80, 1.20, 1.50):
        z *= zs
        r = interp(prof, z)
        a, b = cx - r * SK, cx + r * SK
        sh.line(a, Y(z), b, Y(z), darken(base, .45), 3.2)
        n = int((b - a) / 5)
        sh.add("".join(f'<line x1="{f(a + i * 5)}" y1="{f(Y(z) - 1.6)}" x2="{f(a + i * 5 + 3)}" y2="{f(Y(z) + 1.6)}" stroke="{lighten(base, .3)}" stroke-width="1"/>' for i in range(n)))
    # lug handles on the shoulder
    for s in (-1, 1):
        r = interp(prof, 1.55 * zs)
        sh.ellipse(cx + s * (r + 0.02) * SK, Y(1.55 * zs), 3, 5, base, darken(base, .6), 1)
    # fire-cloud blotches + chips
    sh.flecks(d, bb, 50, darken(base, .45), .8, 2.8, .35)
    sh.flecks(d, bb, 25, lighten(base, .4), .5, 1.2, .5)
    if oil:
        # black oil drips from the rim, stone lid
        drips = "".join(f'<path d="M{f(cx + dx * SK)} {f(Y(1.8 * zs))} q{f(dx * 8)} 20 {f(dx * 4)} {f(28 + abs(dx) * 60)}" stroke="{SOOT}" stroke-width="{f(3 + dx * 4)}" fill="none" opacity=".75" stroke-linecap="round"/>'
                        for dx in (0.12, 0.18, 0.25))
        sh.clipped(d, drips)
        sh.ellipse(cx, Y(2.0 * zs), 0.28 * SK, 3, "#8C7F68", darken("#8C7F68", .6), 1)
    else:
        sh.ellipse(cx, Y(2.0), 0.27 * SK, 2.5, GRAIN, darken(GRAIN, .5), .8)
    return Y


def build():
    mats = [("pithos clay", TERRA), ("oiled clay", OILC), ("plaster", PLAST), ("clay floor", FLOOR), ("grain", GRAIN),
            ("soot", SOOT)]
    sh = struct_sheet("InnerWard", "The Pithos Magazine", 4.00, mats, seed=121)
    sh.extra_frame.append(f'<text x="{f(E(6, 0)[0])}" y="730" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">SECTION B–B · E–W ALONG THE CROSS-AISLE, LOOKING NORTH</text>')
    # oil-lamp glow on the north wall
    lx, ly = E(6.0, F0 + 2.3)
    g1 = sh.rad([(0, "#C4542E", .4), (0.35, "#5A3018", .4), (1, "#14120E", 0)])
    sh.back.append(f'<ellipse cx="{f(lx)}" cy="{f(ly)}" rx="300" ry="210" fill="url(#{g1})"/>')
    erect(sh, 0, 0, 12, F0, f"url(#{sh.lin(FLOOR, 'v', .1, .4)})", darken(FLOOR, .6))
    # north wall face
    wall = poly_path([E(0.7, F0), E(11.3, F0), E(11.3, F0 + 4.0), E(0.7, F0 + 4.0)])
    sh.path(wall, f"url(#{sh.lin(PLAST, 'v', .05, .55)})", darken(PLAST, .6), 1)
    gs = sh.lin(SOOT, "v", 0, 0, stops=[(0, SOOT), (1, PLAST)])
    sh.clipped(wall, f'<rect x="{f(E(0.7, 0)[0])}" y="{f(E(0, F0 + 4)[1])}" width="{f(10.6 * SK)}" height="{f(1.2 * SK)}" fill="url(#{gs})" opacity=".7"/>')
    sh.flecks(wall, (E(0.7, 0)[0], E(0, F0 + 4)[1], E(11.3, 0)[0], E(0, F0)[1]), 120, darken(PLAST, .4), .5, 1.6, .35)
    for hh in (1.0, 2.6):
        erect(sh, 0.7, F0 + hh - 0.09, 11.3, F0 + hh + 0.09, f"url(#{sh.lin('#5A3E28', 'v', .2, .5)})", "#0E0C09", .8)
    # north archway (into next magazine) behind the central aisle
    erect(sh, 4.7, F0, 7.3, F0 + 2.88, "#14120E", darken(PLAST, .6))
    # tally marks + painted row numbers on the wall at 1.8 m
    for x in (1.1, 1.9, 8.3, 10.6):
        a = E(x, F0 + 1.95)
        sh.path(f"M{f(a[0] - 6)} {f(a[1])} v11 M{f(a[0])} {f(a[1])} v11 M{f(a[0] + 6)} {f(a[1])} v11 M{f(a[0] - 8)} {f(a[1] + 6)} h16", "none", RED, 1.8)
    for i in range(14):
        a = E(1.0 + i * 0.12, F0 + 1.45)
        sh.line(a[0], a[1], a[0], a[1] + 7, darken(PLAST, .5), 1)
    # lamp niche
    erect(sh, 5.8, F0 + 3.1, 6.2, F0 + 3.5, darken(SOOT, .2))
    a = E(6.0, F0 + 3.18)
    sh.ellipse(a[0], a[1], 7, 3, TERRA)
    sh.path(f"M{f(a[0] + 5)} {f(a[1] - 2)} q3 -8 0 -12 q-2 6 -3 12 z", "#C4542E")
    # ceiling: cut poles + clay roof
    erect(sh, 0, F0 + 4.24, 12, F0 + 4.55, darken(FLOOR, .15), "#0E0C09", .8)
    for i in range(24):
        sh.circle(*E(0.25 + i * 0.5, F0 + 4.12), 0.12 * SK, "#5A3E28", "#0E0C09", .8)
    # jars (north half seen beyond the cross-aisle)
    for i, x in enumerate(ROWS):
        oil = i >= 2
        pithos(sh, x, oil)
    # benches (cut) in front of jar feet
    for x in ROWS:
        erect(sh, x - 0.6, F0, x + 0.6, F0 + 0.30, f"url(#{sh.lin(FLOOR, 'v', .25, .45)})", darken(FLOOR, .6))
    # grain drifts (west) and oil pools (east)
    for x in (1.5, 3.5, 5.6):
        a = E(x, F0)
        sh.path(f"M{f(a[0] - 26)} {f(a[1])} q26 -10 52 0 z", GRAIN, darken(GRAIN, .5), .6)
    for x in (6.6, 8.3, 10.5):
        a = E(x, F0)
        sh.ellipse(a[0], a[1] - 1, 30, 3, SOOT, op=.9)
        sh.ellipse(a[0] - 6, a[1] - 2, 10, 1.2, lighten(SOOT, .4), op=.7)
    # ladder against an oil jar
    for dx in (0, 0.42):
        sh.line(*E(5.95 + dx, F0), *E(6.55 + dx, F0 + 2.0), "#8A6A48", 3)
    for k in range(7):
        t = (k + 0.5) / 7
        a = E(5.95 + 0.6 * t, F0 + 2.0 * t)
        b = E(6.37 + 0.6 * t, F0 + 2.0 * t)
        sh.line(a[0], a[1], b[0], b[1], "#8A6A48", 2)
    # cut walls with E/W archways choked by amphora piles
    for x0, x1 in ((0, 0.7), (11.3, 12)):
        d = poly_path([E(x0, F0), E(x1, F0), E(x1, F0 + 4.55), E(x0, F0 + 4.55)])
        sh.path(d, "#3A332A", "#0E0C09", 1.2)
        a, b = E(x0, F0 + 4.55), E(x1, F0)
        hatch(sh, d, (a[0], a[1], b[0], b[1]), "#635C4C", 5, .7)
        erect(sh, x0, F0, x1, F0 + 2.88, "#14120E", "#0E0C09")
    for xc in (1.0, 11.0):
        for j, (dx, dh) in enumerate(((-0.12, 0), (0.12, 0), (0, 0.55))):
            cx, gy = E(xc + dx, F0 + dh)
            sh.path(smooth_path([(cx - 6, gy), (cx - 9, gy - 18), (cx - 5, gy - 30), (cx + 5, gy - 30), (cx + 9, gy - 18), (cx + 6, gy)]),
                    f"url(#{sh.lin('#B88A62', 'h', .3, .5)})", darken("#B88A62", .6), .8)
    human(sh, 3.7)
    # dims
    a, b = E(3.1, F0 + 0.05), E(4.3, F0 + 0.05)
    sh.line(a[0], a[1] + 14 + 16.5, b[0], b[1] + 14 + 16.5, "#9A9078", .8)
    sh.text((a[0] + b[0]) / 2, a[1] + 42, "1.20 aisle", 9, "#9A9078", "middle")
    sh.text(E(10.3, 0)[0], E(0, F0 + 3.78)[1], "4.00 clear", 9, "#DCD2BA", "middle")
    # callouts
    sh.callouts([
        (*E(2.55, F0 + 1.0), "GRAIN PITHOS", "2.00 m, 1.10 m belly"),
        (*E(9.45 + 0.3, F0 + 1.3), "OIL PITHOS", "1.80 m, lid, black drips"),
        (*E(4.85 + 0.45, F0 + 0.72), "ROPE BANDS", "4 applied ridges 0.06 m"),
        (*E(6.6, F0 + 0.02), "OIL POOL (FLAMMABLE)", "spreads fire 0.6 m/s"),
        (*E(6.3, F0 + 1.2), "LADDER", "2.00 m, 7 rungs"),
        (*E(11.0, F0 + 0.5), "E/W ARCHWAY", "choked by amphora pile")], 600, 150, 330, slope=1.0)
    sh.callout(*E(1.3, F0 + 2.0), 250, 175, "ROW NUMBERS", "red scribe signs at 1.8 m", anchor="end")
    # ---- plan ----
    plan_frame(sh)
    for x0, y0, x1, y1 in ((0, 0, 0.7, 12), (11.3, 0, 12, 12), (0.7, 0, 11.3, 0.7), (0.7, 11.3, 11.3, 12)):
        prect(sh, x0, y0, x1, y1, "#3A332A", "#635C4C", .6)
    prect(sh, 4.7, 0, 7.3, 0.7, "#1B1813")
    prect(sh, 4.7, 11.3, 7.3, 12, "#1B1813")
    prect(sh, 0, 4.7, 0.7, 7.3, "#1B1813")
    prect(sh, 11.3, 4.7, 12, 7.3, "#1B1813")
    # amphora piles in E/W archways
    for x in (1.0, 11.0):
        for dy in (-0.5, 0, 0.5):
            sh.circle(*PL(x, 6 + dy), 0.17 * PK, "#B88A62", "#0E0C09", .6)
    # benches + jars (3 per half row)
    for i, x in enumerate(ROWS):
        for y0, y1 in ((1.7, 5.2), (6.8, 10.3)):
            prect(sh, x - 0.6, y0, x + 0.6, y1, darken(FLOOR, .2), "#635C4C", .5)
            for j in range(3):
                yc = y0 + 0.58 + j * 1.17
                c = PL(x, yc)
                col = OILC if i >= 2 else TERRA
                sh.circle(*c, 0.55 * PK, f"url(#{sh.lin(col, 'd', .3, .5)})", "#0E0C09", .8)
                sh.circle(*c, 0.25 * PK, darken(col, .45) if i >= 2 else GRAIN, "#0E0C09", .5)
    # oil pools on plan
    for x, y in ((6.0, 3.2), (7.6, 5.95), (10.6, 8.8)):
        sh.ellipse(*PL(x, y), 0.6 * PK, 0.35 * PK, SOOT, "#C4542E", .6, op=.9)
    # scribe's bench in the south strip
    prect(sh, 1.4, 0.8, 3.2, 1.4, darken(FLOOR, .1), "#DCD2BA", .6)
    # vents
    for x in (3.0, 9.0):
        a, b = PL(x - .3, 11.0), PL(x + .3, 10.4)
        sh.add(f'<rect x="{f(a[0])}" y="{f(a[1])}" width="{f(b[0] - a[0])}" height="{f(b[1] - a[1])}" fill="none" stroke="#DCD2BA" stroke-width=".8" stroke-dasharray="2 2"/>')
    # hide spots (X) at row ends on the cross-aisle
    for x in ROWS:
        for y in (5.35, 6.65):
            c = PL(x + (0.45 if x < 6 else -0.45), y)
            sh.path(f"M{f(c[0] - 3)} {f(c[1] - 3)} l6 6 m0 -6 l-6 6", "none", "#DCD2BA", 1.2)
    # section line B-B along the cross-aisle
    sh.line(*PL(-0.6, 6.0), *PL(12.6, 6.0), "#9A9078", .8, dash="8 3 2 3")
    for x in (-0.6, 12.6):
        p = PL(x, 6.0)
        sh.path(f"M{f(p[0])} {f(p[1])} l0 -10 l-4 5 m4 -5 l4 5", "none", "#9A9078", 1)
        sh.text(p[0], p[1] + 12, "B", 9, "#9A9078", "middle")
    socket_label(sh, *PL(6.0, 0.25), "DOOR S 2.60 × 2.88")
    socket_label(sh, *PL(6.0, 11.55), "DOOR N")
    socket_label(sh, *PL(2.3, 0.45), "SCRIBE", col="#9A9078")
    socket_label(sh, *PL(4.25, 6.3), "GRAIN", col=GRAIN)
    socket_label(sh, *PL(9.95, 6.3), "OIL", col="#C4542E")
    socket_label(sh, *PL(6.0, 10.8), "VENT ×2 (WINDOW)")
    legend(sh, [("DOOR", "N · S · E · W archways 2.60 × 2.88"),
                ("E/W DOORS", "half-choked by amphora piles"),
                ("WINDOW", "2 ceiling vents 0.60 × 0.60 m"),
                ("STAIR", "none — ground floor"),
                ("× HIDE", "8 crouch spots at the row ends")])
    return sh
