"""Drawing library for the castle room sheets (docs/art/rooms/), one per module in
Tools/AssetPipeline/asset_specs.ERA_CASTLE_SPECS.

The first ~420 lines are the Bronze Age art-bible library
(Tools/ArtBible/generators/bronze/lib.py) unchanged, so a room sheet uses the same
frame, gradients, figure, ladder and palette strip as the structure sheets. What
is new is at the bottom, under "kit furniture": the Age-aware sheet header, plan
and section helpers in the castle kit's own coordinates (metres from the cell
centre, +y north, the same numbers as Tools/AssetPipeline/castle_*.py), and
overlays for the kit's rules (the clear cross, the four archways, loot anchors).
"""
import math
import random

S = 220.0   # enemy scale, px per metre
G = 690.0   # ground line y

MONO = "Overpass Mono, monospace"
SERIF = "Eczar, Georgia, serif"


def _rgb(h):
    h = h.lstrip("#")
    return [int(h[i:i + 2], 16) for i in (0, 2, 4)]


def _hex(c):
    return "#" + "".join(f"{max(0, min(255, int(round(v)))):02X}" for v in c)


def mix(a, b, t):
    ca, cb = _rgb(a), _rgb(b)
    return _hex([ca[i] + (cb[i] - ca[i]) * t for i in range(3)])


def lighten(h, t):
    return mix(h, "#F2E8D0", t)


def darken(h, t):
    return mix(h, "#0E0C09", t)


def f(v):
    return f"{v:.1f}".rstrip("0").rstrip(".") if abs(v - round(v)) > 1e-6 else str(int(round(v)))


def smooth_path(pts, closed=True, tension=0.5):
    """Catmull-Rom through pts -> cubic bezier path string."""
    n = len(pts)
    if n < 3:
        return "M" + " L".join(f"{f(x)} {f(y)}" for x, y in pts) + (" Z" if closed else "")
    d = [f"M{f(pts[0][0])} {f(pts[0][1])}"]
    rng = range(n) if closed else range(n - 1)
    for i in rng:
        p0 = pts[(i - 1) % n] if closed or i > 0 else pts[i]
        p1 = pts[i]
        p2 = pts[(i + 1) % n]
        p3 = pts[(i + 2) % n] if closed or i + 2 < n else pts[(i + 1) % n]
        k = tension / 3 * 2
        c1 = (p1[0] + (p2[0] - p0[0]) * k / 2, p1[1] + (p2[1] - p0[1]) * k / 2)
        c2 = (p2[0] - (p3[0] - p1[0]) * k / 2, p2[1] - (p3[1] - p1[1]) * k / 2)
        d.append(f"C{f(c1[0])} {f(c1[1])} {f(c2[0])} {f(c2[1])} {f(p2[0])} {f(p2[1])}")
    if closed:
        d.append("Z")
    return " ".join(d)


def poly_path(pts, closed=True):
    return "M" + " L".join(f"{f(x)} {f(y)}" for x, y in pts) + (" Z" if closed else "")


