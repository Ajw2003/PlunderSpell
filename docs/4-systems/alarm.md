# Alarm & Acoustics

The third named pillar of the pitch: "the alarm latches and never decays." `Plunderspell.Alarm` is the
state that pillar lives in; `Plunderspell.Acoustics` is how anything gets to it. Bundled here because
neither means anything without the other — a noise nobody hears is inert, and an alarm with
nothing feeding it never moves.

## What it owns

Acoustics: turning an event at a position (a footstep, a shout, a shattering pot) into an
attenuated `NoiseEvent` delivered to every listener in range. Alarm: accumulating those events into
one castle-wide level, mapping that level to a state (`Calm`/`Stirred`/`Roused`/`HueAndCry`), and
broadcasting state *changes* for `CastleLockdown` (see `castle.md`) and enemy AI to react to. It
does not decide what a listener does about a noise, or what a locked door costs the players to get
past — those belong to `Guards`/`Castle` respectively.

## How it works

- **`AcousticEmitter`** is the authored per-object entry point; **`NoiseBroadcaster.Broadcast`**
  is the same propagation for code with a position but no attached component (a spell effect,
  shattering loot, a guard's shout). Both share one attenuation model: every sound-blocking wall
  between source and listener (counted by a raycast, capped at `AcousticEmitter.MaxWallSegments`)
  halves the perceived strength; anything left under `MinAudibleStrength` is dropped before an
  `INoiseListener` ever sees it.
- **The alarm lives inside `EnemyDirector`** (2026-10-01, #205; formerly `AlarmFSMManager`, same file GUID,
  so scenes keep their component). The director (`Assets/_Project/Scripts/Runtime/Alarm/EnemyDirector.cs:18`)
  is a thin NetworkBehaviour (248 lines; it was 472 before #211) that owns and ticks one class per job, all in
  `Runtime/Alarm/`: `EnemyRegistry.cs:11` (guard and intruder lists, replacing the static
  `CastleGuard.Active`/`Intruders`; `RegisterGuard` :104 forwards to it), `EnemyDirectorBus.cs:11` (the director's ears on
  `EventManager`: `Listen` subscribes the scoring each guard report owes, :22-34), `DirectorAlarm.cs:20` (level, state, latch,
  decay, grace, chasers), `HueAndCry.cs:9`, and the tuning copy `AlarmTuning.cs:7`. Every event between guards, the
  director and the navigation service is an `IEvent` struct on the shared `EventManager` (#299): guards publish
  `EventManager.Instance?.Publish(new NoiseReported(...))` and the director hears it through that bus. The director has no
  `OnX` events or `Publish` overloads any more; it subscribes in `OnEnable` and leaves the bus in `OnDisable`.
  Listeners that serve many guards filter on the event's `Guard`. The bus is global, so there is one director's worth of traffic at a time. The
  `SyncVar`s stay fields of the director (PurrNet needs that) and are handed to the alarm (`:78`); the
  Inspector tuning fields stay on the director so scenes keep their values, and are copied into `AlarmTuning`
  when the alarm is first used (so live edits of them in Play mode no longer reach the alarm). Payload structs
  are in `EnemyDirectorEvents.cs:1`: `NoiseReported`, `IntruderSpotted`, `IntruderLost`, `GuardEngaged`,
  `AlarmChanged`, `GuardDied`, `InvestigateRequest`. Guards raise them (`CastleGuard.cs:1145-1172,1261`);
  the director turns sightings, chases, attacks and noise into alarm points. It sits in the Alarm
  assembly (already referenced by Guards, Audio, Castle, Raid, UI) and holds guards as `Component`, so
  no assembly cycle. `EnemyDirector.Current` is the in-play instance.
- **The hue and cry is a request, not an order.** On reaching HueAndCry the director publishes one
  `InvestigateRequest` per player (`HueAndCry.Raise`, called from `EnemyDirector.OnAlarmStateChanged`), and
  repeats it every `_hueAndCryRepeatSeconds` (3 s, Inspector) at the players' current positions for as long as the
  alarm stays at HueAndCry (`EnemyDirector.TickHueAndCry`, `HueAndCry.Repeat`; server only; #239). Every guard in
  the castle takes the nearest request of a frame, wherever it is (`GuardDirectorLink`: only other reasons, e.g.
  `InvestigateReason.Noise`, keep the 40 m radius). A guard already investigating retargets, since the lead has
  equal strength (a guard that arrived and is looking around too); Chase, Combat and the like do not read leads.
  Older legacy text: each guard within 40 m took the nearest (`CastleGuard.cs:269`) and went to Investigating through `AlertTo`.
  Nothing outside a guard moves it. The fresh guard takes it up as a lead for its Investigate state
  (see "Leads and Investigate (#208)" below).
- **The castle needs witnesses** (2026-10-04, #259; design `docs/plans/alarm-witnesses.md`). The director no
  longer hears noise itself. A guard that hears an intruder's noise reports it (`Guard.cs:125`), and
  `DirectorAlarm.ReportHeardNoise` (`DirectorAlarm.cs:62`) scores `strength * _noiseWeight` once per guard every
  2 s. A guard's first sighting (`ReportSighting(guardId)`, `:72`) adds 20 and makes it a witness; a chasing guard
  is a witness too. Roused needs level 50 and 3 witnesses, Hue and Cry 80 and 5 (`_rousedWitnesses`,
  `_hueAndCryWitnesses` on `EnemyDirector`); points keep accumulating while the witnesses are missing. On its
  first sighting a guard cries out (`GuardCry.cs`: a `NoiseType.GuardCry`, 18 m, strength 0.4, so three walls
  silence it); guards that hear the cry take the crier's position as a lead (`GuardLeads.cs`), and if they see the
  intruder they cry in turn. A cry scores no points. One guard hearing one burst of noise goes to look and leaves
  the castle Calm (`FullRaidIntegrationTests.Test_OneGuardHearingAShoutedLeapDoesNotStirTheCastle`).
  Seen live 2026-10-05 (`Tools/Unity/alarm_witness_check.sh`, seed 777, log
  `docs/generated/castle-floors-2026-10-03/alarm-witness-run1.txt`): one witness holds the castle at Stirred even at
  level 100; with two guards brought within earshot, both came on the cry, cried in turn, and the third witness made
  it Roused within 2 s. Also seen: a guard in a fight cycles Combat -> Investigating -> Chasing every 1-3 s and cries
  each time it re-enters the chase; on seed 777 the ground floor's three guards patrol too far apart to hear each
  other's cries.
- **`EnemyDirector`** is a server-authoritative FSM over a 0–100 level; `TickDecay` bleeds the level off
  at a fixed rate once `_decayDelay` seconds have passed with no noise. `UpdateState` maps the
  level to a state at fixed thresholds (20 / 50 / 80).
- **The latch is the whole point.** Once the computed state reaches `Roused`, `_locked` is set and
  never cleared for the rest of the raid (only `ResetForNewRaid` clears it): `TickDecay` still runs, but `UpdateState` refuses to let
  the *state* fall below its high-water mark even if the numeric level drifts down, and the level
  itself stops decaying at all while locked. Escalation past that point is the only direction left.
- **Guards report straight to the alarm** (2026-09-25, #139). A guard's shout still goes through
  `NoiseBroadcaster`, but the alarm hears it from the castle-wide trigger's centre, so the walls in
  between halved a far guard's shout to one or two points: several guards chasing and hitting you
  never got past Stirred. Now `CastleGuard` also calls `ReportSighting` (+20) when it starts a chase,
  `ReportAttack` (+6) each time it attacks, and `ReportChase(id, chasing)` as it starts and stops
  chasing. Three guards chasing at once lift the level to at least Roused (50), five to Hue and Cry
  (80); two and three until #259. The numbers are serialized on `EnemyDirector`.
  **Until 2026-09-26 none of this reached the alarm in a real raid** (#163): `GuardSpawner` calls
  `CastleGuard.Configure(null, route)` after the guard's `Awake` has found the alarm, and `Configure`
  overwrote it with null. A live raid had 10 guards and 0 connected to the alarm; a guard breaking
  into a chase left it at Calm 0. `Configure` now keeps the found alarm when passed null
  (`GuardTests.Test_ASpawnedGuardKeepsTheAlarmItFound`). Live after: 9 of 9 connected, and a sighting
  took the alarm from 0 to 22 (Stirred).
- **The shout and the hue and cry send guards to you** (2026-09-26, #163). The shout used to be only
  a noise at the shouting guard, muffled by walls, so guards who heard it walked to the shouter. Now,
  when a guard starts a chase, every guard within its shout radius (20 m) is sent to the intruder's
  last known position (`CastleGuard.AlertGuardsNear`); the noise still goes out as well. When the
  alarm reaches Hue and Cry, every guard within `CastleGuard.HueAndCryRadius` (40 m) of a player heads
  for the nearest one. A sent guard investigates the spot; one already chasing, asleep or stunned is
  left alone. `AlertsReceived` counts it. Tests: `GuardTests` (shout, hue and cry). Live: a shout sent
  4 of the 4 guards within 20 m to the player; the hue and cry sent 1 of the 1 free guard within 40 m.
  Trap: the alarm announces a state change through an observers RPC, which reaches the host a frame
  later, so a probe that raises the level and reads the guards in the same frame sees nothing.
- **Each raid starts calm, with a grace** (2026-09-25, #136). `RaidDirector.StartRaid` calls
  `ResetForNewRaid(CastleGuard.ArrivalGraceSeconds)`: level 0, state Calm, the latch released, no
  chasers, and for 20 s nothing raises the alarm. Before this it only set the level to 0, and the
  latch kept the last raid's Hue and Cry, so the next raid began in it.
- **Replication is a state broadcast, not per-value sync.** `EnemyDirector` runs the FSM only on
  the server (`if (isSpawned && !isServer) return;` in `Update`); guards hear on the server only
  (`Guard.OnNoiseHeard` checks `IsAuthority`), so every report reaches the alarm there. State *changes* fan
  out via an `[ObserversRpc(bufferLast: true)]`, so a client that spawns late still receives the
  current state instead of only future transitions.

## Invariants

- **The alarm state can only escalate once locked.** `UpdateState`'s
  `if (_locked && computed < _alarmState.value) computed = _alarmState.value;` is the entire
  guarantee behind "the alarm never decays" in the pitch — remove it and a quiet stretch after a
  loud one would let the castle stand back down.
- **Pure logic stays network-free.** `ReportHeardNoise`, `UpdateState` and `TickDecay` take no PurrNet
  types as parameters and can run under EditMode tests with no server/spawn state at all; only the
  `SyncVar` fields and the RPC wrapper around them are network-aware. Adding a network call inside
  one of those three methods would break that testability for no behavioural gain.

## Traps

- **`NoiseBroadcaster.CountWalls` needs a real `geometryLayerMask`.** Pass `0` (the default) and
  every noise travels with zero occlusion regardless of how many walls are between source and
  listener — silent failure, not an error, because "no walls counted" and "no walls exist" look
  identical from inside the method.
- **This system was hit by the same `isServer`-on-unspawned trap documented in `raid.md`.** An
  `EnemyDirector` that is never spawned (offline/single-player, before that bug was fixed) has
  `isSpawned` false, so `if (isSpawned && !isServer) return;` does *not* return — it happens to
  work by accident of that specific unspawned-is-its-own-authority convention, but any new code in
  this system that checks `isServer` alone, without the `isSpawned` guard, will silently do nothing
  offline.

## Speech noise

`NoiseType.Speech` (appended last) carries `NoiseEvent.Transcript`. `NoiseBroadcaster.BroadcastSpeech`
delivers it to every `INoiseListener` like any noise, then asks each `IEavesdropper.Overhear` whether the
words were taken in and returns that count. Since #259 the director hears nothing itself; speech raises the
alarm only through a guard that hears it.
`CastleGuard` is one: an asleep or stunned guard answers false; otherwise it stores `LastOverheard`.

## Guard navigation

The director owns a server-side navigation service (#222, plan `docs/plans/bespoke-navigation.md` Design 3):
`EnemyDirector.Navigation` (`Assets/_Project/Scripts/Runtime/Alarm/EnemyDirector.cs:88`), a
`GuardNavigationService` (`Runtime/Alarm/Navigation/GuardNavigationService.cs:17`), ticked from the director's
server-only `Update`. The fresh guard (#206, below) is the only thing that moves through it; every guard
prefab carries the fresh guard since #214.

- **Events** on the existing bus (`GuardNavigationEvents.cs`, readonly structs): `MoveRequest(guard,
  destination, speed, reason)` published in; `PathReady`, `Arrived`, `Blocked(reason)` published out, all on `EventManager`. Blocked reasons: no
  map, no walkable cell, unreachable, door closed, obstacle (the sweep held the guard up for 0.5 s).
- **Why an interface.** Castle references Alarm, so Alarm cannot name `CastleNavGraph`. The service plans
  through `IGuardNavigationMap` (`IGuardNavigationMap.cs`); `CastleGuardNavigationMap`
  (`Runtime/Castle/Navigation/CastleGuardNavigationMap.cs`) adapts the graph. Call
  `director.Navigation.SetMap(new CastleGuardNavigationMap(data.NavGraph))`.
- **Paths.** `GuardPathPlanner.TryPlan` (`GuardPathPlanner.cs:30`) asks the graph for the 4-connected cell
  chain, `GuardPathSmoother.Smooth` (`:19`) keeps only the corners (look ahead to the farthest cell with a
  clear straight line), and the result is cached under (start cell, goal cell). The cache key is the cell
  pair, not the room pair the plan suggested: a route is only right from the cell it began in, and guards
  in one room stand in different cells. The cache is dropped whenever a door changes.
- **Moving.** Each tick, per guard: `GuardPathFollower.NextStep` toward the next waypoint, plus
  `GuardSeparation.PushFor` (`:21`, pushes any pair closer than 1 m apart, standing guards included), cut
  down by `GuardSweep.AllowedDistance`, then `GuardMoverStepper.Move` sets the transform and settles height
  onto the graph's floor height (stairs). No Rigidbody, no agent.
- **Doors** (#263). When the sweep's blocking collider belongs to a closed `IHandOpenable`,
  `GuardMoverStepper.Move` calls `Open()` and sweeps the step again: guards have keys, so unlocked and locked
  and barred doors open for them (#264).
- **Sliding** (2026-10-02). When the sweep cuts a step short, `GuardMoverStepper.Slide` keeps the part of the
  step along the surface it hit and sweeps that too, so a route that clips an archway jamb or a cart's corner
  slides past it. A guard walking square into a wall keeps none of its step and is still reported held up,
  then Blocked. Sliding moves only the guard, so the #200 rule holds. Before this, a guard that clipped a
  corner stood on it until its state gave up.
- **Levo's lift** (2026-10-02). The fresh guard has no physics body, so the spell's impulse (`StatusEffectReceiver.cs:215`
  pushes only a non-kinematic Rigidbody) moved nothing and a levitated guard never left the floor; its test only
  checked the Stunned state. `GuardLift` (`Runtime/Guards/Core/GuardLift.cs`), stepped from `Guard.Tick`, now raises
  the guard 1.8 m at 3 m/s while it levitates, turning 45 degrees a second, then drops it under gravity to where the
  lift began: the legacy `CastleGuard.UpdateLevitation` motion, without its fall damage (dropped in the inventory).
  `GuardLanding.HasLanded` waits for the fall. Tested (`GuardIncapacitatedTests`); not yet seen in co-op.
- **Stairs** (2026-10-02). `GuardNavigationTuning.StepHeight` is 0.7 m, two risers, not one: the body is 0.4 m
  wide, so its front edge meets the next riser while its centre is still a step lower, and at 0.35 m every
  staircase tested (KeepStairwell, LateTurretStair, BronzeMegaronStair) stopped guards at the foot.
  `GuardNavigationCastleTests.GuardClimbsTheStairToItsGallery` covers all three with the sweep on; until then that
  test ran with `SweepMask = 0` and so never touched the stair colliders. The scenes save their own copy of the
  tuning (RaidScene, RaidScene.Scaffold, CastleBench x2), so a change to the default must be made there too. Seen in
  co-op: a guard sent to look at the Late turret-stair gallery climbed from y 0.31 to 2.59 and came back down.
- **Guards do not block each other's sweep** (2026-10-02). Guards are on the Default layer with the castle,
  so `SweepMask` cannot leave them out; `GuardSweep` ignores any hit on a registered guard's body instead (the
  service keeps the set). `GuardSeparation` alone keeps them apart. Before this, a crowd round a player locked
  solid, because each guard's body stopped its neighbours'.
- **The #200 rule.** The capsule (bottom raised by `StepHeight` so stair risers pass) is swept with
  `CapsuleCastNonAlloc` into a 16-hit buffer before every step and the step stops `SkinWidth` short of the
  first wall or player. A guard cannot be moved into a player, so it cannot push one through a wall.
- **Doors** (Design 5). `CastleNavPortalGraph` holds an extra cost per link (`CastleNavPortalGraph.cs`,
  `SetExtraCost`); the portal search adds it when entering an archway (`CastleNavPortalSearch.cs:104`) and
  skips an infinite one. `CastleNavGraph.SetDoorCost` (`CastleNavGraph.cs:115`) sets it with no rebuild and
  bumps `DoorVersion`; `IsReachable` (`:86`) searches when any door is closed, because area ids are computed
  once. `CastleLockdown.NavGraph` (`CastleLockdown.cs:39`) is the opt-in: set, a locked door costs
  `LockedDoorCost` (40 m) and a barred door is closed (`CastleLockdownNavigation.MarkDoor`). `RaidDirector` sets
  it at generation (`RaidDirector.cs:262`, `WireLockdownToNavigation` at `:230`), per the 2026-10-02 decision
  (`docs/6-decisions/Decisions.md`): a locked door costs, a barred door blocks, and a guard with no route gets
  `Blocked(DoorClosed)`.
  A door is matched to the archway within 2 m of it on the floor plan; that was not checked against a built
  raid scene.
- **Allocation.** Zero per tick: 0 B over 500 ticks of 20 guards sweeping (`GuardNavigationServiceTests`).
  Planning a new route allocates its array once.
- **Measured** (co-op, `Tools/Unity/coop_guard_check.sh`, seed 3508293, Late Medieval, 20 guards sent
  chasing the host): before the fixes, 0 s moving of 1778 guard-seconds (empty map); after baking every Age,
  117 s stuck of 666; after the dressing, sliding and guard-body fixes, 4.8 s stuck of 466 (1.0%), 447 s moving.
  Logs in `docs/generated/playability-2026-09-30/nav-before-*`, `nav-tiles-fixed-*`, `nav-sweep-fixed-*`.
- **Tests.** `GuardNavigationServiceTests` (flat floor: player against a wall, arrival, a clipped corner, a guard
  in the way, 20 guards, 0 B) and
  `GuardNavigationCastleTests` (real graph: across rooms, KeepStairwell, closed doors).

## Sight and hearing rules (#238)

Measured numbers: `docs/generated/guard-awareness-2026-10-02/findings.md`.

- **Sight.** `GuardBrain.CanSee` tests the side-to-side angle against half the field of view (55 degrees) and the up/down angle
  separately against `MaxLookPitchDegrees` (80, raised from 70 in part 2: a guard 1.5 m under a 4.5 m wall walk looks 73 degrees up to the
  head). A player on a table, a ledge or in mid-jump in front stays seen and a chase is not lost for being airborne; nearly overhead is not seen.
  `GuardSight` aims at three points, head (`TargetAimHeight` 1.0 above the pivot, which is the capsule centre), middle and feet
  (`TargetLowAimHeight` -0.9), and a sighting needs **one** clear ray, so a lintel over the head or a ledge edge over the body no longer hides a
  player whose other half is in view. Rays still stop at geometry layers: a player fully behind a wall, or 1.5 m from the foot of a wall
  they stand 1 m back on, is not seen. **Range:** the serialized `SightRange` (14 on most prefabs) times `GuardBrain.BaseSightScale` (1.5),
  then `GuardBrain.SightRange(alarm)`: x1.15 Stirred, x1.4 Roused, x1.75 Hue and Cry (#229). Noise radii and strengths are unchanged.
- **Footsteps.** `StepAudio` feeds `FootstepNoiseEmitter` for any player that has one, wherever the player is. Pace picks the stance
  (under 2.2 m/s crouch 1.5 m, under 4.5 walk 4 m, else run 8 m; strength 0.4). The raid controller walks at 5 m/s, which is the 8 m run noise.
  **Hold C to creep** (`PlayerInputController`, `PlayerStateMachine.Creeping`, `CreepPace` 0.4): 2 m/s, the 1.5 m crouch footstep. C because
  Ctrl is the whisper modifier and Shift the shout modifier. The input is local; the slower pace is the movement itself, so the host
  measures the same quiet step for a client (`StepAudio` reads each player's travel). Measured in co-op, guard 3 m to the side: walking 5 m/s
  was heard 4 times, creeping 2 m/s 0 times.
- **Landing.** `OnLanding`: falls over 2.5 m/s 6 m at 0.5, over 10 m/s 12 m at 0.8.
- **Loot.** `LootNoise`: impact reach 2 x sqrt(kg) scaled by speed, capped 14 m, strength 0.2 + 0.03 x kg; a piece too heavy to lift being
  dragged scrapes every 0.8 s, 2 x sqrt(kg) m at 0.3 (`LootDragNoise`). Server only. In-game hook not yet verified (see findings).
- **Walls.** The raid player's emitter now counts Default-layer walls. `NoiseBroadcaster.CountWalls` ignores the listener's own body,
  other listeners and anything carrying a Rigidbody, and aims at the middle of the listener. Two walls hide a footstep from a calm guard.
- **Targeting (#270).** Guards ignore downed players entirely: `Interfaces.Downable.IsDown` (implemented via `IDownable` by
  `PlayerNetworkOwnership`) is checked in `GuardSight.Look` and `HueAndCry.Raise`, so a downed player is never seen, attacked or sent
  guards to. A guard keeps its current target while it still sees them; it switches only when the target is lost or down, or another
  standing visible player is under two-thirds of the current target's distance. Test: `GuardDownedTargetTests`.
- **Tests.** `GuardAwarenessTests`, `NoiseSourceTests`, `GuardSightReachTests`, `PlayerCreepTests`.

## A player the guard cannot reach (#237)

The nav map has no cells on tables, rails or ledges, so a melee guard used to run to the spot under the player and stand there or give up.

- **Unreachable** (`Core/GuardReachability`): from the director's map, not guessed. A player is unreachable when `TryGetFloorHeight` finds no floor
  within 1.5 m, or their feet (`pivot + TargetLowAimHeight`) are more than `UnreachableHeight` (0.9 m) above that floor, and it has held for
  `UnreachableConfirmSeconds` (1 s, so a jump does not count). A player within `MeleeReach` of the guard is always reachable (it can hit them).
  With no director or map nothing is unreachable. Ranged guards never ask: they shoot anyway.
- **Hold below** (`States/HoldBelowState`, replicated as Combat): `ChaseState` and `CombatState` hand a melee guard here. It stops, faces the player,
  stays within `ThrowStandOff` (4 m) of the spot under them (moving to a ring place only to get close, or to sidestep a teammate in the line) and
  throws on `GuardRangedAttack.TryThrow`: the same shot path as a bolt (cooldown, `GuardLineOfFire`, attack signal, `NetworkedProjectile`) with
  `ThrowSpeed` 10, `ThrowCooldownSeconds` 2.5 and `ThrowDamageShare` 0.4 of `AttackDamage`. Stone: `Resources/GuardStone.prefab`, built by
  `Editor/StoneForge`, not a PurrNet network prefab (like the bolt). It gives up to Investigate after `UnreachableLoseSightSeconds` (3 s) unseen,
  and returns to Chase (then Combat) as soon as the player is reachable.
- **Throws use the ranged attack turn**, so the director's limit on shooters per player covers stones; the turn is released straight after each throw.
- **Call for help**: entering the state publishes `UnreachableIntruderReported` on the event bus. `GuardDirectorLink.HelpCalled` reaches free
  guards (Patrol or Investigate) within 40 m; `Core/GuardHelpResponse`: ranged guards go to Chase at once, melee guards are given the player's spot as a
  sighting lead and walk at investigate pace, so they come second and hold below themselves.
- **Tests**: `GuardUnreachableTests` (throws and stays; not through a teammate; melee returns; help call). The one real co-op run did not work (the guard stayed on patrol and never saw
  the player), so the real map's answer for a slab is unverified; log in `docs/generated/guard-unreachable-2026-10-03/run.log`.

## The fresh guard core (#206)

`Guard` (`Assets/_Project/Scripts/Runtime/Guards/Core/Guard.cs:24`) is the new guard, a thin NetworkBehaviour. **Not live yet:** no prefab uses it,
because it only has placeholder states (it patrols, sleeps and dies, but does not investigate, chase or
attack until #207-#213), so the legacy `CastleGuard` stays on the prefabs and spawner until #214. Inventory of
every legacy behaviour (keep / change / drop, with re-add issues): `docs/plans/guard-core-inventory.md`.

- **Host.** Owns the replicated SyncVars (state, health, attack signal: `Guard.cs:28-30`, they must be fields of
  the NetworkBehaviour), a `StateMachine<Guard>` (`Guard.cs:32`) and the parts. It decides nothing: a state
  returns its own next state. The only interruptions are status effects (`Guard.cs:222`, a sleep, stun or levitation
  enters Slept or Stunned through `GuardStateSet.IncapacitatedBy`, `GuardStateSet.cs:42`) and death (`Guard.cs:197`, publishes `GuardDied`, enters Dead). Only the server runs `Tick`.
- **One class per job**, none over 215 lines: `Core/GuardHealth` (hit points, `Died`, lobby scale),
  `Core/GuardAttackSignaller` (packed count and kind, client replay), `Core/GuardShove` (Frango knock-back by
  transform with a capsule cast, as there is no Rigidbody), `Core/GuardDirectorLink` (registry, hue and cry
  requests within 40 m), `Core/GuardTuning` (every number), `Senses/GuardSight`, `Senses/GuardSightThrottle`,
  `Senses/GuardArrivalGrace`, `Senses/GuardHearing`, `Movement/GuardNavigator`, `States/*`.
- **Sight** (`Senses/GuardSight.cs:42`). Looks 12 times a second (`GuardSightThrottle.cs:14`), each guard offset
  by a phase from its instance id so they never all look in one frame (#201). Range and cone are tested before
  the raycast; the line of sight skips hits that belong to the target's own colliders (`GuardSight.cs:84`).
  Nobody is seen during the 20 s grace while the alarm is Calm (`GuardArrivalGrace.cs`; `RaidDirector` begins it
  next to the legacy guard's). Range does not grow with the alarm (re-add: #229).
- **Hearing** (`Senses/GuardHearing.cs:33`). A noise of 0.5 or more wakes a sleeper; a noise above the alarm's
  threshold (`GuardBrain.ShouldInvestigate`) raises `NoiseNoticed`. The guard does not move itself: the
  current state decides (Investigate, #208; the noise becomes a lead in `GuardLeads`).
- **Movement** (`Movement/GuardNavigator.cs:67`). The only way the guard moves: `MoveTo` publishes a `MoveRequest`,
  and `RouteReady`, `Reached` and `RouteBlocked` come back filtered to this guard. It registers a mover with
  `director.Navigation` on attach. Nothing else writes the transform except the shove.
- **Wiring.** `RaidDirector` gives the service the castle after generating it
  (`Runtime/Raid/RaidDirector.cs:249`, `SetMap(new CastleGuardNavigationMap(Castle.NavGraph))`, server only).
  `CastleLockdown.NavGraph` is set in the same place (`RaidDirector.cs:262`, see "Doors" above).
- **Patrol** (#207, `States/PatrolState.cs:16`, replaces `PlaceholderPatrolState`, deleted in c30fe4a6). Plans a round of `PatrolPointCount` (3) points 3-8 m from `Guard.Home` (`States/GuardPatrolPlanner.cs:27`,
  `:39`). A point counts only if the map has floor under it (`FindCell`) and `TryFindPath` from the post succeeds,
  so a barred door rules a point out. Picks use the guard's own `System.Random` (`Guard.cs:50`, seeded from the
  post, `Reseed` at `:190`), so the server is deterministic. Walks them in turn with a 1 s pause
  (`PatrolState.cs:70`, `:114`); a finished round is replanned. `Blocked(DoorClosed)` drops the point; any other
  block swaps it for a fresh one (`PatrolState.cs:91`). The route is `States/GuardPatrolRoute.cs`.
  `GuardNavigationService.Map` (`GuardNavigationService.cs:50`) exposes the map to states. Numbers are in
  `GuardTuning` ("Patrol").
- **Leads and Investigate (#208).** `Core/GuardLeads.cs` (`Guard.Leads`, `Guard.cs:54`, built `:99`) holds the one
  strongest waiting lead: a noise (`GuardHearing.NoiseNoticed`, strength 0-1, `GuardLeads.cs:33`), the director's hue
  and cry (`GuardDirectorLink.InvestigateRequested`, strength 2, `GuardLeads.cs:32`) or a new sighting (strength 3,
  offered by Patrol, `PatrolState.cs:66`). `Offer` (`GuardLeads.cs:52`) keeps the stronger, the newer on a tie.
  Patrol returns `States.Investigate` whenever a lead waits (`PatrolState.cs:53`), so there is no if/else ladder.
  The 40 m rule (`GuardDirectorLink.HueAndCryRadius`) no longer applies to the hue and cry (#239), only to other request reasons, and applies before a lead is made;
  the position is then moved 3-5 m at random (`GuardLeads.cs:38`, `GuardBrain.HuntOffsetMin/Max`) so the guard
  goes to "roughly" where the player is. `States/InvestigateState.cs` walks to the spot at
  `Tuning.InvestigateSpeed`, stands `InvestigateLookSeconds` (3 s), then returns to Patrol. A newer lead at least
  as strong re-targets, a weaker one is dropped (`InvestigateState.cs:94`); any `Blocked` (`DoorClosed`,
  `Unreachable`, ...) gives up to Patrol (`:117`). It stands rather than turning to look: only the navigation
  service writes the transform. A sighting raises `InvestigateState.PlayerSeen` (`:36`) and `ReactToPlayerSeen`
  (`:84`) hands over to Chase. Tests: `Tests/Runtime/GuardInvestigateTests.cs`.
- **Chase (#209).** `States/ChaseState.cs:19`, entered from Investigate with `States.Chase.Follow(player)`.
  Runs at `Tuning.ChaseSpeed` via `MoveRequest(Chase)`. It re-plans toward the player at most every
  `ChaseRetargetSeconds` (0.25 s) and only if the player is `ChaseRetargetDistance` (0.75 m) from the planned
  spot (`:95`). Sight lost for `ChaseLoseSightSeconds` (0.75 s, so an edge-of-view flicker does not end it)
  offers the last seen spot as a sighting-strength lead and returns Investigate (`:88`); the guard runs to the
  last seen spot, never the player's real one. Publishes `IntruderSpotted` on enter and `IntruderLost` on exit
  (`:46`, `:54`), no shout. A `Blocked` answer is ignored: the next re-plan tries again, and the navigation
  sweep keeps a guard off a player pinned to a wall (#200). **Combat hand-off (#210):** the
  `ChaseState.InReach` event (`:38`, once per approach, within `MeleeReach` 2 m or `RangedEngageRange` 8 m for a
  ranged guard) and `ReactToInReach` (`:130`), which now returns `States.Combat`. Coming back from Combat uses
  `Follow(player, alreadySpotted: true)` (`:45`) so the sighting is not scored twice.
  **Ranged:** `Core/GuardRangedAttack.cs` (`Guard.RangedAttack`, `Guard.cs:54`, built `:102`) is a guard
  with `Tuning.ProjectilePrefab`. `TryFire` (`:31`) takes the cooldown (1.4 s), signals
  `GuardAttackSignaller.Signal(Projectile)`, publishes `GuardEngaged` and launches the shared
  `NetworkedProjectile` (`:50`), as legacy `CastleGuard.TryAttack`/`FireAt` did (`CastleGuard.cs.txt:1157-1215`).
  Chase calls it every tick while the player is seen, so shots come on the move, out to sight range. Before every
  shot it asks the friendly-fire check (below), so Chase never fires through a teammate either. Tests:
  `Tests/Runtime/GuardChaseTests.cs`.
- **Combat and attack turns (#210).** `States/CombatState.cs:20`, entered from Chase. The guard asks the director
  for a turn by event: `AttackTurnRequested` is published (`GuardAttackTurn.Request`, `Core/GuardAttackTurn.cs:52`),
  `AttackTurnMediator.Handle` (`Alarm/AttackTurnMediator.cs:53`) answers `AttackTurnGranted` or `AttackTurnDenied`
  (the bus hears the request, `EnemyDirectorBus.cs:36-43`; `director.AttackTurns` is the mediator, `EnemyDirector.cs:85`). **Limit: 1 melee and 1 ranged turn
  per player** (`AttackTurnTuning.cs:14`). That is the number in issue #210; the legacy guard had no tokens, no
  windup and no turns, every guard in reach struck on its own 1.4 s cooldown (`CastleGuard.cs.txt:1157-1180`).
  A turn ends when the guard releases it (`CombatState.cs:156`, after a 0.5 s `AttackRecoverySeconds` that spreads
  strikes out; the legacy swing had no windup), on leaving Combat (`GuardAttackTurn.End`, called from
  `CombatState.Exit` :63, which covers death and stun because both change state), on `GuardDied` and
  `UnregisterGuard` (`EnemyDirector.cs:107`), or after `TurnTimeoutSeconds` 3 s (`AttackTurnMediator.cs:89`), so a
  guard that cannot reach the player does not block the others. The alarm itself is the director's and was already
  folded in (#205); turns sit beside it there.
  - **With a turn** a melee guard walks in and strikes (`CloseInAndStrike` :187, `Core/GuardMeleeAttack.cs:31`:
    reach `MeleeReach` 2 m, 12 damage through `Damage.Apply`, attack signal `Melee`, `GuardEngaged`). A ranged
    guard fires (`ShootOrSidestep` :197).
  - **Without a turn** it holds a place on a ring around the player (`States/CombatRing.cs`, melee just outside
    reach, ranged just inside range) through `MoveRequest`, and asks again every 0.2 s only when its own cooldown
    is over (`AskForTurnWhenDue` :169). It turns to face the player (`FaceTarget` :91, yaw only). Its ring
    walks use `MoveReason.Combat` (`CombatState.cs:228`), which the navigation facing skips, so `FaceTarget` wins
    (see "Facing" and "Stunned and slept" below).
  - **No flicker.** Combat holds while the player is inside reach plus `CombatMargin` 2.5 m (`ReasonToLeave` :119).
    Past that: Chase if still seen, Investigate if not seen for `ChaseLoseSightSeconds`.
  - **Pinned players.** Combat never writes position, only the navigation sweep does, so the #200 guarantee
    holds; a guard may walk round the player to its ring place but never into them.
  - **Friendly fire.** `Core/GuardLineOfFire.cs:26` (`IsBlockedByTeammate`): a `SphereCastNonAlloc` of
    `ShotClearanceRadius` 0.3 m from eye to aim point into a 16-hit buffer made once; the nearest hit that is not
    the shooter or the target decides, so a wall in front of a teammate is not friendly fire. A `Guard` or the
    legacy `CastleGuard` counts as a teammate. `GuardRangedAttack.TryFire` (`:44`, check at `:58`) calls it, and
    both Chase and Combat shoot only through `TryFire`. A blocked shot is not fired and does not spend the
    cooldown; `LastShotBlocked` makes Combat sidestep to the next place on the ring (`ShootOrSidestep`).
  - **Owner bugs (leaves a player in front of it, cannot get close).** Not reproduced against the legacy guard
    (not run). On the fresh guard the sight check already skips the target's own colliders (`GuardSight.cs:84`)
    and the stop-short is the navigation sweep, so the guard closes to the player's capsule (inside the 2 m reach);
    `GuardCombatTests.AMeleeGuardWithATurnStrikesAndHurtsThePlayer` shows it landing hits. A co-op run is not
    possible yet: no prefab uses the fresh guard (#214).
  - Tests: `GuardCombatTests`, `GuardAttackTurnTests`, `GuardFriendlyFireTests` (`Tests/Runtime`).
- **Stunned and slept (#211).** Two states over a shared base, `States/IncapacitatedState.cs:13`, because they end
  differently. The status receiver owns the timers (`StatusEffectReceiver.Stun/Sleep/Levitate`), so "stands still
  for a set time" is the status running out and the state waits for it. On entry the guard stops its walk and
  gives back any attack turn (`:25`; Combat's own exit already does too). Exit (`Recover` :33): a lead waiting
  (a noise heard while down, including the noise that woke it) goes to Investigate at it; no lead and the castle
  at `Tuning.InvestigateAfterRecoveryFrom` (Roused) or worse goes to Investigate where it stands; otherwise Patrol.
  - **`SleptState`** (`States/SleptState.cs:11`). A noise of 0.5 or more wakes it early: `GuardHearing.Hear`
    (`Senses/GuardHearing.cs:33`) wakes it and leaves the noise as a lead. A stun or Levo landing on a sleeper
    hands over to Stunned (`:19`).
  - **`StunnedState`** (`States/StunnedState.cs:14`). Frango, and Levo until it lands (owner's answer 2). While
    levitated the guard's position is the spell's, so `NoteLevitation` (`:47`) calls `GuardNavigator.Pause`
    (`Movement/GuardNavigator.cs:84`) which sets `GuardMover.Paused` through `GuardNavigationService.SetPaused`
    (`GuardNavigationService.cs:80`); `TickMover` returns early for a paused mover (`:142`), so the service does not
    step, separate or settle it. `TouchDown` (`:56`) resumes it on landing or on leaving the state. Landing is
    `Core/GuardLanding.cs:22`: the spell is over and the pivot is within `LandingTolerance` 0.15 m of the map's
    floor. No fall damage (dropped, `docs/plans/guard-core-inventory.md`). The fresh guard has no Rigidbody, so
    today Levo lifts nothing; this is ready for whatever moves it.
  - **Stun beats sleep** (`GuardStateSet.cs:42`): a stun or levitation holds longer than a noise can wake.
  - Tests: `GuardIncapacitatedTests` (timer, exit at Calm and Roused, early wake by 0.6 and not 0.3, Levo, turn release).
- **On fire (#212).** `States/OnFireState.cs:18`, driven by `StatusEffectReceiver.IsBurning`. The receiver owns the
  burn: `StatusEffectReceiver.Tick` applies Ignis damage to the guard's health in whole points
  (`Status/StatusEffectReceiver.cs:103-115`), and a lethal tick makes `Guard.OnDied` enter Dead (`Guard.cs:228`). The
  state applies no damage, so nothing is counted twice.
  - **Panic** (`RunToRandomPoint`, `OnFireState.cs:60`). Picks a reachable point around where the guard stands now,
    through the patrol planner's `TryPickPoint` (`States/GuardPatrolPlanner.cs:39`), and runs there at
    `Tuning.PanicSpeed` (5.5, faster than a chase). Arrival or a Blocked answer clears the navigator's destination,
    so the next tick picks again; a failed pick or a Blocked answer waits `Tuning.PanicRetrySeconds` first. Leads are
    never read, and entry stops the walk and gives back any attack turn (`:35`).
  - **Priority (#212, changed by #236).** Stun and levitation outrank burning; fire overrules sleep (owner, 2026-10-02,
    see Decisions). `StatusEffectReceiver.Ignite` wakes a sleeper and `Sleep` does nothing while burning, so a burning
    guard is never asleep. `GuardStateSet.TryInterrupt` (`States/GuardStateSet.cs:58`) sends a status change to Stunned
    or Slept while the guard is incapacitated, and to OnFire when it is burning and not held, even straight out of
    Slept. A burning guard that is stunned stays stunned, and `IncapacitatedState.Recover` (`:34`) enters OnFire when
    the stun ends with the fire still going. Why: a guard that cannot move cannot run about, but fire should wake it.
    `Guard.OnStatusChanged` (`Guard.cs:232`) calls it.
  - **When the fire ends** (`AfterTheFire`, `OnFireState.cs:72`). Health 0 goes to Dead (`:48`, see the Dead entry below).
    A player in sight and in reach (melee reach, or the ranged engage range) goes to Combat, in sight only to Chase.
    Otherwise `GuardRecovery.PatrolOrInvestigate` (`States/GuardRecovery.cs:15`), the same rule a stun or sleep uses:
    a lead or a Roused castle goes to Investigate, else Patrol.
  - Tests: `GuardOnFireTests` (panic between distinct points, no attack and turn released, exits to Combat, Chase,
    Patrol and Dead, stun outranking the fire, fire waking a sleeper into OnFire, Somnus ignored while burning);
    `StatusEffectReceiverFireSleepTests` (the receiver rule alone).
- **Dead (#213).** `Guard.OnDied` (`Guard.cs:228`) clears status, publishes `GuardDied` and enters `States/DeadState.cs:21`.
  - **On entry** it stops and unregisters the mover (`Navigator.Detach`, `GuardNavigator.cs:53`), leaves the director
    registry (`Link.Detach`, which also releases a held attack turn), and closes the eyes and ears
    (`GuardSight.Close`, `GuardHearing.Close`). `Tick` returns itself, so nothing leaves Dead; a second hit is
    ignored by `GuardHealth.TakeDamage` (`GuardHealth.cs:34`).
  - **Visual: a scripted tween, not a ragdoll.** Neither guard prefab has bones or joints (full ragdoll waits on #141)
    and the fresh guard has no Rigidbody. `Core/GuardDeathVisual.cs:49` tips the body 90 degrees sideways over
    `ToppleSeconds` (0.7), lets it lie `LingerSeconds` (2), then scales it to nothing over `FadeSeconds` (1.5) as
    `Core/GuardDust.cs:23` puffs a runtime-built particle burst (no dust effect or dissolve shader exists; the
    sprite shader is the one `GrabBeam` uses). Values are in `GuardTuning` ("Dead" header).
  - **Clients.** `Core/GuardDeathPlayback.cs:24` runs on every peer (added by `[RequireComponent]` on `Guard`, so
    `Guard.cs` did not grow) and starts when the replicated state reads Dead, so a client plays the same fall
    from the existing state SyncVar. It also runs on the server.
  - **Despawn.** After the fade the server alone calls `Destroy(gameObject)` (`GuardDeathPlayback.cs:40`), the path
    the legacy guard used (`CastleGuard.cs:1262`), which PurrNet turns into a network despawn. Legacy guards carried
    and dropped nothing on death, so there is nothing to port.
  - Tests: `GuardDeadStateTests` (leaves navigation, registry and turn; stays Dead; leaves upright; removed only after
    the fade; second death harmless). Not yet seen in a live co-op run.
- **Facing (#211).** `Alarm/Navigation/GuardMoverFacing.cs:25` turns each moving guard toward its path step by
  `GuardNavigationTuning.TurnDegreesPerSecond` (270, `RotateTowards`), called from `TickMover`
  (`GuardNavigationService.cs:146`). Yaw only, so sight cones follow where the guard walks. `MoveReason.Combat`
  moves are skipped. Test: `GuardFacingTests`.
- **Replacing a placeholder.** Edit `States/GuardStateSet.cs`; each placeholder names its issue.
- **Tests.** `GuardCoreSensesTests` (throttle, stagger, cone, own-collider line of sight, grace, wake, threshold)
  and `GuardCoreBodyTests` (a move request reaching Arrived through the service, health, lobby scaling, the
  replicated state, death, shove, attack signal), both PlayMode in `Tests/Runtime`.
