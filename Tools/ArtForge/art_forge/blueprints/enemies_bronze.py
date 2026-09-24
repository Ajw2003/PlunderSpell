"""Bronze Age enemies (docs/art/data/bronze.json, enemies).

Four humanoids on figures.Human, following the two samples in enemies_high.py:
the palace levy (patrol: figure-of-eight shield and a 2.40 m spear), the wall
slinger (ranged: sling on a four-bone chain), the Dendra champion (heavy: a bell of
bronze bands and a boar's-tusk cone) and the keeper of the flame (special: a yoke
of fire-pots and a censer). Bronze here is always warm brown cast/hammered bronze,
never orpiment; nothing carries verdigris or lapis.
"""

from __future__ import annotations

import math

from mathutils import Vector

from .. import figures
from ..figures import ArmPose, Human
from ..kit import Part, spline
from ..spec import Entry
from . import blueprint


# --------------------------------------------------------------------------------
# Shared helpers
# --------------------------------------------------------------------------------

def _sleeve(fig: Human, side: str, mat: str, reach: float, pad: float,
            flare: float = 1.0, paint: list | None = None) -> Part:
    """A short sleeve over a bare arm: from inside the chest (as arm_part starts)
    down the upper arm to `reach` (0..1 of the way to the elbow), open end capped.
    `flare` widens the cuff."""
    h, b = fig.h, fig.bulk
    s = figures.SIDES[side]
    shoulder, elbow = fig.joint(f"shoulder.{side}"), fig.joint(f"elbow.{side}")
    start = shoulder + Vector((-s * 0.036 * h, 0.0, -0.024 * h))
    up = (elbow - shoulder)
    pts = [start, shoulder, shoulder + up * (reach * 0.5), shoulder + up * reach]
    radii = [0.024, 0.030, 0.029, 0.027 * flare]
    secs = [(r * h * b + pad, (r * h * b + pad) * 0.92) for r in radii]
    return Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=f"UpperArm.{side}",
                segments=fig.segments, extras={
                    "path": [tuple(p) for p in pts], "sections": secs,
                    "up": (0.0, 1.0, 0.0), "smooth": True, "bevel": False,
                    "paint": paint or [],
                    "bones": [f"Shoulder.{side}", f"UpperArm.{side}", "Chest"]})


def _hem_band(fig: Human, hem: float, hem_flare: float, pad: float, mat: str,
              z0: float, z1: float, over: float = 0.004) -> Part:
    """A border stripe laid just outside a torso_part skirt, between hem + z0 and
    hem + z1 (the torso loft has no ring there to paint on). Same flare law as
    torso_part, and the same skirt rule so it follows a stride."""
    n = fig.segments * 2
    span = fig.crotch_z - hem
    rings = []
    for dz in (z0, z1):
        f = dz / (span * 0.5)
        flare = hem_flare + (1.0 + (hem_flare - 1.0) * 0.55 - hem_flare) * f
        rings.append(fig.torso_ring(0, 0.092, 0.064, 0.004, n, pad + over, 0.0,
                                    z=hem + dz, flare=flare))
    skirt = {"top": fig.hip_z + 0.02 * fig.h, "bottom": hem, "strength": 0.8}
    return Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone="Spine", extras={
        "rings": rings, "smooth": True, "bevel": False, "skirt": skirt,
        "bones": ["Hips", "Spine", "UpperLeg.L", "UpperLeg.R"]})


def _torso_strap(fig: Human, mat: str, pts_spec: list[tuple[float, float]], pad: float,
                 width: float = 0.025, thick: float = 0.004,
                 bones=("Hips", "Spine", "Chest")) -> Part:
    """A strap laid on the torso through (z, angle_deg) points (see Human.surface)."""
    pts = [tuple(fig.surface(z, a, pad=pad)) for z, a in pts_spec]
    return Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, bone="Chest", segments=4,
                extras={"path": pts, "section": (thick, width), "smooth": True,
                        "bevel": False, "bones": list(bones)})


