# Raid scene assembly

How `Assets/_Project/Scenes/RaidScene.unity` comes to hold the castle, the haul and the garrison —
and which authored assets it is assembled from.

**As of 2026-09-18 the raid scene is hand-authored, not generated.** Read "Authored, not generated"
below before running anything in this document.

This document exists because the scene used to be assembled from *nothing*: `RaidSceneBuilder`
generated its own box room, its own gold cube and its own capsule guard, so none of the modelled art
in the project ever appeared in a raid. See "The placeholder era" below.

Owns: `Assets/_Project/Scripts/Editor/RaidSceneBuilder.cs`,
`Assets/_Project/Scripts/Editor/EnemyPrefabForge.cs`,
`Assets/_Project/Scripts/Editor/RaidLootTableForge.cs`,
`Assets/_Project/Scripts/Editor/CastleMeshImportSettings.cs`,
`Assets/_Project/Scripts/Runtime/Raid/EnemyRoster.cs`,
`Assets/_Project/Scripts/Runtime/Castle/CastleNavMeshBaker.cs`.

## How it works

Three authored catalogues feed the scene. Each is a committed asset, editable by hand; none is
generated at runtime.

| Catalogue | Asset | Holds |
|---|---|---|
| Rooms | `Data/Castle/CastleRoomRegistry.asset` | 25 room prefabs, 5 per zone |
| Loot | `Data/Loot/RaidLootTable.asset` | 9 postings over 5 loot prefabs |
| Enemies | `Data/Enemies/EnemyRoster.asset` | 18 postings over 10 enemy prefabs |

**`Tools/Plunderspell/Build Playable Raid Scene`** wires those three into a scene and saves it. It
places no geometry of its own beyond a ground plane, a light and the extraction pad — every mesh in a
raid comes from a prefab built from a `.blend`. If any catalogue is missing the build aborts and
names all of them, rather than falling back to primitives.

**`Tools/Plunderspell/Forge Enemy Prefabs + Roster`** authors one prefab per model in
`Assets/Models/Enemies/` and the roster that posts them. Each prefab is a **variant of the model**,
not a copy, so re-exporting the `.blend` flows through to the prefab. Onto that variant it adds a
`CapsuleCollider` and `NavMeshAgent` sized from the model's measured bounds, a
`StatusEffectReceiver`, and a `CastleGuard` tuned per role. The tuning table lives in
`EnemyPrefabForge.Specs`; the roles it is derived from are recorded in
`Assets/Models/Enemies/enemy_manifest.json`.

**`Tools/Plunderspell/Forge Raid Loot Table`** pairs the five loot prefabs with the zones they are
found in. Worth climbs inward — Copper Pot (15) at the wall, Ancient Relic (500) in the crypt — so
the long carry out is what the valuable things cost.

### Eras

<!-- ref:da27 -->

The era picked in the Lair decides which rooms, loot and garrison a raid is built from. Added
2026-09-24.

`Data/Eras/EraContentCatalogue.asset` has one entry per `HistoricalEra`. Each entry names a room
registry, a loot table and an enemy roster. `RaidDirector.ApplyEraContent`
(`Runtime/Raid/RaidDirector.cs`) swaps them onto the generator, the loot spawner and the guard
spawner at the start of `BuildCastle`. An empty field keeps the scene's own assignment, which the
director records the first time it runs. The era is a replicated `SyncVar`, because every peer
builds its own castle geometry and the era decides which rooms that is. A client rebuilds if the
era arrives after the seed.

**`Tools/Plunderspell/Forge Era Content (rooms, loot, enemies)`** (`Editor/EraContentForge.cs`)
writes the catalogue and everything in it from the art that exists. Run it again when new art
lands. It writes to fixed paths, so a second run overwrites and does not duplicate.

