using NUnit.Framework;
using Plunderspell.Core;

namespace Plunderspell.Tests.EditMode
{
    public class PlayerStatsTests
    {
        [Test]
        public void ApplyDamage_ClampsAtZero()
        {
            var stats = new PlayerStats(maxHealth: 50, maxMana: 20);

            stats.ApplyDamage(999);

            Assert.AreEqual(0, stats.Health);
        }

        [Test]
        public void Heal_ClampsAtMaxHealth()
        {
            var stats = new PlayerStats(maxHealth: 50, maxMana: 20);
            stats.ApplyDamage(10);

            stats.Heal(999);

            Assert.AreEqual(50, stats.Health);
        }

        [Test]
        public void SpendMana_FailsWhenInsufficient()
        {
            var stats = new PlayerStats(maxHealth: 50, maxMana: 20);

            bool spent = stats.SpendMana(25);

            Assert.IsFalse(spent);
            Assert.AreEqual(20, stats.Mana);
        }

        [Test]
        public void SpendMana_SucceedsAndDeducts()
        {
            var stats = new PlayerStats(maxHealth: 50, maxMana: 20);

            bool spent = stats.SpendMana(15);

            Assert.IsTrue(spent);
            Assert.AreEqual(5, stats.Mana);
        }
    }
}
