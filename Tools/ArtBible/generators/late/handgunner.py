from common_enemy import *
import math

sh = Sheet("handgunner")
sh.frame(f"{AGE_NAME} · ENEMY · RANGED", "Handgunner", "H 1.78 m · ≤ 8k tris · 2048²", "1 m = 220 px · ground y 690")
sh.ladder_enemy()
F, S = View(470, 690, 220), View(820, 690, 220)
steel_defs(sh)
sh.lin("hose", [(0, "#1E1812"), (.35, "#4A3C30"), (.6, "#3A2F26"), (1, "#16110D")])
sh.lin("boot", [(0, "#2A1E15"), (.4, "#5A4331"), (1, "#1A120C")])
sh.lin("jack", [(0, "#4A3F2E"), (.3, "#A08C6C"), (.55, "#8C7A5E"), (1, "#3A3024")])
sh.lin("oak", [(0, "#8A6A45"), (.5, "#6B4F33"), (1, "#3A2A1A")], 0, 0, 0, 1)
sh.lin("iron", [(0, "#6A655E"), (.35, "#3F3C38"), (1, "#1A1917")], 0, 0, 0, 1)
sh.lin("horn", [(0, "#2A1E15"), (.5, "#7A5E42"), (1, "#3A2A1C")])
sh.lin("skin", [(0, "#635C4C"), (.5, "#9A9078"), (1, "#4A4436")])
sh.lin("red", [(0, "#6E1C20"), (.5, "#9E2A2F"), (1, "#5E171B")])
sh.rad("matchglow", [(0, "#FFD9A0"), (.25, "#C4542E"), (1, "#C4542E", 0)])
sh.rad("smoke", [(0, "#9A9078", .45), (1, "#9A9078", 0)])


def quilt(v, x0, x1, y0, y1, step, both=False, bow=0.0):
    x = x0
    while x <= x1 + 1e-6:
        sh.path(v, f"M {x:.3f} {y0} C {x+bow:.3f} {y0+(y1-y0)*.33} {x+bow:.3f} {y0+(y1-y0)*.66} {x:.3f} {y1}",
                stroke="#3A3024", sw=1.1, both=both, extra='opacity=".75"')
        sh.path(v, f"M {x+0.006:.3f} {y0} C {x+bow+0.006:.3f} {y0+(y1-y0)*.33} {x+bow+0.006:.3f} {y0+(y1-y0)*.66} {x+0.006:.3f} {y1}",
                stroke="#B8A482", sw=.5, both=both, extra='opacity=".5"')
        x += step


