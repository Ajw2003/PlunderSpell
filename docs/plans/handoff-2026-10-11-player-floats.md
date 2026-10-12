# Handoff: the player body floats 29 cm above the floor (2026-10-11)

Branch `AjsAgent/player-floats`, cut from `AjsAgent/per-frame-bugs-391` (PR #410, open into
`staging-2026-10-11`, waiting for the owner to merge). Base this branch's PR on staging once #410 is in.

## What is established

- Both bodies, the host's own and its copy of the client's, stand **29 cm** up: capsule bottom
  29.2 cm, foot bones 37.8 cm above the floor. Measured by the new feet check in
  `Tools/Unity/wizard_coop_check.sh` (run label `float-before`, `docs/generated/wizard-362/`).
  Screenshots from before the #391 fixes (14:33) look the same, so it is older than #391.
- Cause: `GroundCheck` / `SnapToGroundHeight` in
  `Assets/_Project/Scripts/Runtime/Player/PlayerStateMachine.cs` take the height as
  `hit.distance - 0.5f`. A SphereCast's `hit.distance` is how far the sphere's centre travelled, so the
  floor is a further `_groundCheckRadius` (0.3) below. The body settled one radius too high.
- `_groundedHeight` is half the capsule height on both prefabs (Player: 2 m capsule, 1.0;
  RaidPlayer: 1.8 m, 0.9), so the capsule was meant to rest on the floor.

## Owner's decision

**Keep the maths fix** (chosen over "lower the model, keep the hover"). The body should really stand
on the floor; whatever the hover was hiding gets fixed properly.

## What is on the branch

- `PlayerStateMachine.cs`: `float currentHeight = hit.distance + _groundCheckRadius - 0.5f;`
- `PlayerIdleState.cs`: comment no longer claims the body hovers.
- `wizard_coop_check.sh`: new `feet` measurement after the idle pose; fails if a capsule is more than
  3 cm off the floor.
- `MovementSpellTests.cs`: **temporary** per-step trace added to
  `Test_AMisfiredSaltusIsAFeebleHopThatLocksTheLegs`'s failure message
  (`TRACE i:height/vy/Grounded/state`). Remove it once the cause is found.

## What is left

1. **The one failing test.** Full PlayMode with the fix: 520 run, 519 passed. Failing:
   `MovementSpellTests.Test_AMisfiredSaltusIsAFeebleHopThatLocksTheLegs`, peak 1.02 m against a 1.0 m
   limit, every run. **Cause found from the trace** (`height/vy/Grounded` per physics step):
   `v0=6.25 0:0.12/6.1/G 1:0.24/5.9/G 2:0.35/5.7/G 3:0.46/5.5/G 4:0.56/5.0/a ... 14:1.02/0.1/a`.
   The launch is 6.25 m/s (the tuning profile asset, not the code default 14 x 0.25), and
   `IsGrounded` stays true for the first 0.46 m of the rise because the SphereCast reaches 1.4 m
   below the body. While "grounded", `ApplyExtraFallGravity` is skipped, so the hop rises higher.
   The old 29 cm hover hid this by leaving the cast's range sooner. **Fix:** set `IsGrounded` only
   when the measured height is within a small tolerance of `_groundedHeight` (a few cm; keep the snap
   working on the way down and on steps), in `GroundCheck`. Then remove the TRACE from the test.
   Fix the cause, never the limit. Check jump, stairs and Saltus tests after: anything reading
   `IsGrounded` (grep it) now sees air sooner.
2. Full PlayMode again (`bash Tools/Unity/run_tests.sh Tests PlayMode`), then EditMode.
3. Co-op: `bash Tools/Unity/wizard_coop_check.sh float-after --build`. The feet check must pass
   (capsule within 3 cm). Look at the screenshots.
4. Because the capsule now touches the floor, check walking feel, stairs and Market tiles in co-op
   (friction on the default physics material, tile edges). This overlaps issue #385 (Market
   collisions, tiles lifting the player).
5. Docs: `docs/4-systems/` player/movement doc on the ground snap, `docs/3-state/ProjectState.md`.
6. Open a PR into `staging-2026-10-11` (`Refs #391` or a new child issue under #380).

## Other open threads from this session

- PR #410 (#391 fixes): co-op check passed (`pr410-*` screenshots); waiting on the owner to merge.
- PR #407 (asset previews): commit `2a0d5da9` pushed all 34 pipeline outputs from CI run
  38186390198; Auto-fix will report whether "rebuild-is-reproducible" passes; merge when green.
- Next per-frame fix in order: #378 redone with events (a `SessionEnding` event published by
  `CoopSession.Leave()` before teardown; no `Update`). See `docs/plans/handoff-2026-10-11-per-frame-fixes.md`.
- PR #387 (creature handoff), the plugin handoff in `Ajw2003/AjsClaudeCodeTools`, issues #381 to #386 and #405 are untouched.
