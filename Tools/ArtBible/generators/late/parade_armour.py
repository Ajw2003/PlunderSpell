from common_enemy import *

K = 1.90 / 1.80  # authored in 1.80 space, the helmet crown lands at 1.90 m
HS, OS = 240 * K, 150 * K
sh = Sheet("parade-armour")
sh.frame(f"{AGE_NAME} · PLUNDER · 900 COIN · 13 ST", "Parade Armour on its Stand",
         "0.72 × 0.72 × 1.90 m · ≤ 5k tris · 1024²", f"hero 1 m = {HS:.0f} px · ortho 1 m = {OS:.0f} px", glow_xy=("30%", "65%"))
item_under(sh, 0.5, 0.5 * HS, f"0.5 m = {0.5*HS:.0f} px (hero) · {0.5*OS:.0f} px (ortho)")

sh.lin("wh", [(0, "#2A2C2E"), (.18, "#6E7174"), (.34, "#C9CCCC"), (.46, "#8E9194"), (.75, "#4A4D50"), (1, "#1E2022")])
sh.lin("whV", [(0, "#B9BCBD"), (.3, "#8E9194"), (1, "#2E3032")], 0, 0, 0, 1)
sh.rad("whR", [(0, "#E0E2E2"), (.3, "#8E9194"), (1, "#26282A")], .36, .3, .8)
sh.lin("gilt", [(0, "#7A5E14"), (.35, "#E3C35A"), (.5, "#C9A227"), (1, "#6E5410")])
sh.lin("oak", [(0, "#3A2A1A"), (.5, "#6B4F33"), (1, "#4A3622")])
sh.lin("oakT", [(0, "#8A6A45"), (1, "#6B4F33")], 0, 0, 0, 1)
sh.lin("vel", [(0, "#4A1418"), (.5, "#7A2228"), (1, "#3A0E12")])
FL, FD = "#D6D8D8", "#2E3032"


