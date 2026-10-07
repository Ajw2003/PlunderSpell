"""Paint over a game screenshot in PlunderSpell's target look: painterly fill, ink outlines,
cross-hatching in the shadows, paper and film grain, graded to the art bible pigments.

Usage: python3 paintover.py <in.png> <out.png> [--crop x0,y0,x1,y1] [--scale 2] [--warm 0..1] [--cel]
Needs pillow, numpy, opencv-python-headless.
"""
import argparse
import numpy as np
import cv2

BONE_BLACK = np.array([0x14, 0x12, 0x0E]) / 255.0
VELLUM = np.array([0xDC, 0xD2, 0xBA]) / 255.0
UMBER = np.array([0x4A, 0x3A, 0x2A]) / 255.0  # mid-shadow tint between the two


def painterly(img):
    """Edge-preserving smoothing, then a directional brush smear so flat greybox reads as strokes."""
    out = img.copy()
    for _ in range(1):
        out = cv2.edgePreservingFilter(out, flags=cv2.RECURS_FILTER, sigma_s=40, sigma_r=0.35)
    h, w = out.shape[:2]
    rng = np.random.default_rng(7)
    # Stroke direction field: low-frequency noise angle, smeared with a short motion blur per band.
    noise = cv2.resize(rng.random((h // 64 + 2, w // 64 + 2)).astype(np.float32), (w, h), interpolation=cv2.INTER_CUBIC)
    result = np.zeros_like(out, dtype=np.float32)
    bands = 4
    for b in range(bands):
        ang = b * 180 / bands + 20
        k = np.zeros((9, 9), np.float32)
        k[4, :] = 1
        k = cv2.warpAffine(k, cv2.getRotationMatrix2D((4, 4), ang, 1), (9, 9))
        k /= k.sum()
        smear = cv2.filter2D(out.astype(np.float32), -1, k)
        weight = np.clip(1 - np.abs((noise * bands) - b - 0.5), 0, 1)[..., None]
        result += smear * weight
    total = sum(np.clip(1 - np.abs((noise * bands) - b - 0.5), 0, 1) for b in range(bands))[..., None]
    out = (result / np.maximum(total, 1e-3)).astype(np.uint8)
    # Bristle texture: fine streaky noise modulating value a little.
    streak = cv2.GaussianBlur(rng.normal(0, 1, (h, w)).astype(np.float32), (0, 0), sigmaX=6, sigmaY=0.6)
    out = np.clip(out.astype(np.float32) * (1 + 0.06 * streak[..., None] / streak.std()), 0, 255)
    return out.astype(np.uint8)


def grade(img, warm):
    """Tint values along bone black -> umber -> vellum while keeping the shot's own value structure,
    and let saturated lights (fire, gold, glow) keep their colour."""
    f = img.astype(np.float32) / 255.0
    f = f ** 0.8  # lift: the greybox captures are darker than a painting reads
    lum = (f @ np.array([0.114, 0.587, 0.299]))[..., None]  # BGR
    ramp = np.where(lum < 0.4,
                    BONE_BLACK[::-1] + (UMBER[::-1] - BONE_BLACK[::-1]) * (lum / 0.4),
                    UMBER[::-1] + (VELLUM[::-1] - UMBER[::-1]) * ((lum - 0.4) / 0.6))
    ramp = ramp * (lum / np.maximum(ramp @ np.array([0.114, 0.587, 0.299]), 1e-3)[..., None])  # keep value
    sat = (f.max(-1) - f.min(-1))[..., None]
    keep = np.clip(sat * 1.8, 0.4, 0.9)
    out = ramp * (1 - keep) + f * keep
    out = out * (1 - 0.3 * warm) + out * np.array([0.85, 0.97, 1.1]) * 0.3 * warm  # warm push, BGR
    return np.clip(out, 0, 1)


def hatch_layer(h, w, angle, spacing, rng):
    """Hand-drawn parallel lines: wobbly, varying weight, broken."""
    layer = np.zeros((h * 2, w * 2), np.float32)
    diag = int(np.hypot(h, w) * 2)
    for i in range(-diag, diag, spacing):
        off = i + rng.normal(0, spacing * 0.15)
        pts = []
        for t in np.linspace(-diag, diag, 24):
            pts.append((w + t, h + off + rng.normal(0, 1.2)))
        pts = np.array(pts, np.float32)
        c, s = np.cos(np.radians(angle)), np.sin(np.radians(angle))
        rot = (pts - [w, h]) @ np.array([[c, s], [-s, c]]) + [w, h]
        cv2.polylines(layer, [rot.astype(np.int32)], False, float(rng.uniform(0.6, 1.0)),
                      thickness=int(rng.choice([1, 1, 2])), lineType=cv2.LINE_AA)
    layer = layer[h // 2: h // 2 + h, w // 2: w // 2 + w]
    breaks = cv2.resize(rng.random((h // 18 + 2, w // 18 + 2)).astype(np.float32), (w, h))
    return layer * (breaks > 0.25)


def hatching(lum, rng):
    """Three tiers: single hatch in shade, cross-hatch in shadow, a third pass in the darkest."""
    h, w = lum.shape
    sp = max(5, w // 260)
    l1 = hatch_layer(h, w, 45, sp, rng)
    l2 = hatch_layer(h, w, -45, sp, rng)
    l3 = hatch_layer(h, w, 10, sp + 2, rng)
    # Thresholds from the shot's own value spread, so dark night scenes still keep lit areas clean.
    p8, p20, p38, p55 = np.percentile(lum, [8, 20, 38, 55])
    soft = lambda x, a, b: np.clip((b - x) / max(b - a, 1e-3), 0, 1)
    ink = l1 * soft(lum, p38, p55) + l2 * soft(lum, p20, p38) + l3 * soft(lum, p8, p20)
    return np.clip(ink, 0, 1)


def outlines(src, rng, cel=False):
    """Ink contour from colour and value edges, thickened and wobbled like a dip pen.
    cel: fewer interior lines, thinner, steadier - a clean contour rather than pen work."""
    g = cv2.cvtColor(src, cv2.COLOR_BGR2LAB)
    g = cv2.bilateralFilter(g, 9, 40, 9)
    e = np.zeros(src.shape[:2], np.uint8)
    for ch in range(3):
        e |= cv2.Canny(g[..., ch], *((30, 90) if cel else (12, 36)))
    h, w = e.shape
    dx = cv2.resize(rng.normal(0, 0.5 if cel else 1.4, (h // 40 + 2, w // 40 + 2)).astype(np.float32), (w, h))
    dy = cv2.resize(rng.normal(0, 0.5 if cel else 1.4, (h // 40 + 2, w // 40 + 2)).astype(np.float32), (w, h))
    mx, my = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
    e = cv2.remap(e, mx + dx, my + dy, cv2.INTER_LINEAR)
    thick = max(1, w // 1300) if cel else max(2, w // 700)
    e = cv2.dilate(e, np.ones((thick + 1, thick + 1), np.uint8))
    e = cv2.GaussianBlur(e.astype(np.float32) / 255, (0, 0), 0.8)
    pressure = cv2.resize(rng.random((h // 60 + 2, w // 60 + 2)).astype(np.float32), (w, h), interpolation=cv2.INTER_CUBIC)
    return np.clip(e * (0.75 + 0.5 * pressure), 0, 1)


def cel_flat(src, scale):
    """Flat colour per surface: mean-shift at capture resolution merges each face's gradient into one
    tone, so the steps follow geometry instead of light falloff."""
    small = cv2.resize(src, None, fx=1 / scale, fy=1 / scale, interpolation=cv2.INTER_AREA)
    flat = cv2.pyrMeanShiftFiltering(small, sp=14, sr=22, maxLevel=2)
    flat = cv2.resize(flat, (src.shape[1], src.shape[0]), interpolation=cv2.INTER_CUBIC)
    return cv2.edgePreservingFilter(flat, flags=cv2.RECURS_FILTER, sigma_s=30, sigma_r=0.15)


def cel_shadow(col):
    """One hard lit/shadow step (at the shot's 30th value percentile); shadow leans cool and darker."""
    lum = (col @ np.array([0.114, 0.587, 0.299])).astype(np.float32)
    cut = np.percentile(lum, 30)
    shadow = np.clip((cut - lum) / 0.01 + 0.5, 0, 1)[..., None]
    out = col * (1 - shadow) + col * np.array([1.05, 0.9, 0.78]) * 0.8 * shadow  # BGR
    return np.clip(out, 0, 1), lum


def paper(h, w, rng):
    """Multiplicative paper: blotchy low-frequency tone plus fibre streaks."""
    blot = sum(cv2.resize(rng.random((h // s + 2, w // s + 2)).astype(np.float32), (w, h), interpolation=cv2.INTER_CUBIC) / (i + 1)
               for i, s in enumerate((200, 60, 15)))
    blot = (blot - blot.mean()) / blot.std()
    fibre = cv2.GaussianBlur(rng.normal(0, 1, (h, w)).astype(np.float32), (0, 0), sigmaX=0.5, sigmaY=3)
    fibre /= fibre.std()
    return 1 + 0.045 * blot + 0.025 * fibre


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("dst")
    ap.add_argument("--crop")
    ap.add_argument("--scale", type=float, default=2.0)
    ap.add_argument("--warm", type=float, default=0.5)
    ap.add_argument("--seed", type=int, default=3)
    ap.add_argument("--cel", action="store_true", help="flat cel bands, light outlines, almost no hatching")
    a = ap.parse_args()
    rng = np.random.default_rng(a.seed)
    img = cv2.imread(a.src, cv2.IMREAD_COLOR)
    if img is None:
        raise SystemExit(f"could not read {a.src}")
    if a.crop:
        x0, y0, x1, y1 = map(int, a.crop.split(","))
        img = img[y0:y1, x0:x1]
    img = cv2.resize(img, None, fx=a.scale, fy=a.scale, interpolation=cv2.INTER_CUBIC)
    h, w = img.shape[:2]

    ink_lines = outlines(img, rng, a.cel)
    ink = BONE_BLACK[::-1]
    if a.cel:
        col, lum = cel_shadow(grade(cel_flat(img, a.scale), a.warm))
        hatch = hatching(lum, rng) * np.clip((np.percentile(lum, 10) - lum) / 0.03, 0, 1)  # deepest shadow only
        col = col * (1 - 0.25 * hatch[..., None]) + ink * 0.25 * hatch[..., None]
        col = col * (1 - 0.7 * ink_lines[..., None]) + ink * 0.7 * ink_lines[..., None]
    else:
        painted = painterly(img)
        col = grade(painted, a.warm)
        lum = col @ np.array([0.114, 0.587, 0.299])
        lum = cv2.GaussianBlur(lum.astype(np.float32), (0, 0), 3)
        hatch = hatching(lum, rng)
        col = col * (1 - 0.6 * hatch[..., None]) + ink * 0.6 * hatch[..., None]
        col = col * (1 - 0.9 * ink_lines[..., None]) + ink * 0.9 * ink_lines[..., None]
    col = col * paper(h, w, rng)[..., None]
    grain = cv2.GaussianBlur(rng.normal(0, 1, (h, w)).astype(np.float32), (0, 0), 0.7)
    col = col + 0.025 * grain[..., None] / grain.std()
    yy, xx = np.mgrid[0:h, 0:w]
    vig = 1 - 0.3 * (((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2) ** 1.4
    col = col * np.clip(vig, 0.55, 1)[..., None]
    cv2.imwrite(a.dst, np.clip(col * 255, 0, 255).astype(np.uint8))


if __name__ == "__main__":
    main()