| Era | Rooms | Loot | Enemies |
|---|---|---|---|
| Bronze Age | own set: 25 rooms and wall pieces, 4 door plugs | 5 ArtForge items, 2 weapons | Palace Levy, Wall Slinger, Dendra Champion |
| High Medieval | the default `CastleRoomRegistry` | 5 ArtForge items, 4 weapons | Lantern Warden, Castle Crossbowman, Household Knight, Alaunt War-hound |
| Late Medieval | own InnerWard and Keep (10 rooms); other zones use High Medieval | 5 ArtForge items, 2 weapons | Sallet Halberdier (the only one modelled) |
| Age of Powder | the default registry (no rooms built yet) | 5 ArtForge items, 2 weapons | Palace Guard, Musketeer |

How each part is built:

- **Rooms.** Each room is a prefab variant of its FBX, set up like the High Medieval rooms: root at
  `(90,0,0)`, a `MeshCollider`, a `CastleRoomModule` and door sockets. The generator asks for six
  pieces by id (`GatehouseModule`, `WallStraight`, `WallCorner`, `Bastion`, `Drawbridge`,
  `CryptChamberFinal`). An era's own piece takes the id of the piece it replaces, for example
  `BronzeLionGate` becomes `GatehouseModule` and `BronzeTholos` becomes `CryptChamberFinal`. So
  the generator needs no knowledge of eras. Loot anchors come from `CastleLootAnchors.json`, using
  the `(x, z, y)` axis mapping measured for the High Medieval set. A zone with no era rooms, and a
  zone with no era door plug, keep the default registry's.
- **Loot.** Each item gets a `LootItem` in `Data/Loot/<Era>/`, with worth, bulk, fragility and
  artifact flag read from `docs/art/data/<age>.json`. It also gets a prefab in
  `Prefabs/Loot/<Era>/`, a variant of the ArtForge FBX with a fitted `BoxCollider`, a `Rigidbody`,
  `Item`, `LootPickup`, `LootValue` and `NetworkTransform`. Worth decides the zone: the cheapest
  item goes to the wall and the dearest to the crypt, with the same weights as
  `RaidLootTableForge`. Weapons are found as loot, so each era also gets the default table's
  weapon entries whose `InventoryItem.EraAcquired` is that era.
- **Enemies.** Each enemy gets a prefab in `Prefabs/Enemies/<Era>/`, set up like
  `EnemyPrefabForge`'s. The capsule and agent are sized to the spec's body `height_m`. Guard tuning
  comes from the spec's `role` (patrol, ranged, heavy, special). Ranged soldiers fire the
  crossbow `Bolt`. They are posted to the zones in their spec. Every zone must have an enemy, so a
  zone with none gets the era's heavy soldier (Keep, Crypt) or patrol (elsewhere). If the era has
  neither, it gets any soldier taller than 1.2 m.

`EraContentTests` checks that every era has loot (with weapons) and enemies in every zone. It checks
that every era's room set has every zone, fixed piece and door plug, that era room roots are
upright, and that the Bronze Age and High Medieval share no room, item or enemy.

What the era content does not do yet:

- The ArtForge enemies have no animation clips. They move in their bind pose, as the original
  roster did.
- The Tripod Cauldron and other odd shapes use a box collider, so they can tip over when they
  land.
- A `special` enemy that is not a hound (the Keeper of the Flame, the Pavisier, the Petardier) is
  not modelled yet.
- The legacy roster (`EnemyRoster.asset`) and loot table (`RaidLootTable.asset`) are no longer
  used for any era. They stay as the scene's fallback.

Verified on 2026-09-24 in the live Editor, through the real Main menu → Lair → Set Out flow, with
seed 4242 in each era. Every era had 20 guards, all 20 on the NavMesh, and 22 loot pieces, none
thrown out of the world. Bronze Age built only Bronze rooms, and the same seed built the same raid
twice. The screenshots are in `docs/generated/era-integration-2026-09-24/`.

### Enemy postings

Every zone draws from a mix, with the common soldiery outside and the rare, dangerous things deep:

| Zone | Roster |
|---|---|
| CurtainWall | Watchman (12), HexTurret (4) |
| OuterBailey | Watchman (10), ManAtArms (8), WarHound (6) |
| InnerWard | ManAtArms (8), Sergeant (5), WarHound (6), SigilWisp (5) |
| Keep | Sergeant (5), SigilWisp (4), VaultWarden (6), HexTurret (3), ArcRevenant (3) |
| Crypt | VaultWarden (5), ArcRevenant (5), CryptRisen (12), GildedColossus (2) |

