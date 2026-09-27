using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// Issue #110: every sword a raid can hand you swings for real damage, and a hit is worth about
    /// a quarter of a guard. Before, only the Arming Sword was a melee weapon, at 12 a hit (5 to 9
    /// swings a guard), and the Bronze Sword and Longsword could not be swung for damage at all.
    /// See docs/4-systems/damage.md, "Melee".
    /// </summary>
    public class MeleeDamageTests
    {
        private static readonly string[] k_swords =
        {
            "Assets/_Project/Prefabs/Weapons/BronzeSword.prefab",
            "Assets/_Project/Prefabs/Weapons/ArmingSword.prefab",
            "Assets/_Project/Prefabs/Weapons/Longsword.prefab",
        };

        /// <summary>The raid guards' health runs from 60 (Wall Slinger) to 100 (Keeper of the Flame).</summary>
        private const float k_typicalGuardHealth = 80f;

        [TestCaseSource(nameof(k_swords))]
        public void Test_EverySwordIsAMeleeWeapon(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            var weapon = prefab.GetComponent<MeleeWeapon>();
            Assert.IsNotNull(weapon, $"{prefab.name} must be swingable.");
            Assert.IsNotNull(weapon.Stats, $"{prefab.name} needs MeleeWeaponStats.");
            Assert.Greater(weapon.Stats.Weight, 0f, $"{prefab.name}'s stats must point at its InventoryItem.");
            Assert.IsNotNull(prefab.GetComponent<Plunderspell.Acoustics.AcousticEmitter>(),
                $"{prefab.name} must make noise when swung.");
        }

        [TestCaseSource(nameof(k_swords))]
        public void Test_ASwordKillsAGuardInThreeOrFourHits(string path)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<MeleeWeapon>();
            Assume.That(weapon != null && weapon.Stats != null, "Covered by Test_EverySwordIsAMeleeWeapon.");

            int hits = Mathf.CeilToInt(k_typicalGuardHealth / weapon.Stats.Damage);
            Assert.That(hits, Is.InRange(3, 4), $"{weapon.name} does {weapon.Stats.Damage} a hit.");
        }

        [Test]
        public void Test_AHeavierSwordHitsHarderAndSwingsSlower()
        {
            var bronze = AssetDatabase.LoadAssetAtPath<GameObject>(k_swords[0]).GetComponent<MeleeWeapon>();
            var arming = AssetDatabase.LoadAssetAtPath<GameObject>(k_swords[1]).GetComponent<MeleeWeapon>();
            Assume.That(bronze != null && bronze.Stats != null && arming != null && arming.Stats != null);

            Assert.Greater(bronze.Stats.Weight, arming.Stats.Weight, "Test premise: the Bronze Sword is heavier.");
            Assert.Greater(bronze.Stats.Damage, arming.Stats.Damage);
            Assert.Greater(bronze.Stats.SwingDuration, arming.Stats.SwingDuration);
        }
    }
}
