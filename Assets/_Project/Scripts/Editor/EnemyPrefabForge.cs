using System.Collections.Generic;
using System.IO;
using RogueAi.Castle;
using RogueAi.Guards;
using RogueAi.Raid;
using RogueAi.Status;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Authors one prefab per enemy model, plus the <see cref="EnemyRoster"/> that posts them to
    /// zones. Run once; the prefabs and the roster are committed assets from then on, editable by
    /// hand like any other.
    ///
    /// See docs/systems/raid-scene-assembly.md ("Enemy prefabs") for why the prefabs are variants of
    /// the model rather than copies of it, and for the per-enemy tuning table.
    /// </summary>
    public static class EnemyPrefabForge
    {
        private const string ModelDirectory = "Assets/Models/Enemies";
        private const string PrefabDirectory = "Assets/_Project/Prefabs/Enemies";
        private const string RosterPath = "Assets/_Project/Data/Enemies/EnemyRoster.asset";

        /// <summary>
        /// How far an enemy's lowest vertex may sit from its own origin before it reads as floating
        /// or sunk in play. Shared with <c>ScaleInvariantTests</c> so the tool that repairs a prefab
        /// and the test that judges it cannot disagree about what "repaired" means.
        /// </summary>
        public const float FootTolerance = 0.10f;

        /// <summary>One enemy's tuning and the zones it garrisons.</summary>
        private readonly struct EnemySpec
        {
            public readonly string Name;
            public readonly float StandingHeight;
            public readonly float PatrolSpeed;
            public readonly float ChaseSpeed;
            public readonly float SightRange;
            public readonly float MaxHealth;
            public readonly (CastleZone Zone, int Weight)[] Posts;

            public EnemySpec(string name, float standingHeight, float patrolSpeed, float chaseSpeed,
                float sightRange, float maxHealth, params (CastleZone, int)[] posts)
            {
                Name = name;
                StandingHeight = standingHeight;
                PatrolSpeed = patrolSpeed;
                ChaseSpeed = chaseSpeed;
                SightRange = sightRange;
                MaxHealth = maxHealth;
                Posts = posts;
            }
        }

        // Tuned from the roles recorded in Assets/Models/Enemies/enemy_manifest.json. Weights put the
        // common soldiery on the outside and the rare, dangerous things in the Keep and the Crypt.
        // The second column is standing height in metres, against the 1.8m human standard in
        // docs/systems/scale.md; BuildPrefab scales each model to it.
        private static readonly EnemySpec[] Specs =
        {
            new EnemySpec("Watchman",       1.80f, 2.0f, 4.0f, 14f,  70f,
                (CastleZone.CurtainWall, 12), (CastleZone.OuterBailey, 10)),
            new EnemySpec("ManAtArms",      1.85f, 2.0f, 4.5f, 15f, 100f,
                (CastleZone.OuterBailey, 8), (CastleZone.InnerWard, 8)),
            new EnemySpec("Sergeant",       1.90f, 2.2f, 5.0f, 17f, 130f,
                (CastleZone.InnerWard, 5), (CastleZone.Keep, 5)),
            new EnemySpec("WarHound",       0.85f, 2.8f, 6.5f, 12f,  55f,
                (CastleZone.OuterBailey, 6), (CastleZone.InnerWard, 6)),
            new EnemySpec("SigilWisp",      1.20f, 3.0f, 5.5f, 16f,  35f,
                (CastleZone.InnerWard, 5), (CastleZone.Keep, 4)),
            new EnemySpec("VaultWarden",    2.10f, 1.4f, 3.2f, 13f, 200f,
                (CastleZone.Keep, 6), (CastleZone.Crypt, 5)),
            // A sentry that never leaves its post: speed 0 means it tracks and fires without walking.
            new EnemySpec("HexTurret",      1.60f, 0f,   0f,   20f,  90f,
                (CastleZone.CurtainWall, 4), (CastleZone.Keep, 3)),
            new EnemySpec("ArcRevenant",    2.10f, 1.8f, 3.8f, 18f, 150f,
                (CastleZone.Keep, 3), (CastleZone.Crypt, 5)),
            new EnemySpec("CryptRisen",     1.75f, 1.6f, 4.2f, 12f,  80f,
                (CastleZone.Crypt, 12)),
            // Head and shoulders over everything else, but under the Crypt's clear height — the
            // tightest room it can be posted to. See docs/systems/scale.md ("Enemies").
            new EnemySpec("GildedColossus", 2.50f, 1.2f, 2.8f, 15f, 400f,
                (CastleZone.Crypt, 2)),
        };

        [MenuItem("Tools/Plunderspell/Forge Enemy Prefabs + Roster")]
        public static void Forge()
        {
            EnsureFolder(PrefabDirectory);
            EnsureFolder(Path.GetDirectoryName(RosterPath).Replace('\\', '/'));

            var roster = LoadOrCreate<EnemyRoster>(RosterPath);
            roster.Entries.Clear();

            var built = new List<string>();
            var missing = new List<string>();

            foreach (EnemySpec spec in Specs)
            {
                string modelPath = $"{ModelDirectory}/{spec.Name}/{spec.Name}.fbx";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (model == null)
                {
                    missing.Add(modelPath);
                    continue;
                }

                GameObject prefab = BuildPrefab(model, spec);
                built.Add(prefab.name);

                foreach ((CastleZone zone, int weight) in spec.Posts)
                {
                    roster.Entries.Add(new EnemyRoster.Entry
                    {
                        EnemyId = spec.Name,
                        Zone = zone,
                        Weight = weight,
                        Prefab = prefab
                    });
                }
            }

            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (missing.Count > 0)
                Debug.LogError($"Plunderspell: {missing.Count} enemy model(s) missing: {string.Join(", ", missing)}");

            Debug.Log($"Plunderspell: forged {built.Count} enemy prefab(s) into {PrefabDirectory} " +
                      $"and {roster.Entries.Count} roster posting(s) into {RosterPath}.");
        }

        /// <summary>
        /// Saves a prefab variant of the model with the components a guard needs. A variant, not a
        /// copy, so re-exporting the .blend flows straight through to every prefab.
        /// </summary>
        private static GameObject BuildPrefab(GameObject model, EnemySpec spec)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.name = spec.Name;

            // The models were authored at assorted heights; scaling each uniformly to its spec's
            // standing height is what makes the whole cast agree on one metre.
            Bounds authored = MeasureBounds(instance);
            float localHeight = Mathf.Max(0.01f, authored.size.y);
            float localRadius = Mathf.Max(0.05f, Mathf.Max(authored.size.x, authored.size.z) * 0.5f);
            float scale = spec.StandingHeight / localHeight;
            instance.transform.localScale = Vector3.one * scale;

            // Not every model is exported feet-on-origin (ArcRevenant's lowest point is 0.154 m up),
            // so measure where the geometry actually is and move it, rather than trusting the export.
            GroundModel(instance);

            float height = spec.StandingHeight;
            float radius = Mathf.Max(0.3f, localRadius * scale);

            // A CapsuleCollider's numbers are local, so they are the *unscaled* measurements; the
            // transform scale above then carries them to the same metres as `height`/`radius`.
            // GroundModel has put the feet on the origin, so the collider is centred on half its height.
            var capsule = instance.AddComponent<CapsuleCollider>();
            capsule.radius = radius / scale;
            capsule.height = localHeight;
            capsule.center = new Vector3(0f, localHeight * 0.5f, 0f);

            // A NavMeshAgent's are world units and ignore the transform scale, so they take the
            // scaled figures instead.
            var agent = instance.AddComponent<NavMeshAgent>();
            agent.radius = radius;
            agent.height = height;
            agent.speed = Mathf.Max(spec.PatrolSpeed, 0.01f);
            agent.angularSpeed = 240f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 0.8f;
            // A turret is placed, not steered: letting it avoid others would drift it off its post.
            agent.obstacleAvoidanceType = spec.PatrolSpeed <= 0f
                ? ObstacleAvoidanceType.NoObstacleAvoidance
                : ObstacleAvoidanceType.LowQualityObstacleAvoidance;

            instance.AddComponent<StatusEffectReceiver>();
            CastleGuard guard = instance.AddComponent<CastleGuard>();
            ApplyGuardTuning(guard, spec, height);

            string path = $"{PrefabDirectory}/{spec.Name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        /// <summary>
        /// Moves the model's children up or down so the lowest thing it draws sits on the root's own
        /// origin, and returns the world-space distance moved. The root itself stays put, so the
        /// components on it (collider, agent, guard) and every spawner that places the root on the
        /// floor keep working unchanged. Measures real geometry, not <c>Renderer.bounds</c> — see
        /// <see cref="PrefabGeometry"/> for why the bounds cannot be trusted on a rigged mesh.
        /// </summary>
        public static float GroundModel(GameObject root)
        {
            if (!PrefabGeometry.TryMeasureVerticalExtent(root, out float lowest, out _))
            {
                return 0f;
            }

            float worldShift = root.transform.position.y - lowest;
            float localShift = worldShift / Mathf.Max(0.0001f, root.transform.lossyScale.y);

            foreach (Transform child in root.transform)
            {
                child.localPosition += new Vector3(0f, localShift, 0f);
            }

            return worldShift;
        }

        /// <summary>
        /// Grounds the enemy prefabs that already exist, in place, without re-forging them. A re-forge
        /// regenerates all ten and would discard the hand edits made since (commit 7a3ec27 alone
        /// touched every one), so this repairs only the prefabs outside <see cref="FootTolerance"/>
        /// and leaves everything else on them untouched. Safe to run repeatedly: a prefab already
        /// inside the tolerance is skipped.
        /// </summary>
        [MenuItem("Tools/Plunderspell/Ground Enemy Prefabs In Place")]
        public static string GroundExistingPrefabs()
        {
            var report = new System.Text.StringBuilder();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDirectory }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);

                try
                {
                    PrefabGeometry.TryMeasureVerticalExtent(contents, out float before, out _);
                    float offset = before - contents.transform.position.y;

                    if (Mathf.Abs(offset) <= FootTolerance)
                    {
                        report.AppendLine($"{contents.name,-16} lowest {offset,+7:F3} m  within tolerance, left alone");
                        continue;
                    }

                    GroundModel(contents);
                    PrefabGeometry.TryMeasureVerticalExtent(contents, out float after, out _);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    report.AppendLine($"{contents.name,-16} lowest {offset,+7:F3} m  grounded, now " +
                                      $"{after - contents.transform.position.y,+7:F3} m");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"Plunderspell: grounded enemy prefabs in place.\n{report}");
            return report.ToString();
        }

        /// <summary>
        /// Writes the guard's inspector fields. They are private and serialized, so this goes through
        /// SerializedObject rather than widening the runtime API just for an authoring tool.
        /// </summary>
        private static void ApplyGuardTuning(CastleGuard guard, EnemySpec spec, float height)
        {
            var so = new SerializedObject(guard);
            so.FindProperty("_sightRange").floatValue = spec.SightRange;
            so.FindProperty("_patrolSpeed").floatValue = spec.PatrolSpeed;
            so.FindProperty("_chaseSpeed").floatValue = spec.ChaseSpeed;
            so.FindProperty("_maxHealth").floatValue = spec.MaxHealth;
            // Eyes sit near the top of the model, so the line-of-sight ray clears its own collider.
            so.FindProperty("_eyeHeight").floatValue = height * 0.9f;
            // Castle geometry is on Default; without this the sight check never hits a wall and
            // guards see through the castle.
            so.FindProperty("_geometryLayers").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds MeasureBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var created = ScriptableObject.CreateInstance<T>();
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
