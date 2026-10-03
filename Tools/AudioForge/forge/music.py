"""Placeholder music: the four adaptive raid layers per Age, the Lair/menu loops, and the stingers.

These are sketches in the right tempo, key and instrument family (docs/plans/audio.md §4.1) so
the MusicDirector can be built and tuned against real stems. All four layers of an Age share one
bar grid and one length, so they stay locked together when crossfaded. They are placeholders:
the adaptive stems are meant to be replaced by composed ones, the non-adaptive tracks by AI music.
"""

import numpy as np

from . import dsp
from .recipes import horn, reed, thump, tick, bell_toll, gong, hiss

MODES = {
    "phrygian": (0, 1, 3, 5, 7, 8, 10),
    "dorian": (0, 2, 3, 5, 7, 9, 10),
    "aeolian": (0, 2, 3, 5, 7, 8, 10),
    "harmonic": (0, 2, 3, 5, 7, 8, 11),
}

AGES = {
    "bronze": dict(bpm=84, root=146.83, mode="phrygian", pluck=0.6, lead="reed", drum="frame",
                   metal="cymbal"),
    "high": dict(bpm=72, root=146.83, mode="dorian", pluck=0.9, lead="choir", drum="tabor",
                 metal="bell"),
    "late": dict(bpm=96, root=220.0, mode="aeolian", pluck=0.5, lead="shawm", drum="side",
                 metal="bell"),
    "powder": dict(bpm=108, root=196.0, mode="harmonic", pluck=1.0, lead="trumpet", drum="timpani",
                   metal="bell"),
    "lair": dict(bpm=66, root=164.81, mode="aeolian", pluck=0.5, lead="gurdy", drum="frame",
                 metal="bell"),
}

BARS = 8


def note_freq(cfg, degree, octave=0):
    steps = MODES[cfg["mode"]]
    octave += degree // len(steps)
    semis = steps[degree % len(steps)] + 12 * octave
    return cfg["root"] * 2 ** (semis / 12)


# --- instruments ---------------------------------------------------------------------------------

def pluck(rng, cfg, freq, dur):
    decay = 0.997 if cfg["pluck"] > 0.8 else 0.994
    return dsp.karplus(rng, freq, dur, cfg["pluck"], decay) * 0.8


def lead(rng, cfg, freq, dur):
    kind = cfg["lead"]
    if kind == "choir":
        t = dsp.time_axis(dur)
        vib = 1 + 0.005 * np.sin(2 * np.pi * 5 * t)
        voices = sum(dsp.saw(freq * d, dur) for d in (0.997, 1.0, 1.004))
        x = dsp.formant(voices.astype(np.float32), (570, 840, 2410)) * vib
        return x * dsp.env_ad(dur, 0.3, 0.8) * 0.8
    if kind == "trumpet":
        return horn(rng, freq, dur, 5.5, 0.9, 0.99) * 0.7
    if kind == "gurdy":
        return reed(rng, freq, dur, 1.0, 0.8) * 0.7
    # reed (aulos) and shawm: buzzy double reeds, the shawm brighter.
    x = reed(rng, freq, dur, 1.0, 1.0)
    return (dsp.highpass(x, 500) if kind == "shawm" else dsp.lowpass(x, 1800)) * 0.7


def drum(rng, cfg, accent=1.0, kind=None):
    kind = kind or cfg["drum"]
    if kind == "frame":
        return thump(rng, 0.35, 80, 0.4, 0.5, 2500, 12) * accent
    if kind == "tabor":
        return dsp.mix(thump(rng, 0.2, 180, 0.2, 0.3, 4000, 25), tick(rng, 0.08, 900, 60, 1.0) * 0.4) * accent
    if kind == "side":
        snare = dsp.bandpass(dsp.white(rng, 0.2), 1500, 7000) * dsp.env_exp(0.2, 25)
        return dsp.mix(snare, thump(rng, 0.15, 200, 0.1, 0.1) * 0.4) * accent
    if kind == "timpani":
        return thump(rng, 0.9, cfg["root"] / 2, 0.05, 0.2, 800, 4) * accent
    return thump(rng, 0.3, 70) * accent


def metal_hit(rng, cfg):
    if cfg["metal"] == "cymbal":
        return hiss(rng, 1.5, 3000, 12000, 3.0) * 0.6
    return bell_toll(rng, note_freq(cfg, 0, 1), 2.5, 1, 1, 1.4, 0) * 0.5


