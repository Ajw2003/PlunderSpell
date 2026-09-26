# Enemy asset pipeline (EnemyForge)

Generates the Plunderspell enemy roster — mesh, rig, textures and exports — from
Python, with no hand-modelling step. Source lives in `Tools/EnemyForge/`, output in
`Assets/Models/Enemies/`, review renders in `Enemy_Renders_Review/`.

## What it produces

Five archetypes, each a single rigged mesh with one baked material:

| Archetype | Role | Height | Triangles | Skinned bones |
|---|---|---|---|---|
| SigilWisp | Scout flier, swarms | 1.03 m | 2 668 | 5 |
| VaultWarden | Melee grunt | 2.25 m | 4 320 | 18 |
| HexTurret | Static sentry | 1.46 m | 4 056 | 4 |
| ArcRevenant | Elite caster, hovers | 2.48 m | 5 940 | 11 |
| GildedColossus | Vault boss | 3.41 m | 6 428 | 18 |

"Skinned bones" counts the bones that actually own geometry, which is one or two
fewer than the rig's bone count: `Root` is a handle for moving the whole enemy and
carries no vertices of its own.

Counts come from `Assets/Models/Enemies/enemy_manifest.json`, which the build writes
on every run; treat that file as the source of truth rather than this table.

## How it works

The pipeline is four stages, one module each.

**`parts.py` — geometry.** An archetype is a flat list of `Part` records, each a
primitive (box, cylinder, cone, sphere, icosphere, torus, shard) with a position,
a full-extent size, an XYZ euler rotation, a surface family and an owning bone.
`build_bmesh` instantiates them into one bmesh. Every part stays a separate closed
island — nothing is welded between parts — which is what lets surface families meet
at crisp boundaries without UV seams. `mirror=True` emits a second copy reflected
across the YZ plane; reflection flips handedness, so those faces are reversed to keep
normals outward.

**`materials.py` — surfacing.** Five surface families (stone, gold, arcane, iron,
cloth) each become a standalone authoring material in its own material slot. Each
exposes four channels, which are baked — as pure emission, so no lighting leaks in —
into one shared texture set. A single plain Principled material is then rebuilt from
those bakes and is the only material that ships. The review renders therefore show
exactly what Unity will load.

**`assemble.py` — assembly.** Bevels and shades the mesh, unwraps it, binds the rig,
runs the bake, and exports FBX (Unity axes), glTF and the `.blend` source. Skin
weights are rigid: one bone per part at weight 1. That is not a shortcut — every
enemy is a rigid construct, so there is nothing to deform smoothly.

**`validate.py` — verification.** Runs before export and fails the build on anything
that would break a game import.

## Invariants

These must stay true; the validator enforces each one.

- Models stand on `z = 0`, centred on `x = y = 0`, facing `-Y`. Blender's FBX exporter
  maps `-Y` to Unity's `+Z` forward, so the pivot lands where Unity expects it.
- Object transforms are identity — location zero, rotation zero, scale one.
- Every shell is closed and manifold, with consistent winding and outward normals
  (checked by signed volume per island: negative means inside-out).
- Every vertex belongs to exactly one bone at weight 1, summing to 1.
- Material slot index equals the family index in `parts.MATERIALS`. `build_bmesh`
  writes that number to each face, so the two cannot drift apart.
- Fliers (`grounded=False`) keep their lowest geometry clear of `z = 0`; grounded
  archetypes touch it within 2 cm.
- Triangle counts stay under each archetype's declared budget.

## Traps

Things that cost time here, recorded so they do not cost it twice.

**bmesh references and index order both go stale.** A bmesh op can reallocate the
element arrays, which invalidates any live `BMVert`/`BMFace` handle held in Python —
reading `.index` later raises `ReferenceError`. Index *ranges* are no better: element
index order does not follow creation order, so `bm.verts[start:]` picks up unrelated
parts. The only reliable approach is to stamp each element the moment it is created,
while the references returned by the primitive op are still live. That is why
`build_bmesh` writes `material_index` and the `bone_id` layer inline rather than
collecting spans and resolving them at the end.

**Material indices are clamped to the number of slots.** Writing `material_index = 3`
to a mesh that has no material slots silently collapses it to 0. This produced a
build where every enemy baked entirely in stone, with all validation passing, because
the indices were forced to 0 after baking anyway. The authoring materials are now
attached before face indices are written, and `texture_and_bake` raises if the
families present on the mesh do not match the families its parts declare.

**Bevel interpolates float vertex attributes across the mesh.** The first design
stored the surface family in a `FLOAT`/`POINT` attribute read by an Attribute node.
Bevel produced fractional and out-of-range values (−0.07, 2.99) where clean integers
were expected. Material slots are integer face data and survive bevel exactly, so
they replaced the attribute entirely.

**A clamped bevel still leaves slivers.** Trim pieces thinner than the bevel width
come out with zero-area faces and therefore zero-area UVs. `use_clamp_overlap` plus a
`dissolve_degenerate` pass after applying the modifier clears them.

**Freestyle is not usable headlessly here.** Enabling `use_freestyle` auto-creates a
`LineSet` whose `linestyle` is `None`, and the renderer dereferences it and aborts the
process. Repairing that lineset stops the crash but Freestyle then draws almost no
lines. The wireframe pass uses a Wireframe shader node instead, which draws the edges
Cycles actually renders. A material override applies to every object in the scene, so
that pass is rendered without a ground plane rather than turning the floor into clay.

**Emission maps clamp at 1.0.** The bakes are 8-bit, so the glow is scaled back up by
`materials.EMISSION_STRENGTH` in the shipping material. Unity needs the same
multiplier on the URP Lit emission colour, as an HDR value.

## Environment

This pipeline runs against the Blender Python module (`pip install bpy`), not a
Blender CLI — there is no `blender` binary on the build machine. Scripts are run as
`python3 script.py`, never `blender --background --python script.py`.

Rendering uses **Cycles on CPU**. EEVEE is unavailable: with no GPU and no
`libEGL.so.1` it does not raise, it aborts the interpreter. Workbench is unavailable
for the same reason.
