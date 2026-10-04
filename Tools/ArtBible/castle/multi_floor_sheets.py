"""
Concept sheets for the multi-floor castle (#197 follow-up, design draft 2026-10-03).

    python Tools/ArtBible/castle/multi_floor_sheets.py            # write both SVGs and render PNGs
    python Tools/ArtBible/castle/multi_floor_sheets.py --no-png   # SVGs only

Writes docs/art/castle/concept/multi-floor-layout.{svg,png} (the three floor plans and a
three-quarter view of the stack) and multi-floor-sections.{svg,png} (a section through the whole
castle, and the two stairwell cells at a builder's scale). Same frame, fonts and palette strip as
the room sheets (Tools/ArtBible/rooms/roomlib.py). The layout numbers below are the design's, not
read from the generator, which does not build floors yet.

PNG rendering uses Python Playwright's Chromium (2400 x 1600, fonts from Google Fonts), the same
way Tools/ArtBible/render_png.cjs renders the room sheets.
"""
import math
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
sys.path.insert(0, os.path.join(REPO, "Tools", "ArtBible", "rooms"))
from roomlib import Sheet, MONO, SERIF, f, darken, lighten  # noqa: E402

OUT = os.path.join(REPO, "docs", "art", "castle", "concept")

# ---- the design (metres; heights above ground, the ground slab's top at 0.30) ----
CELL = 12.0
FZ = 0.30
CLEAR = {"OuterBailey": 3.6, "InnerWard": 4.0, "Keep": 4.6, "Crypt": 3.0, "CurtainWall": 5.2}
KEEP_FLOOR = FZ + CLEAR["InnerWard"] + FZ      # 4.60: the keep slab sits on the ward walls' top (4.30)
KEEP_TOP = KEEP_FLOOR + CLEAR["Keep"]          # 9.20
CRYPT_FLOOR = -CLEAR["Crypt"]                  # -3.00: crypt clear height under the ground slab
CRYPT_SLAB = CRYPT_FLOOR - FZ                  # -3.30
UP_RISE = KEEP_FLOOR - FZ                      # 4.30
DOWN_RISE = FZ - CRYPT_FLOOR                   # 3.30
RISER = 0.18
UP_RISERS = round(UP_RISE / 0.187)             # 23: the newel sheet's ~0.188 m riser over a ward storey
DOWN_RISERS = round(DOWN_RISE / 0.187)         # 18

UP_STAIRS = [(-1, 0), (1, 0)]                  # ground ward cells whose stair climbs into the keep
DOWN_STAIR = (0, 0)                            # ground centre: stair down to the crypt
FINAL = (0, 1)                                 # one example: the generator picks one of the 8 by seed
COURTYARDS = [(-2, 2), (2, -2)]                # examples: carved only from the bailey ring, by seed
GATE = (3, 0)                                  # the gatehouse, east (generator's k_GateOutward)

COL = {
    "CurtainWall": "#4A463C", "OuterBailey": "#8C6A44", "InnerWard": "#A88B64",
    "Keep": "#B9AE96", "Crypt": "#4A4A52", "Courtyard": "#2F3A26",
}
STAIR = "#C4542E"
DOOR = "#6B4F33"
INK = "#DCD2BA"
DIM = "#9A9078"
FAINT = "#635C4C"
GROUND_BG = "#1B1813"

MATERIALS = [("bailey", COL["OuterBailey"]), ("inner ward", COL["InnerWard"]), ("keep", COL["Keep"]),
             ("crypt", COL["Crypt"]), ("curtain", "#4A463C"), ("stair", STAIR), ("door", DOOR)]


def ring(x, y):
    return max(abs(x), abs(y))


def ground_zone(x, y):
    r = ring(x, y)
    if r == 3:
        return "CurtainWall"
    if (x, y) in COURTYARDS:
        return "Courtyard"
    return "OuterBailey" if r == 2 else "InnerWard"


def doors_ground():
    """Every archway between a ward cell and a bailey room (not a courtyard): (cell, side) pairs."""
    out = []
    for x in range(-1, 2):
        for y in range(-1, 2):
            if ring(x, y) != 1:
                continue
            for dx, dy, side in ((1, 0, "E"), (-1, 0, "W"), (0, 1, "N"), (0, -1, "S")):
                n = (x + dx, y + dy)
                if ring(*n) == 2 and ground_zone(*n) == "OuterBailey":
                    out.append(((x, y), side))
    return out


# ---------------------------------------------------------------- plans

