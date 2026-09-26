using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Loot;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// EditMode tests for Milestone 2b — loot fragility, dual-carry thresholds, the four-state alarm
    /// FSM and acoustic occlusion attenuation.
    ///
    /// These exercise the network-free logic seams (ApplyImpact / EvaluatePickup / ApplyNoise /
    /// SetAlarmLevel / ComputeAttenuatedStrength) so they run without a live PurrNet transport. The RPC
    /// wrappers and SyncVar replication are integration concerns validated in PlayMode on the target.
    /// </summary>
    public class LootAcousticsTests
    {
        private static LootPickup CreatePickup(LootItem data)
        {
            var go = new GameObject("LootPickup_Test");
            // RequireComponent(Rigidbody) auto-adds a Rigidbody on AddComponent.
            var pickup = go.AddComponent<LootPickup>();
            pickup.SetData(data);
            return pickup;
        }

        private static LootItem MakeItem(float worth, float bulk, float fragility)
        {
            var item = ScriptableObject.CreateInstance<LootItem>();
            item.Worth = worth;
            item.Bulk = bulk;
            item.Fragility = fragility;
            return item;
        }

        // ---------------------------------------------------------------- Fragility

        [Test]
        public void Test_LootItemFragilityBreaks()
        {
            var item = MakeItem(50f, 2f, fragility: 5f);
            var pickup = CreatePickup(item);

            pickup.ApplyImpact(10f); // 10 m/s > 5 m/s threshold → shatter

            Assert.IsTrue(pickup.IsBroken, "A 10 m/s impact should break a Fragility=5 item.");

            Object.DestroyImmediate(pickup.gameObject);
            Object.DestroyImmediate(item);
        }

        [Test]
        public void Test_LootItemSurvivesLowVelocity()
        {
            var item = MakeItem(50f, 2f, fragility: 5f);
            var pickup = CreatePickup(item);

            pickup.ApplyImpact(3f); // 3 m/s < 5 m/s threshold → survives

            Assert.IsFalse(pickup.IsBroken, "A 3 m/s impact should not break a Fragility=5 item.");

            Object.DestroyImmediate(pickup.gameObject);
            Object.DestroyImmediate(item);
        }

        // ---------------------------------------------------------------- Carry thresholds

        [Test]
        public void Test_DualCarryThreshold()
        {
            var item = MakeItem(200f, bulk: 11f, fragility: 999f);
            var pickup = CreatePickup(item);

            Assert.AreEqual(CarryMode.Dual, pickup.EvaluatePickup(),
                "Bulk 11 (> 10 stone) must require a dual carry.");

            Object.DestroyImmediate(pickup.gameObject);
            Object.DestroyImmediate(item);
        }

        [Test]
        public void Test_SingleCarryThreshold()
        {
            var item = MakeItem(60f, bulk: 9f, fragility: 6f);
            var pickup = CreatePickup(item);

            Assert.AreEqual(CarryMode.Single, pickup.EvaluatePickup(),
                "Bulk 9 (≤ 10 stone) should be a single carry.");

            Object.DestroyImmediate(pickup.gameObject);
            Object.DestroyImmediate(item);
        }

        // ---------------------------------------------------------------- Alarm FSM

        private static AlarmFSMManager CreateAlarm()
        {
            var go = new GameObject("AlarmFSM_Test");
            return go.AddComponent<AlarmFSMManager>();
        }

        [Test]
        public void Test_AlarmLevelIncreases()
        {
            var alarm = CreateAlarm();

            alarm.OnNoiseHeard(new NoiseEvent(Vector3.zero, 1.0f, NoiseType.Gunshot));

            Assert.Greater(alarm.AlarmLevel, 0f, "A strength-1.0 noise must raise the alarm level.");

            Object.DestroyImmediate(alarm.gameObject);
        }

        [Test]
        public void Test_AlarmStateCalmToStirred()
        {
            var alarm = CreateAlarm();

            alarm.SetAlarmLevel(25f);

            Assert.AreEqual(AlarmState.Stirred, alarm.State,
                "Level 25 (20–50) should map to Stirred.");

            Object.DestroyImmediate(alarm.gameObject);
        }

        [Test]
        public void Test_AlarmStateLocksAtRoused()
        {
            var alarm = CreateAlarm();

            alarm.SetAlarmLevel(55f); // 50–80 → Roused, and latches the lock
            Assert.AreEqual(AlarmState.Roused, alarm.State, "Level 55 should be Roused.");
            Assert.IsTrue(alarm.IsLocked, "Reaching Roused must latch the lock.");

            // Manually drop the level; a locked alarm must NOT regress below Roused.
            alarm.SetAlarmLevel(10f);
            Assert.AreEqual(AlarmState.Roused, alarm.State,
                "Once locked, the alarm must stay Roused even as the level falls.");

            Object.DestroyImmediate(alarm.gameObject);
        }

        // ---------------------------------------------------------------- Acoustic occlusion

        [Test]
        public void Test_AcousticOcclusionAttenuates()
        {
            // Two walls → 0.5^2 = 0.25 of the original strength.
            float attenuated = AcousticEmitter.ComputeAttenuatedStrength(1.0f, 2);

            Assert.AreEqual(0.25f, attenuated, 1e-4f,
                "Two walls should attenuate strength to original × 0.25.");
        }
    }
}
