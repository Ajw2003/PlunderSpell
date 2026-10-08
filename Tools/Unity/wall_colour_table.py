"""Mean wall colour and luminance, art vs in-game, per Age (#268 round 4).
Usage: python Tools/Unity/wall_colour_table.py <ages-check dir> [overlay out dir]
Boxes are fractions (x0, y0, x1, y1) of the image. Game boxes avoid the HUD and the flame."""
import os, sys
from PIL import Image, ImageDraw
con = "docs/art/concept/painted"
P = {"BronzeAge": "bronze", "HighMedieval": "high", "LateMedieval": "late", "AgeOfPowder": "powder"}
# art: image -> (lit boxes, shadow boxes)
ART = {
 "bronze-court": ([(0.40,0.30,0.55,0.42),(0.62,0.30,0.70,0.40)], [(0.0,0.33,0.07,0.50),(0.72,0.28,0.80,0.36)]),
 "bronze-hall":  ([(0.56,0.30,0.66,0.42),(0.05,0.12,0.12,0.30)], [(0.0,0.30,0.04,0.50),(0.40,0.12,0.60,0.18)]),
 "high-court":   ([(0.15,0.30,0.27,0.45),(0.78,0.30,0.88,0.45)], [(0.62,0.28,0.70,0.40),(0.40,0.55,0.48,0.62)]),
 "high-hall":    ([(0.20,0.25,0.30,0.40),(0.70,0.25,0.80,0.40)], [(0.30,0.05,0.40,0.15),(0.0,0.1,0.05,0.35)]),
 "late-court":   ([(0.30,0.30,0.40,0.45),(0.58,0.40,0.66,0.52)], [(0.0,0.35,0.05,0.5),(0.80,0.55,0.95,0.65)]),
 "late-hall":    ([(0.45,0.25,0.55,0.35),(0.20,0.40,0.28,0.50)], [(0.0,0.2,0.04,0.4),(0.60,0.05,0.80,0.12)]),
 "powder-court": ([(0.15,0.25,0.30,0.35),(0.60,0.25,0.75,0.35)], [(0.35,0.12,0.45,0.18),(0.80,0.35,0.95,0.45)]),
 "powder-hall":  ([(0.65,0.12,0.80,0.25),(0.80,0.45,0.95,0.55)], [(0.0,0.05,0.12,0.2),(0.45,0.05,0.6,0.12)]),
}
GAME = {  # view -> (lit, shadow)
 "ground-ward": ([(0.64,0.13,0.70,0.30),(0.55,0.28,0.62,0.36)], [(0.08,0.30,0.30,0.55),(0.40,0.55,0.60,0.62)]),
 "keep-corner": ([(0.60,0.35,0.70,0.50),(0.78,0.28,0.85,0.40)], [(0.05,0.35,0.30,0.55),(0.40,0.50,0.55,0.60)]),
}
def stat(im, boxes, draw=None):
    W, H = im.size; px = []
    for b in boxes:
        c = im.crop((int(b[0]*W), int(b[1]*H), int(b[2]*W), int(b[3]*H))).resize((1, 1), Image.BOX).getpixel((0, 0))
        px.append(c)
        if draw: draw.rectangle((b[0]*W, b[1]*H, b[2]*W, b[3]*H), outline=(0, 255, 0))
    m = [sum(p[i] for p in px)/len(px) for i in range(3)]
    return m, 0.2126*m[0]+0.7152*m[1]+0.0722*m[2]
def fmt(s): return "%3d,%3d,%3d L%3d" % (*s[0], s[1])
def main():
    src = sys.argv[1]; ov = sys.argv[2] if len(sys.argv) > 2 else None
    print("%-13s | %-19s | %-19s | %-19s | %-19s" % ("Age", "lit art", "lit game", "shadow art", "shadow game"))
    for age, p in P.items():
        a, g = {"l": [], "s": []}, {"l": [], "s": []}
        for kind in ("court", "hall"):
            n = p + "-" + kind; im = Image.open(f"{con}/{n}-v2.png").convert("RGB"); d = ImageDraw.Draw(im)
            a["l"].append(stat(im, ART[n][0], d)); a["s"].append(stat(im, ART[n][1], d))
            if ov: im.resize((672, 384)).save(f"{ov}/box-{n}.png")
        for v in GAME:
            im = Image.open(f"{src}/{age}/{v}.png").convert("RGB"); d = ImageDraw.Draw(im)
            g["l"].append(stat(im, GAME[v][0], d)); g["s"].append(stat(im, GAME[v][1], d))
            if ov: im.save(f"{ov}/box-{age}-{v}.png")
        avg = lambda L: ([sum(x[0][i] for x in L)/len(L) for i in range(3)], sum(x[1] for x in L)/len(L))
        print("%-13s | %s | %s | %s | %s" % (age, fmt(avg(a["l"])), fmt(avg(g["l"])), fmt(avg(a["s"])), fmt(avg(g["s"]))))
main()
