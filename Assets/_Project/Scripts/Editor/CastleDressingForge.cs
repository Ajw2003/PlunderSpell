using System.IO;
using System.Linq;
using Plunderspell.Castle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Turns the pipeline's dressing models (Assets/_Project/Art/Models/Castle/Dressing) into prefabs
    /// set up the way the room prefabs are (upright root, a mesh collider so the NavMesh walks round
    /// them), fills <see cref="CastleDressingSet"/>, imports their fire anchors, and hands the set to
    /// every castle generator in the open scene. Idempotent. See docs/plans/night-atmosphere.md,
    /// section 4.
    /// </summary>
    public static class CastleDressingForge
    {
        private const string ModelDir = "Assets/_Project/Art/Models/Castle/Dressing";
        private const string PrefabDir = "Assets/_Project/Prefabs/Castle/Dressing";

        // The Blender Z-up to Unity Y-up correction every castle prefab carries on its root.
        private static readonly Vector3 CastleUprightEuler = new Vector3(90f, 0f, 0f);

        [MenuItem("Tools/Plunderspell/Night Atmosphere/Build Castle Dressing")]
        public static void BuildMenu() => Debug.Log(Build());

        /// <summary>Builds the prefabs and the set, and wires the open scene. Returns a report.</summary>
        public static string Build()
        {
            if (!AssetDatabase.IsValidFolder(PrefabDir))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs/Castle", "Dressing");

            var set = AssetDatabase.LoadAssetAtPath<CastleDressingSet>(CastleFireAnchorImporter.DressingSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CastleDressingSet>();
                AssetDatabase.CreateAsset(set, CastleFireAnchorImporter.DressingSetPath);
            }
            set.Entries.Clear();

            string[] models = Directory.GetFiles(ModelDir, "*.fbx").Select(p => p.Replace('\\', '/')).OrderBy(p => p).ToArray();
            foreach (string modelPath in models)
            {
                string key = Path.GetFileNameWithoutExtension(modelPath);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                GameObject prefab = BuildPrefab(model, key);
                (DressingKind kind, CastleZone zone, int weight) = Describe(key);
                set.Entries.Add(new CastleDressingSet.Entry
                {
                    Id = key,
                    Kind = kind,
                    Zone = zone,
                    Prefab = prefab,
                    Weight = weight,
                });
            }
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();

            string fires = CastleFireAnchorImporter.Import();

            int wired = 0;
            foreach (ProceduralCastleGenerator generator in Object.FindObjectsByType<ProceduralCastleGenerator>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(generator);
                so.FindProperty("m_dressing").objectReferenceValue = set;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
                wired++;
            }

            return $"[CastleDressing] {set.Entries.Count} dressings in {CastleFireAnchorImporter.DressingSetPath}; " +
                   $"{wired} generator(s) in the open scene wired. {fires}";
        }

        /// <summary>What a pipeline key is used for, and how often a curtain dressing is picked.</summary>
        private static (DressingKind, CastleZone, int) Describe(string key)
        {
            switch (key)
            {
                case "DressingGateYard": return (DressingKind.GateYard, CastleZone.CurtainWall, 1);
                case "DressingGateSealed": return (DressingKind.GateSealed, CastleZone.CurtainWall, 1);
                case "DressingPlain": return (DressingKind.Curtain, CastleZone.CurtainWall, 1);
                case "CourtyardHerbGarden":
                case "CourtyardMidden":
                case "CourtyardWellYard":
                    return (DressingKind.Courtyard, CastleZone.OuterBailey, 1);
                case "CourtyardTiltyard":
                case "CourtyardCloisterGarth":
                    return (DressingKind.Courtyard, CastleZone.InnerWard, 1);
                case "CourtyardFormalGarden":
                    return (DressingKind.Courtyard, CastleZone.Keep, 1);
                default:
                    return (DressingKind.Curtain, CastleZone.CurtainWall, 2);
            }
        }

        private static GameObject BuildPrefab(GameObject model, string key)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = key;
                instance.transform.localEulerAngles = CastleUprightEuler;
                foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh != null && !filter.TryGetComponent(out MeshCollider _))
                        filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                }
                return PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabDir}/{key}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
