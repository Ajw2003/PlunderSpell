using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Decides on which frames a guard actually looks (#201). Twenty guards linecasting every frame was
    /// 40-80 casts per frame; this gives each guard 12 looks a second instead. The first look is delayed
    /// by a per-guard phase, so guards that spawn together never all look on the same frame. Pure: it
    /// takes time in and says yes or no, so a test can count looks without a scene.
    /// </summary>
    public sealed class GuardSightThrottle
    {
        /// <summary>How many times a second a guard looks.</summary>
        public const float LooksPerSecond = 12f;

        private const float Interval = 1f / LooksPerSecond;
        private const int PhaseBuckets = 1000;

        private float _secondsUntilLook;

        public GuardSightThrottle(int guardId)
        {
            Phase = Mathf.Abs(guardId % PhaseBuckets) / (float)PhaseBuckets * Interval;
            _secondsUntilLook = Phase;
        }

        /// <summary>Seconds this guard waits before its first look. Between 0 and one interval.</summary>
        public float Phase { get; }

        /// <summary>Advances the clock and says whether the guard looks on this frame.</summary>
        public bool IsLookDue(float deltaTime)
        {
            _secondsUntilLook -= deltaTime;
            if (_secondsUntilLook > 0f)
                return false;

            // Carry the overshoot so the rate holds, but never owe more than one look: a long frame
            // must not be followed by a burst.
            _secondsUntilLook = Mathf.Max(_secondsUntilLook + Interval, 0f);
            return true;
        }

        /// <summary>Test seam: the next <see cref="IsLookDue"/> says yes.</summary>
        public void LookNext() => _secondsUntilLook = 0f;
    }
}
