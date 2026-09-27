using NUnit.Framework;
using Plunderspell.Castle;
using Plunderspell.Loot;
using Plunderspell.Raid;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The loot balance rules in docs/plans/loot-balance.md (#142, #148): nothing breaks from a
    /// waist-high drop, and no era fills the busy outer zones with pieces too heavy to lift.
    /// </summary>
    public class LootBalanceTests
    {
        /// <summary>Impact speed of a 1 m fall, rounded up: √(2 × 9.81 × 1) ≈ 4.43 m/s.</summary>
        private const float k_WaistHighDropSpeed = 4.5f;

        private static readonly string[] k_EraTables =
        {
            "Assets/_Project/Data/Loot/BronzeAge/RaidLootTable_BronzeAge.asset",
            "Assets/_Project/Data/Loot/HighMedieval/RaidLootTable_HighMedieval.asset",
            "Assets/_Project/Data/Loot/LateMedieval/RaidLootTable_LateMedieval.asset",
            "Assets/_Project/Data/Loot/AgeOfPowder/RaidLootTable_AgeOfPowder.asset",
        };

        [Test]
        public void Test_NoLootBreaksFromAWaistHighDrop()
        {
            int checkedCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:LootItem", new[] { "Assets/_Project/Data/Loot" }))
            {
                var item = AssetDatabase.LoadAssetAtPath<LootItem>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.GreaterOrEqual(item.Fragility, k_WaistHighDropSpeed,
                    $"{item.name} breaks at {item.Fragility} m/s, below a waist-high drop.");
                checkedCount++;
            }
            Assert.Greater(checkedCount, 20, "Sanity: the loot assets were found.");
        }

        [Test]
        public void Test_TheOuterZonesHoldNothingTooHeavyToLift()
        {
            foreach (string path in k_EraTables)
            {
                var table = AssetDatabase.LoadAssetAtPath<RaidLootTable>(path);
                Assert.IsNotNull(table, path);
                foreach (RaidLootTable.Entry entry in table.Entries)
                {
                    if (entry.Zone == CastleZone.Keep || entry.Zone == CastleZone.Crypt || entry.Prefab == null)
                        continue;
                    var body = entry.Prefab.GetComponent<Rigidbody>();
                    // Item lives in an assembly this test assembly does not reference; find it by name.
                    var item = entry.Prefab.GetComponent("Item");
                    Assert.IsNotNull(body, entry.Prefab.name);
                    Assert.IsNotNull(item, entry.Prefab.name);
                    // Item.Load needs Awake to have run; on a prefab asset, read its parts directly.
                    float grip = new SerializedObject(item).FindProperty("_gripStrength").floatValue;
                    float load = entry.Item.WeightKg * -Physics.gravity.y / grip;
                    Assert.LessOrEqual(load, 1f,
                        $"{entry.Item.name} ({entry.Item.WeightKg} kg) is too heavy to lift but spawns in the {entry.Zone} of {table.name}.");
                }
            }
        }
    }
}
