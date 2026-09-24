"""Shared helpers for the Late Medieval concept sheets. Author in metres, emit SVG pixels."""
import re, random, math

MONO = "Overpass Mono, monospace"
TITLE = "Eczar, Georgia, serif"
AGE_NAME = "THE LATE MEDIEVAL"
REPO_CONCEPT = "/home/user/PlunderSpell/docs/art/concept/late/"

TOK = re.compile(r"[MLCQSTZmlcqstz]|-?\d*\.?\d+(?:e-?\d+)?")


class View:
    """Maps metre coordinates (x right, y up) to sheet pixels."""
    def __init__(self, cx, gy, s, flip=False):
        self.cx, self.gy, self.s, self.flip = cx, gy, s, flip

    def x(self, m):
        return self.cx + (-m if self.flip else m) * self.s

    def y(self, m):
        return self.gy - m * self.s

    def p(self, d, mirror=False):
        out, nums = [], []
        for t in TOK.findall(d):
            if t.isalpha():
                out.append(t)
            else:
                nums.append(float(t))
                if len(nums) == 2:
                    mx = -nums[0] if mirror else nums[0]
                    out.append(f"{self.x(mx):.1f} {self.y(nums[1]):.1f}")
                    nums = []
        return " ".join(out)


class Sheet:
    def __init__(self, slug):
        self.slug = slug
        self.defs, self.body, self.top = [], [], []
        self.n = 0

    def d(self, s):
        self.defs.append(s)

    def add(self, s):
        self.body.append(s)

    def path(self, v, d, fill="none", stroke=None, sw=1, extra="", mirror=False, both=False):
        st = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
        for m in ([False, True] if both else [mirror]):
            self.add(f'<path d="{v.p(d, m)}" fill="{fill}"{st} {extra}/>')

    def lin(self, gid, stops, x1=0, y1=0, x2=1, y2=0):
        s = "".join(f'<stop offset="{o}" stop-color="{c}"' + (f' stop-opacity="{a}"' if a is not None else "") + "/>"
                    for o, c, *rest in stops for a in [rest[0] if rest else None])
        self.d(f'<linearGradient id="{gid}" x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}">{s}</linearGradient>')

    def rad(self, gid, stops, cx=.5, cy=.5, r=.5, fx=None, fy=None):
        s = "".join(f'<stop offset="{o}" stop-color="{c}"' + (f' stop-opacity="{a}"' if a is not None else "") + "/>"
                    for o, c, *rest in stops for a in [rest[0] if rest else None])
        f = f' fx="{fx}" fy="{fy}"' if fx is not None else ""
        self.d(f'<radialGradient id="{gid}" cx="{cx}" cy="{cy}" r="{r}"{f}>{s}</radialGradient>')

    def flecks(self, v, box, n, colour, seed, rmin=0.003, rmax=0.009, opacity=0.6, clip=None):
        rnd = random.Random(seed)
        x0, y0, x1, y1 = box
        g = [f'<g fill="{colour}" opacity="{opacity}"' + (f' clip-path="url(#{clip})"' if clip else "") + ">"]
        for _ in range(n):
            x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
            r = rnd.uniform(rmin, rmax) * v.s
            g.append(f'<ellipse cx="{v.x(x):.1f}" cy="{v.y(y):.1f}" rx="{r:.1f}" ry="{r*rnd.uniform(.4,1):.1f}"/>')
        g.append("</g>")
        self.add("".join(g))

    def rivets(self, v, pts, r=0.006, fill="#8E8C84", hi="#C9C3B2"):
        g = ['<g>']
        for x, y in pts:
            g.append(f'<circle cx="{v.x(x):.1f}" cy="{v.y(y):.1f}" r="{r*v.s:.1f}" fill="{fill}"/>'
                     f'<circle cx="{v.x(x)-r*v.s*.3:.1f}" cy="{v.y(y)-r*v.s*.3:.1f}" r="{r*v.s*.4:.1f}" fill="{hi}"/>')
        g.append("</g>")
        self.add("".join(g))

    def text(self, x, y, s, size=12, fill="#DCD2BA", anchor="start", font=MONO, extra=""):
        a = f' text-anchor="{anchor}"' if anchor != "start" else ""
        self.top.append(f'<text x="{x:.1f}" y="{y:.1f}"{a} font-family="{font}" font-size="{size}" fill="{fill}" {extra}>{s}</text>')

    def callout(self, px, py, tx, ty, label, sub=None, anchor="start"):
        """Dot at the part (px,py), elbow leader, label at (tx,ty)."""
        ex = tx - 6 if anchor == "start" else tx + 6
        self.top.append(f'<circle cx="{px:.1f}" cy="{py:.1f}" r="2.6" fill="#DCD2BA"/>'
                        f'<polyline points="{px:.1f},{py:.1f} {ex:.1f},{ty-4:.1f}" fill="none" stroke="#9A9078" stroke-width="0.9"/>')
        self.text(tx, ty, label, 12, "#DCD2BA", anchor)
        if sub:
            self.text(tx, ty + 14, sub, 10.5, "#9A9078", anchor)

    # ─── frame ───
    def frame(self, kicker, name, tr1, tr2, glow_c="#2A2012", glow_xy=("50%", "70%"), grid_y=690, grid_step=55):
        self.frame_parts = (kicker, name, tr1, tr2, glow_c, glow_xy, grid_y, grid_step)

    def ladder_enemy(self):
        g = [f'<g font-family="{MONO}" font-size="11" fill="#635C4C" stroke="#635C4C">',
             '<line x1="60" y1="690" x2="60" y2="140" stroke-width="1"/>']
        for i, lab in enumerate(["0", "0.5", "1.0", "1.5", "2.0", "2.5"]):
            y = 690 - i * 110
            g.append(f'<line x1="54" y1="{y}" x2="66" y2="{y}"/><text x="72" y="{y+4}" stroke="none">{lab}</text>')
        for i in range(1, 26, 2):
            y = 690 - i * 22
            if i % 5:
                g.append(f'<line x1="57" y1="{y}" x2="63" y2="{y}" stroke-width=".6"/>')
        g.append("</g>")
        self.under = "".join(g) + HUMAN_ENEMY
        self.under += (f'<text x="470" y="730" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">FRONT</text>'
                       f'<text x="820" y="730" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">SIDE</text>')

    def palette(self, mats):
        n = len(mats)
        step = 1120 / n
        g = [f'<g id="palette" font-family="{MONO}" font-size="10" fill="#9A9078">']
        for i, (hexc, name) in enumerate(mats):
            x = 40 + i * step
            label = f"{hexc} {name}"
            fs = min(10, (step - 50) / (len(label) * 0.62))
            g.append(f'<rect x="{x:.0f}" y="748" width="34" height="18" fill="{hexc}" stroke="#332D22" stroke-width=".6"/>'
                     f'<text x="{x+40:.0f}" y="761" font-size="{fs:.1f}">{label}</text>')
        g.append("</g>")
        self.pal = "".join(g)

    def write(self):
        kicker, name, tr1, tr2, glow_c, (gx, gy), grid_y, gstep = self.frame_parts
        out = ['<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 800" width="1200" height="800">',
               "<defs>",
               f'<pattern id="grid" width="{gstep}" height="{gstep}" patternUnits="userSpaceOnUse" x="0" y="{grid_y}">'
               f'<path d="M{gstep} 0 H0 V{gstep}" fill="none" stroke="#262119" stroke-width="1"/></pattern>',
               f'<radialGradient id="glow" cx="{gx}" cy="{gy}" r="60%"><stop offset="0" stop-color="{glow_c}" stop-opacity=".9"/>'
               '<stop offset="1" stop-color="#14120E" stop-opacity="0"/></radialGradient>',
               *self.defs, "</defs>",
               '<rect width="1200" height="800" fill="#14120E"/>',
               '<rect width="1200" height="800" fill="url(#glow)"/>',
               '<rect x="0" y="120" width="1200" height="570" fill="url(#grid)"/>',
               '<line x1="40" y1="690" x2="1160" y2="690" stroke="#635C4C" stroke-width="1.5"/>',
               '<rect x="12" y="12" width="1176" height="776" fill="none" stroke="#332D22" stroke-width="1"/>',
               f'<text x="40" y="54" font-family="{MONO}" font-size="12" letter-spacing="3" fill="#635C4C">{kicker}</text>',
               f'<text x="40" y="96" font-family="{TITLE}" font-size="38" font-weight="700" fill="#DCD2BA">{name}</text>',
               f'<text x="1160" y="54" text-anchor="end" font-family="{MONO}" font-size="12" fill="#9A9078">{tr1}</text>',
               f'<text x="1160" y="76" text-anchor="end" font-family="{MONO}" font-size="12" fill="#9A9078">{tr2}</text>',
               getattr(self, "under", ""),
               '<g id="drawing">', *self.body, "</g>",
               f'<g id="callouts" font-family="{MONO}">', *self.top, "</g>",
               getattr(self, "pal", ""),
               "</svg>"]
        path = REPO_CONCEPT + self.slug + ".svg"
        txt = "\n".join(out)
        ids = re.findall(r'\bid="([^"]+)"', txt)
        dup = {i for i in ids if ids.count(i) > 1}
        assert not dup, dup
        open(path, "w").write(txt)
        print(path, "elements:", txt.count("<") - txt.count("</"))


