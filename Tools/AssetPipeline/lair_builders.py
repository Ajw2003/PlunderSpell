"""
The Lair: the physical cellar the player returns to between raids, and its props
(docs/plans/diegetic-ui-lair-market.md, "The Lair, as a place"; concept image
docs/art/concept/lair/lair.png). Issue 286.

Runs only inside Blender's Python, like every other builder. Every key is built
from stacked primitives plus extruded polygons (`_prism`), in the shared pigment
atlas; no pigment is added.

Plan, looking down, +Y is the back (north) wall, +X to the right:

    north wall (long):   weapon rack stands in front of it at x ~ -4.2, the low
                         Market door is cut through it at x = -0.7 (2.6 m wide),
                         the hearth and chimney breast are built into it at x = 3.5
    west wall (short):   the arched recess for the portal arch, centred on y = 0
    ledger table, four strongboxes, century dial: placed in Unity (see README)

The cellar is NOT a 12 m castle cell: it is a one-off ~14 x 10 m interior (16.4 x
11.6 m outside, floor slab included), so it is exempt from the cell-footprint check
(only subdir "Castle*" modules are held to it). Origin: centre of the floor, the
slab's underside on Z = 0; the walkable floor is Z = 0.30 as in scale.md.

Pivot convention: every prop is bottom-flush (lowest vertex on Z = 0) and centred
on X/Y, as the validator requires. The four dial rings are the one subtlety: a
ring should turn about its own centre, but a ring centred on its axle would put
half of it below Z = 0. Each ring is therefore authored FLAT (a horizontal
annulus, tube radius 0.026 m, so it spans Z = -0.026..+0.026, inside the 3 cm
pivot tolerance) and centred on its axle, so its pivot IS the axle. Unity tilts
and spins each ring about its own pivot, and places all four rings at the same
point, the top of LairCenturyDialStand's axle (Z = 1.30 above the stand's base).
"""
import math

import bmesh
from mathutils import Euler, Vector

import mesh_kit as mk
import room_kit as rk

# ── pigments (all existing; nothing added to the atlas) ───────────────────
STONE = "vellum_faint"      # the cellar's walls and vault
DRESSED = "vellum_dim"      # ribs, door and hearth dressings, the portal arch
FLAG = "ash_hi"             # flagstones
JOINT = "bone_black"        # flag joints, soot
OAK = "oak"
LEATHER = "leather"
IRON = "line"               # blackened iron
STEEL = "iron"
BRASS = "bronze"            # the dial's brass
GOLD = "orpiment"           # value, flame
VELLUM = "vellum"           # pages, candle wax
EMBER = "madder"            # embers, the ribbon
PORTAL = "verdigris"        # the four portal stones


def _box(bm, uv, pigment, centre, size):
    """A box that overlaps whatever it touches (see room_kit.paint_box)."""
    rk.paint_box(bm, uv, pigment, centre, size, grow_axis="xyz")


def _sb(bm, uv, pigment, centre, size, rot=None):
    """A box exactly as sized: for strips too thin to grow by paint_box's OVERLAP."""
    mk.paint(bm, mk.add_box(bm, size, loc=centre, rot=rot), pigment, uv)


def _cyl(bm, uv, pigment, centre, radius, depth, rot=None, segments=8, radius2=None):
    mk.paint(bm, mk.add_cylinder(bm, radius, depth, loc=centre, rot=rot, segments=segments, radius2=radius2),
             pigment, uv)


def _limb(bm, uv, pigment, p0, p1, radius, segments=6):
    """A cylinder from p0 to p1 (a splayed leg, a brace)."""
    a, b = Vector(p0), Vector(p1)
    d = b - a
    rot = d.to_track_quat("Z", "Y").to_euler()
    _cyl(bm, uv, pigment, tuple((a + b) / 2), radius, d.length, rot=rot, segments=segments)


def _prism(bm, uv, pigment, pts, axis, c0, c1):
    """A closed prism: the 2D polygon `pts` extruded along `axis` from c0 to c1.
    pts are (y, z) for axis "x", (x, z) for axis "y", (x, y) for axis "z". The
    polygon may be concave (a wall with an arched doorway notched in it)."""
    def at(p, c):
        return {"x": (c, p[0], p[1]), "y": (p[0], c, p[1]), "z": (p[0], p[1], c)}[axis]
    lo = [bm.verts.new(at(p, c0)) for p in pts]
    hi = [bm.verts.new(at(p, c1)) for p in pts]
    faces = [bm.faces.new(lo[::-1]), bm.faces.new(hi)]
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        faces.append(bm.faces.new((lo[i], lo[j], hi[j], hi[i])))
    mk.paint(bm, faces, pigment, uv)


