using System.Collections.Generic;
using NUnit.Framework;
using RogueAi.Extraction;
using RogueAi.Loot;
using RogueAi.UI;
using UnityEngine;

namespace RogueAi.Tests
{
    /// <summary>
    /// Extraction tallies what is standing on the pad, and the HUD says so while the raid is still
    /// running. See docs/systems/raid.md, "Carrying and extracting".
    /// </summary>
    public class ExtractionHaulTests
    {
        private readonly List<Object> m_tracked = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in m_tracked)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }

            m_tracked.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            m_tracked.Add(o);
            return o;
        }

        private LootValue MakePiece(string name, float worth)
        {
            var go = Track(new GameObject($"Loot_{name}"));
            var data = Track(ScriptableObject.CreateInstance<LootItem>());
            data.DisplayName = name;
            data.Worth = worth;

            var value = go.AddComponent<LootValue>();
            value.SetItem(data);
            return value;
        }

        private ExtractionZone MakeZone()
        {
            var go = Track(new GameObject("ExtractionZone"));
            go.AddComponent<BoxCollider>().isTrigger = true;
            return go.AddComponent<ExtractionZone>();
        }

        [Test]
        public void Test_TheZoneTalliesTheWorthOfWhatIsStandingInIt()
        {
            ExtractionZone zone = MakeZone();

            zone.TrackLoot(MakePiece("Goblet", 80f));
            zone.TrackLoot(MakePiece("Relic", 200f));

            Assert.AreEqual(280f, zone.WorthInZone, 0.001f);
            Assert.AreEqual(2, zone.PiecesInZone);
        }

        [Test]
        public void Test_RuinedLootIsWorthNothing()
        {
            ExtractionZone zone = MakeZone();

            LootValue intact = MakePiece("Intact", 100f);
            LootValue smashed = MakePiece("Smashed", 100f);
            smashed.Ruin();

            zone.TrackLoot(intact);
            zone.TrackLoot(smashed);

            Assert.AreEqual(100f, zone.WorthInZone, 0.001f,
                "A shattered treasure pays nothing, however far it was carried.");
        }

        /// <summary>
        /// The running total is the point of the change: a player stacking loot on the pad should
        /// watch the number climb, not find out what it was worth after the raid has ended.
        /// </summary>
        [Test]
        public void Test_TheHaulIsAnnouncedAsLootArrives()
        {
            ExtractionZone zone = MakeZone();

            float lastWorth = -1f;
            int lastPieces = -1;
            zone.HaulInZoneChanged += (worth, pieces) =>
            {
                lastWorth = worth;
                lastPieces = pieces;
            };

            zone.TrackLoot(MakePiece("Plate", 40f));

            Assert.AreEqual(40f, lastWorth, 0.001f, "Adding loot must announce the new total.");
            Assert.AreEqual(1, lastPieces);
        }

        [Test]
        public void Test_ResolvingPaysOutWhatWasStandingInTheZone()
        {
            ExtractionZone zone = MakeZone();
            zone.TrackLoot(MakePiece("Chalice", 120f));

            float resolvedWorth = -1f;
            zone.ExtractionResolved += (worth, _) => resolvedWorth = worth;

            zone.ResolveExtraction();

            Assert.AreEqual(120f, resolvedWorth, 0.001f);
        }

        [Test]
        public void Test_TheHudReadsOutTheHaulWhileTheRaidIsStillRunning()
        {
            var empty = new RaidHudModel(default, 0f, default, 0f, string.Empty, false,
                string.Empty, false, 0f, 0f, string.Empty, 0f, 0);
            Assert.AreEqual("Haul: bring loot to the portal", empty.HaulText,
                "An empty pad should tell the player what to do, not show a zero.");

            var oneP = new RaidHudModel(default, 0f, default, 0f, string.Empty, false,
                string.Empty, false, 0f, 0f, string.Empty, 250f, 1);
            Assert.AreEqual("Haul: 250 gold (1 piece)", oneP.HaulText);

            var many = new RaidHudModel(default, 0f, default, 0f, string.Empty, false,
                string.Empty, false, 0f, 0f, string.Empty, 1320f, 4);
            Assert.AreEqual("Haul: 1,320 gold (4 pieces)", many.HaulText);
        }

        /// <summary>
        /// The old system is still in the tree during the transition, and breaking a piece through
        /// it has to reach the value the zone now tallies.
        /// </summary>
        [Test]
        public void Test_BreakingThroughTheOldPickupStillRuinsTheValue()
        {
            var go = Track(new GameObject("Loot_Bridged"));
            var data = Track(ScriptableObject.CreateInstance<LootItem>());
            data.Worth = 500f;

            var pickup = go.AddComponent<LootPickup>();
            pickup.SetData(data);
            LootValue value = go.AddComponent<LootValue>();
            value.SetItem(data);

            ExtractionZone zone = MakeZone();
            zone.TrackLoot(value);
            Assert.AreEqual(500f, zone.WorthInZone, 0.001f);

            pickup.Break();

            Assert.AreEqual(0f, zone.WorthInZone, 0.001f,
                "Breaking through LootPickup must ruin the LootValue the zone tallies.");
        }
    }
}
