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

Checked against this table on 2026-10-02, after building the core (tests in `Tests/Runtime/GuardCore*Tests.cs`, 17 pass):

- **Kept and tested:** state, health and attack-signal SyncVars; server-only AI; IHealth; lobby scaling; the Frango shove;
  sight cone, own-collider line of sight, 12 Hz throttle with stagger, the 20 s grace; hearing threshold by alarm and the
  loud-noise wake; a move reaching Arrived through the service.
- **Kept, not tested:** the hue and cry radius and nearest-player rule (`GuardDirectorLink`), the client replay of the
  attack signal and state (needs a networked run), `GuardArrivalGrace` being begun by `RaidDirector`.
- **Deferred to the state issues, as marked above:** attacking (#210), `IntruderSpotted`/`IntruderLost`/`GuardEngaged`
  publishing (#209, #210), the real Patrol, Stunned and Dead (#207, #211, #213).
- **Dropped as listed**, none ported. Re-add issues: #225 spawner routes, #226 detour/skip, #227 shout as noise, #228 chatter,
  #229 alarm-scaled vision.

## Parity check (#214, 2026-10-02)

Read-only check by reading the code; nothing was run (the Editor was busy with a test run). Paths are under
`Assets/_Project/Scripts/Runtime/`. One line per row of the table above, in table order.

Keep and change rows:

- State, health and attack-signal SyncVars: **matches**. `Guards/Core/Guard.cs:29-31` are fields of the NetworkBehaviour.
- Server-only AI: **matches**. `Guard.cs:180-185` `Update` ticks only when `IsAuthority`; clients run nothing.
- `GuardAlertState` enum: **matches**. `Guards/GuardAlertState.cs:21-28` adds Combat, OnFire, Dead; Searching (line 19) is set by no state (only mapped for audio, `Audio/AudioLookups.cs:423`).
- IHealth, `TakeDamage`, `IsDead`: **matches**. `Guards/Core/GuardHealth.cs:31-42`, exposed at `Guard.cs:77-86`.
- Death (clear status, publish `GuardDied`, despawn): **matches**, updated by #213. `Guard.cs:233-238` clears status, publishes, enters Dead; `States/DeadState.cs:21-28` leaves the registries and closes eyes and ears; `Core/GuardDeathPlayback.cs:37-40` destroys after the fade (the "despawn at once" stub in the row is gone).
- Lobby scaling: **matches**. `Guard.cs:216-224`, same two signatures as legacy `CastleGuard.cs:1289,1298`.
- Frango shove: **matches**. `Core/GuardShove.cs:31-50` carries the transform with a capsule check (`:52-67`); wired at `Guard.cs:89,193`.
- Status effects (receiver; Levo changed): **matches**. `Guard.cs:227-231` and `States/GuardStateSet.cs:58-66` read `IsIncapacitated`/`IsBurning`; Levo is a Stunned state that recovers on landing (`States/StunnedState.cs:33-43`, `Core/GuardLanding.cs:22-37`), no float-and-fall ported.
- Sight cone, range, `CanSee`: **matches**. `Senses/GuardSight.cs:87-90` calls `GuardBrain.CanSee`.
- Line of sight ignoring the target's colliders: **matches**. `GuardSight.cs:99-106` skips hits under the target.
- Sight throttle 12 Hz, staggered: **matches**. `Senses/GuardSightThrottle.cs:14,23`, used at `GuardSight.cs:58`.
- 20 s arrival grace: **differs (location only)**. Behaviour matches (`Senses/GuardArrivalGrace.cs:14,20`, honoured at `GuardSight.cs:52`, begun at `Raid/RaidDirector.cs:216`), but the row names `GuardSight.cs` `BeginArrivalGrace`/`EndArrivalGrace`; they are `GuardArrivalGrace.Begin`/`End` in their own file. Row text is stale.
- Hearing threshold by alarm: **matches**. `Senses/GuardHearing.cs:49` via `GuardBrain.ShouldInvestigate` (`Guards/GuardBrain.cs:68-79`).
- Loud noise wakes sleepers (0.5): **matches**. `GuardHearing.cs:17,42-43`. A sleeping guard no longer hears noises too quiet to wake it: `GuardHearing.cs:45-47` returns before the noise can become a lead (today's fix).
- Hearing raises an event, guard does not move itself: **matches**. `GuardHearing.cs:23,53` raises `NoiseNoticed`; `Core/GuardLeads.cs:33` turns it into a lead; the state decides (`States/PatrolState.cs:53-54`). Row calls it `NoiseHeard`; the event is `NoiseNoticed`.
- Hue and cry investigate requests, 40 m, nearest player: **matches**. `Core/GuardDirectorLink.cs:16,61-73`; forwarded to `GuardLeads.cs:32`; Investigate is `States/InvestigateState.cs`. Row names `GuardHearing.IsWithinHueAndCryRadius`; the rule is in `GuardDirectorLink`.
- Register with the director, listen for events: **matches**. `GuardDirectorLink.cs:39-48`.
- Publish `IntruderSpotted` / `IntruderLost` / `GuardEngaged` / `GuardDied`: **matches**. Spotted and Lost at `States/ChaseState.cs:57,63` and `States/CombatState.cs:60,67`; Engaged at `Core/GuardMeleeAttack.cs:42` and `Core/GuardRangedAttack.cs:64`; Died at `Guard.cs:236`.
- Attack signal and client replay: **matches**. `Core/GuardAttackSignaller.cs:31-46`, started on clients at `Guard.cs:146`.
- Melee strike / ranged `FireAt`, cooldown: **matches**, now done by #209/#210 rather than "not ported". `Core/GuardMeleeAttack.cs:31-49` (1.4 s cooldown, reach), `Core/GuardRangedAttack.cs`, turn-taking in `States/CombatState.cs:138-152`.
- Movement requests, arrival, path result: **matches**. `Movement/GuardNavigator.cs:67-74` sends `MoveRequest`; `:89-111` turns `PathReady`/`Arrived`/`Blocked` into events.
- Wander near the post becomes Patrol: **matches**. `States/PatrolState.cs:70-83`; the placeholder state is gone (`States/GuardStateSet.cs:14`).
- `IntruderSpotted` first-sighting scoring: **matches**. `ChaseState.cs:57` passes `!_alreadySpotted`; Combat passes false (`CombatState.cs:60`) so it is not scored twice; no shout.
- Alarm-scaled move speed: **differs**. Gap: `GuardBrain.MoveSpeed` (`Guards/GuardBrain.cs:38`) still exists, but no state calls it (only `Tests/Runtime/GuardTests.cs` does); the states use the flat `PatrolSpeed`/`ChaseSpeed`/`InvestigateSpeed` (`PatrolState.cs:82`, `ChaseState.cs:116`, `InvestigateState.cs:110`), so guards no longer speed up as the alarm rises. The row said the states would pass the speed they want; none applies the alarm bonus.
- `SetAlertState` seam, public `Tick`, `Configure`: **differs (name only)**. `Guard.Tick` (`Guard.cs:189`) and `Configure(director)` (`:170`, route argument gone) match; the state seam is `Guard.ChangeState` (`:213`), not `ForceState` as the row says. Row text is stale.

Known items from the plan:

- A sighting on patrol goes to Investigate first, then Chase: **matches**. `PatrolState.cs:62-67` offers a sighting lead, `:53-54` goes to Investigate, `States/InvestigateState.cs:74-89` then hands over to Chase (plan state table, `docs/plans/guard-fsm-restructure.md:92`).
- Guards ignore each other's bodies in the sweep and slide along walls: **matches**. `Alarm/Navigation/GuardSweep.cs:56-61` ignores guard bodies; `Alarm/Navigation/GuardMoverStepper.cs:38-42,58-66` slides along what cut the step.
- Landing does no fall damage: **matches** (dropped). `Core/GuardLanding.cs:10` says so and the class only answers "has landed"; nothing in `Guards/Core` or `Guards/States` applies fall damage.

Drop rows (checked that the fresh code does not do it; re-add issue named where the row has one):

- Dynamic Rigidbody and NavMeshAgent: **matches** (not done). No `Rigidbody` or `NavMeshAgent` use under `Guards/`; `Core/GuardShove.cs:6` and `Alarm/Navigation/GuardMoverStepper.cs:7` say so. No re-add issue (#223).
- Guards ignoring each other's colliders: **matches**. Spacing is `Alarm/Navigation/GuardSeparation.cs:21`; no collider-ignore code.
- Slippery physics material: **matches**. No `PhysicsMaterial` in `Guards/`.
- Snap to mesh, `Reachable`, resend 0.75 m, `HasArrivedAt`: **matches**. The service plans (`GuardNavigationService.cs:130`) and raises Arrived (`:165-169`); none of those names exist in `Guards/`.
- Steer: **matches**. No `Steer` in `Guards/`. No re-add issue (owner's answer 4).
- Stuck watchdog, refresh path: **matches**. Replaced by Blocked: `GuardNavigationService.cs:172-178`.
- Stuck watchdog, detour and skip: **matches** (not done). Closest is Patrol swapping a blocked point (`PatrolState.cs:91-110`). Re-add issue **#226**.
- Search sweep: **matches**. No state sets Searching; Investigate replaces it (`InvestigateState.cs:9-12`). `GuardBrain.SweepOffset` (`GuardBrain.cs:104`) is left over and unused. No separate issue.
- `KeepHunting` re-send: **matches**. Nothing re-sends; the hue and cry is a one-off lead (`GuardLeads.cs:32`). `GuardBrain.HuntDue`/`ShouldHunt` (`GuardBrain.cs:131,150`) are left over and unused.
- Patrol route from spawner transforms: **matches**. `Configure` takes no route (`Guard.cs:170`); Patrol plans random points (`PatrolState.cs:74`). Re-add issue **#225**.
- Overheard chatter: **matches**. `Guard` implements only `INoiseListener` (`Guard.cs:25`), not `IEavesdropper` (`Acoustics/INoiseListener.cs:68`). Re-add issue **#228**.
- Alarm-scaled vision: **matches**. `GuardSight.cs:89` uses the base `SightRange`; `GuardBrain.SightRange` (`GuardBrain.cs:26`) has no caller. Re-add issue **#229**.
- Shout as noise: **matches**. No `RaiseTheCry` or `AlertGuardsNear` anywhere outside the legacy guard. Re-add issue **#227**.
- Fall damage on landing: **matches**. See the known item above; no separate issue.

### Summary

Table rows checked: 39 (25 keep or change, 14 drop). **Matches 36, differs 3, missing 0.** The three known items from the plan
are checked on top of that, all matching (the landing item is the same as the fall-damage drop row).

Gaps:

1. **Alarm-scaled move speed is not applied** (differs): `Guards/GuardBrain.cs:38` is unused by any state; speeds are flat (`PatrolState.cs:82`, `ChaseState.cs:116`, `InvestigateState.cs:110`). Needs a decision: wire it in or drop the row.
2. **Arrival grace row names the wrong file and methods** (text only): real code is `Senses/GuardArrivalGrace.cs:20,23`.
3. **State seam row names `ForceState`** (text only): real name is `Guard.ChangeState`, `Guard.cs:213`.
4. **Legacy decision code still compiled** (not a parity gap, but #214 asks for it to go): `GuardBrain.NextState` (`GuardBrain.cs:190`), `SweepOffset` (`:104`), `ShouldFollowNoise`/`ShouldHunt`/`HuntDue` (`:120,131,150`), `SightRange` (`:26`) and `MoveSpeed` (`:38`) have no caller in the fresh guard.

Not verified: any behaviour at run time (this was a read of the code, no tests or co-op run); the "Kept, not tested" items above stay untested.
