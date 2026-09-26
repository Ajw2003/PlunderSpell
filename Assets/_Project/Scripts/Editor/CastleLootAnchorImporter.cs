using System.Collections.Generic;
using System.IO;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Copies the loot anchors the asset pipeline writes (Assets/_Project/Data/Castle/CastleLootAnchors.json,
    /// Blender coordinates) into each <see cref="CastleRoomModuleData.LootAnchors"/>, in the space the
    /// generator places modules in. Re-run it after rebuilding the castle meshes.
    ///
    /// The Blender-to-Unity axis change is not assumed: the importer places each room prefab the way
    /// the generator does, tries the four plausible mappings, and keeps the one whose anchors actually
    /// land on a surface. It reports that hit rate, so a pipeline change that moves the axes shows up
    /// as a low number instead of loot floating in the air.
    /// </summary>
    public static class CastleLootAnchorImporter
    {
        private const string JsonPath = "Assets/_Project/Data/Castle/CastleLootAnchors.json";
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";

        [System.Serializable]
        private class AnchorFile
        {
            public string space;
        }

        [MenuItem("Tools/Plunderspell/Import Castle Loot Anchors")]
        public static void ImportMenu() => Debug.Log(Import());

        /// <summary>Imports the anchors and returns a one-line report.</summary>
        public static string Import()
        {
            if (!File.Exists(JsonPath))
                return $"[LootAnchors] {JsonPath} not found; run the asset pipeline first.";
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
            if (registry == null)
                return $"[LootAnchors] No registry at {RegistryPath}.";

            Dictionary<string, List<Vector3>> rooms = ParseRooms(File.ReadAllText(JsonPath));

            System.Func<Vector3, Vector3>[] mappings =
            {
                b => new Vector3(b.x, b.z, -b.y),
                b => new Vector3(b.x, b.z, b.y),
                b => new Vector3(-b.x, b.z, b.y),
                b => new Vector3(-b.x, b.z, -b.y),
            };
            string[] names = { "(x, z, -y)", "(x, z, y)", "(-x, z, y)", "(-x, z, -y)" };

            int bestIndex = 0, bestHits = -1, total = 0;
            for (int m = 0; m < mappings.Length; m++)
            {
                int hits = CountHits(registry, rooms, mappings[m], out total);
                if (hits > bestHits)
                {
                    bestHits = hits;
                    bestIndex = m;
                }
            }

            int rooted = 0;
            foreach (CastleRoomModuleData entry in registry.Modules)
            {
                if (entry == null)
                    continue;
                if (rooms.TryGetValue(entry.RoomId, out List<Vector3> blender))
                {
                    entry.LootAnchors = blender.ConvertAll(b => mappings[bestIndex](b)).ToArray();
                    rooted++;
                }
                else
                {
                    entry.LootAnchors = new Vector3[0];
                }
            }

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            return $"[LootAnchors] {rooted} rooms, {total} anchors; axis mapping {names[bestIndex]} " +
                   $"puts {bestHits}/{total} on a surface.";
        }

        /// <summary>How many anchors sit on a surface (within 0.15 m below) when each prefab is placed
        /// as the generator places it with no extra rotation.</summary>
        private static int CountHits(CastleRoomRegistry registry, Dictionary<string, List<Vector3>> rooms,
            System.Func<Vector3, Vector3> map, out int total)
        {
            int hits = 0;
            total = 0;
            var origin = new Vector3(0f, 0f, 5000f); // well away from any open scene
            foreach (CastleRoomModuleData entry in registry.Modules)
            {
                if (entry?.Prefab == null || !rooms.TryGetValue(entry.RoomId, out List<Vector3> anchors))
                    continue;
                GameObject probe = Object.Instantiate(entry.Prefab, origin, entry.Prefab.transform.rotation);
                try
                {
                    Physics.SyncTransforms();
                    foreach (Vector3 b in anchors)
                    {
                        total++;
                        Vector3 p = origin + map(b);
                        if (Physics.Raycast(p + Vector3.up * 0.4f, Vector3.down, out RaycastHit hit, 0.6f)
                            && hit.transform.IsChildOf(probe.transform)
                            && Mathf.Abs(hit.point.y - p.y) < 0.15f)
                            hits++;
                    }
                }
                finally
                {
                    Object.DestroyImmediate(probe);
                }
            }
            return hits;
        }

        /// <summary>Reads {"rooms": {"Key": [[x,y,z], ...]}} without a JSON library.</summary>
        private static Dictionary<string, List<Vector3>> ParseRooms(string json)
        {
            var rooms = new Dictionary<string, List<Vector3>>();
            int at = json.IndexOf("\"rooms\"", System.StringComparison.Ordinal);
            if (at < 0)
                return rooms;
            int i = json.IndexOf('{', at) + 1;
            while (i > 0 && i < json.Length)
            {
                int keyStart = json.IndexOf('"', i);
                if (keyStart < 0)
                    break;
                int keyEnd = json.IndexOf('"', keyStart + 1);
                string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1);
                int listStart = json.IndexOf('[', keyEnd);
                int depth = 0, j = listStart;
                for (; j < json.Length; j++)
                {
                    if (json[j] == '[') depth++;
                    else if (json[j] == ']' && --depth == 0) break;
                }
                string body = json.Substring(listStart + 1, j - listStart - 1);
                var points = new List<Vector3>();
                foreach (string triple in body.Split(']'))
                {
                    string cleaned = triple.Replace("[", "").Replace(",\n", ",").Trim().Trim(',').Trim();
                    if (cleaned.Length == 0)
                        continue;
                    string[] parts = cleaned.Split(',');
                    if (parts.Length != 3)
                        continue;
                    points.Add(new Vector3(
                        float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture)));
                }
                rooms[key] = points;
                i = j + 1;
                int next = json.IndexOf('"', i);
                int close = json.IndexOf('}', i);
                if (next < 0 || (close >= 0 && close < next))
                    break;
            }
            return rooms;
        }
    }
}
