"""Builds every sound in manifest.csv into Assets/_Project/Audio/.

For each row and variant, in priority order:
  1. a finished file in Tools/AudioForge/final/<name>_NN.{wav,ogg,mp3,flac}: an AI take, a
     recording or a composed track that has replaced the placeholder. Used as-is apart from
     loudness and format.
  2. the row's recipe: Kenney CC0 files and/or synth recipes, mixed.

Writes <folder>/<name>_NN.ogg (48 kHz Vorbis) and build_report.csv, the ledger of where every
shipped file came from and under what licence. The ledger is what answers Steam's AI disclosure.

    python3 -m forge.build              # everything (run from Tools/AudioForge)
    python3 -m forge.build 'sfx_spell_*' # only rows whose name matches
"""

import csv
import fnmatch
import hashlib
import shlex
import sys
from concurrent.futures import ProcessPoolExecutor
from math import gcd
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy import signal

from . import dsp, music
from .categories import CATEGORY, category, target_rms_db
from .recipes import RECIPES

FORGE = Path(__file__).resolve().parent.parent
REPO = FORGE.parent.parent
OUT = REPO / "Assets" / "_Project" / "Audio"
KENNEY = FORGE / "library" / "kenney"
FINAL = FORGE / "final"
MANIFEST = FORGE / "manifest.csv"
REPORT = FORGE / "build_report.csv"

PEAK = 10 ** (-1 / 20)  # -1 dBFS
FINAL_EXTENSIONS = (".wav", ".flac", ".ogg", ".mp3")


def seed_for(name, variant):
    digest = hashlib.sha256(f"{name}#{variant}".encode()).digest()
    return int.from_bytes(digest[:8], "little")


def parse_args(tokens):
    params = {}
    for tok in tokens:
        key, _, value = tok.partition("=")
        for convert in (int, float, str):
            try:
                params[key] = convert(value)
                break
            except ValueError:
                continue
    return params


