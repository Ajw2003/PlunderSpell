"""
One function per castle/level module — the mesh behind each RoomId entry
in Assets/_Project/Data/Castle/CastleRoomRegistry.asset.

Same authoring convention as builders.py: bottom-flush at local Z=0,
stack mesh_kit/room_kit primitives, paint straight from the pigment
palette. room_kit.room_shell() gives every enclosed chamber its floor and
four walls (with an archway wherever `door_sides` names a side); each
function then adds a small set-piece so the room reads as its own place
rather than a bare box, the same way a weapon builder's silhouette comes
from 3-4 stacked primitives rather than one blob.

The five CurtainWall modules are not enclosed rooms — they're the wall
itself (rampart, corner, bastion, drawbridge, gatehouse) — so they use
room_kit's wall/tower/crenellation primitives directly instead of
room_shell.
"""
import math

from mathutils import Euler

import mesh_kit as mk
import room_kit as rk

# Per-zone shell height and accent pigment (banners, trim, highlights) —
# not sourced from the C#; this preview's own reading of the moodboard's
# era-accent idea (palette.ERA_ACCENT) applied to the five castle zones.
ZONE_HEIGHT = {
    "CurtainWall": 5.2,
    "OuterBailey": 3.2,
    "InnerWard": 3.8,
    "Keep": 4.6,
    "Crypt": 2.6,
}
ZONE_ACCENT = {
    "CurtainWall": "verdigris_lo",
    "OuterBailey": "madder",
    "InnerWard": "verdigris",
    "Keep": "orpiment",
    "Crypt": "lapis",
}

STONE = "iron"
MORTAR = "ash"
PLASTER = "vellum_dim"
TIMBER = "oak"
METAL = "bronze"
DEEP = "bone_black"
HIDE = "leather"

UP = None  # no rotation


def _shell(bm, uv, zone, door_sides):
    h = ZONE_HEIGHT[zone]
    floor_z = rk.room_shell(bm, uv, h, STONE, trim=PLASTER, floor_pigment=MORTAR, door_sides=door_sides)
    return h, floor_z


# ── CurtainWall: the wall itself, not an enclosed room ──────────────────

def build_wall_straight(bm, uv):
    h = ZONE_HEIGHT["CurtainWall"]
    rk.wall_run(bm, uv, "south", 0, h, STONE, trim_pigment=ZONE_ACCENT["CurtainWall"])
    mk.paint(bm, mk.add_box(bm, (rk.FOOTPRINT, 1.6, 0.2), loc=(0, -rk.HALF + 1.2, h + 0.1)), MORTAR, uv)
    rk.crenellations(bm, uv, h + 0.2, STONE, size=rk.FOOTPRINT)
    for x in (-3.0, 3.0):
        mk.paint(bm, mk.add_box(bm, (0.5, 0.3, 1.2), loc=(x, -rk.HALF + 0.6, h * 0.55)), DEEP, uv)


def build_wall_corner(bm, uv):
    h = ZONE_HEIGHT["CurtainWall"]
    # south claims the SW corner cube; west is shortened so it stops at
    # south's inner face instead of doubly covering that same cube (see
    # room_kit.room_shell's note on why full-length + full-length overlaps).
    rk.wall_run(bm, uv, "south", 0, h, STONE, trim_pigment=ZONE_ACCENT["CurtainWall"])
    rk.wall_run(bm, uv, "west", 0, h, STONE, trim_pigment=ZONE_ACCENT["CurtainWall"],
                length=rk.FOOTPRINT - rk.WALL_T, offset=rk.WALL_T / 2)
    corner = (-rk.HALF + 1.0, -rk.HALF + 1.0)
    rk.tower_drum(bm, uv, 2.1, h + 1.4, STONE, loc=(*corner, 0), trim=ZONE_ACCENT["CurtainWall"], segments=10)
    rk.crenellations(bm, uv, h + 1.4 + 0.2, STONE, size=4.2, center=corner)


