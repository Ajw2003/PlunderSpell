"""The pose library: named poses on the shared figures.Human rig.

A pose is {bone: (rx, ry, rz)} degrees, each bone's rotation RELATIVE TO ITS PARENT
expressed in the rest pose's world axes (mathx docstring):

    X  + tips an upright bone (Spine, Neck) forward; + swings a hanging bone (an
         arm, a thigh) BACK. So a thigh lifting forward is negative X.
    Y  rolls sideways. For a hanging left arm, - takes it out (abducts); right arm +.
    Z  turns about the vertical; + turns the figure's front toward its own left.

Bones a pose does not name are at rest. Arms and legs are usually IK-driven (feet
targets, the weapon), so most poses only set the spine, head and the free arm.
Poses compose with `merge` (later wins), `add` (rotations summed per axis, for
small layered tweaks) and `mirror` (.L <-> .R, Y and Z negated).
"""

from __future__ import annotations


def merge(*poses: dict) -> dict:
    out: dict = {}
    for p in poses:
        out.update(p)
    return out


def add(*poses: dict) -> dict:
    out: dict = {}
    for p in poses:
        for bone, r in p.items():
            a = out.get(bone, (0.0, 0.0, 0.0))
            out[bone] = (a[0] + r[0], a[1] + r[1], a[2] + r[2])
    return out


def scale(pose: dict, f: float) -> dict:
    return {b: (r[0] * f, r[1] * f, r[2] * f) for b, r in pose.items()}


def mirror(pose: dict) -> dict:
    out = {}
    for bone, (rx, ry, rz) in pose.items():
        if bone.endswith(".L"):
            bone = bone[:-2] + ".R"
        elif bone.endswith(".R"):
            bone = bone[:-2] + ".L"
        out[bone] = (rx, -ry, -rz)
    return out


# ---- standing -------------------------------------------------------------------

STAND = {}
RELAXED = {
    "Spine": (1.5, 0.0, 0.0), "Chest": (1.0, 0.0, 0.0), "Neck": (-1.0, 0.0, 0.0),
    "UpperArm.L": (2.0, -2.0, 0.0), "UpperArm.R": (2.0, 2.0, 0.0),
    "LowerArm.L": (-8.0, 0.0, 0.0), "LowerArm.R": (-8.0, 0.0, 0.0),
}
WEIGHT_LEFT = {"Hips": (0.0, -2.5, 2.0), "Spine": (1.5, 1.5, -1.0), "Chest": (1.0, 1.5, -1.0)}
WEIGHT_RIGHT = mirror(WEIGHT_LEFT)
BREATH_IN = {"Chest": (-2.0, 0.0, 0.0), "Neck": (1.0, 0.0, 0.0),
             "Shoulder.L": (0.0, 0.0, 0.0)}
LOOK_LEFT = {"Neck": (0.0, 0.0, 10.0), "Head": (-2.0, 0.0, 16.0)}
LOOK_RIGHT = mirror(LOOK_LEFT)

# ---- alert ----------------------------------------------------------------------

TENSE = {"Spine": (4.0, 0.0, 0.0), "Chest": (2.0, 0.0, 0.0), "Neck": (2.0, 0.0, 0.0),
         "UpperArm.L": (-6.0, -6.0, 0.0), "UpperArm.R": (-6.0, 6.0, 0.0),
         "LowerArm.L": (-20.0, 0.0, 0.0), "LowerArm.R": (-20.0, 0.0, 0.0)}
HEAD_SNAP_LEFT = {"Neck": (0.0, 0.0, 22.0), "Head": (-6.0, 0.0, 36.0), "Chest": (0.0, 0.0, 6.0)}
TURN_CHEST_LEFT = {"Spine": (2.0, 0.0, 10.0), "Chest": (0.0, 0.0, 14.0),
                   "Neck": (0.0, 0.0, 12.0), "Head": (-3.0, 0.0, 14.0)}

