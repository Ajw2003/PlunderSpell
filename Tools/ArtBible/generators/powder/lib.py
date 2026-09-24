"""Tiny SVG concept-sheet library for the Powder Age art bible."""
import math
import random

GROUND = 690
SCALE = 220  # enemy scale px per metre

FRAME_MID = "#635C4C"
FRAME_TXT = "#9A9078"
VELLUM = "#DCD2BA"
INK = "#0E0C0A"
LIGHT = "#F2D9A8"  # warm firelight tint used only for mixing highlights
MONO = "Overpass Mono, monospace"
SERIF = "Eczar, Georgia, serif"


def hx(c):
    c = c.lstrip("#")
    return tuple(int(c[i:i + 2], 16) for i in (0, 2, 4))


def to_hex(rgb):
    return "#" + "".join(f"{max(0, min(255, int(round(v)))):02X}" for v in rgb)


def mix(a, b, t):
    a, b = hx(a), hx(b)
    return to_hex(tuple(a[i] + (b[i] - a[i]) * t for i in range(3)))


def lit(c, t=0.3):
    return mix(c, LIGHT, t)


def dk(c, t=0.4):
    return mix(c, "#000000", t)


def fmt(v):
    s = f"{v:.1f}"
    return s[:-2] if s.endswith(".0") else s


def smooth_path(pts, closed=True, tension=0.5):
    """Catmull-Rom through pts -> cubic bezier path string."""
    n = len(pts)
    if n < 3:
        return "M" + " L".join(f"{fmt(x)} {fmt(y)}" for x, y in pts) + (" Z" if closed else "")
    d = [f"M{fmt(pts[0][0])} {fmt(pts[0][1])}"]
    rng = range(n) if closed else range(n - 1)
    for i in rng:
        p0 = pts[(i - 1) % n] if (closed or i > 0) else pts[0]
        p1 = pts[i]
        p2 = pts[(i + 1) % n]
        p3 = pts[(i + 2) % n] if (closed or i + 2 < n) else pts[-1]
        k = tension / 3 * 2
        c1 = (p1[0] + (p2[0] - p0[0]) * k / 2, p1[1] + (p2[1] - p0[1]) * k / 2)
        c2 = (p2[0] - (p3[0] - p1[0]) * k / 2, p2[1] - (p3[1] - p1[1]) * k / 2)
        d.append(f"C{fmt(c1[0])} {fmt(c1[1])} {fmt(c2[0])} {fmt(c2[1])} {fmt(p2[0])} {fmt(p2[1])}")
    if closed:
        d.append("Z")
    return " ".join(d)


def poly_path(pts, closed=True):
    s = "M" + " L".join(f"{fmt(x)} {fmt(y)}" for x, y in pts)
    return s + (" Z" if closed else "")


