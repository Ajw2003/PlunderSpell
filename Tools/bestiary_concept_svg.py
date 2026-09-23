"""Hand-authored vector concept sheets for the Plunderspell bestiary brief (18 Sept 2026).

One sheet per enemy plus a to-scale lineup. Every figure is drawn as explicit SVG geometry in
metres (y up, 0 = floor) so heights are exact against the 1.80 m standard human in
docs/systems/scale.md. Humanoids are hung off the EnemyForge proportion table so the household
reads as one family at different sizes.

Run:  python3 Tools/bestiary_concept_svg.py
Out:  docs/generated/bestiary/*.svg
"""

import math
import re
import textwrap
from pathlib import Path

OUT_DIR = Path(__file__).resolve().parent.parent / "docs" / "generated" / "bestiary"

# Shared human proportions, copied from Tools/EnemyForge/enemy_forge/archetypes.py.
ANKLE, KNEE, HIP = 0.050, 0.278, 0.528
WAIST, CHEST, SHOULDER, NECK = 0.590, 0.700, 0.812, 0.826
SHOULDER_X, HIP_X = 0.106, 0.056

# Pigments from docs/generated/plunderspell-moodboard.html; everything else is the drab register.
INK = "#14120E"
ASH = "#1E1A14"
VELLUM = "#DCD2BA"
VELLUM_DIM = "#9A9078"
VELLUM_FAINT = "#635C4C"
ORPIMENT = "#C9A227"
MADDER = "#C4542E"
LAPIS = "#7A6AA0"
VERDIGRIS = "#5FA288"

C = dict(
    wool="#6B5A45", wool_dk="#4E4132", wool_lt="#85725A",
    cape="#5A5247", cape_dk="#433D35",
    hose="#5F5A50", hose_dk="#46423B",
    linen="#CFC4A8", linen_dk="#A99E83", linen_grave="#B3A98F", linen_grave_dk="#8C8370",
    bone="#D8CFB6", bone_dk="#AFA58C",
    leather="#5A3E28", leather_dk="#3E2A1B",
    oak="#7A5B3A", oak_dk="#553E27",
    iron="#5E6166", iron_dk="#43464B",
    steel="#8E959C", steel_lt="#BFC5CA", steel_dk="#666D74",
    mail="#6F7378",
    skin="#B08A6A", skin_dk="#8C6B50", skin_old="#A88468",
    hair_grey="#9C978C", hair_dk="#4A3F33",
    tallow="#E89A3C", flame="#FFE2B0", horn_pane="#A8834F",
    madder=MADDER, madder_dk="#8E3A1F",
    lapis=LAPIS, lapis_glow="#B6A8F0",
    gild="#8C7A45", gild_dk="#5F5231",
    vest="#3F3A33", vest_band="#5B4330",
    stone="#5A5750", stone_dk="#3F3D38", stone_lt="#716D64",
    gambeson="#8E846C", gambeson_dk="#6C6452",
    hawk="#6A6258", hawk_dk="#4A443C", hawk_pale="#C2B89E",
    hound="#6E5A43", hound_dk="#4B3D2D",
    smoke="#9A958A",
)

STROKE = f'stroke="{INK}" stroke-width="0.012" stroke-linejoin="round" stroke-linecap="round"'
THIN = f'stroke="{INK}" stroke-width="0.007" stroke-linejoin="round" stroke-linecap="round"'


# ------------------------------------------------------------------ primitives (metre space)

def n(v):
    return f"{v:.4f}".rstrip("0").rstrip(".") if abs(v) > 1e-9 else "0"


def pt(p):
    return f"{n(p[0])},{n(p[1])}"


def poly_d(points):
    return "M" + " L".join(pt(p) for p in points) + " Z"


def smooth_d(points, closed=True):
    """Catmull-Rom through the points, emitted as cubic Béziers - for cloth and flesh."""
    pts = list(points)
    count = len(pts)
    d = f"M{pt(pts[0])}"
    last = count if closed else count - 1
    for i in range(last):
        p0 = pts[(i - 1) % count] if closed or i > 0 else pts[0]
        p1 = pts[i]
        p2 = pts[(i + 1) % count]
        p3 = pts[(i + 2) % count] if closed or i + 2 < count else pts[-1]
        c1 = (p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6)
        c2 = (p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6)
        d += f" C{pt(c1)} {pt(c2)} {pt(p2)}"
    return d + (" Z" if closed else "")


def shape(d, fill, shade=0.0, stroke=STROKE, extra=""):
    out = f'<path d="{d}" fill="{fill}" {stroke} {extra}/>'
    if shade:
        out += f'<path d="{d}" fill="#000" opacity="{shade}"/>'
    return out


def poly(points, fill, shade=0.0, stroke=STROKE, extra=""):
    return shape(poly_d(points), fill, shade, stroke, extra)


def blob(points, fill, shade=0.0, stroke=STROKE, extra=""):
    return shape(smooth_d(points), fill, shade, stroke, extra)


def circle(c, r, fill, stroke=STROKE, extra=""):
    return f'<circle cx="{n(c[0])}" cy="{n(c[1])}" r="{n(r)}" fill="{fill}" {stroke} {extra}/>'


def ellipse(c, rx, ry, fill, rot=0.0, stroke=STROKE, extra=""):
    return (f'<ellipse cx="{n(c[0])}" cy="{n(c[1])}" rx="{n(rx)}" ry="{n(ry)}" fill="{fill}" '
            f'transform="rotate({n(rot)} {n(c[0])} {n(c[1])})" {stroke} {extra}/>')


def line(a, b, color=INK, width=0.008, extra=""):
    return (f'<path d="M{pt(a)} L{pt(b)}" stroke="{color}" stroke-width="{n(width)}" '
            f'stroke-linecap="round" fill="none" {extra}/>')


def curve(points, color=INK, width=0.008, extra=""):
    return f'<path d="{smooth_d(points, closed=False)}" stroke="{color}" stroke-width="{n(width)}" stroke-linecap="round" fill="none" {extra}/>'


def fold(points, opacity=0.35, width=0.007):
    return curve(points, INK, width, f'opacity="{opacity}"')


def taper(a, b, wa, wb, fill, shade=0.0, stroke=STROKE, extra=""):
    """A limb segment from a to b, width wa at a and wb at b, with rounded ends."""
    dx, dy = b[0] - a[0], b[1] - a[1]
    length = math.hypot(dx, dy) or 1e-6
    nx, ny = -dy / length, dx / length
    a1 = (a[0] + nx * wa / 2, a[1] + ny * wa / 2)
    a2 = (a[0] - nx * wa / 2, a[1] - ny * wa / 2)
    b1 = (b[0] + nx * wb / 2, b[1] + ny * wb / 2)
    b2 = (b[0] - nx * wb / 2, b[1] - ny * wb / 2)
    d = (f"M{pt(a1)} L{pt(b1)} A{n(wb / 2)},{n(wb / 2)} 0 0 0 {pt(b2)} "
         f"L{pt(a2)} A{n(wa / 2)},{n(wa / 2)} 0 0 0 {pt(a1)} Z")
    return shape(d, fill, shade, stroke, extra)


def glow(c, r, color, opacity=0.9):
    return (f'<circle cx="{n(c[0])}" cy="{n(c[1])}" r="{n(r)}" fill="{color}" opacity="{opacity}" '
            f'filter="url(#glowBlur)" data-fx="1"/>')


def star5(c, r):
    pts = []
    for i in range(10):
        rr = r if i % 2 == 0 else r * 0.42
        a = math.radians(90 + i * 36)
        pts.append((c[0] + rr * math.cos(a), c[1] + rr * math.sin(a)))
    return pts


class Rig:
    """Joint positions for one household man: the shared proportion table times his height."""

    def __init__(self, height, build=1.0):
        self.h = height
        self.b = build

    def p(self, fx, fy):
        return (fx * self.h, fy * self.h)

    def w(self, frac):
        return frac * self.h * self.b


def face(c, rx, ry, skin, turn=0.0, brow_drop=0.0, lids=0.0, beard=None, stubble=False,
         scar=False, age=0.0, mouth_down=0.0):
    """A plain human face. `turn` shifts features sideways (-1..1) to sell a turned head."""
    out = [ellipse(c, rx, ry, skin)]
    ex = rx * 0.38
    ey = c[1] + ry * 0.12
    shift = turn * rx * 0.35
    for side in (-1, 1):
        ecx = c[0] + side * ex + shift
        squeeze = 0.75 if side * turn > 0.3 else 1.0
        out.append(ellipse((ecx, ey), rx * 0.13 * squeeze, ry * 0.07, INK, stroke=""))
        brow_y = ey + ry * (0.2 - brow_drop)
        out.append(line((ecx - rx * 0.2, brow_y + side * ry * 0.02 * (1 if brow_drop else 0)),
                        (ecx + rx * 0.2, brow_y - side * ry * 0.02 * (1 if brow_drop else 0)), INK, ry * 0.07))
        if lids:
            out.append(line((ecx - rx * 0.16, ey + ry * 0.04), (ecx + rx * 0.16, ey + ry * 0.04), skin, ry * 0.08 * lids))
            out.append(curve([(ecx - rx * 0.15, ey - ry * 0.1), (ecx, ey - ry * 0.15), (ecx + rx * 0.15, ey - ry * 0.1)],
                             INK, ry * 0.02, 'opacity="0.5"'))
        if age:
            out.append(line((ecx + side * rx * 0.2, ey), (ecx + side * rx * 0.32, ey - ry * 0.06), INK, ry * 0.02,
                            f'opacity="{0.5 * age}"'))
    nose_x = c[0] + shift * 1.3
    out.append(curve([(nose_x, ey - ry * 0.02), (nose_x - rx * 0.1 * (1 if turn >= 0 else -1), ey - ry * 0.32),
                      (nose_x, ey - ry * 0.38)], INK, ry * 0.035, 'opacity="0.7"'))
    my = c[1] - ry * 0.52
    out.append(curve([(nose_x - rx * 0.28, my - ry * mouth_down), (nose_x, my), (nose_x + rx * 0.28, my - ry * mouth_down)],
                     INK, ry * 0.035))
    if stubble:
        out.append(shape(smooth_d([(c[0] - rx * 0.85, c[1] - ry * 0.2), (c[0], c[1] - ry * 1.0),
                                   (c[0] + rx * 0.85, c[1] - ry * 0.2), (c[0], c[1] - ry * 0.62)]),
                         "#3A3129", stroke="", extra='opacity="0.28"'))
    if beard:
        out.append(blob([(c[0] - rx * 0.9, c[1] - ry * 0.15), (c[0] - rx * 0.55, c[1] - ry * 0.9),
                         (c[0] + shift, c[1] - ry * (1.35 + (0.5 if beard == "long" else 0))),
                         (c[0] + rx * 0.55, c[1] - ry * 0.9), (c[0] + rx * 0.9, c[1] - ry * 0.15),
                         (nose_x + rx * 0.3, my + ry * 0.05), (nose_x - rx * 0.3, my + ry * 0.05)],
                        C["hair_grey"], stroke=THIN))
        out.append(curve([(nose_x - rx * 0.28, my), (nose_x, my + ry * 0.02), (nose_x + rx * 0.28, my)], INK, ry * 0.04))
    if scar:
        out.append(line((c[0] - rx * 0.75 + shift, ey + ry * 0.35), (c[0] - rx * 0.05 + shift, ey - ry * 0.55),
                        "#6E4A3A", ry * 0.05))
        out.append(line((c[0] + rx * 0.3 + shift, c[1] - ry * 0.35), (c[0] + rx * 0.75 + shift, c[1] - ry * 0.1),
                        "#6E4A3A", ry * 0.04))
    return "".join(out)


def mail_skirt(points, shade=0.0):
    return poly(points, "url(#mail)", shade)


def foot(ankle, length, height, fill, direction=-1, shade=0.0):
    x, y = ankle
    pts = [(x - direction * length * 0.25, y + height * 0.5), (x - direction * length * 0.3, 0),
           (x + direction * length * 0.75, 0), (x + direction * length * 0.8, height * 0.35),
           (x + direction * length * 0.3, height * 0.8)]
    return blob(pts, fill, shade)


def hand(c, r, skin, shade=0.0):
    return circle(c, r, skin) + (circle(c, r, "#000", stroke="", extra=f'opacity="{shade}"') if shade else "")


# ------------------------------------------------------------------ the ten designs

