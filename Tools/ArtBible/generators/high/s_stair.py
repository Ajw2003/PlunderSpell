from lib import *

s = Sheet("spiral-stair")
LIME = "#A89F8A"; TREAD = "#B5AC96"; OAK = "#5E4630"; IRON = "#3A3632"; ROPE = "#C4B89C"; SOOT = "#1E1B17"; FIRE = "#C4542E"
K = 80; X0 = 100; G = 690
U = lambda u: X0 + u * K
Z = lambda z: G - z * K
p_ash = ashlar(s, "ash", 36, 18, LIME, X0, 0)
p_ashd = ashlar(s, "ashd", 36, 18, shade(LIME, .6), X0, 0)
p_cut = hatch(s, "cut", shade(LIME, .8), 7, 1)
g_fire = s.rg(FIRE, FIRE, .5, 0, .5, .5, .5)
g_fire2 = s.rg("#E8C080", FIRE, .9, 0, .5, .5, .5)
g_oak = s.lg(shade(OAK, 1.25), shade(OAK, .6), 0, 0, 1, 0)
g_tread = s.lg(shade(TREAD, 1.2), shade(TREAD, .55), 0, 0, 0, 1)

C, R, RN = 5.0, 1.2, .125        # drum centre (section u), inner radius, newel radius
RISE, STEP = 4.9 / 26, 15        # 26 risers per storey, 15 degrees each
parts = [metre_ladder(60, K, G, 5.5, 1)]
sec = []
# lobby (u 0..3): back wall ashlar, floor slab; drum walls cut; interior back wall darker
sec.append(f'<rect x="{U(0)}" y="{Z(4.9)}" width="{3*K}" height="{4.6*K}" fill="{p_ash}"/>')
sec.append(f'<rect x="{U(0)}" y="{Z(4.9)}" width="{3*K}" height="{4.6*K}" fill="#14120E" opacity=".35"/>')
sec.append(f'<rect x="{U(3.8)}" y="{Z(5.2)}" width="{2.4*K}" height="{4.9*K}" fill="{p_ashd}"/>')
sec.append(f'<rect x="{U(0)}" y="{Z(.3)}" width="{7*K}" height="{.3*K}" fill="{p_cut}" stroke="#14120E"/>')
sec.append(f'<rect x="{U(0)}" y="{Z(5.2)}" width="{3.8*K}" height="{.3*K}" fill="{p_cut}" stroke="#14120E"/>')
# back-half steps (seen beyond the cut): riser faces, clockwise ascent
steps = []
for i in range(27):
    th = math.radians(-STEP * i)
    h = .3 + RISE * i
    if math.sin(th) > 0.01:  # north half = beyond the cut plane
        x1 = U(C + RN * math.cos(th)); x2 = U(C + R * math.cos(th))
        lo, hi = min(x1, x2), max(x1, x2)
        shade_f = .6 + .4 * math.sin(th)
        steps.append(f'<rect x="{lo:.1f}" y="{Z(h):.1f}" width="{max(hi-lo,2):.1f}" height="{RISE*K:.1f}" fill="{shade(TREAD, shade_f)}" stroke="#14120E" stroke-width=".8"/>')
        steps.append(f'<path d="M{lo:.1f},{Z(h):.1f} L{hi:.1f},{Z(h):.1f}" stroke="{shade(TREAD,1.4)}" stroke-width="1.2"/>')
        # helical soffit underside line
        steps.append(f'<path d="M{lo:.1f},{Z(h)+RISE*K:.1f} L{hi:.1f},{Z(h-.3)+RISE*K:.1f}" stroke="{SOOT}" stroke-width="1" opacity=".5"/>')