def _arc(cx, cz, r, a0_deg, a1_deg, n):
    """n+1 points on a circle, a0 -> a1 degrees (0 deg = +x, 90 deg = up)."""
    return [(cx + r * math.cos(math.radians(a0_deg + (a1_deg - a0_deg) * i / n)),
             cz + r * math.sin(math.radians(a0_deg + (a1_deg - a0_deg) * i / n))) for i in range(n + 1)]


def _torus_flat(bm, uv, pigment, radius, tube, segments=28, sides=6):
    """A horizontal ring about the Z axis, centred on the origin."""
    verts = []
    for i in range(segments):
        a = 2 * math.pi * i / segments
        ring = []
        for k in range(sides):
            t = 2 * math.pi * (k + 0.5) / sides
            rr = radius + tube * math.cos(t)
            ring.append(bm.verts.new((rr * math.cos(a), rr * math.sin(a), tube * math.sin(t))))
        verts.append(ring)
    faces = []
    for i in range(segments):
        i2 = (i + 1) % segments
        for k in range(sides):
            k2 = (k + 1) % sides
            faces.append(bm.faces.new((verts[i][k], verts[i2][k], verts[i2][k2], verts[i][k2])))
    mk.paint(bm, faces, pigment, uv)


# ══ LairCellar ═════════════════════════════════════════════════════════════

FLOOR = 0.30            # walkable floor, scale.md
HX, HY = 7.0, 5.0       # interior half-extents: 14 x 10 m
WALL = 0.8              # long-wall thickness
END = 1.2               # end-wall thickness
SPRING = FLOOR + 2.6    # vault springing, 2.6 m above the floor
CROWN = FLOOR + 4.2     # vault crown, 4.2 m above the floor
RISE = CROWN - SPRING
VR = (HY ** 2 + RISE ** 2) / (2 * RISE)     # vault radius (segmental barrel)
VZC = CROWN - VR                            # its centre height
WALL_TOP = 3.6
DOOR_CX, DOOR_W, DOOR_H = -0.7, 2.6, 2.16   # the Market door: 2.6 m wide, 2.16 m clear (scale.md's tightest archway)
DOOR_RISE = 0.5
HEARTH_X = 3.5
RECESS_W, RECESS_SPRING = 3.2, 2.3          # portal recess: half-width 1.6, springs 2.0 m above the floor


def _vault_z(y):
    return VZC + math.sqrt(max(VR ** 2 - y ** 2, 0.0))


def _vault(bm, uv):
    a0 = math.asin(HY / VR)
    n = 12
    step = 2 * a0 / n
    for i in range(n):
        th = -a0 + (i + 0.5) * step
        rr = VR + 0.25
        _sb(bm, uv, STONE, (0, rr * math.sin(th), VZC + rr * math.cos(th)),
            (2 * (HX + 1.1), rr * step * 1.2, 0.5), rot=Euler((-th, 0, 0)))
    # transverse ribs and the pilasters they spring from
    for x in (-5.5, 1.3, 6.1):
        for i in range(n):
            th = -a0 + (i + 0.5) * step
            rr = VR - 0.075
            _sb(bm, uv, DRESSED, (x, rr * math.sin(th), VZC + rr * math.cos(th)),
                (0.55 - 0.02 * (i % 2), rr * step * 1.25, 0.25), rot=Euler((-th, 0, 0)))
        for s in (-1, 1):
            _box(bm, uv, DRESSED, (x, s * (HY - 0.15), FLOOR + 1.3), (0.55, 0.32, 2.6))
            _box(bm, uv, DRESSED, (x, s * (HY - 0.2), SPRING - 0.1), (0.7, 0.42, 0.16))


