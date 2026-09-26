using System.Collections.Generic;
using RogueAi.Castle;
using RogueAi.Guards;
using UnityEngine;

namespace RogueAi.Raid
{
    /// <summary>
    /// Puts the garrison planned by <see cref="GuardPlacementPlanner"/> into the world. Like
    /// <see cref="LootSpawner"/>, the plan half is pure and the spawn half is server-only.
    /// </summary>
    public class GuardSpawner : MonoBehaviour
    {
        [Tooltip("Guard prefab. Without one the placements are data-only (planned, not spawned).")]
        [SerializeField] private GameObject _guardPrefab;

        [Tooltip("Scales every zone's guard density. 0 disables the garrison entirely.")]
        [Range(0f, 2f)]
        [SerializeField] private float _densityScale = 1f;

        [Tooltip("Parent for spawned guards. Auto-created if left null.")]
        [SerializeField] private Transform _container;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<GuardPlacement> _lastPlan = new List<GuardPlacement>();

        /// <summary>The garrison plan behind the guards currently in the world.</summary>
        public IReadOnlyList<GuardPlacement> LastPlan => _lastPlan;

        /// <summary>Guards currently spawned.</summary>
        public IReadOnlyList<GameObject> Spawned => _spawned;

        public float DensityScale { get => _densityScale; set => _densityScale = value; }
        public GameObject GuardPrefab { get => _guardPrefab; set => _guardPrefab = value; }

        /// <summary>Plans and spawns the garrison for a castle. Returns the plan.</summary>
        public IReadOnlyList<GuardPlacement> SpawnFor(ProceduralCastleData castle, int seed)
        {
            Clear();

            List<GuardPlacement> plan = GuardPlacementPlanner.Plan(castle, seed, _densityScale);
            _lastPlan.AddRange(plan);

            if (_guardPrefab == null)
                return _lastPlan;

            EnsureContainer();
            for (int i = 0; i < plan.Count; i++)
                Spawn(plan[i]);

            return _lastPlan;
        }

        private void Spawn(GuardPlacement placement)
        {
            GameObject go = Instantiate(_guardPrefab, placement.Position, Quaternion.identity, _container);
            _spawned.Add(go);

            var guard = go.GetComponent<CastleGuard>();
            if (guard == null)
                return;

            // Patrol waypoints are plain child transforms so the guard's inspector-facing route
            // field works identically whether a scene author or this spawner filled it in.
            var route = new List<Transform>(placement.PatrolRoute.Count);
            for (int i = 0; i < placement.PatrolRoute.Count; i++)
            {
                var point = new GameObject($"Waypoint_{i}");
                point.transform.SetParent(_container, false);
                point.transform.position = placement.PatrolRoute[i];
                route.Add(point.transform);
                _spawned.Add(point);
            }

            guard.Configure(null, route);
        }

        /// <summary>Removes the garrison. Called when a raid ends.</summary>
        public void Clear()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                GameObject go = _spawned[i];
                if (go == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
            }
            _spawned.Clear();
            _lastPlan.Clear();
        }

        private void EnsureContainer()
        {
            if (_container != null)
                return;
            var go = new GameObject("SpawnedGuards");
            go.transform.SetParent(transform, false);
            _container = go.transform;
        }
    }
}
