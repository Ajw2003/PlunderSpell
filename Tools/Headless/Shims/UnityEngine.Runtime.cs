// Headless shim for the remaining UnityEngine runtime surface: attributes, logging, time, physics,
// input and persistence. Physics is a real (if simple) AABB world so acoustics can be tested.
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine
{
    // --- Attributes -------------------------------------------------------------------------

    [AttributeUsage(AttributeTargets.Field)] public class SerializeFieldAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class SerializeReferenceAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HideInInspector : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class NonReorderableAttribute : Attribute { }
    public class PropertyAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class HeaderAttribute : PropertyAttribute { public HeaderAttribute(string header) { } }
    public class TooltipAttribute : PropertyAttribute { public TooltipAttribute(string tooltip) { } }
    public class RangeAttribute : PropertyAttribute { public RangeAttribute(float min, float max) { } }
    public class MinAttribute : PropertyAttribute { public MinAttribute(float min) { } }
    public class TextAreaAttribute : PropertyAttribute
    {
        public TextAreaAttribute() { }
        public TextAreaAttribute(int minLines, int maxLines) { }
    }
    public class SpaceAttribute : PropertyAttribute { public SpaceAttribute() { } public SpaceAttribute(float height) { } }
    [AttributeUsage(AttributeTargets.Class)]
    public class DefaultExecutionOrderAttribute : Attribute { public DefaultExecutionOrderAttribute(int order) { } }
    [AttributeUsage(AttributeTargets.Field)]
    public class ColorUsageAttribute : PropertyAttribute
    {
        public ColorUsageAttribute(bool showAlpha) { }
        public ColorUsageAttribute(bool showAlpha, bool hdr) { }
    }
    [AttributeUsage(AttributeTargets.Class)]
    public class CreateAssetMenuAttribute : Attribute
    {
        public string fileName; public string menuName; public int order;
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireComponent : Attribute
    {
        /// <summary>The dependencies AddComponent must satisfy first, exactly as the editor does.</summary>
        public readonly Type[] Types;
        public RequireComponent(Type a) => Types = new[] { a };
        public RequireComponent(Type a, Type b) => Types = new[] { a, b };
        public RequireComponent(Type a, Type b, Type c) => Types = new[] { a, b, c };
    }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class AddComponentMenu : Attribute { public AddComponentMenu(string menu) { } }
    [AttributeUsage(AttributeTargets.Class)] public class ExecuteAlways : Attribute { }
    [AttributeUsage(AttributeTargets.Class)] public class ExecuteInEditMode : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class ContextMenu : Attribute { public ContextMenu(string name) { } }

    public enum RuntimeInitializeLoadType { AfterAssembliesLoaded, BeforeSplashScreen, BeforeSceneLoad, AfterSceneLoad, SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }

    // --- Logging ----------------------------------------------------------------------------

    /// <summary>Captures every log line so tests can assert on warnings/errors instead of losing them.</summary>
    public static class Debug
    {
        public enum Level { Log, Warning, Error, Assert }

        public readonly struct Entry
        {
            public readonly Level Severity;
            public readonly string Message;
            public Entry(Level severity, string message) { Severity = severity; Message = message; }
            public override string ToString() => $"[{Severity}] {Message}";
        }

        private static readonly List<Entry> _entries = new List<Entry>();

        /// <summary>Every line logged since the last <see cref="ClearLog"/>.</summary>
        public static IReadOnlyList<Entry> Log_Entries => _entries;
        /// <summary>When true, log lines are also written to stdout (useful when a test fails).</summary>
        public static bool EchoToConsole { get; set; }

        public static void ClearLog() => _entries.Clear();
        public static IEnumerable<Entry> EntriesOf(Level level) => _entries.Where(e => e.Severity == level);
        public static bool LoggedContaining(string fragment) =>
            _entries.Any(e => e.Message != null && e.Message.Contains(fragment));

        private static void Add(Level level, object message)
        {
            var entry = new Entry(level, message?.ToString() ?? "null");
            _entries.Add(entry);
            if (EchoToConsole) Console.WriteLine(entry);
        }

        public static void Log(object message) => Add(Level.Log, message);
        public static void Log(object message, Object context) => Add(Level.Log, message);
        public static void LogWarning(object message) => Add(Level.Warning, message);
        public static void LogWarning(object message, Object context) => Add(Level.Warning, message);
        public static void LogError(object message) => Add(Level.Error, message);
        public static void LogError(object message, Object context) => Add(Level.Error, message);
        public static void LogException(Exception e) => Add(Level.Error, e?.ToString());
        public static void LogFormat(string format, params object[] args) => Add(Level.Log, string.Format(format, args));
        public static void LogWarningFormat(string format, params object[] args) => Add(Level.Warning, string.Format(format, args));
        public static void LogErrorFormat(string format, params object[] args) => Add(Level.Error, string.Format(format, args));
        public static void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition) Add(Level.Assert, message);
        }
        public static void DrawLine(Vector3 a, Vector3 b, Color c, float duration = 0f) { }
        public static void DrawRay(Vector3 o, Vector3 d, Color c, float duration = 0f) { }
    }

    public static class Gizmos
    {
        public static Color color { get; set; }
        public static void DrawWireSphere(Vector3 center, float radius) { }
        public static void DrawSphere(Vector3 center, float radius) { }
        public static void DrawLine(Vector3 a, Vector3 b) { }
        public static void DrawWireCube(Vector3 center, Vector3 size) { }
        public static void DrawCube(Vector3 center, Vector3 size) { }
        public static void DrawRay(Vector3 origin, Vector3 dir) { }
    }

    // --- Time / application -----------------------------------------------------------------

    /// <summary>Deterministic clock: tests advance it explicitly with <see cref="Advance"/>.</summary>
    public static class Time
    {
        public static float time { get; private set; }
        public static float deltaTime { get; set; } = 1f / 60f;
        public static float fixedDeltaTime { get; set; } = 0.02f;
        public static float unscaledTime => time;
        public static float unscaledDeltaTime => deltaTime;
        public static float timeScale { get; set; } = 1f;
        public static int frameCount { get; private set; }
        public static float realtimeSinceStartup => time;

        /// <summary>Advance the simulated clock by <paramref name="seconds"/> and set deltaTime to match.</summary>
        public static void Advance(float seconds)
        {
            deltaTime = seconds;
            time += seconds;
            frameCount++;
        }

        public static void Reset()
        {
            time = 0f;
            frameCount = 0;
            deltaTime = 1f / 60f;
        }
    }

    public static class Application
    {
        public static bool isPlaying { get; set; } = true;
        public static bool isEditor => false;
        public static bool isBatchMode => true;
        public static string dataPath => AppDomain.CurrentDomain.BaseDirectory;
        public static string streamingAssetsPath => System.IO.Path.Combine(dataPath, "StreamingAssets");
        public static string persistentDataPath => System.IO.Path.Combine(dataPath, "Persistent");
        public static RuntimePlatform platform => RuntimePlatform.LinuxPlayer;
        public static void Quit() { }
    }

    public enum RuntimePlatform { WindowsPlayer, WindowsEditor, LinuxPlayer, LinuxEditor, OSXPlayer, OSXEditor }

    /// <summary>Deterministic stand-in for UnityEngine.Random — seedable so tests stay reproducible.</summary>
    public static class Random
    {
        private static System.Random _rng = new System.Random(12345);
        public static void InitState(int seed) => _rng = new System.Random(seed);
        public static float value => (float)_rng.NextDouble();
        public static float Range(float min, float max) => min + (float)_rng.NextDouble() * (max - min);
        public static int Range(int minInclusive, int maxExclusive) =>
            maxExclusive <= minInclusive ? minInclusive : _rng.Next(minInclusive, maxExclusive);
        public static Vector3 insideUnitSphere =>
            new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f)).normalized * value;
        public static Vector2 insideUnitCircle => new Vector2(Range(-1f, 1f), Range(-1f, 1f));
        public static Vector3 onUnitSphere => new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f)).normalized;
        public static Quaternion rotation => Quaternion.Euler(Range(0f, 360f), Range(0f, 360f), Range(0f, 360f));
    }

    public static class SystemInfo
    {
        public static Rendering.GraphicsDeviceType graphicsDeviceType => Rendering.GraphicsDeviceType.Null;
        public static string deviceName => "headless";
        public static string deviceModel => "headless";
        public static string graphicsDeviceName => "headless";
    }

    public static class Microphone
    {
        public static string[] devices => Array.Empty<string>();
        public static AudioClip Start(string device, bool loop, int lengthSec, int frequency) => null;
        public static void End(string device) { }
        public static int GetPosition(string device) => 0;
        public static void GetDeviceCaps(string device, out int min, out int max) { min = 0; max = 0; }
    }

    public enum AudioDataLoadState { Unloaded, Loading, Loaded, Failed }
    public enum AudioClipLoadType { DecompressOnLoad, CompressedInMemory, Streaming }
    public enum AudioRolloffMode { Logarithmic, Linear, Custom }

    public struct AudioConfiguration { public int sampleRate; public int dspBufferSize; }

    /// <summary>The audio engine's clock and config. Nothing plays, so dspTime is the game clock and Reset does nothing.</summary>
    public static class AudioSettings
    {
        public static double dspTime => Time.time;
        public static AudioConfiguration GetConfiguration() => new AudioConfiguration { sampleRate = 48000, dspBufferSize = 1024 };
        public static bool Reset(AudioConfiguration config) => true;
    }

    public class AudioClip : Object
    {
        public int channels = 1;
        public int frequency = 16000;
        public int samples;
        public AudioDataLoadState loadState = AudioDataLoadState.Loaded;
        public AudioClipLoadType loadType = AudioClipLoadType.DecompressOnLoad;
        public bool GetData(float[] data, int offset) => true;
    }

    public class AudioSource : Behaviour
    {
        public AudioClip clip;
        public float volume = 1f;
        public bool loop;
        public bool playOnAwake;
        public Audio.AudioMixerGroup outputAudioMixerGroup;
        public bool isPlaying { get; private set; }
        public void Play() => isPlaying = true;
        public void Stop() => isPlaying = false;
        public void PlayOneShot(AudioClip c, float volumeScale = 1f) { }
        public float pitch = 1f;
        public float spatialBlend;
        public float minDistance = 1f;
        public float maxDistance = 500f;
        public AudioRolloffMode rolloffMode;
        /// <summary>Stored only: playback never advances headlessly.</summary>
        public float time;
        public int timeSamples;
        /// <summary>Treated as started now: there is no audio clock to wait on.</summary>
        public void PlayScheduled(double dspTime) => isPlaying = true;
    }

    /// <summary>In-memory PlayerPrefs. Deterministic and resettable, unlike the real registry-backed one.</summary>
    /// <summary>Minimal JsonUtility: enough for the {"text":"..."} payloads the voice layer parses.</summary>
    public static class JsonUtility
    {
        public static T FromJson<T>(string json) where T : new()
        {
            var result = new T();
            if (string.IsNullOrEmpty(json)) return result;
            foreach (System.Reflection.FieldInfo f in typeof(T).GetFields())
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    json, "\"" + f.Name + "\"\\s*:\\s*\"([^\"]*)\"");
                if (match.Success && f.FieldType == typeof(string)) f.SetValue(result, match.Groups[1].Value);
            }
            return result;
        }

        public static string ToJson(object obj, bool prettyPrint = false)
        {
            var parts = new List<string>();
            foreach (System.Reflection.FieldInfo f in obj.GetType().GetFields())
                parts.Add($"\"{f.Name}\":\"{f.GetValue(obj)}\"");
            return "{" + string.Join(",", parts) + "}";
        }
    }

    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public static void SetInt(string key, int value) => _values[key] = value;
        public static void SetFloat(string key, float value) => _values[key] = value;
        public static void SetString(string key, string value) => _values[key] = value;
        public static int GetInt(string key, int def = 0) => _values.TryGetValue(key, out object v) && v is int i ? i : def;
        public static float GetFloat(string key, float def = 0f) => _values.TryGetValue(key, out object v) && v is float f ? f : def;
        public static string GetString(string key, string def = "") => _values.TryGetValue(key, out object v) && v is string s ? s : def;
        public static bool HasKey(string key) => _values.ContainsKey(key);
        public static void DeleteKey(string key) => _values.Remove(key);
        public static void DeleteAll() => _values.Clear();
        public static void Save() { }
    }

    // --- Rendering / presentation stubs ------------------------------------------------------

    public class Renderer : Component
    {
        public bool enabled = true;
        public Material material;
        public Material sharedMaterial;
        public Rendering.ShadowCastingMode shadowCastingMode = Rendering.ShadowCastingMode.On;
        public bool receiveShadows = true;
        public Rendering.LightProbeUsage lightProbeUsage = Rendering.LightProbeUsage.BlendProbes;

        /// <summary>A zero-size box at the renderer's position -- no real mesh extents headlessly.</summary>
        public Bounds bounds => new Bounds(transform.position, Vector3.zero);

        public Material[] sharedMaterials
        {
            get => _materials ?? (_materials = new[] { sharedMaterial });
            set { _materials = value; sharedMaterial = value != null && value.Length > 0 ? value[0] : null; }
        }
        private Material[] _materials;
        public void GetSharedMaterials(List<Material> results) { results.Clear(); results.AddRange(sharedMaterials); }
        public void SetSharedMaterials(List<Material> materials) => sharedMaterials = materials.ToArray();

        public void SetPropertyBlock(MaterialPropertyBlock block) { }
        public void GetPropertyBlock(MaterialPropertyBlock block) { }
    }
    public class MeshRenderer : Renderer { }

    public class TrailRenderer : Renderer { }

    /// <summary>Handed to the model-import hooks; no animation data exists headlessly.</summary>
    public class AnimationClip : Object { }

    public enum LineAlignment { View, TransformZ }

    /// <summary>Records the polyline and its look; nothing is drawn.</summary>
    public class LineRenderer : Renderer
    {
        private Vector3[] _positions = new Vector3[0];
        public bool useWorldSpace = true;
        public int numCapVertices;
        public int numCornerVertices;
        public LineAlignment alignment;
        public Gradient colorGradient;
        public float startWidth;
        public float endWidth;
        public int positionCount
        {
            get => _positions.Length;
            set => System.Array.Resize(ref _positions, value);
        }
        public void SetPositions(Vector3[] positions)
        {
            if (_positions.Length < positions.Length) System.Array.Resize(ref _positions, positions.Length);
            System.Array.Copy(positions, _positions, System.Math.Min(positions.Length, _positions.Length));
        }
        public void SetPosition(int index, Vector3 position) => _positions[index] = position;
        public Vector3 GetPosition(int index) => _positions[index];
    }

    public enum PhysicsMaterialCombine { Average, Minimum, Multiply, Maximum }

    public class PhysicsMaterial : Object
    {
        public float staticFriction = 0.6f;
        public float dynamicFriction = 0.6f;
        public float bounciness;
        public PhysicsMaterialCombine frictionCombine;
        public PhysicsMaterialCombine bounceCombine;
        public PhysicsMaterial() { }
        public PhysicsMaterial(string name) { this.name = name; }
    }
    public class SkinnedMeshRenderer : Renderer
    {
        public Mesh sharedMesh;

        /// <summary>No real skinning headlessly; copies sharedMesh's vertices unposed.</summary>
        public void BakeMesh(Mesh target, bool useScale)
        {
            if (target != null && sharedMesh != null)
                target.vertices = sharedMesh.vertices;
        }
    }
    public class Material : Object
    {
        public Color color;
        public int renderQueue = -1;
        public Shader shader;
        public Material() { }
        public Material(Material src) { }
        public Material(Shader shader) => this.shader = shader;
        public Texture mainTexture;
        public void SetColor(string name, Color value) { _colors[name] = value; if (name == "_Color" || name == "_BaseColor") color = value; }
        public void SetColor(int nameID, Color value) => SetColor(Shader.NameOf(nameID), value);
        private readonly Dictionary<string, float> _floats = new Dictionary<string, float>();
        private readonly Dictionary<string, Vector4> _vectors = new Dictionary<string, Vector4>();
        public void SetFloat(string name, float value) => _floats[name] = value;
        public void SetFloat(int nameID, float value) => SetFloat(Shader.NameOf(nameID), value);
        public float GetFloat(string name) => _floats.TryGetValue(name, out float f) ? f : 0f;
        public float GetFloat(int nameID) => GetFloat(Shader.NameOf(nameID));
        public void SetVector(string name, Vector4 value) => _vectors[name] = value;
        public void SetVector(int nameID, Vector4 value) => SetVector(Shader.NameOf(nameID), value);
        public Vector4 GetVector(string name) => _vectors.TryGetValue(name, out Vector4 v) ? v : Vector4.zero;
        public void SetTexture(int nameID, Texture value) => SetTexture(Shader.NameOf(nameID), value);
        public Texture GetTexture(int nameID) => GetTexture(Shader.NameOf(nameID));
        public Color GetColor(int nameID) => GetColor(Shader.NameOf(nameID));
        public MaterialGlobalIlluminationFlags globalIlluminationFlags;
        private readonly Dictionary<string, Color> _colors = new Dictionary<string, Color>();
        private readonly Dictionary<string, Texture> _textures = new Dictionary<string, Texture>();
        private readonly HashSet<string> _keywords = new HashSet<string>();
        public void SetTexture(string name, Texture value) => _textures[name] = value;
        public Texture GetTexture(string name) => _textures.TryGetValue(name, out Texture t) ? t : null;
        public Color GetColor(string name) => _colors.TryGetValue(name, out Color c) ? c : color;
        public void EnableKeyword(string keyword) => _keywords.Add(keyword);
        public void DisableKeyword(string keyword) => _keywords.Remove(keyword);
        public bool IsKeywordEnabled(string keyword) => _keywords.Contains(keyword);
        public bool HasProperty(string name) => true;
        public bool HasProperty(int nameID) => true;
    }
    public class Mesh : Object
    {
        public Vector3[] normals = Array.Empty<Vector3>();
        public Vector2[] uv = Array.Empty<Vector2>();
        public void Clear() { vertices = Array.Empty<Vector3>(); triangles = Array.Empty<int>(); normals = Array.Empty<Vector3>(); uv = Array.Empty<Vector2>(); }
        public void SetVertices(List<Vector3> list) => vertices = list.ToArray();
        public void SetNormals(List<Vector3> list) => normals = list.ToArray();
        /// <summary>Only channel 0 (<see cref="uv"/>) is kept.</summary>
        public void SetUVs(int channel, List<Vector2> list) { if (channel == 0) uv = list.ToArray(); }
        /// <summary>One submesh only: the shim keeps a single triangle list.</summary>
        public void SetTriangles(List<int> list, int submesh) => triangles = list.ToArray();
        /// <summary>No-op: <see cref="bounds"/> is always computed from the current vertices.</summary>
        public void RecalculateBounds() { }
        public int[] triangles = Array.Empty<int>();
        public Vector3[] vertices = Array.Empty<Vector3>();
        public int vertexCount => vertices.Length;

        /// <summary>Axis-aligned bounds of the vertices, as Unity recalculates them.</summary>
        public Bounds bounds
        {
            get
            {
                if (vertices.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);
                var b = new Bounds(vertices[0], Vector3.zero);
                for (int i = 1; i < vertices.Length; i++) b.Encapsulate(vertices[i]);
                return b;
            }
        }
    }
    public class MeshFilter : Component { public Mesh mesh; public Mesh sharedMesh; }
    public class Sprite : Object { }
    public class Texture : Object { }
    public enum TextureFormat { RGBA32, RGB24, Alpha8 }

    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }
    public enum FilterMode { Point, Bilinear, Trilinear }

    public class Texture2D : Texture
    {
        public int width { get; }
        public int height { get; }
        public TextureWrapMode wrapMode { get; set; }
        public FilterMode filterMode { get; set; } = FilterMode.Bilinear;

        /// <summary>Unity's built-in 4×4 white texture.</summary>
        public static Texture2D whiteTexture { get; } = new Texture2D(4, 4);
        /// <summary>No pixel storage headlessly; nothing reads pixels back.</summary>
        public void SetPixels(Color[] colours) { }
        public void SetPixels32(Color32[] colours) { }

        public Texture2D(int w, int h) { width = w; height = h; }
        public Texture2D(int w, int h, TextureFormat format, bool mipChain) { width = w; height = h; }
        public void SetPixel(int x, int y, Color colour) { }
        public void Apply() { }
        public void ReadPixels(Rect source, int destX, int destY) { }

        /// <summary>Real encoding needs a GPU readback this shim has none of; returns an empty PNG-ish stub.</summary>
        public byte[] EncodeToPNG() => Array.Empty<byte>();
    }

    /// <summary>No real off-screen rendering headlessly -- see Tools/Headless/README.md.</summary>
    public class RenderTexture : Object
    {
        public static RenderTexture active { get; set; }

        public int width;
        public int height;
        public int depth;

        public RenderTexture(int width, int height, int depth) { this.width = width; this.height = height; this.depth = depth; }

        public void Release() { }
    }
    public enum ParticleSystemStopBehavior { StopEmittingAndClear, StopEmitting }
    public enum ParticleSystemSimulationSpace { Local, World, Custom }
    public enum ParticleSystemShapeType { Sphere, Hemisphere, Cone, Box }

    /// <summary>
    /// Configuration is recorded, nothing is simulated. The modules are reference types here (they
    /// are write-through structs in Unity), so a write through <c>ps.main</c> persists the same way.
    /// </summary>
    public class ParticleSystem : Component
    {
        public struct MinMaxCurve
        {
            public float constantMin, constantMax;
            public MinMaxCurve(float constant) { constantMin = constantMax = constant; }
            public MinMaxCurve(float min, float max) { constantMin = min; constantMax = max; }
            public static implicit operator MinMaxCurve(float constant) => new MinMaxCurve(constant);
        }

        public struct MinMaxGradient
        {
            public Color color;
            public Color colorMax;
            public Gradient gradient;
            public MinMaxGradient(Color color) { this.color = color; colorMax = color; gradient = null; }
            public MinMaxGradient(Color min, Color max) { color = min; colorMax = max; gradient = null; }
            public MinMaxGradient(Gradient gradient) { color = Color.white; colorMax = Color.white; this.gradient = gradient; }
            public static implicit operator MinMaxGradient(Color color) => new MinMaxGradient(color);
            public static implicit operator MinMaxGradient(Gradient gradient) => new MinMaxGradient(gradient);
        }

        public class MainModule
        {
            public bool loop = true;
            public bool playOnAwake = true;
            public MinMaxCurve startLifetime = 5f;
            public MinMaxCurve startSpeed = 5f;
            public MinMaxCurve startSize = 1f;
            public MinMaxGradient startColor = Color.white;
            public float gravityModifier;
            public float duration = 5f;
            public ParticleSystemSimulationSpace simulationSpace;
            public int maxParticles = 1000;
        }

        public class EmissionModule
        {
            public bool enabled = true;
            public float rateOverTimeMultiplier = 10f;
            public MinMaxCurve rateOverTime = 10f;
        }

        public class ShapeModule
        {
            public bool enabled = true;
            public ParticleSystemShapeType shapeType = ParticleSystemShapeType.Cone;
            public float radius = 1f;
            public float angle = 25f;
        }

        public class NoiseModule
        {
            public bool enabled;
            public MinMaxCurve strength = 1f;
            public float frequency = 0.5f;
        }

        public class ColorOverLifetimeModule
        {
            public bool enabled;
            public MinMaxGradient color;
        }

        private readonly MainModule _main = new MainModule();
        private readonly EmissionModule _emission = new EmissionModule();
        private readonly ShapeModule _shape = new ShapeModule();
        public MainModule main => _main;
        public EmissionModule emission => _emission;
        public ShapeModule shape => _shape;
        private readonly NoiseModule _noise = new NoiseModule();
        private readonly ColorOverLifetimeModule _colorOverLifetime = new ColorOverLifetimeModule();
        public NoiseModule noise => _noise;
        public ColorOverLifetimeModule colorOverLifetime => _colorOverLifetime;

        public bool IsPlaying { get; private set; }
        /// <summary>Particles requested through <see cref="Emit"/>; they never age out headlessly.</summary>
        public int particleCount { get; private set; }
        public void Play() => IsPlaying = true;
        public void Stop() => IsPlaying = false;
        public void Stop(bool withChildren, ParticleSystemStopBehavior behavior)
        {
            IsPlaying = false;
            if (behavior == ParticleSystemStopBehavior.StopEmittingAndClear) particleCount = 0;
        }
        public void Emit(int count) => particleCount += count;
    }
    public class ParticleSystemRenderer : Renderer { }
    public enum LightShadows { None, Hard, Soft }

    public class Light : Behaviour
    {
        public float intensity = 1f;
        public Color color;
        public LightType type = LightType.Point;
        public float range = 10f;
        public LightShadows shadows = LightShadows.None;
        public float shadowStrength = 1f;
        public float shadowBias = 0.05f;
        public float shadowNormalBias = 0.4f;
        public float shadowNearPlane = 0.2f;
    }

    [Flags]
    public enum MaterialGlobalIlluminationFlags { None = 0, RealtimeEmissive = 1, BakedEmissive = 2, EmissiveIsBlack = 4, AnyEmissive = 3 }

    // Humanoid avatar description, as ModelImporter.humanDescription carries it. Data only.
    public struct HumanLimit
    {
        public bool useDefaultValues;
        public Vector3 min;
        public Vector3 max;
        public Vector3 center;
        public float axisLength;
    }

    public struct HumanBone
    {
        public string humanName;
        public string boneName;
        public HumanLimit limit;
    }

    public struct SkeletonBone
    {
        public string name;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    public struct HumanDescription
    {
        public HumanBone[] human;
        public SkeletonBone[] skeleton;
        public float upperArmTwist;
        public float lowerArmTwist;
        public float upperLegTwist;
        public float lowerLegTwist;
        public float armStretch;
        public float legStretch;
        public float feetSpacing;
        public bool hasTranslationDoF;
    }

    /// <summary>Only the two flags the import validator reads. No avatar is ever built headlessly.</summary>
    public class Avatar : Object
    {
        public bool isHuman { get; set; }
        public bool isValid { get; set; }
    }
    public struct Ray
    {
        public Vector3 origin;
        public Vector3 direction;
        public Ray(Vector3 origin, Vector3 direction) { this.origin = origin; this.direction = direction.normalized; }
        public Vector3 GetPoint(float distance) => origin + direction * distance;
    }

    public enum CameraClearFlags { Skybox, SolidColor, Depth, Nothing }

    public class Camera : Behaviour
    {
        private static Camera _main;
        /// <summary>Returns the first camera in the headless scene, mirroring Camera.main's tag lookup.</summary>
        public static Camera main => _main != null ? _main : (_main = Object.FindObjectOfType<Camera>());
        public float fieldOfView = 60f;
        public float depth;

        /// <summary>Every enabled camera on an active object, in the headless scene.</summary>
        private static List<Camera> Live() =>
            Object.FindObjectsOfType<Camera>().Where(c => c.isActiveAndEnabled).ToList();
        public static int allCamerasCount => Live().Count;
        public static int GetAllCameras(Camera[] cameras)
        {
            List<Camera> live = Live();
            int n = Math.Min(cameras.Length, live.Count);
            for (int i = 0; i < n; i++) cameras[i] = live[i];
            return n;
        }
        public CameraClearFlags clearFlags;
        public Color backgroundColor;
        public bool orthographic;
        public float orthographicSize = 5f;
        public float nearClipPlane = 0.3f;
        public float farClipPlane = 1000f;
        public int cullingMask = ~0;
        public RenderTexture targetTexture;

        public Ray ViewportPointToRay(Vector3 viewportPoint) => new Ray(transform.position, transform.forward);
        public Ray ScreenPointToRay(Vector3 screenPoint) => new Ray(transform.position, transform.forward);
        public Ray ScreenPointToRay(Vector2 screenPoint) => new Ray(transform.position, transform.forward);
        public Vector3 WorldToViewportPoint(Vector3 world) => new Vector3(0.5f, 0.5f, 1f);

        /// <summary>Stub projection -- see Tools/Headless/README.md, "What it does and does not prove".</summary>
        public Vector3 WorldToScreenPoint(Vector3 world) => new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 1f);

        /// <summary>No off-screen rendering headlessly -- see Tools/Headless/README.md.</summary>
        public void Render() { }
    }
    public class Canvas : Behaviour
    {
        public RenderMode renderMode;
        public Camera worldCamera;
        public float planeDistance = 100f;
        public int sortingOrder;
    }
    public class Animator : Behaviour
    {
        private readonly Dictionary<int, object> _params = new Dictionary<int, object>();
        public static int StringToHash(string name) => name?.GetHashCode() ?? 0;
        public void SetBool(int hash, bool value) => _params[hash] = value;
        public void SetBool(string name, bool value) => _params[StringToHash(name)] = value;
        public bool GetBool(int hash) => _params.TryGetValue(hash, out object v) && v is bool b && b;
        public bool GetBool(string name) => GetBool(StringToHash(name));
        public void SetFloat(int hash, float value) => _params[hash] = value;
        public void SetFloat(string name, float value) => _params[StringToHash(name)] = value;
        public float GetFloat(string name) => _params.TryGetValue(StringToHash(name), out object v) && v is float f ? f : 0f;
        public void SetTrigger(string name) => _params[StringToHash(name)] = true;
        public void SetInteger(string name, int value) => _params[StringToHash(name)] = value;
        public void Play(string state) { }
    }

    // --- Physics -----------------------------------------------------------------------------

    [Serializable]
    public struct LayerMask
    {
        public int value;
        public static implicit operator int(LayerMask m) => m.value;
        public static implicit operator LayerMask(int v) => new LayerMask { value = v };
        public static int GetMask(params string[] layerNames) => ~0;
        public static string LayerToName(int layer) => layer.ToString();
        public static int NameToLayer(string name) => 0;
    }

    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public enum CollisionDetectionMode { Discrete, Continuous, ContinuousDynamic, ContinuousSpeculative }
    public enum RigidbodyConstraints { None = 0, FreezePositionX = 2, FreezePositionY = 4, FreezePositionZ = 8, FreezeRotation = 112, FreezeAll = 126 }
    public enum ForceMode { Force, Acceleration, Impulse, VelocityChange }
    public enum ConfigurableJointMotion { Locked, Limited, Free }

    public enum RigidbodyInterpolation { None, Interpolate, Extrapolate }

    public class Rigidbody : Component
    {
        public RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
        public float maxAngularVelocity = 7f;
        public bool useGravity = true;
        public bool isKinematic;
        public bool detectCollisions = true;
        public float mass = 1f;
        public float drag;
        public float angularDrag = 0.05f;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public Vector3 linearVelocity { get => velocity; set => velocity = value; }
        public RigidbodyConstraints constraints = RigidbodyConstraints.None;
        public CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Discrete;
        public Vector3 position { get => transform.position; set => transform.position = value; }
        public Quaternion rotation { get => transform.rotation; set => transform.rotation = value; }
        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force) => velocity += force / Mathf.Max(mass, 0.0001f);
        public void AddExplosionForce(float force, Vector3 position, float radius) { }

        /// <summary>Local-space centre of mass; the shim never recomputes it from colliders.</summary>
        public Vector3 centerOfMass = Vector3.zero;
        public Vector3 worldCenterOfMass => transform.position + transform.rotation * centerOfMass;
        public Vector3 inertiaTensor = Vector3.one;
        public Quaternion inertiaTensorRotation = Quaternion.identity;
        /// <summary>Unity 6 name for <see cref="angularDrag"/>.</summary>
        public float angularDamping { get => angularDrag; set => angularDrag = value; }

        /// <summary>The point's velocity: the body's plus the spin about its centre of mass.</summary>
        public Vector3 GetPointVelocity(Vector3 worldPoint) =>
            velocity + Vector3.Cross(angularVelocity, worldPoint - worldCenterOfMass);

        /// <summary>Linear part only, applied like <see cref="AddForce"/>; no torque is derived from the offset (no rotational simulation).</summary>
        public void AddForceAtPosition(Vector3 force, Vector3 position, ForceMode mode = ForceMode.Force) => AddForce(force, mode);

        /// <summary>Applied instantly like <see cref="AddForce"/>, through the diagonal inertia tensor (principal-frame rotation ignored).</summary>
        public void AddTorque(Vector3 torque, ForceMode mode = ForceMode.Force) =>
            angularVelocity += new Vector3(
                torque.x / Mathf.Max(inertiaTensor.x, 0.0001f),
                torque.y / Mathf.Max(inertiaTensor.y, 0.0001f),
                torque.z / Mathf.Max(inertiaTensor.z, 0.0001f));
        public bool freezeRotation;
        public void MovePosition(Vector3 p) => transform.position = p;
        public void MoveRotation(Quaternion r) => transform.rotation = r;
        /// <summary>No sleeping bodies in this shim, so waking one does nothing.</summary>
        public void WakeUp() { }
        public bool IsSleeping() => false;
    }

    public class Joint : Component
    {
        public Rigidbody connectedBody;
        public Vector3 anchor;
        public Vector3 connectedAnchor;
        public bool autoConfigureConnectedAnchor = true;
        public float breakForce = float.PositiveInfinity;
    }

    public class ConfigurableJoint : Joint
    {
        public ConfigurableJointMotion xMotion, yMotion, zMotion;
        public ConfigurableJointMotion angularXMotion, angularYMotion, angularZMotion;
    }

    public class FixedJoint : Joint { }
    public class HingeJoint : Joint { }

    /// <summary>Axis-aligned box collider. Every live collider registers with <see cref="Physics"/>.</summary>
    public class Collider : Component
    {
        public bool isTrigger;
        public bool enabled = true;
        public Vector3 center = Vector3.zero;
        public Vector3 size = Vector3.one;
        /// <summary>Stored only: the AABB physics world has no friction model.</summary>
        public PhysicsMaterial sharedMaterial;

        /// <summary>
        /// Local-space box this collider fills. Approximation: sphere/capsule are their bounding boxes.
        /// </summary>
        protected virtual Vector3 LocalSize => size;

        /// <summary>
        /// World AABB. Honours lossyScale like Unity (centre and size scale; Unity scales a sphere by the max
        /// axis and a capsule radius by the max of x/z, which Abs-scaling per axis approximates). Rotation is
        /// ignored: the box stays axis-aligned (Unity would take the AABB of the rotated box).
        /// </summary>
        public virtual Bounds bounds
        {
            get
            {
                Vector3 s = transform.lossyScale;
                Vector3 c = Vector3.Scale(center, s);
                Vector3 z = LocalSize;
                return new Bounds(transform.position + c, new Vector3(Mathf.Abs(z.x * s.x), Mathf.Abs(z.y * s.y), Mathf.Abs(z.z * s.z)));
            }
        }
        public Rigidbody attachedRigidbody => GetComponentInParent<Rigidbody>();

        /// <summary>Nearest point on the collider's axis-aligned box (every shim collider is one).</summary>
        public Vector3 ClosestPoint(Vector3 position) => bounds.ClosestPoint(position);

        protected Collider() => Physics.Register(this);
    }

    public class BoxCollider : Collider { }
    public class SphereCollider : Collider
    {
        public float radius = 0.5f;
        protected override Vector3 LocalSize => Vector3.one * (radius * 2f);
    }
    public class CapsuleCollider : Collider
    {
        public float radius = 0.5f; public float height = 2f;
        protected override Vector3 LocalSize => new Vector3(radius * 2f, Mathf.Max(height, radius * 2f), radius * 2f);
    }
    public class MeshCollider : Collider { public bool convex; public Mesh sharedMesh; }
    public class CharacterController : Collider
    {
        public bool isGrounded => true;
        public Vector3 velocity;
        public void Move(Vector3 motion) => transform.position += motion;
    }

    /// <summary>Handle onto the shim's single physics world; every query forwards to <see cref="Physics"/>.</summary>
    public struct PhysicsScene
    {
        public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance = float.PositiveInfinity,
            int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            Physics.Raycast(origin, direction, out hit, maxDistance, layerMask, q);

        /// <summary>Same capsule approximation as <see cref="Physics.CheckCapsule"/>.</summary>
        public int OverlapCapsule(Vector3 point0, Vector3 point1, float radius, Collider[] results,
            int layerMask = Physics.DefaultRaycastLayers, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            int n = 0;
            foreach (Collider c in Physics.CapsuleOverlaps(point0, point1, radius, layerMask))
            {
                if (n >= results.Length) break;
                results[n++] = c;
            }
            return n;
        }
    }

    public struct RaycastHit
    {
        public Collider collider;
        public Vector3 point;
        public Vector3 normal;
        public float distance;
        public Transform transform => collider?.transform;
        public Rigidbody rigidbody => collider?.attachedRigidbody;
    }

    public struct ContactPoint
    {
        public Vector3 point;
        public Vector3 normal;
    }

    public class Collision
    {
        public Collider collider;
        public Vector3 relativeVelocity;
        /// <summary>Contacts a test chose to supply; empty unless set, since nothing is simulated.</summary>
        public ContactPoint[] contacts = Array.Empty<ContactPoint>();
        public int contactCount => contacts.Length;
        public ContactPoint GetContact(int index) => contacts[index];
        public GameObject gameObject => collider?.gameObject;
        public Transform transform => collider?.transform;
        public Collision() { }
        public Collision(Collider collider, Vector3 relativeVelocity)
        {
            this.collider = collider;
            this.relativeVelocity = relativeVelocity;
        }
    }

    /// <summary>
    /// A small but real physics query world: colliders are tracked in a list and queries do honest
    /// sphere-overlap and segment/AABB intersection tests, so occlusion and range logic is exercised.
    /// </summary>
    public static class Physics
    {
        private static readonly List<Collider> _colliders = new List<Collider>();

        internal static void Register(Collider c) { if (!_colliders.Contains(c)) _colliders.Add(c); }
        public static void ClearWorld() => _colliders.Clear();
        public static IReadOnlyList<Collider> AllColliders => _colliders;

        private static IEnumerable<Collider> Live(int layerMask) => _colliders
            .Where(c => c != null && c.enabled && c.gameObject != null && c.gameObject.activeInHierarchy)
            .Where(c => (layerMask & (1 << c.gameObject.layer)) != 0);

        public static Collider[] OverlapSphere(Vector3 position, float radius, int layerMask = ~0,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            Live(layerMask).Where(c => c.bounds.SqrDistance(position) <= radius * radius).ToArray();

        /// <summary>No-op: colliders in this shim have no deferred transform sync to flush.</summary>
        public static void SyncTransforms() { }

        /// <summary>
        /// Approximates the capsule as its midpoint sphere of radius `radius` plus half the capsule's
        /// own length -- looser than a true swept capsule, but this codebase only uses it as a
        /// "would anything be jammed here" clearance check.
        /// </summary>
        public static bool CheckCapsule(Vector3 point0, Vector3 point1, float radius, int layerMask = ~0,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            return CapsuleOverlaps(point0, point1, radius, layerMask).Any();
        }

        internal static IEnumerable<Collider> CapsuleOverlaps(Vector3 point0, Vector3 point1, float radius, int layerMask)
        {
            Vector3 mid = (point0 + point1) * 0.5f;
            float reach = radius + (point1 - point0).magnitude * 0.5f;
            return Live(layerMask).Where(c => c.bounds.SqrDistance(mid) <= reach * reach);
        }

        /// <summary>
        /// Treats the box as axis-aligned (orientation ignored): the only caller, ExtractionZone,
        /// passes an unrotated zone volume in every test.
        /// </summary>
        /// <summary>Axis-aligned, like <see cref="OverlapBoxNonAlloc"/>.</summary>
        public static Collider[] OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation,
            int layerMask = ~0, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            var results = new Collider[_colliders.Count];
            int count = OverlapBoxNonAlloc(center, halfExtents, results, orientation, layerMask, q);
            Array.Resize(ref results, count);
            return results;
        }

        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results,
            Quaternion orientation, int layerMask = ~0, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            var box = new Bounds(center, halfExtents * 2f);
            int count = 0;
            foreach (Collider c in Live(layerMask))
            {
                if (count >= results.Length) break;
                Bounds other = c.bounds;
                bool overlaps = Mathf.Abs(other.center.x - box.center.x) <= other.extents.x + box.extents.x
                                && Mathf.Abs(other.center.y - box.center.y) <= other.extents.y + box.extents.y
                                && Mathf.Abs(other.center.z - box.center.z) <= other.extents.z + box.extents.z;
                if (overlaps) results[count++] = c;
            }
            return count;
        }

        public static int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results,
            int layerMask = ~0, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            int count = 0;
            foreach (Collider c in Live(layerMask))
            {
                if (count >= results.Length) break;
                if (c.bounds.SqrDistance(position) <= radius * radius) results[count++] = c;
            }
            return count;
        }

        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance = float.PositiveInfinity,
            int layerMask = ~0, QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            var hits = new List<RaycastHit>();
            Vector3 dir = direction.normalized;
            foreach (Collider c in Live(layerMask))
            {
                if (!SegmentIntersectsBounds(origin, dir, maxDistance, c.bounds, out float distance)) continue;
                hits.Add(new RaycastHit
                {
                    collider = c,
                    distance = distance,
                    point = origin + dir * distance,
                    normal = -dir
                });
            }
            return hits.OrderBy(h => h.distance).ToArray();
        }

        /// <summary>Unity's default query mask: every layer but Ignore Raycast (2).</summary>
        public const int DefaultRaycastLayers = ~(1 << 2);
        public const int AllLayers = ~0;

        /// <summary>Hits sorted nearest first (Unity's NonAlloc order is unspecified), truncated to the buffer.</summary>
        public static int RaycastNonAlloc(Vector3 origin, Vector3 direction, RaycastHit[] results,
            float maxDistance = float.PositiveInfinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            SweepInto(results, origin, direction, maxDistance, layerMask, 0f);

        /// <summary>Approximation: the swept sphere is the ray against bounds grown by the radius (every shim collider is an AABB).</summary>
        public static int SphereCastNonAlloc(Vector3 origin, float radius, Vector3 direction, RaycastHit[] results,
            float maxDistance = float.PositiveInfinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            SweepInto(results, origin, direction, maxDistance, layerMask, radius);

        /// <summary>Approximation: swept as a sphere of the capsule's radius from the capsule's midpoint; its height is ignored.</summary>
        public static int CapsuleCastNonAlloc(Vector3 point1, Vector3 point2, float radius, Vector3 direction,
            RaycastHit[] results, float maxDistance = float.PositiveInfinity, int layerMask = DefaultRaycastLayers,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal) =>
            SweepInto(results, (point1 + point2) * 0.5f, direction, maxDistance, layerMask, radius);

        /// <summary>
        /// Allocation-free (the guard navigation tests assert zero allocation per tick, as Unity's NonAlloc
        /// calls give): keeps the nearest results.Length hits sorted nearest first. For a swept sphere the
        /// normal is the real one, from the nearest point of the unswollen box to the sphere's centre at the
        /// hit, so a swept body that clips a corner gets a normal it can slide along; a plain ray keeps -dir.
        /// </summary>
        /// <summary>
        /// A swept sphere meets the box rounded at its edges and corners, not the box grown into a bigger
        /// box (whose square corners stop a body that Unity would let slide past). The grown box only
        /// brackets the entry time; from there sphere-trace the distance to the real box (1-Lipschitz, so
        /// each step is safe) until the sphere touches.
        /// </summary>
        private static bool TraceRoundedBox(Bounds raw, Vector3 origin, Vector3 dir, float maxDistance, float radius, ref float t)
        {
            for (int i = 0; i < 64; i++)
            {
                Vector3 p = origin + dir * t;
                float gap = (p - raw.ClosestPoint(p)).magnitude - radius;
                if (gap <= 1e-4f) return true;
                t += gap;
                if (t > maxDistance) return false;
            }
            return false; // grazing a corner: never converged, so it does not touch
        }

        private static int SweepInto(RaycastHit[] results, Vector3 origin, Vector3 direction, float maxDistance, int layerMask, float grow)
        {
            Vector3 dir = direction.normalized;
            int n = 0;
            for (int i = 0; i < _colliders.Count; i++)
            {
                Collider c = _colliders[i];
                if (c == null || !c.enabled || c.gameObject == null || !c.gameObject.activeInHierarchy
                    || (layerMask & (1 << c.gameObject.layer)) == 0)
                    continue;
                Bounds raw = c.bounds;
                Bounds b = raw;
                b.extents += new Vector3(grow, grow, grow);
                float distance;
                if (grow > 0f && b.Contains(origin))
                {
                    // Inside the grown box but maybe outside the rounded shape (its square corners are
                    // empty): trace from here, else a body in a corner zone tunnels through. Already
                    // touching the real shape means overlapping at the start: no hit, as for a ray.
                    distance = 0f;
                    if ((origin - raw.ClosestPoint(origin)).magnitude <= grow) continue;
                    if (!TraceRoundedBox(raw, origin, dir, maxDistance, grow, ref distance)) continue;
                }
                else
                {
                    if (!SegmentIntersectsBounds(origin, dir, maxDistance, b, out distance)) continue;
                    if (grow > 0f && !TraceRoundedBox(raw, origin, dir, maxDistance, grow, ref distance)) continue;
                }

                Vector3 point = origin + dir * distance;
                Vector3 normal = -dir;
                if (grow > 0f)
                {
                    Vector3 nearest = raw.ClosestPoint(point);
                    Vector3 away = point - nearest;
                    if (away.sqrMagnitude > 1e-10f) { normal = away.normalized; point = nearest; }
                }

                int at = n;
                while (at > 0 && results[at - 1].distance > distance) at--;
                if (at >= results.Length) continue;
                int last = Math.Min(n, results.Length - 1);
                for (int k = last; k > at; k--) results[k] = results[k - 1];
                results[at] = new RaycastHit { collider = c, distance = distance, point = point, normal = normal };
                if (n < results.Length) n++;
            }
            return n;
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask,
            QueryTriggerInteraction q) => Raycast(origin, direction, out _, maxDistance, layerMask, q);

        public static bool Linecast(Vector3 start, Vector3 end, int layerMask, QueryTriggerInteraction q) =>
            Linecast(start, end, layerMask);

        public static Vector3 gravity { get; set; } = new Vector3(0f, -9.81f, 0f);

        /// <summary>Recorded rather than simulated: tests can assert which pairs were un-collided.</summary>
        public static void IgnoreCollision(Collider a, Collider b, bool ignore = true)
        {
            if (a == null || b == null) return;
            IgnoredPairs.Add((a.GetInstanceID(), b.GetInstanceID()));
        }

        public static readonly HashSet<(int, int)> IgnoredPairs = new HashSet<(int, int)>();
        public static void IgnoreLayerCollision(int layerA, int layerB, bool ignore = true) { }

        public static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance = float.PositiveInfinity,
            int layerMask = ~0) => Raycast(ray.origin, ray.direction, out hit, maxDistance, layerMask);

        public static bool SphereCast(Vector3 origin, float radius, Vector3 direction, out RaycastHit hit,
            float maxDistance = float.PositiveInfinity, int layerMask = ~0) =>
            Raycast(origin, direction, out hit, maxDistance, layerMask);

        public static bool CheckSphere(Vector3 position, float radius, int layerMask = ~0) =>
            OverlapSphere(position, radius, layerMask).Length > 0;

        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit,
            float maxDistance = float.PositiveInfinity, int layerMask = ~0,
            QueryTriggerInteraction q = QueryTriggerInteraction.UseGlobal)
        {
            RaycastHit[] hits = RaycastAll(origin, direction, maxDistance, layerMask, q);
            hit = hits.Length > 0 ? hits[0] : default;
            return hits.Length > 0;
        }

        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance = float.PositiveInfinity,
            int layerMask = ~0) => Raycast(origin, direction, out _, maxDistance, layerMask);

        public static bool Linecast(Vector3 start, Vector3 end, int layerMask = ~0)
        {
            Vector3 d = end - start;
            return Raycast(start, d.normalized, out _, d.magnitude, layerMask);
        }

        /// <summary>
        /// Slab-method ray/AABB intersection, restricted to a finite segment length.
        ///
        /// A collider containing the origin reports no hit, matching Unity: "Raycasts will not detect
        /// Colliders for which the raycast origin is inside the collider." Without this, a player's
        /// own trigger volume swallows every interaction ray they cast.
        /// </summary>
        private static bool SegmentIntersectsBounds(Vector3 origin, Vector3 dir, float length, Bounds b, out float distance)
        {
            distance = 0f;
            if (b.Contains(origin))
                return false;

            float tMin = 0f, tMax = length;
            Vector3 lo = b.min, hi = b.max;

            if (!Slab(origin.x, dir.x, lo.x, hi.x, ref tMin, ref tMax)) return false;
            if (!Slab(origin.y, dir.y, lo.y, hi.y, ref tMin, ref tMax)) return false;
            if (!Slab(origin.z, dir.z, lo.z, hi.z, ref tMin, ref tMax)) return false;

            distance = tMin;
            return true;
        }

        private static bool Slab(float origin, float dir, float lo, float hi, ref float tMin, ref float tMax)
        {
            if (Mathf.Abs(dir) < 1e-8f) return origin >= lo && origin <= hi;
            float t1 = (lo - origin) / dir;
            float t2 = (hi - origin) / dir;
            if (t1 > t2) (t1, t2) = (t2, t1);
            tMin = Mathf.Max(tMin, t1);
            tMax = Mathf.Min(tMax, t2);
            return tMin <= tMax;
        }
    }

    // --- Input --------------------------------------------------------------------------------

    public enum KeyCode
    {
        None = 0, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Alpha0, Alpha1, Alpha2, Alpha3, Alpha4, Alpha5, Alpha6, Alpha7, Alpha8, Alpha9,
        Space, Return, Escape, Tab, Backspace, Delete, UpArrow, DownArrow, LeftArrow, RightArrow,
        LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt, CapsLock,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, Mouse0, Mouse1, Mouse2
    }

    /// <summary>Scriptable input: tests push key state in and the component reads it as usual.</summary>
    public static class Input
    {
        private static readonly HashSet<KeyCode> _held = new HashSet<KeyCode>();
        private static readonly HashSet<KeyCode> _down = new HashSet<KeyCode>();
        private static readonly HashSet<KeyCode> _up = new HashSet<KeyCode>();

        public static bool GetKey(KeyCode key) => _held.Contains(key);
        public static bool GetKeyDown(KeyCode key) => _down.Contains(key);
        public static bool GetKeyUp(KeyCode key) => _up.Contains(key);
        public static float GetAxis(string axis) => 0f;
        public static float GetAxisRaw(string axis) => 0f;
        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonDown(int button) => false;
        public static Vector3 mousePosition => Vector3.zero;

        /// <summary>Press a key: registers a one-frame KeyDown and holds it until <see cref="Release"/>.</summary>
        public static void Press(KeyCode key) { _down.Add(key); _held.Add(key); _up.Remove(key); }
        /// <summary>Release a key: registers a one-frame KeyUp.</summary>
        public static void Release(KeyCode key) { _up.Add(key); _held.Remove(key); _down.Remove(key); }
        /// <summary>Clears the one-frame KeyDown/KeyUp edges, as Unity does between frames.</summary>
        public static void NewFrame() { _down.Clear(); _up.Clear(); }
        public static void Reset() { _held.Clear(); _down.Clear(); _up.Clear(); }
    }
}

