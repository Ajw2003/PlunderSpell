using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using Plunderspell.Status;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Tests for the guards — the reason noise matters. Most of these drive
    /// <see cref="GuardBrain"/> directly, because that is where the decisions live; the rest step a
    /// real <see cref="CastleGuard"/> through a situation one tick at a time.
    /// </summary>
    public class GuardTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        // The arrival grace is process-wide: a test elsewhere that started a raid would otherwise
        // leave these guards unable to see anyone.
        [SetUp]
        public void SetUp() => CastleGuard.EndArrivalGrace();

        [TearDown]
        public void TearDown()
        {
            CastleGuard.ClearIntruders();
            foreach (Object o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _spawned.Add(o);
            return o;
        }

        private CastleGuard MakeGuard(Vector3 position, AlarmFSMManager alarm = null)
        {
            var go = Track(new GameObject("Guard"));
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            var guard = go.AddComponent<CastleGuard>();
            guard.Configure(alarm);
            return guard;
        }

        private AlarmFSMManager MakeAlarm()
        {
            var go = Track(new GameObject("Alarm"));
            return go.AddComponent<AlarmFSMManager>();
        }

        private Transform MakeIntruder(Vector3 position)
        {
            var go = Track(new GameObject("Intruder"));
            go.transform.position = position;
            CastleGuard.RegisterIntruder(go.transform);
            return go.transform;
        }

        // --- Hearing ------------------------------------------------------------------------

        [Test]
        public void Test_ALoudNoisePullsAGuardOffPatrol()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Assert.AreEqual(GuardAlertState.Patrolling, guard.State);

            guard.OnNoiseHeard(new NoiseEvent(new Vector3(0f, 0f, 8f), 0.6f, NoiseType.VoiceCast));

            Assert.AreEqual(GuardAlertState.Investigating, guard.State,
                "A guard that hears a spell being shouted must come and look.");
            Assert.IsTrue(guard.InvestigationTarget.HasValue);
        }

        [Test]
        public void Test_AQuietNoiseIsIgnoredWhileCalm()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);

            guard.OnNoiseHeard(new NoiseEvent(new Vector3(0f, 0f, 8f), 0.05f, NoiseType.Footstep));

            Assert.AreEqual(GuardAlertState.Patrolling, guard.State,
                "A whispered cast must not give the player away.");
        }

        [Test]
        public void Test_AnAlertedCastleMakesGuardsJumpier()
        {
            const float quiet = GuardBrain.NoiseNoticeThreshold * 0.6f;

            Assert.IsFalse(GuardBrain.ShouldInvestigate(quiet, AlarmState.Calm),
                "While calm, that noise is background.");
            Assert.IsTrue(GuardBrain.ShouldInvestigate(quiet, AlarmState.Roused),
                "Once roused, the same noise is worth checking.");
        }

        [Test]
        public void Test_LoudNoiseWakesASleepingGuard()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            StatusEffectReceiver status = guard.GetComponent<StatusEffectReceiver>();
            status.Sleep(30f);
            Assert.IsTrue(guard.IsIncapacitated);

            guard.OnNoiseHeard(new NoiseEvent(Vector3.zero, 0.9f, NoiseType.Explosion));

            Assert.IsFalse(status.IsAsleep,
                "Somnus buys time; it does not remove a patrol. A thunderclap wakes them.");
        }

        [Test]
        public void Test_AQuietNoiseDoesNotWakeASleepingGuard()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            StatusEffectReceiver status = guard.GetComponent<StatusEffectReceiver>();
            status.Sleep(30f);

            guard.OnNoiseHeard(new NoiseEvent(Vector3.zero, 0.2f, NoiseType.Footstep));

            Assert.IsTrue(status.IsAsleep, "Tiptoeing past a sleeping guard must work.");
        }

        // --- Seeing -------------------------------------------------------------------------

        [Test]
        public void Test_ACalmGuardSeesNobodyDuringTheArrivalGrace()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            MakeIntruder(new Vector3(0f, 0f, 6f));

            CastleGuard.BeginArrivalGrace();
            guard.Tick(0.1f);

            Assert.AreEqual(GuardAlertState.Patrolling, guard.State,
                "Players arriving at the gate need a moment before the garrison can see them.");
        }

        [Test]
        public void Test_AGuardSeesAnIntruderInFrontOfIt()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            MakeIntruder(new Vector3(0f, 0f, 6f));   // dead ahead: guards face +Z by default

            guard.Tick(0.1f);

            Assert.AreEqual(GuardAlertState.Chasing, guard.State);
        }

        [Test]
        public void Test_AGuardDoesNotSeeAnIntruderBehindIt()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            MakeIntruder(new Vector3(0f, 0f, -6f));

            guard.Tick(0.1f);

            Assert.AreEqual(GuardAlertState.Patrolling, guard.State,
                "Sneaking up behind a guard must work.");
        }

        [Test]
        public void Test_AGuardDoesNotSeeAnIntruderTooFarAway()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            MakeIntruder(new Vector3(0f, 0f, 500f));

            guard.Tick(0.1f);

            Assert.AreEqual(GuardAlertState.Patrolling, guard.State);
        }

        [Test]
        public void Test_AnAlertedCastleSeesFurther()
        {
            float calm = GuardBrain.SightRange(10f, AlarmState.Calm);
            float roused = GuardBrain.SightRange(10f, AlarmState.Roused);
            float hueAndCry = GuardBrain.SightRange(10f, AlarmState.HueAndCry);

            Assert.Greater(roused, calm);
            Assert.Greater(hueAndCry, roused);
        }

        [Test]
        public void Test_ASleepingGuardSeesNothing()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            guard.GetComponent<StatusEffectReceiver>().Sleep(30f);
            MakeIntruder(new Vector3(0f, 0f, 4f));

            guard.Tick(0.1f);

            Assert.AreEqual(GuardAlertState.Incapacitated, guard.State,
                "A sleeping guard must not catch you standing in front of it.");
        }

        // --- Losing the trail ----------------------------------------------------------------

        [Test]
        public void Test_LosingSightLeadsToASearchThenBackToPatrol()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Transform intruder = MakeIntruder(new Vector3(0f, 0f, 5f));

            guard.Tick(0.1f);
            Assert.AreEqual(GuardAlertState.Chasing, guard.State);

            CastleGuard.UnregisterIntruder(intruder);   // vanished round a corner
            guard.Tick(0.1f);
            Assert.AreEqual(GuardAlertState.Searching, guard.State);

            guard.Tick(GuardBrain.SearchPatience + 1f);
            Assert.AreEqual(GuardAlertState.Patrolling, guard.State,
                "A calm castle eventually gives up looking.");
        }

        [Test]
        public void Test_AtHueAndCryTheHuntNeverEnds()
        {
            AlarmFSMManager alarm = MakeAlarm();
            alarm.SetAlarmLevel(95f);
            Assert.AreEqual(AlarmState.HueAndCry, alarm.State);

            CastleGuard guard = MakeGuard(Vector3.zero, alarm);
            Transform intruder = MakeIntruder(new Vector3(0f, 0f, 5f));

            guard.Tick(0.1f);
            CastleGuard.UnregisterIntruder(intruder);
            guard.Tick(0.1f);
            guard.Tick(GuardBrain.SearchPatience * 5f);

            Assert.AreEqual(GuardAlertState.Searching, guard.State,
                "Once the castle is fully up, letting it max out is not a decision you can take back.");
        }

        // --- Speed --------------------------------------------------------------------------

        [Test]
        public void Test_GuardsMoveFasterWhenChasingAndWhenTheCastleIsUp()
        {
            float patrol = GuardBrain.MoveSpeed(2f, 5f, GuardAlertState.Patrolling, AlarmState.Calm);
            float chase = GuardBrain.MoveSpeed(2f, 5f, GuardAlertState.Chasing, AlarmState.Calm);
            float chaseRoused = GuardBrain.MoveSpeed(2f, 5f, GuardAlertState.Chasing, AlarmState.HueAndCry);
            float stopped = GuardBrain.MoveSpeed(2f, 5f, GuardAlertState.Incapacitated, AlarmState.HueAndCry);

            Assert.Greater(chase, patrol);
            Assert.Greater(chaseRoused, chase);
            Assert.AreEqual(0f, stopped, "A stunned guard does not move.");
        }

        // --- Raising the cry -----------------------------------------------------------------

        [Test]
        public void Test_SpottingAnIntruderRaisesTheAlarm()
        {
            var alarmGo = Track(new GameObject("Alarm"));
            alarmGo.transform.position = new Vector3(1f, 0f, 0f);
            alarmGo.AddComponent<BoxCollider>();
            AlarmFSMManager alarm = alarmGo.AddComponent<AlarmFSMManager>();

            CastleGuard guard = MakeGuard(Vector3.zero, alarm);
            MakeIntruder(new Vector3(0f, 0f, 5f));

            guard.Tick(0.1f);

            Assert.AreEqual(GuardAlertState.Chasing, guard.State);
            Assert.Greater(alarm.AlarmLevel, 0f,
                "A guard that spots you shouts, and the shout reaches the alarm.");
        }

        [Test]
        public void Test_AGuardShoutsAgainOnItsNextChase()
        {
            AlarmFSMManager alarm = MakeAlarm();
            CastleGuard guard = MakeGuard(Vector3.zero, alarm);
            Transform intruder = MakeIntruder(new Vector3(0f, 0f, 5f));

            guard.Tick(0.1f);
            CastleGuard.UnregisterIntruder(intruder);
            guard.Tick(0.1f);
            guard.Tick(GuardBrain.SearchPatience + 1f);
            Assert.AreEqual(GuardAlertState.Patrolling, guard.State, "Sanity: the first chase is over.");

            alarm.SetAlarmLevel(0f);
            CastleGuard.RegisterIntruder(intruder);
            guard.Tick(0.1f);

            Assert.AreEqual(GuardAlertState.Chasing, guard.State);
            Assert.Greater(alarm.AlarmLevel, 0f, "A guard back on patrol must shout again when it spots someone.");
        }

        [Test]
        public void Test_ThreeGuardsChasingIsHueAndCry()
        {
            AlarmFSMManager alarm = MakeAlarm();

            alarm.ReportChase(1, true);
            Assert.Less(alarm.State, AlarmState.Roused, "One guard on the chase is not the castle up in arms.");
            alarm.ReportChase(2, true);
            Assert.AreEqual(AlarmState.Roused, alarm.State, "Two guards chasing at once rouse the castle.");
            alarm.ReportChase(3, true);
            Assert.AreEqual(AlarmState.HueAndCry, alarm.State,
                "Several guards chasing and attacking must reach Hue and Cry (#139).");
        }

        [Test]
        public void Test_GuardsSpottingAndAttackingRaiseTheAlarmThroughWalls()
        {
            AlarmFSMManager alarm = MakeAlarm();

            alarm.ReportSighting();
            float afterSighting = alarm.AlarmLevel;
            alarm.ReportAttack();

            Assert.GreaterOrEqual(afterSighting, 20f, "A sighting is not muffled by the walls between guard and alarm.");
            Assert.Greater(alarm.AlarmLevel, afterSighting, "An attack adds to it.");
        }

        [Test]
        public void Test_ANewRaidStartsCalmAndStaysCalmThroughTheGrace()
        {
            AlarmFSMManager alarm = MakeAlarm();
            alarm.SetAlarmLevel(100f);
            Assert.AreEqual(AlarmState.HueAndCry, alarm.State);

            alarm.ResetForNewRaid(20f);
            Assert.AreEqual(AlarmState.Calm, alarm.State, "The last raid's Hue and Cry must not carry over (#136).");
            Assert.IsFalse(alarm.IsLocked);

            alarm.ApplyNoise(1f);
            alarm.ReportSighting();
            alarm.ReportChase(1, true);
            alarm.ReportChase(2, true);
            alarm.ReportChase(3, true);
            Assert.AreEqual(0f, alarm.AlarmLevel, "Nothing raises the alarm during the arrival grace.");

            alarm.ResetForNewRaid(0f);
            alarm.ReportSighting();
            Assert.Greater(alarm.AlarmLevel, 0f, "After the grace the alarm works again.");
        }

        // --- Moving -------------------------------------------------------------------------

        [Test]
        public void Test_AGuardWithoutANavMeshStillWalksToTheNoise()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            var noiseAt = new Vector3(0f, 0f, 10f);

            guard.OnNoiseHeard(new NoiseEvent(noiseAt, 0.7f, NoiseType.GlassBreak));

            for (int i = 0; i < 20; i++)
                guard.Tick(0.1f);

            Assert.Less(Vector3.Distance(guard.transform.position, noiseAt), 10f,
                "A procedural castle has no baked NavMesh; guards must still be able to move.");
        }

        [Test]
        public void Test_AnIncapacitatedGuardDoesNotDrift()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            guard.OnNoiseHeard(new NoiseEvent(new Vector3(0f, 0f, 10f), 0.7f, NoiseType.GlassBreak));
            guard.GetComponent<StatusEffectReceiver>().Stun(30f);

            Vector3 before = guard.transform.position;
            for (int i = 0; i < 20; i++)
                guard.Tick(0.1f);

            Assert.AreEqual(before.z, guard.transform.position.z, 0.001f,
                "A stunned guard must stay exactly where it fell.");
        }

        [Test]
        public void Test_IntruderTagRegistersAndUnregisters()
        {
            var go = Track(new GameObject("TaggedPlayer"));
            go.AddComponent<IntruderTag>();
            Transform playerTransform = go.transform;

            Assert.Contains(playerTransform, (System.Collections.ICollection)CastleGuard.Intruders,
                "A tagged player must be visible to guards at runtime.");

            Object.DestroyImmediate(go);
            Assert.IsFalse(CastleGuard.Intruders.Contains(playerTransform),
                "…and must stop being watched for once it is gone.");
        }

        // --- Damage -------------------------------------------------------------------------

        [Test]
        public void Test_ABurningGuardEventuallyDies()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            StatusEffectReceiver status = guard.GetComponent<StatusEffectReceiver>();

            status.Ignite(50f, 10f);
            for (int i = 0; i < 10; i++)
                status.Tick(1f);

            Assert.IsTrue(guard.IsDead, "Ignis must be able to kill a guard outright.");
            Assert.AreEqual(GuardAlertState.Incapacitated, guard.State);
        }
    }
}
