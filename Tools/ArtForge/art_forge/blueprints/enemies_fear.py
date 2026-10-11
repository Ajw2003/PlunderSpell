"""The fear pass's horrors (docs/art/data/fear.json, enemies; #386).

The Wick: the Lantern Warden's true form, approved 2026-10-11 (docs/7-design/fear-pass.md,
"The true form"; concept docs/art/concept/fear/wick-v2-1-true-form.png). Built on the same
figures.Human as the warden, scaled to a 3.0 m stature with long arms, so it is the same
species turned inside out: the man has become his own lantern, and he is burning.
"""

from __future__ import annotations

import math

from mathutils import Vector

from .. import figures
from ..figures import ArmPose, Human
from ..kit import Part
from ..spec import Entry
from . import blueprint

PAD = 0.05   # the stretched gambeson over the frame


def _cage_and_cap(fig: Human) -> list[Part]:
    """The head: four bone bars rising 0.36 m from the neck, bowed out to 0.30 m across,
    knuckled, meeting at an iron ring; the flame inside; the kettle hat, 1.25x, as the vented
    cap on top, fire jetting from its vents."""
    c0 = fig.lean((0.0, 0.0, fig.neck_z + 0.03))
    rise = 0.36
    ct = fig.lean((0.0, 0.0, c0.z + rise))
    fig.add_bone("Cage", c0, ct, "Head")
    parts = []
    for k in range(4):
        a = math.radians(45 + 90 * k)
        pts = []
        for i in range(8):
            u = i / 7
            r = 0.055 + 0.100 * math.sin(math.pi * u) + 0.01 * (1 - u)
            z = c0.z + rise * u
            p = fig.lean((math.cos(a) * r, math.sin(a) * r, z))
            pts.append(tuple(p))
        parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="bone", bone="Cage", segments=6, extras={
            "path": pts, "sections": [(0.016, 0.016)] * 7 + [(0.012, 0.012)],
            "rigid": True, "smooth": True, "bevel": False}))
        for u in (0.3, 0.5, 0.7):   # vertebra knuckles along each bar
            r = 0.055 + 0.100 * math.sin(math.pi * u) + 0.01 * (1 - u)
            p = fig.lean((math.cos(a) * (r + 0.006), math.sin(a) * (r + 0.006), c0.z + rise * u))
            parts.append(Part("sphere", tuple(p), (0.036, 0.036, 0.026), mat="bone", bone="Cage",
                              segments=6, rings=4, extras={"rigid": True, "bevel": False}))
    parts.append(Part("torus", tuple(ct + Vector((0, 0, -0.004))), (0.13, 0.13, 0.13), mat="blackened_iron",
                      bone="Cage", segments=10, rings=4, minor=0.14, extras={"rigid": True, "bevel": False}))
    # the flame inside the cage: hot at the root, madder above, rooted in a blackened wick
    base = c0 + Vector((0.0, 0.0, 0.03))
    fpath = [tuple(fig.lean((0, 0, base.z + d))) for d in (0.0, 0.05, 0.12, 0.20, 0.27, 0.32)]
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="flame", bone="Cage", segments=8, extras={
        "path": fpath, "sections": [(0.03, 0.03), (0.07, 0.065), (0.075, 0.07), (0.055, 0.05), (0.025, 0.022), (0, 0)],
        "paint": [{"mat": "lit_horn_flame_core", "min": (-1, -2, base.z - 0.01), "max": (1, 2, base.z + 0.15)}],
        "prop": True, "smooth": True, "bevel": False}))
    parts.append(Part("cyl", tuple(c0 + Vector((0, 0, 0.01))), (0.03, 0.03, 0.04), mat="char", bone="Cage",
                      segments=6, extras={"rigid": True, "bevel": False}))
    # the cap: the warden's kettle hat at 1.25x, lifted on the cage
    s = 1.25
    r_dome, r_brim, dome_h = 0.106 * s, 0.200 * s, 0.17
    drop = math.tan(math.radians(10.0)) * (r_brim - r_dome)
    profile = [(0.0, dome_h - 0.004), (0.030 * s, dome_h - 0.012), (0.062 * s, dome_h - 0.036),
               (0.088 * s, dome_h - 0.075), (0.102 * s, 0.040), (r_dome, 0.004),
               (0.140 * s, -drop * 0.40), (r_brim - 0.008, -drop), (r_brim, -drop - 0.008),
               (r_brim - 0.005, -drop - 0.017), (0.150 * s, -drop * 0.45 - 0.012), (r_dome - 0.01, -0.012)]
    hat_base = ct + Vector((0.0, 0.0, 0.004))
    fig.add_bone("Hat", hat_base, hat_base + Vector((0, 0, dome_h)), "Cage")
    parts.append(Part("lathe", tuple(hat_base), (1, 1, 1), mat="blackened_iron", bone="Hat", segments=20,
                      extras={"profile": profile, "rigid": True, "smooth": True}))
    vents = []
    for k in range(4):   # four vents punched in the dome, front-facing, fire jetting out
        a = math.radians(-90 + (k - 1.5) * 26)
        rr = 0.085 * s
        at = hat_base + Vector((math.cos(a) * rr, math.sin(a) * rr, 0.10))
        vents.append(at)
        parts.append(Part("sphere", tuple(at), (0.030, 0.030, 0.030), mat="vent_fire", bone="Hat",
                          segments=6, rings=4, extras={"rigid": True, "bevel": False}))
    return parts


