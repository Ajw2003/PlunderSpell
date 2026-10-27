using NUnit.Framework;
using Plunderspell.Raid;

namespace Plunderspell.Tests
{
    /// <summary>The rules for raising the torn page (#449).</summary>
    public class PageRaiseTests
    {
        [Test]
        public void OnlyTheHolderPressingReadRaisesThePage()
        {
            Assert.AreEqual(1f, PageRaise.Target(true, true));
            Assert.AreEqual(0f, PageRaise.Target(false, true));
            Assert.AreEqual(0f, PageRaise.Target(true, false));
        }

        [Test]
        public void ThePageTakesAQuarterSecondToRiseAndFall()
        {
            Assert.AreEqual(0.5f, PageRaise.Step(0f, 1f, 0.125f), 1e-4f);
            Assert.AreEqual(1f, PageRaise.Step(0.9f, 1f, 1f));
            Assert.AreEqual(0f, PageRaise.Step(0.1f, 0f, 1f));
        }
    }
}
