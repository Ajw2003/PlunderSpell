"""
The outer bailey's furniture (docs/plans/night-atmosphere.md, section 4):
dressing laid over a straight curtain-wall cell, and the yards that fill the
courtyard cells carved out of the interior.

A curtain dressing shares its cell with the wall module, in the same frame as
castle_builders.build_wall_straight: the wall stands on the south edge
(y -6 .. -5.5), the bailey strip is everything north of it. Every dressing:
  * keeps its props within PROP_DEPTH of the wall's inner face, leaving the
    rest of the strip (more than 7 m) as a clear lane, so the ring stays
    walkable and a portal can open anywhere in it;
  * lays the same cobbled path across the cell, so the ring reads as one loop;
  * leaves x = 0 on the wall face clear for the wall's own torch;
  * carries its fire anchors: a yard brazier on the path's wall side.
The ground under a curtain cell is the scene's ground plane at z = 0.

A courtyard fills a whole 12 m cell whose neighbours are rooms; its archways
are plugged (ProceduralCastleGenerator.SealOpenArchways), so it is seen over
the walls and lit for the fog, not walked through.

All pigments come from palette.py; orpiment (treasure) and madder (alarm,
fire, blood) are left out on purpose.
"""
import math

from mathutils import Euler

import castle_builders as cb
import mesh_kit as mk
import room_kit as rk

TIMBER = "oak"
HAY = "vellum_dim"
SACK = "leather"
IRON = "line"
STONE = "iron"
COBBLE = "vellum_faint"
MUD = "ash"
GREEN = "verdigris_lo"
WATER = "bone_black"

WALL_FACE = -rk.HALF + rk.WALL_T      # -5.5, the wall's inner face
PROP_DEPTH = 3.8                       # props stay south of WALL_FACE + PROP_DEPTH
PATH_Y = (-1.2, 1.8)                   # the cobbled path across the cell


def _box(bm, uv, pigment, center, size):
    rk.paint_box(bm, uv, pigment, center, size, grow_axis="xyz")


def _cyl(bm, uv, pigment, r, h, loc, rot=None, segments=8, r2=None):
    mk.paint(bm, mk.add_cylinder(bm, r, h, loc=loc, rot=rot, segments=segments, radius2=r2), pigment, uv)


def _path(bm, uv):
    """The ring path: a low cobbled strip with a darker muddy edge."""
    y0, y1 = PATH_Y
    _box(bm, uv, COBBLE, (0, (y0 + y1) / 2, 0.02), (rk.FOOTPRINT, y1 - y0, 0.04))
    _box(bm, uv, MUD, (0, y0 - 0.25, 0.012), (rk.FOOTPRINT, 0.5, 0.024))


def _yard_brazier(x, y=-1.9, lit=0):
    cb._fire("Brazier", x, y, 0.0, lit=lit)


def _barrel(bm, uv, x, y, r=0.42, h=0.9):
    _cyl(bm, uv, TIMBER, r, h, (x, y, h / 2), segments=10)
    _cyl(bm, uv, IRON, r * 1.04, 0.06, (x, y, h * 0.22), segments=10)
    _cyl(bm, uv, IRON, r * 1.04, 0.06, (x, y, h * 0.78), segments=10)


def _sack(bm, uv, x, y, pigment=SACK):
    mk.paint(bm, mk.add_sphere(bm, 0.38, loc=(x, y, 0.3), segments=8, rings=6, scale=(1.0, 0.8, 0.8)), pigment, uv)


