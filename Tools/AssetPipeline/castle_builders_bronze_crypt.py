"""
Bronze Age Crypt pieces: BronzeDromos, BronzeGraveCircle, BronzeLarnaxVault, BronzeTholos, BronzeShaftStair.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/bronze/<Key>.json,
drawing: docs/art/rooms/concept/bronze/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_bronze.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_bronze import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


# ── Crypt: tholos tombs and shaft graves ────────────────────────────────

ASHLAR = "vellum_faint"


def _stele(bm, uv, x, y, fz, facing_north):
    """A grave stele on its base, the slab at the back of the base (away from the
    room's centre line), the base's front half free for offerings."""
    back = 0.1 if facing_north else -0.1
    cb._box(bm, uv, ASHLAR, (x, y, fz + 0.15), (0.9, 0.5, 0.30))
    ek.prism(bm, uv, "vellum_dim", [(-0.30, 0.0), (0.30, 0.0), (0.27, 1.30), (-0.27, 1.30)], 0.15,
             loc=(x, y + back, fz + 0.30), along="y")


def build_bronze_dromos(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeDromos.svg"""
    h, fz = room_shell(bm, uv, "Crypt")
    # Corbelled ashlar courses along the east and west walls, in the quadrants, stepping in as they rise.
    for sgn in (-1, 1):
        for sy in (-1, 1):
            for bottom, top, depth in ((0.0, 1.0, 0.8), (1.0, 2.0, 1.0), (2.0, 2.7, 1.2)):
                cb._box(bm, uv, ASHLAR, (sgn * (IN - depth / 2), sy * 3.725, fz + (bottom + top) / 2),
                        (depth, 3.45, top - bottom))
    # A grave stele in each quadrant, offerings laid on the bases of two of them.
    for x, y in ((3.2, 4.8), (-3.2, 4.8), (3.2, -4.8), (-3.2, -4.8)):
        _stele(bm, uv, x, y, fz, facing_north=y > 0)
    cb._anchor(3.2, 4.65, fz + 0.30)
    cb._anchor(-3.2, -4.65, fz + 0.30)
    # Amphora offerings, an offering table, and a painted runner down the north-south axis.
    for x, y in ((2.3, 4.4), (-2.4, 4.3), (2.4, -4.2)):
        ek.jar(bm, uv, GRAIN_JAR, x, y, fz, 0.62, 0.34, mouth=0.12, segments=8)
    cb._table(bm, uv, ASHLAR, -3.2, 3.6, fz, 0.6, 0.6, h=0.7)
    cb._box(bm, uv, RED, (0, 0, fz + 0.015), (1.2, 2 * IN - 0.1, 0.03))


def build_bronze_grave_circle(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeGraveCircle.svg"""
    h, fz = room_shell(bm, uv, "Crypt")
    r, angles = 5.0, [25 + k * 8 for k in range(6)]
    # The ring: four arcs of upright slabs at r 5.00 m, set tangentially, capped pair by pair.
    for qx, qy in ((1, 1), (-1, 1), (-1, -1), (1, -1)):
        pts = []
        for a in angles:
            t = math.radians(a)
            x, y = qx * r * math.cos(t), qy * r * math.sin(t)
            pts.append((x, y))
            mk.paint(bm, mk.add_box(bm, (0.2, 0.6, 1.0), loc=(x, y, fz + 0.5), rot=Euler((0, 0, math.atan2(y, x)))),
                     "vellum_dim", uv)
        for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
            mx, my = (x0 + x1) / 2, (y0 + y1) / 2
            mk.paint(bm, mk.add_box(bm, (0.3, math.hypot(x1 - x0, y1 - y0) + 0.3, 0.12), loc=(mx, my, fz + 1.06),
                                    rot=Euler((0, 0, math.atan2(my, mx)))), ASHLAR, uv)
    # A shaft grave's cover slab inside each arc, a gold cup left on each.
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * 2.7, sy * 2.7
            cb._box(bm, uv, ASHLAR, (x, y, fz + 0.1), (1.2, 0.8, 0.2))
            mk.paint(bm, mk.add_cylinder(bm, 0.08, 0.10, loc=(x - 0.3, y, fz + 0.25), segments=8, radius2=0.05),
                     GOLD, uv)
            cb._anchor(x, y, fz + 0.2)


def build_bronze_larnax_vault(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeLarnaxVault.svg"""
    h, fz = room_shell(bm, uv, "Crypt")
    for sx in (-1, 1):
        for sy in (-1, 1):
            larnax(bm, uv, sx * 3.3, sy * 4.9, fz, along_x=True)          # along the north / south wall
    for sx in (-1, 1):
        for sy in (-1, 1):
            larnax(bm, uv, sx * 4.95, sy * 2.9, fz, along_x=False)        # along the east / west wall
    for sx in (-1, 1):
        for sy in (-1, 1):
            pithos(bm, uv, sx * 4.9, sy * 4.9, fz, height=1.0, belly=0.6)
