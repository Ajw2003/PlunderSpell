from lib import *

LINEN, HIDE, LEATHER, BRONZE, ASH, SKIN = "#CFC3A2", "#B79B78", "#6B4A2E", "#9B6A38", "#8A6A48", "#9C6E4E"
HIDE_DARK = "#4A3526"  # dapple patches (from the oxhide note)
SOOT = "#2B231B"


def spear(sh, F, x, top=2.40):
    # shaft
    F.limb((x, 0.02), (x, top - 0.26), 0.035, 0.032, ASH, light=0.3)
    F.limb((x, 1.00), (x, 1.25), 0.045, 0.045, LINEN)            # grip wrap
    for k in range(6):
        F.line((x - 0.022, 1.02 + k * 0.04), (x + 0.022, 1.05 + k * 0.04), darken(LINEN, 0.4), 0.8)
    F.limb((x, -0.0), (x, 0.08), 0.012, 0.03, BRONZE, cap=False)  # butt-spike
    # leaf blade
    blade = [(x, top), (x + 0.028, top - 0.09), (x + 0.024, top - 0.18), (x + 0.012, top - 0.22),
             (x - 0.012, top - 0.22), (x - 0.024, top - 0.18), (x - 0.028, top - 0.09)]
    d = F.shape(blade, BRONZE, light=0.4)
    F.line((x, top - 0.01), (x, top - 0.22), lighten(BRONZE, 0.45), 1.2)       # midrib
    F.line((x - 0.024, top - 0.09), (x - 0.02, top - 0.18), lighten(BRONZE, 0.6), 0.8, op=.8)  # edge polish
    F.limb((x, top - 0.22), (x, top - 0.29), 0.022, 0.03, BRONZE, cap=False)   # socket
    F.line((x - 0.015, top - 0.25), (x + 0.015, top - 0.25), darken(BRONZE, 0.5), 0.8)


