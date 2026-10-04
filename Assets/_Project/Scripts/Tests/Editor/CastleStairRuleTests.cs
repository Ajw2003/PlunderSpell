using NUnit.Framework;
using Plunderspell.Castle;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>A stair module opens one way on its exit level and like any room on its lobby level (#247).</summary>
    public class CastleStairRuleTests
    {
        private static ProceduralCastleData.PlacedModule UpStair()
        {
            return new ProceduralCastleData.PlacedModule("StairUp", Vector3.zero, Quaternion.identity,
                CastleZone.InnerWard, new Vector2Int(-1, 0))
            {
                Level = CastleLevels.Ground, Storeys = 2, ExitLevel = CastleLevels.Keep, ExitFacing = Vector2Int.right
            };
        }

        [Test]
        public void ExitLevel_OpensOnlyTowardItsFacing()
        {
            var stair = UpStair();
            Assert.IsTrue(CastleStairRule.Opens(stair, CastleLevels.Keep, Vector2Int.right));
            Assert.IsFalse(CastleStairRule.Opens(stair, CastleLevels.Keep, Vector2Int.up));
            Assert.IsFalse(CastleStairRule.Opens(stair, CastleLevels.Keep, Vector2Int.left));
        }

        [Test]
        public void LobbyLevel_OpensEveryWay_AndAnOrdinaryRoomAlways()
        {
            var stair = UpStair();
            Assert.IsTrue(CastleStairRule.Opens(stair, CastleLevels.Ground, Vector2Int.left));
            var room = new ProceduralCastleData.PlacedModule("Hall", Vector3.zero, Quaternion.identity,
                CastleZone.Keep, Vector2Int.zero) { Level = CastleLevels.Keep };
            Assert.IsTrue(CastleStairRule.Opens(room, CastleLevels.Keep, Vector2Int.down));
            Assert.AreEqual(CastleLevels.Keep, room.TopLevel);
            Assert.AreEqual(CastleLevels.Keep, stair.TopLevel);
        }

        [Test]
        public void LevelHeights_MatchTheDesign()
        {
            Assert.AreEqual(0f, CastleLevels.RootY(CastleLevels.Ground));
            Assert.AreEqual(4.3f, CastleLevels.RootY(CastleLevels.Keep), 1e-4f);
            Assert.AreEqual(-3.3f, CastleLevels.RootY(CastleLevels.Crypt), 1e-4f);
            Assert.AreEqual(0, CastleLevels.Index(CastleLevels.Crypt));
            Assert.AreEqual(2, CastleLevels.Index(CastleLevels.Keep));
        }
    }
}
