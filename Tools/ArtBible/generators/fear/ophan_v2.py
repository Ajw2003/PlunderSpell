"""The Ophan, second round (#386): the owner chose it and asked for something closer to the source
(Ezekiel 1 and 10, Isaiah 6), much larger, and plainly beyond defeating, keeping the colour scheme
(tarnished pewter, vellum, bone black, lapis portal light). Two sheets:

  ophan-v2-1-the-wheels      the whole being at true scale beside the 1.80 m human
  ophan-v2-2-in-the-castle   inside a Keep cell, larger than the room, and its rim up close

From the source: a wheel within a wheel, their rims high and dreadful and full of eyes all round;
four faces (a man, a lion, an ox, an eagle); wings, six of them as the seraphim have, two covering
the face, two covering the feet, two spread; fire with lightning going out of it.
The magical twist: it is a clock of the world. Its great rim is engraved with the years of the four
Ages, it stands in several moments at once (faint echoes of it a heartbeat early and late), and
stone it passes through is pulled back to before it was laid, then closes behind it. It never uses
a door.

Run from the repo root:  python3 Tools/ArtBible/generators/fear/ophan_v2.py
"""
import math
from lib import *  # noqa: F403
from creatures import PEWTER, DEEP, GHOST, eye

BONE = VELLUM
AGES = ["c. 1200 BC", "AD 1190", "AD 1470", "AD 1640"]   # the four Ages' years, as on the art bible


def wing(s, root, tip, width, n, flip=1, tint=1.0, eyes=0):
    """A long feathered wing from root to tip: a covert body plus n primary feathers along its trailing edge."""
    (rx, ry), (tx, ty) = root, tip
    dx, dy = tx - rx, ty - ry; L = math.hypot(dx, dy); ux, uy = dx / L, dy / L; nx, ny = -uy * flip, ux * flip
    out = []
    feather = s.lg(shade(BONE, 1.0 * tint), shade(PEWTER, .7 * tint), 0, 0, 1, 1)
    feather2 = s.lg(shade(BONE, 1.1 * tint), shade(BONE, .75 * tint), 0, 0, 1, 1)

    def plume(u, ln_k, w_k, fill):
        bx, by = rx + dx * u, ry + dy * u
        ln = width * ln_k
        a = math.atan2(ny, nx) + (u - .55) * .9 * flip
        fx, fy = bx + math.cos(a) * ln, by + math.sin(a) * ln
        w = width * w_k
        px, py = -math.sin(a) * w, math.cos(a) * w
        bend = .35 * flip   # each feather curls toward the tip of the wing
        cx_, cy_ = (bx + fx) / 2 + ux * ln * bend * .3, (by + fy) / 2 + uy * ln * bend * .3
        return (f'<path d="M{bx+px*.6:.1f},{by+py*.6:.1f} Q{cx_+px:.1f},{cy_+py:.1f} {fx:.1f},{fy:.1f} '
                f'Q{cx_-px:.1f},{cy_-py:.1f} {bx-px*.6:.1f},{by-py*.6:.1f} Z" fill="{fill}" stroke="{shade(PEWTER,.42*tint)}" stroke-width="1.4"/>'
                f'<path d="M{bx:.1f},{by:.1f} Q{cx_:.1f},{cy_:.1f} {fx:.1f},{fy:.1f}" fill="none" stroke="{shade(PEWTER,.55*tint)}" stroke-width="1.1"/>')
    m = n + 5
    for i in range(m):   # primaries, root to tip, each overlapping the one before
        u = .08 + .92 * i / (m - 1)
        out.append(plume(u, .55 + .75 * u, .085 + .03 * u, feather))
    for i in range(m - 2):   # a second row of coverts, shorter, laid over the primaries
        u = .1 + .85 * i / (m - 3)
        out.append(plume(u, .32 + .3 * u, .08, feather2))
    # the covert: the wing's arm, broad at the root and tapering to the tip
    cw = width * .42
    out.append(f'<path d="M{rx+nx*cw*.5:.1f},{ry+ny*cw*.5:.1f} Q{rx+dx*.45+nx*cw*.9:.1f},{ry+dy*.45+ny*cw*.9:.1f} {tx:.1f},{ty:.1f} '
               f'Q{rx+dx*.5-nx*cw*.25:.1f},{ry+dy*.5-ny*cw*.25:.1f} {rx-nx*cw*.35:.1f},{ry-ny*cw*.35:.1f} Z" '
               f'fill="{s.lg(shade(BONE,1.05*tint), shade(BONE,.72*tint), 0, 0, 1, 1)}" stroke="{shade(PEWTER,.45*tint)}" stroke-width="1.6"/>')
    for i in range(5):   # scalloped edge of the small coverts along the arm
        u = .1 + .17 * i
        sx_, sy_ = rx + dx * u + nx * cw * .2, ry + dy * u + ny * cw * .2
        out.append(f'<path d="M{sx_:.1f},{sy_:.1f} q{nx*cw*.35+ux*20:.1f},{ny*cw*.35+uy*20:.1f} {ux*44:.1f},{uy*44:.1f}" fill="none" stroke="{shade(PEWTER,.5*tint)}" stroke-width="1.2"/>')
    for i in range(eyes):   # "their whole body, their backs, their hands and their wings were full of eyes"
        u = .3 + .6 * i / max(1, eyes - 1)
        ex, ey = rx + dx * u + nx * cw * .35, ry + dy * u + ny * cw * .35
        out.append(eye(ex, ey, 22, math.degrees(math.atan2(dy, dx))))
    return ''.join(out)


