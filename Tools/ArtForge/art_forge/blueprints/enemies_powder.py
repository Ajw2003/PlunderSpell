"""Age of Powder enemies (docs/art/data/powder.json, enemies).

Four rigged humanoids on figures.Human, following the two samples in
enemies_high.py: the Palace Guard (partisan-guard), the Musketeer, the
Cuirassier and the Petardier.

Shared helpers at the top build the recurring pieces: draped collars/gorgets
laid on the torso surface, thin strips laid along a surface (slashed panes,
lames), annular lofts (hat brims, collars), swords on hangers, horn lanterns.

The art bible lists no skin family for any of these four; every concept sheet
swatches skin (#B88E6E, sooted #8E6A52 on the petardier), so each blueprint
adds it as an extra family.
"""

from __future__ import annotations

import math

import bpy
from mathutils import Vector

from .. import figures
from ..figures import ArmPose, Human
from ..kit import Part, spline
from ..spec import Entry
from . import blueprint


# Heat-weighting retry: moved into the shared framework (rig.smooth_weights_with_retry).


SKIN = {"skin": {"name": "Skin", "base": "#B88E6E", "rough": 0.6,
                 "notes": "Not in the JSON materials; the concept sheet's skin swatch."}}


# --------------------------------------------------------------------------------
# Shared helpers
# --------------------------------------------------------------------------------

def _v(p) -> Vector:
    return Vector(p) if not isinstance(p, Vector) else p.copy()


def _deg(euler) -> tuple:
    return tuple(math.degrees(a) for a in euler)


def _track(direction: Vector) -> tuple:
    """XYZ degrees that turn local +Z onto `direction` (for cyl/cone/torus)."""
    return _deg(Vector(direction).normalized().to_track_quat("Z", "Y").to_euler())


def _ring_loft(sections: list[list[tuple]], mat: str, bone: str, **extras) -> Part:
    """A closed (torus-like) loft: `sections` is a list of rings, each ring a
    list of points at the same angles. Consecutive rings trace the cross-section
    (e.g. inner-top, outer-bottom, outer-under, inner-under) so the result is a
    thin closed band with no poles and no caps."""
    ex = {"rings": sections, "closed": True, "bevel": False, "smooth": True}
    ex.update(extras)
    return Part("loft", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, extras=ex)


def _drape(fig: Human, mat: str, z_top: float, r_top: tuple, z_bot: float, pad: float,
           thick: float, n: int = 24, bone: str = "Chest", front_drop: float = 0.0,
           **extras) -> Part:
    """A collar, gorget or cape laid on the shoulders: from an ellipse round the
    neck (half-axes r_top at z_top) down onto the torso surface at z_bot (plus
    `pad`), `thick` metres thick. `front_drop` lowers the front edge (a falling
    collar's points)."""
    top, bot, bot2, top2 = [], [], [], []
    c = fig.lean((0.0, 0.004 * fig.h, z_top))
    for j in range(n):
        a = 2.0 * math.pi * j / n
        ca, sa = math.cos(a), math.sin(a)
        drop = front_drop * max(0.0, -sa) ** 2
        zb = z_bot - drop
        p_top = Vector((c.x + ca * r_top[0], c.y + sa * r_top[1], c.z))
        p_bot = fig.surface(zb, math.degrees(a), pad=pad)
        out = (p_bot - Vector((c.x, c.y, p_bot.z)))
        out.z = 0.0
        out = out.normalized() if out.length > 1e-6 else Vector((ca, sa, 0))
        top.append(tuple(p_top))
        bot.append(tuple(p_bot))
        bot2.append(tuple(p_bot + out * thick + Vector((0, 0, -thick * 0.5))))
        top2.append(tuple(p_top + Vector((ca, sa, 0)) * thick * 0.6
                          + Vector((0, 0, thick))))
    ex = {"bones": ["Chest", "Neck", "Shoulder.L", "Shoulder.R"]}
    ex.update(extras)
    return _ring_loft([top, bot, bot2, top2], mat, bone, **ex)


def _strip(path: list, mat: str, bone: str, width: float, thick: float,
           up, bones: list[str] | None = None, **extras) -> Part:
    """A thin flat strip (slash, pane, strap, lame) laid along `path`. `up` is the
    outward direction; the strip is `width` wide across the path, `thick` deep."""
    ex = {"path": [tuple(p) for p in path], "section": (thick / 2, width / 2),
          "up": tuple(up), "bevel": False, "smooth": True}
    if bones:
        ex["bones"] = bones
    ex.update(extras)
    return Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, segments=4, extras=ex)


def _arm_frame(fig: Human, side: str):
    """(shoulder, elbow, wrist, axis of upper arm, outward, forward) for layering
    slashes, wings and couters on a sleeve."""
    sh, el, wr = (fig.joint(f"{j}.{side}") for j in ("shoulder", "elbow", "wrist"))
    axis = (el - sh).normalized()
    s = figures.SIDES[side]
    out = Vector((s, 0.0, 0.0))
    out = (out - out.dot(axis) * axis).normalized()
    fwd = axis.cross(out).normalized()
    if fwd.y > 0:
        fwd = -fwd
    return sh, el, wr, axis, out, fwd


def _sleeve_radius(fig: Human, t: float, pad: float) -> float:
    """Approximate radius of arm_part's upper-arm sweep at fraction t shoulder->elbow."""
    r = 0.030 + (0.0225 - 0.030) * t
    return r * fig.h * fig.bulk + pad


def _sword(fig: Human, hilt: Vector, direction, blade: float, steel: str, scabbard: str,
           grip_mat: str, swept: bool = True, chape: str | None = None,
           bone: str = "Hips") -> list[Part]:
    """A sword in its scabbard on a hanger: the hilt at `hilt` (top of the
    scabbard mouth), the scabbard running `blade` metres along `direction`
    (down and back), the grip, a guard and a pommel pointing the other way."""
    d = Vector(direction).normalized()
    tip = hilt + d * (blade + 0.03)
    rigid = {"rigid": True}
    parts = [Part("tube", (0, 0, 0), (1, 1, 1), mat=scabbard, bone=bone, segments=6,
                  extras={"path": [tuple(hilt), tuple(hilt + d * (blade * 0.6)), tuple(tip)],
                          "section": (0.020, 0.011), "up": (1.0, 0.0, 0.0),
                          "bevel": False, "smooth": True, **rigid})]
    if chape:
        parts.append(Part("cone", tuple(tip - d * 0.04), (0.034, 0.024, 0.08), mat=chape,
                          bone=bone, rot=_track(d), segments=6, taper=0.35,
                          extras={**rigid, "bevel": False}))
    # grip + pommel above the mouth, pointing up and forward
    g0 = hilt - d * 0.012
    g1 = hilt - d * 0.12
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=grip_mat, bone=bone, segments=6,
                      extras={"path": [tuple(g0), tuple(g1)], "section": (0.014, 0.014),
                              "bevel": False, "smooth": True, **rigid}))
    parts.append(Part("sphere", tuple(g1 - d * 0.018), (0.036, 0.036, 0.036), mat=steel,
                      bone=bone, segments=8, rings=5, extras={**rigid, "bevel": False}))
    # cross guard across the blade, and a swept knuckle bow / shell
    side = d.cross(Vector((0, -1, 0)))
    if side.length < 1e-3:
        side = Vector((1, 0, 0))
    side.normalize()
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=steel, bone=bone, segments=6,
                      extras={"path": [tuple(hilt - side * 0.09 - d * 0.02),
                                       tuple(hilt), tuple(hilt + side * 0.09 - d * 0.02)],
                              "section": (0.007, 0.007), "bevel": False, "smooth": True,
                              **rigid}))
    if swept:
        fwd = d.cross(side).normalized()
        if fwd.y > 0:
            fwd = -fwd
        bow = [hilt + side * 0.05 - d * 0.01, hilt + fwd * 0.05 - d * 0.05,
               hilt + fwd * 0.055 - d * 0.10, g1 + fwd * 0.012]
        parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=steel, bone=bone, segments=5,
                          extras={"path": spline([tuple(p) for p in bow], 2),
                                  "section": (0.005, 0.005), "bevel": False,
                                  "smooth": True, **rigid}))
    return parts


def _horn_lantern(fig: Human, ring: Vector, size: tuple, frame: str, pane: str,
                  bone: str, parent: str, prop: bool = True) -> list[Part]:
    """A small horn lantern hanging from `ring`: horn box, top and base plates, a
    pyramid cap, four corner posts, a ring. Rigid on its own bone."""
    w, _d, hgt = size
    cap_top = ring.z - 0.02
    body_top = cap_top - 0.045
    body_bot = body_top - hgt
    c = Vector((ring.x, ring.y, 0.0))
    fig.add_bone(bone, ring, (c.x, c.y, body_bot), parent)
    ex = {"prop": True} if prop else {"rigid": True}
    mid = (body_top + body_bot) / 2
    parts = [
        Part("torus", tuple(ring), (0.04, 0.04, 0.04), mat=frame, bone=bone,
             rot=(90.0, 0.0, 0.0), segments=8, rings=4, minor=0.18,
             extras={**ex, "bevel": False}),
        Part("box", (c.x, c.y, mid), (w - 0.01, w - 0.01, hgt - 0.01), mat=pane, bone=bone,
             extras=dict(ex)),
        Part("box", (c.x, c.y, body_bot + 0.006), (w + 0.01, w + 0.01, 0.012), mat=frame,
             bone=bone, extras=dict(ex)),
        Part("box", (c.x, c.y, body_top - 0.005), (w + 0.01, w + 0.01, 0.010), mat=frame,
             bone=bone, extras=dict(ex)),
        Part("cone", (c.x, c.y, body_top + 0.022), (w * 1.05, w * 1.05, 0.045), mat=frame,
             bone=bone, segments=4, rot=(0, 0, 45.0), taper=0.15, extras=dict(ex)),
    ]
    for sx, sy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
        parts.append(Part("box", (c.x + sx * w / 2, c.y + sy * w / 2, mid),
                          (0.012, 0.012, hgt), mat=frame, bone=bone, extras=dict(ex)))
    for face in range(4):   # one mid bar per face
        a = math.radians(90 * face)
        n = Vector((math.cos(a), math.sin(a), 0.0))
        at = c + n * (w / 2 - 0.003)
        parts.append(Part("box", (at.x, at.y, mid), (0.006, 0.006, hgt - 0.01), mat=frame,
                          bone=bone, rot=(0, 0, math.degrees(a)),
                          extras={**ex, "bevel": False}))
    return parts


def _band_z(side_hi: str, z_hi: float, z_lo: float, angle_deg: float) -> float:
    """Height of a shoulder-to-hip band at a torso angle (see _diag_band)."""
    zc, amp = (z_hi + z_lo) / 2, (z_hi - z_lo) / 2
    a_hi = 0.0 if side_hi == "L" else math.pi
    return zc + amp * math.cos(math.radians(angle_deg) - a_hi)


def _diag_band(fig: Human, mat: str, side_hi: str, z_hi: float, z_lo: float, pad: float,
               width: float, thick: float, n: int = 20) -> Part:
    """A sash or baldric: a closed flat band round the torso, high (z_hi) over the
    `side_hi` shoulder, low (z_lo) at the opposite hip."""
    ring = []
    for k in range(n):
        a = 360.0 * k / n
        ring.append(tuple(fig.surface(_band_z(side_hi, z_hi, z_lo, a), a, pad=pad)))
    amp = (z_hi - z_lo) / 2
    s = 1.0 if side_hi == "L" else -1.0
    tilt = Vector((-s * amp / 0.17, 0.0, 1.0)).normalized()
    return Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, bone="Chest", segments=4,
                extras={"path": ring, "closed": True, "section": (width / 2, thick / 2),
                        "up": tuple(tilt), "bevel": False, "smooth": True,
                        "bones": ["Hips", "Spine", "Chest", f"Shoulder.{side_hi}"]})


