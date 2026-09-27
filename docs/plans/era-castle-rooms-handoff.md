# Era castle rooms: handoff

How to carry on the work in [`era-castle-rooms.md`](era-castle-rooms.md): fill every Age's castle
set to match the castle bench, **a reference sheet first, then the model built to it**, committing
and pushing after each room. Branch: `claude/castle-bench-rooms-mwucab`. Written 2026-09-24.

## Where it stands

| Age | Done | Left |
|---|---|---|
| Bronze Age | All 25 rooms and wall pieces, 4 door plugs, contact sheet, [`docs/art/rooms/BronzeAge.md`](../art/rooms/BronzeAge.md) | nothing |
| Late Medieval | All 25 rooms and wall pieces, 4 door plugs, contact sheet, [`docs/art/rooms/LateMedieval.md`](../art/rooms/LateMedieval.md) | nothing |
| Age of Powder | nothing | all 26 (see the plan's tables); a `_powder.py` sheet-helper module (copy `_late.py`'s shape), a Powder wall-sheet helper if its curtain pieces share a section (as `_late_wall.py` does) |

What each room contains (quadrant by quadrant, and its loot anchors) is in the plan's per-Age
tables. The art bible for each Age is commit `4de86a9`:
`git show 4de86a9:docs/art/late.md`, and its structure sheets under `docs/art/concept/<age>/`
(extract them with `git show 4de86a9:docs/art/concept/late/crooked-barbican.png > /tmp/x.png`).

## The per-room procedure

Run from the repo root. Every command here was run in this session's container (Blender 4.0.2
from apt, Node 22 with a global Playwright). A new container needs `apt-get update && apt-get
install -y blender` first.

1. **Read the plan's row** for the room and the art bible entry, if it has one.
2. **Write the sheet generator** `Tools/ArtBible/rooms/generators/<Age>/<Key>.py`, with `build()`
   returning a `room_sheet(...)`: a section E–W at y = 0 looking north (left, 1 m = 55 px) and a plan
   (right, 1 m = 30 px). It defines `ANCHORS = [(x, y, z), ...]` in L1…Ln order and passes them to
   `kit_loot`. Copy the nearest finished room in the same Age as a template. Helpers:
   `Tools/ArtBible/rooms/roomlib.py` (`kit_*`, `KE`/`KP`, `kerect`/`kprect`, `kit_gallery_section`,
   `kit_l_stair_plan`) and the Age's `_<age>.py` (`_bronze.py`, `_late.py`; Bronze walls use
   `_curtain.py`).
3. **Render and look at it:**
   `python3 Tools/ArtBible/rooms/make_rooms.py <Key>` then
   `NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs --rooms <Key>`, then open
   `docs/art/rooms/concept/<Age>/<Key>.png`. Fix overlapping labels. `sh.callouts(items, lx, ytop,
   ybot)` spreads the labels between `ytop` and `ybot`, and the materials legend clips long names.
4. **Write the spec** `docs/art/rooms/data/<Age>/<Key>.json` with: `key`, `name`, `age`, `zone`,
   `role`, `summary`, `description`, `build` (≥ 8 lines, exact dimensions), `sockets` (≥ 2; rooms
   must name the zone's archway), `loot_anchors` (rooms ≥ 2, in **the sheet's L1…Ln order**; wall
   pieces `[]`), `materials` (≥ 4, with pigment), `gameplay` (≥ 3), `budget`, `concept`, and
   `kit_changes` where the kit overrides the art bible.
5. **Write the builder** `build_<snake_key>(bm, uv)` in
   `Tools/AssetPipeline/castle_builders_<age>_<zone>.py` (zone is `curtain`, `bailey`, `ward`,
   `keep` or `crypt`). Start with `h, fz = room_shell(bm, uv, "<Zone>")`; the floor top is `fz`
   (0.30). Use `cb._box`, `cb._table` / `cb._chest` (these register a loot anchor), `cb._anchor`,
   `mk.add_cylinder` / `add_sphere` / `add_box` with `mk.paint`, and `era_kit` (`ek.prism`,
   `ek.wall_panel`, `ek.framed_panel`, `ek.jar`, `ek.wheel`). Shared furniture for an Age goes in
   `castle_builders_<age>.py`: Late has `candle_stand`, `wall_hearth`, `iron_chest`, `gun_loop`,
   and `book_press` in the ward file.
