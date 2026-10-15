using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Plunderspell.Castle;
using Plunderspell.Raid;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>#420: every zone with a ground-floor room gets exactly one more guard, in every Age.</summary>
    public class GroundFloorExtraGuardTests
    {
        // The Age of Powder has no registry of its own; it builds from the default castle rooms.
        [TestCase("BronzeAge", "CastleRoomRegistry_BronzeAge")]
        [TestCase("HighMedieval", "CastleRoomRegistry")]
        [TestCase("LateMedieval", "CastleRoomRegistry_LateMedieval")]
        [TestCase("AgeOfPowder", "CastleRoomRegistry")]
        public void EachGroundFloorZoneGetsExactlyOneMoreGuard(string age, string registryName)
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(
                $"Assets/_Project/Data/Castle/{registryName}.asset");
            var go = new GameObject("ExtraGuardTestGenerator");
            try
            {
                var generator = go.AddComponent<ProceduralCastleGenerator>();
                generator.Registry = registry;
                int zonesChecked = 0;
                for (int seed = 1; seed <= 20; seed++)
                {
                    ProceduralCastleData castle = generator.Generate(seed);
                    List<GuardPlacement> before = GuardPlacementPlanner.Plan(castle, seed, 1f, -1, false);
                    List<GuardPlacement> after = GuardPlacementPlanner.Plan(castle, seed);
                    zonesChecked += AssertOneMorePerGroundZone(age, seed, castle, before, after);
                }
                generator.ClearGenerated();
                Assert.Greater(zonesChecked, 0, $"{age}: no ground-floor zone was ever checked");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static int AssertOneMorePerGroundZone(string age, int seed, ProceduralCastleData castle,
            List<GuardPlacement> before, List<GuardPlacement> after)
        {
            Assert.AreEqual(before.Count + after.Skip(before.Count).Count(), after.Count, $"{age} seed {seed}");
            for (int i = 0; i < before.Count; i++)
                Assert.AreEqual(before[i].ModuleIndex, after[i].ModuleIndex, $"{age} seed {seed}: old guard {i} moved");

            List<GuardPlacement> extras = after.Skip(before.Count).ToList();
            Assert.AreEqual(extras.Count, extras.Select(extra => extra.Zone).Distinct().Count(),
                $"{age} seed {seed}: two extras in one zone");
            foreach (GuardPlacement extra in extras)
            {
                ProceduralCastleData.PlacedModule room = castle.PlacedModules[extra.ModuleIndex];
                Assert.AreEqual(CastleLevels.Ground, room.Level, $"{age} seed {seed}: extra guard is upstairs");
            }
            Assert.AreEqual(0, after.Count(guard => castle.PlacedModules[guard.ModuleIndex].Level != CastleLevels.Ground)
                - before.Count(guard => castle.PlacedModules[guard.ModuleIndex].Level != CastleLevels.Ground),
                $"{age} seed {seed}: upper floors changed");
            return extras.Count;
        }
    }
}
