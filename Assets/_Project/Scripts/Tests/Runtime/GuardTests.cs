using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// What the guards report to the castle, and the pure decisions in <see cref="GuardBrain"/> that the
    /// fresh guard still reads (#214). The behaviour of one guard (seeing, hearing, patrolling, chasing) is
    /// tested per state in GuardCore*Tests, GuardPatrolTests, GuardInvestigateTests and the others; the
    /// legacy tests that stepped a CastleGuard were moved there or dropped, see "Parity check (#214)" in
    /// docs/plans/guard-core-inventory.md.
    /// </summary>
    public class GuardTests
    {
        private readonly GuardCoreRig _rig = new GuardCoreRig();
        private readonly List<Object> _spawned = new List<Object>();

        [SetUp]
        public void SetUp() => _rig.SetUp();

        [TearDown]
        public void TearDown()
        {
            _rig.TearDown();
            foreach (Object spawned in _spawned)
            {
                if (spawned != null)
                    Object.DestroyImmediate(spawned);
            }
            _spawned.Clear();
        }

        // --- Pure decisions --------------------------------------------------------------------

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
        public void Test_AnAlertedCastleSeesFurther()
        {
            float calm = GuardBrain.SightRange(10f, AlarmState.Calm);
            float roused = GuardBrain.SightRange(10f, AlarmState.Roused);
            float hueAndCry = GuardBrain.SightRange(10f, AlarmState.HueAndCry);

            Assert.Greater(roused, calm);
            Assert.Greater(hueAndCry, roused);
        }

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

        // --- What a guard reports to the director (#163) ---------------------------------------

        [Test]
        public void Test_ASpawnedGuardKeepsTheAlarmItFound()
        {
            var body = new GameObject("Guard");
            _spawned.Add(body);
            Guard guard = body.AddComponent<Guard>();
            guard.Configure(null);   // what a spawner used to do

            Assert.AreSame(_rig.Director, guard.Link.Director,
                "Configure(null) wiped the alarm every spawned guard had found, so none could report a sighting.");
        }

        [Test]
        public void Test_ASightingReachesTheAlarm()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.MakeIntruder(new Vector3(0f, 0f, 6f));
            float before = _rig.Director.AlarmLevel;

            LookAndTick(guard);

            Assert.AreEqual(GuardAlertState.Chasing, guard.State, "Test premise: it sees the intruder.");
            Assert.Greater(_rig.Director.AlarmLevel, before, "Spotting an intruder must raise the alarm.");
        }

        [Test]
        public void Test_ASpottingRaisesTheAlarmAgainOnTheNextChase()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.MakeIntruder(new Vector3(0f, 0f, 5f));

            LookAndTick(guard);
            Assert.AreEqual(GuardAlertState.Chasing, guard.State, "Sanity: the first chase has begun.");

            guard.ChangeState(guard.States.Patrol);
            _rig.Director.SetAlarmLevel(0f);
            LookAndTick(guard);

            Assert.AreEqual(GuardAlertState.Chasing, guard.State);
            Assert.Greater(_rig.Director.AlarmLevel, 0f,
                "A guard back on patrol must raise the alarm again when it spots someone.");
        }

        // Two looks: a patrolling guard that sees someone goes to Investigate first, and Investigate hands a
        // sighting on to Chase (docs/plans/guard-fsm-restructure.md, the state table).
        private static void LookAndTick(Guard guard)
        {
            for (int look = 0; look < 2; look++)
            {
                guard.Sight.LookNext();
                guard.Tick(0.1f);
            }
        }

        // --- The director's own rules ----------------------------------------------------------

        [Test]
        public void Test_FiveGuardsChasingIsHueAndCry()
        {
            EnemyDirector alarm = _rig.Director;

            alarm.ReportChase(1, true);
            alarm.ReportChase(2, true);
            Assert.Less(alarm.State, AlarmState.Roused, "Two guards on the chase is not the castle up in arms (#259).");
            alarm.ReportChase(3, true);
            Assert.AreEqual(AlarmState.Roused, alarm.State, "Three guards chasing at once rouse the castle.");
            alarm.ReportChase(4, true);
            Assert.AreEqual(AlarmState.Roused, alarm.State, "Four is still the lockdown.");
            alarm.ReportChase(5, true);
            Assert.AreEqual(AlarmState.HueAndCry, alarm.State,
                "Five guards chasing at once must reach Hue and Cry (#139, #259).");
        }

        [Test]
        public void Test_GuardsSpottingAndAttackingRaiseTheAlarmThroughWalls()
        {
            EnemyDirector alarm = _rig.Director;

            alarm.ReportSighting(1);
            float afterSighting = alarm.AlarmLevel;
            alarm.ReportAttack();

            Assert.GreaterOrEqual(afterSighting, 20f, "A sighting is not muffled by the walls between guard and alarm.");
            Assert.Greater(alarm.AlarmLevel, afterSighting, "An attack adds to it.");
        }

        [Test]
        public void Test_ANewRaidStartsCalmAndStaysCalmThroughTheGrace()
        {
            EnemyDirector alarm = _rig.Director;
            alarm.SetAlarmLevel(100f, 5);
            Assert.AreEqual(AlarmState.HueAndCry, alarm.State);

            alarm.ResetForNewRaid(20f);
            Assert.AreEqual(AlarmState.Calm, alarm.State, "The last raid's Hue and Cry must not carry over (#136).");
            Assert.IsFalse(alarm.IsLocked);

            alarm.Publish(new NoiseReported(alarm, Vector3.zero, 1f));
            alarm.ReportSighting(1);
            alarm.ReportChase(1, true);
            alarm.ReportChase(2, true);
            alarm.ReportChase(3, true);
            Assert.AreEqual(0f, alarm.AlarmLevel, "Nothing raises the alarm during the arrival grace.");

            alarm.ResetForNewRaid(0f);
            alarm.ReportSighting(1);
            Assert.Greater(alarm.AlarmLevel, 0f, "After the grace the alarm works again.");
        }

        [Test]
        public void Test_IntruderTagRegistersAndUnregisters()
        {
            var go = new GameObject("TaggedPlayer");
            _spawned.Add(go);
            go.AddComponent<IntruderTag>();
            Transform playerTransform = go.transform;

            Assert.IsTrue(_rig.Director.IsIntruder(playerTransform),
                "A tagged player must be visible to guards at runtime.");

            Object.DestroyImmediate(go);
            Assert.IsFalse(_rig.Director.IsIntruder(playerTransform),
                "...and must stop being watched for once it is gone.");
        }
    }
}
