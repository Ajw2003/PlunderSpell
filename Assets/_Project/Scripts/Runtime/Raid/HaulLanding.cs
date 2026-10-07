using Code.Scripts.EventSystems;
using Plunderspell.Extraction;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Lands the last raid's haul on the Lair floor at the child "HaulLanding": on the server, the
    /// pieces carried through the portal are spawned again there, replacing the previous pile. The pile
    /// is cleared when the next raid starts. Its pieces are its own spawner's, so the raid's loot
    /// clear never touches them; they are not saved. See docs/4-systems/raid.md, "How the haul comes home".
    /// </summary>
    public class HaulLanding : MonoBehaviour
    {
        [Tooltip("Spawns and owns the pile. Separate from the raid's spawner so the raid's clear leaves it alone.")]
        [SerializeField] private LootSpawner _pile;

        private void OnEnable()
        {
            EventManager.Instance?.Subscribe(this, (HaulExtracted e) => Land(e));
            EventManager.Instance?.Subscribe(this, (RaidPhaseChanged e) =>
            {
                if (e.Phase == RaidPhase.Generating && IsServer())
                    _pile.Clear();
            });
        }

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private static bool IsServer() => NetworkManager.main == null || !NetworkManager.main.isClientOnly;

        private void Land(HaulExtracted haul)
        {
            if (!IsServer() || _pile == null)
                return;

            RaidLootTable table = RaidTable();
            var points = HaulLayout.Offsets(haul.Pieces.Count);
            for (int i = 0; i < points.Count; i++)
                points[i] = transform.TransformPoint(points[i]) + Vector3.up * LootPlacementPlanner.AnchorLift;
            _pile.SpawnPile(haul.Pieces, points, table);
        }

        /// <summary>The raid's current loot table, which maps each piece back to its prefab.</summary>
        private RaidLootTable RaidTable()
        {
            foreach (LootSpawner spawner in FindObjectsByType<LootSpawner>(FindObjectsSortMode.None))
            {
                if (spawner != _pile && spawner.Table != null)
                    return spawner.Table;
            }
            return null;
        }
    }
}
