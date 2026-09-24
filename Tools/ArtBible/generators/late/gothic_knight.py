from common_enemy import *

# Authored in "1.80 space" and drawn at 220 px/m × 1.95/1.80, so the crown lands at 1.95 m.
K = 1.95 / 1.80
sh = Sheet("gothic-knight")
sh.frame(f"{AGE_NAME} · ENEMY · HEAVY", "Gothic Man-at-Arms", "H 1.95 m · ≤ 12k tris · 2048²", "1 m = 220 px · ground y 690")
sh.ladder_enemy()
F, S = View(470, 690, 220 * K), View(820, 690, 220 * K)

# white harness: polished, cold, reads lighter than the blackened guards
sh.lin("wh", [(0, "#2A2C2E"), (.18, "#6E7174"), (.34, "#C9CCCC"), (.46, "#8E9194"), (.75, "#4A4D50"), (1, "#1E2022")])
sh.lin("whV", [(0, "#B9BCBD"), (.3, "#8E9194"), (1, "#2E3032")], 0, 0, 0, 1)
sh.rad("whR", [(0, "#D6D8D8"), (.3, "#8E9194"), (1, "#26282A")], .36, .3, .8)
sh.lin("ash", [(0, "#4F4231"), (.45, "#A08968"), (1, "#5C4C38")])
sh.lin("red", [(0, "#6E1C20"), (.5, "#9E2A2F"), (1, "#5E171B")])
mail_pattern(sh)
FL, FD = "#D6D8D8", "#2E3032"  # flute highlight / flute shadow


def flute(v, d, both=False, w=1.0):
    sh.path(v, d, stroke=FD, sw=1.6 * w, both=both, extra='opacity=".75"')
    sh.path(v, d, stroke=FL, sw=.6 * w, both=both, extra='opacity=".8" transform="translate(1.4,0)"')


# ───────────── FRONT ─────────────
figure_shadow(sh, 470, 120)
# mail between the legs
sh.path(F, "M -0.06 0.98 L 0.06 0.98 L 0.05 0.80 L -0.05 0.80 Z", "url(#mail)")
# sabatons
sh.path(F, "M 0.05 0.105 L 0.145 0.105 C 0.158 0.06 0.172 0.02 0.168 0.0 L 0.03 0.0 C 0.034 0.04 0.044 0.075 0.05 0.105 Z", "url(#wh)", "#101112", .8, both=True)
for yy in (0.08, 0.055, 0.03):
    sh.path(F, f"M 0.04 {yy} L 0.162 {yy}", stroke=FD, sw=1, both=True)
# greaves
sh.path(F, "M 0.055 0.46 C 0.05 0.35 0.055 0.22 0.06 0.10 L 0.145 0.10 C 0.155 0.22 0.17 0.33 0.165 0.46 Z", "url(#wh)", "#101112", 1, both=True)
sh.path(F, "M 0.11 0.46 L 0.103 0.11", stroke=FL, sw=1, both=True)
# cuisses with flutes
sh.path(F, "M 0.02 0.86 C 0.10 0.88 0.19 0.86 0.20 0.80 C 0.205 0.70 0.19 0.62 0.18 0.56 L 0.05 0.56 C 0.04 0.65 0.03 0.75 0.02 0.86 Z", "url(#wh)", "#101112", 1, both=True)
for x0 in (0.07, 0.11, 0.15):
    flute(F, f"M {x0} 0.84 C {x0+0.005} 0.75 {x0} 0.66 {x0-0.005} 0.60", both=True)
# poleyns with fan wing
sh.path(F, "M 0.045 0.565 C 0.08 0.595 0.15 0.595 0.18 0.565 L 0.185 0.46 C 0.14 0.43 0.08 0.43 0.045 0.46 Z", "url(#whR)", "#101112", 1, both=True)
sh.path(F, "M 0.176 0.565 C 0.24 0.555 0.25 0.47 0.19 0.445 C 0.20 0.49 0.195 0.53 0.176 0.565 Z", "url(#whV)", "#101112", .8, both=True)
for a in (0.54, 0.51, 0.48):
    sh.path(F, f"M 0.186 {a:.2f} L 0.23 {a-0.01:.2f}", stroke=FD, sw=.9, both=True)
