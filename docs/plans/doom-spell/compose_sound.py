"""Composes the Doom spell prototype sound from CC0 recordings already in the repo plus synthesis.

Run from anywhere:  python3 docs/plans/doom-spell/compose_sound.py
Writes doom_spell_full.ogg (the whole 20 s cast, timed to prototype.html) and doom_spell_full.wav
next to this file, and prints the peak and loudness of the result.

Every recorded layer is CC0: Kenney (impact-sounds, sci-fi-sounds) and rubberduck's 80 CC0 RPG SFX
from OpenGameArt, all under Tools/AudioForge/library/. The rest is synthesised here.

The timeline below must match TIMELINE in prototype.html.
"""
from pathlib import Path

import numpy as np
import soundfile as sf
from scipy.signal import butter, fftconvolve, resample, sosfilt

HERE = Path(__file__).resolve().parent
LIB = HERE.parents[2] / "Tools" / "AudioForge" / "library"
RATE = 48000
LENGTH_S = 21.0

# Seconds. Same names and values as TIMELINE in prototype.html.
CHARGE = 0.0       # caster's hands blacken, the word is spoken
RELEASE = 3.0      # the small red-black orb leaves the hands and floats up
OPEN = 5.0         # the orb tears open into a red black hole above the boss
INHALE = 14.2      # everything is sucked in, sound included
SILENCE = 15.0     # dead air
DETONATE = 15.4    # the hole collapses onto the boss


def load(relative: str, ratio: float = 1.0, reverse: bool = False) -> np.ndarray:
    """Stereo float array at RATE, pitched by ratio (0.5 = an octave down and twice as long)."""
    audio, rate = sf.read(LIB / relative, always_2d=True, dtype="float32")
    if audio.shape[1] == 1:
        audio = np.repeat(audio, 2, axis=1)
    target = int(len(audio) * RATE / rate / ratio)
    audio = resample(audio, target, axis=0)
    return audio[::-1] if reverse else audio


def lowpass(audio: np.ndarray, hz: float) -> np.ndarray:
    return sosfilt(butter(4, hz, "low", fs=RATE, output="sos"), audio, axis=0)


def highpass(audio: np.ndarray, hz: float) -> np.ndarray:
    return sosfilt(butter(2, hz, "high", fs=RATE, output="sos"), audio, axis=0)


def fade(audio: np.ndarray, fade_in: float = 0.0, fade_out: float = 0.0) -> np.ndarray:
    out = audio.copy()
    n_in, n_out = int(fade_in * RATE), int(fade_out * RATE)
    if n_in:
        out[:n_in] *= np.linspace(0, 1, n_in)[:, None] ** 2
    if n_out:
        out[-n_out:] *= np.linspace(1, 0, n_out)[:, None] ** 2
    return out


class Mix:
    def __init__(self) -> None:
        self.dry = np.zeros((int(LENGTH_S * RATE), 2))
        self.wet = np.zeros_like(self.dry)

    def add(self, audio: np.ndarray, at: float, db: float = 0.0, reverb: float = 0.4) -> None:
        start = int(at * RATE)
        end = min(start + len(audio), len(self.dry))
        clip = audio[: end - start] * 10 ** (db / 20)
        self.dry[start:end] += clip
        self.wet[start:end] += clip * reverb


def tone(seconds: float, start_hz: float, end_hz: float, harmonics=(1.0,)) -> np.ndarray:
    t = np.arange(int(seconds * RATE)) / RATE
    hz = start_hz * (end_hz / start_hz) ** (t / seconds)
    phase = 2 * np.pi * np.cumsum(hz) / RATE
    wave = sum(gain * np.sin(phase * (i + 1)) for i, gain in enumerate(harmonics))
    return np.stack([wave, wave], axis=1)


def heartbeat(depth_hz: float = 48) -> np.ndarray:
    """A lub-dub thump: two decaying sub sines."""
    def thump(hz: float, seconds: float) -> np.ndarray:
        t = np.arange(int(seconds * RATE)) / RATE
        wave = np.sin(2 * np.pi * hz * t * (1 - 0.35 * t / seconds)) * np.exp(-t * 14)
        return wave
    lub, dub = thump(depth_hz, 0.35), thump(depth_hz * 0.85, 0.4) * 0.7
    out = np.zeros(int(0.8 * RATE))
    out[: len(lub)] += lub
    gap = int(0.22 * RATE)
    out[gap : gap + len(dub)] += dub
    return np.stack([out, out], axis=1)


