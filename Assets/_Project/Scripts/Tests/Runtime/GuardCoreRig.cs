using System.Collections.Generic;
using Interfaces;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>Builds fresh-core guards (#206) and their surroundings for the core tests, and cleans them up.</summary>
    internal sealed class GuardCoreRig
    {
        private readonly List<Object> _created = new List<Object>();

        public EnemyDirector Director { get; private set; }

        public void SetUp()
        {
            GuardArrivalGrace.End();
            TestDirector.Reset();
            Director = TestDirector.Ensure();
            Director.Navigation.SetMap(new FlatFloor());
        }

        public void TearDown()
        {
            TestDirector.Reset();
            foreach (Object created in _created)
            {
                if (created != null)
                    Object.DestroyImmediate(created);
            }
            _created.Clear();
            DestroyLeftoverProjectiles();
            GuardArrivalGrace.End();
        }

        // A shot or thrown stone only destroys itself on impact or after a delay of game time, which a test
        // does not run, so it would survive into the next test and block that guard's sweep at the spawn point
        // (a GuardStone left at the origin made 9 Chase and Combat tests fail, 2026-10-03).
        private static void DestroyLeftoverProjectiles()
        {
            foreach (NetworkedProjectile projectile in Object.FindObjectsByType<NetworkedProjectile>(FindObjectsSortMode.None))
                Object.DestroyImmediate(projectile.gameObject);
        }

        public Guard MakeGuard(Vector3 position)
        {
            var body = new GameObject("CoreGuard");
            _created.Add(body);
            body.transform.position = position;
            Guard guard = body.AddComponent<Guard>();
            guard.Configure(Director);
            return guard;
        }

        /// <summary>A player stand-in the guards register as an intruder, with a collider of its own.</summary>
        public Transform MakeIntruder(Vector3 position)
        {
            var body = new GameObject("Intruder");
            _created.Add(body);
            body.transform.position = position;
            body.AddComponent<CapsuleCollider>();
            Director.RegisterIntruder(body.transform);
            return body.transform;
        }

        public GameObject MakeWall(Vector3 position, Vector3 size)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _created.Add(wall);
            wall.transform.position = position;
            wall.transform.localScale = size;
            return wall;
        }

        /// <summary>Advances the director's navigation service as its Update would, for a whole number of steps.</summary>
        public void RunNavigation(Guard guard, int steps, float deltaTime)
        {
            for (int i = 0; i < steps; i++)
            {
                guard.Tick(deltaTime);
                Director.Navigation.Tick(deltaTime);
            }
        }

        /// <summary>
        /// Steps several guards together the way the game does: every guard looks (skipping the 12 Hz wait),
        /// ticks, then the director's navigation and attack turns advance once. Physics is told about moved
        /// transforms first, as the navigation sweep would otherwise hit stale positions.
        /// </summary>
        public void StepTogether(int steps, float deltaTime, params Guard[] guards)
        {
            for (int i = 0; i < steps; i++)
            {
                Physics.SyncTransforms();
                foreach (Guard guard in guards)
                {
                    guard.Sight.LookNext();
                    guard.Tick(deltaTime);
                }
                Director.Navigation.Tick(deltaTime);
                Director.AttackTurns.Tick(deltaTime);
            }
        }

        /// <summary>A player stand-in that can be hurt, so a test can count the damage guards deal.</summary>
        public Victim MakeVictim(Vector3 position)
        {
            Victim victim = MakeIntruder(position).gameObject.AddComponent<Victim>();
            return victim;
        }

        /// <summary>Health that only counts what it is dealt.</summary>
        public sealed class Victim : MonoBehaviour, IHealth
        {
            public float DamageTaken { get; private set; }
            public float CurrentHealth => 100000f - DamageTaken;
            public float MaxHealth => 100000f;
            public void TakeDamage(float damage) => DamageTaken += damage;
            public void TakeDamage(float damage, float impactVelocity) => DamageTaken += damage;
        }

        /// <summary>An empty floor at height zero where every point is walkable and a path is a straight line.</summary>
        private sealed class FlatFloor : IGuardNavigationMap
        {
            private const float WaypointSpacing = 0.5f;

            public int Version => 0;

            public int FindCell(Vector3 position) => Mathf.RoundToInt(position.x * 2f) * 4096 + Mathf.RoundToInt(position.z * 2f) + 1000000;

            public bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> path, out BlockedReason failure)
            {
                failure = BlockedReason.Unreachable;
                path.Clear();
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / WaypointSpacing));
                for (int i = 0; i <= steps; i++)
                    path.Add(Vector3.Lerp(from, to, i / (float)steps));
                return true;
            }

            public bool IsWalkClear(Vector3 from, Vector3 to, float halfWidth) => true;

            public bool TryGetFloorHeight(Vector3 position, out float floorHeight)
            {
                floorHeight = 0f;
                return true;
            }
        }
    }
}