def levy_front(sh, cx=470):
    F = Fig(sh, cx)
    # hair behind head
    F.shape([(-0.085, 1.60), (-0.09, 1.50), (-0.075, 1.43), (0.075, 1.43), (0.09, 1.50), (0.085, 1.60), (0, 1.62)],
            "#3A2A1E", flat=True)
    # legs
    for s in (-1, 1):
        F.limb((0.085 * s, 0.62), (0.08 * s, 0.09), 0.105, 0.062, SKIN)
        F.curve([(0.05 * s, 0.49), (0.08 * s, 0.505), (0.11 * s, 0.49)], darken(SKIN, 0.35), 1.2, op=.7)  # knee
        F.ell(0.09 * s, 0.03, 0.058, 0.03, LEATHER)                  # sandal/foot
        for k in range(3):
            F.line((0.05 * s, 0.05 + k * 0.035), (0.11 * s, 0.07 + k * 0.035), LEATHER, 1.3)
        # dust on shins
        sh.flecks(smooth_path(F.pts([(0.03 * s, 0.35), (0.13 * s, 0.35), (0.12 * s, 0.05), (0.04 * s, 0.05)])),
                  F.bbox([(0.03 * s, 0.35), (0.13 * s, 0.05)]), 18, lighten(ASH, 0.3), op=.45)
    # left arm (viewer right) upper arm, behind shield
    F.limb((0.235, 1.25), (0.285, 1.07), 0.085, 0.075, SKIN)
    # right arm (viewer left)
    F.limb((-0.235, 1.25), (-0.275, 1.08), 0.085, 0.075, SKIN)
    F.limb((-0.275, 1.08), (-0.30, 0.96), 0.072, 0.06, SKIN)
    # tunic
    tun = [(-0.09, 1.43), (-0.20, 1.395), (-0.255, 1.33), (-0.265, 1.22), (-0.215, 1.20), (-0.19, 1.10), (-0.175, 1.00),
           (-0.20, 0.80), (-0.235, 0.58), (-0.12, 0.56), (0, 0.575), (0.12, 0.56), (0.235, 0.58), (0.20, 0.80),
           (0.175, 1.00), (0.19, 1.10), (0.215, 1.20), (0.265, 1.22), (0.255, 1.33), (0.20, 1.395), (0.09, 1.43), (0, 1.41)]
    d = F.shape(tun, LINEN, light=0.2, dark=0.5)
    bb = F.bbox(tun)
    # folds
    for x0, x1 in ((-0.12, -0.15), (-0.04, -0.05), (0.05, 0.07), (0.13, 0.16)):
        F.curve([(x0, 0.97), (x0 + (x1 - x0) * .4, 0.8), (x1, 0.6)], darken(LINEN, 0.3), 1.4, op=.6)
    F.curve([(-0.17, 1.34), (-0.10, 1.25), (-0.02, 1.22)], darken(LINEN, 0.3), 1.2, op=.5)
    F.curve([(0.17, 1.34), (0.10, 1.25), (0.02, 1.22)], darken(LINEN, 0.3), 1.2, op=.5)
    F.curve([(-0.21, 1.37), (-0.24, 1.26)], lighten(LINEN, 0.5), 1.6, op=.7)   # rim light
    # red hem stripe + armpit sweat + soot on hem
    sh.clipped(d, f'<rect x="{bb[0]}" y="{G - 0.615 * S:.1f}" width="{bb[2] - bb[0]:.1f}" height="{0.03 * S:.1f}" fill="#8E3F2C" opacity=".85"/>')
    sh.flecks(d, (bb[0], G - 0.66 * S, bb[2], bb[3]), 40, SOOT, 0.6, 1.6, .5)
    sh.flecks(d, (bb[0], bb[1], bb[2], G - 1.2 * S), 20, darken(LINEN, 0.35), 1, 3, .25)
    # belt
    belt = [(-0.18, 0.985), (0.18, 0.985), (0.182, 1.035), (-0.182, 1.035)]
    F.shape(belt, LEATHER, smooth=False)
    F.shape([(-0.02, 0.98), (0.02, 0.98), (0.02, 1.04), (-0.02, 1.04)], BRONZE, smooth=False)
    # water skin (viewer left hip) and bread bag
    F.line((-0.16, 1.02), (-0.2, 0.95), LEATHER, 1.5)
    F.ell(-0.215, 0.88, 0.055, 0.08, LEATHER, light=0.35)
    F.ell(-0.215, 0.965, 0.018, 0.015, darken(LEATHER, 0.3))
    # dagger scabbard (viewer right hip) on baldric
    F.curve([(-0.17, 1.39), (0.0, 1.20), (0.15, 1.04)], LEATHER, 2.4)
    F.shape([(0.14, 1.03), (0.17, 1.03), (0.20, 0.70), (0.185, 0.67), (0.165, 0.70)], LEATHER, smooth=False)
    F.ell(0.145, 1.06, 0.022, 0.022, "#E0D6BE")
    # neck + head
    F.limb((0, 1.40), (0, 1.48), 0.09, 0.085, SKIN)
    hx, hy = F.ell(0, 1.555, 0.078, 0.105, SKIN, light=0.3)
    # beard + face
    F.shape([(-0.075, 1.55), (-0.06, 1.48), (0, 1.445), (0.06, 1.48), (0.075, 1.55), (0.05, 1.51), (0, 1.50), (-0.05, 1.51)],
            "#3A2A1E", flat=True)
    for s in (-1, 1):
        F.ell(0.032 * s, 1.565, 0.012, 0.006, "#1E1A16")
        F.curve([(0.05 * s, 1.585), (0.03 * s, 1.592), (0.012 * s, 1.586)], "#3A2A1E", 1.6)
        F.ell(0.082 * s, 1.555, 0.012, 0.025, SKIN)
    F.curve([(0.0, 1.58), (0.008, 1.535), (-0.006, 1.525)], darken(SKIN, 0.4), 1.1)
    F.curve([(-0.02, 1.507), (0.0, 1.503), (0.02, 1.507)], darken(SKIN, 0.55), 1.2)
    # cap
    cap = [(-0.095, 1.585), (-0.09, 1.635), (-0.062, 1.675), (0, 1.688), (0.062, 1.675), (0.09, 1.635), (0.095, 1.585)]
    dcap = F.shape(cap, LEATHER, light=0.3)
    for x in (-0.05, 0, 0.05):
        F.curve([(x * 1.8, 1.59), (x * 1.2, 1.65), (x * 0.3, 1.685)], darken(LEATHER, 0.45), 1.1)
    F.shape([(-0.1, 1.575), (0.1, 1.575), (0.102, 1.6), (-0.102, 1.6)], darken(LEATHER, 0.1), light=0.3)
    F.ell(0, 1.69, 0.016, 0.01, BRONZE, light=0.5)
    F.curve([(-0.09, 1.58), (-0.07, 1.47), (0, 1.445)], LEATHER, 1.1, op=.8)  # chin strap
    sh.flecks(dcap, F.bbox(cap), 12, lighten(LEATHER, 0.4), 0.5, 1.2, .5)
    # hand gripping spear
    spear(sh, F, -0.30)
    F.ell(-0.30, 0.965, 0.036, 0.048, SKIN, light=0.35)
    F.line((-0.325, 0.975), (-0.275, 0.985), darken(SKIN, 0.4), 0.9)
    # shield (held at left side, turned ~55°, foreshortened)
    sx = 0.36
    sp = [(0, 1.625), (0.10, 1.60), (0.17, 1.50), (0.19, 1.34), (0.16, 1.14), (0.115, 1.00), (0.16, 0.86), (0.2, 0.66),
          (0.18, 0.46), (0.10, 0.35), (0, 0.33), (-0.10, 0.35), (-0.17, 0.46), (-0.18, 0.66), (-0.14, 0.86), (-0.10, 1.00),
          (-0.14, 1.14), (-0.17, 1.34), (-0.15, 1.50), (-0.09, 1.60)]
    sp = [(sx + x, h) for x, h in sp]
    ds = F.shape(sp, HIDE, direction="h", light=0.3, dark=0.5, sw=1.6)
    bbs = F.bbox(sp)
    # dapple patches
    patches = []
    rng = sh.rng
    for i in range(9):
        px = rng.uniform(bbs[0], bbs[2]); py = rng.uniform(bbs[1], bbs[3])
        rx = rng.uniform(10, 24); ry = rng.uniform(8, 18)
        pts = [(px + rx * math.cos(a) * rng.uniform(.7, 1.1), py + ry * math.sin(a) * rng.uniform(.7, 1.1))
               for a in [k * math.pi / 4 for k in range(8)]]
        patches.append(f'<path d="{smooth_path(pts)}" fill="{HIDE_DARK}" opacity=".85"/>')
    sh.clipped(ds, "".join(patches))
    # curvature shading: dark band on the far edge
    sh.clipped(ds, f'<rect x="{bbs[2] - 22:.1f}" y="{bbs[1]:.1f}" width="30" height="{bbs[3] - bbs[1]:.1f}" fill="{darken(HIDE, .7)}" opacity=".45"/>')
    # spine and bowed side-bars
    F.curve([(sx - 0.01, 1.60), (sx - 0.02, 1.00), (sx - 0.01, 0.36)], darken(HIDE, 0.55), 2.4)
    F.curve([(sx - 0.012, 1.60), (sx - 0.022, 1.00), (sx - 0.012, 0.36)], lighten(HIDE, 0.4), 0.9, op=.7)
    for k in (1.12, 0.88):
        F.curve([(sx - 0.14, k), (sx - 0.02, k + 0.02), (sx + 0.14, k)], darken(HIDE, 0.45), 1.6)
    # stitched rim
    F.curve(sp + [sp[0]], lighten(HIDE, 0.5), 1.0, op=.55)
    sh.flecks(ds, bbs, 60, lighten(HIDE, 0.55), 0.5, 1.4, .5)
    # telamon strap over shoulder
    F.curve([(0.20, 1.39), (0.27, 1.45), (0.33, 1.58)], LEATHER, 3)


