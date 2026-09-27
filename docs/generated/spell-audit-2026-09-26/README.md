# Spell audit, 2026-09-26 (#106)

Every spell cast at a real guard (or door) in a solo Bronze Age raid in the Editor, to answer #106:
"make other spells actually useful; right now Ignis is by far the most useful".

## How it was run

In Play mode, a raid started through the game state (`Lair` then `Playing`, solo), then:

1. `setup.cs` (through `unity command eval_file`) puts the player 5 m from the nearest living guard,
   aimed at its chest, with full mana and health, and records the guard's health and position. It
   also keeps the crosshair on the guard every frame, as a player tracking it would; a keyed cast
   chants for 1.5 s, and a patrolling guard walks out of the aim in that time.
2. The spell is cast through the keyboard casting service
   (`VoiceServiceLocator.Keyboard.SimulateKeyPress`), the same path as pressing its number key.
3. `read.cs` reports the guard's health, how far it moved, its alert state and status effects.
4. `door.cs` does the same for Porta and the nearest closed door. `loot-near.cs` counts loot
   within 4 m (Aurum Voco's coin).

## Results

All at normal volume.

| Spell | Before fixes | After |
|---|---|---|
| Ignis | 60 to 12 health over 5 s (burning) | unchanged |
| Frango | 30 damage and a 2.8 m shove | unchanged |
| Levo | lifted 1.9 m, then back on the floor in one frame, **0 damage** | falls over about 0.5 s, **16.2 damage** |
| Aurum Voco | a 40-worth coin heap at the caster's feet; its clatter woke a sleeping guard | unchanged |
| Velox, Saltus | checked earlier the same day (`docs/generated/dodge-and-leap-2026-09-26/`) | |
| Somnus | **slept 0 guards** when the crosshair was not exactly on the guard | slept a guard that had walked 3.7 m off the crosshair during the chant |
| Porta | **0 doors in the raid**, nothing to open | unchanged; needs #111 |

The Levo trace (guard height every 6 frames, `a` = airborne), after the fix:
`... 2.2a 2.1a 2.0a 1.9a 1.8a 1.6a 1.5a 1.3a 1.1a 0.9a 0.7a 0.4a 0.4`. Before it, the height went
from 1.9 straight to 0.1.

Seen along the way: an idle player 20 s into a raid is found and killed by a guard, the arrival
grace ending as designed.