SHOUT_INHALE = {"Spine": (-4.0, 0.0, 0.0), "Chest": (-8.0, 0.0, 0.0), "Neck": (-4.0, 0.0, 0.0),
                "Head": (-10.0, 0.0, 0.0),
                "UpperArm.L": (-62.0, -14.0, 20.0), "LowerArm.L": (-112.0, 0.0, 0.0),
                "Hand.L": (0.0, 0.0, 0.0)}
SHOUT_BELLOW = {"Spine": (6.0, 0.0, 0.0), "Chest": (8.0, 0.0, 0.0), "Neck": (10.0, 0.0, 0.0),
                "Head": (-16.0, 0.0, 0.0),
                "UpperArm.L": (-66.0, -18.0, 24.0), "LowerArm.L": (-118.0, 0.0, 0.0),
                "UpperArm.R": (-6.0, 10.0, 0.0), "LowerArm.R": (-30.0, 0.0, 0.0)}

# ---- hits and falls -----------------------------------------------------------------

FLINCH = {"Spine": (-5.0, 0.0, -3.0), "Chest": (-8.0, 2.0, -4.0), "Neck": (8.0, 0.0, 0.0),
          "Head": (10.0, 0.0, 6.0),
          "UpperArm.L": (-18.0, -10.0, 0.0), "UpperArm.R": (-18.0, 10.0, 0.0),
          "LowerArm.L": (-40.0, 0.0, 0.0), "LowerArm.R": (-40.0, 0.0, 0.0)}
RECOIL = {"Hips": (-6.0, 0.0, 0.0), "Spine": (-10.0, 0.0, -4.0), "Chest": (-14.0, 3.0, -6.0),
          "Neck": (14.0, 0.0, 0.0), "Head": (14.0, 0.0, 8.0),
          "UpperArm.L": (-30.0, -34.0, 0.0), "UpperArm.R": (-24.0, 30.0, 0.0),
          "LowerArm.L": (-30.0, 0.0, 0.0), "LowerArm.R": (-36.0, 0.0, 0.0)}
CATCH_BALANCE = {"Hips": (4.0, 0.0, 0.0), "Spine": (8.0, 0.0, 2.0), "Chest": (6.0, 0.0, 2.0),
                 "Neck": (-4.0, 0.0, 0.0), "Head": (-6.0, 0.0, 0.0),
                 "UpperArm.L": (-24.0, -26.0, 0.0), "UpperArm.R": (-20.0, 24.0, 0.0),
                 "LowerArm.L": (-24.0, 0.0, 0.0), "LowerArm.R": (-24.0, 0.0, 0.0)}
FALL_BACK = {"Hips": (-34.0, 0.0, 0.0), "Spine": (14.0, 0.0, 0.0), "Chest": (12.0, 0.0, 0.0),
             "Neck": (16.0, 0.0, 0.0), "Head": (12.0, 0.0, 0.0),
             "UpperArm.L": (-40.0, -40.0, 0.0), "UpperArm.R": (-40.0, 40.0, 0.0),
             "LowerArm.L": (-30.0, 0.0, 0.0), "LowerArm.R": (-30.0, 0.0, 0.0)}
SIT_HIT = {"Hips": (-62.0, 0.0, 0.0), "Spine": (10.0, 0.0, 0.0), "Chest": (4.0, 0.0, 0.0),
           "Neck": (20.0, 0.0, 0.0), "Head": (14.0, 0.0, 0.0),
           "UpperArm.L": (30.0, -30.0, 0.0), "UpperArm.R": (30.0, 30.0, 0.0),
           "LowerArm.L": (-12.0, 0.0, 0.0), "LowerArm.R": (-12.0, 0.0, 0.0)}
LYING = {"Hips": (-88.0, 0.0, 0.0), "Spine": (-2.0, 0.0, 0.0), "Chest": (-2.0, 0.0, 0.0),
         "Neck": (6.0, 0.0, 0.0), "Head": (-4.0, 0.0, 10.0),
         "UpperArm.L": (-10.0, -48.0, 0.0), "UpperArm.R": (-8.0, 44.0, 0.0),
         "LowerArm.L": (-24.0, 0.0, 0.0), "LowerArm.R": (-30.0, 0.0, 0.0)}
