"""Helpers for the High Medieval concept sheets. Emits SVG strings on the art-bible frame."""
import random, math, colorsys
from xml.sax.saxutils import escape

MONO = "Overpass Mono, monospace"
SERIF = "Eczar, Georgia, serif"
AGE_NAME = "THE HIGH MEDIEVAL"
OUT = "/home/user/PlunderSpell/docs/art/concept/high/"


def hexrgb(h):
    h = h.lstrip('#'); return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def rgbhex(r, g, b):
    return '#%02X%02X%02X' % tuple(max(0, min(255, int(round(v)))) for v in (r, g, b))


def shade(h, f):
    """f < 1 darkens toward black-brown, f > 1 lightens toward vellum."""
    r, g, b = hexrgb(h)
    if f <= 1:
        return rgbhex(r * f, g * f, b * f * 0.97)
    t = f - 1
    vr, vg, vb = hexrgb('#DCD2BA')
    return rgbhex(r + (vr - r) * t, g + (vg - g) * t, b + (vb - b) * t)


class Sheet:
    def __init__(self, slug):
        self.slug = slug
        self.defs = []
        self.body = []
        self.over = []
        self.rng = random.Random(slug)
        self._gid = 0

    # ── defs ──
    def lg(self, c1, c2, x1=0, y1=0, x2=1, y2=0, mid=None, name=None):
        self._gid += 1
        gid = name or f"g{self._gid}"
        stops = f'<stop offset="0" stop-color="{c1}"/>'
        if mid:
            stops += f'<stop offset=".5" stop-color="{mid}"/>'
        stops += f'<stop offset="1" stop-color="{c2}"/>'
        self.defs.append(f'<linearGradient id="{gid}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{stops}</linearGradient>')
        return f"url(#{gid})"

    def form(self, base, horizontal=True, name=None):
        """Cylinder-ish form shading: light upper-left, core shadow right."""
        if horizontal:
            return self.lg(shade(base, 1.18), shade(base, 0.55), 0, 0, 1, 0, mid=base, name=name)
        return self.lg(shade(base, 1.18), shade(base, 0.55), 0, 0, 0, 1, mid=base, name=name)

    def rg(self, c1, c2, o1=1, o2=0, cx=.5, cy=.5, r=.5, name=None):
        self._gid += 1
        gid = name or f"r{self._gid}"
        self.defs.append(f'<radialGradient id="{gid}" cx="{cx}" cy="{cy}" r="{r}"><stop offset="0" stop-color="{c1}" stop-opacity="{o1}"/>'
                         f'<stop offset="1" stop-color="{c2}" stop-opacity="{o2}"/></radialGradient>')
        return f"url(#{gid})"

    def add(self, s):
        self.body.append(s)

    def top(self, s):
        self.over.append(s)

    # ── primitives ──
    def flecks(self, x0, y0, w, h, n, color, rmin=.4, rmax=1.4, op=.5, clip=None):
        out = []
        for _ in range(n):
            x = x0 + self.rng.random() * w; y = y0 + self.rng.random() * h
            r = rmin + self.rng.random() * (rmax - rmin)
            out.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{r:.2f}"/>')
        cp = f' clip-path="url(#{clip})"' if clip else ''
        return f'<g fill="{color}" opacity="{op}"{cp}>' + ''.join(out) + '</g>'

    def clip(self, d, cid):
        self.defs.append(f'<clipPath id="{cid}"><path d="{d}"/></clipPath>')
        return cid


def text(x, y, s, size=12, fill="#DCD2BA", anchor="start", family=MONO, extra=""):
    return f'<text x="{x}" y="{y}" font-family="{family}" font-size="{size}" fill="{fill}" text-anchor="{anchor}"{extra}>{escape(s)}</text>'


