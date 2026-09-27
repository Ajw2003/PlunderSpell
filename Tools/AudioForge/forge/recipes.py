"""Named sound recipes. The manifest's `recipe` column calls these as `synth:<name> key=value ...`.

Every recipe takes a seeded numpy Generator plus keyword parameters and returns mono float32 at
dsp.SAMPLE_RATE. Recipes jitter their own parameters from the generator, so variant 02 of a sound
differs from variant 01 but rebuilding variant 02 gives the same file every time.
"""

import numpy as np

from . import dsp
from .dsp import SAMPLE_RATE

RECIPES = {}


def recipe(fn):
    RECIPES[fn.__name__] = fn
    return fn


def _j(rng, value, spread=0.08):
    """Jitter a parameter by +/- spread (fractional)."""
    return value * (1 + rng.uniform(-spread, spread))


# --- air, whooshes, breath -----------------------------------------------------------------------

@recipe
def whoosh(rng, dur=0.4, f0=300.0, f1=3000.0, q=1.5, attack=0.45):
    dur = _j(rng, dur)
    x = dsp.swept_bandpass(dsp.pink(rng, dur), _j(rng, f0), _j(rng, f1), q=q)
    return x * dsp.env_ad(dur, attack)


@recipe
def breath(rng, dur=0.5, inhale=1, low=500.0, high=4000.0):
    x = dsp.bandpass(dsp.pink(rng, dur), low, high)
    env = dsp.env_ad(dur, 0.7 if inhale else 0.15, 1.5)
    return x * env * 0.6


@recipe
def hiss(rng, dur=1.0, low=2000.0, high=9000.0, decay=0.0, flutter=0.0):
    x = dsp.bandpass(dsp.pink(rng, dur), low, high)
    if flutter:
        t = dsp.time_axis(dur)
        x *= 0.7 + 0.3 * np.sin(2 * np.pi * flutter * t + rng.uniform(0, 6))
    env = dsp.env_exp(dur, decay, 0.01) if decay else np.ones(len(x), dtype=np.float32)
    return dsp.fade(x * env, 0.01, 0.05)


@recipe
def wind(rng, dur=20.0, low=150.0, high=900.0, gust=0.15):
    x = dsp.bandpass(dsp.pink(rng, dur), low, high)
    t = dsp.time_axis(dur)
    lfo = 0.6 + 0.4 * np.sin(2 * np.pi * gust * t + rng.uniform(0, 6)) * np.sin(
        2 * np.pi * gust * 0.37 * t + rng.uniform(0, 6))
    return x * lfo


@recipe
def brush(rng, dur=0.05, low=900.0, high=3500.0):
    """A fingertip across vellum: a whisper of band-limited noise with a soft start."""
    x = dsp.bandpass(dsp.pink(rng, dur), low, high)
    return x * dsp.env_ad(dur, 0.35, 1.5)


# --- impacts -------------------------------------------------------------------------------------

@recipe
def thump(rng, dur=0.25, freq=90.0, drop=0.5, click=0.3, noise_cut=1500.0, decay=18.0):
    freq = _j(rng, freq)
    body = dsp.sine(dsp.glide(freq * (1 + drop), freq, dur, 0.3), dur) * dsp.env_exp(dur, decay)
    hit = dsp.lowpass(dsp.white(rng, dur), noise_cut) * dsp.env_exp(dur, decay * 3)
    return body + hit * click


@recipe
def tick(rng, dur=0.05, freq=2200.0, decay=120.0, noise=0.3):
    freq = _j(rng, freq, 0.05)
    # Mostly a band of noise around freq (wood), only a little pure tone (which reads as a beep).
    tone = dsp.sine(freq, dur) * dsp.env_exp(dur, decay, 0.001) * 0.35
    n = dsp.bandpass(dsp.pink(rng, dur), freq * 0.5, freq * 1.6) * dsp.env_exp(dur, decay * 1.5, 0.0008)
    return tone + n * (0.6 + noise)


@recipe
def metal_ring(rng, dur=1.2, freq=620.0, decay=3.0, clank=0.5):
    freq = _j(rng, freq)
    ring = dsp.bell(freq, dur, partials=(1.0, 1.47, 2.09, 2.56, 3.9, 5.2), decay=decay)
    hit = dsp.bandpass(dsp.pink(rng, dur), 1200, 6000) * dsp.env_exp(dur, 60, 0.0008)
    return ring * 0.6 + hit * clank


