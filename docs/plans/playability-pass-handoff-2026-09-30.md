# Handoff — playability pass (2026-09-30)

Written partway through. Plan: [`playability-pass-2026-09-30.md`](playability-pass-2026-09-30.md).
Parent issue #192.

## Where things are

- **Branch:** `claude/playability-fixes`, cut from `claude/check-pr-177` (PR #178: the audio pass, save
  slots and guards overhearing chatter). Pushed. No PR opened yet.
- **Paused, not lost:** the guard mimic prototype on `claude/guard-mimic-prototype`. Its one uncommitted
  edit (an unused `_audioListener` field in `UIRoot.cs`, plus line-ending churn in
  `RuntimePipelineConfig.json`) is `stash@{0}`, "mimic branch WIP". Restore it with
  `git switch claude/guard-mimic-prototype` then `git stash pop`.
- **Untracked, never commit:** `Assets/StreamingAssets/LLM/` and `Assets/StreamingAssets/LLM.meta`.
  These are left behind by the mimic branch, and this branch does not use them.

| Step | Issue | State |
|---|---|---|
| 1. Guards never stand still | #193 (refs #188) | Done; the owner has not played it yet |
| 2. Guards follow sound | #194 (refs #189) | Done; the owner has not played it yet |
| 3. Hue and cry keeps hunting (rough position, owner's choice) | #195 (refs #190) | Done; the owner has not played it yet |
| 4. Saved settings apply at start-up | #181 | Partial, WIP commit 01e40615: re-pushes volumes for 1.5 s after start-up and audio restart, plus a test. Compiles; the test passes, and fails with the fix disabled (2026-10-01). Not yet checked in a real fresh build launch; other settings not covered |
| 5. Lights and shadows fade instead of popping | #196 (refs #165, #166) | Not started |
| 6. Stairs lead somewhere, doors open | #197 (refs #111) | Not started |

Follow-ups filed 2026-10-01: #198 (the guard attack test that fails, possibly from before these changes) and
#199 (the co-op scripts leave a changed line in `ProjectSettings.asset`).

**Update 2026-10-01:** the step 4/5 executor hit the usage limit partway through #181 and never reached #196.

**Update 2026-10-01 (later):**
- #181: done, gap split to #202.
- #198: fixed in the test.
- #199: co-op scripts restore settings byte-for-byte.
- #200: guards are slippery dynamic bodies that ignore each other. Co-op stuck time is 1.7 s of 1811.3; the crush is fixed host-side; the client-side lag case is unchecked.
- New issues: #201 (profiling), #202 (microphone change doesn't reach chatter).

Issues stay open until the owner has tested; closing one always asks.

## What steps 1–3 proved

- **Stuck time:** a co-op run (Editor host plus a Development client built from this checkout), seed
  3508293, 20 guards provoked into a chase, 90 s, measured by `Tools/Unity/coop_guard_check.sh`. Stuck
  guard-seconds fell from 451.0 of 1798.9 (25.1%) to 71.1 of 1768.5 (4.0%). Before, seven guards were
  stuck 63–84 s each; after, the worst guard was stuck 21.5 s. Reports are in
  `docs/generated/playability-2026-09-30/` (`before-*`, `after-*`).
- **Tests:** play-mode `Plunderspell.Tests.Guard*` re-run by the planning session: 55 total, 54 passed,
  1 failed. The failure is `GuardAttackTests.Test_EveryAttackBumpsTheReplicatedSignal` ("No attack yet.
  Expected: 0 But was: 1"). The executor reports it failing on the pre-change code too; nobody has
  re-checked that.
- **Not checked:** nobody has watched the sweep and look-around in a raid, and a player standing on
  furniture has no test.
- **Docs:** the "Guards that keep moving" section in `docs/4-systems/raid.md`.

## Traps for the next session

- **Two Unity Editors run.** One is on `.claude/worktrees/busy-bose-a7647f` (the voice-mimicry
  worktree; not ours, leave it alone). The other is on this checkout, started by the executor at
  18:52 (PID 21920 at the time). The `unity` CLI auto-detects and once sent a whole round of compiles,
  tests and a co-op run to the wrong one. Every call must carry
  `--project-path C:/Users/aj/Desktop/GameDev/PlunderSpell`. `Tools/Unity/pin.sh` does this for the
  Tools/Unity scripts. Confirm first with an eval that returns `Application.dataPath`.
- **Tests:** every test runs in the PlayMode runner; EditMode finds 0.
- **Settings residue:** the co-op scripts leave an extra `preloadedAssets` line in
  `ProjectSettings/ProjectSettings.asset`. Revert it and never commit it. The cleanup bug is in both
  `coop_guard_check.sh` and `coop_carry_check.sh`.
- **Play mode:** check it before any recompile; a recompile during Play has hung the Editor before.

## Next

1. Finish #181 from 01e40615: confirm the real cause with a fresh launch of a build, then cover the
   other saved settings. Then #196.
2. Step 6 (#197): walk real castles for several seeds, list stairs that lead nowhere and doors that will
   not open, then fix what the list shows.
3. Open a PR from `claude/playability-fixes` into `main`. Its body says `Refs #192`, never a closing
   word. Note that it includes PR #178's commits.
4. Later rounds the owner deferred: carrying (#175, #171, #173) and co-op launch (#167, #168).