class Sheet:
    def __init__(self, seed=1):
        self.defs = []
        self.body = []
        self.n = 0
        self.gcache = {}
        self.rnd = random.Random(seed)

    def uid(self, p="e"):
        self.n += 1
        return f"{p}{self.n}"

    def add(self, s):
        self.body.append(s)

    # ── gradients ──
    def lin(self, stops, x1=0, y1=0, x2=1, y2=0, key=None):
        if key and key in self.gcache:
            return self.gcache[key]
        gid = self.uid("g")
        st = "".join(f'<stop offset="{o}" stop-color="{c}"' + (f' stop-opacity="{a}"' if a != 1 else "") + "/>"
                     for o, c, a in [(s + (1,))[:3] if len(s) == 2 else s for s in stops])
        self.defs.append(f'<linearGradient id="{gid}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{st}</linearGradient>')
        if key:
            self.gcache[key] = gid
        return gid

    def rad(self, stops, cx=0.5, cy=0.5, r=0.5, fx=None, fy=None, key=None, user=False):
        if key and key in self.gcache:
            return self.gcache[key]
        gid = self.uid("g")
        st = "".join(f'<stop offset="{o}" stop-color="{c}"' + (f' stop-opacity="{a}"' if a != 1 else "") + "/>"
                     for o, c, a in [(s + (1,))[:3] if len(s) == 2 else s for s in stops])
        extra = f' fx="{fx}" fy="{fy}"' if fx is not None else ""
        units = ' gradientUnits="userSpaceOnUse"' if user else ""
        self.defs.append(f'<radialGradient id="{gid}" cx="{cx}" cy="{cy}" r="{r}"{extra}{units}>{st}</radialGradient>')
        if key:
            self.gcache[key] = gid
        return gid

    def form(self, base, direction="h", light=0.32, dark=0.5):
        """Cylindrical form gradient: warm-lit left, base, dark right."""
        key = f"form{base}{direction}{light}{dark}"
        if direction == "h":
            return self.lin([(0, lit(base, light)), (0.35, base), (0.8, dk(base, dark * 0.6)), (1, dk(base, dark))], key=key)
        if direction == "v":
            return self.lin([(0, lit(base, light)), (0.4, base), (1, dk(base, dark))], 0, 0, 0, 1, key=key)
        if direction == "d":
            return self.lin([(0, lit(base, light)), (0.45, base), (1, dk(base, dark))], 0, 0, 1, 1, key=key)
        if direction == "r":  # right-lit (for profile views facing right)
            return self.lin([(0, dk(base, dark)), (0.25, dk(base, dark * 0.5)), (0.65, base), (1, lit(base, light))], key=key)
        if direction == "sphere":
            return self.rad([(0, lit(base, light + 0.15)), (0.45, base), (1, dk(base, dark))], 0.5, 0.5, 0.62, 0.32, 0.3, key=key)
        raise ValueError(direction)

    # ── primitives ──
    def path(self, d, fill="none", stroke=None, sw=1, op=None, extra=""):
        s = f'<path d="{d}" fill="{fill}"'
        if stroke:
            s += f' stroke="{stroke}" stroke-width="{sw}" stroke-linejoin="round" stroke-linecap="round"'
        if op is not None:
            s += f' opacity="{op}"'
        self.add(s + extra + "/>")

    def shape(self, pts, base, smooth=True, direction="h", outline=True, light=0.32, dark=0.5, op=None, sw=1.1, tension=0.5):
        d = smooth_path(pts, True, tension) if smooth else poly_path(pts)
        fill = base if direction is None else f"url(#{self.form(base, direction, light, dark)})"
        self.path(d, fill, INK if outline else None, sw, op)
        return d

    def flat(self, pts, color, smooth=True, op=None, stroke=None, sw=1, tension=0.5):
        d = smooth_path(pts, True, tension) if smooth else poly_path(pts)
        self.path(d, color, stroke, sw, op)
        return d

    def line(self, pts, color, sw=1.0, op=None, smooth=True, dash=None, tension=0.5):
        d = smooth_path(pts, False, tension) if smooth and len(pts) > 2 else poly_path(pts, False)
        extra = f' stroke-dasharray="{dash}"' if dash else ""
        self.path(d, "none", color, sw, op, extra)

    def circle(self, cx, cy, r, fill, stroke=None, sw=1, op=None):
        s = f'<circle cx="{fmt(cx)}" cy="{fmt(cy)}" r="{fmt(r)}" fill="{fill}"'
        if stroke:
            s += f' stroke="{stroke}" stroke-width="{sw}"'
        if op is not None:
            s += f' opacity="{op}"'
        self.add(s + "/>")

    def ellipse(self, cx, cy, rx, ry, fill, stroke=None, sw=1, op=None, rot=0):
        s = f'<ellipse cx="{fmt(cx)}" cy="{fmt(cy)}" rx="{fmt(rx)}" ry="{fmt(ry)}" fill="{fill}"'
        if rot:
            s += f' transform="rotate({fmt(rot)} {fmt(cx)} {fmt(cy)})"'
        if stroke:
            s += f' stroke="{stroke}" stroke-width="{sw}"'
        if op is not None:
            s += f' opacity="{op}"'
        self.add(s + "/>")

    def rect(self, x, y, w, h, fill, stroke=None, sw=1, op=None, rx=0):
        s = f'<rect x="{fmt(x)}" y="{fmt(y)}" width="{fmt(w)}" height="{fmt(h)}" fill="{fill}"'
        if rx:
            s += f' rx="{fmt(rx)}"'
        if stroke:
            s += f' stroke="{stroke}" stroke-width="{sw}"'
        if op is not None:
            s += f' opacity="{op}"'
        self.add(s + "/>")

    def text(self, x, y, s, size=12, fill=VELLUM, anchor="start", family=MONO, weight=None, ls=None, op=None, halo=True):
        a = f' text-anchor="{anchor}"' if anchor != "start" else ""
        # A dark outline painted under the glyphs keeps a label readable where a leader line or drawing passes behind it.
        a += ' stroke="#14120E" stroke-width="3.5" stroke-linejoin="round" paint-order="stroke"' if halo else ""
        w = f' font-weight="{weight}"' if weight else ""
        l = f' letter-spacing="{ls}"' if ls else ""
        o = f' opacity="{op}"' if op is not None else ""
        s = s.replace("&", "&amp;").replace("<", "&lt;")
        self.add(f'<text x="{fmt(x)}" y="{fmt(y)}" font-family="{family}" font-size="{size}"{a}{w}{l}{o} fill="{fill}">{s}</text>')

    # ── clipping groups ──
    def clip_open(self, d):
        cid = self.uid("c")
        self.defs.append(f'<clipPath id="{cid}"><path d="{d}"/></clipPath>')
        self.add(f'<g clip-path="url(#{cid})">')

    def close(self):
        self.add("</g>")

    def group(self, extra=""):
        self.add(f"<g {extra}>")

    # ── painterly helpers ──
    def flecks(self, x0, y0, x1, y1, n, color, rmin=0.6, rmax=1.8, op=(0.2, 0.55)):
        for _ in range(n):
            x = self.rnd.uniform(x0, x1)
            y = self.rnd.uniform(y0, y1)
            r = self.rnd.uniform(rmin, rmax)
            self.ellipse(x, y, r, r * self.rnd.uniform(0.5, 1.0), color,
                         op=round(self.rnd.uniform(*op), 2), rot=self.rnd.uniform(0, 180))

    def shadow_side(self, d, x_from, width, color="#000000", op=0.35):
        """Darken the right part of a shape (clip to the shape, overlay a soft band)."""
        self.clip_open(d)
        g = self.lin([(0, color, 0), (1, color, op)])
        self.rect(x_from, 0, width, 800, f"url(#{g})")
        self.close()

    def rivets(self, pts, r=1.8, base="#6E7E92"):
        for x, y in pts:
            self.circle(x, y, r, dk(base, 0.5))
            self.circle(x - r * 0.3, y - r * 0.3, r * 0.5, lit(base, 0.4), op=0.8)

    def glow(self, cx, cy, r, color="#C4542E", op=0.55):
        g = self.rad([(0, color, op), (0.4, color, op * 0.4), (1, color, 0)])
        self.circle(cx, cy, r, f"url(#{g})")

    # ── sheet frame ──
    def render(self, age_label, name, tr1, tr2, palette, view_labels=None, ladder="enemy", human=True, ground=True,
               glow_c=None):
        out = ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 800" width="1200" height="800">', "<defs>",
               '<pattern id="grid" width="55" height="55" patternUnits="userSpaceOnUse" x="0" y="690"><path d="M55 0 H0 V55" fill="none" stroke="#262119" stroke-width="1"/></pattern>',
               f'<radialGradient id="glow" cx="{glow_c[0] if glow_c else "50%"}" cy="{glow_c[1] if glow_c else "70%"}" r="60%"><stop offset="0" stop-color="#2A2012" stop-opacity=".9"/><stop offset="1" stop-color="#14120E" stop-opacity="0"/></radialGradient>']
        out += self.defs
        out.append("</defs>")
        out.append('<rect width="1200" height="800" fill="#14120E"/>')
        out.append('<rect width="1200" height="800" fill="url(#glow)"/>')
        out.append('<rect x="0" y="120" width="1200" height="570" fill="url(#grid)"/>')
        if ground:
            out.append('<line x1="40" y1="690" x2="1160" y2="690" stroke="#635C4C" stroke-width="1.5"/>')
        out.append('<rect x="12" y="12" width="1176" height="776" fill="none" stroke="#332D22" stroke-width="1"/>')
        out.append(f'<text x="40" y="54" font-family="{MONO}" font-size="12" letter-spacing="3" fill="#635C4C">{age_label}</text>')
        out.append(f'<text x="40" y="96" font-family="{SERIF}" font-size="38" font-weight="700" fill="#DCD2BA">{name}</text>')
        out.append(f'<text x="1160" y="54" text-anchor="end" font-family="{MONO}" font-size="12" fill="#9A9078">{tr1}</text>')
        out.append(f'<text x="1160" y="76" text-anchor="end" font-family="{MONO}" font-size="12" fill="#9A9078">{tr2}</text>')
        if ladder == "enemy":
            out.append(f'<g font-family="{MONO}" font-size="11" fill="#635C4C" stroke="#635C4C">'
                       '<line x1="60" y1="690" x2="60" y2="140" stroke-width="1"/>')
            for i, lab in enumerate(["0", "0.5", "1.0", "1.5", "2.0", "2.5"]):
                y = 690 - 110 * i
                out.append(f'<line x1="54" y1="{y}" x2="66" y2="{y}"/><text x="72" y="{y + 4}" stroke="none">{lab}</text>')
            out.append("</g>")
        if human:
            out.append('<g fill="#635C4C" opacity=".45"><ellipse cx="160" cy="320" rx="20" ry="26"/>'
                       '<path d="M160 346 C138 350 126 360 124 380 L118 500 L130 502 L138 410 L140 520 L142 690 L156 690 L160 540 L164 690 L178 690 L180 520 L182 410 L190 502 L202 500 L196 380 C194 360 182 350 160 346 Z"/></g>'
                       f'<text x="160" y="712" text-anchor="middle" font-family="{MONO}" font-size="10" fill="#635C4C">1.80 m</text>')
        for x, lab in (view_labels or []):
            out.append(f'<text x="{x}" y="730" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">{lab}</text>')
        out += labels_on_top(self.body)
        # palette
        out.append(f'<g font-family="{MONO}" font-size="10" fill="#9A9078">')
        widths = [40 + len(f"{h} {l}") * 6.1 for h, l in palette]
        gap = (1120 - sum(widths)) / max(len(palette) - 1, 1)
        assert gap >= 8, f"palette too wide: {palette}"
        x = 40
        for i, (hexc, label) in enumerate(palette):
            if i:
                x += widths[i - 1] + gap
            out.append(f'<rect x="{fmt(x)}" y="748" width="34" height="18" fill="{hexc}" stroke="#332D22" stroke-width="1"/>'
                       f'<text x="{fmt(x + 40)}" y="761">{hexc} {label}</text>')
        out.append("</g></svg>")
        return "\n".join(out)


