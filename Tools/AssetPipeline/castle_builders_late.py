"""
Late Medieval: a Burgundian fortress, c. 1450. The LateMedieval castle set, one function per module in
asset_specs.LATE_CASTLE_SPECS. See docs/plans/era-castle-rooms.md for the plan
of every piece (set-pieces by quadrant, loot anchors, the High Medieval room each
one stands in for) and for where the kit's rules override the art bible.

Fortresses within fortresses: dressed sandstone lined with brick, machicolations,
keyhole gun-loops, conical tower roofs, pavises and the first guns.

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

WALL = "vellum_dim"      # dressed sandstone
CURTAIN = "vellum_dim"
BRICK = "leather"        # brick lining
TIMBER = "oak"
IRON = "line"            # blackened iron
STEEL = "iron"           # bright plate, gun barrels
TAPESTRY = "verdigris_lo"
CLOTH = "madder"         # cloth of estate, hangings
LINEN = "vellum"
SOOT = "bone_black"
GOLD = "orpiment"        # value only
ROOF = "leather"         # clay tile on the conical roofs
# Late curtain-wall pieces keep their outer face this far inside the cell
# edge, so a machicolated parapet can project outward and still stay in the
# cell. All five share the line, so the wall stays continuous.
FACE_INSET = 0.45

ZONE_FLOOR = {"OuterBailey": "ash_hi", "InnerWard": "vellum_faint", "Keep": "oak", "Crypt": "bone_black"}
ZONE_TRIM = {"CurtainWall": "leather", "OuterBailey": "leather", "InnerWard": "verdigris_lo", "Keep": "madder", "Crypt": "ash"}


def _shell(bm, uv, zone):
    """This Age's walls, floor and trim on the shared room shell."""
    return cb._shell(bm, uv, zone, stone=WALL, trim=ZONE_TRIM[zone], floor=ZONE_FLOOR[zone])


# ── Door plugs: the High Medieval size (same archway), this Age's stone ──

def build_late_door_plug_outer_bailey(bm, uv):
    cb._door_plug(bm, uv, "OuterBailey", stone=WALL)


def build_late_door_plug_inner_ward(bm, uv):
    cb._door_plug(bm, uv, "InnerWard", stone=WALL)


def build_late_door_plug_keep(bm, uv):
    cb._door_plug(bm, uv, "Keep", stone=WALL)


def build_late_door_plug_crypt(bm, uv):
    cb._door_plug(bm, uv, "Crypt", stone=WALL)
