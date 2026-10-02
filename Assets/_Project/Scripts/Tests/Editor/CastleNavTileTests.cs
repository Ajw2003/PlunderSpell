using NUnit.Framework;
using Plunderspell.Castle;
using UnityEditor;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// Checks the baked nav tiles (Tools/Plunderspell/Bake Castle Nav Tiles) kept in the registry:
    /// a room has archway portals on all four sides, a sane walkable share, and the stairwell has
    /// stairs, meaning connected cells whose heights step up. Issue #220.
    /// </summary>
    public class CastleNavTileTests
    {
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";

        private static CastleNavTile Tile(string roomId)
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            CastleRoomModuleData entry = registry.GetById(roomId);
            Assert.That(entry, Is.Not.Null, roomId);
            Assert.That(entry.NavTile.IsBaked, Is.True, roomId + " has no baked nav tile");
            return entry.NavTile;
        }

        [Test]
        public void KitchenHasPortalsOnAllSidesAndSaneWalkableShare()
        {
            CastleNavTile t = Tile("KitchenRoom");
            Assert.That(t.PortalNorth.Length, Is.GreaterThan(0));
            Assert.That(t.PortalEast.Length, Is.GreaterThan(0));
            Assert.That(t.PortalSouth.Length, Is.GreaterThan(0));
            Assert.That(t.PortalWest.Length, Is.GreaterThan(0));

            int columns = 0;
            for (int i = 0; i < CastleNavTile.Size * CastleNavTile.Size; i++)
            {
                if (t.Walkable[i] != 0)
                    columns++;
            }
            float share = columns / (float)(CastleNavTile.Size * CastleNavTile.Size);
            Assert.That(share, Is.InRange(0.3f, 0.9f));
        }

        [Test]
        public void KeepStairwellHasStairStepsAndAGalleryLevel()
        {
            CastleNavTile t = Tile("KeepStairwell");
            int steps = 0;
            for (int z = 0; z < CastleNavTile.Size - 1; z++)
            {
                for (int x = 0; x < CastleNavTile.Size - 1; x++)
                {
                    if (!t.IsWalkable(0, x, z))
                        continue;
                    if (IsStep(t, x, z, x + 1, z) || IsStep(t, x, z, x, z + 1))
                        steps++;
                }
            }
            Assert.That(steps, Is.GreaterThan(0), "no connected height steps along X or Z");
            Assert.That(t.LevelCount, Is.EqualTo(2));
        }

        private static bool IsStep(CastleNavTile t, int x0, int z0, int x1, int z1)
        {
            if (!t.IsWalkable(0, x1, z1))
                return false;
            float d = System.Math.Abs(t.Height(0, x1, z1) - t.Height(0, x0, z0));
            return d > 0.05f && d <= CastleNavTile.StepHeight;
        }
    }
}
