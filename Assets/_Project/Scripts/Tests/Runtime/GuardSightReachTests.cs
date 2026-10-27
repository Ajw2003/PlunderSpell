using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// How well a guard sees a player on a ledge, behind a lintel, far away or high above it (#238 part 2),
    /// and how the alarm stretches that (#229). The intruder's pivot is its centre, as the raid player's is:
    /// the guard aims 1 m above it (the head), at the middle and 0.9 m below it (the feet).
    /// </summary>
    public class GuardSightReachTests
    {
        private const float Frame = 1f / 60f;
        private const float PivotAboveFeet = 1.19f;

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

        private bool Sees(Guard guard)
        {
            Physics.SyncTransforms();
            guard.Sight.LookNext();
            guard.Tick(Frame);
            return guard.Sight.Visible != null;
        }

        [Test]
        public void APlayerOnALedgeWhoseHeadIsClearButWhoseBodyIsBehindTheEdgeIsSeen()
        {
            Guard guard = GuardAtOrigin();
            // A wall walk 2.5 m high, its edge 1 m from the guard; the player stands 1.5 m back from the edge.
            _rig.MakeWall(new Vector3(0f, 1.25f, 2.5f), new Vector3(4f, 2.5f, 3f));
            _rig.MakeIntruder(new Vector3(0f, 2.5f + PivotAboveFeet, 2.5f));
            Assert.That(Sees(guard), Is.True);
        }

        [Test]
        public void APlayerOnAWallWalkThreeMetresAwayIsSeenButOneOnItsFarSideOfTheEdgeAtOnePointFiveIsNot()
        {
            // The live co-op case (the harness's first two ledge cases fell inside the 20 s arrival grace):
            // a 4.5 m wall walk 2 m deep, the player in the middle, the guard on the floor at 3 m, then 1.5 m.
            Guard guard = GuardAtOrigin();
            _rig.MakeIntruder(new Vector3(0f, 4.5f + PivotAboveFeet, 3f));
            _rig.MakeWall(new Vector3(0f, 2.25f, 3f), new Vector3(3f, 4.5f, 2f));
            Assert.That(Sees(guard), Is.True, "3 m: the head clears the wall's edge");

            guard.transform.position = new Vector3(0f, 0f, 1.5f);
            Assert.That(Sees(guard), Is.False, "1.5 m: the wall itself hides the player, whatever the angle");
        }

        [Test]
        public void APlayerWhoseHeadIsHiddenByALintelButWhoseBodyIsInViewIsSeen()
        {
            Guard guard = GuardAtOrigin();
            // A beam across a doorway at head height, 3 m ahead of the guard, the player 6 m ahead.
            _rig.MakeWall(new Vector3(0f, 2.1f, 3f), new Vector3(4f, 0.6f, 0.4f));
            _rig.MakeIntruder(new Vector3(0f, PivotAboveFeet, 6f));
            Assert.That(Sees(guard), Is.True, "the chest and feet are in plain view under the beam");
        }

        [Test]
        public void APlayerFullyBehindAWallIsNotSeen()
        {
            Guard guard = GuardAtOrigin();
            _rig.MakeWall(new Vector3(0f, 3f, 3f), new Vector3(8f, 8f, 0.5f));
            _rig.MakeIntruder(new Vector3(0f, PivotAboveFeet, 6f));
            Assert.That(Sees(guard), Is.False);
        }

        [Test]
        public void AGuardSeesFartherThanFourteenMetresButNotWithoutLimit()
        {
            Guard guard = GuardAtOrigin();
            Transform intruder = _rig.MakeIntruder(new Vector3(0f, PivotAboveFeet, 18f));
            Assert.That(Sees(guard), Is.True, "18 m is in range");

            intruder.position = new Vector3(0f, PivotAboveFeet, 25f);
            Assert.That(Sees(guard), Is.False, "25 m is out of range while Calm");
        }

        [Test]
        public void TheAlarmStretchesHowFarAGuardSees()
        {
            Guard guard = GuardAtOrigin();
            _rig.MakeIntruder(new Vector3(0f, PivotAboveFeet, 25f));
            Assert.That(Sees(guard), Is.False, "out of range while Calm");

            _rig.Director.SetAlarmLevel(60f, 3); // Roused needs three witnesses (#259)
            Assert.That(Sees(guard), Is.True, "the same player is in range once the castle is Roused");
        }

        [Test]
        public void AGuardRightUnderAPlayerHighAboveItStillSeesThemButNotWhenTheyAreOverhead()
        {
            Guard guard = GuardAtOrigin();
            Transform intruder = _rig.MakeIntruder(new Vector3(0f, 6f, 1.2f));
            Assert.That(Sees(guard), Is.True, "about 75 degrees up is a head tilted back, not out of sight");

            intruder.position = new Vector3(0f, 6f, 0.3f);
            Assert.That(Sees(guard), Is.False, "nearly straight overhead is not in view");
        }
    }
}