def _cart(bm, uv, x, y, load=None, along_x=True):
    """A two-wheeled cart, bed 1.3 x 2.4, resting on its wheels and a prop at the front."""
    sx, sy = (2.4, 1.3) if along_x else (1.3, 2.4)
    _box(bm, uv, TIMBER, (x, y, 0.72), (sx, sy, 0.12))
    for side in (-1, 1):
        if along_x:
            _box(bm, uv, TIMBER, (x, y + side * (sy / 2 - 0.05), 0.95), (sx, 0.08, 0.35))
            _cyl(bm, uv, TIMBER, 0.55, 0.12, (x - 0.3, y + side * (sy / 2 + 0.1), 0.55),
                 rot=Euler((math.radians(90), 0, 0)), segments=10)
        else:
            _box(bm, uv, TIMBER, (x + side * (sx / 2 - 0.05), y, 0.95), (0.08, sy, 0.35))
            _cyl(bm, uv, TIMBER, 0.55, 0.12, (x + side * (sx / 2 + 0.1), y - 0.3, 0.55),
                 rot=Euler((0, math.radians(90), 0)), segments=10)
    # The shafts rest on the ground in front.
    if along_x:
        _box(bm, uv, TIMBER, (x + sx / 2 + 0.6, y, 0.35), (1.4, 0.8, 0.1))
        _box(bm, uv, TIMBER, (x + sx / 2 + 1.2, y, 0.17), (0.12, 0.8, 0.34))
    else:
        _box(bm, uv, TIMBER, (x, y + sy / 2 + 0.6, 0.35), (0.8, 1.4, 0.1))
        _box(bm, uv, TIMBER, (x, y + sy / 2 + 1.2, 0.17), (0.8, 0.12, 0.34))
    if load:
        _box(bm, uv, load, (x, y, 1.05), (sx - 0.25, sy - 0.25, 0.55))


def _bale(bm, uv, x, y, z=0.0, along_x=True):
    size = (1.1, 0.55, 0.5) if along_x else (0.55, 1.1, 0.5)
    _box(bm, uv, HAY, (x, y, z + 0.25), size)


# ── Curtain dressings ────────────────────────────────────────────────────

def build_dressing_lean_to(bm, uv):
    """A timber shed roofed against the wall, barrels and sacks under it."""
    _path(bm, uv)
    x0, x1 = -5.4, -0.9
    for x in (x0 + 0.15, x1 - 0.15):
        _box(bm, uv, TIMBER, (x, WALL_FACE + 3.1, 1.25), (0.2, 0.2, 2.5))
        _box(bm, uv, TIMBER, (x, WALL_FACE + 0.1, 1.6), (0.2, 0.2, 3.2))
    # Roof: sloping from the wall down to the front posts.
    run, drop = 3.2, 0.7
    angle = math.atan2(drop, run)
    _box(bm, uv, TIMBER, ((x0 + x1) / 2, WALL_FACE + 1.6, 2.9),
         (x1 - x0 + 0.3, math.hypot(run, drop) + 0.2, 0.12))
    # Slope it by rebuilding as a rotated box: mesh_kit rotates about the box centre.
    mk.paint(bm, mk.add_box(bm, (x1 - x0 + 0.3, math.hypot(run, drop) + 0.2, 0.1),
                            loc=((x0 + x1) / 2, WALL_FACE + 1.6, 3.0),
                            rot=Euler((angle, 0, 0))), TIMBER, uv)
    for x, y in ((-4.6, WALL_FACE + 0.6), (-3.7, WALL_FACE + 0.6), (-4.2, WALL_FACE + 1.5)):
        _barrel(bm, uv, x, y)
    for x, y in ((-2.1, WALL_FACE + 0.5), (-1.6, WALL_FACE + 1.0)):
        _sack(bm, uv, x, y)
    # Out in the open, right of the torch: a stack of crates.
    _box(bm, uv, TIMBER, (3.3, WALL_FACE + 0.5, 0.35), (0.7, 0.7, 0.7))
    _box(bm, uv, TIMBER, (4.1, WALL_FACE + 0.5, 0.35), (0.7, 0.7, 0.7))
    _box(bm, uv, TIMBER, (3.7, WALL_FACE + 0.5, 1.05), (0.7, 0.7, 0.7))
    _yard_brazier(2.6)


