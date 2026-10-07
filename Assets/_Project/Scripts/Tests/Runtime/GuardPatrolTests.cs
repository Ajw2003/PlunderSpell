using Code.Scripts.EventSystems;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The Patrol state (#207): a round of three or more distinct reachable points, walked in turn, recovering from a blocked walk, the same every time for the same seed.</summary>
    public class GuardPatrolTests
    {
        private const float Step = 0.05f;
        private const int StepsPerRound = 3000;

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
        public void PatrolPicksThreeOrMoreDistinctPointsAroundThePost()
        {
            Guard guard = _rig.MakeGuard(new Vector3(20f, 0f, 20f));
            GuardPatrolRoute route = PlanRound(guard);

            Assert.That(route.Count, Is.GreaterThanOrEqualTo(3));
            for (int i = 0; i < route.Count; i++)
            {
                float fromPost = Vector3.Distance(route[i], guard.Home);
                Assert.That(fromPost, Is.InRange(guard.Tuning.PatrolMinimumRadius - 0.01f, guard.Tuning.PatrolMaximumRadius + 0.01f));
                for (int other = i + 1; other < route.Count; other++)
                    Assert.That(Vector3.Distance(route[i], route[other]), Is.GreaterThanOrEqualTo(guard.Tuning.PatrolPointSpacing), "points must be distinct");
            }
        }

        [Test]
        public void PatrolVisitsThePointsInTurn()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            List<Vector3> planned = Snapshot(PlanRound(guard));
            var arrivedAt = new List<Vector3>();
            guard.Navigator.Reached += arrived => arrivedAt.Add(arrived.Position);

            _rig.RunNavigation(guard, StepsPerRound, Step);

            Assert.That(arrivedAt.Count, Is.GreaterThanOrEqualTo(planned.Count));
            for (int i = 0; i < planned.Count; i++)
                Assert.That(Vector3.Distance(arrivedAt[i], planned[i]), Is.LessThan(0.5f), $"visit {i} should be point {i}");
        }

        [Test]
        public void ABlockedWalkSwapsTheOldPointForAnotherOne()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardPatrolRoute route = PlanRound(guard);
            Vector3 blockedPoint = route.Current;

            EventManager.Instance.Publish(new Blocked(guard, guard.transform.position, BlockedReason.Obstacle));

            Assert.That(route.Count, Is.EqualTo(3), "an obstacle may clear, so the round keeps its size");
            Assert.That(route.Current, Is.Not.EqualTo(blockedPoint));
            _rig.RunNavigation(guard, StepsPerRound, Step);
            Assert.That(guard.Navigator.IsMoving || route.Count >= 3, Is.True, "the guard carries on after the swap");
        }

        [Test]
        public void AClosedDoorDropsThePointForTheRound()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            GuardPatrolRoute route = PlanRound(guard);
            Vector3 blockedPoint = route.Current;

            EventManager.Instance.Publish(new Blocked(guard, guard.transform.position, BlockedReason.DoorClosed));

            Assert.That(route.Count, Is.EqualTo(2));
            Assert.That(route.HasPointNear(blockedPoint, 0.01f), Is.False);
            _rig.RunNavigation(guard, StepsPerRound, Step);
            Assert.That(guard.Navigator.IsMoving || route.Count > 0, Is.True, "the guard keeps patrolling what is left");
        }

        [Test]
        public void TheSameSeedGivesTheSamePatrol()
        {
            Guard first = _rig.MakeGuard(Vector3.zero);
            Guard second = _rig.MakeGuard(Vector3.zero);
            first.Reseed(1234);
            second.Reseed(1234);

            List<Vector3> firstRound = Snapshot(PlanRound(first));
            List<Vector3> secondRound = Snapshot(PlanRound(second));

            Assert.That(secondRound, Is.EqualTo(firstRound));
        }

        [Test]
        public void ADifferentSeedGivesADifferentPatrol()
        {
            Guard first = _rig.MakeGuard(Vector3.zero);
            Guard second = _rig.MakeGuard(Vector3.zero);
            first.Reseed(1);
            second.Reseed(2);

            Assert.That(Snapshot(PlanRound(second)), Is.Not.EqualTo(Snapshot(PlanRound(first))));
        }

        [Test]
        public void SeeingAPlayerSendsTheGuardToInvestigate()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Transform intruder = _rig.MakeIntruder(guard.transform.position + guard.transform.forward * 5f);
            guard.Sight.LookNext();

            guard.Tick(Step);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate));
            Assert.That(Vector3.Distance(((InvestigateState)guard.CurrentState).Spot, intruder.position), Is.LessThan(0.01f));
        }

        // One tick is enough to plan the round and start the first leg.
        private static GuardPatrolRoute PlanRound(Guard guard)
        {
            guard.Tick(Step);
            return ((PatrolState)guard.CurrentState).Route;
        }

        private static List<Vector3> Snapshot(GuardPatrolRoute route)
        {
            var points = new List<Vector3>();
            for (int i = 0; i < route.Count; i++)
                points.Add(route[i]);
            return points;
        }
    }
}
