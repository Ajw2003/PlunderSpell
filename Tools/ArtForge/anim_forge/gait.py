"""Parametric in-place gait, matched to the agent speed so the feet do not slide.

The clip is in place: the NavMeshAgent moves the guard at `speed` m/s along -Y
(his front). So a planted foot must move BACKWARD (+Y) at exactly `speed` in the
clip's frame; in the world, where the agent's travel is added back, it stands
still. Each foot is driven by a target, and the legs reach it by IK:

    stance (phase 0..duty):  the flat-foot heel point slides +Y at `speed`; the
                             foot rolls on the heel (heel rocker, toe up), lies
                             flat, then rolls on the ball (toe-off), each pivot
                             staying on its own ground point.
    swing  (duty..1):        the ankle travels from toe-off to the next heel
                             strike on a minimum-jerk curve, lifted by
                             `clearance`, never below the floor.

Cycle = two steps; the left heel strikes at t = 0, the right at cycle / 2 (the
clip's Footstep events). Stride = speed * cycle. The hips bob (highest at
mid-stance for a walk, lowest for a run), sway toward the stance leg, yaw and roll
with the pelvis; `drop` (how much lower than standing the hips ride) is solved so
no leg is asked to reach more than `max_extension` of its length, which is what
keeps the IK from ever falling short (a short leg would slide).
"""

from __future__ import annotations

import math
from dataclasses import dataclass, replace

from mathutils import Vector

from . import mathx
from .clip import Clip, Frame, foot_geometry, foot_pose
from .mathx import euler_q


@dataclass
class GaitParams:
    speed: float            # m/s the agent moves
    cycle: float            # seconds for two steps
    duty: float = 0.60      # stance fraction of the cycle per foot
    heel_pitch: float = 16.0    # toe-up at heel strike (deg)
    toe_pitch: float = 38.0     # heel-up at toe-off (deg)
    heel_rocker: float = 0.14   # stance fraction rolling on the heel
    toe_rocker: float = 0.42    # stance fraction (from the end) rolling on the ball
    clearance: float = 0.06     # extra ankle lift mid-swing (m)
    kick: float = 0.0           # heel kicks back toward the seat (run), m
    world_lock: float = 0.3     # swing interpolation: 0 clip space .. 1 world space
    ahead: float = -0.03        # ankle y at mid-stance relative to the hip (- = ahead)
    width: float = 0.9          # heel x as a fraction of rest
    bob: float = 0.018          # hip bob amplitude (m)
    run: bool = False           # bob phase: lowest at mid-stance
    max_extension: float = 0.985
    lean: float = 3.0           # spine forward lean (deg)
    pelvis_yaw: float = 6.0
    pelvis_roll: float = 4.0
    sway: float = 0.022
    arm_swing: float = 18.0
    arm_spread: float = 4.0     # extra spread so sleeves clear the coat
    elbow: float = 14.0
    elbow_swing: float = 12.0
    head_bob: float = 2.0


