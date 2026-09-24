from common_enemy import *

sh = Sheet("pavisier")
sh.frame(f"{AGE_NAME} · ENEMY · SPECIAL", "Pavisier", "H 1.80 m · pavise 1.30 m · ≤ 8k tris · 2048²", "1 m = 220 px · ground y 690")
sh.ladder_enemy()
Fm, Fp = View(470 - 0.22 * 220, 690, 220), View(470 + 0.30 * 220, 690, 220)
Sm = View(820 - 0.25 * 220, 690, 220)
steel_defs(sh)
mail_pattern(sh)
sh.lin("hose", [(0, "#1E1812"), (.35, "#4A3C30"), (.6, "#3A2F26"), (1, "#16110D")])
sh.lin("boot", [(0, "#2A1E15"), (.4, "#5A4331"), (1, "#1A120C")])
sh.lin("red", [(0, "#5E171B"), (.4, "#9E2A2F"), (.7, "#8A2328"), (1, "#4E1216")])
sh.lin("wht", [(0, "#8F887A"), (.3, "#D6CDB6"), (.6, "#E4DCC6"), (1, "#9F9784")])
sh.lin("gesso", [(0, "#B9B09B"), (.5, "#E0D8C2"), (1, "#A8A08C")])
sh.lin("ridge", [(0, "#8F887A"), (.3, "#EAE2CC"), (.6, "#C9C0AA"), (1, "#6E6858")])
sh.lin("pop", [(0, "#6E5E44"), (.5, "#B09A74"), (1, "#7A6848")])
sh.lin("iron", [(0, "#6A655E"), (.35, "#3F3C38"), (1, "#1A1917")], 0, 0, 0, 1)
sh.lin("skin", [(0, "#635C4C"), (.5, "#9A9078"), (1, "#4A4436")])

PAV = "M -0.28 0.05 L -0.31 1.24 C -0.20 1.31 0.20 1.31 0.31 1.24 L 0.28 0.05 Z"

# ───────────── FRONT: man ─────────────
figure_shadow(sh, 470, 140)
V = Fm
sh.path(V, "M 0.02 0.88 C 0.10 0.89 0.18 0.87 0.19 0.80 C 0.20 0.66 0.172 0.58 0.166 0.50 C 0.176 0.40 0.17 0.30 0.152 0.20 C 0.146 0.15 0.142 0.12 0.142 0.10 L 0.066 0.10 C 0.06 0.20 0.05 0.35 0.05 0.45 C 0.045 0.55 0.03 0.70 0.02 0.88 Z", "url(#hose)", both=True)
sh.path(V, "M 0.058 0.13 L 0.148 0.13 C 0.162 0.08 0.178 0.03 0.172 0.0 L 0.036 0.0 C 0.036 0.04 0.048 0.08 0.058 0.13 Z", "url(#boot)", "#120C08", .8, both=True)
sh.path(V, "M 0.045 0.34 L 0.172 0.34 L 0.17 0.13 L 0.055 0.13 Z", "url(#boot)", "#120C08", .8, both=True)  # short boots
sh.path(V, "M 0.042 0.345 L 0.175 0.345 L 0.176 0.315 L 0.043 0.315 Z", "#6A5038", both=True)
# mail shirt hem showing below the coat
sh.path(V, "M -0.20 0.76 L 0.20 0.76 L 0.21 0.70 C 0.1 0.69 -0.1 0.69 -0.21 0.70 Z", "url(#mail)", "#1A1B1A", .8)
# parti-coloured livery coat: wearer's right white, left red
coat_r = "M 0 1.53 C -0.08 1.53 -0.16 1.52 -0.23 1.49 C -0.21 1.38 -0.20 1.28 -0.185 1.20 C -0.17 1.14 -0.165 1.10 -0.165 1.06 C -0.19 0.97 -0.215 0.86 -0.235 0.74 C -0.15 0.72 -0.07 0.715 0 0.715 Z"
sh.path(V, coat_r, "url(#wht)", "#5E574B", .8)
sh.path(V, coat_r, "url(#red)", "#3A0E10", .8, mirror=True)
for x0 in (-0.20, -0.14, -0.08, 0.06, 0.12, 0.18):  # skirt pleats
    c = "#6E6656" if x0 < 0 else "#4E1216"
    sh.path(V, f"M {x0*0.8:.3f} 1.04 C {x0*0.9:.3f} 0.95 {x0:.3f} 0.85 {x0*1.08:.3f} 0.73", stroke=c, sw=1.3, extra='opacity=".8"')
