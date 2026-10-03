using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>A melee guard that can see a player it cannot reach (#237): it holds below and throws stones instead of walking off, never through a teammate, goes back to melee when the player comes down, and calls ranged guards over.</summary>
    public class GuardUnreachableTests
    {
        private const float Step = 0.05f;

        // On the flat rig the floor is at height zero, so a player pivot 3 m up has its feet 2.1 m above the floor.
        private static readonly Vector3 Ledge = new Vector3(0f, 3f, 5f);

        private GuardCoreRig _rig;
        private GameObject _boltTemplate;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
            _boltTemplate = new GameObject("TestBolt");
            _boltTemplate.AddComponent<Rigidbody>().useGravity = false;
        }

        [TearDown]
        public void TearDown()
        {
            DestroyAll("GuardStone(Clone)");
            DestroyAll("TestBolt(Clone)");
            Object.DestroyImmediate(_boltTemplate);
            _rig.TearDown();
        }

        private static void DestroyAll(string name)
        {
            GameObject found;
            while ((found = GameObject.Find(name)) != null)
                Object.DestroyImmediate(found);
        }

        private static float HorizontalDistance(Component a, Component b)
        {
            Vector3 apart = a.transform.position - b.transform.position;
            apart.y = 0f;
            return apart.magnitude;
        }

        [Test]
        public void AMeleeGuardThatSeesAPlayerOnALedgeThrowsAndStaysBelowIt()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardCoreRig.Victim player = _rig.MakeVictim(Ledge);

            _rig.StepTogether(160, Step, guard);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.HoldBelow));
            Assert.That(guard.AttackSignal.Count, Is.GreaterThanOrEqualTo(2), "it keeps throwing on its cooldown");
            Assert.That(guard.AttackSignal.LastKind, Is.EqualTo(GuardAttackKind.Projectile));
            Assert.That(HorizontalDistance(guard, player), Is.LessThan(5f), "it did not walk away");
        }

        [Test]
        public void AMeleeGuardDoesNotThrowThroughATeammateAndSidestepsInstead()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Transform player = _rig.MakeIntruder(new Vector3(0f, 3f, 6f));
            MakeTallTeammateAt(new Vector3(0f, 0f, 3f));
            guard.Reach.IsUnreachable(player, 2f); // confirmed unreachable, as Chase has before it hands over
            guard.States.HoldBelow.Hold(player);
            guard.ChangeState(guard.States.HoldBelow);

            _rig.StepTogether(3, Step, guard);

            Assert.That(guard.AttackSignal.Count, Is.EqualTo(0), "no throw with the teammate in the line");
            Assert.That(guard.RangedAttack.LastShotBlocked, Is.True);

            _rig.StepTogether(100, Step, guard);

            Assert.That(guard.AttackSignal.Count, Is.GreaterThanOrEqualTo(1), "it throws once its new place has a clear line");
        }

        [Test]
        public void ThePlayerComingDownWithinReachEndsTheThrowingAndBringsBackTheMelee()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardCoreRig.Victim player = _rig.MakeVictim(Ledge);
            _rig.StepTogether(80, Step, guard);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.HoldBelow), "set-up: it was holding below");

            int throwsAfterwards = 0;
            guard.AttackSignal.Attacked += kind => { if (kind == GuardAttackKind.Projectile) throwsAfterwards++; };
            player.transform.position = new Vector3(0f, 0f, 3f);
            Physics.SyncTransforms();
            _rig.StepTogether(120, Step, guard);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Combat));
            Assert.That(throwsAfterwards, Is.EqualTo(0), "no more stones once the player is reachable");
            Assert.That(guard.AttackSignal.LastKind, Is.EqualTo(GuardAttackKind.Melee));
            Assert.That(player.DamageTaken, Is.GreaterThanOrEqualTo(guard.Tuning.AttackDamage));
        }

        [Test]
        public void TheCallForHelpSendsARangedGuardToEngageAndMarksTheSpotForAMeleeOne()
        {
            Guard caller = _rig.MakeGuard(Vector3.zero);
            Guard archer = _rig.MakeGuard(new Vector3(-10f, 0f, 0f));
            archer.Tuning.ProjectilePrefab = _boltTemplate;
            Guard brawler = _rig.MakeGuard(new Vector3(10f, 0f, 0f));
            Transform player = _rig.MakeIntruder(Ledge);

            caller.States.HoldBelow.Hold(player);
            caller.ChangeState(caller.States.HoldBelow);

            Assert.That(archer.CurrentState, Is.SameAs(archer.States.Chase), "the archer is sent after the player");
            Assert.That(archer.States.Chase.Target, Is.SameAs(player));
            Assert.That(brawler.CurrentState, Is.Not.SameAs(brawler.States.Chase), "the melee guard walks, it is not sent running");
            Assert.That(brawler.Leads.HasLead, Is.True, "but it has the player's spot to go and look at");
        }

        // A guard whose body fills the space between the thrower and a player on a ledge.
        private Guard MakeTallTeammateAt(Vector3 position)
        {
            Guard teammate = _rig.MakeGuard(position);
            CapsuleCollider body = teammate.gameObject.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 3f, 0f);
            body.height = 8f;
            body.radius = 0.5f;
            Physics.SyncTransforms();
            return teammate;
        }
    }
}