def gun(v, p0, p1, scale_len=1.0, seed=1):
    """Handgonne on an oak tiller, drawn in local metres and rotated onto p0 -> p1."""
    (x0, y0), (x1, y1) = p0, p1
    ang = math.degrees(math.atan2(y1 - y0, x1 - x0))
    L = View(0, 0, 220)
    g = [f'<g transform="translate({v.x(x0):.1f},{v.y(y0):.1f}) rotate({-ang:.2f}) scale({scale_len:.3f},1)">']
    def P(d, fill, stroke=None, sw=1, extra=""):
        st = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
        g.append(f'<path d="{L.p(d)}" fill="{fill}"{st} {extra}/>')
    P("M 0 -0.024 C -0.012 -0.02 -0.012 0.02 0 0.026 L 0.80 0.022 L 0.80 -0.020 Z", "url(#oak)", "#1E150C", .8)
    P("M 0.05 0.018 L 0.70 0.016", "none", "#A8865C", .6, 'opacity=".6"')
    for bx in (0.10, 0.74, 0.775):
        P(f"M {bx} -0.026 L {bx+0.016} -0.026 L {bx+0.016} 0.028 L {bx} 0.028 Z", "url(#iron)", "#0E0D0C", .6)
    # barrel: breech, reinforces, muzzle flare
    P("M 0.79 -0.046 L 0.90 -0.044 L 1.08 -0.034 L 1.08 0.034 L 0.90 0.044 L 0.79 0.046 Z", "url(#iron)", "#0E0D0C", 1)
    for rx in (0.84, 0.93, 1.01):
        P(f"M {rx} -0.046 L {rx+0.014} -0.046 L {rx+0.014} 0.046 L {rx} 0.046 Z", "#2A2826", "#0E0D0C", .6)
    P("M 1.075 -0.046 L 1.11 -0.048 L 1.11 0.048 L 1.075 0.046 Z", "url(#iron)", "#0E0D0C", .8)
    P("M 1.09 -0.02 L 1.11 -0.02 L 1.11 0.02 L 1.09 0.02 Z", "#050505")
    # touch-hole pan on top at the breech
    P("M 0.80 0.046 L 0.83 0.046 L 0.825 0.062 L 0.805 0.062 Z", "#2A2826", "#0E0D0C", .6)
    # hook lug under the barrel (for bracing on a merlon)
    P("M 0.92 -0.044 L 0.98 -0.044 L 0.96 -0.11 L 0.935 -0.11 L 0.945 -0.06 Z", "url(#iron)", "#0E0D0C", .8)
    rnd = random.Random(seed)
    for _ in range(18):  # soot at the muzzle and touch-hole
        cx = rnd.choice([rnd.uniform(1.0, 1.1), rnd.uniform(0.79, 0.86)])
        cy = rnd.uniform(-0.04, 0.05)
        g.append(f'<circle cx="{L.x(cx):.1f}" cy="{L.y(cy):.1f}" r="{rnd.uniform(.6, 2):.1f}" fill="#0A0908" opacity=".7"/>')
    g.append(f'<path d="{L.p("M 0.79 0.030 L 1.08 0.024")}" stroke="#8A857C" stroke-width=".8" fill="none" opacity=".7"/>')
    g.append("</g>")
    sh.add("".join(g))


def smoke(v, x, y, seed):
    rnd = random.Random(seed)
    for i in range(7):
        r = 0.02 + i * 0.012
        sh.add(f'<circle cx="{v.x(x + rnd.uniform(-0.02, 0.03) + i*0.012):.1f}" cy="{v.y(y + 0.03 + i*0.045):.1f}" r="{r*220:.1f}" fill="url(#smoke)"/>')


# ───────────── FRONT ─────────────
figure_shadow(sh, 470, 100)
# legs + shoes + garters
sh.path(F, "M 0.02 0.87 C 0.10 0.88 0.18 0.86 0.19 0.79 C 0.20 0.65 0.172 0.57 0.165 0.49 C 0.175 0.40 0.17 0.30 0.152 0.20 C 0.146 0.15 0.142 0.12 0.142 0.10 L 0.066 0.10 C 0.06 0.20 0.05 0.35 0.05 0.45 C 0.045 0.55 0.03 0.70 0.02 0.87 Z", "url(#hose)", both=True)
sh.path(F, "M 0.048 0.47 L 0.172 0.47 L 0.172 0.445 L 0.049 0.445 Z", "#5A4331", both=True)
sh.path(F, "M 0.058 0.13 L 0.148 0.13 C 0.162 0.08 0.178 0.03 0.172 0.0 L 0.036 0.0 C 0.036 0.04 0.048 0.08 0.058 0.13 Z", "url(#boot)", "#120C08", .8, both=True)
sh.flecks(F, (-0.17, 0.0, 0.17, 0.06), 18, "#6B6250", 3, .002, .005, .5)
# jack skirt + torso
jack = "M -0.24 1.47 C -0.232 1.35 -0.215 1.20 -0.19 1.08 C -0.20 1.0 -0.218 0.9 -0.235 0.80 C -0.1 0.775 0.1 0.775 0.235 0.80 C 0.218 0.9 0.20 1.0 0.19 1.08 C 0.215 1.20 0.232 1.35 0.24 1.47 C 0.16 1.51 -0.16 1.51 -0.24 1.47 Z"
sh.path(F, jack, "url(#jack)", "#2A2218", 1)
sh.add(f'<clipPath id="jackclip"><path d="{F.p(jack)}"/></clipPath>')
sh.add('<g clip-path="url(#jackclip)">')
quilt(F, -0.22, 0.22, 0.78, 1.50, 0.036)
sh.add('</g>')
sh.path(F, "M -0.235 0.80 C -0.1 0.775 0.1 0.775 0.235 0.80 L 0.232 0.83 C 0.1 0.81 -0.1 0.81 -0.232 0.83 Z", "#3A3024", extra='opacity=".6"')
# front lacing
for i, yy in enumerate([1.42, 1.36, 1.30, 1.24, 1.18, 1.12]):
    sh.path(F, f"M -0.012 {yy} L 0.012 {yy-0.03}", stroke="#2A1E15", sw=1.2)
    sh.path(F, f"M 0.012 {yy} L -0.012 {yy-0.03}", stroke="#2A1E15", sw=1.2)
