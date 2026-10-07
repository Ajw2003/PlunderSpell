"""
The Market: the lantern-lit night yard between the Ages, its four stalls and their props
(docs/plans/diegetic-ui-lair-market.md, "The Market, and haggling"; concept
docs/art/concept/lair/market.png). Issue 287.

Runs only inside Blender's Python, like every other builder. Same toolbox as the Lair
(lair_builders' box / cylinder / prism helpers) and the same atlas: no pigment is added.

Local frame of every stall prop: base centre on the origin, the FRONT (where the player
stands, the counter, the slate) toward local -Y, the vendor behind toward +Y.

MarketYard is a one-off ~20 x 20 m open-air yard (NOT a 12 m castle cell, so no cell-footprint
check; only "Castle*" modules are held to it): no roof, flagged floor slab whose walkable top
is Z = 0.30 (scale.md), low walls east/west/south (a 4 m gap in the south wall is the way in)
and house-fronts on the north side. Static dressing only. Origin: centre of the yard, slab
underside on Z = 0.

Small items: the validator has no minimum-size rule (only a 3 cm pivot tolerance), so MarketCoin
(4 cm across, 4 mm thick) needs no exception; it is simply bottom-flush like the rest.
MarketScalesBeam is the one piece whose pivot is NOT its base: it tips about its fulcrum, so it
is authored centred on the fulcrum and kept within +/-0.03 m in Z (like the Lair's dial rings).
"""
import math

from mathutils import Euler

import mesh_kit as mk
from lair_builders import _arc, _box, _cyl, _limb, _prism, _sb

# ── pigments (all existing) ───────────────────────────────────────────────
STONE = "vellum_faint"
DRESSED = "vellum_dim"
FLAG = "ash_hi"
PAVE = "line"
JOINT = "bone_black"
OAK, LEATHER = "oak", "leather"
IRON, STEEL = "line", "iron"
BRASS, GOLD = "bronze", "orpiment"
VELLUM = "vellum"
TARP = "verdigris_lo"
RED, LAPIS, VERD = "madder", "lapis", "verdigris"

FL = 0.30                      # walkable floor of the yard (scale.md)
LANTERN_H = 0.42               # lantern base to the top of its hanging ring


def _slab(bm, uv, pigment, centre, size, rx=0.0):
    """A box exactly as sized, tilted about X by rx radians (an awning, a roof pitch)."""
    _sb(bm, uv, pigment, centre, size, rot=Euler((rx, 0, 0)))


def _ball(bm, uv, pigment, centre, r, segments=10, rings=6):
    mk.paint(bm, mk.add_sphere(bm, r, loc=centre, segments=segments, rings=rings), pigment, uv)


# ══ the yard ═══════════════════════════════════════════════════════════════

HALF = 10.0
WELL_R_OUT, WELL_R_IN = 0.95, 0.6
# lantern posts (x, y): the arm of each points at the yard's centre
LANTERN_POSTS = [(-8.0, 6.0), (8.0, 6.0), (-8.0, -4.0), (8.0, -4.0), (-3.0, -8.2), (3.0, -8.2)]
POST_H, ARM = 2.7, 0.55
WELL_LANTERN = (0.55, 0.0, 2.3)     # hook of the lantern hung from the well's crossbeam (x, y, height above the floor)


def post_hook(p):
    """Yard-local x, y and height above the floor of the hook at the end of a lantern post's arm."""
    n = math.hypot(*p)
    return (p[0] - ARM * p[0] / n, p[1] - ARM * p[1] / n, POST_H - 0.12)


def _rand(i, k):
    return (math.sin(i * 12.9898 + k * 78.233) * 43758.5453) % 1.0


