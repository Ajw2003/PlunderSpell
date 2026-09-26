"""
The four greyscale detail textures the castle's surface shader projects over
the flat palette colours (docs/plans/night-atmosphere.md, section 3), packed
into the channels of one tiling 512x512 texture:

    R  stone: block coursing with mortar joints and chisel noise
    G  wood:  planks with grain
    B  iron:  hammered pitting
    A  cloth: a soft weave

Each channel averages about 0.5, so the shader can multiply albedo by
2 * detail without brightening or darkening a surface overall. Every pattern
tiles: noise is built on a periodic lattice and blocks wrap at the edges.
The texture covers 2 x 2 m of world, matching the shader's default scale.

Run from the repo root:
    python Tools/AssetPipeline/make_detail_textures.py
Writes Assets/_Project/Art/Textures/SurfaceDetail.png. Deterministic: the
same seed writes the same bytes.
"""
import os

import numpy as np
from PIL import Image

SIZE = 512
SEED = 20260925
REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(REPO, "Assets", "_Project", "Art", "Textures", "SurfaceDetail.png")


def periodic_noise(rng, cells, size=SIZE):
    """Value noise on a cells x cells lattice that wraps, smoothstep-interpolated."""
    lattice = rng.random((cells, cells))
    coords = np.arange(size) * cells / size
    i0 = np.floor(coords).astype(int)
    f = coords - i0
    f = f * f * (3 - 2 * f)
    i1 = (i0 + 1) % cells
    i0 %= cells
    fx, fy = np.meshgrid(f, f)
    x0, y0 = np.meshgrid(i0, i0)
    x1, y1 = np.meshgrid(i1, i1)
    a = lattice[y0, x0]
    b = lattice[y0, x1]
    c = lattice[y1, x0]
    d = lattice[y1, x1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(rng, base_cells, octaves, size=SIZE):
    total = np.zeros((size, size))
    amplitude, weight = 1.0, 0.0
    for o in range(octaves):
        total += periodic_noise(rng, base_cells * (2 ** o), size) * amplitude
        weight += amplitude
        amplitude *= 0.5
    return total / weight


def normalise(channel, mean=0.5, spread=0.22):
    channel = channel - channel.mean()
    channel = channel / (channel.std() + 1e-6) * spread
    return np.clip(channel + mean, 0.0, 1.0)


def stone(rng):
    """Ashlar courses: 4 rows of blocks over 2 m, every other row offset by half a block."""
    y, x = np.mgrid[0:SIZE, 0:SIZE] / SIZE
    rows = 4
    row = np.floor(y * rows).astype(int)
    blocks_per_row = 3
    shifted = (x + (row % 2) * 0.5 / blocks_per_row) % 1.0
    col = np.floor(shifted * blocks_per_row).astype(int)
    # Distance to the nearest joint, in texture units.
    fy = y * rows - row
    fx = shifted * blocks_per_row - col
    joint = np.minimum(np.minimum(fy, 1 - fy) / rows, np.minimum(fx, 1 - fx) / blocks_per_row)
    mortar = np.clip(joint / 0.006, 0, 1)
    bevel = np.clip(joint / 0.02, 0, 1)
    # Each block its own shade.
    shade = rng.random((rows, blocks_per_row))[row % rows, col % blocks_per_row]
    chisel = fbm(rng, 16, 4)
    value = 0.55 + (shade - 0.5) * 0.22 + (chisel - 0.5) * 0.4
    value = value * (0.55 + 0.45 * bevel)
    value = value * mortar + 0.22 * (1 - mortar)
    return normalise(value, spread=0.2)


def wood(rng):
    """Vertical boards 0.25 m wide with streaked grain and a dark seam between them."""
    y, x = np.mgrid[0:SIZE, 0:SIZE] / SIZE
    boards = 8
    board = np.floor(x * boards).astype(int)
    fx = x * boards - board
    seam = np.clip(np.minimum(fx, 1 - fx) / 0.04, 0, 1)
    streak = periodic_noise(rng, 64, SIZE)
    grain = np.sin((x * 36 + fbm(rng, 4, 3) * 3) * np.pi) * 0.5 + 0.5
    tone = rng.random(boards)[board % boards]
    stretched = np.repeat(streak[:, ::8], 8, axis=1)[:, :SIZE]
    value = 0.5 + (tone - 0.5) * 0.3 + (grain - 0.5) * 0.15 + (stretched - 0.5) * 0.12
    value = value * (0.35 + 0.65 * seam)
    return normalise(value, spread=0.18)


def iron(rng):
    """Hammer marks: shallow round dimples scattered over fine noise."""
    y, x = np.mgrid[0:SIZE, 0:SIZE] / SIZE
    value = fbm(rng, 24, 3) * 0.5 + 0.25
    for _ in range(60):
        cx, cy, r = rng.random(), rng.random(), 0.01 + rng.random() * 0.025
        dx = np.abs(x - cx)
        dx = np.minimum(dx, 1 - dx)
        dy = np.abs(y - cy)
        dy = np.minimum(dy, 1 - dy)
        d = np.sqrt(dx * dx + dy * dy) / r
        value -= np.clip(1 - d, 0, 1) ** 2 * 0.15
        value += np.clip(1 - np.abs(d - 1) * 6, 0, 1) * 0.03
    return normalise(value, spread=0.16)


def cloth(rng):
    """A plain weave: threads over and under, softened."""
    y, x = np.mgrid[0:SIZE, 0:SIZE] / SIZE
    threads = 64
    warp = np.sin(x * threads * 2 * np.pi) * 0.5 + 0.5
    weft = np.sin(y * threads * 2 * np.pi) * 0.5 + 0.5
    over = ((np.floor(x * threads) + np.floor(y * threads)) % 2).astype(float)
    value = over * warp + (1 - over) * weft
    value = value * 0.6 + fbm(rng, 8, 3) * 0.4
    return normalise(value, spread=0.14)


def main():
    rng = np.random.default_rng(SEED)
    channels = [stone(rng), wood(rng), iron(rng), cloth(rng)]
    rgba = np.stack(channels, axis=-1)
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    Image.fromarray((rgba * 255).round().astype(np.uint8), "RGBA").save(OUT, optimize=True)
    print(f"wrote {OUT}: means " + ", ".join(f"{c.mean():.3f}" for c in channels))


if __name__ == "__main__":
    main()