SIT_UP = {"Hips": (-52.0, 0.0, 0.0), "Spine": (18.0, 0.0, 0.0), "Chest": (14.0, 0.0, 0.0),
          "Neck": (6.0, 0.0, 0.0), "Head": (0.0, 0.0, 0.0),
          "UpperArm.L": (40.0, -24.0, 0.0), "UpperArm.R": (40.0, 24.0, 0.0),
          "LowerArm.L": (-6.0, 0.0, 0.0), "LowerArm.R": (-6.0, 0.0, 0.0)}
SQUAT_RISE = {"Hips": (26.0, 0.0, 0.0), "Spine": (14.0, 0.0, 0.0), "Chest": (6.0, 0.0, 0.0),
              "Neck": (-14.0, 0.0, 0.0), "Head": (-16.0, 0.0, 0.0),
              "UpperArm.L": (-40.0, -10.0, 0.0), "UpperArm.R": (-40.0, 10.0, 0.0),
              "LowerArm.L": (-30.0, 0.0, 0.0), "LowerArm.R": (-30.0, 0.0, 0.0)}
HALF_RISE = {"Hips": (16.0, 0.0, 0.0), "Spine": (8.0, 0.0, 0.0), "Chest": (4.0, 0.0, 0.0),
             "Neck": (-8.0, 0.0, 0.0), "Head": (-8.0, 0.0, 0.0),
             "UpperArm.L": (-14.0, -6.0, 0.0), "UpperArm.R": (-14.0, 6.0, 0.0),
             "LowerArm.L": (-20.0, 0.0, 0.0), "LowerArm.R": (-20.0, 0.0, 0.0)}

DUCK = {"Spine": (12.0, 0.0, 0.0), "Chest": (10.0, 0.0, 0.0), "Neck": (12.0, 0.0, 0.0),
        "Head": (4.0, 0.0, 0.0)}

DOZE = {"Spine": (7.0, 0.0, 0.0), "Chest": (10.0, 0.0, 0.0), "Neck": (22.0, 0.0, 0.0),
        "Head": (18.0, 0.0, 4.0),
        "UpperArm.L": (-4.0, 1.0, 0.0), "UpperArm.R": (-4.0, -1.0, 0.0),
        "LowerArm.L": (-10.0, 0.0, 0.0), "LowerArm.R": (-10.0, 0.0, 0.0)}
DOZE_NOD = add(DOZE, {"Neck": (6.0, 0.0, 0.0), "Head": (8.0, 0.0, 0.0), "Chest": (2.0, 0.0, 0.0)})
DOZE_INHALE = add(DOZE, {"Chest": (-3.0, 0.0, 0.0), "Neck": (-1.0, 0.0, 0.0)})

# Levo: held in the air. Legs are FK here (no floor to reach).
LEVITATE_A = {"Spine": (-8.0, 0.0, 4.0), "Chest": (-6.0, 0.0, 6.0), "Neck": (10.0, 0.0, 0.0),
              "Head": (16.0, 0.0, -10.0),
              "UpperArm.L": (-70.0, -50.0, 0.0), "LowerArm.L": (-40.0, 0.0, 0.0),
              "UpperArm.R": (-20.0, 60.0, 0.0), "LowerArm.R": (-70.0, 0.0, 0.0),
              "UpperLeg.L": (-38.0, 0.0, 0.0), "LowerLeg.L": (70.0, 0.0, 0.0), "Foot.L": (24.0, 0.0, 0.0),
              "UpperLeg.R": (12.0, 0.0, 0.0), "LowerLeg.R": (26.0, 0.0, 0.0), "Foot.R": (34.0, 0.0, 0.0)}
LEVITATE_B = mirror(LEVITATE_A)

# ---- polearm family (arms come from the weapon IK) ----------------------------------

