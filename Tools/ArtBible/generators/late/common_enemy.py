"""Shared defs for enemy sheets: steel/cloth gradients, mail pattern, saltire."""
import math, random
from lib import *


def steel_defs(sh, pre="st", base="#2E2F31"):
    # cylindrical plate: dark edges, a cold rim highlight left of centre
    sh.lin(f"{pre}H", [(0, "#131416"), (.22, "#3A3B3E"), (.38, "#6A6B6B"), (.5, base), (.8, "#1E1F21"), (1, "#0E0F10")])
    sh.lin(f"{pre}V", [(0, "#5A5B5C"), (.25, base), (1, "#111213")], 0, 0, 0, 1)
    sh.rad(f"{pre}R", [(0, "#707172"), (.35, "#3A3B3E"), (1, "#111214")], .38, .3, .75)


def mail_pattern(sh, pid="mail", c1="#6D6E6C", c2="#2A2B2A", size=4):
    s = size
    sh.d(f'<pattern id="{pid}" width="{s}" height="{s*0.8:.1f}" patternUnits="userSpaceOnUse">'
         f'<rect width="{s}" height="{s*0.8:.1f}" fill="{c2}"/>'
         f'<path d="M0 {s*0.4:.1f} Q{s/2:.1f} {-s*0.1:.1f} {s} {s*0.4:.1f}" fill="none" stroke="{c1}" stroke-width="1.1"/>'
         f'<path d="M{-s/2:.1f} {s*0.8:.1f} Q0 {s*0.3:.1f} {s/2:.1f} {s*0.8:.1f} M{s/2:.1f} {s*0.8:.1f} Q{s:.1f} {s*0.3:.1f} {s*1.5:.1f} {s*0.8:.1f}" fill="none" stroke="{c1}" stroke-width="1.1"/>'
         f'</pattern>')


def ragged_bar(v, p0, p1, w, seed, knots=5):
    """Burgundian ragged-staff saltire arm: a knotted branch from p0 to p1, width w (m)."""
    rnd = random.Random(seed)
    (x0, y0), (x1, y1) = p0, p1
    L = math.hypot(x1 - x0, y1 - y0)
    ux, uy = (x1 - x0) / L, (y1 - y0) / L
    nx, ny = -uy, ux
    left, right = [], []
    steps = 14
    for i in range(steps + 1):
        t = i / steps
        cx, cy = x0 + ux * L * t, y0 + uy * L * t
        wl = w / 2 * rnd.uniform(.8, 1.1)
        wr = w / 2 * rnd.uniform(.8, 1.1)
        left.append((cx + nx * wl, cy + ny * wl))
        right.append((cx - nx * wr, cy - ny * wr))
        if 0 < i < steps and i % 3 == 1:  # a lopped knot sticking out
            side = 1 if (i // 3) % 2 else -1
            k = w * .55
            base = left if side > 0 else right
            px, py = cx + nx * side * (w / 2 + k), cy + ny * side * (w / 2 + k)
            base.append((px + ux * w * .25, py + uy * w * .25))
            base.append((cx + nx * side * w / 2 + ux * w * .45, cy + ny * side * w / 2 + uy * w * .45))
    pts = left + right[::-1]
    d = "M " + " L ".join(f"{x:.4f} {y:.4f}" for x, y in pts) + " Z"
    return d


def figure_shadow(sh, cx, w):
    sh.add(f'<ellipse cx="{cx}" cy="692" rx="{w}" ry="7" fill="#0B0A08" opacity=".7"/>')
