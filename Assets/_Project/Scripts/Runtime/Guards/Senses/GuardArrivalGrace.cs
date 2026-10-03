using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The grace after a raid starts: until it runs out, and while the alarm is still calm, no guard sees
    /// a player. Guards still hear, so noise still draws them. Without it a patrol passing within sight
    /// of the gate killed players still reading the HUD (seen in co-op testing, 2026-09-23). It is
    /// process-wide because it belongs to the raid, not to a guard.
    /// </summary>
    public static class GuardArrivalGrace
    {
        /// <summary>How long after a raid starts a calm garrison cannot see the players.</summary>
        public const float Seconds = 20f;

        private static float s_endsAt;

        public static bool IsActive => Time.time < s_endsAt;

        public static void Begin() => s_endsAt = Time.time + Seconds;

        /// <summary>Ends the grace now. For tests: a test that started a raid would otherwise blind the next one's guards.</summary>
        public static void End() => s_endsAt = 0f;
    }
}
