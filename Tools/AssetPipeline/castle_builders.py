"""
One function per castle/level module — the mesh behind each RoomId entry
in Assets/_Project/Data/Castle/CastleRoomRegistry.asset.

Same authoring convention as builders.py: bottom-flush at local Z=0,
stack mesh_kit/room_kit primitives, paint straight from the pigment
palette. room_kit.room_shell() gives every enclosed chamber its floor and
four walls, with an archway on all four sides (see _shell); each
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
# Wall height above the floor slab's top face, per zone. Sized against the
# 1.8m standard human documented in docs/4-systems/scale.md: the tightest
# zone (Crypt) still leaves a full body's worth of headroom, and each ring
# outward is grander than the one inside it.
ZONE_HEIGHT = {
    "CurtainWall": 5.2,
    "OuterBailey": 3.6,
    "InnerWard": 4.0,
    "Keep": 4.6,
    "Crypt": 3.0,
}
# Floor per zone, so walking from one ring to the next reads as moving into
# a different part of the building: packed earth in the bailey, pale
# flagstone in the household, dark boards in the keep, black in the crypt.
ZONE_FLOOR = {
    "CurtainWall": "ash",
    "OuterBailey": "ash_hi",
    "InnerWard": "vellum_faint",
    "Keep": "line",
    "Crypt": "bone_black",
}
ZONE_ACCENT = {
    "CurtainWall": "verdigris_lo",
    "OuterBailey": "madder",
    "InnerWard": "verdigris",
    "Keep": "orpiment",
    "Crypt": "lapis",
}

# The curtain-wall pieces: not rooms, so they have no walkway to keep clear.
WALL_BUILDERS = {
    "build_wall_straight", "build_wall_corner", "build_bastion",
    "build_drawbridge", "build_gatehouse_module",
}

STONE = "iron"
MORTAR = "ash"
PLASTER = "vellum_dim"
TIMBER = "oak"
METAL = "bronze"
DEEP = "bone_black"
HIDE = "leather"

UP = None  # no rotation


def _shell(bm, uv, zone, stone=STONE, trim=None, floor=None):
    """An enclosed room's floor and four walls, with an archway on every
    side — no per-room door list. The generator places modules on a grid
    without consulting their geometry, so any room that opens on only
    some sides will sooner or later sit archway-to-blank-wall against its
    neighbour. Four openings everywhere makes every 4-adjacency a real
    connection; the sides that end up facing nothing are sealed at
    placement time with that zone's door plug (see _door_plug below and
    ProceduralCastleGenerator.SealOpenArchways).

    `stone`/`trim`/`floor` default to this file's High Medieval tables; the
    other Ages' builders pass their own pigments (docs/plans/era-castle-rooms.md)."""
    h = ZONE_HEIGHT[zone]
    floor_z = rk.room_shell(bm, uv, h, stone, trim=trim or ZONE_ACCENT[zone],
                            floor_pigment=floor or ZONE_FLOOR[zone], door_sides=rk.SIDES)
    # A torch beside two of the four archways, on opposite walls, so every
    # room has some fire and its doors read in the dark. One burns from the
    # start, the other is lit once the castle stirs.
    ow, _ = rk.opening_size(h)
    x = ow / 2 + 0.55
    z = floor_z + min(2.3, h - 0.6)
    _fire("Sconce", x, rk.HALF - rk.WALL_T, z, facing=(0.0, -1.0), lit=0)
    _fire("Sconce", -x, -rk.HALF + rk.WALL_T, z, facing=(0.0, 1.0), lit=1)
    return h, floor_z


def _door_plug(bm, uv, zone, stone=STONE):
    """A plain stone slab that exactly fills one of `zone`'s archways.
    One per zone rather than one for the whole castle because the opening
    is derived from the zone's wall height (room_kit.opening_size), so a
    single plug size would be too small for the Keep and too big for the
    Crypt.

    Authored bottom-flush with the *opening*, not the room: the generator
    positions it at the archway's sill, which is the floor slab's top."""
    ow, oh = rk.opening_size(ZONE_HEIGHT[zone])
    size = (ow + rk.OVERLAP, rk.WALL_T + rk.OVERLAP, oh + rk.OVERLAP)
    mk.paint(bm, mk.add_box(bm, size, loc=(0, 0, size[2] / 2)), stone, uv)


def build_door_plug_outer_bailey(bm, uv):
    _door_plug(bm, uv, "OuterBailey")


def build_door_plug_inner_ward(bm, uv):
    _door_plug(bm, uv, "InnerWard")


def build_door_plug_keep(bm, uv):
    _door_plug(bm, uv, "Keep")


def build_door_plug_crypt(bm, uv):
    _door_plug(bm, uv, "Crypt")


# ── CurtainWall: the wall itself, not an enclosed room ──────────────────
#
# Battlements go only on the side that has a wall, and sit ON the wall top
# (base == wall height). They used to ring the whole cell 0.2 m above the
# top, so three sides of merlons hung in the air over every wall run.

