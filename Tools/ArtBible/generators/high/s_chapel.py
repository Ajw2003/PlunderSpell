from lib import *

s = Sheet("castle-chapel")
LIME = "#A89F8A"; WASH = "#CFC5AE"; RUBY = "#7E2A26"; WOAD = "#3E5470"; LEAD = "#2A2622"; OAK = "#5E4630"; GILT = "#C9A227"; FIRE = "#C4542E"
K = 70; X0 = 90; G = 690
U = lambda u: X0 + (u - .6) * K     # section across the cell, north (u .6) on the left
Z = lambda z: G - z * K
p_cut = hatch(s, "cut", shade(LIME, .8), 7, 1)
p_ash = ashlar(s, "ash", 28, 14, shade(WASH, .85), X0, 0)
g_wash = s.lg(shade(WASH, .75), shade(WASH, .35), 0, 0, 0, 1)
g_vault = s.lg(shade(WASH, .35), shade(WASH, .8), 0, 0, 0, 1)
g_fire = s.rg(FIRE, FIRE, .45, 0, .5, .5, .5)
g_candle = s.rg("#E8C080", FIRE, .95, 0, .5, .5, .5)
g_gilt = s.lg(shade(GILT, 1.35), shade(GILT, .55), 0, 0, 1, 1, mid=GILT)
g_ruby = s.lg(shade(RUBY, 1.6), RUBY, 0, 0, 0, 1)
g_woad = s.lg(shade(WOAD, 1.6), WOAD, 0, 0, 0, 1)
g_oak = s.lg(shade(OAK, 1.2), shade(OAK, .6), 0, 0, 1, 0)

F0 = .3                      # floor top
IN0, IN1 = 3.3, 8.7          # chapel interior, north..south (m)
SPR, CRN = F0 + 2.9, F0 + 4.3
parts = [metre_ladder(58, K, G, 5.5, 1)]
sec = []
sec.append(f'<rect x="{U(.6)}" y="{Z(F0)}" width="{9*K}" height="{F0*K}" fill="{p_cut}" stroke="#14120E"/>')
# east wall beyond: limewash with false-ashlar lines, pointed vault silhouette
vault = (f"M{U(IN0)},{Z(F0)} L{U(IN0)},{Z(SPR)} C{U(IN0)},{Z(CRN-.3)} {U(6-1.2)},{Z(CRN)} {U(6)},{Z(CRN+.2)} "
         f"C{U(6+1.2)},{Z(CRN)} {U(IN1)},{Z(CRN-.3)} {U(IN1)},{Z(SPR)} L{U(IN1)},{Z(F0)} Z")
s.clip(vault, "vlt")
sec.append(f'<path d="{vault}" fill="{p_ash}"/>')
sec.append(f'<path d="{vault}" fill="{g_wash}" opacity=".55"/>')
# painted masonry lines are already the ashlar; add a painted dado of kermes/woad bands
sec.append(f'<g clip-path="url(#vlt)"><rect x="{U(IN0)}" y="{Z(F0+1.2)}" width="{5.4*K}" height="{1.2*K}" fill="{RUBY}" opacity=".55"/>'
           + ''.join(f'<path d="M{U(IN0)+i*21},{Z(F0+1.2)} q10,24 0,{1.2*K:.0f}" fill="none" stroke="{WASH}" stroke-width="1.2" opacity=".35"/>' for i in range(19))
           + f'<rect x="{U(IN0)}" y="{Z(F0+1.25)}" width="{5.4*K}" height="5" fill="{WOAD}"/></g>')
