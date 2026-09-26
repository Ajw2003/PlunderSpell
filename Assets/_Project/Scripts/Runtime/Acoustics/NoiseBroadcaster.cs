using UnityEngine;

namespace RogueAi.Acoustics
{
    /// <summary>
    /// Component-free noise propagation. <see cref="AcousticEmitter"/> is the authored, per-object
    /// entry point; this is the same propagation available to code that has a position but no
    /// emitter — spell effects, shattering loot, a guard's shout.
    ///
    /// The attenuation model is identical (and shared, not duplicated): every sound-blocking wall
    /// between source and listener halves the perceived strength, capped at
    /// <see cref="AcousticEmitter.MaxWallSegments"/> walls, and anything quieter than
    /// <see cref="AcousticEmitter.MinAudibleStrength"/> is dropped.
    /// </summary>
    public static class NoiseBroadcaster
    {
        private static readonly Collider[] _overlapBuffer = new Collider[64];

        /// <summary>
        /// Delivers a noise to every <see cref="INoiseListener"/> within <paramref name="radius"/>,
        /// attenuated by the walls in between. Returns how many listeners actually heard it, which is
        /// what makes this directly assertable in tests.
        /// </summary>
        public static int Broadcast(Vector3 origin, float radius, float strength, NoiseType type,
            int listenerLayerMask = ~0, int geometryLayerMask = 0)
        {
            if (radius <= 0f || strength <= 0f)
                return 0;

            int heard = 0;
            int count = Physics.OverlapSphereNonAlloc(origin, radius, _overlapBuffer,
                listenerLayerMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider hit = _overlapBuffer[i];
                if (hit == null)
                    continue;

                var listener = hit.GetComponentInParent<INoiseListener>();
                if (listener == null)
                    continue;

                int walls = CountWalls(origin, hit.transform.position, geometryLayerMask);
                float attenuated = AcousticEmitter.ComputeAttenuatedStrength(strength, walls);
                if (attenuated <= AcousticEmitter.MinAudibleStrength)
                    continue;

                listener.OnNoiseHeard(new NoiseEvent(origin, attenuated, type));
                heard++;
            }

            return heard;
        }

        /// <summary>Counts sound-blocking walls on the segment, capped at the model's maximum.</summary>
        public static int CountWalls(Vector3 from, Vector3 to, int geometryLayerMask)
        {
            if (geometryLayerMask == 0)
                return 0;

            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist <= Mathf.Epsilon)
                return 0;

            RaycastHit[] hits = Physics.RaycastAll(from, dir.normalized, dist, geometryLayerMask,
                QueryTriggerInteraction.Ignore);
            return Mathf.Min(hits.Length, AcousticEmitter.MaxWallSegments);
        }
    }
}