def labels_on_top(body):
    """Move top-level outlined labels after the drawing, so no line or shape is painted over a label."""
    import re
    drawing, labels, depth = [], [], 0
    for element in body:
        opens = len(re.findall(r"<g[\s>]", element)) - len(re.findall(r"<g[^>]*/>", element))
        delta = opens - element.count("</g>")
        if depth == 0 and delta == 0 and element.startswith("<text") and 'paint-order="stroke"' in element:
            labels.append(element)
        else:
            drawing.append(element)
        depth += delta
    return drawing + [backed(label) for label in labels]


def backed(label):
    """Put a dark plate under a label, so a line passing behind it stops at its edge rather than showing between letters."""
    import html, re
    if "transform=" in label:
        return label
    attr = lambda name, default: (re.search(rf'\b{name}="([^"]+)"', label) or [None, default])[1]
    x, y, size = float(attr("x", 0)), float(attr("y", 0)), float(attr("font-size", 12))
    spacing, anchor = float(attr("letter-spacing", 0)), attr("text-anchor", "start")
    content = html.unescape(re.sub(r"<[^>]+>", "", label))
    if len(content.strip()) <= 2:
        return label  # a stencil mark or tick number drawn on the object, not a label
    width = len(content) * (size * 0.6 + spacing)
    left = x - width if anchor == "end" else x - width / 2 if anchor == "middle" else x
    pad = 3
    plate = (f'<rect x="{fmt(left - pad)}" y="{fmt(y - size * 0.82 - pad / 2)}" width="{fmt(width + 2 * pad)}" '
             f'height="{fmt(size * 1.1 + pad)}" rx="2" fill="#14120E" opacity="0.82"/>')
    return plate + label


