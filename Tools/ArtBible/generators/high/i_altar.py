from lib import *

s = Sheet("gilded-altarpiece")
GILT = "#C9A227"; WOAD = "#3E5470"; KER = "#7E2A26"; GESSO = "#DCD2BA"; OAK = "#5E4630"; GRIS = "#8A8274"
g_gilt = s.lg(shade(GILT, 1.35), shade(GILT, .55), 0, 0, 1, 1, mid=GILT)
g_giltv = s.lg(shade(GILT, 1.25), shade(GILT, .6), 0, 0, 0, 1, mid=GILT)
g_woad = s.lg(shade(WOAD, 1.3), shade(WOAD, .6), 0, 0, 1, 1)
g_ker = s.lg(shade(KER, 1.4), shade(KER, .6), 0, 0, 1, 1)
g_oak = s.lg(shade(OAK, 1.2), shade(OAK, .6), 0, 0, 1, 1)
g_gris = s.lg(shade(GRIS, 1.3), shade(GRIS, .6), 0, 0, 1, 1)
g_glow = s.rg(GILT, GILT, .3, 0, .5, .5, .5)
g_candle = s.rg("#2A2012", "#14120E", .0, 0)


def punch_halo(cx, cy, r):
    o = [f'<circle cx="{cx}" cy="{cy}" r="{r}" fill="{shade(GILT,1.25)}" stroke="{shade(GILT,.5)}" stroke-width="3"/>']
    for a in range(0, 360, 15):
        o.append(f'<circle cx="{cx + (r-8)*math.cos(math.radians(a)):.1f}" cy="{cy + (r-8)*math.sin(math.radians(a)):.1f}" r="3" fill="{shade(GILT,.55)}"/>')
    return ''.join(o)


def figure(cx, top, h, mantle, robe, seated=False):
    """A stylised painted saint: head, halo, mantle; top = crown of head, h = figure height (design mm)."""
    hr = h * .075
    o = [punch_halo(cx, top + hr, hr * 1.9)]
    o.append(f'<ellipse cx="{cx}" cy="{top+hr}" rx="{hr*.8:.1f}" ry="{hr:.1f}" fill="{GESSO}" stroke="#2A2418" stroke-width="2"/>')
    o.append(f'<path d="M{cx-hr*.4:.1f},{top+hr*.9:.1f} l{hr*.25:.1f},0 M{cx+hr*.15:.1f},{top+hr*.9:.1f} l{hr*.25:.1f},0 M{cx},{top+hr*1.05:.1f} l0,{hr*.3:.1f} M{cx-hr*.2:.1f},{top+hr*1.55:.1f} l{hr*.4:.1f},0" stroke="#2A2418" stroke-width="2.5"/>')
    o.append(f'<path d="M{cx-hr*.9:.1f},{top+hr*.5:.1f} C{cx-hr*1.1:.1f},{top-hr*.2:.1f} {cx+hr*1.1:.1f},{top-hr*.2:.1f} {cx+hr*.9:.1f},{top+hr*.5:.1f} C{cx+hr*.6:.1f},{top:.1f} {cx-hr*.6:.1f},{top:.1f} {cx-hr*.9:.1f},{top+hr*.5:.1f} Z" fill="{shade(OAK,.8)}"/>')
    sh = top + hr * 2.2; bot = top + h
    w = h * .22
    if seated:
        o.append(f'<path d="M{cx-w*1.3:.1f},{bot-h*.35:.1f} L{cx+w*1.3:.1f},{bot-h*.35:.1f} L{cx+w*1.4:.1f},{bot:.1f} L{cx-w*1.4:.1f},{bot:.1f} Z" fill="{g_gilt}" stroke="#2A2418" stroke-width="2"/>')
    o.append(f'<path d="M{cx-w*.5:.1f},{sh:.1f} L{cx+w*.5:.1f},{sh:.1f} L{cx+w*.8:.1f},{bot:.1f} L{cx-w*.8:.1f},{bot:.1f} Z" fill="{robe}" stroke="#2A2418" stroke-width="2"/>')
    o.append(f'<path d="M{cx-w*.55:.1f},{sh:.1f} C{cx-w*1.2:.1f},{sh+h*.25:.1f} {cx-w*1.3:.1f},{bot-h*.2:.1f} {cx-w*1.05:.1f},{bot:.1f} L{cx+w*.2:.1f},{bot:.1f} C{cx+w*.6:.1f},{bot-h*.35:.1f} {cx+w*.1:.1f},{sh+h*.3:.1f} {cx+w*.55:.1f},{sh:.1f} Z" fill="{mantle}" stroke="#2A2418" stroke-width="2"/>')
    for i in range(3):
        o.append(f'<path d="M{cx-w*(.9-i*.3):.1f},{sh+h*.2:.1f} C{cx-w*(1-i*.3):.1f},{sh+h*.45:.1f} {cx-w*(1.1-i*.3):.1f},{bot-h*.15:.1f} {cx-w*(.95-i*.3):.1f},{bot-4:.1f}" fill="none" stroke="#14120E" stroke-width="3" opacity=".35"/>')
    o.append(f'<path d="M{cx-w*.55:.1f},{sh:.1f} C{cx-w*1.2:.1f},{sh+h*.25:.1f} {cx-w*1.3:.1f},{bot-h*.2:.1f} {cx-w*1.05:.1f},{bot:.1f}" fill="none" stroke="{GILT}" stroke-width="4"/>')
    return ''.join(o)


