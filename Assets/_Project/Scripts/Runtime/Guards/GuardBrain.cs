using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Everything a guard decides, as pure functions. No scene, no components, no time — just the
    /// inputs a guard has and the conclusion it reaches.
    ///
    /// Keeping the decisions here rather than inside the MonoBehaviour is what makes guard behaviour
    /// testable. The legacy guard's state rules (NextState, the stuck watchdog, the search sweep, the
    /// hunt re-send) went with it (#214); <see cref="MoveSpeed"/> waits on #234.
    /// </summary>
    public static class GuardBrain
    {
        /// <summary>Noise quieter than this is background; a guard never reacts to it.</summary>
        public const float NoiseNoticeThreshold = 0.12f;

        /// <summary>
        /// How far a guard can see, in metres, given the castle-wide alert level. An alerted castle
        /// is a castle with torches lit and eyes open.
        /// </summary>
        public static float SightRange(float baseRange, AlarmState alarm)
        {
            switch (alarm)
            {
                case AlarmState.Stirred: return baseRange * 1.15f;
                case AlarmState.Roused: return baseRange * 1.4f;
                case AlarmState.HueAndCry: return baseRange * 1.75f;
                default: return baseRange;
            }
        }

        /// <summary>Movement speed for the alert level and what the guard is doing.</summary>
        public static float MoveSpeed(float patrolSpeed, float chaseSpeed, GuardAlertState state,
            AlarmState alarm)
        {
            float alarmBonus;
            switch (alarm)
            {
                case AlarmState.Stirred: alarmBonus = 1.1f; break;
                case AlarmState.Roused: alarmBonus = 1.25f; break;
                case AlarmState.HueAndCry: alarmBonus = 1.4f; break;
                default: alarmBonus = 1f; break;
            }

            switch (state)
            {
                case GuardAlertState.Chasing:
                case GuardAlertState.Searching:
                    return chaseSpeed * alarmBonus;
                case GuardAlertState.Investigating:
                    return Mathf.Lerp(patrolSpeed, chaseSpeed, 0.5f) * alarmBonus;
                case GuardAlertState.Incapacitated:
                    return 0f;
                default:
                    return patrolSpeed * alarmBonus;
            }
        }

        /// <summary>
        /// Whether a noise is worth walking over to. A louder castle-wide alert makes guards jumpier,
        /// so the same footstep that was ignored while Calm pulls a guard off patrol once Roused.
        /// </summary>
        public static bool ShouldInvestigate(float noiseStrength, AlarmState alarm)
        {
            float threshold;
            switch (alarm)
            {
                case AlarmState.Stirred: threshold = NoiseNoticeThreshold * 0.75f; break;
                case AlarmState.Roused: threshold = NoiseNoticeThreshold * 0.5f; break;
                case AlarmState.HueAndCry: threshold = NoiseNoticeThreshold * 0.4f; break;
                default: threshold = NoiseNoticeThreshold; break;
            }
            return noiseStrength >= threshold;
        }

        /// <summary>Metres from a player the hue and cry sends a guard (<c>GuardLeads</c>): they know roughly
        /// where, so players who break away and hide can still slip the hunt.</summary>
        public const float HuntOffsetMin = 3f;
        public const float HuntOffsetMax = 5f;

        /// <summary>
        /// Whether a guard can see a point: inside the cone, inside range, and not through a wall.
        /// Occlusion is left to the caller (<paramref name="lineOfSight"/>) so this stays pure.
        /// </summary>
        public static bool CanSee(Vector3 eye, Vector3 forward, Vector3 target, float range,
            float fovDegrees, bool lineOfSight)
        {
            if (!lineOfSight)
                return false;

            Vector3 toTarget = target - eye;
            if (toTarget.sqrMagnitude > range * range)
                return false;

            return IsWithinYaw(forward, toTarget, fovDegrees) && Mathf.Abs(PitchOf(toTarget)) <= MaxLookPitchDegrees;
        }

        /// <summary>How far up or down a guard can look, in degrees, whatever way its head is turned (#238).
        /// 80 lets a guard at the foot of a stair railing or a wall walk keep a player on it in view (measured
        /// 73 degrees to the head at 1.5 m under a 4.5 m wall walk); one nearly overhead is still not seen.</summary>
        public const float MaxLookPitchDegrees = 80f;

        /// <summary>
        /// Multiplies every guard's serialized sight range (14 m on the prefabs), so guards see farther than they
        /// did (owner, 2026-10-02) without editing each prefab and keeping the differences between guard kinds.
        /// </summary>
        public const float BaseSightScale = 1.5f;

        // The field of view is the side-to-side angle: height alone never takes a player out of the cone.
        // Straight above or below the guard there is no side-to-side angle, so that counts as in front.
        private static bool IsWithinYaw(Vector3 forward, Vector3 toTarget, float fovDegrees)
        {
            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
            Vector3 flatTarget = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flatForward.sqrMagnitude < 0.0001f || flatTarget.sqrMagnitude < 0.0001f)
                return true;

            return Vector3.Angle(flatForward, flatTarget) <= fovDegrees * 0.5f;
        }

        private static float PitchOf(Vector3 toTarget)
        {
            float flat = Mathf.Sqrt(toTarget.x * toTarget.x + toTarget.z * toTarget.z);
            return Mathf.Atan2(toTarget.y, flat) * Mathf.Rad2Deg;
        }

    }
}