sh.path(F, "M -0.20 1.03 L 0.20 1.03 L 0.198 1.065 L -0.198 1.065 Z", "#3A2A1C", "#140E09", .8)
sh.path(F, "M 0.10 1.028 L 0.14 1.028 L 0.14 1.067 L 0.10 1.067 Z", "none", "#6D6E6C", 1.4)
# shot pouch at left hip
sh.path(F, "M 0.13 1.03 L 0.21 1.03 C 0.22 0.97 0.215 0.92 0.19 0.90 L 0.14 0.90 C 0.125 0.93 0.122 0.98 0.13 1.03 Z", "#5A4331", "#1A120C", .8)
sh.path(F, "M 0.128 1.03 L 0.212 1.03 L 0.205 0.985 C 0.18 0.975 0.15 0.975 0.132 0.985 Z", "#4A3626", "#1A120C", .6)
# collar / padded coif
sh.path(F, "M -0.10 1.57 C -0.122 1.51 -0.132 1.475 -0.145 1.45 L 0.145 1.45 C 0.132 1.475 0.122 1.51 0.10 1.57 C 0.05 1.535 -0.05 1.535 -0.10 1.57 Z", "url(#jack)", "#2A2218", .8)
for yy in (1.48, 1.51, 1.54):
    sh.path(F, f"M -0.12 {yy} C -0.05 {yy-0.01} 0.05 {yy-0.01} 0.12 {yy}", stroke="#3A3024", sw=.9)
# arms (quilted sleeves)
for m in (False, True):
    sh.path(F, "M 0.215 1.47 C 0.30 1.47 0.335 1.41 0.34 1.31 C 0.345 1.21 0.335 1.15 0.33 1.12 L 0.235 1.12 C 0.228 1.25 0.222 1.35 0.215 1.47 Z", "url(#jack)", "#2A2218", 1, mirror=m)
    for yy in (1.40, 1.35, 1.30, 1.25, 1.20, 1.15):
        sh.path(F, f"M 0.228 {yy} C 0.26 {yy-0.012} 0.30 {yy-0.012} 0.338 {yy}", stroke="#3A3024", sw=.9, mirror=m)
# livery badge on left upper arm: white patch, red ragged saltire
for m in (False, True):
    sh.path(F, "M 0.25 1.40 L 0.325 1.40 L 0.325 1.30 L 0.2875 1.28 L 0.25 1.30 Z", "#D6CDB6", "#5E574B", .6, mirror=m)
    sh.add(f'<path d="{F.p(ragged_bar(F, (0.255, 1.395), (0.32, 1.305), 0.014, 3, 3), m)}" fill="url(#red)"/>'
           f'<path d="{F.p(ragged_bar(F, (0.32, 1.395), (0.255, 1.305), 0.014, 5, 3), m)}" fill="url(#red)"/>')
