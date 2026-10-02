using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>Friendly fire (#210): a ranged guard never fires with a teammate in the line, in Combat and in Chase, and the one shared check (<see cref="GuardLineOfFire"/>, reached through <see cref="GuardRangedAttack.TryFire"/>) lets the shot go once the line is clear.</summary>
    public class GuardFriendlyFireTests
    {
        private const float Step = 0.05f;

        private GuardCoreRig _rig;
        private GameObject _shotTemplate;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
            _shotTemplate = new GameObject("TestShot");
            _shotTemplate.AddComponent<Rigidbody>().useGravity = false;
        }

        [TearDown]
        public void TearDown()
        {
            GameObject shot;
            while ((shot = GameObject.Find("TestShot(Clone)")) != null)
                Object.DestroyImmediate(shot);
            Object.DestroyImmediate(_shotTemplate);
            _rig.TearDown();
        }

        private Guard MakeArcherAt(Vector3 position)
        {
            Guard archer = _rig.MakeGuard(position);
            archer.Tuning.ProjectilePrefab = _shotTemplate;
            return archer;
        }

        // A teammate stands in the line of fire: a guard with a body that fills the space at shooting height.
        private Guard MakeTeammateAt(Vector3 position)
        {
            Guard teammate = _rig.MakeGuard(position);
            CapsuleCollider body = teammate.gameObject.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 1f, 0f);
            body.height = 2f;
            body.radius = 0.4f;
            Physics.SyncTransforms();
            return teammate;
        }

        [Test]
        public void TheCheckBlocksAShotThroughATeammateAndLetsItGoOnceTheLineIsClear()
        {
            Guard archer = MakeArcherAt(Vector3.zero);
            Transform player = _rig.MakeIntruder(new Vector3(0f, 0f, 6f));
            Guard teammate = MakeTeammateAt(new Vector3(0f, 0f, 3f));

            bool firedWithTeammate = archer.RangedAttack.TryFire(player, 0f);
            bool blocked = archer.RangedAttack.LastShotBlocked;
            Object.DestroyImmediate(teammate.gameObject);
            Physics.SyncTransforms();
            bool firedWhenClear = archer.RangedAttack.TryFire(player, 0f);

            Assert.That(firedWithTeammate, Is.False);
            Assert.That(blocked, Is.True);
            Assert.That(archer.AttackSignal.Count, Is.EqualTo(1), "only the clear shot was signalled");
            Assert.That(firedWhenClear, Is.True, "a blocked shot does not spend the cooldown");
        }

        [Test]
        public void AWallInFrontOfATeammateIsNotFriendlyFire()
        {
            Guard archer = MakeArcherAt(Vector3.zero);
            Transform player = _rig.MakeIntruder(new Vector3(0f, 0f, 8f));
            _rig.MakeWall(new Vector3(0f, 1f, 2f), new Vector3(4f, 3f, 0.2f));
            MakeTeammateAt(new Vector3(0f, 0f, 4f));

            archer.RangedAttack.TryFire(player, 0f);

            Assert.That(archer.RangedAttack.LastShotBlocked, Is.False, "the wall is first; the shot dies there, not on a guard");
        }

        [Test]
        public void ARangedGuardInCombatDoesNotFireThroughATeammateAndSidestepsInstead()
        {
            Guard archer = MakeArcherAt(Vector3.zero);
            Transform player = _rig.MakeIntruder(new Vector3(0f, 0f, 6f));
            MakeTeammateAt(new Vector3(0f, 0f, 3f));
            archer.States.Combat.Engage(player);
            archer.ChangeState(archer.States.Combat);

            _rig.StepTogether(3, Step, archer);

            Assert.That(archer.AttackSignal.Count, Is.EqualTo(0), "no shot while the teammate is in the line");
            Assert.That(archer.RangedAttack.LastShotBlocked, Is.True);
            Assert.That(archer.Navigator.IsMoving, Is.True, "it is moving to a new place on the ring");
        }

        [Test]
        public void ARangedGuardInCombatShootsOnceItsSidestepClearsTheLine()
        {
            Guard archer = MakeArcherAt(Vector3.zero);
            Transform player = _rig.MakeIntruder(new Vector3(0f, 0f, 6f));
            MakeTeammateAt(new Vector3(0f, 0f, 3f));
            archer.States.Combat.Engage(player);
            archer.ChangeState(archer.States.Combat);

            _rig.StepTogether(60, Step, archer);

            Assert.That(archer.AttackSignal.Count, Is.GreaterThanOrEqualTo(1), "from its new place the line is clear");
            Assert.That(archer.AttackSignal.LastKind, Is.EqualTo(GuardAttackKind.Projectile));
        }

        [Test]
        public void ARangedGuardChasingDoesNotFireThroughATeammate()
        {
            Guard archer = MakeArcherAt(Vector3.zero);
            _rig.MakeIntruder(new Vector3(0f, 0f, 12f));
            MakeTeammateAt(new Vector3(0f, 0f, 1.5f));

            _rig.StepTogether(30, Step, archer);

            Assert.That(archer.CurrentState, Is.SameAs(archer.States.Chase), "setup: it is chasing, out of engage range");
            Assert.That(archer.AttackSignal.Count, Is.EqualTo(0), "the teammate is in the line the whole time");
            Assert.That(archer.RangedAttack.LastShotBlocked, Is.True);
        }
    }
}