def rim(s, rx, ry, rot, w, cy, eyes, eye_w, marks=0, back=False, eye_in=0.0):
    """One wheel as an ellipse (rx, ry) rotated by rot about (0, cy): rim, hour marks, and eyes along it."""
    g = s.lg(shade(PEWTER, 1.3), shade(PEWTER, .4), 0, 0, 1, 1, mid=PEWTER)
    out = [f'<g transform="translate(0,{cy}) rotate({rot})" opacity="{.55 if back else 1}">',
           f'<ellipse cx="0" cy="0" rx="{rx}" ry="{ry}" fill="none" stroke="{BLACK}" stroke-width="{w+5}"/>',
           f'<ellipse cx="0" cy="0" rx="{rx}" ry="{ry}" fill="none" stroke="{g}" stroke-width="{w}"/>',
           f'<ellipse cx="0" cy="0" rx="{rx-w*.32:.1f}" ry="{max(1, ry-w*.32):.1f}" fill="none" stroke="{shade(PEWTER,.5)}" stroke-width="1.6"/>',
           f'<ellipse cx="0" cy="0" rx="{rx+w*.32:.1f}" ry="{ry+w*.32:.1f}" fill="none" stroke="{shade(PEWTER,1.35)}" stroke-width="1.6" opacity=".7"/>']
    for h in range(marks):
        a = 2 * math.pi * h / marks
        c, sn = math.cos(a), math.sin(a)
        k0, k1 = (1 - w * .45 / max(rx, ry)), (1 + w * (.45 if h % 6 else .6) / max(rx, ry))
        out.append(f'<path d="M{c*rx*k0:.1f},{sn*ry*k0:.1f} L{c*rx*k1:.1f},{sn*ry*k1:.1f}" stroke="{BLACK}" stroke-width="{4 if h % 6 == 0 else 2}"/>')
    for i in range(eyes):
        a = 2 * math.pi * (i + .5) / eyes
        ex, ey = math.cos(a) * (rx - eye_in), math.sin(a) * (ry - eye_in)
        out.append(eye(ex, ey, eye_w, (math.degrees(a) + 90 + (i * 47) % 50 - 25) if rx > ry * .5 else 90 + (i * 31) % 40 - 20, open_=.75 + (i % 3) * .12))
    out.append('</g>')
    return ''.join(out)