def _door_wall(bm, uv):
    """The north (back) wall, with the low Market door notched through it."""
    hw = DOOR_W / 2
    top = FLOOR + DOOR_H
    dr = (hw ** 2 + DOOR_RISE ** 2) / (2 * DOOR_RISE)
    dzc = top - dr
    alpha = math.degrees(math.asin(hw / dr))
    arch = _arc(DOOR_CX, dzc, dr, 90 + alpha, 90 - alpha, 10)
    x0, x1 = DOOR_CX - hw, DOOR_CX + hw
    z0 = FLOOR - 0.05
    pts = [(-HX - 0.1, z0), (x0, z0)] + arch + [(x1, z0), (HX + 0.1, z0), (HX + 0.1, WALL_TOP), (-HX - 0.1, WALL_TOP)]
    _prism(bm, uv, STONE, pts, "y", HY, HY + WALL)
    # the door leaf, set back in the reveal: oak planks, iron straps
    leaf = [(x0 - 0.04, FLOOR)] + _arc(DOOR_CX, dzc, dr + 0.04, 90 + alpha, 90 - alpha, 10) + [(x1 + 0.04, FLOOR)]
    _prism(bm, uv, OAK, leaf, "y", HY + 0.36, HY + 0.5)
    for z in (FLOOR + 0.75, FLOOR + 1.6):
        _sb(bm, uv, IRON, (DOOR_CX, HY + 0.34, z), (DOOR_W - 0.1, 0.05, 0.1))
    for dx in (-0.65, 0.0, 0.65):
        _sb(bm, uv, LEATHER, (DOOR_CX + dx, HY + 0.355, FLOOR + 0.95), (0.025, 0.02, 1.2))
    _sb(bm, uv, GOLD, (DOOR_CX + 0.8, HY + 0.33, FLOOR + 1.0), (0.1, 0.04, 0.1))
    # the dressed arch ring round the doorway, on the room face
    ring_r0, ring_r1 = dr, dr + 0.26
    m = 9
    for i in range(m):
        a_a = 90 + alpha - (2 * alpha) * i / m + 0.8
        a_b = 90 + alpha - (2 * alpha) * (i + 1) / m - 0.8
        p = [(DOOR_CX + ring_r0 * math.cos(math.radians(a)), dzc + ring_r0 * math.sin(math.radians(a))) for a in (a_a, a_b)]
        q = [(DOOR_CX + ring_r1 * math.cos(math.radians(a)), dzc + ring_r1 * math.sin(math.radians(a))) for a in (a_b, a_a)]
        _prism(bm, uv, DRESSED if i % 2 == 0 else STONE, p + q, "y", HY - 0.1, HY + 0.1)
    # jamb stones to the springing
    for sx in (-1, 1):
        for k in range(3):
            zz = FLOOR + 0.3 + k * 0.55
            _box(bm, uv, DRESSED if k % 2 == 0 else STONE,
                 (DOOR_CX + sx * (hw + 0.13), HY - 0.04, zz), (0.28, 0.2, 0.5))


def _end_plate(bm, uv, x0, x1, notch):
    """An end wall plate: the barrel's cross-section filled up to the vault, with
    an optional arched notch rising from the floor."""
    a0 = math.asin(HY / VR)
    n = 14
    arc = [(HY - 2 * HY * i / n, _vault_z(HY - 2 * HY * i / n) + 0.15) for i in range(n + 1)]
    z0 = FLOOR - 0.05
    pts = [(-HY - WALL, z0)]
    if notch:
        w = RECESS_W / 2
        pts += [(-w, z0), (-w, RECESS_SPRING)] + _arc(0, RECESS_SPRING, w, 180, 0, 12)[1:-1] \
            + [(w, RECESS_SPRING), (w, z0)]
    pts += [(HY + WALL, z0), (HY + WALL, arc[0][1])] + arc[1:-1] + [(-HY - WALL, arc[-1][1])]
    _prism(bm, uv, STONE, pts, "x", x0, x1)