def plan(sh, ox, oy, px, cells, title, sub):
    """cells: {(x,y): zone}. ox, oy: screen centre of cell (0,0). px: pixels per cell."""
    gap = 3
    for (x, y), zone in cells.items():
        cx, cy = ox + x * px, oy - y * px
        fill = COL[zone]
        sh.add(f'<rect x="{f(cx - px / 2 + gap / 2)}" y="{f(cy - px / 2 + gap / 2)}" width="{f(px - gap)}" '
               f'height="{f(px - gap)}" fill="{fill}" stroke="{darken(fill, .5)}" stroke-width=".8"/>')
        if zone == "Courtyard":
            sh.add(f'<path d="M{f(cx - px / 2 + 4)} {f(cy + px / 2 - 4)} L{f(cx + px / 2 - 4)} {f(cy - px / 2 + 4)}" '
                   f'stroke="#4F5E3A" stroke-width="1"/>')
    n = max(max(abs(x), abs(y)) for x, y in cells)
    half = (n + .5) * px
    sh.extra_frame.append(f'<text x="{f(ox)}" y="{f(oy - half - 26)}" text-anchor="middle" font-family="{MONO}" '
                          f'font-size="11" letter-spacing="3" fill="{FAINT}">{title}</text>')
    sh.extra_frame.append(f'<text x="{f(ox)}" y="{f(oy - half - 12)}" text-anchor="middle" font-family="{MONO}" '
                          f'font-size="9.5" fill="{DIM}">{sub}</text>')


def stair_mark(sh, cx, cy, px, label, down=False):
    """Ember frame, treads across the middle, an arrow in the top third, the label along the bottom edge."""
    s = px * .26
    sh.add(f'<rect x="{f(cx - px / 2 + 3)}" y="{f(cy - px / 2 + 3)}" width="{f(px - 6)}" height="{f(px - 6)}" '
           f'fill="none" stroke="{STAIR}" stroke-width="2"/>')
    for i in range(3):                                       # treads
        y = cy - s * .35 + i * s * .5
        sh.line(cx - s, y, cx + s, y, STAIR, 1.2, op=.8)
    ay = cy - px * .3
    head = 4 if down else -4
    sh.path(f"M{f(cx - 5)} {f(ay - head)} L{f(cx)} {f(ay + head)} L{f(cx + 5)} {f(ay - head)}", "none", INK, 1.4)
    sh.text(cx, cy + px / 2 - 5, label, 7.5, INK, "middle", ls="1")


def door_mark(sh, cx, cy, px, side):
    half = px / 2
    w, t = px * .32, 4
    if side in ("N", "S"):
        y = cy - half if side == "N" else cy + half
        sh.add(f'<rect x="{f(cx - w / 2)}" y="{f(y - t / 2)}" width="{f(w)}" height="{t}" fill="{DOOR}" stroke="{INK}" stroke-width=".6"/>')
    else:
        x = cx + half if side == "E" else cx - half
        sh.add(f'<rect x="{f(x - t / 2)}" y="{f(cy - w / 2)}" width="{t}" height="{f(w)}" fill="{DOOR}" stroke="{INK}" stroke-width=".6"/>')


def sealed_mark(sh, cx, cy, px, side):
    """A sealed face drawn as a heavy wall line (keep and crypt outer faces, stair-top side walls)."""
    h = px / 2 - 1.5
    a = {"N": (cx - h, cy - h, cx + h, cy - h), "S": (cx - h, cy + h, cx + h, cy + h),
         "E": (cx + h, cy - h, cx + h, cy + h), "W": (cx - h, cy - h, cx - h, cy + h)}[side]
    sh.line(*a, "#14120E", 3)


# ---------------------------------------------------------------- three-quarter view

class Iso:
    def __init__(self, cx, cy, k, zx=2.0):
        self.cx, self.cy, self.k, self.zx = cx, cy, k, zx      # zx: height exaggeration

    def p(self, X, Y, Z):
        # Seen from the south-east: X - Y is depth toward the viewer, so it moves a point down the
        # screen; X + Y runs across.
        return (self.cx + (X + Y) * .866 * self.k, self.cy + (X - Y) * .5 * self.k - Z * self.k * self.zx)


