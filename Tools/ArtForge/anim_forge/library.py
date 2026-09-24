"""Every clip AnimForge builds in phase A1, authored on the reference human.

`clips(ref, warden)` returns {clip id: ClipDef}. Ids, FBX files, loop flags and
event times come from Tools/ArtForge/anim_spec.json (the source of truth); this
module supplies the motion. `anim.py build` fails if the two disagree.

Coordinates: metres in the reference's armature space, the figure facing -Y, +X its
own left. The reference right fist rests at (-0.27, -0.10, 0.81).
"""

from __future__ import annotations

import math
from dataclasses import dataclass, field

from . import poses as P
from .clip import Clip, FootKey, WeaponKey
from .gait import GaitClip, GaitParams
from .poses import add, merge, mirror

BOTH_ARMS = {f"{b}.{s}" for b in ("Shoulder", "UpperArm", "LowerArm", "Hand") for s in "LR"}
RIGHT_ARM = {f"{b}.R" for b in ("Shoulder", "UpperArm", "LowerArm", "Hand")}
WARDEN_PROPS = {"Glaive", "LanternRing", "LanternBody"}


@dataclass
class ClipDef:
    clip: Clip
    rig: str = "ref"                 # "ref" (Humanoid FBX) or "warden" (its own FBX)
    weapon: bool = False             # two-handed weapon IK (polearm family)
    carry_mask: set = field(default_factory=set)   # review: warden carry over these bones
    agent_yaw: object = None         # review: t -> degrees the agent turns (alert_turn)
    review_lift: float = 0.0         # review: metres the engine lifts him (levitate)
    carry: str = "warden_carry"      # review: which warden carry pose the layer plays


def _loop_close(c: Clip, pose, **kw):
    c.key(c.length, pose, **kw)


# ---------------------------------------------------------------------------------
# Base
# ---------------------------------------------------------------------------------

WALK = GaitParams(speed=2.0, cycle=0.8667, duty=0.56, ahead=0.06, toe_pitch=45.0,
                  heel_pitch=22.0, pelvis_yaw=8.0, bob=0.018, clearance=0.06, lean=4.0,
                  arm_swing=20.0, elbow=16.0, elbow_swing=14.0)
WALK_SLOW = GaitParams(speed=1.1, cycle=1.1, duty=0.60, ahead=0.02, toe_pitch=40.0,
                       heel_pitch=18.0, pelvis_yaw=5.0, pelvis_roll=3.0, bob=0.012,
                       clearance=0.045, lean=2.0, arm_swing=12.0, elbow=12.0, elbow_swing=8.0,
                       sway=0.018)
RUN = GaitParams(speed=4.2, cycle=0.6667, duty=0.32, ahead=0.12, toe_pitch=55.0,
                 heel_pitch=5.0, heel_rocker=0.10, toe_rocker=0.5, run=True, bob=0.03,
                 clearance=0.10, kick=0.20, max_extension=0.96, lean=11.0, pelvis_yaw=9.0,
                 pelvis_roll=4.0, sway=0.012, width=0.7, arm_swing=34.0, arm_spread=8.0,
                 elbow=78.0, elbow_swing=18.0, head_bob=1.5)


def idle(ref) -> Clip:
    c = Clip("idle", 3.0, loop=True, notes="weight shifts left, right; breathing; glances")
    base = P.RELAXED
    c.key(0.00, add(base, P.WEIGHT_LEFT), hips=(0.022, 0.0, -0.010))
    c.key(0.75, add(base, P.WEIGHT_LEFT, P.BREATH_IN, P.LOOK_LEFT), hips=(0.020, 0.0, -0.008))
    c.key(1.50, add(base, P.WEIGHT_RIGHT, P.scale(P.LOOK_LEFT, 0.3)), hips=(-0.022, 0.0, -0.010))
    c.key(2.25, add(base, P.WEIGHT_RIGHT, P.BREATH_IN, P.scale(P.LOOK_RIGHT, 0.6)),
          hips=(-0.020, 0.0, -0.008))
    _loop_close(c, add(base, P.WEIGHT_LEFT), hips=(0.022, 0.0, -0.010))
    return c


