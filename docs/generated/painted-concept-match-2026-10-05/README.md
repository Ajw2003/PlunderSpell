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

## Round 4 (colour and brightness only)
Measured with `Tools/Unity/wall_colour_table.py` (hand-picked boxes, mean RGB and luminance; art boxes are rough and a few
land on windows or brick, so treat art figures as +-15%). Per-Age stone tints (`_PlunderStoneTint`) multiplied up and shifted
toward gold (G up, B down), ambient lifted and made less orange. Fog, cel, paint, soot untouched.

Round 3 (before):
    Age           | lit art             | lit game            | shadow art          | shadow game        
    BronzeAge     | 109, 64, 26 L 71 |  81, 40, 30 L 48 | 109, 65, 28 L 71 |  41, 17, 15 L 22
    HighMedieval  | 102, 66, 38 L 72 |  67, 35, 26 L 41 |  52, 36, 20 L 38 |  37, 17, 15 L 21
    LateMedieval  | 109, 73, 37 L 78 |  77, 42, 32 L 49 |  66, 41, 21 L 45 |  47, 22, 17 L 27
    AgeOfPowder   |  53, 42, 30 L 44 |  66, 33, 24 L 39 |  44, 32, 20 L 33 |  36, 17, 14 L 21

Round 4 (after):
    Age           | lit art             | lit game            | shadow art          | shadow game        
    BronzeAge     | 109, 64, 26 L 71 | 100, 59, 41 L 66 | 109, 65, 28 L 71 |  65, 36, 22 L 41
    HighMedieval  | 102, 66, 38 L 72 |  89, 58, 39 L 63 |  52, 36, 20 L 38 |  64, 42, 24 L 45
    LateMedieval  | 109, 73, 37 L 78 |  99, 68, 47 L 73 |  66, 41, 21 L 45 |  81, 52, 29 L 56
    AgeOfPowder   |  53, 42, 30 L 44 |  69, 40, 26 L 45 |  44, 32, 20 L 33 |  45, 27, 18 L 30

Saw: lit wall luminance now within about 10% of the art in every Age, hue tan/gold instead of brick-red, fire pools still
the brightest thing. Shadow wall is within 20% except LateMedieval (game 56 vs art 45) and High (45 vs 38) slightly bright.
Still off: the in-game views show the same module geometry in all four Ages (stairs.txt identical across Ages; only fire
counts and tint differ), so each Age's own rooms (columns, vaults, tapestries, props) are absent, and the tiled brick
reads as brick, not painted plaster. The frames match the art's colour and light, not yet its style or content.
