from lib import *

s = Sheet("illuminated-psalter")
GILT = "#C9A227"; IVORY = "#D8CDB0"; LEATH = "#5E3A2A"; VELL = "#DCD2BA"; GAR = "#6E1F2A"; SAPH = "#3E5470"; OAK = "#5E4630"
g_gilt = s.lg(shade(GILT, 1.35), shade(GILT, .5), 0, 0, 1, 1, mid=GILT)
g_ivory = s.lg(shade(IVORY, 1.2), shade(IVORY, .65), 0, 0, 1, 1)
g_leath = s.lg(shade(LEATH, 1.2), shade(LEATH, .6), 0, 0, 1, 1)
g_gar = s.rg(shade(GAR, 1.8), GAR, 1, 1, .35, .3, .6)
g_saph = s.rg(shade(SAPH, 1.8), SAPH, 1, 1, .35, .3, .6)
g_pages = s.lg(VELL, shade(VELL, .7), 0, 0, 0, 1)
g_glint = s.rg(GILT, GILT, .22, 0, .5, .5, .5)


def cover(tag):
    """Treasure-binding front board, design units mm, 190 x 260."""
    o = [f'<rect x="0" y="0" width="190" height="260" rx="4" fill="{g_leath}" stroke="#14120E" stroke-width="1.5"/>',
         f'<rect x="8" y="8" width="174" height="244" rx="3" fill="{g_gilt}" stroke="#14120E" stroke-width="1"/>',
         f'<rect x="30" y="30" width="130" height="200" fill="{LEATH}" stroke="#14120E" stroke-width="1"/>']
    # filigree scrolls on the frame
    for y in range(20, 250, 16):
        o.append(f'<path d="M14,{y} q5,-6 10,0 q-5,6 -10,0 M166,{y} q5,-6 10,0 q-5,6 -10,0" fill="none" stroke="{shade(GILT,.55)}" stroke-width="1"/>')
    for x in range(36, 160, 16):
        o.append(f'<path d="M{x},14 q6,5 0,10 q-6,-5 0,-10 M{x},236 q6,5 0,10 q-6,-5 0,-10" fill="none" stroke="{shade(GILT,.55)}" stroke-width="1"/>')
    # cabochons: alternating garnet and sapphire
    k = 0
    for (x, y) in [(19, 19), (171, 19), (19, 241), (171, 241), (95, 19), (95, 241), (19, 130), (171, 130), (19, 75), (171, 75), (19, 185), (171, 185), (57, 19), (133, 19), (57, 241), (133, 241)]:
        big = (x, y) in [(19, 19), (171, 19), (19, 241), (171, 241)]
        f = g_gar if k % 2 == 0 else g_saph; k += 1
        r = 8 if big else 5.5
        o.append(f'<circle cx="{x}" cy="{y}" r="{r+2}" fill="{shade(GILT,1.2)}" stroke="#14120E" stroke-width=".8"/><ellipse cx="{x}" cy="{y}" rx="{r}" ry="{r*1.1:.1f}" fill="{f}" stroke="#14120E" stroke-width=".6"/>'
                 f'<circle cx="{x-r*.35:.1f}" cy="{y-r*.4:.1f}" r="{r*.25:.1f}" fill="#FFFFFF" opacity=".7"/>')
    # ivory plaque: Christ in Majesty, simplified
    o.append(f'<rect x="38" y="40" width="114" height="180" rx="2" fill="{g_ivory}" stroke="#14120E" stroke-width="1"/>')
    o.append(f'<path d="M95,52 C125,60 132,120 125,170 C120,200 105,210 95,212 C85,210 70,200 65,170 C58,120 65,60 95,52 Z" fill="none" stroke="{shade(IVORY,.55)}" stroke-width="2"/>')
    o.append(f'<circle cx="95" cy="82" r="12" fill="none" stroke="{shade(IVORY,.5)}" stroke-width="2"/><circle cx="95" cy="84" r="7" fill="{shade(IVORY,.85)}" stroke="{shade(IVORY,.5)}" stroke-width="1"/>')
    o.append(f'<path d="M80,100 C84,96 106,96 110,100 L114,150 C110,160 80,160 76,150 Z" fill="{shade(IVORY,.9)}" stroke="{shade(IVORY,.5)}" stroke-width="1.4"/>')
    o.append(f'<path d="M86,110 L84,148 M95,104 L95,152 M104,110 L106,148 M78,160 C85,172 105,172 112,160 L116,195 L74,195 Z" fill="none" stroke="{shade(IVORY,.55)}" stroke-width="1.2"/>')
    o.append(f'<path d="M108,108 l12,-10 M118,96 l4,4 M72,120 c-4,6 -4,10 0,14" fill="none" stroke="{shade(IVORY,.5)}" stroke-width="1.6"/>')
    o.append(f'<path d="M44,50 l12,0 M44,58 l8,0 M146,50 l-12,0 M146,58 l-8,0 M44,210 l12,0 M146,210 l-12,0" stroke="{shade(IVORY,.55)}" stroke-width="1.4"/>')
    # gilt rub: frame edges worn to leather where hands go
    o.append(f'<path d="M8,110 L8,150 L14,150 L14,110 Z M176,100 L182,100 L182,160 L176,160 Z" fill="{LEATH}" opacity=".55"/>')
    o.append(s.flecks(8, 8, 174, 244, 80, "#1E1B17", .3, 1.2, .35))
    return ''.join(o)