def alert_turn(ref) -> Clip:
    c = Clip("alert_turn", 0.6, notes="turn LEFT 90 deg: the head leads, the chest follows, "
             "the hips lag; the agent yaws the body 90 deg over the same 0.6 s")
    r = P.RELAXED
    c.key(0.00, r)
    # anticipation: a tense half-beat before the snap
    c.key(0.08, add(r, P.TENSE, {"Hips": (0, 0, -6)}), hips=(0, 0, -0.02),
          feet={"R": FootKey(yaw=-8)}, ease="out")
    c.key(0.20, add(r, P.TENSE, P.HEAD_SNAP_LEFT, {"Hips": (0, 0, -22)}), hips=(0, 0.01, -0.03),
          feet={"L": FootKey(yaw=-24), "R": FootKey(yaw=-30, pitch=8)}, ease="out")
    c.key(0.33, add(r, P.TENSE, P.scale(P.HEAD_SNAP_LEFT, 0.5), P.TURN_CHEST_LEFT,
                    {"Hips": (0, 0, -12)}), hips=(0, 0.0, -0.025),
          feet={"L": FootKey(x=0.16, y=0.0, yaw=-4), "R": FootKey(yaw=-18, pitch=4)})
    c.key(0.45, add(r, P.scale(P.TENSE, 0.6), P.scale(P.TURN_CHEST_LEFT, 0.4)),
          hips=(0, 0.0, -0.015), feet={"L": FootKey(x=0.16, y=0.0), "R": FootKey(yaw=-6)})
    c.key(0.60, add(r, P.scale(P.TENSE, 0.3)), hips=(0, 0, -0.008),
          feet={"L": FootKey(), "R": FootKey()}, ease="out")
    return c


def alert_turn_yaw(t: float) -> float:
    from .mathx import ease
    return 90.0 * ease("smooth", min(1.0, t / 0.52))


def shout(ref) -> Clip:
    c = Clip("shout", 1.2, notes="cups the left hand to the mouth and bellows")
    r = P.RELAXED
    c.key(0.00, r)
    c.key(0.28, add(r, P.SHOUT_INHALE), hips=(0, 0.01, 0.005), ease="inout")
    c.key(0.42, add(r, P.SHOUT_BELLOW), hips=(0, -0.02, -0.01), ease="in")
    c.key(0.50, add(r, P.SHOUT_BELLOW, {"Chest": (2, 0, 0), "Head": (-3, 0, 0)}),
          hips=(0, -0.025, -0.012), ease="overshoot")
    c.key(0.90, add(r, P.SHOUT_BELLOW, {"Chest": (-1, 0, 0)}), hips=(0, -0.02, -0.01))
    c.key(1.20, r, ease="out")
    return c


def hit_react(ref) -> Clip:
    c = Clip("hit_react", 0.4, notes="additive flinch (reference frame 0)")
    c.key(0.00, P.RELAXED)
    c.key(0.07, add(P.RELAXED, P.FLINCH), hips=(0, 0.03, -0.015), ease="out")
    c.key(0.40, P.RELAXED, ease="out")
    return c


def stagger(ref) -> Clip:
    c = Clip("stagger", 0.8, notes="recoil, catch step back with the right foot, recover")
    r = P.RELAXED
    c.key(0.00, r)
    c.key(0.12, add(r, P.RECOIL), hips=(0, 0.09, -0.05), ease="out")
    c.key(0.32, add(r, P.CATCH_BALANCE), hips=(0, 0.12, -0.09),
          feet={"R": FootKey(y=0.32, pitch=10)}, ease="inout")
    c.key(0.52, add(r, P.scale(P.CATCH_BALANCE, 0.5)), hips=(0, 0.06, -0.05),
          feet={"R": FootKey(y=0.32)})
    c.key(0.62, add(r, P.scale(P.CATCH_BALANCE, 0.3)), hips=(0, 0.03, -0.03),
          feet={"R": FootKey(y=0.32)})
    c.key(0.80, r, feet={"R": FootKey()}, ease="out")
    return c