def build_wall_straight(bm, uv):
    h = ZONE_HEIGHT["CurtainWall"]
    rk.wall_run(bm, uv, "south", 0, h, STONE, trim_pigment=ZONE_ACCENT["CurtainWall"])
    # The wall-walk: a timber allure on the wall's inner face, carried on corbels.
    _box(bm, uv, TIMBER, (0, -rk.HALF + rk.WALL_T + 0.55, h - 0.1), (rk.FOOTPRINT - 0.1, 1.1, 0.2))
    for x in (-4.5, -1.5, 1.5, 4.5):
        _box(bm, uv, STONE, (x, -rk.HALF + rk.WALL_T + 0.3, h - 0.55), (0.35, 0.6, 0.7))
    rk.crenellations(bm, uv, h, STONE, size=rk.FOOTPRINT, sides=("south",))
    for x in (-3.0, 3.0):
        _box(bm, uv, DEEP, (x, -rk.HALF + rk.WALL_T + 0.05, h * 0.55), (0.5, 0.12, 1.2))
    _fire("Sconce", 0.0, -rk.HALF + rk.WALL_T, 2.9, facing=(0.0, 1.0), lit=0)


def build_wall_corner(bm, uv):
    h = ZONE_HEIGHT["CurtainWall"]
    # south claims the SW corner cube; west is shortened so it stops at
    # south's inner face instead of doubly covering that same cube.
    rk.wall_run(bm, uv, "south", 0, h, STONE, trim_pigment=ZONE_ACCENT["CurtainWall"])
    rk.wall_run(bm, uv, "west", 0, h, STONE, trim_pigment=ZONE_ACCENT["CurtainWall"],
                length=rk.FOOTPRINT - rk.WALL_T, offset=rk.WALL_T / 2)
    # 2.3 in from the cell edge: the drum's trim course is the widest thing
    # on it (radius * 1.05 == 2.205), so anything closer hangs over the neighbour.
    corner = (-rk.HALF + 2.3, -rk.HALF + 2.3)
    top = rk.tower_drum(bm, uv, 2.1, h + 1.4, STONE, loc=(*corner, 0), trim=ZONE_ACCENT["CurtainWall"], segments=10)
    rk.crenellations(bm, uv, top, STONE, size=4.2, center=corner)
    _fire("Beacon", corner[0], corner[1], top, lit=2)


def build_bastion(bm, uv):
    h = ZONE_HEIGHT["CurtainWall"]
    rk.wall_run(bm, uv, "south", 0, h * 0.82, STONE, trim_pigment=ZONE_ACCENT["CurtainWall"])
    top = rk.tower_drum(bm, uv, 3.6, h + 2.0, STONE, loc=(0, -1.2, 0), trim=ZONE_ACCENT["CurtainWall"], segments=12)
    rk.crenellations(bm, uv, top, STONE, size=6.6, center=(0, -1.2))
    _fire("Beacon", 0.0, -1.2, top, lit=2)
    _fire("Sconce", 0.0, -1.2 + 3.6, 2.9, facing=(0.0, 1.0), lit=0)
    # Arrow loops set into the drum's face (touching it, not floating off it).
    for ang in range(0, 360, 45):
        rad = math.radians(ang)
        mk.paint(bm, mk.add_box(bm, (0.35, 0.25, 0.7),
                                loc=(math.cos(rad) * 3.5, -1.2 + math.sin(rad) * 3.5, top * 0.6)), DEEP, uv)


def build_drawbridge(bm, uv):
    h = ZONE_HEIGHT["CurtainWall"]
    rk.wall_run(bm, uv, "south", 0, h, STONE, opening=(4.2, 3.6), trim_pigment=ZONE_ACCENT["CurtainWall"])
    rk.crenellations(bm, uv, h, STONE, size=rk.FOOTPRINT, sides=("south",))
    # The deck runs inward from the gate rather than out over a moat that
    # isn't modelled: a deck longer than HALF leaves the cell entirely.
    deck_len = 5.0
    _box(bm, uv, TIMBER, (0, -rk.HALF + deck_len / 2, 0.11), (3.8, deck_len, 0.22))
    for x in (-1.7, 1.7):
        # Chains from the deck's inner end up to the gate arch, anchored at both ends.
        mk.paint(bm, mk.add_cylinder(bm, 0.09, 3.4, loc=(x, -rk.HALF + 0.4, 1.9), segments=6), METAL, uv)
        mk.paint(bm, mk.add_box(bm, (0.18, 0.18, 3.6),
                                loc=(x, -rk.HALF + 1.5, 1.7), rot=Euler((math.radians(-35), 0, 0))), METAL, uv)


def build_gatehouse_module(bm, uv):
    h = ZONE_HEIGHT["CurtainWall"] + 1.2
    rk.wall_run(bm, uv, "south", 0, h, STONE, opening=(4.6, 4.2), trim_pigment=ZONE_ACCENT["CurtainWall"])
    for x in (-3.6, 3.6):
        top = rk.tower_drum(bm, uv, 2.0, h + 2.4, STONE, loc=(x, -1.0, 0), trim=ZONE_ACCENT["CurtainWall"], segments=10)
        rk.crenellations(bm, uv, top, STONE, size=3.8, center=(x, -1.0))
        _fire("Sconce", x, -1.0 + 2.0, 2.8, facing=(0.0, 1.0), lit=0)
    # The portcullis, raised: bars hang from the gate lintel, bottoms clear of a walking head.
    for x in (-2.3, -0.8, 0.8, 2.3):
        _box(bm, uv, METAL, (x, -rk.HALF + 0.35, 4.2 - 1.0), (0.18, 0.18, 2.0))
    _box(bm, uv, METAL, (0, -rk.HALF + 0.35, 4.2 - 0.05), (5.0, 0.3, 0.3))


# ── Room layout grammar ─────────────────────────────────────────────────
#
# Every enclosed room has an archway in the middle of each wall, and the
# player (and every guard) walks between them through the middle. So:
#   * a clear cross, |x| < 1.6 or |y| < 1.6, up to 2 m above the floor;
#   * furniture goes in the four corner quadrants, backed against the walls;
#   * anything raised stands on legs or on something grounded.
# validate_in_blender.validate_castle_layout enforces all three at build time.