def page(tag):
    """An illuminated leaf, 180 x 250 mm."""
    o = [f'<rect x="0" y="0" width="180" height="250" fill="{VELL}" stroke="{shade(VELL,.6)}" stroke-width="1"/>',
         f'<rect x="20" y="22" width="44" height="46" fill="{SAPH}"/><rect x="24" y="26" width="36" height="38" fill="{GILT}"/>',
         f'<path d="M32,58 L32,32 L42,32 C54,32 54,46 42,46 L32,46 M42,46 L54,58" fill="none" stroke="{GAR}" stroke-width="5"/>']
    for i in range(14):
        y = 30 + i * 14; x0 = 72 if y < 72 else 20
        o.append(f'<path d="M{x0},{y} L{160 - (i * 7 % 23)},{y}" stroke="#2A2418" stroke-width="3" opacity=".7" stroke-dasharray="{4 + i % 3} 2"/>')
        if i % 5 == 3:
            o.append(f'<rect x="20" y="{y-5}" width="8" height="8" fill="{GAR}"/>')
    o.append(f'<path d="M166,20 C176,60 160,120 172,170 C176,200 166,220 170,236" fill="none" stroke="{SAPH}" stroke-width="3"/>')
    o.append(f'<circle cx="170" cy="90" r="4" fill="{GILT}"/><circle cx="168" cy="150" r="4" fill="{GILT}"/>')
    return ''.join(o)


HK = 1300
cam = Cam(330, 560, HK, yaw=28, pitch=38)
W, D, T = .19, .26, .08
x0, y0 = -W / 2, -D / 2
parts = [f'<circle cx="330" cy="430" r="260" fill="{g_glint}"/>']
parts.append(f'<ellipse cx="345" cy="600" rx="230" ry="40" fill="#0B0A08" opacity=".7"/>')
# bottom board, page block, top board
parts.append(cam.box(x0, y0, 0, x0 + W, y0 + D, .012, g_leath, g_leath, g_leath))
parts.append(cam.box(x0 + .006, y0 + .004, .012, x0 + W - .004, y0 + D - .004, T - .012, VELL, g_pages, g_pages))
# page-edge lines on front and right faces
for i in range(1, 14):
    z = .012 + (T - .024) * i / 14
    a = cam.p(x0 + .006, y0 + .004, z); b = cam.p(x0 + W - .004, y0 + .004, z); c = cam.p(x0 + W - .004, y0 + D - .004, z)
    parts.append(f'<path d="M{a[0]:.1f},{a[1]:.1f} L{b[0]:.1f},{b[1]:.1f} L{c[0]:.1f},{c[1]:.1f}" fill="none" stroke="{shade(VELL,.6)}" stroke-width=".6" opacity=".7"/>')
parts.append(cam.box(x0, y0, T - .012, x0 + W, y0 + D, T, g_leath, g_leath, g_leath))
mtx = cam.face_matrix((x0, y0 + D, T), (W, 0, 0), (0, -D, 0), 190, 260)
parts.append(f'<g transform="{mtx}">{cover("h")}</g>')
# clasps over the fore-edge (right face)
for yy in (.07, .19):
    pts = [(x0 + W - .03, y0 + yy - .008, T), (x0 + W, y0 + yy - .008, T), (x0 + W + .004, y0 + yy - .008, T * .5), (x0 + W + .004, y0 + yy + .008, T * .5), (x0 + W, y0 + yy + .008, T), (x0 + W - .03, y0 + yy + .008, T)]
    parts.append(cam.poly(pts, fill=LEATH, stroke="#14120E", stroke_width=".8"))
    c = cam.p(x0 + W + .004, y0 + yy, T * .45)
    parts.append(f'<rect x="{c[0]-6:.1f}" y="{c[1]-9:.1f}" width="12" height="16" rx="2" fill="{g_gilt}" stroke="#14120E" stroke-width=".8"/>')
