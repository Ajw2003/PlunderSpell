# Steam Co-op Framework for Rogue-Ai-Extraction-Game

> **Historical record — completed.** Kept as-is for the reasoning behind the current architecture.
> One part of it no longer holds: the planetary gravity described below (`RogueAi.Gravity`,
> `GravitySource`, `GravityReceiver`, and the gravity-relative movement frame) was removed by the
> Plunderspell pivot to flat world gravity. Everything else — the PurrNet and Steam work, the FSM
> and item systems, the assembly layout — is still live. See [`plunderspell.md`](plunderspell.md).

## Context

The goal is plug-and-play Steam co-op: launch → host → shift-tab → invite → friend joins. No IPs,
no config, either end. Plus a third-person controller with physics grabbing/carrying and
adjustable Mario-Galaxy-style gravity.

This is a **port and integration job, not a greenfield build**. Far more exists than a first pass
suggested — the work is spread across non-default branches, and `RogueLikeSlop/main` is five months
stale, so judging these repos by their checked-out branches badly understates them.

### What already works

**`RogueLikeSlop/ThirdPerson`** (2026-06-12, Unity 6000.3.10f1) — ~2,900 lines beyond `main`:

| System | File | State |
|---|---|---|
| Galaxy gravity | `Managers/GravitySource.cs`, `Player/PlayerController.cs` | Spherical + directional sources, influence radii, nearest-wins resolution, surface alignment via `Quaternion.FromToRotation`, gravity-relative jump, jitter-free ground snap by proportional error correction |
| Physics grab/carry/throw | `Items/Item.cs` | `StartDragging`/`StopDragging`/`Throw`, `MovePosition` follow, kinematic-while-held, velocity-scaled impact damage with cooldown |
| Carrying enemies | `States/MonsterPickedUpState.cs` | Struggle timers, escape rolls, per-second choke damage |
| Third-person camera | `Player/ThirdPersonCameraController.cs` | Orbit, `SmoothDamp`, clamped pitch |
| Enemy AI / items / combat | Monster FSM (6 states), `ItemManager`, `PhysicalGun`, `SpellBook`, `HealthBar` | Working |

**`Rogue-Ai-Extraction-Game/develop`** (2025-10-14) — PurrNet + PurrLobby vendored: `SteamLobbyProvider`,
`SteamTransport`, friends list, lobby browser, member lists, ready states, view management.

**Genuinely absent:** climbing and sliding only. Both **deferred** by decision — ship co-op first.

### The real engineering problem

`ThirdPerson` carries **two parallel controller lineages that were never merged**:

- `RawMathPlayerController` — gravity-aware, but **transform-based** (`transform.position +=`),
  legacy `Input.GetAxis`, camera-as-child. Does not interact with physics at all.
- `PlayerStateMachine` — **Rigidbody**-based, new Input System, camera-relative, clean FSM — but
  **not gravity-aware**: hardcodes `Vector3.up` and `rb.linearVelocity.y` throughout.

Merging these is the core task: the gravity math from the former, expressed in the latter's
Rigidbody frame. Everything else is porting and wiring.

## Decisions taken

| Decision | Choice |
|---|---|
| Foundation | **`develop`** — PurrNet + PurrLobby |
| Source of gameplay | **`RogueLikeSlop/ThirdPerson`** |
| Movement authority | **Client-authoritative owner + interpolation** |
| Climb / slide | **Deferred** until co-op is solid |
| Owen's work | **Ignored entirely.** No PCG cherry-pick, no borrowing from `feature/Owen/PCG`. We author our own architecture |

**Ignoring Owen's work costs nothing here.** His four commits in `develop` are package plumbing
(`Added facepunch stuff`, `removed old steamworks`, `Added extra netcode packages`) and ayden's
`purr net rework` already superseded all of it — under PurrNet we use its own `SteamTransport`, so
none of that code is reachable. Nothing needs stripping. `develop`'s history is left intact; we
simply build nothing on top of his branch.

**Port cost is small:** only 2 of 50 files on `ThirdPerson` reference `Unity.Netcode`, and just one
(`NetworkedProjectile.cs`, 35 lines) is a `NetworkBehaviour`. The gameplay code is netcode-agnostic.

## How we write this code

Self-documenting by construction. A comment explaining *what* a line does means the line needs
renaming, not annotating.

- **Names carry the meaning.** `ResolveDominantGravitySource` over `UpdateGravity`; `surfaceNormal`
  over `hit.normal` copied into `targetUp`. Booleans read as questions: `isGrounded`, `canGrab`.
