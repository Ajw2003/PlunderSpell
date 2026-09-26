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
        private static readonly Collider[] _buffer = new Collider[128];

        /// <summary>Every distinct <typeparamref name="T"/> within <paramref name="radius"/>, nearest first.</summary>
        public static List<T> FindAll<T>(Vector3 origin, float radius, int layerMask = ~0) where T : class
        {
            var found = new List<T>();
            var seen = new HashSet<object>();
            var distances = new List<float>();

            int count = Physics.OverlapSphereNonAlloc(origin, radius, _buffer, layerMask,
                QueryTriggerInteraction.Collide);

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
