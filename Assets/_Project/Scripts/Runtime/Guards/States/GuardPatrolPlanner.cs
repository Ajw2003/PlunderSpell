using System.Collections.Generic;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Chooses patrol points. A point counts only if the castle map has walkable floor under it and a
    /// complete route to it from the guard's post exists right now, so a barred door (which the map
    /// reports as "no route") rules a point out. It uses the guard's own seeded random numbers, so the
    /// server picks the same points every time for the same seed.
    /// </summary>
    public sealed class GuardPatrolPlanner
    {
        private readonly GuardTuning _tuning;
        private readonly System.Func<System.Random> _random;
        private readonly List<Vector3> _routeScratch = new List<Vector3>();

        /// <param name="random">Asked for on every pick, so a reseed on the guard takes effect at once.</param>
        public GuardPatrolPlanner(GuardTuning tuning, System.Func<System.Random> random)
        {
            _tuning = tuning;
            _random = random;
        }

        /// <summary>Fills <paramref name="route"/> with a fresh round. It may hold fewer points than asked for if the post is boxed in.</summary>
        public void PlanRound(IGuardNavigationMap map, Vector3 post, GuardPatrolRoute route)
        {
            route.Clear();
            int wanted = Mathf.Max(_tuning.PatrolPointCount, 1);
            for (int i = 0; i < wanted; i++)
            {
                if (TryPickPoint(map, post, route, out Vector3 point))
                    route.Add(point);
            }
        }

        /// <summary>Tries random points around <paramref name="post"/> until one is reachable and not already on the route.</summary>
        public bool TryPickPoint(IGuardNavigationMap map, Vector3 post, GuardPatrolRoute route, out Vector3 point)
        {
            point = post;
            if (map == null)
                return false;

            for (int attempt = 0; attempt < _tuning.PatrolPickAttempts; attempt++)
            {
                Vector3 candidate = RandomPointAround(post);
                if (route.HasPointNear(candidate, _tuning.PatrolPointSpacing) || !IsReachable(map, post, candidate))
                    continue;

                point = candidate;
                return true;
            }
            return false;
        }

        private Vector3 RandomPointAround(Vector3 post)
        {
            System.Random random = _random();
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float radius = Mathf.Lerp(_tuning.PatrolMinimumRadius, _tuning.PatrolMaximumRadius, (float)random.NextDouble());
            return post + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        // FindCell finds the floor under the point; TryFindPath proves a whole route, doors included.
        private bool IsReachable(IGuardNavigationMap map, Vector3 post, Vector3 candidate)
        {
            return map.FindCell(candidate) >= 0 && map.TryFindPath(post, candidate, _routeScratch, out BlockedReason ignored);
        }
    }
}
