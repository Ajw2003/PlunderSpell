"""Solve, measure and export the A1 clips.

    rigs()                 -> (ref Skeleton, warden Skeleton): the reference human (with the
                              polearm's Weapon bones) and the Lantern Warden, from its .blend
    solve_ref(d, ref)      -> reference frames (what the Humanoid FBX carries)
    on_warden(cid, d, ...) -> warden frames: native, or retargeted from the reference like
                              Unity's Humanoid, with the carry layer, hand IK and the lantern
    export_all(...)        -> the FBX files + Animations/anim_manifest.json
"""

from __future__ import annotations

import json
import os

import bpy
from mathutils import Quaternion, Vector

from . import FPS, OUT_DIR, SPEC_PATH, WARDEN_BLEND, bake, clip as clipmod, library, props
from .skeleton import (HUMAN_BONES, WEAPON, Skeleton, build_armature, reference_bones)
from .solve import Solved, layer_override, retarget, solve, to_local

FBX_BASE = "Humanoid_Base.fbx"
FBX_POLE = "Humanoid_Polearm.fbx"
FBX_WARDEN = "LanternWarden_Signature.fbx"


def load_spec() -> dict:
    with open(SPEC_PATH, encoding="utf-8") as handle:
        return json.load(handle)


def check_against_spec(defs: dict, spec: dict) -> list[str]:
    """Every a1 clip in the spec has a definition with the same length and loop flag."""
    problems = []
    for cid, c in spec["clips"].items():
        if c["status"] != "a1":
            continue
        if cid not in defs:
            problems.append(f"{cid}: status a1 in anim_spec.json but library.py does not define it")
            continue
        d = defs[cid].clip
        if bool(c["loop"]) != bool(d.loop):
            problems.append(f"{cid}: loop {d.loop} in library.py, {c['loop']} in the spec")
        if not getattr(d, "speed", None) and abs(float(c["length_s"]) - d.length) > 1e-6:
            problems.append(f"{cid}: length {d.length} s in library.py, {c['length_s']} s in the spec")
    for cid in defs:
        if cid not in spec["clips"]:
            problems.append(f"{cid}: defined in library.py but not in anim_spec.json")
    return problems


def rigs() -> tuple[Skeleton, Skeleton, bpy.types.Object, bpy.types.Object]:
    """Open the warden's .blend and build the reference armature beside it. Returns
    (ref, warden, warden_rig_object, warden_mesh_object)."""
    bpy.ops.wm.open_mainfile(filepath=WARDEN_BLEND)
    wrig = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
    wmesh = next(o for o in bpy.context.scene.objects if o.type == "MESH")
    for pb in wrig.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.matrix_basis.identity()
    ref_obj = build_armature(reference_bones(with_weapon=True), "ReferenceHuman")
    ref = Skeleton.from_armature(ref_obj)
    bpy.data.objects.remove(ref_obj, do_unlink=True)
    warden = Skeleton.from_armature(wrig).retarget_from(ref)
    bpy.context.view_layer.update()
    return ref, warden, wrig, wmesh


def times(c) -> list[float]:
    n = max(1, round(c.length * FPS))
    return [i / FPS for i in range(n + 1)]


def solve_ref(d: library.ClipDef, ref: Skeleton) -> list[Solved]:
    return [solve(ref, d.clip.sample(t, ref), WEAPON if d.weapon else None) for t in times(d.clip)]


def solve_native(d: library.ClipDef, rig: Skeleton) -> list[Solved]:
    """A clip authored on the warden's own rig (carry poses, signature clips)."""
    return [solve(rig, d.clip.sample(t, rig), None) for t in times(d.clip)]


def carry_local(defs: dict, warden: Skeleton, name: str = "warden_carry") -> dict:
    solved = solve_native(defs[name], warden)[0]
    props.hang(warden, [solved], FPS, loop=False)
    return to_local(warden, solved)


def detach_time(spec: dict, cid: str, enemy_clip: str | None = None) -> float | None:
    """Seconds of the lantern's PropDetach for this clip on the warden, if any."""
    rows = spec["enemies"]["lantern-warden"]["clips"]
    events = []
    if cid in spec["clips"]:
        events += spec["clips"][cid]["events"]
    for r in rows:
        if r.get("source") == cid and (enemy_clip is None or r["clip"] == enemy_clip):
            events += r.get("events", [])
    for e in events:
        if e["type"] == "PropDetach" and (e.get("param") or {}).get("prop") == "LanternRing":
            return e["t"]
    return None


def on_warden(cid: str, d: library.ClipDef, ref: Skeleton, warden: Skeleton, defs: dict,
              spec: dict, ref_frames: list[Solved] | None = None, hand_ik: bool = True,
              carry: dict | None = None) -> list[Solved]:
    """The clip as it will look on the Lantern Warden."""
    if d.rig == "warden":
        frames = solve_native(d, warden)
    else:
        ref_frames = ref_frames or solve_ref(d, ref)
        frames = []
        for s, t in zip(ref_frames, times(d.clip)):
            lh = d.clip.sample(t, ref).lhand if d.weapon else 0.0
            w = retarget(warden, s, weapon_bone="Glaive" if d.weapon else None,
                         lhand=lh if hand_ik else 0.0)
            if d.carry_mask:
                w = layer_override(warden, w, carry or carry_local(defs, warden), d.carry_mask)
            frames.append(w)
    at = detach_time(spec, cid)
    if at is not None:
        props.drop(warden, frames, FPS, at * d.clip.length)
    else:
        props.hang(warden, frames, FPS, loop=d.clip.loop)
    return frames


