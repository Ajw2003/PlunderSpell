"""Bronze Age plunder (docs/art/data/bronze.json, items)."""

from __future__ import annotations

import math

from ..kit import Part, arc_path
from ..spec import Entry
from . import blueprint


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
    profile = [
        (0.000, 0.000),              # toe tip
        (0.015, 0.006),
        (0.025, 0.022),              # knob toe, 0.05 dia
        (0.025, 0.048),
        (0.021, 0.064),              # pinch above the knob
        (0.046, 0.110),
        (0.090, 0.200),              # the taper bows out slightly
        (0.126, 0.290),
        (0.152, 0.350),
        (0.160, band_lo),            # band bottom
        (body_r, shoulder_z),        # carination: max diameter
        (0.166, band_hi),            # band top
        (0.130, 0.466),              # angular shoulder slopes in
        (0.074, 0.497),
        (neck_r, 0.514),             # neck, 0.08 m tall
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

    # Loop handles: oval-section straps (0.035 wide × 0.02 thick) on ±X, arching
    # 0.08 m out. The arc's ends are buried in the body, so each handle is its own
    # closed island that visibly grows out of the jar.
    arc_centre_x, arc_centre_z, arc_r = body_r + 0.005, shoulder_z - 0.03, 0.068
    for side in (1, -1):
        path = arc_path((side * arc_centre_x, 0.0, arc_centre_z), arc_r,
                        128.0, -128.0, 10,
                        axis_u=(side, 0.0, 0.0), axis_v=(0.0, 0.0, 1.0))
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
                                           (start[0] + tip[0] * 0.5, -end_r - 0.002, 0.530 + tip[1] * 0.5),
                                           (start[0] + tip[0], -end_r, end_z)],
                                  "section": (0.0035, 0.0035), "up": (0, 1, 0),
                                  "smooth": True}))

    # Wine stain: a dried run from the seal down one side (front-right), over the
    # band, following the surface. Sits a hair proud of the facets.
    run = []
    for i, z in enumerate((0.575, 0.556, 0.520, 0.500, 0.480, 0.455, 0.430, 0.400,
                           0.370, 0.335, 0.300, 0.270)):
        r = _radius_at(profile, z) + 0.0022
        angle = math.radians(-62.0 + 9.0 * math.sin(i * 0.9))
        run.append((math.cos(angle) * r, math.sin(angle) * r, z))
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="wine_stain", segments=4,
                      extras={"path": run, "section": (0.0030, 0.0085), "smooth": True}))

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


BLUEPRINTS = {
    "sealed-amphora": sealed_amphora,
}
