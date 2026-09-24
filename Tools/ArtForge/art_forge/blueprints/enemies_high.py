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
    # Yawed 35° off pure edge-forward so the blade reads in the front view too.
    parts.append(Part("prism", (x, y, overall - blade_len), (1, 1, 0.009),
                      mat="blackened_iron", bone="Glaive", rot=(90.0, 0.0, -90.0 + 35.0),
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
    body_bot = body_top - 0.24
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

    # Review pose: the lantern_raise_search beat. The lantern chain counter-rotates
    # so it hangs from the raised fist instead of pointing along the forearm.
    pose = dict(figures.HUMAN_TEST_POSE)
    pose["LanternRing"] = (105.0, 0.0, 0.0)
    return blueprint(
        entry, parts, bevel=0.004, **fig.rig(pose),
        family_overrides={
            # "emissive when lit (#C4542E flame at the core)": the panes carry the glow.
            # The emit hex is scaled by EnemyForge's EMISSION_STRENGTH (9x) in the
            # shipping material; #C4542E at 9x blows out to pink-white, so the
            # baked mask stores madder at ~35 % and the multiplier brings it back.
            "horn_pane": {"emit": "#4A1E0C", "rough": 0.45},
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


# --------------------------------------------------------------------------------
# Alaunt War-hound (special) — low, deep-chested, blocky head forward, a blue
# blanket on its back and a jagged ring of spikes at the neck.
# --------------------------------------------------------------------------------

def _coat(fig: Quadruped, y_front: float, y_back: float, hem_z: float) -> list[Part]:
    """The padded woad coat: a quilted shell draped over the back from withers to
    croup down to `hem_z`, a kermes bordure along every edge, an argent chevron on
    each flank. Built as a loft of C-shaped sections (outer arc, then inner arc
    back), so it is one closed shell standing a few cm off the body."""
    stations = 18
    arc = 12
    rings = []
    ys = [y_front + (y_back - y_front) * i / (stations - 1) for i in range(stations)]
    for i, y in enumerate(ys):
        cz, hw, hh, keel = fig.body_at(y)
        # angle where the body surface reaches the hem height
        s = max(-0.95, min(0.95, (hem_z / fig.sz - cz) / hh))
        a0 = math.asin(s)
        quilt = 0.017 if i % 2 == 0 else 0.012      # 5 cm channels, every other station
        edge = i in (0, stations - 1)
        outer = fig.body_ring(y, cz, hw, hh, keel, arc, pad=0.006 if edge else quilt,
                              a0=a0, a1=math.pi - a0, closed=False)
        inner = fig.body_ring(y, cz, hw, hh, keel, 5, pad=-0.004,
                              a0=a0, a1=math.pi - a0, closed=False)
        rings.append(outer + list(reversed(inner)))
    band = 0.03
    paint = [
        {"mat": "kermes_gules", "min": (-1, -2, -1), "max": (1, 2, hem_z + band)},
        {"mat": "kermes_gules", "min": (-1, -2, -1), "max": (1, ys[0] * fig.sy + band, 2)},
        {"mat": "kermes_gules", "min": (-1, ys[-1] * fig.sy - band, -1), "max": (1, 2, 2)},
    ]
    parts = [Part("loft", (0, 0, 0), (1, 1, 1), mat="woad_coat", bone="Spine2", extras={
        "rings": rings, "paint": paint, "bevel": False,
        "bones": ["Pelvis", "Spine1", "Spine2", "Spine3", "Chest", "Scapula.L",
                  "Scapula.R", "Femur.L", "Femur.R"]})]

    # Argent chevron on each flank: an inverted V strip laid on the coat.
    y_mid = (y_front + y_back) / 2
    for side in (1.0, -1.0):
        path = []
        for k in range(9):
            t = k / 8.0
            y = y_mid + (t - 0.5) * 0.46
            z = 0.66 - abs(t - 0.5) * 2.0 * (0.66 - hem_z - 0.045)
            cz, hw, hh, keel = fig.body_at(y)
            a = math.asin(max(-0.95, min(0.95, (z - cz) / hh)))
            ring = fig.body_ring(y, cz, hw, hh, keel, 1, pad=0.022, a0=a, a1=a,
                                 closed=False)
            px, py, pz = ring[0]
            path.append((side * px, py * 1.0, pz))
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="wool_argent", bone="Spine2",
                          segments=4, extras={
                              "path": path, "section": (0.004, 0.028),
                              "up": (side, 0.0, 0.0), "bevel": False,
                              "bones": ["Spine1", "Spine2", "Spine3", "Chest"]}))
    return parts


def _harness(fig: Quadruped, y_front: float, hem_z: float) -> list[Part]:
    """Leather breast strap round the chest front and a girth behind the forelegs."""
    parts = []
    # girth: a closed ring round the ribs just behind the elbows, over the coat on
    # the back and on the hair under the belly. Width runs along the body (Y).
    y = -0.08
    cz, hw, hh, keel = fig.body_at(y)
    ring = []
    for k in range(16):
        a = 2 * math.pi * k / 16
        z_here = (cz + math.sin(a) * hh) * fig.sz
        pad = 0.019 if z_here > hem_z - 0.01 else 0.008
        ring.append(fig.body_ring(y, cz, hw, hh, keel, 1, pad=pad, a0=a, a1=a,
                                  closed=False)[0])
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="collar_leather", bone="Spine3",
                      segments=4, extras={
                          "path": ring, "closed": True, "section": (0.022, 0.005),
                          "up": (0.0, 1.0, 0.0), "bevel": False, "smooth": True,
                          "bones": ["Spine2", "Spine3", "Chest"]}))
    # breast strap: from the coat's front edge on the left flank, round the chest
    # front at mid height, back along the right flank.
    z = 0.575 * fig.sz
    left = []
    for yy in (y_front + 0.02, -0.34, -0.38, -0.405):
        _cz, hw_y, _hh, _k = fig.body_at(yy)
        left.append((hw_y * fig.sx + 0.012, yy * fig.sy, z))
    left.append((0.052 * fig.sx, -0.428 * fig.sy, z))
    tip = (0.0, -0.442 * fig.sy, z)
    path = left + [tip] + [(-x, y2, z2) for x, y2, z2 in reversed(left)]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="collar_leather", bone="Chest",
                      segments=4, extras={
                          "path": path, "section": (0.020, 0.005), "up": (0, 0, 1),
                          "bevel": False, "smooth": True,
                          "bones": ["Chest", "Spine3", "Neck1", "Scapula.L", "Scapula.R"]}))
    return parts


