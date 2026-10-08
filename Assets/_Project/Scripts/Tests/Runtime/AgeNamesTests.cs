using NUnit.Framework;
using Plunderspell.Inventory;
using Plunderspell.Lair;

namespace Plunderspell.Tests
{
    /// <summary>The century dial's Age order and the plaque's words (#358).</summary>
    public class AgeNamesTests
    {
        [Test]
        public void Next_GoesBronzeHighLateAndPowderThenRound()
        {
            Assert.AreEqual(HistoricalEra.HighMedieval, AgeNames.Next(HistoricalEra.BronzeAge));
            Assert.AreEqual(HistoricalEra.LateMedieval, AgeNames.Next(HistoricalEra.HighMedieval));
            Assert.AreEqual(HistoricalEra.AgeOfPowder, AgeNames.Next(HistoricalEra.LateMedieval));
            Assert.AreEqual(HistoricalEra.BronzeAge, AgeNames.Next(HistoricalEra.AgeOfPowder));
        }

        [Test]
        public void Plaque_HoldsTheAgesNameDateAndBlurb()
        {
            string plaque = AgeNames.Plaque(HistoricalEra.AgeOfPowder);
            StringAssert.Contains("Age of Powder", plaque);
            StringAssert.Contains("Stratum IV", plaque);
            StringAssert.Contains("c. 1620", plaque);
            StringAssert.Contains("magazines of black powder", plaque);
            StringAssert.Contains("c. 1200 BC", AgeNames.Plaque(HistoricalEra.BronzeAge));
        }
    }
}
