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

from mathutils import Matrix, Vector

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


# --------------------------------------------------------------------------------
# Wall Slinger (ranged) — slim, bare-chested, sling cord hanging from the right
# fist, bulging hip pouch, bandana with two tails, short wrapped kilt.
# --------------------------------------------------------------------------------

def _bar(a, b, r: float, mat: str, bone: str, segments: int = 8, **extras) -> Part:
    """A short straight tube from a to b (bracers, thongs, hilts)."""
    return Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, segments=segments,
                extras={"path": [tuple(a), tuple(b)], "section": (r, r), "smooth": True,
                        "bevel": False, **extras})


def _curly_cap(fig: Human, mat: str, top_z: float, tilt: float = -18.0) -> Part:
    """Short hair as a tilted dome over the skull: low at the nape, high over the
    forehead so the face stays clear."""
    h = fig.h
    c = fig.lean((0.0, 0.010 * h, 0.938 * h))
    dome = top_z - c.z
    prof = [(0.0, dome), (0.030, dome - 0.004), (0.058, dome - 0.020), (0.078, dome - 0.050),
            (0.086, dome - 0.085), (0.084, 0.0), (0.070, -0.012)]
    return Part("lathe", tuple(c), (0.94, 1.12, 1.0), mat=mat, bone="Head", segments=14,
                rot=(tilt, 0.0, 0.0),
                extras={"profile": prof, "rigid": True, "smooth": True, "bevel": False})


def _pouch(fig: Human, name: str, at: Vector, dims: tuple, stones: int,
           mat: str = "rawhide") -> list[Part]:
    """A bulging leather bag on its own jiggle bone (child of Hips) with a rolled
    mouth and `stones` river stones showing in it."""
    w, d, hgt = dims
    fig.add_bone(name, at + Vector((0, 0, hgt * 0.5)), at - Vector((0, 0, hgt * 0.4)), "Hips")
    prof = [(0.0, -hgt * 0.5), (w * 0.30, -hgt * 0.48), (w * 0.47, -hgt * 0.30),
            (w * 0.50, 0.0), (w * 0.46, hgt * 0.30), (w * 0.40, hgt * 0.44),
            (w * 0.42, hgt * 0.50), (w * 0.34, hgt * 0.50)]
    parts = [Part("lathe", tuple(at), (1.0, d / w, 1.0), mat=mat, bone=name, segments=12,
                  extras={"profile": prof, "rigid": True, "smooth": True, "bevel": False})]
    for k in range(stones):
        a = 2 * math.pi * k / max(1, stones) + 0.4
        p = at + Vector((math.cos(a) * w * 0.16, math.sin(a) * d * 0.16, hgt * 0.52))
        parts.append(Part("sphere", tuple(p), (0.05, 0.036, 0.034), mat="river_stone",
                          bone=name, rot=(0, 0, math.degrees(a)), segments=8, rings=5,
                          extras={"rigid": True, "bevel": False}))
    return parts


def _sling(fig: Human) -> list[Part]:
    """Two braided cords 0.80 m from the right fist to a 0.12 x 0.06 m leather
    cradle holding a stone; a finger loop on the retained cord. A four-bone chain
    (Sling1..Sling4) off Hand.R; in idle it hangs from the fist."""
    g = fig.grip("R")
    top = g + Vector((0.0, 0.0, -0.035))
    cradle = Vector((g.x - 0.05, g.y - 0.03, 0.17))
    chain = [top.lerp(cradle, t) for t in (0.0, 0.25, 0.5, 0.75, 1.0)]
    chain[-1] = cradle - Vector((0, 0, 0.03))
    fig.prop_bone("Sling1", "R", chain[0], chain[1])
    for i in range(2, 5):
        fig.add_bone(f"Sling{i}", chain[i - 1], chain[i], f"Sling{i - 1}")
    bones = ["Sling1", "Sling2", "Sling3", "Sling4"]
    parts = []
    for s in (1.0, -1.0):
        end = cradle + Vector((s * 0.055, 0.0, 0.012))
        path = []
        for k in range(9):
            t = k / 8
            p = top.lerp(end, t)
            p.x += s * 0.012 * math.sin(math.pi * t)    # the two cords part a little
            path.append(tuple(p))
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="braided_wool_cord",
                          bone="Sling2", segments=5, extras={
                              "path": path, "section": (0.0045, 0.0045), "smooth": True,
                              "bevel": False, "bones": bones}))
    # cradle: a shallow leather cup open upward, the stone sitting in it
    parts.append(Part("sphere", tuple(cradle), (0.12, 0.06, 0.035), mat="rawhide",
                      bone="Sling4", segments=10, rings=5,
                      extras={"rigid": True, "bevel": False}))
    parts.append(Part("sphere", tuple(cradle + Vector((0, 0, 0.018))), (0.05, 0.036, 0.034),
                      mat="river_stone", bone="Sling4", segments=8, rings=5,
                      extras={"rigid": True, "bevel": False}))
    # finger loop round the fist
    parts.append(Part("torus", tuple(g + Vector((0, 0, -0.01))), (0.07, 0.07, 0.07),
                      mat="braided_wool_cord", bone="Sling1", rot=(0, 90, 0), segments=10,
                      rings=4, minor=0.1, extras={"rigid": True, "bevel": False}))
    return parts