def callout(px, py, lx, ly, label, sub, side="right"):
    """Dot at the part (px,py); label at (lx,ly). side=right: text starts at lx; left: text ends at lx."""
    anchor = "start" if side == "right" else "end"
    ex = lx - 6 if side == "right" else lx + 6
    elbow = ex - 14 if side == "right" else ex + 14
    s = (f'<path d="M{px},{py} L{elbow},{ly - 4} L{ex},{ly - 4}" fill="none" stroke="#9A9078" stroke-width=".8"/>'
         f'<circle cx="{px}" cy="{py}" r="2.6" fill="#DCD2BA" stroke="#14120E" stroke-width="1"/>')
    s += text(lx, ly, label, 12, "#DCD2BA", anchor)
    if sub:
        s += text(lx, ly + 14, sub, 10.5, "#9A9078", anchor)
    return s


def palette(materials, y=748):
    out = [f'<g id="palette" font-family="{MONO}" font-size="10" fill="#9A9078">']
    n = len(materials)
    step = min(170, 1120 // n)
    for i, (name, hx) in enumerate(materials):
        x = 40 + i * step
        out.append(f'<rect x="{x}" y="{y}" width="34" height="18" fill="{hx}" stroke="#332D22" stroke-width=".6"/>'
                   f'<text x="{x + 40}" y="{y + 8}">{hx}</text><text x="{x + 40}" y="{y + 19}" fill="#635C4C">{escape(name)}</text>')
    out.append('</g>')
    return ''.join(out)


def frame_open(s, kicker, name, tr1, tr2, glow_cx="50%", glow_cy="70%", glow_col="#2A2012"):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 800" width="1200" height="800">\n'
            f'<defs>\n<pattern id="grid" width="55" height="55" patternUnits="userSpaceOnUse" x="0" y="690">'
            f'<path d="M55 0 H0 V55" fill="none" stroke="#262119" stroke-width="1"/></pattern>\n'
            f'<radialGradient id="glow" cx="{glow_cx}" cy="{glow_cy}" r="60%"><stop offset="0" stop-color="{glow_col}" stop-opacity=".9"/>'
            f'<stop offset="1" stop-color="#14120E" stop-opacity="0"/></radialGradient>\n'
            + '\n'.join(s.defs) + '\n</defs>\n'
            f'<rect width="1200" height="800" fill="#14120E"/>\n<rect width="1200" height="800" fill="url(#glow)"/>\n'
            f'<rect x="0" y="120" width="1200" height="570" fill="url(#grid)"/>\n'
            f'<line x1="40" y1="690" x2="1160" y2="690" stroke="#635C4C" stroke-width="1.5"/>\n'
            f'<rect x="12" y="12" width="1176" height="776" fill="none" stroke="#332D22" stroke-width="1"/>\n'
            + text(40, 54, kicker, 12, "#635C4C", extra=' letter-spacing="3"') + '\n'
            + f'<text x="40" y="96" font-family="{SERIF}" font-size="38" font-weight="700" fill="#DCD2BA">{escape(name)}</text>\n'
            + text(1160, 54, tr1, 12, "#9A9078", "end") + '\n' + text(1160, 76, tr2, 12, "#9A9078", "end") + '\n')


def metre_ladder(x=60, px_per_m=220, ground=690, top_m=2.5, step=0.5):
    out = [f'<g font-family="{MONO}" font-size="11" fill="#635C4C" stroke="#635C4C">',
           f'<line x1="{x}" y1="{ground}" x2="{x}" y2="{ground - top_m * px_per_m}" stroke-width="1"/>']
    m = 0.0
    while m <= top_m + 1e-6:
        y = ground - m * px_per_m
        lab = f"{m:g}" if m != int(m) or step >= 1 else (f"{m:.1f}" if m else "0")
        out.append(f'<line x1="{x - 6}" y1="{y:.1f}" x2="{x + 6}" y2="{y:.1f}"/><text x="{x + 12}" y="{y + 4:.1f}" stroke="none">{lab}</text>')
        m += step
    out.append('</g>')
    return ''.join(out)


def human(cx=160, ground=690, px_per_m=220, label=True, op=.45):
    """The template's 1.80 m standard human, rescaled to px_per_m, feet on ground at cx."""
    k = px_per_m / 220.0
    s = (f'<g transform="translate({cx},{ground}) scale({k}) translate(-160,-690)" fill="#635C4C" opacity="{op}">'
         '<ellipse cx="160" cy="320" rx="20" ry="26"/>'
         '<path d="M160 346 C138 350 126 360 124 380 L118 500 L130 502 L138 410 L140 520 L142 690 L156 690 L160 540 L164 690 L178 690 L180 520 L182 410 L190 502 L202 500 L196 380 C194 360 182 350 160 346 Z"/></g>')
    if label:
        s += text(cx, ground + 22, "1.80 m", 10, "#635C4C", "middle")
    return s


