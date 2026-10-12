#!/usr/bin/env python3
"""AnimForge for the Ophan of Hours (#412, under #386): build and review its five clips.

    python3.11 Tools/ArtForge/anim_ophan.py build     # key the Ophan's own rig, export
                                                      # Animations/TheOphanOfHours_Generic.fbx,
                                                      # re-import to verify, write
                                                      # ophan_anim_manifest.json
    python3.11 Tools/ArtForge/anim_ophan.py review [--only ID ...] [--no-mp4]
                                                      # docs/art/anim/fear/<clip>.png (+ .mp4)

The Ophan is a Generic rig with no body (wheels, one eye, six wings), so nothing here goes
through the humanoid solver or a retarget: each clip is a function of time that gives every
bone a turn about its head, written in the armature's world axes relative to its parent, as
poses.py does for the humans (+X tips forward, +Z turns toward +X, the model faces -Y). Root
never moves; the agent moves the creature. Core may bob and, in ophan_arrive, scale.

Clip list, lengths, loop flags and events: anim_spec.json (layer "fear", enemy "ophan").
"""

from __future__ import annotations

import argparse
import json
import math
import os
import shutil
import sys
import tempfile
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import anim_forge  # noqa: E402  (imports bpy before mathutils)
import bpy  # noqa: E402
from anim_forge import FPS, OUT_DIR, REVIEW_DIR, bake, forge  # noqa: E402
from mathutils import Euler, Quaternion, Vector  # noqa: E402
from anim import _rel  # noqa: E402

FBX = "TheOphanOfHours_Generic.fbx"
MANIFEST = "ophan_anim_manifest.json"
BLEND = os.path.join(anim_forge.REPO_ROOT, "Assets", "Models", "ArtBible", "Enemies", "Fear",
                     "TheOphanOfHours", "TheOphanOfHours.blend")
CONCEPT = os.path.join(anim_forge.REPO_ROOT, "docs", "art", "concept", "fear", "ophan-v2-1-the-wheels.png")
FEAR_REVIEW = os.path.join(REVIEW_DIR, "fear")
TAU = 2.0 * math.pi

# The wheels' spin axes, from the blueprint (enemies_fear.ophan): the great wheel lies in XZ,
# the inner wheel in YZ, the tilted wheel is XY turned by Euler (18, 0, -18) degrees.
GREAT_AXIS = Vector((0.0, 1.0, 0.0))
INNER_AXIS = Vector((1.0, 0.0, 0.0))
TILT_AXIS = Vector((0.0, 0.0, 1.0))
TILT_AXIS.rotate(Euler((math.radians(18.0), 0.0, math.radians(-18.0))))
HOVER_DEG_S = 60.0    # the great wheel's turn while hovering: one turn in the 6 s loop


# ---------------------------------------------------------------- pose helpers

def rot(rx: float = 0.0, ry: float = 0.0, rz: float = 0.0) -> Quaternion:
    """World-axis turn in degrees, applied X, then Y, then Z (as poses.py)."""
    qx = Quaternion((1, 0, 0), math.radians(rx))
    qy = Quaternion((0, 1, 0), math.radians(ry))
    qz = Quaternion((0, 0, 1), math.radians(rz))
    return qz @ qy @ qx


def spin(axis: Vector, deg: float) -> Quaternion:
    return Quaternion(axis, math.radians(deg))


class Pose(dict):
    """{bone: Quaternion} turns, plus Core's offset (metres) and uniform scale."""

    def __init__(self):
        super().__init__()
        self.core_offset = Vector((0.0, 0.0, 0.0))
        self.core_scale = 1.0

    def wing(self, name: str, root=(0.0, 0.0, 0.0), tip=(0.0, 0.0, 0.0)) -> None:
        """Set both sides of a wing; the values are the left wing's, mirrored for the right
        (X unchanged, Y and Z negated)."""
        self[f"{name}.L"] = rot(*root)
        self[f"{name}.L.Tip"] = rot(*tip)
        self[f"{name}.R"] = rot(root[0], -root[1], -root[2])
        self[f"{name}.R.Tip"] = rot(tip[0], -tip[1], -tip[2])

    def wheels(self, great: float, inner: float, tilt: float) -> None:
        self["GreatWheel"] = spin(GREAT_AXIS, great)
        self["InnerWheel"] = spin(INNER_AXIS, inner)
        self["TiltWheel"] = spin(TILT_AXIS, tilt)


