using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using RogueAi.Castle;
using RogueAi.EditorTools;
using RogueAi.Inventory;

namespace RogueAi.Tests.Editor
{
    /// <summary>
    /// The art-bible enemies' numbers, read from docs/art/data/*.json and the ArtForge manifest
    /// (docs/plans/artbible-enemies-in-engine.md, E0–E2). Pure: runs without the importer or any
    /// forged prefab, so a JSON edit that breaks the forge fails here first.
    /// </summary>
    public class ArtBibleEnemyCatalogTests
    {
        private static string ProjectRoot()
        {
            string root = ArtBibleEnemyCatalog.FindProjectRoot();
            Assert.IsNotNull(root, $"No {ArtBibleEnemyCatalog.ManifestPath} above the test's data path.");
            return root;
        }

        private static List<ArtBibleEnemySpec> LoadAll(out List<string> problems)
        {
            problems = new List<string>();
            return ArtBibleEnemyCatalog.Load(ProjectRoot(), problems);
        }

        // --- Parsing -------------------------------------------------------------------------------

        [Test]
        public void Test_TheJsonReaderReadsNestingEscapesAndNumbers()
        {
            object tree = ArtBibleJson.Parse(
                "{\"a\": [1, -2.5e1, {\"b\": \"x\\\"y\\u00d7\"}], \"t\": true, \"n\": null}");

            List<object> a = ArtBibleJson.GetList(tree, "a");
            Assert.AreEqual(3, a.Count);
            Assert.AreEqual(1.0, a[0]);
            Assert.AreEqual(-25.0, a[1]);
            Assert.AreEqual("x\"y×", ArtBibleJson.GetString(a[2], "b"));
            Assert.AreEqual(true, ArtBibleJson.Get(tree, "t"));
            Assert.IsNull(ArtBibleJson.Get(tree, "n"));
        }

        [Test]
        public void Test_MalformedJsonThrowsRatherThanReturningHalfATree()
        {
            Assert.Throws<FormatException>(() => ArtBibleJson.Parse("{\"a\": [1, 2}"));
            Assert.Throws<FormatException>(() => ArtBibleJson.Parse("{\"a\": 1} trailing"));
        }

        [Test]
        public void Test_ASyntheticEnemyBecomesASpec()
        {
            const string age = "{\"enemies\": [{\"slug\": \"test-guard\", \"name\": \"Test Guard\", " +
                               "\"role\": \"ranged\", \"zones\": [\"CurtainWall\", \"Crypt\"], \"height_m\": 1.75, " +
                               "\"rig\": {\"skeleton\": \"Humanoid (Unity Mecanim)\"}}]}";
            const string manifest = "{\"assets\": [{\"key\": \"enemies/late/test-guard\", \"name\": \"TestGuard\", " +
                                    "\"passed\": true, \"stats\": {\"height\": 2.3, \"height_body_m\": 1.76, " +
                                    "\"width\": 0.8}, \"families\": {\"x\": {\"emit\": \"#FF0000\"}}, " +
                                    "\"files\": {\"fbx\": \"Assets/Models/ArtBible/Enemies/Late/TestGuard/TestGuard.fbx\"}}]}";
            var problems = new List<string>();

            List<ArtBibleEnemySpec> specs = ArtBibleEnemyCatalog.Parse(new[] { ("late", age) }, manifest, problems);

            Assert.AreEqual(1, specs.Count, string.Join("\n", problems));
            ArtBibleEnemySpec spec = specs[0];
            Assert.AreEqual("TestGuard", spec.Name);
            Assert.AreEqual(HistoricalEra.LateMedieval, spec.Era);
            Assert.AreEqual(ArtBibleRole.Ranged, spec.Role);
            CollectionAssert.AreEqual(new[] { CastleZone.CurtainWall, CastleZone.Crypt }, spec.Zones);
            Assert.AreEqual(1.75f, spec.BodyHeight, 1e-4f);
            Assert.AreEqual(2.3f, spec.HeightWithProps, 1e-4f);
            Assert.IsTrue(spec.IsHumanoid);
            Assert.IsTrue(spec.Emissive);
            Assert.AreEqual(ArtBibleAttack.Projectile, spec.Tuning.Attack);
            Assert.AreEqual(7, spec.RosterWeight);
            Assert.AreEqual("Assets/_Project/Prefabs/Enemies/ArtBible/Late/TestGuard.prefab", spec.PrefabPath);
            Assert.AreEqual(2.16f, spec.ArchwayClearance, 1e-4f, "The Crypt's archway is the lowest it passes.");
            Assert.AreEqual(1.75f, spec.AgentHeight, 1e-4f);
            Assert.IsTrue(spec.NeedsArchwayDuck, "2.3 m of props will not pass a 2.16 m archway upright.");
        }

