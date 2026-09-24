# Generated

Tool-produced deliverables. Nothing here is hand-edited — if one of these needs to change, it
gets regenerated from the tool that made it, not patched in place.

| File | Produced by | What it is |
|---|---|---|
| `castle-generator-visualization.html` | hand-authored, standalone | Interactive visualization of the procedural castle generator's ward layout. Open directly in a browser. |
| `plunderspell-moodboard.html` | hand-authored, standalone | Illustrated mood board for the pitch bible (`docs/plunderspell.md`) — palette, light, per-era art direction. Open directly in a browser. |
| `plunderspell-art-bible-moodboard.html` | `Tools/ArtBible/build_art_bible.py` | Companion mood board covering only structures, enemies and plunder of the four Ages, with every concept sheet inline. Generated from `docs/art/data/*.json` — edit those, not this. |
| `ui-preview.html` | hand-authored, standalone | Source-accurate HTML preview of the six UI screens, built alongside `UIScreenshotPlayModeTests.cs`'s captures in `UI_Verification_Screenshots/`. |
| `enemy-stance-screenshots/` | `Tools ▸ Plunderspell ▸ Capture Enemy Stance Screenshots` (`EnemyStanceScreenshotForge`) | Every enemy prefab side-on on a ground slab, plus a per-enemy feet close-up and `stance-report.txt` of measured lowest point and height. `before/` and `after/` are the evidence for issue 94; the `test-*.json` files are the raw `ScaleInvariantTests` results either side of the fix. |
| `github-issues.json` | `Tools/mkissues.py` | The manifest of GitHub issues that script has filed — issue number, title and URL, one entry per successful `gh issue create` call. Regenerate by re-running the script; it appends whatever `ISSUES` list is in the script at the time. |

The first three predate this pipeline's `docs/generated/` convention and were authored by hand
rather than by a script — they live here because they're deliverables you view rather than edit,
not because a build step produced them. `github-issues.json` is the one genuine tool-output entry
so far.
