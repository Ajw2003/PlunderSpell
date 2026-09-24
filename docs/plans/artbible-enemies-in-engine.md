# Art bible enemies in the engine (plan, 2026-09-24): approved, in progress

Getting the 16 ArtForge enemies (`Assets/Models/ArtBible/Enemies/`) into a raid as working guards:
imported, prefabbed, posted to the castle, and checked in the engine the same way they were checked
in Blender. Animation has its own plan:
[`artbible-enemy-animations.md`](artbible-enemy-animations.md). Both follow the
[`docs/art/WORKFLOW.md`](../art/WORKFLOW.md) loop:

```
plan → build → audit (in-engine sheet beside the concept) → fix → audit → commit
```

Status: **plan only. Nothing here is built.** The decisions marked **[DECIDE]** need the user
before work starts.

## Where things stand (checked against the code, 2026-09-24)

| Fact | Evidence |
|---|---|
| The 16 models are imported. Unity has written `.meta` files for them | `main` commits `bc1672c` / `30bbd94` ("meta"). The finished set is PR #128; `main` has 10 of 16 from an earlier merge |
| They import as **Generic** rigs with no avatar | `LanternWarden.fbx.meta`: `animationType: 2`, `avatarSetup: 0` |
| No prefab, roster entry or spawn uses them | `EnemyPrefabForge.Specs` lists only the 10 EnemyForge enemies (`Assets/_Project/Scripts/Editor/EnemyPrefabForge.cs:62`) |
| A guard is one `CastleGuard` (PurrNet `NetworkBehaviour`) with a synced alert state and health, plus a `NavMeshAgent`, a `CapsuleCollider` and a `StatusEffectReceiver` | `EnemyPrefabForge.BuildPrefab`, `CastleGuard.cs:25`, `:82`, `:85` |
| The roster picks by **zone only**. The Age chosen in the Lair does not reach the guard planner | `EnemyRoster.PickForZone(zone, rng)`; `docs/ProjectState.md`, "Choosing an era in the Lair does nothing" |
| Era-specific castle rooms are being built by another agent, also not yet wired to era | `docs/plans/era-castle-rooms.md` |
| Attacks run on the server only. Clients see a projectile or a health change, but get no event saying "this guard swung" | `CastleGuard.TryAttack` (`:536`), with no RPC in the file |
| Nothing animates any enemy today | No `Animator` anywhere under `Runtime/Guards` or `Runtime/Raid` |

## Decisions recorded (2026-09-24, from the user)

- **Old roster:** the art-bible set replaces the household four (Watchman, Man-at-Arms, Sergeant,
  War-hound). The five supernatural enemies stay for the Crypt.
- **Era gating:** enemies spawn only in their own Age. The era pass-through is built once, shared
  with the era-rooms work.
- **Animation source:** AnimForge keyframes, authored in Blender by code.
- **Ragdoll:** yes, for killed or thrown guards. Clips cover sleep, stun and knock-down.
- **Motion:** in-place clips. The NavMesh agent moves the guard, and that movement is what the
  network syncs. The clips only animate.
- **Textures:** stay at 1024.
- **LODs and spring bones:** after the first playtest.

## Decisions for the user **[DECIDE]**

1. **What happens to the 10 EnemyForge enemies?**
   - **A (recommended):** the art-bible set replaces the household four (Watchman, ManAtArms,
     Sergeant, WarHound). The supernatural five stay in the roster only for the Crypt, until the
     bestiary question in `docs/plans/moodboard-gap-closure.md` §2.6 is settled.
   - **B:** keep all 26 side by side.
   - **C:** retire all 10 now.
2. **Era gating.**
   - **A (recommended):** enemies spawn only in their own Age, which needs the era wiring the
     room agent also needs. Build it once, shared: see phase E3.
   - **B:** mix all four Ages until that wiring lands.
3. **Texture resolution.** Rebake enemies at 2048 as the brief asks (about 4× the size in the repo),
   or keep 1024 until profiling says otherwise. Recommended: keep 1024 now, and make it a one-flag
   change.
4. **LODs.** Generate LOD1/LOD2 in ArtForge now, or after the first playtest. Recommended: after.

## Phases

Each phase ends with its own audit, commit and push, as in the workflow.

### E0: import settings owned by code
An `AssetPostprocessor` (`Editor/ArtBibleModelImporter.cs`) owns the import settings of everything
under `Assets/Models/ArtBible/`, so nobody sets them by hand in the Inspector:
- **Humans:** Humanoid rig, avatar created from the model. The bone names are already Unity-Humanoid
  (`Tools/ArtForge/art_forge/figures.py`, `UNITY_HUMANOID`). Map them explicitly, not by
  auto-detect, so a rename fails loudly.
- **Hound:** Generic rig, root bone `Root`.
- **Materials:** URP Lit, rebuilt from the baked maps. `_MetallicGlossMap` comes from
  `<Name>_MetallicGloss.png`, and emission is multiplied by 9 as HDR (the ×9 that
  `enemy_forge.materials.EMISSION_STRENGTH` bakes out).
- **Model and textures:** scale factor 1, no mesh read/write, compressed textures with mipmaps. The
  ORM map imports as linear, not sRGB.

The existing `ArtAssetImportValidator.cs` gains a check that each of these settings holds.