def _buttons(fig: Human, z0: float, z1: float, count: int, mat: str, pad: float,
             size: float = 0.014) -> list[Part]:
    out = []
    for i in range(count):
        z = z0 + (z1 - z0) * i / max(1, count - 1)
        p = fig.surface(z, -90.0, pad=pad)
        out.append(Part("sphere", tuple(p), (size, size * 0.7, size), mat=mat,
                        bone="Chest" if z > fig.chest_z else "Spine", segments=6, rings=4,
                        extras={"rigid": True, "bevel": False}))
    return out


# --------------------------------------------------------------------------------
# Palace Guard (partisan-guard, patrol) — the partisan first, then the boat-shaped
# crested morion, then the flared paned trunk-hose.
# --------------------------------------------------------------------------------

def _morion(fig: Human, base: Vector, steel: str, bone: str = "Morion") -> list[Part]:
    """Blued-steel morion: half-ellipsoid skull 0.24 long x 0.20 wide x 0.13 tall,
    a raised comb front to back, a brim upturned to points front and back (0.40 m
    tip to tip), 12 lining rivets, cheek-pieces. Rigid on its own bone."""
    skull_h, rx, ry = 0.13, 0.100, 0.120
    fig.add_bone(bone, base, base + Vector((0, 0, skull_h)), "Head")
    rigid = {"rigid": True}
    prof = []
    for k in range(7):
        t = math.radians(90.0 * k / 6)
        prof.append((max(0.0, math.cos(t)), math.sin(t)))
    profile = [(0.95, -0.03)] + [(r, z) for r, z in prof[:-1]] + [(0.0, 1.0)]
    parts = [Part("lathe", tuple(base), (rx, ry, skull_h), mat=steel, bone=bone,
                  segments=20, extras={"profile": profile, "smooth": True, "bevel": False,
                                       **rigid})]
    # comb: a crescent crest in the YZ plane, riding the skull from front to back
    outline_top, outline_bot = [], []
    for k in range(11):
        u = -0.105 + 0.21 * k / 10
        sk = skull_h * math.sqrt(max(0.0, 1.0 - (u / ry) ** 2))
        crest = 0.055 * math.sqrt(max(0.0, 1.0 - (u / 0.108) ** 2)) ** 0.8
        outline_top.append((u, sk + crest))
        outline_bot.append((u, sk - 0.012))
    outline = outline_top + list(reversed(outline_bot))
    parts.append(Part("prism", tuple(base), (1, 1, 0.008), mat=steel, bone=bone,
                      rot=(90.0, 0.0, 90.0), extras={"outline": outline, **rigid,
                                                     "bevel": False}))
    # brim: angle-dependent annulus. Along Y (front/back) it reaches 0.20 m and
    # sweeps up; at the sides it hugs the skull.
    n = 28
    rings = [[], [], [], []]
    for j in range(n):
        a = 2.0 * math.pi * j / n
        ca, sa = math.cos(a), math.sin(a)
        f = abs(sa) ** 3
        r_in_x, r_in_y = rx * 0.97, ry * 0.97
        inner = Vector((ca * r_in_x, sa * r_in_y, 0.0))
        reach = 0.030 + (0.200 - ry - 0.010) * f
        lift = 0.012 + 0.085 * abs(sa) ** 4
        outer = Vector((ca * (r_in_x + reach), sa * (r_in_y + reach), lift))
        rings[0].append(tuple(base + inner + Vector((0, 0, 0.012))))
        rings[1].append(tuple(base + outer + Vector((0, 0, 0.006))))
        rings[2].append(tuple(base + outer + Vector((0, 0, -0.004))))
        rings[3].append(tuple(base + inner + Vector((0, 0, -0.006))))
    parts.append(_ring_loft(rings, steel, bone, **rigid))
    # rivets round the base of the skull
    for k in range(12):
        a = 2.0 * math.pi * (k + 0.5) / 12
        at = base + Vector((math.cos(a) * rx * 1.0, math.sin(a) * ry * 1.0, 0.025))
        parts.append(Part("sphere", tuple(at), (0.015, 0.015, 0.015), mat=steel, bone=bone,
                          segments=5, rings=3, extras={**rigid, "bevel": False}))
    # cheek-pieces, 0.12 m, hanging from the sides
    for s in (1.0, -1.0):
        at = base + Vector((s * (rx * 0.93), -0.012, -0.055))
        parts.append(Part("box", tuple(at), (0.010, 0.065, 0.12), mat=steel, bone=bone,
                          rot=(0.0, s * -6.0, 0.0), extras=dict(rigid)))
    return parts


def _partisan(fig: Human, steel: str, haft: str, tassel: str) -> list[Part]:
    """Partisan: 2.20 m octagonal ash haft (32 mm), 0.42 m blued-steel head (leaf
    blade 0.07 m wide, two upturned flukes 0.20 m across), socket with two 0.30 m
    langets, 0.08 m ferrule, two murrey tassels. Upright in the right fist, butt on
    the ground; a prop bone on Hand.R. The head faces the front."""
    g = fig.grip("R")
    x, y = g.x, g.y
    haft_len, head_len, socket = 2.20, 0.42, 0.10
    top = haft_len
    fig.prop_bone("Partisan", "R", head=g, tail=(x, y, top + head_len))
    prop = {"prop": True}
    parts = [
        Part("cyl", (x, y, haft_len / 2), (0.032, 0.032, haft_len), mat=haft,
             bone="Partisan", segments=8, extras={**prop, "bevel": False}),
        Part("cyl", (x, y, 0.04), (0.036, 0.036, 0.08), mat=steel, bone="Partisan",
             segments=8, extras={**prop, "bevel": False}),
        Part("cyl", (x, y, top - socket / 2 + 0.02), (0.040, 0.040, socket), mat=steel,
             bone="Partisan", segments=8, taper=0.85, extras={**prop, "bevel": False}),
        Part("sphere", (x, y, top + 0.02), (0.05, 0.05, 0.03), mat=steel, bone="Partisan",
             segments=8, rings=4, extras={**prop, "bevel": False}),
    ]
    for s in (1.0, -1.0):   # langets down the haft, left and right of the blade
        parts.append(Part("box", (x + s * 0.017, y, top - 0.08 - 0.15), (0.005, 0.012, 0.30),
                          mat=steel, bone="Partisan", extras={**prop, "bevel": False}))
    # blade outline in (u across, v up from the haft top): leaf blade with flukes
    base_v = 0.03
    L = head_len - base_v
    outline = [(0.0, base_v), (0.022, base_v), (0.03, base_v + 0.04),
               (0.10, base_v + 0.03), (0.105, base_v + 0.10),       # right fluke tip
               (0.085, base_v + 0.075), (0.036, base_v + 0.085),
               (0.035, base_v + 0.16), (0.022, base_v + 0.28), (0.0, base_v + L)]
    full = outline + [(-u, v) for u, v in reversed(outline[1:-1])]
    parts.append(Part("prism", (x, y, top), (1, 1, 0.012), mat=steel, bone="Partisan",
                      rot=(90.0, 0.0, 0.0), extras={"outline": full, **prop}))
    # the midrib, thicker, so the etched blade reads in the side view too
    parts.append(Part("prism", (x, y, top), (1, 1, 0.022), mat=steel, bone="Partisan",
                      rot=(90.0, 0.0, 0.0),
                      extras={"outline": [(-0.012, base_v + 0.02), (0.012, base_v + 0.02),
                                          (0.006, base_v + L - 0.05), (0.0, base_v + L - 0.02),
                                          (-0.006, base_v + L - 0.05)], **prop}))
    # two murrey tassels hanging at the socket, front and back
    for s in (1.0, -1.0):
        at = Vector((x + s * 0.028, y, top - socket - 0.03))
        parts.append(Part("cone", tuple(at), (0.036, 0.036, 0.11), mat=tassel,
                          bone="Partisan", segments=6, taper=0.25,
                          rot=(0.0, 180.0, 0.0), extras={**prop, "bevel": False}))
        parts.append(Part("sphere", tuple(at + Vector((0, 0, 0.06))), (0.026, 0.026, 0.026),
                          mat=tassel, bone="Partisan", segments=6, rings=4,
                          extras={**prop, "bevel": False}))
    return parts


def _trunk_hose(fig: Human, side: str, mat: str, lining: str, width: float,
                bottom_z: float) -> list[Part]:
    """One leg of paned trunk-hose: a pumpkin puff from the waist to mid-thigh
    around the thigh, with thin lining strips (the gaps between the 6 cm panes)
    down its meridians. Weighted to Hips and the thigh."""
    s = figures.SIDES[side]
    hip, knee = fig.joint(f"hip.{side}"), fig.joint(f"knee.{side}")
    axis = (knee - hip).normalized()
    top_z = fig.belt_z + 0.01
    cx = s * (width / 2 - 0.13)
    span = top_z - bottom_z
    stations = [   # (z, half-width x, half-depth y): widest low, round as a gourd
        (top_z, 0.095, 0.085),
        (top_z - 0.18 * span, 0.122, 0.112),
        (top_z - 0.42 * span, 0.146, 0.132),
        (top_z - 0.66 * span, 0.140, 0.126),
        (top_z - 0.86 * span, 0.108, 0.100),
        (bottom_z + 0.012, 0.074, 0.072),
        (bottom_z, 0.062, 0.062),
    ]
    pts, secs = [], []
    for z, hx, hy in stations:
        f = max(0.0, min(1.0, (hip.z + 0.05 - z) / (hip.z - bottom_z + 0.05)))
        c = Vector((cx, 0.0, z)).lerp(hip + axis * ((hip.z - z) / max(1e-6, -axis.z)), f)
        pts.append(c)
        secs.append((hy, hx))
    bones = ["Hips", f"UpperLeg.{side}"]
    parts = [Part("sweep", (0, 0, 0), (1, 1, 1), mat=mat, bone=f"UpperLeg.{side}",
                  segments=16, extras={"path": [tuple(p) for p in pts], "sections": secs,
                                       "up": (0.0, 1.0, 0.0), "smooth": True,
                                       "bevel": False, "bones": bones})]
    # lining gaps: 7 per leg, skipping the inner thigh
    for k in range(7):
        a = math.radians(-160.0 + 320.0 * k / 6) if s > 0 else math.radians(20.0 + 320.0 * k / 6)
        ca, sa = math.cos(a), math.sin(a)
        if ca * s < -0.55:
            continue
        path = []
        for p, (hy, hx) in zip(pts[1:-1], secs[1:-1]):
            path.append(p + Vector((ca * hx * 1.02, sa * hy * 1.02, 0.0)))
        parts.append(_strip(path, lining, f"UpperLeg.{side}", 0.014, 0.004,
                            up=(ca, sa, 0.0), bones=bones))
    return parts


