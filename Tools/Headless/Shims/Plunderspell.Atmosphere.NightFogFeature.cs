// Headless stand-in for Runtime/Atmosphere/NightFogFeature.cs, which is excluded from the headless
// build: it is pure URP render-graph plumbing (a ScriptableRenderPass) with no testable logic.
// Carries only what the rest of the game touches: the IsActive switch CastleAtmosphere flips, and
// being a renderer feature the Editor forge can add to a renderer. Keep in sync with the real one.
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Plunderspell.Atmosphere
{
    public class NightFogFeature : ScriptableRendererFeature
    {
        [SerializeField] private Shader _shader;

        public static bool IsActive { get; set; }
    }
}