# right forearm down to the tiller end
sh.path(F, "M -0.335 1.14 C -0.31 1.07 -0.265 1.0 -0.225 0.955 L -0.160 0.995 C -0.19 1.045 -0.225 1.095 -0.245 1.14 Z", "url(#jack)", "#2A2218", 1)
# the gun, diagonal at the port
gun(F, (-0.30, 0.86), (0.40, 1.72), 1.0, 4)
# left forearm folded up to the stock
sh.path(F, "M 0.335 1.16 C 0.29 1.225 0.19 1.305 0.105 1.365 L 0.072 1.30 C 0.15 1.24 0.22 1.17 0.255 1.10 Z", "url(#jack)", "#2A2218", 1)
for t in (0.3, 0.55, 0.8):
    x = 0.335 + (0.105 - 0.335) * t
    y = 1.16 + (1.365 - 1.16) * t
    sh.path(F, f"M {x-0.03:.3f} {y-0.05:.3f} L {x+0.01:.3f} {y+0.01:.3f}", stroke="#3A3024", sw=.9)
# hands (leather mitts)
sh.path(F, "M 0.10 1.37 C 0.07 1.38 0.045 1.36 0.045 1.33 C 0.047 1.30 0.07 1.28 0.095 1.29 Z", "#5A4331", "#1A120C", .8)
sh.path(F, "M -0.232 0.96 C -0.235 0.93 -0.215 0.905 -0.19 0.91 C -0.165 0.915 -0.152 0.94 -0.158 0.97 C -0.18 0.985 -0.21 0.98 -0.232 0.96 Z", "#5A4331", "#1A120C", .8)
# bandolier: left shoulder to right hip
sh.path(F, "M 0.14 1.49 L 0.19 1.47 L -0.16 0.95 L -0.21 0.97 Z", "#5A4331", "#1A120C", .8)
sh.rivets(F, [(0.10, 1.40), (0.02, 1.28), (-0.06, 1.16), (-0.14, 1.04)], r=0.005)
# coiled spare match on the strap
for i in range(4):
    sh.add(f'<ellipse cx="{F.x(0.02 - i*0.006):.1f}" cy="{F.y(1.25 - i*0.012):.1f}" rx="{0.05*220:.1f}" ry="{0.022*220:.1f}" fill="none" stroke="#6B4F33" stroke-width="2.2"/>')
    sh.add(f'<ellipse cx="{F.x(0.02 - i*0.006):.1f}" cy="{F.y(1.25 - i*0.012):.1f}" rx="{0.05*220:.1f}" ry="{0.022*220:.1f}" fill="none" stroke="#A8865C" stroke-width=".6"/>')