def iso_box(sh, iso, x, y, z0, z1, fill, op=1.0, dashed=False):
    """A cell-sized box, seen from the south-east: draws the top, the south (-Y) and east (+X) faces."""
    X0, X1 = x * CELL - CELL / 2 + .4, x * CELL + CELL / 2 - .4
    Y0, Y1 = y * CELL - CELL / 2 + .4, y * CELL + CELL / 2 - .4

    def poly(pts, col):
        d = "M" + " L".join(f"{f(a)} {f(b)}" for a, b in pts) + " Z"
        extra = ' stroke-dasharray="3 2"' if dashed else ""
        sh.add(f'<path d="{d}" fill="{col}" stroke="{darken(fill, .55)}" stroke-width=".7" opacity="{op}"{extra}/>')
    P = iso.p
    poly([P(X0, Y0, z1), P(X1, Y0, z1), P(X1, Y1, z1), P(X0, Y1, z1)], lighten(fill, .12))   # top
    poly([P(X0, Y0, z0), P(X1, Y0, z0), P(X1, Y0, z1), P(X0, Y0, z1)], darken(fill, .18))   # south face
    poly([P(X1, Y0, z0), P(X1, Y1, z0), P(X1, Y1, z1), P(X1, Y0, z1)], darken(fill, .35))   # east face


def three_quarter(sh, cx, cy_keep, cy_ground, cy_crypt, k, zx):
    """Exploded three-quarter view: the keep, ground and crypt drawn as three layers stacked down the
    sheet. They share one grid, so a cell sits at the same screen x in every layer and the stair lines
    run straight down between them. Heights within a layer are measured from that layer's own floor."""
    keep, ground, crypt = Iso(cx, cy_keep, k, zx), Iso(cx, cy_ground, k, zx), Iso(cx, cy_crypt, k, zx)
    order = lambda cells: sorted(cells, key=lambda c: c[0] - c[1])   # noqa: E731  back (low X - Y) first

    for x, y in order([(x, y) for x in range(-1, 2) for y in range(-1, 2)]):
        iso_box(sh, crypt, x, y, 0, FZ + CLEAR["Crypt"], COL["Crypt"])
    for x, y in order([(x, y) for x in range(-3, 4) for y in range(-3, 4)]):
        z = ground_zone(x, y)
        top = .3 if z == "Courtyard" else FZ + CLEAR[z]
        iso_box(sh, ground, x, y, 0, top, COL[z])
    for x, y in order([(x, y) for x in range(-1, 2) for y in range(-1, 2)]):
        iso_box(sh, keep, x, y, 0, FZ + CLEAR["Keep"], COL["Keep"])

    # guide lines between the layers at the inner block's corners
    for gx_, gy_ in ((-1.5, -1.5), (1.5, -1.5), (1.5, 1.5), (-1.5, 1.5)):
        X, Y = gx_ * CELL, gy_ * CELL
        sh.line(*ground.p(X, Y, FZ + CLEAR["InnerWard"]), *keep.p(X, Y, 0), FAINT, .8, dash="2 4", layer=sh.back)
        sh.line(*crypt.p(X, Y, FZ + CLEAR["Crypt"]), *ground.p(X, Y, 0), FAINT, .8, dash="2 4", layer=sh.back)
    # stairs: ward stair cell top -> keep stair-top cell; ground centre -> crypt foot
    for (x, y) in UP_STAIRS:
        a, b = ground.p(x * CELL, y * CELL, FZ + CLEAR["InnerWard"]), keep.p(x * CELL, y * CELL, FZ + CLEAR["Keep"])
        sh.line(*a, *b, STAIR, 2.2, dash="5 3")
        sh.circle(*a, 3, STAIR)
        sh.circle(*b, 3, STAIR)
    a, b = ground.p(0, 0, FZ + CLEAR["InnerWard"]), crypt.p(0, 0, FZ + CLEAR["Crypt"])
    sh.line(*a, *b, STAIR, 2.2, dash="5 3")
    sh.circle(*a, 3, STAIR)
    sh.circle(*b, 3, STAIR)
    left = cx - 4.8 * CELL * 2 * .866 * k
    for cy, text, sub in ((cy_keep, "KEEP", f"+{KEEP_FLOOR:.2f} m"), (cy_ground, "GROUND", "0.00 m"),
                          (cy_crypt, "CRYPT", f"{CRYPT_FLOOR:+.2f} m")):
        sh.text(left, cy - 4, text, 9.5, INK, "start", ls="2")
        sh.text(left, cy + 10, sub, 8.5, DIM)


# ---------------------------------------------------------------- sheet 1

