from lib import *
from gallery import label

WAL = "#3B2A1E"; PLASTER = "#D2C7AC"; GLASS = "#7E9377"; CROC = "#4E4A36"; GESSO = "#BFAE86"; BAIZE = "#3E4A30"
SPIRIT = "#B6A56A"; BRASS = "#8D7340"; BELLY = "#8A8266"; DUST = "#5A5245"; IRON = "#2A2622"
CX, FLOOR, S = 250, 663, 70          # section: x = 0 at the cell centreline, 1 m = 70 px, shows x -2.0 .. 6.0
E = lambda x, z: (CX + x * S, FLOOR - z * S)
H = 4.60                               # clear height to the beam soffit
CEIL = 4.90                            # coffered ceiling plane (beams hang 0.30 below it)
XL = -2.0                              # break line at the west edge of the drawn section
WI = 5.4                               # east wall inner face


def R(x0, z0, x1, z1):
    """rect args (x, y, w, h) for a metre box."""
    a, b = E(x0, z1), E(x1, z0)
    return a[0], a[1], b[0] - a[0], b[1] - a[1]


def arch_pts(half=1.3, spring=2.01, n=16, cx=0.0, z0=0.0):
    pts = [E(cx - half, z0), E(cx + half, z0), E(cx + half, spring)]
    pts += [E(cx + half * math.cos(a), spring + half * math.sin(a)) for a in [i * math.pi / n for i in range(1, n)]]
    pts += [E(cx - half, spring)]
    return pts


def shell(sh):
    # north wall inner face (plaster), soot toward the ceiling
    g = sh.lin([(0, dk(PLASTER, 0.6)), (0.3, dk(PLASTER, 0.3)), (1, dk(PLASTER, 0.05))], 0, 0, 0, 1)
    sh.rect(*R(XL, 0, WI, CEIL), f"url(#{g})")
    sh.flecks(*E(XL + 0.1, CEIL - 0.1), *E(WI - 0.1, 3.9), 50, dk(PLASTER, 0.35), 1, 3, (0.05, 0.15))
    # north archway 2.60 x 3.31 with the next room glimpsed
    ap = arch_pts()
    gi = sh.lin([(0, "#0E0C0A"), (1, "#221D16")], 0, 0, 0, 1)
    sh.path(poly_path(ap), f"url(#{gi})", INK, 1)
    for k, sc in enumerate((0.55, 0.32)):
        z0 = 0.55 - k * 0.2
        ip = [E(-1.3 * sc, z0), E(1.3 * sc, z0), E(1.3 * sc, z0 + 2.01 * sc)] + \
             [E(1.3 * sc * math.cos(a), z0 + (2.01 + 1.3 * math.sin(a)) * sc) for a in [i * math.pi / 12 for i in range(1, 12)]] + \
             [E(-1.3 * sc, z0 + 2.01 * sc)]
        sh.path(poly_path(ip), "none", "#3A3226", 1.2, op=0.8)
    # walnut architrave round the arch
    arc = [E(1.3, 0), E(1.3, 2.01)] + [E(1.3 * math.cos(a), 2.01 + 1.3 * math.sin(a)) for a in [i * math.pi / 16 for i in range(1, 16)]] + [E(-1.3, 2.01), E(-1.3, 0)]
    sh.line(arc, WAL, 0.14 * S, smooth=False)
    sh.line([E(1.37, 0), E(1.37, 2.01)] + [E(1.37 * math.cos(a), 2.01 + 1.37 * math.sin(a)) for a in [i * math.pi / 16 for i in range(1, 16)]] + [E(-1.37, 2.01), E(-1.37, 0)],
            lit(WAL, 0.4), 1.1, smooth=False, op=0.8)
    # case returns into the archway (0.30 strip each side)
    for x0 in (1.3, -1.6):
        sh.rect(*R(x0 + 0.1, 0, x0 + 0.3, 3.85), f"url(#{sh.form(WAL, 'h', 0.35)})", INK, 0.8)
    # floor slab (cut) and floor line
    poche(sh, *E(XL, 0), (6.0 - XL) * S, 0.3 * S)
    sh.rect(*R(XL, 0, WI, 0.03), dk(PLASTER, 0.45), INK, 0.6)
    # ceiling slab (cut) with coffer recesses 0.15 deep on a 1.20 m grid
    poche(sh, *E(XL, CEIL + 0.35), (6.0 - XL) * S, 0.35 * S)
    for i in range(-2, 5):
        xa, xb = i * 1.2 - 0.6 + 0.125, i * 1.2 + 0.6 - 0.125
        xa, xb = max(xa, XL), min(xb, WI)
        if xb <= xa:
            continue
        sh.rect(*R(xa, CEIL, xb, CEIL + 0.15), f"url(#{sh.form(WAL, 'v', 0.25, 0.4)})", INK, 0.7)
        # painted coffer panel: gesso rosette
        cx, cy = E((xa + xb) / 2, CEIL + 0.075)
        sh.ellipse(cx, cy, 0.12 * S, 2.2, GESSO, op=0.55)
    # central E-W beam (seen in elevation, just north of the cut) 0.25 x 0.30, soffit at 4.60
    sh.rect(*R(XL, H, WI, CEIL), f"url(#{sh.form(WAL, 'v', 0.35, 0.5)})", INK, 1)
    sh.line([E(XL, H + 0.04), E(WI, H + 0.04)], lit(WAL, 0.45), 1.2, smooth=False, op=0.8)
    for i in range(18):
        x = XL + 0.2 + i * 0.42
        cx, cy = E(x, H + 0.15)
        sh.ellipse(cx, cy, 5, 2.4, GESSO, op=0.35)
    # soot above the candle-crown
    sh.glow(*E(2.5, 4.7), 120, "#14120E", 0.75)
    # east wall cut through its archway: poche above the crown, dark void below
    poche(sh, *E(WI, CEIL + 0.35), 0.6 * S, (CEIL + 0.35 - 3.31) * S)
    gv = sh.lin([(0, "#0E0C0A"), (1, "#1E1A14")], 0, 0, 1, 0)
    sh.rect(*R(WI, 0, 6.0, 3.31), f"url(#{gv})", INK, 1)
    sh.text(E(5.7, 1.65)[0], E(5.7, 1.65)[1], "ARCH E", 8, FRAME_TXT, "middle")
    # break line on the west edge
    zz = [E(XL + (0.06 if k % 2 else -0.06), -0.3 + k * 0.35) for k in range(17)]
    sh.line(zz, FRAME_TXT, 1, smooth=False)