def wall_slinger(entry: Entry):
    # 1.66 m to the crown of the bandana/curls; skull 1.62 m. Wiry youth, 0.40 m
    # shoulders; both arms hang (the sling dangles from the right fist in idle).
    fig = Human(height=1.62, bulk=0.88, shoulders=0.40,
                arm_r=ArmPose(spread=12.0, swing=3.0, elbow=14.0),
                arm_l=ArmPose(spread=11.0, swing=2.0, elbow=18.0))
    h = fig.h
    kilt_top = fig.belt_z
    hem = kilt_top - 0.42
    # Bare torso; the wrapped kilt is the same loft painted linen below the belt,
    # with its skirt flare and skirt skinning rule.
    parts = [fig.torso_part("weathered_skin", hem=hem, hem_flare=1.22, segments=24,
                            paint=[{"mat": "linen", "min": (-1, -1, -1),
                                    "max": (1, 1, kilt_top)}])]
    parts.append(_hem_band(fig, hem, 1.22, 0.0, "haematite_red", 0.004, 0.044, over=0.003))
    for side in ("L", "R"):
        parts.append(fig.arm_part(side, "weathered_skin"))
        parts += fig.hand_part(side, "weathered_skin")
        parts.append(fig.leg_part(side, "weathered_skin"))
        parts.append(fig.foot_part(side, "rawhide", length=0.25, point=0.0))
        # sandal thongs to mid-calf
        ank, knee = fig.joint(f"ankle.{side}"), fig.joint(f"knee.{side}")
        for t, r in ((0.06, 0.036), (0.30, 0.037), (0.55, 0.043)):
            p = ank.lerp(knee, t)
            parts.append(Part("cyl", (p.x, p.y + 0.004, p.z), (r * 2 * fig.bulk ** 0.5 + 0.004,
                                                               r * 2 + 0.006, 0.012),
                              mat="rawhide", bone=f"LowerLeg.{side}", segments=10,
                              extras={"bevel": False, "smooth": True,
                                      "bones": [f"LowerLeg.{side}", f"Foot.{side}"]}))
    parts += fig.head_part("weathered_skin", face="weathered_skin", features="hair")
    parts.append(_curly_cap(fig, "hair", 1.662))

    # Bandana: a rolled linen band round the brow, knotted at the back, two 0.20 m
    # tails on their own bones.
    bc = fig.lean((0.0, 0.008 * h, 0.958 * h))
    band = [(0.084, -0.024), (0.100, -0.022), (0.106, -0.008), (0.107, 0.008),
            (0.101, 0.022), (0.084, 0.024)]
    parts.append(Part("lathe", tuple(bc), (0.84, 1.0, 1.0), mat="linen", bone="Head",
                      rot=(-16.0, 0.0, 0.0), segments=18,
                      extras={"profile": band, "rigid": True, "smooth": True,
                              "bevel": False}))
    knot = bc + Vector((0.0, 0.110, -0.024))
    parts.append(Part("sphere", tuple(knot), (0.05, 0.035, 0.04), mat="linen", bone="Head",
                      segments=8, rings=5, extras={"rigid": True, "bevel": False}))
    for s, side in ((1.0, "L"), (-1.0, "R")):
        a = knot + Vector((s * 0.012, 0.010, -0.010))
        tip = a + Vector((s * 0.05, 0.05, -0.19))
        bone = fig.add_bone(f"BandanaTail.{side}", a, tip, "Head")
        path = [tuple(a.lerp(tip, t) + Vector((0, 0.02 * math.sin(math.pi * t), 0)))
                for t in (0.0, 0.33, 0.66, 1.0)]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="linen", bone=bone, segments=4,
                          extras={"path": path, "section": (0.004, 0.018),
                                  "up": (0, 1, 0), "smooth": True, "bevel": False,
                                  "rigid": True}))

    # 4 cm belt; front overlap edge of the kilt with six tassels
    parts.append(fig.band(fig.belt_z, "rawhide", height=0.04, pad=0.006))
    for k in range(6):
        z = kilt_top - 0.06 - k * 0.063
        f = max(0.0, min(1.0, (z - hem) / (fig.crotch_z - hem)))
        p = fig.surface(max(z, fig.crotch_z), -62.0, pad=0.02 + (1 - f) * 0.05)
        p.z = z
        parts.append(Part("cone", tuple(p - Vector((0, 0, 0.04))), (0.018, 0.018, 0.08),
                          mat="linen", bone="Hips", rot=(180.0, 0.0, 0.0), segments=5,
                          taper=0.2, extras={"bevel": False, "bones": ["Hips",
                                             "UpperLeg.L", "UpperLeg.R"]}))
    # linen baldric: left hip, up across the chest, over the right shoulder, down
    # the back to the left hip.
    front = [(fig.belt_z + 0.04, -20.0), (fig.belt_z + 0.16, -55.0), (fig.chest_z, -100.0),
             (fig.chest_z + 0.12, -128.0)]
    back = [(fig.chest_z + 0.12, 128.0), (fig.chest_z, 100.0), (fig.belt_z + 0.16, 55.0),
            (fig.belt_z + 0.04, 20.0)]
    pts = ([tuple(fig.surface(z, a, pad=0.004)) for z, a in front]
           + [(-p[0], p[1], p[2]) for p in _over_shoulder(fig, "L", 0.006)]
           + [tuple(fig.surface(z, a, pad=0.004)) for z, a in back])
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="linen", bone="Chest", segments=4,
                      extras={"path": pts, "section": (0.004, 0.030), "smooth": True,
                              "bevel": False, "bones": ["Spine", "Chest", "Shoulder.R"]}))
    # stone pouches: main on the right hip (0.22 x 0.14 x 0.24), baldric pouch
    # on the left hip (0.16)
    pr = fig.surface(fig.belt_z - 0.14, -150.0, pad=0.085)
    parts += _pouch(fig, "Pouch.R", pr, (0.20, 0.14, 0.24), 3)
    pl = fig.surface(fig.belt_z - 0.11, -25.0, pad=0.075)
    parts += _pouch(fig, "Pouch.L", pl, (0.15, 0.11, 0.16), 2)
    # rawhide bracer, laced, on the left forearm (0.14 m)
    el, wr = fig.joint("elbow.L"), fig.joint("wrist.L")
    b0, b1 = wr.lerp(el, 0.08), wr.lerp(el, 0.08 + 0.14 / (el - wr).length)
    parts.append(_bar(b0, b1, 0.036 * fig.bulk ** 0.5, "rawhide", "LowerArm.L", segments=10,
                      bones=["LowerArm.L", "Hand.L"]))
    # bronze knife in the belt at the back
    kb = fig.surface(fig.belt_z - 0.02, 70.0, pad=0.02)
    parts.append(_bar(kb + Vector((0.0, 0.0, -0.14)), kb + Vector((0.02, 0.0, 0.03)),
                      0.018, "rawhide", "Hips", segments=6, rigid=True))
    parts.append(_bar(kb + Vector((0.02, 0.0, 0.03)), kb + Vector((0.03, 0.0, 0.13)),
                      0.014, "knife_bronze", "Hips", segments=6, rigid=True))

    parts += _sling(fig)

    pose = dict(figures.HUMAN_TEST_POSE)
    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(pose),
        family_overrides={
            "linen": {"grain": 0.25, "wear_to": "#A89A7A", "wear_amount": 0.3},
            "rawhide": {"grain": 0.2},
        },
        extra_families={
            "hair": {"name": "Dark hair", "base": "#2B231B", "rough": 0.8,
                     "notes": "Short curly dark hair (build bullet); no hair family in "
                              "the slinger's list."},
            "knife_bronze": {"name": "Cast bronze", "base": "#9B6A38", "rough": 0.45,
                             "metal": 1.0,
                             "notes": "The 0.30 m bronze knife (build bullet, 'warm brown'); "
                                      "the slinger's list has no bronze. Hex from the levy's "
                                      "cast bronze."},
        },
        notes=[
            "Sling is a four-bone chain Sling1..Sling4 off Hand.R, hanging in idle; the "
            "cords are weighted along the chain, cradle and stone rigid on Sling4.",
            "Kilt is the torso loft painted linen below the belt (so it takes the skirt "
            "rule), with a separate haematite border band; overlap edge shown by six "
            "tassels only.",
            "Bones added: BandanaTail.L/R, Pouch.R/L (jiggle). Not built: kilt_front/"
            "kilt_back cloth bones; spare sling bullets as separate projectile mesh.",
        ])


# --------------------------------------------------------------------------------
# Dendra Champion (heavy) — a cone on a bell: stacked flaring bronze bands, huge
# domed shoulder-guards, a tall collar hiding the chin, a pale-striped tusk cone
# with a tuft, a long thin sword held low.
# --------------------------------------------------------------------------------

def _euler_from_axes(x_axis: Vector, y_axis: Vector) -> tuple:
    """XYZ degrees for a rotation taking local X -> x_axis, local Y -> y_axis."""
    x = x_axis.normalized()
    y = (y_axis - x * y_axis.dot(x)).normalized()
    z = x.cross(y)
    m = Matrix((x, y, z)).transposed()
    return tuple(math.degrees(a) for a in m.to_euler("XYZ"))


