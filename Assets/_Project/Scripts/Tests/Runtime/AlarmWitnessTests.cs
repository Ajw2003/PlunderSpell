using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The alarm needs witnesses (#259): states wait for guards that have seen an intruder, the castle
    /// scores only the noise its guards hear, and a guard's cry brings guards without scoring.</summary>
    public class AlarmWitnessTests
    {
        private GuardCoreRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown() => _rig.TearDown();

        // --- The witness gates ----------------------------------------------------------------------

        [Test]
        public void OneSightingAndASomnusLikeNoiseHeardByOneGuardIsStirredAtMost()
        {
            EnemyDirector alarm = _rig.Director;

            alarm.ReportSighting(1);
            alarm.Publish(new NoiseReported(alarm, Vector3.zero, 0.9f));
            alarm.Publish(new NoiseReported(alarm, Vector3.zero, 0.6f));

            Assert.That(alarm.State, Is.EqualTo(AlarmState.Stirred));
        }

        [Test]
        public void PointsKeepAccumulatingWhileTheWitnessesAreMissing()
        {
            EnemyDirector alarm = _rig.Director;
            alarm.ReportSighting(1);

            alarm.SetAlarmLevel(95f);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Stirred), "95 points, one witness");
            Assert.That(alarm.AlarmLevel, Is.EqualTo(95f));

            alarm.ReportSighting(2);
            alarm.ReportSighting(3);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Roused), "three witnesses open the lockdown, not the Hue and Cry");
        }

        [Test]
        public void ThreeSightingGuardsAndPointsRouseTheCastleAndFiveRaiseTheHueAndCry()
        {
            EnemyDirector alarm = _rig.Director;

            for (int guardId = 1; guardId <= 4; guardId++)
                alarm.ReportSighting(guardId);
            Assert.That(alarm.AlarmLevel, Is.EqualTo(80f));
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Roused), "80 points and four witnesses");

            alarm.ReportSighting(5);
            Assert.That(alarm.State, Is.EqualTo(AlarmState.HueAndCry));
        }

        [Test]
        public void TheSameGuardSightingAgainIsStillOneWitness()
        {
            EnemyDirector alarm = _rig.Director;
            for (int i = 0; i < 4; i++)
                alarm.ReportSighting(1);

            Assert.That(alarm.AlarmLevel, Is.EqualTo(80f));
            Assert.That(alarm.State, Is.EqualTo(AlarmState.Stirred));
        }

        [Test]
        public void ANewRaidForgetsTheWitnesses()
        {
            EnemyDirector alarm = _rig.Director;
            for (int guardId = 1; guardId <= 3; guardId++)
                alarm.ReportSighting(guardId);
            alarm.ResetForNewRaid(0f);

            alarm.SetAlarmLevel(60f);

            Assert.That(alarm.State, Is.EqualTo(AlarmState.Stirred));
        }

        // --- The castle hears through its guards -----------------------------------------------------

        [Test]
        public void ANoiseNoGuardHeardScoresNothing()
        {
            EnemyDirector alarm = _rig.Director;
            alarm.gameObject.AddComponent<SphereCollider>().radius = 5f;
            Physics.SyncTransforms();

            int heard = NoiseBroadcaster.Broadcast(Vector3.zero, 10f, 1f, NoiseType.Gunshot);

            Assert.That(heard, Is.EqualTo(0), "the director no longer listens for itself");
            Assert.That(alarm.AlarmLevel, Is.EqualTo(0f));
        }

        [Test]
        public void ANoiseScoresOncePerGuardThatHeardItAndNotAgainWithinTwoSeconds()
        {
            Guard first = MakeListener(Vector3.zero);
            Guard second = MakeListener(new Vector3(4f, 0f, 0f));
            var noise = new NoiseEvent(Vector3.zero, 0.8f, NoiseType.Gunshot);

            first.OnNoiseHeard(noise);
            Assert.That(_rig.Director.AlarmLevel, Is.EqualTo(0.8f * 15f).Within(0.01f));

            first.OnNoiseHeard(noise);
            Assert.That(_rig.Director.AlarmLevel, Is.EqualTo(0.8f * 15f).Within(0.01f), "the same guard within 2 s");

            second.OnNoiseHeard(noise);
            Assert.That(_rig.Director.AlarmLevel, Is.EqualTo(2f * 0.8f * 15f).Within(0.01f), "a second guard hears it too");
        }

        [Test]
        public void ASleepingGuardHearsNothingSoReportsNothing()
        {
            Guard guard = MakeListener(Vector3.zero);
            guard.Status.Sleep(30f);

            guard.OnNoiseHeard(new NoiseEvent(Vector3.zero, 0.3f, NoiseType.Footstep));

            Assert.That(_rig.Director.AlarmLevel, Is.EqualTo(0f));
        }

        // --- The cry for help -----------------------------------------------------------------------

        [Test]
        public void ACryScoresNothingAndItsCrierIgnoresIt()
        {
            Guard crier = MakeListener(Vector3.zero);
            Guard listener = MakeListener(new Vector3(10f, 0f, 0f));

            crier.Cry.Raise();

            Assert.That(_rig.Director.AlarmLevel, Is.EqualTo(0f), "a cry never raises alarm points");
            Assert.That(crier.Leads.HasLead, Is.False, "a guard ignores its own cry");
            Assert.That(listener.Leads.HasLead, Is.True);
        }

        [Test]
        public void AGuardWithinCryRangeGoesToWhereTheCrierStood()
        {
            Guard crier = MakeListener(new Vector3(3f, 0f, 2f));
            Guard listener = MakeListener(new Vector3(15f, 0f, 2f));

            crier.Cry.Raise();

            Assert.That(listener.Leads.HasLead, Is.True);
            Assert.That(listener.Leads.Take(), Is.EqualTo(crier.transform.position));
        }

        [Test]
        public void AGuardBeyondCryRangeDoesNotHearIt()
        {
            Guard crier = MakeListener(Vector3.zero);
            Guard far = MakeListener(new Vector3(GuardCry.Radius + 4f, 0f, 0f));

            crier.Cry.Raise();

            Assert.That(far.Leads.HasLead, Is.False);
        }

        [Test]
        public void ThreeWallsMuffleACryToNothingAndOneWallDoesNot()
        {
            Guard crier = MakeListener(Vector3.zero);
            crier.Tuning.GeometryLayers = ~0;
            Guard listener = MakeListener(new Vector3(12f, 0f, 0f));

            _rig.MakeWall(new Vector3(4f, 0f, 0f), new Vector3(0.2f, 4f, 8f));
            Physics.SyncTransforms();
            crier.Cry.Raise();
            Assert.That(listener.Leads.HasLead, Is.True, "one wall halves it but it is still heard");

            listener.Leads.Clear();
            _rig.MakeWall(new Vector3(6f, 0f, 0f), new Vector3(0.2f, 4f, 8f));
            _rig.MakeWall(new Vector3(8f, 0f, 0f), new Vector3(0.2f, 4f, 8f));
            Physics.SyncTransforms();
            crier.Cry.Raise();
            Assert.That(listener.Leads.HasLead, Is.False, "three walls drop it below hearing");
        }

        [Test]
        public void AGuardsFirstSightingMakesItCryAndOnlyTheSightingScores()
        {
            Guard crier = MakeListener(Vector3.zero);
            Guard listener = MakeListener(new Vector3(-12f, 0f, 0f));
            _rig.MakeIntruder(new Vector3(0f, 0f, 5f));

            for (int look = 0; look < 2; look++)
            {
                crier.Sight.LookNext();
                crier.Tick(0.1f);
            }

            Assert.That(crier.State, Is.EqualTo(GuardAlertState.Chasing), "premise: it saw the intruder");
            Assert.That(listener.Leads.HasLead, Is.True, "the cry reached the other guard");
            Assert.That(_rig.Director.AlarmLevel, Is.EqualTo(20f), "the sighting scores; the cry adds nothing");
        }

        // A guard with a body to hear through: the rig's guards have no collider, so no broadcast reaches them.
        private Guard MakeListener(Vector3 position)
        {
            Guard guard = _rig.MakeGuard(position);
            guard.gameObject.AddComponent<CapsuleCollider>();
            return guard;
        }
    }
}