def centre_panel():
    """1100 x 1380 mm, gable from y 0 to 180, panel field 180..1380."""
    o = [f'<path d="M0,1380 L0,180 L550,0 L1100,180 L1100,1380 Z" fill="{g_oak}" stroke="#14120E" stroke-width="4"/>',
         f'<path d="M40,1340 L40,200 L550,40 L1060,200 L1060,1340 Z" fill="{g_gilt}" stroke="#2A2418" stroke-width="3"/>']
    # trefoil arch over the enthroned Virgin
    o.append(f'<path d="M140,1300 L140,420 C140,300 260,260 330,300 C380,200 720,200 770,300 C840,260 960,300 960,420 L960,1300 Z" fill="none" stroke="{shade(GILT,.5)}" stroke-width="10"/>')
    for x in range(60, 1060, 36):
        o.append(f'<circle cx="{x}" cy="1320" r="5" fill="{shade(GILT,.55)}"/>')
    for i in range(12):
        o.append(f'<path d="M{80+i*80},{360+ (i%3)*140} l18,-18 l18,18 l-18,18 Z" fill="none" stroke="{shade(GILT,.6)}" stroke-width="2" opacity=".6"/>')
    o.append(figure(550, 360, 880, g_woad, g_ker, seated=True))
    # the Child on the lap
    o.append(punch_halo(600, 760, 48))
    o.append(f'<ellipse cx="600" cy="760" rx="24" ry="30" fill="{GESSO}" stroke="#2A2418" stroke-width="2"/><path d="M570,790 L640,790 L650,900 L560,900 Z" fill="{GESSO}" stroke="#2A2418" stroke-width="2"/>')
    # gable roundel + crockets
    o.append(f'<circle cx="550" cy="140" r="60" fill="{g_woad}" stroke="{GILT}" stroke-width="8"/><path d="M520,150 L550,110 L580,150" fill="none" stroke="{GESSO}" stroke-width="8"/>')
    for t in (.2, .4, .6, .8):
        o.append(f'<path d="M{550*t:.0f},{180-180*t:.0f} l-18,-26 l30,6 Z M{1100-550*t:.0f},{180-180*t:.0f} l18,-26 l-30,6 Z" fill="{g_gilt}" stroke="#2A2418" stroke-width="2"/>')
    # gilt rub + candle soot
    o.append(f'<path d="M40,1100 L40,1340 L120,1340 Z M1060,1100 L1060,1340 L980,1340 Z" fill="{OAK}" opacity=".45"/>')
    o.append(s.flecks(40, 200, 1020, 1140, 180, "#14120E", 1.5, 5, .3))
    o.append(f'<path d="M40,200 L550,40 L1060,200 L1060,420 L40,420 Z" fill="#14120E" opacity=".18"/>')
    return ''.join(o)


def wing(inner, saint_mantle, saint_robe):
    """550 x 1200 mm; inner face painted on gilt, outer face grisaille."""
    if inner:
        o = [f'<rect x="0" y="0" width="550" height="1200" fill="{g_oak}" stroke="#14120E" stroke-width="4"/>',
             f'<path d="M30,1170 L30,160 C30,60 520,60 520,160 L520,1170 Z" fill="{g_gilt}" stroke="#2A2418" stroke-width="3"/>',
             figure(275, 230, 860, saint_mantle, saint_robe)]
        o.append(s.flecks(30, 60, 490, 1110, 90, "#14120E", 1.5, 5, .3))
    else:
        o = [f'<rect x="0" y="0" width="550" height="1200" fill="{g_gris}" stroke="#14120E" stroke-width="4"/>',
             f'<path d="M40,1160 L40,180 C40,80 510,80 510,180 L510,1160 Z" fill="{shade(GRIS,.8)}" stroke="{shade(GRIS,.5)}" stroke-width="4"/>',
             figure(275, 240, 850, g_gris, shade(GRIS, 1.2))]
        o.append(s.flecks(30, 60, 490, 1110, 70, "#14120E", 1.5, 5, .3))
    return ''.join(o)


