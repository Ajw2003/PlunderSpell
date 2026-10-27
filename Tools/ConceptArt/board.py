"""Before/after board: each source screenshot beside its paintover. Usage: python3 board.py <out-dir>"""
import re
import sys
from PIL import Image, ImageDraw

out = sys.argv[1]
pairs = []
for line in open("Tools/ConceptArt/render_set.sh"):
    m = re.match(r"P (\S+) \$O/(\S+\.png)(.*)", line)
    if m:
        src = m.group(1).replace("$G", "docs/generated").replace("$M", "docs/art/models")
        crop = re.search(r"--crop (\d+),(\d+),(\d+),(\d+)", m.group(3))
        pairs.append((src, m.group(2), tuple(map(int, crop.groups())) if crop else None))

W, H, PAD = 520, 330, 14
board = Image.new("RGB", (PAD + 2 * (2 * W + PAD) + PAD, PAD + ((len(pairs) + 1) // 2) * (H + 34)), (0x14, 0x12, 0x0E))
d = ImageDraw.Draw(board)
for i, (src, name, crop) in enumerate(pairs):
    before = Image.open(src).convert("RGB")
    if crop:
        before = before.crop(crop)
    after = Image.open(f"{out}/{name}").convert("RGB")
    x0 = PAD + (i % 2) * (2 * W + 2 * PAD)
    y0 = PAD + (i // 2) * (H + 34)
    for j, im in enumerate((before, after)):
        im.thumbnail((W, H))
        board.paste(im, (x0 + j * W + (W - im.width) // 2, y0 + (H - im.height) // 2))
    d.text((x0, y0 + H + 6), f"{name}  <-  {src}", fill=(0xDC, 0xD2, 0xBA))
board.save(f"{out}/00-before-after-board.jpg", quality=88)
print(f"{len(pairs)} pairs -> {out}/00-before-after-board.jpg")
