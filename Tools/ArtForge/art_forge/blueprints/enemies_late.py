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

from mathutils import Matrix, Vector

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


# --------------------------------------------------------------------------------
# Handgunner (ranged) — the spark first, then the brim and the stick.
# --------------------------------------------------------------------------------

def _kettle(fig: Human, mat: str, crown: float, r_dome: float, dome_h: float,
            r_brim: float, droop: float, rivets: int, bone: str = "Hat") -> list[Part]:
    """A one-piece kettle hat: dome with a raised ridge, a brim turned down `droop`
    degrees ending in a rolled edge, lining rivets round the crown base. Rigid on
    its own bone under Head (it gets knocked off)."""
    cy = 0.004 * fig.h
    base = Vector((0.0, cy, crown - dome_h))
    drop = math.tan(math.radians(droop)) * (r_brim - r_dome)
    profile = [
        (0.0, dome_h - 0.003), (0.032, dome_h - 0.010), (0.064, dome_h - 0.030),
        (0.090, dome_h - 0.062), (r_dome - 0.004, 0.040), (r_dome, 0.004),
        ((r_dome + r_brim) / 2, -drop * 0.45), (r_brim - 0.008, -drop),
        (r_brim, -drop - 0.007), (r_brim - 0.006, -drop - 0.016),
        ((r_dome + r_brim) / 2, -drop * 0.5 - 0.011), (r_dome - 0.010, -0.010)]
    fig.add_bone(bone, base, base + Vector((0, 0, dome_h)), "Head")
    parts = [Part("lathe", tuple(base), (1.0, 1.04, 1.0), mat=mat, bone=bone, segments=22,
                  extras={"profile": profile, "rigid": True, "smooth": True})]
    ridge = [base + Vector((0.0, math.cos(a) * (r_dome - 0.010) * 1.04,
                            0.006 + math.sin(a) * (dome_h - 0.010)))
             for a in [math.radians(d) for d in range(20, 161, 20)]]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, segments=6,
                      extras={"path": [tuple(p) for p in ridge], "section": (0.008, 0.006),
                              "rigid": True, "smooth": True, "bevel": False}))
    for k in range(rivets):
        a = 2.0 * math.pi * (k + 0.5) / rivets
        at = base + Vector((math.cos(a) * (r_dome + 0.001), math.sin(a) * (r_dome + 0.001) * 1.04,
                            0.022))
        parts.append(Part("sphere", tuple(at), (0.012, 0.012, 0.012), mat=mat, bone=bone,
                          segments=6, rings=4, extras={"rigid": True, "bevel": False}))
    return parts


def _front_lacing(fig: Human, pad: float, mat: str, z0: float, z1: float,
                  crossings: int = 6) -> Part:
    """A leather thong zig-zagging up the front opening, laid on the surface."""
    pts = []
    steps = 2 * crossings
    for i in range(steps + 1):
        z = z0 + (z1 - z0) * i / steps
        side = -1.0 if i % 2 else 1.0
        pts.append(tuple(fig.surface(z, -90.0 + side * 6.0, pad=pad + 0.004)))
    return Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, bone="Chest", segments=4,
                extras={"path": pts, "section": (0.004, 0.006), "smooth": True,
                        "bevel": False, "bones": ["Spine", "Chest", "Hips"]})


def _badge(fig: Human, side: str, z: float, white: str, red: str) -> list[Part]:
    """A 0.10 x 0.12 m livery patch with a red saltire, stitched to the outside of
    the upper sleeve at height z. Rigid on UpperArm."""
    shoulder, elbow, _w = _arm_axis(fig, side)
    t = (shoulder.z - z) / max(1e-6, shoulder.z - elbow.z)
    c = shoulder.lerp(elbow, t)
    s = 1.0 if side == "L" else -1.0
    axis = (shoulder - elbow).normalized()
    out = Vector((s, 0.0, 0.0))
    out = (out - axis * out.dot(axis)).normalized()
    r = 0.030 * fig.h * fig.bulk + 0.018
    at = c + out * (r + 0.002)
    yaw = 90.0 if side == "L" else -90.0
    bone = f"UpperArm.{side}"
    parts = [Part("box", tuple(at), (0.10, 0.12, 0.006), mat=white, bone=bone,
                  rot=(0.0, yaw, 0.0), extras={"rigid": True, "bevel": False})]
    for lean in (38.0, -38.0):
        parts.append(Part("box", tuple(at + out * 0.004), (0.016, 0.12, 0.004), mat=red,
                          bone=bone, rot=(lean, yaw, 0.0),
                          extras={"rigid": True, "bevel": False}))
    return parts


def _handgonne(fig: Human, g: Vector, aim: Vector) -> list[Part]:
    """Oak tiller 0.80 m (5 x 4 cm, tail cut flat) held in the right fist near its
    tail, the fat octagonal wrought-iron barrel (0.32 m, 7.5 cm across, 3 hoops,
    sooted touch-hole pan, hook lug under the muzzle) bound on by 3 bands, the
    whole thing sloped up over the left shoulder along `aim`."""
    d = aim.normalized()
    tail = g - d * 0.12
    tiller_end = tail + d * 0.80
    b0 = tiller_end - d * 0.10
    b1 = b0 + d * 0.32
    fig.prop_bone("Gun", "R", head=g, tail=b1)
    prop = {"prop": True}
    # "up" for the gun: world up with the aim removed (the lug hangs below it).
    up = Vector((0.0, 0.0, 1.0))
    up = (up - d * up.dot(d)).normalized()
    parts = [
        _seg("cyl", tail, tiller_end, 0.025, "oak_tiller", "Gun", segments=4, rx=0.021,
             extras={**prop}),
        _seg("cyl", b0, b1, 0.0375, "gun_iron", "Gun", segments=8, extras={**prop}),
        # muzzle mouth darkened (bore) and the breech pan
        _seg("cyl", b1 - d * 0.004, b1 + d * 0.002, 0.024, "soot_bore", "Gun", segments=8,
             extras={**prop, "bevel": False}),
    ]
    for t in (0.06, 0.52, 0.94):   # the 3 reinforcing hoops
        c = b0 + d * (0.32 * t)
        parts.append(_seg("cyl", c - d * 0.012, c + d * 0.012, 0.043, "gun_iron", "Gun",
                          segments=8, extras={**prop, "bevel": False}))
    for t in (0.25, 0.50, 0.75):   # tiller bands
        c = tail + d * (0.80 * t)
        parts.append(_seg("cyl", c - d * 0.010, c + d * 0.010, 0.029, "gun_iron", "Gun",
                          segments=4, rx=0.025, extras={**prop, "bevel": False}))
    pan = b0 + d * 0.04 + up * 0.040
    parts.append(Part("box", tuple(pan), (0.03, 0.03, 0.012), mat="soot_bore", bone="Gun",
                      rot=_rot_to(up), extras={**prop, "bevel": False}))
    lug = b1 - d * 0.03 - up * 0.065
    parts.append(_seg("cyl", b1 - d * 0.03 - up * 0.03, lug, 0.012, "gun_iron", "Gun",
                      segments=4, extras={**prop, "bevel": False}))
    parts.append(_seg("cyl", lug, lug - d * 0.035, 0.010, "gun_iron", "Gun", segments=4,
                      extras={**prop, "bevel": False}))
    return parts


