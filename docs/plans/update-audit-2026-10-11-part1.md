# Update audit, part 1 (2026-10-11)

Read-only audit of the per-frame methods (`Update`, `LateUpdate`, `FixedUpdate`, and state callbacks run every frame)
in the first 33 runtime files that have one. Rule being audited against: per-frame methods are only for work that
genuinely runs every frame; change goes through `EventManager`, timing through coroutines or scheduled calls
(owner, 2026-10-11). Gathered by a read-only scout; line numbers are the ones it read. Part 2 covers the other 33 files
and the tests: [`update-audit-2026-10-11-part2.md`](update-audit-2026-10-11-part2.md).

Buckets: **KEEP** truly per-frame; **EVENT** watches for a change and should listen for it; **TIMER** counts time and
should be a coroutine or scheduled call; **REMOVE** dead, redundant or debug-only.

| `file:line` | What it does | Bucket | Replacement or reason |
|---|---|---|---|
| `Guards/Core/Guard.cs:195` | Steps the guard's brain and movement | KEEP | Drives AI and motion each frame |
| `Items/ItemManager.cs:318` | Places the grab beam after the camera has moved | KEEP | Follows the camera every frame |
| `Net/CarryBeamRelay.cs:74` | Draws another player's carry beam | KEEP | Follows a moving body (send rate not checked) |
| `Items/Item.cs:595` | Spring that pulls a carried item to the hand | KEEP | Physics |
| `Loot/LootPickup.cs:624` | Carried loot's physics | KEEP | Physics |
| `Audio/MusicDirector.cs:131` | Cross-fades music stems | KEEP | A fade changes every frame |
| `Audio/StepAudio.cs:49` | Footstep sounds from movement | KEEP | Reads motion each frame |
| `Core/UI/HealthBar.cs:44` | Turns the bar to face the camera | KEEP | Billboard; but see other issues |
| `Core/StateMachine/BaseStateMachine.cs:22` | Runs the current state each frame | KEEP | The state machine's tick |
| `Atmosphere/NightLookPreview.cs:526` | Fire flicker | KEEP | Visibly changes every frame |
| `Atmosphere/PortalGlow.cs:97` | Portal swirl | KEEP | Visibly changes every frame |
| `Audio/AudioDirector.cs:119` | Volume fades | KEEP | A fade changes every frame |
| `Audio/ImpactAudio.cs:151` | Sliding-object sound | KEEP | Follows motion |
| `Items/Weapons/RangedWeapon.cs:59` | Reload bar | KEEP | Progress changes every frame |
| `Alarm/EnemyDirector.cs:179` | Checks the alarm level for a change | EVENT | Publish `AlarmLevelChanged` from the level's setter |
| `Atmosphere/CastleAtmosphere.cs:188` | Checks settings for a change | EVENT | Listen for the settings-changed event |
| `Atmosphere/CastleFireSpawner.cs:40` | Waits for the castle to exist | EVENT | New `CastleBuilt`, from the castle generator |
| `Atmosphere/NightLookPreview.cs:272` | Waits for the castle to exist | EVENT | `CastleBuilt` |
| `Audio/GuardVoiceDirector.cs:65` | Watches for guard state changes | EVENT | Listen for guard events |
| `Extraction/ExtractionZone.cs:186` | Watches the extraction state | EVENT | The networked value's change callback |
| `Guards/Core/GuardDeathPlayback.cs:21` | Watches for death | EVENT | Listen for the guard's death event |
| `Items/ItemManager.cs:86` | Reads grab/throw input each frame | EVENT | Input System `performed` callbacks |
| `Loot/LootInteractor.cs:55` | Reads interact input each frame | EVENT | Input System `performed` callbacks |
| `Net/PlayerNetworkOwnership.cs:171` | Watches for the local player dying | EVENT | New `LocalPlayerDied` |
| `Enemies/States/MonsterIdleState.cs:26` | 0.2 s throttle | TIMER | Coroutine or scheduled call |
| `Enemies/States/MonsterPatrolState.cs:27` | 0.2 s throttle | TIMER | Coroutine or scheduled call |
| `Enemies/States/MonsterPursueState.cs:19` | 0.2 s throttle | TIMER | Coroutine or scheduled call |
| `Enemies/States/MonsterAttackState.cs:22` | Attack cooldown | TIMER | Coroutine or scheduled call |
| `Enemies/States/MonsterPickedUpState.cs:55` | Time held | TIMER | Coroutine or scheduled call |
| `Guards/Senses/GuardSight.cs:52` | Sight check every so often | TIMER | Scheduled tick |
| `Audio/AudioGapProbe.cs:31` | Diagnostic used only by an eval script | REMOVE | Debug-only, sits in runtime code |
| `Core/StateMachine/BaseStateMachine.cs:27` | Per-frame call that does nothing for monsters | REMOVE | No-op |
| `Enemies/States/MonsterState.cs:18` | Empty per-frame method | REMOVE | Dead |
| `Enemies/States/MonsterDeadState.cs:16` | Empty per-frame method | REMOVE | Dead |

**Counts:** KEEP 14, EVENT 10, TIMER 6, REMOVE 4 (34).

**Other pattern breaks seen:**
- `Enemies/States/MonsterState.cs:4` is marked obsolete, yet every `Monster*State` still inherits it.
- `Camera.main` read every frame (`Items/ItemManager.cs:90`); cache it.
- `Core/UI/HealthBar.cs:46` reads the model directly instead of listening for health events.
- `Alarm/EnemyDirector.cs` does too many jobs (a god class).

**Not checked:** `UpdatePortalFreeze`, and `CarryBeamRelay`'s network send rate.
