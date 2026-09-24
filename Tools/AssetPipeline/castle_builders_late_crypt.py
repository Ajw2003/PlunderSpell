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


BONE = "vellum"


def build_late_charnel_house(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateCharnelHouse.svg"""
    h, fz = room_shell(bm, uv, "Crypt")
    anchored = {(1, 1): 1.5, (-1, 1): 0.8, (1, -1): 0.8, (-1, -1): 1.5}
    for sx in (-1, 1):
        for sy in (-1, 1):
            a, b = sorted((sx * 2.2, sx * 5.2))
            cx, cy = (a + b) / 2, sy * (IN - 0.25)
            for x in (a + 0.04, b - 0.04):
                cb._box(bm, uv, TIMBER, (x, cy, fz + 1.05), (0.08, 0.5, 2.1))
            for z in (0.1, 0.8, 1.5):
                cb._box(bm, uv, TIMBER, (cx, cy, fz + z + 0.025), (2.92, 0.5, 0.05))
                cb._box(bm, uv, BONE, (cx, sy * (IN - 0.28), fz + z + 0.125), (2.7, 0.34, 0.15))
                for k in range(7):
                    if k == 3 and anchored[(sx, sy)] == z:
                        continue
                    x = a + 0.25 + k * (b - a - 0.5) / 6
                    # An eight-sided drum facing the room: round from the front, and quads, not a sphere's fans.
                    mk.paint(bm, mk.add_cylinder(bm, 0.1, 0.16, loc=(x, sy * (IN - 0.38), fz + z + 0.29),
                                                 rot=Euler((math.radians(90), 0, 0)), segments=8), BONE, uv)
            cb._anchor(sx * 3.7, sy * (IN - 0.38), fz + anchored[(sx, sy)] + 0.2)
    # NE: the little altar against the east wall, a candle on it.
    cb._box(bm, uv, WALL, (IN - 0.3, 2.9, fz + 0.5), (0.6, 0.9, 1.0))
    mk.paint(bm, mk.add_cylinder(bm, 0.05, 0.03, loc=(5.1, 2.9, fz + 1.015), segments=6), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.025, 0.2, loc=(5.1, 2.9, fz + 1.13), segments=6), LINEN, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.018, 0.06, loc=(5.1, 2.9, fz + 1.26), segments=4, radius2=0.004), CLOTH, uv)
    # SW: the charnel cart, its load of bones, a shovel.
    kx, ky = -3.6, -3.0
    cb._box(bm, uv, TIMBER, (kx, ky, fz + 0.6), (1.2, 0.7, 0.1))
    cb._box(bm, uv, BONE, (kx, ky, fz + 0.72), (1.0, 0.5, 0.14))
    mk.paint(bm, mk.add_cylinder(bm, 0.03, 0.9, loc=(kx + 0.2, ky, fz + 0.3), rot=Euler((math.radians(90), 0, 0)),
                                 segments=6), IRON, uv)
    for dy in (-0.42, 0.42):
        ek.wheel(bm, uv, TIMBER, kx + 0.2, ky + dy, fz + 0.3, 0.3, along="y")
    cb._box(bm, uv, TIMBER, (kx - 0.45, ky, fz + 0.28), (0.08, 0.08, 0.56))
    cb._box(bm, uv, TIMBER, (kx - 1.0, ky, fz + 0.6), (0.8, 0.04, 0.04))


def build_late_effigy_crypt(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateEffigyCrypt.svg"""
    h, fz = room_shell(bm, uv, "Crypt")
    # NE: the founder's tomb chest and his gilded effigy.
    fx, fy, fh = 3.6, 4.2, 0.9
    cb._box(bm, uv, WALL, (fx, fy, fz + fh / 2), (2.2, 1.0, fh))
    cb._box(bm, uv, "vellum_faint", (fx, fy, fz + fh - 0.04), (2.28, 1.08, 0.08))
    top = fz + fh
    cb._box(bm, uv, GOLD, (fx - 0.1, fy, top + 0.1), (1.6, 0.45, 0.2))
    ek.sphere(bm, uv, GOLD, fx + 0.8, fy, top + 0.02, 0.11, segments=8, rings=4)
    ek.prism(bm, uv, GOLD, [(-0.1, 0.0), (0.1, 0.0), (0.0, 0.16)], 0.12, loc=(fx + 0.15, fy, top + 0.2), along="y")
    cb._anchor(fx, fy + 0.36, top)
    # The canopy: four oak posts, the madder tester and its gilt fringe, hangings at the back.
    ch = fz + 2.6
    for dx in (-1.3, 1.3):
        for dy in (-0.7, 0.7):
            cb._box(bm, uv, TIMBER, (fx + dx, fy + dy, (fz + ch) / 2), (0.14, 0.14, ch - fz))
    cb._box(bm, uv, CLOTH, (fx, fy, ch + 0.075), (2.8, 1.6, 0.15))
    cb._box(bm, uv, GOLD, (fx, fy - 0.815, ch - 0.03), (2.8, 0.03, 0.08))
    cb._box(bm, uv, CLOTH, (fx, fy + 0.7, (top + 0.3 + ch) / 2), (2.6, 0.04, ch - top - 0.3))
    # The candle hearse before the tomb: legs, base bar, two sloped bars, seven candles.
    hx, hy, hw, hh = 3.6, 2.5, 1.2, 1.1
    base = fz + 0.4
    for dx in (-hw / 2 + 0.1, hw / 2 - 0.1):
        cb._box(bm, uv, IRON, (hx + dx, hy, fz + 0.2), (0.04, 0.04, 0.4))
    cb._box(bm, uv, IRON, (hx, hy, base), (hw, 0.04, 0.04))
    tilt = math.atan2(hw / 2, hh)
    length = math.hypot(hw / 2, hh)
    for s in (-1, 1):
        mk.paint(bm, mk.add_box(bm, (0.04, 0.04, length), loc=(hx + s * hw / 4, hy, base + hh / 2),
                                rot=Euler((0, -s * tilt, 0))), IRON, uv)
    for k in range(7):
        t = k / 6
        x = hx - hw / 2 + t * hw
        z = base + hh * (1 - abs(2 * t - 1))
        mk.paint(bm, mk.add_cylinder(bm, 0.018, 0.16, loc=(x, hy, z + 0.08), segments=6), LINEN, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.014, 0.05, loc=(x, hy, z + 0.185), segments=4, radius2=0.003), CLOTH, uv)
    # NW, SE, SW: lesser tomb chests, a brass plate let into each lid.
    for x, y in ((-3.6, 4.6), (3.6, -4.6), (-3.6, -4.6)):
        cb._box(bm, uv, WALL, (x, y, fz + 0.4), (2.0, 0.8, 0.8))
        cb._box(bm, uv, "bronze", (x - 0.55, y, fz + 0.805), (0.5, 0.35, 0.01))
        cb._anchor(x, y, fz + 0.8)
