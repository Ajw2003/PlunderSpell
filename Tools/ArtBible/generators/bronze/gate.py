from lib import *

LIME, CONG, PLAST, MUD, OAK, BRZ, SOOT = "#8C7F68", "#9A8666", "#B7803E", "#8A5A3C", "#5A3E28", "#9B6A38", "#2B231B"
GROUND = (40, 770)
F0 = 0.30   # slab top


def cyclopean(sh, x0, x1, h0, h1, seed):
    """Irregular polygonal block courses between x0..x1 (m) and h0..h1 (m above slab)."""
    rng = sh.rng
    a, b = E(x0, h1 + F0), E(x1, h0 + F0)
    d = poly_path([a, (b[0], a[1]), b, (a[0], b[1])])
    blocks = []
    h = h0
    while h < h1 - 0.05:
        ch = min(rng.uniform(0.8, 1.5), h1 - h)
        x = x0
        while x < x1 - 0.05:
            w = min(rng.uniform(0.9, 2.1), x1 - x)
            j = lambda: rng.uniform(-0.12, 0.12)
            pts = [(x + 0.04 + j(), h + 0.04), (x + w * .5 + j(), h + 0.03 + abs(j()) * .5), (x + w - 0.04 + j(), h + 0.04),
                   (x + w - 0.03, h + ch * .5 + j()), (x + w - 0.04 + j(), h + ch - 0.04), (x + w * .4 + j(), h + ch - 0.03),
                   (x + 0.04 + j(), h + ch - 0.04), (x + 0.03, h + ch * .5 + j())]
            pp = [E(px, ph + F0) for px, ph in pts]
            tint = mix(LIME, CONG, rng.uniform(0, .5))
            tint = darken(tint, rng.uniform(0, .12)) if rng.random() < .5 else lighten(tint, rng.uniform(0, .08))
            blocks.append((pp, tint))
            x += w
        h += ch
    sh.path(d, darken(LIME, .6), darken(LIME, .7), 1)
    inner = []
    for pp, tint in blocks:
        gid = sh.lin(tint, "d", .22, .4)
        inner.append(f'<path d="{smooth_path(pp, tension=.25)}" fill="url(#{gid})" stroke="{darken(tint, .55)}" stroke-width="1"/>')
        # chamfer highlight top-left
        inner.append(f'<path d="{smooth_path(pp[5:8] + pp[0:1], closed=False, tension=.25)}" fill="none" stroke="{lighten(tint, .35)}" stroke-width="1.1" opacity=".7"/>')
    sh.clipped(d, "".join(inner))
    # chinking stones in the joints
    for _ in range(int((x1 - x0) * (h1 - h0) * 2.2)):
        px, ph = rng.uniform(x0 + .1, x1 - .1), rng.uniform(h0 + .1, h1 - .1)
        cx, cy = E(px, ph + F0)
        sh.ellipse(cx, cy, rng.uniform(2, 4.5), rng.uniform(1.5, 3), darken(LIME, .15), darken(LIME, .6), .6)
    sh.flecks(d, (a[0], a[1], b[0], b[1]), 120, SOOT, .5, 1.6, .45)
    return d