- **Comments only for *why*** — a non-obvious physics constraint, a platform quirk, an ordering
  dependency. Never a restatement of the line below.
- **Small methods with one responsibility.** The ported `RawMathPlayerController.UpdateGravity` does
  source resolution, surface alignment, jump, gravity integration and ground snapping in one
  ~70-line method. Split along those seams — each becomes a named method that needs no comment.
- `[SerializeField]` private fields over public ones; `ScriptableObject`s for tunables over
  scattered public floats.
- Assembly definitions per module, with dependencies pointing one way: `Player` → `Gravity` → `Core`.
  `Gravity` must not reference `Net` or `Steam`.

### Efficiency fixes the port should carry

The existing code resolves references by search at runtime — correct but wasteful, and worth fixing
while the code is being touched anyway:

- `RawMathPlayerController` calls `FindObjectsByType<GravitySource>` and then loops **every** source
  **every frame**. Replace with a registry: each `GravitySource` adds itself in `OnEnable` and
  removes itself in `OnDisable`. No scene search, no per-frame allocation.
- `Item.cs` and `MonsterPickedUpState` both call `FindFirstObjectByType` at runtime. Inject or cache
  these in `Awake` instead — the standards doc forbids `Find` in per-frame callbacks, and these are
  one lookup away from being in one.

---

## Phase 0 — Converge the branches

- Branch `claude/steam-multiplayer-framework-xia7ch` from **`origin/develop`**.
- **Unity version conflict:** `develop` is 6000.2.7f2, `ThirdPerson` is 6000.3.10f1. Converge on
  **6000.3.10f1** — the ported gameplay is the larger body of code and PurrNet is vendored source
  that compiles either way. Verify PurrNet's Codegen post-processor (`Assets/PurrNet/Codegen/`) on
  6.3 first; it hooks the compilation pipeline and is the one component a version bump could break.
- Restructure to `Assets/_Project/Scripts/Runtime/{Core,Gravity,Player,Items,Enemies,Net}` per
  `csharp-unity-standards.md`. Leave `Assets/PurrNet` and `Assets/PurrLobby` untouched as vendored
  third-party. Add `.editorconfig`.
- Keep `steam_appid.txt` = 480 (Spacewar) for dev. **Ship blocker:** needs the real appid.

## Phase 1 — Port gameplay, single-player first

Bring `ThirdPerson`'s systems across **unnetworked** and confirm they still work. Networking on top
of a broken port is untestable.

- Port `EventManager`, `SingletonBase`, `BaseStateMachine`/`IState`/`PlayerState`, the Monster FSM,
  `Item.cs`, `ItemManager`, `GravitySource`, `ThirdPersonCameraController`, `HealthBar`.
- **Fix while porting:** `SingletonBase.Instance` auto-creates a GameObject when `_instance` is
  null, so `EventManager.Instance?.Publish(...)` can never be null and spawns a stray object after
  teardown. Make the getter return null instead of self-creating, and honour `PersistBetweenScenes`
  (it currently always calls `DontDestroyOnLoad`).
- Drop `NetworkedProjectile.cs`'s NGO inheritance — re-added as PurrNet in Phase 3.

## Phase 2 — Merge the two controllers *(the core task)*

One Rigidbody controller, gravity-aware, driven by the FSM and the new Input System.

- `GravityReceiver` component: resolves the dominant `GravitySource`, sets `rb.useGravity = false`,
  applies `-up * strength` via `AddForce`, exposes `Up`, and slerps alignment. Reuses
  `GravitySource.GetGravityDirection` unchanged.
- Rewrite `PlayerWalkState` / `PlayerJumpState` in the gravity frame: replace `Vector3.up` with
  `receiver.Up`, and `linearVelocity.y` with `Vector3.Dot(velocity, receiver.Up)`. Camera-relative
  input projects onto the gravity plane — `Vector3.ProjectOnPlane(camForward, up)`, extending the
  projection already in `PlayerWalkState`.
- Ground check: `Physics.SphereCast` along `-up`, porting `RawMathPlayerController`'s proportional
  error-correction snap (it is what removed the camera jitter). **Replaces the broken
  `OnTriggerEnter`/`Exit` `IsGrounded` on `PlayerStateMachine`** — any trigger currently grounds you.
- Retire `RawMathPlayerController`, `Simple3DPlayerController`, `StrategyPlayerController` once the
  merged controller is verified. Unify on the new Input System; delete legacy `Input.GetAxis` paths.
