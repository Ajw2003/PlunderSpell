"""Synthesises the "asleep" lines, which no text-to-speech voice can do: slow deep snores.

Reads record-lines.json and writes a WAV for every line whose direction mentions a snore into
takes-tts/<age>/, next to the text-to-speech clips, under the same file name a human take would get.
Deterministic (seeded by age and line number). Standard library only.

    python Tools/GuardVoice/make_snores.py
"""

import json
import math
import random
import struct
import wave
from pathlib import Path

HERE = Path(__file__).resolve().parent
RATE = 22050
PEAK = 0.6


def one_pole_low_pass(samples, cutoff_hz):
    coefficient = 1.0 - math.exp(-2.0 * math.pi * cutoff_hz / RATE)
    state = 0.0
    out = []
    for value in samples:
        state += coefficient * (value - state)
        out.append(state)
    return out


def envelope(count, attack, release):
    """Rises over the first `attack` fraction, falls over the last `release` fraction."""
    out = []
    for i in range(count):
        t = i / count
        if t < attack:
            out.append(t / attack)
        elif t > 1.0 - release:
            out.append((1.0 - t) / release)
        else:
            out.append(1.0)
    return out


def inhale(rng, seconds):
    """A rising rasp: noise with the top rolled off."""
    count = int(seconds * RATE)
    noise = [rng.gauss(0.0, 1.0) for _ in range(count)]
    band = one_pole_low_pass(noise, 900.0)
    shape = envelope(count, 0.75, 0.25)
    return [0.55 * b * s for b, s in zip(band, shape)]


def exhale(rng, seconds, pitch_hz):
    """The snore itself: a low buzz that flutters, over a little breath noise."""
    count = int(seconds * RATE)
    flutter_hz = rng.uniform(22.0, 32.0)
    phase = 0.0
    buzz = []
    for i in range(count):
        t = i / RATE
        vibrato = 1.0 + 0.04 * math.sin(2.0 * math.pi * 3.0 * t)
        phase += pitch_hz * vibrato / RATE
        saw = 2.0 * (phase % 1.0) - 1.0
        rattle = 0.5 + 0.5 * math.sin(2.0 * math.pi * flutter_hz * t)
        buzz.append(saw * rattle)
    buzz = one_pole_low_pass(buzz, 700.0)
    breath = one_pole_low_pass([rng.gauss(0.0, 1.0) for _ in range(count)], 1200.0)
    shape = envelope(count, 0.3, 0.45)
    return [(1.0 * b + 0.25 * n) * s for b, n, s in zip(buzz, breath, shape)]


def snore(rng, breaths, pitch_hz, pause_seconds):
    out = []
    for _ in range(breaths):
        out += inhale(rng, rng.uniform(0.9, 1.2))
        out += exhale(rng, rng.uniform(1.2, 1.6), pitch_hz * rng.uniform(0.95, 1.05))
        out += [0.0] * int(pause_seconds * RATE * rng.uniform(0.8, 1.2))
    peak = max(abs(v) for v in out) or 1.0
    return [v * PEAK / peak for v in out]


def write_wav(path, samples):
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(RATE)
        wav.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, v)) * 32767)) for v in samples))


def main():
    data = json.loads((HERE / "record-lines.json").read_text(encoding="utf-8"))
    written = 0
    for age in data["ages"]:
        for line in age["lines"]:
            if "snore" not in line["direction"].lower():
                continue
            rng = random.Random(f"{age['id']}-{line['number']}")
            if line["number"] == 1:
                samples = snore(rng, breaths=3, pitch_hz=70.0, pause_seconds=0.9)
            else:
                samples = snore(rng, breaths=4, pitch_hz=85.0, pause_seconds=0.5)
            seconds = len(samples) / RATE
            if seconds < line["minSeconds"]:
                raise SystemExit(f"{line['file']}: made {seconds:.1f} s but the script asks for {line['minSeconds']} s")
            write_wav(HERE / "takes-tts" / age["id"] / line["file"], samples)
            written += 1
    print(f"Wrote {written} snore clips to {HERE / 'takes-tts'}")


if __name__ == "__main__":
    main()
