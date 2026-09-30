# Playability pass — guards, levels and polish (2026-09-30)

Branch `claude/playability-fixes`, cut from `claude/check-pr-177` (PR #178: the audio pass, save
slots and guards overhearing chatter). The guard mimic prototype is paused on
`claude/guard-mimic-prototype`, untouched; its one uncommitted edit (an unused `_audioListener`
field in `UIRoot.cs`) is in `git stash` as "mimic branch WIP".

The owner picked two groups: guards (#188, #189, #190) and levels & polish (#111, #181, #165,
#166). Carrying (#175, #171, #173) and co-op launch (#167, #168) wait for a later round.

## What the code does today (read 2026-09-30)

- **Searching guards park.** `CastleGuard.Act` sends a `Searching` guard to
  `_lastKnownIntruderPosition` and nothing moves that point again, so once there the guard
  stands still for `GuardBrain.SearchPatience` (12 s) — and at the hue and cry, forever, because
  `GuardBrain.NextState` keeps it `Searching` (`GuardBrain.cs:125-130`). That is the "stands
  waiting" in #188 and the "doesn't hunt" in #190.
- **The hue and cry fires once.** `CastleGuard.OnAlarmStateChanged` (`CastleGuard.cs:199`) sends
  each guard within 40 m to the nearest player's position at the moment the alarm flips, then
  never again.
- **Noise only matters to a patrolling guard.** `OnNoiseHeard` (`CastleGuard.cs:450`) sets
  `_investigationTarget`, but only a `Patrolling` guard switches to use it; a `Searching` guard
  ignores what it hears (#189).
- **Nothing notices a stuck agent.** `MoveTo` calls `SetDestination` every frame and trusts it.
  A destination off the NavMesh (a player on a table, a noise inside a wall) or a blocked path
  gives an agent that never arrives and never gives up. A guard whose route has fewer than two
  points "stands its post" by design.
- **Fire lights switch hard.** `FireRules.ShareLights` re-ranks fires by distance and grants
  shadowed / unshadowed / none; a fire crossing a rank boundary snaps its light or shadow on or
  off in one frame (#165, #166).
- **Volumes are read, not applied.** `AudioLevels` loads the saved values, but #181 reports the
  mix only follows once a slider is moved.

## Steps

1. **Guards never stand still (#188).** A stuck watchdog: a guard with a destination that has not
   made progress for about 1.5 s re-paths, and if that fails picks a reachable point near its
   goal. Destinations are snapped to the nearest NavMesh point before use, so an off-mesh target
   is still walkable. A searching guard sweeps a few reachable points around the last-known
   position instead of parking on it. Investigating ends with a short look around, then back to
   the route. Every spawned guard gets a patrol route of at least two points.
2. **Guards follow sound (#189).** A player noise a searching or chasing guard hears moves its
   last-known position to the noise, so noise steers the hunt, not only starts it.
3. **The hue and cry keeps hunting (#190).** While the alarm is at `HueAndCry`, searching guards
   are re-sent to the nearest player's current position every few seconds, not just once.
4. **Saved settings apply at start-up (#181).** Reproduce with a simulated fresh launch, find why
   the mix ignores saved volumes, apply every saved setting at start-up and on change.
5. **Lights and shadows stop popping (#165, #166).** Fade a fire's light and shadow strength in
   and out over a short time when its grant changes, with a margin so a fire near a rank
   boundary does not flicker between grants.
6. **Stairs and doors (#111).** Walk real castles for several seeds, list every stair that leads
   nowhere and every door that will not open, then fix what the list shows.

## How each step is checked

- `GuardBrain` changes get edit-mode tests first; `CastleGuard` behaviour gets play-mode tests
  stepping `Tick` (a guard behind a table reaches a point near it; a searching guard at the hue
  and cry is re-sent when the player moves; a heard noise moves a searching guard).
- One co-op run per change (Editor host + built client), with a logged count of seconds each
  guard spent with a destination and no progress. Before and after numbers go in
  `docs/generated/playability-2026-09-30/`.
- Pop-in: the same camera path captured before and after, frames looked at.
- Settings: change volume, quit, relaunch, hear it without touching the slider.
