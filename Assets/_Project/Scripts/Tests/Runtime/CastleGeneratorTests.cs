using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Castle;
using Plunderspell.Spells;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// EditMode tests for the Milestone 2a procedural castle generator, socket compatibility and
    /// A* path validation. Also re-runs the M1 Levenshtein check to guard against regressions.
    /// </summary>
    public class CastleGeneratorTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private ProceduralCastleGenerator MakeGenerator()
        {
            var go = new GameObject("CastleGen");
            _spawned.Add(go);
            return go.AddComponent<ProceduralCastleGenerator>();
        }

        [Test]
        public void Test_DeterministicGeneration()
        {
            var gen = MakeGenerator();
            ProceduralCastleData a = gen.Generate(42);
            // Snapshot before regenerating (Generate clears/rebuilds internal state).
            var snapshot = new List<ProceduralCastleData.PlacedModule>(a.PlacedModules);

            ProceduralCastleData b = gen.Generate(42);

            Assert.AreEqual(snapshot.Count, b.PlacedModules.Count,
                "Same seed must yield the same number of modules.");
            for (int i = 0; i < snapshot.Count; i++)
            {
                Assert.AreEqual(snapshot[i].RoomId, b.PlacedModules[i].RoomId, $"RoomId mismatch at {i}");
                Assert.AreEqual(snapshot[i].Zone, b.PlacedModules[i].Zone, $"Zone mismatch at {i}");
                Assert.AreEqual(snapshot[i].GridPosition, b.PlacedModules[i].GridPosition,
                    $"GridPosition mismatch at {i}");
                Assert.That(Vector3.Distance(snapshot[i].Position, b.PlacedModules[i].Position),
                    Is.LessThan(0.001f), $"Position mismatch at {i}");
                Assert.That(Quaternion.Angle(snapshot[i].Rotation, b.PlacedModules[i].Rotation),
                    Is.LessThan(0.01f), $"Rotation mismatch at {i}");
            }
        }

        [Test]
        public void Test_AllZonesPresent()
        {
            var gen = MakeGenerator();
            ProceduralCastleData data = gen.Generate(12345);

            var zones = new HashSet<CastleZone>();
            foreach (var pm in data.PlacedModules)
                zones.Add(pm.Zone);

            foreach (CastleZone z in System.Enum.GetValues(typeof(CastleZone)))
                Assert.IsTrue(zones.Contains(z), $"Zone {z} missing from generated castle.");
        }

        [Test]
        public void Test_PathValidatorFindsPath()
        {
            var gen = MakeGenerator();
            ProceduralCastleData data = gen.Generate(42);

            bool found = CastlePathValidator.ValidatePath(data, out List<Vector2Int> path);
            Assert.IsTrue(found, "Expected a walkable crypt->extraction path for seed 42.");
            Assert.IsNotNull(path);
            Assert.Greater(path.Count, 0, "Path should contain at least one cell.");
        }

        [Test]
        public void Test_PathValidatorOnEmptyData()
        {
            var empty = new ProceduralCastleData(0);
            bool found = CastlePathValidator.ValidatePath(empty, out List<Vector2Int> path);
            Assert.IsFalse(found, "Empty layout must not produce a path.");
            Assert.IsNotNull(path);
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void Test_SocketCompatibilityRules()
        {
            Assert.IsTrue(SocketPoint.AreTypesCompatible(SocketType.Door, SocketType.Door));
            Assert.IsTrue(SocketPoint.AreTypesCompatible(SocketType.Staircase, SocketType.Staircase));
            Assert.IsTrue(SocketPoint.AreTypesCompatible(SocketType.Window, SocketType.Window));
            Assert.IsTrue(SocketPoint.AreTypesCompatible(SocketType.ArchOpening, SocketType.ArchOpening));
            Assert.IsTrue(SocketPoint.AreTypesCompatible(SocketType.MurderHole, SocketType.WallSegment));
            Assert.IsTrue(SocketPoint.AreTypesCompatible(SocketType.WallSegment, SocketType.MurderHole));

            Assert.IsFalse(SocketPoint.AreTypesCompatible(SocketType.Door, SocketType.Window));
            Assert.IsFalse(SocketPoint.AreTypesCompatible(SocketType.Staircase, SocketType.Door));
            Assert.IsFalse(SocketPoint.AreTypesCompatible(SocketType.MurderHole, SocketType.MurderHole));
        }

        [Test]
        public void Test_ExtractionExitAssigned()
        {
            var gen = MakeGenerator();
            ProceduralCastleData data = gen.Generate(777);
            Assert.GreaterOrEqual(data.ExtractionExitIndex, 0, "An extraction exit must be assigned.");
            ProceduralCastleData.PlacedModule exit = data.PlacedModules[data.ExtractionExitIndex];
            Assert.IsTrue(exit.IsExtractionExit);
            Assert.AreEqual(CastleZone.CurtainWall, exit.Zone,
                "The castle's one gate is the way out, so the exit is a curtain-wall module.");
            Assert.AreEqual("GatehouseModule", exit.RoomId,
                "The extraction exit should be the gatehouse specifically.");
        }

        [Test]
        public void Test_CurtainWallIsAClosedLoop()
        {
            var gen = MakeGenerator();
            int radius = gen.CurtainWallRadius;

            foreach (int seed in new[] { 42, 777, 12345, -9, 20260917 })
            {
                ProceduralCastleData data = gen.Generate(seed);

                var byCell = new Dictionary<Vector2Int, ProceduralCastleData.PlacedModule>();
                foreach (var pm in data.PlacedModules)
                    byCell[pm.GridPosition] = pm;

                int gatehouses = 0;
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) != radius)
                            continue;

                        var cell = new Vector2Int(x, y);
                        Assert.IsTrue(byCell.TryGetValue(cell, out var pm),
                            $"seed {seed}: perimeter cell {cell} is a hole in the curtain wall.");
                        Assert.AreEqual(CastleZone.CurtainWall, pm.Zone,
                            $"seed {seed}: perimeter cell {cell} is not a curtain-wall module.");

                        if (pm.RoomId == "GatehouseModule")
                            gatehouses++;
                    }
                }

                Assert.AreEqual(1, gatehouses, $"seed {seed}: the castle must have exactly one gate.");
            }
        }

        [Test]
        public void Test_InteriorRoomCountIsPlayable()
        {
            var gen = MakeGenerator();

            foreach (int seed in new[] { 42, 777, 12345, -9 })
            {
                ProceduralCastleData data = gen.Generate(seed);

                int rooms = 0;
                foreach (var pm in data.PlacedModules)
                {
                    if (ProceduralCastleGenerator.IsEnclosedRoom(pm.Zone))
                        rooms++;
                }

                Assert.That(rooms, Is.InRange(40, 60),
                    $"seed {seed}: {rooms} interior rooms is outside the playable range.");
            }
        }

        /// <summary>
        /// Covers the derivation, not collider clearance: a data-only layout has no prefabs, so the
        /// capsule probe finds every candidate clear and the spawn lands on the first one. What this
        /// asserts is that for any seed the spawn is on the floor, inside the gate cell, and on the
        /// castle side of the wall.
        /// </summary>
        [Test]
        public void Test_SpawnIsJustInsideTheGatehouseForEverySeed()
        {
            var gen = MakeGenerator();
            const float expectedY = CastleSpawnResolver.FloorHeight + CastleSpawnResolver.FloorClearance
                                    + CastleSpawnResolver.PlayerHeight * 0.5f;

            foreach (int seed in new[] { 42, 777, 12345, -9, 20260917, 1 })
            {
                ProceduralCastleData data = gen.Generate(seed);
                Assert.GreaterOrEqual(data.ExtractionExitIndex, 0, $"seed {seed}: no extraction exit.");

                Vector3 gate = data.PlacedModules[data.ExtractionExitIndex].Position;
                Vector3 spawn = CastleSpawnResolver.ResolveSpawn(data);

                Assert.That(spawn.y, Is.EqualTo(expectedY).Within(0.001f),
                    $"seed {seed}: spawn is not standing on the floor.");

                var flat = new Vector2(spawn.x - gate.x, spawn.z - gate.z);
                Assert.That(flat.magnitude, Is.LessThan(6f),
                    $"seed {seed}: spawn {spawn} is outside the gate cell at {gate}.");
                Assert.Less(spawn.x, gate.x,
                    $"seed {seed}: spawn {spawn} is outside the wall rather than inside the gate.");
            }
        }

        [Test]
        public void Test_AdjacentRoomsAreDoorConnected()
        {
            var gen = MakeGenerator();

            foreach (int seed in new[] { 42, 777, 12345, -9 })
            {
                ProceduralCastleData data = gen.Generate(seed);

                var byCell = new Dictionary<Vector2Int, CastleZone>();
                foreach (var pm in data.PlacedModules)
                    byCell[pm.GridPosition] = pm.Zone;

                var directions = new[]
                {
                    Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down
                };

                int connections = 0;
                foreach (var pm in data.PlacedModules)
                {
                    if (!ProceduralCastleGenerator.IsEnclosedRoom(pm.Zone))
                        continue;

                    foreach (Vector2Int dir in directions)
                    {
                        if (!byCell.TryGetValue(pm.GridPosition + dir, out CastleZone neighbourZone))
                            continue;
                        if (!ProceduralCastleGenerator.IsEnclosedRoom(neighbourZone))
                            continue;

                        Assert.IsTrue(ProceduralCastleGenerator.HasArchwayFacing(pm.Zone, dir),
                            $"seed {seed}: {pm.RoomId} at {pm.GridPosition} has no archway facing {dir}.");
                        Assert.IsTrue(ProceduralCastleGenerator.HasArchwayFacing(neighbourZone, -dir),
                            $"seed {seed}: the module at {pm.GridPosition + dir} has no archway facing {-dir}.");
                        connections++;
                    }
                }

                Assert.Greater(connections, 0,
                    $"seed {seed}: expected at least one enclosed-room adjacency to check.");
            }
        }

        [Test]
        public void Test_CurtainWallIsNotAnEnclosedRoom()
        {
            Assert.IsFalse(ProceduralCastleGenerator.IsEnclosedRoom(CastleZone.CurtainWall));
            Assert.IsFalse(ProceduralCastleGenerator.HasArchwayFacing(CastleZone.CurtainWall, Vector2Int.up));

            foreach (CastleZone zone in new[]
                     {
                         CastleZone.OuterBailey, CastleZone.InnerWard, CastleZone.Keep, CastleZone.Crypt
                     })
            {
                Assert.IsTrue(ProceduralCastleGenerator.IsEnclosedRoom(zone));
                Assert.IsTrue(ProceduralCastleGenerator.HasArchwayFacing(zone, Vector2Int.up));
                Assert.IsTrue(ProceduralCastleGenerator.HasArchwayFacing(zone, Vector2Int.down));
                Assert.IsTrue(ProceduralCastleGenerator.HasArchwayFacing(zone, Vector2Int.left));
                Assert.IsTrue(ProceduralCastleGenerator.HasArchwayFacing(zone, Vector2Int.right));
                Assert.IsFalse(ProceduralCastleGenerator.HasArchwayFacing(zone, new Vector2Int(1, 1)));
            }
        }

        [Test]
        public void Test_LevenshteinFromM1_StillPasses()
        {
            Assert.AreEqual(1, MisfireEngine.LevenshteinDistance("IGNIS", "AGNIS"));
            Assert.AreEqual(1, MisfireEngine.LevenshteinDistance("FRANGO", "FRANCO"));
            Assert.AreEqual(0, MisfireEngine.LevenshteinDistance("PORTA", "PORTA"));
            Assert.AreEqual(5, MisfireEngine.LevenshteinDistance("", "PORTA"));
        }
    }
}
