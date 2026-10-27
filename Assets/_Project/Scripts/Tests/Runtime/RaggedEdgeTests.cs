using NUnit.Framework;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The torn edge of the demand page (#458).</summary>
    public class RaggedEdgeTests
    {
        private const int Width = 96, Height = 672;

        [Test]
        public void TheStubIsSolidAtTheSpineAndClearAtItsFarEdge()
        {
            Texture2D stub = RaggedEdge.Stub(Width, Height);
            Color32[] pixels = stub.GetPixels32();
            for (int y = 0; y < Height; y++)
            {
                Assert.AreEqual(255, pixels[y * Width + Width - 1].a, $"spine side, row {y}");
                Assert.AreEqual(0, pixels[y * Width].a, $"far side, row {y}"); // Reach never goes past the strip's width
            }
            Object.DestroyImmediate(stub);
        }

        [Test]
        public void TheTearLineVariesAndEdgePixelsAreBothClearAndSolid()
        {
            Texture2D stub = RaggedEdge.Stub(Width, Height);
            Color32[] pixels = stub.GetPixels32();
            int shortest = Width, longest = 0, clear = 0, solid = 0;
            for (int y = 0; y < Height; y++)
            {
                int solidPixels = 0;
                for (int x = 0; x < Width; x++)
                    if (pixels[y * Width + x].a > 0)
                        solidPixels++;
                shortest = Mathf.Min(shortest, solidPixels);
                longest = Mathf.Max(longest, solidPixels);
            }
            foreach (Color32 pixel in pixels)
            {
                if (pixel.a == 0) clear++; else solid++;
            }
            Assert.Greater(longest - shortest, Width / 3, "the tear wanders by a third of the strip or more");
            Assert.Greater(clear, 0);
            Assert.Greater(solid, 0);
            Object.DestroyImmediate(stub);
        }

        [Test]
        public void TheCutPageKeepsItsFarSideAndLosesAnEdgeOnTheSpineSide()
        {
            var page = new Texture2D(256, 365, TextureFormat.RGBA32, false);
            var paper = new Color32[256 * 365];
            for (int i = 0; i < paper.Length; i++)
                paper[i] = new Color32(200, 200, 200, 255);
            page.SetPixels32(paper);
            page.Apply(false, false);

            Texture2D cut = RaggedEdge.Cut(page);
            Color32[] pixels = cut.GetPixels32();
            int gone = 0;
            for (int y = 0; y < 365; y++)
            {
                Assert.AreEqual(255, pixels[y * 256].a, $"far side, row {y}");
                Assert.AreEqual(0, pixels[y * 256 + 255].a, $"spine edge, row {y}");
                for (int x = 0; x < 256; x++)
                    if (pixels[y * 256 + x].a == 0) gone++;
            }
            Assert.Greater(gone, 365 * 5, "a strip is cut away");
            Assert.Less(gone, 256 * 365 / 4, "but only a strip");
            Object.DestroyImmediate(page);
            Object.DestroyImmediate(cut);
        }
    }
}
