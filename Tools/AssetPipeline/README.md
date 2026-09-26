# Plunderspell asset pipeline

Procedural, script-driven prop and level-module generation for the vertical
slice. Every entry in `asset_specs.py` corresponds 1:1 to either a
`ScriptableObject` in `Assets/_Project/Data/{Inventory,Loot}` (weapons/loot,
built in `builders.py`) or a `RoomId` in
`Assets/_Project/Data/Castle/CastleRoomRegistry.asset` (castle modules, built
in `castle_builders.py` on top of the architectural primitives in
`room_kit.py` — floor/wall/door shells, crenellations, towers, spiral
stairs) that had no mesh behind it — see the art direction in
`docs/plunderspell-moodboard.html` (pigment palette, per-era kit lists) for
where the shapes and colours come from, and the pigment-to-zone mapping at
the top of `castle_builders.py` for how that palette was extended to
architecture without adding new pigments (the 4x4 atlas was already full).

## Requirements

- Blender (tested against 4.0.2, installed here via `apt-get install blender`)
- Blender's Python needs `numpy` for the FBX exporter add-on
  (`apt-get install python3-numpy` — the apt Blender package uses the
  system Python, not a bundled interpreter, so this is a normal apt install,
  not a `pip install` into Blender)
- `pip install pillow trimesh` for the palette texture generator and the
  standalone validator

## Regenerating everything

```bash
Tools/AssetPipeline/run_pipeline.sh            # palette -> build -> validate -> render -> sheet
Tools/AssetPipeline/run_pipeline.sh --force    # ignore the manifest, rebuild everything
```

Run it through the wrapper rather than calling the steps by hand: it sets
`PYTHONHASHSEED=0`, which the pipeline needs to be reproducible (see
below). The individual steps still work standalone if you need them:

```bash
python3 Tools/AssetPipeline/make_palette_texture.py   # only if palette.py changes
PYTHONHASHSEED=0 blender -b -P Tools/AssetPipeline/build_assets.py
python3 Tools/AssetPipeline/validate_asset.py --all
```

## Reproducibility: why nothing churns on a no-op rebuild

This repo commits its build artifacts, so a rebuild that changes nothing
must produce no diff. Neither output format gives you that for free:

- **Blender's FBX exporter derives object UIDs from Python string
  hashes**, which are salted per process — so the same mesh exported twice
  differed by 127 bytes. `PYTHONHASHSEED=0` (set by `run_pipeline.sh`)
  takes that to 3 bytes: the minute/second/millisecond that the exporter
  always stamps into the header, which no option disables.
- **bmesh emits faces in a different order from run to run** for props
  built from spheres or the lathe. Verified to be a pure permutation —
  identical faces, windings and normals, just stored in a different
  order.
- **Cycles' sampling is not byte-reproducible**, so re-rendering an
  unchanged prop rewrote ~450KB of PNG for no visual difference.

Rather than fight the formats, the pipeline fingerprints the *mesh*
(`manifest.py`) and simply doesn't rewrite a file whose content is
unchanged. `asset_manifest.json` records each prop's content hash,
triangle count and budget, and the fingerprint its preview was rendered
from. The preview key also includes a hash of `render_previews.py`, so
changing the camera or lights invalidates every preview automatically.

The fingerprint is order-independent but winding-, UV- and
material-sensitive: a flipped normal or a moved vertex still shows up as
a change. Net effect: running the pipeline twice leaves `git status`
clean.

`build_assets.py` builds each prop with `bmesh`, runs the full validation
gate (transforms, manifold/isolated-vertex/quad-dominant geometry, UV
bounds, triangle budget) *before* export, writes:

- `Assets/_Project/Art/Models/{Weapons,Loot}/<Key>.fbx` — the Unity-facing
  deliverable, textures embedded (`embed_textures=True`) so Unity never hits
  a missing material reference on import.
- `/tmp/plunderspell_glb/<Key>.glb` — a scratch companion export used only
  by `validate_asset.py`. trimesh has no FBX loader at all (confirmed by
  checking `trimesh.exchange.load.mesh_loaders` — only glb/gltf are
  registered), so this is the only way to get an *independent*,
  outside-Blender check on the exported geometry.

then re-imports the FBX into a scratch collection and diffs triangle/vertex
counts and material/UV presence against the pre-export mesh, to catch
anything the exporter silently dropped.