def build_dressing_woodpile(bm, uv):
    """Logs stacked against the wall, a hay cart and a chopping block."""
    _path(bm, uv)
    rows = [4, 3, 2]
    for level, count in enumerate(rows):
        for i in range(count):
            y = WALL_FACE + 0.25 + i * 0.34 + level * 0.17
            _cyl(bm, uv, TIMBER, 0.17, 2.6, (-3.4, y, 0.17 + level * 0.3),
                 rot=Euler((0, math.radians(90), 0)), segments=7)
    _cart(bm, uv, 2.9, WALL_FACE + 2.0, load=HAY, along_x=True)
    _cyl(bm, uv, TIMBER, 0.36, 0.5, (-0.9, WALL_FACE + 2.4, 0.25), segments=10)
    _box(bm, uv, TIMBER, (-0.9, WALL_FACE + 2.4, 0.62), (0.05, 0.5, 0.25))
    _yard_brazier(-1.6, y=-1.0)


def build_dressing_pens(bm, uv):
    """A wattle pen with a trough, and a chicken coop."""
    _path(bm, uv)
    x0, x1 = -5.2, -0.8
    y0, y1 = WALL_FACE + 0.1, WALL_FACE + 3.2
    posts = [(x0, y1), (x1, y1)] + [(x0 + i * (x1 - x0) / 4, y1) for i in range(1, 4)] \
        + [(x0, y0 + (y1 - y0) / 2), (x1, y0 + (y1 - y0) / 2)]
    for x, y in posts:
        _box(bm, uv, TIMBER, (x, y, 0.55), (0.12, 0.12, 1.1))
    for z in (0.45, 0.9):
        _box(bm, uv, SACK, ((x0 + x1) / 2, y1, z), (x1 - x0, 0.06, 0.18))
        _box(bm, uv, SACK, (x0, (y0 + y1) / 2, z), (0.06, y1 - y0, 0.18))
        _box(bm, uv, SACK, (x1, (y0 + y1) / 2, z), (0.06, y1 - y0, 0.18))
    _box(bm, uv, TIMBER, (-3.0, WALL_FACE + 1.3, 0.25), (1.6, 0.5, 0.5))
    _box(bm, uv, WATER, (-3.0, WALL_FACE + 1.3, 0.49), (1.4, 0.35, 0.03))
    # The coop: a raised hutch on legs with a pitched roof.
    for dx in (-0.6, 0.6):
        for dy in (-0.4, 0.4):
            _box(bm, uv, TIMBER, (3.0 + dx, WALL_FACE + 1.1 + dy, 0.2), (0.1, 0.1, 0.4))
    _box(bm, uv, TIMBER, (3.0, WALL_FACE + 1.1, 0.8), (1.4, 1.0, 0.8))
    mk.paint(bm, mk.add_box(bm, (1.6, 0.75, 0.08), loc=(3.0, WALL_FACE + 0.85, 1.35),
                            rot=Euler((math.radians(-30), 0, 0))), SACK, uv)
    mk.paint(bm, mk.add_box(bm, (1.6, 0.75, 0.08), loc=(3.0, WALL_FACE + 1.35, 1.35),
                            rot=Euler((math.radians(30), 0, 0))), SACK, uv)
    _yard_brazier(1.3)


def build_dressing_training(bm, uv):
    """Pells to hack at, a weapon rack against the wall, straw butts."""
    _path(bm, uv)
    for x, y in ((-4.3, WALL_FACE + 2.4), (-2.6, WALL_FACE + 2.8)):
        _cyl(bm, uv, TIMBER, 0.13, 1.8, (x, y, 0.9), segments=8)
        _cyl(bm, uv, IRON, 0.16, 0.08, (x, y, 1.3), segments=8)
    # The rack: two uprights, two rails, staves leaning in.
    for x in (1.6, 3.8):
        _box(bm, uv, TIMBER, (x, WALL_FACE + 0.35, 0.9), (0.12, 0.12, 1.8))
    for z in (0.5, 1.5):
        _box(bm, uv, TIMBER, (2.7, WALL_FACE + 0.35, z), (2.3, 0.1, 0.1))
    for i in range(5):
        x = 1.9 + i * 0.42
        mk.paint(bm, mk.add_box(bm, (0.06, 0.06, 1.9), loc=(x, WALL_FACE + 0.5, 0.95),
                                rot=Euler((math.radians(-8), 0, 0))), TIMBER, uv)
        _box(bm, uv, STONE, (x, WALL_FACE + 0.36, 1.85), (0.12, 0.05, 0.25))
    for x in (-0.9, 0.3):
        _cyl(bm, uv, HAY, 0.5, 0.9, (x + 4.6, WALL_FACE + 2.6, 0.45), segments=10)
    _yard_brazier(-0.9, y=-1.0)


