using System.Collections.Generic;
using System.IO;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Authors one door-plug prefab per enclosed zone from the FBX the asset pipeline exports, and
    /// registers them on the <see cref="CastleRoomRegistry"/> so the generator can find them.
    ///
    /// See docs/systems/scale.md ("Archways") for why there is one plug per zone rather than one
    /// for the whole castle.
    /// </summary>
    public static class CastleDoorPlugForge
    {
        private const string ModelDirectory = "Assets/_Project/Art/Models/Castle";
        private const string PrefabDirectory = "Assets/_Project/Prefabs/Castle";
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";

        /// <summary>Root rotation at which a castle mesh stands floor-down (see <see cref="CastlePrefabOrientationFix"/>).</summary>
        private static readonly Vector3 UprightEuler = new Vector3(90f, 0f, 0f);

        private static readonly (string Key, CastleZone Zone)[] Plugs =
        {
            ("DoorPlugOuterBailey", CastleZone.OuterBailey),
            ("DoorPlugInnerWard", CastleZone.InnerWard),
            ("DoorPlugKeep", CastleZone.Keep),
            ("DoorPlugCrypt", CastleZone.Crypt)
        };

        [MenuItem("Tools/Plunderspell/Forge Castle Door Plugs")]
        public static void Forge()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
            if (registry == null)
            {
                Debug.LogError($"Plunderspell: no castle room registry at {RegistryPath}.");
                return;
            }

            var missing = new List<string>();
            registry.DoorPlugs.Clear();

            foreach ((string key, CastleZone zone) in Plugs)
            {
                string modelPath = $"{ModelDirectory}/{key}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    missing.Add(modelPath);
                    continue;
                }

                registry.DoorPlugs.Add(new CastleRoomModuleData
                {
                    RoomId = key,
                    Zone = zone,
                    Prefab = BuildPrefab(model, key),
                    Weight = 1
                });
            }

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (missing.Count > 0)
            {
                Debug.LogError("Plunderspell: door plug model(s) missing: " +
                               string.Join(", ", missing) +
                               " — run the Blender asset pipeline first.");
            }

            Debug.Log($"Plunderspell: forged {registry.DoorPlugs.Count} door plug prefab(s) into " +
                      $"{PrefabDirectory} and registered them on {RegistryPath}.");
        }

        /// <summary>
        /// Saves a prefab variant of the plug model with the collider that makes it a wall. A
        /// variant, not a copy, so a re-export of the FBX flows straight through.
        /// </summary>
        private static GameObject BuildPrefab(GameObject model, string key)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = key;
            instance.transform.localEulerAngles = UprightEuler;

            MeshFilter filter = instance.GetComponentInChildren<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
            {
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }

            EnsureFolder(PrefabDirectory);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabDirectory}/{key}.prefab");
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static void EnsureFolder(string path)
        {
            if (Directory.Exists(path))
                return;
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}