# three lancets in the east wall, stained glass
for (u, w, top) in ((5.1, .45, 3.9), (6.0, .5, 4.2), (6.9, .45, 3.9)):
    sill = F0 + 1.8
    lan = f"M{U(u-w/2)},{Z(sill)} L{U(u-w/2)},{Z(top+F0-.45)} Q{U(u-w/2)},{Z(top+F0-.1)} {U(u)},{Z(top+F0)} Q{U(u+w/2)},{Z(top+F0-.1)} {U(u+w/2)},{Z(top+F0-.45)} L{U(u+w/2)},{Z(sill)} Z"
    sec.append(f'<path d="{lan}" fill="{LEAD}" stroke="#14120E" stroke-width="2"/>')
    cid = f"lan{int(u*10)}"
    s.clip(lan, cid)
    panes = []
    rows = int((top - 1.8) / .22) + 2
    for r in range(rows):
        for c in range(2):
            col = (g_ruby, g_woad, WASH)[(r + c) % 3] if u != 6.0 else (g_woad, g_ruby, g_woad)[(r * 2 + c) % 3]
            panes.append(f'<rect x="{U(u-w/2)+c*w*K/2+1:.1f}" y="{Z(sill)-(r+1)*.22*K+1:.1f}" width="{w*K/2-2:.1f}" height="{.22*K-2:.1f}" fill="{col}"/>')
    panes.append(f'<circle cx="{U(u)}" cy="{Z(sill+1.1)}" r="{w*K*.32:.1f}" fill="{WASH}" stroke="{LEAD}" stroke-width="2"/>')
    sec.append(f'<g clip-path="url(#{cid})">' + ''.join(panes) + '</g>')
    sec.append(f'<path d="M{U(u)},{Z(sill)} L{U(u)},{Z(top+F0)}" stroke="{LEAD}" stroke-width="2"/>')
    sec.append(fracture(f"M{U(u-w/2)-4},{Z(sill)+4} L{U(u+w/2)+4},{Z(sill)+4}", 1.2))
# vault ribs (transverse + wall ribs) and cut vault mass
sec.append(f'<path d="{vault}" fill="none" stroke="{shade(WASH,.4)}" stroke-width="7"/>')
sec.append(f'<path d="{vault}" fill="none" stroke="{WASH}" stroke-width="2" opacity=".6"/>')
sec.append(f'<path d="M{U(IN0)},{Z(SPR)} C{U(IN0+.6)},{Z(CRN-.4)} {U(5.6)},{Z(CRN+.05)} {U(6)},{Z(CRN+.2)} M{U(IN1)},{Z(SPR)} C{U(IN1-.6)},{Z(CRN-.4)} {U(6.4)},{Z(CRN+.05)} {U(6)},{Z(CRN+.2)}" fill="none" stroke="{shade(WASH,.5)}" stroke-width="4" opacity=".7"/>')
sec.append(f'<circle cx="{U(6)}" cy="{Z(CRN+.2)}" r="9" fill="{g_gilt}" stroke="#14120E"/>')
roof = f"M{U(IN0-.9)},{Z(F0)} L{U(IN0-.9)},{Z(5.2)} L{U(IN1+.9)},{Z(5.2)} L{U(IN1+.9)},{Z(F0)} L{U(IN1)},{Z(F0)} L{U(IN1)},{Z(SPR)} C{U(IN1)},{Z(CRN-.3)} {U(6+1.2)},{Z(CRN)} {U(6)},{Z(CRN+.2)} C{U(6-1.2)},{Z(CRN)} {U(IN0)},{Z(CRN-.3)} {U(IN0)},{Z(SPR)} L{U(IN0)},{Z(F0)} Z"
sec.append(f'<path d="{roof}" fill="{p_cut}" stroke="#14120E" stroke-width="1.2"/>')
# sacristy strip to the north (u .6..2.4) with small door
sec.append(f'<rect x="{U(.6)}" y="{Z(3.6)}" width="{1.8*K}" height="{3.3*K}" fill="{shade(LIME,.35)}"/>')
sec.append(f'<rect x="{U(.6)}" y="{Z(3.9)}" width="{1.8*K}" height="{.3*K}" fill="{p_cut}" stroke="#14120E"/>')
sec.append(f'<rect x="{U(.6)-4}" y="{Z(5.2)}" width="{.0001+4}" height="{4.9*K}" fill="{p_cut}"/>')
sec.append(f'<rect x="{U(IN0-.9)}" y="{Z(F0+2.1)}" width="{.9*K}" height="{2.1*K}" fill="#0B0A08" stroke="#14120E"/>')
sec.append(f'<path d="M{U(IN0-.9)},{Z(F0)} L{U(IN0-1.7)},{Z(F0)+6} L{U(IN0-1.7)},{Z(F0+2.1)+6} L{U(IN0-.9)},{Z(F0+2.1)} Z" fill="{g_oak}" stroke="#14120E"/>')
sec.append(f'<rect x="{U(.8)}" y="{Z(F0+.7)}" width="{.9*K}" height="{.7*K}" fill="{g_oak}" stroke="#14120E"/><path d="M{U(.8)},{Z(F0+.55)} h{.9*K}" stroke="{LEAD}" stroke-width="3"/>')
# floor step and altar
sec.append(f'<rect x="{U(4.3)}" y="{Z(F0+.15)}" width="{3.4*K}" height="{.15*K}" fill="{shade(LIME,1.1)}" stroke="#14120E"/>')
alt = (U(5.1), Z(F0 + .15 + 1.0), 1.8 * K, 1.0 * K)
sec.append(f'<rect x="{alt[0]}" y="{alt[1]}" width="{alt[2]}" height="{alt[3]}" fill="{shade(LIME,1.05)}" stroke="#14120E"/>')
sec.append(f'<path d="M{alt[0]},{alt[1]} h{alt[2]} v{.75*K} h{-alt[2]} Z" fill="{RUBY}" stroke="#14120E"/>')
sec.append(f'<path d="M{alt[0]},{alt[1]+8} h{alt[2]}" stroke="{WOAD}" stroke-width="6"/><path d="M{alt[0]},{alt[1]+2} h{alt[2]}" stroke="{WASH}" stroke-width="3"/>')
for i in range(6):
    sec.append(f'<path d="M{alt[0]+10+i*21},{alt[1]+12} l0,{.6*K:.0f}" stroke="{shade(RUBY,.6)}" stroke-width="2" opacity=".6"/>')
