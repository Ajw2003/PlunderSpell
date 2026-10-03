using NUnit.Framework;
using Plunderspell.Guards;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The garrison's balance knobs on <see cref="GuardSpawner"/>: guards spawn slower and hit softer
    /// than their prefabs (#117, #118), without re-forging every enemy prefab.
    /// </summary>
    public class GarrisonBalanceTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        [Test]
        public void ScaleTuning_MultipliesSpeedsAndDamage()
        {
            _go = new GameObject("Guard");
            var guard = _go.AddComponent<Guard>();
            GuardTuning tuning = guard.Tuning;
            float patrol = tuning.PatrolSpeed, chase = tuning.ChaseSpeed, damage = tuning.AttackDamage;

            guard.ScaleTuning(0.5f, 0.25f);

            Assert.AreEqual(patrol * 0.5f, tuning.PatrolSpeed, 1e-4f, "patrol speed");
            Assert.AreEqual(chase * 0.5f, tuning.ChaseSpeed, 1e-4f, "chase speed");
            Assert.AreEqual(damage * 0.25f, tuning.AttackDamage, 1e-4f, "damage per hit");
        }

        [Test]
        public void NewSpawner_DefaultsToSlowerSofterGuards()
        {
            _go = new GameObject("Spawner");
            var spawner = _go.AddComponent<GuardSpawner>();

            Assert.Less(spawner.SpeedScale, 1f, "guards should be slower than their prefabs (#117)");
            Assert.Less(spawner.DamageScale, 1f, "guards should hit softer than their prefabs (#118)");
        }
    }
}