def lerp(a: float, b: float, u: float) -> float:
    return a + (b - a) * u


def smooth(u: float) -> float:
    u = min(1.0, max(0.0, u))
    return u * u * (3.0 - 2.0 * u)


def window(t: float, t0: float, t1: float) -> float:
    """0 before t0, 1 after t1, smooth between."""
    return smooth((t - t0) / max(1e-6, t1 - t0))


def overshoot(u: float, amount: float = 0.12) -> float:
    """0 -> 1 passing the target by `amount` and settling."""
    u = min(1.0, max(0.0, u))
    c1 = 17.0 * amount          # easeOutBack: c1 1.70158 passes the target by about 10 %
    return 1.0 + (c1 + 1.0) * (u - 1.0) ** 3 + c1 * (u - 1.0) ** 2


# ---------------------------------------------------------------- the five clips

def hover_turn(t: float, length: float) -> Pose:
    """The wheels turn (one turn each per loop, at different moment-to-moment rates), the
    wings breathe every 3 s, the eye drifts, the whole floats up and down 12 cm."""
    u = t / length
    p = Pose()
    p.wheels(-360.0 * u, 360.0 * u + 24.0 * math.sin(TAU * u), -360.0 * u - 18.0 * math.sin(2 * TAU * u))
    p.core_offset = Vector((0.0, 0.0, 0.12 * math.sin(2 * TAU * u)))
    p["Core"] = rot(0.0, 2.0 * math.sin(TAU * u), 0.0)
    p["Eye"] = rot(4.0 * math.sin(2 * TAU * u + 0.5), 0.0, 10.0 * math.sin(TAU * u) + 4.0 * math.sin(3 * TAU * u))
    b = math.sin(2 * TAU * u)
    lag = math.sin(2 * TAU * u - 0.6)
    p.wing("WingUpper", (0.0, 6.0 * b, 4.0 * b), (0.0, 5.0 * lag, 0.0))
    p.wing("WingMid", (0.0, 0.0, 3.0 * math.sin(2 * TAU * u + 1.0)), (0.0, 0.0, 2.0 * lag))
    p.wing("WingLower", (4.0 * math.sin(2 * TAU * u + 2.0), 0.0, 0.0), (3.0 * lag, 0.0, 0.0))
    return p


def advance(t: float, length: float) -> Pose:
    """The drift through walls: leaning 8 degrees into the travel, wheels faster (one turn in
    4 s), the upper wings beating slow (2 s a beat), the body rising on each downstroke."""
    u = t / length
    p = Pose()
    p.wheels(-360.0 * u, 360.0 * u, -360.0 * u)
    beat = math.sin(2 * TAU * u)
    lag = math.sin(2 * TAU * u - 0.9)
    p.core_offset = Vector((0.0, 0.0, 0.20 * math.sin(2 * TAU * u - 0.5)))
    p["Core"] = rot(8.0, 0.0, 0.0)
    p["Eye"] = rot(-4.0, 0.0, 6.0 * math.sin(TAU * u))
    p.wing("WingUpper", (0.0, 18.0 * beat, 6.0 * math.sin(2 * TAU * u - math.pi / 2)), (0.0, 14.0 * lag, 0.0))
    p.wing("WingMid", (0.0, 0.0, 5.0 * beat), (0.0, 0.0, 4.0 * lag))
    p.wing("WingLower", (8.0 + 5.0 * beat, 0.0, 0.0), (6.0 + 4.0 * lag, 0.0, 0.0))
    return p


GAZE_YAW, GAZE_PITCH = 25.0, 18.0   # a player 10 m off, to its left, on the floor