sh.path(F, "M 0.07 0.52 C 0.09 0.535 0.14 0.535 0.16 0.52", stroke=FD, sw=1.2, both=True)
# tassets (Gothic, pointed)
sh.path(F, "M 0.025 0.985 L 0.195 0.995 L 0.212 0.82 L 0.12 0.735 L 0.035 0.80 Z", "url(#wh)", "#101112", 1, both=True)
for x0 in (0.07, 0.12, 0.17):
    flute(F, f"M {x0} 0.98 L {0.12 + (x0-0.12)*0.3:.3f} 0.77", both=True, w=.8)
# fauld
sh.path(F, "M -0.19 1.085 L 0.19 1.085 L 0.215 0.975 L -0.215 0.975 Z", "url(#wh)", "#101112", 1)
for yy in (1.05, 1.015):
    sh.path(F, f"M -0.2 {yy} L 0.2 {yy}", stroke=FD, sw=1.3)
    sh.path(F, f"M -0.2 {yy-0.004} L 0.2 {yy-0.004}", stroke=FL, sw=.5)
sh.rivets(F, [(-0.17, 1.03), (0.17, 1.03), (-0.18, 0.995), (0.18, 0.995)], r=0.006, fill="#6E7174", hi="#E0E0DC")
# livery sash knotted at the left hip
sh.path(F, "M -0.20 1.10 C -0.05 1.06 0.10 1.02 0.21 0.99 L 0.22 1.03 C 0.10 1.07 -0.05 1.11 -0.19 1.14 Z", "url(#red)", "#3A0E10", .8)
sh.path(F, "M 0.20 1.01 C 0.25 0.95 0.24 0.86 0.26 0.80 L 0.235 0.80 C 0.22 0.87 0.22 0.94 0.19 1.00 Z", "url(#red)", "#3A0E10", .8)
sh.path(F, "M 0.21 1.00 C 0.23 0.96 0.28 0.93 0.29 0.87 L 0.27 0.87 C 0.26 0.91 0.22 0.95 0.20 0.98 Z", "#6E1C20")
# breastplate + cusped plackart
sh.path(F, "M -0.23 1.46 C -0.22 1.36 -0.20 1.22 -0.16 1.12 L -0.175 1.08 L 0.175 1.08 L 0.16 1.12 C 0.20 1.22 0.22 1.36 0.23 1.46 C 0.14 1.50 -0.14 1.50 -0.23 1.46 Z", "url(#wh)", "#101112", 1.2)
sh.path(F, "M 0 1.48 L 0 1.30", stroke=FL, sw=1.2)
sh.path(F, "M -0.175 1.08 C -0.185 1.15 -0.175 1.22 -0.145 1.26 C -0.12 1.25 -0.1 1.28 -0.085 1.265 C -0.065 1.29 -0.045 1.28 -0.035 1.30 L 0 1.37 L 0.035 1.30 C 0.045 1.28 0.065 1.29 0.085 1.265 C 0.1 1.28 0.12 1.25 0.145 1.26 C 0.175 1.22 0.185 1.15 0.175 1.08 Z", "url(#wh)", "#101112", 1.2)
for x0 in (0.03, 0.07, 0.11, 0.15):
    flute(F, f"M {x0*0.4:.3f} 1.09 C {x0*0.7:.3f} 1.15 {x0:.3f} 1.20 {x0:.3f} 1.26", both=True)
sh.path(F, "M -0.18 1.40 L -0.12 1.40 L -0.12 1.36 L -0.18 1.37 Z", "#4A4D50", "#101112", .8)  # lance rest
sh.rivets(F, [(-0.17, 1.385), (-0.13, 1.385)], r=0.005, fill="#6E7174", hi="#E0E0DC")
# gorget
sh.path(F, "M -0.14 1.52 C -0.08 1.48 0.08 1.48 0.14 1.52 L 0.13 1.46 C 0.08 1.44 -0.08 1.44 -0.13 1.46 Z", "url(#wh)", "#101112", 1)
# arms
for m in (False, True):
    sh.path(F, "M 0.235 1.34 L 0.33 1.30 L 0.332 1.14 L 0.245 1.14 Z", "url(#wh)", "#101112", 1, mirror=m)  # rerebrace
    sh.path(F, "M 0.30 1.205 C 0.34 1.21 0.36 1.16 0.35 1.12 C 0.41 1.13 0.42 1.18 0.40 1.22 C 0.37 1.24 0.33 1.23 0.30 1.205 Z", "url(#whV)", "#101112", .8, mirror=m)  # couter fan
    sh.path(F, "M 0.228 1.19 C 0.26 1.215 0.33 1.215 0.352 1.175 L 0.352 1.10 C 0.32 1.08 0.25 1.08 0.228 1.10 Z", "url(#whR)", "#101112", 1, mirror=m)
    for a in (0.36, 0.38):
        sh.path(F, f"M {a} 1.135 L {a+0.01} 1.205", stroke=FD, sw=.8, mirror=m)
