using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Atmosphere
{
    /// <summary>
    /// Moves an enemy's or a piece of plunder's opaque URP Lit materials onto Plunderspell/Surface in
    /// texture mode (docs/plans/night-atmosphere.md, section 3): they keep their ArtForge baked
    /// textures and emission, and take the castle's banded light, so they sit in the same painted
    /// world. One converted material per source material, shared by every copy.
    /// </summary>
    public static class SurfaceConverter
    {
        private static readonly Dictionary<Material, Material> s_converted = new Dictionary<Material, Material>();
        private static readonly List<Material> s_scratch = new List<Material>();

        private static readonly int s_baseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int s_mainTex = Shader.PropertyToID("_MainTex");
        private static readonly int s_baseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_emissionMap = Shader.PropertyToID("_EmissionMap");
        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int s_surface = Shader.PropertyToID("_Surface");

        /// <summary>Converts every eligible material under <paramref name="root"/>. Particles and trails are left alone.</summary>
        public static void Convert(GameObject root, Shader surface)
        {
            if (root == null || surface == null)
                return;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                    continue;
                renderer.GetSharedMaterials(s_scratch);
                bool changed = false;
                for (int i = 0; i < s_scratch.Count; i++)
                {
                    Material converted = Converted(s_scratch[i], surface);
                    if (converted != s_scratch[i])
                    {
                        s_scratch[i] = converted;
                        changed = true;
                    }
                }
                if (changed)
                    renderer.SetSharedMaterials(s_scratch);
            }
        }

        private static Material Converted(Material source, Shader surface)
        {
            if (source == null || source.shader == surface)
                return source;
            if (s_converted.TryGetValue(source, out Material cached))
                return cached;

            // Only opaque lit materials: transparent, unlit and effect shaders have their own reasons.
            string shaderName = source.shader.name;
            bool isLit = shaderName == "Universal Render Pipeline/Lit" || shaderName == "Universal Render Pipeline/Simple Lit";
            bool isOpaque = !source.HasProperty(s_surface) || source.GetFloat(s_surface) < 0.5f;
            if (!isLit || !isOpaque)
            {
                s_converted[source] = source;
                return source;
            }

            var material = new Material(surface) { name = source.name + " (Surface)" };
            Texture baseMap = source.HasProperty(s_baseMap) ? source.GetTexture(s_baseMap)
                : source.HasProperty(s_mainTex) ? source.GetTexture(s_mainTex) : null;
            if (baseMap != null)
                material.SetTexture(s_baseMap, baseMap);
            if (source.HasProperty(s_baseColor))
                material.SetColor(s_baseColor, source.GetColor(s_baseColor));
            if (source.IsKeywordEnabled("_EMISSION"))
            {
                if (source.HasProperty(s_emissionMap) && source.GetTexture(s_emissionMap) != null)
                    material.SetTexture(s_emissionMap, source.GetTexture(s_emissionMap));
                material.SetColor(s_emissionColor, source.GetColor(s_emissionColor));
            }
            // Texture mode: the baked texture is the detail, and bodies carry no wall grime.
            material.SetFloat("_DetailStrength", 0f);
            material.SetFloat("_GroundGrime", 1f);
            material.SetFloat("_IsStone", 0f);
            material.SetFloat("_VertexSoot", 0f);
            s_converted[source] = material;
            return material;
        }
    }
}
