"""
Stitch every previews/<Key>.png rendered by render_previews.py into one
contact sheet. Plain python3 + Pillow (no bpy) since Blender's bundled
Python here has no PIL.

    python3 Tools/AssetPipeline/make_contact_sheet.py
"""
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import asset_specs  # noqa: E402

PREVIEWS_DIR = os.path.join(os.path.dirname(__file__), "previews")
THUMB = 260
PAD = 14
LABEL_H = 26


def main():
    # The original sheet (props + the High Medieval castle) keeps exactly its
    # old contents; each other Age gets a sheet of its own, so one image
    # shows one Age's castle side by side (docs/plans/era-castle-rooms.md).
    era_keys = {spec["key"] for spec in asset_specs.ERA_CASTLE_SPECS}
    write_sheet([s["key"] for s in asset_specs.ALL_SPECS if s["key"] not in era_keys], "_contact_sheet.png")
    for era in dict.fromkeys(s["era"] for s in asset_specs.ERA_CASTLE_SPECS):
        keys = [s["key"] for s in asset_specs.ERA_CASTLE_SPECS if s["era"] == era]
        write_sheet(keys, f"_contact_sheet_{era}.png")


def write_sheet(keys, filename):
    paths = [os.path.join(PREVIEWS_DIR, f"{k}.png") for k in keys]
    missing = [p for p in paths if not os.path.isfile(p)]
    if missing:
        raise SystemExit(f"missing renders, run render_previews.py first: {missing}")

    cols = 4
    rows = math.ceil(len(paths) / cols)
    cell_w, cell_h = THUMB, THUMB + LABEL_H
    sheet = Image.new(
        "RGB",
        (cols * cell_w + (cols + 1) * PAD, rows * cell_h + (rows + 1) * PAD),
        (20, 18, 14),
    )
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("DejaVuSans.ttf", 15)
    except OSError:
        font = ImageFont.load_default()

    for i, (key, p) in enumerate(zip(keys, paths)):
        img = Image.open(p).convert("RGB").resize((THUMB, THUMB))
        col, row = i % cols, i // cols
        x = PAD + col * (cell_w + PAD)
        y = PAD + row * (cell_h + PAD)
        sheet.paste(img, (x, y))
        draw.text((x + 4, y + THUMB + 4), key, fill=(220, 210, 186), font=font)

    out = os.path.join(PREVIEWS_DIR, filename)
    sheet.save(out)
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