def knock_down(ref) -> Clip:
    c = Clip("knock_down", 1.2, notes="non-lethal fall onto the back; ends in LYING (get_up's start)")
    r = P.RELAXED
    c.key(0.00, r)
    c.key(0.14, add(r, P.RECOIL), hips=(0, 0.10, -0.08), ease="out")
    c.key(0.42, P.FALL_BACK, hips=(0, 0.26, -0.42),
          feet={"L": FootKey(y=-0.10), "R": FootKey(y=-0.16)}, ease="in")
    c.key(0.66, P.SIT_HIT, hips=(0, 0.40, -0.79),
          feet={"L": FootKey(y=-0.30, pitch=-20), "R": FootKey(y=-0.34, pitch=-24)}, ease="in")
    c.key(0.92, P.LYING, hips=(0, 0.52, -0.815),
          feet={"L": FootKey(y=-0.30, pitch=-70), "R": FootKey(y=-0.32, pitch=-70)}, ease="in")
    c.key(1.02, add(P.LYING, {"Neck": (-8, 0, 0), "Head": (-6, 0, 0)}), hips=(0, 0.52, -0.80),
          feet={"L": FootKey(y=-0.30, pitch=-70), "R": FootKey(y=-0.32, pitch=-70)}, ease="out")
    c.key(1.20, P.LYING, hips=(0, 0.52, -0.815),
          feet={"L": FootKey(y=-0.30, pitch=-70), "R": FootKey(y=-0.32, pitch=-70)}, ease="inout")
    return c


def get_up(ref) -> Clip:
    c = Clip("get_up", 2.4, notes="from LYING: sit up, draw the feet in, rock forward to a squat, stand")
    lie = {"L": FootKey(y=-0.30, pitch=-70), "R": FootKey(y=-0.32, pitch=-70)}
    c.key(0.00, P.LYING, hips=(0, 0.52, -0.815), feet=lie)
    c.key(0.55, P.SIT_UP, hips=(0, 0.46, -0.80),
          feet={"L": FootKey(y=0.02), "R": FootKey(y=-0.02)}, ease="inout")
    c.key(1.15, P.SQUAT_RISE, hips=(0, 0.10, -0.46),
          feet={"L": FootKey(y=0.02), "R": FootKey(y=-0.02)}, ease="inout")
    c.key(1.70, P.HALF_RISE, hips=(0, 0.03, -0.18),
          feet={"L": FootKey(y=0.02), "R": FootKey(y=-0.02)}, ease="inout")
    c.key(2.05, P.RELAXED, hips=(0, 0.0, -0.01), feet={"L": FootKey(), "R": FootKey()},
          ease="inout")
    c.key(2.40, P.RELAXED, ease="out")
    return c


def archway_duck(ref) -> Clip:
    c = Clip("archway_duck", 0.5, notes="additive: head and shoulders dip ~0.12 m")
    c.key(0.00, P.RELAXED)
    c.key(0.20, add(P.RELAXED, P.DUCK), hips=(0, 0.0, -0.06), ease="inout")
    c.key(0.50, add(P.RELAXED, P.DUCK), hips=(0, 0.0, -0.06), ease="linear")
    return c


def sleep(ref) -> Clip:
    c = Clip("sleep", 4.0, loop=True, notes="Somnus: dozes on his feet; a slow breath and a nod")
    c.key(0.0, P.DOZE, hips=(0, 0.01, -0.045))
    c.key(1.6, P.DOZE_INHALE, hips=(0, 0.01, -0.040))
    c.key(2.9, P.DOZE_NOD, hips=(0, 0.012, -0.05), ease="in")
    c.key(3.3, add(P.DOZE, {"Head": (-6, 0, 0)}), hips=(0, 0.01, -0.044), ease="out")
    c.key(4.0, P.DOZE, hips=(0, 0.01, -0.045))
    return c


def levitate_struggle(ref) -> Clip:
    c = Clip("levitate_struggle", 1.2, loop=True, feet_ik=False,
             notes="Levo holds him in the air (the engine lifts the transform); legs kick, arms flail")
    c.key(0.0, P.LEVITATE_A)
    c.key(0.3, merge(P.scale(P.LEVITATE_A, 0.3), P.scale(P.LEVITATE_B, 0.3)), ease="inout")
    c.key(0.6, P.LEVITATE_B, ease="out")
    c.key(0.9, merge(P.scale(P.LEVITATE_A, 0.3), P.scale(P.LEVITATE_B, 0.3)), ease="inout")
    c.key(1.2, P.LEVITATE_A, ease="out")
    return c