def levy_side(sh, cx=820):
    F = Fig(sh, cx)
    # far leg
    F.limb((-0.03, 0.62), (-0.07, 0.09), 0.10, 0.062, darken(SKIN, 0.25))
    F.shape([(-0.11, 0.0), (0.07, 0.0), (0.075, 0.03), (-0.03, 0.07), (-0.11, 0.05)], darken(LEATHER, 0.2))
    # spear far? no, near hand
    # far arm hidden by shield; shield (far side, forward)
    sp = [(0.13, 1.625), (0.24, 1.52), (0.30, 1.32), (0.27, 1.08), (0.245, 0.98), (0.29, 0.80), (0.285, 0.55),
          (0.21, 0.38), (0.13, 0.33), (0.16, 0.40), (0.195, 0.60), (0.20, 0.85), (0.17, 0.98), (0.185, 1.15),
          (0.19, 1.40), (0.155, 1.58)]
    # near leg
    F.limb((0.03, 0.62), (0.045, 0.09), 0.105, 0.062, SKIN)
    F.curve([(0.075, 0.52), (0.09, 0.47), (0.08, 0.43)], darken(SKIN, 0.35), 1.2, op=.7)
    F.shape([(-0.03, 0.0), (0.16, 0.0), (0.165, 0.03), (0.06, 0.075), (-0.03, 0.06)], LEATHER)
    for k in range(3):
        F.line((0.02, 0.05 + k * 0.035), (0.075, 0.07 + k * 0.035), darken(LEATHER, 0.3), 1.3)
    sh.flecks(smooth_path(F.pts([(0.0, 0.35), (0.09, 0.35), (0.08, 0.05), (0.01, 0.05)])),
              F.bbox([(0.0, 0.35), (0.09, 0.05)]), 14, lighten(ASH, 0.3), op=.45)
    # hair ponytail
    F.shape([(-0.07, 1.60), (-0.11, 1.52), (-0.10, 1.42), (-0.075, 1.40), (-0.06, 1.50)], "#3A2A1E", flat=True)
    # tunic profile
    tun = [(-0.03, 1.43), (-0.11, 1.39), (-0.125, 1.30), (-0.11, 1.10), (-0.10, 1.00), (-0.13, 0.85), (-0.15, 0.60),
           (-0.03, 0.565), (0.10, 0.575), (0.15, 0.60), (0.12, 0.85), (0.09, 1.00), (0.11, 1.15), (0.13, 1.27),
           (0.10, 1.38), (0.04, 1.43)]
    d = F.shape(tun, LINEN, direction="hr", light=0.2, dark=0.5)
    bb = F.bbox(tun)
    sh.clipped(d, f'<rect x="{bb[0]}" y="{G - 0.615 * S:.1f}" width="{bb[2] - bb[0]:.1f}" height="{0.03 * S:.1f}" fill="#8E3F2C" opacity=".85"/>')
    sh.flecks(d, (bb[0], G - 0.66 * S, bb[2], bb[3]), 30, SOOT, 0.6, 1.6, .5)
    for x0 in (-0.08, 0.0, 0.07):
        F.curve([(x0, 0.97), (x0 - 0.01, 0.8), (x0 - 0.03, 0.6)], darken(LINEN, 0.3), 1.3, op=.6)
    F.shape([(-0.105, 0.985), (0.095, 0.985), (0.097, 1.035), (-0.107, 1.035)], LEATHER, smooth=False)
    # water skin at near hip
    F.ell(-0.02, 0.88, 0.06, 0.085, LEATHER, light=0.35)
    F.line((0.0, 1.0), (-0.02, 0.96), LEATHER, 1.5)
    # shield over front of body
    ds = F.shape(sp, HIDE, direction="hr", light=0.35, dark=0.55, sw=1.6)
    bbs = F.bbox(sp)
    sh.flecks(ds, bbs, 25, HIDE_DARK, 2, 6, .7)
    F.curve([(0.155, 1.60), (0.27, 1.32), (0.25, 0.98), (0.27, 0.62), (0.16, 0.36)], darken(HIDE, 0.5), 1.4, op=.7)
    # neck, head
    F.limb((-0.01, 1.40), (0.0, 1.48), 0.095, 0.09, SKIN)
    F.ell(0.01, 1.555, 0.092, 0.105, SKIN, light=0.3)
    F.shape([(0.098, 1.57), (0.118, 1.535), (0.10, 1.525)], SKIN, smooth=False)        # nose
    F.shape([(-0.02, 1.54), (0.02, 1.49), (0.07, 1.45), (0.1, 1.48), (0.09, 1.51), (0.05, 1.53)], "#3A2A1E", flat=True)
    F.ell(0.06, 1.567, 0.01, 0.006, "#1E1A16")
    F.curve([(0.04, 1.585), (0.07, 1.59), (0.09, 1.583)], "#3A2A1E", 1.5)
    F.ell(-0.01, 1.55, 0.016, 0.028, SKIN, light=0.4)
    cap = [(-0.10, 1.585), (-0.095, 1.64), (-0.05, 1.68), (0.02, 1.688), (0.08, 1.665), (0.105, 1.62), (0.10, 1.585)]
    dcap = F.shape(cap, LEATHER, light=0.3)
    F.shape([(-0.105, 1.575), (0.108, 1.575), (0.11, 1.6), (-0.107, 1.6)], darken(LEATHER, 0.1), light=0.3)
    for x in (-0.05, 0.03):
        F.curve([(x, 1.59), (x + 0.005, 1.65), (x + 0.01, 1.684)], darken(LEATHER, 0.45), 1.1)
    F.ell(0.005, 1.69, 0.016, 0.01, BRONZE, light=0.5)
    F.curve([(-0.02, 1.58), (0.0, 1.50), (0.04, 1.45)], LEATHER, 1.1, op=.8)
    # telamon strap across chest
    F.curve([(-0.08, 1.40), (0.03, 1.25), (0.16, 1.10)], LEATHER, 3)
    # near arm + spear
    spear(sh, F, 0.14)
    F.limb((-0.005, 1.35), (0.02, 1.10), 0.095, 0.08, SKIN)
    F.shape([(-0.075, 1.36), (-0.04, 1.41), (0.03, 1.405), (0.06, 1.33), (0.05, 1.24), (-0.01, 1.22), (-0.06, 1.25)], LINEN, light=0.2)        # sleeve
    F.limb((0.02, 1.10), (0.12, 0.98), 0.075, 0.062, SKIN)
    F.ell(0.14, 0.975, 0.04, 0.045, SKIN, light=0.35)
    # dagger scabbard at back hip
    F.shape([(-0.10, 1.03), (-0.07, 1.03), (-0.2, 0.72), (-0.22, 0.73)], LEATHER, smooth=False)


