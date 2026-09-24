# Phase 0 — a playable loop (2026-09-23)

The live plan for Phase 0 of `docs/plans/GitIssues/Priority_Queue.md` (#102 and its children #100,
#14, #101), widened by the user's brief for this session:

> objects have weight and physics like REPO, swing something hit someone deal damage to enemies, to
> friends or to yourself as the player. The damage is such that it can be visibly read either as the
> player receiving damage or dealing damage with some sort of visual feedback. voice casting spells
> actually working, and a full extraction loop playable.

Every checkpoint is verified in the live Editor (Play mode, driven over the Unity CLI) with a
screenshot in `docs/generated/playtest-2026-09-23/`, then committed and pushed.

## What a play session found before any fix (CastleBench, 2026-09-23)

- The player died within ~30 s of standing still, from physics "impact damage" (`Item.cs`
  turns any collision into damage), with **no visible sign at all**: the HUD health bar reads
  `GameServices.PlayerStats`, a second health number the body never writes to.
- On death `PlayerDeadState` reloads the scene; the game state was left on `MainMenu` with no menu
  showing.
- A raid can only end when its 10-minute clock expires or on the undocumented F5 key. The HUD's
  "Extracting…" bar (`HUDScreen`, `ExtractionController`) exists and is never started.
- Voice recognition had never worked in any build (fixed at checkpoint 1, below).

## Checkpoints

1. **Voice casting recognises real speech** — done, `1dae40b`. Real Vosk binaries + model,
   main-thread mic capture, English "heard-as" grammar, speech + number keys together.
   `SpeechRecognitionTests` 20/20.
2. **One damage pathway with a source** (#14) — `Damage.Apply(target, amount, source, point, kind)`
   is the only way anything is hurt; it raises `Damage.Dealt`. Every existing call site moves to it.
3. **Damage you can read** (#14, #12) — driven by `Damage.Dealt`: floating numbers at the hit
   point, a red flash on whatever was hit, a health bar that appears over a damaged enemy (green
   over a friend), and for the local player a red screen-edge flash, a real health bar, and a line
   naming what hurt them ("yourself" included).
4. **Death has a consequence you can see** — dying ends the raid as a loss: a "You died" screen,
   then back to the Lair. No scene reload.
5. **Weight** — a held object is a real physics body pulled toward the hand by a spring, so heavy
   things lag and sag, swinging one carries momentum, and what it hits takes damage from the impact
   (enemies, friends, or the holder's own thrown junk on the rebound). Throws scale with mass.
6. **Extraction you can perform** (#101) — standing in the extraction zone runs the 8 s
   "Extracting…" countdown; stepping out cancels it; finishing banks the loot in the zone and
   returns to the Lair. Only players count as players saved.
7. **Every scene gets a working player** (#100) — `Player.prefab` carries what CastleBench's player
   has: casting, push-to-cast, noise emitters, status effects.
8. **The loop, twice** — Lair → Set Out → raid → carry loot → extract → Lair → Set Out again, in
   the Editor, then in a standalone build.

## Not in scope this session

Phase 1+ polish (#51 camera shake/hit-stop, #52 wind-up animation, #47 loudness meter) unless a
checkpoint above needs it to be readable. Multiplayer friendly-fire is built on the same pathway
but can only be verified with a second client, which this session does not have.
