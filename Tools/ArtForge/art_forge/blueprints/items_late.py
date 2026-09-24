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


# --------------------------------------------------------------------------------
# Banker's ledger
# --------------------------------------------------------------------------------

def _resample(points, step: float) -> list[tuple]:
    """Points every `step` metres along a 3D polyline (for chain links)."""
    out = [tuple(points[0])]
    carry = 0.0
    for a, b in zip(points, points[1:]):
        seg = math.dist(a, b)
        t = step - carry
        while t <= seg:
            f = t / seg
            out.append(tuple(a[i] + (b[i] - a[i]) * f for i in range(3)))
            t += step
        carry = seg - (t - step)
    return out


def bankers_ledger(entry: Entry):
    """The Livre des changes lying front board up: calf over oak boards, spine along
    the back (+Y), fore-edge facing the viewer with two iron strap clasps and two
    letters of credit on wax-sealed tags; five iron bosses and a paper label on the
    front board; a 22-link library chain from the head of the back board trailing
    on the floor to a wrenched staple with a splinter of desk on it."""
    W, D, _H = entry.dims                      # 0.30 × 0.22 × 0.09 (+ 0.60 m chain)
    hw, hd = W / 2.0, D / 2.0
    board = 0.008
    block_h = 0.070
    top = board * 2 + block_h                   # 0.086: top of the front board
    parts: list[Part] = []
    calf, iron, paper = "calf_cover", "blackened_iron", "rag_paper"

    # --- Boards and text block. The block sits 5 mm in from the head, tail and
    # fore-edge (the boards' squares); the calf spine rounds over the back.
    parts.append(Part("box", (0, 0, board / 2), (W, D, board), mat=calf))
    parts.append(Part("box", (0, 0, top - board / 2), (W, D, board), mat=calf))
    parts.append(Part("box", (0, 0.004, board + block_h / 2), (W - 0.010, D - 0.012, block_h),
                      mat=paper, extras={"bevel": False}))
    parts.append(Part("cyl", (0, hd - 0.006, top / 2), (top, 0.026, W), mat=calf,
                      rot=(0, 90, 0), segments=12, extras={"bevel": False}))
    for x in (-0.085, 0.0, 0.085):                      # three raised bands
        parts.append(Part("cyl", (x, hd - 0.004, top / 2), (top + 0.004, 0.034, 0.012), mat=calf,
                          rot=(0, 90, 0), segments=8, extras={"bevel": False}))

    # --- Blind tooling on the front board: double fillet frame and diagonals.
    z_tool = top + 0.0003
    for inset in (0.018, 0.026):
        frame = [(-hw + inset, -hd + inset, z_tool), (hw - inset, -hd + inset, z_tool),
                 (hw - inset, hd - inset, z_tool), (-hw + inset, hd - inset, z_tool)]
        parts.append(_tube(frame, (0.0012, 0.0012), calf, segments=3, closed=True,
                           up=(0, 0, 1)))
    i0 = 0.026
    for sx in (1, -1):
        parts.append(_tube([(sx * (-hw + i0), -hd + i0, z_tool), (sx * (hw - i0), hd - i0, z_tool)],
                           (0.0012, 0.0012), calf, segments=3, up=(0, 0, 1)))

    # --- Five blackened domed bosses, 3 cm × 1 cm: corners and centre (the grab).
    dome = [(0.015, 0.0), (0.0135, 0.004), (0.008, 0.0085), (0.0, 0.010)]
    for x, y in ((-hw + 0.035, -hd + 0.035), (hw - 0.035, -hd + 0.035),
                 (-hw + 0.035, hd - 0.035), (hw - 0.035, hd - 0.035), (0.0, 0.0)):
        parts.append(_lathe(dome, iron, loc=(x, y, top - 0.0005), segments=8))

    # --- Paper label with a clerk's inked title (two ink strokes).
    lx, ly = 0.0, hd - 0.055
    parts.append(Part("box", (lx, ly, top + 0.0007), (0.080, 0.030, 0.0015), mat=paper,
                      extras={"bevel": False}))
    for dy, w in ((0.005, 0.060), (-0.006, 0.042)):
        parts.append(Part("box", (lx - (0.060 - w) / 2, ly + dy, top + 0.0016), (w, 0.0025, 0.0006),
                          mat=iron, extras={"bevel": False}))

    # --- Two strap-and-pin clasps over the fore-edge (front, -Y).
    for x in (-0.075, 0.075):
        strap = [(x, -hd + 0.030, top + 0.0012), (x, -hd - 0.002, top + 0.0012),
                 (x, -hd - 0.0035, top - 0.012), (x, -hd - 0.0035, board + 0.010)]
        parts.append(_tube(strap, (0.0015, 0.0125), iron, segments=4, up=(0, 0, 1), smooth=False))
        parts.append(Part("cyl", (x, -hd - 0.004, board / 2 + 0.002), (0.008, 0.008, 0.010),
                          mat=iron, rot=(90, 0, 0), segments=6, extras={"bevel": False}))

    # --- Letters of credit tucked in the fore-edge, each with a parchment tag and
    # a red wax seal hanging over the edge.
    for x, z in ((-0.030, 0.052), (0.035, 0.040)):
        parts.append(Part("box", (x, -hd + 0.004 - 0.018, z), (0.070, 0.040, 0.002), mat=paper,
                          rot=(0, 0, 4 if x < 0 else -6), extras={"bevel": False}))
        tag_x, tag_y = x - 0.012, -hd - 0.030
        parts.append(_tube([(tag_x, tag_y + 0.004, z - 0.001), (tag_x, tag_y - 0.001, z - 0.012),
                            (tag_x + 0.002, tag_y - 0.002, 0.022)],
                           (0.0008, 0.006), paper, segments=4, up=(0, -1, 0), smooth=False))
        parts.append(Part("cyl", (tag_x + 0.002, tag_y - 0.006, 0.0115), (0.022, 0.022, 0.006),
                          mat="sealing_wax", rot=(90, 0, 0), segments=10,
                          extras={"bevel": False}))

    # --- Library chain: riveted hasp at the head of the back board, 22 links
    # (0.60 m) trailing on the floor in an arc to the wrenched staple.
    parts.append(Part("box", (-hw - 0.004, hd - 0.050, board / 2), (0.014, 0.024, 0.010), mat=iron))
    route = spline([(-hw - 0.010, hd - 0.050, 0.0), (-hw - 0.090, hd - 0.060, 0.0),
                    (-hw - 0.175, hd - 0.115, 0.0), (-hw - 0.205, -0.030, 0.0),
                    (-hw - 0.170, -hd - 0.060, 0.0), (-hw - 0.080, -hd - 0.095, 0.0),
                    (-hw + 0.065, -hd - 0.085, 0.0)], 6)
    pitch = 0.60 / 22
    centres = _resample(route, pitch)[:23]
    for k in range(22):
        a, b = centres[k], centres[k + 1]
        cxk, cyk = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        dx, dy = b[0] - a[0], b[1] - a[1]
        n = math.hypot(dx, dy)
        dx, dy = dx / n, dy / n
        half_l, half_w, wire = 0.0195, 0.0062, 0.0022
        if k % 2 == 0:                          # lying flat
            side, cz, normal = (-dy, dx, 0.0), wire, (0.0, 0.0, 1.0)
        else:                                   # standing on edge (clear of the mitred corners)
            side, cz, normal = (0.0, 0.0, 1.0), half_w + wire * 1.45, (dy, -dx, 0.0)
        link = [(cxk + dx * sl * half_l + side[0] * sw * half_w,
                 cyk + dy * sl * half_l + side[1] * sw * half_w,
                 cz + side[2] * sw * half_w)
                for sl, sw in ((1, 1), (-1, 1), (-1, -1), (1, -1))]
        parts.append(_tube(link, (wire, wire), iron, segments=3, closed=True, up=normal))
    # The staple: a 6 cm U wrenched from the desk, bent, with its oak splinter.
    ex, ey, _ = centres[22]
    staple = [(ex - 0.004, ey - 0.020, 0.004), (ex - 0.002, ey - 0.002, 0.014),
              (ex + 0.030, ey + 0.004, 0.012), (ex + 0.052, ey - 0.012, 0.004)]
    parts.append(_tube(staple, (0.0035, 0.0035), iron, segments=4))
    parts.append(Part("box", (ex + 0.040, ey - 0.020, 0.008), (0.040, 0.015, 0.012),
                      mat="oak_boards", rot=(0, 0, -20)))

    return blueprint(
        entry, parts,
        bevel=0.0015,
        family_overrides={
            # Calf rubbed through to the oak at the corners and clasp edges.
            calf: {"wear_to": "#6B4F33", "wear_amount": 0.30, "grain": 0.25},
            # Leaf lines on the fore-edge (albedo bands: no normal map is baked).
            paper: {"ridges": (0.0025, 0.22), "grain": 0.18},
        },
        bbox_overrides={
            "X": (0.52, "the dimension line adds 'plus a 0.60 m chain': 22 links "
                             "trailing from the head of the back board reach 0.21 m past the "
                             "book's head on the floor"),
            "Y": (0.35, "the trailing chain and staple curl 0.12 m in front of the "
                        "fore-edge (dimension line: 'plus a 0.60 m chain')"),
        },
        notes=["Label text, tooling depth and the seal devices are below mesh resolution; "
               "left to a decal / normal map (EnemyForge bakes no normals).",
               "Chain links are 4-corner closed tubes with a 3-sided wire to fit the 0.5k "
               "chain share of the budget."],
    )