def lion_relief(sh, cx, by, k, detail=True):
    """Triangular lion slab: base centred at (cx, by) px, k px/m (base 2.40, height 0.96)."""
    tri = [(cx - 1.2 * k, by), (cx + 1.2 * k, by), (cx, by - 0.96 * k)]
    d = poly_path(tri)
    sh.path(d, f"url(#{sh.lin(CONG, 'v', .25, .35)})", darken(CONG, .6), 1.4)
    u = k / 100.0
    rel = []
    rc = darken(CONG, .38)
    hl = lighten(CONG, .3)
    # column + double plinth
    rel.append(f'<path d="M{f(cx - 14 * u)} {f(by - 8 * u)} h{f(28 * u)} v{f(-8 * u)} h{f(-28 * u)} z M{f(cx - 10 * u)} {f(by - 16 * u)} h{f(20 * u)} v{f(-6 * u)} h{f(-20 * u)} z" fill="{rc}"/>')
    rel.append(f'<path d="M{f(cx - 5.5 * u)} {f(by - 22 * u)} L{f(cx - 8 * u)} {f(by - 70 * u)} L{f(cx + 8 * u)} {f(by - 70 * u)} L{f(cx + 5.5 * u)} {f(by - 22 * u)} Z" fill="{mix(CONG, rc, .5)}" stroke="{rc}" stroke-width="1"/>')
    rel.append(f'<ellipse cx="{f(cx)}" cy="{f(by - 74 * u)}" rx="{f(12 * u)}" ry="{f(4.5 * u)}" fill="{rc}"/>')
    for s in (-1, 1):
        # rampant lion (headless), forepaws on the plinth
        body = [(cx + s * 16, by - 20), (cx + s * 30, by - 46), (cx + s * 58, by - 52), (cx + s * 84, by - 30),
                (cx + s * 92, by - 6), (cx + s * 80, by - 2), (cx + s * 74, by - 20), (cx + s * 56, by - 26),
                (cx + s * 44, by - 20), (cx + s * 40, by - 2), (cx + s * 30, by - 2), (cx + s * 30, by - 22)]
        pts = [(cx + (x - cx) * u, by + (y - by) * u) for x, y in body]
        rel.append(f'<path d="{smooth_path(pts, tension=.35)}" fill="{mix(CONG, rc, .35)}" stroke="{rc}" stroke-width="1.2"/>')
        # neck stump with dowel hole
        nx, ny = cx + s * 24 * u, by - 48 * u
        rel.append(f'<path d="M{f(nx - s * 4 * u)} {f(ny + 8 * u)} q{f(s * -2 * u)} {f(-10 * u)} {f(s * 10 * u)} {f(-12 * u)} l{f(s * 8 * u)} {f(8 * u)} z" fill="{mix(CONG, rc, .35)}" stroke="{rc}" stroke-width="1"/>')
        rel.append(f'<circle cx="{f(nx + s * 5 * u)}" cy="{f(ny - 2 * u)}" r="{f(2.2 * u)}" fill="{darken(CONG, .7)}"/>')
        # tail
        rel.append(f'<path d="M{f(cx + s * 88 * u)} {f(by - 20 * u)} q{f(s * 10 * u)} {f(-6 * u)} {f(s * 6 * u)} {f(-18 * u)}" fill="none" stroke="{rc}" stroke-width="{f(2 * u)}"/>')
        rel.append(f'<path d="M{f(cx + s * 34 * u)} {f(by - 44 * u)} q{f(s * 20 * u)} {f(-8 * u)} {f(s * 44 * u)} {f(0)}" fill="none" stroke="{hl}" stroke-width="1.2" opacity=".8"/>')
    sh.clipped(d, "".join(rel))
    if detail:
        sh.flecks(d, (cx - 1.2 * k, by - 0.96 * k, cx + 1.2 * k, by), 60, darken(CONG, .5), .5, 1.3, .5)
    return d