def _well(bm, uv):
    for course in range(3):
        z0, z1 = FL + course * 0.3, FL + (course + 1) * 0.3
        n = 12
        for i in range(n):
            off = 0.5 * course
            a0 = 2 * math.pi * (i + off) / n + 0.03
            a1 = 2 * math.pi * (i + 1 + off) / n - 0.03
            ro = WELL_R_OUT + 0.01 * (i % 2)
            pts = [(WELL_R_IN * math.cos(a0), WELL_R_IN * math.sin(a0)), (ro * math.cos(a0), ro * math.sin(a0)),
                   (ro * math.cos(a1), ro * math.sin(a1)), (WELL_R_IN * math.cos(a1), WELL_R_IN * math.sin(a1))]
            _prism(bm, uv, DRESSED if (i + course) % 2 == 0 else STONE, pts, "z", z0 - 0.01 * course, z1 - 0.012)
    _cyl(bm, uv, TARP, (0, 0, FL + 0.5), WELL_R_IN - 0.02, 0.04, segments=14)               # the black water
    for s in (-1, 1):
        _box(bm, uv, OAK, (s * 1.18, 0, FL + 1.25), (0.12, 0.12, 2.5))                      # the two posts
        _box(bm, uv, STONE, (s * 1.18, 0, FL + 0.07), (0.3, 0.3, 0.14))                      # footings
    _box(bm, uv, OAK, (0, 0, FL + 2.72), (2.7, 0.14, 0.14))                                  # crossbeam
    _cyl(bm, uv, OAK, (0, 0, FL + 2.3), 0.07, 2.36, rot=Euler((0, math.pi / 2, 0)), segments=8)   # windlass roller
    _sb(bm, uv, IRON, (1.3, -0.15, FL + 2.3), (0.04, 0.3, 0.04))                              # crank
    _sb(bm, uv, LEATHER, (0, 0, FL + 2.0), (0.02, 0.02, 0.62))                                # rope
    _cyl(bm, uv, OAK, (0, 0, FL + 1.8), 0.13, 0.26, segments=8, radius2=0.1)                  # bucket
    _sb(bm, uv, IRON, (WELL_LANTERN[0], 0, FL + WELL_LANTERN[2] + 0.25), (0.015, 0.015, 0.5))   # lantern chain


def _lantern_post(bm, uv, p):
    x, y = p
    n = math.hypot(x, y)
    ux, uy = -x / n, -y / n
    _box(bm, uv, STONE, (x, y, FL + 0.12), (0.42, 0.42, 0.24))
    _cyl(bm, uv, OAK, (x, y, FL + POST_H / 2), 0.065, POST_H, segments=8)
    _limb(bm, uv, OAK, (x, y, FL + POST_H - 0.05), (x + ux * ARM, y + uy * ARM, FL + POST_H - 0.05), 0.04, segments=6)
    _limb(bm, uv, OAK, (x, y, FL + POST_H - 0.55), (x + ux * 0.3, y + uy * 0.3, FL + POST_H - 0.1), 0.025, segments=4)   # brace
    hx, hy, hz = post_hook(p)
    _sb(bm, uv, IRON, (hx, hy, FL + hz + 0.05), (0.015, 0.015, 0.14))                         # hook


def _house(bm, uv, x0, x1, h, windows, ridge=1.1):
    y0, y1 = 8.5, 10.0
    cx, w = (x0 + x1) / 2, x1 - x0
    _box(bm, uv, STONE, (cx, (y0 + y1) / 2, FL + h / 2), (w, y1 - y0, h))
    _prism(bm, uv, OAK, [(y0 - 0.25, FL + h), (y1 + 0.1, FL + h), ((y0 + y1) / 2, FL + h + ridge)], "x", x0 - 0.15, x1 + 0.15)
    for i, (wx, wz, lit) in enumerate(windows):
        _sb(bm, uv, OAK, (cx + wx, y0 - 0.012, FL + wz), (0.64, 0.03, 0.89))
        _sb(bm, uv, GOLD if lit else JOINT, (cx + wx, y0 - 0.03 - 0.001 * i, FL + wz), (0.5, 0.04, 0.75))