@recipe
def shatter(rng, dur=1.0, brightness=1.0, pieces=40, low=1500.0):
    out = dsp.highpass(dsp.pink(rng, dur), low) * dsp.env_exp(dur, 12, 0.001) * 0.8
    for _ in range(int(pieces)):
        f = rng.uniform(1800, 7000) * brightness
        d = rng.uniform(0.02, 0.08)
        ping = dsp.sine(f, d) * dsp.env_exp(d, rng.uniform(70, 160), 0.0005)
        dsp.place(out, ping, rng.exponential(dur / 5), rng.uniform(0.1, 0.4))
    return dsp.fade(out, 0.0005, 0.05)


@recipe
def crumble(rng, dur=2.0, density=60.0, size=1.0):
    rumble = dsp.lowpass(dsp.brown(rng, dur), 200 * size + 60) * dsp.env_ad(dur, 0.1, 1.5)
    grit = dsp.lowpass(dsp.impulse_train(rng, dur, density), 3000) * dsp.env_ad(dur, 0.2, 1.2)
    stones = np.zeros(dsp.seconds(dur), dtype=np.float32)
    for _ in range(int(density * dur / 4)):
        s = thump(rng, 0.15, rng.uniform(80, 300), 0.3, 0.6, 2500, 30)
        dsp.place(stones, s, rng.uniform(0, dur * 0.8), rng.uniform(0.1, 0.5))
    return rumble * 1.2 + grit * 2.0 + stones


@recipe
def explosion(rng, dur=3.0, size=1.0):
    boom = dsp.sine(dsp.glide(90 * size ** -0.3, 30, dur, 0.2), dur) * dsp.env_exp(dur, 3 / size)
    blast = dsp.lowpass(dsp.white(rng, dur), 1800) * dsp.env_exp(dur, 6 / size, 0.001)
    tail = dsp.lowpass(dsp.brown(rng, dur), 150) * dsp.env_ad(dur, 0.05, 2.0)
    return dsp.saturate(boom * 1.2 + blast + tail * 1.5, 1.3)


@recipe
def gunshot(rng, dur=1.5, size=1.0):
    crack = dsp.bandpass(dsp.pink(rng, dur), 600, 7000) * dsp.env_exp(dur, 45, 0.0008)
    body = explosion(rng, dur, 0.4 * size) * 0.8
    return dsp.saturate(crack * 1.5 + body, 1.4)


# --- creaks, chains, rattles, coins --------------------------------------------------------------

@recipe
def creak(rng, dur=0.8, rate=40.0, pitch=400.0, rise=1.3):
    t_rate = dsp.glide(_j(rng, rate), _j(rng, rate * rise), dur)
    phase = np.cumsum(t_rate) / SAMPLE_RATE
    clicks = (np.diff(np.floor(phase), prepend=0) > 0).astype(np.float32)
    body = dsp.bandpass(clicks, pitch * 0.7, pitch * 2.5) * 6
    return dsp.fade(body * dsp.env_ad(dur, 0.3, 1.0), 0.02, 0.05)


@recipe
def rattle(rng, dur=0.6, density=40.0, freq=3000.0, spread=0.5):
    out = np.zeros(dsp.seconds(dur), dtype=np.float32)
    for _ in range(int(density * dur)):
        f = freq * rng.uniform(1 - spread, 1 + spread)
        d = rng.uniform(0.02, 0.08)
        dsp.place(out, dsp.bell(f, d, decay=40), rng.uniform(0, dur - d), rng.uniform(0.2, 1.0))
    return out


@recipe
def coins(rng, dur=0.8, count=8, spread=0.6):
    out = np.zeros(dsp.seconds(dur), dtype=np.float32)
    for _ in range(int(count)):
        f = rng.uniform(1900, 3600)
        d = rng.uniform(0.06, 0.16)
        ring = dsp.bell(f, d, partials=(1.0, 2.32, 3.1), decay=35) * 0.5
        clink = dsp.bandpass(dsp.pink(rng, d), 2000, 7000) * dsp.env_exp(d, 90, 0.0005)
        dsp.place(out, ring + clink, rng.uniform(0, dur * spread), rng.uniform(0.3, 1.0))
    return out


