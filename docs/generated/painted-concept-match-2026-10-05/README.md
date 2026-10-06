# Painted concept match (#268)

Each round: `round-N/after/<Age>/` (ages_check views), `sheet-<Age>.png` (before | after), `compare-<Age>.png`
(that Age's two concept v2 images on top, in-game ground-ward and keep-corner below). Values live in
`Tools/Unity/eval/set_paint_look.cs`, `NightLooks.cs`.

## Round 1
Changed: fog density 0.038 to 0.012 and fire scatter 1.9 to 0.35 (halo gone), ambient lifted to warm umber, cel softness
0.04 to 0.3, two-tone blotch shader, grain 0.45, ink 0.6, new wall soot (0.4). Saw: walls now read across the room, blotches and
brick mottling visible, halo gone. Still off: much darker and redder than the art; fire discs still bright and round.

## Round 2
Changed: ambient about 1.7x brighter, stone tints toward gold. Saw: fill-lit walls readable, keep corner no longer black.
Still off: hue is brick red, not tan/gold; Bronze and Powder look the same as High.

## Round 3
Changed: ambient less orange, stone tints more neutral/yellow, cel softness 0.45, grain 0.6. Saw: slightly warmer-neutral,
mottle strongest yet, pools softer. Still off: NOT the same style. Surfaces are still the kit's brick albedo (red) with a
square-tiled look; the art's plaster, flat limestone fields and dark-ink outlines on props are not there; per-Age palettes
do not differ in these two views (all four show the same brick modules); exposure is far below the art's golden fill.