def _shell_ring(z_top: float, z_bot: float, r_top: float, r_bot: float, t: float,
                roll: float = 0.0) -> list[tuple]:
    """Lathe profile for an open truncated-cone band `t` thick, hollow below, with
    a thin lid at the top (hidden inside whatever it hangs from) and an optional
    rolled lower edge. Coordinates relative to the lathe origin (z = 0 at z_bot)."""
    h = z_top - z_bot
    out = [(0.0, h), (r_top + t, h), (r_bot + t, roll * 0.5)]
    if roll:
        out += [(r_bot + t + roll * 0.6, 0.0), (r_bot + t * 0.5, -roll * 0.4)]
    out += [(r_bot, 0.0), (r_top, h - 0.012), (0.0, h - 0.012)]
    return out


def _tusk_helmet(fig: Human, base_z: float, top_z: float) -> list[Part]:
    """Boar's-tusk cone 0.26 m dia. on a felt cap: 4 rows of tusk plates
    alternating slant, bronze top knob with a dark horsehair tuft (2-bone chain),
    tusk-plated cheek-pieces hinged at the temples (Cheek.L/R)."""
    h = fig.h
    base = fig.lean((0.0, 0.004 * h, base_z))
    cone_h = top_z - 0.035 - base.z
    r0 = 0.132
    fig.add_bone("Helmet", base, base + Vector((0, 0, cone_h)), "Head")
    prof = [(r0 - 0.010, -0.012), (r0, 0.0), (r0 * 0.93, cone_h * 0.25),
            (r0 * 0.80, cone_h * 0.50), (r0 * 0.58, cone_h * 0.75), (0.035, cone_h * 0.95),
            (0.024, cone_h), (0.0, cone_h)]
    sy = 1.08
    parts = [Part("lathe", tuple(base), (1.0, sy, 1.0), mat="felt_and_leather", bone="Helmet",
                  segments=18, extras={"profile": prof, "rigid": True, "smooth": True,
                                       "bevel": False})]

    def radius_at(zr):
        for (ra, za), (rb, zb) in zip(prof[1:], prof[2:]):
            if za <= zr <= zb:
                return ra + (rb - ra) * (zr - za) / (zb - za)
        return prof[1][0]

    rows = [(0.024, 24), (0.064, 22), (0.104, 19), (0.142, 15)]
    for k, (zr, count) in enumerate(rows):
        slant = 22.0 if k % 2 == 0 else -22.0
        r = radius_at(zr) + 0.006
        slope = Vector((0.0, 0.0, 1.0))
        for i in range(count):
            a = 2 * math.pi * (i + 0.5 * (k % 2)) / count
            n = Vector((math.cos(a), math.sin(a) * sy, 0.0)).normalized()
            t = Vector((-math.sin(a), math.cos(a), 0.0))
            # up the cone surface: mostly Z, tipped inward by the cone slope
            dr = radius_at(zr + 0.02) - radius_at(zr - 0.02)
            up = (slope * 0.04 + n * dr).normalized()
            q = Matrix.Rotation(math.radians(slant), 3, n)
            up_s = q @ up
            at = base + Vector((math.cos(a) * r, math.sin(a) * r * sy, zr))
            parts.append(Part("box", tuple(at), (0.016, 0.008, 0.034), mat="boar_s_tusk",
                              bone="Helmet", rot=_euler_from_axes(q @ t, n),
                              extras={"rigid": True, "bevel": False,
                                      "_up": tuple(up_s)}))
    for part in parts[1:]:
        # the box's long side must follow the slanted up-slope direction: rebuild
        # the rotation from (width = up x normal, depth = normal)
        up_s = Vector(part.extras.pop("_up"))
        at = Vector(part.loc) - base
        n = Vector((at.x, at.y / sy, 0.0)).normalized()
        n = Vector((n.x, n.y * sy, 0.0)).normalized()
        width = up_s.cross(n)
        part.rot = _euler_from_axes(width, n)
    # knob and tuft
    knob = base + Vector((0, 0, cone_h + 0.016))
    parts.append(Part("sphere", tuple(knob), (0.040, 0.040, 0.036), mat="hammered_bronze_plate",
                      bone="Helmet", segments=8, rings=5, extras={"rigid": True,
                                                                  "bevel": False}))
    t0 = knob + Vector((0, 0.004, 0.016))
    t1 = t0 + Vector((0.0, 0.040, 0.012))
    t2 = t1 + Vector((0.0, 0.045, -0.045))
    fig.add_bone("Tuft1", t0, t1, "Helmet")
    fig.add_bone("Tuft2", t1, t2, "Tuft1")
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="horsehair", bone="Tuft1",
                      segments=6, extras={
                          "path": [tuple(t0), tuple(t0.lerp(t1, 0.5)), tuple(t1),
                                   tuple(t1.lerp(t2, 0.5)), tuple(t2)],
                          "sections": [(0.012, 0.016), (0.018, 0.022), (0.020, 0.022),
                                       (0.016, 0.014), (0.0, 0.0)],
                          "smooth": True, "bevel": False, "bones": ["Tuft1", "Tuft2"]}))
    # cheek-pieces hanging from the temples, 0.14 x 0.08 m
    for s, side in ((1.0, "L"), (-1.0, "R")):
        hinge = base + Vector((s * 0.112, -0.030, -0.010))
        fig.add_bone(f"Cheek.{side}", hinge, hinge + Vector((0, -0.01, -0.14)), "Helmet")
        c = hinge + Vector((s * 0.004, -0.012, -0.072))
        parts.append(Part("sphere", tuple(c), (0.028, 0.080, 0.145), mat="boar_s_tusk",
                          bone=f"Cheek.{side}", rot=(0.0, 0.0, s * -18.0), segments=10,
                          rings=6, extras={"rigid": True, "bevel": False}))
        for dz in (0.03, -0.01, -0.05):             # rows on the cheek-piece
            parts.append(Part("box", tuple(c + Vector((s * 0.014, 0.0, dz))),
                              (0.006, 0.066, 0.008), mat="felt_and_leather",
                              bone=f"Cheek.{side}", rot=(0.0, 0.0, s * -18.0),
                              extras={"rigid": True, "bevel": False}))
    return parts