def _hearth(bm, uv):
    cx = HEARTH_X
    y0, y1 = 3.9, HY + 0.05          # breast front face, and into the wall
    ym = (y0 + y1) / 2
    d = y1 - y0
    fire_w, fire_top = 1.2, 1.4
    for s in (-1, 1):
        _box(bm, uv, DRESSED, (cx + s * (fire_w / 2 + 0.45), ym, (FLOOR + 1.88) / 2), (0.9, d, 1.88 - FLOOR))
    _box(bm, uv, DRESSED, (cx, ym, (fire_top + 1.95) / 2), (3.0, d, 1.95 - fire_top))
    _box(bm, uv, STONE, (cx, (4.2 + y1) / 2, (1.95 + 3.7) / 2), (2.4, y1 - 4.2, 3.7 - 1.95))   # the chimney breast narrows above the mantel
    _box(bm, uv, DRESSED, (cx, ym - 0.1, 2.02), (3.4, d + 0.2, 0.14))                          # mantel shelf
    _box(bm, uv, FLAG, (cx, 4.1, FLOOR + 0.05), (2.0, 1.8, 0.1))                                # hearthstone
    _sb(bm, uv, JOINT, (cx, HY - 0.04, (FLOOR + fire_top) / 2 + 0.05), (fire_w, 0.1, fire_top - FLOOR + 0.1))   # soot-black firebox back
    _sb(bm, uv, EMBER, (cx, 4.5, FLOOR + 0.12), (0.9, 0.5, 0.03))                                # the bed of embers
    for dz, dy in ((0.0, 0.0), (0.12, 0.08)):
        _cyl(bm, uv, LEATHER, (cx, 4.45 + dy, FLOOR + 0.2 + dz), 0.07, 0.95, rot=Euler((0, math.pi / 2, 0)), segments=6)
    for dx, h in ((-0.3, 0.5), (0.0, 0.7), (0.28, 0.45)):
        _cyl(bm, uv, GOLD if h > 0.6 else EMBER, (cx + dx, 4.5, FLOOR + 0.42 + h / 2), 0.1, h, segments=6, radius2=0.0)


def _flags(bm, uv):
    z = FLOOR + 0.004
    for y in (-3.5, -1.75, 0.0, 1.75, 3.5):
        _sb(bm, uv, JOINT, (0, y, z), (2 * HX, 0.03, 0.016))
    for x in (-5.25, -3.5, -1.75, 0.0, 1.75, 3.5, 5.25):
        _sb(bm, uv, JOINT, (x, 0, z + 0.001), (0.03, 2 * HY, 0.016))


def build_lair_cellar(bm, uv):
    ox, oy = HX + END, HY + WALL
    _box(bm, uv, FLAG, (0, 0, FLOOR / 2), (2 * ox, 2 * oy, FLOOR))
    # NB: pieces that stack in one plane (ribs, arch courses) differ by a hair in
    # width or depth, because Cycles self-shadows coplanar overlapping faces black.
    _flags(bm, uv)
    _door_wall(bm, uv)
    _box(bm, uv, STONE, (0, -HY - WALL / 2, (FLOOR - 0.05 + WALL_TOP) / 2), (2 * (HX + 0.1), WALL, WALL_TOP - FLOOR + 0.05))
    _vault(bm, uv)
    # end walls: the east one plain; the west one in two layers so the portal
    # recess is a real niche 0.7 m deep with a back wall behind it
    _end_plate(bm, uv, HX, HX + END, notch=False)
    _end_plate(bm, uv, -HX - 0.7, -HX, notch=True)
    _end_plate(bm, uv, -HX - END, -HX - 0.65, notch=False)
    _hearth(bm, uv)


# ══ props ══════════════════════════════════════════════════════════════════

def build_lair_portal_arch(bm, uv):
    """Freestanding stone arch, 2.6 m wide x 3.2 m tall x 0.5 m deep, on a threshold
    slab carrying the four portal stones (Ready up: a hand on each)."""
    _box(bm, uv, DRESSED, (0, 0, 0.05), (2.8, 1.0, 0.1))
    for i, x in enumerate((-0.9, -0.3, 0.3, 0.9)):
        _sb(bm, uv, PORTAL, (x, -0.38, 0.12), (0.4, 0.3, 0.05))
    spring = 1.9
    for s in (-1, 1):
        for k in range(3):
            _box(bm, uv, DRESSED if k % 2 == 0 else STONE, (s * 1.125, 0, 0.1 + 0.3 + k * 0.6), (0.35, 0.5 + 0.03 * (k % 2), 0.6))   # alternate depths: no coplanar faces
        _box(bm, uv, DRESSED, (s * 1.1, 0, spring - 0.03), (0.5, 0.58, 0.12))
    m = 11
    for i in range(m):
        a_a = 180 - 180 * i / m + 0.9
        a_b = 180 - 180 * (i + 1) / m - 0.9
        p = [(0.95 * math.cos(math.radians(a)), spring + 0.95 * math.sin(math.radians(a))) for a in (a_a, a_b)]
        q = [(1.3 * math.cos(math.radians(a)), spring + 1.3 * math.sin(math.radians(a))) for a in (a_b, a_a)]
        d = 0.25 + 0.015 * (i % 2)
        if i == m // 2:                    # the keystone: deeper, in the lightest stone
            d, pig = 0.29, VELLUM
        else:
            pig = DRESSED if i % 2 == 0 else STONE
        _prism(bm, uv, pig, p + q, "y", -d, d)