def falling_shepard(seconds: float) -> np.ndarray:
    """An endlessly falling tone (Shepard-Risset glissando): the ear never hears it reach bottom."""
    t = np.arange(int(seconds * RATE)) / RATE
    out = np.zeros_like(t)
    octaves, cycle = 6, 7.0
    for k in range(octaves):
        position = ((k - t / cycle) % octaves) / octaves          # 0..1, sliding down
        hz = 40 * 2 ** (position * octaves)
        weight = np.sin(np.pi * position) ** 2                     # loud mid-range, silent at edges
        out += weight * np.sin(2 * np.pi * np.cumsum(hz) / RATE)
    out /= octaves
    left = out
    right = np.roll(out, int(0.011 * RATE))
    return np.stack([left, right], axis=1)


def noise_swell(seconds: float, rng: np.random.Generator) -> np.ndarray:
    """Reversed-cymbal style inhale: band-limited noise rising steeply in level and brightness."""
    n = int(seconds * RATE)
    noise = rng.standard_normal((n, 2))
    env = np.linspace(0, 1, n)[:, None] ** 4
    dark = lowpass(noise, 900) * (1 - env)
    bright = highpass(noise, 1800) * env
    return (dark + bright) * env


def hall_impulse(seconds: float, rng: np.random.Generator) -> np.ndarray:
    """A synthetic cathedral impulse response: decorrelated decaying noise, darker as it fades."""
    n = int(seconds * RATE)
    t = np.arange(n) / RATE
    noise = rng.standard_normal((n, 2)) * np.exp(-t * 6.9 / seconds)[:, None]
    early = lowpass(noise, 6000)
    late = lowpass(noise, 1400)
    blend = np.clip(t / (seconds * 0.4), 0, 1)[:, None]
    impulse = early * (1 - blend) + late * blend
    return impulse / np.sqrt(np.sum(impulse ** 2, axis=0))


def render(mix: "Mix", rng: np.random.Generator) -> np.ndarray:
    wet = fftconvolve(mix.wet, hall_impulse(5.5, rng), axes=0)[: len(mix.dry)]
    return highpass(mix.dry + wet * 0.55, 22)                      # nothing the speakers can't move