class Sheet:
    def __init__(self, kind, header, name, tr1, tr2, materials, seed=1):
        self.kind = kind          # enemy | item | structure
        self.header = header
        self.name = name
        self.tr1, self.tr2 = tr1, tr2
        self.materials = materials
        self.defs = []
        self.back = []            # drawn under the figure (light glows etc.)
        self.body = []
        self.call = []
        self.extra_frame = []
        self._n = 0
        self.rng = random.Random(seed)
        self._grads = {}

    # ---------- ids / defs ----------
    def uid(self, p="e"):
        self._n += 1
        return f"{p}{self._n}"

    def lin(self, base, direction="h", light=0.28, dark=0.45, stops=None):
        key = ("lin", base, direction, light, dark)
        if key in self._grads:
            return self._grads[key]
        gid = self.uid("g")
        x1, y1, x2, y2 = {"h": (0, 0, 1, 0), "hr": (1, 0, 0, 0), "v": (0, 0, 0, 1), "vr": (0, 1, 0, 0),
                          "d": (0, 0, 1, 1)}[direction]
        st = stops or [(0, lighten(base, light)), (0.38, base), (1, darken(base, dark))]
        s = "".join(f'<stop offset="{o}" stop-color="{c}"/>' for o, c in st)
        self.defs.append(f'<linearGradient id="{gid}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{s}</linearGradient>')
        self._grads[key] = gid
        return gid

    def rad(self, stops, cx=0.5, cy=0.5, r=0.5, fx=None, fy=None):
        gid = self.uid("r")
        s = "".join(f'<stop offset="{o}" stop-color="{c}" stop-opacity="{a}"/>' for o, c, a in stops)
        fxy = f' fx="{fx}" fy="{fy}"' if fx is not None else ""
        self.defs.append(f'<radialGradient id="{gid}" cx="{cx}" cy="{cy}" r="{r}"{fxy}>{s}</radialGradient>')
        return gid

    def clip(self, d):
        cid = self.uid("c")
        self.defs.append(f'<clipPath id="{cid}"><path d="{d}"/></clipPath>')
        return cid

    # ---------- drawing ----------
    def add(self, s, layer=None):
        (layer if layer is not None else self.body).append(s)

    def path(self, d, fill="none", stroke=None, sw=1, op=None, extra="", layer=None):
        a = f'<path d="{d}" fill="{fill}"'
        if stroke:
            a += f' stroke="{stroke}" stroke-width="{sw}" stroke-linejoin="round" stroke-linecap="round"'
        if op is not None:
            a += f' opacity="{op}"'
        self.add(a + f"{extra}/>", layer)

    def shape(self, pts, base, direction="h", outline=None, smooth=True, light=0.28, dark=0.45, sw=1.2,
              flat=False, op=None):
        """A shaded form: gradient fill + darker outline. Returns the path d for clipping/flecks."""
        d = smooth_path(pts) if smooth else poly_path(pts)
        fill = base if flat else f"url(#{self.lin(base, direction, light, dark)})"
        self.path(d, fill, outline or darken(base, 0.62), sw, op)
        return d

    def flecks(self, d, bbox, n, color, rmin=0.6, rmax=1.8, op=0.55):
        cid = self.clip(d)
        x0, y0, x1, y1 = bbox
        parts = [f'<g clip-path="url(#{cid})" fill="{color}" opacity="{op}">']
        for _ in range(n):
            x = self.rng.uniform(x0, x1)
            y = self.rng.uniform(y0, y1)
            r = self.rng.uniform(rmin, rmax)
            parts.append(f'<circle cx="{f(x)}" cy="{f(y)}" r="{f(r)}"/>')
        parts.append("</g>")
        self.add("".join(parts))

    def clipped(self, d, inner):
        cid = self.clip(d)
        self.add(f'<g clip-path="url(#{cid})">{inner}</g>')

    def line(self, x1, y1, x2, y2, stroke, sw=1, op=None, dash=None, layer=None):
        a = f'<line x1="{f(x1)}" y1="{f(y1)}" x2="{f(x2)}" y2="{f(y2)}" stroke="{stroke}" stroke-width="{sw}" stroke-linecap="round"'
        if op is not None:
            a += f' opacity="{op}"'
        if dash:
            a += f' stroke-dasharray="{dash}"'
        self.add(a + "/>", layer)

    def circle(self, cx, cy, r, fill, stroke=None, sw=1, op=None, layer=None):
        a = f'<circle cx="{f(cx)}" cy="{f(cy)}" r="{f(r)}" fill="{fill}"'
        if stroke:
            a += f' stroke="{stroke}" stroke-width="{sw}"'
        if op is not None:
            a += f' opacity="{op}"'
        self.add(a + "/>", layer)

    def ellipse(self, cx, cy, rx, ry, fill, stroke=None, sw=1, op=None, rot=0, layer=None):
        a = f'<ellipse cx="{f(cx)}" cy="{f(cy)}" rx="{f(rx)}" ry="{f(ry)}" fill="{fill}"'
        if stroke:
            a += f' stroke="{stroke}" stroke-width="{sw}"'
        if op is not None:
            a += f' opacity="{op}"'
        if rot:
            a += f' transform="rotate({f(rot)} {f(cx)} {f(cy)})"'
        self.add(a + "/>", layer)

    def text(self, x, y, s, size=11, fill="#9A9078", anchor="start", family=MONO, ls=None, weight=None, layer=None):
        a = f'<text x="{f(x)}" y="{f(y)}" font-family="{family}" font-size="{size}" fill="{fill}"'
        if anchor != "start":
            a += f' text-anchor="{anchor}"'
        if ls:
            a += f' letter-spacing="{ls}"'
        if weight:
            a += f' font-weight="{weight}"'
        s = s.replace("&", "&amp;").replace("<", "&lt;")
        self.add(a + f">{s}</text>", layer if layer is not None else self.call)

    def callout(self, px, py, lx, ly, label, sub=None, anchor="start", elbow=True):
        """Leader from part (px,py) to label at (lx,ly)."""
        tx = lx - 6 if anchor == "start" else lx + 6
        if elbow:
            mid = (tx - 14, ly - 4) if anchor == "start" else (tx + 14, ly - 4)
            d = f"M{f(px)} {f(py)} L{f(mid[0])} {f(mid[1])} L{f(tx)} {f(ly - 4)}"
        else:
            d = f"M{f(px)} {f(py)} L{f(tx)} {f(ly - 4)}"
        self.call.append(f'<path d="{d}" fill="none" stroke="#9A9078" stroke-width=".8" opacity=".85"/>')
        self.call.append(f'<circle cx="{f(px)}" cy="{f(py)}" r="2.6" fill="#DCD2BA" stroke="#14120E" stroke-width="1"/>')
        self.text(lx, ly, label, 12, "#DCD2BA", anchor)
        if sub:
            # A room sheet's plan starts at x = 790: a left-anchored subtitle that would run
            # into it wraps onto a second line at a word break (10.5 px mono ≈ 6.3 px a char).
            limit = int((790 - lx) / 6.3) if anchor == "start" and lx < 790 else 10 ** 6
            lines, line = [], ""
            words = []
            for word in sub.split(" "):          # keep a unit with its number ("2.40 m" never splits)
                if words and word in ("m", "m,", "m.", "tris", "st"):
                    words[-1] += " " + word
                else:
                    words.append(word)
            for word in words:
                if line and len(line) + 1 + len(word) > limit:
                    lines.append(line)
                    line = word
                else:
                    line = f"{line} {word}" if line else word
            lines.append(line)
            lines = [ln[:-2] if ln.endswith(" ·") else ln for ln in lines]    # no dangling separator at a break
            for i, text in enumerate(lines):
                self.text(lx, ly + 14 + i * 13, text, 10.5, "#9A9078", anchor)

    def callouts(self, items, lx, ytop, ybot, anchor="start", slope=0.0):
        """items: (px, py, label, sub). Sorted by part height (skewed by slope for spread-out parts) so
        leaders do not cross; spread evenly."""
        items = sorted(items, key=lambda t: t[1] - slope * abs(lx - t[0]))
        n = len(items)
        step = (ybot - ytop) / max(n - 1, 1)
        for i, (px, py, lab, sub) in enumerate(items):
            self.callout(px, py, lx, ytop + i * step, lab, sub, anchor)

    # ---------- frame ----------
    def frame_top(self):
        return [
            '<rect width="1200" height="800" fill="#14120E"/>',
            '<rect width="1200" height="800" fill="url(#glow)"/>',
            '<rect x="0" y="120" width="1200" height="570" fill="url(#grid)"/>',
        ]

    def render(self, ground=(40, 1160)):
        head = ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 800" width="1200" height="800">\n'
                f'  <!-- Plunderspell castle room sheet · {self.header} · generated. Rules: docs/art/rooms/README.md -->\n')
        defs = ['<pattern id="grid" width="55" height="55" patternUnits="userSpaceOnUse" x="0" y="690">'
                '<path d="M55 0 H0 V55" fill="none" stroke="#262119" stroke-width="1"/></pattern>',
                '<radialGradient id="glow" cx="50%" cy="70%" r="60%"><stop offset="0" stop-color="#2A2012" stop-opacity=".9"/>'
                '<stop offset="1" stop-color="#14120E" stop-opacity="0"/></radialGradient>'] + self.defs
        out = [head, "  <defs>\n    " + "\n    ".join(defs) + "\n  </defs>\n"]
        out += ["  " + s + "\n" for s in self.frame_top()]
        out += ["  " + s + "\n" for s in self.back]
        if ground:
            out.append(f'  <line x1="{ground[0]}" y1="690" x2="{ground[1]}" y2="690" stroke="#635C4C" stroke-width="1.5"/>\n')
        out.append('  <rect x="12" y="12" width="1176" height="776" fill="none" stroke="#332D22" stroke-width="1"/>\n')
        # title block
        out.append(f'  <text x="40" y="54" font-family="{MONO}" font-size="12" letter-spacing="3" fill="#635C4C">{self.header}</text>\n')
        out.append(f'  <text x="40" y="96" font-family="{SERIF}" font-size="38" font-weight="700" fill="#DCD2BA">{self.name}</text>\n')
        out.append(f'  <text x="1160" y="54" text-anchor="end" font-family="{MONO}" font-size="12" fill="#9A9078">{self.tr1}</text>\n')
        out.append(f'  <text x="1160" y="76" text-anchor="end" font-family="{MONO}" font-size="12" fill="#9A9078">{self.tr2}</text>\n')
        out += ["  " + s + "\n" for s in self.extra_frame]
        out.append('  <g id="drawing">\n' + "".join("    " + s + "\n" for s in self.body) + "  </g>\n")
        out.append(f'  <g id="callouts" font-family="{MONO}">\n' + "".join("    " + s + "\n" for s in self.call) + "  </g>\n")
        # palette strip
        n = len(self.materials)
        step = 1120 / max(n, 1)
        chips = []
        for i, (nm, hx) in enumerate(self.materials):
            x = 40 + i * step
            chips.append(f'<rect x="{f(x)}" y="748" width="34" height="18" fill="{hx}" stroke="#332D22" stroke-width=".6"/>'
                         f'<text x="{f(x + 40)}" y="761">{hx} {nm}</text>')
        out.append(f'  <g id="palette" font-family="{MONO}" font-size="10" fill="#9A9078">\n' +
                   "".join("    " + c + "\n" for c in chips) + "  </g>\n")
        out.append("</svg>\n")
        return "".join(out)