namespace UnityEngine.Rendering
{
    public enum GraphicsDeviceType { Null = 4, Direct3D11 = 2, OpenGLCore = 17, Vulkan = 21, Metal = 16 }
    public enum RenderQueue { Background = 1000, Geometry = 2000, AlphaTest = 2450, GeometryLast = 2500, Transparent = 3000, Overlay = 4000 }
    public enum CullMode { Off, Front, Back }
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum LightProbeUsage { Off, BlendProbes, UseProxyVolume, CustomProvided }
}

namespace UnityEngine
{
    public enum AmbientMode { Skybox, Trilight, Flat, Custom }
    public enum FogMode { Linear = 1, Exponential = 2, ExponentialSquared = 3 }

    public static class RenderSettings
    {
        public static AmbientMode ambientMode { get; set; }
        public static Color ambientLight { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
        public static bool fog { get; set; }
        public static FogMode fogMode { get; set; } = FogMode.Exponential;
        public static Color fogColor { get; set; }
        public static float fogDensity { get; set; } = 0.01f;
        public static Material skybox { get; set; }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
        /// <summary>The one implicit physics world every headless collider lives in.</summary>
        public PhysicsScene GetPhysicsScene() => default;

        public string name;
        public int buildIndex;
        public string path;
        public bool isLoaded;
        public bool isDirty;
        public bool IsValid() => true;

        /// <summary>Every parentless object currently registered -- this shim has one implicit scene.</summary>
        public GameObject[] GetRootGameObjects() =>
            SceneRegistry.AllObjects.Where(g => g != null && g.transform.parent == null).ToArray();
    }
    /// <summary>No real async work headlessly; Drain() advances one frame per yield regardless.</summary>
    public class AsyncOperation { public bool isDone => true; }

