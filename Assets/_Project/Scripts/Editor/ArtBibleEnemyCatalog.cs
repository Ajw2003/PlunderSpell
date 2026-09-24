using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RogueAi.Castle;
using RogueAi.Inventory;
using UnityEngine;

namespace RogueAi.EditorTools
{
    /// <summary>The four jobs the art bible gives every Age (docs/art/data/*.json, "role").</summary>
    public enum ArtBibleRole
    {
        Patrol,
        Ranged,
        Heavy,
        Special
    }

    /// <summary>How an enemy hurts you, mapped onto the existing <c>CastleGuard</c> modes.</summary>
    public enum ArtBibleAttack
    {
        /// <summary>Strikes at arm's reach.</summary>
        Melee,

        /// <summary>Fires or throws the Bolt projectile at sight range.</summary>
        Projectile
    }

    /// <summary>A guard's numbers: the columns of the role table in the plan.</summary>
    public readonly struct ArtBibleTuning
    {
        public readonly float PatrolSpeed;
        public readonly float ChaseSpeed;
        public readonly float SightRange;
        public readonly float MaxHealth;
        public readonly ArtBibleAttack Attack;

        public ArtBibleTuning(float patrolSpeed, float chaseSpeed, float sightRange, float maxHealth,
            ArtBibleAttack attack)
        {
            PatrolSpeed = patrolSpeed;
            ChaseSpeed = chaseSpeed;
            SightRange = sightRange;
            MaxHealth = maxHealth;
            Attack = attack;
        }
    }

    /// <summary>One art-bible enemy, joined from its Age's JSON and the ArtForge manifest.</summary>
    public sealed class ArtBibleEnemySpec
    {
        /// <summary>The art bible's slug, e.g. <c>lantern-warden</c>.</summary>
        public string Slug;

        /// <summary>The model's name, e.g. <c>LanternWarden</c>: the FBX, the prefab and the roster id.</summary>
        public string Name;

        /// <summary>Display name, e.g. "Lantern Warden".</summary>
        public string Title;

        /// <summary>The Age's slug: bronze, high, late or powder.</summary>
        public string AgeSlug;

        public HistoricalEra Era;
        public ArtBibleRole Role;
        public CastleZone[] Zones = Array.Empty<CastleZone>();

        /// <summary>Standing height without props, in metres (JSON <c>height_m</c>).</summary>
        public float BodyHeight;

        /// <summary>Body height ArtForge measured on the built model (manifest <c>height_body_m</c>).</summary>
        public float MeasuredBodyHeight;

        /// <summary>Everything the model draws, props included (manifest <c>stats.height</c>).</summary>
        public float HeightWithProps;

        /// <summary>Widest horizontal extent, props included (manifest <c>stats.width</c>).</summary>
        public float Width;

        /// <summary>The FBX, as a project-relative asset path (manifest <c>files.fbx</c>).</summary>
        public string FbxPath;

        /// <summary>True for the Unity Humanoid rigs; false for the hound's Generic rig.</summary>
        public bool IsHumanoid = true;

        /// <summary>True when a material family glows (manifest <c>families.*.emit</c>).</summary>
        public bool Emissive;

        public ArtBibleTuning Tuning;

        /// <summary>Bones that get a <c>Socket.</c> child: hands and the props they carry.</summary>
        public string[] SocketBones = Array.Empty<string>();

        /// <summary>The bone the small warm light hangs from, or null for an enemy without one.</summary>
        public string LightBone;

        /// <summary>The Age's folder name under Assets/Models/ArtBible/Enemies and the prefab tree.</summary>
        public string AgeFolder => ArtBibleEnemyCatalog.AgeFolderFor(AgeSlug);

        /// <summary>Where the forge saves this enemy's prefab variant.</summary>
        public string PrefabPath => $"{ArtBibleEnemyCatalog.PrefabRoot}/{AgeFolder}/{Name}.prefab";