6. **Set the budget** in `Tools/AssetPipeline/asset_specs.py` (the room's `tri_budget`) at about
   1.5–2× the triangle count, and match it in the sheet header and the JSON.
7. **Run the cycle:** `Tools/ArtBible/rooms/room_cycle.sh <Key>`. It renders the sheet, builds and
   validates the model, renders the preview and overhead images, and checks the sheet against the
   model. It must end `OK: 1 room sheet(s) validate and match their models`.
8. **Look at the renders:** `Tools/AssetPipeline/previews/<Key>.png` and
   `previews/overhead/<Key>.png`, against the plan.
9. **Commit and push** (the user asked for this after every model):
   `git add -A Tools docs Assets && git commit -m "feat: <Key> room sheet and model ..." && git push -u origin claude/castle-bench-rooms-mwucab`.
   End the message with the two trailer lines used in the branch history (`Co-Authored-By` and
   `Claude-Session`).

When an Age's zone is finished: `python3 Tools/ArtBible/build_room_sheets.py --only <Age> --check
--models` (all sheets), then `python3 Tools/ArtBible/build_room_sheets.py --only <Age>`, which writes
`docs/art/rooms/<Age>.md`. Then `python3 Tools/AssetPipeline/make_contact_sheet.py` writes the
per-Age contact sheet. Update the M3 row in `docs/3-state/ProjectState.md`.

## Rules the validator enforces (learned the hard way)

- **No two vertices at one position.** `cb._box` grows every box by 0.01 m on each side, so parts
  meant to sit apart need gaps of **≥ 0.04 m** (slabs, books, chequer squares). Parts that share an
  outer corner (a post at a frame's corner, a back panel as wide as its sides) collide: make one
  0.02 m larger or smaller. Stacked cylinders of equal radius share rings, so vary the radius. To
  find the culprits, run
  `blender -b -P /tmp/claude-0/-home-user-PlunderSpell/d6c1f9a7-a61a-5e3a-8d13-3bd4da75a971/scratchpad/dupes.py -- <Key>`,
  a scratch script that prints the duplicate positions. If it is gone, rewrite it: import the
  module, build into a bmesh, and group vertices by rounded coordinates.
- **Closed meshes only** (no non-manifold edges): build solids, not open shells.
- **Room grammar** (enclosed rooms): keep the clear cross (|x| < 1.6 or |y| < 1.6, up to 2 m)
  empty; furniture goes in the quadrants (from 1.8 m); nothing floats (every part touches
  something grounded); the roof is open, so nothing hangs from above.
- **Loot anchors:** `cb._table`, `cb._chest`, `cb._shelf`, `cb._sarcophagus` and the Bronze
  `tripod`, `larnax` and `ingot_stack` register one each. Count them against the sheet.
- **Wall pieces** (`kind="wall"`) have the wall on local south (a corner piece: south + west), the
  gate in the south wall, no loot anchors, and stay inside ±6.05. Late wall pieces keep their outer
  face `FACE_INSET` (0.45 m) inside the cell edge.
- `build_assets.py --only <Key>` takes **one** key; a prefix builds every match.
- `from module import *` skips `_underscore` names, so shared helpers must be public.
- Sheet helper modules share one `sys.path` across Ages: give them unique names (no second
  `_curtain.py`).

## Final checks (once every Age is done)

- `Tools/AssetPipeline/run_pipeline.sh` runs clean, and the High Medieval set stays unchanged.
- `python3 Tools/ArtBible/build_room_sheets.py --check --models` reports all OK.
- Update the plan so its tables match what was built (for example, the Great Hall's hearth is on
  the west wall north of centre). Update `docs/3-state/ProjectState.md` and `docs/5-today/Today.md`. Push.
