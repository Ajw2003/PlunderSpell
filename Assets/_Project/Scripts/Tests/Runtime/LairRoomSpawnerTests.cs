using NUnit.Framework;
using Plunderspell.Core;
using Plunderspell.Raid;

namespace Plunderspell.Tests
{
    /// <summary>Resuming from the pause menu leaves the player where they stood; every other way in is an arrival (#309, #359).</summary>
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
        public void ResumingFromThePauseMenuDoesNotMoveThePlayer()
        {
            Assert.IsFalse(LairRoomSpawner.IsArrival(GameState.Paused));
        }

        [Test]
        public void TheLairAndAPauseBegunThereAreSafeARaidIsNot()
        {
            var states = new GameStateManager();
            states.ChangeState(GameState.LairRoom);
            Assert.IsTrue(states.InSafePlace);
            states.ChangeState(GameState.Paused);
            Assert.IsTrue(states.InSafePlace);
            states.ChangeState(GameState.Playing);
            states.ChangeState(GameState.Paused);
            Assert.IsFalse(states.InSafePlace);
        }

        [Test]
        public void PausingInTheLairResumesToTheLairAndPausingInARaidToTheRaid()
        {
            var states = new GameStateManager();
            states.ChangeState(GameState.LairRoom);
            states.ChangeState(GameState.Paused);
            states.ChangeState(GameState.Settings);
            states.ChangeState(GameState.Paused);
            Assert.AreEqual(GameState.LairRoom, states.PausedFrom, "Settings in between must not forget where the pause began.");

            states.ChangeState(GameState.Playing);
            states.ChangeState(GameState.Paused);
            Assert.AreEqual(GameState.Playing, states.PausedFrom);
        }

        [Test]
        public void TheRaidHudDrawsOverARaidPauseButNotOverTheLair()
        {
            var states = new GameStateManager();
            states.ChangeState(GameState.LairRoom);
            Assert.IsFalse(states.RaidOnScreen);
            states.ChangeState(GameState.Paused);
            Assert.IsFalse(states.RaidOnScreen, "paused in the Lair: the raid clock and alarm showed behind the pause menu");

            states.ChangeState(GameState.Playing);
            Assert.IsTrue(states.RaidOnScreen);
            states.ChangeState(GameState.Paused);
            Assert.IsTrue(states.RaidOnScreen);
        }
    }
}