def watchman():
    r = Rig(1.75, 1.0)
    p, w = r.p, r.w
    out = []
    # Lantern light on the floor and air first, so it sits behind everything.
    out.append(ellipse(p(-0.14, 0.01), w(0.42), w(0.05), C["tallow"], stroke="", extra='opacity="0.18" filter="url(#softBlur)" data-fx="1"'))
    out.append(glow(p(-0.13, 0.40), w(0.30), C["tallow"], 0.22))
    # Staff behind the far hand.
    out.append(taper(p(0.155, 0.0), p(0.182, 1.03), w(0.022), w(0.02), C["oak"]))
    out.append(circle(p(0.183, 1.035), w(0.02), C["oak_dk"]))
    # Far leg and foot.
    out.append(taper(p(0.04, 0.53), p(0.05, 0.28), w(0.075), w(0.055), C["hose"], 0.2))
    out.append(taper(p(0.05, 0.28), p(0.055, 0.055), w(0.055), w(0.04), C["hose"], 0.2))
    out.append(foot(p(0.055, 0.05), w(0.11), w(0.06), C["leather_dk"], -1, 0.15))
    # Near leg, stepping.
    out.append(taper(p(-0.045, 0.53), p(-0.06, 0.28), w(0.08), w(0.058), C["hose"]))
    out.append(taper(p(-0.06, 0.28), p(-0.07, 0.055), w(0.058), w(0.042), C["hose"]))
    out.append(foot(p(-0.07, 0.05), w(0.12), w(0.06), C["leather_dk"], -1))
    # Far arm (on the staff).
    out.append(taper(p(0.095, 0.79), p(0.15, 0.665), w(0.055), w(0.045), C["wool"], 0.25))
    out.append(taper(p(0.15, 0.665), p(0.165, 0.60), w(0.045), w(0.04), C["wool"], 0.25))
    # Tunic, knee length, undyed wool.
    out.append(blob([p(-0.05, 0.83), p(-0.11, 0.80), p(-0.105, 0.70), p(-0.09, 0.59), p(-0.12, 0.44),
                     p(-0.155, 0.31), p(-0.05, 0.29), p(0.05, 0.305), p(0.145, 0.315), p(0.115, 0.45),
                     p(0.085, 0.59), p(0.10, 0.70), p(0.10, 0.80), p(0.05, 0.83)], C["wool"]))
    out.append(fold([p(-0.06, 0.55), p(-0.08, 0.42), p(-0.09, 0.32)]))
    out.append(fold([p(0.02, 0.56), p(0.03, 0.43), p(0.02, 0.31)]))
    out.append(fold([p(0.07, 0.55), p(0.1, 0.33)]))
    out.append(shape(smooth_d([p(0.02, 0.83), p(0.10, 0.80), p(0.10, 0.70), p(0.085, 0.59), p(0.115, 0.45),
                               p(0.145, 0.315), p(0.06, 0.305)]), "#000", stroke="", extra='opacity="0.18"'))
    # Belt, buckle and coin pouch.
    out.append(poly([p(-0.093, 0.605), p(0.088, 0.605), p(0.086, 0.578), p(-0.091, 0.578)], C["leather"]))
    out.append(poly([p(-0.02, 0.609), p(0.01, 0.609), p(0.01, 0.574), p(-0.02, 0.574)], "none", stroke=f'stroke="{C["iron"]}" stroke-width="0.012"'))
    out.append(line(p(0.045, 0.578), p(0.05, 0.545), C["leather_dk"], w(0.006)))
    out.append(blob([p(0.03, 0.548), p(0.075, 0.55), p(0.078, 0.51), p(0.052, 0.495), p(0.028, 0.51)], C["leather"]))
    out.append(poly([p(0.03, 0.548), p(0.075, 0.55), p(0.07, 0.528), p(0.035, 0.528)], C["leather_dk"]))
    # Hooded shoulder-cape: the cheapest way to say civilian.
    out.append(blob([p(-0.07, 0.87), p(-0.13, 0.83), p(-0.16, 0.75), p(-0.158, 0.672), p(-0.12, 0.66),
                     p(-0.07, 0.645), p(-0.02, 0.655), p(0.03, 0.642), p(0.09, 0.656), p(0.145, 0.668),
                     p(0.15, 0.75), p(0.12, 0.83), p(0.07, 0.87)], C["cape"]))
    out.append(fold([p(-0.08, 0.8), p(-0.1, 0.68)]))
    out.append(fold([p(0.06, 0.8), p(0.08, 0.67)]))
    out.append(fold([p(-0.01, 0.8), p(-0.02, 0.66)]))
    # Near arm, carrying the lantern low.
    out.append(taper(p(-0.105, 0.79), p(-0.135, 0.645), w(0.06), w(0.048), C["wool"]))
    out.append(taper(p(-0.135, 0.645), p(-0.125, 0.51), w(0.048), w(0.042), C["wool"]))
    # The horn lantern: four-post frame, horn panes, tallow candle.
    lx, top, bot = -0.125, 0.47, 0.36
    out.append(line(p(lx, 0.505), p(lx, top + 0.012), C["iron_dk"], w(0.006)))
    out.append(ellipse(p(lx, 0.50), w(0.012), w(0.01), "none", stroke=f'stroke="{C["iron_dk"]}" stroke-width="0.008"'))
    out.append(poly([p(lx - 0.038, top), p(lx + 0.038, top), p(lx + 0.038, bot), p(lx - 0.038, bot)], C["horn_pane"], extra='opacity="0.92"'))
    out.append(glow(p(lx, 0.405), w(0.05), C["tallow"], 0.95))
    out.append(taper(p(lx, bot + 0.012), p(lx, 0.392), w(0.016), w(0.016), C["bone"], stroke=THIN))
    out.append(shape(smooth_d([p(lx, 0.44), p(lx + 0.009, 0.405), p(lx, 0.393), p(lx - 0.009, 0.405)]), C["tallow"], stroke=""))
    out.append(shape(smooth_d([p(lx, 0.428), p(lx + 0.004, 0.405), p(lx, 0.397), p(lx - 0.004, 0.405)]), C["flame"], stroke=""))
    for px in (-0.04, 0.0, 0.04):
        out.append(line(p(lx + px, top + 0.004), p(lx + px, bot - 0.004), C["oak_dk"], w(0.009)))
    out.append(poly([p(lx - 0.048, top), p(lx + 0.048, top), p(lx + 0.03, top + 0.03), p(lx - 0.03, top + 0.03)], C["iron"]))
    out.append(poly([p(lx - 0.045, bot), p(lx + 0.045, bot), p(lx + 0.045, bot - 0.012), p(lx - 0.045, bot - 0.012)], C["iron"]))
    out.append(hand(p(-0.125, 0.51), w(0.022), C["skin"]))
    out.append(hand(p(0.166, 0.60), w(0.022), C["skin"], 0.15))
    # Hood up, liripipe hanging behind; a tired face three hours into the shift.
    hc = p(-0.01, 0.922)
    out.append(blob([p(-0.075, 0.87), p(-0.08, 0.95), p(-0.04, 1.0), p(0.03, 1.005), p(0.1, 0.975),
                     p(0.14, 0.93), p(0.12, 0.905), p(0.075, 0.93), p(0.07, 0.87)], C["cape"]))
    out.append(face(hc, w(0.043), w(0.058), C["skin"], turn=-0.15, lids=1.0, stubble=True, mouth_down=0.06, brow_drop=0.05))
    out.append(shape(smooth_d([p(-0.06, 0.875), p(-0.062, 0.94), p(-0.035, 0.99), p(0.015, 0.99), p(0.045, 0.95),
                               p(0.045, 0.875), p(0.035, 0.955), p(0.0, 0.978), p(-0.036, 0.968), p(-0.05, 0.93)]),
                     C["cape_dk"]))
    return "".join(out)


def man_at_arms():
    r = Rig(1.80, 1.02)
    p, w = r.p, r.w
    out = []
    # Far leg in mail chausses.
    out.append(taper(p(0.045, 0.53), p(0.06, 0.28), w(0.078), w(0.058), "url(#mail)", 0.28))
    out.append(taper(p(0.06, 0.28), p(0.07, 0.055), w(0.058), w(0.042), "url(#mail)", 0.28))
    out.append(foot(p(0.07, 0.05), w(0.11), w(0.06), C["leather_dk"], -1, 0.15))
    # Near leg, mid-stride.
    out.append(taper(p(-0.045, 0.53), p(-0.075, 0.285), w(0.082), w(0.06), "url(#mail)"))
    out.append(taper(p(-0.075, 0.285), p(-0.095, 0.055), w(0.06), w(0.044), "url(#mail)"))
    out.append(foot(p(-0.095, 0.05), w(0.12), w(0.06), C["leather_dk"], -1))
    # Far arm in mail, swinging back slightly.
    out.append(taper(p(0.1, 0.79), p(0.135, 0.64), w(0.058), w(0.048), "url(#mail)", 0.28))
    out.append(taper(p(0.135, 0.64), p(0.15, 0.51), w(0.048), w(0.04), "url(#mail)", 0.28))
    out.append(hand(p(0.152, 0.50), w(0.022), C["leather"], 0.2))
    # Hauberk shows at the shoulders and split skirt.
    out.append(blob([p(-0.11, 0.81), p(-0.12, 0.7), p(-0.11, 0.55), p(-0.12, 0.42), p(0.12, 0.42), p(0.11, 0.55),
                     p(0.11, 0.7), p(0.105, 0.81), p(0.0, 0.835)], "url(#mail)"))
    # Undyed linen surcoat, split front for walking.
    out.append(blob([p(-0.07, 0.815), p(-0.1, 0.77), p(-0.095, 0.62), p(-0.13, 0.42), p(-0.16, 0.25),
                     p(-0.04, 0.24), p(-0.012, 0.42), p(0.012, 0.42), p(0.03, 0.255), p(0.15, 0.265),
                     p(0.12, 0.42), p(0.09, 0.62), p(0.095, 0.77), p(0.065, 0.815)], C["linen"]))
    out.append(shape(smooth_d([p(0.01, 0.81), p(0.095, 0.77), p(0.09, 0.62), p(0.12, 0.42), p(0.15, 0.265),
                               p(0.03, 0.255), p(0.012, 0.42)]), "#000", stroke="", extra='opacity="0.14"'))
    out.append(poly([p(-0.012, 0.42), p(0.012, 0.42), p(0.03, 0.255), p(-0.04, 0.24)], "url(#mail)", extra='opacity="0.95"'))
    for a, b in (((-0.06, 0.56), (-0.1, 0.27)), ((0.05, 0.56), (0.1, 0.28)), ((-0.03, 0.75), (-0.05, 0.62))):
        out.append(fold([p(*a), p(*b)]))
    # Sword belt and sheathed arming sword at the left hip.
    out.append(poly([p(-0.1, 0.60), p(0.095, 0.585), p(0.093, 0.56), p(-0.098, 0.575)], C["leather"]))
    out.append(line(p(0.07, 0.57), p(0.2, 0.2), C["leather_dk"], w(0.036)))
    out.append(line(p(0.07, 0.57), p(0.2, 0.2), C["leather"], w(0.026)))
    out.append(poly([p(0.19, 0.225), p(0.21, 0.225), p(0.205, 0.195), p(0.2, 0.19)], C["iron"]))
    out.append(line(p(0.03, 0.62), p(0.075, 0.585), C["steel_dk"], w(0.012)))
    out.append(line(p(0.035, 0.575), p(0.1, 0.61), C["steel"], w(0.014)))
    out.append(line(p(0.055, 0.60), p(0.02, 0.645), C["leather_dk"], w(0.016)))
    out.append(circle(p(0.012, 0.652), w(0.012), C["steel"]))
    # Near arm, hand resting forward.
    out.append(taper(p(-0.108, 0.79), p(-0.14, 0.645), w(0.062), w(0.05), "url(#mail)"))
    out.append(taper(p(-0.14, 0.645), p(-0.125, 0.51), w(0.05), w(0.042), "url(#mail)"))
    out.append(hand(p(-0.122, 0.50), w(0.023), C["leather"]))
    # Mail coif and kettle helm with a wide beaten brim; head turned toward a noise.
    out.append(blob([p(-0.07, 0.82), p(-0.075, 0.9), p(-0.05, 0.955), p(0.05, 0.955), p(0.075, 0.9), p(0.07, 0.82),
                     p(0.0, 0.8)], "url(#mail)"))
    out.append(face(p(0.004, 0.905), w(0.036), w(0.045), C["skin"], turn=0.75, brow_drop=0.02))
    out.append(blob([p(-0.045, 0.9), p(-0.04, 0.95), p(0.0, 0.965), p(0.04, 0.95), p(0.048, 0.9), p(0.04, 0.945),
                     p(0.0, 0.952), p(-0.035, 0.94)], "url(#mail)"))
    out.append(ellipse(p(0.0, 0.962), w(0.115), w(0.022), C["steel"], rot=-3))
    out.append(ellipse(p(0.0, 0.958), w(0.115), w(0.016), "#000", rot=-3, stroke="", extra='opacity="0.25"'))
    out.append(shape(smooth_d([p(-0.06, 0.965), p(-0.055, 1.0), p(0.0, 1.02), p(0.055, 1.0), p(0.06, 0.965)]), C["steel"]))
    out.append(fold([p(-0.03, 0.975), p(-0.02, 1.005)], 0.4))
    out.append(line(p(-0.1, 0.962), p(0.09, 0.967), C["steel_lt"], w(0.004), 'opacity="0.6"'))
    return "".join(out)


