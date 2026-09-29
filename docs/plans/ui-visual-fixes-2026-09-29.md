# UI visual fixes, 2026-09-29

First look at the redesigned UI in the real Editor (Unity 6000.3.15f1). The Console was clean (0
errors). Screens were captured in Play mode with `unity command capture_game_view --source screen`;
the raid HUD through the real flow (Play Solo, Lair, Set Out). Before-shots:
`docs/generated/ui-fix-2026-09-29/before/`. Reference: `docs/generated/ui-redesign/frames/`.

Layout matches the mockup on every screen. The defects are in type, not structure.

## Issues found

| # | Where | What is wrong | Cause |
|---|---|---|---|
| 1 | every mono eyebrow, key hint, label | Tracking is about 2.5x the mockup's. Spell-panel header reads "HOLD [V] TO CAMSATN A" (two strings overlap); footer "SHIFT SHOUT . CTRL WHISP" runs off the panel; alarm labels "STIRRED" and "ROUSED" touch | `UITheme.Tracked` joins letters with a thin space, but Overpass Mono gives every space the same 0.62 em cell (measured: U+0020, 2009, 200A, 2006, 202F, 2005 all advance 62 of 100) |
| 2 | Lair ledger (Owed, Banked), Victory figure | The big number rides up into the label above it (OWED overlaps "0", "Home with" overlaps the gold figure) and "COIN" hangs below and away from it | `CreateFigureRow` bottom-aligns text in a box shorter than Eczar's line box; Eczar has a deep descent, so the baseline sits about half an em above the box bottom while the mono unit sits near its bottom |
| 3 | Pause panel | "The raid goes on" wraps (as in the mockup) but the two lines are about 115 px apart and overprint the eyebrow and the sentence | Eczar line box at 64 px in a 64 px tall rect, default line spacing |
| 4 | Raid HUD, top-right money | "OWED 0 . BANKED 2,500" and "Nothing in the portal yet" vanish against a bright wall | Flat vellum/faint text with no backing or shadow |
| 5 | Lair "Last raid" cell | Empty before the first raid | Text set to empty string |

Not defects, noted: the Victory piece list is not built (recorded in `docs/5-today/Today.md`); forcing
`GameState.Playing` without a raid leaves the Lair screen up and throws in PurrNet (a test artefact, not
reachable by play).

## Fixes

1. `UITheme.Tracked(text, fontSize)`: separate letters with a rich-text spacer `<size=N> </size>`
   sized so its advance is 0.18 em (mockup). IMGUI styles turn on `richText`. Pass the font size at
   every call site. Update `UIThemeTests`.
2. `UIFactory.CreateFigureRow`: place the figure by measured line metrics so the baseline lands on the
   row bottom and the unit shares it.
3. `PauseMenuScreen`: tighten the title's line spacing and give it room for two lines; move the
   sentence below.
4. `RaidHudView`: a soft shadow under HUD text drawn over the world.
5. `LairScreen`: "No raid yet." when there is no last raid.

## Verify

Recompile, read Console for `error CS`, run the UI EditMode tests, then repeat every capture and read
each frame against the before-shot and the mockup. After-shots go in
`docs/generated/ui-fix-2026-09-29/after/`.