@recipe
def coin_count(rng, steps=5, step=0.07, base=1400.0, rise=1.06):
    dur = steps * step + 0.4
    out = np.zeros(dsp.seconds(dur), dtype=np.float32)
    for i in range(int(steps)):
        f = base * rise ** i
        dsp.place(out, dsp.bell(f, 0.2, partials=(1.0, 2.32), decay=22), i * step, 0.8)
    return out


@recipe
def coin_drain(rng, steps=6, step=0.09, base=2600.0, fall=0.9):
    return coin_count(rng, steps, step, base, fall)


# --- fire ----------------------------------------------------------------------------------------

@recipe
def crackle(rng, dur=15.0, density=25.0, body=0.5, low=800.0):
    pops = dsp.bandpass(dsp.impulse_train(rng, dur, density, 0.9), low, 6000) * 3
    roar = dsp.lowpass(dsp.brown(rng, dur), 400) * body
    return pops + roar


@recipe
def whumph(rng, dur=0.7, size=1.0):
    air = dsp.swept_bandpass(dsp.pink(rng, dur), 120, 900 * size, q=1.0) * dsp.env_ad(dur, 0.15)
    low = dsp.sine(dsp.glide(60, 110, dur), dur) * dsp.env_ad(dur, 0.2) * 0.6
    return air * 1.5 + low + crackle(rng, dur, 30, 0.0) * dsp.env_ad(dur, 0.4) * 0.5


# --- tones, bells, the lapis voice ---------------------------------------------------------------

@recipe
def chime(rng, notes="880", dur=0.6, gap=0.08, decay=6.0, gold=0):
    freqs = [float(n) for n in str(notes).split("/")]
    total = dur + gap * len(freqs)
    out = np.zeros(dsp.seconds(total), dtype=np.float32)
    partials = (1.0, 2.32, 4.25, 6.63) if gold else (1.0, 2.0, 3.0, 4.2)
    for i, f in enumerate(freqs):
        dsp.place(out, dsp.bell(f, dur, partials=partials, decay=decay), i * gap)
    return out


@recipe
def bell_toll(rng, freq=220.0, dur=4.0, count=1, interval=1.5, decay=0.9, big=1):
    total = dur + interval * (count - 1)
    out = np.zeros(dsp.seconds(total), dtype=np.float32)
    partials = (0.5, 1.0, 1.2, 1.5, 2.0, 2.5, 3.0) if big else (1.0, 2.0, 2.76, 5.4)
    for i in range(int(count)):
        b = dsp.bell(_j(rng, freq, 0.005), dur, partials=partials, decay=decay)
        dsp.place(out, dsp.mix(b, tick(rng, 0.05, freq * 4, 80, 0.8) * 0.5), i * interval)
    return out


@recipe
def gong(rng, freq=110.0, dur=5.0):
    partials = (1.0, 1.52, 2.2, 2.81, 3.43, 4.1, 5.3)
    t = dsp.time_axis(dur)
    bloom = 1 - np.exp(-t * 4)
    return dsp.mix(dsp.bell(freq, dur, partials, decay=0.6) * (0.5 + 0.5 * bloom), thump(rng, 0.4, 60) * 0.6)


@recipe
def horn(rng, freq=110.0, dur=2.5, vibrato=5.0, brass=0.6, bend=0.94):
    t = dsp.time_axis(dur)
    f = dsp.glide(freq * bend, freq, dur, 0.15) * (1 + 0.006 * np.sin(2 * np.pi * vibrato * t))
    x = np.zeros_like(t)
    for k in range(1, 12):
        x += np.sin(2 * np.pi * np.cumsum(f * k) / SAMPLE_RATE) / (k ** (1.6 - brass))
    x = dsp.lowpass(x.astype(np.float32), 1800 + 2000 * brass)
    return dsp.saturate(x * dsp.env_ad(dur, 0.25, 0.8), 1.1)


@recipe
def reed(rng, freq=220.0, dur=0.6, drop=0.75, buzz=1.0):
    """A crumhorn-ish buzzy reed with a sagging pitch: the comic misfire blat."""
    f = dsp.glide(_j(rng, freq, 0.03), freq * drop, dur, 2.0)
    phase = np.cumsum(f) / SAMPLE_RATE
    square = np.sign(np.sin(2 * np.pi * phase)).astype(np.float32)
    x = dsp.lowpass(dsp.bandpass(square, 250, 2500), 1400) * buzz
    return x * dsp.env_ad(dur, 0.05, 0.6) * 0.8


