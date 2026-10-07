using System.Reflection;
using Interfaces;
using NUnit.Framework;
using Plunderspell.UI;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>#216: the damage feedback view expires numbers correctly and its Update allocates nothing.
    /// OnGUI is not driven: it needs a GUI context the headless harness does not have.</summary>
    public class DamageFeedbackViewTests
    {
        private GameObject _host;
        private GameObject _target;
        private DamageFeedbackView _view;
        private MethodInfo _update;

        [SetUp]
        public void SetUp()
        {
            Time.Reset();
            _host = new GameObject("view");
            _view = _host.AddComponent<DamageFeedbackView>();
            _target = new GameObject("target");
            _update = typeof(DamageFeedbackView).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            Object.DestroyImmediate(_target);
        }

        private void Hit() => Damage.ReportRemote(new DamageReport(_target.transform, null, null, 5f,
            Vector3.zero, DamageKind.Impact, 50f, 100f));

        private void Tick() => _update.Invoke(_view, null);

        [Test]
        public void NumbersExpireAfterTheirLifetime()
        {
            Hit();
            Time.Advance(0.6f);
            Hit();
            Tick();
            Assert.AreEqual(2, _view.LiveNumberCount);

            Time.Advance(0.6f); // first is 1.2s old (> 1.1), second 0.6s
            Tick();
            Assert.AreEqual(1, _view.LiveNumberCount);

            Time.Advance(0.6f);
            Tick();
            Assert.AreEqual(0, _view.LiveNumberCount);
        }

        [Test]
        public void UpdateAllocatesNothingWithLiveNumbers()
        {
            Hit();
            for (int i = 0; i < 10; i++)
                Tick(); // warm-up (reflection and JIT)

            var args = new object[0];
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
                _update.Invoke(_view, args);
            long after = System.GC.GetAllocatedBytesForCurrentThread();

            Assert.AreEqual(1, _view.LiveNumberCount);
            Assert.AreEqual(0, after - before);
        }
    }
}