# ---------------------------------------------------------------------------------
# Polearm family (weapon IK: right hand carries the haft, left hand grips it)
# ---------------------------------------------------------------------------------

def _n(v):
    m = math.sqrt(sum(x * x for x in v))
    return tuple(x / m for x in v)


# Stance: right (lead) foot forward, left back and turned out.
POLE_FEET = {"R": FootKey(y=-0.20, yaw=10), "L": FootKey(x=0.16, y=0.20, yaw=-22)}
READY = WeaponKey(grip=(-0.10, -0.38, 1.00), dir=_n((-0.26, -1.0, 0.36)), edge=(0.0, 0.0, -1.0))


def polearm_guard(ref) -> Clip:
    c = Clip("polearm_guard", 2.0, loop=True, notes="ready stance, haft levelled at the hip")
    breathe = WeaponKey(grip=(-0.10, -0.38, 1.01), dir=_n((-0.26, -1.0, 0.38)), edge=(0, 0, -1))
    c.key(0.0, P.POLE_READY, hips=(0, 0.0, -0.06), feet=POLE_FEET, weapon=READY)
    c.key(1.0, add(P.POLE_READY, P.BREATH_IN), hips=(0, 0.0, -0.055), feet=POLE_FEET,
          weapon=breathe)
    c.key(2.0, P.POLE_READY, hips=(0, 0.0, -0.06), feet=POLE_FEET, weapon=READY)
    return c


def polearm_thrust(ref) -> Clip:
    c = Clip("polearm_thrust", 0.9, notes="draw back, lunge-thrust (hit 0.45 s), recover")
    draw = WeaponKey(grip=(-0.12, -0.16, 1.02), dir=_n((-0.24, -1.0, 0.30)), edge=(0, 0, -1))
    ext = WeaponKey(grip=(-0.05, -0.80, 1.07), dir=_n((-0.12, -1.0, 0.12)), edge=(0, 0, -1))
    over = WeaponKey(grip=(-0.05, -0.85, 1.06), dir=_n((-0.12, -1.0, 0.10)), edge=(0, 0, -1))
    lunge = {"R": FootKey(y=-0.52, yaw=6), "L": FootKey(x=0.16, y=0.24, yaw=-22, pitch=14)}
    c.key(0.00, P.POLE_READY, hips=(0, 0.0, -0.06), feet=POLE_FEET, weapon=READY)
    c.key(0.30, P.POLE_DRAW, hips=(0, 0.06, -0.07), feet=POLE_FEET, weapon=draw,
          ease="inout")
    c.key(0.43, P.POLE_EXTEND, hips=(0, -0.20, -0.11), feet=lunge, weapon=ext, ease="in")
    c.key(0.50, P.POLE_EXTEND, hips=(0, -0.22, -0.12), feet=lunge, weapon=over, ease="out")
    c.key(0.62, P.POLE_EXTEND, hips=(0, -0.21, -0.115), feet=lunge, weapon=ext, ease="inout")
    c.key(0.90, P.POLE_READY, hips=(0, 0.0, -0.06), feet=POLE_FEET, weapon=READY, ease="inout")
    return c


def polearm_sweep(ref) -> Clip:
    c = Clip("polearm_sweep", 1.1, notes="wind up right, cut across at knee height (hit 0.55 s)")
    wind = WeaponKey(grip=(-0.30, -0.22, 1.02), dir=_n((-0.85, -0.45, 0.10)), edge=_n((0.3, -0.6, 0)))
    cut = WeaponKey(grip=(-0.04, -0.48, 0.86), dir=_n((0.05, -1.0, -0.42)), edge=(1, 0, 0))
    follow = WeaponKey(grip=(0.12, -0.36, 0.88), dir=_n((0.85, -0.50, -0.30)), edge=_n((0.4, 0.8, 0)))
    step = {"R": FootKey(y=-0.40, yaw=0), "L": FootKey(x=0.16, y=0.20, yaw=-22, pitch=10)}
    c.key(0.00, P.POLE_READY, hips=(0, 0.0, -0.06), feet=POLE_FEET, weapon=READY)
    c.key(0.38, P.SWEEP_WIND, hips=(-0.02, 0.04, -0.08), feet=POLE_FEET, weapon=wind,
          ease="inout")
    c.key(0.55, P.SWEEP_CUT, hips=(0.0, -0.10, -0.14), feet=step, weapon=cut, ease="in")
    c.key(0.74, P.SWEEP_FOLLOW, hips=(0.03, -0.12, -0.13), feet=step, weapon=follow,
          ease="overshoot")
    c.key(1.10, P.POLE_READY, hips=(0, 0.0, -0.06), feet=POLE_FEET, weapon=READY, ease="inout")
    return c


