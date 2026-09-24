# ArtForge

Turns the art bible (`docs/art/data/<age>.json` plus the concept sheets in
`docs/art/concept/`) into game-ready models: mesh, one baked 1024² texture set, FBX
(Unity axes), glTF and `.blend`, gated by validation, plus a review sheet that sets
the render beside the concept art.

It is built **on top of** `Tools/EnemyForge`, not beside it. The part primitives, the
authoring-to-texture bake, the packed maps, the shipping material, the shade/unwrap
and the geometry validator are EnemyForge's own code, imported through `sys.path`.
EnemyForge's files are untouched. Read `docs/systems/enemy-asset-pipeline.md`,
especially **Traps**: every trap there applies here too.

**Status:** the framework handles all three kinds (`items`, `structures`,
`enemies`), but only two **items** exist and have been tested: `bronze/sealed-amphora`
and `high/gilded-altarpiece`. No structure or enemy blueprints exist yet. The rigged
(enemy) path is a hook that calls EnemyForge's rig code and has never been run
through ArtForge.

## Requirements

- `python3` 3.11 with `bpy` 5.0 (`pip install bpy`). No `blender` binary is used.
- Cycles on CPU only. EEVEE and Workbench abort the interpreter here.
- `render.py` needs Pillow for the sheet: `pip install pillow`.

## Commands

Run these from the repo root.

```bash
python3 Tools/ArtForge/build.py items                                   # every item with a blueprint
python3 Tools/ArtForge/build.py items --age bronze --only sealed-amphora
python3 Tools/ArtForge/build.py items --only sealed-amphora gilded-altarpiece --resolution 1024

python3 Tools/ArtForge/render.py items --only sealed-amphora gilded-altarpiece
python3 Tools/ArtForge/render.py items --age high --samples 64 --resolution 900
```

| Flag | build.py | render.py |
|---|---|---|
| `kind` (positional) | `items` \| `structures` \| `enemies` | same |
| `--age A` (repeatable) | all four ages | all four ages |
| `--only slug ...` | every slug with a blueprint | same |
| `--resolution N` | baked texture size, default 1024 | pixels per view, default 700 |
| `--samples N` | n/a | Cycles samples, default 32 (the wireframe pass uses half) |
| `--out DIR` | output root, default `Assets/Models/ArtBible` | n/a |

`build.py` prints a validation report per asset and exits non-zero if any asset
fails, crashes, or is named in `--only` without a blueprint. `render.py` exits
non-zero if a model has not been built.

Timings on 4 CPU cores: about 3 s to build an item (bake included), and about 40 s
to render its sheet at the defaults.

## Where outputs go

| Path | What |
|---|---|
| `Assets/Models/ArtBible/<Kind>/<Age>/<PascalName>/<PascalName>.fbx` | Unity import (−Y forward in Blender → +Z in Unity) |
| `…/<PascalName>/glTF/<PascalName>.gltf` (+ `.bin`, textures) | glTF, separate files |
| `…/<PascalName>/<PascalName>.blend` | source, with texture paths relative to it |
| `…/<PascalName>/Textures/` | `_BaseMap`, `_Roughness`, `_Metallic`, `_Emission`, packed `_MetallicGloss` (URP) and `_ORM` (glTF) |
| `Assets/Models/ArtBible/artforge_manifest.json` | one entry per asset; a `--only` run merges and keeps the others |
| `docs/art/models/<age>/<slug>.png` | review sheet: concept on the left, three-quarter / front / side / wireframe renders on the right, caption with tris/budget and bbox vs spec |

Example: `Assets/Models/ArtBible/Items/Bronze/SealedAmphora/SealedAmphora.fbx`.

## Layout

```
art_forge/
  __init__.py     puts Tools/EnemyForge on sys.path; KINDS, AGES, REPO_ROOT
  spec.py         JSON entry -> Entry (dims, budget, material families with PBR defaults)
  kit.py          build_bmesh + new part kinds (lathe, prism, tube) + authoring helpers
  materials.py    per-family authoring material, registered for EnemyForge's bake; pigment rules
  blueprint.py    the Blueprint dataclass
  assemble.py     build object, weighted bevel, EnemyForge shade/unwrap, bake, export
  validate.py     EnemyForge validate() via an adapter, plus art-bible size checks
  blueprints/
    __init__.py   registry: make(kind, age, slug), available(kind)
    items_bronze.py, items_high.py, ...   one module per (kind, age)
build.py          entry point: build + validate + export + manifest
render.py         entry point: review sheets
```