# --------------------------------------------------------------------------------
# Jewelled hat-badge
# --------------------------------------------------------------------------------

def _facing(kind: str, loc, size, mat: str, **kwargs) -> Part:
    """A part whose local +Z points at the viewer (-Y): lathes, discs and prisms
    drawn in world XZ. Rot X 90 maps local (x, y, z) to world (x, -z, y)."""
    extras = kwargs.pop("extras", {})
    extras.setdefault("bevel", False)
    return Part(kind, loc, size, mat=mat, rot=(90.0, 0.0, 0.0), extras=extras, **kwargs)


def _quatrefoil(lobe_offset: float, lobe_r: float, per_lobe: int) -> list[tuple]:
    """Outer boundary of four overlapping circles on the X and Z axes (XZ outline)."""
    a, r = lobe_offset, lobe_r
    half_chord = math.sqrt(r * r - (a * math.sqrt(2) / 2) ** 2)
    cusp = a / math.sqrt(2) + half_chord                # distance of an outer cusp
    out = []
    for k, (cx, cz) in enumerate(((a, 0), (0, a), (-a, 0), (0, -a))):
        base = math.radians(90 * k)
        # This lobe's arc runs from the cusp at base-45° to the cusp at base+45°.
        p0 = (cusp * math.cos(base - math.pi / 4), cusp * math.sin(base - math.pi / 4))
        p1 = (cusp * math.cos(base + math.pi / 4), cusp * math.sin(base + math.pi / 4))
        t0 = math.atan2(p0[1] - cz, p0[0] - cx)
        t1 = math.atan2(p1[1] - cz, p1[0] - cx)
        while t1 < t0:
            t1 += 2 * math.pi
        for i in range(per_lobe):
            t = t0 + (t1 - t0) * i / per_lobe
            out.append((cx + math.cos(t) * r, cz + math.sin(t) * r))
    return out, cusp