# retable = the Gilded Altarpiece (plunder) standing on the mensa
rk = K
rx0, rb = U(6) - .55 * rk, alt[1]
sec.append(f'<path d="M{rx0},{rb} L{rx0},{rb-1.38*rk} L{U(6)},{rb-1.6*rk} L{rx0+1.1*rk},{rb-1.38*rk} L{rx0+1.1*rk},{rb} Z" fill="{g_gilt}" stroke="#14120E" stroke-width="1.5"/>')
sec.append(f'<rect x="{rx0}" y="{rb-.22*rk}" width="{1.1*rk}" height="{.22*rk}" fill="{shade(GILT,.8)}" stroke="#14120E"/>')
sec.append(f'<path d="M{rx0+8},{rb-.3*rk} L{rx0+8},{rb-1.2*rk} Q{U(6)},{rb-1.55*rk} {rx0+1.1*rk-8},{rb-1.2*rk} L{rx0+1.1*rk-8},{rb-.3*rk} Z" fill="none" stroke="{shade(GILT,.5)}" stroke-width="2"/>')
sec.append(f'<path d="M{U(6)-10},{rb-.3*rk} L{U(6)-12},{rb-1.0*rk} Q{U(6)},{rb-1.2*rk} {U(6)+12},{rb-1.0*rk} L{U(6)+10},{rb-.3*rk} Z" fill="{WOAD}"/><circle cx="{U(6)}" cy="{rb-1.08*rk}" r="7" fill="{WASH}"/>')
# candle stands (iron prickets), candles, glow
for u in (4.6, 7.4):
    cx = U(u); base = Z(F0 + .15)
    sec.append(f'<circle cx="{cx}" cy="{base-1.5*K}" r="140" fill="{g_fire}"/>')
    sec.append(f'<path d="M{cx-14},{base} L{cx},{base-18} L{cx+14},{base} Z M{cx-2},{base-18} L{cx-2},{base-1.3*K} L{cx+2},{base-1.3*K} L{cx+2},{base-18} Z" fill="{LEAD}"/>')
    sec.append(f'<path d="M{cx-12},{base-1.3*K} h24 v-4 h-24 Z" fill="{LEAD}"/><rect x="{cx-4}" y="{base-1.3*K-30}" width="8" height="26" fill="{WASH}"/>')
    sec.append(f'<path d="M{cx},{base-1.3*K-44} C{cx-5},{base-1.3*K-36} {cx-4},{base-1.3*K-30} {cx},{base-1.3*K-30} C{cx+4},{base-1.3*K-30} {cx+5},{base-1.3*K-36} {cx},{base-1.3*K-44} Z" fill="{g_candle}"/>')