def sergeant():
    r = Rig(1.85, 1.14)
    p, w = r.p, r.w
    out = []
    # Wide stance: far leg.
    out.append(taper(p(0.055, 0.53), p(0.09, 0.28), w(0.078), w(0.058), "url(#mail)", 0.28))
    out.append(taper(p(0.09, 0.28), p(0.11, 0.055), w(0.058), w(0.044), "url(#mail)", 0.28))
    out.append(ellipse(p(0.09, 0.285), w(0.035), w(0.03), C["steel"], extra='opacity="1"'))
    out.append(foot(p(0.11, 0.05), w(0.11), w(0.06), C["leather_dk"], 1, 0.15))
    # Near leg.
    out.append(taper(p(-0.055, 0.53), p(-0.095, 0.28), w(0.082), w(0.06), "url(#mail)"))
    out.append(taper(p(-0.095, 0.28), p(-0.115, 0.055), w(0.06), w(0.046), "url(#mail)"))
    out.append(ellipse(p(-0.095, 0.285), w(0.037), w(0.032), C["steel"]))
    out.append(foot(p(-0.115, 0.05), w(0.12), w(0.06), C["leather_dk"], -1))
    # Far arm.
    out.append(taper(p(0.11, 0.79), p(0.15, 0.64), w(0.06), w(0.05), "url(#mail)", 0.28))
    out.append(taper(p(0.15, 0.64), p(0.16, 0.51), w(0.05), w(0.042), "url(#mail)", 0.28))
    out.append(hand(p(0.162, 0.50), w(0.024), C["leather"], 0.2))
    # Coat-of-plates over mail.
    out.append(blob([p(-0.115, 0.81), p(-0.12, 0.62), p(-0.13, 0.43), p(0.13, 0.43), p(0.12, 0.62), p(0.115, 0.81),
                     p(0.0, 0.835)], "url(#mail)"))
    out.append(blob([p(-0.1, 0.8), p(-0.105, 0.6), p(0.105, 0.6), p(0.1, 0.8), p(0.0, 0.82)], C["leather_dk"]))
    # Madder tabard over the plate: he is the alarm.
    out.append(blob([p(-0.075, 0.82), p(-0.1, 0.77), p(-0.1, 0.6), p(-0.135, 0.36), p(-0.02, 0.35),
                     p(0.0, 0.47), p(0.02, 0.35), p(0.135, 0.36), p(0.1, 0.6), p(0.1, 0.77), p(0.075, 0.82)], C["madder"]))
    out.append(shape(smooth_d([p(0.01, 0.82), p(0.1, 0.77), p(0.1, 0.6), p(0.135, 0.36), p(0.02, 0.35), p(0.0, 0.47)]),
                     "#000", stroke="", extra='opacity="0.2"'))
    for x in (-0.085, 0.085):
        for y in (0.78, 0.72, 0.66):
            out.append(circle(p(x, y), w(0.006), C["steel_lt"], stroke=""))
    for a, b in (((-0.05, 0.57), (-0.09, 0.38)), ((0.05, 0.57), (0.09, 0.38)), ((-0.02, 0.76), (-0.03, 0.62))):
        out.append(fold([p(*a), p(*b)]))
    out.append(poly([p(-0.105, 0.61), p(0.105, 0.60), p(0.103, 0.575), p(-0.103, 0.585)], C["leather"]))
    # Baldric across the chest, and the horn - rendered as clearly as the mace.
    out.append(poly([p(-0.09, 0.81), p(-0.06, 0.815), p(0.13, 0.55), p(0.1, 0.54)], C["leather"]))
    horn = smooth_d([p(0.075, 0.52), p(0.13, 0.535), p(0.19, 0.51), p(0.225, 0.46), p(0.228, 0.43),
                     p(0.215, 0.425), p(0.2, 0.46), p(0.165, 0.49), p(0.115, 0.497), p(0.078, 0.49)])
    out.append(shape(horn, C["bone"]))
    out.append(shape(smooth_d([p(0.08, 0.492), p(0.13, 0.5), p(0.18, 0.485), p(0.21, 0.44), p(0.225, 0.43),
                               p(0.215, 0.425), p(0.2, 0.46), p(0.165, 0.49), p(0.115, 0.497)]),
                     "#000", stroke="", extra='opacity="0.18"'))
    out.append(ellipse(p(0.076, 0.505), w(0.008), w(0.016), C["bone_dk"]))
    for t in (0.1, 0.155):
        out.append(line(p(t, 0.528), p(t + 0.005, 0.492), C["leather_dk"], w(0.012)))
    out.append(poly([p(0.212, 0.44), p(0.228, 0.43), p(0.231, 0.418), p(0.214, 0.42)], C["iron_dk"]))
    out.append(line(p(0.1, 0.54), p(0.1, 0.527), C["leather_dk"], w(0.006)))
    # Steel pauldrons widen the silhouette.
    for side, shd in ((-1, 0.0), (1, 0.25)):
        out.append(blob([p(side * 0.06, 0.83), p(side * 0.14, 0.825), p(side * 0.17, 0.77), p(side * 0.155, 0.72),
                         p(side * 0.1, 0.75)], C["steel"], shd))
        out.append(fold([p(side * 0.08, 0.79), p(side * 0.15, 0.775)], 0.45))
    # Near arm swinging the flanged mace, head down.
    out.append(taper(p(-0.12, 0.77), p(-0.16, 0.64), w(0.058), w(0.05), "url(#mail)"))
    out.append(taper(p(-0.16, 0.64), p(-0.17, 0.51), w(0.05), w(0.044), "url(#mail)"))
    out.append(taper(p(-0.172, 0.52), p(-0.2, 0.26), w(0.018), w(0.018), C["oak"]))
    mace_c = p(-0.205, 0.235)
    for ang in (0, 45, 90, 135):
        rad = math.radians(ang)
        dx, dy = math.cos(rad) * w(0.045), math.sin(rad) * w(0.028)
        out.append(poly([(mace_c[0] - dx, mace_c[1] - dy - w(0.02)), (mace_c[0] + dx, mace_c[1] + dy - w(0.02)),
                         (mace_c[0] + dx, mace_c[1] + dy + w(0.02)), (mace_c[0] - dx, mace_c[1] - dy + w(0.02))],
                        C["iron"], extra='opacity="0.95"'))
    out.append(ellipse(mace_c, w(0.02), w(0.04), C["iron_dk"]))
    out.append(hand(p(-0.17, 0.51), w(0.026), C["leather"]))
    # Mail aventail and flat-topped great helm with a horizontal vision slit.
    out.append(blob([p(-0.075, 0.82), p(-0.08, 0.87), p(0.08, 0.87), p(0.075, 0.82), p(0.0, 0.805)], "url(#mail)"))
    helm = [p(-0.062, 0.86), p(-0.064, 0.985), p(-0.05, 1.0), p(0.05, 1.0), p(0.064, 0.985), p(0.062, 0.86), p(0.0, 0.852)]
    out.append(poly(helm, C["steel"]))
    out.append(poly([p(0.02, 0.855), p(0.062, 0.86), p(0.064, 0.985), p(0.05, 1.0), p(0.025, 1.0)], "#000", stroke="", extra='opacity="0.22"'))
    out.append(poly([p(-0.056, 0.935), p(0.056, 0.935), p(0.056, 0.925), p(-0.056, 0.925)], INK))
    out.append(line(p(0.0, 0.86), p(0.0, 0.998), C["steel_dk"], w(0.012)))
    for i in range(4):
        out.append(circle(p(0.02 + 0.01 * (i % 2), 0.9 - 0.012 * i), w(0.004), INK, stroke=""))
    out.append(line(p(-0.064, 0.985), p(-0.03, 1.0), C["steel_lt"], w(0.004), 'opacity="0.7"'))
    return "".join(out)


def war_hound():
    out = []
    coat, dark = C["hound"], C["hound_dk"]
    out.append(ellipse((0.05, 0.0), 0.75, 0.03, "#000", stroke="", extra='opacity="0.35" filter="url(#softBlur)" data-fx="1"'))
    # Far legs first.
    out.append(taper((-0.33, 0.6), (-0.43, 0.39), 0.11, 0.075, coat, 0.28))
    out.append(taper((-0.43, 0.39), (-0.6, 0.19), 0.075, 0.055, coat, 0.28))
    out.append(ellipse((-0.64, 0.17), 0.05, 0.028, coat, rot=-35, extra=''))
    out.append(taper((0.42, 0.6), (0.47, 0.4), 0.15, 0.09, coat, 0.28))
    out.append(taper((0.47, 0.4), (0.62, 0.17), 0.09, 0.06, coat, 0.28))
    out.append(taper((0.62, 0.17), (0.72, 0.03), 0.06, 0.05, coat, 0.28))
    out.append(ellipse((0.74, 0.025), 0.05, 0.022, coat))
    # Tail streaming back.
    out.append(shape(smooth_d([(0.58, 0.77), (0.75, 0.82), (0.93, 0.83), (0.96, 0.8), (0.78, 0.76), (0.6, 0.72)]), coat))
    # Body: deep chest, tucked belly - lean, not cartoon.
    body = smooth_d([(-0.5, 0.62), (-0.42, 0.8), (-0.2, 0.9), (0.05, 0.87), (0.3, 0.8), (0.5, 0.8), (0.63, 0.7),
                     (0.6, 0.55), (0.45, 0.45), (0.28, 0.47), (0.05, 0.42), (-0.2, 0.4), (-0.42, 0.45)])
    out.append(shape(body, coat))
    out.append(shape(smooth_d([(-0.35, 0.83), (-0.2, 0.895), (0.05, 0.868), (0.3, 0.8), (0.5, 0.8), (0.6, 0.74),
                               (0.4, 0.73), (0.1, 0.77), (-0.2, 0.8)]), dark, stroke=""))
    out.append(fold([(-0.3, 0.55), (-0.15, 0.48), (0.05, 0.47)], 0.3))
    out.append(fold([(-0.25, 0.7), (-0.2, 0.55)], 0.25))
    for x in (-0.12, -0.06, 0.0):
        out.append(fold([(x, 0.7), (x + 0.02, 0.52)], 0.2))
    # Neck and the head carried low.
    out.append(shape(smooth_d([(-0.2, 0.88), (-0.45, 0.82), (-0.64, 0.74), (-0.72, 0.62), (-0.64, 0.5),
                               (-0.52, 0.52), (-0.45, 0.58)]), coat))
    out.append(shape(smooth_d([(-0.62, 0.77), (-0.72, 0.78), (-0.82, 0.72), (-0.86, 0.66), (-0.96, 0.62),
                               (-0.985, 0.585), (-0.96, 0.565), (-0.86, 0.565), (-0.76, 0.54), (-0.66, 0.56)]), coat))
    out.append(shape(smooth_d([(-0.9, 0.555), (-0.86, 0.565), (-0.76, 0.54), (-0.7, 0.53), (-0.8, 0.49), (-0.92, 0.49), (-0.95, 0.51)]),
                     "#4A2521"))
    out.append(shape(smooth_d([(-0.72, 0.535), (-0.8, 0.49), (-0.92, 0.47), (-0.95, 0.495), (-0.9, 0.465), (-0.78, 0.465), (-0.7, 0.51)]), coat))
    for x in (-0.94, -0.9, -0.86, -0.82):
        out.append(poly([(x - 0.012, 0.565), (x + 0.012, 0.565), (x, 0.535)], C["bone"], stroke=THIN))
    for x in (-0.91, -0.87, -0.83):
        out.append(poly([(x - 0.012, 0.49), (x + 0.012, 0.49), (x, 0.52)], C["bone"], stroke=THIN))
    out.append(ellipse((-0.975, 0.598), 0.018, 0.014, INK))
    out.append(fold([(-0.9, 0.61), (-0.82, 0.595)], 0.4))
    out.append(fold([(-0.84, 0.705), (-0.8, 0.68)], 0.6, 0.01))
    out.append(ellipse((-0.8, 0.67), 0.014, 0.01, INK, rot=-15))
    out.append(circle((-0.805, 0.673), 0.003, C["bone"], stroke=""))
    # Ears pinned back.
    out.append(poly([(-0.72, 0.77), (-0.6, 0.8), (-0.57, 0.77), (-0.66, 0.74)], dark))
    # Iron-studded collar.
    out.append(poly([(-0.52, 0.83), (-0.575, 0.82), (-0.665, 0.55), (-0.61, 0.535)], C["leather"]))
    for t in (0.15, 0.35, 0.55, 0.75):
        out.append(circle((-0.547 - 0.087 * t, 0.825 - 0.28 * t), 0.012, C["iron"]))
    out.append(ellipse((-0.64, 0.525), 0.018, 0.022, "none", stroke=f'stroke="{C["iron_dk"]}" stroke-width="0.009"'))
    # Near legs over the body.
    out.append(taper((-0.37, 0.62), (-0.5, 0.42), 0.13, 0.085, coat))
    out.append(taper((-0.5, 0.42), (-0.7, 0.22), 0.085, 0.06, coat))
    out.append(ellipse((-0.76, 0.19), 0.055, 0.03, coat, rot=-30))
    for i in range(3):
        out.append(line((-0.8 - i * 0.012, 0.19 + i * 0.012), (-0.815 - i * 0.012, 0.175 + i * 0.012), INK, 0.006))
    out.append(shape(smooth_d([(0.36, 0.72), (0.52, 0.72), (0.58, 0.55), (0.55, 0.42), (0.45, 0.4), (0.38, 0.5)]), coat))
    out.append(taper((0.53, 0.44), (0.72, 0.26), 0.1, 0.065, coat))
    out.append(taper((0.72, 0.26), (0.85, 0.14), 0.065, 0.055, coat))
    out.append(ellipse((0.88, 0.13), 0.05, 0.024, coat, rot=15))
    out.append(fold([(0.4, 0.68), (0.5, 0.55), (0.52, 0.44)], 0.3))
    # Dust kicked up by the grounded hind foot.
    for dx, r_, op in ((0.82, 0.05, 0.25), (0.9, 0.035, 0.2), (0.96, 0.025, 0.15)):
        out.append(circle((dx, 0.05), r_, C["smoke"], stroke="", extra=f'opacity="{op}" filter="url(#softBlur)" data-fx="1"'))
    return "".join(out)