**Audit:** an EditMode test that every ArtBible enemy FBX has the right rig type, and that every
Humanoid avatar is valid (`Avatar.isHuman && isValid`).

### E1: prefabs from the art bible's own numbers
`Editor/ArtBibleEnemyForge.cs`, a sibling of `EnemyPrefabForge`, following the same rules
(`docs/systems/raid-scene-assembly.md`, "Enemy prefabs"). It reads `docs/art/data/*.json` and
`artforge_manifest.json`, so the height, zones and role come from the one source rather than
another hand-typed table.
- One **prefab variant** per model: `Assets/_Project/Prefabs/Enemies/ArtBible/<Age>/<Name>.prefab`.
  Variants keep the `.blend` link, per the existing rule.
- **Tuning by role** (patrol, ranged, heavy, special), overridable per enemy:

  | Role | Patrol speed | Chase speed | Sight | Health | Attack |
  |---|---|---|---|---|---|
  | patrol | 2.0 | 4.2 | 14 m | 80 | melee |
  | ranged | 1.9 | 3.6 | 20 m | 60 | projectile |
  | heavy | 1.5 | 3.2 | 13 m | 180 | melee |
  | special | per enemy | | | | |

  The four specials (hound, Keeper of the Flame, Pavisier, Petardier) get a per-enemy row. Start
  their behaviour from the nearest existing mode: hound = melee chaser, keeper and petardier =
  thrown projectile, pavisier = melee shield-bearer.
- **Collider and agent** from the measured height (the JSON `height_m`, props excluded). The agent
  height is capped so every enemy fits the archways of its posted zones (`docs/systems/scale.md`).
  An enemy taller than an archway gets the "archway duck" flag the animation plan uses.
- **Props and lights.** Prop bones become sockets (`Prop.Lantern`, `Hand.R`, …). The Lantern Warden,
  Keeper of the Flame, handgunner and musketeer match, and the petardier's grenado get a small warm
  point light on the emissive part: "light that always comes from something standing in the room"
  (the mood board). Lights are off at LOD distance.
- **Grounding** uses the existing `EnemyPrefabForge.GroundModel`.

**Audit:**
- `ScaleInvariantTests` extended to the new prefabs. Each must be within ±5 % of its JSON height,
  grounded within 0.10 m, and no taller than the clear height of any zone it is posted to.
- An **in-engine review sheet** per enemy, like `EnemyStanceScreenshotForge`: the concept sheet
  beside Unity screenshots (front, side, three-quarter under the raid's lighting), written to
  `docs/art/engine/<age>/<slug>.png`. The same side-by-side as the ArtForge sheets. This catches
  material, scale and pivot mistakes that only show up in the engine.

### E2: roster entries
`ArtBibleEnemyForge` appends entries to `EnemyRoster.asset`, with zones and weights taken from each
enemy's JSON `zones`. The weight comes from the role: patrol 10, ranged 7, heavy 4, special 3.
Whether old entries are removed is decision 1.

**Audit:**
- The existing "every zone has at least one enemy posting" invariant still holds.
- A seed sweep (`CastleAudit`-style, 5 seeds) lists which enemies spawned where, as a table in the
  plan's log.

### E3: era reaches the guards (shared with the rooms work)
- `EnemyRoster.Entry` gains `HistoricalEra Era`.
- `PickForZone(zone, era, rng)` filters by era first, then zone. If that leaves the pool empty, it
  falls back to zone only and logs a warning. Never silently.
- `RaidDirector.Era` is passed through `GuardSpawner`.
- This touches the same seam the era-castle-rooms work needs, `RaidDirector.Era` reaching the
  castle generator. Build the pass-through once, coordinated with that plan, not twice.

**Audit:** an EditMode test that a Bronze Age seed plan contains only Bronze Age enemies (decision 2A).

### E4: in a raid, networked
- **Spawn:** the prefabs spawn through the existing `GuardSpawner` and PurrNet path, unchanged.
- **Attacks reach every player.** Add one replicated attack signal to `CastleGuard`: a synced
  attack counter, or an `ObserversRpc` carrying the attack kind. Every client can then play the
  swing. Today only the server knows. The animation plan depends on this.
- **Audit:**
  - Run CombatBench with each enemy: it patrols, chases, attacks, and can be put to sleep by Somnus
    and lifted by Levo.
  - Do one two-player co-op run in the raid.
  - Screenshots go in `docs/art/engine/`.

## What this plan does not cover
- **Behaviour** each concept describes beyond the existing guard modes: halberdier pairs, pavise
  cover for the handgunner, the petardier blowing doors, the Keeper's fire hazard. Each is a
  gameplay feature and gets its own issue. The models support them (sockets, bones) but don't
  implement them.
- **Loot-table wiring** for the 20 plunder items (a separate, smaller plan).

## Who can run what
- **This container:** the C# (editor tools, runtime, tests), compiled and unit-tested with
  `Tools/Headless/verify.sh`, and any ArtForge rebuild.
- **A machine with the Unity editor:** running the importer and forge menu items, the in-engine
  review sheets, and CombatBench / co-op play. Each of those steps is handed over with its exact
  menu path or batch-mode command, and is marked `UNTESTED` until it has been run there.
