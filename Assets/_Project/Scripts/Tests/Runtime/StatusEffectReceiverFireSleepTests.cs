using NUnit.Framework;
using Plunderspell.Status;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>Fire overrules Somnus on the shared receiver (#236): igniting wakes a sleeper, and a burning target cannot be put to sleep.</summary>
    public class StatusEffectReceiverFireSleepTests
    {
        private GameObject _go;
        private StatusEffectReceiver _receiver;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Receiver");
            _receiver = _go.AddComponent<StatusEffectReceiver>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        [Test]
        public void IgnitingASleeperWakesItAndRaisesStatusChanged()
        {
            _receiver.Sleep(30f);
            int changes = 0;
            _receiver.StatusChanged += _ => changes++;

            _receiver.Ignite(5f, 10f);

            Assert.That(_receiver.IsAsleep, Is.False);
            Assert.That(_receiver.IsBurning, Is.True);
            Assert.That(changes, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void SleepOnABurningTargetIsIgnored()
        {
            _receiver.Ignite(5f, 10f);

            _receiver.Sleep(30f);

            Assert.That(_receiver.IsAsleep, Is.False);
            Assert.That(_receiver.IsBurning, Is.True);
        }

        [Test]
        public void SleepWorksAgainOnceTheFireIsOut()
        {
            _receiver.Ignite(5f, 1f);
            _receiver.Tick(1.5f);

            _receiver.Sleep(30f);

            Assert.That(_receiver.IsAsleep, Is.True);
        }
    }
}
