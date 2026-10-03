using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The Combat state (#210) on the flat rig: reach hands over from Chase, a melee guard strikes, Combat does not flicker back to Chase, a lost player ends it, and a player pinned against a wall is never pushed (#200).</summary>
    public class GuardCombatTests
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

        private Guard GuardInCombatWith(GuardCoreRig.Victim victim)
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.States.Combat.Engage(victim.transform);
            guard.ChangeState(guard.States.Combat);
            return guard;
        }

        [Test]
        public void ComingWithinReachGoesToCombat()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.MakeVictim(new Vector3(0f, 0f, 4f));

            _rig.StepTogether(120, Step, guard);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Combat));
            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Combat), "the replicated state says Combat too");
        }

        [Test]
        public void AMeleeGuardWithATurnStrikesAndHurtsThePlayer()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 4f));

            _rig.StepTogether(120, Step, guard);

            Assert.That(guard.AttackSignal.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(guard.AttackSignal.LastKind, Is.EqualTo(GuardAttackKind.Melee));
            Assert.That(victim.DamageTaken, Is.GreaterThanOrEqualTo(guard.Tuning.AttackDamage));
        }

        [Test]
        public void ThePlayerSteppingBackALittleDoesNotFlickerBackToChase()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard guard = GuardInCombatWith(victim);
            _rig.StepTogether(5, Step, guard);

            // Measured from where the guard stands, which is not where it started once it has taken a place on the ring.
            victim.transform.position = guard.transform.position + guard.transform.forward * 3.5f;
            _rig.StepTogether(10, Step, guard);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Combat), "3.5 m is past the 2 m reach but inside the margin");
        }

        [Test]
        public void ThePlayerWalkingWellOutOfReachButStillInViewReturnsTheGuardToChase()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard guard = GuardInCombatWith(victim);
            _rig.StepTogether(5, Step, guard);

            victim.transform.position = guard.transform.position + guard.transform.forward * 9f;
            _rig.StepTogether(3, Step, guard);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Chase));
        }

        [Test]
        public void LosingThePlayerLeavesCombatForInvestigate()
        {
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(0f, 0f, 1.5f));
            Guard guard = GuardInCombatWith(victim);
            _rig.StepTogether(5, Step, guard);

            victim.transform.position = new Vector3(0f, 0f, 200f);
            _rig.StepTogether(60, Step, guard);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate));
        }

        [Test]
        public void AGuardNeverPushesAPlayerPinnedAgainstAWall()
        {
            _rig.MakeWall(new Vector3(5.5f, 1f, 0f), new Vector3(1f, 3f, 6f));
            GuardCoreRig.Victim victim = _rig.MakeVictim(new Vector3(4.55f, 0f, 0f));
            CapsuleCollider body = victim.GetComponent<CapsuleCollider>();
            body.radius = 0.4f;
            body.height = 1.8f;
            body.center = new Vector3(0f, 0.9f, 0f);
            Physics.SyncTransforms();
            Vector3 playerBefore = victim.transform.position;
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            // The guard may walk round the player to a place on the ring, but never into them.
            float closest = float.MaxValue;
            for (int i = 0; i < 200; i++)
            {
                _rig.StepTogether(1, Step, guard);
                Vector3 apart = guard.transform.position - playerBefore;
                apart.y = 0f;
                closest = Mathf.Min(closest, apart.magnitude);
            }

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Combat));
            Assert.That(victim.transform.position, Is.EqualTo(playerBefore), "the player must not be displaced");
            Assert.That(closest, Is.GreaterThanOrEqualTo(guard.Tuning.BodyRadius + 0.4f - 0.001f), "the guard's body never overlapped the player's");
            Assert.That(closest, Is.LessThan(2f), "it did walk right up to the player");
            Assert.That(victim.DamageTaken, Is.GreaterThan(0f), "and it still lands its hits");
        }
    }
}
