using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Plunderspell.Atmosphere
{
    /// <summary>
    /// Draws the castle's night fog over the camera colour after the opaques, before transparents,
    /// so flames and the portal draw on top and fog themselves (NightFogCommon.hlsl). One full-screen
    /// triangle, blended scene * transmittance + light; no colour copy.
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
        private NightFogPass _pass;

        /// <summary>Set by <see cref="CastleAtmosphere"/>; the pass does nothing while it is false.</summary>
        public static bool IsActive { get; set; }

        public override void Create()
        {
            if (_shader == null)
                _shader = Shader.Find("Hidden/Plunderspell/NightFog");
            _pass = new NightFogPass { renderPassEvent = RenderPassEvent.BeforeRenderingTransparents };
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
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }

        private class NightFogPass : ScriptableRenderPass
        {
            public Material Material;

            private class PassData
            {
                public Material Material;
            }

            public NightFogPass()
            {
                profilingSampler = new ProfilingSampler("Night Fog");
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (!resources.cameraDepthTexture.IsValid())
                    return;

                using (IRasterRenderGraphBuilder builder =
                       renderGraph.AddRasterRenderPass("Night Fog", out PassData data, profilingSampler))
                {
                    data.Material = Material;
                    builder.UseTexture(resources.cameraDepthTexture);
                    builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc((PassData passData, RasterGraphContext context) =>
                        context.cmd.DrawProcedural(Matrix4x4.identity, passData.Material, 0,
                            MeshTopology.Triangles, 3, 1));
                }
            }
        }
    }
}
