# Handoff — art bible enemies (ArtForge), 2026-09-24

Written because the session was about to run out of usage while four enemy workers were
mid-run. Everything below is committed on `claude/dreamy-curie-jnrkbu`. `Tools/autosave.sh`
was committing every 3 minutes, so at most about 3 minutes of worker output is missing.

## Where it stands

| Kind | Status |
|---|---|
| Art bible (spec, concept sheets, mood board) | Done, merged in Ajw2003/PlunderSpell#122 |
| 20 plunder items (ArtForge) | Done, merged in #122 |
| 12 structures | **Not ours.** Another agent owns them. Do not touch. |
| 16 enemies | **In progress** on this branch |

Enemies:
- **Done and checked:** `high/lantern-warden` and `high/alaunt-hound`. Both passed validation,
  and their review sheets were compared by eye with the concepts.
- **In progress when the session ended:** four workers, one per Age, were writing the other 14.
  Some review sheets already existed (palace-levy, wall-slinger, castle-crossbowman,
  sallet-halberdier, partisan-guard, musketeer). A sheet existing does **not** mean that enemy
  is finished or passes. Re-check everything.

| Age | Module | Enemies |
|---|---|---|
| bronze | `enemies_bronze.py` | palace-levy, wall-slinger, dendra-champion, flame-keeper |
| high | `enemies_high.py` | lantern-warden ✓, alaunt-hound ✓, castle-crossbowman, household-knight |
| late | `enemies_late.py` | sallet-halberdier, handgunner, gothic-knight, pavisier |
| powder | `enemies_powder.py` | partisan-guard, musketeer, cuirassier, petardier |

Modules live in `Tools/ArtForge/art_forge/blueprints/`.

## Setup in a fresh session

The container does not keep installs. Run from the repo root:

```bash
pip install bpy==5.0.1 pillow                     # Blender as a Python module, plus sheet compositing
python3 -c "import bpy; print(bpy.app.version_string)"   # expect: 5.0.1
git checkout claude/dreamy-curie-jnrkbu && git pull
```

`bpy` 5.0.1 needs Python 3.11. Cycles runs on CPU only, because EEVEE and Workbench abort the
process without a GPU.

## Step 1 — find out what the cut-off left behind

```bash
python3 Tools/ArtForge/build.py enemies           # builds every enemy that has a blueprint
```

Each enemy prints `[PASS]` or `[FAIL]` with the reason. A slug with no builder is simply not
built; `build.py` lists what exists. Then open each sheet in `docs/art/models/<age>/<slug>.png`
and compare it with `docs/art/concept/<age>/<slug>.png`.

## Step 2 — finish each unfinished enemy (docs/art/WORKFLOW.md, steps 5–7)

Protect the work first:

```bash
Tools/autosave.sh Tools/ArtForge Assets/Models/ArtBible/Enemies \
    Assets/Models/ArtBible/artforge_manifest.json docs/art/models
```

Then, for each enemy, loop until it clearly reads as its concept:

```bash
python3 Tools/ArtForge/build.py enemies --age <age> --only <slug>    # must print [PASS]
python3 Tools/ArtForge/render.py enemies --age <age> --only <slug>   # writes the review sheet
```

Each time, open the sheet and check:
- silhouette and key props against the concept;
- colours from the JSON only;
- height against `height_m`, and triangles against the budget (both on the caption);
- the **posed** views: the skin bends without tearing, and props follow the hands.

Fix the blueprint, then rebuild and re-render.

To hand this to agents again, give each one `Tools/ArtForge/enemy_worker_brief.md` plus its
Age, module and slug list. That is exactly how the current workers were briefed. Tell a worker
which enemies already pass, so it finishes the rest instead of redoing them. Run one worker per
Age, because they share a checkout.

## Step 3 — close out

```bash
python3 Tools/ArtForge/build.py enemies    # twice; expect "16 built, 0 crashed" / "All models passed validation."
python3 Tools/ArtForge/build.py items      # must still be "20 built … All models passed validation."
python3 Tools/ArtForge/render.py enemies   # all sheets; about 1 min each on 4 cores
```

Then:
1. Stop the auto-save (Ctrl-C, or stop its task).
2. Update `docs/Today.md` and `docs/ProjectState.md`. ProjectState still says the enemies are not
   modelled.
3. Update the "Not done yet" list in `Tools/ArtForge/README.md`.
4. Commit, push, and open a PR to `main`.

## Tools, in one place

| Tool | What it does | Doc |
|---|---|---|
| `Tools/ArtBible/build_art_bible.py` | Validates `docs/art/data/*.json`; writes the handoff sheets and mood board | `Tools/ArtBible/README.md` |
| `Tools/ArtBible/render_png.cjs` | Concept SVG → PNG | same |
| `Tools/ArtForge/build.py` | Blueprint → validated FBX/glTF/.blend with baked textures (items, enemies) | `Tools/ArtForge/README.md` |
| `Tools/ArtForge/render.py` | Review sheet: concept beside the model renders (plus posed views for enemies) | same |
| `Tools/ArtForge/art_forge/figures.py` | Shared parametric human (Unity-Humanoid bones) and quadruped | same, "figures" section |
| `Tools/autosave.sh` | Commit-and-push loop for long runs; `claude/*` branches only | its header |
| `docs/art/WORKFLOW.md` | The loop: plan → sheet → audit → fix → model → audit → fix → commit | — |

## Known gaps (not bugs, not started)

- **Enemy rigs:**
  - No animation clips.
  - No spring bones: the skirts, coats and jowls the JSON asks for.
  - Fists only, with no fingers.
- **Textures:**
  - Baked at 1024; the brief says 2048 for enemies (`--resolution 2048`).
  - No normal maps and no LODs, on items or enemies.
- **Review renders:** silver and steel render too warm. Judge them from the baked colour.
- **Colour choices:**
  - The Lantern Warden's lantern glow is stored darker (`#4A1E0C`), because the material
    multiplies emission by 9.
  - The warden uses one extra colour (`woad_mud`).
- **In Unity:** nothing has been imported into Unity. There are no `.meta` files, and the items
  aren't in any loot table.