# ---- metrics ----------------------------------------------------------------------

def foot_slide(skel: Skeleton, frames: list[Solved], speed: float, loops: int = 2,
               contact: float = 0.004) -> dict:
    """Planted-foot slide in the WORLD (the agent's travel at `speed` added back).
    For each sole point (heel, ball, toe) of each foot, every run of frames where it
    is within `contact` of the floor is one contact; slide = the XY extent of the
    point over that run. Also returns the trace for the review sheet."""
    period = len(frames) - 1
    trace = {}
    result = {}
    for side in ("L", "R"):
        g = clipmod.foot_geometry(skel, side)
        for k in ("heel", "ball", "toe"):
            pts = []
            for i in range(period * loops + 1):
                s = frames[i % period] if period else frames[0]
                t = i / FPS
                p = skel.fk(s.Qx, s.hips, s.free).point(f"Foot.{side}", g[k])
                pts.append((t, p + Vector((0.0, -speed * t, 0.0))))
            trace[(side, k)] = pts
            runs, cur = [], []
            for t, p in pts:
                if p.z < contact:
                    cur.append(p)
                elif cur:
                    runs.append(cur)
                    cur = []
            if cur:
                runs.append(cur)
            slides = [max((a - b).to_2d().length for a in r for b in r) for r in runs if len(r) > 1]
            result[f"{side}.{k}"] = {"contacts": len(runs), "max_slide_m": max(slides, default=0.0),
                                     "min_z_m": min(p.z for _t, p in pts)}
    worst = max(v["max_slide_m"] for v in result.values())
    return {"points": result, "max_slide_m": worst, "trace": trace}


def haft_error(skel: Skeleton, frames: list[Solved], weapon_bone: str, lh: list[float]) -> float:
    """Worst distance (m) from the left fist centre to the haft axis while the left
    hand is meant to be on it (IK weight > 0.99)."""
    worst = 0.0
    for s, w in zip(frames, lh):
        if w < 0.99:
            continue
        posed = skel.fk(s.Qx, s.hips, s.free)
        head = posed.heads[weapon_bone]
        axis = posed.world[weapon_bone] @ skel.direction(weapon_bone)
        fist = posed.point("Hand.L", skel.grip("L"))
        off = fist - head
        worst = max(worst, (off - axis * off.dot(axis)).length)
    return worst


def lowest_point(skel: Skeleton, frames: list[Solved]) -> float:
    low = 1e9
    for s in frames:
        posed = skel.fk(s.Qx, s.hips, s.free)
        for side in ("L", "R"):
            g = clipmod.foot_geometry(skel, side)
            for k in ("heel", "ball", "toe"):
                low = min(low, posed.point(f"Foot.{side}", g[k]).z)
    return low


# ---- export -----------------------------------------------------------------------

def _fresh_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = FPS


def export_reference(path: str, clip_frames: dict[str, list[Solved]], with_weapon: bool,
                     root_name: str) -> dict:
    _fresh_scene()
    obj = build_armature(reference_bones(with_weapon=with_weapon), root_name)
    skel = Skeleton.from_armature(obj)
    actions = [bake.bake_action(obj, skel, cid, frames, FPS) for cid, frames in clip_frames.items()]
    bake.export_fbx(obj, actions, path, FPS)
    return {"bones": len(skel.order)}


def export_warden(path: str, clip_frames: dict[str, list[Solved]]) -> dict:
    bpy.ops.wm.open_mainfile(filepath=WARDEN_BLEND)
    for o in [o for o in bpy.context.scene.objects if o.type != "ARMATURE"]:
        bpy.data.objects.remove(o, do_unlink=True)      # animation only: no mesh
    rig = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
    rig.name = "LanternWarden_Rig"
    for pb in rig.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.matrix_basis.identity()
    skel = Skeleton.from_armature(rig)
    bpy.context.scene.render.fps = FPS
    actions = [bake.bake_action(rig, skel, cid, frames, FPS) for cid, frames in clip_frames.items()]
    bake.export_fbx(rig, actions, path, FPS)
    return {"bones": len(skel.order)}


def verify_fbx(path: str, expected: dict[str, int]) -> list[str]:
    """Re-import the FBX: every clip present as a take with the right frame count."""
    takes = bake.read_back(path)
    problems = []
    for cid, n in expected.items():
        match = [k for k in takes if k == cid or k.endswith("|" + cid)]
        if not match:
            problems.append(f"{os.path.basename(path)}: take {cid} missing (takes: {sorted(takes)})")
            continue
        a, b = takes[match[0]]
        if b - a != n - 1:
            problems.append(f"{os.path.basename(path)}: take {cid} has frames {a}..{b}, expected {n}")
    return problems, takes