    public enum LoadSceneMode { Single, Additive }

    public struct LoadSceneParameters
    {
        public LoadSceneMode loadSceneMode;
        public LoadSceneParameters(LoadSceneMode mode) { loadSceneMode = mode; }
    }

    public static class SceneManager
    {
        public static Scene GetActiveScene() => new Scene { name = "Headless", isLoaded = true };
        public static Scene GetSceneByPath(string path) => new Scene { name = path, path = path, isLoaded = false };
        public static Scene CreateScene(string name) => new Scene { name = name, isLoaded = true };
        public static void SetActiveScene(Scene scene) { }
        public static AsyncOperation UnloadSceneAsync(Scene scene) => new AsyncOperation();
        /// <summary>Every object already lives in the one implicit scene, so there is nothing to move.</summary>
        public static void MoveGameObjectToScene(GameObject go, Scene scene) { }
        public static void LoadScene(string name) { }
        public static void LoadScene(int index) { }
        public static event Action<Scene, Scene> activeSceneChanged;
    }
}

namespace UnityEngine.Events
{
    public class UnityEventBase { }
    public class UnityEvent : UnityEventBase
    {
        private readonly List<Action> _calls = new List<Action>();
        // Unity's UnityEvent takes only UnityAction; a second Action overload made every lambda ambiguous.
        public void AddListener(UnityAction call) => _calls.Add(new Action(call.Invoke));
        public void RemoveListener(UnityAction call) => _calls.RemoveAll(c => c.Target == (object)call.Target && c.Method == call.Method);
        public void RemoveAllListeners() => _calls.Clear();
        public void Invoke() { foreach (Action c in _calls.ToArray()) c(); }
    }
    public class UnityEvent<T> : UnityEventBase
    {
        private readonly List<Action<T>> _calls = new List<Action<T>>();
        public void AddListener(UnityAction<T> call) => _calls.Add(new Action<T>(call.Invoke));
        public void RemoveListener(UnityAction<T> call) => _calls.RemoveAll(c => c.Target == (object)call.Target && c.Method == call.Method);
        public void RemoveAllListeners() => _calls.Clear();
        public void Invoke(T arg) { foreach (Action<T> c in _calls.ToArray()) c(arg); }
    }
    public delegate void UnityAction();
}

namespace UnityEngine.UI
{
    public class Graphic : Behaviour
    {
        public Color color;
        public bool raycastTarget = true;
        public RectTransform rectTransform => (RectTransform)transform;
    }

