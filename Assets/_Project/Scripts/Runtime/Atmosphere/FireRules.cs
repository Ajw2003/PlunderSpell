using System.Collections.Generic;
using RogueAi.Alarm;
using RogueAi.Castle;
using UnityEngine;

namespace RogueAi.Atmosphere
{
    /// <summary>
    /// How the castle's fires answer the alarm (docs/plans/night-atmosphere.md, section 2), and
    /// which of them the machine can afford to light. Pure, so both are tested without a scene.
    /// </summary>
    public static class FireRules
    {
        /// <summary>How much bigger and hotter a fire burns at full alert.</summary>
        public const float FullAlertFlare = 1.5f;

        /// <summary>Whether a fire first lit at <paramref name="litFrom"/> burns in <paramref name="state"/>.</summary>
        public static bool IsLit(FireKind kind, int litFrom, AlarmState state)
        {
            if (kind == FireKind.Hearth)
                return true;
            return (int)state >= litFrom;
        }

        /// <summary>
        /// How much a lit fire is flared, 1 being its calm size. Braziers flare when the castle is
        /// roused; at the hue and cry everything burns at its maximum.
        /// </summary>
        public static float Flare(FireKind kind, AlarmState state)
        {
            if (state == AlarmState.HueAndCry)
                return FullAlertFlare;
            if (state == AlarmState.Roused && (kind == FireKind.Brazier || kind == FireKind.Beacon))
                return FullAlertFlare;
            return 1f;
        }

        /// <summary>The light a fire gets from the budget.</summary>
        public enum LightGrant
        {
            /// <summary>Flame and fog glow only.</summary>
            None = 0,

            /// <summary>Lights the world, no shadows.</summary>
            Unshadowed = 1,

            /// <summary>Lights the world and casts shadows.</summary>
            Shadowed = 2,
        }

        /// <summary>
        /// Shares out lights by distance: the nearest <paramref name="shadowed"/> lit fires cast
        /// shadows, the next <paramref name="unshadowed"/> light without them, and the rest are flame
        /// and glow only. Unlit fires get nothing. Ties go to the lower index, so the grant is stable.
        /// </summary>
        /// <param name="sqrDistances">Squared distance of each fire from the camera.</param>
        /// <param name="isLit">Whether each fire is burning.</param>
        /// <param name="grants">Filled with one grant per fire.</param>
        /// <param name="order">Scratch list, reused to avoid allocating each frame.</param>
        public static void ShareLights(IReadOnlyList<float> sqrDistances, IReadOnlyList<bool> isLit,
            int shadowed, int unshadowed, List<LightGrant> grants, List<int> order)
        {
            grants.Clear();
            order.Clear();
            for (int i = 0; i < sqrDistances.Count; i++)
            {
                grants.Add(LightGrant.None);
                if (isLit[i])
                    order.Add(i);
            }

            order.Sort((a, b) =>
            {
                int byDistance = sqrDistances[a].CompareTo(sqrDistances[b]);
                return byDistance != 0 ? byDistance : a.CompareTo(b);
            });

            long unshadowedEnd = (long)shadowed + unshadowed;
            for (int rank = 0; rank < order.Count; rank++)
            {
                if (rank < shadowed)
                    grants[order[rank]] = LightGrant.Shadowed;
                else if (rank < unshadowedEnd)
                    grants[order[rank]] = LightGrant.Unshadowed;
                else
                    break;
            }
        }

        /// <summary>Squared distance helper kept here so callers and tests measure the same way.</summary>
        public static float SqrDistance(Vector3 a, Vector3 b) => (a - b).sqrMagnitude;
    }
}
