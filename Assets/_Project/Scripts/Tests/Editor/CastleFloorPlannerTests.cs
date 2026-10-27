using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Plunderspell.Castle;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The stacked castle's floor plan (#247, docs/plans/multi-floor-castle.md section 1).</summary>
    public class CastleFloorPlannerTests
    {
        private static readonly Vector2Int GateApproach = new Vector2Int(2, 0);

        private static List<PlannedRoom> Plan(int seed) => CastleFloorPlanner.Plan(new System.Random(seed), 0.9f, GateApproach);

        [Test]
        public void EveryLevelHasItsRooms()
        {
            foreach (int seed in new[] { 1, 42, 777, 12345, -9 })
            {
                List<PlannedRoom> plan = Plan(seed);
                Assert.AreEqual(9 - 2, plan.Count(r => r.Level == CastleLevels.Keep), $"seed {seed}: keep rooms (the stair heads belong to the stairs)");
                Assert.AreEqual(9, plan.Count(r => r.Level == CastleLevels.Crypt), $"seed {seed}: crypt modules (8 rooms + the down-stair)");
                Assert.AreEqual(1, plan.Count(r => r.Kind == PlannedRoomKind.StairDown && r.Level == CastleLevels.Crypt && r.Cell == Vector2Int.zero), $"seed {seed}: one down-stair at the centre");
                Assert.AreEqual(8, plan.Count(r => r.Level == CastleLevels.Ground && r.Zone == CastleZone.InnerWard), $"seed {seed}: ward ring incl. two stairs");
                int bailey = plan.Count(r => r.Zone == CastleZone.OuterBailey);
                Assert.That(bailey, Is.InRange(13, 16), $"seed {seed}: bailey rooms after courtyards");
                Assert.AreEqual(1, plan.Count(r => r.Kind == PlannedRoomKind.FinalChamber), $"seed {seed}: one final chamber");
            }
        }

        [Test]
        public void StairsSitWhereTheDesignPutsThem()
        {
            List<PlannedRoom> plan = Plan(42);
            var ups = plan.Where(r => r.Kind == PlannedRoomKind.StairUp).ToList();
            CollectionAssert.AreEquivalent(new[] { new Vector2Int(-1, 0), new Vector2Int(1, 0) }, ups.Select(u => u.Cell));
            foreach (PlannedRoom up in ups)
            {
                Assert.AreEqual(CastleLevels.Ground, up.Level);
                Assert.AreEqual(-up.Cell, up.ExitFacing, "an up-stair's head opens toward the keep centre");
            }
            PlannedRoom down = plan.Single(r => r.Kind == PlannedRoomKind.StairDown);
            Assert.AreEqual(Vector2Int.zero, down.Cell);
            Assert.AreEqual(CastleLevels.Crypt, down.Level);
            Assert.AreEqual(Vector2Int.up, down.ExitFacing, "the foot opens north");
        }

        [Test]
        public void FinalChamberIsACryptRingCell_AndThePlanIsDeterministic()
        {
            PlannedRoom final = Plan(777).Single(r => r.Kind == PlannedRoomKind.FinalChamber);
            Assert.AreEqual(CastleLevels.Crypt, final.Level);
            Assert.AreEqual(1, Mathf.Max(Mathf.Abs(final.Cell.x), Mathf.Abs(final.Cell.y)));
            CollectionAssert.AreEqual(Plan(777).Select(r => (r.Cell, r.Level, r.Kind)), Plan(777).Select(r => (r.Cell, r.Level, r.Kind)));
        }

        [Test]
        public void CourtyardsAreOnlyBaileyCells_AndNeverTheGateApproach()
        {
            foreach (int seed in new[] { 1, 42, 777, 12345, -9 })
            {
                var ground = new HashSet<Vector2Int>(Plan(seed).Where(r => r.Level == CastleLevels.Ground).Select(r => r.Cell));
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0)
                            continue; // the down-stair's upper storey fills the centre
                        Assert.IsTrue(ground.Contains(new Vector2Int(x, y)), $"seed {seed}: inner cell {x},{y} carved");
                    }
                Assert.IsTrue(ground.Contains(GateApproach), $"seed {seed}: the gate approach was carved");
            }
        }
    }
}
