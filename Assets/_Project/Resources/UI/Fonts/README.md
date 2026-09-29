# UI fonts

Loaded at runtime by `Plunderspell.UI.UIFonts` through `Resources.Load<Font>("UI/Fonts/<name>")`.
The typefaces are the design system's (`docs/generated/design-system/project/README.md`), all under
the SIL Open Font License 1.1 (the `OFL-*.txt` files here), from `github.com/google/fonts`.

| File | Role |
|---|---|
| `Eczar-SemiBold.ttf`, `Eczar-ExtraBold.ttf` | display: titles, the raid clock, spell words, big figures |
| `Spectral-Light.ttf`, `Spectral-Regular.ttf`, `Spectral-Italic.ttf` | body: sentences and captions |
| `OverpassMono-Medium.ttf`, `OverpassMono-SemiBold.ttf` | labels, keys and numbers in rows |

Eczar and Overpass Mono are published only as variable fonts, and Unity's legacy text draws a
variable font at its default weight (400 and 300). The static weights here were cut from
`Eczar[wght].ttf` (600, 800) and `OverpassMono[wght].ttf` (500, 600) with fontTools
(`fontTools.varLib.instancer.instantiateVariableFont`, `updateFontNames=True`), 2026-09-28.