        /// <summary>The lowest archway among the zones this enemy is posted to.</summary>
        public float ArchwayClearance =>
            Zones.Length == 0 ? ArtBibleEnemyCatalog.ArchwayHeight(CastleZone.Crypt)
                : Zones.Min(ArtBibleEnemyCatalog.ArchwayHeight);

        /// <summary>The NavMesh agent's height: the body, but never more than the lowest archway.</summary>
        public float AgentHeight => Mathf.Min(BodyHeight, ArchwayClearance);

        /// <summary>True when a prop rises above an archway this enemy must walk through.</summary>
        public bool NeedsArchwayDuck => HeightWithProps > ArchwayClearance;

        /// <summary>Roster weight, from the role (patrol 10, ranged 7, heavy 4, special 3).</summary>
        public int RosterWeight => ArtBibleEnemyCatalog.WeightFor(Role);

        public override string ToString() => $"{Name} ({Era}, {Role})";
    }

    /// <summary>One line of the enemy roster the forge will write.</summary>
    public readonly struct ArtBiblePosting
    {
        public readonly string EnemyId;
        public readonly HistoricalEra Era;
        public readonly CastleZone Zone;
        public readonly int Weight;

        public ArtBiblePosting(string enemyId, HistoricalEra era, CastleZone zone, int weight)
        {
            EnemyId = enemyId;
            Era = era;
            Zone = zone;
            Weight = weight;
        }
    }

    /// <summary>
    /// Reads the sixteen art-bible enemies from the art bible's own data (docs/art/data/*.json) and
    /// the ArtForge manifest, so height, zones and role come from one source instead of another
    /// hand-typed table. Pure: no AssetDatabase, so the parsing and the numbers derived from it are
    /// tested headlessly. The Unity half is <see cref="ArtBibleEnemyForge"/>.
    /// See docs/plans/artbible-enemies-in-engine.md (E1, E2).
    /// </summary>
    public static class ArtBibleEnemyCatalog
    {
        public const string DataDirectory = "docs/art/data";
        public const string ManifestPath = "Assets/Models/ArtBible/artforge_manifest.json";
        public const string ModelRoot = "Assets/Models/ArtBible";
        public const string PrefabRoot = "Assets/_Project/Prefabs/Enemies/ArtBible";

        /// <summary>The Ages, oldest first, by their art-bible slug.</summary>
        public static readonly string[] AgeSlugs = { "bronze", "high", "late", "powder" };

        /// <summary>
        /// Models rigged Generic rather than Humanoid. Kept as a constant for the importer, which
        /// must not read docs at import time; <c>ArtBibleEnemyCatalogTests</c> checks it against the
        /// JSON's <c>rig.skeleton</c> so the two cannot drift.
        /// </summary>
        public static readonly string[] GenericRigModels = { "AlauntWarHound" };

        /// <summary>
        /// Models with a glowing material family. Constant for the same reason as
        /// <see cref="GenericRigModels"/>, and checked against the manifest's <c>emit</c> fields.
        /// </summary>
        public static readonly string[] EmissiveModels =
        {
            "KeeperOfTheFlame", "LanternWarden", "Handgunner", "Musketeer", "PalaceGuard", "Petardier"
        };

        /// <summary>Roster weights by role (the plan's E2).</summary>
        public static int WeightFor(ArtBibleRole role)
        {
            switch (role)
            {
                case ArtBibleRole.Patrol: return 10;
                case ArtBibleRole.Ranged: return 7;
                case ArtBibleRole.Heavy: return 4;
                default: return 3;
            }
        }

        /// <summary>The role table (the plan's E1). Specials are per enemy: see <see cref="TuningFor(ArtBibleEnemySpec)"/>.</summary>
        public static ArtBibleTuning TuningFor(ArtBibleRole role)
        {
            switch (role)
            {
                case ArtBibleRole.Patrol: return new ArtBibleTuning(2.0f, 4.2f, 14f, 80f, ArtBibleAttack.Melee);
                case ArtBibleRole.Ranged: return new ArtBibleTuning(1.9f, 3.6f, 20f, 60f, ArtBibleAttack.Projectile);
                case ArtBibleRole.Heavy: return new ArtBibleTuning(1.5f, 3.2f, 13f, 180f, ArtBibleAttack.Melee);
                default: return new ArtBibleTuning(1.8f, 3.8f, 14f, 80f, ArtBibleAttack.Melee);
            }
        }

