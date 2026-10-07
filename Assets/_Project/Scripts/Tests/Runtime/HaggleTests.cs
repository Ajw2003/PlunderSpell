using System;
using NUnit.Framework;
using Plunderspell.Market;

namespace Plunderspell.Tests
{
    /// <summary>The haggle rules from docs/plans/diegetic-ui-lair-market.md, "The numbers behind it" (#312).</summary>
    public class HaggleTests
    {
        [Test]
        public void PlusBelowTheLimitRaisesTheOfferByATenth()
        {
            var haggle = new Haggle(limit: 100f, openingOffer: 60f, patience: 3);
            Assert.AreEqual(HaggleOutcome.Raised, haggle.Answer(HaggleWord.Plus));
            Assert.AreEqual(66f, haggle.Offer, 0.001f);
        }

        [Test]
        public void PlusAboveTheLimitCostsOnePatienceAndKeepsTheOffer()
        {
            var haggle = new Haggle(limit: 100f, openingOffer: 95f, patience: 3);
            Assert.AreEqual(HaggleOutcome.Refused, haggle.Answer(HaggleWord.Plus));
            Assert.AreEqual(95f, haggle.Offer, 0.001f);
            Assert.AreEqual(2, haggle.Patience);
        }

        [Test]
        public void TheFenceWalksAwayAtTheFirstRefusal()
        {
            var haggle = new Haggle(limit: 100f, openingOffer: 95f, patience: HaggleRules.Patience(Vendor.Fence));
            Assert.AreEqual(HaggleOutcome.WillNotBuy, haggle.Answer(HaggleWord.Plus));
            Assert.IsTrue(haggle.IsOver);
        }

        [Test]
        public void SatisSellsForTheCoinsOnTheCounter()
        {
            var haggle = new Haggle(limit: 100f, openingOffer: 60f, patience: 3);
            haggle.Answer(HaggleWord.Plus);
            Assert.AreEqual(HaggleOutcome.Sold, haggle.Answer(HaggleWord.Satis));
            Assert.AreEqual(66f, haggle.Offer, 0.001f);
            Assert.IsTrue(haggle.IsOver);
        }

        [Test]
        public void ValeEndsTheHaggleWithoutASale()
        {
            var haggle = new Haggle(limit: 100f, openingOffer: 60f, patience: 3);
            Assert.AreEqual(HaggleOutcome.WalkedAway, haggle.Answer(HaggleWord.Vale));
            Assert.Throws<InvalidOperationException>(() => haggle.Answer(HaggleWord.Plus));
        }

        [Test]
        public void AGoodHagglerGetsMostOfTheLimit()
        {
            var haggle = new Haggle(limit: 100f, openingOffer: 55f, patience: 3);
            while (haggle.Answer(HaggleWord.Plus) == HaggleOutcome.Raised) { }
            Assert.Greater(haggle.Offer, 90f);
            Assert.LessOrEqual(haggle.Offer, 100f);
        }

        [Test]
        public void TheOpeningOfferIsBelowTheLimitAndLowerOnASecondVisit()
        {
            Haggle first = HaggleRules.Open(Vendor.Goldsmith, worth: 200f, interest: 1f, mood: 1f, new Random(7), cameBack: false);
            Haggle again = HaggleRules.Open(Vendor.Goldsmith, worth: 200f, interest: 1f, mood: 1f, new Random(7), cameBack: true);
            Assert.AreEqual(200f, first.Limit, 0.001f);
            Assert.That(first.Offer, Is.InRange(200f * HaggleRules.OpeningLow, 200f * HaggleRules.OpeningHigh));
            Assert.AreEqual(first.Offer * HaggleRules.CameBackOpening, again.Offer, 0.001f);
        }

        [TestCase(Vendor.Fence, LootCategory.Metal, 1f)]
        [TestCase(Vendor.Fence, LootCategory.Other, 1f)]
        [TestCase(Vendor.Goldsmith, LootCategory.Metal, 1.3f)]
        [TestCase(Vendor.Goldsmith, LootCategory.Holy, 0.7f)]
        [TestCase(Vendor.Pardoner, LootCategory.Holy, 1.3f)]
        [TestCase(Vendor.Pardoner, LootCategory.Arms, 0.7f)]
        [TestCase(Vendor.Antiquarian, LootCategory.Curio, 1.3f)]
        [TestCase(Vendor.Antiquarian, LootCategory.Arms, 1.3f)]
        [TestCase(Vendor.Antiquarian, LootCategory.Metal, 0.7f)]
        [TestCase(Vendor.Antiquarian, LootCategory.Other, 0.7f)]
        public void EachVendorWantsHisKindOfPiece(Vendor vendor, LootCategory category, float interest)
        {
            Assert.AreEqual(interest, HaggleRules.Interest(vendor, category), 0.0001f);
        }

        [TestCase(Vendor.Fence, 1)]
        [TestCase(Vendor.Goldsmith, 3)]
        [TestCase(Vendor.Pardoner, 3)]
        [TestCase(Vendor.Antiquarian, 4)]
        public void EachVendorHasHisPatience(Vendor vendor, int patience)
        {
            Assert.AreEqual(patience, HaggleRules.Patience(vendor));
        }
    }
}