def build_market_yard(bm, uv):
    _box(bm, uv, FLAG, (0, 0, FL / 2), (2 * HALF, 2 * HALF, FL))
    for v in [i * 1.25 for i in range(-7, 8)]:
        _sb(bm, uv, JOINT, (0, v, FL + 0.004), (2 * HALF, 0.03, 0.016))
        _sb(bm, uv, JOINT, (v, 0, FL + 0.005), (0.03, 2 * HALF, 0.016))
    _cyl(bm, uv, PAVE, (0, 0, FL + 0.01), 3.7, 0.018, segments=28)          # the lit cobbled round about the well
    for i in range(54):                                                    # loose cobbles in rings
        ring = 1.55 + 0.45 * (i % 5)
        a = 2 * math.pi * (i / 54 * 5 + _rand(i, 1) * 0.3)
        r = ring + (_rand(i, 2) - 0.5) * 0.25
        _sb(bm, uv, FLAG if i % 4 else STONE, (r * math.cos(a), r * math.sin(a), FL + 0.021),
            (0.22 + 0.08 * _rand(i, 3), 0.18 + 0.06 * _rand(i, 4), 0.02), rot=Euler((0, 0, a + _rand(i, 5))))
    _well(bm, uv)
    for p in LANTERN_POSTS:
        _lantern_post(bm, uv, p)
    # walls: east and west full length, south with a 4 m way in
    for s in (-1, 1):
        _box(bm, uv, STONE, (s * (HALF - 0.25), 0, FL + 0.55), (0.5, 2 * HALF - 1.0, 1.1))
        _box(bm, uv, DRESSED, (s * (HALF - 0.25), 0, FL + 1.15), (0.62, 2 * HALF - 0.9, 0.12))
        xs = s * (HALF + 2.0) / 2
        _box(bm, uv, STONE, (xs, -HALF + 0.25, FL + 0.55), (HALF - 2.0, 0.5, 1.1))
        _box(bm, uv, DRESSED, (xs, -HALF + 0.25, FL + 1.15), (HALF - 1.9, 0.62, 0.12))
        _box(bm, uv, STONE, (s * 2.0, -HALF + 0.25, FL + 0.8), (0.5, 0.62, 1.6))
    # north: four house fronts, lit and dark windows
    _house(bm, uv, -10.0, -4.6, 5.2, [(-1.6, 2.6, True), (0.2, 2.9, False), (1.7, 2.4, True), (-0.4, 4.2, False)])
    _house(bm, uv, -4.4, 1.0, 4.4, [(-1.2, 2.5, False), (1.0, 2.7, True)], ridge=0.9)
    _house(bm, uv, 1.2, 6.6, 5.8, [(-1.5, 2.4, True), (0.3, 3.1, True), (1.8, 2.5, False), (0.3, 4.8, False)], ridge=1.3)
    _house(bm, uv, 6.8, 10.0, 4.6, [(-0.8, 2.6, False), (0.9, 3.2, True)], ridge=1.0)


# ══ stalls and their props ═════════════════════════════════════════════════

def build_market_counter(bm, uv):
    """The stall counter the haggle happens over: 1.8 x 0.6 m, top at 1.00 m. Plain flat top
    (coins and loot are put on it), planked front on local -Y."""
    _box(bm, uv, OAK, (0, 0.02, 0.45), (1.7, 0.5, 0.9))
    _box(bm, uv, JOINT, (0, 0, 0.04), (1.74, 0.52, 0.08))
    for i in range(8):
        x = -0.7 + i * 0.2
        _sb(bm, uv, LEATHER if i % 2 else OAK, (x, -0.255 - 0.002 * (i % 2), 0.48), (0.19, 0.02, 0.76))
    _sb(bm, uv, BRASS, (0, -0.265, 0.88), (1.66, 0.012, 0.04))
    _box(bm, uv, OAK, (0, 0, 0.97), (1.8, 0.6, 0.06))                         # top: 0.94 to 1.00
    for s in (-1, 1):
        _box(bm, uv, OAK, (s * 0.88, 0, 0.85), (0.08, 0.5, 0.2))


