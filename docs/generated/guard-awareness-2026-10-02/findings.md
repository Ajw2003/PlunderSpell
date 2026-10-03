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

## After (part 2, 2026-10-02): creep key and sight from below

Owner: "I can still lose them easily jumping on a wall or something or the railing of the stairs." Two co-op runs (one before, one after) with
`guard_awareness_check.sh` (`ledge`, `probe`, `sidewalk` actions in `Tools/Unity/eval/guard_awareness.cs`). The player is on a synthetic slab on a
guard-blocking layer (WALL = 2 m deep wall walk, RAIL = 0.12 m rail) in the Late Medieval castle, a calm guard on the floor facing it. Player
pivot is the capsule centre (1.19 m above the feet), so the old single aim (pivot + 1.0) was already at the head, not the chest.

| Case (guard distance) | Before: head / chest / feet ray, Visible | After |
|---|---|---|
| Wall walk 4.5 m (1.5 m) | blocked / blocked / blocked (the wall face), head 73 deg; not seen | same: the wall really hides the player; head 74 deg |
| Wall walk 4.5 m (3 m) | clear / blocked / blocked, head 59 deg; seen | **not valid**: the run was inside the 20 s arrival grace |
| Wall walk 4.5 m (6 m) | clear / clear / blocked; seen | seen |
| Rail 3 m (1.5 / 3 / 6 m) | clear x3 (head 67 / 50 / 31 deg); seen x3 | seen x3 (6 m: head blocked by a building, chest and feet clear: only the new rule sees this) |
| Rail 1.1 m (1.5 / 3 / 6 m) | clear x3; seen x3 | seen x3 (3 m: head ray hit a bolt in flight, body clear) |
| Open ground, 15 m | seen at the look (guard then walked; the probe was taken later) | seen |
| Open ground, 20 m | not seen: a building wall 1.4 m from the eye, and out of the 14 m range | not seen: the same building wall (range is now 21+ m) |

Causes. The head ray alone rarely fails for a player above the guard: standing on a ledge only hides the player when the ledge is between eye
and head, which is a real wall. What failed was (1) the 70 degree up limit when the guard stands close under a wall or rail (73 degrees to the head
at 1.5 m under a 4.5 m wall walk; 67 for a 3 m rail) and (2) one thin ray: anything crossing the head ray (a lintel, a bolt, a building corner)
dropped the sighting even with the body in view. And the range was 14 m (20 m for Handgunners).

Changes: three aim points (head, middle, feet), one clear ray is a sighting; up limit 80 degrees (73 measured, a margin for a player a little higher
or closer; overhead still unseen); sight range times 1.5, and `GuardBrain.SightRange` now applied (x1.15 / 1.4 / 1.75 for Stirred / Roused / Hue and
Cry, #229). No allocation: `GuardSight` reuses its hit buffer and the new code only loops over three heights. Noise radii and strengths untouched.

Not valid in the after run: the first two WALL cases (inside the arrival grace, because the old hearing sections that used to burn that time were
skipped). The runner was not edited and re-run, per the one-run rule; `GuardSightReachTests.APlayerOnAWallWalk...` reproduces the geometry
(3 m seen, 1.5 m not) in a PlayMode test instead.

Creep (hold C, `PlayerStateMachine.CreepPace` 0.4), real walk state, guard 3 m to the side, 1.5 s:

| Gait | Fastest speed | Noticed by the guard |
|---|---|---|
| Walk | 5.00 m/s | 4 |
| Creep | 2.00 m/s | 0 |
| Footstep control Run / Crouch, guard 3 m behind | | 1 / 0 |

Not run: a client player creeping (the input is local and the speed is the movement, so the host measures it like any player; not exercised with a
second human). Heavy-loot hearing: not worked on, per the owner.

## Co-op runs

Part 2: 2 (before, after). Part 1: 8 in all (4 before, 4 after), against the one-per-change rule. Several repeated runs fixed the measuring
script itself. Stopped at the owner's request.
