"""
Bronze Age CurtainWall pieces: BronzeLionGate, BronzeWallStraight, BronzeWallCorner, BronzeBastion, BronzeGateApproach.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/bronze/<Key>.json,
drawing: docs/art/rooms/concept/bronze/<Key>.svg). The sheet is the reference: the
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
# height (art bible, the Lion Gate).

WALL_DEPTH = 2.4
WALK_Z = 4.1
PARAPET_T = 0.4

# Block widths along a run, reused (offset) per course so the joints never
# line up vertically. Fixed lists, not random: the build must be identical
# on every run.
_COURSE_WIDTHS = ((2.2, 1.4, 1.9, 1.1, 2.4, 1.6, 1.4), (1.3, 2.3, 1.2, 2.0, 1.5, 2.1, 1.6))
_COURSE_HEIGHTS = (2.2, 1.9)
_DEPTH_JITTER = (0.0, -0.15, 0.1, -0.05, 0.12, -0.1, 0.05)


def _cyclopean_run(bm, uv, x0, x1, y_out, depth, top=WALK_Z, along="x"):
    """A run of irregular blocks from x0 to x1 (or y0 to y1 when along == "y"),
    its outer face flush on y_out (x_out) and `depth` thick inward, in two
    courses up to `top`. Inner faces are ragged; the outer face stays on the
    cell line so neighbouring modules meet."""
    sign = 1 if y_out < 0 else -1          # inward direction
    z = 0.0
    for c, (widths, ch) in enumerate(zip(_COURSE_WIDTHS, _COURSE_HEIGHTS)):
        ch = ch if c < len(_COURSE_HEIGHTS) - 1 else top - z
        pos, i = x0, 0
        while pos < x1 - 0.05:
            w = min(widths[i % len(widths)], x1 - pos)
            d = depth + _DEPTH_JITTER[(i + c * 3) % len(_DEPTH_JITTER)]
            cu = pos + w / 2
            cv = y_out + sign * d / 2
            if along == "x":
                cb._box(bm, uv, CURTAIN, (cu, cv, z + ch / 2), (w, d, ch))
            else:
                cb._box(bm, uv, CURTAIN, (cv, cu, z + ch / 2), (d, w, ch))
            pos += w
            i += 1
        z += ch


def _mudbrick_parapet(bm, uv, x0, x1, y_out, along="x"):
    """A 1.1 m mud-brick breastwork on the outer edge of the wall-walk, with
    rounded merlons (a box under a half-sunk cylinder) and sling slots."""
    sign = 1 if y_out < 0 else -1
    cv = y_out + sign * PARAPET_T / 2
    length = x1 - x0
    mid = (x0 + x1) / 2
    low_h = 0.5
    if along == "x":
        cb._box(bm, uv, MUDBRICK, (mid, cv, WALK_Z + low_h / 2), (length, PARAPET_T, low_h))
    else:
        cb._box(bm, uv, MUDBRICK, (cv, mid, WALK_Z + low_h / 2), (PARAPET_T, length, low_h))
    step = 1.2
    n = int(length // step)
    start = mid - (n - 1) * step / 2
    for k in range(n):
        u = start + k * step
        box_z = WALK_Z + low_h + 0.15
        cyl_rot = Euler((math.radians(90), 0, 0)) if along == "x" else Euler((0, math.radians(90), 0))
        if along == "x":
            cb._box(bm, uv, MUDBRICK, (u, cv, box_z), (0.6, PARAPET_T, 0.3))
            mk.paint(bm, mk.add_cylinder(bm, 0.3, PARAPET_T, loc=(u, cv, WALK_Z + 0.8), rot=cyl_rot,
                                         segments=8), MUDBRICK, uv)
            if k < n - 1:          # sling slots between merlons, never past the run's end
                cb._box(bm, uv, SOOT, (u + step / 2, cv + sign * (PARAPET_T / 2 + 0.02), WALK_Z + 0.3),
                        (0.12, 0.06, 0.4))
        else:
            cb._box(bm, uv, MUDBRICK, (cv, u, box_z), (PARAPET_T, 0.6, 0.3))
            mk.paint(bm, mk.add_cylinder(bm, 0.3, PARAPET_T, loc=(cv, u, WALK_Z + 0.8), rot=cyl_rot,
                                         segments=8), MUDBRICK, uv)
            if k < n - 1:
                cb._box(bm, uv, SOOT, (cv + sign * (PARAPET_T / 2 + 0.02), u + step / 2, WALK_Z + 0.3),
                        (0.06, 0.12, 0.4))


def _wall_walk(bm, uv, x0, x1, y_out, depth, along="x"):
    """The plastered walk on top of the mass, inside the parapet."""
    sign = 1 if y_out < 0 else -1
    walk_d = depth - PARAPET_T - 0.2
    cv = y_out + sign * (PARAPET_T + walk_d / 2)
    mid, length = (x0 + x1) / 2, x1 - x0
    size = (length, walk_d, 0.1) if along == "x" else (walk_d, length, 0.1)
    loc = (mid, cv, WALK_Z + 0.05) if along == "x" else (cv, mid, WALK_Z + 0.05)
    cb._box(bm, uv, "vellum_faint", loc, size)


def build_bronze_wall_straight(bm, uv):
    _cyclopean_run(bm, uv, -rk.HALF, rk.HALF, -rk.HALF, WALL_DEPTH)
    _wall_walk(bm, uv, -rk.HALF, rk.HALF, -rk.HALF, WALL_DEPTH)
    _mudbrick_parapet(bm, uv, -rk.HALF, rk.HALF, -rk.HALF)
