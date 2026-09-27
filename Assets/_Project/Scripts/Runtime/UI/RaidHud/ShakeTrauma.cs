using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>
    /// Camera shake as "trauma": events add to a 0..1 value that drains steadily, and the view moves
    /// by trauma squared, so small bumps barely register and big ones stack into a real jolt. Plain
    /// C#, so it can be tested without a scene. See docs/4-systems/damage.md, "Camera shake".
    /// </summary>
    public class ShakeTrauma
    {
        /// <summary>Trauma lost per second.</summary>
        public const float DecayPerSecond = 1.5f;

        /// <summary>Largest pitch and yaw swing, in degrees, at full trauma.</summary>
        public const float MaxTurnDegrees = 5f;

        /// <summary>Largest roll, in degrees, at full trauma.</summary>
        public const float MaxRollDegrees = 6f;

        /// <summary>How fast the shake wanders, in noise samples per second.</summary>
        private const float k_frequency = 22f;

        private float _time;

        /// <summary>Current trauma, 0..1.</summary>
        public float Trauma { get; private set; }

        /// <summary>Adds trauma, capped at 1. Negative amounts are ignored.</summary>
        public void Add(float amount)
        {
            if (amount > 0f)
                Trauma = Mathf.Min(1f, Trauma + amount);
        }

        /// <summary>Drains trauma and returns this frame's view offset.</summary>
        public Quaternion Step(float deltaTime)
        {
            _time += deltaTime;
            Trauma = Mathf.Max(0f, Trauma - DecayPerSecond * deltaTime);
            if (Trauma <= 0f)
                return Quaternion.identity;

            float shake = Trauma * Trauma;
            float t = _time * k_frequency;
            // Three unrelated rows of Perlin noise, recentred on zero, so the axes don't move together.
            float pitch = MaxTurnDegrees * shake * (Mathf.PerlinNoise(t, 0.1f) * 2f - 1f);
            float yaw = MaxTurnDegrees * shake * (Mathf.PerlinNoise(t, 7.3f) * 2f - 1f);
            float roll = MaxRollDegrees * shake * (Mathf.PerlinNoise(t, 13.7f) * 2f - 1f);
            return Quaternion.Euler(pitch, yaw, roll);
        }

        /// <summary>Stops shaking at once.</summary>
        public void Clear() => Trauma = 0f;
    }
}
