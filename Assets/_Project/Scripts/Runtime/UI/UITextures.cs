using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>
    /// The few textures the UI needs, generated on first use and cached, so the redesign ships no
    /// image assets. Everything is white-on-transparent unless it carries its own colour, so a
    /// Graphic's colour tints it.
    /// </summary>
    public static class UITextures
    {
        private struct Glow
        {
            public float X;
            public float Y;
            public float RadiusX;
            public float RadiusY;
            public Color Colour;
            public float Alpha;
        }

        private const int BackdropWidth = 480;
        private const int BackdropHeight = 270;
        private const int SigilSize = 300;

        // The sigil is drawn in the mockup's 100-unit view box; this is pixels per unit.
        private const float SigilScale = SigilSize / 100f;

        private static Texture2D _white;
        private static Texture2D _backdrop;
        private static Texture2D _backdropDefeat;
        private static Texture2D _fade;
        private static Texture2D _sigil;
        private static readonly Dictionary<long, Texture2D> Rings = new Dictionary<long, Texture2D>();

        public static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = Create(1, 1, FilterMode.Point);
                    _white.SetPixels32(new[] { new Color32(255, 255, 255, 255) });
                    _white.Apply();
                }
                return _white;
            }
        }

        /// <summary>
        /// Bone-black with a warm orpiment glow top-left, a faint verdigris glow top-right and a faint
        /// madder glow along the bottom: the mockup's .s-ground, the lair at 2 am.
        /// </summary>
        public static Texture2D Backdrop
        {
            get
            {
                if (_backdrop == null)
                {
                    _backdrop = BuildBackdrop(
                        // Listed bottom layer first, as the CSS stacks them last to first.
                        new Glow { X = 0.50f, Y = 1.20f, RadiusX = 0.70f, RadiusY = 0.60f, Colour = UITheme.Madder, Alpha = 0.07f },
                        new Glow { X = 0.88f, Y = 0.22f, RadiusX = 0.34f, RadiusY = 0.30f, Colour = UITheme.Verdigris, Alpha = 0.06f },
                        new Glow { X = 0.18f, Y = 0.08f, RadiusX = 0.46f, RadiusY = 0.40f, Colour = UITheme.Orpiment, Alpha = 0.12f });
                }
                return _backdrop;
            }
        }

        /// <summary>The ground for the death screen: the same bone-black with one madder glow.</summary>
        public static Texture2D BackdropDefeat
        {
            get
            {
                if (_backdropDefeat == null)
                {
                    _backdropDefeat = BuildBackdrop(
                        new Glow { X = 0.30f, Y = 0.40f, RadiusX = 0.60f, RadiusY = 0.50f, Colour = UITheme.Madder, Alpha = 0.14f });
                }
                return _backdropDefeat;
            }
        }

        /// <summary>
        /// Bone-black that thickens from left to right (35%, 80% at 60% across, 94%): the pause
        /// overlay, so the world stays visible on the left and the panel sits on near-solid ground.
        /// </summary>
        public static Texture2D FadeLeftToRight
        {
            get
            {
                if (_fade == null)
                {
                    const int width = 256;
                    _fade = Create(width, 1, FilterMode.Bilinear);
                    var pixels = new Color32[width];
                    Color ground = UITheme.BoneBlack;
                    for (int x = 0; x < width; x++)
                    {
                        float t = x / (width - 1f);
                        float alpha = t < 0.6f
                            ? Mathf.Lerp(0.35f, 0.80f, t / 0.6f)
                            : Mathf.Lerp(0.80f, 0.94f, (t - 0.6f) / 0.4f);
                        pixels[x] = ToColor32(new Color(ground.r, ground.g, ground.b, alpha));
                    }
                    _fade.SetPixels32(pixels);
                    _fade.Apply();
                }
                return _fade;
            }
        }

        /// <summary>
        /// An anti-aliased white circle outline. The outline is <paramref name="thickness"/> pixels
        /// wide at the texture's own size, so draw it at half size for a hairline.
        /// </summary>
        public static Texture2D Ring(int size, float thickness)
        {
            long key = ((long)size << 32) | (uint)Mathf.RoundToInt(thickness * 100f);
            if (Rings.TryGetValue(key, out Texture2D cached) && cached != null)
                return cached;

            Texture2D texture = Create(size, size, FilterMode.Bilinear);
            var pixels = new Color32[size * size];
            float centre = size * 0.5f;
            float radius = centre - thickness * 0.5f - 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - centre;
                    float dy = y + 0.5f - centre;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float coverage = Coverage(Mathf.Abs(distance - radius) - thickness * 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            Rings[key] = texture;
            return texture;
        }

        /// <summary>
        /// The main menu's sigil: an outer ring in Line colour, a dashed verdigris ring inside it, and
        /// in orpiment a small glyph (a vertical stroke, a chevron, a circle and a base bar).
        /// </summary>
        public static Texture2D Sigil
        {
            get
            {
                if (_sigil == null)
                    _sigil = BuildSigil();
                return _sigil;
            }
        }

        private static Texture2D BuildSigil()
        {
            var pixels = new Color[SigilSize * SigilSize];
            // Transparent pixels carry the outer ring's colour so bilinear filtering does not
            // fringe the edge with black.
            var clear = new Color(UITheme.Line.r, UITheme.Line.g, UITheme.Line.b, 0f);
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            for (int y = 0; y < SigilSize; y++)
            {
                for (int x = 0; x < SigilSize; x++)
                {
                    // Texture rows run bottom to top; the view box runs top to bottom.
                    float ux = (x + 0.5f) / SigilScale;
                    float uy = (SigilSize - y - 0.5f) / SigilScale;
                    Color pixel = clear;

                    Over(ref pixel, UITheme.Line, StrokeCircle(ux, uy, 50f, 50f, 46f, 1.5f));
                    Over(ref pixel, UITheme.Verdigris, DashedCircle(ux, uy, 50f, 50f, 38f, 1.2f, 3f, 4f));

                    float glyph = Mathf.Max(
                        Mathf.Max(Bar(ux, uy, 50f, 22f, 50f, 70f, 2.5f), Bar(ux, uy, 38f, 34f, 50f, 22f, 2.5f)),
                        Mathf.Max(Bar(ux, uy, 50f, 22f, 62f, 34f, 2.5f), Bar(ux, uy, 36f, 76f, 64f, 76f, 2.5f)));
                    glyph = Mathf.Max(glyph, StrokeCircle(ux, uy, 50f, 50f, 5f, 2.5f));
                    Over(ref pixel, UITheme.Orpiment, glyph);

                    pixels[y * SigilSize + x] = pixel;
                }
            }

            Texture2D texture = Create(SigilSize, SigilSize, FilterMode.Bilinear);
            var bytes = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                bytes[i] = ToColor32(pixels[i]);
            texture.SetPixels32(bytes);
            texture.Apply();
            return texture;
        }

        private static Texture2D BuildBackdrop(params Glow[] glows)
        {
            Texture2D texture = Create(BackdropWidth, BackdropHeight, FilterMode.Bilinear);
            var pixels = new Color32[BackdropWidth * BackdropHeight];
            for (int y = 0; y < BackdropHeight; y++)
            {
                // Fractions from the top-left, like the CSS gradients they reproduce.
                float v = 1f - (y + 0.5f) / BackdropHeight;
                for (int x = 0; x < BackdropWidth; x++)
                {
                    float u = (x + 0.5f) / BackdropWidth;
                    Color colour = UITheme.BoneBlack;
                    for (int g = 0; g < glows.Length; g++)
                    {
                        float dx = (u - glows[g].X) / glows[g].RadiusX;
                        float dy = (v - glows[g].Y) / glows[g].RadiusY;
                        float t = Mathf.Sqrt(dx * dx + dy * dy);
                        // Full strength at the centre, gone by 70% of the radius.
                        float alpha = glows[g].Alpha * (1f - Mathf.Clamp01(t / 0.7f));
                        colour = Color.Lerp(colour, glows[g].Colour, alpha);
                    }
                    pixels[y * BackdropWidth + x] = ToColor32(colour);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D Create(int width, int height, FilterMode filter)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = filter,
                hideFlags = HideFlags.DontSave,
            };
            return texture;
        }

        /// <summary>Pixel coverage for a signed distance (in pixels) from a shape's edge, negative inside.</summary>
        private static float Coverage(float signedDistance) => Mathf.Clamp01(0.5f - signedDistance);

        private static float StrokeCircle(float x, float y, float cx, float cy, float radius, float width)
        {
            float dx = x - cx;
            float dy = y - cy;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            return Coverage((Mathf.Abs(distance - radius) - width * 0.5f) * SigilScale);
        }

        /// <summary>A ring of dashes measured along its circumference, starting at 3 o'clock and running clockwise.</summary>
        private static float DashedCircle(float x, float y, float cx, float cy, float radius, float width, float dash, float gap)
        {
            float ring = StrokeCircle(x, y, cx, cy, radius, width);
            if (ring <= 0f)
                return 0f;

            float angle = Mathf.Atan2(y - cy, x - cx);
            if (angle < 0f)
                angle += Mathf.PI * 2f;
            float period = dash + gap;
            float along = angle * radius % period;
            float inside = along < dash
                ? Mathf.Min(along, dash - along)
                : -Mathf.Min(along - dash, period - along);
            return ring * Mathf.Clamp01(0.5f + inside * SigilScale);
        }

        /// <summary>A stroke from (ax, ay) to (bx, by) with square caps, as an oriented box.</summary>
        private static float Bar(float x, float y, float ax, float ay, float bx, float by, float width)
        {
            float dx = bx - ax;
            float dy = by - ay;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            float dirX = dx / length;
            float dirY = dy / length;
            float px = x - (ax + bx) * 0.5f;
            float py = y - (ay + by) * 0.5f;
            float along = Mathf.Abs(px * dirX + py * dirY) - (length + width) * 0.5f;
            float across = Mathf.Abs(-px * dirY + py * dirX) - width * 0.5f;
            float outside = Mathf.Sqrt(Mathf.Max(along, 0f) * Mathf.Max(along, 0f) + Mathf.Max(across, 0f) * Mathf.Max(across, 0f));
            float inside = Mathf.Min(Mathf.Max(along, across), 0f);
            return Coverage((outside + inside) * SigilScale);
        }

        /// <summary>Straight-alpha "over": lays a colour at the given coverage onto a pixel.</summary>
        private static void Over(ref Color destination, Color source, float coverage)
        {
            if (coverage <= 0f)
                return;

            float outAlpha = coverage + destination.a * (1f - coverage);
            float keep = destination.a * (1f - coverage);
            destination = new Color(
                (source.r * coverage + destination.r * keep) / outAlpha,
                (source.g * coverage + destination.g * keep) / outAlpha,
                (source.b * coverage + destination.b * keep) / outAlpha,
                outAlpha);
        }

        private static Color32 ToColor32(Color c) => new Color32(
            (byte)Mathf.RoundToInt(Mathf.Clamp01(c.r) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(c.g) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(c.b) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(c.a) * 255f));
    }
}
