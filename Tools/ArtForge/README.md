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

**Status:** all 20 **items** are built. The rigged **enemy** path works end to end
and has two samples that set the bar for the other 14: `high/lantern-warden` (a
humanoid on `figures.Human`) and `high/alaunt-hound` (a quadruped on
`figures.Quadruped`). No structure blueprints exist in this module yet (another
agent owns structures).

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

python3 Tools/ArtForge/build.py enemies                                 # every enemy with a blueprint
python3 Tools/ArtForge/build.py enemies --age high --only lantern-warden
python3 Tools/ArtForge/render.py enemies --only lantern-warden alaunt-hound
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
to render its sheet at the defaults. An enemy builds in about 3 s (heat weighting
included) and its six-view sheet renders in about 58 s.

## Where outputs go

| Path | What |
|---|---|
| `Assets/Models/ArtBible/<Kind>/<Age>/<PascalName>/<PascalName>.fbx` | Unity import (−Y forward in Blender → +Z in Unity) |
| `…/<PascalName>/glTF/<PascalName>.gltf` (+ `.bin`, textures) | glTF, separate files |
| `…/<PascalName>/<PascalName>.blend` | source, with texture paths relative to it |
| `…/<PascalName>/Textures/` | `_BaseMap`, `_Roughness`, `_Metallic`, `_Emission`, packed `_MetallicGloss` (URP) and `_ORM` (glTF) |
| `Assets/Models/ArtBible/artforge_manifest.json` | one entry per asset; a `--only` run merges and keeps the others |
| `docs/art/models/<age>/<slug>.png` | review sheet: concept on the left, three-quarter / front / side / wireframe renders on the right, caption with tris/budget and bbox vs spec. Enemies get six views (see "Enemies") |

Example: `Assets/Models/ArtBible/Items/Bronze/SealedAmphora/SealedAmphora.fbx`.
Enemies: `Assets/Models/ArtBible/Enemies/High/LanternWarden/LanternWarden.fbx`, which
holds the armature and the skinned mesh (EnemyForge's exporter: `-Z` forward, `Y` up,
no leaf bones), and a glTF with one skin.

## Layout

