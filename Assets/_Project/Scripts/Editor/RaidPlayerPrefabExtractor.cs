using StateMachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Lifts the raid's player out of the authored scene into an authored prefab, and leaves the
    /// scene holding an instance of it. Run once; after that the prefab is the thing to edit.
    ///
    /// See docs/systems/raid-scene-assembly.md, "Authored, not generated".
    /// </summary>
    public static class RaidPlayerPrefabExtractor
    {
        private const string k_ScenePath = "Assets/_Project/Scenes/RaidScene.unity";
        private const string k_PrefabPath = "Assets/_Project/Prefabs/RaidPlayer.prefab";

        [MenuItem("Tools/Plunderspell/Extract Raid Player Prefab")]
        public static void ExtractRaidPlayerPrefab()
        {
            EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Single);

            var stateMachine = Object.FindFirstObjectByType<PlayerStateMachine>();
            if (stateMachine == null)
            {
                Debug.LogError($"[RaidPlayer] {k_ScenePath} has no PlayerStateMachine to extract.");
                return;
            }

            GameObject player = stateMachine.gameObject;
            if (PrefabUtility.IsPartOfPrefabInstance(player))
            {
                Debug.Log($"[RaidPlayer] {player.name} is already a prefab instance; nothing to do.");
                return;
            }

            // Connect, rather than plain SaveAsPrefabAsset: the scene keeps the same object, now as
            // an instance, so every reference already pointing at it survives the extraction.
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(player, k_PrefabPath,
                InteractionMode.AutomatedAction);

            if (prefab == null)
            {
                Debug.LogError($"[RaidPlayer] Failed to write {k_PrefabPath}.");
                return;
            }

            // Marking dirty is not optional in batchmode: SaveOpenScenes is a no-op on a scene Unity
            // does not think has changed, and it reports that by returning false, not by throwing —
            // which is how the prefab got written while the scene kept its unconnected object.
            Scene scene = player.scene;
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, k_ScenePath))
            {
                Debug.LogError($"[RaidPlayer] Wrote {k_PrefabPath} but could not save {k_ScenePath}. " +
                               "The scene still holds an unconnected object.");
                return;
            }

            Debug.Log($"[RaidPlayer] Extracted {player.name} to {k_PrefabPath}; " +
                      $"{k_ScenePath} now holds an instance.");
        }
    }
}
