"""Drawing helpers shared by the Bronze Age room sheets: the Age's palette and
the furniture it keeps drawing (columns, spiral bands, tripods, pithoi, benches,
horns of consecration, larnakes). Section helpers take kit x and heights; plan
helpers take kit x, y. Colours are the art bible's (docs/art/bronze.md)."""
import math

from roomlib import *

PLAST = "#C9A77A"     # painted lime plaster
OCHRE = "#B7803E"     # ochre plaster
MUD = "#8A5A3C"       # mud-brick
BLUE = "#3F6F86"      # fresco blue
RED = "#8E3F2C"       # haematite red
MADDER_ = "#C4542E"   # hearth fire
CYP = "#5A3E28"       # cypress / oak timber
SOOT = "#2B231B"
GYP = "#D8CDB2"       # gypsum
BRONZE = "#9B6A38"
LINEN = "#CFC3A2"
TERRA = "#A0603E"     # pithos terracotta
OILCLAY = "#6A4430"
STONE = "#8C7F68"     # cyclopean limestone
GOLD = "#C9A227"      # value only
FLOOR = "#7A6A58"


def spiral_band(sh, x0, x1, y, h, col, bg=None):
    """Running spiral band between px x0..x1 at top y px, height h px."""
    out = []
    if bg:
        out.append(f'<rect x="{f(x0)}" y="{f(y)}" width="{f(x1 - x0)}" height="{f(h)}" fill="{bg}"/>')
    r = h * .32
    n = max(1, int((x1 - x0) / (h * 1.1)))
    for i in range(n):
        cx = x0 + (i + .5) * (x1 - x0) / n
        cy = y + h / 2
        out.append(f'<path d="M{f(cx - r * 1.6)} {f(cy + r * .9)} C{f(cx - r)} {f(cy + r * 1.2)} {f(cx - r * 1.1)} {f(cy - r)} '
                   f'{f(cx)} {f(cy - r)} A{f(r)} {f(r)} 0 1 1 {f(cx - r * .2)} {f(cy + r * .6)} A{f(r * .5)} {f(r * .5)} 0 1 1 {f(cx + r * .3)} {f(cy)}" '
                   f'fill="none" stroke="{col}" stroke-width="{f(max(1, h * .07))}"/>')
    sh.add("".join(out))


def fresco_band(sh, zone, bottom=1.40, top=3.00, figures=True, x_sections=((-IN, -1.3), (1.3, IN))):
    """The painted band on the back wall either side of the archway, spirals top and bottom."""
    for x0, x1 in x_sections:
        kerect(sh, x0, FZ + bottom, x1, FZ + top, mix(PLAST, "#E0C8A0", .3), darken(PLAST, .5), .8)
        spiral_band(sh, KE(x0, 0)[0], KE(x1, 0)[0], KE(0, FZ + top + .18)[1], 0.18 * SK, BLUE, bg=RED)
        spiral_band(sh, KE(x0, 0)[0], KE(x1, 0)[0], KE(0, FZ + bottom + .22)[1], 0.22 * SK, BLUE, bg=mix(BLUE, PLAST, .5))
    if figures:
        for i, x in enumerate((-4.9, -4.1, -3.3, 3.3, 4.1, 4.9)):
            bx, by = KE(x, FZ + bottom + .22)
            col = RED if i % 2 else BLUE
            sh.path(f"M{f(bx)} {f(by)} l6 -34 l-3 -8 a6 6 0 1 1 8 0 l-3 8 l6 34 z", col, darken(col, .5), .8, op=.9)


def griffin(sh, x, y, s, col, facing=1):
    """Couchant griffin silhouette, lower-left at (x, y) px."""
    pts = [(0, 0), (10, -18), (30, -24), (58, -22), (78, -30), (84, -52), (96, -60), (106, -56), (104, -46), (114, -44),
           (104, -38), (94, -34), (92, -18), (100, 0)]
    sh.path(smooth_path([(x + facing * px * s, y + py * s) for px, py in pts], tension=.35), col, darken(col, .5), 1, op=.9)
    w = [(40, -24), (46, -52), (64, -64), (70, -46), (62, -28)]
    sh.path(smooth_path([(x + facing * px * s, y + py * s) for px, py in w], tension=.35), BLUE, darken(BLUE, .5), 1, op=.9)
    sh.circle(x + facing * 100 * s, y - 52 * s, 1.6 * s + .4, "#14120E")