sec.append(''.join(steps))
# newel post
sec.append(f'<rect x="{U(C-RN)}" y="{Z(5.2)}" width="{2*RN*K}" height="{4.9*K}" fill="{g_tread}" stroke="#14120E"/>')
for zz in [.3 + RISE * i for i in range(0, 27, 2)]:
    sec.append(f'<path d="M{U(C-RN)},{Z(zz)} h{2*RN*K}" stroke="{shade(TREAD,.5)}" stroke-width=".8"/>')
# cut steps in the section plane: east (u C+..R) at i=0 and 24, west (u C-R..) at i=12
for i, side in ((1, 1), (24, 1), (12, -1), (13, -1), (25, 1)):
    h = .3 + RISE * i
    a, b = (U(C + RN), U(C + R)) if side > 0 else (U(C - R), U(C - RN))
    sec.append(f'<path d="M{a:.1f},{Z(h):.1f} L{b:.1f},{Z(h):.1f} L{b:.1f},{Z(h-.34):.1f} L{a:.1f},{Z(h-.28):.1f} Z" fill="{p_cut}" stroke="#14120E"/>')
# drum walls (cut)
sec.append(f'<rect x="{U(3.0)}" y="{Z(5.5)}" width="{.8*K}" height="{5.2*K}" fill="{p_cut}" stroke="#14120E"/>')
sec.append(f'<rect x="{U(6.2)}" y="{Z(5.5)}" width="{.8*K}" height="{5.2*K}" fill="{p_cut}" stroke="#14120E"/>')
# doorway from the lobby into the drum at floor level (lower socket) and at the top (upper socket)
sec.append(f'<path d="M{U(3.0)},{Z(.3)} L{U(3.0)},{Z(2.2)} Q{U(3.4)},{Z(2.5)} {U(3.8)},{Z(2.2)} L{U(3.8)},{Z(.3)} Z" fill="#0B0A08" stroke="#14120E"/>')
sec.append(f'<rect x="{U(2.95)}" y="{Z(2.25)}" width="{.1*K}" height="{1.95*K}" fill="{g_oak}" stroke="#14120E"/>')
sec.append(f'<path d="M{U(3.0)},{Z(5.2)} L{U(3.0)},{Z(5.5)}" stroke="#14120E"/>')
sec.append(f'<path d="M{U(3.0)},{Z(5.2)} L{U(3.8)},{Z(5.2)}" stroke="#DCD2BA" stroke-dasharray="4 3"/>')
# slit windows in the east drum wall (splayed)
for zz in (1.4, 3.6):
    sec.append(f'<path d="M{U(6.2)},{Z(zz+.6)} L{U(7.0)},{Z(zz+.35)} L{U(7.0)},{Z(zz-.35)} L{U(6.2)},{Z(zz-.6)} Z" fill="#0B0A08" stroke="#14120E"/>')
# rope handrail on the outer wall, iron eyes
pts = []
for i in range(0, 27):
    th = math.radians(-STEP * i)
    if math.sin(th) > 0.01 and .3 + RISE * i + .9 < 5.1:
        pts.append((U(C + (R - .05) * math.cos(th)), Z(.3 + RISE * i + .9)))
for j in range(len(pts) - 1):
    if abs(pts[j + 1][0] - pts[j][0]) < 40:
        sec.append(f'<path d="M{pts[j][0]:.1f},{pts[j][1]:.1f} L{pts[j+1][0]:.1f},{pts[j+1][1]:.1f}" stroke="{ROPE}" stroke-width="2.4"/>')
for (x, y) in pts[::3]:
    sec.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="3" fill="none" stroke="{IRON}" stroke-width="1.5"/>')
