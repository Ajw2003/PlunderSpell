"""
Age of Powder: a Habsburg palace-fortress, c. 1620. The AgeOfPowder castle set, one function per module in
asset_specs.POWDER_CASTLE_SPECS. See docs/plans/era-castle-rooms.md for the plan
of every piece (set-pieces by quadrant, loot anchors, the High Medieval room each
one stands in for) and for where the kit's rules override the art bible.

Lime plaster, black walnut, blued steel, glass by the acre, gilt everywhere, and black
powder under the ballroom. The walls are low, sloped and angular, built to take cannon.

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

WALL = "vellum"          # lime plaster
CURTAIN = "vellum_faint" # rampart stone
EARTH = "ash_hi"         # the rampart's earth fill and terreplein
BRICK = "leather"
WALNUT = "line"          # black walnut: furniture, frames, wainscot
OAK = "oak"              # parquet, rough timber, carriages
STEEL = "iron"           # blued steel, cannon
GLASS = "verdigris_lo"   # forest window glass
CLOTH = "madder"         # murrey livery, upholstery
LINEN = "vellum"
POWDER = "bone_black"
GILT = "orpiment"        # gilt frames and plate: in this Age the gilt is the plunder

ZONE_FLOOR = {"OuterBailey": "ash_hi", "InnerWard": "vellum_faint", "Keep": "oak", "Crypt": "bone_black"}
ZONE_TRIM = {"CurtainWall": "vellum_dim", "OuterBailey": "oak", "InnerWard": "line", "Keep": "orpiment", "Crypt": "iron"}


def _shell(bm, uv, zone):
    """This Age's walls, floor and trim on the shared room shell."""
    return cb._shell(bm, uv, zone, stone=WALL, trim=ZONE_TRIM[zone], floor=ZONE_FLOOR[zone])


# ── Door plugs: the High Medieval size (same archway), this Age's stone ──

def build_powder_door_plug_outer_bailey(bm, uv):
    cb._door_plug(bm, uv, "OuterBailey", stone=WALL)


def build_powder_door_plug_inner_ward(bm, uv):
    cb._door_plug(bm, uv, "InnerWard", stone=WALL)


def build_powder_door_plug_keep(bm, uv):
    cb._door_plug(bm, uv, "Keep", stone=WALL)


def build_powder_door_plug_crypt(bm, uv):
    cb._door_plug(bm, uv, "Crypt", stone=WALL)