def armour(v, flat=None, detail=True):
    def P(d, fill, stroke="#101112", sw=1, both=False, mirror=False, extra=""):
        if flat:
            sh.path(v, d, flat, both=both, mirror=mirror)
        else:
            sh.path(v, d, fill, stroke, sw, both=both, mirror=mirror, extra=extra)
    # post (visible under the tassets)
    P("M -0.03 0.16 L 0.03 0.16 L 0.03 1.40 L -0.03 1.40 Z", "url(#oak)", "#1E150C")
    # tassets + fauld
    P("M 0.025 0.985 L 0.195 0.995 L 0.212 0.82 L 0.12 0.735 L 0.035 0.80 Z", "url(#wh)", both=True)
    P("M -0.19 1.085 L 0.19 1.085 L 0.215 0.975 L -0.215 0.975 Z", "url(#wh)")
    # breastplate + plackart
    P("M -0.23 1.46 C -0.22 1.36 -0.20 1.22 -0.16 1.12 L -0.175 1.08 L 0.175 1.08 L 0.16 1.12 C 0.20 1.22 0.22 1.36 0.23 1.46 C 0.14 1.50 -0.14 1.50 -0.23 1.46 Z", "url(#wh)", sw=1.2)
    P("M -0.175 1.08 C -0.185 1.15 -0.175 1.22 -0.145 1.26 C -0.12 1.25 -0.1 1.28 -0.085 1.265 C -0.065 1.29 -0.045 1.28 -0.035 1.30 L 0 1.37 L 0.035 1.30 C 0.045 1.28 0.065 1.29 0.085 1.265 C 0.1 1.28 0.12 1.25 0.145 1.26 C 0.175 1.22 0.185 1.15 0.175 1.08 Z", "url(#wh)", sw=1.2)
    P("M -0.14 1.52 C -0.08 1.48 0.08 1.48 0.14 1.52 L 0.13 1.46 C 0.08 1.44 -0.08 1.44 -0.13 1.46 Z", "url(#wh)")
    # arms hanging on the stand's yoke
    for m in (False, True):
        P("M 0.235 1.34 L 0.33 1.30 L 0.332 1.14 L 0.245 1.14 Z", "url(#wh)", mirror=m)
        P("M 0.228 1.19 C 0.26 1.215 0.33 1.215 0.352 1.175 L 0.352 1.10 C 0.32 1.08 0.25 1.08 0.228 1.10 Z", "url(#whR)", mirror=m)
        P("M 0.30 1.205 C 0.34 1.21 0.36 1.16 0.35 1.12 C 0.41 1.13 0.42 1.18 0.40 1.22 C 0.37 1.24 0.33 1.23 0.30 1.205 Z", "url(#whV)", mirror=m)
        P("M 0.245 1.10 L 0.34 1.10 C 0.345 1.0 0.335 0.93 0.325 0.88 L 0.255 0.88 C 0.25 0.95 0.245 1.02 0.245 1.10 Z", "url(#wh)", mirror=m)
        P("M 0.235 0.90 L 0.348 0.90 L 0.332 0.84 C 0.332 0.80 0.317 0.76 0.292 0.75 C 0.267 0.75 0.252 0.78 0.252 0.83 Z", "url(#wh)", mirror=m)
        P("M 0.15 1.50 C 0.24 1.55 0.34 1.52 0.36 1.42 C 0.37 1.34 0.35 1.28 0.33 1.26 C 0.29 1.30 0.22 1.33 0.18 1.36 Z", "url(#whR)", sw=1.2, mirror=m)
    # bevor + sallet
    P("M -0.122 1.615 C -0.132 1.53 -0.112 1.45 -0.062 1.41 C -0.022 1.39 0.022 1.39 0.062 1.41 C 0.112 1.45 0.132 1.53 0.122 1.615 C 0.06 1.63 -0.06 1.63 -0.122 1.615 Z", "url(#wh)")
    P("M -0.157 1.58 C -0.162 1.68 -0.142 1.76 -0.092 1.79 C -0.05 1.805 0.05 1.805 0.092 1.79 C 0.142 1.76 0.162 1.68 0.157 1.58 C 0.10 1.572 -0.10 1.572 -0.157 1.58 Z", "url(#whR)", sw=1.2)
    if flat or not detail:
        return
    # flutes
    for x0 in (0.03, 0.07, 0.11, 0.15):
        sh.path(v, f"M {x0*0.4:.3f} 1.09 C {x0*0.7:.3f} 1.15 {x0:.3f} 1.20 {x0:.3f} 1.26", stroke=FD, sw=1.2, both=True, extra='opacity=".7"')
    for yy in (1.05, 1.015):
        sh.path(v, f"M -0.2 {yy} L 0.2 {yy}", stroke=FD, sw=1.1)
    for m in (False, True):
        for yy in (1.44, 1.39, 1.34):
            sh.path(v, f"M 0.19 {yy+0.03} C 0.25 {yy+0.02} 0.31 {yy} 0.35 {yy-0.04}", stroke=FD, sw=1.2, mirror=m)
    # gilt-etched bands (orpiment — this gold is the loot)
    for x0 in (-0.105, 0.0, 0.105):
        d = f"M {x0-0.018} 1.47 L {x0+0.018} 1.47 L {x0+0.015} 1.10 L {x0-0.015} 1.10 Z"
        sh.path(v, d, "url(#gilt)", "#5A440E", .6)
        for yy in [1.44 - i * 0.035 for i in range(10)]:
            sh.path(v, f"M {x0-0.01} {yy} C {x0-0.004} {yy+0.012} {x0+0.004} {yy-0.012} {x0+0.01} {yy}", stroke="#3A2C08", sw=.8)
    sh.path(v, "M -0.23 1.46 C -0.14 1.50 0.14 1.50 0.23 1.46", stroke="#C9A227", sw=3)
    sh.path(v, "M -0.175 1.08 C -0.185 1.15 -0.175 1.22 -0.145 1.26 C -0.12 1.25 -0.1 1.28 -0.085 1.265 C -0.065 1.29 -0.045 1.28 -0.035 1.30 L 0 1.37 L 0.035 1.30 C 0.045 1.28 0.065 1.29 0.085 1.265 C 0.1 1.28 0.12 1.25 0.145 1.26 C 0.175 1.22 0.185 1.15 0.175 1.08", stroke="#C9A227", sw=2.4)
    sh.path(v, "M -0.19 1.085 L 0.19 1.085", stroke="#C9A227", sw=2.4)
    sh.path(v, "M -0.215 0.975 L 0.215 0.975", stroke="#C9A227", sw=2)
    for m in (False, True):
        sh.path(v, "M 0.15 1.50 C 0.24 1.55 0.34 1.52 0.36 1.42", stroke="#C9A227", sw=2.4, mirror=m)
        sh.path(v, "M 0.212 0.82 L 0.12 0.735 L 0.035 0.80", stroke="#C9A227", sw=2, mirror=m)
        sh.path(v, "M 0.235 0.90 L 0.348 0.90", stroke="#C9A227", sw=2.4, mirror=m)
    sh.path(v, "M -0.157 1.58 C -0.10 1.572 0.10 1.572 0.157 1.58", stroke="#C9A227", sw=3)
    sh.path(v, "M -0.15 1.64 C -0.10 1.63 0.10 1.63 0.15 1.64 L 0.15 1.665 C 0.10 1.655 -0.10 1.655 -0.15 1.665 Z", "url(#gilt)", "#5A440E", .6)  # brow band
    sh.path(v, "M -0.125 1.675 C -0.06 1.668 0.06 1.668 0.125 1.675 L 0.12 1.69 C 0.06 1.683 -0.06 1.683 -0.12 1.69 Z", "#050505")
    sh.path(v, "M 0 1.80 L 0 1.70", stroke=FL, sw=1.4)
    sh.path(v, "M -0.08 1.785 C -0.12 1.76 -0.145 1.70 -0.148 1.62", stroke="#E0E2E2", sw=1.1)
    # velvet-faced strap at the shoulder, gilt buckle
    for m in (False, True):
        sh.path(v, "M 0.14 1.49 L 0.17 1.49 L 0.175 1.40 L 0.145 1.40 Z", "url(#vel)", mirror=m)
        sh.path(v, "M 0.14 1.43 L 0.178 1.43 L 0.178 1.41 L 0.14 1.41 Z", "none", "#C9A227", 1.2, mirror=m)
    # gilt rubbed off where hands lift it: bare steel patches on bands
    for (x, y) in ((0.0, 1.30), (-0.105, 1.20), (0.105, 1.38)):
        sh.add(f'<ellipse cx="{v.x(x):.1f}" cy="{v.y(y):.1f}" rx="{0.012*v.s:.1f}" ry="{0.03*v.s:.1f}" fill="#8E9194" opacity=".8"/>')
    sh.flecks(v, (-0.23, 0.74, 0.23, 1.8), 40, "#1E1B17", 5, .002, .006, .4)


