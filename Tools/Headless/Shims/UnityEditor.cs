// Shim of the UnityEditor surface the project's editor tooling uses.
//
// Editor scripts are where a Unity project quietly rots: nothing compiles them in CI, and a broken
// one is only discovered by opening the editor. Compiling them headlessly costs this file and keeps
// the scene builders honest. Nothing here DOES anything — these tools cannot meaningfully run
// outside the editor — but a compile error in them is caught before someone opens Unity.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
        public MenuItem(string itemName, bool isValidateFunction) { }
        public MenuItem(string itemName, bool isValidateFunction, int priority) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CustomEditor : Attribute
    {
        public CustomEditor(Type inspectedType) { }
        public CustomEditor(Type inspectedType, bool editorForChildClasses) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOnLoadAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class InitializeOnLoadMethodAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class DidReloadScripts : Attribute
    {
        public DidReloadScripts() { }
        public DidReloadScripts(int order) { }
    }

    public class Editor : ScriptableObject
    {
        public UnityEngine.Object target { get; set; }
        public SerializedObject serializedObject { get; } = new SerializedObject();
        public virtual void OnInspectorGUI() { }
        public void DrawDefaultInspector() { }
        public static Editor CreateEditor(UnityEngine.Object obj) => null;
    }

    public class EditorWindow : ScriptableObject
    {
        public string title { get; set; }
        public Rect position { get; set; }
        public static T GetWindow<T>() where T : EditorWindow => CreateInstance<T>();
        public static T GetWindow<T>(string title) where T : EditorWindow => CreateInstance<T>();
        public void Show() { }
        public void Close() { }
        public void Repaint() { }
        public virtual void OnGUI() { }
    }

    public class SerializedObject
    {
        public SerializedObject() { }
        public SerializedObject(UnityEngine.Object obj) { }
        public SerializedProperty FindProperty(string path) => new SerializedProperty();
        public void Update() { }
        public bool ApplyModifiedProperties() => true;
        public void ApplyModifiedPropertiesWithoutUndo() { }
    }

    public class SerializedProperty
    {
        public int intValue { get; set; }
        public float floatValue { get; set; }
        public bool boolValue { get; set; }
        public string stringValue { get; set; }
        public UnityEngine.Object objectReferenceValue { get; set; }
    }

    public static class EditorGUILayout
    {
        public static void LabelField(string label) { }
        public static void LabelField(string label, string value) { }
        public static void LabelField(string label, GUIStyle style) { }
        public static void LabelField(string label, string value, GUIStyle style) { }
        public static void HelpBox(string message, MessageType type) { }
        public static int IntField(string label, int value) => value;
        public static float FloatField(string label, float value) => value;
        public static bool Toggle(string label, bool value) => value;
        public static string TextField(string label, string value) => value;
        public static void Space() { }
        public static void PropertyField(SerializedProperty property) { }
        public static void BeginHorizontal() { }
        public static void EndHorizontal() { }
        public static void BeginVertical() { }
        public static void EndVertical() { }
    }

    public static class EditorStyles
    {
        public static GUIStyle boldLabel { get; } = new GUIStyle { fontStyle = FontStyle.Bold };
        public static GUIStyle label { get; } = new GUIStyle();
        public static GUIStyle miniLabel { get; } = new GUIStyle();
        public static GUIStyle helpBox { get; } = new GUIStyle();
        public static GUIStyle toolbarButton { get; } = new GUIStyle();
    }

    public static class EditorGUI
    {
        public static bool EndChangeCheck() => false;
        public static void BeginChangeCheck() { }
    }

    public enum MessageType { None, Info, Warning, Error }

    public static class EditorUtility
    {
        public static bool DisplayDialog(string title, string message, string ok) => true;
        public static bool DisplayDialog(string title, string message, string ok, string cancel) => true;
        public static void DisplayProgressBar(string title, string info, float progress) { }
        public static bool DisplayCancelableProgressBar(string title, string info, float progress) => false;
        public static void ClearProgressBar() { }
        public static void SetDirty(UnityEngine.Object target) { }
        public static string OpenFilePanel(string title, string directory, string extension) => string.Empty;
        public static string SaveFilePanel(string title, string directory, string name, string extension) => string.Empty;
    }

    public enum ImportAssetOptions { Default, ForceUpdate, ForceSynchronousImport, ForceUncompressedImport }

    public static class AssetDatabase
    {
        public static void CreateAsset(UnityEngine.Object asset, string path) { }
        public static void SaveAssets() { }
        public static void Refresh() { }
        public static void Refresh(ImportAssetOptions options) { }
        public static string GenerateUniqueAssetPath(string path) => path;
        public static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object => null;
        public static UnityEngine.Object[] LoadAllAssetsAtPath(string path) => Array.Empty<UnityEngine.Object>();
        public static string GetAssetPath(UnityEngine.Object asset) => string.Empty;
        public static string[] FindAssets(string filter) => Array.Empty<string>();
        public static string[] FindAssets(string filter, string[] searchInFolders) => Array.Empty<string>();
        public static string GUIDToAssetPath(string guid) => string.Empty;
        public static string AssetPathToGUID(string path) => string.Empty;
        public static bool IsValidFolder(string path) => true;
        public static string CreateFolder(string parent, string name) => $"{parent}/{name}";
        public static bool DeleteAsset(string path) => true;
        public static void ImportAsset(string path) { }
        public static void ImportAsset(string path, ImportAssetOptions options) { }
    }

    public enum InteractionMode { UserAction, AutomatedAction }

    public static class PrefabUtility
    {
        public static GameObject SaveAsPrefabAsset(GameObject root, string path) => root;
        public static GameObject SaveAsPrefabAsset(GameObject root, string path, out bool success)
        {
            success = true;
            return root;
        }
        public static UnityEngine.Object InstantiatePrefab(UnityEngine.Object target) => target;
        public static UnityEngine.Object InstantiatePrefab(UnityEngine.Object target, Scene scene) => target;

        /// <summary>No real prefab asset headlessly, so this returns an empty stand-in root.</summary>
        public static GameObject LoadPrefabContents(string path) => new GameObject("PrefabContents");
        public static void UnloadPrefabContents(GameObject root) { }
        public static bool IsPartOfPrefabInstance(UnityEngine.Object obj) => false;
        public static GameObject SaveAsPrefabAssetAndConnect(GameObject root, string path, InteractionMode mode) => root;
        public static UnityEngine.Object GetCorrespondingObjectFromSource(UnityEngine.Object instance) => null;
    }

    public static class Selection
    {
        public static GameObject activeGameObject { get; set; }
        public static UnityEngine.Object activeObject { get; set; }
        public static UnityEngine.Object[] objects { get; set; } = Array.Empty<UnityEngine.Object>();
    }

    public static class Undo
    {
        public static void RecordObject(UnityEngine.Object target, string name) { }
        public static void RegisterCreatedObjectUndo(UnityEngine.Object target, string name) { }
        public static T AddComponent<T>(GameObject go) where T : Component => go.AddComponent<T>();
        public static void DestroyObjectImmediate(UnityEngine.Object target) => UnityEngine.Object.DestroyImmediate(target);
    }

    public static class EditorApplication
    {
        public static bool isPlaying { get; set; }
        public static bool isCompiling => false;
        public static event Action update;
        /// <summary>A delegate field, as in Unity (subscribe with +=). Nothing drains it headlessly.</summary>
        public static Action delayCall;

        /// <summary>No real process to exit headlessly -- a no-op, not Environment.Exit.</summary>
        public static void Exit(int returnValue) { }
    }

    public static class EditorPrefs
    {
        private static readonly Dictionary<string, object> _values = new Dictionary<string, object>();
        public static void SetBool(string key, bool value) => _values[key] = value;
        public static bool GetBool(string key, bool def = false) => _values.TryGetValue(key, out object v) && v is bool b ? b : def;
        public static void SetString(string key, string value) => _values[key] = value;
        public static string GetString(string key, string def = "") => _values.TryGetValue(key, out object v) && v is string s ? s : def;
        public static void SetInt(string key, int value) => _values[key] = value;
        public static int GetInt(string key, int def = 0) => _values.TryGetValue(key, out object v) && v is int i ? i : def;
    }

    /// <summary>
    /// Only exists so the postprocessors (CastleMeshImportSettings, ArtBibleModelImporter) compile
    /// headlessly; a postprocessor's callbacks are never invoked outside a real asset import, so
    /// nothing here does anything.
    /// </summary>
    public class AssetPostprocessor
    {
        public string assetPath { get; set; } = string.Empty;
        public AssetImporter assetImporter { get; set; }
        public UnityEditor.AssetImporters.AssetImportContext context { get; set; }
    }

    /// <summary>
    /// No importer exists headlessly: GetAtPath always returns null, so a validator reading import
    /// settings reports every asset as not imported (see Tools/Headless/README.md).
    /// </summary>
    public class AssetImporter
    {
        public string assetPath { get; set; } = string.Empty;
        public static AssetImporter GetAtPath(string path) => null;
        public void SaveAndReimport() { }
    }

    public enum ModelImporterAnimationType { None, Legacy, Generic, Human }
    public enum ModelImporterAvatarSetup { NoAvatar, CreateFromThisModel, CopyFromOther }
    public enum ModelImporterMaterialImportMode { None, ImportStandard, ImportViaMaterialDescription }

    public class ModelImporter : AssetImporter
    {
        public bool isReadable;
        public bool bakeAxisConversion;
        public float globalScale = 1f;
        public bool useFileScale = true;
        public bool importAnimation = true;
        public string motionNodeName = string.Empty;
        public ModelImporterAnimationType animationType = ModelImporterAnimationType.Generic;
        public ModelImporterAvatarSetup avatarSetup = ModelImporterAvatarSetup.NoAvatar;
        public ModelImporterMaterialImportMode materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        public HumanDescription humanDescription;
    }

    public enum TextureImporterType { Default, NormalMap, GUI, Sprite, Cursor, Cookie, Lightmap, SingleChannel }
    public enum TextureImporterCompression { Uncompressed, Compressed, CompressedHQ, CompressedLQ }

    public class TextureImporter : AssetImporter
    {
        public TextureImporterType textureType;
        public bool sRGBTexture = true;
        public bool mipmapEnabled = true;
        public bool isReadable;
        public int maxTextureSize = 2048;
        public TextureImporterCompression textureCompression = TextureImporterCompression.Compressed;
    }
}