        [Test]
        public void Test_ProblemsAreReportedAndTheEnemySkipped()
        {
            const string age = "{\"enemies\": [" +
                               "{\"slug\": \"bad-role\", \"role\": \"wizard\", \"zones\": [\"Keep\"], \"height_m\": 1.8}," +
                               "{\"slug\": \"unbuilt\", \"role\": \"patrol\", \"zones\": [\"Keep\"], \"height_m\": 1.8}]}";
            var problems = new List<string>();

            List<ArtBibleEnemySpec> specs = ArtBibleEnemyCatalog.Parse(new[] { ("bronze", age) },
                "{\"assets\": []}", problems);

            Assert.IsEmpty(specs, "Nothing is guessed: both enemies are skipped.");
            Assert.IsTrue(problems.Any(p => p.Contains("unknown role 'wizard'")), string.Join("\n", problems));
            Assert.IsTrue(problems.Any(p => p.Contains("bronze/unbuilt") && p.Contains("not in")),
                string.Join("\n", problems));
        }

        // --- The real data ------------------------------------------------------------------------

        [Test]
        public void Test_TheArtBibleHasFourEnemiesPerAgeOneOfEachRole()
        {
            List<ArtBibleEnemySpec> specs = LoadAll(out List<string> problems);

            Assert.IsEmpty(problems, string.Join("\n", problems));
            Assert.AreEqual(16, specs.Count);
            foreach (HistoricalEra era in Enum.GetValues(typeof(HistoricalEra)))
            {
                ArtBibleRole[] roles = specs.Where(s => s.Era == era).Select(s => s.Role).OrderBy(r => r).ToArray();
                CollectionAssert.AreEqual(
                    new[] { ArtBibleRole.Patrol, ArtBibleRole.Ranged, ArtBibleRole.Heavy, ArtBibleRole.Special },
                    roles, $"{era} must have one enemy of each role.");
            }
        }

        [Test]
        public void Test_RoleWeightsAreThePlansTable()
        {
            Assert.AreEqual(10, ArtBibleEnemyCatalog.WeightFor(ArtBibleRole.Patrol));
            Assert.AreEqual(7, ArtBibleEnemyCatalog.WeightFor(ArtBibleRole.Ranged));
            Assert.AreEqual(4, ArtBibleEnemyCatalog.WeightFor(ArtBibleRole.Heavy));
            Assert.AreEqual(3, ArtBibleEnemyCatalog.WeightFor(ArtBibleRole.Special));

            foreach (ArtBiblePosting posting in ArtBibleEnemyCatalog.Postings(LoadAll(out _)))
            {
                Assert.Contains(posting.Weight, new[] { 10, 7, 4, 3 }, $"{posting.EnemyId} in {posting.Zone}");
            }
        }

        [Test]
        public void Test_RoleTuningIsThePlansTable()
        {
            ArtBibleTuning patrol = ArtBibleEnemyCatalog.TuningFor(ArtBibleRole.Patrol);
            Assert.AreEqual((2.0f, 4.2f, 14f, 80f, ArtBibleAttack.Melee),
                (patrol.PatrolSpeed, patrol.ChaseSpeed, patrol.SightRange, patrol.MaxHealth, patrol.Attack));
            ArtBibleTuning ranged = ArtBibleEnemyCatalog.TuningFor(ArtBibleRole.Ranged);
            Assert.AreEqual((1.9f, 3.6f, 20f, 60f, ArtBibleAttack.Projectile),
                (ranged.PatrolSpeed, ranged.ChaseSpeed, ranged.SightRange, ranged.MaxHealth, ranged.Attack));
            ArtBibleTuning heavy = ArtBibleEnemyCatalog.TuningFor(ArtBibleRole.Heavy);
            Assert.AreEqual((1.5f, 3.2f, 13f, 180f, ArtBibleAttack.Melee),
                (heavy.PatrolSpeed, heavy.ChaseSpeed, heavy.SightRange, heavy.MaxHealth, heavy.Attack));

            Dictionary<string, ArtBibleEnemySpec> byName = LoadAll(out _).ToDictionary(s => s.Name);
            Assert.AreEqual(ArtBibleAttack.Melee, byName["AlauntWarHound"].Tuning.Attack, "The hound is a melee chaser.");
            Assert.AreEqual(ArtBibleAttack.Projectile, byName["KeeperOfTheFlame"].Tuning.Attack, "The Keeper throws.");
            Assert.AreEqual(ArtBibleAttack.Projectile, byName["Petardier"].Tuning.Attack, "The Petardier throws.");
            Assert.AreEqual(ArtBibleAttack.Melee, byName["Pavisier"].Tuning.Attack, "The Pavisier is a shield-bearer.");
        }