def _rapier(fig: Human) -> list[Part]:
    """0.92 m bronze blade, 4 cm at the hilt tapering to a needle, strong midrib;
    horned guard 0.12 m; bone grip and 5 cm pommel. Carried low in the right fist,
    point forward and down; prop bone Sword on Hand.R."""
    g = fig.grip("R")
    d = Vector((-0.16, -0.42, -0.89)).normalized()      # blade direction
    side = d.cross(Vector((0.0, 0.0, 1.0))).normalized()  # blade width axis
    guard = g + d * 0.055
    tip = guard + d * 0.92
    fig.prop_bone("Sword", "R", guard, tip)
    prop = {"prop": True}
    parts = [
        _bar(g - d * 0.06, guard, 0.016, "bone_and_ivory", "Sword", segments=8, **prop),
        Part("sphere", tuple(g - d * 0.085), (0.05, 0.05, 0.05), mat="bone_and_ivory",
             bone="Sword", segments=8, rings=5, extras={**prop, "bevel": False}),
    ]
    # horned guard: a short bar bent back toward the hand at both ends
    horn = [guard + side * 0.06 - d * 0.03, guard + side * 0.045, guard,
            guard - side * 0.045, guard - side * 0.06 - d * 0.03]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="hammered_bronze_plate", bone="Sword",
                      segments=6, extras={"path": [tuple(p) for p in horn],
                                          "section": (0.010, 0.014), "smooth": True,
                                          "bevel": False, **prop}))
    outline = [(-0.020, 0.0), (0.020, 0.0), (0.017, 0.25), (0.012, 0.60), (0.006, 0.82),
               (0.0, 0.92), (-0.006, 0.82), (-0.012, 0.60), (-0.017, 0.25)]
    # prism: outline X -> width (side), outline Y -> d, extrusion Z -> thickness
    parts.append(Part("prism", tuple(guard), (1, 1, 0.006), mat="hammered_bronze_plate",
                      bone="Sword", rot=_euler_from_axes(side, d),
                      extras={"outline": outline, **prop}))
    parts.append(_bar(guard, guard + d * 0.80, 0.0055, "hammered_bronze_plate", "Sword",
                      segments=5, **prop))
    return parts


def dendra_champion(entry: Entry):
    # 1.92 m to the helmet knob, 1.78 m bare head. Broad (bulk 1.18); arms spread
    # wide so they hang outside the bell of plates.
    fig = Human(height=1.78, bulk=1.18, shoulders=0.58,
                arm_r=ArmPose(spread=24.0, swing=6.0, elbow=35.0),
                arm_l=ArmPose(spread=24.0, swing=2.0, elbow=20.0))
    h = fig.h
    hem = 0.50
    parts = [fig.torso_part("linen", pad=0.01, hem=hem, hem_flare=1.55, segments=24)]
    for side in ("L", "R"):
        parts.append(fig.arm_part(side, "skin"))
        parts.append(_sleeve(fig, side, "linen", reach=0.95, pad=0.010, flare=1.1))
        parts.append(_sleeve(fig, side, "hammered_bronze_plate", reach=0.62, pad=0.022))
        parts += fig.hand_part(side, "skin")
        parts.append(fig.leg_part(side, "skin"))
        parts.append(fig.foot_part(side, "felt_and_leather", length=0.29, point=0.0))
        # greaves 0.32 m: a bronze sleeve round the shin, laced behind
        knee, ank = fig.joint(f"knee.{side}"), fig.joint(f"ankle.{side}")
        shin = ank - knee
        g0 = ank - shin.normalized() * 0.03
        g1 = g0 - shin.normalized() * 0.32
        pts = [g0, g0.lerp(g1, 0.35), g0.lerp(g1, 0.7), g1]
        secs = [(0.052, 0.056), (0.064, 0.070), (0.072, 0.080), (0.070, 0.078)]
        parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat="hammered_bronze_plate",
                          bone=f"LowerLeg.{side}", segments=12, extras={
                              "path": [tuple(p) for p in pts], "sections": secs,
                              "offsets": [(0.0, 0.0), (0.006, 0.0), (0.008, 0.0), (0.0, 0.0)],
                              "up": (0.0, 1.0, 0.0), "rigid": True, "bevel": False}))
    parts += fig.head_part("skin", face="skin", features="horsehair")

    # Cuirass: a bell of hammered bronze, front + back as one shell, three incised
    # ridge-lines; hung on Chest/Spine.
    cz = fig.belt_z - 0.005
    ridge = []
    for zr in (0.10, 0.19, 0.28):
        ridge += [(None, zr - 0.006), ("r", zr), (None, zr + 0.006)]
    body = [(0.286, 0.0), (0.268, 0.05), (0.256, 0.12), (0.252, 0.22), (0.250, 0.30),
            (0.240, 0.36), (0.212, 0.41), (0.160, 0.445), (0.120, 0.455), (0.0, 0.455)]
    prof = [(0.0, 0.02), (0.270, 0.02)] + body[:-1] + [(0.0, body[-1][1])]
    prof = [(0.0, 0.02), (0.272, 0.024), (0.296, 0.0)] + body[1:]
    cuirass_paint = []
    parts.append(Part("lathe", (0.0, 0.004, cz), (1.0, 0.80, 1.0), mat="hammered_bronze_plate",
                      bone="Chest", segments=28, extras={
                          "profile": prof, "smooth": True, "bevel": False,
                          "paint": cuirass_paint,
                          "bones": ["Spine", "Chest", "Hips"]}))
    for zr, rr in ((0.10, 0.258), (0.19, 0.254), (0.28, 0.252)):
        parts.append(Part("torus", (0.0, 0.004, cz + zr), (2 * rr, 2 * rr * 0.80, 0.012),
                          mat="hammered_bronze_plate", bone="Chest", segments=28, rings=4,
                          minor=0.02, extras={"bevel": False, "smooth": True,
                                              "bones": ["Spine", "Chest", "Hips"]}))

    # Neck guard: tall collar 0.26 m dia. x 0.14 m, flaring, rolled top edge.
    col_z = fig.shoulder_z - 0.030
    fig.add_bone("Collar", (0, 0.004, col_z), (0, 0.004, col_z + 0.14), "Chest")
    parts.append(Part("lathe", (0.0, 0.004, col_z), (1.0, 0.92, 1.0),
                      mat="hammered_bronze_plate", bone="Collar", segments=20, extras={
                          "profile": [(0.0, 0.03), (0.150, 0.03), (0.126, 0.05),
                                      (0.130, 0.125), (0.140, 0.145), (0.144, 0.157),
                                      (0.132, 0.161), (0.116, 0.145), (0.0, 0.145)],
                          "rigid": True, "smooth": True, "bevel": False}))

    # Shoulder guards: domes 0.30 m wide over each shoulder, curving down the arm.
    for s, side in ((1.0, "L"), (-1.0, "R")):
        sh = fig.joint(f"shoulder.{side}")
        c = sh + Vector((s * 0.02, 0.004, 0.02))
        dome = [(0.0, 0.10), (0.06, 0.094), (0.11, 0.07), (0.145, 0.02), (0.152, -0.02),
                (0.148, -0.045), (0.128, -0.01), (0.09, 0.04), (0.0, 0.05)]
        parts.append(Part("lathe", tuple(c), (1.0, 1.0, 1.0), mat="hammered_bronze_plate",
                          bone=f"Shoulder.{side}", rot=(0.0, s * 38.0, 0.0), segments=18,
                          extras={"profile": dome, "smooth": True, "bevel": False,
                                  "bones": [f"Shoulder.{side}", f"UpperArm.{side}",
                                            "Chest"]}))

    # Skirt rings: three nested truncated cones, 0.14 m each, 0.58 -> 0.76 m dia.,
    # each on its own bone under Hips (they swing as a bell).
    tops = [cz + 0.02, cz - 0.10, cz - 0.22]
    dias = [(0.58, 0.64), (0.64, 0.70), (0.70, 0.76)]
    for i, (zt, (d0, d1)) in enumerate(zip(tops, dias), start=1):
        zb = zt - 0.14
        bone = fig.add_bone(f"Ring{i}", (0, 0, zt), (0, 0, zb), "Hips")
        prof = _shell_ring(zt, zb, d0 / 2 - 0.012, d1 / 2 - 0.012, 0.010, roll=0.012)
        parts.append(Part("lathe", (0.0, 0.004, zb), (1.0, 0.74, 1.0),
                          mat="hammered_bronze_plate", bone=bone, segments=28,
                          extras={"profile": prof, "rigid": True, "smooth": True,
                                  "bevel": False}))
        # dome-head rivets along the lower edge, every ~0.1 m
        n = 22
        r = d1 / 2 + 0.004
        for k in range(n):
            a = 2 * math.pi * (k + 0.5) / n
            at = (math.cos(a) * r, 0.004 + math.sin(a) * r * 0.74, zb + 0.022)
            parts.append(Part("sphere", at, (0.014, 0.014, 0.014),
                              mat="rivet_bronze", bone=bone, segments=5, rings=3,
                              extras={"rigid": True, "bevel": False}))

    # The JSON's 0.30 m cone would sit on the eyes; the concept's (and this) base is
    # at the brow, 1.71 m, so the cone is 0.18 m tall to reach the 1.92 m knob.
    parts += _tusk_helmet(fig, base_z=0.960 * h, top_z=1.92)
    parts += _rapier(fig)

    # Review pose: the test pose, with the arm raise capped to 55° (the JSON's
    # pauldron limit on shoulder abduction).
    pose = dict(figures.HUMAN_TEST_POSE)
    pose["UpperArm.L"] = (-55.0, 0.0, 0.0)
    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(pose),
        family_overrides={
            "hammered_bronze_plate": {"wear_to": "#B08050", "wear_amount": 0.35,
                                      "grain": 0.25},
            "boar_s_tusk": {"rough": 0.4},
        },
        extra_families={
            "skin": {"name": "Weathered skin", "base": "#9C6E4E", "rough": 0.75,
                     "notes": "Face, forearms and knees (the concept shows them bare); "
                              "the champion's list has no skin. Hex from the levy."},
            "rivet_bronze": {"name": "Rubbed bronze rivets", "base": "#B08050",
                             "rough": 0.35, "metal": 1.0,
                             "notes": "The JSON's 'brighter #B08050 on raised edges and "
                                      "rivet heads' as geometry on the top row."},
        },
        notes=[
            "Rapier (Sword prop bone on Hand.R) is left out of the height check.",
            "Bones added: Helmet (detachable), Tuft1-2, Cheek.L/R, Collar, Ring1-3 "
            "(skirt bands under Hips, rigid, for damped secondary swing).",
            "Not built: tower shield variant and its shield_back socket; lacing "
            "between the rings; rivets only on the rings' lower edges.",
        ])


