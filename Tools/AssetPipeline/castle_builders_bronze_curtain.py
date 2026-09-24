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


def _mass(bm, uv, u0, u1, depth, top=WALK_Z, side="south", seed=0, bottom=0.0, pigment=CURTAIN):
    """A cyclopean run from u0 to u1, `depth` thick from the cell edge, bottom..top."""
    for b in cy.blocks(u0, u1, top, seed=seed, bottom=bottom):
        d0, d1 = b["inset"], depth + b["dj"]
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
