#!/usr/bin/env python3
"""AnimForge: build, export and review the enemy animation clips (plan phase A1).

    python3 Tools/ArtForge/anim.py build                 # solve every A1 clip, export the FBX
                                                          # files, re-import them to verify, write
                                                          # Animations/anim_manifest.json
    python3 Tools/ArtForge/anim.py review                # sheet + MP4 per clip in docs/art/anim/
    python3 Tools/ArtForge/anim.py review --only walk run --no-mp4 --samples 16
    python3 Tools/ArtForge/anim.py metrics               # the numbers only (fast, no render)

The clip list, lengths, loop flags and events are in Tools/ArtForge/anim_spec.json;
the motion is in Tools/ArtForge/anim_forge/library.py. Exits non-zero on any
spec/library disagreement, a failed export check or a missing render.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import anim_forge  # noqa: E402  (bpy, art_forge, EnemyForge on sys.path)
from anim_forge import FPS, OUT_DIR, REVIEW_DIR, clip as clipmod, forge, library  # noqa: E402
from anim_forge.skeleton import WEAPON  # noqa: E402

REPO = anim_forge.REPO_ROOT


def _rel(p):
    return os.path.relpath(p, REPO)


def detect_footsteps(skel, frames, length) -> list[dict]:
    """Normalized times a foot lands: its lowest sole point drops from above 1.5 cm
    to within 0.5 cm of the floor."""
    out = []
    for side in ("L", "R"):
        g = clipmod.foot_geometry(skel, side)
        was_up = None
        for i, s in enumerate(frames):
            posed = skel.fk(s.Qx, s.hips, s.free)
            low = min(posed.point(f"Foot.{side}", g[k]).z for k in ("heel", "ball", "toe"))
            if was_up is None:
                was_up = low > 0.015
            if low > 0.015:
                was_up = True
            elif low < 0.005 and was_up:
                out.append({"foot": side, "t": round(i / FPS / max(length, 1e-6), 3)})
                was_up = False
    return sorted(out, key=lambda e: e["t"])


def gather(only=None):
    ref, warden, wrig, wmesh = forge.rigs()
    defs = library.clips(ref, warden)
    spec = forge.load_spec()
    problems = forge.check_against_spec(defs, spec)
    if problems:
        print("anim_spec.json and library.py disagree:")
        for p in problems:
            print("  - " + p)
        raise SystemExit(1)
    ids = list(defs)
    if only:
        unknown = sorted(set(only) - set(defs))
        if unknown:
            raise SystemExit(f"no such clip(s): {', '.join(unknown)}; known: {', '.join(defs)}")
        ids = [c for c in ids if c in only]
    return ref, warden, wrig, wmesh, defs, spec, ids


def measure(cid, d, ref, warden, defs, spec, carry):
    """Solve on the reference and on the warden; return frames and metrics."""
    ref_frames = forge.solve_ref(d, ref) if d.rig == "ref" else None
    w_frames = forge.on_warden(cid, d, ref, warden, defs, spec, ref_frames, carry=carry)
    m = {"frames": len(w_frames), "length_s": round(d.clip.length, 4), "loop": d.clip.loop}
    native = ref_frames if ref_frames is not None else w_frames
    skel = ref if ref_frames is not None else warden
    m["ik_shortfall_m"] = round(max((max(s.shortfall.values(), default=0.0) for s in native),
                                    default=0.0), 4)
    if getattr(d.clip, "speed", None):
        m["speed_mps"] = d.clip.speed
        m["stride_m"] = round(d.clip.stride(), 3)
        m["hip_drop_m"] = round(d.clip.drop, 3)
        slide_ref = forge.foot_slide(ref, ref_frames, d.clip.speed)
        slide_w = forge.foot_slide(warden, w_frames, d.clip.speed)
        m["foot_slide_ref_max_m"] = round(slide_ref["max_slide_m"], 4)
        m["foot_slide_warden_max_m"] = round(slide_w["max_slide_m"], 4)
        m["_trace"] = slide_w
    if d.clip.feet_ik:
        m["lowest_sole_ref_m"] = round(forge.lowest_point(skel, native), 4)
        m["lowest_sole_warden_m"] = round(forge.lowest_point(warden, w_frames), 4)
    if d.weapon:
        lh = [d.clip.sample(t, ref).lhand for t in forge.times(d.clip)]
        m["haft_error_ref_m"] = round(forge.haft_error(ref, ref_frames, WEAPON, lh), 4)
        raw = forge.on_warden(cid, d, ref, warden, defs, spec, ref_frames, hand_ik=False, carry=carry)
        per = []
        per_raw = []
        for s, r in zip(w_frames, raw):
            per.append(forge.haft_error(warden, [s], "Glaive", [1.0]))
            per_raw.append(forge.haft_error(warden, [r], "Glaive", [1.0]))
        on = [w > 0.99 for w in lh]
        m["haft_error_warden_ik_m"] = round(max([e for e, o in zip(per, on) if o], default=0.0), 4)
        m["haft_error_warden_retarget_only_m"] = round(max([e for e, o in zip(per_raw, on) if o],
                                                           default=0.0), 4)
        m["_haft"] = (per, per_raw, lh)
    m["detected_footsteps"] = detect_footsteps(skel, native, d.clip.length)
    return ref_frames, w_frames, m


def cmd_metrics(args) -> int:
    ref, warden, _r, _m, defs, spec, ids = gather(args.only)
    carry = forge.carries(defs, warden)
    for cid in ids:
        _rf, _wf, m = measure(cid, defs[cid], ref, warden, defs, spec, carry)
        print(cid, json.dumps({k: v for k, v in m.items() if not k.startswith("_")}))
    return 0


def cmd_build(args) -> int:
    started = time.time()
    ref, warden, _r, _m, defs, spec, ids = gather(None)
    carry = forge.carries(defs, warden)
    groups = {forge.FBX_BASE: [], forge.FBX_POLE: [], forge.FBX_WARDEN: []}
    solved, metrics = {}, {}
    for cid in ids:
        d = defs[cid]
        fbx = spec["clips"][cid]["fbx"]
        if fbx not in groups:
            raise SystemExit(f"{cid}: spec names FBX {fbx}, which A1 does not build")
        ref_frames, w_frames, m = measure(cid, d, ref, warden, defs, spec, carry)
        solved[cid] = ref_frames if d.rig == "ref" else w_frames
        metrics[cid] = m
        groups[fbx].append(cid)
        print(f"  solved {cid:22s} {m['frames']:3d} frames"
              + (f"  slide ref {m['foot_slide_ref_max_m'] * 100:.2f} cm, warden "
                 f"{m['foot_slide_warden_max_m'] * 100:.2f} cm" if "speed_mps" in m else "")
              + (f"  haft ref {m['haft_error_ref_m'] * 100:.2f} cm" if "haft_error_ref_m" in m else ""))
    problems = []
    manifest = {"about": "Written by Tools/ArtForge/anim.py build. One entry per exported FBX; "
                         "events are copied from anim_spec.json (normalized time).",
                "fps": FPS, "fbx": []}
    for fbx, cids in groups.items():
        path = os.path.join(OUT_DIR, fbx)
        frames = {c: solved[c] for c in cids}
        if fbx == forge.FBX_WARDEN:
            info = forge.export_warden(path, frames)
            skeleton = "Lantern Warden rig (Assets/Models/ArtBible/Enemies/High/LanternWarden)"
        else:
            info = forge.export_reference(path, frames, with_weapon=(fbx == forge.FBX_POLE),
                                          root_name="ReferenceHuman")
            skeleton = ("reference human: figures.Human() 1.76 m"
                        + (" + Weapon, Weapon_GripL" if fbx == forge.FBX_POLE else ""))
        bad, takes = forge.verify_fbx(path, {c: len(solved[c]) for c in cids})
        problems += bad
        print(f"wrote {_rel(path)}: {len(cids)} clips, {info['bones']} bones; re-import takes: "
              f"{', '.join(sorted(takes))}")
        entry = {"file": _rel(path), "skeleton": skeleton, "bones": info["bones"], "clips": []}
        for c in cids:
            sc = spec["clips"][c]
            take = next((k for k in takes if k == c or k.endswith("|" + c)), None)
            entry["clips"].append({
                "clip": c, "take": take, "layer": sc["layer"], "family": sc["family"],
                "loop": sc["loop"], "additive": sc["additive"], "events": sc["events"],
                **{k: v for k, v in metrics[c].items() if not k.startswith("_")}})
        manifest["fbx"].append(entry)
    out = os.path.join(OUT_DIR, "anim_manifest.json")
    with open(out, "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, indent=1)
        handle.write("\n")
    print(f"wrote {_rel(out)}")
    for cid in ids:
        want = sorted((e["param"]["foot"], e["t"]) for e in spec["clips"][cid]["events"]
                      if e["type"] == "Footstep")
        got = sorted((e["foot"], e["t"]) for e in metrics[cid]["detected_footsteps"])
        if want and d_mismatch(want, got, defs[cid].clip):
            print(f"  note: {cid} Footstep events {want} vs detected landings {got}")
    print(f"built {len(ids)} clips in {time.time() - started:.0f}s")
    if problems:
        print("EXPORT PROBLEMS:")
        for p in problems:
            print("  - " + p)
        return 1
    return 0


def d_mismatch(want, got, c) -> bool:
    tol = 2.5 / FPS / max(c.length, 1e-6)
    for foot, t in want:
        if not any(f == foot and (abs(g - t) <= tol or (c.loop and abs(abs(g - t) - 1) <= tol))
                   for f, g in got):
            return True
    return False


def cmd_review(args) -> int:
    from anim_forge import review
    ref, warden, wrig, wmesh, defs, spec, ids = gather(args.only)
    carry = forge.carries(defs, warden)
    stage = review.Stage(wrig, wmesh, warden)
    failures = []
    for cid in ids:
        t0 = time.time()
        d = defs[cid]
        _rf, frames, m = measure(cid, d, ref, warden, defs, spec, carry)
        ts = forge.times(d.clip)
        offsets = [((d.agent_yaw(t) if d.agent_yaw else 0.0), d.review_lift) for t in ts]
        lo, hi = review._bounds(warden, frames, offsets)
        stage.frame(lo, hi, args.tile, args.samples)
        scratch = os.path.join(REVIEW_DIR, f".{cid}_tiles")
        os.makedirs(scratch, exist_ok=True)
        events = spec["clips"][cid]["events"] + [
            e for r in spec["enemies"]["lantern-warden"]["clips"] if r.get("source") == cid
            for e in r.get("events", []) if e not in spec["clips"][cid]["events"]]
        tiles = []
        for k, i in enumerate(review.still_indices(len(frames), d.clip.loop)):
            yaw, lift = offsets[i]
            stage.pose(frames[i], yaw, lift)
            p = stage.shoot(os.path.join(scratch, f"{k}.png"))
            t = i / FPS
            ev = review.event_at(events, t / d.clip.length, d.clip.length)
            tiles.append((p, f"{t:.2f}s ({t / d.clip.length:.2f})" + (("  " + " + ".join(ev)) if ev else "")))
        panel = None
        width = 8 + 4 * args.tile + 24 + 1400
        if "_trace" in m:
            pts = {k: v["max_slide_m"] for k, v in m["_trace"]["points"].items()}
            panel = review.trace_panel(m["_trace"]["trace"], d.clip.speed, 2400, 380,
                                       {k: {"max_slide_m": v} for k, v in pts.items()})
        elif "_haft" in m:
            per, raw, lh = m["_haft"]
            panel = review.haft_panel(per, raw, lh, 2400, 300)
        sc = spec["clips"][cid]
        evs = ", ".join(f"{e['type']}{'(' + (e.get('param') or {}).get('foot', (e.get('param') or {}).get('prop', '')) + ')' if e.get('param') else ''} "
                        f"@{e['t']:.2f}" for e in events) or "none"
        caption = [f"{cid}  ·  {sc['layer']}{' / ' + sc['family'] if sc['family'] else ''}  ·  "
                   f"{d.clip.length:.3f} s, {m['frames']} frames @ {FPS} fps  ·  "
                   f"{'LOOP' if d.clip.loop else 'one-shot'}{'  ·  ADDITIVE' if sc['additive'] else ''}"
                   f"  ·  {sc['fbx']}",
                   f"events (normalized): {evs}",
                   f"on the Lantern Warden: "
                   + ("retargeted from the reference like Humanoid" if d.rig == "ref" else "its own rig")
                   + (f"; carry layer over {len(d.carry_mask)} bones" if d.carry_mask else "")
                   + ("; agent turns 90 deg (rig rotated)" if d.agent_yaw else "")
                   + (f"; lifted {d.review_lift} m (the engine's Levo lift)" if d.review_lift else "")
                   + "; lantern swing simulated and baked (no spring bones)",
                   "  ·  ".join(f"{k} {v}" for k, v in m.items()
                                if not k.startswith("_") and k not in ("frames", "length_s", "loop")),
                   f"notes: {d.clip.notes}"]
        out = review.compose(cid, tiles, caption, panel, os.path.join(REVIEW_DIR, f"{cid}.png"),
                             args.tile)
        import shutil
        shutil.rmtree(scratch, ignore_errors=True)
        msg = f"{_rel(out)}"
        if not args.no_mp4:
            mp4 = review.movie(stage, frames, offsets, events, d.clip.length, d.clip.loop,
                               os.path.join(REVIEW_DIR, f"{cid}.mp4"), resolution=args.mp4_res,
                               samples=args.mp4_samples, label=cid)
            msg += f" + {_rel(mp4)}"
        print(f"  {cid:22s} -> {msg} in {time.time() - t0:.0f}s")
    return 1 if failures else 0


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
    mt = sub.add_parser("metrics")
    mt.add_argument("--only", nargs="*")
    args = ap.parse_args()
    return {"build": cmd_build, "review": cmd_review, "metrics": cmd_metrics}[args.cmd](args)


if __name__ == "__main__":
    sys.exit(main())
