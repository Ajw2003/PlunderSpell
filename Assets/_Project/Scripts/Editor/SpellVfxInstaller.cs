using RogueAi.Spells.Vfx;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// Puts a <see cref="SpellVfxDirector"/> into the authored scenes, with the bolt assigned.
    /// Idempotent, and a targeted edit rather than a scene rebuild — the raid scene is hand-authored.
    ///
    /// See docs/systems/spells.md, "Seeing a cast".
    /// </summary>
    public static class SpellVfxInstaller
    {
        private const string k_BoltPath = "Assets/_Project/Prefabs/Projectiles/Bolt.prefab";

        private static readonly string[] k_Scenes =
        {
            "Assets/_Project/Scenes/RaidScene.unity",
            "Assets/_Project/Scenes/CombatBench.unity",
        };

        [MenuItem("Tools/Plunderspell/Install Spell VFX")]
        public static void InstallSpellVfx()
        {
            var bolt = AssetDatabase.LoadAssetAtPath<GameObject>(k_BoltPath);
            if (bolt == null)
            {
                Debug.LogError($"[SpellVfx] No projectile at {k_BoltPath}. Run Forge Projectile Prefab first.");
                return;
            }

            foreach (string scenePath in k_Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                var director = Object.FindFirstObjectByType<SpellVfxDirector>();
                if (director == null)
                {
                    var go = new GameObject("SpellVfx");
                    director = go.AddComponent<SpellVfxDirector>();
                }

                var serialized = new SerializedObject(director);
                serialized.FindProperty("m_boltPrefab").objectReferenceValue = bolt;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, scenePath))
                {
                    Debug.LogError($"[SpellVfx] Could not save {scenePath}.");
                    continue;
                }

                Debug.Log($"[SpellVfx] Installed in {scenePath}.");
            }
        }
    }
}
