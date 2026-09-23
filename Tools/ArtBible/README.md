# ArtBible

Turns the per-Age art specs in `docs/art/data/*.json` into:

- `docs/art/<age>.md` — the handoff sheet a 3D artist builds from (one per Age);
- `docs/generated/plunderspell-art-bible-moodboard.html` — the illustrated mood board of the
  structures, enemies and plunder, in the style of the original pitch mood board;
- `docs/art/concept/<age>/<slug>.png` — a 2400 × 1600 render of every concept SVG.

The rules the specs follow are in [`docs/art/BRIEF.md`](../../docs/art/BRIEF.md).

## Commands

Run from the repo root.

```bash
python3 Tools/ArtBible/build_art_bible.py            # validate all four Ages, write the .md sheets and the mood board
python3 Tools/ArtBible/build_art_bible.py --check    # validate only, write nothing
NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs   # render every concept SVG to PNG (needs Playwright + Chromium)
```

The builder exits non-zero and names every problem it found (a missing field, a wrong count, an
enemy taller than a zone it is posted to, a concept SVG that does not exist). It writes nothing
unless the whole set validates.

## JSON shape (one file per Age)

```jsonc
{
  "slug": "bronze",                 // bronze | high | late | powder — also the concept folder name
  "name": "The Bronze Age",
  "year": "c. 1200 BC",
  "stratum": "I",
  "headline": "Citadels of mud-brick and smoke",
  "intro": "One paragraph, herald's voice.",
  "palette": [ { "name": "Ochre plaster", "hex": "#B7803E", "use": "walls, floors" } ],

  "structures": [ {                 // exactly 3
    "slug": "lion-gate", "name": "The Lion Gate", "zone": "CurtainWall",
    "footprint": "12 × 12 m, one cell", "height_m": 5.2,
    "summary": "One line.", "description": "Paragraph.",
    "build": ["Geometry instruction with real dimensions", "..."],
    "sockets": ["Door, north face: 2.60 × 2.59 m", "..."],
    "materials": [ { "name": "Cyclopean limestone", "hex": "#8C7F68", "notes": "tiling 2 m, ..." } ],
    "gameplay": ["What it does in play: breakable, flammable, ..."],
    "budget": "≤ 25k tris; ...",
    "concept": "concept/bronze/lion-gate.svg"
  } ],

  "enemies": [ {                    // exactly 4, roles patrol / ranged / heavy / special once each
    "slug": "palace-levy", "name": "Palace Levy", "role": "patrol",
    "zones": ["CurtainWall", "OuterBailey"], "height_m": 1.70,
    "summary": "One line.", "description": "Paragraph.",
    "silhouette": "What reads at ten paces in the dark.",
    "build": ["Part: shape, size, material", "..."],
    "materials": [ { "name": "...", "hex": "#...", "notes": "..." } ],
    "rig": { "skeleton": "Humanoid (Unity Mecanim), ...", "animations": ["idle", "..."] },
    "breakables": ["Detachable part / damage state", "..."],
    "budget": "≤ 8k tris, one 2048² set",
    "dont": ["Thing to avoid", "..."],
    "concept": "concept/bronze/palace-levy.svg"
  } ],

  "items": [ {                      // exactly 5; worth/bulk/fragility map onto LootItem
    "slug": "oxhide-ingot", "name": "Oxhide Ingot",
    "worth": 180, "bulk": 4, "fragility": 999, "artifact": false,
    "dimensions": "0.60 × 0.40 × 0.05 m (W × D × H)",
    "summary": "One line.", "description": "Paragraph.",
    "build": ["..."], "materials": [ { "name": "...", "hex": "#...", "notes": "..." } ],
    "grab": "Where hands go.", "breaks": "How it fractures, into how many pieces.",
    "budget": "≤ 1.5k tris, 1024² set",
    "concept": "concept/bronze/oxhide-ingot.svg"
  } ]
}
```

Zones are the five in `docs/systems/scale.md`: `Crypt`, `OuterBailey`, `InnerWard`, `Keep`,
`CurtainWall`. Paths in `concept` are relative to `docs/art/`.
