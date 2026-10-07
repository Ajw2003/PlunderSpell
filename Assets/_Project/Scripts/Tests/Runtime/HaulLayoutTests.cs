using NUnit.Framework;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The haul pile's layout keeps pieces apart (#310).</summary>
    public class HaulLayoutTests
    {
        [TestCase(1)]
        [TestCase(21)]
        public void NoTwoPiecesShareASpot(int count)
        {
            var offsets = HaulLayout.Offsets(count);
            Assert.AreEqual(count, offsets.Count);
            for (int a = 0; a < count; a++)
            for (int b = a + 1; b < count; b++)
                Assert.GreaterOrEqual(Vector3.Distance(offsets[a], offsets[b]), HaulLayout.Spacing - 0.001f);
        }

        [Test]
        public void ASmallHaulLiesAtTheCentre()
        {
            var offsets = HaulLayout.Offsets(3);
            Assert.AreEqual(Vector3.zero, offsets[0]);
            foreach (Vector3 o in offsets)
                Assert.LessOrEqual(o.magnitude, HaulLayout.Spacing + 0.001f);
        }

        [Test]
        public void OverflowStacksAboveTheFirstLayer()
        {
            var offsets = HaulLayout.Offsets(22);
            Assert.AreEqual(HaulLayout.LayerHeight, offsets[21].y - offsets[0].y, 0.001f);
        }

        [Test]
        public void AHaulStartingAfterEarlierPiecesContinuesTheSameGrid()
        {
            var all = HaulLayout.Offsets(25);
            var later = HaulLayout.Offsets(5, 20);
            for (int i = 0; i < 5; i++)
                Assert.AreEqual(all[20 + i], later[i], "A later haul takes the next cells, layers when full.");
        }

        [Test]
        public void FirstLayerStaysInsideTheLandingPad()
        {
            foreach (Vector3 o in HaulLayout.Offsets(21))
            {
                Assert.LessOrEqual(Mathf.Abs(o.x), 0.5f);
                Assert.LessOrEqual(Mathf.Abs(o.z), 1.5f);
            }
        }
    }
}
