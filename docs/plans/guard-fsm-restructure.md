# Guard AI as a proper state machine, with an enemy mediator (plan, 2026-10-01)

This replaces the first draft of this file from earlier the same day, which the owner superseded
with the behaviour spec below before approving it.

## The owner's spec (2026-10-01)

- **Patrol.** Pick 3 or more random points in a radius that are reachable, and walk them. Seeing a
  player or hearing something sends the guard to Investigate.
- **Investigate.** A noise, movement, or something out of the ordinary: the guard comes looking. If
  it sees you and you run, it comes after you and starts fighting.
- **Chase.** Starts right after Investigate if you run. The guard follows fast; ranged attackers shoot.
- **Combat.** Guards take turns attacking the player. Ranged guards let loose, but they are aware of
  their surroundings and teammates, so they don't shoot them.
- **The mediator.** Taking turns needs a central enemy mediator that talks to the guards through
  events. The alarm FSM could become part of it, or the other way round.
- **Stunned / Slept.** Stays still for a set time, then transitions out.
- **On fire.** Freaks out and runs around randomly, then goes back to idle (Patrol), Combat, or Dead.
- **Dead.** Ragdolls, then fades to dust.
- This changes how attacking works.

## What exists today (read from code)

- **`CastleGuard.cs`** is 1316 lines:
  - one transition switch (`GuardBrain.NextState`) and one action switch (`Act`);
  - Levo is a 75-line nested if/else (`UpdateLevitation`);
  - the stuck watchdog is a 63-line chain (`WatchProgress`);
  - it calls the alarm directly (`_alarm?.ReportSighting/ReportChase/ReportAttack`).
- **States today:** Patrolling, Investigating, Chasing, Searching, Incapacitated.
- **The shared `StateMachine.BaseStateMachine`** (used by the player and the monsters) is a
  MonoBehaviour, so a PurrNet `NetworkBehaviour` guard can't inherit it. Its `ChangeState` never
  calls `Exit()` on the old state.
- **`AlarmFSMManager`** (238 lines, a NetworkBehaviour) has the Calm → Stirred → Roused → HueAndCry
  latch, an `AlarmStateChanged` event, and `ReportSighting`, `ReportAttack` and `ReportChase`.
- **Burning** already exists as a status: `StatusEffectReceiver.IsBurning`.
- **Guard prefabs have no ragdoll rig:** no joints or bones in ManAtArms or PalaceGuard, and the
  animations are #141.