def layout_sheet():
    sh = Sheet("structure", "PLUNDERSPELL · CASTLE LAYOUT · DESIGN DRAFT 2026-10-03", "The Stacked Castle",
               "3 floors · 5 × 5 ground · keep and crypt 3 × 3",
               "plans 1 cell = 12 m · three-quarter exploded, heights × 2", MATERIALS, seed=11)
    # --- plans: ground (left), keep (top right of plans), crypt (bottom right of plans)
    gpx = 34
    gx, gy = 190, 420
    ground = {(x, y): ground_zone(x, y) for x in range(-3, 4) for y in range(-3, 4)}
    plan(sh, gx, gy, gpx, ground, "GROUND · LEVEL 0", "bailey ring, inner ward ring, stairs")
    for (x, y) in UP_STAIRS:
        stair_mark(sh, gx + x * gpx, gy - y * gpx, gpx, "UP")
    stair_mark(sh, gx + DOWN_STAIR[0] * gpx, gy - DOWN_STAIR[1] * gpx, gpx, "DN", down=True)
    for (x, y), side in doors_ground():
        door_mark(sh, gx + x * gpx, gy - y * gpx, gpx, side)
    cx, cy = gx + GATE[0] * gpx, gy - GATE[1] * gpx
    sh.add(f'<rect x="{f(cx + gpx / 2 - 4)}" y="{f(cy - 7)}" width="5" height="14" fill="#14120E" stroke="{INK}" stroke-width=".8"/>')
    sh.text(gx, gy + 3.5 * gpx + 22, "gatehouse east · two courtyards shown (seeded)", 9, DIM, "middle")

    kpx = 34
    kx, ky = 470, 250
    keep = {(x, y): "Keep" for x in range(-1, 2) for y in range(-1, 2)}
    plan(sh, kx, ky, kpx, keep, "KEEP · LEVEL 1", f"floor +{KEEP_FLOOR:.2f} m")
    for (x, y) in UP_STAIRS:
        stair_mark(sh, kx + x * kpx, ky - y * kpx, kpx, "TOP")
        for side in ("N", "S", "E" if x > 0 else "W"):
            sealed_mark(sh, kx + x * kpx, ky - y * kpx, kpx, side)
        door_mark(sh, kx + x * kpx, ky - y * kpx, kpx, "W" if x > 0 else "E")
    for x in range(-1, 2):
        for y in range(-1, 2):
            for side, (dx, dy) in (("N", (0, 1)), ("S", (0, -1)), ("E", (1, 0)), ("W", (-1, 0))):
                if ring(x + dx, y + dy) > 1:
                    sealed_mark(sh, kx + x * kpx, ky - y * kpx, kpx, side)

    cxp, cyp = 470, 530
    crypt = {(x, y): "Crypt" for x in range(-1, 2) for y in range(-1, 2)}
    plan(sh, cxp, cyp, kpx, crypt, "CRYPT · LEVEL −1", f"floor {CRYPT_FLOOR:+.2f} m")
    stair_mark(sh, cxp, cyp, kpx, "FOOT", down=True)
    for side in ("S", "E", "W"):
        sealed_mark(sh, cxp, cyp, kpx, side)
    door_mark(sh, cxp, cyp, kpx, "N")
    fx, fy = cxp + FINAL[0] * kpx, cyp - FINAL[1] * kpx
    sh.add(f'<path d="M{f(fx)} {f(fy - 7)} L{f(fx + 7)} {f(fy)} L{f(fx)} {f(fy + 7)} L{f(fx - 7)} {f(fy)} Z" fill="#C9A227"/>')
    for x in range(-1, 2):
        for y in range(-1, 2):
            for side, (dx, dy) in (("N", (0, 1)), ("S", (0, -1)), ("E", (1, 0)), ("W", (-1, 0))):
                if ring(x + dx, y + dy) > 1:
                    sealed_mark(sh, cxp + x * kpx, cyp - y * kpx, kpx, side)
    # vertical links between the plans
    for (x, y) in UP_STAIRS:
        sh.line(gx + x * gpx, gy - y * gpx - gpx / 2, kx + x * kpx, ky - y * kpx + kpx / 2, STAIR, .8, op=.5, dash="3 3")
    sh.line(gx, gy + gpx / 2, cxp, cyp - kpx / 2, STAIR, .8, op=.5, dash="3 3")

    # --- three-quarter view
    three_quarter(sh, 880, 275, 425, 568, 1.9, 2.0)
    sh.extra_frame.append(f'<text x="860" y="170" text-anchor="middle" font-family="{MONO}" font-size="11" '
                          f'letter-spacing="3" fill="{FAINT}">THREE-QUARTER · FROM THE SOUTH-EAST</text>')
    sh.extra_frame.append(f'<text x="860" y="184" text-anchor="middle" font-family="{MONO}" font-size="9.5" '
                          f'fill="{DIM}">exploded into its three floors · ember lines = stairs · heights × 2</text>')

    # --- key
    rows = [("GROUND", "16 bailey (minus courtyards) around 8 inner ward; centre stair down"),
            ("KEEP", f"9 rooms over the inner 3 × 3 at +{KEEP_FLOOR:.2f} m; outer faces sealed"),
            ("CRYPT", f"9 rooms under the inner 3 × 3 at {CRYPT_FLOOR:+.2f} m; final chamber ◆ seeded"),
            ("STAIRS", f"2 up (W, E ward cells, {UP_RISERS} risers) · 1 down (centre, {DOWN_RISERS} risers)"),
            ("DOORS", f"{len(doors_ground())} bailey↔ward · 1 at each stair top · 1 at the crypt foot"),
            ("ROOMS", "≈ 34 above ground + 9 crypt (today: ≈ 44 + 1)")]
    for i, (tag, txt) in enumerate(rows):
        sh.text(640, 620 + i * 16, tag, 9.5, INK, ls="1")
        sh.text(715, 620 + i * 16, txt, 9.5, DIM)
    return sh


