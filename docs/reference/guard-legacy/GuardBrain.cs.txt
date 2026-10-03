using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Everything a guard decides, as pure functions. No scene, no components, no time — just the
    /// inputs a guard has and the conclusion it reaches.
    ///
    /// Keeping the decisions here rather than inside the MonoBehaviour is what makes guard behaviour
    /// testable: "does a shouted spell three rooms away actually pull a guard off patrol" is an
    /// assertion about <see cref="NextState"/>, not something to watch for in play.
    /// </summary>
    public static class GuardBrain
    {
        /// <summary>Noise quieter than this is background; a guard never reacts to it.</summary>
        public const float NoiseNoticeThreshold = 0.12f;

        /// <summary>Seconds a guard keeps searching after losing sight before giving up.</summary>
        public const float SearchPatience = 12f;

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

        // -----------------------------------------------------------------------------------------
        // Never standing still (#193). See docs/4-systems/raid.md, "Guards that keep moving".
        // -----------------------------------------------------------------------------------------

        /// <summary>A guard with a destination that covers less than <see cref="StuckProgress"/> in
        /// this many seconds is stuck.</summary>
        public const float StuckSeconds = 1.5f;

        /// <summary>Metres a guard must cover within <see cref="StuckSeconds"/> to count as moving.</summary>
        public const float StuckProgress = 0.3f;

        /// <summary>Seconds an investigating guard turns on the spot after reaching the noise.</summary>
        public const float LookAroundSeconds = 1.6f;

        /// <summary>Whether a guard that moved <paramref name="moved"/> metres over a full stuck window
        /// has made no real progress.</summary>
        public static bool IsStuck(float moved) => moved < StuckProgress;

        /// <summary>
        /// Where the <paramref name="index"/>-th search point lies around the last-known position, as
        /// an offset: the angle steps 100 degrees each time (so neighbours are not side by side) and
        /// the distance cycles through 4, 6 and 8 metres. Pure, so a guard's sweep is a known pattern.
        /// </summary>
        public static Vector3 SweepOffset(int index)
        {
            float angle = index * 100f * Mathf.Deg2Rad;
            float radius = 4f + (Mathf.Abs(index) % 3) * 2f;
            return new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
        }

        // -----------------------------------------------------------------------------------------
        // Following sound and the hue and cry (#194, #195)
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Whether a noise should steer a guard that is already hunting: it must be loud enough to
        /// notice, and the guard must be searching, or chasing without eyes on anyone. A guard that
        /// can see its target has no use for a noise.
        /// </summary>
        public static bool ShouldFollowNoise(GuardAlertState state, bool seesIntruder,
            float noiseStrength, AlarmState alarm)
        {
            if (!ShouldInvestigate(noiseStrength, alarm))
                return false;
            return state == GuardAlertState.Searching
                || (state == GuardAlertState.Chasing && !seesIntruder);
        }

        /// <summary>Whether the hue and cry should keep sending a guard in this state toward the
        /// players: it is at the top alert and the guard is not already fighting or down.</summary>
        public static bool ShouldHunt(GuardAlertState state, AlarmState alarm) =>
            alarm >= AlarmState.HueAndCry
            && (state == GuardAlertState.Searching || state == GuardAlertState.Investigating);

        /// <summary>Metres from a player the hue and cry sends a guard: they know roughly where, so
        /// players who break away and hide can still slip the hunt.</summary>
        public const float HuntOffsetMin = 3f;
        public const float HuntOffsetMax = 5f;

        /// <summary>Seconds between re-sends at the hue and cry, before the per-guard stagger.</summary>
        public const float HuntInterval = 3f;

        /// <summary>
        /// Seconds until a guard is next re-sent at the hue and cry: <see cref="HuntInterval"/> plus
        /// up to a second that depends on the guard, so twenty guards do not all re-path on one frame.
        /// </summary>
        public static float HuntDelay(int guardId) => HuntInterval + (Mathf.Abs(guardId) % 10) * 0.1f;

        /// <summary>Whether a re-send is due once <paramref name="timeSinceSent"/> seconds have passed.</summary>
        public static bool HuntDue(float timeSinceSent, int guardId) => timeSinceSent >= HuntDelay(guardId);

        /// <summary>
        /// The rough offset from a player's position: a direction (<paramref name="angle01"/>, a turn
        /// from 0 to 1) and a distance between <see cref="HuntOffsetMin"/> and
        /// <see cref="HuntOffsetMax"/> (<paramref name="distance01"/>). Random numbers come in so
        /// this stays pure.
        /// </summary>
        public static Vector3 HuntOffset(float angle01, float distance01)
        {
            float angle = angle01 * Mathf.PI * 2f;
            float radius = Mathf.Lerp(HuntOffsetMin, HuntOffsetMax, Mathf.Clamp01(distance01));
            return new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
        }

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

            return Vector3.Angle(forward, toTarget) <= fovDegrees * 0.5f;
        }

        /// <summary>
        /// The state a guard should be in next.
        ///
        /// The rule that gives the alarm teeth is the last one: at
        /// <see cref="AlarmState.HueAndCry"/> a guard that has lost its target does not go back to
        /// patrolling. Once the castle is up, it stays up — so letting the alarm max out is a
        /// decision the players do not get to take back.
        /// </summary>
        public static GuardAlertState NextState(GuardAlertState current, bool incapacitated,
            bool seesIntruder, bool hasInvestigationTarget, float timeSinceLastContact,
            AlarmState alarm)
        {
            if (incapacitated)
                return GuardAlertState.Incapacitated;

            if (seesIntruder)
                return GuardAlertState.Chasing;

            switch (current)
            {
                case GuardAlertState.Incapacitated:
                    // Just woke up. An alerted castle means straight back to searching, not patrolling.
                    return alarm >= AlarmState.Roused ? GuardAlertState.Searching : GuardAlertState.Patrolling;

                case GuardAlertState.Chasing:
                    return GuardAlertState.Searching;

                case GuardAlertState.Searching:
                    if (timeSinceLastContact < SearchPatience)
                        return GuardAlertState.Searching;
                    return alarm >= AlarmState.HueAndCry
                        ? GuardAlertState.Searching   // the hunt never ends once the castle is up
                        : GuardAlertState.Patrolling;

                case GuardAlertState.Investigating:
                    if (hasInvestigationTarget)
                        return GuardAlertState.Investigating;
                    return GuardAlertState.Patrolling;

                default:
                    return hasInvestigationTarget ? GuardAlertState.Investigating : GuardAlertState.Patrolling;
            }
        }
    }
}