def _over_shoulder(fig: Human, side: str, pad: float, n: int = 5) -> list[tuple]:
    """Points arching over the top of one shoulder, front to back, for a strap."""
    s = figures.SIDES[side]
    x = s * 0.075 * fig.h
    z_top = fig.shoulder_z + 0.018 * fig.h
    hd = fig.torso_dims(fig.shoulder_z - 0.02)[1] + pad
    c = fig.lean((0.0, 0.0, fig.shoulder_z))
    out = []
    for k in range(n):
        a = math.pi * k / (n - 1)          # 0 = front, pi = back
        out.append((x, c.y - math.cos(a) * hd, z_top - (1.0 - math.sin(a)) * 0.05))
    return out


def _hair(fig: Human, mat: str, length_z: float, width: float = 1.0,
          back: float = 1.0) -> Part:
    """A hair mass: a shell over the back and sides of the skull falling to
    `length_z` behind the neck. Skinned to Head/Neck (and Chest if it reaches)."""
    h = fig.h
    rings = []
    rows = [(0.990, 0.030, 0.038), (0.965, 0.047, 0.058), (0.935, 0.050, 0.060),
            (0.900, 0.048, 0.056)]
    z_rows = [(zf * h, hw * h * width, hd * h) for zf, hw, hd in rows]
    zs = [z_rows[-1][0] - (z_rows[-1][0] - length_z) * t for t in (0.5, 1.0)]
    z_rows += [(zs[0], 0.052 * h * width, 0.040 * h), (zs[1], 0.050 * h * width, 0.030 * h)]
    for i, (z, hw, hd) in enumerate(z_rows):
        c = fig.lean((0.0, (0.012 * h if i < 4 else 0.040 * h * back), z))
        ring = []
        for j in range(10):
            a = math.pi * (-0.05 + 1.1 * j / 9)             # an arc round the back
            ring.append((c.x + math.cos(a) * hw, c.y + math.sin(a) * hd * 1.02, z))
        for j in range(4):                                   # back across the inside
            a = math.pi * (1.05 - 1.1 * j / 3)
            k = 0.80 if i < 4 else 0.55
            ring.append((c.x + math.cos(a) * hw * k, c.y + math.sin(a) * hd * k, z))
        rings.append(ring)
    return Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone="Head", extras={
        "rings": rings, "smooth": True, "bevel": False,
        "bones": ["Head", "Neck", "Chest"]})


# --------------------------------------------------------------------------------
# Palace Levy (patrol) — shield like a pinched hourglass, spear line over the head.
# --------------------------------------------------------------------------------

def _leather_cap(fig: Human, top_z: float) -> list[Part]:
    """Boiled-leather skull-cap 0.21 m dia. x 0.12 m, six raised gore seams, a
    rolled brim, a small bronze boss knob. Its own `Cap` bone (detachable)."""
    h = fig.h
    dome_h, r = 0.115, 0.104
    base = fig.lean((0.0, 0.004 * h, top_z - dome_h - 0.012))
    fig.add_bone("Cap", base, base + Vector((0, 0, dome_h)), "Head")
    prof = [(0.0, dome_h), (0.030, dome_h - 0.004), (0.060, dome_h - 0.018),
            (0.085, dome_h - 0.042), (0.100, dome_h - 0.075), (r, 0.018),
            (r + 0.012, 0.012), (r + 0.014, 0.0), (r + 0.004, -0.008), (r - 0.010, -0.004)]
    parts = [Part("lathe", tuple(base), (0.94, 1.0, 1.0), mat="boiled_leather", bone="Cap",
                  segments=18, extras={"profile": prof, "rigid": True, "smooth": True,
                                       "bevel": False})]
    for k in range(6):                                  # gore seams
        a = 2 * math.pi * k / 6 + math.pi / 6
        path = []
        for t in (0.12, 0.35, 0.58, 0.80, 0.97):
            ang = t * math.pi / 2
            rr = r * math.cos(ang) + 0.003
            path.append(tuple(base + Vector((math.cos(a) * rr * 0.94, math.sin(a) * rr,
                                             0.018 + math.sin(ang) * (dome_h - 0.018)))))
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="boiled_leather", bone="Cap",
                          segments=4, extras={"path": path, "section": (0.004, 0.004),
                                              "rigid": True, "smooth": True, "bevel": False}))
    parts.append(Part("sphere", tuple(base + Vector((0, 0, dome_h + 0.008))),
                      (0.030, 0.030, 0.026), mat="cast_bronze", bone="Cap", segments=8,
                      rings=5, extras={"rigid": True, "bevel": False}))
    # chin strap: down each cheek to under the chin
    for s in (1.0, -1.0):
        pts = [(s * 0.085, base.y - 0.004, base.z + 0.004),
               (s * 0.070, base.y - 0.030, fig.eye_z - 0.02),
               (s * 0.052, base.y - 0.055, fig.chin_z + 0.035),
               (s * 0.020, base.y - 0.075, fig.chin_z + 0.005)]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="boiled_leather", bone="Head",
                          segments=4, extras={"path": [tuple(fig.lean(p)) for p in pts],
                                              "section": (0.003, 0.007), "rigid": True,
                                              "smooth": True, "bevel": False}))
    return parts