# powder flask (horn) at right hip
sh.path(F, "M -0.20 0.99 C -0.26 0.95 -0.29 0.86 -0.27 0.80 C -0.25 0.78 -0.22 0.79 -0.215 0.82 C -0.22 0.88 -0.20 0.93 -0.17 0.965 Z", "url(#horn)", "#140E09", .8)
sh.path(F, "M -0.215 0.82 L -0.27 0.80 L -0.272 0.785 L -0.212 0.803 Z", "#2E2F31")
sh.path(F, "M -0.20 0.99 L -0.17 0.965 L -0.16 0.985 L -0.19 1.01 Z", "#2E2F31")
# burning match looped over the left wrist
sh.path(F, "M 0.09 1.30 C 0.12 1.20 0.22 1.10 0.24 1.02 C 0.25 0.99 0.26 0.98 0.265 0.975", stroke="#6B4F33", sw=2)
sh.add(f'<circle cx="{F.x(0.266):.1f}" cy="{F.y(0.975):.1f}" r="9" fill="url(#matchglow)"/>')
sh.add(f'<circle cx="{F.x(0.266):.1f}" cy="{F.y(0.975):.1f}" r="1.8" fill="#FFD9A0"/>')
smoke(F, 0.27, 0.98, 2)
# face under the kettle hat
sh.path(F, "M -0.076 1.63 C -0.082 1.57 -0.062 1.515 0 1.505 C 0.062 1.515 0.082 1.57 0.076 1.63 Z", "url(#skin)", "#2A241C", .8)
sh.path(F, "M -0.076 1.63 L 0.076 1.63 L 0.074 1.60 C 0.04 1.59 -0.04 1.59 -0.074 1.60 Z", "#14120E", extra='opacity=".7"')
sh.path(F, "M -0.045 1.585 L -0.02 1.585", stroke="#14120E", sw=2)
sh.path(F, "M 0.02 1.585 L 0.045 1.585", stroke="#14120E", sw=2)
sh.path(F, "M -0.035 1.545 C -0.015 1.555 0.015 1.555 0.035 1.545", stroke="#3A3024", sw=2.2)
# kettle hat
sh.path(F, "M -0.122 1.64 C -0.132 1.72 -0.082 1.775 0 1.78 C 0.082 1.775 0.132 1.72 0.122 1.64 Z", "url(#stR)", "#0A0B0C", 1.2)
sh.path(F, "M -0.218 1.622 C -0.17 1.662 -0.13 1.668 -0.11 1.668 L 0.11 1.668 C 0.13 1.668 0.17 1.662 0.218 1.622 C 0.20 1.608 0.15 1.614 0.12 1.625 L -0.12 1.625 C -0.15 1.614 -0.20 1.608 -0.218 1.622 Z", "url(#stH)", "#0A0B0C", 1)
sh.path(F, "M -0.20 1.628 C -0.16 1.655 -0.13 1.66 -0.11 1.66", stroke="#7C7D7C", sw=.9)
sh.path(F, "M 0 1.78 L 0 1.67", stroke="#5E5F60", sw=1.2)
sh.path(F, "M -0.07 1.765 C -0.11 1.74 -0.12 1.70 -0.118 1.66", stroke="#8C8D8C", sw=1, extra='opacity=".8"')
sh.rivets(F, [(-0.10, 1.645), (-0.035, 1.642), (0.035, 1.642), (0.10, 1.645)], r=0.005)
sh.flecks(F, (-0.2, 1.62, 0.2, 1.77), 18, "#0A0908", 5, .002, .006, .6)
sh.flecks(F, (-0.24, 0.8, 0.24, 1.5), 45, "#2A2218", 9, .003, .008, .45)