- **Movement that works and stays** (from #193 and #200):
  - a dynamic body driven by the NavMeshAgent;
  - zero friction, and guards ignore each other's colliders;
  - the stuck watchdog, NavMesh snapping, and the 0.75 m resend.

  Co-op stuck time was 1.7 s of 1811 s.

## Design

- **A generic, plain-C# state machine**, `StateMachine<TContext>` in Core/StateMachine.
  - `ChangeState` runs `Exit` then `Enter`.
  - Each state returns its own next state, so there are no transition switches.
  - The shared `BaseStateMachine` gets the same `Exit` fix, after each player and monster `Exit`
    has been checked for side effects that never used to run.
- **The enemy mediator (`EnemyDirector`, server-side).** It owns:
  - the guard and intruder registries, replacing the static `Intruders` and `Active` lists;
  - the alarm level;
  - the shared events: `NoiseReported`, `IntruderSpotted`, `IntruderLost`, `GuardEngaged`,
    `AlarmChanged`, `GuardDied`;
  - the combat turn system.

  Guards never call each other or the alarm directly. They raise events, and the director answers
  with events. **Proposed:** the director absorbs `AlarmFSMManager`'s logic and the alarm becomes
  one part of the director, because the director already sees every sighting and engagement the
  alarm scores. Audio, HUD and raid code subscribe to the director's events instead of holding
  guard or alarm references.
- **Taking turns.** The director hands out attack tokens per target: by default one melee token
  and one ranged token per player.
  - A guard in Combat without a token holds position at a ring around the target, facing it, and
    gives way to the attacker.
  - A token is released after the attack, or after a timeout if the attack never happens.
- **Friendly fire.** Before a ranged guard fires, a sphere cast along the shot checks the path. If a
  teammate is in the line, the guard sidesteps along the ring instead of shooting.
- **The guard's parts.** `CastleGuard` becomes a thin NetworkBehaviour that owns the FSM and these
  parts:
  - `GuardSight`: the cone, the range by alarm, and a line-of-sight check that ignores the target's
    own colliders;
  - `GuardHearing`: the noise threshold and the overheard line;
  - `GuardMotor`: body, agent, stuck watchdog, sweep points, shove;
  - `GuardCombat`: melee and ranged, the cooldown, the replicated attack signal;
  - health.

  The parts raise events; the states call the parts.
- **Replication.** The state still replicates as an enum SyncVar, extended with Combat, OnFire and
  Dead, so clients, audio and the HUD see it as today.
- **Death.** Death no longer destroys the guard on the spot. Dead hands the body to physics (a
  topple, until #141 adds a rig for a real ragdoll), then fades it to dust and despawns over the
  network.

## The states and their exits

| State | Exits to |
|---|---|
| **Patrol**: 3+ random reachable points around the guard's post | Investigate (a noise, or seeing a player); Stunned; OnFire; Dead |
| **Investigate**: go to the noise or sighting, look around | Chase (sees a player who runs); Combat (a player within reach); Patrol (nothing found) |
| **Chase**: fast follow; ranged guards shoot on the move | Combat (target within engagement range); Investigate (lost sight, search the last-known spot) |
| **Combat**: turns through director tokens; ranged guards avoid friendly fire | Chase (target leaves the engagement range plus a margin, so there is no flicker); Investigate (lost them) |
| **Stunned / Slept**: stands still for a set time | Patrol or Investigate, depending on the alarm level |
| **OnFire**: panic-runs to random reachable points while burning | Patrol, Combat (a player close), or Dead |
| **Dead**: topple, fade to dust, despawn | (none) |

Every state can go to Stunned, OnFire or Dead when that status lands, except Dead.

## Open questions for the owner

1. **Search and the hue and cry.** Today a guard that loses you sweeps the area, and at the hue and
   cry every guard keeps being sent near the nearest player. The spec has no Search state. Should
   Investigate absorb Search, and should the hue and cry become a director event that puts nearby
   guards into Investigate?
2. **Levo.** A levitated guard floats, then takes fall damage. Is that a state of its own
   ("Airborne"), or part of Stunned?
3. **Sleep woken by noise.** Today Somnus sleep breaks on a loud noise. Keep that, or sleep is a
   fixed time, as the spec reads?
4. **The no-NavMesh `Steer` fallback.** Drop it (recommended), or keep it?
5. **The alarm.** Merge it into the director (proposed), or keep it separate and have the director
   wrap it?

## Steps (parent #203, one issue each)

1. (#204) A generic state machine, plus the `Exit` fix on the shared base.
2. (#205) The enemy director: registries, events, the alarm inside it; the old direct calls and statics go.
3. (#206) Split the guard into its parts (sight, hearing, motor, combat, health). No behaviour change, and
   the guard tests stay green.
4. (#207) Patrol state: 3+ random reachable points.
5. (#208) Investigate state.
6. (#209) Chase state, with ranged guards firing on the move.
7. (#210) Combat state: attack tokens, holding a ring, ranged friendly-fire avoidance, the new attack flow.
   It also fixes "guards leave you while you're in front of them" and "can't get close enough to
   hit", test first.
8. (#211) Stunned / Slept state.
9. (#212) OnFire state.
10. (#213) Dead state: topple, dust, despawn. The full ragdoll waits on #141.
11. (#214) Parity and cleanup:
    - remove `GuardBrain.NextState`, `Act` and `UpdateLevitation`;
    - all guard tests pass, updated where the spec changes behaviour;
    - one co-op run: stuck time stays low, guards take turns and land hits, no friendly fire;
    - docs updated.