IN = rk.HALF - rk.WALL_T          # 5.5: the inner face of each wall

# Where loot can sit in the room just built: a table top, a chest lid, a
# shelf, an altar. build_assets collects these per module into
# Assets/_Project/Data/Castle/CastleLootAnchors.json (Blender coordinates,
# Z up); LootPlacementPlanner puts each piece on one instead of scattering
# it across the floor.
LOOT_ANCHORS = []


def _anchor(x, y, z):
    LOOT_ANCHORS.append((round(x, 3), round(y, 3), round(z, 3)))


# Where the module burns a fire (docs/plans/night-atmosphere.md, section 2).
# build_assets writes these per module into
# Assets/_Project/Data/Castle/CastleFireAnchors.json (Blender coordinates,
# Z up); CastleFireAnchorImporter copies them into the room registry and
# CastleFireSpawner lights one FireSource per anchor at runtime.
#   kind:    "Sconce" | "Brazier" | "Hearth" | "Beacon"
#   facing:  (x, y) direction the fire faces; a sconce faces off its wall
#   lit:     alarm state that first lights it, 0 Calm .. 3 HueAndCry
#   holder:  True when the fire brings its own iron (bracket, bowl, basket);
#            False when the room already models it and only the flame is
#            wanted, in which case the point is the flame's base.
FIRE_ANCHORS = []


def _fire(kind, x, y, z, facing=(0.0, 1.0), lit=0, holder=True):
    FIRE_ANCHORS.append({
        "kind": kind,
        "p": [round(x, 3), round(y, 3), round(z, 3)],
        "facing": [round(facing[0], 3), round(facing[1], 3)],
        "lit": lit,
        "holder": holder,
    })
Q0 = 1.8                           # a quadrant starts this far from each centre line


def _box(bm, uv, pigment, center, size):
    rk.paint_box(bm, uv, pigment, center, size, grow_axis="xyz")


def _table(bm, uv, pigment, x, y, fz, w, d, h=0.8, top=0.08):
    """A table on four legs, top at fz + h."""
    _box(bm, uv, pigment, (x, y, fz + h - top / 2), (w, d, top))
    _anchor(x, y, fz + h)
    for sx in (-1, 1):
        for sy in (-1, 1):
            _box(bm, uv, pigment, (x + sx * (w / 2 - 0.1), y + sy * (d / 2 - 0.1), fz + (h - top) / 2),
                 (0.1, 0.1, h - top))


def _bench(bm, uv, x, y, fz, w, d=0.35, along_x=True, pigment=TIMBER):
    sx, sy = (w, d) if along_x else (d, w)
    _box(bm, uv, pigment, (x, y, fz + 0.4), (sx, sy, 0.08))
    for o in (-1, 1):
        if along_x:
            _box(bm, uv, pigment, (x + o * (w / 2 - 0.1), y, fz + 0.18), (0.08, d, 0.36))
        else:
            _box(bm, uv, pigment, (x, y + o * (w / 2 - 0.1), fz + 0.18), (d, 0.08, 0.36))


def _barrel(bm, uv, x, y, fz, r=0.45, h=0.95, pigment=TIMBER):
    mk.paint(bm, mk.add_cylinder(bm, r, h, loc=(x, y, fz + h / 2), segments=10), pigment, uv)


def _banner(bm, uv, side, along, fz, pigment, width=1.2, height=2.2, drop=0.6):
    """A hanging banner flat against a wall's inner face, top `drop` below the wall top zone."""
    t = 0.06
    z = fz + 3.0 - height / 2 - drop + 1.0
    if side == "north":
        _box(bm, uv, pigment, (along, IN - t / 2 + 0.01, z), (width, t, height))
    elif side == "south":
        _box(bm, uv, pigment, (along, -IN + t / 2 - 0.01, z), (width, t, height))
    elif side == "east":
        _box(bm, uv, pigment, (IN - t / 2 + 0.01, along, z), (t, width, height))
    else:
        _box(bm, uv, pigment, (-IN + t / 2 - 0.01, along, z), (t, width, height))


def _shelf(bm, uv, x, y, fz, w, levels=3, along_x=True, depth=0.45, pigment=TIMBER):
    """A standing shelf unit against a wall: two uprights and `levels` boards."""
    h = 0.5 + levels * 0.55
    sx, sy = (w, depth) if along_x else (depth, w)
    for o in (-1, 1):
        if along_x:
            _box(bm, uv, pigment, (x + o * (w / 2 - 0.05), y, fz + h / 2), (0.1, depth, h))
        else:
            _box(bm, uv, pigment, (x, y + o * (w / 2 - 0.05), fz + h / 2), (depth, 0.1, h))
    for i in range(levels):
        _box(bm, uv, pigment, (x, y, fz + 0.35 + i * 0.55), (sx, sy, 0.06))
    # On the top board: the lower ones have 0.5 m under the next board, too
    # little for a chest, and the top one is still within arm's reach.
    _anchor(x, y, fz + 0.35 + (levels - 1) * 0.55 + 0.03)


def _pillar(bm, uv, x, y, fz, h, r=0.3, pigment=STONE):
    mk.paint(bm, mk.add_cylinder(bm, r, h, loc=(x, y, fz + h / 2), segments=8), pigment, uv)


