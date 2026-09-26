## 1. claude/plunderspell-asset-pipeline-2cx4w4 (merged)
- Clean merge, no conflicts (2 commits: fbx meta files, art import validator, CI workflows, headless test harness).
- Compile: 0 `error CS` in compile_asset-pipeline.log. Batchmode exited cleanly.
- EditMode tests: 1/1 passed (results_asset-pipeline.xml) — GeneratedPropsImportCleanly.

## 2. origin/claude/plunderspell-enemy-assets-nsfuqn (merged)
- Clean merge, no conflicts (3 commits: 5 rigged enemy models, EnemyForge Blender pipeline, render review contact sheets).
- Compile: 0 `error CS` in compile_enemy-assets.log. Clean batchmode exit.
- EditMode tests: 1/1 passed (results_enemy-assets.xml).

## 3. origin/claude/plunderspell-ui-system-ur2o22 (merged)
- Merge itself was clean (no git conflicts), but surfaced a real structural problem: two
  .asmdef files in Assets/_Project/Scripts/Runtime/Core/ (pre-existing Core.asmdef vs new
  Plunderspell.Core.asmdef). Fixed by relocating the new branch's 8 script files + its asmdef
  into Core/GameFlow/, preserving .meta files so GUIDs/references stayed intact.
- Compile: 0 error CS after the fix.
- EditMode: 12/12 passed.
- PlayMode: found and fixed a real bug in UIScreenshotPlayModeTests (WaitForEndOfFrame is not
  supported under -batchmode at all — see KNOWN_ISSUES.md for full diagnosis). Rewrote the
  capture to use a dedicated camera + RenderTexture instead. Verified 34/34 PlayMode tests pass,
  and visually inspected all 7 produced screenshots as real rendered UI.
- Also committed ~100 Unity-generated .meta files this and the prior enemy-assets branch never
  committed themselves (needed for stable GUIDs).

## 4. claude/inspiring-davinci-83909p (merged)
- 8 files conflicted. All resolved and verified against the actual current codebase, not just
  pattern-matched:
  - PlayerStateMachine.cs, Item.cs, PlayerDodgeState.cs: comment-only differences (both sides
    made the identical gravity-removal code change, just worded the explanatory comments
    differently) - kept HEAD's wording.
  - PlayerJumpState.cs, PlayerWalkState.cs: a REAL API mismatch. HEAD's side called
    CameraRelativeInputOnGravityPlane(up), a method that no longer exists anywhere in the
    codebase (grep confirmed) - the current PlayerState.cs base class only defines the
    parameterless CameraRelativeInput(). Took the incoming branch's side for both; HEAD's side
    would not have compiled.
  - TestSceneBuilder.cs: real merge of both sides' test-scene tuning - combined branch's
    GroundSize/GroundThickness constants and comment reasoning, adopted branch's "drop item from
    height" behavior (a better test of gravity restoration, which is what this branch is about),
    dropped an unused `groundTransform` parameter that HEAD's BuildTestItem accepted but never
    used.
  - Gravity.asmdef vs Voice/RogueAi.Voice.asmdef "rename": a git similarity-detection false
    positive (both are small boilerplate-heavy JSON files) - not a real rename. Verified content
    of both; kept HEAD's Voice.asmdef untouched (unrelated system, already in use) and left
    Gravity.asmdef deleted (agreed on both sides - the gravity module is gone).
- Compile: 0 error CS.
- EditMode: 12/12 passed.
- PlayMode: 34/34 passed.

## 5. origin/claude/plunderspell-fable-prompt-iutej1 (merged)
- Down to 1 commit after branch 4 merged (it shared 3 commits with inspiring-davinci).
- Pure documentation add (docs/prompts/plunderspell-fable.md), no code changes, no conflicts.
- Skipped Unity compile/test for this one specifically: zero .cs/.asmdef/asset changes means
  zero compile risk. Not a shortcut on code changes - there are none to verify.

## 6. claude/level-generation-visual-pqdsr8 (merged)
- 1 real conflict: Tools/AssetPipeline/README.md - both sides independently appended a different
  new doc section (HEAD: "Verifying the Unity side"; branch: "Adding a new castle module").
  Non-overlapping, both valuable - kept both, branch's section first (fits right after the
  existing "Adding a new prop" section), HEAD's after.
- Compile: 0 error CS.
- EditMode: 12/12 passed.
- PlayMode: 34/34 passed.

## 7. claude/plunderspell-core-gameplay-o2av06 (merged) — final branch

Largest and most substantial branch (9 commits: spells, guards, the full raid loop, HUD, README).

**Merge conflicts (4 files, all a single real rename):** SpellWord.cs/SpellLexicon.cs and two
test files all had the same conflict: HEAD had renamed the illegal `SpellWord` field (CS0542,
same name as its class) to `spellWord`; the incoming branch had independently renamed it to
`Word`. Verified which name is actually load-bearing before choosing: the incoming branch's own
new file `FullRaidIntegrationTests.cs` (arrives with this merge, not itself conflicted) already
uses `.Word` - so `Word` was kept everywhere, not pattern-matched blindly.

**Compile fixes needed after the merge (three separate real defects, each verified against the
actual codebase before fixing, none shimmed):**
1. Same class of bug as branch 3: two `.asmdef` files landed in `Assets/_Project/Scripts/Runtime/UI/`
   (existing `Plunderspell.UI.asmdef` + incoming `RogueAi.UI.asmdef`, brought in by 3 new
   `RaidHud*.cs` files). Moved the new files + asmdef into `UI/RaidHud/`.
2. `RogueAi.UI.asmdef` (now in RaidHud/) was missing a `PurrNet.Runtime` reference needed by
   `RaidHudPresenter.cs`'s use of NetworkBehaviour-derived types (RaidDirector, ExtractionZone,
   etc.) - added it, matching the same reference already used by every sibling `RogueAi.*.asmdef`.
3. Two more pre-existing project asmdefs were missing references their branch's own new files
   needed: `RogueAi.Tests.asmdef` was missing `RogueAi.Core` (for `IHealth`/`Interfaces`, used by
   the new `SpellEffectTests.cs`), and `Editor.asmdef` was missing `PurrNet.Runtime` (for the new
   `RaidSceneBuilder.cs`, which instantiates NetworkBehaviour-derived components).

**Real test bugs found and fixed (surfaced for the first time once the above compile errors were
fixed - these tests had never successfully run before):**
- `ExtractionZone` requires the abstract `Collider` type ([RequireComponent(typeof(Collider))]),
  which Unity cannot auto-add. Three new test files (`FullRaidIntegrationTests.cs`,
  `HudAndInteractionTests.cs`, `RaidLoopTests.cs`) called `AddComponent<ExtractionZone>()`
  directly without adding a concrete collider first, unlike the one already-working sibling test
  and the real production scene builder (`RaidSceneBuilder.BuildExtractionZone`, verified
  correct) which both add a `BoxCollider` first. Added the missing `AddComponent<BoxCollider>().isTrigger = true;`
  to all three, matching the established pattern.
- `GuardTests.Test_IntruderTagRegistersAndUnregisters` accessed `go.transform` *after*
  `Object.DestroyImmediate(go)`, which throws `MissingReferenceException` in this Unity version.
  Fixed by capturing the `Transform` reference before destroying the GameObject.

**Final verification:**
- Compile: 0 error CS.
- EditMode: 12/12 passed.
- PlayMode: 105/105 passed (up from 34 - this branch's new suites: FullRaidIntegrationTests,
  GuardTests, HudAndInteractionTests, RaidLoopTests, SpellEffectTests, CastleGeneratorTests).