def jar(sh, x, z, h, w):
    """specimen jar standing on a shelf at (x, z)."""
    xa, xb = x - w / 2, x + w / 2
    sh.rect(*R(xa, z, xb, z + h * 0.82), SPIRIT, INK, 0.5, op=0.85)
    sh.rect(*R(xa, z + h * 0.82, xb, z + h), lit(GLASS, 0.3), INK, 0.5, op=0.6)
    sh.rect(*R(xa - 0.01, z + h, xb + 0.01, z + h + 0.025), dk(SPIRIT, 0.35), INK, 0.4)   # wax-and-bladder lid
    cx, cy = E(x, z + h * 0.4)
    sh.ellipse(cx, cy, w * S * 0.28, h * S * 0.22, dk(SPIRIT, 0.55), op=0.75)             # floating specimen
    sh.line([E(xa + 0.015, z + 0.02), E(xa + 0.015, z + h * 0.78)], lit(SPIRIT, 0.6), 1, smooth=False, op=0.7)


def curios(sh, x0, x1):
    """shelf contents between x0 and x1 (behind glass)."""
    rnd = sh.rnd
    for zs in (1.10, 1.60, 2.10, 2.60, 3.10):
        x = x0 + 0.06
        while x < x1 - 0.12:
            kind = rnd.random()
            if kind < 0.55:
                h = rnd.choice((0.12, 0.20, 0.32)) if zs < 3.1 else rnd.choice((0.12, 0.20))
                w = h * 0.55
                jar(sh, x + w / 2, zs + 0.02, h, w)
                x += w + 0.05
            elif kind < 0.75:
                # coral / horn branch
                bx, by = E(x + 0.08, zs + 0.02)
                sh.line([(bx, by), (bx - 2, by - 12), (bx - 6, by - 20)], lit(GESSO, 0.25), 1.6)
                sh.line([(bx, by - 8), (bx + 5, by - 17), (bx + 4, by - 24)], lit(GESSO, 0.25), 1.4)
                sh.line([(bx - 2, by - 12), (bx - 1, by - 26)], lit(GESSO, 0.25), 1.2)
                x += 0.20
            elif kind < 0.9:
                # shell
                cx, cy = E(x + 0.07, zs + 0.06)
                sh.ellipse(cx, cy, 5, 4, lit(PLASTER, 0.1), INK, 0.5)
                sh.line([(cx - 4, cy + 2), (cx, cy - 4), (cx + 4, cy + 2)], dk(PLASTER, 0.3), 0.6, smooth=False)
                x += 0.16
            else:
                x += 0.12


