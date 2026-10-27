using System.IO;
using Plunderspell.Loot;
using Plunderspell.Raid;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the coin pouch a sale puts on the counter: the MarketPouch model with a body, a box collider fitted to it,
    /// a LootPickup and Item (so it is carried like loot), a CoinPouch and a network transform. No LootValue: it is
    /// not loot. Its LootItem only carries the carry numbers (the body takes its real weight from the coins).
    /// </summary>
    public static class CoinPouchForge
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Market/CoinPouch.prefab";
        private const string ItemPath = "Assets/_Project/Data/Market/CoinPouchItem.asset";
        private const string ModelPath = "Assets/_Project/Art/Models/Market/MarketPouch.fbx";

        [MenuItem("Tools/Plunderspell/Build Coin Pouch")]
        public static void BuildCoinPouch()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError($"[Market] No model at {ModelPath}; the coin pouch is not built.");
                return;
            }

            LootItem item = AssetDatabase.LoadAssetAtPath<LootItem>(ItemPath);
            if (item == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ItemPath));
                item = ScriptableObject.CreateInstance<LootItem>();
                AssetDatabase.CreateAsset(item, ItemPath);
            }
            item.Worth = 0f;
            item.Category = Plunderspell.Market.LootCategory.Other;
            item.WeightKg = CoinPouch.MinKilos;
            item.Fragility = 999f; // coins do not shatter
            item.DisplayName = "Coin pouch";
            EditorUtility.SetDirty(item);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = "CoinPouch";
                var box = instance.AddComponent<BoxCollider>();
                Bounds local = LocalBounds(instance);
                box.center = local.center;
                box.size = local.size;
                instance.AddComponent<Rigidbody>().mass = CoinPouch.MinKilos;
                instance.AddComponent<Item>();
                var pickup = instance.AddComponent<LootPickup>();
                pickup.SetData(item);
                var pickupObject = new SerializedObject(pickup);
                pickupObject.FindProperty("_meshRenderer").objectReferenceValue = instance.GetComponentInChildren<MeshRenderer>();
                pickupObject.ApplyModifiedPropertiesWithoutUndo();
                instance.AddComponent<CoinPouch>();
                instance.AddComponent<PurrNet.NetworkTransform>();

                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Market] Built {PrefabPath} (item {ItemPath}).");
        }

        // The meshes' bounds in the root's own axes (the import's Z-up correction sits on the root).
        private static Bounds LocalBounds(GameObject root)
        {
            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
            var bounds = new Bounds();
            bool any = false;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                Bounds m = filter.sharedMesh.bounds;
                Matrix4x4 toLocal = toRoot * filter.transform.localToWorldMatrix;
                for (int c = 0; c < 8; c++)
                {
                    Vector3 corner = m.center + Vector3.Scale(m.extents,
                        new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1));
                    Vector3 p = toLocal.MultiplyPoint3x4(corner);
                    if (!any)
                        bounds = new Bounds(p, Vector3.zero);
                    else
                        bounds.Encapsulate(p);
                    any = true;
                }
            }
            return bounds;
        }
    }
}