sh.path(V, "M -0.17 1.03 L 0.17 1.03 L 0.168 1.065 L -0.168 1.065 Z", "#3A2A1C", "#140E09", .8)
sh.path(V, "M -0.02 1.028 L 0.02 1.028 L 0.02 1.067 L -0.02 1.067 Z", "none", "#6D6E6C", 1.4)
# falchion at the right hip
sh.path(V, "M -0.15 1.02 L -0.13 1.02 C -0.14 0.90 -0.17 0.78 -0.19 0.70 C -0.23 0.72 -0.24 0.76 -0.235 0.80 C -0.2 0.86 -0.17 0.94 -0.15 1.02 Z", "#3A2A1C", "#140E09", .8)
sh.path(V, "M -0.18 1.04 L -0.10 1.04 L -0.10 1.06 L -0.18 1.06 Z", "#3F3C38")
sh.path(V, "M -0.148 1.06 L -0.132 1.06 L -0.132 1.14 L -0.148 1.14 Z", "#5A4331")
# sleeves (counterchanged) and gloves
sh.path(V, "M -0.215 1.47 C -0.30 1.47 -0.335 1.41 -0.34 1.31 C -0.345 1.2 -0.335 1.0 -0.32 0.90 L -0.245 0.90 C -0.235 1.05 -0.225 1.25 -0.215 1.47 Z", "url(#red)", "#3A0E10", .8)
sh.path(V, "M -0.325 0.915 L -0.24 0.915 C -0.235 0.86 -0.245 0.80 -0.265 0.785 C -0.29 0.775 -0.31 0.80 -0.318 0.84 Z", "#5A4331", "#1A120C", .8)
# left sleeve reaching to the pavise edge
# mail standard (collar) + open sallet + face
sh.path(V, "M -0.12 1.56 C -0.14 1.52 -0.16 1.50 -0.18 1.48 C -0.06 1.45 0.06 1.45 0.18 1.48 C 0.16 1.50 0.14 1.52 0.12 1.56 Z", "url(#mail)", "#1A1B1A", .8)
sh.path(V, "M -0.074 1.64 C -0.08 1.58 -0.06 1.53 0 1.52 C 0.06 1.53 0.08 1.58 0.074 1.64 Z", "url(#skin)", "#2A241C", .8)
sh.path(V, "M -0.045 1.60 L -0.02 1.60", stroke="#14120E", sw=2)
sh.path(V, "M 0.02 1.60 L 0.045 1.60", stroke="#14120E", sw=2)
sh.path(V, "M -0.03 1.555 C -0.01 1.55 0.01 1.55 0.03 1.555", stroke="#3A3024", sw=1.4)
sh.path(V, "M 0 1.59 L -0.006 1.57 L 0.006 1.57", stroke="#4A4436", sw=1)
sh.path(V, "M -0.145 1.60 C -0.155 1.70 -0.132 1.78 -0.085 1.795 C -0.045 1.805 0.045 1.805 0.085 1.795 C 0.132 1.78 0.155 1.70 0.145 1.60 L 0.09 1.63 C 0.06 1.645 -0.06 1.645 -0.09 1.63 Z", "url(#stR)", "#0A0B0C", 1.2)
sh.path(V, "M -0.07 1.785 C -0.12 1.76 -0.14 1.70 -0.14 1.63", stroke="#8C8D8C", sw=1, extra='opacity=".8"')
sh.rivets(V, [(-0.12, 1.615), (-0.05, 1.64), (0.05, 1.64), (0.12, 1.615)], r=0.005)
sh.flecks(V, (-0.23, 0.72, 0.23, 1.5), 40, "#2A2218", 9, .003, .008, .4)

