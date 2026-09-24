from lib import *

LINEN, WOOL, CLAY, OLIVE, BRZ, EMBER, RED = "#CFC3A2", "#3A2E26", "#A0603E", "#6E5236", "#9B6A38", "#C4542E", "#8E3F2C"
SKIN, HAIR, SOOT = "#9C6E4E", "#2B231B", "#2B231B"


def glow(sh, F, x, h, r, strength=.55):
    gid = sh.rad([(0, EMBER, strength), (0.45, EMBER, strength * .35), (1, EMBER, 0)])
    cx, cy = F.p(x, h)
    sh.circle(cx, cy, r * S, f"url(#{gid})")


def firepot(sh, F, x, top, flip=1):
    """Pot hanging on a cord from (x, top); cord 0.35 m; pot 0.18 dia x 0.20."""
    F.line((x - 0.01, top), (x - 0.012, top - 0.33), "#9C8660" if False else lighten(OLIVE, .35), 1.2)
    F.line((x + 0.01, top), (x + 0.012, top - 0.33), lighten(OLIVE, .35), 1.2)
    c = top - 0.35 - 0.10
    neck = [(x - 0.035, c + 0.10), (x + 0.035, c + 0.10), (x + 0.03, c + 0.075)]
    body = [(x - 0.035, c + 0.105), (x + 0.035, c + 0.105), (x + 0.04, c + 0.075), (x + 0.085, c + 0.03),
            (x + 0.09, c - 0.03), (x + 0.06, c - 0.085), (x, c - 0.10), (x - 0.06, c - 0.085), (x - 0.09, c - 0.03),
            (x - 0.085, c + 0.03), (x - 0.04, c + 0.075)]
    glow(sh, F, x, c + 0.11, 0.09, .5)
    d = F.shape(body, CLAY, light=.3, dark=.55)
    # soot streaks from the neck
    streaks = "".join(
        f'<path d="M{f(F.p(x + dx, c + 0.08)[0])} {f(F.p(x + dx, c + 0.08)[1])} q{f(dx * 20)} 12 {f(dx * 30)} {f(24 + abs(dx) * 200)}" '
        f'stroke="{SOOT}" stroke-width="{f(2 + abs(dx) * 30)}" fill="none" opacity=".38" stroke-linecap="round"/>'
        for dx in (-0.03, -0.005, 0.02, 0.04))
    sh.clipped(d, streaks)
    sh.flecks(d, F.bbox(body), 10, lighten(CLAY, .4), .5, 1.2, .5)
    # bung + wick with ember
    F.shape([(x - 0.03, c + 0.105), (x + 0.03, c + 0.105), (x + 0.025, c + 0.125), (x - 0.025, c + 0.125)], darken(CLAY, .3), smooth=False)
    F.line((x, c + 0.125), (x + 0.008 * flip, c + 0.15), HAIR, 1.4)
    F.dot(x + 0.008 * flip, c + 0.152, 2.6, EMBER)
    F.dot(x + 0.008 * flip, c + 0.152, 1.2, lighten(EMBER, .6))
    return c


def skirt(sh, F, x0, top, halfw, side=False):
    """5 flounces from waist (top) to hem (0.02), flaring."""
    n = 5
    hh = (top - 0.02) / n
    for i in range(n):
        t, b = top - i * hh, top - (i + 1) * hh
        wt = halfw[0] + (halfw[1] - halfw[0]) * (i / n) ** 0.9
        wb = halfw[0] + (halfw[1] - halfw[0]) * ((i + 1) / n) ** 0.9
        base = LINEN if i % 2 == 0 else WOOL
        pts = [(x0 - wt * .92, t), (x0 + wt * .92, t), (x0 + wb, b + 0.01), (x0 + wb * .6, b - 0.012), (x0, b - 0.018),
               (x0 - wb * .6, b - 0.012), (x0 - wb, b + 0.01)]
        d = F.shape(pts, base, light=.3 if base == LINEN else .22, dark=.55, sw=1.2, smooth=False)
        bb = F.bbox(pts)
        # checker border along the lower edge
        cells = []
        y = G - (b + 0.012) * S
        k = 0
        xx = bb[0]
        while xx < bb[2]:
            if k % 2 == 0:
                cells.append(f'<rect x="{f(xx)}" y="{f(y - 2)}" width="4.4" height="4.4" fill="{RED}"/>')
            else:
                cells.append(f'<rect x="{f(xx)}" y="{f(y - 2)}" width="4.4" height="4.4" fill="{lighten(LINEN, .2)}"/>')
            xx += 4.4
            k += 1
        sh.clipped(d, "".join(cells))
        # vertical pleat folds
        for j in range(-3, 4):
            F.line((x0 + j * wt * .25, t - 0.01), (x0 + j * wb * .27, b + 0.02), darken(base, .45), 1, op=.5)
        if base == WOOL:
            sh.flecks(d, bb, 25, lighten(WOOL, .35), .5, 1.2, .5)
        if i == n - 1:
            # scorched bites on the hem
            for _ in range(5):
                bx = sh.rng.uniform(x0 - wb * .9, x0 + wb * .9)
                F.ell(bx, b + 0.005, 0.025, 0.018, SOOT)
                F.dot(bx, b + 0.018, 1.3, EMBER, op=.7)


