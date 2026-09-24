from lib import *

s = Sheet("gatehouse-portcullis")
LIME = "#A89F8A"; CORE = "#7D7566"; OAK = "#5E4630"; IRON = "#3A3632"; SOOT = "#1E1B17"; FIRE = "#C4542E"
p_ash = ashlar(s, "ash", 30, 15, LIME)
p_ash_d = ashlar(s, "ashd", 30, 15, shade(LIME, .62))
p_ash_s = ashlar(s, "ashs", 13, 6.6, LIME)
p_cut = hatch(s, "cut", CORE, 6, 1)
g_oak = s.lg(shade(OAK, 1.25), shade(OAK, .6), 0, 0, 1, 0)
g_fire = s.rg(FIRE, FIRE, .55, 0, .5, .5, .5)
g_fire2 = s.rg("#E8C080", FIRE, .9, 0, .5, .5, .5)
g_vault = s.lg("#14120E", shade(LIME, .5), 0, 0, 0, 1)
g_passage = s.lg(shade(LIME, .42), shade(LIME, .7), 0, 0, 0, 1)

K = 50; X0 = 110; G = 690
X = lambda y: X0 + y * K
Z = lambda z: G - z * K
parts = [metre_ladder(70, K, G, 5.5, 1)]
sec = []
# ground + slab (cut)
sec.append(f'<rect x="{X(0)}" y="{Z(.3)}" width="{12*K}" height="{.3*K}" fill="{p_cut}" stroke="#14120E" stroke-width="1"/>')
# far wall of passage (west wall, seen beyond the cut) y 0..10, floor .3 to vault
sec.append(f'<rect x="{X(0)}" y="{Z(4.3)}" width="{10*K}" height="{4.0*K}" fill="{p_ash_d}"/>')
sec.append(f'<rect x="{X(0)}" y="{Z(4.3)}" width="{10*K}" height="{4.0*K}" fill="{g_passage}" opacity=".55"/>')
# pointed barrel vault: springing 2.6 above floor (z 2.9), crown 3.31 above floor (z 3.61); the vault mass is cut (hatched)
sec.append(f'<path d="M{X(0)},{Z(3.61)} L{X(10)},{Z(3.61)} L{X(10)},{Z(4.3)} L{X(0)},{Z(4.3)} Z" fill="{p_cut}" stroke="#14120E" stroke-width="1.2"/>')
sec.append(f'<path d="M{X(0)},{Z(2.9)} L{X(10)},{Z(2.9)} L{X(10)},{Z(3.61)} L{X(0)},{Z(3.61)} Z" fill="{g_vault}" opacity=".85"/>')
for y in [i * .6 for i in range(1, 17)]:
    sec.append(f'<path d="M{X(y)},{Z(2.9)} L{X(y)},{Z(3.61)}" stroke="{shade(LIME,.35)}" stroke-width="1"/>')
# murder holes through the vault
for y in (3.0, 4.2, 5.4):
    sec.append(f'<rect x="{X(y-.15)}" y="{Z(4.3)}" width="{.3*K}" height="{.69*K}" fill="#0B0A08" stroke="{FIRE}" stroke-width=".8" stroke-opacity=".0"/>')
# portcullis slot through the vault at y 1.2, chamber on the deck with windlass
sec.append(f'<rect x="{X(1.1)}" y="{Z(4.3)}" width="{.2*K}" height="{.69*K}" fill="#0B0A08"/>')
# portcullis: oak lattice, iron-shod; lowered to 1.2 m above floor (half-dropped)
pb, pt = 1.5, 5.0
sec.append(f'<rect x="{X(1.12)}" y="{Z(pt)}" width="{.16*K}" height="{(pt-pb)*K}" fill="{g_oak}" stroke="#14120E" stroke-width="1"/>')
for zz in [pb + i * .35 for i in range(11)]:
    sec.append(f'<rect x="{X(1.1)}" y="{Z(zz)-2}" width="{.2*K}" height="4" fill="{IRON}"/>')
for i in range(3):
    sec.append(f'<path d="M{X(1.14)+i*2.5:.1f},{Z(pb)} l1.2,9 l1.2,-9" fill="{IRON}"/>')
# deck + parapet + merlons (cut end wall at north y 0 and wall-walk beyond)
sec.append(f'<rect x="{X(0)}" y="{Z(5.5)}" width="{.6*K}" height="{.9*K}" fill="{p_cut}" stroke="#14120E" stroke-width="1"/>')
for y in (3.4, 5.6, 7.8):
    sec.append(f'<rect x="{X(y)}" y="{Z(5.5)}" width="{1.2*K}" height="{.6*K}" fill="{p_ash}" stroke="{shade(LIME,.5)}" stroke-width="1"/>')