def _brazier(bm, uv, x, y, fz, pigment="madder", metal=METAL):
    mk.paint(bm, mk.add_cylinder(bm, 0.08, 1.0, loc=(x, y, fz + 0.5), segments=6), metal, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.35, 0.25, loc=(x, y, fz + 1.12), segments=8, radius2=0.2), metal, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.25, 0.08, loc=(x, y, fz + 1.27), segments=8), pigment, uv)
    _fire("Brazier", x, y, fz + 1.31, holder=False)


def _chest(bm, uv, x, y, fz, w=1.0, d=0.6, h=0.55, trim=None, body=TIMBER):
    _box(bm, uv, body, (x, y, fz + h / 2), (w, d, h))
    _anchor(x, y, fz + h + (0.05 if trim else 0.0))
    if trim:
        _box(bm, uv, trim, (x, y, fz + h + 0.02), (w, d, 0.05))


def _stair_to_gallery(bm, uv, fz, top, pigment, rail, deck=TIMBER):
    """An L-shaped stair in the south-west quadrant up to a railed gallery
    filling the north-west quadrant, bridged over the walkway above head
    height. The first flight starts at the walkway's edge and climbs west
    along the south wall to a corner landing; the second climbs north along
    the west wall. The foot must face open floor: a stair whose bottom step
    sits against a wall can only be mounted from the side, and the NavMesh
    does not join a stair's side to the floor, so nobody could climb it."""
    steps = 4                        # per flight
    rise = top / (2 * steps)
    x0, x_land = -Q0 - 0.1, -IN + 1.5    # first flight: east (foot) to the landing
    run_x = (x0 - x_land) / steps
    y_south = -IN + 0.75             # centre line of the first flight
    for i in range(steps):
        x = x0 - (i + 0.5) * run_x
        z = fz + rise * (i + 1)
        _box(bm, uv, pigment, (x, y_south, (fz + z) / 2), (run_x, 1.5, z - fz))
    # Corner landing at half height.
    z_mid = fz + rise * steps
    _box(bm, uv, pigment, (-IN + 0.75, -IN + 0.75, (fz + z_mid) / 2), (1.5, 1.5, z_mid - fz))
    # Second flight north along the west wall to the bridge.
    y0, y_top = -IN + 1.5, -Q0 - 0.2
    run_y = (y_top - y0) / steps
    for i in range(steps):
        y = y0 + (i + 0.5) * run_y
        z = z_mid + rise * (i + 1)
        _box(bm, uv, pigment, (-IN + 0.75, y, (fz + z) / 2), (1.5, run_y, z - fz))
    # Bridge over the east-west walkway, then the gallery.
    _box(bm, uv, deck, (-IN + 0.75, 0, fz + top - 0.1), (1.5, 2 * Q0 + 0.4, 0.2))
    _box(bm, uv, deck, (-(IN + Q0) / 2, (IN + Q0) / 2, fz + top - 0.1), (IN - Q0, IN - Q0, 0.2))
    # Posts under the gallery, clear of the walkway.
    for x, y in ((-Q0 - 0.1, Q0 + 0.1), (-Q0 - 0.1, IN - 0.2), (-IN + 0.2, Q0 + 0.1)):
        _box(bm, uv, deck, (x, y, fz + (top - 0.2) / 2), (0.2, 0.2, top - 0.2))
    # Railings along the gallery's open edges, leaving the bridge's end open.
    _box(bm, uv, rail, (-Q0 - 0.05, (IN + Q0) / 2, fz + top + 0.45), (0.08, IN - Q0, 0.9))
    x_gap = -IN + 1.5
    _box(bm, uv, rail, ((x_gap - Q0) / 2, Q0 + 0.05, fz + top + 0.45), (abs(x_gap + Q0), 0.08, 0.9))


# ── OuterBailey: working buildings ──────────────────────────────────────

def build_stable_block(bm, uv):
    h, fz = _shell(bm, uv, "OuterBailey")
    # Two stalls in each northern quadrant, backed against the north wall.
    for sign in (-1, 1):
        for i, cx in enumerate((2.8, 4.6)):
            x = sign * cx
            _box(bm, uv, TIMBER, (x - sign * 0.85, IN - 1.2, fz + 0.7), (0.12, 2.4, 1.4))
            _box(bm, uv, TIMBER, (x, IN - 0.3, fz + 0.35), (1.4, 0.5, 0.7))    # manger
        _box(bm, uv, TIMBER, (sign * (IN - 0.06), IN - 1.2, fz + 0.7), (0.12, 2.4, 1.4))
    # Hay bales stacked in the south-east, a water trough in the south-west.
    for (x, y, z) in ((3.0, -4.6, 0), (4.4, -4.6, 0), (3.7, -4.6, 0.6), (4.6, -3.2, 0)):
        _box(bm, uv, "vellum_dim", (x, y, fz + 0.3 + z), (1.2, 0.8, 0.6))
    _box(bm, uv, STONE, (-4.2, -4.8, fz + 0.3), (2.2, 0.8, 0.6))
    _box(bm, uv, "lapis", (-4.2, -4.8, fz + 0.62), (2.0, 0.6, 0.04))
    _anchor(3.7, -4.6, fz + 1.2)    # on the hay stack
    _anchor(4.6, -3.2, fz + 0.6)