def build_dressing_gate_yard(bm, uv):
    """Beside the gate: carts waiting to leave, hay, and the main brazier pair's."""
    _path(bm, uv)
    _cart(bm, uv, -3.2, WALL_FACE + 1.8, load=HAY, along_x=True)
    _bale(bm, uv, 2.8, WALL_FACE + 0.5)
    _bale(bm, uv, 3.9, WALL_FACE + 0.5)
    _bale(bm, uv, 3.35, WALL_FACE + 0.5, z=0.5)
    _bale(bm, uv, 4.4, WALL_FACE + 1.5, along_x=False)
    _barrel(bm, uv, 1.5, WALL_FACE + 0.6)
    _yard_brazier(0.9, y=-1.6)
    _yard_brazier(-0.9, y=-1.6)


def build_dressing_plain(bm, uv):
    """An open stretch, so the ring does not read as cluttered: the path and a trough."""
    _path(bm, uv)
    _box(bm, uv, STONE, (3.6, WALL_FACE + 0.5, 0.35), (1.8, 0.6, 0.7))
    _box(bm, uv, WATER, (3.6, WALL_FACE + 0.5, 0.69), (1.6, 0.4, 0.03))


# ── Courtyards: carved interior cells ───────────────────────────────────

def _yard_floor(bm, uv, pigment):
    _box(bm, uv, pigment, (0, 0, 0.02), (rk.FOOTPRINT - 0.2, rk.FOOTPRINT - 0.2, 0.04))


def _hedge(bm, uv, x, y, sx, sy, h=0.9):
    _box(bm, uv, GREEN, (x, y, h / 2), (sx, sy, h))


def build_courtyard_herb_garden(bm, uv):
    _yard_floor(bm, uv, MUD)
    for x in (-2.6, 2.6):
        for y in (-2.6, 2.6):
            _box(bm, uv, TIMBER, (x, y, 0.2), (3.2, 3.2, 0.4))
            _box(bm, uv, GREEN, (x, y, 0.45), (2.9, 2.9, 0.12))
    cb._fire("Brazier", 0.0, 0.0, 0.04, lit=0)


def build_courtyard_midden(bm, uv):
    _yard_floor(bm, uv, MUD)
    # Heaps of refuse: low cones, their bases on the ground.
    for x, y, r in ((-2.0, -1.5, 1.8), (1.5, 1.8, 1.3), (2.2, -2.4, 1.0)):
        _cyl(bm, uv, IRON, r, r * 0.55, (x, y, r * 0.275), segments=10, r2=r * 0.25)
    for x, y in ((-4.4, 3.8), (-3.5, 4.4)):
        _barrel(bm, uv, x, y)
    cb._fire("Brazier", 3.6, 3.6, 0.04, lit=1)


def build_courtyard_well_yard(bm, uv):
    _yard_floor(bm, uv, COBBLE)
    _cyl(bm, uv, STONE, 1.1, 0.8, (0, 0, 0.44), segments=12)
    _cyl(bm, uv, WATER, 0.85, 0.05, (0, 0, 0.85), segments=12)
    for x in (-1.0, 1.0):
        _box(bm, uv, TIMBER, (x, 0, 1.4), (0.15, 0.15, 2.0))
    mk.paint(bm, mk.add_box(bm, (2.4, 1.3, 0.08), loc=(0, -0.45, 2.45),
                            rot=Euler((math.radians(-28), 0, 0))), TIMBER, uv)
    mk.paint(bm, mk.add_box(bm, (2.4, 1.3, 0.08), loc=(0, 0.45, 2.45),
                            rot=Euler((math.radians(28), 0, 0))), TIMBER, uv)
    _box(bm, uv, TIMBER, (0, 0, 2.35), (2.2, 0.12, 0.12))
    _box(bm, uv, STONE, (3.4, -3.4, 0.3), (2.0, 0.7, 0.6))
    cb._fire("Brazier", -3.6, 3.6, 0.04, lit=0)


