using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Decides where a guard goes for a sound it heard (#485). A 0.3 m sphere is swept from the guard toward the
    /// source: a clear line means the guard walks to the sound itself. A wall in the way means the sound came in
    /// through a doorway or hole, so the guard goes to the first one on its route and faces the way back to the
    /// source from there. The map answers the doorway question; this class only owns the decision.
    /// </summary>
    public sealed class GuardSoundApproach
    {
        /// <summary>How thick the sweep is, so a gap too narrow to walk through counts as a wall.</summary>
        public const float SweepRadius = 0.3f;

        // The sweep runs at chest height and stops this short of the source, so the thing that made the noise does not block itself.
        private const float SweepHeight = 1f;
        private const float StopShortOfSource = 0.5f;
        private const int HitBufferSize = 16;

        private readonly Guard _guard;
        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];

        public GuardSoundApproach(Guard guard)
        {
            _guard = guard;
        }

        /// <summary>
        /// True when a wall is in the way and the map knows a doorway on the route: <paramref name="entry"/> is that
        /// doorway and <paramref name="heading"/> points from it back to the source. False means go to the source.
        /// </summary>
        public bool TryFindEntry(Vector3 source, out Vector3 entry, out Vector3 heading)
        {
            entry = source;
            heading = Vector3.zero;
            if (!IsWallInTheWay(source))
                return false;

            IGuardNavigationMap map = _guard.Link.Director != null ? _guard.Link.Director.Navigation.Map : null;
            if (map == null || !map.TryFindFirstDoorway(_guard.transform.position, source, out entry))
            {
                entry = source;
                return false;
            }

            heading = source - entry;
            heading.y = 0f;
            if (heading.sqrMagnitude < 0.0001f)
                return false;
            heading.Normalize();
            return true;
        }

        private bool IsWallInTheWay(Vector3 source)
        {
            Vector3 origin = _guard.transform.position + Vector3.up * SweepHeight;
            Vector3 toSource = source + Vector3.up * SweepHeight - origin;
            float distance = toSource.magnitude - StopShortOfSource;
            if (distance <= 0f)
                return false;

            int count = Physics.SphereCastNonAlloc(origin, SweepRadius, toSource.normalized, _hits, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i].collider;
                if (!hit.transform.IsChildOf(_guard.transform) && hit.GetComponentInParent<Guard>() == null)
                    return true;
            }
            return false;
        }
    }
}
