# Handoff — 2026-10-08: the new staging branch, and merging the diegetic raid UI into it

For a local session with Unity. Tracking issue: #368.

## Where things stand

- **New staging branch: `claude/staging-2026-10-08`**, cut from `claude/lair-market` at `0d5b0fe7` and pushed.
  It holds nothing else yet.
- **#368** lists every branch with work that staging does not have (compared by patch content, not commit
  ID), grouped into what to merge, what to decide, and what is already covered.
- **The diegetic raid UI merge was started in the cloud and stopped before committing**, because the owner
  ruled that no code change lands without a Unity check. Nothing from it is pushed. Redo it locally using
  the steps below, or start from the cloud's resolved merge on `claude/wip-staging-merge-unverified`
  (`98507951`, a real two-parent merge of `0d5b0fe7` and `claude/staging-2026-10-07`). It is pushed only as a
  backup: it has not been opened in Unity, and it does not include this handoff commit.

## The merge to do: `claude/rimlight-318` minus the glow commit

The owner asked to merge `claude/rimlight-318` and leave out the WIP glow commit
`1391e480` ("verdigris glow on what you can use…", #318) for now.

**Merge `claude/staging-2026-10-07`, not `claude/rimlight-318`.** Diffing the two showed that
`claude/staging-2026-10-07` is exactly `claude/rimlight-318` minus `1391e480` (the only difference is that
commit's changes to `LootHighlight.cs` and `LootInteractor.cs`). Merging it brings in everything else,
including the #318 handoff doc, and leaves the glow unmerged, so `claude/rimlight-318` can still bring it
in later. Merging rimlight and then reverting the glow would mark the glow as merged, and a later merge
would not bring it back.

It brings in: event-driven damage feedback, camera shake, music and backdrop camera (#304, #305);
`EnemyDirectorBus` → `EnemyDirectorListener`, `LoopBus` → `LoopPool`; the mic meter test (#303);
Calm fires in every castle module (#317); the pocket watch on T (#324), the grimoire on Tab (#325), the
clean raid HUD strip (#326); and the co-op check output and screenshots.

### Three conflicts, and how the cloud session resolved them (unverified in Unity)

1. **`Assets/_Project/Scripts/Runtime/UI/RaidHud/DamageFeedbackView.cs`, `IsInWorld`.** Staging (#359)
   changed the check to `GameState.RaidOnScreen`, so feedback never draws in the Lair or when paused from
   it. The incoming branch caches the answer in `_inWorld` and updates it on `GameStateChanged`, using
   the old Playing-or-Paused rule. Resolution: keep the cache, compute it with staging's rule. Replace
   the incoming `InWorld(GameState)` helper with:

   ```csharp
   private static bool RaidOnScreen()
   {
       var gameState = Plunderspell.Core.GameServices.GameState;
       return gameState == null || gameState.RaidOnScreen;
   }
   ```

   and have both the `GameStateChanged` subscription and `OnEnable` set `_inWorld = RaidOnScreen();`.
   Keep `private bool IsInWorld() => _inWorld;`. `GameStateManager` publishes `GameStateChanged` after
   it updates `CurrentState` and `PausedFrom`, so `RaidOnScreen` is current when the handler reads it.

2. **`Assets/_Project/Scripts/Runtime/UI/RaidHud/RaidHudView.cs`, `OnGUI`.** Drop both sides of the
   conflict block. `DrawTopLeft` and `DrawTopRight` are gone because #326 moved that content onto the
   watch and grimoire, and staging (#353) already sets the crosshair's `HasTarget` near the top of
   `OnGUI`. Keep staging's `RaidOnScreen` early return.

3. **`docs/6-decisions/Decisions.md`.** Keep both sides as separate entries, newest first: staging's two
   2026-10-07 entries (debt shares; loot earned by selling), then the incoming "2026-10-06 (later) — The
   event bus move landed" entry.

Checked in the cloud: no `.cs` file still references `EnemyDirectorBus` or `LoopBus`. `GrimoireView` and
`WatchView` (via `HudHoldKeys`) draw only in `GameState.Playing`, so they do not show in the Lair.

### What the cloud could and could not check

The headless harness (`Tools/Headless/verify.sh --build`) **does not compile `claude/lair-market` itself**:
40 errors, all missing shims for `TMPro`/`TextMeshPro` (`CounterSlate`, `HostSetsOutLine`,
`LairCenturyDial`, `LairLedgerBook`), `Pose` (`CarriedTravel`), `Vector3Int` (`ProceduralCastleGenerator`)
and `PlayerInputs` (`GameInput`). The merged tree shows the same 40 errors and no new ones. Because the
build stops at those errors, they could be hiding new errors from the merge. **Only Unity can say whether
the merge compiles.** Fixing those shims is separate work worth its own issue.

## Next, on aj's PC (Unity)

1. Pull and merge on the staging branch:
   `git fetch origin` → `git checkout claude/staging-2026-10-08` →
   `git merge --no-ff origin/claude/staging-2026-10-07`, then resolve the three conflicts as above.
2. Open the project in Unity and confirm it compiles clean.
3. Run EditMode and PlayMode. The last Unity run on `claude/staging-2026-10-07`'s ancestor
   (`docs/5-today/Today.md`, 2026-10-06 night) had EditMode with 3 known failures (`ArtAssetImportTests`,
   `LootAmountTests…AboutDoubleTheOldHaul`, `LootBalanceTests…TooHeavyToLift`) and PlayMode 413/413.
   Compare against those numbers and against `claude/lair-market`'s own suites in `docs/3-state/ProjectState.md`.
4. Play it. In a raid: hold T for the watch, hold Tab for the grimoire, take a hit (vignette, shake,
   music). Then in the Lair room and Market: no raid HUD, no damage vignette, not even when paused there.
5. Commit the merge and push `claude/staging-2026-10-08`. Then continue down #368: item 2
   (`claude/busy-mendel-t5y57j`, the wizard), item 3 (#349), item 4 (voice mimicry), and the older
   branches in item 5.

Nothing is to be closed or deleted (PRs, branches) without the owner saying so, per #368.
