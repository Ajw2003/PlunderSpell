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


def _tongue(at, height: float, width: float, mat: str, bone: str, lean, sway: float) -> Part:
    """One flame tongue: a sweep from a round root through an S-curve to a point."""
    at = Vector(at)
    side = Vector((-lean[1], lean[0], 0.0))
    if side.length < 1e-6:
        side = Vector((1.0, 0.0, 0.0))
    side = side.normalized() * height * 0.09 * sway
    pts = [at]
    for u, wob in ((0.22, 0.6), (0.45, 1.0), (0.68, -0.8), (0.86, -0.3), (1.0, 0.4)):
        pts.append(at + Vector((lean[0] * height * u, lean[1] * height * u, height * u)) + side * wob)
    w = width
    return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, segments=6, extras={
        "path": [tuple(p) for p in pts],
        "sections": [(w * 0.7, w * 0.55), (w * 1.1, w * 0.8), (w * 1.0, w * 0.72), (w * 0.62, w * 0.45),
                     (w * 0.3, w * 0.22), (0, 0)],
        "up": (0.0, 1.0, 0.0), "prop": True, "bevel": False, "smooth": True})


def _flame(at, height: float, width: float, bone: str, lean=(0.0, 0.0), sway: float = 1.0,
           layers: int = 3) -> list[Part]:
    """A painted flame in three tones, each layer smaller and nearer the viewer (-Y): a dark
    madder shell, madder, and a pale core. Geometry for now; a fire shader replaces it later
    (owner, 2026-10-11). Props: rigid on `bone`, left out of the height check."""
    at = Vector(at)
    parts = [_tongue(at, height, width, "flame_shadow", bone, lean, sway)]
    if layers >= 2:
        parts.append(_tongue(at + Vector((0, -width * 0.38, height * 0.02)), height * 0.78, width * 0.74,
                             "flame", bone, lean, -sway * 0.8))
    if layers >= 3:
        parts.append(_tongue(at + Vector((0, -width * 0.66, height * 0.04)), height * 0.48, width * 0.46,
                             "lit_horn_flame_core", bone, lean, sway * 0.6))
    return parts


def _blaze(at, height: float, width: float, bone: str, lean=(0.0, 0.0), n: int = 3,
           spread: float = 1.0, layers: int = 3) -> list[Part]:
    """A cluster of n tongues side by side, the middle one tallest."""
    at = Vector(at)
    side = Vector((-lean[1], lean[0], 0.0)) if (lean[0] or lean[1]) else Vector((1.0, 0.0, 0.0))
    side = side.normalized() if side.length > 1e-6 else Vector((1.0, 0.0, 0.0))
    parts = []
    for i in range(n):
        u = i - (n - 1) / 2
        h = height * (1.0 - 0.28 * abs(u) / max(1, (n - 1) / 2))
        parts += _flame(at + side * (u * width * 1.3 * spread), h, width * (0.85 if u else 1.0), bone,
                        lean=(lean[0] + u * 0.12, lean[1]), sway=1.0 if i % 2 else -1.0, layers=layers)
    return parts


