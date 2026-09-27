using System.IO;
using Plunderspell.Castle;
using Plunderspell.Loot;
using Plunderspell.Raid;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Authors the <see cref="RaidLootTable"/> that pairs the hand-modelled loot prefabs with the
    /// zones they are found in. Run once; the table is a committed asset from then on.
    ///
    /// See docs/4-systems/raid-scene-assembly.md ("Loot table") for the zone/weight rationale.
    /// </summary>
    public static class RaidLootTableForge
    {
        private const string ItemDirectory = "Assets/_Project/Data/Loot";
        private const string PrefabDirectory = "Assets/_Project/Prefabs/Loot";
        private const string TablePath = "Assets/_Project/Data/Loot/RaidLootTable.asset";

        /// <summary>One loot asset and the zones it turns up in.</summary>
        private readonly struct LootSpec
        {
            public readonly string Name;
            public readonly (CastleZone Zone, int Weight)[] Posts;

            public LootSpec(string name, params (CastleZone, int)[] posts)
            {
                Name = name;
                Posts = posts;
            }
        }

        // Worth climbs inward — Copper Pot 15 at the wall, Ancient Relic 500 in the crypt — so the
        // long carry back out is what the valuable things cost.
        private static readonly LootSpec[] Specs =
        {
            new LootSpec("CopperPot",    (CastleZone.CurtainWall, 14), (CastleZone.OuterBailey, 10)),
            new LootSpec("SilverPlate",  (CastleZone.OuterBailey, 12), (CastleZone.InnerWard, 8)),
            new LootSpec("GoldenGoblet", (CastleZone.InnerWard, 10),   (CastleZone.Keep, 8)),
            new LootSpec("HeavyChest",   (CastleZone.Keep, 8),         (CastleZone.Crypt, 6)),
            new LootSpec("AncientRelic", (CastleZone.Crypt, 10)),
        };

        [MenuItem("Tools/Plunderspell/Forge Raid Loot Table")]
        public static void Forge()
        {
            EnsureFolder(Path.GetDirectoryName(TablePath).Replace('\\', '/'));

            RaidLootTable table = LoadOrCreate(TablePath);
            table.Entries.Clear();

            int missing = 0;
            foreach (LootSpec spec in Specs)
            {
                var item = AssetDatabase.LoadAssetAtPath<LootItem>($"{ItemDirectory}/{spec.Name}.asset");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDirectory}/{spec.Name}.prefab");

                if (item == null || prefab == null)
                {
                    Debug.LogError($"Plunderspell: loot '{spec.Name}' missing " +
                                   $"{(item == null ? "item asset" : "")} {(prefab == null ? "prefab" : "")}".Trim());
                    missing++;
                    continue;
                }

                foreach ((CastleZone zone, int weight) in spec.Posts)
                {
                    table.Entries.Add(new RaidLootTable.Entry
                    {
                        Item = item,
                        Zone = zone,
                        Weight = weight,
                        Prefab = prefab
                    });
                }
            }

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Plunderspell: forged {table.Entries.Count} loot placement(s) into {TablePath}" +
                      (missing > 0 ? $" ({missing} spec(s) skipped)." : "."));
        }

        private static RaidLootTable LoadOrCreate(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<RaidLootTable>(path);
            if (existing != null)
                return existing;

            var created = ScriptableObject.CreateInstance<RaidLootTable>();
            AssetDatabase.CreateAsset(created, path);
            return created;
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
