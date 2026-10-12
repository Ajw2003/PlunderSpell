"""The Wick's clips (#386, the fear pass): the Lantern Warden's true form, and the warden's half
of the turn. Authored on the reference human like every other clip, so they play on any
Humanoid; `clips(ref, scale)` returns {clip id: ClipDef}. Lengths, loop flags and events are in
anim_spec.json (layer and status "fear").

The turn is 1.6 s in four beats (docs/7-design/fear-pass.md, "The true form"): `warden_turn`
plays on the warden (seen; the face splits and the lantern drops at 0.4 s; he doubles over),
the engine swaps the model at 0.9 s, and `wick_emerge` plays on the Wick (bursts up and settles
into the hunch). The Wick is 3.0 m to the cage top on a 1.76 m reference: `scale` is the Wick's
hip height over the reference's, and gaits are authored at agent speed / scale, so that on the
Wick the feet plant at the agent's speed (1.4 m/s stalking, 4.2 m/s hunting).

In place, no root motion. Fingers are rigid on Hand.L (no finger bones): a "grip" is the hand
pitching. The Cage, Hat and Blade bones are never keyed; they ride on Head and Hand.R.
"""

from __future__ import annotations

from dataclasses import replace

from mathutils import Vector

from . import poses as P
from .clip import Clip, FootKey
from .gait import GaitClip
from .library import RUN, ClipDef, GaitParams, _loop_close
from .mathx import euler_q
from .poses import add, mirror, scale as pscale

# ---- poses (reference rig, degrees; see poses.py for the axes) ----------------------------

# The Wick's stance: hunched from the waist, head thrust forward and up, arms hanging forward
# and out (clear of the coat), elbows bent.
HUNCH = {"Hips": (4.0, 0.0, 0.0), "Spine": (12.0, 0.0, 0.0), "Chest": (10.0, 0.0, 0.0),
         "Neck": (16.0, 0.0, 0.0), "Head": (-14.0, 0.0, 0.0),
         "Shoulder.L": (6.0, 0.0, 0.0), "Shoulder.R": (6.0, 0.0, 0.0),
         "UpperArm.L": (-12.0, -16.0, 0.0), "UpperArm.R": (-12.0, 16.0, 0.0),
         "LowerArm.L": (-26.0, 0.0, 0.0), "LowerArm.R": (-32.0, 0.0, 0.0),
         "Hand.L": (-8.0, 0.0, 0.0)}
HUNCH_HIPS = (0.0, 0.03, -0.06)
BREATHE = {"Chest": (-3.0, 0.0, 0.0), "Neck": (-2.0, 0.0, 0.0), "Shoulder.L": (-2.0, 0.0, 0.0),
           "Shoulder.R": (-2.0, 0.0, 0.0)}
SWAY_L = {"Hips": (0.0, -2.0, 2.0), "Spine": (0.0, 2.0, -2.0), "Head": (0.0, -6.0, 8.0)}
SWAY_R = mirror(SWAY_L)
CLAW = {"Hand.L": (-26.0, 0.0, 0.0), "LowerArm.L": (-6.0, 0.0, 0.0)}     # fingers curl in
LOOK_L = {"Spine": (0.0, 0.0, 10.0), "Chest": (0.0, 0.0, 16.0), "Neck": (0.0, 0.0, 22.0),
          "Head": (0.0, 0.0, 26.0)}                                    # 74 deg in all
LOOK_R = mirror(LOOK_L)
STOOP = {"Spine": (14.0, 0.0, 0.0), "Chest": (12.0, 0.0, 0.0), "Neck": (8.0, 0.0, 0.0),
         "Head": (-10.0, 0.0, 0.0)}
RECOIL = {"Spine": (-10.0, 0.0, -4.0), "Chest": (-12.0, 2.0, -6.0), "Neck": (-8.0, 0.0, 0.0),
          "Head": (-16.0, 0.0, 6.0), "UpperArm.L": (6.0, -14.0, 0.0), "UpperArm.R": (6.0, 14.0, 0.0)}

