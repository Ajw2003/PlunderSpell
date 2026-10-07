using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the Market yard prefab from <c>Tools/AssetPipeline/placements/market.json</c>, which
    /// <c>render_market_scene.py --placements</c> writes from the same code that places the reference render:
    /// the yard, four stalls, counters, slates, scales, coins and lanterns, plus a warm light per lantern,
    /// colliders and one spawn point per player inside the south way in. Rebuilt from scratch every run.
    /// </summary>
    public static class MarketYardForge
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Market/MarketYard.prefab";
        public const string LairExitName = "LairExit";
        private const string PlacementsPath = "Tools/AssetPipeline/placements/market.json";
        private const string ModelDirectory = "Assets/_Project/Art/Models/Market";
        private const float Floor = 0.30f;

        /// <summary>Small props that a player should be able to walk through or knock, not stand on.</summary>
        private static readonly string[] NoCollider = { "MarketCoin", "MarketCoinStack", "MarketPouch", "MarketLantern" };

        private static readonly Color Warm = new Color(1.0f, 0.62f, 0.28f);

        [Serializable]
        private class Placements
        {
            public Model[] models;
            public LanternLight[] lights;
        }

        [Serializable]
        private class Model
        {
            public string key;
            public float[] position;
            public float zDegrees;
        }

        [Serializable]
        private class LanternLight
        {
            public float[] position;
            public float watts;
        }

        [MenuItem("Tools/Plunderspell/Build Market Yard")]
        public static void BuildMarketYard()
        {
            if (!File.Exists(PlacementsPath))
            {
                Debug.LogError($"[Market] No {PlacementsPath}. Write it with: blender -b -P " +
                               "Tools/AssetPipeline/render_market_scene.py -- --placements " + PlacementsPath);
                return;
            }

            var placements = JsonUtility.FromJson<Placements>(File.ReadAllText(PlacementsPath));
            var root = new GameObject("MarketYard");

            foreach (Model model in placements.models)
                BlenderPlacement.PlaceModel(root.transform, $"{ModelDirectory}/{model.key}.fbx", ToVector(model.position),
                    new Vector3(0f, 0f, model.zDegrees), withCollider: Array.IndexOf(NoCollider, model.key) < 0);

            // The render's lanterns are 70-160 W Cycles lights; Unity's intensity is set by eye, scaled the same way.
            for (int i = 0; i < placements.lights.Length; i++)
                BlenderPlacement.AddPointLight(root.transform, $"LanternLight{i + 1}", ToVector(placements.lights[i].position),
                    Warm, placements.lights[i].watts / 80f, 7f);

            // Four players stand inside the 4 m way in on the south wall, facing the well (Blender +Y).
            var spawns = new GameObject("PlayerSpawns");
            spawns.transform.SetParent(root.transform, false);
            for (int i = 0; i < 4; i++)
            {
                var spawn = new GameObject($"Spawn{i + 1}");
                spawn.transform.SetParent(spawns.transform, false);
                spawn.transform.localPosition = BlenderPlacement.ToUnity(new Vector3(-1.5f + i * 1.0f, -8.0f, Floor));
                spawn.transform.localRotation = Quaternion.LookRotation(BlenderPlacement.ToUnity(Vector3.up));
            }

            // The way back to the Lair: walking out through the 4 m way in (Blender y -10) takes you home.
            // It spans y -10.1..-9.5, clear of the spawns at y -8. RaidSceneRooms points it at the Lair door.
            var exit = new GameObject(LairExitName);
            exit.transform.SetParent(root.transform, false);
            exit.transform.localPosition = BlenderPlacement.ToUnity(new Vector3(0f, -9.8f, Floor + 1.2f));
            var box = exit.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(4.0f, 2.4f, 0.6f);
            var travel = exit.AddComponent<Plunderspell.Raid.RoomTravel>();
            var travelObject = new SerializedObject(travel);
            travelObject.FindProperty("_trigger").enumValueIndex = (int)Plunderspell.Raid.RoomTravel.Trigger.WalkThrough;
            travelObject.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[Market] Built {PrefabPath}: {placements.models.Length} models, {placements.lights.Length} lights.");
        }

        private static Vector3 ToVector(float[] xyz) => new Vector3(xyz[0], xyz[1], xyz[2]);
    }
}
