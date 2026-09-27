using UnityEngine;

namespace Code.Scripts.Singleton
{
    // Generic Singleton base class to ensure a single instance of a MonoBehaviour-derived class
    public class SingletonBase<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        protected virtual bool PersistBetweenScenes => true;

        // Never auto-creates a GameObject, and collapses a destroyed instance to a real null so
        // that `Instance?.Publish(...)` at call sites behaves. See docs/4-systems/core.md - Traps.
        public static T Instance => _instance != null ? _instance : null;

        protected virtual void Awake()
        {
            if (!_instance)
            {
                _instance = this as T;
                if (PersistBetweenScenes) DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