namespace UnityEditor.SceneManagement
{
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public enum OpenSceneMode { Single, Additive, AdditiveWithoutLoading }

    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) =>
            new Scene { name = "GeneratedScene" };
        public static bool SaveScene(Scene scene, string path) => true;
        public static void MarkSceneDirty(Scene scene) { }
        public static Scene OpenScene(string path) => new Scene { name = path };
        public static Scene OpenScene(string path, OpenSceneMode mode) => new Scene { name = path };
        public static bool CloseScene(Scene scene, bool removeScene) => true;

        /// <summary>
        /// No real .unity deserialization headlessly, so this returns an empty stand-in scene rather
        /// than loading RaidScene.unity's actual content. See Tools/Headless/README.md, "What it does
        /// and does not prove" -- RaidSceneCastingTests fails on this for exactly that reason.
        /// </summary>
        public static AsyncOperation LoadSceneAsyncInPlayMode(string path, LoadSceneParameters parameters) =>
            new AsyncOperation();
    }
}

namespace UnityEditor.AssetImporters
{
    /// <summary>Import dependencies are recorded by the real importer only; nothing is tracked here.</summary>
    public class AssetImportContext
    {
        public void DependsOnArtifact(string path) { }
        public void DependsOnSourceAsset(string path) { }
    }
}