def _collar(fig: Quadruped) -> list[Part]:
    """5 cm black leather collar, eight 3 cm iron cone spikes, a leash ring (on its
    own spring bone, CollarRing)."""
    centre, tangent, radius = fig.neck_frame(0.42)
    up = Vector((0, 0, 1))
    side = tangent.cross(up).normalized()
    top = side.cross(tangent).normalized()
    rot = tangent.to_track_quat("Z", "Y").to_euler()
    deg = tuple(math.degrees(a) for a in rot)
    r = radius + 0.010
    parts = [Part("cyl", tuple(centre), (2 * r, 2 * r, 0.05), mat="collar_leather",
                  bone="Neck1", rot=deg, segments=16,
                  extras={"rigid": True, "smooth": False})]
    for k in range(8):
        a = 2 * math.pi * (k + 0.5) / 8
        n = side * math.cos(a) + top * math.sin(a)
        base = centre + n * (r + 0.016)
        spike_rot = n.to_track_quat("Z", "Y").to_euler()
        parts.append(Part("cone", tuple(base), (0.026, 0.026, 0.036), mat="spike_iron",
                          bone="Neck1", rot=tuple(math.degrees(v) for v in spike_rot),
                          segments=5, extras={"rigid": True, "bevel": False}))
    ring_at = centre - top * (r + 0.022)
    fig.add_bone("CollarRing", ring_at + top * 0.012, ring_at - top * 0.03, "Neck1")
    parts.append(Part("torus", tuple(ring_at - top * 0.012), (0.045, 0.045, 0.045),
                      mat="spike_iron", bone="CollarRing", rot=deg, segments=10, rings=5,
                      minor=0.18, extras={"rigid": True, "bevel": False}))
    return parts


