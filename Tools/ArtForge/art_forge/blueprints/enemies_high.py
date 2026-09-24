"""High Medieval enemies (docs/art/data/high.json, enemies).

The two samples that set the bar for the rigged path: a patrol humanoid built on
figures.Human with clothing layers and hand props, and a quadruped built on
figures.Quadruped with a coat, harness and collar. Read these before writing a
new enemy; the README's "Enemies" section explains the API they use.
"""

from __future__ import annotations

import math

from mathutils import Vector

from .. import figures
from ..figures import ArmPose, Human, Quadruped
from ..kit import Part, spline
from ..spec import Entry
from . import blueprint


# --------------------------------------------------------------------------------
# Lantern Warden (patrol) — light, brim, pole, in that order.
# --------------------------------------------------------------------------------

def _kettle_hat(fig: Human, mat: str) -> list[Part]:
    """One-piece kettle hat: dome with a raised ridge, a 0.40 m brim sloping 10°
    down with a rolled edge, four lining rivets. Rigid on the Hat bone."""
    h = fig.h
    base = fig.lean((0.0, 0.004 * h, 0.958 * h))
    crown = 1.80   # the spec's height is the crown of this hat
    dome_h = crown - base.z
    r_dome, r_brim = 0.106, 0.200
    drop = math.tan(math.radians(10.0)) * (r_brim - r_dome)
    profile = [
        (0.0, dome_h - 0.004), (0.030, dome_h - 0.010), (0.062, dome_h - 0.030),
        (0.088, dome_h - 0.062), (0.102, 0.035), (r_dome, 0.004),      # dome
        (0.140, -drop * 0.40), (r_brim - 0.006, -drop),                  # brim, 10° down
        (r_brim, -drop - 0.006), (r_brim - 0.004, -drop - 0.014),        # rolled edge
        (0.150, -drop * 0.45 - 0.010), (r_dome - 0.008, -0.010),         # underside
    ]
    fig.add_bone("Hat", base, base + Vector((0, 0, dome_h)), "Head")
    parts = [Part("lathe", tuple(base), (1, 1, 1), mat=mat, bone="Hat", segments=20,
                  extras={"profile": profile, "rigid": True, "smooth": True,
                          "paint": []})]
    # The raised central ridge, front to back over the dome.
    ridge = [base + Vector((0.0, math.cos(a) * 0.098, 0.004 + math.sin(a) * (dome_h - 0.012)))
             for a in [math.radians(d) for d in range(20, 161, 20)]]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, bone="Hat", segments=6,
                      extras={"path": [tuple(p) for p in ridge], "section": (0.008, 0.006),
                              "rigid": True, "smooth": True, "bevel": False}))
    for k in range(4):
        a = math.radians(45 + 90 * k)
        at = base + Vector((math.cos(a) * 0.104, math.sin(a) * 0.104, 0.022))
        parts.append(Part("sphere", tuple(at), (0.012, 0.012, 0.012), mat=mat, bone="Hat",
                          segments=6, rings=4, extras={"rigid": True, "bevel": False}))
    return parts


def _glaive(fig: Human) -> list[Part]:
    """2.05 m overall: ash haft 3 cm, iron socket + 2 langets 0.14 m, 0.35 m
    single-edged blade 6 cm deep with a 5 cm back fluke, 5 cm butt ferrule.
    Upright in the right fist, butt on the ground; a prop bone on Hand.R."""
    g = fig.grip("R")
    x, y = g.x, g.y
    overall, blade_len, socket_len = 2.05, 0.35, 0.14
    haft_top = overall - blade_len - socket_len
    fig.prop_bone("Glaive", "R", head=g, tail=(x, y, overall))
    prop = {"prop": True}
    parts = [
        Part("cyl", (x, y, (0.05 + haft_top) / 2), (0.030, 0.030, haft_top - 0.05),
             mat="ash_haft", bone="Glaive", segments=8, extras={**prop, "smooth": True,
                                                              "bevel": False}),
        Part("cyl", (x, y, 0.026), (0.034, 0.034, 0.052), mat="blackened_iron",
             bone="Glaive", segments=8, taper=0.9, extras=dict(prop)),
        Part("cyl", (x, y, haft_top + socket_len / 2), (0.038, 0.038, socket_len),
             mat="blackened_iron", bone="Glaive", segments=8, taper=0.8, extras=dict(prop)),
    ]
    for side in (1.0, -1.0):   # langets down the haft, front and back
        parts.append(Part("box", (x, y + side * 0.017, haft_top - 0.05), (0.012, 0.005, 0.14),
                          mat="blackened_iron", bone="Glaive", extras=dict(prop)))
    # Blade in the YZ plane: edge forward (-Y), fluke back (+Y). Outline u = toward
    # -Y from the haft axis, v = up from the socket; rot (90, 0, -90) maps u -> -Y,
    # v -> +Z and the extrusion onto X.
    outline = [(-0.014, 0.0), (0.022, 0.0), (0.056, 0.07), (0.062, 0.17), (0.046, 0.26),
               (0.0, blade_len), (-0.012, 0.27), (-0.013, 0.15), (-0.052, 0.125),
               (-0.016, 0.095), (-0.014, 0.0)]
    parts.append(Part("prism", (x, y, overall - blade_len), (1, 1, 0.009),
                      mat="blackened_iron", bone="Glaive", rot=(90.0, 0.0, -90.0),
                      extras={"outline": outline, **prop}))
    return parts


