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

        private static bool HasCellNear(CastleNavTile tile, float height)
        {
            for (int i = 0; i < tile.Walkable.Length; i++)
                if (tile.Walkable[i] != 0 && System.Math.Abs(tile.HeightCm[i] / CastleNavTile.HeightScale - height) < 0.05f)
                    return true;
            return false;
        }
    }
}
