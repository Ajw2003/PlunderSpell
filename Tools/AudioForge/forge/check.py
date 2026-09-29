"""Checks the built audio against the manifest. Exits non-zero on any failure.

    python3 -m forge.check     (from Tools/AudioForge)

Fails on: a manifest file missing from Assets/_Project/Audio; an .ogg there that no manifest row
owns; a file that is silent, clipped or not 48 kHz; a loop whose end does not meet its start; a
file with no licence in build_report.csv; a finished file in final/ with no row in
final/LICENCES.csv. Then prints how far each sound is from its shipping source.
"""

import csv
import sys
from pathlib import Path

import numpy as np
import soundfile as sf

from .build import FINAL, MANIFEST, OUT, REPO, REPORT, load_picks

SILENT_DB = -60.0
CLIP_DB = -0.5
SEAM_STEP_RATIO = 1.5  # a loop join may step at most this much more than the sound ever does
SEAM_FLOOR = 0.02      # ignore joins smaller than this fraction of peak (inaudible)


def main():
    problems = []
    rows = list(csv.DictReader(MANIFEST.open()))
    report = {r["file"]: r for r in csv.DictReader(REPORT.open())} if REPORT.exists() else {}
    expected = {}
    for row in rows:
        for v in range(1, int(row["variants"]) + 1):
            expected[OUT / row["folder"] / f"{row['name']}_{v:02d}.ogg"] = row

    for path, row in expected.items():
        rel = path.relative_to(REPO).as_posix()
        if not path.exists():
            problems.append(f"missing: {rel}")
            continue
        info = sf.info(str(path))
        if info.samplerate != 48000:
            problems.append(f"not 48 kHz ({info.samplerate}): {rel}")
        x, _ = sf.read(str(path), dtype="float32", always_2d=True)
        x = x.mean(axis=1)
        peak = np.max(np.abs(x))
        rms_db = 20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-12)
        if rms_db < SILENT_DB:
            problems.append(f"silent ({rms_db:.0f} dB RMS): {rel}")
        if 20 * np.log10(peak + 1e-12) > CLIP_DB:
            problems.append(f"too hot ({20 * np.log10(peak):.1f} dBFS peak): {rel}")
        if row["loop"] == "1":
            # A click is a step at the join bigger than any step the sound makes on its own.
            seam = abs(float(x[0]) - float(x[-1]))
            biggest_step = float(np.max(np.abs(np.diff(x))))
            if seam > SEAM_STEP_RATIO * biggest_step and seam > SEAM_FLOOR * peak:
                problems.append(f"loop seam clicks ({seam / biggest_step:.1f}x its largest step): {rel}")
        entry = report.get(rel)
        if entry is None or not entry.get("licence"):
            problems.append(f"no licence in build_report.csv: {rel}")

    for path in OUT.rglob("*.ogg"):
        if path not in expected:
            problems.append(f"not in manifest: {path.relative_to(REPO)}")

    licences = FINAL / "LICENCES.csv"
    listed = {r["file"] for r in csv.DictReader(licences.open())} if licences.exists() else set()
    for path in FINAL.glob("*.*"):
        if path.name not in ("LICENCES.csv", "README.md") and path.name not in listed:
            problems.append(f"final/{path.name} has no row in final/LICENCES.csv")

    statuses = {}
    for r in report.values():
        statuses[r["status"]] = statuses.get(r["status"], 0) + 1
    by_final = {}
    picks = load_picks()  # a pick makes its variant a library sound
    for row in rows:
        for v in range(int(row["variants"])):
            final = "L" if (row["name"], v) in picks else row["final"]
            by_final[final] = by_final.get(final, 0) + 1
    print(f"{len(expected)} files expected, {len(rows)} sounds")
    print("status:          " + ", ".join(f"{k} {v}" for k, v in sorted(statuses.items())))
    print("shipping source: " + ", ".join(f"{k} {v}" for k, v in sorted(by_final.items())) +
          "   (G generated, L library, A AI sound, R recorded, C composed, M AI music)")
    for p in problems:
        print("FAIL " + p)
    print(f"{'FAILED' if problems else 'OK'}: {len(problems)} problem(s)")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