def build_lair_ledger_table(bm, uv):
    """Long oak table, 3.2 x 1.0 m, top at 0.80 m. Open underneath lengthwise, so the
    strongboxes slide in at its foot."""
    h, top = 0.80, 0.08
    for i, y in enumerate((-0.375, -0.125, 0.125, 0.375)):
        _box(bm, uv, OAK if i % 2 == 0 else LEATHER, (0, y, h - top / 2), (3.2, 0.25, top))
    for sy in (-1, 1):
        _box(bm, uv, OAK, (0, sy * 0.42, h - top - 0.05), (2.8, 0.05, 0.1))
    for sx in (-1, 1):
        _box(bm, uv, OAK, (sx * 1.48, 0, h - top - 0.05), (0.05, 0.76, 0.1))
        _box(bm, uv, OAK, (sx * 1.48, 0, 0.2), (0.06, 0.76, 0.06))
        for sy in (-1, 1):
            _box(bm, uv, OAK, (sx * 1.48, sy * 0.38, (h - top) / 2), (0.12, 0.12, h - top))


def build_lair_ledger(bm, uv):
    """The great ledger, open: 0.72 x 0.52 m. Four columns, one per wizard: a divider
    strip between each pair, a heading of one to four bars (I..IV) and ruled lines."""
    _sb(bm, uv, LEATHER, (0, 0, 0.015), (0.72, 0.52, 0.03))
    for s in (-1, 1):
        cx = s * 0.17
        _sb(bm, uv, "vellum_dim", (cx, 0, 0.04), (0.33, 0.47, 0.03))
        _sb(bm, uv, VELLUM, (cx, 0, 0.0575), (0.33, 0.47, 0.015))
    _sb(bm, uv, "vellum_faint", (0, 0, 0.066), (0.012, 0.47, 0.004))      # the gutter
    for x in (-0.17, 0.17):
        _sb(bm, uv, "vellum_faint", (x, 0, 0.066), (0.006, 0.44, 0.004))   # column dividers
    for ci, cx in enumerate((-0.25, -0.09, 0.09, 0.25)):
        for b in range(ci + 1):                                            # heading: I, II, III, IIII
            _sb(bm, uv, "bone_black", (cx - ci * 0.0075 + b * 0.015, 0.2, 0.0675), (0.007, 0.03, 0.006))
        for k in range(7):
            _sb(bm, uv, "vellum_dim", (cx, 0.14 - k * 0.05, 0.0665), (0.13, 0.005, 0.003))
    _sb(bm, uv, EMBER, (0.0, -0.3, 0.0665), (0.012, 0.18, 0.003))          # ribbon marker


def build_lair_strongbox(bm, uv):
    """Iron-banded oak strongbox, 0.6 x 0.4 x 0.4 m, with a domed lid."""
    _box(bm, uv, OAK, (0, 0, 0.13), (0.6, 0.4, 0.26))
    lid = [(-0.2, 0.25)] + _arc(0, 0.15, 0.25, 143.13, 36.87, 8) + [(0.2, 0.25)]
    _prism(bm, uv, OAK, lid, "x", -0.3, 0.3)
    for x in (-0.2, 0.2):
        _sb(bm, uv, IRON, (x, 0, 0.13), (0.06, 0.412, 0.262))
        band = [(-0.206, 0.25)] + _arc(0, 0.15, 0.256, 143.13, 36.87, 8) + [(0.206, 0.25)]
        _prism(bm, uv, IRON, band, "x", x - 0.03, x + 0.03)
    _sb(bm, uv, GOLD, (0, -0.205, 0.25), (0.1, 0.02, 0.12))
    _sb(bm, uv, IRON, (0, -0.21, 0.22), (0.05, 0.02, 0.06))


