using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Switches the <c>_EMISSION</c> keyword on for every material imported with a loot model, so the
    /// focus glow from <c>LootHighlight</c> is actually drawn.
    ///
    /// <c>LootHighlight</c> pushes an <c>_EmissionColor</c> through a <c>MaterialPropertyBlock</c>, but
    /// a URP Lit material ignores emission while its keyword is off and a property block cannot turn a
    /// keyword on. Every loot material shipped with it off, so the glow changed no pixels at all
    /// (measured: 0.00% of the frame). Issue <see href="https://github.com/Ajw2003/PlunderSpell/issues/15"/>.
    ///
    /// The materials are embedded in the FBXs, so they cannot be edited in place — a reimport rewrites
    /// them. A postprocessor reapplies the keyword on every import, and covers loot added later. The
    /// keyword is enabled on the material itself, rather than at runtime, so a player build keeps the
    /// emission shader variant instead of stripping it.
    /// </summary>
    public class LootMaterialImportSettings : AssetPostprocessor
    {
        public const string LootModelRoot = "Assets/_Project/Art/Models/Loot/";

        // Bump when this postprocessor's behaviour changes, so Unity reimports the models it touches.
        public override uint GetVersion() => 3;

        // After URP's own material-description handling, which would otherwise set the keyword back
        // from the (black) emission colour.
        public override int GetPostprocessOrder() => 1000;

        // OnPostprocessMaterial is never called for a material embedded in a model, and edits made to
        // the model's renderers in OnPostprocessModel are in memory only and are lost on the next
        // asset unload. This is the hook that hands over the material actually being written.
        private void OnPreprocessMaterialDescription(MaterialDescription description, Material material,
            AnimationClip[] animations)
        {
            if (!assetPath.StartsWith(LootModelRoot, System.StringComparison.OrdinalIgnoreCase))
                return;

            material.EnableKeyword("_EMISSION");

            // Tiny rather than black: URP derives the keyword from the colour, and black turns it
            // straight back off. Invisible at this level, and LootHighlight's block overrides it.
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", new Color(0.001f, 0.001f, 0.001f, 1f));

            // URP derives the keyword from these flags as well as the colour, and drops it whenever
            // they say the emission is black. Realtime, not baked: the glow is a runtime cue.
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }
}