def handgunner(entry: Entry):
    # Eyes 1.64 m -> stature 1.752 m; the kettle hat's crown is at 1.78 m. Slightly
    # stocky (bulk 1.12), 0.48 m shoulders. The right fist holds the tiller near
    # its tail at the hip, the barrel on the left shoulder; the left fist holds the
    # lit match low at the side.
    fig = Human(height=1.752, bulk=1.10, shoulders=0.48,
                arm_r=ArmPose(spread=11.0, swing=8.0, elbow=34.0),
                arm_l=ArmPose(spread=13.0, swing=2.0, elbow=26.0))
    h = fig.h
    pad = 0.022
    parts = [fig.torso_part("padded_jack", pad=pad, hem=0.815, hem_flare=1.22, collar=0.08,
                            quilt=0.07, segments=56)]
    for side in ("L", "R"):
        parts.append(fig.arm_part(side, "padded_jack", pad=pad * 0.8, quilt_rings=13))
        parts += fig.hand_part(side, "skin")
        # knee boots to 0.50 m, cuff turned down
        parts.append(fig.leg_part(side, "hose", paint=[
            {"mat": "leather", "min": (-1, -1, -1), "max": (1, 1, 0.50)}]))
        parts.append(fig.foot_part(side, "leather", length=0.27, point=0.2))
        knee = fig.joint(f"knee.{side}")
        ank = fig.joint(f"ankle.{side}")
        cuff = ank.lerp(knee, (0.475 - ank.z) / (knee.z - ank.z))
        parts.append(Part("cyl", tuple(cuff), (0.090, 0.094, 0.05), mat="leather",
                          bone=f"LowerLeg.{side}", segments=12, taper=1.12,
                          extras={"bevel": False, "smooth": True,
                                  "bones": [f"LowerLeg.{side}", f"UpperLeg.{side}"]}))
        parts += _badge(fig, side, 1.35, "livery_white", "livery_red")
    parts += fig.head_part("skin", features="leather")
    parts += _kettle(fig, "kettle_steel", crown=1.78, r_dome=0.111, dome_h=0.14,
                     r_brim=0.215, droop=12.0, rivets=8)
    parts.append(_front_lacing(fig, pad, "leather", fig.belt_z + 0.05, 0.815 * h))

    # Belt at 1.02 m, iron buckle; shot pouch (right hip), horn flask (left hip).
    parts.append(fig.band(1.02, "leather", height=0.04, pad=0.008, torso_pad=pad))
    buckle = fig.surface(1.02, -90.0, pad=pad + 0.012)
    parts.append(Part("torus", tuple(buckle), (0.05, 0.05, 0.045), mat="gun_iron",
                      bone="Hips", rot=(90, 0, 0), segments=4, rings=4, minor=0.2,
                      extras={"rigid": True, "bevel": False}))
    pouch = fig.surface(0.95, -140.0, pad=pad + 0.03)
    fig.add_bone("Pouch", pouch + Vector((0, 0, 0.07)), pouch - Vector((0, 0, 0.07)), "Hips")
    parts.append(Part("box", tuple(pouch), (0.12, 0.05, 0.14), mat="leather", bone="Pouch",
                      rot=(0, 0, 40.0), extras={"rigid": True}))
    parts.append(Part("box", tuple(pouch + Vector((-0.006, -0.008, 0.05))), (0.126, 0.03, 0.06),
                      mat="leather", bone="Pouch", rot=(10, 0, 40.0), extras={"rigid": True}))
    parts.append(Part("cyl", tuple(pouch + Vector((-0.02, -0.03, 0.02))), (0.012, 0.012, 0.03),
                      mat="oak_tiller", bone="Pouch", rot=(0, 90, 40.0), segments=6,
                      extras={"rigid": True, "bevel": False}))
    top = fig.surface(0.98, -20.0, pad=pad + 0.03)
    fig.add_bone("Flask", top, top + Vector((0.03, 0.0, -0.24)), "Hips")
    horn = [top + Vector((0.0, 0.0, 0.0)), top + Vector((0.035, -0.01, -0.09)),
            top + Vector((0.035, -0.02, -0.18)), top + Vector((0.0, -0.03, -0.25))]
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="horn", bone="Flask", segments=8,
                      extras={"path": [tuple(p) for p in horn],
                              "sections": [(0.036, 0.036), (0.032, 0.032), (0.022, 0.022),
                                           (0.010, 0.010)],
                              "rigid": True, "smooth": True, "bevel": False}))
    parts.append(_seg("cyl", horn[0] + Vector((0, 0, -0.012)), horn[0] + Vector((0, 0, 0.012)),
                      0.039, "gun_iron", "Flask", segments=8, extras={"rigid": True}))
    parts.append(_seg("cyl", horn[3], horn[3] + Vector((-0.01, -0.01, -0.04)), 0.010,
                      "gun_iron", "Flask", segments=6, extras={"rigid": True, "bevel": False}))
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="leather", bone="Flask", segments=4,
                      extras={"path": [tuple(fig.surface(1.02, -30.0, pad + 0.012)),
                                       tuple(top + Vector((0.0, 0.0, 0.01)))],
                              "section": (0.004, 0.004), "rigid": True, "bevel": False}))

    # Bandolier: buff strap over the left shoulder to the right hip, round the back.
    band = []
    for z, ang in ((0.96, -150.0), (1.10, -125.0), (1.24, -95.0), (1.36, -62.0),
                   (1.43, -30.0), (1.45, 10.0), (1.40, 55.0), (1.28, 90.0),
                   (1.14, 125.0), (1.00, 160.0)):
        band.append(tuple(fig.surface(z, ang, pad=pad + 0.012)))
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="buff_leather", bone="Chest",
                      segments=4, extras={
                          "path": band, "section": (0.005, 0.025), "up": (0, 0, 1),
                          "smooth": True, "bevel": False,
                          "bones": ["Hips", "Spine", "Chest", "Shoulder.L"]}))

    # Slow match: a coil on a toggle at the chest, the lit end in the left fist.
    coil = fig.surface(1.25, -105.0, pad=pad + 0.02)
    parts.append(Part("torus", tuple(coil), (0.10, 0.10, 0.10), mat="hemp_match", bone="Chest",
                      rot=(80, 0, 0), segments=12, rings=5, minor=0.16,
                      extras={"rigid": True, "bevel": False}))
    parts.append(Part("torus", tuple(coil + Vector((0, -0.01, 0))), (0.07, 0.07, 0.07),
                      mat="hemp_match", bone="Chest", rot=(80, 0, 0), segments=10, rings=5,
                      minor=0.2, extras={"rigid": True, "bevel": False}))
    gl = fig.grip("L")
    _s, _e, _w, _t, fore_l = fig._arm["L"]
    tip = gl + fore_l * 0.10 + Vector((0.0, -0.02, 0.02))
    fig.prop_bone("MatchCord", "L", head=gl, tail=tip)
    loop = [gl + Vector((0.0, 0.0, 0.01)), gl + Vector((0.01, 0.02, -0.10)),
            gl + Vector((0.0, 0.03, -0.18)), gl + Vector((-0.02, 0.01, -0.10)),
            gl - fore_l * 0.01]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="hemp_match", bone="MatchCord",
                      segments=5, extras={"path": [tuple(p) for p in loop],
                                          "section": (0.005, 0.005), "prop": True,
                                          "smooth": True, "bevel": False}))
    parts.append(_seg("cyl", gl, tip, 0.005, "hemp_match", "MatchCord", segments=5,
                      extras={"prop": True, "bevel": False}))
    parts.append(Part("sphere", tuple(tip), (0.022, 0.022, 0.03), mat="match",
                      bone="MatchCord", rot=_rot_to(tip - gl), segments=8, rings=5,
                      extras={"prop": True, "bevel": False}))

    g = fig.grip("R")
    parts += _handgonne(fig, g, Vector((0.30, 0.0, 0.62)))

    pose = dict(figures.HUMAN_TEST_POSE)
    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(pose),
        family_overrides={
            # "Emissive tip of the slow match": EnemyForge multiplies emission by 9,
            # so the mask stores the madder dimmed (the warden's horn-pane trick).
            "match": {"emit": "#5A2210", "rough": 0.6},
            "kettle_steel": {"rough": 0.45},
            "padded_jack": {"grain": 0.30},
        },
        extra_families={
            "skin": {"name": "Skin", "base": "#9C7A5E", "rough": 0.7,
                     "notes": "Face and bare hands: the JSON lists no skin family."},
            "hose": {"name": "Dark wool hose", "base": "#3A2F26", "rough": 0.85,
                     "notes": "Build: 'dark wool hose'; hex from the halberdier's Hose "
                              "(same livery), the JSON lists none here."},
            "livery_white": {"name": "Livery white", "base": "#D6CDB6", "rough": 0.9,
                             "notes": "The sleeve badge's 'white cloth patch'; hex from "
                                      "the halberdier's Livery white."},
            "buff_leather": {"name": "Buff leather", "base": "#8A6A4C", "rough": 0.7,
                             "notes": "JSON Leather notes: 'bandolier (buff, lighter)'."},
            "horn": {"name": "Cow horn", "base": "#B8A07A", "rough": 0.45,
                     "notes": "The powder flask is cow horn; no horn family in the JSON."},
            "hemp_match": {"name": "Hemp match cord", "base": "#7A6A50", "rough": 0.9,
                           "notes": "The 1.2 m slow-match cord itself (only its tip is "
                                    "madder)."},
            "soot_bore": {"name": "Soot", "base": "#1C1A18", "rough": 0.9,
                          "notes": "The bore and the sooted flash-pan at the touch-hole."},
        },
        notes=[
            "Handgonne (tiller + barrel) is a prop bone Gun on Hand.R; MatchCord is a "
            "prop bone on Hand.L carrying the lit tip. Both are left out of the height.",
            "Hat is its own bone under Head (detachable); Pouch and Flask are bones "
            "under Hips (rigid, not springs).",
            "Not built: the 4-bone match_cord chain (one bone), jack_skirt springs, the "
            "smoke plume (VFX), the switchable shoulder socket for the gun, powder smut "
            "and singe decals.",
        ])


