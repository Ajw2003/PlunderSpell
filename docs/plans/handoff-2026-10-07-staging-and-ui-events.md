# Handoff 2026-10-07: get staging compiling in Unity, check what the cloud session could not, then move the HUD onto events

**For:** an agent on aj's PC, with the Unity Editor and the `unity` CLI.
**From:** the cloud session of 2026-10-06/07, which had no Unity: it built and tested only through
`Tools/Headless/verify.sh`, which compiles the game against stand-in Unity APIs and does not catch everything Unity
catches (see "Traps" below).
**Repo on aj's PC:** `C:\Users\aj\Desktop\GameDev\PlunderSpell`. The `Tools/Unity/*.sh` scripts are bash: run them
from Git Bash.

Read first, in this order: `CLAUDE.md`, `docs/1-landing/README.md`, this file, `docs/plans/ui-events-mvc.md`,
`Tools/Unity/README.md` (how to drive the Editor and wait on it correctly).

---

## 1. Where things stand

**Branch to work from: `claude/staging-2026-10-07`** (pushed; head `ae6dd7af` at handoff). It is `main` plus, merged in
this order:

| PR | Branch | What | Issue |
|---|---|---|---|
| #290 | `claude/headless-harness-281` | Headless harness compiles again (shims only) | #281 |
| #291 | `claude/mic-change-chatter-202` | A new microphone in Settings reaches guard chatter at once | #202 |
| #292 | `claude/hud-damage-alloc-216` | Raid HUD damage numbers stop allocating every frame | #216 |
| #294 | `claude/voice-buffers-215` | Voice input reads the mic in fixed chunks, no per-frame allocation | #215 |
| #295 | `claude/naming-fixes-219` | Player / ranged weapon / grab beam follow the naming standard | #219 |
| #285 | `claude/diegetic-lair-market-proposal` | Approved diegetic UI + Lair + Market proposal, concept art | #284 |
| #288 | `claude/lair-models-286` | Lair models (12) | #286 |
| #293 | `claude/market-models-287` | Market models (15) | #287 |
| #296 | `claude/guard-tests-289` | Headless physics shims match Unity for the guard tests | #289 |

Plus three commits made on staging itself: `docs/plans/lair-market-in-engine.md`, the fix below, and
`docs/plans/ui-events-mvc.md`.

**The compile errors aj saw** were in `Assets/_Project/Scripts/Tests/Runtime/DamageFeedbackViewTests.cs`: it called
`Time.Reset()` / `Time.Advance()`, which exist only in the headless harness. Fixed on staging (`7edf7559`) and on
#292's branch by wrapping that file in `#if HEADLESS`. Unity has **not** been run since; that is your first job.

**Headless result on staging:** `Failed: 99, Passed: 397, Skipped: 4, Total: 500`. The 99 are harness limits (scene,
asset and Resources loading, no rigidbody simulation), listed in `Tools/Headless/README.md`. No Unity-side test run of
this branch exists yet.