def build_market_slate_board(bm, uv):
    """A chalk slate on a post, 1.6 m tall, board 0.86 x 0.58 m facing local -Y."""
    _box(bm, uv, OAK, (0, 0, 0.03), (0.5, 0.24, 0.06))
    _box(bm, uv, OAK, (0, 0, 0.03), (0.2, 0.5, 0.06))
    _box(bm, uv, OAK, (0, 0.06, 0.7), (0.09, 0.07, 1.4))
    _box(bm, uv, OAK, (0, 0, 1.3), (0.86, 0.06, 0.58))
    _sb(bm, uv, FLAG, (0, -0.03, 1.3), (0.74, 0.02, 0.46))
    for r, (n, w) in enumerate(((5, 0.12), (4, 0.11), (3, 0.1))):
        for k in range(n):
            ww = w * (0.6 + 0.8 * _rand(r * 7 + k, 9))
            _sb(bm, uv, VELLUM, (-0.3 + k * 0.145 + ww / 2, -0.0415, 1.46 - r * 0.15), (ww, 0.006, 0.022))


def build_market_scales_base(bm, uv):
    """Balance scales' stand: round foot, pillar, forked head; the beam rests on its top (Z = 0.50)."""
    _cyl(bm, uv, BRASS, (0, 0, 0.015), 0.13, 0.03, segments=10)
    _cyl(bm, uv, BRASS, (0, 0, 0.05), 0.06, 0.05, segments=10, radius2=0.03)
    _cyl(bm, uv, BRASS, (0, 0, 0.26), 0.022, 0.4, segments=8)
    _cyl(bm, uv, BRASS, (0, 0, 0.45), 0.04, 0.05, segments=8)
    for s in (-1, 1):
        _sb(bm, uv, BRASS, (0, s * 0.03, 0.488), (0.05, 0.012, 0.05))                # the fork the beam sits in
    _sb(bm, uv, IRON, (0, 0, 0.45), (0.02, 0.09, 0.02))


def build_market_scales_beam(bm, uv):
    """The beam, centred on its fulcrum (the pivot) so it can tip: 0.60 m, within +/-0.03 m in Z."""
    _sb(bm, uv, BRASS, (0, 0, 0), (0.56, 0.018, 0.034))
    _sb(bm, uv, BRASS, (0, 0, 0.0), (0.05, 0.028, 0.05))
    _sb(bm, uv, GOLD, (0, 0, 0.0), (0.012, 0.034, 0.012))
    for s in (-1, 1):
        _sb(bm, uv, BRASS, (s * 0.28, 0, -0.004), (0.03, 0.02, 0.046))
        _cyl(bm, uv, IRON, (s * 0.28, 0, -0.01), 0.012, 0.014, rot=Euler((math.pi / 2, 0, 0)), segments=6)


def build_market_scales_pan(bm, uv):
    """One pan: dish, three chains and a hanging ring; the ring is at 0.26 m above the base."""
    prof = [(0.012, 0.0), (0.07, 0.004), (0.092, 0.034), (0.086, 0.036), (0.065, 0.012), (0.012, 0.009)]
    mk.paint(bm, mk.add_lathe(bm, prof, segments=12), BRASS, uv)
    for i in range(3):
        a = math.radians(120 * i + 30)
        _limb(bm, uv, STEEL, (0.088 * math.cos(a), 0.088 * math.sin(a), 0.034), (0.0, 0.0, 0.24), 0.005, segments=4)
    _cyl(bm, uv, BRASS, (0, 0, 0.25), 0.016, 0.022, segments=8)


