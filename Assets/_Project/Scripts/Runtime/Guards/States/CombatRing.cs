using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Where a guard waits its turn (#210): a place on a ring around the player. The place comes from the
    /// guard's own id, spread by the golden angle so guards that start close together fan out instead of
    /// queueing on one spot. A sidestep turns the place by a fixed step, which is how a ranged guard whose
    /// shot is blocked gets a new line. Pure maths, so it is tested without a scene.
    /// </summary>
    public static class CombatRing
    {
        private const float GoldenAngleDegrees = 137.508f;
        private const float SidestepDegrees = 50f;
        private const int IdSpread = 1024;

        /// <summary>The point on the ring of <paramref name="radius"/> around <paramref name="target"/> for
        /// this guard, on the target's floor level.</summary>
        public static Vector3 SlotAround(Vector3 target, int guardId, int sidestep, float radius)
        {
            float degrees = (guardId & (IdSpread - 1)) * GoldenAngleDegrees + sidestep * SidestepDegrees;
            float radians = degrees * Mathf.Deg2Rad;
            return target + new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians)) * radius;
        }
    }
}
