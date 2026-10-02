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
  so scenes keep their component). The director (`Assets/_Project/Scripts/Runtime/Alarm/EnemyDirector.cs:1`)
  also owns the guard and intruder registries (`RegisterGuard` :130, `RegisterIntruder` :140, replacing
  the static `CastleGuard.Active`/`Intruders`) and a typed event bus (`Publish` :187-230; payload structs in
  `EnemyDirectorEvents.cs:1`): `NoiseReported`, `IntruderSpotted`, `IntruderLost`, `GuardEngaged`,
  `AlarmChanged`, `GuardDied`, `InvestigateRequest`. Guards raise them (`CastleGuard.cs:1145-1172,1261`);
  the director turns sightings, chases, attacks and noise into alarm points. It sits in the Alarm
  assembly (already referenced by Guards, Audio, Castle, Raid, UI) and holds guards as `Component`, so
  no assembly cycle. `EnemyDirector.Current` is the in-play instance.
- **The hue and cry is a request, not an order.** On reaching HueAndCry the director publishes one
  `InvestigateRequest` per player (`EnemyDirector.cs:234`, called from `SetState` :381); each guard
  within 40 m takes the nearest (`CastleGuard.cs:269`) and goes to Investigating through `AlertTo`.
  Nothing outside a guard moves it. Until the Investigate state (#208) exists this maps to the old
  `Investigating` behaviour.
- **`EnemyDirector`** is a server-authoritative FSM: `ApplyNoise(strength)` adds
  `strength * _noiseWeight` to a 0–100 level and stamps the time; `TickDecay` bleeds the level off
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
  chasing. Two guards chasing at once lift the level to at least Roused (50), three to Hue and Cry
  (80). The numbers are serialized on `EnemyDirector`.
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
  the server (`if (isSpawned && !isServer) return;` in `Update`); a client-side `OnNoiseHeard` call
  forwards to the server via `ReportNoiseServer` instead of applying locally. State *changes* fan
  out via an `[ObserversRpc(bufferLast: true)]`, so a client that spawns late still receives the
  current state instead of only future transitions.

## Invariants

- **The alarm state can only escalate once locked.** `UpdateState`'s
  `if (_locked && computed < _alarmState.value) computed = _alarmState.value;` is the entire
  guarantee behind "the alarm never decays" in the pitch — remove it and a quiet stretch after a
  loud one would let the castle stand back down.
- **Pure logic stays network-free.** `ApplyNoise`, `UpdateState` and `TickDecay` take no PurrNet
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
words were taken in and returns that count (the alarm hears the noise but is not an eavesdropper).
`CastleGuard` is one: an asleep or stunned guard answers false; otherwise it stores `LastOverheard`.