# spine hinge fracture on hero (left edge of top board)
a = cam.p(x0, y0, T); b = cam.p(x0, y0 + D, T)
parts.append(fracture(f"M{a[0]:.1f},{a[1]-2:.1f} L{b[0]:.1f},{b[1]-2:.1f}"))
# orthos
OK = 900
FX, SX = 790, 1070
fx0, fy0 = FX - 190 * OK / 1000 / 2, 690 - 260 * OK / 1000
parts.append(f'<g transform="translate({fx0:.1f},{fy0:.1f}) scale({OK/1000})">{cover("f")}</g>')
sw = 80 * OK / 1000; sy0 = 690 - 260 * OK / 1000
parts.append(f'<rect x="{SX-sw/2:.1f}" y="{sy0:.1f}" width="{sw:.1f}" height="{260*OK/1000:.1f}" fill="{g_pages}" stroke="#14120E" stroke-width="1"/>')
for i in range(1, 12):
    x = SX - sw / 2 + 10 + (sw - 20) * i / 12
    parts.append(f'<path d="M{x:.1f},{sy0+2:.1f} L{x:.1f},{688}" stroke="{shade(VELL,.6)}" stroke-width=".5"/>')
parts.append(f'<rect x="{SX-sw/2:.1f}" y="{sy0:.1f}" width="10" height="{260*OK/1000:.1f}" fill="{g_leath}" stroke="#14120E" stroke-width="1"/>')
parts.append(f'<rect x="{SX+sw/2-10:.1f}" y="{sy0:.1f}" width="10" height="{260*OK/1000:.1f}" fill="{g_leath}" stroke="#14120E" stroke-width="1"/>')
parts.append(f'<rect x="{SX-sw/2-4:.1f}" y="{sy0:.1f}" width="4" height="{260*OK/1000:.1f}" fill="{g_gilt}"/>')
for yy in (.07, .19):
    y = 690 - (.26 - yy) * OK
    parts.append(f'<rect x="{SX-sw/2+4:.1f}" y="{y-7:.1f}" width="{sw-8:.1f}" height="14" fill="{LEATH}" stroke="#14120E" stroke-width=".8"/>'
                 f'<rect x="{SX-7:.1f}" y="{y-8:.1f}" width="14" height="16" rx="2" fill="{g_gilt}" stroke="#14120E" stroke-width=".8"/>')
parts.append(fracture(f"M{SX-sw/2:.1f},{sy0-6:.1f} L{SX-sw/2:.1f},{696}", 1.4))
parts.append(view_label(330, "HERO 3/4 · 1 m = 1300 px", 730)); parts.append(view_label(FX, "FRONT (COVER)", 730)); parts.append(view_label(SX, "SIDE (FORE-EDGE)", 730))
parts.append(scale_bar(900, 400, 90, "0.1 m = 90 px (orthos)"))
# scattered leaves inset
parts.append(text(700, 160, "HARD IMPACT > 5 m/s: 24 LEAVES SCATTER", 10.5, "#C4542E", extra=' letter-spacing="1"'))
for i, (x, y, r) in enumerate([(730, 200, -14), (820, 185, 9), (905, 210, -4), (990, 190, 17), (1070, 205, -22)]):
    parts.append(f'<g transform="translate({x},{y}) rotate({r}) scale(.36)" opacity="{.95 - i*.08:.2f}">{page(i)}</g>')
parts.append(grab(170, 535, "GRAB · SPINE", -12, 20, "end"))
parts.append(grab(430, 585, "GRAB · FORE-EDGE", 12, 16))
C = [callout(250, 470, 215, 180, "Ivory plaque, carved", "Christ in Majesty, 11×18 cm", side="left"),
     callout(208, 420, 215, 235, "Silver-gilt frame, 22 mm", "filigree scrolls, soldered", side="left"),
     callout(330, 322, 530, 250, "16 cabochons", "garnet + sapphire, collet-set"),
     callout(508, 470, 560, 330, "Two clasps, strap + catch", "tawed leather, gilt catch"),
     callout(470, 545, 560, 390, "Page block, 180 leaves", "vellum, gilt-edged"),
     callout(282, 575, 215, 290, "Oak boards, 12 mm", "red-brown tawed leather", side="left")]
parts += C
parts.append(text(880, 432, "IGNIS or water: pages ruined, 4 s burn;", 10.5, "#C4542E"))
parts.append(text(880, 446, "worth falls to the board (150)", 10.5, "#9A9078"))
head = item_frame(s, "Illuminated Psalter", 350, 1, "0.19 × 0.26 × 0.08 m · ≤ 1.5k tris · 1024²", "orthos 1 m = 900 px · fragility 5 m/s")
write(s, head, parts, palette([("gilt (orpiment)", GILT), ("ivory", IVORY), ("tawed leather", LEATH), ("vellum", VELL), ("garnet", GAR), ("sapphire", SAPH)]))
