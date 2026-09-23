using System.Text;
using NUnit.Framework;
using RogueAi.EditorTools;
using RogueAi.Loot;
using UnityEditor;
using UnityEngine;

namespace RogueAi.Tests.Editor
{
    /// <summary>
    /// Proves the focus glow can be seen, not just that the component runs. Issue
    /// <see href="https://github.com/Ajw2003/PlunderSpell/issues/15"/>.
    ///
    /// <c>LootHighlight</c> sets an emission colour, which a URP Lit material ignores unless its
    /// <c>_EMISSION</c> keyword is on. Every loot material once shipped with it off and the glow
    /// changed no pixels, while a test of the component alone would have passed.
    /// </summary>
    public class LootHighlightTests
    {
        private const string k_LootPrefabDirectory = "Assets/_Project/Prefabs/Loot";

        [Test]
        public void Test_EveryLootMaterialCanGlow()
        {
            var failures = new StringBuilder();
            int measured = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { k_LootPrefabDirectory }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null)
                {
                    continue;
                }

                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null)
                        {
                            continue;
                        }

                        measured++;
                        if (!material.HasProperty("_EmissionColor") || !material.IsKeywordEnabled("_EMISSION"))
                        {
                            failures.AppendLine($"  {prefab.name} / {material.name}: emission keyword is off, " +
                                                $"so the focus glow would draw nothing. Reimport {LootMaterialImportSettings.LootModelRoot}.");
                        }
                    }
                }
            }

            Assert.Greater(measured, 0, "No loot materials found, so this asserted nothing.");
            Assert.IsEmpty(failures.ToString(), $"Loot materials that cannot glow:\n{failures}");
        }

        [Test]
        public void Test_FocusingLootRaisesItsEmissionAndUnfocusingRestoresIt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{k_LootPrefabDirectory}/GoldenGoblet.prefab");
            Assert.IsNotNull(prefab, "GoldenGoblet prefab is missing.");

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                // Awake does not run outside Play Mode, and it is what fills the renderer list.
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

                highlight.SetHighlighted(true);
                Assert.Greater(EmissionOf(found[0]), 0.1f, "Focused loot has no emission.");

                highlight.SetHighlighted(false);
                Assert.AreEqual(0f, EmissionOf(found[0]), 0.001f, "Unfocused loot still glows.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static float EmissionOf(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetColor("_EmissionColor").maxColorComponent;
        }
    }
}