# rushlight niche + glow
nx, nz = U(3.95), Z(2.9)
sec.append(f'<circle cx="{nx+40}" cy="{nz}" r="170" fill="{g_fire}"/>')
sec.append(f'<path d="M{nx-8},{nz+10} L{nx+8},{nz+10} L{nx+8},{nz-16} Q{nx},{nz-26} {nx-8},{nz-16} Z" fill="#0B0A08"/><path d="M{nx},{nz-10} C{nx-5},{nz-3} {nx-3},{nz+3} {nx},{nz+3} C{nx+3},{nz+3} {nx+5},{nz-3} {nx},{nz-10} Z" fill="{g_fire2}"/>')
sec.append(f'<path d="M{nx-12},{nz-30} C{nx},{nz-80} {nx+30},{nz-120} {nx+10},{nz-160}" fill="none" stroke="{SOOT}" stroke-width="16" opacity=".35"/>')
sec.append(s.flecks(U(3.8), Z(5.2), 2.4 * K, 4.9 * K, 90, SOOT, .6, 1.8, .4))
sec.append(human(U(1.5), Z(.3), K, label=False, op=.9).replace('fill="#635C4C"', 'fill="#9A9078"'))
sec.append(text(U(1.5), Z(.3) + 14, "1.80 m", 9, "#9A9078", "middle"))
parts += sec
parts.append(sock(U(3.4), Z(.3) - 8, "STAIR-DOWN", "start"))
parts.append(sock(U(3.4), Z(5.2) - 10, "STAIR-UP (next floor +4.90)", "start"))
parts.append(sock(U(7.0), Z(3.6), "SLIT WINDOW 0.10 × 0.90", "end"))
parts.append(sock(U(3.0), Z(1.2), "DOOR 0.90 × 2.10", "end"))
parts.append(text(U(3.5), Z(0) + 22, "SECTION B–B THROUGH THE NEWEL, LOOKING NORTH · 1 m = 80 px", 10.5, "#635C4C", "middle", extra=' letter-spacing="2"'))
parts.append(text(U(C), Z(5.5) - 12, "26 risers × 0.188 m · 15° per tread · inner Ø 2.40 m", 9.5, "#9A9078", "middle"))

# plan
PK = 34; PX, PY = 760, 150
Px = lambda x: PX + x * PK
Py = lambda y: PY + y * PK
pl = [plan_grid(PX, PY, PK)]
wall = f'fill="{p_cut}" stroke="#14120E" stroke-width="1.2"'
# perimeter walls 0.8 with archway gaps 2.6 on W, S; N/E blocked by drum on that corner side (E archway kept)
pl.append(f'<path d="M{Px(0)},{Py(0)} h{12*PK} v{12*PK} h{-12*PK} Z M{Px(.8)},{Py(.8)} v{10.4*PK} h{10.4*PK} v{-10.4*PK} Z" fill-rule="evenodd" {wall}/>')
for (x, y, w, h) in ((4.7, 11.2, 2.6, .8), (0, 4.7, .8, 2.6), (11.2, 4.7, .8, 2.6), (4.7, 0, 2.6, .8)):
    pl.append(f'<rect x="{Px(x)}" y="{Py(y)}" width="{w*PK:.1f}" height="{h*PK:.1f}" fill="#18150F"/>')
cx, cy = 8.6, 3.4
pl.append(f'<circle cx="{Px(cx)}" cy="{Py(cy)}" r="{2.0*PK}" {wall}/>')
pl.append(f'<circle cx="{Px(cx)}" cy="{Py(cy)}" r="{1.2*PK}" fill="#211C15" stroke="#14120E"/>')
for i in range(24):
    th = math.radians(-STEP * i + 225)
    col = shade(TREAD, .4 + .6 * i / 24)
    pl.append(f'<line x1="{Px(cx)+RN*PK*math.cos(th):.1f}" y1="{Py(cy)-RN*PK*math.sin(th):.1f}" x2="{Px(cx)+1.2*PK*math.cos(th):.1f}" y2="{Py(cy)-1.2*PK*math.sin(th):.1f}" stroke="{col}" stroke-width="1.2"/>')
