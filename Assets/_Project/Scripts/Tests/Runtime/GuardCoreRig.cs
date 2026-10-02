using System.Collections.Generic;
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
            GuardArrivalGrace.End();
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
