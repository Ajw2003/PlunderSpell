"""Drawing helpers shared by the Late Medieval room sheets: the Age's palette (the art
bible's, docs/art/late.md) and the furniture it keeps drawing (tables, benches,
iron-bound chests, casks, hearth hoods, tapestries, presses, candle stands).
Section helpers take kit x and heights; plan helpers take kit x, y."""
import math

from roomlib import *

SAND = "#A88B64"      # dressed sandstone: walls, hoods, flagstones
RUBBLE = "#7A6A52"    # rubble core
BRICK = "#8A4B32"     # brick lining, strong rooms, roof tile
OAK = "#6B4F33"       # gates, trusses, shelves, pavises, hafts
IRON = "#2E2F31"      # blackened iron: studs, lattices, grilles, barrels
STEEL = "#8C9096"     # bright plate, a polished barrel
WOOL = "#4F5E3A"      # tapestry wool, counting cloths
ESTATE = "#9E2A2F"    # cloth of estate
LINEN = "#D6CDB6"
SOOT = "#1E1B17"
GOLD = "#C9A227"      # value only
PEWTER = "#9A9A90"
FIRE = "#C4542E"
FLAG = "#5E5040"      # the flagged floors, seen in section


def ashlar_courses(sh, zone, x_sections=((-IN, -1.3), (1.3, IN)), col=SAND, course=0.45):
    """Coursed ashlar joints on the back wall either side of the archway: bed joints
    every `course`, perpends staggered course to course."""
    top = FZ + ZONE_CLEAR[zone] - 0.45
    for x0, x1 in x_sections:
        k = 0
        z = FZ + course
        while z < top - 1e-6:
            sh.line(*KE(x0, z), *KE(x1, z), darken(col, .3), .7, op=.55)
            off = 0.35 if k % 2 else 0.0
            x = x0 + 0.5 + off
            while x < x1 - 0.1:
                sh.line(*KE(x, z - course), *KE(x, z), darken(col, .3), .6, op=.4)
                x += 0.8
            z += course
            k += 1


def table(sh, x, w, h, base=FZ, col=OAK, top=0.07, cloth=None, chequer=False):
    """A trestle table in elevation; `cloth` hangs a cloth over the top (chequered for counting)."""
    for dx in (-w / 2 + .12, w / 2 - .12):
        leg = poly_path([KE(x + dx - .12, base), KE(x + dx + .12, base), KE(x + dx + .03, base + h - top),
                         KE(x + dx - .03, base + h - top)])
        sh.path(leg, darken(col, .15), darken(col, .6), .7)
    kerect(sh, x - w / 2, base + h - top, x + w / 2, base + h, f"url(#{sh.lin(col, 'v', .25, .45)})", darken(col, .6), .8)
    if cloth:
        kerect(sh, x - w / 2 - .03, base + h - .22, x + w / 2 + .03, base + h + .01, cloth, darken(cloth, .5), .7)
        if chequer:
            n = int(w / 0.25)
            for i in range(n):
                if i % 2 == 0:
                    kerect(sh, x - w / 2 + i * .25, base + h - .11, x - w / 2 + (i + 1) * .25, base + h + .01,
                           darken(cloth, .3))
                else:
                    kerect(sh, x - w / 2 + i * .25, base + h - .22, x - w / 2 + (i + 1) * .25, base + h - .11,
                           darken(cloth, .3))


def bench(sh, x0, x1, base=FZ, h=0.45, col=OAK):
    """A plank bench on splayed legs, in elevation."""
    kerect(sh, x0, base + h - .06, x1, base + h, f"url(#{sh.lin(col, 'v', .25, .45)})", darken(col, .6), .7)
    for x in (x0 + .12, x1 - .12):
        sh.line(*KE(x, base + h - .06), *KE(x - .06 if x < (x0 + x1) / 2 else x + .06, base), darken(col, .2), 3)


def chest(sh, x, w, h, base=FZ, col=OAK, straps=IRON, lock=True):
    """An iron-bound oak chest in elevation: strap bands, a lock plate."""
    kerect(sh, x - w / 2, base, x + w / 2, base + h, f"url(#{sh.lin(col, 'v', .25, .5)})", darken(col, .6), .9)
    kerect(sh, x - w / 2, base + h - .06, x + w / 2, base + h, straps)
    for dx in (-w / 2 + .08, -w / 6, w / 6, w / 2 - .12):
        kerect(sh, x + dx, base, x + dx + .04, base + h, straps, op=.9)
    if lock:
        kerect(sh, x - .06, base + h - .22, x + .06, base + h - .08, lighten(straps, .25), "#0E0C09", .5)


