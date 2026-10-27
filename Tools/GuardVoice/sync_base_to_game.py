#!/usr/bin/env python3
"""Copies the guard base lines into the game's Resources folder.

For every line in record-lines.json the source is the real take in takes/<age>/ when it exists,
otherwise the stand-in in takes-tts/<age>/. Files in Assets/_Project/Resources/GuardVoice/ that are
not in the set are removed. Run again after dropping real takes into takes/<age>/.
Usage: python Tools/GuardVoice/sync_base_to_game.py
"""
import json
import shutil
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
DEST = REPO / "Assets" / "_Project" / "Resources" / "GuardVoice"


def main() -> int:
    lines = json.loads((HERE / "record-lines.json").read_text(encoding="utf-8"))
    wanted = {}
    for age in lines["ages"]:
        for line in age["lines"]:
            wanted[(line["age"], line["file"])] = None

    real = standin = 0
    missing = []
    for (age, name) in sorted(wanted):
        take = HERE / "takes" / age / name
        tts = HERE / "takes-tts" / age / name
        if take.exists():
            source, real = take, real + 1
        elif tts.exists():
            source, standin = tts, standin + 1
        else:
            missing.append(f"{age}/{name}")
            continue
        target = DEST / age / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)

    keep = {DEST / age / name for (age, name) in wanted}
    removed = 0
    for path in DEST.rglob("*"):
        if path.is_file() and path.suffix == ".wav" and path not in keep:
            path.unlink()
            meta = Path(str(path) + ".meta")
            if meta.exists():
                meta.unlink()
            removed += 1

    print(f"{len(wanted)} lines: {real} real takes, {standin} stand-ins, {removed} stale files removed")
    if missing:
        print("MISSING (no take and no stand-in): " + ", ".join(missing))
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
