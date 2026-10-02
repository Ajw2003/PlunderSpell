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
- **Entrances from the strip (2026-09-26, #140).**
<!-- ref:f151 -->
  The strip between the curtain wall and the block of rooms had every archway onto it plugged, so
  a team whose portal opened there could not get in, and the drawbridge (a curtain-wall module
  outside the sealed gate) was an allowed arrival too. `CastleEntrancePlanner.Plan`
  (`Assets/_Project/Scripts/Runtime/Castle/CastleEntrancePlanner.cs`) now picks the strip cells
  that open into the room inward of them: a plain run of wall (not a corner, bastion or the gate),
  at an even position along its side so entrances are two cells apart, with an enclosed room, not
  a courtyard, on its inner side. A side with none of those gets its qualifying cell nearest the
  middle, so every side has a way in. The generator stores them in
  `ProceduralCastleData.EntranceCells` and `IsArchwayConnected` leaves those archways unplugged;
  `CastleArrivalPlanner` only opens the portal on the strip at an entrance, and
  `RaidDirector.FacingTarget` turns a strip arrival toward the entrance instead of the portal.
  Typically 6 to 8 entrances a castle. Pure, so every peer derives the same ones. Courtyards stay
  sealed off from their rooms, as before. `CastleArrivalTests` covers a strip arrival being in
  front of a way in, nobody arriving outside the wall, and every side having an entrance. Checked
  on the real castle for 12 seeds: every arrival has a complete NavMesh path to the crypt, and the
  three strip arrivals (seeds 43, 64, 88) have no plug in front and open looking at the archway
  (`docs/generated/issue-140-entrances/`).
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

## Nav tiles

`CastleNavTile` (`Assets/_Project/Scripts/Runtime/Castle/CastleNavTile.cs`) is each room module's walkable
grid, stored in `CastleRoomModuleData.NavTile`: 24 x 24 cells of 0.5 m in module-local space (module
centred on its origin), two stacked layers per cell column (a gallery over a floor), per layer a walkable
byte and a floor height in centimetres (`short`), plus the archway portal cell indices for each side and a
level count. About 3.5 KB per module, plain arrays, no allocation on read. Two cells connect when their
heights differ by at most `StepHeight` (0.45 m); stairs are walkable cells whose heights step up.

`Tools/Plunderspell/Bake Castle Nav Tiles` (`CastleNavTileBaker.cs`) writes it: each prefab goes into a
preview scene with its import rotation, a downward ray per cell finds the stacked floors, and a guard
capsule (radius 0.4, 1.85 m, lifted one step so the floor and stair risers do not count) clears each
surface. Wall tops and lintels are dropped unless they connect to an archway floor. A portal is an archway
cell where a guard fits, so 4 cells per side, not the full 2.6 m. Re-run it after the castle meshes change.
Results and per-module top-down overlays are in `docs/generated/nav-tiles-2026-10-02/`. The curtain-wall
pieces (gatehouse, straight wall, corner, bastion) have no floor mesh of their own, only walls and towers:
the yard they stand on is the scene's ground plane. For those the baker adds a virtual ground at y = 0
wherever the guard capsule clears (`CastleNavTileBaker.cs:134`) and opens portals along the whole edge
(`CastleNavTileBaker.cs:160`), because the strip runs on into the next piece. The drawbridge is excluded
(it spans a moat) and keeps only its deck.

## Nav graph

`CastleNavGraph` (`Assets/_Project/Scripts/Runtime/Castle/Navigation/CastleNavGraph.cs:42`) is the castle's
walkable map, built at the end of generation (`ProceduralCastleGenerator.cs:115`, after `SealOpenArchways`
so it sees which archways stayed open) and kept on `ProceduralCastleData.NavGraph` (not serialized). It is
a pure function of the layout and the registry, so each peer builds it from the seed and nothing is sent.
Guards are server-side, so only the server needs to query it. Issue #221, plan
`docs/plans/bespoke-navigation.md` Design 2.

- **Fine layer**, `CastleNavGrid`: every placed module's baked tile, rotated by the module's quarter turns
  to its placement (`CastleNavStitcher.cs:40`), as flat arrays of walkable, height and area id. Cell id is
  `module * 1152 + layer * 576 + row * 24 + column`.
- **Open archways**, `CastleNavArchwayRule.cs:17`: room to room is always open; a room to the curtain wall
  is open only at the gatehouse and at a strip entrance (`EntranceCells`); the strip itself is open along
  its length; the cell outside the gatehouse (the drawbridge) is closed, because the gate is sealed
  (`CastleBoundary`). `CastleNavStitcher.FindLinks` (`:85`) turns each open join into a `NavLink`, one per
  archway, and joins the area ids of the cells it connects.
- **Coarse layer**, `CastleNavPortalGraph`: nodes are the links, edges are the walking cost across a room
  between two of its archways, flooded once at build (`ComputeRoomCrossings`, `:62`).
- **Queries**: `NearestWalkableCell` (3D distance within 3 m by default, so a gallery and the floor under it
  are told apart), `IsReachable` (a single comparison of area ids), `FindPath` (`CastleNavGraph.cs:98`):
  portal A* (`CastleNavPortalSearch.cs:54`) picks the archways, grid A* (`CastleNavGridSearch.cs:34`) walks
  each room, and `CastleNavPathFinder.Find` (`:28`) stitches the hops. All buffers are preallocated; the heap
  and the path list only grow during warm-up. The path follows 4-connected cells, so whatever moves along it
  smooths it.
- **The entrance.** The gatehouse is not a way in (sealed since 2026-09-25; raids arrive by portal, on a strip
  entrance or inside the walls). Players come in on the strip, so the strip pieces had to be walkable: they
  now are (see Nav tiles), every strip entrance joins the crypt in the test, and the gatehouse passage is part
  of the graph and joins the strip. Its outward side to the drawbridge is deliberately closed.
- **Not in the graph yet**: furniture the tiles did not capture beyond what the capsule hit at bake time,
  dressing placed per seed (`CastleDressingPlanner`), and doors that lock (`CastleLockdown`). Areas are
  computed once, so a closed door would need the area ids recomputed (issue for #222).

Tests: `CastleNavGraphTests` (`Assets/_Project/Scripts/Tests/Editor/CastleNavGraphTests.cs`): same seed twice
gives the same checksum; strip entrances and the gatehouse join the crypt and the drawbridge stays cut off;
reachability against a NavMesh baked in the Editor over each generated castle, with the audit's 25 floor
probes per room (5 seeds, `docs/generated/nav-graph-2026-10-02/reachability.md`); and query timing with zero
allocation (`timings.txt`). On a probe both sides call floor, reachability never differs. The two differ only
on whether a probe has floor at all (about 6% of probes, mostly rooms with long furniture): the graph is
the more permissive on narrow gaps. The cause is not isolated; an agent radius of 0.4 m on the NavMesh
instead of 0.5 m removed only about a fifth of the differences.