def build():
    mats = [("limestone", LIME), ("conglom.", CONG), ("plaster", PLAST), ("mud-brick", MUD),
            ("oak", OAK), ("bronze", BRZ), ("soot", SOOT)]
    sh = struct_sheet("CurtainWall", "The Lion Gate", 5.20, mats, seed=101)
    sh.extra_frame.append(f'<text x="{f(E(6, 0)[0])}" y="730" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">SOUTH ELEVATION · OUTSIDE FACE</text>')
    # warm torchlight from inside, spilling through the door gap
    gid = sh.rad([(0, "#C4542E", .35), (0.4, "#2A2012", .5), (1, "#14120E", 0)])
    cx, cy = E(6, 1.6)
    sh.back.append(f'<ellipse cx="{f(cx)}" cy="{f(cy)}" rx="260" ry="200" fill="url(#{gid})"/>')
    # slab
    erect(sh, 0, 0, 12, F0, f"url(#{sh.lin('#7A6A58', 'v', .1, .4)})", darken(LIME, .6))
    erect(sh, 4.7, 0.2, 7.3, F0, CONG, darken(CONG, .6))   # threshold
    # passage interior (dark, doors set back)
    erect(sh, 4.7, F0, 7.3, F0 + 3.74, darken(OAK, .55))
    # door leaves (closed, seen through the passage)
    for x0 in (4.7, 6.0):
        a = E(x0 + 0.02, F0 + 3.70)
        b = E(x0 + 1.28, F0)
        sh.path(poly_path([a, (b[0], a[1]), b, (a[0], b[1])]), f"url(#{sh.lin(OAK, 'h', .2, .5)})", darken(OAK, .6), 1)
        for i in range(1, 6):
            xx = E(x0 + i * 0.22, 0)[0]
            sh.line(xx, a[1], xx, b[1], darken(OAK, .45), 1)
        for hh in (0.5, 1.9, 3.3):
            sh.line(a[0], E(0, F0 + hh)[1], b[0], E(0, F0 + hh)[1], darken(OAK, .35), 1.2, op=.6)
    # bronze sheathing on the leading edges + nails
    for xe in (5.98, 6.02):
        sh.line(*E(xe, F0), *E(xe, F0 + 3.70), BRZ, 5)
        for i in range(12):
            sh.circle(*E(xe, F0 + 0.2 + i * 0.3), 1.4, lighten(BRZ, .4))
    # light slit between leaves
    sh.line(*E(6.0, F0 + 0.1), *E(6.0, F0 + 3.6), "#C4542E", 1.2, op=.7)
    # masses
    for x0, x1, sd in ((0.3, 4.7, 1), (7.3, 11.7, 2)):
        cyclopean(sh, x0, x1, 0, 4.10, sd)
    # jambs (dressed conglomerate) and lintel
    for x0 in (3.8, 7.3):
        erect(sh, x0, F0, x0 + 0.9, F0 + 3.74, f"url(#{sh.lin(CONG, 'h', .3, .45)})", darken(CONG, .6), 1.2)
        # cart scuff
        erect(sh, x0, F0 + 0.4, x0 + 0.9, F0 + 0.6, darken(CONG, .3), op=.5)
    sh.path(poly_path([E(3.8, F0 + 3.74), E(8.2, F0 + 3.74), E(8.2, F0 + 4.24), E(6.0, F0 + 4.29), E(3.8, F0 + 4.24)]),
            f"url(#{sh.lin(CONG, 'v', .35, .45)})", darken(CONG, .6), 1.4)
    # corbelled masonry either side of the relieving triangle (stepped courses)
    cd = poly_path([E(4.7, F0 + 4.24), E(7.3, F0 + 4.24), E(7.3, F0 + 5.20), E(4.7, F0 + 5.20)])
    sh.path(cd, f"url(#{sh.lin(LIME, 'd', .2, .4)})", darken(LIME, .6), 1)
    for i in range(1, 3):
        sh.line(*E(4.7, F0 + 4.24 + i * 0.32), *E(7.3, F0 + 4.24 + i * 0.32), darken(LIME, .55), 1)
    for i in range(3):
        for s_ in (-1, 1):
            xs = 6.0 + s_ * (1.2 - i * 0.4)
            sh.line(*E(xs, F0 + 4.24 + i * 0.32), *E(xs, F0 + 4.24 + (i + 1) * 0.32), darken(LIME, .55), 1)
    lion_relief(sh, *E(6.0, F0 + 4.24), SK)
    # parapets
    for x0, x1 in ((0.3, 4.7), (7.3, 11.7)):
        a, b = E(x0, F0 + 5.20), E(x1, F0 + 4.10)
        erect(sh, x0, F0 + 4.10, x1, F0 + 4.75, f"url(#{sh.lin(MUD, 'v', .2, .45)})", darken(MUD, .6))
        x = x0
        n = 0
        while x < x1 - 0.3:
            w = 0.8
            mx0, mx1 = x, min(x + w, x1)
            pts = [E(mx0, F0 + 4.75), E(mx0, F0 + 5.05), E(mx0 + 0.15, F0 + 5.20), E(mx1 - 0.15, F0 + 5.20), E(mx1, F0 + 5.05), E(mx1, F0 + 4.75)]
            sh.path(smooth_path(pts, tension=.3), f"url(#{sh.lin(MUD, 'h', .25, .45)})", darken(MUD, .6), 1)
            if n % 2 == 1:
                sh.path(poly_path([E(mx0 + 0.34, F0 + 4.45), E(mx0 + 0.46, F0 + 4.45), E(mx0 + 0.46, F0 + 4.95), E(mx0 + 0.34, F0 + 4.95)]), darken(SOOT, .3))
            x += w + 0.6
            n += 1
        # brick coursing
        for hh in (4.3, 4.5):
            sh.line(*E(x0, F0 + hh), *E(x1, F0 + hh), darken(MUD, .4), .8, op=.6)
        sh.flecks(poly_path([E(x0, F0 + 5.2), E(x1, F0 + 5.2), E(x1, F0 + 4.1), E(x0, F0 + 4.1)]),
                  (E(x0, 0)[0], E(0, F0 + 5.2)[1], E(x1, 0)[0], E(0, F0 + 4.1)[1]), 40, lighten(MUD, .4), .6, 1.4, .5)
    human(sh, 5.35)
    # detail inset: lion relief 1 m = 110 px
    ix, iy = 330, 300
    sh.extra_frame.append(f'<text x="{ix}" y="150" text-anchor="middle" font-family="{MONO}" font-size="10" letter-spacing="2" fill="#635C4C">LION SLAB DETAIL · 1 m = 110 px</text>')
    lion_relief(sh, ix, iy, 110)
    sh.text(ix, iy + 16, "2.40 × 0.96 × 0.50 m · heads missing (dowel holes)", 9.5, "#9A9078", "middle")
    # callouts on the elevation
    sh.callout(*E(10.5, F0 + 2.3), 620, 250, "CYCLOPEAN MASS", "4.40 × 3.20 × 4.10 m")
    sh.callout(*E(9.5, F0 + 4.9), 620, 190, "PARAPET 1.10 m", "mud-brick, sling slots")
    sh.callout(*E(4.3, F0 + 3.99), 290, 352, "LINTEL", "4.40 × 3.20 × 0.50 m", anchor="end", elbow=False)
    # dimension: opening
    ax, ay = E(4.7, F0 + 3.74)
    bx, by = E(7.3, F0)
    sh.line(ax, by + 14, bx, by + 14, "#9A9078", .8)
    sh.text((ax + bx) / 2, by + 26, "2.60", 9, "#9A9078", "middle")
    sh.line(ax + 8, ay, ax + 8, by, "#DCD2BA", .8, dash="2 3")
    sh.text(ax + 12, (ay + by) / 2 - 40, "3.74", 9.5, "#DCD2BA")
    # ---- plan ----
    plan_frame(sh)
    wall = darken(LIME, .2)
    for x0, x1 in ((0.3, 4.7), (7.3, 11.7)):
        a, b = PL(x0, 7.6), PL(x1, 4.4)
        d = poly_path([a, (b[0], a[1]), b, (a[0], b[1])])
        sh.path(d, wall, darken(LIME, .6), 1.2)
        hatch(sh, d, (a[0], a[1], b[0], b[1]), darken(LIME, .6), 5, .6)
        # parapet line on the south edge
        prect(sh, x0, 4.4, x1, 4.8, MUD, darken(MUD, .6), .8)
    # jambs darker
    prect(sh, 3.8, 4.4, 4.7, 7.6, CONG, darken(CONG, .6), 1)
    prect(sh, 7.3, 4.4, 8.2, 7.6, CONG, darken(CONG, .6), 1)
    # paved strip
    prect(sh, 4.4, 0, 7.6, 12, "#2A251D", op=.8)
    for yy in range(1, 12):
        sh.line(*PL(4.4, yy), *PL(7.6, yy), "#332D22", .8)
    for xr in (5.3, 6.7):
        sh.line(*PL(xr, 0), *PL(xr, 12), "#4A4234", 1, dash="4 3")
    # doors: leaves swing north (inward), bar
    for s in (-1, 1):
        hx, hy = PL(6.0 + s * 1.3, 7.4)
        sh.line(hx, hy, *PL(6.0 + s * 1.3 - s * 0.9, 8.3), OAK, 3)
        sh.path(f"M{f(PL(6.0, 7.4)[0])} {f(hy)} A{f(1.3 * PK)} {f(1.3 * PK)} 0 0 {0 if s < 0 else 1} {f(PL(6.0 + s * 1.3 - s * 0.9, 8.3)[0])} {f(PL(6.0 + s * 1.3 - s * 0.9, 8.3)[1])}",
                "none", "#635C4C", .8, extra=' stroke-dasharray="2 3"')
    sh.line(*PL(4.4, 7.2), *PL(7.6, 7.2), OAK, 3.5, op=.8)
    # stair on the east mass, north face
    prect(sh, 7.6, 7.6, 11.6, 8.6, "#3A332A", "#635C4C", .8)
    for i in range(15):
        sh.line(*PL(7.6 + i * 4.0 / 14, 7.6), *PL(7.6 + i * 4.0 / 14, 8.6), "#635C4C", .7)
    sh.path(f"M{f(PL(7.8, 8.1)[0])} {f(PL(7.8, 8.1)[1])} L{f(PL(11.3, 8.1)[0])} {f(PL(11.3, 8.1)[1])} l-6 -4 m6 4 l-6 4", "none", "#DCD2BA", 1)
    # sling slots
    for x in (1.2, 2.5, 3.8, 8.2, 9.5, 10.8):
        a = PL(x, 4.4)
        sh.path(f"M{f(a[0] - 2)} {f(a[1])} l-4 10 h12 l-4 -10 z", "#14120E", "#DCD2BA", .6)
    # torch sockets
    for x in (4.4, 7.6):
        a = PL(x, 7.75)
        sh.circle(*a, 3.5, BRZ, "#DCD2BA", .6)
        g2 = sh.rad([(0, "#C4542E", .5), (1, "#C4542E", 0)])
        sh.circle(*a, 18, f"url(#{g2})")
    # labels
    socket_label(sh, *PL(6.0, 1.2), "DOOR 2.60 × 3.74")
    socket_label(sh, *PL(9.6, 8.95), "STAIR → WALK 4.10")
    socket_label(sh, *PL(2.5, 3.6), "ARROW-LOOPS ×6")
    socket_label(sh, *PL(6.0, 10.6), "INSIDE (NORTH)", col="#9A9078")
    socket_label(sh, *PL(2.5, 6.0), "SOLID MASS")
    socket_label(sh, *PL(9.6, 6.0), "SOLID MASS")
    socket_label(sh, *PL(2.4, 8.6), "TORCH ×2 (N FACE)", col="#C4542E")
    legend(sh, [("DOOR", "gate passage, double oak leaves, barred N"),
                ("STAIR", "1.00 m, 14 risers, east mass N face"),
                ("ARROW-LOOP", "sling slots 0.12 × 0.50 m in parapet"),
                ("MURDER-HOLE", "none (flanked, not overhead)"),
                ("TORCH", "2 bronze rings N face at 2.20 m")])
    return sh
