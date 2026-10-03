using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Castle;
using Plunderspell.Guards;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Issue #154: a bigger lobby meets more guards, and tougher ones, so a four-player raid is not
    /// tuned for one. See docs/4-systems/raid.md, "Lobby size".
    /// </summary>
    public class LobbyScalingTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        [Test]
        public void Test_ASoloRaidIsUnscaled()
        {
            Assert.AreEqual(1f, GuardSpawner.LobbyScale(1, 0.35f));
            Assert.AreEqual(1f, GuardSpawner.LobbyScale(0, 0.35f), "An empty count is treated as solo.");
            Assert.AreEqual(2.05f, GuardSpawner.LobbyScale(4, 0.35f), 1e-4f);
        }

        [Test]
        public void Test_FourPlayersMeetMoreGuardsThanOne()
        {
            var go = new GameObject("CastleGen");
            _spawned.Add(go);
            var generator = go.AddComponent<ProceduralCastleGenerator>();

            int solo = 0, four = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                solo += GuardPlacementPlanner.Plan(castle, seed, GuardSpawner.LobbyScale(1, 0.35f)).Count;
                four += GuardPlacementPlanner.Plan(castle, seed, GuardSpawner.LobbyScale(4, 0.35f)).Count;
            }
            Assert.Greater(four, solo * 1.5f, $"Four players met {four} guards over 20 castles, one player {solo}.");
        }

        [Test]
        public void Test_GuardHealthScalesAndStartsFull()
        {
            var go = new GameObject("Guard");
            _spawned.Add(go);
            go.AddComponent<BoxCollider>();
            var guard = go.AddComponent<Guard>();
            float solo = guard.MaxHealth;

            guard.ScaleHealth(GuardSpawner.LobbyScale(4, 0.25f));

            Assert.AreEqual(solo * 1.75f, guard.MaxHealth, 1e-3f);
            Assert.AreEqual(guard.MaxHealth, guard.CurrentHealth, 1e-3f, "A scaled guard starts at full health.");
        }
    }
}