# catch: draw the left arm back, then drive it forward at chest height and close the hand
CATCH_DRAW = {"Spine": (4.0, 0.0, -10.0), "Chest": (2.0, 0.0, -14.0),
              "UpperArm.L": (24.0, -24.0, 0.0), "LowerArm.L": (-60.0, 0.0, 0.0), "Hand.L": (10.0, 0.0, 0.0),
              "Head": (-18.0, 0.0, 4.0)}
CATCH_LUNGE = {"Hips": (10.0, 0.0, 0.0), "Spine": (18.0, 0.0, 8.0), "Chest": (10.0, 0.0, 12.0),
               "Neck": (6.0, 0.0, 0.0), "Head": (-24.0, 0.0, -6.0),
               "UpperArm.L": (-78.0, -8.0, 0.0), "LowerArm.L": (-8.0, 0.0, 0.0), "Hand.L": (6.0, 0.0, 0.0),
               "UpperArm.R": (14.0, 24.0, 0.0), "LowerArm.R": (-30.0, 0.0, 0.0)}
CATCH_CLOSE = add(CATCH_LUNGE, {"LowerArm.L": (-34.0, 0.0, 0.0), "Hand.L": (-44.0, 0.0, 0.0),
                                "UpperArm.L": (10.0, 0.0, 0.0), "Chest": (-4.0, 0.0, 0.0)})

# the turn: the warden goes rigid, clutches his face, doubles over (the swap point)
SEIZE = {"Spine": (-6.0, 0.0, 0.0), "Chest": (-8.0, 0.0, 0.0), "Neck": (-6.0, 0.0, 0.0),
         "Head": (-14.0, 0.0, 0.0), "UpperArm.L": (0.0, -10.0, 0.0), "UpperArm.R": (0.0, 10.0, 0.0),
         "LowerArm.L": (-10.0, 0.0, 0.0), "LowerArm.R": (-10.0, 0.0, 0.0)}
CLUTCH_FACE = {"Spine": (-4.0, 0.0, 0.0), "Chest": (-6.0, 0.0, 0.0), "Neck": (-10.0, 0.0, 0.0),
               "Head": (-22.0, 0.0, 0.0),
               "UpperArm.L": (-62.0, 18.0, 0.0), "UpperArm.R": (-62.0, -18.0, 0.0),
               "LowerArm.L": (-112.0, 0.0, 0.0), "LowerArm.R": (-112.0, 0.0, 0.0),
               "Hand.L": (-10.0, 0.0, -20.0), "Hand.R": (-10.0, 0.0, 20.0)}
DOUBLED = {"Hips": (12.0, 0.0, 0.0), "Spine": (30.0, 0.0, 0.0), "Chest": (24.0, 0.0, 0.0),
           "Neck": (16.0, 0.0, 0.0), "Head": (8.0, 0.0, 0.0),
           "UpperArm.L": (-40.0, 14.0, 0.0), "UpperArm.R": (-40.0, -14.0, 0.0),
           "LowerArm.L": (-118.0, 0.0, 0.0), "LowerArm.R": (-118.0, 0.0, 0.0)}
DOUBLED_HIPS = (0.0, 0.08, -0.16)
BURST = {"Hips": (-4.0, 0.0, 0.0), "Spine": (-14.0, 0.0, 0.0), "Chest": (-18.0, 0.0, 0.0),
         "Neck": (-10.0, 0.0, 0.0), "Head": (-30.0, 0.0, 0.0),
         "Shoulder.L": (-6.0, -8.0, 0.0), "Shoulder.R": (-6.0, 8.0, 0.0),
         "UpperArm.L": (-34.0, -56.0, 0.0), "UpperArm.R": (-34.0, 56.0, 0.0),
         "LowerArm.L": (-70.0, 0.0, 0.0), "LowerArm.R": (-70.0, 0.0, 0.0), "Hand.L": (24.0, 0.0, 0.0)}


def _hunched_gait(name: str, params: GaitParams, ref, extra: dict, notes: str) -> GaitClip:
    """A gait with the Wick's head and arms laid over it (a frame layer, after the gait)."""
    c = GaitClip(name, params, ref, notes=notes)
    over = {b: euler_q(r) for b, r in extra.items()}

    def layer(clip, t, frame):
        for bone, q in over.items():
            frame.pose[bone] = q @ frame.pose.get(bone, euler_q((0.0, 0.0, 0.0)))
    c.layers.append(layer)
    return c


