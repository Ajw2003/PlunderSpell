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


BLUEPRINTS = {
    "parade-armour": parade_armour,
}
