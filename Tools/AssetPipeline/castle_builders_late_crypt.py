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


def build_late_oubliette(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateOubliette.svg"""
    h, fz = room_shell(bm, uv, "Crypt")
    cx0, part, fy0, bar_h = 3.5, 0.3, 2.2, 2.3
    door = (4.25, 4.95)
    for sx in (-1, 1):
        for sy in (-1, 1):
            # The partition on the cell's inner side, running back to the north or south wall.
            ylen = IN - fy0
            cb._box(bm, uv, WALL, (sx * (cx0 + part / 2), sy * (fy0 + ylen / 2), fz + (bar_h + 0.15) / 2),
                    (part, ylen, bar_h + 0.15))
            # The barred front facing the crossing: bars, three rails either side of the doorway, the lintel.
            fy = sy * fy0
            x = cx0 + part + 0.12
            while x < IN - 0.05:
                if not (door[0] - 0.02 < x < door[1] + 0.02):
                    cb._box(bm, uv, IRON, (sx * x, fy, fz + bar_h / 2), (0.04, 0.04, bar_h))
                x += 0.18
            for z in (0.3, 1.2, 2.0):
                for u0, u1 in ((cx0 + part, door[0]), (door[1], IN)):
                    cb._box(bm, uv, IRON, (sx * (u0 + u1) / 2, fy, fz + z), (u1 - u0, 0.03, 0.04))
            w = IN - cx0 - part
            cb._box(bm, uv, IRON, (sx * (cx0 + part + w / 2), fy, fz + bar_h + 0.075), (w, 0.1, 0.15))
            # Inside: straw on the floor and a plank shelf against the outer wall.
            cb._box(bm, uv, "bronze", (sx * (cx0 + part + w / 2), sy * (fy0 + ylen / 2), fz + 0.06),
                    (w - 0.3, ylen - 0.4, 0.12))
            cb._box(bm, uv, TIMBER, (sx * (IN - 0.225), sy * 3.9, fz + 0.525), (0.45, 1.2, 0.05))
    cb._anchor(IN - 0.225, 3.9, fz + 0.55)
    cb._anchor(-(IN - 0.225), -3.9, fz + 0.55)
    # NW: the stocks before the cell, a bench for the prisoner.
    sx, sy = -2.6, 3.4
    for dx in (-0.74, 0.74):
        cb._box(bm, uv, TIMBER, (sx + dx, sy, fz + 0.375), (0.12, 0.12, 0.75))
    cb._box(bm, uv, TIMBER, (sx, sy, fz + 0.52), (1.6, 0.12, 0.2))
    for dx in (-0.25, 0.25):
        cb._box(bm, uv, SOOT, (sx + dx, sy - 0.07, fz + 0.52), (0.1, 0.02, 0.1))
    cb._bench(bm, uv, sx, sy - 0.35, fz, 1.4, 0.3, True, TIMBER)
    cb._anchor(sx, sy - 0.35, fz + 0.44)
    # Shackles on the north wall by the archway.
    for x, z in ((2.0, 1.4), (2.6, 1.3), (-2.2, 1.4)):
        ek.wall_panel(bm, uv, IRON, "north", x, fz + z + 0.25, 0.1, 0.1, depth=0.05)
        ek.wall_panel(bm, uv, IRON, "north", x, fz + z, 0.03, 0.26, depth=0.03, proud=0.01)
    # SE: the oubliette's flat iron grate.
    gx, gy = 2.6, -3.4
    cb._box(bm, uv, SOOT, (gx, gy, fz + 0.015), (1.2, 1.2, 0.03))
    for k in range(1, 6):
        cb._box(bm, uv, IRON, (gx - 0.6 + k * 0.2, gy, fz + 0.045), (0.04, 1.2, 0.03))
