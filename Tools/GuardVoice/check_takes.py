"""Checks a folder of guard voice takes against record-lines.json: is every line there, long enough,
audible, and not clipped? Works on the text-to-speech stand-ins and on real recordings alike.

    python Tools/GuardVoice/check_takes.py Tools/GuardVoice/takes-tts          # every Age
    python Tools/GuardVoice/check_takes.py Tools/GuardVoice/takes/powder powder  # one folder, one Age

Exit code 1 when any line is missing or flagged, so it can gate a commit. Standard library only;
reads 16-bit PCM WAV, which is what the recorder page and make_tts_base.ps1 write.
"""

import array
import json
import math
import sys
import wave
from collections import defaultdict
from pathlib import Path

HERE = Path(__file__).resolve().parent
SILENT_PEAK = 0.03
RAIL = 32700
# One or two samples at full scale is a stray peak, not clipping; a real clip sits on the rail for a run.
CLIPPED_MIN_SAMPLES = 10


def read_take(path):
    with wave.open(str(path), "rb") as wav:
        if wav.getsampwidth() != 2:
            raise ValueError(f"{path.name}: expected 16-bit PCM, got {8 * wav.getsampwidth()}-bit")
        channels = wav.getnchannels()
        rate = wav.getframerate()
        frames = array.array("h")
        frames.frombytes(wav.readframes(wav.getnframes()))
    mono = frames[::channels] if channels > 1 else frames
    return mono, rate


def measure(mono, rate):
    if not mono:
        return 0.0, 0.0, -120.0, 0
    peak = max(abs(v) for v in mono) / 32768.0
    rms = math.sqrt(sum(v * v for v in mono) / len(mono)) / 32768.0
    on_rail = sum(1 for v in mono if abs(v) >= RAIL)
    return len(mono) / rate, peak, 20.0 * math.log10(max(rms, 1e-6)), on_rail


def main():
    folder = Path(sys.argv[1]) if len(sys.argv) > 1 else HERE / "takes-tts"
    only_age = sys.argv[2] if len(sys.argv) > 2 else None
    data = json.loads((HERE / "record-lines.json").read_text(encoding="utf-8"))

    problems = 0
    rms_by_loudness = defaultdict(list)
    checked = 0
    for age in data["ages"]:
        if only_age and age["id"] != only_age:
            continue
        for line in age["lines"]:
            path = folder / age["id"] / line["file"] if not only_age else folder / line["file"]
            if not path.exists():
                print(f"MISSING  {line['file']}")
                problems += 1
                continue
            seconds, peak, rms_db, on_rail = measure(*read_take(path))
            checked += 1
            rms_by_loudness[line["loudness"]].append(rms_db)
            flags = []
            if seconds < line["minSeconds"]:
                flags.append(f"short ({seconds:.1f} s, wants {line['minSeconds']} s)")
            if peak < SILENT_PEAK:
                flags.append(f"nearly silent (peak {peak:.3f})")
            if on_rail >= CLIPPED_MIN_SAMPLES:
                flags.append(f"clipped ({on_rail} samples at full scale)")
            if flags:
                problems += 1
                print(f"FLAG     {line['file']}: {'; '.join(flags)}")

    print(f"\nChecked {checked} takes.")
    for loudness in ("quiet", "normal", "loud"):
        values = rms_by_loudness.get(loudness)
        if values:
            print(f"  {loudness:<7} lines: mean level {sum(values) / len(values):6.1f} dBFS over {len(values)} takes")
    print(f"{problems} problem(s).")
    sys.exit(1 if problems else 0)


if __name__ == "__main__":
    main()