# --------------------------------------------------------------------------------
# Keeper of the Flame (special) — a stepped bell of five flounces, a yoke across
# both shoulders with two fire-pots swinging from each end, a flat polos, and a
# censer on chains in the right hand.
# --------------------------------------------------------------------------------

class _ReachingHuman(Human):
    """A Human whose arms can be placed by a target grip point (two-bone IK) rather
    than by ArmPose angles. ArmPose only bends the elbow forward, so a hand raised
    to grip a yoke beside the shoulder cannot be reached with it. Worked around here
    (figures.py is shared): `grips` = {side: (grip point, elbow hint direction)}."""

    def __init__(self, *args, grips: dict | None = None, **kwargs):
        self._grips = grips or {}
        super().__init__(*args, **kwargs)

    def _arm_points(self, side: str):
        if side not in self._grips:
            return super()._arm_points(side)
        h, s = self.h, figures.SIDES[side]
        target, hint = (Vector(v) for v in self._grips[side])
        shoulder = self.lean((s * self.shoulder_x, 0.0, self.shoulder_z))
        l1, l2 = 0.172 * h, 0.145 * h + 0.045 * h        # upper arm; elbow -> grip
        d = target - shoulder
        dist = min(d.length, l1 + l2 - 1e-3)
        dn = d.normalized()
        perp = (hint - dn * hint.dot(dn)).normalized()
        a = math.acos(max(-1.0, min(1.0, (l1 * l1 + dist * dist - l2 * l2) / (2 * l1 * dist))))
        upper = dn * math.cos(a) + perp * math.sin(a)
        elbow = shoulder + upper * l1
        fore = (target - elbow).normalized()
        wrist = elbow + fore * (0.145 * h)
        return shoulder, elbow, wrist, wrist + fore * (0.090 * h), fore


def _ellipse(z: float, rx: float, ry: float, n: int, cy: float = 0.0,
             a0: float = 0.0) -> list[tuple]:
    return [(math.cos(a0 + 2 * math.pi * j / n) * rx,
             cy + math.sin(a0 + 2 * math.pi * j / n) * ry, z) for j in range(n)]


# Flounce edges read off the concept (1 m = 367 px): bottom z and bottom diameter
# of each tier. Waist 0.36 m -> hem 0.96 m, as the JSON.
_TIERS = [(0.81, 0.50), (0.62, 0.62), (0.42, 0.76), (0.23, 0.86), (0.02, 0.96)]
_SKIRT_DEPTH = 0.86        # front-to-back / side-to-side
_SKIRT_DY = 0.012


def _skirt_radius(z: float, top_z: float, top_r: float) -> float:
    """Outer half-width of the skirt at z (the widest flounce edge at or above z
    included, so a layer laid over it clears every edge)."""
    r_prev_z, r_prev = top_z, top_r
    best = top_r
    for bz, bd in _TIERS:
        rb = bd / 2
        if z >= bz:
            f = (r_prev_z - z) / max(r_prev_z - bz, 1e-6)
            return max(best, r_prev * 0.92 + (rb - r_prev * 0.92) * max(0.0, f))
        best = max(best, rb)
        r_prev_z, r_prev = bz, rb
    return best


