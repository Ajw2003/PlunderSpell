using System.Collections.Generic;
using UnityEngine;

namespace RogueAi.Spells
{
    /// <summary>
    /// Shared target acquisition for spell effects: an overlap query that returns the distinct
    /// components implementing a given interface, nearest first.
    ///
    /// Distinct matters: a single guard can own several colliders, and a spell that hit it once per
    /// collider would do triple damage to anything well-modelled. Nearest-first matters for the
    /// single-target spells, which take only the closest hit.
    /// </summary>
    public static class SpellTargeting
    {
        // Grows when full: inside a castle room the walls and floor alone can fill 128 slots, and a
        // capped buffer silently dropped the actual target (the same bug the extraction pad had).
        private static Collider[] _buffer = new Collider[256];

        private static int Overlap(Vector3 origin, float radius, int layerMask)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, radius, _buffer, layerMask, QueryTriggerInteraction.Collide);
            while (count == _buffer.Length)
            {
                _buffer = new Collider[_buffer.Length * 2];
                count = Physics.OverlapSphereNonAlloc(origin, radius, _buffer, layerMask, QueryTriggerInteraction.Collide);
            }
            return count;
        }

        /// <summary>
        /// What the caster is aiming at: the target closest to the crosshair within the cone and
        /// range, preferring the more centred one when two are close. Something within arm's length
        /// counts even if off-centre. Spells used to take whatever was nearest a point in front of
        /// the caster's face, often something beside or behind them (#106).
        /// </summary>
        public static T FindAimed<T>(Vector3 origin, Vector3 direction, float range, float coneDegrees,
            Transform exclude, int layerMask = ~0) where T : class
        {
            direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
            Vector3 eye = origin - direction; // the cast origin sits 1 m in front of the eye
            int count = Overlap(origin, range, layerMask);

            T best = null;
            float bestScore = float.MaxValue;
            var seen = new HashSet<object>();
            for (int i = 0; i < count; i++)
            {
                Collider hit = _buffer[i];
                if (hit == null || (exclude != null && hit.transform.IsChildOf(exclude)))
                    continue;
                var target = hit.GetComponentInParent<T>();
                if (target == null || !seen.Add(target))
                    continue;
                if (exclude != null && target is Component tc && tc.transform.IsChildOf(exclude))
                    continue;

                Vector3 toTarget = hit.bounds.center - eye;
                float distance = toTarget.magnitude;
                float angle = Vector3.Angle(direction, toTarget);
                bool pointBlank = distance < 1.5f;
                if (angle > coneDegrees && !pointBlank)
                    continue;

                // Centred beats near: a degree off the crosshair costs as much as 0.4 m of distance.
                float score = angle + distance * 2.5f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = target;
                }
            }
            return best;
        }

        /// <summary>Where the crosshair lands: the first solid thing along the aim, or the end of its range.</summary>
        public static Vector3 AimPoint(Vector3 origin, Vector3 direction, float range, Transform exclude)
        {
            direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, range, ~0, QueryTriggerInteraction.Ignore);
            float nearest = range;
            foreach (RaycastHit h in hits)
            {
                if (exclude != null && h.transform.IsChildOf(exclude))
                    continue;
                if (h.distance < nearest)
                    nearest = h.distance;
            }
            return origin + direction * nearest;
        }

        /// <summary>Every distinct <typeparamref name="T"/> within <paramref name="radius"/>, nearest first.</summary>
        public static List<T> FindAll<T>(Vector3 origin, float radius, int layerMask = ~0) where T : class
        {
            var found = new List<T>();
            var seen = new HashSet<object>();
            var distances = new List<float>();

            int count = Overlap(origin, radius, layerMask);

            for (int i = 0; i < count; i++)
            {
                Collider hit = _buffer[i];
                if (hit == null)
                    continue;

                var target = hit.GetComponentInParent<T>();
                if (target == null || !seen.Add(target))
                    continue;

                float distance = Vector3.Distance(origin, hit.transform.position);
                int insertAt = distances.Count;
                for (int j = 0; j < distances.Count; j++)
                {
                    if (distance < distances[j]) { insertAt = j; break; }
                }
                distances.Insert(insertAt, distance);
                found.Insert(insertAt, target);
            }

            return found;
        }

        /// <summary>The nearest <typeparamref name="T"/> in range, or null.</summary>
        public static T FindNearest<T>(Vector3 origin, float radius, int layerMask = ~0) where T : class
        {
            List<T> all = FindAll<T>(origin, radius, layerMask);
            return all.Count > 0 ? all[0] : null;
        }

        /// <summary>
        /// The nearest <typeparamref name="T"/> in range that is not the caster — the rule every
        /// intended (non-misfire) spell follows, so a shouted Ignis cannot roast the person casting it.
        /// </summary>
        public static T FindNearestExcluding<T>(Vector3 origin, float radius, Transform exclude,
            int layerMask = ~0) where T : class
        {
            foreach (T candidate in FindAll<T>(origin, radius, layerMask))
            {
                if (exclude != null && candidate is Component c && c.transform.IsChildOf(exclude))
                    continue;
                return candidate;
            }
            return null;
        }
    }
}