        // The four specials, each started from the nearest existing guard mode (plan, E1): the hound
        // is a melee chaser (the old WarHound's numbers), the Keeper and the Petardier throw, the
        // Pavisier is a slow melee shield-bearer. Any enemy can be overridden here by name.
        private static readonly Dictionary<string, ArtBibleTuning> s_overrides =
            new Dictionary<string, ArtBibleTuning>
            {
                ["AlauntWarHound"] = new ArtBibleTuning(2.8f, 6.5f, 12f, 55f, ArtBibleAttack.Melee),
                ["KeeperOfTheFlame"] = new ArtBibleTuning(1.6f, 3.4f, 16f, 70f, ArtBibleAttack.Projectile),
                ["Pavisier"] = new ArtBibleTuning(1.6f, 3.4f, 14f, 140f, ArtBibleAttack.Melee),
                ["Petardier"] = new ArtBibleTuning(1.8f, 3.8f, 16f, 70f, ArtBibleAttack.Projectile),
            };

        /// <summary>The enemy's tuning: its override if it has one, else its role's row.</summary>
        public static ArtBibleTuning TuningFor(ArtBibleEnemySpec spec) =>
            s_overrides.TryGetValue(spec.Name, out ArtBibleTuning tuning) ? tuning : TuningFor(spec.Role);

        // Prop bones that become sockets, and the bone a warm light hangs from, per model. The bone
        // names are ArtForge's (read off each model's glTF node list). Hands are added for every
        // Humanoid on top of these.
        private static readonly Dictionary<string, (string[] Props, string Light)> s_props =
            new Dictionary<string, (string[], string)>
            {
                ["PalaceLevy"] = (new[] { "SpearRoot", "ShieldRoot" }, null),
                ["WallSlinger"] = (new[] { "Sling1", "Pouch.R" }, null),
                ["DendraChampion"] = (new[] { "Sword", "Helmet" }, null),
                ["KeeperOfTheFlame"] = (new[] { "Censer3", "Yoke" }, "Censer3"),
                ["LanternWarden"] = (new[] { "Glaive", "LanternBody" }, "LanternBody"),
                ["CastleCrossbowman"] = (new[] { "Crossbow", "Quiver" }, null),
                ["HouseholdKnight"] = (new[] { "Sword", "Shield", "Helm" }, null),
                ["AlauntWarHound"] = (new[] { "Jaw", "CollarRing" }, null),
                ["SalletHalberdier"] = (new[] { "Halberd", "Baselard" }, null),
                ["Handgunner"] = (new[] { "Gun", "MatchCord" }, "MatchCord"),
                ["GothicManAtArms"] = (new[] { "Poleaxe" }, null),
                ["Pavisier"] = (new[] { "Pavise", "Falchion" }, null),
                ["PalaceGuard"] = (new[] { "Partisan", "Lantern" }, "Lantern"),
                ["Musketeer"] = (new[] { "Musket", "Match", "Rest" }, "Match"),
                ["Cuirassier"] = (new[] { "Helm" }, null),
                ["Petardier"] = (new[] { "Grenado.1", "Linstock", "Madrier" }, "Grenado.1"),
            };

        /// <summary>
        /// Archway height per zone, from docs/systems/scale.md ("Archways"; the CurtainWall gate from
        /// the art bible's Lion Gate, 3.74 m). The geometry is the source; this mirrors it.
        /// </summary>
        public static float ArchwayHeight(CastleZone zone)
        {
            switch (zone)
            {
                case CastleZone.Crypt: return 2.16f;
                case CastleZone.OuterBailey: return 2.59f;
                case CastleZone.InnerWard: return 2.88f;
                case CastleZone.Keep: return 3.31f;
                default: return 3.74f; // CurtainWall
            }
        }

