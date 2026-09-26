using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Stands the castle room prefabs the right way up.
    ///
    /// Their roots were saved at (270, 0, 0) on top of the mesh child's own (270, 0, 0), which
    /// composes to a 180-degree flip about X: the room hangs below the floor. The upright root
    /// rotation is (90, 0, 0), measured rather than reasoned — see
    /// docs/systems/raid-scene-assembly.md ("Orientation").
    ///
    /// The loot and enemy prefabs are *not* touched: their roots are already correct, and applying
    /// this to them would tip them over instead.
    /// </summary>
    public static class CastlePrefabOrientationFix
    {
        private const string PrefabDirectory = "Assets/_Project/Prefabs/Castle";

        /// <summary>The root rotation at which a castle room stands on the ground, floor down.</summary>
        private static readonly Vector3 UprightEuler = new Vector3(90f, 0f, 0f);

        [MenuItem("Tools/Plunderspell/Fix Castle Prefab Orientation")]
        public static void Fix()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDirectory });
            int changed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                    continue;

                if (root.transform.localEulerAngles == UprightEuler)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                    continue;
                }

                root.transform.localEulerAngles = UprightEuler;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                changed++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Plunderspell: set {changed} castle prefab root(s) upright to {UprightEuler}.");
        }
    }
}
