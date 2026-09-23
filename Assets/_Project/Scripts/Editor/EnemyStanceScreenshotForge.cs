using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Photographs every enemy prefab standing on a solid ground slab, side-on and orthographic, and
    /// writes what was measured next to the images. Issue
    /// <see href="https://github.com/Ajw2003/PlunderSpell/issues/94"/>.
    ///
    /// The slab is the point of the picture: a foot that sinks is cut off by it and a foot that
    /// floats leaves a visible gap, exactly as in a raid. It renders in a throwaway scene so the
    /// scene the Editor had open is left as it was found.
    /// </summary>
    public static class EnemyStanceScreenshotForge
    {
        private const string k_EnemyPrefabDirectory = "Assets/_Project/Prefabs/Enemies";
        private const string k_DefaultOutputFolder = "docs/generated/enemy-stance-screenshots";

        private const float k_Spacing = 2.0f;
        private const float k_SlabDepth = 4f;
        private const float k_SlabThickness = 1f;

        // Lineup frame: the ground line at the bottom edge, tall enough for the 2.5 m Colossus.
        private const float k_LineupBottom = -0.5f;
        private const float k_LineupTop = 2.9f;
        private const int k_LineupPixelsPerMetre = 200;

        // Feet close-up frame: 1.5 m wide by 1.0 m tall around the ground line.
        private const float k_FeetBottom = -0.3f;
        private const float k_FeetTop = 0.7f;
        private const int k_FeetPixelsPerMetre = 600;

        [MenuItem("Tools/Plunderspell/Capture Enemy Stance Screenshots")]
        public static void CaptureAll()
        {
            Capture(k_DefaultOutputFolder);
        }

        /// <summary>
        /// Photographs the roster into <paramref name="outputFolder"/> (project-relative) and returns
        /// the measurement report, which is also written to <c>stance-report.txt</c> there.
        /// </summary>
        public static string Capture(string outputFolder)
        {
            if (!SceneScreenshot.HasGraphicsDevice)
            {
                Debug.LogError("[EnemyStance] No graphics device. Re-run without -nographics.");
                return null;
            }

            Scene original = SceneManager.GetActiveScene();
            if (original.isDirty)
            {
                Debug.LogError($"[EnemyStance] '{original.name}' has unsaved changes; save or discard " +
                               "them first, this tool swaps scenes to photograph in a clean one.");
                return null;
            }

            string originalPath = original.path;
            string directory = Path.Combine(Directory.GetCurrentDirectory(), outputFolder);
            Directory.CreateDirectory(directory);

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                return CaptureInFreshScene(directory, outputFolder);
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalPath))
                {
                    EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
                }
            }
        }

        private static string CaptureInFreshScene(string directory, string outputFolder)
        {
            var prefabs = new List<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { k_EnemyPrefabDirectory }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }

            prefabs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            float rowWidth = prefabs.Count * k_Spacing;
            BuildStage(rowWidth);

            var report = new StringBuilder();
            report.AppendLine("Enemy stance report — each prefab instantiated at the origin, ground slab top at y=0.");
            report.AppendLine("bounds  = lowest point of Renderer.bounds (what the foot test measured before issue 94; padded for skinned meshes).");
            report.AppendLine("vertex  = lowest point of the actual baked geometry (what is drawn).");
            report.AppendLine("height  = highest minus lowest point of the baked geometry (how tall it really draws).");
            report.AppendLine();

            var placed = new List<(GameObject Instance, string Name)>();
            for (int i = 0; i < prefabs.Count; i++)
            {
                var position = new Vector3(i * k_Spacing + k_Spacing * 0.5f, 0f, 0f);
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i]);
                instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));
                placed.Add((instance, prefabs[i].name));

                (float boundsLowest, float vertexLowest) = MeasureLowest(instance);
                PrefabGeometry.TryMeasureVerticalExtent(instance, out float lowest, out float highest);
                report.AppendLine($"{prefabs[i].name,-16} bounds {boundsLowest - position.y,+7:F3} m   " +
                                  $"vertex {vertexLowest - position.y,+7:F3} m   " +
                                  $"height {highest - lowest,6:F3} m");
            }

            float lineupHeight = k_LineupTop - k_LineupBottom;
            SceneScreenshot.Capture(
                new Vector3(rowWidth * 0.5f, k_LineupBottom + lineupHeight * 0.5f, -10f),
                Quaternion.identity, true, lineupHeight * 0.5f,
                Path.Combine(directory, "enemy-stance-lineup.png"),
                Mathf.RoundToInt(rowWidth * k_LineupPixelsPerMetre),
                Mathf.RoundToInt(lineupHeight * k_LineupPixelsPerMetre));

            float feetHeight = k_FeetTop - k_FeetBottom;
            foreach ((GameObject instance, string name) in placed)
            {
                SceneScreenshot.Capture(
                    new Vector3(instance.transform.position.x, k_FeetBottom + feetHeight * 0.5f, -10f),
                    Quaternion.identity, true, feetHeight * 0.5f,
                    Path.Combine(directory, $"enemy-feet-{name}.png"),
                    Mathf.RoundToInt(1.5f * k_FeetPixelsPerMetre),
                    Mathf.RoundToInt(feetHeight * k_FeetPixelsPerMetre));
            }

            File.WriteAllText(Path.Combine(directory, "stance-report.txt"), report.ToString());
            Debug.Log($"[EnemyStance] Wrote {placed.Count + 1} images and stance-report.txt to {outputFolder}.");
            return report.ToString();
        }

        private static void BuildStage(float rowWidth)
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.4f;
            sun.transform.rotation = Quaternion.Euler(35f, -25f, 0f);

            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.7f;
            fill.transform.rotation = Quaternion.Euler(20f, 160f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);

            // The slab's top face is y=0 and its front face sits in front of every foot, so anything
            // below the line is hidden the way the real floor would hide it.
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "GroundSlab";
            slab.transform.localScale = new Vector3(rowWidth + 4f, k_SlabThickness, k_SlabDepth);
            slab.transform.position = new Vector3(rowWidth * 0.5f, -k_SlabThickness * 0.5f, 0f);
            slab.GetComponent<Renderer>().sharedMaterial = UnlitColour(new Color(0.16f, 0.17f, 0.2f));

            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "GroundLine";
            line.transform.localScale = new Vector3(rowWidth + 4f, 0.012f, 0.01f);
            line.transform.position = new Vector3(rowWidth * 0.5f, 0f, -k_SlabDepth * 0.5f - 0.01f);
            line.GetComponent<Renderer>().sharedMaterial = UnlitColour(new Color(1f, 0.25f, 0.2f));
        }

        private static Material UnlitColour(Color colour)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", colour);
            return material;
        }

        /// <summary>
        /// Lowest world-space Y two ways: from <see cref="Renderer.bounds"/> and from the baked
        /// geometry (<see cref="PrefabGeometry"/>). They agree for rigid meshes and disagree for
        /// skinned ones, whose bounds are a padded box rather than a fit to the vertices.
        /// </summary>
        private static (float BoundsLowest, float VertexLowest) MeasureLowest(GameObject instance)
        {
            float boundsLowest = float.MaxValue;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is ParticleSystemRenderer))
                {
                    boundsLowest = Mathf.Min(boundsLowest, renderer.bounds.min.y);
                }
            }

            return boundsLowest == float.MaxValue
                ? (0f, 0f)
                : (boundsLowest, PrefabGeometry.LowestY(instance));
        }
    }
}
