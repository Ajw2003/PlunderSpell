"""Bronze Age plunder (docs/art/data/bronze.json, items)."""

from __future__ import annotations

import math
import random

from .. import kit
from ..kit import Part, rounded_rect, spline
from ..spec import Entry
from . import blueprint


# --------------------------------------------------------------------------------
# Loft: this module wrote the first loft kind and registered it as "bronze_loft".
# It now lives in kit.py as the shared "loft" kind (same code); the old name is
# kept as an alias so these blueprints build exactly as before.
#   extras["rings"] = [ring, ...], each ring a list of (x, y, z) in metres with the
#   same point count, or a single point (a pole; only first/last).
# --------------------------------------------------------------------------------

LOFT = "bronze_loft"
_loft = kit._loft
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
    N = 14
    # (x, z centre, half-width, half-height, squareness); head at -X, tail at +X.
    stations = [
        (0.092, 0.061, 0.018, 0.019, 2.0),    # rounded rump
        (0.084, 0.063, 0.030, 0.029, 2.1),
        (0.068, 0.0645, 0.038, 0.0375, 2.2),
        (0.040, 0.0665, 0.040, 0.0415, 2.2),  # widest (0.08 m) and highest: the hump
        (0.010, 0.065, 0.040, 0.041, 2.2),    # belly sags lowest mid-body
        (-0.020, 0.062, 0.037, 0.036, 2.2),
        (-0.040, 0.061, 0.031, 0.029, 2.3),   # neck dip
        (-0.055, 0.059, 0.030, 0.027, 2.5),   # back of the head
        (-0.072, 0.0575, 0.031, 0.0255, 2.8),
        (-0.088, 0.0555, 0.031, 0.0245, 3.0), # blocky muzzle, 0.062 wide
        (-0.098, 0.055, 0.027, 0.021, 3.0),
        (-0.101, 0.055, 0.020, 0.015, 3.0),
    ]
    rings = [[(0.097, 0.0, 0.061)]]
    rings += [_superellipse_ring(x, zc, hw, hh, e, N) for x, zc, hw, hh, e in stations]
    rings.append([(-0.1035, 0.0, 0.055)])

    def flank_y(x, z, side=1):
        _, zc, hw, hh, e = _station_at(stations, x)
        t = min(1.0, abs(z - zc) / hh)
        return side * hw * (1.0 - t ** e) ** (1.0 / e)

    body = Part(LOFT, (0, 0, 0), (1, 1, 1), mat="faience_glaze", extras={
        "rings": rings, "smooth": True, "paint": [
            # Hand-painted glaze highlight: a streak along the back and on the crown.
            {"mat": "glaze_highlight", "min": (0.0, -0.012, 0.1), "max": (0.06, 0.012, 1)},
            {"mat": "glaze_highlight", "min": (-0.082, -0.012, 0.083), "max": (-0.058, 0.012, 1)},
        ]})
    parts = [body]

    # Legs: stumpy lathed posts, a slightly spread flat foot, top buried in the belly.
    leg = [(0.0122, 0.0), (0.0124, 0.004), (0.0114, 0.012), (0.0110, 0.050)]
    for i, (lx, ly) in enumerate(((-0.040, -0.025), (-0.040, 0.025),
                                  (0.058, -0.026), (0.058, 0.026))):
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
                              "path": [(lx - 0.002, ly + side * 0.0116, 0.006),
                                       (lx + 0.001, ly + side * 0.0110, 0.022),
                                       (lx + 0.003, ly + side * 0.0106, 0.036)],
                              "section": (0.0008, 0.0012), "up": (0, side, 0),
                              "smooth": True, "bevel": False}))

    # Tail stub, 0.01 m, drooping.
    parts.append(Part("cone", (0.100, 0.0, 0.061), (0.008, 0.008, 0.014), mat="faience_glaze",
                      rot=(0, 115, 0), segments=6, extras={"bevel": False, "smooth": True}))

    # Head details: knob eyes with black pupils, small ears, nostril bumps.
    for side in (1, -1):
        ex, ez = -0.066, 0.080
        ey = side * (flank_y(ex, ez) - 0.002)
        parts.append(Part("sphere", (ex, ey, ez), (0.016, 0.015, 0.015), mat="faience_glaze",
                          segments=7, rings=4, extras={"bevel": False, "smooth": True}))
        parts.append(Part("sphere", (ex - 0.0055, ey + side * 0.002, ez + 0.004),
                          (0.007, 0.007, 0.007), mat="manganese_black_paint",
                          segments=6, rings=4, extras={"bevel": False, "smooth": True}))
        parts.append(Part("sphere", (-0.052, side * 0.019, 0.087), (0.007, 0.010, 0.016),
                          mat="faience_glaze", rot=(side * 12, -10, 0), segments=6, rings=4,
                          extras={"bevel": False, "smooth": True}))
        parts.append(Part("sphere", (-0.097, side * 0.011, 0.072), (0.010, 0.009, 0.008),
                          mat="faience_glaze", segments=6, rings=4,
                          extras={"bevel": False, "smooth": True}))
        # Black outline at the mouth: a long curve down the side of the muzzle.
        mouth = [(-0.0995, 0.045), (-0.093, 0.041), (-0.080, 0.0405), (-0.066, 0.046)]
        parts.append(_painted_line(mouth, flank_y, side))

    # Lotus blooms (3 each flank) and reeds; a butterfly on the near (-Y) hip.
    for side in (1, -1):
        for bx in (-0.024, 0.014, 0.052):
            # stem, then an open fan of three curved petals round the calyx
            bloom = [(bx - 0.002, 0.038), (bx + 0.0005, 0.058),
                     (bx - 0.008, 0.063), (bx - 0.013, 0.073), (bx - 0.004, 0.067),
                     (bx - 0.003, 0.076), (bx + 0.001, 0.082), (bx + 0.004, 0.068),
                     (bx + 0.012, 0.071), (bx + 0.013, 0.062), (bx + 0.002, 0.059)]
            parts.append(_painted_line(bloom, flank_y, side))
        for rx, lean in ((-0.004, 0.006), (0.034, -0.005), (0.074, 0.004)):
            reed = [(rx, 0.036), (rx + lean * 0.5, 0.054), (rx + lean, 0.072)]
            parts.append(_painted_line(reed, flank_y, side))
    fly_x, fly_z = 0.072, 0.086
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