```
art_forge/
  __init__.py     puts Tools/EnemyForge on sys.path; KINDS, AGES, REPO_ROOT
  spec.py         JSON entry -> Entry (dims, budget, material families with PBR defaults)
  kit.py          build_bmesh + new part kinds (lathe, prism, tube, loft, sweep) + helpers
  figures.py      rigged figures for enemies: Human (Unity Humanoid bones), Quadruped
  rig.py          per-part skinning rules after heat weighting; review-pose application
  materials.py    per-family authoring material, registered for EnemyForge's bake; pigment rules
  blueprint.py    the Blueprint dataclass
  assemble.py     build object, weighted bevel, EnemyForge shade/unwrap, bake, export
  validate.py     EnemyForge validate() via an adapter, plus art-bible size and rig checks
  blueprints/
    __init__.py   registry: make(kind, age, slug), available(kind)
    items_bronze.py, items_high.py, ...   one module per (kind, age)
    enemies_high.py   the two enemy samples: lantern-warden, alaunt-hound
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
| `bones` | None | enemies: EnemyForge bone dicts. Setting this selects the rigged path. Pass `**figure.rig()` rather than writing them. |
| `forward_bones` | `[]` | enemies: bones whose head→tail must point to −Y (feet, a beast's head); how validation proves the model faces the front |
| `pose` | `{}` | enemies: the review pose for the sheet's POSED views, `{bone: (rx, ry, rz)}` world-space degrees (see `rig.apply_pose`) |
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
| `loft` | `rings=[[(x,y,z), ...], ...]`, `closed=` | Skins a stack of rings (any shape, same point count; a 1-point ring is a pole, ends only) into one solid. Torsos, heads, shoes, a dog's body, the hound's C-section coat. Build rings with `kit.section()` or by hand. Promoted from `items_bronze.py`'s `bronze_loft`, which is now an alias of it. |
| `sweep` | `path`, `sections=[(rn, rb), ...]` (one per point), `up=`, `power=`, `offsets=` | A tube whose section changes along the path: limbs, tails, necks, fists. A `(0, 0)` section at either end makes a pointed pole. `power` 2 = ellipse, 4 = rounded box. `offsets=[(dn, db)]` shift ring centres (a calf bulging back). Not mitred: sample the path smoothly. |

Options that work on any part:

- `extras["paint"] = [{"mat", "min", "max"}]` restamps faces whose centroid falls in a
  local-space box with another family. Painted bands stay part of the same shell (no
  floating decals). Put profile points on the band edges so the edge is crisp.
- `extras["smooth"] = True` shades the whole part smooth. EnemyForge's auto-smooth
  angle is 34°, so a tube with fewer than 11 sides renders faceted without it.
- `extras["bevel"] = False` keeps the asset bevel off this part.

- `extras["rigid"]`, `extras["prop"]`, `extras["bones"]`, `extras["skirt"]`: skinning
  rules on the rigged path, see "Skinning" below.

Helpers in `kit.py`: `spline(points, n)` (Catmull-Rom, for tube paths and profiles),
`arc_path`, `rounded_rect`, `gable_outline`, `ring_of`, `section(centre, half_u,
half_v, n, u_axis, v_axis, power, start_deg, bulge)` (one superellipse loft ring).

`items_powder.py` still registers its own `powder_whorl` kind (a sweep that opens
into a hollow mouth). It is too specific to promote; use `sweep` for anything new.

## Enemies

An enemy blueprint builds a **figure** (`art_forge/figures.py`), asks it for body
parts in the JSON's materials, adds clothing, armour and props against the figure's
landmarks, and hands `**figure.rig()` to `blueprint(...)`. `rig()` supplies
`bones`, `forward_bones` and the review `pose`. Everything stands on z = 0, centred,
facing −Y. `.L` is the figure's own left, which is +X (the viewer's right in a front
view). Props go in the right hand (`.R`, −X) unless the JSON says otherwise.

### `figures.Human`

```python
Human(height=1.76, bulk=1.0, shoulders=None, stoop=0.0,
      arm_l=ArmPose(), arm_r=ArmPose(), stance=2.5, segments=12)
