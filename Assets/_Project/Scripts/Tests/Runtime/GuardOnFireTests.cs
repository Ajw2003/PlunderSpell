using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>OnFire (#212) on the flat rig: panic runs between distinct points, no attacks and no held turn, every way the fire can end (Combat, Chase, Patrol, Dead), stun outranking the fire, and fire overruling sleep (#236).</summary>
    public class GuardOnFireTests
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

        // Turns the guard to look at a spot, so the player there is inside its sight cone.
        private static void Face(Guard guard, Vector3 spot)
        {
            Vector3 toSpot = spot - guard.transform.position;
            guard.transform.rotation = Quaternion.LookRotation(new Vector3(toSpot.x, 0f, toSpot.z));
        }

        [Test]
        public void ABurningGuardPanicsAndRunsBetweenDistinctPoints()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.StepTogether(5, Step, guard);

            guard.Status.Ignite(1f, 30f);
            var destinations = new HashSet<Vector3>();
            for (int i = 0; i < 100; i++)
            {
                Advance(guard, Step);
                if (guard.Navigator.Destination.HasValue)
                    destinations.Add(guard.Navigator.Destination.Value);
            }

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.OnFire));
            Assert.That(guard.State, Is.EqualTo(GuardAlertState.OnFire), "the replicated state says OnFire");
            Assert.That(destinations.Count, Is.GreaterThanOrEqualTo(3), "it kept picking new points");
        }

        [Test]
        public void ABurningGuardDoesNotAttackAndGivesItsTurnBack()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.States.Combat.Engage(victim.transform);
            guard.ChangeState(guard.States.Combat);
            _rig.StepTogether(10, Step, guard);
            Assert.That(_rig.Director.AttackTurns.Holds(guard), Is.True, "the guard held a turn while fighting");

            guard.Status.Ignite(1f, 30f);
            float damageBefore = victim.DamageTaken;
            Advance(guard, 3f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.OnFire));
            Assert.That(_rig.Director.AttackTurns.Holds(guard), Is.False, "the turn was released");
            Assert.That(victim.DamageTaken, Is.EqualTo(damageBefore), "no strike while burning");
        }

        [Test]
        public void AFireThatEndsWithAPlayerInReachSendsTheGuardToCombat()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Ignite(1f, 0.3f);
            Advance(guard, 0.2f);
            Transform player = _rig.MakeIntruder(guard.transform.position + Vector3.forward);
            Face(guard, player.position);

            Advance(guard, 0.3f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Combat));
        }

        [Test]
        public void AFireThatEndsWithAPlayerOnlyInSightSendsTheGuardToChase()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Ignite(1f, 0.3f);
            Advance(guard, 0.2f);
            Transform player = _rig.MakeIntruder(guard.transform.position + Vector3.forward * 9f);
            Face(guard, player.position);

            Advance(guard, 0.3f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Chase));
            Assert.That(guard.States.Chase.Target, Is.SameAs(player));
        }

        [Test]
        public void AFireThatEndsWithNobodyAboutSendsTheGuardBackToPatrol()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Ignite(1f, 0.3f);
            Advance(guard, 0.2f);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.OnFire));

            Advance(guard, 0.3f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Patrol), "calm castle: back to the round");
        }

        [Test]
        public void AFireThatKillsTheGuardEntersDead()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Ignite(500f, 5f);

            for (int i = 0; i < 40 && !guard.IsDead; i++)
                Advance(guard, Step);

            Assert.That(guard.IsDead, Is.True, "the receiver's burn ticks killed it");
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Dead));
        }

        [Test]
        public void AStunOutranksTheFireAndTheGuardPanicsOnlyWhenTheStunEnds()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Ignite(1f, 30f);
            Advance(guard, 0.2f);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.OnFire));

            guard.Status.Stun(1f);
            Advance(guard, 0.5f);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Stunned), "burning does not break a stun");

            Advance(guard, 0.7f);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.OnFire), "the stun ended and the fire is still going");
        }

        [Test]
        public void FireLandingOnAStunnedGuardDoesNotPullItOutOfTheStun()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Stun(1f);
            Advance(guard, 0.2f);

            guard.Status.Ignite(1f, 30f);
            Advance(guard, 0.2f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Stunned));
        }

        [Test]
        public void FireOverrulesSleepSoASleepingGuardSetAlightWakesIntoOnFire()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Sleep(30f);
            Advance(guard, 0.2f);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Slept));

            guard.Status.Ignite(1f, 30f);
            Advance(guard, 0.1f);

            Assert.That(guard.Status.IsAsleep, Is.False, "the fire woke it");
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.OnFire), "straight to panic, not Patrol or Investigate");
        }

        [Test]
        public void SomnusOnABurningGuardLeavesItAwakeAndBurning()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Status.Ignite(1f, 30f);
            Advance(guard, 0.2f);

            guard.Status.Sleep(30f);
            Advance(guard, 0.2f);

            Assert.That(guard.Status.IsAsleep, Is.False);
            Assert.That(guard.Status.IsBurning, Is.True);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.OnFire));
        }
    }
}
