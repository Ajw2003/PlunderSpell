"""The player character (docs/art/data/lair.json, players): the wizard.

One outfit for every player; only the colour of the dyed families (robe_wool,
hat_felt, hat_band) and the name on the hat band change. Built on figures.Human like
the Lantern Warden. The hat is on its own bone (Hat, child of Head) so the game can
hide it from the owner's camera and drop it. See docs/plans/wizard-character.md.
"""

from __future__ import annotations

import math

from mathutils import Vector

from .. import figures
from ..figures import ArmPose, Human
from ..kit import Part, spline
from ..spec import Entry
from . import blueprint

ROBE_PAD = 0.014
DYED = ["robe_wool", "hat_felt", "hat_band"]


def _hat(fig: Human) -> list[Part]:
    """Tall cone with the tip flopped back, a drooping brim and a band. Hat bone, rigid."""
    base = fig.lean((0.0, 0.0, 0.963 * fig.h))
    fig.add_bone("Hat", base, base + Vector((0, 0, 0.15)), "Head")
    r0 = 0.108

    def cone_r(t):
        return r0 * (1.0 - t) ** 1.15

    path = spline([(0, 0, -0.005), (0, 0, 0.15), (0, 0.005, 0.30), (0, 0.03, 0.43),
                   (0, 0.09, 0.52), (0, 0.17, 0.55)], 6)
    n = len(path)
    sections = [(cone_r(i / (n - 1)),) * 2 for i in range(n - 1)] + [(0.0, 0.0)]
    rigid = {"rigid": True, "prop": True, "smooth": True, "bevel": False}
    parts = [Part("sweep", tuple(base), (1, 1, 1), mat="hat_felt", bone="Hat", segments=16,
                  extras={"path": path, "sections": sections, "up": (0, 1, 0), **rigid})]
    parts.append(Part("lathe", tuple(base), (1, 1, 1), mat="hat_felt", bone="Hat", segments=24,
                      extras={"profile": [(0, -0.004), (0.10, -0.006), (0.175, -0.026),
                                          (0.188, -0.020), (0.178, -0.012), (0.10, 0.008),
                                          (0.0, 0.012)], **rigid}))
    parts.append(Part("lathe", tuple(base), (1, 1, 1), mat="hat_band", bone="Hat", segments=24,
                      extras={"profile": [(0.07, 0.012), (0.115, 0.012), (0.100, 0.078),
                                          (0.07, 0.078)], **rigid}))
    return parts


def _sleeve(fig: Human, side: str, mat: str) -> Part:
    """A wide sleeve: shoulder to a flared cuff just past the wrist, over the arm."""
    s = figures.SIDES[side]
    shoulder, elbow, wrist, _tip, _fore = fig._arm[side]
    up = (elbow - shoulder).normalized()
    fore = (wrist - elbow).normalized()
    pts = [shoulder + Vector((-s * 0.065, 0.0, -0.043)), shoulder,
           shoulder + up * 0.12, shoulder + up * 0.25, elbow,
           elbow + fore * 0.10, elbow + fore * 0.19, wrist + fore * 0.02]
    radii = [0.050, 0.058, 0.060, 0.064, 0.068, 0.076, 0.088, 0.096]
    return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=f"UpperArm.{side}", segments=16,
                extras={"path": [tuple(p) for p in pts],
                        "sections": [(r, r * 0.92) for r in radii],
                        "up": (0.0, 1.0, 0.0), "smooth": True, "bevel": False,
                        "bones": [f"Shoulder.{side}", f"UpperArm.{side}",
                                  f"LowerArm.{side}", "Chest"]})


def _cape(fig: Human) -> Part:
    """A short shoulder cape: a collar ring flaring to a hem over the chest and upper arms."""
    h, b = fig.h, fig.bulk
    rows = [(1.500, 0.0, 0.8), (1.465, 0.0, 1.05), (1.41, 0.004, 1.20),
            (1.35, 0.010, 1.34), (1.31, 0.014, 1.38), (1.305, 0.004, 1.32)]
    rings = []
    for z, pad, flare in rows:
        hw, hd, dy = fig.torso_dims(z)
        rings.append(fig.torso_ring(0, hw / (h * b), hd / (h * b), dy / h, 32,
                                    pad + ROBE_PAD + 0.012, 0.0, 0.0, z=z, flare=flare))
    return Part("loft", (0, 0, 0), (1, 1, 1), mat="robe_wool", bone="Chest", extras={
        "rings": rings, "smooth": True, "bevel": False,
        "bones": ["Chest", "Spine", "Neck", "Shoulder.L", "Shoulder.R",
                  "UpperArm.L", "UpperArm.R"]})


def _rot_to_radial(angle_deg: float) -> float:
    """Part rot Z so the local Y axis (a thin slab's thickness) points outward at angle."""
    return angle_deg + 270.0