def case_run(sh, x0, x1, bays, hide_bay=None):
    """north-wall case in elevation: cupboard base 0-0.90, glazed upper 0.90-3.60, cornice 3.60-3.85."""
    # base cupboard
    sh.rect(*R(x0, 0, x1, 0.90), f"url(#{sh.form(WAL, 'v', 0.3, 0.45)})", INK, 1)
    sh.rect(*R(x0, 0, x1, 0.08), dk(WAL, 0.4), INK, 0.6)
    bw = (x1 - x0) / bays
    for i in range(bays):
        a = x0 + i * bw
        sh.rect(*R(a + 0.05, 0.14, a + bw - 0.05, 0.82), "none", lit(WAL, 0.35), 1, op=0.8)
        sh.rect(*R(a + 0.10, 0.20, a + bw - 0.10, 0.76), f"url(#{sh.form(WAL, 'd', 0.25, 0.4)})", INK, 0.6)
        cx, cy = E(a + bw - 0.14, 0.50)
        sh.circle(cx, cy, 1.6, BRASS)
    if hide_bay is not None:
        a = x0 + hide_bay * bw
        sh.rect(*R(a + 0.03, 0.10, a + bw - 0.03, 0.86), "none", VELLUM, 1, op=0.7)
        cx, cy = E(a + bw / 2, 0.47)
        sh.text(cx, cy + 3, "HIDE", 8, VELLUM, "middle", ls="1")
    # upper case: back, shelves, contents
    sh.rect(*R(x0, 0.90, x1, 3.60), f"url(#{sh.form(WAL, 'h', 0.2, 0.5)})", INK, 1)
    sh.rect(*R(x0 + 0.05, 0.95, x1 - 0.05, 3.55), dk(WAL, 0.35))
    for zs in (1.10, 1.60, 2.10, 2.60, 3.10):
        sh.rect(*R(x0 + 0.05, zs - 0.03, x1 - 0.05, zs), lit(WAL, 0.3), INK, 0.4)
    curios(sh, x0 + 0.05, x1 - 0.05)
    # glazed doors 0.60 x 2.60 with 3 x 6 panes, fingerprint smudges at lock height
    dw = (x1 - x0 - 0.10) / max(1, round((x1 - x0 - 0.10) / 0.6))
    n = int(round((x1 - x0 - 0.10) / dw))
    for i in range(n):
        a = x0 + 0.05 + i * dw
        sh.rect(*R(a, 0.95, a + dw, 3.55), GLASS, op=0.16)
        sh.path(poly_path([E(a + 0.05, 1.6), E(a + 0.35, 3.3), E(a + 0.45, 3.3), E(a + 0.12, 1.4)]), lit(GLASS, 0.5), op=0.12)
        for k in range(1, 3):
            xx = a + k * dw / 3
            sh.line([E(xx, 0.95), E(xx, 3.55)], dk(WAL, 0.2), 1.1, smooth=False)
        for k in range(1, 6):
            zz = 0.95 + k * 2.6 / 6
            sh.line([E(a, zz), E(a + dw, zz)], dk(WAL, 0.2), 1.1, smooth=False)
        sh.rect(*R(a, 0.95, a + dw, 3.55), "none", lit(WAL, 0.25), 2.2)
        cx, cy = E(a + dw / 2, 1.18)
        sh.ellipse(cx, cy, 0.2 * S, 0.12 * S, lit(GLASS, 0.6), op=0.1)
        kx, ky = E(a + dw - 0.05, 1.10)
        sh.circle(kx, ky, 1.5, BRASS)
    # cornice 3.60 - 3.85, dust on top
    sh.rect(*R(x0 - 0.05, 3.60, x1 + 0.05, 3.70), f"url(#{sh.form(WAL, 'v', 0.4)})", INK, 0.8)
    sh.rect(*R(x0 - 0.08, 3.70, x1 + 0.08, 3.78), f"url(#{sh.form(WAL, 'v', 0.35)})", INK, 0.8)
    sh.rect(*R(x0 - 0.12, 3.78, x1 + 0.12, 3.85), f"url(#{sh.form(WAL, 'v', 0.45)})", INK, 0.8)
    sh.line([E(x0 - 0.12, 3.852), E(x1 + 0.12, 3.852)], DUST, 1.6, smooth=False)


def crest(sh):
    # narwhal tusk 2.20 m laid along the north cornice
    t0, t1 = 1.95, 4.15
    pts_top = [E(t0 + (t1 - t0) * k / 12, 3.86 + 0.07 * (1 - k / 12) + 0.005) for k in range(13)]
    pts_bot = [E(t0 + (t1 - t0) * k / 12, 3.86) for k in range(13)]
    sh.path(poly_path(pts_top + list(reversed(pts_bot))), f"url(#{sh.form(PLASTER, 'v', 0.3, 0.35)})", INK, 0.8)
    for k in range(22):
        x = t0 + 0.05 + k * 0.095
        th = 0.07 * (1 - (x - t0) / (t1 - t0))
        a, b = E(x, 3.86), E(x + 0.05, 3.86 + th)
        sh.line([a, b], dk(PLASTER, 0.35), 0.7, smooth=False, op=0.8)
    # turtle shells at the east end, antlers at the west end of the run
    cx, cy = E(4.55, 3.86)
    sh.path(f"M{fmt(cx - 18)} {fmt(cy)} Q{fmt(cx)} {fmt(cy - 26)} {fmt(cx + 18)} {fmt(cy)} Z", f"url(#{sh.form(CROC, 'sphere', 0.4)})", INK, 0.8)
    for d in (-8, 0, 8):
        sh.line([(cx + d, cy - 2), (cx + d * 0.6, cy - 14)], dk(CROC, 0.4), 0.8, smooth=False)
    ax, ay = E(1.72, 3.87)
    sh.line([(ax, ay), (ax - 2, ay - 14), (ax - 12, ay - 30)], lit(GESSO, 0.1), 2)
    sh.line([(ax - 2, ay - 14), (ax + 10, ay - 28)], lit(GESSO, 0.1), 1.8)
    sh.line([(ax - 6, ay - 22), (ax - 2, ay - 34)], lit(GESSO, 0.1), 1.4)
    sh.line([(ax + 4, ay - 20), (ax + 12, ay - 22)], lit(GESSO, 0.1), 1.4)
    sh.ellipse(ax, ay - 2, 5, 3, dk(GESSO, 0.3), INK, 0.6)