def keeper_front(sh, cx=470):
    F = Fig(sh, cx)
    # hair behind (ringlets to mid-back, visible at sides)
    F.shape([(-0.09, 1.55), (-0.12, 1.35), (-0.10, 1.22), (0.10, 1.22), (0.12, 1.35), (0.09, 1.55), (0, 1.60)], HAIR, flat=True)
    for s in (-1, 1):
        for k in range(5):
            F.dot(0.105 * s, 1.45 - k * 0.05, 3.5, lighten(HAIR, .15))
    # yoke across the shoulders (behind neck)
    F.limb((-0.66, 1.38), (0.66, 1.38), 0.05, 0.05, OLIVE, direction="v", light=.35)
    for s in (-1, 1):
        F.curve([(0.25 * s, 1.40), (0.10 * s, 1.425)], lighten(OLIVE, .4), 1.5, op=.7)
        F.ell(0.66 * s, 1.38, 0.03, 0.03, darken(OLIVE, .5))   # charred end
    # pots: two per end
    for s in (-1, 1):
        firepot(sh, F, 0.62 * s, 1.37, s)
        firepot(sh, F, 0.48 * s, 1.37, s)
    skirt(sh, F, 0, 1.00, (0.18, 0.48))
    # apron
    ap = [(-0.13, 1.00), (0.13, 1.00), (0.15, 0.62), (0.0, 0.58), (-0.15, 0.62)]
    da = F.shape(ap, LINEN, light=.25, dark=.5)
    sh.clipped(da, f'<rect x="{f(F.p(-0.2, 0)[0])}" y="{f(G - 0.66 * S)}" width="{f(0.4 * S)}" height="{f(0.06 * S)}" fill="{SOOT}" opacity=".55"/>')
    F.curve([(-0.15, 0.62), (0, 0.58), (0.15, 0.62)], RED, 2)
    sh.flecks(da, F.bbox(ap), 40, SOOT, .6, 2.2, .35)   # soot smudges from handling pots
    # arms
    F.limb((-0.19, 1.32), (-0.25, 1.10), 0.075, 0.065, SKIN)
    F.limb((-0.25, 1.10), (-0.30, 0.92), 0.062, 0.05, SKIN)
    F.limb((0.19, 1.32), (0.33, 1.25), 0.075, 0.065, SKIN)          # left arm raised to steady yoke
    F.limb((0.33, 1.25), (0.40, 1.38), 0.062, 0.05, SKIN)
    for k in range(3):
        F.ell(-0.295 + k * 0.004, 0.96 + k * 0.02, 0.035, 0.009, BRZ, light=.5)
        F.ell(0.388 - k * 0.01, 1.335 - k * 0.018, 0.012, 0.03, BRZ, light=.5, rot=-30)
    F.ell(0.405, 1.39, 0.03, 0.035, SKIN, light=.35)
    # bodice
    bo = [(-0.07, 1.43), (-0.19, 1.39), (-0.24, 1.30), (-0.20, 1.24), (-0.15, 1.14), (-0.13, 1.00), (0.13, 1.00),
          (0.15, 1.14), (0.20, 1.24), (0.24, 1.30), (0.19, 1.39), (0.07, 1.43)]
    d = F.shape(bo, LINEN, light=.25, dark=.5)
    F.shape([(-0.07, 1.43), (0.07, 1.43), (0.04, 1.30), (0.0, 1.28), (-0.04, 1.30)], SKIN, light=.3)
    F.shape([(-0.09, 1.26), (0.09, 1.26), (0.085, 1.20), (-0.085, 1.20)], darken(LINEN, .1), light=.3)   # breast band
    for k in range(4):
        F.line((-0.06 + k * 0.04, 1.255), (-0.04 + k * 0.04, 1.205), RED, 1)
    for s in (-1, 1):
        F.curve([(0.24 * s, 1.30), (0.20 * s, 1.24)], RED, 2.6)
    F.shape([(-0.135, 1.00), (0.135, 1.00), (0.137, 1.04), (-0.137, 1.04)], RED, smooth=False)   # sash
    sh.flecks(d, F.bbox(bo), 16, SOOT, .5, 1.3, .35)
    # censer from right hand (viewer left)
    hx, hy = F.p(-0.30, 0.90)
    bx = -0.33
    for dx in (-0.06, 0.0, 0.06):
        F.line((-0.30, 0.90), (bx + dx, 0.36), BRZ, 1, op=.9)
    glow(sh, F, bx, 0.34, 0.14, .5)
    F.shape([(bx - 0.08, 0.36), (bx + 0.08, 0.36), (bx + 0.06, 0.29), (bx, 0.27), (bx - 0.06, 0.29)], BRZ, light=.45)
    F.shape([(bx - 0.075, 0.36), (bx, 0.41), (bx + 0.075, 0.36)], darken(BRZ, .1), light=.45)
    for k in range(4):
        F.dot(bx - 0.045 + k * 0.03, 0.375, 1.8, EMBER)
    F.line((bx, 0.27), (bx, 0.24), BRZ, 3)
    F.ell(-0.30, 0.895, 0.033, 0.042, SKIN, light=.35)
    # smoke wisps
    F.curve([(bx, 0.42), (bx + 0.03, 0.52), (bx - 0.02, 0.62), (bx + 0.02, 0.74)], "#6E665A" if False else lighten(WOOL, .45), 2.5, op=.35)
    # neck, head, polos
    F.limb((0, 1.39), (0, 1.45), 0.075, 0.07, SKIN)
    F.ell(0, 1.505, 0.07, 0.095, SKIN, light=.3)
    for s in (-1, 1):
        F.ell(0.028 * s, 1.515, 0.011, 0.005, "#1E1A16")
        F.curve([(0.045 * s, 1.532), (0.026 * s, 1.54), (0.01 * s, 1.535)], HAIR, 1.4)
    F.curve([(0.0, 1.53), (0.006, 1.49), (-0.004, 1.484)], darken(SKIN, .4), 1)
    F.curve([(-0.016, 1.46), (0, 1.455), (0.016, 1.46)], RED, 1.6)
    F.shape([(-0.075, 1.55), (-0.05, 1.59), (0.05, 1.59), (0.075, 1.55), (0.03, 1.565), (-0.03, 1.565)], HAIR, flat=True)
    F.curve([(-0.03, 1.56), (-0.05, 1.54), (-0.04, 1.525)], HAIR, 2)   # forelock curl
    po = [(-0.09, 1.585), (0.09, 1.585), (0.09, 1.72), (-0.09, 1.72)]
    F.shape(po, LINEN, smooth=False, light=.3)
    F.ell(0, 1.72, 0.09, 0.012, lighten(LINEN, .2))
    F.shape([(-0.091, 1.60), (0.091, 1.60), (0.091, 1.625), (-0.091, 1.625)], RED, smooth=False)