        [Test]
        public void Test_EveryAgeGarrisonsEveryZoneOutsideTheCrypt()
        {
            List<ArtBiblePosting> postings = ArtBibleEnemyCatalog.Postings(LoadAll(out _));

            foreach (HistoricalEra era in Enum.GetValues(typeof(HistoricalEra)))
            {
                foreach (CastleZone zone in Enum.GetValues(typeof(CastleZone)))
                {
                    // The Crypt is garrisoned in every Age by the supernatural enemies
                    // (EnemyPrefabForge, AnyEra); the art bible only posts the Keeper of the Flame there.
                    if (zone == CastleZone.Crypt)
                        continue;
                    Assert.IsTrue(postings.Any(p => p.Era == era && p.Zone == zone),
                        $"No {era} art-bible enemy garrisons {zone}: that Age would fall back to other Ages there.");
                }
            }
        }

        [Test]
        public void Test_ArtForgeBuiltEveryBodyWithinFivePercentOfTheArtBible()
        {
            foreach (ArtBibleEnemySpec spec in LoadAll(out _))
            {
                Assert.AreEqual(spec.BodyHeight, spec.MeasuredBodyHeight, spec.BodyHeight * 0.05f,
                    $"{spec.Name}: ArtForge measured {spec.MeasuredBodyHeight:F3} m, the art bible says {spec.BodyHeight:F2} m.");
            }
        }

        [Test]
        public void Test_EveryAgentFitsTheArchwaysOfItsZones()
        {
            foreach (ArtBibleEnemySpec spec in LoadAll(out _))
            {
                foreach (CastleZone zone in spec.Zones)
                {
                    Assert.LessOrEqual(spec.AgentHeight, ArtBibleEnemyCatalog.ArchwayHeight(zone),
                        $"{spec.Name}'s agent would not path through {zone}'s archway.");
                }
            }
        }

        [Test]
        public void Test_ThePalaceGuardsPartisanNeedsTheArchwayDuck()
        {
            Dictionary<string, ArtBibleEnemySpec> byName = LoadAll(out _).ToDictionary(s => s.Name);
            Assert.IsTrue(byName["PalaceGuard"].NeedsArchwayDuck,
                "2.62 m with the partisan, posted to the Outer Bailey's 2.59 m archway.");
            Assert.IsFalse(byName["Handgunner"].NeedsArchwayDuck);
        }

        // --- Constants that mirror the data, checked against it -----------------------------------

        [Test]
        public void Test_TheGenericRigListMatchesTheArtBible()
        {
            string[] generic = LoadAll(out _).Where(s => !s.IsHumanoid).Select(s => s.Name).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(ArtBibleEnemyCatalog.GenericRigModels.OrderBy(n => n).ToArray(), generic);
        }

        [Test]
        public void Test_TheEmissiveListMatchesTheManifest()
        {
            string[] emissive = LoadAll(out _).Where(s => s.Emissive).Select(s => s.Name).OrderBy(n => n).ToArray();
            CollectionAssert.AreEqual(ArtBibleEnemyCatalog.EmissiveModels.OrderBy(n => n).ToArray(), emissive);
        }

        [Test]
        public void Test_EveryEmissiveEnemyHasALightBone()
        {
            foreach (ArtBibleEnemySpec spec in LoadAll(out _).Where(s => s.Emissive))
            {
                Assert.IsFalse(string.IsNullOrEmpty(spec.LightBone), $"{spec.Name} glows but has no light bone.");
                CollectionAssert.Contains(spec.SocketBones, spec.LightBone,
                    $"{spec.Name}'s light hangs from a prop that should also be a socket.");
            }
        }