`GuardPlacementPlanner` still decides *how many* guards stand *where* and what they walk; the roster
only answers *which one*, from a fourth seed-derived RNG stream (`seed * 31 + 24593`) so picking an
enemy cannot shift the castle, the loot, or where the garrison stands.

### Navigation

The castle is instantiated from the seed at runtime, so its NavMesh is built at runtime too. A bake
done in the Editor would only ever cover the empty ground plane.

`RaidDirector.BuildCastle` calls `CastleNavMeshBaker.Rebuild()` in the one window where it works:
**after** the rooms are instantiated and **before** the garrison spawns. A guard spawned before the
bake lands off-mesh and stands still for the entire raid.

`CastleMeshImportSettings` forces Read/Write on everything under `Art/Models/Castle/`, because
`NavMeshSurface` has to read those meshes to bake them. It is an `AssetPostprocessor` rather than a
one-off pass so that re-exporting a `.blend` cannot quietly undo it.

### Orientation

The models come from Blender (Z-up) into Unity (Y-up), and the three prefab families do **not** agree
on where that correction lives. This was measured by instantiating each prefab at candidate rotations
and reading world bounds, not reasoned about:

| Family | Root rotation | Upright when instantiated with |
|---|---|---|
| Castle rooms | `(90,0,0)` (repaired) | the prefab's own root rotation |
| Loot | `(270,0,0)` | the prefab's own root rotation |
| Enemies | identity (correction on mesh child) | the prefab's own root rotation |

The castle prefabs were originally saved with a root of `(270,0,0)` *on top of* their mesh child's
own `(270,0,0)`, which composes to a 180-degree flip about X — the room hangs below the floor.
`Tools/Plunderspell/Fix Castle Prefab Orientation` sets those roots to `(90,0,0)`.

Because every family is now upright at its own root rotation, the rule at every spawn site is the
same: **compose with `prefab.transform.rotation`, never replace it.** `Instantiate(prefab, pos, rot,
parent)` overwrites the root rotation, which is what laid every room on its edge.

### Getting into a raid

The raid no longer starts on scene load. The flow is
**Main menu -> Lair -> Set Out -> raid -> back to the Lair**:

- `MainMenuScreen`'s Play goes to `GameState.Lair`, not straight to `Playing`.
- `LairScreen` shows the debt, the banked gold and the four eras, and reads `LairHubManager`
  directly. Set Out moves to `GameState.Playing`.
- `RaidBootstrapper` listens for that transition and calls `RaidDirector.StartRaid()`, taking the
  era from the lair. It ignores Paused/Inventory -> Playing, which are returns, not departures.
- When `RaidDirector.RaidResolved` fires, the bootstrapper puts the game back in `GameState.Lair`
  so the takings land against the debt.
- `_autoStart` still exists on `RaidBootstrapper` but defaults to **false**. Turn it on to skip the
  menu while iterating on the raid itself.

`RaidHudView` only draws in `Playing`, `Paused` or `Inventory`. It is IMGUI, which renders over the
uGUI canvas, so an always-on HUD sits on top of the menu and the lair.

## Authored, not generated

`RaidScene.unity` is edited by hand and saved. Nothing regenerates it.

`RaidSceneBuilder` writes `RaidScene.Scaffold.unity` instead — a throwaway reference build for
checking that the catalogues still assemble into something coherent. It is not the scene that is
played, and it has no hand-tuning in it.

| | |
|---|---|
| `Scenes/RaidScene.unity` | **authored.** Hand-edited. The scene that is played. |
| `Scenes/RaidScene.Scaffold.unity` | generated by *Tools ▸ Plunderspell ▸ Build Raid Scene Scaffold*. Disposable. |
| `Prefabs/RaidPlayer.prefab` | **authored.** The player rig. Both scenes instance it. |

### Why it changed

