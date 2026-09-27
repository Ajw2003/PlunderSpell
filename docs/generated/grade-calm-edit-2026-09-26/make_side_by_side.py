"""Builds labelled before/after side-by-sides for the Grade_Calm edit and prints how much each
view's colour moved. Run from this folder: python make_side_by_side.py"""
from PIL import Image, ImageChops, ImageDraw, ImageStat

VIEWS = ["1-portal-strip", "2-along-strip", "3-great-hall"]

for view in VIEWS:
    before = Image.open(f"{view}-before.png").convert("RGB")
    after = Image.open(f"{view}-after.png").convert("RGB")
    w, h = before.size
    sheet = Image.new("RGB", (w * 2 + 12, h + 44), (20, 18, 16))
    sheet.paste(before, (0, 44))
    sheet.paste(after, (w + 12, 44))
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 14), "BEFORE: committed Grade_Calm (lift 0.99,0.99,1.02 / gain 1.05,1,0.92)", fill=(235, 225, 210))
    draw.text((w + 24, 14), "AFTER: your edit (lift 0.97,0.97,1.00 / gain 1.00,0.95,0.88)", fill=(235, 225, 210))
    sheet.save(f"{view}-side-by-side.png")

    mb = ImageStat.Stat(before).mean
    ma = ImageStat.Stat(after).mean
    diff = ImageStat.Stat(ImageChops.difference(before, after)).mean
    print(f"{view}: mean RGB before {[round(v, 1) for v in mb]}, after {[round(v, 1) for v in ma]}, "
          f"mean abs difference {[round(v, 2) for v in diff]} (of 255)")