def table(sh):
    # walnut table 2.40 x 1.10 x 0.82 at the cell centre, north of the cut
    x0, x1 = -1.2, 1.2
    sh.rect(*R(x0, 0.76, x1, 0.82), BAIZE, INK, 0.8)
    sh.line([E(x0 + 0.05, 0.815), E(x1 - 0.05, 0.815)], lit(BAIZE, 0.4), 1, smooth=False, op=0.8)
    sh.rect(*R(x0 + 0.05, 0.62, x1 - 0.05, 0.76), f"url(#{sh.form(WAL, 'v', 0.4, 0.5)})", INK, 0.8)
    for lx in (-1.05, -0.35, 0.35, 1.05):
        pts = [(-0.04, 0.62), (-0.04, 0.55), (-0.11, 0.45), (-0.11, 0.30), (-0.05, 0.22), (-0.04, 0.05), (-0.06, 0.0),
               (0.06, 0.0), (0.04, 0.05), (0.05, 0.22), (0.11, 0.30), (0.11, 0.45), (0.04, 0.55), (0.04, 0.62)]
        sh.shape([E(lx + a, b) for a, b in pts], WAL, smooth=True, direction="h", light=0.45, dark=0.5, sw=0.8, tension=0.35)
    sh.rect(*R(x0 + 0.1, 0.10, x1 - 0.1, 0.14), f"url(#{sh.form(WAL, 'v', 0.4)})", INK, 0.6)
    # loot sockets: Kunstschrank 0.95 x 1.30 on the centre, instrument slots at the ends
    sh.rect(*R(-0.475, 0.82, 0.475, 2.12), "none", VELLUM, 1, op=0.75)
    sh.path(f"M{fmt(E(-0.475, 0.82)[0])} {fmt(E(-0.475, 0.82)[1])} L{fmt(E(0.475, 2.12)[0])} {fmt(E(0.475, 2.12)[1])}", "none", VELLUM, 0.6, op=0.4, extra=' stroke-dasharray="3 3"')
    sh.add(f'<rect x="{fmt(R(-0.475, 0.82, 0.475, 2.12)[0])}" y="{fmt(R(-0.475, 0.82, 0.475, 2.12)[1])}" width="{fmt(0.95 * S)}" height="{fmt(1.30 * S)}" fill="none" stroke="#14120E" stroke-width="1" stroke-dasharray="4 4"/>')
    cx, cy = E(0, 1.47)
    sh.text(cx, cy - 4, "CABINET", 8, VELLUM, "middle", ls="1")
    sh.text(cx, cy + 8, "SOCKET", 8, VELLUM, "middle", ls="1")
    for sx in (-0.95, 0.95):
        sh.rect(*R(sx - 0.12, 0.82, sx + 0.12, 1.16), "none", VELLUM, 0.8, op=0.6)
    cx, cy = E(-0.95, 1.21)
    sh.text(cx, cy, "SLOT", 7, FRAME_TXT, "middle")
    cx, cy = E(0.95, 1.21)
    sh.text(cx, cy, "SLOT", 7, FRAME_TXT, "middle")


def globe(sh, x):
    # 0.70 m sphere in a walnut stand; horizon ring at 0.95, total 1.25
    for lx, op in ((-0.42, 0.7), (0.42, 0.7), (-0.2, 1), (0.2, 1)):
        pts = [(-0.035, 0.95), (-0.045, 0.7), (-0.06, 0.55), (-0.04, 0.4), (-0.03, 0.05), (0.03, 0.05), (0.04, 0.4), (0.06, 0.55), (0.045, 0.7), (0.035, 0.95)]
        sh.shape([E(x + lx + a, b) for a, b in pts], WAL if op == 1 else dk(WAL, 0.3), smooth=True, direction="h", light=0.45, sw=0.7, tension=0.35)
    sh.rect(*R(x - 0.42, 0.16, x + 0.42, 0.20), f"url(#{sh.form(WAL, 'v', 0.4)})", INK, 0.6)
    cx, cy = E(x, 0.90)
    r = 0.35 * S
    sh.circle(cx, cy, r, f"url(#{sh.form(GESSO, 'sphere', 0.3, 0.55)})", INK, 1)
    sh.clip_open(f"M{fmt(cx - r)} {fmt(cy)} A{fmt(r)} {fmt(r)} 0 1 0 {fmt(cx + r)} {fmt(cy)} A{fmt(r)} {fmt(r)} 0 1 0 {fmt(cx - r)} {fmt(cy)} Z")
    for k in range(-5, 6):
        sh.ellipse(cx, cy, abs(k) / 6 * r + 0.5, r, "none", dk(GESSO, 0.4), 0.6, op=0.55)
    for k in (-0.6, -0.3, 0.3, 0.6):
        sh.line([(cx - r, cy + k * r), (cx + r, cy + k * r)], dk(GESSO, 0.4), 0.5, smooth=False, op=0.5)
    # continents blotched in the gores
    for (dx, dy, rx, ry) in ((-0.3, -0.2, 0.25, 0.3), (0.25, 0.1, 0.2, 0.35), (-0.05, 0.45, 0.3, 0.12)):
        sh.ellipse(cx + dx * r, cy + dy * r, rx * r, ry * r, dk(GESSO, 0.25), op=0.5, rot=20)
    sh.rect(cx - r, cy - r, 2 * r, 2 * r, f"url(#{sh.rad([(0, '#F2D9A8', 0.0), (1, '#6A4A20', 0.35)])})")
    sh.close()
    sh.circle(cx, cy, r + 3, "none", BRASS, 2.2)                 # meridian ring
    sh.circle(cx - 0.8, cy - 0.8, r + 3, "none", lit(BRASS, 0.4), 0.6, op=0.7)
    sh.ellipse(cx, E(x, 0.95)[1], 0.45 * S, 3.5, f"url(#{sh.form(WAL, 'v', 0.45)})", INK, 0.8)   # horizon ring
    sh.rect(cx - 2, cy - r - 8, 4, 6, BRASS)