def _lantern(fig: Human) -> list[Part]:
    """Horn-paned iron box 14 × 14 × 24 cm, conical vented cap, 5 cm carrying ring,
    three frame bars per face. Hangs from the left fist on a two-bone swing chain
    (LanternRing -> LanternBody), rigid on each."""
    g = fig.grip("L")
    ring_c = g + Vector((0.0, 0.0, -0.012))
    cap_top = ring_c.z - 0.030
    body_top = cap_top - 0.060
    body_bot = body_top - 0.24 + 0.06
    c = Vector((ring_c.x, ring_c.y, 0.0))
    fig.prop_bone("LanternRing", "L", head=ring_c, tail=(c.x, c.y, cap_top))
    fig.add_bone("LanternBody", (c.x, c.y, cap_top), (c.x, c.y, body_bot), "LanternRing")
    prop = {"prop": True}
    w, mid = 0.14, (body_top + body_bot) / 2
    parts = [
        Part("torus", tuple(ring_c), (0.05, 0.05, 0.05), mat="blackened_iron",
             bone="LanternRing", rot=(90.0, 0.0, 0.0), segments=10, rings=5, minor=0.16,
             extras={**prop, "bevel": False}),
        Part("cyl", (c.x, c.y, cap_top + 0.006), (0.010, 0.010, 0.028), mat="blackened_iron",
             bone="LanternRing", segments=6, extras={**prop, "bevel": False}),
        # horn panes (a slightly inset box: the glowing part)
        Part("box", (c.x, c.y, mid), (w - 0.012, w - 0.012, body_top - body_bot - 0.01),
             mat="horn_pane", bone="LanternBody", extras=dict(prop)),
        # base plate, top plate, conical cap
        Part("box", (c.x, c.y, body_bot + 0.008), (w + 0.01, w + 0.01, 0.016),
             mat="blackened_iron", bone="LanternBody", extras=dict(prop)),
        Part("box", (c.x, c.y, body_top - 0.006), (w + 0.01, w + 0.01, 0.012),
             mat="blackened_iron", bone="LanternBody", extras=dict(prop)),
        Part("cone", (c.x, c.y, body_top + 0.030), (w * 1.02, w * 1.02, 0.060),
             mat="blackened_iron", bone="LanternBody", segments=4, rot=(0, 0, 45.0),
             taper=0.12, extras=dict(prop)),
    ]
    height = body_top - body_bot
    for sx, sy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):   # corner posts
        parts.append(Part("box", (c.x + sx * w / 2, c.y + sy * w / 2, mid),
                          (0.014, 0.014, height), mat="blackened_iron", bone="LanternBody",
                          extras=dict(prop)))
    for face in range(4):   # three bars per face
        a = math.radians(90 * face)
        n = Vector((math.cos(a), math.sin(a), 0.0))
        t = Vector((-n.y, n.x, 0.0))
        for u in (-0.036, 0.0, 0.036):
            at = c + n * (w / 2 - 0.004) + t * u
            parts.append(Part("box", (at.x, at.y, mid),
                              (0.006 + abs(t.x) * 0.0, 0.006, height - 0.012),
                              mat="blackened_iron", bone="LanternBody",
                              rot=(0, 0, math.degrees(a)), extras={**prop, "bevel": False}))
    return parts


def _lacing(fig: Human, pad: float) -> Part:
    """The gambeson's front lacing: a leather thong zig-zagging six crossings from
    the belt to the collar, laid on the quilted surface."""
    z0, z1 = fig.belt_z + 0.04, 0.815 * fig.h
    pts = []
    for i in range(13):
        z = z0 + (z1 - z0) * i / 12
        side = -1.0 if i % 2 else 1.0
        p = fig.surface(z, -90.0 + side * 6.0, pad=pad + 0.004)
        pts.append(tuple(p))
    return Part("tube", (0, 0, 0), (1, 1, 1), mat="leather", bone="Chest", segments=4,
                extras={"path": pts, "section": (0.004, 0.006), "smooth": True,
                        "bevel": False, "bones": ["Spine", "Chest", "Hips"]})