def fix_gaze(t: float, length: float) -> Pose:
    """The eye snaps to a player in 0.1 s; every wheel stops dead and holds for a beat; the
    upper wings flare; then the wheels start again. The engine aims the eye at the real player
    (a LookAt on Eye); this clip gives the snap and the stillness."""
    p = Pose()
    v = -HOVER_DEG_S
    stop, resume = 0.12, 1.05

    def wheel(rate: float) -> float:
        if t < stop:
            return rate * (t - t * t / (2 * stop))
        if t < resume:
            return rate * stop / 2.0
        s = t - resume
        return rate * (stop / 2.0 + s * s / (2.0 * (length - resume)))
    p.wheels(wheel(v), wheel(-v), wheel(v))
    snap = overshoot(t / 0.10)
    tremor = 0.6 * math.sin(TAU * 11.0 * t) * window(t, 0.1, 0.2) * (1.0 - window(t, resume, length))
    p["Eye"] = rot(GAZE_PITCH * snap + tremor, 0.0, GAZE_YAW * snap)
    flare = window(t, 0.0, 0.12) * (1.0 - window(t, resume, length))
    p.core_offset = Vector((0.0, 0.10 * flare, 0.05 * flare))
    p["Core"] = rot(-3.0 * flare + 0.4 * tremor, 0.0, 0.0)
    p.wing("WingUpper", (0.0, -12.0 * flare, 6.0 * flare), (0.0, -8.0 * flare, 0.0))
    p.wing("WingMid", (0.0, 0.0, -6.0 * flare), (0.0, 0.0, -4.0 * flare))
    p.wing("WingLower", (-4.0 * flare, 0.0, 0.0), (-3.0 * flare, 0.0, 0.0))
    return p


TAKE_HIT_S = 0.9


def take(t: float, length: float) -> Pose:
    """It draws its lower wings back (0-0.6 s), sweeps them forward and in round what it came
    for while it stoops (0.6-0.9 s, AttackHit at 0.9 s: the take, or the kill), holds them
    cupped, and lets go (1.3-2.0 s)."""
    p = Pose()
    v = -HOVER_DEG_S
    p.wheels(v * t, -v * t, v * t)
    draw = window(t, 0.0, 0.6) * (1.0 - window(t, 0.6, TAKE_HIT_S))
    strike = window(t, 0.6, TAKE_HIT_S) * (1.0 - window(t, 1.3, length))
    p.core_offset = Vector((0.0, 0.15 * draw - 0.40 * strike, 0.20 * draw - 0.45 * strike))
    p["Core"] = rot(-5.0 * draw + 18.0 * strike, 0.0, 0.0)
    p["Eye"] = rot(15.0 * strike, 0.0, 0.0)
    p.wing("WingLower", (20.0 * draw - 75.0 * strike, 25.0 * strike, 0.0), (10.0 * draw - 30.0 * strike, 10.0 * strike, 0.0))
    p.wing("WingMid", (0.0, -10.0 * draw, 15.0 * draw - 30.0 * strike), (0.0, 0.0, -15.0 * strike))
    p.wing("WingUpper", (0.0, -10.0 * draw + 8.0 * strike, -12.0 * strike), (0.0, -6.0 * draw, 0.0))
    return p


def arrive(t: float, length: float) -> Pose:
    """Unfolds out of a point of light at heart height: Core grows from 2 % to full (by 2.6 s,
    passing it a little and settling by 3.4 s) while it turns a quarter round; the wheels spin
    two turns, fast and slowing to the hover's pace; the wings open from folded, the upper pair
    last; the eye comes down from looking up. Ends where hover_turn starts."""
    u = t / length
    p = Pose()
    f = 3.0 * u - (10.0 / 3.0) * u * u + (4.0 / 3.0) * u ** 3   # 0 -> 1, fast start, ends at hover pace
    p.wheels(-720.0 * f, 720.0 * f, -720.0 * f)
    grow = window(t, 0.0, 2.6)
    p.core_scale = max(0.02, lerp(0.02, 1.0, grow) + 0.04 * math.sin(math.pi * window(t, 2.3, 3.4)))
    p["Core"] = rot(0.0, 0.0, 90.0 * (1.0 - overshoot(t / 3.0, 0.05)))
    p["Eye"] = rot(-40.0 * (1.0 - overshoot((t - 1.2) / 2.0, 0.08)), 0.0, 0.0)
    lower = 1.0 - window(t, 0.8, 2.4)
    mid = 1.0 - window(t, 1.2, 2.8)
    upper = 1.0 - overshoot((t - 1.6) / 1.8, 0.06)
    p.wing("WingUpper", (0.0, 70.0 * upper, -60.0 * upper), (0.0, 50.0 * upper, -20.0 * upper))
    p.wing("WingMid", (0.0, 0.0, -40.0 * mid), (0.0, 0.0, -30.0 * mid))
    p.wing("WingLower", (-50.0 * lower, 20.0 * lower, 0.0), (-30.0 * lower, 0.0, 0.0))
    return p


