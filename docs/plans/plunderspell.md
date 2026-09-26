# PLUNDERSPELL — offshoot plan

## Context

`Rogue-Ai-Extraction-Game` is a co-op extraction game built on a **planetary-gravity** premise. The
ask is to fork its architecture, **drop the planetary gravity**, and redirect it into a medieval
wizard loot-extraction co-op game: voice-cast spells, physics-driven melee and item handling, raids
into different historical Ages, plunder castles/villages/kingdoms, extract back to your lair.

The vision, palette, weapon set, spell lexicon and Age structure live in
[`docs/plunderspell.md`](../plunderspell.md), with an illustrated mood board at
[`docs/plunderspell-moodboard.html`](../plunderspell-moodboard.html). This document is the
engineering plan for getting there.

### What the branch survey actually found (this changed the plan)

The checked-out branch `claude/inspiring-davinci-83909p` is **stale** — identical to `origin/main`,
30 commits behind. All five branches were analysed:

| Branch | Ahead of main | What lives there |
|---|---|---|
| `main` | — | Unity URP scaffold + Steamworks.NET. Nothing playable. |
| `claude/inspiring-davinci-83909p` | 0 | Stale copy of main. **Do not build from this.** |
| `develop` | 10 | PurrNet + PurrLobby vendored, Steam lobby UI. No gameplay. |
| `feature/Owen/PCG` | 9 | Procedural room generation. **Off-limits, clean room — do not open, do not merge.** See Step 3. |
| `claude/steam-multiplayer-framework-xia7ch` | **30** | **The real trunk.** Unity 6000.3.15f1. |

**The trunk is far more advanced than expected, and is already half a wizard game.**
`Assets/_Project/Scripts/Runtime/` contains:

- `Items/Weapons/SpellBook.cs` + `Items/ScriptableObjects/SpellStats.cs` — a working
  **spell-casting weapon** driven by a ScriptableObject (fireRate, spread, projectileCount,
  homing, projectileSize, damage, lifetime). This is the spell system, already built.
- `Items/Item.cs` + `Items/ItemManager.cs` — **physics grab / carry / rotate / throw** with
  velocity-based impact damage (`_damageMultiplier`, `_damageCooldown`). The physics-melee and
  loot-handling pillar already exists.
- `Player/PlayerStateMachine.cs` + 10 states — first-person Rigidbody FSM with health, choke
  damage, ground snap, air control, dodge.
- `Enemies/MonsterStateMachine.cs` + 7 states including `MonsterPickedUpState` — **enemies are
  carryable and throwable objects.**
- `Core/` — `SingletonBase`, `EventManager`/`IEvent`, `BaseStateMachine`/`IState`, `IHealth`,
  `ICarryableCreature`, `IChokeDamageSource`, `HealthBar`. Clean asmdef boundaries
  (Core / Player / Items / Enemies / Gravity / Net / Editor).
- `Net/SteamInviteGateway.cs` + vendored **PurrNet** (with Steam transport) + **PurrLobby**.
  Steam invites, rich presence and cold-launch joins are done.
- `Gravity/Gravity.asmdef` — **the planetary gravity is isolated in its own assembly, two files**
  (`GravitySource.cs`, `GravityReceiver.cs`). This is the single best piece of news in the survey.

### Decisions taken with the user

1. **Voice tech** — on-device keyword spotting (offline, fixed ~40-incantation vocabulary).
2. **Perspective** — first-person (reuses `PlayerStateMachine` and `SpellBook` as-is).
3. **The Ages** — time-travel raids; each Age is a destination with its own loot, tech and weapons.
4. **First milestone** — vertical slice: one castle, one Age, four spells, four players, full loop.

---

## Approach

Fork `origin/claude/steam-multiplayer-framework-xia7ch` (**not** main, **not** the current branch).
Keep every system except gravity. Replace the radial-gravity frame with flat world gravity, then
build the wizard layer on top of the FSMs that already exist.

### Step 1 — Fork from the right place

```
git fetch origin
git checkout -B claude/inspiring-davinci-83909p origin/claude/steam-multiplayer-framework-xia7ch
```

The designated branch currently holds only already-merged history, so resetting it onto the trunk
loses nothing. **Never merge, cherry-pick or rebase `feature/Owen/PCG` into this lineage** — the
clean-room boundary in Step 3 is a git rule as much as an editor one.

