"""Bronze Age plunder (docs/art/data/bronze.json, items)."""

from __future__ import annotations

import math
import random

from .. import kit
from ..kit import Part, spline
from ..spec import Entry
from . import blueprint


# --------------------------------------------------------------------------------
# Loft: a closed solid skinned through a stack of rings (a lathe whose rings need
# not be circles). kit.py has no such kind and it is a shared framework file, so
# this module registers one into kit's builder table on import, under a
# bronze-prefixed name so it cannot collide with anything another module adds.
#   extras["rings"] = [ring, ...], each ring a list of (x, y, z) in metres with the
#   same point count, or a single point (a pole; only first/last). An end ring with
#   more than one point is capped flat with an n-gon. Keep the point order the same
#   rotational sense on every ring.
# --------------------------------------------------------------------------------

LOFT = "bronze_loft"


def _loft(bm, part: Part):
    rings = [[tuple(map(float, p)) for p in ring] for ring in part.extras["rings"]]
    width = max(len(r) for r in rings)
    rows, verts = [], []
    for i, ring in enumerate(rings):
        if len(ring) == 1:
            if 0 < i < len(rings) - 1:
                raise ValueError(f"loft ring {i} is a pole mid-stack")
        elif len(ring) != width:
            raise ValueError(f"loft ring {i} has {len(ring)} points, expected {width}")
        row = [bm.verts.new(p) for p in ring]
        rows.append(row)
        verts.extend(row)
    faces = []
    for a, b in zip(rows, rows[1:]):
        for j in range(width):
            k = (j + 1) % width
            if len(a) == 1 and len(b) == 1:
                raise ValueError("loft has two consecutive poles")
            if len(a) == 1:
                quad = (a[0], b[k], b[j])
            elif len(b) == 1:
                quad = (a[j], a[k], b[0])
            else:
                quad = (a[j], a[k], b[k], b[j])
            faces.append(bm.faces.new(quad))
    if len(rows[0]) > 1:
        faces.append(bm.faces.new(list(reversed(rows[0]))))
    if len(rows[-1]) > 1:
        faces.append(bm.faces.new(rows[-1]))
    kit._orient_outward(bm, faces)
    return verts, faces


kit._NEW_BUILDERS.setdefault(LOFT, _loft)


def _ray_radius(outline, angle: float) -> float:
    """Distance from the origin to a star-shaped closed outline along `angle`."""
    dx, dy = math.cos(angle), math.sin(angle)
    best = None
    for (x0, y0), (x1, y1) in zip(outline, outline[1:] + outline[:1]):
        ex, ey = x1 - x0, y1 - y0
        det = dx * (-ey) - dy * (-ex)
        if abs(det) < 1e-12:
            continue
        t = (x0 * (-ey) - y0 * (-ex)) / det          # along the ray
        u = (dx * y0 - dy * x0) / det                  # along the edge
        if t > 0 and -1e-9 <= u <= 1 + 1e-9:
            best = t if best is None else min(best, t)
    return best or 0.0


def _interp(table, s: float) -> float:
    """Piecewise-linear lookup in [(key, value), ...] sorted by key."""
    table = sorted(table)
    if s <= table[0][0]:
        return table[0][1]
    for (k0, v0), (k1, v1) in zip(table, table[1:]):
        if k0 <= s <= k1:
            return v0 + (v1 - v0) * (s - k0) / (k1 - k0)
    return table[-1][1]


def _radius_at(profile, z: float) -> float:
    """Linear interpolation of a monotonic-in-z lathe profile."""
    for (r0, z0), (r1, z1) in zip(profile, profile[1:]):
        if z0 <= z <= z1 and z1 > z0:
            return r0 + (r1 - r0) * (z - z0) / (z1 - z0)
    raise ValueError(f"z={z} is outside the profile")