ArmPose(spread=9.0, swing=3.0, elbow=12.0)    # degrees: out, forward, forearm bend
```

`height` is the stature to the crown of the skull, **not** the hat. Eyes land at
0.936 × height, so a JSON "eyes 1.65 m" means `height=1.763`. Proportions are
EnemyForge's (`ANKLE, KNEE, HIP, WAIST, CHEST, SHOULDER, NECK` fractions), so ArtForge
and EnemyForge guards are the same species. `bulk` scales girth (torso and limbs),
`shoulders` is the outer deltoid width in metres, `stoop` leans everything above the
waist forward. Arms are posed in the bind pose, so a hand can hold its prop where
the concept shows it; keep `spread` 6-16° so sleeves stay clear of the torso.

Body parts (each already bound to its bones, smooth-shaded, bevel off):

| Method | What |
|---|---|
| `body(skin, torso, sleeves, legs, feet, head=None, hood=False, **torso_kwargs)` | everything below in one call |
| `torso_part(mat, pad, hem, hem_flare, collar, quilt, paint, segments, chest)` | one loft, crotch (or a skirt `hem`, metres) to collar. `pad` = padding in metres; `quilt` 0-0.1 pinches every other ring point (vertical quilting); a hem adds the skirt rule below |
| `arm_part(side, mat, pad, quilt_rings, quilt, segments, paint)` | shoulder to wrist as one sweep (no elbow seam); `quilt_rings` = ringed quilting |
| `hand_part(side, mat)` | closed fist + thumb, on `Hand.<side>` |
| `leg_part(side, mat, pad, segments, paint)` | hip to ankle as one sweep, calf bulging back |
| `foot_part(side, mat, length, segments, point)` | a shoe: flat sole on z = 0, `point` 0-1 sharpens the toe |
| `head_part(mat, face, hood, segments, features)` | neck + skull loft and a nose. `hood=True`: a coif draped onto the shoulders in `mat`, the face painted `face`. `features`: a dark family for eyes |
| `band(z, mat, height, pad, torso_pad, bone)` | a belt/girdle hugging the torso at height z |

Landmarks for layers and props: `joint("wrist.R" | "elbow.L" | "knee.R" | "ankle.L" |
"shoulder.R" | "hip.L" | "ball.R" | "crown" | "chin" | "eyes" | "neck" | "belt")`,
`grip(side)` (centre of the fist), `surface(z, angle_deg, pad)` (a point on the torso:
0° = left, −90° = front, 90° = back, 180° = right), `torso_dims(z)`, `bone(name)`,
`along(bone, t)`, `lean(point)`, and the attributes `h, belt_z, knee_z, hip_z,
waist_z, chest_z, shoulder_z, neck_z, chin_z, eye_z, crotch_z, leg_x`.

Extra bones: `add_bone(name, head, tail, parent)` (hats, cloth springs, a swing chain)
and `prop_bone(name, side, head, tail)` (parented to `Hand.<side>`). Bind props with
`extras={"prop": True}`: rigid, and excluded from the height check.

### Bone naming (Human)

Unity Humanoid (Mecanim) names with Blender side suffixes, so EnemyForge's mirror
and the FBX exporter both work:

```
Root > Hips > Spine > Chest > Neck > Head
Chest > Shoulder.L > UpperArm.L > LowerArm.L > Hand.L        (and .R)
Hips  > UpperLeg.L > LowerLeg.L > Foot.L                      (and .R)
```

`figures.UNITY_HUMANOID` maps Unity's `HumanBodyBones` names (`LeftUpperArm`, ...) to
these, for building the Avatar explicitly if the importer's auto-mapping misreads
`.L`/`.R`. The bind pose is an A-pose with the arms where the blueprint put them;
use "Enforce T-Pose" in Unity's Avatar configuration. The warden adds `Hat` (child of
Head), `Glaive` (prop on Hand.R) and `LanternRing > LanternBody` (a swing chain on
Hand.L): 24 bones, 23 skinned (`Root` carries no vertices).

### `figures.Quadruped`

```python
Quadruped(withers=0.72, length=1.42, chest_width=0.30, segments=10)
```

A reference hound scaled per axis (Z by withers, Y by nose-to-tail, X by chest).
`body(coat, mask, nose=None, teeth=None)` gives the barrel (loft), neck, tail, skull
and muzzle (front painted `mask`), lower jaw, rose ears, eyes, nose, fangs, legs and
paws. Pieces: `body_part`, `neck_part`, `head_parts`, `foreleg_part`, `hindleg_part`,
`paw_part`, `tail_part`. For layers: `body_at(y)` (the barrel's centre z, half-width,
half-height, keel at reference y), `body_ring(y, cz, hw, hh, keel, n, pad, a0, a1,
closed)` (points on the barrel surface, or an arc of it), `neck_frame(t)` (centre,
tangent, radius along the neck, for collars) and `p(x, y, z)` (a reference point,
scaled). Bones (Unity Generic):

```
Root > Pelvis > Spine1 > Spine2 > Spine3 > Chest > Neck1 > Neck2 > Head > Jaw, Ear.L/R
Pelvis > Tail1 > ... > Tail5
Chest  > Scapula.L > Humerus.L > Radius.L > Carpus.L > ForePaw.L   (and .R)
Pelvis > Femur.L > Tibia.L > Hock.L > HindPaw.L                    (and .R)
```

The hound adds `CollarRing` (a spring under Neck1): 36 bones, 35 skinned.

### Skinning

`assemble.build_rigged` runs EnemyForge's `build_armature` and
`apply_smooth_weights` unchanged (heat weighting, smoothing, 4-influence limit,
normalise, rigid fallback), then `rig.apply_bind_rules`, which enforces what each
Part says. The part a vertex came from is read from `kit.PART_LAYER`, an integer face
layer that survives the bevel and unwrap.

| Part extras | Effect |
|---|---|
| `"rigid": True` | every vertex bound 1.0 to `part.bone` (hats, buckles, fists' thumbs, paws) |
| `"prop": True` | rigid, and left out of the enemy height check (weapons, lanterns) |
| `"bones": [...]` | the only bones the part may be weighted to; `.L` swaps to `.R` on mirrored copies |
| `"skirt": {"top", "bottom", "strength", "split", "left", "right"}` | below `top`, hand a growing share (up to `strength` at `bottom`) to the thighs, split across the centre line. `torso_part` sets it; without it a lifted knee goes straight through a coat |
| none | the part's bone, its parent and its children (never Root) |

A vertex whose heat weights are all on disallowed bones (a layer hidden inside another
island) is blended between the two nearest allowed bones by distance and counted in
the warning "N vertices got no allowed heat weight"; that is expected on the hound's
coat and harness, not a failure.

### Review sheet

`render.py enemies` renders six views: three-quarter, front (beside a faint 1.80 m
reference `figures.Human`), side, **POSED** and **POSED FRONT** (the rig's review
pose, to show the skin deforming without tearing), and wireframe. The default poses
are `figures.HUMAN_TEST_POSE` (left arm raised forward to face height, head turned,
right knee lifted and bent, a slight spine twist) and `figures.QUADRUPED_TEST_POSE`
(head and neck turned, jaw open, right foreleg lifted and folded, tail raised). Pass
your own to `fig.rig(pose)` when a prop needs it: the warden adds `LanternRing:
(105, 0, 0)` so the lantern hangs from the raised fist instead of pointing along the
forearm. The caption lists body height vs spec, bones and the posed bones.

### Worked example

```python
# art_forge/blueprints/enemies_late.py (sketch)
from mathutils import Vector
from ..figures import ArmPose, Human
from ..kit import Part
from . import blueprint

