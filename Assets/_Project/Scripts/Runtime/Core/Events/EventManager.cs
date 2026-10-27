using System;
using System.Collections.Generic;
using Code.Scripts.Singleton;
using EventSystems;
using UnityEngine;

namespace Code.Scripts.EventSystems
{
    public class EventManager : SingletonBase<EventManager>
    {
        // Set to false in order to disable all debug logs from EventManager and related classes.
        public static bool DebugLoggingEnabled = false;

        private readonly Dictionary<Type, List<EventSubscription>> _subscriptions = new();
        private readonly List<EventSubscription> _pendingRemovals = new();
        private bool _isPublishing;
        protected override bool PersistBetweenScenes => true;

        // One bus from start-up to quit, so subscriptions survive scene loads and no scene has to hold one.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateForPlay()
        {
            if (Instance == null) new GameObject(nameof(EventManager)).AddComponent<EventManager>();
        }

        private class EventSubscription
        {
            public WeakReference TargetReference { get; set; }
            public Delegate Callback { get; set; }
            public bool MarkedForRemoval { get; set; }
        }

        // EventManager.Instance?.Subscribe(this, (SomeEvent e) => SomeFunctionToCall(e.SomeValue));
        public void Subscribe<T>(object target, Action<T> callback) where T : IEvent
        {
            var eventType = typeof(T);

            if (!_subscriptions.TryGetValue(eventType, out var subscriptionList))
            {
                subscriptionList = new List<EventSubscription>();
                _subscriptions[eventType] = subscriptionList;
            }

            subscriptionList.Add(new EventSubscription
            {
                TargetReference = new WeakReference(target),
                Callback = callback
            });
        }

        public void Unsubscribe<T>(object target) where T : IEvent
        {
            var eventType = typeof(T);

            if (!_subscriptions.TryGetValue(eventType, out var subscriptionList))
                return;

            RemoveTargetFrom(subscriptionList, target);
        }

        public void UnsubscribeFromAllEvents(object target)
        {
            foreach (var subscriptionList in _subscriptions.Values)
            {
                RemoveTargetFrom(subscriptionList, target);
            }
        }

        // Removes every subscription the target holds in the list, not just the first.
        // Mid-publish, removal is deferred so the publishing loop's indices stay valid.
        private void RemoveTargetFrom(List<EventSubscription> subscriptionList, object target)
        {
            for (int i = subscriptionList.Count - 1; i >= 0; i--)
            {
                var subscription = subscriptionList[i];
                if (subscription.TargetReference.Target != target) continue;

                if (_isPublishing)
                {
                    if (subscription.MarkedForRemoval) continue;
                    subscription.MarkedForRemoval = true;
                    _pendingRemovals.Add(subscription);
                }
                else
                {
                    subscriptionList.RemoveAt(i);
                }
            }
        }

        // Live subscriptions held by the target; leak tests check this is 0 after a screen closes.
        public int SubscriptionCount(object target)
        {
            int count = 0;
            foreach (var subscriptionList in _subscriptions.Values)
            {
                foreach (var subscription in subscriptionList)
                {
                    if (!subscription.MarkedForRemoval && subscription.TargetReference.Target == target) count++;
                }
            }
            return count;
        }

        // Live subscriptions on the whole bus whose targets still exist.
        public int TotalSubscriptionCount
        {
            get
            {
                int count = 0;
                foreach (var subscriptionList in _subscriptions.Values)
                {
                    foreach (var subscription in subscriptionList)
                    {
                        if (!subscription.MarkedForRemoval && subscription.TargetReference.Target != null) count++;
                    }
                }
                return count;
            }
        }

        public void UnsubscribeAllTargets()
        {
            var allTargets = GetAllSubscribedTargets();

            foreach (var target in allTargets)
            {
                UnsubscribeFromAllEvents(target);
            }
        }

        public IEnumerable<object> GetAllSubscribedTargets()
        {
            var targets = new HashSet<object>();

            foreach (var subscriptionList in _subscriptions.Values)
            {
                foreach (var subscription in subscriptionList)
                {
                    var target = subscription.TargetReference.Target;
                    if (target != null)
                        targets.Add(target);
                }
            }

            return targets;
        }

        public void Publish<T>(T eventData) where T : IEvent
        {
            var eventType = typeof(T);
            if (DebugLoggingEnabled) Debug.Log($"EventManager: Attempting to publish event of type {eventType.Name}");

            if (!_subscriptions.TryGetValue(eventType, out var subscriptionList))
            {
                if (DebugLoggingEnabled) Debug.Log($"EventManager: No subscriptions found for event type {eventType.Name}");
                return;
            }

            if (DebugLoggingEnabled) Debug.Log($"EventManager: Found {subscriptionList.Count} subscriptions for {eventType.Name}");
            _isPublishing = true;

            for (int i = subscriptionList.Count - 1; i >= 0; i--)
            {
                var subscription = subscriptionList[i];

                if (subscription.MarkedForRemoval)
                {
                    if (DebugLoggingEnabled) Debug.Log($"EventManager: Subscription for {eventType.Name} marked for removal, skipping.");
                    continue;
                }

                var target = subscription.TargetReference.Target;

                if (target == null)
                {
                    if (DebugLoggingEnabled) Debug.LogWarning($"EventManager: Target for subscription of {eventType.Name} is null (likely destroyed), marking for removal.");
                    subscription.MarkedForRemoval = true;
                    _pendingRemovals.Add(subscription);
                    continue;
                }

                try
                {
                    if (DebugLoggingEnabled) Debug.Log($"EventManager: Invoking callback for {eventType.Name} on target {target.GetType().Name}.");
                    ((Action<T>)subscription.Callback).Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error publishing event {typeof(T).Name} on target {target.GetType().Name}: {e.Message}");
                }
            }

            _isPublishing = false;
            CleanupPendingRemovals();
        }

        private void CleanupPendingRemovals()
        {
            if (_pendingRemovals.Count == 0)
                return;

            foreach (var subscription in _pendingRemovals)
            {
                foreach (var kvp in _subscriptions)
                {
                    kvp.Value.Remove(subscription);
                }
            }

            _pendingRemovals.Clear();
        }

        public void Clear()
        {
            _subscriptions.Clear();
            _pendingRemovals.Clear();
            _isPublishing = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Clear();
        }
    }
}