# ───────────── HERO (three-quarter) ─────────────
H = View(370, 690 - 0.08 * HS, HS)
def proj(x, y, z):  # three-quarter: depth runs back-right and up
    return (H.x(x * 0.8 + z * 0.45), H.y(y + z * 0.22))

def box(x0, x1, y0, y1, z0, z1, front, top, side):
    def poly(pts, fill):
        sh.add('<path d="M ' + " L ".join(f"{a:.1f} {b:.1f}" for a, b in pts) + f' Z" fill="{fill}" stroke="#1E150C" stroke-width=".8"/>')
    poly([proj(x0, y0, z0), proj(x1, y0, z0), proj(x1, y1, z0), proj(x0, y1, z0)], front)
    poly([proj(x0, y1, z0), proj(x1, y1, z0), proj(x1, y1, z1), proj(x0, y1, z1)], top)
    poly([proj(x1, y0, z0), proj(x1, y0, z1), proj(x1, y1, z1), proj(x1, y1, z0)], side)

sh.add(f'<ellipse cx="{H.x(0.08):.1f}" cy="{H.y(0.02):.1f}" rx="150" ry="16" fill="#0B0A08" opacity=".7"/>')
box(-0.04, 0.04, 0.0, 0.08, -0.02, 0.36, "#4A3622", "#8A6A45", "#3A2A1A")   # rear beam
box(-0.36, 0.36, 0.0, 0.08, -0.04, 0.04, "url(#oak)", "#8A6A45", "#3A2A1A")  # cross beam
box(-0.04, 0.04, 0.0, 0.08, -0.36, -0.04, "url(#oak)", "#8A6A45", "#3A2A1A")  # front beam
box(-0.07, 0.07, 0.08, 0.16, -0.07, 0.07, "url(#oak)", "#8A6A45", "#3A2A1A")  # centre block
for (x, z) in ((-0.36, 0), (0.36, 0), (0, -0.36)):
    px, py = proj(x, 0.04, z)
    sh.add(f'<circle cx="{px:.1f}" cy="{py:.1f}" r="3" fill="#3F3C38"/>')
