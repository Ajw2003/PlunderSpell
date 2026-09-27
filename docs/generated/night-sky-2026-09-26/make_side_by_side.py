"""Builds labelled before/after side-by-sides for the night sky and prints how much each
view's colour moved. Run from this folder: python make_side_by_side.py"""
from PIL import Image, ImageChops, ImageDraw, ImageStat

VIEWS = ["1-strip-looking-up", "2-along-strip", "3-overhead", "4-great-hall-looking-up"]

for view in VIEWS:
    before = Image.open(f"{view}-before.png").convert("RGB")
    after = Image.open(f"{view}-after.png").convert("RGB")
    w, h = before.size
    sheet = Image.new("RGB", (w * 2 + 12, h + 44), (20, 18, 16))
    sheet.paste(before, (0, 44))
    sheet.paste(after, (w + 12, 44))
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 14), "BEFORE: fog over the whole sky, no stars", fill=(235, 225, 210))
    draw.text((w + 24, 14), "AFTER: sky clarity 0.75 overhead, stars", fill=(235, 225, 210))
    sheet.save(f"{view}-side-by-side.png")

    mb = ImageStat.Stat(before).mean
    ma = ImageStat.Stat(after).mean
    diff = ImageStat.Stat(ImageChops.difference(before, after)).mean
    print(f"{view}: mean RGB before {[round(v, 1) for v in mb]}, after {[round(v, 1) for v in ma]}, "
          f"mean abs difference {[round(v, 2) for v in diff]} (of 255)")
