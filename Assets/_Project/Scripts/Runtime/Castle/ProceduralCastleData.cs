using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
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

        /// <summary>Curtain-wall cells whose room archway onto the strip is open: the ways in from
        /// the strip (#140). Set by <see cref="CastleEntrancePlanner"/> during generation.</summary>
        public List<UnityEngine.Vector2Int> EntranceCells = new List<UnityEngine.Vector2Int>();

        /// <summary>The walkable graph stitched from the placed modules' nav tiles (#221). Built by the
        /// generator on every peer from the seed; never serialized or sent. Empty when the layout was
        /// generated without a registry.</summary>
        [NonSerialized] public CastleNavGraph NavGraph;

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
            /// <summary>The storey the module stands on (<see cref="CastleLevels"/>); its lowest one if it spans more.</summary>
            public int Level;
            /// <summary>How many storeys the module spans: 2 for a stair, 0 or 1 for a room.</summary>
            public int Storeys;
            /// <summary>For a stair, the level where it opens one way only (the head of an up-stair, the foot of a down-stair).</summary>
            public int ExitLevel;
            /// <summary>For a stair, the grid direction it opens toward on <see cref="ExitLevel"/>.</summary>
            public Vector2Int ExitFacing;

            /// <summary>The highest storey the module occupies.</summary>
            public int TopLevel => Level + Mathf.Max(1, Storeys) - 1;

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
                Level = CastleLevels.Ground;
                Storeys = 0;
                ExitLevel = CastleLevels.Ground;
                ExitFacing = Vector2Int.zero;
            }
        }
    }
}
