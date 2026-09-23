using System.Collections.Generic;
using System.IO;
using System.Text;
using RogueAi.Loot;
using UnityEditor;
using UnityEngine;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Photographs every loot prefab twice — as the player sees it unfocused, and with the glow
    /// <see cref="LootInteractor"/> applies when it is looked at — and measures how many pixels
    /// actually changed. Issue <see href="https://github.com/Ajw2003/PlunderSpell/issues/15"/>.
    ///
    /// The measurement is the point: a glow that is switched on in code but changes no pixels is
    /// invisible to the player, and no unit test of the component can tell the difference.
    /// </summary>
    public static class LootHighlightScreenshotForge
    {
        private const string k_LootPrefabDirectory = "Assets/_Project/Prefabs/Loot";
        private const string k_DefaultOutputFolder = "docs/generated/loot-highlight-screenshots";

        private const int k_ImageSize = 700;

        /// <summary>A channel must move by more than this (out of 255) to count as a changed pixel,
        /// so encoder noise is not mistaken for a glow.</summary>
        private const int k_ChangedChannelThreshold = 6;

        [MenuItem("Tools/Plunderspell/Capture Loot Highlight Screenshots")]
        public static void CaptureAll()
        {
            Capture(k_DefaultOutputFolder);
        }

        /// <summary>
        /// Photographs the loot into <paramref name="outputFolder"/> (project-relative) and returns
        /// the report, which is also written to <c>highlight-report.txt</c> there.
        /// </summary>
        public static string Capture(string outputFolder)
        {
            using (IsolatedScene scene = IsolatedScene.Enter("LootHighlight"))
            {
                if (scene == null)
                {
                    return null;
                }

                string directory = Path.Combine(Directory.GetCurrentDirectory(), outputFolder);
                Directory.CreateDirectory(directory);
                return CaptureInFreshScene(directory, outputFolder);
            }
        }

        private static string CaptureInFreshScene(string directory, string outputFolder)
        {
            IsolatedScene.AddStudioLighting();

            var report = new StringBuilder();
            report.AppendLine("Loot highlight report — each prefab photographed unfocused, then with LootHighlight on.");
            report.AppendLine("changed = share of the frame's pixels that moved by more than " +
                              $"{k_ChangedChannelThreshold}/255 in any channel; " +
                              "lift = mean brightness gain (0-255) over those pixels.");
            report.AppendLine();

            var prefabs = new List<GameObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { k_LootPrefabDirectory }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null)
                {
                    prefabs.Add(prefab);
                }
            }

            prefabs.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            foreach (GameObject prefab in prefabs)
            {
                CaptureOne(prefab, directory, report);
            }

            File.WriteAllText(Path.Combine(directory, "highlight-report.txt"), report.ToString());
            Debug.Log($"[LootHighlight] Wrote {prefabs.Count} comparisons and highlight-report.txt to {outputFolder}.");
            return report.ToString();
        }

        private static void CaptureOne(GameObject prefab, string directory, StringBuilder report)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // The real path is LootInteractor.SetHighlight: AddComponent, then SetHighlighted. Awake
            // does not run outside Play Mode, and it is what fills the renderer list, so the list is
            // filled through the same serialized field an Inspector user would set.
            var highlight = instance.AddComponent<LootHighlight>();
            var serialized = new SerializedObject(highlight);
            SerializedProperty renderers = serialized.FindProperty("m_renderers");
            Renderer[] found = instance.GetComponentsInChildren<Renderer>(true);
            renderers.arraySize = found.Length;
            for (int i = 0; i < found.Length; i++)
            {
                renderers.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            Bounds bounds = found[0].bounds;
            foreach (Renderer renderer in found)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            // Camera framing: the whole item with a margin, from a slightly raised three-quarter view.
            float distance = bounds.extents.magnitude / Mathf.Tan(35f * Mathf.Deg2Rad) * 1.25f;
            Vector3 direction = new Vector3(0.35f, 0.3f, -1f).normalized;
            Vector3 position = bounds.center + direction * distance;
            Quaternion rotation = Quaternion.LookRotation(bounds.center - position, Vector3.up);

            string unfocusedPath = Path.Combine(directory, $"loot-{prefab.name}-unfocused.png");
            string focusedPath = Path.Combine(directory, $"loot-{prefab.name}-focused.png");

            highlight.SetHighlighted(false);
            SceneScreenshot.Capture(position, rotation, false, 0f, unfocusedPath, k_ImageSize, k_ImageSize);
            highlight.SetHighlighted(true);
            SceneScreenshot.Capture(position, rotation, false, 0f, focusedPath, k_ImageSize, k_ImageSize);

            (float changed, float lift) = Compare(unfocusedPath, focusedPath,
                Path.Combine(directory, $"loot-{prefab.name}-compare.png"));

            report.AppendLine($"{prefab.name,-14} emission keyword on: {AllMaterialsEmissive(found),-5}  " +
                              $"changed {changed * 100f,6:F2}%   lift {lift,6:F1}");

            Object.DestroyImmediate(instance);
        }

        private static bool AllMaterialsEmissive(Renderer[] renderers)
        {
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material != null && !material.IsKeywordEnabled("_EMISSION"))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Share of pixels that changed and how much brighter they got; also writes the pair
        /// side by side so one image shows the whole before/after.</summary>
        private static (float ChangedShare, float Lift) Compare(string beforePath, string afterPath,
            string sideBySidePath)
        {
            var before = new Texture2D(2, 2, TextureFormat.RGB24, false);
            var after = new Texture2D(2, 2, TextureFormat.RGB24, false);
            before.LoadImage(File.ReadAllBytes(beforePath));
            after.LoadImage(File.ReadAllBytes(afterPath));

            Color32[] a = before.GetPixels32();
            Color32[] b = after.GetPixels32();
            int changed = 0;
            float liftSum = 0f;

            for (int i = 0; i < a.Length; i++)
            {
                int delta = Mathf.Max(Mathf.Abs(b[i].r - a[i].r),
                    Mathf.Max(Mathf.Abs(b[i].g - a[i].g), Mathf.Abs(b[i].b - a[i].b)));
                if (delta > k_ChangedChannelThreshold)
                {
                    changed++;
                    liftSum += (b[i].r + b[i].g + b[i].b - a[i].r - a[i].g - a[i].b) / 3f;
                }
            }

            int width = before.width;
            int height = before.height;
            var sheet = new Texture2D(width * 2, height, TextureFormat.RGB24, false);
            sheet.SetPixels32(0, 0, width, height, a);
            sheet.SetPixels32(width, 0, width, height, b);
            sheet.Apply();
            File.WriteAllBytes(sideBySidePath, sheet.EncodeToPNG());

            Object.DestroyImmediate(before);
            Object.DestroyImmediate(after);
            Object.DestroyImmediate(sheet);

            return (changed / (float)a.Length, changed == 0 ? 0f : liftSum / changed);
        }
    }
}
