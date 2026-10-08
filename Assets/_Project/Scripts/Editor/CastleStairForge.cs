using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Makes prefabs of each Age's stair models (<c>*StairUp.fbx</c> / <c>*StairDown.fbx</c>, built in
    /// Tools/AssetPipeline/castle_builders_stairs.py) and points that Age's registry StairUp and StairDown
    /// entries at them (#256). Same module contract as <see cref="CastleStairPlaceholderForge"/>, whose
    /// placeholders stay in the assets but are no longer referenced once an Age has its own stairs.
    /// </summary>
    public static class CastleStairForge
    {
        // registry asset, model/prefab sub-folder under Castle ("" for High Medieval), pipeline key prefix.
        private static readonly (string registry, string folder, string prefix)[] Ages =
        {
            ("CastleRoomRegistry", "", "Medieval"),
            ("CastleRoomRegistry_LateMedieval", "LateMedieval/", "Late"),
            ("CastleRoomRegistry_BronzeAge", "BronzeAge/", "Bronze"),
        };

        [MenuItem("Tools/Plunderspell/Forge Stairs")]
        public static void Forge()
        {
            foreach (var (registryName, folder, prefix) in Ages)
            {
                var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>($"Assets/_Project/Data/Castle/{registryName}.asset");
                Register(registry, ProceduralCastleGenerator.StairUpId, Build(folder, prefix + "StairUp", ProceduralCastleGenerator.StairUpId), 4.6f);
                Register(registry, ProceduralCastleGenerator.StairDownId, Build(folder, prefix + "StairDown", ProceduralCastleGenerator.StairDownId), 3.6f);
                EditorUtility.SetDirty(registry);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[StairForge] Built the stair prefabs of three Ages and registered them. Import the fire anchors and bake the nav tiles next.");
        }

        // Stood up like every other castle room (the FBX is Z-up), a mesh collider, and the module component.
        private static GameObject Build(string folder, string key, string roomId)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Art/Models/Castle/{folder}{key}.fbx");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = key;
                instance.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
                MeshFilter filter = instance.GetComponentInChildren<MeshFilter>();
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                var module = instance.AddComponent<CastleRoomModule>();
                module.RoomId = roomId;
                module.Zone = CastleZone.InnerWard;
                return PrefabUtility.SaveAsPrefabAsset(instance, $"Assets/_Project/Prefabs/Castle/{folder}{key}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static void Register(CastleRoomRegistry registry, string roomId, GameObject prefab, float upperFloor)
        {
            registry.Stairs.RemoveAll(s => s != null && s.RoomId == roomId);
            registry.Stairs.Add(new CastleRoomModuleData
            {
                RoomId = roomId, Zone = CastleZone.InnerWard, Prefab = prefab, Weight = 1, UpperFloorHeight = upperFloor,
            });
        }
    }
}