def _face_flaps(fig: Human) -> list[Part]:
    """The face, peeled into four flaps hanging from the neck over the collar: skin
    outside, blood inside; one eye on the viewer's left outer flap, teeth on the right."""
    c = fig.lean((0.0, 0.0, fig.neck_z + 0.07))
    parts = []
    for ang, ln in ((-145.0, 0.32), (-112.0, 0.27), (-68.0, 0.27), (-35.0, 0.32)):
        a = math.radians(ang)
        d = Vector((math.cos(a), math.sin(a), 0.0))
        root = c + d * 0.09
        pts = [root, root + d * 0.07 + Vector((0, 0, 0.02)), root + d * 0.15 + Vector((0, 0, -0.01)),
               root + d * 0.20 + Vector((0, 0, -ln * 0.55)), root + d * 0.22 + Vector((0, 0, -ln))]
        parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="skin", bone="Neck", segments=6, extras={
            "path": [tuple(p) for p in pts],
            "sections": [(0.012, 0.055), (0.012, 0.075), (0.011, 0.080), (0.010, 0.060), (0, 0)],
            "up": tuple(d), "power": 2.0, "smooth": True, "bevel": False, "bones": ["Neck", "Chest"]}))
        inner = [p - d * 0.009 for p in pts]
        parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="blood", bone="Neck", segments=6, extras={
            "path": [tuple(p) for p in inner[:-1]],
            "sections": [(0.006, 0.045), (0.006, 0.062), (0.006, 0.066), (0, 0)],
            "up": tuple(d), "smooth": True, "bevel": False, "bones": ["Neck", "Chest"]}))
        mid = pts[3]
        if ang == -145.0:   # the last eye
            e = mid + d * 0.012 + Vector((0, 0, 0.03))
            parts.append(Part("sphere", tuple(e), (0.030, 0.022, 0.020), mat="bone", bone="Neck", segments=8, rings=5,
                              extras={"rigid": True, "bevel": False}))
            parts.append(Part("sphere", tuple(e + d * 0.010), (0.012, 0.012, 0.012), mat="char", bone="Neck",
                              segments=6, rings=4, extras={"rigid": True, "bevel": False}))
        if ang == -35.0:    # half a jaw, its teeth along the flap's edge
            for k in range(4):
                tz = mid.z + 0.05 - k * 0.03
                tp = Vector((mid.x - 0.03, mid.y - 0.02, tz)) + d * 0.012
                parts.append(Part("cone", tuple(tp), (0.014, 0.014, 0.026), mat="bone", bone="Neck", rot=(0, 90, 0),
                                  segments=4, extras={"rigid": True, "bevel": False}))
    return parts