def predella(w=1100, h=220):
    o = [f'<rect x="0" y="0" width="{w}" height="{h}" fill="{g_oak}" stroke="#14120E" stroke-width="4"/>',
         f'<rect x="30" y="30" width="{w-60}" height="{h-60}" fill="{g_gilt}" stroke="#2A2418" stroke-width="3"/>']
    for i in range(5):
        cx = 30 + (w - 60) * (i + .5) / 5
        o.append(f'<circle cx="{cx:.0f}" cy="{h/2}" r="{h*.28:.0f}" fill="{g_woad}" stroke="{shade(GILT,.5)}" stroke-width="4"/>'
                 f'<circle cx="{cx:.0f}" cy="{h/2-10}" r="{h*.09:.0f}" fill="{GESSO}"/>')
    return ''.join(o)


# ── hero: open, wings angled forward ──
HK = 245
cam = Cam(385, 655, HK, yaw=18, pitch=10)
parts = [f'<circle cx="385" cy="430" r="300" fill="{g_glow}"/>', '<ellipse cx="385" cy="668" rx="280" ry="30" fill="#0B0A08" opacity=".75"/>']
parts.append(cam.box(-.55, -.1, 0, .55, .12, .22, OAK, OAK, shade(OAK, .6), sw=1.2))
parts.append(f'<g transform="{cam.face_matrix((-.55, -.1, .22), (1.1, 0, 0), (0, 0, -.22), 1100, 220)}">{predella()}</g>')
parts.append(f'<g transform="{cam.face_matrix((-.55, 0, 1.60), (1.1, 0, 0), (0, 0, -1.38), 1100, 1380)}">{centre_panel()}</g>')
a45 = math.radians(50)
# left wing: from outer edge to hinge at x=-.55
lo = (-.55 - .55 * math.cos(a45), -.55 * math.sin(a45), 1.42)
parts.append(f'<g transform="{cam.face_matrix(lo, (.55 * math.cos(a45), .55 * math.sin(a45), 0), (0, 0, -1.2), 550, 1200)}">{wing(True, g_ker, g_woad)}</g>')
ro = (.55, 0, 1.42)
parts.append(f'<g transform="{cam.face_matrix(ro, (.55 * math.cos(a45), -.55 * math.sin(a45), 0), (0, 0, -1.2), 550, 1200)}">{wing(True, g_woad, g_ker)}</g>')
# hinge straps
for z in (.4, 1.2):
    for hx in (-.55, .55):
        p = cam.p(hx, 0, z)
        parts.append(f'<rect x="{p[0]-4:.1f}" y="{p[1]-6:.1f}" width="8" height="12" fill="#2A2418"/>')
# fractures: board joints + pinnacle + wing hinges
for bx in (-.18, .18):
    a = cam.p(bx, 0, .22); b = cam.p(bx, 0, 1.60 - .18 * (1 - abs(bx) / .55) - .0)
    parts.append(fracture(f"M{a[0]:.1f},{a[1]:.1f} L{b[0]:.1f},{b[1]:.1f}"))
a = cam.p(-.2, 0, 1.52); b = cam.p(.2, 0, 1.52)
parts.append(fracture(f"M{a[0]:.1f},{a[1]:.1f} L{b[0]:.1f},{b[1]:.1f}"))
for hx in (-.55, .55):
    a = cam.p(hx, 0, .24); b = cam.p(hx, 0, 1.40)
    parts.append(fracture(f"M{a[0]+(-5 if hx<0 else 5):.1f},{a[1]:.1f} L{b[0]+(-5 if hx<0 else 5):.1f},{b[1]:.1f}", 1.3))
