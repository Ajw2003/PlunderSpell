using NUnit.Framework;
using Plunderspell.Lair;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The demand page's ink sketch (#419) inks the piece and leaves the paper around it alone.</summary>
    public class InkSketchTests
    {
        [Test]
        public void Draw_InksTheOutlineAndDarkPartsAndLeavesThePaperWhite()
        {
            const int size = 64;
            var backdrop = new Color32(255, 0, 255, 255);
            var render = new Color32[size * size];
            for (int i = 0; i < render.Length; i++)
            {
                int x = i % size, y = i / size;
                bool onPiece = x >= 16 && x < 48 && y >= 16 && y < 48;
                // A piece lit from one side: bright on the left half, dark on the right.
                render[i] = !onPiece ? backdrop : x < 32 ? new Color32(220, 200, 150, 255) : new Color32(40, 30, 20, 255);
            }

            Color32[] sketch = InkSketch.Draw(render, size, size, backdrop);

            Assert.AreEqual(new Color32(255, 255, 255, 255), sketch[2 * size + 2], "paper far from the piece is untouched");
            Assert.Less(Darkest(sketch, 14, 18, 30, 34), 140, "the piece's left edge is outlined");
            int hatched = 0;
            for (int y = 20; y < 44; y++)
            for (int x = 36; x < 46; x++)
                if (sketch[y * size + x].r < 150)
                    hatched++;
            Assert.Greater(hatched, 20, "the dark half is hatched");
        }

        private static int Darkest(Color32[] pixels, int x0, int x1, int y0, int y1)
        {
            int darkest = 255;
            for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                darkest = Mathf.Min(darkest, pixels[y * 64 + x].r);
            return darkest;
        }
    }
}