def build_market_lantern(bm, uv):
    """Hanging lantern, 0.42 m with its ring: base plate, four iron posts, glowing core, cap."""
    _cyl(bm, uv, IRON, (0, 0, 0.012), 0.075, 0.024, segments=8)
    for i in range(4):
        a = math.pi / 4 + math.pi / 2 * i
        _sb(bm, uv, IRON, (0.062 * math.cos(a), 0.062 * math.sin(a), 0.17), (0.02, 0.02, 0.3), rot=Euler((0, 0, a)))
    _cyl(bm, uv, GOLD, (0, 0, 0.17), 0.05, 0.27, segments=6)                        # the glowing core
    _cyl(bm, uv, IRON, (0, 0, 0.32), 0.09, 0.02, segments=8)
    _cyl(bm, uv, IRON, (0, 0, 0.35), 0.085, 0.045, segments=8, radius2=0.025)
    _cyl(bm, uv, IRON, (0, 0, 0.385), 0.014, 0.04, segments=6)
    _cyl(bm, uv, IRON, (0, 0, 0.405), 0.026, 0.01, rot=Euler((math.pi / 2, 0, 0)), segments=8)   # ring


def build_market_coin(bm, uv):
    """One coin, 4 cm across and 4 mm thick, lying flat."""
    _cyl(bm, uv, GOLD, (0, 0, 0.002), 0.02, 0.004, segments=10)
    _cyl(bm, uv, BRASS, (0, 0, 0.0043), 0.012, 0.0016, segments=8)


def build_market_coin_stack(bm, uv):
    """Eight coins stacked a little crooked, 3 cm tall."""
    for i in range(8):
        _cyl(bm, uv, GOLD if i % 3 else BRASS, (0.003 * math.cos(i * 2.1), 0.003 * math.sin(i * 2.1), 0.002 + 0.0037 * i),
             0.02 - 0.0003 * (i % 2), 0.004, segments=10)


def build_market_pouch(bm, uv):
    """A drawstring coin pouch, 0.18 m tall."""
    prof = [(0.012, 0.0), (0.05, 0.008), (0.075, 0.04), (0.07, 0.085), (0.04, 0.115), (0.03, 0.125),
            (0.05, 0.15), (0.055, 0.175), (0.012, 0.17)]
    mk.paint(bm, mk.add_lathe(bm, prof, segments=10), LEATHER, uv)
    _cyl(bm, uv, RED, (0, 0, 0.127), 0.037, 0.012, segments=10)


def build_market_anvil(bm, uv):
    """An anvil on an oak stump, 0.69 m tall, 0.9 m long (horn toward +X)."""
    _cyl(bm, uv, OAK, (0, 0, 0.225), 0.23, 0.45, segments=10, radius2=0.2)
    _sb(bm, uv, STEEL, (0, 0, 0.47), (0.36, 0.2, 0.06))
    _sb(bm, uv, STEEL, (0.0, 0, 0.55), (0.2, 0.12, 0.1))
    _sb(bm, uv, STEEL, (0.0, 0, 0.64), (0.62, 0.16, 0.09))
    _sb(bm, uv, STEEL, (-0.38, 0, 0.655), (0.16, 0.15, 0.07))
    _cyl(bm, uv, STEEL, (0.45, 0, 0.625), 0.065, 0.3, rot=Euler((0, math.pi / 2, 0)), segments=8, radius2=0.015)


# ── the fence's cart ──────────────────────────────────────────────────────

def _wheel(bm, uv, y, r=0.45):
    cz = r
    _cyl(bm, uv, OAK, (0, y, cz), 0.07, 0.14, rot=Euler((math.pi / 2, 0, 0)), segments=8)
    rm, n = r - 0.03, 12
    for k in range(n):
        a = -math.pi / 2 + 2 * math.pi * k / n
        c = 2 * rm * math.sin(math.pi / n) + 0.02
        _sb(bm, uv, OAK if k % 2 else LEATHER, (rm * math.cos(a), y, cz + rm * math.sin(a)), (c, 0.07, 0.06),
            rot=Euler((0, -(a + math.pi / 2), 0)))
        if k % 2 == 0:
            _limb(bm, uv, OAK, (0.05 * math.cos(a), y, cz + 0.05 * math.sin(a)),
                  ((rm - 0.03) * math.cos(a), y, cz + (rm - 0.03) * math.sin(a)), 0.02, segments=4)