def view_label(x, s, y=730):
    return text(x, y, s, 11, "#635C4C", "middle", extra=' letter-spacing="3"')


def write(sheet, head, parts, tail_palette):
    svg = head + '\n'.join(parts) + '\n' + tail_palette + '\n</svg>\n'
    path = OUT + sheet.slug + '.svg'
    with open(path, 'w') as f:
        f.write(svg)
    n = svg.count('<') - svg.count('</')
    print(f"wrote {path}  (~{n} elements)")


def tube(p0, p1, w0, w1):
    """Tapered tube from p0 to p1 with rounded ends, half-widths w0/w1 (path d)."""
    (x0, y0), (x1, y1) = p0, p1
    dx, dy = x1 - x0, y1 - y0; L = math.hypot(dx, dy); nx, ny = -dy / L, dx / L
    a = (x0 + nx * w0, y0 + ny * w0); b = (x1 + nx * w1, y1 + ny * w1); c = (x1 - nx * w1, y1 - ny * w1); d = (x0 - nx * w0, y0 - ny * w0)
    return (f"M{a[0]:.1f},{a[1]:.1f} L{b[0]:.1f},{b[1]:.1f} Q{x1+dx/L*w1:.1f},{y1+dy/L*w1:.1f} {c[0]:.1f},{c[1]:.1f} "
            f"L{d[0]:.1f},{d[1]:.1f} Q{x0-dx/L*w0:.1f},{y0-dy/L*w0:.1f} {a[0]:.1f},{a[1]:.1f} Z")


def rings(p0, p1, w0, w1, n, color, sw=.35):
    (x0, y0), (x1, y1) = p0, p1
    dx, dy = x1 - x0, y1 - y0; L = math.hypot(dx, dy); nx, ny = -dy / L, dx / L
    out = []
    for i in range(1, n):
        t = i / n; x = x0 + dx * t; y = y0 + dy * t; w = w0 + (w1 - w0) * t
        out.append(f'<path d="M{x+nx*w:.1f},{y+ny*w:.1f} Q{x+dx/L*1.5:.1f},{y+dy/L*1.5:.1f} {x-nx*w:.1f},{y-ny*w:.1f}" fill="none" stroke="{color}" stroke-width="{sw}"/>')
    return ''.join(out)


def mail_pattern(s, pid, base, size=1.3):
    """Riveted-mail texture as a pattern in the referencing element's user space."""
    dark = shade(base, .45); lite = shade(base, 1.35)
    h = size * .8
    s.defs.append(f'<pattern id="{pid}" width="{size}" height="{h:.2f}" patternUnits="userSpaceOnUse">'
                  f'<rect width="{size}" height="{h:.2f}" fill="{base}"/>'
                  f'<path d="M0,{h*.55:.2f} A{size/2:.2f},{h*.5:.2f} 0 0 1 {size},{h*.55:.2f}" fill="none" stroke="{dark}" stroke-width="{size*.22:.2f}"/>'
                  f'<path d="M{size*.2:.2f},{h*.35:.2f} A{size*.3:.2f},{h*.3:.2f} 0 0 1 {size*.6:.2f},{h*.2:.2f}" fill="none" stroke="{lite}" stroke-width="{size*.12:.2f}" opacity=".7"/>'
                  f'</pattern>')
    return f"url(#{pid})"


def shadow_overlay(clip_id, d, op=.3):
    return f'<path d="{d}" fill="#14120E" opacity="{op}" clip-path="url(#{clip_id})"/>'


