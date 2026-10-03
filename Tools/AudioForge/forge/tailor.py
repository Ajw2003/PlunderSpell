"""Library layers: `lib:<root>/<path> [start=s end=s fadein=s fadeout=s xfade=s] [gain= shift= lp= hp= at=]`.

The roots and their licences are in finder/roots.json. A path with spaces is quoted in the recipe:
lib:opengameart/'rpg-sound-pack/RPG Sound Pack/battle/swing3.wav'. The layer composes with kenney: and
synth: layers, the per-category levels and the audit like any other.
"""

import json
from math import gcd
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy import signal

from . import dsp

FORGE = Path(__file__).resolve().parent.parent
ROOTS = FORGE / "finder" / "roots.json"


def roots():
    out = {}
    for r in json.loads(ROOTS.read_text(encoding="utf-8")):
        p = Path(r["path"])
        out[r["name"]] = dict(r, abs=p if p.is_absolute() else FORGE / p)
    return out


def resolve(target):
    """'kenney/rpg-audio/Audio/bookFlip1.ogg' -> (root name, absolute path)."""
    name, _, rel = target.partition("/")
    known = roots()
    if name not in known:
        raise KeyError(f"unknown library root {name!r} in lib:{target}; roots are {sorted(known)}")
    path = known[name]["abs"] / rel
    if not path.is_file():
        raise FileNotFoundError(f"no file {path} for lib:{target}")
    return name, path


def licence(root_name):
    return roots()[root_name]["licence"]


def read_window(path, start=0.0, end=None):
    """Mono float32 at the build's sample rate, cut to [start, end) seconds of the file."""
    with sf.SoundFile(str(path)) as f:
        sr = f.samplerate
        f.seek(min(int(start * sr), f.frames))
        frames = -1 if end is None else max(0, int((end - start) * sr))
        x = f.read(frames, dtype="float32", always_2d=True).mean(axis=1)
    if sr != dsp.SAMPLE_RATE:
        g = gcd(sr, dsp.SAMPLE_RATE)
        x = signal.resample_poly(x, dsp.SAMPLE_RATE // g, sr // g).astype(np.float32)
    return x


def equal_power_loop(x, crossfade=0.5):
    """Fold the tail over the head with an equal-power (sin/cos) crossfade so a picked ambience can loop."""
    c = min(dsp.seconds(crossfade), len(x) // 3)
    if c < 2:
        return x
    body = x[:-c].copy()
    t = np.linspace(0, np.pi / 2, c, dtype=np.float32)
    body[:c] = body[:c] * np.sin(t) + x[-c:] * np.cos(t)
    return body


def render(target, params, trim_silence):
    """The layer's audio and its source label. `params` holds start/end/fadein/fadeout/xfade (popped here)."""
    root, path = resolve(target)
    start = float(params.pop("start", 0.0))
    end = params.pop("end", None)
    end = None if end is None else float(end)
    x = read_window(path, start, end)
    if start == 0.0 and end is None:
        x = trim_silence(x)
    fade_in, fade_out = float(params.pop("fadein", 0.0)), float(params.pop("fadeout", 0.0))
    if fade_in or fade_out:
        x = dsp.fade(x, fade_in, fade_out)
    xfade = float(params.pop("xfade", 0.0))
    if xfade:
        x = equal_power_loop(x, xfade)
    return x, f"lib:{root}/{path.relative_to(roots()[root]['abs']).as_posix()}"
