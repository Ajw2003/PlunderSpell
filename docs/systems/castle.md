# Castle

Builds the fortress a raid happens in, from a single seed, identically on every machine. If this
is wrong, either the game can't agree on a floor plan across the network, or it traps a party
inside a building with no way out.

## What it owns

Deterministic layout generation, verifying that layout is walkable, replicating just the seed
across the network, and the door/lockdown state the alarm drives. It does not decide what loot or
guards go where (`raid.md` — `LootPlacementPlanner`/`GuardPlacementPlanner` consume the layout
this produces) and it does not decide when to escalate (`AlarmFSMManager`, see `alarm.md`) —
`CastleLockdown` only reacts to that state.

## How it works

- **`ProceduralCastleGenerator.Generate(seed)`** builds a closed curtain wall around a dense block
  of concentric wards. The outer Chebyshev ring at `m_curtainWallRadius` (4 by default, so the
  castle is 9 cells / 108 m across) is filled completely: a `WallCorner` drum at each of the four
  turns, a `Bastion` every `m_bastionSpacing` cells along the runs, exactly one `GatehouseModule`
  on the +X axis cell with its `Drawbridge` in the cell immediately outside, and `WallStraight`
  everywhere else. Everything inside that ring is enclosed rooms, zoned by radius — `Crypt` at the
  origin, then `Keep`, `InnerWard`, `OuterBailey` — filled to `m_interiorFillFraction` (0.9, giving
  ~44 rooms) with the remainder left open as courtyards. A `System.Random` seeded once at the top
  drives every decision — never `UnityEngine.Random`, whose global static state is shared with
  VFX/audio and would make generation order-dependent (see `plunderspell.md` §7).
- **Courtyards are carved, not grown.** Candidate cells are shuffled by the seeded RNG and removed
  one at a time, each removal kept only if a flood fill shows the remaining interior is still a
  single 4-connected region. The origin (the path's start) and the cell inward of the gate (its
  end) are never candidates. That is what guarantees the A* validator can always walk out.
- **Wall pieces face outward, and that is not the same rotation as a room.** `build_wall_straight`
  in `Tools/AssetPipeline/castle_builders.py` raises its wall on the module's *south* side, which
  lands on local −Z after the Blender Z-up correction, so `RotationForOutwardWall` yaws the module
  until local −Z points away from the castle — the opposite of `RotationForFacing`, which points a
  room *toward* its anchor. `build_wall_corner` raises south *and* west, so unrotated its two faces
  cover the south-west pair; `RotationForCorner` yaws by that piece's south face, which carries the
  west face onto the corner's other outward side.
- **`CastlePathValidator.ValidatePath`** rasterises every placed module into a walkable grid at
  `CellScale`×`CellScale` resolution per layout cell (so adjacent modules' footprints touch and
  stay 4-connected) and runs a 4-directional A* from the crypt start to the extraction exit. This
  runs once at generation time, never per-frame.
- **`CastleNetworkManager`** is the only thing that crosses the network: the seed, as a PurrNet
  `SyncVar<int>`. Every peer's `OnSeedChanged` handler calls the same deterministic `Generate`
  locally — no mesh, module list, or transform is ever sent. If a layout fails validation, the
  server retries with `seed + 1` (not a fresh random number) up to `maxRetries`, so the seed it
  finally replicates is the one every client independently reproduces.
- **Doorways and door plugs.** Every enclosed room module is authored with an archway on all four
  sides (`_shell` in `Tools/AssetPipeline/castle_builders.py`). The generator places modules on the
  grid without consulting their geometry, so a room with archways on only some sides would sooner
  or later meet its neighbour archway-to-blank-wall. Opening all four makes every 4-adjacency a
  real connection regardless of rotation. The cost is that a room on the edge of the block has
  openings facing nothing, so `SealOpenArchways` runs after placement and plugs every archway that
  does not lead into another enclosed room — including archways onto a curtain-wall cell, which is
  a wall and not somewhere to walk. The single exception is the gatehouse cell, which is the way
  out. Plugs come from `CastleRoomRegistry.GetDoorPlugForZone` (authored by `CastleDoorPlugForge`),
  one per enclosed zone because the archway size is derived from the zone's wall height — see
  `scale.md` ("Archways").
- **The gatehouse is sealed (2026-09-25).** `AssignExtractionExit` still marks it, so
  `ExtractionExitIndex` points at a `CurtainWall` module and `LootPlacementPlanner` and
  `GuardPlacementPlanner` still skip it, but it is no longer the way out or the spawn: raids arrive
  and leave by a portal inside the walls (`raid.md`, "Arriving and leaving by portal").
  `CastleBoundary` (`Assets/_Project/Scripts/Runtime/Castle/CastleBoundary.cs:29`) closes the gate
  arch and the wall tops with four invisible walls on the curtain's outer face.
- **`CastleLockdown`** subscribes to `AlarmState` and locks (`Roused`) then bars (`HueAndCry`)
  every door — deliberately one-way, matching the alarm's own latch, so the castle can't hand back
  a mistake the players already paid for.

