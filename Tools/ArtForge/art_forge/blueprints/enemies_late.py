"""Late Medieval enemies (docs/art/data/late.json, enemies).

The Burgundian household: a sallet halberdier on patrol, a handgunner, a Gothic
man-at-arms in white harness and a pavisier carrying his gunner's pavise. All four
are figures.Human; see enemies_high.py for the reference pattern and the README's
"Enemies" section for the API.

Shared helpers at the top place plate pieces between landmarks (`_seg`), lay cloth
panels over the torso front or back (`_panel`, `_torso_y`) and lay strips on those
panels (`_strip`). Everything stands on z = 0, faces -Y, and `.L` is +X.
"""

from __future__ import annotations

import math

from mathutils import Vector

from .. import figures
from ..figures import ArmPose, Human
from ..kit import Part
from ..spec import Entry
from . import blueprint


# --------------------------------------------------------------------------------
# Shared helpers
# --------------------------------------------------------------------------------

def _rot_to(direction) -> tuple:
    """XYZ Euler degrees turning local +Z onto `direction` (not parallel to Y)."""
    q = Vector(direction).normalized().to_track_quat("Z", "Y")
    return tuple(math.degrees(a) for a in q.to_euler())


def _seg(kind: str, a, b, radius: float, mat: str, bone: str, segments: int = 10,
         taper: float = 1.0, rx: float | None = None, extras: dict | None = None) -> Part:
    """A cylinder/cone from point a to point b (plate tubes, hafts, lames)."""
    a, b = Vector(a), Vector(b)
    r2 = 2.0 * radius
    return Part(kind, tuple((a + b) / 2), (2.0 * rx if rx else r2, r2, (b - a).length),
                mat=mat, bone=bone, rot=_rot_to(b - a), segments=segments, taper=taper,
                extras=dict(extras or {}))


def _dome(base, axis, radius: float, height: float, mat: str, bone: str,
          segments: int = 12, squash: float = 1.0, extras: dict | None = None) -> Part:
    """A solid dome (lathe) standing on `base`, its pole along `axis`."""
    profile = [(radius, 0.0), (radius * 0.97, height * 0.30), (radius * 0.86, height * 0.60),
               (radius * 0.60, height * 0.86), (radius * 0.28, height * 0.98), (0.0, height)]
    return Part("lathe", tuple(base), (1.0, squash, 1.0), mat=mat, bone=bone,
                rot=_rot_to(axis), segments=segments,
                extras={"profile": profile, "smooth": True, **(extras or {})})


def _torso_y(fig: Human, x: float, z: float, pad: float, back: bool = False,
             floor: float | None = None) -> tuple[float, float]:
    """(x, y) on the torso surface (plus pad) at height z, for a given x. The x is
    pulled in to 97 % of the half-width so a panel wider than the body wraps round
    its side instead of sticking out. `floor` holds the dims below that z (a skirt
    hangs straight from the hips)."""
    zz = max(z, floor) if floor is not None else z
    hw, hd, dy = fig.torso_dims(zz)
    zf = zz / fig.h
    k = 1.08 if (0.66 < zf < 0.8 and not back) else 1.0
    a, b = hw * k + pad, hd * k + pad
    # Saturate smoothly (never clamp): clamped columns would stack on one point,
    # and duplicate vertices make Blender's heat weighting fail for the whole mesh.
    u = abs(x) / a
    if u > 0.8:
        u = 0.8 + 0.17 * math.tanh((u - 0.8) / 0.17)
    xe = math.copysign(u * a, x)
    e = 2.0 / 2.4
    ca = (abs(xe) / a) ** (1.0 / e)
    sa = math.sqrt(max(0.0, 1.0 - ca * ca))
    y = sa ** e * b
    return xe, dy + (y if back else -y)