def keeper_side(sh, cx=820):
    F = Fig(sh, cx)
    # far pots (behind), yoke end-on
    firepot(sh, F, -0.06, 1.39, -1)
    F.shape([(-0.10, 1.53), (-0.14, 1.35), (-0.15, 1.12), (-0.08, 1.10), (-0.05, 1.35)], HAIR, flat=True)
    for k in range(7):
        F.dot(-0.12 + 0.005 * k, 1.45 - k * 0.05, 3.4, lighten(HAIR, .15))
    skirt(sh, F, 0.0, 1.00, (0.13, 0.40))
    # apron front edge
    F.shape([(0.05, 1.0), (0.12, 1.0), (0.19, 0.62), (0.08, 0.59)], LINEN, light=.25, smooth=False)
    F.curve([(0.19, 0.62), (0.08, 0.59)], RED, 2)
    # bodice profile
    bo = [(-0.03, 1.43), (-0.11, 1.39), (-0.12, 1.25), (-0.10, 1.10), (-0.09, 1.00), (0.10, 1.00), (0.10, 1.12),
          (0.13, 1.25), (0.11, 1.36), (0.05, 1.43)]
    d = F.shape(bo, LINEN, direction="hr", light=.25, dark=.5)
    F.shape([(-0.095, 1.00), (0.10, 1.00), (0.10, 1.04), (-0.095, 1.04)], RED, smooth=False)
    sh.flecks(d, F.bbox(bo), 10, SOOT, .5, 1.3, .35)
    # yoke end-on across shoulders (a short stub with a notch)
    F.ell(-0.02, 1.40, 0.05, 0.03, OLIVE, light=.35)
    F.ell(-0.02, 1.40, 0.02, 0.014, darken(OLIVE, .5))
    # near pots
    firepot(sh, F, 0.04, 1.39, 1)
    # neck + head
    F.limb((0.0, 1.39), (0.015, 1.45), 0.08, 0.075, SKIN)
    F.ell(0.02, 1.505, 0.085, 0.095, SKIN, light=.3)
    F.shape([(0.10, 1.52), (0.118, 1.49), (0.10, 1.482)], SKIN, smooth=False)
    F.ell(0.07, 1.515, 0.01, 0.005, "#1E1A16")
    F.curve([(0.085, 1.458), (0.098, 1.456)], RED, 1.6)
    F.shape([(-0.07, 1.46), (-0.07, 1.56), (0.0, 1.59), (0.08, 1.58), (0.03, 1.55), (-0.02, 1.50)], HAIR, flat=True)
    po = [(-0.075, 1.585), (0.095, 1.585), (0.095, 1.72), (-0.075, 1.72)]
    F.shape(po, LINEN, smooth=False, light=.3)
    F.shape([(-0.076, 1.60), (0.096, 1.60), (0.096, 1.625), (-0.076, 1.625)], RED, smooth=False)
    # near arm swinging the censer forward
    F.limb((0.0, 1.33), (0.08, 1.12), 0.075, 0.065, SKIN)
    F.limb((0.08, 1.12), (0.24, 1.02), 0.062, 0.05, SKIN)
    for k in range(3):
        F.ell(0.20 + k * 0.012, 1.045 - k * 0.008, 0.01, 0.03, BRZ, light=.5, rot=-55)
    bx, bh = 0.40, 0.58
    for dx in (-0.06, 0.0, 0.06):
        F.line((0.25, 1.01), (bx + dx, bh + 0.02), BRZ, 1, op=.9)
    glow(sh, F, bx, bh, 0.15, .55)
    F.shape([(bx - 0.08, bh + 0.02), (bx + 0.08, bh + 0.02), (bx + 0.06, bh - 0.05), (bx, bh - 0.07), (bx - 0.06, bh - 0.05)], BRZ, light=.45)
    F.shape([(bx - 0.075, bh + 0.02), (bx, bh + 0.07), (bx + 0.075, bh + 0.02)], darken(BRZ, .1), light=.45)
    for k in range(4):
        F.dot(bx - 0.045 + k * 0.03, bh + 0.035, 1.8, EMBER)
    F.curve([(bx, bh + 0.08), (bx - 0.04, bh + 0.2), (bx + 0.01, bh + 0.32), (bx - 0.03, bh + 0.45)], lighten(WOOL, .45), 2.5, op=.35)
    F.ell(0.255, 1.01, 0.033, 0.04, SKIN, light=.35)
    # painted red soles
    F.shape([(-0.02, 0.0), (0.12, 0.0), (0.12, 0.015), (-0.02, 0.015)], RED, smooth=False)