def polearm_hook(ref) -> Clip:
    c = Clip("polearm_hook", 1.2, notes="reach past, hook with the back fluke (hit 0.5 s), drag back")
    reach = WeaponKey(grip=(-0.05, -0.76, 1.16), dir=_n((-0.12, -1.0, 0.22)), edge=(0, 0, -1))
    hook = WeaponKey(grip=(-0.06, -0.62, 1.00), dir=_n((-0.12, -1.0, -0.12)), edge=(0, 0, -1))
    drag = WeaponKey(grip=(-0.10, -0.18, 0.98), dir=_n((-0.22, -1.0, -0.05)), edge=(0, 0, -1))
    lunge = {"R": FootKey(y=-0.46, yaw=6), "L": FootKey(x=0.16, y=0.24, yaw=-22, pitch=12)}
    back = {"R": FootKey(y=-0.20, yaw=10), "L": FootKey(x=0.16, y=0.42, yaw=-22)}
    c.key(0.00, P.POLE_READY, hips=(0, 0.0, -0.06), feet=POLE_FEET, weapon=READY)
    c.key(0.36, P.HOOK_REACH, hips=(0, -0.18, -0.10), feet=lunge, weapon=reach, ease="inout")
    c.key(0.50, P.HOOK_REACH, hips=(0, -0.18, -0.12), feet=lunge, weapon=hook, ease="in")
    c.key(0.94, P.HOOK_DRAG, hips=(0, 0.14, -0.08), feet=back, weapon=drag, ease="inout")
    c.key(1.20, P.POLE_READY, hips=(0, 0.0, -0.06), feet=POLE_FEET, weapon=READY, ease="inout")
    return c


def polearm_chop(ref) -> Clip:
    c = Clip("polearm_chop", 1.1, notes="raise overhead (0.45 s tell), chop down (hit 0.6 s)")
    raise_ = WeaponKey(grip=(-0.10, -0.14, 1.58), dir=_n((0.05, 0.25, 1.0)), edge=(0, -1, 0))
    strike = WeaponKey(grip=(-0.06, -0.55, 1.02), dir=_n((-0.10, -0.85, -0.55)), edge=(0, 0, -1))
    over = WeaponKey(grip=(-0.06, -0.55, 0.96), dir=_n((-0.10, -0.78, -0.63)), edge=(0, 0, -1))
    step = {"R": FootKey(y=-0.44, yaw=6), "L": FootKey(x=0.16, y=0.22, yaw=-22, pitch=10)}
    c.key(0.00, P.POLE_READY, hips=(0, 0.0, -0.06), feet=POLE_FEET, weapon=READY)
    c.key(0.45, P.CHOP_RAISE, hips=(0, 0.04, -0.03), feet=POLE_FEET, weapon=raise_,
          elbows={"L": (0.8, 0.3, -0.5), "R": (-0.9, 0.2, -0.3)}, ease="inout")
    c.key(0.60, P.CHOP_STRIKE, hips=(0, -0.14, -0.14), feet=step, weapon=strike, ease="in")
    c.key(0.68, P.CHOP_STRIKE, hips=(0, -0.15, -0.15), feet=step, weapon=over, ease="out")
    c.key(1.10, P.POLE_READY, hips=(0, 0.0, -0.06), feet=POLE_FEET, weapon=READY, ease="inout")
    return c


