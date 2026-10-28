using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>A guard whose route to the player or to a lead fails (#481): it walks to the nearest floor it can reach, and a melee guard that still cannot get at the player holds below.</summary>
    public class GuardNoRouteTests
    {
        private const float Step = 0.05f;
        private const float EdgeX = 3f;

        private GuardCoreRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new GuardCoreRig();
            _rig.SetUp();
            _rig.Director.Navigation.SetMap(new FloorEndingAtEdge());
        }

        [TearDown]
        public void TearDown()
        {
            DestroyAll("GuardStone(Clone)");
            _rig.TearDown();
        }

        private static void DestroyAll(string name)
        {
            GameObject found;
            while ((found = GameObject.Find(name)) != null)
                Object.DestroyImmediate(found);
        }

        [Test]
        public void AChasingMeleeGuardWalksToTheEdgeOfTheFloorAndThenHoldsBelow()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            Transform player = _rig.MakeIntruder(new Vector3(EdgeX + 4f, 0f, 0f));
            guard.States.Chase.Follow(player);
            guard.ChangeState(guard.States.Chase);

            _rig.StepTogether(120, Step, guard);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.HoldBelow));
            Assert.That(guard.transform.position.x, Is.EqualTo(EdgeX).Within(1f), "it walked to the nearest floor, not stood still");
        }

        [Test]
        public void AnInvestigatingGuardWalksToTheNearestFloorInsteadOfGivingUp()
        {
            Guard guard = _rig.MakeGuard(Vector3.zero);
            guard.Leads.Offer(new Vector3(EdgeX + 5f, 0f, 0f), GuardLeads.SightingStrength);
            guard.ChangeState(guard.States.Investigate);

            _rig.StepTogether(40, Step, guard);

            Assert.That(guard.CurrentState, Is.SameAs(guard.States.Investigate), "it is looking round, not back on patrol");
            Assert.That(guard.transform.position.x, Is.EqualTo(EdgeX).Within(1f));
        }

        /// <summary>A flat floor that stops at x = EdgeX: no route past it, and the nearest floor to anything beyond is the edge.</summary>
        private sealed class FloorEndingAtEdge : IGuardNavigationMap
        {
            public int Version => 0;

            public int FindCell(Vector3 position) => Mathf.RoundToInt(position.x * 2f) * 4096 + Mathf.RoundToInt(position.z * 2f) + 1000000;

            public bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> path, out BlockedReason failure)
            {
                failure = BlockedReason.Unreachable;
                path.Clear();
                if (to.x > EdgeX)
                    return false;
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / 0.5f));
                for (int i = 0; i <= steps; i++)
                    path.Add(Vector3.Lerp(from, to, i / (float)steps));
                return true;
            }

            public bool IsWalkClear(Vector3 from, Vector3 to, float halfWidth) => true;

            public bool TryFindNearestReachable(Vector3 from, Vector3 target, out Vector3 reachable)
            {
                reachable = new Vector3(Mathf.Min(target.x, EdgeX), 0f, target.z);
                return true;
            }

            public bool TryGetFloorHeight(Vector3 position, out float floorHeight)
            {
                floorHeight = 0f;
                return true;
            }
        }
    }
}