def read_audio(path):
    x, sr = sf.read(str(path), dtype="float32", always_2d=True)
    x = x.mean(axis=1)
    if sr != dsp.SAMPLE_RATE:
        g = gcd(sr, dsp.SAMPLE_RATE)
        x = signal.resample_poly(x, dsp.SAMPLE_RATE // g, sr // g).astype(np.float32)
    return x


def trim_silence(x, threshold_db=-50):
    threshold = 10 ** (threshold_db / 20) * (np.max(np.abs(x)) or 1)
    loud = np.nonzero(np.abs(x) > threshold)[0]
    return x[loud[0]:loud[-1] + 1] if len(loud) else x


def render_layer(layer, rng, variant):
    kind, _, rest = layer.partition(":")
    tokens = shlex.split(rest)
    target, params = tokens[0], parse_args(tokens[1:])
    gain_db = float(params.pop("gain", 0))
    at = float(params.pop("at", 0))  # start this layer later, in seconds (knock... knock)
    lp = float(params.pop("lp", 0))  # low-pass this layer at lp Hz (tame a fizzy recording)
    shift = float(params.pop("shift", 1.0))
    sources = []
    if kind == "kenney":
        pack, _, pattern = target.partition("/")
        files = sorted((KENNEY / pack / "Audio").glob(pattern + ("" if pattern.endswith(".ogg") else ".ogg")))
        if not files:
            raise FileNotFoundError(f"no Kenney file matches {target}")
        chosen = files[variant % len(files)]
        if variant >= len(files):  # more variants than files: reuse with a pitch nudge
            shift *= 1 + 0.05 * ((variant // len(files)) % 2 * 2 - 1)
        x = trim_silence(read_audio(chosen))
        sources.append(f"kenney/{pack}/{chosen.name}")
    elif kind == "synth":
        x = RECIPES[target](rng, **params)
        sources.append(f"synth:{target}")
    elif kind == "music":
        if target == "raid":
            x = music.raid_layer(rng, params["age"], params["layer"])
        elif target == "theme":
            x = music.theme_loop(rng, params["variant"])
        elif target == "sting":
            x = music.stinger(rng, params["kind"], params.get("age", "lair"))
        else:
            raise KeyError(f"unknown music target {target}")
        sources.append(f"music:{target}")
    else:
        raise KeyError(f"unknown recipe kind {kind!r} in {layer!r}")
    x = dsp.pitch_shift(np.nan_to_num(x.astype(np.float32)), shift)
    if lp > 0:
        x = dsp.lowpass(x, lp, 4)
    if at > 0:
        x = np.concatenate([np.zeros(dsp.seconds(at), dtype=np.float32), x])
    return x * 10 ** (gain_db / 20), sources


def find_final(name, variant):
    for ext in FINAL_EXTENSIONS:
        path = FINAL / f"{name}_{variant + 1:02d}{ext}"
        if path.exists():
            return path
    return None


def loudness(x, row):
    if row["name"].startswith("mus_raid_"):
        layer = {"calm": -24, "stirred": -26, "roused": -24, "huecry": -21}
        return dsp.normalise_rms(x, next((v for k, v in layer.items() if row["name"].endswith("_" + k)), -22))
    if row["loop"] == "1" and row["bus"] == "SFX/Ambience":
        return dsp.normalise_rms(x, -26 if row["spatial"] == "2d" else -22)
    # Everything else sits at its category's level (categories.py), never above -1 dBFS peak.
    return dsp.normalise_rms(x, target_rms_db(row))


def cap_length(x, row):
    """A one-shot longer than its category allows (a UI page-turn running 0.8 s) is faded out."""
    limit = CATEGORY[category(row)]["max_s"]
    if row["loop"] == "1" or not limit or len(x) <= dsp.seconds(limit):
        return x
    return dsp.fade(x[:dsp.seconds(limit)], 0.0, min(0.15, limit / 4))


def write_if_changed(path, x):
    """libsndfile stamps each Ogg stream with a random serial number, so identical audio makes a
    different file every build. Keep the old file when its decoded audio is unchanged, so a
    rebuild only shows up in git where a sound actually changed."""
    tmp = path.with_suffix(".tmp.ogg")
    # Vorbis overshoots the source peak on hard transients (a gunshot by up to +4 dB), so measure
    # the decoded file and turn the source down until the encoded peak sits under -1 dBFS.
    for _ in range(4):
        sf.write(str(tmp), x, dsp.SAMPLE_RATE, format="OGG", subtype="VORBIS")
        encoded_peak = np.max(np.abs(sf.read(str(tmp), dtype="float32")[0]))
        if encoded_peak <= PEAK:
            break
        x = x * (PEAK / encoded_peak) * 0.98
    if path.exists():
        old, _ = sf.read(str(path), dtype="float32")
        new, _ = sf.read(str(tmp), dtype="float32")
        if old.shape == new.shape and np.array_equal(old, new):
            tmp.unlink()
            return
    tmp.replace(path)


def build_one(row, variant):
    name = row["name"]
    rng = np.random.default_rng(seed_for(name, variant))
    final = find_final(name, variant)
    if final is not None:
        x = read_audio(final)
        if row["loop"] != "1":
            x = trim_silence(x)  # recordings arrive with a breath of room noise either side
        sources = [f"final/{final.name}"]
        status = {"A": "ai", "M": "ai-music", "R": "recorded", "C": "composed"}.get(row["final"], "final")
    else:
        parts, sources = [], []
        for layer in row["recipe"].split(" + "):
            x_layer, src = render_layer(layer.strip(), rng, variant)
            parts.append(x_layer)
            sources += src
        x = dsp.mix(*parts)
        uses_kenney = any(s.startswith("kenney/") for s in sources)
        if row["final"] == "G":
            status = "generated"
        elif row["final"] == "L" and uses_kenney:
            status = "library-cc0"
        else:
            status = "placeholder"
    x = np.nan_to_num(x)
    if row["loop"] == "1":
        x = dsp.make_loop(x, min(0.5, len(x) / dsp.SAMPLE_RATE / 4))
    else:
        # Mixed layers are padded to the longest one; drop the dead tail so a one-shot ends
        # when it stops sounding. Loops keep their exact length.
        audible = np.nonzero(np.abs(x) > 10 ** (-60 / 20) * (np.max(np.abs(x)) or 1))[0]
        if len(audible):
            x = x[:audible[-1] + dsp.seconds(0.02)]
        x = dsp.fade(x, 0.001, 0.02)
        x = cap_length(x, row)
    x = loudness(x, row)
    folder = OUT / row["folder"]
    folder.mkdir(parents=True, exist_ok=True)
    path = folder / f"{name}_{variant + 1:02d}.ogg"
    write_if_changed(path, x)
    licence = "CC0 (Kenney)" if any(s.startswith("kenney/") for s in sources) else "original (AudioForge)"
    if final is not None:
        licence = "see final/LICENCES.csv"
    return dict(file=str(path.relative_to(REPO)), name=name, variant=variant + 1, status=status,
                final_source=row["final"], seconds=round(len(x) / dsp.SAMPLE_RATE, 3),
                peak_db=round(20 * np.log10(np.max(np.abs(x)) + 1e-12), 1),
                rms_db=round(20 * np.log10(np.sqrt(np.mean(x ** 2)) + 1e-12), 1),
                sources=" + ".join(sources), licence=licence)


def _job(args):
    row, variant = args
    try:
        return build_one(row, variant)
    except Exception as exc:  # reported per file, then the build fails loudly at the end
        return dict(name=row["name"], variant=variant + 1, error=f"{type(exc).__name__}: {exc}")


def main(argv):
    patterns = argv or ["*"]
    rows = list(csv.DictReader(MANIFEST.open()))
    selected = [r for r in rows if any(fnmatch.fnmatch(r["name"], p) for p in patterns)]
    jobs = [(r, v) for r in selected for v in range(int(r["variants"]))]
    print(f"building {len(jobs)} files from {len(selected)} sounds...", flush=True)
    results = []
    with ProcessPoolExecutor() as pool:
        for i, result in enumerate(pool.map(_job, jobs, chunksize=4), 1):
            results.append(result)
            if i % 100 == 0 or i == len(jobs):
                print(f"  {i}/{len(jobs)}", flush=True)
    errors = [r for r in results if "error" in r]
    for e in errors:
        print(f"ERROR {e['name']}_{e['variant']:02d}: {e['error']}")
    good = [r for r in results if "error" not in r]
    previous = []
    if REPORT.exists() and patterns != ["*"]:
        rebuilt = {(r["name"], str(r["variant"])) for r in good}
        previous = [r for r in csv.DictReader(REPORT.open()) if (r["name"], r["variant"]) not in rebuilt]
    merged = sorted(previous + good, key=lambda r: (r["file"]))
    if merged:
        with REPORT.open("w", newline="") as f:
            writer = csv.DictWriter(f, fieldnames=list(good[0].keys()) if good else list(merged[0].keys()))
            writer.writeheader()
            writer.writerows(merged)
    counts = {}
    for r in good:
        counts[r["status"]] = counts.get(r["status"], 0) + 1
    print(f"built {len(good)} files: " + ", ".join(f"{k} {v}" for k, v in sorted(counts.items())))
    if errors:
        print(f"{len(errors)} file(s) FAILED")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