# ───────────── FRONT: pavise ─────────────
V = Fp
sh.path(V, "M -0.28 0.06 L -0.26 0.0 L -0.24 0.06 Z", "#3F3C38")
sh.path(V, "M 0.24 0.06 L 0.26 0.0 L 0.28 0.06 Z", "#3F3C38")
sh.path(V, PAV, "url(#gesso)", "#2A241C", 1.4)
sh.add(f'<clipPath id="pavclip"><path d="{V.p(PAV)}"/></clipPath><g clip-path="url(#pavclip)">')
sh.add(f'<path d="{V.p(ragged_bar(V, (-0.36, 1.34), (0.36, 0.0), 0.11, 12, 6))}" fill="url(#red)"/>')
sh.add(f'<path d="{V.p(ragged_bar(V, (0.36, 1.34), (-0.36, 0.0), 0.11, 14, 6))}" fill="url(#red)"/>')
sh.path(V, "M -0.07 0.0 L -0.075 1.32 L 0.075 1.32 L 0.07 0.0 Z", "url(#ridge)", extra='opacity=".55"')
sh.path(V, "M -0.07 0.0 L -0.075 1.32", stroke="#6E6858", sw=1.2)
sh.path(V, "M 0.07 0.0 L 0.075 1.32", stroke="#6E6858", sw=1.2)
# flaked paint showing poplar
rnd = random.Random(8)
for _ in range(16):
    x, y = rnd.uniform(-0.28, 0.28), rnd.uniform(0.08, 1.22)
    w, h = rnd.uniform(0.01, 0.04), rnd.uniform(0.006, 0.02)
    sh.path(V, f"M {x} {y} L {x+w} {y+h*0.3} L {x+w*0.8} {y-h} L {x+w*0.2} {y-h*0.8} Z", "url(#pop)", extra='opacity=".85"')
sh.add('</g>')
# Burgundian briquet (fire-steel) device on the ridge, painted black, red sparks
sh.path(V, "M -0.07 0.80 C -0.07 0.86 -0.03 0.88 0 0.86 C 0.03 0.88 0.07 0.86 0.07 0.80 L 0.055 0.80 C 0.055 0.845 0.03 0.86 0 0.835 C -0.03 0.86 -0.055 0.845 -0.055 0.80 Z", "#1A1917")
sh.path(V, "M -0.075 0.80 L 0.075 0.80 L 0.075 0.785 L -0.075 0.785 Z", "#1A1917")
sh.path(V, "M -0.02 0.78 L 0.02 0.78 L 0.03 0.72 L 0 0.70 L -0.03 0.72 Z", "#6E6858", "#1A1917", .8)  # flint
for ang in (-60, -30, 0, 30, 60, 150, 180, 210):
    a = math.radians(ang + 90)
    x0, y0 = 0.095 * math.cos(a), 0.80 + 0.095 * math.sin(a)
    x1, y1 = 0.13 * math.cos(a), 0.80 + 0.13 * math.sin(a)
    sh.path(V, f"M {x0:.3f} {y0:.3f} L {x1:.3f} {y1:.3f}", stroke="#9E2A2F", sw=2.4)
# iron edge binding, grips, strike marks
sh.path(V, PAV, "none", "#3F3C38", 3.2)
sh.path(V, "M -0.305 1.235 C -0.20 1.30 0.20 1.30 0.305 1.235", stroke="#8A857C", sw=.8)
sh.rivets(V, [(-0.30, y) for y in (0.15, 0.40, 0.65, 0.90, 1.15)] + [(0.30, y) for y in (0.15, 0.40, 0.65, 0.90, 1.15)], r=0.007, fill="#3F3C38", hi="#8A857C")
for (x, y) in ((-0.17, 0.52), (0.16, 1.06), (0.20, 0.30)):
    sh.add(f'<circle cx="{V.x(x):.1f}" cy="{V.y(y):.1f}" r="4.2" fill="#14120E"/>')
    for k in range(6):
        a = k * 1.05 + 0.3
        sh.path(V, f"M {x + 0.02*math.cos(a):.3f} {y + 0.02*math.sin(a):.3f} L {x + 0.04*math.cos(a):.3f} {y + 0.04*math.sin(a):.3f}", stroke="#6E5E44", sw=1)
