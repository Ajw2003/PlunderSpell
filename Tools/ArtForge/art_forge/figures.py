"""Parametric rigged figures for enemy blueprints: a human and a quadruped.

A figure owns two things that must agree: a skeleton (EnemyForge bone dicts) and
body parts already bound to it (each Part carries `bone` plus the list of bones it
may blend with, see rig.py). A blueprint makes a figure, asks it for body parts in
its own materials, adds clothing, armour and props against the figure's landmarks,
and passes `**figure.rig()` to `blueprint(...)`:

    fig = figures.Human(height=1.76, bulk=1.1, shoulders=0.48,
                        arm_r=figures.ArmPose(elbow=95, swing=8))
    parts = fig.body(skin="skin", torso="wool", sleeves="wool", legs="hose",
                     feet="leather")
    parts.append(fig.band(fig.belt_z, "leather", height=0.04))
    grip = fig.grip("R")
    fig.prop_bone("Glaive", "R", head=grip, tail=grip + Vector((0, 0, 0.3)))
    parts.append(Part("cyl", ..., mat="ash_haft", bone="Glaive", extras={"prop": True}))
    return blueprint(entry, parts, **fig.rig())

Every figure stands on z = 0, centred on x = y = 0 and faces -Y. `.L` is the
figure's own left, which is +X (the viewer's right in a front view).

Proportions come from EnemyForge's household (`enemy_forge.archetypes`: ANKLE,
KNEE, HIP, WAIST, CHEST, SHOULDER, NECK, SHOULDER_X, HIP_X, all fractions of the
stature), so an ArtForge guard is the same species as an EnemyForge one.

Human bone names follow Unity Humanoid (Mecanim) with Blender side suffixes:
Root > Hips > Spine > Chest > Neck > Head; Chest > Shoulder.X > UpperArm.X >
LowerArm.X > Hand.X; Hips > UpperLeg.X > LowerLeg.X > Foot.X. `UNITY_HUMANOID`
maps Unity's HumanBodyBones to them for an explicit avatar. Extra bones (props,
hats, lantern chains, cloth springs) hang off these with `add_bone`/`prop_bone`.
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field

from mathutils import Vector

from enemy_forge.archetypes import (ANKLE, CHEST, HIP, HIP_X, KNEE, NECK, SHOULDER,
                                    SHOULDER_X, WAIST)

from .kit import Part, spline

__all__ = ["ArmPose", "Human", "Quadruped", "UNITY_HUMANOID", "HUMAN_TEST_POSE",
           "QUADRUPED_TEST_POSE"]

SIDES = {"L": 1.0, "R": -1.0}

# Unity HumanBodyBones -> bone name, for building an Avatar by hand if the
# importer's auto-mapping misreads the Blender ".L/.R" suffixes.
UNITY_HUMANOID = {
    "Hips": "Hips", "Spine": "Spine", "Chest": "Chest", "Neck": "Neck", "Head": "Head",
    "LeftShoulder": "Shoulder.L", "LeftUpperArm": "UpperArm.L",
    "LeftLowerArm": "LowerArm.L", "LeftHand": "Hand.L",
    "RightShoulder": "Shoulder.R", "RightUpperArm": "UpperArm.R",
    "RightLowerArm": "LowerArm.R", "RightHand": "Hand.R",
    "LeftUpperLeg": "UpperLeg.L", "LeftLowerLeg": "LowerLeg.L", "LeftFoot": "Foot.L",
    "RightUpperLeg": "UpperLeg.R", "RightLowerLeg": "LowerLeg.R", "RightFoot": "Foot.R",
}

# The review sheet's POSED view: left arm raised forward to face height, head
# turned, right knee lifted and bent. World-space degrees, see rig.apply_pose.
HUMAN_TEST_POSE = {
    "UpperArm.L": (-80.0, 0.0, 0.0),
    "LowerArm.L": (-25.0, 0.0, 0.0),
    "Head": (0.0, 0.0, 35.0),
    "Spine": (0.0, 0.0, -8.0),
    "UpperLeg.R": (-35.0, 0.0, 0.0),
    "LowerLeg.R": (55.0, 0.0, 0.0),
}

QUADRUPED_TEST_POSE = {
    "Neck2": (0.0, 0.0, 18.0),
    "Head": (8.0, 0.0, 22.0),
    "Jaw": (22.0, 0.0, 0.0),
    "Humerus.R": (-40.0, 0.0, 0.0),
    "Radius.R": (75.0, 0.0, 0.0),
    "Femur.L": (20.0, 0.0, 0.0),
    "Tail1": (35.0, 0.0, 0.0),
    "Tail3": (20.0, 0.0, 15.0),
}


def _v(p) -> Vector:
    return Vector(p) if not isinstance(p, Vector) else p.copy()


def _t(p: Vector) -> tuple:
    return (round(p.x, 5), round(p.y, 5), round(p.z, 5))


def _resample(points: list[Vector], values: list, count: int) -> tuple[list, list]:
    """Evenly resample a polyline (and a per-point value list) by arc length."""
    lengths = [0.0]
    for a, b in zip(points, points[1:]):
        lengths.append(lengths[-1] + (b - a).length)
    total = lengths[-1]
    out_p, out_v = [], []
    for i in range(count):
        s = total * i / (count - 1)
        k = max(0, min(len(points) - 2, next((j for j in range(len(lengths) - 1)
                                               if lengths[j + 1] >= s), len(points) - 2)))
        span = lengths[k + 1] - lengths[k]
        f = 0.0 if span < 1e-9 else (s - lengths[k]) / span
        out_p.append(points[k].lerp(points[k + 1], f))
        va, vb = values[k], values[k + 1]
        if isinstance(va, tuple):
            out_v.append(tuple(x + (y - x) * f for x, y in zip(va, vb)))
        else:
            out_v.append(va + (vb - va) * f)
    return out_p, out_v


class Figure:
    """Shared bone bookkeeping for Human and Quadruped."""

    forward: list[str] = []
    test_pose: dict = {}

    def __init__(self):
        self.bones: list[dict] = []
        self._names: set[str] = set()

    def add_bone(self, name: str, head, tail, parent: str | None) -> str:
        """Add an extra bone (prop, hat, cloth spring). Head/tail in metres."""
        if name in self._names:
            raise ValueError(f"bone {name!r} already exists")
        if parent is not None and parent not in self._names:
            raise ValueError(f"bone {name!r}: parent {parent!r} does not exist yet")
        if (_v(tail) - _v(head)).length < 1e-3:
            raise ValueError(f"bone {name!r} is shorter than 1 mm")
        spec = dict(name=name, head=_t(_v(head)), tail=_t(_v(tail)))
        if parent:
            spec["parent"] = parent
        self.bones.append(spec)
        self._names.add(name)
        return name

    def bone(self, name: str) -> tuple[Vector, Vector]:
        for spec in self.bones:
            if spec["name"] == name:
                return Vector(spec["head"]), Vector(spec["tail"])
        raise KeyError(f"no bone {name!r}; bones are {sorted(self._names)}")

    def along(self, name: str, t: float) -> Vector:
        """The point a fraction `t` of the way from a bone's head to its tail."""
        head, tail = self.bone(name)
        return head.lerp(tail, t)

    def rig(self, pose: dict | None = None) -> dict:
        """Blueprint keyword arguments: bones, forward_bones and the review pose."""
        return {"bones": [dict(b) for b in self.bones],
                "forward_bones": list(self.forward),
                "pose": dict(self.test_pose if pose is None else pose)}


