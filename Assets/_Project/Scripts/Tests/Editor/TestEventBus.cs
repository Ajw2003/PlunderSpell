using System.Reflection;
using Code.Scripts.EventSystems;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// A shared <see cref="EventManager"/> for Edit Mode tests. Edit Mode never runs Awake, so nothing sets
    /// <c>EventManager.Instance</c>; this runs Awake and OnDestroy by hand so director and guard code that
    /// publishes on the shared bus can be tested.
    /// </summary>
    internal static class TestEventBus
    {
        private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        private static GameObject _owner;

        /// <summary>Makes the shared bus, replacing any left from an earlier test.</summary>
        public static EventManager Create()
        {
            Destroy();
            _owner = new GameObject("TestEventBus");
            EventManager bus = _owner.AddComponent<EventManager>();
            typeof(EventManager).GetMethod("Awake", Hidden).Invoke(bus, null);
            return bus;
        }

        public static void Destroy()
        {
            if (_owner == null)
                return;
            EventManager bus = _owner.GetComponent<EventManager>();
            typeof(EventManager).GetMethod("OnDestroy", Hidden).Invoke(bus, null);
            Object.DestroyImmediate(_owner);
            _owner = null;
        }
    }
}
