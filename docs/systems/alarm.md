# Alarm & Acoustics

The third named pillar of the pitch: "the alarm latches and never decays." `RogueAi.Alarm` is the
state that pillar lives in; `RogueAi.Acoustics` is how anything gets to it. Bundled here because
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
- **`AlarmFSMManager`** is a server-authoritative FSM: `ApplyNoise(strength)` adds
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
  (80). The numbers are serialized on `AlarmFSMManager`.
- **Each raid starts calm, with a grace** (2026-09-25, #136). `RaidDirector.StartRaid` calls
  `ResetForNewRaid(CastleGuard.ArrivalGraceSeconds)`: level 0, state Calm, the latch released, no
  chasers, and for 20 s nothing raises the alarm. Before this it only set the level to 0, and the
  latch kept the last raid's Hue and Cry, so the next raid began in it.
- **Replication is a state broadcast, not per-value sync.** `AlarmFSMManager` runs the FSM only on
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
  `AlarmFSMManager` that is never spawned (offline/single-player, before that bug was fixed) has
  `isSpawned` false, so `if (isSpawned && !isServer) return;` does *not* return — it happens to
  work by accident of that specific unspawned-is-its-own-authority convention, but any new code in
  this system that checks `isServer` alone, without the `isSpawned` guard, will silently do nothing
  offline.