# --- the stems -----------------------------------------------------------------------------------

def _grid(cfg):
    beat = 60.0 / cfg["bpm"]
    length = BARS * 4 * beat
    return beat, length


def _canvas(length):
    # Rendered with a tail, which wraps back onto the start so the loop is seamless.
    return np.zeros(dsp.seconds(length + 4.0), dtype=np.float32)


def _wrap(canvas, length):
    n = dsp.seconds(length)
    out = canvas[:n].copy()
    tail = canvas[n:]
    out[:len(tail)] += tail
    return out


def _melody(rng, length_notes, span=5):
    degree, notes = 0, []
    for _ in range(length_notes):
        degree = int(np.clip(degree + rng.choice([-2, -1, -1, 0, 1, 1, 2]), -1, span))
        notes.append(degree)
    return notes


def layer_calm(rng, cfg):
    beat, length = _grid(cfg)
    c = _canvas(length)
    for bar in range(0, BARS, 4):
        span = beat * 16
        d = dsp.lowpass(dsp.saw(note_freq(cfg, 0, -1), span) + dsp.saw(note_freq(cfg, 4, -1), span) * 0.6, 600)
        dsp.place(c, d * dsp.env_ad(span, 0.3, 0.6), bar * 4 * beat, 0.35)
    for bar in range(BARS):
        if rng.random() < 0.6:
            deg = int(rng.choice([0, 2, 4, 5]))
            dsp.place(c, pluck(rng, cfg, note_freq(cfg, deg), beat * 3), (bar * 4 + rng.integers(0, 3)) * beat, 0.5)
    return _wrap(c, length)


