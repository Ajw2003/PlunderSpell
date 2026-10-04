using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Makes prefabs of the High Medieval stair models (MedievalStairUp/Down.fbx, built in
    /// Tools/AssetPipeline/castle_builders_stairs.py) and points the High Medieval registry's StairUp and
    /// StairDown entries at them (#256). Same module contract as <see cref="CastleStairPlaceholderForge"/>;
    /// the Bronze and Late registries keep the placeholders until their own stairs are modelled.
    /// </summary>
    public static class CastleMedievalStairForge
    {
        private const string ModelDir = "Assets/_Project/Art/Models/Castle";
        private const string PrefabDir = "Assets/_Project/Prefabs/Castle";
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";

        [MenuItem("Tools/Plunderspell/Forge Medieval Stairs")]
        public static void Forge()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
            Register(registry, ProceduralCastleGenerator.StairUpId, Build("MedievalStairUp", ProceduralCastleGenerator.StairUpId), 4.6f);
            Register(registry, ProceduralCastleGenerator.StairDownId, Build("MedievalStairDown", ProceduralCastleGenerator.StairDownId), 3.6f);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Debug.Log("[StairForge] Built MedievalStairUp and MedievalStairDown prefabs and registered them in the High Medieval registry. Bake the nav tiles next.");
        }

        // Stood up like every other castle room (the FBX is Z-up), a mesh collider, and the module component.
        private static GameObject Build(string key, string roomId)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{key}.fbx");
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
                return PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabDir}/{key}.prefab");
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
