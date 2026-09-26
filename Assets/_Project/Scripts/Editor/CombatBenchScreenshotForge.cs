using System.Collections.Generic;
using System.IO;
using Plunderspell.Guards;
using Plunderspell.Playtest;
using Plunderspell.Raid;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Photographs the combat bench with a spawn on the floor. See docs/systems/combat-bench.md,
    /// "Verification", for the headless invocation and why the panel can never appear in these.
    /// </summary>
    public static class CombatBenchScreenshotForge
    {
        private const string k_ScenePath = "Assets/_Project/Scenes/CombatBench.unity";
        private const string k_OutputFolder = "docs/generated/combat-bench-screenshots";

        private const int k_SpawnCount = 5;
        private const float k_EyeHeight = 1.65f;
        private const float k_LineupSpacing = 1.6f;
        private const float k_LineupCameraDistance = 14f;

        private static readonly (string Name, Vector3 Offset, bool LooksAtCentre)[] k_Views =
        {
            ("plan", new Vector3(0f, 46f, 0f), true),
            ("aerial-ne", new Vector3(26f, 18f, 26f), true),
            ("aerial-sw", new Vector3(-26f, 18f, -26f), true),
            ("eye", new Vector3(0f, k_EyeHeight, -9f), true),
        };

        [MenuItem("Tools/Plunderspell/Capture Combat Bench Screenshots")]
        public static void CaptureAll()
        {
            if (!SceneScreenshot.HasGraphicsDevice)
            {
                Debug.LogError("[BenchShots] No graphics device. Re-run without -nographics.");
                EditorApplication.Exit(1);
                return;
            }

            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), k_OutputFolder);
            Directory.CreateDirectory(outputDirectory);

            EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Single);

            var bench = Object.FindFirstObjectByType<CombatBench>();
            if (bench == null)
            {
                Debug.LogError($"[BenchShots] {k_ScenePath} has no CombatBench.");
                EditorApplication.Exit(1);
                return;
            }

            GameObject prefab = FirstSpawnablePrefab(bench.Roster);
            if (prefab != null)
            {
                bench.Spawn(prefab, k_SpawnCount, GuardAlertState.Chasing);
            }
            else
            {
                Debug.LogWarning("[BenchShots] Roster has no prefab; capturing an empty arena.");
            }

            int written = 0;
            foreach ((string name, Vector3 offset, bool looksAtCentre) in k_Views)
            {
                bool isPlan = name == "plan";
                Quaternion rotation = isPlan
                    ? Quaternion.Euler(90f, 0f, 0f)
                    : Quaternion.LookRotation(new Vector3(0f, k_EyeHeight, 0f) - offset, Vector3.up);

                SceneScreenshot.Capture(offset, rotation, isPlan, 24f,
                    Path.Combine(outputDirectory, $"bench-{name}.png"));
                written++;
            }

            bench.ClearSpawned();
            written += CaptureScaleLineup(bench, outputDirectory);

            Debug.Log($"[BenchShots] Wrote {written} images to {k_OutputFolder}, " +
                      $"with {k_SpawnCount} spawned enemies in frame.");
        }

        /// <summary>
        /// The whole cast stood in a row beside the player capsule: the one frame that shows whether
        /// the models agree on a metre. Issue
        /// <see href="https://github.com/Ajw2003/PlunderSpell/issues/6"/>.
        /// </summary>
        private static int CaptureScaleLineup(CombatBench bench, string outputDirectory)
        {
            EnemyRoster roster = bench.Roster;
            if (roster == null)
            {
                return 0;
            }

            // The row runs away from the player capsule, which stays at the origin as the 1.80 m
            // reference every enemy is read against.
            var placed = new List<GameObject>();
            float x = k_LineupSpacing;

            foreach (EnemyRoster.Entry entry in roster.Entries)
            {
                if (entry == null || entry.Prefab == null)
                {
                    continue;
                }

                GameObject enemy = Object.Instantiate(entry.Prefab,
                    new Vector3(x, 0f, 0f), Quaternion.Euler(0f, 180f, 0f));
                enemy.name = $"Lineup_{entry.EnemyId}";
                placed.Add(enemy);
                x += k_LineupSpacing;
            }

            float rowEnd = x - k_LineupSpacing;
            float centreX = rowEnd * 0.5f;
            var focus = new Vector3(centreX, 1.4f, 0f);

            // Orthographic size is half the frame *height*, so it has to come from the row's width
            // divided by the aspect ratio — deriving it from the width directly zooms far enough out
            // that 2 m figures become specks.
            float aspect = SceneScreenshot.DefaultWidth / (float)SceneScreenshot.DefaultHeight;
            float orthographicSize = (centreX + k_LineupSpacing) / aspect;

            // The camera must stay inside the arena: the wall is opaque, and a camera behind it
            // photographs the wall.
            var frontCamera = new Vector3(centreX, focus.y, -k_LineupCameraDistance);
            SceneScreenshot.Capture(frontCamera, Quaternion.identity, isOrthographic: true,
                orthographicSize, Path.Combine(outputDirectory, "bench-scale-lineup.png"));

            var angledCamera = new Vector3(centreX - 12f, 6f, -k_LineupCameraDistance);
            SceneScreenshot.Capture(angledCamera,
                Quaternion.LookRotation(focus - angledCamera, Vector3.up),
                isOrthographic: false, orthographicSize,
                Path.Combine(outputDirectory, "bench-scale-lineup-angled.png"));

            foreach (GameObject enemy in placed)
            {
                Object.DestroyImmediate(enemy);
            }

            return 2;
        }

        private static GameObject FirstSpawnablePrefab(EnemyRoster roster)
        {
            if (roster == null)
            {
                return null;
            }

            for (int i = 0; i < roster.Entries.Count; i++)
            {
                EnemyRoster.Entry entry = roster.Entries[i];
                if (entry != null && entry.Prefab != null)
                {
                    return entry.Prefab;
                }
            }

            return null;
        }
    }
}