def crypt_risen():
    r = Rig(1.70, 0.86)
    p, w = r.p, r.w
    out = []
    glow_c = C["lapis_glow"]
    lin, lin_dk = C["linen_grave"], C["linen_grave_dk"]
    # Far leg, bent and toed in.
    out.append(taper(p(0.04, 0.52), p(0.07, 0.27), w(0.066), w(0.05), lin, 0.3))
    out.append(taper(p(0.07, 0.27), p(0.04, 0.06), w(0.04), w(0.03), C["bone"], 0.3))
    out.append(foot(p(0.04, 0.05), w(0.1), w(0.04), C["bone"], -1, 0.3))
    # Near leg.
    out.append(taper(p(-0.04, 0.52), p(-0.075, 0.27), w(0.07), w(0.052), lin))
    out.append(taper(p(-0.075, 0.27), p(-0.05, 0.06), w(0.04), w(0.03), C["bone"]))
    out.append(foot(p(-0.05, 0.05), w(0.1), w(0.04), C["bone"], 1))
    for y in (0.46, 0.4, 0.34):
        out.append(line(p(-0.1, y + 0.02), p(-0.02, y - 0.01), lin_dk, w(0.008)))
    # Far arm hanging limp.
    out.append(taper(p(0.085, 0.76), p(0.12, 0.6), w(0.045), w(0.035), lin, 0.3))
    out.append(taper(p(0.12, 0.6), p(0.14, 0.44), w(0.022), w(0.018), C["bone"], 0.3))
    for fx in (-0.012, 0.0, 0.012):
        out.append(line(p(0.14 + fx, 0.44), p(0.145 + fx * 1.4, 0.39), C["bone_dk"], w(0.008)))
    # Torso in torn grave-wrappings, ribs showing through.
    out.append(blob([p(-0.1, 0.78), p(-0.095, 0.62), p(-0.085, 0.5), p(-0.12, 0.38), p(-0.07, 0.35),
                     p(-0.03, 0.4), p(0.02, 0.33), p(0.06, 0.39), p(0.11, 0.36), p(0.09, 0.5), p(0.085, 0.62),
                     p(0.08, 0.78), p(-0.01, 0.8)], lin))
    for y in (0.72, 0.66, 0.54, 0.47):
        out.append(line(p(-0.09, y), p(0.08, y + 0.03), lin_dk, w(0.008), 'opacity="0.8"'))
    out.append(blob([p(-0.06, 0.73), p(-0.07, 0.64), p(-0.02, 0.58), p(0.045, 0.6), p(0.05, 0.7), p(0.0, 0.75)], "#1A1726"))
    out.append(glow(p(-0.005, 0.66), w(0.07), glow_c, 0.75))
    for i, y in enumerate((0.71, 0.685, 0.66, 0.635)):
        out.append(curve([p(-0.055, y), p(-0.02, y + 0.012), p(0.03, y + 0.004 - i * 0.003)], C["bone"], w(0.011)))
    out.append(line(p(-0.012, 0.74), p(-0.01, 0.6), C["bone_dk"], w(0.012)))
    # Ragged strips hanging from the hem.
    for x, length in ((-0.1, 0.1), (-0.05, 0.07), (0.04, 0.12), (0.09, 0.08)):
        out.append(poly([p(x - 0.012, 0.38), p(x + 0.012, 0.38), p(x + 0.006, 0.38 - length), p(x - 0.003, 0.38 - length + 0.02)],
                        lin, 0.08, stroke=THIN))
    # Near arm half-raised, reaching - dragged upright, obedient to nobody.
    out.append(taper(p(-0.09, 0.76), p(-0.15, 0.63), w(0.048), w(0.036), lin))
    out.append(taper(p(-0.15, 0.63), p(-0.23, 0.56), w(0.022), w(0.018), C["bone"]))
    for i, fx in enumerate((-0.012, 0.0, 0.012)):
        out.append(line(p(-0.23, 0.56 + fx), p(-0.27 - 0.005 * i, 0.545 + fx * 1.5), C["bone"], w(0.009)))
    out.append(poly([p(-0.14, 0.66), p(-0.12, 0.64), p(-0.13, 0.56), p(-0.145, 0.58)], lin, stroke=THIN))
    # The misspoken word still burning at the joints.
    for j in (p(-0.15, 0.63), p(0.12, 0.6), p(-0.075, 0.27), p(0.07, 0.27)):
        out.append(glow(j, w(0.03), glow_c, 0.85))
        out.append(circle(j, w(0.009), "#E6E0FF", stroke=""))
    # Skull, head hung forward.
    out.append(line(p(-0.02, 0.8), p(-0.035, 0.84), C["bone_dk"], w(0.028)))
    sk = p(-0.045, 0.885)
    out.append(blob([p(-0.09, 0.89), p(-0.08, 0.95), p(-0.035, 0.975), p(0.01, 0.955), p(0.02, 0.9),
                     p(0.0, 0.85), p(-0.04, 0.83), p(-0.075, 0.845)], C["bone"]))
    out.append(shape(smooth_d([p(-0.03, 0.97), p(0.01, 0.955), p(0.02, 0.9), p(0.0, 0.85), p(-0.02, 0.9)]), "#000", stroke="", extra='opacity="0.18"'))
    for ex in (-0.065, -0.025):
        out.append(ellipse(p(ex, 0.9), w(0.016), w(0.02), "#1A1726"))
        out.append(glow(p(ex, 0.9), w(0.022), glow_c, 0.95))
        out.append(circle(p(ex, 0.9), w(0.006), "#F0ECFF", stroke=""))
    out.append(poly([p(-0.049, 0.88), p(-0.041, 0.88), p(-0.045, 0.865)], "#1A1726", stroke=""))
    out.append(blob([p(-0.07, 0.855), p(-0.02, 0.855), p(-0.025, 0.825), p(-0.045, 0.815), p(-0.065, 0.825)], C["bone_dk"]))
    for tx in (-0.062, -0.054, -0.046, -0.038, -0.03):
        out.append(line(p(tx, 0.855), p(tx, 0.842), INK, w(0.004)))
    out.append(fold([p(-0.08, 0.93), p(-0.06, 0.965)], 0.3))
    # Scrap of burial shroud still over the crown.
    out.append(poly([p(-0.095, 0.9), p(-0.08, 0.96), p(-0.03, 0.98), p(0.015, 0.955), p(0.03, 0.87), p(0.01, 0.94), p(-0.04, 0.955), p(-0.075, 0.93)], lin, stroke=THIN))
    return "".join(out)


def hawk(pose="glide"):
    """One goshawk seen from below, head up the +y axis; wingspan ~1.0 m in the glide pose."""
    out = []
    if pose == "stoop":
        wing = [(-0.04, 0.1), (-0.12, 0.08), (-0.18, -0.05), (-0.21, -0.3), (-0.12, -0.2), (-0.05, -0.08)]
        tips = [[(-0.17, -0.17), (-0.21, -0.3), (-0.135, -0.21)]]
        bars = [((-0.06, 0.05 - 0.05 * k), (-0.15, 0.02 - 0.07 * k)) for k in range(3)]
    else:
        wing = [(-0.04, 0.09), (-0.18, 0.135), (-0.32, 0.125), (-0.43, 0.085), (-0.505, 0.035), (-0.475, 0.012),
                (-0.505, -0.012), (-0.47, -0.032), (-0.49, -0.058), (-0.44, -0.07), (-0.3, -0.1), (-0.16, -0.11),
                (-0.05, -0.085)]
        tips = [[(-0.45, 0.07), (-0.505, 0.035), (-0.46, 0.02)], [(-0.46, 0.0), (-0.505, -0.012), (-0.46, -0.022)],
                [(-0.45, -0.04), (-0.49, -0.058), (-0.44, -0.07), (-0.42, -0.05)]]
        bars = [((-0.07, 0.06 - 0.04 * k), (-0.38 + 0.02 * k, 0.07 - 0.045 * k)) for k in range(4)]
    # Jesses and bells trail from the legs, beside the tail.
    for s_ in (-1, 1):
        out.append(curve([(s_ * 0.03, -0.12), (s_ * 0.07, -0.17), (s_ * 0.08, -0.22), (s_ * 0.11, -0.27)], C["leather"], 0.012))
        out.append(circle((s_ * 0.115, -0.285), 0.016, C["iron"]))
        out.append(line((s_ * 0.115 - 0.01, -0.29), (s_ * 0.115 + 0.01, -0.29), INK, 0.004))
    for side in (-1, 1):
        pts = [(x * -side, y) for x, y in wing]
        out.append(poly(pts, C["hawk_pale"]))
        for a, b in bars:
            out.append(line((a[0] * -side, a[1]), (b[0] * -side, b[1]), C["hawk"], 0.014, 'opacity="0.75"'))
        for tip in tips:
            out.append(poly([(x * -side, y) for x, y in tip], C["hawk_dk"], stroke=THIN))
        out.append(line((pts[0][0], pts[0][1]), (pts[2][0], pts[2][1]), C["hawk_dk"], 0.02))
    # Tail fan with bars, drawn under the body.
    out.append(poly([(-0.045, -0.15), (-0.1, -0.42), (0.1, -0.42), (0.045, -0.15)], C["hawk_pale"]))
    for y in (-0.22, -0.3, -0.38):
        half = 0.045 + (-0.15 - y) * 0.2
        out.append(line((-half, y), (half, y), C["hawk"], 0.018))
    out.append(line((-0.1, -0.415), (0.1, -0.415), C["hawk_dk"], 0.02))
    # Body, barred breast, head with pale brow stripe.
    out.append(ellipse((0, 0), 0.07, 0.19, C["hawk_pale"]))
    for y in (-0.08, -0.03, 0.02, 0.07):
        out.append(curve([(-0.05, y), (0, y - 0.012), (0.05, y)], C["hawk"], 0.01))
    out.append(circle((0, 0.2), 0.058, C["hawk_dk"]))
    for s in (-1, 1):
        out.append(line((s * 0.018, 0.225), (s * 0.05, 0.205), C["hawk_pale"], 0.01))
        out.append(circle((s * 0.03, 0.21), 0.009, INK, stroke=""))
    out.append(poly([(-0.012, 0.25), (0.012, 0.25), (0.0, 0.285)], C["iron_dk"], stroke=THIN))
    return "".join(out)


def falconers_hawks():
    birds = [
        ((-0.35, 1.55), 0.95, -70, "glide"),
        ((0.4, 2.1), 0.85, -100, "glide"),
        ((0.45, 0.8), 0.9, -140, "stoop"),
    ]
    out = []
    for (x, y), s, rot, pose in birds:
        out.append(f'<g transform="translate({n(x)} {n(y)}) rotate({rot}) scale({s})">{hawk(pose)}</g>')
    for x, y in ((-0.35, 1.55), (0.4, 2.1), (0.45, 0.8)):
        out.append(ellipse((x, 0.0), 0.2, 0.02, "#000", stroke="", extra='opacity="0.25" filter="url(#softBlur)" data-fx="1"'))
    return "".join(out)


def treasury_bodyguard():
    r = Rig(2.25, 1.22)
    p, w = r.p, r.w
    out = []
    # Poleaxe planted beside him, butt on the floor.
    out.append(taper(p(-0.2, 0.0), p(-0.185, 0.93), w(0.016), w(0.016), C["oak"]))
    head_y = 0.88
    out.append(poly([p(-0.19, head_y + 0.075), p(-0.18, head_y + 0.075), p(-0.183, head_y + 0.11)], C["steel"]))
    out.append(shape(smooth_d([p(-0.192, head_y + 0.06), p(-0.25, head_y + 0.075), p(-0.29, head_y + 0.06),
                               p(-0.3, head_y), p(-0.28, head_y - 0.04), p(-0.25, head_y - 0.01), p(-0.192, head_y - 0.0)]), C["steel"]))
    out.append(poly([p(-0.178, head_y + 0.035), p(-0.14, head_y + 0.04), p(-0.14, head_y + 0.015), p(-0.178, head_y + 0.02)], C["steel_dk"]))
    for dx in (-0.13, -0.12):
        out.append(poly([p(dx, head_y + 0.045), p(dx + 0.006, head_y + 0.045), p(dx + 0.006, head_y + 0.01), p(dx, head_y + 0.01)], C["steel_dk"], stroke=THIN))
    out.append(line(p(-0.187, head_y - 0.08), p(-0.187, head_y), C["iron_dk"], w(0.022)))
    # Far leg in full harness.
    for a, b, wa, wb in (((0.06, 0.53), (0.09, 0.28), 0.085, 0.064), ((0.09, 0.28), (0.1, 0.055), 0.062, 0.05)):
        out.append(taper(p(*a), p(*b), w(wa), w(wb), C["steel"], 0.3))
    out.append(ellipse(p(0.09, 0.285), w(0.04), w(0.034), C["steel"], extra=''))
    out.append(foot(p(0.1, 0.05), w(0.12), w(0.06), C["steel_dk"], 1, 0.2))
    # Near leg.
    for a, b, wa, wb in (((-0.06, 0.53), (-0.1, 0.28), 0.09, 0.066), ((-0.1, 0.28), (-0.115, 0.055), 0.064, 0.052)):
        out.append(taper(p(*a), p(*b), w(wa), w(wb), C["steel"]))
    out.append(ellipse(p(-0.1, 0.285), w(0.042), w(0.036), C["steel"]))
    out.append(line(p(-0.12, 0.23), p(-0.11, 0.09), C["steel_lt"], w(0.006), 'opacity="0.7"'))
    out.append(foot(p(-0.115, 0.05), w(0.13), w(0.06), C["steel_dk"], -1))
    # Mail skirt, fauld and breastplate.
    out.append(mail_skirt([p(-0.12, 0.6), p(0.12, 0.6), p(0.135, 0.43), p(-0.135, 0.43)]))
    for i, y in enumerate((0.6, 0.565, 0.53)):
        out.append(poly([p(-0.12 - i * 0.004, y), p(0.12 + i * 0.004, y), p(0.123 + i * 0.004, y - 0.04), p(-0.123 - i * 0.004, y - 0.04)], C["steel"]))
    out.append(blob([p(-0.115, 0.81), p(-0.12, 0.7), p(-0.105, 0.6), p(0.105, 0.6), p(0.12, 0.7), p(0.115, 0.81), p(0.0, 0.83)], C["steel"]))
    out.append(line(p(0.0, 0.82), p(0.0, 0.61), C["steel_dk"], w(0.008)))
    out.append(shape(smooth_d([p(-0.09, 0.78), p(-0.095, 0.68), p(-0.06, 0.63), p(-0.07, 0.72)]), C["steel_lt"], stroke="", extra='opacity="0.5"'))
    out.append(shape(smooth_d([p(0.02, 0.82), p(0.115, 0.81), p(0.12, 0.7), p(0.105, 0.6), p(0.02, 0.6)]), "#000", stroke="", extra='opacity="0.2"'))
    # Near arm gripping the poleaxe.
    for side in (-1,):
        out.append(blob([p(-0.05, 0.83), p(-0.14, 0.825), p(-0.165, 0.77), p(-0.14, 0.72), p(-0.1, 0.76)], C["steel"]))
        out.append(taper(p(-0.14, 0.75), p(-0.175, 0.64), w(0.058), w(0.05), C["steel"]))
        out.append(circle(p(-0.175, 0.64), w(0.03), C["steel"]))
        out.append(taper(p(-0.175, 0.64), p(-0.188, 0.56), w(0.05), w(0.044), C["steel"]))
        out.append(blob([p(-0.2, 0.575), p(-0.17, 0.575), p(-0.168, 0.535), p(-0.2, 0.53)], C["steel_dk"]))
    # Great helm, plain - chosen for size, not rank.
    out.append(blob([p(-0.07, 0.83), p(-0.075, 0.86), p(0.075, 0.86), p(0.07, 0.83), p(0.0, 0.82)], "url(#mail)"))
    helm = [p(-0.058, 0.855), p(-0.06, 0.98), p(-0.045, 0.998), p(0.045, 0.998), p(0.06, 0.98), p(0.058, 0.855), p(0.0, 0.848)]
    out.append(poly(helm, C["steel"]))
    out.append(poly([p(-0.052, 0.93), p(0.052, 0.93), p(0.052, 0.921), p(-0.052, 0.921)], INK))
    out.append(line(p(0.0, 0.855), p(0.0, 0.996), C["steel_dk"], w(0.01)))
    # Pavise in the off hand: the silhouette says immovable before it says dangerous.
    pav = [p(0.02, 0.1), p(0.3, 0.12), p(0.305, 0.72), p(0.17, 0.75), p(0.015, 0.72)]
    out.append(poly(pav, "#5E574C"))
    out.append(poly([p(0.145, 0.105), p(0.185, 0.108), p(0.19, 0.745), p(0.15, 0.74)], "#6E665A"))
    out.append(poly([p(0.15, 0.107), p(0.3, 0.12), p(0.305, 0.72), p(0.17, 0.75)], "#000", stroke="", extra='opacity="0.22"'))
    for y in (0.2, 0.62):
        out.append(line(p(0.02, y), p(0.302, y + 0.01), C["iron"], w(0.012)))
    for y in (0.2, 0.62):
        for x in (0.04, 0.12, 0.2, 0.28):
            out.append(circle(p(x, y + 0.003), w(0.006), C["iron_dk"], stroke=""))
    out.append(fold([p(0.05, 0.4), p(0.1, 0.45)], 0.35))
    out.append(fold([p(0.22, 0.3), p(0.26, 0.35)], 0.35))
    out.append(blob([p(0.08, 0.83), p(0.14, 0.825), p(0.17, 0.77), p(0.14, 0.72), p(0.1, 0.76)], C["steel"], 0.25))
    return "".join(out)


