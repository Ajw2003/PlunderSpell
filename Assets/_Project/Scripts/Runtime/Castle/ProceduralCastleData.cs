using System;
using System.Collections.Generic;
using UnityEngine;

namespace RogueAi.Castle
{
    /// <summary>
    /// Fully serializable result of a generation pass. Because generation is deterministic from
    /// <see cref="Seed"/>, only the seed needs to travel across the network — this structure is a
    /// local, inspectable snapshot of what that seed produced.
    /// </summary>
    [Serializable]
    public class ProceduralCastleData
    {
        [Tooltip("The seed that produced this layout.")]
        public int Seed;

        [Tooltip("Every module placed, in placement order (crypt center first).")]
        public List<PlacedModule> PlacedModules = new List<PlacedModule>();

        /// <summary>Index of the placed module flagged as the extraction exit, or -1.</summary>
        public int ExtractionExitIndex = -1;

        /// <summary>Index of the crypt final chamber (path start), or -1.</summary>
        public int CryptStartIndex = -1;

        /// <summary>The bailey's furniture and the courtyards' yards, from its own seed stream.</summary>
        public List<PlacedDressing> Dressings = new List<PlacedDressing>();

        public ProceduralCastleData() { }

        public ProceduralCastleData(int seed)
        {
            Seed = seed;
        }

        /// <summary>A single placed room: which prefab, where, facing, and its zone.</summary>
        [Serializable]
        public struct PlacedModule
        {
            public string RoomId;
            public Vector3 Position;
            public Quaternion Rotation;
            public CastleZone Zone;
            public Vector2Int GridPosition;
            public bool IsExtractionExit;
            public bool IsCryptEntry;

            public PlacedModule(string roomId, Vector3 position, Quaternion rotation,
                CastleZone zone, Vector2Int gridPosition)
            {
                RoomId = roomId;
                Position = position;
                Rotation = rotation;
                Zone = zone;
                GridPosition = gridPosition;
                IsExtractionExit = false;
                IsCryptEntry = false;
            }
        }
    }
}
