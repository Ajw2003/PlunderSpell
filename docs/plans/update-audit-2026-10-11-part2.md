# Update audit, part 2 (2026-10-11)

Read-only audit against the owner's rule of 2026-10-11 (Update/LateUpdate/FixedUpdate only for work that must run
every frame; state changes through `EventManager`; timing in coroutines or scheduled calls; MVC, singletons,
factories, SOLID) and the event rules in `docs/4-systems/core.md` and `docs/6-decisions/Decisions.md` (2026-10-06
entries). One row per method, or per distinct part of a method. Every `file:line` below was read on 2026-10-11.

`HaulLanding.cs` has uncommitted work-in-progress edits on branch `AjsAgent/lair-items-378`; it is audited as it is
on disk, so its line numbers may move when that work is committed.

Paths are under `Assets/_Project/Scripts/Runtime/`. The player state callbacks are dispatched every frame by
`Core/StateMachine/BaseStateMachine.cs:22` (Update) and `:27` (FixedUpdate); that file is not in this audit's list.

## Runtime methods

| `file:line` | method | what it does | bucket | replacement or reason | other issues |
|---|---|---|---|---|---|
| `Net/SteamBootstrap.cs:72` | Update | Pumps Steamworks callbacks while Steam is ready. | KEEP | Steamworks.NET only delivers callbacks when `SteamAPI.RunCallbacks` is called; there is no push. | none |
| `Player/PlayerInputController.cs:49-55` | Update (menu gate) | Every frame a menu is open, zeroes look, movement and creep. | EVENT | `GameStateChanged`: zero them once when `AcceptsInputIn(e.Current)` turns false. | Reads `GameServices.GameState` every frame. |
| `Player/PlayerInputController.cs:57` | Update (creep) | Polls the C key with `IsPressed` and copies it to `Creeping`. | EVENT | `_creep.performed` / `_creep.canceled` callbacks set `Creeping` (input publishes on performed and cancelled, core.md). | none |
| `Player/PlayerInputController.cs:60-64` | Update (rotate lock) | Stops looking while `ItemManager.IsRotatingObject` is true. | EVENT | New `ItemRotationChanged(bool)` published by `ItemManager` when the middle button goes down/up while dragging; `IsRotatingObject` itself polls `Mouse.current.middleButton` (`Items/ItemManager.cs:49-50`). | Singleton read every frame. |
| `Player/PlayerInputController.cs:66` | Update (look) | Applies the stored mouse delta to the camera. | KEEP | Camera rotation follows the mouse every frame. | `PlayerStateMachine.Look` (`Player/PlayerStateMachine.cs:141-142`) scales a mouse delta by `Time.deltaTime`, so turn speed depends on frame rate. Bug outside Update: `Awake` (`:23`) calls `EnableAllInputs` on every `PlayerIdleEvent`, which `+=`s the move/jump/attack/look/click handlers again each time (`:73-75`, `:91-92`, `:129-130`, `:155-156`, `:181-182`) without removing them, so handlers stack; that subscription is never removed (`OnDestroy` `:208-216` has no `UnsubscribeFromAllEvents`). `PlayerIdleEvent` is a class, not a struct (`Core/Events/PlayerInputEvents/PlayerIdleEvent.cs:3`). |
| `Player/PlayerState.cs:19` | Update (virtual) | Empty per-frame hook for states. | KEEP | The dispatch hook the walk and idle states use for movement. | none |
| `Player/PlayerState.cs:27` | FixedUpdate (virtual) | Empty physics-step hook for states. | KEEP | The dispatch hook the dodge and jump states use for physics. | none |
| `Player/PlayerStateMachine.cs:381` | FixedUpdate | Ground check, Saltus slam, the state's physics step, extra fall gravity. | KEEP | Rigidbody physics every step. | 573-line class that is health, choke source, body, spell-movable, look and state machine at once (God class). `SlamLanded` is still a C# event (core.md says so). |
| `Player/States/PlayerDodgeState.cs:28` | FixedUpdate (state) | Holds the dash velocity until the dash time is over, then hands back to walk/idle. | KEEP | Velocity is rewritten every physics step; the end check rides the same step. | none |
| `Player/States/PlayerIdleState.cs:16` | Update (state) | Zeroes horizontal velocity every frame. | KEEP | A physics constraint that must hold while idle. | Writes rigidbody velocity in Update, not FixedUpdate. |
| `Player/States/PlayerJumpState.cs:20` | FixedUpdate (state) | Air steering and the landing check. | KEEP | Physics forces every step. | none |
| `Player/States/PlayerWalkState.cs:10` | Update (state) | Sets velocity from camera-relative input times the pace. | KEEP | Movement. | Writes rigidbody velocity in Update, not FixedUpdate. Reads `ItemManager.Instance.CarriedItem` every frame: cache it from `CarriedItemChanged`. Logs a warning every frame while `CameraTransform` is null (`:14`). |
| `Player/WizardAnimationDriver.cs:75` | Update (own view) | Checks every frame whether this body is the local one, to draw it shadow-only. | EVENT | `LocalPlayerChanged` (carries the player). | none |
| `Player/WizardAnimationDriver.cs:76` | Update (facing) | Turns the model to the camera's yaw. | KEEP | Follows the view every frame. | none |
| `Player/WizardAnimationDriver.cs:78-83,95` | Update (speed) | Measures and smooths ground speed and sets the Speed parameter. | KEEP | Animation blending. | none |
| `Player/WizardAnimationDriver.cs:85-91,99` | Update (down) | Watches `dead` / `RemoteDown` for a change and sets Dead. | EVENT | Same-object direct call (2026-10-06 rule): `PlayerStateMachine` on death and the `RemoteDown` setter call the driver once. | none |
| `Player/WizardAnimationDriver.cs:92-93` | Update (vanish) | Hides the body once it has been down for `_vanishAfterSeconds`. | TIMER | Coroutine started on going down, waits `_vanishAfterSeconds`, then `SetVisible(false)`. | none |
| `Player/WizardAnimationDriver.cs:96` | Update (crouch) | Sets Crouch from `Creeping` every frame. | EVENT | Same-object direct call from the `Creeping` setter (and the remote flag setter). | none |
| `Player/WizardAnimationDriver.cs:97` | Update (airborne) | Sets Airborne from the grounded flag. | KEEP | Grounded changes on physics steps; a cheap per-frame copy. | none |
| `Player/WizardAnimationDriver.cs:98` | Update (casting) | Sets Casting from `PushToCastController.IsCasting`. | EVENT | `CastingStateChanged` (filter `e.Controller == _cast`); remote via the flag setter. | none |
| `Playtest/CombatBenchHud.cs:41` | Update | Polls F1 with `WasPressedThisFrame` to toggle the panel. | EVENT | `GameInput.Actions.Debug.ToggleBenchHud.performed` callback. | `OnGUI` allocates a `List` (`:131`), a `string[]` (`:104`) and a `GUIStyle` (`:152`) on every call. |
| `Playtest/FreeLookPlaytestController.cs:63` | Update (gate) | Skips everything unless playing. | EVENT | `GameStateChanged` sets a cached flag. | Harness only (ItemGym). |
| `Playtest/FreeLookPlaytestController.cs:66` | Update (look) | Mouse look. | KEEP | Camera follows the mouse. | none |
| `Playtest/FreeLookPlaytestController.cs:67` | Update (stance) | Polls Dodge/Sprint `IsPressed` for the stance. | EVENT | performed/canceled callbacks on Dodge and Sprint. | none |
| `Playtest/FreeLookPlaytestController.cs:70` | FixedUpdate | Moves the body and counts footsteps; gated on playing. | KEEP | Physics movement. | Jump read with `IsPressed` inside FixedUpdate (`:116`); the play gate polls `GameServices.IsPlaying` (`:72`). |
| `Raid/CounterSlate.cs:49` | Update | Cross-fades the slate text after `Show`. | KEEP | Alpha visibly changes every frame during the fade. | Idles with an early return the rest of the time; a coroutine started in `Show` would leave no Update. |
| `Raid/HaulLanding.cs:60-65` | Update (teardown) | Each frame, if the server is not running, forgets a torn-down pile. (WIP file) | EVENT | `CoopChanged` ("session's players, lobby or connection changed", `Core/GameFlow/GameFlowEvents.cs:58`): check `ServerReady` there. | WIP, uncommitted. |
| `Raid/HaulLanding.cs:66-70` | Update (first restore) | Restores the pile from the save on the first frame the server is ready. (WIP file) | EVENT | Same `CoopChanged` handler; plus `OnEnable` when already ready. | WIP, uncommitted. |
| `Raid/HaulLanding.cs:74-75` | Update (count) | Counts live pieces every frame and saves when the count differs. (WIP file) | EVENT | New `HaulPileChanged` published by the pile's `LootSpawner` on spawn/remove/destroy (or a piece's `OnDestroy`); `Remove` (`:128-132`) already saves directly. | `LiveCount` iterates the pile with `TryGetComponent` every frame (`:99-108`). |
| `Raid/HaulLanding.cs:76-81` | Update (moved check) | Every 2 s on the server, saves if any piece moved more than 5 cm. (WIP file) | TIMER | Coroutine or `InvokeRepeating` every `MoveCheckInterval` while restored on the server; also save on `CarriedItemChanged` to null (a piece put down). | `RaidTable`/`AllTables` use `FindObjectsByType`/`FindFirstObjectByType` (`:228`, `:240`), on land and restore only. |
| `Raid/HostSetsOutLine.cs:53` | Update | Eases the "host sets out" line in and out after `Show`. | KEEP | Alpha visibly changes every frame for one second. | Could be a coroutine started in `Show`. |
| `Raid/LairCenturyDial.cs:56-57` | Update (key) | Polls Interact with `WasPressedThisFrame` and turns the Age. | EVENT | `Interact.performed` callback, then `CanTurn`. | `FindFirstObjectByType<LairHubManager>` every frame until found (`:51-52`). |
| `Raid/LairCenturyDial.cs:59-61` | Update (era watch) | Reads the selected era every frame and starts a turn when it differs. | EVENT | `AgeChosen` (carries `Era`; published on host select `Lair/LairHubManager.cs:140` and on clients `:149`). | View reads the model directly. |
| `Raid/LairCenturyDial.cs:62` | Update (ease) | Turns the rings and fades the plaque during a turn. | KEEP | Visible animation over `TurnSeconds`. | Could be a coroutine started by the `AgeChosen` handler. |
| `Raid/LairLedgerBook.cs:51-52` | Update (last raid) | Compares the last-raid worth and left-behind every frame. | EVENT | New `LastRaidChanged(float worth, int leftBehind)` published by `LairHubManager` at `:159`, `:165`, `:302` and `:81-82`. | `FindFirstObjectByType` every frame until found (`:46-47`). |
| `Raid/LairLedgerBook.cs:53-54` | Update (redraw) | Redraws when a handler marked the book dirty. | EVENT | Call `Redraw` from the existing handlers (`:33-38`); no dirty flag. | View reads `LairHubManager` directly instead of event payloads (`:68-74`). |
| `Raid/LairRoomSpawner.cs:35` | Update | Waits every frame for a local player to place after arriving in the Lair. | EVENT | `LocalPlayerChanged`; place at once in the `GameStateChanged` handler when `Local` already exists. | none |
| `Raid/RaidBootstrapper.cs:111-119` | Update (auto-start) | Counts `_startDelay` and starts a raid once. | TIMER | `Invoke`/coroutine from `Start` when `_autoStart`. | none |
| `Raid/RaidBootstrapper.cs:121-122` | Update (debug key) | Polls CallExtraction with `WasPressedThisFrame`. | EVENT | `Debug.CallExtraction.performed` callback. | Debug control. |
| `Raid/RaidBootstrapper.cs:124-125` | Update (debug key) | Polls NextRaid with `WasPressedThisFrame`. | EVENT | `Debug.NextRaid.performed` callback. | Debug control. |
| `Raid/RaidDirector.cs:192-193` | Update (client build) | On a client, builds the castle when phase and seed are both in. | REMOVE | `OnLayoutReplicated` (`:651-656`) and `OnPhaseReplicated` (`:697-698`) already build on whichever arrives second. | While `BuildCastle` cannot build (no generator, `:297-300`) this retries and logs every frame. |
| `Raid/RaidDirector.cs:196-200` | Update (seats) | Every 0.5 s in the Lair, recounts which seats have a wizard. | EVENT | `CoopChanged` (players changed) and `LocalPlayerChanged`; or a new `PlayerBodyChanged` published from `PlayerStateMachine` spawn and `OnDestroy` (`Player/PlayerStateMachine.cs:307`). | `SeatsPresent` runs `FindObjectsByType` and allocates a `bool[]` each time (`:206-218`). 772-line class (phase, layout, castle build, campaign/ledger/era sync, listener fallback, seats): God class. |
| `Raid/RaidListenerFallback.cs:27` | LateUpdate | Every frame, enables its listener only if no camera has one. | EVENT | As `BackdropCamera` (Decisions 2026-10-06 later): check on `GameStateChanged`, scene load and `LocalPlayerChanged` (the Raid assembly already uses `PlayerStateMachine`, e.g. `Raid/LairRoomSpawner.cs:37`). Same accepted limit: a listener toggled with none of those is missed. | `TryGetComponent` on every camera every frame. |
| `Raid/RoomTravel.cs:23` | Update | Polls Interact and travels if the door is looked at. | EVENT | `Interact.performed` callback. | Every door polls the same key. |
| `Raid/SellCounter.cs:116-121` | Update (clear line) | Clears the vendor's line `LineSeconds` after it was shown. | TIMER | Coroutine/`Invoke` started in `Show` (`:303-309`), cancelled by the next line. | none |
| `Raid/SellCounter.cs:122-127,137-138` | Update (keys) | Reads 1/2/3 via `ReadKeys` (`:141-146`) when near and a haggle or line is open. | EVENT | Haggle.Plus/Satis/Vale `performed` callbacks routed to the nearest counter, as `HaggleVoiceRouter` routes spoken words via `ListeningDistance`. | `Camera.main` every frame in `PlayerIsNear` (`:162`). |
| `Raid/SellCounter.cs:129-133` | Update (piece gone) | Ends the haggle when the piece left the counter or was destroyed. | EVENT | `OnTriggerExit` on the counter volumes, plus `CarriedItemChanged` when the piece is picked up. | none |
| `Raid/SellCounter.cs:135-136` | Update (open) | Searches the counter volumes for a resting piece every frame. | EVENT | `OnTriggerEnter`/`OnTriggerStay` on `_top`/`_foot`; check `Rests` only while a candidate is inside. | Every frame on every server counter: `new[] { _top, _foot }` (`:169`), allocating `Physics.OverlapBox` (`:173`), `GetComponentInParent` per hit (`:176`). |
| `Spells/SpellCastingSystem.cs:183` | Update (mana) | Regenerates mana by whole points. | TIMER | Coroutine started when mana drops below max (`PlayerStatsChanged`), stopping at full. | Reads `GameServices.PlayerStats` every frame. |
| `Spells/SpellCastingSystem.cs:185-190` | Update (chant end) | Casts the chanted spell when the chant time is up. | TIMER | Coroutine/`Invoke` for `KeyboardCastSeconds` started when the chant starts. | none |
| `Spells/SpellCastingSystem.cs:192` | Update (chant publish) | Publishes chant progress each frame while chanting, then once on end. | KEEP | Continuous value, publish on every change (Decisions 2026-10-06). | Could live inside the chant coroutine. |
| `Spells/Vfx/SpellBurst.cs:142` | Update | Grows and fades the burst, then destroys it. | KEEP | Visible animation each frame. | none |
| `Status/StatusEffectReceiver.cs:87,93` | Update / Tick | Counts down burn, stun, sleep, levitate and applies burn damage. | TIMER | Coroutine per active status (burn as a damage tick), or `enabled = false` when nothing is active and re-enabled by `Ignite`/`Stun`/`Sleep`/`Levitate`; keep `Tick` as the test seam. | Runs on every actor every frame with no status active. |
| `UI/GameFlowInput.cs:10` | Update | Polls `Keyboard.current.escapeKey` and pauses/resumes. | EVENT | A Pause `InputAction` `performed` callback. | Hard-coded key bypasses the action map; summary mentions Tab = inventory but there is no Tab code (`:7`). |
| `UI/RaidHud/CameraShakeDirector.cs:115` | Update | Decays trauma and writes the view shake onto the local player. | KEEP | Visible shake every frame. | Writes `ViewShake` every frame even at zero trauma. |
| `UI/RaidHud/DamageFeedbackView.cs:313-322` | Update (expiry) | Drops damage numbers and hurt lines past their lifetime. | TIMER | Remove each entry by coroutine/scheduled call when added, or skip when the lists are empty. | none |
| `UI/RaidHud/DamageFeedbackView.cs:323` | Update (vignette) | Fades the hit vignette. | KEEP | Visible fade. | none |
| `UI/RaidHud/DamageFeedbackView.cs:325-335` | Update (flash) | Restores flashed renderers after `k_flashSeconds`. | TIMER | Coroutine per flash started in `StartFlash` (`:264`). | none |
| `UI/RaidHud/DamageFeedbackView.cs:337-348` | Update (bars) | Forgets health bars not hurt for `k_barSeconds`. | TIMER | Scheduled removal per target on hit. | 535-line view that keeps its own state; `Camera.main` every `OnGUI` (`:357`). |
| `UI/RaidHud/GrimoireView.cs:55` | Update | Polls the mouse wheel to turn pages while the book is open. | EVENT | A scroll `InputAction` `performed` callback in a presenter/controller. | A view reading input breaks the core.md MVC rule ("a view draws the model it was given"). `Label`/`LabelRight` allocate a `GUIContent` per label each `OnGUI` (`:176`, `:185`). |
| `UI/RaidHud/HudHoldKeys.cs:45` | Update (watch key) | Polls T and publishes `WatchRaised` on change. | EVENT | T `InputAction` performed/canceled callbacks. | Class summary (`:9-10`) says input must be read each frame; the Input System delivers it as callbacks. |
| `UI/RaidHud/HudHoldKeys.cs:48` | Update (book key) | Polls Tab and publishes `GrimoireOpened` on change. | EVENT | Tab `InputAction` performed/canceled callbacks. | none |
| `UI/RaidHud/HudHoldKeys.cs:48` (auto-open) | Update (first raid) | Keeps the book open until `_autoOpenUntil`. | TIMER | Coroutine started in `OnGameStateChanged` (`:38`) that closes after `FirstRaidSeconds`. | none |
| `UI/RaidHud/HudHoldKeys.cs:44` | Update (play gate) | Polls `GameServices.IsPlaying`. | EVENT | `GameStateChanged`, already subscribed at `:25`. | none |
| `UI/RaidHud/HudHoldKeys.cs:49` | Update (carrying) | Polls `ItemManager.CarriedItem` to block the book. | EVENT | `CarriedItemChanged`. | none |
| `Voice/MockVoiceInputService.cs:102` | Update | While listening, polls digits 1-8 and emits a phrase. | EVENT | `InputAction`s for 1-8, modifiers read in the callback. | `Debug.Log` on every emit (`:158`). |
| `Voice/PushToCastController.cs:54-61` | Update (gate) | Ends casting when not playing or in the Lair room. | EVENT | `GameStateChanged`. | none |
| `Voice/PushToCastController.cs:63-71` | Update (key) | Polls V pressed/released to open and close the mic. | EVENT | V `InputAction` started/canceled callbacks. | Hard-coded `Key`, not the action map. Summary (`:13-16`) names `IVoiceInputService.OnPhraseRecognized`, which core.md says no longer exists. |
| `Voice/VoskVoiceInputService.cs:630` | MainThreadPump.Update | Reads new microphone samples into the recogniser and meter. | KEEP | Unity's `Microphone` gives no callback; the ring buffer must be drained; it returns at once with nothing open (`:431`). | Hidden helper GameObject per service. |

