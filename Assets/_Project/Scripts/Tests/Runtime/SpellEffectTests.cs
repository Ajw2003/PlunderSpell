using System.Collections.Generic;
using Interfaces;
using NUnit.Framework;
using RogueAi.Acoustics;
using RogueAi.Alarm;
using RogueAi.Loot;
using RogueAi.Spells;
using RogueAi.Status;
using RogueAi.Voice;
using UnityEngine;

namespace RogueAi.Tests
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
            data.Bulk = 2f;
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

        [Test]
        public void Test_FrangoShattersLoot()
        {
            LootPickup vase = MakeLoot(new Vector3(0f, 0f, 2f));

            int broken = SpellEffectRegistry.Execute(
                Context(SpellId.Frango, CastVolume.Normal, Vector3.zero));

            Assert.AreEqual(1, broken, "Frango should have broken the vase in range.");
            Assert.IsTrue(vase.IsBroken, "Frango must shatter loot — including your own.");
        }

        [Test]
        public void Test_FrangoIsIdempotentOnAlreadyBrokenLoot()
        {
            LootPickup vase = MakeLoot(new Vector3(0f, 0f, 2f));
            vase.Break();

            int broken = SpellEffectRegistry.Execute(
                Context(SpellId.Frango, CastVolume.Normal, Vector3.zero));

            Assert.AreEqual(0, broken, "Already-broken loot must not be counted again.");
        }

        // --- Somnus / Tonitrus --------------------------------------------------------------

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

        [Test]
        public void Test_TonitrusStunsEveryoneInRange()
        {
            StatusEffectReceiver a = MakeActor("GuardA", new Vector3(0f, 0f, 2f));
            StatusEffectReceiver b = MakeActor("GuardB", new Vector3(2f, 0f, 0f));

            int stunned = SpellEffectRegistry.Execute(
                Context(SpellId.Tonitrus, CastVolume.Normal, Vector3.zero));

            Assert.AreEqual(2, stunned, "Tonitrus is an area stun: both guards.");
            Assert.IsTrue(a.IsStunned && b.IsStunned);
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
                SpellId.Tonitrus, SpellId.Somnus, SpellId.CadaverSurge, SpellId.Porta
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
        public void Test_ShoutedTonitrusIsLoudEnoughToStirTheCastle()
        {
            AlarmFSMManager alarm = MakeAlarmListener(new Vector3(0f, 0f, 3f));

            SpellEffectRegistry.Execute(Context(SpellId.Tonitrus, CastVolume.Shout, Vector3.zero));

            Assert.GreaterOrEqual((int)alarm.State, (int)AlarmState.Stirred,
                "A shouted thunderclap must at least stir the castle.");
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