# ---------------- enemy frame furniture ----------------

def enemy_furniture(sh):
    ef = sh.extra_frame
    ef.append(f'<g font-family="{MONO}" font-size="11" fill="#635C4C" stroke="#635C4C">'
              '<line x1="60" y1="690" x2="60" y2="140" stroke-width="1"/>' +
              "".join(f'<line x1="54" y1="{690 - i * 110}" x2="66" y2="{690 - i * 110}"/>'
                      f'<text x="72" y="{694 - i * 110}" stroke="none">{lab}</text>'
                      for i, lab in enumerate(["0", "0.5", "1.0", "1.5", "2.0", "2.5"])) + "</g>")
    ef.append('<g fill="#635C4C" opacity=".45"><ellipse cx="160" cy="320" rx="20" ry="26"/>'
              '<path d="M160 346 C138 350 126 360 124 380 L118 500 L130 502 L138 410 L140 520 L142 690 L156 690 '
              'L160 540 L164 690 L178 690 L180 520 L182 410 L190 502 L202 500 L196 380 C194 360 182 350 160 346 Z"/></g>')
    ef.append(f'<text x="160" y="712" text-anchor="middle" font-family="{MONO}" font-size="10" fill="#635C4C">1.80 m</text>')
    ef.append(f'<text x="470" y="730" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">FRONT</text>')
    ef.append(f'<text x="820" y="730" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">SIDE</text>')


def height_mark(sh, h, x1=900, label=None, lx=214):
    """Dashed line at the figure's top, labelled near the ladder, so height reads against it."""
    y = G - h * S
    sh.extra_frame.append(f'<line x1="66" y1="{f(y)}" x2="{x1}" y2="{f(y)}" stroke="#635C4C" stroke-width=".8" stroke-dasharray="3 5"/>')
    sh.extra_frame.append(f'<text x="{lx}" y="{f(y - 5)}" font-family="{MONO}" font-size="10" fill="#9A9078">{label or f"{h:.2f} m"}</text>')


