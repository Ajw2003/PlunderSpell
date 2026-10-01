using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// A chasing guard must not walk its body into a player and shove them through a wall (#200).
    /// Built like a raid guard (agent and collider, no Rigidbody) against a player-shaped dynamic body.
    /// </summary>
    public class GuardShoveTests
    {
        private readonly List<Object> _spawned = new List<Object>();
        private readonly List<NavMeshDataInstance> _meshes = new List<NavMeshDataInstance>();

        [SetUp]
        public void SetUp() => CastleGuard.EndArrivalGrace();

        [TearDown]
        public void TearDown()
        {
            CastleGuard.ClearIntruders();
            foreach (NavMeshDataInstance mesh in _meshes)
                mesh.Remove();
            _meshes.Clear();
            foreach (Object o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _spawned.Add(o);
            return o;
        }

        [UnityTest]
        public IEnumerator Test_AChasingGuardDoesNotShoveAPlayerThroughAWall()
        {
            Assume.That(Application.isPlaying, "Needs physics and the navigation update, which only run in play mode.");

            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(30f, 0.2f, 30f);

            var sources = new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(30f, 0.2f, 30f),
                    transform = Matrix4x4.TRS(new Vector3(0f, -0.1f, 0f), Quaternion.identity, Vector3.one),
                    area = 0
                }
            };
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0), sources,
                new Bounds(Vector3.zero, new Vector3(32f, 4f, 32f)), Vector3.zero, Quaternion.identity);
            _meshes.Add(NavMesh.AddNavMeshData(data));

            // A thin wall at x = 8, the player's back against it.
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(8.15f, 1.5f, 0f);
            wall.transform.localScale = new Vector3(0.3f, 3f, 10f);
            const float wallFace = 8f;

            var player = Track(new GameObject("Player"));
            player.transform.position = new Vector3(7.55f, 0.05f, 0f);
            var playerCollider = player.AddComponent<CapsuleCollider>();
            playerCollider.radius = 0.4f;
            playerCollider.height = 1.8f;
            playerCollider.center = new Vector3(0f, 0.9f, 0f);
            var body = player.AddComponent<Rigidbody>();
            body.mass = 1f;                 // as the player prefab
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            CastleGuard.RegisterIntruder(player.transform);

            var guardGo = Track(new GameObject("Guard"));
            guardGo.transform.position = new Vector3(2f, 0f, 0f);
            guardGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var guardCollider = guardGo.AddComponent<CapsuleCollider>();
            guardCollider.radius = 0.39f;
            guardCollider.height = 1.8f;
            guardCollider.center = new Vector3(0f, 0.9f, 0f);
            var agent = guardGo.AddComponent<NavMeshAgent>();
            agent.radius = 0.39f;
            agent.height = 1.8f;
            agent.stoppingDistance = 0.8f;  // as the guard prefabs
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            var guard = guardGo.AddComponent<CastleGuard>();
            guard.Configure(null);
            Assert.IsTrue(agent.isOnNavMesh, "Test premise: the guard stands on the runtime NavMesh.");

            float closest = float.MaxValue;
            float furthestPlayerX = float.MinValue;
            float end = Time.time + 8f;
            while (Time.time < end)
            {
                yield return new WaitForFixedUpdate();
                Vector3 apart = guardGo.transform.position - player.transform.position;
                apart.y = 0f;
                closest = Mathf.Min(closest, apart.magnitude);
                furthestPlayerX = Mathf.Max(furthestPlayerX, player.transform.position.x);
            }

            Assert.AreEqual(GuardAlertState.Chasing, guard.State, "Test premise: the guard is chasing the player.");
            Assert.LessOrEqual(furthestPlayerX + playerCollider.radius, wallFace + 0.05f,
                "The player stays on the near side of the wall.");
            Assert.GreaterOrEqual(closest, guardCollider.radius + playerCollider.radius - 0.05f,
                "The guard's body never overlaps the player's.");
        }

        /// <summary>
        /// The #200 physics itself: the stop-short is bypassed (tiny attack range), so the guard is driven
        /// at chase speed straight into a player-shaped dynamic capsule (r 0.5) standing against a real
        /// 0.5 m wall. The player's centre must never pass the wall's near face less its radius.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_AGuardDrivenIntoAPlayerAgainstAThinWallCannotCrushThemThrough()
        {
            Assume.That(Application.isPlaying, "Needs physics and the navigation update, which only run in play mode.");

            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(30f, 0.2f, 30f);
            var sources = new List<NavMeshBuildSource>
            {
                new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(30f, 0.2f, 30f),
                    transform = Matrix4x4.TRS(new Vector3(0f, -0.1f, 0f), Quaternion.identity, Vector3.one),
                    area = 0
                }
            };
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0), sources,
                new Bounds(Vector3.zero, new Vector3(32f, 4f, 32f)), Vector3.zero, Quaternion.identity);
            _meshes.Add(NavMesh.AddNavMeshData(data));

            // 0.5 m wall, near face at x = 8.
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(8.25f, 1.5f, 0f);
            wall.transform.localScale = new Vector3(0.5f, 3f, 10f);
            const float wallFace = 8f;

            var player = Track(new GameObject("Player"));
            player.transform.position = new Vector3(7.5f, 0.05f, 0f);
            var playerCollider = player.AddComponent<CapsuleCollider>();
            playerCollider.radius = 0.5f;
            playerCollider.height = 2f;
            playerCollider.center = new Vector3(0f, 1f, 0f);
            var body = player.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            CastleGuard.RegisterIntruder(player.transform);

            var guardGo = Track(new GameObject("Guard"));
            guardGo.transform.position = new Vector3(3f, 0f, 0f);
            guardGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var guardCollider = guardGo.AddComponent<CapsuleCollider>();
            guardCollider.radius = 0.39f;
            guardCollider.height = 1.8f;
            guardCollider.center = new Vector3(0f, 0.9f, 0f);
            // Like the ManAtArms prefab: a body that CastleGuard.Awake makes kinematic.
            guardGo.AddComponent<Rigidbody>();
            var agent = guardGo.AddComponent<NavMeshAgent>();
            agent.radius = 0.39f;
            agent.height = 1.8f;
            agent.stoppingDistance = 0f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            var guard = guardGo.AddComponent<CastleGuard>();
            guard.Configure(null);
            // Bypass the stop-short so the push really happens, at the hue-and-cry chase speed.
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(CastleGuard).GetField("_attackRange", flags).SetValue(guard, 0.01f);
            typeof(CastleGuard).GetField("_chaseSpeed", flags).SetValue(guard, 6.3f);
            Assert.IsTrue(agent.isOnNavMesh, "Test premise: the guard stands on the runtime NavMesh.");

            float closest = float.MaxValue;
            float furthestPlayerX = float.MinValue;
            float end = Time.time + 6f;
            while (Time.time < end)
            {
                yield return new WaitForFixedUpdate();
                furthestPlayerX = Mathf.Max(furthestPlayerX, player.transform.position.x);
                Vector3 apart = guardGo.transform.position - player.transform.position;
                apart.y = 0f;
                closest = Mathf.Min(closest, apart.magnitude);
            }

            Assert.Less(closest, guardCollider.radius + playerCollider.radius + 0.3f,
                "Test premise: the guard was driven into contact with the player (closest " + closest + ").");
            Assert.LessOrEqual(furthestPlayerX, wallFace - playerCollider.radius + 0.05f,
                "The player's centre never crosses the wall's near face plus its radius (furthest centre x "
                + furthestPlayerX + ").");
        }
    }
}