def partisan_guard(entry: Entry):
    # 1.80 m proportions, slim; the morion's comb brings the crown to ~1.85 m.
    fig = Human(height=1.76, bulk=0.95, shoulders=0.46,
                arm_r=ArmPose(spread=12.0, swing=2.0, elbow=32.0),
                arm_l=ArmPose(spread=14.0, swing=-2.0, elbow=48.0))
    wool, lining, steel = "murrey_livery_wool", "pane_lining", "blued_steel"
    pad = 0.012
    parts = [fig.torso_part(wool, pad=pad, chest=0.13, segments=24)]
    for side in ("L", "R"):
        s = figures.SIDES[side]
        parts.append(fig.arm_part(side, wool, pad=0.006))
        parts += fig.hand_part(side, "skin")
        parts.append(fig.leg_part(side, wool, pad=0.004))
        parts.append(fig.foot_part(side, "black_leather", length=0.27, point=0.1))
        # rosette on the latchet
        ball = fig.joint(f"ball.{side}")
        parts.append(Part("sphere", (ball.x, ball.y + 0.03, 0.060), (0.045, 0.03, 0.035),
                          mat=lining, bone=f"Foot.{side}", segments=6, rings=4,
                          extras={"rigid": True, "bevel": False}))
        # garter with a ribbon bow below the knee
        knee, ankle = fig.joint(f"knee.{side}"), fig.joint(f"ankle.{side}")
        garter = knee + (ankle - knee) * 0.12
        parts.append(Part("cyl", tuple(garter), (0.086, 0.088, 0.025), mat="black_leather",
                          bone=f"LowerLeg.{side}", segments=12,
                          extras={"bevel": False, "smooth": True,
                                  "bones": [f"LowerLeg.{side}", f"UpperLeg.{side}"]}))
        for dx in (0.03, -0.03):
            parts.append(Part("sphere", (garter.x + s * 0.045 + dx * 0.4, garter.y - 0.01 + dx,
                                         garter.z), (0.03, 0.04, 0.045), mat=lining,
                              bone=f"LowerLeg.{side}", segments=6, rings=4,
                              extras={"rigid": True, "bevel": False}))
        parts += _trunk_hose(fig, side, wool, lining, width=0.55,
                             bottom_z=fig.hip_z - 0.52 * (fig.hip_z - fig.knee_z))
        # slashed upper sleeve: 5 lining panes, and a 5 cm shoulder-wing roll
        sh, el, _wr, axis, out, fwd = _arm_frame(fig, side)
        for k in range(5):
            ang = math.radians(-70.0 + 140.0 * k / 4)
            dirn = out * math.cos(ang) + fwd * math.sin(ang)
            path = []
            for t in (0.18, 0.4, 0.62, 0.8):
                r = _sleeve_radius(fig, t, 0.006) + 0.002
                path.append(sh + (el - sh) * t + dirn * r)
            parts.append(_strip(path, lining, f"UpperArm.{side}", 0.013, 0.004, up=dirn,
                                bones=[f"UpperArm.{side}", f"Shoulder.{side}"]))
        wing = sh + (el - sh) * 0.07
        parts.append(Part("torus", tuple(wing), (0.135, 0.12, 0.13), mat=wool,
                          bone=f"UpperArm.{side}", rot=_track(axis), segments=12, rings=6,
                          minor=0.28, extras={"bevel": False, "smooth": True,
                                              "bones": [f"UpperArm.{side}",
                                                        f"Shoulder.{side}", "Chest"]}))

    # Head: bare face, a dark goatee and moustache; the morion over it.
    parts += fig.head_part("skin", face="skin", features="black_leather")
    chin = fig.lean((0.0, -0.050 * fig.h, 0.878 * fig.h))
    parts.append(Part("cone", tuple(chin), (0.028, 0.02, 0.05), mat="black_leather",
                      bone="Head", rot=(180.0, 0.0, 0.0), segments=6, taper=0.3,
                      extras={"rigid": True, "bevel": False}))
    lip = fig.lean((0.0, -0.058 * fig.h, 0.897 * fig.h))
    parts.append(Part("box", tuple(lip), (0.055, 0.012, 0.008), mat="black_leather",
                      bone="Head", extras={"rigid": True, "bevel": False}))
    base = fig.lean((0.0, 0.004 * fig.h, 0.957 * fig.h))
    parts += _morion(fig, base, steel)

    # Gorget (two lames) under a white falling collar.
    parts.append(_drape(fig, steel, fig.neck_z + 0.03, (0.066, 0.058), fig.neck_z - 0.05,
                        pad=0.022, thick=0.008))
    parts.append(_drape(fig, steel, fig.neck_z - 0.005, (0.10, 0.075), fig.neck_z - 0.07,
                        pad=0.026, thick=0.006))
    parts.append(_drape(fig, "linen_collar", fig.neck_z + 0.045, (0.062, 0.056),
                        fig.neck_z - 0.035, pad=0.045, thick=0.008, front_drop=0.02))

    # 12 buttons down the doublet front, to the peascod point.
    parts += _buttons(fig, fig.belt_z + 0.05, fig.neck_z - 0.08, 12, steel, pad + 0.018,
                      size=0.013)

    # Sash, right shoulder to left hip, knotted with a fringed tail.
    z_hi, z_lo = fig.shoulder_z - 0.03, fig.belt_z - 0.02
    parts.append(_diag_band(fig, wool, "R", z_hi, z_lo, pad + 0.012, 0.18, 0.010))
    knot = fig.surface(z_lo, -20.0, pad=pad + 0.03)
    parts.append(Part("sphere", tuple(knot), (0.08, 0.05, 0.07), mat=wool, bone="Hips",
                      segments=8, rings=5, extras={"rigid": True, "bevel": False}))
    tail = [knot + Vector((0.01, -0.01, -0.02)), knot + Vector((0.03, -0.02, -0.14)),
            knot + Vector((0.04, -0.02, -0.30))]
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat=wool, bone="Hips", segments=6,
                      extras={"path": [tuple(p) for p in tail],
                              "sections": [(0.012, 0.04), (0.010, 0.045), (0.008, 0.05)],
                              "up": (0.0, 1.0, 0.0), "power": 3.0, "smooth": True,
                              "bevel": False, "rigid": True}))
    fringe = tail[-1] + Vector((0, 0, -0.03))
    parts.append(Part("box", tuple(fringe), (0.10, 0.012, 0.05), mat="black_leather",
                      bone="Hips", extras={"rigid": True, "bevel": False}))

    # Belt 4 cm, steel buckle; sword on a hanger at the left hip; lantern on a
    # belt hook at the right front.
    parts.append(fig.band(fig.belt_z, "black_leather", height=0.04, pad=0.02, torso_pad=pad))
    buckle = fig.surface(fig.belt_z, -90.0, pad=pad + 0.03)
    parts.append(Part("torus", tuple(buckle), (0.05, 0.05, 0.045), mat=steel, bone="Hips",
                      rot=(90, 0, 0), segments=4, rings=4, minor=0.2,
                      extras={"rigid": True, "bevel": False}))
    hilt = fig.surface(fig.belt_z - 0.05, -25.0, pad=pad + 0.06)
    parts += _sword(fig, hilt, (0.10, 0.55, -0.83), 0.85, steel, "black_leather",
                    "black_leather", swept=True, chape=steel)
    purse = fig.surface(fig.belt_z - 0.07, 150.0, pad=pad + 0.03)
    parts.append(Part("box", tuple(purse), (0.07, 0.03, 0.10), mat="black_leather",
                      bone="Hips", rot=(0, 0, 60.0), extras={"rigid": True}))
    hook = fig.surface(fig.belt_z - 0.02, -150.0, pad=pad + 0.07)
    parts.append(Part("cyl", tuple(hook + Vector((0, 0, 0.02))), (0.012, 0.012, 0.05),
                      mat=steel, bone="Hips", segments=5, extras={"rigid": True,
                                                                  "bevel": False}))
    parts += _horn_lantern(fig, hook - Vector((0, 0, 0.02)), (0.12, 0.12, 0.22), steel,
                           "horn_pane", "Lantern", "Hips")

    parts += _partisan(fig, steel, "ash_haft", wool)

    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(),
        family_overrides={
            "horn_pane": {"emit": "#4A1E0C", "rough": 0.45},
            # JSON roughness 0.35 renders near-black in the review studio (README
            # Traps: metals render dark); 0.5 lets the blue read as on the concept.
            "blued_steel": {"rough": 0.5, "wear_to": "#6E7E92", "wear_amount": 0.35},
        },
        extra_families={
            **SKIN,
            "pane_lining": {"name": "Murrey pane lining", "base": "#8A5A5E", "rough": 0.85,
                            "notes": "The paler lining under the slashed panes: JSON "
                                     "'lighter pane lining #8A5A5E'. Also the ribbon "
                                     "rosettes and garter bows."},
        },
        notes=[
            "Partisan (2.62 m upright) is a prop bone on Hand.R, left out of the height "
            "check; carried grounded as on the concept sheet, not sloped.",
            "Morion is its own bone under Head (detachable). Its 12 lining rivets are "
            "blued steel, not brass: brass reads as orpiment and he carries no plunder.",
            "Lantern hangs on its own bone under Hips (the belt socket).",
            "Sash is a single murrey band (no black stripe). Not built: sash_01-03 and "
            "breech_L/R spring bones, the purse's coin, IK target for the left hand.",
        ])


# --------------------------------------------------------------------------------
# Musketeer (ranged) — two glowing match ends, the broad plumed hat, the long
# musket line, the forked rest, the ring of hanging apostles.
# --------------------------------------------------------------------------------

def _broad_hat(fig: Human, base: Vector, felt: str, band: str, plume: str,
               crown_h: float = 0.16) -> list[Part]:
    """Black felt hat: truncated-cone crown (0.18 m across the top), a 0.46 m brim
    with a soft wavy edge, cocked up on the figure's left, a murrey band and one
    ostrich plume curling from the cocked side to the back. On its own Hat bone."""
    fig.add_bone("Hat", base, base + Vector((0, 0, crown_h)), "Head")
    rigid = {"rigid": True}
    profile = [(0.098, -0.01), (0.104, 0.02), (0.100, crown_h * 0.6), (0.092, crown_h - 0.01),
               (0.085, crown_h), (0.0, crown_h + 0.004)]
    parts = [Part("lathe", tuple(base), (1, 1.08, 1), mat=felt, bone="Hat", segments=16,
                  extras={"profile": profile, "smooth": True, "bevel": False, **rigid})]
    parts.append(Part("lathe", tuple(base), (1, 1.08, 1), mat=band, bone="Hat", segments=16,
                      extras={"profile": [(0.107, 0.012), (0.108, 0.045), (0.101, 0.045),
                                          (0.100, 0.012)],
                              "smooth": True, "bevel": False, **rigid}))
    n = 28
    rings = [[], [], [], []]
    for j in range(n):
        a = 2.0 * math.pi * j / n
        ca, sa = math.cos(a), math.sin(a)
        cock = max(0.0, ca) ** 3          # the left side (+X) turned up
        r_out = 0.23 - 0.03 * cock
        wave = 0.006 * math.sin(5 * a)
        lift = -0.010 + wave + 0.13 * cock + 0.015 * sa ** 2
        inner = Vector((ca * 0.097, sa * 0.105, 0.0))
        outer = Vector((ca * r_out, sa * r_out * 1.02, lift))
        if cock > 0.3:   # turned-up brim leans back against the crown
            outer.x = ca * (0.20 - 0.07 * (cock - 0.3))
        rings[0].append(tuple(base + inner + Vector((0, 0, 0.008))))
        rings[1].append(tuple(base + outer + Vector((0, 0, 0.005))))
        rings[2].append(tuple(base + outer + Vector((0, 0, -0.005))))
        rings[3].append(tuple(base + inner + Vector((0, 0, -0.006))))
    parts.append(_ring_loft(rings, felt, "Hat", **rigid))
    # the pin holding the cocked side, and the plume from it, curling to the back
    pin = base + Vector((0.115, -0.02, 0.07))
    parts.append(Part("sphere", tuple(pin), (0.022, 0.016, 0.022), mat=band, bone="Hat",
                      segments=6, rings=4, extras={**rigid, "bevel": False}))
    ctrl = [pin, pin + Vector((0.01, -0.03, 0.08)), pin + Vector((-0.04, 0.02, 0.125)),
            pin + Vector((-0.10, 0.11, 0.11)), pin + Vector((-0.13, 0.20, 0.06)),
            pin + Vector((-0.12, 0.25, -0.02))]
    path = spline([tuple(p) for p in ctrl], 2)
    m = len(path)
    secs = []
    for i in range(m):
        t = i / (m - 1)
        w = 0.012 + 0.05 * math.sin(math.pi * min(1.0, t * 1.15)) if i < m - 1 else 0.0
        secs.append((w, w * 0.35) if i < m - 1 else (0.0, 0.0))
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat=plume, bone="Hat", segments=6,
                      extras={"path": path, "sections": secs, "up": (1.0, 0.0, 0.0),
                              "smooth": True, "bevel": False, **rigid}))
    return parts