# ───────────── SIDE ─────────────
figure_shadow(sh, 820, 110)
sh.path(S, "M -0.13 0.87 C -0.15 0.75 -0.12 0.60 -0.10 0.50 C -0.12 0.40 -0.12 0.28 -0.09 0.12 L 0.00 0.12 C 0.01 0.25 0.03 0.35 0.02 0.45 C 0.04 0.55 0.07 0.70 0.07 0.87 Z", "#241D16")
sh.path(S, "M -0.09 0.13 L 0.005 0.13 C 0.02 0.08 0.09 0.05 0.16 0.02 L 0.16 0.0 L -0.10 0.0 C -0.11 0.05 -0.10 0.09 -0.09 0.13 Z", "#22180F")
sh.path(S, "M -0.10 0.88 C -0.12 0.75 -0.09 0.60 -0.07 0.50 C -0.09 0.40 -0.09 0.28 -0.06 0.12 L 0.03 0.12 C 0.04 0.25 0.06 0.35 0.05 0.45 C 0.07 0.55 0.10 0.70 0.10 0.88 Z", "url(#hose)", "#0F0C09", .8)
sh.path(S, "M -0.085 0.47 L 0.058 0.47 L 0.056 0.445 L -0.083 0.445 Z", "#5A4331")
sh.path(S, "M -0.062 0.13 L 0.035 0.13 C 0.05 0.08 0.12 0.05 0.19 0.02 L 0.19 0.0 L -0.072 0.0 C -0.082 0.05 -0.072 0.09 -0.062 0.13 Z", "url(#boot)", "#120C08", .8)
sjack = "M -0.125 1.47 C -0.155 1.35 -0.155 1.20 -0.13 1.08 C -0.155 1.0 -0.175 0.9 -0.185 0.80 L 0.165 0.80 C 0.155 0.9 0.14 1.0 0.125 1.08 C 0.165 1.2 0.175 1.33 0.145 1.43 C 0.115 1.49 0.065 1.52 0.02 1.53 Z"
sh.path(S, sjack, "url(#jack)", "#2A2218", 1)
sh.add(f'<clipPath id="sjackclip"><path d="{S.p(sjack)}"/></clipPath><g clip-path="url(#sjackclip)">')
quilt(S, -0.16, 0.16, 0.78, 1.52, 0.036, bow=0.015)
sh.add('</g>')
sh.path(S, "M -0.16 1.03 L 0.13 1.03 L 0.13 1.065 L -0.16 1.065 Z", "#3A2A1C", "#140E09", .8)
# shot pouch (seen on the near hip) and flask behind
sh.path(S, "M -0.04 1.03 L 0.05 1.03 C 0.06 0.97 0.055 0.92 0.03 0.90 L -0.02 0.90 C -0.04 0.93 -0.045 0.98 -0.04 1.03 Z", "#5A4331", "#1A120C", .8)
sh.path(S, "M -0.042 1.03 L 0.052 1.03 L 0.045 0.985 C 0.02 0.975 -0.01 0.975 -0.035 0.985 Z", "#4A3626", "#1A120C", .6)
sh.path(S, "M -0.13 1.00 C -0.20 0.96 -0.23 0.87 -0.21 0.81 C -0.19 0.79 -0.16 0.80 -0.155 0.83 C -0.16 0.89 -0.14 0.94 -0.11 0.975 Z", "url(#horn)", "#140E09", .8)
# bandolier over the shoulder
sh.path(S, "M -0.04 1.53 L 0.01 1.53 L -0.08 1.00 L -0.12 1.00 Z", "#5A4331", "#1A120C", .8)
# collar, face, hat
sh.path(S, "M -0.08 1.58 L 0.07 1.56 L 0.11 1.46 L -0.12 1.46 Z", "url(#jack)", "#2A2218", .8)
for yy in (1.49, 1.52, 1.55):
    sh.path(S, f"M -0.10 {yy} L 0.09 {yy-0.01}", stroke="#3A3024", sw=.9)
sh.path(S, "M 0.07 1.635 L 0.098 1.60 L 0.086 1.585 C 0.092 1.57 0.086 1.55 0.07 1.535 C 0.05 1.52 0.02 1.515 0.0 1.525 L -0.05 1.56 L -0.08 1.635 Z", "url(#skin)", "#2A241C", .8)
sh.path(S, "M 0.07 1.63 L -0.08 1.63 L -0.08 1.60 L 0.075 1.60 Z", "#14120E", extra='opacity=".6"')
sh.path(S, "M 0.05 1.59 L 0.065 1.59", stroke="#14120E", sw=2)
sh.path(S, "M 0.06 1.555 C 0.075 1.55 0.085 1.548 0.088 1.55", stroke="#3A3024", sw=2)
sh.path(S, "M -0.112 1.64 C -0.122 1.72 -0.072 1.775 0.01 1.78 C 0.09 1.775 0.132 1.72 0.122 1.64 Z", "url(#stR)", "#0A0B0C", 1.2)
sh.path(S, "M -0.205 1.60 C -0.17 1.64 -0.13 1.655 -0.10 1.66 L 0.11 1.66 C 0.14 1.655 0.18 1.64 0.215 1.60 L 0.205 1.595 C 0.17 1.625 0.14 1.635 0.11 1.64 L -0.10 1.64 C -0.13 1.635 -0.17 1.625 -0.195 1.595 Z", "url(#stH)", "#0A0B0C", 1)
sh.path(S, "M -0.06 1.765 C -0.10 1.74 -0.115 1.70 -0.11 1.66", stroke="#8C8D8C", sw=1, extra='opacity=".8"')
sh.rivets(S, [(-0.09, 1.65), (0.0, 1.65), (0.09, 1.65)], r=0.005)
# near arm: upper arm, forearm up to the stock
sh.path(S, "M -0.065 1.46 C -0.085 1.35 -0.075 1.22 -0.055 1.12 L 0.045 1.115 C 0.065 1.25 0.065 1.38 0.055 1.46 Z", "url(#jack)", "#2A2218", 1)
for yy in (1.40, 1.34, 1.28, 1.22, 1.16):
    sh.path(S, f"M -0.075 {yy} C -0.03 {yy-0.01} 0.02 {yy-0.01} 0.058 {yy}", stroke="#3A3024", sw=.9)
