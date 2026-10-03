using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// A puff of dust for a dead guard (#213). No dust effect exists in the project, so this builds the
    /// smallest one: a particle system on its own object (not a child, so it outlives the shrinking body),
    /// that removes itself once the particles are gone. The material is the flat sprite shader the
    /// project's grab beam already uses.
    /// </summary>
    public sealed class GuardDust
    {
        private const float LifetimeSeconds = 1.2f;

        private readonly Transform _body;

        public GuardDust(Transform body)
        {
            _body = body;
        }

        /// <summary>Puffs <paramref name="particleCount"/> particles out at <paramref name="height"/> above the body's pivot.</summary>
        public void Burst(int particleCount, float height)
        {
            var puff = new GameObject("GuardDust");
            puff.transform.position = _body.position + Vector3.up * height;
            ParticleSystem particles = puff.AddComponent<ParticleSystem>();
            Configure(particles);
            particles.Emit(particleCount);
            Object.Destroy(puff, LifetimeSeconds + 0.5f);
        }

        private static void Configure(ParticleSystem particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = LifetimeSeconds;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor = new Color(0.62f, 0.56f, 0.46f, 0.8f);
            main.gravityModifier = -0.05f;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                particles.GetComponent<ParticleSystemRenderer>().material = new Material(shader);
        }
    }
}