def build_bastion(bm, uv):
    h = ZONE_HEIGHT["CurtainWall"]
    rk.wall_run(bm, uv, "south", 0, h * 0.82, STONE, trim_pigment=ZONE_ACCENT["CurtainWall"])
    top = rk.tower_drum(bm, uv, 3.6, h + 2.0, STONE, loc=(0, -1.2, 0), trim=ZONE_ACCENT["CurtainWall"], segments=12)
    rk.crenellations(bm, uv, top + 0.2, STONE, size=6.6, center=(0, -1.2))
    for ang in range(0, 360, 45):
        rad = math.radians(ang)
        mk.paint(bm, mk.add_box(bm, (0.35, 0.25, 0.7), loc=(math.cos(rad) * 3.0, -1.2 + math.sin(rad) * 3.0, top * 0.6)), DEEP, uv)


def build_drawbridge(bm, uv):
    h = ZONE_HEIGHT["CurtainWall"]
    rk.wall_run(bm, uv, "south", 0, h, STONE, opening=(4.2, 3.6), trim_pigment=ZONE_ACCENT["CurtainWall"])
    rk.crenellations(bm, uv, h + 0.2, STONE, size=rk.FOOTPRINT)
    deck_len = rk.HALF + 3.5
    mk.paint(bm, mk.add_box(bm, (3.8, deck_len, 0.22), loc=(0, -rk.HALF - deck_len / 2 + 0.4, 0.11)), TIMBER, uv)
    for x in (-1.7, 1.7):
        mk.paint(bm, mk.add_cylinder(bm, 0.09, h * 0.9, loc=(x, -rk.HALF + 0.3, h * 0.45), segments=6), METAL, uv)
        mk.paint(bm, mk.add_box(bm, (0.18, 0.18, 3.2), loc=(x, -rk.HALF + 1.6, h * 0.75), rot=Euler((math.radians(35), 0, 0))), METAL, uv)


def build_gatehouse_module(bm, uv):
    h = ZONE_HEIGHT["CurtainWall"] + 1.2
    rk.wall_run(bm, uv, "south", 0, h, STONE, opening=(4.6, 4.2), trim_pigment=ZONE_ACCENT["CurtainWall"])
    for x in (-3.6, 3.6):
        top = rk.tower_drum(bm, uv, 2.0, h + 2.4, STONE, loc=(x, -1.0, 0), trim=ZONE_ACCENT["CurtainWall"], segments=10)
        rk.crenellations(bm, uv, top + 0.2, STONE, size=3.8, center=(x, -1.0))
    for x in (-2.3, -0.8, 0.8, 2.3):
        mk.paint(bm, mk.add_box(bm, (0.18, 0.18, 4.0), loc=(x, -rk.HALF + 0.35, 2.0)), METAL, uv)
    mk.paint(bm, mk.add_box(bm, (5.0, 0.3, 0.4), loc=(0, -rk.HALF + 0.35, 4.05)), METAL, uv)


# ── OuterBailey ──────────────────────────────────────────────────────────

def build_stable_block(bm, uv):
    h, fz = _shell(bm, uv, "OuterBailey", ("south",))
    for i, x in enumerate((-3.6, -1.2, 1.2, 3.6)):
        mk.paint(bm, mk.add_box(bm, (1.8, 3.2, 1.4), loc=(x, 1.0, fz + 0.7)), TIMBER, uv)
        mk.paint(bm, mk.add_box(bm, (0.12, 3.2, 0.9), loc=(x + 0.9, 1.0, fz + 0.45)), TIMBER, uv)
    mk.paint(bm, mk.add_box(bm, (rk.FOOTPRINT * 0.9, 1.6, 0.35), loc=(0, -2.6, fz + 0.18)), HIDE, uv)


def build_blacksmith_shop(bm, uv):
    h, fz = _shell(bm, uv, "OuterBailey", ("south", "east"))
    mk.paint(bm, mk.add_cylinder(bm, 1.1, 1.0, loc=(-2.6, -2.6, fz + 0.5), segments=10), DEEP, uv)
    mk.paint(bm, mk.add_box(bm, (0.3, 0.3, 1.8), loc=(-2.6, -2.6, fz + 1.9)), STONE, uv)
    mk.paint(bm, mk.add_box(bm, (1.3, 0.5, 0.55), loc=(0.6, -2.2, fz + 0.5)), STONE, uv)
    mk.paint(bm, mk.add_box(bm, (0.5, 0.5, 0.55), loc=(0.6, -2.2, fz + 1.05)), METAL, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.05, 0.6, loc=(2.4, -2.4, fz + 0.9), segments=6), METAL, uv)


