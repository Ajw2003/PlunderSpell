# Guard core inventory (#206, 2026-10-02)

Port rule: every behaviour of the legacy guard, marked keep / change / drop, before the fresh core is written.
Legacy = `Assets/_Project/Scripts/Runtime/Guards/CastleGuard.cs` (frozen copy `docs/reference/guard-legacy/CastleGuard.cs.txt`,
tag `guard-legacy-2026-10-01`; line numbers are the live file's). The keep and drop lists are the owner's, from the
#206 re-scope comment. "Where" says which new class carries a kept behaviour (all under `Runtime/Guards/`).

| Legacy behaviour | Legacy line | Mark | Where in the fresh core, or the re-add issue |
|---|---|---|---|
| State, health and attack-signal SyncVars | 82-89 | keep | `Core/Guard.cs` (SyncVars must be fields of the NetworkBehaviour) |
| Server-only AI (`Update` returns on a client) | 389-395 | keep | `Core/Guard.cs` `Update` |
| `GuardAlertState` replicated enum | GuardAlertState.cs | change | Extended with Combat, OnFire, Dead (owner's spec); Searching stays unused until #214 |
| IHealth, `TakeDamage`, `IsDead` | 125, 1243-1267 | keep | `Core/GuardHealth.cs` |
| Death: `ClearAll` status, publish `GuardDied`, destroy | 1245-1263 | change | Publishes `GuardDied`, enters the Dead state; the Dead state (#213) owns topple and despawn. Core despawns at once as a stub |
| Lobby scaling `ScaleTuning` / `ScaleHealth` | 1288-1305 | keep | `Core/Guard.cs` (same signatures, so the spawner can call either guard) |
| Frango shove | 383-387, 352-380 | change | `Core/GuardShove.cs`: same knock-back by a burst of speed, applied to the transform through a capsule check, not a Rigidbody |
| Status effects (Somnus, Frango, Ignis, Levo) via `StatusEffectReceiver` | 125, 423-526 | keep (receiver); change (Levo) | Core reads `IsIncapacitated`/`IsBurning`; the Levo float-and-fall (`UpdateLevitation`) is **not** ported: owner's answer 2, a levitated guard is Stunned and recovers on landing, which is the Stunned state (#211) |
| Sight: cone, range, `CanSee` | 716-754 | keep | `Senses/GuardSight.cs` |
| Line of sight that ignores the target's own colliders | 736 | change | Raycast hits that belong to the target are skipped (legacy relied on the geometry mask to leave players out) |
| Sight throttled to 10-15 Hz, staggered per guard | 736 (#201) | keep (new) | `Senses/GuardSightThrottle.cs`, 12 Hz, phase from the instance id |
| 20 s arrival grace | 699-714 | keep | `Senses/GuardSight.cs` static `BeginArrivalGrace` / `EndArrivalGrace` |
| Hearing threshold by alarm | 642-660 | keep | `Senses/GuardHearing.cs` via `GuardBrain.ShouldInvestigate` |
| A loud noise wakes sleepers (0.5) | 642-692 | keep | `Senses/GuardHearing.cs` |
| Hearing raises an event, guard does not move itself | 642-660 | change | Hearing publishes to the guard's own `NoiseHeard` event; the current state decides |
| Hue and cry: investigate requests from the director, nearest player, 40 m | 237-286 | change | Core receives `InvestigateRequest` and forwards it to the state (`Guard.InvestigateRequested`); the radius rule lives in `GuardHearing.IsWithinHueAndCryRadius`; the Investigate state is #208 |
| Register with the director, listen for events | 175-183, 242-265 | keep | `Core/GuardDirectorLink.cs` |
| Publish `IntruderSpotted` / `IntruderLost` / `GuardEngaged` / `GuardDied` | 1117-1155, 1170, 1255 | keep | Raised by the states and `GuardCombatSignal` (states #207-#213); the core exposes `Director` |
| Attack signal (`SignalAttack`, client replay, `Attacked` event) | 186-195, 339-346 | keep | `Core/GuardAttackSignaller.cs` |
| Melee strike / ranged `FireAt`, cooldown | 1157-1222 | change | Out of core scope: the Combat state (#210) and Chase (#209) own attacking. Not ported here |
| Movement by NavMeshAgent plus a dynamic Rigidbody | 148-172, 352-380 | drop | Replaced by director navigation (the sweep keeps the #200 rule). No re-add issue: #223 switches the NavMesh off |
| Movement requests, arrival, path result | 976-1014 | change | `Movement/GuardNavigator.cs`: only `MoveRequest` out, `PathReady`/`Arrived`/`Blocked` in |
| Guards ignoring each other's colliders | 175-200 | drop | Spacing is the service's `GuardSeparation`; the guard has no body to jam |
| Slippery physics material | 203-218 | drop | No physics body |
| Snap to mesh, `Reachable`, resend 0.75 m, `HasArrivedAt` | 944-975, 1114 | drop | The service plans on the castle graph and raises Arrived |
| Steer (no-agent straight-line move) | 1097-1112 | drop | Owner's answer 4. No re-add issue; the state machine controls everything |
| Stuck watchdog: refresh path | 1017-1060 | drop | The service raises Blocked (obstacle) instead |
| Stuck watchdog: detour and skip | 1061-1095 | drop | Re-add if needed: **#226** |
| Search sweep (`Search`, `NextSweepGoal`, `SweepOffset`) | 822-850 | drop | Replaced by Investigate (#208), owner's answer 1. No separate re-add issue |
| `KeepHunting` re-send at the hue and cry | 566-596 | drop | The director's hue and cry request replaces it; the Investigate state decides (#208) |
| Patrol route from spawner transforms | 875-900, 1274-1286 | drop | Re-add if needed: **#225**. Patrol picks its own random reachable points (#207) |
| Wander near the post | 902-925 | change | Becomes the Patrol state (#207); the core's placeholder `PlaceholderPatrolState` only requests moves |
| Overheard chatter (`Overhear`, `LastOverheard`, `Overheard`) | 662-690 | drop | Re-add if needed: **#228** |
| Alarm-scaled vision (`GuardBrain.SightRange`) | 722, 1160 | drop | Re-add if needed: **#229**. Core sights at the base range |
| The shout as noise (`RaiseTheCry`, `AlertGuardsNear`) | 307-320, 1129-1136, 1233-1238 | drop | Re-add if needed: **#227** |
| `IntruderSpotted` first-sighting scoring | 1117-1155 | change | Same event; raised by the Chase state (#209), no shout |
| Alarm-scaled move speed (`GuardBrain.MoveSpeed`) | 756-762 | change | Stays a pure function in `GuardBrain`; the states pass the speed they want (#207-#209) |
| `SetAlertState` test seam, `Tick` public, `Configure` | 1224-1230, 528, 1274 | change | `Guard.Tick` and `Guard.ForceState` stay as seams; `Configure(director)` stays; the route argument is dropped with #225 |
| Fall damage on landing from Levo | 500-526 | drop | Stunned state (#211) decides what landing does. No separate issue |

## Drops with no re-add issue, as the owner's list stands

Dynamic physics body, Steer, the Search sweep, Levo's float-and-fall. Each is replaced rather than removed
(navigation sweep, state machine, Investigate, Stunned), so none needs a "re-add if needed" issue.

## Verified at the end

See the "Verification" section appended below once the core is built.
