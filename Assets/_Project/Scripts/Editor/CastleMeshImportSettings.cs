using UnityEditor;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Forces Read/Write and baked axis conversion on the castle room models.
    ///
    /// The castle is instantiated from a seed at runtime. The runtime NavMesh bake that needed readable
    /// room meshes is gone (#223: guards walk the nav graph, baked from the prefabs in the Editor), but
    /// the settings stay as they were until someone confirms nothing else reads these meshes at runtime.
    ///
    /// A postprocessor rather than a one-off pass, so re-exporting a .blend cannot quietly undo it.
    /// See docs/4-systems/raid-scene-assembly.md ("Navigation").
    /// </summary>
    public class CastleMeshImportSettings : AssetPostprocessor
    {
        private const string CastleModelRoot = "Assets/_Project/Art/Models/Castle/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(CastleModelRoot, System.StringComparison.OrdinalIgnoreCase))
                return;

            var importer = (ModelImporter)assetImporter;
            importer.isReadable = true;
            // Without this a castle model imports at a 270-degree root instead of 90 and the
            // prefab's upright root then flips it — see docs/4-systems/raid-scene-assembly.md
            // ("Orientation").
            importer.bakeAxisConversion = true;
        }
    }
}
