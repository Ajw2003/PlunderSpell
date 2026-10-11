# Handoff: per-frame code to events and timers, and the audit's other fixes (2026-10-11)

For a fresh session in this repo. Parent issue #389, children #390-#402. The finding behind every item, with
`file:line`, is in the audit: [`update-audit-2026-10-11.md`](update-audit-2026-10-11.md) (summary and "Second read")
and its [part 1](update-audit-2026-10-11-part1.md) and [part 2](update-audit-2026-10-11-part2.md). Those three
files are on branch `AjsAgent/update-audit` (PR #388) until it merges into `staging-2026-10-11`: read them there
with `git show AjsAgent/update-audit:docs/plans/update-audit-2026-10-11.md`.

## The rule (owner, 2026-10-11)

`Update`, `LateUpdate` and `FixedUpdate` are only for work that genuinely changes every frame: movement, physics,
camera follow, a value that visibly changes every frame. Everything else:
- **A change** (a count, a state, a flag, an object appearing) is published through `EventManager` by the thing that
  changes, and only what needs to know listens (docs/4-systems/core.md; Decisions 2026-10-06).
- **Time** (later, every N seconds, a fade that runs only sometimes) is a coroutine or scheduled call started when it
  begins.
- **Input** comes from `GameInput`'s actions as `performed`/`canceled` callbacks, never `Keyboard.current` or
  `Mouse.current`, never a hard-coded key.
- **Several flags for one thing** become one state (an enum or state classes).
- Architecture: MVC (views drawn from models, fed by events), singletons, factories, SOLID.
- Every per-frame method that stays carries `// per-frame: <reason>` on the line above it. Once the house-rules
  plugin's new hook lands (Ajw2003/AjsClaudeCodeTools#218), a new one without that comment is refused at the write.

The owner stopped work on 2026-10-11 because agents kept putting logic in `Update`. Do not prescribe or write a poll,
a "check every N seconds", or a per-frame timer, anywhere, including in a builder's brief. If something seems to need
one, stop and ask.

## How to work here

- **Branches:** one `AjsAgent/<topic>` per issue, cut from `staging-2026-10-11`; pull request into
  `staging-2026-10-11` with `Refs #<issue>` (never Closes/Fixes; the owner closes issues after testing). These are
  agent-owned branches: commit and push without asking. Never `main`.
- **Delegation:** one `@house-rules:builder` per issue, one at a time, working in this folder on its branch. Not in a
  worktree, and never `cd` or `git -C`: the guard refuses commands whose repo it cannot follow, and Unity only runs
  on this folder. The brief names, for each change, the event or pattern that carries it.
- **Unity:** the Editor is open on this folder. Before recompiling check it is not in Play mode
  (`Tools/Unity/README.md`, "Before anything"). `bash Tools/Unity/recompile.sh`, then
  `bash Tools/Unity/run_tests.sh Plunderspell EditMode` and `... PlayMode`. Never pipe them through `tail` or `head`;
  write the output to `docs/generated/<topic>-*.log` and add it with `git add -f` (`*.log` is ignored).
- **Co-op by default:** every in-game check runs the Editor as host plus the Development build as client, through
  the `Tools/Unity/*_check.sh` scripts. Ones that cover this work: `hud_events_check.sh` (HUD, watch, grimoire, hit
  feedback), `coop_lair_check.sh` (Lair, Market, dial, ledger, counters), `lair_pile_check.sh` (Lair loot after
  quitting to the menu; on branch `AjsAgent/lair-items-378`), `wizard_coop_check.sh` (the wizard's flags),
  `coop_carry_check.sh` (carrying), `alarm_witness_check.sh` (the alarm). Run the ones whose system you touched,
  twice, and look at their screenshots.
- **Baseline before you start:** staging's tip passed EditMode 243 / 3 skipped / 0 failed and PlayMode 518/518 on
  2026-10-11 (`docs/generated/staging-2026-10-11-*.log`). Every pull request keeps that, plus its new tests.
- **Leave alone** the Unity import noise in the tree (the Spectral font asset, `ProjectSettings.asset`, a
  `timings.txt`, two wizard `.fbx.meta` files). Commit only the paths you changed.
- **Docs:** each pull request updates the system doc it touched (`docs/4-systems/`), `docs/5-today/Today.md` and
  `docs/3-state/ProjectState.md`.

## Order

Do them in this order; each is its own issue, branch and pull request.

### 1. #390 — Write the rule into the project
`docs/4-systems/core.md` gains a section "When `Update` is allowed": the four buckets (keep, event, timer, remove)
with one example of each from this code; a dated entry in `docs/6-decisions/Decisions.md`; one line in `CLAUDE.md`
pointing at it. Docs only.

### 2. #391 — The four bugs
1. `Player/PlayerInputController.cs:23` re-runs `EnableAllInputs` (`:227`) on every `PlayerIdleEvent`, adding the
   input handlers again each time. Subscribe once; remove the idle re-enable or make enabling idempotent; unsubscribe
   in `OnDestroy` (`:208`).
2. `Player/PlayerStateMachine.cs:141-142` multiplies the mouse's movement by `Time.deltaTime`; mouse delta is already
   per frame, so turn speed depends on frame rate. Drop the `deltaTime`; retune `MouseSensitivity` so the feel at
   60 fps is unchanged.
3. `Player/States/PlayerWalkState.cs:10` and `Player/States/PlayerIdleState.cs:16` write the physics body's velocity
   in `Update`; move to the state's `FixedUpdate`.
4. `Player/States/PlayerWalkState.cs:14` logs a warning every frame without a camera; log once.
Tests: one per bug (handlers fire once after several idles; same turn for the same mouse movement at two frame rates).
Check: `wizard_coop_check.sh`, and walk and look by hand in co-op.

### 3. #378 — Lair loot keeps its place, done with events
Branch `AjsAgent/lair-items-378` already has the pose save (`737915e3`: each piece's position and rotation are
saved, old saves still load, 4 EditMode tests pass) and the co-op check `Tools/Unity/lair_pile_check.sh`, which
caught the real bug: quitting to the menu wiped the saved pile (2 -> 0). Its tip, `2676730f`, is a labelled stopgap
that still polls in `Update`. Redo `Raid/HaulLanding.cs` with no `Update` at all:
- Save when a haul lands (`HaulExtracted`, as now), when a piece is removed or sold (`Remove`, as now), when the
  company sets out (`RaidPhaseChanged` to `Generating`), and when the session ends: add a `SessionEnding` event
  published by `CoopSession.Leave()` (`Net/CoopSession.cs:119`) **before** it shuts the network down, and save on
  application quit.
- Restore when the server starts (a server-started event, or `CoopChanged` with the server running), and on
  `SaveSlotLoaded`, never from a per-frame check.
- A piece a player moves within the Lair is saved at the next of those moments. A crash loses moves since then;
  that trade-off was accepted on 2026-10-11.
Check: `lair_pile_check.sh` twice (it must PASS all five checks), then `coop_lair_check.sh`. Then open the pull
request for `AjsAgent/lair-items-378`.

### 4. #392 — Input through the actions
Add actions to the input map for every direct read and hard-coded key, each handled by a `performed`/`canceled`
callback: `Items/ItemManager.cs:50,112,114,126,150,167` (aim, rotate, scroll, mouse delta), `UI/GameFlowInput.cs:12`
(Escape), `UI/RaidHud/HudHoldKeys.cs:43-48` (T, Tab), `UI/RaidHud/GrimoireView.cs:60` (wheel; a view must not read
input, so this goes in a presenter), `Voice/PushToCastController.cs:63` (V), `Voice/MockVoiceInputService.cs:107`
(digits), `Player/PlayerInputController.cs:20,57` (the C key for creeping), `Playtest/CombatBenchHud.cs:41` (F1),
`Raid/RaidBootstrapper.cs:121-125` (debug keys). Key polls that become callbacks: `Raid/RoomTravel.cs:23`,
`Raid/LairCenturyDial.cs:56`, `Raid/SellCounter.cs:122-127,137-146` (1/2/3 routed to the nearest counter, as
`HaggleVoiceRouter` does for spoken words), `Loot/LootInteractor.cs:55`, `Items/ItemManager.cs:86`,
`Playtest/FreeLookPlaytestController.cs:67`. Tests that drive these keys move to `InputSystem.Update()` after the
press. Check: `hud_events_check.sh`, `coop_lair_check.sh`, `coop_carry_check.sh`.

### 5. #393 — Game-state and local-player checks become events
`Player/PlayerInputController.cs:49-55` and `Voice/PushToCastController.cs:54-61` (menu gates: `GameStateChanged`),
`UI/RaidHud/HudHoldKeys.cs:44,49` (play gate, carrying: `GameStateChanged`, `CarriedItemChanged`),
`Playtest/FreeLookPlaytestController.cs:63`, `Player/WizardAnimationDriver.cs:75,85-91,96,98` (own body:
`LocalPlayerChanged`; down and crouch: a direct call from the setter on the same object; casting:
`CastingStateChanged`), `Raid/LairRoomSpawner.cs:35` (`LocalPlayerChanged`), `Player/PlayerInputController.cs:60-64`
(a new `ItemRotationChanged` from `ItemManager`), `Net/PlayerNetworkOwnership.cs:171` (a new `LocalPlayerDied`).
Check: `wizard_coop_check.sh`, `hud_events_check.sh`.

### 6. #394 — Lair and Market react to events
`Raid/LairCenturyDial.cs:59-61` (`AgeChosen`), `Raid/LairLedgerBook.cs:51-54` (a new `LastRaidChanged` from
`LairHubManager`; redraw from the handlers, no dirty flag; stop the `FindFirstObjectByType` retry at `:46`),
`Raid/SellCounter.cs:129-136` (pieces arriving and leaving by `OnTriggerEnter`/`OnTriggerExit` on the counter's
volumes, plus `CarriedItemChanged`; the per-frame `OverlapBox` and allocations go), `Raid/RaidDirector.cs:192-193`
(remove: the replication handlers at `:651-656` and `:697-698` already build the castle), `Raid/RaidDirector.cs:196`
(seats: from player join and leave events, not a search every 0.5 s), `Raid/RaidListenerFallback.cs:27` (check on
`GameStateChanged`, scene load and `LocalPlayerChanged`, as `BackdropCamera` does). Check: `coop_lair_check.sh`
twice.

### 7. #395 — Castle, atmosphere and audio react to events
`Alarm/EnemyDirector.cs:179` (publish `AlarmLevelChanged` from the level's setter, on every peer),
`Atmosphere/CastleAtmosphere.cs:188` (the settings event), `Atmosphere/CastleFireSpawner.cs:40` and
`Atmosphere/NightLookPreview.cs:272` (a new `CastleBuilt` from the castle generator), `Audio/GuardVoiceDirector.cs:65`
(guard events), `Audio/AudioDirector.cs:119` (three jobs: casting from `CastingStateChanged`; the volume settle as a
fade that runs only while fading; new audio sources announce themselves instead of a timed search). Check:
`alarm_witness_check.sh`, `hud_events_check.sh` (it reads the music on both machines).

### 8. #396 — Timers and fades become coroutines
`Status/StatusEffectReceiver.cs:87,93` (a coroutine per active status; keep `Tick` as the test seam),
`Spells/SpellCastingSystem.cs:183,185-192` (mana regen from `PlayerStatsChanged`; the chant as one coroutine that
also publishes its progress), `UI/RaidHud/DamageFeedbackView.cs:313-348` (expiry, flash and bars),
`Raid/SellCounter.cs:116-121` (the vendor's line), `Raid/RaidBootstrapper.cs:111-119` (auto-start),
`UI/RaidHud/HudHoldKeys.cs:48` (the first-raid auto-open), `Player/WizardAnimationDriver.cs:92-93` (vanish), the
monster throttles (`Enemies/States/MonsterIdleState.cs:26`, `MonsterPatrolState.cs:27`, `MonsterPursueState.cs:19`,
`MonsterAttackState.cs:22`, `MonsterPickedUpState.cs:55`), and every fade that today idles in `Update`:
`Audio/MusicDirector.cs:131`, `Raid/CounterSlate.cs:49`, `Raid/HostSetsOutLine.cs:53`, `Raid/LairCenturyDial.cs:62`,
`Items/Weapons/RangedWeapon.cs:59`, `UI/RaidHud/CameraShakeDirector.cs:115`, `UI/RaidHud/DamageFeedbackView.cs:323`.
Check: `hud_events_check.sh`, `coop_lair_check.sh`.

### 9. #397 — Extraction and guard death
`Extraction/ExtractionZone.cs:186` (the networked value's change callback), `Guards/Core/GuardDeathPlayback.cs:21`
(the guard's death event). Check: a co-op raid that extracts (`coop_lair_check.sh` does one) and a guard death.

### 10. #398 — Remove dead per-frame code
`Audio/AudioGapProbe.cs:31` (eval-only diagnostic in runtime code; move it to `Tools/Unity/eval/` if still used),
`Core/StateMachine/BaseStateMachine.cs:27`, `Enemies/States/MonsterState.cs:18`, `Enemies/States/MonsterDeadState.cs:16`.
`Enemies/States/MonsterState.cs:4` is marked obsolete but every `Monster*State` inherits it: list what still uses the
Monster states and ask the owner whether they go entirely. Inventory what is removed (keep / change / drop) before
deleting.

### 11. #399 — The local player's camera
`Items/ItemManager.cs:45` finds "the" camera with `FindFirstObjectByType<Camera>()`, which can pick a backdrop camera
or another player's; `Camera.main` is read in 10 runtime files, several every frame (`Items/ItemManager.cs:90`,
`Raid/SellCounter.cs:162`, `UI/RaidHud/DamageFeedbackView.cs:357`). Each holds the local player's camera, set once from
`LocalPlayerChanged`.

### 12. #400 — Item's carry flags become one state (design first)
`Items/Item.cs` (1,249 lines, 73 `if`s) keeps `_isDragging` `:37`, `_isInHand` `:58`, `_isRotating` `:49`,
`_inPortal` `:548` and `_frozenByPortal` `:549`, which describe one thing and can contradict each other. Write a short
proposal (one state, enum or state classes; how to split the class by job) into the issue and get the owner's yes
before any code. Carrying is fragile and networked: `coop_carry_check.sh` before and after, twice each.

### 13. #401 — Tests stop waiting by polling
The 26 rows in part 2's tests table: each wait becomes an event, a direct call, `InputSystem.Update()` or
`Physics.Simulate`. Do each alongside the code fix it depends on (for example the grimoire and watch tests with #392),
and the rest here.

### 14. #402 — Review only: crowded classes and network ownership
Proposals in the issue, no code without the owner's approval: `Net/PlayerNetworkOwnership.cs` (47 `if`s in 294 lines,
all ownership decisions), `Raid/RaidDirector.cs` (772 lines), `Player/PlayerStateMachine.cs` (573),
`Alarm/EnemyDirector.cs`, `UI/RaidHud/DamageFeedbackView.cs` (535); and the small merges: `UI/RaidHud/RaidHudPresenter.cs:78,80`
(watch up and grimoire open are one "which panel" value) and `Audio/MusicDirector.cs:60` (`_raidRunning` copies a game
state it can hear about).

## Along the way

Every per-frame method the audit kept (36 of them) gets its `// per-frame: <reason>` comment when its file is next
touched, using the reason in the audit's KEEP row.

## Before you stop, each time

Update `docs/5-today/Today.md` and `docs/3-state/ProjectState.md`, commit on your branch, push, and say in the pull
request exactly what was and was not checked in Unity and in co-op.