def _matchlock(fig: Human, stock: str, steel: str, match_mat: str) -> list[Part]:
    """Matchlock musket, 1.55 m: 1.15 m blued barrel (octagonal breech, round
    muzzle), walnut stock with a fish-tail butt, flat lockplate, S-shaped
    serpentine holding the match, ramrod under the barrel. Held grounded at the
    order in the right fist, lock outward; a prop bone on Hand.R."""
    g = fig.grip("R")
    x, y = g.x, g.y + 0.012
    overall, barrel = 1.55, 1.15
    breech = overall - barrel
    fig.prop_bone("Musket", "R", head=g, tail=(x, y, overall))
    prop = {"prop": True}
    # stock profile in (u = toward the front of the gun = -Y, v = up)
    outline = [(-0.05, 0.0), (0.075, 0.0), (0.06, 0.05), (0.035, 0.20), (0.03, 0.34),
               (0.022, breech + 0.02), (0.025, breech + 0.58), (0.012, breech + 0.62),
               (-0.018, breech + 0.62), (-0.020, breech + 0.02), (-0.018, 0.34),
               (-0.020, 0.30), (-0.045, 0.06)]
    # rot (90, 0, 90): u -> +Y, v -> +Z, extrusion along X. Negate u for -Y front.
    parts = [Part("prism", (x, y, 0.0), (1, 1, 0.045), mat=stock, bone="Musket",
                  rot=(90.0, 0.0, 90.0),
                  extras={"outline": [(-u, v) for u, v in outline], **prop})]
    parts.append(Part("cyl", (x, y - 0.004, breech + 0.20), (0.034, 0.034, 0.40),
                      mat=steel, bone="Musket", segments=8, taper=0.9,
                      extras={**prop, "bevel": False}))
    parts.append(Part("cyl", (x, y - 0.004, breech + 0.40 + (barrel - 0.40) / 2),
                      (0.028, 0.028, barrel - 0.40), mat=steel, bone="Musket", segments=8,
                      taper=0.92, extras={**prop, "bevel": False, "smooth": True}))
    parts.append(Part("cyl", (x, y - 0.004, overall - 0.012), (0.032, 0.032, 0.024),
                      mat=steel, bone="Musket", segments=8, extras={**prop, "bevel": False}))
    # ramrod in front of the barrel (under it, as the gun is held)
    parts.append(Part("cyl", (x, y - 0.034, breech + 0.55), (0.009, 0.009, 1.0), mat=steel,
                      bone="Musket", segments=5, extras={**prop, "bevel": False}))
    # lockplate on the outer (right, -X) side and the S serpentine
    lx = x - 0.026
    parts.append(Part("box", (lx, y + 0.005, breech + 0.02), (0.008, 0.04, 0.18), mat=steel,
                      bone="Musket", extras=dict(prop)))
    serp = [(lx - 0.006, y + 0.02, breech - 0.04), (lx - 0.010, y + 0.045, breech - 0.01),
            (lx - 0.010, y + 0.035, breech + 0.03), (lx - 0.008, y + 0.005, breech + 0.04),
            (lx - 0.008, y - 0.01, breech + 0.07)]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=steel, bone="Musket", segments=5,
                      extras={"path": spline(serp, 2), "section": (0.005, 0.005),
                              "smooth": True, "bevel": False, **prop}))
    return parts


def _rest(fig: Human, staff: str, iron: str) -> list[Part]:
    """Forked musket rest: 1.30 m ash staff, 0.08 m iron U-fork at the top, iron
    spike at the foot, upright in the left fist."""
    g = fig.grip("L")
    x, y = g.x, g.y
    fig.prop_bone("Rest", "L", head=g, tail=(x, y, 1.30))
    prop = {"prop": True}
    parts = [Part("cyl", (x, y, 0.65), (0.026, 0.026, 1.18), mat=staff, bone="Rest",
                  segments=6, extras={**prop, "bevel": False, "smooth": True}),
             Part("cone", (x, y, 0.035), (0.03, 0.03, 0.07), mat=iron, bone="Rest",
                  segments=5, rot=(180.0, 0.0, 0.0), taper=0.05,
                  extras={**prop, "bevel": False})]
    fork = [(x - 0.045, y, 1.33), (x - 0.042, y, 1.27), (x - 0.02, y, 1.24),
            (x + 0.02, y, 1.24), (x + 0.042, y, 1.27), (x + 0.045, y, 1.33)]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=iron, bone="Rest", segments=5,
                      extras={"path": spline(fork, 2), "section": (0.008, 0.008),
                              "smooth": True, "bevel": False, **prop}))
    parts.append(Part("cyl", (x, y, 1.23), (0.03, 0.03, 0.03), mat=iron, bone="Rest",
                      segments=6, extras={**prop, "bevel": False}))
    return parts


def _slow_match(fig: Human, cord: str, ember: str) -> list[Part]:
    """The slow match looped over the left fist, lit at both ends: a hanging loop
    of 8 mm cord and two glowing ember tips. On its own bone under Hand.L."""
    g = fig.grip("L")
    s = 1.0
    fig.prop_bone("Match", "L", head=g, tail=g + Vector((0, 0, -0.25)))
    prop = {"prop": True}
    loop = [g + Vector((s * 0.05, -0.02, 0.06)), g + Vector((s * 0.045, 0.0, -0.02)),
            g + Vector((s * 0.06, 0.01, -0.15)), g + Vector((s * 0.03, 0.02, -0.26)),
            g + Vector((s * -0.02, 0.01, -0.22)), g + Vector((s * -0.005, -0.02, -0.10)),
            g + Vector((s * 0.03, -0.05, -0.04))]
    path = spline([tuple(p) for p in loop], 2)
    parts = [Part("tube", (0, 0, 0), (1, 1, 1), mat=cord, bone="Match", segments=5,
                  extras={"path": path, "section": (0.005, 0.005), "smooth": True,
                          "bevel": False, **prop})]
    tail = Vector(path[-1]) + Vector((0.0, -0.06, -0.12))
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=cord, bone="Match", segments=5,
                      extras={"path": [path[-1], tuple(tail)], "section": (0.005, 0.005),
                              "smooth": True, "bevel": False, **prop}))
    for tip in (Vector(path[0]), tail):
        parts.append(Part("sphere", tuple(tip), (0.022, 0.022, 0.022), mat=ember,
                          bone="Match", segments=6, rings=4, extras={**prop, "bevel": False}))
    return parts


def musketeer(entry: Entry):
    # A 1.80 m man, broader-shouldered; the hat crown and plume reach ~1.92 m.
    fig = Human(height=1.79, bulk=1.05, shoulders=0.52,
                arm_r=ArmPose(spread=12.0, swing=-4.0, elbow=38.0),
                arm_l=ArmPose(spread=14.0, swing=-2.0, elbow=40.0))
    buff, wool, felt = "buff_leather", "murrey_livery_wool", "black_felt"
    walnut, steel = "black_walnut", "blued_steel"
    pad = 0.016   # the 8 mm buff coat over a doublet
    rain = {"mat": "buff_rain", "min": (-1, -1, fig.shoulder_z - 0.03), "max": (1, 1, 3)}
    parts = [fig.torso_part(buff, pad=pad, hem=0.70, hem_flare=1.30, collar=0.02,
                            segments=24, paint=[rain])]
    for side in ("L", "R"):
        s = figures.SIDES[side]
        el = fig.joint(f"elbow.{side}")
        # buff coat sleeve to the elbow, murrey wool sleeve below it
        parts.append(fig.arm_part(side, buff, pad=0.010, paint=[
            {"mat": wool, "min": (-1, -1, -1), "max": (1, 1, el.z - 0.03)},
            {"mat": "buff_rain", "min": (-1, -1, fig.shoulder_z - 0.02), "max": (1, 1, 3)}]))
        parts += fig.hand_part(side, "skin")
        knee = fig.joint(f"knee.{side}")
        parts.append(fig.leg_part(side, wool, pad=0.006, paint=[
            {"mat": buff, "min": (-1, -1, -1), "max": (1, 1, knee.z - 0.04)}]))
        parts.append(fig.foot_part(side, buff, length=0.29, point=0.05))
        # bucket-top boot: a flared cuff turned down 0.15 m below the knee
        cuff = knee + (fig.joint(f"ankle.{side}") - knee) * 0.08
        parts.append(Part("lathe", tuple(cuff), (1, 1, 1), mat=buff, bone=f"LowerLeg.{side}",
                          segments=12, extras={
                              "profile": [(0.058, -0.15), (0.074, -0.14), (0.098, -0.02),
                                          (0.100, 0.0), (0.090, 0.004), (0.060, -0.02)],
                              "smooth": True, "bevel": False,
                              "bones": [f"LowerLeg.{side}", f"UpperLeg.{side}"]}))
        # short buff wings on the shoulders
        sh, _el, _wr, axis, _out, _fwd = _arm_frame(fig, side)
        wing = sh + (el - sh) * 0.08
        parts.append(Part("torus", tuple(wing), (0.15, 0.13, 0.14), mat="buff_rain",
                          bone=f"UpperArm.{side}", rot=_track(axis), segments=12, rings=5,
                          minor=0.24, extras={"bevel": False, "smooth": True,
                                              "bones": [f"UpperArm.{side}",
                                                        f"Shoulder.{side}", "Chest"]}))

    # Head: moustache, goatee and shoulder-length hair under the hat.
    parts += fig.head_part("skin", face="skin", features=felt)
    chin = fig.lean((0.0, -0.050 * fig.h, 0.880 * fig.h))
    parts.append(Part("cone", tuple(chin), (0.026, 0.02, 0.05), mat=walnut, bone="Head",
                      rot=(180.0, 0.0, 0.0), segments=6, taper=0.3,
                      extras={"rigid": True, "bevel": False}))
    lip = fig.lean((0.0, -0.057 * fig.h, 0.898 * fig.h))
    parts.append(Part("box", tuple(lip), (0.06, 0.012, 0.010), mat=walnut, bone="Head",
                      rot=(0, 0, 0), extras={"rigid": True, "bevel": False}))
    for s in (1.0, -1.0):
        top = fig.lean((s * 0.070, 0.010, 0.955 * fig.h))
        bot = fig.lean((s * 0.078, 0.022, 0.878 * fig.h))
        parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat=walnut, bone="Head", segments=6,
                          extras={"path": [tuple(top), tuple(top.lerp(bot, 0.5)), tuple(bot)],
                                  "sections": [(0.030, 0.050), (0.026, 0.050), (0.018, 0.040)],
                                  "up": (1.0, 0.0, 0.0), "smooth": True, "bevel": False,
                                  "rigid": True}))
    base = fig.lean((0.0, 0.004 * fig.h, 0.952 * fig.h))
    parts += _broad_hat(fig, base, felt, wool, wool, crown_h=0.165)

    # Falling linen band collar.
    parts.append(_drape(fig, "linen_collar", fig.neck_z + 0.045, (0.064, 0.058),
                        fig.neck_z - 0.03, pad=pad + 0.03, thick=0.008, front_drop=0.02))
    # Hooks down the coat front.
    for i in range(5):
        z = fig.belt_z + 0.08 + i * 0.075
        p = fig.surface(z, -90.0, pad=pad + 0.012)
        parts.append(Part("box", tuple(p), (0.03, 0.008, 0.008), mat=steel, bone="Spine",
                          extras={"rigid": True, "bevel": False}))

    # Waist-belt (black felt-black leather) with a short sword on the left.
    parts.append(fig.band(fig.belt_z, felt, height=0.045, pad=0.012, torso_pad=pad))
    buckle = fig.surface(fig.belt_z, -90.0, pad=pad + 0.022)
    parts.append(Part("torus", tuple(buckle), (0.05, 0.05, 0.05), mat=steel, bone="Hips",
                      rot=(90, 0, 0), segments=4, rings=4, minor=0.2,
                      extras={"rigid": True, "bevel": False}))
    hilt = fig.surface(fig.belt_z - 0.04, -20.0, pad=pad + 0.07)
    parts += _sword(fig, hilt, (0.14, 0.50, -0.85), 0.70, steel, felt, walnut, swept=False)
    # Bullet bag at the right front hip, priming flask beside it.
    bag = fig.surface(fig.belt_z - 0.09, -128.0, pad=pad + 0.05)
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat=buff, bone="Hips", segments=8,
                      extras={"path": [tuple(bag + Vector((0, 0, 0.06))), tuple(bag),
                                       tuple(bag - Vector((0, 0, 0.05)))],
                              "sections": [(0.03, 0.045), (0.045, 0.06), (0.0, 0.0)],
                              "up": (0.0, 1.0, 0.0), "power": 2.5, "smooth": True,
                              "bevel": False, "rigid": True}))
    flask = fig.surface(fig.belt_z - 0.10, -100.0, pad=pad + 0.04)
    parts.append(Part("cone", tuple(flask), (0.05, 0.03, 0.14), mat="buff_rain", bone="Hips",
                      segments=6, taper=0.35, rot=(180.0, 0.0, 10.0),
                      extras={"rigid": True, "bevel": False}))

    # Bandolier: 5 cm felt baldric over the left shoulder to the right hip,
    # twelve apostles hanging off the front and flanks.
    z_hi, z_lo = fig.shoulder_z - 0.02, fig.belt_z + 0.02
    parts.append(_diag_band(fig, felt, "L", z_hi, z_lo, pad + 0.010, 0.05, 0.008))
    for k in range(12):
        ang = -30.0 - 120.0 * k / 11            # left-front round to the right flank
        zb = _band_z("L", z_hi, z_lo, ang)
        top = fig.surface(zb - 0.02, ang, pad=pad + 0.03)
        c = top - Vector((0, 0, 0.055))
        bone = "Chest" if zb > fig.chest_z else "Spine"
        parts.append(Part("cyl", tuple(c), (0.035, 0.035, 0.10),
                          mat="turned_wood_chargers", bone=bone, segments=6,
                          extras={"rigid": True, "bevel": False}))
        parts.append(Part("cyl", tuple(c + Vector((0, 0, 0.058))), (0.040, 0.040, 0.022),
                          mat=walnut, bone=bone, segments=6,
                          extras={"rigid": True, "bevel": False}))

    parts += _matchlock(fig, walnut, steel, "turned_wood_chargers")
    parts += _rest(fig, "ash_haft", steel)
    parts += _slow_match(fig, "turned_wood_chargers", "match_ember")

    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(),
        family_overrides={
            # 9x shipping emission blows #C4542E out to white; store it dimmer so
            # the ember reads as the madder glow on the concept.
            "match_ember": {"emit": "#5A2410", "rough": 0.8},
            "blued_steel": {"rough": 0.5, "wear_to": "#6E7E92", "wear_amount": 0.3},
            "buff_leather": {"grain": 0.35},
        },
        extra_families={
            **SKIN,
            "buff_rain": {"name": "Buff leather, rain-darkened", "base": "#7C6848",
                          "rough": 0.85, "notes": "JSON buff-leather note: 'rain-dark on "
                          "shoulders #7C6848'. Also the horn priming flask."},
            "linen_collar": {"name": "Linen collar", "base": "#D2C7AC", "rough": 0.9,
                             "notes": "Build bullet 'plain falling linen band collar'; the "
                                      "musketeer's JSON lists no linen (hex from the "
                                      "guard's)."},
            "ash_haft": {"name": "Ash haft", "base": "#8A7254", "rough": 0.7,
                         "notes": "Build bullet 'ash staff 1.30 m' for the rest; the "
                                  "musketeer's JSON lists no ash (hex from the guard's)."},
        },
        notes=[
            "Musket (1.55 m), rest (1.30 m) and slow match are prop bones on Hand.R / "
            "Hand.L / Hand.L. The musket is held grounded at the order as on the "
            "concept's front view, not shouldered.",
            "Hat is its own bone under Head; the plume is rigid on it (no plume springs).",
            "Baldric is black felt (the concept labels the chargers 'on felt band'); "
            "the JSON lists no black leather for him. The sword scabbard is felt-black too.",
            "Bucket-top boots are buff leather (concept: 'dressed buff').",
            "Not built: the 12 charger spring bones, coat skirt_F/B/L/R, ember sockets, "
            "the smoke ribbon card, the spare match coil, powder smudges.",
        ])


