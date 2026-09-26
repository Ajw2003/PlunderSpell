// Headless shim of Unity's object/component model: enough of GameObject, Component, Transform,
// MonoBehaviour and ScriptableObject to actually construct and drive gameplay components in tests.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine
{
    [Flags]
    public enum HideFlags
    {
        None = 0, HideInHierarchy = 1, HideInInspector = 2, DontSaveInEditor = 4,
        NotEditable = 8, DontSaveInBuild = 16, DontUnloadUnusedAsset = 32,
        DontSave = 52, HideAndDontSave = 61
    }

    public class Object
    {
        private static int _nextId = 1;
        internal bool _destroyed;
        public string name = string.Empty;
        public HideFlags hideFlags = HideFlags.None;
        public int InstanceId { get; } = _nextId++;

        public int GetInstanceID() => InstanceId;
        public override string ToString() => name;

        /// <summary>Mirrors Unity's "fake null": a destroyed object compares equal to null.</summary>
        public static bool operator ==(Object a, Object b)
        {
            bool an = ReferenceEquals(a, null) || a._destroyed;
            bool bn = ReferenceEquals(b, null) || b._destroyed;
            if (an && bn) return true;
            if (an || bn) return false;
            return ReferenceEquals(a, b);
        }

        public static bool operator !=(Object a, Object b) => !(a == b);
        public static implicit operator bool(Object o) => !(o == null);
        public override bool Equals(object other) => ReferenceEquals(this, other);
        public override int GetHashCode() => InstanceId;

        public static void Destroy(Object obj, float delay = 0f) => DestroyImmediate(obj);

        public static void DestroyImmediate(Object obj, bool allowDestroyingAssets = false)
        {
            if (ReferenceEquals(obj, null) || obj._destroyed) return;
            switch (obj)
            {
                case GameObject go: go.InternalDestroy(); break;
                case Component c: c.gameObject?.InternalRemoveComponent(c); break;
                default: obj._destroyed = true; break;
            }
        }

        public static void DontDestroyOnLoad(Object target) { /* no scene loading headlessly */ }

        public static T Instantiate<T>(T original) where T : Object => Instantiate(original, Vector3.zero, Quaternion.identity, null);

        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent = null)
            where T : Object
        {
            switch ((Object)original)
            {
                case GameObject go:
                {
                    var clone = GameObject.CloneOf(go);
                    clone.transform.position = position;
                    clone.transform.rotation = rotation;
                    if (parent != null) clone.transform.SetParent(parent, true);
                    return (T)(Object)clone;
                }
                case Component c:
                {
                    var clone = GameObject.CloneOf(c.gameObject);
                    clone.transform.position = position;
                    clone.transform.rotation = rotation;
                    if (parent != null) clone.transform.SetParent(parent, true);
                    return (T)(Object)clone.GetComponent(c.GetType());
                }
                default:
                    return original;
            }
        }

        public static T Instantiate<T>(T original, Transform parent) where T : Object =>
            Instantiate(original, Vector3.zero, Quaternion.identity, parent);

        public static T[] FindObjectsOfType<T>() where T : Component => SceneRegistry.FindComponents<T>().ToArray();
        public static T FindObjectOfType<T>() where T : Component => SceneRegistry.FindComponents<T>().FirstOrDefault();
        public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) where T : Component => FindObjectsOfType<T>();
        public static T FindFirstObjectByType<T>() where T : Component => FindObjectOfType<T>();
        public static T FindAnyObjectByType<T>() where T : Component => FindObjectOfType<T>();
    }

    public enum FindObjectsSortMode { None, InstanceID }

    /// <summary>
    /// The headless stand-in for the active scene: every live GameObject is registered here. Named
    /// SceneRegistry rather than Scene so it cannot be confused with UnityEngine.SceneManagement.Scene.
    /// </summary>
    public static class SceneRegistry
    {
        private static readonly List<GameObject> _objects = new List<GameObject>();

        internal static void Register(GameObject go) { if (!_objects.Contains(go)) _objects.Add(go); }
        internal static void Unregister(GameObject go) => _objects.Remove(go);

        public static IReadOnlyList<GameObject> AllObjects => _objects;

        public static IEnumerable<T> FindComponents<T>()
        {
            foreach (GameObject go in _objects.ToArray())
            {
                if (go == null) continue;
                foreach (Component c in go.Components)
                    if (c is T t) yield return t;
            }
        }

        /// <summary>Test teardown: destroy everything so suites cannot leak state into each other.</summary>
        public static void Clear()
        {
            foreach (GameObject go in _objects.ToArray())
                go.InternalDestroy();
            _objects.Clear();
        }
    }

    public class GameObject : Object
    {
        private readonly List<Component> _components = new List<Component>();
        public int layer;
        public string tag = "Untagged";
        private bool _active = true;

        public Transform transform { get; }
        public GameObject gameObject => this;
        public IReadOnlyList<Component> Components => _components;

        public GameObject(string name = "GameObject")
        {
            this.name = name;
            transform = new Transform();
            transform.BindTo(this);
            _components.Add(transform);
            SceneRegistry.Register(this);
        }

        public GameObject(string name, params Type[] componentTypes) : this(name)
        {
            foreach (Type t in componentTypes) AddComponent(t);
        }

        public bool activeSelf => _active;
        public bool activeInHierarchy =>
            _active && (transform.parent == null || transform.parent.gameObject.activeInHierarchy);
        public void SetActive(bool value) => _active = value;

        public T AddComponent<T>() where T : Component => (T)AddComponent(typeof(T));

        public Component AddComponent(Type type)
        {
            AddRequiredComponents(type);
            var component = (Component)Activator.CreateInstance(type, nonPublic: true);
            component.BindTo(this);
            _components.Add(component);
            component.InvokeMessage("Awake");
            if (_active) component.InvokeMessage("OnEnable");
            return component;
        }

        /// <summary>
        /// Mirrors the editor's [RequireComponent] behaviour: adding a component first adds anything
        /// it declares as required (recursively), so a component's Awake can rely on it being there.
        /// </summary>
        private void AddRequiredComponents(Type type)
        {
            foreach (RequireComponent attr in type.GetCustomAttributes(typeof(RequireComponent), true))
            {
                if (attr.Types == null) continue;
                foreach (Type required in attr.Types)
                {
                    if (required == null || typeof(Transform).IsAssignableFrom(required)) continue;
                    if (_components.Any(required.IsInstanceOfType)) continue;
                    Type concrete = required.IsAbstract ? ConcreteFor(required) : required;
                    if (concrete != null) AddComponent(concrete);
                }
            }
        }

        /// <summary>Picks a usable subclass when a required component type is abstract (e.g. Collider).</summary>
        private static Type ConcreteFor(Type abstractType) =>
            abstractType == typeof(Renderer) ? typeof(MeshRenderer) : null;

        public T GetComponent<T>() => _components.OfType<T>().FirstOrDefault();

        public bool TryGetComponent<T>(out T component)
        {
            component = GetComponent<T>();
            return component != null;
        }

        public bool CompareTag(string other) => tag == other;

        /// <summary>Builds a primitive with the collider and renderer the real one would have.</summary>
        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            var go = new GameObject(type.ToString());
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            switch (type)
            {
                case PrimitiveType.Sphere: go.AddComponent<SphereCollider>(); break;
                case PrimitiveType.Capsule: go.AddComponent<CapsuleCollider>(); break;
                case PrimitiveType.Plane:
                case PrimitiveType.Quad:
                case PrimitiveType.Cube:
                case PrimitiveType.Cylinder:
                default: go.AddComponent<BoxCollider>(); break;
            }
            return go;
        }

        public static GameObject Find(string name) =>
            SceneRegistry.AllObjects.FirstOrDefault(g => g != null && g.name == name);
        public static GameObject FindGameObjectWithTag(string tag) =>
            SceneRegistry.AllObjects.FirstOrDefault(g => g != null && g.tag == tag);
        public static GameObject[] FindGameObjectsWithTag(string tag) =>
            SceneRegistry.AllObjects.Where(g => g != null && g.tag == tag).ToArray();
        public Component GetComponent(Type t) => _components.FirstOrDefault(t.IsInstanceOfType);
        public T[] GetComponents<T>() => _components.OfType<T>().ToArray();
        public void GetComponents<T>(List<T> results) { results.Clear(); results.AddRange(_components.OfType<T>()); }

        public T GetComponentInChildren<T>(bool includeInactive = false)
        {
            T self = GetComponent<T>();
            if (self != null) return self;
            foreach (Transform child in transform.Children)
            {
                T found = child.gameObject.GetComponentInChildren<T>(includeInactive);
                if (found != null) return found;
            }
            return default;
        }

        public T[] GetComponentsInChildren<T>(bool includeInactive = false)
        {
            var results = new List<T>();
            CollectInChildren(results);
            return results.ToArray();
        }

        public void GetComponentsInChildren<T>(bool includeInactive, List<T> results)
        {
            results.Clear();
            CollectInChildren(results);
        }

        private void CollectInChildren<T>(List<T> results)
        {
            results.AddRange(_components.OfType<T>());
            foreach (Transform child in transform.Children)
                child.gameObject.CollectInChildren(results);
        }

        public T GetComponentInParent<T>(bool includeInactive = false)
        {
            GameObject current = this;
            while (current != null)
            {
                T found = current.GetComponent<T>();
                if (found != null) return found;
                current = current.transform.parent?.gameObject;
            }
            return default;
        }

        internal void InternalRemoveComponent(Component c)
        {
            if (c is Transform) return;
            _components.Remove(c);
            c.InvokeMessage("OnDisable");
            c.InvokeMessage("OnDestroy");
            c._destroyed = true;
        }

        internal void InternalDestroy()
        {
            if (_destroyed) return;
            _destroyed = true;
            foreach (Transform child in transform.Children.ToArray())
                child.gameObject.InternalDestroy();
            foreach (Component c in _components.ToArray())
            {
                if (c is Transform) continue;
                c.InvokeMessage("OnDisable");
                c.InvokeMessage("OnDestroy");
                c._destroyed = true;
            }
            _components.Clear();
            transform.SetParent(null, false);
            SceneRegistry.Unregister(this);
        }

        /// <summary>Shallow clone used by Instantiate: copies component types and public field values.</summary>
        internal static GameObject CloneOf(GameObject source)
        {
            var clone = new GameObject(source.name + "(Clone)") { layer = source.layer, tag = source.tag };
            foreach (Component c in source.Components)
            {
                if (c is Transform) continue;
                Component copy = clone.AddComponent(c.GetType());
                CopyFields(c, copy);
            }
            foreach (Transform child in source.transform.Children)
            {
                GameObject childClone = CloneOf(child.gameObject);
                childClone.transform.SetParent(clone.transform, false);
                childClone.transform.localPosition = child.localPosition;
                childClone.transform.localRotation = child.localRotation;
            }
            return clone;
        }

        private static void CopyFields(object from, object to)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type t = from.GetType(); t != null && t != typeof(Component) && t != typeof(Object); t = t.BaseType)
                foreach (FieldInfo f in t.GetFields(flags | BindingFlags.DeclaredOnly))
                    if (!f.IsInitOnly) f.SetValue(to, f.GetValue(from));
        }
    }

    public abstract class Component : Object
    {
        private GameObject _gameObject;

        public GameObject gameObject => _gameObject;
        public Transform transform => _gameObject?.transform;

        internal void BindTo(GameObject go)
        {
            _gameObject = go;
            if (string.IsNullOrEmpty(name)) name = go.name;
        }

        public T GetComponent<T>() => _gameObject.GetComponent<T>();
        public Component GetComponent(Type t) => _gameObject.GetComponent(t);
        public T[] GetComponents<T>() => _gameObject.GetComponents<T>();
        public void GetComponents<T>(List<T> results) => _gameObject.GetComponents(results);
        public T GetComponentInChildren<T>(bool includeInactive = false) => _gameObject.GetComponentInChildren<T>(includeInactive);
        public T[] GetComponentsInChildren<T>(bool includeInactive = false) => _gameObject.GetComponentsInChildren<T>(includeInactive);
        public void GetComponentsInChildren<T>(bool includeInactive, List<T> results) => _gameObject.GetComponentsInChildren(includeInactive, results);
        public T GetComponentInParent<T>(bool includeInactive = false) => _gameObject.GetComponentInParent<T>(includeInactive);
        public T AddComponent<T>() where T : Component => _gameObject.AddComponent<T>();
        public bool TryGetComponent<T>(out T component) => _gameObject.TryGetComponent(out component);
        public string tag { get => _gameObject.tag; set => _gameObject.tag = value; }
        public bool CompareTag(string other) => _gameObject.CompareTag(other);

        /// <summary>Invokes a Unity message (Awake/Start/OnEnable/...) if the component declares one.</summary>
        internal void InvokeMessage(string message, params object[] args)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (Type t = GetType(); t != null && t != typeof(Component); t = t.BaseType)
            {
                MethodInfo m = t.GetMethod(message, flags | BindingFlags.DeclaredOnly,
                    null, args.Select(a => a?.GetType() ?? typeof(object)).ToArray(), null);
                if (m == null) continue;
                m.Invoke(this, args);
                return;
            }
        }
    }

    public class Transform : Component, IEnumerable<Transform>
    {
        private readonly List<Transform> _children = new List<Transform>();

        public Transform parent { get; private set; }
        public Vector3 localPosition { get; set; } = Vector3.zero;
        public Quaternion localRotation { get; set; } = Quaternion.identity;
        public Vector3 localScale { get; set; } = Vector3.one;

        public IReadOnlyList<Transform> Children => _children;
        public int childCount => _children.Count;
        public Transform root => parent == null ? this : parent.root;

        public Vector3 position
        {
            get => parent == null ? localPosition : parent.position + parent.rotation * localPosition;
            set => localPosition = parent == null ? value : Quaternion.Inverse(parent.rotation) * (value - parent.position);
        }

        public Quaternion rotation
        {
            get => parent == null ? localRotation : parent.rotation * localRotation;
            set => localRotation = parent == null ? value : Quaternion.Inverse(parent.rotation) * value;
        }

        public Vector3 forward => rotation * Vector3.forward;
        public Vector3 up => rotation * Vector3.up;
        public Vector3 right => rotation * Vector3.right;
        public Vector3 eulerAngles { get; set; }

        public void SetParent(Transform newParent, bool worldPositionStays = true)
        {
            Vector3 worldPos = position;
            Quaternion worldRot = rotation;

            parent?._children.Remove(this);
            parent = newParent;
            newParent?._children.Add(this);

            if (worldPositionStays)
            {
                position = worldPos;
                rotation = worldRot;
            }
        }

        public Transform GetChild(int index) => _children[index];

        public Transform Find(string childName)
        {
            foreach (Transform c in _children)
            {
                if (c.gameObject.name == childName) return c;
                Transform nested = c.Find(childName);
                if (nested != null) return nested;
            }
            return null;
        }

        public Vector3 TransformDirection(Vector3 direction) => rotation * direction;
        public Vector3 TransformPoint(Vector3 point) => position + rotation * point;
        public Vector3 InverseTransformPoint(Vector3 worldPoint) => Quaternion.Inverse(rotation) * (worldPoint - position);
        public Vector3 InverseTransformDirection(Vector3 worldDir) => Quaternion.Inverse(rotation) * worldDir;
        public void LookAt(Transform target) => LookAt(target.position);
        public void LookAt(Vector3 worldPosition) => LookAt(worldPosition, Vector3.up);
        public void LookAt(Vector3 worldPosition, Vector3 worldUp)
        {
            Vector3 dir = worldPosition - position;
            if (dir.sqrMagnitude > 1e-9f) rotation = Quaternion.LookRotation(dir, worldUp);
        }

        public bool IsChildOf(Transform candidate)
        {
            for (Transform t = this; t != null; t = t.parent)
                if (ReferenceEquals(t, candidate)) return true;
            return false;
        }

        public void DetachChildren()
        {
            foreach (Transform c in _children.ToArray()) c.SetParent(null, true);
        }

        public void SetSiblingIndex(int index) { }
        public int GetSiblingIndex() => parent?._children.IndexOf(this) ?? 0;
        public void Translate(Vector3 delta) => position += delta;
        public void Rotate(Vector3 eulerAngles) => localRotation = localRotation * Quaternion.Euler(eulerAngles);
        public void Rotate(Vector3 axis, float angle) => localRotation = localRotation * Quaternion.AngleAxis(angle, axis);
        public void Rotate(float x, float y, float z) => Rotate(new Vector3(x, y, z));
        public void RotateAround(Vector3 point, Vector3 axis, float angle)
        {
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            position = point + q * (position - point);
            rotation = q * rotation;
        }
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { position = p; rotation = r; }

        public IEnumerator<Transform> GetEnumerator() => _children.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class Behaviour : Component
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled && gameObject != null && gameObject.activeInHierarchy;
    }

    public class MonoBehaviour : Behaviour
    {
        public bool useGUILayout = false;

        /// <summary>
        /// Headless coroutines run synchronously to completion, draining nested yields. Frame-based
        /// yields (null, WaitForEndOfFrame) advance immediately; WaitForSeconds is not simulated, so
        /// time-dependent coroutines must be driven explicitly by the test.
        /// </summary>
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            Drain(routine, 0);
            return new Coroutine();
        }

        private static void Drain(IEnumerator routine, int depth)
        {
            if (routine == null || depth > 32) return;
            int guard = 0;
            while (routine.MoveNext() && guard++ < 100000)
                if (routine.Current is IEnumerator nested) Drain(nested, depth + 1);
        }

        public void StopCoroutine(Coroutine routine) { }
        public void StopCoroutine(IEnumerator routine) { }
        public void StopAllCoroutines() { }
        public void Invoke(string method, float delay) => InvokeMessage(method);
        public void CancelInvoke() { }
        public bool IsInvoking() => false;
        public void print(object message) => Debug.Log(message);
    }

    public class Coroutine { }
    public sealed class WaitForSeconds
    {
        private readonly float _seconds;
        public WaitForSeconds(float seconds) => _seconds = seconds;
        public float Seconds => _seconds;
    }
    public sealed class WaitForFixedUpdate { }
    public sealed class WaitForEndOfFrame { }
    public sealed class WaitUntil { public WaitUntil(Func<bool> predicate) { } }

    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject
        {
            var instance = (T)Activator.CreateInstance(typeof(T), nonPublic: true);
            instance.name = typeof(T).Name;
            instance.InvokeMessage("Awake");
            return instance;
        }

        public static ScriptableObject CreateInstance(Type type)
        {
            var instance = (ScriptableObject)Activator.CreateInstance(type, nonPublic: true);
            instance.name = type.Name;
            instance.InvokeMessage("Awake");
            return instance;
        }

        internal void InvokeMessage(string message)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo m = GetType().GetMethod(message, flags);
            m?.Invoke(this, null);
        }

        /// <summary>Test helper: run the asset's OnValidate exactly as the editor would.</summary>
        public void Validate() => InvokeMessage("OnValidate");
    }
}
