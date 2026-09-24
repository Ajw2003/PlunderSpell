using System.Collections.Generic;
using System.IO;
using RogueAi.Castle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Renders a generated castle to PNG so a layout change can be judged by eye rather than by
    /// reading coordinates out of a log. See docs/systems/castle.md, "Visual verification", for
    /// which view catches which class of fault and why the set is shaped this way.
    ///
    /// Run headlessly with -executeMethod RogueAi.EditorTools.CastleScreenshotForge.CaptureAll.
    /// A real graphics device is required, so the batchmode invocation must NOT pass -nographics.
    /// </summary>
    public static class CastleScreenshotForge
    {
        private const string k_ScenePath = "Assets/_Project/Scenes/RaidScene.unity";
        private const string k_OutputFolder = "docs/generated/castle-screenshots";
        private const string k_GroundObjectName = "Ground";

        private const int k_CaptureWidth = 1600;
        private const int k_CaptureHeight = 900;

        private const float k_PlanOrthographicSize = 78f;
        private const float k_PlanAltitude = 200f;
        private const float k_ElevationOrthographicSize = 46f;

        private const float k_EyeHeight = 1.65f;
        private const float k_AerialDistance = 150f;
        private const float k_AerialAltitude = 95f;
        private const float k_GateApproachDistance = 38f;

        private static readonly int[] k_Seeds = { 12345, 777, 20260917 };

        // Only the first seed gets the full sweep; the others get plan plus one aerial, which is
        // enough to tell a real layout fault from a one-seed fluke without tripling the image count.
        private const int k_FullSweepSeed = 12345;

        private static readonly (string Name, Vector3 Direction)[] k_AerialCorners =
        {
            ("ne", new Vector3(1f, 0f, 1f)),
            ("se", new Vector3(1f, 0f, -1f)),
            ("sw", new Vector3(-1f, 0f, -1f)),
            ("nw", new Vector3(-1f, 0f, 1f)),
        };

        [MenuItem("Tools/Plunderspell/Capture Castle Screenshots")]
        public static void CaptureAll()
        {
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), k_OutputFolder);
            Directory.CreateDirectory(outputDirectory);

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Debug.LogError("[CastleShots] No graphics device. Re-run without -nographics.");
                EditorApplication.Exit(1);
                return;
            }

            EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Single);

            var generator = Object.FindFirstObjectByType<ProceduralCastleGenerator>();
            if (generator == null)
            {
                Debug.LogError($"[CastleShots] {k_ScenePath} has no ProceduralCastleGenerator.");
                EditorApplication.Exit(1);
                return;
            }

            EnsureSunlight();
            GameObject ground = GameObject.Find(k_GroundObjectName);
            int imageCount = 0;

            foreach (int seed in k_Seeds)
            {
                ProceduralCastleData data = generator.Generate(seed);
                bool isFullSweep = seed == k_FullSweepSeed;

                // The ground plane fills an overhead frame with flat colour, hiding the wall seams
                // and archways the plan exists to show. Every other view needs it for the horizon.
                SetGroundVisible(ground, false);
                imageCount += CapturePlan(data, seed, outputDirectory);
                SetGroundVisible(ground, true);

                imageCount += CaptureAerials(data, seed, outputDirectory, isFullSweep);

                if (isFullSweep)
                {
                    imageCount += CaptureElevation(data, seed, outputDirectory);
                    imageCount += CaptureGateApproach(data, seed, outputDirectory);
                    imageCount += CaptureCourtyard(data, seed, outputDirectory);
                    imageCount += CaptureEyeLevel(data, seed, outputDirectory);
                }

                Debug.Log($"[CastleShots] seed {seed}: {data.PlacedModules.Count} modules.");
            }

            generator.ClearGenerated();
            Debug.Log($"[CastleShots] Wrote {imageCount} images to {k_OutputFolder}.");
        }

        /// <summary>Top-down orthographic floor plan: gaps, overlaps, and the overall shape.</summary>
        private static int CapturePlan(ProceduralCastleData data, int seed, string outputDirectory)
        {
            Vector3 center = LayoutCenter(data);

            RenderFrom(new Vector3(center.x, k_PlanAltitude, center.z), Quaternion.Euler(90f, 0f, 0f),
                isOrthographic: true, k_PlanOrthographicSize,
                Path.Combine(outputDirectory, $"seed-{seed}-plan.png"));
            return 1;
        }

        /// <summary>
        /// Three-quarter aerials from each corner: the silhouette and massing test, and the view
        /// that exposes a module yawed the wrong way, which an overhead plan flattens out.
        /// </summary>
        private static int CaptureAerials(ProceduralCastleData data, int seed, string outputDirectory,
            bool isFullSweep)
        {
            Vector3 center = LayoutCenter(data);
            int written = 0;

            foreach ((string name, Vector3 direction) in k_AerialCorners)
            {
                Vector3 position = center + direction.normalized * k_AerialDistance
                                   + Vector3.up * k_AerialAltitude;

                RenderFrom(position, Quaternion.LookRotation(center - position, Vector3.up),
                    isOrthographic: false, k_PlanOrthographicSize,
                    Path.Combine(outputDirectory, $"seed-{seed}-aerial-{name}.png"));
                written++;

                if (!isFullSweep)
                {
                    break;
                }
            }

            return written;
        }

        /// <summary>
        /// Orthographic side elevation, Issue 19's own check: every floor flush at Y=0, and the
        /// zone heights stepping sensibly rather than floating or sinking.
        /// </summary>
        private static int CaptureElevation(ProceduralCastleData data, int seed, string outputDirectory)
        {
            Vector3 center = LayoutCenter(data);
            var position = new Vector3(center.x, 12f, center.z - 300f);

            RenderFrom(position, Quaternion.Euler(0f, 0f, 0f), isOrthographic: true,
                k_ElevationOrthographicSize,
                Path.Combine(outputDirectory, $"seed-{seed}-elevation.png"));
            return 1;
        }

        /// <summary>
        /// Standing outside the gate looking in. The curtain wall's outward face is the thing most
        /// likely to be rotated wrong, because each wall module raises its wall on one named side.
        /// </summary>
        private static int CaptureGateApproach(ProceduralCastleData data, int seed,
            string outputDirectory)
        {
            if (data.ExtractionExitIndex < 0)
            {
                Debug.LogWarning("[CastleShots] No extraction exit; skipping the gate approach.");
                return 0;
            }

            Vector3 center = LayoutCenter(data);
            Vector3 gate = data.PlacedModules[data.ExtractionExitIndex].Position;

            Vector3 outward = gate - center;
            outward.y = 0f;
            outward = outward.sqrMagnitude > 0.001f ? outward.normalized : Vector3.right;

            var position = new Vector3(gate.x, k_EyeHeight, gate.z) + outward * k_GateApproachDistance;
            var target = new Vector3(gate.x, k_EyeHeight, gate.z);

            RenderFrom(position, Quaternion.LookRotation(target - position, Vector3.up),
                isOrthographic: false, k_PlanOrthographicSize,
                Path.Combine(outputDirectory, $"seed-{seed}-gate.png"));
            return 1;
        }

        /// <summary>Ground level inside the walls: do the wards enclose, or read as loose boxes?</summary>
        private static int CaptureCourtyard(ProceduralCastleData data, int seed, string outputDirectory)
        {
            Vector3 center = LayoutCenter(data);
            var position = new Vector3(center.x, k_EyeHeight, center.z);

            RenderFrom(position, Quaternion.Euler(-8f, 45f, 0f), isOrthographic: false,
                k_PlanOrthographicSize,
                Path.Combine(outputDirectory, $"seed-{seed}-courtyard.png"));
            return 1;
        }

        /// <summary>
        /// Standing eye-level in the crypt looking out: the check that archways actually line up
        /// through successive rooms instead of opening onto a wall.
        /// </summary>
        private static int CaptureEyeLevel(ProceduralCastleData data, int seed, string outputDirectory)
        {
            Vector3 origin = data.CryptStartIndex >= 0
                ? data.PlacedModules[data.CryptStartIndex].Position
                : LayoutCenter(data);

            RenderFrom(new Vector3(origin.x, k_EyeHeight, origin.z), Quaternion.identity,
                isOrthographic: false, k_PlanOrthographicSize,
                Path.Combine(outputDirectory, $"seed-{seed}-eye.png"));
            return 1;
        }

        /// <summary>Delegates to the shared capture path; see <see cref="SceneScreenshot"/>.</summary>
        private static void RenderFrom(Vector3 position, Quaternion rotation, bool isOrthographic,
            float orthographicSize, string filePath)
        {
            SceneScreenshot.Capture(position, rotation, isOrthographic, orthographicSize, filePath,
                k_CaptureWidth, k_CaptureHeight);
        }

        private static void SetGroundVisible(GameObject ground, bool isVisible)
        {
            if (ground == null)
            {
                return;
            }

            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = isVisible;
            }
        }

        /// <summary>Mean of every placed module position, so a view frames the castle it built.</summary>
        private static Vector3 LayoutCenter(ProceduralCastleData data)
        {
            List<ProceduralCastleData.PlacedModule> modules = data.PlacedModules;
            if (modules.Count == 0)
            {
                return Vector3.zero;
            }

            var sum = Vector3.zero;
            for (int i = 0; i < modules.Count; i++)
            {
                sum += modules[i].Position;
            }

            return sum / modules.Count;
        }

        /// <summary>
        /// Without a light every view renders as flat silhouettes, which hides exactly the seams
        /// and wall thicknesses these captures exist to show.
        /// </summary>
        private static void EnsureSunlight()
        {
            foreach (Light existing in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (existing.type == LightType.Directional)
                {
                    return;
                }
            }

            var lightGo = new GameObject("CastleScreenshotSun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
    }
}