# crossbow bolt stuck in, seen end-on: nock + three fletches
bx, by = -0.12, 1.02
for a in (90, 210, 330):
    r = math.radians(a)
    sh.path(V, f"M {bx} {by} L {bx + 0.035*math.cos(r):.3f} {by + 0.035*math.sin(r):.3f}", stroke="#6E6858", sw=3)
sh.add(f'<circle cx="{V.x(bx):.1f}" cy="{V.y(by):.1f}" r="3" fill="#5A4331" stroke="#1A120C"/>')
sh.flecks(V, (-0.30, 0.05, 0.30, 1.28), 50, "#1E1B17", 33, .002, .008, .4)
# left glove on the pavise edge
sh.path(Fm, "M 0.215 1.47 C 0.30 1.47 0.345 1.42 0.36 1.36 C 0.37 1.32 0.39 1.29 0.42 1.26 L 0.35 1.22 C 0.32 1.27 0.29 1.31 0.27 1.34 C 0.25 1.37 0.23 1.41 0.215 1.40 Z", "url(#wht)", "#5E574B", .8)
sh.path(Fm, "M 0.34 1.235 C 0.36 1.30 0.40 1.30 0.435 1.28 L 0.44 1.21 C 0.43 1.19 0.40 1.19 0.39 1.21 L 0.385 1.25 L 0.36 1.21 Z", "#5A4331", "#1A120C", .8)
for xx in (0.40, 0.415, 0.43):
    sh.path(Fm, f"M {xx} 1.27 L {xx} 1.21", stroke="#2A1E15", sw=.8)

# ───────────── SIDE ─────────────
figure_shadow(sh, 820, 150)
V = Sm
# the man, facing right, one arm out to the pavise
sh.path(V, "M -0.13 0.88 C -0.15 0.75 -0.12 0.60 -0.10 0.50 C -0.12 0.40 -0.12 0.28 -0.09 0.12 L 0.00 0.12 C 0.01 0.25 0.03 0.35 0.02 0.45 C 0.04 0.55 0.07 0.70 0.07 0.88 Z", "#241D16")
sh.path(V, "M -0.09 0.13 L 0.005 0.13 C 0.02 0.08 0.09 0.05 0.16 0.02 L 0.16 0.0 L -0.10 0.0 C -0.11 0.05 -0.10 0.09 -0.09 0.13 Z", "#22180F")
sh.path(V, "M -0.10 0.90 C -0.12 0.75 -0.09 0.60 -0.07 0.50 C -0.09 0.40 -0.09 0.28 -0.06 0.12 L 0.03 0.12 C 0.04 0.25 0.06 0.35 0.05 0.45 C 0.07 0.55 0.10 0.70 0.10 0.90 Z", "url(#hose)", "#0F0C09", .8)
sh.path(V, "M -0.075 0.34 L 0.052 0.34 L 0.045 0.13 L -0.062 0.13 Z", "url(#boot)", "#120C08", .8)
sh.path(V, "M -0.062 0.13 L 0.035 0.13 C 0.05 0.08 0.12 0.05 0.19 0.02 L 0.19 0.0 L -0.072 0.0 C -0.082 0.05 -0.072 0.09 -0.062 0.13 Z", "url(#boot)", "#120C08", .8)
sh.path(V, "M -0.17 0.76 L 0.15 0.76 L 0.16 0.70 L -0.18 0.70 Z", "url(#mail)", "#1A1B1A", .8)
sh.path(V, "M -0.11 1.50 C -0.135 1.40 -0.135 1.25 -0.115 1.08 C -0.15 0.98 -0.175 0.86 -0.19 0.73 L 0.165 0.73 C 0.15 0.86 0.13 0.98 0.11 1.08 C 0.14 1.2 0.155 1.32 0.125 1.42 C 0.10 1.49 0.06 1.53 0.02 1.54 Z", "url(#red)", "#3A0E10", .8)
for x0 in (-0.12, -0.06, 0.0, 0.06, 0.11):
    sh.path(V, f"M {x0*0.8:.3f} 1.04 C {x0*0.9:.3f} 0.95 {x0:.3f} 0.85 {x0*1.1:.3f} 0.74", stroke="#4E1216", sw=1.3, extra='opacity=".8"')
