from common_enemy import *

HS, OS = 150, 110
sh = Sheet("rolled-tapestry")
sh.frame(f"{AGE_NAME} · PLUNDER · 650 COIN · 11 ST", "Rolled Tapestry",
         "3.40 × 0.45 × 0.45 m · ≤ 5k tris · 1024²", f"hero 1 m = {HS} px along the roll · ortho 1 m = {OS} px", glow_xy=("35%", "70%"))
item_under(sh, 0.5, 0.5 * HS, f"0.5 m = {0.5*HS:.0f} px (hero) · {0.5*OS:.0f} px (ortho)",
           views=((360, "THREE-QUARTER"), (715, "END"), (960, "SIDE")))

sh.d('<pattern id="mille" width="22" height="22" patternUnits="userSpaceOnUse">'
     '<rect width="22" height="22" fill="#3A4A2C"/>'
     '<path d="M3 18 C5 14 8 13 10 14 M14 6 C16 3 19 3 20 5" stroke="#6E7E4E" stroke-width="1.4" fill="none"/>'
     '<circle cx="5" cy="6" r="1.8" fill="#A8905E"/><circle cx="16" cy="15" r="1.6" fill="#9E2A2F"/>'
     '<circle cx="11" cy="20" r="1.2" fill="#D6CDB6"/><circle cx="19" cy="9" r="1.1" fill="#A8905E"/>'
     '<path d="M8 9 L9 11 L7 11 Z M17 19 L18 21 L16 21 Z" fill="#6E7E4E"/></pattern>')
sh.d('<pattern id="border" width="16" height="16" patternUnits="userSpaceOnUse">'
     '<rect width="16" height="16" fill="#6B5238"/><path d="M0 8 C4 2 12 14 16 8" stroke="#A8905E" stroke-width="2" fill="none"/>'
     '<circle cx="8" cy="8" r="1.6" fill="#9E2A2F"/></pattern>')
sh.lin("rollV", [(0, "#1E2616", .0), (0, "#56663E"), (.3, "#6E7E4E"), (.55, "#4F5E3A"), (1, "#1A2012")], 0, 0, 0, 1)
sh.lin("shade", [(0, "#000", 0), (.5, "#000", .05), (1, "#0B0A08", .7)], 0, 0, 0, 1)
sh.lin("hi", [(0, "#DCD2BA", 0), (.25, "#DCD2BA", .18), (.4, "#DCD2BA", 0)], 0, 0, 0, 1)
sh.lin("linen", [(0, "#8F8672"), (.3, "#DDD3BB"), (.6, "#C9BFA6"), (1, "#6E6656")], 0, 0, 0, 1)
sh.rad("endcap", [(0, "#3A4A2C"), (1, "#26301C")])

L, R = 3.40 * HS, 0.225 * HS
ANG = -9.5
X0, Y0 = 110, 575
g = sh.add
g(f'<ellipse cx="{X0 + L/2*0.99:.1f}" cy="{Y0 - L/2*0.165 + R + 6:.1f}" rx="{L/2+20:.1f}" ry="14" fill="#0B0A08" opacity=".6" transform="rotate({ANG} {X0 + L/2:.1f} {Y0 - L/2*0.165 + R:.1f})"/>')
# flap of the outer turn, fallen open on the floor at the front end (drawn in sheet px)
g(f'<path d="M {X0+18} {Y0+R-6} L {X0+160} {Y0+R-30} L {X0+196} {Y0+R+52} L {X0+30} {Y0+R+62} Z" fill="url(#mille)" stroke="#1A2012" stroke-width="1"/>')
g(f'<path d="M {X0+30} {Y0+R+62} L {X0+196} {Y0+R+52} L {X0+193} {Y0+R+40} L {X0+28} {Y0+R+50} Z" fill="url(#border)"/>')
g(f'<path d="M {X0+18} {Y0+R-6} L {X0+160} {Y0+R-30} L {X0+170} {Y0+R-2} L {X0+22} {Y0+R+18} Z" fill="#0B0A08" opacity=".45"/>')
for i in range(10):  # fringe of warp ends
    fx = X0 + 32 + i * 16.4
    fy = Y0 + R + 62 - i * 1.0
    g(f'<path d="M {fx:.1f} {fy:.1f} L {fx-1:.1f} {fy+9:.1f}" stroke="#A8905E" stroke-width="1.2"/>')
