#!/usr/bin/env python3
"""AnimForge for the Wick (#386, the fear pass): build and review its clips.

    python3.11 Tools/ArtForge/anim_wick.py build     # solve on the reference, export
                                                     # Animations/Humanoid_Fear.fbx, re-import to
                                                     # verify, write fear_anim_manifest.json
    python3.11 Tools/ArtForge/anim_wick.py review [--only ID ...] [--no-mp4]
                                                     # docs/art/anim/fear/<clip>.png (+ .mp4): the
                                                     # Wick's clips on the Wick, warden_turn on the
                                                     # Lantern Warden (the lantern drops)

Clip list, lengths, loop flags and events: anim_spec.json (layer "fear"); motion:
anim_forge/library_wick.py. anim.py and anim_player.py never see these clips.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import sys
import tempfile
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import anim_forge  # noqa: E402
from anim_forge import FPS, OUT_DIR, REVIEW_DIR, forge, library_wick, props  # noqa: E402
from anim_forge.solve import retarget  # noqa: E402
from anim import _rel, d_mismatch, detect_footsteps  # noqa: E402
from anim_player import lowest_bone  # noqa: E402

FBX = "Humanoid_Fear.fbx"
WICK_BLEND = os.path.join(anim_forge.REPO_ROOT, "Assets", "Models", "ArtBible", "Enemies", "Fear",
                          "TheWick", "TheWick.blend")
WICK_CONCEPT = os.path.join(anim_forge.REPO_ROOT, "docs", "art", "concept", "fear", "wick-v2-1-true-form.png")
FEAR_REVIEW = os.path.join(REVIEW_DIR, "fear")
ON_WARDEN = {"warden_turn"}


def wick_rig():
    """The reference and the Wick's skeleton, and the Wick's hip scale."""
    ref, wick, wrig, wmesh = forge.rigs(WICK_BLEND)
    return ref, wick, wrig, wmesh


def solve_all(scale: float, only=None):
    import bpy  # noqa: F401
    ref, _w, _r, _m = forge.rigs()
    defs = library_wick.clips(ref, scale)
    spec = forge.load_spec()
    for cid, d in defs.items():
        sc = spec["clips"].get(cid)
        if sc is None or sc["layer"] != "fear":
            raise SystemExit(f"{cid}: not in anim_spec.json as a fear clip")
        if bool(sc["loop"]) != d.clip.loop or (not d.clip.speed and abs(sc["length_s"] - d.clip.length) > 1e-5):
            raise SystemExit(f"{cid}: library_wick.py and anim_spec.json disagree on loop/length")
    missing = [c for c, v in spec["clips"].items() if v["layer"] == "fear" and c not in defs]
    if missing:
        raise SystemExit(f"in the spec but not in library_wick.py: {missing}")
    if only and set(only) - set(defs):
        raise SystemExit(f"no such clip(s): {sorted(set(only) - set(defs))}")
    ids = [c for c in defs if not only or c in only]
    return ref, defs, spec, ids


def crown(skel, frames) -> tuple[float, float]:
    """Lowest and highest crown (the top of the Hat bone, else Head) over the clip."""
    bone = "Hat" if "Hat" in skel.head else "Head"
    zs = [skel.fk(s.Qx, s.hips, s.free).tail(bone).z for s in frames]
    return min(zs), max(zs)


def measure(cid, d, ref, rig=None):
    frames = forge.solve_ref(d, ref)
    m = {"frames": len(frames), "length_s": round(d.clip.length, 4), "loop": d.clip.loop}
    m["ik_shortfall_m"] = round(max(max(s.shortfall.values(), default=0.0) for s in frames), 4)
    if d.clip.speed:
        m["speed_ref_mps"] = round(d.clip.speed, 4)
        m["stride_ref_m"] = round(d.clip.stride(), 3)
        m["hip_drop_ref_m"] = round(d.clip.drop, 3)
        m["foot_slide_ref_max_m"] = round(forge.foot_slide(ref, frames, d.clip.speed)["max_slide_m"], 4)
    if d.clip.feet_ik:
        m["lowest_sole_ref_m"] = round(forge.lowest_point(ref, frames), 4)
    m["lowest_bone_ref_m"] = round(lowest_bone(ref, frames), 4)
    m["detected_footsteps"] = detect_footsteps(ref, frames, d.clip.length)
    rframes = None
    if rig is not None:
        rframes = [retarget(rig, s) for s in frames]
        if d.clip.speed:
            speed = getattr(d.clip, "wick_speed", d.clip.speed * rig.hip_scale)
            slide = forge.foot_slide(rig, rframes, speed)
            m["speed_on_rig_mps"] = speed
            m["foot_slide_rig_max_m"] = round(slide["max_slide_m"], 4)
            m["_trace"] = slide
            m["_speed"] = speed
        lo, hi = crown(rig, rframes)
        m["crown_on_rig_m"] = [round(lo, 3), round(hi, 3)]
        m["lowest_bone_rig_m"] = round(lowest_bone(rig, rframes), 4)
    return frames, rframes, m


def cmd_build(args) -> int:
    t0 = time.time()
    ref, wick, _r, _m = wick_rig()
    scale = wick.hip_scale
    ref, defs, spec, ids = solve_all(scale)
    solved, metrics, problems = {}, {}, []
    for cid in ids:
        solved[cid], _w, metrics[cid] = measure(cid, defs[cid], ref)
        m = metrics[cid]
        print(f"  solved {cid:13s} {m['frames']:3d} frames"
              + (f"  slide {m['foot_slide_ref_max_m'] * 100:.2f} cm" if "foot_slide_ref_max_m" in m else "")
              + f"  lowest bone {m['lowest_bone_ref_m']:.3f} m  IK short {m['ik_shortfall_m'] * 100:.1f} cm")
        if m.get("foot_slide_ref_max_m", 0.0) >= 0.01:
            problems.append(f"{cid}: foot slide {m['foot_slide_ref_max_m'] * 100:.2f} cm >= 1 cm")
        if m["lowest_bone_ref_m"] < -0.01:
            problems.append(f"{cid}: a bone is {m['lowest_bone_ref_m']} m below the floor")
    path = os.path.join(OUT_DIR, FBX)
    info = forge.export_reference(path, solved, with_weapon=False, root_name="ReferenceHuman")
    bad, takes = forge.verify_fbx(path, {c: len(solved[c]) for c in ids})
    problems += bad
    print(f"wrote {_rel(path)}: {len(ids)} clips, {info['bones']} bones; takes: {', '.join(sorted(takes))}")
    entry = {"file": _rel(path), "skeleton": "reference human: figures.Human() 1.76 m. Plays on the Wick "
             f"(hip scale {scale:.3f}) and, for warden_turn, on the Lantern Warden; the Wick's Cage, Hat "
             "and Blade bones are never keyed", "bones": info["bones"], "clips": []}
    for c in ids:
        sc = spec["clips"][c]
        take = next((k for k in takes if k == c or k.endswith("|" + c)), None)
        entry["clips"].append({"clip": c, "take": take, "layer": sc["layer"], "loop": sc["loop"],
                               "additive": sc["additive"], "events": sc["events"],
                               "agent_speed_mps": sc["speed_mps"],
                               **{k: v for k, v in metrics[c].items() if not k.startswith("_")}})
        want = sorted((e["param"]["foot"], e["t"]) for e in sc["events"] if e["type"] == "Footstep")
        got = sorted((e["foot"], e["t"]) for e in metrics[c]["detected_footsteps"])
        if want and d_mismatch(want, got, defs[c].clip):
            print(f"  note: {c} Footstep events {want} vs detected landings {got}")
    manifest = {"about": "Written by Tools/ArtForge/anim_wick.py build. Events are copied from "
                         "anim_spec.json (normalized time).", "fps": FPS, "hip_scale_wick": round(scale, 4),
                "fbx": [entry]}
    out = os.path.join(OUT_DIR, "fear_anim_manifest.json")
    with open(out, "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, indent=1)
        handle.write("\n")
    print(f"wrote {_rel(out)}; built {len(ids)} clips in {time.time() - t0:.0f}s")
    for p in problems:
        print("  PROBLEM: " + p)
    return 1 if problems else 0


def cmd_review(args) -> int:
    from anim_forge import review
    ref, wick, _r, _m = wick_rig()
    scale = wick.hip_scale
    _ref, defs, spec, ids = solve_all(scale, args.only)
    os.makedirs(FEAR_REVIEW, exist_ok=True)
    for blend, group, concept, label in ((WICK_BLEND, [c for c in ids if c not in ON_WARDEN], WICK_CONCEPT, "Wick"),
                                         (forge.WARDEN_BLEND, [c for c in ids if c in ON_WARDEN],
                                          anim_forge.WARDEN_CONCEPT, "Lantern Warden")):
        if not group:
            continue
        review.WARDEN_CONCEPT = concept
        ref, rig_skel, wrig, wmesh = forge.rigs(blend)
        rig_skel.name = label
        print(f"reviewing on {_rel(blend)}")
        stage = review.Stage(wrig, wmesh, rig_skel)
        for cid in group:
            t0 = time.time()
            d = defs[cid]
            _f, frames, m = measure(cid, d, ref, rig_skel)
            sc = spec["clips"][cid]
            events = sc["events"]
            drop = next((e for e in events if e["type"] == "PropDetach"), None)
            if label == "Lantern Warden":
                if drop:
                    props.drop(rig_skel, frames, FPS, drop["t"] * d.clip.length)
                else:
                    props.hang(rig_skel, frames, FPS, loop=d.clip.loop)
            offsets = [(0.0, 0.0)] * len(frames)
            lo, hi = review._bounds(rig_skel, frames, offsets)
            stage.frame(lo, hi, args.tile, args.samples)
            scratch = tempfile.mkdtemp(prefix=f"animwick_{cid}_")
            tiles = []
            for k, i in enumerate(review.still_indices(len(frames), d.clip.loop)):
                stage.pose(frames[i])
                p = stage.shoot(os.path.join(scratch, f"{k}.png"))
                t = i / FPS
                ev = review.event_at(events, t / d.clip.length, d.clip.length)
                tiles.append((p, f"{t:.2f}s ({t / d.clip.length:.2f})" + (("  " + " + ".join(ev)) if ev else "")))
            panel = None
            if "_trace" in m:
                pts = {k: {"max_slide_m": v["max_slide_m"]} for k, v in m["_trace"]["points"].items()}
                panel = review.trace_panel(m["_trace"]["trace"], m["_speed"], 2400, 380, pts)
            evs = ", ".join(f"{e['type']}{'(' + str(list((e.get('param') or {}).values())[0]) + ')' if e.get('param') else ''}"
                            f" @{e['t']:.2f}" for e in events) or "none"
            caption = [f"{cid}  ·  fear  ·  {d.clip.length:.3f} s, {m['frames']} frames @ {FPS} fps  ·  "
                       f"{'LOOP' if d.clip.loop else 'one-shot'}{'  ·  ADDITIVE' if sc['additive'] else ''}  ·  {sc['fbx']}",
                       f"events (normalized): {evs}",
                       f"on the {label}: retargeted from the reference like Humanoid (hip scale "
                       f"{rig_skel.hip_scale:.3f})",
                       "  ·  ".join(f"{k} {v}" for k, v in m.items()
                                    if not k.startswith("_") and k not in ("frames", "length_s", "loop",
                                                                           "detected_footsteps")),
                       f"notes: {d.clip.notes}"]
            out = review.compose(cid, tiles, caption, panel, os.path.join(FEAR_REVIEW, f"{cid}.png"), args.tile)
            shutil.rmtree(scratch, ignore_errors=True)
            msg = _rel(out)
            if not args.no_mp4:
                mp4 = review.movie(stage, frames, offsets, events, d.clip.length, d.clip.loop,
                                   os.path.join(FEAR_REVIEW, f"{cid}.mp4"), resolution=args.mp4_res,
                                   samples=args.mp4_samples, label=cid)
                msg += f" + {_rel(mp4)}"
            print(f"  {cid:13s} -> {msg} in {time.time() - t0:.0f}s")
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