def gold_death_mask(entry: Entry):
    """Repoussé gold face: an oval dished sheet with a rolled rim, hollow behind,
    closed almond eyes, a thin nose ridge, upturned moustache, beard incisions and
    pierced ear tabs. Stands upright facing -Y (as the concept's front view)."""
    W, D, H = entry.dims                      # face 0.26 × dish 0.08 × 0.31
    hw, top_h, bot_h = W / 2.0, 0.148, 0.158
    z0 = bot_h * 1.012                        # rolled rim's lowest point at z = 0
    M = 24

    # Face outline in XZ about (0, z0): broad brow, tapering to the chin.
    outline = []
    for j in range(M):
        t = 2.0 * math.pi * j / M - math.pi / 2.0
        c, sn = math.cos(t), math.sin(t)
        e = 2.5 if sn > 0 else 2.0
        x = math.copysign(abs(c) ** (2.0 / e), c) * hw
        x *= 1.0 - 0.26 * max(0.0, -sn) ** 2          # chin narrower than temples
        z = math.copysign(abs(sn) ** (2.0 / e), sn) * (top_h if sn > 0 else bot_h)
        outline.append((x, z))

    def y_front(u):                            # the dish: rim at +Y, face bulges to -Y
        return 0.030 - 0.052 * (1.0 - min(u, 1.0) ** 1.7)

    def ring(u, y):
        return [(u * x, y, z0 + u * z) for x, z in outline]

    shell = 0.002                              # 0.8 mm sheet, modelled 2 mm
    rings = [[(0.0, y_front(0) + shell, z0)]]
    rings += [ring(u * 0.985, y_front(u) + shell) for u in (0.4, 0.8, 0.975)]
    rings += [ring(0.995, 0.0395), ring(1.012, 0.0360)]          # rolled rim
    rings += [ring(u, y_front(u)) for u in (1.0, 0.92, 0.78, 0.56, 0.30)]
    rings.append([(0.0, y_front(0), z0)])
    parts = [Part(LOFT, (0, 0, 0), (1, 1, 1), mat="orpiment_gold", extras={
        "rings": rings, "smooth": True,
        "paint": [{"mat": "gold_highlight", "min": (-1, 0.0345, -1), "max": (1, 1, 1)}]})]

    def surf(x, z, lift=0.0005):
        """Point on the front face at (x, z), `lift` metres proud of it."""
        dz = z - z0
        r = math.hypot(x, dz)
        u = 0.0 if r < 1e-9 else r / _ray_radius(outline, math.atan2(dz, x))
        return (x, y_front(u) - lift, z)

    def line(points, mat, section, sides=4, lift=0.0005):
        return Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, segments=sides,
                    extras={"path": [surf(x, z, lift) for x, z in points],
                            "section": section, "up": (0, -1, 0),
                            "smooth": True, "bevel": False})

    for side in (1, -1):
        # Brows: raised arcs, rubbed bright.
        brow = [(side * (0.014 + 0.078 * t), 0.212 + 0.016 * math.sin(math.pi * (0.25 + 0.75 * t))
                 - 0.006 * t) for t in (0.0, 0.2, 0.4, 0.6, 0.8, 1.0)]
        parts.append(line(brow, "gold_highlight", (0.0035, 0.0045), lift=0.001))
        # Closed almond eyes: a sunk dark socket, a raised upper lid, lashes below.
        ex, ez = side * 0.050, 0.180
        sx, sy, sz = surf(ex, ez, 0.0)
        parts.append(Part("sphere", (sx, sy + 0.002, sz), (0.060, 0.012, 0.019),
                          mat="deep_gold_shadow", segments=6, rings=4,
                          extras={"bevel": False, "smooth": True}))
        lid = [(ex + side * 0.030 * a, ez + 0.0085 * math.cos(math.pi * a / 2.0) - 0.001)
               for a in (-1.0, -0.6, -0.2, 0.2, 0.6, 1.0)]
        parts.append(line(lid, "gold_highlight", (0.003, 0.0032), lift=0.002))
        for a in (-0.45, 0.0, 0.45):
            lx = ex + side * 0.028 * a
            lz = ez - 0.006 * math.cos(math.pi * a / 2.0) - 0.003
            parts.append(line([(lx, lz), (lx, lz - 0.008)], "tomb_dust", (0.001, 0.0012),
                              sides=3, lift=0.0))
        # Upturned moustache, 0.05 m each side, tips rubbed bright.
        stache = [(side * 0.003, 0.108), (side * 0.014, 0.1025), (side * 0.029, 0.100),
                  (side * 0.043, 0.1035), (side * 0.053, 0.112), (side * 0.057, 0.124),
                  (side * 0.053, 0.132)]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="orpiment_gold", segments=5,
                          extras={"path": [surf(x, z, 0.001) for x, z in stache],
                                  "section": (0.004, 0.0055), "up": (0, -1, 0),
                                  "smooth": True, "bevel": False, "paint": [
                                      {"mat": "gold_highlight",
                                       "min": (0.046 if side > 0 else -1, -1, -1),
                                       "max": (1 if side > 0 else -0.046, 1, 1)}]}))
        # Ear tab: a flat rounded plate on the rim, pierced for the binding cord.
        ear_x = side * (hw + 0.012)
        parts.append(Part("prism", (0.0, 0.027, 0.0), (1, 1, 0.005), mat="orpiment_gold",
                          rot=(90, 0, 0), extras={"bevel": False, "outline":
                              rounded_rect(0.042, 0.062, 0.016, 2, cx=ear_x, cy=0.170)}))
        parts.append(Part("cyl", (ear_x + side * 0.006, 0.0243, 0.170), (0.009, 0.009, 0.002),
                          mat="tomb_dust", rot=(90, 0, 0), segments=6,
                          extras={"bevel": False}))
        # Fixing holes at the temple and beside the chin.
        for fx, fz in ((0.106, 0.250), (0.030, 0.022)):
            px, py, pz = surf(side * fx, fz, 0.0)
            parts.append(Part("sphere", (px, py, pz), (0.007, 0.004, 0.007), mat="tomb_dust",
                              segments=4, rings=3, extras={"bevel": False}))
        # Nostril.
        parts.append(Part("sphere", (side * 0.0075, surf(0, 0.121)[1] - 0.014, 0.1205),
                          (0.007, 0.005, 0.005), mat="deep_gold_shadow", segments=5,
                          rings=3, extras={"bevel": False}))

    # Nose: a thin ridge from between the brows widening to the tip.
    nose = [[surf(0, 0.222, -0.004)]]
    for z, w, p in ((0.212, 0.0055, 0.004), (0.185, 0.0065, 0.010), (0.155, 0.0085, 0.016),
                    (0.133, 0.0125, 0.021), (0.121, 0.0135, 0.018)):
        yc = surf(0, z, 0.0)[1]
        nose.append([(w * math.cos(a), yc - p * max(0.0, -math.sin(a)) + 0.004 * max(0.0, math.sin(a)), z)
                     for a in (2.0 * math.pi * k / 8 for k in range(8))])
    nose.append([(0.0, surf(0, 0.113)[1] - 0.004, 0.112)])
    parts.append(Part(LOFT, (0, 0, 0), (1, 1, 1), mat="orpiment_gold", extras={
        "rings": nose, "smooth": True, "bevel": False,
        "paint": [{"mat": "gold_highlight", "min": (-1, -1, 0.128), "max": (1, 1, 0.2)}]}))

    # Mouth: thin closed lips with a faint smile line.
    mx, my, mz = surf(0, 0.084, 0.0)
    parts.append(Part("sphere", (mx, my + 0.001, mz), (0.042, 0.010, 0.013),
                      mat="orpiment_gold", segments=7, rings=3,
                      extras={"bevel": False, "smooth": True}))
    parts.append(line([(-0.021, 0.0855), (-0.008, 0.0835), (0.008, 0.0835), (0.021, 0.0855)],
                      "deep_gold_shadow", (0.0012, 0.0014), sides=3, lift=0.005))

    # Beard: parallel incisions along the jaw and chin, dusted with tomb earth.
    for i in range(-7, 8):
        bx = i * 0.0135
        edge = 1.0 - 0.26 * 0.9                                # chin taper near the bottom
        z_hi = 0.070 if abs(bx) < 0.034 else (0.098 if abs(bx) < 0.075 else 0.135)
        # lowest z inside the outline at this x, less a margin
        lo_z = z0 - bot_h * (1.0 - (abs(bx) / (hw * edge)) ** 2) ** 0.5 + 0.018 \
            if abs(bx) < hw * edge else z_hi - 0.02
        lo_z = min(lo_z, z_hi - 0.02)
        pts = [(bx, z_hi), (bx * 1.04, lo_z)]
        parts.append(line(pts, "tomb_dust", (0.0012, 0.0013), sides=3, lift=0.0003))

    # The old burial crumple on the lower left cheek: two creased facets.
    cx, cz = -0.066, 0.072
    px, py, pz = surf(cx, cz, 0.0)
    qx, qy, _ = surf(cx - 0.01, cz, 0.0)
    yaw = math.degrees(math.atan2(qy - py, 0.01))
    for mat, pts, dy in (("deep_gold_shadow", [(-0.013, -0.012), (0.009, -0.015), (-0.003, 0.018)], 0.0),
                         ("gold_highlight", [(-0.003, 0.018), (0.009, -0.015), (0.015, 0.009)], 0.0006)):
        parts.append(Part("prism", (px, py - 0.0008 - dy, pz), (1, 1, 0.0016), mat=mat,
                          rot=(90, 0, yaw), extras={"bevel": False, "outline": pts}))

    return blueprint(
        entry, parts,
        bevel=0.0,          # a beaten sheet: the rolled rim is modelled, nothing is machined
        family_overrides={
            "orpiment_gold": {"rough": 0.32, "wear_to": "#E6C75A", "wear_amount": 0.2,
                              "grain": 0.12},
            # Deep gold is still gold (metallic), just darker and duller in the hollows.
            "deep_gold_shadow": {"metal": 1.0, "rough": 0.45},
            "gold_highlight": {"rough": 0.25},
            "tomb_dust": {"rough": 0.95},
        },
        bbox_overrides={
            "X": (0.33, "the 0.26 m width is the face oval (the concept's front view "
                        "dimensions the face only); the 0.04 m ear tabs of the build "
                        "bullets stand out beyond it on both sides"),
        },
        notes=["The linen shroud is an environment prop (JSON), not modelled here.",
               "Crumpled-state swap mesh (≤ 1k) not built yet.",
               "Brow hatching is below mesh resolution; left to the normal map."],
    )