def build_barracks_bunk(bm, uv):
    h, fz = _shell(bm, uv, "OuterBailey", ("south", "north"))
    for row in (-2.8, 0.2, 3.2):
        for lvl in (0, 1):
            mk.paint(bm, mk.add_box(bm, (3.6, 1.0, 0.16), loc=(-1.6, row, fz + 0.32 + lvl * 0.9)), TIMBER, uv)
        mk.paint(bm, mk.add_box(bm, (3.6, 0.9, 0.14), loc=(-1.6, row, fz + 0.16)), HIDE, uv)
    mk.paint(bm, mk.add_box(bm, (1.6, 1.6, 0.7), loc=(3.6, 3.4, fz + 0.35)), TIMBER, uv)


def build_well_courtyard(bm, uv):
    h, fz = _shell(bm, uv, "OuterBailey", ("north", "south", "east", "west"))
    mk.paint(bm, mk.add_cylinder(bm, 1.1, 0.9, loc=(0, 0, fz + 0.45), segments=12, radius2=1.0), STONE, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.85, 0.05, loc=(0, 0, fz + 0.87), segments=12), DEEP, uv)
    for x in (-0.95, 0.95):
        mk.paint(bm, mk.add_box(bm, (0.14, 0.14, 1.6), loc=(x, 0, fz + 0.9 + 0.8)), TIMBER, uv)
    mk.paint(bm, mk.add_box(bm, (2.2, 0.14, 0.14), loc=(0, 0, fz + 0.9 + 1.55)), TIMBER, uv)


def build_storehouse_room(bm, uv):
    h, fz = _shell(bm, uv, "OuterBailey", ("south",))
    for i, (x, y) in enumerate([(-3.0, -2.0), (-1.2, -2.0), (0.6, -2.0), (-3.0, 1.5), (-1.2, 1.5)]):
        mk.paint(bm, mk.add_cylinder(bm, 0.55, 0.95, loc=(x, y, fz + 0.475), segments=10), TIMBER, uv)
    for x in (2.6, 3.7):
        mk.paint(bm, mk.add_box(bm, (0.9, 0.9, 0.9), loc=(x, 2.6, fz + 0.45)), TIMBER, uv)


# ── InnerWard ────────────────────────────────────────────────────────────

def build_great_hall_main(bm, uv):
    h, fz = _shell(bm, uv, "InnerWard", ("north", "south", "east", "west"))
    mk.paint(bm, mk.add_box(bm, (rk.FOOTPRINT * 0.6, 1.6, 0.1), loc=(0, 0, fz + 0.05)), ZONE_ACCENT["InnerWard"], uv)
    for side in (-1, 1):
        for i in range(3):
            x = side * 3.6
            y = -3.2 + i * 3.2
            mk.paint(bm, mk.add_cylinder(bm, 0.3, h - 0.3, loc=(x, y, fz + (h - 0.3) / 2), segments=8), STONE, uv)
    mk.paint(bm, mk.add_box(bm, (2.6, 0.6, 1.0), loc=(0, rk.HALF - 1.4, fz + 0.5)), TIMBER, uv)


def build_chapel_room(bm, uv):
    h, fz = _shell(bm, uv, "InnerWard", ("south",))
    mk.paint(bm, mk.add_box(bm, (1.6, 0.6, 1.0), loc=(0, rk.HALF - 1.2, fz + 0.5)), STONE, uv)
    mk.paint(bm, mk.add_box(bm, (0.9, 0.2, 1.2), loc=(0, rk.HALF - 1.2, fz + 1.6)), ZONE_ACCENT["InnerWard"], uv)
    for x in (-2.6, 2.6):
        mk.paint(bm, mk.add_cylinder(bm, 0.08, 1.0, loc=(x, rk.HALF - 1.0, fz + 1.5), segments=6), METAL, uv)
    for row in (-2.6, -1.0, 0.6):
        mk.paint(bm, mk.add_box(bm, (2.4, 0.35, 0.5), loc=(0, row, fz + 0.25)), TIMBER, uv)


def build_kitchen_room(bm, uv):
    h, fz = _shell(bm, uv, "InnerWard", ("north", "west"))
    mk.paint(bm, mk.add_box(bm, (2.4, 1.0, 1.0), loc=(-3.2, -3.2, fz + 0.5)), STONE, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.35, 0.35, loc=(-3.2, -3.2, fz + 1.15), segments=10), DEEP, uv)
    mk.paint(bm, mk.add_box(bm, (2.6, 1.0, 0.1), loc=(2.4, -2.8, fz + 0.9)), TIMBER, uv)
    for x in (1.4, 2.4, 3.4):
        mk.paint(bm, mk.add_cylinder(bm, 0.5, 0.6, loc=(x, 3.0, fz + 0.3), segments=10), TIMBER, uv)


