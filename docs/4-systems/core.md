# Core

Shared infrastructure every other module sits on: the singleton base, the event bus, and the state
machine contract. Ported from `RogueLikeSlop@ThirdPerson`.

`Plunderspell.Foundation` references nothing. Every other assembly references it, so anything added here is
paid for everywhere — keep it small.

## The event rule

One event bus carries everything one system tells another, and it is `EventManager`. Nothing else in the code is a
bus: `EnemyDirectorListener` is the director's subscriber and `LoopPool` is a pool of audio sources (both were
called "Bus" until #304, which read as duplicate logic and was not). `TestEventBus` only makes an `EventManager`
for Edit Mode tests. The rules, decided 2026-10-06 (`docs/6-decisions/Decisions.md`):

- **What goes on the bus:** an event one system raises for another. A component talking to its own object (a guard's
  navigator, senses and states, a door's own open state, one player's state machine) stays a direct call.
- **Events are structs** (`readonly struct ... : IEvent`) with the new value in them, so publishing allocates nothing
  and no listener has to look the value up. They are published by the system that owns the value, only when it
  changes. Continuous values (mic loudness, chant progress, the raid clock, the alarm level) publish on every change;
  input publishes on both performed and cancelled.
- **Publish with `EventManager.Instance?.Publish(...)`.** The instance is made before the first scene loads and lives
  to quit; with none (an Edit Mode test that did not make one) nothing is delivered.
- **Always unsubscribe.** A listener subscribes in `OnEnable` (or when its screen opens) and calls
  `UnsubscribeFromAllEvents(this)` when it closes, changes state, is disabled or quits. Tests prove it with
  `SubscriptionCount(listener) == 0`.
- **Model, presenter, view.** A model is a plain data snapshot (`RaidHudModel`). A presenter subscribes to the events
  it needs, updates the model when one fires, and is the only thing that knows which systems feed the view. A view
  draws the model it was given and reads no other system. IMGUI's `OnGUI` still runs each frame (drawing is not
  communication); it reads a cached model. The raid HUD is the worked example (`raid.md`).
- **Not every per-frame thing is an event.** Simulation that is per-frame by nature (movement, physics, guard ticks,
  camera follow, a cross-fade) stays an `Update`. A thing that has to react to a camera appearing, which Unity gives no
  event for, is checked on the events that can cause it (`BackdropCamera`, with its limit below).

## How it works

- **`SingletonBase<T>`** — one scene-owned instance per type, claimed in `Awake`. A second instance
  destroys itself. `PersistBetweenScenes` (default true) controls `DontDestroyOnLoad`.