def faces(s, cy, k=1.0):
    """The four faces about the core: the man to the front, the lion to the left, the eagle to the right, the ox above."""
    gm = s.lg(shade(PEWTER, 1.35), shade(PEWTER, .55), 0, 0, 1, 1)
    out = []
    # the man: a calm pewter face, eyes closed, light in the seams
    out.append(f'<g transform="translate(0,{cy}) scale({k})">')
    out.append(f'<path d="M-24,-36 C-26,-6 -18,22 0,34 C18,22 26,-6 24,-36 C14,-46 -14,-46 -24,-36 Z" fill="{gm}" stroke="{BLACK}" stroke-width="2"/>')
    out.append(f'<path d="M-15,-8 Q-9,-4 -3,-8 M3,-8 Q9,-4 15,-8" fill="none" stroke="{BLACK}" stroke-width="2.4"/>')
    out.append(f'<path d="M0,-6 L-3,10 L3,10" fill="none" stroke="{shade(PEWTER,.45)}" stroke-width="1.6"/>')
    out.append(f'<path d="M-8,20 Q0,23 8,20" fill="none" stroke="{GHOST}" stroke-width="2.2"/>')
    out.append(f'<path d="M0,-44 L0,-36" stroke="{GHOST}" stroke-width="2"/>')
    # the lion, in profile to the left: a heavy muzzle and a mane of hammered points
    out.append(f'<g transform="translate(-62,6)">')
    for i in range(11):
        a = math.radians(-160 + i * 26)
        out.append(f'<path d="M{math.cos(a)*16:.1f},{math.sin(a)*16:.1f} L{math.cos(a)*34:.1f},{math.sin(a)*34:.1f} L{math.cos(a+.2)*18:.1f},{math.sin(a+.2)*18:.1f} Z" fill="{shade(PEWTER,.8)}" stroke="{BLACK}" stroke-width="1.2"/>')
    out.append(f'<path d="M14,-18 C0,-22 -22,-16 -30,-2 L-36,4 L-30,10 C-24,18 -6,22 14,16 Z" fill="{gm}" stroke="{BLACK}" stroke-width="1.8"/>')
    out.append(f'<path d="M-12,-6 l6,-1" stroke="{BLACK}" stroke-width="2.4"/><path d="M-34,8 L-22,12" stroke="{BLACK}" stroke-width="1.6"/></g>')
    # the eagle, in profile to the right: a hooked beak
    out.append(f'<g transform="translate(62,4)">')
    out.append(f'<path d="M-16,-22 C4,-30 24,-22 30,-8 C40,-6 44,2 38,12 C34,6 28,4 26,4 C20,16 2,22 -16,18 Z" fill="{gm}" stroke="{BLACK}" stroke-width="1.8"/>')
    out.append(f'<circle cx="16" cy="-10" r="3" fill="{BLACK}"/><circle cx="15" cy="-11" r="1" fill="{GHOST}"/>')
    for i in range(4):
        out.append(f'<path d="M{-14+i*7},{-16+i*2} q-4,8 -8,12" fill="none" stroke="{shade(PEWTER,.5)}" stroke-width="1.4"/>')
    out.append('</g>')
    # the ox, above and behind: the horns show over the man's brow
    out.append(f'<path d="M-30,-58 C-48,-62 -56,-78 -50,-92 C-42,-78 -30,-74 -14,-64 Z M30,-58 C48,-62 56,-78 50,-92 C42,-78 30,-74 14,-64 Z" fill="{shade(BONE,.85)}" stroke="{BLACK}" stroke-width="1.6"/>')
    out.append(f'<path d="M-18,-66 C-10,-74 10,-74 18,-66 L14,-50 L-14,-50 Z" fill="{shade(PEWTER,.7)}" stroke="{BLACK}" stroke-width="1.4"/>')
    out.append('</g>')
    return ''.join(out)


def lightning(cy, R, seed=3):
    import random
    r = random.Random(seed)
    out = []
    for i in range(7):
        a = 2 * math.pi * i / 7 + r.uniform(-.2, .2)
        pts = [(0, cy)]
        for j in range(1, 6):
            d = R * j / 5 * r.uniform(.85, 1.05)
            aa = a + r.uniform(-.25, .25)
            pts.append((math.cos(aa) * d, cy + math.sin(aa) * d))
        d = "M" + " L".join(f"{x:.0f},{y:.0f}" for x, y in pts)
        out.append(f'<path d="{d}" fill="none" stroke="{LAPIS}" stroke-width="9" opacity=".35"/><path d="{d}" fill="none" stroke="{GHOST}" stroke-width="2.6"/>')
    return ''.join(out)


