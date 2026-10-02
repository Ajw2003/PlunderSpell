# Bespoke navigation to replace the NavMesh (plan draft, 2026-10-01)

Owner's request (2026-10-01): the director controls movement. Replace Unity's NavMesh wholesale with
our own navigation, built for the procedurally generated castle, networked, using the event system
and integrating efficiently with random generation. The fresh guard drops the overheard chatter, the
alarm-scaled vision range and the dynamic physics body. Part of #203. The guard as it was is frozen
in `docs/reference/guard-legacy/`, tag `guard-legacy-2026-10-01`.

**Reading of "the director controls movement".** A guard's state decides *where* to go (patrol here,
chase that player). The director's navigation decides *how*: the route, the steering, spacing
between guards, and actually moving the body. States send move requests as events, and the
navigation answers with events (path ready, arrived, blocked).

## What uses the NavMesh today (scout inventory, 2026-10-01)

- **Guards.** `CastleGuard`: SetDestination, Warp, SamplePosition, CalculatePath, nextPosition.
- **Monsters.** `MonsterStateMachine.cs:144,261-277`: SamplePosition and SetDestination. (Not guards.)
- **Items.** `Items/Item.cs:112,192` hold a NavMeshAgent. (Not guards.)
- **Spells.** `PrimarySpellEffects.cs:120-128`: Frango checks `isOnNavMesh` and calls `agent.Move`.
- **Spawning.** `GuardSpawner.cs:169-181` validates spawn-to-home paths.
- **Bake.** `CastleNavMeshBaker` bakes at runtime. Three scenes hold a NavMeshSurface (RaidScene,
  RaidScene.Scaffold, CastleBench).
- **Editor.** `CastleAudit` (floor and loot reachability); `EnemyPrefabForge`, `EraContentForge`
  and `ArtBibleEnemyForge` add NavMeshAgent to all 23 enemy prefabs; `NightAtmosphereForge` adds
  NavMeshObstacle to props.
- **Tests.** `GuardMovementTests` and `GuardShoveTests` build a runtime NavMesh;
  `ScaleInvariantTests` checks for an agent.

## What the generator gives us (and what it doesn't)

**Already recorded:**
- 12 m cells on a 9×9 grid (radius 4);
- rooms joined only through 2.6 m archways on their 4 sides (`SealOpenArchways` plugs the rest);
- strip entrances;
- a 2×-resolution grid A* (`CastlePathValidator`) that already runs at generation time;
- generation that is a pure function of the seed on every peer.

**Not recorded:**
- furniture footprints (they exist only as prefab colliders);
- stairs, galleries and floor levels (geometry only).

## Design

1. **Nav tiles authored once per room module, not baked per raid.** An editor tool probes each room
   module prefab once, with physics casts against its colliders, and records:
   - a walkable grid for that module (proposed 0.5 m cells, 24×24 per module), each cell's floor
     height and level, which captures stairs and galleries as connected cells with a height step;
   - the archway portal cells on each side.

   This is stored alongside the loot and fire anchors in `CastleRoomModuleData` (or in a sidecar
   asset). It's the same pattern as `CastleLootAnchorImporter`. The tool reruns whenever room
   meshes change.
2. **The castle graph is stitched at generation.** In the same deterministic pass that places
   modules, each placed module's tile is rotated into place, and tiles are joined through matching
   archway portals. Sealed archways stay closed. That gives two layers:
   - a coarse **portal graph**: one node per open archway, edges across a room with precomputed costs.
     It has at most a few hundred nodes, so A* on it is microseconds;
   - a fine **cell grid** inside each room, used for the first and last leg and for local steering.

   Nothing is baked at runtime and nothing new goes over the network: every peer can build the
   same graph from the seed. Only the server needs it for guards.
3. **The director's navigation service (server).**
   - It takes `MoveRequest(guard, destination, speed, reason)` events and plans portal A* plus local
     grid paths. Paths are cached per room pair, and nothing allocates per frame (pooled path
     buffers).
   - It moves every guard each tick along its path, with simple separation from other guards.
   - **Before every step it sweeps the guard's capsule** (`CapsuleCast`, non-allocating) and stops
     short of players and walls. That keeps the #200 wall-crush fix without a physics body.
   - It answers with `PathReady`, `Arrived` and `Blocked` events.
   - It replaces SamplePosition/CalculatePath with "nearest walkable cell" and "is reachable"
     queries on the graph.
4. **Networking.**
   - *Phase 1:* the server moves guards and the existing NetworkTransform replicates them, as today.
     That's low risk.
   - *Phase 2:* replicate each guard's path segment and speed as an event, so clients walk it
     locally. Corrections are sent only on divergence, which uses much less bandwidth than syncing
     20 transforms every tick.
5. **Doors.** Locked or barred doors (`CastleLockdown`) mark their portal closed or costed in the
   graph, which they don't do today.

## Open scope questions for the owner

1. **"Wholesale".** Monsters and items also use the NavMesh. Should the new system replace it for
   them too, in the same project, or guards first with monsters and items moved over afterwards?
   (The NavMesh package stays until the last user goes.)
2. **Networking.** Phase 1 (keep NetworkTransform) first, then path replication, or straight to
   path replication?
3. **Grid resolution.** 0.5 m cells, so a guard (r 0.32–0.39) takes one cell plus clearance. Fine,
   or coarser for speed?

## Steps (issues to be created once approved)

1. The nav tile authoring tool and data: probe each room module, store grid, heights and portals.
2. Stitch the castle nav graph at generation, plus graph queries (nearest cell, reachable) and tests
   against today's `CastleAudit` reachability.
3. The navigation service in the director: move requests, portal A* plus local paths, the capsule
   sweep, separation, events.
4. The fresh guard core on top of it (replaces #206). Then the states #207–#213 use move requests.
5. Move the other NavMesh users (spawner validation, Frango, Levo landing, CastleAudit, tests).
   Monsters and items are included or not per question 1.
6. Remove the NavMesh: the baker, the surfaces in scenes, the agents in prefabs and forges, the
   package if nothing is left.
7. Phase 2 networking (path replication), per question 2.