    /// <summary>uGUI's texture-drawing Graphic. Nothing is drawn headlessly.</summary>
    public class RawImage : Graphic
    {
        public Texture texture;
    }

    /// <summary>Only the layout hints the UI factory sets; nothing lays out headlessly.</summary>
    public class LayoutElement : Behaviour
    {
        public float minWidth;
        public float minHeight;
        public float preferredWidth;
        public float preferredHeight;
        public float flexibleWidth;
        public float flexibleHeight;
    }

    public class Image : Graphic
    {
        public enum Type { Simple, Sliced, Tiled, Filled }
        public enum FillMethod { Horizontal, Vertical, Radial90, Radial180, Radial360 }
        public enum OriginHorizontal { Left, Right }
        public enum OriginVertical { Bottom, Top }

        public float fillAmount = 1f;
        public Sprite sprite;
        public Type type;
        public FillMethod fillMethod;
        public int fillOrigin;
    }

    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }

    public class Text : Graphic
    {
        public string text = string.Empty;
        public int fontSize;
        public float lineSpacing = 1f;
        public TextAnchor alignment;
        public Font font;
        public HorizontalWrapMode horizontalOverflow;
        public VerticalWrapMode verticalOverflow;
        public bool resizeTextForBestFit;
        public int resizeTextMinSize;
        public int resizeTextMaxSize;