def build_blacksmith_shop(bm, uv):
    h, fz = _shell(bm, uv, "OuterBailey")
    # Forge in the north-west corner with its hood against both walls.
    _box(bm, uv, STONE, (-4.3, 4.3, fz + 0.5), (2.2, 2.2, 1.0))
    _box(bm, uv, "madder", (-4.3, 4.3, fz + 1.02), (1.4, 1.4, 0.06))
    _fire("Hearth", -4.3, 4.3, fz + 1.05, holder=False)
    _box(bm, uv, STONE, (-4.7, 4.7, fz + 2.2), (1.4, 1.4, 2.4))
    # Anvil on its stump in the north-east, quench tub beside it.
    _barrel(bm, uv, 3.2, 3.4, fz, r=0.4, h=0.6)
    _box(bm, uv, METAL, (3.2, 3.4, fz + 0.75), (0.9, 0.35, 0.3))
    _anchor(3.2, 3.4, fz + 0.9)     # on the anvil
    _barrel(bm, uv, 4.6, 4.6, fz, r=0.5, h=0.7, pigment=METAL)
    # Workbench along the south wall, rack of blades on the east wall.
    _table(bm, uv, TIMBER, 3.6, -4.9, fz, 3.0, 1.0, h=0.9)
    _box(bm, uv, TIMBER, (IN - 0.1, -3.4, fz + 1.4), (0.2, 2.6, 0.12))
    for i in range(5):
        _box(bm, uv, METAL, (IN - 0.2, -4.4 + i * 0.5, fz + 1.0), (0.05, 0.08, 1.0))
    _box(bm, uv, "madder", (-4.0, -4.6, fz + 0.4), (1.6, 1.0, 0.8))  # coal heap


def build_barracks_bunk(bm, uv):
    h, fz = _shell(bm, uv, "OuterBailey")
    # A two-tier bunk in each quadrant, head against the east or west wall.
    for sx in (-1, 1):
        for sy in (-1, 1):
            x = sx * (IN - 1.1)
            y = sy * 3.6
            for lvl, z in enumerate((0.45, 1.45)):
                _box(bm, uv, TIMBER, (x, y, fz + z), (2.0, 1.0, 0.12))
                _box(bm, uv, HIDE, (x, y, fz + z + 0.11), (1.9, 0.9, 0.1))
            for px in (-0.95, 0.95):
                for py in (-0.45, 0.45):
                    _box(bm, uv, TIMBER, (x + px, y + py, fz + 0.9), (0.1, 0.1, 1.8))
            _chest(bm, uv, x - sx * 1.5, y, fz, w=0.6, d=0.9, h=0.5)
    _banner(bm, uv, "north", -3.4, fz, ZONE_ACCENT["OuterBailey"])
    _banner(bm, uv, "north", 3.4, fz, ZONE_ACCENT["OuterBailey"])


def build_well_courtyard(bm, uv):
    h, fz = _shell(bm, uv, "OuterBailey")
    # The well in the south-east quadrant, its winch posts on the rim.
    mk.paint(bm, mk.add_cylinder(bm, 1.1, 0.9, loc=(3.4, -3.4, fz + 0.45), segments=12, radius2=1.0), STONE, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.85, 0.05, loc=(3.4, -3.4, fz + 0.87), segments=12), DEEP, uv)
    for dx in (-0.9, 0.9):
        _box(bm, uv, TIMBER, (3.4 + dx, -3.4, fz + 0.9 + 0.8), (0.14, 0.14, 1.6))
    _box(bm, uv, TIMBER, (3.4, -3.4, fz + 0.9 + 1.55), (2.0, 0.14, 0.14))
    # A handcart in the north-west, barrels and troughs round the edges.
    _box(bm, uv, TIMBER, (-3.8, 3.8, fz + 0.75), (2.2, 1.3, 0.35))
    _anchor(-3.8, 3.8, fz + 0.93)   # in the cart
    for dx in (-0.8, 0.8):
        mk.paint(bm, mk.add_cylinder(bm, 0.55, 0.12, loc=(-3.8 + dx, 3.8 - 0.72, fz + 0.55),
                                     rot=Euler((math.radians(90), 0, 0)), segments=10), TIMBER, uv)
    for (x, y) in ((4.6, 4.6), (3.6, 4.8), (-4.8, -4.6)):
        _barrel(bm, uv, x, y, fz)
    _box(bm, uv, STONE, (-3.6, -4.9, fz + 0.3), (2.2, 0.7, 0.6))


def build_storehouse_room(bm, uv):
    h, fz = _shell(bm, uv, "OuterBailey")
    # Shelving along the north and south walls, barrels and crates in the corners.
    for sx in (-1, 1):
        _shelf(bm, uv, sx * 3.7, IN - 0.3, fz, 3.0)
        _shelf(bm, uv, sx * 3.7, -IN + 0.3, fz, 3.0)
    for (x, y) in ((-2.6, 3.6), (-3.6, 3.4), (2.6, -3.6), (3.6, -3.4)):
        _barrel(bm, uv, x, y, fz)
    for (x, y, z) in ((4.5, 3.4, 0), (4.5, 3.4, 0.9), (-4.5, -3.4, 0)):
        _box(bm, uv, TIMBER, (x, y, fz + 0.45 + z), (0.9, 0.9, 0.9))
    _anchor(-4.5, -3.4, fz + 0.9)


# ── InnerWard: the household ────────────────────────────────────────────

def build_great_hall_main(bm, uv):
    h, fz = _shell(bm, uv, "InnerWard")
    # Two long feasting tables broken by the cross aisle, benches either side.
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * 3.65, sy * 3.4
            _table(bm, uv, TIMBER, x, y, fz, 3.5, 1.1)
            _bench(bm, uv, x, y - 0.95, fz, 3.3)
            _bench(bm, uv, x, y + 0.95, fz, 3.3)
    # A runner down the aisle (flat, walkable), pillars in the quadrant corners.
    _box(bm, uv, ZONE_ACCENT["InnerWard"], (0, 0, fz + 0.02), (rk.FOOTPRINT - 1.2, 1.6, 0.04))
    for sx in (-1, 1):
        for sy in (-1, 1):
            _pillar(bm, uv, sx * 1.95, sy * 1.95, fz, h - 0.3, r=0.25)
    for side in ("east", "west"):
        _banner(bm, uv, side, 3.6, fz, ZONE_ACCENT["InnerWard"])
        _banner(bm, uv, side, -3.6, fz, ZONE_ACCENT["InnerWard"])