def crocodile(sh):
    # 3.40 m stuffed Nile crocodile, head west, underside at 3.20, hung from two hooks at x = +/-1.00
    bz = 3.20
    top = [(-1.72, 0.07), (-1.55, 0.10), (-1.35, 0.14), (-1.2, 0.22), (-1.05, 0.24), (-0.9, 0.30), (-0.5, 0.36), (-0.1, 0.36),
           (0.2, 0.32), (0.5, 0.24), (0.9, 0.16), (1.3, 0.09), (1.72, 0.03)]
    bot = [(1.72, 0.0), (1.2, 0.0), (0.6, 0.0), (0.1, -0.005), (-0.4, -0.005), (-0.9, 0.0), (-1.15, 0.02), (-1.35, 0.03), (-1.72, 0.035)]
    body = [E(x, bz + z) for x, z in top] + [E(x, bz + z) for x, z in bot]
    d = sh.shape(body, CROC, smooth=True, direction="v", light=0.25, dark=0.5, sw=1.1, tension=0.4)
    sh.clip_open(d)
    # pale belly band, dusty back
    sh.rect(*R(-1.8, bz - 0.05, 1.8, bz + 0.06), BELLY, op=0.75)
    sh.rect(*R(-1.8, bz + 0.26, 1.8, bz + 0.45), DUST, op=0.55)
    # belly scale bands
    for k in range(40):
        x = -1.2 + k * 0.07
        sh.line([E(x, bz), E(x, bz + 0.06)], dk(BELLY, 0.3), 0.6, smooth=False, op=0.6)
    # flank scute grid
    for row, zr in enumerate((0.10, 0.17, 0.24)):
        for k in range(34):
            x = -0.95 + k * 0.08 + (0.04 if row % 2 else 0)
            cx, cy = E(x, bz + zr)
            sh.rect(cx - 2.4, cy - 2, 4.8, 4, "none", dk(CROC, 0.4), 0.6, op=0.7)
    # polished snout
    sh.path(poly_path([E(-1.72, bz + 0.05), E(-1.3, bz + 0.13), E(-1.3, bz + 0.10), E(-1.7, bz + 0.04)]), lit(CROC, 0.5), op=0.6)
    sh.close()
    # dorsal scutes (osteoderm ridge) along the back
    for k in range(26):
        x = -0.95 + k * 0.1
        zt = interp_top(top, x)
        a, b, c = E(x - 0.035, bz + zt - 0.01), E(x, bz + zt + 0.04 * (1 - max(x, 0) / 2)), E(x + 0.035, bz + zt - 0.01)
        sh.path(poly_path([a, b, c]), dk(CROC, 0.2), INK, 0.5)
    # slightly open jaws, teeth, glass eye
    sh.line([E(-1.72, bz + 0.04), E(-1.45, bz + 0.07), E(-1.2, bz + 0.1)], INK, 1.2, smooth=False)
    for k in range(6):
        x = -1.66 + k * 0.07
        cx, cy = E(x, bz + 0.045 + k * 0.004)
        sh.path(f"M{fmt(cx - 1.4)} {fmt(cy)} l1.4 3 l1.4 -3 z", PLASTER)
    ex, ey = E(-1.12, bz + 0.2)
    sh.ellipse(ex, ey, 4, 3, SPIRIT, INK, 0.6)
    sh.circle(ex, ey, 1.4, INK)
    sh.circle(ex - 1.2, ey - 1, 0.9, "#F2D9A8")
    # legs splayed (stuffed pose)
    for lx, s in ((-0.75, -1), (-0.55, 1), (0.25, -1), (0.45, 1)):
        a = E(lx, bz + 0.04)
        pts = [a, (a[0] + s * 6, a[1] + 6), (a[0] + s * 12, a[1] + 8), (a[0] + s * 13, a[1] + 11), (a[0] + s * 3, a[1] + 9), (a[0] - s * 3, a[1] + 3)]
        sh.shape(pts, dk(CROC, 0.1), smooth=True, direction="v", light=0.2, sw=0.7)
    # hooks + chains to the beam soffit
    for hx, attach in ((-1.0, bz + 0.31), (1.0, bz + 0.16)):
        top_y, hook_y = E(hx, H)[1], E(hx, attach + 0.60)[1]
        sh.line([(E(hx, 0)[0], top_y), (E(hx, 0)[0], hook_y)], IRON, 2.4, smooth=False)
        sh.path(f"M{fmt(E(hx, 0)[0])} {fmt(hook_y)} q-5 3 0 7", "none", IRON, 1.8)
        y = hook_y + 5
        k = 0
        while y < E(hx, attach)[1] - 3:
            if k % 2:
                sh.line([(E(hx, 0)[0], y - 3), (E(hx, 0)[0], y + 3)], lit(IRON, 0.3), 1.6, smooth=False)
            else:
                sh.ellipse(E(hx, 0)[0], y, 2.3, 3.6, "none", lit(IRON, 0.35), 1.2)
            y += 5
            k += 1
        sh.rect(E(hx, 0)[0] - 6, E(hx, attach)[1] - 1, 12, 3, IRON)


