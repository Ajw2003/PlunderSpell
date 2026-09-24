using Interfaces;
using NUnit.Framework;
using RogueAi.Lair;
using UnityEngine;

namespace RogueAi.Tests
{
    /// <summary>
    /// The two co-op rules that fail silently if broken: a hit sent to the machine that owns the
    /// target must not also land here, and showing a friend the host's campaign must never overwrite
    /// the friend's own save. See docs/systems/net.md.
    /// </summary>
    public class CoopRulesTests
    {
        private sealed class Target : MonoBehaviour, IHealth
        {
            public float CurrentHealth { get; set; } = 100f;
            public float MaxHealth => 100f;
            public void TakeDamage(float damage) => CurrentHealth -= damage;
            public void TakeDamage(float damage, float impactVelocity) => CurrentHealth -= damage;
        }

        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            Damage.Forward = null;
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void Test_AForwardedHitIsNotAlsoAppliedHere()
        {
            _go = new GameObject("Target");
            var target = _go.AddComponent<Target>();
            int forwarded = 0;
            Damage.Forward = (t, amount, source, instigator, point, kind, impact) => { forwarded++; return true; };

            float lost = Damage.Apply(target, 25f, null, null, Vector3.zero, DamageKind.Melee);

            Assert.AreEqual(1, forwarded, "The hit should have been offered to the network.");
            Assert.AreEqual(0f, lost);
            Assert.AreEqual(100f, target.CurrentHealth, "A forwarded hit landed twice: here and on its owner.");
        }

        [Test]
        public void Test_AHitThatStaysHereStillApplies()
        {
            _go = new GameObject("Target");
            var target = _go.AddComponent<Target>();
            Damage.Forward = (t, amount, source, instigator, point, kind, impact) => false;

            Assert.AreEqual(25f, Damage.Apply(target, 25f, null, null, Vector3.zero, DamageKind.Melee));
            Assert.AreEqual(75f, target.CurrentHealth);
        }

        [Test]
        public void Test_ShowingTheHostsCampaignDoesNotOverwriteThisMachinesSave()
        {
            _go = new GameObject("Lair");
            var lair = _go.AddComponent<LairHubManager>();
            float savedDebt = lair.TotalDebt, savedGold = lair.AccumulatedGold;

            lair.ShowHostCampaign(savedDebt + 1234f, savedGold + 99f, 42f);
            Assert.AreEqual(savedDebt + 1234f, lair.TotalDebt, "The Lair should show the host's debt.");

            lair.Load();
            Assert.AreEqual(savedDebt, lair.TotalDebt, "Joining a friend overwrote this machine's saved debt.");
            Assert.AreEqual(savedGold, lair.AccumulatedGold, "Joining a friend overwrote this machine's saved gold.");
        }
    }
}
