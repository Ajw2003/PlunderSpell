using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Core;
using RogueAi.Guards;
using RogueAi.Playtest;
using RogueAi.Status;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueAi.Tests
{
    /// <summary>
    /// Tests for the combat bench: that it opens in a state the player can act in, and that spawning
    /// puts the asked-for enemies where they can be reached. See docs/systems/combat-bench.md.
    /// </summary>
    public class CombatBenchTests
    {
        private readonly List<Object> m_tracked = new List<Object>();
        private GameState m_stateBeforeTest;

        [SetUp]
        public void SetUp()
        {
            GameServices.Initialize();
            m_stateBeforeTest = GameServices.GameState.CurrentState;
        }

        [TearDown]
        public void TearDown()
        {
            GameServices.GameState.ChangeState(m_stateBeforeTest);
            CastleGuard.ClearIntruders();

            foreach (Object o in m_tracked)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }

            m_tracked.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            m_tracked.Add(o);
            return o;
        }

        private CombatBench MakeBench()
        {
            var go = Track(new GameObject("CombatBench"));
            return go.AddComponent<CombatBench>();
        }

        /// <summary>A stand-in for an enemy prefab: the components the bench actually touches.</summary>
        private GameObject MakeGuardPrefab()
        {
            var go = Track(new GameObject("TestGuard"));
            go.AddComponent<StatusEffectReceiver>();
            go.AddComponent<CastleGuard>();
            return go;
        }

        [UnityTest]
        public IEnumerator Test_TheBenchOpensInPlayNotInTheMainMenu()
        {
            GameServices.GameState.ChangeState(GameState.MainMenu);
            MakeBench();

            yield return null;

            Assert.IsTrue(GameServices.IsPlaying,
                "The bench must put itself into play; every input path gates on it, and the state " +
                "starts at MainMenu, so otherwise the body cannot move.");
        }

        [UnityTest]
        public IEnumerator Test_SpawnPlacesTheRequestedCount()
        {
            CombatBench bench = MakeBench();
            GameObject prefab = MakeGuardPrefab();
            yield return null;

            bench.Spawn(prefab, 3, GuardAlertState.Patrolling);

            Assert.AreEqual(3, bench.Spawned.Count);
            foreach (GameObject enemy in bench.Spawned)
            {
                Track(enemy);
                Assert.IsNotNull(enemy, "A spawned enemy went missing before it could be fought.");
            }
        }

        [UnityTest]
        public IEnumerator Test_SpawnedEnemiesRingTheOriginRatherThanStackingOnIt()
        {
            CombatBench bench = MakeBench();
            GameObject prefab = MakeGuardPrefab();
            yield return null;

            bench.Spawn(prefab, 4, GuardAlertState.Patrolling);

            var seen = new List<Vector3>();
            foreach (GameObject enemy in bench.Spawned)
            {
                Track(enemy);
                foreach (Vector3 other in seen)
                {
                    Assert.Greater(Vector3.Distance(enemy.transform.position, other), 0.5f,
                        "Enemies spawned on top of each other; physics will fling them apart.");
                }

                seen.Add(enemy.transform.position);
            }
        }

        [UnityTest]
        public IEnumerator Test_SpawnedGuardsStartInTheRequestedAlertState()
        {
            CombatBench bench = MakeBench();
            GameObject prefab = MakeGuardPrefab();
            yield return null;

            bench.Spawn(prefab, 1, GuardAlertState.Chasing);
            GameObject spawned = Track(bench.Spawned[0]);

            Assert.AreEqual(GuardAlertState.Chasing, spawned.GetComponent<CastleGuard>().State,
                "Starting a guard already chasing is the point: a target that fights back.");
        }

        [UnityTest]
        public IEnumerator Test_ClearRemovesEverythingTheBenchSpawned()
        {
            CombatBench bench = MakeBench();
            GameObject prefab = MakeGuardPrefab();
            yield return null;

            bench.Spawn(prefab, 3, GuardAlertState.Patrolling);
            bench.ClearSpawned();

            // Destroy is deferred to the end of the frame, so the objects are only really gone next frame.
            yield return null;

            Assert.AreEqual(0, bench.Spawned.Count, "Clear must empty the bench's own list.");
        }
    }
}
