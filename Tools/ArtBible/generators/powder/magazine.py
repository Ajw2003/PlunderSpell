from lib import *
from gallery import label

BRICK = "#7A4A36"; MORTAR = "#B7AC92"; OAK = "#6A5034"; COOPER = "#7C5A38"; COPPER = "#9A5B38"; POWDER = "#1E1C1A"; GLASS = "#7E9377"
CX, FLOOR, S = 110, 663, 90
E = lambda x, z: (CX + x * S, FLOOR - z * S)
H = 4.0
SPRING, CROWN, HALF = 2.40, 3.95, 4.8


def vault_z(x):
    # segmental arc through (±4.8, 2.40) and (0, 3.95)
    rise = CROWN - SPRING
    R = (HALF ** 2 + rise ** 2) / (2 * rise)
    return CROWN - R + math.sqrt(max(R * R - x * x, 0))


def brick_field(sh, x0, z0, x1, z1, clip_d=None, header=False):
    if clip_d:
        sh.clip_open(clip_d)
    sh.rect(*E(x0, z1), (x1 - x0) * S, (z1 - z0) * S, dk(MORTAR, 0.35))
    bh, bw = 0.08, (0.12 if header else 0.24)
    rnd = sh.rnd
    row = 0
    z = z0
    while z < z1:
        off = (bw / 2) * (row % 2)
        x = x0 - off
        while x < x1:
            c = mix(BRICK, rnd.choice(["#000000", "#F2D9A8"]), rnd.uniform(0, 0.12))
            sh.rect(*E(x + 0.005, z + bh - 0.005), (bw - 0.01) * S, (bh - 0.01) * S, c)
            x += bw
        z += bh
        row += 1
    if clip_d:
        sh.close()


def barrel_end(sh, x, y, r):
    sh.circle(x, y, r, f"url(#{sh.form(COOPER, 'sphere', 0.35, 0.55)})", INK, 1)
    sh.circle(x, y, r * 0.86, "none", "#8A6E48", 1.8)
    for k in range(5):
        yy = y - r * 0.7 + k * r * 0.35
        w = math.sqrt(max(r * r * 0.74 - (yy - y) ** 2, 0))
        sh.line([(x - w, yy), (x + w, yy)], dk(COOPER, 0.4), 0.8, smooth=False, op=0.7)
    sh.text(x, y + 3, "✕", 9, dk(MORTAR, 0.1), "middle", op=0.7)