def alaunt_hound(entry: Entry):
    # JSON: 0.85 m at the top of the head, 0.72 m withers, 1.42 m nose to tail
    # tip, 0.30 m across the chest.
    fig = Quadruped(withers=0.72, length=1.42, chest_width=0.30)
    parts = fig.body("fawn_coat", "dark_mask", teeth="wool_argent")
    y_front, y_back, hem = -0.30, 0.34, 0.49
    parts += _coat(fig, y_front, y_back, hem)
    parts += _harness(fig, y_front, hem)
    parts += _collar(fig)
    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(),
        family_overrides={
            # "faint brindle stripes on the flanks": dark stripes rubbed into the fawn.
            "fawn_coat": {"wear_to": "#3A3026", "wear_amount": 0.30, "grain": 0.30},
            "spike_iron": {"rough": 0.5},
            # The JSON's "wet nose roughness 0.2" is parsed for the whole family and
            # made the mask read as lacquer; the nose alone is wet.
            "dark_mask": {"rough": 0.7},
        },
        notes=[
            "Teeth use the 'Wool argent' vellum (#C4B89C): the JSON lists no tooth "
            "material and the concept draws the fangs in that off-white.",
            "Rig follows the JSON's quadruped chain but with 5 tail and 2 neck bones "
            "and no coat/jowl spring bones (not built); CollarRing is the one spring.",
        ])


# --------------------------------------------------------------------------------
# Shared: a Human whose fists can be placed by two-bone IK.
# --------------------------------------------------------------------------------

class _ReachHuman(Human):
    """figures.Human with optional two-bone IK per arm.

    ArmPose can only bend the forearm forward in the arm's own plane, so two fists
    can never meet on a centred crossbow tiller. Rather than edit the shared
    figures.py, this subclass overrides `_arm_points`: `reach={"R": (x, y, z)}`
    puts that fist's grip (figures.Human.grip) exactly on the point, with the
    elbow bent toward `pole` (a direction; default out, back and down). Arms not
    named keep their ArmPose. Bone lengths are the base class's."""

    def __init__(self, *args, reach: dict | None = None, pole: dict | None = None,
                 **kwargs):
        self._reach = {k: Vector(v) for k, v in (reach or {}).items()}
        self._pole = {k: Vector(v) for k, v in (pole or {}).items()}
        super().__init__(*args, **kwargs)

    def _arm_points(self, side):
        if side not in self._reach:
            return super()._arm_points(side)
        s, h = figures.SIDES[side], self.h
        shoulder = self.lean((s * self.shoulder_x, 0.0, self.shoulder_z))
        target = self._reach[side]
        a, b = 0.172 * h, (0.145 + 0.045) * h        # shoulder->elbow, elbow->grip
        axis = target - shoulder
        d = min(axis.length, (a + b) * 0.995)
        axis.normalize()
        pole = self._pole.get(side, Vector((s * 1.0, 0.35, -0.6)))
        perp = (pole - axis * pole.dot(axis)).normalized()
        x = (a * a - b * b + d * d) / (2.0 * d)
        y = math.sqrt(max(0.0, a * a - x * x))
        elbow = shoulder + axis * x + perp * y
        fore = (shoulder + axis * d - elbow).normalized()
        wrist = elbow + fore * (0.145 * h)
        return shoulder, elbow, wrist, wrist + fore * (0.090 * h), fore


def _ring_path(centre: Vector, u: Vector, v: Vector, ru: float, rv: float,
               n: int) -> list[tuple]:
    return [tuple(centre + u * (math.cos(2 * math.pi * k / n) * ru)
                  + v * (math.sin(2 * math.pi * k / n) * rv)) for k in range(n)]


# --------------------------------------------------------------------------------
# Castle Crossbowman (ranged) — grey rounded mail head on red shoulders, the
# crossbow's prod a horizontal bar across the hips: a T at waist height.
# --------------------------------------------------------------------------------

def _cervelliere(fig: Human, top: float) -> list[Part]:
    """Hemispherical iron skull cap, 0.22 m across and 0.10 m tall, over the coif.
    Its own bone under Head (it detaches on a head hit)."""
    base = fig.lean((0.0, 0.002 * fig.h, top - 0.10))
    fig.add_bone("SkullCap", base, base + Vector((0, 0, 0.10)), "Head")
    r = 0.112
    profile = [(0.0, 0.100), (0.036, 0.096), (0.066, 0.084), (0.090, 0.064),
               (0.105, 0.038), (0.111, 0.012), (r + 0.003, 0.002), (r - 0.004, -0.006)]
    return [Part("lathe", tuple(base), (1, 1, 1), mat="cap_iron", bone="SkullCap",
                 segments=18, extras={"profile": profile, "rigid": True, "smooth": True})]


