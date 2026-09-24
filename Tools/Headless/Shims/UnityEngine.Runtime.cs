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
    }

    public static class Microphone
    {
        public static string[] devices => Array.Empty<string>();
        public static AudioClip Start(string device, bool loop, int lengthSec, int frequency) => null;
        public static void End(string device) { }
        public static int GetPosition(string device) => 0;
        public static void GetDeviceCaps(string device, out int min, out int max) { min = 0; max = 0; }
    }

    public class AudioClip : Object
    {
        public int channels = 1;
        public int frequency = 16000;
        public int samples;
        public bool GetData(float[] data, int offset) => true;
    }

    public class AudioSource : Behaviour
    {
        public AudioClip clip;
        public float volume = 1f;
        public bool loop;
        public bool playOnAwake;
        public bool isPlaying { get; private set; }
        public void Play() => isPlaying = true;
        public void Stop() => isPlaying = false;
        public void PlayOneShot(AudioClip c, float volumeScale = 1f) { }
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

        /// <summary>A zero-size box at the renderer's position -- no real mesh extents headlessly.</summary>
        public Bounds bounds => new Bounds(transform.position, Vector3.zero);

        public void SetPropertyBlock(MaterialPropertyBlock block) { }
        public void GetPropertyBlock(MaterialPropertyBlock block) { }
    }
    public class MeshRenderer : Renderer { }
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
        public Shader shader;
        public Material() { }
        public Material(Material src) { }
        public Material(Shader shader) => this.shader = shader;
        public Texture mainTexture;
        public void SetColor(string name, Color value) { _colors[name] = value; if (name == "_Color" || name == "_BaseColor") color = value; }
        public void SetColor(int nameID, Color value) => color = value;
        public void SetFloat(string name, float value) { }
        public void SetFloat(int nameID, float value) { }
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
        public int[] triangles = Array.Empty<int>();
        public Vector3[] vertices = Array.Empty<Vector3>();
        public int vertexCount => vertices.Length;
    }
    public class MeshFilter : Component { public Mesh mesh; public Mesh sharedMesh; }
    public class Sprite : Object { }
    public class Texture : Object { }
    public enum TextureFormat { RGBA32, RGB24, Alpha8 }

    public enum TextureWrapMode { Repeat, Clamp, Mirror, MirrorOnce }

    public class Texture2D : Texture
    {
        public int width { get; }
        public int height { get; }
        public TextureWrapMode wrapMode { get; set; }

        /// <summary>Unity's built-in 4×4 white texture.</summary>
        public static Texture2D whiteTexture { get; } = new Texture2D(4, 4);
        /// <summary>No pixel storage headlessly; nothing reads pixels back.</summary>
        public void SetPixels(Color[] colours) { }

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
    public class ParticleSystem : Component
    {
        public bool IsPlaying { get; private set; }
        public void Play() => IsPlaying = true;
        public void Stop() => IsPlaying = false;
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

        public Bounds bounds => new Bounds(transform.position + center, size);
        public Rigidbody attachedRigidbody => GetComponentInParent<Rigidbody>();

        /// <summary>Nearest point on the collider's axis-aligned box (every shim collider is one).</summary>
        public Vector3 ClosestPoint(Vector3 position) => bounds.ClosestPoint(position);

        protected Collider() => Physics.Register(this);
    }

    public class BoxCollider : Collider { }
    public class SphereCollider : Collider
    {
        public float radius = 0.5f;
        public new Bounds bounds => new Bounds(transform.position + center, Vector3.one * (radius * 2f));
    }
    public class CapsuleCollider : Collider { public float radius = 0.5f; public float height = 2f; }
    public class MeshCollider : Collider { public bool convex; public Mesh sharedMesh; }
    public class CharacterController : Collider
    {
        public bool isGrounded => true;
        public Vector3 velocity;
        public void Move(Vector3 motion) => transform.position += motion;
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
            Vector3 mid = (point0 + point1) * 0.5f;
            float reach = radius + (point1 - point0).magnitude * 0.5f;
            return Live(layerMask).Any(c => c.bounds.SqrDistance(mid) <= reach * reach);
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
    public enum CullMode { Off, Front, Back }
}

namespace UnityEngine
{
    public enum AmbientMode { Skybox, Trilight, Flat, Custom }

    public static class RenderSettings
    {
        public static AmbientMode ambientMode { get; set; }
        public static Color ambientLight { get; set; }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene
    {
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
        public void AddListener(Action call) => _calls.Add(call);
        public void AddListener(UnityAction call) => _calls.Add(new Action(call.Invoke));
        public void RemoveListener(Action call) => _calls.Remove(call);
        public void RemoveAllListeners() => _calls.Clear();
        public void Invoke() { foreach (Action c in _calls.ToArray()) c(); }
    }
    public class UnityEvent<T> : UnityEventBase
    {
        private readonly List<Action<T>> _calls = new List<Action<T>>();
        public void AddListener(Action<T> call) => _calls.Add(call);
        public void AddListener(UnityAction<T> call) => _calls.Add(new Action<T>(call.Invoke));
        public void RemoveListener(Action<T> call) => _calls.Remove(call);
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
        public RectTransform rectTransform => (RectTransform)transform;
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
        public TextAnchor alignment;
        public Font font;
        public HorizontalWrapMode horizontalOverflow;
        public VerticalWrapMode verticalOverflow;
    }

    public class Slider : Behaviour
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }

        public float value;
        public float minValue;
        public float maxValue = 1f;
        public RectTransform fillRect;
        public RectTransform handleRect;
        public Graphic targetGraphic;
        public Direction direction;
        public Events.UnityEvent<float> onValueChanged = new Events.UnityEvent<float>();
    }

    public struct ColorBlock
    {
        public Color normalColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color disabledColor;
    }

    public class Button : Behaviour
    {
        public Events.UnityEvent onClick = new Events.UnityEvent();
        public ColorBlock colors;
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
        public Color Evaluate(float t) => colorKeys.Length == 0 ? Color.white : colorKeys[0].color;
    }

    public struct GradientColorKey { public Color color; public float time; }
    public struct GradientAlphaKey { public float alpha; public float time; }
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

        public int fontSize;
        public FontStyle fontStyle;
        public TextAnchor alignment;
        public bool richText;
        public GUIStyleState normal = new GUIStyleState();
        public GUIStyle() { }
        public GUIStyle(GUIStyle other)
        {
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
        public static int PropertyToID(string name) => name.GetHashCode();
    }

    public class AudioListener : Behaviour
    {
        public static float volume { get; set; } = 1f;
        public static bool pause { get; set; }
    }

    public enum CursorLockMode { None, Locked, Confined }

    public static class Cursor
    {
        public static CursorLockMode lockState { get; set; } = CursorLockMode.None;
        public static bool visible { get; set; } = true;
    }

    /// <summary>Only used as a Resources.Load&lt;Font&gt; target in this codebase; never actually loaded headlessly.</summary>
    public class Font : Object { }

    public static class Resources
    {
        public static T Load<T>(string path) where T : class => null;
        public static T GetBuiltinResource<T>(string path) where T : class => null;
    }
}