def murder_hole_battery():
    out = []
    # Wall section: stone face with the gallery embrasure.
    out.append(poly([(-0.95, 0.0), (0.95, 0.0), (0.95, 1.95), (-0.95, 1.95)], C["stone"], extra='data-fx="1"'))
    rows = [(0.0, 0.3), (0.3, 0.58), (0.58, 0.85), (1.45, 1.7), (1.7, 1.95)]
    for i, (y0, y1) in enumerate(rows):
        x = -0.95 + (0.18 if i % 2 else 0.0)
        while x < 0.95:
            wdt = 0.38 + 0.08 * ((i * 7 + int(x * 10)) % 3)
            x1 = min(x + wdt, 0.95)
            out.append(poly([(max(x, -0.95) + 0.01, y0 + 0.01), (x1 - 0.01, y0 + 0.01), (x1 - 0.01, y1 - 0.01), (max(x, -0.95) + 0.01, y1 - 0.01)],
                            C["stone_lt"] if (i + int(x * 10)) % 3 == 0 else C["stone"], stroke=THIN, extra='data-fx="1"'))
            x = x1
    for y0, y1 in ((0.85, 1.45),):
        for x0, x1 in ((-0.95, -0.62), (0.62, 0.95)):
            out.append(poly([(x0 + 0.01, y0 + 0.01), (x1 - 0.01, y0 + 0.01), (x1 - 0.01, y1 - 0.01), (x0 + 0.01, y1 - 0.01)], C["stone"], stroke=THIN, extra='data-fx="1"'))
    # Narrow arrow-loop in the flanking masonry.
    out.append(poly([(0.76, 0.95), (0.8, 0.95), (0.8, 1.38), (0.76, 1.38)], INK, extra='data-fx="1"'))
    out.append(poly([(0.73, 1.14), (0.83, 1.14), (0.83, 1.17), (0.73, 1.17)], INK, extra='data-fx="1"'))
    # Embrasure: dark gallery behind.
    out.append(poly([(-0.6, 0.86), (0.6, 0.86), (0.6, 1.44), (-0.6, 1.44)], "#0E0C09", extra='data-fx="1"'))
    # Crew in shadow: only arms and shoulders show.
    out.append(blob([(0.2, 0.9), (0.18, 1.12), (0.28, 1.25), (0.5, 1.25), (0.56, 1.1), (0.55, 0.9)], C["gambeson_dk"]))
    for y in (0.95, 1.02, 1.09, 1.16):
        out.append(line((0.22, y), (0.54, y), INK, 0.006, 'opacity="0.5"'))
    out.append(ellipse((0.38, 1.3), 0.13, 0.025, C["iron"]))
    out.append(shape(smooth_d([(0.29, 1.3), (0.3, 1.36), (0.38, 1.38), (0.46, 1.36), (0.47, 1.3)]), C["iron"]))
    out.append(taper((0.24, 1.18), (0.12, 1.1), 0.07, 0.06, C["gambeson"]))
    out.append(taper((0.12, 1.1), (0.05, 1.16), 0.06, 0.05, C["gambeson"]))
    out.append(hand((0.05, 1.165), 0.028, C["skin_dk"]))
    out.append(taper((-0.5, 0.9), (-0.44, 1.05), 0.07, 0.06, C["gambeson_dk"], 0.35))
    out.append(hand((-0.44, 1.06), 0.026, C["skin_dk"], 0.3))
    out.append(line((-0.52, 1.02), (-0.36, 1.09), C["oak_dk"], 0.014))
    out.append(poly([(-0.36, 1.09), (-0.33, 1.1), (-0.345, 1.08)], C["iron"], stroke=THIN))
    out.append(poly([(-0.6, 0.86), (0.6, 0.86), (0.6, 1.44), (-0.6, 1.44)], "#000", stroke="", extra='opacity="0.35" data-fx="1"'))
    # Sill and corbels: a machicolated gallery reading.
    out.append(poly([(-0.66, 0.8), (0.66, 0.8), (0.66, 0.87), (-0.66, 0.87)], C["stone_lt"], extra='data-fx="1"'))
    for x in (-0.5, -0.17, 0.17, 0.5):
        out.append(poly([(x - 0.07, 0.8), (x + 0.07, 0.8), (x + 0.04, 0.68), (x - 0.04, 0.68)], C["stone"], extra='data-fx="1"'))
    out.append(poly([(-0.66, 1.44), (0.66, 1.44), (0.66, 1.5), (-0.66, 1.5)], C["stone_lt"], extra='data-fx="1"'))
    # Pivot post and iron yoke - it yaws, but it is bolted down.
    out.append(poly([(-0.06, 0.87), (0.04, 0.87), (0.03, 1.02), (-0.05, 1.02)], C["oak_dk"]))
    out.append(poly([(-0.08, 1.0), (0.06, 1.0), (0.05, 1.12), (0.03, 1.12), (0.03, 1.03), (-0.05, 1.03), (-0.05, 1.12), (-0.07, 1.12)], C["iron"]))
    out.append(circle((-0.01, 1.1), 0.02, C["iron_dk"]))
    # Stock and windlass.
    out.append(taper((0.3, 1.23), (-0.72, 1.02), 0.075, 0.06, C["oak"]))
    out.append(line((0.25, 1.24), (-0.66, 1.05), C["oak_dk"], 0.012, 'opacity="0.7"'))
    out.append(taper((0.31, 1.3), (0.31, 1.16), 0.05, 0.05, C["oak_dk"]))
    for a in (0, 90):
        rad = math.radians(a + 20)
        out.append(line((0.31 - 0.08 * math.cos(rad), 1.23 - 0.08 * math.sin(rad)), (0.31 + 0.08 * math.cos(rad), 1.23 + 0.08 * math.sin(rad)), C["iron_dk"], 0.014))
    # Bow limbs across the front, foreshortened, pulled back to the nut.
    out.append(curve([(-0.9, 0.86), (-0.66, 1.0), (-0.58, 1.08), (-0.42, 1.28), (-0.26, 1.42)], C["oak_dk"], 0.05))
    out.append(curve([(-0.9, 0.86), (-0.66, 1.0), (-0.58, 1.08), (-0.42, 1.28), (-0.26, 1.42)], C["oak"], 0.03))
    out.append(line((-0.9, 0.86), (-0.08, 1.15), C["bone"], 0.008))
    out.append(line((-0.26, 1.42), (-0.08, 1.15), C["bone"], 0.008))
    out.append(poly([(-0.64, 0.99), (-0.52, 1.05), (-0.55, 1.1), (-0.66, 1.05)], C["iron"]))
    # Bolt on the stock, iron head out through the loop.
    out.append(line((-0.08, 1.15), (-0.86, 0.99), C["oak"], 0.02))
    out.append(poly([(-0.86, 1.005), (-0.86, 0.975), (-0.98, 0.972), ], C["iron"]))
    for dy in (-0.02, 0.02):
        out.append(poly([(-0.08, 1.15), (-0.03, 1.15 + dy), (-0.13, 1.14 + dy)], C["linen_dk"], stroke=THIN))
    return "".join(out)


def castle_chaplain():
    r = Rig(2.5, 0.95)
    p, w = r.p, r.w
    out = []
    # Processional cross staff in the far hand.
    out.append(taper(p(0.16, 0.0), p(0.17, 1.08), w(0.018), w(0.016), C["oak"]))
    cx, cy = 0.17, 1.03
    out.append(poly([p(cx - 0.012, cy - 0.04), p(cx + 0.012, cy - 0.04), p(cx + 0.012, cy + 0.07), p(cx - 0.012, cy + 0.07)], C["iron"]))
    out.append(poly([p(cx - 0.05, cy + 0.02), p(cx + 0.05, cy + 0.02), p(cx + 0.05, cy + 0.035), p(cx - 0.05, cy + 0.035)], C["iron"]))
    for dx, dy in ((0, 0.07), (-0.05, 0.028), (0.05, 0.028)):
        out.append(circle(p(cx + dx, cy + dy), w(0.009), C["iron_dk"]))
    out.append(circle(p(cx, cy + 0.028), w(0.011), "#DDE3E6"))
    # Alb to the ankles with a hint of mail at the hem.
    out.append(blob([p(-0.1, 0.8), p(-0.12, 0.4), p(-0.14, 0.05), p(0.14, 0.05), p(0.12, 0.4), p(0.1, 0.8)], C["linen"]))
    out.append(poly([p(-0.14, 0.07), p(0.14, 0.07), p(0.145, 0.035), p(-0.145, 0.035)], "url(#mail)"))
    out.append(foot(p(-0.06, 0.04), w(0.09), w(0.035), C["leather_dk"], -1))
    out.append(foot(p(0.06, 0.04), w(0.08), w(0.035), C["leather_dk"], 1, 0.2))
    for x in (-0.08, -0.03, 0.03, 0.08):
        out.append(fold([p(x, 0.3), p(x * 1.2, 0.07)], 0.3))
    # Far arm in a wide alb sleeve, holding the cross.
    out.append(taper(p(0.09, 0.78), p(0.14, 0.63), w(0.06), w(0.055), C["linen"], 0.25))
    out.append(taper(p(0.14, 0.63), p(0.16, 0.56), w(0.06), w(0.07), C["linen"], 0.25))
    out.append(poly([p(0.13, 0.56), p(0.19, 0.555), p(0.19, 0.54), p(0.13, 0.545)], "url(#mail)"))
    out.append(hand(p(0.165, 0.535), w(0.022), C["skin_old"], 0.15))
    # Chasuble: heavy undyed-dark wool, bell-shaped, a plain umber orphrey - no gold thread.
    out.append(blob([p(-0.07, 0.82), p(-0.12, 0.78), p(-0.16, 0.55), p(-0.17, 0.33), p(0.0, 0.3),
                     p(0.17, 0.33), p(0.16, 0.55), p(0.12, 0.78), p(0.07, 0.82)], C["vest"]))
    out.append(poly([p(-0.018, 0.8), p(0.018, 0.8), p(0.018, 0.31), p(-0.018, 0.31)], C["vest_band"]))
    out.append(poly([p(-0.09, 0.79), p(-0.07, 0.8), p(0.0, 0.68), p(0.0, 0.64)], C["vest_band"]))
    out.append(poly([p(0.09, 0.79), p(0.07, 0.8), p(0.0, 0.68), p(0.0, 0.64)], C["vest_band"]))
    for x in (-0.12, -0.07, 0.07, 0.12):
        out.append(fold([p(x * 0.7, 0.7), p(x, 0.34)], 0.4))
    out.append(shape(smooth_d([p(0.02, 0.82), p(0.12, 0.78), p(0.16, 0.55), p(0.17, 0.33), p(0.02, 0.3)]), "#000", stroke="", extra='opacity="0.2"'))
    # Sealed reliquary on a cord: oak, iron-bound, rock-crystal window.
    out.append(line(p(0.05, 0.4), p(0.07, 0.34), C["leather_dk"], w(0.005)))
    out.append(poly([p(0.045, 0.34), p(0.1, 0.34), p(0.1, 0.29), p(0.045, 0.29)], C["oak_dk"]))
    out.append(poly([p(0.04, 0.34), p(0.0725, 0.365), p(0.105, 0.34)], C["oak_dk"]))
    out.append(poly([p(0.06, 0.332), p(0.085, 0.332), p(0.085, 0.302), p(0.06, 0.302)], "#DDE3E6"))
    out.append(line(p(0.045, 0.315), p(0.1, 0.315), C["iron"], w(0.005)))
    # Near arm swinging the censer; incense trails upward.
    out.append(taper(p(-0.09, 0.78), p(-0.15, 0.66), w(0.06), w(0.055), C["linen"]))
    out.append(taper(p(-0.15, 0.66), p(-0.16, 0.6), w(0.06), w(0.07), C["linen"]))
    out.append(poly([p(-0.19, 0.6), p(-0.13, 0.6), p(-0.13, 0.585), p(-0.19, 0.585)], "url(#mail)"))
    out.append(hand(p(-0.16, 0.575), w(0.022), C["skin_old"]))
    cen = p(-0.23, 0.41)
    for dx in (-0.018, 0.0, 0.018):
        out.append(line(p(-0.16, 0.57), (cen[0] + w(dx), cen[1] + w(0.03)), C["iron_dk"], w(0.004)))
    out.append(blob([(cen[0] - w(0.035), cen[1] + w(0.02)), (cen[0], cen[1] + w(0.045)), (cen[0] + w(0.035), cen[1] + w(0.02)),
                     (cen[0] + w(0.03), cen[1] - w(0.02)), (cen[0], cen[1] - w(0.035)), (cen[0] - w(0.03), cen[1] - w(0.02))], C["iron"]))
    for dx in (-0.015, 0.0, 0.015):
        out.append(circle((cen[0] + w(dx), cen[1]), w(0.005), "#E8B070", stroke=""))
    smoke = [(cen[0], cen[1] + w(0.05)), (cen[0] - w(0.05), cen[1] + w(0.12)), (cen[0] + w(0.02), cen[1] + w(0.2)),
             (cen[0] - w(0.06), cen[1] + w(0.3)), (cen[0] - w(0.02), cen[1] + w(0.42)), (cen[0] - w(0.09), cen[1] + w(0.52))]
    out.append(curve(smoke, C["smoke"], w(0.03), 'opacity="0.35" filter="url(#softBlur)" data-fx="1"'))
    out.append(curve(smoke, C["smoke"], w(0.01), 'opacity="0.45"'))
    out.append(curve([(cen[0] + w(0.02), cen[1] + w(0.05)), (cen[0] + w(0.07), cen[1] + w(0.15)), (cen[0] + w(0.03), cen[1] + w(0.26)),
                      (cen[0] + w(0.08), cen[1] + w(0.34))], C["smoke"], w(0.008), 'opacity="0.35"'))
    # Amice at the neck; old, tall, stern; tonsured grey head and long beard.
    out.append(blob([p(-0.06, 0.83), p(-0.04, 0.8), p(0.04, 0.8), p(0.06, 0.83), p(0.0, 0.845)], C["linen"]))
    hc = p(0.0, 0.905)
    out.append(face(hc, w(0.038), w(0.052), C["skin_old"], turn=-0.1, beard="long", age=1.0, brow_drop=0.08))
    out.append(shape(smooth_d([p(-0.037, 0.9), p(-0.035, 0.945), p(0.0, 0.96), p(0.035, 0.945), p(0.037, 0.9), p(0.03, 0.935), p(0.0, 0.945), p(-0.03, 0.935)]), C["hair_grey"]))
    out.append(ellipse(p(0.0, 0.952), w(0.022), w(0.009), C["skin_old"]))
    return "".join(out)


