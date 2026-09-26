using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Extraction;
using Plunderspell.Inventory;
using Plunderspell.Lair;
using Plunderspell.Loot;
using Plunderspell.Spells;
using Plunderspell.Voice;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests.Integration
{
    /// <summary>
    /// Milestone-3 end-to-end integration suite. Coroutine [UnityTest]s exercise the four-simulated-client
    /// replication contract via <see cref="NetworkTestHarness"/>; the remaining [Test]s exercise the raid
    /// loop's network-free logic seams (voice → spell resolve, misfire, fragility, alarm escalation,
    /// extraction tally, cross-era equip, debt, downed-player bulk).
    ///
    /// A headless runner cannot open four real sockets, so client/seed replication is simulated in-process
    /// (see NetworkTestHarness) — we validate the replication invariants, not the socket transport.
    /// </summary>
    public class PlunderspellIntegrationTests
    {
        private readonly List<Object> _spawned = new List<Object>();
        private NetworkTestHarness _harness;

        [TearDown]
        public void TearDown()
        {
            _harness?.Teardown();
            _harness = null;
            foreach (var o in _spawned)
                if (o != null) Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        // ----- helpers -----------------------------------------------------------------------

        private T Track<T>(T o) where T : Object { _spawned.Add(o); return o; }

        private SpellLexicon BuildLexicon()
        {
            var ignis = ScriptableObject.CreateInstance<SpellWord>();
            ignis.Word = "IGNIS";
            ignis.spellId = SpellId.Ignis;
            ignis.misfireId = SpellId.MisfireIgnis;
            ignis.AltPronunciations = new[] { "AGNIS", "IGNISH" };
            Track(ignis);

            var lex = ScriptableObject.CreateInstance<SpellLexicon>();
            lex.MaxNearMatchDistance = 2;
            lex.Spells = new List<SpellWord> { ignis };
            Track(lex);
            return lex;
        }

        private LootPickup MakeLoot(float worth, float fragility = 1000f)
        {
            var go = Track(new GameObject("Loot"));
            var pickup = go.AddComponent<LootPickup>(); // RequireComponent adds Rigidbody
            var data = Track(ScriptableObject.CreateInstance<LootItem>());
            data.Worth = worth;
            data.Fragility = fragility;
            data.Bulk = 3f;
            pickup.SetData(data);
            return pickup;
        }

        // ===== Networked replication (simulated 4 clients) ===================================

        [UnityTest]
        public IEnumerator Test_FourClientsConnect()
        {
            _harness = new NetworkTestHarness();
            _harness.StartHost();
            _harness.ConnectClient(1);
            _harness.ConnectClient(2);
            _harness.ConnectClient(3);

            yield return new WaitForSeconds(1f);

            // Host (index 0) + 3 clients = 4 connected peers.
            Assert.AreEqual(4, _harness.ConnectedCount, "Expected host + 3 clients connected.");
        }

        [UnityTest]
        public IEnumerator Test_CastleSeedReplicates()
        {
            _harness = new NetworkTestHarness();
            _harness.StartHost();
            _harness.ConnectClient(1);
            _harness.ConnectClient(2);
            _harness.ConnectClient(3);

            const int hostSeed = 20260912;
            _harness.ReplicateCastleSeed(hostSeed); // server-authoritative SyncVar fan-out

            yield return new WaitForSeconds(0.5f);

            foreach (var c in _harness.Clients)
                Assert.AreEqual(hostSeed, c.CastleSeed,
                    $"Client {c.Index} seed did not match host seed.");
        }

        // ===== Voice / spells ================================================================

        [Test]
        public void Test_VoiceMockCastIgnis()
        {
            var lex = BuildLexicon();
            SpellId resolved = MisfireEngine.Resolve("IGNIS", lex);
            Assert.AreEqual(SpellId.Ignis, resolved);
        }

        [Test]
        public void Test_MisfireEngineViaIntegration()
        {
            var lex = BuildLexicon();
            SpellId resolved = MisfireEngine.Resolve("AGNIS", lex);
            Assert.AreEqual(SpellId.MisfireIgnis, resolved);
        }

        // ===== Loot fragility ================================================================

        [Test]
        public void Test_LootPickupFragility()
        {
            var go = Track(new GameObject("Fragile"));
            var pickup = go.AddComponent<LootPickup>();
            var data = Track(ScriptableObject.CreateInstance<LootItem>());
            data.Bulk = 3f;
            data.Fragility = 5f;
            pickup.SetData(data);

            Assert.IsTrue(pickup.WouldBreak(10f),
                "Impact velocity 10 above fragility 5 should break the item.");
        }

        // ===== Alarm escalation ==============================================================

        [Test]
        public void Test_AlarmEscalationToHueCry()
        {
            var go = Track(new GameObject("Alarm"));
            var alarm = go.AddComponent<AlarmFSMManager>();

            // 6 noise events at strength 1.0 (weight 15) → 90 ≥ 80 → HueAndCry, latched.
            for (int i = 0; i < 6; i++)
                alarm.ApplyNoise(1.0f);

            Assert.AreEqual(AlarmState.HueAndCry, alarm.State);
            Assert.IsTrue(alarm.IsLocked, "Alarm should latch (_locked) at Roused/HueAndCry.");
        }

        // ===== Extraction tally ==============================================================

        [Test]
        public void Test_ExtractionTally()
        {
            var go = Track(new GameObject("Zone"));
            go.AddComponent<BoxCollider>().isTrigger = true;
            var zone = go.AddComponent<ExtractionZone>();

            zone.TrackLoot(MakeLoot(80f));
            zone.TrackLoot(MakeLoot(40f));
            zone.TrackLoot(MakeLoot(200f));

            var (worth, _) = zone.ResolveLocally();
            Assert.AreEqual(320f, worth, 0.001f, "Extraction worth should tally 80+40+200.");
        }

        // ===== Cross-era inventory ===========================================================

        [Test]
        public void Test_CrossEraEquip()
        {
            var item = Track(ScriptableObject.CreateInstance<InventoryItem>());
            item.EraAcquired = HistoricalEra.AgeOfPowder;
            Assert.IsTrue(item.CanEquipInEra(HistoricalEra.BronzeAge),
                "Cross-era equipping must always be allowed.");
        }

        // ===== Debt system ===================================================================

        [Test]
        public void Test_DebtSystem()
        {
            PlayerPrefs.DeleteKey("TotalDebt");
            PlayerPrefs.DeleteKey("AccumulatedGold");
            var go = Track(new GameObject("Lair"));
            var lair = go.AddComponent<LairHubManager>();
            lair.Load();

            float before = lair.TotalDebt; // 500 default
            lair.ApplyExtractionResult(200f);
            Assert.Less(lair.TotalDebt, before, "Debt should be reduced after extraction payment.");
        }

        // ===== Downed player bulk ============================================================

        [Test]
        public void Test_DownedPlayerBulk()
        {
            var go = Track(new GameObject("Downed"));
            go.AddComponent<LootPickup>();
            var adapter = go.AddComponent<DownedPlayerCarryAdapter>();
            adapter.EnterDownedState();

            var pickup = go.GetComponent<LootPickup>();
            Assert.IsNotNull(pickup.Data, "Downed adapter should assign body loot data.");
            Assert.AreEqual(12f, pickup.Data.Bulk, 0.001f,
                "A downed player body should be Bulk 12 (forces dual carry).");
        }
    }
}