class Fig:
    """Metre-space helper for a figure centred at cx (px)."""

    def __init__(self, sh, cx, mirror=1):
        self.sh, self.cx, self.m = sh, cx, mirror

    def p(self, x, h):
        return (self.cx + self.m * x * S, G - h * S)

    def pts(self, lst):
        return [self.p(x, h) for x, h in lst]

    def shape(self, lst, base, **kw):
        return self.sh.shape(self.pts(lst), base, **kw)

    def limb(self, a, b, wa, wb, base, direction="h", light=0.25, dark=0.45, cap=True):
        (x1, y1), (x2, y2) = self.p(*a), self.p(*b)
        dx, dy = x2 - x1, y2 - y1
        L = math.hypot(dx, dy) or 1
        nx, ny = -dy / L, dx / L
        ra, rb = wa * S / 2, wb * S / 2
        pts = [(x1 + nx * ra, y1 + ny * ra), (x2 + nx * rb, y2 + ny * rb),
               (x2 - nx * rb, y2 - ny * rb), (x1 - nx * ra, y1 - ny * ra)]
        d = poly_path(pts)
        if cap:
            d += f" M{f(x1 + ra)} {f(y1)} A{f(ra)} {f(ra)} 0 1 0 {f(x1 - ra)} {f(y1)} A{f(ra)} {f(ra)} 0 1 0 {f(x1 + ra)} {f(y1)} Z"
            d += f" M{f(x2 + rb)} {f(y2)} A{f(rb)} {f(rb)} 0 1 0 {f(x2 - rb)} {f(y2)} A{f(rb)} {f(rb)} 0 1 0 {f(x2 + rb)} {f(y2)} Z"
        gid = self.sh.lin(base, direction, light, dark)
        self.sh.path(d, f"url(#{gid})", None)
        # outline only along the long sides for a softer painted look
        self.sh.path(poly_path([pts[0], pts[1]], False) + " " + poly_path([pts[2], pts[3]], False),
                     "none", darken(base, 0.62), 1.1)
        return d

    def ell(self, x, h, rx, ry, base, direction="h", light=0.3, dark=0.45, rot=0):
        cx, cy = self.p(x, h)
        gid = self.sh.lin(base, direction, light, dark)
        self.sh.ellipse(cx, cy, rx * S, ry * S, f"url(#{gid})", darken(base, 0.62), 1.1, rot=rot)
        return cx, cy

    def line(self, a, b, color, sw=1, op=None, dash=None):
        (x1, y1), (x2, y2) = self.p(*a), self.p(*b)
        self.sh.line(x1, y1, x2, y2, color, sw, op, dash)

    def dot(self, x, h, r, color, op=None):
        cx, cy = self.p(x, h)
        self.sh.circle(cx, cy, r, color, op=op)

    def curve(self, lst, color, sw=1, op=None, smooth=True):
        d = smooth_path(self.pts(lst), closed=False) if smooth else poly_path(self.pts(lst), closed=False)
        self.sh.path(d, "none", color, sw, op)

    def bbox(self, lst):
        pp = self.pts(lst)
        xs, ys = [p[0] for p in pp], [p[1] for p in pp]
        return (min(xs), min(ys), max(xs), max(ys))


# ---------------- item furniture ----------------
VERD = "#5FA288"
MADDER = "#C4542E"


def item_sheet(name, worth, bulk, dims, budget, scales, materials, seed=5):
    sh = Sheet("item", f"THE BRONZE AGE · PLUNDER · {worth} COIN · {bulk} ST", name,
               f"{dims} · {budget}", scales, materials, seed=seed)
    return sh


def view_label(sh, x, s, y=730):
    sh.extra_frame.append(f'<text x="{x}" y="{y}" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">{s}</text>')


def scale_bar(sh, x, y, px, label, ticks=5):
    ef = sh.extra_frame
    ef.append(f'<g stroke="#9A9078" stroke-width="1"><line x1="{f(x)}" y1="{y}" x2="{f(x + px)}" y2="{y}"/>' +
              "".join(f'<line x1="{f(x + px * i / ticks)}" y1="{y - (5 if i in (0, ticks) else 3)}" x2="{f(x + px * i / ticks)}" y2="{y + (5 if i in (0, ticks) else 3)}"/>'
                      for i in range(ticks + 1)) + "</g>")
    ef.append(f'<rect x="{f(x)}" y="{y - 2}" width="{f(px / ticks)}" height="4" fill="#9A9078"/>')
    ef.append(f'<text x="{f(x + px + 8)}" y="{y + 4}" font-family="{MONO}" font-size="10" fill="#9A9078">{label}</text>')


def grab(sh, x, y, lx=None, ly=None, label="GRAB"):
    sh.call.append(f'<circle cx="{f(x)}" cy="{f(y)}" r="8" fill="none" stroke="{VERD}" stroke-width="1.8"/>')
    sh.call.append(f'<circle cx="{f(x)}" cy="{f(y)}" r="2.2" fill="{VERD}"/>')
    if lx is not None:
        sh.call.append(f'<line x1="{f(x)}" y1="{f(y)}" x2="{f(lx)}" y2="{f(ly)}" stroke="{VERD}" stroke-width=".8" opacity=".8"/>')
        sh.text(lx + (4 if lx >= x else -4), ly + 4, label, 10, VERD, "start" if lx >= x else "end", ls="1.5")


def crack(sh, pts, sw=1.4):
    d = smooth_path(pts, closed=False, tension=0.3)
    sh.call.append(f'<path d="{d}" fill="none" stroke="{MADDER}" stroke-width="{sw}" stroke-dasharray="5 3" stroke-linecap="round"/>')


class Proj:
    """Three-quarter projection: yaw a (deg), elevation e (deg), k px/m, origin (cx, gy) at z=0."""

    def __init__(self, cx, gy, k, a=32, e=24):
        self.cx, self.gy, self.k = cx, gy, k
        self.ca, self.sa = math.cos(math.radians(a)), math.sin(math.radians(a))
        self.ce, self.se = math.cos(math.radians(e)), math.sin(math.radians(e))

    def __call__(self, x, y, z):
        sx = self.cx + self.k * (x * self.ca + y * self.sa)
        d = -x * self.sa + y * self.ca
        sy = self.gy - self.k * (z * self.ce + d * self.se)
        return (sx, sy)

    def pts(self, lst):
        return [self(*p) for p in lst]