def section(sh):
    # clip region of chamber interior
    vault = [E(x / 10, vault_z(x / 10)) for x in range(0, 49)]
    chamber = [E(0, 0)] + [E(HALF, 0), E(HALF, SPRING)] + list(reversed(vault)) + [E(0, CROWN)]
    dch = poly_path(chamber)
    # outer mass (cut): walls + vault crown poché to 4.3 m total from floor
    poche(sh, *E(0, 0), 6.0 * S, 0.3 * S, "#5A3A2A")
    poche(sh, *E(HALF, 4.6), 1.2 * S, 4.6 * S, "#5A3A2A")
    sh.path(poly_path([E(0, CROWN + 0.35), E(0, 4.6), E(HALF, 4.6)] + [E(HALF, SPRING)] + [E(x / 10, vault_z(x / 10) + 0.0) for x in range(48, -1, -1)]),
            f"url(#{hatch(sh, '#5A3A2A')})", "#8A8070", 1)
    # back (north) wall inner face: brick in elevation
    brick_field(sh, 0, 0, HALF, CROWN, dch)
    sh.clip_open(dch)
    # vault soffit rings (seen in section as depth bands)
    for k in range(1, 5):
        sh.path(poly_path([E(x / 10, vault_z(x / 10) - 0.06 * k) for x in range(0, 49)], closed=False), "none", dk(BRICK, 0.3), 1, op=0.5)
    # damp tide mark, soot over lamp-box
    sh.rect(*E(0, 0.40), HALF * S, 0.40 * S, "#1E1C1A", op=0.25)
    sh.glow(*E(4.4, 3.2), 150, POWDER, 0.7)
    sh.close()
    sh.path(dch, "none", INK, 1.4)
    # impost band
    sh.rect(*E(HALF - 0.12, SPRING + 0.15), 0.12 * S, 0.15 * S, MORTAR, INK, 0.8)
    # north door (copper-nailed) half, in the kit archway 2.60 x 2.88
    door = [E(0, 0), E(1.3, 0), E(1.3, 2.88), E(0, 2.88)]
    sh.path(poly_path(door), f"url(#{sh.form(OAK, 'h', 0.3)})", INK, 1.2)
    sh.path(poly_path([E(0, 2.88), E(1.45, 2.88), E(1.45, 3.05), E(0, 3.05)]), MORTAR, INK, 0.8)  # stone lintel
    for i in range(12):
        for j in range(24):
            if (i + j) % 2 == 0:
                x = 0.05 + i * 0.105
                z = 0.08 + j * 0.115
                cx, cy = E(x, z)
                sh.circle(cx, cy, 2.0, COPPER)
                sh.circle(cx - 0.6, cy - 0.6, 0.8, lit(COPPER, 0.5))
    for z in (0.5, 1.45, 2.4):  # bronze strap hinges
        sh.rect(*E(0.6, z + 0.05), 0.7 * S, 0.1 * S, dk(COPPER, 0.1), INK, 0.8)
    sh.rect(*E(0, 1.35), 1.35 * S, 0.08 * S, lit(COPPER, 0.25), INK, 0.8)  # drop bar
    # racks (double-sided, end-on) with barrels lying on three tiers
    for rx0 in (1.8,):
        rx1 = rx0 + 1.2
        for x in (rx0, rx0 + 0.6, rx1):
            sh.rect(*E(x - 0.075, 1.9), 0.15 * S, 1.9 * S, f"url(#{sh.form(OAK, 'h', 0.3)})", INK, 0.8)
        for z in (0.10, 0.70, 1.30):
            sh.rect(*E(rx0 - 0.1, z), 1.4 * S, 0.10 * S, f"url(#{sh.form(OAK, 'v', 0.3)})", INK, 0.8)
            for bx in (rx0 + 0.3, rx0 + 0.9):
                barrel_end(sh, *E(bx, z + 0.285), 0.275 * S)
        sh.rect(*E(rx0 - 0.1, 1.95), 1.4 * S, 0.08 * S, f"url(#{sh.form(OAK, 'v', 0.3)})", INK, 0.8)
    # half-kegs by the door
    for kx in (3.5, 4.0):
        x, y = E(kx, 0.25)
        sh.path(smooth_path([(x - 16, y + 22), (x - 19, y), (x - 16, y - 22), (x + 16, y - 22), (x + 19, y), (x + 16, y + 22)]), f"url(#{sh.form(COOPER, 'h', 0.35)})", INK, 1)
        for d in (-12, 0, 12):
            sh.line([(x - 18, y + d), (x + 18, y + d)], "#8A6E48", 1.8, smooth=False)
    x, y = E(4.0, 0.5)
    sh.ellipse(x, y, 18, 4, POWDER, INK, 0.8)  # open keg
    # powder trail
    sh.path(smooth_path([E(0.9, 0.0), E(2.0, 0.015), E(3.0, 0.0), E(3.5, 0.02)], closed=False), "none", POWDER, 3, op=0.9)
    # floor boards
    sh.rect(*E(0, 0.04), HALF * S, 0.04 * S, OAK, INK, 0.6)
    # lamp-box: seen in section in the east wall at 2.20 sill (glazed niche, lamp behind)
    lx0, lz0 = HALF, 2.20
    sh.rect(*E(lx0, lz0 + 0.8), 1.2 * S, 0.8 * S, "#1A1510", INK, 1)
    for gx in (lx0 + 0.05, lx0 + 0.35):
        sh.rect(*E(gx, lz0 + 0.78), 0.04 * S, 0.76 * S, GLASS, INK, 0.6, op=0.85)
    sh.glow(*E(lx0 + 0.9, lz0 + 0.4), 90, "#F2D9A8", 0.7)
    sh.glow(*E(lx0 + 0.2, lz0 + 0.4), 170, "#C4542E", 0.18)
    x, y = E(lx0 + 0.9, lz0 + 0.4)
    sh.rect(x - 7, y - 10, 14, 20, dk(COPPER, 0.3), INK, 0.8)
    sh.ellipse(x, y - 2, 3, 6, "#F2D9A8")
    # zig-zag vents in the east wall at 0.60 and 3.20
    for z in (0.60,):
        pts = [E(HALF, z), E(HALF + 0.4, z + 0.2), E(HALF + 0.8, z), E(HALF + 1.2, z + 0.2)]
        sh.line(pts, "#0A0908", 0.10 * S, smooth=False)
        sh.line(pts, dk(COPPER, 0.4), 1, smooth=False, dash="2 2")
    # human
    human(sh, E(1.45, 0)[0], FLOOR, S)
    label(sh, *E(0.6, 2.0), 150, 150, "DOOR N · COPPER-NAILED", "2.60 × 2.88 m · 120 nails · bronze bar")
    label(sh, *E(2.1, 1.0), 150, 200, "RACK · 3 TIERS, BARRELS", "0.55 × 0.75 m · hazel hoops")
    label(sh, *E(5.3, 2.6), 430, 150, "LAMP-BOX WINDOW", "0.60 × 0.80 m · double glass")
    label(sh, *E(5.2, 0.7), 430, 200, "VENT (ZIG-ZAG)", "0.10 × 0.45 m · copper mesh")
    label(sh, *E(2.4, 3.45), 430, 250, "SEGMENTAL BRICK VAULT", "springs 2.40 · crown 3.95 m", )