def build_guard_room_inner(bm, uv):
    h, fz = _shell(bm, uv, "InnerWard", ("north", "south"))
    mk.paint(bm, mk.add_box(bm, (0.2, 3.2, 2.0), loc=(-3.6, 0, fz + 1.0)), TIMBER, uv)
    for i in range(4):
        mk.paint(bm, mk.add_box(bm, (0.06, 0.5, 1.4), loc=(-3.55, -1.4 + i * 0.95, fz + 1.3)), METAL, uv)
    mk.paint(bm, mk.add_box(bm, (2.2, 1.0, 0.5), loc=(2.4, -2.6, fz + 0.25)), TIMBER, uv)
    mk.paint(bm, mk.add_box(bm, (1.0, 0.4, 0.9), loc=(2.2, -2.6, fz + 0.7)), STONE, uv)


def build_armoured_courtyard(bm, uv):
    h, fz = _shell(bm, uv, "InnerWard", ("north", "south", "east", "west"))
    mk.paint(bm, mk.add_cylinder(bm, 0.16, 1.7, loc=(0, 0, fz + 0.85), segments=8), TIMBER, uv)
    mk.paint(bm, mk.add_box(bm, (0.5, 0.14, 1.1), loc=(0, 0.05, fz + 1.5)), HIDE, uv)
    for x in (-3.6, 3.6):
        mk.paint(bm, mk.add_box(bm, (0.1, 1.6, 2.2), loc=(x, -3.0, fz + 1.1)), ZONE_ACCENT["InnerWard"], uv)


# ── Keep ─────────────────────────────────────────────────────────────────

def build_throne_room_keep(bm, uv):
    h, fz = _shell(bm, uv, "Keep", ("south",))
    mk.paint(bm, mk.add_box(bm, (2.2, 1.4, 0.3), loc=(0, rk.HALF - 2.0, fz + 0.15)), STONE, uv)
    mk.paint(bm, mk.add_box(bm, (1.2, 0.8, 1.1), loc=(0, rk.HALF - 1.9, fz + 0.85)), ZONE_ACCENT["Keep"], uv)
    mk.paint(bm, mk.add_box(bm, (1.2, 0.2, 1.8), loc=(0, rk.HALF - 1.55, fz + 1.7)), ZONE_ACCENT["Keep"], uv)
    for side in (-1, 1):
        for i in range(2):
            mk.paint(bm, mk.add_cylinder(bm, 0.28, h - 0.5, loc=(side * 3.8, -2.6 + i * 3.8, fz + (h - 0.5) / 2), segments=8), STONE, uv)


def build_treasury_vault(bm, uv):
    h, fz = _shell(bm, uv, "Keep", ("south",))
    for i, (x, y) in enumerate([(-2.6, -2.4), (-1.0, -2.4), (0.6, -2.4), (-2.6, -0.6), (-1.0, -0.6)]):
        rk.paint_box(bm, uv, TIMBER, (x, y, fz + 0.3 + (i % 2) * 0.1), (1.0, 0.75, 0.6))
        rk.paint_box(bm, uv, ZONE_ACCENT["Keep"], (x, y, fz + 0.63 + (i % 2) * 0.1), (1.0, 0.75, 0.06))
    for x in (2.2, 3.2):
        mk.paint(bm, mk.add_sphere(bm, 0.22, loc=(x, 2.4, fz + 0.22)), ZONE_ACCENT["Keep"], uv)


def build_royal_bedchamber(bm, uv):
    h, fz = _shell(bm, uv, "Keep", ("south",))
    mk.paint(bm, mk.add_box(bm, (2.4, 3.2, 0.5), loc=(-1.6, 0.4, fz + 0.25)), TIMBER, uv)
    for x in (-2.6, -0.6):
        for y in (-1.0, 1.8):
            mk.paint(bm, mk.add_box(bm, (0.16, 0.16, 1.9), loc=(x, y, fz + 0.95)), TIMBER, uv)
    mk.paint(bm, mk.add_box(bm, (2.6, 3.4, 0.06), loc=(-1.6, 0.4, fz + 1.9)), ZONE_ACCENT["Keep"], uv)
    mk.paint(bm, mk.add_box(bm, (1.6, 0.6, 0.5), loc=(3.0, -2.6, fz + 0.25)), STONE, uv)


