using NUnit.Framework;
using Plunderspell.Market;

namespace Plunderspell.Tests
{
    /// <summary>The Collector's equal shares (#313). Pure arithmetic, also run by plain .NET (Tools/MarketRules).</summary>
    public class CollectorTests
    {
        private static readonly bool[] One = { true, false, false, false };
        private static readonly bool[] Two = { true, true, false, false };
        private static readonly bool[] Four = { true, true, true, true };

        [Test]
        public void Test_OneWizardOwesTheWholeDebtUpToTheirPurse()
        {
            Assert.AreEqual(new[] { 300, 0, 0, 0 }, CollectorRules.Take(500f, new[] { 300, 0, 0, 0 }, One));
            Assert.AreEqual(new[] { 500, 0, 0, 0 }, CollectorRules.Take(500f, new[] { 900, 0, 0, 0 }, One));
        }

        [Test]
        public void Test_TwoWizardsOweHalfEach()
        {
            Assert.AreEqual(250, CollectorRules.Share(500f, 2));
            Assert.AreEqual(new[] { 250, 250, 0, 0 }, CollectorRules.Take(500f, new[] { 400, 300, 0, 0 }, Two));
        }

        [Test]
        public void Test_FourWizardsOweAQuarterEach()
        {
            Assert.AreEqual(new[] { 125, 125, 125, 125 }, CollectorRules.Take(500f, new[] { 999, 999, 999, 999 }, Four));
        }

        [Test]
        public void Test_AShortPurseGivesAllItHasAndNobodyElseCovers()
        {
            Assert.AreEqual(new[] { 250, 40, 0, 0 }, CollectorRules.Take(500f, new[] { 400, 40, 0, 0 }, Two));
        }

        [Test]
        public void Test_AFriendCoversAShareByBankingIntoTheFriendsPurse()
        {
            // Seat 1 has nothing; seat 0 banked its own share and seat 1's into seat 1's strongbox.
            Assert.AreEqual(new[] { 250, 250, 0, 0 }, CollectorRules.Take(500f, new[] { 250, 250, 0, 0 }, Two));
        }

        [Test]
        public void Test_AnAbsentSeatsPurseIsLeftAloneAndIsNotCounted()
        {
            Assert.AreEqual(new[] { 500, 0, 0, 0 }, CollectorRules.Take(500f, new[] { 800, 800, 0, 0 }, One));
        }

        [Test]
        public void Test_ARoundedUpShareNeverTakesMoreThanTheDebt()
        {
            int[] taken = CollectorRules.Take(500f, new[] { 999, 999, 999, 0 }, new[] { true, true, true, false });
            Assert.AreEqual(new[] { 167, 167, 166, 0 }, taken);
        }

        [Test]
        public void Test_NothingOwedOrNobodyPresentTakesNothing()
        {
            Assert.AreEqual(new[] { 0, 0, 0, 0 }, CollectorRules.Take(0f, new[] { 50, 50, 0, 0 }, Two));
            Assert.AreEqual(new[] { 0, 0, 0, 0 }, CollectorRules.Take(500f, new[] { 50, 50, 0, 0 }, new bool[4]));
        }
    }
}
