# Night atmosphere: the castle's look, and the outer bailey (design, 2026-09-24)

Status: **design approved on 2026-09-24, with section 6 (the portal, no outside) added at
approval; not built.** This document is the
spec. The implementation plan that executes it will be written from it, and this file stays the
reference for *what* and *why*.

## Why this exists

The user asked for a second pass on the High Medieval castle, the outer area first, and to "anchor
the aesthetic of this project before we build any more rooms or enemies". Before this, the raid
rendered with Unity's defaults: the default skybox, one sun, no fog, an untouched default Volume
profile (`Assets/Settings/DefaultVolumeProfile.asset`), no custom shaders, and every castle surface
a flat palette colour. The strip between the curtain wall and the rooms was a flat grey plane.

## Decisions taken (2026-09-24, in order)

| Question | Decision |
|---|---|
| Time of day | Night. |
| Surfaces | One world-projected stylised shader on everything now; bespoke hand-painted textures for hero pieces (gatehouse, keep, chapel) later. |
| Outer area | An outer bailey inside the wall. No usable wall walk, no approach outside the wall. |
| Does darkness change gameplay? | Not yet. Visual only, but every fire is tagged as a light source so a stealth system can read it later. |
| Hardware | Steam Deck and a weak PC must run it; it scales up when the machine has headroom. |
| Reference look | None of the four references (Dishonored, Sea of Thieves, Thief/Hunt, Valheim). The user picked the warm, fire-lit fog of two earlier test renders, and set the brief: calm and unsuspecting, then alive once alerted. Renders: `docs/generated/look-samples-2026-09-24/` (`contact-sheet.png` for the rejected four, `calm-vs-alert.png` for the chosen direction). |
| How calm becomes alert | Step with each alarm state (`Calm` → `Stirred` → `Roused` → `HueAndCry`), easing over ~2 s. |
| How fires get into a generated castle | Authored fire anchors per module, the same pipeline loot anchors use. |
| Audio | Out of scope. This pass is visual only; bells and ambience come later. |
| Leaving the castle (added at approval) | Players cannot leave. They arrive through a portal at a random spot inside the walls, and the outside is cut entirely. |
| How the team gets out | The same portal they arrived by. When the raid timer runs out it closes, and anyone outside it is stuck. |

The look samples were rendered in Blender (`Tools/LookSamples/`) because the Unity Editor was
blocked on a scene-recovery dialog that session. They compare light, fog and grade only.

## 1. Art direction

**Anchor: a sleeping castle in warm fog. Fire is the only warmth and the only safety. When the
alarm rises, the fire spreads and reddens.**

1. **Fire leads, the moon supports.** Warm light comes only from flame (sconces, braziers,
   hearths, beacons, lanterns). The moon is a faint cool fill that separates silhouettes from the
   sky and never lights a scene by itself. Anywhere far from fire is dark enough to hide in.
2. **The fog is the canvas.** Firelight shows as glowing halos in the fog, not only as lit walls.
   Distance falls off into warm-black haze, which also hides the edge of the world.
3. **Colour follows the art bible's pigment rules** (`docs/art/BRIEF.md`, "Look"). Orpiment gold is
   treasure only. Madder red is reserved for the alarm, full-alert fire and blood, so a red castle
   means trouble. Lapis and verdigris are magic only, so spells and portals are the only cool,
   saturated glow in the scene.
4. **Stone goes darker at night.** The limestone swatch (`#A89F8A`) renders near-white under light
   in every sample. Night stone is a darker, warmer value of it, so fire reads against it.
5. **Stylised, painterly, chunky; never photographic.** Soft banded light, soot in crevices, worn
   edges. Silhouette before detail.
6. **Every era obeys the same rules.** Bronze, Late and Powder keep their palettes but share the
   night, fog and fire logic. Only the stone and flame tints shift per era.

## 2. Lighting and the alarm states

**Fire anchors (data).** Each castle module's Blender builder registers fire anchors beside its loot
anchors: a position, a kind, and the alarm state that first lights it. They export to a new
`Assets/_Project/Data/Castle/CastleFireAnchors.json` and are imported onto the room prefab, mirroring
`CastleLootAnchors.json` and `CastleLootAnchorImporter`. Kinds:

| Kind | What | Lit from |
|---|---|---|
| Sconce | wall torch | Calm (some) / Stirred (all) |
| Brazier | free-standing iron bowl, yards and halls | Calm, flares at Roused |
| Hearth | fireplace, forge, kitchen fire | always |
| Beacon | large fire on the wall walk or a tower | Roused |

**Fire sources (runtime).** One `FireSource` prefab per kind: a stylised flame (flipbook or
particles), embers, a flickering point light, a fog halo sprite (section 5), and a `LightSource`
tag that records radius and intensity for a future stealth system. At full alert a fire grows
about 1.5×, burns hotter and shifts toward madder red.

**The director.** One `CastleAtmosphere` component subscribes to
`AlarmFSMManager.AlarmStateChanged` (`Assets/_Project/Scripts/Runtime/Alarm/AlarmFSMManager.cs:50`),
which already fires on every peer. Each client blends its own visuals, so nothing new is
networked.

| | Calm | Stirred | Roused | HueAndCry |
|---|---|---|---|---|
| Fires lit | hearths, some sconces | all sconces | braziers flare, beacons lit | everything, at maximum |
| Flame | amber | amber | orange | madder |
| Fog | warm, thick | warmer | glowing orange | red-orange glow |
| Grade | soft | a little more contrast | punchier | hard contrast, red lift |

Transitions ease over about 2 seconds.

**Shadow budget.** Only the nearest fires cast shadows: 2 on Low, 4 on Medium, 8 on High. The rest
light without shadows up to a per-level cap; beyond that a distant fire is flame and halo only, with
no light. A castle of fifty fires stays affordable on a Deck.

## 3. Surfaces: `Plunderspell/Surface`

One Shader Graph shader for the whole castle, with a cheaper variant on Low.

1. **Base colour** from the existing palette atlas and UVs (`Tools/AssetPipeline/mesh_kit.py:100`),
   unchanged. No module needs new UVs.
2. **World-projected detail.** Four greyscale tiling textures, projected by world position (the
   triplanar method: three projections blended by surface angle). The detail is chosen by pigment.
   Every mesh already has one material per pigment (`WallStraight_oak`, `_iron` and so on), so the
   import step can map them automatically: stone pigments get block coursing and chisel noise, oak
   gets planks and grain, iron gets hammered pitting, cloth gets a soft weave. The textures come
   from a small committed Python generator, 512² each.
3. **Soot and wear.** The Blender pipeline bakes a "dirty vertex colours" pass into every mesh at
   build time. It darkens crevices, inner corners and the undersides of arches, and brightens outer
   edges. The shader reads it. Every module is rebuilt once.
4. **Painted, banded light.** Two or three soft steps instead of a smooth falloff, with a
   warm-dark tint on the shadowed side instead of grey. This covers the moon, every fire and SSAO.
5. **Night stone tint.** A per-era darkening value for stone pigments, instead of re-authoring the
   palette.

Enemies and plunder keep their ArtForge baked textures but get the same banded light, so they sit in
the same painted world. On Low the triplanar detail becomes a single blend; soot and banding stay,
because they cost almost nothing. The later hero-piece pass gives the gatehouse, keep and chapel real
UVs and painted textures through the same shader in a texture mode.

## 4. The outer bailey

The curtain wall is a ring of 12 m cells (`docs/systems/castle.md:17`). Each cell raises its wall on
the outer edge, which leaves a strip about 10 m wide between the wall and the rooms. That strip is
the bailey. The courtyards carved out of the interior become outdoor places with a purpose.

**Curtain-cell dressing variants.** New Blender-built variants of `WallStraight` and `WallCorner`.
Same wall, with the strip furnished against it:

| Variant | Dressing |
|---|---|
| Lean-to | timber shed under the wall, barrels, sacks |
| Woodpile | stacked logs, a hay cart, a chopping block |
| Pens | wattle fence, trough, a chicken coop |
| Training | pells, a weapon rack, straw butts |
| Gate yard | carts, hay bales, the main brazier pair; always beside the gatehouse |
| Plain | an open stretch, so it does not read as cluttered |

Each variant carries fire anchors (wall sconces and a yard brazier). The generator picks variants by
seed, never the same variant twice in a row.

**Ground.** Curtain cells carry a packed-earth path looping the ring, with mud and cobble patches
from the stone detail texture. The flat grey plane is gone.

**Courtyards.** An empty interior cell gets a courtyard module chosen by zone: herb garden, midden
or well yard in the outer bailey; tiltyard or cloister garth in the inner ward; a small formal
garden near the keep. Each has fire anchors.

