using System.Collections.Generic;
using System.IO;
using RogueAi.Spells;
using RogueAi.Spells.Vfx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Photographs every spell's visual, so "the spells are visible" is something you can look at
    /// rather than something a test asserts about a GameObject's existence.
    ///
    /// See docs/systems/spells.md, "Seeing a cast".
    /// </summary>
    public static class SpellVfxScreenshotForge
    {
        private const string k_ScenePath = "Assets/_Project/Scenes/CombatBench.unity";
        private const string k_OutputFolder = "docs/generated/spell-vfx-screenshots";
        private const string k_BoltPath = "Assets/_Project/Prefabs/Projectiles/Bolt.prefab";

        private const float k_Spacing = 4.5f;

        /// <summary>Mid-bloom: bright, well grown, not yet faded out.</summary>
        private const float k_CaptureProgress = 0.4f;

        private static readonly SpellId[] k_Showcase =
        {
            SpellId.Ignis, SpellId.Frango, SpellId.Levo, SpellId.AurumVoco,
            SpellId.Tonitrus, SpellId.Somnus, SpellId.CadaverSurge, SpellId.Porta,
            SpellId.MisfireIgnis,
        };

        [MenuItem("Tools/Plunderspell/Capture Spell VFX Screenshots")]
        public static void CaptureAll()
        {
            if (!SceneScreenshot.HasGraphicsDevice)
            {
                Debug.LogError("[SpellShots] No graphics device. Re-run without -nographics.");
                EditorApplication.Exit(1);
                return;
            }

            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), k_OutputFolder);
            Directory.CreateDirectory(outputDirectory);

            EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Single);

            var spawned = new List<GameObject>();
            float x = 0f;

            foreach (SpellId spell in k_Showcase)
            {
                SpellLook look = SpellLookbook.For(spell);
                var at = new Vector3(x, 1.6f, 0f);

                SpellBurst burst = SpellBurst.Spawn(at, look.Colour, look.Radius, 0.45f);
                burst.SetProgress(k_CaptureProgress);
                burst.name = $"Burst_{spell}";
                spawned.Add(burst.gameObject);

                if (look.Style == SpellVisualStyle.Bolt)
                {
                    GameObject bolt = SpawnTintedBolt(at + Vector3.forward * 2.2f, look.Colour);
                    if (bolt != null)
                    {
                        bolt.name = $"Bolt_{spell}";
                        spawned.Add(bolt);
                    }
                }

                x += k_Spacing;
            }

            float centreX = (x - k_Spacing) * 0.5f;
            var focus = new Vector3(centreX, 1.6f, 0f);
            float aspect = SceneScreenshot.DefaultWidth / (float)SceneScreenshot.DefaultHeight;
            float orthographicSize = (centreX + k_Spacing) / aspect;

            // Camera stays inside the arena: the bench wall is opaque, and a camera behind it
            // photographs the wall.
            var front = new Vector3(centreX, focus.y, -16f);
            SceneScreenshot.Capture(front, Quaternion.identity, isOrthographic: true,
                orthographicSize, Path.Combine(outputDirectory, "spells-lineup.png"));

            var angled = new Vector3(centreX - 14f, 7f, -14f);
            SceneScreenshot.Capture(angled, Quaternion.LookRotation(focus - angled, Vector3.up),
                isOrthographic: false, orthographicSize,
                Path.Combine(outputDirectory, "spells-lineup-angled.png"));

            var close = new Vector3(0f, 1.6f, -5f);
            SceneScreenshot.Capture(close,
                Quaternion.LookRotation(new Vector3(0f, 1.6f, 0f) - close, Vector3.up),
                isOrthographic: false, orthographicSize,
                Path.Combine(outputDirectory, "spell-ignis-close.png"));

            foreach (GameObject go in spawned)
            {
                Object.DestroyImmediate(go);
            }

            Debug.Log($"[SpellShots] Wrote 3 images to {k_OutputFolder} for " +
                      $"{k_Showcase.Length} spell looks.");
        }

        /// <summary>
        /// A spell going off inside the actual raid scene, at the player's own spawn, seen from the
        /// player's eye. The bench captures prove the looks exist; this proves they show up in the
        /// game.
        /// </summary>
        [MenuItem("Tools/Plunderspell/Capture A Cast In The Raid Scene")]
        public static void CaptureRaidSceneCast()
        {
            if (!SceneScreenshot.HasGraphicsDevice)
            {
                Debug.LogError("[SpellShots] No graphics device. Re-run without -nographics.");
                EditorApplication.Exit(1);
                return;
            }

            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), k_OutputFolder);
            Directory.CreateDirectory(outputDirectory);

            EditorSceneManager.OpenScene("Assets/_Project/Scenes/RaidScene.unity", OpenSceneMode.Single);

            var casting = Object.FindFirstObjectByType<SpellCastingSystem>();
            if (casting == null)
            {
                Debug.LogError("[SpellShots] The raid scene has no SpellCastingSystem.");
                EditorApplication.Exit(1);
                return;
            }

            Transform player = casting.transform;
            Vector3 hands = player.position + player.forward * 1.0f + Vector3.up * 1.5f;

            SpellLook look = SpellLookbook.For(SpellId.Tonitrus);
            SpellBurst burst = SpellBurst.Spawn(hands, look.Colour, look.Radius, 0.45f);
            burst.SetProgress(k_CaptureProgress);

            Vector3 eye = player.position + Vector3.up * 1.65f - player.forward * 1.2f;
            SceneScreenshot.Capture(eye, Quaternion.LookRotation(hands - eye, Vector3.up),
                isOrthographic: false, 10f,
                Path.Combine(outputDirectory, "raid-cast-eye.png"));

            Vector3 over = player.position + Vector3.up * 6f - player.forward * 7f;
            SceneScreenshot.Capture(over, Quaternion.LookRotation(hands - over, Vector3.up),
                isOrthographic: false, 10f,
                Path.Combine(outputDirectory, "raid-cast-over.png"));

            Object.DestroyImmediate(burst.gameObject);
            Debug.Log($"[SpellShots] Wrote 2 raid-scene cast images to {k_OutputFolder}.");
        }

        /// <summary>
        /// Every look, one image each, from the scene's own player camera at its own position — not
        /// an approximation of it. Answers "does the caster see their own spell", which the bench
        /// lineup and the single-spell raid capture do not: both put the camera further from the
        /// burst than the real rig does. See docs/systems/spells.md, "A burst is invisible from
        /// inside itself".
        /// </summary>
        [MenuItem("Tools/Plunderspell/Capture Every Cast From The Raid Scene Eye")]
        public static void CaptureEveryCastInRaidSceneFromTheEye() =>
            CaptureEveryCastFromTheCastersEye("Assets/_Project/Scenes/RaidScene.unity", "raid");

        /// <summary>See <see cref="CaptureEveryCastInRaidSceneFromTheEye"/>; same capture, the bench
        /// instead of the raid.</summary>
        [MenuItem("Tools/Plunderspell/Capture Every Cast From The Combat Bench Eye")]
        public static void CaptureEveryCastInCombatBenchFromTheEye() =>
            CaptureEveryCastFromTheCastersEye(k_ScenePath, "bench");

        private static void CaptureEveryCastFromTheCastersEye(string scenePath, string filePrefix)
        {
            if (!SceneScreenshot.HasGraphicsDevice)
            {
                Debug.LogError("[SpellShots] No graphics device. Re-run without -nographics.");
                EditorApplication.Exit(1);
                return;
            }

            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), k_OutputFolder);
            Directory.CreateDirectory(outputDirectory);

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var casting = Object.FindFirstObjectByType<SpellCastingSystem>();
            if (casting == null)
            {
                Debug.LogError($"[SpellShots] {scenePath} has no SpellCastingSystem.");
                EditorApplication.Exit(1);
                return;
            }

            Transform player = casting.transform;
            var castingFields = new SerializedObject(casting);
            float forwardOffset = castingFields.FindProperty("_castOriginForwardOffset").floatValue;
            float castHeight = castingFields.FindProperty("_castOriginHeight").floatValue;
            Vector3 hands = player.position + player.forward * forwardOffset + Vector3.up * castHeight;

            Camera playerCamera = Camera.main;
            Vector3 eyePos = playerCamera != null
                ? playerCamera.transform.position
                : player.position + Vector3.up * 1.65f;
            Quaternion eyeRot = playerCamera != null
                ? playerCamera.transform.rotation
                : Quaternion.LookRotation(hands - eyePos, Vector3.up);

            int written = 0;
            foreach (SpellId spell in k_Showcase)
            {
                SpellLook look = SpellLookbook.For(spell);

                GameObject bolt = look.Style == SpellVisualStyle.Bolt
                    ? SpawnTintedBolt(hands + player.forward * 1.5f, look.Colour)
                    : null;

                SpellBurst burst = SpellBurst.Spawn(hands, look.Colour, look.Radius, 0.45f);
                burst.SetProgress(k_CaptureProgress);

                string path = Path.Combine(outputDirectory, $"{filePrefix}-cast-eye-{spell}.png");
                SceneScreenshot.Capture(eyePos, eyeRot, isOrthographic: false, 10f, path);
                written++;

                Object.DestroyImmediate(burst.gameObject);
                if (bolt != null)
                {
                    Object.DestroyImmediate(bolt);
                }
            }

            Debug.Log($"[SpellShots] Wrote {written} eye-view images to {k_OutputFolder} for {scenePath}.");
        }

        private static GameObject SpawnTintedBolt(Vector3 position, Color colour)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_BoltPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[SpellShots] No projectile at {k_BoltPath}.");
                return null;
            }

            GameObject bolt = Object.Instantiate(prefab, position, Quaternion.identity);
            if (bolt.TryGetComponent(out ProjectileTint tint))
            {
                tint.Apply(colour);
            }

            return bolt;
        }
    }
}
