"""
Late Medieval CurtainWall pieces: LateBarbican, LateWallStraight, LateWallCorner, LateBastion, LateDrawbridge.

Built from the room sheets in docs/art/rooms/ (spec: docs/art/rooms/data/LateMedieval/<Key>.json,
drawing: docs/art/rooms/concept/LateMedieval/<Key>.svg). The sheet is the reference: the
dimensions, placements and loot anchors here match it. Palette, zone tables and
room_shell come from castle_builders_late.py; the rules are in its docstring and in
docs/plans/era-castle-rooms.md.
"""
from castle_builders_late import *  # noqa: F401,F403  palette, room_shell, cb, ek, mk, rk, math, Euler


# ── CurtainWall: machicolated sandstone ────────────────────────────────
#
# Every Late wall piece keeps its outer face FACE_INSET (0.45 m) inside the
# cell edge; the corbels carry the parapet back out to the cell line, so the
# machicolated crown stays inside the cell and neighbouring pieces meet. A run is
# described along its wall (u) and inward from the cell edge (d), as in the
# Bronze curtain file. The numbers match the sheets' _late_wall.py.

H = rk.HALF
FACE = FACE_INSET
MASS = 1.6
WALK_Z = 4.0
FRIEZE = (3.0, 3.4)
CORBEL = (3.4, 3.7, 4.0)
CORBEL_PITCH, CORBEL_W = 0.9, 0.3
PARAPET = (0.05, 0.33)
LOW_TOP, MERLON_TOP = 4.6, 5.2
MERLON_PITCH, MERLON_W = 1.3, 0.8
TIMBER_WALK = (2.05, 3.05, 3.9)
LOOP_Z = 1.0


def _at(side, u, d, z):
    return (u, -H + d, z) if side == "south" else (-H + d, u, z)


def _size(side, su, sd, sz):
    return (su, sd, sz) if side == "south" else (sd, su, sz)


