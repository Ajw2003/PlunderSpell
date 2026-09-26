using System.Collections.Generic;
using UnityEngine;

namespace RogueAi.Castle
{
    /// <summary>
    /// Where the team steps out of the portal: a room chosen from the layout and the raid seed, so
    /// every peer derives the same spot without it crossing the network.
    ///
    /// Only the outer rings qualify. Starting in the keep or the crypt would skip the whole climb
    /// toward the best loot, and the gatehouse is sealed. See docs/plans/night-atmosphere.md,
    /// section 6.
    /// </summary>
    public static class CastleArrivalPlanner
    {
        /// <summary>Returned when a layout has no room the team may arrive in.</summary>
        public const int NoArrival = -1;

        /// <summary>
        /// How far in from a curtain-wall cell's centre to stand. That cell's wall runs along its
        /// outer half; the open strip of bailey is on the inner half.
        /// </summary>
        private const float k_CurtainInwardOffset = 3.5f;

        /// <summary>True for the zones a raid may start in.</summary>
        public static bool IsArrivalZone(CastleZone zone) =>
            zone == CastleZone.CurtainWall || zone == CastleZone.OuterBailey || zone == CastleZone.InnerWard;

        /// <summary>
        /// The index into <see cref="ProceduralCastleData.PlacedModules"/> the team arrives in, or
        /// <see cref="NoArrival"/>. Uses its own random stream, so it cannot shift the castle, the
        /// loot or the garrison.
        /// </summary>
        public static int ChooseModule(ProceduralCastleData layout, int seed)
        {
            if (layout?.PlacedModules == null)
                return NoArrival;

            var candidates = new List<int>();
            for (int i = 0; i < layout.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = layout.PlacedModules[i];
                if (module.IsExtractionExit || i == layout.ExtractionExitIndex)
                    continue;
                if (!IsArrivalZone(module.Zone))
                    continue;
                candidates.Add(i);
            }

            if (candidates.Count == 0)
                return NoArrival;

            var rng = new System.Random(unchecked(seed * 37 + 7919));
            return candidates[rng.Next(candidates.Count)];
        }

        /// <summary>The point in that module to stand the portal on, before any overlap probing.</summary>
        public static Vector3 AnchorFor(ProceduralCastleData layout, int moduleIndex)
        {
            ProceduralCastleData.PlacedModule module = layout.PlacedModules[moduleIndex];
            if (module.Zone != CastleZone.CurtainWall)
                return module.Position;

            return module.Position
                   + CastleSpawnResolver.InwardDirection(module.GridPosition) * k_CurtainInwardOffset;
        }
    }
}