def _fire(fig: Human) -> list[Part]:
    """Fire on the back and shoulders, out of the coat's tears, along the hem, at the elbows."""
    parts = []
    sz = fig.shoulder_z
    back_y = fig.lean((0, 0, sz)).y
    for s, bone in ((1.0, "Shoulder.L"), (-1.0, "Shoulder.R")):
        parts += _blaze((s * 0.16, back_y + 0.04, sz + 0.0), 0.62, 0.075, bone, lean=(s * 0.10, 0.10), n=3)
        parts += _blaze((s * 0.36, back_y + 0.02, sz - 0.05), 0.46, 0.065, bone, lean=(s * 0.22, 0.06), n=2)
    parts += _blaze((0.0, back_y + 0.22, sz - 0.10), 0.80, 0.10, "Chest", lean=(0.0, 0.22), n=3, spread=1.2,
                    layers=2)
    for z, ang, h in ((2.08, -48.0, 0.30), (1.92, -132.0, 0.28), (1.62, -42.0, 0.22)):
        p = fig.surface(z, ang, pad=PAD + 0.01)
        parts += _flame(p, h, 0.06, "Spine" if z < 1.9 else "Chest", lean=(0.0, -0.12))
    for i, a in enumerate((-150, -118, -90, -62, -30)):
        r = math.radians(a)
        p = (math.cos(r) * 0.38, math.sin(r) * 0.30, 1.07)
        parts += _flame(p, 0.24 + 0.08 * (i % 2), 0.07, "Hips", lean=(math.cos(r) * 0.15, -0.05),
                        sway=1.0 if i % 2 else -1.0, layers=2 if i % 2 else 3)
    for side, bone in (("L", "UpperArm.L"), ("R", "UpperArm.R")):
        e = fig.joint(f"elbow.{side}")
        s = 1.0 if side == "L" else -1.0
        parts += _flame(e + Vector((s * 0.07, 0.02, 0.02)), 0.34, 0.07, bone, lean=(s * 0.2, 0.0))
    return parts


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
    # the flame inside the cage, in three tones, rooted in a blackened wick
    parts += _flame(c0 + Vector((0.0, 0.0, 0.03)), 0.36, 0.085, "Cage", lean=(0.0, -0.05), sway=0.6)
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
        parts += _flame(at + Vector((0, -0.01, 0.01)), 0.20 + 0.07 * (k % 2), 0.035, "Hat",
                        lean=(math.cos(a) * 0.6, -0.35), sway=1.0 if k % 2 else -1.0, layers=2)
        parts.append(Part("sphere", tuple(at), (0.030, 0.030, 0.030), mat="vent_fire", bone="Hat",
                          segments=6, rings=4, extras={"rigid": True, "bevel": False}))
    parts += _blaze(hat_base + Vector((0, 0.02, dome_h - 0.02)), 0.34, 0.06, "Hat", n=3, layers=3)
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
    parts += _flame(foot + Vector((0, 0, 0.20)), 0.26, 0.05, "Spine", sway=0.7)   # the candle, burning
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
        {"mat": "flame_shadow", "min": (-0.12, -2, 1.88), "max": (0.12, cav_y, 2.34)},
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
    parts += _fire(fig)

    return blueprint(
        entry, parts, bevel=0.004, **fig.rig(),
        # Fire is a shader in engine, not geometry (owner, 2026-10-11): the FireMask says where.
        heat_skip_props=True,   # ~90 small flame islands break Blender's heat solve otherwise
        fire_families={"burning_wool": 1.0, "vent_fire": 1.0, "flame": 1.0, "flame_shadow": 1.0,
                       "lit_horn_flame_core": 0.8, "char": 0.35},
        family_overrides={
            # No glow (owner, 2026-10-11): the fire is painted, in three tones, until the shader.
            "flame_shadow": {"emit": None, "rough": 0.8, "grain": 0.18,
                             "streaks": {"colour": "#4A1A10", "amount": 0.5, "scale": 40.0}},
            "flame": {"emit": None, "rough": 0.7, "grain": 0.12,
                      "streaks": {"colour": "#E0803A", "amount": 0.45, "scale": 45.0}},
            "lit_horn_flame_core": {"emit": None, "rough": 0.6, "grain": 0.08,
                                    "gradients": [{"colour": "#F4E2B0", "z0": 0.0, "z1": 3.2, "amount": 0.4}]},
            "vent_fire": {"emit": None},
            "gambeson_wool": {"grain": 0.42, "weave": (0.010, 0.22),
                              "gradients": [{"colour": "#1E1610", "z0": 2.05, "z1": 2.60, "amount": 0.9},
                                            {"colour": "#3A2C1E", "z0": 1.55, "z1": 1.05, "amount": 0.85}],
                              "streaks": {"colour": "#8A7656", "amount": 0.32, "scale": 22.0},
                              "flecks": [{"colour": "#2A1E16", "amount": 0.18, "scale": 130.0},
                                         {"colour": "#6E2E24", "amount": 0.10, "scale": 70.0}]},
            "burning_wool": {"grain": 0.5, "cracks": {"colour": "#8A3A1A", "amount": 0.6, "scale": 30.0, "width": 0.06},
                             "flecks": [{"colour": "#C4542E", "amount": 0.25, "scale": 140.0}]},
            "char": {"grain": 0.45, "cracks": {"colour": "#5A2A16", "amount": 0.55, "scale": 26.0, "width": 0.05},
                     "flecks": [{"colour": "#0E0A08", "amount": 0.4, "scale": 110.0}]},
            "skin": {"grain": 0.4, "cracks": {"colour": "#6E2E24", "amount": 0.45, "scale": 55.0, "width": 0.03},
                     "streaks": {"colour": "#5A2218", "amount": 0.4, "scale": 35.0},
                     "flecks": [{"colour": "#5A4030", "amount": 0.3, "scale": 140.0}]},
            "blood": {"grain": 0.3, "streaks": {"colour": "#3A1410", "amount": 0.6, "scale": 30.0},
                      "flecks": [{"colour": "#8E3A26", "amount": 0.3, "scale": 90.0}]},
            "bone": {"grain": 0.32, "cracks": {"colour": "#6A5A44", "amount": 0.6, "scale": 48.0, "width": 0.025},
                     "gradients": [{"colour": "#7A6448", "z0": 2.9, "z1": 2.4, "amount": 0.5}],
                     "flecks": [{"colour": "#3A2E1E", "amount": 0.3, "scale": 120.0}]},
            "woad_hose": {"grain": 0.38, "weave": (0.008, 0.2),
                          "gradients": [{"colour": "#1E2630", "z0": 0.9, "z1": 0.05, "amount": 0.85}],
                          "streaks": {"colour": "#2E3A48", "amount": 0.4, "scale": 30.0}},
            "leather": {"grain": 0.4, "cracks": {"colour": "#2A1E16", "amount": 0.5, "scale": 60.0, "width": 0.03}},
            "tallow": {"grain": 0.3, "streaks": {"colour": "#A89870", "amount": 0.5, "scale": 50.0}},
            "blackened_iron": {"rough": 0.55, "wear_to": "#2A2826", "wear_amount": 0.4,
                               "flecks": [{"colour": "#5A3A28", "amount": 0.3, "scale": 70.0}]},
        },
        notes=[
            "Fire is geometry for now (owner, 2026-10-11): painted three-tone tongues, no glow, "
            "props rigid on the bone they sit on, left out of the height check. A fire shader "
            "replaces them later; TheWick_FireMask.png is still baked for it (white = flames, "
            "grey = smoulder).",
            "The lantern is not on this model: it is the warden's, dropped at the turn and left "
            "where it falls.",
            "Not built: the hunch of the shoulders above the neck (figures.Human has no shoulder "
            "raise; stoop 22 degrees stands in), toes bursting through the shoes.",
        ])


BLUEPRINTS = {"wick": wick}