## Tests that wait by polling or by hope

Paths are under `Assets/_Project/Scripts/Tests/Runtime/`.

| `file:line` | what it waits for | deterministic replacement |
|---|---|---|
| `BackdropCameraTests.cs:71-72` | Two frames after publishing `GameStateChanged`, hoping the backdrop has looked. | Assert straight after `Publish` if the handler is synchronous (not checked: `BackdropCamera.cs` is outside this audit's list). |
| `BackdropCameraTests.cs:77-78` | Same, after the second `GameStateChanged`. | Same. |
| `CastingInputTests.cs:87`, `:93` | A frame for `PushToCastController.Update` to poll V. | With an action callback, `InputSystem.Update()` after `Press`/`Release`, then assert. |
| `CastingInputTests.cs:116`, `:119`, `:123` | Frames for PushToCast and `MockVoiceInputService.Update` to poll V and 6. | Same. |
| `CastingInputTests.cs:131` | `WaitForSeconds(KeyboardCastSeconds + 0.2f)` hoping the chant has finished. | Wait (bounded) for `CastResolved`, or drive the chant clock through a test seam like `StatusEffectReceiver.Tick`. |
| `CastingInputTests.cs:167`, `:169`, `:172` | Frames for the key polls. | `InputSystem.Update()` after each key. |
| `CastingInputTests.cs:177` | `WaitForSeconds` to show nothing was cast. | Advance the chant clock by a seam past `KeyboardCastSeconds`, then assert. |
| `CastingInputTests.cs:200`, `:202` | Frames for the digit poll. | `InputSystem.Update()`. |
| `CastingInputTests.cs:222` | A frame for PushToCast's state gate and key poll. | Gate on `GameStateChanged`; `InputSystem.Update()`. |
| `ChangeEventTests.cs:88` | A frame for `AlarmLevelChanged`, published from `EnemyDirector.Update` (core.md). | Publish from the level setter, or tick the director directly. |
| `GrimoireTests.cs:79`, `:83`, `:98`, `:103` | Frames for `HudHoldKeys.Update` to poll Tab. | Tab action callback; `InputSystem.Update()`. |
| `GrimoireTests.cs:117`, `:122`, `:124` | Frames for `HudHoldKeys.Update` to read `_autoOpenUntil` after a state change. | Open the book in the `GameStateChanged` handler; assert right after `ChangeState`. |
| `IntegrationTests/PlunderspellIntegrationTests.cs:85` | `WaitForSeconds(1f)` hoping three clients connected. | Bounded `WaitUntil` on the harness's connected count, or its connect callback. |
| `IntegrationTests/PlunderspellIntegrationTests.cs:103` | `WaitForSeconds(0.5f)` hoping the seed replicated. | Bounded wait on each client's SyncVar `onChanged`. |
| `LootSettleAndInputGatingTests.cs:311`, `:317`, `:324`, `:341` | Frames for `PlayerInputController.Update`'s menu gate. | Gate on `GameStateChanged`; assert right after `ChangeState`. |
| `MicMeterTests.cs:35` | `WaitForSeconds(1.5f)` hoping the meter published. | Bounded wait for the first `MicLevelChanged`. |
| `MicMeterTests.cs:41` | `WaitForSeconds(0.5f)` to show it stopped. | Assert no `MicLevelChanged` beyond the reset after a fixed number of pump frames. |
| `PlayableLoopTests.cs:179` | `WaitForSeconds(0.5f)` hoping the zone polled the loot and player. | Call the zone's check directly, or wait for `HaulInZoneChanged`/`ExtractionStarted`. |
| `PlayableLoopTests.cs:185-186` | Real-time loop until `ExtractionResolved` or the full countdown plus 2 s. | Shorten the duration or tick the extraction clock directly; the event subscription is already there. |
| `PlayableLoopTests.cs:211`, `:215` | `WaitForSeconds(0.5f)` hoping the zone noticed the player arrive/leave. | Wait for `ExtractionStarted`/`ExtractionCancelled`. |
| `PlayableLoopTests.cs:234` | `WaitForSeconds(0.3f)` hoping the dragged item settled. | `Physics.Simulate` a fixed number of steps, or a bounded wait on distance. |
| `RaidListenerFallbackTests.cs:38`, `:42` | A frame for `RaidListenerFallback.LateUpdate` to rescan cameras. | After the EVENT change above: publish the event, assert at once. |
| `RaidSceneCastingTests.cs:45-46` | Up to 120 frames polling `FindFirstObjectByType<SpellCastingSystem>`. | Bounded wait on `LocalPlayerChanged`. |
| `RaidSceneCastingTests.cs:120-123` | Up to 0.5 s polling `FindObjectsByType<SpellBurst>` counts. | Bounded wait on `CastResolved`, then count once. |
| `SavedSettingsStartupTests.cs:61-62` | Two frames hoping the start-up path re-pushed the saved levels. | Call the re-apply directly, or wait on an event it publishes (mechanism not read in this audit). |
| `WatchTests.cs:82`, `:86`, `:99`, `:105`, `:109` | Frames for `HudHoldKeys.Update` to poll T and the play gate. | T action callback and `InputSystem.Update()`; gate on `GameStateChanged`. |

Looked at and left out, because the wait is the thing under test or Unity requires it: one-frame waits for `Start` or
deferred `Destroy` (`CombatBenchTests.cs`, `CombatBenchCastingTests.cs`, `GuardDeadStateTests.cs:86,90`,
`SpellVfxTests.cs:147`, `RaidSceneCastingTests.cs:36,78,88,99,138`, `BackdropCameraTests.cs:61,89,106`,
`CastingInputTests.cs:82,160,219`, `GrimoireTests.cs:95`); `GuardInvestigateTests.cs:97` (a frame deliberately passes,
per its comment); per-frame behaviour being measured (`CarryFeelTests.cs:75`, `PlayableLoopTests.cs:239-240,292,302,315,319-320`,
`ChangeEventTests.cs:105-106`, `BackdropCameraTests.cs:94-95`); the network round trip already fed by
an event (`RaidSceneCastingTests.cs:167-169`); the burst self-destruct timeout (`SpellVfxTests.cs:110-114`); mixer
diagnostics logged per frame (`SavedAudioSettingsTests.cs:97,108,187,213`); screenshot render frames
(`PlayMode/UIScreenshotPlayModeTests.cs:54,112,126`).

## Counts

Runtime rows, one bucket each:

| bucket | rows |
|---|---|
| KEEP | 22 |
| EVENT | 35 |
| TIMER | 11 |
| REMOVE | 1 |
| total | 69 |

Test waits listed: 26 rows in the tests table.
