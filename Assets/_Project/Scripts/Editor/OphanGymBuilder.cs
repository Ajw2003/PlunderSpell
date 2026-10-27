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
            var ophan = Object.FindFirstObjectByType<OphanOfHours>();
            if (ophan == null)
                return "[OphanGym] not built: " + RaidScenePath + " has no OphanOfHours to put the gym beside.";
            GameObject network = ophan.gameObject;
            network.AddComponent<OphanGym>();
            network.AddComponent<OphanGymKeys>();
            network.AddComponent<OphanGymPanel>();
            EditorSceneManager.SaveScene(scene);
            return "[OphanGym] built " + GymScenePath + " from " + RaidScenePath + ", gym on " + network.name + ".";
        }
    }
}
