using UnityEngine;

namespace Plunderspell.Spells.Vfx
{
    /// <summary>
    /// A spell going off: an expanding, fading shell of light that destroys itself. Built from a
    /// primitive rather than a particle asset because the project has no authored VFX yet.
    ///
    /// See docs/systems/spells.md, "Seeing a cast".
    /// </summary>
    public class SpellBurst : MonoBehaviour
    {
        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_color = Shader.PropertyToID("_Color");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int s_cull = Shader.PropertyToID("_Cull");

        private MaterialPropertyBlock m_block;
        private Renderer m_renderer;
        private Material m_materialInstance;
        private Light m_light;

        private Color m_colour = Color.white;
        private float m_startRadius = 0.2f;
        private float m_endRadius = 2.5f;
        private float m_duration = 0.45f;
        private float m_age;

        /// <summary>
        /// Spawns a burst at <paramref name="position"/>. Everything it needs is built here, so a
        /// caller needs no prefab and no scene setup.
        /// </summary>
        public static SpellBurst Spawn(Vector3 position, Color colour, float radius, float duration)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SpellBurst";
            go.transform.position = position;

            // A burst is light, not matter. Disabled before it is destroyed because Destroy is
            // deferred to the end of the frame, and a live collider expanding to 2.5 m would punt
            // every piece of loot in the room across it first.
            var collider = go.GetComponent<Collider>();
            collider.enabled = false;
            SafeDestroy(collider);

            var burst = go.AddComponent<SpellBurst>();
            burst.m_colour = colour;
            burst.m_endRadius = Mathf.Max(0.3f, radius);
            burst.m_startRadius = burst.m_endRadius * 0.15f;
            burst.m_duration = Mathf.Max(0.05f, duration);

            // Applied now rather than on the first Update: otherwise the first frame of every cast
            // renders as an untinted 1 m sphere at the primitive's default scale.
            burst.SetProgress(0f);
            return burst;
        }

        private void Awake()
        {
            m_renderer = GetComponent<Renderer>();
            EnsureGlow();
            EnsureDoubleSided();
        }

        /// <summary>See docs/systems/spells.md, "A burst is invisible from inside itself".</summary>
        private void EnsureDoubleSided()
        {
            if (m_renderer == null || m_materialInstance != null)
            {
                return;
            }

            // Object.Instantiate + assigning to sharedMaterial gets the same per-burst instance the
            // renderer.material getter would, without its "will leak materials" edit-mode warning —
            // and OnDestroy below cleans the instance up so it does not actually leak either.
            m_materialInstance = Object.Instantiate(m_renderer.sharedMaterial);
            m_renderer.sharedMaterial = m_materialInstance;

            if (m_materialInstance.HasProperty(s_cull))
            {
                m_materialInstance.SetFloat(s_cull, (float)UnityEngine.Rendering.CullMode.Off);
            }
        }

        private void OnDestroy() => SafeDestroy(m_materialInstance);

        /// <summary>Destroy is illegal outside play mode; every capture tool runs this in edit mode.</summary>
        private static void SafeDestroy(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        private void EnsureGlow()
        {
            if (m_light != null)
            {
                return;
            }

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(transform, false);
            m_light = lightGo.AddComponent<Light>();
            m_light.type = LightType.Point;
        }

        private void Update()
        {
            m_age += Time.deltaTime;
            float t = Mathf.Clamp01(m_age / m_duration);
            SetProgress(t);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Puts the burst at <paramref name="t"/> (0..1) through its life. Public so a capture tool
        /// can photograph a burst mid-bloom without running the game.
        /// </summary>
        public void SetProgress(float t)
        {
            t = Mathf.Clamp01(t);

            if (m_renderer == null)
            {
                // Spawn() calls SetProgress before returning, and Awake does not reliably run
                // synchronously from every AddComponent call site (editor automation in particular) —
                // so this cannot assume Awake has already set these up.
                m_renderer = GetComponent<Renderer>();
                EnsureDoubleSided();
            }

            // Fast out, slow to a stop: a spell should arrive rather than drift outwards.
            float eased = 1f - (1f - t) * (1f - t);
            float radius = Mathf.Lerp(m_startRadius, m_endRadius, eased);
            transform.localScale = Vector3.one * (radius * 2f);

            float fade = 1f - t;
            Tint(m_colour, fade);

            if (m_light != null)
            {
                m_light.color = m_colour;
                m_light.range = radius * 4f;
                m_light.intensity = 4f * fade;
            }
        }

        private void Tint(Color colour, float fade)
        {
            if (m_renderer == null)
            {
                return;
            }

            m_block ??= new MaterialPropertyBlock();
            var faded = new Color(colour.r, colour.g, colour.b, fade);

            m_renderer.GetPropertyBlock(m_block);
            m_block.SetColor(s_baseColor, faded);
            m_block.SetColor(s_color, faded);
            m_block.SetColor(s_emissionColor, colour * (3f * fade));
            m_renderer.SetPropertyBlock(m_block);
        }
    }
}