def callouts(sh, items, x_label=968, y0=150, dy=None, y_max=690):
    """items: list of (tx, ty, label, sub, [lx, ly, anchor]). Right-margin labels by default."""
    auto = [it for it in items if len(it) == 4]
    n = len(auto)
    if dy is None:
        dy = min(58, (y_max - y0) / max(n, 1))
    auto_sorted = sorted(auto, key=lambda it: it[1])
    placed = {}
    for i, it in enumerate(auto_sorted):
        placed[id(it)] = (x_label, y0 + i * dy, "start")
    labels = []
    for it in items:
        if len(it) == 4:
            lx, ly, anchor = placed[id(it)]
        else:
            lx, ly, anchor = it[4], it[5], it[6]
        tx, ty, lab, sub = it[:4]
        elbow = lx - 12 if anchor == "start" else lx + 12
        sh.line([(tx, ty), (elbow, ly - 4), (lx - (4 if anchor == "start" else -4), ly - 4)], FRAME_TXT, 0.8, smooth=False)
        sh.circle(tx, ty, 2.6, VELLUM, INK, 0.8)
        labels.append((lx, ly, lab, sub, anchor))
    # Labels go down after every leader line, so no line is drawn over a label.
    for lx, ly, lab, sub, anchor in labels:
        sh.text(lx, ly, lab, 12, VELLUM, anchor)
        if sub:
            sh.text(lx, ly + 14, sub, 10.5, FRAME_TXT, anchor)


