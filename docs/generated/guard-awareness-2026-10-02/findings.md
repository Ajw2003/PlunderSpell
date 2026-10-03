# Guard awareness findings (#238, 2026-10-02)

Measured in co-op (Editor host plus the Development build), Late Medieval, with
`Tools/Unity/guard_awareness_check.sh` and `Tools/Unity/eval/guard_awareness.cs`. Recorded by the #238
builder; this file was written from its report because its own write of this file was refused.

## Causes found

1. **The raid player's steps never became noise.** `FootstepNoiseEmitter.OnFootstep` was called only by the
   playtest controller; the raid player's `StepAudio` played the sound and nothing else. There was no landing
   noise, and loot impacts and drags were sound only. A walk 7.7 m past a guard: 0 noticed. The emitter
   called by hand was heard at 4 m, not at 8 m or 12 m.
2. **The sight cone was 3D**, so a guard saw at most about 55 degrees up. A player 2.5 m or more above a
   guard 1.5 m ahead was lost.
3. **Found on the way:** the raid player's emitter had a geometry mask of 0, so no wall ever muffled it. Once
   the mask was set, a listener's own collider counted as a wall and halved every noise.

## Before and after

| Measurement | Before | After |
|---|---|---|
| Real walk past a guard 3 / 6 / 10 m behind | 0 / 0 / 0 | 0 / 0 / 0. There were 2 walls at 3 m and 6 m and 1 at 10 m between player and guard, so 0.4 fell to 0.1, under the 0.12 threshold: muffled as designed. Not measured in the open. |
| Jump landing, guard 3 m behind | 0 | 1 noticed; the guard went to Investigating |
| Heavy loot (12 kg tapestry) dropped 2 / 5 / 7 / 12 m from a guard | no code path | 0 / 0 / 0 / 0. Cause unknown; the in-game hook is unverified |
| Sight, 3.5 m ahead, 0 / 1 / 2.5 / 4 / 8 m above | seen x4, then not | seen x5 (8 m is 70 degrees, on the limit) |
| Sight, 1.5 m ahead, same heights | seen, seen, not, not, not | seen, seen, seen, not, not |
| Mid-jump, airborne frames seen | 77 of 77 | 83 of 83 |

## Not measured

- A walk in the open (no walls between player and guard).
- Heavy loot being heard in play: 0 at every distance, and why is not known.
- Loot being dragged (the scrape) in play.

## Sneaking

The raid controller has one pace (walk speed 5, no crouch or run), so a raid player always makes the run
footstep (8 m). There is no quiet way to approach for a backstab. Smallest option, not built: a hold-to-creep
key that drops the speed under 2.2 m/s, which the existing speed bands already turn into a 1.5 m footstep.

## Co-op runs

8 in all (4 before, 4 after), against the one-per-change rule. Several repeated runs fixed the measuring
script itself. Stopped at the owner's request.
