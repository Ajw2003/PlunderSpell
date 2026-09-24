"""
Bronze Age Keep pieces: BronzeMegaron, BronzeTreasury, BronzeQueensHall, BronzeBathRoom, BronzeMegaronStair.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/bronze/<Key>.json,
drawing: docs/art/rooms/concept/bronze/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_bronze.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_bronze import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


# ── Keep: the wanax's quarters ──────────────────────────────────────────

def build_bronze_megaron(bm, uv):
    h, fz = room_shell(bm, uv, "Keep")
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
        clay_bench(bm, uv, sx * 3.1, IN - 0.25, fz, 2.4)
        clay_bench(bm, uv, sx * 3.6, -IN + 0.25, fz, 3.2)
    # Two tripods by the hearth, three offering tables.
    tripod(bm, uv, -3.5, 2.0, fz)
    tripod(bm, uv, 3.3, -2.0, fz)
    cb._table(bm, uv, "vellum_faint", -4.6, -3.6, fz, 0.6, 0.6, h=0.7)
    cb._table(bm, uv, "vellum_faint", -2.4, -4.3, fz, 0.6, 0.6, h=0.7)
    cb._table(bm, uv, "vellum_faint", 4.6, -3.6, fz, 0.6, 0.6, h=0.7)
    fresco_band(bm, uv, fz, sides=("north", "south", "west"))


def build_bronze_treasury(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeTreasury.svg"""
    h, fz = room_shell(bm, uv, "Keep")
    # A haematite dado on every wall section: the only paint in the room.
    for side in rk.SIDES:
        for along in (-3.4, 3.4):
            ek.wall_panel(bm, uv, RED, side, along, fz, 4.1, 0.40)
    # NE: the stepped gypsum plinth, the gold death-mask on top.
    cb._box(bm, uv, LINEN, (4.3, 4.3, fz + 0.25), (1.4, 1.4, 0.50))
    cb._box(bm, uv, LINEN, (4.3, 4.3, fz + 0.70), (0.8, 0.8, 0.40))
    mk.paint(bm, mk.add_sphere(bm, 0.14, loc=(4.3, 4.3, fz + 0.90 + 0.1), segments=8, rings=6,
                               scale=(1.0, 0.45, 1.4)), GOLD, uv)
    cb._anchor(4.3, 4.3, fz + 0.90)
    # Two ingot stacks on timber pallets, crosswise layers of 0.60 × 0.40 × 0.06 slabs.
    for x, y, w, d, layers in ((4.0, -4.3, 1.8, 1.2, 4), (-3.9, -2.9, 1.2, 0.9, 3)):
        cb._box(bm, uv, TIMBER, (x, y, fz + 0.06), (w, d, 0.12))
        nx, ny = int(w / 0.62), int(d / 0.42)
        for k in range(layers):
            z = fz + 0.12 + k * 0.06 + 0.03
            for j in range(nx):
                for i in range(ny):
                    cb._box(bm, uv, METAL, (x - w / 2 + 0.33 + j * 0.62, y - d / 2 + 0.24 + i * 0.42, z),
                            (0.56 if k % 2 == 0 else 0.52, 0.36 if k % 2 == 0 else 0.38, 0.06))
        cb._anchor(x, y, fz + 0.12 + layers * 0.06)
    # Two bronze-bound timber chests along the south wall, west of the archway.
    for x in (-4.6, -2.9):
        cb._chest(bm, uv, x, -4.9, fz, w=1.0, d=0.6, h=0.55, trim=METAL)
    # NW: the faience shelf against the north wall, with blue pieces on its boards.
    cb._shelf(bm, uv, -3.7, IN - 0.3, fz, 3.0, levels=3)
    for i, z in enumerate((0.35, 0.90, 1.45)):
        for k in range(5):
            px = -5.2 + 0.35 + k * 0.55 + (i % 2) * 0.2
            if px > -2.4:
                continue
            mk.paint(bm, mk.add_cylinder(bm, 0.08, 0.26, loc=(px, IN - 0.3, fz + z + 0.03 + 0.13), segments=6,
                                         radius2=0.04), FRESCO, uv)     # a faience flask
    # A bronze tripod each side (the east one by the plinth), clear of the walkway.
    tripod(bm, uv, -2.4, 2.6, fz)
    tripod(bm, uv, 2.6, 4.6, fz)
    # SE: two bronze cauldrons on the floor by the east wall, something in each.
    for x in (4.2, 5.0):
        mk.paint(bm, mk.add_cylinder(bm, 0.34, 0.45, loc=(x, -2.3, fz + 0.225), segments=10, radius2=0.26),
                 METAL, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.25, 0.04, loc=(x, -2.3, fz + 0.43), segments=10), SOOT, uv)
        cb._anchor(x, -2.3, fz + 0.45)