def interp_top(top, x):
    for (x0, z0), (x1, z1) in zip(top, top[1:]):
        if x0 <= x <= x1:
            return z0 + (z1 - z0) * (x - x0) / (x1 - x0)
    return top[-1][1]


def crown(sh, x):
    # iron candle-crown 1.00 m dia, 8 candles, ring at 3.00 m, key light
    sh.glow(*E(x, 3.1), 280, "#F2D9A8", 0.22)
    sh.glow(*E(x, 3.1), 130, "#C4542E", 0.12)
    apex = E(x, 3.62)
    sh.line([E(x, H), apex], IRON, 2, smooth=False)
    for dx in (-0.5, -0.17, 0.17, 0.5):
        sh.line([apex, E(x + dx, 3.02)], IRON, 0.9, smooth=False, op=0.9)
    cx, cy = E(x, 3.0)
    sh.ellipse(cx, cy, 0.5 * S, 4, "none", IRON, 2.6)
    sh.ellipse(cx, cy - 0.6, 0.5 * S, 4, "none", lit(IRON, 0.35), 0.6, op=0.8)
    for dx in (-0.46, -0.33, -0.12, 0.12, 0.33, 0.46):
        bx, by = E(x + dx, 3.0 + (0.01 if abs(dx) < 0.2 else 0))
        sh.rect(bx - 1.8, by - 11, 3.6, 11, PLASTER, INK, 0.4)
        sh.glow(bx, by - 15, 11, "#F2D9A8", 0.9)
        sh.path(f"M{fmt(bx)} {fmt(by - 20)} q3 4 0 7 q-3 -3 0 -7 z", "#F2D9A8")


def section(sh):
    shell(sh)
    case_run(sh, 1.6, 4.95, 3, hide_bay=0)
    case_run(sh, XL, -1.6, 1)
    # east case end panel seen in elevation (runs along the east wall)
    sh.rect(*R(4.95, 0, WI, 3.85), f"url(#{sh.form(WAL, 'h', 0.3, 0.55)})", INK, 1)
    sh.rect(*R(5.0, 0.95, 5.35, 3.55), "none", lit(WAL, 0.25), 1, op=0.8)
    crest(sh)
    table(sh)
    globe(sh, 3.4)
    human(sh, E(4.45, 0)[0], FLOOR, S)
    crocodile(sh)
    crown(sh, 2.5)
    # dims
    sh.line([E(-1.72, 3.05), E(1.72, 3.05)], FRAME_TXT, 0.7, smooth=False, dash="3 3")
    sh.text(E(0.05, 3.72)[0], E(0, 3.72)[1] + 3, "3.40 m · underside 3.20", 9, "#2A2622", "middle")
    # clear height tick
    sh.line([E(WI - 0.05, H), E(WI + 0.1, H)], VELLUM, 1, smooth=False)
    label(sh, *E(-0.45, 3.45), 130, 148, "STUFFED CROCODILE", "3.40 m · 2 chains · cut → drops")
    label(sh, *E(-1.05, 2.9), 130, 198, "ARCHWAY ×4 (DOOR)", "2.60 × 3.31 m, centred")
    label(sh, *E(-0.75, 0.79), 130, 248, "CENTRAL TABLE", "2.40 × 1.10 × 0.82 · baize")
    label(sh, *E(2.5, 3.0), 350, 148, "CANDLE-CROWN (KEY LIGHT)", "1.00 m · 8 candles · at 3.00")
    label(sh, *E(2.9, 2.35), 350, 198, "WALL CASE · 12 LOOT SLOTS", "glazed doors 0.60 × 2.60")
    label(sh, *E(1.85, 0.3), 350, 248, "CUPBOARD BASE (HIDE ×4)", "1.20 × 0.45 × 0.90 m")
    label(sh, *E(3.3, 3.9), 575, 148, "CORNICE CREST", "narwhal tusk 2.20 m")
    label(sh, *E(4.25, 2.2), 575, 198, "SPECIMEN JARS", "spirit fill · burns")
    label(sh, *E(3.4, 0.95), 575, 248, "GLOBE ×2 (ROLLS)", "0.70 m · 1.25 m tall")