def limb(pts, ws, fill, outline="#14120E", ow=.45):
    """Union of tapered tubes along pts: dark outline underlay then fill, so joints read as one limb."""
    under, over = [], []
    for i in range(len(pts) - 1):
        under.append(f'<path d="{tube(pts[i], pts[i+1], ws[i]+ow, ws[i+1]+ow)}"/>')
        over.append(f'<path d="{tube(pts[i], pts[i+1], ws[i], ws[i+1])}"/>')
    return f'<g fill="{outline}">' + ''.join(under) + f'</g><g fill="{fill}">' + ''.join(over) + '</g>'


def vcallout(px, py, lx, ly, label, sub):
    """Label block above a part: text at (lx,ly), leader from the block's lower-left to the part."""
    bx, by = lx + 4, ly + 20
    return (f'<path d="M{lx},{by} L{bx+60},{by} M{bx+30},{by} L{px},{py}" fill="none" stroke="#9A9078" stroke-width=".8"/>'
            f'<circle cx="{px}" cy="{py}" r="2.6" fill="#DCD2BA" stroke="#14120E" stroke-width="1"/>'
            + text(lx, ly, label, 12, "#DCD2BA") + (text(lx, ly + 13, sub, 10.5, "#9A9078") if sub else ''))


class Cam:
    """Orthographic camera for three-quarter hero views. World: X right, Y away, Z up (metres)."""
    def __init__(self, ox, oy, scale, yaw=32, pitch=22):
        self.ox, self.oy, self.k = ox, oy, scale
        self.cy, self.sy = math.cos(math.radians(yaw)), math.sin(math.radians(yaw))
        self.cp, self.sp = math.cos(math.radians(pitch)), math.sin(math.radians(pitch))

    def p(self, X, Y, Z):
        u = X * self.cy + Y * self.sy
        d = -X * self.sy + Y * self.cy
        return (self.ox + u * self.k, self.oy - (Z * self.cp + d * self.sp) * self.k)

    def poly(self, pts, **attrs):
        a = ' '.join(f'{k.replace("_","-")}="{v}"' for k, v in attrs.items())
        return '<path d="M' + ' L'.join(f'{x:.1f},{y:.1f}' for x, y in (self.p(*q) for q in pts)) + f' Z" {a}/>'

    def face_matrix(self, O, E1, E2, w, h):
        """SVG matrix mapping design coords (0..w, 0..h, y down) onto the face O + E1*(x/w) + E2*(y/h)."""
        o = self.p(*O)
        a = self.p(O[0] + E1[0], O[1] + E1[1], O[2] + E1[2]); b = self.p(O[0] + E2[0], O[1] + E2[1], O[2] + E2[2])
        return f'matrix({(a[0]-o[0])/w:.5f},{(a[1]-o[1])/w:.5f},{(b[0]-o[0])/h:.5f},{(b[1]-o[1])/h:.5f},{o[0]:.2f},{o[1]:.2f})'

    def box(self, x0, y0, z0, x1, y1, z1, top, front, side, stroke="#14120E", sw=.8):
        """Visible faces from a front-right-above camera: front (y0), right (x1), top (z1)."""
        o = [self.poly([(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)], fill=front, stroke=stroke, stroke_width=sw, stroke_linejoin="round"),
             self.poly([(x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1)], fill=side, stroke=stroke, stroke_width=sw, stroke_linejoin="round"),
             self.poly([(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)], fill=top, stroke=stroke, stroke_width=sw, stroke_linejoin="round")]
        return ''.join(o)


def scale_bar(x, y, px, label):
    """A bar px long with 5 ticks and a label: e.g. '0.1 m = 50 px'."""
    o = [f'<g stroke="#9A9078" stroke-width="1"><line x1="{x}" y1="{y}" x2="{x+px}" y2="{y}"/>']
    for i in range(6):
        tx = x + px * i / 5; h = 6 if i in (0, 5) else 3
        o.append(f'<line x1="{tx:.1f}" y1="{y-h}" x2="{tx:.1f}" y2="{y+h}"/>')
    o.append(f'<rect x="{x}" y="{y-2}" width="{px/2:.1f}" height="4" fill="#9A9078" stroke="none"/></g>')
    o.append(text(x + px + 10, y + 4, label, 10.5, "#9A9078"))
    return ''.join(o)