def sealed_amphora(entry: Entry):
    """Canaanite jar: carinated shoulder, pointed knob toe, two loop handles,
    clay-sealed mouth with a cord tied twice round the neck."""
    W, D, H = entry.dims                      # 0.34 × 0.34 × 0.62
    body_r = W / 2.0                          # max diameter at the shoulder
    shoulder_z = 0.42 / 0.62 * H              # "at the shoulder (0.42 m up)"
    neck_r = 0.05                             # neck 0.10 dia
    rim_r = 0.06                              # everted rolled rim 0.12 dia
    seal_top = H

    # One profile for jar, rim and seal lump: a single closed shell, with the
    # sealing clay and the painted band stamped on by z-range (kit paint regions).
    # Profile points sit on each band edge so the colour changes on an edge loop.
    band_lo, band_hi = shoulder_z - 0.034, shoulder_z + 0.006
    rim_top = 0.586
    # The taper from toe to shoulder bows outward (a full belly narrowing to a
    # stem), not a straight cone: r = pinch + (body_r - pinch) * (1 - (1 - t)^1.6).
    pinch_r, pinch_z = 0.021, 0.064

    def belly(t: float) -> tuple[float, float]:
        z = pinch_z + (band_lo - pinch_z) * t
        return pinch_r + (0.166 - pinch_r) * (1.0 - (1.0 - t) ** 1.6), z

    profile = [
        (0.000, 0.000),              # toe tip
        (0.015, 0.006),
        (0.025, 0.022),              # knob toe, 0.05 dia
        (0.025, 0.048),
        (pinch_r, pinch_z),          # pinch above the knob
        *[belly(t) for t in (0.14, 0.34, 0.56, 0.78)],
        belly(1.0),                  # band bottom
        (body_r, shoulder_z),        # carination: max diameter
        (0.166, band_hi),            # band top
        (0.150, 0.455),              # rounded shoulder dome up to the neck
        (0.118, 0.482),
        (0.076, 0.503),
        (neck_r, 0.516),             # neck, 0.08 m tall
        (neck_r - 0.002, 0.560),
        (rim_r, 0.568),              # rolled rim
        (rim_r + 0.002, 0.577),
        (rim_r - 0.004, rim_top),
        (0.047, rim_top + 0.001),    # sealing clay lump starts
        (0.043, 0.604),
        (0.026, 0.616),
        (0.000, seal_top),
    ]
    SEG = 16
    parts = [Part(
        "lathe", (0, 0, 0), (1, 1, 1), mat="buff_terracotta", segments=SEG,
        extras={"profile": profile, "paint": [
            {"mat": "haematite_red", "min": (-1, -1, band_lo - 1e-4), "max": (1, 1, band_hi + 1e-4)},
            {"mat": "sealing_clay", "min": (-1, -1, rim_top + 8e-4), "max": (1, 1, 1)},
        ]},
    )]

    # Signet impression: a small raised ring pressed into the top of the lump.
    parts.append(Part("torus", (0.0, -0.006, seal_top - 0.006), (0.032, 0.032, 0.012),
                      mat="sealing_clay", segments=8, rings=4, minor=0.22,
                      rot=(-12, 0, 0)))

    # Loop handles: oval-section straps (0.035 wide × 0.02 thick) on ±X, springing
    # from the shoulder just above the band, arching 0.08 m out and down into the
    # belly — ear-shaped, as the concept's front view draws them. Both ends are
    # buried in the body, so each handle is its own closed island.
    top_z, low_z = band_hi + 0.010, band_lo - 0.064
    reach = body_r + 0.08
    mid_z = (top_z + low_z) / 2.0
    half = (top_z - low_z) / 2.0
    ear = [(body_r - 0.012 + (reach - body_r + 0.012) * math.cos(math.radians(a)) ** 0.8
            * (1 if math.cos(math.radians(a)) >= 0 else -1),
            mid_z + half * math.sin(math.radians(a)))
           for a in (88, 62, 34, 6, -24, -52, -80)]
    loop = ([(_radius_at(profile, top_z) - 0.022, top_z + 0.004)]     # buried end
            + [(x, z) for x, z in spline(ear, 2)]
            + [(_radius_at(profile, low_z) - 0.020, low_z - 0.004)])  # buried end
    for side in (1, -1):
        path = [(side * x, 0.0, z) for x, z in loop]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="buff_terracotta", segments=6,
                          extras={"path": path, "section": (0.0175, 0.010),
                                  "up": (0.0, 1.0, 0.0), "smooth": True}))

    # Flax cord: two turns round the neck under the seal, a knot on the front
    # (-Y) and two short tails.
    for z in (0.528, 0.540):
        r = _radius_at(profile, z) + 0.002
        ring = [(math.cos(a) * r, math.sin(a) * r, z)
                for a in (2 * math.pi * i / 10 for i in range(10))]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="flax_cord", segments=4,
                          extras={"path": ring, "closed": True, "section": (0.0045, 0.0045),
                                  "smooth": True}))
    knot_y = -(_radius_at(profile, 0.534) + 0.006)
    parts.append(Part("sphere", (0.004, knot_y, 0.534), (0.016, 0.013, 0.014),
                      mat="flax_cord", segments=6, rings=4))
    for dx, tip in ((-0.006, (-0.014, -0.035)), (0.010, (0.018, -0.045))):
        start = (0.004 + dx, knot_y - 0.001, 0.530)
        end_z = 0.530 + tip[1]
        end_r = _radius_at(profile, end_z) + 0.004
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="flax_cord", segments=4,
                          extras={"path": [start,
                                           (start[0] + tip[0] * 0.5,
                                            -(_radius_at(profile, 0.530 + tip[1] * 0.5) + 0.004),
                                            0.530 + tip[1] * 0.5),
                                           (start[0] + tip[0], -end_r, end_z)],
                                  "section": (0.0035, 0.0035), "up": (0, 1, 0),
                                  "smooth": True}))

    # Wine stain: a dried run from the seal down one side (front-right), over the
    # band, following the surface. Sits a hair proud of the facets.
    run = []
    for i, z in enumerate((0.575, 0.552, 0.515, 0.492, 0.470, 0.445, 0.418, 0.385,
                           0.350, 0.312, 0.275)):
        r = _radius_at(profile, z) + 0.0022
        angle = math.radians(-60.0 + 11.0 * math.sin(i * 1.1))
        run.append((math.cos(angle) * r, math.sin(angle) * r, z))
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="wine_stain", segments=4,
                      extras={"path": run, "section": (0.0028, 0.0065), "smooth": True}))

    return blueprint(
        entry, parts,
        # No bevel: a thrown pot has no machined edges, and the bevel's angle limit
        # would catch every edge of the 4- and 6-sided cords and straps.
        bevel=0.0,
        family_overrides={
            # Wheel ridges every 0.01 m (the brief asks for them in a normal map;
            # EnemyForge bakes no normals, so they are painted as faint albedo bands),
            # and fire-clouding rubbed in towards a darker clay.
            "buff_terracotta": {"ridges": (0.01, 0.10), "wear_to": "#7A5438",
                                "wear_amount": 0.22, "grain": 0.20},
            "haematite_red": {"rough": 0.8, "grain": 0.18},
            "wine_stain": {"rough": 0.55},
        },
        bbox_overrides={
            "X": (W + 2 * 0.08 + 0.02,
                  "handles arch 0.08 m out from the 0.34 m body on ±X (build bullet); "
                  "the JSON dimension line gives the body only"),
        },
        notes=["Ring stand omitted: the JSON marks it optional and not part of the pick-up.",
               "Signet figure and potter's mark are below mesh resolution; left to a decal."],
    )


