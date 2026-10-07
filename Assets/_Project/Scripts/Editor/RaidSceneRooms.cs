using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Places the Lair room and the Market yard into RaidScene, replacing any earlier copy. RaidScene is
    /// authored, not built, so this is how the rooms get there. Both stand far east of the origin: the
    /// castle's curtain wall reaches 3 cells of 12 m (about 45 m) round it.
    /// </summary>
    public static class RaidSceneRooms
    {
        private const string ScenePath = "Assets/_Project/Scenes/RaidScene.unity";
        public static readonly Vector3 LairPosition = new Vector3(1000f, 0f, 0f);
        public static readonly Vector3 MarketPosition = new Vector3(1100f, 0f, 0f);

        [MenuItem("Tools/Plunderspell/Place Lair And Market In Raid Scene")]
        public static void PlaceRooms()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            Place(scene, LairRoomForge.PrefabPath, "LairRoom", LairPosition);
            Place(scene, MarketYardForge.PrefabPath, "MarketYard", MarketPosition);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Place(UnityEngine.SceneManagement.Scene scene, string prefabPath, string name, Vector3 position)
        {
            foreach (GameObject existing in scene.GetRootGameObjects())
                if (existing.name == name)
                    Object.DestroyImmediate(existing);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[RaidSceneRooms] No prefab at {prefabPath}; build it first. {name} is not in {ScenePath}.");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = position;
            Debug.Log($"[RaidSceneRooms] Placed {name} at {position} in {ScenePath}.");
        }
    }
}