# the roll body in its own rotated frame
g(f'<g transform="translate({X0},{Y0}) rotate({ANG})">')
g(f'<path d="M 0 {-R} L {L} {-R} A {R*0.42:.1f} {R} 0 0 1 {L} {R} L 0 {R} Z" fill="url(#mille)"/>')
g(f'<path d="M 0 {-R} L {L} {-R} A {R*0.42:.1f} {R} 0 0 1 {L} {R} L 0 {R} Z" fill="url(#shade)"/>')
g(f'<path d="M 0 {-R} L {L} {-R} L {L} {R} L 0 {R} Z" fill="url(#hi)"/>')
# linen wrapper over the middle third, three hemp ties
g(f'<path d="M {L*0.36:.1f} {-R-2} L {L*0.70:.1f} {-R-2} L {L*0.70:.1f} {R+2} L {L*0.36:.1f} {R+2} Z" fill="url(#linen)" stroke="#5E574B" stroke-width=".8"/>')
for fx in (0.40, 0.46, 0.52, 0.58, 0.64):
    g(f'<path d="M {L*fx:.1f} {-R} C {L*fx+6:.1f} {-R*0.3:.1f} {L*fx-4:.1f} {R*0.3:.1f} {L*fx+3:.1f} {R}" stroke="#8F8672" stroke-width="1" fill="none" opacity=".8"/>')
for tx in (0.14, 0.38, 0.68, 0.90):
    x = L * tx
    g(f'<path d="M {x:.1f} {-R-3} A {R*0.35:.1f} {R+3} 0 0 1 {x:.1f} {R+3}" stroke="#8A7456" stroke-width="4" fill="none"/>'
      f'<path d="M {x:.1f} {-R-3} A {R*0.35:.1f} {R+3} 0 0 1 {x:.1f} {R+3}" stroke="#C9B08A" stroke-width="1" fill="none" stroke-dasharray="2 3"/>')
g(f'<path d="M {L*0.90+R*0.35:.1f} {R*0.2:.1f} C {L*0.90+R*0.35+10:.1f} {R*0.6:.1f} {L*0.90+R*0.35+4:.1f} {R+12:.1f} {L*0.90+R*0.35+10:.1f} {R+22:.1f}" stroke="#8A7456" stroke-width="3" fill="none"/>')
# the near end: spiral of wool turns
g(f'<ellipse cx="0" cy="0" rx="{R*0.42:.1f}" ry="{R}" fill="url(#endcap)" stroke="#1A2012" stroke-width="1"/>')
pts = []
for i in range(0, 220):
    t = i / 220 * 6 * 2 * math.pi
    rr = (1 - i / 220) * 0.95
    pts.append((rr * R * 0.42 * math.cos(t), rr * R * math.sin(t)))
g('<path d="M ' + " L ".join(f"{a:.1f} {b:.1f}" for a, b in pts) + '" fill="none" stroke="#8A9A62" stroke-width="1.1"/>')
g('<path d="M ' + " L ".join(f"{a+0.8:.1f} {b+0.8:.1f}" for a, b in pts) + '" fill="none" stroke="#1A2012" stroke-width=".7"/>')
g(f'<circle cx="0" cy="0" r="3" fill="#14120E"/>')
g('</g>')
sh.flecks(View(0, 0, 1), (X0 + 40, -(Y0 + 20), X0 + 540, -(Y0 - 100)), 40, "#1E1B17", 12, 1, 2.5, .35)

