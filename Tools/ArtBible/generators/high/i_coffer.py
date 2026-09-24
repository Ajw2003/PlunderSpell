from lib import *

s = Sheet("coin-coffer")
OAK = "#6B4E33"; IRON = "#3A3632"; RUST = "#7A4A2A"; SIL = "#B8B4A8"; LINEN = "#C4B89C"
g_oak_f = s.lg(shade(OAK, 1.15), shade(OAK, .75), 0, 0, 1, 1)
g_oak_s = s.lg(shade(OAK, .7), shade(OAK, .45), 0, 0, 1, 1)
g_oak_t = s.lg(shade(OAK, 1.35), shade(OAK, 1.0), 0, 0, 1, 1)
g_iron = s.lg(shade(IRON, 1.6), shade(IRON, .7), 0, 0, 1, 1)
g_sil = s.rg(shade(SIL, 1.4), shade(SIL, .6), 1, 1, .35, .35, .7)
g_glint = s.rg(SIL, SIL, .14, 0, .5, .5, .5)


def nails(x, y0, y1, step=22):
    return ''.join(f'<circle cx="{x}" cy="{y}" r="3.2" fill="{shade(IRON,1.8)}" stroke="#14120E" stroke-width="1"/>' for y in range(int(y0), int(y1), step))


def planks(w, y0, y1, n, fill):
    o = [f'<rect x="0" y="{y0}" width="{w}" height="{y1-y0}" fill="{fill}"/>']
    for i in range(1, n):
        y = y0 + (y1 - y0) * i / n
        o.append(f'<path d="M0,{y:.0f} L{w},{y:.0f}" stroke="#14120E" stroke-width="2.5" opacity=".7"/>')
    for i in range(18):
        yy = y0 + s.rng.random() * (y1 - y0); xx = s.rng.random() * w * .8
        o.append(f'<path d="M{xx:.0f},{yy:.0f} q{40+s.rng.random()*80:.0f},{s.rng.random()*6-3:.1f} {100+s.rng.random()*80:.0f},0" fill="none" stroke="{shade(OAK,.55)}" stroke-width="1.5" opacity=".6"/>')
    return ''.join(o)


def strap(x, y0, y1, w=30):
    return (f'<rect x="{x}" y="{y0}" width="{w}" height="{y1-y0}" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/>'
            f'<path d="M{x+2},{y0} L{x+2},{y1}" stroke="{RUST}" stroke-width="2" opacity=".6"/>' + nails(x + w / 2, y0 + 12, y1 - 4))


def front_face(split=False):
    o = [planks(500, 0, 100, 1, g_oak_f), planks(500, 100, 300, 2, g_oak_f)]
    o.append('<path d="M0,100 L500,100" stroke="#0B0A08" stroke-width="5"/>')
    for x in (60, 235, 410):
        o.append(strap(x, 0, 96)); o.append(strap(x, 104, 300))
    o.append(f'<rect x="0" y="0" width="500" height="14" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/>')
    o.append(f'<rect x="0" y="286" width="500" height="14" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/>')
    for x in (0, 470):
        o.append(f'<path d="M{x},0 L{x+30},0 L{x+30},300 L{x},300 Z" fill="{g_iron}" stroke="#14120E" stroke-width="1.5" opacity=".95"/>' + nails(x + 15, 26, 290, 26))
    # hasp, staple, barrel padlock
    o.append(f'<path d="M232,60 L268,60 L262,150 L238,150 Z" fill="{g_iron}" stroke="#14120E" stroke-width="2"/>')
    o.append(f'<rect x="244" y="132" width="12" height="22" rx="4" fill="none" stroke="{shade(IRON,1.8)}" stroke-width="4"/>')
    o.append(f'<rect x="222" y="158" width="56" height="28" rx="12" fill="{g_iron}" stroke="#14120E" stroke-width="2"/>')
    o.append(f'<path d="M230,172 L270,172 M236,160 L236,184 M264,160 L264,184" stroke="{RUST}" stroke-width="2"/>')
    o.append(f'<rect x="274" y="166" width="10" height="12" fill="#0B0A08"/>')
    o.append(s.flecks(0, 0, 500, 300, 110, "#14120E", .8, 2.4, .35))
    o.append(s.flecks(0, 0, 500, 300, 40, RUST, .8, 2.6, .5))
    return ''.join(o)