def layer_stirred(rng, cfg):
    beat, length = _grid(cfg)
    c = _canvas(length)
    pattern = [0, 4, 7, 4]
    for i in range(BARS * 8):
        deg = pattern[i % 4] + (0 if (i // 16) % 2 == 0 else -2)
        dsp.place(c, pluck(rng, cfg, note_freq(cfg, deg), beat), i * beat / 2, 0.35)
    for bar in range(BARS):
        dsp.place(c, drum(rng, cfg, 0.5), bar * 4 * beat)
    return _wrap(c, length)


def layer_roused(rng, cfg):
    beat, length = _grid(cfg)
    c = _canvas(length)
    for i in range(BARS * 8):
        accent = 1.0 if i % 4 == 0 else 0.45
        if i % 2 == 0 or rng.random() < 0.3:
            dsp.place(c, drum(rng, cfg, accent), i * beat / 2, 0.7)
    phrase = _melody(rng, 8)
    for rep in range(BARS // 2):
        for j, deg in enumerate(phrase):
            dsp.place(c, lead(rng, cfg, note_freq(cfg, deg), beat * 0.95), (rep * 8 + j) * beat, 0.35)
    return _wrap(c, length)


def layer_huecry(rng, cfg):
    beat, length = _grid(cfg)
    c = _canvas(length)
    for i in range(BARS * 16):
        accent = 1.0 if i % 8 == 0 else (0.6 if i % 4 == 0 else 0.3)
        dsp.place(c, drum(rng, cfg, accent, "side" if cfg["drum"] != "side" else "tabor"), i * beat / 4, 0.5)
    for bar in range(0, BARS, 2):
        dsp.place(c, metal_hit(rng, cfg), bar * 4 * beat, 0.8)
    phrase = _melody(rng, 16, 7)
    for rep in range(BARS // 4):
        for j, deg in enumerate(phrase):
            dsp.place(c, lead(rng, cfg, note_freq(cfg, deg, 1), beat * 0.9), (rep * 16 + j) * beat, 0.35)
    for bar in range(BARS):
        low = horn(rng, note_freq(cfg, 0, -1), beat * 3.5, 4, 0.4, 1.0)
        dsp.place(c, low, bar * 4 * beat, 0.4)
    return _wrap(c, length)


LAYERS = {"calm": layer_calm, "stirred": layer_stirred, "roused": layer_roused, "huecry": layer_huecry}


def raid_layer(rng, age, layer):
    return LAYERS[layer](rng, AGES[age])


def theme_loop(rng, variant="title"):
    """Lair/menu/results loops on the lute-and-hurdy-gurdy palette."""
    cfg = dict(AGES["lair"])
    if variant in ("results_success",):
        cfg["bpm"] = 112
        cfg["mode"] = "dorian"
    if variant in ("lair_after_loss", "results_failure"):
        cfg["bpm"] = 56
    beat, length = _grid(cfg)
    c = _canvas(length)
    span = length
    gurdy = dsp.lowpass(dsp.saw(note_freq(cfg, 0, -1), span) + dsp.saw(note_freq(cfg, 4, -1), span), 900)
    dsp.place(c, gurdy, 0.0, 0.18)
    phrase = _melody(rng, 16, 7)
    density = {"title": 1.0, "credits": 1.0, "lair": 0.6, "lair_after_loss": 0.4,
               "results_success": 1.0, "results_failure": 0.5}.get(variant, 0.8)
    for rep in range(BARS // 4):
        for j, deg in enumerate(phrase):
            if rng.random() < density:
                dsp.place(c, pluck(rng, cfg, note_freq(cfg, deg, 1), beat * 2), (rep * 16 + j) * beat, 0.5)
    if variant in ("title", "credits", "results_success"):
        for bar in range(BARS):
            dsp.place(c, drum(rng, cfg, 0.6), bar * 4 * beat)
            dsp.place(c, drum(rng, cfg, 0.3), (bar * 4 + 2) * beat)
    return _wrap(c, length)


# --- stingers ------------------------------------------------------------------------------------

def stinger(rng, kind, age="lair"):
    cfg = AGES[age]
    if kind == "alarm_stirred":
        return mix_notes(rng, cfg, [(0, -1, 0.0, 2.5)], kind="lead", gain=0.6)
    if kind == "alarm_roused":
        return dsp.mix(mix_notes(rng, cfg, [(0, 0, 0.0, 0.4), (2, 0, 0.35, 0.4), (4, 0, 0.7, 0.4), (3, 0, 1.05, 2.0)],
                                 kind="lead"), drum(rng, cfg, 1.0))
    if kind == "alarm_huecry":
        out = mix_notes(rng, cfg, [(0, 0, 0.0, 2.5), (4, 0, 0.0, 2.5), (7, 0, 0.0, 2.5)], kind="lead")
        out = dsp.mix(out, metal_hit(rng, cfg), gong(rng, cfg["root"] / 2, 4.0) * 0.6)
        for i in range(6):
            dsp.place(out, drum(rng, cfg, 1.0 - i * 0.12), i * 0.12)
        return out
    if kind == "gold_found":
        from .recipes import chime, shimmer
        return dsp.mix(chime(rng, "1318/1568/1976/2637", 1.5, 0.09, 3, 1), shimmer(rng, 2.5, 660, 5, 0.1) * 0.4)
    if kind == "player_down":
        return dsp.mix(drum(rng, cfg, 1.0, "frame"), bell_toll(rng, 110, 4.0, 1, 1, 0.8, 1) * 0.6)
    if kind == "extract_success":
        return mix_notes(rng, AGES["powder"], [(0, 0, 0, 0.3), (2, 0, 0.25, 0.3), (4, 0, 0.5, 0.3), (7, 0, 0.75, 1.8)],
                         kind="lead")
    if kind == "raid_lost":
        cfg_l = AGES["late"]
        return dsp.mix(horn(rng, note_freq(cfg_l, 4, -1), 3.0, 4, 0.5, 1.0),
                       horn(rng, note_freq(cfg_l, 0, -1), 3.0, 4, 0.5, 0.9) * 0.6)
    if kind == "portal_opened":
        from .recipes import shimmer
        return shimmer(rng, 4.0, 220, 7, 0.6, 3.0, 1.5)
    if kind == "portal_warning":
        return dsp.mix(bell_toll(rng, 330, 3.0, 1, 1, 1.2, 0), shimmer_short(rng))
    if kind == "spell_misfire":
        return reed(rng, 233, 0.7, 0.7, 1.2)
    raise KeyError(kind)


def shimmer_short(rng):
    from .recipes import shimmer
    return shimmer(rng, 2.0, 440, 4, 0.05) * 0.4


def mix_notes(rng, cfg, notes, kind="lead", gain=1.0):
    total = max(start + dur for _, _, start, dur in notes) + 1.0
    out = np.zeros(dsp.seconds(total), dtype=np.float32)
    for deg, octv, start, dur in notes:
        f = note_freq(cfg, deg, octv)
        voice = lead(rng, cfg, f, dur) if kind == "lead" else pluck(rng, cfg, f, dur)
        dsp.place(out, voice, start, gain)
    return out