@recipe
def shimmer(rng, dur=1.5, root=440.0, voices=6, attack=0.5, tremolo=6.0, rise=1.0):
    """The lapis layer: detuned glassy sines with tremolo. Every spell sits on this."""
    t = dsp.time_axis(dur)
    out = np.zeros_like(t)
    ratios = (1.0, 1.5, 2.0, 2.52, 3.0, 4.0, 5.04)
    for i in range(int(voices)):
        f = root * ratios[i % len(ratios)] * rng.uniform(0.995, 1.005)
        glide = dsp.glide(f, f * rise, dur)
        out += dsp.sine(glide, dur) * (0.8 ** i)
    trem = 0.75 + 0.25 * np.sin(2 * np.pi * tremolo * t)
    return (out * trem * dsp.env_ad(dur, attack, 1.2)).astype(np.float32)


@recipe
def drone(rng, dur=20.0, freqs="110/165", tremolo=0.2, bright=800.0):
    t = dsp.time_axis(dur)
    out = np.zeros_like(t)
    for f in [float(v) for v in str(freqs).split("/")]:
        out += dsp.saw(f * rng.uniform(0.998, 1.002), dur) + dsp.saw(f * 1.003, dur) * 0.5
    out = dsp.lowpass(out.astype(np.float32), bright)
    return out * (0.8 + 0.2 * np.sin(2 * np.pi * tremolo * t))


@recipe
def hum(rng, dur=4.0, freq=110.0, tremolo=4.0, harm=0.3):
    t = dsp.time_axis(dur)
    x = dsp.sine(freq, dur) + dsp.sine(freq * 2, dur) * harm + dsp.sine(freq * 3.01, dur) * harm / 2
    return x * (0.85 + 0.15 * np.sin(2 * np.pi * tremolo * t))


@recipe
def whine(rng, dur=1.0, f0=400.0, f1=900.0, wobble=7.0, depth=0.04):
    t = dsp.time_axis(dur)
    f = dsp.glide(f0, f1, dur) * (1 + depth * np.sin(2 * np.pi * wobble * t))
    x = dsp.sine(f, dur) + dsp.sine(f * 2, dur) * 0.3
    return x * dsp.env_ad(dur, 0.2, 0.7) * 0.7


@recipe
def boing(rng, dur=0.5, freq=180.0):
    t = dsp.time_axis(dur)
    f = freq * (1 + 0.5 * np.exp(-t * 8) * np.sin(2 * np.pi * 14 * t))
    return dsp.sine(f, dur) * dsp.env_exp(dur, 6)


@recipe
def heartbeat(rng, bpm=70.0, beats=4):
    period = 60 / bpm
    out = np.zeros(dsp.seconds(period * beats), dtype=np.float32)
    for i in range(int(beats)):
        dsp.place(out, thump(rng, 0.2, 50, 0.3, 0.05, 300, 22), i * period)
        dsp.place(out, thump(rng, 0.2, 45, 0.3, 0.05, 300, 25), i * period + 0.22, 0.6)
    return out


# --- ambience pieces -----------------------------------------------------------------------------

@recipe
def drips(rng, dur=20.0, rate=0.8):
    out = np.zeros(dsp.seconds(dur), dtype=np.float32)
    for _ in range(max(1, int(dur * rate))):
        f = rng.uniform(900, 2200)
        d = dsp.sine(dsp.glide(f * 0.6, f, 0.06), 0.06) * dsp.env_exp(0.06, 60)
        dsp.place(out, d, rng.uniform(0, dur - 0.1), rng.uniform(0.2, 0.7))
    return out


@recipe
def crickets(rng, dur=20.0, voices=5):
    out = np.zeros(dsp.seconds(dur), dtype=np.float32)
    t = dsp.time_axis(dur)
    for _ in range(int(voices)):
        f = rng.uniform(3200, 4400)
        rate = rng.uniform(12, 20)
        gate = (np.sin(2 * np.pi * rate * t + rng.uniform(0, 6)) > 0.6).astype(np.float32)
        phrase = (np.sin(2 * np.pi * rng.uniform(0.2, 0.5) * t + rng.uniform(0, 6)) > 0).astype(np.float32)
        out += dsp.sine(f, dur) * dsp.lowpass(gate * phrase, 300) * rng.uniform(0.03, 0.1)
    return out


