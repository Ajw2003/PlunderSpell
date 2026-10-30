using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Where the hidden escape portal opens: a room chosen from the layout and the raid seed, in the
    /// farthest third of the castle (by walking distance from the arrival point), so every peer
    /// derives the same room without it crossing the network. See docs/4-systems/raid.md, section
    /// "The arrival portal closes; a hidden escape portal opens".
    /// </summary>
    public static class CastleEscapePlanner
    {
        /// <summary>Returned when no room qualifies.</summary>
        public const int NoEscape = -1;

        /// <summary>
        /// A module's Position is at floor level; the nav grid's cells stand a little above it
        /// (the nav tests lift points the same way).
        /// </summary>
        private static readonly Vector3 s_standingLift = Vector3.up * 0.4f;

        /// <summary>
        /// Picks from the farthest third of the candidates. A NaN, infinite or negative distance means
        /// that module is not a candidate. Uses its own random stream, so it cannot shift the castle,
        /// the loot or the garrison. Returns the index into <paramref name="distances"/>, or <see cref="NoEscape"/>.
        /// </summary>
        public static int ChooseFromDistances(IReadOnlyList<float> distances, int seed)
        {
            if (distances == null)
                return NoEscape;

            var ranked = new List<int>();
            for (int i = 0; i < distances.Count; i++)
            {
                float distance = distances[i];
                if (!float.IsNaN(distance) && !float.IsInfinity(distance) && distance >= 0f)
                    ranked.Add(i);
            }

            if (ranked.Count == 0)
                return NoEscape;

            // Farthest first; equal distances keep the lower index first so the order is stable.
            ranked.Sort((a, b) =>
            {
                int byDistance = distances[b].CompareTo(distances[a]);
                return byDistance != 0 ? byDistance : a.CompareTo(b);
            });

            int farthestThird = Mathf.Max(1, Mathf.CeilToInt(ranked.Count / 3f));
            var rng = new System.Random(unchecked(seed * 53 + 104729));
            return ranked[rng.Next(farthestThird)];
        }

        /// <summary>
        /// The index into <see cref="ProceduralCastleData.PlacedModules"/> the escape portal opens in,
        /// or <see cref="NoEscape"/>. The arrival room, the extraction exit and rooms the guards
        /// cannot walk to are not candidates.
        /// </summary>
        public static int ChooseModule(ProceduralCastleData layout, CastleNavGraph nav, int arrivalModule,
            Vector3 arrivalPoint, int seed)
        {
            if (layout?.PlacedModules == null || nav == null)
                return NoEscape;

            var distances = new List<float>(layout.PlacedModules.Count);
            var path = new List<Vector3>();
            for (int i = 0; i < layout.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = layout.PlacedModules[i];
                bool excluded = i == arrivalModule || i == layout.ExtractionExitIndex || module.IsExtractionExit;
                distances.Add(excluded ? float.NaN : WalkingDistance(nav, arrivalPoint, module.Position + s_standingLift, path));
            }

            return ChooseFromDistances(distances, seed);
        }

        /// <summary>Length of the nav path between two points, or NaN when there is none.</summary>
        private static float WalkingDistance(CastleNavGraph nav, Vector3 from, Vector3 to, List<Vector3> path)
        {
            if (!nav.FindPath(from, to, path) || path.Count == 0)
                return float.NaN;

            float length = 0f;
            for (int i = 1; i < path.Count; i++)
                length += Vector3.Distance(path[i - 1], path[i]);
            return length;
        }
    }
}