def cask_end(sh, x, base, r=0.40, col=OAK, hoops=IRON, cradle=True):
    """A cask lying on its side, seen end-on: the head, hoops, and the cradle under it."""
    cy = base + r + (0.12 if cradle else 0)
    cx, cyp = KE(x, cy)
    sh.circle(cx, cyp, r * SK, f"url(#{sh.lin(col, 'h', .3, .5)})", darken(col, .6), 1)
    sh.circle(cx, cyp, r * SK * .82, "none", hoops, 1.6)
    for k in (-1, 0, 1):
        sh.line(cx - r * SK * .75, cyp + k * r * SK * .35, cx + r * SK * .75, cyp + k * r * SK * .35, darken(col, .3), .6)
    if cradle:
        kerect(sh, x - r * .9, base, x + r * .9, base + 0.14, darken(col, .2), darken(col, .6), .6)


def cask_side(sh, x0, x1, base, r=0.40, col=OAK, hoops=IRON, cradle=True):
    """A cask lying on its side, seen side-on, bulging at the middle, on a cradle."""
    z0 = base + (0.12 if cradle else 0)
    mid = (x0 + x1) / 2
    pts = [KE(x0, z0 + r * .15), KE(mid, z0), KE(x1, z0 + r * .15), KE(x1, z0 + r * 1.85), KE(mid, z0 + 2 * r),
           KE(x0, z0 + r * 1.85)]
    sh.path(smooth_path(pts, tension=.25), f"url(#{sh.lin(col, 'v', .3, .5)})", darken(col, .6), 1)
    for u in (.12, .3, .7, .88):
        xx = x0 + (x1 - x0) * u
        sh.line(*KE(xx, z0 + r * .08), *KE(xx, z0 + r * 1.92), hoops, 1.5)
    if cradle:
        for xx in (x0 + .15, x1 - .15):
            kerect(sh, xx - .06, base, xx + .06, base + .2, darken(col, .2), darken(col, .6), .5)


def hearth_hood(sh, x, w, base=FZ, mouth=1.30, top=None, col=SAND, fire=True):
    """A wall hearth seen face-on: the opening, jambs, lintel and a hood tapering to the flue."""
    top = top or base + 3.2
    kerect(sh, x - w / 2, base, x + w / 2, base + mouth, "#0E0C09")
    for dx in (-w / 2, w / 2 - .18):
        kerect(sh, x + dx, base, x + dx + .18, base + mouth, f"url(#{sh.lin(col, 'h', .3, .5)})", darken(col, .6), .8)
    kerect(sh, x - w / 2 - .1, base + mouth, x + w / 2 + .1, base + mouth + .22, lighten(col, .08), darken(col, .6), .8)
    hood = poly_path([KE(x - w / 2 - .1, base + mouth + .22), KE(x + w / 2 + .1, base + mouth + .22),
                      KE(x + w * .22, top), KE(x - w * .22, top)])
    sh.path(hood, f"url(#{sh.lin(col, 'h', .25, .5)})", darken(col, .6), 1)
    sh.clipped(hood, f'<rect x="{f(KE(x - w, 0)[0])}" y="{f(KE(0, top)[1])}" width="{f(2 * w * SK)}" '
                     f'height="{f((top - base - mouth) * SK * .7)}" fill="{SOOT}" opacity=".35"/>')
    if fire:
        fx, fy = KE(x, base + .05)
        sh.path(f"M{f(fx - 22)} {f(fy)} q6 -34 22 -44 q-2 18 10 26 q6 -12 4 -22 q14 16 8 40 z", FIRE, op=.9)
        kerect(sh, x - .45, base, x + .45, base + .06, IRON)


def tapestry(sh, x0, x1, z0, z1, col=WOOL, border=OAK, rod=IRON):
    """A verdure tapestry on a rod: green ground, a scatter of flowers, a narrow border."""
    kerect(sh, x0, z0, x1, z1, f"url(#{sh.lin(col, 'v', .2, .5)})", darken(col, .6), .8)
    kerect(sh, x0 + .06, z0 + .06, x1 - .06, z1 - .06, "none", lighten(border, .1), .8)
    n = int((x1 - x0) * (z1 - z0) * 5)
    for k in range(n):
        u = ((k * 37) % 97) / 97
        v = ((k * 59) % 89) / 89
        c = (GOLD, ESTATE, LINEN)[k % 3]
        sh.circle(*KE(x0 + .1 + u * (x1 - x0 - .2), z0 + .1 + v * (z1 - z0 - .2)), 1.3, c, op=.8)
    kerect(sh, x0 - .05, z1, x1 + .05, z1 + .04, rod)


def candle_stand(sh, x, base=FZ, h=1.5, col=IRON, lights=1):
    """A standing iron pricket candle stand: tripod foot, stem, drip pan, candle(s)."""
    for dx in (-.18, .18):
        sh.line(*KE(x, base + .2), *KE(x + dx, base), col, 2)
    sh.line(*KE(x, base + .2), *KE(x, base + h), col, 2.4)
    kerect(sh, x - .12, base + h, x + .12, base + h + .03, col)
    for k in range(lights):
        cx = x + (k - (lights - 1) / 2) * .08
        kerect(sh, cx - .015, base + h + .03, cx + .015, base + h + .2, LINEN)
        sh.ellipse(*KE(cx, base + h + .24), 2.2, 4, FIRE)