def polearm_cant(ref) -> Clip:
    c = Clip("polearm_cant", 0.4, notes="additive over the carry: the upright pole tilts 40 deg "
             "forward (tip 2.05 -> ~1.70 m); hold the last frame while under the door")
    up = WeaponKey(grip=(-0.26, -0.34, 1.08), dir=(0.0, 0.0, 1.0), edge=(0.0, -1.0, 0.0))
    a = math.radians(40.0)
    canted = WeaponKey(grip=(-0.26, -0.46, 1.00), dir=(0.0, -math.sin(a), math.cos(a)),
                       edge=(0.0, -math.cos(a), -math.sin(a)))
    c.key(0.00, P.RELAXED, weapon=up, lhand=0.0)
    c.key(0.40, P.RELAXED, weapon=canted, lhand=0.0, ease="inout")
    return c


# ---------------------------------------------------------------------------------
# The Lantern Warden: carry poses and signature clips, on the warden's own rig
# ---------------------------------------------------------------------------------

def warden_carry(rig) -> Clip:
    c = Clip("warden_carry", 1.0, loop=True, notes="glaive upright in the right fist, "
             "butt clear of the floor; lantern hanging from the left fist")
    c.key(0.0, P.WARDEN_CARRY)
    c.key(1.0, P.WARDEN_CARRY)
    return c


def warden_carry_run(rig) -> Clip:
    c = Clip("warden_carry_run", 1.0, loop=True, notes="lantern held out in front at chest "
             "height, glaive angled forward at the port")
    c.key(0.0, P.WARDEN_CARRY_RUN)
    c.key(1.0, P.WARDEN_CARRY_RUN)
    return c


def lantern_raise_search(rig) -> Clip:
    c = Clip("lantern_raise_search", 2.5, notes="lantern out at arm's length, sweeps 90 deg "
             "(left to right), looks along it, lowers")
    carry = P.WARDEN_CARRY
    left = add(merge(carry, P.LANTERN_RAISE), {"Spine": (0, 0, 16), "Chest": (0, 0, 20),
                                                "Neck": (0, 0, 6), "Head": (0, 0, 6)})
    right = add(merge(carry, P.LANTERN_RAISE), {"Spine": (0, 0, -18), "Chest": (0, 0, -24),
                                                 "Neck": (0, 0, -6), "Head": (0, 0, -8)})
    c.key(0.00, carry)
    c.key(0.12, add(carry, {"UpperArm.L": (6, 0, 0), "Spine": (2, 0, 0)}), ease="out")   # dip
    c.key(0.55, left, hips=(0.01, 0, -0.01), feet={"L": FootKey(yaw=6)}, ease="overshoot")
    c.key(0.75, left, hips=(0.01, 0, -0.01), feet={"L": FootKey(yaw=6)})
    c.key(1.55, right, hips=(-0.01, 0, -0.012), feet={"R": FootKey(yaw=-6)}, ease="inout")
    c.key(1.95, add(right, {"Head": (4, 0, -4)}), hips=(-0.01, 0, -0.012),
          feet={"R": FootKey(yaw=-6)})
    c.key(2.50, carry, ease="inout")
    return c