**Rules that must hold:**

- **Navigation.** Dressing leaves a walkable lane of at least 2.5 m around the whole ring.
  `Tools/Plunderspell/Audit Castle Navigation` (`Assets/_Project/Scripts/Editor/CastleAudit.cs`)
  must still report every room reachable and 100% of floor reachable on its five seeds.
- **Cover, not walls.** Sheds and carts break sight lines but never block the path.
- **Loot is unchanged.** Loot still never spawns on a curtain-wall cell
  (`docs/systems/castle.md:83`). Loot in the sheds would be a gameplay change and needs its own
  decision.
- **Eras.** High Medieval first. The other eras get their own dressing kits in the same variant
  slots later.

## 5. Fog, post-processing and quality levels

**Fog, in layers:**

1. **Base fog, every level.** URP's built-in distance fog, with colour and density set by
   `CastleAtmosphere` per alarm state, plus height fog computed in the surface shader (thick at the
   ground, thinning upward, so towers rise out of it).
2. **Fire halos, every level.** A soft additive sprite on every flame imitates firelight scattering
   in fog. This is most of the look in the chosen renders, and it is cheap.
3. **Volumetric fog, High only.** A custom URP render pass (Render Graph) that raymarches the fog at
   quarter resolution for light shafts from the moon and through arrow slits. URP has no built-in
   volumetrics, so this is written from scratch. **It is the riskiest item and is built last and
   optional.** Layers 1 and 2 must look finished without it.

**Post-processing.** Four Volume profiles (Calm, Stirred, Roused, HueAndCry) blended by
`CastleAtmosphere`:

- Tonemapping: ACES, so the fire runs hot without clipping.
- Grade: a warm split (amber highlights, a cool moon edge in the shadows), reddening per state.
- Bloom: carries the flame glow; stronger per state.
- Vignette: subtle; tighter at HueAndCry.
- Film grain: very light, High only.
- Not used: motion blur and depth of field. Both cost readability in a co-op fight.

**Quality levels** replace today's `Mobile` and `PC` (`ProjectSettings/QualitySettings.asset`):

| | Low (Deck, weak PC) | Medium | High |
|---|---|---|---|
| Shadowed fires | 2 | 4 | 8 |
| Unshadowed fires | 16 | 32 | all |
| Moon shadows | 1 cascade, 30 m | 2 cascades, 50 m | 4 cascades, 80 m |
| Surface detail | single blend | triplanar | triplanar |
| SSAO | off | half resolution | full |
| Volumetric fog | off | off | on |
| Anti-aliasing | FXAA | SMAA | SMAA |

A Graphics setting joins the existing Settings screen. The default is Medium; Low is selected
automatically on a Steam Deck.

## 6. Arriving and leaving: the portal, and no outside

Added when the spec was approved (2026-09-24). The user: "explicitly prevent the player from leaving
the castle for now, spawn them randomly in the castle via the portal instead of the baked spawn
location currently outside, and then we cut the need for an outside wholesale."

**Today.** `CastleSpawnResolver.ResolveSpawn` places the team just inside the gatehouse
(`Assets/_Project/Scripts/Runtime/Castle/CastleSpawnResolver.cs:56`), and
`RaidDirector.PlacePlayerAtSpawn` fans the players round that point
(`Assets/_Project/Scripts/Runtime/Raid/RaidDirector.cs:374`). The `ExtractionZone` pad stands by the
gate, outside the wall. The gatehouse is the castle's "extraction exit", which the guard and loot
planners keep clear (`GuardPlacementPlanner.cs:89`, `LootPlacementPlanner.cs:84`).

**Arrival.** The team steps out of one portal at a random, reachable spot inside the walls:

- Chosen from the raid's seed, so every peer computes the same spot with no new networking.
- Candidates: open floor in the outer bailey strip and in outer-bailey and inner-ward rooms. Never
  the crypt or the keep, never the sealed gatehouse, and never within a set distance of a guard
  post. The navigation audit's reachability check must pass from the chosen spot.
- The players stand in a ring round the portal, reusing the existing per-owner offset.
- The portal is lapis, the art bible's portal and voice colour, and carries its own light and fog
  halo, so it reads as a landmark from across the bailey.