        public static HistoricalEra EraFor(string ageSlug)
        {
            switch (ageSlug)
            {
                case "bronze": return HistoricalEra.BronzeAge;
                case "high": return HistoricalEra.HighMedieval;
                case "late": return HistoricalEra.LateMedieval;
                case "powder": return HistoricalEra.AgeOfPowder;
                default: throw new ArgumentException($"Unknown art-bible Age '{ageSlug}'.", nameof(ageSlug));
            }
        }

        public static string AgeFolderFor(string ageSlug) =>
            string.IsNullOrEmpty(ageSlug) ? "Unknown" : char.ToUpperInvariant(ageSlug[0]) + ageSlug.Substring(1);

        /// <summary>Reads every Age's JSON and the manifest from a project root on disk.</summary>
        public static List<ArtBibleEnemySpec> Load(string projectRoot, List<string> problems)
        {
            var ages = new List<(string, string)>();
            foreach (string age in AgeSlugs)
            {
                string path = Path.Combine(projectRoot, DataDirectory, age + ".json");
                if (File.Exists(path))
                    ages.Add((age, File.ReadAllText(path)));
                else
                    problems.Add($"missing art-bible data file {DataDirectory}/{age}.json");
            }

            string manifestPath = Path.Combine(projectRoot, ManifestPath);
            if (!File.Exists(manifestPath))
            {
                problems.Add($"missing ArtForge manifest {ManifestPath}");
                return new List<ArtBibleEnemySpec>();
            }

            return Parse(ages, File.ReadAllText(manifestPath), problems);
        }

        /// <summary>
        /// Joins each Age's <c>enemies</c> with the manifest's <c>enemies/&lt;age&gt;/&lt;slug&gt;</c>
        /// entry. Every problem (an unknown role or zone, a model the manifest does not have, a
        /// failed build) is added to <paramref name="problems"/> and that enemy is skipped; nothing
        /// is guessed.
        /// </summary>
        public static List<ArtBibleEnemySpec> Parse(IEnumerable<(string AgeSlug, string Json)> ages,
            string manifestJson, List<string> problems)
        {
            var manifest = new Dictionary<string, object>();
            foreach (object asset in ArtBibleJson.GetList(ArtBibleJson.Parse(manifestJson), "assets"))
            {
                string key = ArtBibleJson.GetString(asset, "key");
                if (key != null)
                    manifest[key] = asset;
            }

            var specs = new List<ArtBibleEnemySpec>();
            foreach ((string ageSlug, string json) in ages)
            {
                HistoricalEra era = EraFor(ageSlug);
                foreach (object enemy in ArtBibleJson.GetList(ArtBibleJson.Parse(json), "enemies"))
                {
                    ArtBibleEnemySpec spec = ParseEnemy(ageSlug, era, enemy, manifest, problems);
                    if (spec != null)
                        specs.Add(spec);
                }
            }
            return specs;
        }