def _panel(fig: Human, rows: list[tuple[float, float]], pad: float, thick: float,
           back: bool = False, cols: int = 9, dags: list[float] | None = None,
           floor: float | None = None) -> list[list[tuple]]:
    """Loft rings for a cloth panel over the torso front (or back).

    rows = [(z, half_width), ...] bottom to top. Each ring is the outer arc then the
    inner arc back, so the panel is one closed shell `thick` metres deep standing
    `pad` off the torso. `dags` shifts the bottom ring's columns in z (a ragged hem).
    """
    rings = []
    for i, (z, hw) in enumerate(rows):
        outer, inner = [], []
        for c in range(cols):
            x = -hw + 2.0 * hw * c / (cols - 1)
            dz = dags[c] if (dags and i == 0) else 0.0
            xo, yo = _torso_y(fig, x, z, pad + thick, back, floor)
            xi, yi = _torso_y(fig, x, z, pad, back, floor)
            outer.append((xo, yo, z + dz))
            inner.append((xi, yi, z + dz))
        rings.append(outer + list(reversed(inner)))
    return rings


def _strip(fig: Human, pts2d: list[tuple[float, float]], pad: float, widths: list[float],
           mat: str, bone: str, back: bool = False, thick: float = 0.003,
           floor: float | None = None, extras: dict | None = None) -> Part:
    """A flat strip laid on the torso through (x, z) points: a saltire bar, a sash."""
    path = []
    for x, z in pts2d:
        xs, ys = _torso_y(fig, x, z, pad, back, floor)
        path.append((xs, ys, z))
    return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, segments=4, extras={
        "path": path, "sections": [(thick, w) for w in widths],
        "up": (0.0, 1.0 if back else -1.0, 0.0), "power": 4.0, "bevel": False,
        "smooth": True, **(extras or {})})


def _arm_axis(fig: Human, side: str):
    shoulder, elbow = fig.joint(f"shoulder.{side}"), fig.joint(f"elbow.{side}")
    wrist = fig.joint(f"wrist.{side}")
    return shoulder, elbow, wrist


def _lerp(a, b, t):
    return Vector(a).lerp(Vector(b), t)


# --------------------------------------------------------------------------------
# Sallet Halberdier (patrol) — tail, X, blade.
# --------------------------------------------------------------------------------

def _sallet(fig: Human, mat: str, rivet: str, crown: float, rim_front: float,
            rim_side: float, rim_back: float, back_reach: float, slit: float | None,
            front_reach: float = 0.132, half_w: float = 0.126, n: int = 20,
            bone: str = "Sallet") -> list[Part]:
    """A one-piece sallet: a solid dome loft whose rim dips from the brow to a tail
    that sweeps out behind the neck. Rings run rim -> crown pole. With `slit` (the
    z of the sight-slit) three rings pinch the brow inward there; a low ridge rises
    along the centre line. Rigid on its own bone under Head (it comes off)."""
    cy = 0.004 * fig.h
    base = Vector((0.0, cy, rim_side))
    fig.add_bone(bone, base, (0.0, cy, crown), "Head")

    def rim_z(sa):
        return rim_side + (-sa) * (rim_front - rim_side) if sa < 0 else \
            rim_side - sa * (rim_side - rim_back)

    def point(j, theta, inset=0.0, ridge=0.0):
        a = 2.0 * math.pi * j / n
        ca, sa = math.cos(a), math.sin(a)
        ct, st = math.cos(theta), math.sin(theta)
        ry = front_reach if sa < 0 else 0.118 + (back_reach - 0.118) * (1.0 - st) ** 2.4
        zr = rim_z(sa)
        r = 1.0 - (inset if (sa < -0.8) else 0.0)
        x = ca * half_w * ct * r
        y = cy + sa * ry * ct * r
        z = zr + (crown - zr) * st
        if ridge and abs(ca) < 0.05:
            x, y = x, y + math.copysign(ridge, sa) * ct
            z += ridge * st
        return (x, y, z)

    thetas = [(0.0, 0.0)]
    if slit is not None:
        for dz, inset in ((-0.007, 0.0), (0.0, 0.10), (0.007, 0.0)):
            st = (slit + dz - rim_front) / (crown - rim_front)
            thetas.append((math.asin(max(0.0, min(1.0, st))), inset))
    for deg in (22, 38, 54, 68, 80):
        thetas.append((math.radians(deg), 0.0))
    rings = []
    for theta, inset in thetas:
        ridge = 0.008 if theta > 0.2 else 0.0
        rings.append([point(j, theta, inset, ridge) for j in range(n)])
    rings.append([(0.0, cy, crown)])
    parts = [Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, extras={
        "rings": rings, "smooth": False, "rigid": True, "bevel": False})]
    for k in range(12):   # lining rivets round the rim
        j = (k + 0.5) * n / 12
        p = Vector(point(j, math.radians(6.0)))
        out = Vector((p.x, p.y - cy, 0.0)).normalized() * 0.004
        parts.append(Part("ico", tuple(p + out), (0.011, 0.011, 0.011), mat=rivet, bone=bone,
                          subdivisions=1, extras={"rigid": True, "bevel": False}))
    return parts