# viewer-right arm hangs; viewer-left arm reaches out to the poleaxe
sh.path(F, "M 0.245 1.10 L 0.34 1.10 C 0.345 1.0 0.335 0.93 0.325 0.88 L 0.255 0.88 C 0.25 0.95 0.245 1.02 0.245 1.10 Z", "url(#wh)", "#101112", 1)
sh.path(F, "M 0.235 0.90 L 0.348 0.90 L 0.332 0.84 C 0.332 0.80 0.317 0.76 0.292 0.75 C 0.267 0.75 0.252 0.78 0.252 0.83 Z", "url(#wh)", "#101112", 1)
for x0 in (0.26, 0.285, 0.31):
    sh.path(F, f"M {x0} 0.835 L {x0+0.005} 0.77", stroke=FD, sw=1)
sh.path(F, "M 0.235 0.90 L 0.348 0.90 L 0.345 0.885 L 0.238 0.885 Z", FL, extra='opacity=".6"')
sh.path(F, "M -0.245 1.10 L -0.34 1.10 C -0.36 1.02 -0.375 0.96 -0.385 0.92 L -0.318 0.90 C -0.30 0.96 -0.27 1.03 -0.245 1.10 Z", "url(#wh)", "#101112", 1)

# pauldrons (left larger, Gothic asymmetry) + besagews
for m, sc in ((False, 1.0), (True, 0.92)):
    sh.path(F, f"M 0.15 1.50 C 0.24 {1.50+0.05*sc:.3f} {0.15+0.19*sc:.3f} 1.52 {0.15+0.21*sc:.3f} 1.42 C {0.15+0.22*sc:.3f} 1.34 {0.15+0.2*sc:.3f} 1.28 {0.15+0.18*sc:.3f} 1.26 C 0.29 1.30 0.22 1.33 0.18 1.36 Z", "url(#whR)", "#101112", 1.2, mirror=m)
    for yy in (1.44, 1.39, 1.34):
        sh.path(F, f"M 0.19 {yy+0.03} C 0.25 {yy+0.02} 0.31 {yy} {0.15+0.2*sc:.3f} {yy-0.04}", stroke=FD, sw=1.3, mirror=m)
        sh.path(F, f"M 0.19 {yy+0.034} C 0.25 {yy+0.024} 0.31 {yy+0.004} {0.15+0.2*sc:.3f} {yy-0.036}", stroke=FL, sw=.6, mirror=m)
    sh.add(f'<circle cx="{F.x(0.215 if not m else -0.215):.1f}" cy="{F.y(1.30):.1f}" r="{0.045*F.s:.1f}" fill="url(#whR)" stroke="#101112"/>')
    sh.add(f'<circle cx="{F.x(0.215 if not m else -0.215):.1f}" cy="{F.y(1.30):.1f}" r="{0.012*F.s:.1f}" fill="#4A4D50"/>')