        private static ArtBibleEnemySpec ParseEnemy(string ageSlug, HistoricalEra era, object enemy,
            Dictionary<string, object> manifest, List<string> problems)
        {
            string slug = ArtBibleJson.GetString(enemy, "slug");
            string where = $"{ageSlug}/{slug ?? "?"}";

            if (!TryParseRole(ArtBibleJson.GetString(enemy, "role"), out ArtBibleRole role))
            {
                problems.Add($"{where}: unknown role '{ArtBibleJson.GetString(enemy, "role")}'");
                return null;
            }

            var zones = new List<CastleZone>();
            foreach (object zone in ArtBibleJson.GetList(enemy, "zones"))
            {
                if (zone is string name && Enum.TryParse(name, false, out CastleZone parsed))
                    zones.Add(parsed);
                else
                    problems.Add($"{where}: unknown zone '{zone}'");
            }
            if (zones.Count == 0)
            {
                problems.Add($"{where}: posted to no zone");
                return null;
            }

            float? height = ArtBibleJson.GetFloat(enemy, "height_m");
            if (!height.HasValue || height.Value <= 0f)
            {
                problems.Add($"{where}: no height_m");
                return null;
            }

            if (!manifest.TryGetValue($"enemies/{ageSlug}/{slug}", out object asset))
            {
                problems.Add($"{where}: not in {ManifestPath}; build it with Tools/ArtForge first");
                return null;
            }
            if (ArtBibleJson.Get(asset, "passed") is bool passed && !passed)
                problems.Add($"{where}: the manifest records a failed ArtForge build; forging it anyway");

            object stats = ArtBibleJson.Get(asset, "stats");
            string skeleton = ArtBibleJson.GetString(ArtBibleJson.Get(enemy, "rig"), "skeleton")
                              ?? ArtBibleJson.GetString(enemy, "rig") ?? string.Empty;

            var spec = new ArtBibleEnemySpec
            {
                Slug = slug,
                Name = ArtBibleJson.GetString(asset, "name"),
                Title = ArtBibleJson.GetString(enemy, "name"),
                AgeSlug = ageSlug,
                Era = era,
                Role = role,
                Zones = zones.ToArray(),
                BodyHeight = height.Value,
                MeasuredBodyHeight = ArtBibleJson.GetFloat(stats, "height_body_m")
                                     ?? ArtBibleJson.GetFloat(stats, "height") ?? height.Value,
                HeightWithProps = ArtBibleJson.GetFloat(stats, "height") ?? height.Value,
                Width = ArtBibleJson.GetFloat(stats, "width") ?? 0.7f,
                FbxPath = ArtBibleJson.GetString(ArtBibleJson.Get(asset, "files"), "fbx"),
                IsHumanoid = skeleton.IndexOf("Generic", StringComparison.OrdinalIgnoreCase) < 0,
                Emissive = HasEmissiveFamily(asset),
            };

            if (string.IsNullOrEmpty(spec.Name) || string.IsNullOrEmpty(spec.FbxPath))
            {
                problems.Add($"{where}: the manifest entry has no name or no fbx path");
                return null;
            }

            spec.Tuning = TuningFor(spec);
            if (s_props.TryGetValue(spec.Name, out (string[] Props, string Light) props))
            {
                spec.SocketBones = spec.IsHumanoid
                    ? new[] { "Hand.R", "Hand.L" }.Concat(props.Props).ToArray()
                    : props.Props;
                spec.LightBone = spec.Emissive ? props.Light : null;
            }
            else
            {
                problems.Add($"{where}: no prop-socket row for {spec.Name}; only the hands get sockets");
                spec.SocketBones = spec.IsHumanoid ? new[] { "Hand.R", "Hand.L" } : Array.Empty<string>();
            }

            return spec;
        }

        private static bool HasEmissiveFamily(object asset)
        {
            if (!(ArtBibleJson.Get(asset, "families") is Dictionary<string, object> families))
                return false;
            foreach (object family in families.Values)
            {
                if (ArtBibleJson.Get(family, "emit") != null)
                    return true;
            }
            return false;
        }

        public static bool TryParseRole(string text, out ArtBibleRole role)
        {
            switch (text)
            {
                case "patrol": role = ArtBibleRole.Patrol; return true;
                case "ranged": role = ArtBibleRole.Ranged; return true;
                case "heavy": role = ArtBibleRole.Heavy; return true;
                case "special": role = ArtBibleRole.Special; return true;
                default: role = ArtBibleRole.Special; return false;
            }
        }

        /// <summary>One roster posting per zone each enemy lists, weighted by its role.</summary>
        public static List<ArtBiblePosting> Postings(IEnumerable<ArtBibleEnemySpec> specs)
        {
            var postings = new List<ArtBiblePosting>();
            foreach (ArtBibleEnemySpec spec in specs)
            {
                foreach (CastleZone zone in spec.Zones)
                    postings.Add(new ArtBiblePosting(spec.Name, spec.Era, zone, spec.RosterWeight));
            }
            return postings;
        }
    }
}
