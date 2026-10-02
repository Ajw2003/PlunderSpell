# Guard AI as a proper state machine (plan, 2026-10-01)

Owner's request: enemies use a real FSM like the player, talk through events rather than direct
references, no long if/switch chains, and fix guards leaving a player who stands in front of them
and not getting close enough to hit since the #200 collision changes.

Inventory read from code on `claude/playability-fixes` (scout report plus a spot check).
`CastleGuard.cs` is 1316 lines.

## What exists

- **The player's FSM.** `StateMachine.BaseStateMachine`
  (`Assets/_Project/Scripts/Runtime/Core/StateMachine/BaseStateMachine.cs`) is a MonoBehaviour with
  `ChangeState`, `Update` and `FixedUpdate`; `IState` has Enter, Update, Exit and FixedUpdate.
  - **Bug:** `ChangeState` never calls `Exit()` on the old state. It affects the player and the
    monsters.
  - It can't be a guard's base, because `CastleGuard` must be a PurrNet `NetworkBehaviour`.
- **The monster FSM.** `MonsterStateMachine` (Enemies/), with Idle, Patrol, Pursue, Attack, Dead and
  PickedUp states, is live for the throwable creatures. It is separate from the guards.
- **The guards today.** One enum (`GuardAlertState`), one transition function (`GuardBrain.NextState`,
  a switch) and one action switch (`CastleGuard.Act`). Levitation is a 75-line nested if/else
  (`UpdateLevitation`). The stuck watchdog is a 63-line chain (`WatchProgress`). The guard calls the
  alarm directly (`_alarm?.ReportSighting/ReportChase/ReportAttack`).

## Keep, change, drop

| Behaviour today | Plan |
|---|---|
| Patrol route, wander when there is no route | **Keep**, as the Patrol state |
| Investigate a noise, look around on arrival | **Keep**, as the Investigate state |
| Chase with stop-short, attack in reach | **Change**: Chase hands over to a new **Attack** state. It stays engaged while the target is inside reach plus a margin, keeps facing it, and doesn't drop to Search on one missed sight frame |
| Search sweep around the last-known spot; never give up at the hue and cry | **Keep**, as the Search state |
| Hue and cry re-sends near the nearest player (rough, 3–5 m) | **Keep**, as an alarm event the Search and Investigate states react to |
| Asleep, stunned, dead | **Keep**, as the Incapacitated state |
| Levo lift and fall damage | **Keep**, as an Airborne state, which replaces the nested if/else |
| Frango shove (`IShovable`) | **Keep**, in the motor |
| Sight: cone, range by alarm, linecast, arrival grace | **Keep**, as a `GuardSight` part. **Fix (once a test confirms it):** the line-of-sight check ignores the target's own colliders |
| Hearing, wake threshold, overhear and eavesdrop | **Keep**, as a `GuardHearing` part that raises events |
| Dynamic body driven by the agent, zero friction, guards ignore each other, stuck watchdog, NavMesh snap, 0.75 m resend | **Keep**, as a `GuardMotor` part. Re-path, detour and skip become small steps instead of one long chain |
| Projectile guards (turrets) | **Keep**, inside the Attack state |
| Health, `IHealth`, `ScaleHealth`/`ScaleTuning` | **Keep** |
| SyncVars: state, health, attack signal; server-only AI | **Keep**. The replicated enum is set from the current state class, so clients and audio see the same values as today |
| Alarm reports | **Change**: the guard raises `Spotted`, `ChaseChanged` and `Attacked` events, and the alarm subscribes. No more `_alarm?.Report…` calls |
| Static `Intruders` / `Active` lists | **Change**: move to a small registry. Users (audio, spawner, raid) keep the same calls through it |
| `Steer`, the straight-line walk with no NavMesh | **Ask**: raids always bake a NavMesh. Keep as a fallback, or drop it (and its one test)? |
| Test seams: `Tick`, `SetAlertState`, `Configure` | **Keep**, so the 58 guard tests still drive guards the same way |

## Design

- **A generic, plain-C# state machine** (`StateMachine<TContext>`) in Core/StateMachine, not a
  MonoBehaviour, so a NetworkBehaviour can own one.
  - `ChangeState` calls `Exit` then `Enter`.
  - Each state owns its own transitions: a state's `Tick` returns the next state or itself. That
    replaces both the transition switch and the action switch.
- **Fix the shared base:** `BaseStateMachine.ChangeState` calls `Exit()`. Each player and monster
  state's `Exit` gets checked first, because some may never have run and could now change behaviour.
- **CastleGuard** becomes a thin NetworkBehaviour that owns the parts (sight, hearing, motor, combat,
  health) and the FSM, and does the networking. Parts talk to states through events; states call
  the parts' methods.

## Steps (issues)

1. A generic state machine, plus the `Exit` fix on the shared base.
2. Split CastleGuard into sight, hearing, motor and combat parts, with no behaviour change. All 58
   guard tests stay green.
3. Guard states as classes, replacing `NextState`, `Act` and `UpdateLevitation`.
4. Alarm, audio and raid talk to guards through events and the registry, not direct references.
5. Reproduce, then fix, "a guard leaves a player in front of it" and "can't get close enough to
   hit": test first (the line-of-sight self-block, and the stop-short versus the reach), then the
   Attack state.
6. Parity check:
   - every guard test passes;
   - one co-op run, where stuck time stays low (1.7 s today) and guards actually land hits;
   - every keep/change/drop line above checked against the new code.
