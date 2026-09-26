using System.Collections.Generic;
using Plunderspell.Castle;
using Plunderspell.Guards;
using Plunderspell.Inventory;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Puts the garrison planned by <see cref="GuardPlacementPlanner"/> into the world. Like
    /// <see cref="LootSpawner"/>, the plan half is pure and the spawn half is server-only.
    /// </summary>
    public class GuardSpawner : MonoBehaviour
    {
        [Tooltip("Which enemy garrisons which zone. Takes precedence over the single guard prefab.")]
        [SerializeField] private EnemyRoster _roster;

        [Tooltip("Fallback guard prefab, used for any zone the roster does not cover. Without a " +
                 "roster and without this, the placements are data-only (planned, not spawned).")]
        [SerializeField] private GameObject _guardPrefab;

        [Tooltip("Scales every zone's guard density. 0 disables the garrison entirely.")]
        [Range(0f, 2f)]
        [SerializeField] private float _densityScale = 1f;

        [Tooltip("Scales every spawned guard's patrol and chase speed. Applied once at spawn, on top " +
                 "of the prefab's own tuning, so one number balances the whole roster (#117).")]
        [Range(0.25f, 2f)]
        [SerializeField] private float _speedScale = 0.8f;

        [Tooltip("Scales every spawned guard's damage per hit. Applied once at spawn, on top of the " +
                 "prefab's own tuning (#118).")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _damageScale = 0.65f;

        [Tooltip("Parent for spawned guards. Auto-created if left null.")]
        [SerializeField] private Transform _container;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<GuardPlacement> _lastPlan = new List<GuardPlacement>();

        /// <summary>The garrison plan behind the guards currently in the world.</summary>
        public IReadOnlyList<GuardPlacement> LastPlan => _lastPlan;

        /// <summary>Guards currently spawned.</summary>
        public IReadOnlyList<GameObject> Spawned => _spawned;

        public float DensityScale { get => _densityScale; set => _densityScale = value; }
        public float SpeedScale { get => _speedScale; set => _speedScale = value; }
        public float DamageScale { get => _damageScale; set => _damageScale = value; }
        public GameObject GuardPrefab { get => _guardPrefab; set => _guardPrefab = value; }
        public EnemyRoster Roster { get => _roster; set => _roster = value; }

        /// <summary>The Age the current garrison was drawn for.</summary>
        public HistoricalEra LastEra { get; private set; }

        /// <summary>
        /// Plans and spawns the garrison for a castle, drawing only enemies of
        /// <paramref name="era"/> where the roster has them. Returns the plan.
        /// <paramref name="safeModuleIndex"/> is the arrival room guards keep clear of; -1 keeps the
        /// old gatehouse ring.
        /// </summary>
        public IReadOnlyList<GuardPlacement> SpawnFor(ProceduralCastleData castle, int seed,
            HistoricalEra era, int safeModuleIndex = -1)
        {
            Clear();
            LastEra = era;

            List<GuardPlacement> plan = GuardPlacementPlanner.Plan(castle, seed, _densityScale, safeModuleIndex);
            _lastPlan.AddRange(plan);

            if (_roster == null && _guardPrefab == null)
                return _lastPlan;

            // A fourth independent stream, matching the planner's convention: picking *which* enemy
            // must not shift the castle, the loot, or where the garrison stands.
            var rng = new System.Random(unchecked(seed * 31 + 24593));

            _roster?.ResetWarnings();
            EnsureContainer();
            for (int i = 0; i < plan.Count; i++)
                Spawn(plan[i], era, rng);

            return _lastPlan;
        }

        /// <summary>The enemy prefab for a placement: the roster's pick, else the fallback prefab.</summary>
        private GameObject PrefabFor(GuardPlacement placement, HistoricalEra era, System.Random rng)
        {
            if (_roster != null)
            {
                GameObject fromRoster = _roster.PickForZone(placement.Zone, era, rng);
                if (fromRoster != null)
                    return fromRoster;
            }
            return _guardPrefab;
        }

        private void Spawn(GuardPlacement placement, HistoricalEra era, System.Random rng)
        {
            GameObject prefab = PrefabFor(placement, era, rng);
            if (prefab == null)
                return;

            // The enemy prefabs keep the axis correction on their mesh child, so their root rotation
            // is already identity — but compose rather than replace, so a prefab that corrects on the
            // root (as the castle and loot prefabs do) also stands up correctly here.
            GameObject go = Instantiate(prefab, placement.Position, prefab.transform.rotation,
                _container);
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
            guard.ScaleTuning(_speedScale, _damageScale);
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