def revolve(sh, cx, gy, k, prof, base, e=16, light=.35, dark=.55, sw=1.4, ridges=None, ridge_col=None):
    """Draw a surface of revolution in a slight top-down view. prof: [(z, r)] bottom->top.
    Returns (outline d, helper to map (z, r_frac, side) -> screen)."""
    ce, se = math.cos(math.radians(e)), math.sin(math.radians(e))

    def Y(z):
        return gy - z * k * ce

    left = [(cx - r * k, Y(z)) for z, r in prof]
    right = [(cx + r * k, Y(z)) for z, r in reversed(prof)]
    # bottom arc (front half of the base ellipse)
    z0, r0 = prof[0]
    bot = [(cx + r0 * k * math.cos(t), Y(z0) + r0 * k * se * math.sin(t)) for t in [i * math.pi / 8 for i in range(1, 8)]]
    pts = left + right + bot
    d = smooth_path(pts, tension=.4)
    sh.path(d, f"url(#{sh.lin(base, 'h', light, dark)})", darken(base, .62), sw)
    if ridges:
        col = ridge_col or darken(base, .35)
        for z in ridges:
            r = interp(prof, z)
            sh.path(f"M{f(cx - r * k)} {f(Y(z))} A{f(r * k)} {f(r * k * se)} 0 0 0 {f(cx + r * k)} {f(Y(z))}", "none", col, .8, op=.55)
    return d, Y, se


def interp(prof, z):
    for (z0, r0), (z1, r1) in zip(prof, prof[1:]):
        if z0 <= z <= z1:
            return r0 + (r1 - r0) * (z - z0) / ((z1 - z0) or 1)
    return prof[-1][1]


# ---------------- structure furniture ----------------
SK = 55.0          # elevation px per metre
EX0 = 100.0        # elevation x of the cell's west edge
PK = 30.0          # plan px per metre
PX0, PY0 = 800.0, 530.0   # plan origin (SW corner of the cell), y grows north (up)


def E(x, h):
    return (EX0 + x * SK, G - h * SK)


def PL(x, y):
    return (PX0 + x * PK, PY0 - y * PK)


def struct_sheet(zone, name, clear, materials, seed=7):
    sh = Sheet("structure", f"THE BRONZE AGE · STRUCTURE · {zone.upper()}", name,
               f"12 × 12 m cell · clear {clear:.2f} m · ≤ 25k tris",
               "section 1 m = 55 px · plan 1 m = 30 px", materials, seed=seed)
    ef = sh.extra_frame
    # height ladder (metres above the slab top, slab 0.30 m)
    ef.append(f'<g font-family="{MONO}" font-size="10" fill="#635C4C" stroke="#635C4C">'
              f'<line x1="60" y1="690" x2="60" y2="{G - 6.5 * SK:.0f}" stroke-width="1"/>' +
              "".join(f'<line x1="54" y1="{G - (i + .3) * SK:.1f}" x2="66" y2="{G - (i + .3) * SK:.1f}"/>'
                      f'<text x="30" y="{G - (i + .3) * SK + 4:.1f}" stroke="none">{i}</text>' for i in range(7)) +
              f'<text x="24" y="{G - 6.75 * SK:.0f}" stroke="none">m</text></g>')
    return sh


def human(sh, x, h0=0.30):
    """1.80 m reference human standing on the slab at elevation x (m)."""
    cx, gy = E(x, h0)
    s = 1.80 * SK / 396.0
    sh.add(f'<g transform="translate({f(cx - 160 * s)} {f(gy - 690 * s)}) scale({s:.5f})" fill="#DCD2BA" opacity=".5">'
           '<ellipse cx="160" cy="320" rx="20" ry="26"/>'
           '<path d="M160 346 C138 350 126 360 124 380 L118 500 L130 502 L138 410 L140 520 L142 690 L156 690 L160 540 L164 690 L178 690 L180 520 L182 410 L190 502 L202 500 L196 380 C194 360 182 350 160 346 Z"/></g>')
    sh.text(cx, gy - 1.80 * SK - 6, "1.80 m", 9, "#9A9078", "middle")


def plan_frame(sh, title="PLAN · 12 × 12 m CELL"):
    x0, y0 = PL(0, 12)
    sh.add(f'<rect x="{f(x0)}" y="{f(y0)}" width="{f(12 * PK)}" height="{f(12 * PK)}" fill="#1B1813" stroke="#635C4C" stroke-width="1"/>')
    ticks = []
    for i in range(13):
        a = PL(i, 0); b = PL(0, i)
        ticks.append(f'<line x1="{f(a[0])}" y1="{f(a[1])}" x2="{f(a[0])}" y2="{f(a[1] + (6 if i % 6 == 0 else 3))}"/>')
        ticks.append(f'<line x1="{f(b[0])}" y1="{f(b[1])}" x2="{f(b[0] - (6 if i % 6 == 0 else 3))}" y2="{f(b[1])}"/>')
    sh.add(f'<g stroke="#635C4C" stroke-width="1">{"".join(ticks)}</g>')
    sh.text(PX0, PY0 + 18, "0", 9, "#635C4C", "middle")
    sh.text(PX0 + 6 * PK, PY0 + 18, "6", 9, "#635C4C", "middle")
    sh.text(PX0 + 12 * PK, PY0 + 18, "12 m", 9, "#635C4C", "middle")
    sh.extra_frame.append(f'<text x="{f(PX0 + 6 * PK)}" y="{f(PL(0, 12)[1] - 14)}" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">{title}</text>')
    # north arrow
    nx, ny = PL(12.9, 11.2)
    sh.add(f'<path d="M{f(nx)} {f(ny - 14)} l6 16 l-6 -4 l-6 4 z" fill="#9A9078"/>')
    sh.text(nx, ny + 16, "N", 10, "#9A9078", "middle")


def prect(sh, x0, y0, x1, y1, fill, stroke=None, sw=1, op=None):
    a, b = PL(x0, y1), PL(x1, y0)
    extra = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    o = f' opacity="{op}"' if op is not None else ""
    sh.add(f'<rect x="{f(a[0])}" y="{f(a[1])}" width="{f(b[0] - a[0])}" height="{f(b[1] - a[1])}" fill="{fill}"{extra}{o}/>')