        /// <summary>No font metrics headlessly: a rough monospace estimate, only ever used for layout.</summary>
        public float preferredWidth => (text?.Length ?? 0) * (fontSize > 0 ? fontSize : 14) * 0.6f;
        public float preferredHeight => (fontSize > 0 ? fontSize : 14) * 1.2f;
    }

    public class Selectable : Behaviour
    {
        public Graphic targetGraphic;
        public ColorBlock colors = new ColorBlock
        {
            normalColor = Color.white, highlightedColor = Color.white, pressedColor = Color.white,
            selectedColor = Color.white, disabledColor = Color.white, colorMultiplier = 1f, fadeDuration = 0.1f,
        };
    }

    public class Slider : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }

        public float value;
        public float minValue;
        public float maxValue = 1f;
        public RectTransform fillRect;
        public RectTransform handleRect;
        public Direction direction;
        public Events.UnityEvent<float> onValueChanged = new Events.UnityEvent<float>();
    }

    public struct ColorBlock
    {
        public Color normalColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color selectedColor;
        public Color disabledColor;
        public float colorMultiplier;
        public float fadeDuration;
    }

    public class Button : Selectable
    {
        public Events.UnityEvent onClick = new Events.UnityEvent();
    }
}

namespace UnityEngine.AI
{
    public enum ObstacleAvoidanceType { NoObstacleAvoidance, LowQualityObstacleAvoidance,
        MedQualityObstacleAvoidance, GoodQualityObstacleAvoidance, HighQualityObstacleAvoidance }