def wick_idle(ref) -> Clip:
    c = Clip("wick_idle", 3.0, loop=True, notes="hunched; breathes, sways, the cage head tilts, "
             "the left hand curls and opens")
    h = HUNCH_HIPS
    c.key(0.00, add(HUNCH, SWAY_L), hips=(0.02, h[1], h[2]))
    c.key(0.75, add(HUNCH, SWAY_L, BREATHE, CLAW), hips=(0.018, h[1], h[2] + 0.004))
    c.key(1.50, add(HUNCH, SWAY_R), hips=(-0.02, h[1], h[2]))
    c.key(2.25, add(HUNCH, SWAY_R, BREATHE, pscale(CLAW, 0.5)), hips=(-0.018, h[1], h[2] + 0.004))
    _loop_close(c, add(HUNCH, SWAY_L), hips=(0.02, h[1], h[2]))
    return c


def wick_search(ref) -> Clip:
    c = Clip("wick_search", 2.5, loop=True, notes="the cage head sweeps 74 deg left, then right, the "
             "chest following; its light sweeps with it")
    h = HUNCH_HIPS
    c.key(0.0, HUNCH, hips=h)
    c.key(0.7, add(HUNCH, LOOK_L), hips=h, ease="inout")
    c.key(1.0, add(HUNCH, LOOK_L, BREATHE), hips=h)
    c.key(1.8, add(HUNCH, LOOK_R), hips=h, ease="inout")
    c.key(2.1, add(HUNCH, LOOK_R, BREATHE), hips=h)
    _loop_close(c, HUNCH, hips=h, ease="inout")
    return c


def wick_stoop(ref) -> Clip:
    c = Clip("wick_stoop", 0.5, notes="additive over any state: bends from the waist and knees "
             "to take the crown under a 2.59 m arch; held by the layer weight")
    h = HUNCH_HIPS
    c.key(0.0, HUNCH, hips=h)
    c.key(0.5, add(HUNCH, STOOP), hips=(0.0, h[1] + 0.06, h[2] - 0.15), ease="inout")
    return c


def wick_flinch(ref) -> Clip:
    c = Clip("wick_flinch", 0.4, notes="additive: the blow jerks it back, it does not stop")
    h = HUNCH_HIPS
    c.key(0.00, HUNCH, hips=h)
    c.key(0.08, add(HUNCH, RECOIL), hips=(0.0, h[1] + 0.04, h[2] - 0.01), ease="out")
    c.key(0.40, HUNCH, hips=h, ease="out")
    return c


def wick_catch(ref) -> Clip:
    c = Clip("wick_catch", 1.2, notes="draws the left arm back, lunges, the hand closes on the player "
             "at chest height (AttackHit 0.5 s), pulls in, settles")
    h = HUNCH_HIPS
    lunge_feet = {"L": FootKey(y=-0.34, pitch=0.0), "R": FootKey(pitch=18.0)}
    c.key(0.00, HUNCH, hips=h)
    c.key(0.28, add(HUNCH, CATCH_DRAW), hips=(0.0, h[1] + 0.06, h[2] - 0.02), ease="out")
    c.key(0.46, add(HUNCH, CATCH_LUNGE), hips=(0.0, h[1] - 0.20, h[2] - 0.06), feet=lunge_feet, ease="in")
    c.key(0.56, add(HUNCH, CATCH_CLOSE), hips=(0.0, h[1] - 0.22, h[2] - 0.07), feet=lunge_feet,
          ease="overshoot")
    c.key(0.85, add(HUNCH, CATCH_CLOSE, {"UpperArm.L": (16.0, 0.0, 0.0), "Spine": (-6.0, 0.0, 0.0)}),
          hips=(0.0, h[1] - 0.12, h[2] - 0.04), feet=lunge_feet, ease="inout")
    c.key(1.20, add(HUNCH, pscale(CLAW, 0.6)), hips=h, feet={"L": FootKey(), "R": FootKey()}, ease="inout")
    return c