cxp = H.x(0)
dx, dy = 0.03 * 0.45 * HS, -0.03 * 0.22 * HS
sh.add(f'<g transform="translate({cxp+dx:.1f},{dy:.1f}) scale(0.8,1) translate({-cxp:.1f},0)">')
armour(H, flat="#26282A")
sh.add('</g>')
sh.add(f'<g transform="translate({cxp:.1f},0) scale(0.8,1) translate({-cxp:.1f},0)">')
armour(H)
sh.add('</g>')

# ───────────── ORTHOS ─────────────
O = View(800, 690, OS)
sh.path(O, "M -0.36 0 L 0.36 0 L 0.36 0.08 L -0.36 0.08 Z", "url(#oak)", "#1E150C", .8)
sh.path(O, "M -0.07 0.08 L 0.07 0.08 L 0.07 0.16 L -0.07 0.16 Z", "url(#oak)", "#1E150C", .8)
armour(O)
Sd = View(1030, 690, OS)
sh.path(Sd, "M -0.36 0 L 0.36 0 L 0.36 0.08 L -0.36 0.08 Z", "url(#oak)", "#1E150C", .8)
sh.path(Sd, "M -0.07 0.08 L 0.07 0.08 L 0.07 0.16 L -0.07 0.16 Z", "url(#oak)", "#1E150C", .8)
sh.path(Sd, "M -0.03 0.16 L 0.03 0.16 L 0.03 1.40 L -0.03 1.40 Z", "url(#oak)", "#1E150C", .8)
sh.path(Sd, "M -0.01 0.99 L 0.14 0.99 L 0.155 0.82 L 0.075 0.745 L -0.005 0.82 Z", "url(#wh)", "#101112", .8)
sh.path(Sd, "M -0.13 1.08 C -0.17 1.02 -0.18 0.96 -0.16 0.90 L -0.08 0.92 L -0.08 1.08 Z", "url(#wh)", "#101112", .8)
sh.path(Sd, "M -0.13 1.085 L 0.12 1.085 L 0.155 0.975 L -0.165 0.975 Z", "url(#wh)", "#101112", .8)
sh.path(Sd, "M -0.10 1.48 C -0.145 1.38 -0.145 1.22 -0.115 1.08 L 0.10 1.08 C 0.145 1.18 0.165 1.30 0.145 1.40 C 0.125 1.47 0.065 1.50 0.0 1.50 Z", "url(#wh)", "#101112", 1)
sh.path(Sd, "M 0.10 1.08 C 0.145 1.18 0.165 1.30 0.145 1.40", stroke="#C9A227", sw=2)
sh.path(Sd, "M -0.04 1.10 L 0.045 1.10 C 0.05 1.0 0.045 0.93 0.04 0.88 L -0.03 0.88 C -0.035 0.95 -0.04 1.02 -0.04 1.10 Z", "url(#wh)", "#101112", .8)
sh.path(Sd, "M -0.045 0.90 L 0.06 0.90 L 0.055 0.84 C 0.06 0.80 0.045 0.76 0.02 0.75 C -0.01 0.75 -0.03 0.78 -0.03 0.83 Z", "url(#wh)", "#101112", .8)
sh.path(Sd, "M -0.05 1.40 L 0.05 1.40 L 0.05 1.14 L -0.045 1.14 Z", "url(#wh)", "#101112", .8)
sh.path(Sd, "M -0.06 1.19 C -0.075 1.14 -0.04 1.09 0.01 1.09 C 0.06 1.10 0.07 1.16 0.05 1.20 Z", "url(#whR)", "#101112", .8)
sh.path(Sd, "M -0.14 1.50 C -0.05 1.555 0.07 1.54 0.118 1.46 C 0.135 1.40 0.115 1.33 0.095 1.30 C 0.035 1.34 -0.06 1.36 -0.125 1.36 C -0.155 1.44 -0.155 1.50 -0.14 1.50 Z", "url(#whR)", "#101112", 1)
sh.path(Sd, "M 0.142 1.615 C 0.152 1.53 0.122 1.45 0.062 1.41 C 0.022 1.40 -0.028 1.41 -0.05 1.44 L -0.042 1.60 C 0.02 1.612 0.08 1.618 0.142 1.615 Z", "url(#wh)", "#101112", .8)
sh.path(Sd, "M 0.135 1.58 C 0.145 1.68 0.115 1.76 0.045 1.795 C -0.025 1.81 -0.10 1.78 -0.14 1.71 C -0.172 1.65 -0.212 1.60 -0.275 1.565 C -0.20 1.548 -0.14 1.555 -0.10 1.58 C -0.02 1.58 0.08 1.58 0.135 1.58 Z", "url(#whR)", "#101112", 1)
sh.path(Sd, "M 0.135 1.58 C 0.08 1.58 -0.02 1.58 -0.10 1.58 C -0.14 1.555 -0.20 1.548 -0.275 1.565", stroke="#C9A227", sw=2)
sh.path(Sd, "M 0.02 1.652 L 0.14 1.652 L 0.14 1.666 L 0.025 1.666 Z", "#050505")

