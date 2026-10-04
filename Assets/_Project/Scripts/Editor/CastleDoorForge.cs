using System.IO;
using PurrNet;
using Plunderspell.Castle;
using Plunderspell.Raid;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the three door prefabs (#248) from primitives: one per archway height (InnerWard, Keep, Crypt), each a
    /// networked <see cref="CastleDoor"/> root with a hinge at one jamb carrying a cube oak leaf. They live under
    /// Prefabs/, where PurrNet's auto-generated network prefab list picks them up.
    /// </summary>
    public static class CastleDoorForge
    {
        private const string PrefabDir = "Assets/_Project/Prefabs/Castle";
        // Borrowed from the existing stairwell so the doors sit in the castle's look (see CastleStairPlaceholderForge).
        private const string StyleModel = "Assets/_Project/Art/Models/Castle/KeepStairwell.fbx";
        private const float OpeningWidth = 2.6f, LeafThickness = 0.12f;

        private static readonly (string Name, CastleZone Zone)[] Doors =
        {
            ("CastleDoor_InnerWard", CastleZone.InnerWard),
            ("CastleDoor_Keep", CastleZone.Keep),
            ("CastleDoor_Crypt", CastleZone.Crypt),
        };

        [MenuItem("Tools/Plunderspell/Forge Castle Doors")]
        public static void Forge()
        {
            Material oak = StyleMaterial("KeepStairwell_oak");
            Directory.CreateDirectory(PrefabDir);
            foreach ((string name, CastleZone zone) in Doors)
                Build(name, ArtBibleEnemyCatalog.ArchwayHeight(zone), oak);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[DoorForge] Built {Doors.Length} door prefabs in {PrefabDir}.");
        }

        /// <summary>Adds a door spawner holding the three forged prefabs to a scene's director. Used by the scene builder.</summary>
        public static void WireSpawner(RaidDirector director)
        {
            var go = new GameObject("CastleDoorSpawner");
            var spawner = go.AddComponent<CastleDoorSpawner>();
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("_innerWardDoor").objectReferenceValue = LoadDoor("CastleDoor_InnerWard");
            serialized.FindProperty("_keepDoor").objectReferenceValue = LoadDoor("CastleDoor_Keep");
            serialized.FindProperty("_cryptDoor").objectReferenceValue = LoadDoor("CastleDoor_Crypt");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            director.DoorSpawner = spawner;
            EditorUtility.SetDirty(director);
        }

        private static GameObject LoadDoor(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");

        private static void Build(string name, float height, Material oak)
        {
            var root = new GameObject(name);
            root.AddComponent<NetworkIdentity>();
            var door = root.AddComponent<CastleDoor>();

            // The hinge sits on one jamb; the leaf hangs off it, so rotating the hinge swings the door.
            var hinge = new GameObject("Hinge");
            hinge.transform.SetParent(root.transform, false);
            hinge.transform.localPosition = new Vector3(-OpeningWidth / 2f, 0f, 0f);

            GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Leaf";
            leaf.transform.SetParent(hinge.transform, false);
            leaf.transform.localPosition = new Vector3(OpeningWidth / 2f, height / 2f, 0f);
            leaf.transform.localScale = new Vector3(OpeningWidth, height, LeafThickness);
            leaf.GetComponent<Renderer>().sharedMaterial = oak;

            var serialized = new SerializedObject(door);
            serialized.FindProperty("_hinge").objectReferenceValue = hinge.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        private static Material StyleMaterial(string name)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(StyleModel))
            {
                if (asset is Material material && material.name == name)
                    return material;
            }
            Debug.LogWarning($"[DoorForge] {name} not found in {StyleModel}; the doors keep the default material.");
            return null;
        }
    }
}
