using System.Collections.Generic;
using Interfaces;
using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Loot;
using Plunderspell.Spells;
using Plunderspell.Status;
using Plunderspell.Voice;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Tests for the spell-effect layer: that a resolved cast actually changes the world, that a
    /// misfire hurts the caster rather than the target, and that casting is audible — the last one
    /// being the constraint the whole stealth game rests on.
    /// </summary>
    public class SpellEffectTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            SpellEffectRegistry.Reset();
            foreach (Object o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        // --- Helpers -----------------------------------------------------------------------

        private T Track<T>(T o) where T : Object
        {
            _spawned.Add(o);
            return o;
        }

        /// <summary>An actor at a position: a collider so spells can find it, and a status receiver.</summary>
        private StatusEffectReceiver MakeActor(string name, Vector3 position)
        {
            var go = Track(new GameObject(name));
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            return go.AddComponent<StatusEffectReceiver>();
        }

        private LootPickup MakeLoot(Vector3 position, float worth = 50f, float fragility = 8f)
        {
            var go = Track(new GameObject("Loot"));
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            var pickup = go.AddComponent<LootPickup>();

            var data = Track(ScriptableObject.CreateInstance<LootItem>());
            data.Worth = worth;
            data.Fragility = fragility;
            data.WeightKg = 2f;
            pickup.SetData(data);
            return pickup;
        }

        private AlarmFSMManager MakeAlarmListener(Vector3 position)
        {
            var go = Track(new GameObject("Alarm"));
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            return go.AddComponent<AlarmFSMManager>();
        }

        private static SpellEffectContext Context(SpellId spell, CastVolume volume, Vector3 origin,
            Transform caster = null)
        {
            var identity = caster != null ? caster.GetComponent<PurrNet.NetworkIdentity>() : null;
            return new SpellEffectContext(spell, volume, origin, Vector3.forward, identity);
        }

        // --- Ignis --------------------------------------------------------------------------

        [Test]
        public void Test_IgnisBurnsTarget()
        {
            StatusEffectReceiver victim = MakeActor("Guard", new Vector3(0f, 0f, 3f));

            int affected = SpellEffectRegistry.Execute(
                Context(SpellId.Ignis, CastVolume.Normal, Vector3.zero));

            Assert.AreEqual(1, affected, "Ignis should have found exactly one burnable target.");
            Assert.IsTrue(victim.IsBurning, "Ignis must set its target on fire.");
        }

        [Test]
        public void Test_IgnisDoesNothingWhenNothingIsInRange()
        {
            MakeActor("Guard", new Vector3(0f, 0f, 500f));

            int affected = SpellEffectRegistry.Execute(
                Context(SpellId.Ignis, CastVolume.Normal, Vector3.zero));

            Assert.AreEqual(0, affected, "A spell cast at nothing affects nothing — and must not throw.");
        }

        [Test]
        public void Test_ShoutingBurnsHarderThanWhispering()
        {
            float whisperDamage = BurnDamageForOneSecond(CastVolume.Whisper);
            float shoutDamage = BurnDamageForOneSecond(CastVolume.Shout);

            Assert.Greater(shoutDamage, whisperDamage,
                "A shouted Ignis must hurt more than a whispered one — volume scales power.");
        }

        /// <summary>
        /// Burns a single fresh target at a given volume for one second and reports the damage taken.
        /// The target is the only burnable thing in the scene for the duration, so the measurement
        /// cannot be confused by another test's leftovers.
        /// </summary>
        private float BurnDamageForOneSecond(CastVolume volume)
        {
            var go = new GameObject($"BurnTarget_{volume}");
            go.transform.position = new Vector3(0f, 0f, 2f);
            go.AddComponent<BoxCollider>();
            var health = go.AddComponent<FakeHealthComponent>();
            var status = go.AddComponent<StatusEffectReceiver>();

            SpellEffectRegistry.Execute(Context(SpellId.Ignis, volume, Vector3.zero));
            status.Tick(1f);

            float damage = health.TotalDamage;
            Object.DestroyImmediate(go);
            return damage;
        }

        // --- Frango -------------------------------------------------------------------------

        /// <summary>A target with health, for spells that hurt.</summary>
        private class Target : MonoBehaviour, Interfaces.IHealth
        {
            public float Health = 100f;
            public float CurrentHealth => Health;
            public float MaxHealth => 100f;
            public void TakeDamage(float damage) => Health = Mathf.Max(0f, Health - damage);
            public void TakeDamage(float damage, float impactVelocity) => TakeDamage(damage);
        }

        /// <summary>Something with health at a position, with a collider so a spell can find it.</summary>
        private Target MakeTarget(Vector3 position)
        {
            var go = Track(new GameObject("Target"));
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            return go.AddComponent<Target>();
        }

        [Test]
        public void Test_FrangoLeavesYourLootAlone()
        {
            // Frango used to shatter every breakable in range, and the only breakables were the
            // players' own valuables, so it could only ever cost you (#106).
            LootPickup vase = MakeLoot(new Vector3(0f, 0f, 2f));

            int hit = SpellEffectRegistry.Execute(
                Context(SpellId.Frango, CastVolume.Normal, Vector3.zero));

            Assert.AreEqual(0, hit);
            Assert.IsFalse(vase.IsBroken, "A force blast at your own haul must not smash it.");
        }

        [Test]
        public void Test_FrangoHurtsAndStaggersWhatYouAimAt()
        {
            StatusEffectReceiver guard = MakeActor("Guard", new Vector3(0f, 0f, 3f));
            var health = guard.gameObject.AddComponent<Target>();

            int hit = SpellEffectRegistry.Execute(
                Context(SpellId.Frango, CastVolume.Normal, Vector3.zero));

            Assert.AreEqual(1, hit);
            Assert.AreEqual(100f - SpellTuning.FrangoDamage, health.Health, 0.01f,
                "The blast must hurt what it hits.");
            Assert.IsTrue(guard.IsStunned, "The blast must stagger what it hits.");
        }

        [Test]
        public void Test_SomnusSleepsGuardsButNotTheCaster()
        {
            StatusEffectReceiver caster = MakeActor("Caster", Vector3.zero);
            caster.gameObject.AddComponent<PurrNet.NetworkIdentity>();
            StatusEffectReceiver guard = MakeActor("Guard", new Vector3(0f, 0f, 3f));

            int slept = SpellEffectRegistry.Execute(
                Context(SpellId.Somnus, CastVolume.Whisper, Vector3.zero, caster.transform));

            Assert.AreEqual(1, slept, "Only the guard should have fallen asleep.");
            Assert.IsTrue(guard.IsAsleep, "Somnus must put a guard to sleep.");
            Assert.IsFalse(caster.IsAsleep, "An intended spell must never hit the caster.");
        }

        /// <summary>
        /// #106: Somnus burst where the crosshair's ray ended, so a guard a hand's width off the
        /// crosshair (the ray passing it and ending on a wall far behind) slept through it, while
        /// Ignis, Frango and Levo all forgive that much. It now centres on the aimed sleeper.
        /// </summary>
        [Test]
        public void Test_SomnusSleepsAGuardSlightlyOffTheCrosshair()
        {
            StatusEffectReceiver caster = MakeActor("Caster", Vector3.zero);
            caster.gameObject.AddComponent<PurrNet.NetworkIdentity>();
            // 17 degrees off the aim line: inside the aim cone, but the ray down the line misses its
            // collider and runs on to the end of its range, 9 m beyond it.
            StatusEffectReceiver guard = MakeActor("Guard", new Vector3(1.5f, 0f, 5f));

            int slept = SpellEffectRegistry.Execute(
                Context(SpellId.Somnus, CastVolume.Normal, Vector3.zero, caster.transform));

            Assert.AreEqual(1, slept, "The guard under the crosshair must fall asleep.");
            Assert.IsTrue(guard.IsAsleep);
        }

        [Test]
        public void Test_TheSlamHurtsWhatIsAroundTheLandingButNotTheCaster()
        {
            Target caster = MakeTarget(Vector3.zero);
            Target near = MakeTarget(new Vector3(0f, 0f, 1f));
            Target edge = MakeTarget(new Vector3(0f, 0f, SpellTuning.SlamRadius - 0.3f));
            Target far = MakeTarget(new Vector3(0f, 0f, SpellTuning.SlamRadius + 3f));

            int hit = SaltusEffect.ResolveSlam(Vector3.zero, SpellTuning.SaltusSlamSpeed, caster.transform, ~0, 0);

            Assert.AreEqual(2, hit, "The slam reaches everything in its radius except the caster.");
            Assert.Less(near.CurrentHealth, edge.CurrentHealth, "Nearer the landing hurts more.");
            Assert.Less(edge.CurrentHealth, edge.MaxHealth, "The edge of the radius is still hit.");
            Assert.AreEqual(far.MaxHealth, far.CurrentHealth, "Outside the radius is untouched.");
            Assert.AreEqual(caster.MaxHealth, caster.CurrentHealth, "The caster is never hurt by their own slam.");
        }

        [Test]
        public void Test_ASlowLandingHurtsLessThanAFullSlam()
        {
            Target soft = MakeTarget(new Vector3(0f, 0f, 1f));
            SaltusEffect.ResolveSlam(Vector3.zero, SpellTuning.SaltusSlamSpeed * 0.25f, null, ~0, 0);
            float softDamage = soft.MaxHealth - soft.CurrentHealth;
            Object.DestroyImmediate(soft.gameObject);

            Target hard = MakeTarget(new Vector3(0f, 0f, 1f));
            SaltusEffect.ResolveSlam(Vector3.zero, SpellTuning.SaltusSlamSpeed, null, ~0, 0);
            float hardDamage = hard.MaxHealth - hard.CurrentHealth;

            Assert.Greater(hardDamage, softDamage, "The slam scales with how fast it lands.");
        }

        [Test]
        public void Test_AMovementSpellWithNoBodyToMoveCannotBeCast()
        {
            foreach (SpellId id in new[] { SpellId.Velox, SpellId.Saltus, SpellId.MisfireVelox, SpellId.MisfireSaltus })
            {
                var mover = SpellEffectRegistry.Find(id) as ICasterMovementSpell;
                Assert.IsNotNull(mover, $"{id} must move its caster, on the caster's machine.");
                Assert.IsFalse(mover.CanMove(Context(id, CastVolume.Normal, Vector3.zero)),
                    $"{id} with no caster body must fizzle rather than spend mana on nothing.");
            }
        }

        // --- Misfires -----------------------------------------------------------------------

        [Test]
        public void Test_MisfiredIgnisBurnsTheCasterNotTheTarget()
        {
            StatusEffectReceiver caster = MakeActor("Caster", Vector3.zero);
            caster.gameObject.AddComponent<PurrNet.NetworkIdentity>();
            StatusEffectReceiver guard = MakeActor("Guard", new Vector3(0f, 0f, 3f));

            int affected = SpellEffectRegistry.Execute(
                Context(SpellId.MisfireIgnis, CastVolume.Normal, Vector3.zero, caster.transform));

            Assert.AreEqual(1, affected);
            Assert.IsTrue(caster.IsBurning, "A misfired Ignis must set the CASTER alight.");
            Assert.IsFalse(guard.IsBurning, "The intended target must be untouched by a misfire.");
        }

        [Test]
        public void Test_MisfiredFrangoBreaksCarriedLootFirst()
        {
            var casterGo = Track(new GameObject("Caster"));
            casterGo.AddComponent<BoxCollider>();
            casterGo.AddComponent<PurrNet.NetworkIdentity>();

            LootPickup carried = MakeLoot(Vector3.zero);
            carried.transform.SetParent(casterGo.transform, false);

            LootPickup onTheFloor = MakeLoot(new Vector3(0f, 0f, 2f));

            int broken = SpellEffectRegistry.Execute(
                Context(SpellId.MisFireFrango, CastVolume.Normal, Vector3.zero, casterGo.transform));

            Assert.AreEqual(1, broken);
            Assert.IsTrue(carried.IsBroken, "A misfired Frango must break what YOU are carrying.");
            Assert.IsFalse(onTheFloor.IsBroken, "It must not break the loot you were aiming at.");
        }

        [Test]
        public void Test_EveryMisfireIdHasAnEffect()
        {
            foreach (SpellId id in System.Enum.GetValues(typeof(SpellId)))
            {
                if (!SpellCatalogue.IsMisfire(id))
                    continue;
                Assert.IsNotNull(SpellEffectRegistry.Find(id),
                    $"Misfire {id} has no effect registered — it would fizzle harmlessly.");
            }
        }

        [Test]
        public void Test_EveryPrimarySpellHasAMisfire()
        {
            SpellId[] primaries =
            {
                SpellId.Ignis, SpellId.Frango, SpellId.Levo, SpellId.AurumVoco,
                SpellId.Velox, SpellId.Somnus, SpellId.Saltus, SpellId.Porta
            };

            foreach (SpellId primary in primaries)
            {
                SpellId misfire = SpellCatalogue.DefaultMisfireFor(primary);
                Assert.AreNotEqual(SpellId.None, misfire, $"{primary} has no misfire outcome.");
                Assert.AreEqual(primary, SpellCatalogue.PrimaryFor(misfire),
                    $"{primary} -> {misfire} -> ? must round-trip.");
            }
        }

        // --- Casting is audible ---------------------------------------------------------------

        [Test]
        public void Test_CastingIsHeardByTheAlarm()
        {
            AlarmFSMManager alarm = MakeAlarmListener(new Vector3(0f, 0f, 2f));

            SpellEffectRegistry.Execute(Context(SpellId.Ignis, CastVolume.Normal, Vector3.zero));

            Assert.Greater(alarm.AlarmLevel, 0f,
                "Speaking a spell out loud must reach the alarm — silent casting would break stealth.");
        }

        [Test]
        public void Test_WhisperingIsQuieterThanShouting()
        {
            AlarmFSMManager whisperAlarm = MakeAlarmListener(new Vector3(0f, 0f, 0.4f));
            SpellEffectRegistry.Execute(Context(SpellId.Ignis, CastVolume.Whisper, Vector3.zero));
            float whisperLevel = whisperAlarm.AlarmLevel;

            AlarmFSMManager shoutAlarm = MakeAlarmListener(new Vector3(0f, 0f, 0.4f));
            SpellEffectRegistry.Execute(Context(SpellId.Ignis, CastVolume.Shout, Vector3.zero));
            float shoutLevel = shoutAlarm.AlarmLevel;

            Assert.Greater(shoutLevel, whisperLevel,
                "Shouting must raise the alarm more than whispering — that is the core trade-off.");
        }

        [Test]
        public void Test_ALeapAndItsSlamStirTheCastle()
        {
            AlarmFSMManager alarm = MakeAlarmListener(new Vector3(0f, 0f, 3f));

            SpellEffectRegistry.Execute(Context(SpellId.Saltus, CastVolume.Normal, Vector3.zero));
            float afterWord = alarm.AlarmLevel;
            SaltusEffect.ResolveSlam(Vector3.zero, SpellTuning.SaltusSlamSpeed, null, ~0, 0);

            Assert.Greater(alarm.AlarmLevel, afterWord, "The slam's landing makes a noise of its own.");
            Assert.GreaterOrEqual((int)alarm.State, (int)AlarmState.Stirred,
                "Saying the word and landing the slam must at least stir the castle.");
        }

        // --- Unregistered ids -----------------------------------------------------------------

        [Test]
        public void Test_UnknownSpellReportsNoEffectRatherThanSilentlyPassing()
        {
            int affected = SpellEffectRegistry.Execute(
                Context(SpellId.Aqua, CastVolume.Normal, Vector3.zero));

            Assert.AreEqual(-1, affected,
                "An id with no effect must report -1, distinct from 'affected nothing'.");
        }

        // --- Test doubles -----------------------------------------------------------------------

        /// <summary>A MonoBehaviour health target, so StatusEffectReceiver can resolve it via GetComponent.</summary>
        private class FakeHealthComponent : MonoBehaviour, IHealth
        {
            public float TotalDamage { get; private set; }
            public float CurrentHealth => 100f - TotalDamage;
            public float MaxHealth => 100f;
            public void TakeDamage(float damage) => TotalDamage += damage;
            public void TakeDamage(float damage, float impactVelocity) => TakeDamage(damage);
        }
    }
}
