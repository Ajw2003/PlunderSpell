using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// What a calm guard sees above it and hears around it (#238): the field of view is judged on the
    /// side-to-side angle with a separate, generous limit looking up, and a footstep carries all round a
    /// guard, muffled only by walls between, never by the guard's own body.
    /// </summary>
    public class GuardAwarenessTests
    {
        private const float Frame = 1f / 60f;
        private const float RunRadius = 8f;
        private const float FootstepStrength = 0.4f;
        private const int Default = 1;

        private GuardCoreRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown() => _rig.TearDown();

        private static void Look(Guard guard)
        {
            Physics.SyncTransforms();
            guard.Sight.LookNext();
            guard.Tick(Frame);
        }

        // A listening guard needs a collider, as the real guard prefab has, for the noise search to find it.
        private Guard MakeListeningGuard(Vector3 position)
        {
            Guard guard = _rig.MakeGuard(position);
            guard.gameObject.AddComponent<CapsuleCollider>();
            Physics.SyncTransforms();
            return guard;
        }

        // ---------------------------------------------------------------- Sight

        [Test]
        public void APlayerOnATableInFrontOfTheGuardIsSeen()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Transform player = _rig.MakeIntruder(new Vector3(0f, 3f, 1.5f));

            Look(guard);

            Assert.That(guard.Sight.Visible, Is.EqualTo(player), "3 m up and 1.5 m ahead is 58 degrees up, inside the 70 degree limit");
        }

        [Test]
        public void APlayerFarAboveTheGuardIsNotSeen()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.MakeIntruder(new Vector3(0f, 14f, 1.5f));

            Look(guard);

            Assert.That(guard.Sight.Visible, Is.Null, "13 m up and 1.5 m ahead is nearly straight up, past the 80 degree limit (#238 part 2)");
        }

        [Test]
        public void AGuardStillTurnsItsBackOnAPlayerSideways()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            _rig.MakeIntruder(new Vector3(0f, 3f, -1.5f));

            Look(guard);

            Assert.That(guard.Sight.Visible, Is.Null, "being high does not put a player behind the guard into view");
        }

        [Test]
        public void AChasedPlayerWhoJumpsHighIsNotLostForBeingInTheAir()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Tuning.MeleeReach = 0.5f;
            guard.Tuning.ChaseSpeed = 0f;
            Transform player = _rig.MakeIntruder(new Vector3(0f, 0f, 2.5f));
            guard.States.Chase.Follow(player, false);
            guard.ChangeState(guard.States.Chase);

            player.position = new Vector3(0f, 4.5f, 2.5f);
            for (int i = 0; i < 90; i++)
            {
                Physics.SyncTransforms();
                guard.Sight.LookNext();
                _rig.RunNavigation(guard, 1, 0.025f);
            }

            // Hanging 4.5 m up is out of reach on the flat floor, so after the confirm time (#237) a melee guard
            // holds below and throws; what matters here is that it did not lose the player.
            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Chase).Or.SameAs(guard.States.HoldBelow),
                "2.25 s in the air, longer than the 0.75 s grace");
        }

        // ---------------------------------------------------------------- Hearing

        [Test]
        public void AFootstepBehindAGuardIsHeardWithinTheRunRadius()
        {
            Guard guard = MakeListeningGuard(Vector3.zero);

            int heard = NoiseBroadcaster.Broadcast(new Vector3(0f, 0f, -6f), RunRadius, FootstepStrength, NoiseType.Footstep);

            Assert.That(heard, Is.EqualTo(1));
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(1), "behind is as audible as in front");
        }

        [Test]
        public void AFootstepBeyondTheRunRadiusIsNotHeard()
        {
            Guard guard = MakeListeningGuard(Vector3.zero);

            NoiseBroadcaster.Broadcast(new Vector3(0f, 0f, -10f), RunRadius, FootstepStrength, NoiseType.Footstep);

            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(0));
        }

        [Test]
        public void TheGuardsOwnBodyDoesNotMuffleANoiseInTheOpen()
        {
            Guard guard = MakeListeningGuard(Vector3.zero);
            float heardStrength = 0f;
            guard.Hearing.NoiseNoticed += (origin, strength) => heardStrength = strength;

            NoiseBroadcaster.Broadcast(new Vector3(0f, 0f, -5f), RunRadius, FootstepStrength, NoiseType.Footstep, ~0, Default);

            Assert.That(heardStrength, Is.EqualTo(FootstepStrength).Within(0.001f), "no wall between: full strength");
        }

        [Test]
        public void OneWallMufflesAFootstepButTwoHideIt()
        {
            Guard guard = MakeListeningGuard(Vector3.zero);
            _rig.MakeWall(new Vector3(0f, 1f, -2f), new Vector3(6f, 3f, 0.3f));
            Physics.SyncTransforms();

            NoiseBroadcaster.Broadcast(new Vector3(0f, 0f, -5f), RunRadius, FootstepStrength, NoiseType.Footstep, ~0, Default);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(1), "one wall halves 0.4 to 0.2, still above the 0.12 a calm guard notices");

            _rig.MakeWall(new Vector3(0f, 1f, -3.5f), new Vector3(6f, 3f, 0.3f));
            Physics.SyncTransforms();
            NoiseBroadcaster.Broadcast(new Vector3(0f, 0f, -5f), RunRadius, FootstepStrength, NoiseType.Footstep, ~0, Default);
            Assert.That(guard.Hearing.NoticedCount, Is.EqualTo(1), "two walls leave 0.1, under the threshold: not noticed again");
        }
    }
}