def oxhide_ingot(entry: Entry):
    """Cypriot copper oxhide: a slab with four concave sides flaring into horned
    corner lugs, domed and blistered on the open-mould top, flat below."""
    W, D, H = entry.dims                       # 0.60 × 0.40 × 0.05, lug tip to lug tip
    A, B = W / 2.0, D / 2.0
    bow, p = 0.06, 3.0                         # 0.06 m inward curve on every side
    tip_r = 0.016

    # Outline, counter-clockwise from +Z: east side, NE lug, north side, NW lug ...
    def long_side(x):                          # |y| of the north/south edges
        return B - bow * (1.0 - abs(x / A) ** p)

    def short_side(y):                         # |x| of the east/west edges
        return A - bow * (1.0 - abs(y / B) ** p)

    ts_short = [-0.86, -0.6, -0.3, 0.0, 0.3, 0.6, 0.86]
    ts_long = [0.88, 0.66, 0.4, 0.14, -0.14, -0.4, -0.66, -0.88]
    tip = [(A - tip_r + tip_r * math.cos(math.radians(a)),
            B - tip_r + tip_r * math.sin(math.radians(a))) for a in (8, 45, 82)]
    east = [(short_side(B * t), B * t) for t in ts_short]
    north = [(A * t, long_side(A * t)) for t in ts_long]
    west = [(-short_side(B * t), -B * t) for t in ts_short]
    south = [(-A * t, -long_side(A * t)) for t in ts_long]
    ne = tip
    nw = [(-x, y) for x, y in reversed(tip)]
    sw = [(-x, -y) for x, y in tip]
    se = [(x, -y) for x, y in reversed(tip)]
    outline = east + ne + north + nw + west + sw + south + se

    droop_max = 0.0045                          # lugs bend ~5° down over their last 0.05 m

    def droop(x, y):
        c = (abs(x) / A) * (abs(y) / B)
        return droop_max * max(0.0, min(1.0, (c - 0.45) / 0.5)) ** 1.5

    # (scale about the centre, z) from the flat mould-side bottom up the soft
    # 0.01 m edge round, to the 0.035 m edge and the domed 0.05 m centre.
    bottom = [(0.90, 0.0), (0.978, 0.0025), (1.0, 0.011), (1.0, 0.023)]
    top = [(0.982, 0.0315), (0.935, 0.0355), (0.72, 0.0435), (0.42, 0.0485)]

    # Inner rings relax from the horned outline towards a plain oval, so the dome
    # rises as one smooth swell instead of ridging along the diagonals to the lugs.
    oval = (0.25, 0.155)

    def round_to(s):
        return max(0.0, min(1.0, (0.985 - s) / 0.5))

    def ring_point(s, x, y):
        a = math.atan2(y, x)
        r_oval = 1.0 / math.hypot(math.cos(a) / oval[0], math.sin(a) / oval[1])
        k = round_to(s)
        r = math.hypot(x, y)
        f = s * ((1 - k) + k * r_oval / r)
        return f * x, f * y

    def stack(lift):
        out = [[(0.0, 0.0, lift)]]
        for s, z in bottom + top:
            ring = []
            for x, y in outline:
                px, py = ring_point(s, x, y) if (s, z) in top else (s * x, s * y)
                ring.append((px, py, z + lift - droop(px, py)))
            out.append(ring)
        return out + [[(0.0, 0.0, 0.05 + lift)]]

    # Rest it on its drooped lug tips: lift the slab so the lowest point is z = 0.
    lift = -min(pt[2] for ring in stack(0.0) for pt in ring)
    rings = stack(lift)

    top_table = [(1.0, 0.023)] + top + [(0.0, 0.05)]

    def surface(x, y):
        """Top-face height at (x, y): bisect for the ring scale passing through it."""
        a = math.atan2(y, x)
        ox, oy = math.cos(a) * _ray_radius(outline, a), math.sin(a) * _ray_radius(outline, a)
        r, lo, hi = math.hypot(x, y), 0.0, 1.0
        for _ in range(40):
            mid = (lo + hi) / 2.0
            if math.hypot(*ring_point(mid, ox, oy)) < r:
                lo = mid
            else:
                hi = mid
        return _interp(top_table, lo) + lift - droop(x, y)

    grit_z = lift + 0.0012
    paint = [
        {"mat": "hearth_soot", "min": (-0.22, 0.12, -1), "max": (0.22, 1, lift + 0.013)},
        {"mat": "casting_grit", "min": (-1, -1, -1), "max": (1, 1, grit_z)},
    ]
    for sx in (1, -1):
        for sy in (1, -1):
            lo = (0.255 if sx > 0 else -1, 0.158 if sy > 0 else -1, -1)
            hi = (1 if sx > 0 else -0.255, 1 if sy > 0 else -0.158, 1)
            paint.append({"mat": "rubbed_copper", "min": lo, "max": hi})
    parts = [Part(LOFT, (0, 0, 0), (1, 1, 1), mat="raw_copper",
                  extras={"rings": rings, "paint": paint, "smooth": True})]

    # Blisters and pocks on the open-mould face: dark oxide pits, and a few raised
    # bubbles rubbed bright. Seeded so every build is identical.
    rng = random.Random(1187)
    placed = []
    stamp = (0.19, 0.0)
    while len(placed) < 24:
        x, y = rng.uniform(-0.22, 0.22), rng.uniform(-0.13, 0.13)
        if math.hypot(x - stamp[0], y - stamp[1]) < 0.05:
            continue
        d = rng.uniform(0.016, 0.034)
        if any(math.hypot(x - px, y - py) < (d + pd) * 0.6 for px, py, pd in placed):
            continue
        placed.append((x, y, d))
    for i, (x, y, d) in enumerate(placed):
        z = surface(x, y)
        # Raised bubbles only off the crown, so they never lift the 0.05 m height.
        raised = i % 3 == 0 and z < lift + 0.046
        h = 0.009 if raised else 0.006
        parts.append(Part("sphere", (x, y, z - h * (0.1 if raised else 0.2)),
                          (d, d * rng.uniform(0.7, 0.95), h),
                          mat="rubbed_copper" if raised else "copper_oxide",
                          rot=(0, 0, rng.uniform(0, 180)), segments=6, rings=3,
                          extras={"bevel": False, "smooth": True}))

    # Stamped Cypro-Minoan sign near the east short edge: a trident, 0.06 m.
    def on_top(pts):
        return [(x, y, surface(x, y) - 0.0004) for x, y in pts]

    sign = [
        [(0.160, 0.0), (0.222, 0.0)],
        [(0.222, -0.021), (0.200, -0.021), (0.190, -0.012), (0.188, 0.0),
         (0.190, 0.012), (0.200, 0.021), (0.222, 0.021)],
    ]
    for path in sign:
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="copper_oxide", segments=4,
                          extras={"path": on_top(path), "section": (0.0012, 0.0026),
                                  "up": (0, 0, 1), "bevel": False, "smooth": True}))

    # Casting flash: a thin fin along the front (-Y) long side at mid-edge height.
    flash = [(x * 1.004, y * 1.004 - 0.001, 0.017 + lift - droop(x, y))
             for x, y in south[1:-1]]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="raw_copper", segments=4,
                      extras={"path": flash, "section": (0.0022, 0.0016),
                              "up": (0, 0, 1), "bevel": False, "smooth": True}))

    return blueprint(
        entry, parts,
        # The soft 0.01 m edge round is modelled in the loft rings; the asset bevel
        # would only multiply the 44-point outline.
        bevel=0.0,
        extra_families={
            # The wear bullet asks for brighter rubbed copper on the lug tips and
            # raised blisters; the JSON gives that colour only inside Raw copper's
            # notes ("brighter #C07A4E on high points"), not as its own material.
            "rubbed_copper": {"name": "Rubbed copper (high points)", "base": "#C07A4E",
                              "rough": 0.4, "metal": 1.0, "grain": 0.12},
        },
        family_overrides={
            "raw_copper": {"wear_to": "#5A3524", "wear_amount": 0.25, "grain": 0.28},
            "copper_oxide": {"rough": 0.8},
            # "multiply 30 %" over raw copper: the smudge is baked as its result.
            "hearth_soot": {"base": "#6E4028", "rough": 0.7},
        },
        notes=["Shrinkage wrinkles and sand-cast grain are left to the normal map, "
               "which EnemyForge's bake does not make; blisters and pocks are modelled."],
    )


