using System.Collections.Generic;
using NUnit.Framework;
using RogueAi.Castle;
using RogueAi.Extraction;
using RogueAi.Inventory;
using RogueAi.Lair;
using RogueAi.Loot;
using RogueAi.Raid;
using UnityEngine;

namespace RogueAi.Tests
{
    /// <summary>
    /// Tests for the core loop: Lair → castle → haul → extraction → debt, and the determinism that
    /// lets four peers play the same raid from one replicated seed.
    /// </summary>
    public class RaidLoopTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey("TotalDebt");
            PlayerPrefs.DeleteKey("AccumulatedGold");
            PlayerPrefs.DeleteKey("SelectedEra");
        }

        [TearDown]
        public void TearDown()
        {
            RogueAi.Guards.CastleGuard.ClearIntruders();
            foreach (Object o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _spawned.Add(o);
            return o;
        }

        // --- Fixtures ----------------------------------------------------------------------

        private ProceduralCastleGenerator MakeGenerator()
        {
            var go = Track(new GameObject("CastleGen"));
            return go.AddComponent<ProceduralCastleGenerator>();
        }

        /// <summary>A loot table with one cheap entry per zone plus a rich crypt relic.</summary>
        private RaidLootTable MakeTable()
        {
            var table = Track(ScriptableObject.CreateInstance<RaidLootTable>());

            foreach (CastleZone zone in System.Enum.GetValues(typeof(CastleZone)))
            {
                table.Entries.Add(new RaidLootTable.Entry
                {
                    Item = MakeItem($"{zone} Trinket", 25f),
                    Zone = zone,
                    Weight = 10
                });
            }

            table.Entries.Add(new RaidLootTable.Entry
            {
                Item = MakeItem("Crypt Relic", 500f),
                Zone = CastleZone.Crypt,
                Weight = 1
            });

            return table;
        }

        private LootItem MakeItem(string name, float worth)
        {
            var item = Track(ScriptableObject.CreateInstance<LootItem>());
            item.DisplayName = name;
            item.Worth = worth;
            item.Bulk = 2f;
            item.Fragility = 8f;
            return item;
        }

        private RaidDirector MakeDirector(out LairHubManager lair, out ExtractionZone zone,
            out LootSpawner spawner)
        {
            var lairGo = Track(new GameObject("Lair"));
            lair = lairGo.AddComponent<LairHubManager>();

            var zoneGo = Track(new GameObject("ExtractionZone"));
            zoneGo.AddComponent<BoxCollider>().isTrigger = true;
            zone = zoneGo.AddComponent<ExtractionZone>();

            var spawnerGo = Track(new GameObject("LootSpawner"));
            spawner = spawnerGo.AddComponent<LootSpawner>();
            spawner.Table = MakeTable();

            var directorGo = Track(new GameObject("RaidDirector"));
            var director = directorGo.AddComponent<RaidDirector>();
            director.Configure(MakeGenerator(), spawner, zone, lair);
            return director;
        }

        // --- Phases ------------------------------------------------------------------------

        [Test]
        public void Test_ARaidStartsInTheLairAndEndsBackInIt()
        {
            RaidDirector director = MakeDirector(out _, out ExtractionZone zone, out _);
            Assert.AreEqual(RaidPhase.InLair, director.Phase, "A session begins in the Lair.");

            director.SetFixedSeed(4242);
            director.StartRaid(HistoricalEra.HighMedieval);
            Assert.AreEqual(RaidPhase.Raiding, director.Phase);

            zone.ResolveLocally();
            Assert.AreEqual(RaidPhase.Resolved, director.Phase, "Extraction resolves the raid.");

            director.ReturnToLair();
            Assert.AreEqual(RaidPhase.InLair, director.Phase);
        }

        [Test]
        public void Test_StartingARaidTwiceIsIgnored()
        {
            RaidDirector director = MakeDirector(out _, out _, out _);
            director.SetFixedSeed(11);
            director.StartRaid(HistoricalEra.BronzeAge);
            int seed = director.Seed;

            director.StartRaid(HistoricalEra.AgeOfPowder);

            Assert.AreEqual(seed, director.Seed, "A second StartRaid must not rebuild the castle mid-raid.");
        }

        [Test]
        public void Test_RaidBuildsAWalkableCastle()
        {
            RaidDirector director = MakeDirector(out _, out _, out _);
            director.SetFixedSeed(31337);
            director.StartRaid(HistoricalEra.BronzeAge);

            Assert.IsNotNull(director.Castle, "A raid must have a castle.");
            Assert.IsTrue(CastlePathValidator.ValidatePath(director.Castle, out _),
                "A raid must never start in a castle you cannot walk out of.");
        }

        // --- Determinism -------------------------------------------------------------------

        [Test]
        public void Test_SameSeedYieldsTheSameHaul()
        {
            RaidLootTable table = MakeTable();
            ProceduralCastleData castle = MakeGenerator().Generate(777);

            List<LootPlacement> a = LootPlacementPlanner.Plan(castle, table, 777);
            List<LootPlacement> b = LootPlacementPlanner.Plan(castle, table, 777);

            Assert.AreEqual(a.Count, b.Count, "The same seed must plan the same number of pieces.");
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].ModuleIndex, b[i].ModuleIndex);
                Assert.AreEqual(a[i].Entry.Item.DisplayName, b[i].Entry.Item.DisplayName);
                Assert.AreEqual(a[i].Position.x, b[i].Position.x, 0.0001f);
                Assert.AreEqual(a[i].Position.z, b[i].Position.z, 0.0001f);
            }
        }

        [Test]
        public void Test_DifferentSeedsYieldDifferentHauls()
        {
            RaidLootTable table = MakeTable();
            ProceduralCastleGenerator generator = MakeGenerator();

            List<LootPlacement> a = LootPlacementPlanner.Plan(generator.Generate(1), table, 1);
            List<LootPlacement> b = LootPlacementPlanner.Plan(generator.Generate(2), table, 2);

            bool identical = a.Count == b.Count;
            if (identical)
            {
                for (int i = 0; i < a.Count; i++)
                {
                    if (a[i].ModuleIndex != b[i].ModuleIndex || a[i].Position != b[i].Position)
                    {
                        identical = false;
                        break;
                    }
                }
            }

            Assert.IsFalse(identical, "Two different seeds must not produce the identical raid.");
        }

        // --- Placement rules ---------------------------------------------------------------

        [Test]
        public void Test_TheCryptAlwaysHoldsItsRelic()
        {
            RaidLootTable table = MakeTable();
            ProceduralCastleGenerator generator = MakeGenerator();

            for (int seed = 1; seed <= 20; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                List<LootPlacement> plan = LootPlacementPlanner.Plan(castle, table, seed);

                bool cryptLooted = false;
                foreach (LootPlacement p in plan)
                {
                    if (p.ModuleIndex != castle.CryptStartIndex)
                        continue;
                    cryptLooted = true;
                    Assert.AreEqual("Crypt Relic", p.Entry.Item.DisplayName,
                        $"Seed {seed}: the crypt chamber must hold the richest crypt entry.");
                }

                Assert.IsTrue(cryptLooted, $"Seed {seed}: the crypt final chamber must always be looted.");
            }
        }

        [Test]
        public void Test_TheExtractionRoomIsNeverLooted()
        {
            RaidLootTable table = MakeTable();
            ProceduralCastleGenerator generator = MakeGenerator();

            for (int seed = 1; seed <= 20; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                foreach (LootPlacement p in LootPlacementPlanner.Plan(castle, table, seed))
                {
                    Assert.AreNotEqual(castle.ExtractionExitIndex, p.ModuleIndex,
                        $"Seed {seed}: loot at the exit would delete the carry, which is the game.");
                }
            }
        }

        [Test]
        public void Test_DeeperZonesAreLootedMoreOften()
        {
            RaidLootTable table = MakeTable();
            ProceduralCastleGenerator generator = MakeGenerator();

            int keepRooms = 0, keepLooted = 0, wallRooms = 0, wallLooted = 0;

            for (int seed = 1; seed <= 40; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                var lootedModules = new HashSet<int>();
                foreach (LootPlacement p in LootPlacementPlanner.Plan(castle, table, seed))
                    lootedModules.Add(p.ModuleIndex);

                for (int i = 0; i < castle.PlacedModules.Count; i++)
                {
                    CastleZone zone = castle.PlacedModules[i].Zone;
                    if (zone == CastleZone.Keep)
                    {
                        keepRooms++;
                        if (lootedModules.Contains(i)) keepLooted++;
                    }
                    else if (zone == CastleZone.CurtainWall)
                    {
                        wallRooms++;
                        if (lootedModules.Contains(i)) wallLooted++;
                    }
                }
            }

            Assert.Greater(keepRooms, 0);
            Assert.Greater(wallRooms, 0);
            Assert.Greater(keepLooted / (float)keepRooms, wallLooted / (float)wallRooms,
                "The Keep must be richer than the curtain wall, or there is no reason to go in.");
        }

        // --- Economy -----------------------------------------------------------------------

        [Test]
        public void Test_ExtractedWorthPaysDownTheDebt()
        {
            RaidDirector director = MakeDirector(out LairHubManager lair, out _, out _);
            director.SetFixedSeed(5);
            director.StartRaid(HistoricalEra.BronzeAge);

            float debtAtStart = lair.TotalDebt;
            director.ApplyResult(200f, 2);

            Assert.Less(lair.TotalDebt, debtAtStart, "Extracted worth must reduce the debt.");
            Assert.AreEqual(200f, director.LastWorthExtracted, 0.001f);
            Assert.AreEqual(2, director.LastPlayersSaved);
        }

        [Test]
        public void Test_SettingOutRaisesTheDebt()
        {
            RaidDirector director = MakeDirector(out LairHubManager lair, out _, out _);
            float before = lair.TotalDebt;

            director.SetFixedSeed(9);
            director.StartRaid(HistoricalEra.BronzeAge);

            Assert.Greater(lair.TotalDebt, before,
                "Debt grows every time you set out — that is the campaign clock.");
        }

        [Test]
        public void Test_AnEmptyExtractionStillEndsTheRaid()
        {
            RaidDirector director = MakeDirector(out _, out ExtractionZone zone, out _);
            director.SetFixedSeed(6);
            director.StartRaid(HistoricalEra.BronzeAge);

            zone.ResolveLocally();

            Assert.AreEqual(RaidPhase.Resolved, director.Phase);
            Assert.AreEqual(0f, director.LastWorthExtracted, 0.001f,
                "Leaving with nothing is a valid, and very bad, outcome.");
        }

        [Test]
        public void Test_OnlyLootInsideTheZoneCounts()
        {
            RaidDirector director = MakeDirector(out _, out ExtractionZone zone, out _);
            director.SetFixedSeed(7);
            director.StartRaid(HistoricalEra.BronzeAge);

            LootPickup carried = MakePickup(MakeItem("Chalice", 120f));
            MakePickup(MakeItem("Left Behind", 900f)); // never tracked into the zone

            zone.TrackLoot(carried);
            zone.ResolveLocally();

            Assert.AreEqual(120f, director.LastWorthExtracted, 0.001f,
                "Loot left in the castle is loot lost.");
        }

        [Test]
        public void Test_BrokenLootIsWorthNothing()
        {
            RaidDirector director = MakeDirector(out _, out ExtractionZone zone, out _);
            director.SetFixedSeed(8);
            director.StartRaid(HistoricalEra.BronzeAge);

            LootPickup intact = MakePickup(MakeItem("Intact", 100f));
            LootPickup smashed = MakePickup(MakeItem("Smashed", 100f));
            smashed.Break();

            zone.TrackLoot(intact);
            zone.TrackLoot(smashed);
            zone.ResolveLocally();

            Assert.AreEqual(100f, director.LastWorthExtracted, 0.001f,
                "A shattered treasure pays nothing, however far it was carried.");
        }

        private LootPickup MakePickup(LootItem data)
        {
            var go = Track(new GameObject($"Pickup_{data.DisplayName}"));
            var pickup = go.AddComponent<LootPickup>();
            pickup.SetData(data);
            return pickup;
        }

        // --- Spawning ----------------------------------------------------------------------

        [Test]
        public void Test_ASecondRaidIsWinnableToo()
        {
            RaidDirector director = MakeDirector(out _, out ExtractionZone zone, out _);

            director.SetFixedSeed(101);
            director.StartRaid(HistoricalEra.BronzeAge);
            zone.ResolveLocally();
            director.ReturnToLair();

            director.SetFixedSeed(202);
            director.StartRaid(HistoricalEra.BronzeAge);

            Assert.IsFalse(zone.ExtractionComplete,
                "The zone must be re-armed, or the second raid can never be left.");
            Assert.Greater(zone.TimeRemaining, 0f, "…and its clock restarted.");

            LootPickup haul = MakePickup(MakeItem("Second Raid Haul", 300f));
            zone.TrackLoot(haul);
            zone.ResolveLocally();

            Assert.AreEqual(300f, director.LastWorthExtracted, 0.001f,
                "A second raid must pay out like the first.");
        }

        [Test]
        public void Test_PlayersStayVisibleToGuardsAcrossRaids()
        {
            RaidDirector director = MakeDirector(out _, out ExtractionZone zone, out _);

            var playerGo = Track(new GameObject("Player"));
            playerGo.AddComponent<RogueAi.Guards.IntruderTag>();

            director.SetFixedSeed(55);
            director.StartRaid(HistoricalEra.BronzeAge);
            zone.ResolveLocally();

            Assert.Contains(playerGo.transform,
                (System.Collections.ICollection)RogueAi.Guards.CastleGuard.Intruders,
                "A surviving player must still be visible to guards in the next raid.");
        }

        [Test]
        public void Test_TheGarrisonIsDeterministicAndAvoidsTheExit()
        {
            var generator = MakeGenerator();

            for (int seed = 1; seed <= 15; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);

                List<GuardPlacement> a = GuardPlacementPlanner.Plan(castle, seed);
                List<GuardPlacement> b = GuardPlacementPlanner.Plan(castle, seed);

                Assert.AreEqual(a.Count, b.Count, $"Seed {seed}: the garrison must be deterministic.");

                foreach (GuardPlacement g in a)
                {
                    Assert.AreNotEqual(castle.ExtractionExitIndex, g.ModuleIndex,
                        $"Seed {seed}: a guard on the exit turns every raid into the same fight.");
                    Assert.Greater(g.PatrolRoute.Count, 0, "Every guard must have somewhere to walk.");
                }
            }
        }

        [Test]
        public void Test_GuardsAreDenserDeeperIn()
        {
            Assert.Greater(GuardPlacementPlanner.DensityFor(CastleZone.Keep),
                GuardPlacementPlanner.DensityFor(CastleZone.CurtainWall),
                "The rooms worth entering must be the rooms being watched.");
            Assert.AreEqual(0f, GuardPlacementPlanner.DensityFor(CastleZone.Keep, 0f),
                "Density scale 0 must disable the garrison entirely.");
        }

        [Test]
        public void Test_SpawnedLootMatchesThePlanAndIsClearedAfterTheRaid()
        {
            RaidDirector director = MakeDirector(out _, out ExtractionZone zone, out LootSpawner spawner);
            director.SetFixedSeed(1234);
            director.StartRaid(HistoricalEra.BronzeAge);

            Assert.Greater(spawner.LastPlan.Count, 0, "A raid must have loot in it.");
            Assert.AreEqual(spawner.LastPlan.Count, spawner.Spawned.Count,
                "Every planned piece must actually exist in the world.");

            zone.ResolveLocally();
            Assert.AreEqual(0, spawner.Spawned.Count, "Loot must not survive into the next raid.");
        }
    }
}
