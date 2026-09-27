# Handoff: roofs, then multi-cell rooms, then upper floors (2026-09-26)

Agreed with the owner in a design conversation on 2026-09-26; nothing here is built yet. Order:
**roofs first**, then **rooms spanning 2, 3 or 4 grid cells** (the bespoke per-age rooms), then
**upper floors**. Each gets its own design, plan and build. Only the roofs design was worked
through; the other two still need theirs.

## Roofs: decided

- **Visual only, for now.** Nobody stands on roofs; what is walkable up high belongs to the floors
  project.
- **Approach A: separate roof tiles placed by the generator** (chosen over baking roofs into every
  room model, or generating roof meshes in Unity). The art bible already plans it this way:
  `docs/art/bronze.md` says the megaron's "roof is a separate tile", with a smoke-hole socket into it.

### Section 1: what gets a roof (approved)

- Roofed: every enclosed room cell (OuterBailey, InnerWard, Keep, Crypt).
- Not roofed: curtain-wall cells (wall, bastions, corners, gate keep their own tops; Late Medieval
  towers already have conical roofs, `castle_builders_late_curtain.octagonal_tower`), the drawbridge,
  carved courtyards and the curtain strip. The open spaces keep the night sky
  (`docs/4-systems/atmosphere.md`, "The sky shows through overhead").
- A roof tile per age and zone, 12 × 12 m, underside as ceiling, sitting on the wall top at the zone
  height (`cb.ZONE_HEIGHT`, the same in every age): OuterBailey 3.6 m, InnerWard 4.0 m, Keep 4.6 m,
  Crypt 3.0 m above the 0.30 m floor slab (`docs/4-systems/scale.md`, "Rooms").
- Styles:

| Age | OuterBailey and InnerWard | Keep | Crypt |
|---|---|---|---|
| Bronze | Flat reed-and-clay on round-pole joists, parapet lip | Flat, smoke hole over hearth rooms | Heavy flat slab |
| High Medieval | Pitched slate, gables toward the archways | Flat lead with battlements | Stone barrel vault |
| Late Medieval | Pitched red tile | Flat, crenellated | Stone barrel vault |
| Powder | Pitched slate, dormer stubs | Flat, gun parapet | Brick barrel vault |

- Pitched ridges run along the longer run of neighbouring roofed rooms, so a row reads as one
  building. Block-edge tiles get a finished eave; interior joins a plain valley.
- A room with a hearth fire anchor gets the smoke-hole variant.

### Section 2: how roofs get in (approved)

- Pipeline like the door plugs: builders in each age's `Tools/AssetPipeline/castle_builders_<age>_*.py`,
  exported by `build_assets.py`; a new `CastleRoofForge` editor tool (modelled on
  `Assets/_Project/Scripts/Editor/CastleDoorPlugForge.cs`) registers them in each age's
  `CastleRoomRegistry` under a new `RoofTiles` list keyed by zone and variant (edge, interior,
  smoke hole).
- Placement: a new step in `ProceduralCastleGenerator.Generate`, after `SealOpenArchways`, one tile
  per enclosed room. Variant and ridge direction come from the layout alone (neighbours), never the
  seed's random stream, so every peer builds the same roofs and loot, guards and arrival do not shift.
- Solid but not walkable: colliders (a ceiling you can jump through looks broken, and so do thrown
  loot or a Levo'd guard going through it), excluded from the NavMesh with a `NavMeshModifier` set
  not-walkable. **Saltus under a roof stops at the ceiling**; the full 13 m leap is for the open.
- Lighting: the fire-glow linecast (`CastleAtmosphere.GatherScatterLights`) already treats solid
  geometry as blocking, so indoor fires stop glowing through roofs; the moon's shadows leave
  interiors fire-lit. The fog pass is unchanged.
- Budget: 2–4k triangles a tile, about 40 roofed rooms a castle, so roughly 100–150k added. Measure
  on a real castle; do not assume.

### Still to design for roofs (section 3)

- Testing: an EditMode or PlayMode test that every enclosed room gets exactly one roof at its zone
  height and no curtain, courtyard or strip cell does; that roofs are off the NavMesh (the NavMesh
  audit, `Tools/Plunderspell/Audit Castle Navigation`, must find no walkable area above wall height);
  that no room's own geometry pokes through its roof (the keep stairwell's gallery and any tall
  furniture: check each room prefab's top against its zone height, and raise or trim before placing).
- Then write this up as a spec (`docs/superpowers/specs/`) and an implementation plan.

## Multi-cell rooms: owner's brief only

"Certain rooms can take up 2, 3 or 4 units in the grid; these would be the more bespoke rooms for
each age." Not yet designed. Questions to open with: footprint shapes (1×2, 1×3, 2×2, L?), which
zones allow them, how the generator reserves a footprint and keeps the interior one connected region
(`BuildInterior` carves courtyards one cell at a time today), how archways and door plugs work along
a long wall, and one roof spanning the footprint (the roof design above assumed single cells).

## Upper floors: owner's brief only

"Move on to multiple floors." Not yet designed. It builds on multi-cell rooms (a stair hall or
tower wants a bigger footprint) and on roofs (what becomes walkable up high).