# --------------------------------------------------------------------------------
# Cuirassier (heavy) — a dark armoured column: closed burgonet with a peaked
# visor and comb, big layered pauldrons, long laminated tassets to the knee,
# soft boots below, and two ball-ended pistol butts at the hips.
# --------------------------------------------------------------------------------

def _arc_lame(centre: Vector, u: Vector, v: Vector, radius: float, a0: float, a1: float,
              half_w: float, thick: float, axis: Vector, mat: str, bone: str,
              bones: list[str], steps: int = 7, flare: float = 0.0) -> Part:
    """One armour lame: a curved band round `centre` in the plane of u, v (from
    angle a0 to a1, degrees), `half_w` along `axis` (the limb), `thick` deep. A
    4-sided section makes a ridged, shingled stack. `flare` pushes the ends out."""
    path = []
    for i in range(steps):
        a = math.radians(a0 + (a1 - a0) * i / (steps - 1))
        e = abs(2.0 * i / (steps - 1) - 1.0)
        r = radius * (1.0 + flare * e * e)
        path.append(tuple(centre + (u * math.cos(a) + v * math.sin(a)) * r))
    return Part("tube", (0, 0, 0), (1, 1, 1), mat=mat, bone=bone, segments=4,
                extras={"path": path, "section": (half_w, thick / 2), "up": tuple(axis),
                        "bevel": False, "smooth": True, "bones": bones})


def _burgonet(fig: Human, steel: str, bright: str, dark: str) -> list[Part]:
    """Closed burgonet: skull 0.26 m long x 0.22 m wide down to the jaw, a roped
    comb 0.05 m high, a falling buff over the lower face, a peaked visor with 3
    sight-slits, 3 neck lames behind, dome rivets, the empty plume holder. Helm
    and Visor (a hinge) are their own bones."""
    h = fig.h
    base = fig.lean((0.0, 0.006 * h, 0.858 * h))       # jaw line
    top = h + 0.045
    H = top - base.z
    fig.add_bone("Helm", base, base + Vector((0, 0, H)), "Head")
    rigid = {"rigid": True}
    rx, ry = 0.110, 0.130
    profile = [(0.80, 0.0), (0.88, 0.10), (0.97, 0.30), (1.0, 0.52), (0.93, 0.74),
               (0.72, 0.90), (0.40, 0.98), (0.0, 1.0)]
    parts = [Part("lathe", tuple(base), (rx, ry, H), mat=steel, bone="Helm", segments=16,
                  extras={"profile": profile, "smooth": True, "bevel": False, **rigid})]
    # roped comb over the skull, front to back
    outline_top, outline_bot = [], []
    for k in range(11):
        u = -0.11 + 0.22 * k / 10
        sk = H * math.sqrt(max(0.0, 1.0 - (u / (ry * 0.98)) ** 2)) ** 0.9
        crest = 0.05 * math.sqrt(max(0.0, 1.0 - (u / 0.112) ** 2)) ** 0.7
        outline_top.append((u, sk + crest))
        outline_bot.append((u, sk - 0.015))
    parts.append(Part("prism", tuple(base), (1, 1, 0.010), mat=steel, bone="Helm",
                      rot=(90.0, 0.0, 90.0), extras={
                          "outline": outline_top + list(reversed(outline_bot)), **rigid,
                          "bevel": False}))
    # empty plume holder: a short tube at the back of the comb
    ph = base + Vector((0.0, 0.07, H * 0.93))
    parts.append(Part("cyl", tuple(ph), (0.014, 0.014, 0.07), mat=bright, bone="Helm",
                      segments=6, extras={**rigid, "bevel": False}))
    # visor (hinged at the temples): a peaked shell over the face
    pivot = base + Vector((0.0, -0.01, 0.62 * H))
    fig.add_bone("Visor", pivot, pivot + Vector((0, -0.12, 0)), "Helm")
    face_y = base.y - ry * 0.93
    vis = [(-0.085, -0.05), (0.085, -0.05), (0.09, 0.03), (0.0, 0.075), (-0.09, 0.03)]
    parts.append(Part("prism", (base.x, face_y - 0.012, pivot.z), (1, 1, 0.02),
                      mat=steel, bone="Visor", rot=(90.0, 0.0, 0.0),
                      extras={"outline": vis, **rigid}))
    # the peak: a wedge jutting forward above the slits
    peak = [(0.0, 0.0), (0.055, 0.0), (0.0, 0.03)]
    parts.append(Part("prism", (base.x, face_y - 0.012, pivot.z + 0.045), (1, 1, 0.16),
                      mat=bright, bone="Visor", rot=(90.0, 0.0, 90.0),
                      extras={"outline": [(-a, b) for a, b in peak], **rigid}))
    for k in range(3):   # sight slits
        z = pivot.z + 0.022 - k * 0.018
        parts.append(Part("box", (base.x, face_y - 0.026, z), (0.12, 0.008, 0.008),
                          mat=dark, bone="Visor", extras={**rigid, "bevel": False}))
    # falling buff over mouth and chin, with a grille of breaths
    buff = [(-0.095, -0.13), (0.095, -0.13), (0.10, -0.02), (0.0, 0.01), (-0.10, -0.02)]
    parts.append(Part("prism", (base.x, face_y + 0.002, pivot.z - 0.02), (1, 1, 0.03),
                      mat=steel, bone="Helm", rot=(90.0, 0.0, 0.0),
                      extras={"outline": buff, **rigid}))
    for k in range(3):
        parts.append(Part("box", (base.x + 0.03, face_y - 0.016, pivot.z - 0.085 + k * 0.018),
                          (0.035, 0.006, 0.006), mat=dark, bone="Helm",
                          extras={**rigid, "bevel": False}))
    # three neck lames behind
    for k in range(3):
        c = base + Vector((0.0, 0.01, -0.02 - 0.035 * k))
        parts.append(_arc_lame(c, Vector((1, 0, 0)), Vector((0, 1, 0)), 0.125 + 0.012 * k,
                               -10.0, 190.0, 0.022, 0.008, Vector((0, 0, 1)), steel, "Helm",
                               ["Helm"], steps=7))
    # dome rivets round the skull
    for k in range(10):
        a = 2.0 * math.pi * (k + 0.5) / 10
        at = base + Vector((math.cos(a) * rx * 0.99, math.sin(a) * ry * 0.99, 0.52 * H))
        parts.append(Part("sphere", tuple(at), (0.016, 0.016, 0.016), mat=bright, bone="Helm",
                          segments=5, rings=3, extras={**rigid, "bevel": False}))
    return parts


def _pauldron(fig: Human, side: str, steel: str) -> list[Part]:
    """Asymmetric pauldron of 6 lames, 0.28 m wide, from a dome over the shoulder
    down to the elbow, and a couter with a fan wing."""
    sh, el, wr, axis, out, fwd = _arm_frame(fig, side)
    bones = [f"UpperArm.{side}", f"Shoulder.{side}"]
    big = 1.08 if side == "L" else 1.0            # the left (bridle) side is bigger
    parts = [Part("sphere", tuple(sh + out * 0.025 + Vector((0, 0, 0.015))),
                  (0.27 * big, 0.25 * big, 0.17), mat=steel, bone=f"UpperArm.{side}",
                  segments=14, rings=7, extras={"bevel": False, "smooth": True,
                                                "bones": bones + ["Chest"]})]
    for k in range(6):
        t = 0.10 + 0.13 * k
        c = sh + (el - sh) * t
        r = (0.118 - 0.008 * k) * big
        parts.append(_arc_lame(c + out * 0.012, out, fwd, r, -115.0, 115.0, 0.030, 0.010,
                               axis, steel, f"UpperArm.{side}", bones, steps=7, flare=0.08))
    # couter + fan
    parts.append(Part("sphere", tuple(el + out * 0.01), (0.12, 0.12, 0.12), mat=steel,
                      bone=f"LowerArm.{side}", segments=10, rings=6,
                      extras={"bevel": False, "smooth": True,
                              "bones": [f"LowerArm.{side}", f"UpperArm.{side}"]}))
    parts.append(Part("cyl", tuple(el + out * 0.06), (0.11, 0.11, 0.012), mat=steel,
                      bone=f"LowerArm.{side}", rot=_track(out), segments=10,
                      extras={"rigid": True, "bevel": False}))
    # gauntlet cuff flaring back over the wrist, 0.14 m
    fore = (wr - el).normalized()
    parts.append(Part("cone", tuple(wr - fore * 0.04), (0.12, 0.11, 0.14), mat=steel,
                      bone=f"Hand.{side}", rot=_track(-fore), segments=10, taper=0.65,
                      extras={"bevel": False, "smooth": True,
                              "bones": [f"Hand.{side}", f"LowerArm.{side}"]}))
    return parts