    public class NavMeshAgent : Behaviour
    {
        public bool isOnNavMesh => true;
        public float angularSpeed = 120f;
        public float acceleration = 8f;
        public float radius = 0.5f;
        public float height = 2f;
        public ObstacleAvoidanceType obstacleAvoidanceType;
        public bool updateRotation = true;
        public bool isOnOffMeshLink => false;
        public Vector3 destination { get; set; }
        public float speed = 3.5f;
        public float stoppingDistance;
        public bool isStopped;
        public bool enabled = true;
        public Vector3 velocity;
        public float remainingDistance => Vector3.Distance(transform.position, destination);
        public bool pathPending => false;
        public bool hasPath => true;
        public bool SetDestination(Vector3 target) { destination = target; return true; }
        public void ResetPath() { }
        public void Warp(Vector3 position) => transform.position = position;
        /// <summary>Moves by the offset; there is no mesh edge to clamp against headlessly.</summary>
        public void Move(Vector3 offset) => transform.position += offset;
    }

    public struct NavMeshHit { public Vector3 position; public float distance; public bool hit; }

    public static class NavMesh
    {
        public const int AllAreas = ~0;
        /// <summary>No baked data exists headlessly, so there is nothing to remove.</summary>
        public static void RemoveAllNavMeshData() { }
        public static bool SamplePosition(Vector3 source, out NavMeshHit hit, float maxDistance, int areaMask)
        {
            hit = new NavMeshHit { position = source, distance = 0f, hit = true };
            return true;
        }