# ---------------------------------------------------------------- sheet 2

def human(sh, x, ground_y, k):
    """1.80 m reference figure, feet at screen (x, ground_y), k px per metre."""
    h = 1.80 * k
    sh.add(f'<g fill="{INK}" opacity=".5"><ellipse cx="{f(x)}" cy="{f(ground_y - h + h * .07)}" rx="{f(h * .05)}" ry="{f(h * .07)}"/>'
           f'<rect x="{f(x - h * .08)}" y="{f(ground_y - h * .85)}" width="{f(h * .16)}" height="{f(h * .85)}" rx="{f(h * .04)}"/></g>')

# ---- the newel stair (docs/art/data/high.json "spiral-stair"): drum 4.0 m across outside, 2.4 m inside,
# in the cell's north-east corner, clockwise going up, 15 degrees a tread; here it climbs a ward storey.
DRUM_OUT, DRUM_IN, NEWEL = 2.0, 1.2, 0.12          # radii, metres
DRUM_C = (3.5, 3.5)                                # drum centre, kit metres from the cell centre (+y north)
TREAD_DEG = 15.0


def newel_plan(sh, left, top, k):
    """Plan of a stair cell: the room as a lobby, archways on four sides, the drum in the NE corner."""
    X = lambda x: left + (x + 6) * k                       # noqa: E731
    Yp = lambda y: top + (6 - y) * k                       # noqa: E731
    sh.add(f'<rect x="{f(X(-6))}" y="{f(Yp(6))}" width="{f(12 * k)}" height="{f(12 * k)}" fill="{GROUND_BG}" stroke="{FAINT}"/>')
    wall = COL["InnerWard"]
    for side in ("N", "S", "E", "W"):                      # 0.5 m walls with a 2.60 m archway in the middle
        for a0, a1 in ((-6, -1.3), (1.3, 6)):
            if side in ("N", "S"):
                y0 = 5.5 if side == "N" else -6
                sh.add(f'<rect x="{f(X(a0))}" y="{f(Yp(y0 + .5))}" width="{f((a1 - a0) * k)}" height="{f(.5 * k)}" fill="{wall}"/>')
            else:
                x0 = 5.5 if side == "E" else -6
                sh.add(f'<rect x="{f(X(x0))}" y="{f(Yp(a1))}" width="{f(.5 * k)}" height="{f((a1 - a0) * k)}" fill="{wall}"/>')
    cx, cy = X(DRUM_C[0]), Yp(DRUM_C[1])
    sh.circle(cx, cy, DRUM_OUT * k, "#5C574A", darken("#5C574A", .5), 1)
    sh.circle(cx, cy, DRUM_IN * k, "#2A251D")
    for i in range(int(360 / TREAD_DEG)):                  # tread edges
        t = math.radians(i * TREAD_DEG)
        sh.line(cx + NEWEL * k * math.cos(t), cy - NEWEL * k * math.sin(t),
                cx + DRUM_IN * k * math.cos(t), cy - DRUM_IN * k * math.sin(t), STAIR, .8, op=.7)
    sh.circle(cx, cy, NEWEL * k, INK)
    t = math.radians(225)                                  # the door into the drum faces the room (south-west)
    dx, dy = cx + DRUM_OUT * .92 * k * math.cos(t), cy - DRUM_OUT * .92 * k * math.sin(t)
    sh.add(f'<rect x="{f(dx - 6)}" y="{f(dy - 3)}" width="12" height="6" fill="{DOOR}" stroke="{INK}" stroke-width=".6" '
           f'transform="rotate(45 {f(dx)} {f(dy)})"/>')
    sh.extra_frame.append(f'<text x="{f(X(0))}" y="{f(Yp(6) - 26)}" text-anchor="middle" font-family="{MONO}" font-size="11" '
                          f'letter-spacing="3" fill="{FAINT}">STAIR CELL · PLAN</text>')
    sh.text(X(0), Yp(6) - 12, "ward room as lobby · drum 4.0 / 2.4 m, NE", 8.5, DIM, "middle")
    sh.text(X(0), Yp(-6) + 16, "archways 2.60 × 2.88 · drum door 0.90 × 2.10", 8.5, DIM, "middle")
    sh.text(cx, cy + DRUM_OUT * k + 12, "UP, CLOCKWISE", 8, INK, "middle", ls="1")


