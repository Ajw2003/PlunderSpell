using NUnit.Framework;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The ledger's glowing words tell how soon the demand is due by colour and by how they swell (#447).</summary>
    public class LairLedgerBookTests
    {
        [Test]
        public void Glow_IsRedWhenDueAmberAtTwoAndFaintWhiteAtThree()
        {
            Color due = LairLedgerBook.Glow(1), half = LairLedgerBook.Glow(2), far = LairLedgerBook.Glow(3);
            Assert.Greater(due.r, 0.9f); Assert.Less(due.g, 0.3f);
            Assert.Greater(half.r, 0.9f); Assert.That(half.g, Is.InRange(0.4f, 0.75f)); Assert.Less(half.b, 0.3f);
            Assert.Greater(far.r, 0.85f); Assert.Greater(far.g, 0.85f); Assert.Greater(far.b, 0.8f);
        }

        [Test]
        public void Swell_IsFasterAndBrighterAsTheDemandNears()
        {
            Assert.AreEqual(5f, LairLedgerBook.BreathSeconds(3));
            Assert.Less(LairLedgerBook.BreathSeconds(2), LairLedgerBook.BreathSeconds(3));
            Assert.AreEqual(2f, LairLedgerBook.BreathSeconds(1));
            Assert.Less(LairLedgerBook.PeakBrightness(3), LairLedgerBook.PeakBrightness(2));
            Assert.Less(LairLedgerBook.PeakBrightness(2), LairLedgerBook.PeakBrightness(1));
        }
    }
}
