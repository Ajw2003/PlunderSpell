"""Clips: timed keys of named poses, eased, with foot and weapon targets.

A clip is authored on the REFERENCE human (skeleton.reference_bones) and sampled
into a `Frame` at any time t (seconds). The solver (solve.py) turns a Frame into
world-delta rotations with IK: the legs reach their foot targets, the right hand
carries the weapon to its key, the left hand grips the haft.

    c = Clip("polearm_thrust", 0.9)
    c.key(0.00, "polearm_ready", weapon=WeaponKey((...), (...)), ease="inout")
    c.key(0.34, "thrust_draw", weapon=..., ease="anticipate")   # arrives pulling back
    c.key(0.45, "thrust_extend", weapon=..., ease="in",           # fast strike
          feet={"R": FootKey(y=-0.28)})                           # lead foot steps
    ...

Keys are full poses: a bone a key does not name is at rest (identity) in that key,
so poses are written as `merge(base, overrides)` (poses.py). The ease on a key
shapes the segment ARRIVING at it; "anticipate" dips back before leaving,
"overshoot" passes the key and settles (see mathx.EASE).
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field

from mathutils import Quaternion, Vector

from . import mathx
from .mathx import ease as _ease, euler_q, slerp


@dataclass
class FootKey:
    """Where one foot is at a key, in reference armature space (metres).

    x, y     the flat-foot HEEL ground point; None = the rest position
    pitch    degrees, + = heel up (rolls about the ball), - = toe up (about the heel)
    lift     metres the whole foot is raised (mid-step arcs are automatic)
    yaw      degrees about Z
    """
    x: float | None = None
    y: float | None = None
    pitch: float = 0.0
    lift: float = 0.0
    yaw: float = 0.0


@dataclass
class WeaponKey:
    """The two-handed weapon at a key: right grip point, haft direction (toward the
    tip) and blade-edge direction, in reference armature space."""
    grip: tuple
    dir: tuple
    edge: tuple = (0.0, -1.0, 0.0)


@dataclass
class Key:
    t: float
    pose: dict                      # {bone: (rx, ry, rz)} world-axis degrees
    hips: tuple = (0.0, 0.0, 0.0)
    ease: str = "inout"
    feet: dict = field(default_factory=dict)       # side -> FootKey
    weapon: WeaponKey | None = None
    lhand: float | None = None      # left-hand IK weight onto the haft
    elbows: dict = field(default_factory=dict)     # side -> pole hint (x, y, z)
    extras: dict = field(default_factory=dict)     # non-human bone -> (rx, ry, rz)


@dataclass
class Frame:
    t: float
    pose: dict[str, Quaternion]           # parent-relative (world axes) per human bone
    hips: Vector
    feet: dict | None                     # side -> (ankle Vector, foot Quaternion); None = FK legs
    weapon: tuple | None                  # (grip Vector, rotation Quaternion) of the weapon frame
    lhand: float
    elbows: dict
    extras: dict[str, Quaternion]


class Clip:
    def __init__(self, name: str, length: float, loop: bool = False, feet_ik: bool = True,
                 notes: str = ""):
        self.name = name
        self.length = float(length)
        self.loop = loop
        self.feet_ik = feet_ik
        self.notes = notes
        self.keys: list[Key] = []
        self.speed = None           # set by gait clips (m/s the agent moves)
        self.layers: list = []      # callables (clip, t, frame) -> None, e.g. breathing
        self.contacts = None        # gait clips: planted spans for the review trace

    def key(self, t: float, pose: dict, **kw) -> "Clip":
        if self.keys and t <= self.keys[-1].t:
            raise ValueError(f"{self.name}: key at {t} s is not after {self.keys[-1].t} s")
        if t > self.length + 1e-9:
            raise ValueError(f"{self.name}: key at {t} s is past the clip's {self.length} s")
        self.keys.append(Key(t, dict(pose), **kw))
        return self

    def add_layer(self, fn) -> "Clip":
        self.layers.append(fn)
        return self

    # ---- sampling ---------------------------------------------------------------

    def _segment(self, t: float) -> tuple[Key, Key, float]:
        keys = self.keys
        if not keys:
            raise ValueError(f"{self.name}: no keys")
        if t <= keys[0].t:
            return keys[0], keys[0], 0.0
        for a, b in zip(keys, keys[1:]):
            if t <= b.t:
                u = (t - a.t) / (b.t - a.t)
                return a, b, u
        return keys[-1], keys[-1], 0.0

    def sample(self, t: float, ref) -> Frame:
        a, b, u = self._segment(t)
        f = _ease(b.ease, u)
        bones = set(a.pose) | set(b.pose)
        pose = {n: slerp(euler_q(a.pose.get(n, (0, 0, 0))), euler_q(b.pose.get(n, (0, 0, 0))), f)
                for n in bones}
        hips = Vector(a.hips).lerp(Vector(b.hips), f)
        extras = {n: slerp(euler_q(a.extras.get(n, (0, 0, 0))), euler_q(b.extras.get(n, (0, 0, 0))), f)
                  for n in set(a.extras) | set(b.extras)}
        weapon = None
        if a.weapon or b.weapon:
            wa, wb = a.weapon or b.weapon, b.weapon or a.weapon
            qa, qb = weapon_rotation(wa), weapon_rotation(wb)
            weapon = (Vector(wa.grip).lerp(Vector(wb.grip), f), slerp(qa, qb, f))
        la = a.lhand if a.lhand is not None else (1.0 if a.weapon else 0.0)
        lb = b.lhand if b.lhand is not None else (1.0 if b.weapon else 0.0)
        lhand = la + (lb - la) * mathx.ease("inout", u)
        elbows = {}
        for side in ("L", "R"):
            ea, eb = a.elbows.get(side), b.elbows.get(side)
            if ea or eb:
                elbows[side] = Vector(ea or eb).lerp(Vector(eb or ea), f)
        feet = None
        if self.feet_ik:
            feet = {}
            for side in ("L", "R"):
                fa, fb = a.feet.get(side, FootKey()), b.feet.get(side, FootKey())
                feet[side] = foot_target(ref, side, fa, fb, u)
        frame = Frame(t, pose, hips, feet, weapon, lhand, elbows, extras)
        for layer in self.layers:
            layer(self, t, frame)
        return frame

    def times(self, fps: int) -> list[float]:
        n = max(1, round(self.length * fps))
        return [i / fps for i in range(n + 1)]


def weapon_rotation(w: WeaponKey) -> Quaternion:
    """Rotation from the canonical weapon frame (haft +Z, edge -Y) to this key's."""
    return mathx.frame_rot(mathx.Z, -mathx.Y, Vector(w.dir), Vector(w.edge))