## Writing a blueprint

Each (kind, age) gets one module, `art_forge/blueprints/<kind>_<age>.py`, which
exposes `BLUEPRINTS = {slug: builder}`. The slug must exist in that age's JSON under
that kind, and the registry rejects any that don't. A builder takes the parsed
`spec.Entry` and returns a `Blueprint`, usually through `blueprints.blueprint(entry,
parts, ...)`, which fills in slug, name, age and kind.

What the `Entry` gives you:

| Field | Meaning |
|---|---|
| `entry.dims` | items: `(W, D, H)` metres. W = X, D = Y, H = Z. The model faces −Y. |
| `entry.height_m` | structures, enemies |
| `entry.tri_budget` | parsed from `"≤ 1.5k tris …"` → 1500 |
| `entry.families` | `{slug: spec}` from the JSON `materials`. The slug is the lower-cased name without any parenthetical: `"Gilt (orpiment)"` → `gilt`, `"Buff terracotta"` → `buff_terracotta`. |
| `entry.build` | the JSON build bullets, for reference |

A family spec holds `base` (the JSON hex), `rough`, `metal`, `emit` and `grain`. The
PBR values are derived from keywords in the name (gold, gilt, silver, bronze, iron
and similar are metallic; cloth ~0.9 rough, stone ~0.9, wood ~0.7, glaze ~0.25, and
so on), and any `roughness 0.x` / `metallic 1.0` written in the JSON notes wins.
`python3 -c` over `spec.entry(...)` prints them if you want to check.

`Blueprint` fields:

| Field | Default | Use |
|---|---|---|
| `parts` | required | list of `Part` |
| `family_overrides` | `{}` | `{"gilt": {"rough": 0.22, "wear_to": "#7E2A26"}}`. Keys are listed in `materials.py`. |
| `extra_families` | `{}` | a material the build bullets need but the JSON doesn't list (the altarpiece's iron hinges). Reported as a warning in every build. |
| `bevel` | 0.004 | edge bevel width in metres (0 to disable) |
| `tri_budget` | JSON | override only with a reason |
| `grounded` | True | lowest point must be within 2 cm of z = 0 |
| `bbox_overrides` | `{}` | `{"X": (0.52, "why")}`, for when the JSON's own build bullets contradict its dimension line. The reason is printed on every build and on the sheet. |
| `gold_reason` | None | a structure or enemy that carries orpiment must say why, unless the JSON already marks it plunder/loot/stealable |
| `bones` | None | enemies: EnemyForge bone dicts. Setting this selects the rigged path. |
| `notes` | `[]` | written to the manifest (what was left out, and why) |

### Worked example

```python
# art_forge/blueprints/items_high.py
from ..kit import Part
from . import blueprint

def silver_ewer(entry):
    W, D, H = entry.dims                                   # 0.22 × 0.16 × 0.34
    body = Part("lathe", (0, 0, 0), (1, 1, 1), mat="silver", segments=16, extras={
        "profile": [(0, 0), (0.045, 0.004), (0.05, 0.02), (0.035, 0.03),     # foot
                    (0.07, 0.10), (0.075, 0.15), (0.074, 0.17),                # belly; band edges
                    (0.045, 0.24), (0.03, 0.29),                               # neck
                    (0.042, H), (0.0, H)],                                     # lip, pole
        "paint": [{"mat": "parcel_gilt", "min": (-1, -1, 0.15), "max": (1, 1, 0.17)}],
    })
    handle = Part("tube", (0, 0, 0), (1, 1, 1), mat="silver", segments=6, extras={
        "path": [(0.03, 0, 0.27), (0.12, 0, 0.28), (0.13, 0, 0.18), (0.06, 0, 0.12)],
        "section": (0.008, 0.005), "up": (0, 1, 0), "smooth": True, "bevel": False})
    return blueprint(entry, [body, handle], bevel=0.0,
                     family_overrides={"silver": {"wear_to": "#4A4A44"}})

BLUEPRINTS = {"silver-ewer": silver_ewer}
```