@recipe
def clock(rng, dur=20.0, bpm=60.0):
    out = np.zeros(dsp.seconds(dur), dtype=np.float32)
    for i in range(int(dur * bpm / 60)):
        f = 1700 if i % 2 else 1450
        dsp.place(out, tick(rng, 0.04, f, 200, 1.0), i * 60 / bpm, 0.5)
    return out


@recipe
def murmur_crowd(rng, dur=20.0, voices=6, far=1):
    out = np.zeros(dsp.seconds(dur), dtype=np.float32)
    for _ in range(int(voices)):
        seg = babble(rng, rng.uniform(1.0, 3.0), rng.uniform(95, 160), "calm")
        dsp.place(out, seg, rng.uniform(0, dur - 3.0), rng.uniform(0.3, 0.8))
    return dsp.lowpass(out, 700 if far else 3000)


# --- voices (placeholders until friends record them) ---------------------------------------------

_VOWELS = {
    "a": (730, 1090, 2440), "e": (530, 1840, 2480), "i": (270, 2290, 3010),
    "o": (570, 840, 2410), "u": (300, 870, 2240),
}


@recipe
def babble(rng, dur=1.0, pitch=120.0, mood="calm"):
    """Formant-synth gibberish: syllables with a pitch contour set by mood."""
    mood_shape = {"calm": (4.0, 0.08, 0.6), "alert": (5.0, 0.25, 0.9), "shout": (6.0, 0.35, 1.0),
                  "pain": (3.0, 0.5, 1.0), "sleep": (1.2, 0.05, 0.4), "grunt": (8.0, 0.15, 1.0)}
    syll_rate, contour, loud = mood_shape.get(str(mood), mood_shape["calm"])
    t = dsp.time_axis(dur)
    f0 = pitch * (1 + contour * np.sin(np.pi * t / dur) + 0.02 * np.sin(2 * np.pi * 5 * t))
    phase = np.cumsum(f0) / SAMPLE_RATE
    buzz = dsp.lowpass((2 * (phase % 1.0) - 1).astype(np.float32), 1800)
    out = np.zeros_like(buzz)
    n_syll = max(1, int(dur * syll_rate))
    edges = np.linspace(0, len(buzz), n_syll + 1).astype(int)
    for i in range(n_syll):
        a, b = edges[i], edges[i + 1]
        v = _VOWELS[rng.choice(list(_VOWELS))]
        seg = dsp.formant(buzz[a:b], v)
        env = np.sin(np.linspace(0, np.pi, b - a)) ** 0.7
        out[a:b] = seg * env
    breathiness = dsp.bandpass(dsp.pink(rng, dur), 1500, 5000) * 0.05
    return dsp.lowpass(dsp.saturate((out + breathiness) * loud, 1.15 if mood == "shout" else 1.0), 4000)


@recipe
def hound(rng, dur=0.4, kind="bark"):
    if kind == "growl":
        rough = 1 + 0.6 * dsp.lowpass(dsp.white(rng, dur), 40) * 8
        return babble(rng, dur, 70, "grunt") * rough.astype(np.float32) * 0.6
    if kind == "pant":
        out = np.zeros(dsp.seconds(dur), dtype=np.float32)
        for i in range(int(dur / 0.25)):
            dsp.place(out, breath(rng, 0.2, i % 2, 700, 3500), i * 0.25, 1.0)
        return out
    if kind == "howl":
        return whine(rng, dur, 380, 520, 5, 0.03) + babble(rng, dur, 400, "sleep") * 0.3
    if kind == "yelp":
        return babble(rng, dur, 450, "pain")
    return babble(rng, dur, 260, "shout") + thump(rng, dur, 150, 0.2, 0.2) * 0.3


@recipe
def snore(rng, dur=3.0, pitch=60.0):
    out = np.zeros(dsp.seconds(dur), dtype=np.float32)
    inhale = dsp.lowpass(dsp.pink(rng, dur * 0.5), 600) * dsp.env_ad(dur * 0.5, 0.7)
    rough = 1 + 0.8 * np.sign(np.sin(2 * np.pi * pitch * dsp.time_axis(dur * 0.5)))
    dsp.place(out, inhale * rough.astype(np.float32) * 0.5, 0.0)
    dsp.place(out, breath(rng, dur * 0.4, 0, 300, 1500), dur * 0.55, 0.6)
    return out


# --- silence for rows that must exist but should not sound ---------------------------------------

@recipe
def silence(rng, dur=0.1):
    return np.zeros(dsp.seconds(dur), dtype=np.float32)