# --------------------------------------------------------------------------------
# Human
# --------------------------------------------------------------------------------

@dataclass
class ArmPose:
    """How one arm sits in the bind pose.

    spread  degrees out from the body, seen from the front (A-pose). 6-12 keeps the
            sleeve clear of the torso, which is what makes heat weighting clean.
    swing   degrees the upper arm hangs forward (+) or back (-).
    elbow   degrees the forearm bends forward/up from the upper arm's line: 0 is a
            straight arm, 90 a forearm held level, pointing forward.
    """

    spread: float = 9.0
    swing: float = 3.0
    elbow: float = 12.0


# Fractions of stature. Torso rings: (z, half-width, half-depth, y shift); widths
# are multiplied by bulk. The front (-Y) of chest rings is pushed out by `chest`.
_TORSO = [
    (0.470, 0.074, 0.056, 0.000),   # crotch (bottom cap)
    (0.530, 0.092, 0.062, 0.004),   # hips
    (0.585, 0.086, 0.060, 0.002),   # belly
    (0.625, 0.083, 0.057, 0.002),   # waist
    (0.680, 0.091, 0.062, -0.002),  # lower ribs
    (0.730, 0.099, 0.066, -0.004),  # chest
    (0.775, 0.104, 0.063, -0.002),  # upper chest
    (0.806, 0.100, 0.057, 0.004),   # shoulder line
    (0.822, 0.078, 0.048, 0.005),   # shoulder slope
    (0.832, 0.046, 0.038, 0.006),   # neck base
    (0.842, 0.036, 0.032, 0.006),   # collar top (cap; the neck passes through it)
]

_HEAD_BARE = [   # (z, half-width, half-depth, y shift); last is the crown pole
    (0.815, 0.030, 0.032, 0.004),
    (0.850, 0.031, 0.034, 0.004),
    (0.872, 0.036, 0.044, -0.006),
    (0.895, 0.043, 0.054, -0.004),
    (0.925, 0.045, 0.057, -0.002),
    (0.950, 0.046, 0.058, 0.000),
    (0.972, 0.041, 0.052, 0.002),
    (0.990, 0.028, 0.036, 0.004),
    (1.000, 0.0, 0.0, 0.004),
]

_HEAD_HOOD = [   # a padded coif / hood: drapes onto the shoulders, open face
    (0.795, 0.088, 0.064, 0.006),
    (0.822, 0.066, 0.054, 0.006),
    (0.850, 0.052, 0.054, 0.002),
    (0.878, 0.052, 0.060, -0.004),
    (0.910, 0.054, 0.063, -0.004),
    (0.940, 0.054, 0.063, -0.002),
    (0.965, 0.050, 0.058, 0.000),
    (0.986, 0.036, 0.043, 0.003),
    (1.000, 0.0, 0.0, 0.004),
]