**Other work in flight elsewhere — do not touch:** `claude/project-thread-x1cqae` (108 commits: guards, stairs, doors,
painted look, #247–#280) and `claude/playability-fixes` / `Staging` belong to other sessions. Expect conflicts with
them later in guard and castle code; not your job now.

---

## 2. Task A — staging compiles and its tests run in Unity

1. `git fetch origin` and `git switch claude/staging-2026-10-07` (`git pull` if already on it). Commit nothing of your
   own on it directly; branch off it (`claude/...`) for every change.
2. Open the Editor on the project (`Tools/Unity/README.md`, "Before anything"), make sure it is not in Play mode, then
   `bash Tools/Unity/recompile.sh`. Exit 0 = clean. Paste every error into your notes.
3. **First suspects if it does not compile** (code the headless harness never compiles, because it is inside
   `#if !HEADLESS`):
   - `Assets/_Project/Scripts/Runtime/Voice/VoskVoiceInputService.cs` — #291 added `SwitchChatterMicrophone`,
     `HandleMicrophoneChanged` and the subscribe in `EnsurePump` / unsubscribe in `MainThreadPump.OnDestroy`; #294 added
     `ChunkSamples`, `ReadMicrophone(bool flush)` and `FeedChunk`. They were merged together by git, never compiled.
   - Assembly references: the harness compiles everything as one project, so a missing `.asmdef` reference never shows
     there. #295 moved `GrabBeam` into namespace `Plunderspell.Items` and added `using Plunderspell.Items;` to
     `ItemManager.cs`, `Net/CarryBeamRelay.cs`, `Tests/Runtime/CarryFeelTests.cs`.
4. After it compiles: `git diff --ignore-cr-at-eol --stat Assets/_Project/Net/NetworkPrefabs.asset` must print nothing
   (a failed compile has dropped prefabs from PurrNet's list before — `docs/5-today/Today.md`, 2026-10-02).
5. Run the tests: `bash Tools/Unity/run_tests.sh Plunderspell.Tests EditMode`, then `... PlayMode`. Record the `total`
   line and every `FAILED` line. There is no earlier Unity baseline on this branch: for each failure, check whether it
   also fails on `main` before blaming staging.
6. Fix what staging broke, each fix on its own `claude/` branch off staging, with a PR into
   `claude/staging-2026-10-07`, referencing the PR whose change caused it.

**Done when:** Unity compiles staging with no errors; the EditMode and PlayMode totals are written in
`docs/5-today/Today.md`; every failure is either fixed or shown to fail on `main` too.

---

## 3. Task B — the Unity checks the cloud session could not do

Each PR names its own check; this is the list. Note the result on the PR (one short comment) or in Today.md.

1. **Import the 27 new models** (`Assets/_Project/Art/Models/Lair/`, `.../Market/`): Unity creates their `.meta` files —
   commit those. Check scale (a 1.8 m player beside the 2.16 m Lair door) and that materials are not pink.
2. **#291 mic switch:** with "Guards hear my voice" on, change microphone in Settings; chatter must hear the new one
   without toggling chatter off and on.
3. **#294 voice buffers:** cast by voice and chat with chatter on; the Profiler must show no GC allocation from
   `VoskVoiceInputService.ReadMicrophone` while listening, and casting must still recognise words.
4. **#295 renamed fields:** open `RaidPlayer.prefab`, `Player.prefab` and `Crossbow.prefab`; walk speed, respawn speed
   and the crossbow stats must keep their values (`[FormerlySerializedAs]` maps them on load).
5. **#292 HUD:** Profiler during a raid: no allocations from `DamageFeedbackView`.

---

## 4. Task C — views learn about state through events, not per-frame polling

The plan, with the inventory and file:line references, is `docs/plans/ui-events-mvc.md`. Summary and what to do.

### 4.1 Ask aj two questions first (the plan's "Decisions waiting on the owner")

1. How far the move to `EventManager` goes. Recommended: everything a presenter or view listens to goes through
   `EventManager` now; existing system-to-system C# events move only when touched. (Alternatives: migrate all 80 C#
   events at once; or keep C# events and only remove polling.)
2. Values that change every frame (mic loudness, chant progress, countdown). Recommended: derive or sample — the
   countdown is one event carrying its end time, the view computes what is left; mic level and chant progress are
   sampled by the presenter and published at a capped rate (~15/s) only while active.

Do not start code until aj answers. The steps below assume the recommended answers; adjust if not.

### 4.2 Issues (house rule: a plan over three steps becomes issues before code)

One parent issue "Views learn about state through events, not per-frame polling" (label `Claude created this`,
`ui`, `performance`; body links `docs/plans/ui-events-mvc.md`), and one child per step below (`Part of #N`). PRs say
`Refs #N`, never Closes.

### 4.3 What polls today (verified 2026-10-07)

- `UI/RaidHud/RaidHudPresenter.cs:54` — `Update() => Model = Build()`: reads `RaidDirector.Phase`,
  `ExtractionZone.TimeRemaining`, `EnemyDirector.State/AlarmLevel`, `LairHubManager.TotalDebt/AccumulatedGold`,
  `ItemManager.Instance.CarriedItem/CarriedRangedWeapon`, the local player's interactor — every frame.
- `UI/RaidHud/RaidHudView.cs:261` — calls `_presenter.Build()` again inside `OnGUI` (Layout + Repaint: 2–3 builds per
  frame); also reads `PushToCast.IsCasting`, the speech service's `IsListening/CurrentRms/CurrentDevice`,
  `SpellCastingSystem.Local.IsChanting/ChantingWord/ChantProgress`, `GameServices.PlayerStats.Mana`,
  `GameServices.GameState.CurrentState` (~`:248`, `:433–541`).
- `UI/RaidHud/DamageFeedbackView.cs` — per draw: health of each recently hurt target, local player root, game state.
- `UI/RaidHud/CameraShakeDirector.cs:102` — `PlayerStateMachine.Local` every frame.
- `Audio/MusicDirector.cs:101–134` — game state, raid phase, Age, alarm state every frame.
- `UI/BackdropCamera.cs:33` — `Camera.allCamerasCount` every frame (small).
- Keep as is: `UI/GameFlowInput.cs` (input), `Audio/AudioDirector.cs` (already 1/s), `HUDScreen`, `LairScreen`,
  `SettingsScreen`, menus (already event- or button-driven), and all simulation (movement, physics, guards, camera,
  item dragging).

### 4.4 Change events that already exist (reuse them; route through the bus per decision 1)

`RaidDirector.PhaseChanged`, `RaidDirector.PortalOpened`, `RaidDirector.RaidResolved`; `EnemyDirector.AlarmStateChanged`
and `OnAlarmChanged` (the alarm has its own small bus, `EnemyDirector.Bus`); `ExtractionZone.HaulInZoneChanged`,
`ExtractionResolved`; `ExtractionController.ExtractionStarted/Cancelled/Completed/Progress`;
`GameStateManager.StateChanged`; `PlayerStats.StatsChanged`; `SpellCastingSystem.PhraseResolved/CastResolved`;
`Damage.Dealt`; `AudioInputSettings.MicrophoneChanged/GuardsHearChatterChanged`.

### 4.5 Missing — add at the owner, raising only on change, carrying the new value

- `LairHubManager`: debt / banked gold changed (it has no events today).
- `ItemManager`: carried item changed, carried ranged weapon changed (none today).
- The local player's interaction target changed (find its owner from `RaidHudPresenter.Interactor`).
- Raid timer: one event when the extraction/portal countdown starts or its end time changes, carrying the end time.
- `PlayerStateMachine`: the local player was set (for `CameraShakeDirector`, `DamageFeedbackView`).
- `RaidDirector`: Age chosen for this raid, if `MusicDirector` cannot get it from `PhaseChanged`.

### 4.6 Steps (one child issue each)

1. **The events.** Add §4.5 at their owners. Bus events are small `struct`s implementing `IEvent`
   (`Runtime/Core/Events/EventManager.cs`); `Publish` already allocates nothing when debug logging is off — keep it so.
2. **Raid HUD.** `RaidHudPresenter` subscribes (OnEnable / OnDisable) to what its model needs, updates only the field
   that changed, raises one `Changed`; remove `Update()`. `RaidHudView` draws only from the presenter's cached model:
   delete the `Build()` call in `OnGUI` and every direct read of another system; mic level and chant progress come in
   through the model per decision 2.
3. **The rest.** `DamageFeedbackView` (carry new health in the damage event instead of reading `IHealth` per draw),
   `CameraShakeDirector`, `MusicDirector`, `BackdropCamera`.
4. **Docs.** `docs/4-systems/core.md`: the rule (model = data, presenter subscribes and owns the model, view only
   draws; events are structs raised on change and carrying the value). `docs/4-systems/raid.md` / the HUD section.
   A dated entry in `docs/6-decisions/Decisions.md` (the bus is now the UI's channel; threading deferred until
   measured). Update `docs/3-state/ProjectState.md` if it describes the HUD.

### 4.7 Tests and proof

- EditMode tests per presenter: the model changes when the event fires; it does **not** rebuild without one (count
  builds); unsubscribing on disable leaves no listener.
- A test that a frame with no events allocates nothing in the presenter (`GC.GetAllocatedBytesForCurrentThread`
  around repeated calls, after warm-up). Note: in the Editor, `MethodInfo.Invoke` allocates — call methods directly or
  through an internal test hook, not reflection.
- In a real raid: Profiler capture before and after, `RaidHudPresenter` + `RaidHudView` + `MusicDirector` CPU and GC
  per frame. Write the numbers in the PR. `Tools/Unity/perf_capture.sh` / `perf_report.sh` exist for this.
- `bash Tools/Unity/run_tests.sh Plunderspell.Tests EditMode` and `PlayMode`: no test that passed before fails.
- Also keep `./Tools/Headless/verify.sh` green-as-before (99 failed is the staging baseline).

### 4.8 Threading

The bus stays on the main thread (listeners touch Unity objects). If a profile ever shows publishing as a cost: first
batch (queue in the frame, deliver once), then cut subscribers; moving delivery off the main thread is the last
resort. Do not add threading without a measurement that asks for it.

### 4.9 Done when

No presenter or view in §4.3 reads another system in `Update`/`OnGUI`; the raid HUD model is built only on events;
Profiler numbers before/after are in the PR; Unity EditMode + PlayMode and the headless suite show nothing newly
failing; docs updated. Then the diegetic UI work (`docs/plans/diegetic-ui-lair-market.md`, phase 1) builds on these
events: the fires listen to the alarm, the watch to the timer, the ledger to the debt.

---

## 5. Traps found this session

- **Harness-only APIs.** The headless shims add helpers Unity does not have (`Time.Advance`, `Time.Reset`, …). A test
  that uses them must be inside `#if HEADLESS`. This is what broke staging.
- **`#if !HEADLESS` code is never compiled headlessly** (all of the Vosk microphone path). Only Unity proves it.
- **Assembly boundaries are not checked headlessly** — a new `using` or a type moved between `.asmdef`s can compile
  there and fail in Unity.
- **Commit from the main checkout.** In the cloud session, commits made with `git -C <other worktree>` hit a permission
  prompt ("names another repo") and timed out; commits in the main checkout went straight through. A refused commit is
  never left local: commit it another way or onto a fresh `claude/` branch, and push.
- **Permission prompts that time out leave the dialog open** (Ajw2003/AjsClaudeCodeTools#149). Clicking an old dialog
  late is not known to be safe — deny it if the work has already gone another way.
- **Commit attribution:** commits end with `Committed by AJ's agent`, PRs with `Opened by AJ's agent`; a hook rejects
  Claude attribution.
