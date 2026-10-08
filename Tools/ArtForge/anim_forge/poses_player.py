"""Poses for the player wizard's clips (library_player.py). Same conventions as poses.py;
nothing in poses.py is changed. Bones a pose does not name are at rest."""

from __future__ import annotations

from .poses import add, mirror

# penniless and impatient: shoulders rounded, head pushed forward (added over RELAXED)
HUNCH = {"Spine": (4.0, 0.0, 0.0), "Chest": (6.0, 0.0, 0.0), "Neck": (5.0, 0.0, 0.0),
         "Head": (-7.0, 0.0, 0.0)}

# crouch: pelvis tipped forward, back rounded, arms tucked, eyes up (hips ride 0.40 m low)
CROUCH = {"Hips": (24.0, 0.0, 0.0), "Spine": (14.0, 0.0, 0.0), "Chest": (10.0, 0.0, 0.0),
          "Neck": (-14.0, 0.0, 0.0), "Head": (-14.0, 0.0, 0.0),
          "UpperArm.L": (-22.0, -4.0, 0.0), "UpperArm.R": (-22.0, 4.0, 0.0),
          "LowerArm.L": (-72.0, 0.0, 0.0), "LowerArm.R": (-72.0, 0.0, 0.0)}
CROUCH_BREATH = add(CROUCH, {"Chest": (-2.5, 0.0, 0.0), "Neck": (1.5, 0.0, 0.0)})
CROUCH_LOOK_L = add(CROUCH, {"Neck": (0.0, 0.0, 14.0), "Head": (0.0, 0.0, 22.0), "Chest": (0, 0, 6)})
CROUCH_LOOK_R = add(CROUCH, {"Neck": (0.0, 0.0, -14.0), "Head": (0.0, 0.0, -22.0), "Chest": (0, 0, -6)})

# jump
JUMP_COIL = {"Hips": (26.0, 0.0, 0.0), "Spine": (16.0, 0.0, 0.0), "Chest": (10.0, 0.0, 0.0),
             "Neck": (-14.0, 0.0, 0.0), "Head": (-14.0, 0.0, 0.0),
             "UpperArm.L": (52.0, -10.0, 0.0), "UpperArm.R": (52.0, 10.0, 0.0),
             "LowerArm.L": (-18.0, 0.0, 0.0), "LowerArm.R": (-18.0, 0.0, 0.0)}
JUMP_SPRING = {"Hips": (-4.0, 0.0, 0.0), "Spine": (-6.0, 0.0, 0.0), "Chest": (-8.0, 0.0, 0.0),
               "Neck": (4.0, 0.0, 0.0), "Head": (4.0, 0.0, 0.0),
               "UpperArm.L": (-112.0, -22.0, 0.0), "UpperArm.R": (-112.0, 22.0, 0.0),
               "LowerArm.L": (-10.0, 0.0, 0.0), "LowerArm.R": (-10.0, 0.0, 0.0)}
# airborne: legs FK (no floor), thighs forward, knees folded, arms out and up for balance
JUMP_AIR_A = {"Hips": (6.0, 0.0, 0.0), "Spine": (2.0, 0.0, 0.0), "Chest": (-2.0, 0.0, 0.0),
              "Head": (-6.0, 0.0, 0.0),
              "UpperArm.L": (-30.0, -78.0, 0.0), "UpperArm.R": (-46.0, 70.0, 0.0),
              "LowerArm.L": (-26.0, 0.0, 0.0), "LowerArm.R": (-34.0, 0.0, 0.0),
              "UpperLeg.L": (-42.0, 0.0, 0.0), "LowerLeg.L": (68.0, 0.0, 0.0), "Foot.L": (22.0, 0.0, 0.0),
              "UpperLeg.R": (-24.0, 0.0, 0.0), "LowerLeg.R": (48.0, 0.0, 0.0), "Foot.R": (28.0, 0.0, 0.0)}
JUMP_AIR_B = mirror(JUMP_AIR_A)
JUMP_LAND_IN = {"Hips": (20.0, 0.0, 0.0), "Spine": (12.0, 0.0, 0.0), "Chest": (8.0, 0.0, 0.0),
                "Neck": (-10.0, 0.0, 0.0), "Head": (-12.0, 0.0, 0.0),
                "UpperArm.L": (-28.0, -52.0, 0.0), "UpperArm.R": (-28.0, 52.0, 0.0),
                "LowerArm.L": (-26.0, 0.0, 0.0), "LowerArm.R": (-26.0, 0.0, 0.0)}
JUMP_LAND_ABSORB = {"Hips": (32.0, 0.0, 0.0), "Spine": (18.0, 0.0, 0.0), "Chest": (12.0, 0.0, 0.0),
                    "Neck": (-18.0, 0.0, 0.0), "Head": (-16.0, 0.0, 0.0),
                    "UpperArm.L": (-34.0, -34.0, 0.0), "UpperArm.R": (-34.0, 34.0, 0.0),
                    "LowerArm.L": (-34.0, 0.0, 0.0), "LowerArm.R": (-34.0, 0.0, 0.0)}

# casting: the RIGHT hand is bare and casts; torso turned so the right shoulder leads
CAST_LEAN = {"Hips": (0.0, 0.0, 8.0), "Spine": (5.0, 0.0, 8.0), "Chest": (6.0, 0.0, 10.0),
             "Neck": (-4.0, 0.0, -6.0), "Head": (-6.0, 0.0, -10.0)}
CAST_LEFT_BACK = {"UpperArm.L": (34.0, -14.0, 0.0), "LowerArm.L": (-78.0, 0.0, 0.0),
                  "Hand.L": (-6.0, 0.0, 0.0)}
# right arm forward at shoulder height, forearm cocked up, palm turned out
CAST_ARM = {"UpperArm.R": (-84.0, 6.0, 0.0), "LowerArm.R": (-48.0, 0.0, 0.0),
            "Hand.R": (-14.0, 0.0, 90.0)}
CAST_HOLD = add(CAST_LEAN, CAST_LEFT_BACK, CAST_ARM)
CAST_PULSE = add(CAST_HOLD, {"Chest": (1.5, 0.0, 0.0), "UpperArm.R": (-2.0, 0.0, 0.0),
                             "LowerArm.R": (-3.0, 0.0, 0.0), "Hand.R": (-4.0, 0.0, 0.0)})
CAST_SHIVER = add(CAST_HOLD, {"UpperArm.R": (1.5, 1.5, 0.0), "LowerArm.R": (2.0, 0.0, 0.0),
                              "Hand.R": (3.0, 0.0, 3.0), "Head": (0.0, 0.0, 1.5)})
CAST_DRAW = add(CAST_HOLD, {"Chest": (-4.0, 0.0, -4.0), "Spine": (-3.0, 0.0, 0.0),
                            "UpperArm.R": (4.0, 0.0, 0.0), "LowerArm.R": (-34.0, 0.0, 0.0),
                            "Head": (4.0, 0.0, 0.0)})
CAST_THRUST = add(CAST_HOLD, {"Spine": (6.0, 0.0, 0.0), "Chest": (6.0, 0.0, 4.0),
                              "UpperArm.R": (-6.0, 0.0, 0.0), "LowerArm.R": (40.0, 0.0, 0.0),
                              "Hand.R": (-16.0, 0.0, 0.0), "Head": (-3.0, 0.0, 0.0),
                              "UpperArm.L": (10.0, 0.0, 0.0)})
