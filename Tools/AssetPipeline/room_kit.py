"""
Shared bmesh helpers for Plunderspell castle/level modules, layered on
mesh_kit's primitives the same way builders.py stacks them for props.

Runs only inside Blender's Python (imports bpy/bmesh/mathutils via mesh_kit).

Every module is authored bottom-flush (lowest vertex at local Z=0) and
centred on the local X/Y origin, sized to fill the 12m grid cell
ProceduralCastleGenerator places modules on (see `cellSize` in
ProceduralCastleGenerator.cs) exactly. FOOTPRINT == cellSize deliberately:
a deliberate seam smaller than the cell leaves a visible gap at every
module boundary, which is what issue 19 was. Adjacent modules' outer walls
are therefore coincident by design, and the OVERLAP growth below (a few
millimetres, well under the grid's precision) is what keeps them from
z-fighting rather than a whole-module inset.

validate_in_blender enforces this: any castle module whose XY bounding box
reaches past +/-(FOOTPRINT/2) (plus a tolerance for OVERLAP) fails the
build, so a module can never silently grow back out of its cell.

Doors are left as gaps in a wall run built from up to three box segments
(two jambs + a lintel over the opening) rather than a boolean cut — same
"stack primitives" approach as every prop builder, and it keeps the result
trivially manifold and quad-dominant.
"""
import math

from mathutils import Euler

import mesh_kit as mk

FOOTPRINT = 12.0          # module footprint == ProceduralCastleGenerator cellSize
HALF = FOOTPRINT / 2
WALL_T = 0.5
FLOOR_T = 0.3
TRIM_BAND = 0.45           # capstone course along the top of a wall run

# validate_in_blender flags any two unconnected vertices that sit at the
# same position (see its _duplicate_vert_count) — which every flush butt
# joint between two independently-built boxes produces (floor-to-wall,
# wall-to-trim, jamb-to-lintel, furniture-to-floor). Every box placed
# through this module grows by OVERLAP in whichever axis it's stacked
# along, centred on its original position, so it interpenetrates its
# neighbour by a few millimetres instead of sitting exactly flush — same
# fix the prop builders get for free because their primitives are curved
# or angled enough to never land on an exact shared vertex position.
OVERLAP = 0.02

SIDES = ("north", "south", "east", "west")
_AXIS = {"north": "x", "south": "x", "east": "y", "west": "y"}


def door(height, width=2.6, height_frac=0.72):
    """A door/archway opening sized relative to a wall's clear height."""
    return (width, min(height * height_frac, height - 0.3))


def opening_size(height, trim=True):
    """The archway size a wall of clear `height` actually ends up with —
    `door()` clamped the way wall_run clamps it. A door plug built from
    this exactly fills the hole; built from `door()` alone it would be too
    tall for any wall whose trim band eats into the opening."""
    ow, oh = door(height)
    stone_h = height - TRIM_BAND if trim else height
    return ow, min(oh, stone_h - 0.25)


def paint_box(bm, uv, pigment, loc, size, grow_axis="z"):
    """add_box + paint, grown by OVERLAP along `grow_axis` (or all three
    if "xyz") so this box overlaps whatever it's flush-stacked against
    instead of sharing an exact vertex position with it. `loc`/other axes
    are unaffected — the box just grows symmetrically about its centre."""
    axes = "xyz" if grow_axis == "xyz" else grow_axis
    grown = tuple(s + OVERLAP if a in axes else s for s, a in zip(size, "xyz"))
    mk.paint(bm, mk.add_box(bm, grown, loc=loc), pigment, uv)


def floor_slab(bm, uv, pigment, size=FOOTPRINT, thickness=FLOOR_T, base_z=0.0):
    paint_box(bm, uv, pigment, (0, 0, base_z + thickness / 2), (size, size, thickness))
    return thickness


def _wall_center(side):
    if side == "north":
        return (0, HALF - WALL_T / 2)
    if side == "south":
        return (0, -HALF + WALL_T / 2)
    if side == "east":
        return (HALF - WALL_T / 2, 0)
    return (-HALF + WALL_T / 2, 0)  # west


def wall_run(bm, uv, side, base_z, height, pigment, opening=None,
             trim_pigment=None, length=FOOTPRINT, offset=0.0):
    """One straight wall on `side`, from base_z to base_z + height.
    `opening` is a (width, height) gap centred on the wall (from `door()`),
    or None for a solid run. `trim_pigment` caps the top TRIM_BAND in a
    second colour so the wall reads as coursed stone, not one flat slab.
    `offset` shifts the whole run along its own axis (positive = toward
    that side's "clockwise" end) — use it to pull a shortened run's open
    end back to one side only, e.g. away from a corner another wall
    already claims, instead of shrinking symmetrically from both ends.
    """
    cx, cy = _wall_center(side)
    along = _AXIS[side]
    stone_h = height - TRIM_BAND if trim_pigment else height

    def seg(center_along, seg_len, seg_z, seg_h, pig):
        if seg_len <= 0.02 or seg_h <= 0.02:
            return
        center_along += offset
        if along == "x":
            loc, size = (center_along, cy, seg_z), (seg_len, WALL_T, seg_h)
        else:
            loc, size = (cx, center_along, seg_z), (WALL_T, seg_len, seg_h)
        paint_box(bm, uv, pig, loc, size, grow_axis="xyz")

    if opening is None:
        seg(0, length, base_z + stone_h / 2, stone_h, pigment)
    else:
        ow, oh = opening
        oh = min(oh, stone_h - 0.25)
        jamb_len = (length - ow) / 2
        seg(-(ow / 2 + jamb_len / 2), jamb_len, base_z + stone_h / 2, stone_h, pigment)
        seg(ow / 2 + jamb_len / 2, jamb_len, base_z + stone_h / 2, stone_h, pigment)
        if stone_h > oh:
            seg(0, ow, base_z + oh + (stone_h - oh) / 2, stone_h - oh, pigment)

    if trim_pigment:
        seg(0, length, base_z + stone_h + TRIM_BAND / 2, TRIM_BAND, trim_pigment)


