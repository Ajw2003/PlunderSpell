using UnityEngine;

namespace Plunderspell.Acoustics
{
    /// <summary>Category of a noise event — drives alarm weighting and enemy reaction copy.</summary>
    public enum NoiseType
    {
        Footstep,
        GlassBreak,
        Gunshot,
        VoiceCast,
        ItemDrop,
        Explosion,
        MeleeSwing
    }

    /// <summary>
    /// Immutable description of a single noise occurrence, delivered to every <see cref="INoiseListener"/>
    /// in range after distance + occlusion attenuation has been applied.
    /// </summary>
    public struct NoiseEvent
    {
        /// <summary>World position the noise originated from.</summary>
        public Vector3 Origin;

        /// <summary>Normalised (0..1) loudness after wall occlusion attenuation.</summary>
        public float Strength;

        /// <summary>What produced the noise.</summary>
        public NoiseType Type;

        public NoiseEvent(Vector3 origin, float strength, NoiseType type)
        {
            Origin = origin;
            Strength = strength;
            Type = type;
        }
    }

    /// <summary>
    /// Implemented by anything that reacts to sound — the alarm FSM and individual enemy AI. An
    /// <see cref="AcousticEmitter"/> raises <see cref="OnNoiseHeard"/> on every listener whose collider
    /// falls inside the emitter's radius and whose line of sound is not fully blocked by geometry.
    /// </summary>
    public interface INoiseListener
    {
        void OnNoiseHeard(NoiseEvent noise);
    }
}
