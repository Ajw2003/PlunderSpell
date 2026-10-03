"""Measures every built sound for the things ears call harsh or out of place, since the build
machine cannot listen. Compares against real recorded game sounds (library/) as the reference.

    python3 Tools/AudioForge/audioforge.py audit            # writes docs/generated/audio-audit/
    python3 Tools/AudioForge/audioforge.py audit --refs     # also prints the reference profiles

What each measure means, and the line a sound must stay under, is in AUDIT_RULES below and in
docs/generated/audio-audit/README.md. Measures are proxies for listening, not a replacement.
"""

import csv
import sys
from pathlib import Path

import numpy as np
import soundfile as sf

from .build import FORGE, MANIFEST, OUT, REPO, REPORT, read_audio
from .categories import CATEGORY, category

AUDIT_DIR = REPO / "docs" / "generated" / "audio-audit"
SR = 48000

# The ear is most sensitive, and most easily tired, between 2 and 5 kHz ("presence"). Recorded game
# sounds put roughly 10-35% of their energy there (see the reference table the audit prints).
BANDS = {"low": (20, 250), "lowmid": (250, 2000), "presence": (2000, 5000), "high": (5000, 10000),
         "air": (10000, 20000)}


def measure(x):
    x = x.astype(np.float64)
    n = len(x)
    peak = np.max(np.abs(x)) + 1e-12
    rms = np.sqrt(np.mean(x ** 2)) + 1e-12
    frame, hop = 2048, 1024
    if n < frame:
        x = np.pad(x, (0, frame - n))
    frames = np.lib.stride_tricks.sliding_window_view(x, frame)[::hop] * np.hanning(frame)
    power = np.abs(np.fft.rfft(frames, axis=1)) ** 2
    freqs = np.fft.rfftfreq(frame, 1 / SR)
    energy = power.sum(axis=1)
    loud = energy > energy.max() * 1e-3  # ignore near-silent frames
    p = power[loud].sum(axis=0) + 1e-20
    total = p.sum()
    shares = {k: float(p[(freqs >= lo) & (freqs < hi)].sum() / total) for k, (lo, hi) in BANDS.items()}
    centroid = float((p * freqs).sum() / total)
    band = (freqs >= 200) & (freqs <= 8000)
    lp = power[loud][:, band] + 1e-20
    # How far the strongest frequency stands above the band's average, per frame: a sine or a
    # square-wave buzz reads 25+ dB, a footstep, cloth or a crash reads well under 20.
    tonality = float(np.median(10 * np.log10(lp.max(axis=1) / lp.mean(axis=1))))
    env = np.abs(x[:n])
    env = np.convolve(env, np.ones(48) / 48, mode="same")
    ipk = int(np.argmax(env))
    above10 = np.nonzero(env[:ipk + 1] >= 0.1 * env[ipk])[0]
    attack_ms = (ipk - above10[0]) / SR * 1000 if len(above10) else 0.0
    # Clipping is a run of samples pinned flat at the peak, not merely near it (a low sine spends a
    # lot of time near its peak without being clipped).
    y = x[:n]
    pinned = (np.abs(y[1:]) > 0.95 * peak) & (np.abs(np.diff(y)) < 1e-3 * peak)
    flat_top = float(np.mean(pinned[1:] & pinned[:-1])) if n > 3 else 0.0
    return dict(seconds=round(n / SR, 3), peak_db=round(20 * np.log10(peak), 1), rms_db=round(20 * np.log10(rms), 1),
                crest_db=round(20 * np.log10(peak / rms), 1), centroid_hz=round(centroid),
                presence=round(shares["presence"], 3), high=round(shares["high"], 3), air=round(shares["air"], 3),
                low=round(shares["low"], 3), tonality_db=round(tonality, 1), attack_ms=round(attack_ms, 1),
                flat_top=round(flat_top, 4))


TONAL_LIMIT = 26.0  # dB: a pure tone where a physical sound belongs; calibrated in calibrate()
BUZZ_LIMIT = 20.0   # dB: tonal enough that brightness reads as whistle or buzz rather than hiss

# Brightness is expected for these (glass, coins, bells, fuses, hiss): they are bright by nature.
BRIGHT_OK = ("glass", "coin", "chime", "bell", "fuse", "hiss", "shatter", "gold", "sizzle", "extinguish",
             "ricochet", "match_hiss", "loot_highlight", "mana", "wheellock", "rapier", "tell", "listen")