def sallet_halberdier(entry):
    fig = Human(height=1.76, bulk=1.05, shoulders=0.47,
                arm_r=ArmPose(spread=14, swing=4, elbow=80))     # halberd hand
    parts = fig.body(skin="skin", torso="brigandine", sleeves="linen",
                     legs="hose", feet="leather", hem=0.70, pad=0.015)
    parts.append(fig.band(fig.belt_z, "leather", torso_pad=0.015))

    # A sallet on its own bone so it can come off.
    base = fig.lean((0, 0.004 * fig.h, 0.95 * fig.h))
    fig.add_bone("Helm", base, base + Vector((0, 0, 0.15)), "Head")
    parts.append(Part("lathe", tuple(base), (1, 1, 1), mat="steel", bone="Helm",
                      extras={"profile": [...], "rigid": True}))

    # The halberd: a prop bone on the right hand, upright through the fist.
    g = fig.grip("R")
    fig.prop_bone("Halberd", "R", head=g, tail=(g.x, g.y, 2.3))
    parts.append(Part("cyl", (g.x, g.y, 1.1), (0.03, 0.03, 2.2), mat="ash",
                      bone="Halberd", extras={"prop": True}))
    return blueprint(entry, parts, **fig.rig())

BLUEPRINTS = {"sallet-halberdier": sallet_halberdier}
```

Then `build.py enemies --only sallet-halberdier` and `render.py enemies --only
sallet-halberdier`, open `docs/art/models/late/sallet-halberdier.png`, and iterate
until the stills read as the concept **and** the POSED views deform cleanly.
`enemies_high.py` is the complete reference: hat lathe, glaive prism, lantern swing
chain, quilted gambeson, laced front, belt kit (warden); C-section quilted coat with
painted bordure, chevron strips, harness straps, spiked collar (hound).

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
- **enemies**: body height within ±5 % of `height_m`, measured with parts marked
  `extras["prop"]` left out (a 2.05 m glaive must not fail a 1.80 m man); bbox
  centred; every `forward_bones` bone points to −Y; exactly one Armature modifier;
  at most 4 influences per vertex. EnemyForge's own checks add every vertex
  weighted, weights summing to 1, vertex groups matching bones, grounded and the
  triangle budget. Stats report `rig_bones`, `skinned_bones`, `mean_influences`,
  `height_body_m`, `height_with_props_m`.

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
- **Heat weighting hands out bones by proximity, not intent.** On the first warden
  build the glaive haft picked up UpperLeg, the hat brim picked up Shoulder and the
  gambeson skirt stayed on Hips, so a lifted knee went straight through it.
  `rig.apply_bind_rules` (rigid/prop, allowed bones, the skirt rule) fixes each.
- **A rigid lantern follows the forearm.** Raising the arm swung the lantern
  horizontal. A swing chain (LanternRing > LanternBody) plus a counter-rotation in
  the review pose shows it hanging; in game that chain is a spring.
- **A sweep's first ring stands perpendicular to its first segment.** Starting the
  sleeve at the shoulder joint and heading outward put a vertical ring above the
  shoulder line: peaks on both shoulders. The sleeve now starts inside the chest,
  below the joint.
- **Emission is multiplied by 9 in the shipping material** (EnemyForge's
  `EMISSION_STRENGTH`). Madder `#C4542E` at 9× rendered pink-white; the warden's horn
  panes store `#4A1E0C` so the result reads as a warm flame.
