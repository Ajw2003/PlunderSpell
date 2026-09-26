# EnemyForge

Procedural generation of the Plunderspell enemy roster: meshes, rigs, baked textures,
FBX/glTF exports and multi-angle review renders, all from Python.

Design rationale, invariants and the traps hit while building this are in
`docs/4-systems/enemy-asset-pipeline.md`. This file is just how to run it.

## Requirements

The Blender Python module, not a Blender install:

```bash
pip install bpy
```

`bpy` 5.0 requires **Python 3.11** specifically. Scripts run as `python3 script.py` —
there is no `blender` binary involved.

Rendering uses Cycles on CPU. EEVEE cannot run without a GPU and `libEGL.so.1`, and it
aborts the interpreter rather than raising, so do not switch the engine.

## Building the models

```bash
python3 Tools/EnemyForge/build_enemies.py
```

Writes `Assets/Models/Enemies/<Name>/` and `enemy_manifest.json`, printing a validation
report per enemy. Exits non-zero if any model fails validation.

Options:

| Flag | Default | Effect |
|---|---|---|
| `--out DIR` | `Assets/Models/Enemies` | output root |
| `--resolution N` | `1024` | baked texture size |
| `--only NAME ...` | all | build a subset, e.g. `--only VaultWarden` |

## Rendering the review sheets

```bash
python3 Tools/EnemyForge/render_enemies.py
```

Writes `Enemy_Renders_Review/`: five lit passes and a wireframe pass per enemy, a
contact sheet per enemy, and one roster line-up. Build the models first — the renderer
reads the exported `.blend` files.

| Flag | Default | Effect |
|---|---|---|
| `--resolution N` | `1100` | per-view pixel size |
| `--samples N` | `64` | Cycles samples |
| `--only NAME ...` | all | render a subset |
| `--skip-lineup` | off | skip the roster line-up |
| `--models DIR` / `--out DIR` | see above | override input/output roots |

A full pass at the defaults takes roughly 30 minutes on 4 CPU cores. Drop to
`--resolution 620 --samples 28` for a fast look while iterating.

## Layout

```
enemy_forge/
  parts.py        primitives, mirroring, the Part record
  archetypes.py   the roster: every enemy's parts, bones and budgets
  materials.py    surface families, baking, texture packing
  assemble.py     bevel, unwrap, rig, bake, export
  validate.py     geometry checks that gate the build
build_enemies.py  entry point: build + validate + export
render_enemies.py entry point: review renders
```

## Changing or adding an enemy

Edit `archetypes.py` — it is the only file that describes what an enemy *is*. An
archetype is a list of `Part` records plus a bone list, a declared height and a
triangle budget. Add a new one by writing a builder function and appending it to
`ROSTER`.

Then rebuild and re-render that one enemy:

```bash
python3 Tools/EnemyForge/build_enemies.py --only YourEnemy
python3 Tools/EnemyForge/render_enemies.py --only YourEnemy --skip-lineup
```

The validator will tell you if the result is not game-ready. It is deliberately
strict — a failure there is a bug in the archetype, not a reason to relax the check.
