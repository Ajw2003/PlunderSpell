"""Signal-building blocks for AudioForge's synthesised sounds.

Everything works on mono float32 numpy arrays at SAMPLE_RATE. Every function that uses randomness
takes an explicit numpy Generator, so a sound rebuilt from the same seed is byte-identical.
"""

import numpy as np
from scipy import signal

SAMPLE_RATE = 48000


def seconds(n):
    return int(round(n * SAMPLE_RATE))


def time_axis(duration):
    return np.arange(seconds(duration)) / SAMPLE_RATE


# --- sources -------------------------------------------------------------------------------------

def white(rng, duration):
    return rng.uniform(-1.0, 1.0, seconds(duration)).astype(np.float32)


def pink(rng, duration):
    """Pink noise by spectral shaping: softer and more natural than white for wind and air."""
    n = seconds(duration)
    spectrum = np.fft.rfft(rng.normal(size=n))
    freqs = np.fft.rfftfreq(n, 1.0 / SAMPLE_RATE)
    freqs[0] = freqs[1]
    spectrum /= np.sqrt(freqs)
    out = np.fft.irfft(spectrum, n)
    return normalise_peak(out.astype(np.float32), 1.0)


def brown(rng, duration):
    """Brown noise: rumble, distant fire, sub-bass weight."""
    out = np.cumsum(rng.normal(size=seconds(duration)))
    out = signal.detrend(out)
    return normalise_peak(highpass(out.astype(np.float32), 15), 1.0)


def sine(freq, duration, phase=0.0):
    t = time_axis(duration)
    freq = np.broadcast_to(freq, t.shape) if np.ndim(freq) else freq
    if np.ndim(freq):
        return np.sin(2 * np.pi * np.cumsum(freq) / SAMPLE_RATE + phase).astype(np.float32)
    return np.sin(2 * np.pi * freq * t + phase).astype(np.float32)


def saw(freq, duration):
    """Band-limited-enough sawtooth (additive, harmonics under Nyquist)."""
    t = time_axis(duration)
    out = np.zeros_like(t)
    k = 1
    while k * freq < SAMPLE_RATE / 2.2 and k < 60:
        out += np.sin(2 * np.pi * k * freq * t) / k
        k += 1
    return (out * 0.6).astype(np.float32)


def glide(f_start, f_end, duration, curve=1.0):
    """A frequency contour from f_start to f_end, for sines that sweep."""
    x = np.linspace(0.0, 1.0, seconds(duration)) ** curve
    return (f_start * (f_end / f_start) ** x).astype(np.float32)