def newel_section(sh, cx, base_y, k, foot_h, head_h, risers, title, door_note):
    """Section through the drum: walls cut, the newel, and the treads round it (front half bright, back dim)."""
    Yc = lambda h: base_y - (h - foot_h) * k               # noqa: E731
    rise = (head_h - foot_h) / risers
    top = head_h + 2.2
    sh.add(f'<rect x="{f(cx - DRUM_IN * k)}" y="{f(Yc(top))}" width="{f(2 * DRUM_IN * k)}" height="{f(Yc(foot_h) - Yc(top))}" fill="#1E1A14"/>')
    for sgn in (-1, 1):                                    # the drum wall, cut
        x0 = cx + sgn * DRUM_IN * k
        x1 = cx + sgn * DRUM_OUT * k
        sh.add(f'<rect x="{f(min(x0, x1))}" y="{f(Yc(top))}" width="{f(abs(x1 - x0))}" height="{f(Yc(foot_h) - Yc(top))}" '
               f'fill="#5C574A" stroke="{darken("#5C574A", .5)}" stroke-width=".8"/>')
    for front in (False, True):                            # treads: the back half first, then the front
        for i in range(risers):
            t = math.radians(i * TREAD_DEG)
            if (math.cos(t) > 0) != front:
                continue
            x = cx + math.sin(t) * (DRUM_IN + NEWEL) / 2 * k
            half = max(3.0, abs(math.cos(t)) * (DRUM_IN - NEWEL) / 2 * k)
            y = Yc(foot_h + (i + 1) * rise)
            col = STAIR if front else darken(STAIR, .45)
            sh.add(f'<rect x="{f(x - half)}" y="{f(y)}" width="{f(2 * half)}" height="{f(rise * k * .8)}" fill="{col}" opacity=".9"/>')
    sh.add(f'<rect x="{f(cx - NEWEL * k)}" y="{f(Yc(top))}" width="{f(2 * NEWEL * k)}" height="{f(Yc(foot_h) - Yc(top))}" fill="{INK}" opacity=".75"/>')
    sh.add(f'<rect x="{f(cx - DRUM_OUT * k - 10)}" y="{f(Yc(foot_h))}" width="{f(2 * DRUM_OUT * k + 20)}" height="{f(FZ * k)}" fill="#5E5040"/>')
    sh.line(cx - DRUM_OUT * k - 30, Yc(head_h), cx + DRUM_OUT * k + 30, Yc(head_h), FAINT, .8, dash="3 3")
    sh.add(f'<rect x="{f(cx + DRUM_OUT * k)}" y="{f(Yc(head_h + 2.1))}" width="6" height="{f(2.1 * k)}" fill="{DOOR}" stroke="{INK}" stroke-width=".6"/>')
    sh.text(cx + DRUM_OUT * k + 12, Yc(head_h + 1.0), door_note, 8.5, INK)
    sh.add(f'<rect x="{f(cx - DRUM_OUT * k - 6)}" y="{f(Yc(foot_h + 2.1))}" width="6" height="{f(2.1 * k)}" fill="{DOOR}" stroke="{INK}" stroke-width=".6"/>')
    sh.text(cx - DRUM_OUT * k - 12, Yc(head_h) + 3, f"{head_h:+.2f}", 8.5, DIM, "end")
    sh.text(cx - DRUM_OUT * k - 12, Yc(foot_h) + 3, f"{foot_h:+.2f}", 8.5, DIM, "end")
    human(sh, cx - DRUM_OUT * k - 48, Yc(foot_h), k)
    sh.extra_frame.append(f'<text x="{f(cx)}" y="{f(Yc(top) - 26)}" text-anchor="middle" font-family="{MONO}" font-size="11" '
                          f'letter-spacing="3" fill="{FAINT}">NEWEL STAIR · {title}</text>')
    sh.text(cx, Yc(top) - 12, f"{risers} risers × {rise:.3f} · {TREAD_DEG:.0f} degrees a tread · {risers * TREAD_DEG:.0f} in all",
            8.5, DIM, "middle")



