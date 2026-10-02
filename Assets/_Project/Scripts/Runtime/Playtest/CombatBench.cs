using System.Collections.Generic;
using Plunderspell.Alarm;
using Plunderspell.Core;
using Plunderspell.Guards;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Playtest
{
    /// <summary>
    /// Spawns enemies to fight on demand, so a weapon or a spell can be tried in about two seconds
    /// instead of by starting a raid and walking a castle until something turns up.
    ///
    /// See docs/4-systems/combat-bench.md.
    /// </summary>
    public class CombatBench : MonoBehaviour
    {
        [Tooltip("Every spawnable enemy. The same roster the raid garrisons its castle from.")]
        [SerializeField] private EnemyRoster m_roster;

        [Tooltip("Enemies spawn in a ring around this. Falls back to this object's own transform.")]
        [SerializeField] private Transform m_spawnOrigin;

        [Tooltip("Radius of the spawn ring, in metres.")]
        [Range(2f, 30f)]
        [SerializeField] private float m_spawnRadius = 7f;

        private readonly List<GameObject> m_spawned = new List<GameObject>();

        /// <summary>Everything this bench has spawned and not yet cleared.</summary>
        public IReadOnlyList<GameObject> Spawned => m_spawned;

        /// <summary>The roster the HUD lists its enemy types from.</summary>
        public EnemyRoster Roster => m_roster;

        /// <summary>
        /// Load-bearing, not convenience. See docs/4-systems/combat-bench.md, "The bench puts itself
        /// into play".
        /// </summary>
        private void Start()
        {
            GameServices.Initialize();
            GameServices.GameState.ChangeState(GameState.Playing);
        }

        /// <summary>
        /// Spawns <paramref name="count"/> of <paramref name="prefab"/> evenly around the ring, each
        /// facing the middle so they are looking at whoever is standing there.
        /// </summary>
        public void Spawn(GameObject prefab, int count, GuardAlertState alertState)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            Transform origin = m_spawnOrigin != null ? m_spawnOrigin : transform;

            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : i / (float)count * Mathf.PI * 2f;
                var offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * m_spawnRadius;
                Vector3 position = origin.position + offset;

                GameObject enemy = Instantiate(prefab, position,
                    Quaternion.LookRotation(origin.position - position, Vector3.up));
                enemy.name = $"{prefab.name} ({i + 1})";

                ApplyAlertState(enemy, alertState);
                m_spawned.Add(enemy);
            }
        }

        /// <summary>Removes everything this bench spawned, leaving the arena itself alone.</summary>
        public void ClearSpawned()
        {
            for (int i = 0; i < m_spawned.Count; i++)
            {
                if (m_spawned[i] != null)
                {
                    Destroy(m_spawned[i]);
                }
            }

            m_spawned.Clear();
        }

        /// <summary>Starts the guard in the asked-for state. See docs/4-systems/combat-bench.md,
        /// "Traps", on why forcing a state is a dev seam only.</summary>
        private static void ApplyAlertState(GameObject enemy, GuardAlertState alertState)
        {
            if (!enemy.TryGetComponent(out Guard guard))
            {
                return;
            }

            if (alertState == GuardAlertState.Chasing)
            {
                StartChasing(guard);
            }
            else if (alertState != GuardAlertState.Patrolling)
            {
                // Only a chase can be forced: the other states need a target or a lead the bench does not have.
                Debug.LogWarning($"CombatBench: a guard cannot start in {alertState}; it starts on patrol.");
            }
        }

        // The chase state needs a named player, so the bench gives it the first intruder the director knows.
        private static void StartChasing(Guard guard)
        {
            IReadOnlyList<Transform> intruders = EnemyDirector.IntrudersOf(guard.Link.Director);
            if (intruders.Count == 0)
            {
                Debug.LogWarning("CombatBench: no player is registered to chase, so the guard starts on patrol.");
                return;
            }

            guard.States.Chase.Follow(intruders[0]);
            guard.ChangeState(guard.States.Chase);
        }
    }
}
