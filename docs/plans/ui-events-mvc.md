# Plan: views learn about state through events, not per-frame polling

**Status: decided 2026-10-06 by the owner, and wider than proposed: every event between systems moves onto the bus,
not only what views listen to; continuous values are published on every change; the mic readout moves to Settings.
See `docs/6-decisions/Decisions.md` (2026-10-06) and "Decisions (settled)" at the end. Building now.**
Asked for by the owner: "ensure we are properly using the MVC design pattern and events to communicate anything per
frame like the hud was doing … communication should be on a need to know basis with light weight events that our
event bus handles, and if it becomes a bottleneck we multithread it or make it async."

## What is there today (inventory, `claude/staging-2026-10-07`)

**The bus.** `Runtime/Core/Events/EventManager.cs`: typed publish/subscribe (`Subscribe<T>(target, Action<T>)`,
`Unsubscribe<T>(target)`, `Publish<T>(T) where T : IEvent`), weak references to subscribers, safe against unsubscribing
mid-publish, main thread only. `Publish` allocates nothing (one dictionary lookup, a cached delegate call) unless debug
logging is on. It is barely used: **2 publish sites** (`PlayerIdleState.cs:13`, `PlayerInputController.cs:146`) and
7 event types, all player-state events.

**What the game actually uses.** Plain C# events: **14 `static event`s** (e.g. `Damage.Dealt`,
`SpellCastingSystem.CastResolved`, `LootValue.Ruined`, `AudioInputSettings.MicrophoneChanged`) and **66 instance
`event`s**. They are already lightweight and allocation-free.

**Where state is polled every frame instead of pushed:**

| Where | What it polls | Keep / change / drop |
|---|---|---|
| `UI/RaidHud/RaidHudPresenter.cs:54` | `Update() => Model = Build()`: raid phase, extraction timer, alarm state and level, debt, banked gold, haul, carried item, ranged weapon, interaction target, every frame | **change**: rebuild the model only when one of its sources raises a change event |
| `UI/RaidHud/RaidHudView.cs:261` | calls `_presenter.Build()` again inside `OnGUI` (Layout + Repaint, so 2-3 builds a frame); also reads mic level, chant progress, mana and game state directly (`:433-541`, `:248`) | **change**: draw only from the presenter's cached model; no reads of other systems |
| `UI/RaidHud/DamageFeedbackView.cs` | per draw: health of each recently hurt target, local player root, game state | **change**: health from `Damage.Dealt` (already subscribed; carry the new health in the event) |
| `UI/RaidHud/CameraShakeDirector.cs:102` | `PlayerStateMachine.Local` every frame | **change**: learn the local player once, by event |
| `Audio/MusicDirector.cs:101-134` | game state, raid phase, Age, alarm state every frame | **change**: subscribe to their change events |
| `UI/GameFlowInput.cs:10` | keyboard + game state | **keep**: input has to be read each frame |
| `UI/BackdropCamera.cs:33` | `Camera.allCamerasCount` | **change** (small): switch on scene/camera change |
| `Audio/AudioDirector.cs:123` | throttled to once a second | **keep** |
| `HUDScreen`, `LairScreen`, `SettingsScreen`, menus | already event- or button-driven | **keep** |

Not changed: simulation that is per-frame by nature (movement, physics, guard ticks, camera follow, item dragging).

## The pattern (one rule per role)

- **Model**: a plain data snapshot (`RaidHudModel` already is one). No Unity lookups.
- **Presenter**: subscribes to the events it needs, updates the model when one fires, and raises one `Changed` for
  its view. It is the only thing that knows which systems feed the view: need to know.
- **View**: draws from the model it was given. It never reaches into another system. IMGUI's `OnGUI` still runs every
  frame (that is drawing, not communication); it just reads a cached model.
- **Events**: small structs (no allocation), raised only on change, carrying the new value so no listener has to look
  it up. Missing change events (alarm level changed, raid phase changed, debt/banked changed, extraction timer
  started/stopped, carried item changed, interaction target changed, game state changed, Age chosen) are added at
  the system that owns the value.

**Bottlenecks.** The bus is main-thread only because Unity's API is. If profiling ever shows publishing as a cost,
the first steps are batching (queue during the frame, deliver once) and fewer subscribers; moving delivery off the
main thread is a last resort, because listeners then cannot touch Unity objects. Nothing is threaded now: no
measurement asks for it.

**Fits the diegetic UI.** The approved diegetic UI (`docs/plans/diegetic-ui-lair-market.md`) replaces most of this HUD.
The change events are what the fires (alarm level), the watch (timer) and the Lair ledger (debt) will listen to, so
this work is reused, not thrown away with the HUD.

## Order

1. Add the missing change events at their owners (one issue).
2. `RaidHudPresenter` and `RaidHudView` onto them; remove the second `Build()` (one issue).
3. `DamageFeedbackView`, `CameraShakeDirector`, `MusicDirector`, `BackdropCamera` (one issue).
4. Tests: each presenter updates on the event and does not rebuild without one; a frame with no events allocates
   nothing and builds nothing.

## Decisions waiting on the owner

1. **How far the move to the bus goes.**
   - **(Recommended)** Everything a presenter or view listens to goes through `EventManager` now (HUD, music, and the
     coming Lair / Market / diegetic UI). Existing system-to-system C# events move only when touched.
   - Migrate all 80 C# events to `EventManager` at once: one mechanism everywhere, but it touches guards, spells,
     loot, networking and many tests in one go.
   - Keep typed C# events as "the bus"; only replace the polling with subscriptions.
2. **Values that change every frame by nature** (mic loudness, chant progress, the countdown).
   - **(Recommended)** Derive or sample: the countdown is one event with its end time and the view works out what is
     left; mic level and chant are sampled by their presenter and published at a capped rate (~15 a second) only
     while active. No per-frame events.
   - An event on every change, even every frame.
   - Leave them as a direct read in the view; events only for discrete changes.

## Decisions (settled 2026-10-06)

1. **Scope: every event between systems, at once.** All plain C# events that one system raises for another move
   onto `EventManager`, along with the enemy director's private `EnemyDirectorBus`. A component talking to its own
   object stays a direct call: a guard's navigator, senses and states (`GuardNavigator.Reached`,
   `GuardHearing.NoiseHeard`, `ChaseState.InReach`, ...), a door's own `OpenStateChanged`, one player's
   `StateMachine.StateChanged`. A guard's reports to the director do go on the bus (#252).
2. **Continuous values are published on every change.** Mic level and chant progress raise an event when they
   change; input raises one on both performed and cancelled. No sampling or rate cap.
3. **The mic readout leaves the raid HUD.** Mic sensitivity and the live level meter belong in Settings, for testing
   and tuning there.
4. **Unsubscribing is part of the contract.** Every subscriber unsubscribes when its screen closes, its state
   changes, its object is disabled or destroyed, or the game quits. A test proves that after a screen closes or a
   scene unloads, nothing it subscribed is still listening.

Before the move, `EventManager` itself needs: to survive scene loads and exist in EditMode tests (it is a scene
singleton today, `PersistBetweenScenes => false`), an `UnsubscribeFromAllEvents` without reflection (it calls
`MethodInfo.Invoke` per event type today), and a way to count live subscriptions for the leak test.