def sections_sheet():
    sh = Sheet("structure", "PLUNDERSPELL · CASTLE LAYOUT · DESIGN DRAFT 2026-10-03", "The Stacked Castle · Sections",
               f"keep floor +{KEEP_FLOOR:.2f} · crypt floor {CRYPT_FLOOR:+.2f} · newel risers about 0.19 m",
               "whole castle 1 m = 12 px · stair plan 1 m = 15 px · drums 1 m = 26 px", MATERIALS, seed=12)
    # --- section A-A, E-W through the centre row, looking north
    k = 12.0
    x0 = 600 - 3.5 * CELL * k            # west edge of the curtain cell
    zero = 300                            # screen y of height 0
    Y = lambda h: zero - h * k            # noqa: E731
    Xs = lambda x: 600 + x * k            # noqa: E731
    sh.line(60, zero, 1140, zero, FAINT, 1)
    sh.text(64, zero + 14, "ground 0.00", 8.5, FAINT)
    for i, x in enumerate(range(-3, 4)):
        z = ground_zone(x, 0)
        l, r = Xs(x * CELL - CELL / 2) + 1, Xs(x * CELL + CELL / 2) - 1
        top = FZ + CLEAR["CurtainWall" if z == "CurtainWall" else z]
        sh.add(f'<rect x="{f(l)}" y="{f(Y(top))}" width="{f(r - l)}" height="{f(Y(0) - Y(top))}" fill="{COL[z]}" '
               f'stroke="{darken(COL[z], .5)}" stroke-width=".8" opacity=".9"/>')
        sh.add(f'<rect x="{f(l + 6)}" y="{f(Y(top - .3))}" width="{f(r - l - 12)}" height="{f(Y(FZ) - Y(top - .3))}" fill="#14120E" opacity=".55"/>')
    for x in range(-1, 2):
        l, r = Xs(x * CELL - CELL / 2) + 1, Xs(x * CELL + CELL / 2) - 1
        sh.add(f'<rect x="{f(l)}" y="{f(Y(KEEP_TOP))}" width="{f(r - l)}" height="{f(Y(KEEP_FLOOR - FZ) - Y(KEEP_TOP))}" '
               f'fill="{COL["Keep"]}" stroke="{darken(COL["Keep"], .5)}" stroke-width=".8"/>')
        sh.add(f'<rect x="{f(l + 6)}" y="{f(Y(KEEP_TOP - .3))}" width="{f(r - l - 12)}" height="{f(Y(KEEP_FLOOR) - Y(KEEP_TOP - .3))}" fill="#14120E" opacity=".55"/>')
        sh.add(f'<rect x="{f(l)}" y="{f(Y(0))}" width="{f(r - l)}" height="{f(Y(CRYPT_SLAB) - Y(0))}" '
               f'fill="{COL["Crypt"]}" stroke="{darken(COL["Crypt"], .5)}" stroke-width=".8"/>')
        sh.add(f'<rect x="{f(l + 6)}" y="{f(Y(-.05))}" width="{f(r - l - 12)}" height="{f(Y(CRYPT_FLOOR) - Y(-.05))}" fill="#14120E" opacity=".55"/>')
    for (x, _), (lo, hi) in [(c, (FZ, KEEP_FLOOR)) for c in UP_STAIRS] + [(DOWN_STAIR, (CRYPT_FLOOR, FZ))]:
        l, r = Xs(x * CELL + DRUM_C[0] - DRUM_IN), Xs(x * CELL + DRUM_C[0] + DRUM_IN)
        sh.add(f'<rect x="{f(l)}" y="{f(Y(hi))}" width="{f(r - l)}" height="{f(Y(lo) - Y(hi))}" fill="{STAIR}" opacity=".75"/>')
        n = 6
        for i in range(n):                                    # a zigzag for the turning treads
            y0, y1 = Y(lo + (hi - lo) * i / n), Y(lo + (hi - lo) * (i + 1) / n)
            xa, xb = (l, r) if i % 2 == 0 else (r, l)
            sh.line(xa, y0, xb, y1, darken(STAIR, .5), 1)
    labels = [(-3, "CURTAIN"), (-2, "BAILEY"), (-1, "UP · WARD"), (0, "DN · WARD"), (1, "UP · WARD"), (2, "BAILEY"), (3, "GATE")]
    for x, s in labels:
        sh.text(Xs(x * CELL), Y(CRYPT_SLAB) + 22, s, 8.5, INK, "middle", ls="1")
    sh.text(Xs(0), Y(KEEP_TOP) - 8, "KEEP · LEVEL 1", 9, INK, "middle", ls="2")
    sh.text(Xs(-1.5 * CELL) - 8, Y(CRYPT_FLOOR / 2) + 3, "CRYPT · LEVEL −1", 9, INK, "end", ls="2")
    marks = sorted([(KEEP_TOP, f"+{KEEP_TOP:.2f} keep top"), (KEEP_FLOOR, f"+{KEEP_FLOOR:.2f} keep floor"),
                    (FZ + CLEAR["InnerWard"], f"+{FZ + CLEAR['InnerWard']:.2f} ward top"),
                    (FZ + CLEAR["OuterBailey"], f"+{FZ + CLEAR['OuterBailey']:.2f} bailey top"),
                    (CRYPT_FLOOR, f"{CRYPT_FLOOR:+.2f} crypt floor")], reverse=True)
    last = -1e9
    for h, text in marks:                          # labels pushed apart, leaders to the true height
        ty = max(Y(h), last + 13)
        last = ty
        sh.line(1050, Y(h), 1062, Y(h), FAINT, 1)
        sh.line(1062, Y(h), 1074, ty, FAINT, .7)
        sh.text(1078, ty + 3, text, 8.5, DIM)
    sh.extra_frame.append(f'<text x="600" y="158" text-anchor="middle" font-family="{MONO}" font-size="11" letter-spacing="3" '
                          f'fill="{FAINT}">SECTION A–A · E–W THROUGH THE CENTRE ROW, LOOKING NORTH · 1 m = 12 px</text>')

    # --- the stair cells: the art bible's newel stair (docs/art/data/high.json, spiral-stair) at ward height
    newel_plan(sh, 70, 478, 15.0)
    newel_section(sh, 450, 690, 26.0, FZ, KEEP_FLOOR, UP_RISERS, "UP · WARD → KEEP", "DOOR 0.90 → keep")
    newel_section(sh, 760, 690, 26.0, CRYPT_FLOOR, FZ, DOWN_RISERS, "DOWN · WARD → CRYPT", "DOOR (N) → crypt")
    ages = [("HIGH MEDIEVAL", "newel stair in a drum, NE corner (art bible)"),
            ("LATE MEDIEVAL", "newel stair in a corner turret, gun-loop"),
            ("BRONZE AGE", "gypsum dog-leg round a light well"),
            ("AGE OF POWDER", "dog-leg great stair (to be drawn)")]
    sh.text(950, 470, "STAIR FORM BY AGE", 9.5, INK, ls="2")
    for i, (age, form) in enumerate(ages):
        sh.text(950, 492 + i * 30, age, 9, INK, ls="1")
        sh.text(950, 505 + i * 30, form, 8.5, DIM)
    return sh


