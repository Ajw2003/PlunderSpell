"""Late Medieval plunder (docs/art/data/late.json, items)."""

from __future__ import annotations

import math

from ..kit import Part, spline
from ..spec import Entry
from . import blueprint


# --------------------------------------------------------------------------------
# Shared helpers
# --------------------------------------------------------------------------------

def _radius_at(profile, z: float) -> float:
    """Linear interpolation of a monotonic-in-z lathe profile."""
    for (r0, z0), (r1, z1) in zip(profile, profile[1:]):
        if min(z0, z1) <= z <= max(z0, z1) and z1 != z0:
            return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
    raise ValueError(f"z={z} is outside the profile")


def _lathe(profile, mat, loc=(0.0, 0.0, 0.0), size=(1.0, 1.0, 1.0), segments=16,
           rot=(0.0, 0.0, 0.0), paint=None, bevel=False, mirror=False, **extras) -> Part:
    ex = {"profile": profile, "bevel": bevel, **extras}
    if paint:
        ex["paint"] = paint
    return Part("lathe", loc, size, mat=mat, segments=segments, rot=rot, mirror=mirror,
                extras=ex)


def _tube(path, section, mat, segments=4, up=None, closed=False, smooth=True,
          bevel=False, mirror=False) -> Part:
    ex = {"path": [tuple(p) for p in path], "section": section, "smooth": smooth,
          "bevel": bevel, "closed": closed}
    if up is not None:
        ex["up"] = up
    return Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, segments=segments, mirror=mirror,
                extras=ex)


def _band(mat, z0, z1, ymax=1.0, xmin=-1.0, xmax=1.0, ymin=-1.0):
    return {"mat": mat, "min": (xmin, ymin, z0 - 1e-4), "max": (xmax, ymax, z1 + 1e-4)}


# --------------------------------------------------------------------------------
# Parade armour on its stand
# --------------------------------------------------------------------------------

