using System.Collections;
using Plunderspell.Acoustics;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>The Investigate state (#208): a noise or the hue and cry sends a patrolling guard to look, a newer stimulus re-targets it, it gives up on an unreachable spot, and it goes back to Patrol when nothing is found.</summary>
    public class GuardInvestigateTests
    {
        private const float Step = 0.05f;
        private const int StepsToFinishALook = 600;
        private static readonly Vector3 NoiseSpot = new Vector3(0f, 0f, 8f);

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
        public void ANoiseMakesAPatrollingGuardInvestigateThenReturnToPatrol()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);

            guard.OnNoiseHeard(new NoiseEvent(NoiseSpot, 0.6f, NoiseType.VoiceCast));
            guard.Tick(Step);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate));
            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Investigating));
            float closest = float.MaxValue;
            for (int step = 0; step < StepsToFinishALook && guard.CurrentState != guard.States.Patrol; step++)
            {
                _rig.RunNavigation(guard, 1, Step);
                closest = Mathf.Min(closest, Vector3.Distance(guard.transform.position, NoiseSpot));
            }
            Assert.That(closest, Is.LessThan(1f), "it went to the noise");
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Patrol), "nothing found, so back to patrol");
        }

        [Test]
        public void TheDirectorsInvestigateRequestSendsAGuardInRange()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Vector3 playerSpot = new Vector3(0f, 0f, 20f);

            _rig.Director.Publish(new InvestigateRequest(playerSpot, InvestigateReason.HueAndCry));
            guard.Tick(Step);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate));
            float offset = Vector3.Distance(((InvestigateState)guard.CurrentState).Spot, playerSpot);
            Assert.That(offset, Is.InRange(GuardBrain.HuntOffsetMin - 0.01f, GuardBrain.HuntOffsetMax + 0.01f), "roughly where the player is, not exactly");
        }

        [Test]
        public void TheDirectorsInvestigateRequestIgnoresAGuardOutOfRange()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);

            _rig.Director.Publish(new InvestigateRequest(new Vector3(0f, 0f, GuardDirectorLink.HueAndCryRadius + 10f), InvestigateReason.Noise));
            guard.Tick(Step);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Patrol));
        }

        [Test]
        public void TheHueAndCryReachesAGuardEightyMetresAway()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);

            _rig.Director.Publish(new InvestigateRequest(new Vector3(0f, 0f, 80f), InvestigateReason.HueAndCry));
            guard.Tick(Step);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate));
        }

        // A frame passes between the two raises, as in the game: a guard keeps only the nearest request of one frame.
        [UnityTest]
        public IEnumerator ARepeatOfTheHueAndCrySendsAnInvestigatingGuardToTheNewSpot()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Transform player = _rig.MakeIntruder(new Vector3(0f, 0f, 60f));
            _rig.Director.SetAlarmLevel(100f, 5); // Hue and Cry needs five witnesses (#259)
            guard.Tick(Step);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate));

            yield return null;
            player.position = new Vector3(70f, 0f, 0f);
            _rig.Director.TickHueAndCry(4f);
            guard.Tick(Step);

            float offset = Vector3.Distance(((InvestigateState)guard.CurrentState).Spot, player.position);
            Assert.That(offset, Is.InRange(GuardBrain.HuntOffsetMin - 0.01f, GuardBrain.HuntOffsetMax + 0.01f), "now heading for where the player is");
        }

        [Test]
        public void TheRepeatStopsWhenTheAlarmDropsBelowHueAndCry()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Transform player = _rig.MakeIntruder(new Vector3(0f, 0f, 60f));
            _rig.Director.SetAlarmLevel(100f, 5); // Hue and Cry needs five witnesses (#259)
            guard.Tick(Step);
            _rig.Director.SetAlarmLevel(0f);
            Vector3 spotBefore = ((InvestigateState)guard.CurrentState).Spot;

            player.position = new Vector3(70f, 0f, 0f);
            _rig.Director.TickHueAndCry(10f);
            guard.Tick(Step);

            Assert.That(((InvestigateState)guard.CurrentState).Spot, Is.EqualTo(spotBefore), "no repeat below Hue and Cry");
        }

        [Test]
        public void AStrongerNewStimulusRetargetsTheInvestigation()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.OnNoiseHeard(new NoiseEvent(NoiseSpot, 0.6f, NoiseType.Footstep));
            guard.Tick(Step);
            Vector3 newSpot = new Vector3(15f, 0f, 0f);

            guard.OnNoiseHeard(new NoiseEvent(newSpot, 0.9f, NoiseType.VoiceCast));
            guard.Tick(Step);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate));
            Assert.That(((InvestigateState)guard.CurrentState).Spot, Is.EqualTo(newSpot));
        }

        [Test]
        public void AWeakerNewStimulusDoesNotPullTheGuardAway()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.OnNoiseHeard(new NoiseEvent(NoiseSpot, 0.9f, NoiseType.VoiceCast));
            guard.Tick(Step);

            guard.OnNoiseHeard(new NoiseEvent(new Vector3(15f, 0f, 0f), 0.6f, NoiseType.Footstep));
            guard.Tick(Step);

            Assert.That(((InvestigateState)guard.CurrentState).Spot, Is.EqualTo(NoiseSpot));
            _rig.RunNavigation(guard, StepsToFinishALook, Step);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Patrol), "the weaker noise is forgotten, not queued");
        }

        [TestCase(BlockedReason.Unreachable)]
        [TestCase(BlockedReason.DoorClosed)]
        public void AnUnreachableSpotMakesTheGuardGiveUpAndPatrol(BlockedReason reason)
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.OnNoiseHeard(new NoiseEvent(NoiseSpot, 0.6f, NoiseType.VoiceCast));
            guard.Tick(Step);

            _rig.Director.Publish(new Blocked(guard, guard.transform.position, reason));
            guard.Tick(Step);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Patrol));
        }

        [Test]
        public void SeeingAPlayerWhileInvestigatingRaisesTheChaseHandOff()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.OnNoiseHeard(new NoiseEvent(new Vector3(0f, 0f, -8f), 0.6f, NoiseType.VoiceCast));
            guard.Tick(Step);
            int seen = 0;
            ((InvestigateState)guard.States.Investigate).PlayerSeen += player => seen++;
            _rig.MakeIntruder(guard.transform.position + guard.transform.forward * 5f);
            guard.Sight.LookNext();

            guard.Tick(Step);
            guard.Tick(Step);

            Assert.That(seen, Is.EqualTo(1), "one sighting is one hand-off, not one per look");
        }
    }
}