- **`EventManager`** — a typed publish/subscribe bus. Subscribers are held by `WeakReference`, so a
  destroyed listener does not keep its subscriber alive or throw on publish. Publishing is
  reentrancy-safe: unsubscribes that happen *during* a publish are queued and applied afterwards
  rather than mutating the list mid-iteration.
  There is one bus from start-up to quit: `EventManager.CreateForPlay` (`Runtime/Core/Events/EventManager.cs:21`)
  makes it before the first scene loads and it lives in `DontDestroyOnLoad`, so subscriptions survive scene loads
  and no scene has to hold one (#298). `Unsubscribe<T>` and `UnsubscribeFromAllEvents` remove every subscription
  the listener holds. `SubscriptionCount(target)` and `TotalSubscriptionCount` let a test prove a closed screen
  left nothing listening. Every subscriber unsubscribes when it closes, changes state, is disabled or the game
  quits (owner's rule, `docs/6-decisions/Decisions.md`, 2026-10-06).
- **Game-flow events (#300).** These are `IEvent` structs on the bus, not C# events: `GameStateChanged`,
  `PlayerStatsChanged`, `ExtractionStarted/Cancelled/Completed/Progress`, `MicrophoneChanged`,
  `GuardsHearChatterChanged`, `CoopChanged`, `LocalPlayerDied` (`Runtime/Core/GameFlow/GameFlowEvents.cs`);
  `PortalOpened`, `RaidPhaseChanged`, `RaidResolved` (`Raid/RaidEvents.cs`); `ExtractionResolved`,
  `HaulInZoneChanged` (`Extraction/ExtractionEvents.cs`); `RaidContextPublished` (`Inventory/RaidContextEvents.cs`).
  The sources (`GameStateManager`, `PlayerStats`, `ExtractionController`, `AudioInputSettings`, `CoopSession`,
  `RaidDirector`, `ExtractionZone`, `RaidContext`) publish with `EventManager.Instance?.Publish`, so with no bus
  (an Edit Mode test that never made one) nothing is delivered; `TestEventBus.Create()` makes one. Events carry
  the new values, but a view that needs more reads the source (`GameServices.PlayerStats`, `GameServices.Coop`).
- **Combat, spell, item, loot and voice events (#301).** Also bus events: `DamageDealt` (`Core/Interfaces/DamageEvents.cs`);
  `PhraseResolved`, `CastResolved`, `GoldConjured`, `GoldScattered` (`Spells/SpellEvents.cs`); `ItemImpacted`,
  `ItemImpactedRemotely`, `RangedWeaponFired` (`Items/ItemEvents.cs`); `LootRuined`, `LootFocusChanged`
  (`Loot/LootEvents.cs`); `PhraseRecognized`, `ChatterHeard` (`Voice/VoiceEvents.cs`, published by the voice services on
  the main thread, so `IVoiceInputService` no longer has an event); `ChatterResolved` (`Acoustics/AcousticsEvents.cs`).
  Left as direct calls because the listener is part of the same object: `StatusEffectReceiver.StatusChanged` and
  `GuardHealth.Died` (only their own guard listens). `PlayerStateMachine.SlamLanded` (the spell system listens) is not in #301's list and is still a C# event.
- **Change events (#302).** Published by the owner of the value, each time it changes, carrying the new value, so a view
  never has to poll: `AlarmLevelChanged` (`Alarm/AlarmLevelEvents.cs`, from `EnemyDirector.Update` on every peer, since
  PurrNet 1.15 SyncVars have no hooks); `ExtractionTimerChanged` (`Extraction/ExtractionTimerEvents.cs`, same way, every
  frame the clock moves); `DebtChanged`, `BankedGoldChanged`, `AgeChosen` (`Lair/LairEvents.cs`, from `LairHubManager`:
  load, a raid's result, a new session, a client showing the host's campaign, choosing an Age); `CarriedItemChanged`
  (`Items/CarriedItemEvents.cs`, from `ItemManager`); `DoorFocusChanged` beside `LootFocusChanged`
  (`Loot/LootEvents.cs`); `LocalPlayerChanged` (`Player/LocalPlayerChanged.cs`, whenever `PlayerStateMachine.Local`
  changes); `MicLevelChanged` (`Voice/MicLevelEvents.cs`, each time Vosk reads a different loudness);
  `ChantProgressChanged` (`Spells/ChantEvents.cs`, every frame of a chant, then once with `Chanting` false). Input:
  `PlayerAttackEvent`, `PlayerJumpEvent` and `PlayerWalkEvent` are now structs published with `enable` true on performed
  and false on cancelled. `ChangeEventTests` covers the ledger, Age, alarm level, clock and local player; `HudAndInteractionTests` covers loot and
  door focus and `CastingInputTests` the chant. No test yet: mic level (needs a real microphone), the carried item
  (`ItemManager` starts a drag only from a camera ray and an input callback) and the attack/jump/walk input events
  (need the Input System's actions driven).
- **Per-frame reads removed (#304).** `DamageFeedbackView`, `CameraShakeDirector` and `MusicDirector` no longer read other
  systems each frame; they follow the events above. `BackdropCamera` no longer counts cameras every frame: it looks when
  `GameStateChanged` or a scene load says cameras may have changed, and once more the next frame. **Limit:** a camera
  that comes or goes with none of those happening (a player body despawning mid-raid, say) is not noticed until the next
  such event; the backdrop renders under every other camera, so the cost of a stale "on" is only wasted draw, but a stale
  "off" with no camera left shows an empty frame until then. If that is ever seen, `LocalPlayerChanged` is the event to
  add (it lives in the Player assembly, which the UI assembly cannot reference today).
- **`BaseStateMachine` / `IState` / `PlayerState`** — `Enter`/`Update`/`Exit`/`FixedUpdate`. States
  are plain C# objects constructed once in `Awake`, not MonoBehaviours, which keeps their logic
  testable without a scene.

## Invariants

- **`SingletonBase.Instance` never creates anything.** An absent instance means no scene owner has
  been set up yet; that is information the caller needs, not a problem to paper over by spawning a
  GameObject. The one exception is the event bus, made once at start-up by `EventManager.CreateForPlay`.
- **A subclass overriding `OnDestroy` must call `base.OnDestroy()`.** See Traps.

## Spell-target interfaces

`Core/Interfaces/ISpellTargets.cs` declares `IBreakable`, `ILevitatable`, `ISleepable`,
`IStunnable`, `IIgnitable`, `IOpenable` and `IHandOpenable`. They live in Core for a structural
reason: `Plunderspell.Spells` must be able to affect loot, doors and players, all of which sit downstream
of it, so a direct reference would cycle. A spell only ever sees the interface, found by an overlap
query. Anything that should be affectable implements one — or adds `StatusEffectReceiver`, which
implements four of them.

## Dev tooling

- **`Tools/Plunderspell/Build Playable Raid Scene`**
  (`Assets/_Project/Scripts/Editor/RaidSceneBuilder.cs`) builds a complete playable raid from code:
  rooms with doorways, a placeholder haul, a garrison, the extraction zone, the HUD and a player who
  can walk, grab and cast. Everything it generates is placeholder — it is a harness for playing the
  game, not the art pass. Re-running always starts from an empty scene, so it never leaves a second
  castle behind. See `docs/4-systems/raid.md`.

- **`Tools/Headless/verify.sh`** compiles every gameplay and editor assembly and runs the whole test
  suite without Unity. See `Tools/Headless/README.md`.

- **`Tools/Plunderspell/Build Test Scene`** (`Assets/_Project/Scripts/Editor/TestSceneBuilder.cs`) builds a
  throwaway player prefab, flat ground and grabbable item from code and saves them as
  `Assets/_Project/Scenes/TestScene.unity` / `Assets/_Project/Prefabs/Player.prefab`. It exists
  because nothing in the project has ever been run - there was no player prefab and no test scene -
  and the project decision was a reviewable code path over prefab YAML authored blind. Re-running
  always starts from a fresh empty scene rather than adding to what is already there, so it never
  leaves a duplicate ground behind. Tuning (ground size, spawn height) lives in named consts at the
  top of the file. Not shipped gameplay.

  Because it regenerates both assets wholesale, it is also the supported way to repair them after a
  component is deleted from the project - fix the builder and re-run it rather than hand-editing
  the prefab or scene YAML.

- **Save slots** (`Assets/_Project/Scripts/Runtime/Lair/SaveSlots.cs`, main menu Save row). Three
  campaigns, each with its own debt, gold and era, so a cheap or rich campaign can be kept for
  testing debt and loot rates. Slot 1 uses the PlayerPrefs keys saves had before slots existed
  (`TotalDebt`, `AccumulatedGold`, `SelectedEra`), so an existing campaign is slot 1; slots 2 and 3
  prefix them (`Slot2.TotalDebt`). The active slot is `Save.ActiveSlot`. The stepper calls
  `LairHubManager.LoadSlot`; Reset asks twice ("Sure?") and then `LairHubManager.ResetSlot` deletes
  the slot's keys. A client in someone else's session still shows the host's campaign and never
  writes its own slot. Tests: `SaveSlotTests`.

## Traps

- **Unity's fake null and `?.`** — a destroyed `UnityEngine.Object` is not a real C# null. It
  compares equal to null through Unity's overloaded `==`, but `?.` bypasses that overload entirely
  and happily dereferences it. Since call sites are written `EventManager.Instance?.Publish(...)`,
  the getter collapses a destroyed instance to a genuine null (`_instance != null ? _instance : null`)
  so `?.` behaves. Removing that ternary makes every `?.` call site a latent null-ref after teardown.

- **`OnDestroy` is a hiding trap, not an overriding one.** `SingletonBase.OnDestroy` is `protected
  virtual` and clears `_instance`. A subclass that declares `private void OnDestroy()` *hides* it
  rather than overriding it — Unity calls only the most-derived one, the base never runs, and
  `_instance` is left pointing at a destroyed object. The compiler warns (CS0114) but does not error.
  `EventManager` gets this right with `protected override void OnDestroy()` + `base.OnDestroy()`;
  any future singleton must do the same.
