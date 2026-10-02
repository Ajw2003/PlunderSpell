using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The navigation service on the real castle graph (#222): a route across rooms, up the keep's
    /// stairs, and round or into a closed door. The sweep is switched off here (mask zero) because the
    /// generated castle's own colliders are not what is being tested; the sweep has its own tests.
    /// </summary>
    public class GuardNavigationCastleTests
    {
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";
        private const float Step = 0.05f;
        private const int MaxTicks = 20000;

        private readonly List<PathReady> _ready = new List<PathReady>();
        private readonly List<Arrived> _arrived = new List<Arrived>();
        private readonly List<Blocked> _blocked = new List<Blocked>();
        private GameObject _generatorGo;
        private ProceduralCastleGenerator _generator;
        private EnemyDirector _director;
        private GameObject _guard;

        [SetUp]
        public void SetUp()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            _generatorGo = new GameObject("GuardNavTestGenerator");
            _generator = _generatorGo.AddComponent<ProceduralCastleGenerator>();
            _generator.Registry = registry;
            _director = new GameObject("Director").AddComponent<EnemyDirector>();
            _director.Navigation.Tuning.SweepMask = 0;
            _director.OnPathReady += _ready.Add;
            _director.OnArrived += _arrived.Add;
            _director.OnBlocked += _blocked.Add;
            _guard = new GameObject("Guard");
            _director.Navigation.Register(_guard.transform);
        }

        [TearDown]
        public void TearDown()
        {
            _generator.ClearGenerated();
            Object.DestroyImmediate(_generatorGo);
            Object.DestroyImmediate(_director.gameObject);
            Object.DestroyImmediate(_guard);
            _ready.Clear();
            _arrived.Clear();
            _blocked.Clear();
        }

        [Test]
        public void GuardWalksAcrossRoomsAndPathsAreSmoothedAndCached()
        {
            CastleNavGraph graph = UseCastle(12345, out ProceduralCastleData data);
            Vector3 start = StandAt(graph, data.PlacedModules[data.CryptStartIndex].Position);
            Vector3 goal = FarthestReachable(graph, data, start);
            _guard.transform.position = start;

            _director.Publish(new MoveRequest(_guard.transform, goal, 6f, MoveReason.Investigate));
            Assert.That(_ready.Count, Is.EqualTo(1), "a route should exist across the castle");
            var rawCells = new List<Vector3>();
            graph.FindPath(start, goal, rawCells);
            Assert.That(_ready[0].WaypointCount, Is.LessThan(rawCells.Count / 2), "smoothing should drop most of the cell chain");

            WalkUntilDone();
            Assert.That(_arrived.Count, Is.EqualTo(1));
            Assert.That(Vector3.Distance(Flat(_guard.transform.position), Flat(goal)), Is.LessThan(0.6f));

            _guard.transform.position = start;
            _director.Publish(new MoveRequest(_guard.transform, goal, 6f, MoveReason.Investigate));
            Assert.That(_director.Navigation.Planner.CacheHits, Is.EqualTo(1), "the same start and goal cells should reuse the route");
        }

        [Test]
        public void GuardClimbsTheKeepStairwellToItsGallery()
        {
            CastleNavGraph graph = null;
            ProceduralCastleData data = null;
            int module = -1;
            for (int seed = 1; seed < 60 && module < 0; seed++)
            {
                graph = UseCastle(seed, out data);
                module = IndexOfRoom(data, "KeepStairwell");
            }
            Assert.That(module, Is.GreaterThanOrEqualTo(0), "no seed in 1..59 placed a KeepStairwell");

            LowestAndHighestCell(graph, module, out Vector3 low, out Vector3 high);
            Assert.That(high.y - low.y, Is.GreaterThan(1f), "the gallery should sit well above the floor");
            _guard.transform.position = low;
            _director.Publish(new MoveRequest(_guard.transform, high, 4f, MoveReason.Patrol));
            Assert.That(_ready.Count, Is.EqualTo(1));

            float highestReached = low.y;
            for (int tick = 0; tick < MaxTicks && _arrived.Count == 0 && _blocked.Count == 0; tick++)
            {
                _director.Navigation.Tick(Step);
                highestReached = Mathf.Max(highestReached, _guard.transform.position.y);
            }
            Assert.That(_arrived.Count, Is.EqualTo(1), "blocked: " + (_blocked.Count > 0 ? _blocked[0].Reason.ToString() : "no"));
            Assert.That(_guard.transform.position.y, Is.EqualTo(high.y).Within(0.15f), "the guard should settle onto the gallery floor");
        }

        [Test]
        public void ClosedDoorsAreRoutedRoundOrReportedBlocked()
        {
            CastleNavGraph graph = UseCastle(12345, out ProceduralCastleData data);
            Vector3 start = StandAt(graph, data.PlacedModules[data.CryptStartIndex].Position);
            Vector3 goal = FarthestReachable(graph, data, start);
            _guard.transform.position = start;
            var route = new List<Vector3>();
            Assert.That(graph.FindPath(start, goal, route), Is.True);

            int link = graph.FindLinkNear(route[route.Count / 2], 6f);
            Assert.That(link, Is.GreaterThanOrEqualTo(0));
            int versionBefore = graph.DoorVersion;
            graph.SetDoorCost(link, CastleNavGraph.ClosedDoor);
            Assert.That(graph.DoorVersion, Is.Not.EqualTo(versionBefore), "closing a door must change the version so cached routes drop");

            // Either the graph finds another way round, which must avoid the shut door, or reports it blocked.
            bool stillReachable = graph.IsReachable(start, goal);
            Assert.That(graph.FindPath(start, goal, route), Is.EqualTo(stillReachable), "IsReachable and FindPath must agree");

            _director.Publish(new MoveRequest(_guard.transform, goal, 6f, MoveReason.Chase));
            if (stillReachable)
                Assert.That(_ready.Count, Is.EqualTo(1));
            else
                Assert.That(_blocked[0].Reason, Is.EqualTo(BlockedReason.DoorClosed));

            CloseEveryDoor(graph);
            _guard.transform.position = start;
            Assert.That(graph.IsReachable(start, goal), Is.False, "with every door shut the far room is cut off");
            _ready.Clear();
            _blocked.Clear();
            _director.Publish(new MoveRequest(_guard.transform, goal, 6f, MoveReason.Chase));
            Assert.That(_ready.Count, Is.EqualTo(0));
            Assert.That(_blocked.Count, Is.EqualTo(1));
            Assert.That(_blocked[0].Reason, Is.EqualTo(BlockedReason.DoorClosed));

            graph.ClearDoorCosts();
            Assert.That(graph.IsReachable(start, goal), Is.True, "reopening the doors restores the route");
        }

        private CastleNavGraph UseCastle(int seed, out ProceduralCastleData data)
        {
            data = _generator.Generate(seed);
            _director.Navigation.SetMap(new CastleGuardNavigationMap(data.NavGraph));
            return data.NavGraph;
        }

        private void WalkUntilDone()
        {
            for (int tick = 0; tick < MaxTicks && _arrived.Count == 0 && _blocked.Count == 0; tick++)
                _director.Navigation.Tick(Step);
        }

        private static void CloseEveryDoor(CastleNavGraph graph)
        {
            for (int link = 0; link < graph.LinkCount; link++)
                graph.SetDoorCost(link, CastleNavGraph.ClosedDoor);
        }

        private static Vector3 Flat(Vector3 point) => new Vector3(point.x, 0f, point.z);

        private static Vector3 StandAt(CastleNavGraph graph, Vector3 near)
        {
            int cell = graph.NearestWalkableCell(near + Vector3.up * 0.35f, 6f);
            Assert.That(cell, Is.GreaterThanOrEqualTo(0), "no floor near " + near);
            return graph.CellPosition(cell);
        }

        private static Vector3 FarthestReachable(CastleNavGraph graph, ProceduralCastleData data, Vector3 from)
        {
            Vector3 best = from;
            foreach (ProceduralCastleData.PlacedModule placed in data.PlacedModules)
            {
                int cell = graph.NearestWalkableCell(placed.Position + Vector3.up * 0.35f, 3f);
                if (cell < 0 || !graph.IsReachable(from, graph.CellPosition(cell)))
                    continue;
                if (Vector3.Distance(from, graph.CellPosition(cell)) > Vector3.Distance(from, best))
                    best = graph.CellPosition(cell);
            }
            Assert.That(Vector3.Distance(from, best), Is.GreaterThan(30f), "the far room should be several rooms away");
            return best;
        }

        private static int IndexOfRoom(ProceduralCastleData data, string roomId)
        {
            for (int i = 0; i < data.PlacedModules.Count; i++)
            {
                if (data.PlacedModules[i].RoomId == roomId)
                    return i;
            }
            return -1;
        }

        // A cell is walkable when it is its own nearest walkable cell.
        private static void LowestAndHighestCell(CastleNavGraph graph, int module, out Vector3 low, out Vector3 high)
        {
            low = new Vector3(0f, float.MaxValue, 0f);
            high = new Vector3(0f, float.MinValue, 0f);
            for (int local = 0; local < CastleNavGrid.CellsPerModule; local++)
            {
                int cell = module * CastleNavGrid.CellsPerModule + local;
                Vector3 position = graph.CellPosition(cell);
                if (graph.NearestWalkableCell(position, 0.1f) != cell)
                    continue;
                if (position.y < low.y)
                    low = position;
                if (position.y > high.y)
                    high = position;
            }
        }
    }
}