- **Bug to fix:** `Item.cs` sets `_rb.useGravity = true` in `Awake`, `StopDragging` and `Throw`.
  Under custom gravity, thrown items will fall along world `-Y` instead of toward the planet. Items
  need a `GravityReceiver` too, and those three assignments must go.

## Phase 3 — Network it

- Player prefab: `NetworkIdentity` + PurrNet `NetworkTransform`, owner-authoritative via
  `NetworkRules`. Gate input, camera and gravity force application on ownership.
- Non-owner players: `rb.isKinematic = true`. Remote bodies follow the transform rather than
  simulating locally, or they fight interpolation and jitter.
- Replicate FSM state as a `SyncVar<PlayerStateId>` byte enum for animation — not the state objects.
- **Gravity needs no replication.** `GetGravityDirection` is a pure function of position over static
  level data, so every client computes an identical result independently. Zero bandwidth.
- Re-add `NetworkedProjectile` as a PurrNet `NetworkBehaviour`.
- Use PurrNet's `PlayerSpawner` for per-client spawning.

## Phase 4 — Networked grabbing & carrying

- **Ownership transfer is the mechanism.** On grab, `GiveOwnership(player)` via PurrNet's
  `GlobalOwnershipModule`; the holder simulates locally, everyone else interpolates. Release returns
  ownership to the server so dropped objects settle consistently.
- `Item.cs` already does the hard local work (`MovePosition` follow, kinematic-while-held,
  impact damage) — it needs an ownership gate around `FixedUpdate` and `OnCollisionEnter`, not a
  rewrite.
- Guard `MonsterPickedUpState`'s struggle coroutines to server-only so escape rolls are not
  simulated independently per client.
- Handle owner disconnect mid-carry: object must return to server ownership, not leak or freeze.

## Phase 5 — Close the invite gap

**Verified missing from both branches** (`grep -inE "connect_lobby|SetRichPresence"` → zero hits):

1. **Cold launch from an invite.** Parse `+connect_lobby <id>` from
   `Environment.GetCommandLineArgs()` at boot. Without it, accepting an invite while the game is
   closed silently does nothing — the single most common gap in this flow.
2. **Friends-list Join Game button.** `SteamFriends.SetRichPresence("connect", $"+connect_lobby {id}")`.

Wire both into PurrLobby's existing `SteamLobbyProvider`, which already handles the in-game path
via its `GameLobbyJoinRequested_t` callback (`SteamLobbyProvider.cs:179`).

---

## Verification

Each phase is verified before the next starts.

- **P0:** project opens on 6000.3.10f1, zero console errors, PurrNet Codegen runs clean.
- **P1:** single-player — walk, jump, grab, throw, enemy AI, all as they behaved on `ThirdPerson`.
- **P2:** walk a planet's full circumference without camera flip or losing ground contact at the
  poles; throw an item and confirm it arcs toward the planet, not world-down.
- **P3 (local):** two instances via Multiplayer Play Mode over PurrNet's `LocalTransport` /
  `UDPTransport` on `127.0.0.1` — each sees the other walk a planet smoothly, no rubber-banding.
- **P4:** A grabs, B sees it in A's hands; A throws, B sees a consistent landing point; A
  disconnects mid-carry and the object recovers.
- **P5 (Steam):** two machines, two Steam accounts. Test all three paths independently —
  friends-list join, overlay invite while running, and **cold launch with the game closed**.
- **Automated:** EditMode tests for logic needing no scene — gravity source resolution,
  `GetGravityDirection` math, FSM transition tables. Keep that logic in plain C# classes, not
  `MonoBehaviour`s, per the standards doc.

**Testing setup.** Two Steam accounts are needed (one account cannot host and join itself), and both
are available two ways:

- **Second physical machine** with the alt account — the highest-fidelity test, and the only one
  that exercises a real network path between hosts. Use this to sign off each Steam-facing phase.
- **VM on the primary machine** running the alt account — fine for fast iteration on the invite
  flow. Caveats: the Steam overlay needs working 3D acceleration in the guest to render shift-tab,
  and both accounts must own the appid (free for Spacewar/480, so not an issue during dev).

Keep PurrNet's `LocalTransport`/`UDPTransport` selectable anyway for pure-gameplay iteration where
Steam adds nothing but startup time.

## Sequencing

Phase 5 is independent of 1–4 and is the cheapest proof of the headline requirement — worth doing
early, in parallel, to validate the invite round-trip before gameplay is layered on. Phases 1 → 2 →
3 → 4 are strictly ordered. Phase 2 is the highest-risk item and the one to allow slack for.