def _tassets(fig: Human, side: str, steel: str, top_z: float) -> list[Part]:
    """Laminated tasset, 12 lames of 4 cm from the waist to the knee, 0.26 m wide
    at the top, ending in a poleyn with a small fan. Bound to Hips and the thigh
    so it follows a stride as a stiff shell."""
    s = figures.SIDES[side]
    hip, knee = fig.joint(f"hip.{side}"), fig.joint(f"knee.{side}")
    axis = (knee - hip).normalized()
    bones = ["Hips", f"UpperLeg.{side}"]
    parts = []
    bottom_z = knee.z + 0.07
    n = 12
    # angles: front (-90) through the outer side; mirrored for the right leg
    a0, a1 = (-172.0, 25.0) if s > 0 else (-8.0, 155.0)
    for k in range(n):
        f = k / (n - 1)
        z = top_z - (top_z - bottom_z) * f
        t = (hip.z - z) / max(1e-6, -axis.z)
        c = hip + axis * t
        c.x += s * 0.03 * (1.0 - f)        # the upper lames sit out over the hip
        r = 0.150 - 0.060 * f
        parts.append(_arc_lame(c, Vector((1, 0, 0)), Vector((0, 1, 0)), r, a0, a1, 0.024,
                               0.012, Vector((0, 0, 1)), steel, f"UpperLeg.{side}", bones,
                               steps=8 if k < 6 else 7))
    # poleyn: knee-cop + fan, on the lower leg side of the knee
    kb = [f"LowerLeg.{side}", f"UpperLeg.{side}"]
    parts.append(Part("sphere", tuple(knee + Vector((0, -0.04, 0.0))), (0.15, 0.12, 0.14),
                      mat=steel, bone=f"LowerLeg.{side}", segments=10, rings=6,
                      extras={"bevel": False, "smooth": True, "bones": kb}))
    parts.append(Part("cyl", tuple(knee + Vector((s * 0.07, -0.02, 0.0))),
                      (0.11, 0.11, 0.012), mat=steel, bone=f"LowerLeg.{side}",
                      rot=(0.0, 90.0, 0.0), segments=10, extras={"bevel": False,
                                                                  "bones": kb}))
    return parts


def _holster_pistol(fig: Human, at: Vector, direction, blued: str, walnut: str,
                    leather: str, steel: str) -> list[Part]:
    """A black leather saddle holster (0.40 m) hanging from the belt, muzzle
    down, and a wheellock pistol in it carried butt-forward: walnut stock with a
    0.07 m ball pommel and steel cap, lockplate and 4 cm wheel showing at the
    mouth. Rigid on Hips (the holster socket)."""
    d = Vector(direction).normalized()
    rigid = {"rigid": True}
    bottom = at + d * 0.40
    parts = [Part("sweep", (0, 0, 0), (1, 1, 1), mat=leather, bone="Hips", segments=8,
                  extras={"path": [tuple(at), tuple(at + d * 0.2), tuple(bottom)],
                          "sections": [(0.048, 0.040), (0.040, 0.034), (0.024, 0.022)],
                          "up": (0.0, 1.0, 0.0), "smooth": True, "bevel": False, **rigid})]
    # butt out of the mouth, angled forward and up
    fwd = Vector((0.0, -1.0, 0.4)).normalized()
    wrist = at - d * 0.02
    butt = wrist + fwd * 0.17
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat=walnut, bone="Hips", segments=6,
                      extras={"path": [tuple(wrist), tuple(wrist.lerp(butt, 0.5)), tuple(butt)],
                              "sections": [(0.022, 0.016), (0.020, 0.014), (0.026, 0.018)],
                              "up": (1.0, 0.0, 0.0), "smooth": True, "bevel": False,
                              **rigid}))
    parts.append(Part("sphere", tuple(butt + fwd * 0.03), (0.07, 0.07, 0.07), mat=walnut,
                      bone="Hips", segments=8, rings=5, extras={**rigid, "bevel": False}))
    parts.append(Part("sphere", tuple(butt + fwd * 0.062), (0.04, 0.04, 0.03), mat=steel,
                      bone="Hips", segments=6, rings=4, rot=_track(fwd),
                      extras={**rigid, "bevel": False}))
    side = Vector((1.0 if at.x > 0 else -1.0, 0.0, 0.0))
    parts.append(Part("cyl", tuple(wrist + side * 0.028 + d * 0.01), (0.04, 0.04, 0.012),
                      mat=blued, bone="Hips", rot=(0.0, 90.0, 0.0), segments=8,
                      extras={**rigid, "bevel": False}))
    return parts


def cuirassier(entry: Entry):
    # A 1.80 m heavy man in 6-10 cm of armour and padding; the burgonet's comb
    # reaches ~1.95 m. Shoulders 0.62 m across the pauldrons.
    fig = Human(height=1.82, bulk=1.15, shoulders=0.56,
                arm_r=ArmPose(spread=20.0, swing=2.0, elbow=20.0),
                arm_l=ArmPose(spread=20.0, swing=2.0, elbow=20.0))
    steel, bright, blued = "blackened_steel", "bright_steel", "blued_steel"
    leather, boot, walnut, wool = "black_leather", "riding_boot_leather", "black_walnut", \
        "murrey_livery_wool"
    pad = 0.035
    # the cuirass: breastplate with a peascod point, back-plate; below the waist
    # the torso is buff breeches hidden by the tassets (painted black leather).
    parts = [fig.torso_part(steel, pad=pad, chest=0.16, segments=24, paint=[
        {"mat": leather, "min": (-1, -1, -1), "max": (1, 1, fig.waist_z - 0.04)}])]
    # medial ridge, rubbed bright
    ridge = [fig.surface(z, -90.0, pad=pad + 0.018) for z in
             (fig.waist_z + 0.02, fig.waist_z + 0.10, fig.chest_z, fig.chest_z + 0.10,
              fig.neck_z - 0.06)]
    parts.append(_strip(ridge, bright, "Chest", 0.012, 0.012, up=(0, -1, 0),
                        bones=["Chest", "Spine"]))
    # rolled bottom edge of the breast- and back-plate
    parts.append(fig.band(fig.waist_z - 0.02, steel, height=0.03, pad=pad + 0.012,
                          bone="Spine"))
    # gorget, two plates, rolled edge bright
    parts.append(_drape(fig, steel, fig.neck_z + 0.07, (0.075, 0.068), fig.neck_z - 0.06,
                        pad=pad + 0.012, thick=0.012))
    parts.append(_drape(fig, bright, fig.neck_z - 0.045, (0.13, 0.10), fig.neck_z - 0.075,
                        pad=pad + 0.03, thick=0.006))

    for side in ("L", "R"):
        s = figures.SIDES[side]
        parts.append(fig.arm_part(side, steel, pad=0.022))          # vambraces
        parts += fig.hand_part(side, steel)                          # fingered gauntlets
        knee = fig.joint(f"knee.{side}")
        parts.append(fig.leg_part(side, boot, pad=0.012, paint=[
            {"mat": leather, "min": (-1, -1, 0.62), "max": (1, 1, 3)}]))
        parts.append(fig.foot_part(side, boot, length=0.30, point=0.0))
        # boot tops turned down, top at 0.60 m
        cuff_at = Vector((knee.x, knee.y + 0.004, 0.0))
        cuff_at.x += (fig.joint(f"ankle.{side}").x - knee.x) * (knee.z - 0.60) / (knee.z - 0.091)
        cuff_at.z = 0.60
        parts.append(Part("lathe", tuple(cuff_at), (1, 1, 1), mat=boot,
                          bone=f"LowerLeg.{side}", segments=12, extras={
                              "profile": [(0.070, -0.14), (0.090, -0.12), (0.108, -0.01),
                                          (0.108, 0.0), (0.095, 0.004), (0.070, -0.02)],
                              "smooth": True, "bevel": False,
                              "bones": [f"LowerLeg.{side}", f"UpperLeg.{side}"]}))
        # spur leathers: a strap over the instep
        ank = fig.joint(f"ankle.{side}")
        parts.append(Part("torus", (ank.x, ank.y + 0.01, 0.07), (0.13, 0.15, 0.05),
                          mat=leather, bone=f"Foot.{side}", rot=(0.0, 0.0, 0.0),
                          segments=10, rings=4, minor=0.12,
                          extras={"rigid": True, "bevel": False}))
        parts += _pauldron(fig, side, steel)
        parts += _tassets(fig, side, steel, fig.waist_z - 0.03)

    parts += fig.head_part("skin", face="skin")
    parts += _burgonet(fig, steel, bright, leather)

    # Sword belt, the murrey field sash tied at the right hip, holsters either
    # side, the Pallasch on a hanger at the left.
    parts.append(fig.band(fig.waist_z - 0.005, wool, height=0.10, pad=pad + 0.028,
                          bone="Hips"))
    knot = fig.surface(fig.waist_z - 0.02, 180.0 + 25.0, pad=pad + 0.05)
    parts.append(Part("sphere", tuple(knot), (0.07, 0.07, 0.08), mat=wool, bone="Hips",
                      segments=8, rings=5, extras={"rigid": True, "bevel": False}))
    for dy, dz in ((-0.03, -0.35), (0.03, -0.33)):
        tail = [knot, knot + Vector((-0.02, dy, -0.15)), knot + Vector((-0.03, dy * 1.5, dz))]
        parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat=wool, bone="Hips", segments=6,
                          extras={"path": [tuple(p) for p in tail],
                                  "sections": [(0.030, 0.010), (0.040, 0.008), (0.045, 0.008)],
                                  "up": (0.0, 1.0, 0.0), "power": 3.0, "smooth": True,
                                  "bevel": False, "bones": ["Hips", "UpperLeg.R"]}))
    parts.append(fig.band(fig.waist_z - 0.075, leather, height=0.04, pad=pad + 0.03,
                          bone="Hips"))
    for side, ang in (("L", -8.0), ("R", 188.0)):
        at = fig.surface(fig.waist_z - 0.08, ang, pad=pad + 0.07)
        s = figures.SIDES[side]
        parts += _holster_pistol(fig, at, (s * 0.12, 0.25, -0.96), blued, walnut, leather,
                                 steel)
    hilt = fig.surface(fig.waist_z - 0.10, 40.0, pad=pad + 0.08)
    parts += _sword(fig, hilt, (0.18, 0.55, -0.82), 0.95, steel, leather, leather,
                    swept=True, chape=steel)

    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(),
        family_overrides={
            "blackened_steel": {"rough": 0.5, "wear_to": "#7E8A94", "wear_amount": 0.25},
            "blued_steel": {"rough": 0.45},
        },
        extra_families={
            **SKIN,
            "bright_steel": {"name": "Blackened steel, rubbed bright", "base": "#7E8A94",
                             "rough": 0.35, "metal": 1.0,
                             "notes": "JSON blackened-steel note: 'every edge rubbed to "
                                      "#7E8A94'; the breast ridge, visor peak, gorget "
                                      "edge and rivets carry it as geometry."},
        },
        notes=[
            "Helm and Visor (hinge) are their own bones under Head; the face inside is "
            "modelled (skin) for the open-visor state.",
            "Tassets are 12 lames per leg bound to Hips + UpperLeg (a stiff shell), not "
            "the JSON's tasset_L/R_01-03 chain; pauldrons follow UpperArm, not twist.",
            "Pistols sit in holsters on Hips; no right-hand pistol prop bone and no sword "
            "prop bone yet (both would be sockets for the draw animations).",
            "Not built: proof-mark dent, rust spots, sash_tail springs, butterfly spur "
            "leathers (a plain strap), mud to 0.10 m.",
        ])

# --------------------------------------------------------------------------------
# Petardier
# --------------------------------------------------------------------------------

def _torso_x(fig: Human, z: float, x0: float, pad: float, back: bool = False) -> Vector:
    """The torso-surface point (plus pad) at height z whose x is nearest x0, on
    the front (or the back)."""
    best = None
    for k in range(91):
        a = (-90.0 if not back else 90.0) + (90.0 if x0 >= 0 else -90.0) * (k / 90.0) \
            * (1.0 if not back else -1.0)
        p = fig.surface(z, a, pad=pad)
        if best is None or abs(p.x - x0) < abs(best.x - x0):
            best = p
    return best