def _flounced_skirt(fig: Human, top_z: float, top_rx: float, top_ry: float,
                    n: int = 36) -> Part:
    """Five stepped flounces as one closed loft: each tier flares down to a lipped
    edge, then the surface steps in under the edge and the next tier starts. Tiers
    alternate linen / soot wool; each edge carries a 2.5 cm red-and-linen checker.
    Skinned to Hips only, then the skirt rule hands the lower bell to the thighs
    by position (deterministic, so the apron laid over it moves the same way)."""
    rings, paint = [], []
    prev_z, prev_r = top_z, top_rx
    rings.append(_ellipse(top_z, top_rx, top_ry, n, _SKIRT_DY))
    band = 0.025
    for k, (bz, bd) in enumerate(_TIERS):
        rb = bd / 2
        rt = prev_r * 0.92 if k else prev_r
        if k:           # step in under the previous edge
            rings.append(_ellipse(prev_z, rt, rt * _SKIRT_DEPTH, n, _SKIRT_DY))
        mid_z = (prev_z + bz + band) / 2
        rm = rt + (rb - rt) * 0.55
        rings.append(_ellipse(mid_z, rm, rm * _SKIRT_DEPTH, n, _SKIRT_DY))
        rings.append(_ellipse(bz + band, rb - 0.004, (rb - 0.004) * _SKIRT_DEPTH, n,
                              _SKIRT_DY))
        rings.append(_ellipse(bz, rb + 0.006, (rb + 0.006) * _SKIRT_DEPTH, n, _SKIRT_DY))
        if k % 2 == 1:          # soot-wool tier body (above its border)
            paint.append({"mat": "soot_dyed_wool", "min": (-2, -2, bz + band + 0.002),
                          "max": (2, 2, prev_z - 0.001)})
        # checker: every other face of the border band red, the rest linen
        paint.append({"mat": "linen", "min": (-2, -2, bz + 0.004),
                      "max": (2, 2, bz + band - 0.004)})
        rr = rb + 0.001
        for j in range(0, n, 2):
            a = 2 * math.pi * (j + 0.5) / n
            cx, cy = math.cos(a) * rr, _SKIRT_DY + math.sin(a) * rr * _SKIRT_DEPTH
            w = math.pi * rr / n * 0.6
            paint.append({"mat": "haematite_red",
                          "min": (cx - w, cy - w, bz + 0.004),
                          "max": (cx + w, cy + w, bz + band - 0.004)})
        prev_z, prev_r = bz, rb
    skirt = {"top": fig.hip_z, "bottom": 0.0, "strength": 0.45, "split": 0.35}
    return Part("loft", (0, 0, 0), (1, 1, 1), mat="linen", bone="Hips", extras={
        "rings": rings, "smooth": True, "bevel": False, "paint": paint, "skirt": skirt,
        "bones": ["Hips"]})


def _apron(fig: Human, top_z: float, bottom_z: float, front: bool,
           top_r: float) -> Part:
    """One panel of the double apron: a thin curved sheet hanging from the belt over
    the flounces, rounded at the bottom; red border down the sides, scorched
    (soot) along the lower edge. Same Hips + skirt rule as the skirt."""
    sgn = -1.0 if front else 1.0
    m, thick = 7, 0.008
    rings = []
    rows = 7
    for i in range(rows):
        t = i / (rows - 1)
        z = top_z + (bottom_z - top_z) * t
        r = max(top_r + 0.012 + (0.33 - top_r) * t ** 1.3,
                _skirt_radius(z, top_z + 0.03, top_r) + 0.014)
        half = math.radians(40.0) * (1.0 - 0.55 * max(0.0, (t - 0.7) / 0.3) ** 1.5)
        outer, inner = [], []
        for j in range(m):
            a = sgn * math.pi / 2 + half * (-1.0 + 2.0 * j / (m - 1))
            for lst, rr in ((outer, r + thick), (inner, r)):
                lst.append((math.cos(a) * rr, _SKIRT_DY + math.sin(a) * rr * _SKIRT_DEPTH, z))
        rings.append(outer + list(reversed(inner)))
    paint = [{"mat": "haematite_red", "min": (-2, -2, bottom_z - 0.01),
              "max": (2, 2, bottom_z + 0.022)}]
    # soot scorch on the lowest band, and two soot handprints on the front
    paint.insert(0, {"mat": "soot_dyed_wool", "min": (-2, -2, bottom_z - 0.01),
                     "max": (2, 2, bottom_z + 0.050)})
    if front:
        for hx, hz in ((-0.09, top_z - 0.22), (0.06, top_z - 0.30)):
            paint.insert(1, {"mat": "soot_dyed_wool", "min": (hx - 0.025, -2, hz - 0.03),
                             "max": (hx + 0.025, 2, hz + 0.03)})
    skirt = {"top": fig.hip_z, "bottom": 0.0, "strength": 0.45, "split": 0.35}
    return Part("loft", (0, 0, 0), (1, 1, 1), mat="linen", bone="Hips", extras={
        "rings": rings, "smooth": True, "bevel": False, "paint": paint, "skirt": skirt,
        "bones": ["Hips"]})


def _yoke_path(n: int = 13) -> list[Vector]:
    """Olive-wood pole 1.30 m: over both shoulders, bowed back round the neck."""
    ctrl = [(-0.65, 0.030, 1.382), (-0.40, 0.030, 1.370), (-0.17, 0.040, 1.366),
            (0.0, 0.088, 1.372), (0.17, 0.040, 1.366), (0.40, 0.030, 1.370),
            (0.65, 0.030, 1.382)]
    return [Vector(p) for p in spline(ctrl, 3)]


def _yoke_at(x: float) -> Vector:
    path = _yoke_path()
    for a, b in zip(path, path[1:]):
        if a.x <= x <= b.x:
            return a.lerp(b, (x - a.x) / max(b.x - a.x, 1e-6))
    return path[-1] if x > 0 else path[0]


def _yoke_and_pots(fig: Human) -> list[Part]:
    """The yoke (rigid on Yoke, child of Chest) and four fire-pots, each on a cord
    bone (PotCord<n>.<side>, child of Yoke) and a pot bone (Pot<n>.<side>)."""
    path = _yoke_path()
    y0 = Vector((0.0, 0.088, 1.372))
    fig.add_bone("Yoke", y0 + Vector((0, -0.06, -0.06)), y0 + Vector((0, 0, 0.02)), "Chest")
    parts = [Part("tube", (0, 0, 0), (1, 1, 1), mat="olive_wood", bone="Yoke", segments=8,
                  extras={"path": [tuple(p) for p in path], "section": (0.025, 0.025),
                          "smooth": True, "bevel": False, "rigid": True,
                          "paint": [{"mat": "soot_dyed_wool", "min": (0.60, -1, 0),
                                     "max": (1, 1, 2)},
                                    {"mat": "soot_dyed_wool", "min": (-1, -1, 0),
                                     "max": (-0.60, 1, 2)}]})]
    # carved notches: a raised collar either side of each cord seat
    for x in (-0.62, -0.48, 0.48, 0.62):
        for dx in ((0.028,) if x > 0 else (-0.028,)):
            p = _yoke_at(x + dx)
            parts.append(Part("cyl", tuple(p), (0.058, 0.058, 0.010), mat="olive_wood",
                              bone="Yoke", rot=(0, 90, 0), segments=8,
                              extras={"rigid": True, "bevel": False, "smooth": True}))
    # pots: (x on the yoke, y offset of the pot so pairs clear each other)
    specs = [(0.48, -0.050, "1"), (0.62, 0.070, "2")]
    for side, s in (("L", 1.0), ("R", -1.0)):
        for x, dy, k in specs:
            top = _yoke_at(s * x)
            neck = Vector((s * x, top.y + dy, top.z - 0.35))
            bottom = neck - Vector((0, 0, 0.20))
            cord = fig.add_bone(f"PotCord{k}.{side}", top, neck, "Yoke")
            pot = fig.add_bone(f"Pot{k}.{side}", neck, bottom, cord)
            for sx in (-1.0, 1.0):     # doubled flax cord, to either side of the neck
                a = top + Vector((sx * 0.010, 0.0, -0.022))
                b = neck + Vector((sx * 0.036, 0.0, -0.012))
                parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="linen", bone=cord,
                                  segments=4, extras={
                                      "path": [tuple(a), tuple(a.lerp(b, 0.5)), tuple(b)],
                                      "section": (0.0035, 0.0035), "smooth": True,
                                      "bevel": False, "rigid": True}))
            parts.append(Part("torus", tuple(top + Vector((0, 0, 0.0))), (0.062, 0.062, 0.062),
                              mat="linen", bone=cord, rot=(0, 90, 0), segments=8, rings=4,
                              minor=0.08, extras={"rigid": True, "bevel": False}))
            parts.append(Part("torus", tuple(neck + Vector((0, 0, -0.018))),
                              (0.080, 0.080, 0.012), mat="linen", bone=cord, segments=10,
                              rings=4, minor=0.1, extras={"rigid": True, "bevel": False}))
            # globular pot 0.18 x 0.20, neck 0.07; bottom at `bottom`
            prof = [(0.0, 0.0), (0.050, 0.006), (0.084, 0.050), (0.090, 0.095),
                    (0.074, 0.140), (0.036, 0.170), (0.034, 0.186), (0.040, 0.196),
                    (0.028, 0.200), (0.0, 0.200)]
            paint = [{"mat": "ember_madder", "min": (-1, -1, 0.186), "max": (1, 1, 1)}]
            for j in range(3):          # soot streaks: single face columns, neck down
                col = (3 * j + int(k) + (0 if side == "L" else 5)) % 10
                a = 2 * math.pi * (col + 0.5) / 10      # a face column's centre
                cx, cy = math.cos(a) * 0.075, math.sin(a) * 0.075
                paint.append({"mat": "soot_dyed_wool",
                              "min": (cx - 0.012, cy - 0.012, 0.02 + 0.04 * (j % 2)),
                              "max": (cx + 0.012, cy + 0.012, 0.185)})
            parts.append(Part("lathe", tuple(bottom), (1, 1, 1), mat="fire_pot_clay",
                              bone=pot, segments=10, extras={
                                  "profile": prof, "smooth": True, "bevel": False,
                                  "rigid": True, "paint": paint}))
            # clay bung and its smouldering wick
            parts.append(Part("cone", tuple(bottom + Vector((0, 0, 0.212))),
                              (0.014, 0.014, 0.034), mat="ember_madder", bone=pot,
                              segments=5, taper=0.2, extras={"rigid": True, "bevel": False}))
    return parts