def jewelled_hat_badge(entry: Entry):
    """A gold quatrefoil standing upright on its pearl drop: four white enamel roses
    with gold bosses, green leaves between them, a table-cut balas ruby set as a
    lozenge in a clawed gold collet, seven seed pearls at the tips and cusps, a pear
    pearl on a loop below, and the hinged pin 6 mm behind the plate."""
    W, D, H = entry.dims                       # 0.065 × 0.014 × 0.080
    gold, pearl = "gold", "pearl"
    parts: list[Part] = []
    z0 = 0.0505                                # plate centre height
    lobe_r = 0.014                             # 4 lobes, 0.028 dia
    a = W / 2.0 - lobe_r                       # lobe centre offset: 0.0185
    plate_t = 0.002

    # --- Quatrefoil plate (front face at y = 0).
    outline, cusp = _quatrefoil(a, lobe_r, 8)
    parts.append(_facing("prism", (0, plate_t / 2, z0), (1, 1, plate_t), gold,
                         extras={"outline": outline}))

    # --- Four white enamel roses in the round: five-petalled flower, domed heart,
    # gold boss 4 mm.
    petals = [(math.cos(t) * 0.0110 * (0.80 + 0.20 * abs(math.cos(2.5 * t))),
               math.sin(t) * 0.0110 * (0.80 + 0.20 * abs(math.cos(2.5 * t))))
              for t in (2 * math.pi * i / 15 + math.pi / 2 for i in range(15))]
    heart = [(0.0070, 0.0), (0.0058, 0.0010), (0.0030, 0.0017), (0.0, 0.0019)]
    for lx, lz in ((a, 0), (0, a), (-a, 0), (0, -a)):
        parts.append(_facing("prism", (lx, -0.0009, z0 + lz), (1, 1, 0.0014), "white_enamel",
                             extras={"outline": petals}))
        parts.append(_facing("lathe", (lx, -0.0015, z0 + lz), (1, 1, 1), "white_enamel",
                             segments=8, extras={"profile": heart}))
        parts.append(Part("ico", (lx, -0.0034, z0 + lz), (0.004, 0.003, 0.004), mat=gold,
                          subdivisions=1, extras={"bevel": False, "smooth": True}))

    # --- Green enamel leaves on the diagonals, pointing out from the collet.
    leaf = [(-0.005, 0.0), (-0.002, 0.0022), (0.002, 0.0022), (0.005, 0.0),
            (0.002, -0.0022), (-0.002, -0.0022)]
    for k in range(4):
        t = math.radians(45 + 90 * k)
        c, s = math.cos(t), math.sin(t)
        pts = [(x * c - y * s, x * s + y * c) for x, y in leaf]
        d = 0.0115
        parts.append(_facing("prism", (c * d, -0.0010, z0 + s * d), (1, 1, 0.0012),
                             "green_enamel", extras={"outline": pts}))

    # --- Balas ruby: 12 mm table-cut stone set as a lozenge in a 16 mm raised gold
    # collet, four claws each tipped with a bead; the stone stands 5 mm proud.
    collet = [(0.0080, 0.0), (0.0080, 0.0012), (0.0068, 0.0024)]
    parts.append(_facing("lathe", (0, -0.0002, z0), (1, 1, 1), gold, segments=4,
                         extras={"profile": collet}))
    stone = [(0.0060, 0.0), (0.0060, 0.0012), (0.0036, 0.0030)]
    parts.append(_facing("lathe", (0, -0.0024, z0), (1, 1, 1), "balas_ruby", segments=4,
                         extras={"profile": stone}))
    for k in range(4):
        t = math.radians(90 * k)
        cx, cz = math.cos(t) * 0.0056, math.sin(t) * 0.0056
        parts.append(_tube([(cx * 1.25, -0.0024, z0 + cz * 1.25), (cx, -0.0046, z0 + cz)],
                           (0.0007, 0.0007), gold, segments=3))
        parts.append(Part("sphere", (cx, -0.0048, z0 + cz), (0.0019, 0.0019, 0.0019),
                          mat=gold, segments=6, rings=4, extras={"bevel": False}))

    # --- Seven seed pearls, 5.5 mm: three lobe tips (the bottom tip carries the
    # drop) and the four cusps, each seated on the rim.
    spots = [(0.0, a + lobe_r - 0.0005), (a + lobe_r - 0.0005, 0.0),
             (-(a + lobe_r - 0.0005), 0.0)]
    spots += [(math.cos(t) * (cusp + 0.0008), math.sin(t) * (cusp + 0.0008))
              for t in (math.radians(45 + 90 * k) for k in range(4))]
    for px, pz in spots:
        parts.append(Part("sphere", (px, 0.0008, z0 + pz), (0.0055, 0.0055, 0.0055), mat=pearl,
                          segments=6, rings=4, extras={"bevel": False, "smooth": True}))

    # --- Pearl drop: gold loop 5 mm under the bottom lobe, a small gold cap, and a
    # pear-shaped pearl 9 × 13 mm reaching the ground.
    bottom = z0 - a - lobe_r                   # bottom lobe tip
    loop_z = bottom - 0.0012
    parts.append(Part("torus", (0, 0.0010, loop_z), (0.0052, 0.0052, 0.0052), mat=gold,
                      rot=(90, 0, 0), segments=8, rings=4, minor=0.28,
                      extras={"bevel": False, "smooth": True}))
    cap_top = loop_z - 0.0020
    parts.append(Part("cone", (0, 0.0010, cap_top - 0.0011), (0.0048, 0.0048, 0.0022), mat=gold,
                      rot=(180, 0, 0), segments=8, extras={"bevel": False}))
    pear = [(0.0, 0.0), (0.0026, 0.0006), (0.0040, 0.0024), (0.0045, 0.0048),
            (0.0040, 0.0074), (0.0027, 0.0100), (0.0012, 0.0120), (0.0, cap_top - 0.0015)]
    parts.append(_lathe(pear, pearl, loc=(0, 0.0010, 0.0), segments=10, smooth=True))

    # --- Back: the hinged pin, 0.07 m, 6 mm behind the plate: hinge block at the
    # top, the pin running down to a hooked catch.
    back = plate_t
    pin_y = back + 0.006
    parts.append(Part("box", (0.004, back + 0.0035, z0 + 0.030), (0.006, 0.007, 0.005),
                      mat=gold, extras={"bevel": False}))
    parts.append(_tube([(0.004, pin_y, z0 + 0.030), (0.0035, pin_y, z0 - 0.040 + 0.030)],
                       (0.0008, 0.0008), gold, segments=4))
    parts.append(_tube([(0.0035, back, z0 - 0.034), (0.0035, pin_y, z0 - 0.034),
                        (0.0035, pin_y, z0 - 0.042)],
                       (0.0011, 0.0011), gold, segments=4, up=(1, 0, 0)))

    return blueprint(
        entry, parts,
        bevel=0.0,
        family_overrides={
            # Polished bright on the rim, darker in the recesses behind the roses.
            # Roughness 0.32 rather than the JSON's 0.2: at 0.2 the plate mirrors the
            # black studio and reads brown on the sheet (see README trap on metals).
            gold: {"wear_to": "#8A6A18", "wear_amount": 0.12, "grain": 0.08, "rough": 0.32},
            pearl: {"grain": 0.10},
        },
        notes=["Stood upright on the pearl drop (the orientation the concept draws); the "
               "drop is the ground contact.",
               "The 1 mm beaded rim (~100 beads) would take the whole budget; it is a plain "
               "raised rim step here, the beads left to the normal map / texture.",
               "Petal translucency, the ruby's facet highlight and the pearl iridescence "
               "are shader work; the AURUM VOCO glow mask is the gold family's slot."],
    )