sec.append(f'<rect x="{X(0)}" y="{Z(4.6)}" width="{10*K}" height="{.3*K}" fill="{p_ash}" stroke="{shade(LIME,.5)}" stroke-width="1"/>')
# windlass over the slot
wx, wz = X(2.0), Z(4.75)
sec.append(f'<rect x="{wx-22}" y="{wz-18}" width="44" height="18" fill="{g_oak}" stroke="#14120E" stroke-width="1"/><circle cx="{wx}" cy="{wz-26}" r="12" fill="none" stroke="{OAK}" stroke-width="4"/>'
           f'<path d="M{wx-12},{wz-26} L{wx+12},{wz-26} M{wx},{wz-38} L{wx},{wz-14}" stroke="{OAK}" stroke-width="3"/><path d="M{X(1.2)},{Z(4.3)} L{wx-8},{wz-26}" stroke="#9A9078" stroke-width="1.2"/>')
# outer arch jamb (north, y 0) and inner arch (y 10) cut
sec.append(f'<rect x="{X(0)}" y="{Z(3.61)}" width="{.6*K}" height="{3.31*K}" fill="none" stroke="#14120E" stroke-width="1.2"/>')
# arrow loop in the far passage wall (from the west guard room)
sec.append(f'<rect x="{X(2.35)}" y="{Z(2.2)}" width="7" height="{1.1*K}" fill="#0B0A08"/><rect x="{X(2.35)-6}" y="{Z(1.75)}" width="19" height="6" fill="#0B0A08"/>')
# doors at y 7: two oak leaves (one shown ajar, seen edge-on as the cut leaf) + drawbar slot
sec.append(f'<rect x="{X(6.9)}" y="{Z(3.61)}" width="{.12*K}" height="{3.31*K}" fill="{g_oak}" stroke="#14120E" stroke-width="1"/>')
sec.append(f'<path d="M{X(7.02)},{Z(.3)} L{X(8.2)},{Z(.3)} L{X(8.2)},{Z(3.2)} L{X(7.02)},{Z(3.61)} Z" fill="{g_oak}" stroke="#14120E" stroke-width="1" opacity=".9"/>')
for zz in (.9, 1.8, 2.7):
    sec.append(f'<path d="M{X(7.05)},{Z(zz)} L{X(8.15)},{Z(zz)+2}" stroke="{IRON}" stroke-width="3"/>')
sec.append(f'<rect x="{X(6.5)}" y="{Z(1.65)}" width="{.35*K}" height="10" fill="#0B0A08" stroke="{shade(LIME,.4)}"/>')
sec.append(f'<rect x="{X(4.0)}" y="{Z(1.62)}" width="{2.5*K}" height="7" fill="{g_oak}" stroke="#14120E" stroke-width=".8"/>')
# torch sconce + firelight
tx, tz = X(5.6), Z(2.3)
sec.append(f'<circle cx="{tx}" cy="{tz}" r="150" fill="{g_fire}"/>')
sec.append(f'<path d="M{tx-3},{tz+22} L{tx+3},{tz+22} L{tx+5},{tz} L{tx-5},{tz} Z" fill="{IRON}"/><path d="M{tx},{tz-18} C{tx-8},{tz-8} {tx-5},{tz} {tx},{tz} C{tx+5},{tz} {tx+8},{tz-8} {tx},{tz-18} Z" fill="{g_fire2}"/>')
sec.append(f'<path d="M{tx-20},{Z(3.61)} C{tx-10},{tz-40} {tx+10},{tz-40} {tx+20},{Z(3.61)}" fill="{SOOT}" opacity=".55"/>')
# bailey side open beyond y 10: ground
sec.append(f'<path d="M{X(10)},{Z(4.3)} L{X(10)},{Z(.3)}" stroke="#14120E" stroke-width="1.5"/>')
sec.append(f'<rect x="{X(10)}" y="{Z(4.3)}" width="{.6*K}" height="{.69*K}" fill="{p_cut}" stroke="#14120E"/>')
sec.append(s.flecks(X(0), Z(4.3), 10 * K, 4 * K, 120, SOOT, .6, 1.8, .35))
sec.append(human(X(8.9), Z(.3), K, label=False, op=.9).replace('fill="#635C4C"', 'fill="#9A9078"'))
sec.append(text(X(8.9), Z(.3) + 12, "1.80 m", 9, "#9A9078", "middle"))
parts += sec
# section callouts / socket labels
parts.append(sock(X(1.2), Z(4.3) - 60, "PORTCULLIS SLOT", "start"))
parts.append(sock(X(3.0) + 8, Z(4.3) + 12, "MURDER-HOLE ×3", "start"))
parts.append(sock(X(2.35) + 3, Z(2.2) - 8, "ARROW-LOOP", "start"))
parts.append(sock(X(6.95), Z(3.61) - 10, "DOOR 2.60 × 3.31", "end"))
parts.append(sock(X(0.3), Z(1.6), "DOOR (OUTER)", "start"))
parts.append(sock(X(5.25), Z(1.62) + 14, "DRAWBAR", "start"))
parts.append(text(X(6), Z(0) + 20, "LONG SECTION A–A, LOOKING WEST · 1 m = 50 px", 10.5, "#635C4C", "middle", extra=' letter-spacing="2"'))
parts.append(text(X(0), Z(0) + 20, "N (outside)", 9.5, "#9A9078"))
parts.append(text(X(12), Z(0) + 20, "S (bailey)", 9.5, "#9A9078", "end"))

