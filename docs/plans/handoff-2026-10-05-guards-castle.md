# Handoff: guards, raids, alarm and castle (2026-10-05)

For the next agent. Start here, then `docs/1-landing/README.md` for the project at large. The owner played
the current build on 2026-10-05: "it is finally starting to feel like a game". This pass is about finishing
the remaining guard, raid, alarm and castle fixes, not adding new systems.

## Where things are

- **Branch:** work on `claude/playability-fixes` (pushed, head `325852e9`). The owner made a local branch
  `Staging` at `aaeb9b94` as a reference point for the build they played. **Never commit to `Staging`, move it,
  or push it** unless the owner asks; it exists only on their machine.
- **Unity Editor:** open on this project, idle, not in Play mode (checked 2026-10-05). If `eval.sh` prints
  "No Unity Editor instances found", the Editor is closed: `unity open "C:/Users/aj/Desktop/GameDev/PlunderSpell"`
  and wait for `unity command editor_status --project-path <repo>` to say `ready` (about 30 s).
- **Last full PlayMode suite:** 390/393 on `b0ba9174`; the three failures were `SpellEffectTests`, fixed in
  `325852e9` (SpellEffectTests 18/18). The full suite has not been re-run since that fix.
- **House rules plugin:** updated to 2.52.0 on 2026-10-05. New: a permission prompt nobody answers for 5 minutes is
  refused (never approved); don't retry the same action, route around it only where that doesn't have the same
  effect, carry on, and list it under "Waiting on you" in the final reply. The owner wants to see this work in this
  session, so expect some prompts to go unanswered on purpose.

## Done in the last sessions (all pushed, issues commented, none closed: the owner closes after testing)

| Issue | What | Evidence |
|---|---|---|
| #254 | crypt reachable (Ground plane has a 12 m well hole) | player walked to the crypt floor in Play |
| #255 | guards spawn and patrol on every floor | seed 777: 9 guards, 3 per floor, on their floors |
| #248 | 13 networked doors where zones meet | `Tools/Unity/coop_door_check.sh`: 5/5 checks pass in co-op |
| #256 | stairs = each Age's stairwell extended to keep and crypt | High Medieval walked in a raid; Late/Bronze pass the flood test |
| #257 | all four Ages checked, 11 views each | `docs/generated/ages-check-2026-10-04/` |
| #258 | curtain fires no longer float in Bronze and Late | `eval/light_audit.cs`: 0 floating, 0 wall-less sconces, all Ages |
| #259 | alarm needs witnesses; guards cry for help | AlarmWitnessTests 13/13; **live raid check NOT run yet** |

## To do, in this order (owner chose the scope on 2026-10-05)

1. **#259 live check.** Run the raid check that was interrupted:
   start Play (`unity command editor_play --caller plugin --skill unity-cli --no-banner`), then
   `bash Tools/Unity/eval.sh --file Tools/Unity/eval/start_solo_raid.cs`, wait 4 s,
   `bash Tools/Unity/eval.sh --file Tools/Unity/eval/set_out_seed.cs`, wait ~20 s for guards to settle, then run
   `Tools/Unity/eval/alarm_witness_check.cs` with `__ACTION__` set to `spot` once and `sample` every 2 s for 30 s
   (inline it with `sed "s|__ACTION__|spot|"`, as the co-op scripts do). Write the lines to
   `docs/generated/castle-floors-2026-10-03/alarm-witness-run1.txt` (the file there now is junk from a run whose
   Editor was closed; overwrite it). Expect: one guard sees the player and cries; guards within ~18 m come over;
   the state reaches Roused only once 3 different guards have seen the player. Stop Play after. Then the full
   PlayMode suite once (`bash Tools/Unity/run_tests.sh Plunderspell.Tests PlayMode`, ~10 min, run it in the
   background).
2. **#240** being sent back to the menu mid-raid instead of the death screen. Reproduce first.
3. **#250** tune the stacked castle per floor: fog, guards and loot per floor. Fog is heavy everywhere (see the
   ages-check pictures).
4. **#260** railings round the stair wells (players can drop through). Builder:
   `Tools/AssetPipeline/castle_builders_stairs.py`, forge `Tools/Plunderspell/Forge Stairs`. Re-forging wipes the
   stairs' walk-map tiles: run Tools/Plunderspell/Bake Castle Nav Tiles after, and keep
   `CastleStairPlaceholderTests` (the flood test) passing.
5. **#261** keep guards dip into the slab at the up-stair head (x 6.0 seam), seen once.
6. **#233** the flaky ranged-chase test.
7. **#241** guard ranges: tweak sight and fire range for ordinary guards; only rangers shoot at range; melee guards
   throw only when no living ranger is left in the castle, tracked by the game state. Read the issue; ask the
   owner for numbers if the issue doesn't give them.

Not in scope unless the owner says so: #252 (husk guards, needs a design talk), #234 (alarm speed, a decision for
the owner), roofs, "outside".

## How to work here (lessons that cost time)

- **Before any recompile or test run**, `bash Tools/Unity/eval.sh 'return UnityEditor.EditorApplication.isPlaying + " " + UnityEditor.EditorApplication.isCompiling;'`
  must print `False False`. After a compile, `git diff --ignore-cr-at-eol --stat Assets/_Project/Net/NetworkPrefabs.asset`
  must print no stats.
- **Never hand-edit an open scene's `.unity` file.** On 2026-10-05 a builder edited RaidScene and two other scenes
  on disk; Unity raised a "reload modified scenes?" dialog and froze its main thread until the owner clicked it
  (eval calls time out with "Main thread operation timed out after 5000ms"). Change scene values through the Editor
  (SerializedObject, then save the scene).
- **A builder that looks STALLED** is usually waiting on a long Unity run (full suite ~10 min) or a prompt. Check the
  Editor log tail and `git status` before assuming it died.
- **The castle hears only through its guards now** (#259). A test that used an `EnemyDirector` as the listener must
  put a `Guard` at the listening spot instead; the alarm scores each guard once every 2 s.
- **Checks to reuse:** `Tools/Unity/ages_check.sh` (every Age, light audit + 11 views),
  `Tools/Unity/coop_door_check.sh`, `Tools/Unity/eval/guards_per_floor.cs`, `stair_leg.cs`, `light_audit.cs`.
  Look at the pictures before judging; numbers have missed things.
- **Commits:** on `claude/playability-fixes` only, scoped to your files, message ending `Committed by AJ's agent`,
  push after each. Leave the unrelated dirty files alone (ProjectSettings/*, UI_Verification_Screenshots/*,
  docs/generated/nav-graph-2026-10-02/*, Assets/StreamingAssets/LLM*, CastleDressingSet.asset).
- **Docs:** `docs/5-today/Today.md` gets an entry; `docs/4-systems/alarm.md` and `castle.md` describe the current
  mechanisms; reversals go in `docs/6-decisions/Decisions.md`.
