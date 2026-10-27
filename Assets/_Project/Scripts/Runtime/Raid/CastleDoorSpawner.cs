using System.Collections.Generic;
using Plunderspell.Castle;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Puts one door per <see cref="CastleDoorPlanner"/> entry into the world (#248). Server-only, like
    /// <see cref="GuardSpawner"/>: the rooms are built locally on every peer, so the doors are the network objects
    /// that clients receive. Doors start closed and unlocked.
    /// </summary>
    public class CastleDoorSpawner : MonoBehaviour
    {
        [Tooltip("Door sized to the inner ward's archway (2.88 m).")]
        [SerializeField] private GameObject _innerWardDoor;

        [Tooltip("Door sized to the keep's archway (3.31 m).")]
        [SerializeField] private GameObject _keepDoor;

        [Tooltip("Door sized to the crypt's archway (2.16 m).")]
        [SerializeField] private GameObject _cryptDoor;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>Doors currently in the world.</summary>
        public IReadOnlyList<GameObject> Spawned => _spawned;

        public void SpawnFor(ProceduralCastleData castle)
        {
            Clear();
            foreach (PlannedDoor door in CastleDoorPlanner.Plan(castle))
            {
                GameObject prefab = PrefabFor(door.Zone);
                if (prefab == null)
                    continue;
                _spawned.Add(Instantiate(prefab, door.Position, door.Rotation, transform));
            }
        }

        /// <summary>Removes the doors. Called when a raid ends.</summary>
        public void Clear()
        {
            foreach (GameObject go in _spawned)
            {
                if (go == null)
                    continue;
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
            }
            _spawned.Clear();
        }

        private GameObject PrefabFor(CastleZone zone)
        {
            switch (zone)
            {
                case CastleZone.Keep: return _keepDoor;
                case CastleZone.Crypt: return _cryptDoor;
                default: return _innerWardDoor;
            }
        }
    }
}
