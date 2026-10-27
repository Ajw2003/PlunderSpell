"""One side-by-side sheet per Age from two ages_check.sh runs (#266).
Usage: python Tools/Unity/before_after_sheet.py <dir holding before/ and after/>
Writes <dir>/sheet-<Age>.png: each view as a before | after pair, two pairs per row."""
import os, sys
from PIL import Image, ImageDraw

root = sys.argv[1]
W, H = 560, 315
for age in sorted(os.listdir(os.path.join(root, "before"))):
    names = sorted(f for f in os.listdir(os.path.join(root, "before", age)) if f.endswith(".png"))
    rows = (len(names) + 1) // 2
    sheet = Image.new("RGB", (4 * W, rows * (H + 16)), (20, 20, 20))
    draw = ImageDraw.Draw(sheet)
    for i, n in enumerate(names):
        for j, side in enumerate(("before", "after")):
            im = Image.open(os.path.join(root, side, age, n)).convert("RGB").resize((W, H))
            x, y = ((i % 2) * 2 + j) * W, (i // 2) * (H + 16) + 16
            sheet.paste(im, (x, y))
            draw.text((x + 4, y - 14), f"{n[:-4]} {side}", fill=(255, 255, 255))
    sheet.save(os.path.join(root, f"sheet-{age}.png"))
    print("wrote", age)
