# Painted concept art — how it is made

Target art for the in-game painted look (#267, part of #262). Generated locally, free, on this machine's
RTX 5070; no cloud service. Generator: [`Tools/ArtForge/painted_concepts.py`](../../../../Tools/ArtForge/painted_concepts.py).

## The recipe that made `high-hall.png` (2026-10-05, owner: "the quality is worth replicating")

Exact, so it can be reproduced or turned into a reusable workflow:

| | |
|---|---|
| Script | `Tools/ArtForge/painted_concepts.py` at commit `462b139a`, run as `python Tools/ArtForge/painted_concepts.py high` |
| Model | `Tongyi-MAI/Z-Image-Turbo` (Hugging Face snapshot `f332072aa78be7aecdf3ee76d5c247082da564a6`), text-to-image |
| Pipeline | `diffusers.ZImagePipeline`, diffusers 0.39.0.dev0, transformers 5.8.1, torch 2.12.0.dev+cu128, `bfloat16`, `enable_model_cpu_offload()` |
| Settings | 1344 x 768, 9 steps, guidance 0.0 (Turbo is distilled: no CFG), seed 231 on a CUDA generator |
| Time | about 5 minutes on first run including model load to the GPU, with the Unity Editor open |

Prompt (scene + Age look + shared style, in that order):

> a vaulted castle chapel with lancet windows of ruby and blue glass, candlesticks, a gilded altarpiece, High
> Medieval stone castle at night, limestone ashlar, oak doors with iron straps, woad blue and undyed wool banners.
> hand-painted concept art for a stylised stealth video game, cel shaded with flat colour bands and crisp shadow
> shapes, thick visible gouache brush strokes on every surface, dirt and grime and soot settled in the crevices,
> worn edges, chipped plaster, dark ink linework, muted umber and vellum palette, deep warm shadows falling away
> into black, lit only by candles, torches and hearth fire, painterly, illustrated, no text, no people in the
> foreground

## Why it works (what to keep in a reusable version)

- **Three-part prompt**: what the room is, what the Age's materials and palette are (from `docs/art/<age>.md`), then
  one shared style block. Only the first two change per image, so every Age reads as one game.
- **The style block names techniques, not artists**: "cel shaded with flat colour bands", "gouache brush strokes",
  "dark ink linework", "grime and soot in the crevices". Z-Image-Turbo renders each of these literally.
- **Turbo at 9 steps, guidance 0**: the model is distilled for that; more steps or CFG muddy it.
- **Fixed seed per run**, so a prompt change is the only thing that changes the picture.

Known gap: this image is brighter and more saturated than the moodboard's candle-lit, umber night. The next
version of the prompt pushes it darker; this recipe stays as the record of the first one.
