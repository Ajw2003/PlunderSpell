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