Then build and render it:

```bash
python3 Tools/ArtForge/build.py items --only silver-ewer
python3 Tools/ArtForge/render.py items --only silver-ewer
```

Open `docs/art/models/high/silver-ewer.png` and compare it to the concept.
**Iterate until it reads like the concept.** Passing validation is necessary but
proves nothing about the look.

For complete working examples, see `items_bronze.py` (lathe body, paint bands, tube
handles and cords, all following the profile) and `items_high.py` (flat work: the
`upright()` / `disc()` / `figure_outline()` helpers for panels, reliefs and figures in
the XZ plane).

## Part kinds

All of EnemyForge's kinds work unchanged: `box`, `cyl`, `cone`, `sphere`, `ico`,
`torus`, `shard`. `size` is the full extent, `rot` is XYZ degrees, and `mirror=True`
reflects across YZ. The new kinds are:

| Kind | extras | Notes |
|---|---|---|
| `lathe` | `profile=[(r, z), ...]` in metres, bottom to top | Revolved about local Z into a closed solid. An end with r = 0 becomes a pole. An end with r > 0 gets a flat cap. Interior points need r > 0, and the profile may turn back down (a cup's inside). `segments` = number of sides. Use `size=(1,1,1)`. |
| `prism` | `outline=[(x, y), ...]` in metres | Extruded along local Z by `size[2]` (centred). Any winding, and concave outlines work because the caps are triangulated by polygon fill. `rot=(90,0,0)` stands it up in world XZ, facing −Y. |
| `tube` | `path=[(x,y,z), ...]`, `section=(rn, rb)`, `up=`, `closed=` | Swept elliptical section with parallel-transport frames and mitred corners. Open ends are capped. `closed=True` makes a ring. `segments` = number of sides. |

Options that work on any part:

- `extras["paint"] = [{"mat", "min", "max"}]` restamps faces whose centroid falls in a
  local-space box with another family. Painted bands stay part of the same shell (no
  floating decals). Put profile points on the band edges so the edge is crisp.
- `extras["smooth"] = True` shades the whole part smooth. EnemyForge's auto-smooth
  angle is 34°, so a tube with fewer than 11 sides renders faceted without it.
- `extras["bevel"] = False` keeps the asset bevel off this part.

Helpers in `kit.py`: `spline(points, n)` (Catmull-Rom, for tube paths and profiles),
`arc_path`, `rounded_rect`, `gable_outline`, `ring_of`.

## Validation

`validate.py` runs EnemyForge's `validate()` through an adapter, which checks:
closed, manifold, consistent winding, outward shells, no degenerate faces, UVs
present and inside 0..1, one material slot, identity transforms, grounded, and the
triangle budget. On the static path it removes exactly EnemyForge's two rig messages
("not assigned to any bone", "no bound armature modifier") and asserts the mesh
really has no vertex groups or armature. It then adds:

- **items**: each bbox axis within ±10 % of the JSON dimensions (W = X, D = Y,
  H = Z), unless the blueprint sets `bbox_overrides`.
- **structures**: footprint within the 12 × 12 m cell (|x|, |y| ≤ 6 m). Warns if the
  model is more than 10 % taller than `height_m`.
- **enemies**: height within ±5 % of `height_m`.

Pigment rules are enforced before building (`materials.discipline_violations`):
orpiment-gold families only on items, unless the material is marked stealable or the
blueprint gives a `gold_reason`, and no verdigris or lapis on enemies. Both are
matched by name and by hex distance.

## Traps

These cost time while building the two samples, and they will cost you too.

- **The bevel triples the triangle count on small round things.** EnemyForge bevels
  every edge sharper than 32°. That catches every edge of a 4- or 6-sided tube and
  every rim of a coin-sized disc. The altarpiece went from 1956 triangles to 5384
  (budget 5000). ArtForge now runs its own weight-limited bevel (same settings as
  EnemyForge's, but parts can opt out with `extras["bevel"] = False`), then calls
  EnemyForge's `finish_geometry` with bevel 0 for shading and unwrap. With opt-outs
  on the discs, tubes and hidden relief, the same model is 2568 triangles.
- **Low-sided tubes render faceted.** The 34° auto-smooth marks every edge of a
  6-sided strap as sharp. Use `extras["smooth"] = True`. It is stored as an integer
  face layer, which survives the bevel just as `material_index` does.
- **EnemyForge's torus is built inside-out.** The whole-mesh `recalc_face_normals` in
  `assemble.build_object` fixes it. Don't remove that pass. The new kinds orient
  themselves by signed volume, and a part with a negative `size` component is
  flipped back.
- **Metals render brown in a black world.** Metallic gilt is almost all reflection,
  so the review studio gives reflection rays a warm environment while camera rays
  see bone-black (a Light Path "Is Camera Ray" mix). Without that, orpiment reads as
  dark lacquer and the sheet lies about the model.
- **The light tree slows this scene down.** With three area lights and a world,
  turning `use_light_tree` off takes a 700 px view from 15.7 s to 10.2 s.
- **The JSON dimension line and the build bullets can disagree.** The amphora's
  0.34 m W is the body. Its handles arch 0.08 m beyond that on each side, so the
  model is 0.52 m across. Record that with `bbox_overrides` and a reason. Don't
  shrink the handles to pass.
- **`import bmesh` fails unless `bpy` has been imported first.** `kit.py` imports
  `bpy` for that reason.
- **Paths in `.blend` files.** The bake loads textures by absolute path.
  `assemble.save_blend` rewrites them relative to the `.blend` as the last step,
  after FBX/glTF export, so the file opens from any checkout. `render.py` opens the
  `.blend` itself (not an append) so those relative paths resolve.
- **Whole-mesh normal recalculation can turn a thin, flattened ring inside out.**
  The Venetian mirror's cushion frame (a closed-ring `tube`, 14 sides, 0.018 m
  half-depth) failed with "1 shells have inverted normals" after
  `recalc_face_normals` in `assemble.build_object`. 12 sides at 0.020 m fixed it.
  If a thin ring fails this way, thicken it or drop sides before touching the pass.
- **Keyword PBR guesses can be wrong.** `spec.py` reads "copper" as metallic, but the
  cabinet's *painted* copper panel is paint. Override the family (`metal: 0`) in the
  blueprint when the name describes a substrate, not a surface.
- **Silver and polished steel still render warm or dark on the review sheets.** The
  warm reflection environment that fixed gilt makes silver read bronze-brown and a
  mirror read taupe, even though the baked base colours hold the JSON hex. Workers
  raised roughness slightly on a few families (ewer silver 0.3, armour steel 0.3,
  badge gold 0.32) to lighten them. A neutral-reflection pass in `render.py` would
  be the real fix.
- **The glTF exporter warns** "More than one shader node tex image used for a
  texture". That comes from EnemyForge's shipping material (ORM feeds both roughness
  and metallic). It's harmless, and it happens on the EnemyForge enemies too.

## Not done yet

- No normal map is baked. The JSON asks for wheel ridges and punch-work as normal
  detail, but EnemyForge's bake makes BaseMap / Roughness / Metallic / Emission only.
  The amphora's ridges are faint albedo bands (`ridges` in the family spec).
- No painted-panel atlas. The altarpiece's figures are shaped relief in flat family
  colours. Faces have no features.
- No LOD1/LOD2 (50 % / 20 %), even though the brief asks for them.
- The rigged enemy path (`assemble.build_rigged`) has not been run. Structures and
  enemies have no blueprints: only the 20 items are built.
- Two part kinds live in blueprint modules rather than `kit.py`, because the item
  workers were told not to edit the shared framework: `bronze_loft` (a lathe whose
  rings need not be circles; `items_bronze.py`) and `powder_whorl` (a sweep whose
  section grows along its path; `items_powder.py`). Both register themselves into
  `kit._NEW_BUILDERS` on import. They belong in `kit.py`.
- No damage or alternate-state meshes (crumpled mask, dented tripod, open cabinet,
  lid-off tureen, ewer dent blend shapes), even where the JSON describes them.