def _mail_mantle(fig: Human, jack_pad: float) -> Part:
    """The coif's mantle: mail falling from under the chin over the shoulders to
    about 1.38 m, a flared collar-cape over the jack. One closed loft whose last
    ring tucks back into the jack so the cap is hidden."""
    h = fig.h
    n = 28
    rows = [   # (z frac, half-width frac, half-depth frac, y shift frac)
        (0.874, 0.050, 0.052, 0.004),
        (0.858, 0.064, 0.058, 0.004),
        (0.842, 0.090, 0.064, 0.005),
        (0.826, 0.114, 0.070, 0.004),
        (0.810, 0.130, 0.075, 0.002),
        (0.795, 0.136, 0.078, 0.000),
        (0.790, 0.108, 0.066, 0.000),   # tuck into the jack
    ]
    rings = []
    for zf, hw, hd, dy in rows:
        c = fig.lean((0.0, dy * h, zf * h))
        ring = []
        for j in range(n):
            a = 2 * math.pi * j / n
            ca, sa = math.cos(a), math.sin(a)
            e = 2.0 / 2.6
            ring.append((c.x + math.copysign(abs(ca) ** e, ca) * hw * h,
                         c.y + math.copysign(abs(sa) ** e, sa) * hd * h, c.z))
        rings.append(ring)
    return Part("loft", (0, 0, 0), (1, 1, 1), mat="mail_steel", bone="Chest", extras={
        "rings": rings, "smooth": True, "bevel": False,
        "bones": ["Chest", "Neck", "Head", "Shoulder.L", "Shoulder.R",
                  "UpperArm.L", "UpperArm.R"]})


def _diamond_quilting(fig: Human, pad: float, z0: float, z1: float,
                      lines: int) -> list[Part]:
    """45° diamond quilting on the jack: two families of helical seams wound in
    opposite directions over the torso surface, each a thin raised cord in the
    jack's own red, so the lattice reads by shading, not by a second colour."""
    parts = []
    steps = 9
    for sense in (1.0, -1.0):
        for k in range(lines):
            a0 = 360.0 * k / lines
            path = []
            for i in range(steps):
                z = z0 + (z1 - z0) * i / (steps - 1)
                hw, hd, _dy = fig.torso_dims(z)
                r = (hw + hd) / 2 + pad
                ang = a0 + sense * math.degrees((z - z0) / r)
                path.append(tuple(fig.surface(z, ang, pad=pad + 0.002)))
            parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="kermes_jack",
                              bone="Spine", segments=3, extras={
                                  "path": path, "section": (0.004, 0.004),
                                  "smooth": True, "bevel": False,
                                  "bones": ["Hips", "Spine", "Chest"]}))
    return parts


