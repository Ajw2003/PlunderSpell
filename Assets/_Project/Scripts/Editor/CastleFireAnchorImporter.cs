using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using RogueAi.Castle;
using UnityEditor;
using UnityEngine;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Copies the fire anchors the asset pipeline writes (Assets/_Project/Data/Castle/CastleFireAnchors.json,
    /// Blender coordinates) into <see cref="CastleRoomModuleData.FireAnchors"/> of every era's room
    /// registry. Re-run it after rebuilding the castle meshes. The mirror of
    /// <see cref="CastleLootAnchorImporter"/>, using the axis mapping that importer measured, (x, z, y).
    /// </summary>
    public static class CastleFireAnchorImporter
    {
        private const string JsonPath = "Assets/_Project/Data/Castle/CastleFireAnchors.json";

        private static readonly string[] s_registryPaths =
        {
            "Assets/_Project/Data/Castle/CastleRoomRegistry.asset",
            "Assets/_Project/Data/Castle/CastleRoomRegistry_BronzeAge.asset",
            "Assets/_Project/Data/Castle/CastleRoomRegistry_LateMedieval.asset",
        };

        [MenuItem("Tools/Plunderspell/Import Castle Fire Anchors")]
        public static void ImportMenu() => Debug.Log(Import());

        /// <summary>Imports the anchors into every registry and returns a one-line report.</summary>
        public static string Import()
        {
            if (!File.Exists(JsonPath))
                return $"[FireAnchors] {JsonPath} not found; run the asset pipeline first.";

            Dictionary<string, CastleFireAnchor[]> rooms = Parse(File.ReadAllText(JsonPath));
            int registries = 0, modules = 0, anchors = 0;
            foreach (string path in s_registryPaths)
            {
                var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(path);
                if (registry == null)
                    continue;
                registries++;
                foreach (CastleRoomModuleData entry in registry.Modules)
                {
                    if (entry == null)
                        continue;
                    // An era room keeps the stand-in's RoomId but its own model, named by its pipeline key.
                    string key = entry.Prefab != null && rooms.ContainsKey(entry.Prefab.name) ? entry.Prefab.name : entry.RoomId;
                    entry.FireAnchors = rooms.TryGetValue(key, out CastleFireAnchor[] found) ? found : new CastleFireAnchor[0];
                    if (entry.FireAnchors.Length > 0)
                    {
                        modules++;
                        anchors += entry.FireAnchors.Length;
                    }
                }
                EditorUtility.SetDirty(registry);
            }
            AssetDatabase.SaveAssets();
            return $"[FireAnchors] {registries} registries, {modules} modules with fire, {anchors} anchors.";
        }

        /// <summary>Blender module-local (X right, Y forward, Z up) to the generator's module space.</summary>
        public static Vector3 BlenderToModule(Vector3 b) => new Vector3(b.x, b.z, b.y);

        /// <summary>Parses the pipeline's file. Public so a test can hold the format to the pipeline's.</summary>
        public static Dictionary<string, CastleFireAnchor[]> Parse(string json)
        {
            var rooms = new Dictionary<string, CastleFireAnchor[]>();
            JObject root = JObject.Parse(json);
            foreach (JProperty room in ((JObject)root["rooms"]).Properties())
            {
                var list = new List<CastleFireAnchor>();
                foreach (JToken anchor in (JArray)room.Value)
                {
                    var p = (JArray)anchor["p"];
                    var facing = (JArray)anchor["facing"];
                    Vector3 position = BlenderToModule(new Vector3((float)p[0], (float)p[1], (float)p[2]));
                    Vector3 face = BlenderToModule(new Vector3((float)facing[0], (float)facing[1], 0f));
                    float yaw = Mathf.Atan2(face.x, face.z) * Mathf.Rad2Deg;
                    if (!System.Enum.TryParse((string)anchor["kind"], out FireKind kind))
                    {
                        Debug.LogWarning($"[FireAnchors] {room.Name}: unknown fire kind {(string)anchor["kind"]}; skipped.");
                        continue;
                    }
                    list.Add(new CastleFireAnchor(position, yaw, kind, (int)anchor["lit"], (bool)anchor["holder"]));
                }
                rooms[room.Name] = list.ToArray();
            }
            return rooms;
        }
    }
}
