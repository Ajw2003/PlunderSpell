using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The planned doors for a fixed seed on each Age's registry (#248).</summary>
    public class CastleDoorPlannerTests
    {
        private const int Seed = 12345;
        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        // Pinned from the first run on each registry: (ward-bailey, keep, crypt) doors.
        [TestCase("CastleRoomRegistry", 10, 2, 1)]
        [TestCase("CastleRoomRegistry_BronzeAge", 10, 2, 1)]
        [TestCase("CastleRoomRegistry_LateMedieval", 10, 2, 1)]
        public void CountsPerZoneForTheSeed(string name, int ward, int keep, int crypt)
        {
            List<PlannedDoor> doors = Plan(name, out _);
            Assert.AreEqual(ward, doors.FindAll(d => d.Zone == CastleZone.InnerWard).Count, $"{name} ward doors");
            Assert.AreEqual(keep, doors.FindAll(d => d.Zone == CastleZone.Keep).Count, $"{name} keep doors");
            Assert.AreEqual(crypt, doors.FindAll(d => d.Zone == CastleZone.Crypt).Count, $"{name} crypt doors");
        }

        [TestCase("CastleRoomRegistry")]
        [TestCase("CastleRoomRegistry_BronzeAge")]
        [TestCase("CastleRoomRegistry_LateMedieval")]
        public void NoArchwayGetsTwoDoorsAndEachIsOpenOnBothSides(string name)
        {
            List<PlannedDoor> doors = Plan(name, out ProceduralCastleData data);
            var seen = new HashSet<Vector3>();
            foreach (PlannedDoor door in doors)
            {
                Assert.IsTrue(seen.Add(door.Position), $"{name}: two doors at {door.Position}");
                Assert.IsTrue(SitsOnOpenArchway(data, door), $"{name}: door at {door.Position} is not on an open archway");
            }
            // Same seed, same list: the planner must not depend on iteration order.
            List<PlannedDoor> again = CastleDoorPlanner.Plan(data);
            for (int i = 0; i < doors.Count; i++)
                Assert.AreEqual(doors[i].Position, again[i].Position);
        }

        private static List<PlannedDoor> Plan(string name, out ProceduralCastleData data)
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>($"Assets/_Project/Data/Castle/{name}.asset");
            var go = new GameObject("DoorPlannerTestGenerator");
            try
            {
                var generator = go.AddComponent<ProceduralCastleGenerator>();
                generator.Registry = registry;
                data = generator.Generate(Seed);
                generator.ClearGenerated();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
            return CastleDoorPlanner.Plan(data);
        }

        // Re-derives the sill from every module side and level, then asks the nav rule about both modules.
        private static bool SitsOnOpenArchway(ProceduralCastleData data, PlannedDoor door)
        {
            foreach (ProceduralCastleData.PlacedModule a in data.PlacedModules)
            {
                for (int level = a.Level; level <= a.TopLevel; level++)
                {
                    foreach (Vector2Int side in Sides)
                    {
                        Vector3 sill = new Vector3(a.Position.x, CastleLevels.RootY(level) + 0.3f, a.Position.z) + new Vector3(side.x, 0f, side.y) * 5.75f;
                        if ((sill - door.Position).sqrMagnitude > 0.0001f)
                            continue;
                        foreach (ProceduralCastleData.PlacedModule b in data.PlacedModules)
                        {
                            if (b.GridPosition == a.GridPosition + side && b.Level <= level && level <= b.TopLevel &&
                                CastleNavArchwayRule.IsOpen(data, a, b, level) && CastleNavArchwayRule.IsOpen(data, b, a, level))
                                return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
