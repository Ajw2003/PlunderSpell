using UnityEngine;

namespace Plunderspell.Lair
{
    /// <summary>
    /// Turns a render of a piece into an ink sketch for the ledger's demand page (#419): a wobbling outline, inner
    /// edges, hatching where the piece is dark and a brown wash deepening with its shade, as if drawn and painted on the
    /// vellum. The result is meant to be multiplied over the page, so white leaves the paper as it is.
    /// </summary>
    public static class InkSketch
    {
        private static readonly Color Ink = new Color(0.20f, 0.12f, 0.06f);
        private const float HatchSpacing = 7f;
        private static readonly Color Wash = new Color(0.80f, 0.68f, 0.52f);

        /// <summary>
        /// <paramref name="render"/> is <paramref name="width"/> by <paramref name="height"/> pixels, bottom row first,
        /// with every pixel off the piece set to <paramref name="background"/>. Returns pixels in the same layout.
        /// </summary>
        public static Color32[] Draw(Color32[] render, int width, int height, Color32 background)
        {
            int count = width * height;
            var inside = new float[count];
            var light = new float[count];
            float darkest = 1f, lightest = 0f;
            for (int i = 0; i < count; i++)
            {
                Color32 c = render[i];
                if (Mathf.Abs(c.r - background.r) + Mathf.Abs(c.g - background.g) + Mathf.Abs(c.b - background.b) < 60)
                {
                    light[i] = 1f; // paper
                    continue;
                }
                inside[i] = 1f;
                light[i] = (0.30f * c.r + 0.59f * c.g + 0.11f * c.b) / 255f;
                darkest = Mathf.Min(darkest, light[i]);
                lightest = Mathf.Max(lightest, light[i]);
            }
            float range = Mathf.Max(lightest - darkest, 0.01f);

            var sketch = new Color32[count];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;

                // A hand never draws the edge where it is: read the edges a pixel or two off, wandering slowly.
                int wx = Mathf.Clamp(x + Mathf.RoundToInt(1.6f * Mathf.Sin(y * 0.043f + 1.3f)), 0, width - 1);
                int wy = Mathf.Clamp(y + Mathf.RoundToInt(1.6f * Mathf.Sin(x * 0.051f)), 0, height - 1);
                float outline = Mathf.Clamp01(Sobel(inside, wx, wy, width, height) * 0.6f);
                float innerEdge = Mathf.Clamp01((Sobel(light, wx, wy, width, height) - 0.30f) * 2.5f) * inside[i];

                float tone = inside[i] * (1f - (light[i] - darkest) / range); // 0 light, 1 dark
                float hatch = 0f;
                if (tone > 0.55f && Stroke(x + y, y))
                    hatch = 0.45f;
                if (tone > 0.80f && Stroke(x - y + height, x))
                    hatch = 0.60f;

                float ink = Mathf.Max(outline, Mathf.Max(innerEdge * 0.8f, hatch)) * Grain(x, y);

                // A brush of thinned ink over the piece, deeper where it is dark: the painted half of the drawing.
                Color wash = Color.Lerp(Color.white, Wash, inside[i] * (0.25f + 0.75f * tone));
                sketch[i] = wash * Color.Lerp(Color.white, Ink, ink);
            }
            return sketch;
        }

        // One diagonal stroke every HatchSpacing pixels, about 1.5 pixels wide, with a slight waver along it.
        private static bool Stroke(int along, int across)
        {
            float at = along + 1.2f * Mathf.Sin(across * 0.09f);
            float into = at - HatchSpacing * Mathf.Floor(at / HatchSpacing);
            return into < 1.5f;
        }

        // Ink takes unevenly on vellum: a fixed per-pixel speckle between 0.7 and 1.
        private static float Grain(int x, int y)
        {
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            return 0.7f + 0.3f * ((h & 0xffff) / 65535f);
        }

        private static float Sobel(float[] values, int x, int y, int width, int height)
        {
            float At(int dx, int dy) => values[Mathf.Clamp(y + dy, 0, height - 1) * width + Mathf.Clamp(x + dx, 0, width - 1)];
            float gx = At(1, -1) + 2f * At(1, 0) + At(1, 1) - At(-1, -1) - 2f * At(-1, 0) - At(-1, 1);
            float gy = At(-1, 1) + 2f * At(0, 1) + At(1, 1) - At(-1, -1) - 2f * At(0, -1) - At(1, -1);
            return Mathf.Sqrt(gx * gx + gy * gy);
        }
    }
}
