using System.Collections;
using System.Collections.Generic;
using Plunderspell.Castle;
using Plunderspell.Loot;
using UnityEngine;

namespace Plunderspell.Raid
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

        [Tooltip("Seconds to hold spawned loot frozen while the rest of the castle finishes arriving.")]
        [SerializeField] private float m_settleDelay = 0.5f;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<LootPlacement> _lastPlan = new List<LootPlacement>();

        private Coroutine m_settleRoutine;

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
        public IReadOnlyList<LootPlacement> SpawnFor(ProceduralCastleData castle, int seed,
            CastleRoomRegistry registry = null)
        {
            Clear();

            List<LootPlacement> plan = LootPlacementPlanner.Plan(castle, _table, seed, registry);
            _lastPlan.AddRange(plan);

            // The castle rooms were instantiated earlier in this same frame, and PhysX does not see a
            // new collider's transform until it is synced. Spawning loot rigidbodies against an
            // unsynced physics scene is what makes them resolve against stale room positions.
            Physics.SyncTransforms();

            for (int i = 0; i < plan.Count; i++)
                Spawn(plan[i]);

            BeginSettling();
            return _lastPlan;
        }

        /// <summary>
        /// Holds every spawned piece frozen for <see cref="m_settleDelay"/>, then hands it back to
        /// physics. See docs/4-systems/raid.md, "How loot settles".
        /// </summary>
        private void BeginSettling()
        {
            FreezeSpawned();

            // Outside play mode there are no coroutines and no physics step, so the freeze is the
            // whole story: nothing will move the loot until the scene is played.
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;

            if (m_settleRoutine != null)
                StopCoroutine(m_settleRoutine);
            m_settleRoutine = StartCoroutine(ReleaseAfterSettleDelay());
        }

        private void FreezeSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                Rigidbody body = RigidbodyOf(_spawned[i]);
                if (body == null)
                    continue;

                body.isKinematic = true;
            }
        }

        private IEnumerator ReleaseAfterSettleDelay()
        {
            yield return new WaitForSeconds(m_settleDelay);
            ReleaseSpawned();
            m_settleRoutine = null;
        }

        /// <summary>
        /// Unfreezes everything that is not already in someone's hands. Public so a test can settle
        /// the haul without waiting out the delay in real time.
        /// </summary>
        public void ReleaseSpawned()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                GameObject go = _spawned[i];
                if (go == null)
                    continue;

                // A carried item is kinematic on purpose — releasing it would drop it out of the
                // carrier's hand socket.
                var pickup = go.GetComponent<LootPickup>();
                if (pickup != null && (pickup.IsBeingCarried || pickup.IsBroken))
                    continue;

                Rigidbody body = RigidbodyOf(go);
                if (body != null)
                    body.isKinematic = false;
            }
        }

        private static Rigidbody RigidbodyOf(GameObject go) =>
            go != null ? go.GetComponent<Rigidbody>() : null;

        /// <summary>Spawns a single loose piece of loot — what Aurum Voco conjures.</summary>
        public GameObject SpawnLoose(LootItem item, GameObject prefab, Vector3 position)
        {
            if (item == null)
                return null;

            EnsureContainer();
            GameObject go = prefab != null
                // Keep the prefab's own rotation: it carries the Blender axis correction, and
                // passing identity here lays the loot on its side.
                ? Instantiate(prefab, position, prefab.transform.rotation, _container)
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

            // Extraction tallies LootValue, so every spawned piece gets one or the haul is worth
            // nothing however carefully it was carried out.
            if (!go.TryGetComponent(out LootValue value))
                value = go.AddComponent<LootValue>();
            value.SetItem(item);

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