- **JSON notes can set PBR for a whole family.** The hound's "Dark mask" notes say
  "wet nose roughness 0.2", which `spec.py` applied to the entire mask and made the
  muzzle look lacquered. Override the family (`rough: 0.7`).
- **A figure's height is not the enemy's height.** `height_m` is to the top of the
  hat (warden) or head (hound); the figure's `height` is the skull crown. Place the
  hat to reach the JSON height and mark weapons `prop` so they do not count.
- **Organic parts must opt out of the bevel.** Quilting pinches exceed the 32° bevel
  angle; bevelled, the torso alone would triple. Every figure part sets
  `"bevel": False`.
- **The glTF exporter warns** "More than one shader node tex image used for a
  texture". That comes from EnemyForge's shipping material (ORM feeds both roughness
  and metallic). It's harmless, and it happens on the EnemyForge enemies too.
- **Heat weighting can fail silently.** Blender prints only "Bone Heat Weighting:
  failed to find solution for one or more bones", `parent_set` still succeeds, and
  every vertex keeps a single bone, so knees, elbows and neck hinge like a puppet on
  the POSED views. The triggers found so far are small, thin parts near or pushed
  through other surfaces: the household knight's 12 helm rivet spheres; the Dendra
  champion's 80 separate tusk-plate boxes (16 x 8 x 34 mm) floating 2 mm off the
  helmet cone, and a 5.5 mm midrib rod pushed through its 6 mm rapier blade (each
  alone was enough). Fix by merging such detail into one part (the tusk rows are now
  one ridged `loft` band each) or dropping it. To find the trigger, bisect: import
  the blueprint, filter `bp.parts`, run `assemble.prepare` +
  `ef_assemble.build_armature` + `apply_smooth_weights`, and read `mean_influences`
  (about 1.0 = failed); name the scratch script something other than `bisect.py`,
  which shadows the stdlib module and crashes `bpy` on import. `validate.py` now
  fails the build with "heat weighting silently failed" when the heat pass leaves
  every vertex on one bone (`max_influences <= 1`). Watch the final
  `mean_influences` stat too: many rigid vertices (rivets, plates) pull it down; a
  healthy humanoid sits around 1.4-1.6.