# grab points: the cross-beam ends and the yoke
grab(sh, *proj(-0.36, 0.06, 0), "GRAB", -12, 22, "end")
grab(sh, *proj(0.36, 0.06, 0), "GRAB", 12, 22)
grab(sh, H.x(0.0), H.y(0.45), "GRAB · post", 12, 4)
# fracture / scatter lines: the harness comes apart at its straps
crack(sh, [(H.x(-0.12*0.8)-2, H.y(1.625)), (H.x(0.12*0.8)+2, H.y(1.625))])
for m in (-1, 1):
    crack(sh, [(H.x(m*0.17*0.8), H.y(1.51)), (H.x(m*0.20*0.8), H.y(1.33))])
    crack(sh, [(H.x(m*0.24*0.8), H.y(1.12)), (H.x(m*0.35*0.8), H.y(1.12))])
crack(sh, [(H.x(-0.18*0.8), H.y(0.98)), (H.x(0.18*0.8), H.y(0.98))])
sh.text(40, 150, "- - -  STRAP LINES: DENTS, THEN SCATTERS INTO 7 PIECES ABOVE 6 m/s", 10.5, "#C4542E", "start", extra='letter-spacing="1"')

tx = 488
sh.callout(H.x(0.08), H.y(1.65), tx, 170, "SALLET, GILT BROW", "white steel, etched band")
sh.callout(H.x(0.084), H.y(1.32), tx, 240, "GILT-ETCHED BANDS", "orpiment: the loot")
sh.callout(H.x(0.14*0.8), H.y(1.45), tx, 310, "VELVET STRAPS", "crimson, gilt buckles")
sh.callout(H.x(-0.02), H.y(0.60), tx, 470, "OAK POST", "0.06 m sq, 1.40 m")
sh.callout(*proj(0.2, 0.08, 0.0), tx, 560, "CROSS FOOT", "oak, 0.72 m span")

sh.palette([("#8E9194", "white harness"), ("#C9A227", "gilt etching"), ("#6B4F33", "oak stand"),
            ("#7A2228", "velvet facing"), ("#3F3C38", "stand iron"), ("#5A4331", "leathers")])
sh.write()
