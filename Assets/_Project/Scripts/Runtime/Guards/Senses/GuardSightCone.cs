using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>Where in a guard's view a point falls (#484).</summary>
    public enum GuardViewZone
    {
        Unseen,
        /// <summary>Seen at once: the centre of the view, or inside the circle round the guard.</summary>
        Immediate,
        /// <summary>The edge of the view: noticed only after <see cref="GuardSideNotice.DelaySeconds"/> in view.</summary>
        Side
    }

    /// <summary>
    /// The three layers of a guard's view (#484). The centre (the tuning's field of view, 110 degrees) reaches the full
    /// range and is seen at once; the sides out to 180 degrees reach half the range; a 2.5 m circle all round is seen
    /// at once. All three keep the up-and-down limit of <see cref="GuardBrain.CanSee"/>. Pure: positions in, a zone
    /// out. Walls are the caller's business.
    /// </summary>
    public static class GuardSightCone
    {
        public const float CircleRadius = 2.5f;
        public const float SideFieldOfView = 180f;
        public const float SideRangeRatio = 0.5f;

        public static GuardViewZone ZoneOf(Vector3 eye, Vector3 forward, Vector3 target, float range, float centreFieldOfView)
        {
            if (GuardBrain.CanSee(eye, forward, target, CircleRadius, 360f, true)
                || GuardBrain.CanSee(eye, forward, target, range, centreFieldOfView, true))
                return GuardViewZone.Immediate;

            return GuardBrain.CanSee(eye, forward, target, range * SideRangeRatio, SideFieldOfView, true)
                ? GuardViewZone.Side
                : GuardViewZone.Unseen;
        }
    }
}