        [Test]
        public void Test_TheBoneMapIsArtForgesUnityHumanoidMap()
        {
            string figures = File.ReadAllText(Path.Combine(ProjectRoot(), "Tools/ArtForge/art_forge/figures.py"));
            Match block = Regex.Match(figures, @"UNITY_HUMANOID\s*=\s*\{(?<body>[^}]*)\}");
            Assert.IsTrue(block.Success, "UNITY_HUMANOID not found in figures.py");

            var python = new Dictionary<string, string>();
            foreach (Match pair in Regex.Matches(block.Groups["body"].Value, "\"(?<k>\\w+)\"\\s*:\\s*\"(?<v>[\\w.]+)\""))
                python[pair.Groups["k"].Value] = pair.Groups["v"].Value;

            CollectionAssert.AreEquivalent(python, ArtBibleModelImporter.HumanBoneMap,
                "ArtBibleModelImporter.HumanBoneMap must be a copy of UNITY_HUMANOID.");
        }

        [Test]
        public void Test_TheBoneMapCoversEveryRequiredHumanoidBone()
        {
            string[] required =
            {
                "Hips", "Spine", "Head", "LeftUpperArm", "LeftLowerArm", "LeftHand", "RightUpperArm",
                "RightLowerArm", "RightHand", "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "RightUpperLeg",
                "RightLowerLeg", "RightFoot"
            };
            string[] mapped = ArtBibleModelImporter.BuildHumanBones().Select(b => b.humanName).ToArray();
            CollectionAssert.IsSubsetOf(required, mapped);
        }

        [Test]
        public void Test_EmissionStrengthMatchesEnemyForge()
        {
            string materials = File.ReadAllText(Path.Combine(ProjectRoot(), "Tools/EnemyForge/enemy_forge/materials.py"));
            Match strength = Regex.Match(materials, @"^EMISSION_STRENGTH\s*=\s*(?<v>[\d.]+)", RegexOptions.Multiline);
            Assert.IsTrue(strength.Success, "EMISSION_STRENGTH not found in materials.py");
            Assert.AreEqual(float.Parse(strength.Groups["v"].Value, System.Globalization.CultureInfo.InvariantCulture),
                ArtBibleModelImporter.EmissionStrength);
        }

        [Test]
        public void Test_ModelPathsAreClassifiedByRig()
        {
            Assert.AreEqual(ArtBibleModelImporter.Kind.HumanoidEnemy,
                ArtBibleModelImporter.KindOf("Assets/Models/ArtBible/Enemies/High/LanternWarden/LanternWarden.fbx"));
            Assert.AreEqual(ArtBibleModelImporter.Kind.GenericEnemy,
                ArtBibleModelImporter.KindOf("Assets/Models/ArtBible/Enemies/High/AlauntWarHound/AlauntWarHound.fbx"));
            Assert.AreEqual(ArtBibleModelImporter.Kind.Item,
                ArtBibleModelImporter.KindOf("Assets/Models/ArtBible/Items/Bronze/OxhideIngot/OxhideIngot.fbx"));
            Assert.IsNull(ArtBibleModelImporter.KindOf("Assets/Models/Enemies/Watchman/Watchman.fbx"));
            Assert.IsNull(ArtBibleModelImporter.KindOf("Assets/Models/ArtBible/Enemies/High/LanternWarden/LanternWarden.blend"));
        }

        [Test]
        public void Test_DataMapsImportLinearAndColourMapsSrgb()
        {
            const string dir = "Assets/Models/ArtBible/Enemies/High/LanternWarden/Textures/";
            Assert.IsTrue(ArtBibleModelImporter.IsLinearTexture(dir + "LanternWarden_ORM.png"));
            Assert.IsTrue(ArtBibleModelImporter.IsLinearTexture(dir + "LanternWarden_MetallicGloss.png"));
            Assert.IsTrue(ArtBibleModelImporter.IsLinearTexture(dir + "LanternWarden_Roughness.png"));
            Assert.IsFalse(ArtBibleModelImporter.IsLinearTexture(dir + "LanternWarden_BaseMap.png"));
            Assert.IsFalse(ArtBibleModelImporter.IsLinearTexture(dir + "LanternWarden_Emission.png"));
        }
    }
}