def _crossbow(fig: Human, butt: Vector, d: Vector, t_nut: float) -> list[Part]:
    """Stirrup crossbow c. 1250: oak tiller 0.72 m, composite horn prod 0.76 m with
    four leather wraps, hemp string spanned to a horn nut, iron tickler 0.20 m
    under the tiller, iron stirrup 0.16 m at the front. Prop on Hand.R; the string
    is its own bone (drawn/slack)."""
    L = 0.72
    front = butt + d * L
    side = Vector((1.0, 0.0, 0.0))
    up = side.cross(d).normalized() * -1.0          # tiller's top (perpendicular, upward)
    if up.z < 0:
        up = -up
    fig.prop_bone("Crossbow", "R", head=tuple(butt), tail=tuple(front))
    prop = {"prop": True}
    tilt = math.degrees(math.atan2(-d.z, -d.y))     # box along Y, pitched down
    parts = []
    # Tiller: a slimmer fore-stock and a deeper butt.
    mid = butt + d * (L * 0.62)
    parts.append(Part("box", tuple(mid - up * 0.004), (0.040, L * 0.76, 0.046),
                      mat="oak_stock", bone="Crossbow", rot=(tilt, 0, 0), extras=dict(prop)))
    rear = butt + d * 0.13
    parts.append(Part("box", tuple(rear - up * 0.010), (0.046, 0.26, 0.066),
                      mat="oak_stock", bone="Crossbow", rot=(tilt, 0, 0), extras=dict(prop)))
    # Nut (horn, 3 cm) set in the top of the tiller, and the tickler under it.
    nut = butt + d * t_nut + up * 0.024
    parts.append(Part("cyl", tuple(nut), (0.030, 0.030, 0.036), mat="horn_prod",
                      bone="Crossbow", rot=(0, 90, 0), segments=8,
                      extras={**prop, "bevel": False, "smooth": True}))
    t0 = butt + d * (t_nut + 0.02) - up * 0.030
    t1 = butt + d * max(0.0, t_nut - 0.17) - up * 0.070
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="cap_iron", bone="Crossbow",
                      segments=4, extras={"path": [tuple(t0), tuple((t0 + t1) / 2 - up * 0.01),
                                                   tuple(t1)],
                                          "section": (0.006, 0.005), **prop,
                                          "bevel": False, "smooth": True}))
    # Prod: across X at the front, tips swept back toward the shooter (braced).
    pc = butt + d * (L - 0.07) + up * 0.012
    span = 0.38
    path, secs = [], []
    for i in range(11):
        u = -1.0 + 2.0 * i / 10
        back = 0.075 * abs(u) ** 1.7
        path.append(tuple(pc + side * (u * span) - d * back))
        w = 0.024 - 0.012 * abs(u)
        secs.append((w * 0.72, w))
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="horn_prod", bone="Crossbow",
                      segments=8, extras={"path": path, "sections": secs, "up": tuple(up),
                                          "power": 2.4, "smooth": True, "bevel": False,
                                          **prop}))
    for u in (-0.66, -0.30, 0.30, 0.66):            # leather wraps at four points
        back = 0.075 * abs(u) ** 1.7
        at = pc + side * (u * span) - d * back
        w = 0.024 - 0.012 * abs(u)
        parts.append(Part("cyl", tuple(at), (w * 1.9, w * 1.6, 0.030), mat="leather",
                          bone="Crossbow", rot=(0, 90, 0), segments=8,
                          extras={**prop, "bevel": False, "smooth": True}))
    # Spanned string: tip -> nut -> tip, on its own bone.
    tips = [pc + side * (s * span) - d * 0.075 for s in (-1.0, 1.0)]
    fig.add_bone("CrossbowString", tuple(nut + up * 0.004), tuple(pc), "Crossbow")
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="leather", bone="CrossbowString",
                      segments=4, extras={
                          "path": [tuple(tips[0]), tuple(nut + up * 0.006), tuple(tips[1])],
                          "section": (0.003, 0.003), **prop, "bevel": False,
                          "smooth": True}))
    # Stirrup: a D of forged iron continuing the tiller's line past the nose.
    sp = [front + side * 0.028, front + side * 0.058 + d * 0.06,
          front + side * 0.050 + d * 0.13, front + side * 0.020 + d * 0.145,
          front - side * 0.020 + d * 0.145, front - side * 0.050 + d * 0.13,
          front - side * 0.058 + d * 0.06, front - side * 0.028]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="cap_iron", bone="Crossbow",
                      segments=5, extras={"path": [tuple(p) for p in sp],
                                          "section": (0.008, 0.008), **prop,
                                          "bevel": False, "smooth": True}))
    return parts