def press(sh, x0, x1, base=FZ, h=2.2, tiers=4, col=OAK, books=True, seed=3):
    """A tall oak press (book or ledger case) with shelves, seen face-on."""
    kerect(sh, x0, base, x1, base + h, f"url(#{sh.lin(col, 'v', .2, .45)})", darken(col, .6), 1)
    step = (h - .15) / tiers
    for t in range(tiers):
        z = base + .1 + t * step
        kerect(sh, x0 + .06, z, x1 - .06, z + step - .06, darken(col, .45))
        if books:
            xx, k = x0 + .08, seed + t * 7
            while xx < x1 - .14:
                w = .05 + (k * 13 % 5) * .012
                hh = step * (.55 + (k * 7 % 4) * .08)
                bc = (BRICK, OAK, darken(WOOL, .1), darken(LINEN, .35))[k % 4]
                kerect(sh, xx, z, xx + w, z + hh, bc, darken(bc, .5), .3)
                xx += w + .01
                k += 1
        kerect(sh, x0 + .04, z - .04, x1 - .04, z, lighten(col, .1))


def plan_box(sh, x, y, w, d, col, stroke="#0E0C09"):
    """A rectangle centred on kit (x, y) in plan, w along x, d along y."""
    kprect(sh, x - w / 2, y - d / 2, x + w / 2, y + d / 2, col, stroke, .6)


def plan_disc(sh, x, y, r, col, stroke="#0E0C09"):
    sh.circle(*KP(x, y), r * PK, col, stroke, .6)


def plan_cask(sh, x, y, length, r, along_x=True, col=OAK):
    """A cask lying on its side, from above: a rounded rectangle with hoop lines."""
    w, d = (length, 2 * r) if along_x else (2 * r, length)
    a, b = KP(x - w / 2, y + d / 2), KP(x + w / 2, y - d / 2)
    sh.add(f'<rect x="{f(a[0])}" y="{f(a[1])}" width="{f(b[0] - a[0])}" height="{f(b[1] - a[1])}" rx="4" '
           f'fill="{col}" stroke="#0E0C09" stroke-width=".6"/>')
    for u in (.15, .85):
        if along_x:
            xx = x - w / 2 + u * w
            sh.line(*KP(xx, y - d / 2), *KP(xx, y + d / 2), IRON, 1.2)
        else:
            yy = y - d / 2 + u * d
            sh.line(*KP(x - w / 2, yy), *KP(x + w / 2, yy), IRON, 1.2)


def armour(sh, x, base=FZ, col=STEEL, stand=OAK):
    """A harness of plate on its stand, face-on: base, legs, faulds, breastplate,
    pauldrons, arms and a sallet with its tail."""
    kerect(sh, x - 0.3, base, x + 0.3, base + 0.06, stand, darken(stand, .6), .6)
    for dx in (-0.1, 0.1):
        kerect(sh, x + dx - 0.07, base + 0.06, x + dx + 0.07, base + 0.92, f"url(#{sh.lin(col, 'h', .35, .55)})",
               darken(col, .6), .7)
        sh.ellipse(*KE(x + dx, base + 0.5), 5, 4, lighten(col, .2), darken(col, .6), .5)
    kerect(sh, x - 0.2, base + 0.92, x + 0.2, base + 1.1, darken(col, .1), darken(col, .6), .7)
    torso = poly_path([KE(x - 0.2, base + 1.1), KE(x + 0.2, base + 1.1), KE(x + 0.23, base + 1.5),
                       KE(x + 0.18, base + 1.62), KE(x - 0.18, base + 1.62), KE(x - 0.23, base + 1.5)])
    sh.path(torso, f"url(#{sh.lin(col, 'h', .4, .6)})", darken(col, .6), .8)
    sh.line(*KE(x, base + 1.15), *KE(x, base + 1.58), lighten(col, .3), 1)
    for s in (-1, 1):
        sh.ellipse(*KE(x + s * 0.24, base + 1.58), 0.1 * SK, 0.07 * SK, col, darken(col, .6), .7)
        kerect(sh, x + s * 0.28 - 0.05, base + 1.05, x + s * 0.28 + 0.05, base + 1.52, darken(col, .05), darken(col, .6), .6)
    hx, hy = KE(x, base + 1.78)
    sh.path(f"M{f(hx - 7)} {f(hy + 5)} a8 9 0 0 1 16 0 l6 5 l-24 0 z", f"url(#{sh.lin(col, 'h', .4, .6)})",
            darken(col, .6), .8)
    sh.line(hx - 6, hy + 2, hx + 7, hy + 2, "#0E0C09", 1.2)


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
