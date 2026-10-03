using Interfaces;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The fresh guard core's body (#206): moving only through director navigation, health, the replicated state, the shove and the attack signal.</summary>
    public class GuardCoreBodyTests
    {
        private const float Step = 0.02f;

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
        public void AMoveRequestReachesArrivedThroughTheNavigationService()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            int pathsReady = 0;
            int arrivals = 0;
            guard.Navigator.RouteReady += answer => pathsReady++;
            guard.Navigator.Reached += answer => arrivals++;
            var destination = new Vector3(5f, 0f, 2f);

            guard.Navigator.MoveTo(destination, 3f, MoveReason.Patrol);
            Assert.That(guard.Navigator.IsMoving, Is.True);
            for (int i = 0; i < 300 && arrivals == 0; i++)
                _rig.Director.Navigation.Tick(Step);

            Assert.That(pathsReady, Is.EqualTo(1));
            Assert.That(arrivals, Is.EqualTo(1));
            Assert.That(Vector3.Distance(guard.transform.position, destination), Is.LessThan(0.4f));
            Assert.That(guard.Navigator.IsMoving, Is.False);
        }

        [Test]
        public void AnotherGuardsAnswerIsNotMine()
        {
            Guard walker = _rig.MakeGuard(Vector3.zero);
            Guard bystander = _rig.MakeGuard(new Vector3(10f, 0f, 0f));
            int bystanderArrivals = 0;
            bystander.Navigator.Reached += answer => bystanderArrivals++;

            walker.Navigator.MoveTo(new Vector3(0f, 0f, 3f), 3f, MoveReason.Patrol);
            for (int i = 0; i < 300; i++)
                _rig.Director.Navigation.Tick(Step);

            Assert.That(bystanderArrivals, Is.EqualTo(0));
        }

        [Test]
        public void ThePatrolPlaceholderStandsUpAndWalksAwayFromItsPost()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Patrolling));

            _rig.RunNavigation(guard, 150, Step);

            Assert.That(guard.Navigator.IsMoving || guard.transform.position != Vector3.zero, Is.True);
            Assert.That(Vector3.Distance(guard.transform.position, Vector3.zero), Is.GreaterThan(0.5f));
        }

        [Test]
        public void DamageTakesHealthOffThroughIHealth()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            IHealth health = guard;

            health.TakeDamage(30f);

            Assert.That(health.CurrentHealth, Is.EqualTo(70f));
            Assert.That(health.MaxHealth, Is.EqualTo(100f));
            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Patrolling));
        }

        [Test]
        public void LobbyScalingRaisesHealthSpeedAndDamage()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            float patrolSpeed = guard.Tuning.PatrolSpeed;
            float damage = guard.Tuning.AttackDamage;

            guard.ScaleHealth(2f);
            guard.ScaleTuning(1.5f, 2f);

            Assert.That(guard.MaxHealth, Is.EqualTo(200f));
            Assert.That(guard.CurrentHealth, Is.EqualTo(200f));
            Assert.That(guard.Tuning.PatrolSpeed, Is.EqualTo(patrolSpeed * 1.5f).Within(0.001f));
            Assert.That(guard.Tuning.AttackDamage, Is.EqualTo(damage * 2f).Within(0.001f));
        }

        [Test]
        public void ASleepSpellMovesTheReplicatedStateAndWakingMovesItBack()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);

            guard.Status.Sleep(30f);
            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Incapacitated));

            guard.Status.WakeUp();
            guard.Tick(Step);
            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Patrolling));
        }

        [Test]
        public void LethalDamageTellsTheDirectorAndEntersTheDeadState()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Component diedGuard = null;
            _rig.Director.OnGuardDied += died => diedGuard = died.Guard;

            guard.TakeDamage(500f);

            Assert.That(guard.IsDead, Is.True);
            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Dead));
            Assert.That(diedGuard, Is.EqualTo(guard));
        }

        [Test]
        public void TheShoveCarriesTheGuardBackAndAWallStopsIt()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            ((IShovable)guard).Shove(new Vector3(1f, 0f, 0f));
            for (int i = 0; i < 20; i++)
                guard.Tick(Step);
            Assert.That(guard.transform.position.x, Is.EqualTo(1f).Within(0.15f));

            Guard blocked = _rig.MakeGuard(new Vector3(0f, 0f, 10f));
            _rig.MakeWall(new Vector3(0.9f, 1f, 10f), new Vector3(0.2f, 3f, 4f));
            Physics.SyncTransforms();
            ((IShovable)blocked).Shove(new Vector3(2f, 0f, 0f));
            for (int i = 0; i < 20; i++)
                blocked.Tick(Step);
            Assert.That(blocked.transform.position.x, Is.LessThan(0.5f));
        }

        [Test]
        public void TheAttackSignalCountsAttacksAndKeepsTheKind()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardAttackKind heard = GuardAttackKind.Melee;
            guard.AttackSignal.Attacked += kind => heard = kind;

            guard.AttackSignal.Signal(GuardAttackKind.Projectile);

            Assert.That(guard.AttackSignal.Count, Is.EqualTo(1));
            Assert.That(guard.AttackSignal.LastKind, Is.EqualTo(GuardAttackKind.Projectile));
            Assert.That(heard, Is.EqualTo(GuardAttackKind.Projectile));
        }
    }
}