def build_market_fence_cart(bm, uv):
    """Hand-cart, 1.5 m bed on two 0.9 m wheels, tarp over the load, two shafts to the ground
    (-X is the front). About 2.5 m long including the shafts, 1.2 m tall."""
    for s in (-1, 1):
        _wheel(bm, uv, s * 0.52)
    _sb(bm, uv, IRON, (0, 0, 0.45), (0.05, 1.04, 0.05))
    _box(bm, uv, OAK, (0, 0, 0.6), (1.5, 0.84, 0.07))
    for s in (-1, 1):
        _box(bm, uv, LEATHER, (0, s * 0.4, 0.74), (1.5, 0.05, 0.25))
        _box(bm, uv, OAK, (s * 0.72, 0, 0.74), (0.05, 0.8, 0.25))
        _limb(bm, uv, OAK, (0.55, s * 0.3, 0.6), (-1.9, s * 0.3, 0.035), 0.035, segments=6)
    _sb(bm, uv, OAK, (-1.75, 0, 0.1), (0.05, 0.64, 0.05))                                # shaft crossbar
    top = 1.2
    cr = (0.8 ** 2 + 0.45 ** 2) / (2 * 0.45)
    al = math.degrees(math.asin(0.8 / cr))
    _prism(bm, uv, TARP, _arc(0, top - cr, cr, 90 + al, 90 - al, 10), "y", -0.46, 0.46)
    for x in (-0.4, 0.2):
        zc = top - cr + math.sqrt(cr * cr - x * x)
        _sb(bm, uv, LEATHER, (x, 0, zc + 0.004), (0.04, 0.94, 0.03), rot=Euler((0, math.atan2(x, math.sqrt(cr * cr - x * x)), 0)))


# ── the goldsmith's stall ─────────────────────────────────────────────────

def build_market_goldsmith_stall(bm, uv):
    """Stall frame 2.4 m wide, 1.1 m deep, back wall and a shelf of gold bars, striped awning
    whose valance hangs to 2.05 m (above head height). The counter is separate; it goes at
    local (0, -0.6). A lantern chain hangs at local (0.9, -0.4), bottom hook at 1.85 m."""
    tilt = math.radians(20)
    for s in (-1, 1):
        _box(bm, uv, OAK, (s * 1.1, 0.55, 1.4), (0.1, 0.1, 2.8))
        _box(bm, uv, OAK, (s * 1.1, -0.55, 1.17), (0.1, 0.1, 2.34))
    for i in range(11):
        _sb(bm, uv, OAK if i % 2 else LEATHER, (-1.0 + i * 0.2, 0.65, 1.2), (0.2 + 0.004 * (i % 2), 0.05, 2.4 - 0.02 * (i % 2)))
    _box(bm, uv, OAK, (0, -0.55, 2.26), (2.3, 0.08, 0.08))
    _box(bm, uv, OAK, (0, 0.5, 1.8), (2.2, 0.4, 0.05))
    for x in (-0.7, 0.0, 0.7):
        _sb(bm, uv, GOLD, (x, 0.5, 1.86), (0.3, 0.13, 0.06))
    _sb(bm, uv, GOLD, (-0.35, 0.52, 1.92), (0.3, 0.13, 0.06))
    L = 1.7 / math.cos(tilt)
    cy, cz = -0.1, 2.564
    for i in range(12):
        _slab(bm, uv, DRESSED if i % 2 == 0 else STONE, (-1.1 + 0.2 * i, cy, cz + 0.004 * (i % 2)),
              (0.206, L, 0.04 + 0.004 * (i % 2)), tilt)
    fy = cy - (L / 2) * math.cos(tilt) - 0.005
    fz = cz - (L / 2) * math.sin(tilt) - 0.02
    for i in range(12):
        x = -1.1 + 0.2 * i
        pts = [(x - 0.095, fz + 0.03), (x - 0.095, fz - 0.14), (x, fz - 0.21), (x + 0.095, fz - 0.14), (x + 0.095, fz + 0.03)]
        _prism(bm, uv, DRESSED if i % 2 == 0 else STONE, pts, "y", fy - 0.012, fy + 0.012)
    _sb(bm, uv, IRON, (0.9, -0.4, 2.15), (0.015, 0.015, 0.6))


