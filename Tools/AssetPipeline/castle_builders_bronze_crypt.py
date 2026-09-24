"""
Bronze Age Crypt pieces: BronzeDromos, BronzeGraveCircle, BronzeLarnaxVault, BronzeTholos, BronzeShaftStair.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/BronzeAge/<Key>.json,
drawing: docs/art/rooms/concept/BronzeAge/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_bronze.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
import bmesh

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


THOLOS_COURSES = [(4.90, 0.00), (4.55, 0.65), (4.20, 1.30), (3.85, 1.95)]   # (inner radius, bottom)
THOLOS_HC = 0.65


def _beehive_quadrant(bm, uv, qx, qy, fz, a0=28.0, a1=62.0, facets=6):
    """One quadrant's corbelled courses: each course a closed block whose inner face is
    the ring arc from a0 to a1 degrees off the x axis, facing the centre, and whose
    back runs out to just short of the walls (the 45° facet corner lands on the room's
    corner), so the corner reads packed solid. Each course above the first sinks
    room_kit.OVERLAP into the one below rather than sharing its vertices."""
    w = IN - 0.02
    angles = [a0 + k * (a1 - a0) / facets for k in range(facets + 1)]
    for k, (r, bottom) in enumerate(THOLOS_COURSES):
        z0 = fz + bottom - (rk.OVERLAP if k else 0.0)
        z1 = fz + bottom + THOLOS_HC
        rings = []                                  # [inner bottom, inner top, outer top, outer bottom] per angle
        for a in angles:
            c, s = math.cos(math.radians(a)), math.sin(math.radians(a))
            d = w / max(c, s)
            rings.append([bm.verts.new((qx * rr * c, qy * rr * s, z)) for rr, z in ((r, z0), (r, z1), (d, z1), (d, z0))])
        faces = [bm.faces.new(rings[0]), bm.faces.new(list(reversed(rings[-1])))]
        for ra, rb in zip(rings, rings[1:]):
            for m in range(4):
                n = (m + 1) % 4
                faces.append(bm.faces.new((ra[m], rb[m], rb[n], ra[n])))
        bmesh.ops.recalc_face_normals(bm, faces=faces)
        mk.paint(bm, faces, ASHLAR if k % 2 == 0 else "vellum_dim", uv)


def build_bronze_tholos(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeTholos.svg"""
    h, fz = room_shell(bm, uv, "Crypt")
    for qx in (-1, 1):
        for qy in (-1, 1):
            _beehive_quadrant(bm, uv, qx, qy, fz)
    # NE: the king on his bier under a gold mask, a tripod brazier at his feet.
    cb._box(bm, uv, ASHLAR, (2.7, 2.35, fz + 0.25), (1.8, 0.7, 0.5))
    cb._box(bm, uv, RED, (2.7, 2.35, fz + 0.41), (1.82, 0.72, 0.06))
    cb._box(bm, uv, LINEN, (2.7, 2.35, fz + 0.53), (1.7, 0.6, 0.06))
    cb._box(bm, uv, GOLD, (3.3, 2.35, fz + 0.585), (0.22, 0.18, 0.05))
    cb._anchor(2.7, 2.35, fz + 0.56)
    tripod(bm, uv, 2.0, 3.35, fz)
    # NW: the offering table and its gold cups.
    offering_table(bm, uv, -2.6, 2.6, fz, w=0.9, d=0.6, h=0.7, pigment=ASHLAR)
    for dx, r in ((-0.3, 0.05), (0.0, 0.055), (0.25, 0.045)):
        mk.paint(bm, mk.add_cylinder(bm, r, 0.10, loc=(-2.6 + dx, 2.6, fz + 0.75), segments=8, radius2=r * 0.8),
                 GOLD, uv)
    # SW: the bronze-bound chest and two amphorae.
    cb._chest(bm, uv, -2.8, -2.3, fz, w=1.0, d=0.6, h=0.55, trim=METAL, body=TIMBER)
    amphora(bm, uv, -2.0, -3.2, fz)
    amphora(bm, uv, -2.6, -3.4, fz)
    # SE: the standing brazier.
    cb._brazier(bm, uv, 2.7, -2.7, fz, pigment=RED, metal=METAL)


def build_bronze_shaft_stair(bm, uv):
    """docs/art/rooms/concept/BronzeAge/BronzeShaftStair.svg"""
    h, fz = room_shell(bm, uv, "Crypt")
    # NW: the kit stair up the west wall to the dais, the larnax lying in state on it.
    cb._stair_to_dais(bm, uv, fz, 1.0, ASHLAR)
    larnax(bm, uv, -4.4, 4.55, fz + 1.0, along_x=False)
    # SW: the offering table with gold cups, amphorae round it.
    offering_table(bm, uv, -3.4, -3.4, fz, w=0.6, d=0.6, h=0.7, pigment=ASHLAR)
    for dx, r in ((-0.15, 0.05), (0.15, 0.045)):
        mk.paint(bm, mk.add_cylinder(bm, r, 0.10, loc=(-3.4 + dx, -3.4, fz + 0.75), segments=8, radius2=r * 0.8),
                 GOLD, uv)
    for x, y in ((-4.6, -4.6), (-4.8, -3.8), (-3.8, -4.8)):
        amphora(bm, uv, x, y, fz)
    # SE: the kerbed shaft, its void, the sheerlegs and the basket hanging over it.
    sx, sy = 3.7, -3.4
    for oy in (-0.6, 0.6):
        cb._box(bm, uv, "vellum_dim", (sx, sy + oy, fz + 0.175), (2.2, 0.2, 0.35))
    for ox in (-1.0, 1.0):
        cb._box(bm, uv, "vellum_dim", (sx + ox, sy, fz + 0.175), (0.2, 1.0, 0.35))
    cb._box(bm, uv, SOOT, (sx, sy, fz + 0.01), (1.82, 1.02, 0.02))
    lean = math.atan2(0.6, 2.2)
    for x in (2.4, 5.0):
        for side in (-1, 1):
            mk.paint(bm, mk.add_box(bm, (0.1, 0.1, math.hypot(0.6, 2.2)), loc=(x, sy + side * 0.3, fz + 1.1),
                                    rot=Euler((side * lean, 0, 0))), TIMBER, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.06, 2.8, loc=(sx, sy, fz + 2.15), rot=Euler((0, math.pi / 2, 0)), segments=6),
             TIMBER, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.015, 1.39, loc=(sx, sy, fz + 1.47), segments=4), LINEN, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.22, 0.30, loc=(sx, sy, fz + 0.65), segments=8, radius2=0.18), TIMBER, uv)
    cb._anchor(sx, sy, fz + 0.80)
    # NE: storage jars along the north wall; braziers either side of the north archway.
    for x in (2.4, 3.5, 4.6):
        pithos(bm, uv, x, 4.9, fz, height=1.2, belly=0.7, lid=True)
    cb._brazier(bm, uv, -2.4, 2.8, fz, pigment=RED, metal=METAL)
    cb._brazier(bm, uv, 4.4, 2.6, fz, pigment=RED, metal=METAL)
