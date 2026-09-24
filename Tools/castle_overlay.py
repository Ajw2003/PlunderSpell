"""Draws CastleAudit's floor probes over an overhead castle render.

Usage: python Tools/castle_overlay.py <probes.txt> <overhead.png> <out.png>
probes.txt: one "x,z,walkable,reached" line per probe (written by the audit run).
Green = floor you can walk to from the spawn, red = floor you cannot, grey = no floor (blocked by a prop).
"""
import sys
from PIL import Image, ImageDraw

probes_path, overhead_path, out_path = sys.argv[1:4]
S = 1400
scale = S / 124.0
ox, oz = 6 - 62, -62

def to_px(x, z):
    return ((x - ox) * scale, S - (z - oz) * scale)

im = Image.open(overhead_path).convert('RGB').resize((S, S))
draw = ImageDraw.Draw(im)
for line in open(probes_path):
    x, z, walkable, reached = line.strip().split(',')
    px, py = to_px(float(x), float(z))
    colour = (40, 220, 60) if reached == '1' else ((230, 40, 40) if walkable == '1' else (140, 140, 140))
    draw.ellipse([px - 7, py - 7, px + 7, py + 7], fill=colour, outline=(0, 0, 0))
im.save(out_path)
print('drew', out_path)
