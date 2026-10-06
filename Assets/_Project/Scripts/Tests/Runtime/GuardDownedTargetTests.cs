using System.Collections.Generic;
using Interfaces;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>Guards ignore downed players and keep their target until it is lost or a player is much closer (#270).</summary>
    public class GuardDownedTargetTests
    {
        private const float Frame = 1f / 60f;
        private const float PivotAboveFeet = 1.19f;

        private sealed class FakePlayer : MonoBehaviour, IDownable
        {
            public bool IsDown { get; set; }
        }

        private GuardCoreRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown() => _rig.TearDown();

        private Guard GuardAtOrigin()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Tuning.GeometryLayers = ~0;
            return guard;
        }

        private FakePlayer Player(Vector3 position)
        {
            Transform body = _rig.MakeIntruder(position + Vector3.up * PivotAboveFeet);
            return body.gameObject.AddComponent<FakePlayer>();
        }

        private static void Look(Guard guard)
        {
            Physics.SyncTransforms();
            guard.Sight.LookNext();
            guard.Tick(Frame);
        }

        [Test]
        public void AGuardSeesTheStandingPlayerNotACloserDownedOne()
        {
            Guard guard = GuardAtOrigin();
            FakePlayer downed = Player(new Vector3(0f, 0f, 3f));
            downed.IsDown = true;
            FakePlayer standing = Player(new Vector3(0.5f, 0f, 9f));

            Look(guard);

            Assert.That(guard.Sight.Visible, Is.SameAs(standing.transform));
        }

        [Test]
        public void AGuardNeverSeesALoneDownedPlayer()
        {
            Guard guard = GuardAtOrigin();
            Player(new Vector3(0f, 0f, 3f)).IsDown = true;

            Look(guard);

            Assert.That(guard.Sight.Visible, Is.Null);
        }

        [Test]
        public void AChasingGuardDropsATargetThatGoesDown()
        {
            Guard guard = GuardAtOrigin();
            FakePlayer player = Player(new Vector3(0f, 0f, 10f));
            // A first sighting cries for help and investigates (#259), so the chase is entered directly.
            Look(guard);
            guard.States.Chase.Follow(player.transform, false);
            guard.ChangeState(guard.States.Chase);
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Chase), "setup: chasing");

            player.IsDown = true;
            for (int i = 0; i < 30; i++)
            {
                Look(guard);
                guard.Tick(Frame * 20f);
            }

            Assert.That(guard.Sight.Visible, Is.Null);
            Assert.That(guard.CurrentState, Is.Not.SameAs(guard.States.Chase).And.Not.SameAs(guard.States.Combat));
        }

        [Test]
        public void TheHueAndCryRaisesNoRequestAtADownedPlayer()
        {
            var registry = new EnemyRegistry();
            Transform downed = Player(new Vector3(0f, 0f, 3f)).transform;
            Transform standing = Player(new Vector3(0f, 0f, 9f)).transform;
            downed.GetComponent<FakePlayer>().IsDown = true;
            registry.AddIntruder(downed);
            registry.AddIntruder(standing);
            var requests = new List<InvestigateRequest>();

            new HueAndCry(registry, requests.Add, 5f).Raise();

            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests[0].Position, Is.EqualTo(standing.position));
        }

        [Test]
        public void AGuardKeepsItsTargetWhenAnotherPlayerIsOnlySlightlyCloser()
        {
            Guard guard = GuardAtOrigin();
            FakePlayer first = Player(new Vector3(-1f, 0f, 10f));
            Look(guard);
            Assert.That(guard.Sight.Visible, Is.SameAs(first.transform), "setup");

            Player(new Vector3(1f, 0f, 8f)); // 80% of the distance: closer, but not under two-thirds
            Look(guard);

            Assert.That(guard.Sight.Visible, Is.SameAs(first.transform));
        }

        [Test]
        public void AGuardSwitchesWhenAnotherPlayerIsUnderTwoThirdsAsFar()
        {
            Guard guard = GuardAtOrigin();
            FakePlayer first = Player(new Vector3(-1f, 0f, 10f));
            Look(guard);

            FakePlayer closer = Player(new Vector3(1f, 0f, 5f));
            Look(guard);

            Assert.That(guard.Sight.Visible, Is.SameAs(closer.transform));
        }
    }
}