def build():
    sh = Sheet("enemy", "THE BRONZE AGE · ENEMY · SPECIAL", "Keeper of the Flame", "H 1.72 m · ≤ 8k tris · 2048²",
               "1 m = 220 px · ground y 690",
               [("linen", LINEN), ("soot wool", WOOL), ("pot clay", CLAY), ("olive wood", OLIVE), ("bronze", BRZ),
                ("ember", EMBER), ("haematite", RED)], seed=41)
    enemy_furniture(sh)
    height_mark(sh, 1.72, x1=880, label="1.72 m (polos)")
    # crypt archway guide
    sh.extra_frame.append(f'<line x1="66" y1="{G - 2.16 * S:.1f}" x2="880" y2="{G - 2.16 * S:.1f}" stroke="#635C4C" stroke-width=".6" stroke-dasharray="1 5"/>')
    sh.extra_frame.append(f'<text x="214" y="{G - 2.16 * S - 5:.1f}" font-family="{MONO}" font-size="10" fill="#635C4C">Crypt archway 2.16 m</text>')
    for cx in (470, 820):
        sh.ellipse(cx, 690, 120, 8, "#0E0C09", op=.6)
    keeper_front(sh)
    keeper_side(sh)
    sh.callouts([
        (820 + 0.0 * S, G - 1.68 * S, "LINEN POLOS", "0.18 × 0.12 m, red band"),
        (820 - 0.02 * S, G - 1.40 * S, "OLIVE-WOOD YOKE", "1.30 m, across both shoulders"),
        (820 + 0.07 * S, G - 0.92 * S, "FIRE-POTS ×4", "clay 0.18 m, lit wick"),
        (820 + 0.40 * S, G - 0.56 * S, "BRONZE CENSER", "3 chains 0.60 m, embers"),
        (820 + 0.18 * S, G - 0.75 * S, "DOUBLE APRON", "linen, scorched edge"),
        (820 + 0.36 * S, G - 0.25 * S, "FLOUNCED SKIRT", "5 tiers, linen / soot wool"),
        (820 + 0.14 * S, G - 1.24 * S, "LINEN BODICE", "short sleeve, red wave band")], 968, 170, 640)
    return sh
