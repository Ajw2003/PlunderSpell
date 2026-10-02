using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The one friendly-fire check (#210): is a teammate standing in the path of a shot? A sphere is swept
    /// from the shooter to the aim point and the nearest thing it hits decides: a teammate blocks the
    /// shot, a wall or the target itself does not (a wall in front of a teammate stops the shot first).
    /// <see cref="GuardRangedAttack"/> calls it before every shot, so Chase and Combat share it. The hit
    /// buffer is made once, so a check allocates nothing.
    /// </summary>
    public sealed class GuardLineOfFire
    {
        private const int HitBufferSize = 16;

        private readonly Guard _shooter;
        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];

        public GuardLineOfFire(Guard shooter)
        {
            _shooter = shooter;
        }

        /// <summary>True when a teammate is the first thing between <paramref name="origin"/> and
        /// <paramref name="aim"/>. Triggers and the shooter's own and the target's colliders are skipped.</summary>
        public bool IsBlockedByTeammate(Vector3 origin, Vector3 aim, Transform target)
        {
            Vector3 toAim = aim - origin;
            float distance = toAim.magnitude;
            if (distance <= 0f)
                return false;

            int count = Physics.SphereCastNonAlloc(origin, _shooter.Tuning.ShotClearanceRadius, toAim / distance,
                _hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return NearestObstacleIsTeammate(count, target);
        }

        private bool NearestObstacleIsTeammate(int count, Transform target)
        {
            float nearest = float.MaxValue;
            bool teammate = false;
            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i].collider;
                if (_hits[i].distance >= nearest || IsShooterOrTarget(hit.transform, target))
                    continue;

                nearest = _hits[i].distance;
                teammate = IsTeammate(hit);
            }
            return teammate;
        }

        private bool IsShooterOrTarget(Transform hit, Transform target)
        {
            return hit.IsChildOf(_shooter.transform) || (target != null && hit.IsChildOf(target));
        }

        // The legacy CastleGuard is still on the prefabs until #214, so it counts as a teammate too.
        private static bool IsTeammate(Collider hit)
        {
            return hit.GetComponentInParent<Guard>() != null || hit.GetComponentInParent<CastleGuard>() != null;
        }
    }
}