def compose() -> np.ndarray:
    rng = np.random.default_rng(1666)
    mix = Mix()
    bell = "kenney/impact-sounds/Audio/impactBell_heavy_001.ogg"

    # --- Charge: a funeral toll and a drone rising out of the floor.
    mix.add(lowpass(load(bell, 0.5), 3000), CHARGE + 0.25, db=-4, reverb=0.9)
    drone = load("kenney/sci-fi-sounds/Audio/spaceEngineLow_000.ogg", 0.45)
    drone = np.concatenate([drone, drone[::-1], drone])            # long enough to sit under the hole
    drone = lowpass(drone, 700)
    drone_end = INHALE + 0.6
    drone = fade(drone[: int((drone_end - CHARGE) * RATE)], fade_in=3.0, fade_out=0.6)
    mix.add(drone, CHARGE, db=-12, reverb=0.3)
    mix.add(fade(tone(RELEASE - CHARGE, 28, 36, (1, 0.3)), 1.5, 0.3), CHARGE, db=-10, reverb=0.1)

    # --- Release: second toll, the orb lifts away with a low whoosh, the heart starts.
    mix.add(lowpass(load(bell, 0.42), 2500), RELEASE, db=-3, reverb=0.9)
    mix.add(lowpass(load("kenney/sci-fi-sounds/Audio/forceField_000.ogg", 0.35), 1800), RELEASE, db=-8)
    for beat in (RELEASE + 0.5, RELEASE + 1.15, RELEASE + 1.65):
        mix.add(heartbeat(), beat, db=-2, reverb=0.15)

    # --- Open: the tear. Third, lowest toll, a pitched-down roar dragged backwards into a boom.
    mix.add(lowpass(load(bell, 0.33), 2000), OPEN, db=-1, reverb=1.0)
    mix.add(load("opengameart/80-cc0-rpg-sfx/creature_roar_03.ogg", 0.4, reverse=True), OPEN - 2.9, db=-9)
    mix.add(load("kenney/sci-fi-sounds/Audio/lowFrequency_explosion_000.ogg", 0.6), OPEN, db=-3)
    mix.add(load("opengameart/80-cc0-rpg-sfx/creature_roar_02.ogg", 0.32), OPEN + 0.1, db=-6, reverb=0.7)

    # --- The hole holds: endless falling tone, sub tightening upward, chains being dragged in,
    #     the heart racing, a toll every few seconds.
    hold = INHALE - OPEN
    mix.add(fade(falling_shepard(hold + 0.8), 1.5, 0.8), OPEN, db=-17, reverb=0.5)
    mix.add(fade(tone(hold + 0.8, 30, 46, (1, 0.5, 0.2)), 0.8, 0.8), OPEN, db=-9, reverb=0.1)
    howl = load("kenney/sci-fi-sounds/Audio/spaceEngineLarge_000.ogg", 0.3)
    howl = np.concatenate([howl, howl[::-1]])[: int((hold + 0.8) * RATE)]
    mix.add(fade(highpass(lowpass(howl, 2200), 120), 2.0, 0.8), OPEN, db=-16, reverb=0.6)
    for at, which, ratio in ((6.2, 3, 0.6), (7.4, 1, 0.55), (8.9, 2, 0.7), (10.1, 3, 0.5),
                             (11.0, 1, 0.65), (12.3, 2, 0.55), (13.1, 3, 0.6)):
        mix.add(load(f"opengameart/80-cc0-rpg-sfx/chain_0{which}.ogg", ratio), at, db=-15, reverb=0.8)
    beat, gap = OPEN + 1.0, 1.1
    while beat < INHALE:
        mix.add(heartbeat(52), beat, db=-4, reverb=0.15)
        beat += gap
        gap = max(0.42, gap * 0.9)
    for toll_at in (8.5, 12.0):
        mix.add(lowpass(load(bell, 0.3), 1800), toll_at, db=-6, reverb=1.0)

    # --- Inhale: a swell and a backwards bell pull everything toward silence.
    mix.add(noise_swell(SILENCE - INHALE, rng), INHALE, db=-10, reverb=0.2)
    reverse_bell = lowpass(load(bell, 0.35, reverse=True), 2500)
    mix.add(reverse_bell, SILENCE - len(reverse_bell) / RATE, db=-4, reverb=0.3)

    body = render(mix, rng)
    # Master automation on everything before the blast: the hole swells from a third of full level
    # up to full by the inhale, then the whole hall (reverb included) is pulled down to near silence.
    t = np.arange(len(body)) / RATE
    swell = np.interp(t, [0, OPEN, INHALE - 0.4, SILENCE - 0.15, DETONATE - 0.02, LENGTH_S],
                      [0.55, 0.45, 1.0, 0.02, 0.015, 0.015])
    body *= swell[:, None]

    # --- Detonate: the collapse, and the last toll ringing out across an empty hall.
    finale = Mix()
    finale.add(load("kenney/sci-fi-sounds/Audio/lowFrequency_explosion_000.ogg", 0.7), DETONATE, db=0, reverb=0.6)
    finale.add(load("kenney/sci-fi-sounds/Audio/lowFrequency_explosion_001.ogg", 0.5), DETONATE + 0.05, db=-2)
    finale.add(load("kenney/sci-fi-sounds/Audio/explosionCrunch_000.ogg", 0.55), DETONATE, db=-5, reverb=0.7)
    finale.add(fade(tone(2.5, 55, 24, (1, 0.4)), 0.0, 2.0), DETONATE, db=-2, reverb=0.2)
    finale.add(lowpass(load(bell, 0.25), 1600), DETONATE + 0.35, db=-6, reverb=1.2)
    for at in (DETONATE + 0.6, DETONATE + 0.9, DETONATE + 1.3):
        stones = f"opengameart/80-cc0-rpg-sfx/stones_0{int(at * 10) % 4 + 1}.ogg"
        finale.add(load(stones, 0.7), at, db=-14, reverb=0.8)

    out = body * 0.42 + render(finale, rng)
    out = np.tanh(out * 1.2) / 1.2                                 # soft-limit the peaks, no hard clip
    peak = np.max(np.abs(out))
    out *= 0.89 / peak                                             # -1 dBFS
    tail = int(0.8 * RATE)
    out[-tail:] *= np.linspace(1, 0, tail)[:, None]
    return out


def main() -> None:
    out = compose()
    sf.write(HERE / "doom_spell_full.wav", out, RATE, subtype="PCM_16")
    sf.write(HERE / "doom_spell_full.ogg", out, RATE, format="OGG", subtype="VORBIS")
    rms_db = 20 * np.log10(np.sqrt(np.mean(out ** 2)))
    peak_db = 20 * np.log10(np.max(np.abs(out)))
    print(f"wrote doom_spell_full.ogg/.wav  {len(out) / RATE:.1f} s  peak {peak_db:.1f} dBFS  rms {rms_db:.1f} dBFS")


if __name__ == "__main__":
    main()