def _rod(a, b, radius: float, mat: str, bone: str, segments: int = 10,
         taper: float = 1.0, rx: float | None = None, extras: dict | None = None) -> Part:
    """_seg as a two-point sweep. EnemyForge's axis-aligned cyl/cone/box primitives
    come out with exactly symmetric cotangent weights; when such an island is hidden
    from its nearest bone (a flute on a plate, a strap on a cuisse) Blender's heat
    Laplacian goes exactly singular and heat weighting fails for the whole mesh.
    A sweep's frames break that symmetry. taper 0 closes the far end to a point."""
    a, b = Vector(a), Vector(b)
    d = (b - a).normalized()
    up = Vector((0.0, 0.0, 1.0)) if abs(d.z) < 0.9 else Vector((0.0, 1.0, 0.0))
    r2 = rx or radius
    return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, segments=segments,
                extras={"path": [tuple(a), tuple(b)],
                        "sections": [(radius, r2), (radius * taper, r2 * taper)],
                        "up": tuple(up), **(extras or {})})


def _block(centre, size, mat: str, bone: str, extras: dict | None = None) -> Part:
    """An axis-aligned box as a rounded-square sweep along Z (see _rod for why)."""
    c = Vector(centre)
    sx, sy, sz = size
    return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, segments=4,
                extras={"path": [tuple(c - Vector((0, 0, sz / 2))), tuple(c + Vector((0, 0, sz / 2)))],
                        "sections": [(sy / 2, sx / 2)] * 2, "up": (0.0, 1.0, 0.0),
                        "power": 6.0, **(extras or {})})


# --------------------------------------------------------------------------------
# Gothic Man-at-Arms (heavy) — glitter before shape: round armet with a beak, big
# fluted shoulders, a pointed waist, a short axe on a pole at his side.
# --------------------------------------------------------------------------------

def _plate_pt(fig: Human, x: float, z: float, pad: float, chest: float) -> Vector:
    """A point on the front of `fig.torso_part(pad=pad, chest=chest)` at (x, z),
    following the loft exactly (ring points at one angle, lerped between rows), so
    a plate laid on a strongly globose breastplate is not buried in it. _torso_y
    assumes the default chest bulge and misses by centimetres at chest=0.17."""
    rows = figures._TORSO
    h, b, e = fig.h, fig.bulk, 2.0 / 2.4

    def ring_pt(row, a):
        zf, hw, hd, dy = row
        sa, ca = math.sin(a), math.cos(a)
        k = 1.0 + (chest if 0.66 < zf < 0.8 else 0.0) * max(0.0, -sa) ** 2
        return Vector((math.copysign(abs(ca) ** e, ca) * (hw * h * b + pad) * k,
                       dy * h + math.copysign(abs(sa) ** e, sa) * (hd * h * b + pad) * k,
                       zf * h))
    zf = z / h
    i = max(0, min(len(rows) - 2, next((j for j in range(len(rows) - 1)
                                        if rows[j + 1][0] >= zf), len(rows) - 2)))
    f = (zf - rows[i][0]) / (rows[i + 1][0] - rows[i][0])

    def at(a):
        return ring_pt(rows[i], a).lerp(ring_pt(rows[i + 1], a), f)
    lo, hi = -math.pi / 2, -math.pi / 2 + math.copysign(math.pi / 2 * 0.98, x)
    for _ in range(40):   # bisect the ring angle for the wanted x
        mid = (lo + hi) / 2
        if abs(at(mid).x) < abs(x):
            lo = mid
        else:
            hi = mid
    p = at((lo + hi) / 2)
    return Vector((p.x, p.y, z))


def _ellipsoid_pt(base: Vector, axis: Vector, radius: float, height: float,
                  theta: float, phi: float, out: float = 0.0) -> Vector:
    """A point on a dome (see _dome) at polar angle theta (0 = pole, 90 = rim) and
    azimuth phi, `out` metres proud of it. The dome is treated as an ellipsoid cap,
    which runs a few millimetres inside _dome's fuller profile (hence `out`)."""
    a = Vector(axis).normalized()
    u = a.cross(Vector((0.0, 1.0, 0.0)))
    if u.length < 1e-3:
        u = a.cross(Vector((1.0, 0.0, 0.0)))
    u.normalize()
    v = a.cross(u).normalized()
    t, p = math.radians(theta), math.radians(phi)
    radial = u * math.cos(p) + v * math.sin(p)
    pt = base + a * (height * math.cos(t)) + radial * (radius * math.sin(t))
    n = (a * (math.cos(t) / max(height, 1e-6)) + radial * (math.sin(t) / radius)).normalized()
    return pt + n * out