HUMAN_ENEMY = ('<g fill="#635C4C" opacity=".45"><ellipse cx="160" cy="320" rx="20" ry="26"/>'
               '<path d="M160 346 C138 350 126 360 124 380 L118 500 L130 502 L138 410 L140 520 L142 690 L156 690 L160 540 L164 690 '
               'L178 690 L180 520 L182 410 L190 502 L202 500 L196 380 C194 360 182 350 160 346 Z"/></g>'
               '<text x="160" y="712" text-anchor="middle" font-family="Overpass Mono, monospace" font-size="10" fill="#635C4C">1.80 m</text>')


def human(x, ground, s, colour="#635C4C", opacity=.45):
    """1.80 m reference human at arbitrary scale s px/m, feet centred at x on ground."""
    k = s * 1.8 / 396.0
    return (f'<g transform="translate({x - 160*k:.1f},{ground - 690*k:.1f}) scale({k:.4f})" fill="{colour}" opacity="{opacity}">'
            + HUMAN_ENEMY.split('<text')[0].replace('<g fill="#635C4C" opacity=".45">', '').replace('</g>', '') + '</g>')


# ─── item + structure helpers ───
def item_under(sh, bar_m, bar_px, bar_label, x=40, y=704, views=((380, "THREE-QUARTER"), (800, "FRONT"), (1030, "SIDE"))):
    """Scale bar (bar_m metres = bar_px px) plus view labels under the ground line."""
    g = [f'<g font-family="{MONO}" font-size="10" fill="#9A9078">']
    half = bar_px / 2
    g.append(f'<rect x="{x}" y="{y}" width="{half:.1f}" height="6" fill="#9A9078"/>'
             f'<rect x="{x+half:.1f}" y="{y}" width="{half:.1f}" height="6" fill="none" stroke="#9A9078"/>'
             f'<text x="{x+bar_px+10:.1f}" y="{y+7}">{bar_label}</text>')
    g.append("</g>")
    for vx, lab in views:
        g.append(f'<text x="{vx}" y="730" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" fill="#635C4C">{lab}</text>')
    sh.under = "".join(g)


