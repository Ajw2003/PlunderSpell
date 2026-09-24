You are drawing and then modelling ONE zone of ONE Age of the Plunderspell castle: a reference sheet
for every piece first (at the depth of the art bible's structure sheets), then the Blender model,
built to that sheet. Your assignment (Age, zone, keys, and your builder file) is in the message
that sent you this brief. Work in the worktree you were given. The repo root there is where the
commands below run.

READ FIRST, all of them:
- `docs/art/rooms/README.md`: what a sheet is, the JSON shape, the kit's rules, the sheet
  conventions, and the commands. Obey every one.
- `docs/plans/era-castle-rooms.md`: the convention section, "Where the kit overrides the art
  bible", the per-Age palette table, and your Age's table of pieces (Role / Build / Loot anchors).
  The plan's line for each piece is its brief. You may improve on it, but keep its role and
  keep it inside the kit's rules.
- `docs/art/<age>.md` (`bronze`, `late` or `powder`) and `docs/art/BRIEF.md`: the Age's look,
  palette hexes, materials and voice. Look at a few of that Age's structure sheets:
  `docs/art/concept/<age>/*.png`, with the Read tool.
- The reference piece, which sets the quality bar and the format:
  `docs/art/rooms/data/BronzeAge/BronzeMegaron.json`,
  `Tools/ArtBible/rooms/generators/BronzeAge/BronzeMegaron.py`,
  `docs/art/rooms/concept/BronzeAge/BronzeMegaron.png` (Read it), and its model
  `build_bronze_megaron` in `Tools/AssetPipeline/castle_builders_bronze_keep.py`.
- `Tools/ArtBible/rooms/roomlib.py`, especially the "kit furniture" section at the bottom.
- `Tools/AssetPipeline/castle_builders.py` (the High Medieval set: helpers `_table`, `_chest`,
  `_shelf`, `_bench`, `_barrel`, `_banner`, `_brazier`, `_pillar`, `_sarcophagus`,
  `_stair_to_gallery`, `_stair_to_dais`, `_anchor`, and the High Medieval room each of your pieces
  stands in for), `Tools/AssetPipeline/era_kit.py` (prism, jar, wheel, cannon, tapered_column,
  cone_roof, wall_panel, framed_panel, balustrade, horns_of_consecration, disc, sphere),
  `Tools/AssetPipeline/room_kit.py`, and your Age's base file `Tools/AssetPipeline/castle_builders_<age>.py`
  (palette constants, ZONE_FLOOR, ZONE_TRIM, room_shell; the Late one also has FACE_INSET).
- `Tools/AssetPipeline/validate_in_blender.py` (`validate_object`, `validate_castle_layout`): the
  gate your models must pass.

FOR EACH OF YOUR KEYS, IN THIS ORDER:
1. **Spec.** Write `docs/art/rooms/data/<Age>/<Key>.json` per the README's shape. Give it
   art-bible depth: 8–12 build bullets with real dimensions in kit coordinates (x east, y north,
   from the cell centre; heights above ground, floor top at 0.30), sockets, 4–7 materials (the
   art-bible hex, then the kit pigment from the plan's palette table), 3–5 gameplay bullets, and
   the loot anchors (at least 2 for a room) with exact xyz.
2. **Sheet.** Write `Tools/ArtBible/rooms/generators/<Age>/<Key>.py` (`from roomlib import *`,
   `build()` returns `room_sheet(...)`). The section is on the left, with the 1.80 m human
   (`khuman`), the archway (`kit_arch_section`) and cut walls hatched. The plan is on the right:
   `kit_plan` for rooms; `kit_plan(..., shell=False)` for curtain-wall pieces, with the wall drawn
   on the south side. Then `kit_loot` with the same anchors, a section line A–A, socket labels,
   `kit_legend`, 4–7 callouts, and firelight glow from the room's light. Helpers shared by your
   generators go in `Tools/ArtBible/rooms/generators/<Age>/_<zone>.py` (files starting with `_`
   are skipped as sheets). Then:
   `python3 Tools/ArtBible/rooms/make_rooms.py <Key>` and
   `NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs --rooms <Key>`.
   **Look at the PNG with the Read tool.** Fix anything overlapping, cropped, off-scale, crude or
   unreadable, and re-render. It must read as concept art for a real place, like the art bible's
   structure sheets, not a diagram.
3. **Model.** Write `build_<snake_key>` in YOUR builder file only, built to the sheet: the same
   placements, dimensions and loot anchors. Go beyond boxes. Use the era_kit primitives and real
   silhouettes (profiles, tapers, wheels, jars, roofs, prisms), within the kit's rules: the clear
   cross, furniture in the quadrants, nothing floating, nothing past ±6.05 m, bottom-flush,
   quad-dominant ≥ 75 %, and no duplicate-position vertices (build through `cb._box`/
   `rk.paint_box` so flush parts overlap). Build:
   `PYTHONHASHSEED=0 blender -b -P Tools/AssetPipeline/build_assets.py -- --only <Key>`
   and fix until it prints PASS. Set your key's `tri_budget` in `Tools/AssetPipeline/asset_specs.py`
   to about 1.5–2× the reported count (edit only your own keys' lines). Aim for ≤ 2.5k tris per
   room and ≤ 2k per wall piece.
4. **Preview.** `PYTHONHASHSEED=0 blender -b -P Tools/AssetPipeline/render_previews_only.py -- <Key>`,
   then Read `Tools/AssetPipeline/previews/<Key>.png` and compare it with the sheet. Fix the model
   if it does not read as the sheet's room.
5. **Hold the model to the sheet.** `python3 Tools/ArtBible/build_room_sheets.py --only <Key> --models`
   must print OK. Put the tri count in the spec's `budget` ("≤ Nk tris (kit); M built").

When all your keys are done, run `python3 Tools/ArtBible/build_room_sheets.py --only <each key> --models`
once more for the whole set, and a last `build_assets.py --only <your keys, comma-separated>`.

FILES YOU MAY TOUCH, and nothing else:
- `docs/art/rooms/data/<Age>/<your keys>.json`, `docs/art/rooms/concept/<Age>/<your keys>.{svg,png}`
- `Tools/ArtBible/rooms/generators/<Age>/<your keys>.py` and `.../_<zone>.py`
- your builder file `Tools/AssetPipeline/castle_builders_<age>_<zone>.py`
- your keys' `tri_budget` in `Tools/AssetPipeline/asset_specs.py`
- generated by the commands above: `Assets/_Project/Art/Models/Castle/<Age>/<your keys>.fbx`,
  `Tools/AssetPipeline/previews/<your keys>.png`, `Tools/AssetPipeline/asset_manifest.json`,
  `Assets/_Project/Data/Castle/CastleLootAnchors.json`

Do not edit shared code (`roomlib.py`, `era_kit.py`, `castle_builders.py`, `room_kit.py`,
`castle_builders_<age>.py`, the build tools). If you need a helper, put it in your own builder
file or your `_<zone>.py`. If shared code has a bug, say so in your report rather than fixing it.
Do not run `make_contact_sheet.py`, `run_pipeline.sh` or `build_room_sheets.py` without `--only`.

COMMIT in your worktree when done (`git add` exactly the files above, one commit, message
`feat: <Age> <zone> castle pieces: sheets and models`, ending with the two attribution lines
you were given). Do not push.

FINAL REPORT (short): for each key, the tri count, the anchor count, and a line on what the room is.
Then anything you could not get right, and any shared-code problem you hit.