def parade_armour(entry: Entry):
    """White-steel parade harness on an oak post and cross foot: sallet with a gilt
    brow and a long tail, bevor, globose breastplate with three gilt-etched bands
    and a cusped plackart border, fluted pauldrons with gilt rims and crimson
    velvet straps, arms hanging to 0.95 m, gilt-edged fauld and two pointed
    tassets. No legs: the post shows below the tassets."""
    W, D, H = entry.dims                        # 0.72 × 0.72 × 1.90
    parts: list[Part] = []
    steel, gilt = "white_harness", "gilt_etching"

    # --- Stand: cross foot (two halved oak bars), collar block, post, iron yoke.
    foot_h = 0.08
    parts.append(Part("box", (0, 0, foot_h / 2), (W, 0.08, foot_h), mat="oak_stand"))
    parts.append(Part("box", (0, 0, foot_h / 2), (0.08, D, foot_h), mat="oak_stand"))
    parts.append(Part("box", (0, 0, foot_h + 0.05), (0.14, 0.14, 0.10), mat="oak_stand"))
    parts.append(Part("cyl", (0, 0, foot_h + 0.05), (0.022, 0.022, 0.17), mat="stand_iron",
                      rot=(90, 0, 0), segments=8, extras={"bevel": False}))
    post_top = foot_h + 1.40
    parts.append(Part("box", (0, 0, (foot_h + post_top) / 2), (0.06, 0.06, 1.40),
                      mat="oak_stand"))
    parts.append(Part("box", (0, 0, post_top - 0.02), (0.40, 0.04, 0.03), mat="stand_iron"))
    # Padded leather dummy torso inside the plates (seen only at the gaps) and
    # the leather neck under the bevor.
    parts.append(Part("box", (0, 0.0, 1.36), (0.28, 0.14, 0.24), mat="leathers",
                      extras={"bevel": False}))
    parts.append(Part("cyl", (0, 0.0, 1.555), (0.13, 0.12, 0.10), mat="leathers",
                      segments=12, extras={"bevel": False}))

    # --- Breastplate (with backplate): a lathe squashed front-to-back.
    sy, cy = 0.62, -0.006
    torso = [(0.150, 1.215), (0.160, 1.25), (0.185, 1.32), (0.203, 1.39), (0.207, 1.44),
             (0.196, 1.49), (0.165, 1.525), (0.110, 1.545), (0.070, 1.550)]
    parts.append(_lathe(torso, steel, loc=(0, cy, 0), size=(1, sy, 1), segments=24))

    def front(x: float, z: float, lift: float = 0.003) -> tuple:
        """A point on the breastplate's front (-Y) surface, `lift` proud of it."""
        r = _radius_at(torso, z)
        y = cy - sy * math.sqrt(max(r * r - x * x, 1e-6)) - lift
        return (x, y, z)

    # Three vertical gilt-etched bands (0.025 m) and fan flutes between them.
    for x in (-0.075, 0.0, 0.075):
        path = [front(x * (0.85 + 0.15 * (z - 1.24) / 0.26), z)
                for z in (1.235, 1.29, 1.35, 1.41, 1.47, 1.505)]
        parts.append(_tube(path, (0.003, 0.0145), gilt, segments=6, up=(0, -1, 0)))
    for x0, x1 in ((-0.030, -0.045), (-0.018, -0.028), (0.030, 0.045), (0.018, 0.028),
                   (-0.110, -0.135), (0.110, 0.135)):
        path = [front(x0 + (x1 - x0) * t, 1.235 + 0.23 * t, 0.001) for t in (0, 0.35, 0.7, 1.0)]
        parts.append(_tube(path, (0.0025, 0.0035), steel, segments=4, up=(0, -1, 0)))
    # Cusped plackart outlined in gilt: an ogee rising from the waist to a point.
    ogee = [(-0.150, 1.225), (-0.140, 1.30), (-0.110, 1.345), (-0.062, 1.36),
            (-0.030, 1.385), (-0.010, 1.415), (0.0, 1.435)]
    ogee = ogee + [(-x, z) for x, z in reversed(ogee[:-1])]
    parts.append(_tube([front(x, z, 0.004) for x, z in spline(ogee, 2)], (0.003, 0.007), gilt,
                       segments=4, up=(0, -1, 0)))

    # --- Fauld: three gilt-edged lames flaring over the hips.
    for i, (zt, zb, rt, rb) in enumerate(((1.235, 1.190, 0.152, 0.172),
                                          (1.200, 1.155, 0.166, 0.182),
                                          (1.165, 1.120, 0.176, 0.192))):
        prof = [(rb, zb), (rb - 0.001, zb + 0.009), (rt, zt)]
        parts.append(_lathe(prof, steel, loc=(0, cy, 0), size=(1, sy, 1), segments=24,
                            paint=[_band(gilt, zb, zb + 0.009)]))

    # --- Tassets: two pointed plates hanging to ~1.00 m, gilt border behind a
    # slightly smaller steel face.
    def tasset(inset: float) -> list[tuple]:
        w, h = 0.09 - inset, 0.24
        return [(-w, h - inset), (w, h - inset), (w, 0.07), (0.012, 0.0 + inset * 1.8),
                (-w + 0.01, 0.06)]
    for side in (1, -1):
        x = side * 0.085
        base = 0.975
        ry = 0.12 * 0.62 + 0.012
        for inset, mat, dy in ((0.0, gilt, 0.0), (0.008, steel, -0.004)):
            outline = [(side * px, pz) for px, pz in tasset(inset)]
            parts.append(Part("prism", (x, -ry + dy, base), (1, 1, 0.008 if inset else 0.010),
                              mat=mat, rot=(80, 0, side * 14.0),
                              extras={"outline": outline, "bevel": False}))

    # --- Pauldrons: fluted bells over the shoulders, three lames, gilt rim.
    pauldron = [(0.104, -0.200), (0.106, -0.184), (0.100, -0.181), (0.110, -0.140),
                (0.103, -0.137), (0.116, -0.095), (0.120, -0.060), (0.112, -0.020),
                (0.092, 0.015), (0.060, 0.038), (0.028, 0.048), (0.0, 0.050)]
    parts.append(_lathe(pauldron, steel, loc=(0.212, -0.004, 1.550), size=(1, 0.92, 1),
                        rot=(0, -16, 0), segments=18, mirror=True,
                        paint=[_band(gilt, -0.200, -0.184)]))
    # Crimson velvet straps over the shoulders with gilt buckles on the chest.
    for side in (1, -1):
        x = side * 0.135
        strap = [(x, 0.05, 1.548), (x, -0.02, 1.552)] + [front(x, z, 0.004)
                                                        for z in (1.52, 1.49, 1.455)]
        parts.append(_tube(strap, (0.003, 0.015), "velvet_facing", segments=4, up=(0, -1, 1)))
        bx, by, bz = front(x, 1.445, 0.008)
        parts.append(Part("box", (bx, by, bz), (0.036, 0.008, 0.026), mat=gilt,
                          extras={"bevel": False}))

    # --- Arms: rerebrace, fan couter and vambrace, gilt cuff band, to 0.95 m.
    arm = [(0.052, 0.950), (0.053, 0.972), (0.047, 0.976), (0.044, 1.06), (0.048, 1.17),
           (0.042, 1.19), (0.042, 1.25), (0.050, 1.27), (0.052, 1.44), (0.036, 1.46)]
    ax = 0.258
    parts.append(_lathe(arm, steel, loc=(ax, 0.0, 0.0), segments=12, mirror=True,
                        paint=[_band(gilt, 0.950, 0.972)]))
    parts.append(Part("sphere", (ax, 0.004, 1.222), (0.112, 0.108, 0.100), mat=steel,
                      segments=12, rings=6, mirror=True, extras={"bevel": False}))
    fan = [(0.0, -0.045), (0.030, -0.020), (0.045, 0.020), (0.030, 0.042), (0.0, 0.030),
           (-0.022, 0.040), (-0.034, 0.018), (-0.020, -0.020)]
    parts.append(Part("prism", (ax + 0.054, 0.004, 1.222), (1.15, 1.15, 0.008), mat=steel,
                      rot=(90, 0, 90), mirror=True, extras={"outline": fan, "bevel": False}))

    # --- Sallet: rounded skull, gilt brow band, dark sight-slit, keel, long tail;
    # bevor below it.
    hy = 0.012
    skull = [(0.121, 1.680), (0.123, 1.710), (0.123, 1.735), (0.123, 1.752),
             (0.119, 1.790), (0.106, 1.830), (0.080, 1.866), (0.045, 1.888), (0.0, 1.900)]
    parts.append(_lathe(skull, steel, loc=(0, hy, 0), size=(1, 1.08, 1), segments=24,
                        paint=[_band(gilt, 1.680, 1.710),
                               _band("stand_iron", 1.735, 1.752, ymax=-0.07,
                                     xmin=-0.066, xmax=0.066)]))
    keel = []
    for a in (-80, -55, -30, 0, 30, 55, 80):
        t = math.radians(a)
        z = 1.752 + (1.900 - 1.752) * math.cos(t) ** 0.8
        r = _radius_at(skull, z)
        keel.append((0.0, hy + 1.08 * r * math.sin(t) * 1.0 + (0.002 * math.sin(t)),
                     z + 0.003))
    parts.append(_tube(keel, (0.004, 0.004), steel, segments=4, up=(1, 0, 0)))
    # Tail: a flared skirt set back behind the skull and tipped down at the back,
    # so it sweeps out behind the neck and flares a little at the sides; its front
    # arc stays buried in the skull. Gilt edge continues the brow band.
    skirt = [(0.184, -0.044), (0.182, -0.032), (0.150, 0.000), (0.112, 0.036)]
    parts.append(_lathe(skirt, steel, loc=(0, 0.085, 1.690), size=(0.86, 1.0, 1),
                        rot=(-14, 0, 0), segments=24,
                        paint=[_band(gilt, -0.044, -0.032)]))
    bevor = [(0.078, 1.555), (0.094, 1.590), (0.101, 1.630), (0.100, 1.672), (0.094, 1.690)]
    parts.append(_lathe(bevor, steel, loc=(0, -0.026, 0), size=(1, 0.88, 1), segments=20))

    return blueprint(
        entry, parts,
        bevel=0.003,
        family_overrides={
            # Gilt rubbed back to bright steel where hands have admired it.
            gilt: {"wear_to": "#8E9194", "wear_amount": 0.22, "grain": 0.12},
            # The JSON's 0.18 (high points) to 0.4 (flutes), averaged: a single
            # 0.18 value mirrors the black studio and renders the plate near-black.
            steel: {"grain": 0.12, "rough": 0.30},
            "oak_stand": {"grain": 0.3},
        },
        notes=["Etched foliage in the gilt bands and the flute dust are texture/normal "
               "detail; EnemyForge's bake makes no normal map, so bands are flat gilt.",
               "Gauntlets omitted, as the JSON says (stolen last year)."],
    )