        /// <summary>No baked mesh headlessly: every path is the straight segment, and complete,
        /// matching SamplePosition's "everywhere is on the mesh".</summary>
        public static bool CalculatePath(Vector3 source, Vector3 target, int areaMask, NavMeshPath path)
        {
            path.corners = new[] { source, target };
            path.status = NavMeshPathStatus.PathComplete;
            return true;
        }
    }

    public enum NavMeshPathStatus { PathComplete, PathPartial, PathInvalid }

    public class NavMeshPath
    {
        public Vector3[] corners = Array.Empty<Vector3>();
        public NavMeshPathStatus status = NavMeshPathStatus.PathInvalid;
        public void ClearCorners() => corners = Array.Empty<Vector3>();
    }
}

namespace UnityEngine
{
    /// <summary>Colour ramp stub — evaluation returns the colour authored at the sampled key.</summary>
    public class Gradient
    {
        public GradientColorKey[] colorKeys = Array.Empty<GradientColorKey>();
        public GradientAlphaKey[] alphaKeys = Array.Empty<GradientAlphaKey>();
        public void SetKeys(GradientColorKey[] color, GradientAlphaKey[] alpha) { colorKeys = color; alphaKeys = alpha; }
        public Color Evaluate(float t) => colorKeys.Length == 0 ? Color.white : colorKeys[0].color;
    }

