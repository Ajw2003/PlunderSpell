"""
Late Medieval CurtainWall pieces: LateBarbican, LateWallStraight, LateWallCorner, LateBastion, LateDrawbridge.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/LateMedieval/<Key>.json,
drawing: docs/art/rooms/concept/LateMedieval/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_late.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_late import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


# ── CurtainWall: machicolated sandstone ────────────────────────────────
#
# Every Late wall piece keeps its outer face FACE_INSET (0.45 m) inside the
# cell edge; the corbels carry the parapet back out to the cell line, so the
# machicolated crown stays inside the cell and neighbouring pieces meet. A run is
# described along its wall (u) and inward from the cell edge (d), as in the
# Bronze curtain file. The numbers match the sheets' _late_wall.py.

H = rk.HALF
FACE = FACE_INSET
MASS = 1.6
WALK_Z = 4.0
FRIEZE = (3.0, 3.4)
CORBEL = (3.4, 3.7, 4.0)
CORBEL_PITCH, CORBEL_W = 0.9, 0.3
PARAPET = (0.05, 0.33)
LOW_TOP, MERLON_TOP = 4.6, 5.2
MERLON_PITCH, MERLON_W = 1.3, 0.8
TIMBER_WALK = (2.05, 3.05, 3.9)
LOOP_Z = 1.0


def _at(side, u, d, z):
    return (u, -H + d, z) if side == "south" else (-H + d, u, z)


def _size(side, su, sd, sz):
    return (su, sd, sz) if side == "south" else (sd, su, sz)


def _centres(u0, u1, pitch):
    n = int((u1 - u0 + 1e-6) // pitch)
    mid = (u0 + u1) / 2
    return [mid - (n - 1) * pitch / 2 + k * pitch for k in range(n)]


def _box(bm, uv, pigment, side, u0, u1, d0, d1, z0, z1):
    cb._box(bm, uv, pigment, _at(side, (u0 + u1) / 2, (d0 + d1) / 2, (z0 + z1) / 2),
            _size(side, u1 - u0, d1 - d0, z1 - z0))


def keyhole_loop(bm, uv, side, u, z0=LOOP_Z, d=FACE):
    """A keyhole gun-loop on an outer face at d: a soot slit over a round port."""
    _box(bm, uv, SOOT, side, u - 0.05, u + 0.05, d - 0.03, d + 0.01, z0 + 0.3, z0 + 1.2)
    rot = Euler((math.radians(90), 0, 0)) if side == "south" else Euler((0, math.radians(90), 0))
    mk.paint(bm, mk.add_cylinder(bm, 0.1, 0.04, loc=_at(side, u, d - 0.01, z0 + 0.15), rot=rot, segments=8), SOOT, uv)


def crown(bm, uv, u0, u1, side="south", base=0.0, d_face=FACE):
    """The machicolated crown of a run: brick frieze on the face, two-stage corbels, the
    parapet carried out on them with merlons, all `base` higher (a tower's crown)."""
    _box(bm, uv, BRICK, side, u0, u1, d_face - 0.03, d_face + 0.01, base + FRIEZE[0], base + FRIEZE[1])
    for u in _centres(u0, u1, CORBEL_PITCH):
        _box(bm, uv, WALL, side, u - CORBEL_W / 2, u + CORBEL_W / 2, d_face - 0.2, d_face + 0.02,
             base + CORBEL[0], base + CORBEL[1])
        _box(bm, uv, WALL, side, u - CORBEL_W / 2 - 0.03, u + CORBEL_W / 2 + 0.03, PARAPET[0], d_face + 0.04,
             base + CORBEL[1], base + CORBEL[2])
    _box(bm, uv, WALL, side, u0, u1, PARAPET[0], PARAPET[1], base + CORBEL[2], base + LOW_TOP)
    for u in _centres(u0, u1, MERLON_PITCH):
        _box(bm, uv, WALL, side, u - MERLON_W / 2, u + MERLON_W / 2, PARAPET[0], PARAPET[1], base + LOW_TOP,
             base + MERLON_TOP)


def late_run(bm, uv, u0, u1, side="south", loops=(), timber=True):
    """A standard Late run from u0 to u1: the sandstone mass, its crown, keyhole loops and
    the timber walk on stone corbels along the inner face."""
    _box(bm, uv, WALL, side, u0, u1, FACE, FACE + MASS, 0.0, WALK_Z)
    crown(bm, uv, u0, u1, side)
    for u in loops:
        keyhole_loop(bm, uv, side, u)
    if timber:
        d0, d1, under = TIMBER_WALK
        _box(bm, uv, TIMBER, side, u0, u1, d0, d1, under, WALK_Z)
        for u in _centres(u0, u1, 1.5):
            _box(bm, uv, WALL, side, u - 0.15, u + 0.15, d0 - 0.02, d0 + 0.4, under - 0.5, under)


def build_late_wall_straight(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateWallStraight.svg"""
    late_run(bm, uv, -H, H, loops=(-3.6, 0.0, 3.6))
