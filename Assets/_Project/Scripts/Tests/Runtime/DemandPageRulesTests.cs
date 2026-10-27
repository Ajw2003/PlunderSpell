using NUnit.Framework;
using Plunderspell.Raid;

namespace Plunderspell.Tests
{
    /// <summary>The rules for the torn ledger page (#448).</summary>
    public class DemandPageRulesTests
    {
        [Test]
        public void APageInTheBookIsTornByTheSeatThatTriesFirst()
        {
            Assert.AreEqual(1, DemandPageRules.Tear(DemandPageRules.InBook, 1));
            Assert.IsFalse(DemandPageRules.IsTorn(DemandPageRules.InBook));
            Assert.IsTrue(DemandPageRules.IsTorn(1));
        }

        [Test]
        public void ATornPageStaysWithItsHolderWhenAnotherTriesToTearIt()
        {
            Assert.AreEqual(1, DemandPageRules.Tear(1, 0));
            Assert.AreEqual(0, DemandPageRules.Tear(0, 0));
        }

        [Test]
        public void SeatZeroCanHoldThePage()
        {
            Assert.IsTrue(DemandPageRules.IsTorn(DemandPageRules.Tear(DemandPageRules.InBook, 0)));
        }
    }
}