def ophan(s, cy, R, with_wings=True, echoes=True, wings_scale=1.0):
    """The whole being in cm, centred at (0, cy), great wheel radius R."""
    out = []
    halo = s.rg(LAPIS, LAPIS, .5, 0, .5, .5, .5)
    core = s.rg("#F2EEFF", LAPIS, 1, 0, .5, .5, .5)
    out.append(f'<circle cx="0" cy="{cy}" r="{R*1.45:.0f}" fill="{halo}"/>')
    W = wings_scale
    if with_wings:
        # the two spread wings, up and out; the two covering the feet, down
        out.append(wing(s, (-R * .25, cy - R * .2), (-R * 1.85 * W, cy - R * 1.35 * W), R * .55 * W, 11, flip=1, eyes=4))
        out.append(wing(s, (R * .25, cy - R * .2), (R * 1.85 * W, cy - R * 1.35 * W), R * .55 * W, 11, flip=-1, eyes=4))
        out.append(wing(s, (-R * .2, cy + R * .3), (-R * .8 * W, cy + R * 1.28 * W), R * .4 * W, 8, flip=-1, tint=.8, eyes=2))
        out.append(wing(s, (R * .2, cy + R * .3), (R * .8 * W, cy + R * 1.28 * W), R * .4 * W, 8, flip=1, tint=.8, eyes=2))
    if echoes:   # it stands in several moments at once: faint copies a heartbeat early and late
        for dx, op in ((-R * .22, .16), (R * .22, .12)):
            out.append(f'<g transform="translate({dx:.0f},0)" opacity="{op}">'
                       f'<ellipse cx="0" cy="{cy}" rx="{R}" ry="{R}" fill="none" stroke="{GHOST}" stroke-width="{R*.06:.0f}"/>'
                       f'<ellipse cx="0" cy="{cy}" rx="{R*.24:.0f}" ry="{R}" fill="none" stroke="{GHOST}" stroke-width="{R*.05:.0f}"/></g>')
    # back halves first: the tilted wheel and the edge-on wheel behind, then the core, then the great wheel
    out.append(rim(s, R * .92, R * .3, -16, R * .07, cy, 14, R * .07, back=True))
    out.append(f'<circle cx="0" cy="{cy}" r="{R*.42:.0f}" fill="{core}"/>')
    out.append(lightning(cy, R * .95))
    out.append(faces(s, cy, k=R / 300))
    out.append(rim(s, R * .24, R, 0, R * .065, cy, 12, R * .065, marks=0))
    out.append(rim(s, R, R, 0, R * .085, cy, 28, R * .085, marks=48))
    if with_wings:
        # the two wings that cover its face, crossed low over the core so only the man's brow shows
        out.append(wing(s, (-R * .55, cy - R * .1), (R * .6, cy + R * .42), R * .3, 7, flip=1, tint=.92, eyes=2))
        out.append(wing(s, (R * .55, cy - R * .1), (-R * .6, cy + R * .42), R * .3, 7, flip=-1, tint=.92, eyes=2))
    return ''.join(out)


def engraved_years(cy, R, size):
    """The four Ages' years engraved round the great rim, in portal light."""
    pid = f"yearRing{int(R)}"
    rr = R * 1.0
    d = f"M{-rr},{cy} A{rr},{rr} 0 1,1 {rr},{cy} A{rr},{rr} 0 1,1 {-rr},{cy}"
    txt = "   ·   ".join(AGES) + "   ·   "
    return (f'<path id="{pid}" d="{d}" fill="none"/>'
            f'<text font-family="{MONO}" font-size="{size}" fill="{GHOST}" letter-spacing="{size*.4:.1f}" opacity=".9">'
            f'<textPath href="#{pid}" startOffset="2%">{txt}{txt}</textPath></text>')


def floor_ripples(R, n=3):
    out = []
    for i in range(n):
        rx = R * (1.0 + .45 * i)
        out.append(f'<ellipse cx="0" cy="0" rx="{rx:.0f}" ry="{rx*.08:.0f}" fill="none" stroke="{LAPIS}" stroke-width="{4-i}" opacity="{.6-.15*i:.2f}"/>')
        for h in range(12):
            a = 2 * math.pi * h / 12
            out.append(f'<path d="M{math.cos(a)*rx:.0f},{math.sin(a)*rx*.08:.1f} l0,-6" stroke="{LAPIS}" stroke-width="2" opacity="{.5-.12*i:.2f}"/>')
    return ''.join(out)


