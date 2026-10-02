using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The Chase state (#209) on the flat rig: a seen player starts a chase, the guard follows a moving player, loses them to Investigate, reaches them for the Combat hand-off, shoots on the move if ranged, and never pushes a player pinned against a wall (#200).</summary>
    public class GuardChaseTests
    {
        private const float Step = 0.05f;
        private const int StepsToNotice = 6;
        private const int StepsToLoseSight = 60;
        private const float PlayerCapsuleRadius = 0.4f;

        private GuardCoreRig _rig;
        private GameObject _shotTemplate;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown()
        {
            GameObject shot;
            while ((shot = GameObject.Find("TestShot(Clone)")) != null)
                Object.DestroyImmediate(shot);
            if (_shotTemplate != null)
                Object.DestroyImmediate(_shotTemplate);
            _rig.TearDown();
        }

        // Looks every step instead of waiting for the 12 Hz throttle, so the test does not depend on timing.
        // Physics is told about moved players first: transforms are not synced into the physics world by
        // themselves, and the navigation sweep would otherwise hit the player's old position.
        private void StepGuard(Guard guard, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                Physics.SyncTransforms();
                guard.Sight.LookNext();
                _rig.RunNavigation(guard, 1, Step);
            }
        }

        private Guard GuardChasingAPlayerAt(Vector3 playerPosition, out Transform player)
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            player = _rig.MakeIntruder(playerPosition);
            StepGuard(guard, StepsToNotice);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Chase), "setup: the guard should be chasing");
            return guard;
        }

        [Test]
        public void SeeingAPlayerStartsAChaseAtChaseSpeed()
        {
            Guard guard = GuardChasingAPlayerAt(new Vector3(0f, 0f, 10f), out Transform player);

            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Chasing));
            Assert.That(guard.States.Chase.Target, Is.SameAs(player));
            Assert.That(guard.Navigator.IsMoving, Is.True);
        }

        [Test]
        public void ChaseFollowsAMovingPlayer()
        {
            Guard guard = GuardChasingAPlayerAt(new Vector3(0f, 0f, 8f), out Transform player);

            for (int i = 0; i < 100; i++)
            {
                player.position += new Vector3(0f, 0f, 2f * Step);
                StepGuard(guard, 1);
            }

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Chase));
            Assert.That(guard.transform.position.z, Is.GreaterThan(12f), "it ran after the player");
            Assert.That(Vector3.Distance(guard.transform.position, player.position), Is.LessThan(3f), "and caught up, being faster");
        }

        [Test]
        public void LosingThePlayerSendsTheGuardToInvestigateTheLastSeenSpot()
        {
            Guard guard = GuardChasingAPlayerAt(new Vector3(0f, 0f, 10f), out Transform player);
            Vector3 lastSeen = new Vector3(0f, 0f, 11f);
            player.position = lastSeen;
            StepGuard(guard, 2);

            player.position = new Vector3(0f, 0f, 200f);
            StepGuard(guard, StepsToLoseSight);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate));
            Assert.That(((InvestigateState)guard.CurrentState).Spot, Is.EqualTo(lastSeen));
        }

        [Test]
        public void ComingWithinReachFiresTheCombatHandOffOnce()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.MakeIntruder(new Vector3(0f, 0f, 4f));
            int reached = 0;
            guard.States.Chase.InReach += target => reached++;

            StepGuard(guard, 120);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Chase), "Combat is #210; until then the guard keeps chasing");
            Assert.That(reached, Is.EqualTo(1), "once per approach, not once per look");
        }

        [Test]
        public void ARangedGuardRaisesTheAttackSignalWhileItIsMoving()
        {
            _shotTemplate = new GameObject("TestShot");
            _shotTemplate.AddComponent<Rigidbody>().useGravity = false;
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Tuning.ProjectilePrefab = _shotTemplate;
            _rig.MakeIntruder(new Vector3(0f, 0f, 13f));
            int shotsWhileMoving = 0;
            guard.AttackSignal.Attacked += kind =>
            {
                if (kind == GuardAttackKind.Projectile && guard.Navigator.IsMoving && guard.transform.position.z > 0.5f)
                    shotsWhileMoving++;
            };

            StepGuard(guard, 50);

            Assert.That(guard.AttackSignal.Count, Is.GreaterThanOrEqualTo(2), "it keeps shooting on its cooldown");
            Assert.That(shotsWhileMoving, Is.GreaterThanOrEqualTo(1), "at least one shot was fired after it had started running");
            Assert.That(guard.AttackSignal.LastKind, Is.EqualTo(GuardAttackKind.Projectile));
        }

        [Test]
        public void AMeleeGuardDoesNotShoot()
        {
            Guard guard = GuardChasingAPlayerAt(new Vector3(0f, 0f, 10f), out Transform player);

            StepGuard(guard, 50);

            Assert.That(guard.AttackSignal.Count, Is.EqualTo(0));
        }

        [Test]
        public void AGuardChasingAPlayerPinnedAgainstAWallNeitherPushesNorCrushesThem()
        {
            _rig.MakeWall(new Vector3(5.5f, 1f, 0f), new Vector3(1f, 3f, 6f));
            Transform player = _rig.MakeIntruder(new Vector3(4.55f, 0f, 0f));
            // A player-shaped body with its pivot at the feet, as the real players have.
            CapsuleCollider body = player.GetComponent<CapsuleCollider>();
            body.radius = PlayerCapsuleRadius;
            body.height = 1.8f;
            body.center = new Vector3(0f, 0.9f, 0f);
            Physics.SyncTransforms();
            Vector3 playerBefore = player.position;
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            StepGuard(guard, 200);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Chase));
            Assert.That(player.position, Is.EqualTo(playerBefore), "the player must not be displaced");
            Assert.That(guard.transform.position.x + guard.Tuning.BodyRadius, Is.LessThanOrEqualTo(playerBefore.x - PlayerCapsuleRadius + 0.001f),
                "the guard's body must stop before the player");
            Assert.That(guard.transform.position.x, Is.GreaterThan(playerBefore.x - PlayerCapsuleRadius - 1.5f), "it walked right up to the player");
        }
    }
}
