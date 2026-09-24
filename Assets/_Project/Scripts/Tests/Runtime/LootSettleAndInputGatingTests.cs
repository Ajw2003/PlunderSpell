using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Player;
using Plunderspell.Core;
using Plunderspell.UI;
using RogueAi.Castle;
using RogueAi.Loot;
using RogueAi.Playtest;
using RogueAi.Raid;
using StateMachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace RogueAi.Tests
{
    /// <summary>
    /// Tests for the three things that make the raid playable rather than merely running: loot that
    /// stays where it was planned instead of being flung out of the castle, input that stops at the
    /// edge of a menu, and a cursor that is where the player expects it.
    /// </summary>
    public class LootSettleAndInputGatingTests
    {
        /// <summary>How far a spawned item may drift from its planned spot before it counts as flung.</summary>
        private const float k_flungDistance = 1f;

        private readonly List<Object> m_spawned = new List<Object>();
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
            foreach (Object o in m_spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            m_spawned.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            m_spawned.Add(o);
            return o;
        }

        // --- Issue 20: loot settles instead of exploding -----------------------------------------

        private RaidLootTable MakeTable()
        {
            var table = Track(ScriptableObject.CreateInstance<RaidLootTable>());

            foreach (CastleZone zone in System.Enum.GetValues(typeof(CastleZone)))
            {
                var item = Track(ScriptableObject.CreateInstance<LootItem>());
                item.DisplayName = $"{zone} Trinket";
                item.Bulk = 3f;
                item.Worth = 50f;

                table.Entries.Add(new RaidLootTable.Entry
                {
                    Item = item,
                    Zone = zone,
                    Weight = 1,
                });
            }

            return table;
        }

        private LootSpawner MakeSpawner(out ProceduralCastleData castle)
        {
            var generatorGo = Track(new GameObject("CastleGen"));
            castle = generatorGo.AddComponent<ProceduralCastleGenerator>().Generate(4242);

            var spawnerGo = Track(new GameObject("LootSpawner"));
            var spawner = spawnerGo.AddComponent<LootSpawner>();
            spawner.Table = MakeTable();
            return spawner;
        }

        /// <summary>A slab wide enough that anything flung sideways misses it and falls out of range.</summary>
        private void MakeFloor(float topY)
        {
            var go = Track(new GameObject("Floor"));
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(500f, 1f, 500f);
            go.transform.position = new Vector3(0f, topY - 0.5f, 0f);
        }

        [Test]
        public void Test_SpawnedLootIsFrozenUntilTheSceneHasSettled()
        {
            LootSpawner spawner = MakeSpawner(out ProceduralCastleData castle);

            spawner.SpawnFor(castle, 4242);

            Assert.IsNotEmpty(spawner.Spawned, "The fixture must actually spawn something to prove anything.");
            foreach (GameObject go in spawner.Spawned)
            {
                var body = go.GetComponent<Rigidbody>();
                Assert.IsNotNull(body, $"{go.name} has no rigidbody to freeze.");
                Assert.IsTrue(body.isKinematic,
                    $"{go.name} spawned live: a body overlapping a wall is depenetrated across the map.");
            }
        }

        [UnityTest]
        public IEnumerator Test_NoSpawnedLootEndsUpBelowTheFloorOrFarAboveIt()
        {
            LootSpawner spawner = MakeSpawner(out ProceduralCastleData castle);

            IReadOnlyList<LootPlacement> plan = spawner.SpawnFor(castle, 4242);
            var planned = new List<Vector3>();
            float lowestPlanned = float.MaxValue;
            float highestPlanned = float.MinValue;
            foreach (LootPlacement placement in plan)
            {
                planned.Add(placement.Position);
                lowestPlanned = Mathf.Min(lowestPlanned, placement.Position.y);
                highestPlanned = Mathf.Max(highestPlanned, placement.Position.y);
            }

            float floorTop = lowestPlanned - 0.5f;
            MakeFloor(floorTop);

            // The table has no prefabs, so the spawner builds bare objects. A rigidbody with no
            // collider falls through everything, which would prove nothing about settling.
            foreach (GameObject go in spawner.Spawned)
                go.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;

            spawner.ReleaseSpawned();

            // Long enough for a depenetration impulse to have thrown anything loose clean out of the
            // castle, had one been applied, and long enough for an honest drop to land.
            for (int i = 0; i < 60; i++)
                yield return new WaitForFixedUpdate();

            IReadOnlyList<GameObject> spawnedLoot = spawner.Spawned;
            Assert.AreEqual(planned.Count, spawnedLoot.Count);

            for (int i = 0; i < spawnedLoot.Count; i++)
            {
                Vector3 position = spawnedLoot[i].transform.position;
                string name = spawnedLoot[i].name;

                Assert.GreaterOrEqual(position.y, floorTop - 0.5f,
                    $"{name} fell through the floor to {position}.");
                Assert.LessOrEqual(position.y, highestPlanned + 1f,
                    $"{name} was launched to {position}.");

                var plannedFlat = new Vector2(planned[i].x, planned[i].z);
                var actualFlat = new Vector2(position.x, position.z);
                Assert.Less(Vector2.Distance(plannedFlat, actualFlat), k_flungDistance,
                    $"{name} left its planned spot {planned[i]} for {position}.");
            }
        }

        [Test]
        public void Test_SettlingHandsFreeLootBackToPhysicsButLeavesCarriedLootAlone()
        {
            LootSpawner spawner = MakeSpawner(out ProceduralCastleData castle);
            spawner.SpawnFor(castle, 4242);

            var carrierGo = Track(new GameObject("Carrier"));
            LootPickup carried = spawner.Spawned[0].GetComponent<LootPickup>();
            carried.PerformPickup(carrierGo.AddComponent<LootInteractor>());

            spawner.ReleaseSpawned();

            Assert.IsTrue(carried.GetComponent<Rigidbody>().isKinematic,
                "Releasing a carried item drops it out of the carrier's hand.");

            for (int i = 1; i < spawner.Spawned.Count; i++)
            {
                Assert.IsFalse(spawner.Spawned[i].GetComponent<Rigidbody>().isKinematic,
                    "Free loot must be handed back to physics so it can rest on the floor.");
            }
        }

        // --- Issue 9: input stops at the edge of a menu ------------------------------------------

        private FreeLookPlaytestController MakeWalker()
        {
            var go = Track(new GameObject("Walker"));
            go.AddComponent<Rigidbody>().useGravity = false;
            return go.AddComponent<FreeLookPlaytestController>();
        }

        [UnityTest]
        public IEnumerator Test_MovementIsIgnoredWhileAMenuIsOpenAndResumesInPlay()
        {
            FreeLookPlaytestController walker = MakeWalker();
            var body = walker.GetComponent<Rigidbody>();

            // Settings opened mid-play, not the pause menu: pausing freezes time (#107), and a frozen
            // world never reaches the physics steps this test waits on. The gate is the same for every menu.
            GameServices.GameState.ChangeState(GameState.Settings);
            body.linearVelocity = new Vector3(0f, 0f, 5f);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(5f, body.linearVelocity.z, 0.01f,
                "A paused game must not drive the body; the controller wrote to it anyway.");

            GameServices.GameState.ChangeState(GameState.Playing);

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            // With no input held the controller drives horizontal velocity to zero — which is only
            // observable if it is reading input again at all.
            Assert.AreEqual(0f, body.linearVelocity.z, 0.01f,
                "Returning to play must resume input without any restart logic.");
        }

        // --- Issue 8: the cursor follows the state -----------------------------------------------

        private CursorLockPolicy MakeCursorPolicy()
        {
            var go = Track(new GameObject("Cursor"));
            return go.AddComponent<CursorLockPolicy>();
        }

        [Test]
        public void Test_TheCursorIsCapturedOnlyWhilePlaying()
        {
            Assert.IsTrue(CursorLockPolicy.ShouldCapture(GameState.Playing));

            foreach (GameState state in new[]
                     {
                         GameState.MainMenu, GameState.Lair, GameState.Paused,
                         GameState.Settings,
                     })
            {
                Assert.IsFalse(CursorLockPolicy.ShouldCapture(state),
                    $"{state} is a screen the player clicks; the cursor must be theirs.");
            }
        }

        [Test]
        public void Test_TheCursorFollowsTheGameStateAndIsFreedByLosingFocus()
        {
            CursorLockPolicy policy = MakeCursorPolicy();

            GameServices.GameState.ChangeState(GameState.Playing);
            Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState);
            Assert.IsFalse(Cursor.visible);

            GameServices.GameState.ChangeState(GameState.Paused);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState);
            Assert.IsTrue(Cursor.visible);

            GameServices.GameState.ChangeState(GameState.Playing);
            policy.SetWindowFocused(false);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState,
                "Alt-tabbing must hand the cursor back to the desktop.");
            Assert.IsTrue(Cursor.visible);

            policy.SetWindowFocused(true);
            Assert.AreEqual(CursorLockMode.Locked, Cursor.lockState,
                "Coming back to a game still in play must re-capture the cursor.");

            GameServices.GameState.ChangeState(GameState.Paused);
            policy.SetWindowFocused(false);
            policy.SetWindowFocused(true);
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState,
                "Coming back to a pause menu must leave the cursor alone.");
        }

        // --- Issue 9, on the controller the raid actually uses ------------------------------------

        /// <summary>
        /// The raid's player, not the ItemGym harness: a body carrying the real
        /// <see cref="PlayerStateMachine"/> and <see cref="PlayerInputController"/>, with the camera
        /// transform the look path dereferences.
        /// </summary>
        private PlayerStateMachine MakeRaidPlayer()
        {
            var go = Track(new GameObject("RaidPlayer"));
            go.AddComponent<Rigidbody>().useGravity = false;

            var stateMachine = go.AddComponent<PlayerStateMachine>();
            stateMachine.CameraTransform = Track(new GameObject("Eye")).transform;
            stateMachine.CameraTransform.SetParent(go.transform, false);

            go.AddComponent<PlayerInputController>();
            return stateMachine;
        }

        /// <summary>
        /// Guards the specific regression this pass fixed: issue #9 was closed against
        /// <see cref="FreeLookPlaytestController"/>, which RaidScene does not contain, so the raid's
        /// own player kept walking behind an open menu.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_TheRaidPlayerStopsMovingWhileAMenuIsOpen()
        {
            PlayerStateMachine player = MakeRaidPlayer();

            GameServices.GameState.ChangeState(GameState.Playing);
            player.Move(new Vector2(0f, 1f));
            yield return null;

            Assert.AreEqual(1f, player.MovementDirection.y, 0.01f,
                "While playing, the gate must not touch movement the input system delivered.");

            GameServices.GameState.ChangeState(GameState.Paused);
            yield return null;

            Assert.AreEqual(Vector2.zero, player.MovementDirection,
                "A paused game must clear held movement; the last delivered value was kept instead.");

            GameServices.GameState.ChangeState(GameState.Playing);
            player.Move(new Vector2(0f, 1f));
            yield return null;

            Assert.AreEqual(1f, player.MovementDirection.y, 0.01f,
                "Returning to play must resume input without any restart logic.");
        }

        /// <summary>
        /// The same states the cursor rule uses, asserted against the input rule, so the two cannot
        /// drift into disagreeing about what "playing" means — a menu the cursor is free on but the
        /// player still walks behind is exactly what #9 was.
        /// </summary>
        [Test]
        public void Test_InputIsAcceptedOnlyWhilePlaying()
        {
            Assert.IsTrue(PlayerInputController.AcceptsInputIn(GameState.Playing));

            foreach (GameState state in new[]
                     {
                         GameState.MainMenu, GameState.Lair, GameState.Paused,
                         GameState.Settings,
                     })
            {
                Assert.IsFalse(PlayerInputController.AcceptsInputIn(state),
                    $"{state} is a screen the player clicks; the world must not react to input.");
                Assert.AreEqual(CursorLockPolicy.ShouldCapture(state),
                    PlayerInputController.AcceptsInputIn(state),
                    $"The cursor rule and the input rule disagree about {state}.");
            }
        }
    }
}