# ─────────────────────────────── sheet 1: the whole being at true scale ───────────────────────────────
def sheet_wheels():
    s = Sheet("ophan-v2-1-the-wheels")
    K = 0.55          # px per cm: 1 m = 55 px
    cx = 500
    R = 290           # great wheel radius: 5.8 m across
    cy = -(R + 160)   # its lowest rim 1.6 m off the floor, the lower wings below that
    f = [f'<g transform="translate({cx},690) scale({K})">', floor_ripples(R)]
    f.append(ophan(s, cy, R))
    f.append(engraved_years(cy, R * 1.0, 16))
    f.append('</g>')
    P = lambda x, y: (round(cx + K * x, 1), round(690 + K * y, 1))
    top = cy - R * 1.35 - R * .55
    C = [callout(*P(R * .7, cy - R * .7), 900, 150, "The great wheel, 5.8 m across", "48 hour marks; the four Ages' years"),
         callout(*P(-R * .05, cy - R), 900, 205, "A wheel within a wheel", "at right angles: it goes any way"),
         callout(*P(R * .97, cy + R * .1), 900, 260, "Rims full of eyes, all round", "each looking its own way"),
         callout(*P(0, cy - R * .2), 900, 315, "Four faces: man, lion, ox, eagle", "two wings veil them"),
         callout(*P(R * 1.5, cy - R * 1.2), 900, 370, "Six wings, as the seraphim", "two spread, 11 m tip to tip"),
         callout(*P(R * .5, cy + R * 1.2), 900, 425, "Two wings cover its feet", "it never touches the floor"),
         callout(*P(-R * .35, cy + R * .6), 320, 470, "Echoes: it is a heartbeat early", "and late, all at once", side="left"),
         callout(*P(R * 1.1, -2), 900, 480, "The floor keeps time", "rings of hour marks spread out")]
    rows = [("Size", "9.6 m from the lower wing tips to the upper; 11 m wide. The castle's tallest ceiling is 5.2 m. It does not fit, and it does not need to.")]
    head_ = frame_open(s, "FEAR PASS · THE CREATURE · ROUND 2", "The Ophan of Hours", "H 9.6 m · wings 11 m · lapis: the owner's call", "1 m = 55 px · ground y 690", glow_cx="42%", glow_cy="45%", glow_col="#1E1A2A")
    ladder = metre_ladder(px_per_m=55, top_m=10, step=1)
    parts = [ladder, human(cx=150, px_per_m=55), view_label(cx, "FRONT"), ''.join(f), ''.join(C), notes(900, 540, rows, width=260)]
    write_plate(s, head_, parts, palette([("tarnished pewter", PEWTER), ("wings, eyes (vellum)", BONE), ("pupil (bone black)", BLACK), ("portal light (lapis)", LAPIS), ("lightning, core", GHOST), ("hollow (deep lapis)", DEEP)]))


# ─────────────────────────────── sheet 2: in the castle, and the rim up close ───────────────────────────────
def stone(x, y, w, h, raw=False):
    """A wall block: coursed ashlar, or a raw quarry lump where the Ophan has pulled it back in time."""
    if not raw:
        return f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="#4A4236" stroke="#2A251D" stroke-width="1.2"/>'
    return (f'<path d="M{x+3},{y+5} L{x+w*.4:.1f},{y-3} L{x+w-2},{y+4} L{x+w+3},{y+h*.6:.1f} L{x+w-6},{y+h+3} L{x+6},{y+h-1} L{x-3},{y+h*.5:.1f} Z" '
            f'fill="#5E5446" stroke="{LAPIS}" stroke-width="1.6" opacity=".9"/>')