def build_courtyard_tiltyard(bm, uv):
    _yard_floor(bm, uv, MUD)
    for i in range(7):
        _box(bm, uv, TIMBER, (-4.5 + i * 1.5, 0, 0.7), (0.15, 0.15, 1.4))
    for z in (0.7, 1.3):
        _box(bm, uv, TIMBER, (0, 0, z), (9.2, 0.1, 0.12))
    _cyl(bm, uv, TIMBER, 0.12, 2.2, (0, 4.0, 1.1), segments=8)
    _box(bm, uv, TIMBER, (0, 4.0, 2.1), (1.6, 0.12, 0.12))
    cb._fire("Brazier", -4.6, -4.2, 0.04, lit=0)
    cb._fire("Brazier", 4.6, 4.2, 0.04, lit=1)


def build_courtyard_cloister_garth(bm, uv):
    _yard_floor(bm, uv, COBBLE)
    _box(bm, uv, GREEN, (0, 0, 0.06), (8.0, 8.0, 0.08))
    _cyl(bm, uv, STONE, 0.9, 0.5, (0, 0, 0.3), segments=10)
    _box(bm, uv, STONE, (0, 0, 1.3), (0.3, 0.3, 1.6))
    _box(bm, uv, STONE, (0, 0, 1.6), (1.0, 0.25, 0.25))
    for x, y in ((-4.6, -4.6), (4.6, 4.6)):
        cb._fire("Brazier", x, y, 0.04, lit=0)


def build_courtyard_formal_garden(bm, uv):
    _yard_floor(bm, uv, COBBLE)
    for sx in (-1, 1):
        for sy in (-1, 1):
            _hedge(bm, uv, sx * 3.0, sy * 1.2, 4.0, 0.5)
            _hedge(bm, uv, sx * 1.2, sy * 3.0, 0.5, 4.0)
    _cyl(bm, uv, STONE, 1.3, 0.5, (0, 0, 0.29), segments=12)
    _cyl(bm, uv, WATER, 1.1, 0.04, (0, 0, 0.55), segments=12)
    _cyl(bm, uv, STONE, 0.2, 1.4, (0, 0, 1.2), segments=8)
    for x, y in ((-4.8, 0), (4.8, 0)):
        cb._fire("Brazier", x, y, 0.04, lit=0)


# ── The sealed gate (docs/plans/night-atmosphere.md, section 6) ─────────

def build_dressing_gate_sealed(bm, uv):
    """Laid over the gatehouse: the portcullis down in the arch and the inner
    doors shut and barred, so the one gate reads as closed. The arch is 4.6 m
    wide and 4.2 m high on the cell's south wall (build_gatehouse_module)."""
    arch_y = -rk.HALF + 0.35
    for i in range(7):
        x = -2.1 + i * 0.7
        _box(bm, uv, IRON, (x, arch_y, 2.05), (0.14, 0.14, 4.1))
    for z in (0.9, 2.1, 3.3):
        _box(bm, uv, IRON, (0, arch_y, z), (4.4, 0.12, 0.12))
    # The inner leaves, shut, with a bar across them in its brackets.
    door_y = -rk.HALF + rk.WALL_T + 0.1
    for side in (-1, 1):
        _box(bm, uv, TIMBER, (side * 1.15, door_y, 1.9), (2.3, 0.15, 3.8))
    _box(bm, uv, TIMBER, (0, door_y + 0.2, 1.6), (5.0, 0.2, 0.25))
    for x in (-2.3, 2.3):
        _box(bm, uv, IRON, (x, door_y + 0.15, 1.6), (0.12, 0.3, 0.4))