`validate_asset.py --all` is the second opinion: pure `python3 + trimesh`,
no `bpy`. It checks the same invariants (UV bounds, isolated vertices,
triangle budget, and manifoldness — computed by *position*, not vertex
index, since every face here owns its own independent UV island by design
and glTF export duplicates a vertex at every one of those seams; a raw
index-based edge check would flag that duplication as thousands of fake
"boundary edges").

## Style: the pigment atlas, not photographic texturing

Every prop samples one shared 4x4 flat-colour texture
(`Assets/_Project/Art/Textures/PlunderspellPalette.png`, generated by
`make_palette_texture.py` from the named pigments in `palette.py` — lifted
directly from the moodboard's CSS custom properties). `mesh_kit.paint()`
UV-maps each face into an inset square inside its pigment's cell. Multiple
faces legitimately sharing one cell is the intended trim-sheet technique
for flat colour, not an accidental UV overlap — what the validators actually
check for is a face's UV *bleeding across* into a neighbouring pigment's
cell.

## Pivot convention

Every builder in `builders.py` is authored bottom-flush: the mesh's lowest
vertex sits at local `Z=0`, and the shape rises along `+Z` from there. The
validator checks this as a real invariant (lowest vertex within 3cm of
Z=0), not just `object.location == origin`.

## Blender → Unity axis conversion

Castle module builders (`castle_builders.py`, `room_kit.py`) place doors and
props in Blender's native Z-up space — `door_sides` names a wall by compass
direction in *that* space. Confirmed empirically against the actual
imported FBX bounds (a room's Y-height in Unity matches its Blender
`ZONE_HEIGHT`; the Drawbridge module's deck, built at a large negative
Blender Y, lands at large *positive* Unity Z):

```
Unity(X, Y, Z) = Blender(X, Z, -Y)
```

Height carries straight across (Blender Z → Unity Y). North/south swap
under the Y negation; east/west pass straight through. Concretely, for
`SocketPoint.Direction` (which is Unity's own compass, independent of the
Blender authoring labels): a Blender `"north"` door → Unity `Direction.South`;
`"south"` → `Direction.North`; `"east"`/`"west"` → the same-named Unity
direction. Every door in `room_shell` sits centred on its wall (local
offset 0 along the wall), at Unity world position `±(HALF - WALL_T/2)` on
the relevant horizontal axis and Unity Y = `FLOOR_T + doorHeight/2` where
`doorHeight = min(zoneHeight * 0.72, zoneHeight - 0.3)` (see `door()` in
`room_kit.py`). This is what
`Assets/_Project/Prefabs/Castle/*.prefab`'s `SocketPoint` children are
placed from.

## Rendering preview screenshots

There's no Unity Editor (or GPU/EGL) in this environment, so previews are
rendered with Blender's Cycles CPU backend against the already-exported
FBX files — what you see is exactly what Unity will import, embedded
texture included:

```bash
PYTHONHASHSEED=0 blender -b -P Tools/AssetPipeline/render_previews.py
python3 Tools/AssetPipeline/make_contact_sheet.py      # -> previews/_contact_sheet.png
```

Previews whose prop and rig are both unchanged are skipped (see the
reproducibility section); pass `-- --force` to re-render regardless.

`previews/` is committed so reviewers can see the props without a Unity
Editor or Blender install. Re-run the two commands above and commit the
result whenever a builder or the palette changes — the PNGs aren't
authoritative (the FBX/GLB exports are); they're a rendering of them and
will drift out of date if regenerated and not recommitted.

## Adding a new prop

1. Add an entry to `WEAPON_SPECS` or `LOOT_SPECS` in `asset_specs.py`
   (key, builder function name, triangle budget, output subdirectory).
2. Write the builder in `builders.py`: stack `mesh_kit.add_box` /
   `add_cylinder` / `add_sphere` calls, `mesh_kit.paint(...)` each part with
   a pigment name from `palette.py`, keep the lowest vertex at Z=0.
3. Re-run `build_assets.py` — the report at the bottom names every check
   that failed, per asset. Fix and re-run until `N/N assets passed`.

## Adding a new castle module

1. Add an entry to `CASTLE_SPECS` in `asset_specs.py` with `subdir="Castle"`
   — its `key` must match a `RoomId` already in `CastleRoomRegistry.asset`.
2. Write the builder in `castle_builders.py`. For an enclosed room, start
   from `room_kit.room_shell(...)` (floor + four walls, an archway on every
   side named in `door_sides`) and add 1-3 set-piece primitives so it reads
   as its own place; for a CurtainWall segment, compose `room_kit.wall_run`
   / `tower_drum` / `crenellations` directly (it's the wall itself, not an
   enclosed room). `ZONE_HEIGHT`/`ZONE_ACCENT` at the top of the file give
   every zone a consistent height and accent colour — pull from those
   rather than hardcoding new ones.
3. Use `room_kit.paint_box(...)` (not `mesh_kit.add_box` directly) for
   anything that sits flush against another part (floor, walls, another
   furniture piece) — two independently-built boxes that just touch
   produce exact duplicate-position vertices, which the validator flags;
   `paint_box` grows the box a couple of centimetres so it overlaps instead.
   That doesn't cover two *fully overlapping* boxes (e.g. both walls at a
   corner running the whole footprint) — see the comments in
   `room_kit.room_shell` and `build_wall_corner` for how those are avoided
   instead (one side claims the corner, the other stops short of it).