def _armet(fig: Human, mat: str, dark: str, strap: str, crown: float) -> list[Part]:
    """Armet: a rounded skull 0.25 m wide closing under the chin, a low comb, a
    wrapper strapped over the jaw, a sparrow-beak visor projecting 0.10 m on its own
    hinge bone (Visor, opens 70 deg), twin sight slits above it, six breaths on the
    wearer's right, and a 0.07 m rondel on a 0.05 m stem at the back (Rondel)."""
    z0 = crown - 0.39
    sy = 1.12   # skull depth / width
    # sight slits are painted bands between the two profile points at 0.183/0.197
    slit = [{"mat": dark, "min": (sx0, -1.0, 0.1835), "max": (sx1, -0.05, 0.1965)}
            for sx0, sx1 in ((0.012, 0.088), (-0.088, -0.012))]
    prof = [(0.086, 0.0), (0.100, 0.020), (0.112, 0.060), (0.122, 0.130), (0.1243, 0.183),
            (0.1249, 0.197), (0.125, 0.200),
            (0.119, 0.270), (0.101, 0.322), (0.071, 0.360), (0.036, 0.383), (0.0, 0.390)]
    parts = [Part("lathe", (0.0, 0.0, z0), (1.0, sy, 1.0), mat=mat, bone="Head", segments=24,
                  extras={"profile": prof, "rigid": True, "smooth": True, "bevel": False,
                          "paint": slit})]

    def skull_r(dz):
        for (ra, za), (rb, zb) in zip(prof, prof[1:]):
            if za <= dz <= zb:
                return ra + (rb - ra) * (dz - za) / (zb - za)
        return 0.0

    # Low comb over the skull from brow to nape.
    comb = []
    for deg in range(-70, 71, 14):
        a = math.radians(deg)
        dz = 0.20 + 0.19 * math.cos(a)
        y = math.copysign(skull_r(dz) * sy, a) if abs(deg) > 1 else 0.0
        comb.append((0.0, y * 0.985, z0 + dz - 0.004))
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, bone="Head", segments=6,
                      extras={"path": comb, "section": (0.012, 0.005), "up": (1, 0, 0),
                              "rigid": True, "smooth": True, "bevel": False}))

    # Wrapper plate over the jaw, strapped round the back of the neck.
    zw = z0 + 0.045
    rw = skull_r(0.045) + 0.010
    wrap = [(math.cos(math.radians(d)) * rw, math.sin(math.radians(d)) * rw * sy, zw)
            for d in range(200, 341, 20)]
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone="Head", segments=4,
                      extras={"path": wrap, "sections": [(0.006, 0.030)] * len(wrap),
                              "up": (0, 0, 1), "power": 4.0, "rigid": True,
                              "smooth": True, "bevel": False}))
    back = [(math.cos(math.radians(d)) * (rw - 0.004), math.sin(math.radians(d)) * (rw - 0.004) * sy,
             zw) for d in range(-10, 191, 25)]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=strap, bone="Head", segments=4,
                      extras={"path": back, "section": (0.004, 0.012), "up": (0, 0, 1),
                              "rigid": True, "smooth": True, "bevel": False}))

    # Sparrow-beak visor: loft from inside the skull front out to the beak tip.
    zv = z0 + 0.100          # visor centre line
    stations = [(-0.080, 0.124, 0.078, 0.0), (-0.128, 0.120, 0.075, 0.0),
                (-0.162, 0.102, 0.064, -0.004), (-0.197, 0.066, 0.044, -0.009),
                (-0.226, 0.028, 0.022, -0.014)]
    n = 16

    def vis_ring(y, hw, hz, dz):
        # keel: the centre line pushed forward so the beak has a horizontal ridge
        ring = []
        for j in range(n):
            a = 2.0 * math.pi * j / n
            ca, sa = math.cos(a), math.sin(a)
            x = math.copysign(abs(ca) ** 0.9, ca) * hw
            z = zv + dz + math.copysign(abs(sa) ** 1.1, sa) * hz
            ring.append((x, y - 0.018 * (1.0 - abs(ca)) * (abs(sa) < 0.99), z))
        return ring
    rings = [vis_ring(*st) for st in stations]
    tip = (0.0, -0.250, zv - 0.018)
    rings.append([tip])
    pivot = Vector((0.0, -0.02, zv + 0.02))
    fig.add_bone("Visor", pivot, Vector(tip) + Vector((0.0, 0.03, 0.0)), "Head")
    parts.append(Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone="Visor", extras={
        "rings": rings, "rigid": True, "smooth": False, "bevel": False,
        "paint": [{"mat": dark, "min": (-1.0, -0.188, zv - 0.03), "max": (-0.06, -0.13, zv + 0.03)}]}))
    # hinge pivots at the sides
    for s in (1.0, -1.0):
        parts.append(_rod((s * 0.114, -0.060, zv + 0.005), (s * 0.128, -0.060, zv + 0.005),
                          0.013, mat, "Visor", segments=8,
                          extras={"rigid": True, "bevel": False}))
    # Breaths on the wearer's right cheek and the sight slits are painted dark on
    # the visor and skull shells: detached studs there broke the heat solve.
    # Rondel on its stem at the back.
    zr = z0 + 0.150
    yb = skull_r(0.150) * sy
    fig.add_bone("Rondel", (0.0, yb - 0.01, zr), (0.0, yb + 0.06, zr), "Head")
    parts.append(_rod((0.0, yb - 0.01, zr), (0.0, yb + 0.050, zr), 0.009, mat,
                      "Rondel", segments=6, extras={"rigid": True, "bevel": False}))
    parts.append(_rod((0.0, yb + 0.048, zr), (0.0, yb + 0.058, zr), 0.035, mat,
                      "Rondel", segments=14, extras={"rigid": True}))
    return parts


def _fan_wing(centre: Vector, out: Vector, up: Vector, size: float, mat: str, bone: str,
              dark: str | None = None) -> list[Part]:
    """A fluted fan wing (couter/poleyn): a flat fan standing off `centre` toward
    `out`, in the plane of `out` and `up`, with ridges radiating from its root."""
    out, up = out.normalized(), up.normalized()
    normal = out.cross(up).normalized()
    fan = [(-0.45, 0.10), (0.05, 0.02), (0.55, 0.30), (0.95, 0.00), (0.55, -0.30),
           (0.05, -0.02), (-0.45, -0.10)]
    # A prism (its caps are polygon-filled; a two-ring loft fans a concave cap
    # into overlapping triangles, which broke heat weighting for the whole mesh).
    up = normal.cross(out).normalized()
    basis = Matrix((out, up, normal)).transposed()
    rot = tuple(math.degrees(a) for a in basis.to_euler("XYZ"))
    parts = [Part("prism", tuple(centre), (1.0, 1.0, 0.010), mat=mat, bone=bone, rot=rot,
                  extras={"outline": [(u * size, v * size) for u, v in fan], "rigid": True,
                          "bevel": False})]
    if dark:
        for v in (0.18, 0.0, -0.18):   # flute grooves
            # separate roots: coincident vertices break the heat solve
            a = centre + out * (0.12 * size) + up * (v * 0.35 * size) + normal * 0.0055
            b = centre + out * (0.80 * size) + up * (v * size) + normal * 0.0055
            parts.append(_rod(a, b, 0.0035, dark, bone, segments=4,
                              extras={"rigid": True, "bevel": False}))
    return parts


def _poleaxe(fig: Human, g: Vector, mat: str, haft: str, strap: str) -> list[Part]:
    """1.70 m overall, grounded at the right side through the right fist: ash haft
    3.5 cm, square iron butt spike; a 0.32 m head (axe 0.14 x 0.18 m outboard, a
    4-point hammer inboard, 0.18 m top spike), 0.20 m langets and a 0.11 m rondel
    guard at 1.10 m. Prop bone Poleaxe on Hand.R."""
    x, y = g.x, g.y
    overall, head = 1.70, 0.32
    sock = overall - head          # 1.38
    fig.prop_bone("Poleaxe", "R", head=(x, y, g.z), tail=(x, y, overall))
    pr = {"prop": True}
    parts = [
        _rod((x, y, 0.08), (x, y, sock), 0.0175, haft, "Poleaxe", segments=8,
             extras={**pr, "smooth": True, "bevel": False}),
        _rod((x, y, 0.10), (x, y, 0.0), 0.018, mat, "Poleaxe", segments=4, taper=0.0,
             extras=dict(pr)),
        _rod((x, y, sock - 0.01), (x, y, sock + 0.11), 0.021, mat, "Poleaxe", segments=8,
             extras=dict(pr)),
        _rod((x, y, overall - 0.18), (x, y, overall), 0.016, mat, "Poleaxe", segments=4,
             taper=0.0, extras=dict(pr)),
        _rod((x, y, 1.095), (x, y, 1.105), 0.055, mat, "Poleaxe", segments=12,
             extras=dict(pr)),
    ]
    for sgn in (1.0, -1.0):
        parts.append(_block((x, y + sgn * 0.019, sock - 0.10), (0.012, 0.004, 0.20), mat,
                            "Poleaxe", extras=dict(pr)))
    for zg in (0.90, 1.30):   # leather grip wraps at the two polished grips
        parts.append(_rod((x, y, zg - 0.035), (x, y, zg + 0.035), 0.0195, strap, "Poleaxe",
                          segments=8, extras={**pr, "bevel": False, "smooth": True}))
    # Axe blade outboard (-X), 0.14 deep x 0.18 tall, flared crescent edge.
    blade = [(0.015, 0.060), (0.080, 0.075), (0.140, 0.010), (0.150, 0.100),
             (0.140, 0.190), (0.080, 0.125), (0.015, 0.140)]
    parts.append(Part("prism", (x, y, sock + 0.02), (1.0, 1.0, 0.009), mat=mat, bone="Poleaxe",
                      rot=(90.0, 0.0, 180.0), extras={"outline": blade, **pr}))
    # Hammer inboard (+X): a short square neck and a four-point face.
    hz = sock + 0.10
    parts.append(Part("box", (x + 0.045, y, hz), (0.06, 0.030, 0.030), mat=mat,
                      bone="Poleaxe", extras=dict(pr)))
    parts.append(Part("box", (x + 0.080, y, hz), (0.014, 0.050, 0.050), mat=mat,
                      bone="Poleaxe", extras=dict(pr)))
    for dy in (-0.013, 0.013):
        for dz in (-0.013, 0.013):
            parts.append(_rod((x + 0.085, y + dy, hz + dz), (x + 0.103, y + dy, hz + dz),
                              0.009, mat, "Poleaxe", segments=4, taper=0.0,
                              extras={**pr, "bevel": False}))
    return parts