def _figure_eight_shield(fig: Human) -> list[Part]:
    """Oxhide figure-of-eight: 1.30 m tall, 0.72 m at the lobes, 0.46 m at the
    waist; bowed forward about both axes; a raised wicker spine and two bowed
    side-bars; dappled patches. Rigid on ShieldRoot (child of LowerArm.L)."""
    g = fig.grip("L")
    H = 1.30
    zc = 0.33 + H / 2                               # bottom 0.33 m, as the concept
    n = 13                                          # points across the face
    bow, vbow, thick = 0.12, 0.06, 0.022
    # The fist holds a grip just behind the centre of the face: put the inner
    # surface's centre 4 cm in front of it, so the bowed edges curl back round
    # the body.
    cx = g.x + 0.04
    v_g = vbow * (1.0 - ((g.z - zc) / (H / 2)) ** 2)
    back_y = g.y - 0.045 + bow + v_g
    fig.add_bone("ShieldRoot", g, g + Vector((0.0, -0.07, 0.25)), "LowerArm.L")
    # half-width along the height (z relative to the centre): two lobes, a waist
    ctrl = [(-0.65, 0.0), (-0.635, 0.13), (-0.60, 0.24), (-0.52, 0.32), (-0.40, 0.36),
            (-0.27, 0.34), (-0.14, 0.27), (-0.05, 0.232), (0.0, 0.23)]
    ctrl = ctrl + [(-z, w) for z, w in reversed(ctrl[:-1])]
    prof = spline(ctrl, 2)
    rings = []
    for zr, hw in prof:
        z = zc + zr
        v = vbow * (1.0 - (zr / (H / 2)) ** 2)
        if hw < 1e-4:
            rings.append([(cx, back_y - v - bow * 0.5, z)])
            continue
        outer, inner = [], []
        for j in range(n):
            u = -1.0 + 2.0 * j / (n - 1)
            x = cx + u * hw
            y = back_y - v - bow * (1.0 - u * u) * min(1.0, hw / 0.23)
            outer.append((x, y - thick, z))
            inner.append((x, y, z))
        rings.append(outer + list(reversed(inner)))
    # dappled patches (the JSON's #4A3526 hand-painted hide), in world metres
    # Each patch is a blob of three overlapping boxes so it does not read square.
    blobs = [(-0.16, 0.44, 0.20), (0.14, 0.34, 0.16), (-0.02, 0.02, 0.15),
             (0.18, -0.20, 0.14), (-0.18, -0.40, 0.18), (0.02, 0.56, 0.10),
             (-0.20, 0.20, 0.10), (0.08, -0.46, 0.10)]
    paint = []
    for bx, bz, d in blobs:
        for ox, oz, sx, sz in ((0.0, 0.0, 0.5, 0.36), (0.12, 0.10, 0.34, 0.5),
                               (-0.10, -0.08, 0.30, 0.44)):
            x0, z0 = bx + ox * d - sx * d, bz + oz * d - sz * d
            paint.append({"mat": "oxhide_dapple", "min": (cx + x0, -2.0, zc + z0),
                          "max": (cx + x0 + 2 * sx * d, 2.0, zc + z0 + 2 * sz * d)})
    # scuffed pale rim: the outermost column on both faces
    parts = [Part("loft", (0, 0, 0), (1, 1, 1), mat="oxhide", bone="ShieldRoot", extras={
        "rings": rings, "smooth": True, "bevel": False, "paint": paint, "prop": True})]
    # raised wicker spine down the middle of the face, and the two bowed side-bars
    spine = []
    for zr in [-0.60 + 1.2 * k / 10 for k in range(11)]:
        v = vbow * (1.0 - (zr / (H / 2)) ** 2)
        spine.append((cx, back_y - v - bow - thick - 0.006, zc + zr))
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="oxhide", bone="ShieldRoot",
                      segments=6, extras={"path": spine, "section": (0.012, 0.012),
                                          "smooth": True, "bevel": False, "prop": True}))
    for zr in (0.10, -0.10):
        bar = []
        hw = 0.24
        for j in range(7):
            u = -0.92 + 1.84 * j / 6
            v = vbow * (1.0 - (zr / (H / 2)) ** 2)
            y = back_y - v - bow * (1.0 - u * u) - thick - 0.005
            bar.append((cx + u * hw, y, zc + zr + 0.06 * (1 - u * u) * (1 if zr > 0 else -1)))
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="oxhide", bone="ShieldRoot",
                          segments=6, extras={"path": bar, "section": (0.010, 0.010),
                                              "smooth": True, "bevel": False, "prop": True}))
    # central wooden handgrip behind the face, through the fist
    parts.append(Part("cyl", (g.x, g.y, g.z), (0.03, 0.03, 0.16), mat="ash_haft",
                      bone="ShieldRoot", segments=6, extras={"prop": True, "bevel": False}))
    face_y = back_y - v_g - bow * (1.0 - ((g.x - cx) / 0.23) ** 2)
    for dz in (0.07, -0.07):
        parts.append(Part("box", (g.x, (g.y + face_y) / 2, g.z + dz),
                          (0.03, abs(g.y - face_y) + 0.02, 0.025), mat="ash_haft",
                          bone="ShieldRoot", extras={"prop": True, "bevel": False}))
    return parts


