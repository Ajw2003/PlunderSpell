using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The route smoother's cost (#243): each straight-line check samples its whole length, so the total
    /// length checked must grow with the route, not with its square.
    /// </summary>
    public class GuardPathSmootherTests
    {
        private sealed class MeasuringMap : IGuardNavigationMap
        {
            private readonly FlatNavigationMap _floor = new FlatNavigationMap();
            public float LengthChecked;

            public int Version => 0;
            public int FindCell(Vector3 position) => _floor.FindCell(position);
            public bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> path, out BlockedReason failure) =>
                _floor.TryFindPath(from, to, path, out failure);
            public bool TryGetFloorHeight(Vector3 position, out float floorHeight) =>
                _floor.TryGetFloorHeight(position, out floorHeight);

            public bool IsWalkClear(Vector3 from, Vector3 to, float halfWidth)
            {
                LengthChecked += Vector3.Distance(from, to);
                return true;
            }
        }

        [Test]
        public void LongStraightRoute_ChecksLengthInProportionToTheRoute()
        {
            var map = new MeasuringMap();
            var chain = new List<Vector3>();
            map.TryFindPath(Vector3.zero, new Vector3(60f, 0f, 0f), chain, out _);
            var smoother = new GuardPathSmoother();

            smoother.Smooth(chain, map, 0.35f);

            Assert.That(map.LengthChecked, Is.LessThan(10f * 60f),
                "smoothing a 60 m straight checked " + map.LengthChecked + " m of line");
            Assert.That(smoother.Kept[0], Is.EqualTo(chain[0]));
            Assert.That(smoother.Kept[smoother.Kept.Count - 1], Is.EqualTo(chain[chain.Count - 1]));
        }
    }
}