def erect(sh, x0, h0, x1, h1, fill, stroke=None, sw=1, op=None):
    a, b = E(x0, h1), E(x1, h0)
    extra = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    o = f' opacity="{op}"' if op is not None else ""
    sh.add(f'<rect x="{f(a[0])}" y="{f(a[1])}" width="{f(b[0] - a[0])}" height="{f(b[1] - a[1])}" fill="{fill}"{extra}{o}/>')


def socket_label(sh, x, y, s, anchor="middle", col="#DCD2BA"):
    sh.text(x, y, s, 9.5, col, anchor, ls="1")


def hatch(sh, d, bbox, col, step=6, op=.5):
    x0, y0, x1, y1 = bbox
    lines = "".join(f'<line x1="{f(x)}" y1="{f(y1)}" x2="{f(x + (y1 - y0))}" y2="{f(y0)}"/>'
                    for x in [x0 - (y1 - y0) + i * step for i in range(int(((x1 - x0) + (y1 - y0)) / step) + 1)])
    sh.clipped(d, f'<g stroke="{col}" stroke-width=".8" opacity="{op}">{lines}</g>')


def legend(sh, rows, x=800, y=575):
    for i, (tag, txt) in enumerate(rows):
        sh.text(x, y + i * 15, tag, 9.5, "#DCD2BA", ls="1")
        sh.text(x + 92, y + i * 15, txt, 9.5, "#9A9078")



# ---------------- kit furniture (castle room sheets) ----------------
#
# Coordinates: the castle kit's own. x east, y north, metres from the cell
# centre (-6..6); heights are metres above the ground, so the floor slab's
# top is at FZ = 0.30 and a room's walls rise from there. These are the
# numbers the Blender builder uses, so a dimension read off the sheet can go
# straight into Tools/AssetPipeline/castle_builders_*.py.

AGE_NAMES = {"BronzeAge": "THE BRONZE AGE", "HighMedieval": "THE HIGH MEDIEVAL",
             "LateMedieval": "THE LATE MEDIEVAL", "AgeOfPowder": "THE AGE OF POWDER"}
ZONE_CLEAR = {"CurtainWall": 5.2, "OuterBailey": 3.6, "InnerWard": 4.0, "Keep": 4.6, "Crypt": 3.0}
ARCH_H = {"OuterBailey": 2.59, "InnerWard": 2.88, "Keep": 3.31, "Crypt": 2.16, "CurtainWall": 3.74}
FZ = 0.30
HALF = 6.0
WALL_T = 0.5
IN = HALF - WALL_T          # 5.5, the inner face of a room wall
Q0 = 1.8                    # a quadrant starts this far from each centre line
WALKWAY = 1.6               # the clear cross: |x| < 1.6 or |y| < 1.6, up to 2 m
LOOT = "#C9A227"            # orpiment: value only
WALK = "#9A9078"


def KE(x, h):
    """Section point: kit x (-6..6, west to east) and height above ground."""
    return E(x + HALF, h)


def KP(x, y):
    """Plan point: kit x, y (-6..6)."""
    return PL(x + HALF, y + HALF)


def kerect(sh, x0, h0, x1, h1, fill, stroke=None, sw=1, op=None):
    """erect() in kit x."""
    erect(sh, x0 + HALF, h0, x1 + HALF, h1, fill, stroke, sw, op)


def kprect(sh, x0, y0, x1, y1, fill, stroke=None, sw=1, op=None):
    """prect() in kit x, y."""
    prect(sh, x0 + HALF, y0 + HALF, x1 + HALF, y1 + HALF, fill, stroke, sw, op)


def room_sheet(age, zone, name, materials, budget, section_label, seed=7):
    """A castle room sheet: the structure-sheet frame with this Age in the
    header, section on the left (1 m = 55 px), plan on the right (1 m = 30 px).
    `budget` is the kit triangle budget, e.g. "≤ 2.4k tris (kit)"."""
    clear = ZONE_CLEAR[zone]
    sh = struct_sheet(zone, name, clear, materials, seed=seed)
    sh.header = f"{AGE_NAMES[age]} · CASTLE ROOM · {zone.upper()}"
    sh.tr1 = f"12 × 12 m cell · clear {clear:.2f} m · {budget}"
    sh.extra_frame.append(f'<text x="{f(KE(0, 0)[0])}" y="730" text-anchor="middle" font-family="{MONO}" '
                          f'font-size="11" letter-spacing="3" fill="#635C4C">{section_label}</text>')
    return sh


def kit_plan(sh, zone, floor, wall, trim=None, archways=True, title="PLAN · 12 × 12 m CELL", shell=True):
    """The plan frame with the kit's room shell drawn in: floor, four 0.5 m
    walls, an archway 2.60 m wide centred on every side, and the clear cross
    as a faint dashed band. Draw furniture after this, then kit_loot().
    shell=False (curtain-wall pieces) draws only the frame and the ground:
    a wall piece has no room shell and no clear cross; draw its wall on the
    south side yourself, and mark "OUTSIDE" (south) and "BAILEY" (north)."""
    plan_frame(sh, title)
    kprect(sh, -HALF, -HALF, HALF, HALF, floor)
    if not shell:
        return
    for x0, y0, x1, y1 in ((-HALF, IN, HALF, HALF), (-HALF, -HALF, HALF, -IN),
                           (-HALF, -IN, -IN, IN), (IN, -IN, HALF, IN)):
        kprect(sh, x0, y0, x1, y1, wall, darken(wall, .5), .8)
    if trim:
        for x0, y0, x1, y1 in ((-HALF, HALF - .12, HALF, HALF), (-HALF, -HALF, HALF, -HALF + .12)):
            kprect(sh, x0, y0, x1, y1, trim)
    if archways:
        for x0, y0, x1, y1 in ((-1.3, IN - .02, 1.3, HALF), (-1.3, -HALF, 1.3, -IN + .02),
                               (-HALF, -1.3, -IN + .02, 1.3), (IN - .02, -1.3, HALF, 1.3)):
            kprect(sh, x0, y0, x1, y1, floor)
    # The clear cross: kept free of furniture up to 2 m (validate_in_blender).
    for x0, y0, x1, y1 in ((-WALKWAY, -IN, WALKWAY, IN), (-IN, -WALKWAY, IN, WALKWAY)):
        a, b = KP(x0, y1), KP(x1, y0)
        sh.add(f'<rect x="{f(a[0])}" y="{f(a[1])}" width="{f(b[0] - a[0])}" height="{f(b[1] - a[1])}" '
               f'fill="{WALK}" fill-opacity=".05" stroke="{WALK}" stroke-opacity=".35" stroke-width=".8" stroke-dasharray="4 4"/>')