# poleaxe (1.70 m real = 1.57 in authoring space), shaft at x = -0.40
PX = -0.40
sh.path(F, f"M {PX-0.016} 0 L {PX+0.016} 0 L {PX+0.016} 1.40 L {PX-0.016} 1.40 Z", "url(#ash)")
sh.path(F, f"M {PX-0.02} 0 L {PX+0.02} 0 L {PX+0.02} 0.05 L {PX-0.02} 0.05 Z", "url(#whV)")  # butt spike cap
sh.path(F, f"M {PX-0.012} 0 L {PX} -0.0 L {PX+0.012} 0 Z", "#4A4D50")
sh.path(F, f"M {PX-0.018} 1.05 L {PX+0.018} 1.05 L {PX+0.018} 1.47 L {PX-0.018} 1.47 Z", "url(#wh)", "#101112", .8)  # langets
sh.rivets(F, [(PX, 1.10), (PX, 1.18), (PX, 1.26), (PX, 1.34)], r=0.004, fill="#6E7174", hi="#E0E0DC")
sh.add(f'<ellipse cx="{F.x(PX):.1f}" cy="{F.y(1.02):.1f}" rx="{0.05*F.s:.1f}" ry="{0.012*F.s:.1f}" fill="url(#wh)" stroke="#101112"/>')  # rondel
# axe blade outward, hammer inward, top spike
sh.path(F, f"M {PX-0.018} 1.47 C {PX-0.08} 1.48 {PX-0.15} 1.50 {PX-0.19} 1.53 C {PX-0.175} 1.46 {PX-0.175} 1.40 {PX-0.195} 1.33 C {PX-0.14} 1.35 {PX-0.07} 1.38 {PX-0.018} 1.40 Z", "url(#wh)", "#101112", 1)
sh.path(F, f"M {PX-0.19} 1.53 C {PX-0.175} 1.46 {PX-0.175} 1.40 {PX-0.195} 1.33", stroke="#E0E0DC", sw=1.4)
sh.path(F, f"M {PX+0.018} 1.45 L {PX+0.075} 1.46 L {PX+0.075} 1.39 L {PX+0.018} 1.40 Z", "url(#whV)", "#101112", 1)
for yy in (1.445, 1.425, 1.405):
    sh.path(F, f"M {PX+0.075} {yy} L {PX+0.086} {yy-0.004} L {PX+0.075} {yy-0.012} Z", "#4A4D50")
sh.path(F, f"M {PX-0.02} 1.47 L {PX} 1.57 L {PX+0.02} 1.47 Z", "url(#wh)", "#101112", .8)
# right gauntlet on the haft
sh.path(F, "M -0.33 0.935 C -0.345 0.93 -0.43 0.93 -0.44 0.90 C -0.445 0.86 -0.43 0.83 -0.40 0.82 C -0.37 0.82 -0.345 0.84 -0.34 0.87 Z", "url(#wh)", "#101112", 1)
for yy in (0.895, 0.87, 0.845):
    sh.path(F, f"M -0.43 {yy} L -0.37 {yy}", stroke=FD, sw=1)

# armet
sh.path(F, "M -0.13 1.60 C -0.14 1.70 -0.11 1.78 -0.06 1.80 C -0.02 1.81 0.02 1.81 0.06 1.80 C 0.11 1.78 0.14 1.70 0.13 1.60 C 0.12 1.54 0.08 1.49 0.0 1.48 C -0.08 1.49 -0.12 1.54 -0.13 1.60 Z", "url(#whR)", "#0C0D0E", 1.2)
sh.path(F, "M 0 1.80 L 0 1.70", stroke=FL, sw=1.4)
sh.path(F, "M -0.125 1.585 C -0.08 1.51 0.08 1.51 0.125 1.585 L 0.12 1.54 C 0.08 1.49 -0.08 1.49 -0.12 1.54 Z", "url(#wh)", "#0C0D0E", .8)  # wrapper
sh.path(F, "M -0.115 1.685 L 0.115 1.685 C 0.115 1.625 0.065 1.565 0 1.545 C -0.065 1.565 -0.115 1.625 -0.115 1.685 Z", "url(#wh)", "#0C0D0E", 1)  # visor
sh.path(F, "M 0 1.685 L 0 1.55", stroke=FL, sw=1.2)
sh.path(F, "M -0.10 1.668 L -0.015 1.668 L -0.015 1.678 L -0.10 1.678 Z", "#050505")
sh.path(F, "M 0.015 1.668 L 0.10 1.668 L 0.10 1.678 L 0.015 1.678 Z", "#050505")
for i in range(5):
    sh.add(f'<circle cx="{F.x(0.03 + i*0.014):.1f}" cy="{F.y(1.61 - i*0.012):.1f}" r="1.3" fill="#050505"/>')
sh.path(F, "M -0.07 1.79 C -0.115 1.765 -0.13 1.70 -0.128 1.62", stroke="#E0E0DC", sw=1.2, extra='opacity=".8"')
sh.rivets(F, [(-0.125, 1.68), (0.125, 1.68)], r=0.008, fill="#6E7174", hi="#E0E0DC")
# soot in the crevices, scuffs
sh.flecks(F, (-0.23, 0.0, 0.23, 1.5), 60, "#1E1B17", 31, .002, .006, .45)
sh.flecks(F, (-0.13, 1.5, 0.13, 1.8), 12, "#1E1B17", 32, .002, .006, .5)

