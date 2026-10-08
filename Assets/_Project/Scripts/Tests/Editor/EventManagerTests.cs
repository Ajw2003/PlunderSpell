using System;
using Code.Scripts.EventSystems;
using EventSystems;
using NUnit.Framework;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The event bus delivers, unsubscribes every subscription a listener holds, is safe to leave
    /// mid-publish, and publishes without allocating. See docs/4-systems/core.md, "Event bus".
    /// </summary>
    public class EventManagerTests
    {
        private struct TestEvent : IEvent
        {
            public int Value;
        }

        private struct OtherTestEvent : IEvent
        {
        }

        private class Listener
        {
            public int Received;
        }

        private GameObject _busObject;
        private EventManager _bus;

        [SetUp]
        public void CreateBus()
        {
            // In Edit Mode Awake does not run, so this component is a private bus, not the shared Instance.
            _busObject = new GameObject("EventManagerTests");
            _bus = _busObject.AddComponent<EventManager>();
        }

        [TearDown]
        public void DestroyBus()
        {
            UnityEngine.Object.DestroyImmediate(_busObject);
        }

        [Test]
        public void APublishedEventReachesItsSubscriber()
        {
            var listener = new Listener();
            _bus.Subscribe(listener, (TestEvent e) => listener.Received += e.Value);

            _bus.Publish(new TestEvent { Value = 3 });

            Assert.AreEqual(3, listener.Received);
        }

        [Test]
        public void UnsubscribeRemovesEverySubscriptionOfThatListenerForTheType()
        {
            var listener = new Listener();
            _bus.Subscribe(listener, (TestEvent e) => listener.Received++);
            _bus.Subscribe(listener, (TestEvent e) => listener.Received++);

            _bus.Unsubscribe<TestEvent>(listener);
            _bus.Publish(new TestEvent());

            Assert.AreEqual(0, listener.Received, "a second subscription of the same listener was left behind");
            Assert.AreEqual(0, _bus.SubscriptionCount(listener));
        }

        [Test]
        public void UnsubscribeFromAllEventsLeavesTheListenerWithNothing()
        {
            var listener = new Listener();
            var bystander = new Listener();
            _bus.Subscribe(listener, (TestEvent e) => listener.Received++);
            _bus.Subscribe(listener, (OtherTestEvent e) => listener.Received++);
            _bus.Subscribe(bystander, (TestEvent e) => bystander.Received++);

            _bus.UnsubscribeFromAllEvents(listener);

            Assert.AreEqual(0, _bus.SubscriptionCount(listener));
            Assert.AreEqual(1, _bus.SubscriptionCount(bystander), "another listener lost its subscription");
            Assert.AreEqual(1, _bus.TotalSubscriptionCount);
        }

        [Test]
        public void UnsubscribingDuringPublishIsSafeAndTakesEffect()
        {
            var listener = new Listener();
            _bus.Subscribe(listener, (TestEvent e) =>
            {
                listener.Received++;
                _bus.UnsubscribeFromAllEvents(listener);
            });

            _bus.Publish(new TestEvent());
            _bus.Publish(new TestEvent());

            Assert.AreEqual(1, listener.Received);
            Assert.AreEqual(0, _bus.TotalSubscriptionCount);
        }

        [Test]
        public void PublishingAllocatesNothing()
        {
            var listener = new Listener();
            _bus.Subscribe(listener, (TestEvent e) => listener.Received += e.Value);
            for (int i = 0; i < 100; i++) _bus.Publish(new TestEvent { Value = 1 });

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) _bus.Publish(new TestEvent { Value = 1 });
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Assert.AreEqual(0, allocated, "Publish allocated with debug logging off");
        }
    }
}