def _grenado(at: Vector, iron: str, plug: str, fuse: str, bone: str,
             ember: str | None = None, cup: str | None = None, prop: bool = False) -> list[Part]:
    """A 0.09 m cast-iron grenado with a wooden fuse plug and a 0.06 m fuse; a
    leather cup under it when it hangs on the belt, an ember on the fuse when lit."""
    ex = {"prop": True} if prop else {"rigid": True}
    parts = [Part("sphere", tuple(at), (0.09, 0.09, 0.09), mat=iron, bone=bone,
                  segments=10, rings=6, extras={**ex, "bevel": False, "smooth": True}),
             Part("cyl", tuple(at + Vector((0, 0, 0.048))), (0.022, 0.022, 0.018), mat=plug,
                  bone=bone, segments=6, extras={**ex, "bevel": False})]
    f0 = at + Vector((0, 0, 0.056))
    path = [f0, f0 + Vector((0.006, -0.004, 0.03)), f0 + Vector((0.018, -0.012, 0.055))]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=fuse, bone=bone, segments=4,
                      extras={"path": [tuple(p) for p in path], "section": (0.0035, 0.0035),
                              "smooth": True, "bevel": False, **ex}))
    if ember:
        parts.append(Part("sphere", tuple(path[-1]), (0.018, 0.018, 0.018), mat=ember,
                          bone=bone, segments=6, rings=4, extras={**ex, "bevel": False}))
    if cup:
        parts.append(Part("cyl", tuple(at - Vector((0, 0, 0.02))), (0.100, 0.100, 0.055),
                          mat=cup, bone=bone, segments=10, taper=0.8,
                          rot=(180.0, 0.0, 0.0), extras={**ex, "bevel": False,
                                                          "smooth": True}))
    return parts


def _madrier(fig: Human, pad: float, oak: str, iron: str, bronze: str, patina: str,
             fuse: str, soot: str) -> list[Part]:
    """The madrier (0.60 x 0.60 x 0.08 m oak plank, top at 1.65 m) on the back,
    tilted with the stoop, two iron bands and a hanging hook; the bronze bell
    (0.30 m tall, 0.26 m mouth) bolted mouth-down to its outer face by four iron
    straps, so it points behind him. All rigid on the Madrier bone (Chest), the
    backpack socket the plant_petard animation detaches."""
    s = math.radians(fig.stoop)
    n = Vector((0.0, math.cos(s), math.sin(s)))          # out of the back
    up = Vector((0.0, -math.sin(s), math.cos(s)))        # up the back
    top_z, side, thick = 1.65, 0.60, 0.08
    cz = top_z - up.z * side / 2
    # push the plank's inner face just clear of the jerkin between its bottom and
    # the shoulder line
    d = max(fig.surface(z, a, pad=pad + 0.012).dot(n)
            for z in [cz - 0.28 + 0.04 * i for i in range(12)]
            if z < fig.shoulder_z + 0.02 for a in (60.0, 90.0, 120.0))
    d_c = d + 0.006 + thick / 2
    c = Vector((0.0, (d_c - cz * n.z) / n.y, cz))
    # The bone sits in the air just past the bell's crown, pointing out along its
    # axis. Heat weighting needs every mesh island to see some bone (a fully
    # hidden island makes the solve singular and it fails for the whole mesh);
    # buried in the plank, this bone was hidden from the bell and the straps.
    crown_out = c + n * (thick / 2 + 0.40)
    fig.add_bone("Madrier", crown_out, crown_out + n * 0.25, "Chest")
    rig = {"rigid": True}
    rot = (fig.stoop, 0.0, 0.0)      # local Z -> up the back, local Y -> n
    parts = [Part("box", tuple(c), (side, thick, side), mat=oak, bone="Madrier", rot=rot,
                  extras={**rig, "paint": [{"mat": soot, "min": (-1, -1, -1),
                                            "max": (1, 1, -side / 2 + 0.035)}]})]
    for f in (-0.34, 0.34):         # two iron bands wrapped round the plank
        parts.append(Part("box", tuple(c + up * (f * side)), (side + 0.012, thick + 0.012, 0.04),
                          mat=iron, bone="Madrier", rot=rot, extras=dict(rig)))
    # hanging hook at the top edge
    t = c + up * (side / 2)
    hook = [t - n * 0.005, t + up * 0.05 - n * 0.005, t + up * 0.075 - n * 0.03,
            t + up * 0.06 - n * 0.055, t + up * 0.035 - n * 0.05]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=iron, bone="Madrier", segments=5,
                      extras={"path": spline([tuple(p) for p in hook], 2),
                              "section": (0.008, 0.008), "smooth": True, "bevel": False,
                              **rig}))
    # the bell: lathe along n from the plank's outer face
    base = c + n * (thick / 2 - 0.002)
    prof = [(0.130, 0.0), (0.138, 0.012), (0.132, 0.026), (0.122, 0.030),
            (0.116, 0.090), (0.124, 0.100), (0.124, 0.112), (0.112, 0.120),
            (0.100, 0.190), (0.106, 0.200), (0.104, 0.212), (0.092, 0.218),
            (0.074, 0.262), (0.046, 0.290), (0.0, 0.300)]
    parts.append(Part("lathe", tuple(base), (1, 1, 1), mat=bronze, bone="Madrier",
                      rot=_track(n), segments=16, extras={
                          "profile": prof, "smooth": True, "bevel": False, **rig,
                          "paint": [{"mat": patina, "min": (-1, -1, 0.089), "max": (1, 1, 0.121)},
                                    {"mat": patina, "min": (-1, -1, 0.189), "max": (1, 1, 0.219)}]}))
    # touch-hole boss and fuse stub at the crown
    crown = base + n * 0.30
    parts.append(Part("cyl", tuple(crown + n * 0.006), (0.036, 0.036, 0.02), mat=bronze,
                      bone="Madrier", rot=_track(n), segments=8, extras={**rig, "bevel": False}))
    fz = [crown + n * 0.015, crown + n * 0.05 + up * 0.02, crown + n * 0.07 + up * 0.06]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=fuse, bone="Madrier", segments=4,
                      extras={"path": [tuple(p) for p in fz], "section": (0.005, 0.005),
                              "smooth": True, "bevel": False, **rig}))
    # four iron straps from the bell's shoulder out to bolts on the plank
    x = Vector((1.0, 0.0, 0.0))
    for k in range(4):
        a = math.radians(45.0 + 90.0 * k)
        r = x * math.cos(a) + up * math.sin(a)
        p0 = base + n * 0.16 + r * 0.113
        p1 = base + n * 0.035 + r * 0.150
        p2 = base + n * 0.004 + r * 0.235
        parts.append(_strip([p0, p1, p2], iron, "Madrier", 0.03, 0.008,
                            up=tuple((r + n).normalized()), rigid=True))
    return parts