# ───────────── SIDE ─────────────
figure_shadow(sh, 820, 120)
# poleaxe on the far side, planted forward (blade edge-on)
QX = 0.23
sh.path(S, f"M {QX-0.016} 0 L {QX+0.016} 0 L {QX+0.016} 1.40 L {QX-0.016} 1.40 Z", "url(#ash)")
sh.path(S, f"M {QX-0.018} 1.05 L {QX+0.018} 1.05 L {QX+0.018} 1.47 L {QX-0.018} 1.47 Z", "url(#wh)", "#101112", .8)
sh.path(S, f"M {QX-0.006} 1.33 L {QX+0.006} 1.33 L {QX+0.006} 1.53 L {QX-0.006} 1.53 Z", "#B9BCBD", "#101112", .6)
sh.path(S, f"M {QX-0.012} 1.47 L {QX} 1.57 L {QX+0.012} 1.47 Z", "url(#wh)", "#101112", .8)
sh.add(f'<ellipse cx="{S.x(QX):.1f}" cy="{S.y(1.02):.1f}" rx="{0.05*S.s:.1f}" ry="{0.012*S.s:.1f}" fill="url(#wh)" stroke="#101112"/>')
sh.path(S, "M 0.02 1.12 C 0.08 1.06 0.14 1.00 0.19 0.95 L 0.225 0.99 C 0.17 1.04 0.11 1.10 0.06 1.16 Z", "#5A5D60", "#101112", .8)
sh.path(S, f"M {QX-0.045} 0.965 L {QX+0.01} 0.975 L {QX+0.035} 0.93 C {QX+0.045} 0.89 {QX+0.035} 0.85 {QX+0.01} 0.84 C {QX-0.02} 0.835 {QX-0.04} 0.86 {QX-0.035} 0.90 Z", "#6E7174", "#101112", .8)
for yy in (0.905, 0.88, 0.855):
    sh.path(S, f"M {QX-0.03} {yy} L {QX+0.035} {yy}", stroke=FD, sw=.9)
# back leg
sh.path(S, "M -0.13 0.88 C -0.15 0.75 -0.11 0.62 -0.08 0.56 L -0.1 0.46 C -0.12 0.36 -0.115 0.24 -0.09 0.10 L 0.01 0.10 C 0.02 0.22 0.035 0.34 0.03 0.46 L 0.04 0.56 C 0.06 0.65 0.07 0.76 0.07 0.88 Z", "#3A3C3E")
sh.path(S, "M -0.09 0.10 L 0.01 0.10 C 0.05 0.06 0.13 0.03 0.21 0.01 L 0.21 0.0 L -0.11 0.0 C -0.115 0.04 -0.105 0.07 -0.09 0.10 Z", "#2E3032")
# front leg
sh.path(S, "M -0.10 0.88 C -0.12 0.75 -0.08 0.62 -0.05 0.56 L 0.06 0.56 C 0.08 0.65 0.10 0.76 0.10 0.88 Z", "url(#wh)", "#101112", 1)
for x0 in (-0.05, 0.0, 0.05):
    flute(S, f"M {x0} 0.86 C {x0+0.005} 0.76 {x0+0.01} 0.66 {x0+0.012} 0.59")
sh.path(S, "M -0.07 0.46 C -0.095 0.36 -0.09 0.24 -0.065 0.10 L 0.035 0.10 C 0.045 0.22 0.06 0.34 0.055 0.46 Z", "url(#wh)", "#101112", 1)
sh.path(S, "M -0.062 0.105 L 0.04 0.105 C 0.08 0.065 0.16 0.035 0.24 0.012 L 0.24 0.0 L -0.08 0.0 C -0.085 0.04 -0.075 0.075 -0.062 0.105 Z", "url(#whV)", "#101112", 1)
for x0 in (0.05, 0.09, 0.13, 0.17):
    sh.path(S, f"M {x0} {0.0} L {x0-0.03} {0.09 - (x0-0.05)*0.5:.3f}", stroke=FD, sw=1)
sh.path(S, "M -0.02 0.585 C 0.05 0.59 0.085 0.52 0.07 0.45 L -0.06 0.45 C -0.05 0.50 -0.04 0.55 -0.02 0.585 Z", "url(#whR)", "#101112", 1)
sh.path(S, "M -0.03 0.56 C -0.11 0.575 -0.13 0.50 -0.06 0.455 C -0.06 0.49 -0.045 0.53 -0.03 0.56 Z", "url(#whV)", "#101112", .8)
for yy in (0.53, 0.50, 0.475):
    sh.path(S, f"M -0.05 {yy} L -0.105 {yy+0.005}", stroke=FD, sw=.9)
