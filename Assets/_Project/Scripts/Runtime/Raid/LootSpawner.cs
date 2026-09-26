using System.Collections.Generic;
using RogueAi.Castle;
using RogueAi.Loot;
using UnityEngine;

namespace RogueAi.Raid
{
    /// <summary>
    /// Turns a <see cref="LootPlacementPlanner"/> plan into actual objects in the scene.
    ///
    /// The split matters: the plan is deterministic and network-free, so every peer computes the
    /// identical haul from the replicated seed. Only the spawning half touches the scene, and on a
    /// networked raid only the server runs it.
    /// </summary>
    public class LootSpawner : MonoBehaviour
    {
        [Tooltip("What can be found where.")]
        [SerializeField] private RaidLootTable _table;

        [Tooltip("Parent for spawned loot. Auto-created if left null.")]
        [SerializeField] private Transform _container;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<LootPlacement> _lastPlan = new List<LootPlacement>();

        public RaidLootTable Table { get => _table; set => _table = value; }

        /// <summary>The plan behind the loot currently in the world.</summary>
        public IReadOnlyList<LootPlacement> LastPlan => _lastPlan;

        /// <summary>Loot objects currently spawned.</summary>
        public IReadOnlyList<GameObject> Spawned => _spawned;

        /// <summary>
        /// Plans and spawns the haul for a castle. Returns the plan, which is complete even when no
        /// prefabs are assigned — a table without prefabs yields a data-only plan, exactly as the
        /// castle generator yields a data-only layout.
        /// </summary>
        public IReadOnlyList<LootPlacement> SpawnFor(ProceduralCastleData castle, int seed)
        {
            Clear();

            List<LootPlacement> plan = LootPlacementPlanner.Plan(castle, _table, seed);
            _lastPlan.AddRange(plan);

            for (int i = 0; i < plan.Count; i++)
                Spawn(plan[i]);

            return _lastPlan;
        }

        /// <summary>Spawns a single loose piece of loot — what Aurum Voco conjures.</summary>
        public GameObject SpawnLoose(LootItem item, GameObject prefab, Vector3 position)
        {
            if (item == null)
                return null;

            EnsureContainer();
            GameObject go = prefab != null
                ? Instantiate(prefab, position, Quaternion.identity, _container)
                : new GameObject($"Loot_{item.DisplayName}");

            if (prefab == null)
            {
                go.transform.SetParent(_container, false);
                go.transform.position = position;
            }

            var pickup = go.GetComponent<LootPickup>();
            if (pickup == null)
                pickup = go.AddComponent<LootPickup>();
            pickup.SetData(item);

            _spawned.Add(go);
            return go;
        }

        private void Spawn(LootPlacement placement)
        {
            RaidLootTable.Entry entry = placement.Entry;
            if (entry?.Item == null)
                return;
            SpawnLoose(entry.Item, entry.Prefab, placement.Position);
        }

        /// <summary>Destroys everything spawned for the last raid. Called when a raid ends.</summary>
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
            var go = new GameObject("SpawnedLoot");
            go.transform.SetParent(transform, false);
            _container = go.transform;
        }
    }
}