def karplus(rng, freq, duration, brightness=0.5, decay=0.996):
    """Plucked string (lyre, lute, harpsichord) by Karplus-Strong."""
    n = seconds(duration)
    period = max(2, int(SAMPLE_RATE / freq))
    buf = rng.uniform(-1, 1, period)
    buf = lowpass(buf.astype(np.float32), 800 + 8000 * brightness) if period > 30 else buf
    blocks = []
    for _ in range(n // period + 1):
        blocks.append(buf.copy())
        buf = decay * 0.5 * (buf + np.roll(buf, -1))
    return np.concatenate(blocks)[:n].astype(np.float32)


def bell(freq, duration, partials=(1.0, 2.0, 2.76, 5.4, 8.93), decay=2.0, brightness=1.0):
    """Inharmonic struck metal: church bells, gongs, the gold timbre."""
    t = time_axis(duration)
    out = np.zeros_like(t)
    for i, ratio in enumerate(partials):
        amp = (0.7 ** i) * (brightness if i > 1 else 1.0)
        out += amp * np.sin(2 * np.pi * freq * ratio * t) * np.exp(-t * decay * (1 + 0.6 * i))
    return out.astype(np.float32)


def impulse_train(rng, duration, rate, jitter=0.5):
    """Random clicks at roughly `rate` per second (crackle, rattle, gravel)."""
    n = seconds(duration)
    out = np.zeros(n, dtype=np.float32)
    count = max(1, int(duration * rate))
    positions = rng.integers(0, n, count)
    out[positions] = rng.uniform(1 - jitter, 1.0, count) * rng.choice([-1, 1], count)
    return out


# --- filters -------------------------------------------------------------------------------------

def _sos(kind, cutoff, order=2):
    nyq = SAMPLE_RATE / 2
    if isinstance(cutoff, (tuple, list)):
        cutoff = [min(max(c, 10), nyq * 0.95) / nyq for c in cutoff]
    else:
        cutoff = min(max(cutoff, 10), nyq * 0.95) / nyq
    return signal.butter(order, cutoff, btype=kind, output="sos")


def lowpass(x, cutoff, order=2):
    return signal.sosfilt(_sos("lowpass", cutoff, order), x).astype(np.float32)


def highpass(x, cutoff, order=2):
    return signal.sosfilt(_sos("highpass", cutoff, order), x).astype(np.float32)


def bandpass(x, low, high, order=2):
    return signal.sosfilt(_sos("bandpass", (low, high), order), x).astype(np.float32)


def swept_bandpass(x, centre_start, centre_end, q=2.0, blocks=64):
    """A bandpass whose centre moves over the sound: the core of every whoosh."""
    out = np.zeros_like(x)
    edges = np.linspace(0, len(x), blocks + 1).astype(int)
    centres = np.geomspace(centre_start, centre_end, blocks)
    fade = 256
    for i in range(blocks):
        a, b = edges[i], edges[i + 1]
        lo, hi = centres[i] / (1 + 1 / q), centres[i] * (1 + 1 / q)
        a0 = max(0, a - fade)
        seg = bandpass(x[a0:b + fade], lo, hi)
        seg = seg[a - a0:a - a0 + (b - a)]
        out[a:b] += seg
    return out


def formant(x, freqs, widths=(80, 90, 120)):
    """Pass a buzz through vowel formants: the placeholder guard voices."""
    out = np.zeros_like(x)
    for f, w in zip(freqs, widths):
        out += bandpass(x, f - w, f + w) * (1.0 if f == freqs[0] else 0.6)
    return out


# --- envelopes and shaping -----------------------------------------------------------------------

def env_exp(duration, decay, attack=0.002):
    t = time_axis(duration)
    env = np.exp(-t * decay)
    a = seconds(attack)
    if a > 0:
        env[:a] *= np.linspace(0, 1, a)
    return env.astype(np.float32)


def env_ad(duration, attack, release_curve=2.0):
    """Rise over `attack` of the length, then fall: swells and whooshes."""
    n = seconds(duration)
    peak = max(1, int(n * attack))
    rise = np.linspace(0, 1, peak) ** 2
    fall = np.linspace(1, 0, n - peak) ** release_curve
    return np.concatenate([rise, fall]).astype(np.float32)


def fade(x, fade_in=0.003, fade_out=0.01):
    x = x.copy()
    a, b = min(seconds(fade_in), len(x) // 2), min(seconds(fade_out), len(x) // 2)
    if a:
        x[:a] *= np.linspace(0, 1, a)
    if b:
        x[-b:] *= np.linspace(1, 0, b)
    return x


def saturate(x, drive=2.0):
    return (np.tanh(x * drive) / np.tanh(drive)).astype(np.float32)


def pitch_shift(x, ratio):
    """Resample-style shift (changes length too), the classic variation trick."""
    if abs(ratio - 1.0) < 1e-4:
        return x
    n_out = max(1, int(len(x) / ratio))
    return signal.resample(x, n_out).astype(np.float32)


def fit(x, n):
    if len(x) >= n:
        return x[:n]
    return np.concatenate([x, np.zeros(n - len(x), dtype=np.float32)])


def mix(*parts):
    n = max(len(p) for p in parts)
    return np.sum([fit(p, n) for p in parts], axis=0).astype(np.float32)


def place(target, sound, at_seconds, gain=1.0):
    start = seconds(at_seconds)
    end = min(len(target), start + len(sound))
    if start < len(target):
        target[start:end] += sound[:end - start] * gain
    return target


def make_loop(x, crossfade=0.5):
    """Fold the tail over the head so the join is inaudible when looped."""
    c = min(seconds(crossfade), len(x) // 3)
    body = x[:-c].copy()
    ramp = np.linspace(0, 1, c, dtype=np.float32)
    body[:c] = body[:c] * ramp + x[-c:] * (1 - ramp)
    return body


def normalise_peak(x, peak):
    m = np.max(np.abs(x)) if len(x) else 0
    return x if m < 1e-9 else (x / m * peak).astype(np.float32)


def normalise_rms(x, dbfs):
    rms = np.sqrt(np.mean(x.astype(np.float64) ** 2))
    if rms < 1e-9:
        return x
    y = x * (10 ** (dbfs / 20) / rms)
    peak = np.max(np.abs(y))
    return (y / peak * 0.891 if peak > 0.891 else y).astype(np.float32)
