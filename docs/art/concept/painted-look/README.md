# Painted-look concept art (2026-10-06)

Paintovers of existing captures and art-bible model sheets, aimed at the shader look being built
for the game: painterly fill, dip-pen ink outlines, cross-hatching only in shadow (three tiers, by
the shot's own value spread), paper tone and film grain, graded toward the art bible pigments
(Bone Black, umber, Vellum) while fire, gold and portal light keep their colour.

These are concepts, not in-game renders: no game asset or shader was changed.

- `00-before-after-board.jpg` puts every source beside its paintover.
- `01`–`11` are the full-size pieces.
- `cel-shaded/` is the second pass, after feedback asking for less cross-hatching, softer lines and more
  cel shading: flat colour per surface (mean-shift), one hard cool shadow step, thin steady outlines,
  and hatching only as a faint touch in the deepest shadow. Same sources, same names.

Regenerate (needs `pillow numpy opencv-python-headless`):

    bash Tools/ConceptArt/render_set.sh docs/art/concept/painted-look
    python3 Tools/ConceptArt/board.py docs/art/concept/painted-look
    bash Tools/ConceptArt/render_set.sh docs/art/concept/painted-look/cel-shaded --cel
    python3 Tools/ConceptArt/board.py docs/art/concept/painted-look/cel-shaded

Sources and per-shot settings live in `Tools/ConceptArt/render_set.sh`; the look itself is
`Tools/ConceptArt/paintover.py`.