def gothic_knight(entry: Entry):
    # Eyes 1.72 m -> stature 1.838 m; the armet crown reaches 1.95 m. Stands
    # square, poleaxe grounded at the right side, left gauntlet hanging.
    fig = Human(height=1.838, bulk=1.10, shoulders=0.50, stance=3.5,
                arm_r=ArmPose(spread=20.0, swing=0.0, elbow=30.0),
                arm_l=ArmPose(spread=16.0, swing=2.0, elbow=20.0))
    H, M, S, R, A, D = ("white_harness", "mail_voiders", "straps", "livery_sash",
                        "ash_haft", "soot")
    bpad = 0.036   # breastplate over the arming doublet
    # Breastplate as the torso: globose chest, plate collar (gorget) under the
    # armet, mail skirt below the fauld painted in voiders mail.
    parts = [fig.torso_part(H, pad=bpad, hem=0.82, hem_flare=1.16, collar=0.05,
                            chest=0.17, segments=24, paint=[
                                {"mat": M, "min": (-1, -1, -1), "max": (1, 1, 0.950)}])]

    # Cusped plackart rising from the waist to a point at 1.52 m, 7 fan flutes.
    chest = 0.17
    ppad = bpad + 0.004
    rows = [(1.04, 0.175), (1.10, 0.165), (1.18, 0.135), (1.27, 0.095), (1.36, 0.058),
            (1.44, 0.028), (1.52, 0.006)]
    prings = []
    for z, hw in rows:
        outer, inner = [], []
        # 15 columns converging on the point: the 7 odd ones stand 6 mm proud, so the
        # fan flutes are corrugations of the one plate (a separate strip per flute
        # is a detached island, and those broke the heat solve; see _rod)
        for c in range(15):
            x = -hw + 2.0 * hw * c / 14
            rise = 0.006 if (c % 2 and z < 1.47) else 0.0
            outer.append(tuple(_plate_pt(fig, x, z, ppad + 0.006 + rise, chest)))
            inner.append(tuple(_plate_pt(fig, x, z, ppad, chest)))
        prings.append(outer + list(reversed(inner)))
    parts.append(Part("loft", (0, 0, 0), (1, 1, 1), mat=H, bone="Spine", extras={
        "rings": prings, "bevel": False, "smooth": False,
        "paint": [{"mat": D, "min": (-1, -1, 1.03), "max": (1, 1, 1.055)}],
        "bones": ["Hips", "Spine", "Chest"]}))
    # lance-rest bolt hole on the right breast
    lr = fig.surface(1.30, -130.0, pad=bpad + 0.004)
    parts.append(_rod(lr + Vector((0.006, 0.008, 0.0)), lr - Vector((0.006, 0.008, 0.0)), 0.009, D,
                      "Chest", segments=6, extras={"rigid": True, "bevel": False}))

    # Fauld: 3 lames 1.06-0.95 m, each stepping out over the one below, built into
    # the torso loft itself (bands around it were blind shells for heat weighting).
    torso = parts[0]
    old = torso.extras["rings"]

    def sample(z, scale):
        for ra, rb in zip(old, old[1:]):
            za, zb = ra[0][2], rb[0][2]
            if za <= z <= zb:
                f = (z - za) / (zb - za)
                pts = [Vector(a).lerp(Vector(b), f) for a, b in zip(ra, rb)]
                cy = sum(p.y for p in pts) / len(pts)
                return [(p.x * scale, cy + (p.y - cy) * scale, z) for p in pts]
        raise ValueError(z)
    lames = []
    for top, bot, k in ((1.065, 1.028, 1.0), (1.028, 0.991, 1.0), (0.991, 0.952, 1.0)):
        lames += [sample(bot + 0.001, 1.07), sample(bot + 0.004, 1.075), sample(top - 0.002, 1.02)]
    lames.sort(key=lambda r: r[0][2])
    keep = [r for r in old if not (0.945 < r[0][2] < 1.068)]
    torso.extras["rings"] = sorted(keep + lames, key=lambda r: r[0][2])

    # Tassets: two pointed plates 0.18 m wide, 1.06 -> 0.82 m, 3 flutes each.
    for side in ("L", "R"):
        s = figures.SIDES[side]
        cx = s * 0.105
        trs = []
        for z, hw, yf in ((1.06, 0.090, -0.176), (0.98, 0.090, -0.186), (0.92, 0.088, -0.196),
                          (0.88, 0.060, -0.204), (0.845, 0.030, -0.212), (0.82, 0.006, -0.218)):
            outer, inner = [], []
            for c in range(7):   # 3 flutes: columns 1, 3, 5 stand proud
                u = -1.0 + 2.0 * c / 6
                yy = yf - 0.018 * (1.0 - u * u) - (0.006 if c % 2 else 0.0)
                outer.append((cx + u * hw, yy - 0.008, z))
                inner.append((cx + u * hw, yf - 0.018 * (1.0 - u * u), z))
            trs.append(outer + list(reversed(inner)))
        parts.append(Part("loft", (0, 0, 0), (1, 1, 1), mat=H, bone=f"UpperLeg.{side}",
                          extras={"rings": trs, "rigid": True, "bevel": False,
                                  "paint": [{"mat": S, "min": (cx - 0.02, -1, 1.02),
                                             "max": (cx + 0.02, 1, 1.07)}]}))

    # Arms: mail sleeves (voiders show at the armpit and elbow) under rigid plate.
    for side in ("L", "R"):
        s = figures.SIDES[side]
        shoulder, elbow, wrist = _arm_axis(fig, side)
        fore = (wrist - elbow).normalized()
        upv = (shoulder - elbow).normalized()
        # No mail sleeve under the plates: every hidden layer makes the plate over
        # it blind to its bone, and enough of those break the heat solve. The
        # rerebrace runs from inside the pauldron to the couter, the vambrace from
        # the couter into the gauntlet cuff; mail shows only as the armpit voider.
        parts += fig.hand_part(side, H)
        ua, la = f"UpperArm.{side}", f"LowerArm.{side}"
        rb = 0.029 * fig.h * fig.bulk + 0.010
        parts.append(_rod(shoulder.lerp(elbow, 0.30), elbow + upv * 0.01, rb,
                          H, ua, segments=12, taper=0.86,
                          extras={"rigid": True, "bevel": False, "smooth": True}))
        parts.append(_rod(elbow - fore * 0.02, wrist + fore * 0.01, rb * 0.92,
                          H, la, segments=12, taper=0.80,
                          extras={"rigid": True, "bevel": False, "smooth": True}))
        parts.append(_rod(shoulder + Vector((-s * 0.05, 0.0, -0.075)),
                          shoulder.lerp(elbow, 0.34), rb * 1.02, M, ua, segments=10,
                          extras={"rigid": True, "bevel": False, "smooth": True}))
        # couter: a cop and a fluted fan wing on the outside of the elbow
        parts.append(Part("sphere", tuple(elbow + Vector((s * 0.006, 0.012, 0.0))),
                          (0.105, 0.105, 0.105), mat=H, bone=ua, segments=10, rings=6,
                          extras={"rigid": True, "bevel": False}))
        outw = Vector((s, 0.25, 0.0))
        parts += _fan_wing(elbow + Vector((s * 0.035, 0.02, 0.0)), outw, upv, 0.10, H, ua)
        # gauntlet cuff: flared and fluted, 0.11 m back over the forearm
        parts.append(_rod(wrist + fore * 0.012, wrist - fore * 0.10, 0.043, H,
                          f"Hand.{side}", segments=10, taper=1.40,
                          extras={"rigid": True, "bevel": False}))
        # pauldron: a fluted dome 0.24 m wide and two lames to ~1.30 m
        cap_axis = upv + Vector((s * 0.35, 0.0, 0.0))
        cap_base = shoulder - upv * 0.045 + Vector((s * 0.010, 0.0, 0.0))
        parts.append(_dome(cap_base, cap_axis, 0.122, 0.105, H, ua, segments=14,
                           extras={"rigid": True, "bevel": False}))
        for t, r in ((0.20, 0.108), (0.34, 0.096), (0.47, 0.085)):
            a = shoulder + (elbow - shoulder) * (t - 0.07)
            b = shoulder + (elbow - shoulder) * (t + 0.07)
            parts.append(_rod(b, a, r, H, ua, segments=14, taper=1.10,
                              extras={"rigid": True, "bevel": False, "smooth": True}))
        # besagew disc at the front of the armpit
        bz = shoulder + Vector((-s * 0.005, -0.125, -0.10))
        parts.append(_rod(bz + Vector((0, 0.01, 0)), bz, 0.045, H, ua, segments=12,
                          extras={"rigid": True}))

    # Legs: mail under cuisses, fan poleyns at 0.50 m and full greaves.
    for side in ("L", "R"):
        s = figures.SIDES[side]
        hip, knee, ank = fig.joint(f"hip.{side}"), fig.joint(f"knee.{side}"), \
            fig.joint(f"ankle.{side}")
        ul, ll = f"UpperLeg.{side}", f"LowerLeg.{side}"
        # Cuisse and greave are the leg (no hose under them; see the arms). The
        # cuisse's 2 flutes and the hinge straps are painted.
        r_th = 0.041 * fig.h * fig.bulk + 0.012
        parts.append(_rod(knee.lerp(hip, 1.10), knee.lerp(hip, -0.02), r_th * 0.86,
                          H, ul, segments=12, taper=1.14,
                          extras={"rigid": True, "bevel": False, "smooth": True}))
        r_sh = 0.031 * fig.h * fig.bulk + 0.012
        parts.append(_rod(ank.lerp(knee, -0.02), ank.lerp(knee, 0.96), r_sh * 0.82,
                          H, ll, segments=12, taper=1.30,
                          extras={"rigid": True, "bevel": False, "smooth": True, "paint": [
                              {"mat": S, "min": (-1, -1, ank.z + 0.18), "max": (1, 1, ank.z + 0.22)}]}))
        # poleyn: knee cop and a fluted side wing 0.10 m
        parts.append(Part("sphere", tuple(knee + Vector((0, -0.045, 0.01))), (0.13, 0.10, 0.13),
                          mat=H, bone=ll, segments=12, rings=6,
                          extras={"rigid": True, "bevel": False}))
        parts += _fan_wing(knee + Vector((s * 0.050, -0.02, 0.01)), Vector((s, -0.2, 0.0)),
                           Vector((0, 0, 1)), 0.10, H, ll)
        # sabaton with a poulaine toe, 0.34 m long, with lame ridges
        foot = fig.foot_part(side, H, length=0.34, point=1.0, segments=10)
        parts.append(foot)
        heel_y = ank.y + 0.045 * fig.h
        for k in range(7):
            t = 0.30 + 0.085 * k
            yy = heel_y - t * 0.34
            hw = (0.030 + (0.033 - 0.030) * min(1, (t - 0.32) / 0.23) if t < 0.55 else
                  0.033 + (0.030 - 0.033) * (t - 0.55) / 0.23 if t < 0.78 else
                  0.030 - 0.016 * (t - 0.78) / 0.15) * fig.h
            top = (0.062 + (0.042 - 0.062) * min(1, max(0, (t - 0.32) / 0.23)) if t < 0.55
                   else 0.042 + (0.030 - 0.042) * (t - 0.55) / 0.23 if t < 0.78
                   else 0.030 + (0.020 - 0.030) * (t - 0.78) / 0.15) * fig.h
            if hw <= 0.012:
                continue
            cx = ank.x + s * 0.006 * fig.h * t
            hh = top / 2.0
            arc = []
            for d in range(15, 166, 30):
                a = math.radians(d)
                arc.append((cx + math.cos(a) * hw * 1.02, yy,
                            hh + math.sin(a) * hh * 1.15 + 0.003))
            parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=H, bone=f"Foot.{side}",
                              segments=4, extras={"path": arc, "section": (0.004, 0.006),
                                                  "rigid": True, "smooth": True,
                                                  "bevel": False}))

    # Neck: a mail standard between the gorget collar and the armet.
    parts.append(_rod((0, 0.004, fig.neck_z + 0.02), (0, 0.004, fig.chin_z + 0.03),
                      0.058, M, "Neck", segments=12,
                      extras={"bevel": False, "smooth": True, "bones": ["Neck", "Head", "Chest"]}))
    parts += _armet(fig, H, D, S, crown=1.95)

    # Livery sash: over the right shoulder to the left hip, knot and 0.30 m tails.
    sp = ppad + 0.012
    sash = []
    for z, ang in ((0.99, -35.0), (1.10, -58.0), (1.22, -85.0), (1.33, -112.0),
                   (1.43, -140.0), (1.50, -165.0), (1.51, 165.0), (1.46, 130.0),
                   (1.34, 95.0), (1.20, 60.0), (1.06, 25.0), (1.00, -5.0)):
        sash.append(tuple(fig.surface(z, ang, pad=sp + (0.02 if 1.45 < z else 0.0))))
    sash.append(sash[0])
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=R, bone="Chest", segments=4,
                      extras={"path": sash[:-1], "closed": True, "section": (0.006, 0.040),
                              "up": (0, 0, 1), "smooth": True, "bevel": False,
                              "bones": ["Hips", "Spine", "Chest", "Shoulder.R"]}))
    knot = fig.surface(1.00, -18.0, pad=sp + 0.02)
    parts.append(Part("ico", tuple(knot), (0.055, 0.035, 0.045), mat=R, bone="Hips",
                      subdivisions=2, extras={"bevel": False, "smooth": True,
                                              "bones": ["Hips", "Spine"]}))
    for dx, dz in ((0.015, 0.30), (-0.02, 0.27)):
        tail = [knot + Vector((0, -0.01, -0.03)), knot + Vector((dx, -0.03, -0.15)),
                knot + Vector((dx * 1.6, -0.035, -dz))]
        parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat=R, bone="Hips", segments=4,
                          extras={"path": [tuple(p) for p in tail],
                                  "sections": [(0.008, 0.018), (0.006, 0.022), (0.005, 0.020)],
                                  "up": (0, -1, 0), "power": 4.0, "smooth": True,
                                  "bevel": False, "bones": ["Hips", "UpperLeg.L"]}))

    # The signet ring (orpiment, stealable) on the right gauntlet's finger.
    _s, _e, wr, _t, fr = fig._arm["R"]
    ring_at = wr + fr * (0.060 * fig.h) + Vector((0.0, -0.028, 0.0))
    parts.append(Part("torus", tuple(ring_at), (0.036, 0.036, 0.018), mat="signet_gold",
                      bone="Hand.R", rot=_rot_to(fr), segments=10, rings=4, minor=0.22,
                      extras={"rigid": True, "bevel": False}))

    parts += _poleaxe(fig, fig.grip("R"), H, A, S)

    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(),
        gold_reason="The signet ring on the right gauntlet is loot: 'a gold signet ring "
                    "(orpiment, 40 coin) ... the only gold on him, and only because you "
                    "can steal it'.",
        family_overrides={
            # Polished white harness, a little rougher so the review studio shows it
            # pale rather than as a warm mirror (see README Traps).
            "white_harness": {"rough": 0.42, "wear_to": "#5E6064", "wear_amount": 0.18},
            "mail_voiders": {"rough": 0.5, "ridges": (0.010, 0.40)},
        },
        extra_families={
            "signet_gold": {"name": "Gilt (orpiment) signet", "base": "#C9A13A",
                            "rough": 0.3, "metal": 1.0,
                            "notes": "Breakables: a gold signet ring on the right "
                                     "gauntlet, stealable loot (40 coin)."},
        },
        notes=[
            "Poleaxe (1.70 m) is a prop bone on Hand.R, left out of the 1.95 m height.",
            "Visor is its own hinge bone under Head (opens 70 deg); Rondel under Head.",
            "Plates are rigid on their limb bones over mail sleeves and hose, so a bent "
            "elbow or knee shows mail between plates rather than stretched steel.",
            "The sash follows the JSON (over the right shoulder to the left hip); the "
            "concept front draws it as a waist sash.",
            "Not built: pauldron twist helper bones, tasset and sash-tail springs (tassets "
            "are rigid on the thighs), a true slit opening and breaths (dark studs), "
            "scratch and soot decals.",
        ])