**Leaving.** The portal is the `ExtractionZone`. It spawns with the portal instead of sitting in the
scene by the gate. Its rules are unchanged: standing in it starts the leaving countdown, and when the
raid timer runs out it resolves, banking only the loot inside it and counting only the living players
in it (`Assets/_Project/Scripts/Runtime/Extraction/ExtractionZone.cs:200`). **Once it closes, anyone
outside is stuck:** they are not counted as saved, whatever they carry is lost, and the summary names
them as left behind. Any harsher cost for being left behind (lost gear, death) is a separate gameplay
decision, not made here. In the last minute of the timer the portal visibly falters (it flickers and
shrinks), so the closing is readable without a HUD glance.

**No way out.**

- The gatehouse becomes a sealed set piece: portcullis down, gate barred. It keeps its role id, so the
  generator and planners are untouched, but it is no longer an exit and never a spawn.
- A boundary collider runs along the top of the curtain wall, so a spell that launches a player
  (Levo, a Frango blast) cannot throw anyone over it.
- **The outside is gone.** No ground plane beyond the wall, no pad by the gate. The ground ends a
  short margin past the wall's foot, and past the battlements there is only fog and the night sky.
  This removes the "anything outside the wall" work altogether.

**Rules that must hold:** every seed yields an arrival spot (with a logged fallback to the old
gate-inside point if none qualifies); the arrival spot is reachable to every room on the NavMesh; the
extraction tests still pass with the zone spawned at runtime; a player launched upward at the wall
lands back inside.

## Verification

Every step is checked by eye in the real Editor, not only by tests:

- Fixed-camera captures of the CastleBench bailey (the in-engine version of the Blender vignette)
  and one interior, in Calm and in HueAndCry, on Low and on High, committed under
  `docs/generated/`.
- Unity Profiler frame timings for the same views. Low must hold 60 fps on a machine throttled to
  Deck-class performance.
- The navigation audit, unchanged pass criteria, after the bailey dressing lands.
- EditMode tests for the pure logic: which anchors are lit per alarm state, the shadow budget's
  nearest-N selection, variant selection by seed (determinism, no repeats, gate yard placement),
  and arrival-spot selection by seed (determinism, excluded zones, distance from guard posts,
  fallback).
- A Play-mode run through Lair → Set Out → portal arrival → carry loot back → leave, and one where
  the timer runs out with a player outside the portal.

## Build order

Each step is committed and pushed on its own and ends with captures.

0. **Portal and the sealed castle.** Seeded arrival spot, the portal carrying the `ExtractionZone`,
   the sealed gatehouse, the wall-top boundary, the outside removed. Comes first because every
   capture after it should be taken from inside a castle with no outside.
1. **Night baseline.** Quality levels, moon, sky, base fog, the four Volume profiles and
   `CastleAtmosphere` blending them on `AlarmStateChanged`. Visible in the existing castle at once.
2. **Fire.** `FireSource` prefabs, fire anchors through the asset pipeline and importer, the
   per-state lighting and the shadow budget. Fire halos.
3. **Surfaces.** Detail texture generator, vertex soot bake in the pipeline, `Plunderspell/Surface`
   and the pigment-to-detail import mapping; enemies and plunder moved onto banded lighting.
4. **Outer bailey.** Curtain dressing variants, the ring path, courtyard modules, generator
   selection, the navigation audit re-run.
5. **Volumetric fog (High, optional).**

## Out of scope

Audio (bells, ambience); stealth detection by light; the wall walk; anything outside the curtain
wall (now removed rather than deferred, see section 6); any penalty for being left behind beyond
today's; loot in bailey dressing; castle-revamp phases 3–5 (themed wings, the crypt below, doors and
hazards in `docs/plans/castle-revamp.md`), which this pass neither does nor blocks; bespoke hero
textures (the later B pass); dressing kits for eras other than High Medieval.

## Risks

- **Volumetric fog** is custom rendering code; see section 5. It is last and optional for that
  reason.
- **Banded lighting** means a custom lighting function inside Shader Graph that loops over Forward+
  lights. It must stay compatible with URP 17.3's Forward+ light loop; if it cannot, the fallback is
  URP Lit plus the grade, losing the banding but keeping everything else.
- **Rebuilding every module** for the vertex soot bake touches every castle FBX. The rebuild is
  scripted and deterministic, and the loot-anchor check (`build_room_sheets.py --models`) and
  the navigation audit must pass after it.