def _superellipse_ring(x, zc, hw, hh, e, n):
    """A ring in the YZ plane at `x`: half-width hw (Y), half-height hh (Z)."""
    ring = []
    for j in range(n):
        t = 2.0 * math.pi * j / n
        c, s = math.cos(t), math.sin(t)
        ring.append((x, math.copysign(abs(c) ** (2.0 / e), c) * hw,
                     zc + math.copysign(abs(s) ** (2.0 / e), s) * hh))
    return ring


def _station_at(stations, x):
    """Linear blend of (x, zc, hw, hh, e) stations at `x`."""
    ordered = sorted(stations)
    for a, b in zip(ordered, ordered[1:]):
        if a[0] <= x <= b[0]:
            f = (x - a[0]) / (b[0] - a[0])
            return tuple(a[i] + (b[i] - a[i]) * f for i in range(5))
    raise ValueError(f"x={x} is outside the stations")


def faience_hippo(entry: Entry):
    """Egyptian blue-glazed hippopotamus: barrel body on four stumpy legs, blocky
    muzzle, knob eyes and ears, black-line lotus, reeds and a butterfly."""
    W, D, H = entry.dims                      # 0.20 long × 0.08 wide × 0.11 tall
    N = 16
    # (x, z centre, half-width, half-height, squareness); head at -X, tail at +X.
    stations = [
        (0.089, 0.061, 0.020, 0.022, 2.2),
        (0.080, 0.062, 0.032, 0.032, 2.3),
        (0.062, 0.063, 0.039, 0.036, 2.4),
        (0.030, 0.062, 0.040, 0.036, 2.4),    # widest: 0.08 m
        (0.000, 0.061, 0.039, 0.035, 2.4),
        (-0.030, 0.059, 0.036, 0.032, 2.4),
        (-0.048, 0.060, 0.030, 0.027, 2.4),   # neck
        (-0.062, 0.062, 0.029, 0.026, 2.6),   # back of the head
        (-0.080, 0.058, 0.031, 0.027, 2.9),   # cheeks: muzzle 0.06 wide
        (-0.093, 0.056, 0.030, 0.025, 3.2),
        (-0.100, 0.056, 0.024, 0.019, 3.2),
    ]
    rings = [[(0.094, 0.0, 0.061)]]
    rings += [_superellipse_ring(x, zc, hw, hh, e, N) for x, zc, hw, hh, e in stations]
    rings.append([(-0.1025, 0.0, 0.056)])

    def flank_y(x, z, side=1):
        _, zc, hw, hh, e = _station_at(stations, x)
        t = min(1.0, abs(z - zc) / hh)
        return side * hw * (1.0 - t ** e) ** (1.0 / e)

    body = Part(LOFT, (0, 0, 0), (1, 1, 1), mat="faience_glaze", extras={
        "rings": rings, "smooth": True, "paint": [
            # Hand-painted glaze highlight: a streak along the back and on the crown.
            {"mat": "glaze_highlight", "min": (-0.005, -0.012, 0.093), "max": (0.055, 0.012, 1)},
            {"mat": "glaze_highlight", "min": (-0.090, -0.012, 0.079), "max": (-0.066, 0.012, 1)},
        ]})
    parts = [body]

    # Legs: stumpy lathed posts, a slightly spread flat foot, top buried in the belly.
    leg = [(0.0112, 0.0), (0.0114, 0.004), (0.0104, 0.012), (0.0100, 0.044)]
    for i, (lx, ly) in enumerate(((-0.047, -0.023), (-0.047, 0.023),
                                  (0.056, -0.024), (0.056, 0.024))):
        paint = []
        if i in (0, 3):   # chipped feet: the white quartz core shows through
            paint = [{"mat": "quartz_frit_core", "min": (-1, -1 if i == 0 else 0.004, -1),
                      "max": (0.004 if i == 0 else 1, 1, 0.0041)}]
        parts.append(Part("lathe", (lx, ly, 0.0), (1, 1, 1), mat="faience_glaze", segments=8,
                          extras={"profile": leg, "smooth": True, "bevel": False,
                                  "paint": paint}))
        # Reed stem painted up the outside of each leg.
        side = 1 if ly > 0 else -1
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="manganese_black_paint",
                          segments=3, extras={
                              "path": [(lx - 0.002, ly + side * 0.0106, 0.006),
                                       (lx + 0.001, ly + side * 0.0102, 0.022),
                                       (lx + 0.003, ly + side * 0.0098, 0.036)],
                              "section": (0.0008, 0.0012), "up": (0, side, 0),
                              "smooth": True, "bevel": False}))

    # Tail stub, 0.01 m, drooping.
    parts.append(Part("cone", (0.097, 0.0, 0.058), (0.008, 0.008, 0.014), mat="faience_glaze",
                      rot=(0, 115, 0), segments=6, extras={"bevel": False, "smooth": True}))

    # Head details: knob eyes with black pupils, small ears, nostril bumps.
    for side in (1, -1):
        ex, ez = -0.074, 0.081
        ey = side * (flank_y(ex, ez) - 0.002)
        parts.append(Part("sphere", (ex, ey, ez), (0.013, 0.012, 0.012), mat="faience_glaze",
                          segments=8, rings=5, extras={"bevel": False, "smooth": True}))
        parts.append(Part("sphere", (ex - 0.004, ey + side * 0.002, ez + 0.003),
                          (0.006, 0.006, 0.006), mat="manganese_black_paint",
                          segments=6, rings=4, extras={"bevel": False, "smooth": True}))
        parts.append(Part("sphere", (-0.058, side * 0.017, 0.087), (0.007, 0.010, 0.016),
                          mat="faience_glaze", rot=(side * 12, -10, 0), segments=6, rings=4,
                          extras={"bevel": False, "smooth": True}))
        parts.append(Part("sphere", (-0.096, side * 0.010, 0.074), (0.010, 0.009, 0.008),
                          mat="faience_glaze", segments=6, rings=4,
                          extras={"bevel": False, "smooth": True}))
        # Black outline at the mouth: a long curve down the side of the muzzle.
        mouth = [(-0.0985, 0.046), (-0.093, 0.0425), (-0.082, 0.0425), (-0.071, 0.047)]
        parts.append(_painted_line(mouth, flank_y, side))

    # Lotus blooms (3 each flank) and reeds; a butterfly on the near (-Y) hip.
    for side in (1, -1):
        for bx in (-0.024, 0.014, 0.052):
            bloom = [(bx - 0.002, 0.036), (bx, 0.052), (bx + 0.001, 0.066),
                     (bx - 0.010, 0.080), (bx - 0.003, 0.070),
                     (bx + 0.001, 0.086), (bx + 0.004, 0.070),
                     (bx + 0.012, 0.079), (bx + 0.003, 0.066)]
            parts.append(_painted_line(bloom, flank_y, side))
        for rx, lean in ((-0.004, 0.006), (0.034, -0.005), (0.074, 0.004)):
            reed = [(rx, 0.036), (rx + lean * 0.5, 0.056), (rx + lean, 0.076)]
            parts.append(_painted_line(reed, flank_y, side))
    fly_x, fly_z = 0.070, 0.086
    fly_y = -(flank_y(fly_x, fly_z) + 0.0006)
    parts.append(Part("prism", (fly_x, fly_y, fly_z), (1, 1, 0.0016), mat="manganese_black_paint",
                      rot=(90 - 32, 0, 0), extras={"bevel": False, "outline": [
                          (-0.008, 0.005), (0.0, 0.0008), (0.008, 0.005), (0.007, -0.004),
                          (0.0, -0.0008), (-0.007, -0.004)]}))

    return blueprint(
        entry, parts,
        bevel=0.0,        # a moulded, glazed figure: no hard edges anywhere
        family_overrides={
            # Glossy vitreous glaze (JSON note: roughness 0.15), pooled darker in
            # the crevices and under the belly.
            "faience_glaze": {"rough": 0.15, "wear_to": "#2E6680", "wear_amount": 0.3,
                              "grain": 0.16},
            "glaze_highlight": {"rough": 0.15, "grain": 0.1},
            "manganese_black_paint": {"rough": 0.2},   # painted under the glaze
        },
        notes=["Line-painting is modelled as hair-thin raised strokes on the glaze "
               "(the bake has no decal pass); a texture artist can move it to albedo."],
    )


def _painted_line(points_xz, flank_y, side, lift=0.0004):
    """A black manganese stroke lying on the hippo's flank, from (x, z) points."""
    path = [(x, flank_y(x, z, side) + side * lift, z) for x, z in points_xz]
    return Part("tube", (0, 0, 0), (1, 1, 1), mat="manganese_black_paint", segments=3,
                extras={"path": path, "section": (0.0009, 0.0013), "up": (0, side, 0),
                        "smooth": True, "bevel": False})


BLUEPRINTS = {
    "sealed-amphora": sealed_amphora,
    "oxhide-ingot": oxhide_ingot,
    "faience-hippo": faience_hippo,
}