sec.append(s.flecks(U(IN0), Z(CRN), 5.4 * K, 2 * K, 90, LEAD, .6, 1.8, .45))
sec.append(human(U(3.9), Z(F0), K, label=False, op=.9).replace('fill="#635C4C"', 'fill="#9A9078"'))
sec.append(text(U(3.9), Z(F0) + 13, "1.80 m", 9, "#9A9078", "middle"))
parts += sec
parts.append(sock(U(IN0) - 8, Z(F0 + 3.4), "LANCET ×3, 0.45 × 2.10 →", "end"))
parts.append(sock(U(IN0-.45), Z(F0 + 2.1) - 8, "SACRISTY DOOR 0.90 × 2.10", "end"))
parts.append(text(U(6), Z(F0 + .15 + 1.0) + 18, "", 9, "#9A9078", "middle"))
parts.append(text(U(5.1), Z(0) + 22, "SECTION C–C ACROSS THE NAVE, LOOKING EAST · 1 m = 70 px", 10.5, "#635C4C", "middle", extra=' letter-spacing="2"'))
parts.append(vcallout(U(6) + 20, Z(F0 + 2.2), 330, 282, "Retable = Gilded Altarpiece", "the plunder, 1.10 × 1.60 m"))
parts.append(vcallout(U(6.9) + 12, Z(F0 + 1.1), 555, 282, "Altar, frontal, 1 step", "mensa 1.8 × 0.9 × 1.0 m"))
parts.append(vcallout(U(IN0) + 30, Z(SPR + .9), 100, 282, "Rib vault, 2 bays", "crown 4.60 · spring 2.90"))

# plan
PK = 34; PX, PY = 760, 150
Px = lambda x: PX + x * PK
Py = lambda y: PY + y * PK
pl = [plan_grid(PX, PY, PK)]
wall = f'fill="{p_cut}" stroke="#14120E" stroke-width="1.2"'
pl.append(f'<path d="M{Px(0)},{Py(0)} h{12*PK} v{12*PK} h{-12*PK} Z M{Px(.6)},{Py(.6)} v{10.8*PK} h{10.8*PK} v{-10.8*PK} Z" fill-rule="evenodd" {wall}/>')
pl.append(f'<path d="M{Px(.9)},{Py(2.4)} h{10.2*PK} v{7.2*PK} h{-10.2*PK} Z M{Px(1.8)},{Py(3.3)} v{5.4*PK} h{8.4*PK} v{-5.4*PK} Z" fill-rule="evenodd" {wall}/>')
# openings: W archway into the nave, N sacristy door, S archway (cell), N archway (cell) into the sacristy strip
for (x, y, w, h) in ((.9, 4.7, .9, 2.6), (0, 4.7, .6, 2.6), (4.7, 11.4, 2.6, .6), (4.7, 0, 2.6, .6), (8.2, 2.4, .9, .9), (11.4, 4.7, .6, 2.6)):
    pl.append(f'<rect x="{Px(x)}" y="{Py(y)}" width="{w*PK:.1f}" height="{h*PK:.1f}" fill="#18150F"/>')