def column(sh, x, top, base=FZ, shaft=RED, cap=SOOT, hearth_side=None):
    """A down-tapering column (0.34 → 0.46 m) with a cushion capital and abacus, base..top."""
    kerect(sh, x - 0.30, base, x + 0.30, base + 0.10, cap, darken(cap, .5))
    st = top - 0.46
    d = poly_path([KE(x - 0.17, base + 0.10), KE(x + 0.17, base + 0.10), KE(x + 0.23, st), KE(x - 0.23, st)])
    sh.path(d, f"url(#{sh.lin(shaft, 'h', .3, .55)})", darken(shaft, .6), 1.2)
    side = hearth_side if hearth_side is not None else (1 if x < 0 else -1)
    a = KE(x + side * 0.05, base)
    sh.clipped(d, f'<rect x="{f(min(a[0], a[0] + side * 30))}" y="{f(KE(0, st)[1])}" width="30" '
                  f'height="{f((st - base) * SK)}" fill="{SOOT}" opacity=".4"/>')
    sh.flecks(d, (KE(x - .25, 0)[0], KE(0, st)[1], KE(x + .25, 0)[0], KE(0, base + .1)[1]), 24, SOOT, .5, 1.3, .5)
    c = [KE(x - 0.23, st), KE(x - 0.35, st + 0.16), KE(x - 0.30, top - 0.10), KE(x + 0.30, top - 0.10),
         KE(x + 0.35, st + 0.16), KE(x + 0.23, st)]
    sh.path(smooth_path(c, tension=.35), f"url(#{sh.lin(cap, 'h', .35, .5)})", "#0E0C09", 1)
    kerect(sh, x - 0.35, top - 0.10, x + 0.35, top, lighten(cap, .15), "#0E0C09")


def tripod(sh, x, base=FZ, legs=0.90, r=0.30):
    """A bronze tripod cauldron in elevation."""
    bx, by = KE(x, base + legs + 0.1)
    w = r * SK
    sh.path(f"M{f(bx - w)} {f(by)} a{f(w)} {f(w * .55)} 0 0 0 {f(2 * w)} 0 z", f"url(#{sh.lin(BRONZE, 'h', .35, .5)})",
            darken(BRONZE, .6), 1)
    sh.line(bx - w, by, bx + w, by, lighten(BRONZE, .3), 1.4)
    for dx in (-.7, 0, .7):
        sh.line(bx + dx * w * .6, by + 4, bx + dx * w * 1.2, KE(0, base)[1], BRONZE, 1.8)


def jar(sh, x, base, height, belly, col, rim=None, bands=(), lid=None, lugs=True):
    """A turned jar in elevation (pithos, amphora, hydria), standing on `base`."""
    r = belly / 2
    m = (rim if rim is not None else belly * 0.45) / 2
    prof = [(0, r * .45), (height * .2, r * .85), (height * .45, r), (height * .75, r * .8), (height * .92, m),
            (height * .97, m * 1.15), (height, m * 1.1)]
    left = [KE(x - rr, base + z) for z, rr in prof]
    right = [KE(x + rr, base + z) for z, rr in reversed(prof)]
    d = smooth_path(left + right, tension=.3)
    sh.path(d, f"url(#{sh.lin(col, 'h', .35, .55)})", darken(col, .6), 1.1)
    for z in bands:
        rr = r * (1 - abs(z / height - .45) * .7)
        sh.line(*KE(x - rr * .96, base + z), *KE(x + rr * .96, base + z), darken(col, .35), 1.6, op=.8)
    if lugs:
        for s in (-1, 1):
            a = KE(x + s * r * .78, base + height * .72)
            sh.path(f"M{f(a[0])} {f(a[1])} q{f(s * 7)} 4 0 12", "none", darken(col, .4), 2)
    if lid:
        kerect(sh, x - m * 1.25, base + height, x + m * 1.25, base + height + 0.06, lid, darken(lid, .5), .8)
    return d


