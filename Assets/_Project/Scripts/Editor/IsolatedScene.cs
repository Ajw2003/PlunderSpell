using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace RogueAi.EditorTools
{
    /// <summary>
    /// A clean, empty scene to photograph in, which puts back the scene the Editor had open when it
    /// is disposed. Shared by the screenshot forges so each does not carry its own copy of the swap.
    /// </summary>
    public sealed class IsolatedScene : IDisposable
    {
        private readonly string _originalScenePath;

        private IsolatedScene(string originalScenePath)
        {
            _originalScenePath = originalScenePath;
        }

        /// <summary>
        /// Swaps to an empty scene. Returns null, after logging why, when this Unity cannot render
        /// or the open scene has unsaved changes that swapping would discard.
        /// </summary>
        public static IsolatedScene Enter(string toolName)
        {
            if (!SceneScreenshot.HasGraphicsDevice)
            {
                Debug.LogError($"[{toolName}] No graphics device. Re-run without -nographics.");
                return null;
            }

            Scene original = SceneManager.GetActiveScene();
            if (original.isDirty)
            {
                Debug.LogError($"[{toolName}] '{original.name}' has unsaved changes; save or discard " +
                               "them first, this tool swaps scenes to photograph in a clean one.");
                return null;
            }

            string originalPath = original.path;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return new IsolatedScene(originalPath);
        }

        public void Dispose()
        {
            if (!string.IsNullOrEmpty(_originalScenePath))
            {
                EditorSceneManager.OpenScene(_originalScenePath, OpenSceneMode.Single);
            }
        }

        /// <summary>A key and a fill light plus flat ambient, so nothing photographs black.</summary>
        public static void AddStudioLighting()
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
        }

        /// <summary>A URP Unlit material of one flat colour, for backdrops and marker lines.</summary>
        public static Material UnlitColour(Color colour)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", colour);
            return material;
        }
    }
}
