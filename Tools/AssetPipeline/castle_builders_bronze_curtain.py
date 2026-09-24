"""
Bronze Age CurtainWall pieces: BronzeLionGate, BronzeWallStraight, BronzeWallCorner, BronzeBastion, BronzeGateApproach.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/BronzeAge/<Key>.json,
drawing: docs/art/rooms/concept/BronzeAge/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_bronze.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_bronze import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


# ── CurtainWall: cyclopean masonry and mud-brick breastworks ────────────
#
# Bronze Age walls are thick rather than tall: a 2.4 m mass of huge
# irregular blocks up to a plastered wall-walk at 4.1 m, then a 1.1 m
# mud-brick parapet with rounded merlons to the shared 5.2 m CurtainWall
# height (art bible, the Lion Gate). The block layout is cyclopean.py's, the
# same one the sheets draw. A run is described along its wall (u) and inward
# from the cell edge (d): "south" puts u on x and d on +y from y = -HALF,
# "west" puts u on y and d on +x from x = -HALF.

import cyclopean as cy  # noqa: E402

H = rk.HALF
WALL_DEPTH = 2.4
WALK_Z = 4.1
PARAPET_T = 0.4
PARAPET_IN = 0.04          # the parapet's outer face, inside the cell line so the slots stay in the footprint
LOW = 0.5                  # the breastwork under the merlons
PITCH = 1.2                # merlon + crenel
CONGLOM = ASHLAR = "vellum_faint"


def _at(side, u, d, z):
    return (u, -H + d, z) if side == "south" else (-H + d, u, z)


def _size(side, su, sd, sz):
    return (su, sd, sz) if side == "south" else (sd, su, sz)


def _merlon_centres(u0, u1):
    n = int((u1 - u0 + 1e-6) // PITCH)
    mid = (u0 + u1) / 2
    return [mid - (n - 1) * PITCH / 2 + k * PITCH for k in range(n)]


def _mass(bm, uv, u0, u1, depth, top=WALK_Z, side="south", seed=0, bottom=0.0, pigment=CURTAIN, ragged=True):
    """A cyclopean run from u0 to u1, `depth` thick from the cell edge, bottom..top.
    ragged=False keeps the inner face flat (a face that is plastered or built against)."""
    for b in cy.blocks(u0, u1, top, seed=seed, bottom=bottom):
        d0, d1 = b["inset"], depth + (b["dj"] if ragged else 0.0)
        cb._box(bm, uv, pigment, _at(side, (b["u0"] + b["u1"]) / 2, (d0 + d1) / 2, (b["z0"] + b["z1"]) / 2),
                _size(side, b["u1"] - b["u0"], d1 - d0, b["z1"] - b["z0"]))


def _walk(bm, uv, u0, u1, depth, z=WALK_Z, side="south"):
    """The plastered walk on top of a run, from behind the parapet to 0.2 m short of the
    ragged inner face (the shallowest block reaches depth - 0.15)."""
    d0, d1 = PARAPET_IN + PARAPET_T, depth - 0.2
    cb._box(bm, uv, WALL, _at(side, (u0 + u1) / 2, (d0 + d1) / 2, z + 0.05), _size(side, u1 - u0, d1 - d0, 0.1))


def _parapet(bm, uv, u0, u1, base=WALK_Z, side="south", slots=True):
    """The 1.1 m mud-brick breastwork on the outer edge: a 0.5 m low wall, rounded merlons
    (a box under a half-sunk cylinder) at 1.2 m pitch, and a sling slot, a dark inset on
    the outer face, in every other merlon from the first."""
    d = PARAPET_IN + PARAPET_T / 2
    cb._box(bm, uv, MUDBRICK, _at(side, (u0 + u1) / 2, d, base + LOW / 2), _size(side, u1 - u0, PARAPET_T, LOW))
    rot = Euler((math.radians(90), 0, 0)) if side == "south" else Euler((0, math.radians(90), 0))
    for k, u in enumerate(_merlon_centres(u0, u1)):
        cb._box(bm, uv, MUDBRICK, _at(side, u, d, base + LOW + 0.15), _size(side, 0.6, PARAPET_T, 0.3))
        mk.paint(bm, mk.add_cylinder(bm, 0.3, PARAPET_T, loc=_at(side, u, d, base + LOW + 0.3), rot=rot, segments=8),
                 MUDBRICK, uv)
        if slots and k % 2 == 0:
            cb._box(bm, uv, SOOT, _at(side, u, PARAPET_IN, base + 0.6), _size(side, 0.12, 0.06, 0.5))


def build_bronze_wall_straight(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeWallStraight.svg"""
    _mass(bm, uv, -H, H, WALL_DEPTH, seed=0)
    _walk(bm, uv, -H, H, WALL_DEPTH)
    _parapet(bm, uv, -H, H)