`RaidSceneBuilder` had drifted out of agreement with the scene it claimed to build. Its
`BuildPlayer` assembled a body carrying `FreeLookPlaytestController`, the ItemGym harness, while
the committed `RaidScene.unity` carried `PlayerStateMachine` + `PlayerInputController`. Running
*Build Playable Raid Scene* would therefore have silently replaced the real player with the
harness — and, after 2026-09-18, undone issue 9's input gate with it.

A generator and a hand-edited artefact cannot both own the same file. The generator gave up the
path.

### What replaces the builder as the safety net

A generated scene had its references re-wired on every run, so a dropped reference fixed itself. An
authored one keeps whatever was last saved. `AuthoredRaidSceneTests` (EditMode) is what catches that
now: it opens the scene and asserts that the network spawns the player from `RaidPlayer.prefab`
(no player is placed in the scene; see `net.md`), that the prefab carries the shipping controller
and *not* `FreeLookPlaytestController`, the `IntruderTag`, push-to-cast, camera and network
components the raid expects, that it matches the CastleBench player's setup, and that every
serialised reference on `RaidDirector` and `RaidHudPresenter` is still assigned.

### Editing the player

Edit `Prefabs/RaidPlayer.prefab`; the raid's `PlayerSpawner` creates one per connection from it.
Its setup copies the player built into `CastleBench.unity`, which is the reference because voice
casting was confirmed on it: push-to-cast on V, eye 0.75 m above the capsule's centre, hand socket
under the eye, mass 1, no `LootInteractor` (pickup is `ItemManager`'s mouse drag). Until
2026-09-23 the prefab had push-to-cast on F19 and the eye 1.65 m above centre, so casting in the
raid did nothing and the view sat in door lintels. `AuthoredRaidSceneTests` checks the key and the
eye height.

`Prefabs/Player.prefab` is a *different*, older rig — third-person, 2.0 m × 0.5 m — built by
`TestSceneBuilder` for `TestScene.unity`. It is not the raid player and does not meet the 1.80 m
standard in `scale.md`.

## Invariants

- **`RaidSceneBuilder` never writes `RaidScene.unity`.** It writes the scaffold path, and asserts
  that the two differ before it does anything else.
- **The raid player is a prefab instance in every scene that has one.** An inline copy is how the
  scaffold and the authored scene came to disagree in the first place.
- **The scene is assembled from authored assets or not at all.** `RaidSceneBuilder` aborts when a
  catalogue is missing. The silent fallback to primitives is what hid the problem for so long.
- **Enemy prefabs are variants of their model.** Never copies — a copy severs the link to the
  `.blend` and the art stops flowing through.
- **Every zone has at least one enemy posting and one loot posting.** A zone with an empty pool
  spawns nothing there, silently.
- **The NavMesh is baked between castle generation and guard spawning.** Not in `Start()`, not
  in the Editor.
- **Spawn sites compose with the prefab's rotation, never replace it.** Passing a bare rotation to
  `Instantiate` discards the Blender axis correction the prefab root carries.
- **Castle models are Read/Write enabled.** An unreadable mesh still bakes in the Editor and
  silently produces no surface in a player build.

## Traps

- **Opening a scene unloads every asset nothing in it references yet.** `RaidSceneBuilder` used to
  load the three catalogue ScriptableObjects and *then* call
  `EditorSceneManager.NewScene`, which destroyed the objects those local references pointed at.
  `TryLoadAuthoredAssets` returned true, the null check passed, and the generator, loot spawner and
  guard spawner were then all wired to a fake-null — the assembled scene had no room registry at
  all. Load authored assets only after the scene exists.
- **A new castle FBX imports with the wrong root rotation unless `bakeAxisConversion` is on.**
  Every castle model committed before 2026-09-17 had it ticked by hand in its `.meta`; the first
  one exported afterwards did not, imported at a 270-degree root instead of 90, and the upright
  root the prefabs carry (see "Orientation") then turned it upside down.
  `CastleMeshImportSettings` now forces it, so the tick can no longer be missed.
- **Loot can be thrown by its own spawn.** `LootPlacementPlanner` is pure, so it knows a room's
  centre but not the shape of the room's mesh. It spawns loot 0.5 m above the room origin, scattered
  up to 3 m. Against the old flat placeholder floor that was always safe; against real room geometry
  a piece can spawn *inside* a wall or a prop, and PhysX ejects an overlapping rigidbody hard.
  Measured on seed-varied runs: roughly **2–7 of ~19 pieces** per raid end up flung into the air or
  out of the world. This is a pre-existing defect that the real art made visible; it is **not fixed**.
  Reducing `ScatterRadius` from 3 m to 1 m only moved it from 7/19 to 5/18, so the dominant cause is
  props at the room centre, not the scatter. A proper fix needs the spawner to find a clear resting
  spot (the planner must stay pure), and should be done with that constraint in mind — an earlier
  attempt that raycast for the floor made it *worse* (15/22) by landing loot on room roofs.

- **`GameServices` is initialised after scene `OnEnable`.** `UIBootstrapper` calls
  `GameServices.Initialize()` from `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, which runs
  *later* than the `Awake`/`OnEnable` of objects already in the scene. Anything in a scene that
  touches `GameServices` from `OnEnable` must call `Initialize()` itself first — it is idempotent.
  Not doing so threw a NullReferenceException that silently ate the whole menu-to-raid transition.

- **Play mode does not tick while the Editor is unfocused.** `Application.runInBackground` is now on
  in Player Settings, but the Editor still needs `unity command set_autotick --enable true` to run
  frames while being driven from the CLI. Without it a driven Play session sits at frame 1 forever
  and every "nothing spawned" reading is a lie.

- **PhysX does not see a new collider until transforms are synced.** The rooms are instantiated and
  the loot is spawned in the same frame, so `LootSpawner.SpawnFor` calls `Physics.SyncTransforms()`
  first. Without it, queries run against an empty physics scene.

- **Room centres have no floor collider.** A downward ray from a room's centre passes through the
  room and hits the ground plane at y = 0. The room prefabs are shells; the ground plane is the
  floor. Anything probing for "the floor of this room" needs to know that.

- **`AssetDatabase.GenerateUniqueAssetPath` is not idempotent.** `RaidSceneBuilder` used it to save
  generated assets, so every re-run minted `GeneratedLootTable 1.asset`, `… 2.asset` and left the
  scene pointing at the original. It now overwrites a fixed path instead. The stale duplicates still
  in `Data/Generated/` are from that era.

- **`GildedColossus` is genuinely rare.** Weight 2 in the Crypt only; it appeared twice in 523 spawns
  across 40 seeds. That is intended for a vault boss, but it means a spot-check of one raid will
  usually not contain one.

## The placeholder era

Before this, `RaidSceneBuilder` generated everything it needed at build time: a `GeneratedRoomRegistry`
whose only entry was a primitive box room used for all five zones, a `GeneratedLootTable` of five
identical gold cubes, and a capsule `CastleGuard` with no `NavMeshAgent` — so the garrison could not
have moved even if a NavMesh had existed, and none did.

Meanwhile the project already contained 25 modelled castle rooms wired into a complete
`CastleRoomRegistry`, 5 modelled loot prefabs with colliders and `LootPickup` data, and 10 rigged
enemy models. None of it was referenced by anything. The builder's own doc comment described this as
intentional ("Everything it generates is placeholder… a harness for playing the game, not the art
pass") — the harness simply outlived the art arriving.

## Verification

Driven against a live Editor through the Unity CLI pipeline package (`unity command …`). On a
representative raid:

- 40–48 castle rooms instantiated, ~12–15k triangles of modelled geometry
- all 5 loot prefabs present; all 10 enemy prefabs reachable across 40 seeds (523 spawns)
- every spawned enemy on the NavMesh (13/13, 12/12, 15/15 on separate runs)
- same seed twice produces an identical garrison
- 116/116 tests pass (12 EditMode, 104 PlayMode)

Screenshots in `Raid_Scene_Verification/`.
