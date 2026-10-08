using NUnit.Framework;
using Plunderspell.Lair;

namespace Plunderspell.Tests
{
    /// <summary>The ledger book's pages say what the Lair screen's ledger says (#357).</summary>
    public class LedgerPageTextTests
    {
        [Test]
        public void LeftPage_GivesDebtGrowthAndLastRaidWithLeftBehind()
        {
            string page = LedgerPageText.LeftPage(1250f, 50f, 300f, 2);
            StringAssert.Contains("1,250", page);
            StringAssert.Contains("the debt grows by 50 each raid it stands", page);
            StringAssert.Contains("Brought home 300 coin · 2 left behind", page);
            StringAssert.Contains("No raid yet.", LedgerPageText.LeftPage(500f, 50f, -1f, 0));
            StringAssert.Contains("Came home with nothing", LedgerPageText.LeftPage(500f, 50f, 0f, 0));
        }

        [Test]
        public void RightPage_ShowsOnlySeatsInPlayAndTheCollectorLine()
        {
            string page = LedgerPageText.RightPage(new[] { 120, 0, 0, 0 }, new[] { 0, 0, 0, 0 },
                new[] { true, false, false, false }, 250, "The Collector takes 5 from I.");
            StringAssert.Contains("I  Purse 120\n    Owes 250  Paid 0", page);
            StringAssert.DoesNotContain("II ", page);
            StringAssert.Contains("The Collector takes 5 from I.", page);
        }
    }
}
