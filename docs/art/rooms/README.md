# Castle room sheets

One reference sheet per castle room and curtain-wall piece of the Bronze Age, Late Medieval and
Age of Powder castle sets. There are 71 sheets: every module in
`Tools/AssetPipeline/asset_specs.ERA_CASTLE_SPECS` except the door plugs, which are plain slabs.
Each sheet is drawn **before** its model is built, and the model is built to it. The sheet is the
reference; the model is the kit's flat-colour version of it.

The plan behind the rooms (which room stands in for which, and why) is
[`docs/plans/era-castle-rooms.md`](../../plans/era-castle-rooms.md). The look of each Age comes
from the art bible ([`docs/art/BRIEF.md`](../BRIEF.md), `docs/art/<age>.md`). The sheet frame is
the art bible's structure sheet.

## What a sheet is

| Path | What |
|---|---|
| `docs/art/rooms/data/<Age>/<Key>.json` | the spec, detailed enough to build the room from with no questions |
| `Tools/ArtBible/rooms/generators/<Age>/<Key>.py` | the generator that draws the sheet (`build()` returns a `roomlib` Sheet) |
| `docs/art/rooms/concept/<Age>/<Key>.svg`, `.png` | the drawing and its 2400 × 1600 render |
| `docs/art/rooms/<Age>.md` | the handoff page for the whole Age, generated from the JSON |

The reference example for all of this is `BronzeMegaron`: its JSON, generator, sheet, and the model in
`Tools/AssetPipeline/castle_builders_bronze_keep.py`.

## Commands

Run from the repo root.

```bash
python3 Tools/ArtBible/rooms/make_rooms.py BronzeMegaron                      # generator -> SVG (Age name or key prefix; none = all)
NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs --rooms BronzeMegaron   # SVG -> PNG beside it
python3 Tools/ArtBible/build_room_sheets.py --only BronzeMegaron --check      # validate the spec and sheet
python3 Tools/ArtBible/build_room_sheets.py --only BronzeMegaron --models     # ...and hold the built model to it
python3 Tools/ArtBible/build_room_sheets.py                                   # validate everything, write docs/art/rooms/<Age>.md
```

`--only` takes an Age (`BronzeAge`), a whole key (matches only that key) or a key prefix.
`--models` compares the sheet's loot anchors with the ones the Blender builder registered
(`Assets/_Project/Data/Castle/CastleLootAnchors.json`). It checks the count, and that each anchor is
within 0.15 m. This is what shows the model was built to its sheet.

## JSON

```jsonc
{
  "key": "BronzeMegaron",            // the asset_specs key, and the RoomId
  "name": "The Megaron",
  "age": "BronzeAge",                 // HistoricalEra name
  "zone": "Keep",
  "role": "ThroneRoomKeep",           // the High Medieval room it stands in for (the plan's "Role")
  "summary": "One line.",
  "description": "3–5 sentences in the herald's voice (docs/plunderspell.md): what the room is, how it plays.",
  "source": "optional: the art bible structure it adapts",
  "kit_changes": ["optional: each place the kit overrides the art bible, and why"],
  "build": ["8–12 bullets: part, primitive shape, real dimensions and kit coordinates, material"],
  "sockets": ["Archway ×4 ... 2.60 × 3.31 m (Keep)", "windows, stairs, gun-loops, ..."],
  "loot_anchors": [ {"at": "throne seat", "xyz": [5.05, 3.7, 0.76]} ],   // >= 2 for a room
  "materials": [ {"name": "Painted lime plaster", "hex": "#C9A77A", "pigment": "bronze", "notes": "..."} ],
  "gameplay": ["3–5 bullets: what burns, breaks, hides, where loot and cover are"],
  "budget": "≤ 2.4k tris (kit)",
  "concept": "concept/BronzeAge/BronzeMegaron.svg"
}
```

- **Coordinates are the kit's.** x east, y north, in metres from the cell centre (−6…6). Heights
  are above the ground, so the floor slab's top is at 0.30. These are the numbers the builder
  uses. The sheet's section and plan are drawn in them too (`roomlib.KE`, `KP`).
- `materials.hex` is the art-pass colour, from the Age's art-bible palette. `materials.pigment` is
  the atlas pigment the kit model uses for it (`Tools/AssetPipeline/palette.py`), per the plan's
  per-Age palette table.
- `loot_anchors` must match what the model registers. Use the helpers that register anchors
  (`_table`, `_chest`, `_shelf`, `_sarcophagus`, or `cb._anchor(x, y, z)`) at exactly these points.

## The kit's rules, which every sheet shows and every model obeys

- 12 × 12 m cell, 0.30 m slab, 0.50 m walls. Clear heights: Crypt 3.00, OuterBailey 3.60,
  InnerWard 4.00, Keep 4.60, CurtainWall 5.20.
- **Rooms:** an archway 2.60 m wide centred on all four walls (height: OuterBailey 2.59, InnerWard
  2.88, Keep 3.31, Crypt 2.16), all open. Keep a clear cross, `|x| < 1.6` or `|y| < 1.6`, free up to
  2 m above the floor; only flat decor 0.12 m or less may sit in it. Furniture goes in the four
  corner quadrants, backed against the walls. Open roof: nothing hangs from a ceiling.
- **Curtain-wall pieces:** the wall on the local south side (corner: south + west), the gate in
  the south wall, the interior open to the north, and nothing past ±6.05 m.
- **Nothing floats:** every part rests on the ground or touches something that does.
- `kit_plan()` draws the shell, the four archways and the clear cross (dashed); `kit_loot()` marks
  the anchors L1…Ln. A room sheet always shows both.

## Sheet conventions

The art bible's structure-sheet conventions ([`docs/art/BRIEF.md`](../BRIEF.md),
`Tools/ArtBible/worker_brief.md`), in `roomlib`:

- viewBox 1200 × 800. Section on the left at 1 m = 55 px, with the height ladder and the 1.80 m
  human (`khuman`). Plan on the right at 1 m = 30 px, north up, with a section line A–A. A legend
  under the plan, and the palette strip at the foot.
- Title block `<AGE> · CASTLE ROOM · <ZONE>`, then the room's name. Top right: the cell, the clear
  height and the kit budget.
- It must look like concept art, not a diagram: gradients for form, soot and wear, firelight
  glow from the room's light source, 4–7 callouts with leader lines that do not cross the drawing
  needlessly, and labels that never overlap. Aim for 300–700 elements.
- Colours come only from the room's `materials` plus the frame colours. Orpiment only on things
  worth stealing, madder only for fire, blood and alarm, never lapis, and verdigris only for
  grab marks.
- No `<image>`, `<script>` or `foreignObject`; ids unique within the file.