# ---- feet ----------------------------------------------------------------------

def foot_geometry(ref, side: str) -> dict:
    """Rest foot landmarks of a skeleton: ankle, heel ground point, ball ground point."""
    ankle = ref.head[f"Foot.{side}"].copy()
    ball = ref.tail[f"Foot.{side}"].copy()
    heel = Vector((ankle.x, ankle.y + 0.045, 0.0))
    ball_g = Vector((ball.x, ball.y, 0.0))
    toe = ball_g + (ball_g - heel).normalized() * 0.05
    return {"ankle": ankle, "heel": heel, "ball": ball_g, "toe": toe}


def foot_pose(ref, side: str, heel_xy: Vector, pitch: float, lift: float = 0.0,
              yaw: float = 0.0) -> tuple[Vector, Quaternion]:
    """Ankle position and foot rotation for a flat-foot heel point on the ground,
    rolled by `pitch` about the heel (toe up, pitch < 0) or the toe tip (heel up).

    The rigs have no toe bone, so a heel-up foot rolls on the tip of the shoe: rolling
    on the ball would push the rigid toe through the floor."""
    g = foot_geometry(ref, side)
    rot = mathx.euler_q((pitch, 0.0, yaw))
    yaw_q = mathx.euler_q((0.0, 0.0, yaw))
    rest_pivot = g["heel"] if pitch < 0.0 else g["toe"]
    pivot = g["heel"] + yaw_q @ (rest_pivot - g["heel"])
    pivot = pivot + Vector((heel_xy.x, heel_xy.y, lift)) - g["heel"]
    ankle = pivot + rot @ (g["ankle"] - rest_pivot)
    return ankle, rot


def foot_target(ref, side: str, a: FootKey, b: FootKey, u: float) -> tuple[Vector, Quaternion]:
    g = foot_geometry(ref, side)
    pa = Vector((g["heel"].x if a.x is None else a.x, g["heel"].y if a.y is None else a.y))
    pb = Vector((g["heel"].x if b.x is None else b.x, g["heel"].y if b.y is None else b.y))
    moving = (pb - pa).length > 1e-4
    s = mathx.ease("smooth", u) if moving else 0.0
    xy = pa.lerp(pb, s)
    arc = 0.07 * math.sin(math.pi * u) * min(1.0, (pb - pa).length / 0.15) if moving else 0.0
    pitch = a.pitch + (b.pitch - a.pitch) * mathx.ease("inout", u)
    if moving:
        pitch -= 12.0 * math.sin(math.pi * u)    # toe up mid-step clears the floor
    lift = a.lift + (b.lift - a.lift) * mathx.ease("inout", u) + arc
    yaw = a.yaw + (b.yaw - a.yaw) * mathx.ease("inout", u)
    return foot_pose(ref, side, Vector((xy.x, xy.y, 0.0)), pitch, lift, yaw)
