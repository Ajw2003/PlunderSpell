# Art bible brief — structures, enemies and plunder of the four Ages

This is the brief every per-Age handoff sheet in `docs/art/` was written against. It exists so a
3D artist (or a later agent adding a fifth Age) can see the rules the sheets obey, rather than
reverse-engineering them from the sheets.

Source of truth for the look: the pitch bible [`docs/plunderspell.md`](../plunderspell.md) and its
mood board [`docs/generated/plunderspell-moodboard.html`](../generated/plunderspell-moodboard.html).
Source of truth for sizes: [`docs/4-systems/scale.md`](../4-systems/scale.md).

## What each Age gets

| Kind | Count per Age | Total |
|---|---|---|
| Structures (architectural set pieces / module kits) | 3 | 12 |
| Enemies | 4 | 16 |
| Plunder items (loot) | 5 | 20 |

"Items" here means **plunder** — the things you steal, which map one-to-one onto the game's
`LootItem` asset (`Assets/_Project/Scripts/Runtime/Loot/LootItem.cs`): `Worth` (coin), `Bulk`
(stone; over 10 forces a two-player carry), `Fragility` (impact speed in m/s that shatters it; 999
means unbreakable), `IsArtifact`. Weapons already have their own section in the original mood board
and are not repeated here.

## The register: the household, not monsters

The pitch's threat is human — "the household", guards, a war-hound at your heels. Every enemy here
is a person or an animal that plausibly defended a building in that century. No constructs,
spirits or casters. (This takes the pitch's side of the bestiary fork raised in
[`docs/plans/moodboard-gap-closure.md`](../plans/moodboard-gap-closure.md) §2.6 for the new
roster; it does not delete or rework the five existing supernatural enemies.)

Each Age's four enemies fill four roles: **patrol** (the common guard), **ranged**, **heavy**
(the one you avoid), and **special** (the Age's signature threat).

## Hard constraints (from `docs/4-systems/scale.md`)

- Standard human: **1.80 m**, eyes 1.65 m, body radius 0.40 m.
- Zone clear heights: Crypt 3.00 m · OuterBailey 3.60 m · InnerWard 4.00 m · Keep 4.60 m ·
  CurtainWall 5.20 m (open). **No enemy may be taller than the clear height of the shortest zone
  it is posted to.**
- Archways: 2.60 m wide; Crypt 2.16 m tall, OuterBailey 2.59, InnerWard 2.88, Keep 3.31. An enemy
  that must path through a zone must fit its archway (crouch/duck animation allowed, state it).
- Castle modules: **12.0 m × 12.0 m** footprint, 0.30 m floor slab, walls rise from the slab top.
  Structures are authored to that grid.
- Models stand on their origin (z = 0), centred, facing forward (−Y in Blender → +Z in Unity).
- Bulk: silver ewer ≈ 2 st, altarpiece 14 st, a downed friend 12 st. Over 10 st = two carriers.

## Look (from the mood board)

- Stylised, never photographic. Thick readable shapes; the silhouette reads at ten paces in the
  dark before any face does.
- Hand-painted surfaces, soot in crevices, gilt worn off wherever a hand would touch.
- Pigments only: Bone Black `#14120E`, Vellum `#DCD2BA`, Verdigris `#5FA288` (arcane / touchable
  glow only), Orpiment `#C9A227` (gold and value **only** — nothing else in the game is gold),
  Madder `#C4542E` (fire, alarm, blood), Lapis `#7A6AA0` (voice, portals). Enemy costume never uses
  orpiment unless the gold on it is itself stealable; it never uses verdigris or lapis.
- Budgets (per model, LOD0): patrol/ranged enemy ≤ 8k tris, heavy ≤ 12k, beast ≤ 6k; hand-held
  plunder ≤ 1.5k, two-hand plunder ≤ 3k, dual-carry plunder ≤ 5k; structure modules ≤ 25k per
  12 m cell. One 2048² texture set per enemy (Albedo, Normal, packed ORM), 1024² per plunder
  item, trim sheets + tiling sets for structures. LOD1 50%, LOD2 20%.

## Concept art conventions

Every sheet is an SVG in `docs/art/concept/<age>/<slug>.svg` (viewBox `0 0 1200 800`) with a PNG
render beside it (`<slug>.png`, 2400 × 1600).

- Background Bone Black / Ash with a faint 0.5 m grid in `#262119`; ground line marked.
- **Enemies**: front and side orthographic views at the same scale, a Vellum-faint 1.80 m
  reference human silhouette, a metre ladder, callouts naming parts and materials.
- **Plunder**: a three-quarter hero view plus front and side orthographics, a 0.5 m scale bar,
  callouts, grab points marked in Verdigris.
- **Structures**: elevation or section plus a plan on the 12 m cell, a 1.80 m human for scale,
  sockets (door / window / arrow-loop / stair / murder-hole) labelled.
- A palette strip of the model's own material swatches, hex labelled.
- Callout and label text in `Overpass Mono`; titles in `Eczar`.

## Where it goes

| Path | What |
|---|---|
| `docs/art/data/<age>.json` | the structured spec for one Age — the single source |
| `docs/art/concept/<age>/*.svg`, `*.png` | concept sheets |
| `docs/art/<age>.md` | the handoff sheet, generated from the JSON |
| `docs/generated/plunderspell-art-bible-moodboard.html` | the illustrated mood board, generated |
| `Tools/ArtBible/` | the generator and the PNG renderer |