# north elevation inset, 1 m = 20 px
k = 20; ex0 = 330; eg = 345
E = lambda x: ex0 + x * k
EZ = lambda z: eg - z * k
el = [f'<line x1="{E(0)}" y1="{eg}" x2="{E(12)}" y2="{eg}" stroke="#635C4C"/>']
for (a, b) in ((1.5, 4.7), (7.3, 10.5)):
    el.append(f'<path d="M{E(a)},{eg} L{E(a)},{EZ(4.9)} L{E(b)},{EZ(4.9)} L{E(b)},{eg} Z" fill="{p_ash_s}" stroke="#14120E"/>')
    el.append(f'<path d="M{E(a)},{eg} L{E(a)},{EZ(4.9)} L{E(a+1)},{EZ(4.9)} L{E(a+.6)},{eg} Z" fill="{SOOT}" opacity=".25"/>')
    for i in range(4):
        el.append(f'<rect x="{E(a + .15 + i * .82):.1f}" y="{EZ(5.5)}" width="{.5*k}" height="{.6*k}" fill="{p_ash_s}" stroke="#14120E"/>')
    el.append(f'<rect x="{E((a+b)/2)-2}" y="{EZ(3.2)}" width="4" height="{1.1*k}" fill="#0B0A08"/>')
el.append(f'<rect x="{E(0)}" y="{EZ(4.9)}" width="{1.5*k}" height="{4.9*k}" fill="{p_ash_s}" stroke="#14120E"/><rect x="{E(10.5)}" y="{EZ(4.9)}" width="{1.5*k}" height="{4.9*k}" fill="{p_ash_s}" stroke="#14120E"/>')
el.append(f'<rect x="{E(4.7)}" y="{EZ(4.9)}" width="{2.6*k}" height="{4.9*k}" fill="{p_ash_s}" stroke="#14120E"/>')
el.append(f'<path d="M{E(4.7)},{eg} L{E(4.7)},{EZ(2.6)} Q{E(4.7)},{EZ(3.31)} {E(6)},{EZ(3.61)} Q{E(7.3)},{EZ(3.31)} {E(7.3)},{EZ(2.6)} L{E(7.3)},{eg} Z" fill="#0B0A08" stroke="#14120E"/>')
for i in range(6):
    el.append(f'<line x1="{E(4.85+i*.46):.1f}" y1="{EZ(3.4)}" x2="{E(4.85+i*.46):.1f}" y2="{EZ(1.5)}" stroke="{OAK}" stroke-width="2.5"/>')
for zz in (1.8, 2.3, 2.8, 3.3):
    el.append(f'<line x1="{E(4.8)}" y1="{EZ(zz)}" x2="{E(7.2)}" y2="{EZ(zz)}" stroke="{OAK}" stroke-width="2"/>')
el.append(human(E(3.5) - 16, eg, k, label=False, op=.5))
parts.append(''.join(el))
parts.append(text(E(6), eg + 18, "NORTH ELEVATION · 1 m = 20 px · merlons at 5.20 m above floor", 9.5, "#635C4C", "middle", extra=' letter-spacing="1"'))

# plan, 1 m = 34 px
PK = 34; PX, PY = 750, 150
Px = lambda x: PX + x * PK
Py = lambda y: PY + y * PK
pl = [plan_grid(PX, PY, PK)]
wall = f'fill="{p_cut}" stroke="#14120E" stroke-width="1.2"'
# tower masses with D-fronts (north at top)
for (a, b) in ((1.5, 4.7), (7.3, 10.5)):
    pl.append(f'<path d="M{Px(a)},{Py(10)} L{Px(a)},{Py(1.6)} C{Px(a)},{Py(-.1)} {Px(b)},{Py(-.1)} {Px(b)},{Py(1.6)} L{Px(b)},{Py(10)} Z" {wall}/>')
    pl.append(f'<rect x="{Px(a+.8)}" y="{Py(2.2)}" width="{(b-a-1.6)*PK:.1f}" height="{3.2*PK}" fill="#18150F" stroke="#14120E"/>')
