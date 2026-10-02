using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Castle;
using Plunderspell.Guards;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// Patrol on the real castle graph (#207): once the doors are barred, a point behind a door is not
    /// reachable and the planner never offers it. The state's reaction to the resulting Blocked(DoorClosed)
    /// is in the PlayMode GuardPatrolTests.
    /// </summary>
    public class GuardPatrolBarredDoorTests
    {
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";
        private const int Picks = 200;

        private GameObject _generatorGo;
        private ProceduralCastleGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            _generatorGo = new GameObject("PatrolBarredDoorGenerator");
            _generator = _generatorGo.AddComponent<ProceduralCastleGenerator>();
            _generator.Registry = registry;
        }

        [TearDown]
        public void TearDown()
        {
            _generator.ClearGenerated();
            Object.DestroyImmediate(_generatorGo);
        }

        [Test]
        public void ABarredDoorMakesAPatrolPointUnreachableAndThePlannerNeverOffersIt()
        {
            ProceduralCastleData data = _generator.Generate(12345);
            CastleNavGraph graph = data.NavGraph;
            int startCell = graph.NearestWalkableCell(data.PlacedModules[data.CryptStartIndex].Position + Vector3.up * 0.35f, 6f);
            Vector3 post = graph.CellPosition(startCell);
            var map = new CastleGuardNavigationMap(graph);
            var random = new System.Random(99);
            var planner = new GuardPatrolPlanner(WideTuning(), () => random);

            List<Vector3> openPicks = PickMany(planner, map, post);
            CloseEveryDoor(graph);
            int cutOff = CountUnreachable(graph, post, openPicks);
            Assert.That(cutOff, Is.GreaterThan(0), "with a wide radius some points must sit behind a door, or this test proves nothing");

            List<Vector3> barredPicks = PickMany(planner, map, post);
            Assert.That(barredPicks.Count, Is.GreaterThanOrEqualTo(3), "a post still has points on its own side of the doors");
            Assert.That(CountUnreachable(graph, post, barredPicks), Is.EqualTo(0), "no barred-off point may be chosen");
        }

        private static GuardTuning WideTuning()
        {
            return new GuardTuning { PatrolMinimumRadius = 4f, PatrolMaximumRadius = 40f, PatrolPickAttempts = 32 };
        }

        private static List<Vector3> PickMany(GuardPatrolPlanner planner, CastleGuardNavigationMap map, Vector3 post)
        {
            var picks = new List<Vector3>();
            var route = new GuardPatrolRoute();
            for (int i = 0; i < Picks; i++)
            {
                if (planner.TryPickPoint(map, post, route, out Vector3 point))
                    picks.Add(point);
            }
            return picks;
        }

        private static int CountUnreachable(CastleNavGraph graph, Vector3 post, List<Vector3> points)
        {
            int count = 0;
            for (int i = 0; i < points.Count; i++)
                count += graph.IsReachable(post, points[i]) ? 0 : 1;
            return count;
        }

        private static void CloseEveryDoor(CastleNavGraph graph)
        {
            for (int link = 0; link < graph.LinkCount; link++)
                graph.SetDoorCost(link, CastleNavGraph.ClosedDoor);
        }
    }
}