def build_chapel_room(bm, uv):
    h, fz = _shell(bm, uv, "InnerWard")
    # The altar on a dais in the north-east corner, candles either side.
    _box(bm, uv, STONE, (3.9, 3.9, fz + 0.15), (3.0, 3.0, 0.3))
    _box(bm, uv, STONE, (4.2, 4.2, fz + 0.8), (1.6, 0.9, 1.0))
    _box(bm, uv, "vellum", (4.2, 4.2, fz + 1.32), (1.7, 1.0, 0.04))
    _anchor(4.2, 4.2, fz + 1.34)    # on the altar
    for dx in (-1.0, 1.0):
        mk.paint(bm, mk.add_cylinder(bm, 0.07, 1.1, loc=(4.2 + dx, 3.2, fz + 0.3 + 0.55), segments=6), METAL, uv)
    # Pews in the two southern quadrants, facing north.
    for sx in (-1, 1):
        for y in (-2.4, -3.6, -4.8):
            _bench(bm, uv, sx * 3.6, y, fz, 3.2, d=0.45)
    _banner(bm, uv, "north", 3.6, fz, ZONE_ACCENT["InnerWard"], width=1.6)
    _brazier(bm, uv, -4.6, 4.6, fz, pigment="orpiment")


def build_kitchen_room(bm, uv):
    h, fz = _shell(bm, uv, "InnerWard")
    # The hearth in the north-west corner, a cauldron sitting in it.
    _box(bm, uv, STONE, (-4.3, 4.6, fz + 0.6), (2.4, 1.2, 1.2))
    _box(bm, uv, "madder", (-4.3, 4.4, fz + 1.22), (1.6, 0.6, 0.04))
    _fire("Hearth", -4.3, 4.0, fz + 1.24, holder=False)
    mk.paint(bm, mk.add_cylinder(bm, 0.4, 0.45, loc=(-4.3, 4.4, fz + 1.45), segments=10), DEEP, uv)
    _box(bm, uv, STONE, (-4.3, 5.0, fz + 2.3), (2.0, 0.8, 2.2))
    # Prep tables, north-east and south-east; flour sacks and barrels south-west.
    _table(bm, uv, TIMBER, 3.6, 4.5, fz, 3.0, 1.0, h=0.9)
    _table(bm, uv, TIMBER, 4.5, -3.6, fz, 1.0, 3.0, h=0.9)
    for (x, y) in ((-3.0, -4.6), (-4.1, -4.6), (-4.6, -3.4)):
        _barrel(bm, uv, x, y, fz, r=0.4, h=0.8)
    _shelf(bm, uv, -IN + 0.3, 3.0, fz, 1.8, along_x=False)


def build_guard_room_inner(bm, uv):
    h, fz = _shell(bm, uv, "InnerWard")
    # Weapon racks on the west wall either side of the archway.
    for y in (-3.6, 3.6):
        _box(bm, uv, TIMBER, (-IN + 0.1, y, fz + 1.2), (0.2, 2.4, 0.12))
        for i in range(4):
            _box(bm, uv, METAL, (-IN + 0.15, y - 0.9 + i * 0.6, fz + 0.9), (0.06, 0.12, 1.4))
    # The watch's table and stools, south-east; a cot, north-east.
    _table(bm, uv, TIMBER, 3.6, -3.6, fz, 2.0, 1.2)
    for (x, y) in ((2.2, -3.6), (5.0, -3.6)):
        _barrel(bm, uv, x, y, fz, r=0.25, h=0.45)
    _box(bm, uv, TIMBER, (4.4, 4.0, fz + 0.3), (1.0, 2.2, 0.6))
    _box(bm, uv, HIDE, (4.4, 4.0, fz + 0.64), (0.9, 2.0, 0.08))
    _brazier(bm, uv, 2.4, 2.4, fz)


def build_armoured_courtyard(bm, uv):
    h, fz = _shell(bm, uv, "InnerWard")
    # A practice yard: a straw dummy in each quadrant.
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * 3.6, sy * 3.6
            _box(bm, uv, TIMBER, (x, y, fz + 0.9), (0.18, 0.18, 1.8))
            _box(bm, uv, TIMBER, (x, y, fz + 1.4), (1.1, 0.14, 0.14))
            _box(bm, uv, "orpiment", (x, y, fz + 1.1), (0.55, 0.4, 0.7))
            _box(bm, uv, STONE, (x, y, fz + 0.06), (0.7, 0.7, 0.12))
    _chest(bm, uv, 4.8, 0 + 4.8, fz, w=0.9, d=0.6)
    # Shields hung on the east and west walls.
    for side in ("east", "west"):
        for along in (-3.6, 3.6):
            _banner(bm, uv, side, along, fz, ZONE_ACCENT["InnerWard"], width=1.0, height=1.2)


# ── Keep: the lord's rooms ──────────────────────────────────────────────