def lantern_warden(entry: Entry):
    # Eyes at 1.65 m (JSON) -> stature 1.65 / 0.936 = 1.763 m; the kettle hat
    # brings the crown to 1.80 m. Stocky and padded: bulk 1.12, 0.48 m shoulders.
    fig = Human(height=1.763, bulk=1.10, shoulders=0.48, stoop=4.0,
                arm_r=ArmPose(spread=16.0, swing=4.0, elbow=78.0),
                arm_l=ArmPose(spread=14.0, swing=-2.0, elbow=16.0))
    pad = 0.020   # gambeson padding over the body
    parts = [fig.torso_part("gambeson_wool", pad=pad, hem=0.66, hem_flare=1.32,
                            collar=0.05, quilt=0.07, segments=32)]
    for side in ("L", "R"):
        parts.append(fig.arm_part(side, "gambeson_wool", pad=pad * 0.8, quilt_rings=13))
        parts += fig.hand_part(side, "skin")
        # Hose darkens from the knee down (mud): paint the lower leg.
        knee_z = fig.knee_z + 0.02
        parts.append(fig.leg_part(side, "woad_hose", paint=[
            {"mat": "woad_mud", "min": (-1, -1, -1), "max": (1, 1, knee_z)}]))
        parts.append(fig.foot_part(side, "leather", length=0.27, point=0.4))
        # 3 cm leather garter below the knee.
        knee = fig.joint(f"knee.{side}")
        garter = knee + (fig.joint(f"ankle.{side}") - knee) * 0.16
        parts.append(Part("cyl", tuple(garter), (0.074, 0.078, 0.03), mat="leather",
                          bone=f"LowerLeg.{side}", segments=12,
                          extras={"bevel": False, "smooth": True,
                                  "bones": [f"LowerLeg.{side}", f"UpperLeg.{side}"]}))
    parts += fig.head_part("gambeson_wool", face="skin", hood=True, features="leather")
    parts += _kettle_hat(fig, "blackened_iron")

    # Belt at 1.03 m with an iron frame buckle, purse (left front), ballock knife
    # in its sheath (right hip).
    parts.append(fig.band(fig.belt_z, "leather", height=0.04, pad=0.008, torso_pad=pad))
    buckle = fig.surface(fig.belt_z, -90.0, pad=pad + 0.012)
    parts.append(Part("torus", tuple(buckle), (0.05, 0.05, 0.045), mat="blackened_iron",
                      bone="Hips", rot=(90, 0, 0), segments=4, rings=4, minor=0.2,
                      extras={"rigid": True, "bevel": False}))
    purse = fig.surface(fig.belt_z - 0.075, -58.0, pad=pad + 0.018)
    parts.append(Part("box", tuple(purse), (0.08, 0.03, 0.13), mat="leather", bone="Hips",
                      rot=(0, 0, -30.0), extras={"rigid": True}))
    parts.append(Part("box", tuple(purse + Vector((0.004, -0.008, 0.045))),
                      (0.086, 0.02, 0.05), mat="leather", bone="Hips", rot=(8, 0, -30.0),
                      extras={"rigid": True}))
    sheath = fig.surface(fig.belt_z - 0.10, -150.0, pad=pad + 0.02)
    parts.append(Part("cyl", tuple(sheath), (0.032, 0.022, 0.20), mat="leather", bone="Hips",
                      rot=(8, -10, 0), segments=6, taper=0.45, extras={"rigid": True}))
    parts.append(Part("sphere", tuple(sheath + Vector((0.012, -0.004, 0.13))),
                      (0.034, 0.030, 0.030), mat="ash_haft", bone="Hips", segments=8, rings=5,
                      extras={"rigid": True, "bevel": False}))
    parts.append(_lacing(fig, pad))

    parts += _glaive(fig)
    parts += _lantern(fig)

    return blueprint(
        entry, parts, bevel=0.004, **fig.rig(),
        family_overrides={
            # "emissive when lit (#C4542E flame at the core)": the panes carry the glow.
            "horn_pane": {"emit": "#C4542E", "rough": 0.45},
            "blackened_iron": {"rough": 0.55, "wear_to": "#2A2826", "wear_amount": 0.4},
            "gambeson_wool": {"grain": 0.30},
        },
        extra_families={
            "woad_mud": {"name": "Woad hose, muddied", "base": "#2B3A4E", "rough": 0.9,
                         "notes": "Woad hose darkened 30 % below the knee (JSON: 'mud to "
                                  "the knee (darken 30 %)')."},
        },
        notes=[
            "Glaive (2.05 m) and lantern are prop bones (Glaive on Hand.R; LanternRing -> "
            "LanternBody on Hand.L) and are left out of the 1.80 m height check.",
            "Hat is its own bone under Head (detachable per the JSON rig).",
            "Not built: gambeson_skirt x4 and coif_back spring bones, the candle inside "
            "the lantern (the horn panes carry the emissive), the scorched sleeve.",
        ])


BLUEPRINTS = {
    "lantern-warden": lantern_warden,
}
