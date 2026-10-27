"""The player wizard's clips (issue #361), authored on the reference human like the enemy
clips, so they are Humanoid-retargetable. `clips(ref)` returns {clip id: ClipDef}; lengths,
loop flags and events are in anim_spec.json (layer "player", status "player").

In place, no root motion: the Rigidbody moves the player (walk input 5.0 m/s, creep 2.0).
No clip keys a Hat bone: the reference skeleton has none.
"""

from __future__ import annotations

from dataclasses import replace

from . import poses as P, poses_player as W
from mathutils import Vector

from . import mathx
from .clip import Clip, FootKey
from .gait import GaitClip
from .library import RUN, WALK, ClipDef, GaitParams, _loop_close, idle
from .poses import add

# 5.0 m/s: RUN's flight-phase jog, quicker cadence (stride 3.0 m)
JOG = replace(RUN, speed=5.0, cycle=0.6, duty=0.30, ahead=0.14, kick=0.18, lean=12.0,
              arm_swing=30.0, elbow=70.0)
# creeping: short quick steps, hips 0.30 m low, pelvis and back tipped forward, arms tucked
SNEAK = GaitParams(speed=2.0, cycle=0.6, duty=0.64, ahead=0.10, toe_pitch=22.0, heel_pitch=8.0,
                   clearance=0.04, bob=0.008, lean=14.0, pelvis_yaw=5.0, pelvis_roll=3.0,
                   sway=0.02, arm_swing=5.0, elbow=60.0, elbow_swing=5.0, head_bob=1.0,
                   crouch=0.30, hip_pitch=18.0, width=1.0)


def player_idle(ref) -> Clip:
    c = idle(ref)                       # library.idle's motion, hunched over
    c.notes = "library.idle's weight shifts and glances, shoulders rounded: shifty, impatient"
    for k in c.keys:
        k.pose = add(k.pose, W.HUNCH)
    return c


def crouch_idle(ref) -> Clip:
    c = Clip("crouch_idle", 2.0, loop=True, notes="knees well bent, hips 0.40 m low, back rounded, "
             "arms tucked; breathes and glances left and right")
    hips = (0.0, 0.11, -0.40)
    c.key(0.0, W.CROUCH, hips=hips)
    c.key(0.5, W.CROUCH_LOOK_L, hips=(0.0, 0.11, -0.39))
    c.key(1.0, W.CROUCH_BREATH, hips=(0.0, 0.11, -0.41))
    c.key(1.5, W.CROUCH_LOOK_R, hips=(0.0, 0.11, -0.39))
    _loop_close(c, W.CROUCH, hips=hips)
    return c


def jump_takeoff(ref) -> Clip:
    c = Clip("jump_takeoff", 8 / 30, notes="coil, spring up on the toes with the arms swinging "
             "up; the last frame has both feet off the floor (impulse)")
    r = P.RELAXED
    c.key(0.0, r)
    c.key(0.10, W.JUMP_COIL, hips=(0.0, 0.10, -0.30), ease="inout")
    c.key(0.20, W.JUMP_SPRING, hips=(0.0, -0.02, 0.0),
          feet={"L": FootKey(pitch=40, lift=0.03), "R": FootKey(pitch=40, lift=0.03)}, ease="in")
    c.key(8 / 30, W.JUMP_SPRING, hips=(0.0, -0.04, 0.03),
          feet={"L": FootKey(pitch=52, lift=0.12), "R": FootKey(pitch=52, lift=0.09)}, ease="out")
    return c


def jump_air(ref) -> Clip:
    c = Clip("jump_air", 0.6, loop=True, feet_ik=False,
             notes="airborne: thighs forward, knees folded, arms out and up, a slight flail")
    c.key(0.0, W.JUMP_AIR_A)
    c.key(0.3, W.JUMP_AIR_B, ease="inout")
    _loop_close(c, W.JUMP_AIR_A, ease="inout")
    return c


def jump_land(ref) -> Clip:
    c = Clip("jump_land", 10 / 30, notes="feet reach the floor, knees and hips absorb, "
             "rise back to the idle stance")
    c.key(0.0, W.JUMP_LAND_IN, hips=(0.0, 0.04, -0.03),
          feet={"L": FootKey(lift=0.10, pitch=20), "R": FootKey(lift=0.05, pitch=20)})
    c.key(0.07, W.JUMP_LAND_ABSORB, hips=(0.0, 0.10, -0.34), ease="out")
    c.key(0.20, add(P.RELAXED, W.JUMP_LAND_IN), hips=(0.0, 0.05, -0.17), ease="inout")
    c.key(10 / 30, add(P.RELAXED, P.WEIGHT_LEFT), hips=(0.022, 0.0, -0.010), ease="inout")
    return c


STANCE = {"R": FootKey(y=-0.12, yaw=8), "L": FootKey(x=0.04, y=0.10, yaw=-14)}


def cast_hold(ref) -> Clip:
    c = Clip("cast_hold", 1.2, loop=True, notes="upper body: right arm forward at shoulder height, "
             "palm out, trembling and pulsing; left arm drawn back; chest and head lean in")
    hips = (0.0, -0.02, -0.03)
    c.key(0.0, W.CAST_HOLD, hips=hips, feet=STANCE)
    c.key(0.2, W.CAST_SHIVER, hips=hips, feet=STANCE, ease="inout")
    c.key(0.5, W.CAST_PULSE, hips=(0.0, -0.03, -0.035), feet=STANCE, ease="inout")
    c.key(0.7, W.CAST_SHIVER, hips=hips, feet=STANCE, ease="inout")
    c.key(0.9, W.CAST_HOLD, hips=hips, feet=STANCE, ease="inout")
    c.key(1.05, add(W.CAST_HOLD, {"UpperArm.R": (0.0, -1.0, 0.0), "Hand.R": (0, 0, -2)}),
          hips=hips, feet=STANCE, ease="inout")
    _loop_close(c, W.CAST_HOLD, hips=hips, feet=STANCE)
    return c