### Step 2 — Excise planetary gravity

Delete `Assets/_Project/Scripts/Runtime/Gravity/` (asmdef + 2 files + metas), then repair the
three consumers that reference it:

- `Player/PlayerStateMachine.cs` — drop `[RequireComponent(typeof(GravityReceiver))]` and the
  `GravityReceiver` property; replace `receiver.Up` with `Vector3.up`. Its `Look()` already uses
  `Space.Self`, so it needs no change. Keep `freezeRotation`.
- `Player/States/PlayerWalkState.cs`, `PlayerJumpState.cs`, `PlayerRunState.cs` — these express
  movement against `receiver.Up`; collapse to world up and re-enable `Rigidbody.useGravity`.
- `Items/Item.cs` — remove `[RequireComponent(typeof(GravityReceiver))]`; set `useGravity = true`
  in `Awake()`. The comment block about delegating gravity goes with it.

Set `ProjectSettings/DynamicsManager.asset` gravity back to `(0, -9.81, 0)`.

**Net effect: the removal is ~2 files plus three small edits.** The asmdef boundary means nothing
else in the project can have leaked a dependency on it.

### Step 3 — Build the castle generator from scratch

**Constraint: `feature/Owen/PCG` is off-limits — clean room.** Its author has left the project, so
that branch is not a reference, not a prototype scaffold, and not a shortcut. Do not open
`ProceduralRoom.cs`, `RoomSpawner.cs`, `SeedManager.cs`, or the room prefabs. Do not diff against
them, do not check the branch out, and do not port "just the seed sync". The generator is written
from the specification below and from castle architecture, nothing else.

> **Provenance record.** During the branch survey that produced this plan — before the restriction
> was known — `ProceduralRoom.cs` was read in full (~78 lines), along with that branch's file list.
> Nothing else from it was opened. That file implements a recursive branching room-web: a room picks
> a random rotation from a whitelist, picks N of its own spawn points at random, and asks a singleton
> spawner to place a child room there, rejecting overlaps by retry. The design specified below shares
> none of that — it is a deterministic outward-in ward nesting with socket-typed joins and post-hoc
> reachability validation. They are different algorithms solving the problem differently. This is
> recorded so the provenance is documented rather than assumed. For a strictly untainted
> implementation, the generator should be written by someone who has not read that file, working
> from this specification alone.

New asmdef `Assets/_Project/Scripts/Runtime/World/`, written fresh:

- `CastleGenerator.cs` — generates **concentric wards**, not a generic room web, because that's what
  a castle actually is: curtain wall → outer bailey → inner ward → keep → undercroft/crypt. This is
  also better game design than a random graph: loot density rises as you go inward, so extraction is
  always a fighting retreat back out through everything you already woke up.
- `RoomModule.cs` — a prefab with **tagged sockets** (`door`, `window`, `arrow-loop`, `stair-up`,
  `stair-down`, `murder-hole`). Assembly is socket-matching, which gives architecturally plausible
  results and lets level artists add modules without touching code.
- `GenerationSeed.cs` — the host rolls one `int` and replicates it over PurrNet before level load.
  Only the seed crosses the wire, never geometry.
- **Determinism is a hard requirement.** Use an explicitly-passed `System.Random` instance
  throughout — never `UnityEngine.Random`, whose global static state is shared with VFX, audio and
  every other system, making generation order-dependent and clients divergent.
- `LayoutValidator.cs` — after generation, flood-fill to prove every extraction portal can reach the
  keep and the crypt. Reject and reroll on failure. A castle that soft-locks a run is worse than a
  slow generator.

### Step 4 — Voice casting (the new pillar)

New asmdef `Assets/_Project/Scripts/Runtime/Voice/`:

- `MicCapture.cs` — `UnityEngine.Microphone` ring buffer, 16 kHz mono, push-to-cast on a new
  `Incant` input action added to `Player/Input/PlayerInputs.inputactions`.
- `IncantationRecogniser.cs` — on-device keyword spotter over a fixed lexicon. Unity Sentis with a
  small KWS model is the preferred host; a Vosk native plugin is the fallback.
- `SpellLexicon.cs` — `ScriptableObject` mapping incantation → `SpellStats`. This slots directly
  into the **existing** `SpellBook.spellStats` field; `SpellBook.AssignStats()` already
  re-reads every stat, so swapping the asset at runtime is a one-line call.
