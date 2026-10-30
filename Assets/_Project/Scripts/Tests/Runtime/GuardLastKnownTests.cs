using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>A guard that lost a player goes back to patrol only after 8 s with nothing new and with the last-known spot 3 or more rooms away (#487).</summary>
    public class GuardLastKnownTests
    {
        private const float Step = 0.05f;
        private static readonly Vector3 LastSeen = new Vector3(0f, 0f, 2f);

        private GuardCoreRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
        }

        [TearDown]
        public void TearDown() => _rig.TearDown();

        // The guard has just lost sight of a player who was last seen at LastSeen heading along +z, and is looking there.
        private Guard MakeHuntingGuard()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            // Far behind and out of sight, so the guard walking on along +z never finds them and the hunt runs its course.
            Transform player = _rig.MakeIntruder(new Vector3(0f, 0f, -60f));
            guard.LastKnown.Sighted(player, Vector3.zero);
            guard.LastKnown.Sighted(player, LastSeen);
            guard.LastKnown.Hunt(player);
            guard.Leads.Offer(LastSeen, GuardLeads.SightingStrength);
            guard.ChangeState(guard.States.Investigate);
            return guard;
        }

        // Returns the seconds run until the guard left Investigate, or the whole span when it never did.
        private float RunUntilPatrol(Guard guard, float seconds)
        {
            int steps = Mathf.RoundToInt(seconds / Step);
            for (int i = 0; i < steps; i++)
            {
                _rig.RunNavigation(guard, 1, Step);
                if (guard.CurrentState == guard.States.Patrol)
                    return i * Step;
            }
            return seconds;
        }

        [Test]
        public void AGuardDoesNotGiveUpBeforeEightSecondsEvenWhenTheSpotIsFarAway()
        {
            _rig.SetRoomsApart(5);
            Guard guard = MakeHuntingGuard();

            float seconds = RunUntilPatrol(guard, GuardLastKnown.GiveUpSeconds - 0.5f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate), "still searching at " + seconds + " s");
        }

        [Test]
        public void AGuardDoesNotGiveUpWhenTheSpotIsFewerThanThreeRoomsAway()
        {
            _rig.SetRoomsApart(GuardLastKnown.GiveUpRooms - 1);
            Guard guard = MakeHuntingGuard();

            RunUntilPatrol(guard, 30f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate), "two rooms is still close, so it keeps hunting");
        }

        [Test]
        public void AGuardGivesUpOnceEightSecondsPassAndTheSpotIsThreeRoomsAway()
        {
            _rig.SetRoomsApart(GuardLastKnown.GiveUpRooms);
            Guard guard = MakeHuntingGuard();

            float seconds = RunUntilPatrol(guard, 30f);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Patrol), "both conditions hold, so back to patrol");
            Assert.That(seconds, Is.GreaterThanOrEqualTo(GuardLastKnown.GiveUpSeconds));
        }

        [Test]
        public void AGuardSearchesAlongTheHeadingAfterTheLastKnownSpot()
        {
            _rig.SetRoomsApart(GuardLastKnown.GiveUpRooms - 1);
            Guard guard = MakeHuntingGuard();

            RunUntilPatrol(guard, 8f);

            Assert.That(guard.transform.position.z, Is.GreaterThan(LastSeen.z + 1f), "it walked on past the spot the way the player was going");
        }
    }
}