def grab(sh, px, py, label="GRAB", dx=14, dy=-10, anchor="start"):
    sh.top.append(f'<circle cx="{px:.1f}" cy="{py:.1f}" r="7" fill="none" stroke="#5FA288" stroke-width="2"/>'
                  f'<circle cx="{px:.1f}" cy="{py:.1f}" r="2.2" fill="#5FA288"/>'
                  f'<text x="{px+dx:.1f}" y="{py+dy:.1f}" font-family="{MONO}" font-size="10" letter-spacing="1.5" fill="#5FA288"'
                  + (f' text-anchor="{anchor}"' if anchor != "start" else "") + f'>{label}</text>')


def crack(sh, pts_px, w=1.6):
    d = "M " + " L ".join(f"{x:.1f} {y:.1f}" for x, y in pts_px)
    sh.top.append(f'<path d="{d}" fill="none" stroke="#C4542E" stroke-width="{w}" stroke-dasharray="5 3"/>')


def struct_under(sh, s=55, top_m=7, marks=()):
    """Height ladder (1 m ticks from the ground) and a 1.80 m human is placed by the caller."""
    g = [f'<g font-family="{MONO}" font-size="11" fill="#635C4C" stroke="#635C4C">',
         f'<line x1="60" y1="690" x2="60" y2="{690 - top_m*s}" stroke-width="1"/>']
    for i in range(top_m + 1):
        y = 690 - i * s
        g.append(f'<line x1="54" y1="{y}" x2="66" y2="{y}"/><text x="42" y="{y+4}" stroke="none" text-anchor="end">{i}</text>')
        if i < top_m:
            g.append(f'<line x1="57" y1="{y - s/2}" x2="63" y2="{y - s/2}" stroke-width=".6"/>')
    g.append("</g>")
    for (m, lab) in marks:
        y = 690 - m * s
        g.append(f'<line x1="66" y1="{y:.1f}" x2="96" y2="{y:.1f}" stroke="#9A9078" stroke-dasharray="3 3"/>'
                 f'<text x="70" y="{y-4:.1f}" font-family="{MONO}" font-size="9" fill="#9A9078">{lab}</text>')
    sh.under = "".join(g)


def keyhole(cx, cy, s, fill="#0B0A08", stroke="#8A4B32", slit_h=0.9, hole=0.20, slit_w=0.08):
    """Keyhole gun-loop centred on its round hole at (cx, cy) px; s px/m."""
    r = hole / 2 * s
    w = slit_w / 2 * s
    return (f'<path d="M {cx-w:.1f} {cy:.1f} L {cx-w:.1f} {cy - slit_h*s:.1f} L {cx+w:.1f} {cy - slit_h*s:.1f} L {cx+w:.1f} {cy:.1f} Z" fill="{fill}" stroke="{stroke}" stroke-width="1.2"/>'
            f'<circle cx="{cx:.1f}" cy="{cy:.1f}" r="{r:.1f}" fill="{fill}" stroke="{stroke}" stroke-width="1.2"/>')