# --------------------------------------------------------------------------------
# Pavisier (special) — a tall pale rectangle with a big red X beside a man in a
# half-white half-red coat: the shield reads first.
# --------------------------------------------------------------------------------

class _ReachHuman(Human):
    """figures.Human with two-bone IK for chosen fists: reach={"L": (x, y, z)} puts
    that fist's grip exactly on the point, the elbow bent toward `pole`. Copied from
    enemies_high._ReachHuman (a shared-framework change would be figures.py's),
    so this module does not depend on another Age's module importing cleanly."""

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


_PAV_W, _PAV_SAG, _PAV_T = 0.62, 0.05, 0.04
_PAV_Z0, _PAV_Z1 = 0.04, 1.30
_PAV_XS = [-0.31, -0.29, -0.22, -0.15, -0.085, -0.052, -0.047, 0.0, 0.047, 0.052, 0.085,
           0.15, 0.22, 0.29, 0.31]


def _pav_front(x: float) -> float:
    """Front face y of the pavise (local, spine centred on 0), -Y = front: curved in
    plan with a 0.05 m sagitta, a raised spine 0.10 m wide standing 0.015 proud."""
    y = -_PAV_SAG * (1.0 - (x / (_PAV_W / 2)) ** 2)
    if abs(x) < 0.05:
        y -= 0.015 * (1.0 - (x / 0.05) ** 4)
    return y