def height_mark(sh, x_to, h, label=None, label_x=None):
    y = GROUND - SCALE * h
    sh.line([(66, y), (x_to, y)], "#9A9078", 0.8, op=0.7, smooth=False, dash="4 4")
    sh.text(label_x if label_x is not None else x_to + 4, y - 4, label or f"{h:.2f} m", 10, "#9A9078")


class Fig:
    """Metre-space helper for a figure standing at pixel x = cx on the ground line."""

    def __init__(self, cx, scale=SCALE, ground=GROUND):
        self.cx, self.s, self.g = cx, scale, ground

    def p(self, x, y):
        return (self.cx + x * self.s, self.g - y * self.s)

    def pts(self, lst):
        return [self.p(x, y) for x, y in lst]

    def mirror(self, lst):
        """Symmetric outline: given right half from top to bottom, return full closed outline."""
        right = [(x, y) for x, y in lst]
        left = [(-x, y) for x, y in reversed(lst)]
        return self.pts(right + left)


def limb(a, wa, b, wb, bulge=0.0, n=3):
    """Tapered capsule outline (pixel space) from a (width wa) to b (width wb). Returns point list."""
    (ax, ay), (bx, by) = a, b
    dx, dy = bx - ax, by - ay
    L = math.hypot(dx, dy) or 1
    nx, ny = -dy / L, dx / L
    left, right = [], []
    for i in range(n + 1):
        t = i / n
        w = (wa + (wb - wa) * t) / 2 * (1 + bulge * math.sin(math.pi * t))
        cx, cy = ax + dx * t, ay + dy * t
        left.append((cx + nx * w, cy + ny * w))
        right.append((cx - nx * w, cy - ny * w))
    capb = (bx + dx / L * wb * 0.35, by + dy / L * wb * 0.35)
    capa = (ax - dx / L * wa * 0.35, ay - dy / L * wa * 0.35)
    return left + [capb] + list(reversed(right)) + [capa]


