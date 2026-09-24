"""
Bronze Age: a Mycenaean citadel, c. 1200 BC. The BronzeAge castle set's shared base: its palette, zone tables,
room_shell and door plugs. The pieces themselves are in
castle_builders_bronze_<zone>.py (curtain, bailey, ward, keep, crypt), each built to
its room sheet in docs/art/rooms/. Plan: docs/plans/era-castle-rooms.md.

Low and warm: ochre plaster, mud-brick, bronze, red-painted columns that taper
downwards, horns of consecration on the parapets, and everything dry enough to burn.

Every piece follows the same convention as castle_builders.py (the High Medieval set): the zone heights
and archways are shared, rooms start from room_shell and keep the clear cross,
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


def room_shell(bm, uv, zone):
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