# ── the pardoner's booth ──────────────────────────────────────────────────

def _house_shape(bm, uv, pigment, x, y, z, w, h):
    _sb(bm, uv, pigment, (x, y, z + h * 0.35), (w, 0.1, h * 0.7))
    _prism(bm, uv, pigment, [(x - w / 2 - 0.01, z + h * 0.7), (x + w / 2 + 0.01, z + h * 0.7), (x, z + h)], "y", y - 0.052, y + 0.052)


def _cross(bm, uv, pigment, x, y, z, h):
    _sb(bm, uv, pigment, (x, y, z + h / 2), (h * 0.14, h * 0.14, h))
    _sb(bm, uv, pigment, (x, y, z + h * 0.7), (h * 0.5, h * 0.15, h * 0.14))


def build_market_pardoner_booth(bm, uv):
    """Roofed booth 2.4 x 1.8 m, gable roof to 3.2 m with a cross on the ridge, hung with relic
    shapes (two gilt reliquaries, two lapis boxes with red seals, a cross). Counter goes at local
    (0, -0.95). A lantern chain hangs at local (0.9, -0.85), bottom hook at 2.0 m."""
    for sx in (-1, 1):
        for sy in (-1, 1):
            _box(bm, uv, OAK, (sx * 1.1, sy * 0.85, 1.2), (0.1, 0.1, 2.4))
        _box(bm, uv, OAK, (sx * 1.1, 0, 2.42), (0.1, 1.8, 0.1))
    _box(bm, uv, OAK, (0, 0.85, 2.38), (2.3, 0.1, 0.1))
    _box(bm, uv, OAK, (0, -0.85, 2.38), (2.3, 0.1, 0.1))
    _box(bm, uv, OAK, (0, 0.1, 2.36), (2.2, 0.08, 0.08))                          # the beam the relics hang from
    for i in range(11):
        _sb(bm, uv, OAK if i % 2 else LEATHER, (-1.0 + i * 0.2, 0.95, 1.1), (0.2 + 0.004 * (i % 2), 0.05, 2.2))
    th = math.atan2(0.65, 1.5)
    L = math.hypot(0.65, 1.5)
    _slab(bm, uv, LEATHER, (0, -0.75, 2.875), (3.0, L, 0.06), th)
    _slab(bm, uv, OAK, (0, 0.76, 2.875), (3.04, L + 0.02, 0.07), -th)
    _box(bm, uv, OAK, (0, 0, 3.2), (3.1, 0.14, 0.12))
    _cross(bm, uv, VELLUM, 0, 0, 3.22, 0.55)
    for x, z, kind in ((-0.8, 1.75, "house"), (-0.4, 1.9, "box"), (0.0, 1.8, "cross"), (0.4, 1.75, "house"), (0.8, 1.9, "box")):
        _sb(bm, uv, LEATHER, (x, 0.1, (z + 2.36) / 2), (0.012, 0.012, 2.36 - z))
        if kind == "house":
            _house_shape(bm, uv, GOLD, x, 0.1, z - 0.18, 0.15, 0.18)
        elif kind == "box":
            _sb(bm, uv, LAPIS, (x, 0.1, z - 0.1), (0.13, 0.07, 0.2))
            _sb(bm, uv, RED, (x, 0.0635, z - 0.1), (0.06, 0.012, 0.06))
        else:
            _cross(bm, uv, VELLUM, x, 0.1, z - 0.3, 0.3)
    _sb(bm, uv, IRON, (0.9, -0.85, 2.2), (0.015, 0.015, 0.35))


# ── the antiquarian's cabinet ─────────────────────────────────────────────

