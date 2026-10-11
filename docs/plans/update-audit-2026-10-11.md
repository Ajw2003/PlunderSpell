# Update audit (2026-10-11)

**Why.** On 2026-10-11 an agent saved the Lair pile from a polling check in `Update`; the poll read a torn-down pile
as empty and wiped the save (#378). The owner asked for every per-frame method in the codebase to be audited against
the rule: `Update`, `LateUpdate` and `FixedUpdate` are only for work that genuinely runs every frame. Changes go
through `EventManager` on a need-to-know basis, timing through coroutines or scheduled calls, and the architecture is
MVC (views fed by events), singletons, factories and SOLID. Read-only; nothing was changed.

**Scope.** All 66 runtime scripts that have a per-frame method, every editor script (none has one), and the tests that
wait by polling. Details, with `file:line` for every row:
- [Part 1](update-audit-2026-10-11-part1.md): the first 33 runtime files.
- [Part 2](update-audit-2026-10-11-part2.md): the other 33 runtime files, and the tests.

## What it found

| Bucket | Part 1 | Part 2 | Total | Meaning |
|---|---:|---:|---:|---|
| KEEP | 14 | 22 | **36** | Truly per-frame: movement, physics, camera, fades, animation |
| EVENT | 10 | 35 | **45** | Watches for a change every frame; should listen for an event |
| TIMER | 6 | 11 | **17** | Counts time; should be a coroutine or scheduled call |
| REMOVE | 4 | 1 | **5** | Dead, redundant or debug-only |
| **Methods or parts** | 34 | 69 | **103** | |

Tests that wait by polling or by a fixed delay that hopes something happened: **26** (part 2).

So about two thirds of the per-frame code (67 of 103) does not need to run every frame.

## A bug found on the way

`Player/PlayerInputController.cs:23` subscribes to `PlayerIdleEvent` with `EnableAllInputs`, and
`EnableAllInputs` (`:227`) adds the jump, attack, look and other handlers with `+=` every time, without removing the
earlier ones. Each time the player goes idle the handlers stack, so one key press can fire several times; the
subscription is also never removed. Found by reading; not yet run.

## Second read (by hand, 2026-10-11)

The owner asked for a second read, widened to: too many true/false flags and `if` chains, the old input system, and
logic that only works in single player or causes networking trouble. Part 2 checked out line by line. Corrections and
additions:

**Reclassified.**
- A fade or short animation that runs only sometimes is a **TIMER**, not KEEP: a coroutine started when it begins,
  so no `Update` sits idling every frame. This moves the music cross-fade (`Audio/MusicDirector.cs:131`), the slate
  fade (`Raid/CounterSlate.cs:49`), the "host sets out" line (`Raid/HostSetsOutLine.cs:53`), the century dial's turn
  (`Raid/LairCenturyDial.cs:62`), the reload bar (`Items/Weapons/RangedWeapon.cs:59`), the camera shake
  (`UI/RaidHud/CameraShakeDirector.cs:115`, which writes the view every frame even at zero) and the hit vignette
  (`UI/RaidHud/DamageFeedbackView.cs:323`).
- `Audio/AudioDirector.cs:119` is three things, not one KEEP: it checks whether you are casting every frame (EVENT:
  `CastingStateChanged`), settles volume fades (TIMER, only while fading), and searches for new audio sources every
  `PollSeconds` (EVENT: sources announce themselves).

**Old input system.** None. `Tests/Editor/NoOldInputTests.cs` already fails on `UnityEngine.Input`. But ten places
read the mouse or keyboard **directly** instead of through `GameInput`'s actions, so the keys cannot be rebound and
skip the input gating: `Items/ItemManager.cs:50,112,114,126,150,167` (aim, rotate, scroll, mouse delta),
`UI/GameFlowInput.cs:12` (Escape), `UI/RaidHud/HudHoldKeys.cs:43` (T, Tab), `UI/RaidHud/GrimoireView.cs:60` (wheel),
`Voice/PushToCastController.cs:63` (V), `Voice/MockVoiceInputService.cs:107` (digits), and a hard-coded C key for
creeping, `Player/PlayerInputController.cs:20`. Each becomes an action in the input map with a callback.

**Too many flags, too many `if`s.**
- `Items/Item.cs`: 1,249 lines, 73 `if`s, nine flags. Five of them (`_isDragging` `:37`, `_isInHand` `:58`,
  `_isRotating` `:49`, `_inPortal` `:548`, `_frozenByPortal` `:549`) describe one thing, what is happening to the item,
  and can contradict each other. One state (an enum, or a small state class each) replaces them. The class does too
  many jobs.
- `Player/PlayerNetworkOwnership.cs`: 47 `if`s in 294 lines, all deciding who owns what over the network. Needs a
  careful read before anything moves.
- Minor: `UI/RaidHud/RaidHudPresenter.cs:78,80` (`_watchUp`, `_grimoireOpen`) are one "which panel is up" value;
  `Audio/MusicDirector.cs:60` `_raidRunning` copies a game state it can learn from `GameStateChanged`.
- Fine: `UI/RaidHud/RaidHudModel.cs`'s flags are a read-only snapshot for the view, which is MVC working.
- God classes noted by both parts: `Raid/RaidDirector.cs` (772 lines), `Player/PlayerStateMachine.cs` (573),
  `Alarm/EnemyDirector.cs`, `UI/RaidHud/DamageFeedbackView.cs` (535).

**Single player only, or networking risks.**
- Checked and fine: game-changing random rolls happen on the server only. Spell effects, misfires included, run
  through `[ServerRpc]` (`Spells/SpellCastingSystem.cs:329-334`); the castle seed is rolled in a `ServerRpc`
  (`Castle/CastleNetworkManager.cs:61-64`); the haggle mood is rolled on every machine but only the server's is used
  (`Raid/SellCounter.cs:104,112`).
- `Items/ItemManager.cs:45` finds "the" camera with `FindFirstObjectByType<Camera>()`: with a backdrop camera or a
  second player's camera present it can pick the wrong one. Use the local player's camera.
- `Raid/RaidDirector.cs:196-218` recounts seats by searching every player object every 0.5 s; joins and leaves
  already come as events.
- `Camera.main` is read in 10 runtime files, several every frame (`Raid/SellCounter.cs:162`,
  `Items/ItemManager.cs:90`); each should hold the local player's camera.

**Bugs found on the way** (by reading; none run yet):
1. Input handlers stack: `Player/PlayerInputController.cs:23` re-runs `EnableAllInputs` (`:227`) on every
   `PlayerIdleEvent`, adding the jump, attack, look and other handlers again each time without removing them.
2. Mouse look depends on frame rate: `Player/PlayerStateMachine.cs:141-142` multiplies the mouse's movement by
   `Time.deltaTime`, though mouse movement is already per frame, so at 120 fps you turn half as far as at 60 fps
   (relevant to the Steam Deck pass, #377).
3. Movement written to the physics body in `Update` instead of `FixedUpdate`: `Player/States/PlayerWalkState.cs:10`,
   `Player/States/PlayerIdleState.cs:16`.
4. `Player/States/PlayerWalkState.cs:14` logs a warning every frame while the camera is unassigned.

## Where it goes next

- The rule is written into the project (`docs/4-systems/core.md`, "When `Update` is allowed") and into the house-rules
  plugin (Ajw2003/AjsClaudeCodeTools#218).
- The fixes go one system at a time, each its own issue under one parent, starting with the Lair pile (#378) done with
  events.