# culet + tasset
sh.path(S, "M -0.13 1.08 C -0.17 1.02 -0.18 0.96 -0.16 0.90 L -0.08 0.92 L -0.08 1.08 Z", "url(#wh)", "#101112", 1)
for yy in (1.03, 0.98, 0.94):
    sh.path(S, f"M -0.17 {yy} L -0.08 {yy+0.01}", stroke=FD, sw=1.1)
sh.path(S, "M -0.01 0.99 L 0.14 0.99 L 0.155 0.82 L 0.075 0.745 L -0.005 0.82 Z", "url(#wh)", "#101112", 1)
for x0 in (0.03, 0.075, 0.12):
    flute(S, f"M {x0} 0.98 L {0.075 + (x0-0.075)*0.3:.3f} 0.78", w=.8)
# torso, fauld, sash
sh.path(S, "M -0.13 1.085 L 0.12 1.085 L 0.155 0.975 L -0.165 0.975 Z", "url(#wh)", "#101112", 1)
sh.path(S, "M -0.10 1.48 C -0.145 1.38 -0.145 1.22 -0.115 1.08 L 0.10 1.08 C 0.145 1.18 0.165 1.30 0.145 1.40 C 0.125 1.47 0.065 1.50 0.0 1.50 Z", "url(#wh)", "#101112", 1.2)
sh.path(S, "M 0.10 1.08 C 0.145 1.18 0.165 1.30 0.145 1.40 L 0.13 1.26 L 0.06 1.30 L 0.03 1.20 L 0.0 1.10 Z", "url(#whV)", "#101112", .8, extra='opacity=".85"')  # plackart
for x0 in (-0.09, -0.05):
    flute(S, f"M {x0} 1.10 C {x0-0.01} 1.22 {x0-0.01} 1.34 {x0+0.01} 1.44")
sh.path(S, "M -0.12 1.10 C 0.0 1.07 0.08 1.04 0.14 1.02 L 0.145 1.055 C 0.08 1.075 0.0 1.105 -0.115 1.135 Z", "url(#red)", "#3A0E10", .8)
sh.path(S, "M 0.14 1.08 L 0.21 1.08 L 0.21 1.05 L 0.13 1.05 Z", "#4A4D50", "#101112", .8)  # lance rest
# gorget, armet with sparrow-beak visor, rondel
sh.path(S, "M -0.09 1.52 L 0.08 1.52 L 0.10 1.46 L -0.11 1.46 Z", "url(#wh)", "#101112", 1)
sh.path(S, "M -0.13 1.53 L -0.19 1.53", stroke="#4A4D50", sw=3)
sh.add(f'<circle cx="{S.x(-0.205):.1f}" cy="{S.y(1.53):.1f}" r="{0.04*S.s:.1f}" fill="url(#whR)" stroke="#101112"/>')
sh.path(S, "M 0.13 1.62 C 0.14 1.72 0.10 1.79 0.02 1.80 C -0.06 1.80 -0.12 1.75 -0.13 1.66 C -0.135 1.58 -0.11 1.52 -0.07 1.49 L 0.06 1.48 C 0.11 1.52 0.13 1.57 0.13 1.62 Z", "url(#whR)", "#0C0D0E", 1.2)
sh.path(S, "M -0.13 1.66 C -0.12 1.75 -0.06 1.80 0.02 1.80", stroke="#E0E0DC", sw=1.2, extra='opacity=".8"')
sh.path(S, "M -0.02 1.53 C 0.04 1.50 0.10 1.52 0.12 1.56", stroke=FD, sw=1.2)
sh.path(S, "M 0.02 1.72 C 0.08 1.72 0.13 1.695 0.145 1.665 C 0.19 1.625 0.19 1.60 0.165 1.58 C 0.125 1.56 0.065 1.55 0.02 1.55 C 0.0 1.60 0.0 1.68 0.02 1.72 Z", "url(#wh)", "#0C0D0E", 1)
sh.path(S, "M 0.06 1.665 L 0.16 1.655 L 0.158 1.645 L 0.06 1.655 Z", "#050505")
for i in range(4):
    sh.add(f'<circle cx="{S.x(0.07 + i*0.02):.1f}" cy="{S.y(1.60):.1f}" r="1.3" fill="#050505"/>')
