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
            GameObject lair = Place(scene, LairRoomForge.PrefabPath, "LairRoom", LairPosition);
            GameObject market = Place(scene, MarketYardForge.PrefabPath, "MarketYard", MarketPosition);
            if (lair != null && market != null)
            {
                // The Lair's Market door leads to the Market's spawns; the Market's way out to just inside that door.
                Connect(lair.transform.Find(LairRoomForge.MarketDoorName), market.transform.Find("PlayerSpawns"));
                Connect(market.transform.Find(MarketYardForge.LairExitName), lair.transform.Find(LairRoomForge.MarketDoorArrivalsName));
            }
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject Place(UnityEngine.SceneManagement.Scene scene, string prefabPath, string name, Vector3 position)
        {
            foreach (GameObject existing in scene.GetRootGameObjects())
                if (existing.name == name)
                    Object.DestroyImmediate(existing);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[RaidSceneRooms] No prefab at {prefabPath}; build it first. {name} is not in {ScenePath}.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.position = position;
            Debug.Log($"[RaidSceneRooms] Placed {name} at {position} in {ScenePath}.");
            return instance;
        }

        /// <summary>Points the RoomTravel on <paramref name="from"/> at <paramref name="to"/>, saved as a scene override.</summary>
        private static void Connect(Transform from, Transform to)
        {
            var travel = from != null ? from.GetComponent<Plunderspell.Raid.RoomTravel>() : null;
            if (travel == null || to == null)
            {
                Debug.LogError($"[RaidSceneRooms] Cannot connect {(from != null ? from.name : "a missing door")} to " +
                               $"{(to != null ? to.name : "a missing destination")}; rebuild both room prefabs.");
                return;
            }

            var serialized = new SerializedObject(travel);
            serialized.FindProperty("_destination").objectReferenceValue = to;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