def build_throne_room_keep(bm, uv):
    h, fz = _shell(bm, uv, "Keep")
    # The throne on a dais in the north-east corner, turned to face the hall.
    _box(bm, uv, STONE, (3.8, 3.8, fz + 0.2), (3.4, 3.4, 0.4))
    _box(bm, uv, ZONE_ACCENT["Keep"], (4.4, 4.4, fz + 0.4 + 0.45), (1.2, 1.0, 0.9))
    _box(bm, uv, ZONE_ACCENT["Keep"], (4.4, 4.95, fz + 0.4 + 1.3), (1.2, 0.2, 1.8))
    _anchor(3.2, 3.2, fz + 0.4)     # at the foot of the throne, on the dais
    _anchor(2.6, 4.6, fz + 0.4)
    # A carpet from the crossing to the dais (flat), pillars and banners.
    _box(bm, uv, "madder", (1.9, 1.9, fz + 0.02), (1.4, 1.4, 0.04))
    for (x, y) in ((-2.2, 2.2), (-2.2, -2.2), (2.2, -2.2)):
        _pillar(bm, uv, x, y, fz, h - 0.3)
    for side, along in (("north", -3.6), ("west", 3.6), ("east", -3.6), ("south", 3.6)):
        _banner(bm, uv, side, along, fz, ZONE_ACCENT["Keep"])
    _brazier(bm, uv, 2.2, 5.0, fz)
    _brazier(bm, uv, 5.0, 2.2, fz)


def build_treasury_vault(bm, uv):
    h, fz = _shell(bm, uv, "Keep")
    # Strongboxes on the floor in every quadrant, coin heaps between them.
    for sx in (-1, 1):
        for sy in (-1, 1):
            for i, (dx, dy) in enumerate(((2.5, 4.6), (4.0, 4.6), (4.6, 3.0))):
                _chest(bm, uv, sx * dx, sy * dy, fz, w=1.0 if dy > 4 else 0.7, d=0.7 if dy > 4 else 1.0,
                       h=0.6, trim=ZONE_ACCENT["Keep"])
            mk.paint(bm, mk.add_cylinder(bm, 0.5, 0.3, loc=(sx * 3.0, sy * 3.0, fz + 0.15),
                                         segments=10, radius2=0.1), ZONE_ACCENT["Keep"], uv)
    _banner(bm, uv, "north", -3.6, fz, ZONE_ACCENT["Keep"])
    _banner(bm, uv, "south", 3.6, fz, ZONE_ACCENT["Keep"])


def build_royal_bedchamber(bm, uv):
    h, fz = _shell(bm, uv, "Keep")
    # A canopied bed in the north-west corner; the canopy rests on its posts.
    bx, by = -3.9, 3.8
    _box(bm, uv, TIMBER, (bx, by, fz + 0.3), (2.4, 3.0, 0.6))
    _box(bm, uv, "vellum", (bx, by, fz + 0.65), (2.2, 2.8, 0.1))
    _anchor(bx, by + 0.6, fz + 0.7)  # on the bed
    for dx in (-1.1, 1.1):
        for dy in (-1.4, 1.4):
            _box(bm, uv, TIMBER, (bx + dx, by + dy, fz + 1.1), (0.14, 0.14, 2.2))
    _box(bm, uv, ZONE_ACCENT["Keep"], (bx, by, fz + 2.23), (2.5, 3.1, 0.06))
    _chest(bm, uv, bx, by - 1.8, fz, w=1.6, d=0.6, trim=ZONE_ACCENT["Keep"])
    # A wardrobe in the north-east, a washstand in the south-east, a rug.
    _box(bm, uv, TIMBER, (4.4, IN - 0.4, fz + 1.1), (1.8, 0.8, 2.2))
    _table(bm, uv, STONE, 4.8, -3.6, fz, 0.9, 1.4)
    _box(bm, uv, "madder", (-3.6, -3.6, fz + 0.02), (2.6, 2.6, 0.04))


def build_lords_solar(bm, uv):
    h, fz = _shell(bm, uv, "Keep")
    # A writing desk and chair in the north-east, a bookcase on the north wall.
    # The desk stands clear of the bookcase so there is room to walk between them.
    _table(bm, uv, TIMBER, 3.6, 2.9, fz, 2.0, 1.0)
    _box(bm, uv, TIMBER, (3.6, 2.05, fz + 0.25), (0.6, 0.6, 0.5))
    _shelf(bm, uv, 3.8, IN - 0.3, fz, 3.0, levels=3)
    # A hearth on the west wall, two chairs before it; a rug.
    _box(bm, uv, STONE, (-IN + 0.5, 3.8, fz + 0.7), (1.0, 2.4, 1.4))
    _box(bm, uv, "madder", (-IN + 0.9, 3.8, fz + 0.3), (0.3, 1.2, 0.5))
    _fire("Hearth", -IN + 1.2, 3.8, fz + 0.05, facing=(1.0, 0.0), holder=False)
    for y in (2.6, 5.0):
        _box(bm, uv, HIDE, (-3.3, y, fz + 0.3), (0.8, 0.8, 0.6))
    _box(bm, uv, ZONE_ACCENT["Keep"], (-3.6, -3.6, fz + 0.02), (2.6, 2.0, 0.04))
    _brazier(bm, uv, 3.8, -4.4, fz)


def build_keep_stairwell(bm, uv):
    h, fz = _shell(bm, uv, "Keep")
    _stair_to_gallery(bm, uv, fz, 2.6, STONE, METAL)
    # Something to climb for, on the gallery; torches below.
    _chest(bm, uv, -4.3, 4.4, fz + 2.6, w=1.0, d=0.6, trim=ZONE_ACCENT["Keep"])
    _brazier(bm, uv, 3.8, 3.8, fz)
    _brazier(bm, uv, 3.8, -3.8, fz)
    _banner(bm, uv, "east", 3.6, fz, ZONE_ACCENT["Keep"])