CLIPS = {
    "ophan_hover_turn": hover_turn,
    "ophan_advance": advance,
    "ophan_fix_gaze": fix_gaze,
    "ophan_take": take,
    "ophan_arrive": arrive,
}


# ---------------------------------------------------------------- rig, frames, bake

def open_rig():
    bpy.ops.wm.open_mainfile(filepath=BLEND)
    rig = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
    mesh = next(o for o in bpy.context.scene.objects if o.type == "MESH")
    for pb in rig.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.matrix_basis.identity()
    bpy.context.view_layer.update()
    return rig, mesh


def frame_times(length: float) -> list[float]:
    n = max(1, round(length * FPS))
    return [i / FPS for i in range(n + 1)]


def basis(rig, pose: Pose) -> dict[str, tuple[Quaternion, Vector, float]]:
    """Each bone's pose-basis (rotation, location, scale). A world-axis turn R about the bone's
    head, relative to its parent, is rest_q^-1 @ R @ rest_q in the bone's own axes."""
    out = {}
    for pb in rig.pose.bones:
        rest_q = pb.bone.matrix_local.to_quaternion()
        r = pose.get(pb.name, Quaternion())
        q = rest_q.inverted() @ r @ rest_q
        loc = Vector((0.0, 0.0, 0.0))
        scale = 1.0
        if pb.name == "Core":
            loc = rest_q.inverted() @ pose.core_offset
            scale = pose.core_scale
        out[pb.name] = (q, loc, scale)
    return out


def solve(cid: str, length: float, rig) -> list[dict]:
    fn = CLIPS[cid]
    return [basis(rig, fn(t, length)) for t in frame_times(length)]


def apply(rig, frame: dict) -> None:
    for name, (q, loc, s) in frame.items():
        pb = rig.pose.bones[name]
        pb.rotation_quaternion = q
        pb.location = loc
        pb.scale = (s, s, s)


def bake_action(rig, cid: str, frames: list[dict]) -> bpy.types.Action:
    action = bpy.data.actions.new(cid)
    action.use_fake_user = True
    if rig.animation_data is None:
        rig.animation_data_create()
    rig.animation_data.action = action
    previous: dict[str, Quaternion] = {}
    for i, frame in enumerate(frames):
        for name, (q, loc, s) in frame.items():
            pb = rig.pose.bones[name]
            prev = previous.get(name)
            if prev is not None and prev.dot(q) < 0.0:
                q = -q
            previous[name] = q.copy()
            pb.rotation_quaternion = q
            pb.location = loc
            pb.scale = (s, s, s)
            for path in ("location", "rotation_quaternion", "scale"):
                pb.keyframe_insert(path, frame=i, group=name)
    curves = bake._fcurves(action)
    want = 10 * len(rig.pose.bones)
    if len(curves) != want:
        raise RuntimeError(f"{cid}: expected {want} F-curves after keying, found {len(curves)}")
    for fc in curves:
        for kp in fc.keyframe_points:
            kp.interpolation = "LINEAR"
    action.frame_range = (0, len(frames) - 1)
    return action


# ---------------------------------------------------------------- measurements

def mesh_extent(rig, mesh, frame: dict) -> tuple[float, float]:
    """Lowest and highest point of the skinned mesh (metres) in this pose."""
    apply(rig, frame)
    bpy.context.view_layer.update()
    dg = bpy.context.evaluated_depsgraph_get()
    ev = mesh.evaluated_get(dg)
    m = ev.to_mesh()
    try:
        zs = [(mesh.matrix_world @ v.co).z for v in m.vertices]
    finally:
        ev.to_mesh_clear()
    return min(zs), max(zs)


def loop_seam_deg(frames: list[dict]) -> float:
    """Largest turn between a loop's first and last frame, over every bone (0 = seamless)."""
    worst = 0.0
    for name, (q0, _l0, _s0) in frames[0].items():
        q1 = frames[-1][name][0]
        worst = max(worst, math.degrees(q0.rotation_difference(q1).angle))
    return worst


def measure(rig, mesh, cid: str, frames: list[dict], loop: bool, step: int = 3) -> dict:
    lows, highs = zip(*(mesh_extent(rig, mesh, f) for f in frames[::step] + [frames[-1]]))
    m = {"frames": len(frames), "lowest_point_m": round(min(lows), 3), "highest_point_m": round(max(highs), 3)}
    if loop:
        m["loop_seam_deg"] = round(loop_seam_deg(frames), 3)
    return m


