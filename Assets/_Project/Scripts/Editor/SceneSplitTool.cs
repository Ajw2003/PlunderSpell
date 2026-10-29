using System.Collections.Generic;
using Plunderspell.Alarm;
using Plunderspell.Castle;
using Plunderspell.Extraction;
using Plunderspell.Raid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// One-time migration for #538 (docs/4-systems/scene-flow.md): moves RaidScene's shared roots into a new
    /// Bootstrap scene and the Lair and Market rooms into a new Lair scene, leaves the raid in RaidScene with a
    /// <see cref="RaidSceneContents"/> pointing at its pieces, and puts Bootstrap, Lair and RaidScene first in the build.
    /// Safe to run again only on an unsplit RaidScene: a root already moved is reported missing and skipped.
    /// </summary>
    public static class SceneSplitTool
    {
        private const string RaidPath = "Assets/_Project/Scenes/RaidScene.unity";
        private const string BootstrapPath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string LairPath = "Assets/_Project/Scenes/Lair.unity";

        private static readonly string[] BootstrapRoots = { "Network", "Lair", "RaidDirector", "ItemManager", "SpellVfx", "RaidHud", "PlayerSpawnPoints" };
        // The rooms keep their place, 1000 m and 1100 m out, so they never overlap the raid scene loaded beside them.
        private static readonly string[] LairRoots = { "LairRoom", "MarketYard" };

        [MenuItem("Tools/Plunderspell/Scenes/Split Raid Scene Into Bootstrap, Lair and Raid")]
        public static void Split()
        {
            Scene raid = EditorSceneManager.OpenScene(RaidPath, OpenSceneMode.Single);
            Scene bootstrap = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            Scene lair = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            Move(raid, bootstrap, BootstrapRoots);
            Move(raid, lair, LairRoots);
            AddContents(raid);
            if (Find(bootstrap, "SceneFlow") == null)
            {
                var flow = new GameObject("SceneFlow");
                SceneManager.MoveGameObjectToScene(flow, bootstrap);
                flow.AddComponent<SceneFlow>();
            }

            EditorSceneManager.SaveScene(bootstrap, BootstrapPath);
            EditorSceneManager.SaveScene(lair, LairPath);
            EditorSceneManager.SaveScene(raid);
            SetBuildOrder();
            Debug.Log($"[SceneSplit] Wrote {BootstrapPath} and {LairPath}; {RaidPath} keeps the raid.");
        }

        private static void Move(Scene from, Scene to, string[] names)
        {
            foreach (string name in names)
            {
                GameObject root = Find(from, name);
                if (root == null)
                {
                    Debug.LogError($"[SceneSplit] No root '{name}' in {from.path}; not moved.");
                    continue;
                }
                SceneManager.MoveGameObjectToScene(root, to);
            }
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name)
                    return root;
            return null;
        }

        private static void AddContents(Scene raid)
        {
            GameObject holder = Find(raid, "RaidSceneContents") ?? new GameObject("RaidSceneContents");
            SceneManager.MoveGameObjectToScene(holder, raid);
            if (!holder.TryGetComponent(out RaidSceneContents contents))
                contents = holder.AddComponent<RaidSceneContents>();
            var so = new SerializedObject(contents);
            so.FindProperty("_generator").objectReferenceValue = FindIn<ProceduralCastleGenerator>(raid);
            so.FindProperty("_castleNetwork").objectReferenceValue = FindIn<CastleNetworkManager>(raid);
            so.FindProperty("_lootSpawner").objectReferenceValue = FindIn<LootSpawner>(raid);
            so.FindProperty("_guardSpawner").objectReferenceValue = FindIn<GuardSpawner>(raid);
            so.FindProperty("_doorSpawner").objectReferenceValue = FindIn<CastleDoorSpawner>(raid);
            so.FindProperty("_extractionZone").objectReferenceValue = FindIn<ExtractionZone>(raid);
            so.FindProperty("_alarm").objectReferenceValue = FindIn<EnemyDirector>(raid);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T FindIn<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            Debug.LogWarning($"[SceneSplit] No {typeof(T).Name} in {scene.path}.");
            return null;
        }

        private static void SetBuildOrder()
        {
            var first = new[] { BootstrapPath, LairPath, RaidPath };
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (string path in first)
                scenes.Add(new EditorBuildSettingsScene(path, true));
            foreach (EditorBuildSettingsScene existing in EditorBuildSettings.scenes)
                if (System.Array.IndexOf(first, existing.path) < 0)
                    scenes.Add(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
