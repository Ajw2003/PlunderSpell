using System.Collections.Generic;
using UnityEngine;

namespace RogueAi.Castle
{
    /// <summary>
    /// Works out where a raid starts from a generated layout: standing on the floor just inside the
    /// gatehouse, which is the castle's one gate and its extraction exit both.
    ///
    /// Shared by the runtime director (which builds from whatever seed the raid rolled) and by the
    /// editor scene builder (which bakes a spawn for the authored scene), so there is one probe and
    /// one set of player dimensions rather than a copy per caller.
    ///
    /// See docs/systems/scale.md ("Spawning").
    /// </summary>
    public static class CastleSpawnResolver
    {
        /// <summary>Height of the player capsule the spawn has to fit.</summary>
        public const float PlayerHeight = 1.8f;

        /// <summary>Radius of the player capsule the spawn has to fit.</summary>
        public const float PlayerRadius = 0.4f;

        /// <summary>Thickness of a room module's floor slab, i.e. the Y a room is walked on.</summary>
        public const float FloorHeight = 0.3f;

        /// <summary>
        /// Clearance above the floor for the spawn probe. The slab's top face is a couple of
        /// millimetres proud of <see cref="FloorHeight"/> (room_kit grows every stacked box slightly
        /// so flush joints do not z-fight), so a capsule resting at exactly FloorHeight intersects
        /// it and every candidate spawn reads as blocked.
        /// </summary>
        public const float FloorClearance = 0.05f;

        /// <summary>
        /// How far inside the gate cell to start looking. The gatehouse's own wall, portcullis bars
        /// and flanking drum towers all crowd the outward half of that cell.
        /// </summary>
        private const float k_InwardBias = 3.5f;

        /// <summary>
        /// The world position to place the player at for <paramref name="layout"/>. Falls back to
        /// the crypt, then to the origin, so a layout without a gate still spawns somebody.
        ///
        /// Callers that have just instantiated the castle must have run
        /// <see cref="Physics.SyncTransforms"/> first, or every overlap probe reads clear.
        /// </summary>
        public static Vector3 ResolveSpawn(ProceduralCastleData layout)
        {
            if (layout == null || layout.PlacedModules.Count == 0)
            {
                Debug.LogWarning("[CastleSpawn] Empty castle layout; spawning at the origin.");
                return Vector3.up * (FloorHeight + FloorClearance + PlayerHeight * 0.5f);
            }

            int index = layout.ExtractionExitIndex >= 0
                ? layout.ExtractionExitIndex
                : layout.CryptStartIndex;
            if (index < 0 || index >= layout.PlacedModules.Count)
                index = 0;

            ProceduralCastleData.PlacedModule gate = layout.PlacedModules[index];
            Vector3 inward = InwardDirection(gate.GridPosition);

            return FirstClearStandingPoint(gate.Position + inward * k_InwardBias);
        }

        /// <summary>
        /// Where the portal opens and the team stands: a clear point in the room
        /// <see cref="CastleArrivalPlanner"/> picks for this seed. Falls back to
        /// <see cref="ResolveSpawn"/>, with a warning, when no room qualifies, so a raid always
        /// starts somewhere.
        ///
        /// Callers that have just instantiated the castle must have run
        /// <see cref="Physics.SyncTransforms"/> first, or every overlap probe reads clear.
        /// </summary>
        public static Vector3 ResolveArrival(ProceduralCastleData layout, int seed, out int moduleIndex)
        {
            moduleIndex = CastleArrivalPlanner.ChooseModule(layout, seed);
            if (moduleIndex == CastleArrivalPlanner.NoArrival)
            {
                Debug.LogWarning("[CastleSpawn] No arrival room in this layout; arriving at the gate instead.");
                return ResolveSpawn(layout);
            }

            return FirstClearStandingPoint(CastleArrivalPlanner.AnchorFor(layout, moduleIndex));
        }

        /// <summary>
        /// The first offset from <paramref name="anchor"/> where a player-sized capsule standing on
        /// the floor touches nothing. Offsets fan outward from the anchor, because a cell's
        /// set-piece (a well, a stair core, a portcullis) is usually exactly there.
        /// </summary>
        public static Vector3 FirstClearStandingPoint(Vector3 anchor)
        {
            foreach (Vector2 offset in StandingOffsets())
            {
                Vector3 feet = anchor + new Vector3(offset.x, FloorHeight + FloorClearance, offset.y);
                Vector3 bottom = feet + Vector3.up * PlayerRadius;
                Vector3 top = feet + Vector3.up * (PlayerHeight - PlayerRadius);
                // Triggers are ignored deliberately: the alarm's listening volume spans the whole
                // castle, so counting it as an obstruction would reject every candidate.
                if (!Physics.CheckCapsule(bottom, top, PlayerRadius, ~0, QueryTriggerInteraction.Ignore))
                    return feet + Vector3.up * (PlayerHeight * 0.5f);
            }

            Debug.LogWarning("[CastleSpawn] No clear standing point near the gate; using its centre.");
            return anchor + Vector3.up * (FloorHeight + FloorClearance + PlayerHeight * 0.5f);
        }

        /// <summary>Anchor, then two rings of eight, out to just inside the cell walls.</summary>
        private static IEnumerable<Vector2> StandingOffsets()
        {
            yield return Vector2.zero;
            foreach (float radius in new[] { 2.5f, 4.25f })
            {
                for (int step = 0; step < 8; step++)
                {
                    float angle = step * Mathf.PI * 0.25f;
                    yield return new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                }
            }
        }

        /// <summary>The world direction from a perimeter cell back toward the castle's centre.</summary>
        internal static Vector3 InwardDirection(Vector2Int cell)
        {
            if (cell == Vector2Int.zero)
                return Vector3.zero;

            int ring = Mathf.Max(Mathf.Abs(cell.x), Mathf.Abs(cell.y));
            if (Mathf.Abs(cell.x) == ring)
                return new Vector3(cell.x > 0 ? -1f : 1f, 0f, 0f);
            return new Vector3(0f, 0f, cell.y > 0 ? -1f : 1f);
        }
    }
}
