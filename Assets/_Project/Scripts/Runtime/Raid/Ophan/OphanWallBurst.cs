using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Where the Ophan's charged beam bursts through stone (#461): sparks and dust blown out the far side, shown on every
    /// machine, so the target sees the wall give way. The hole itself is broken by the fracture system (#472); the glowing
    /// square that stood in for it before is gone (aj saw it hang in front of the eye, 2026-10-28). Removes itself.
    /// </summary>
    public sealed class OphanWallBurst : MonoBehaviour
    {
        private const float HoleMetres = 2.4f;
        private const float LifeSeconds = 4f;
        private static readonly Color Molten = new Color(1f, 0.55f, 0.2f, 1f);

        /// <summary>A burst where the beam, travelling along <paramref name="beamDirection"/>, meets stone at <paramref name="point"/>.</summary>
        public static void At(Vector3 point, Vector3 beamDirection)
        {
            var go = new GameObject("OphanWallBurst");
            go.transform.SetPositionAndRotation(point, Quaternion.LookRotation(beamDirection));
            go.AddComponent<OphanWallBurst>();
        }

        private void Awake()
        {
            BuildEmbers(Resources.Load<Material>("GrabBeam"));
            Destroy(gameObject, LifeSeconds);
        }

        // Dust and embers thrown on along the beam, out of the wall's far side.
        private void BuildEmbers(Material beam)
        {
            var embers = gameObject.AddComponent<ParticleSystem>();
            embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = embers.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(Molten, new Color(0.5f, 0.45f, 0.4f, 0.8f));
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            ParticleSystem.EmissionModule emission = embers.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 120) });
            ParticleSystem.ShapeModule shape = embers.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = HoleMetres * 0.4f;
            // Stretched along their flight so they read as sparks: the beam material has no round mask, so plain
            // billboards drew as squares.
            var sparks = GetComponent<ParticleSystemRenderer>();
            sparks.sharedMaterial = beam;
            sparks.renderMode = ParticleSystemRenderMode.Stretch;
            sparks.velocityScale = 0.04f;
            embers.Play();
        }
    }
}
