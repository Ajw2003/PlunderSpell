using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using NUnit.Framework;
using Plunderspell.Castle;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The castle nav graph (#221): the same graph from the same seed, reachability that agrees with
    /// the NavMesh the audit uses, the entrance strip and gatehouse joined, and queries that cost
    /// microseconds and allocate nothing. The NavMesh half bakes a real NavMesh in the Editor over
    /// the generated castle, as <c>CastleAudit</c> does in Play mode, and probes the same 25 floor
    /// points per room from the arrival point.
    /// </summary>
    public class CastleNavGraphTests
    {
        private const string RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";
        private const string ReportDir = "docs/generated/nav-graph-2026-10-02";
        private static readonly int[] s_seeds = { 12345, 777, 2024, 31337, 90210 };

        private GameObject _generatorGo;
        private ProceduralCastleGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(RegistryPath);
            Assert.That(registry, Is.Not.Null);
            _generatorGo = new GameObject("NavGraphTestGenerator");
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
        public void AStackedColumn_FindsTheFloorYouStandOn()
        {
            ProceduralCastleData data = _generator.Generate(42);
            CastleNavGraph graph = data.NavGraph;
            int keep = graph.NearestWalkableCell(new Vector3(0f, 4.7f, 0f));
            int crypt = graph.NearestWalkableCell(new Vector3(0f, 0f, 12f) + Vector3.up * -2.9f);
            Assert.That(graph.CellPosition(keep).y, Is.EqualTo(4.6f).Within(0.05f), "a point on the keep floor found another floor");
            Assert.That(graph.CellPosition(crypt).y, Is.EqualTo(-3.0f).Within(0.05f), "a point in the crypt found another floor");
        }

        [Test]
        public void TheStairsJoinTheFloors()
        {
            foreach (int seed in new[] { 1, 42, 777 })
            {
                ProceduralCastleData data = _generator.Generate(seed);
                CastleNavGraph graph = data.NavGraph;
                // A bailey room the seed kept (courtyards have no tile, so a fixed cell could be one).
                Vector3 bailey = System.Linq.Enumerable.First(data.PlacedModules, m => m.Zone == CastleZone.OuterBailey).Position + Vector3.up * 0.4f;
                Vector3 keep = new Vector3(0f, 4.7f, 12f);
                Vector3 crypt = data.PlacedModules[data.CryptStartIndex].Position + Vector3.up * 0.4f;
                Assert.IsTrue(graph.IsReachable(bailey, keep), $"seed {seed}: no walk from the bailey up to the keep");
                Assert.IsTrue(graph.IsReachable(bailey, crypt), $"seed {seed}: no walk from the bailey down to the final chamber");
            }
        }

        [Test]
        public void SameSeedGivesTheSameGraph()
        {
            ProceduralCastleData first = _generator.Generate(12345);
            int checksum = first.NavGraph.Checksum();
            int links = first.NavGraph.LinkCount;
            int cells = first.NavGraph.WalkableCellCount;
            Assert.That(links, Is.GreaterThan(40), "a castle has dozens of open archways");
            Assert.That(cells, Is.GreaterThan(10000));

            ProceduralCastleData second = _generator.Generate(12345);
            Assert.That(second.NavGraph, Is.Not.SameAs(first.NavGraph));
            Assert.That(second.NavGraph.Checksum(), Is.EqualTo(checksum));
            Assert.That(second.NavGraph.LinkCount, Is.EqualTo(links));
            Assert.That(second.NavGraph.WalkableCellCount, Is.EqualTo(cells));

            Assert.That(_generator.Generate(777).NavGraph.Checksum(), Is.Not.EqualTo(checksum),
                "a different seed should give a different graph");
        }

        [Test]
        public void EntranceStripAndGatehouseAreJoined()
        {
            foreach (int seed in s_seeds)
            {
                ProceduralCastleData data = _generator.Generate(seed);
                CastleNavGraph graph = data.NavGraph;
                Vector3 crypt = Stand(data.PlacedModules[data.CryptStartIndex].Position);
                int cryptCell = graph.NearestWalkableCell(crypt, 3f);
                Assert.That(cryptCell, Is.GreaterThanOrEqualTo(0), $"seed {seed}: no walkable cell at the crypt");

                // Every strip entrance: the cell in front of the way in connects to the crypt.
                Assert.That(data.EntranceCells.Count, Is.GreaterThan(0));
                foreach (Vector2Int entrance in data.EntranceCells)
                {
                    Vector3 front = Stand(CastleArrivalPlanner.AnchorFor(data, IndexOf(data, entrance)));
                    Assert.That(graph.IsReachable(front, crypt), Is.True,
                        $"seed {seed}: strip entrance {entrance} is not joined to the crypt");
                }

                // The gatehouse passage is part of the graph: it has cells and joins the strip,
                // even though the sealed gate keeps the outside out.
                ProceduralCastleData.PlacedModule gate = data.PlacedModules[data.ExtractionExitIndex];
                Vector3 inward = new Vector3(-Mathf.Sign(gate.GridPosition.x), 0f, 0f); // the gate sits on the +X axis
                Vector3 inside = Stand(gate.Position + inward * 3.5f);
                Assert.That(graph.IsReachable(inside, crypt), Is.True, $"seed {seed}: gatehouse not joined to the crypt");

                Vector3 deck = Stand(gate.Position - inward * 12f);
                Assert.That(graph.IsReachable(deck, crypt), Is.False,
                    $"seed {seed}: the drawbridge outside the sealed gate must stay cut off");
            }
        }

        [Test]
        public void ReachabilityAgreesWithTheNavMesh()
        {
            var report = new StringBuilder();
            report.AppendLine("# Nav graph vs NavMesh reachability (#221)");
            report.AppendLine();
            report.AppendLine("Generated by `CastleNavGraphTests.ReachabilityAgreesWithTheNavMesh`. For each seed the castle is generated, a NavMesh");
            report.AppendLine("is baked over it in the Editor (physics colliders, as RaidSceneBuilder sets up), and the same 25 floor probes per room");
            report.AppendLine("that `CastleAudit` uses are tried from the arrival point, on the NavMesh and on the nav graph.");
            report.AppendLine();
            report.AppendLine("| Seed | Rooms | Probes | Both reach | Neither reaches | NavMesh only | Graph only | Floor on NavMesh only | Floor on graph only |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|");
            var details = new StringBuilder();
            int totalOnlyNav = 0, totalOnlyGraph = 0, totalProbes = 0, roomDisagree = 0, bothFloorDisagree = 0;

            foreach (int seed in s_seeds)
            {
                ProceduralCastleData data = _generator.Generate(seed);
                Physics.SyncTransforms();
                Vector3 arrival = CastleSpawnResolver.ResolveArrival(data, seed, out _);

                var navGo = new GameObject("NavGraphTestNavMesh");
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.transform.position = new Vector3(0f, -0.5f, 0f);
                ground.transform.localScale = new Vector3(200f, 1f, 200f);
                var surface = navGo.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                try
                {
                    Physics.SyncTransforms();
                    surface.BuildNavMesh();
                    bool spawnOnMesh = NavMesh.SamplePosition(arrival, out NavMeshHit spawnHit, 3f, NavMesh.AllAreas);
                    int spawnCell = data.NavGraph.NearestWalkableCell(arrival, 3f);
                    Assert.That(spawnOnMesh, Is.True, $"seed {seed}: arrival point is off the NavMesh");
                    Assert.That(spawnCell, Is.GreaterThanOrEqualTo(0), $"seed {seed}: arrival point is off the nav graph");

                    int rooms = 0, probes = 0, both = 0, neither = 0, onlyNav = 0, onlyGraph = 0, floorNav = 0, floorGraph = 0;
                    var path = new NavMeshPath();
                    foreach (ProceduralCastleData.PlacedModule m in data.PlacedModules)
                    {
                        // The probes sample the ground floor height; keep, crypt and stair modules stand at other
                        // heights, and the stairs have their own floor tests (TheStairsJoinTheFloors).
                        if (!ProceduralCastleGenerator.IsEnclosedRoom(m.Zone) || m.Level != CastleLevels.Ground || m.Storeys > 1)
                            continue;
                        rooms++;
                        int roomNav = 0, roomGraph = 0;
                        for (int gx = -2; gx <= 2; gx++)
                        {
                            for (int gz = -2; gz <= 2; gz++)
                            {
                                Vector3 probe = m.Position + new Vector3(gx * 2.2f, 0.35f, gz * 2.2f);
                                bool navFloor = NavMesh.SamplePosition(probe, out NavMeshHit h, 0.5f, NavMesh.AllAreas) && h.position.y < 0.55f;
                                bool navReach = navFloor && NavMesh.CalculatePath(spawnHit.position, h.position, NavMesh.AllAreas, path)
                                                && path.status == NavMeshPathStatus.PathComplete;
                                int cell = data.NavGraph.NearestWalkableCell(probe, 0.5f);
                                bool graphFloor = cell >= 0 && data.NavGraph.CellPosition(cell).y < 0.55f;
                                bool graphReach = graphFloor && data.NavGraph.IsReachable(spawnCell, cell);
                                probes++;
                                if (navReach) roomNav++;
                                if (graphReach) roomGraph++;
                                if (navFloor && !graphFloor) floorNav++;
                                if (graphFloor && !navFloor) floorGraph++;
                                if (navFloor && graphFloor && navReach != graphReach)
                                    bothFloorDisagree++;
                                if (navReach && graphReach)
                                {
                                    both++;
                                }
                                else if (!navReach && !graphReach)
                                {
                                    neither++;
                                }
                                else if (navReach)
                                {
                                    onlyNav++;
                                    details.AppendLine($"- seed {seed}: {m.RoomId} {m.GridPosition} probe ({gx},{gz}) NavMesh reaches it, graph does not (graph floor {graphFloor}, navmesh floor {navFloor}).");
                                }
                                else
                                {
                                    onlyGraph++;
                                    details.AppendLine($"- seed {seed}: {m.RoomId} {m.GridPosition} probe ({gx},{gz}) graph reaches it, NavMesh does not (navmesh floor {navFloor}).");
                                }
                            }
                        }
                        if ((roomNav == 0) != (roomGraph == 0))
                        {
                            roomDisagree++;
                            details.AppendLine($"- seed {seed}: ROOM {m.RoomId} {m.GridPosition}: NavMesh reaches {roomNav}/25, graph {roomGraph}/25.");
                        }
                    }
                    report.AppendLine($"| {seed} | {rooms} | {probes} | {both} | {neither} | {onlyNav} | {onlyGraph} | {floorNav} | {floorGraph} |");
                    totalOnlyNav += onlyNav;
                    totalOnlyGraph += onlyGraph;
                    totalProbes += probes;
                }
                finally
                {
                    Object.DestroyImmediate(navGo);
                    Object.DestroyImmediate(ground);
                    NavMesh.RemoveAllNavMeshData();
                }
            }

            report.AppendLine();
            report.AppendLine($"Rooms where one side reaches nothing and the other something: {roomDisagree}.");
            report.AppendLine($"Probes with floor on both sides where reachability differs: {bothFloorDisagree}.");
            report.AppendLine();
            report.AppendLine("## Differences");
            report.AppendLine();
            report.Append(details);
            Directory.CreateDirectory(ReportDir);
            File.WriteAllText(Path.Combine(ReportDir, "reachability.md"), report.ToString());
            Debug.Log($"[NavGraphTests] {totalProbes} probes, NavMesh-only {totalOnlyNav}, graph-only {totalOnlyGraph}, room disagreements {roomDisagree}, both-floor reachability disagreements {bothFloorDisagree}.");

            Assert.That(roomDisagree, Is.EqualTo(0), "a room is reachable on one side only; see " + ReportDir + "/reachability.md");
            // Reachability itself must agree wherever both sides have floor; what differs is where each
            // thinks there is floor at all (furniture gaps), which the report lists.
            Assert.That(bothFloorDisagree, Is.EqualTo(0), "reachability differs on a probe both sides call floor; see " + ReportDir + "/reachability.md");
            Assert.That(totalOnlyNav + totalOnlyGraph, Is.LessThan(totalProbes / 10), "more than 10% of probes differ on whether there is floor; see " + ReportDir + "/reachability.md");
        }

        [Test]
        public void QueriesAreFastAndAllocateNothing()
        {
            ProceduralCastleData data = _generator.Generate(12345);
            CastleNavGraph graph = data.NavGraph;

            // Pairs spread over every room, so paths cross many archways.
            var points = new List<Vector3>();
            foreach (ProceduralCastleData.PlacedModule m in data.PlacedModules)
            {
                if (!ProceduralCastleGenerator.IsEnclosedRoom(m.Zone))
                    continue;
                points.Add(m.Position + new Vector3(-3f, 0.35f, 2f));
                points.Add(m.Position + new Vector3(2.5f, 0.35f, -3f));
            }
            var path = new List<Vector3>(4096);

            // Warm-up grows the shared buffers once.
            for (int i = 0; i < points.Count; i++)
                graph.FindPath(points[i], points[(i * 7 + 3) % points.Count], path);

            const int Runs = 2000;
            long before = GC.GetAllocatedBytesForCurrentThread();
            int found = 0, pathPoints = 0;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < Runs; i++)
            {
                if (graph.FindPath(points[i % points.Count], points[(i * 7 + 3) % points.Count], path))
                {
                    found++;
                    pathPoints += path.Count;
                }
            }
            sw.Stop();
            double pathUs = sw.Elapsed.TotalMilliseconds * 1000.0 / Runs;

            sw.Restart();
            int nearest = 0;
            for (int i = 0; i < Runs; i++)
                nearest += graph.NearestWalkableCell(points[i % points.Count], 3f) >= 0 ? 1 : 0;
            sw.Stop();
            double nearestUs = sw.Elapsed.TotalMilliseconds * 1000.0 / Runs;

            sw.Restart();
            int reachable = 0;
            for (int i = 0; i < Runs; i++)
                reachable += graph.IsReachable(points[i % points.Count], points[(i * 7 + 3) % points.Count]) ? 1 : 0;
            sw.Stop();
            double reachUs = sw.Elapsed.TotalMilliseconds * 1000.0 / Runs;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            string line = $"{Runs} queries over {points.Count} points, seed 12345: FindPath {pathUs:F1} us avg ({found} found, {pathPoints / Math.Max(1, found)} points avg), " +
                          $"NearestWalkableCell {nearestUs:F2} us ({nearest} hit), IsReachable {reachUs:F2} us ({reachable} true); allocated {allocated} bytes.";
            Directory.CreateDirectory(ReportDir);
            File.WriteAllText(Path.Combine(ReportDir, "timings.txt"), line + Environment.NewLine);
            Debug.Log("[NavGraphTests] " + line);

            Assert.That(found, Is.EqualTo(reachable), "FindPath and IsReachable disagree");
            Assert.That(found, Is.GreaterThan(Runs / 2));
            Assert.That(allocated, Is.EqualTo(0), "queries allocated after warm-up");
        }

        private static Vector3 Stand(Vector3 floorPoint) => floorPoint + Vector3.up * 0.35f;

        private static int IndexOf(ProceduralCastleData data, Vector2Int cell)
        {
            for (int i = 0; i < data.PlacedModules.Count; i++)
            {
                if (data.PlacedModules[i].GridPosition == cell)
                    return i;
            }
            return -1;
        }
    }
}