- **It also fails at random on an unchanged mesh.** The Petardier failed on about
  half its builds, and the Palace Guard on about one in four. Retrying identical
  input fails identically, so `rig.smooth_weights_with_retry` (called from
  `assemble.build_rigged`) restores the rigid weights, nudges mesh and rig by under
  1 mm, retries up to 8 times, and puts both back exactly. Each retry prints
  "heat weighting collapsed to one bone (attempt n/8), retrying". Every failure seen
  so far recovered on the second attempt. The retry does not fix a mesh that fails
  every time (the knight's rivets, the Dendra tusks): those still need the part fix
  above.

## AnimForge (enemy animation)

Keyframed clips for the art-bible humans, authored by code in Blender and exported as
animation-only FBX (plan: `docs/plans/artbible-enemy-animations.md`, phases A0-A1).
Phase A1 proves the pipeline on one enemy, the Lantern Warden: the base clips, the
polearm family, and the warden's carry poses and signature clips.

### Commands

```bash
python3 Tools/ArtForge/anim_spec_check.py            # A0 audit: every JSON clip mapped or dropped
python3 Tools/ArtForge/anim_spec_check.py --built    # + every a1 clip is in an exported FBX
python3 Tools/ArtForge/anim.py metrics               # solve every clip, print the numbers (seconds)
python3 Tools/ArtForge/anim.py build                 # FBX files + anim_manifest.json, re-imported to verify
python3 Tools/ArtForge/anim.py review                # sheet + MP4 per clip in docs/art/anim/
python3 Tools/ArtForge/anim.py review --only walk polearm_thrust --no-mp4 --samples 16
```

`build` takes about 5 s. `review` takes about 30 s a clip for the sheet (8 Cycles frames
at 420 px) plus about 2 s a frame for the MP4 (360 px, 8 samples), about 25 minutes for
all 23. Every command exits non-zero when `anim_spec.json` and `library.py` disagree
(clip missing, length or loop flag different), when a re-imported FBX lacks a take or
has the wrong frame count, or when a render is not written. `build` also prints a note
for any clip whose `Footstep` events are not where a foot actually lands.

### Outputs

| Path | What |
|---|---|
| `Tools/ArtForge/anim_spec.json` | A0: the clip taxonomy. `clips` = what AnimForge authors (layer, family, FBX, length, loop, additive, speed, events, status `a1`/`planned`). `enemies` = all 178 clip names the 16 JSON specs list, each mapped to a source clip (plus a carry pose and a playback rate) or dropped with a reason. **The source of truth**: edit it by hand. `anim_spec_seed.py` wrote the first version and refuses to run again without `--force`. |
| `Assets/Models/ArtBible/Animations/Humanoid_Base.fbx` | the 13 base clips on the reference human (20 bones) |
| `…/Humanoid_Polearm.fbx` | the 6 polearm clips on the reference human + `Weapon` (prop on `Hand.R`) and `Weapon_GripL` (the left hand's IK target on the haft), 22 bones |
| `…/LanternWarden_Signature.fbx` | `warden_carry`, `warden_carry_run`, `lantern_raise_search`, `death_drop_lantern` on the warden's own 24-bone rig (they key `Glaive`, `LanternRing`, `LanternBody`) |
| `…/anim_manifest.json` | per FBX: clips, take names, frames, loop, additive, events, and the measured numbers (slide, IK shortfall, fist-to-haft error, lowest sole point, detected landings) |
| `docs/art/anim/<clip>.png` / `.mp4` | review sheet and movie per clip, on the Lantern Warden |

The FBX files carry no Unity import settings, events or loop flags; the engine's importer
reads those from `anim_spec.json`. FBX takes are named `<armature>|<clip>`
(`ReferenceHuman|walk`, `LanternWarden_Rig|lantern_raise_search`); strip the prefix.
Every bone is keyed on every frame at 30 fps with linear interpolation. `Hips` carries
location (bob, sway, the fall); nothing else translates except a detached prop. `Root` never
moves: **every clip is in place** and the NavMeshAgent moves the guard.

### Layout

```
anim.py                     CLI: build | review | metrics
anim_spec.json              A0 taxonomy (source of truth); anim_spec_check.py audits it
anim_forge/
  mathx.py      world-axis rotations, frame_rot, two-bone IK, easing (inout, in, out,
                smooth, overshoot, anticipate)
  skeleton.py   Skeleton (rest data from an armature), Python FK, the retarget corrections,
                the reference human and its Weapon bones
  poses.py      the pose library: named poses + merge / add / scale / mirror
  clip.py       Clip = timed keys (pose, hips, feet, weapon, left-hand weight, elbow hints);
                FootKey, WeaponKey; foot rolling geometry
  gait.py       GaitClip: parametric in-place walk/run matched to the agent speed
  solve.py      Frame -> world-delta rotations with leg and arm IK; retarget(); layer_override()
  props.py      the warden's lantern: simulated swing (baked) and the drop on PropDetach
  library.py    every A1 clip
  bake.py       Actions (every bone, every frame), animation-only FBX export, read-back
  forge.py      rigs, solving on each rig, metrics, export
  review.py     sheets and MP4s
```

### How a clip is made

A **pose** is `{bone: (rx, ry, rz)}` degrees: each bone's rotation relative to its parent,
written in the rest pose's world axes, applied Z·Y·X. +X tips an upright bone (spine, neck)
forward and swings a hanging bone (arm, thigh) *back*; so a thigh lifting forward is −X.
Y rolls sideways (a left arm abducts with −Y, a right arm with +Y). +Z turns the front toward
the figure's own left. Bones a pose doesn't name are at rest. `poses.merge` (later wins),
`add` (sums, for layered tweaks), `scale`, `mirror` (.L↔.R, Y and Z negated).

A **clip** is timed keys. Each key is a full pose, plus `hips=(x, y, z)` (metres), `feet=`
per side `FootKey(x, y, pitch, lift, yaw)` (the flat-foot heel point on the floor; the legs
reach it by IK, so feet stay planted while the hips move), `weapon=WeaponKey(grip, dir, edge)`
(the right fist carries the haft; the left fist grips it `LEFT_GRIP_OFFSET` = −0.45 m along
the haft, weight `lhand`), and `elbows=` pole hints. The key's `ease` shapes the segment
arriving at it: `anticipate` dips back before leaving, `overshoot` passes the key and
settles, `in` is a strike, `out` a recovery. A foot that changes position between keys
steps on an arc.

```python
c = Clip("polearm_thrust", 0.9)
c.key(0.00, P.POLE_READY, hips=(0, 0, -0.06), feet=POLE_FEET, weapon=READY)
c.key(0.30, P.POLE_DRAW,  hips=(0, 0.06, -0.07), feet=POLE_FEET, weapon=draw)          # anticipation
c.key(0.43, P.POLE_EXTEND, hips=(0, -0.20, -0.11), feet=lunge, weapon=ext, ease="in")   # strike
c.key(0.50, P.POLE_EXTEND, hips=(0, -0.22, -0.12), feet=lunge, weapon=over, ease="out") # overshoot
```

Add the clip to `library.clips()` and to `anim_spec.json` (same id, length, loop, FBX,
events), then `anim.py build` and `anim.py review --only <id>`, and open the sheet.

**Locomotion** is `GaitClip(name, GaitParams(speed, cycle, duty, ...), ref)`. Planted feet
move backward at exactly `speed` (in the world, with the agent's travel added back, they stand
still). The stance rolls heel → flat → toe tip; the swing is a minimum-jerk curve with a lift
arc across the whole swing. The hip drop that keeps every leg within `max_extension` of its
length is solved, so the IK never falls short. Left heel strikes at 0, right at 0.5.

| Clip | Speed | Cycle | Stride | Hip drop |
|---|---|---|---|---|
| `walk_slow` | 1.1 m/s (the JSON patrol pace) | 1.10 s | 1.21 m | see the manifest |
| `walk` | 2.0 m/s (engine patrol agent) | 0.867 s | 1.73 m | |
| `run` | 4.2 m/s (engine chase agent) | 0.667 s | 2.80 m | |

Another speed plays the nearest clip at `agent speed / authored speed` (the spec's
`playback_rate`); keep it within about 0.75-1.3 or the cadence reads wrong.

### Rigs and retargeting

Clips are solved on the **reference human**, `figures.Human()` at its defaults (1.76 m,
default A-pose arms). The solver works in world-delta rotations (how far each bone turned
from its rest, about its posed head), which are independent of bone roll. To play a clip on
another human (the warden), each bone is turned to point where the reference bone points:
`Qx @ C⁻¹`, with `C` the swing between the two rests. That is what Unity's Humanoid retarget
does, so the review renders show what the engine will show. The hips offset scales by hip
height.

The warden's arms rest in its carry (right forearm level for the glaive): its **carry pose**
is its rest arms plus small tweaks (`poses.WARDEN_CARRY`). In the review, base clips play with
the carry as an **override layer** over the arms and prop bones (`solve.layer_override`),
like the Animator's upper-body mask; `library.ClipDef.carry_mask` says which bones per clip.

**Two-handed weapons.** The reference's `Weapon` bone sits in the right fist exactly as the
warden's glaive does (measured from the warden rig). After a Humanoid retarget the left hand
misses the haft by up to 0.5 m on another body; `Weapon_GripL` is the IK target, and the
review applies that IK (standing in for Unity's `SetIKPosition`) and plots both.

### Traps

- **`import bpy` before `mathutils`.** The `anim_forge` package imports `bpy` first;
  a script that imports `mathutils` before it fails with "No module named 'mathutils'".
- **No toe bone.** A heel-up foot rolling on the ball pushes the rigid toe 3 cm through the
  floor. `clip.foot_pose` rolls on the toe tip.
- **A swing interpolated in clip space smears.** The foot stops dead in the clip while it
  still brushes the floor, which is a 6-12 cm slide in the world. A world-space swing
  (zero world velocity at both ends) fixes the smear, but the foot then reaches past the
  strike point and the run's hips had to drop 27 cm to reach. The fix: a lift arc across
  the whole swing (`sin(πu)^0.8`), so the foot is off the floor on every swing frame, plus
  a 30 % world blend (`GaitParams.world_lock`). Slide is now at most 0.22 cm.
- **A prop bone's rotation goes stale after IK.** Moving `Hand.R` by IK does not move the
  explicitly keyed `Weapon` rotation; the left hand then grips a haft that is no longer
  there (7-50 cm off). `solve._follow` re-parents props after every arm solve.
- **The carry pose tips the glaive.** Raising the right fist (so the butt clears the floor
  when the walk lowers the hips 6 cm) tips the haft 10°; `Hand.R +29°` straightens it.
- **Blender 5 video output:** `image_settings.media_type = "VIDEO"` must be set before
  `file_format = "FFMPEG"`, or the enum rejects it. Playwright's bundled ffmpeg
  (`/opt/pw-browsers/ffmpeg-*`) encodes only VP8/WebM and cannot write MP4, so the MP4s
  come from Blender's own FFmpeg (H.264) through the sequencer.
- **Blender 5 layered Actions** keep F-curves in `action.layers[].strips[].channelbags`;
  `bake._fcurves` handles both APIs and the bake fails if it finds fewer curves than
  7 per bone.
- **The JSON's speeds are not the agent's.** The warden's JSON walks at 1.1 m/s and runs at
  3.4; the engine role table moves a patrol guard at 2.0 and 4.2. Clips match the agent.


- No normal map is baked. The JSON asks for wheel ridges and punch-work as normal
  detail, but EnemyForge's bake makes BaseMap / Roughness / Metallic / Emission only.
  The amphora's ridges are faint albedo bands (`ridges` in the family spec).
- No painted-panel atlas. The altarpiece's figures are shaped relief in flat family
  colours. Faces have no features.
- No LOD1/LOD2 (50 % / 20 %), even though the brief asks for them.
- All 16 enemies are built, but several dropped small detail (rivets, flutes, straps)
  to keep heat weighting stable; see each blueprint's comments.
- Cloth and jiggle spring bones the JSON rigs ask for are not built: the warden's
  gambeson_skirt ×4 and coif_back, the hound's coat_front/rear/L/R and jowl_L/R.
  The hound has 2 neck and 5 tail bones, not the JSON's exact chain names.
- No blend shapes, no separate jaw/mouth for humans, no fingers (fists only).
- Enemy textures bake at 1024² by default; the brief asks 2048² for enemies
  (`--resolution 2048` works but was not used for the samples).
- `powder_whorl` stays in `items_powder.py` (too specific to promote); `loft` is now
  in `kit.py` and `bronze_loft` is an alias of it.
- No damage or alternate-state meshes (crumpled mask, dented tripod, open cabinet,
  lid-off tureen, ewer dent blend shapes), even where the JSON describes them.