class Human(Figure):
    """A standing person, `height` metres to the crown of the skull (not the hat).

    height     stature. Eyes land at 0.936 of it (1.65 m on a 1.76 m man).
    bulk       girth of torso and limbs (1.0 = EnemyForge's watchman).
    shoulders  outer deltoid-to-deltoid width in metres (default 0.25 * height).
    stoop      degrees the upper body leans forward from the waist.
    arm_l/_r   ArmPose for each arm's bind pose.
    stance     degrees each leg splays out from vertical.
    segments   sides of torso/limb sections (torso uses 2x; see quilting below).
    """

    forward = ["Foot.L", "Foot.R"]
    test_pose = HUMAN_TEST_POSE

    def __init__(self, height: float = 1.76, bulk: float = 1.0,
                 shoulders: float | None = None, stoop: float = 0.0,
                 arm_l: ArmPose | None = None, arm_r: ArmPose | None = None,
                 stance: float = 2.5, segments: int = 12):
        super().__init__()
        h = self.h = float(height)
        self.bulk = float(bulk)
        self.stoop = float(stoop)
        self.segments = segments
        self.arms = {"L": arm_l or ArmPose(), "R": arm_r or ArmPose()}
        self.shoulders = shoulders if shoulders is not None else 0.25 * h

        # Landmarks (metres). Everything above the waist is leaned by `stoop`.
        self.crotch_z = 0.470 * h
        self.hip_z, self.knee_z, self.ankle_z = HIP * h, KNEE * h, ANKLE * h
        self.waist_z, self.chest_z = WAIST * h, CHEST * h
        self.shoulder_z, self.neck_z = SHOULDER * h, NECK * h
        self.chin_z, self.eye_z = 0.872 * h, 0.936 * h
        self.belt_z = 0.585 * h
        self.leg_x = HIP_X * h * self.bulk ** 0.35
        self.deltoid = 0.036 * h * self.bulk
        self.shoulder_x = max(SHOULDER_X * h * 0.9, self.shoulders / 2.0 - self.deltoid * 1.25)

        self._make_bones(stance)

    # ---- geometry helpers ------------------------------------------------------

    def lean(self, p) -> Vector:
        """Apply the stoop: points above the waist shift toward -Y with height."""
        p = _v(p)
        if p.z > self.waist_z and self.stoop:
            p.y -= (p.z - self.waist_z) * math.tan(math.radians(self.stoop))
        return p

    def _arm_points(self, side: str) -> tuple[Vector, Vector, Vector, Vector, Vector]:
        s, h, pose = SIDES[side], self.h, self.arms[side]
        shoulder = self.lean((s * self.shoulder_x, 0.0, self.shoulder_z))
        sp, sw = math.radians(pose.spread), math.radians(pose.swing)
        upper = Vector((s * math.sin(sp) * math.cos(sw), -math.sin(sw),
                        -math.cos(sp) * math.cos(sw))).normalized()
        elbow = shoulder + upper * (0.172 * h)
        forward = Vector((0.0, -1.0, 0.0))
        perp = forward - forward.dot(upper) * upper
        perp.normalize()
        e = math.radians(pose.elbow)
        fore = (upper * math.cos(e) + perp * math.sin(e)).normalized()
        wrist = elbow + fore * (0.145 * h)
        hand_tip = wrist + fore * (0.090 * h)
        return shoulder, elbow, wrist, hand_tip, fore

    def _make_bones(self, stance: float) -> None:
        h = self.h
        add = self.add_bone
        add("Root", (0, 0, 0), (0, 0, 0.12 * h), None)
        add("Hips", (0, 0, self.hip_z), (0, 0, self.waist_z), "Root")
        add("Spine", (0, 0, self.waist_z), self.lean((0, 0, self.chest_z)), "Hips")
        add("Chest", self.lean((0, 0, self.chest_z)), self.lean((0, 0, self.neck_z)), "Spine")
        add("Neck", self.lean((0, 0, self.neck_z)), self.lean((0, 0, self.chin_z)), "Chest")
        add("Head", self.lean((0, 0, self.chin_z)), self.lean((0, 0, h)), "Neck")
        self._arm = {}
        self._leg = {}
        for side, s in SIDES.items():
            shoulder, elbow, wrist, tip, fore = self._arm_points(side)
            self._arm[side] = (shoulder, elbow, wrist, tip, fore)
            add(f"Shoulder.{side}", self.lean((s * 0.03 * h, 0, self.shoulder_z + 0.004 * h)),
                shoulder, "Chest")
            add(f"UpperArm.{side}", shoulder, elbow, f"Shoulder.{side}")
            add(f"LowerArm.{side}", elbow, wrist, f"UpperArm.{side}")
            add(f"Hand.{side}", wrist, tip, f"LowerArm.{side}")

            splay = math.tan(math.radians(stance))
            hip = Vector((s * self.leg_x, 0.0, self.hip_z))
            knee = Vector((s * (self.leg_x + splay * (self.hip_z - self.knee_z)),
                           -0.006 * h, self.knee_z))
            ankle = Vector((s * (self.leg_x + splay * (self.hip_z - self.ankle_z)),
                            0.0, self.ankle_z))
            ball = ankle + Vector((s * 0.008 * h, -0.090 * h, -0.036 * h))
            self._leg[side] = (hip, knee, ankle, ball)
            add(f"UpperLeg.{side}", hip, knee, "Hips")
            add(f"LowerLeg.{side}", knee, ankle, f"UpperLeg.{side}")
            add(f"Foot.{side}", ankle, ball, f"LowerLeg.{side}")

    # ---- landmarks for layers and props ------------------------------------------

    def joint(self, name: str) -> Vector:
        """Named landmarks: shoulder/elbow/wrist/hip/knee/ankle + '.L'/'.R',
        and 'crown', 'chin', 'eyes', 'neck', 'belt'."""
        base, _, side = name.partition(".")
        if side:
            arm = dict(zip(("shoulder", "elbow", "wrist", "fingertips"), self._arm[side][:4]))
            leg = dict(zip(("hip", "knee", "ankle", "ball"), self._leg[side]))
            return (arm | leg)[base].copy()
        z = {"crown": self.h, "chin": self.chin_z, "eyes": self.eye_z,
             "neck": self.neck_z, "belt": self.belt_z}[base]
        return self.lean((0.0, 0.0, z))

    def grip(self, side: str) -> Vector:
        """Centre of the closed fist, where a haft or a lantern ring passes."""
        _s, _e, wrist, _tip, fore = self._arm[side]
        return wrist + fore * (0.045 * self.h)

    def prop_bone(self, name: str, side: str, head, tail) -> str:
        """A prop bone parented to Hand.<side>. Bind props to it with
        extras={"prop": True} (rigid, and left out of the height check)."""
        return self.add_bone(name, head, tail, f"Hand.{side}")

    # ---- body parts ------------------------------------------------------------

    def torso_ring(self, z_frac: float, hw: float, hd: float, dy: float, n: int,
                   pad: float = 0.0, chest: float = 0.08, quilt: float = 0.0,
                   z: float | None = None, flare: float = 1.0) -> list[tuple]:
        """One torso loft ring (metres). `quilt` indents every other point by that
        fraction, which reads as vertical quilting channels on a gambeson."""
        h, b = self.h, self.bulk
        zz = z_frac * h if z is None else z
        centre = self.lean((0.0, dy * h, zz))
        out = []
        for j in range(n):
            a = 2.0 * math.pi * j / n
            ca, sa = math.cos(a), math.sin(a)
            k = 1.0 + chest * max(0.0, -sa) ** 2 - 0.04 * max(0.0, sa) ** 2
            if quilt and j % 2:
                k *= 1.0 - quilt
            e = 2.0 / 2.4
            x = math.copysign(abs(ca) ** e, ca) * (hw * h * b * flare + pad) * k
            y = math.copysign(abs(sa) ** e, sa) * (hd * h * b * flare + pad) * k
            out.append((centre.x + x, centre.y + y, zz))
        return out

    def torso_part(self, mat: str, pad: float = 0.0, hem: float | None = None,
                   hem_flare: float = 1.3, collar: float = 0.0, quilt: float = 0.0,
                   paint: list | None = None, segments: int | None = None,
                   chest: float = 0.08) -> Part:
        """The torso as one loft from crotch (or a skirt hem) to the collar.

        pad        metres added all round (padding, a coat over the body).
        hem        z in metres of a skirt/coat hem below the crotch; the torso then
                   flares out to `hem_flare` x the hip width and closes flat there,
                   with the legs passing through (they are separate islands).
        collar     metres a stand collar rises above the neck base.
        quilt      0..0.1: vertical quilting channels (see torso_ring).
        Weighted to Hips/Spine/Chest/Neck/Shoulders, and UpperLeg below the hips so a
        skirt follows a stride.
        """
        n = segments or self.segments * 2
        rows = list(_TORSO)
        rings = []
        if hem is not None:
            if hem >= self.crotch_z:
                raise ValueError(f"hem {hem:.2f} m must be below the crotch "
                                 f"({self.crotch_z:.2f} m)")
            span = self.crotch_z - hem
            for f, flare in ((0.0, hem_flare), (0.5, 1.0 + (hem_flare - 1.0) * 0.55)):
                z = hem + span * f
                rings.append(self.torso_ring(0, 0.092, 0.064, 0.004, n, pad, 0.0, quilt,
                                             z=z, flare=flare))
            rows = rows[1:]
        for zf, hw, hd, dy in rows:
            rings.append(self.torso_ring(zf, hw, hd, dy, n, pad, chest if 0.66 < zf < 0.8
                                         else 0.0, quilt if zf < 0.82 else 0.0))
        if collar > 0.0:
            zf, hw, hd, dy = rows[-2]
            rings[-1] = self.torso_ring(zf, hw * 0.92, hd * 0.95, dy, n, pad * 0.5,
                                        z=zf * self.h + collar)
        skirt = {"top": self.hip_z + 0.02 * self.h,
                 "bottom": hem if hem is not None else self.crotch_z,
                 "strength": 0.8 if hem is not None else 0.5}
        return Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone="Spine", extras={
            "rings": rings, "smooth": True, "paint": paint or [], "bevel": False,
            "skirt": skirt,
            "bones": ["Hips", "Spine", "Chest", "Neck", "Shoulder.L", "Shoulder.R",
                      "UpperLeg.L", "UpperLeg.R"]})

    def torso_dims(self, z: float) -> tuple[float, float, float]:
        """(half-width, half-depth, y shift) of the bare torso at height z, metres,
        bulk applied, before any pad. For fitting layers and trinkets."""
        zf = z / self.h
        rows = _TORSO
        k = max(0, min(len(rows) - 2, next((i for i in range(len(rows) - 1)
                                             if rows[i + 1][0] >= zf), len(rows) - 2)))
        a, b = rows[k], rows[k + 1]
        f = max(0.0, min(1.0, (zf - a[0]) / (b[0] - a[0])))
        hw, hd, dy = (a[i] + (b[i] - a[i]) * f for i in (1, 2, 3))
        return hw * self.h * self.bulk, hd * self.h * self.bulk, dy * self.h

    def surface(self, z: float, angle_deg: float, pad: float = 0.0) -> Vector:
        """A point on the torso surface (plus `pad`) at height z. Angle 0 = the
        figure's left (+X), -90 = front (-Y), 90 = back, 180 = right. Where to hang
        a purse, pin a buckle or start a strap."""
        hw, hd, dy = self.torso_dims(z)
        a = math.radians(angle_deg)
        e = 2.0 / 2.4
        ca, sa = math.cos(a), math.sin(a)
        c = self.lean((0.0, dy, z))
        return Vector((c.x + math.copysign(abs(ca) ** e, ca) * (hw + pad),
                       c.y + math.copysign(abs(sa) ** e, sa) * (hd + pad), z))

    def band(self, z: float, mat: str, height: float = 0.04, pad: float = 0.012,
             torso_pad: float = 0.0, bone: str = "Hips") -> Part:
        """A belt/girdle/garter band hugging the torso at height z (metres)."""
        n = self.segments * 2
        hw, hd, dy = self.torso_dims(z)
        hw, hd, dy = hw / (self.h * self.bulk), hd / (self.h * self.bulk), dy / self.h
        rings = [self.torso_ring(0, hw, hd, dy, n, pad + torso_pad, 0.0, z=zz)
                 for zz in (z - height / 2, z + height / 2)]
        return Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, extras={
            "rings": rings, "smooth": True, "bevel": False,
            "bones": ["Hips", "Spine", "UpperLeg.L", "UpperLeg.R"]})

    def arm_part(self, side: str, mat: str, pad: float = 0.0, quilt_rings: int = 0,
                 quilt: float = 0.06, segments: int | None = None,
                 paint: list | None = None) -> Part:
        """Shoulder cap to wrist as one sweep (no seam at the elbow).
        `quilt_rings` > 0 resamples the sleeve into that many stations and pinches
        every other one by `quilt`: ring quilting on a padded sleeve."""
        h, b = self.h, self.bulk
        s = SIDES[side]
        shoulder, elbow, wrist, _tip, _fore = self._arm[side]
        # Start inside the chest, below the joint: a start ring level with the joint
        # stands up out of the shoulder line as a peak.
        start = shoulder + Vector((-s * 0.036 * h, 0.0, -0.024 * h))
        up_dir = (elbow - shoulder).normalized()
        fore_dir = (wrist - elbow).normalized()
        pts = [start, shoulder,
               shoulder + up_dir * 0.172 * h * 0.35, shoulder + up_dir * 0.172 * h * 0.70,
               elbow,
               elbow + fore_dir * 0.145 * h * 0.35, elbow + fore_dir * 0.145 * h * 0.72, wrist]
        radii = [0.024, 0.030, 0.029, 0.026, 0.0225, 0.024, 0.021, 0.0165]
        if quilt_rings:
            pts, radii = _resample(pts, radii, quilt_rings)
        sections = []
        for i, r in enumerate(radii):
            r = r * h * b + pad
            if quilt_rings and i % 2 and 0 < i < len(radii) - 1:
                r *= 1.0 - quilt
            sections.append((r, r * 0.92))
        return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=f"UpperArm.{side}",
                    segments=segments or self.segments, extras={
                        "path": [tuple(p) for p in pts], "sections": sections,
                        "up": (0.0, 1.0, 0.0), "smooth": True, "paint": paint or [],
                        "bevel": False, "bones": [f"Shoulder.{side}", f"UpperArm.{side}",
                                  f"LowerArm.{side}", "Chest"]})

    def hand_part(self, side: str, mat: str, segments: int = 8) -> list[Part]:
        """A closed fist (mitten) and a thumb, on Hand.<side>."""
        h = self.h
        s = SIDES[side]
        _sh, _el, wrist, _tip, fore = self._arm[side]
        palm_normal = Vector((-s, 0.0, 0.0))
        width_axis = fore.cross(palm_normal)
        if width_axis.length < 1e-3:
            width_axis = Vector((0.0, 1.0, 0.0))
        width_axis.normalize()
        pts = [wrist - fore * 0.006 * h, wrist + fore * 0.020 * h,
               wrist + fore * 0.050 * h, wrist + fore * 0.070 * h, wrist + fore * 0.078 * h]
        secs = [(0.015 * h, 0.011 * h), (0.024 * h, 0.014 * h), (0.026 * h, 0.017 * h),
                (0.022 * h, 0.015 * h), (0.0, 0.0)]
        fist = Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=f"Hand.{side}",
                    segments=segments, extras={
                        "path": [tuple(p) for p in pts], "sections": secs,
                        "up": tuple(width_axis), "power": 2.6, "smooth": True,
                        "bevel": False, "bones": [f"Hand.{side}", f"LowerArm.{side}"]})
        thumb_at = wrist + fore * 0.040 * h - width_axis * 0.020 * h - palm_normal * 0.006 * h
        thumb = Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=f"Hand.{side}",
                     segments=6, extras={
                         "path": [tuple(wrist + fore * 0.012 * h - width_axis * 0.014 * h),
                                  tuple(thumb_at), tuple(thumb_at + fore * 0.02 * h)],
                         "sections": [(0.009 * h, 0.007 * h), (0.008 * h, 0.006 * h), (0, 0)],
                         "smooth": True, "bevel": False, "rigid": True})
        return [fist, thumb]

    def leg_part(self, side: str, mat: str, pad: float = 0.0, segments: int | None = None,
                 paint: list | None = None) -> Part:
        """Hip to ankle as one sweep, calf bulging backward; ends inside the shoe."""
        h, b = self.h, self.bulk
        s = SIDES[side]
        hip, knee, ankle, _ball = self._leg[side]
        start = hip + Vector((-s * 0.030 * h, 0.0, 0.040 * h))
        thigh = (knee - hip)
        shin = (ankle - knee)
        pts = [start, hip, hip + thigh * 0.35, hip + thigh * 0.72, knee,
               knee + shin * 0.28, knee + shin * 0.62, ankle, ankle - Vector((0, 0, 0.018 * h))]
        radii = [0.040, 0.045, 0.041, 0.034, 0.030, 0.032, 0.025, 0.0195, 0.0185]
        back = [0.0, 0.0, 0.0, 0.0, 0.0, 0.007, 0.004, 0.0, 0.0]
        sections = [(r * h * b + pad, r * h * b * 1.02 + pad) for r in radii]
        offsets = [(o * h, 0.0) for o in back]
        return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=f"UpperLeg.{side}",
                    segments=segments or self.segments, extras={
                        "path": [tuple(p) for p in pts], "sections": sections,
                        "offsets": offsets, "up": (0.0, 1.0, 0.0), "smooth": True,
                        "paint": paint or [], "bevel": False,
                        "bones": ["Hips", f"UpperLeg.{side}", f"LowerLeg.{side}",
                                  f"Foot.{side}"]})

    def foot_part(self, side: str, mat: str, length: float | None = None,
                  segments: int = 10, point: float = 0.3) -> Part:
        """A shoe: loft from heel to toe, flat sole on z = 0. `point` 0..1 sharpens
        the toe (0 = round, 1 = a poulaine)."""
        h = self.h
        s = SIDES[side]
        _hip, _knee, ankle, _ball = self._leg[side]
        length = length or 0.153 * h
        heel_y = ankle.y + 0.045 * h
        # (t along the foot, half-width, top z, centre x drift)
        stations = [(0.00, 0.020, 0.040), (0.10, 0.027, 0.062), (0.32, 0.030, 0.062),
                    (0.55, 0.033, 0.042), (0.78, 0.030, 0.030), (0.93, 0.020 - 0.006 * point,
                                                                 0.020)]
        rings = []
        for t, hw, top in stations:
            y = heel_y - t * length
            cx = ankle.x + s * 0.006 * h * t
            half_h = top * h / 2.0
            ring = []
            for j in range(segments):
                a = 2.0 * math.pi * j / segments
                x = cx + math.cos(a) * hw * h
                z = max(0.0, half_h + math.sin(a) * half_h * 1.15)
                ring.append((x, y, z))
            rings.append(ring)
        rings.append([(ankle.x + s * 0.006 * h, heel_y - length, 0.010 * h)])
        return Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone=f"Foot.{side}", extras={
            "rings": rings, "smooth": True, "bevel": False,
            "bones": [f"Foot.{side}", f"LowerLeg.{side}"]})

    def head_part(self, mat: str, face: str | None = None, hood: bool = False,
                  segments: int = 16, features: str | None = None) -> list[Part]:
        """Neck + skull as one loft, with a nose. With `hood=True` the loft is a
        padded coif that drapes onto the shoulders in `mat`, and the open face is
        painted `face`. `features` (a dark family) adds brows and eyes."""
        h = self.h
        rows = _HEAD_HOOD if hood else _HEAD_BARE
        rings = []
        for zf, hw, hd, dy in rows:
            c = self.lean((0.0, dy * h, zf * h))
            if hw == 0.0:
                rings.append([tuple(c)])
                continue
            ring = []
            for j in range(segments):
                a = 2.0 * math.pi * j / segments
                ca, sa = math.cos(a), math.sin(a)
                k = 1.0 - 0.07 * max(0.0, -sa) ** 3   # a flatter face than skull
                ring.append((c.x + ca * hw * h * k, c.y + sa * hd * h * k, c.z))
            rings.append(ring)
        face_y = self.lean((0.0, -0.030 * h, self.eye_z)).y
        paint = []
        if face and face != mat:
            lo = self.lean((0.0, 0.0, 0.862 * h))
            paint.append({"mat": face, "min": (-0.036 * h, -1.0, lo.z),
                          "max": (0.036 * h, face_y, 0.958 * h)})
        head = Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone="Head", extras={
            "rings": rings, "smooth": True, "paint": paint, "bevel": False,
            "bones": ["Head", "Neck"] + (["Chest"] if hood else [])})
        front = self.lean((0.0, -0.058 * h, 0.912 * h))
        if hood:
            front.y -= 0.004 * h
        nose = Part("sphere", tuple(front), (0.020 * h, 0.022 * h, 0.036 * h),
                    mat=face or mat, bone="Head", rot=(-18.0, 0.0, 0.0), segments=8, rings=6,
                    extras={"rigid": True, "bevel": False})
        parts = [head, nose]
        if features:
            for s in (1.0, -1.0):
                eye = self.lean((s * 0.019 * h, -0.054 * h, self.eye_z - 0.004 * h))
                parts.append(Part("sphere", tuple(eye), (0.016 * h, 0.008 * h, 0.006 * h),
                                  mat=features, bone="Head", segments=8, rings=4,
                                  extras={"rigid": True, "bevel": False}))
        return parts

    def body(self, skin: str, torso: str, sleeves: str | None = None,
             legs: str | None = None, feet: str | None = None, head: str | None = None,
             hood: bool = False, **torso_kwargs) -> list[Part]:
        """The whole figure in one call: torso, arms, fists, legs, shoes, head.

        `head` is the hood/coif family when hood=True (the face stays `skin`).
        Extra keyword arguments go to torso_part (pad, hem, collar, quilt, paint).
        """
        parts = [self.torso_part(torso, **torso_kwargs)]
        for side in SIDES:
            parts.append(self.arm_part(side, sleeves or torso))
            parts += self.hand_part(side, skin)
            parts.append(self.leg_part(side, legs or torso))
            parts.append(self.foot_part(side, feet or legs or torso))
        parts += self.head_part(head or skin, face=skin, hood=hood)
        return parts


