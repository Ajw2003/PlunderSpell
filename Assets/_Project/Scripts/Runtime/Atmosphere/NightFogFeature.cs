using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Plunderspell.Atmosphere
{
    /// <summary>
    /// Draws the castle's night fog over the camera colour after the opaques, before transparents,
    /// so flames and the portal draw on top and fog themselves (NightFogCommon.hlsl). One full-screen
    /// triangle, blended scene * transmittance + light; no colour copy. Just after it, the shader's
    /// Ink pass multiplies in outlines and paper grain (#231); drawn before, the fog's glow washed it out.
    ///
    /// The fog's colour, density and the fires it scatters are shader globals owned by
    /// <see cref="CastleAtmosphere"/>. With no atmosphere in the scene the density global is zero and
    /// the pass is skipped, so menus and benches without one render untouched.
    /// See docs/4-systems/atmosphere.md ("Fog").
    /// </summary>
    public class NightFogFeature : ScriptableRendererFeature
    {
        [Tooltip("Hidden/Plunderspell/NightFog. Referenced here so builds include it.")]
        [SerializeField] private Shader _shader;

        private Material _material;
        private NightFogPass _inkPass;
        private NightFogPass _pass;

        /// <summary>Set by <see cref="CastleAtmosphere"/>; the pass does nothing while it is false.</summary>
        public static bool IsActive { get; set; }

        public override void Create()
        {
            if (_shader == null)
                _shader = Shader.Find("Hidden/Plunderspell/NightFog");
            _inkPass = new NightFogPass("Ink", 1) { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
            _pass = new NightFogPass("Night Fog", 0) { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!IsActive || _shader == null)
                return;

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                return;

            if (_material == null)
                _material = CoreUtils.CreateEngineMaterial(_shader);

            _pass.Material = _material;
            _pass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(_pass);
            _inkPass.Material = _material;
            _inkPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            renderer.EnqueuePass(_inkPass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }

        private class NightFogPass : ScriptableRenderPass
        {
            public Material Material;

            private readonly string _name;
            private readonly int _shaderPass;

            private class PassData
            {
                public Material Material;
                public int ShaderPass;
            }

            public NightFogPass(string name, int shaderPass)
            {
                _name = name;
                _shaderPass = shaderPass;
                profilingSampler = new ProfilingSampler(name);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (!resources.cameraDepthTexture.IsValid())
                    return;

                using (IRasterRenderGraphBuilder builder =
                       renderGraph.AddRasterRenderPass(_name, out PassData data, profilingSampler))
                {
                    data.Material = Material;
                    data.ShaderPass = _shaderPass;
                    builder.UseTexture(resources.cameraDepthTexture);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc((PassData passData, RasterGraphContext context) =>
                        context.cmd.DrawProcedural(Matrix4x4.identity, passData.Material, passData.ShaderPass,
                            MeshTopology.Triangles, 3, 1));
                }
            }
        }
    }
}
