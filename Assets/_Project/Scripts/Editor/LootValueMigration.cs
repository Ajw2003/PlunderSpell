using RogueAi.Loot;
using UnityEditor;
using UnityEngine;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Gives every loot prefab a <see cref="LootValue"/>, carrying across the <c>LootItem</c> its
    /// <c>LootPickup</c> already referenced. Idempotent; safe to re-run.
    ///
    /// See docs/systems/raid.md, "Carrying and extracting".
    /// </summary>
    public static class LootValueMigration
    {
        private const string k_LootPrefabDirectory = "Assets/_Project/Prefabs/Loot";

        [MenuItem("Tools/Plunderspell/Add Loot Value To Loot Prefabs")]
        public static void AddLootValueToLootPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { k_LootPrefabDirectory });
            int added = 0;
            int alreadyPresent = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    if (root.TryGetComponent(out LootValue existing))
                    {
                        alreadyPresent++;
                        CopyDataFromPickup(root, existing);
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        continue;
                    }

                    var value = root.AddComponent<LootValue>();
                    CopyDataFromPickup(root, value);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    added++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[LootValue] {added} prefab(s) gained a LootValue, {alreadyPresent} already had one, " +
                      $"across {guids.Length} prefab(s) in {k_LootPrefabDirectory}.");
        }

        /// <summary>
        /// Takes the worth from wherever it is already authored. `_data` is a private
        /// <c>[SerializeField]</c> on <c>LootPickup</c>, so it is read through SerializedObject
        /// rather than by widening the field for a one-off migration.
        /// </summary>
        private static void CopyDataFromPickup(GameObject root, LootValue value)
        {
            if (!root.TryGetComponent(out LootPickup pickup))
            {
                return;
            }

            var serialized = new SerializedObject(pickup);
            SerializedProperty data = serialized.FindProperty("_data");
            if (data?.objectReferenceValue is LootItem item)
            {
                value.SetItem(item);
                EditorUtility.SetDirty(value);
            }
        }
    }
}