def warden_turn(ref) -> Clip:
    c = Clip("warden_turn", 0.9, notes="on the warden: seen, goes rigid; hands fly to the face as it "
             "splits and the lantern drops (PropDetach 0.4 s); doubles over; the model swaps at 0.9 s")
    r = P.RELAXED
    c.key(0.00, r)
    c.key(0.07, add(r, SEIZE), hips=(0.0, 0.0, -0.005), ease="out")
    c.key(0.30, add(r, CLUTCH_FACE), hips=(0.0, 0.02, -0.03), ease="in")
    c.key(0.42, add(r, CLUTCH_FACE, {"Head": (-10.0, 0.0, 8.0), "Chest": (-4.0, 0.0, 0.0)}),
          hips=(0.0, 0.03, -0.03), ease="overshoot")
    c.key(0.70, add(r, DOUBLED), hips=DOUBLED_HIPS, ease="inout")
    c.key(0.90, add(r, DOUBLED, {"Spine": (4.0, 0.0, 0.0), "Neck": (6.0, 0.0, 0.0)}),
          hips=(DOUBLED_HIPS[0], DOUBLED_HIPS[1], DOUBLED_HIPS[2] - 0.02), ease="inout")
    return c


def wick_emerge(ref) -> Clip:
    c = Clip("wick_emerge", 0.7, notes="on the Wick from the swap: from the warden's doubled pose it "
             "bursts up, back arched, arms flung wide, cage head thrown back; settles into the hunch")
    c.key(0.00, add(P.RELAXED, DOUBLED, {"Spine": (4.0, 0.0, 0.0), "Neck": (6.0, 0.0, 0.0)}),
          hips=(DOUBLED_HIPS[0], DOUBLED_HIPS[1], DOUBLED_HIPS[2] - 0.02))
    c.key(0.24, BURST, hips=(0.0, 0.0, -0.01), ease="in")
    c.key(0.34, add(BURST, {"Spine": (-3.0, 0.0, 0.0), "Head": (-6.0, 0.0, 0.0)}), hips=(0.0, 0.0, -0.005),
          ease="out")
    c.key(0.70, HUNCH, hips=HUNCH_HIPS, ease="inout")
    return c


# stalking: long slow strides; hunting: low and fast, arms swinging forward
STALK = GaitParams(speed=1.4, cycle=1.4, duty=0.62, ahead=0.04, toe_pitch=36.0, heel_pitch=14.0,
                   pelvis_yaw=7.0, pelvis_roll=4.0, bob=0.016, clearance=0.05, lean=20.0, sway=0.02,
                   arm_swing=10.0, arm_spread=14.0, elbow=28.0, elbow_swing=8.0, head_bob=1.0,
                   crouch=0.04, hip_pitch=6.0)
HUNT = replace(RUN, speed=4.2, cycle=0.70, lean=26.0, arm_swing=40.0, arm_spread=16.0, elbow=56.0,
               crouch=0.05, hip_pitch=8.0, head_bob=1.0)
HEAD_UP = {"Neck": (10.0, 0.0, 0.0), "Head": (-20.0, 0.0, 0.0)}


def clips(ref, scale: float) -> dict[str, ClipDef]:
    """scale: the Wick's hip height over the reference's. Gaits authored at speed / scale."""
    stalk = _hunched_gait("wick_stalk", replace(STALK, speed=STALK.speed / scale), ref, HEAD_UP,
                          notes=f"1.4 m/s on the Wick (authored at {STALK.speed / scale:.3f} m/s on the "
                                f"reference, x{scale:.3f} hip scale); long slow strides, hunched")
    hunt = _hunched_gait("wick_hunt", replace(HUNT, speed=HUNT.speed / scale), ref, HEAD_UP,
                         notes=f"4.2 m/s on the Wick (authored at {HUNT.speed / scale:.3f} m/s on the "
                               f"reference); low and fast, arms swinging forward")
    stalk.wick_speed, hunt.wick_speed = STALK.speed, HUNT.speed
    out = {}
    for c in (warden_turn(ref), wick_emerge(ref), wick_idle(ref), stalk, hunt, wick_search(ref),
              wick_catch(ref), wick_flinch(ref), wick_stoop(ref)):
        out[c.name] = ClipDef(c)
    return out