def _censer(fig: Human) -> list[Part]:
    """Bronze brazier bowl 0.16 x 0.10 with a perforated lid on three chains 0.60 m
    to a ring handle in the right fist. Chain bones Censer1..3 (Censer1 on Hand.R);
    the bowl is rigid on Censer3. Hung mid-swing, forward of the skirt, as the
    concept's side view."""
    g = fig.grip("R")
    ring = g + Vector((0.0, 0.0, -0.030))
    rim = ring + Vector((-0.025, -0.165, -0.575))        # 0.60 m of chain
    pts = [ring.lerp(rim, t) for t in (0.0, 1 / 3, 2 / 3, 1.0)]
    fig.prop_bone("Censer1", "R", pts[0], pts[1])
    fig.add_bone("Censer2", pts[1], pts[2], "Censer1")
    fig.add_bone("Censer3", pts[2], pts[3] - Vector((0, 0, 0.06)), "Censer2")
    bones = ["Censer1", "Censer2", "Censer3"]
    parts = [Part("torus", tuple(g + Vector((0.0, 0.0, -0.005))), (0.075, 0.075, 0.075),
                  mat="cast_bronze", bone="Censer1", rot=(0, 90, 0), segments=10, rings=4,
                  minor=0.12, extras={"prop": True, "bevel": False})]
    for j in range(3):                      # three chains to three lugs on the rim
        a = 2 * math.pi * j / 3 + 0.5
        lug = rim + Vector((math.cos(a) * 0.080, math.sin(a) * 0.080, 0.0))
        path = [tuple(ring.lerp(lug, t)) for t in (0.0, 0.2, 0.4, 0.6, 0.8, 1.0)]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="cast_bronze", bone="Censer2",
                          segments=4, extras={"path": path, "section": (0.0035, 0.0035),
                                              "smooth": True, "bevel": False,
                                              "bones": bones}))
    base = rim - Vector((0, 0, 0.085))
    bowl = [(0.0, 0.0), (0.030, 0.0), (0.030, 0.012), (0.052, 0.020), (0.072, 0.045),
            (0.080, 0.075), (0.084, 0.085), (0.070, 0.085), (0.0, 0.085)]
    parts.append(Part("lathe", tuple(base), (1, 1, 1), mat="cast_bronze", bone="Censer3",
                      segments=14, extras={"profile": bowl, "smooth": True, "bevel": False,
                                           "prop": True}))
    # perforated domed lid: ember glow through the holes and at the knob
    lid = [(0.0, 0.0), (0.078, 0.0), (0.074, 0.018), (0.058, 0.036), (0.030, 0.048),
           (0.012, 0.052), (0.012, 0.066), (0.0, 0.068)]
    paint = [{"mat": "ember_madder", "min": (-1, -1, 0.049), "max": (1, 1, 1)}]
    for j in range(6):
        a = 2 * math.pi * j / 6
        cx, cy = math.cos(a) * 0.060, math.sin(a) * 0.060
        paint.append({"mat": "ember_madder", "min": (cx - 0.016, cy - 0.016, 0.012),
                      "max": (cx + 0.016, cy + 0.016, 0.034)})
    parts.append(Part("lathe", tuple(base + Vector((0, 0, 0.084))), (1, 1, 1),
                      mat="cast_bronze", bone="Censer3", segments=14,
                      extras={"profile": lid, "smooth": True, "bevel": False, "prop": True,
                              "paint": paint}))
    return parts


def _polos(fig: Human, top_z: float) -> list[Part]:
    """Flat-topped linen polos 0.18 m dia., seated on the skull and reaching
    `top_z`, bound low with a fresco-red band. Own bone (detachable)."""
    base = fig.lean((0.0, 0.006 * fig.h, fig.h - 0.030))
    hgt = top_z - base.z
    fig.add_bone("Polos", base, base + Vector((0, 0, hgt)), "Head")
    prof = [(0.0, 0.0), (0.084, 0.0), (0.088, 0.010), (0.091, 0.022), (0.091, 0.052),
            (0.090, hgt - 0.008),
            (0.086, hgt), (0.0, hgt)]
    return [Part("lathe", tuple(base), (1.0, 1.0, 1.0), mat="linen", bone="Polos",
                 segments=18, extras={
                     "profile": prof, "rigid": True, "smooth": True, "bevel": False,
                     "paint": [{"mat": "haematite_red", "min": (-1, -1, 0.022),
                                "max": (1, 1, 0.052)}]})]


def _ring_round(centre: Vector, axis: Vector, radius: float, section: float, mat: str,
                bone: str, **extras) -> Part:
    """A closed tube ring round `axis` (bangles, sleeve borders)."""
    axis = axis.normalized()
    u = axis.cross(Vector((0, 0, 1)) if abs(axis.z) < 0.9 else Vector((1, 0, 0))).normalized()
    v = axis.cross(u)
    pts = [tuple(centre + (u * math.cos(a) + v * math.sin(a)) * radius)
           for a in (2 * math.pi * j / 9 for j in range(9))]
    return Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, segments=4,
                extras={"path": pts, "section": (section, section), "closed": True,
                        "smooth": True, "bevel": False, **extras})