def kit_loot(sh, points, label=True):
    """Loot anchors on the plan: small orpiment diamonds, numbered. `points`
    is [(x, y), ...] in kit metres; the same points (with heights) go in the
    room's JSON `loot_anchors` and are registered by the builder."""
    # Invisible record of the numbering, so build_room_sheets.py can check the spec lists
    # its loot anchors in the same L1…Ln order as the drawing.
    sh.add('<g id="loot-anchors" data-xy="' + ";".join(f"{x:.2f},{y:.2f}" for x, y in points) + '"/>')
    for i, (x, y) in enumerate(points, 1):
        cx, cy = KP(x, y)
        sh.add(f'<path d="M{f(cx)} {f(cy - 5)} l5 5 l-5 5 l-5 -5 z" fill="{LOOT}" stroke="#14120E" stroke-width=".8"/>')
        if label:
            # Dark halo under the label so it reads on pale furniture as well as dark floor.
            sh.add(f'<text x="{f(cx + 7)}" y="{f(cy + 3.5)}" font-family="{MONO}" font-size="8.5" fill="{LOOT}" '
                   f'stroke="#14120E" stroke-width="2.4" paint-order="stroke">L{i}</text>')


def kit_arch_section(sh, zone, x_centre=0.0, fill="#14120E"):
    """The archway opening seen in a section's back wall (2.60 m wide, the
    zone's archway height), dark, with its size labelled."""
    h = ARCH_H[zone]
    kerect(sh, x_centre - 1.3, FZ, x_centre + 1.3, FZ + h, fill)
    a = KE(x_centre, FZ + h)
    sh.text(a[0], a[1] - 5, f"ARCHWAY 2.60 × {h:.2f}", 8.5, "#635C4C", "middle")


def khuman(sh, x, floor=FZ):
    """The 1.80 m reference human standing at kit x on the floor (section)."""
    human(sh, x + HALF, floor)


def kit_legend(sh, rows, x=800, y=585):
    """legend() placed under the plan."""
    legend(sh, rows, x, y)


# ---------------- shared section / plan pieces (every room sheet repeats these) ----------------

def kit_glow(sh, x, h, colour, rx=300, ry=200, strength=.5):
    """Warm light glow behind the section, centred on kit (x, h): the room's fire or lamp."""
    g = sh.rad([(0, colour, strength), (0.35, darken(colour, .45), strength * .75), (1, "#14120E", 0)])
    cx, cy = KE(x, h)
    sh.back.append(f'<ellipse cx="{f(cx)}" cy="{f(cy - 30)}" rx="{rx}" ry="{ry}" fill="url(#{g})"/>')


def kit_slab(sh, colour):
    """The 0.30 m floor slab across the whole section."""
    kerect(sh, -HALF, 0, HALF, FZ, f"url(#{sh.lin(colour, 'v', .1, .4)})", darken(colour, .6))


def kit_back_wall(sh, zone, plaster, soot="#2B231B", trim=None, archway=True, soot_depth=1.2):
    """The far wall's inner face seen in a section, slab top to wall top, with
    soot darkening down from the top, the zone's archway cut in it (dark) and
    the 0.45 m trim course along the top. Returns the wall's path."""
    top = FZ + ZONE_CLEAR[zone]
    wall = poly_path([KE(-IN, FZ), KE(IN, FZ), KE(IN, top), KE(-IN, top)])
    sh.path(wall, f"url(#{sh.lin(plaster, 'v', .1, .5)})", darken(plaster, .6), 1)
    gs = sh.lin(soot, "v", 0, 0, stops=[(0, soot), (0.45, soot), (1, plaster)])
    sh.clipped(wall, f'<rect x="{f(KE(-IN, 0)[0])}" y="{f(KE(0, top)[1])}" width="{f(2 * IN * SK)}" '
                     f'height="{f(soot_depth * SK)}" fill="url(#{gs})" opacity=".55"/>')
    if archway:
        kit_arch_section(sh, zone)
    if trim:
        kerect(sh, -HALF, top - 0.45, HALF, top, f"url(#{sh.lin(trim, 'v', .2, .5)})", darken(trim, .6), .8)
    return wall


def kit_cut_walls(sh, zone, through_archways=True, fill="#3A332A"):
    """The east and west walls where the section plane cuts them, hatched.
    Cut at y = 0 the plane passes through the E and W archways, so only the
    lintel over each opening is cut (through_archways); otherwise the full wall."""
    top = FZ + ZONE_CLEAR[zone]
    bottom = FZ + ARCH_H[zone] if through_archways else FZ
    for x0, x1 in ((-HALF, -IN), (IN, HALF)):
        d = poly_path([KE(x0, bottom), KE(x1, bottom), KE(x1, top), KE(x0, top)])
        sh.path(d, fill, "#0E0C09", 1.2)
        a, b = KE(x0, top), KE(x1, bottom)
        hatch(sh, d, (a[0], a[1], b[0], b[1]), "#635C4C", 5, .7)


