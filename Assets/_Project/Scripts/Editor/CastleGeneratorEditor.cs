using System.Collections.Generic;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Custom inspector for <see cref="ProceduralCastleGenerator"/> giving designers one-click
    /// preview, clearing and path validation directly in the editor.
    /// </summary>
    [CustomEditor(typeof(ProceduralCastleGenerator))]
    public class CastleGeneratorEditor : UnityEditor.Editor
    {
        private int _seedField = 42;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var generator = (ProceduralCastleGenerator)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Castle Generator Tools", EditorStyles.boldLabel);

            _seedField = EditorGUILayout.IntField("Preview Seed", _seedField);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Preview (Random Seed)"))
            {
                int seed = Random.Range(1, int.MaxValue);
                _seedField = seed;
                generator.Generate(seed);
                Debug.Log($"[CastleGenEditor] Generated preview with random seed {seed}.");
            }

            if (GUILayout.Button("Generate Preview (Seed Field)"))
            {
                generator.Generate(_seedField);
                Debug.Log($"[CastleGenEditor] Generated preview with seed {_seedField}.");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear Generated"))
            {
                generator.ClearGenerated();
                Debug.Log("[CastleGenEditor] Cleared generated rooms.");
            }

            if (GUILayout.Button("Validate Path"))
            {
                ValidateCurrent(generator);
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void ValidateCurrent(ProceduralCastleGenerator generator)
        {
            ProceduralCastleData data = generator.LastGenerated;
            if (data == null)
            {
                EditorUtility.DisplayDialog("Castle Path Validation",
                    "No castle has been generated yet. Generate a preview first.", "OK");
                return;
            }

            bool ok = CastlePathValidator.ValidatePath(data, out List<Vector2Int> path);
            string message = ok
                ? $"Path found!\n\nSeed: {data.Seed}\nModules: {data.PlacedModules.Count}\n" +
                  $"Path length: {path.Count} cells (crypt \u2192 extraction)."
                : $"NO valid path found.\n\nSeed: {data.Seed}\nModules: {data.PlacedModules.Count}\n" +
                  "The crypt and extraction exit are not connected.";

            EditorUtility.DisplayDialog("Castle Path Validation", message, "OK");
        }
    }
}