# --------------------------------------------------------------------------------
# Gilded nef
# --------------------------------------------------------------------------------

def gilded_nef(entry: Entry):
    """A silver-gilt carrack salt on a lobed foot and baluster stem: round-bellied
    hull with silver strakes and a niello band under the gunwale, silver deck with
    a gilt salt lid, crenellated sterncastle (stern at -X) and raked forecastle,
    three silver masts with a mid-mast platform, a gilt fighting top with two cast
    sailors and a pennant, a gilt bowsprit (bow at +X) and silver-wire rigging."""
    W, D, H = entry.dims                       # 0.52 × 0.18 × 0.56
    gilt, silver, wire = "silver_gilt", "silver", "silver_wire"
    parts: list[Part] = []

    # --- Lobed foot, baluster stem with knop, niello ring at the stem's base: one
    # lathe from the floor to the hull.
    foot = [(0.100, 0.000), (0.100, 0.006), (0.090, 0.022), (0.070, 0.042),
            (0.045, 0.058), (0.026, 0.068),                        # foot dome, 0.07 tall
            (0.017, 0.070), (0.017, 0.078),                        # niello ring
            (0.012, 0.090), (0.030, 0.103), (0.030, 0.113),        # knop, 0.06 dia
            (0.011, 0.126), (0.014, 0.140), (0.034, 0.150)]        # stem into the hull
    parts.append(_lathe(foot, gilt, segments=16, smooth=False,
                        paint=[_band("niello", 0.070, 0.078)]))
    # Eight chased radial ribs raise the eight lobes.
    for k in range(8):
        t = math.radians(22.5 + 45 * k)
        c, s = math.cos(t), math.sin(t)
        rib = [(c * r, s * r, z + 0.0025) for r, z in ((0.101, 0.004), (0.090, 0.022),
                                                          (0.070, 0.042), (0.040, 0.061))]
        parts.append(_tube(rib, (0.004, 0.006), gilt, segments=3, up=(c, s, 0)))

    # --- Hull: a round-bellied bowl, 0.40 long × 0.18 beam, keel at 0.14 m and the
    # deck at 0.28 m; five silver strakes, the niello band, tarnish underneath.
    xc, sy = -0.01, 0.45
    keel, deck = 0.140, 0.280
    strakes = [0.158, 0.180, 0.201, 0.221, 0.240]
    prof = [(0.0, keel), (0.070, keel + 0.004)]
    body = {0.158: 0.128, 0.180: 0.160, 0.201: 0.180, 0.221: 0.192, 0.240: 0.198}
    for z in strakes:
        r = body[z]
        prof += [(r, z - 0.0025), (r + 0.0015, z), (r, z + 0.0025)]
    prof += [(0.200, 0.252), (0.200, 0.271), (0.203, 0.274), (0.201, deck)]
    paint = [_band("tarnish", keel, keel + 0.004)]
    paint += [_band(silver, z - 0.0025, z + 0.0025) for z in strakes]
    paint += [_band("niello", 0.252, 0.271), _band(silver, deck - 1e-3, deck + 1e-3)]
    parts.append(_lathe(prof, gilt, loc=(xc, 0, 0), size=(1, sy, 1), segments=20, paint=paint))

    def side(x: float, z: float, sign: int, lift: float = 0.0015) -> tuple:
        r = _radius_at(prof[1:], z)
        u = max(-0.999, min(0.999, (x - xc) / r))
        return (x, sign * (sy * r * math.sqrt(1 - u * u) + lift), z)

    # Silver wave scroll along the niello band.
    for sign in (1, -1):
        wave = [side(x, 0.2615 + 0.0045 * math.sin(i * math.pi / 2), sign)
                for i, x in enumerate([xc - 0.15 + 0.3 * j / 14 for j in range(15)])]
        parts.append(_tube(wave, (0.0012, 0.0016), silver, segments=3, up=(0, sign, 0)))

    # --- Deck: the gilt salt lid over the oval well, hinged at the mainmast foot.
    main_x = 0.0
    parts.append(Part("cyl", (main_x - 0.035, 0, deck + 0.002), (0.14, 0.06, 0.004), mat=gilt,
                      segments=14, extras={"bevel": False}))
    parts.append(Part("cyl", (main_x - 0.035, 0, deck + 0.0045), (0.012, 0.012, 0.003), mat=gilt,
                      segments=6, extras={"bevel": False}))                # lid knob

    # --- Sterncastle: crenellated silver box 0.10 × 0.14 × 0.05, four square
    # gunports, a gilt rail with five merlons on each side.
    sx0, sx1 = -0.212, -0.112
    s_top = deck + 0.050
    # The stern rises as a flat gilt wall from the belly to the castle (the
    # concept's side view), rather than the bowl's rounded end.
    parts.append(Part("prism", (0, 0, 0), (1, 1, 0.128), mat=gilt, rot=(90, 0, 0),
                      extras={"outline": [(sx0, 0.215), (sx0 + 0.030, 0.196), (sx1 + 0.020, 0.200),
                                          (sx1 + 0.020, deck), (sx0, deck)],
                              "paint": [{"mat": "niello", "min": (-1, 0.252, -1),
                                         "max": (1, 0.271, 1)}]}))
    parts.append(Part("box", ((sx0 + sx1) / 2, 0, deck + 0.022), (0.100, 0.140, 0.052),
                      mat=silver, extras={"paint": [{"mat": "tarnish", "min": (-1, -1, -1),
                                                     "max": (1, 1, -0.49)}]}))
    parts.append(Part("box", ((sx0 + sx1) / 2, 0, s_top), (0.104, 0.144, 0.008), mat=gilt))
    for sign in (1, -1):
        for x in (sx0 + 0.030, sx0 + 0.068):
            parts.append(Part("box", (x, sign * 0.0705, deck + 0.022), (0.016, 0.003, 0.016),
                              mat="niello", extras={"bevel": False}))
        for i in range(5):
            x = sx0 + 0.008 + i * 0.021
            parts.append(Part("box", (x, sign * 0.066, s_top + 0.009), (0.010, 0.008, 0.012),
                              mat=gilt, extras={"bevel": False}))

    # --- Forecastle: a smaller silver castle raked forward, with its own gilt rail.
    f_out = [(0.105, 0.0), (0.170, 0.0), (0.200, 0.045), (0.095, 0.045)]
    parts.append(Part("prism", (0, 0, deck - 0.004), (1, 1, 0.12), mat=silver, rot=(90, 0, 0),
                      extras={"outline": f_out}))
    f_top = deck - 0.004 + 0.045
    parts.append(Part("prism", (0, 0, f_top + 0.003), (1, 1, 0.124), mat=gilt, rot=(90, 0, 0),
                      extras={"outline": [(0.093, 0.0), (0.202, 0.0), (0.202, 0.006),
                                          (0.093, 0.006)]}))
    for sign in (1, -1):
        for i in range(4):
            x = 0.100 + i * 0.030
            parts.append(Part("box", (x, sign * 0.056, f_top + 0.013), (0.010, 0.008, 0.012),
                              mat=gilt, extras={"bevel": False}))

    # --- Masts. Mainmast to 0.56 m overall with the mid-mast platform (0.20 dia),
    # the gilt fighting top (0.05 cup) with two cast sailors, and a pennant.
    main_top = 0.535
    parts.append(Part("cyl", (main_x, 0, (deck + main_top) / 2), (0.006, 0.006, main_top - deck),
                      mat=silver, segments=6, extras={"bevel": False, "smooth": True}))
    platform_z = 0.440
    parts.append(_lathe([(0.0, 0.0), (0.100, 0.006), (0.097, 0.009), (0.0, 0.011)], silver,
                        loc=(main_x, 0, platform_z), segments=16))
    parts.append(_lathe([(0.012, 0.0), (0.025, 0.014), (0.026, 0.034), (0.022, 0.035)], gilt,
                        loc=(main_x, 0, 0.490), segments=10))
    for dx in (-0.009, 0.009):                                  # the two cast sailors
        parts.append(Part("cone", (main_x + dx, 0.006, 0.490 + 0.030), (0.008, 0.008, 0.011),
                          mat=gilt, segments=5, extras={"bevel": False}))
        parts.append(Part("ico", (main_x + dx, 0.006, 0.490 + 0.039), (0.0065, 0.0065, 0.0065),
                          mat=gilt, subdivisions=1, extras={"bevel": False, "smooth": True}))
    parts.append(Part("cyl", (main_x, 0, (main_top + H) / 2), (0.003, 0.003, H - main_top),
                      mat=silver, segments=5, extras={"bevel": False}))
    parts.append(Part("prism", (0, 0, 0), (1, 1, 0.0015), mat=silver, rot=(90, 0, 0),
                      extras={"outline": [(main_x + 0.001, H - 0.002), (main_x + 0.050, H - 0.009),
                                          (main_x + 0.040, H - 0.012), (main_x + 0.050, H - 0.016),
                                          (main_x + 0.001, H - 0.018)], "bevel": False}))
    # Foremast on the forecastle with a small top; mizzen on the sterncastle with a
    # gilt lateen yard.
    fore_x, fore_top = 0.140, 0.430
    parts.append(Part("cyl", (fore_x, 0, (f_top + fore_top) / 2), (0.005, 0.005, fore_top - f_top),
                      mat=silver, segments=6, extras={"bevel": False, "smooth": True}))
    parts.append(_lathe([(0.0, 0.0), (0.050, 0.004), (0.048, 0.007), (0.0, 0.008)], silver,
                        loc=(fore_x, 0, 0.395), segments=12))
    parts.append(_lathe([(0.008, 0.0), (0.014, 0.012), (0.012, 0.018)], gilt,
                        loc=(fore_x, 0, 0.405), segments=8))
    miz_x, miz_top = -0.160, 0.440
    parts.append(Part("cyl", (miz_x, 0, (s_top + miz_top) / 2), (0.005, 0.005, miz_top - s_top),
                      mat=silver, segments=6, extras={"bevel": False, "smooth": True}))
    parts.append(_tube([(miz_x - 0.052, 0.004, 0.388), (miz_x + 0.080, 0.004, 0.452)],
                       (0.0028, 0.0028), gilt, segments=4))
    # Bowsprit: gilt spar 0.12 m raked 30° up from the forecastle.
    b0 = (0.188, 0.0, f_top - 0.010)
    b1 = (b0[0] + 0.12 * math.cos(math.radians(30)), 0.0, b0[2] + 0.12 * math.sin(math.radians(30)))
    parts.append(_tube([b0, b1], (0.004, 0.004), gilt, segments=5))

    # --- Rigging: silver wire from the tops to the gunwales, soldered each end.
    def gun(x, sign):
        return side(x, deck - 0.004, sign, 0.001)
    stays = []
    for sign in (1, -1):
        for x in (-0.070, -0.035, 0.035, 0.070):                # main shrouds
            stays.append(((main_x, 0, 0.492), gun(x, sign)))
        stays.append(((fore_x, 0, 0.400), gun(fore_x - 0.050, sign)))   # fore shrouds
        stays.append(((miz_x, 0, 0.425), (miz_x + sign * 0.0, sign * 0.068, s_top)))
    stays.append(((main_x, 0, 0.520), (fore_x, 0, 0.425)))      # main stay to the fore top
    stays.append(((fore_x, 0, 0.425), b1))                      # fore stay to the bowsprit
    stays.append(((main_x, 0, 0.520), (miz_x, 0, 0.438)))       # main to the mizzen
    for a, b in stays:
        parts.append(_tube([a, b], (0.0009, 0.0009), wire, segments=3))

    return blueprint(
        entry, parts,
        bevel=0.0015,
        family_overrides={
            # Gilt rubbed through to silver on the gunwale and the foot lobes.
            gilt: {"wear_to": "#B8B6AE", "wear_amount": 0.20, "grain": 0.10, "rough": 0.32},
            silver: {"rough": 0.34, "wear_to": "#5A5A55", "wear_amount": 0.12},
        },
        bbox_overrides={
            "Y": (0.20, "the build bullets give the lobed foot and the mid-mast platform "
                        "0.20 m diameters, wider than the 0.18 m beam on the dimension line"),
        },
        notes=["Rigging wire is 1.8 mm across, not the JSON's 0.8 mm: at 0.8 mm it "
               "vanishes at game distance and on the review sheet (LOD1+ alpha cards "
               "as the JSON says).",
               "Foremast height taken from the concept (top at 0.43 m); the JSON's "
               "'foremast 0.30 m' can't be an overall height above a 0.28 m deck.",
               "Salt crystals round the lid and the strake-groove tarnish are texture "
               "work; tarnish is modelled under the keel and the sterncastle."],
    )


BLUEPRINTS = {
    "parade-armour": parade_armour,
    "rolled-tapestry": rolled_tapestry,
    "bankers-ledger": bankers_ledger,
    "jewelled-hat-badge": jewelled_hat_badge,
    "gilded-nef": gilded_nef,
}