def render_pngs(paths):
    from playwright.sync_api import sync_playwright
    fonts = ("https://fonts.googleapis.com/css2?family=Eczar:wght@500;600;700;800"
             "&family=Overpass+Mono:wght@400;600&display=swap")
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page(viewport={"width": 1200, "height": 800}, device_scale_factor=2)
        for svg_path in paths:
            svg = open(svg_path, encoding="utf-8").read()
            page.set_content(f'<!doctype html><html><head><link rel="stylesheet" href="{fonts}">'
                             '<style>html,body{margin:0;background:#14120E}svg{display:block;width:1200px;height:800px}</style>'
                             f'</head><body>{svg}</body></html>', wait_until="load", timeout=20000)
            page.evaluate("document.fonts.ready")
            png = svg_path[:-4] + ".png"
            page.locator("svg").first.screenshot(path=png)
            print("rendered", os.path.relpath(png, REPO))
        browser.close()


def main():
    os.makedirs(OUT, exist_ok=True)
    written = []
    for name, build in (("multi-floor-layout", layout_sheet), ("multi-floor-sections", sections_sheet)):
        path = os.path.join(OUT, name + ".svg")
        with open(path, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(build().render(ground=None))
        print("wrote", os.path.relpath(path, REPO))
        written.append(path)
    if "--no-png" not in sys.argv:
        render_pngs(written)


if __name__ == "__main__":
    main()
