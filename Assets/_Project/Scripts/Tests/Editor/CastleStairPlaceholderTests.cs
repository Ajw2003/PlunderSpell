using NUnit.Framework;
using Plunderspell.Castle;
using UnityEditor;

namespace Plunderspell.Tests.Editor
{
    /// <summary>Each Age's registry knows the two placeholder stairs, baked over both storeys (#247).</summary>
    public class CastleStairPlaceholderTests
    {
        [TestCase("CastleRoomRegistry")]
        [TestCase("CastleRoomRegistry_BronzeAge")]
        [TestCase("CastleRoomRegistry_LateMedieval")]
        public void RegistryHasBakedStairs(string name)
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>($"Assets/_Project/Data/Castle/{name}.asset");
            foreach (string id in new[] { ProceduralCastleGenerator.StairUpId, ProceduralCastleGenerator.StairDownId })
            {
                CastleRoomModuleData stair = registry.GetById(id);
                Assert.IsNotNull(stair, $"{name}: no {id}");
                Assert.IsNotNull(stair.Prefab, $"{name}: {id} has no prefab");
                Assert.IsTrue(stair.NavTile.IsBaked, $"{name}: {id} is not baked");
                Assert.IsTrue(HasCellNear(stair.NavTile, 0.30f), $"{name}: {id} has no walkable cell on its lower floor");
                Assert.IsTrue(HasCellNear(stair.NavTile, stair.UpperFloorHeight), $"{name}: {id} has no walkable cell on its upper floor");
                Assert.IsEmpty(registry.GetModulesForZone(CastleZone.InnerWard).FindAll(m => m.RoomId == id),
                    $"{name}: {id} must not be in the random room pool");
            }
        }

        [TestCase("CastleRoomRegistry")]
        [TestCase("CastleRoomRegistry_BronzeAge")]
        [TestCase("CastleRoomRegistry_LateMedieval")]
        public void RampConnectsLowerFloorToUpperFloor(string name)
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>($"Assets/_Project/Data/Castle/{name}.asset");
            foreach (string id in new[] { ProceduralCastleGenerator.StairUpId, ProceduralCastleGenerator.StairDownId })
            {
                CastleRoomModuleData stair = registry.GetById(id);
                float furthest = FloodFromLowestCell(stair.NavTile, stair.UpperFloorHeight, out string furthestCell);
                Assert.GreaterOrEqual(furthest, stair.UpperFloorHeight - CastleNavTile.StepHeight,
                    $"{name}: {id} ramp does not reach the upper floor; furthest height {furthest:F2} at {furthestCell}");
            }
        }

        // Walks 4-neighbours on any layer, stepping only up to StepHeight; returns the highest height reached.
        private static float FloodFromLowestCell(CastleNavTile tile, float upperFloor, out string furthestCell)
        {
            int size = CastleNavTile.Size;
            var seen = new bool[CastleNavTile.Layers * size * size];
            var stack = new System.Collections.Generic.Stack<(int layer, int x, int z)>();
            (int layer, int x, int z) start = (0, 0, 0);
            float lowest = float.MaxValue;
            for (int z = 0; z < size; z++)
                for (int x = 0; x < size; x++)
                    if (tile.IsWalkable(0, x, z) && tile.Height(0, x, z) < lowest)
                    {
                        lowest = tile.Height(0, x, z);
                        start = (0, x, z);
                    }
            stack.Push(start);
            seen[CastleNavTile.Index(0, start.x, start.z)] = true;
            float furthest = lowest;
            furthestCell = $"layer 0 x {start.x} z {start.z}";
            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                float h = tile.Height(cur.layer, cur.x, cur.z);
                if (h > furthest)
                {
                    furthest = h;
                    furthestCell = $"layer {cur.layer} x {cur.x} z {cur.z}";
                }
                for (int d = 0; d < 4; d++)
                {
                    int nx = cur.x + dx[d], nz = cur.z + dz[d];
                    if (nx < 0 || nz < 0 || nx >= size || nz >= size)
                        continue;
                    for (int layer = 0; layer < CastleNavTile.Layers; layer++)
                    {
                        int index = CastleNavTile.Index(layer, nx, nz);
                        if (seen[index] || !tile.IsWalkable(layer, nx, nz))
                            continue;
                        if (System.Math.Abs(tile.Height(layer, nx, nz) - h) > CastleNavTile.StepHeight)
                            continue;
                        seen[index] = true;
                        stack.Push((layer, nx, nz));
                    }
                }
            }
            return furthest;
        }

        private static bool HasCellNear(CastleNavTile tile, float height)
        {
            for (int i = 0; i < tile.Walkable.Length; i++)
                if (tile.Walkable[i] != 0 && System.Math.Abs(tile.HeightCm[i] / CastleNavTile.HeightScale - height) < 0.05f)
                    return true;
            return false;
        }
    }
}