# ── Crypt: under the keep ───────────────────────────────────────────────

def _sarcophagus(bm, uv, x, y, fz, along_x=True, body=DEEP, lid=STONE):
    sx, sy = (2.0, 0.9) if along_x else (0.9, 2.0)
    _box(bm, uv, body, (x, y, fz + 0.3), (sx, sy, 0.6))
    _box(bm, uv, lid, (x, y, fz + 0.65), (sx + 0.1, sy + 0.1, 0.12))
    _anchor(x, y, fz + 0.71)


def build_crypt_antechamber(bm, uv):
    h, fz = _shell(bm, uv, "Crypt")
    _sarcophagus(bm, uv, -3.8, 4.2, fz)
    _sarcophagus(bm, uv, 3.8, 4.2, fz)
    for (x, y) in ((-2.4, -2.4), (2.4, -2.4)):
        _brazier(bm, uv, x, y, fz, pigment="lapis")
    _box(bm, uv, ZONE_ACCENT["Crypt"], (0, 0, fz + 0.02), (1.2, rk.FOOTPRINT - 1.2, 0.04))


def build_tomb_corridor(bm, uv):
    h, fz = _shell(bm, uv, "Crypt")
    # Burial niches cut along the east and west walls, in the quadrants.
    for sx in (-1, 1):
        for y in (-4.4, -2.8, 2.8, 4.4):
            for lvl, z in enumerate((0.0, 0.9)):
                _box(bm, uv, STONE, (sx * (IN - 0.5), y, fz + 0.1 + z), (1.0, 1.2, 0.2))
                _box(bm, uv, DEEP, (sx * (IN - 0.5), y, fz + 0.45 + z), (0.9, 1.0, 0.5))
                if lvl == 0 and y > 0:
                    _anchor(sx * (IN - 0.5), y, fz + 0.7)
    _box(bm, uv, ZONE_ACCENT["Crypt"], (0, 0, fz + 0.02), (0.6, rk.FOOTPRINT - 1.2, 0.04))


def build_burial_vault(bm, uv):
    h, fz = _shell(bm, uv, "Crypt")
    # Coffins racked three high on stone shelves in each quadrant, against the north and south walls.
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * 3.7, sy * (IN - 0.5)
            for lvl in range(3):
                z = 0.05 + lvl * 0.75
                _box(bm, uv, STONE, (x, y, fz + z + 0.05), (3.0, 1.0, 0.1))
                _box(bm, uv, DEEP, (x, y, fz + z + 0.35), (2.0, 0.8, 0.5))

            for dx in (-1.45, 1.45):
                _box(bm, uv, STONE, (x + dx, y, fz + 1.2), (0.1, 1.0, 2.4))
            # Grave goods laid on the floor in front of each rack: the shelves are
            # too close together for anything to sit on a coffin.
            _anchor(x, sy * (IN - 1.6), fz)


def build_crypt_chamber_final(bm, uv):
    h, fz = _shell(bm, uv, "Crypt")
    # The rune circle at the crossing is inlaid (flat); the altar holding the
    # castle's best prize stands in the north-east, ringed by braziers.
    mk.paint(bm, mk.add_cylinder(bm, 1.5, 0.04, loc=(0, 0, fz + 0.02), segments=16), ZONE_ACCENT["Crypt"], uv)
    _box(bm, uv, STONE, (3.8, 3.8, fz + 0.15), (3.0, 3.0, 0.3))
    _box(bm, uv, DEEP, (4.0, 4.0, fz + 0.3 + 0.45), (1.6, 0.9, 0.9))
    mk.paint(bm, mk.add_sphere(bm, 0.22, loc=(4.0, 4.0, fz + 1.2 + 0.22)), ZONE_ACCENT["Crypt"], uv)
    _anchor(3.3, 2.9, fz + 0.3)     # on the dais before the altar: the prize here is the castle's biggest
    for (x, y) in ((2.4, 4.8), (4.8, 2.4), (2.4, 2.4)):
        _brazier(bm, uv, x, y, fz + 0.3, pigment="lapis")
    for (x, y) in ((-3.8, 3.8), (-3.8, -3.8), (3.8, -3.8)):
        _sarcophagus(bm, uv, x, y, fz, along_x=(x < 0))


def _stair_to_dais(bm, uv, fz, top, pigment):
    """A short flight up the west wall of the north-west quadrant to a raised
    dais in that corner: for rooms too low for a gallery."""
    steps = 4
    rise = top / steps
    run = 1.4 / steps
    for i in range(steps):
        z = fz + rise * (i + 1)
        _box(bm, uv, pigment, (-4.4, Q0 + 0.1 + i * run + run / 2, (fz + z) / 2), (2.2, run, z - fz))
    _box(bm, uv, pigment, (-4.4, (Q0 + 1.5 + IN) / 2, (fz + fz + top) / 2), (2.2, IN - Q0 - 1.5, top))


def build_crypt_stairwell(bm, uv):
    h, fz = _shell(bm, uv, "Crypt")
    _stair_to_dais(bm, uv, fz, 1.0, DEEP)
    _sarcophagus(bm, uv, -4.4, 4.5, fz + 1.0)
    _brazier(bm, uv, 3.8, 3.8, fz, pigment="lapis")
    _brazier(bm, uv, 3.8, -3.8, fz, pigment="lapis")