def petardier(entry: Entry):
    # Stooped 8 deg under the plank; skull crown 1.74 m, the flat cap adds ~0.015.
    fig = Human(height=1.74, bulk=1.07, shoulders=0.47, stoop=8.0,
                arm_l=ArmPose(spread=11.0, swing=10.0, elbow=70.0),   # lit grenado
                arm_r=ArmPose(spread=11.0, swing=4.0, elbow=45.0))    # linstock
    canvas, leather, iron = "padded_canvas", "scorched_leather", "cast_iron"
    wool, oak, bronze, ember = "murrey_livery_wool", "oak_plank", "petard_bronze", "fuse_ember"
    skin, soot, patina = "sooted_skin", "soot", "bronze_patina"
    pad = 0.022          # the thick padded jerkin
    hem_soot = {"mat": soot, "min": (-1, -1, -1), "max": (1, 1, fig.hip_z - 0.01)}
    parts = [fig.torso_part(canvas, pad=pad, quilt=0.06, segments=24, collar=0.012,
                            paint=[hem_soot])]
    for side in ("L", "R"):
        s = figures.SIDES[side]
        el = fig.joint(f"elbow.{side}")
        sh, _el, _wr, axis, _out, _fwd = _arm_frame(fig, side)
        # murrey shirt sleeve rolled to the elbow, sooted forearm below
        parts.append(fig.arm_part(side, wool, pad=0.006, paint=[
            {"mat": skin, "min": (-1, -1, -1), "max": (1, 1, el.z - 0.035)}]
            if el.z - 0.035 > fig.joint(f"wrist.{side}").z else [
            {"mat": skin, "min": (-1, -1, -1), "max": (1, 1, 3)}]))
        roll = el + (sh - el).normalized() * 0.02
        parts.append(Part("torus", tuple(roll), (0.105, 0.105, 0.09), mat=wool,
                          bone=f"UpperArm.{side}", rot=_track(axis), segments=10, rings=4,
                          minor=0.25, extras={"bevel": False, "smooth": True,
                                              "bones": [f"UpperArm.{side}",
                                                        f"LowerArm.{side}"]}))
        # the jerkin's padded armhole roll (it is sleeveless)
        wing = sh + (el - sh) * 0.05
        parts.append(Part("torus", tuple(wing), (0.135, 0.12, 0.11), mat=canvas,
                          bone=f"UpperArm.{side}", rot=_track(axis), segments=12, rings=5,
                          minor=0.26, extras={"bevel": False, "smooth": True,
                                              "bones": [f"UpperArm.{side}",
                                                        f"Shoulder.{side}", "Chest"]}))
        parts += fig.hand_part(side, skin)
        # canvas breeches and canvas gaiters, leather garters at the knee and ankle
        parts.append(fig.leg_part(side, canvas, pad=0.010))
        parts.append(fig.foot_part(side, leather, length=0.29, point=0.0))
        knee, ank = fig.joint(f"knee.{side}"), fig.joint(f"ankle.{side}")
        for at, r in ((knee + (ank - knee) * 0.10, 0.066), (ank + (knee - ank) * 0.12, 0.052)):
            parts.append(Part("cyl", tuple(at), (2 * r, 2 * r, 0.03), mat=leather,
                              bone=f"LowerLeg.{side}", rot=_track(knee - ank), segments=10,
                              extras={"bevel": False, "smooth": True,
                                      "bones": [f"LowerLeg.{side}", f"UpperLeg.{side}"]}))

    # Head: sooted face, singed beard and moustache, the round sapper's cap.
    parts += fig.head_part(skin, face=skin, features=soot)
    h = fig.h
    beard = fig.lean((0.0, -0.036 * h, 0.878 * h))
    parts.append(Part("sphere", tuple(beard), (0.088, 0.075, 0.07), mat=soot, bone="Head",
                      segments=10, rings=5, extras={"rigid": True, "bevel": False,
                                                    "smooth": True}))
    lip = fig.lean((0.0, -0.058 * h, 0.899 * h))
    parts.append(Part("box", tuple(lip), (0.065, 0.014, 0.012), mat=soot, bone="Head",
                      extras={"rigid": True, "bevel": False}))
    parts += _sapper_cap(fig, leather)

    # Shoulder straps from the plank over the shoulders, down the front to the belt.
    for side in ("L", "R"):
        sx = figures.SIDES[side] * 0.105
        # surface() has no chest bulge (torso_ring's `chest`), so the front points
        # sit further out than the jerkin pad alone.
        pts = [_torso_x(fig, fig.shoulder_z - 0.02, sx, pad + 0.03, back=True),
               fig.lean((sx, 0.012 * h, 0.826 * h + 0.022)),
               _torso_x(fig, fig.shoulder_z - 0.01, sx, pad + 0.016),
               _torso_x(fig, fig.chest_z, sx, pad + 0.030),
               _torso_x(fig, fig.waist_z, sx, pad + 0.012),
               _torso_x(fig, fig.belt_z + 0.03, sx, pad + 0.010)]
        parts.append(_strip(spline([tuple(p) for p in pts], 3), leather, "Chest", 0.05,
                            0.008, up=(0.0, 1.0, 0.0),
                            bones=["Chest", "Spine", f"Shoulder.{side}"]))

    parts += _madrier(fig, pad, oak, iron, bronze, patina, leather, soot)

    # Waist-belt with four grenados in leather cups across the front.
    parts.append(fig.band(fig.belt_z, leather, height=0.05, pad=0.012, torso_pad=pad))

    # Heavy leather apron from the belt to the knee, a curved slab.
    top, bot = fig.belt_z - 0.01, fig.knee_z + 0.01
    rings = []
    for i in range(7):
        z = top + (bot - top) * i / 6
        yf = fig.surface(z, -90.0, pad=pad + 0.012).y if z > fig.crotch_z else \
            fig.surface(fig.crotch_z + 0.01, -90.0, pad=pad + 0.012).y - 0.012
        hw = 0.235 + 0.02 * i / 6
        outer, inner = [], []
        for k in range(9):
            u = -1.0 + 2.0 * k / 8
            x = u * hw
            y = yf + 0.07 * u * u - (0.004 * i)
            outer.append((x, y - 0.012, z))
            inner.append((x, y, z))
        rings.append(outer + inner[::-1])
    parts.append(Part("loft", (0, 0, 0), (1, 1, 1), mat=leather, bone="Hips", extras={
        "rings": rings, "smooth": True, "bevel": False,
        "paint": [{"mat": soot, "min": (-0.12, -1, bot + 0.05), "max": (0.05, 1, bot + 0.22)}],
        "skirt": {"top": fig.crotch_z + 0.08, "bottom": bot, "strength": 0.85, "split": 0.08},
        "bones": ["Hips", "UpperLeg.L", "UpperLeg.R"]}))

    for k, ang in enumerate((-58.0, -76.0, -104.0, -122.0)):
        z = fig.belt_z - 0.035
        at = fig.surface(z, ang, pad=pad + 0.075)
        # socket bone from above the fuse, through the grenado, back toward the
        # belt: the fuse, the plug and the belt can all see it (see _madrier)
        inward = (fig.surface(z, ang, pad=0.0) - at).normalized()
        name = fig.add_bone(f"Grenado.{k + 1}", at + Vector((0.0, 0.0, 0.11)),
                            at + inward * 0.07, "Hips")
        parts += _grenado(at, iron, oak, leather, name, cup=leather)

    # Right hip: sapper's mallet and a coil of fuse cord; left hip: water-flask.
    # Tools.R and Flask.L: hip sockets (the JSON's tools on the right hip, flask on
    # the left), sticking out past the kit so heat weighting can see them.
    t0 = fig.surface(fig.belt_z - 0.06, 135.0, pad=0.0)
    fig.add_bone("Tools.R", t0, fig.surface(fig.belt_z - 0.12, 135.0, pad=pad + 0.20), "Hips")
    f0 = fig.surface(fig.belt_z - 0.08, 20.0, pad=0.0)
    fig.add_bone("Flask.L", f0, fig.surface(fig.belt_z - 0.10, 20.0, pad=pad + 0.18), "Hips")
    m0 = fig.surface(fig.belt_z - 0.02, 150.0, pad=pad + 0.03)
    m1 = m0 + Vector((-0.03, 0.04, -0.24))
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=oak, bone="Tools.R", segments=6,
                      extras={"path": [tuple(m0), tuple(m1)], "section": (0.014, 0.014),
                              "smooth": True, "bevel": False, "rigid": True}))
    parts.append(Part("cyl", tuple(m1 + (m1 - m0).normalized() * 0.03), (0.07, 0.07, 0.13),
                      mat=oak, bone="Tools.R", rot=(0.0, 90.0, 20.0), segments=8,
                      extras={"rigid": True, "bevel": False}))
    coil = fig.surface(fig.belt_z - 0.10, 118.0, pad=pad + 0.03)
    parts.append(Part("torus", tuple(coil), (0.14, 0.14, 0.04), mat=canvas, bone="Tools.R",
                      rot=(90.0, 0.0, -30.0), segments=12, rings=5, minor=0.22,
                      extras={"rigid": True, "bevel": False, "smooth": True}))
    parts.append(Part("torus", tuple(coil), (0.10, 0.10, 0.03), mat=canvas, bone="Tools.R",
                      rot=(90.0, 0.0, -30.0), segments=10, rings=4, minor=0.22,
                      extras={"rigid": True, "bevel": False, "smooth": True}))
    fl = fig.surface(fig.belt_z - 0.11, 20.0, pad=pad + 0.05)
    parts.append(Part("sweep", (0, 0, 0), (1, 1, 1), mat=leather, bone="Flask.L", segments=8,
                      extras={"path": [tuple(fl + Vector((0, 0, 0.10))),
                                       tuple(fl + Vector((0, 0, 0.07))), tuple(fl),
                                       tuple(fl - Vector((0, 0, 0.09))),
                                       tuple(fl - Vector((0, 0, 0.10)))],
                              "sections": [(0.012, 0.012), (0.03, 0.03), (0.06, 0.035),
                                           (0.055, 0.032), (0.0, 0.0)],
                              "up": (1.0, 0.0, 0.0), "power": 2.4, "smooth": True,
                              "bevel": False, "rigid": True}))

    # Left hand: the one lit grenado. Right hand: the linstock, slow match coiled
    # round it, its tip glowing.
    gl = fig.grip("L")
    fore_l = (fig.joint("wrist.L") - fig.joint("elbow.L")).normalized()
    g_at = gl + fore_l * 0.035 + Vector((0, 0, 0.045))
    fig.prop_bone("Grenado.5", "L", head=gl, tail=g_at + Vector((0, 0, 0.11)))
    parts += _grenado(g_at, iron, oak, leather, "Grenado.5", ember=ember, prop=True)

    gr = fig.grip("R")
    d = Vector((0.0, -0.18, 1.0)).normalized()
    lo, hi = gr - d * 0.16, gr + d * 0.44
    fig.prop_bone("Linstock", "R", head=gr, tail=hi)
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat="ash_haft", bone="Linstock",
                      segments=6, extras={"path": [tuple(lo), tuple(hi)],
                                          "section": (0.013, 0.013), "smooth": True,
                                          "bevel": False, "prop": True}))
    x = Vector((1.0, 0.0, 0.0))
    y = d.cross(x).normalized()
    helix = []
    for i in range(22):
        t = 0.10 + 0.30 * i / 21
        a = i * 0.9
        helix.append(tuple(gr + d * t + (x * math.cos(a) + y * math.sin(a)) * 0.019))
    tip = hi + y * 0.02 + d * 0.02
    helix += [tuple(hi - d * 0.02 + y * 0.018), tuple(tip)]
    parts.append(Part("tube", (0, 0, 0), (1, 1, 1), mat=canvas, bone="Linstock", segments=4,
                      extras={"path": helix, "section": (0.006, 0.006), "smooth": True,
                              "bevel": False, "prop": True}))
    parts.append(Part("sphere", tuple(tip), (0.024, 0.024, 0.024), mat=ember,
                      bone="Linstock", segments=6, rings=4,
                      extras={"prop": True, "bevel": False}))

    return blueprint(
        entry, parts, bevel=0.003, **fig.rig(),
        family_overrides={
            # 9x shipping emission: store the madder ember dimmer (see musketeer).
            "fuse_ember": {"emit": "#5A2410", "rough": 0.8},
            # patina patches in the casting, per the JSON bronze note
            "petard_bronze": {"wear_to": "#4A3A26", "wear_amount": 0.3},
            "scorched_leather": {"wear_to": "#1E1C1A", "wear_amount": 0.3},
        },
        extra_families={
            "sooted_skin": {"name": "Sooted skin", "base": "#8E6A52", "rough": 0.65,
                            "notes": "Not in the JSON materials; the concept sheet's "
                                     "'sooted skin' swatch (build: soot-blackened face "
                                     "and forearms)."},
            "soot": {"name": "Soot", "base": "#3E382E", "rough": 0.95,
                     "notes": "JSON padded-canvas note 'soot to #3E382E at the hem'; also "
                              "the singed beard, the plank's scorched edge and the "
                              "powder spill on the apron."},
            "bronze_patina": {"name": "Bronze patina", "base": "#4A3A26", "rough": 0.6,
                              "metal": 1.0, "notes": "JSON petard-bronze note: 'dark brown "
                              "patina #4A3A26 in the mouldings'."},
            "ash_haft": {"name": "Ash haft", "base": "#8A7254", "rough": 0.7,
                         "notes": "Build bullet 'short linstock 0.60 m (ash)'; the "
                                  "petardier's JSON lists no ash (hex from the guard's)."},
        },
        notes=[
            "Madrier + petard are rigid on a Madrier bone under Chest (backpack socket); "
            "Grenado.1-4 are belt-socket bones on Hips, Grenado.5 (the lit one) and the "
            "Linstock are prop bones on Hand.L / Hand.R.",
            "The linstock is modelled in the right hand (JSON: 'belt / right hand'); tucked "
            "in the belt it would clash with the plank.",
            "Apron is one slab on Hips with the skirt rule (thighs), not apron_01-02 bones; "
            "the straps are weighted to Chest/Shoulder, not strap_L/R bones; the cap is "
            "rigid on a Cap bone.",
            "Not built: burn-fleck decals, powder-spill decal (a soot patch instead), the "
            "gaiter buttons, the water-flask stopper, the fuse-cord ends.",
        ])


def _sapper_cap(fig: Human, leather: str) -> list[Part]:
    """Soft round leather cap: a 0.04 m turned-up band over the brow and a
    low hemispherical crown, ear flaps tied up at the sides. Rigid on Cap."""
    h = fig.h
    base = fig.lean((0.0, 0.0, 0.948 * h))
    # The Cap bone lies across, just above the dome, from ear flap to ear flap:
    # heat weighting needs the flaps and the dome to see a bone (see _madrier).
    fig.add_bone("Cap", fig.lean((0.075, 0.004 * h, h + 0.022)),
                 fig.lean((-0.075, 0.004 * h, h + 0.022)), "Head")
    n = 16
    # (z, half-width, half-depth, y shift) in metres, from the skull + padding
    rows = [(0.946 * h, 0.046 * h + 0.008, 0.058 * h + 0.008, 0.0),
            (0.944 * h, 0.046 * h + 0.020, 0.058 * h + 0.020, 0.0),
            (0.946 * h + 0.040, 0.045 * h + 0.022, 0.057 * h + 0.022, 0.001),
            (0.946 * h + 0.044, 0.045 * h + 0.010, 0.057 * h + 0.010, 0.001),
            (0.978 * h, 0.040 * h + 0.012, 0.051 * h + 0.012, 0.002),
            (0.992 * h, 0.028 * h + 0.012, 0.036 * h + 0.012, 0.004),
            (1.000 * h + 0.012, 0.0, 0.0, 0.004)]
    rings = []
    for z, hw, hd, dy in rows:
        c = fig.lean((0.0, dy * h, z))
        if hw == 0.0:
            rings.append([tuple(c)])
            continue
        rings.append([(c.x + math.cos(2 * math.pi * j / n) * hw,
                       c.y + math.sin(2 * math.pi * j / n) * hd, c.z) for j in range(n)])
    parts = [Part("loft", (0, 0, 0), (1, 1, 1), mat=leather, bone="Cap",
                  extras={"rings": rings, "smooth": True, "bevel": False, "rigid": True})]
    # Ear flaps turned up against the sides of the crown, tied at the top.
    for s in (1.0, -1.0):
        path = [fig.lean((s * (0.046 * h + 0.026), 0.004 * h, 0.946 * h + 0.030)),
                fig.lean((s * (0.044 * h + 0.026), 0.004 * h, 0.970 * h)),
                fig.lean((s * (0.036 * h + 0.026), 0.004 * h, 0.990 * h))]
        parts.append(_strip(spline([tuple(p) for p in path], 2), leather, "Cap", 0.055,
                            0.010, up=(s, 0.0, 0.3), rigid=True))
        parts.append(Part("sphere", tuple(path[-1]), (0.022, 0.022, 0.022), mat=leather,
                          bone="Cap", segments=6, rings=4,
                          extras={"rigid": True, "bevel": False}))
    return parts


BLUEPRINTS = {
    "partisan-guard": partisan_guard,
    "musketeer": musketeer,
    "cuirassier": cuirassier,
    "petardier": petardier,
}
