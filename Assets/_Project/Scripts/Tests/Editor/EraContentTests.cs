using System;
using NUnit.Framework;
using RogueAi.Castle;
using RogueAi.Inventory;
using RogueAi.Raid;
using UnityEditor;
using UnityEngine;

namespace RogueAi.Tests.Editor
{
    /// <summary>
    /// Guards the per-era raid content forged by <c>EraContentForge</c>. See
    /// docs/systems/raid-scene-assembly.md, "Eras".
    /// </summary>
    public class EraContentTests
    {
        private const string k_CataloguePath = "Assets/_Project/Data/Eras/EraContentCatalogue.asset";
        private const string k_DefaultRegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";

        // The pieces ProceduralCastleGenerator asks for by id rather than drawing from a zone pool.
        private static readonly string[] k_FixedPieceIds =
            { "WallStraight", "WallCorner", "Bastion", "GatehouseModule", "Drawbridge", "CryptChamberFinal" };

        private static EraContentCatalogue Catalogue() =>
            AssetDatabase.LoadAssetAtPath<EraContentCatalogue>(k_CataloguePath);

        private static CastleZone[] Zones => (CastleZone[])Enum.GetValues(typeof(CastleZone));

        [Test]
        public void EveryEraHasAnEntryWithItsOwnLootAndEnemies()
        {
            EraContentCatalogue catalogue = Catalogue();
            Assert.IsNotNull(catalogue, $"No catalogue at {k_CataloguePath}; run Forge Era Content.");

            foreach (HistoricalEra era in Enum.GetValues(typeof(HistoricalEra)))
            {
                EraContentCatalogue.Entry entry = catalogue.For(era);
                Assert.IsNotNull(entry, $"{era} has no catalogue entry.");
                Assert.IsNotNull(entry.Loot, $"{era} has no loot table.");
                Assert.IsNotNull(entry.Enemies, $"{era} has no enemy roster.");
                // Weapons are found as loot; an era table without them strips every weapon from the raid.
                Assert.IsTrue(entry.Loot.Entries.Exists(e => e.Item != null && e.Item.name.StartsWith("Weapon_")),
                    $"{era} loot table carries no weapons.");
            }
        }

        /// <summary>A zone with no posting spawns nothing there, silently.</summary>
        [Test]
        public void EveryEraPostsLootAndEnemiesToEveryZone()
        {
            foreach (EraContentCatalogue.Entry entry in Catalogue().Entries)
            {
                foreach (CastleZone zone in Zones)
                {
                    Assert.IsNotEmpty(entry.Loot.EntriesFor(zone), $"{entry.Era} has no loot in {zone}.");
                    Assert.IsNotEmpty(entry.Enemies.EntriesFor(zone), $"{entry.Era} has no enemy in {zone}.");
                }
            }
        }

        /// <summary>An era room set is a whole castle: every zone, every fixed piece, every plug.</summary>
        [Test]
        public void EveryEraRoomSetBuildsAWholeCastle()
        {
            var fallback = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(k_DefaultRegistryPath);
            foreach (EraContentCatalogue.Entry entry in Catalogue().Entries)
            {
                CastleRoomRegistry rooms = entry.Rooms != null ? entry.Rooms : fallback;
                foreach (CastleZone zone in Zones)
                    Assert.IsNotEmpty(rooms.GetModulesForZone(zone), $"{entry.Era} has no rooms in {zone}.");
                foreach (string id in k_FixedPieceIds)
                    Assert.IsNotNull(rooms.GetById(id)?.Prefab, $"{entry.Era} has no '{id}' piece.");
                foreach (CastleZone zone in Zones)
                {
                    if (ProceduralCastleGenerator.IsEnclosedRoom(zone))
                        Assert.IsNotNull(rooms.GetDoorPlugForZone(zone), $"{entry.Era} has no door plug for {zone}.");
                }
            }
        }

        /// <summary>
        /// The two finished eras must be told apart by what is in them, which is the point of the
        /// catalogue: no Bronze Age room, item or enemy may also appear in the High Medieval set.
        /// </summary>
        [Test]
        public void BronzeAgeAndHighMedievalShareNothing()
        {
            EraContentCatalogue catalogue = Catalogue();
            EraContentCatalogue.Entry bronze = catalogue.For(HistoricalEra.BronzeAge);
            EraContentCatalogue.Entry high = catalogue.For(HistoricalEra.HighMedieval);
            CastleRoomRegistry highRooms = high.Rooms != null
                ? high.Rooms
                : AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(k_DefaultRegistryPath);

            Assert.IsNotNull(bronze.Rooms, "The Bronze Age has its own room set.");
            foreach (CastleRoomModuleData module in bronze.Rooms.Modules)
            {
                foreach (CastleRoomModuleData other in highRooms.Modules)
                    Assert.AreNotSame(module.Prefab, other.Prefab, $"{module.RoomId} is in both eras.");
            }
            foreach (RaidLootTable.Entry loot in bronze.Loot.Entries)
                Assert.IsFalse(high.Loot.Entries.Exists(e => e.Prefab == loot.Prefab), $"{loot.Item.DisplayName} is in both eras.");
            foreach (EnemyRoster.Entry enemy in bronze.Enemies.Entries)
                Assert.IsFalse(high.Enemies.Entries.Exists(e => e.Prefab == enemy.Prefab), $"{enemy.EnemyId} is in both eras.");
        }

        /// <summary>Spawn sites compose with the prefab root rotation, so every root must stand upright.</summary>
        [Test]
        public void EraRoomPrefabsStandUpright()
        {
            foreach (EraContentCatalogue.Entry entry in Catalogue().Entries)
            {
                if (entry.Rooms == null)
                    continue;
                foreach (CastleRoomModuleData module in entry.Rooms.Modules)
                {
                    Vector3 euler = module.Prefab.transform.eulerAngles;
                    Assert.AreEqual(90f, euler.x, 0.5f, $"{module.Prefab.name} root is not the upright (90,0,0).");
                    Assert.IsNotNull(module.Prefab.GetComponentInChildren<MeshCollider>(), $"{module.Prefab.name} has no collider.");
                }
            }
        }
    }
}