def _bolt_quiver(fig: Human, pad: float) -> list[Part]:
    """Stiff leather box 0.10 × 0.08 × 0.36 m at the right hip, two tooled bands,
    twelve 0.35 m bolts standing fletch-up. On a Quiver bone under Hips."""
    at = fig.surface(fig.belt_z - 0.14, 185.0, pad=pad + 0.06)
    at.y += 0.01
    top = at + Vector((0, 0, 0.18))
    fig.add_bone("Quiver", top, at - Vector((0, 0, 0.18)), "Hips")
    rig = {"rigid": True}
    parts = [Part("box", tuple(at), (0.08, 0.10, 0.36), mat="leather", bone="Quiver",
                  rot=(0, -6, 0), extras=dict(rig))]
    for dz in (0.11, -0.12):
        parts.append(Part("box", tuple(at + Vector((0.0, 0, dz))), (0.088, 0.108, 0.022),
                          mat="leather", bone="Quiver", rot=(0, -6, 0),
                          extras={**rig, "bevel": False}))
    # Bolts: 4 cm of shaft and the wooden fletching showing above the mouth.
    for k in range(12):
        gx = (k % 3 - 1) * 0.022
        gy = (k // 3 - 1.5) * 0.022
        base = top + Vector((gx - 0.018, gy, 0.0))
        parts.append(Part("cyl", tuple(base + Vector((0, 0, 0.02))), (0.009, 0.009, 0.06),
                          mat="oak_stock", bone="Quiver", segments=4,
                          extras={**rig, "bevel": False}))
        parts.append(Part("box", tuple(base + Vector((0, 0, 0.055))), (0.020, 0.003, 0.040),
                          mat="horn_prod", bone="Quiver", rot=(0, 0, 45.0 + 30 * k),
                          extras={**rig, "bevel": False}))
    return parts


def _belt_hook(fig: Human, pad: float) -> list[Part]:
    """Spanning belt-hook: a double iron claw 8 cm on a 7 cm strap, front-right."""
    top = fig.surface(fig.belt_z - 0.015, -118.0, pad=pad + 0.012)
    fig.add_bone("BeltHook", top, top - Vector((0, 0, 0.15)), "Hips")
    parts = [Part("box", tuple(top - Vector((0, 0, 0.035))), (0.028, 0.008, 0.07),
                  mat="leather", bone="BeltHook", rot=(0, 0, 28.0),
                  extras={"rigid": True})]
    bar = top - Vector((0, 0.004, 0.072))
    parts.append(Part("box", tuple(bar), (0.050, 0.010, 0.012), mat="cap_iron",
                      bone="BeltHook", rot=(0, 0, 28.0), extras={"rigid": True}))
    for s in (-1.0, 1.0):
        base = bar + Vector((s * 0.018, s * 0.009, 0.0))
        claw = [base, base - Vector((0, 0.0, 0.04)), base - Vector((0, 0.022, 0.07)),
                base - Vector((0, 0.030, 0.050))]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="cap_iron", bone="BeltHook",
                          segments=4, extras={"path": [tuple(p) for p in claw],
                                              "section": (0.005, 0.005), "rigid": True,
                                              "smooth": True, "bevel": False}))
    return parts