# --------------------------------------------------------------------------------
# Quadruped
# --------------------------------------------------------------------------------

# Reference dog, metres: withers 0.72, nose-to-tail 1.42, chest 0.30 wide. Scaled
# per axis by the constructor. Body rings along Y (rump first): (y, centre z,
# half-width, half-height, keel) where keel narrows the underside.
_DOG_BODY = [
    (0.440, 0.600, 0.0, 0.0, 0.0),
    (0.410, 0.600, 0.075, 0.085, 0.10),
    (0.340, 0.595, 0.120, 0.115, 0.15),
    (0.240, 0.590, 0.122, 0.118, 0.20),
    (0.100, 0.598, 0.112, 0.108, 0.22),
    (-0.020, 0.572, 0.128, 0.148, 0.30),
    (-0.140, 0.553, 0.145, 0.170, 0.36),
    (-0.240, 0.558, 0.150, 0.164, 0.36),
    (-0.330, 0.570, 0.130, 0.138, 0.30),
    (-0.400, 0.585, 0.090, 0.098, 0.20),
    (-0.430, 0.592, 0.0, 0.0, 0.0),
]
_DOG_HEAD = [   # (y, centre z, half-width, half-height, squareness power)
    (-0.452, 0.782, 0.0, 0.0, 2.0),
    (-0.475, 0.785, 0.072, 0.056, 2.0),
    (-0.510, 0.785, 0.104, 0.065, 2.2),
    (-0.570, 0.775, 0.110, 0.064, 2.4),
    (-0.625, 0.755, 0.084, 0.060, 2.6),
    (-0.665, 0.737, 0.068, 0.056, 3.0),
    (-0.730, 0.726, 0.066, 0.052, 3.2),
    (-0.772, 0.730, 0.055, 0.043, 3.0),
    (-0.790, 0.735, 0.034, 0.030, 2.5),
    (-0.796, 0.736, 0.0, 0.0, 2.0),
]