# carriers' grabs at predella ends
gl = cam.p(-.55, -.05, .11); gr = cam.p(.55, .0, .11)
parts.append(grab(round(gl[0] - 2, 1), round(gl[1], 1), "CARRIER A", -12, 22, "end"))
parts.append(grab(round(gr[0] + 4, 1), round(gr[1], 1), "CARRIER B", 12, 22))
# ── orthos at 1 m = 180 px, closed (carry state) ──
OK = 180
FX, SX = 830, 1010
k = OK / 1000
parts.append(f'<g transform="translate({FX-550*k:.1f},{690-220*k:.1f}) scale({k})">{predella()}</g>')
parts.append(f'<g transform="translate({FX-550*k:.1f},{690-1600*k:.1f}) scale({k})"><path d="M0,1380 L0,180 L550,0 L1100,180 L1100,1380 Z" fill="{g_oak}" stroke="#14120E" stroke-width="4"/>'
             f'<path d="M40,200 L550,40 L1060,200 Z" fill="{g_gilt}" stroke="#2A2418" stroke-width="3"/><circle cx="550" cy="140" r="50" fill="{g_woad}" stroke="{GILT}" stroke-width="8"/></g>')
parts.append(f'<g transform="translate({FX-550*k:.1f},{690-1420*k:.1f}) scale({k})">{wing(False, None, None)}</g>')
parts.append(f'<g transform="translate({FX:.1f},{690-1420*k:.1f}) scale({k})">{wing(False, None, None)}</g>')
parts.append(f'<path d="M{FX},{690-1420*k:.1f} L{FX},{690-220*k:.1f}" stroke="#14120E" stroke-width="2"/>')
parts.append(fracture(f"M{FX-550*k-3:.1f},{690-1420*k:.1f} l0,{1200*k:.1f} M{FX+550*k+3:.1f},{690-1420*k:.1f} l0,{1200*k:.1f}", 1.2))
# side: predella 0.20 deep, panel 0.12 incl. closed wings
parts.append(f'<rect x="{SX-.11*OK:.1f}" y="{690-.22*OK:.1f}" width="{.22*OK:.1f}" height="{.22*OK:.1f}" fill="{g_oak}" stroke="#14120E" stroke-width="1"/>')
parts.append(f'<rect x="{SX-.02*OK:.1f}" y="{690-1.42*OK:.1f}" width="{.12*OK:.1f}" height="{1.2*OK:.1f}" fill="{g_oak}" stroke="#14120E" stroke-width="1"/>')
parts.append(f'<rect x="{SX+.06*OK:.1f}" y="{690-1.42*OK:.1f}" width="{.04*OK:.1f}" height="{1.2*OK:.1f}" fill="{g_gris}" stroke="#14120E" stroke-width=".8"/>')
parts.append(f'<path d="M{SX-.02*OK:.1f},{690-1.42*OK:.1f} L{SX-.02*OK:.1f},{690-1.6*OK:.1f} L{SX+.06*OK:.1f},{690-1.6*OK:.1f} L{SX+.06*OK:.1f},{690-1.42*OK:.1f} Z" fill="{g_gilt}" stroke="#14120E" stroke-width="1"/>')
parts.append(human(1110, 690, OK, label=True, op=.4))
parts.append(view_label(385, "HERO 3/4, OPEN · 1 m = 245 px", 730)); parts.append(view_label(FX, "FRONT, CLOSED", 730)); parts.append(view_label(SX, "SIDE", 730))
parts.append(scale_bar(730, 168, 90, "0.5 m = 90 px (orthos)"))
C = [callout(*[round(v, 1) for v in cam.p(0, 0, 1.52)], 700, 210, "Gable roundel + crockets", "carved limewood, gilt"),
     callout(*[round(v, 1) for v in cam.p(.05, 0, 1.0)], 700, 255, "Centre: Virgin enthroned", "tempera on gesso, gilt ground"),
     callout(*[round(v, 1) for v in cam.p(.55 + .3 * math.cos(a45), -.3 * math.sin(a45), 1.3)], 700, 300, "Wing, 0.55 × 1.20 m", "hinged, folds shut to carry"),
     callout(*[round(v, 1) for v in cam.p(.3, -.1, .15)], 700, 345, "Predella, 5 roundels", "oak box, 1.10 × 0.20 × 0.22 m")]
parts += C
parts.append(text(700, 382, "Wing backs: grisaille (closed view)", 10.5, "#9A9078"))
parts.append(text(40, 150, "FRAGILE 3 m/s — one fall down a stair and it is kindling: worth 0", 11, "#C4542E"))
head = item_frame(s, "Gilded Altarpiece", 1400, 14, "1.10 × 0.20 × 1.60 m closed · ARTIFACT · ≤ 5k tris · 1024²", "orthos 1 m = 180 px · two carriers")
write(s, head, parts, palette([("gilt (orpiment)", GILT), ("woad azure", WOAD), ("kermes", KER), ("gesso", GESSO), ("oak panel", OAK), ("grisaille", GRIS)]))