def _chest(fig: Human) -> list[Part]:
    """The burst lacing: six bone ribs bowed outward like the lantern's iron bars across the
    lit cavity, a sternum, and the tallow candle burning at its foot."""
    parts = []
    z0, z1 = 1.86, 2.34
    for s in (1.0, -1.0):
        for k, f in enumerate((0.45, 0.8, 1.15)):
            pts = []
            for i in range(7):
                u = i / 6
                z = z1 - (z1 - z0) * u
                deg = 3.0 + f * 17.0 * math.sin(math.pi * u)
                pts.append(tuple(fig.surface(z, -90.0 + s * deg, pad=PAD + 0.014)))
            parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="bone", bone="Chest", segments=6, extras={
                "path": pts, "sections": [(0.013 - 0.002 * k, 0.013 - 0.002 * k)] * 6 + [(0.009, 0.009)],
                "smooth": True, "bevel": False, "bones": ["Spine", "Chest"]}))
    sternum = [tuple(fig.surface(z1 - (z1 - z0) * i / 5, -90.0, pad=PAD + 0.008)) for i in range(6)]
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="bone", bone="Chest", segments=6, extras={
        "path": sternum, "sections": [(0.018, 0.014)] * 6, "smooth": True, "bevel": False,
        "bones": ["Spine", "Chest"]}))
    foot = fig.surface(z0 + 0.02, -90.0, pad=PAD + 0.05)
    parts.append(Part("cyl", tuple(foot + Vector((0, 0, 0.10))), (0.06, 0.06, 0.20), mat="tallow", bone="Spine",
                      segments=8, extras={"rigid": True, "bevel": False}))
    parts.append(Part("sphere", tuple(foot + Vector((0, 0, 0.205))), (0.05, 0.05, 0.03), mat="vent_fire", bone="Spine",
                      segments=6, rings=4, extras={"rigid": True, "bevel": False}))   # the candle's lit top
    return parts


def _blade_arm(fig: Human) -> list[Part]:
    """Right arm: the glaive's 0.70 m blade grows from the wrist, edge forward, point down;
    skin grown over an iron socket."""
    _sh, _el, wrist, _tip, fore = fig._arm["R"]
    fig.add_bone("Blade", wrist, wrist + fore * 0.70, "Hand.R")
    parts = [Part("sweep", (0, 0, 0), (1, 1, 1), mat="skin", bone="Hand.R", segments=8, extras={
        "path": [tuple(wrist - fore * 0.03), tuple(wrist + fore * 0.04), tuple(wrist + fore * 0.09)],
        "sections": [(0.060, 0.055), (0.058, 0.052), (0.040, 0.036)], "rigid": True, "smooth": True,
        "bevel": False})]
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="blackened_iron", bone="Blade", segments=6, extras={
        "path": [tuple(wrist + fore * 0.08), tuple(wrist + fore * 0.13)],
        "sections": [(0.034, 0.030), (0.026, 0.022)], "rigid": True, "bevel": False}))
    fwd = Vector((0.0, -1.0, 0.0))
    fwd = (fwd - fwd.dot(fore) * fore).normalized()
    pts, secs = [], []
    for i in range(7):
        u = i / 6
        p = wrist + fore * (0.12 + 0.62 * u) + fwd * (0.035 * math.sin(math.pi * u * 0.9))
        pts.append(tuple(p))
        secs.append((0.010 * (1 - 0.6 * u), 0.060 * math.sin(math.pi * min(1.0, 0.18 + u * 0.82)) + 0.004))
    secs[-1] = (0.0, 0.0)
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="blackened_iron", bone="Blade", segments=4, extras={
        "path": pts, "sections": secs, "up": tuple(fwd), "rigid": True, "bevel": False}))
    return parts