def _lathe_face_boxes(profile, segments, bands, mat, pad=0.0015):
    """Paint regions that each catch exactly one lathe face: a tiny box round the
    centroid of every face in the given profile bands (band i spans profile
    points i and i+1). Lets the inside of a thin-walled vessel take a different
    family from the outside at the same height, which a single box cannot."""
    regions = []
    for i in bands:
        (r0, z0), (r1, z1) = profile[i], profile[i + 1]
        for j in range(segments):
            angles = (2.0 * math.pi * j / segments, 2.0 * math.pi * (j + 1) / segments)
            pts = []
            for r, z in ((r0, z0), (r1, z1)):
                if r <= 1e-6:
                    pts.append((0.0, 0.0, z))
                else:
                    pts += [(math.cos(t) * r, math.sin(t) * r, z) for t in angles]
            c = [sum(p[k] for p in pts) / len(pts) for k in range(3)]
            regions.append({"mat": mat, "min": tuple(v - pad for v in c),
                            "max": tuple(v + pad for v in c)})
    return regions


def bronze_tripod(entry: Entry):
    """Tripod cauldron: deep hammered bowl with a rolled lip, three splayed strip
    legs on riveted plates ending in paw feet, twisted-rope ring handles, bracing
    struts, a chased spiral band and bull protomes at the leg joints."""
    W, D, H = entry.dims                      # 0.80 × 0.80 × 1.05
    rim_z, bowl_r, depth = 0.88, 0.33, 0.36
    wall = 0.006                              # 3 mm wall, modelled 6 mm
    S = 28
    parts: list[Part] = []

    def outer_pt(theta_deg):
        t = math.radians(theta_deg)
        return (bowl_r * math.sin(t), rim_z - depth * math.cos(t))

    def inner_pt(theta_deg):
        t = math.radians(theta_deg)
        return ((bowl_r - wall) * math.sin(t), rim_z - (depth - wall) * math.cos(t))

    def bowl_radius(z):
        return bowl_r * math.sqrt(max(0.0, 1.0 - ((rim_z - z) / depth) ** 2))

    soot_top = 0.61                            # underside and inner lower half
    scorch_top = 0.68                          # fire-scorch fade above the soot
    band_lo, band_hi = 0.815, 0.855           # chased spiral band, 0.04 m
    soot_t = math.degrees(math.acos((rim_z - soot_top) / depth))
    scorch_t = math.degrees(math.acos((rim_z - scorch_top) / depth))
    band_t = [math.degrees(math.acos((rim_z - z) / depth)) for z in (band_lo, band_hi)]
    outer = [(0.0, rim_z - depth)] + [outer_pt(t) for t in
                                      (14, 30, soot_t, scorch_t, 68, band_t[0], band_t[1], 89)]
    lip = [(0.336, 0.873), (0.344, 0.881), (0.342, 0.890), (0.332, 0.893), (0.324, 0.886)]
    inner_t = (84, 70, scorch_t, soot_t, 30, 12)
    inner = [inner_pt(t) for t in inner_t] + [(0.0, rim_z - depth + wall)]
    profile = outer + lip + inner
    first_inner_band = len(outer) + len(lip) - 1          # lip end -> first inner point
    inside = _lathe_face_boxes(profile, S, range(first_inner_band, len(profile) - 1),
                               "bronze_shadow")
    parts.append(Part("lathe", (0, 0, 0), (1, 1, 1), mat="cast_bronze", segments=S, extras={
        "profile": profile, "smooth": True, "bevel": False,
        "paint": inside + [
            {"mat": "bronze_shadow", "min": (-1, -1, -1), "max": (1, 1, scorch_top + 1e-4)},
            {"mat": "hearth_soot", "min": (-1, -1, -1), "max": (1, 1, soot_top + 1e-4)},
            {"mat": "bronze_shadow", "min": (-1, -1, band_lo - 1e-4), "max": (1, 1, band_hi + 1e-4)},
            {"mat": "rubbed_bronze", "min": (-1, -1, 0.870), "max": (1, 1, 1)},
        ]}))

    # Grey ash crust lying in the bottom of the bowl.
    parts.append(Part("lathe", (0, 0, 0), (1, 1, 1), mat="ash_residue", segments=16, extras={
        "profile": [(0.0, 0.535), (0.15, 0.568), (0.158, 0.5755), (0.0, 0.577)],
        "bevel": False}))

    # Legs at the front (-Y) and ±120°.
    leg_angles = (-90.0, 30.0, 150.0)
    top_r, top_z, foot_r, foot_z = 0.334, 0.845, 0.415, 0.035
    alpha = math.atan2(foot_r - top_r, top_z - foot_z)          # outward splay
    length = math.hypot(foot_r - top_r, top_z - foot_z)
    strut_z = 0.30
    strut_pts = []
    for phi_deg in leg_angles:
        phi = math.radians(phi_deg)
        radial = (math.cos(phi), math.sin(phi))
        tangent = (-math.sin(phi), math.cos(phi))

        def at(r, z, t=0.0):
            return (radial[0] * r + tangent[0] * t, radial[1] * r + tangent[1] * t, z)

        mid_r, mid_z = (top_r + foot_r) / 2.0, (top_z + foot_z) / 2.0
        parts.append(Part("box", at(mid_r, mid_z), (0.06, 0.02, length), mat="cast_bronze",
                          rot=(math.degrees(alpha), 0.0, phi_deg - 90.0)))
        # Incised zigzag down the leg's outer face.
        zig = []
        for k in range(11):
            f = 0.12 + 0.76 * k / 10
            r = top_r + (foot_r - top_r) * f + 0.0105 * math.cos(alpha)
            z = top_z - (top_z - foot_z) * f + 0.0105 * math.sin(alpha)
            zig.append(at(r, z, 0.016 if k % 2 else -0.016))
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="bronze_shadow", segments=3,
                          extras={"path": zig, "section": (0.0025, 0.0028),
                                  "up": (radial[0], radial[1], 0.0), "bevel": False}))
        # Riveted attachment plate over the leg top, tilted to lie on the bowl.
        parts.append(Part("box", at(0.340, 0.800), (0.10, 0.034, 0.14), mat="cast_bronze",
                          rot=(-11.0, 0.0, phi_deg - 90.0)))
        for rz in (0.755, 0.800, 0.845):
            plate_face = 0.340 + 0.017 + (rz - 0.800) * math.tan(math.radians(11.0))
            parts.append(Part("sphere", at(plate_face, rz), (0.015, 0.015, 0.015),
                              mat="rubbed_bronze", segments=6, rings=4,
                              extras={"bevel": False, "smooth": True}))
        # Paw foot: a pad and three toes, flat on the floor.
        parts.append(Part("sphere", at(foot_r + 0.008, 0.0225), (0.075, 0.068, 0.045),
                          mat="cast_bronze", rot=(0, 0, phi_deg), segments=8, rings=5,
                          extras={"bevel": False, "smooth": True}))
        for t in (-0.022, 0.0, 0.022):
            parts.append(Part("sphere", at(foot_r + 0.040, 0.013, t), (0.026, 0.024, 0.026),
                              mat="rubbed_bronze", segments=6, rings=4,
                              extras={"bevel": False, "smooth": True}))
        # Bull protome on the rim above the plate: head, muzzle, two horns.
        parts.append(Part("sphere", at(0.352, 0.892), (0.052, 0.042, 0.040), mat="rubbed_bronze",
                          rot=(0, 18, phi_deg), segments=8, rings=5,
                          extras={"bevel": False, "smooth": True}))
        parts.append(Part("sphere", at(0.377, 0.882), (0.022, 0.030, 0.022), mat="rubbed_bronze",
                          rot=(0, 0, phi_deg), segments=6, rings=4,
                          extras={"bevel": False, "smooth": True}))
        for side in (1, -1):
            parts.append(Part("cone", at(0.345, 0.915, side * 0.018), (0.012, 0.012, 0.036),
                              mat="rubbed_bronze", rot=(-side * 40.0, 0.0, phi_deg),
                              segments=5, extras={"bevel": False, "smooth": True}))
        f = (top_z - strut_z) / (top_z - foot_z)
        strut_pts.append(at(top_r + (foot_r - top_r) * f, strut_z))

    # Bracing struts in a triangle 0.30 m up, ends buried in the legs.
    for a, b in zip(strut_pts, strut_pts[1:] + strut_pts[:1]):
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="cast_bronze", segments=6,
                          extras={"path": [a, b], "section": (0.006, 0.006), "up": (0, 0, 1),
                                  "smooth": True, "bevel": False}))

    # Chased running spirals: small raised coils round the band, clear of the
    # handles and leg joints.
    band_z = (band_lo + band_hi) / 2.0
    for k in range(18):
        phi_deg = k * 20.0 + 10.0
        near = [abs((phi_deg - a + 180.0) % 360.0 - 180.0) for a in leg_angles + (0.0, 180.0)]
        if min(near) < 11.0:
            continue
        phi = math.radians(phi_deg)
        r = bowl_radius(band_z) + 0.002
        parts.append(Part("torus", (math.cos(phi) * r, math.sin(phi) * r, band_z),
                          (0.028, 0.028, 0.008), mat="rubbed_bronze",
                          rot=(90.0, 0.0, phi_deg + 90.0), segments=8, rings=4, minor=0.28,
                          extras={"bevel": False, "smooth": True}))

    # Ring handles on ±X, in the YZ plane: 0.24 m outer, 0.025 m twisted-rope section.
    ring_r, sec_r, K, P = 0.12 - 0.0125, 0.0125, 40, 7
    ring_z = rim_z + 0.17 - 0.12 + 0.0            # top of the ring at 1.05 m
    for sx in (1, -1):
        x0 = sx * (bowl_r + 0.010)
        rings = []
        for i in range(K):
            phi = 2.0 * math.pi * i / K
            cy, cz = ring_r * math.cos(phi), ring_z + ring_r * math.sin(phi)
            n1 = (0.0, math.cos(phi), math.sin(phi))
            twist = 4.0 * phi
            ring = []
            for j in range(P):
                psi = 2.0 * math.pi * j / P
                rr = sec_r * (0.86 + 0.14 * math.cos(3.0 * (psi - twist)))
                ring.append((x0 + rr * math.sin(psi),
                             cy + rr * math.cos(psi) * n1[1],
                             cz + rr * math.cos(psi) * n1[2]))
            rings.append(ring)
        parts.append(Part(LOFT, (0, 0, 0), (1, 1, 1), mat="rubbed_bronze",
                          extras={"rings": rings, "closed": True, "smooth": True,
                                  "bevel": False}))

    return blueprint(
        entry, parts,
        bevel=0.004,        # cast legs and plates keep a small edge bevel
        family_overrides={
            "cast_bronze": {"wear_to": "#5E3E22", "wear_amount": 0.2, "grain": 0.22},
            # Chased lines and the interior are still bronze, only darker.
            "bronze_shadow": {"metal": 1.0, "rough": 0.6},
            "hearth_soot": {"rough": 0.85},
        },
        notes=["Dent states (3) and the bent-leg state are separate swaps, not built here.",
               "Legs splay about 5.6° rather than 8°: at 8° from the bowl wall the feet "
               "would stand well outside the 0.80 m footprint the same bullet gives.",
               "Spiral chasing is modelled as raised coils; fine chased lines are left "
               "to the normal map, which EnemyForge's bake does not make."],
    )


def _painted_line(points_xz, flank_y, side, lift=0.0004):
    """A black manganese stroke lying on the hippo's flank, from (x, z) points."""
    path = [(x, flank_y(x, z, side) + side * lift, z) for x, z in points_xz]
    return Part("tube", (0, 0, 0), (1, 1, 1), mat="manganese_black_paint", segments=3,
                extras={"path": path, "section": (0.0008, 0.0009), "up": (0, side, 0),
                        "smooth": True, "bevel": False})


BLUEPRINTS = {
    "sealed-amphora": sealed_amphora,
    "oxhide-ingot": oxhide_ingot,
    "faience-hippo": faience_hippo,
    "gold-death-mask": gold_death_mask,
    "bronze-tripod": bronze_tripod,
}