pl.append(f'<circle cx="{Px(cx)}" cy="{Py(cy)}" r="{RN*PK+1:.1f}" fill="{TREAD}" stroke="#14120E"/>')
# up arrow, clockwise
arc = f"M{Px(cx)+.75*PK*math.cos(math.radians(225)):.1f},{Py(cy)-.75*PK*math.sin(math.radians(225)):.1f} A{.75*PK},{.75*PK} 0 1 1 {Px(cx)+.75*PK*math.cos(math.radians(-60)):.1f},{Py(cy)-.75*PK*math.sin(math.radians(-60)):.1f}"
pl.append(f'<path d="{arc}" fill="none" stroke="#DCD2BA" stroke-width="1.4"/>')
ex, ey = Px(cx) + .75 * PK * math.cos(math.radians(-60)), Py(cy) - .75 * PK * math.sin(math.radians(-60))
pl.append(f'<path d="M{ex:.1f},{ey:.1f} l-8,-2 l3,7 Z" fill="#DCD2BA"/>')
# doorway into the drum (SW)
th = math.radians(225)
dx, dy = Px(cx) + 1.6 * PK * math.cos(th), Py(cy) - 1.6 * PK * math.sin(th)
pl.append(f'<rect x="{dx-12:.1f}" y="{dy-12:.1f}" width="24" height="24" fill="#211C15" transform="rotate(45 {dx:.1f} {dy:.1f})"/>')
# slits
for a in (20, -60, 110):
    t2 = math.radians(a)
    pl.append(f'<line x1="{Px(cx)+1.2*PK*math.cos(t2):.1f}" y1="{Py(cy)-1.2*PK*math.sin(t2):.1f}" x2="{Px(cx)+2.0*PK*math.cos(t2):.1f}" y2="{Py(cy)-2.0*PK*math.sin(t2):.1f}" stroke="#0B0A08" stroke-width="4"/>')
# defender vs attacker diagram
pl.append(f'<circle cx="{Px(cx)}" cy="{Py(cy)}" r="130" fill="{g_fire}" opacity=".5"/>')
parts.append(''.join(pl))
parts.append(sock(Px(cx) + 2.0 * PK * math.cos(math.radians(20)), Py(cy) - 2.0 * PK * math.sin(math.radians(20)), "SLIT ×3", "start"))
parts.append(sock(dx, dy + 8, "DOOR 0.90 (STAIR)", "end"))
parts.append(sock(Px(cx), Py(cy) + 12, "UP ↻", "start"))
parts.append(sock(Px(6), Py(12) - 4, "DOOR S 2.60 × 3.31", "start"))
parts.append(sock(Px(0) + 4, Py(6), "DOOR W", "start"))
parts.append(sock(Px(12) - 4, Py(6.9), "DOOR E", "end"))
parts.append(sock(Px(4.9), Py(0) + 4, "DOOR N", "end"))
parts.append(text(Px(6), Py(12) + 34, "PLAN · the drum sits in the NE corner", 9.5, "#635C4C", "middle", extra=' letter-spacing="1"'))
notes = ["Clockwise going up: a defender coming", "down has his sword arm on the open", "outer side; you, climbing, have yours", "against the newel.", "",
         "Tread 1.20 m long, 0.19 m rise;", "0.35 m at the wall, 0.03 at the newel.", "Dropped plunder rolls to the bottom:", "the altarpiece arrives worth 0."]
for i, n in enumerate(notes):
    parts.append(text(40, 150 + i * 13.5, n, 10.5, "#9A9078"))
head = frame_open(s, f"{AGE_NAME} · STRUCTURE · KEEP", "The Newel Stair", "12 × 12 m cell · H 4.60 m (+4.90 per storey) · ≤ 25k tris", "section 1 m = 80 px · plan 1 m = 34 px", glow_cx="40%", glow_cy="60%")
write(s, head, parts, palette([("limestone ashlar", LIME), ("worn tread", TREAD), ("oak", OAK), ("iron", IRON), ("rope rail", ROPE), ("soot", SOOT)]))
