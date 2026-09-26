using UnityEngine;

namespace RogueAi.Acoustics
{
    /// <summary>
    /// Emits noise into the world. Attach to footstep audio sources, breakable loot, spell effects or
    /// the player's "mouth". On <see cref="Emit"/> (or <see cref="EmitWithAmplitude"/>) it gathers every
    /// <see cref="INoiseListener"/> inside <see cref="BaseNoiseRadius"/> and delivers a
    /// <see cref="NoiseEvent"/> to each, attenuated by any walls between emitter and listener.
    ///
    /// Occlusion model: a ray is cast from the emitter toward each listener; every "Geometry" wall it
    /// crosses halves the perceived strength (×0.5 per wall, up to 3 segments). Noise below
    /// <see cref="MinAudibleStrength"/> is dropped.
    /// </summary>
    public class AcousticEmitter : MonoBehaviour
    {
        [Header("Noise profile")]
        [Tooltip("Base radius in metres (e.g. 5 for a footstep, 15 for a gunshot).")]
        public float BaseNoiseRadius = 5.0f;

        [Tooltip("Base loudness, normalised 0..1.")]
        [Range(0f, 1f)]
        public float BaseNoiseStrength = 0.5f;

        [Tooltip("Category tag for this emitter's noise.")]
        public NoiseType NoiseType = NoiseType.Footstep;

        [Header("Layers")]
        [Tooltip("Layers that contain INoiseListener components (alarm manager, enemies).")]
        [SerializeField] private LayerMask _noiseListenerLayer = ~0;

        [Tooltip("Layers treated as sound-blocking walls for occlusion.")]
        [SerializeField] private LayerMask _geometryLayer;

        /// <summary>Per-wall attenuation factor.</summary>
        public const float WallAttenuation = 0.5f;

        /// <summary>Maximum number of wall segments considered before a noise is treated as blocked.</summary>
        public const int MaxWallSegments = 3;

        /// <summary>Noise quieter than this (after attenuation) is inaudible and discarded.</summary>
        public const float MinAudibleStrength = 0.05f;

        /// <summary>Emits this emitter's default noise profile.</summary>
        public void Emit() => EmitNoise(BaseNoiseRadius, BaseNoiseStrength);

        /// <summary>Emits a scaled version of the profile (e.g. run vs. walk, or shout vs. whisper).</summary>
        public void EmitWithAmplitude(float amplitudeMultiplier)
        {
            EmitNoise(BaseNoiseRadius * amplitudeMultiplier,
                      Mathf.Clamp01(BaseNoiseStrength * amplitudeMultiplier));
        }

        /// <summary>
        /// Core propagation: find listeners in range, attenuate by wall occlusion and notify each.
        /// Delegates to <see cref="NoiseBroadcaster"/> so authored emitters and code-driven noise
        /// (spell effects, shattering loot) cannot drift apart. Returns the number of listeners
        /// that heard it.
        /// </summary>
        public int EmitNoise(float radius, float strength) =>
            NoiseBroadcaster.Broadcast(transform.position, radius, strength, NoiseType,
                _noiseListenerLayer, _geometryLayer);

        /// <summary>
        /// Pure occlusion maths: <c>strength × 0.5^wallCount</c>, clamped to at most
        /// <see cref="MaxWallSegments"/> walls. Network-free and side-effect-free so it is directly
        /// unit-testable.
        /// </summary>
        public static float ComputeAttenuatedStrength(float strength, int wallCount)
        {
            wallCount = Mathf.Clamp(wallCount, 0, MaxWallSegments);
            return strength * Mathf.Pow(WallAttenuation, wallCount);
        }
    }
}
