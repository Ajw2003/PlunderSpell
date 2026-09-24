using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RogueAi.Castle;
using RogueAi.Guards;
using RogueAi.Inventory;
using RogueAi.Loot;
using RogueAi.Raid;
using RogueAi.Status;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Turns the per-era art that exists so far into raid content: prefabs, loot items, loot tables,
    /// enemy rosters and room registries, gathered into the <see cref="EraContentCatalogue"/> that
    /// <see cref="RaidDirector"/> reads when a raid sets out. Re-run after new art lands; every
    /// output lives at a fixed path and is overwritten, never duplicated.
    ///
    /// See docs/systems/raid-scene-assembly.md ("Eras") for what each era is built from and what it
    /// falls back to while its art is unfinished.
    /// </summary>
    public static class EraContentForge
    {
        private const string ManifestPath = "Assets/Models/ArtBible/artforge_manifest.json";
        private const string ArtSpecDirectory = "docs/art/data";
        private const string LootAnchorPath = "Assets/_Project/Data/Castle/CastleLootAnchors.json";
        private const string CastleModelDirectory = "Assets/_Project/Art/Models/Castle";
        private const string DefaultRegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";
        private const string DefaultLootTablePath = "Assets/_Project/Data/Loot/RaidLootTable.asset";
        private const string BoltPrefabPath = "Assets/_Project/Prefabs/Projectiles/Bolt.prefab";
        private const string CataloguePath = "Assets/_Project/Data/Eras/EraContentCatalogue.asset";

        /// <summary>Root rotation at which a castle mesh stands floor-down (see <see cref="CastlePrefabOrientationFix"/>).</summary>
        private static readonly Vector3 CastleUprightEuler = new Vector3(90f, 0f, 0f);

        /// <summary>The art bible's short age names, as used by the ArtForge manifest and docs/art/data.</summary>
        private static readonly (string Age, string Folder, HistoricalEra Era)[] Ages =
        {
            ("bronze", "Bronze", HistoricalEra.BronzeAge),
            ("high", "High", HistoricalEra.HighMedieval),
            ("late", "Late", HistoricalEra.LateMedieval),
            ("powder", "Powder", HistoricalEra.AgeOfPowder),
        };

        // The curtain-wall pieces the generator asks for by id. An era's own piece takes the id of the
        // High Medieval piece it stands in for, so the generator needs no per-era knowledge.
        private static readonly Dictionary<string, string> BronzeWallRoles = new Dictionary<string, string>
        {
            { "BronzeLionGate", "GatehouseModule" },
            { "BronzeWallStraight", "WallStraight" },
            { "BronzeWallCorner", "WallCorner" },
            { "BronzeBastion", "Bastion" },
            { "BronzeGateApproach", "Drawbridge" },
            // The one room the generator always puts at the crypt's centre.
            { "BronzeTholos", "CryptChamberFinal" },
        };

        /// <summary>Every era room model on disk, by the zone it was built for (asset_specs.py).</summary>
        private static readonly (string Key, CastleZone Zone)[] BronzeRooms =
        {
            ("BronzeLionGate", CastleZone.CurtainWall), ("BronzeWallStraight", CastleZone.CurtainWall),
            ("BronzeWallCorner", CastleZone.CurtainWall), ("BronzeBastion", CastleZone.CurtainWall),
            ("BronzeGateApproach", CastleZone.CurtainWall),
            ("BronzeChariotShed", CastleZone.OuterBailey), ("BronzeFoundry", CastleZone.OuterBailey),
            ("BronzeLevyBarracks", CastleZone.OuterBailey), ("BronzeCistern", CastleZone.OuterBailey),
            ("BronzeOilPress", CastleZone.OuterBailey),
            ("BronzePithosMagazine", CastleZone.InnerWard), ("BronzeFrescoCourt", CastleZone.InnerWard),
            ("BronzeShrine", CastleZone.InnerWard), ("BronzePalaceKitchen", CastleZone.InnerWard),
            ("BronzeTabletArchive", CastleZone.InnerWard),
            ("BronzeMegaron", CastleZone.Keep), ("BronzeTreasury", CastleZone.Keep),
            ("BronzeQueensHall", CastleZone.Keep), ("BronzeBathRoom", CastleZone.Keep),
            ("BronzeMegaronStair", CastleZone.Keep),
            ("BronzeDromos", CastleZone.Crypt), ("BronzeGraveCircle", CastleZone.Crypt),
            ("BronzeLarnaxVault", CastleZone.Crypt), ("BronzeTholos", CastleZone.Crypt),
            ("BronzeShaftStair", CastleZone.Crypt),
        };

        private static readonly (string Key, CastleZone Zone)[] BronzePlugs =
        {
            ("BronzeDoorPlugOuterBailey", CastleZone.OuterBailey), ("BronzeDoorPlugInnerWard", CastleZone.InnerWard),
            ("BronzeDoorPlugKeep", CastleZone.Keep), ("BronzeDoorPlugCrypt", CastleZone.Crypt),
        };

        // The Late Medieval curtain-wall stand-ins, by the High Medieval id each replaces
        // (docs/art/rooms/LateMedieval.md, "Stands in for").
        private static readonly Dictionary<string, string> LateWallRoles = new Dictionary<string, string>
        {
            { "LateBarbican", "GatehouseModule" },
            { "LateWallStraight", "WallStraight" },
            { "LateWallCorner", "WallCorner" },
            { "LateBastion", "Bastion" },
            { "LateDrawbridge", "Drawbridge" },
            // The one room the generator always puts at the crypt's centre.
            { "LateEffigyCrypt", "CryptChamberFinal" },
        };

        /// <summary>Every Late Medieval room model, by the zone it was built for (asset_specs.py).</summary>
        private static readonly (string Key, CastleZone Zone)[] LateRooms =
        {
            ("LateBarbican", CastleZone.CurtainWall), ("LateWallStraight", CastleZone.CurtainWall),
            ("LateWallCorner", CastleZone.CurtainWall), ("LateBastion", CastleZone.CurtainWall),
            ("LateDrawbridge", CastleZone.CurtainWall),
            ("LateArtilleryYard", CastleZone.OuterBailey), ("LateGunFoundry", CastleZone.OuterBailey),
            ("LateHandgunnerBarracks", CastleZone.OuterBailey), ("LateBrewhouse", CastleZone.OuterBailey),
            ("LateTreadwheelWell", CastleZone.OuterBailey),
            ("LateCountingHouse", CastleZone.InnerWard), ("LateArmouryHall", CastleZone.InnerWard),
            ("LateSpitKitchen", CastleZone.InnerWard), ("LateChantryChapel", CastleZone.InnerWard),
            ("LateLibrary", CastleZone.InnerWard),
            ("LateGreatHall", CastleZone.Keep), ("LateJewelHouse", CastleZone.Keep),
            ("LateStateBedchamber", CastleZone.Keep), ("LateTapestrySolar", CastleZone.Keep),
            ("LateTurretStair", CastleZone.Keep),
            ("LateUndercroft", CastleZone.Crypt), ("LateOubliette", CastleZone.Crypt),
            ("LateCharnelHouse", CastleZone.Crypt), ("LateEffigyCrypt", CastleZone.Crypt),
            ("LateUndercroftStair", CastleZone.Crypt),
        };

        private static readonly (string Key, CastleZone Zone)[] LatePlugs =
        {
            ("LateDoorPlugOuterBailey", CastleZone.OuterBailey), ("LateDoorPlugInnerWard", CastleZone.InnerWard),
            ("LateDoorPlugKeep", CastleZone.Keep), ("LateDoorPlugCrypt", CastleZone.Crypt),
        };

        // Worth climbs inward, as in RaidLootTableForge: the cheapest piece at the wall, the dearest
        // in the crypt. Indexed by the item's rank by worth within its era.
        private static readonly (CastleZone Zone, int Weight)[][] LootPostsByRank =
        {
            new[] { (CastleZone.CurtainWall, 14), (CastleZone.OuterBailey, 10) },
            new[] { (CastleZone.OuterBailey, 12), (CastleZone.InnerWard, 8) },
            new[] { (CastleZone.InnerWard, 10), (CastleZone.Keep, 8) },
            new[] { (CastleZone.Keep, 8), (CastleZone.Crypt, 6) },
            new[] { (CastleZone.Crypt, 10) },
        };

        // ---------------------------------------------------------------------------------------
        // JSON shapes (only the fields read here)
        // ---------------------------------------------------------------------------------------

        [Serializable] private class Manifest { public ManifestAsset[] assets; }
        [Serializable] private class ManifestAsset
        {
            public string kind, age, slug, name, title;
            public bool passed;
            public ManifestStats stats;
            public ManifestFiles files;
        }
        [Serializable] private class ManifestStats { public float height; }
        [Serializable] private class ManifestFiles { public string fbx; }

        [Serializable] private class AgeSpec { public ItemSpec[] items; public EnemySpec[] enemies; }
        [Serializable] private class ItemSpec { public string slug, name; public float worth, bulk, fragility; public bool artifact; }
        [Serializable] private class EnemySpec { public string slug, name, role; public string[] zones; public float height_m; }

        // ---------------------------------------------------------------------------------------

        [MenuItem("Tools/Plunderspell/Forge Era Content (rooms, loot, enemies)")]
        public static void ForgeMenu() => Debug.Log(Forge());

        /// <summary>Forges every era's content and the catalogue. Returns a readable report.</summary>
        public static string Forge()
        {
            var report = new StringBuilder("[EraContent]\n");

            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            Dictionary<string, List<Vector3>> anchors = ReadLootAnchors(report);

            var catalogue = LoadOrCreate<EraContentCatalogue>(CataloguePath);
            catalogue.Entries.Clear();

            var defaultRegistry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(DefaultRegistryPath);
            var defaultLoot = AssetDatabase.LoadAssetAtPath<RaidLootTable>(DefaultLootTablePath);

            foreach ((string age, string folder, HistoricalEra era) in Ages)
            {
                var spec = JsonUtility.FromJson<AgeSpec>(File.ReadAllText($"{ArtSpecDirectory}/{age}.json"));

                var entry = new EraContentCatalogue.Entry { Era = era };
                entry.Loot = ForgeLoot(era, age, manifest, spec, defaultLoot, report);
                entry.Enemies = ForgeEnemies(era, age, manifest, spec, report);
                entry.Rooms = ForgeRooms(era, defaultRegistry, anchors, report);
                catalogue.Entries.Add(entry);
            }

            EditorUtility.SetDirty(catalogue);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            report.AppendLine($"Catalogue: {CataloguePath}");
            return report.ToString();
        }

        // ---------------------------------------------------------------------------------------
        // Loot
        // ---------------------------------------------------------------------------------------

        private static RaidLootTable ForgeLoot(HistoricalEra era, string age, Manifest manifest, AgeSpec spec,
            RaidLootTable defaults, StringBuilder report)
        {
            var built = new List<(LootItem Item, GameObject Prefab)>();
            foreach (ManifestAsset asset in manifest.assets)
            {
                if (asset.kind != "items" || asset.age != age || !asset.passed)
                    continue;
                ItemSpec item = Array.Find(spec.items, i => i.slug == asset.slug);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(asset.files.fbx);
                if (item == null || model == null)
                {
                    report.AppendLine($"  {era} loot {asset.name}: skipped ({(item == null ? "no spec" : "no model")})");
                    continue;
                }

                LootItem data = LoadOrCreate<LootItem>($"Assets/_Project/Data/Loot/{era}/{asset.name}.asset");
                data.DisplayName = asset.title;
                data.Worth = item.worth;
                data.Bulk = item.bulk;
                data.Fragility = item.fragility;
                data.IsArtifact = item.artifact;
                EditorUtility.SetDirty(data);

                built.Add((data, BuildLootPrefab(model, data, $"Assets/_Project/Prefabs/Loot/{era}/{asset.name}.prefab")));
            }

            built.Sort((a, b) => a.Item.Worth.CompareTo(b.Item.Worth));

            var table = LoadOrCreate<RaidLootTable>($"Assets/_Project/Data/Loot/{era}/RaidLootTable_{era}.asset");
            table.Entries.Clear();
            for (int rank = 0; rank < built.Count; rank++)
            {
                // More than five items would share the deepest rank; fewer leave the deep zones to
                // the items that exist, which the planner handles by falling back per zone.
                (CastleZone Zone, int Weight)[] posts = LootPostsByRank[Mathf.Min(rank, LootPostsByRank.Length - 1)];
                foreach ((CastleZone zone, int weight) in posts)
                {
                    table.Entries.Add(new RaidLootTable.Entry
                    {
                        Item = built[rank].Item, Zone = zone, Weight = weight, Prefab = built[rank].Prefab
                    });
                }
            }
            int weapons = 0;
            if (defaults != null)
            {
                // Weapons are found as loot (docs/Decisions.md, 2026-09-23). Each era keeps the
                // default table's weapons that belong to it, by the era its inventory item names.
                foreach (RaidLootTable.Entry weapon in defaults.Entries)
                {
                    if (weapon?.Item == null || !weapon.Item.name.StartsWith("Weapon_", StringComparison.Ordinal))
                        continue;
                    string inventoryPath = $"Assets/_Project/Data/Inventory/{weapon.Item.name.Substring("Weapon_".Length)}.asset";
                    var inventoryItem = AssetDatabase.LoadAssetAtPath<InventoryItem>(inventoryPath);
                    if (inventoryItem != null && inventoryItem.EraAcquired != era)
                        continue;
                    table.Entries.Add(new RaidLootTable.Entry
                    {
                        Item = weapon.Item, Zone = weapon.Zone, Weight = weapon.Weight, Prefab = weapon.Prefab
                    });
                    weapons++;
                }

                table.CurtainWallDensity = defaults.CurtainWallDensity;
                table.OuterBaileyDensity = defaults.OuterBaileyDensity;
                table.InnerWardDensity = defaults.InnerWardDensity;
                table.KeepDensity = defaults.KeepDensity;
                table.CryptDensity = defaults.CryptDensity;
            }
            EditorUtility.SetDirty(table);

            report.AppendLine($"  {era} loot: {built.Count} item(s), {weapons} weapon posting(s), {table.Entries.Count} posting(s)");
            return built.Count > 0 ? table : null;
        }

        /// <summary>
        /// A prefab variant of the item model carrying what the haul needs: a body, a box collider
        /// fitted to the mesh, the pickup, the drag handle, the tally value and a network transform.
        /// The ArtForge models are exported in metres with the Z-up correction on their root, so the
        /// root rotation and scale are kept as imported.
        /// </summary>
        private static GameObject BuildLootPrefab(GameObject model, LootItem data, string path)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = Path.GetFileNameWithoutExtension(path);

                var box = instance.AddComponent<BoxCollider>();
                Bounds local = LocalMeshBounds(instance);
                box.center = local.center;
                box.size = local.size;

                var body = instance.AddComponent<Rigidbody>();
                body.mass = Mathf.Max(0.5f, data.Bulk);

                instance.AddComponent<Item>();
                var pickup = instance.AddComponent<LootPickup>();
                pickup.SetData(data);
                var pickupSo = new SerializedObject(pickup);
                pickupSo.FindProperty("_meshRenderer").objectReferenceValue = instance.GetComponentInChildren<MeshRenderer>();
                pickupSo.ApplyModifiedPropertiesWithoutUndo();

                instance.AddComponent<LootValue>().SetItem(data);
                instance.AddComponent<PurrNet.NetworkTransform>();

                EnsureFolder(Path.GetDirectoryName(path));
                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        /// <summary>Bounds of every mesh under <paramref name="root"/>, in the root's local space.</summary>
        private static Bounds LocalMeshBounds(GameObject root)
        {
            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
            bool any = false;
            var bounds = new Bounds();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                    continue;
                Bounds m = filter.sharedMesh.bounds;
                Matrix4x4 toLocal = toRoot * filter.transform.localToWorldMatrix;
                for (int c = 0; c < 8; c++)
                {
                    var corner = new Vector3(
                        (c & 1) == 0 ? m.min.x : m.max.x,
                        (c & 2) == 0 ? m.min.y : m.max.y,
                        (c & 4) == 0 ? m.min.z : m.max.z);
                    Vector3 p = toLocal.MultiplyPoint3x4(corner);
                    if (!any)
                    {
                        bounds = new Bounds(p, Vector3.zero);
                        any = true;
                    }
                    else
                        bounds.Encapsulate(p);
                }
            }
            return any ? bounds : new Bounds(Vector3.zero, Vector3.one * 0.1f);
        }

        // ---------------------------------------------------------------------------------------
        // Enemies
        // ---------------------------------------------------------------------------------------

        /// <summary>Guard tuning per art-bible role: patrol speed, chase speed, sight, health, weight.</summary>
        private static (float Patrol, float Chase, float Sight, float Health, int Weight) Tuning(string role, float height)
        {
            // A dog-sized "special" is a hound: fast, fragile and short-sighted.
            if (role == "special" && height < 1.2f)
                return (2.8f, 6.5f, 12f, 55f, 6);
            switch (role)
            {
                case "patrol": return (2.0f, 4.2f, 14f, 80f, 12);
                case "ranged": return (1.8f, 3.8f, 20f, 60f, 8);
                case "heavy": return (1.5f, 3.4f, 15f, 180f, 6);
                default: return (2.0f, 4.5f, 16f, 100f, 5);
            }
        }

        private static EnemyRoster ForgeEnemies(HistoricalEra era, string age, Manifest manifest, AgeSpec spec,
            StringBuilder report)
        {
            var roster = LoadOrCreate<EnemyRoster>($"Assets/_Project/Data/Enemies/EnemyRoster_{era}.asset");
            roster.Entries.Clear();
            var bolt = AssetDatabase.LoadAssetAtPath<GameObject>(BoltPrefabPath);

            var built = new List<(EnemySpec Spec, GameObject Prefab, string Name)>();
            foreach (ManifestAsset asset in manifest.assets)
            {
                if (asset.kind != "enemies" || asset.age != age || !asset.passed)
                    continue;
                EnemySpec enemy = Array.Find(spec.enemies, e => e.slug == asset.slug);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(asset.files.fbx);
                if (enemy == null || model == null)
                {
                    report.AppendLine($"  {era} enemy {asset.name}: skipped ({(enemy == null ? "no spec" : "no model")})");
                    continue;
                }

                GameObject prefab = BuildEnemyPrefab(model, enemy, asset.stats.height,
                    $"Assets/_Project/Prefabs/Enemies/{era}/{asset.name}.prefab", bolt);
                built.Add((enemy, prefab, asset.name));

                int weight = Tuning(enemy.role, enemy.height_m).Weight;
                foreach (string zoneName in enemy.zones)
                {
                    if (Enum.TryParse(zoneName, out CastleZone zone))
                        roster.Entries.Add(new EnemyRoster.Entry { EnemyId = asset.name, Zone = zone, Era = era, Weight = weight, Prefab = prefab });
                }
            }

            // Every zone must have someone in it, or the planner's guards there spawn as nothing.
            // An unfinished era fills a gap with its heaviest soldier deep in the castle and its
            // patrol outside, falling back to whoever it has.
            foreach (CastleZone zone in (CastleZone[])Enum.GetValues(typeof(CastleZone)))
            {
                if (built.Count == 0 || roster.EntriesFor(zone).Count > 0)
                    continue;
                string wanted = zone == CastleZone.Keep || zone == CastleZone.Crypt ? "heavy" : "patrol";
                int pick = built.FindIndex(b => b.Spec.role == wanted);
                if (pick < 0)
                    pick = built.FindIndex(b => b.Spec.height_m >= 1.2f);
                if (pick < 0)
                    pick = 0;
                roster.Entries.Add(new EnemyRoster.Entry { EnemyId = built[pick].Name, Zone = zone, Era = era, Weight = 6, Prefab = built[pick].Prefab });
                report.AppendLine($"  {era} enemies: {zone} had no posting; filled with {built[pick].Name}");
            }

            EditorUtility.SetDirty(roster);
            report.AppendLine($"  {era} enemies: {built.Count} prefab(s), {roster.Entries.Count} posting(s)");
            return built.Count > 0 ? roster : null;
        }

        /// <summary>
        /// A prefab variant of the rigged enemy model with the components a guard needs, set up the
        /// way <see cref="EnemyPrefabForge"/> sets up the original roster. ArtForge exports in
        /// metres, so the model is only rescaled if its measured height disagrees with the
        /// manifest's by more than 5%.
        /// </summary>
        private static GameObject BuildEnemyPrefab(GameObject model, EnemySpec spec, float manifestHeight,
            string path, GameObject bolt)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = Path.GetFileNameWithoutExtension(path);

                float scale = 1f;
                if (PrefabGeometry.TryMeasureVerticalExtent(instance, out float low, out float high) && manifestHeight > 0f)
                {
                    float measured = high - low;
                    if (measured > 0.01f && Mathf.Abs(measured / manifestHeight - 1f) > 0.05f)
                        scale = manifestHeight / measured;
                }
                instance.transform.localScale = Vector3.one * scale;
                EnemyPrefabForge.GroundModel(instance);

                // The body, not the weapon held over it, is what the capsule and agent stand for.
                float height = Mathf.Max(0.5f, spec.height_m);
                Bounds local = LocalRendererFootprint(instance);
                float radius = Mathf.Clamp(Mathf.Max(local.size.x, local.size.z) * 0.5f * scale, 0.3f, 0.6f);

                var capsule = instance.AddComponent<CapsuleCollider>();
                capsule.radius = radius / scale;
                capsule.height = height / scale;
                capsule.center = new Vector3(0f, height * 0.5f / scale, 0f);

                (float patrol, float chase, float sight, float health, _) = Tuning(spec.role, spec.height_m);

                var agent = instance.AddComponent<NavMeshAgent>();
                agent.radius = radius;
                agent.height = height;
                agent.speed = patrol;
                agent.angularSpeed = 240f;
                agent.acceleration = 12f;
                agent.stoppingDistance = 0.8f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

                instance.AddComponent<StatusEffectReceiver>();
                CastleGuard guard = instance.AddComponent<CastleGuard>();
                var so = new SerializedObject(guard);
                so.FindProperty("_sightRange").floatValue = sight;
                so.FindProperty("_patrolSpeed").floatValue = patrol;
                so.FindProperty("_chaseSpeed").floatValue = chase;
                so.FindProperty("_maxHealth").floatValue = health;
                so.FindProperty("_eyeHeight").floatValue = height * 0.9f;
                so.FindProperty("_geometryLayers").intValue = 1;
                if (spec.role == "ranged" && bolt != null)
                    so.FindProperty("_projectilePrefab").objectReferenceValue = bolt;
                so.ApplyModifiedPropertiesWithoutUndo();

                instance.AddComponent<PurrNet.NetworkTransform>();

                EnsureFolder(Path.GetDirectoryName(path));
                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        /// <summary>Horizontal footprint of the model's renderers, in the root's unscaled local space.</summary>
        private static Bounds LocalRendererFootprint(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one * 0.6f);
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers)
                b.Encapsulate(r.bounds);
            float s = Mathf.Max(0.0001f, root.transform.lossyScale.x);
            return new Bounds(b.center / s, b.size / s);
        }

        // ---------------------------------------------------------------------------------------
        // Rooms
        // ---------------------------------------------------------------------------------------

        private static CastleRoomRegistry ForgeRooms(HistoricalEra era, CastleRoomRegistry defaults,
            Dictionary<string, List<Vector3>> anchors, StringBuilder report)
        {
            switch (era)
            {
                case HistoricalEra.BronzeAge:
                    return ForgeRegistry(era, "BronzeAge", BronzeRooms, BronzePlugs, BronzeWallRoles,
                        defaults, keepDefaultZones: false, anchors, report);
                case HistoricalEra.LateMedieval:
                    return ForgeRegistry(era, "LateMedieval", LateRooms, LatePlugs, LateWallRoles,
                        defaults, keepDefaultZones: false, anchors, report);
                default:
                    // High Medieval is the default registry; the Age of Powder has no rooms yet.
                    report.AppendLine($"  {era} rooms: scene default ({(defaults != null ? defaults.name : "none")})");
                    return null;
            }
        }

        /// <summary>
        /// Builds an era's registry from its room models. With <paramref name="keepDefaultZones"/>,
        /// every zone the era has no rooms for keeps the default registry's rooms, so a half-built
        /// era still has a whole castle. Door plugs fall back per zone the same way.
        /// </summary>
        private static CastleRoomRegistry ForgeRegistry(HistoricalEra era, string folder,
            (string Key, CastleZone Zone)[] rooms, (string Key, CastleZone Zone)[] plugs,
            Dictionary<string, string> roles, CastleRoomRegistry defaults, bool keepDefaultZones,
            Dictionary<string, List<Vector3>> anchors, StringBuilder report)
        {
            var registry = LoadOrCreate<CastleRoomRegistry>($"Assets/_Project/Data/Castle/CastleRoomRegistry_{era}.asset");
            registry.Modules.Clear();
            registry.DoorPlugs.Clear();

            var coveredZones = new HashSet<CastleZone>();
            int built = 0;
            foreach ((string key, CastleZone zone) in rooms)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{CastleModelDirectory}/{folder}/{key}.fbx");
                if (model == null)
                {
                    report.AppendLine($"  {era} room {key}: no model, skipped");
                    continue;
                }

                string roomId = roles.TryGetValue(key, out string role) ? role : key;
                CastleRoomModuleData template = defaults != null ? defaults.GetById(roomId) : null;
                GameObject prefab = BuildRoomPrefab(model, key, roomId, zone, template?.Prefab,
                    $"Assets/_Project/Prefabs/Castle/{folder}/{key}.prefab");

                registry.Modules.Add(new CastleRoomModuleData
                {
                    RoomId = roomId,
                    Zone = zone,
                    Prefab = prefab,
                    Weight = template != null ? template.Weight : 1,
                    LootAnchors = anchors.TryGetValue(key, out List<Vector3> blender)
                        ? blender.ConvertAll(BlenderToModule).ToArray()
                        : new Vector3[0]
                });
                coveredZones.Add(zone);
                built++;
            }

            if (keepDefaultZones && defaults != null)
            {
                foreach (CastleRoomModuleData module in defaults.Modules)
                {
                    if (module != null && !coveredZones.Contains(module.Zone))
                        registry.Modules.Add(module);
                }
            }

            var pluggedZones = new HashSet<CastleZone>();
            foreach ((string key, CastleZone zone) in plugs)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{CastleModelDirectory}/{folder}/{key}.fbx");
                if (model == null)
                    continue;
                registry.DoorPlugs.Add(new CastleRoomModuleData
                {
                    RoomId = key, Zone = zone, Weight = 1,
                    Prefab = BuildPlugPrefab(model, key, $"Assets/_Project/Prefabs/Castle/{folder}/{key}.prefab")
                });
                pluggedZones.Add(zone);
            }
            if (defaults != null)
            {
                foreach (CastleRoomModuleData plug in defaults.DoorPlugs)
                {
                    if (plug != null && !pluggedZones.Contains(plug.Zone))
                        registry.DoorPlugs.Add(plug);
                }
            }

            EditorUtility.SetDirty(registry);
            report.AppendLine($"  {era} rooms: {built} era room(s), {registry.Modules.Count} module(s) total, " +
                              $"{registry.DoorPlugs.Count} door plug(s)");
            return registry;
        }

        /// <summary>
        /// The same mapping <see cref="CastleLootAnchorImporter"/> measured for the High Medieval set,
        /// (x, z, y): the era rooms come out of the same pipeline and the same room kit.
        /// </summary>
        private static Vector3 BlenderToModule(Vector3 b) => new Vector3(b.x, b.z, b.y);

        /// <summary>
        /// A prefab variant of a room model, set up as the High Medieval rooms are: upright root, a
        /// mesh collider, the module component and its sockets. A curtain-wall piece copies its
        /// sockets from the High Medieval piece it stands in for; a room gets a door on each wall at
        /// its zone's archway height, read off any default room of that zone.
        /// </summary>
        private static GameObject BuildRoomPrefab(GameObject model, string key, string roomId, CastleZone zone,
            GameObject template, string path)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = key;
                instance.transform.localEulerAngles = CastleUprightEuler;
                AddMeshCollider(instance);

                var module = instance.AddComponent<CastleRoomModule>();
                module.Zone = zone;
                module.RoomId = roomId;

                if (template != null)
                {
                    foreach (SocketPoint source in template.GetComponentsInChildren<SocketPoint>(true))
                        AddSocket(instance.transform, source.name, source.transform.localPosition, source.Type, source.Facing);
                }
                else if (zone != CastleZone.CurtainWall)
                {
                    float y = SocketHeightFor(zone);
                    AddSocket(instance.transform, "Socket_north", new Vector3(0f, y, -5.35f), SocketType.Door, Direction.South);
                    AddSocket(instance.transform, "Socket_south", new Vector3(0f, y, 5.35f), SocketType.Door, Direction.North);
                    AddSocket(instance.transform, "Socket_east", new Vector3(5.35f, y, 0f), SocketType.Door, Direction.East);
                    AddSocket(instance.transform, "Socket_west", new Vector3(-5.35f, y, 0f), SocketType.Door, Direction.West);
                }
                module.PopulateSockets();

                EnsureFolder(Path.GetDirectoryName(path));
                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static GameObject BuildPlugPrefab(GameObject model, string key, string path)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = key;
                instance.transform.localEulerAngles = CastleUprightEuler;
                AddMeshCollider(instance);
                EnsureFolder(Path.GetDirectoryName(path));
                return PrefabUtility.SaveAsPrefabAsset(instance, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void AddMeshCollider(GameObject instance)
        {
            MeshFilter filter = instance.GetComponentInChildren<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
        }

        private static void AddSocket(Transform parent, string name, Vector3 localPosition, SocketType type, Direction facing)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var socket = go.AddComponent<SocketPoint>();
            socket.Type = type;
            socket.Facing = facing;
        }

        private static float SocketHeightFor(CastleZone zone)
        {
            var defaults = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(DefaultRegistryPath);
            if (defaults != null)
            {
                foreach (CastleRoomModuleData module in defaults.GetModulesForZone(zone))
                {
                    SocketPoint socket = module.Prefab != null ? module.Prefab.GetComponentInChildren<SocketPoint>(true) : null;
                    if (socket != null)
                        return socket.transform.localPosition.y;
                }
            }
            return 1.5f;
        }

        // ---------------------------------------------------------------------------------------
        // Plumbing
        // ---------------------------------------------------------------------------------------

        private static Dictionary<string, List<Vector3>> ReadLootAnchors(StringBuilder report)
        {
            var rooms = new Dictionary<string, List<Vector3>>();
            if (!File.Exists(LootAnchorPath))
            {
                report.AppendLine($"  no loot anchors at {LootAnchorPath}; era rooms get none");
                return rooms;
            }

            // {"rooms": {"Key": [[x, y, z], ...], ...}, "space": "..."}
            string json = File.ReadAllText(LootAnchorPath);
            int at = json.IndexOf("\"rooms\"", StringComparison.Ordinal);
            int i = json.IndexOf('{', at) + 1;
            while (i > 0 && i < json.Length)
            {
                int keyStart = json.IndexOf('"', i);
                int close = json.IndexOf('}', i);
                if (keyStart < 0 || (close >= 0 && close < keyStart))
                    break;
                int keyEnd = json.IndexOf('"', keyStart + 1);
                string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1);
                int listStart = json.IndexOf('[', keyEnd);
                int depth = 0, j = listStart;
                for (; j < json.Length; j++)
                {
                    if (json[j] == '[') depth++;
                    else if (json[j] == ']' && --depth == 0) break;
                }
                var points = new List<Vector3>();
                foreach (string triple in json.Substring(listStart + 1, j - listStart - 1).Split(']'))
                {
                    string[] parts = triple.Replace("[", "").Trim().Trim(',').Split(',');
                    if (parts.Length != 3)
                        continue;
                    points.Add(new Vector3(
                        float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture),
                        float.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture)));
                }
                rooms[key] = points;
                i = j + 1;
            }
            return rooms;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;
            EnsureFolder(Path.GetDirectoryName(path));
            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