def plan(sh):
    s = 32
    x0, y0 = 768, 236
    P = plan_frame(sh, x0, y0, s, 1.2, [("N", 0, 2.6), ("S", 0, 2.6)])
    # blind arches E/W: brick infill panels (recessed)
    for side in ("E", "W"):
        xa = x0 + (12 - 1.2) * s if side == "E" else x0
        sh.rect(xa + (0.1 * s if side == "W" else 0), P(0, 1.3)[1], 1.1 * s, 2.6 * s, BRICK, INK, 0.8, op=0.9)
        # Label runs vertically inside the arch panel, clear of the barrel racks.
        lx, ly = xa + (0.1 * s if side == "W" else 0) + 0.55 * s, P(0, 0)[1]
        sh.add(f'<text x="{fmt(lx)}" y="{fmt(ly)}" transform="rotate(-90 {fmt(lx)} {fmt(ly)})" text-anchor="middle" dominant-baseline="middle" '
               f'font-family="{MONO}" font-size="8.5" letter-spacing="0.5" fill="{VELLUM}" stroke="#14120E" stroke-width="3" '
               f'stroke-linejoin="round" paint-order="stroke">BLIND ARCH</text>')
    # door leaves at N
    xa, ya = P(-1.3, 6)
    sh.rect(xa, ya + 1.2 * s - 3, 2.6 * s, 3, COPPER)
    for sgn in (-1, 1):
        cx, cy = P(sgn * 1.3, 4.8)
        sh.path(f"M{fmt(cx)} {fmt(cy)} A {fmt(1.3 * s)} {fmt(1.3 * s)} 0 0 {1 if sgn < 0 else 0} {fmt(cx - sgn * 1.3 * s)} {fmt(cy - 1.3 * s)}", "none", COPPER, 0.8, op=0.6)
    # racks
    for xc in (-2.4, 2.4):
        xa, ya = P(xc - 0.6, 3.6)
        sh.rect(xa, ya, 1.2 * s, 7.2 * s, OAK, INK, 0.8, op=0.9)
        for j in range(12):
            for i in range(2):
                sh.circle(xa + (0.3 + 0.6 * i) * s, ya + (0.3 + 0.6 * j) * s, 0.27 * s, COOPER, INK, 0.5)
    # kegs by the door
    for kx in (-3.9, -3.4, 3.4, 3.9):
        sh.circle(*P(kx, 4.2), 0.2 * s, COOPER, INK, 0.5)
    # runner + powder trail
    sh.rect(*P(-0.8, 4.8), 1.6 * s, 9.6 * s, MORTAR, op=0.12)
    sh.path(smooth_path([P(0.2, 4.0), P(-0.3, 2.0), P(0.3, -0.5), P(-0.2, -2.5)], closed=False), "none", POWDER, 3)
    # lamp box on east wall
    x, y = P(4.8, 1.8 + 0.3)
    sh.rect(x, y, 1.2 * s, 0.6 * s, "#1A1510", GLASS, 1)
    sh.glow(x + 1.0 * s, y + 0.3 * s, 30, "#F2D9A8", 0.6)
    plan_label(sh, x - 6, y + 12, "LAMP-BOX", "end", VELLUM, 8.5)
    # vents
    for (vx, vy) in ((4.8, -2.5), (4.8, 3.0), (-4.8, -2.5), (-4.8, 3.0), (-2.0, -4.8), (2.0, -4.8)):
        a = P(vx, vy)
        if abs(vx) > 4:
            sgn = 1 if vx > 0 else -1
            pts = [a, (a[0] + sgn * 0.4 * s, a[1] - 5), (a[0] + sgn * 0.8 * s, a[1] + 5), (a[0] + sgn * 1.2 * s, a[1])]
        else:
            pts = [a, (a[0] - 5, a[1] + 0.4 * s), (a[0] + 5, a[1] + 0.8 * s), (a[0], a[1] + 1.2 * s)]
        sh.line(pts, "#0A0908", 3, smooth=False)
    plan_label(sh, P(4.8, -2.5)[0] - 6, P(4.8, -2.5)[1] + 3, "VENT", "end", FRAME_TXT, 8.5)
    # blast radius 18 m (reaches beyond the cell)
    cx, cy = P(0, 0)
    plan_label(sh, cx, y0 + 12 * s + 32, "DOOR S 2.60 (open, to lighting passage)", "middle")
    plan_label(sh, cx, y0 - 12, "DOOR N 2.60 · copper-nailed", "middle")
    north_arrow(sh, x0 + 12 * s - 14, y0 + 30)
    sh.rect(*P(-4.8, 4.8), 9.6 * s, 9.6 * s, "none", "#C4542E", 1.2, op=0.8)
    plan_label(sh, cx, y0 + 12 * s + 48, "- - - hazard volume 9.6 × 9.6 × 4.0 m · blast radius 18 m (1.5 cells)", "middle", "#C4542E", 8.5)


def build():
    sh = Sheet(seed=32)
    section(sh)
    struct_ladder(sh, 70, FLOOR, S, 4.0, clear=4.0)
    sh.text(70, FLOOR + 20, "floor", 9, FRAME_MID, "middle")
    plan(sh)
    return sh.render("THE AGE OF POWDER · STRUCTURE · INNERWARD", "The Powder Magazine",
                     "H 4.00 m clear · 12 × 12 m cell · ≤ 25k tris", "section 1 m = 90 px · plan 1 m = 32 px",
                     [(BRICK, "brick"), (MORTAR, "lime mortar"), (OAK, "oak"), (COOPER, "cooper's oak"), (COPPER, "copper nails"), (POWDER, "powder black")],
                     view_labels=[(380, "HALF SECTION E–W, LOOKING NORTH"), (960, "PLAN")], ladder=None, human=False, glow_c=("48%", "45%"))