- `MisfireTable.cs` — maps low-confidence and near-miss recognitions onto comedy outcomes. This is
  the design centrepiece, not an error path.

Only the **recognised spell id** goes over the wire (a PurrNet RPC), never audio — voice chat stays
Steam's problem.

Fix while here: `SpellBook.AssignStats()` mutates `_projectilePrefab`'s components and
`transform.localScale` on the **shared prefab asset**, which leaks across casters and persists in
the editor. Apply those to the instance in `FireProjectile()` instead.

### Step 5 — The Ages

`World/Age.cs` as a `ScriptableObject`: era name, date range, room prefab set, loot table, enemy
roster, weapon tier, light rig, fog/colour grade. The lair hub holds a portal per unlocked Age.
Carrying a later Age's weapon into an earlier one is the intended power fantasy — explicitly do not
balance it away.

### Step 6 — Loot, extraction, meta

- `Items/LootValue.cs` — value, bulk (in stone), fragility. Fragility keys off the impact-damage
  path `Item.OnCollisionEnter` already computes, so dropping a reliquary breaks it for free.
- `World/ExtractionPortal.cs` — the return gate; raid timer; who got out with what.
- `Meta/LairState.cs` — persistent lair, JSON to `Application.persistentDataPath`, host-authoritative.

### Critical files

| Path (on the trunk branch) | Action |
|---|---|
| `Assets/_Project/Scripts/Runtime/Gravity/` | **Delete** |
| `Assets/_Project/Scripts/Runtime/Player/PlayerStateMachine.cs` | Strip gravity coupling |
| `Assets/_Project/Scripts/Runtime/Player/States/Player{Walk,Jump,Run}State.cs` | World-up movement |
| `Assets/_Project/Scripts/Runtime/Items/Item.cs` | Re-enable `useGravity`; add fragility |
| `Assets/_Project/Scripts/Runtime/Items/Weapons/SpellBook.cs` | Fix shared-prefab mutation; accept lexicon swaps |
| `Assets/_Project/Scripts/Runtime/Items/ScriptableObjects/SpellStats.cs` | Extend: element, incantation, misfire |
| `Assets/_Project/Scripts/Runtime/Player/Input/PlayerInputs.inputactions` | Add `Incant` |
| `Assets/_Project/Scripts/Runtime/Core/` | Reuse `SingletonBase`, `EventManager`, `IHealth` — do not re-invent |
| `Assets/_Project/Scripts/Runtime/World/` | **New** — castle generator, written from scratch (Step 3) |

---

## Verification

1. **Compile gate** — open in Unity 6000.3.15f1; all seven asmdefs compile with zero references to
   `GravityReceiver` (`grep -r "Gravity" Assets/_Project` returns nothing).
2. **Gravity regression** — in `TestScene`, drop a crate and a player from height: both fall along
   world −Y, land flat, and do not slerp their rotation. Throw a crate at a monster and confirm
   `Item.OnCollisionEnter` still deals velocity-scaled damage.
3. **Voice** — a bench scene logging `(recognised id, confidence, latency ms)` per utterance.
   Target: >90% top-1 on the 40-word lexicon, <150 ms, across four accents.
4. **Determinism** — run `CastleGenerator` twice with the same seed in a fresh domain reload and
   diff the resulting transform hierarchy hashes. They must be identical. Then run it with the
   scene's VFX and audio active, to catch any accidental `UnityEngine.Random` dependency.
5. **Multiplayer** — two editor instances plus one build; host via the PurrLobby Steam provider,
   confirm invite cold-launch through `SteamInviteGateway`, confirm both clients build the same
   castle from the replicated seed, and that a thrown item's impact resolves identically on both.
6. **The loop** — four players enter, loot, one dies, survivors extract; the lair reflects what
   actually came back.
7. **Provenance gate before ship** — `git log --diff-filter=A --format='%an' -- <path>` over
   `Assets/_Project/` to confirm no file in the shipping build originates from the excluded branch.

---

## Status

Planning complete; no implementation yet. Milestone 0 (fork clean, cut gravity) is the next step —
see [`docs/plunderspell.md`](../plunderspell.md) §10 for the full milestone breakdown.
