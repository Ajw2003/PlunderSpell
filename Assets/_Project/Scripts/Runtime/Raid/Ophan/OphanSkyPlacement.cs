using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Where the Ophan hangs: far off in the sky beyond the castle, on the side away from where the players arrived, so
    /// they see it across the castle; its centre <see cref="OphanTuning.Distance"/> out and
    /// <see cref="OphanTuning.Elevation"/> degrees up, facing the castle's centre so its eye is on the players.
    /// </summary>
    public static class OphanSkyPlacement
    {
        public static Vector3 Centre(Vector3 castleCentre, Vector3 arrivalPoint, OphanTuning tuning)
        {
            Vector3 away = castleCentre - arrivalPoint;
            away.y = 0f;
            if (away.sqrMagnitude < 1f)
                away = Vector3.forward;
            away.Normalize();
            float height = tuning.Distance * Mathf.Tan(tuning.Elevation * Mathf.Deg2Rad);
            return new Vector3(castleCentre.x, 0f, castleCentre.z) + away * tuning.Distance + Vector3.up * height;
        }

        /// <summary>Faces the castle's centre at a standing player's eye height (1.65 m).</summary>
        public static Quaternion Facing(Vector3 centre, Vector3 castleCentre) =>
            Quaternion.LookRotation(new Vector3(castleCentre.x, 1.65f, castleCentre.z) - centre, Vector3.up);
    }
}