sh.path(V, "M -0.14 1.03 L 0.12 1.03 L 0.12 1.065 L -0.14 1.065 Z", "#3A2A1C", "#140E09", .8)
sh.path(V, "M -0.05 1.02 L -0.03 1.02 C -0.06 0.90 -0.11 0.80 -0.15 0.72 C -0.19 0.73 -0.20 0.76 -0.19 0.79 C -0.14 0.86 -0.08 0.94 -0.05 1.02 Z", "#3A2A1C", "#140E09", .8)
sh.path(V, "M -0.06 1.04 L 0.0 1.04 L 0.0 1.06 L -0.06 1.06 Z", "#3F3C38")
sh.path(V, "M -0.12 1.56 C -0.14 1.52 -0.14 1.49 -0.12 1.47 L 0.10 1.47 C 0.12 1.50 0.11 1.53 0.09 1.56 Z", "url(#mail)", "#1A1B1A", .8)
sh.path(V, "M 0.07 1.64 L 0.098 1.605 L 0.086 1.59 C 0.092 1.575 0.086 1.555 0.07 1.54 C 0.05 1.525 0.02 1.52 0.0 1.53 L -0.05 1.56 L -0.08 1.64 Z", "url(#skin)", "#2A241C", .8)
sh.path(V, "M 0.05 1.60 L 0.066 1.60", stroke="#14120E", sw=2)
sh.path(V, "M 0.105 1.62 C 0.115 1.70 0.09 1.78 0.02 1.80 C -0.05 1.81 -0.11 1.78 -0.14 1.72 C -0.16 1.67 -0.19 1.63 -0.23 1.60 C -0.17 1.59 -0.12 1.60 -0.09 1.62 C -0.02 1.63 0.05 1.635 0.105 1.62 Z", "url(#stR)", "#0A0B0C", 1.2)
sh.path(V, "M 0.02 1.80 C -0.05 1.81 -0.11 1.78 -0.14 1.72", stroke="#8C8D8C", sw=1.1, extra='opacity=".8"')
sh.rivets(V, [(0.08, 1.625), (0.0, 1.628), (-0.08, 1.622), (-0.16, 1.61)], r=0.005)
# arm reaching to the pavise's rear grip
sh.path(V, "M -0.055 1.46 C -0.07 1.38 -0.05 1.30 -0.02 1.24 L 0.06 1.28 C 0.05 1.35 0.05 1.42 0.05 1.46 Z", "url(#red)", "#3A0E10", .8)
sh.path(V, "M -0.03 1.25 C 0.08 1.20 0.20 1.17 0.36 1.16 L 0.36 1.23 C 0.22 1.24 0.10 1.27 0.04 1.30 Z", "url(#red)", "#3A0E10", .8)
sh.flecks(V, (-0.18, 0.73, 0.15, 1.5), 30, "#2A0A0C", 19, .003, .008, .4)

# pavise in profile, leaning back 4.4°, prop behind
base_x, base_y = Sm.x(0.46), 690
L = View(0, 0, 220)
g = [f'<g transform="translate({base_x:.1f},{base_y}) rotate(-4.4)">']
def P(d, fill, stroke=None, sw=1, extra=""):
    st = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    g.append(f'<path d="{L.p(d)}" fill="{fill}"{st} {extra}/>')