pl.append(f'<rect x="{Px(0)}" y="{Py(3)}" width="{1.5*PK}" height="{2.4*PK}" {wall}/><rect x="{Px(10.5)}" y="{Py(3)}" width="{1.5*PK}" height="{2.4*PK}" {wall}/>')
pl.append(f'<rect x="{Px(4.7)}" y="{Py(0)}" width="{2.6*PK}" height="{10*PK}" fill="{shade(LIME,.35)}"/>')
pl.append(f'<rect x="{Px(4.7)}" y="{Py(1.12)}" width="{2.6*PK}" height="{.16*PK}" fill="{OAK}" stroke="{IRON}"/>')
for y in (3.0, 4.2, 5.4):
    pl.append(f'<rect x="{Px(5.85)}" y="{Py(y-.15)}" width="{.3*PK}" height="{.3*PK}" fill="none" stroke="#DCD2BA" stroke-dasharray="2 2"/>')
pl.append(f'<path d="M{Px(4.7)},{Py(7)} L{Px(5.9)},{Py(7.9)} M{Px(7.3)},{Py(7)} L{Px(6.1)},{Py(7.9)}" stroke="{OAK}" stroke-width="4"/>')
pl.append(f'<rect x="{Px(4.2)}" y="{Py(6.8)}" width="{3.6*PK}" height="5" fill="{OAK}" opacity=".7"/>')
# newel stair in east tower mass
pl.append(f'<circle cx="{Px(9.2)}" cy="{Py(8.3)}" r="{1.1*PK}" fill="#18150F" stroke="#14120E"/>')
for a in range(0, 360, 30):
    pl.append(f'<line x1="{Px(9.2)}" y1="{Py(8.3)}" x2="{Px(9.2)+1.1*PK*math.cos(math.radians(a)):.1f}" y2="{Py(8.3)+1.1*PK*math.sin(math.radians(a)):.1f}" stroke="#635C4C"/>')
pl.append(f'<circle cx="{Px(9.2)}" cy="{Py(8.3)}" r="5" fill="{p_cut}" stroke="#14120E"/>')
# arrow loops
for (x, y, dx, dy) in ((3.1, .2, 0, -1), (8.9, .2, 0, -1), (4.7, 3.3, 1, 0), (7.3, 3.3, -1, 0)):
    pl.append(f'<path d="M{Px(x)},{Py(y)} l{dx*10-dy*4},{dy*10-dx*4} l{dy*8},{dx*8} Z" fill="#0B0A08" stroke="#DCD2BA" stroke-width=".6"/>')
pl.append(f'<circle cx="{Px(6)}" cy="{Py(6)}" r="60" fill="{g_fire}"/>')
parts.append(''.join(pl))
parts.append(sock(Px(6), Py(0) - 2, "DOOR N", "start"))
parts.append(sock(Px(6), Py(10), "DOOR S", "start"))
parts.append(sock(Px(3.1), Py(.2), "ARROW-LOOP ×4", "end"))
parts.append(sock(Px(9.2), Py(8.3) - 44, "STAIR ↑ deck", "end"))
parts.append(sock(Px(6), Py(4.2), "MURDER-HOLE", "start"))
parts.append(sock(Px(5), Py(1.2), "PORTCULLIS", "end"))
parts.append(sock(Px(0), Py(4.2), "WALL-WALK W", "start"))
parts.append(sock(Px(12), Py(4.2) + 16, "WALL-WALK E", "end"))
parts.append(text(Px(6), Py(12) + 34, "PLAN · north = outside", 9.5, "#635C4C", "middle", extra=' letter-spacing="1"'))

notes = ["TONITRUS on the windlass drops the", "portcullis: 0.9 t of oak and iron,", "lethal to anything beneath it.", "", "Murder-holes: AURUM VOCO shows", "the gold above; boiling oil comes", "down them once the house is up.", "", "Drawbar: 3 m oak beam, LEVO lifts", "it, FRANGO splits it (loud)."]
for i, n in enumerate(notes):
    parts.append(text(40, 160 + i * 15, n, 10.5, "#9A9078"))
head = frame_open(s, f"{AGE_NAME} · STRUCTURE · CURTAINWALL", "The Gatehouse", "12 × 12 m cell · H 5.20 m · ≤ 25k tris", "section 1 m = 50 px · plan 1 m = 34 px", glow_cx="40%", glow_cy="75%")
write(s, head, parts, palette([("limestone ashlar", LIME), ("rubble core", CORE), ("oak", OAK), ("wrought iron", IRON), ("soot", SOOT), ("torch fire", FIRE)]))