def build_lords_solar(bm, uv):
    h, fz = _shell(bm, uv, "Keep", ("south",))
    mk.paint(bm, mk.add_box(bm, (1.8, 0.8, 0.75), loc=(-2.6, -2.6, fz + 0.375)), TIMBER, uv)
    mk.paint(bm, mk.add_box(bm, (0.5, 0.5, 0.75), loc=(-3.2, -1.6, fz + 0.375)), TIMBER, uv)
    mk.paint(bm, mk.add_box(bm, (2.0, 0.9, 0.1), loc=(2.4, 2.4, fz + 0.05)), ZONE_ACCENT["Keep"], uv)
    for x in (1.6, 3.2):
        mk.paint(bm, mk.add_cylinder(bm, 0.4, 1.4, loc=(x, 2.4, fz + 0.7), segments=10), STONE, uv)


def build_keep_stairwell(bm, uv):
    h, fz = _shell(bm, uv, "Keep", ("north", "south"), )
    rk.stair_core(bm, uv, 2.6, h, STONE, loc=(0, 0, fz), steps=16, rail_pigment=METAL)


# ── Crypt ────────────────────────────────────────────────────────────────

def build_crypt_antechamber(bm, uv):
    h, fz = _shell(bm, uv, "Crypt", ("south",))
    mk.paint(bm, mk.add_box(bm, (2.0, 0.9, 0.55), loc=(0, 0, fz + 0.275)), DEEP, uv)
    mk.paint(bm, mk.add_box(bm, (2.1, 1.0, 0.14), loc=(0, 0, fz + 0.62)), STONE, uv)
    for x in (-3.4, 3.4):
        mk.paint(bm, mk.add_cylinder(bm, 0.08, 1.3, loc=(x, -2.4, fz + 0.65), segments=6), METAL, uv)


def build_tomb_corridor(bm, uv):
    h, fz = _shell(bm, uv, "Crypt", ("north", "south"))
    for side in (-1, 1):
        for y in (-3.4, -1.0, 1.4, 3.8):
            mk.paint(bm, mk.add_box(bm, (0.9, 0.5, 0.4), loc=(side * 4.4, y, fz + 0.2)), DEEP, uv)
    mk.paint(bm, mk.add_box(bm, (0.5, rk.FOOTPRINT * 0.7, 0.05), loc=(0, 0, fz + 0.025)), ZONE_ACCENT["Crypt"], uv)


def build_burial_vault(bm, uv):
    h, fz = _shell(bm, uv, "Crypt", ("south",))
    for row in range(3):
        for col in range(2):
            x = -2.4 + col * 4.8
            z = fz + 0.35 + row * 0.7
            mk.paint(bm, mk.add_box(bm, (2.0, 0.8, 0.55), loc=(x, -2.0, z)), DEEP, uv)


def build_crypt_chamber_final(bm, uv):
    h, fz = _shell(bm, uv, "Crypt", ("north", "south", "east", "west"))
    mk.paint(bm, mk.add_cylinder(bm, 2.4, 0.12, loc=(0, 0, fz + 0.06), segments=16), ZONE_ACCENT["Crypt"], uv)
    mk.paint(bm, mk.add_box(bm, (1.6, 0.7, 0.6), loc=(0, 0, fz + 0.42)), DEEP, uv)
    mk.paint(bm, mk.add_sphere(bm, 0.22, loc=(0, 0, fz + 0.9)), ZONE_ACCENT["Crypt"], uv)
    for ang in range(0, 360, 60):
        rad = math.radians(ang)
        mk.paint(bm, mk.add_cylinder(bm, 0.07, 1.1, loc=(math.cos(rad) * 3.4, math.sin(rad) * 3.4, fz + 0.55), segments=6), METAL, uv)


def build_crypt_stairwell(bm, uv):
    h, fz = _shell(bm, uv, "Crypt", ("north", "south"))
    rk.stair_core(bm, uv, 2.4, h, DEEP, loc=(0, 0, fz), steps=14, rail_pigment=METAL)