class Proj:
    """Three-quarter axonometric: viewer front-right, above. X width (right-down), Y depth (right-up), Z up."""

    def __init__(self, ox, oy, s, a=18, b=32, yk=0.75):
        self.ox, self.oy, self.s = ox, oy, s
        self.ca, self.sa = math.cos(math.radians(a)), math.sin(math.radians(a))
        self.cb, self.sb = math.cos(math.radians(b)) * yk, math.sin(math.radians(b)) * yk

    def p(self, X, Y, Z):
        s = self.s
        return (self.ox + X * s * self.ca + Y * s * self.cb, self.oy + X * s * self.sa - Y * s * self.sb - Z * s)

    def pts(self, lst):
        return [self.p(*t) for t in lst]

    def box(self, sh, x0, y0, z0, w, d, h, base, front=True, right=True, top=True, sw=1.0, light=0.3):
        P = self.p
        faces = []
        if top:
            faces.append(("top", [P(x0, y0, z0 + h), P(x0 + w, y0, z0 + h), P(x0 + w, y0 + d, z0 + h), P(x0, y0 + d, z0 + h)], mix(base, "#B8AC94", light * 0.45)))
        if right:
            faces.append(("right", [P(x0 + w, y0, z0), P(x0 + w, y0 + d, z0), P(x0 + w, y0 + d, z0 + h), P(x0 + w, y0, z0 + h)], dk(base, 0.35)))
        if front:
            faces.append(("front", [P(x0, y0, z0), P(x0 + w, y0, z0), P(x0 + w, y0, z0 + h), P(x0, y0, z0 + h)], base))
        out = {}
        for name, pts, col in faces:
            g = sh.lin([(0, lit(col, 0.12)), (1, dk(col, 0.18))], 0, 0, 1, 1, key=f"box{col}")
            sh.path(poly_path(pts), f"url(#{g})", INK, sw)
            out[name] = pts
        return out


def scale_bar(sh, x, y, px_per_m, metres=0.5, label=None):
    L = px_per_m * metres
    sh.rect(x, y, L / 2, 5, VELLUM, op=0.8)
    sh.rect(x + L / 2, y, L / 2, 5, "none", VELLUM, 0.8, op=0.8)
    for t in (0, 0.5, 1):
        sh.line([(x + L * t, y - 3), (x + L * t, y + 8)], VELLUM, 0.8, smooth=False, op=0.8)
    sh.text(x, y + 20, label or f"{metres:g} m = {L:g} px", 10, FRAME_TXT)


def grab(sh, x, y, label="GRAB", dx=10, dy=-8, anchor="start", hidden=False):
    sh.circle(x, y, 7, "none", "#5FA288", 1.6)
    sh.circle(x, y, 2.4, "#5FA288")
    if hidden:
        sh.add(f'<circle cx="{fmt(x)}" cy="{fmt(y)}" r="11" fill="none" stroke="#5FA288" stroke-width="1" stroke-dasharray="3 3"/>')
    sh.text(x + dx, y + dy, label, 10, "#5FA288", anchor, ls="1")


def fracture(sh, pts, smooth=False):
    sh.line(pts, "#C4542E", 1.5, smooth=smooth, dash="6 4", op=0.95)


# ── structure helpers ──
HATCH_ID = {}


def hatch(sh, color="#4A4232"):
    key = "hatch" + color
    if key in sh.gcache:
        return sh.gcache[key]
    hid = sh.uid("h")
    sh.defs.append(f'<pattern id="{hid}" width="7" height="7" patternUnits="userSpaceOnUse" patternTransform="rotate(45)">'
                   f'<rect width="7" height="7" fill="#1C1914"/><line x1="0" y1="0" x2="0" y2="7" stroke="{color}" stroke-width="1.4"/></pattern>')
    sh.gcache[key] = hid
    return hid


def poche(sh, x, y, w, h, color="#4A4232"):
    sh.rect(x, y, w, h, f"url(#{hatch(sh, color)})", "#8A8070", 1)


