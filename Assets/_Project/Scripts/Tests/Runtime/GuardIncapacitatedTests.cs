using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>Stunned and Slept (#211) on the flat rig: the timer, the exit by alarm level, a sleeper woken by a loud noise only, Levo holding a guard until it lands, and a stunned guard giving its attack turn back.</summary>
    public class GuardIncapacitatedTests
    {
        private const float Step = 0.05f;

        private GuardCoreRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown() => _rig.TearDown();

        // The status receiver counts its own timers in Update; a test with no frames ticks it by hand.
        private void Advance(Guard guard, float seconds)
        {
            int steps = Mathf.CeilToInt(seconds / Step);
            for (int i = 0; i < steps; i++)
            {
                guard.Status.Tick(Step);
                _rig.StepTogether(1, Step, guard);
            }
        }

        [Test]
        public void AStunnedGuardStandsStillForTheStunThenReturnsToPatrol()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.StepTogether(10, Step, guard);
            Assert.That(guard.Navigator.IsMoving, Is.True, "patrolling before the stun");

            guard.Status.Stun(1f);
            Vector3 stoppedAt = guard.transform.position;
            Advance(guard, 0.5f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Stunned));
            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Incapacitated));
            Assert.That(guard.Navigator.IsMoving, Is.False, "the move was cancelled on entry");
            Assert.That(guard.transform.position, Is.EqualTo(stoppedAt), "it stood still");

            Advance(guard, 0.7f);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Patrol), "calm castle: back to the round");
        }

        [Test]
        public void ARousedCastleSendsARecoveredGuardToInvestigate()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.Director.SetAlarmLevel(60f, 3); // Roused needs three witnesses (#259)

            guard.Status.Stun(0.5f);
            Advance(guard, 0.7f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate));
        }

        [Test]
        public void ASleeperWakesEarlyForALoudNoiseAndGoesToLookAtIt()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Vector3 noiseAt = new Vector3(0f, 0f, 8f);
            guard.Status.Sleep(30f);

            guard.OnNoiseHeard(new NoiseEvent(noiseAt, 0.3f, NoiseType.Footstep));
            Advance(guard, 0.2f);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Slept), "0.3 is under the wake threshold");

            guard.OnNoiseHeard(new NoiseEvent(noiseAt, 0.6f, NoiseType.VoiceCast));
            Advance(guard, 0.2f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate), "woken 30 s early");
            Assert.That(((InvestigateState)guard.States.Investigate).Spot, Is.EqualTo(noiseAt));
        }

        [Test]
        public void ASleeperLeftAloneWakesWhenTheSleepEndsAndPatrols()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Sleep(1f);

            Advance(guard, 0.5f);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Slept));

            Advance(guard, 0.7f);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Patrol));
        }

        [Test]
        public void ALevitatedGuardIsStunnedUntilItLandsAndItsMoverIsPausedMeanwhile()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Levitate(Vector3.up * 4f, 1f);

            // Regression (2026-10-02 co-op): the spell's impulse had no physics body to push, so the guard never left the floor.
            Advance(guard, 0.5f);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Stunned));
            Assert.That(guard.transform.position.y, Is.GreaterThan(1f), "Levo lifts the guard, and the paused mover does not pull it back down");

            Advance(guard, 0.6f);
            Assert.That(guard.Status.IsLevitating, Is.False, "the spell is over");
            Assert.That(guard.transform.position.y, Is.GreaterThan(0.1f), "it is still falling");
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Stunned), "and stays stunned until it lands");

            Advance(guard, 1.5f);
            Assert.That(guard.transform.position.y, Is.EqualTo(0f).Within(0.01f), "it falls back to the floor it left");
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Patrol), "recovers on landing");

            Vector3 resumedAt = guard.transform.position;
            Advance(guard, 1f);
            Assert.That(Vector3.Distance(guard.transform.position, resumedAt), Is.GreaterThan(0.5f), "the mover was resumed, so the patrol walks");
        }

        [Test]
        public void AStunnedGuardGivesItsAttackTurnBack()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.States.Combat.Engage(victim.transform);
            guard.ChangeState(guard.States.Combat);
            _rig.StepTogether(10, Step, guard);
            Assert.That(_rig.Director.AttackTurns.Holds(guard), Is.True, "the guard held a turn while fighting");

            guard.Status.Stun(2f);

            Assert.That(_rig.Director.AttackTurns.Holds(guard), Is.False);
            Assert.That(_rig.Director.AttackTurns.TurnsOn(victim.transform, false), Is.EqualTo(0));
        }
    }
}