def _levy_spear(fig: Human) -> list[Part]:
    """2.40 m: ash shaft 3.5 cm, 0.26 m leaf-shaped bronze head with midrib and
    socket, 0.08 m bronze butt-spike, 0.25 m linen grip wrap at 1.05 m."""
    g = fig.grip("R")
    x, y = g.x, g.y
    overall, head_len, butt = 2.40, 0.26, 0.08
    socket_z = overall - head_len
    fig.prop_bone("SpearRoot", "R", head=g, tail=(x, y, overall))
    prop = {"prop": True}
    parts = [
        Part("cyl", (x, y, (butt + socket_z) / 2), (0.035, 0.035, socket_z - butt),
             mat="ash_haft", bone="SpearRoot", segments=8,
             extras={**prop, "smooth": True, "bevel": False}),
        Part("cone", (x, y, butt / 2), (0.036, 0.036, butt), mat="cast_bronze",
             bone="SpearRoot", segments=8, rot=(180.0, 0.0, 0.0), taper=0.15,
             extras={**prop, "bevel": False}),
        Part("cyl", (x, y, g.z + 0.03), (0.041, 0.041, 0.25), mat="linen",
             bone="SpearRoot", segments=8, extras={**prop, "smooth": True, "bevel": False}),
        Part("cyl", (x, y, socket_z + 0.035), (0.038, 0.038, 0.07), mat="cast_bronze",
             bone="SpearRoot", segments=8, taper=0.75, extras={**prop, "bevel": False}),
    ]
    # leaf blade, flat in XZ so the leaf reads in the front view; a lathe-free
    # prism plus a thin midrib cylinder.
    L = head_len - 0.05
    leaf = [(0.0, 0.0), (0.016, 0.02), (0.025, 0.07), (0.024, 0.11), (0.015, 0.16),
            (0.0, L), (-0.015, 0.16), (-0.024, 0.11), (-0.025, 0.07), (-0.016, 0.02)]
    parts.append(Part("prism", (x, y, socket_z + 0.05), (1, 1, 0.008), mat="cast_bronze",
                      bone="SpearRoot", rot=(90.0, 0.0, 0.0),
                      extras={"outline": leaf, **prop}))
    parts.append(Part("cyl", (x, y, socket_z + 0.05 + L * 0.45), (0.012, 0.016, L * 0.9),
                      mat="cast_bronze", bone="SpearRoot", segments=6, taper=0.3,
                      extras={**prop, "bevel": False}))
    return parts