def castle_crossbowman(entry: Entry):
    # 1.78 m to the top of the skull cap; the cap (0.10 m) sits on the coif, so the
    # skull crown (figure height) is ~1.73 m. Lean: bulk 0.95, shoulders 0.46 m.
    h = 1.73
    # Carry pose: both fists on the tiller at the waist, nose down, stirrup
    # forward and low. The JSON asks for 30° down, but a 0.72 m tiller plus a
    # 0.15 m stirrup at 30° reaches 0.9 m in front of the toes and fails the
    # rig's bbox-centred check (|centre| <= 0.25 x depth + 5 cm); 56° is the
    # shallowest that passes, and it puts the stirrup at shin/knee height.
    # Fists are placed on the tiller by IK.
    d = Vector((0.0, -math.cos(math.radians(56)), -math.sin(math.radians(56))))
    butt = Vector((0.0, -0.12, 1.15))
    t_r, t_nut, t_l = 0.06, 0.17, 0.29
    grip_r = butt + d * t_r + Vector((0, 0, -0.012))
    grip_l = butt + d * t_l + Vector((0, 0, -0.012))
    fig = _ReachHuman(height=h, bulk=0.95, shoulders=0.46,
                      reach={"R": grip_r, "L": grip_l},
                      pole={"R": Vector((-1.0, 0.5, -0.5)), "L": Vector((1.0, 0.5, -0.5))})
    pad = 0.018   # padded jack
    parts = [fig.torso_part("kermes_jack", pad=pad, hem=0.78, hem_flare=1.18,
                            collar=0.05, quilt=0.0, segments=32)]
    for side in ("L", "R"):
        parts.append(fig.arm_part(side, "kermes_jack", pad=pad * 0.8, quilt_rings=13))
        parts += fig.hand_part(side, "skin")
        parts.append(fig.leg_part(side, "hose_wool", paint=[
            {"mat": "hose_mud", "min": (-1, -1, -1), "max": (1, 1, 0.30)}]))
        parts.append(fig.foot_part(side, "leather", length=0.26, point=0.25))
        # Ankle boot shaft to 0.17 m, a button on the outer side.
        ankle = fig.joint(f"ankle.{side}")
        s = figures.SIDES[side]
        parts.append(Part("cyl", (ankle.x, ankle.y + 0.006, 0.105), (0.088, 0.096, 0.13),
                          mat="leather", bone=f"LowerLeg.{side}", segments=12, taper=0.94,
                          extras={"smooth": True, "bevel": False,
                                  "bones": [f"LowerLeg.{side}", f"Foot.{side}"]}))
        parts.append(Part("sphere", (ankle.x + s * 0.046, ankle.y, 0.13), (0.014, 0.014, 0.014),
                          mat="cap_iron", bone=f"LowerLeg.{side}", segments=6, rings=4,
                          extras={"rigid": True, "bevel": False}))
    # Mail coif: the hood loft in mail with the face open, the mantle over it.
    parts += fig.head_part("mail_steel", face="skin", hood=True, features="leather")
    parts.append(_mail_mantle(fig, pad))
    # Ventail thong laced across the chin.
    chin = [fig.lean((sx * 0.034 * h, -0.052 * h + abs(sx) * 0.012 * h,
                      0.874 * h - (1 - abs(sx)) * 0.006 * h)) for sx in (-1, -0.5, 0, 0.5, 1)]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="leather", bone="Head", segments=4,
                      extras={"path": [tuple(p) for p in chin], "section": (0.004, 0.004),
                              "rigid": True, "smooth": True, "bevel": False}))
    parts += _cervelliere(fig, entry.height_m)

    parts += _diamond_quilting(fig, pad, 0.80, 1.34, lines=16)
    parts.append(fig.band(fig.belt_z, "leather", height=0.04, pad=0.008, torso_pad=pad))
    buckle = fig.surface(fig.belt_z, -90.0, pad=pad + 0.012)
    parts.append(Part("torus", tuple(buckle), (0.048, 0.048, 0.042), mat="cap_iron",
                      bone="Hips", rot=(90, 0, 0), segments=4, rings=4, minor=0.2,
                      extras={"rigid": True, "bevel": False}))
    parts += _belt_hook(fig, pad)
    parts += _bolt_quiver(fig, pad)
    parts += _crossbow(fig, butt, d, t_nut)

    # Review pose: aim_hold's first beat. Both upper arms rotate by the same
    # world-X angle about pivots that differ only in X, so the two fists stay on
    # the tiller as it comes up; the right knee lifts as for the stirrup.
    pose = {
        "UpperArm.L": (-28.0, 0.0, 0.0), "UpperArm.R": (-28.0, 0.0, 0.0),
        "Head": (0.0, 0.0, 30.0), "Spine": (0.0, 0.0, -8.0),
        "UpperLeg.R": (-35.0, 0.0, 0.0), "LowerLeg.R": (55.0, 0.0, 0.0),
    }
    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(pose),
        family_overrides={
            # 8 mm riveted rings: the bake has no normal map, so ring rows are
            # albedo bands (ridges) and rust blooms in patches.
            "mail_steel": {"ridges": (0.010, 0.45), "wear_to": "#5A4638",
                           "wear_amount": 0.22, "rough": 0.5},
            # Sun-faded toward pink in patches (strongest read on the shoulders).
            "kermes_jack": {"wear_to": "#94463C", "wear_amount": 0.30, "grain": 0.28},
            # Banded horn laminate.
            "horn_prod": {"wear_to": "#7A6A50", "wear_amount": 0.45},
            "oak_stock": {"wear_to": "#3A2A1C", "wear_amount": 0.2},
        },
        extra_families={
            "cap_iron": {"name": "Cap iron", "base": "#7C8288", "rough": 0.38, "metal": 1.0,
                         "wear_to": "#4A4C50", "wear_amount": 0.30,
                         "notes": "The skull cap, stirrup, tickler, belt hook and buckle "
                                  "are plain iron (JSON build bullets); the JSON lists "
                                  "only 'Mail steel', whose ring banding must not land "
                                  "on plate. Same hex, no ridges."},
            "hose_mud": {"name": "Hose wool, muddied", "base": "#4A3F30", "rough": 0.95,
                         "notes": "Brown hose darkened to the shin (JSON: 'mud to the "
                                  "shin')."},
        },
        notes=[
            "Crossbow (0.72 m tiller, 0.76 m prod) is a prop on Hand.R; the string is "
            "its own bone (CrossbowString) for drawn/slack. Both fists are placed on "
            "the tiller by two-bone IK in the blueprint (_ReachHuman).",
            "SkullCap (detachable), Quiver (child of Hips) and BeltHook are their own "
            "bones.",
            "Not built: coif_mantle x2 spring bones, the stirrup-foot IK target, "
            "ring-pattern normal map (mail rows are albedo bands), jack damage states, "
            "the purse.",
        ])


BLUEPRINTS = {
    "lantern-warden": lantern_warden,
    "alaunt-hound": alaunt_hound,
    "castle-crossbowman": castle_crossbowman,
}