def _belt_kit(fig: Human) -> list[Part]:
    parts = [fig.band(fig.belt_z, "glove_leather", height=0.05, pad=0.008, torso_pad=ROBE_PAD)]
    rigid = {"rigid": True, "bevel": False}
    bz = fig.belt_z
    # Brass buckle on the front: a square frame.
    buckle = fig.surface(bz, -90.0, pad=ROBE_PAD + 0.014)
    parts.append(Part("torus", tuple(buckle), (0.058, 0.058, 0.050), mat="brass", bone="Hips",
                      rot=(90, 0, 0), segments=4, rings=4, minor=0.2, extras=rigid))

    # Grimoire at the RIGHT hip (-X): a calfskin book hung from the belt, brass corners.
    a = -150.0
    centre = fig.surface(bz - 0.115, a, pad=ROBE_PAD + 0.034)
    rz = _rot_to_radial(a)
    parts.append(Part("box", tuple(centre), (0.115, 0.040, 0.150), mat="calfskin", bone="Hips",
                      rot=(0, 0, rz), extras=rigid))
    ca, sa = math.cos(math.radians(rz)), math.sin(math.radians(rz))
    for dx in (-0.056, 0.056):
        for dz in (-0.073, 0.073):
            at = centre + Vector((dx * ca, dx * sa, dz))
            parts.append(Part("box", tuple(at), (0.026, 0.046, 0.026), mat="brass",
                              bone="Hips", rot=(0, 0, rz), extras=rigid))

    # LEFT hip (+X): coin pouch, and the portal watch on its chain.
    pouch = fig.surface(bz - 0.115, 20.0, pad=ROBE_PAD + 0.040)
    parts.append(Part("sphere", tuple(pouch), (0.105, 0.075, 0.125), mat="calfskin",
                      bone="Hips", segments=10, rings=7, extras=rigid))
    parts.append(Part("cyl", tuple(pouch + Vector((0, 0, 0.066))), (0.050, 0.050, 0.030),
                      mat="calfskin", bone="Hips", segments=8, extras=rigid))
    belt_pt = fig.surface(bz - 0.01, -50.0, pad=ROBE_PAD + 0.014)
    watch = fig.surface(bz - 0.185, -62.0, pad=ROBE_PAD + 0.032)
    mid = (belt_pt + watch) / 2 + Vector((0.02, -0.014, 0.0))
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="brass", bone="Hips", segments=6,
                      extras={"path": spline([tuple(belt_pt), tuple(mid), tuple(watch)], 4),
                              "section": (0.005, 0.005), "up": (0, 0, 1), "smooth": True,
                              "bevel": False, "rigid": True}))
    parts.append(Part("cyl", tuple(watch + Vector((0, -0.006, -0.02))), (0.062, 0.062, 0.020),
                      mat="brass", bone="Hips", rot=(90, 0, 0), segments=16, extras=rigid))
    # Brass clasp at the throat, on the cape front.
    clasp = fig.surface(1.455, -90.0, pad=ROBE_PAD + 0.050)
    parts.append(Part("cyl", tuple(clasp), (0.048, 0.048, 0.014), mat="brass", bone="Chest",
                      rot=(90, 0, 0), segments=14, extras=rigid))
    return parts


def _beard(fig: Human) -> list[Part]:
    """Short and scruffy: a jaw mass and a ragged point."""
    h = fig.h
    c = fig.lean((0.0, -0.058 * h, 0.874 * h))
    rigid = {"rigid": True, "bevel": False}
    return [Part("sphere", tuple(c + Vector((0, 0.010, 0.0))), (0.120, 0.085, 0.080),
                 mat="beard_hair", bone="Head", segments=10, rings=6, extras=rigid),
            Part("cone", tuple(c + Vector((0.004, -0.006, -0.040))), (0.060, 0.045, 0.080),
                 mat="beard_hair", bone="Head", rot=(180, 0, 8), segments=6, extras=rigid)]


def wizard(entry: Entry):
    fig = Human(height=1.80, bulk=1.0, shoulders=0.46,
                arm_l=ArmPose(spread=11.0, swing=3.0, elbow=10.0),
                arm_r=ArmPose(spread=11.0, swing=3.0, elbow=10.0))
    seam = [{"mat": "hat_band", "min": (-0.017, -1.0, -1.0),
             "max": (0.017, -0.02, fig.belt_z - 0.03)}]
    parts = [fig.torso_part("robe_wool", pad=ROBE_PAD, hem=0.08, hem_flare=1.95,
                            collar=0.02, segments=48, paint=seam)]
    parts[0].extras["skirt"]["strength"] = 1.0   # a lifted knee must drag the whole hem
    for side in ("L", "R"):
        parts.append(_sleeve(fig, side, "robe_wool"))
        parts += fig.hand_part(side, "skin" if side == "R" else "glove_leather")
        parts.append(fig.leg_part(side, "boot_leather"))
        parts.append(fig.foot_part(side, "boot_leather", length=0.27, point=0.2))
    parts += fig.head_part("skin", features="eye_dark")
    parts += _beard(fig)
    parts.append(_cape(fig))
    parts += _belt_kit(fig)
    parts += _hat(fig)

    return blueprint(
        entry, parts, bevel=0.003, dye_families=DYED, **fig.rig(),
        family_overrides={
            "hat_band": {"rough": 0.9},
            "beard_hair": {"rough": 0.9},
            "brass": {"rough": 0.4},
        },
        notes=[
            "Height 1.80 m is to the crown of the skull; the hat (own bone, child of Head, "
            "prop: excluded from the height check) adds about 0.47 m.",
            "Dyed families: robe_wool (robe, cape, sleeves, seam excepted), hat_felt, "
            "hat_band. The robe's front seam is painted hat_band (darker, dyed). The baked "
            "<Name>_DyeMask.png is white on the dyed families.",
            "Right hand (-X) bare, left (+X) gloved; grimoire at the right hip, pouch and "
            "watch at the left. Arms hang relaxed; no props in the hands.",
            "Not built: fingers, the hat-band name (a runtime texture), cloth springs.",
        ])


BLUEPRINTS = {"wizard": wizard}