POLE_READY = {"Hips": (0.0, 0.0, 26.0), "Spine": (6.0, 0.0, -8.0), "Chest": (4.0, 0.0, -8.0),
              "Neck": (-4.0, 0.0, -6.0), "Head": (-4.0, 0.0, -4.0)}
POLE_DRAW = {"Hips": (-2.0, 0.0, 34.0), "Spine": (0.0, 0.0, -2.0), "Chest": (-2.0, 0.0, 0.0),
             "Neck": (0.0, 0.0, -12.0), "Head": (-4.0, 0.0, -14.0)}
POLE_EXTEND = {"Hips": (4.0, 0.0, 18.0), "Spine": (14.0, 0.0, -10.0), "Chest": (8.0, 0.0, -8.0),
               "Neck": (-12.0, 0.0, -2.0), "Head": (-10.0, 0.0, 0.0)}
SWEEP_WIND = {"Hips": (0.0, 0.0, 40.0), "Spine": (4.0, 0.0, 14.0), "Chest": (2.0, 0.0, 12.0),
              "Neck": (-2.0, 0.0, -28.0), "Head": (0.0, 0.0, -26.0)}
SWEEP_CUT = {"Hips": (6.0, 0.0, 6.0), "Spine": (14.0, 0.0, -10.0), "Chest": (8.0, 0.0, -12.0),
             "Neck": (-10.0, 0.0, 8.0), "Head": (-8.0, 0.0, 6.0)}
SWEEP_FOLLOW = {"Hips": (6.0, 0.0, -20.0), "Spine": (10.0, 0.0, -20.0), "Chest": (4.0, 0.0, -18.0),
                "Neck": (-8.0, 0.0, 20.0), "Head": (-6.0, 0.0, 18.0)}
HOOK_REACH = {"Hips": (6.0, 0.0, 16.0), "Spine": (16.0, 0.0, -10.0), "Chest": (6.0, 0.0, -8.0),
              "Neck": (-14.0, 0.0, 0.0), "Head": (-10.0, 0.0, 0.0)}
HOOK_DRAG = {"Hips": (-8.0, 0.0, 30.0), "Spine": (-4.0, 0.0, -6.0), "Chest": (-6.0, 0.0, -6.0),
             "Neck": (6.0, 0.0, -8.0), "Head": (2.0, 0.0, -8.0)}
CHOP_RAISE = {"Hips": (-2.0, 0.0, 20.0), "Spine": (-6.0, 0.0, -6.0), "Chest": (-8.0, 0.0, -6.0),
              "Neck": (0.0, 0.0, -4.0), "Head": (-6.0, 0.0, -4.0)}
CHOP_STRIKE = {"Hips": (8.0, 0.0, 16.0), "Spine": (18.0, 0.0, -8.0), "Chest": (10.0, 0.0, -8.0),
               "Neck": (-14.0, 0.0, 0.0), "Head": (-10.0, 0.0, 0.0)}

# ---- the Lantern Warden (signature, on its own rig: its arms' rest IS the carry) ----

WARDEN_CARRY = {"UpperArm.R": (-12.0, 0.0, 0.0), "LowerArm.R": (-14.0, 0.0, 0.0),
                "Hand.R": (29.0, 0.0, 0.0), "UpperArm.L": (-4.0, 0.0, 0.0)}
WARDEN_CARRY_RUN = {"UpperArm.R": (-14.0, 4.0, 0.0), "LowerArm.R": (-18.0, 0.0, 0.0),
                    "Hand.R": (26.0, 0.0, 0.0),
                    "UpperArm.L": (-58.0, 10.0, -12.0), "LowerArm.L": (-58.0, 0.0, 0.0)}
LANTERN_RAISE = {"UpperArm.L": (-84.0, 6.0, 10.0), "LowerArm.L": (-4.0, 0.0, 0.0),
                 "Spine": (2.0, 0.0, 0.0), "Chest": (-2.0, 0.0, 0.0), "Neck": (4.0, 0.0, 0.0),
                 "Head": (6.0, 0.0, 0.0)}
