using System;
using System.Collections.Generic;
using System.IO;
using Plunderspell.Market;
using Plunderspell.Raid;
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

        /// <summary>The stall model of each vendor, indexed by <see cref="Vendor"/>: how a counter finds its vendor.</summary>
        private static readonly string[] StallModels =
            { "MarketFenceCart", "MarketGoldsmithStall", "MarketPardonerBooth", "MarketAntiquarianCabinet" };

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

            var counters = new List<GameObject>();
            var stalls = new Dictionary<Vendor, GameObject>();
            foreach (Model model in placements.models)
            {
                GameObject slot = BlenderPlacement.PlaceModel(root.transform, $"{ModelDirectory}/{model.key}.fbx", ToVector(model.position),
                    new Vector3(0f, 0f, model.zDegrees), withCollider: Array.IndexOf(NoCollider, model.key) < 0);
                if (model.key == "MarketCounter")
                    counters.Add(slot);
                else if (Array.IndexOf(StallModels, model.key) >= 0)
                    stalls[(Vendor)Array.IndexOf(StallModels, model.key)] = slot;
            }
            foreach (GameObject counter in counters)
                AddSellCounter(counter, stalls);

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

        /// <summary>
        /// Gives a counter its vendor (the one whose stall model stands nearest), a trigger volume on its top, a placeholder
        /// capsule figure behind it (towards the stall, no collider) and a chalk slate standing on the counter.
        /// </summary>
        private static void AddSellCounter(GameObject counter, Dictionary<Vendor, GameObject> stalls)
        {
            Vendor vendor = Vendor.Fence;
            float nearest = float.MaxValue;
            foreach (KeyValuePair<Vendor, GameObject> stall in stalls)
            {
                float distance = Vector3.Distance(counter.transform.position, stall.Value.transform.position);
                if (distance < nearest)
                {
                    nearest = distance;
                    vendor = stall.Key;
                }
            }

            // The counter's size in its own axes: measure it unturned, then turn it back.
            Quaternion turn = counter.transform.rotation;
            counter.transform.rotation = Quaternion.identity;
            Bounds bounds = counter.GetComponentInChildren<Renderer>().bounds;
            foreach (Renderer part in counter.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(part.bounds);
            counter.transform.rotation = turn;

            Vector3 offset = bounds.center - counter.transform.position;
            var top = new GameObject("SellZone");
            top.transform.SetParent(counter.transform, false);
            top.transform.localPosition = new Vector3(offset.x, bounds.max.y + 0.2f - counter.transform.position.y, offset.z);
            var zone = top.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(bounds.size.x, 0.6f, bounds.size.z); // a hand's breadth under the top to half a metre over it

            // Behind the counter is towards its stall (the Fence's cart), a metre from the counter's middle.
            Vector3 toStall = stalls[vendor].transform.position - counter.transform.position;
            toStall.y = 0f;
            Transform yard = counter.transform.parent;
            var figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            figure.name = "VendorFigure";
            UnityEngine.Object.DestroyImmediate(figure.GetComponent<Collider>());
            figure.transform.SetParent(yard, false);
            figure.transform.position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + toStall.normalized * 1.0f + Vector3.up * 0.45f;
            figure.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);

            // The chalk slate stands on the counter top at its stall edge, facing the player's side.
            Vector3 back = Quaternion.Inverse(turn) * toStall.normalized;
            float depth = Mathf.Abs(back.x) * bounds.size.x * 0.5f + Mathf.Abs(back.z) * bounds.size.z * 0.5f - 0.15f;
            // A little to the player's right: the stalls' scales stand mid-counter and would hide the words.
            Vector3 right = Vector3.Cross(Vector3.up, toStall.normalized);
            Vector3 slateAt = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + toStall.normalized * depth + right * 0.4f;
            CounterSlate slate = CounterSlateBuilder.Build(yard, slateAt, toStall.normalized);

            AddLip(counter, bounds, offset);
            BoxCollider foot = AddSellFoot(counter, bounds, turn, toStall.normalized);

            // A scene network object, so the server owns the haggle and the client's word reaches it. A sale puts a network-spawned CoinPouch prefab on it.
            counter.AddComponent<PurrNet.NetworkIdentity>();
            var sellCounter = counter.AddComponent<SellCounter>();
            sellCounter.Set(vendor, zone, foot, figure.GetComponent<Renderer>(), slate);
            var pouch = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPouchForge.PrefabPath);
            if (pouch == null)
                Debug.LogError($"[Market] No {CoinPouchForge.PrefabPath}; build the coin pouch first, or {counter.name} sells for nothing.");
            var counterObject = new SerializedObject(sellCounter);
            counterObject.FindProperty("_pouchPrefab").objectReferenceValue = pouch != null ? pouch.GetComponent<CoinPouch>() : null;
            counterObject.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The floor in front of the counter on the player's side (away from the stall), as wide as the counter, a metre
        /// and a quarter deep and a metre high: a piece too heavy to lift alone is towed here to sell (#356).
        /// </summary>
        private static BoxCollider AddSellFoot(GameObject counter, Bounds bounds, Quaternion turn, Vector3 toStall)
        {
            Vector3 back = Quaternion.Inverse(turn) * toStall;
            float halfDepth = Mathf.Abs(back.x) * bounds.size.x * 0.5f + Mathf.Abs(back.z) * bounds.size.z * 0.5f;
            float width = Mathf.Abs(back.z) * bounds.size.x + Mathf.Abs(back.x) * bounds.size.z;
            const float depth = 1.25f, height = 1f;

            var foot = new GameObject("SellFoot");
            foot.transform.SetParent(counter.transform.parent, false);
            // bounds were measured with the counter unturned: turn its centre's offset back to find it in the yard.
            Vector3 centre = counter.transform.position + turn * (bounds.center - counter.transform.position);
            foot.transform.position = new Vector3(centre.x, bounds.min.y + height * 0.5f, centre.z)
                                      - toStall * (halfDepth + depth * 0.5f);
            foot.transform.rotation = Quaternion.LookRotation(-toStall, Vector3.up);
            var zone = foot.AddComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.size = new Vector3(width, height, depth);
            return zone;
        }

        /// <summary>A 4 cm rim round the counter top, so a piece set down near the edge stays on it.</summary>
        private static void AddLip(GameObject counter, Bounds bounds, Vector3 offset)
        {
            const float rim = 0.04f;
            var lip = new GameObject("Lip");
            lip.transform.SetParent(counter.transform, false);
            lip.transform.localPosition = new Vector3(offset.x, bounds.max.y - counter.transform.position.y + rim * 0.5f, offset.z);
            Vector3 half = bounds.size * 0.5f;
            AddRim(lip, new Vector3(0f, 0f, half.z), new Vector3(bounds.size.x, rim, rim));
            AddRim(lip, new Vector3(0f, 0f, -half.z), new Vector3(bounds.size.x, rim, rim));
            AddRim(lip, new Vector3(half.x, 0f, 0f), new Vector3(rim, rim, bounds.size.z));
            AddRim(lip, new Vector3(-half.x, 0f, 0f), new Vector3(rim, rim, bounds.size.z));
        }

        private static void AddRim(GameObject lip, Vector3 centre, Vector3 size)
        {
            var box = lip.AddComponent<BoxCollider>();
            box.center = centre;
            box.size = size;
        }

        private static Vector3 ToVector(float[] xyz) => new Vector3(xyz[0], xyz[1], xyz[2]);
    }
}
