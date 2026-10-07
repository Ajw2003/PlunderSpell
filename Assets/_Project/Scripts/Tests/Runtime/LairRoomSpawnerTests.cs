using NUnit.Framework;
using Plunderspell.Core;
using Plunderspell.Raid;

namespace Plunderspell.Tests
{
    /// <summary>Closing the Lair screen leaves the player where they stood; every other way in is an arrival (#309).</summary>
    public class LairRoomSpawnerTests
    {
        [TestCase(GameState.MainMenu)]
        [TestCase(GameState.Playing)]
        [TestCase(GameState.GameOver)]
        public void ComingHomeStandsThePlayerAtTheirSpawn(GameState previous)
        {
            Assert.IsTrue(LairRoomSpawner.IsArrival(previous));
        }

        [Test]
        public void ClosingTheLairScreenDoesNotMoveThePlayer()
        {
            Assert.IsFalse(LairRoomSpawner.IsArrival(GameState.Lair));
        }
    }
}