def _pav_back(x: float) -> float:
    return -_PAV_SAG * (1.0 - (x / (_PAV_W / 2)) ** 2) + _PAV_T


def _pavise(fig: Human, c: Vector, grip: Vector) -> list[Part]:
    """1.30 x 0.62 x 0.04 m pavise standing on two spiked iron feet, its centre
    line at c (x, y). One loft whose rings are the curved plan section, so the whole
    face is one shell: the ragged saltire, the briquet device, the iron rim and the
    dents are painted cells on it (flat colour, and no floating islands for heat
    weighting to choke on). Prop bone Pavise on Hand.L; PaviseProp (hinge) under it
    carries the 0.85 m poplar prop leg."""
    cx, cy = c.x, c.y
    zs = [_PAV_Z0, 0.06] + [0.06 + (1.28 - 0.06) * k / 24 for k in range(1, 24)] + [1.28, _PAV_Z1]
    rings = []
    for z in zs:
        front = [(cx + x, cy + _pav_front(x), z) for x in _PAV_XS]
        back = [(cx + x, cy + _pav_back(x), z) for x in reversed(_PAV_XS)]
        rings.append(front + back)
    # Paint: a cell map on the front face, then the rim on every face.
    paint = []

    def cell(x0, x1, z0, z1, mat, front_only=True):
        xm = (x0 + x1) / 2
        ymax = cy + _pav_front(xm) + 0.012 if front_only else 1.0
        paint.append({"mat": mat, "min": (cx + x0 - 0.001, -1.0, z0 - 0.001),
                      "max": (cx + x1 + 0.001, ymax, z1 + 0.001)})
    half = _PAV_W / 2
    diag = Vector((2 * half, -(_PAV_Z1 - _PAV_Z0), 0)).normalized()   # top-left -> bottom-right
    ragged = [0.0, 0.012, -0.010, 0.016, -0.006, 0.010, -0.014, 0.004]
    for i in range(len(_PAV_XS) - 1):
        for j in range(len(zs) - 1):
            x0, x1, z0, z1 = _PAV_XS[i], _PAV_XS[i + 1], zs[j], zs[j + 1]
            xm, zm = (x0 + x1) / 2, (z0 + z1) / 2
            best = 9.0
            for sx in (1.0, -1.0):   # both bars of the saltire, corner to corner
                p = Vector((sx * xm + half, zm - _PAV_Z1, 0.0))
                best = min(best, abs(p.x * diag.y - p.y * diag.x))
            if best < 0.062 + ragged[(i * 3 + j) % len(ragged)]:
                cell(x0, x1, z0, z1, "livery_red")
    # The briquet (fire-steel) device at the crossing, 0.16 m wide: a bow and two
    # hooked ends in iron-black paint, with the flint below.
    for x0, x1, z0, z1 in ((-0.085, 0.085, 0.70, 0.7508), (-0.085, -0.052, 0.649, 0.70),
                           (0.052, 0.085, 0.649, 0.70), (-0.047, 0.047, 0.598, 0.649)):
        cell(x0, x1, z0, z1, "iron_binding")
    # 4 ball-shot dents and a few gesso chips showing poplar.
    for x0, x1, z0, z1 in ((0.15, 0.22, 1.04, 1.09), (-0.22, -0.15, 0.39, 0.445),
                              (0.15, 0.22, 0.24, 0.29), (-0.15, -0.085, 1.14, 1.19)):
        cell(x0, x1, z0, z1, "iron_binding")
    for x0, x1, z0, z1 in ((0.22, 0.29, 1.19, 1.24), (-0.29, -0.22, 0.09, 0.14),
                           (0.085, 0.15, 0.44, 0.50), (-0.29, -0.22, 0.85, 0.90),
                           (0.22, 0.29, 0.55, 0.60)):
        cell(x0, x1, z0, z1, "poplar")
    # Iron binding strip 2 cm round the rim (front, back and edges).
    for x0, x1 in ((-half - 0.01, -0.29), (0.29, half + 0.01)):
        cell(x0, x1, -1.0, 3.0, "iron_binding", front_only=False)
    for z0, z1 in ((-1.0, 0.06), (1.28, 3.0)):
        cell(-half - 0.01, half + 0.01, z0, z1, "iron_binding", front_only=False)
    # The back is bare poplar (inside the rim).
    paint.insert(0, {"mat": "poplar", "min": (cx - half, cy - 0.02, -1.0),
                     "max": (cx + half, cy + 0.2, 3.0)})

    ymid = cy + _PAV_T / 2 - _PAV_SAG
    fig.prop_bone("Pavise", "L", head=(cx, ymid, 1.22), tail=(cx, ymid, 0.10))
    pr = {"prop": True}
    parts = [Part("loft", (0, 0, 0), (1, 1, 1), mat="gesso_white", bone="Pavise",
                  extras={"rings": rings, "paint": paint, "bevel": False, **pr})]
    # Two spiked iron feet at the base corners, their roots buried in the board.
    for sx in (1.0, -1.0):
        x = cx + sx * 0.265
        yb = cy + (_pav_front(sx * 0.265) + _pav_back(sx * 0.265)) / 2
        parts.append(_rod((x, yb, 0.075), (x, yb, 0.0), 0.012, "iron_binding", "Pavise",
                          segments=4, taper=0.0, extras=dict(pr)))
    # Three rawhide grip loops on the back of the spine (ends sunk in the board).
    yb0 = cy + _pav_back(0.0)
    for z in (0.35, 0.75, 1.10):
        loop = [(cx - 0.045, yb0 - 0.006, z), (cx - 0.035, yb0 + 0.030, z),
                (cx + 0.035, yb0 + 0.030, z), (cx + 0.045, yb0 - 0.006, z)]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="leather", bone="Pavise",
                          segments=4, extras={"path": loop, "section": (0.006, 0.012),
                                              "up": (0, 0, 1), "smooth": True,
                                              "bevel": False, **pr}))
    # The hinged prop leg, swung down to the turf behind.
    hinge = Vector((cx, yb0 - 0.010, 0.86))
    foot = Vector((cx, yb0 + 0.30, 0.0))
    fig.add_bone("PaviseProp", hinge, foot, "Pavise")
    parts.append(_rod(hinge, foot + Vector((0, 0, 0.012)), 0.018, "poplar", "PaviseProp",
                      segments=6, rx=0.012, extras=dict(pr)))
    parts.append(_rod(hinge + Vector((-0.03, 0.012, 0.0)), hinge + Vector((0.03, 0.012, 0.0)),
                      0.009, "iron_binding", "PaviseProp", segments=6, extras=dict(pr)))
    # The stuck crossbow bolt, 0.35 m, buried in the outer side edge at 0.65 m.
    ex = cx + half
    root = Vector((ex - 0.012, cy + 0.02, 0.65))
    d = Vector((0.40, -0.90, 0.08)).normalized()   # sticking out toward the front
    tail = root + d * 0.35
    parts.append(_rod(root, tail, 0.0065, "poplar", "Pavise", segments=5, extras=dict(pr)))
    for ang in (0.0, 90.0):   # two crossed fletching vanes near the nock
        v = Vector((0.0, math.cos(math.radians(ang)), math.sin(math.radians(ang))))
        v = (v - d * v.dot(d)).normalized()
        a = tail - d * 0.085
        parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="leather", bone="Pavise",
                          segments=4, extras={"path": [tuple(a), tuple(tail - d * 0.01)],
                                              "sections": [(0.022, 0.0015), (0.012, 0.0015)],
                                              "up": tuple(v), "power": 4.0, "bevel": False,
                                              **pr}))
    return parts


