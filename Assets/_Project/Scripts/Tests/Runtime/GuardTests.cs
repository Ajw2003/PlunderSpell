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

        // --- Raising the castle (#163) -------------------------------------------------------

        [Test]
        public void Test_ASpawnedGuardKeepsTheAlarmItFound()
        {
            AlarmFSMManager alarm = MakeAlarm();
            var go = Track(new GameObject("Guard"));
            go.AddComponent<BoxCollider>();
            var guard = go.AddComponent<CastleGuard>();
            guard.Configure(null);   // what GuardSpawner does

            Assert.AreSame(alarm, guard.Alarm,
                "Configure(null) wiped the alarm every spawned guard had found, so none could report a sighting.");
        }

        [Test]
        public void Test_ASightingReachesTheAlarm()
        {
            AlarmFSMManager alarm = MakeAlarm();
            CastleGuard guard = MakeGuard(Vector3.zero, alarm);
            MakeIntruder(new Vector3(0f, 0f, 6f));
            float before = alarm.AlarmLevel;

            guard.Tick(0.1f);

            Assert.AreEqual(GuardAlertState.Chasing, guard.State, "Test premise: it sees the intruder.");
            Assert.Greater(alarm.AlarmLevel, before, "Spotting an intruder must raise the alarm.");
        }

        [Test]
        public void Test_TheShoutSendsNearbyGuardsToTheIntruder()
        {
            CastleGuard spotter = MakeGuard(Vector3.zero);
            CastleGuard near = MakeGuard(new Vector3(-10f, 0f, -5f));
            CastleGuard far = MakeGuard(new Vector3(-40f, 0f, 0f));
            near.transform.rotation = Quaternion.Euler(0f, 180f, 0f);  // facing away: hears, cannot see
            far.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Vector3 intruder = new Vector3(0f, 0f, 6f);
            MakeIntruder(intruder);

            spotter.Tick(0.1f);

            Assert.AreEqual(GuardAlertState.Chasing, spotter.State, "Test premise: the spotter sees the intruder.");
            Assert.AreEqual(GuardAlertState.Investigating, near.State, "A guard in earshot must answer the shout.");
            Assert.AreEqual(intruder, near.InvestigationTarget, "It goes to the intruder, not to the shouter.");
            Assert.AreEqual(1, near.AlertsReceived);
            Assert.AreEqual(0, far.AlertsReceived, "A guard 40 m away is out of the shout's 20 m.");
        }

        [Test]
        public void Test_TheHueAndCrySendsGuardsNearAPlayerToThem()
        {
            AlarmFSMManager alarm = MakeAlarm();
            CastleGuard near = MakeGuard(new Vector3(0f, 0f, -30f), alarm);
            CastleGuard far = MakeGuard(new Vector3(0f, 0f, -80f), alarm);
            near.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            far.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            Vector3 player = Vector3.zero;
            MakeIntruder(player);

            alarm.SetAlarmLevel(100f);

            Assert.AreEqual(AlarmState.HueAndCry, alarm.State, "Test premise.");
            Assert.AreEqual(GuardAlertState.Investigating, near.State, "The hue and cry must send guards within 40 m.");
            Assert.AreEqual(player, near.InvestigationTarget);
            Assert.AreEqual(0, far.AlertsReceived, "A guard 80 m away is outside the hue and cry's reach.");
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

        /// <summary>
        /// #106: raid guards have no Rigidbody, and without one a guard Levo let go of was put
        /// straight back on the floor, a 1.8 m drop in one frame that cost it nothing. It must fall
        /// and take the fall damage.
        /// </summary>
        [Test]
        public void Test_ALevitatedGuardWithNoBodyFallsAndIsHurt()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Assert.IsNull(guard.GetComponent<Rigidbody>(), "Raid guards have no Rigidbody.");
            StatusEffectReceiver status = guard.GetComponent<StatusEffectReceiver>();
            float before = guard.CurrentHealth;

            status.Levitate(Vector3.up, 1f);
            for (int i = 0; i < 60 && status.IsLevitating; i++)
            {
                guard.UpdateLevitation(0.02f);
                status.Tick(0.02f);
            }
            Assert.Greater(guard.transform.position.y, 1.5f, "Levo must lift the guard.");

            Assert.IsTrue(guard.UpdateLevitation(0.02f), "Let go, the guard must fall, not land at once.");
            Assert.IsTrue(guard.IsAirborne);
            Assert.Greater(guard.transform.position.y, 1.5f, "The fall starts where the lift ended.");

            for (int i = 0; i < 200 && guard.IsAirborne; i++)
                guard.UpdateLevitation(0.02f);

            Assert.IsFalse(guard.IsAirborne, "The guard must land.");
            Assert.AreEqual(0f, guard.transform.position.y, 0.05f, "It lands where it was lifted from.");
            Assert.Less(guard.CurrentHealth, before - 10f, "A 1.8 m drop must hurt.");
        }

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