4. Re-run `build_assets.py` the same way — fix and re-run until
   `N/N assets passed`, same as a prop.

## Verifying the Unity side

The pipeline validates geometry in Blender, which says nothing about whether
Unity's importer is happy with the result. `ArtAssetImportValidator`
(`Assets/_Project/Scripts/Editor/`) closes that gap: it checks every FBX is
indexed by the AssetDatabase, produces a non-empty mesh whose triangle count
matches `asset_manifest.json`, has materials with the embedded palette
texture bound, and has a generated `.meta`.

Run it headless from the repo root (adjust the Unity path for your install):

```bash
# macOS
/Applications/Unity/Hub/Editor/6000.3.15f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -projectPath . \
  -executeMethod ArtAssetImportValidator.ValidateFromCommandLine \
  -logFile Logs/unity-art-import.log

# Windows
"C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe" ^
  -batchmode -nographics -projectPath . ^
  -executeMethod ArtAssetImportValidator.ValidateFromCommandLine ^
  -logFile Logs\unity-art-import.log
```

It exits non-zero on failure and writes `Logs/art-import-report.txt` (both
gitignored). The same checks also run as an EditMode test
(`ArtAssetImportTests`), so they show up in the Test Runner window and in CI
— see `.github/workflows/`.

## In-Editor wiring (done)

This pass used to stop at validated meshes on disk, with prefab/registry
wiring left as a manual in-Editor step. That step is now done, driven live
through the Unity CLI's Pipeline connection rather than hand-edited YAML:

- `Assets/_Project/Prefabs/Loot/*.prefab` and `Assets/_Project/Prefabs/Weapons/*.prefab`
  wrap each Loot/Weapons FBX with a fitted `BoxCollider` plus `LootPickup`
  (loot, wired to its matching `Data/Loot/*.asset`) or `Item` (weapons —
  no world-pickup system exists for `InventoryItem` yet, so these are a
  plain grabbable object rather than raid-economy loot). The 5 real loot
  pieces are wired into `GeneratedLootTable.asset`'s `Entries`.
- `Assets/_Project/Prefabs/Castle/*.prefab` wrap each Castle FBX with a
  `MeshCollider` per mesh piece and a `CastleRoomModule` whose `SocketPoint`
  children are placed from the exact door geometry each room was built with
  (see "Blender → Unity axis conversion" above) — every entry in
  `CastleRoomRegistry.asset` now points at its real prefab instead of
  `{fileID: 0}`. Verified by actually running `RaidScene` and inspecting
  the generated castle: 35 rooms placed, doors between adjacent rooms line
  up, 0 console errors.
- `CastleGuard.prefab`'s placeholder capsule was replaced with the
  `VaultWarden` rig (static pose — no animation clips exist yet), keeping
  its existing `CapsuleCollider`/AI tuning untouched.

The other 4 enemy models (`SigilWisp`, `ArcRevenant`, `GildedColossus`,
`HexTurret`) have no corresponding gameplay slot yet — `GuardSpawner` only
supports one guard archetype — so they remain unwired pending a multi-enemy-type
feature.