def cast_release(ref) -> Clip:
    c = Clip("cast_release", 0.5, notes="from cast_hold: snap back, thrust the right hand forward "
             "(SpellRelease 0.2 s), relax toward the idle arms")
    hips = (0.0, -0.02, -0.03)
    relax = add(P.RELAXED, W.HUNCH)
    c.key(0.00, W.CAST_HOLD, hips=hips, feet=STANCE)
    c.key(0.08, W.CAST_DRAW, hips=(0.0, 0.0, -0.04), feet=STANCE, ease="out")
    c.key(0.20, W.CAST_THRUST, hips=(0.0, -0.08, -0.06), feet=STANCE, ease="in")
    c.key(0.28, W.CAST_THRUST, hips=(0.0, -0.09, -0.06), feet=STANCE, ease="out")
    c.key(0.50, relax, hips=(0.0, 0.0, -0.01), feet={"L": FootKey(), "R": FootKey()}, ease="inout")
    return c


def _knees_up(clip, t, frame):
    """Once the hips pitch past ~40 deg forward the knees must fold up (+Z), not 'forward'
    (which is down into the floor); blended in as the body goes prone."""
    fwd = frame.pose["Hips"] @ Vector((0.0, -1.0, 0.0)) if "Hips" in frame.pose else Vector((0, -1, 0))
    w = min(1.0, max(0.0, (-fwd.z - 0.2) / 0.3))
    if w > 0.0:
        frame.leg_hint = fwd.lerp(Vector((0.0, 0.3, 1.0)), mathx.ease("smooth", w)).normalized()


def death_collapse(ref) -> Clip:
    c = Clip("death_collapse", 1.5, notes="stagger, knees buckle, pitches forward onto the face "
             "and lies still from 1.3 s; the game then hides the body and drops the hat")
    r = P.RELAXED
    jolt = add(r, P.RECOIL)
    buckle = add(r, {"Hips": (10, 0, 4), "Spine": (14, 0, 0), "Chest": (12, 0, 0), "Neck": (14, 0, 0),
                     "Head": (10, 0, 0), "UpperArm.L": (-8, -10, 0), "UpperArm.R": (4, 6, 0),
                     "LowerArm.L": (-20, 0, 0), "LowerArm.R": (-20, 0, 0)})
    kneel = add(r, {"Hips": (8, 0, 6), "Spine": (18, 0, 0), "Chest": (14, 0, 0), "Neck": (18, 0, 0),
                    "Head": (10, 0, 0), "UpperArm.L": (-20, -12, 0), "UpperArm.R": (-6, 10, 0),
                    "LowerArm.L": (-20, 0, 0), "LowerArm.R": (-20, 0, 0)})
    prone = {"Hips": (84, 0, 8), "Spine": (4, 0, 0), "Chest": (2, 0, -4), "Neck": (-10, 0, 0),
             "Head": (-12, 0, 30),
             "UpperArm.L": (0, -85, 0), "LowerArm.L": (0, 0, 0),
             "UpperArm.R": (-165, 25, 0), "LowerArm.R": (-20, 0, 0)}
    toes = {"L": FootKey(y=0.60, pitch=80), "R": FootKey(y=0.62, pitch=80)}
    c.key(0.00, r)
    c.key(0.12, jolt, hips=(0, 0.08, -0.03), ease="out")
    c.key(0.45, buckle, hips=(0, 0.05, -0.26), feet={"R": FootKey(y=0.14, pitch=20)}, ease="inout")
    c.key(0.75, kneel, hips=(0, 0.10, -0.46),
          feet={"L": FootKey(y=0.16, pitch=60), "R": FootKey(y=0.30, pitch=62)}, ease="in")
    c.key(1.12, prone, hips=(0, -0.30, -0.75), feet=toes, ease="in")
    c.key(1.22, add(prone, {"Head": (5, 0, 0)}), hips=(0, -0.31, -0.74), feet=toes, ease="out")
    c.key(1.30, prone, hips=(0, -0.31, -0.75), feet=toes, ease="inout")
    c.key(1.50, prone, hips=(0, -0.31, -0.75), feet=toes)
    return c.add_layer(_knees_up)


def clips(ref) -> dict[str, ClipDef]:
    out = {
        "player_idle": ClipDef(player_idle(ref)),
        "player_walk": ClipDef(GaitClip("player_walk", WALK, ref, notes="2.0 m/s: slow-movement blend")),
        "player_jog": ClipDef(GaitClip("player_jog", JOG, ref,
                                       notes="5.0 m/s: the player's walk input, a brisk jog")),
        "crouch_idle": ClipDef(crouch_idle(ref)),
        "crouch_walk": ClipDef(GaitClip("crouch_walk", SNEAK, ref, notes="2.0 m/s creep, hips 0.30 m low")),
        "jump_takeoff": ClipDef(jump_takeoff(ref)),
        "jump_air": ClipDef(jump_air(ref)),
        "jump_land": ClipDef(jump_land(ref)),
        "cast_hold": ClipDef(cast_hold(ref)),
        "cast_release": ClipDef(cast_release(ref)),
        "death_collapse": ClipDef(death_collapse(ref)),
    }
    for cid, d in out.items():
        d.clip.name = cid
    return out