class Quadruped(Figure):
    """A four-legged beast built on a reference hound, scaled per axis.

    withers      height at the top of the shoulders (Z scale).
    length       nose to tail tip (Y scale).
    chest_width  across the chest (X scale).
    segments     sides of limb and tail sections.

    Bones (Unity Generic): Root > Pelvis > Spine1 > Spine2 > Spine3 > Chest >
    Neck1 > Neck2 > Head > Jaw, Ear.L/R; Pelvis > Tail1..Tail5; Chest > Scapula.X >
    Humerus.X > Radius.X > Carpus.X > ForePaw.X; Pelvis > Femur.X > Tibia.X >
    Hock.X > HindPaw.X.
    """

    forward = ["Head", "ForePaw.L", "ForePaw.R"]
    test_pose = QUADRUPED_TEST_POSE

    def __init__(self, withers: float = 0.72, length: float = 1.42,
                 chest_width: float = 0.30, segments: int = 10):
        super().__init__()
        self.sx, self.sy, self.sz = chest_width / 0.30, length / 1.42, withers / 0.72
        self.segments = segments
        self._make_bones()

    def p(self, x, y, z) -> Vector:
        """A reference-dog point, scaled to this beast."""
        return Vector((x * self.sx, y * self.sy, z * self.sz))

    # Reference joint positions (the .L side; x mirrored for .R).
    _FORE = [(0.060, -0.270, 0.680), (0.115, -0.250, 0.550), (0.115, -0.210, 0.380),
             (0.105, -0.235, 0.100), (0.105, -0.250, 0.045), (0.105, -0.320, 0.030)]
    _HIND = [(0.105, 0.300, 0.560), (0.115, 0.200, 0.360), (0.105, 0.360, 0.155),
             (0.105, 0.330, 0.045), (0.105, 0.270, 0.030)]
    _TAIL = [(0.0, 0.400, 0.640), (0.0, 0.470, 0.630), (0.0, 0.545, 0.560),
             (0.0, 0.595, 0.450), (0.0, 0.615, 0.340), (0.0, 0.620, 0.250)]

    def _make_bones(self) -> None:
        p, add = self.p, self.add_bone
        add("Root", (0, 0, 0), (0, 0, 0.15 * self.sz), None)
        spine = [(0.360, 0.610), (0.240, 0.612), (0.100, 0.612), (-0.040, 0.604),
                 (-0.160, 0.602), (-0.280, 0.620)]
        names = ["Pelvis", "Spine1", "Spine2", "Spine3", "Chest"]
        parent = "Root"
        for name, (y0, z0), (y1, z1) in zip(names, spine, spine[1:]):
            add(name, p(0, y0, z0), p(0, y1, z1), parent)
            parent = name
        add("Neck1", p(0, -0.280, 0.620), p(0, -0.380, 0.710), "Chest")
        add("Neck2", p(0, -0.380, 0.710), p(0, -0.470, 0.770), "Neck1")
        add("Head", p(0, -0.470, 0.770), p(0, -0.790, 0.735), "Neck2")
        add("Jaw", p(0, -0.525, 0.722), p(0, -0.765, 0.688), "Head")
        for side, s in SIDES.items():
            add(f"Ear.{side}", p(s * 0.072, -0.500, 0.835), p(s * 0.112, -0.435, 0.800), "Head")
            f = [p(s * x, y, z) for x, y, z in self._FORE]
            add(f"Scapula.{side}", f[0], f[1], "Chest")
            add(f"Humerus.{side}", f[1], f[2], f"Scapula.{side}")
            add(f"Radius.{side}", f[2], f[3], f"Humerus.{side}")
            add(f"Carpus.{side}", f[3], f[4], f"Radius.{side}")
            add(f"ForePaw.{side}", f[4], f[5], f"Carpus.{side}")
            k = [p(s * x, y, z) for x, y, z in self._HIND]
            add(f"Femur.{side}", k[0], k[1], "Pelvis")
            add(f"Tibia.{side}", k[1], k[2], f"Femur.{side}")
            add(f"Hock.{side}", k[2], k[3], f"Tibia.{side}")
            add(f"HindPaw.{side}", k[3], k[4], f"Hock.{side}")
        t = [p(*q) for q in self._TAIL]
        parent = "Pelvis"
        for i in range(5):
            add(f"Tail{i + 1}", t[i], t[i + 1], parent)
            parent = f"Tail{i + 1}"

    # ---- rings -------------------------------------------------------------------

    def body_ring(self, y: float, cz: float, hw: float, hh: float, keel: float,
                  n: int, pad: float = 0.0, a0: float = 0.0, a1: float = 2 * math.pi,
                  closed: bool = True) -> list[tuple]:
        """Points on a body cross-section (XZ plane) at reference y, scaled.
        Angles run from a0 to a1 (0 = +X, pi/2 = up); keel narrows the underside."""
        out = []
        count = n if closed else n + 1
        for j in range(count):
            a = a0 + (a1 - a0) * j / n
            ca, sa = math.cos(a), math.sin(a)
            k = 1.0 - keel * max(0.0, -sa)
            x = ca * (hw * k * self.sx + pad)
            z = cz * self.sz + sa * (hh * self.sz + pad)
            out.append((x, y * self.sy, z))
        return out

    def body_at(self, y: float) -> tuple[float, float, float, float]:
        """(centre z, half-width, half-height, keel) of the reference body at y."""
        rows = sorted(_DOG_BODY[1:-1], key=lambda r: r[0])
        if y <= rows[0][0]:
            return rows[0][1:]
        for a, b in zip(rows, rows[1:]):
            if a[0] <= y <= b[0]:
                f = (y - a[0]) / (b[0] - a[0])
                return tuple(a[i] + (b[i] - a[i]) * f for i in range(1, 5))
        return rows[-1][1:]

    # ---- parts -------------------------------------------------------------------

    def body_part(self, mat: str, paint: list | None = None, segments: int = 16) -> Part:
        rings = []
        for y, cz, hw, hh, keel in _DOG_BODY:
            if hw == 0.0:
                rings.append([tuple(self.p(0, y, cz))])
            else:
                rings.append(self.body_ring(y, cz, hw, hh, keel, segments))
        return Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone="Spine2", extras={
            "rings": rings, "smooth": True, "paint": paint or [], "bevel": False,
            "bones": ["Pelvis", "Spine1", "Spine2", "Spine3", "Chest", "Neck1",
                      "Scapula.L", "Scapula.R", "Femur.L", "Femur.R", "Tail1"]})

    def neck_part(self, mat: str, segments: int | None = None) -> Part:
        p = self.p
        pts = [p(0, -0.250, 0.620), p(0, -0.330, 0.672), p(0, -0.400, 0.728),
               p(0, -0.450, 0.765), p(0, -0.500, 0.782)]
        secs = [(0.105, 0.115), (0.088, 0.092), (0.074, 0.076), (0.066, 0.068), (0.060, 0.060)]
        sections = [(a * self.sx, b * self.sz) for a, b in secs]
        return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone="Neck1",
                    segments=segments or self.segments + 2, extras={
                        "path": [tuple(q) for q in pts], "sections": sections,
                        "up": (1.0, 0.0, 0.0), "smooth": True, "bevel": False,
                        "bones": ["Chest", "Neck1", "Neck2", "Head"]})

    def neck_frame(self, t: float) -> tuple[Vector, Vector, float]:
        """(centre, tangent, radius) a fraction t along the neck, for collars."""
        p = self.p
        pts = [p(0, -0.250, 0.620), p(0, -0.330, 0.672), p(0, -0.400, 0.728),
               p(0, -0.450, 0.765), p(0, -0.500, 0.782)]
        radii = [0.110, 0.090, 0.075, 0.067, 0.060]
        pos, rad = _resample(pts, radii, 21)
        i = max(1, min(19, round(t * 20)))
        tangent = (pos[i + 1] - pos[i - 1]).normalized()
        return pos[i], tangent, rad[i] * self.sx

    def head_parts(self, mat: str, mask: str, nose: str | None = None,
                   teeth: str | None = None, segments: int = 14) -> list[Part]:
        """Skull + muzzle loft (front painted `mask`), lower jaw, ears, nose, fangs."""
        p = self.p
        rings = []
        for y, cz, hw, hh, power in _DOG_HEAD:
            if hw == 0.0:
                rings.append([tuple(p(0, y, cz))])
                continue
            e = 2.0 / power
            ring = []
            for j in range(segments):
                a = 2.0 * math.pi * j / segments
                ca, sa = math.cos(a), math.sin(a)
                x = math.copysign(abs(ca) ** e, ca) * hw
                # Heavy flews: the lower half of the muzzle hangs lower and wider.
                drop = 1.18 if (sa < 0 and y < -0.64) else 1.0
                z = cz + math.copysign(abs(sa) ** e, sa) * hh * drop
                ring.append(tuple(p(x, y, z)))
            rings.append(ring)
        stop = p(0, -0.628, 0).y
        paint = [{"mat": mask, "min": (-1, -2, -1), "max": (1, stop, 2)}]
        parts = [Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone="Head", extras={
            "rings": rings, "smooth": True, "paint": paint, "bevel": False,
            "bones": ["Head", "Neck2"]})]

        jaw = []
        for y, cz, hw, hh in ((-0.520, 0.708, 0.050, 0.026), (-0.600, 0.694, 0.056, 0.026),
                              (-0.700, 0.684, 0.054, 0.022), (-0.760, 0.688, 0.040, 0.016)):
            jaw.append([tuple(p(math.cos(2 * math.pi * j / 10) * hw, y,
                                cz + math.sin(2 * math.pi * j / 10) * hh)) for j in range(10)])
        jaw.append([tuple(p(0, -0.772, 0.690))])
        parts.append(Part("loft", (0, 0, 0), (1, 1, 1), mat=mask, bone="Jaw", extras={
            "rings": jaw, "smooth": True, "rigid": True, "bevel": False}))

        for side, s in SIDES.items():
            # Rose ears: folded back and down against the skull, not pricked.
            ear = [p(s * 0.066, -0.505, 0.836), p(s * 0.098, -0.478, 0.836),
                   p(s * 0.118, -0.452, 0.806), p(s * 0.121, -0.445, 0.770)]
            parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat=mask, bone=f"Ear.{side}",
                              segments=6, extras={
                                  "path": [tuple(q) for q in ear],
                                  "sections": [(0.028, 0.008), (0.036, 0.007),
                                               (0.026, 0.006), (0.0, 0.0)],
                                  "up": (s * 1.0, 0.0, 0.6), "smooth": True, "rigid": True,
                                  "bevel": False}))
            eye = p(s * 0.058, -0.622, 0.790)
            parts.append(Part("sphere", tuple(eye), (0.024, 0.016, 0.018), mat=mask,
                              bone="Head", segments=8, rings=5, rot=(0, 0, s * 25.0),
                              extras={"rigid": True, "bevel": False}))
        parts.append(Part("sphere", tuple(p(0, -0.790, 0.748)), (0.060, 0.030, 0.040),
                          mat=nose or mask, bone="Head", segments=10, rings=6,
                          extras={"rigid": True, "bevel": False}))
        if teeth:
            for s in (1.0, -1.0):
                for y, z, length in ((-0.748, 0.700, 0.030), (-0.750, 0.708, -0.028)):
                    bone = "Jaw" if length > 0 else "Head"
                    base = p(s * 0.034, y, z)
                    parts.append(Part("cone", tuple(base + Vector((0, 0, length / 2))),
                                      (0.012, 0.012, abs(length)), mat=teeth, bone=bone,
                                      rot=(0.0 if length > 0 else 180.0, 0, 0), segments=5,
                                      extras={"rigid": True, "bevel": False}))
        return parts

    def foreleg_part(self, side: str, mat: str, segments: int | None = None) -> Part:
        s = SIDES[side]
        pts = [self.p(s * x, y, z) for x, y, z in self._FORE[:5]]
        pts.insert(3, pts[2].lerp(pts[3], 0.45))
        secs = [(0.050, 0.075), (0.056, 0.072), (0.046, 0.052), (0.044, 0.046),
                (0.032, 0.034), (0.029, 0.031)]
        return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=f"Humerus.{side}",
                    segments=segments or self.segments, extras={
                        "path": [tuple(q) for q in pts],
                        "sections": [(a * self.sx, b * self.sy) for a, b in secs],
                        "up": (1.0, 0.0, 0.0), "smooth": True, "bevel": False,
                        "bones": ["Chest", f"Scapula.{side}", f"Humerus.{side}",
                                  f"Radius.{side}", f"Carpus.{side}"]})

    def hindleg_part(self, side: str, mat: str, segments: int | None = None) -> Part:
        s = SIDES[side]
        hip, stifle, hock, foot, _toe = [self.p(s * x, y, z) for x, y, z in self._HIND]
        inside = self.p(s * 0.055, 0.300, 0.640)
        pts = [inside, hip, hip.lerp(stifle, 0.5), stifle, stifle.lerp(hock, 0.45), hock,
               hock.lerp(foot, 0.5), foot]
        secs = [(0.070, 0.120), (0.080, 0.125), (0.074, 0.108), (0.046, 0.056),
                (0.036, 0.050), (0.028, 0.034), (0.025, 0.029), (0.025, 0.028)]
        return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=f"Femur.{side}",
                    segments=segments or self.segments, extras={
                        "path": [tuple(q) for q in pts],
                        "sections": [(a * self.sx, b * self.sy) for a, b in secs],
                        "up": (1.0, 0.0, 0.0), "smooth": True, "bevel": False,
                        "bones": ["Pelvis", f"Femur.{side}", f"Tibia.{side}", f"Hock.{side}"]})

    def paw_part(self, side: str, mat: str, hind: bool = False) -> Part:
        s = SIDES[side]
        if hind:
            at, bone = self.p(s * 0.105, 0.305, 0.034), f"HindPaw.{side}"
        else:
            at, bone = self.p(s * 0.105, -0.278, 0.036), f"ForePaw.{side}"
        return Part("sphere", tuple(at), (0.088 * self.sx, 0.108 * self.sy, 0.072 * self.sz),
                    mat=mat, bone=bone, segments=10, rings=6,
                    extras={"bevel": False, "rigid": True})

    def tail_part(self, mat: str, segments: int = 8) -> Part:
        ctrl = [self.p(*q) for q in self._TAIL]
        pts = [Vector(q) for q in spline([tuple(c) for c in ctrl], 2)]
        pts, _unused = _resample(pts, [0.0] * len(pts), 11)
        radii = [0.034, 0.032, 0.029, 0.026, 0.022, 0.019, 0.016, 0.013, 0.010, 0.007, 0.0]
        return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone="Tail2",
                    segments=segments, extras={
                        "path": [tuple(q) for q in pts],
                        "sections": [(r * self.sx, r * self.sx) for r in radii],
                        "up": (1.0, 0.0, 0.0), "smooth": True, "bevel": False,
                        "bones": ["Pelvis", "Tail1", "Tail2", "Tail3", "Tail4", "Tail5"]})

    def body(self, coat: str, mask: str, nose: str | None = None,
             teeth: str | None = None) -> list[Part]:
        """The whole bare animal in one call."""
        parts = [self.body_part(coat), self.neck_part(coat), self.tail_part(coat)]
        parts += self.head_parts(coat, mask, nose=nose, teeth=teeth)
        for side in SIDES:
            parts.append(self.foreleg_part(side, coat))
            parts.append(self.hindleg_part(side, coat))
            parts.append(self.paw_part(side, coat))
            parts.append(self.paw_part(side, coat, hind=True))
        return parts
