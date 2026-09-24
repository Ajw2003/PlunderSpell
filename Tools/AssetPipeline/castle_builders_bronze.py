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


# The tombs under the citadel are built of limestone, not plastered mud-brick.
ZONE_WALL = {"Crypt": CURTAIN}


def room_shell(bm, uv, zone):
    """This Age's walls, floor and trim on the shared room shell."""
    return cb._shell(bm, uv, zone, stone=ZONE_WALL.get(zone, WALL), trim=ZONE_TRIM[zone], floor=ZONE_FLOOR[zone])


# ── Door plugs: the High Medieval size (same archway), this Age's stone ──

def build_bronze_door_plug_outer_bailey(bm, uv):
    cb._door_plug(bm, uv, "OuterBailey", stone=WALL)


def build_bronze_door_plug_inner_ward(bm, uv):
    cb._door_plug(bm, uv, "InnerWard", stone=WALL)


def build_bronze_door_plug_keep(bm, uv):
    cb._door_plug(bm, uv, "Keep", stone=WALL)


def build_bronze_door_plug_crypt(bm, uv):
    cb._door_plug(bm, uv, "Crypt", stone=ZONE_WALL["Crypt"])


# ── Furniture shared by the Bronze Age rooms ────────────────────────────
# Each registers its loot anchor (where it has one) the way castle_builders'
# _table/_chest do, so a room that places one gets the anchor its sheet shows.

def clay_bench(bm, uv, x, y, fz, w, d=0.5, along_x=True, pigment=None):
    """A plastered clay bench built into the wall, 0.4 m high."""
    size = (w, d, 0.4) if along_x else (d, w, 0.4)
    cb._box(bm, uv, pigment or LINEN, (x, y, fz + 0.2), size)
    cb._anchor(x, y, fz + 0.4)


def tripod(bm, uv, x, y, fz, h=0.9, r=0.3):
    """A bronze tripod cauldron: a bowl on three splayed legs."""
    for k in range(3):
        ang = k * 2 * math.pi / 3
        lx, ly = x + math.cos(ang) * r * 0.7, y + math.sin(ang) * r * 0.7
        mk.paint(bm, mk.add_cylinder(bm, 0.03, h, loc=(lx, ly, fz + h / 2), segments=6), METAL, uv)
    mk.paint(bm, mk.add_cylinder(bm, r, 0.28, loc=(x, y, fz + h + 0.1), segments=10, radius2=r * 0.7), METAL, uv)
    cb._anchor(x, y, fz + h + 0.24)


def fresco_band(bm, uv, fz, sides=rk.SIDES, bottom=1.4, top=3.0):
    """A painted procession band on each wall section either side of its
    archway, with a haematite border above it."""
    for side in sides:
        for along in (-3.4, 3.4):
            ek.wall_panel(bm, uv, FRESCO, side, along, fz + bottom, 4.1, top - bottom)
            ek.wall_panel(bm, uv, RED, side, along, fz + top, 4.1, 0.18)


def offering_table(bm, uv, x, y, fz, w=0.6, d=0.6, h=0.7, pigment="vellum_faint"):
    """A small painted offering table on four legs; anchor on its top."""
    cb._table(bm, uv, pigment, x, y, fz, w, d, h=h)


def pithos(bm, uv, x, y, z, height=1.7, belly=1.0, pigment=GRAIN_JAR, lid=False, segments=6):
    """A man-high storage jar standing on z: foot, ovoid belly, rolled rim, one
    rope band at the shoulder, and for oil a stone lid disc. A six-sided
    profile by default: a magazine holds two dozen of these."""
    r, m = belly / 2, belly * 0.25
    profile = [(0.001, 0.0), (r * 0.5, 0.0), (r, height * 0.42), (r * 0.72, height * 0.8),
               (m, height * 0.94), (m * 1.15, height), (0.001, height)]
    mk.paint(bm, mk.add_lathe(bm, profile, segments=segments, loc=(x, y, z)), pigment, uv)
    mk.paint(bm, mk.add_cylinder(bm, r * 0.93, 0.05, loc=(x, y, z + height * 0.62), segments=segments), SOOT, uv)
    if lid:
        mk.paint(bm, mk.add_cylinder(bm, m * 1.2, 0.06, loc=(x, y, z + height + 0.03), segments=segments),
                 "vellum_dim", uv)


def amphora(bm, uv, x, y, z, height=0.62, belly=0.34, pigment=GRAIN_JAR):
    """A pointed-foot amphora standing on z (in a rack or a ring stand)."""
    ek.jar(bm, uv, pigment, x, y, z, height, belly, mouth=belly * 0.35, segments=8)


def larnax(bm, uv, x, y, fz, along_x=True, w=1.6, d=0.6, h=0.55, pigment=GRAIN_JAR):
    """A painted clay chest-coffin on four short legs with a gabled lid; the
    anchor is on the lid's ridge."""
    legs = 0.12
    sx, sy = (w, d) if along_x else (d, w)
    for ox in (-1, 1):
        for oy in (-1, 1):
            cb._box(bm, uv, pigment, (x + ox * (sx / 2 - 0.08), y + oy * (sy / 2 - 0.08), fz + legs / 2), (0.12, 0.12, legs))
    cb._box(bm, uv, pigment, (x, y, fz + legs + h / 2), (sx, sy, h))
    cb._box(bm, uv, RED, (x, y, fz + legs + h * 0.6), (sx + 0.01, sy + 0.01, 0.06))     # painted band
    top = fz + legs + h
    if along_x:
        ek.prism(bm, uv, pigment, [(-sy / 2 - 0.03, 0), (sy / 2 + 0.03, 0), (0, 0.2)], sx + 0.06, loc=(x, y, top), along="x")
    else:
        ek.prism(bm, uv, pigment, [(-sx / 2 - 0.03, 0), (sx / 2 + 0.03, 0), (0, 0.2)], sy + 0.06, loc=(x, y, top), along="y")
    cb._anchor(x, y, top + 0.2)


def horns(bm, uv, x, y, z, size=0.8, along="x", pigment=None):
    """Horns of consecration on z (no anchor)."""
    ek.horns_of_consecration(bm, uv, pigment or LINEN, x, y, z, size=size, along=along)


def ingot_stack(bm, uv, x, y, fz, w, d, layers):
    """Oxhide ingots stacked crosswise on a timber pallet w × d × 0.12 m, the
    ingots 0.56 × 0.36 × 0.06 m (alternate layers 0.52 × 0.38, so no two rows
    ever touch face to face); the anchor sits on top of the stack."""
    cb._box(bm, uv, TIMBER, (x, y, fz + 0.06), (w, d, 0.12))
    nx, ny = int(w / 0.62), int(d / 0.42)
    for k in range(layers):
        z = fz + 0.12 + k * 0.06 + 0.03
        for j in range(nx):
            for i in range(ny):
                cb._box(bm, uv, METAL, (x - w / 2 + 0.33 + j * 0.62, y - d / 2 + 0.24 + i * 0.42, z),
                        (0.56 if k % 2 == 0 else 0.52, 0.36 if k % 2 == 0 else 0.38, 0.06))
    cb._anchor(x, y, fz + 0.12 + layers * 0.06)