def castellan():
    r = Rig(3.4, 1.3)
    p, w = r.p, r.w
    out = []
    gild, gild_dk = C["gild"], C["gild_dk"]

    def gild_edge(a, b):
        return line(a, b, gild, w(0.009)) + line(a, b, gild_dk, w(0.003), 'opacity="0.6"')

    def dent(c, s=1.0):
        return (ellipse(c, w(0.01 * s), w(0.006 * s), "#000", stroke="", extra='opacity="0.35"')
                + curve([(c[0] - w(0.01 * s), c[1] - w(0.004 * s)), (c[0], c[1] - w(0.008 * s)), (c[0] + w(0.01 * s), c[1] - w(0.004 * s))],
                        C["steel_lt"], w(0.003), 'opacity="0.6"'))

    # Heavy wool cloak behind.
    out.append(blob([p(-0.1, 0.82), p(-0.2, 0.7), p(-0.22, 0.35), p(-0.2, 0.08), p(0.2, 0.08), p(0.22, 0.35), p(0.2, 0.7), p(0.1, 0.82)], C["wool_dk"]))
    out.append(fold([p(-0.17, 0.6), p(-0.19, 0.12)], 0.35))
    out.append(fold([p(0.17, 0.6), p(0.19, 0.12)], 0.35))
    # Legs in dented parade harness.
    for sgn, shd in ((1, 0.28), (-1, 0.0)):
        hx, kx, ax = 0.06 * sgn, 0.1 * sgn, 0.12 * sgn
        out.append(taper(p(hx, 0.53), p(kx, 0.28), w(0.09), w(0.066), C["steel"], shd))
        out.append(taper(p(kx, 0.28), p(ax, 0.055), w(0.064), w(0.052), C["steel"], shd))
        out.append(ellipse(p(kx, 0.285), w(0.042), w(0.036), C["steel"]))
        out.append(ellipse(p(kx, 0.285), w(0.03), w(0.025), "none", stroke=f'stroke="{gild}" stroke-width="{n(w(0.008))}"'))
        out.append(foot(p(ax, 0.05), w(0.13), w(0.06), C["steel_dk"], sgn, shd))
        if shd:
            out.append(poly([p(kx - 0.04, 0.3), p(kx + 0.04, 0.3), p(kx + 0.04, 0.05), p(kx - 0.04, 0.05)], "#000", stroke="", extra='opacity="0"'))
    out.append(dent(p(-0.11, 0.18)))
    out.append(dent(p(-0.07, 0.43), 1.2))
    # Mail skirt, fauld, breastplate - the one sanctioned gilding, tarnished.
    out.append(mail_skirt([p(-0.125, 0.6), p(0.125, 0.6), p(0.14, 0.42), p(-0.14, 0.42)]))
    for i, y in enumerate((0.6, 0.565, 0.53)):
        out.append(poly([p(-0.123 - i * 0.004, y), p(0.123 + i * 0.004, y), p(0.126 + i * 0.004, y - 0.04), p(-0.126 - i * 0.004, y - 0.04)], C["steel"]))
        out.append(gild_edge(p(-0.126 - i * 0.004, y - 0.037), p(0.126 + i * 0.004, y - 0.037)))
    out.append(blob([p(-0.12, 0.81), p(-0.125, 0.7), p(-0.11, 0.6), p(0.11, 0.6), p(0.125, 0.7), p(0.12, 0.81), p(0.0, 0.835)], C["steel"]))
    out.append(shape(smooth_d([p(0.02, 0.83), p(0.12, 0.81), p(0.125, 0.7), p(0.11, 0.6), p(0.02, 0.6)]), "#000", stroke="", extra='opacity="0.2"'))
    out.append(gild_edge(p(-0.11, 0.8), p(0.11, 0.8)))
    out.append(curve([p(-0.06, 0.78), p(-0.03, 0.7), p(0.0, 0.66), p(0.03, 0.7), p(0.06, 0.78)], gild, w(0.008)))
    out.append(curve([p(-0.04, 0.74), p(0.0, 0.7), p(0.04, 0.74)], gild_dk, w(0.005)))
    out.append(line(p(0.0, 0.82), p(0.0, 0.61), C["steel_dk"], w(0.008)))
    out.append(dent(p(-0.06, 0.66), 1.5))
    out.append(dent(p(0.05, 0.74)))
    out.append(line(p(-0.09, 0.72), p(-0.04, 0.69), C["steel_lt"], w(0.004)))
    # Kite shield, oversized, on the far arm: the family device in earth, not colour.
    shield = [p(0.03, 0.8), p(0.33, 0.82), p(0.34, 0.6), p(0.28, 0.35), p(0.18, 0.12), p(0.08, 0.35), p(0.02, 0.6)]
    out.append(blob([p(0.08, 0.83), p(0.15, 0.825), p(0.18, 0.77), p(0.15, 0.72), p(0.1, 0.76)], C["steel"], 0.25))
    out.append(poly(shield, "#5A5347"))
    out.append(poly([p(0.03, 0.8), p(0.33, 0.82), p(0.34, 0.6), p(0.28, 0.35), p(0.18, 0.12), p(0.08, 0.35), p(0.02, 0.6)],
                    "none", stroke=f'stroke="{gild}" stroke-width="{n(w(0.014))}" stroke-linejoin="round"'))
    out.append(poly([p(0.06, 0.45), p(0.18, 0.66), p(0.31, 0.47), p(0.3, 0.4), p(0.18, 0.58), p(0.07, 0.38)], C["oak_dk"]))
    out.append(poly(shield[:3] + [p(0.18, 0.12)], "#000", stroke="", extra='opacity="0.12"'))
    for a, b in (((0.08, 0.72), (0.14, 0.62)), ((0.22, 0.3), (0.26, 0.4)), ((0.25, 0.75), (0.3, 0.7))):
        out.append(line(p(*a), p(*b), C["bone_dk"], w(0.004), 'opacity="0.6"'))
    # Near arm and the war-hammer held low across the body.
    out.append(blob([p(-0.05, 0.84), p(-0.15, 0.835), p(-0.18, 0.77), p(-0.15, 0.72), p(-0.1, 0.76)], C["steel"]))
    out.append(gild_edge(p(-0.16, 0.8), p(-0.08, 0.835)))
    out.append(taper(p(-0.15, 0.75), p(-0.19, 0.63), w(0.06), w(0.052), C["steel"]))
    out.append(circle(p(-0.19, 0.63), w(0.032), C["steel"]))
    out.append(taper(p(-0.19, 0.63), p(-0.17, 0.52), w(0.052), w(0.046), C["steel"]))
    out.append(taper(p(-0.131, 0.64), p(-0.3, 0.09), w(0.02), w(0.02), C["oak"]))
    hh = p(-0.3, 0.09)
    out.append(poly([(hh[0] - w(0.06), hh[1] + w(0.04)), (hh[0] + w(0.04), hh[1] + w(0.06)), (hh[0] + w(0.05), hh[1] - w(0.02)), (hh[0] - w(0.05), hh[1] - w(0.04))], C["iron"]))
    out.append(poly([(hh[0] + w(0.04), hh[1] + w(0.04)), (hh[0] + w(0.12), hh[1] + w(0.01)), (hh[0] + w(0.045), hh[1] - w(0.005))], C["iron_dk"]))
    out.append(poly([(hh[0] - w(0.06), hh[1] + w(0.03)), (hh[0] - w(0.09), hh[1] + w(0.035)), (hh[0] - w(0.09), hh[1] - w(0.03)), (hh[0] - w(0.055), hh[1] - w(0.03))], C["iron_dk"]))
    out.append(blob([p(-0.19, 0.535), p(-0.15, 0.535), p(-0.148, 0.495), p(-0.19, 0.49)], C["steel_dk"]))
    # Mail aventail; open bascinet with the visor raised; old, scarred, unbothered.
    out.append(blob([p(-0.08, 0.82), p(-0.085, 0.875), p(0.085, 0.875), p(0.08, 0.82), p(0.0, 0.8)], "url(#mail)"))
    out.append(face(p(0.0, 0.905), w(0.033), w(0.045), C["skin_old"], turn=-0.05, beard="short", age=1.0, scar=True, brow_drop=0.1))
    out.append(shape(smooth_d([p(-0.05, 0.87), p(-0.052, 0.95), p(-0.02, 0.99), p(0.0, 1.0), p(0.02, 0.99), p(0.052, 0.95),
                               p(0.05, 0.87), p(0.04, 0.935), p(0.0, 0.952), p(-0.04, 0.935)]), C["steel"]))
    out.append(curve([p(-0.05, 0.9), p(-0.045, 0.94), p(0.0, 0.955), p(0.045, 0.94), p(0.05, 0.9)], gild, w(0.007)))
    out.append(blob([p(-0.045, 0.95), p(-0.04, 0.985), p(0.0, 1.01), p(0.04, 0.985), p(0.045, 0.95), p(0.0, 0.965)], C["steel_dk"]))
    out.append(circle(p(-0.048, 0.94), w(0.008), gild))
    out.append(circle(p(0.048, 0.94), w(0.008), gild))
    return "".join(out)


def ghost_human(x=0.0):
    """Dashed 1.80 m reference human - the game's measuring stick."""
    r = Rig(1.8)
    p, w = r.p, r.w
    st = f'stroke="{VELLUM_FAINT}" stroke-width="0.01" stroke-dasharray="0.03 0.02"'
    parts = [
        ellipse(p(x / 1.8, 0.93), w(0.04), w(0.055), "none", stroke=st),
        blob([p(x / 1.8 - 0.1, 0.81), p(x / 1.8 - 0.09, 0.53), p(x / 1.8 + 0.09, 0.53), p(x / 1.8 + 0.1, 0.81)], "none", stroke=st),
    ]
    for s in (-1, 1):
        parts.append(taper(p(x / 1.8 + 0.045 * s, 0.53), p(x / 1.8 + 0.05 * s, 0.03), w(0.07), w(0.045), "none", stroke=st))
        parts.append(taper(p(x / 1.8 + 0.105 * s, 0.79), p(x / 1.8 + 0.12 * s, 0.48), w(0.05), w(0.04), "none", stroke=st))
    return "".join(parts)


# ------------------------------------------------------------------ roster text