def flame_keeper(entry: Entry):
    # 1.72 m to the top of the polos, 1.60 m bare head. Slender (bulk 0.86). Left
    # fist grips the yoke outboard of the shoulder; right fist forward at the hip
    # carrying the censer (the concept: the JSON rig puts the censer chain on the
    # left hand, but the concept's front view has it in her right; followed the
    # concept, and the unhook/throw beats then use the free left hand).
    grip_l = _yoke_at(0.40)
    fig = _ReachingHuman(height=1.60, bulk=0.86, shoulders=0.38, grips={
        "L": (tuple(grip_l), (0.55, -0.25, -1.0)),
        "R": ((-0.27, -0.33, 0.93), (-0.4, 0.3, -1.0)),
    })
    h = fig.h
    pad = 0.006
    top_z = fig.waist_z + 0.035

    # Bodice: fitted linen jacket to the waist (the torso loft runs on to the
    # crotch inside the skirt), open V at the throat.
    vneck = [{"mat": "skin", "min": (-w, -1.0, z), "max": (w, -0.02, 2.0)}
             for w, z in ((0.062, fig.shoulder_z - 0.02), (0.042, fig.shoulder_z - 0.06),
                          (0.024, fig.shoulder_z - 0.10))]
    parts = [fig.torso_part("linen", pad=pad, segments=24, paint=vneck)]
    # breast-band under the open front: a linen panel with red lacing
    bb = fig.surface(fig.chest_z - 0.035, -90.0, pad=pad + 0.010)
    parts.append(Part("sphere", tuple(bb), (0.15, 0.030, 0.070), mat="linen", bone="Chest",
                      segments=12, rings=6, extras={"rigid": True, "bevel": False,
                                                    "smooth": True}))
    for k in range(4):
        x = -0.045 + 0.030 * k
        parts.append(_bar(bb + Vector((x - 0.010, -0.014, 0.022)),
                          bb + Vector((x + 0.010, -0.014, -0.022)), 0.004, "haematite_red",
                          "Chest", segments=4, rigid=True))
    for side in ("L", "R"):
        parts.append(fig.arm_part(side, "skin"))
        parts.append(_sleeve(fig, side, "linen", reach=0.42, pad=0.010, flare=1.25))
        sh, el = fig.joint(f"shoulder.{side}"), fig.joint(f"elbow.{side}")
        cuff = sh + (el - sh) * 0.40
        parts.append(_ring_round(cuff, el - sh, 0.027 * h * fig.bulk * 1.25 + 0.012, 0.009,
                                 "haematite_red", f"UpperArm.{side}",
                                 bones=[f"UpperArm.{side}", f"Shoulder.{side}"]))
        parts += fig.hand_part(side, "skin")
        parts.append(fig.leg_part(side, "skin"))
        parts.append(fig.foot_part(side, "skin", length=0.24, point=0.0))
        # bronze bangles, 3 per wrist, 0.07 m
        wr = fig.joint(f"wrist.{side}")
        fore = (wr - fig.joint(f"elbow.{side}")).normalized()
        for k, d in enumerate((0.018, 0.034, 0.050)):
            parts.append(_ring_round(wr - fore * d, fore, 0.029, 0.0055, "cast_bronze",
                                     f"LowerArm.{side}", rigid=True))
    parts += fig.head_part("skin", face="skin", features="hair")
    parts.append(_hair(fig, "hair", length_z=fig.chest_z - 0.02, width=1.05, back=1.1))
    # forelock curl
    fl = fig.lean((0.028, -0.060, 0.978 * h))
    parts.append(Part("torus", tuple(fl), (0.034, 0.034, 0.034), mat="hair", bone="Head",
                      rot=(0, 90, 0), segments=8, rings=4, minor=0.22,
                      extras={"rigid": True, "bevel": False}))
    parts += _polos(fig, 1.72)

    parts.append(_flounced_skirt(fig, top_z, fig.torso_dims(top_z)[0] + 0.016,
                                 fig.torso_dims(top_z)[1] + 0.016))
    top_r = fig.torso_dims(top_z)[0] + 0.020
    for front in (True, False):
        parts.append(_apron(fig, top_z - 0.005, top_z - 0.44, front, top_r))
    # fresco-red sash over the skirt top
    parts.append(fig.band(fig.waist_z + 0.01, "haematite_red", height=0.055, pad=0.030))

    parts += _yoke_and_pots(fig)
    parts += _censer(fig)

    # Review pose: chant_alarm-ish. The censer arm comes up instead of the yoke arm
    # (which stays on the yoke); the censer chain counter-rotates so it hangs; the
    # left pots swing forward, the right ones back (they keep swinging).
    pose = {k: v for k, v in figures.HUMAN_TEST_POSE.items()
            if k not in ("UpperArm.L", "LowerArm.L")}
    pose["UpperArm.R"] = (-80.0, 0.0, 0.0)
    pose["LowerArm.R"] = (-25.0, 0.0, 0.0)
    pose["Censer1"] = (105.0, 0.0, 0.0)
    pose["PotCord1.L"] = (-18.0, 0.0, 0.0)
    pose["PotCord2.L"] = (-12.0, 0.0, 0.0)
    pose["PotCord1.R"] = (14.0, 0.0, 0.0)
    pose["PotCord2.R"] = (20.0, 0.0, 0.0)
    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(pose),
        family_overrides={
            "linen": {"grain": 0.22, "wear_to": "#A89A7A", "wear_amount": 0.25},
            # Emission is multiplied by 9 in the shipping material (README, Traps):
            # store madder at ~35 % so the glow reads as a coal, not pink-white.
            "ember_madder": {"emit": "#4A1E0C", "rough": 0.6},
            "cast_bronze": {"rough": 0.45, "wear_to": "#4A3220", "wear_amount": 0.3},
            "soot_dyed_wool": {"rough": 0.95},
        },
        extra_families={
            "skin": {"name": "Skin", "base": "#9C6E4E", "rough": 0.7,
                     "notes": "Face, bare forearms and feet (build bullets); the keeper's "
                              "list has no skin. Hex from the levy's weathered skin."},
            "hair": {"name": "Dark hair", "base": "#2B231B", "rough": 0.8,
                     "notes": "Long dark ringlets (build bullet); no hair family in her "
                              "list. Hex from the champion's horsehair."},
        },
        notes=[
            "Censer (Censer1 prop bone on Hand.R -> Censer2 -> Censer3, bowl rigid on "
            "Censer3) is in the RIGHT hand as the concept draws it; the JSON rig says left.",
            "Yoke is rigid on its own bone under Chest; four pots each hang on "
            "PotCord<n>.<side> -> Pot<n>.<side> (one cord bone, not the JSON's two).",
            "Polos is its own bone under Head (detachable).",
            "Skirt and aprons are weighted to Hips, then the skirt rule shares the lower "
            "bell with the thighs (45 %).",
            "Not built: skirt flounce ring bones x5 and hair chain x3 (springs); "
            "painted soles; scorched bites in the hem; singed hair strands.",
        ])


BLUEPRINTS = {
    "palace-levy": palace_levy,
    "wall-slinger": wall_slinger,
    "dendra-champion": dendra_champion,
    "flame-keeper": flame_keeper,
}