def death_drop_lantern(rig) -> Clip:
    c = Clip("death_drop_lantern", 1.6, notes="jolts, lets the lantern go (PropDetach 0.26 s), "
             "buckles to the knees and pitches forward; the ragdoll may take over from 0.8 s")
    carry = P.WARDEN_CARRY
    jolt = add(carry, {"Spine": (-8, 0, -4), "Chest": (-12, 0, -6), "Neck": (10, 0, 0),
                       "Head": (14, 0, 6), "UpperArm.L": (-10, -16, 0), "LowerArm.L": (-24, 0, 0)})
    buckle = add(carry, {"Hips": (10, 0, 4), "Spine": (14, 0, 0), "Chest": (12, 0, 0),
                         "Neck": (14, 0, 0), "Head": (10, 0, 0),
                         "UpperArm.L": (-8, -10, 0), "UpperArm.R": (4, 6, 0),
                         "LowerArm.R": (8, 0, 0)})
    kneel = add(carry, {"Hips": (8, 0, 6), "Spine": (18, 0, 0), "Chest": (14, 0, 0),
                        "Neck": (18, 0, 0), "Head": (10, 0, 0),
                        "UpperArm.L": (-20, -12, 0), "UpperArm.R": (-6, 10, 0),
                        "LowerArm.R": (20, 0, 0)})
    face = {"Hips": (84, 0, 8), "Spine": (4, 0, 0), "Chest": (2, 0, -4), "Neck": (-10, 0, 0),
            "Head": (-12, 0, 30),
            "UpperArm.L": (-150, -30, 0), "LowerArm.L": (-20, 0, 0),
            "UpperArm.R": (-120, 30, 0), "LowerArm.R": (-40, 0, 0)}
    kneel_feet = {"L": FootKey(y=0.16, pitch=60), "R": FootKey(y=0.30, pitch=62)}
    c.key(0.00, carry)
    c.key(0.18, jolt, hips=(0, 0.06, -0.02), ease="out")
    c.key(0.48, buckle, hips=(0, 0.04, -0.26), feet={"R": FootKey(y=0.14, pitch=20)}, ease="inout")
    c.key(0.80, kneel, hips=(0, 0.10, -0.46), feet=kneel_feet, ease="in")
    c.key(1.22, face, hips=(0, -0.30, -0.80),
          feet={"L": FootKey(y=0.60, pitch=80), "R": FootKey(y=0.62, pitch=80)}, ease="in")
    c.key(1.34, add(face, {"Head": (6, 0, 0)}), hips=(0, -0.31, -0.79),
          feet={"L": FootKey(y=0.60, pitch=80), "R": FootKey(y=0.62, pitch=80)}, ease="out")
    c.key(1.60, face, hips=(0, -0.31, -0.80),
          feet={"L": FootKey(y=0.60, pitch=80), "R": FootKey(y=0.62, pitch=80)}, ease="inout")
    return c


# ---------------------------------------------------------------------------------

def clips(ref, warden) -> dict[str, ClipDef]:
    both = BOTH_ARMS | WARDEN_PROPS
    right = RIGHT_ARM | {"Glaive"}
    out = {
        "idle": ClipDef(idle(ref), carry_mask=both),
        "walk_slow": ClipDef(GaitClip("walk_slow", WALK_SLOW, ref,
                                      notes="the JSON patrol pace 1.1 m/s"), carry_mask=both),
        "walk": ClipDef(GaitClip("walk", WALK, ref, notes="engine patrol speed 2.0 m/s"),
                        carry_mask=both),
        "run": ClipDef(GaitClip("run", RUN, ref, notes="engine chase speed 4.2 m/s; flight phase, heel kick"),
                      carry_mask=both, carry="warden_carry_run"),
        "alert_turn": ClipDef(alert_turn(ref), carry_mask=both, agent_yaw=alert_turn_yaw),
        "shout": ClipDef(shout(ref), carry_mask=right),
        "hit_react": ClipDef(hit_react(ref), carry_mask=both),
        "stagger": ClipDef(stagger(ref)),
        "knock_down": ClipDef(knock_down(ref)),
        "get_up": ClipDef(get_up(ref)),
        "archway_duck": ClipDef(archway_duck(ref), carry_mask=both),
        "sleep": ClipDef(sleep(ref), carry_mask=both),
        "levitate_struggle": ClipDef(levitate_struggle(ref), review_lift=0.45),
        "polearm_guard": ClipDef(polearm_guard(ref), weapon=True),
        "polearm_thrust": ClipDef(polearm_thrust(ref), weapon=True),
        "polearm_sweep": ClipDef(polearm_sweep(ref), weapon=True),
        "polearm_hook": ClipDef(polearm_hook(ref), weapon=True),
        "polearm_chop": ClipDef(polearm_chop(ref), weapon=True),
        "polearm_cant": ClipDef(polearm_cant(ref), weapon=True, carry_mask={"Hand.L", "LowerArm.L",
                                "UpperArm.L", "Shoulder.L", "LanternRing", "LanternBody"}),
        "warden_carry": ClipDef(warden_carry(warden), rig="warden"),
        "warden_carry_run": ClipDef(warden_carry_run(warden), rig="warden"),
        "lantern_raise_search": ClipDef(lantern_raise_search(warden), rig="warden"),
        "death_drop_lantern": ClipDef(death_drop_lantern(warden), rig="warden"),
    }
    for cid, d in out.items():
        d.clip.name = cid
    return out
