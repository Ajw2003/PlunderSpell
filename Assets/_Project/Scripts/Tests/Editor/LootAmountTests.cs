using System.Collections.Generic;
using NUnit.Framework;
using RogueAi.Castle;
using RogueAi.Inventory;
using RogueAi.Raid;
using UnityEditor;
using UnityEngine;

namespace RogueAi.Tests.Editor
{
    /// <summary>
    /// How much loot a real raid holds, with the real era rooms, tables and generator: about double
    /// the old one-per-room haul, one item per anchor, and the crypt centre full.
    /// See docs/plans/staging-playtest-2-2026-09-24.md, part 2.
    /// </summary>
    public class LootAmountTests
    {
        private const int k_Seeds = 8;
        private ProceduralCastleGenerator _generator;

        // The test runner's own scene is a throwaway, so the castle is built there and removed after.
        [SetUp]
        public void SetUp() => _generator = new GameObject("TestGenerator").AddComponent<ProceduralCastleGenerator>();

        [TearDown]
        public void TearDown()
        {
            if (_generator == null)
                return;
            _generator.ClearGenerated();
            Object.DestroyImmediate(_generator.gameObject);
        }

        private static IEnumerable<(HistoricalEra, CastleRoomRegistry, RaidLootTable)> Eras()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<EraContentCatalogue>("Assets/_Project/Data/Eras/EraContentCatalogue.asset");
            var defaultRooms = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>("Assets/_Project/Data/Castle/CastleRoomRegistry.asset");
            foreach (HistoricalEra era in System.Enum.GetValues(typeof(HistoricalEra)))
            {
                EraContentCatalogue.Entry entry = catalogue.For(era);
                yield return (era, entry?.Rooms != null ? entry.Rooms : defaultRooms, entry.Loot);
            }
        }

        [Test]
        public void EachEraPlacesAboutDoubleTheOldHaulOneItemPerAnchor()
        {
            foreach ((HistoricalEra era, CastleRoomRegistry rooms, RaidLootTable loot) in Eras())
            {
                _generator.Registry = rooms;
                int total = 0;
                for (int seed = 1; seed <= k_Seeds; seed++)
                {
                    ProceduralCastleData castle = _generator.Generate(seed * 7919);
                    List<LootPlacement> plan = LootPlacementPlanner.Plan(castle, loot, seed * 7919, rooms);
                    total += plan.Count;

                    var perRoom = new Dictionary<int, List<Vector3>>();
                    foreach (LootPlacement p in plan)
                    {
                        if (!perRoom.TryGetValue(p.ModuleIndex, out List<Vector3> spots))
                            perRoom[p.ModuleIndex] = spots = new List<Vector3>();
                        foreach (Vector3 other in spots)
                            Assert.Greater(Vector3.Distance(other, p.Position), 0.01f,
                                $"{era} seed {seed}: two items share a spot in room {castle.PlacedModules[p.ModuleIndex].RoomId}.");
                        spots.Add(p.Position);
                    }

                    foreach (KeyValuePair<int, List<Vector3>> room in perRoom)
                    {
                        Vector3[] anchors = rooms.GetById(castle.PlacedModules[room.Key].RoomId)?.LootAnchors;
                        if (anchors != null && anchors.Length > 0)
                            Assert.LessOrEqual(room.Value.Count, anchors.Length,
                                $"{era} seed {seed}: {castle.PlacedModules[room.Key].RoomId} holds more items than it has anchors.");
                    }

                    Vector3[] cryptAnchors = rooms.GetById(castle.PlacedModules[castle.CryptStartIndex].RoomId)?.LootAnchors;
                    int cryptItems = perRoom.TryGetValue(castle.CryptStartIndex, out List<Vector3> crypt) ? crypt.Count : 0;
                    Assert.AreEqual(Mathf.Max(1, cryptAnchors?.Length ?? 0), cryptItems,
                        $"{era} seed {seed}: the crypt centre should fill every one of its anchors.");
                    if (cryptAnchors != null && cryptAnchors.Length > 0)
                    {
                        ProceduralCastleData.PlacedModule cryptRoom = castle.PlacedModules[castle.CryptStartIndex];
                        Vector3 first = cryptRoom.Position + cryptRoom.Rotation * cryptAnchors[0] + Vector3.up * LootPlacementPlanner.AnchorLift;
                        LootPlacement richest = plan.Find(p => p.ModuleIndex == castle.CryptStartIndex);
                        Assert.Less(Vector3.Distance(first, richest.Position), 0.01f,
                            $"{era} seed {seed}: the crypt's richest item should sit on its first anchor.");
                    }
                    _generator.ClearGenerated();
                }

                float perRaid = total / (float)k_Seeds;
                Assert.GreaterOrEqual(perRaid, 40f,
                    $"{era}: {perRaid:F1} items per raid; the old one-per-room haul was about 22 and the target is about double.");
            }
        }
    }
}