def plan(sh):
    s = 32
    x0, y0 = 768, 236
    P = plan_frame(sh, x0, y0, s, 0.6, [("N", 0, 2.6), ("S", 0, 2.6), ("E", 0, 2.6), ("W", 0, 2.6),
                                         ("S", -3.4, 1.4), ("S", 3.4, 1.4)])
    # windows south: leaded glazing in the wall gap
    for xc in (-3.4, 3.4):
        xa, ya = P(xc - 0.7, -5.4)
        sh.rect(xa, ya, 1.4 * s, 0.6 * s, "#1A1712", GLASS, 1.2)
        sh.line([(xa, ya + 0.3 * s), (xa + 1.4 * s, ya + 0.3 * s)], GLASS, 1.4, smooth=False)
        sh.glow(xa + 0.7 * s, ya - 10, 40, GLASS, 0.18)
        plan_label(sh, xa + 0.7 * s, y0 + 12 * s + 32, "WINDOW", "middle", size=9)
    # case runs 0.45 deep, stop 0.30 short of openings
    runs = {"N": [(-5.4, -1.6), (1.6, 5.4)], "E": [(-5.4, -1.6), (1.6, 5.4)], "W": [(-5.4, -1.6), (1.6, 5.4)],
            "S": [(-5.4, -4.4), (-2.4, -1.6), (1.6, 2.4), (4.4, 5.4)]}
    for side, segs in runs.items():
        for a, b in segs:
            if side == "N":
                xa, ya = P(a, 5.4); sh.rect(xa, ya, (b - a) * s, 0.45 * s, WAL, lit(WAL, 0.4), 0.8)
            if side == "S":
                xa, ya = P(a, -4.95); sh.rect(xa, ya, (b - a) * s, 0.45 * s, WAL, lit(WAL, 0.4), 0.8)
            if side == "E":
                xa, ya = P(4.95, b); sh.rect(xa, ya, 0.45 * s, (b - a) * s, WAL, lit(WAL, 0.4), 0.8)
            if side == "W":
                xa, ya = P(-5.4, b); sh.rect(xa, ya, 0.45 * s, (b - a) * s, WAL, lit(WAL, 0.4), 0.8)
    # glass line on case fronts
    for a, b in runs["N"]:
        sh.line([P(a, 4.95), P(b, 4.95)], GLASS, 1.2, smooth=False)
    for a, b in runs["E"]:
        sh.line([P(4.95, a), P(4.95, b)], GLASS, 1.2, smooth=False)
    for a, b in runs["W"]:
        sh.line([P(-4.95, a), P(-4.95, b)], GLASS, 1.2, smooth=False)
    # 12 loot slots on the shelves (0.55 wide)
    slots = [(-4.3, 5.18, "h"), (-2.7, 5.18, "h"), (2.7, 5.18, "h"), (4.3, 5.18, "h"),
             (5.18, 4.3, "v"), (5.18, 2.7, "v"), (5.18, -2.7, "v"), (5.18, -4.3, "v"),
             (-5.18, 4.3, "v"), (-5.18, 2.7, "v"), (-5.18, -2.7, "v"), (-5.18, -4.3, "v")]
    for x, y, o in slots:
        c = P(x, y)
        w, h = (0.55 * s, 0.3 * s) if o == "h" else (0.3 * s, 0.55 * s)
        sh.rect(c[0] - w / 2, c[1] - h / 2, w, h, "none", VELLUM, 1)
    # 4 hide cupboards (1.20 wide) marked
    for x, y, o in ((-3.5, 5.18, "h"), (3.5, 5.18, "h"), (5.18, 3.5, "v"), (-5.18, 3.5, "v")):
        c = P(x, y)
        w, h = (1.2 * s, 0.45 * s) if o == "h" else (0.45 * s, 1.2 * s)
        sh.rect(c[0] - w / 2, c[1] - h / 2, w, h, "none", FRAME_TXT, 1, op=0.9)
        sh.text(c[0], c[1] + 3, "×", 10, VELLUM, "middle")
    # ceiling beams E-W every 1.20 m (overhead, dashed); central beam stronger
    for k in range(-4, 5):
        y = k * 1.2
        sh.line([P(-5.4, y), P(5.4, y)], "#3A3226" if k else FRAME_TXT, 1 if k else 1.2, smooth=False, dash="6 4", op=0.9)
    # table, cabinet socket, instrument slots, patrol loop
    xa, ya = P(-1.2, 2.25)
    sh.path(f"M{fmt(P(-2.0, 0.85)[0])} {fmt(P(-2.0, 0.85)[1])} H{fmt(P(2.0, 0.85)[0])} V{fmt(P(2.0, 2.95)[1])} H{fmt(P(-2.0, 2.95)[0])} Z",
            "none", "#C4542E", 1, op=0.55, extra=' stroke-dasharray="2 4"')
    sh.rect(xa, ya, 2.4 * s, 1.1 * s, BAIZE, WAL, 1.5)
    c = P(0, 1.7)
    sh.rect(c[0] - 0.475 * s, c[1] - 0.275 * s, 0.95 * s, 0.55 * s, "none", VELLUM, 1)
    for sx in (-0.95, 0.95):
        c2 = P(sx, 1.7)
        sh.circle(*c2, 0.13 * s, "none", VELLUM, 0.9)
    # globes
    for gx in (-3.4, 3.4):
        g = P(gx, 2.6)
        sh.circle(*g, 0.45 * s, "none", WAL, 1.4)
        sh.circle(*g, 0.35 * s, f"url(#{sh.form(GESSO, 'sphere', 0.3)})", INK, 0.8)
    # crocodile overhead on the central beam, head west
    cp = [P(-1.7, 0), P(-1.3, 0.1), P(-0.9, 0.2), P(-0.4, 0.225), P(0.3, 0.18), P(1.0, 0.08), P(1.7, 0.0),
          P(1.0, -0.08), P(0.3, -0.18), P(-0.4, -0.225), P(-0.9, -0.2), P(-1.3, -0.1)]
    sh.path(smooth_path(cp), CROC, VELLUM, 0.8, op=0.85, extra=' stroke-dasharray="3 2"')
    for hx in (-1.0, 1.0):
        sh.circle(*P(hx, 0), 2.6, IRON, VELLUM, 0.8)
    # candle-crown
    cc = P(2.5, 0.9)
    sh.glow(*cc, 60, "#F2D9A8", 0.35)
    sh.circle(*cc, 0.5 * s, "none", IRON, 2)
    for k in range(8):
        a = k * math.pi / 4
        sh.circle(cc[0] + 0.5 * s * math.cos(a), cc[1] + 0.5 * s * math.sin(a), 1.6, "#F2D9A8")
    # section line A-A (just south of the central beam)
    ya_ = P(0, -0.3)[1]
    sh.line([(x0 - 14, ya_), (x0 + 12 * s + 14, ya_)], FRAME_TXT, 0.8, smooth=False, dash="8 3 2 3")
    for xx in (x0 - 14, x0 + 12 * s + 14):
        sh.path(f"M{fmt(xx)} {fmt(ya_)} l0 -10 l-4 5 m4 -5 l4 5", "none", FRAME_TXT, 1)
        sh.text(xx, ya_ + 12, "A", 9, FRAME_TXT, "middle")
    # labels
    plan_label(sh, P(0, 6)[0], y0 - 12, "DOOR N 2.60 × 3.31", "middle")
    plan_label(sh, P(0, -6)[0], y0 + 12 * s + 32, "DOOR S", "middle")
    plan_label(sh, x0 + 12 * s - 0.6 * s - 16, P(0, 0.35)[1] + 3, "DOOR E", "end", size=9)
    plan_label(sh, x0 + 0.6 * s + 16, P(0, 0.35)[1] + 3, "DOOR W", "start", size=9)
    plan_label(sh, P(0, 3.25)[0], P(0, 3.25)[1] + 3, "TABLE · CABINET SOCKET", "middle", FRAME_TXT, 8.5)
    plan_label(sh, P(-1.0, -0.95)[0], P(0, -0.95)[1] + 3, "CROCODILE OVERHEAD · HOOKS ±1.00", "middle", FRAME_TXT, 8.5)
    plan_label(sh, P(3.4, 1.6)[0], P(0, 1.6)[1] + 3, "GLOBE", "middle", FRAME_TXT, 8.5)
    plan_label(sh, P(3.5, 0.9)[0] + 2, P(0, 0.9)[1] + 3, "CROWN", "start", FRAME_TXT, 8.5)
    plan_label(sh, P(0, -2.0)[0], P(0, -2.0)[1] + 3, "cuirassier patrol loop", "middle", "#C4542E", 8.5)
    plan_label(sh, P(0, 4.4)[0], P(0, 4.4)[1] + 3, "□ LOOT ×12 (SHELF)  × HIDE ×4", "middle", FRAME_TXT, 8.5)
    north_arrow(sh, x0 + 12 * s - 14, y0 + 30)


def build():
    sh = Sheet(seed=33)
    section(sh)
    struct_ladder(sh, 70, FLOOR, S, 4.6, clear=4.6)
    sh.text(70, FLOOR + 20, "floor", 9, FRAME_MID, "middle")
    plan(sh)
    return sh.render("THE AGE OF POWDER · STRUCTURE · KEEP", "The Kunstkammer",
                     "H 4.60 m clear · 12 × 12 m cell · ≤ 25k tris", "section 1 m = 70 px · plan 1 m = 32 px",
                     [(WAL, "walnut"), (PLASTER, "plaster"), (GLASS, "case glass"), (CROC, "croc hide"),
                      (GESSO, "gesso"), (BAIZE, "baize"), (SPIRIT, "spirit")],
                     view_labels=[(390, "SECTION A–A · E–W, LOOKING NORTH"), (960, "PLAN")], ladder=None, human=False,
                     glow_c=("42%", "50%"))
