You are producing ONE Age of the Plunderspell art bible: concept-art SVG sheets plus a structured JSON spec detailed enough to hand straight to a 3D artist so they can build each asset without asking questions. Repo: /home/user/PlunderSpell. Do NOT git commit or push — the coordinator commits. Do not touch files outside your Age's paths listed below.

READ FIRST (all of them):
- /home/user/PlunderSpell/docs/art/BRIEF.md — the rules (scale, palette, budgets, household register, sheet conventions). Obey every one.
- /home/user/PlunderSpell/Tools/ArtBible/README.md — the exact JSON shape.
- /home/user/PlunderSpell/Tools/ArtBible/sheet_template.svg — the sheet frame. Every SVG you make starts from it (same background, glow, grid, ground line, border, title block, palette strip). Keep the frame; adapt the ladder/human/labels per sheet type as described below.
- /home/user/PlunderSpell/docs/systems/scale.md — heights, zones, archways, the 12 m cell.
- Skim /home/user/PlunderSpell/docs/plunderspell.md for voice and the Age's description.

WHAT YOU PRODUCE
1. /home/user/PlunderSpell/docs/art/data/<AGE>.json — exactly 3 structures, 4 enemies (roles patrol, ranged, heavy, special once each), 5 items, following the README schema exactly (slugs kebab-case; concept paths "concept/<AGE>/<slug>.svg").
2. /home/user/PlunderSpell/docs/art/concept/<AGE>/<slug>.svg — one sheet per entry (12 sheets), viewBox 0 0 1200 800.

CONTENT DEPTH (this is the point — a 3D artist builds from it with no follow-up questions):
- description: 3–5 sentences in the herald's voice from the pitch (wry, confident, period-flavoured) that also says what the thing IS and how it plays.
- build: 8–14 bullets for enemies, 6–10 for items, 8–12 for structures. Each bullet names a part, its primitive shape, real dimensions in metres (or cm), construction/period detail, and material. E.g. "Helmet: boar's-tusk cap, hemispherical 0.24 m dia × 0.18 m, 4 rows of tusk plates (each 6 × 2 cm) on a leather base; cheek-pieces 0.12 m long, hinged". Include edge wear, soot, gilt-rub placement where relevant.
- materials: 4–7 per entry, each with hex and texturing notes (roughness feel, wear, tiling scale). Obey the pigment rules: orpiment #C9A227 family ONLY on stealable gold; no verdigris/lapis on enemies; madder only for fire/blood/danger cues.
- enemies: silhouette (what reads at 10 paces), rig.skeleton (Unity Humanoid-compatible or custom quadruped; extra bones for straps/cloth/props), rig.animations 8–12 named clips with a short note each (e.g. "alert_turn — 0.6 s, snaps head toward noise first, body follows"), breakables 3–5 (detachable helmet/shield, damage states, what drops as loot if anything), budget, dont 3–5 things to avoid. Heights MUST be under the clear height of every zone listed, and must make sense for the archways (state crouch if needed).
- items: worth (coin; existing references: copper pot 100, golden goblet 150, silver plate 100, ancient relic 500 artifact; range 60–2000), bulk in stone (silver ewer ≈ 2, altarpiece 14; >10 = two carriers), fragility m/s (2 = glass-fragile, 3–5 = delicate, 6–9 = sturdy, 999 = unbreakable), artifact true for at most 2 per Age, dimensions "W × D × H m", grab points, breaks (how it fractures: number of pieces, what's left, does it spill anything).
- structures: fit the 12 × 12 m cell and the zone's clear height (Crypt 3.00, OuterBailey 3.60, InnerWard 4.00, Keep 4.60, CurtainWall 5.20 open). Sockets must match the archway rule (2.60 m wide, zone height from scale.md) plus any windows/arrow-loops/stairs/murder-holes. gameplay 3–5 bullets (what breaks, burns, hides, what spell interacts).

CONCEPT ART (SVG) — this must look like real concept art, not clip-art:
- Hand-author rich SVG: layered paths with painterly shading (use linearGradient/radialGradient for form, darker multiply-ish shadow shapes, rim highlights in lighter tone, soot/wear flecks, visible plate edges, straps, rivets, folds, textile patterns). Aim for 150–400 elements per enemy sheet; confident silhouettes first.
- Colour strictly from that entry's materials list + the frame colours.
- ENEMIES: scale 1 m = 220 px, ground y = 690. Front view centred x≈470, side (profile) view centred x≈820, both at the same scale, feet on the ground line, heights exactly matching height_m (top at y = 690 − 220·h). Keep the template's metre ladder and 1.80 m human. Quadrupeds: side view as the main view, front view secondary. Callouts (6–10) with leader lines to parts naming part + material, placed in the right margin (x 960–1180) or gaps, not overlapping the figure. Title block: "<AGE NAME> · ENEMY · <ROLE>", name, top-right "H x.xx m · ≤ Nk tris · 2048²" and the scale line.
- ITEMS: hero three-quarter view large on the left/centre (x 120–640), front and side orthographics smaller on the right (x 700–1150) at a stated scale; replace the metre ladder/human with a scale bar (0.5 m or 0.1 m as suits) labelled in px; mark grab points with small verdigris #5FA288 circles + "GRAB" label; show fracture lines as dashed madder #C4542E where it breaks (or "UNBREAKABLE" note). Title block "<AGE NAME> · PLUNDER · <worth> COIN · <bulk> ST".
- STRUCTURES: left ~60% an elevation or cut section with a 1.80 m human silhouette and a height ladder (choose a scale, e.g. 1 m = 100 px, state it top-right), right ~40% a plan view of the 12 × 12 m cell (square with 1 m ticks) showing walls, openings, sockets labelled (DOOR/WINDOW/ARROW-LOOP/STAIR/MURDER-HOLE). Title block "<AGE NAME> · STRUCTURE · <ZONE>". Warm firelight glow from the light source in the room.
- Text: Overpass Mono for labels, Eczar for titles (font-family with fallbacks as in the template). Keep text inside the frame, no overlaps.
- Ids unique within each file. No external references, no <image>, no scripts, no foreignObject.

CHECK YOUR WORK (required, loop until clean):
- cd /home/user/PlunderSpell && python3 Tools/ArtBible/build_art_bible.py --only <AGE>   → must print OK.
- NODE_PATH="$(npm root -g)" node Tools/ArtBible/render_png.cjs <AGE>   → renders PNGs beside the SVGs (keep them; they are deliverables).
- LOOK at every PNG with the Read tool. Fix anything that is overlapping, cropped, crude, off-scale (check heights against the ladder), or unreadable, then re-render. Do at least one full review pass of all 12.
- Final report (short): the 12 names with slug, each enemy height and zones, each item worth/bulk/fragility, and anything you could not get right.