def _falchion(fig: Human, pad: float) -> list[Part]:
    """0.75 m falchion in a leather scabbard on the left hip, hanging back: a broad
    single-edged blade (0.06 m widening to the clipped point) inside a scabbard of
    the same shape, a 0.11 m leather grip, iron cross and pommel. Its own bone
    under Hips (the rig swaps it to Hand.R when drawn). The scabbard's mouth is sunk
    into the coat so heat weighting can see it."""
    mouth = fig.surface(0.99, -30.0, pad=pad - 0.004)
    d = Vector((0.06, 0.40, -0.91)).normalized()
    tip = mouth + d * 0.62
    fig.add_bone("Falchion", mouth - d * 0.13, tip, "Hips")
    out = Vector((1.0, 0.0, 0.0))
    out = (out - d * out.dot(d)).normalized()
    path = [mouth + out * 0.004 + d * t for t in (0.0, 0.20, 0.42, 0.56, 0.62)]
    parts = [Part("sweep", (0, 0, 0), (1, 1, 1), mat="leather", bone="Falchion", segments=6,
                  extras={"path": [tuple(p) for p in path],
                          "sections": [(0.012, 0.034), (0.012, 0.038), (0.012, 0.045),
                                       (0.010, 0.040), (0.004, 0.012)],
                          "up": tuple(out), "power": 3.0, "rigid": True, "bevel": False,
                          "smooth": True})]
    guard = mouth - d * 0.012
    parts.append(_rod(guard - out * 0.05, guard + out * 0.05, 0.008, "iron_binding",
                      "Falchion", segments=6, extras={"rigid": True, "bevel": False}))
    grip_end = guard - d * 0.11
    parts.append(_rod(guard, grip_end, 0.014, "leather", "Falchion", segments=6,
                      extras={"rigid": True, "bevel": False, "smooth": True}))
    parts.append(Part("ico", tuple(grip_end - d * 0.012), (0.034, 0.034, 0.030),
                      mat="iron_binding", bone="Falchion", subdivisions=1,
                      extras={"rigid": True, "bevel": False}))
    return parts


def pavisier(entry: Entry):
    # Eyes 1.65 m -> stature 1.763 m; the open sallet's crown reaches 1.80 m.
    # 0.48 m shoulders. The pavise stands at his left front, his left fist on its
    # rim (placed by IK); the right arm hangs by the falchion side.
    h = 1.763
    pav = Vector((0.53, -0.12, 0.0))
    grip_l = Vector((pav.x - 0.19, pav.y + (_pav_front(-0.19) + _pav_back(-0.19)) / 2,
                     _PAV_Z1 + 0.015))
    fig = _ReachHuman(height=h, bulk=1.02, shoulders=0.48, stance=3.0,
                      arm_r=ArmPose(spread=10.0, swing=4.0, elbow=16.0),
                      reach={"L": grip_l}, pole={"L": Vector((1.0, 0.5, -0.9))})
    pad = 0.014
    white = {"mat": "gesso_white", "min": (-1.0, -1.0, -1.0), "max": (0.0, 1.0, 3.0)}
    mail = [{"mat": "mail", "min": (-1, -1, 1.425), "max": (1, 1, 3)},
            {"mat": "mail", "min": (-1, -1, -1), "max": (1, 1, 0.80)}]
    # Pied coat: white on his right (-X), red on his left, pleated skirt to 0.80 m;
    # the mail shirt's hem 4 cm below it and the mail standard round the neck are
    # painted bands of the same shell (no second torso layer under the coat).
    parts = [fig.torso_part("livery_red", pad=pad, hem=0.76, hem_flare=1.24, collar=0.075,
                            quilt=0.06, segments=36, paint=[white] + mail)]
    for side in ("L", "R"):
        # sleeves counterchanged: red on the white side, white on the red side
        sleeve = "gesso_white" if side == "L" else "livery_red"
        parts.append(fig.arm_part(side, sleeve, pad=0.008, paint=[
            {"mat": "mail", "min": (-1, -1, 1.425), "max": (1, 1, 3)}]))
        parts += fig.hand_part(side, "leather")
        parts.append(fig.leg_part(side, "hose", paint=[
            {"mat": "leather", "min": (-1, -1, -1), "max": (1, 1, 0.50)}]))
        parts.append(fig.foot_part(side, "leather", length=0.27, point=0.1))
        knee, ank = fig.joint(f"knee.{side}"), fig.joint(f"ankle.{side}")
        cuff = ank.lerp(knee, (0.495 - ank.z) / (knee.z - ank.z))
        parts.append(Part("cyl", tuple(cuff), (0.090, 0.094, 0.05), mat="leather",
                          bone=f"LowerLeg.{side}", segments=12, taper=1.12,
                          extras={"bevel": False, "smooth": True,
                                  "bones": [f"LowerLeg.{side}", f"UpperLeg.{side}"]}))
    parts += fig.head_part("skin", features="leather")
    # Open sallet: blackened, 0.24 m wide, face open from brow to chin, 0.18 m tail.
    parts += _sallet(fig, "sallet", "iron_binding", crown=1.80, rim_front=1.700,
                     rim_side=1.600, rim_back=1.560, back_reach=0.235, slit=None,
                     front_reach=0.128, half_w=0.121)

    # Belt 4 cm at 1.02 m, iron buckle; falchion on the left hip.
    parts.append(fig.band(1.02, "leather", height=0.04, pad=0.004, torso_pad=pad))
    buckle = fig.surface(1.02, -90.0, pad=pad + 0.008)
    parts.append(Part("torus", tuple(buckle), (0.05, 0.05, 0.045), mat="iron_binding",
                      bone="Hips", rot=(90, 0, 0), segments=4, rings=4, minor=0.2,
                      extras={"rigid": True, "bevel": False}))
    parts += _falchion(fig, pad)
    parts += _pavise(fig, pav, grip_l)

    # Review pose: the default test pose mirrored onto the free right arm, and the
    # left fist swinging the pavise a little forward, so the POSED views prove the
    # pavise follows the hand.
    pose = dict(figures.HUMAN_TEST_POSE)
    pose.pop("UpperArm.L"); pose.pop("LowerArm.L")
    pose.update({"UpperArm.R": (-80.0, 0.0, 0.0), "LowerArm.R": (-25.0, 0.0, 0.0),
                 "UpperArm.L": (-14.0, 0.0, 0.0)})
    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(pose),
        family_overrides={
            "sallet": {"rough": 0.45},
            "mail": {"rough": 0.5, "ridges": (0.010, 0.40)},
            "livery_red": {"rough": 0.9},
        },
        extra_families={
            "skin": {"name": "Skin", "base": "#9C7A5E", "rough": 0.7,
                     "notes": "The open face: the JSON lists no skin family (hex as the "
                              "handgunner's)."},
            "hose": {"name": "Dark wool hose", "base": "#3A2F26", "rough": 0.85,
                     "notes": "Build: 'dark wool hose'; hex from the halberdier's Hose, "
                              "the JSON lists none here."},
        },
        notes=[
            "The pavise (1.30 m) is a prop bone Pavise on Hand.L, left out of the 1.80 m "
            "height; PaviseProp is its hinge bone. In game the pavise re-parents to the "
            "world when planted.",
            "Sallet is its own bone under Head (detachable); Falchion is a bone under Hips.",
            "The saltire, briquet device, rim binding, dents and chips are painted cells "
            "on the pavise shell (a coarse, ragged staircase at this resolution).",
            "Not built: coat_skirt spring chains (the skirt rule hands the skirt to the "
            "thighs), the painted red sparks, mud on the lower 0.2 m, gesso crazing, "
            "damage states.",
        ])


BLUEPRINTS = {
    "sallet-halberdier": sallet_halberdier,
    "handgunner": handgunner,
    "gothic-knight": gothic_knight,
    "pavisier": pavisier,
}