ROSTER = [
    dict(key="watchman", name="Watchman", tier="Calm", tier_color=VELLUM_DIM, height=1.75, forge=1.80, draw=watchman,
         role="Walks a fixed route carrying the only light in a dark room. Unarmoured and slow to react; you avoid him, you don't fight him.",
         notes=["A household servant on night rounds, not a soldier: rough wool tunic, a hooded shoulder-cape to say civilian.",
                "Horn lantern with a four-post frame, carried low. The tallow flame is the only warm light in the scene.",
                "Oak staff for balance and knocking on doors, not a weapon he'd draw. Leather belt and coin pouch.",
                "Tired and bored, three hours into a shift: heavy lids, stubble, a slight stoop."],
         palette=[(C["wool"], "Undyed wool", "tunic"), (C["cape"], "Grey-brown wool", "hooded cape"),
                  (C["leather"], "Leather", "belt, pouch, shoes"), (C["oak"], "Oak", "staff, lantern posts"),
                  (C["tallow"], "Tallow-orange", "the flame only: the scene's one warm accent")],
         reserved=dict(orpiment="None.", lapis="None.", madder="None: he's the calm before it.", verdigris="None."),
         extent=(-0.5, 0.45)),
    dict(key="falconers-hawks", name="Falconer's Hawks", formerly="SigilWisp", tier="Scout · early warning", tier_color=VELLUM_DIM,
         height=1.0, height_label="~1.0 m wingspan", forge=1.20, forge_label="SigilWisp 1.20 m", draw=falconers_hawks,
         role="Fast, fragile, and in numbers. They spot the party from range and give their position away before a fight starts.",
         notes=["Trained goshawks flown by an unseen household falconer. Falconry is period-real, so no magic is needed.",
                "Leather jesses and small bells still on the legs. The bells are the \"you've been spotted\" cue (flag for audio).",
                "Muted grey-brown plumage with a barred pale underside and a pale brow stripe.",
                "The menace is in behaviour (wheeling, stooping, scattering), shown as glide, bank and stoop."],
         palette=[(C["hawk_pale"], "Barred buff", "underside, tail"), (C["hawk_dk"], "Slate brown", "head, wingtips"),
                  (C["leather"], "Leather", "jesses"), (C["iron"], "Iron", "bells, beak tip")],
         reserved=dict(orpiment="None: the eye is kept dark, not yellow.", lapis="None.", madder="None.", verdigris="None."),
         extent=(-0.9, 0.95)),
    dict(key="man-at-arms", name="Man-at-Arms", tier="Stirred", tier_color="#8E959C", height=1.80, forge=1.85, draw=man_at_arms,
         role="The one guard who comes to check on a noise. Mail-armoured, sword-armed.",
         notes=["The garrison proper: a full mail hauberk under an undyed linen surcoat, split for walking.",
                "Kettle helm with a wide beaten-steel brim over a mail coif.",
                "Arming sword sheathed at the left hip. It is only drawn in combat animation.",
                "Walking pose, head turned toward something he just heard: capable and calm, investigating."],
         palette=[(C["linen"], "Undyed linen", "surcoat"), (C["mail"], "Mail", "hauberk, coif, chausses"),
                  (C["steel"], "Beaten steel", "kettle helm"), (C["leather"], "Leather", "belt, scabbard, gloves")],
         reserved=dict(orpiment="None.", lapis="None.", madder="None: stirred, not yet roused.", verdigris="None."),
         extent=(-0.45, 0.5)),
    dict(key="sergeant", name="Sergeant", tier="Roused", tier_color=MADDER, height=1.85, forge=1.90, draw=sergeant,
         role="Leads the sweep and carries the horn that escalates the raid to the Hue and Cry, the most consequential prop in the bestiary.",
         notes=["Visibly the senior man: coat-of-plates over mail, with steel pauldrons that widen the silhouette.",
                "Flat-topped great helm with a horizontal vision slit over a mail aventail.",
                "Flanged mace: blunt, practical, with no opinion about armour or doors.",
                "The horn on its baldric is bone-white and drawn at weapon weight: a raid lives or dies by it."],
         palette=[(MADDER, "Madder", "tabard: he IS the alarm"), (C["steel"], "Steel", "helm, pauldrons, poleyns"),
                  (C["mail"], "Mail", "under-armour"), (C["bone"], "Bone/horn", "the horn"), (C["oak"], "Oak", "mace haft")],
         reserved=dict(orpiment="None.", lapis="None.", madder="Tabard. The only household member allowed it, because he's the one who sounds the alarm.", verdigris="None."),
         extent=(-0.5, 0.5)),
    dict(key="war-hound", name="War-Hound", tier="Hue & Cry · the chase", tier_color=MADDER, height=0.90, height_label="0.90 m at the shoulder",
         forge=0.85, draw=war_hound,
         role="The payoff for getting caught: fast and relentless, runs the party down once the alarm is loud enough.",
         notes=["A real dog: a big, rough-coated mastiff type, lean and muscular rather than monstrous.",
                "Mid-gallop with ears pinned back, head carried low and teeth bared. The pose sells speed and commitment.",
                "A plain leather collar studded with iron. No armour: it's an animal, not a soldier.",
                "The coat is in the household's undyed wool-brown family, so it belongs to the same estate."],
         palette=[(C["hound"], "Wool-brown coat", "matches the household"), (C["hound_dk"], "Dark saddle", "back and ears"),
                  (C["leather"], "Leather", "collar"), (C["iron"], "Iron", "collar studs, ring")],
         reserved=dict(orpiment="None.", lapis="None.", madder="None on the dog. The tier chip is madder because it arrives with the alarm.", verdigris="None."),
         extent=(-1.0, 1.0)),
    dict(key="treasury-bodyguard", name="Treasury Bodyguard", formerly="VaultWarden", tier="Strongroom · armoured melee", tier_color="#8E959C",
         height=2.25, forge=2.10, draw=treasury_bodyguard,
         role="Slow, heavily armoured. He closes the distance and hits hard: the tanky melee check at valuable doors.",
         notes=["A hand-picked, oversized man-at-arms kept at the treasury door, chosen for size rather than rank.",
                "The heaviest plate the estate owns: breastplate and limb harness over mail, with a plain great helm.",
                "Poleaxe planted upright and a pavise in the off hand. Less decorated than the Sergeant, more armoured than anyone.",
                "A planted, square stance. The silhouette says \"immovable\" before it says \"dangerous\"."],
         palette=[(C["steel"], "Dull steel", "full harness"), (C["mail"], "Mail", "skirt, aventail"),
                  ("#5E574C", "Ash-painted oak", "pavise face"), (C["iron"], "Iron", "pavise bands, poleaxe langets")],
         reserved=dict(orpiment="None: he guards the gold, he doesn't wear it.", lapis="None.", madder="None.", verdigris="None."),
         extent=(-0.75, 0.75)),
    dict(key="murder-hole-battery", name="Murder-Hole Battery", formerly="HexTurret", tier="Static sentry · ranged", tier_color="#8E959C",
         height=1.46, height_label="~1.46 m footprint", forge=1.60, forge_label="HexTurret 1.60 m", draw=murder_hole_battery,
         role="Fixed in place. It yaws to track the party and fires at range: a turret in all but name.",
         notes=["A manned emplacement rather than a creature: a heavy mounted crossbow (a scorpion) bolted into a machicolated gallery.",
                "The hero prop is the weapon's business end: foreshortened bow, iron-headed bolt, windlass, and the iron yoke it pivots on.",
                "The crew stays in shadow. Only a gambeson'd shoulder, arms on the windlass, a kettle-hat brim and a loader's hand show.",
                "It fits the castle generator's existing arrow-loop/murder-hole sockets. Its natural home is Stratum III."],
         palette=[(C["stone"], "Ashlar", "the wall it lives in"), (C["oak"], "Oak", "stock, bow, post"),
                  (C["iron"], "Iron", "yoke, bolt head, bands"), (C["gambeson"], "Undyed gambeson", "crew, in shadow"),
                  (C["bone"], "Sinew/bone", "bowstring")],
         reserved=dict(orpiment="None.", lapis="None.", madder="None.", verdigris="None: it's a machine, not an arcane object."),
         extent=(-1.0, 1.0)),
    dict(key="castle-chaplain", name="Castle Chaplain", formerly="ArcRevenant", tier="Elite · support & ranged", tier_color="#8E959C",
         height=2.5, forge=2.10, draw=castle_chaplain,
         role="The household's one caster-support: wards and blesses nearby guards, and strikes at range with thrown blessed oil.",
         notes=["The estate's own chaplain. The chapel is worth more than everything around it, so its priest is the natural elite.",
                "Linen alb under a heavy, dark wool chasuble with a plain umber orphrey. Mail shows at the cuffs and hem.",
                "The censer trailing smoke keeps VFX continuity with the old caster. He strides, never hovers.",
                "A processional cross, and a sealed reliquary on a cord. Height flag: 2.5 m, read as a very tall old man."],
         palette=[(C["vest"], "Dark undyed wool", "chasuble"), (C["vest_band"], "Umber", "orphrey band (no gold thread)"),
                  (C["linen"], "Linen", "alb, amice"), (C["iron"], "Iron", "censer, cross"),
                  ("#DDE3E6", "Rock crystal", "reliquary window, cross boss")],
         reserved=dict(orpiment="None: the reliquary is iron-bound oak, so it doesn't read as loot.", lapis="None: a blessing isn't a misspoken word.",
                       madder="None.", verdigris="None on him. If the ward VFX needs a colour, use verdigris (arcane/interactive), never lapis."),
         extent=(-0.7, 0.55)),
    dict(key="castellan", name="The Castellan", formerly="GildedColossus", tier="Vault boss", tier_color=MADDER, height=3.4, forge=2.50,
         draw=castellan,
         role="The boss: the single largest, hardest fight in the raid. Fight design is still open (issue #43).",
         notes=["An aging, enormous veteran knight: the lord's own castellan, the household's final line of defence.",
                "Heirloom parade plate chased with tarnished gilding, deliberately duller than orpiment and dented from use.",
                "Asymmetric loadout: a long war-hammer held low and an oversized kite shield. Lopsided without invented anatomy.",
                "Bascinet with the visor up: scarred, grey, unbothered. A neutral ready pose to leave room for #43's telegraph."],
         palette=[(C["steel"], "Old steel", "parade harness"), (C["gild"], "Tarnished gilding", "edge chasing only"),
                  ("#5A5347", "Ash-painted shield", "with an umber chevron device"), (C["wool_dk"], "Dark wool", "cloak"),
                  (C["iron"], "Iron", "hammer head")],
         reserved=dict(orpiment="None pure. The gilding is heirloom plate the household owns, mixed well under orpiment (compare the swatch).",
                       lapis="None.", madder="None.", verdigris="None."),
         extent=(-1.1, 1.2)),
    dict(key="crypt-risen", name="Crypt-Risen", tier="Spell-raised", tier_color=LAPIS, height=1.70, forge=1.75, draw=crypt_risen,
         role="What sleeps under the keep, and what a misfired CADAVER SURGE wakes. The one enemy allowed to be supernatural.",
         notes=["A corpse in torn linen grave-wrappings (burial dress, not a uniform), leaner than any living guard.",
                "Bone shows at the skull, hands, shins and through a tear over the ribs.",
                "Lapis leaks from the sockets, the ribs and every joint: the misspoken word still burning.",
                "Stooped and uneven, one arm limp and one reaching. Pitiable and wrong, obedient to nobody."],
         palette=[(C["linen_grave"], "Grave linen", "wrappings"), (C["bone"], "Bone", "skull, hands, shins"),
                  (C["lapis_glow"], "Lapis glow", "eyes, ribs, joints"), ("#1A1726", "Void", "sockets, chest cavity")],
         reserved=dict(orpiment="None.", lapis="Eyes, ribs and joints. Allowed only here, because a spell raised it.", madder="None.", verdigris="None."),
         extent=(-0.45, 0.4)),
]


# ------------------------------------------------------------------ sheet assembly

SHARED_DEFS = f'''
    <pattern id="mail" width="0.022" height="0.016" patternUnits="userSpaceOnUse">
      <rect width="0.022" height="0.016" fill="{C["mail"]}"/>
      <path d="M0,0.008 a0.0055,0.0055 0 0,0 0.011,0 M0.011,0.016 a0.0055,0.0055 0 0,0 0.011,0 M0.011,0 a0.0055,0.0055 0 0,0 0.011,0"
            stroke="#474B50" stroke-width="0.0022" fill="none"/>
      <circle cx="0.0055" cy="0.0045" r="0.0012" fill="#A9AFB5"/>
      <circle cx="0.0165" cy="0.0125" r="0.0012" fill="#A9AFB5"/>
    </pattern>
    <filter id="glowBlur" x="-2" y="-2" width="5" height="5"><feGaussianBlur stdDeviation="0.02"/></filter>
    <filter id="softBlur" x="-1" y="-1" width="3" height="3"><feGaussianBlur stdDeviation="0.02"/></filter>
    <filter id="silhouette"><feFlood flood-color="{INK}"/><feComposite in2="SourceAlpha" operator="in"/></filter>
    <radialGradient id="stageLight" cx="50%" cy="85%" r="70%">
      <stop offset="0" stop-color="#2B261D"/><stop offset="1" stop-color="{ASH}"/>
    </radialGradient>'''

FONT_DISPLAY = "Georgia, 'Times New Roman', serif"
FONT_MONO = "'DejaVu Sans Mono', Menlo, Consolas, monospace"


def silhouette_of(figure_svg):
    """The figure minus glows, cast light and set-dressing - what a player reads at range."""
    return re.sub(r'<[^<>]*data-fx="1"[^<>]*/>', "", figure_svg)


def esc(text):
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def wrapped(x, y, text, width_chars, size, color, line_h, family=FONT_DISPLAY, style=""):
    lines = textwrap.wrap(text, width_chars)
    spans = "".join(f'<tspan x="{x}" dy="{0 if i == 0 else line_h}">{esc(t)}</tspan>' for i, t in enumerate(lines))
    return (f'<text x="{x}" y="{y}" font-family="{family}" font-size="{size}" fill="{color}" {style}>{spans}</text>',
            y + line_h * len(lines))


def height_label(entry):
    return entry.get("height_label", f'{entry["height"]:.2f} m')