def struct_ladder(sh, x, floor_y, s, max_m, step=0.5, clear=None):
    sh.line([(x, floor_y), (x, floor_y - max_m * s)], FRAME_MID, 1, smooth=False)
    k = 0
    while k * step <= max_m + 1e-6:
        m = k * step
        y = floor_y - m * s
        major = abs(m - round(m)) < 1e-6
        sh.line([(x - (6 if major else 3), y), (x + (6 if major else 3), y)], FRAME_MID, 1, smooth=False)
        if major and not (clear and abs(m - clear) < 1e-6):
            sh.text(x - 10, y + 4, f"{m:.0f}", 11, FRAME_MID, "end")
        k += 1
    if clear:
        y = floor_y - clear * s
        sh.line([(x - 8, y), (x + 8, y)], VELLUM, 1.4, smooth=False)
        sh.text(x - 10, y - 6, f"{clear:.2f}", 11, VELLUM, "end")


def human(sh, x, floor_y, s, op=0.55):
    k = s / 220
    sh.add(f'<g transform="translate({fmt(x)} {fmt(floor_y)}) scale({k:.4f}) translate(-160 -690)" fill="#9A9078" opacity="{op}">'
           '<ellipse cx="160" cy="320" rx="20" ry="26"/>'
           '<path d="M160 346 C138 350 126 360 124 380 L118 500 L130 502 L138 410 L140 520 L142 690 L156 690 L160 540 L164 690 L178 690 L180 520 L182 410 L190 502 L202 500 L196 380 C194 360 182 350 160 346 Z"/></g>')
    sh.text(x, floor_y + 14 if floor_y < 680 else floor_y - 1.9 * s, "1.80 m", 9, FRAME_TXT, "middle")


def plan_frame(sh, x0, y0, s, wall, openings):
    """12 x 12 m cell plan. openings: list of (side, centre_m_from_cell_centre, width_m). Returns P(x_m, y_m) with (0,0) at cell centre, +y north (up)."""
    N = 12 * s
    P = lambda x, y: (x0 + (x + 6) * s, y0 + (6 - y) * s)
    sh.rect(x0, y0, N, N, "#1A1712", "#635C4C", 1)
    # 1 m ticks
    for i in range(13):
        L = 7 if i % 6 == 0 else 4
        for (ax, ay, bx, by) in ((x0 + i * s, y0, x0 + i * s, y0 - L), (x0 + i * s, y0 + N, x0 + i * s, y0 + N + L),
                                 (x0, y0 + i * s, x0 - L, y0 + i * s), (x0 + N, y0 + i * s, x0 + N + L, y0 + i * s)):
            sh.line([(ax, ay), (bx, by)], FRAME_MID, 1, smooth=False)
    sh.text(x0, y0 + N + 20, "0", 9, FRAME_MID, "middle")
    sh.text(x0 + N, y0 + N + 20, "12 m", 9, FRAME_MID, "middle")
    sh.text(x0 + N / 2, y0 + N + 20, "6", 9, FRAME_MID, "middle")
    # walls as poché with gaps
    w = wall * s
    segs = {"N": [], "S": [], "E": [], "W": []}
    for side, c, width in openings:
        segs[side].append((c - width / 2, c + width / 2))
    def run(side):
        cuts = sorted(segs[side])
        pos = -6.0
        out = []
        for a, b in cuts:
            out.append((pos, a))
            pos = b
        out.append((pos, 6.0))
        return out
    for a, b in run("N"):
        poche(sh, P(a, 6)[0], y0, (b - a) * s, w)
    for a, b in run("S"):
        poche(sh, P(a, 6)[0], y0 + N - w, (b - a) * s, w)
    for a, b in run("W"):
        poche(sh, x0, P(0, b)[1], w, (b - a) * s)
    for a, b in run("E"):
        poche(sh, x0 + N - w, P(0, b)[1], w, (b - a) * s)
    return P


def north_arrow(sh, x, y):
    sh.path(poly_path([(x, y - 14), (x + 6, y + 4), (x, y), (x - 6, y + 4)]), VELLUM, INK, 0.8)
    sh.text(x, y - 18, "N", 10, VELLUM, "middle")


def plan_label(sh, x, y, text, anchor="middle", color=VELLUM, size=9.5):
    sh.text(x, y, text, size, color, anchor, ls="0.5")