- **Room layout grammar (2026-09-23, castle revamp).** Every enclosed room keeps a clear cross,
  `|x| < 1.6 m` or `|y| < 1.6 m` up to 2 m above the floor, between its four archways; furniture
  goes in the four corner quadrants against the walls, and anything raised stands on something
  grounded. `validate_in_blender.validate_castle_layout` fails the asset build if a room breaks it
  or if any mesh island floats. Each zone has its own floor and trim pigment (`ZONE_FLOOR`,
  `ZONE_ACCENT` in `castle_builders.py`). Stairwells climb to a gallery or dais, never into the sky.
- **Navigation is audited, not assumed.** `CastlePathValidator` walks a rasterised grid and cannot
  see furniture. `Tools/Plunderspell/Audit Castle Navigation` (`CastleAudit.cs`) checks 25 floor
  points per room and every loot piece on the real baked NavMesh, for five seeds. Results and
  before/after overlays: `docs/generated/castle-survey-2026-09-23/`.
- **Loot sits on furniture, not on the floor.** Each room builder registers loot anchors (table
  tops, chest lids, the top board of a bookcase, altars, the throne dais, crypt niches) through
  `_anchor` in `castle_builders.py`. `build_assets.py` writes them to
  `Assets/_Project/Data/Castle/CastleLootAnchors.json` in Blender space, and
  `Tools/Plunderspell/Import Castle Loot Anchors` (`CastleLootAnchorImporter.cs`) copies them into
  `CastleRoomModuleData.LootAnchors`, choosing the axis mapping that puts the most anchors on a
  surface (it reports the hit rate; 59/59 today). `LootPlacementPlanner.Plan(..., registry)` puts
  each piece on a random anchor of its room, 0.08 m above it, and never on a curtain-wall cell.
  Re-run the importer after rebuilding the castle meshes. The audit counts a piece as reachable
  when walkable floor the spawn can path to lies within 1.6 m across and 1.8 m below it, which is
  "can a player reach it", not "can a player stand on the table".
- **A stair's foot faces open floor.** The NavMesh does not join the side of a stair to the floor,
  so a flight whose bottom step touches a wall cannot be climbed. The keep stairwell is an L: first
  flight west along the south wall from the walkway, a corner landing, second flight north.

## Invariants

- **A raid never starts in a castle the crypt can't reach the exit from.** Generation is retried
  (seed walked forward, not re-rolled from scratch) until `CastlePathValidator` passes; see
  `raid.md`'s "A raid never starts in a castle you cannot walk out of."
- **Only the seed is ever sent over the network.** Generation must stay a pure function of it —
  any source of nondeterminism inside `Generate` (wall-clock time, `UnityEngine.Random`, iteration
  order over an unordered collection) breaks every client's ability to agree on the same building.
- **Lockdown only ever gets stricter.** `CastleLockdown` has no path back to unlocked; it mirrors
  `AlarmFSMManager`'s own latch at `Roused`.

- **Two 4-adjacent enclosed rooms are always door-connected, and no archway opens into empty
  space.** The first half comes from every room opening on all four sides, the second from
  `SealOpenArchways`. `CastleGeneratorTests.Test_AdjacentRoomsAreDoorConnected` holds both.
- **The curtain wall is a closed loop.** Every cell on the outer ring perimeter carries a
  `CurtainWall` module and there is exactly one gatehouse, for every seed.
  `Test_CurtainWallIsAClosedLoop` holds it.
- **The interior stays one 4-connected region and stays playable.** Courtyards are only carved
  where connectivity survives, and `Test_InteriorRoomCountIsPlayable` keeps the room count in
  40-60 so the castle is neither a corridor nor a city.
- **A module never leaves its grid cell.** `room_kit.FOOTPRINT` equals
  `ProceduralCastleGenerator.cellSize` (12 m) exactly, and `validate_in_blender` fails the asset
  build for any castle module whose XY bounding box reaches past ±6.05 m. Without that gate a
  module quietly grows into its neighbour's cell, which is what issue 19 was.

## Traps

- **The previous castle must leave physics before the next one is baked.** `ClearGenerated`
  deactivates each old piece before `Destroy`, because `Destroy` lands at the end of the frame and
  the next castle is generated and baked within that same frame. Without it the NavMesh bake saw
  both castles overlaid and the old walls sealed the new doorways: from the second raid on, 50–99%
  of the castle was unreachable for players' guards and the audit alike.

- **The layout depends on whether a registry is assigned.** `PickWeighted` consumes a random draw
  when there is a room pool and returns early without one when there is not, so the same seed
  produces a different castle with and without prefabs. Compare two layouts only when both
  generators have the same registry.
- **`UnityEngine.Random` anywhere in the generation path is a silent multiplayer desync,** not a
  crash — clients drift apart with no error, because nothing here re-validates against a
  replicated layout, only against the seed. See `plunderspell.md` §7 for why this is called out as
  a hard constraint rather than a style preference.
- **A module's rasterised footprint must stay `CellScale`-aligned with its neighbours.** A room
  prefab whose collider doesn't match the grid footprint the generator assumed can pass placement
  but fail path validation (or the reverse), because the two use different representations of the
  same layout.