def sheet(entry):
    W, H = 1400, 900
    figure = entry["draw"]()
    stage_x0, stage_x1, ground = 30, 770, 810
    fig_top = entry["height"] if entry["key"] not in ("falconers-hawks",) else 2.45
    if entry["key"] == "murder-hole-battery":
        fig_top = 1.95
    span = max(fig_top, 1.8) * 1.06
    scale = min(690 / span, 680 / (entry["extent"][1] - entry["extent"][0] + 0.9))
    fig_cx = 470
    ghost_x = fig_cx + (entry["extent"][0] - 0.35) * scale
    body = []
    body.append(f'<rect width="{W}" height="{H}" fill="{INK}"/>')
    body.append(f'<rect x="{stage_x0}" y="30" width="{stage_x1 - stage_x0}" height="{H - 60}" fill="url(#stageLight)" stroke="{C["stone_dk"]}"/>')
    body.append(f'<rect x="{stage_x0}" y="{ground}" width="{stage_x1 - stage_x0}" height="{H - 30 - ground}" fill="#17140F"/>')
    body.append(f'<line x1="{stage_x0}" y1="{ground}" x2="{stage_x1}" y2="{ground}" stroke="{VELLUM_FAINT}" stroke-width="1.5"/>')
    # Metre ruler.
    rx = 62
    top_m = span
    body.append(f'<line x1="{rx}" y1="{ground}" x2="{rx}" y2="{ground - top_m * scale:.1f}" stroke="{VELLUM_FAINT}" stroke-width="1"/>')
    m = 0.0
    while m <= top_m + 1e-6:
        y = ground - m * scale
        major = abs(m - round(m)) < 1e-6
        body.append(f'<line x1="{rx - (9 if major else 5)}" y1="{y:.1f}" x2="{rx}" y2="{y:.1f}" stroke="{VELLUM_FAINT}"/>')
        if major or scale > 200:
            body.append(f'<text x="{rx - 12}" y="{y + 4:.1f}" text-anchor="end" font-family="{FONT_MONO}" font-size="11" fill="{VELLUM_FAINT}">{m:.1f}</text>')
        m += 0.25 if scale > 200 else 0.5
    # 1.80 m reference and target height.
    body.append(f'<g transform="translate({ghost_x:.1f} {ground}) scale({scale:.2f} {-scale:.2f})">{ghost_human()}</g>')
    body.append(f'<text x="{ghost_x:.1f}" y="{ground + 22}" text-anchor="middle" font-family="{FONT_MONO}" font-size="11" fill="{VELLUM_FAINT}">1.80 m standard</text>')
    ty = ground - entry["height"] * scale
    if entry["key"] not in ("falconers-hawks", "murder-hole-battery"):
        body.append(f'<line x1="{rx}" y1="{ty:.1f}" x2="{stage_x1 - 20}" y2="{ty:.1f}" stroke="{VELLUM_DIM}" stroke-dasharray="6 5" stroke-width="1"/>')
        body.append(f'<text x="{rx + 8}" y="{ty - 7:.1f}" font-family="{FONT_MONO}" font-size="13" fill="{VELLUM_DIM}">{height_label(entry)}</text>')
    elif entry["key"] == "falconers-hawks":
        y0, x0, x1 = ground - 2.45 * scale, fig_cx + (0.4 - 0.5 * 0.85) * scale, fig_cx + (0.4 + 0.5 * 0.85) * scale
        body.append(f'<text x="{rx + 8}" y="{ground - 2.42 * scale:.1f}" font-family="{FONT_MONO}" font-size="13" fill="{VELLUM_DIM}">{height_label(entry)} · birds drawn at true size</text>')
    body.append(f'<g transform="translate({fig_cx} {ground}) scale({scale:.2f} {-scale:.2f})"><use href="#fig"/></g>')
    if entry["key"] == "murder-hole-battery":
        y0, x0, x1 = ground - 0.6 * scale, fig_cx - 0.98 * scale, fig_cx + 0.48 * scale
        body.append(f'<line x1="{x0:.1f}" y1="{y0:.1f}" x2="{x1:.1f}" y2="{y0:.1f}" stroke="{VELLUM}" stroke-width="2"/>')
        for xx in (x0, x1):
            body.append(f'<line x1="{xx:.1f}" y1="{y0 - 6:.1f}" x2="{xx:.1f}" y2="{y0 + 6:.1f}" stroke="{VELLUM}" stroke-width="2"/>')
        body.append(f'<text x="{(x0 + x1) / 2:.1f}" y="{y0 + 20:.1f}" text-anchor="middle" font-family="{FONT_MONO}" font-size="13" fill="{VELLUM}">{height_label(entry)}</text>')
    # Silhouette inset: the alarm-distance read.
    box_w, box_h = 150, 190
    bx, by = stage_x1 - box_w - 16, 46
    s_scale = min((box_h - 40) / max(fig_top, 1.0), (box_w - 20) / (entry["extent"][1] - entry["extent"][0]))
    body.append(f'<rect x="{bx}" y="{by}" width="{box_w}" height="{box_h}" fill="{VELLUM}" opacity="0.9"/>')
    body.append(f'<g transform="translate({bx + box_w / 2 - (entry["extent"][0] + entry["extent"][1]) / 2 * s_scale:.1f} {by + box_h - 24}) '
                f'scale({s_scale:.2f} {-s_scale:.2f})"><use href="#sil" filter="url(#silhouette)"/></g>')
    body.append(f'<text x="{bx + box_w / 2}" y="{by + box_h - 8}" text-anchor="middle" font-family="{FONT_MONO}" font-size="10" fill="{ASH}">SILHOUETTE READ</text>')
    # Right column: identity, role, notes, palette discipline.
    x = 800
    body.append(f'<text x="{x}" y="64" font-family="{FONT_MONO}" font-size="11" letter-spacing="2" fill="{VELLUM_FAINT}">PLUNDERSPELL BESTIARY · STRATUM II · c.1250</text>')
    body.append(f'<text x="{x}" y="112" font-family="{FONT_DISPLAY}" font-size="42" fill="{VELLUM}">{esc(entry["name"])}</text>')
    y = 138
    if entry.get("formerly"):
        body.append(f'<text x="{x}" y="{y}" font-family="{FONT_DISPLAY}" font-style="italic" font-size="14" fill="{VELLUM_DIM}">formerly {entry["formerly"]}. Placeholder cross-reference only.</text>')
        y += 22
    chip_w = 16 + 8.2 * len(entry["tier"])
    body.append(f'<rect x="{x}" y="{y}" width="{chip_w:.0f}" height="24" rx="3" fill="{entry["tier_color"]}"/>')
    body.append(f'<text x="{x + 8}" y="{y + 17}" font-family="{FONT_MONO}" font-size="12" fill="{INK}">{esc(entry["tier"].upper())}</text>')
    forge_text = entry.get("forge_label", f'{entry["forge"]:.2f} m')
    body.append(f'<text x="{x + chip_w + 14:.0f}" y="{y + 17}" font-family="{FONT_MONO}" font-size="12" fill="{VELLUM_DIM}">brief {esc(height_label(entry))} · scale.md {esc(forge_text)}</text>')
    y += 50
    block, y = wrapped(x, y, entry["role"], 70, 15, VELLUM, 20, style='font-style="italic"')
    body.append(block)
    y += 14
    body.append(f'<text x="{x}" y="{y}" font-family="{FONT_MONO}" font-size="11" letter-spacing="2" fill="{VELLUM_FAINT}">CONCEPT</text>')
    y += 22
    for note in entry["notes"]:
        body.append(f'<circle cx="{x + 4}" cy="{y - 5}" r="2.5" fill="{VELLUM_DIM}"/>')
        block, y = wrapped(x + 16, y, note, 72, 14, VELLUM, 19)
        body.append(block)
        y += 6
    y += 8
    body.append(f'<text x="{x}" y="{y}" font-family="{FONT_MONO}" font-size="11" letter-spacing="2" fill="{VELLUM_FAINT}">MATERIALS</text>')
    y += 14
    for i, (hexv, name, use) in enumerate(entry["palette"]):
        col, row = i % 2, i // 2
        sx, sy = x + col * 290, y + row * 34
        body.append(f'<rect x="{sx}" y="{sy}" width="24" height="24" fill="{hexv}" stroke="{VELLUM_FAINT}"/>')
        body.append(f'<text x="{sx + 32}" y="{sy + 10}" font-family="{FONT_DISPLAY}" font-size="13" fill="{VELLUM}">{esc(name)}</text>')
        body.append(f'<text x="{sx + 32}" y="{sy + 24}" font-family="{FONT_MONO}" font-size="10" fill="{VELLUM_DIM}">{esc(use)}</text>')
    y += 34 * ((len(entry["palette"]) + 1) // 2) + 16
    body.append(f'<text x="{x}" y="{y}" font-family="{FONT_MONO}" font-size="11" letter-spacing="2" fill="{VELLUM_FAINT}">RESERVED PIGMENTS</text>')
    y += 12
    for key, hexv, label in (("orpiment", ORPIMENT, "Orpiment"), ("lapis", LAPIS, "Lapis"), ("madder", MADDER, "Madder"), ("verdigris", VERDIGRIS, "Verdigris")):
        body.append(f'<rect x="{x}" y="{y}" width="14" height="14" fill="{hexv}"/>')
        text = f'{label}: {entry["reserved"][key]}'
        block, y2 = wrapped(x + 22, y + 11, text, 76, 12, VELLUM if entry["reserved"][key] != "None." else VELLUM_DIM, 16, FONT_MONO)
        body.append(block)
        y = y2 + 4
    body.append(f'<text x="{W - 30}" y="{H - 14}" text-anchor="end" font-family="{FONT_MONO}" font-size="10" fill="{VELLUM_FAINT}">'
                f'Hand-authored SVG · Tools/bestiary_concept_svg.py · concept thumbnail for paint-over, not final art</text>')
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" role="img" aria-labelledby="t">'
            f'<title id="t">{esc(entry["name"])}: Plunderspell bestiary concept sheet</title>'
            f'<defs>{SHARED_DEFS}<g id="fig">{figure}</g><g id="sil">{silhouette_of(figure)}</g></defs>'
            + "".join(body) + "</svg>\n")


def lineup():
    scale = 175
    ground = 760
    gap = 0.35
    total = sum(e["extent"][1] - e["extent"][0] + gap for e in ROSTER) + gap
    W = int(total * scale) + 120
    H = 1060
    figures = {e["key"]: e["draw"]() for e in ROSTER}
    defs = "".join(f'<g id="fig-{k}">{v}</g><g id="sil-{k}">{silhouette_of(v)}</g>' for k, v in figures.items())
    body = [f'<rect width="{W}" height="{H}" fill="{INK}"/>',
            f'<rect x="20" y="20" width="{W - 40}" height="{ground - 20}" fill="url(#stageLight)"/>',
            f'<line x1="20" y1="{ground}" x2="{W - 20}" y2="{ground}" stroke="{VELLUM_FAINT}" stroke-width="1.5"/>']
    for m in (1.0, 1.8, 2.5, 3.4):
        y = ground - m * scale
        body.append(f'<line x1="80" y1="{y:.1f}" x2="{W - 20}" y2="{y:.1f}" stroke="{VELLUM_FAINT}" stroke-dasharray="4 8" opacity="0.6"/>')
        body.append(f'<text x="74" y="{y + 4:.1f}" text-anchor="end" font-family="{FONT_MONO}" font-size="12" fill="{VELLUM_FAINT}">{m:.1f} m</text>')
    body.append(f'<text x="100" y="{ground - 3.4 * scale - 30:.0f}" font-family="{FONT_DISPLAY}" font-size="30" fill="{VELLUM}">The household, to scale</text>')
    body.append(f'<text x="100" y="{ground - 3.4 * scale - 8:.0f}" font-family="{FONT_MONO}" font-size="12" fill="{VELLUM_DIM}">'
                f'Stratum II, c.1250. Roughly the order a raid meets them. Heights per the 18 Sept brief; the dashed 1.8 m line is the standard human.</text>')
    x = 100 + gap * scale
    sil_y = H - 60
    for e in ROSTER:
        left, right = e["extent"]
        cx = x - left * scale
        body.append(f'<g transform="translate({cx:.1f} {ground}) scale({scale} {-scale})"><use href="#fig-{e["key"]}"/></g>')
        mid = x + (right - left) * scale / 2
        body.append(f'<text x="{mid:.1f}" y="{ground + 28}" text-anchor="middle" font-family="{FONT_DISPLAY}" font-size="18" fill="{VELLUM}">{esc(e["name"])}</text>')
        body.append(f'<rect x="{mid - 4:.1f}" y="{ground + 40}" width="8" height="8" fill="{e["tier_color"]}"/>')
        body.append(f'<text x="{mid:.1f}" y="{ground + 64}" text-anchor="middle" font-family="{FONT_MONO}" font-size="11" fill="{VELLUM_DIM}">{esc(e["tier"])}</text>')
        body.append(f'<text x="{mid:.1f}" y="{ground + 80}" text-anchor="middle" font-family="{FONT_MONO}" font-size="11" fill="{VELLUM_FAINT}">{esc(height_label(e))}</text>')
        s2 = 40
        body.append(f'<g transform="translate({mid - (left + right) / 2 * s2:.1f} {sil_y}) scale({s2} {-s2})"><use href="#sil-{e["key"]}" filter="url(#silhouette)"/></g>')
        x += (right - left + gap) * scale
    body.append(f'<rect x="20" y="{ground + 96}" width="{W - 40}" height="{H - ground - 116}" fill="{VELLUM}" opacity="0.9"/>')
    body.append(f'<text x="34" y="{ground + 116}" font-family="{FONT_MONO}" font-size="11" fill="{ASH}">SILHOUETTES: THREAT SHOULD READ BEFORE THE WEAPON DOES</text>')
    # Silhouettes must sit on top of the vellum strip, so move them after it.
    sil = [b for b in body if 'filter="url(#silhouette)"' in b]
    rest = [b for b in body if b not in sil]
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {H}" width="{W}" height="{H}" role="img" aria-labelledby="t">'
            f'<title id="t">Plunderspell bestiary lineup</title><defs>{SHARED_DEFS}{defs}</defs>'
            + "".join(rest) + "".join(sil) + "</svg>\n")


if __name__ == "__main__":
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    for i, entry in enumerate(ROSTER, start=1):
        path = OUT_DIR / f'{i:02d}-{entry["key"]}.svg'
        path.write_text(sheet(entry), encoding="utf-8")
        print(f"wrote {path}")
    lineup_path = OUT_DIR / "00-lineup.svg"
    lineup_path.write_text(lineup(), encoding="utf-8")
    print(f"wrote {lineup_path}")