def _halberd(fig: Human, g: Vector) -> list[Part]:
    """2.08 m overall: ash shaft 3.2 cm to 1.78 m, iron shoe 5 cm, a 0.30 m head
    (square top spike, 0.20 x 0.16 m axe blade outboard, 8 cm back fluke), two
    0.18 m langets. Upright through the right fist, grounded by the right foot."""
    x, y = g.x, g.y
    overall, head = 2.08, 0.30
    socket_z = overall - head
    fig.prop_bone("Halberd", "R", head=(x, y, g.z), tail=(x, y, overall))
    prop = {"prop": True}
    parts = [
        Part("cyl", (x, y, (0.05 + socket_z) / 2), (0.032, 0.032, socket_z - 0.05), mat="ash",
             bone="Halberd", segments=8, extras={**prop, "smooth": True, "bevel": False,
                                                 "paint": []}),
        Part("cyl", (x, y, 0.025), (0.036, 0.036, 0.05), mat="plate_steel", bone="Halberd",
             segments=8, taper=0.8, extras=dict(prop)),
        Part("cyl", (x, y, socket_z + 0.04), (0.040, 0.040, 0.10), mat="plate_steel",
             bone="Halberd", segments=8, taper=0.85, extras=dict(prop)),
        # square top spike
        Part("cone", (x, y, overall - 0.075), (0.030, 0.030, 0.15), mat="plate_steel",
             bone="Halberd", segments=4, rot=(0, 0, 45.0), extras=dict(prop)),
    ]
    for side in (1.0, -1.0):   # langets down the shaft, front and back
        parts.append(Part("box", (x, y + side * 0.017, socket_z - 0.07), (0.012, 0.004, 0.18),
                          mat="plate_steel", bone="Halberd", extras=dict(prop)))
    # Axe blade in the XZ plane, edge outboard (-X, away from the body), 0.16 m
    # deep and 0.20 m tall with a concave edge; the back fluke points +X. Outline
    # (u, v): u = metres outboard from the shaft axis, v = up from the socket.
    blade = [(0.012, 0.035), (0.075, 0.045), (0.150, 0.000), (0.172, 0.060),
             (0.160, 0.130), (0.172, 0.215), (0.090, 0.170), (0.012, 0.185)]
    parts.append(Part("prism", (x, y, socket_z + 0.02), (1.0, 1.0, 0.008), mat="plate_steel",
                      bone="Halberd", rot=(90.0, 0.0, 180.0),
                      extras={"outline": blade, **prop}))
    fluke = [(-0.012, 0.080), (-0.088, 0.040), (-0.030, 0.125), (-0.012, 0.140)]
    parts.append(Part("prism", (x, y, socket_z + 0.02), (1.0, 1.0, 0.010), mat="plate_steel",
                      bone="Halberd", rot=(90.0, 0.0, 180.0),
                      extras={"outline": fluke, **prop}))
    return parts


