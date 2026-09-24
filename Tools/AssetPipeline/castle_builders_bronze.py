"""
Bronze Age: a Mycenaean citadel, c. 1200 BC. The BronzeAge castle set, one function per module in
asset_specs.BRONZE_CASTLE_SPECS. See docs/plans/era-castle-rooms.md for the plan
of every piece (set-pieces by quadrant, loot anchors, the High Medieval room each
one stands in for) and for where the kit's rules override the art bible.

Low and warm: ochre plaster, mud-brick, bronze, red-painted columns that taper
downwards, horns of consecration on the parapets, and everything dry enough to burn.

Same convention as castle_builders.py (the High Medieval set): the zone heights
and archways are shared, rooms start from _shell and keep the clear cross,
furniture goes in the corner quadrants, nothing floats, and every room
registers at least two loot anchors on furniture. Wall pieces put their wall on
the local south side (corner: south + west) and their gate in the south wall.
"""
import math

from mathutils import Euler

import castle_builders as cb
import era_kit as ek
import mesh_kit as mk
import room_kit as rk

IN = cb.IN
Q0 = cb.Q0
ZONE_HEIGHT = cb.ZONE_HEIGHT

WALL = "bronze"          # ochre lime plaster, the room walls
CURTAIN = "vellum_dim"   # cyclopean limestone, the curtain wall
MUDBRICK = "leather"
TIMBER = "oak"
METAL = "bronze"
RED = "madder"           # haematite-red columns, hearth fire
FRESCO = "verdigris_lo"  # stand-in for fresco blue (the atlas has no blue)
LINEN = "vellum"
SOOT = "bone_black"
GOLD = "orpiment"        # value only
GRAIN_JAR = "leather"
OIL_JAR = "madder"       # the oil burns: madder is the fire colour

ZONE_FLOOR = {"OuterBailey": "leather", "InnerWard": "ash_hi", "Keep": "vellum_faint", "Crypt": "bone_black"}
ZONE_TRIM = {"CurtainWall": "leather", "OuterBailey": "leather", "InnerWard": "verdigris_lo", "Keep": "madder", "Crypt": "vellum_dim"}


def _shell(bm, uv, zone):
    """This Age's walls, floor and trim on the shared room shell."""
    return cb._shell(bm, uv, zone, stone=WALL, trim=ZONE_TRIM[zone], floor=ZONE_FLOOR[zone])


# ── Door plugs: the High Medieval size (same archway), this Age's stone ──

def build_bronze_door_plug_outer_bailey(bm, uv):
    cb._door_plug(bm, uv, "OuterBailey", stone=WALL)


def build_bronze_door_plug_inner_ward(bm, uv):
    cb._door_plug(bm, uv, "InnerWard", stone=WALL)


def build_bronze_door_plug_keep(bm, uv):
    cb._door_plug(bm, uv, "Keep", stone=WALL)


def build_bronze_door_plug_crypt(bm, uv):
    cb._door_plug(bm, uv, "Crypt", stone=WALL)


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


# ── Keep: the wanax's quarters ──────────────────────────────────────────

def _clay_bench(bm, uv, x, y, fz, w, d=0.5, along_x=True, pigment=None):
    """A plastered clay bench built into the wall, 0.4 m high."""
    size = (w, d, 0.4) if along_x else (d, w, 0.4)
    cb._box(bm, uv, pigment or LINEN, (x, y, fz + 0.2), size)
    cb._anchor(x, y, fz + 0.4)


def _tripod(bm, uv, x, y, fz, h=0.9, r=0.3):
    """A bronze tripod cauldron: a bowl on three splayed legs."""
    for k in range(3):
        ang = k * 2 * math.pi / 3
        lx, ly = x + math.cos(ang) * r * 0.7, y + math.sin(ang) * r * 0.7
        mk.paint(bm, mk.add_cylinder(bm, 0.03, h, loc=(lx, ly, fz + h / 2), segments=6), METAL, uv)
    mk.paint(bm, mk.add_cylinder(bm, r, 0.28, loc=(x, y, fz + h + 0.1), segments=10, radius2=r * 0.7), METAL, uv)
    cb._anchor(x, y, fz + h + 0.24)


def _fresco_band(bm, uv, fz, sides=rk.SIDES, bottom=1.4, top=3.0):
    """A painted procession band on each wall section either side of its
    archway, with a haematite border above it."""
    for side in sides:
        for along in (-3.4, 3.4):
            ek.wall_panel(bm, uv, FRESCO, side, along, fz + bottom, 4.1, top - bottom)
            ek.wall_panel(bm, uv, RED, side, along, fz + top, 4.1, 0.18)


def build_bronze_megaron(bm, uv):
    h, fz = _shell(bm, uv, "Keep")
    # The hearth: a flat painted ring at the crossing (the art bible's raised
    # rim would sit across the walkway, so it is inlaid instead).
    ek.disc(bm, uv, FRESCO, 0, 0, fz, 1.8, thickness=0.04, segments=16)
    ek.disc(bm, uv, RED, 0, 0, fz, 1.5, thickness=0.07, segments=16)
    ek.disc(bm, uv, SOOT, 0, 0, fz, 1.05, thickness=0.1, segments=16)
    # Four red columns on the 5 m square round the hearth, tapering downward.
    for sx in (-1, 1):
        for sy in (-1, 1):
            ek.tapered_column(bm, uv, RED, SOOT, sx * 2.5, sy * 2.5, fz, h)
    # The throne against the east wall in the NE quadrant, facing west, with
    # painted griffins either side of it.
    tx, ty = IN - 0.45, 3.7
    cb._box(bm, uv, LINEN, (tx, ty, fz + 0.23), (0.7, 1.0, 0.46))
    cb._box(bm, uv, LINEN, (IN - 0.1, ty, fz + 0.9), (0.25, 1.0, 1.8))
    for k, dy in enumerate((-0.35, 0.0, 0.35)):
        mk.paint(bm, mk.add_cylinder(bm, 0.14, 0.25, loc=(IN - 0.1, ty + dy, fz + 1.8 + (0.12 if k == 1 else 0.06)),
                                     rot=Euler((0, math.radians(90), 0)), segments=8), LINEN, uv)
    cb._anchor(tx, ty, fz + 0.46)
    cb._box(bm, uv, "vellum_faint", (tx - 0.9, ty, fz + 0.06), (1.2, 1.6, 0.12))   # footstool step
    cb._anchor(tx - 0.9, ty, fz + 0.12)
    for dy in (-1.5, 1.4):
        ek.wall_panel(bm, uv, FRESCO, "east", ty + dy, fz + 0.9, 1.4, 1.1)
    # Clay benches with fleeces along the north and south walls, in the quadrants.
    for sx in (-1, 1):
        _clay_bench(bm, uv, sx * 3.1, IN - 0.25, fz, 2.4)
        _clay_bench(bm, uv, sx * 3.6, -IN + 0.25, fz, 3.2)
    # Two tripods by the hearth, three offering tables.
    _tripod(bm, uv, -3.5, 2.0, fz)
    _tripod(bm, uv, 3.3, -2.0, fz)
    cb._table(bm, uv, "vellum_faint", -4.6, -3.6, fz, 0.6, 0.6, h=0.7)
    cb._table(bm, uv, "vellum_faint", -2.4, -4.3, fz, 0.6, 0.6, h=0.7)
    cb._table(bm, uv, "vellum_faint", 4.6, -3.6, fz, 0.6, 0.6, h=0.7)
    _fresco_band(bm, uv, fz, sides=("north", "south", "west"))