sh.path(S, "M -0.05 1.10 C 0.03 1.12 0.10 1.20 0.165 1.26 L 0.135 1.33 C 0.07 1.27 0.01 1.21 -0.055 1.17 Z", "url(#jack)", "#2A2218", 1)
gun(S, (-0.10, 0.90), (0.36, 1.60), 0.75, 7)
sh.path(S, "M 0.13 1.33 C 0.12 1.30 0.13 1.26 0.16 1.255 C 0.19 1.255 0.20 1.28 0.195 1.31 C 0.18 1.335 0.15 1.34 0.13 1.33 Z", "#5A4331", "#1A120C", .8)
sh.path(S, "M 0.14 1.30 C 0.16 1.20 0.20 1.08 0.215 1.00", stroke="#6B4F33", sw=2)
sh.add(f'<circle cx="{S.x(0.217):.1f}" cy="{S.y(0.995):.1f}" r="9" fill="url(#matchglow)"/>')
sh.add(f'<circle cx="{S.x(0.217):.1f}" cy="{S.y(0.995):.1f}" r="1.8" fill="#FFD9A0"/>')
smoke(S, 0.22, 1.0, 5)
sh.flecks(S, (-0.16, 0.8, 0.15, 1.5), 36, "#2A2218", 19, .003, .008, .45)

cx = 352
sh.callout(F.x(-0.05), F.y(1.75), cx, 160, "KETTLE HAT", "steel, 0.43 m brim", "end")
sh.callout(F.x(0.34), F.y(1.66), cx, 210, "HANDGONNE", "wrought iron, 0.32 m", "end")
sh.callout(F.x(-0.23), F.y(1.46), cx, 380, "PADDED JACK", "linen, 28 quilt rows", "end")
sh.callout(F.x(-0.10), F.y(1.10), cx, 430, "OAK TILLER", "0.80 m, iron bands", "end")
sh.callout(F.x(-0.24), F.y(0.86), cx, 480, "POWDER FLASK", "cow-horn, iron caps", "end")
rx = 978
sh.callout(S.x(0.27), S.y(1.52), rx, 175, "TOUCH-HOLE", "sooted breech pan")
sh.callout(S.x(0.33), S.y(1.44), rx, 240, "HOOK LUG", "braces on a merlon")
sh.callout(S.x(0.217), S.y(0.995), rx, 330, "SLOW MATCH", "lit, glows madder")
sh.callout(S.x(-0.05), S.y(1.30), rx, 410, "BANDOLIER", "buff leather strap")
sh.callout(S.x(0.02), S.y(0.95), rx, 480, "SHOT POUCH", "lead balls, 18 mm")
sh.callout(F.x(-0.30), F.y(1.30), cx, 260, "LIVERY BADGE", "each sleeve, saltire", "end")

sh.palette([("#8C7A5E", "padded jack"), ("#2E2F31", "kettle steel"), ("#3F3C38", "gun iron"),
            ("#6B4F33", "oak tiller"), ("#5A4331", "leather"), ("#9E2A2F", "livery red"), ("#C4542E", "match")])
sh.write()
