using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The fresh guard core's senses (#206): the sight throttle, the cone and line of sight, the arrival grace, and hearing.</summary>
    public class GuardCoreSensesTests
    {
        private const float Frame = 1f / 60f;

        private GuardCoreRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown() => _rig.TearDown();

        [Test]
        public void SightThrottleLooksAboutTwelveTimesASecond()
        {
            var throttle = new GuardSightThrottle(7);
            int looks = 0;
            for (int frame = 0; frame < 60; frame++)
            {
                if (throttle.IsLookDue(Frame))
                    looks++;
            }
            Assert.That(looks, Is.InRange(11, 12), "60 frames at 60 fps is one second");
        }

        [Test]
        public void SightThrottleStaggersGuardsSoTheyDoNotAllLookTogether()
        {
            var first = new GuardSightThrottle(100);
            var second = new GuardSightThrottle(600);
            Assert.That(first.Phase, Is.Not.EqualTo(second.Phase).Within(0.001f));
            Assert.That(first.Phase, Is.InRange(0f, 1f / GuardSightThrottle.LooksPerSecond));
        }

        [Test]
        public void GuardTakesAboutTwelveLooksASecondNotOnePerFrame()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.MakeIntruder(new Vector3(0f, 0f, 5f));
            for (int frame = 0; frame < 60; frame++)
                guard.Tick(Frame);
            Assert.That(guard.Sight.LookCount, Is.InRange(11, 13));
        }

        [Test]
        public void GuardSeesAnIntruderInFrontButNotBehind()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Transform front = _rig.MakeIntruder(new Vector3(0f, 0f, 6f));
            guard.Sight.LookNext();
            guard.Tick(Frame);
            Assert.That(guard.Sight.Visible, Is.EqualTo(front));

            front.position = new Vector3(0f, 0f, -6f);
            guard.Sight.LookNext();
            guard.Tick(Frame);
            Assert.That(guard.Sight.Visible, Is.Null, "behind the guard is outside the cone");
        }

        [Test]
        public void AWallBlocksSightButTheTargetsOwnColliderDoesNot()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Tuning.GeometryLayers = ~0;
            Transform intruder = _rig.MakeIntruder(new Vector3(0f, 0f, 6f));

            guard.Sight.LookNext();
            guard.Tick(Frame);
            Assert.That(guard.Sight.Visible, Is.EqualTo(intruder), "the intruder's own capsule must not hide it");

            _rig.MakeWall(new Vector3(0f, 1f, 3f), new Vector3(4f, 3f, 0.5f));
            Physics.SyncTransforms();
            guard.Sight.LookNext();
            guard.Tick(Frame);
            Assert.That(guard.Sight.Visible, Is.Null, "a wall in between blocks the line");
        }

        [Test]
        public void NoOneIsSeenDuringTheCalmArrivalGraceButGuardsSeeAfterIt()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.MakeIntruder(new Vector3(0f, 0f, 6f));

            GuardArrivalGrace.Begin();
            guard.Sight.LookNext();
            guard.Tick(Frame);
            Assert.That(guard.Sight.Visible, Is.Null, "inside the 20 s grace while Calm");

            GuardArrivalGrace.End();
            guard.Sight.LookNext();
            guard.Tick(Frame);
            Assert.That(guard.Sight.Visible, Is.Not.Null);
        }

        [Test]
        public void ALoudNoiseWakesASleepingGuardAndAQuietOneDoesNot()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Sleep(30f);

            guard.OnNoiseHeard(new NoiseEvent(new Vector3(0f, 0f, 8f), 0.3f, NoiseType.Footstep));
            Assert.That(guard.Status.IsAsleep, Is.True, "0.3 is under the wake threshold");

            guard.OnNoiseHeard(new NoiseEvent(new Vector3(0f, 0f, 8f), 0.6f, NoiseType.VoiceCast));
            Assert.That(guard.Status.IsAsleep, Is.False);
        }

        [Test]
        public void HearingThresholdFollowsTheAlarmLevel()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Vector3 heardAt = Vector3.zero;
            guard.Hearing.NoiseNoticed += (origin, strength) => heardAt = origin;
            const float quiet = GuardBrain.NoiseNoticeThreshold * 0.6f;
            var noise = new NoiseEvent(new Vector3(0f, 0f, 8f), quiet, NoiseType.Footstep);

            guard.OnNoiseHeard(noise);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(0), "background noise while Calm");

            _rig.Director.SetAlarmLevel(60f);
            guard.OnNoiseHeard(noise);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(1), "the same noise matters once Roused");
            Assert.That(heardAt, Is.EqualTo(noise.Origin));
        }
    }
}