def _steps_up(bm, uv, u_face, u_dir, depth, side, low, high, n):
    """n stone steps on a walk, climbing from `low` (the walk's top) toward a face at
    u_face, the highest against it; the next rise lands on `high`. Each step is a
    block standing on the walk, as wide as the walk."""
    rise = (high - low) / (n + 1)
    d0, d1 = PARAPET_IN + PARAPET_T, depth - 0.2
    for k in range(n):
        u = u_face + u_dir * 0.35 * (k + 0.5)
        top = high - rise * (k + 1)
        cb._box(bm, uv, ASHLAR, _at(side, u, (d0 + d1) / 2, (low + top) / 2), _size(side, 0.35, d1 - d0, top - low))


def build_bronze_wall_corner(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeWallCorner.svg"""
    t, top = 4.4, 5.5
    te = -H + t                                            # the tower's east and north faces
    _mass(bm, uv, -H, te, t, top=top, seed=3)
    d0 = PARAPET_IN + PARAPET_T
    cb._box(bm, uv, WALL, ((-H + d0 + te) / 2, (-H + d0 + te - 0.2) / 2, top + 0.05), (te + H - d0, te - 0.2 + H - d0, 0.1))
    _parapet(bm, uv, -H, te, base=top)
    _parapet(bm, uv, -H + d0, te, base=top, side="west")
    for side, seed in (("south", 1), ("west", 2)):
        _mass(bm, uv, te, H, WALL_DEPTH, side=side, seed=seed)
        _walk(bm, uv, te, H, WALL_DEPTH, side=side)
        _parapet(bm, uv, te, H, side=side)
        _steps_up(bm, uv, te, 1, WALL_DEPTH, side, WALK_Z + 0.1, top + 0.1, 3)


def build_bronze_bastion(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeBastion.svg"""
    tw, td, top = 3.5, 4.5, 5.5
    _mass(bm, uv, -tw, tw, td, top=top, seed=4)
    d0 = PARAPET_IN + PARAPET_T
    cb._box(bm, uv, WALL, (0, -H + (d0 + td - 0.2) / 2, top + 0.05), (2 * tw, td - 0.2 - d0, 0.1))
    # The crest: a low mud-brick wall on the outer edge and five horns of consecration on it.
    cb._box(bm, uv, MUDBRICK, (0, -H + PARAPET_IN + PARAPET_T / 2, top + LOW / 2), (2 * tw, PARAPET_T, LOW))
    for x in (-2.8, -1.4, 0.0, 1.4, 2.8):
        horns(bm, uv, x, -H + PARAPET_IN + 0.2, top + LOW, size=0.8, along="x")
    for u0, u1, seed, sgn in ((-H, -tw, 5, -1), (tw, H, 6, 1)):
        _mass(bm, uv, u0, u1, WALL_DEPTH, seed=seed)
        _walk(bm, uv, u0, u1, WALL_DEPTH)
        _parapet(bm, uv, u0, u1)
        _steps_up(bm, uv, sgn * tw, sgn, WALL_DEPTH, "south", WALK_Z + 0.1, top + 0.1, 3)


def build_bronze_gate_approach(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeGateApproach.svg"""
    half, jamb, beam_z = 2.1, 0.6, 3.8
    d0, d1 = PARAPET_IN + PARAPET_T, WALL_DEPTH - 0.2
    for u0, u1, seed in ((-H, -half - jamb, 7), (half + jamb, H, 8)):
        _mass(bm, uv, u0, u1, WALL_DEPTH, seed=seed)
        _walk(bm, uv, u0, u1, WALL_DEPTH)
    # The jambs: dressed conglomerate, full depth and height; the beams pocket into them.
    for sgn in (-1, 1):
        cb._box(bm, uv, CONGLOM, (sgn * (half + jamb / 2), -H + WALL_DEPTH / 2, WALK_Z / 2), (jamb, WALL_DEPTH, WALK_Z))
    for d in (0.25, 1.2, 2.0):
        cb._box(bm, uv, TIMBER, (0, -H + d, beam_z + 0.15), (2 * half + 0.6, 0.3, 0.3))
    # The deck: oak planks over the beams under the breastwork and the walk, jamb to jamb.
    cb._box(bm, uv, TIMBER, (0, -H + (PARAPET_IN + d1) / 2, WALK_Z + 0.05), (2 * (half + jamb), d1 - PARAPET_IN, 0.1))
    _parapet(bm, uv, -H, H)
    # The road: staggered limestone slabs 0.06 m thick through the gate and 5 m in, two cart ruts on them.
    for k in range(5):
        cuts = (-1.8, -0.6, 0.6, 1.8) if k % 2 == 0 else (-1.8, -0.9, 0.9, 1.8)
        for a, b in zip(cuts, cuts[1:]):
            cb._box(bm, uv, "vellum_dim", ((a + b) / 2, -H + k + 0.5, 0.03), (b - a - 0.04, 0.96, 0.06))
    for x in (-0.7, 0.7):
        cb._box(bm, uv, SOOT, (x, -H + 2.5, 0.065), (0.12, 5.0, 0.01))
    # The stele beside the road, its carved face to the road.
    sx, sy = 2.8, -2.6
    cb._box(bm, uv, CONGLOM, (sx, sy, 0.15), (0.5, 0.9, 0.30))
    ek.prism(bm, uv, "vellum_dim", [(-0.30, 0.0), (0.30, 0.0), (0.27, 1.30), (-0.27, 1.30)], 0.15,
             loc=(sx + 0.1, sy, 0.30), along="x")


def build_bronze_lion_gate(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeLionGate.svg (the art bible's Lion Gate, kit-built)."""
    depth, pas, jo = 3.2, 1.3, 2.2
    l0, l1 = 3.74, 4.24
    face = -H + depth                                      # the masses' flat north face
    for u0, u1, seed in ((-H, -jo, 9), (jo, H, 10)):
        _mass(bm, uv, u0, u1, depth, seed=seed, ragged=False)
        _walk(bm, uv, u0, u1, depth)
        _parapet(bm, uv, u0, u1)
    # Jambs, lintel, and the corbelled courses that leave the relieving triangle over it.
    for sgn in (-1, 1):
        cb._box(bm, uv, CONGLOM, (sgn * (pas + jo) / 2, -H + depth / 2, l0 / 2), (jo - pas, depth, l0))
    cb._box(bm, uv, CONGLOM, (0, -H + depth / 2, (l0 + l1) / 2), (2 * jo, depth, l1 - l0))
    for c, g in enumerate((1.2, 0.8, 0.4)):
        z = l1 + 0.32 * c
        for sgn in (-1, 1):
            cb._box(bm, uv, CONGLOM, (sgn * (g + jo) / 2, -H + depth / 2, z + 0.16), (jo - g, depth, 0.32))
    # The lion slab, 0.10 m behind the corbels' face, and its relief in front of it: the altar
    # plinth, the down-tapering column and its capital, two lions (headless, as found).
    ek.prism(bm, uv, "vellum_dim", [(-1.2, 0.0), (1.2, 0.0), (0.0, 0.96)], 0.5, loc=(0, -H + 0.35, l1), along="y")
    cb._box(bm, uv, ASHLAR, (0, -H + 0.06, l1 + 0.07), (0.5, 0.08, 0.14))
    ek.prism(bm, uv, ASHLAR, [(-0.055, 0.14), (0.055, 0.14), (0.08, 0.76), (-0.08, 0.76)], 0.08,
             loc=(0, -H + 0.06, l1), along="y")
    cb._box(bm, uv, ASHLAR, (0, -H + 0.06, l1 + 0.79), (0.22, 0.08, 0.06))
    lion = [(0.35, 0.04), (0.85, 0.04), (0.40, 0.60), (0.26, 0.56)]
    for sgn in (-1, 1):
        prof = lion if sgn > 0 else [(-u, z) for u, z in reversed(lion)]
        ek.prism(bm, uv, ASHLAR, prof, 0.08, loc=(0, -H + 0.06, l1), along="y")
    # Inside: ochre plaster and a painted dado on the west mass; the oak leaves folded back.
    cb._box(bm, uv, WALL, (-(H + jo) / 2, face + 0.02, 1.2), (H - jo, 0.04, 2.4))
    cb._box(bm, uv, FRESCO, (-(H + jo) / 2, face + 0.05, 1.35), (H - jo, 0.02, 0.3))
    for sgn in (-1, 1):
        cb._box(bm, uv, TIMBER, (sgn * (pas + 0.65), face + 0.12, 1.85), (1.3, 0.14, 3.7))
        cb._box(bm, uv, METAL, (sgn * (pas + 1.24), face + 0.2, 1.85), (0.12, 0.02, 3.7))
        for z in (0.5, 1.85, 3.2):
            cb._box(bm, uv, TIMBER, (sgn * (pas + 0.6), face + 0.21, z), (1.1, 0.04, 0.16))
        # A bronze torch ring on the north face, 2.20 m up, the torch in it.
        x = sgn * 2.9
        cb._box(bm, uv, METAL, (x, face + 0.125, 2.2), (0.08, 0.25, 0.08))
        mk.paint(bm, mk.add_cylinder(bm, 0.04, 0.5, loc=(x, face + 0.22, 2.35), segments=6), TIMBER, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.07, 0.18, loc=(x, face + 0.22, 2.69), segments=6, radius2=0.01), RED, uv)
    cb._box(bm, uv, TIMBER, (-4.2, face + 0.35, 0.08), (3.2, 0.16, 0.16))           # the bar, down
    # The stair: fourteen 0.30 m rises west to east up the east mass's north face to the walk.
    x0, steps = 2.75, 14
    tread = (H - x0) / steps
    for k in range(steps):
        top = 0.3 * (k + 1)
        cb._box(bm, uv, ASHLAR, (x0 + tread * (k + 0.5), face + 0.5, top / 2), (tread, 1.0, top))
    # The road: twelve rows of limestone slabs through the cell, joints staggered, two ruts.
    row = 2 * H / 12
    for k in range(12):
        cuts = (-1.6, 0.0, 1.6) if k % 2 == 0 else (-1.6, -0.8, 0.8, 1.6)
        for a, b in zip(cuts, cuts[1:]):
            cb._box(bm, uv, "vellum_dim", ((a + b) / 2, -H + row * (k + 0.5), 0.03), (b - a - 0.04, row - 0.04, 0.06))
    for x in (-0.7, 0.7):
        cb._box(bm, uv, SOOT, (x, 0, 0.065), (0.12, 2 * H, 0.01))