def end_face():
    o = [planks(320, 0, 100, 1, g_oak_s), planks(320, 100, 300, 2, g_oak_s), '<path d="M0,100 L320,100" stroke="#0B0A08" stroke-width="5"/>']
    for x in (0, 290):
        o.append(strap(x, 0, 300))
    o.append(f'<rect x="0" y="0" width="320" height="14" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/><rect x="0" y="286" width="320" height="14" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/>')
    o.append(f'<rect x="130" y="130" width="60" height="22" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/>')
    o.append(f'<path d="M125,150 C120,215 200,215 195,150" fill="none" stroke="#14120E" stroke-width="12"/><path d="M125,150 C120,215 200,215 195,150" fill="none" stroke="{shade(IRON,1.7)}" stroke-width="7"/>')
    o.append(s.flecks(0, 0, 320, 300, 60, "#14120E", .8, 2.4, .35))
    return ''.join(o)


def top_face():
    o = [planks(500, 0, 320, 2, g_oak_t)]
    for x in (60, 235, 410):
        o.append(f'<rect x="{x}" y="0" width="30" height="320" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/>' +
                 ''.join(f'<circle cx="{x+15}" cy="{y}" r="3.2" fill="{shade(IRON,1.8)}" stroke="#14120E" stroke-width="1"/>' for y in range(16, 320, 22)))
    o.append(f'<rect x="0" y="0" width="500" height="14" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/><rect x="0" y="306" width="500" height="14" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/>')
    o.append(f'<rect x="0" y="0" width="30" height="320" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/><rect x="470" y="0" width="30" height="320" fill="{g_iron}" stroke="#14120E" stroke-width="1.5"/>')
    o.append(f'<path d="M150,120 C220,110 300,140 360,125" fill="none" stroke="{shade(OAK,1.5)}" stroke-width="10" opacity=".35"/>')
    o.append(s.flecks(0, 0, 500, 320, 90, "#14120E", .8, 2.4, .3))
    return ''.join(o)


