#!/usr/bin/env python3
"""AnimForge for the player wizard (issue #361): build and review the player clips.

    python3.11 Tools/ArtForge/anim_player.py build      # solve on the reference, export
                                                         # Animations/Humanoid_Player.fbx, re-import
                                                         # to verify, write player_anim_manifest.json
    python3.11 Tools/ArtForge/anim_player.py review [--only ID ...] [--no-mp4] [--blend PATH]
                                                         # docs/art/anim/player/<clip>.png (+ .mp4)
                                                         # on the wizard (else the Lantern Warden)

Clip list, lengths, loop flags and events: anim_spec.json (layer "player"); motion:
anim_forge/library_player.py. anim.py never sees these clips (it gathers library.clips only).
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
from anim_forge import FPS, OUT_DIR, REVIEW_DIR, clip as clipmod, forge, library_player  # noqa: E402
from anim_forge.skeleton import HUMAN_BONES  # noqa: E402
from anim_forge.solve import retarget  # noqa: E402
from anim import _rel, d_mismatch, detect_footsteps  # noqa: E402

FBX = "Humanoid_Player.fbx"
WIZARD_BLEND = os.path.join(anim_forge.REPO_ROOT, "Assets", "Models", "ArtBible", "Players", "Lair",
                            "Wizard", "Wizard.blend")
WIZARD_CONCEPT = os.path.join(anim_forge.REPO_ROOT, "docs", "art", "concept", "wizard", "1-the-wizard.png")
PLAYER_REVIEW = os.path.join(REVIEW_DIR, "player")


def lowest_bone(skel, frames) -> float:
    """Lowest bone head or tail of any bone (a body lying down, not just the soles)."""
    low = 1e9
    for s in frames:
        posed = skel.fk(s.Qx, s.hips, s.free)
        for n in HUMAN_BONES:
            low = min(low, posed.heads[n].z, posed.tail(n).z)
    return low


def solve_all(only=None):
    ref, _w, _r, _m = forge.rigs()
    defs = library_player.clips(ref)
    spec = forge.load_spec()
    for cid, d in defs.items():
        sc = spec["clips"].get(cid)
        if sc is None or sc["layer"] != "player":
            raise SystemExit(f"{cid}: not in anim_spec.json as a player clip")
        if bool(sc["loop"]) != d.clip.loop or (not d.clip.speed and abs(sc["length_s"] - d.clip.length) > 1e-5):
            raise SystemExit(f"{cid}: library_player.py and anim_spec.json disagree on loop/length")
    missing = [c for c, v in spec["clips"].items() if v["layer"] == "player" and c not in defs]
    if missing:
        raise SystemExit(f"in the spec but not in library_player.py: {missing}")
    ids = [c for c in defs if not only or c in only]
    if only and set(only) - set(defs):
        raise SystemExit(f"no such clip(s): {sorted(set(only) - set(defs))}")
    return ref, defs, spec, ids


def measure(cid, d, ref, spec, wiz=None):
    frames = forge.solve_ref(d, ref)
    m = {"frames": len(frames), "length_s": round(d.clip.length, 4), "loop": d.clip.loop}
    m["ik_shortfall_m"] = round(max(max(s.shortfall.values(), default=0.0) for s in frames), 4)
    if d.clip.speed:
        m["speed_mps"] = d.clip.speed
        m["stride_m"] = round(d.clip.stride(), 3)
        m["hip_drop_m"] = round(d.clip.drop, 3)
        slide = forge.foot_slide(ref, frames, d.clip.speed)
        m["foot_slide_ref_max_m"] = round(slide["max_slide_m"], 4)
        m["_trace"] = slide
    if d.clip.feet_ik:
        m["lowest_sole_ref_m"] = round(forge.lowest_point(ref, frames), 4)
    m["lowest_bone_ref_m"] = round(lowest_bone(ref, frames), 4)
    m["detected_footsteps"] = detect_footsteps(ref, frames, d.clip.length)
    wframes = None
    if wiz is not None:
        wframes = [retarget(wiz, s) for s in frames]
        if d.clip.speed:
            m["foot_slide_wizard_max_m"] = round(forge.foot_slide(wiz, wframes, d.clip.speed)["max_slide_m"], 4)
        m["lowest_bone_wizard_m"] = round(lowest_bone(wiz, wframes), 4)
    return frames, wframes, m


def cmd_build(args) -> int:
    t0 = time.time()
    ref, defs, spec, ids = solve_all()
    solved, metrics, problems = {}, {}, []
    for cid in ids:
        solved[cid], _w, metrics[cid] = measure(cid, defs[cid], ref, spec)
        m = metrics[cid]
        print(f"  solved {cid:15s} {m['frames']:3d} frames"
              + (f"  slide {m['foot_slide_ref_max_m'] * 100:.2f} cm" if "speed_mps" in m else "")
              + f"  lowest bone {m['lowest_bone_ref_m']:.3f} m")
        if m.get("foot_slide_ref_max_m", 0.0) >= 0.01:
            problems.append(f"{cid}: foot slide {m['foot_slide_ref_max_m'] * 100:.2f} cm >= 1 cm")
        if cid == "death_collapse" and m["lowest_bone_ref_m"] < -0.01:
            problems.append(f"{cid}: a bone is {m['lowest_bone_ref_m']} m below the floor")
    path = os.path.join(OUT_DIR, FBX)
    info = forge.export_reference(path, solved, with_weapon=False, root_name="ReferenceHuman")
    bad, takes = forge.verify_fbx(path, {c: len(solved[c]) for c in ids})
    problems += bad
    print(f"wrote {_rel(path)}: {len(ids)} clips, {info['bones']} bones; takes: {', '.join(sorted(takes))}")
    entry = {"file": _rel(path), "skeleton": "reference human: figures.Human() 1.76 m (no Hat bone; "
             "the wizard's Hat bone is never keyed)", "bones": info["bones"], "clips": []}
    for c in ids:
        sc = spec["clips"][c]
        take = next((k for k in takes if k == c or k.endswith("|" + c)), None)
        entry["clips"].append({"clip": c, "take": take, "layer": sc["layer"], "family": sc["family"],
                               "loop": sc["loop"], "additive": sc["additive"], "events": sc["events"],
                               **{k: v for k, v in metrics[c].items() if not k.startswith("_")}})
        want = sorted((e["param"]["foot"], e["t"]) for e in sc["events"] if e["type"] == "Footstep")
        got = sorted((e["foot"], e["t"]) for e in metrics[c]["detected_footsteps"])
        if want and d_mismatch(want, got, defs[c].clip):
            print(f"  note: {c} Footstep events {want} vs detected landings {got}")
    manifest = {"about": "Written by Tools/ArtForge/anim_player.py build. Events are copied from "
                         "anim_spec.json (normalized time).", "fps": FPS, "fbx": [entry]}
    out = os.path.join(OUT_DIR, "player_anim_manifest.json")
    with open(out, "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, indent=1)
        handle.write("\n")
    print(f"wrote {_rel(out)}; built {len(ids)} clips in {time.time() - t0:.0f}s")
    for p in problems:
        print("  PROBLEM: " + p)
    return 1 if problems else 0


def cmd_review(args) -> int:
    from anim_forge import review
    blend = args.blend or (WIZARD_BLEND if os.path.exists(WIZARD_BLEND) else forge.WARDEN_BLEND)
    on_wizard = os.path.abspath(blend) == os.path.abspath(WIZARD_BLEND)
    review.WARDEN_CONCEPT = WIZARD_CONCEPT if on_wizard else anim_forge.WARDEN_CONCEPT
    ref, defs, spec, ids = solve_all(args.only)
    ref, rig_skel, wrig, wmesh = forge.rigs(blend)
    rig_skel.name = "Wizard" if on_wizard else "Lantern Warden"
    print(f"reviewing on {_rel(blend)}")
    stage = review.Stage(wrig, wmesh, rig_skel)
    out_dir = os.path.join(PLAYER_REVIEW if on_wizard else os.path.join(PLAYER_REVIEW, "warden_check"))
    for cid in ids:
        t0 = time.time()
        d = defs[cid]
        _f, frames, m = measure(cid, d, ref, spec, rig_skel)
        offsets = [(0.0, 0.0)] * len(frames)
        lo, hi = review._bounds(rig_skel, frames, offsets)
        stage.frame(lo, hi, args.tile, args.samples)
        sc = spec["clips"][cid]
        events = sc["events"]
        scratch = tempfile.mkdtemp(prefix=f"animplayer_{cid}_")
        tiles = []
        for k, i in enumerate(review.still_indices(len(frames), d.clip.loop)):
            stage.pose(frames[i])
            p = stage.shoot(os.path.join(scratch, f"{k}.png"))
            t = i / FPS
            ev = review.event_at(events, t / d.clip.length, d.clip.length)
            tiles.append((p, f"{t:.2f}s ({t / d.clip.length:.2f})" + (("  " + " + ".join(ev)) if ev else "")))
        panel = None
        if "_trace" in m:
            pts = {k: v["max_slide_m"] for k, v in m["_trace"]["points"].items()}
            panel = review.trace_panel(m["_trace"]["trace"], d.clip.speed, 2400, 380,
                                       {k: {"max_slide_m": v} for k, v in pts.items()})
        evs = ", ".join(f"{e['type']}{'(' + e['param']['foot'] + ')' if e.get('param') else ''} @{e['t']:.2f}"
                        for e in events) or "none"
        caption = [f"{cid}  ·  player  ·  {d.clip.length:.3f} s, {m['frames']} frames @ {FPS} fps  ·  "
                   f"{'LOOP' if d.clip.loop else 'one-shot'}  ·  {sc['fbx']}",
                   f"events (normalized): {evs}",
                   f"on the {rig_skel.name}: retargeted from the reference like Humanoid; the Hat bone is not keyed",
                   "  ·  ".join(f"{k} {v}" for k, v in m.items()
                                if not k.startswith("_") and k not in ("frames", "length_s", "loop")),
                   f"notes: {d.clip.notes}"]
        out = review.compose(cid, tiles, caption, panel, os.path.join(out_dir, f"{cid}.png"), args.tile)
        shutil.rmtree(scratch, ignore_errors=True)
        msg = _rel(out)
        if not args.no_mp4:
            mp4 = review.movie(stage, frames, offsets, events, d.clip.length, d.clip.loop,
                               os.path.join(out_dir, f"{cid}.mp4"), resolution=args.mp4_res,
                               samples=args.mp4_samples, label=cid)
            msg += f" + {_rel(mp4)}"
        print(f"  {cid:15s} -> {msg} in {time.time() - t0:.0f}s")
    return 0


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    sub.add_parser("build")
    r = sub.add_parser("review")
    r.add_argument("--only", nargs="*")
    r.add_argument("--no-mp4", action="store_true")
    r.add_argument("--blend", help="rig to review on (default: the wizard if built, else the warden)")
    r.add_argument("--samples", type=int, default=20)
    r.add_argument("--tile", type=int, default=420)
    r.add_argument("--mp4-res", type=int, default=360)
    r.add_argument("--mp4-samples", type=int, default=8)
    args = ap.parse_args()
    return {"build": cmd_build, "review": cmd_review}[args.cmd](args)


if __name__ == "__main__":
    sys.exit(main())
