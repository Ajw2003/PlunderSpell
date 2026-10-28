using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Where the Ophan's charged beam bursts through stone (#461): a glowing hole with dust and embers blown out the far
    /// side, shown on every machine half a second before the hit lands, so the target sees the wall give way. This is
    /// the look of the burst only: the hole itself is now cut by OphanWallCut (#467), reversing pick 9A. Fades and
    /// removes itself.
    /// </summary>
    public sealed class OphanWallBurst : MonoBehaviour
    {
        private const float HoleMetres = 2.4f;
        private const float GlowSeconds = 4f;
        private static readonly Color Molten = new Color(1f, 0.55f, 0.2f, 1f);

        private Material _glow;
        private float _bornAt;

        /// <summary>A burst where the beam, travelling along <paramref name="beamDirection"/>, meets stone at <paramref name="point"/>.</summary>
        public static void At(Vector3 point, Vector3 beamDirection)
        {
            var go = new GameObject("OphanWallBurst");
            go.transform.SetPositionAndRotation(point, Quaternion.LookRotation(beamDirection));
            go.AddComponent<OphanWallBurst>();
        }

        private void Awake()
        {
            _bornAt = Time.time;
            var beam = Resources.Load<Material>("GrabBeam");
            _glow = new Material(beam);
            _glow.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            BuildHole();
            BuildEmbers(beam);
            Destroy(gameObject, GlowSeconds);
        }

        private void BuildHole()
        {
            GameObject hole = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(hole.GetComponent<Collider>());
            hole.name = "Hole";
            hole.transform.SetParent(transform, false);
            hole.transform.localScale = Vector3.one * HoleMetres;
            var shown = hole.GetComponent<MeshRenderer>();
            shown.sharedMaterial = _glow;
            shown.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
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
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
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
            GetComponent<ParticleSystemRenderer>().sharedMaterial = beam;
            embers.Play();
        }

        // per-frame: the hole cools from white-hot to nothing over a few seconds
        private void Update()
        {
            float t = Mathf.Clamp01((Time.time - _bornAt) / GlowSeconds);
            Color colour = Color.Lerp(Color.white, Molten, t * 3f);
            colour.a = 1f - t * t;
            _glow.SetColor("_BaseColor", colour * 2f);
        }

        private void OnDestroy() => Destroy(_glow);
    }
}
