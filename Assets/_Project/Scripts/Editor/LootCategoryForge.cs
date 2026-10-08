using System.Collections.Generic;
using Plunderspell.Loot;
using Plunderspell.Market;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Sets <see cref="LootItem.Category"/> on every loot asset from the table below (the same table as
    /// docs/4-systems/market.md). A name not in the table is reported and left alone. Safe to run again.
    /// </summary>
    public static class LootCategoryForge
    {
        private static readonly Dictionary<string, LootCategory> Table = new Dictionary<string, LootCategory>
        {
            // Age of Powder
            { "Astrolabe", LootCategory.Curio }, { "CabinetOfCuriosities", LootCategory.Curio }, { "NautilusCup", LootCategory.Curio },
            { "SilverServiceTureen", LootCategory.Metal }, { "VenetianMirror", LootCategory.Curio },
            // Bronze Age
            { "FaienceHippopotamus", LootCategory.Curio }, { "GoldDeathMask", LootCategory.Metal }, { "OxhideIngot", LootCategory.Metal },
            { "SealedAmphora", LootCategory.Other }, { "TripodCauldron", LootCategory.Metal },
            // High Medieval
            { "ArmReliquary", LootCategory.Holy }, { "CoinCoffer", LootCategory.Metal }, { "GildedAltarpiece", LootCategory.Holy },
            { "IlluminatedPsalter", LootCategory.Holy }, { "SilverEwer", LootCategory.Metal },
            // Late Medieval
            { "BankersLedger", LootCategory.Other }, { "GildedNef", LootCategory.Metal }, { "JewelledHatBadge", LootCategory.Metal },
            { "ParadeArmourOnItsStand", LootCategory.Arms }, { "RolledTapestry", LootCategory.Other },
            // Any era
            { "AncientRelic", LootCategory.Curio }, { "CopperPot", LootCategory.Metal }, { "GoldenGoblet", LootCategory.Metal },
            { "HeavyChest", LootCategory.Other }, { "Loot_Conjured_Coin", LootCategory.Metal }, { "SilverPlate", LootCategory.Metal },
            { "Weapon_ArmingSword", LootCategory.Arms }, { "Weapon_BronzeSword", LootCategory.Arms }, { "Weapon_Crossbow", LootCategory.Arms },
            { "Weapon_FlintlockPistol", LootCategory.Arms }, { "Weapon_Longsword", LootCategory.Arms }, { "Weapon_Matchlock", LootCategory.Arms },
            { "Weapon_PaviseShield", LootCategory.Arms }, { "Weapon_PlateHelm", LootCategory.Arms }, { "Weapon_PowderGrenade", LootCategory.Arms },
            { "Weapon_RoundShield", LootCategory.Arms },
            // Generated placeholders (each also exists as "<name> 1")
            { "Loot_Crown_of_the_Founder", LootCategory.Metal }, { "Loot_Gilded_Chest", LootCategory.Other },
            { "Loot_Glass_Reliquary", LootCategory.Holy }, { "Loot_Silver_Plate", LootCategory.Metal }, { "Loot_Tin_Cup", LootCategory.Metal },
        };

        [MenuItem("Tools/Plunderspell/Set Loot Categories")]
        public static string Apply()
        {
            int set = 0;
            var unknown = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:LootItem"))
            {
                var item = AssetDatabase.LoadAssetAtPath<LootItem>(AssetDatabase.GUIDToAssetPath(guid));
                string name = item.name.EndsWith(" 1") ? item.name.Substring(0, item.name.Length - 2) : item.name;
                if (!Table.TryGetValue(name, out LootCategory category))
                {
                    unknown.Add(item.name);
                    continue;
                }

                item.Category = category;
                EditorUtility.SetDirty(item);
                set++;
            }

            AssetDatabase.SaveAssets();
            return $"set {set}; not in the table: {(unknown.Count == 0 ? "none" : string.Join(", ", unknown))}";
        }
    }
}