def grab(x, y, label="GRAB", dx=10, dy=-8, anchor="start"):
    return (f'<circle cx="{x}" cy="{y}" r="7" fill="none" stroke="#5FA288" stroke-width="2"/>'
            f'<circle cx="{x}" cy="{y}" r="2.2" fill="#5FA288"/>'
            + text(x + dx, y + dy, label, 10, "#5FA288", anchor, extra=' letter-spacing="1.5"'))


def fracture(d, sw=1.6):
    return f'<path d="{d}" fill="none" stroke="#C4542E" stroke-width="{sw}" stroke-dasharray="5 4" stroke-linecap="round"/>'


def item_frame(s, name, worth, bulk, tr1, tr2, glow_cx="35%", glow_cy="60%"):
    return frame_open(s, f"{AGE_NAME} · PLUNDER · {worth} COIN · {bulk} ST", name, tr1, tr2, glow_cx, glow_cy)


def ashlar(s, pid, bw, bh, base, x0=0, y0=0):
    """Coursed ashlar texture: running bond, mortar lines, a light top edge per block."""
    s.defs.append(f'<pattern id="{pid}" width="{bw}" height="{bh*2}" patternUnits="userSpaceOnUse" x="{x0}" y="{y0}">'
                  f'<rect width="{bw}" height="{bh*2}" fill="{base}"/>'
                  f'<rect x="1" y="1" width="{bw-2}" height="{bh-2}" fill="{shade(base,1.07)}"/>'
                  f'<rect x="{-bw/2+1}" y="{bh+1}" width="{bw-2}" height="{bh-2}" fill="{shade(base,.93)}"/>'
                  f'<rect x="{bw/2+1}" y="{bh+1}" width="{bw-2}" height="{bh-2}" fill="{shade(base,.97)}"/>'
                  f'<path d="M0,0 H{bw} M0,{bh} H{bw} M0,0 V{bh} M{bw/2},{bh} V{bh*2}" stroke="{shade(base,.55)}" stroke-width="1"/>'
                  f'<path d="M2,2 H{bw*.6}" stroke="{shade(base,1.3)}" stroke-width=".8" opacity=".6"/></pattern>')
    return f"url(#{pid})"


def hatch(s, pid, color, gap=6, sw=1):
    s.defs.append(f'<pattern id="{pid}" width="{gap}" height="{gap}" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
                  f'<rect width="{gap}" height="{gap}" fill="{shade(color,.8)}"/><line x1="0" y1="0" x2="0" y2="{gap}" stroke="{shade(color,.45)}" stroke-width="{sw}"/></pattern>')
    return f"url(#{pid})"


def plan_grid(x0, y0, K, label="12 × 12 m CELL · 1 m = {K} px"):
    o = [f'<rect x="{x0}" y="{y0}" width="{12*K}" height="{12*K}" fill="#18150F" stroke="#635C4C" stroke-width="1.2" stroke-dasharray="6 4"/>']
    for i in range(13):
        h = 7 if i % 3 == 0 else 4
        o.append(f'<line x1="{x0+i*K}" y1="{y0}" x2="{x0+i*K}" y2="{y0-h}" stroke="#635C4C"/><line x1="{x0}" y1="{y0+i*K}" x2="{x0-h}" y2="{y0+i*K}" stroke="#635C4C"/>')
        if i % 3 == 0:
            o.append(text(x0 + i * K, y0 - 10, f"{i}", 9, "#635C4C", "middle"))
            o.append(text(x0 - 10, y0 + i * K + 3, f"{i}", 9, "#635C4C", "end"))
    o.append(text(x0 + 6 * K, y0 + 12 * K + 18, label.format(K=K), 10.5, "#635C4C", "middle", extra=' letter-spacing="2"'))
    return ''.join(o)


def sock(x, y, label, anchor="start", col="#DCD2BA"):
    return (f'<rect x="{x-3}" y="{y-3}" width="6" height="6" fill="#DCD2BA" stroke="#14120E" stroke-width=".8"/>'
            + text(x + (6 if anchor == "start" else -6), y + 3.5, label, 9.5, col, anchor, extra=' letter-spacing="1"'))
