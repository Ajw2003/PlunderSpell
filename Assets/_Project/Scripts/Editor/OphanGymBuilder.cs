using Plunderspell.Playtest;
using Plunderspell.Raid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Writes the Ophan Gym (#452): a fresh copy of the main level with the gym's three components added beside the
    /// Ophan, so the gym never tests an old castle. Run it again whenever RaidScene.unity changes; the co-op launcher's
    /// --gym option runs it before every gym build. See docs/4-systems/fear.md, "The Ophan Gym".
    /// </summary>
    public static class OphanGymBuilder
    {
        public const string RaidScenePath = "Assets/_Project/Scenes/RaidScene.unity";
        public const string GymScenePath = "Assets/_Project/Scenes/OphanGym.unity";

        [MenuItem("Tools/Plunderspell/Build Ophan Gym")]
        public static void BuildFromMenu() => Debug.Log(Build());

        /// <summary>Rebuilds OphanGym.unity and opens it. Returns what it did, or why it did not.</summary>
        public static string Build()
        {
            if (EditorApplication.isPlaying)
                return "[OphanGym] not built: stop Play mode first.";
            if (EditorSceneManager.GetActiveScene().isDirty)
                return "[OphanGym] not built: the open scene has unsaved changes. Save or discard them first.";

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GymScenePath) != null && !AssetDatabase.DeleteAsset(GymScenePath))
                return "[OphanGym] not built: could not replace the old " + GymScenePath;
            if (!AssetDatabase.CopyAsset(RaidScenePath, GymScenePath))
                return "[OphanGym] not built: could not copy " + RaidScenePath;

            var scene = EditorSceneManager.OpenScene(GymScenePath, OpenSceneMode.Single);
            // The Ophan is in the Bootstrap scene since the split (#542): the gym is its own root in this raid scene,
            // loaded by SceneFlow in place of RaidScene when the launcher asks for the gym.
            var gym = new GameObject("OphanGym");
            gym.AddComponent<OphanGym>();
            gym.AddComponent<OphanGymKeys>();
            gym.AddComponent<OphanGymPanel>();
            EditorSceneManager.SaveScene(scene);
            var build = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!build.Exists(s => s.path == GymScenePath))
            {
                build.Add(new EditorBuildSettingsScene(GymScenePath, true));
                EditorBuildSettings.scenes = build.ToArray();
            }
            return "[OphanGym] built " + GymScenePath + " from " + RaidScenePath + ", gym on its own root.";
        }
    }
}