def room_shell(bm, uv, height, stone, trim=None, floor_pigment=None,
                door_sides=(), base_z=0.0, footprint=FOOTPRINT):
    """Floor + four walls, with an archway on every side named in
    `door_sides` and a solid wall everywhere else. Returns the Z the
    floor sits on top of (== base_z + FLOOR_T), i.e. where furniture
    should be placed."""
    floor_slab(bm, uv, floor_pigment or stone, size=footprint, thickness=FLOOR_T, base_z=base_z)
    wall_base = base_z + FLOOR_T
    # North/south run the full footprint and claim all four corners;
    # east/west stop short at those walls' inner faces instead of also
    # running full-length, or every corner would be double-covered by two
    # exactly congruent box regions — a real overlap, not a flush touch,
    # which OVERLAP growth (paint_box) cannot resolve since growing two
    # already-identical corners by the same amount keeps them identical.
    for side in SIDES:
        opening = door(height) if side in door_sides else None
        side_len = footprint if side in ("north", "south") else footprint - 2 * WALL_T
        wall_run(bm, uv, side, wall_base, height, stone, opening=opening,
                 trim_pigment=trim, length=side_len)
    return wall_base


def crenellations(bm, uv, base_z, pigment, size=FOOTPRINT, merlon=0.9, gap=0.7, height=0.7, inset=0.15,
                   center=(0.0, 0.0), sides=SIDES):
    """A ring of battlement teeth (merlons) around a flat roof edge of
    `size`, centred on `center` — pass the same (x, y) used to place the
    tower/wall this caps, or every ring renders at the room origin, and
    two rings capping two different towers land exactly on top of each
    other (identical, fully-coincident geometry, not just a touching one)."""
    cx0, cy0 = center
    half = size / 2 - inset
    step = merlon + gap
    n = max(1, int((size - inset * 2) // step))
    start = -half + merlon / 2
    # Only the sides that actually have a wall under them: a one-sided wall
    # run capped on all four sides left three rows of merlons in mid-air.
    for side in sides:
        # north/south and east/west run the same `start` formula, so
        # without a stagger every side's first tooth lands on exactly the
        # same corner point as its neighbour's first tooth (two boxes
        # sharing one exact vertex, not just interpenetrating like
        # paint_box's growth already handles). Offsetting east/west by
        # half a step keeps the ring's spacing but staggers its phase so
        # the four sides' teeth never land on the same corner.
        side_start = start + (step / 2 if side in ("east", "west") else 0)
        for i in range(n):
            c = side_start + i * step
            if c + merlon / 2 > half:
                continue
            if side == "north":
                loc = (cx0 + c, cy0 + half - WALL_T / 4, base_z + height / 2)
            elif side == "south":
                loc = (cx0 + c, cy0 - half + WALL_T / 4, base_z + height / 2)
            elif side == "east":
                loc = (cx0 + half - WALL_T / 4, cy0 + c, base_z + height / 2)
            else:
                loc = (cx0 - half + WALL_T / 4, cy0 + c, base_z + height / 2)
            size3 = (merlon, WALL_T * 0.5, height) if side in ("north", "south") else (WALL_T * 0.5, merlon, height)
            paint_box(bm, uv, pigment, loc, size3, grow_axis="xyz")


def stair_core(bm, uv, radius, height, pigment, loc=(0, 0, 0), steps=14, rail_pigment=None):
    """A spiral newel stair: a solid core cylinder plus a helical ramp of
    thin wedge-ish treads (small boxes rotated and stacked). Distinct
    silhouette compared to a room built purely from boxes."""
    core_r = radius * 0.28
    mk.paint(bm, mk.add_cylinder(bm, core_r, height, loc=(loc[0], loc[1], loc[2] + height / 2), segments=8), pigment, uv)
    tread_len = radius - core_r
    for i in range(steps):
        t = i / steps
        ang = t * math.pi * 2.4
        z = loc[2] + 0.05 + t * (height - 0.1)
        cx = loc[0] + math.cos(ang) * (core_r + tread_len / 2)
        cy = loc[1] + math.sin(ang) * (core_r + tread_len / 2)
        rot = Euler((0, 0, ang))
        mk.paint(bm, mk.add_box(bm, (tread_len, radius * 0.62, 0.09), loc=(cx, cy, z), rot=rot), pigment, uv)
    if rail_pigment:
        mk.paint(bm, mk.add_cylinder(bm, 0.03, height, loc=(loc[0], loc[1], loc[2] + height / 2), segments=6), rail_pigment, uv)


def tower_drum(bm, uv, radius, height, pigment, loc=(0, 0, 0), trim=None, segments=10):
    stone_h = height - TRIM_BAND if trim else height
    mk.paint(bm, mk.add_cylinder(bm, radius, stone_h, loc=(loc[0], loc[1], loc[2] + stone_h / 2), segments=segments), pigment, uv)
    if trim:
        mk.paint(bm, mk.add_cylinder(bm, radius * 1.05, TRIM_BAND, loc=(loc[0], loc[1], loc[2] + stone_h + TRIM_BAND / 2), segments=segments), trim, uv)
    return stone_h + (TRIM_BAND if trim else 0)