# east archway of the cell opens into the passage behind the altar wall, not the nave
pl.append(f'<rect x="{Px(1.8)}" y="{Py(3.3)}" width="{8.4*PK}" height="{5.4*PK}" fill="{shade(WASH,.25)}"/>')
for (a, b) in ((1.8, 6.0), (6.0, 10.2)):
    pl.append(f'<path d="M{Px(a)},{Py(3.3)} L{Px(b)},{Py(8.7)} M{Px(a)},{Py(8.7)} L{Px(b)},{Py(3.3)}" stroke="#9A9078" stroke-dasharray="4 3"/>')
pl.append(f'<path d="M{Px(6)},{Py(3.3)} L{Px(6)},{Py(8.7)}" stroke="#9A9078" stroke-width="2"/>')
pl.append(f'<rect x="{Px(8.9)}" y="{Py(4.4)}" width="{1.3*PK}" height="{3.2*PK}" fill="{shade(LIME,1.1)}" opacity=".5"/>')
pl.append(f'<rect x="{Px(9.3)}" y="{Py(5.1)}" width="{.9*PK}" height="{1.8*PK}" fill="{RUBY}" stroke="#14120E"/>')
pl.append(f'<rect x="{Px(10.0)}" y="{Py(5.45)}" width="{.2*PK}" height="{1.1*PK}" fill="{g_gilt}"/>')
for y in (4.6, 7.4):
    pl.append(f'<circle cx="{Px(9.5)}" cy="{Py(y)}" r="5" fill="{LEAD}"/><circle cx="{Px(9.5)}" cy="{Py(y)}" r="40" fill="{g_fire}"/>')
for y in (5.1, 6.0, 6.9):
    pl.append(f'<rect x="{Px(10.2)}" y="{Py(y-.225)}" width="{.9*PK}" height="{.45*PK}" fill="{g_woad}" stroke="#14120E"/>')
for x in (4.0, 7.0):
    pl.append(f'<rect x="{Px(x-.225)}" y="{Py(8.7)}" width="{.45*PK}" height="{.9*PK}" fill="{g_ruby}" stroke="#14120E"/>')
for i in range(4):
    pl.append(f'<rect x="{Px(2.6+i*1.4)}" y="{Py(4.0)}" width="{.4*PK}" height="{1.6*PK}" fill="{g_oak}"/><rect x="{Px(2.6+i*1.4)}" y="{Py(6.4)}" width="{.4*PK}" height="{1.6*PK}" fill="{g_oak}"/>')
parts.append(''.join(pl))
parts.append(sock(Px(1.35), Py(6), "DOOR W 2.60 × 3.31", "start"))
parts.append(sock(Px(10.65), Py(6.9), "LANCET ×3", "end"))
parts.append(sock(Px(7), Py(9.15), "LANCET ×2 (S)", "start"))
parts.append(sock(Px(8.65), Py(2.85), "SACRISTY", "end"))
parts.append(sock(Px(6), Py(.3), "DOOR N", "start"))
parts.append(sock(Px(6), Py(11.7), "DOOR S", "start"))
parts.append(sock(Px(11.7), Py(4.4), "DOOR E", "end"))
parts.append(text(Px(6), Py(12) + 34, "PLAN · nave 8.4 × 5.4 m, two vault bays", 9.5, "#635C4C", "middle", extra=' letter-spacing="1"'))
notes = ["The richest room in the Age: altarpiece,", "plate, reliquary — and five windows that", "shatter at a thrown ewer and wake the", "whole house (glass is the loudest thing", "in the building).", "", "IGNIS: the frontal and the benches burn;", "the vault does not."]
for i, n in enumerate(notes):
    parts.append(text(40, 150 + i * 13.5, n, 10.5, "#9A9078"))
head = frame_open(s, f"{AGE_NAME} · STRUCTURE · KEEP", "The Chapel", "12 × 12 m cell · H 4.60 m · ≤ 25k tris", "section 1 m = 70 px · plan 1 m = 34 px", glow_cx="35%", glow_cy="65%")
write(s, head, parts, palette([("limestone", LIME), ("limewash", WASH), ("ruby glass", RUBY), ("woad glass", WOAD), ("lead came", LEAD), ("oak", OAK), ("gilt (plunder)", GILT)]))
