using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The one torn edge of the demand page (#458), drawn as alpha-cut textures so the stub left in the book and the page torn
    /// from it fit each other. Both are seen with the spine on the right of the picture. The tear runs from the spine to
    /// <see cref="Reach"/> (a share of <see cref="StripWidth"/>) at each height; a few rows send a longer wisp out, and the
    /// paper thins to a pale, fibrous fringe along the line. <see cref="Stub"/> keeps the paper that lies nearer the spine than
    /// the tear; <see cref="Cut"/> keeps the paper farther from it.
    /// </summary>
    public static class RaggedEdge
    {
        /// <summary>How wide the stub is at its widest, in metres.</summary>
        public const float StripWidth = 0.06f;

        public const string UnlitResource = "Raid/TornPageUnlit"; // under Assets/_Project/Resources, made by LairRoomForge

        private const float PageWidth = 0.325f; // the book's page, in metres, which the stub's share of a page is measured against
        private const float Seed = 45.8f;
        private static readonly Color32 Paper = new Color32(228, 228, 228, 255);
        private static readonly Color32 Fibre = new Color32(255, 255, 255, 255);

        /// <summary>Where the tear lies at this height (0 bottom, 1 top): a share of the strip's width from the spine, 0.35 to 1.</summary>
        public static float Reach(float height)
        {
            float n = Mathf.PerlinNoise(height * 4f + Seed, 0.3f) * 0.55f
                    + Mathf.PerlinNoise(height * 19f + Seed, 1.7f) * 0.3f
                    + Mathf.PerlinNoise(height * 80f + Seed, 3.1f) * 0.15f;
            return Mathf.Lerp(0.35f, 1f, Mathf.Clamp01((n - 0.25f) / 0.5f));
        }

        /// <summary>The strip left in the book: paper from the spine (right edge) out to the tear, clear beyond it.</summary>
        public static Texture2D Stub(int width, int height)
        {
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                (float tear, float fringe) = Line(y, height);
                for (int x = 0; x < width; x++)
                {
                    float away = (width - 1 - x + 0.5f) / width; // from the spine, as a share of the strip
                    float kept = Mathf.Clamp01(1f - (away - (tear - fringe)) / fringe); // 1 well inside, 0 at the tear
                    bool solid = away < tear && Roll(x, y) < kept;
                    Color32 colour = away < tear - fringe ? Grain(Paper, x, y) : Fibre;
                    colour.a = (byte)(solid ? 255 : 0);
                    pixels[y * width + x] = colour;
                }
            }
            return Finish(pixels, width, height, "TornStub");
        }

        /// <summary>A copy of the page with the paper nearer the spine (its right edge) than the tear cut away.</summary>
        public static Texture2D Cut(Texture2D page)
        {
            int width = page.width, height = page.height;
            Color32[] pixels = page.GetPixels32();
            float strip = StripWidth / PageWidth; // the strip's width as a share of the page
            for (int y = 0; y < height; y++)
            {
                (float tear, float fringe) = Line(y, height);
                for (int x = 0; x < width; x++)
                {
                    float away = (width - 1 - x + 0.5f) / (width * strip);
                    if (away >= tear + fringe)
                        continue;
                    float kept = Mathf.Clamp01((away - tear) / fringe); // 0 at the tear, 1 once past the fringe
                    Color32 colour = pixels[y * width + x];
                    if (away < tear || Roll(x, y) >= kept)
                        colour.a = 0;
                    else
                        colour = Color32.Lerp(colour, Fibre, 0.5f);
                    pixels[y * width + x] = colour;
                }
            }
            return Finish(pixels, width, height, "TornPage");
        }

        /// <summary>The tear and the width of the fibrous fringe along it for one row, as shares of the strip.</summary>
        private static (float tear, float fringe) Line(int row, int rows)
        {
            float wisp = Roll(row, -2) > 0.88f ? Roll(row, -3) * 0.12f : 0f;
            float tear = Reach((row + 0.5f) / rows) + (Roll(row, -1) - 0.5f) * 0.03f + wisp;
            return (Mathf.Min(tear, 1.1f), 0.03f + 0.05f * Roll(row, -4));
        }

        private static Color32 Grain(Color32 colour, int x, int y)
        {
            int shade = (int)((Roll(x, y + 9000) - 0.5f) * 14f);
            return new Color32((byte)(colour.r + shade), (byte)(colour.g + shade), (byte)(colour.b + shade), 255);
        }

        // The same pixel always rolls the same number, 0 up to 1, so every machine draws the same tear.
        private static float Roll(int a, int b)
        {
            float v = Mathf.Sin(a * 12.9898f + b * 78.233f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        private static Texture2D Finish(Color32[] pixels, int width, int height, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false); // kept readable: the dropped page is cut from the drawing, which stays readable too
            return texture;
        }

        /// <summary>
        /// A page material that clips the transparent parts of the tear. The room's forge saves the same settings as assets
        /// (so a build keeps the clip shader variant); this builds them when no asset is at hand.
        /// </summary>
        public static Material PageMaterial(string shaderName, Color colour)
        {
            var saved = Resources.Load<Material>(UnlitResource); // keeps the clip variant in a build
            var material = saved != null ? new Material(saved) { color = colour } : new Material(Shader.Find(shaderName)) { color = colour, renderQueue = 2450 };
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.EnableKeyword("_ALPHATEST_ON");
            return material;
        }
    }
}