HK = 720
cam = Cam(350, 610, HK, yaw=30, pitch=26)
W, D, H = .50, .32, .30
x0, y0 = -W / 2, -D / 2
parts = [f'<circle cx="350" cy="480" r="260" fill="{g_glint}"/>', '<ellipse cx="385" cy="625" rx="270" ry="45" fill="#0B0A08" opacity=".75"/>']
parts.append(cam.box(x0, y0, 0, x0 + W, y0 + D, H, OAK, OAK, OAK, sw=1.2))
parts.append(f'<g transform="{cam.face_matrix((x0, y0, H), (W, 0, 0), (0, 0, -H), 500, 300)}">{front_face()}</g>')
parts.append(f'<g transform="{cam.face_matrix((x0 + W, y0, H), (0, D, 0), (0, 0, -H), 320, 300)}">{end_face()}</g>')
parts.append(f'<g transform="{cam.face_matrix((x0, y0 + D, H), (W, 0, 0), (0, -D, 0), 500, 320)}">{top_face()}</g>')
parts.append(f'<g transform="{cam.face_matrix((x0 + W, y0, H), (0, D, 0), (0, 0, -H), 320, 300)}" opacity=".45"><rect width="320" height="300" fill="#14120E"/></g>')
# fracture: lid hinge line + front plank joint
a = cam.p(x0, y0, H - .10); b = cam.p(x0 + W, y0, H - .10); c = cam.p(x0 + W, y0 + D, H - .10)
parts.append(fracture(f"M{a[0]:.1f},{a[1]:.1f} L{b[0]:.1f},{b[1]:.1f} L{c[0]:.1f},{c[1]:.1f}"))
a = cam.p(x0 + .03, y0, .10); b = cam.p(x0 + .23, y0, .10)
parts.append(fracture(f"M{a[0]:.1f},{a[1]:.1f} L{b[0]:.1f},{b[1]:.1f}"))
# orthos at 1 m = 400 px
OK = 400
FX, SX = 800, 1060
parts.append(f'<g transform="translate({FX-W*OK/2:.1f},{690-H*OK:.1f}) scale({OK/1000})">{front_face()}</g>')
parts.append(f'<g transform="translate({SX-D*OK/2:.1f},{690-H*OK:.1f}) scale({OK/1000})">{end_face()}</g>')
parts.append(fracture(f"M{FX-W*OK/2:.1f},{690-.2*OK:.1f} l{W*OK:.1f},0", 1.4))
parts.append(view_label(350, "HERO 3/4 · 1 m = 720 px", 730)); parts.append(view_label(FX, "FRONT", 730)); parts.append(view_label(SX, "SIDE (END)", 730))
parts.append(scale_bar(890, 470, 40, "0.1 m = 40 px (orthos)"))
# spill inset
parts.append(text(700, 170, "BURST > 8 m/s: LID OFF, COINS SPILL", 10.5, "#C4542E", extra=' letter-spacing="1"'))
rng = random.Random(7)
spill = []
for i in range(70):
    ang = rng.random() * math.pi; r = rng.random() ** .6 * 200
    x = 900 + math.cos(ang) * r * 1.2; y = 300 - math.sin(ang) * r * .35 + 20
    spill.append(f'<ellipse cx="{x:.1f}" cy="{y:.1f}" rx="6.5" ry="3" fill="{g_sil}" stroke="#14120E" stroke-width=".6"/>')
parts.append(''.join(spill))
parts.append(f'<path d="M820,318 C840,290 880,285 900,300 C920,285 960,290 980,318 Z" fill="{LINEN}" stroke="#14120E" stroke-width="1"/><path d="M895,300 l-6,-14 l14,4 Z" fill="{LINEN}" stroke="#14120E" stroke-width="1"/>')
parts.append(text(900, 345, "4 linen bags split · 40 penny stacks × 10 coin", 10.5, "#9A9078", "middle"))
parts.append(grab(cam.p(x0 + W, y0 + D / 2, .12)[0] + 2, cam.p(x0 + W, y0 + D / 2, .12)[1] + 6, "GRAB · DROP HANDLE", 12, 18))
pl = cam.p(x0, y0 + D / 2, .14)
parts.append(grab(pl[0] - 4, pl[1], "GRAB", -12, -10, "end"))
C = [callout(*[round(v, 1) for v in cam.p(x0 + .25, y0, .17)], 300, 270, "Barrel padlock + hasp", "iron, key-hole on the end", side="left"),
     callout(*[round(v, 1) for v in cam.p(x0 + .075, y0, .06)], 300, 210, "Strap-iron bands, 30 mm", "blackened, clench-nailed", side="left"),
     callout(*[round(v, 1) for v in cam.p(x0 + .35, y0 + .2, H)], 300, 150, "Oak lid, 25 mm planks", "rubbed pale where it opens", side="left"),
     callout(*[round(v, 1) for v in cam.p(x0 + .45, y0, .25)], 560, 420, "Corner angle irons", "rust bloom at the nails"),
     callout(*[round(v, 1) for v in cam.p(x0 + .5, y0 + .08, .02)], 560, 480, "Oak carcass, 3 planks", "dovetailed, pitch-sealed")]
parts += C
head = item_frame(s, "Coin Coffer", 450, 8, "0.50 × 0.32 × 0.30 m · ≤ 3k tris · 1024²", "orthos 1 m = 400 px · fragility 8 m/s")
write(s, head, parts, palette([("oak", OAK), ("strap iron", IRON), ("rust", RUST), ("silver penny", SIL), ("linen bag", LINEN)]))
