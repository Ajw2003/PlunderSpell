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
prompt → proposal → plan → sheet → audit → fix → audit → model beside sheet → audit → fix → audit
       → rig + pose → audit → clips → audit → engine → audit in engine → docs agree → commit
```

The same loop, made portable for any project and enforced by a Stop-hook gate, is the
`art-pipeline` plugin in Ajw2003/AjsClaudeCodeTools (issue #161 there).

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

## 1a. From a prompt to an approved design

When the request is a design the owner has to choose (the player wizard, #335), draw the options
before anything is modelled:

- Concept plates drawn by code, one per question
  (`claude/wizard-character-plan:docs/art/concept/wizard/source/plates.py`, not merged yet).
- A proposal page that sets them side by side
  (`claude/wizard-character-plan:docs/art/concept/wizard/proposal.html`, built by its
  `source/build.py`), published for the owner to pick from.
- The choice goes into a plan (`docs/plans/<topic>.md`) and a dated `docs/6-decisions/Decisions.md`
  entry, with what was rejected so nobody proposes it again.

## 1. Plan

- Write or extend the brief (`docs/art/BRIEF.md`): counts, roles, scale limits from
  `docs/4-systems/scale.md`, pigment rules, triangle and texture budgets.
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

## 7a. Rig and pose (enemies, the player)

A figure blueprint (`figures.Human`, `figures.Quadruped`) gives Unity-Humanoid bone names, heat
weights with per-part rules, and a test pose. The review sheet's POSED views are the audit: the skin
bends without tearing, props follow the hands, a lifted knee does not break through a robe.

## 7b. Clips (AnimForge)

In-place clips keyed by code on the shared reference skeleton, exported per family as
animation-only FBX files (`Tools/ArtForge/anim.py` for the enemies, `Tools/ArtForge/anim_player.py`
for the wizard). Each clip gets a review sheet in `docs/art/anim/` (8 stills on the real model, a
foot-contact trace for locomotion) and an MP4. Audit: feet do not slide, nothing sinks below the
floor, the pose reads as the action. The clip list, lengths, loops and events are
`Tools/ArtForge/anim_spec.json`; the manifests the exporter writes are what Unity's importer and
tests read.

## 7c. Into the engine

Import rules live in code (`Assets/_Project/Scripts/Editor/ArtBibleModelImporter.cs`), never in
the Inspector. Prefabs, rosters and animator controllers are generated by Editor menus
(Tools ▸ Plunderspell ▸ Forge Art Bible Enemies + Roster; Plunderspell ▸ Wizard ▸ Install On
Player Prefabs). Audit in the engine: a screenshot or capture, and the import validator. A cloud session
without Unity says so and hands over the menu item, marked untested.

## 8. Commit

Once every entry of the batch passes, rebuild the whole kind twice. The second run meets the
state the first left, and the manifest is rewritten consistently. Then commit, push and stop the
auto-save:

```bash
python3 Tools/ArtForge/build.py <kind>     # twice; expect "N built, 0 crashed" / "All models passed validation."
git add -- Tools/ArtForge Assets/Models/ArtBible docs && git commit && git push
```

Update the docs tier that changed (`docs/5-today/Today.md`, `docs/3-state/ProjectState.md`, the tool README's
status line, and `docs/4-systems/scale.md` when a height or the roster changes) in the same commit,
then prove the docs still agree with the data and the code:

```bash
python3 Tools/docs/check_art_docs.py      # expect "art doc disagreements: 0"
python3 Tools/docs/check_doc_links.py     # expect "broken relative links: 0"
```

CI runs both on every change to `docs/`, `Tools/` or the Editor scripts
(`.github/workflows/docs.yml`). `scale.md` once kept the retired enemy roster for weeks after the
art-bible set replaced it; that is the drift these checks exist to catch.