def sheet_castle():
    s = Sheet("ophan-v2-2-in-the-castle")
    K = 0.5           # 1 m = 50 px
    x0 = 70           # left face of the cell
    G = 690
    cell = 1200       # 12 m
    wall = 120        # end walls 1.2 m thick
    st1 = 460         # Keep clear height 4.6 m
    slab = 30
    out = []
    # the two storeys of a Keep cell in section: floors, end walls, the storey above
    def X(cm): return x0 + cm * K
    def Y(cm): return G - cm * K
    out.append(f'<rect x="{X(0)}" y="{Y(2*st1+slab)}" width="{cell*K}" height="{(2*st1+slab)*K}" fill="#1A1712"/>')
    R = 290; cyw = 560   # the Ophan's centre, 5.6 m up: across both storeys and out through the roof
    cx = 600
    # masonry: blocks in the end walls and the slab; blocks the rim passes through are raw
    def seg_dist(px, py, a, b):
        (ax, ay), (bx, by) = a, b
        vx, vy = bx - ax, by - ay
        u = max(0, min(1, ((px - ax) * vx + (py - ay) * vy) / (vx * vx + vy * vy)))
        return math.hypot(px - ax - u * vx, py - ay - u * vy)
    wings_up = [((cx - R * .25, cyw + R * .2), (cx - R * 1.85, cyw + R * 1.35)), ((cx + R * .25, cyw + R * .2), (cx + R * 1.85, cyw + R * 1.35))]

    def near_rim(px, py):
        d = math.hypot(px - cx, py - cyw)
        return abs(d - R) < 50 or any(seg_dist(px, py, a, b) < R * .3 for a, b in wings_up)
    blocks = []
    for (wx0, wx1) in ((0, wall), (cell - wall, cell)):
        for row in range(int((2 * st1 + 2 * slab) / 40) + 1):
            yy = row * 40
            off = 30 if row % 2 else 0
            xx = wx0 - off
            while xx < wx1:
                bw = 60
                a, b_ = max(xx, wx0), min(xx + bw, wx1)
                if b_ > a:
                    blocks.append(stone(X(a), Y(yy + 40), (b_ - a) * K, 40 * K, raw=near_rim((a + b_) / 2, yy + 20)))
                xx += bw
    for yy in (0, st1, 2 * st1 + slab):   # the ground floor slab, the slab between the storeys, the roof
        for xx in range(wall, cell - wall, 60):
            blocks.append(stone(X(xx), Y(yy + slab), 60 * K, slab * K, raw=yy > 0 and near_rim(xx + 30, yy + slab / 2)))
    out.append(''.join(blocks))
    # the Ophan, wings folded close, wheels passing through the slab and both end walls
    out.append(f'<g transform="translate({X(cx)},{Y(0)}) scale({K})">')
    out.append(ophan(s, -cyw, R, with_wings=True, echoes=True, wings_scale=1.0))
    out.append('</g>')
    # two of the company, in the doorway's dark, 1.80 m
    for hx in (X(180), X(260)):
        out.append(human(cx=hx, px_per_m=50, label=False, op=.75))
    out.append(text(X(220), Y(0) + 18, "two of you, 1.80 m", 9.5, "#9A9078", "middle"))
    out.append(text(X(cell / 2), Y(0) + 34, "KEEP CELL · 12 m · TWO STOREYS OF 4.6 m · 1 m = 50 px", 10, "#635C4C", "middle", extra=' letter-spacing="2"'))
    C = [callout(X(cell - 60), Y(cyw + R * 1.2), 740, 150, "Stone pulled back to the quarry", "where it passes; it closes after", side="left"),
         callout(X(cx + R * .92), Y(st1 + slab / 2), 740, 640, "Through floor, walls and roof", "it is larger than the house", side="left")]
    # the inset: a span of the great rim up close, an eye and an engraved year
    ix, iy, iw, ih = 760, 120, 400, 340
    s.clip(f"M{ix},{iy} h{iw} v{ih} h{-iw} Z", "rimInset")
    ins = [f'<rect x="{ix}" y="{iy}" width="{iw}" height="{ih}" fill="#17140F" stroke="#332D22"/>',
           f'<g clip-path="url(#rimInset)"><g transform="translate({ix+iw/2},{iy+ih+520}) scale(1.0)">',
           rim(s, 700, 700, 0, 150, 0, 60, 62, marks=96, eye_in=38),
           engraved_years(0, 700 + 40, 28),
           '</g></g>',
           text(ix + 14, iy + 22, "THE GREAT RIM, CLOSE", 10.5, "#635C4C", extra=' letter-spacing="2.5"'),
           text(ix + 14, iy + ih - 14, "rim 0.25 m deep · eyes 0.20 m · the four Ages' years in portal light", 9.5, "#9A9078")]
    rows = [("When it comes", "As the portal's clock runs low the floors begin to tick. Then the light, then the wheels, through whatever is in the way."),
            ("Why you cannot fight it", "It is larger than the castle and not wholly in one moment. A blow lands on where it was."),
            ("Its counter, proposed", "None. Carry what it asked for, or reach the portal before it reaches you.")]
    head_ = frame_open(s, "FEAR PASS · THE CREATURE · ROUND 2", "The Ophan in the castle", "larger than the room it is in", "lapis: the owner's call", glow_cx="35%", glow_cy="50%", glow_col="#1E1A2A")
    parts = [''.join(out), ''.join(C), ''.join(ins), notes(ix, 490, rows, width=400)]
    write_plate(s, head_, parts, '')


if __name__ == "__main__":
    sheet_wheels()
    sheet_castle()