sh.rivets(S, [(0.0, 1.66)], r=0.012, fill="#6E7174", hi="#E0E0DC")
# near arm: pauldron, rerebrace, couter, vambrace, gauntlet hanging
sh.path(S, "M -0.05 1.40 L 0.05 1.40 L 0.05 1.14 L -0.045 1.14 Z", "url(#wh)", "#101112", 1)
sh.path(S, "M -0.06 1.19 C -0.075 1.14 -0.04 1.09 0.01 1.09 C 0.06 1.10 0.07 1.16 0.05 1.20 Z", "url(#whR)", "#101112", 1)
sh.path(S, "M -0.06 1.18 C -0.11 1.20 -0.13 1.14 -0.09 1.10 C -0.08 1.13 -0.07 1.16 -0.06 1.18 Z", "url(#whV)", "#101112", .8)
sh.path(S, "M -0.04 1.10 L 0.045 1.10 C 0.05 1.0 0.045 0.93 0.04 0.88 L -0.03 0.88 C -0.035 0.95 -0.04 1.02 -0.04 1.10 Z", "url(#wh)", "#101112", 1)
sh.path(S, "M -0.045 0.90 L 0.06 0.90 L 0.055 0.84 C 0.06 0.80 0.045 0.76 0.02 0.75 C -0.01 0.75 -0.03 0.78 -0.03 0.83 Z", "url(#wh)", "#101112", 1)
sh.path(S, "M -0.14 1.50 C -0.05 1.555 0.07 1.54 0.118 1.46 C 0.135 1.40 0.115 1.33 0.095 1.30 C 0.035 1.34 -0.06 1.36 -0.125 1.36 C -0.155 1.44 -0.155 1.50 -0.14 1.55 Z", "url(#whR)", "#101112", 1.2)
for yy in (1.42, 1.38, 1.34):
    sh.path(S, f"M -0.14 {yy+0.02} C -0.05 {yy+0.02} 0.05 {yy} 0.11 {yy-0.03}", stroke=FD, sw=1.3)
    sh.path(S, f"M -0.14 {yy+0.024} C -0.05 {yy+0.024} 0.05 {yy+0.004} 0.11 {yy-0.026}", stroke=FL, sw=.6)
sh.flecks(S, (-0.16, 0.0, 0.16, 1.5), 50, "#1E1B17", 41, .002, .006, .45)

sh.add('<text x="470" y="712" text-anchor="middle" font-family="Overpass Mono, monospace" font-size="10" fill="#635C4C">poleaxe 1.70 m · passes 2.88 m arches upright</text>')

cx = 352
sh.callout(F.x(-0.06), F.y(1.76), cx, 160, "ARMET", "visor + wrapper, rondel", "end")
sh.callout(F.x(PX - 0.14), F.y(1.46), cx, 215, "POLEAXE HEAD", "axe, 4-pt hammer, spike", "end")
sh.callout(F.x(-0.28), F.y(1.42), cx, 270, "PAULDRON", "fluted, besagew", "end")
sh.callout(F.x(-0.10), F.y(1.17), cx, 370, "CUSPED PLACKART", "rising to a point", "end")
sh.callout(F.x(PX), F.y(1.02), cx, 440, "RONDEL GUARD", "on langets, 0.11 m", "end")
sh.callout(F.x(-0.12), F.y(0.80), cx, 510, "POINTED TASSET", "fluted, 3 flutes", "end")
rx = 978
sh.callout(S.x(0.17), S.y(1.61), rx, 170, "SPARROW-BEAK VISOR", "pivots, breaths R side")
sh.callout(S.x(-0.205), S.y(1.53), rx, 250, "RONDEL", "disc on the armet tail")
sh.callout(S.x(-0.09), S.y(1.14), rx, 320, "FAN COUTER", "wing guards the elbow")
sh.callout(S.x(0.10), S.y(1.05), rx, 400, "LIVERY SASH", "red wool, knotted")
sh.callout(S.x(-0.09), S.y(0.51), rx, 500, "FAN POLEYN", "fluted knee wing")
sh.callout(S.x(0.13), S.y(0.035), rx, 590, "SABATON", "7 lames, poulaine toe")

sh.palette([("#8E9194", "white harness"), ("#6D6E6C", "mail voiders"), ("#9E2A2F", "livery sash"),
            ("#8A7456", "ash haft"), ("#5A4331", "straps"), ("#1E1B17", "soot")])
sh.write()
