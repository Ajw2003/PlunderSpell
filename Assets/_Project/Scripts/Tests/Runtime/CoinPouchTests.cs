using NUnit.Framework;
using Plunderspell.Extraction;
using Plunderspell.Lair;
using Plunderspell.Loot;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// A sale's coins are a pouch: heavier with the coins, never loot, banked by a strongbox into its seat's purse.
    /// See docs/4-systems/market.md, "Coins".
    /// </summary>
    public class CoinPouchTests
    {
        private GameObject _lair;
        private int _slotBefore;

        [SetUp]
        public void SetUp()
        {
            _slotBefore = SaveSlots.Active;
            LairHubManager.ResetSlot(SaveSlots.TestSlot);
            SaveSlots.Active = SaveSlots.TestSlot;
            _lair = new GameObject("Lair");
            _lair.AddComponent<LairHubManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_lair);
            LairHubManager.ResetSlot(SaveSlots.TestSlot);
            SaveSlots.Active = _slotBefore;
        }

        private static CoinPouch MakePouch(int coins)
        {
            var go = new GameObject("Pouch");
            go.AddComponent<Rigidbody>();
            var pouch = go.AddComponent<CoinPouch>();
            pouch.Fill(coins);
            return pouch;
        }

        [Test]
        public void Test_PouchWeighsMoreWithEveryCoinAboveAFloor()
        {
            CoinPouch small = MakePouch(5);
            CoinPouch big = MakePouch(500);
            Assert.AreEqual(CoinPouch.MinKilos, small.GetComponent<Rigidbody>().mass, 0.001f);
            Assert.AreEqual(5f, big.GetComponent<Rigidbody>().mass, 0.001f, "0.01 kg a coin.");
            Assert.AreEqual(500, big.Coins);
            Object.DestroyImmediate(small.gameObject);
            Object.DestroyImmediate(big.gameObject);
        }

        [Test]
        public void Test_PouchIsNeverCountedAsLoot()
        {
            CoinPouch pouch = MakePouch(300);
            Assert.IsNull(pouch.GetComponent<LootValue>(), "Extraction, the haul pile and the counters only look for LootValue.");
            Assert.AreEqual(0, ExtractionZone.PiecesOf(Object.FindObjectsByType<LootValue>(FindObjectsSortMode.None)).Count);
            Object.DestroyImmediate(pouch.gameObject);
        }

        [Test]
        public void Test_StrongboxBanksIntoItsSeatsPurseAndTakesThePouch()
        {
            var lair = _lair.GetComponent<LairHubManager>();
            var box = new GameObject("Lid").AddComponent<LairStrongbox>();
            box.SetSeat(1);
            CoinPouch pouch = MakePouch(120);

            box.Accept(pouch);
            box.Accept(pouch); // still touching next physics step: gives nothing more

            Assert.AreEqual(120, lair.Purse(1));
            Assert.AreEqual(0, lair.Purse(0));
            Assert.AreEqual(380f, lair.TotalDebt, 0.01f, "Until the debt splits, every purse pays the shared debt.");
            Assert.AreEqual(120, LairHubManager.PeekPurse(SaveSlots.TestSlot, 1), "The purse is saved.");
            Object.DestroyImmediate(box.gameObject);
            Object.DestroyImmediate(pouch.gameObject);
        }
    }
}
