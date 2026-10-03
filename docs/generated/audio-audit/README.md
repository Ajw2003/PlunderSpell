# Audio audit

*Generated data from `python3 Tools/AudioForge/audioforge.py audit`; this README is the only
hand-written file here.*

Nobody building these sounds can listen to them (the build runs in a container with no speakers
and no ears), so the audit **measures** the properties that make a game sound harsh or out of place.
It is a proxy for listening, not a substitute: a sound can pass every measure and still be wrong for
its moment. Tell whoever runs AudioForge which sounds feel wrong, and add a rule if the measures
missed them.

| File | What it is |
|---|---|
| `audit.csv` | every built file: its measures and any flags (current build) |
| `audit-before.csv` | the same, for the build before the 2026-09-27 harshness pass |

## The measures

| Measure | Meaning | Real recordings (Kenney CC0, median) |
|---|---|---|
| `rms_db` | average level | depends on the sound |
| `crest_db` | peak over average: high = punchy then quiet; low = dense, loud all the way through | ~19 dB |
| `presence` | share of energy at 2–5 kHz, where the ear is most sensitive and tires fastest | 0–16% (impacts low, cloth and paper higher) |
| `centroid_hz` | the "centre of brightness" | 300–3,500 Hz |
| `air` | share above 10 kHz | 0–3% |
| `tonality_db` | how far the strongest frequency stands above the rest (200 Hz–8 kHz): a sine or buzz reads 25+, a footstep under 20 | 13.6–20.4 dB |
| `attack_ms`, `flat_top` | how fast it starts; how much of it is pinned flat against the peak (clipping) | — |

## The rules

- **harsh / shrill**: bright *and* tonal (whistle, buzz, beep). Bright hiss alone (cloth, paper) is not
  flagged, because it isn't what grates.
- **fizzy**: over 25% of energy above 10 kHz.
- **electronic**: a pure tone where a physical sound belongs (UI, foley, physics, world).
- **distorted**: samples pinned flat at the peak.
- **level**: outside the category's level window (`Tools/AudioForge/forge/categories.py`), unless
  the sound is so spiky that its peak already sits at the −1 dBFS ceiling.
- **too long**: a one-shot longer than its category allows.

Sounds that are bright by nature (glass, coins, bells, fuses, hiss) are exempt from the brightness
rules.

**Calibration.** `audit --refs` runs the timbre rules over the 389 real Kenney recordings. On the
181 organic ones (rpg-audio, impact-sounds) they fire 6 times (3%): two creaks that really do
whistle, two thuds clipped in the source, two fizzy belt buckles. The rules fire more on Kenney's
digital interface beeps, which is intended: the plan wants vellum and wood, not beeps.

## 2026-09-27 result

| Flag | Before | After |
|---|---:|---:|
| level | 452 | 3 |
| too long | 20 | 0 |
| harsh | 6 | 0 |
| distorted | 6 | 0 |
| fizzy | 4 | 0 |
| shrill | 1 | 0 |
| **files flagged** | **465** | **3** |

The three left are small clicks (flint, buckle, key) sitting just under their level window. That's
where a click belongs.

Median level by category went from a flat mix (UI at −17 dB RMS, *louder* than weapons at −19) to a
layered one: UI −31, foley −27, spells −21, creatures −21, physics −21, weapons −19.

**What did not move:** median tonality (20.5 → 20.7 dB) and crest (13.9 → 13.8 dB) stay at the
synthetic edge of the real range. They are dominated by the 405 placeholder guard voices and the
other synthesised placeholders. Tuning can't make formant babble sound like a person; the recordings
and AI takes that replace them will.