class GaitClip(Clip):
    """A Clip whose sample() is the parametric gait (keys unused)."""

    def __init__(self, name: str, params: GaitParams, ref, fps: int = 30, notes: str = ""):
        frames = max(2, round(params.cycle * fps))
        params = replace(params, cycle=frames / fps)      # a whole number of frames
        super().__init__(name, params.cycle, loop=True, feet_ik=True, notes=notes)
        self.p = params
        self.speed = params.speed
        self.ref = ref
        self.drop = 0.0
        self.drop = self._solve_drop()

    # ---- feet -------------------------------------------------------------------

    def _heel_strike_y(self, side: str) -> float:
        p = self.p
        g = foot_geometry(self.ref, side)
        back = g["heel"].y - g["ankle"].y          # heel is this far behind the ankle
        return p.ahead + back - p.speed * p.duty * p.cycle / 2.0

    def phase(self, side: str, t: float) -> float:
        return ((t / self.p.cycle) + (0.0 if side == "L" else 0.5)) % 1.0

    def planted(self, side: str, t: float) -> bool:
        return self.phase(side, t) < self.p.duty

    def foot(self, side: str, t: float):
        p = self.p
        g = foot_geometry(self.ref, side)
        x = g["heel"].x * p.width
        y0 = self._heel_strike_y(side)
        psi = self.phase(side, t)
        if psi < p.duty:
            s = psi / p.duty
            y = y0 + p.speed * psi * p.cycle
            if s < p.heel_rocker:
                pitch = -p.heel_pitch * (1.0 - mathx.ease("out", s / p.heel_rocker))
            elif s > 1.0 - p.toe_rocker:
                pitch = p.toe_pitch * mathx.ease("in", (s - (1.0 - p.toe_rocker)) / p.toe_rocker)
            else:
                pitch = 0.0
            return foot_pose(self.ref, side, Vector((x, y, 0.0)), pitch)
        u = (psi - p.duty) / (1.0 - p.duty)
        a0, _q0 = foot_pose(self.ref, side, Vector((x, y0 + p.speed * p.duty * p.cycle, 0.0)),
                            p.toe_pitch)
        a1, _q1 = foot_pose(self.ref, side, Vector((x, y0, 0.0)), -p.heel_pitch)
        # Interpolate in the WORLD (the agent travels `travel` along -Y during the
        # swing), with a minimum-jerk curve: zero world velocity at lift-off and at
        # the strike, so a foot still brushing the floor does not smear.
        # `world_lock` 1 = pure world-space (no smear, but the foot overshoots
        # forward before the strike and costs hip height); 0 = clip space.
        travel = p.speed * (1.0 - p.duty) * p.cycle
        m = mathx.ease("smooth", u)
        ankle = a0.lerp(a1, m) + Vector((0.0, p.world_lock * travel * (u - m), 0.0))
        # The lift arc spans the whole swing, so the foot is clear of the floor on
        # every swing frame and meets it only at the strike.
        ankle.z += p.clearance * math.sin(math.pi * u) ** 0.8
        ankle.y += p.kick * math.sin(math.pi * u) * (1.0 - u) * 1.6
        ankle.z += p.kick * 0.9 * math.sin(math.pi * u) * (1.0 - u) * 1.6
        pitch = p.toe_pitch + (-p.heel_pitch - p.toe_pitch) * mathx.ease("inout", u)
        pitch -= 8.0 * math.sin(math.pi * u)
        q = euler_q((pitch, 0.0, 0.0))
        # Never through the floor: lift so the lowest sole point is at >= 0.
        low = min((ankle + q @ (g[k] - g["ankle"])).z for k in ("heel", "ball", "toe"))
        if low < 0.0:
            ankle.z -= low
        return ankle, q

    # ---- body -------------------------------------------------------------------

    def hips(self, t: float) -> Vector:
        p = self.p
        w = 4.0 * math.pi * (t / p.cycle - p.duty / 2.0)
        z = p.bob * math.cos(w) * (-1.0 if p.run else 1.0) - p.bob - self.drop
        x = p.sway * math.cos(2.0 * math.pi * (t / p.cycle - p.duty / 2.0))
        return Vector((x, 0.0, z))

    def pelvis(self, t: float):
        p = self.p
        c = math.cos(2.0 * math.pi * t / p.cycle)
        r = math.cos(2.0 * math.pi * (t / p.cycle - p.duty / 2.0))
        return -p.pelvis_yaw * c, -p.pelvis_roll * r

    def upper(self, t: float) -> dict:
        p = self.p
        yaw, roll = self.pelvis(t)
        c = math.cos(2.0 * math.pi * t / p.cycle)
        c2 = math.cos(4.0 * math.pi * (t / p.cycle - p.duty / 2.0))
        pose = {
            "Hips": (0.0, roll, yaw),
            "Spine": (p.lean * 0.6, -roll * 0.6, -yaw * 0.9),
            "Chest": (p.lean * 0.4 + (1.0 if p.run else 0.5) * c2, -roll * 0.4, -yaw * 0.7),
            "Neck": (-p.lean * 0.5, 0.0, yaw * 0.3),
            "Head": (-p.lean * 0.4 - p.head_bob * c2, 0.0, yaw * 0.3),
        }
        for side, sgn in (("L", 1.0), ("R", -1.0)):
            swing = sgn * p.arm_swing * c                 # + = back
            fwd = max(0.0, -swing) / max(p.arm_swing, 1e-6)
            pose[f"UpperArm.{side}"] = (swing, sgn * -p.arm_spread, 0.0)
            pose[f"LowerArm.{side}"] = (-(p.elbow + p.elbow_swing * fwd), 0.0, 0.0)
            pose[f"Hand.{side}"] = (-4.0, 0.0, 0.0)
        return pose

    def sample(self, t: float, ref=None) -> Frame:
        pose = {k: euler_q(v) for k, v in self.upper(t).items()}
        feet = {side: self.foot(side, t) for side in ("L", "R")}
        frame = Frame(t, pose, self.hips(t), feet, None, 0.0, {}, {})
        for layer in self.layers:
            layer(self, t, frame)
        return frame

    # ---- drop -------------------------------------------------------------------

    def _reach_ok(self, drop: float) -> bool:
        from .solve import _world_chain
        self.drop = drop
        ref = self.ref
        for i in range(48):
            t = self.p.cycle * i / 48
            frame = self.sample(t)
            Qx = _world_chain(ref, frame.pose)
            posed = ref.fk(Qx, frame.hips)
            for side in ("L", "R"):
                hip = posed.heads[f"UpperLeg.{side}"]
                ankle = frame.feet[side][0]
                full = ref.length(f"UpperLeg.{side}") + ref.length(f"LowerLeg.{side}")
                if (ankle - hip).length > full * self.p.max_extension:
                    return False
        return True

    def _solve_drop(self) -> float:
        drop = 0.0
        while not self._reach_ok(drop):
            drop += 0.002
            if drop > 0.4:
                raise RuntimeError(f"{self.name}: no hip height lets the legs reach; stride too long")
        return drop

    def stride(self) -> float:
        return self.p.speed * self.p.cycle