def palace_levy(entry: Entry):
    # 1.70 m to the cap crown, 1.66 m bare head: stature 1.66. Lean labourer,
    # 0.42 m shoulders. Spear hand (R) forward at the hip; shield hand (L) level.
    fig = Human(height=1.66, bulk=0.92, shoulders=0.42,
                arm_r=ArmPose(spread=15.0, swing=4.0, elbow=58.0),
                arm_l=ArmPose(spread=13.0, swing=2.0, elbow=45.0))
    pad = 0.010
    hem = 0.55
    # knee-length linen tunic, haematite-free: the JSON gives the border stripe as
    # "haematite-red", but lists no red family for the levy -> extra family below.
    parts = [fig.torso_part("linen", pad=pad, hem=hem, hem_flare=1.30, collar=0.0,
                            quilt=0.0, segments=24)]
    parts.append(_hem_band(fig, hem, 1.30, pad, "hem_red", 0.012, 0.042))
    for side in ("L", "R"):
        parts.append(fig.arm_part(side, "weathered_skin"))
        parts.append(_sleeve(fig, side, "linen", reach=0.50, pad=0.012, flare=1.05))
        parts += fig.hand_part(side, "weathered_skin")
        parts.append(fig.leg_part(side, "weathered_skin"))
        parts.append(fig.foot_part(side, "boiled_leather", length=0.27, point=0.0))
        # sandal ankle thongs
        ank = fig.joint(f"ankle.{side}")
        for dz in (0.025, 0.065):
            parts.append(Part("cyl", (ank.x, ank.y + 0.004, ank.z + dz), (0.070, 0.074, 0.012),
                              mat="boiled_leather", bone=f"LowerLeg.{side}", segments=10,
                              extras={"bevel": False, "smooth": True,
                                      "bones": [f"LowerLeg.{side}", f"Foot.{side}"]}))
    parts += fig.head_part("weathered_skin", face="weathered_skin", features="hair")
    parts.append(_hair(fig, "hair", length_z=fig.neck_z - 0.04))
    # short beard: a rounded wedge under the jaw
    chin = fig.lean((0.0, -0.068 * fig.h, fig.chin_z + 0.020))
    parts.append(Part("sphere", tuple(chin), (0.100, 0.070, 0.085), mat="hair",
                      bone="Head", segments=10, rings=6, rot=(-15.0, 0.0, 0.0),
                      extras={"rigid": True, "bevel": False}))
    parts += _leather_cap(fig, 1.70)

    # 5 cm belt, bronze-free frame buckle in leather
    parts.append(fig.band(fig.belt_z, "boiled_leather", height=0.05, pad=0.008,
                          torso_pad=pad))
    buckle = fig.surface(fig.belt_z, -90.0, pad=pad + 0.012)
    parts.append(Part("box", tuple(buckle), (0.045, 0.012, 0.055), mat="boiled_leather",
                      bone="Hips", extras={"rigid": True}))
    # telamon strap: from the shield side of the waist, up across the chest to the
    # right shoulder, down the back.
    front = [(fig.belt_z + 0.06, -40.0), (fig.belt_z + 0.18, -70.0), (fig.chest_z, -105.0),
             (fig.chest_z + 0.12, -130.0)]
    back = [(fig.chest_z + 0.12, 130.0), (fig.chest_z, 105.0), (fig.belt_z + 0.18, 70.0),
            (fig.belt_z + 0.06, 40.0)]
    pts = ([tuple(fig.surface(z, a, pad=pad + 0.004)) for z, a in front]
           + [(-p[0], p[1], p[2]) for p in _over_shoulder(fig, "L", pad + 0.006)]
           + [tuple(fig.surface(z, a, pad=pad + 0.004)) for z, a in back])
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="boiled_leather", bone="Chest",
                      segments=4, extras={"path": pts, "section": (0.004, 0.025),
                                          "smooth": True, "bevel": False,
                                          "bones": ["Spine", "Chest", "Shoulder.R"]}))
    # water skin (0.20 m ovoid) and bread bag on the right hip
    ws = fig.surface(fig.belt_z - 0.14, -140.0, pad=pad + 0.05)
    parts.append(Part("sphere", tuple(ws), (0.12, 0.08, 0.20), mat="boiled_leather",
                      bone="Hips", segments=10, rings=7, rot=(0, 10, 0),
                      extras={"rigid": True, "bevel": False}))
    parts.append(Part("cyl", tuple(ws + Vector((0, 0, 0.11))), (0.03, 0.03, 0.05),
                      mat="boiled_leather", bone="Hips", segments=6,
                      extras={"rigid": True, "bevel": False}))
    bag = fig.surface(fig.belt_z - 0.10, 150.0, pad=pad + 0.035)
    parts.append(Part("box", tuple(bag), (0.10, 0.06, 0.12), mat="linen", bone="Hips",
                      rot=(0, 0, 30.0), extras={"rigid": True}))
    # dagger in its leather scabbard on the left hip (bone pommel)
    sc = fig.surface(fig.belt_z - 0.16, 10.0, pad=pad + 0.02)
    parts.append(Part("cyl", tuple(sc), (0.05, 0.022, 0.30), mat="boiled_leather",
                      bone="Hips", rot=(0, -12, 0), segments=6, taper=0.4,
                      extras={"rigid": True}))
    hilt = sc + Vector((-0.02, 0.0, 0.19))
    parts.append(Part("cyl", tuple(hilt), (0.022, 0.022, 0.09), mat="cast_bronze",
                      bone="Hips", rot=(0, -12, 0), segments=6, extras={"rigid": True,
                                                                        "bevel": False}))
    parts.append(Part("sphere", tuple(hilt + Vector((-0.01, 0, 0.055))), (0.035, 0.035, 0.03),
                      mat="bone_pommel", bone="Hips", segments=8, rings=5,
                      extras={"rigid": True, "bevel": False}))

    parts += _figure_eight_shield(fig)
    parts += _levy_spear(fig)

    # Review pose: the test pose, but the shield arm comes up to guard height
    # (guard_stance) instead of to the face, which would lift 1.3 m of shield
    # overhead like a parasol.
    pose = dict(figures.HUMAN_TEST_POSE)
    pose["UpperArm.L"] = (-40.0, 0.0, 0.0)
    pose["LowerArm.L"] = (-15.0, 0.0, 0.0)
    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(pose),
        family_overrides={
            "cast_bronze": {"rough": 0.45, "wear_to": "#6E4A28", "wear_amount": 0.3},
            "linen": {"grain": 0.25, "wear_to": "#A89A7A", "wear_amount": 0.3},
            "oxhide": {"grain": 0.2},
        },
        extra_families={
            "oxhide_dapple": {"name": "Oxhide dapple", "base": "#4A3526", "rough": 0.75,
                              "notes": "The oxhide's dark patches; the JSON gives #4A3526 "
                                       "in the Oxhide notes, not as its own family."},
            "hem_red": {"name": "Haematite red", "base": "#8E3F2C", "rough": 0.85,
                        "notes": "The tunic's 3 cm haematite-red border stripe (build "
                                 "bullet); the levy's material list has no red."},
            "hair": {"name": "Dark hair", "base": "#2B231B", "rough": 0.8,
                     "notes": "Dark shoulder-length hair and short beard (build bullet); "
                              "no hair family in the levy's list. Hex borrowed from the "
                              "champion's horsehair."},
            "bone_pommel": {"name": "Bone pommel", "base": "#E0D6BE", "rough": 0.55,
                            "notes": "The dagger's bone pommel (build bullet); hex is the "
                                     "champion's 'Bone and ivory'."},
        },
        notes=[
            "Spear (2.40 m, SpearRoot on Hand.R) and shield (ShieldRoot on LowerArm.L) "
            "are props, left out of the 1.70 m height check.",
            "Cap is its own bone under Head (detachable).",
            "Dagger hangs from the belt on the left hip, not a separate baldric; the one "
            "diagonal strap is the shield's telamon.",
            "Not built: telamon_strap 3-bone chain, tunic_hem x4 cloth bones, waterskin "
            "jiggle bone; bread bag is a plain box; sandals are shoes with two thongs.",
        ])


BLUEPRINTS = {
    "palace-levy": palace_levy,
}