P("M -0.03 0.05 L -0.03 1.28 L 0.0 1.30 L 0.012 1.29 L 0.012 0.05 Z", "url(#pop)", "#2A241C", .8)  # board, back
P("M 0.012 0.06 L 0.012 1.29 L 0.02 1.29 L 0.02 0.06 Z", "#D6CDB6")  # gesso face edge
P("M 0.02 0.08 C 0.05 0.10 0.055 0.3 0.055 0.65 C 0.055 1.0 0.05 1.22 0.02 1.285 Z", "url(#ridge)", "#6E6858", .8)  # ridge bulge
P("M -0.03 0.05 L -0.02 0.0 L -0.01 0.05 Z", "#3F3C38")
P("M -0.034 0.05 L -0.034 1.29 L -0.03 1.29 L -0.03 0.05 Z", "#3F3C38")
for yy in (0.35, 0.95):  # rear grips
    P(f"M -0.03 {yy} C -0.08 {yy+0.01} -0.08 {yy+0.09} -0.03 {yy+0.10}", "none", "#5A4331", 3)
P("M -0.03 1.12 C -0.09 1.13 -0.09 1.22 -0.03 1.23", "none", "#5A4331", 3)
P("M 0.055 0.70 L 0.32 0.72 L 0.32 0.71 L 0.055 0.69 Z", "#5A4331")  # crossbow bolt stuck through
P("M 0.28 0.735 L 0.34 0.745 L 0.32 0.715 Z", "#6E6858")
P("M 0.28 0.695 L 0.34 0.685 L 0.32 0.715 Z", "#6E6858")
g.append("</g>")
sh.add("".join(g))
# prop leg hinged at 0.80 on the back, foot spiked into the ground
sh.path(Sm, "M 0.395 0.80 L 0.405 0.79 L 0.17 0.0 L 0.15 0.0 Z", "url(#pop)", "#2A241C", .8)
sh.path(Sm, "M 0.37 0.80 L 0.41 0.80 L 0.41 0.77 L 0.37 0.77 Z", "#3F3C38")
sh.path(Sm, "M 0.15 0.0 L 0.17 0.0 L 0.16 -0.0 Z", "#3F3C38")
# glove on the top grip
sh.path(Sm, "M 0.34 1.15 C 0.37 1.14 0.40 1.16 0.40 1.19 C 0.40 1.23 0.37 1.25 0.34 1.24 Z", "#5A4331", "#1A120C", .8)

cx = 340
sh.callout(Fm.x(-0.08), Fm.y(1.76), cx, 160, "OPEN SALLET", "no visor, to see", "end")
sh.callout(Fm.x(-0.15), Fm.y(1.50), cx, 215, "MAIL STANDARD", "riveted collar", "end")
sh.callout(Fm.x(-0.12), Fm.y(1.25), cx, 380, "PIED LIVERY COAT", "white | red wool", "end")
sh.callout(Fm.x(-0.20), Fm.y(0.76), cx, 440, "FALCHION", "in a leather scabbard", "end")
sh.callout(Fm.x(-0.08), Fm.y(0.73), cx, 500, "MAIL HEM", "shirt under the coat", "end")
rx = 978
sh.callout(Fp.x(0.0), Fp.y(0.84), 640, 225, "BRIQUET DEVICE", "painted fire-steel")
sh.callout(Fp.x(0.20), Fp.y(1.20), 640, 165, "PAVISE FACE", "gesso over poplar, 1.30 m")
sh.callout(Sm.x(0.38), Sm.y(1.10), rx, 300, "REAR GRIPS", "3 rawhide loops")
sh.callout(Sm.x(0.60), Sm.y(0.72), rx, 370, "STUCK BOLT", "trophy, not a weapon")
sh.callout(Sm.x(0.28), Sm.y(0.42), rx, 450, "HINGED PROP", "0.85 m poplar leg")
sh.callout(Sm.x(0.47), Sm.y(0.02), rx, 530, "SPIKED FEET", "bite into turf")

sh.palette([("#B09A74", "poplar"), ("#D6CDB6", "gesso white"), ("#9E2A2F", "livery red"),
            ("#3F3C38", "iron binding"), ("#2E2F31", "sallet"), ("#6D6E6C", "mail"), ("#5A4331", "leather")])
sh.write()
