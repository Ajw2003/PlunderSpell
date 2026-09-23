using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueAi.Castle
{
    /// <summary>
    /// Serializable description of a single room prefab available to the generator.
    /// The <see cref="Prefab"/> reference is filled in by designers; the generation logic works
    /// with null prefabs (placement is computed from metadata) so the pipeline is testable.
    /// </summary>
    [Serializable]
    public class CastleRoomModuleData
    {
        [Tooltip("Stable identifier, must match the CastleRoomModule.RoomId on the prefab.")]
        public string RoomId;

        [Tooltip("Which concentric zone this room belongs to.")]
        public CastleZone Zone;

        [Tooltip("Prefab to instantiate. May be null in data-only/test scenarios.")]
        public GameObject Prefab;

        [Tooltip("Relative spawn frequency within its zone's weighted pool (>=1).")]
        public int Weight = 1;

        [Tooltip("Where loot can sit in this room (table tops, chest lids, shelves, altars), as " +
                 "offsets from the module's position before its placement rotation. Written by " +
                 "Tools/Plunderspell/Import Castle Loot Anchors from the asset pipeline's " +
                 "CastleLootAnchors.json; do not edit by hand.")]
        public Vector3[] LootAnchors = new Vector3[0];
    }

    /// <summary>
    /// ScriptableObject catalogue of every room module, grouped conceptually by zone. Feeds the
    /// <see cref="ProceduralCastleGenerator"/> weighted selection.
    /// </summary>
    [CreateAssetMenu(fileName = "CastleRoomRegistry", menuName = "RogueAi/Castle/Room Registry")]
    public class CastleRoomRegistry : ScriptableObject
    {
        [Tooltip("All room modules known to the generator.")]
        public List<CastleRoomModuleData> Modules = new List<CastleRoomModuleData>();

        [Tooltip("One door-plug prefab per enclosed zone, used to seal an archway that faces an " +
                 "empty cell. Zone is the only field that matters; RoomId is for readability.")]
        public List<CastleRoomModuleData> DoorPlugs = new List<CastleRoomModuleData>();

        /// <summary>Returns all modules that belong to the requested zone.</summary>
        public List<CastleRoomModuleData> GetModulesForZone(CastleZone zone)
        {
            var result = new List<CastleRoomModuleData>();
            for (int i = 0; i < Modules.Count; i++)
            {
                if (Modules[i] != null && Modules[i].Zone == zone)
                    result.Add(Modules[i]);
            }
            return result;
        }

        /// <summary>
        /// The door-plug prefab that fills an archway in <paramref name="zone"/>, or null when
        /// none is registered. Plugs are per-zone because the archway is sized from the zone's
        /// wall height, so one plug cannot fill a Crypt door and a Keep door both.
        /// </summary>
        public GameObject GetDoorPlugForZone(CastleZone zone)
        {
            for (int i = 0; i < DoorPlugs.Count; i++)
            {
                if (DoorPlugs[i] != null && DoorPlugs[i].Zone == zone)
                    return DoorPlugs[i].Prefab;
            }
            return null;
        }

        /// <summary>Finds a module entry by its RoomId, or null if not present.</summary>
        public CastleRoomModuleData GetById(string roomId)
        {
            for (int i = 0; i < Modules.Count; i++)
            {
                if (Modules[i] != null && Modules[i].RoomId == roomId)
                    return Modules[i];
            }
            return null;
        }
    }
}
