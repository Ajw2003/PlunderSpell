"""Painted concept art for each Age (#267): the target the in-game painted look is tuned towards.

Runs locally on Z-Image-Turbo (already in the Hugging Face cache). Prompts are built from the moodboard
(docs/generated/plunderspell-moodboard.html: umber and vellum, candle-soot, nothing lit by nothing) and
the per-Age palettes in docs/art/<age>.md. Writes docs/art/concept/painted/<age>-<scene>.png.

    python Tools/ArtForge/painted_concepts.py            # every Age and scene
    python Tools/ArtForge/painted_concepts.py high       # one Age
"""
import sys
from pathlib import Path

import torch
from diffusers import ZImagePipeline

OUT = Path(__file__).resolve().parents[2] / "docs" / "art" / "concept" / "painted"
SEED = 231

STYLE = (
    "hand-painted concept art for a stylised stealth video game, cel shaded with flat colour bands and crisp "
    "shadow shapes, thick visible gouache brush strokes on every surface, dirt and grime and soot settled in the "
    "crevices, worn edges, chipped plaster, dark ink linework, muted umber and vellum palette, deep warm "
    "shadows falling away into black, lit only by candles, torches and hearth fire, painterly, illustrated, "
    "no text, no people in the foreground"
)

AGES = {
    "bronze": {
        "look": "Bronze Age Mycenaean citadel at night, ochre painted plaster and mud-brick over huge cyclopean "
                "stones, red painted columns, hearth smoke haze, clay pithos jars",
        "hall": "a low-ceilinged megaron hall with a round central hearth, painted dado, smoke-blackened rafters",
        "court": "a citadel courtyard with the lion gate, braziers on poles, mud-brick walls",
    },
    "high": {
        "look": "High Medieval stone castle at night, limestone ashlar, oak doors with iron straps, woad blue "
                "and undyed wool banners",
        "hall": "a vaulted castle chapel with lancet windows of ruby and blue glass, candlesticks, a gilded altarpiece",
        "court": "an inner bailey between curtain walls, wall torches in sconces, a stone well, a spiral stair tower",
    },
    "late": {
        "look": "Late Medieval fortress at night, dressed sandstone and red brick vaults, heavy oak, "
                "black iron, tapestries",
        "hall": "a great hall with a hooded hearth, long oak tables, a tapestry, brick-lined vault",
        "court": "a crooked barbican gate passage with murder holes, lanterns, flagstones wet with rain",
    },
    "powder": {
        "look": "early seventeenth century Habsburg palace at night, lime-washed vellum-grey walls darkening "
                "with soot toward the cornice, black walnut panelling, blued steel, tall windows",
        "hall": "a kunstkammer cabinet room with walnut cabinets of curiosities, candelabra, a brass astrolabe",
        "court": "a palace courtyard under tall dark windows, lanterns, gravel, a vaulted magazine door",
    },
}


def prompts_for(age):
    entry = AGES[age]
    for scene in ("hall", "court"):
        yield scene, f"{entry[scene]}, {entry['look']}. {STYLE}"


def main(ages):
    pipe = ZImagePipeline.from_pretrained("Tongyi-MAI/Z-Image-Turbo", torch_dtype=torch.bfloat16)
    pipe.enable_model_cpu_offload()  # 12 GB card; the Unity Editor holds some of it
    OUT.mkdir(parents=True, exist_ok=True)
    with open(OUT / "prompts.txt", "a", encoding="utf-8") as log:
        for age in ages:
            for scene, prompt in prompts_for(age):
                image = pipe(prompt=prompt, width=1344, height=768, num_inference_steps=9, guidance_scale=0.0,
                             generator=torch.Generator("cuda").manual_seed(SEED)).images[0]
                path = OUT / f"{age}-{scene}.png"
                image.save(path)
                log.write(f"{path.name}\tseed {SEED}\t{prompt}\n")
                print("wrote", path, flush=True)


if __name__ == "__main__":
    main(sys.argv[1:] or list(AGES))
