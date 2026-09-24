"""
Late Medieval: a Burgundian fortress, c. 1450. The LateMedieval castle set's shared base: its palette, zone tables,
room_shell and door plugs. The pieces themselves are in
castle_builders_late_<zone>.py (curtain, bailey, ward, keep, crypt), each built to
its room sheet in docs/art/rooms/. Plan: docs/plans/era-castle-rooms.md.

Fortresses within fortresses: dressed sandstone lined with brick, machicolations,
keyhole gun-loops, conical tower roofs, pavises and the first guns.

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


def room_shell(bm, uv, zone):
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


# ── Furniture every Late zone uses ──────────────────────────────────────


def candle_stand(bm, uv, x, y, fz, h=1.5):
    """A standing iron pricket: foot, stem, drip pan, a candle and its flame."""
    mk.paint(bm, mk.add_cylinder(bm, 0.2, 0.05, loc=(x, y, fz + 0.025), segments=8), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.03, h, loc=(x, y, fz + h / 2), segments=6), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.12, 0.03, loc=(x, y, fz + h + 0.015), segments=8), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.035, 0.18, loc=(x, y, fz + h + 0.12), segments=6), LINEN, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.025, 0.08, loc=(x, y, fz + h + 0.25), segments=6, radius2=0.005), CLOTH, uv)


def wall_hearth(bm, uv, side_x, y, fz, width=1.3, depth=0.9, mouth=1.3, hood_top=4.1):
    """A hooded hearth against the west wall (side_x = -IN) centred at y: jambs, a
    mantel, a sandstone hood tapering back to the wall, a sooted back, a fire."""
    x_face = side_x + depth
    for o in (-1, 1):
        cb._box(bm, uv, WALL, ((side_x + x_face) / 2, y + o * (width / 2 - 0.09), fz + mouth / 2), (depth, 0.18, mouth))
    cb._box(bm, uv, WALL, ((side_x + x_face + 0.1) / 2, y, fz + mouth + 0.125), (depth + 0.1, width + 0.2, 0.25))
    ek.prism(bm, uv, WALL, [(side_x, mouth + 0.25), (x_face + 0.1, mouth + 0.25), (side_x + 0.3, hood_top),
                            (side_x, hood_top)], width, loc=(0, y, fz), along="y")
    ek.wall_panel(bm, uv, SOOT, "west", y, fz, width - 0.36, mouth, depth=0.04)
    for o in (-1, 1):
        cb._box(bm, uv, IRON, (side_x + 0.45, y + o * 0.25, fz + 0.12), (0.5, 0.06, 0.24))
    mk.paint(bm, mk.add_cylinder(bm, 0.25, 0.55, loc=(side_x + 0.4, y, fz + 0.275), segments=6, radius2=0.03), CLOTH, uv)


def iron_chest(bm, uv, x, y, fz, w=1.2, d=0.7, h=0.7, along_x=True):
    """An oak chest bound in blackened iron: lid band, three straps over it, a lock
    plate; registers its loot anchor on the lid."""
    sx, sy = (w, d) if along_x else (d, w)
    cb._chest(bm, uv, x, y, fz, w=sx, d=sy, h=h, trim=IRON, body=TIMBER)
    for o in (-0.375, 0.0, 0.375):
        if along_x:
            cb._box(bm, uv, IRON, (x + o * w, y, fz + h / 2 + 0.01), (0.06, d + 0.02, h + 0.02))
        else:
            cb._box(bm, uv, IRON, (x, y + o * w, fz + h / 2 + 0.01), (d + 0.02, 0.06, h + 0.02))


def gun_loop(bm, uv, side, along, bottom):
    """A keyhole gun-loop on a wall: a dressed surround, a 0.90 m slit and a 0.20 m round
    hole under it, both soot-dark."""
    ek.wall_panel(bm, uv, "vellum_faint", side, along, bottom, 0.6, 1.3, depth=0.06)
    ek.wall_panel(bm, uv, SOOT, side, along, bottom + 0.3, 0.1, 0.9, depth=0.04, proud=0.05)
    off = IN - 0.09
    loc = {"north": (along, off), "south": (along, -off), "east": (off, along), "west": (-off, along)}[side]
    rot = Euler((math.radians(90), 0, 0)) if side in ("north", "south") else Euler((0, math.radians(90), 0))
    mk.paint(bm, mk.add_cylinder(bm, 0.1, 0.04, loc=(*loc, bottom + 0.25), rot=rot, segments=8), SOOT, uv)
