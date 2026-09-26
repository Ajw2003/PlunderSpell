# Integration summary — 2026-09-15/16

Staging branch: `integration/staging-2026-09-15` (branched from `plunderspell-dev`, never
touched `main` or `plunderspell-dev` directly, pushed to `origin` after every step).

## Branches processed

Of the 14 branches originally in scope (all except `feature/Owen/PCG`, excluded by request),
**7 were already fully contained in `plunderspell-dev`** (confirmed with
`git merge-base --is-ancestor`, not just a commit-count heuristic) and needed no action:
`origin/develop`, `claude/steam-multiplayer-framework-xia7ch`, `origin/feature/m0-gravity-removal`,
`origin/feature/m1-voice-casting`, `origin/feature/m2-castle-gen`,
`origin/feature/m2-loot-acoustics`, `origin/feature/m3-extraction-lair`.

**7 branches had real, unmerged commits and were merged in this order:**

| # | Branch | Commits | Conflicts | Real bugs found & fixed |
|---|--------|---------|-----------|--------------------------|
| 1 | `claude/plunderspell-asset-pipeline-2cx4w4` | 2 | 0 | — |
| 2 | `origin/claude/plunderspell-enemy-assets-nsfuqn` | 3 | 0 | — |
| 3 | `origin/claude/plunderspell-ui-system-ur2o22` | 3 | 0 (but see below) | Duplicate `.asmdef` in one folder; PlayMode test incompatible with `-batchmode` |
| 4 | `claude/inspiring-davinci-83909p` | 3 | 8 | HEAD referenced a method that no longer existed anywhere in the codebase |
| 5 | `origin/claude/plunderspell-fable-prompt-iutej1` | 1 | 0 | — (docs only) |
| 6 | `claude/level-generation-visual-pqdsr8` | 4 | 1 | — (doc-only conflict) |
| 7 | `claude/plunderspell-core-gameplay-o2av06` | 9 | 4 | Duplicate `.asmdef`; two assemblies missing `PurrNet.Runtime`/`RogueAi.Core`; two real test bugs |

## Every conflict, and how it was actually resolved (not just "kept ours")

Full narrative detail is in `progress.md` (append-only log, one section per branch). Highlights:

- **Never resolved a conflict by pattern ("comments differ so keep ours") without checking the
  live codebase first.** Two cases where that would have shipped broken code:
  - Branch 4: `PlayerJumpState.cs`/`PlayerWalkState.cs` conflicted between
    `CameraRelativeInputOnGravityPlane(up)` (HEAD) and `CameraRelativeInput()` (incoming). Grepped
    the actual `PlayerState.cs` base class — it only defines the parameterless method. HEAD's side
    would not have compiled; took the incoming side.
  - Branch 7: `SpellWord`/`SpellLexicon` conflicted between field names `spellWord` (HEAD) and
    `Word` (incoming). Checked whether either name was already load-bearing elsewhere — the
    incoming branch's own new `FullRaidIntegrationTests.cs` (arriving un-conflicted in the same
    merge) already used `.Word`. Took `Word` everywhere.
- **A git "rename" across two unrelated files was correctly identified as a similarity-detection
  false positive**, not a real rename (branch 4: `Gravity.asmdef` → `Voice/RogueAi.Voice.asmdef`
  — two small, boilerplate-heavy JSON files that happened to look similar to git's heuristic).
  Verified both files' actual content before resolving.
- **Two genuine multi-side merges** (not just picking a side) where both branches added
  different, valuable content to the same spot: `TestSceneBuilder.cs` (branch 4, combined ground
  sizing constants + the "drop item from height" gravity-test behavior) and
  `Tools/AssetPipeline/README.md` (branch 6, two independently-appended doc sections).

## Real defects found and fixed along the way (all confirmed against the actual toolchain, never shimmed)

1. **Two separate duplicate-`.asmdef`-in-one-folder incidents** (branches 3 and 7): merging two
   branches that each added a new C# system into the same existing folder is a case git's merge
   never flags as a conflict (they're two different new files), but Unity refuses to compile it.
   Fixed both by relocating the newer system's files into their own subfolder, preserving every
   `.meta` file so GUIDs/references stayed intact.
2. **`WaitForEndOfFrame` is not supported under `-batchmode` at all** (branch 3) — confirmed from
   Unity's own error text after a wrong first theory (blamed `-nographics` specifically; disproved
   by reproducing the identical hang with real graphics enabled). Rewrote the affected PlayMode
   screenshot test to render to a `RenderTexture` via a dedicated camera instead of waiting on
   frame presentation. Full diagnosis in `KNOWN_ISSUES.md`, including the corrected root cause.
3. **Three missing assembly references** (branch 7): `RogueAi.UI.asmdef` needed `PurrNet.Runtime`;
   `Editor.asmdef` needed the same; `RogueAi.Tests.asmdef` needed `RogueAi.Core`. Each was a case
   where a branch's own new files needed a reference the shared `.asmdef` never had — none of
   these had ever compiled before this merge exposed them.
4. **Two real, previously-uncaught test bugs** (branch 7), both only surfacing now because the
   compile errors above had always prevented these tests from running before:
   - `ExtractionZone` requires the abstract `Collider` type, which Unity cannot auto-add. Three
     new integration tests called `AddComponent<ExtractionZone>()` without a concrete collider
     first. Verified the real production scene builder (`RaidSceneBuilder.BuildExtractionZone`)
     does this correctly, confirming the bug was isolated to test setup code. Fixed all three.
   - `GuardTests` accessed `GameObject.transform` after `Object.DestroyImmediate` on the same
     object, which throws `MissingReferenceException` in this Unity version. Fixed by capturing
     the `Transform` reference before destroying.

## Final compile & test metrics (fully-integrated staging branch, real Unity batchmode runs)

- **Compile:** 0 `error CS` (`.agent_reports/compile_final.log`).
- **EditMode:** `result="Passed" total="12" passed="12" failed="0"` (`results_final_editmode.xml`).
- **PlayMode:** `result="Passed" total="105" passed="105" failed="0"` (`results_final_playmode.xml`)
  — up from 34 before branch 7 added its new gameplay test suites (FullRaidIntegrationTests,
  GuardTests, HudAndInteractionTests, RaidLoopTests, SpellEffectTests, CastleGeneratorTests).
- **Screenshots:** `UI_Verification_Screenshots/` contains 7 real rendered PNGs (title screen,
  HUD, pause, inventory, settings, victory, game-over), visually inspected, not blank frames.

## Headless build check — not run, and why

The plan called for a final `-buildTarget Win64` headless build check. Ran the command; it did
**not** perform a real player build — `-buildTarget` alone only switches the active platform,
and this project has no `BuildPipeline.BuildPlayer()` entry point anywhere (confirmed by
grepping the whole `Assets/` tree). This is by design, not an oversight: the project's own
`.github/workflows/unity.yml` explicitly runs only the EditMode/PlayMode test suites and states
in its own comments that no build job exists. A real build check was never a meaningful
verification step for this project as currently set up, so it is reported as not applicable
rather than faked.

## Paper trail

Every step's compile log, test XML, and this narrative live under `.agent_reports/` on
`integration/staging-2026-09-15`, committed and pushed after each branch per this repo's
commit-everything policy. `KNOWN_ISSUES.md` documents the `WaitForEndOfFrame`/batchmode finding
in full (including the corrected root cause) as a standalone reference for anyone hitting the
same symptom in the future.
