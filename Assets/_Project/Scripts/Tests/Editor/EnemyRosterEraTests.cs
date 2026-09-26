using System.Collections.Generic;
using NUnit.Framework;
using RogueAi.Castle;
using RogueAi.Inventory;
using RogueAi.Raid;
using UnityEngine;

namespace RogueAi.Tests.Editor
{
    /// <summary>
    /// Enemies spawn only in their own Age (docs/plans/artbible-enemies-in-engine.md, decision 2A,
    /// phase E3), with a logged fall-back rather than an empty room when an Age has nobody posted.
    /// </summary>
    public class EnemyRosterEraTests
    {
        private readonly List<Object> _tracked = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _tracked)
            {
                if (o != null)
                    Object.DestroyImmediate(o);
            }
            _tracked.Clear();
            RaidContext.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _tracked.Add(o);
            return o;
        }

        private GameObject Prefab(string name) => Track(new GameObject(name));

        private static EnemyRoster.Entry Entry(GameObject prefab, CastleZone zone, HistoricalEra era,
            int weight = 10, bool anyEra = false) =>
            new EnemyRoster.Entry
            {
                EnemyId = prefab.name,
                Zone = zone,
                Era = era,
                AnyEra = anyEra,
                Weight = weight,
                Prefab = prefab
            };

        /// <summary>One enemy per Age in every zone, named for its Age, plus an all-Age Crypt ghost.</summary>
        private EnemyRoster MakeRoster()
        {
            var roster = Track(ScriptableObject.CreateInstance<EnemyRoster>());
            foreach (HistoricalEra era in System.Enum.GetValues(typeof(HistoricalEra)))
            {
                foreach (CastleZone zone in System.Enum.GetValues(typeof(CastleZone)))
                    roster.Entries.Add(Entry(Prefab($"{era}_{zone}"), zone, era));
            }
            roster.Entries.Add(Entry(Prefab("Ghost"), CastleZone.Crypt, HistoricalEra.BronzeAge, 10, true));
            return roster;
        }

        [Test]
        public void Test_ThePickIsAlwaysFromTheRaidsOwnAge()
        {
            EnemyRoster roster = MakeRoster();
            var rng = new System.Random(7);

            foreach (CastleZone zone in System.Enum.GetValues(typeof(CastleZone)))
            {
                for (int i = 0; i < 50; i++)
                {
                    GameObject pick = roster.PickForZone(zone, HistoricalEra.LateMedieval, rng);
                    Assert.IsNotNull(pick);
                    Assert.IsTrue(pick.name == $"{HistoricalEra.LateMedieval}_{zone}" || pick.name == "Ghost",
                        $"A Late Medieval raid drew {pick.name} for {zone}.");
                }
            }
        }

        [Test]
        public void Test_AnAnyEraEntrySpawnsInEveryAge()
        {
            EnemyRoster roster = MakeRoster();
            foreach (HistoricalEra era in System.Enum.GetValues(typeof(HistoricalEra)))
            {
                List<EnemyRoster.Entry> pool = roster.EntriesFor(CastleZone.Crypt, era);
                Assert.IsTrue(pool.Exists(entry => entry.EnemyId == "Ghost"),
                    $"The supernatural Crypt enemies belong to no century, so {era} must draw them too.");
                Assert.AreEqual(2, pool.Count, $"{era}'s Crypt pool is its own enemy plus the ghost.");
            }
        }

        [Test]
        public void Test_AnAgeWithNobodyPostedFallsBackToTheZoneAndSaysSo()
        {
            var roster = Track(ScriptableObject.CreateInstance<EnemyRoster>());
            roster.Entries.Add(Entry(Prefab("HighOnly"), CastleZone.Keep, HistoricalEra.HighMedieval));

            GameObject pick = roster.PickForZone(CastleZone.Keep, HistoricalEra.BronzeAge, new System.Random(1));

            Assert.IsNotNull(pick, "An Age gap must not leave the room empty.");
            Assert.AreEqual("HighOnly", pick.name);
            CollectionAssert.Contains(roster.ReportedFallbacks, (CastleZone.Keep, HistoricalEra.BronzeAge),
                "Falling back must be reported (a logged warning), never silent.");

            roster.PickForZone(CastleZone.Keep, HistoricalEra.BronzeAge, new System.Random(2));
            Assert.AreEqual(1, roster.ReportedFallbacks.Count, "Reported once per zone and Age, not per guard.");

            roster.ResetWarnings();
            Assert.AreEqual(0, roster.ReportedFallbacks.Count, "Each raid reports its own gaps.");
        }

        [Test]
        public void Test_AZoneWithNobodyAtAllStillReturnsNull()
        {
            var roster = Track(ScriptableObject.CreateInstance<EnemyRoster>());
            roster.Entries.Add(Entry(Prefab("KeepOnly"), CastleZone.Keep, HistoricalEra.BronzeAge));

            Assert.IsNull(roster.PickForZone(CastleZone.Crypt, HistoricalEra.BronzeAge, new System.Random(1)),
                "No enemy in the zone in any Age: nothing to spawn, and the spawner's fallback prefab decides.");
        }

        [Test]
        public void Test_WeightsSetHowOftenAnEnemyTurnsUp()
        {
            var roster = Track(ScriptableObject.CreateInstance<EnemyRoster>());
            // The art-bible role weights: patrol 10, special 3.
            roster.Entries.Add(Entry(Prefab("Patrol"), CastleZone.OuterBailey, HistoricalEra.AgeOfPowder, 10));
            roster.Entries.Add(Entry(Prefab("Special"), CastleZone.OuterBailey, HistoricalEra.AgeOfPowder, 3));

            var rng = new System.Random(12345);
            int patrols = 0;
            const int draws = 13000;
            for (int i = 0; i < draws; i++)
            {
                if (roster.PickForZone(CastleZone.OuterBailey, HistoricalEra.AgeOfPowder, rng).name == "Patrol")
                    patrols++;
            }

            Assert.AreEqual(10.0 / 13.0, patrols / (double)draws, 0.02,
                "A patrol posting must turn up in proportion to its weight (10 of 13).");
        }

        /// <summary>The plan's E3 audit: a Bronze Age seed plan contains only Bronze Age enemies.</summary>
        [Test]
        public void Test_ABronzeAgeRaidIsGarrisonedOnlyByBronzeAgeEnemies()
        {
            EnemyRoster roster = MakeRoster();
            var generator = Track(new GameObject("CastleGen")).AddComponent<ProceduralCastleGenerator>();
            var spawner = Track(new GameObject("Guards")).AddComponent<GuardSpawner>();
            spawner.Roster = roster;

            int spawned = 0;
            for (int seed = 1; seed <= 5; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                spawner.SpawnFor(castle, seed, HistoricalEra.BronzeAge);

                foreach (GameObject guard in spawner.Spawned)
                {
                    spawned++;
                    string name = guard.name.Replace("(Clone)", string.Empty);
                    Assert.IsTrue(name.StartsWith(HistoricalEra.BronzeAge.ToString()) || name == "Ghost",
                        $"Seed {seed}: a Bronze Age raid spawned {name}.");
                }
                Assert.AreEqual(HistoricalEra.BronzeAge, spawner.LastEra);
            }

            spawner.Clear();
            Assert.Greater(spawned, 0, "No guard spawned in five seeds, so this asserted nothing.");
        }

        [Test]
        public void Test_TheRaidContextCarriesTheAgeToAnyoneWhoAsks()
        {
            Assert.IsFalse(RaidContext.HasCurrent, "No raid, no context.");
            RaidContext heard = default;
            System.Action<RaidContext> listener = context => heard = context;
            RaidContext.Published += listener;
            try
            {
                RaidContext.Publish(new RaidContext(99, HistoricalEra.AgeOfPowder));
            }
            finally
            {
                RaidContext.Published -= listener;
            }

            Assert.IsTrue(RaidContext.HasCurrent);
            Assert.AreEqual(HistoricalEra.AgeOfPowder, RaidContext.Current.Era);
            Assert.AreEqual(99, RaidContext.Current.Seed);
            Assert.AreEqual(RaidContext.Current, heard);

            RaidContext.Clear();
            Assert.IsFalse(RaidContext.HasCurrent);
        }
    }
}
