"""One image per Age: that Age's two painted concept v2 images over two in-game views (#268).
Usage: python Tools/Unity/concept_compare.py <ages-check dir> <out dir>"""
import os, sys
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
W, H = 640, 360
con = "docs/art/concept/painted"
prefix = {"BronzeAge": "bronze", "HighMedieval": "high", "LateMedieval": "late", "AgeOfPowder": "powder"}
for age, p in prefix.items():
    tiles = [os.path.join(con, p + "-court-v2.png"), os.path.join(con, p + "-hall-v2.png"),
             os.path.join(src, age, "ground-ward.png"), os.path.join(src, age, "keep-corner.png")]
    sheet = Image.new("RGB", (2 * W, 2 * H))
    for i, t in enumerate(tiles):
        sheet.paste(Image.open(t).convert("RGB").resize((W, H)), ((i % 2) * W, (i // 2) * H))
    sheet.save(os.path.join(out, f"compare-{age}.png"))
    print("wrote", age)