def _grasping_hand(fig: Human) -> list[Part]:
    """Left hand: open, four fingers 0.40 m long, bone-tipped, curling a little toward the palm."""
    s = 1.0
    _sh, _el, wrist, _tip, fore = fig._arm["L"]
    palm_normal = Vector((-s, 0.0, 0.0))
    width = fore.cross(palm_normal).normalized()
    palm_end = wrist + fore * 0.15
    parts = [Part("sweep", (0, 0, 0), (1, 1, 1), mat="skin", bone="Hand.L", segments=8, extras={
        "path": [tuple(wrist - fore * 0.02), tuple(wrist + fore * 0.07), tuple(palm_end)],
        "sections": [(0.045, 0.030), (0.060, 0.028), (0.058, 0.024)], "up": tuple(width),
        "smooth": True, "bevel": False, "bones": ["Hand.L", "LowerArm.L"]})]
    for j, off in enumerate((-0.042, -0.014, 0.014, 0.042)):
        ln = 0.40 - abs(off) * 1.2
        root = palm_end + width * off - fore * 0.01
        pts = []
        for i in range(6):
            u = i / 5
            p = root + fore * (ln * u) + palm_normal * (0.05 * u * u) + width * (off * 0.6 * u)
            pts.append(p)
        parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="skin", bone="Hand.L", segments=5, extras={
            "path": [tuple(p) for p in pts[:-1]], "sections": [(0.014, 0.012)] * 4 + [(0.010, 0.009)],
            "smooth": True, "bevel": False, "bones": ["Hand.L", "LowerArm.L"]}))
        tip_dir = (pts[-1] - pts[-2]).normalized()
        parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="bone", bone="Hand.L", segments=5, extras={
            "path": [tuple(pts[-2]), tuple(pts[-1]), tuple(pts[-1] + tip_dir * 0.04)],
            "sections": [(0.010, 0.009), (0.008, 0.007), (0, 0)], "rigid": True, "smooth": True,
            "bevel": False}))
    thumb_root = wrist + fore * 0.06 - width * 0.05
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="skin", bone="Hand.L", segments=5, extras={
        "path": [tuple(thumb_root), tuple(thumb_root + fore * 0.10 - width * 0.04),
                 tuple(thumb_root + fore * 0.20 - width * 0.03 + palm_normal * 0.03)],
        "sections": [(0.016, 0.014), (0.013, 0.011), (0, 0)], "rigid": True, "smooth": True, "bevel": False}))
    return parts