# --------------------------------------------------------------------------------
# Rolled tapestry
# --------------------------------------------------------------------------------

def _strip_outline(path, thickness: float) -> list[tuple]:
    """A 2D polyline thickened into a closed outline (for a cloth cross-section)."""
    left, right = [], []
    for i, (x, y) in enumerate(path):
        a = path[max(i - 1, 0)]
        b = path[min(i + 1, len(path) - 1)]
        tx, ty = b[0] - a[0], b[1] - a[1]
        n = math.hypot(tx, ty)
        nx, ny = -ty / n, tx / n
        left.append((x + nx * thickness / 2, y + ny * thickness / 2))
        right.append((x - nx * thickness / 2, y - ny * thickness / 2))
    return left + list(reversed(right))


def rolled_tapestry(entry: Entry):
    """A millefleur verdure rolled on itself and lying on the floor along X:
    spiral end faces, green field scattered with red / buff / cream sprigs, a
    linen wrapper round the middle third, four hemp lashings with tails, and
    0.6 m of the outer turn unrolled at one end showing the guard band and fringe."""
    import random
    L, _D, H = entry.dims                      # 3.40 × 0.45 × 0.45
    R, half = H / 2.0, L / 2.0
    cz = R
    parts: list[Part] = []
    rnd = random.Random(1461)

    # --- The roll: a lathe about the X axis. Each end face has two shallow insets
    # (the turns settling) and a hollow core 0.06 m across, 4 cm deep.
    e1, e2 = 0.0015, 0.0030                     # inset depths
    r1, r2 = 0.160, 0.095                       # inset radii
    core = 0.030
    prof = [(0.0, -half + 0.04), (core, -half + 0.04), (core, -half + e2), (r2, -half + e2),
            (r2, -half + e1), (r1, -half + e1), (r1, -half), (R - 0.004, -half),
            (R, -half + 0.006)]
    prof += [(R, half - 0.006), (R - 0.004, half), (r1, half), (r1, half - e1),
             (r2, half - e1), (r2, half - e2), (core, half - e2), (core, half - 0.04),
             (0.0, half - 0.04)]
    parts.append(_lathe(prof, "field_green", loc=(0, 0, cz), rot=(0, 90, 0), segments=24))

    # Spiral of the turns on both end faces: a thin brown cord of shading between
    # turns, stepping down the insets as it winds out.
    for side in (1, -1):
        pts = []
        turns, n = 5.0, 70
        for i in range(n + 1):
            t = i / n
            r = core + 0.008 + (R - 0.014 - core) * t
            ang = 2 * math.pi * turns * t * side
            depth = e2 if r < r2 else (e1 if r < r1 else 0.0)
            pts.append((side * (half - depth + 0.0012), math.cos(ang) * r, cz + math.sin(ang) * r))
        parts.append(_tube(pts, (0.0024, 0.0050), "wool_brown", segments=3, up=(side, 0, 0)))

    # --- Linen wrapper: a loose sleeve over the middle third (0.40 + 0.75 m in
    # from the left, 1.15 m long), with slack folds along it.
    lx0, lx1 = -half + 1.15, -half + 2.30
    sleeve = [(R + 0.002, lx0)]
    for i in range(1, 8):
        sleeve.append((R + (0.010 if i % 2 else 0.005), lx0 + (lx1 - lx0) * i / 8))
    sleeve.append((R + 0.002, lx1))
    parts.append(_lathe(sleeve, "linen_wrap", loc=(0, 0, cz), rot=(0, 90, 0), segments=22))
    # Mildew spots and stains on the linen.
    for i in range(9):
        x = lx0 + 0.1 + rnd.random() * (lx1 - lx0 - 0.2)
        a = math.radians(rnd.uniform(-95, 60))
        s = rnd.uniform(0.012, 0.03)
        outline = [(math.cos(2 * math.pi * k / 5) * s * rnd.uniform(0.7, 1.2),
                    math.sin(2 * math.pi * k / 5) * s * rnd.uniform(0.7, 1.2)) for k in range(5)]
        rr = R + 0.0085
        parts.append(Part("prism", (x, -math.sin(a) * rr, cz + math.cos(a) * rr), (1, 1, 0.003),
                          mat="wool_brown", rot=(math.degrees(a), 0, 0),
                          extras={"outline": outline, "bevel": False}))

    # --- Millefleur sprigs on the outer turn: small tufts at ~0.12-0.15 m spacing,
    # red, buff and cream, over the part of the roll that is not wrapped or tied.
    colours = ["wool_red", "wool_buff", "linen_wrap", "wool_buff", "wool_red"]
    x = -half + 0.07
    col = 0
    while x < half - 0.05:
        if lx0 - 0.03 < x < lx1 + 0.03:
            x += 0.13
            continue
        for a_deg in range(-110 + (col % 2) * 16, 150, 32):
            a = math.radians(a_deg + rnd.uniform(-5, 5))
            s = rnd.uniform(0.022, 0.032)
            spin = rnd.uniform(0, 2 * math.pi)
            tri = [(math.cos(spin + k * 2.0944) * s, math.sin(spin + k * 2.0944) * s)
                   for k in range(3)]
            rr = R + 0.0005
            px = x + rnd.uniform(-0.03, 0.03)
            parts.append(Part("prism", (px, -math.sin(a) * rr, cz + math.cos(a) * rr),
                              (1, 1, 0.003), mat=rnd.choice(colours),
                              rot=(math.degrees(a), 0, 0),
                              extras={"outline": tri, "bevel": False}))
        x += 0.15
        col += 1

    # --- Hemp ties at 0.40, 1.15, 2.30 and 3.05 m, knotted on the front-top with a
    # hanging 0.2 m tail. The last one has slipped loose.
    for k, along in enumerate((0.40, 1.15, 2.30, 3.05)):
        tx = -half + along
        loose = k == 3
        rr = R + 0.010 if lx0 - 0.01 <= tx <= lx1 + 0.01 else R + 0.004
        ring = []
        for i in range(14):
            a = 2 * math.pi * i / 14
            # The loose one stands off the top and front and skews along the roll;
            # underneath, the roll's weight still pins it to the floor.
            slack = 0.016 * max(0.0, math.cos(a - 0.5)) if loose else 0.0
            ring.append((tx + (0.03 * math.sin(a) if loose else 0.0),
                         -math.sin(a) * (rr + slack), cz + math.cos(a) * (rr + slack)))
        # Each lashing is two turns of the cord, side by side.
        for dx in (-0.0055, 0.0055):
            parts.append(_tube([(p[0] + dx, p[1], p[2]) for p in ring], (0.0055, 0.0055),
                               "hemp_cord", segments=4, closed=True))
        ka = math.radians(-38)
        kr = rr + (0.016 * math.cos(ka - 0.5) if loose else 0.0) + 0.006
        knot = (tx, -math.sin(ka) * kr, cz + math.cos(ka) * kr)
        parts.append(Part("sphere", knot, (0.028, 0.022, 0.022), mat="hemp_cord", segments=6,
                          rings=4, extras={"bevel": False}))
        tail = []
        length = 0.28 if loose else 0.20
        for i, a_deg in enumerate((-40, -58, -76, -92)):
            a = math.radians(a_deg)
            tail.append((tx + 0.01 * i, -math.sin(a) * (kr + 0.004), cz + math.cos(a) * (kr + 0.004)))
        # then hanging straight down off the front of the roll
        tail.append((tx + 0.04, tail[-1][1] - 0.02, tail[-1][2] - (length - 0.14)))
        parts.append(_tube(tail, (0.004, 0.004), "hemp_cord", segments=4, up=(1, 0, 0)))

    # --- Loose end: 0.6 m of the outer turn peeled off the front-bottom at the left
    # end, draping to the floor and lying flat, border and fringe at its free edge.
    fx0, fx1 = -half + 0.04, -half + 0.64
    drape = [(-0.10, 0.012), (-0.180, 0.070), (-0.214, 0.120)]      # (y, z) in world
    a0 = math.atan2(-0.214, 0.120 - cz)
    drape = [(-(R + 0.003) * math.sin(math.radians(d)), cz + (R + 0.003) * math.cos(math.radians(d)))
             for d in (120, 108)]
    drape += [(-0.236, 0.075), (-0.258, 0.030), (-0.300, 0.008), (-0.360, 0.004)]
    flat = [(-0.455, 0.006), (-0.545, 0.004), (-0.565, 0.0045), (-0.650, 0.007)]   # rippled
    path = drape + flat
    outline = _strip_outline([(y, z) for y, z in path], 0.007)
    band_y = -0.555
    parts.append(Part("prism", ((fx0 + fx1) / 2, 0, 0), (1, 1, fx1 - fx0), mat="field_green",
                      rot=(90, 0, 90),
                      extras={"outline": outline, "bevel": False,
                              "paint": [{"mat": "wool_brown", "min": (-1, -1, -1),
                                         "max": (band_y, 1, 1)}]}))
    # Buff wave scroll along the guard band.
    wave = [(fx0 + 0.02 + (fx1 - fx0 - 0.04) * i / 16,
             -0.603 + 0.022 * math.sin(i * math.pi / 2), 0.0085) for i in range(17)]
    parts.append(_tube(wave, (0.0015, 0.005), "wool_buff", segments=4, up=(0, 0, 1)))
    # Wool fringe: short frayed tufts off the free edge, rooted under the band.
    teeth = 16
    for i in range(teeth + 1):
        cx = fx0 + 0.015 + (fx1 - fx0 - 0.03) * i / teeth
        tip = -0.700 + 0.010 * math.sin(i * 1.7)
        parts.append(Part("prism", (0, 0, 0.004), (1, 1, 0.003), mat="wool_buff",
                          extras={"outline": [(cx - 0.008, -0.640), (cx + 0.008, -0.640),
                                              (cx + 0.002 * math.sin(i), tip)],
                                  "bevel": False}))
    # Sprigs on the flap's flat part.
    for i in range(7):
        s = rnd.uniform(0.02, 0.03)
        spin = rnd.uniform(0, 2 * math.pi)
        tri = [(math.cos(spin + k * 2.0944) * s, math.sin(spin + k * 2.0944) * s) for k in range(3)]
        px = fx0 + 0.06 + (fx1 - fx0 - 0.12) * i / 6
        py = -0.40 - (i % 2) * 0.09
        parts.append(Part("prism", (px, py, 0.0085), (1, 1, 0.003), mat=colours[i % 5],
                          extras={"outline": tri, "bevel": False}))

    flap_reach = 0.70 + R
    return blueprint(
        entry, parts,
        bevel=0.0,
        family_overrides={
            "field_green": {"grain": 0.30},
            "linen_wrap": {"wear_to": "#8F8A70", "wear_amount": 0.18, "grain": 0.25},
        },
        bbox_overrides={
            "Y": (flap_reach,
                  "build bullet: 0.6 m of the outer turn unrolled and hanging to the floor; "
                  "lying on the floor it reaches ~0.48 m in front of the 0.45 m roll "
                  "(the JSON dimension line gives the roll alone)"),
        },
        notes=["Roll kept at the drawn 3.40 m (the Great Hall variant is scaled in-engine).",
               "The 3-bone sagging spine is left to the rig step: ArtForge's rigged path is "
               "untested, so this is built static and straight.",
               "Millefleur is modelled as ~200 raised sprigs (no albedo tile in the bake); "
               "the spiral is 5 modelled turns of the ~14 over two insets, no normal map."],
    )


BLUEPRINTS = {
    "parade-armour": parade_armour,
    "rolled-tapestry": rolled_tapestry,
}