def build():
    sh = Sheet("enemy", "THE BRONZE AGE · ENEMY · PATROL", "Palace Levy", "H 1.70 m · ≤ 8k tris · 2048²",
               "1 m = 220 px · ground y 690",
               [("linen", LINEN), ("oxhide", HIDE), ("leather", LEATHER), ("bronze", BRONZE), ("ash haft", ASH),
                ("skin", SKIN)], seed=11)
    enemy_furniture(sh)
    height_mark(sh, 1.70, x1=880, label="1.70 m (cap)")
    height_mark(sh, 2.40, x1=880, label="spear tip 2.40 m")
    # soft ground shadows
    for cx in (470, 820):
        sh.ellipse(cx + 20, 690, 90, 7, "#0E0C09", op=.55)
    levy_front(sh)
    levy_side(sh)
    sh.callouts([
        (820 + 0.07 * S, G - 1.64 * S, "LEATHER CAP", "6 gores · bronze boss knob"),
        (820 + 0.14 * S, G - 2.30 * S, "SPEAR HEAD", "cast bronze leaf, 0.26 m"),
        (820 + 0.28 * S, G - 1.30 * S, "FIGURE-8 SHIELD", "oxhide on wicker, 1.30 m"),
        (820 + 0.14 * S, G - 1.12 * S, "GRIP WRAP", "linen on ash haft, 2.40 m"),
        (820 - 0.10 * S, G - 1.25 * S, "LINEN TUNIC", "knee length, short sleeve"),
        (820 - 0.06 * S, G - 0.88 * S, "BELT + WATER SKIN", "boiled leather, right hip"),
        (820 - 0.12 * S, G - 0.60 * S, "HEM STRIPE", "woven red border, soot"),
        (820 + 0.10 * S, G - 0.03 * S, "RAWHIDE SANDALS", "ankle thongs · dust to 0.3 m")], 968, 170, 640)
    # front-only callout in the left gap
    sh.callout(470 + 0.40 * S, G - 0.62 * S, 600, 650, "HIDE DAPPLE", "hand-painted patches", elbow=False)
    return sh