def wick(entry: Entry):
    # Stature 3.0 m to the top of the cage; the cap brings the crown to about 3.10 m.
    fig = Human(height=3.0, bulk=1.18, shoulders=0.88, stoop=22.0, stance=4.0, arm_length=1.35,
                arm_r=ArmPose(spread=20.0, swing=8.0, elbow=30.0),
                arm_l=ArmPose(spread=20.0, swing=8.0, elbow=24.0))
    cav_y = -0.02   # paint only the front half of the torso
    back_y = fig.lean((0, 0, fig.shoulder_z)).y
    torso_paint = [
        {"mat": "char", "min": (-2, -2, 2.18), "max": (2, 2, 4.0)},
        {"mat": "burning_wool", "min": (-2, back_y - 0.05, 2.10), "max": (2, 2, 4.0)},     # back and shoulders
        {"mat": "burning_wool", "min": (-2, -2, 2.32), "max": (2, 2, 4.0)},
        {"mat": "char", "min": (-2, -2, 0.0), "max": (2, 2, 1.24)},
        {"mat": "burning_wool", "min": (-2, -2, 0.0), "max": (2, 2, 1.13)},                 # the hem
        {"mat": "burning_wool", "min": (0.20, -2, 1.95), "max": (0.42, -0.05, 2.20)},       # tears in the coat
        {"mat": "burning_wool", "min": (-0.42, -2, 1.80), "max": (-0.22, -0.05, 2.02)},
        {"mat": "burning_wool", "min": (0.24, -2, 1.48), "max": (0.42, -0.05, 1.66)},
        {"mat": "blood", "min": (-0.17, -2, 1.82), "max": (0.17, cav_y, 2.40)},
        {"mat": "lit_horn_flame_core", "min": (-0.12, -2, 1.88), "max": (0.12, cav_y, 2.34)},
    ]
    parts = [fig.torso_part("gambeson_wool", pad=PAD, hem=1.05, hem_flare=1.38, quilt=0.09,
                            segments=32, paint=torso_paint)]
    for side in ("L", "R"):
        elbow = fig.joint(f"elbow.{side}")
        parts.append(fig.arm_part(side, "gambeson_wool", pad=0.03, quilt_rings=15, paint=[
            {"mat": "burning_wool", "min": (-2, -2, 2.32), "max": (2, 2, 4.0)},
            {"mat": "char", "min": (-2, -2, elbow.z - 0.10), "max": (2, 2, elbow.z + 0.12)}]))
        knee = fig.joint(f"knee.{side}")
        parts.append(fig.leg_part(side, "woad_hose", pad=-0.05))
        # the hose split at the knee: skin and a knob of bone showing through
        kp = knee + Vector((0.0, -0.07, 0.0))
        parts.append(Part("sphere", tuple(kp), (0.075, 0.035, 0.10), mat="skin", bone=f"LowerLeg.{side}",
                          segments=8, rings=5, extras={"bevel": False, "bones": [f"UpperLeg.{side}", f"LowerLeg.{side}"]}))
        parts.append(Part("sphere", tuple(kp + Vector((0, -0.022, 0.0))), (0.04, 0.03, 0.05), mat="bone",
                          bone=f"LowerLeg.{side}", segments=6, rings=4, extras={"rigid": True, "bevel": False}))
        parts.append(fig.foot_part(side, "leather", length=0.40, point=0.5))
    parts += _cage_and_cap(fig)
    parts += _face_flaps(fig)
    parts += _chest(fig)
    parts.append(fig.band(fig.belt_z - 0.12, "leather", height=0.05, pad=0.01, torso_pad=PAD))
    buckle = fig.surface(fig.belt_z - 0.17, -70.0, pad=PAD + 0.03)
    parts.append(Part("torus", tuple(buckle), (0.07, 0.07, 0.06), mat="blackened_iron", bone="Hips",
                      rot=(90, 0, 20), segments=4, rings=4, minor=0.2, extras={"rigid": True, "bevel": False}))
    parts += _blade_arm(fig)
    parts += _grasping_hand(fig)

    return blueprint(
        entry, parts, bevel=0.004, **fig.rig(),
        # Fire is a shader in engine, not geometry (owner, 2026-10-11): the FireMask says where.
        fire_families={"burning_wool": 1.0, "vent_fire": 1.0, "flame": 1.0,
                       "lit_horn_flame_core": 0.8, "char": 0.35},
        family_overrides={
            # Emissive families: the shipping material scales emission 9x, so the baked mask
            # stores the colour dimmed, as the warden's horn panes do.
            "flame": {"emit": "#4A1E0C", "rough": 0.6},
            "vent_fire": {"emit": "#4A1E0C", "rough": 0.6},
            "burning_wool": {"grain": 0.45},   # dark in the BaseMap; the fire shader brings the light
            "lit_horn_flame_core": {"emit": "#5A4628", "rough": 0.5},
            "blackened_iron": {"rough": 0.55, "wear_to": "#2A2826", "wear_amount": 0.4},
            "gambeson_wool": {"grain": 0.34},
        },
        notes=[
            "Fire is drawn by a shader in engine, not by geometry: TheWick_FireMask.png is white "
            "where flames burn (back, shoulders, hem, coat tears, elbows, the hat's vents, the "
            "cage flame) and grey where the wool smoulders. The only flame mesh is the one "
            "teardrop inside the head cage, the surface the head's fire shader runs on.",
            "The lantern is not on this model: it is the warden's, dropped at the turn and left "
            "where it falls.",
            "Not built: the hunch of the shoulders above the neck (figures.Human has no shoulder "
            "raise; stoop 22 degrees stands in), toes bursting through the shoes.",
        ])


BLUEPRINTS = {"wick": wick}
