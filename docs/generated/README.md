# Generated

Tool-produced deliverables. Nothing here is hand-edited — if one of these needs to change, it
gets regenerated from the tool that made it, not patched in place.

| File | Produced by | What it is |
|---|---|---|
| `castle-generator-visualization.html` | hand-authored, standalone | Interactive visualization of the procedural castle generator's ward layout. Open directly in a browser. |
| `plunderspell-moodboard.html` | hand-authored, standalone | Illustrated mood board for the pitch bible (`docs/plunderspell.md`) — palette, light, per-era art direction. Open directly in a browser. |
| `ui-preview.html` | hand-authored, standalone | Source-accurate HTML preview of the six UI screens, built alongside `UIScreenshotPlayModeTests.cs`'s captures in `UI_Verification_Screenshots/`. |
| `damp-cave-treasure-portal.svg` | `Tools/cave_scene_svg.py` | Vector illustration: a damp cave with a portal on a stone dais, strewn with treasure, weapons and wizard hats. Animated (portal swirl, drips, glints) when opened in a browser. Deterministic — regenerate with `python3 Tools/cave_scene_svg.py`. |
| `damp-cave-treasure-portal.png` | headless Chromium screenshot of the SVG | 1600×900 still preview of the SVG above. |
| `github-issues.json` | `Tools/mkissues.py` | The manifest of GitHub issues that script has filed — issue number, title and URL, one entry per successful `gh issue create` call. Regenerate by re-running the script; it appends whatever `ISSUES` list is in the script at the time. |

The first three predate this pipeline's `docs/generated/` convention and were authored by hand
rather than by a script — they live here because they're deliverables you view rather than edit,
not because a build step produced them. `github-issues.json` is the one genuine tool-output entry
so far.