    public struct GradientColorKey
    {
        public Color color; public float time;
        public GradientColorKey(Color color, float time) { this.color = color; this.time = time; }
    }
    public struct GradientAlphaKey
    {
        public float alpha; public float time;
        public GradientAlphaKey(float alpha, float time) { this.alpha = alpha; this.time = time; }
    }
}

namespace UnityEngine.Serialization
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FormerlySerializedAsAttribute : Attribute
    {
        public FormerlySerializedAsAttribute(string oldName) { }
    }
}

namespace UnityEngine
{
    // --- IMGUI ---------------------------------------------------------------------------------
    // The raid HUD draws with IMGUI so the game is legible before any canvas is authored. Headlessly
    // there is no screen, so these types exist to compile and to let layout code run; drawing is a
    // no-op. What the HUD SAYS is tested through RaidHudModel, which needs none of this.

    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        {
            this.x = x; this.y = y; this.width = width; this.height = height;
        }
        public float xMin => x;
        public float yMin => y;
        public float xMax => x + width;
        public float yMax => y + height;
        public Vector2 center => new Vector2(x + width * 0.5f, y + height * 0.5f);
        public bool Contains(Vector2 p) => p.x >= x && p.x <= xMax && p.y >= y && p.y <= yMax;
    }

    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }

    public class GUIStyleState { public Color textColor = Color.white; public Texture2D background; }

    public class GUIContent
    {
        public string text;
        public GUIContent() { text = string.Empty; }
        public GUIContent(string text) => this.text = text;
    }

    public enum ScaleMode { StretchToFill, ScaleAndCrop, ScaleToFit }

    public static class GUIUtility
    {
        /// <summary>Would rotate GUI.matrix; nothing is drawn headlessly, so the matrix is left as is.</summary>
        public static void RotateAroundPivot(float angle, Vector2 pivot) { }
    }

    public class GUIStyle
    {
        /// <summary>No font metrics headlessly: a rough monospace estimate, only ever used for layout.</summary>
        public Vector2 CalcSize(GUIContent content)
        {
            int size = fontSize > 0 ? fontSize : 13;
            return new Vector2((content?.text?.Length ?? 0) * size * 0.6f, size * 1.2f);
        }

        public Font font;
        public int fontSize;
        public FontStyle fontStyle;
        public TextAnchor alignment;
        public bool richText;
        public bool wordWrap;
        public GUIStyleState normal = new GUIStyleState();
        public GUIStyle() { }
        public GUIStyle(GUIStyle other)
        {
            font = other.font;
            wordWrap = other.wordWrap;
            fontSize = other.fontSize;
            fontStyle = other.fontStyle;
            alignment = other.alignment;
            richText = other.richText;
            normal = new GUIStyleState { textColor = other.normal.textColor, background = other.normal.background };
        }
    }

    public class GUISkin { public GUIStyle label { get; } = new GUIStyle(); public GUIStyle box { get; } = new GUIStyle(); public GUIStyle button { get; } = new GUIStyle(); }

    public static class GUI
    {
        public static GUISkin skin { get; } = new GUISkin();
        public static Color color { get; set; } = Color.white;
        public static Color contentColor { get; set; } = Color.white;
        public static void Label(Rect rect, string text) { }
        public static void Label(Rect rect, string text, GUIStyle style) { }
        public static void Box(Rect rect, string text) { }
        public static bool Button(Rect rect, string text) => false;
        public static void DrawTexture(Rect rect, Texture texture) { }
        public static void DrawTexture(Rect rect, Texture texture, ScaleMode scaleMode) { }
        public static Matrix4x4 matrix { get; set; }
    }

    /// <summary>Opaque layout hint. The headless shim never lays anything out, so it just carries a value.</summary>
    public class GUILayoutOption { public float Value; }

    public static class GUILayout
    {
        public static void Label(string text) { }
        public static void Label(string text, GUIStyle style) { }
        public static bool Button(string text) => false;
        public static void BeginArea(Rect rect) { }
        public static void BeginArea(Rect rect, GUIStyle style) { }
        public static void EndArea() { }
        public static void BeginHorizontal() { }
        public static void EndHorizontal() { }
        public static void BeginVertical() { }
        public static void EndVertical() { }
        public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options) => scrollPosition;
        public static void EndScrollView() { }
        public static bool Toggle(bool value, string text, GUIStyle style) => value;
        public static float HorizontalSlider(float value, float leftValue, float rightValue) => value;
        public static int SelectionGrid(int selected, string[] texts, int columns) => selected;
        public static GUILayoutOption Height(float height) => new GUILayoutOption { Value = height };
        public static GUILayoutOption Width(float width) => new GUILayoutOption { Value = width };
        public static void Space(float pixels) { }
        public static void FlexibleSpace() { }
    }

    public static class GUILayoutUtility
    {
        public static Rect GetRect(float width, float height) => new Rect(0f, 0f, width, height);
    }

    public static class Screen
    {
        public static int width { get; set; } = 1920;
        public static int height { get; set; } = 1080;
    }
}

namespace UnityEngine
{
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum LightType { Spot, Directional, Point, Area }

    public class Shader : Object
    {
        public static Shader Find(string name) => new Shader { name = name };
        private static readonly Dictionary<int, string> s_names = new Dictionary<int, string>();
        public static int PropertyToID(string name)
        {
            int id = name.GetHashCode();
            lock (s_names) s_names[id] = name;
            return id;
        }
        /// <summary>The name an id was issued for; an id never issued by PropertyToID maps to a placeholder.</summary>
        internal static string NameOf(int id)
        {
            lock (s_names) return s_names.TryGetValue(id, out string n) ? n : "#" + id;
        }

        // Globals are recorded, not uploaded, so a test can read back what the game set.
        public static readonly Dictionary<int, Vector4> GlobalVectors = new Dictionary<int, Vector4>();
        public static readonly Dictionary<int, Color> GlobalColors = new Dictionary<int, Color>();
        public static readonly HashSet<string> GlobalKeywords = new HashSet<string>();
        public static void SetGlobalVector(int nameID, Vector4 value) => GlobalVectors[nameID] = value;
        public static void SetGlobalVectorArray(int nameID, Vector4[] values) { }
        public static void SetGlobalColor(int nameID, Color value) => GlobalColors[nameID] = value;
        public static void EnableKeyword(string keyword) => GlobalKeywords.Add(keyword);
        public static void DisableKeyword(string keyword) => GlobalKeywords.Remove(keyword);
    }

    public class AudioListener : Behaviour
    {
        public static float volume { get; set; } = 1f;
        public static bool pause { get; set; }
        /// <summary>Nothing is mixed, so the output is silence.</summary>
        public static void GetOutputData(float[] samples, int channel) => System.Array.Clear(samples, 0, samples.Length);
    }

    /// <summary>The quality levels of a fresh project (Low, Medium, High); switching just records the index.</summary>
    public static class QualitySettings
    {
        private static int _level = 1;
        public static string[] names { get; } = { "Low", "Medium", "High" };
        public static int GetQualityLevel() => _level;
        public static void SetQualityLevel(int index, bool applyExpensiveChanges) => _level = index;
        public static void SetQualityLevel(int index) => SetQualityLevel(index, true);
    }

    public enum CursorLockMode { None, Locked, Confined }

    public static class Cursor
    {
        public static CursorLockMode lockState { get; set; } = CursorLockMode.None;
        public static bool visible { get; set; } = true;
    }

    /// <summary>Only used as a Resources.Load&lt;Font&gt; target in this codebase; never actually loaded headlessly.</summary>
    /// <summary>No font data headlessly: metrics read 0, which the game treats as "unknown" and falls back from.</summary>
    public class Font : Object
    {
        public int fontSize;
        public int ascent;
        public int lineHeight;
        public void RequestCharactersInTexture(string characters, int size) { }
    }

    public enum EventType { MouseDown = 0, MouseUp = 1, MouseMove = 2, MouseDrag = 3, KeyDown = 4, KeyUp = 5, ScrollWheel = 6, Repaint = 7, Layout = 8, Used = 12 }

    /// <summary>IMGUI event. Never raised headlessly; <see cref="current"/> is settable so a test can drive OnGUI.</summary>
    public class Event
    {
        public static Event current { get; set; } = new Event();
        public EventType type = EventType.Layout;
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : class => null;
        public static T GetBuiltinResource<T>(string path) where T : class => null;
        /// <summary>No asset loading headlessly (see <see cref="Load{T}"/>), so no loaded assets exist to find.</summary>
        public static T[] FindObjectsOfTypeAll<T>() where T : Object => Array.Empty<T>();
    }
}
