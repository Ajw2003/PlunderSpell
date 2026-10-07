using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Castle;
using Plunderspell.Extraction;
using Plunderspell.Inventory;
using Plunderspell.Lair;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// The team arrives by portal at a seeded spot inside the walls, leaves by the same portal, and
    /// cannot leave any other way (docs/plans/night-atmosphere.md, section 6). These pin down where
    /// that spot may and may not be, and that the castle is sealed.
    /// </summary>
    public class CastleArrivalTests
    {
        private readonly List<Object> _spawned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _spawned)
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

        private static int Chebyshev(Vector2Int a, Vector2Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        [Test]
        public void Test_TheSameSeedAlwaysArrivesInTheSameRoom()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 25; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                int first = CastleArrivalPlanner.ChooseModule(castle, seed);
                int second = CastleArrivalPlanner.ChooseModule(castle, seed);
                Assert.AreEqual(first, second, $"Seed {seed}: every peer must pick the same arrival.");
            }
        }

        [Test]
        public void Test_TheTeamNeverArrivesInTheKeepCryptOrGate()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 50; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                int index = CastleArrivalPlanner.ChooseModule(castle, seed);
                Assert.AreNotEqual(CastleArrivalPlanner.NoArrival, index, $"Seed {seed}: no arrival chosen.");

                ProceduralCastleData.PlacedModule module = castle.PlacedModules[index];
                Assert.IsTrue(CastleArrivalPlanner.IsArrivalZone(module.Zone),
                    $"Seed {seed}: arrived in {module.Zone}, which is too deep to start in.");
                Assert.IsFalse(module.IsExtractionExit || index == castle.ExtractionExitIndex,
                    $"Seed {seed}: the gatehouse is sealed and is never the arrival.");
            }
        }

        /// <summary>#140: a portal on the curtain strip opens in front of an entrance: the room
        /// inward of it is enclosed, and its archway onto the strip is left open.</summary>
        [Test]
        public void Test_AStripArrivalIsInFrontOfAWayIn()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            int stripArrivals = 0;
            for (int seed = 1; seed <= 120; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                ProceduralCastleData.PlacedModule module = castle.PlacedModules[CastleArrivalPlanner.ChooseModule(castle, seed)];
                if (module.Zone != CastleZone.CurtainWall)
                    continue;
                stripArrivals++;
                Assert.Contains(module.GridPosition, castle.EntranceCells,
                    $"Seed {seed}: arrived on the strip at {module.GridPosition}, where every archway is plugged.");
                Vector2Int inward = CastleEntrancePlanner.InwardCell(module.GridPosition);
                Assert.IsTrue(castle.PlacedModules.Exists(m => m.GridPosition == inward && ProceduralCastleGenerator.IsEnclosedRoom(m.Zone)),
                    $"Seed {seed}: the cell inward of the arrival, {inward}, is not a room to walk into.");
            }
            Assert.Greater(stripArrivals, 0, "Test premise: some seeds should still arrive on the strip.");
        }

        /// <summary>#140 and #147: the drawbridge is a curtain-wall module outside the sealed gate.</summary>
        [Test]
        public void Test_TheTeamNeverArrivesOutsideTheWall()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 120; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                Vector2Int cell = castle.PlacedModules[CastleArrivalPlanner.ChooseModule(castle, seed)].GridPosition;
                Assert.LessOrEqual(Chebyshev(cell, Vector2Int.zero), generator.CurtainWallRadius,
                    $"Seed {seed}: arrived at {cell}, outside the curtain wall.");
            }
        }

        /// <summary>#140: the strip is not a dead end anywhere round the castle.</summary>
        [Test]
        public void Test_EverySideOfTheStripHasAWayIn()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            int radius = generator.CurtainWallRadius;
            for (int seed = 1; seed <= 60; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                var sides = new HashSet<Vector2Int>();
                foreach (Vector2Int cell in castle.EntranceCells)
                {
                    Assert.IsTrue(CastleEntrancePlanner.IsSideCell(cell, radius), $"Seed {seed}: {cell} is not a side of the strip.");
                    sides.Add(CastleEntrancePlanner.SideOf(cell, radius));
                }
                Assert.AreEqual(4, sides.Count,
                    $"Seed {seed}: only {sides.Count} sides of the strip have an entrance ({castle.EntranceCells.Count} in all).");
            }
        }

        [Test]
        public void Test_DifferentSeedsArriveInDifferentPlaces()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            var seen = new HashSet<Vector2Int>();
            for (int seed = 1; seed <= 30; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                int index = CastleArrivalPlanner.ChooseModule(castle, seed);
                seen.Add(castle.PlacedModules[index].GridPosition);
            }
            Assert.GreaterOrEqual(seen.Count, 10, "The arrival should move around the castle from raid to raid.");
        }

        [Test]
        public void Test_AnEmptyLayoutHasNoArrival()
        {
            Assert.AreEqual(CastleArrivalPlanner.NoArrival,
                CastleArrivalPlanner.ChooseModule(new ProceduralCastleData(1), 1));
            Assert.AreEqual(CastleArrivalPlanner.NoArrival, CastleArrivalPlanner.ChooseModule(null, 1));
        }

        [Test]
        public void Test_ACurtainWallArrivalStandsInsideTheWallNotOnIt()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 50; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                for (int i = 0; i < castle.PlacedModules.Count; i++)
                {
                    ProceduralCastleData.PlacedModule module = castle.PlacedModules[i];
                    if (module.Zone != CastleZone.CurtainWall || module.GridPosition == Vector2Int.zero)
                        continue;

                    Vector3 anchor = CastleArrivalPlanner.AnchorFor(castle, i);
                    Assert.Less(new Vector2(anchor.x, anchor.z).magnitude,
                        new Vector2(module.Position.x, module.Position.z).magnitude,
                        $"Seed {seed}: a curtain-wall anchor must sit inward of the cell centre, off the wall.");
                }
            }
        }

        [Test]
        public void Test_ResolveArrivalStandsInTheChosenRoom()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            ProceduralCastleData castle = generator.Generate(777);

            Vector3 point = CastleSpawnResolver.ResolveArrival(castle, 777, out int index);

            Assert.AreEqual(CastleArrivalPlanner.ChooseModule(castle, 777), index);
            Vector3 anchor = CastleArrivalPlanner.AnchorFor(castle, index);
            Assert.Less(Vector2.Distance(new Vector2(point.x, point.z), new Vector2(anchor.x, anchor.z)), 6f,
                "The standing point must stay inside the chosen 12 m cell.");
            Assert.AreEqual(CastleSpawnResolver.FloorHeight + CastleSpawnResolver.FloorClearance
                            + CastleSpawnResolver.PlayerHeight * 0.5f, point.y, 0.001f,
                "Same height convention as ResolveSpawn: the capsule's centre.");
        }

        [Test]
        public void Test_NoOneIsStoodOverAHole()
        {
            // A floor slab (top at FloorHeight) with a 4 m square hole round the anchor, like the
            // moat under the drawbridge that dropped a player on seed 43 (#147).
            var anchor = new Vector3(100f, 0f, 100f);
            float top = CastleSpawnResolver.FloorHeight;
            void Slab(Vector3 centre, Vector3 size)
            {
                GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _spawned.Add(slab);
                slab.transform.position = centre + new Vector3(0f, top - 0.5f, 0f);
                slab.transform.localScale = new Vector3(size.x, 1f, size.z);
            }
            Slab(anchor + new Vector3(-7f, 0f, 0f), new Vector3(10f, 1f, 20f));
            Slab(anchor + new Vector3(7f, 0f, 0f), new Vector3(10f, 1f, 20f));
            Slab(anchor + new Vector3(0f, 0f, -7f), new Vector3(4f, 1f, 10f));
            Slab(anchor + new Vector3(0f, 0f, 7f), new Vector3(4f, 1f, 10f));
            Physics.SyncTransforms();

            Vector3 point = CastleSpawnResolver.FirstClearStandingPoint(anchor);

            float feet = point.y - CastleSpawnResolver.PlayerHeight * 0.5f;
            Assert.IsTrue(Physics.Raycast(new Vector3(point.x, feet + 0.1f, point.z), Vector3.down, 1f),
                $"Stood at {point}, over the hole: the player falls through the floor.");
        }

        [Test]
        public void Test_ResolveArrivalFallsBackToTheGateWhenNothingQualifies()
        {
            var layout = new ProceduralCastleData(5);
            layout.PlacedModules.Add(new ProceduralCastleData.PlacedModule(
                "ThroneRoomKeep", new Vector3(0f, 0f, 0f), Quaternion.identity, CastleZone.Keep, Vector2Int.zero));

            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("No arrival room"));
            Vector3 point = CastleSpawnResolver.ResolveArrival(layout, 5, out int index);

            Assert.AreEqual(CastleArrivalPlanner.NoArrival, index);
            Assert.AreEqual(CastleSpawnResolver.ResolveSpawn(layout), point);
        }

        [Test]
        public void Test_NoGuardIsPostedOrPatrolsNextToTheArrival()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 25; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                int arrival = CastleArrivalPlanner.ChooseModule(castle, seed);
                Vector2Int safe = castle.PlacedModules[arrival].GridPosition;

                foreach (GuardPlacement guard in GuardPlacementPlanner.Plan(castle, seed, 1f, arrival))
                {
                    Vector2Int at = castle.PlacedModules[guard.ModuleIndex].GridPosition;
                    if (castle.PlacedModules[guard.ModuleIndex].Level != CastleLevels.Ground)
                        continue;   // the safe ring covers the portal's own floor only (#247)
                    Assert.Greater(Chebyshev(at, safe), GuardPlacementPlanner.SafeEntranceRadius,
                        $"Seed {seed}: a guard posted beside the portal kills the team on arrival.");

                    foreach (Vector3 stop in guard.PatrolRoute)
                    {
                        foreach (ProceduralCastleData.PlacedModule module in castle.PlacedModules)
                        {
                            // Stops stand on the floor top; the safe ring is a ground-floor idea (#247).
                            if (module.Level != CastleLevels.Ground
                                || (GuardPlacementPlanner.StandingPoint(module) - stop).sqrMagnitude > 0.01f)
                                continue;
                            Assert.Greater(Chebyshev(module.GridPosition, safe), GuardPlacementPlanner.SafeEntranceRadius,
                                $"Seed {seed}: a patrol walks into the safe ring round the portal.");
                        }
                    }
                }
            }
        }

        [Test]
        public void Test_ArrivalIsAGroundFloorSingleStoreyRoom()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 25; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                ProceduralCastleData.PlacedModule room = castle.PlacedModules[CastleArrivalPlanner.ChooseModule(castle, seed)];
                Assert.AreEqual(CastleLevels.Ground, room.Level, $"Seed {seed}: arrival must be on the ground floor.");
                Assert.LessOrEqual(room.Storeys, 1, $"Seed {seed}: arrival must not be a stair.");
                Assert.GreaterOrEqual(CastleArrivalPlanner.AnchorFor(castle, CastleArrivalPlanner.ChooseModule(castle, seed)).y, 0f,
                    $"Seed {seed}: arrival point is below the ground.");
            }
        }

        [Test]
        public void Test_GuardsStandOnTheirOwnFloorAndPatrolWithinIt()
        {
            ProceduralCastleGenerator generator = MakeGenerator();
            for (int seed = 1; seed <= 25; seed++)
            {
                ProceduralCastleData castle = generator.Generate(seed);
                foreach (GuardPlacement guard in GuardPlacementPlanner.Plan(castle, seed))
                {
                    ProceduralCastleData.PlacedModule home = castle.PlacedModules[guard.ModuleIndex];
                    Assert.LessOrEqual(home.Storeys, 1, $"Seed {seed}: a stair is not a post.");
                    Assert.GreaterOrEqual(guard.Position.y, home.Position.y + 0.30f,
                        $"Seed {seed}: a guard stands inside the floor slab.");
                    foreach (Vector3 stop in guard.PatrolRoute)
                        Assert.AreEqual(guard.Position.y, stop.y, 0.01f,
                            $"Seed {seed}: a patrol leaves the guard's floor.");
                }
            }
        }

        [Test]
        public void Test_PlaceAsPortalStandsTheTriggerOnTheFloorPoint()
        {
            var go = new GameObject("ExtractionZone");
            _spawned.Add(go);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(8f, 6f, 8f);
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Marker";
            marker.transform.SetParent(go.transform, false);
            marker.transform.localScale = new Vector3(8f, 0.1f, 8f);
            ExtractionZone zone = go.AddComponent<ExtractionZone>();

            var floor = new Vector3(20f, 0.3f, -8f);
            zone.PlaceAsPortal(floor);

            Assert.AreEqual(floor, go.transform.position);
            Assert.AreEqual(new Vector3(ExtractionZone.PortalFootprint, ExtractionZone.PortalHeight,
                ExtractionZone.PortalFootprint), box.size);
            Assert.AreEqual(ExtractionZone.PortalHeight * 0.5f, box.center.y, 0.001f,
                "The trigger rises from the floor instead of being half buried in it.");
            Assert.AreEqual(ExtractionZone.PortalFootprint, marker.transform.localScale.x, 0.001f);
        }

        [Test]
        public void Test_EveryLivingPlayerOutsideTheClosedPortalIsLeftBehind()
        {
            Assert.AreEqual(2, ExtractionZone.CountLeftBehind(4, 2));
            Assert.AreEqual(0, ExtractionZone.CountLeftBehind(3, 3));
            Assert.AreEqual(0, ExtractionZone.CountLeftBehind(1, 2), "Never negative.");
        }

        [Test]
        public void Test_TheBoundaryStopsAnythingLeavingTheCastle()
        {
            var host = new GameObject("Castle");
            _spawned.Add(host);
            CastleBoundary boundary = CastleBoundary.EnsureOn(host);
            boundary.Rebuild(4, 12f);
            Physics.SyncTransforms();

            float edge = CastleBoundary.OuterEdge(4, 12f);
            Assert.AreEqual(54f, edge, 0.001f, "Ring 4 of 12 m cells: the wall's outer face is 54 m out.");

            foreach (Vector3 direction in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
            {
                // At head height through the gate, and high enough to clear the battlements.
                foreach (float height in new[] { 1.5f, 20f })
                {
                    var origin = new Vector3(0f, height, 0f);
                    RaycastHit[] hits = Physics.RaycastAll(origin, direction, 200f);
                    bool stopped = false;
                    foreach (RaycastHit hit in hits)
                    {
                        if (!hit.collider.transform.IsChildOf(host.transform))
                            continue;
                        stopped = true;
                        Assert.AreEqual(edge, Vector3.Dot(hit.point, direction), 0.05f,
                            "The boundary sits on the wall's outer face, not inside the bailey.");
                    }
                    Assert.IsTrue(stopped, $"Nothing stops a player leaving {direction} at {height} m.");
                }
            }
            Assert.AreEqual(0, host.GetComponentsInChildren<Renderer>().Length, "The boundary is invisible.");

            boundary.Rebuild(4, 12f);
            Assert.AreEqual(4, host.GetComponentsInChildren<BoxCollider>().Length,
                "Rebuilding replaces the walls rather than stacking another set.");
        }

        [Test]
        public void Test_ARaidOpensThePortalInTheArrivalRoomAndSealsTheCastle()
        {
            var lairGo = new GameObject("Lair");
            _spawned.Add(lairGo);
            LairHubManager lair = lairGo.AddComponent<LairHubManager>();

            var zoneGo = new GameObject("ExtractionZone");
            _spawned.Add(zoneGo);
            zoneGo.transform.position = new Vector3(72f, 0f, 0f); // where the scene used to put it
            zoneGo.AddComponent<BoxCollider>().isTrigger = true;
            ExtractionZone zone = zoneGo.AddComponent<ExtractionZone>();

            var playerGo = new GameObject("Player");
            _spawned.Add(playerGo);

            var directorGo = new GameObject("RaidDirector");
            _spawned.Add(directorGo);
            RaidDirector director = directorGo.AddComponent<RaidDirector>();
            ProceduralCastleGenerator generator = MakeGenerator();
            director.Configure(generator, null, zone, lair, playerRoot: playerGo.transform);

            Vector3 opened = Vector3.positiveInfinity;
            director.PortalOpened += point => opened = point;

            director.SetFixedSeed(4242);
            director.StartRaid(HistoricalEra.HighMedieval);

            Assert.AreNotEqual(CastleArrivalPlanner.NoArrival, director.ArrivalModuleIndex);
            Vector3 portal = zoneGo.transform.position;
            Assert.Less(Vector2.Distance(new Vector2(portal.x, portal.z),
                    new Vector2(director.ArrivalPoint.x, director.ArrivalPoint.z)), 0.01f,
                "The portal opens where the team arrives.");
            Assert.AreEqual(portal, opened, "Listeners hear where the portal stands.");

            float edge = CastleBoundary.OuterEdge(generator.CurtainWallRadius, 12f);
            Assert.Less(Mathf.Max(Mathf.Abs(portal.x), Mathf.Abs(portal.z)), edge - 6f,
                "The portal is inside the walls, not out by the gate.");

            float fromPortal = Vector2.Distance(new Vector2(playerGo.transform.position.x, playerGo.transform.position.z),
                new Vector2(portal.x, portal.z));
            Assert.Greater(fromPortal, ExtractionZone.PortalFootprint * 0.5f,
                "The player arrives beside the portal, not inside it, so they do not start leaving at once.");

            Assert.IsNotNull(generator.GetComponent<CastleBoundary>(), "Every raid is sealed in.");
        }
    }
}
