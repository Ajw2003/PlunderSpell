using Code.Scripts.EventSystems;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>Attack turns (#210): the director's mediator lets one melee and one ranged guard attack a player at a time, takes a turn back when its holder dies, leaves combat, or runs out of time, and guards wait their turn on a ring.</summary>
    public class GuardAttackTurnTests
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
        public void TearDown()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            _rig.TearDown();
        }

        private Guard MeleeGuardInCombatWith(Transform player, Vector3 position)
        {
            Guard guard = _rig.MakeGuard(position);
            guard.States.Combat.Engage(player);
            guard.ChangeState(guard.States.Combat);
            return guard;
        }

        [Test]
        public void ThreeMeleeGuardsOnOnePlayerNeverStrikeAtOnceAndTakeTurns()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 3f));
            Guard first = _rig.MakeGuard(new Vector3(-1f, 0f, 0f));
            Guard second = _rig.MakeGuard(new Vector3(0f, 0f, 0f));
            Guard third = _rig.MakeGuard(new Vector3(1f, 0f, 0f));
            int mostHolding = 0;
            int shortestGapInSteps = int.MaxValue;
            int lastStrikeStep = -1000;
            int strikesSeen = 0;
            EventManager.Instance.Subscribe(this, (AttackTurnGranted granted) =>
                mostHolding = Mathf.Max(mostHolding, _rig.Director.AttackTurns.TurnsOn(victim.transform, false)));

            for (int i = 0; i < 160; i++)
            {
                _rig.StepTogether(1, Step, first, second, third);
                int strikesNow = first.AttackSignal.Count + second.AttackSignal.Count + third.AttackSignal.Count;
                if (strikesNow == strikesSeen)
                    continue;

                shortestGapInSteps = Mathf.Min(shortestGapInSteps, i - lastStrikeStep);
                lastStrikeStep = i;
                strikesSeen = strikesNow;
            }

            int recoverySteps = Mathf.RoundToInt(first.Tuning.AttackRecoverySeconds / Step);
            Assert.That(mostHolding, Is.EqualTo(1), "the limit is one melee turn per player, and it was used");
            Assert.That(strikesSeen, Is.GreaterThanOrEqualTo(3));
            Assert.That(shortestGapInSteps, Is.GreaterThanOrEqualTo(recoverySteps - 1), "a turn is held through the recovery, so strikes are spread out");
            Assert.That(first.AttackSignal.Count, Is.GreaterThanOrEqualTo(1), "each guard got a turn");
            Assert.That(second.AttackSignal.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(third.AttackSignal.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void AGuardWithoutATurnIsDeniedAndWaitsOnTheRing()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard holder = MeleeGuardInCombatWith(victim.transform, new Vector3(-1f, 0f, 0f));
            Guard waiter = MeleeGuardInCombatWith(victim.transform, new Vector3(1f, 0f, 0f));
            holder.AttackTurn.Request(victim.transform, false);

            waiter.AttackTurn.Request(victim.transform, false);

            Assert.That(holder.AttackTurn.HasTurn, Is.True);
            Assert.That(waiter.AttackTurn.HasTurn, Is.False, "one melee turn per player, and the holder has it");
        }

        [Test]
        public void AGuardWaitingItsTurnMovesToAPlaceOnTheRingOutsideReach()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 8f));
            Guard holder = MeleeGuardInCombatWith(victim.transform, new Vector3(0f, 0f, 7f));
            Guard waiter = MeleeGuardInCombatWith(victim.transform, new Vector3(0f, 0f, 6f));
            holder.AttackTurn.Request(victim.transform, false);

            // The holder is not stepped, so it keeps the turn (the 3 s timeout is not reached) while the waiter walks to its place.
            _rig.StepTogether(40, Step, waiter);

            float distance = Vector3.Distance(waiter.transform.position, victim.transform.position);
            float ring = waiter.Tuning.MeleeReach + waiter.Tuning.CombatRingPadding;
            Assert.That(distance, Is.EqualTo(ring).Within(0.6f), "it holds a place just outside melee reach");
            Assert.That(waiter.AttackSignal.Count, Is.EqualTo(0), "and does not strike without a turn");
        }

        [Test]
        public void ARangedTurnIsSeparateFromAMeleeTurn()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard melee = MeleeGuardInCombatWith(victim.transform, new Vector3(-1f, 0f, 0f));
            Guard archer = MeleeGuardInCombatWith(victim.transform, new Vector3(1f, 0f, 0f));

            melee.AttackTurn.Request(victim.transform, false);
            archer.AttackTurn.Request(victim.transform, true);

            Assert.That(melee.AttackTurn.HasTurn, Is.True);
            Assert.That(archer.AttackTurn.HasTurn, Is.True, "the issue gives one melee and one ranged turn");
        }

        [Test]
        public void ATurnIsReleasedWhenItsHolderDiesAndTheNextGuardGetsIt()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard holder = MeleeGuardInCombatWith(victim.transform, new Vector3(-1f, 0f, 0f));
            Guard next = MeleeGuardInCombatWith(victim.transform, new Vector3(1f, 0f, 0f));
            holder.AttackTurn.Request(victim.transform, false);
            Assert.That(_rig.Director.AttackTurns.Holds(holder), Is.True, "setup: the holder has the turn");

            holder.TakeDamage(1000f);

            Assert.That(_rig.Director.AttackTurns.Holds(holder), Is.False);
            next.AttackTurn.Request(victim.transform, false);
            Assert.That(next.AttackTurn.HasTurn, Is.True);
        }

        [Test]
        public void ATurnIsReleasedWhenItsHolderLeavesCombat()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard holder = MeleeGuardInCombatWith(victim.transform, new Vector3(-1f, 0f, 0f));
            holder.AttackTurn.Request(victim.transform, false);

            holder.ChangeState(holder.States.Patrol);

            Assert.That(_rig.Director.AttackTurns.Holds(holder), Is.False);
            Assert.That(_rig.Director.AttackTurns.ActiveTurns, Is.EqualTo(0));
        }

        [Test]
        public void ATurnNotUsedInTimeIsTakenBack()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard holder = MeleeGuardInCombatWith(victim.transform, new Vector3(-1f, 0f, 0f));
            holder.AttackTurn.Request(victim.transform, false);

            _rig.Director.AttackTurns.Tick(10f);

            Assert.That(_rig.Director.AttackTurns.Holds(holder), Is.False);
            Assert.That(holder.AttackTurn.HasTurn, Is.False, "the guard sees the turn is gone");
        }
    }
}