def kit_section_line(sh, y=0.0):
    """Section line A–A across the plan at kit y, with its arrows and letters."""
    sh.line(*KP(-6.6, y), *KP(6.6, y), "#9A9078", .8, dash="8 3 2 3")
    for x in (-6.6, 6.6):
        p = KP(x, y)
        sh.path(f"M{f(p[0])} {f(p[1])} l0 -10 l-4 5 m4 -5 l4 5", "none", "#9A9078", 1)
        sh.text(p[0], p[1] + 12, "A", 9, "#9A9078", "middle")


def kit_clear_note(sh, zone, x=4.4, text=None):
    """The clear-height note under the wall top."""
    top = FZ + ZONE_CLEAR[zone]
    sh.text(KE(x, 0)[0], KE(0, top - 0.62)[1], text or f"{ZONE_CLEAR[zone]:.2f} clear · open roof", 9, "#DCD2BA", "middle")


def kit_arch_labels(sh, zone):
    """ARCHWAY N / S labels just inside the plan's north and south archways."""
    h = ARCH_H[zone]
    socket_label(sh, *KP(0, 4.55), f"ARCHWAY N 2.60 × {h:.2f}")
    socket_label(sh, *KP(0, -4.85), f"ARCHWAY S 2.60 × {h:.2f}")


# ---------------- the kit's stairs (castle_builders._stair_to_gallery / _stair_to_dais) ----------------

def kit_gallery_section(sh, top, deck, rail, label=True):
    """The kit L-stair's railed gallery in a section at y = 0 looking north: the NW
    quadrant's deck at `top` above the floor, its posts, the east and south rails,
    and the bridge over the walkway cut by the section plane (hatched)."""
    g0 = FZ + top
    kerect(sh, -IN, g0 - 0.20, -Q0, g0, f"url(#{sh.lin(deck, 'v', .25, .5)})", darken(deck, .6), .8)
    for x in (-5.3, -1.9):
        kerect(sh, x - 0.1, FZ, x + 0.1, g0 - 0.20, f"url(#{sh.lin(deck, 'h', .25, .5)})", darken(deck, .6), .7)
    kerect(sh, -Q0 - 0.09, g0, -Q0 - 0.01, g0 + 0.90, rail, darken(rail, .6), .6)
    kerect(sh, -IN + 1.5, g0 + 0.82, -Q0, g0 + 0.90, rail, darken(rail, .6), .6)
    d = poly_path([KE(-IN, g0 - 0.20), KE(-IN + 1.5, g0 - 0.20), KE(-IN + 1.5, g0), KE(-IN, g0)])
    sh.path(d, deck, "#0E0C09", 1.2)
    a, b = KE(-IN, g0), KE(-IN + 1.5, g0 - 0.2)
    hatch(sh, d, (a[0], a[1], b[0], b[1]), "#635C4C", 4, .8)
    if label:
        sh.text(*KE(-3.0, g0 + 0.25), f"GALLERY +{top:.2f}", 9, "#DCD2BA", "middle")


def kit_l_stair_plan(sh, top, flight, deck, rail="#DCD2BA"):
    """The kit L-stair in plan: four steps west along the south wall from the walkway,
    the corner landing, four steps north along the west wall, the bridge over the
    walkway and the railed gallery filling the NW quadrant; posts and the climb line."""
    steps = 4
    x0, x_land = -Q0 - 0.1, -IN + 1.5
    run_x = (x0 - x_land) / steps
    for i in range(steps):
        xa = x0 - i * run_x
        kprect(sh, xa - run_x, -IN, xa, -IN + 1.5, mix(flight, "#14120E", .45 - i * .06), "#0E0C09", .6)
    kprect(sh, -IN, -IN, -IN + 1.5, -IN + 1.5, mix(flight, "#14120E", .15), "#0E0C09", .6)
    y0, y_top = -IN + 1.5, -Q0 - 0.2
    run_y = (y_top - y0) / steps
    for i in range(steps):
        ya = y0 + i * run_y
        kprect(sh, -IN, ya, -IN + 1.5, ya + run_y, mix(flight, "#14120E", .1 - i * .02), "#0E0C09", .6)
    kprect(sh, -IN, -Q0 - 0.2, -IN + 1.5, Q0 + 0.2, deck, "#0E0C09", .6)
    kprect(sh, -IN, Q0, -Q0, IN, lighten(deck, .1), "#0E0C09", .8)
    for x, y in ((-Q0 - 0.1, Q0 + 0.1), (-Q0 - 0.1, IN - 0.2), (-IN + 0.2, Q0 + 0.1)):
        kprect(sh, x - 0.1, y - 0.1, x + 0.1, y + 0.1, darken(deck, .3), "#0E0C09", .6)
    kprect(sh, -Q0 - 0.1, Q0, -Q0, IN, rail)
    kprect(sh, -IN + 1.5, Q0, -Q0, Q0 + 0.1, rail)
    sh.path(f"M{f(KP(-2.9, -4.75)[0])} {f(KP(-2.9, -4.75)[1])} L{f(KP(-4.6, -4.75)[0])} {f(KP(-4.6, -4.75)[1])} "
            f"L{f(KP(-4.75, -4.6)[0])} {f(KP(-4.75, -4.6)[1])} L{f(KP(-4.75, -2.6)[0])} {f(KP(-4.75, -2.6)[1])}",
            "none", "#DCD2BA", 1, extra='stroke-dasharray="3 2"')
    socket_label(sh, *KP(-3.2, -3.6), "STAIR UP")
    socket_label(sh, *KP(-3.6, 3.0), f"GALLERY +{top:.2f}")