def build_market_antiquarian_cabinet(bm, uv):
    """Tall cabinet of curiosities, 1.2 x 0.5 m, 2.4 m tall, open front toward local -Y, three
    shelves holding a globe, hourglass, curved blade, jug, cone, ball and a gem."""
    _box(bm, uv, "line", (0, 0, 0.09), (1.3, 0.56, 0.18))
    for s in (-1, 1):
        _box(bm, uv, OAK, (s * 0.57, 0, 1.2), (0.07, 0.5, 2.1))
    _box(bm, uv, "line", (0, 0.23, 1.2), (1.14, 0.04, 2.1))
    _box(bm, uv, OAK, (0, 0, 2.2), (1.24, 0.5, 0.08))
    _box(bm, uv, "line", (0, 0, 2.3), (1.38, 0.62, 0.1))
    _box(bm, uv, OAK, (0, 0, 2.38), (1.3, 0.56, 0.06))
    for z in (0.22, 0.88, 1.54):
        _box(bm, uv, OAK, (0, 0, z), (1.1, 0.46, 0.04))
        _sb(bm, uv, BRASS, (0, -0.235, z), (1.12, 0.012, 0.05))
    for s in (-1, 1):
        _sb(bm, uv, BRASS, (s * 0.61, -0.255, 1.2), (0.024, 0.012, 2.1))
    # shelf 1: a globe on a stand, an hourglass
    z = 0.24
    _cyl(bm, uv, BRASS, (-0.3, 0, z + 0.03), 0.08, 0.06, segments=8, radius2=0.04)
    _ball(bm, uv, VERD, (-0.3, 0, z + 0.2), 0.14)
    _sb(bm, uv, BRASS, (-0.3, 0, z + 0.2), (0.34, 0.012, 0.012), rot=Euler((0, 0.4, 0)))
    _cyl(bm, uv, OAK, (0.28, 0, z + 0.015), 0.1, 0.03, segments=8)
    _cyl(bm, uv, OAK, (0.28, 0, z + 0.435), 0.1, 0.03, segments=8)
    for sx in (-1, 1):
        _cyl(bm, uv, BRASS, (0.28 + sx * 0.085, 0, z + 0.23), 0.012, 0.4, segments=4)
    _cyl(bm, uv, VELLUM, (0.28, 0, z + 0.125), 0.07, 0.19, segments=8, radius2=0.012)
    _cyl(bm, uv, VELLUM, (0.28, 0, z + 0.32), 0.012, 0.19, segments=8, radius2=0.07)
    # shelf 2: a curved blade, a pale ball
    z = 0.9
    cz0 = z - 0.28
    outer = _arc(-0.05, cz0, 0.55, 150, 55, 10)
    inner = _arc(-0.05, cz0, 0.505, 55, 150, 10)
    _prism(bm, uv, STEEL, outer + inner, "y", -0.012, 0.012)
    _sb(bm, uv, OAK, (0.18, 0, z + 0.21), (0.05, 0.05, 0.14), rot=Euler((0, 0.9, 0)))
    _ball(bm, uv, VELLUM, (0.4, 0, z + 0.12), 0.1)
    # shelf 3: a jug, a cone, a striped ball with a gem
    z = 1.56
    _cyl(bm, uv, OAK, (-0.33, 0, z + 0.12), 0.1, 0.24, segments=8, radius2=0.07)
    _cyl(bm, uv, LEATHER, (-0.33, 0, z + 0.26), 0.04, 0.05, segments=8)
    _cyl(bm, uv, LAPIS, (0.0, 0, z + 0.16), 0.11, 0.3, rot=Euler((0, 0, math.pi / 4)), segments=4, radius2=0.0)
    _ball(bm, uv, BRASS, (0.35, 0, z + 0.1), 0.1)
    _sb(bm, uv, "line", (0.35, 0, z + 0.1), (0.21, 0.022, 0.025))
    _sb(bm, uv, RED, (0.2, -0.17, z + 0.03), (0.05, 0.05, 0.05), rot=Euler((0.4, 0.4, 0.4)))