def _centres(u0, u1, pitch):
    n = int((u1 - u0 + 1e-6) // pitch)
    mid = (u0 + u1) / 2
    return [mid - (n - 1) * pitch / 2 + k * pitch for k in range(n)]


def _box(bm, uv, pigment, side, u0, u1, d0, d1, z0, z1):
    cb._box(bm, uv, pigment, _at(side, (u0 + u1) / 2, (d0 + d1) / 2, (z0 + z1) / 2),
            _size(side, u1 - u0, d1 - d0, z1 - z0))


def keyhole_loop(bm, uv, side, u, z0=LOOP_Z, d=FACE):
    """A keyhole gun-loop on an outer face at d: a soot slit over a round port."""
    _box(bm, uv, SOOT, side, u - 0.05, u + 0.05, d - 0.03, d + 0.01, z0 + 0.3, z0 + 1.2)
    rot = Euler((math.radians(90), 0, 0)) if side == "south" else Euler((0, math.radians(90), 0))
    mk.paint(bm, mk.add_cylinder(bm, 0.1, 0.04, loc=_at(side, u, d - 0.01, z0 + 0.15), rot=rot, segments=8), SOOT, uv)


def crown(bm, uv, u0, u1, side="south", base=0.0, d_face=FACE):
    """The machicolated crown of a run: brick frieze on the face, two-stage corbels, the
    parapet carried out on them with merlons, all `base` higher (a tower's crown)."""
    _box(bm, uv, BRICK, side, u0, u1, d_face - 0.03, d_face + 0.01, base + FRIEZE[0], base + FRIEZE[1])
    for u in _centres(u0, u1, CORBEL_PITCH):
        _box(bm, uv, WALL, side, u - CORBEL_W / 2, u + CORBEL_W / 2, d_face - 0.2, d_face + 0.02,
             base + CORBEL[0], base + CORBEL[1])
        _box(bm, uv, WALL, side, u - CORBEL_W / 2 - 0.03, u + CORBEL_W / 2 + 0.03, PARAPET[0], d_face + 0.04,
             base + CORBEL[1], base + CORBEL[2])
    _box(bm, uv, WALL, side, u0, u1, PARAPET[0], PARAPET[1], base + CORBEL[2], base + LOW_TOP)
    for u in _centres(u0, u1, MERLON_PITCH):
        _box(bm, uv, WALL, side, u - MERLON_W / 2, u + MERLON_W / 2, PARAPET[0], PARAPET[1], base + LOW_TOP,
             base + MERLON_TOP)


def late_run(bm, uv, u0, u1, side="south", loops=(), timber=True):
    """A standard Late run from u0 to u1: the sandstone mass, its crown, keyhole loops and
    the timber walk on stone corbels along the inner face."""
    _box(bm, uv, WALL, side, u0, u1, FACE, FACE + MASS, 0.0, WALK_Z)
    crown(bm, uv, u0, u1, side)
    for u in loops:
        keyhole_loop(bm, uv, side, u)
    if timber:
        d0, d1, under = TIMBER_WALK
        _box(bm, uv, TIMBER, side, u0, u1, d0, d1, under, WALK_Z)
        for u in _centres(u0, u1, 1.5):
            _box(bm, uv, WALL, side, u - 0.15, u + 0.15, d0 - 0.02, d0 + 0.4, under - 0.5, under)


def build_late_wall_straight(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateWallStraight.svg"""
    late_run(bm, uv, -H, H, loops=(-3.6, 0.0, 3.6))


OCT = Euler((0, 0, math.radians(22.5)))    # an 8-sided cylinder turned so its flats face the axes


def octagon(bm, uv, pigment, cx, cy, z0, z1, r, r_top=None):
    """An octagonal prism (or frustum) with flats facing the axes, circumradius r."""
    return mk.paint(bm, mk.add_cylinder(bm, r, z1 - z0, loc=(cx, cy, (z0 + z1) / 2), rot=OCT, segments=8,
                                        radius2=r if r_top is None else r_top), pigment, uv)


def octagonal_tower(bm, uv, cx, cy, r=2.2, lift=1.6, roof=2.4, outward=(), doors=()):
    """An octagonal sandstone tower with a machicolated crown and a conical tile roof: the
    drum to lift + frieze, a brick frieze ring, corbels on the `outward` flats (angles in
    degrees, 0 = east), a parapet ring to lift + LOW_TOP, the roof on it; soot doorways
    on the `doors` flats at walk level."""
    apo = r * math.cos(math.radians(22.5))
    octagon(bm, uv, WALL, cx, cy, 0.0, lift + FRIEZE[0], r)
    octagon(bm, uv, BRICK, cx, cy, lift + FRIEZE[0], lift + FRIEZE[1], r + 0.03)
    rp = 2.6
    ap = rp * math.cos(math.radians(22.5))
    for ang in outward:
        t = math.radians(ang)
        c, s = math.cos(t), math.sin(t)
        for (d0, d1, z0, z1, w) in ((apo - 0.02, apo + 0.2, CORBEL[0], CORBEL[1], 0.3),
                                    (apo - 0.02, ap - 0.02, CORBEL[1], CORBEL[2], 0.36)):
            dm = (d0 + d1) / 2
            mk.paint(bm, mk.add_box(bm, (d1 - d0, w, z1 - z0), loc=(cx + c * dm, cy + s * dm, lift + (z0 + z1) / 2),
                                    rot=Euler((0, 0, t))), WALL, uv)
    octagon(bm, uv, WALL, cx, cy, lift + CORBEL[1], lift + LOW_TOP, rp)
    octagon(bm, uv, ROOF, cx, cy, lift + LOW_TOP, lift + LOW_TOP + roof, 2.65, r_top=0.02)
    mk.paint(bm, mk.add_cylinder(bm, 0.02, 0.3, loc=(cx, cy, lift + LOW_TOP + roof + 0.13), segments=4), IRON, uv)
    for ang in doors:
        t = math.radians(ang)
        c, s = math.cos(t), math.sin(t)
        mk.paint(bm, mk.add_box(bm, (0.06, 0.9, 1.9), loc=(cx + c * (apo + 0.02), cy + s * (apo + 0.02), WALK_Z + 0.95),
                                rot=Euler((0, 0, t))), SOOT, uv)


def build_late_wall_corner(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateWallCorner.svg"""
    r = 2.2
    apo = r * math.cos(math.radians(22.5))
    c = -H + FACE + apo
    run0 = c + r * math.sin(math.radians(22.5))
    octagonal_tower(bm, uv, c, c, r=r, outward=(180, 225, 270, 135, 315), doors=(0, 90))
    keyhole_loop(bm, uv, "south", c)
    keyhole_loop(bm, uv, "west", c)
    for side in ("south", "west"):
        late_run(bm, uv, run0, H, side=side, loops=(2.1,))


def build_late_bastion(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateBastion.svg"""
    n, r = 16, 3.4
    apo = r * math.cos(math.radians(180 / n))
    cy = -H + FACE + apo
    plat = WALK_Z
    # The drum, sixteen flats (turned so a flat faces south), its platform flagged.
    mk.paint(bm, mk.add_cylinder(bm, r, plat, loc=(0, cy, plat / 2), rot=Euler((0, 0, math.radians(180 / n))),
                                 segments=n), WALL, uv)
    ek.disc(bm, uv, "vellum_faint", 0, cy, plat, 2.45, thickness=0.02, segments=n)
    # The parapet ring: one block per flat, open at the walk entries; a low sill in the embrasure.
    gaps, embrasure = {202.5, 225.0, 315.0, 337.5}, 270.0
    r0, r1, top = 2.5, r, plat + 1.0
    for k in range(n):
        ang = 22.5 * k
        if ang in gaps:
            continue
        t = math.radians(ang)
        chord = 2 * r1 * math.sin(math.radians(11.25)) + 0.14
        rm = (r0 + r1) / 2 - 0.03
        z1 = plat + 0.4 if ang == embrasure else top + (0.03 if k % 2 else 0.0)   # no two overlapping tops coplanar
        mk.paint(bm, mk.add_box(bm, (r1 - r0 - 0.06, chord, z1 - plat), loc=(math.cos(t) * rm, cy + math.sin(t) * rm,
                                                                        (plat + z1) / 2), rot=Euler((0, 0, t))), WALL, uv)
    # Five gunports in the base, on the outward flats.
    for ang in (225.0, 247.5, 270.0, 292.5, 315.0):
        t = math.radians(ang)
        mk.paint(bm, mk.add_box(bm, (0.06, 0.3, 0.3), loc=(math.cos(t) * (apo + 0.01), cy + math.sin(t) * (apo + 0.01),
                                                           0.75), rot=Euler((0, 0, t))), SOOT, uv)
    # The bombard on its bed, muzzle south in the embrasure; gunstones beside it.
    by = cy - 1.1
    cb._box(bm, uv, TIMBER, (0, by, plat + 0.15), (0.7, 1.8, 0.3))
    along_y = Euler((math.radians(90), 0, 0))
    mk.paint(bm, mk.add_cylinder(bm, 0.2, 0.5, loc=(0, by + 0.55, plat + 0.52), rot=along_y, segments=10), IRON, uv)
    mk.paint(bm, mk.add_cylinder(bm, 0.3, 1.2, loc=(0, by - 0.3, plat + 0.6), rot=along_y, segments=12), IRON, uv)
    for dy in (-0.7, -0.1):
        mk.paint(bm, mk.add_cylinder(bm, 0.32, 0.06, loc=(0, by + dy, plat + 0.6), rot=along_y, segments=12), "line", uv)
    for x, y in ((1.2, cy - 0.4), (1.6, cy - 0.1), (1.35, cy + 0.25)):
        ek.sphere(bm, uv, "vellum_faint", x, y, plat + 0.02, 0.2, segments=8, rings=5)
    # The machicolated runs on from both flanks.
    late_run(bm, uv, -H, -2.6, loops=(-4.45,))
    late_run(bm, uv, 2.6, H, loops=(4.45,))


def build_late_drawbridge(bm, uv):
    """docs/art/rooms/concept/LateMedieval/LateDrawbridge.svg"""
    g, jamb, gate_h = 1.5, 0.4, 3.4
    # The wall: two masses, dressed jambs, a lintel carrying the walk over the gate; the crown and
    # timber walk run on unbroken.
    for u0, u1 in ((-H, -g - jamb), (g + jamb, H)):
        _box(bm, uv, WALL, "south", u0, u1, FACE, FACE + MASS, 0.0, WALK_Z)
    for sgn in (-1, 1):
        a, b = sorted((sgn * g, sgn * (g + jamb)))
        _box(bm, uv, "vellum_faint", "south", a, b, FACE, FACE + MASS, 0.0, gate_h)
    _box(bm, uv, WALL, "south", -g - jamb, g + jamb, FACE, FACE + MASS, gate_h, WALK_Z)
    crown(bm, uv, -H, H)
    for u in (-4.2, 4.2):
        keyhole_loop(bm, uv, "south", u)
    d0, d1, under = TIMBER_WALK
    _box(bm, uv, TIMBER, "south", -H, H, d0, d1, under, WALK_Z)
    for u in _centres(-H, H, 1.5):
        if abs(u) > g + 0.2:
            _box(bm, uv, WALL, "south", u - 0.15, u + 0.15, d0 - 0.02, d0 + 0.4, under - 0.5, under)
    # The deck, lowered, from the gate 5 m inward; an iron hinge bar at its south end.
    _box(bm, uv, TIMBER, "south", -g, g, 0.2, 5.2, 0.0, 0.22)
    _box(bm, uv, IRON, "south", -g, g, 0.2, 0.32, 0.22, 0.28)
    # The flèches: oak arms pivoting on posts on the walk, weighted at the south end,
    # chained at the north end to the deck's inner end.
    (s_d, s_z), (n_d, n_z), p_d = (0.15, 5.7), (5.2, 2.4), 1.2
    p_z = s_z + (n_z - s_z) * (p_d - s_d) / (n_d - s_d)
    length = math.hypot(n_d - s_d, n_z - s_z)
    tilt = math.atan2(n_z - s_z, n_d - s_d)
    for x in (-1.3, 1.3):
        mk.paint(bm, mk.add_box(bm, (0.2, length, 0.2), loc=(x, -H + (s_d + n_d) / 2, (s_z + n_z) / 2),
                                rot=Euler((tilt, 0, 0))), TIMBER, uv)
        cb._box(bm, uv, IRON, (x, -H + s_d + 0.15, s_z + 0.28), (0.45, 0.45, 0.45))
        for dx in (-0.2, 0.2):
            cb._box(bm, uv, TIMBER, (x + dx, -H + p_d, (WALK_Z + p_z + 0.1) / 2), (0.14, 0.14, p_z + 0.1 - WALK_Z))
        mk.paint(bm, mk.add_cylinder(bm, 0.04, 0.54, loc=(x, -H + p_d, p_z), rot=Euler((0, math.radians(90), 0)),
                                     segments=6), IRON, uv)
        mk.paint(bm, mk.add_cylinder(bm, 0.03, n_z - 0.22, loc=(x, -H + n_d, (n_z + 0.22) / 2), segments=6), IRON, uv)
