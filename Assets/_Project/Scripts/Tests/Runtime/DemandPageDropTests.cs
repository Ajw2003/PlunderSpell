using NUnit.Framework;
using Plunderspell.Raid;

namespace Plunderspell.Tests
{
    /// <summary>The rules for dropping and picking up the torn ledger page (#450).</summary>
    public class DemandPageDropTests
    {
        [Test]
        public void QDropsThePageOnlyWithEmptyHands()
        {
            Assert.IsTrue(DemandPageRules.QDropsPage(holdsPage: true, carryingLoot: false));
            Assert.IsFalse(DemandPageRules.QDropsPage(holdsPage: true, carryingLoot: true));
            Assert.IsFalse(DemandPageRules.QDropsPage(holdsPage: false, carryingLoot: false));
        }

        [Test]
        public void OnlyTheHolderCanDropAndALyingPageStillCountsAsTorn()
        {
            Assert.AreEqual(DemandPageRules.Lying, DemandPageRules.Drop(1, 1));
            Assert.AreEqual(1, DemandPageRules.Drop(1, 0));
            Assert.AreEqual(DemandPageRules.InBook, DemandPageRules.Drop(DemandPageRules.InBook, 0));
            Assert.IsTrue(DemandPageRules.IsTorn(DemandPageRules.Lying));
        }

        [Test]
        public void OnlyALyingPageCanBePickedUp()
        {
            Assert.AreEqual(0, DemandPageRules.PickUp(DemandPageRules.Lying, 0));
            Assert.AreEqual(1, DemandPageRules.PickUp(1, 0));
            Assert.AreEqual(DemandPageRules.InBook, DemandPageRules.PickUp(DemandPageRules.InBook, 0));
        }

        [Test]
        public void ComingHomeClearsALyingPageBackIntoTheBook()
        {
            // The keeper sets the holder to InBook on Resolved; a lying page is torn until then, and a tear after is allowed again.
            Assert.AreEqual(2, DemandPageRules.Tear(DemandPageRules.InBook, 2));
            Assert.IsFalse(DemandPageRules.IsTorn(DemandPageRules.InBook));
            Assert.AreEqual(DemandPageRules.Lying, DemandPageRules.Tear(DemandPageRules.Lying, 2));
        }
    }
}
