"""Tile the four look samples into one labelled 2x2 sheet for side-by-side comparison.

Run from the repo root after render_look_samples.py (needs Pillow, not Blender):
    python Tools/LookSamples/make_contact_sheet.py
Output: docs/generated/look-samples-2026-09-24/contact-sheet.png
"""

import os

from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "..", "docs", "generated", "look-samples-2026-09-24")

SAMPLES = [
    ("a-dishonored", "A  Dishonored"),
    ("b-sea-of-thieves", "B  Sea of Thieves"),
    ("c-thief-hunt", "C  Thief / Hunt"),
    ("d-valheim", "D  Valheim"),
]
TILE_W, TILE_H = 800, 450


def main():
    sheet = Image.new("RGB", (TILE_W * 2, TILE_H * 2))
    for i, (name, label) in enumerate(SAMPLES):
        tile = Image.open(os.path.join(OUT, name + ".png")).convert("RGB").resize((TILE_W, TILE_H))
        draw = ImageDraw.Draw(tile)
        draw.rectangle((0, 0, 200, 30), fill=(0, 0, 0))
        draw.text((10, 9), label, fill=(230, 220, 190))
        sheet.paste(tile, ((i % 2) * TILE_W, (i // 2) * TILE_H))
    path = os.path.join(OUT, "contact-sheet.png")
    sheet.save(path)
    print(f"[LookSamples] wrote {os.path.abspath(path)}")
    write_state_pair()


def write_state_pair():
    """The chosen direction: the same castle calm, then alerted, stacked."""
    width, height = 960, 540
    sheet = Image.new("RGB", (width, height * 2))
    for i, (name, label) in enumerate([("calm", "CALM  castle asleep"),
                                       ("alert", "ALERT  alarm raised")]):
        tile = Image.open(os.path.join(OUT, name + ".png")).convert("RGB").resize((width, height))
        draw = ImageDraw.Draw(tile)
        draw.rectangle((0, 0, 230, 30), fill=(0, 0, 0))
        draw.text((10, 9), label, fill=(230, 220, 190))
        sheet.paste(tile, (0, i * height))
    path = os.path.join(OUT, "calm-vs-alert.png")
    sheet.save(path)
    print(f"[LookSamples] wrote {os.path.abspath(path)}")


if __name__ == "__main__":
    main()
