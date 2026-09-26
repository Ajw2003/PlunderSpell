"""The Plunderspell enemy roster.

Plunderspell is an extraction raid on an arcane vault whose custodian intelligence
has gone rogue. Every enemy is one of its constructs: vault masonry and machined
iron reanimated with cyan arcane current and repaired with looted gold. The shared
read is dark blue-grey mass, warm gold trim, cold cyan light in the gaps.

All measurements are metres in Unity scale. Models stand on z = 0, centred on
x = y = 0, and face -Y (which Blender's FBX exporter maps to Unity's +Z forward).
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field

from .parts import Part


@dataclass
class Archetype:
    name: str
    role: str
    height: float
    parts: list[Part]
    bones: list[dict]
    tri_budget: int
    bevel: float = 0.010
    wear: float = 1.0
    grounded: bool = True  # False for fliers, whose lowest geometry hangs above z = 0


def ring(count: int, radius: float, z: float, start_deg: float = 0.0, **part_kwargs):
    """Repeat a part evenly around the Z axis — used for legs, crowns and orbits.

    The part is yawed to face outward, and `loc`/`rot` in `part_kwargs` are treated
    as offsets applied on top of the ring placement.
    """
    base_loc = part_kwargs.pop("loc", (0.0, 0.0, 0.0))
    base_rot = part_kwargs.pop("rot", (0.0, 0.0, 0.0))
    out = []
    for i in range(count):
        yaw = start_deg + 360.0 * i / count
        theta = math.radians(yaw)
        out.append(Part(
            loc=(base_loc[0] + math.cos(theta) * radius,
                 base_loc[1] + math.sin(theta) * radius,
                 base_loc[2] + z),
            rot=(base_rot[0], base_rot[1], base_rot[2] + yaw),
            **part_kwargs,
        ))
    return out


# --------------------------------------------------------------------------------
# Sigil Wisp — scout swarm flier. Fast, fragile, hunts in threes.
# --------------------------------------------------------------------------------

def _sigil_wisp() -> Archetype:
    parts = [
        # The glowing core is the outer surface: an iron shell around it would seal
        # in the one thing that identifies this enemy at a distance.
        Part("ico", (0, 0, 0.62), (0.34, 0.34, 0.34), "arcane", "Core", subdivisions=3),
        Part("ico", (0, 0, 0.62), (0.21, 0.21, 0.21), "iron", "Core", subdivisions=1),
    ]
    # Six gold shards caged around the core, points outward.
    parts += ring(6, 0.20, 0.62, kind="shard", size=(0.10, 0.10, 0.30),
                  mat="gold", bone="Core", rot=(0, 90, 0), segments=4)
    # Three gyroscope rings on distinct planes, each on its own bone so they can spin.
    parts += [
        Part("torus", (0, 0, 0.62), (0.64, 0.64, 0.64), "iron", "Ring_A",
             segments=22, rings=6, minor=0.075),
        Part("torus", (0, 0, 0.62), (0.58, 0.58, 0.58), "iron", "Ring_B",
             rot=(68, 0, 0), segments=22, rings=6, minor=0.075),
        Part("torus", (0, 0, 0.62), (0.52, 0.52, 0.52), "gold", "Ring_C",
             rot=(68, 0, 95), segments=22, rings=6, minor=0.065),
    ]
    parts += ring(4, 0.32, 0.62, kind="ico", size=(0.10, 0.10, 0.10),
                  mat="arcane", bone="Ring_A", subdivisions=2)
    # A short debris tail reads as motion even on a still model.
    parts += [
        Part("shard", (0, 0.05, 0.34), (0.12, 0.12, 0.20), "gold", "Tail", segments=4),
        Part("shard", (0, 0.09, 0.20), (0.09, 0.09, 0.15), "arcane", "Tail", segments=4),
        Part("shard", (0, 0.12, 0.10), (0.06, 0.06, 0.11), "gold", "Tail", segments=4),
        Part("shard", (0, -0.02, 0.92), (0.09, 0.09, 0.26), "gold", "Core", segments=4),
    ]
    bones = [
        dict(name="Root", head=(0, 0, 0), tail=(0, 0, 0.30)),
        dict(name="Core", head=(0, 0, 0.44), tail=(0, 0, 0.80), parent="Root"),
        dict(name="Ring_A", head=(0, 0, 0.62), tail=(0, 0, 0.94), parent="Core"),
        dict(name="Ring_B", head=(0, 0, 0.62), tail=(0.32, 0, 0.62), parent="Core"),
        dict(name="Ring_C", head=(0, 0, 0.62), tail=(0, 0.32, 0.62), parent="Core"),
        dict(name="Tail", head=(0, 0.05, 0.44), tail=(0, 0.14, 0.06), parent="Core"),
    ]
    return Archetype("SigilWisp", "Scout flier — fast, fragile, swarms",
                     0.98, parts, bones, tri_budget=3400, bevel=0.005,
                     wear=0.4, grounded=False)


# --------------------------------------------------------------------------------
# Vault Warden — the baseline melee construct the player fights most often.
# --------------------------------------------------------------------------------

def _vault_warden() -> Archetype:
    parts = [
        Part("box", (0.17, -0.05, 0.07), (0.21, 0.38, 0.14), "iron", "Foot.L", mirror=True),
        Part("box", (0.17, -0.22, 0.05), (0.17, 0.10, 0.10), "gold", "Foot.L", mirror=True),
        Part("cyl", (0.17, 0, 0.38), (0.20, 0.20, 0.50), "stone", "Shin.L",
             mirror=True, taper=1.15),
        Part("sphere", (0.16, 0, 0.62), (0.24, 0.24, 0.22), "iron", "Shin.L",
             mirror=True, segments=14, rings=9),
        Part("cyl", (0.16, 0, 0.84), (0.26, 0.26, 0.46), "stone", "Thigh.L",
             mirror=True, taper=0.86),

        Part("box", (0, 0, 1.10), (0.48, 0.30, 0.20), "stone", "Hips"),
        Part("box", (0, 0, 1.21), (0.52, 0.34, 0.07), "gold", "Hips"),
        Part("box", (0, -0.16, 1.21), (0.14, 0.05, 0.11), "arcane", "Hips"),
        Part("cyl", (0, 0, 1.33), (0.38, 0.28, 0.24), "iron", "Spine", taper=1.2),

        Part("box", (0, 0, 1.58), (0.62, 0.38, 0.34), "stone", "Chest"),
        Part("box", (0, -0.19, 1.60), (0.36, 0.06, 0.26), "gold", "Chest"),
        Part("ico", (0, -0.22, 1.58), (0.18, 0.14, 0.18), "arcane", "Chest", subdivisions=2),
        Part("box", (0, 0, 1.78), (0.46, 0.32, 0.08), "gold", "Chest"),
        Part("box", (0, 0.23, 1.58), (0.34, 0.16, 0.30), "iron", "Chest"),
        Part("box", (0.11, 0.31, 1.58), (0.07, 0.04, 0.22), "arcane", "Chest", mirror=True),

        Part("cyl", (0, 0, 1.85), (0.15, 0.15, 0.10), "iron", "Head"),
        Part("box", (0, 0, 1.97), (0.29, 0.31, 0.27), "stone", "Head"),
        Part("box", (0, -0.16, 1.96), (0.23, 0.06, 0.21), "gold", "Head"),
        Part("box", (0.06, -0.195, 1.99), (0.06, 0.03, 0.04), "arcane", "Head", mirror=True),
        Part("shard", (0, 0.03, 2.16), (0.06, 0.13, 0.24), "gold", "Head", segments=4),

        Part("sphere", (0.35, 0, 1.72), (0.32, 0.36, 0.28), "stone", "Shoulder.L",
             mirror=True, segments=14, rings=9),
        Part("torus", (0.35, 0, 1.72), (0.32, 0.40, 0.42), "gold", "Shoulder.L",
             mirror=True, rot=(0, 90, 0), segments=20, rings=7, minor=0.11),
        Part("cyl", (0.37, 0, 1.46), (0.18, 0.18, 0.36), "iron", "UpperArm.L", mirror=True),
        Part("sphere", (0.37, 0, 1.28), (0.20, 0.20, 0.19), "iron", "UpperArm.L",
             mirror=True, segments=12, rings=8),
        Part("box", (0.37, 0, 1.12), (0.23, 0.25, 0.34), "stone", "Forearm.L", mirror=True),
        Part("box", (0.37, 0, 1.27), (0.26, 0.28, 0.05), "gold", "Forearm.L", mirror=True),
        Part("box", (0.37, -0.02, 0.91), (0.23, 0.25, 0.21), "iron", "Hand.L", mirror=True),
        Part("box", (0.37, -0.14, 0.91), (0.17, 0.03, 0.13), "arcane", "Hand.L", mirror=True),
    ]
    bones = [
        dict(name="Root", head=(0, 0, 0), tail=(0, 0, 0.22)),
        dict(name="Hips", head=(0, 0, 1.02), tail=(0, 0, 1.22), parent="Root"),
        dict(name="Spine", head=(0, 0, 1.22), tail=(0, 0, 1.42), parent="Hips"),
        dict(name="Chest", head=(0, 0, 1.42), tail=(0, 0, 1.80), parent="Spine"),
        dict(name="Head", head=(0, 0, 1.80), tail=(0, 0, 2.14), parent="Chest"),
        dict(name="Shoulder.L", head=(0.12, 0, 1.74), tail=(0.35, 0, 1.72),
             parent="Chest", mirror=True),
        dict(name="UpperArm.L", head=(0.37, 0, 1.66), tail=(0.37, 0, 1.28),
             parent="Shoulder.L", mirror=True),
        dict(name="Forearm.L", head=(0.37, 0, 1.28), tail=(0.37, 0, 1.02),
             parent="UpperArm.L", mirror=True),
        dict(name="Hand.L", head=(0.37, 0, 1.02), tail=(0.37, -0.10, 0.84),
             parent="Forearm.L", mirror=True),
        dict(name="Thigh.L", head=(0.16, 0, 1.04), tail=(0.16, 0, 0.62),
             parent="Hips", mirror=True),
        dict(name="Shin.L", head=(0.16, 0, 0.62), tail=(0.17, 0, 0.16),
             parent="Thigh.L", mirror=True),
        dict(name="Foot.L", head=(0.17, 0, 0.16), tail=(0.17, -0.24, 0.07),
             parent="Shin.L", mirror=True),
    ]
    return Archetype("VaultWarden", "Melee grunt — slow, armoured, closes distance",
                     2.26, parts, bones, tri_budget=7000, bevel=0.010, wear=1.0)


# --------------------------------------------------------------------------------
# Hex Turret — static area denial. Yaws on its column, pitches at the barrel.
# --------------------------------------------------------------------------------

def _hex_turret() -> Archetype:
    parts = [
        # Stone, not iron: a flat metallic disc reflects only the dark sky and
        # disappears from every raised camera angle.
        Part("cyl", (0, 0, 0.055), (1.02, 1.02, 0.11), "stone", "Base", segments=18),
        Part("torus", (0, 0, 0.11), (1.08, 1.08, 1.08), "gold", "Base",
             segments=30, rings=8, minor=0.055),
    ]
    parts += ring(3, 0.54, 0.066, start_deg=90, kind="box", size=(0.32, 0.22, 0.13),
                  mat="stone", bone="Base")
    parts += ring(3, 0.62, 0.072, start_deg=90, kind="shard", size=(0.12, 0.12, 0.20),
                  mat="gold", bone="Base", rot=(0, 90, 0), segments=4)
    parts += [
        Part("cyl", (0, 0, 0.48), (0.36, 0.36, 0.68), "stone", "Column",
             segments=14, taper=0.78),
        Part("torus", (0, 0, 0.22), (0.44, 0.44, 0.44), "gold", "Column",
             segments=22, rings=7, minor=0.12),
        Part("torus", (0, 0, 0.72), (0.36, 0.36, 0.36), "gold", "Column",
             segments=22, rings=7, minor=0.12),
        Part("cyl", (0, 0.20, 0.62), (0.08, 0.08, 0.52), "iron", "Column", rot=(22, 0, 0)),
        Part("box", (0, 0, 0.86), (0.30, 0.30, 0.12), "iron", "Head"),

        Part("box", (0, 0.02, 1.06), (0.50, 0.56, 0.40), "stone", "Head"),
        Part("box", (0, 0.02, 1.27), (0.54, 0.46, 0.06), "gold", "Head"),
        Part("box", (0, 0.31, 1.06), (0.28, 0.06, 0.24), "arcane", "Head"),
        Part("box", (0.30, 0.02, 1.06), (0.09, 0.36, 0.32), "iron", "Head", mirror=True),

        Part("cyl", (0, -0.30, 1.06), (0.36, 0.36, 0.22), "iron", "Barrel", rot=(90, 0, 0)),
        Part("torus", (0, -0.40, 1.06), (0.40, 0.40, 0.40), "gold", "Barrel",
             rot=(90, 0, 0), segments=22, rings=7, minor=0.14),
        Part("cyl", (0, -0.42, 1.06), (0.27, 0.27, 0.04), "arcane", "Barrel", rot=(90, 0, 0)),
    ]
    # Radius must stay non-zero: at 0 the three shards land on the same point and
    # intersect into one mangled spike.
    parts += ring(3, 0.11, 1.36, start_deg=90, kind="shard", size=(0.09, 0.09, 0.28),
                  mat="gold", bone="Head", loc=(0, 0.02, 0), rot=(0, 16, 0), segments=4)
    parts += ring(4, 0.30, 1.30, kind="shard", size=(0.06, 0.06, 0.18),
                  mat="arcane", bone="Head", loc=(0, 0.02, 0), segments=4)
    bones = [
        dict(name="Root", head=(0, 0, 0), tail=(0, 0, 0.18)),
        dict(name="Base", head=(0, 0, 0), tail=(0, 0, 0.16), parent="Root"),
        dict(name="Column", head=(0, 0, 0.16), tail=(0, 0, 0.84), parent="Base"),
        dict(name="Head", head=(0, 0, 0.84), tail=(0, 0, 1.30), parent="Column"),
        dict(name="Barrel", head=(0, 0, 1.06), tail=(0, -0.50, 1.06), parent="Head"),
    ]
    return Archetype("HexTurret", "Static sentry — yaws to track, pitches to fire",
                     1.52, parts, bones, tri_budget=5500, bevel=0.009, wear=0.8)


# --------------------------------------------------------------------------------
# Arc Revenant — elite caster. Hovers, so it has no feet and no ground contact.
# --------------------------------------------------------------------------------

def _arc_revenant() -> Archetype:
    # Eight-sided cones, not smooth ones: the faceted silhouette is what separates a
    # robed construct from a lampshade at gameplay distance.
    parts = [
        Part("cone", (0, 0, 0.88), (0.94, 0.88, 1.14), "cloth", "Hips",
             segments=8, taper=0.40),
        Part("cone", (0, 0, 1.62), (0.46, 0.40, 0.42), "cloth", "Chest",
             segments=8, taper=1.20),
    ]
    # Ragged hem, leaning outward so the tatters break the cone's outline.
    hem = [(0.00, 0.40, 0.50, 20.0), (0.14, 0.35, 0.42, 16.0),
           (0.28, 0.44, 0.56, 24.0), (0.43, 0.33, 0.38, 14.0),
           (0.57, 0.42, 0.50, 20.0), (0.71, 0.36, 0.44, 17.0),
           (0.86, 0.46, 0.58, 26.0)]
    for frac, centre_z, length, lean in hem:
        yaw = 360.0 * frac
        theta = math.radians(yaw)
        parts.append(Part("shard",
                          (math.cos(theta) * 0.42, math.sin(theta) * 0.40, centre_z),
                          (0.15, 0.15, length), "cloth", "Hips",
                          rot=(0.0, lean, yaw), segments=4))
    parts += [
        Part("torus", (0, 0, 1.44), (0.50, 0.46, 0.50), "gold", "Spine",
             segments=24, rings=8, minor=0.15),
        Part("ico", (0, -0.21, 1.60), (0.18, 0.14, 0.18), "arcane", "Chest",
             subdivisions=2),

        # Shoulders read as angled pauldrons rather than a brim around the neck.
        Part("cone", (0, 0, 1.80), (0.70, 0.60, 0.28), "cloth", "Chest",
             segments=8, taper=0.55),
        Part("sphere", (0.36, 0, 1.79), (0.36, 0.40, 0.32), "cloth", "Chest",
             mirror=True, segments=14, rings=9),
        Part("torus", (0.36, 0, 1.79), (0.36, 0.44, 0.46), "gold", "Chest",
             mirror=True, rot=(0, 90, 0), segments=20, rings=7, minor=0.10),
        # Collar flaring up behind the head, to frame the mask.
        Part("cone", (0, 0.12, 2.14), (0.40, 0.36, 0.30), "cloth", "Head",
             segments=8, taper=1.70),

        Part("cone", (0, 0, 2.14), (0.40, 0.44, 0.36), "cloth", "Head",
             segments=8, taper=0.30),
        Part("shard", (0, 0.07, 2.38), (0.10, 0.15, 0.24), "cloth", "Head", segments=4),
        Part("box", (0, -0.15, 2.06), (0.22, 0.09, 0.24), "gold", "Head"),
        Part("box", (0.055, -0.20, 2.11), (0.05, 0.03, 0.10), "arcane", "Head",
             mirror=True),
        Part("box", (0, -0.195, 1.98), (0.13, 0.02, 0.03), "arcane", "Head"),

        # Arms angle forward into a casting pose and carry gold joints so they read
        # against the robe instead of vanishing into it.
        Part("sphere", (0.34, 0, 1.78), (0.23, 0.25, 0.21), "gold", "UpperArm.L",
             mirror=True, segments=12, rings=8),
        Part("cyl", (0.37, 0.02, 1.62), (0.16, 0.16, 0.34), "iron", "UpperArm.L",
             mirror=True, rot=(0, 16, 0)),
        Part("sphere", (0.40, 0.02, 1.45), (0.18, 0.18, 0.18), "gold", "UpperArm.L",
             mirror=True, segments=12, rings=8),
        Part("cyl", (0.43, -0.09, 1.27), (0.15, 0.15, 0.36), "iron", "Forearm.L",
             mirror=True, rot=(-22, 0, 0)),
        Part("box", (0.43, -0.09, 1.30), (0.19, 0.16, 0.22), "cloth", "Forearm.L",
             mirror=True, rot=(-22, 0, 0)),
        Part("box", (0.44, -0.23, 1.11), (0.14, 0.16, 0.16), "iron", "Hand.L",
             mirror=True),
        Part("box", (0.44, -0.31, 1.11), (0.10, 0.02, 0.10), "arcane", "Hand.L",
             mirror=True),
    ]
    for dx, yaw in ((-0.05, -14.0), (0.0, 0.0), (0.05, 14.0)):
        parts.append(Part("shard", (0.44 + dx, -0.33, 1.02), (0.05, 0.05, 0.20),
                          "iron", "Hand.L", mirror=True, rot=(28.0, 0.0, yaw),
                          segments=4))

    parts += [
        Part("torus", (0, 0, 1.32), (1.26, 1.26, 1.26), "arcane", "Sigil",
             rot=(74, 0, 22), segments=36, rings=6, minor=0.026),
        Part("torus", (0, 0, 1.32), (1.08, 1.08, 1.08), "gold", "Sigil",
             rot=(68, 0, -58), segments=32, rings=6, minor=0.026),
    ]
    parts += ring(4, 0.62, 1.32, kind="shard", size=(0.07, 0.07, 0.20),
                  mat="arcane", bone="Sigil", rot=(0, 90, 0), segments=4)

    bones = [
        dict(name="Root", head=(0, 0, 0), tail=(0, 0, 0.26)),
        dict(name="Hips", head=(0, 0, 0.32), tail=(0, 0, 1.30), parent="Root"),
        dict(name="Spine", head=(0, 0, 1.30), tail=(0, 0, 1.52), parent="Hips"),
        dict(name="Chest", head=(0, 0, 1.52), tail=(0, 0, 1.90), parent="Spine"),
        dict(name="Head", head=(0, 0, 1.94), tail=(0, 0, 2.36), parent="Chest"),
        dict(name="UpperArm.L", head=(0.33, 0, 1.78), tail=(0.40, 0.02, 1.45),
             parent="Chest", mirror=True),
        dict(name="Forearm.L", head=(0.40, 0.02, 1.45), tail=(0.44, -0.20, 1.15),
             parent="UpperArm.L", mirror=True),
        dict(name="Hand.L", head=(0.44, -0.20, 1.15), tail=(0.45, -0.34, 1.00),
             parent="Forearm.L", mirror=True),
        dict(name="Sigil", head=(0, 0, 1.32), tail=(0, 0, 1.68), parent="Root"),
    ]
    return Archetype("ArcRevenant", "Elite caster — hovers, shields allies, casts at range",
                     2.50, parts, bones, tri_budget=8500, bevel=0.007,
                     wear=0.7, grounded=False)


# --------------------------------------------------------------------------------
# Gilded Colossus — vault boss. Deliberately asymmetric: hammer left, claw right.
# --------------------------------------------------------------------------------

def _gilded_colossus() -> Archetype:
    parts = [
        Part("box", (0.38, -0.06, 0.12), (0.44, 0.68, 0.24), "iron", "Foot.L", mirror=True),
    ]
    for dx in (-0.13, 0.0, 0.13):
        parts.append(Part("shard", (0.38 + dx, -0.40, 0.10), (0.11, 0.11, 0.26),
                          "gold", "Foot.L", mirror=True, rot=(105, 0, 0), segments=4))
    parts += [
        Part("cyl", (0.38, 0, 0.64), (0.42, 0.42, 0.80), "stone", "Shin.L",
             mirror=True, segments=14, taper=0.82),
        Part("sphere", (0.38, -0.05, 1.04), (0.46, 0.48, 0.42), "gold", "Shin.L",
             mirror=True, segments=14, rings=9),
        Part("cyl", (0.36, 0, 1.42), (0.52, 0.52, 0.78), "stone", "Thigh.L",
             mirror=True, segments=14, taper=0.88),
        Part("box", (0.42, 0, 1.80), (0.32, 0.46, 0.36), "iron", "Hips", mirror=True),

        Part("box", (0, 0, 1.82), (0.74, 0.48, 0.34), "stone", "Hips"),
        Part("box", (0, 0, 1.99), (0.82, 0.54, 0.10), "gold", "Hips"),
        Part("box", (0, -0.26, 1.99), (0.22, 0.06, 0.14), "arcane", "Hips"),
        Part("cyl", (0, 0, 2.16), (0.54, 0.44, 0.28), "iron", "Spine", segments=14, taper=1.18),

        Part("box", (0, 0, 2.52), (1.12, 0.64, 0.58), "stone", "Chest"),
        Part("box", (0, -0.32, 2.54), (0.68, 0.08, 0.46), "gold", "Chest"),
        Part("torus", (0, -0.35, 2.54), (0.50, 0.50, 0.50), "gold", "Chest",
             rot=(90, 0, 0), segments=26, rings=8, minor=0.19),
        Part("ico", (0, -0.36, 2.54), (0.32, 0.24, 0.32), "arcane", "Chest", subdivisions=3),
        Part("box", (0, 0, 2.84), (0.88, 0.52, 0.12), "gold", "Chest"),
        Part("box", (0, 0.40, 2.52), (0.64, 0.24, 0.50), "iron", "Chest"),
        Part("box", (0.19, 0.53, 2.52), (0.11, 0.05, 0.38), "arcane", "Chest", mirror=True),
        Part("cyl", (0.27, 0.46, 2.96), (0.14, 0.14, 0.46), "iron", "Chest", mirror=True),

        Part("cyl", (0, 0, 2.92), (0.24, 0.24, 0.14), "iron", "Head"),
        Part("box", (0, 0, 3.06), (0.42, 0.46, 0.36), "stone", "Head"),
        Part("box", (0, -0.23, 3.04), (0.34, 0.08, 0.30), "gold", "Head"),
        Part("box", (0, -0.275, 3.08), (0.27, 0.03, 0.08), "arcane", "Head"),
    ]
    parts += ring(5, 0.20, 3.32, start_deg=18, kind="shard", size=(0.09, 0.09, 0.30),
                  mat="gold", bone="Head", rot=(24, 0, 0), segments=4)

    # Left side carries the hammer, so everything on it is scaled up.
    parts += [
        Part("sphere", (0.80, 0, 2.72), (0.76, 0.82, 0.62), "stone", "Shoulder.L",
             segments=16, rings=10),
        Part("torus", (0.80, 0, 2.72), (0.68, 0.88, 0.86), "gold", "Shoulder.L",
             rot=(0, 90, 0), segments=24, rings=7, minor=0.10),
        Part("cyl", (0.84, 0, 2.32), (0.36, 0.36, 0.58), "iron", "UpperArm.L", segments=14),
        Part("box", (0.88, 0, 1.80), (0.48, 0.52, 0.62), "stone", "Forearm.L"),
        Part("box", (0.88, 0, 2.08), (0.52, 0.56, 0.08), "gold", "Forearm.L"),
        Part("box", (0.94, 0, 1.26), (0.64, 0.72, 0.54), "iron", "Hand.L"),
        Part("box", (0.94, 0, 1.26), (0.70, 0.78, 0.15), "gold", "Hand.L"),
        Part("box", (0.94, -0.38, 1.26), (0.32, 0.05, 0.28), "arcane", "Hand.L"),
        Part("box", (0.94, 0.38, 1.26), (0.32, 0.05, 0.28), "arcane", "Hand.L"),
    ]
    parts += ring(4, 0.26, 0.96, start_deg=45, kind="shard", size=(0.12, 0.12, 0.28),
                  mat="gold", bone="Hand.L", loc=(0.94, 0, 0), rot=(180, 0, 0), segments=4)

    # Right side is the lighter claw arm.
    parts += [
        Part("sphere", (-0.66, 0, 2.72), (0.58, 0.64, 0.52), "stone", "Shoulder.R",
             segments=14, rings=9),
        Part("torus", (-0.66, 0, 2.72), (0.56, 0.70, 0.64), "gold", "Shoulder.R",
             rot=(0, 90, 0), segments=22, rings=7, minor=0.10),
        Part("cyl", (-0.68, 0, 2.34), (0.28, 0.28, 0.56), "iron", "UpperArm.R", segments=14),
        Part("sphere", (-0.69, 0, 2.06), (0.30, 0.30, 0.28), "gold", "UpperArm.R",
             segments=12, rings=8),
        Part("cyl", (-0.70, 0, 1.84), (0.32, 0.32, 0.52), "stone", "Forearm.R", segments=14),
        Part("box", (-0.72, -0.04, 1.48), (0.32, 0.36, 0.28), "iron", "Hand.R"),
        Part("box", (-0.72, -0.22, 1.48), (0.22, 0.04, 0.18), "arcane", "Hand.R"),
    ]
    for dx, yaw in ((-0.11, -14), (0.0, 0), (0.11, 14)):
        parts.append(Part("shard", (-0.72 + dx, -0.26, 1.30), (0.09, 0.09, 0.30),
                          "iron", "Hand.R", rot=(22, 0, yaw), segments=4))

    bones = [
        dict(name="Root", head=(0, 0, 0), tail=(0, 0, 0.30)),
        dict(name="Hips", head=(0, 0, 1.80), tail=(0, 0, 2.06), parent="Root"),
        dict(name="Spine", head=(0, 0, 2.06), tail=(0, 0, 2.30), parent="Hips"),
        dict(name="Chest", head=(0, 0, 2.30), tail=(0, 0, 2.88), parent="Spine"),
        dict(name="Head", head=(0, 0, 2.88), tail=(0, 0, 3.36), parent="Chest"),
        dict(name="Shoulder.L", head=(0.26, 0, 2.74), tail=(0.80, 0, 2.72), parent="Chest"),
        dict(name="UpperArm.L", head=(0.84, 0, 2.60), tail=(0.86, 0, 2.06),
             parent="Shoulder.L"),
        dict(name="Forearm.L", head=(0.86, 0, 2.06), tail=(0.92, 0, 1.52), parent="UpperArm.L"),
        dict(name="Hand.L", head=(0.92, 0, 1.52), tail=(0.94, 0, 0.94), parent="Forearm.L"),
        dict(name="Shoulder.R", head=(-0.26, 0, 2.74), tail=(-0.66, 0, 2.72), parent="Chest"),
        dict(name="UpperArm.R", head=(-0.68, 0, 2.60), tail=(-0.69, 0, 2.10),
             parent="Shoulder.R"),
        dict(name="Forearm.R", head=(-0.69, 0, 2.10), tail=(-0.71, 0, 1.60),
             parent="UpperArm.R"),
        dict(name="Hand.R", head=(-0.71, 0, 1.60), tail=(-0.72, -0.14, 1.26),
             parent="Forearm.R"),
        dict(name="Thigh.L", head=(0.36, 0, 1.80), tail=(0.38, 0, 1.04),
             parent="Hips", mirror=True),
        dict(name="Shin.L", head=(0.38, 0, 1.04), tail=(0.38, 0, 0.26),
             parent="Thigh.L", mirror=True),
        dict(name="Foot.L", head=(0.38, 0, 0.26), tail=(0.38, -0.40, 0.12),
             parent="Shin.L", mirror=True),
    ]
    return Archetype("GildedColossus", "Vault boss — asymmetric siege construct",
                     3.47, parts, bones, tri_budget=14000, bevel=0.012, wear=1.2)


ROSTER = [
    _sigil_wisp(),
    _vault_warden(),
    _hex_turret(),
    _arc_revenant(),
    _gilded_colossus(),
]

BY_NAME = {a.name: a for a in ROSTER}