def bench(sh, x0, x1, base=FZ, h=0.40, col=GYP, fleece=True):
    """A plastered clay bench in elevation, with a fleece on it."""
    kerect(sh, x0, base, x1, base + h, f"url(#{sh.lin(col, 'v', .25, .45)})", darken(col, .6))
    if fleece:
        w = (x1 - x0)
        kerect(sh, x0 + w * .2, base + h, x0 + w * .55, base + h + .08, LINEN, darken(LINEN, .5), .6)


def table(sh, x, w, h, base=FZ, col=FLOOR, top=0.08):
    """A table on legs in elevation."""
    kerect(sh, x - w / 2, base + h - top, x + w / 2, base + h, f"url(#{sh.lin(col, 'v', .25, .45)})", darken(col, .6), .8)
    for dx in (-w / 2 + .05, w / 2 - .1):
        kerect(sh, x + dx, base, x + dx + .05, base + h - top, darken(col, .2), darken(col, .6), .5)


def chest(sh, x, w, h, base=FZ, col=CYP, trim=BRONZE):
    """A timber chest with a bronze-bound lid in elevation."""
    kerect(sh, x - w / 2, base, x + w / 2, base + h, f"url(#{sh.lin(col, 'v', .25, .5)})", darken(col, .6), .8)
    kerect(sh, x - w / 2, base + h - .05, x + w / 2, base + h + .03, trim, darken(trim, .5), .6)
    for dx in (-w / 3, w / 3):
        kerect(sh, x + dx - .02, base, x + dx + .02, base + h, trim, op=.8)


def horns(sh, x, base, size=0.8, col=GYP):
    """Horns of consecration in elevation: a plinth with two up-curving horns."""
    w = size
    kerect(sh, x - w / 2, base, x + w / 2, base + w * .25, col, darken(col, .5), .8)
    for s in (-1, 1):
        a = KE(x + s * w * .3, base + w * .25)
        tip = KE(x + s * w * .5, base + w * .85)
        sh.path(f"M{f(a[0] - s * 5)} {f(a[1])} Q{f(a[0] + s * 2)} {f((a[1] + tip[1]) / 2)} {f(tip[0])} {f(tip[1])} "
                f"Q{f(a[0] + s * 8)} {f((a[1] + tip[1]) / 2 + 6)} {f(a[0] + s * 7)} {f(a[1])} Z",
                f"url(#{sh.lin(col, 'h', .3, .5)})", darken(col, .5), .8)


def larnax(sh, x, w, base=FZ, col=TERRA, h=0.55, legs=0.12):
    """A painted clay chest-coffin on four short legs with a gabled lid, in elevation."""
    for dx in (-w / 2 + .04, w / 2 - .14):
        kerect(sh, x + dx, base, x + dx + .1, base + legs, darken(col, .2), darken(col, .6), .5)
    kerect(sh, x - w / 2, base + legs, x + w / 2, base + legs + h, f"url(#{sh.lin(col, 'v', .2, .5)})", darken(col, .6), .9)
    # painted bands: waves and an octopus-ish swirl
    for k in range(3):
        z = base + legs + h * (.25 + k * .25)
        sh.line(*KE(x - w / 2 + .05, z), *KE(x + w / 2 - .05, z), darken(col, .45), 1, op=.7)
    lid = [KE(x - w / 2 - .03, base + legs + h), KE(x, base + legs + h + .22), KE(x + w / 2 + .03, base + legs + h)]
    sh.path(poly_path(lid), f"url(#{sh.lin(col, 'v', .2, .5)})", darken(col, .6), .9)


def plan_jar(sh, x, y, belly, col, lid=None):
    """A jar seen from above: belly circle and mouth."""
    sh.circle(*KP(x, y), belly / 2 * PK, col, darken(col, .5), .7)
    sh.circle(*KP(x, y), belly * .22 * PK, lid or darken(col, .45))


def plan_box(sh, x, y, w, d, col, stroke="#0E0C09"):
    """A rectangle centred on kit (x, y) in plan, w along x, d along y."""
    kprect(sh, x - w / 2, y - d / 2, x + w / 2, y + d / 2, col, stroke, .6)