# orthos: END and SIDE
E = View(715, 690, OS)
er = 0.225 * OS
g(f'<circle cx="{E.x(0):.1f}" cy="{E.y(0.225):.1f}" r="{er:.1f}" fill="url(#endcap)" stroke="#1A2012"/>')
pts = []
for i in range(0, 160):
    t = i / 160 * 5 * 2 * math.pi
    rr = (1 - i / 160) * 0.95 * er
    pts.append((E.x(0) + rr * math.cos(t), E.y(0.225) + rr * math.sin(t)))
g('<path d="M ' + " L ".join(f"{a:.1f} {b:.1f}" for a, b in pts) + '" fill="none" stroke="#8A9A62" stroke-width="1"/>')
g(f'<path d="M {E.x(-0.225):.1f} {E.y(0.225):.1f} A {er:.1f} {er:.1f} 0 0 0 {E.x(0.225):.1f} {E.y(0.225):.1f}" fill="none" stroke="#8A7456" stroke-width="3"/>')
S = View(960, 690, OS)
sx0, sx1 = S.x(-1.70), S.x(1.70)
g(f'<rect x="{sx0:.1f}" y="{S.y(0.45):.1f}" width="{sx1-sx0:.1f}" height="{0.45*OS:.1f}" rx="6" fill="url(#mille)" stroke="#1A2012"/>')
g(f'<rect x="{sx0:.1f}" y="{S.y(0.45):.1f}" width="{sx1-sx0:.1f}" height="{0.45*OS:.1f}" rx="6" fill="url(#rollV)" opacity=".55"/>')
g(f'<rect x="{S.x(-0.48):.1f}" y="{S.y(0.46):.1f}" width="{1.14*OS:.1f}" height="{0.47*OS:.1f}" fill="url(#linen)" stroke="#5E574B" stroke-width=".8"/>')
for tx in (-1.22, -0.40, 0.62, 1.38):
    g(f'<rect x="{S.x(tx)-2:.1f}" y="{S.y(0.465):.1f}" width="4" height="{0.48*OS:.1f}" fill="#8A7456"/>')
sh.text(S.x(0), S.y(0.45) - 10, "3.40 m", 10, "#9A9078", "middle")

# grab points: the two ends (two carriers)
grab(sh, X0 + 4, Y0 - 8, "GRAB", -10, -16, "end")
ex = X0 + L * math.cos(math.radians(ANG)) - 14
ey = Y0 + L * math.sin(math.radians(ANG))
grab(sh, ex, ey, "GRAB", 12, -14)
sh.text(40, 150, "UNBREAKABLE (999) · BURNS: IGNIS CATCHES IT, WORTH FALLS 10%/s WHILE ALIGHT", 10.5, "#C4542E", "start", extra='letter-spacing="1"')

sh.callout(X0 + 2, Y0 + 10, 60, 420, "WOOL SPIRAL", "~14 turns, 3 mm cloth")
sh.callout(X0 + L * 0.25, Y0 - L * 0.25 * 0.167, 200, 360, "MILLEFLEUR FIELD", "green wool, flowers")
sh.callout(X0 + L * 0.5, Y0 - L * 0.5 * 0.167 - 10, 380, 300, "LINEN WRAPPER", "1.15 m, loose-sewn")
sh.callout(X0 + L * 0.68 * 0.986, Y0 - L * 0.68 * 0.167 - R, 520, 380, "HEMP TIES", "4 lashings, knotted")
sh.callout(X0 + 150, Y0 + R + 50, 380, 655, "BORDER + FRINGE", "brown/buff guard band")
sh.palette([("#4F5E3A", "field green"), ("#6B5238", "wool brown"), ("#A8905E", "wool buff"),
            ("#9E2A2F", "wool red"), ("#C9BFA6", "linen wrap"), ("#8A7456", "hemp cord")])
sh.write()
