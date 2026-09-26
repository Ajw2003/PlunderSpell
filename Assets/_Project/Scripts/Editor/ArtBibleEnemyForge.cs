using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plunderspell.Castle;
using Plunderspell.Guards;
using Plunderspell.Raid;
using Plunderspell.Status;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Plunderspell.EditorTools
{
    // doc-ref d1ef docs/4-systems/raid-scene-assembly.md
    /// <summary>
    /// Authors one prefab variant per art-bible enemy from <see cref="ArtBibleEnemyCatalog"/> and
    /// posts them to the <see cref="EnemyRoster"/>. The sibling of <see cref="EnemyPrefabForge"/>.
    /// </summary>
    public static class ArtBibleEnemyForge
    {
        private const string RosterPath = "Assets/_Project/Data/Enemies/EnemyRoster.asset";
        private const string BoltPath = "Assets/_Project/Prefabs/Projectiles/Bolt.prefab";

        /// <summary>The household four the art-bible set replaces in the roster (plan, decision 1).</summary>
        public static readonly string[] ReplacedEnemies = { "Watchman", "ManAtArms", "Sergeant", "WarHound" };

        /// <summary>Prefix of the empty children that mark where a prop or a hand is.</summary>
        public const string SocketPrefix = "Socket.";

        /// <summary>Name of the warm point light on an emissive prop.</summary>
        public const string PropLightName = "PropLight";

        [MenuItem("Tools/Plunderspell/Forge Art Bible Enemies + Roster")]
        public static void Forge()
        {
            string projectRoot = ArtBibleEnemyCatalog.FindProjectRoot();
            if (projectRoot == null)
            {
                Debug.LogError($"[ArtBible] No {ArtBibleEnemyCatalog.ManifestPath} found above {Application.dataPath}.");
                return;
            }
            var problems = new List<string>();
            List<ArtBibleEnemySpec> specs = ArtBibleEnemyCatalog.Load(projectRoot, problems);

            var roster = AssetDatabase.LoadAssetAtPath<EnemyRoster>(RosterPath);
            if (roster == null)
            {
                Debug.LogError($"[ArtBible] No enemy roster at {RosterPath}. Run " +
                               "Tools/Plunderspell/Forge Enemy Prefabs + Roster first.");
                return;
            }

            var bolt = AssetDatabase.LoadAssetAtPath<GameObject>(BoltPath);
            if (bolt == null)
                problems.Add($"no projectile at {BoltPath}: ranged enemies will fall back to melee " +
                             "(run Tools/Plunderspell/Forge Projectile Prefab)");

            var prefabs = new Dictionary<string, GameObject>();
            foreach (ArtBibleEnemySpec spec in specs)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.FbxPath);
                if (model == null)
                {
                    problems.Add($"{spec.Name}: model missing at {spec.FbxPath}");
                    continue;
                }

                prefabs[spec.Name] = BuildPrefab(model, spec, bolt, problems);
            }

            int posted = WriteRoster(roster, specs, prefabs);
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (string problem in problems)
                Debug.LogWarning($"[ArtBible] {problem}");
            Debug.Log($"[ArtBible] Forged {prefabs.Count}/{specs.Count} art-bible enemy prefab(s) into " +
                      $"{ArtBibleEnemyCatalog.PrefabRoot} and {posted} roster posting(s) into {RosterPath}; " +
                      $"{problems.Count} problem(s).");
        }

        /// <summary>
        /// Replaces this forge's postings in the roster and leaves every other entry alone: the
        /// supernatural Crypt enemies stay (EnemyPrefabForge owns them), the household four go.
        /// Returns the number of postings written.
        /// </summary>
        public static int WriteRoster(EnemyRoster roster, IEnumerable<ArtBibleEnemySpec> specs,
            IReadOnlyDictionary<string, GameObject> prefabs)
        {
            var ids = new HashSet<string>(specs.Select(spec => spec.Name));
            ids.UnionWith(ReplacedEnemies);
            roster.Entries.RemoveAll(entry => entry == null || ids.Contains(entry.EnemyId));

            int posted = 0;
            foreach (ArtBiblePosting posting in ArtBibleEnemyCatalog.Postings(specs))
            {
                if (!prefabs.TryGetValue(posting.EnemyId, out GameObject prefab) || prefab == null)
                    continue;
                roster.Entries.Add(new EnemyRoster.Entry
                {
                    EnemyId = posting.EnemyId,
                    Era = posting.Era,
                    AnyEra = false,
                    Zone = posting.Zone,
                    Weight = posting.Weight,
                    Prefab = prefab
                });
                posted++;
            }
            return posted;
        }

        /// <summary>
        /// Saves a prefab variant of the model with the components a guard needs. A variant, not a
        /// copy, so a re-export of the .blend flows straight through.
        /// </summary>
        private static GameObject BuildPrefab(GameObject model, ArtBibleEnemySpec spec, GameObject bolt,
            List<string> problems)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = spec.Name;
            instance.transform.localScale = Vector3.one;

            EnemyPrefabForge.GroundModel(instance);

            float radius = Mathf.Clamp(spec.Width * 0.5f, 0.3f, 0.45f);
            if (!spec.IsHumanoid)
                radius = Mathf.Clamp(spec.Width * 0.5f, 0.2f, 0.35f);

            // The collider is the body (props excluded): a spear point is not something to bump into.
            var capsule = instance.AddComponent<CapsuleCollider>();
            capsule.radius = radius;
            capsule.height = Mathf.Max(spec.BodyHeight, radius * 2f);
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

            // Capped so the agent fits every archway of the zones it is posted to (scale.md). The
            // plan's "archway duck" is the visual half, flagged on the profile.
            var agent = instance.AddComponent<NavMeshAgent>();
            agent.radius = radius;
            agent.height = spec.AgentHeight;
            agent.speed = Mathf.Max(spec.Tuning.PatrolSpeed, 0.01f);
            agent.angularSpeed = 240f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.8f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            instance.AddComponent<StatusEffectReceiver>();
            CastleGuard guard = instance.AddComponent<CastleGuard>();
            ApplyGuardTuning(guard, spec, bolt);

            var profile = instance.AddComponent<EnemyBodyProfile>();
            profile.Configure(spec.Name, spec.Role.ToString().ToLowerInvariant(), spec.BodyHeight,
                spec.HeightWithProps, spec.ArchwayClearance);

            AddSockets(instance, spec, problems);
            if (spec.Emissive)
                AddPropLight(instance, spec, problems);

            string path = spec.PrefabPath;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static void ApplyGuardTuning(CastleGuard guard, ArtBibleEnemySpec spec, GameObject bolt)
        {
            ArtBibleTuning tuning = spec.Tuning;
            var so = new SerializedObject(guard);
            so.FindProperty("_sightRange").floatValue = tuning.SightRange;
            so.FindProperty("_patrolSpeed").floatValue = tuning.PatrolSpeed;
            so.FindProperty("_chaseSpeed").floatValue = tuning.ChaseSpeed;
            so.FindProperty("_maxHealth").floatValue = tuning.MaxHealth;
            so.FindProperty("_eyeHeight").floatValue = spec.BodyHeight * 0.92f;
            // Castle geometry is on Default; without this guards see through walls (EnemyPrefabForge).
            so.FindProperty("_geometryLayers").intValue = 1;
            if (tuning.Attack == ArtBibleAttack.Projectile && bolt != null)
            {
                so.FindProperty("_projectilePrefab").objectReferenceValue = bolt;
                so.FindProperty("_attackCooldown").floatValue = 2.2f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>An empty <c>Socket.&lt;Bone&gt;</c> child under each socket bone, at the bone.</summary>
        private static void AddSockets(GameObject instance, ArtBibleEnemySpec spec, List<string> problems)
        {
            foreach (string boneName in spec.SocketBones)
            {
                Transform bone = FindDeep(instance.transform, boneName);
                if (bone == null)
                {
                    problems.Add($"{spec.Name}: no bone '{boneName}' for a socket");
                    continue;
                }

                var socket = new GameObject(SocketPrefix + boneName);
                socket.transform.SetParent(bone, false);
            }
        }

        /// <summary>
        /// "Light that always comes from something standing in the room" (the mood board): a small
        /// warm point light on the glowing prop. No shadows, so a dozen guards stay cheap.
        /// </summary>
        private static void AddPropLight(GameObject instance, ArtBibleEnemySpec spec, List<string> problems)
        {
            Transform bone = spec.LightBone != null ? FindDeep(instance.transform, spec.LightBone) : null;
            if (bone == null)
            {
                problems.Add($"{spec.Name}: emissive but no light bone '{spec.LightBone}' found; no prop light");
                return;
            }

            var go = new GameObject(PropLightName);
            go.transform.SetParent(bone, false);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.3f);
            light.intensity = 1.2f;
            light.range = 3.5f;
            light.shadows = LightShadows.None;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
            return null;
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
