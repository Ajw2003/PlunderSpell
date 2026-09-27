using UnityEditor;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Forces Read/Write and baked axis conversion on the castle room models.
    ///
    /// The castle is instantiated from a seed at runtime, so its NavMesh is built at runtime too, and
    /// <c>NavMeshSurface</c> has to read the room meshes to do it. An unreadable mesh still bakes in
    /// the Editor but silently produces no surface in a player build — the garrison would stand still
    /// in a build and walk fine in the Editor.
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