def sallet_halberdier(entry: Entry):
    # Eyes 1.65 m -> stature 1.763 m; the sallet brings the crown to 1.82 m.
    # 0.50 m shoulders over the spaulders; the right fist holds the grounded halberd.
    fig = Human(height=1.763, bulk=1.05, shoulders=0.46,
                arm_r=ArmPose(spread=17.0, swing=6.0, elbow=38.0),
                arm_l=ArmPose(spread=12.0, swing=2.0, elbow=14.0))
    h = fig.h
    bpad = 0.016          # brigandine over the body
    parts = [fig.torso_part("brigandine", pad=bpad, hem=0.80, hem_flare=1.18,
                            collar=0.02, segments=24)]
    for side in ("L", "R"):
        parts.append(fig.arm_part(side, "mail", pad=0.006))
        parts += fig.hand_part(side, "russet_leather")
        parts.append(fig.leg_part(side, "hose"))
        parts.append(fig.foot_part(side, "russet_leather", length=0.28, point=0.0))
    parts += fig.head_part("hose")   # arming cap, hidden under sallet and bevor

    # Brigandine rivets: rows of tinned heads, visible at the tabard's open sides.
    for side in (1.0, -1.0):
        for z in [0.88 + 0.075 * k for k in range(7)]:
            for da in (-14.0, 8.0):
                ang = (0.0 if side > 0 else 180.0) + side * da
                p = fig.surface(z, ang, pad=bpad + 0.003)
                parts.append(Part("ico", tuple(p), (0.011, 0.011, 0.011), mat="mail",
                                  bone="Spine", subdivisions=1,
                                  extras={"bevel": False, "bones": ["Hips", "Spine", "Chest"]}))

    # Livery tabard: front and back panels, 0.40 m wide, hem 0.82 m, dagged.
    tpad = bpad + 0.010
    top = 0.808 * h
    rows = [(0.82, 0.20), (0.90, 0.20), (1.00, 0.20), (1.10, 0.20), (1.20, 0.20),
            (1.30, 0.20), (1.38, 0.19), (top, 0.16)]
    dags = [0.0, -0.025, 0.004, -0.018, 0.0, -0.028, 0.006, -0.015, 0.0]
    tab_rules = {"bones": ["Hips", "Spine", "Chest", "UpperLeg.L", "UpperLeg.R"],
                 "skirt": {"top": fig.hip_z, "bottom": 0.80, "strength": 0.8, "split": 0.08},
                 "bevel": False, "smooth": True}
    grime = {"mat": "livery_grime", "min": (-1, -1, 0.70), "max": (1, 1, 0.905)}
    for back in (False, True):
        parts.append(Part("loft", (0, 0, 0), (1, 1, 1), mat="livery_white", bone="Spine",
                          extras={"rings": _panel(fig, rows, tpad, 0.006, back, dags=dags,
                                                  floor=fig.hip_z),
                                  "paint": [grime], **tab_rules}))
        # The ragged saltire, 7 cm bars corner to corner, with knotted stubs.
        spad = tpad + 0.006
        for sx in (1.0, -1.0):
            pts = [(sx * (-0.17 + 0.34 * t), 1.37 - (1.37 - 0.86) * t)
                   for t in [k / 7 for k in range(8)]]
            widths = [0.030, 0.038, 0.031, 0.040, 0.030, 0.037, 0.032, 0.034]
            parts.append(_strip(fig, pts, spad, widths, "livery_red", "Spine", back,
                                floor=fig.hip_z, extras={k: v for k, v in tab_rules.items()
                                                         if k != "smooth"}))
            # Knotted stubs: short lopped branches off alternate edges of the bar.
            # (Flattened icospheres here broke the whole heat solve; strips don't.)
            dl = math.hypot(0.34, 0.51)
            px, pz = 0.51 / dl, 0.34 * sx / dl
            for t, side in ((0.15, 1.0), (0.32, -1.0), (0.70, 1.0), (0.86, -1.0)):   # clear of the crossing
                x, z = sx * (-0.17 + 0.34 * t), 1.37 - (1.37 - 0.86) * t
                stub = [(x + side * px * r, z + side * pz * r + 0.01 * (r > 0.04))
                        for r in (0.026, 0.045, 0.064)]
                parts.append(_strip(fig, stub, spad + 0.001, [0.013, 0.011, 0.006],
                                    "livery_red", "Spine", back, floor=fig.hip_z,
                                    extras={k: v for k, v in tab_rules.items()
                                            if k != "smooth"}))

    # Belt at 1.02 m over the tabard, iron frame buckle, baselard on the right hip.
    parts.append(fig.band(1.02, "russet_leather", height=0.04, pad=0.006,
                          torso_pad=tpad + 0.006))
    bk = Vector((0.0, _torso_y(fig, 0.0, 1.02, tpad + 0.018)[1], 1.02))
    parts.append(Part("torus", tuple(bk), (0.05, 0.05, 0.045), mat="plate_steel",
                      bone="Hips", rot=(90, 0, 0), segments=4, rings=4, minor=0.2,
                      extras={"rigid": True, "bevel": False}))
    hilt = fig.surface(1.02, -150.0, pad=tpad + 0.03)
    fig.add_bone("Baselard", hilt + Vector((0, 0, 0.08)), hilt - Vector((0, 0, 0.22)), "Hips")
    parts.append(_seg("cyl", hilt + Vector((0.01, 0, -0.01)), hilt + Vector((0.03, -0.01, -0.24)),
                      0.018, "russet_leather", "Baselard", segments=6, taper=0.5,
                      extras={"rigid": True}))
    parts.append(Part("box", tuple(hilt + Vector((0, 0, 0.005))), (0.08, 0.02, 0.018),
                      mat="plate_steel", bone="Baselard", rot=(0, 5, 0),
                      extras={"rigid": True}))
    parts.append(_seg("cyl", hilt + Vector((0, 0, 0.01)), hilt + Vector((-0.003, 0, 0.09)),
                      0.011, "hose", "Baselard", segments=6, extras={"rigid": True}))
    parts.append(Part("box", tuple(hilt + Vector((-0.003, 0, 0.095))), (0.06, 0.02, 0.016),
                      mat="plate_steel", bone="Baselard", extras={"rigid": True}))

    # Sallet (crown 1.82 m, tail down to 1.55 m) and bevor (chin to mouth).
    parts += _sallet(fig, "plate_steel", "mail", crown=1.82, rim_front=1.605,
                     rim_side=1.585, rim_back=1.55, back_reach=0.29, slit=1.66)
    fig.add_bone("SalletTail", (0.0, 0.12, 1.60), (0.0, 0.28, 1.56), "Sallet")
    bev = []
    n_arc = 9
    for z, rx, ry, dy in ((1.42, 0.125, 0.130, 0.010), (1.455, 0.110, 0.112, 0.004),
                          (1.50, 0.098, 0.118, -0.004), (1.56, 0.106, 0.128, -0.004),
                          (1.612, 0.112, 0.130, -0.002)):
        outer, inner = [], []
        for k in range(n_arc):
            a = math.radians(-180.0 + 18.0 + (180.0 - 36.0) * k / (n_arc - 1))
            outer.append((math.cos(a) * rx, dy + math.sin(a) * ry, z))
            inner.append((math.cos(a) * (rx - 0.010), dy + math.sin(a) * (ry - 0.010), z))
        bev.append(outer + list(reversed(inner)))
    parts.append(Part("loft", (0, 0, 0), (1, 1, 1), mat="plate_steel", bone="Neck", extras={
        "rings": bev, "bevel": False, "bones": ["Neck", "Head", "Chest"]}))
    for k in (-1, 0, 1):   # three rivets on the strap
        a = math.radians(-90.0 + 32.0 * k)
        parts.append(Part("ico", (math.cos(a) * 0.113, 0.0 + math.sin(a) * 0.125, 1.50),
                          (0.012, 0.012, 0.012), mat="mail", bone="Neck", subdivisions=1,
                          extras={"rigid": True, "bevel": False}))

    # Spaulders: a cap and two lames down each upper arm to ~1.36 m; couters.
    for side in ("L", "R"):
        shoulder, elbow, wrist = _arm_axis(fig, side)
        up = (shoulder - elbow).normalized()
        s = 1.0 if side == "L" else -1.0
        cap_base = shoulder - up * 0.035 + Vector((s * 0.008, 0, 0))
        parts.append(_dome(cap_base, up + Vector((s * 0.25, 0, 0)), 0.095, 0.090,
                           "plate_steel", f"UpperArm.{side}", segments=12,
                           extras={"rigid": True, "bevel": False}))
        for k, (t, r) in enumerate(((0.13, 0.086), (0.24, 0.076))):
            a = shoulder + (elbow - shoulder) * (t - 0.05)
            b = shoulder + (elbow - shoulder) * (t + 0.07)
            parts.append(_seg("cyl", b, a, r, "plate_steel", f"UpperArm.{side}",
                              segments=12, taper=1.08, extras={"rigid": True, "bevel": False,
                                                              "smooth": True}))
        parts.append(Part("sphere", tuple(elbow + Vector((s * 0.008, 0.012, 0))),
                          (0.10, 0.10, 0.10), mat="plate_steel", bone=f"UpperArm.{side}",
                          segments=10, rings=6, extras={"rigid": True, "bevel": False}))
    # the small pauldron plate on the left only, over the front of the shoulder
    sh = fig.joint("shoulder.L")
    parts.append(_seg("cyl", sh + Vector((0.01, -0.070, -0.02)), sh + Vector((0.012, -0.082, -0.03)),
                      0.055, "plate_steel", "UpperArm.L", segments=10, rx=0.050,
                      extras={"rigid": True}))

    # Poleyns at 0.52 m: a knee cop and a small outboard fan.
    for side in ("L", "R"):
        s = 1.0 if side == "L" else -1.0
        knee = fig.joint(f"knee.{side}")
        parts.append(Part("sphere", tuple(knee + Vector((0, -0.035, 0.02))), (0.12, 0.085, 0.12),
                          mat="plate_steel", bone=f"LowerLeg.{side}", segments=10, rings=6,
                          extras={"rigid": True, "bevel": False}))
        fan = [(0.0, -0.05), (0.05, -0.02), (0.06, 0.03), (0.0, 0.05)]
        parts.append(Part("prism", tuple(knee + Vector((s * 0.045, -0.005, 0.02))),
                          (s * 1.0, 1.0, 0.008), mat="plate_steel", bone=f"LowerLeg.{side}",
                          rot=(90.0, 0.0, s * 90.0 - 90.0 + 90.0),
                          extras={"outline": fan, "rigid": True}))
        # turned-down cuff on the ankle shoe
        ank = fig.joint(f"ankle.{side}")
        parts.append(Part("cyl", tuple(ank + Vector((0, 0.004, 0.028))), (0.092, 0.10, 0.035),
                          mat="russet_leather", bone=f"Foot.{side}", segments=10, taper=1.1,
                          extras={"rigid": True, "bevel": False, "smooth": True}))

    parts += _halberd(fig, fig.grip("R"))

    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(),
        family_overrides={
            "plate_steel": {"rough": 0.55, "wear_to": "#6A4A30", "wear_amount": 0.25},
            "mail": {"rough": 0.45},
        },
        extra_families={
            "russet_leather": {"name": "Russet leather", "base": "#5A3F2C", "rough": 0.65,
                               "notes": "Gloves, shoes, belt and sheath: the build bullets "
                                        "ask for russet leather but the JSON lists no "
                                        "leather family (hex from the concept's gloves)."},
            "livery_grime": {"name": "Livery white, grubby hem", "base": "#A69C86",
                             "rough": 0.9,
                             "notes": "JSON: 'the tabard hem grubby to 0.3 m up'."},
        },
        notes=[
            "Halberd (2.08 m) is a prop bone on Hand.R, left out of the 1.82 m height.",
            "Sallet is its own bone under Head (detachable); SalletTail is a bone for "
            "the knock-back with no vertices of its own (the sallet is one rigid piece).",
            "Not built: tabard_front/back spring chains (the skirt rule hands the tabard "
            "hem to the thighs instead), the pierced trefoil in the axe blade, the "
            "sight-slit as a true opening (a pinched groove), rust bloom decals.",
        ])


BLUEPRINTS = {
    "sallet-halberdier": sallet_halberdier,
}
