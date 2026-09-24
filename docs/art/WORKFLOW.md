# Art workflow — from brief to model

How the art bible's structures, enemies and plunder were produced, from one written brief to a
validated game model. It is the loop to repeat when adding a fifth Age, a new enemy or a new
treasure. The rules each step obeys are in [`BRIEF.md`](BRIEF.md). The tools are
[`Tools/ArtBible/`](../../Tools/ArtBible/README.md) (spec + concept sheets) and
[`Tools/ArtForge/`](../../Tools/ArtForge/README.md) (models).

Every audit step is a person or agent **opening the image and comparing it**. A passing
validator only says the numbers are legal; it does not say the thing looks right. Every
tweak/fix step is followed by another audit, never by the next stage.

```
plan → sheet → audit → fix → audit → model beside sheet → audit → fix → audit → commit
```

## 0. Protect the work first

Before any long run, start the auto-save on your own `claude/*` branch, watching only the
paths this run writes:

```bash
Tools/autosave.sh docs/art Tools/ArtBible                      # spec + concept stage
Tools/autosave.sh Tools/ArtForge Assets/Models/ArtBible/Items \
    Assets/Models/ArtBible/artforge_manifest.json docs/art/models  # model stage (items)
```

It commits and pushes every 3 minutes, so a session that is cut off loses at most 3 minutes of
work. Workers cut off by usage limits mid-run is not hypothetical: it happened to all four
concept workers at once, and nothing was lost. Keep the path list narrow, because another agent
may be writing elsewhere in the same checkout. See the header of `Tools/autosave.sh` for its
rules.

## 1. Plan

- Write or extend the brief (`docs/art/BRIEF.md`): counts, roles, scale limits from
  `docs/systems/scale.md`, pigment rules, triangle and texture budgets.
- Fix the roster before anyone draws: names, slugs, roles, zones, the item list. Give each
  worker one Age and a list of slugs, so no two workers touch the same file.
- Save the worker instructions in the repo (`Tools/ArtBible/worker_brief.md`,
  `Tools/ArtForge/item_worker_brief.md`), so a later session can resume a worker that was cut
  off.

## 2. Generate the spec and concept sheet

For each entry, a worker writes:

- the spec in `docs/art/data/<age>.json` (shape in `Tools/ArtBible/README.md`);
- the concept sheet `docs/art/concept/<age>/<slug>.svg`, from `Tools/ArtBible/sheet_template.svg`.
  The generator scripts live in `Tools/ArtBible/generators/`.

```bash
python3 Tools/ArtBible/build_art_bible.py --only <age>            # validate one Age
NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs <age>  # SVG → 2400×1600 PNG
```

## 3. Audit the sheet

Open every PNG. Check:

- it is on scale (the height ladder and the 1.80 m human);
- no label overlaps a drawing or another label;
- the colours come only from the entry's materials;
- the numbers on the sheet match the JSON (dimensions, worth, bulk, budget).

Diff the JSON against the sheet too: a worker who writes the spec after drawing can disagree
with the drawing (for example, the Great Hall tapestry against the Rolled Tapestry roll).

## 4. Tweak / fix, then audit again

Fix the generator, not the SVG by hand, so the fix survives a regenerate. The Powder label
overlaps, for example, were fixed once in `generators/powder/lib.py`. Re-render and re-open.
Repeat until clean, then build the handoff sheets and mood board:

```bash
python3 Tools/ArtBible/build_art_bible.py   # writes docs/art/<age>.md and the mood board
```

## 5. Generate the model beside the sheet

Write the entry's blueprint in `Tools/ArtForge/art_forge/blueprints/<kind>_<age>.py`, then:

```bash
python3 Tools/ArtForge/build.py <kind> --age <age> --only <slug>   # build + validate + export
python3 Tools/ArtForge/render.py <kind> --age <age> --only <slug>  # review sheet
```

The review sheet `docs/art/models/<age>/<slug>.png` puts the concept sheet on the left and the
model's renders on the right. The model side has three-quarter, front and side views, plus a
wireframe view (items) or a posed view (enemies).

## 6. Audit the model against the sheet

On the review sheet, compare:

- silhouette and proportions;
- colours and key details;
- the bounding box against the spec, and triangles against the budget (both on the caption);
- for enemies, that the posed view bends cleanly without tearing.

Silver, steel and mirrors currently render too warm in the review studio; judge those from the
baked colour, not the render.

## 7. Tweak / fix, then audit again

Change the blueprint, rebuild, re-render, re-open. At least one improvement pass per entry. A
real conflict between the spec's dimension line and its build notes goes in `bbox_overrides`,
with the reason. Don't shrink the model to make it pass.

## 8. Commit

Once every entry of the batch passes, rebuild the whole kind twice. The second run meets the
state the first left, and the manifest is rewritten consistently. Then commit, push and stop the
auto-save:

```bash
python3 Tools/ArtForge/build.py <kind>     # twice; expect "N built, 0 crashed" / "All models passed validation."
git add -- Tools/ArtForge Assets/Models/ArtBible docs && git commit && git push
```

Update the docs tier that changed (`docs/Today.md`, `docs/ProjectState.md`, the tool README's
"Not done yet") in the same commit.
