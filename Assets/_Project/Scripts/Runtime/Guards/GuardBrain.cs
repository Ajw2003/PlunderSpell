using RogueAi.Alarm;
using UnityEngine;

namespace RogueAi.Guards
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