def build_lair_century_dial_stand(bm, uv):
    """The orrery's tripod, 1.3 m to the axle top, on a round plinth (the base)."""
    _cyl(bm, uv, OAK, (0, 0, 0.025), 0.75, 0.05, segments=12)
    hub = 1.0
    for i in range(3):
        a = math.radians(120 * i + 90)
        _limb(bm, uv, OAK, (0.62 * math.cos(a), 0.62 * math.sin(a), 0.04), (0.05 * math.cos(a), 0.05 * math.sin(a), hub), 0.035)
        _sb(bm, uv, BRASS, (0.62 * math.cos(a), 0.62 * math.sin(a), 0.07), (0.1, 0.1, 0.05))
    _cyl(bm, uv, BRASS, (0, 0, hub + 0.03), 0.07, 0.1, segments=8)
    _cyl(bm, uv, BRASS, (0, 0, 1.15), 0.014, 0.32, segments=6)     # the axle
    _cyl(bm, uv, BRASS, (0, 0, 1.31), 0.03, 0.03, segments=8)      # its cap: top of the axle at Z = 1.32


_RING_RADII = {1: 0.30, 2: 0.42, 3: 0.54, 4: 0.66}
_RING_BEAD = {1: "verdigris", 2: "lapis", 3: "madder", 4: "orpiment"}


def _dial_ring(bm, uv, n):
    r = _RING_RADII[n]
    _torus_flat(bm, uv, BRASS, r, 0.026)
    for k in range(4):                    # four lugs, where the ring would carry its pins
        a = math.pi / 2 * k
        _sb(bm, uv, BRASS, (r * math.cos(a), r * math.sin(a), 0.0), (0.05, 0.05, 0.05), rot=Euler((0, 0, a)))
    a = math.radians(45)                  # the Age's coloured bead (the planet in the concept)
    mk.paint(bm, mk.add_sphere(bm, 0.028, loc=(r * math.cos(a), r * math.sin(a), 0.0), segments=8, rings=5), _RING_BEAD[n], uv)


def build_lair_century_dial_ring_1(bm, uv):
    _dial_ring(bm, uv, 1)


def build_lair_century_dial_ring_2(bm, uv):
    _dial_ring(bm, uv, 2)


def build_lair_century_dial_ring_3(bm, uv):
    _dial_ring(bm, uv, 3)


def build_lair_century_dial_ring_4(bm, uv):
    _dial_ring(bm, uv, 4)


def build_lair_weapon_rack(bm, uv):
    """Oak rack, 1.6 m wide x 0.5 m deep x 1.72 m tall: two posts, three rails, a low
    shelf and a row of iron pegs. Stands against a wall; the weapons are placed on it."""
    for s in (-1, 1):
        _box(bm, uv, OAK, (s * 0.76, 0, 0.035), (0.1, 0.5, 0.07))           # foot, wider than the post: no coplanar faces
        _box(bm, uv, OAK, (s * 0.76, 0, 0.86), (0.08, 0.08, 1.72))          # post
    for z in (0.55, 1.0, 1.55):
        _box(bm, uv, OAK, (0, 0, z), (1.5, 0.06, 0.06))   # ends sunk in the posts
    _box(bm, uv, OAK, (0, 0, 1.69), (1.7, 0.1, 0.06))                       # cap
    _box(bm, uv, LEATHER, (0, 0.05, 0.35), (1.5, 0.3, 0.04))               # low shelf
    for x in (-0.5, -0.17, 0.17, 0.5):
        _cyl(bm, uv, STEEL, (x, -0.1, 1.3), 0.015, 0.2, rot=Euler((math.pi / 2, 0, 0)), segments=6)


def build_lair_candle(bm, uv):
    """A candle in an iron holder: round foot, stem, drip dish, spike; 0.28 m tall."""
    _cyl(bm, uv, IRON, (0, 0, 0.0075), 0.08, 0.015, segments=10)
    _cyl(bm, uv, IRON, (0, 0, 0.04), 0.012, 0.07, segments=6)
    _cyl(bm, uv, IRON, (0, 0, 0.075), 0.05, 0.012, segments=10)
    _cyl(bm, uv, IRON, (0, 0, 0.1), 0.006, 0.06, segments=6)
    _cyl(bm, uv, VELLUM, (0, 0, 0.16), 0.02, 0.15, segments=8)
    _cyl(bm, uv, "bone_black", (0, 0, 0.245), 0.004, 0.03, segments=6)
    _cyl(bm, uv, GOLD, (0, 0, 0.275), 0.016, 0.05, segments=8, radius2=0.004)
    _sb(bm, uv, IRON, (0.07, 0, 0.05), (0.025, 0.012, 0.09))               # carrying ring's stem
