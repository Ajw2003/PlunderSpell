"""
Late Medieval Crypt pieces: LateUndercroft, LateOubliette, LateCharnelHouse, LateEffigyCrypt, LateUndercroftStair.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/LateMedieval/<Key>.json,
drawing: docs/art/rooms/concept/LateMedieval/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_late.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_late import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


import castle_builders_late_bailey as bailey  # noqa: E402  cask_on_cradle


def vault_pier(bm, uv, x, y, fz, r=0.4):
    """A squat octagonal pier: a square base, the shaft, a moulded capital, an abacus,
    and the springer stubs of the ribs crossing on top."""
    cb._box(bm, uv, WALL, (x, y, fz + 0.1), (1.0, 1.0, 0.2))
    mk.paint(bm, mk.add_cylinder(bm, r, 1.9, loc=(x, y, fz + 1.15), segments=8), WALL, uv)
    mk.paint(bm, mk.add_cylinder(bm, r + 0.02, 0.25, loc=(x, y, fz + 2.225), segments=8, radius2=0.6), WALL, uv)
    cb._box(bm, uv, "vellum_faint", (x, y, fz + 2.425), (1.1, 1.1, 0.15))
    cb._box(bm, uv, WALL, (x, y, fz + 2.65), (1.6, 0.3, 0.3))
    cb._box(bm, uv, WALL, (x, y, fz + 2.66), (0.32, 1.6, 0.28))


def build_late_undercroft(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateUndercroft.svg"""
    h, fz = room_shell(bm, uv, "Crypt")
    for sx in (-1, 1):
        for sy in (-1, 1):
            vault_pier(bm, uv, sx * 2.5, sy * 2.5, fz)
    cx = IN - 0.05 - 0.6
    top = 0.0
    for sx in (-1, 1):
        for sy in (-1, 1):
            for y in (2.7, 4.2):
                top = bailey.cask_on_cradle(bm, uv, sx * cx, sy * y, fz, r=0.5, length=1.2, along_x=True)
    for x, y in ((cx, 2.7), (-cx, 4.2), (cx, -4.2), (-cx, -2.7)):
        cb._anchor(x, y, top)
