#!/usr/bin/env python3
"""Check Tools/ArtForge/anim_spec.json against the art bible's clip lists (plan A0 audit).

    python3 Tools/ArtForge/anim_spec_check.py            # coverage + shape
    python3 Tools/ArtForge/anim_spec_check.py --built    # also: every a1 clip is in the
                                                         # exported FBX (anim_manifest.json)

Every clip named in docs/art/data/*.json enemies[].rig.animations (a "a / b" entry names
two clips) must appear exactly once under its enemy in the spec, either mapped to an
authored source clip or dropped with a reason. Exits 1 on any problem and prints each.
"""

from __future__ import annotations

import json
import os
import re
import sys
from collections import Counter

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
SPEC = os.path.join(HERE, "anim_spec.json")
MANIFEST = os.path.join(REPO, "Assets", "Models", "ArtBible", "Animations", "anim_manifest.json")
AGES = ["bronze", "high", "late", "powder"]
LAYERS = {"base", "carry", "family", "signature", "hound"}
EVENTS = {"Footstep", "AttackHit", "ProjectileRelease", "PropDetach"}
STATUSES = {"a1", "planned", "dropped"}


def json_clip_names(text: str) -> list[str]:
    head = re.split(r" — ", text, maxsplit=1)[0]
    return [n.strip() for n in head.split("/")]


def check_events(where: str, events: list, problems: list) -> None:
    for e in events:
        if e.get("type") not in EVENTS:
            problems.append(f"{where}: unknown event type {e.get('type')!r}")
        t = e.get("t")
        if not isinstance(t, (int, float)) or not 0.0 <= t <= 1.0:
            problems.append(f"{where}: event {e.get('type')} t={t!r} is not normalized 0..1")
        if e.get("type") == "PropDetach" and not (e.get("param") or {}).get("prop"):
            problems.append(f"{where}: PropDetach without param.prop")


def main() -> int:
    built_check = "--built" in sys.argv
    spec = json.load(open(SPEC, encoding="utf-8"))
    clips, enemies = spec["clips"], spec["enemies"]
    problems: list[str] = []

    for cid, c in clips.items():
        where = f"clips.{cid}"
        if c["layer"] not in LAYERS:
            problems.append(f"{where}: layer {c['layer']!r}")
        if c["status"] not in STATUSES - {"dropped"}:
            problems.append(f"{where}: status {c['status']!r}")
        if not isinstance(c["loop"], bool):
            problems.append(f"{where}: loop must be true/false")
        if not (isinstance(c["length_s"], (int, float)) and c["length_s"] > 0):
            problems.append(f"{where}: length_s {c['length_s']!r}")
        if c["layer"] == "family" and not c.get("family"):
            problems.append(f"{where}: family clip without a family")
        check_events(where, c["events"], problems)

    total = 0
    for age in AGES:
        data = json.load(open(os.path.join(REPO, "docs", "art", "data", f"{age}.json"), encoding="utf-8"))
        for enemy in data["enemies"]:
            slug = enemy["slug"]
            asked = [n for text in enemy["rig"]["animations"] for n in json_clip_names(text)]
            total += len(asked)
            if slug not in enemies:
                problems.append(f"{age}/{slug}: enemy missing from the spec ({len(asked)} clips)")
                continue
            rows = enemies[slug]["clips"]
            have = Counter(r["clip"] for r in rows)
            for name in asked:
                if have[name] == 0:
                    problems.append(f"{slug}/{name}: in the JSON, not in the spec")
            for name, n in have.items():
                if n > 1:
                    problems.append(f"{slug}/{name}: listed {n} times")
                if name not in asked:
                    problems.append(f"{slug}/{name}: in the spec, not in the JSON")
            for r in rows:
                where = f"{slug}/{r['clip']}"
                if r["layer"] not in LAYERS:
                    problems.append(f"{where}: layer {r['layer']!r}")
                if r["status"] not in STATUSES:
                    problems.append(f"{where}: status {r['status']!r}")
                if r["status"] == "dropped":
                    if not r.get("reason"):
                        problems.append(f"{where}: dropped without a reason")
                    continue
                if r.get("source") not in clips:
                    problems.append(f"{where}: source {r.get('source')!r} is not an authored clip")
                    continue
                src = clips[r["source"]]
                if r["layer"] == "carry" and not r.get("carry"):
                    problems.append(f"{where}: carry layer without a carry pose")
                if r["layer"] == "family" and r.get("family") != src.get("family"):
                    problems.append(f"{where}: family {r.get('family')!r} != source's {src.get('family')!r}")
                if not isinstance(r.get("loop"), bool):
                    problems.append(f"{where}: loop must be true/false")
                if r.get("length_s") is None and not src.get("speed_mps"):
                    problems.append(f"{where}: no length and the source is not a gait")
                if src.get("speed_mps") and r.get("agent_speed_mps") is None and r["layer"] != "hound":
                    problems.append(f"{where}: locomotion without agent_speed_mps")
                check_events(where, r.get("events", []), problems)
    for slug in set(enemies) - {e["slug"] for a in AGES for e in json.load(open(
            os.path.join(REPO, "docs", "art", "data", f"{a}.json"), encoding="utf-8"))["enemies"]}:
        problems.append(f"{slug}: in the spec, not in any Age's JSON")

    rows = [r for e in enemies.values() for r in e["clips"]]
    by_layer = Counter(r["layer"] for r in rows)
    by_status = Counter(r["status"] for r in rows)
    print(f"anim_spec.json: {len(rows)} enemy clips for {total} JSON clip names, "
          f"{len(enemies)} enemies, {len(clips)} authored clips")
    print("  by layer:  " + ", ".join(f"{k} {v}" for k, v in sorted(by_layer.items())))
    print("  by status: " + ", ".join(f"{k} {v}" for k, v in sorted(by_status.items())))
    a1 = sorted(c for c, v in clips.items() if v["status"] == "a1")
    print(f"  authored in A1 ({len(a1)}): {', '.join(a1)}")

    if built_check:
        if not os.path.exists(MANIFEST):
            problems.append(f"--built: {os.path.relpath(MANIFEST, REPO)} does not exist (run anim.py build)")
        else:
            man = json.load(open(MANIFEST, encoding="utf-8"))
            built = {c["clip"]: c for f in man["fbx"] for c in f["clips"]}
            for cid in a1:
                if cid not in built:
                    problems.append(f"--built: a1 clip {cid} is not in any exported FBX")
                elif bool(built[cid]["loop"]) != bool(clips[cid]["loop"]):
                    problems.append(f"--built: {cid} loop flag differs from the spec")
            print(f"  built: {len(built)} clips in {len(man['fbx'])} FBX files")

    if problems:
        print(f"\n{len(problems)} problem(s):")
        for p in problems:
            print("  - " + p)
        return 1
    print("coverage OK: every JSON clip is mapped or dropped with a reason")
    return 0


if __name__ == "__main__":
    sys.exit(main())