# ---------------------------------------------------------------- commands

def load_and_check(only=None):
    spec = forge.load_spec()
    rows = {c: v for c, v in spec["clips"].items() if v.get("enemy") == "ophan"}
    if set(rows) != set(CLIPS):
        raise SystemExit(f"anim_spec.json's ophan clips {sorted(rows)} != anim_ophan.py's {sorted(CLIPS)}")
    for cid, v in rows.items():
        if v["fbx"] != FBX:
            raise SystemExit(f"{cid}: anim_spec.json says fbx {v['fbx']}, anim_ophan.py writes {FBX}")
    if only and set(only) - set(CLIPS):
        raise SystemExit(f"no such clip(s): {sorted(set(only) - set(CLIPS))}")
    return spec, [c for c in CLIPS if not only or c in only]


def cmd_build(args) -> int:
    t0 = time.time()
    spec, ids = load_and_check()
    rig, mesh = open_rig()
    solved, metrics, problems = {}, {}, []
    for cid in ids:
        sc = spec["clips"][cid]
        solved[cid] = solve(cid, sc["length_s"], rig)
        m = measure(rig, mesh, cid, solved[cid], sc["loop"])
        metrics[cid] = m
        print(f"  solved {cid:17s} {m['frames']:3d} frames  lowest {m['lowest_point_m']:+.3f} m  "
              f"highest {m['highest_point_m']:.2f} m" + (f"  loop seam {m['loop_seam_deg']:.3f} deg" if "loop_seam_deg" in m else ""))
        if m["lowest_point_m"] < -0.01:
            problems.append(f"{cid}: the mesh goes {m['lowest_point_m']} m below the floor")
        if m.get("loop_seam_deg", 0.0) > 0.01:
            problems.append(f"{cid}: loop seam {m['loop_seam_deg']} deg (first and last frame differ)")
    apply(rig, solved[ids[0]][0])
    actions = [bake_action(rig, cid, solved[cid]) for cid in ids]
    path = os.path.join(OUT_DIR, FBX)
    bake.export_fbx(rig, actions, path, FPS)
    bad, takes = forge.verify_fbx(path, {cid: len(solved[cid]) for cid in ids})
    problems += bad
    print(f"wrote {_rel(path)}: {len(ids)} clips; takes: {', '.join(sorted(takes))}")
    entry = {"file": _rel(path), "skeleton": "the Ophan's own Generic rig (TheOphanOfHours_Rig, 18 bones); "
             "no retarget. Root never moves; Core bobs, and scales in ophan_arrive", "clips": []}
    for cid in ids:
        sc = spec["clips"][cid]
        take = next((k for k in takes if k == cid or k.endswith("|" + cid)), None)
        entry["clips"].append({"clip": cid, "take": take, "layer": sc["layer"], "loop": sc["loop"],
                               "additive": sc["additive"], "length_s": sc["length_s"], "events": sc["events"],
                               **metrics[cid]})
    manifest = {"about": "Written by Tools/ArtForge/anim_ophan.py build. Events are copied from "
                         "anim_spec.json (normalized time).", "fps": FPS, "fbx": [entry]}
    out = os.path.join(OUT_DIR, MANIFEST)
    with open(out, "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, indent=1)
        handle.write("\n")
    print(f"wrote {_rel(out)}; built {len(ids)} clips in {time.time() - t0:.0f}s")
    for p in problems:
        print("  PROBLEM: " + p)
    return 1 if problems else 0


class Stage:
    """The opened Ophan .blend with render.py's review studio round it; duck-types review.Stage
    for review.movie."""

    def __init__(self, rig, mesh):
        self.rig, self.mesh = rig, mesh

    def frame(self, lo: Vector, hi: Vector, resolution: int, samples: int) -> None:
        from anim_forge import review
        studio = review.studio
        for o in [o for o in bpy.context.scene.objects if o.type in {"LIGHT", "CAMERA"}
                  or o.name.startswith("ReviewGround")]:
            bpy.data.objects.remove(o, do_unlink=True)
        studio._configure(resolution, samples)
        studio._world()
        studio._studio((lo + hi) / 2.0, (hi - lo).length)
        studio._ground((hi - lo).length)
        studio._camera(lo, hi, -35.0, 12.0)

    def pose(self, frame: dict, yaw: float = 0.0, lift: float = 0.0) -> None:
        apply(self.rig, frame)
        bpy.context.view_layer.update()

    def shoot(self, path: str, samples=None) -> str:
        if samples:
            bpy.context.scene.cycles.samples = samples
        bpy.context.scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        if not os.path.exists(path):
            raise RuntimeError(f"render did not write {path}")
        return path


def bounds(rig, mesh, frames: list[dict]) -> tuple[Vector, Vector]:
    lo = Vector((1e9, 1e9, 1e9))
    hi = Vector((-1e9, -1e9, -1e9))
    for f in frames[::4] + [frames[-1]]:
        apply(rig, f)
        bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()
        ev = mesh.evaluated_get(dg)
        m = ev.to_mesh()
        for v in m.vertices:
            p = mesh.matrix_world @ v.co
            lo = Vector((min(lo.x, p.x), min(lo.y, p.y), min(lo.z, p.z)))
            hi = Vector((max(hi.x, p.x), max(hi.y, p.y), max(hi.z, p.z)))
        ev.to_mesh_clear()
    lo.z = min(lo.z, 0.0)
    pad = Vector((0.2, 0.2, 0.1))
    return lo - pad, hi + pad


def cmd_review(args) -> int:
    from anim_forge import review
    spec, ids = load_and_check(args.only)
    rig, mesh = open_rig()
    review.WARDEN_CONCEPT = CONCEPT
    stage = Stage(rig, mesh)
    os.makedirs(FEAR_REVIEW, exist_ok=True)
    for cid in ids:
        t0 = time.time()
        sc = spec["clips"][cid]
        length, loop, events = sc["length_s"], sc["loop"], sc["events"]
        frames = solve(cid, length, rig)
        m = measure(rig, mesh, cid, frames, loop)
        lo, hi = bounds(rig, mesh, frames)
        stage.frame(lo, hi, args.tile, args.samples)
        scratch = tempfile.mkdtemp(prefix=f"animophan_{cid}_")
        tiles = []
        for k, i in enumerate(review.still_indices(len(frames), loop)):
            stage.pose(frames[i])
            p = stage.shoot(os.path.join(scratch, f"{k}.png"))
            t = i / FPS
            ev = review.event_at(events, t / length, length)
            tiles.append((p, f"{t:.2f}s ({t / length:.2f})" + (("  " + " + ".join(ev)) if ev else "")))
        evs = ", ".join(f"{e['type']} @{e['t']:.2f}" for e in events) or "none"
        caption = [f"{cid}  ·  fear  ·  {length:.3f} s, {m['frames']} frames @ {FPS} fps  ·  "
                   f"{'LOOP' if loop else 'one-shot'}  ·  {sc['fbx']}",
                   f"events (normalized): {evs}",
                   "on the Ophan of Hours' own Generic rig (no retarget); Root still, the agent moves it",
                   "  ·  ".join(f"{k} {v}" for k, v in m.items() if k != "frames"),
                   f"notes: {sc['notes']}"]
        out = review.compose(cid, tiles, caption, None, os.path.join(FEAR_REVIEW, f"{cid}.png"), args.tile)
        shutil.rmtree(scratch, ignore_errors=True)
        msg = _rel(out)
        if not args.no_mp4:
            mp4 = review.movie(stage, frames, [(0.0, 0.0)] * len(frames), events, length, loop,
                               os.path.join(FEAR_REVIEW, f"{cid}.mp4"), resolution=args.mp4_res,
                               samples=args.mp4_samples, label=cid)
            msg += f" + {_rel(mp4)}"
        print(f"  {cid:17s} -> {msg} in {time.time() - t0:.0f}s")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    sub.add_parser("build")
    r = sub.add_parser("review")
    r.add_argument("--only", nargs="*")
    r.add_argument("--no-mp4", action="store_true")
    r.add_argument("--samples", type=int, default=20)
    r.add_argument("--tile", type=int, default=420)
    r.add_argument("--mp4-res", type=int, default=360)
    r.add_argument("--mp4-samples", type=int, default=8)
    args = ap.parse_args()
    return {"build": cmd_build, "review": cmd_review}[args.cmd](args)


if __name__ == "__main__":
    sys.exit(main())