def rules(row, m):
    cat = category(row)
    spec = CATEGORY[cat]
    bright_ok = any(k in row["name"] for k in BRIGHT_OK)
    flags = []
    # Bright hiss (cloth, paper, air) is not what grates; bright *tonal* energy (whistle, buzz, beep)
    # is. So the brightness rules need both.
    tonal = m["tonality_db"] > BUZZ_LIMIT
    if not bright_ok and tonal and m["presence"] > 0.30:
        flags.append(f"harsh: {m['presence']:.0%} of a tonal sound's energy at 2-5 kHz")
    if not bright_ok and tonal and m["centroid_hz"] > 3500:
        flags.append(f"shrill: a tonal sound centred at {m['centroid_hz']} Hz")
    if m["air"] > 0.25 and not bright_ok:
        flags.append(f"fizzy: {m['air']:.0%} of energy above 10 kHz")
    if spec["organic"] and m["tonality_db"] > TONAL_LIMIT and cat in ("ui", "foley", "physics", "world") and not bright_ok:
        flags.append(f"electronic: a pure tone ({m['tonality_db']} dB above its band) where a physical sound belongs")
    if m["flat_top"] > 0.003:
        flags.append(f"distorted: {m['flat_top']:.1%} of samples squashed against the peak")
    if m["attack_ms"] < 0.5 and m["presence"] + m["high"] > 0.45 and cat not in ("ui",) and not bright_ok:
        flags.append("clicky start: full level in under 0.5 ms with a bright top")
    lo, hi = spec["level"]
    # A very spiky sound can sit under its level window with its peak already at the -1 dBFS
    # ceiling; raising it would clip, and its transients carry its loudness. Not a fault.
    peak_limited = m["peak_db"] >= -1.6 and m["rms_db"] < lo
    if row["loop"] != "1" and not (lo <= m["rms_db"] <= hi) and not peak_limited:
        flags.append(f"level: {m['rms_db']} dB RMS, {cat} sits at {lo}..{hi}")
    if spec["max_s"] and row["loop"] != "1" and m["seconds"] > spec["max_s"]:
        flags.append(f"too long: {m['seconds']} s for a {cat} one-shot (max {spec['max_s']})")
    return cat, flags


def reference_profiles():
    groups = {}
    for path in sorted((FORGE / "library").rglob("*.ogg")):
        stem = path.stem.lower()
        key = ("footstep" if "footstep" in stem else "impact" if "impact" in stem else
               "ui" if path.parts[-3] in ("interface-sounds", "ui-audio") else "foley")
        groups.setdefault(key, []).append(measure(read_audio(path)))
    out = {}
    for key, ms in groups.items():
        out[key] = {k: round(float(np.median([m[k] for m in ms])), 3) for k in ("presence", "centroid_hz", "air", "tonality_db")}
        out[key]["files"] = len(ms)
    return out


def calibrate():
    """How often each timbre rule fires on real recordings. A rule that flags real game sounds is
    measuring the wrong thing; each should stay near zero here."""
    counts, total = {}, 0
    for path in sorted((FORGE / "library").rglob("*.ogg")):
        m = measure(read_audio(path))
        row = dict(name=path.stem.lower(), folder="Physics", bus="SFX/World", loop="1")
        if path.parts[-3] in ("interface-sounds", "ui-audio"):
            row.update(folder="UI", bus="UI")
        _, flags = rules(row, m)
        total += 1
        for fl in flags:
            counts[fl.split(":")[0]] = counts.get(fl.split(":")[0], 0) + 1
    return total, counts


def main(argv=None):
    argv = argv or []
    rows = list(csv.DictReader(MANIFEST.open()))
    status = {(r["name"], int(r["variant"])): r["status"] for r in csv.DictReader(REPORT.open())}
    results = []
    for row in rows:
        for v in range(1, int(row["variants"]) + 1):
            path = OUT / row["folder"] / f"{row['name']}_{v:02d}.ogg"
            x, _ = sf.read(str(path), dtype="float32", always_2d=True)
            m = measure(x.mean(axis=1))
            cat, flags = rules(row, m)
            results.append(dict(file=str(path.relative_to(REPO)), name=row["name"], variant=v, category=cat,
                                status=status.get((row["name"], v), "?"), recipe=row["recipe"], **m,
                                flags=" | ".join(flags)))
    AUDIT_DIR.mkdir(parents=True, exist_ok=True)
    with (AUDIT_DIR / "audit.csv").open("w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=list(results[0].keys()))
        writer.writeheader()
        writer.writerows(results)
    flagged = [r for r in results if r["flags"]]
    kinds = {}
    for r in flagged:
        for fl in r["flags"].split(" | "):
            kinds[fl.split(":")[0]] = kinds.get(fl.split(":")[0], 0) + 1
    print(f"{len(results)} files measured, {len(flagged)} flagged")
    for k, v in sorted(kinds.items(), key=lambda kv: -kv[1]):
        print(f"  {k}: {v}")
    if "--refs" in argv:
        for k, v in reference_profiles().items():
            print(f"  reference {k}: {v}")
        total, counts = calibrate()
        print(f"  calibration: of {total} real recordings, rules fired: " +
              (", ".join(f"{k} {v}" for k, v in sorted(counts.items())) or "none"))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
