using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Guards;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Guards never stand still, follow sound, and keep hunting at the hue and cry (#193, #194, #195).
    /// The decisions are asserted on <see cref="GuardBrain"/>; the rest step a real
    /// <see cref="CastleGuard"/> with no NavMesh (it steers straight), and two play-mode tests use a
    /// runtime NavMesh for the snapping and the stuck watchdog.
    /// </summary>
    public class GuardMovementTests
    {
        private readonly List<Object> _spawned = new List<Object>();
        private readonly List<NavMeshDataInstance> _meshes = new List<NavMeshDataInstance>();

        [SetUp]
        public void SetUp() => CastleGuard.EndArrivalGrace();

        [TearDown]
        public void TearDown()
        {
            CastleGuard.ClearIntruders();
            foreach (NavMeshDataInstance mesh in _meshes)
                mesh.Remove();
            _meshes.Clear();
            foreach (Object o in _spawned)
                if (o != null)
                    Object.DestroyImmediate(o);
            _spawned.Clear();
        }

        private T Track<T>(T o) where T : Object
        {
            _spawned.Add(o);
            return o;
        }

        private CastleGuard MakeGuard(Vector3 position, AlarmFSMManager alarm = null)
        {
            var go = Track(new GameObject("Guard"));
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            var guard = go.AddComponent<CastleGuard>();
            guard.Configure(alarm);
            return guard;
        }

        private AlarmFSMManager MakeAlarm(float level = 0f)
        {
            var alarm = Track(new GameObject("Alarm")).AddComponent<AlarmFSMManager>();
            if (level > 0f)
                alarm.SetAlarmLevel(level);
            return alarm;
        }

        private Transform MakeIntruder(Vector3 position)
        {
            var go = Track(new GameObject("Intruder"));
            go.transform.position = position;
            CastleGuard.RegisterIntruder(go.transform);
            return go.transform;
        }

        private static void Run(CastleGuard guard, float seconds, float step = 0.1f)
        {
            for (float t = 0f; t < seconds; t += step)
                guard.Tick(step);
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // --- GuardBrain ----------------------------------------------------------------------

        [Test]
        public void Test_StuckMeansUnderThirtyCentimetresInTheWindow()
        {
            Assert.IsTrue(GuardBrain.IsStuck(0f));
            Assert.IsTrue(GuardBrain.IsStuck(GuardBrain.StuckProgress - 0.01f));
            Assert.IsFalse(GuardBrain.IsStuck(GuardBrain.StuckProgress));
            Assert.AreEqual(1.5f, GuardBrain.StuckSeconds, 0.001f);
        }

        [Test]
        public void Test_TheSweepVisitsSpreadOutPointsWithinEightMetres()
        {
            var points = new List<Vector3>();
            for (int i = 0; i < 6; i++)
            {
                Vector3 offset = GuardBrain.SweepOffset(i);
                Assert.That(offset.magnitude, Is.InRange(3.99f, 8.01f), "Sweep point " + i);
                Assert.AreEqual(0f, offset.y, 0.0001f);
                points.Add(offset);
            }
            for (int a = 0; a < points.Count; a++)
                for (int b = a + 1; b < points.Count; b++)
                    Assert.Greater(Vector3.Distance(points[a], points[b]), 2f,
                        "Sweep points " + a + " and " + b + " must not sit on top of each other.");
        }

        [Test]
        public void Test_OnlyAHuntingGuardFollowsANoise()
        {
            const float loud = 0.6f;
            Assert.IsTrue(GuardBrain.ShouldFollowNoise(GuardAlertState.Searching, false, loud, AlarmState.Calm));
            Assert.IsTrue(GuardBrain.ShouldFollowNoise(GuardAlertState.Chasing, false, loud, AlarmState.Calm),
                "A chaser that has lost sight follows the noise.");
            Assert.IsFalse(GuardBrain.ShouldFollowNoise(GuardAlertState.Chasing, true, loud, AlarmState.Calm),
                "A chaser that can see its target does not.");
            Assert.IsFalse(GuardBrain.ShouldFollowNoise(GuardAlertState.Patrolling, false, loud, AlarmState.Calm),
                "A patrolling guard investigates instead; that path is unchanged.");
            Assert.IsFalse(GuardBrain.ShouldFollowNoise(GuardAlertState.Searching, false, 0.01f, AlarmState.Calm),
                "Background noise is not a lead.");
        }

        [Test]
        public void Test_OnlyTheHueAndCryKeepsSearchersAndInvestigatorsHunting()
        {
            Assert.IsTrue(GuardBrain.ShouldHunt(GuardAlertState.Searching, AlarmState.HueAndCry));
            Assert.IsTrue(GuardBrain.ShouldHunt(GuardAlertState.Investigating, AlarmState.HueAndCry));
            Assert.IsFalse(GuardBrain.ShouldHunt(GuardAlertState.Searching, AlarmState.Roused));
            Assert.IsFalse(GuardBrain.ShouldHunt(GuardAlertState.Chasing, AlarmState.HueAndCry));
            Assert.IsFalse(GuardBrain.ShouldHunt(GuardAlertState.Incapacitated, AlarmState.HueAndCry));
            Assert.IsFalse(GuardBrain.ShouldHunt(GuardAlertState.Patrolling, AlarmState.HueAndCry));
        }

        [Test]
        public void Test_TheHuntOffsetIsThreeToFiveMetresInAnyDirection()
        {
            for (float angle = 0f; angle <= 1f; angle += 0.125f)
                for (float distance = 0f; distance <= 1f; distance += 0.25f)
                    Assert.That(GuardBrain.HuntOffset(angle, distance).magnitude, Is.InRange(2.99f, 5.01f));
        }

        [Test]
        public void Test_TheHuntIsStaggeredPerGuardAndDueAfterAFewSeconds()
        {
            Assert.AreNotEqual(GuardBrain.HuntDelay(1), GuardBrain.HuntDelay(2));
            for (int id = -50; id < 50; id++)
                Assert.That(GuardBrain.HuntDelay(id), Is.InRange(3f, 4f));
            Assert.IsFalse(GuardBrain.HuntDue(1f, 7));
            Assert.IsTrue(GuardBrain.HuntDue(4f, 7));
        }

        // --- #193: never standing still -----------------------------------------------------

        [Test]
        public void Test_ASearchingGuardSweepsInsteadOfParking()
        {
            AlarmFSMManager alarm = MakeAlarm(95f);   // the hunt never ends, so it must keep moving
            CastleGuard guard = MakeGuard(Vector3.zero, alarm);
            Transform intruder = MakeIntruder(new Vector3(0f, 0f, 5f));
            guard.Tick(0.1f);
            Assert.AreEqual(GuardAlertState.Chasing, guard.State, "Test premise.");
            CastleGuard.UnregisterIntruder(intruder);
            guard.Tick(0.1f);
            Assert.AreEqual(GuardAlertState.Searching, guard.State, "Test premise.");
            Vector3 centre = guard.LastKnownIntruderPosition;

            var goals = new List<Vector3>();
            float farthest = 0f;
            for (int i = 0; i < 600; i++)
            {
                guard.Tick(0.1f);
                if (guard.Destination.HasValue
                    && (goals.Count == 0 || Flat(goals[goals.Count - 1], guard.Destination.Value) > 0.5f))
                    goals.Add(guard.Destination.Value);
                farthest = Mathf.Max(farthest, Flat(guard.transform.position, centre));
            }

            Assert.GreaterOrEqual(goals.Count, 4,
                "A searching guard must keep choosing new points, not stand on the last-known spot.");
            Assert.Greater(farthest, 3f, "It must actually walk around the spot.");
        }

        [Test]
        public void Test_AnInvestigatingGuardLooksAroundThenReturnsToItsRoute()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            guard.OnNoiseHeard(new NoiseEvent(new Vector3(0f, 0f, 3f), 0.7f, NoiseType.GlassBreak));

            // Walk until it reaches the noise: the destination is dropped while it looks around.
            guard.Tick(0.1f);   // Destination is only set once it has ticked
            int guardLimit = 0;
            while (guard.Destination.HasValue && guardLimit++ < 200)
                guard.Tick(0.1f);
            Assert.AreEqual(GuardAlertState.Investigating, guard.State, "Test premise: it arrived and is looking.");

            float yaw = guard.transform.eulerAngles.y;
            Run(guard, 1f);
            Assert.AreEqual(GuardAlertState.Investigating, guard.State, "Still looking round.");
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(yaw, guard.transform.eulerAngles.y)), 30f,
                "It turns on the spot rather than standing there.");

            Run(guard, GuardBrain.LookAroundSeconds);
            Assert.AreEqual(GuardAlertState.Patrolling, guard.State, "Then back to the route.");
        }

        [Test]
        public void Test_AGuardWithNoRouteWandersBetweenPoints()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            var goals = new List<Vector3>();
            float farthest = 0f;
            for (int i = 0; i < 400; i++)
            {
                guard.Tick(0.1f);
                if (guard.Destination.HasValue
                    && (goals.Count == 0 || Flat(goals[goals.Count - 1], guard.Destination.Value) > 0.5f))
                    goals.Add(guard.Destination.Value);
                farthest = Mathf.Max(farthest, Flat(guard.transform.position, Vector3.zero));
            }

            Assert.GreaterOrEqual(goals.Count, 3, "A guard with no route wanders rather than standing its post.");
            Assert.That(farthest, Is.InRange(2f, 12f), "And stays near where it was posted.");
        }

        [Test]
        public void Test_AGuardStuckAgainstAWallGivesUpItsWaypointAndMovesOn()
        {
            // No NavMesh, so the guard steers straight: a wall it cannot pass is a wall it pushes at.
            // Stand-in for the wall: the guard is held in place, as a wall would hold it.
            CastleGuard guard = MakeGuard(Vector3.zero);
            var a = Track(new GameObject("A")).transform;
            var b = Track(new GameObject("B")).transform;
            a.position = new Vector3(0f, 0f, 20f);
            b.position = new Vector3(20f, 0f, 0f);
            guard.Configure(null, new List<Transform> { a, b });

            var seen = new List<Vector3>();
            for (int i = 0; i < 100; i++)
            {
                guard.transform.position = Vector3.zero;   // the wall
                guard.Tick(0.1f);
                if (guard.Destination.HasValue && (seen.Count == 0 || Flat(seen[seen.Count - 1], guard.Destination.Value) > 0.5f))
                    seen.Add(guard.Destination.Value);
            }

            Assert.GreaterOrEqual(seen.Count, 2, "After about 3 s without progress the guard must skip to the next waypoint.");
            Assert.AreEqual(b.position, seen[1], "The next waypoint is the one after the blocked one.");
        }

        [Test]
        public void Test_AGuardPlannedWithOnePointGetsASecondNearby()
        {
            var home = new Vector3(10f, 0f, -4f);
            for (int seed = 0; seed < 20; seed++)
            {
                Vector3 extra = Plunderspell.Raid.GuardSpawner.NearbyPoint(home, new System.Random(seed));
                Assert.That(Flat(extra, home), Is.InRange(2.9f, 5.1f), "Seed " + seed);
            }
        }

        // --- #194: following sound -----------------------------------------------------------

        [Test]
        public void Test_ANoiseMovesASearchingGuardsTarget()
        {
            AlarmFSMManager alarm = MakeAlarm(95f);
            CastleGuard guard = MakeGuard(Vector3.zero, alarm);
            Transform intruder = MakeIntruder(new Vector3(0f, 0f, 5f));
            guard.Tick(0.1f);
            CastleGuard.UnregisterIntruder(intruder);
            guard.Tick(0.1f);
            Assert.AreEqual(GuardAlertState.Searching, guard.State, "Test premise.");
            Vector3 was = guard.LastKnownIntruderPosition;
            var noiseAt = new Vector3(-30f, 0f, 20f);

            guard.OnNoiseHeard(new NoiseEvent(noiseAt, 0.5f, NoiseType.GlassBreak));
            guard.Tick(0.1f);

            Assert.AreNotEqual(was, guard.LastKnownIntruderPosition);
            Assert.AreEqual(noiseAt, guard.LastKnownIntruderPosition, "The search is now centred on the noise.");
            Assert.AreEqual(noiseAt, guard.Destination, "And the guard heads there.");
            Assert.AreEqual(GuardAlertState.Searching, guard.State, "It stays in the hunt; a noise is not a new state.");
        }

        [Test]
        public void Test_AChaserThatLostSightHeadsForTheNoise()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            guard.SetAlertState(GuardAlertState.Chasing);
            var noiseAt = new Vector3(15f, 0f, 15f);

            guard.OnNoiseHeard(new NoiseEvent(noiseAt, 0.6f, NoiseType.Explosion));

            Assert.AreEqual(noiseAt, guard.LastKnownIntruderPosition);
        }

        [Test]
        public void Test_AChaserWhoCanSeeItsTargetIgnoresANoise()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            Vector3 target = new Vector3(0f, 0f, 6f);
            MakeIntruder(target);
            guard.Tick(0.1f);
            Assert.AreEqual(GuardAlertState.Chasing, guard.State, "Test premise.");

            guard.OnNoiseHeard(new NoiseEvent(new Vector3(40f, 0f, 40f), 0.9f, NoiseType.Explosion));

            Assert.AreEqual(target, guard.LastKnownIntruderPosition, "It keeps its eyes on the player.");
        }

        [Test]
        public void Test_ANoiseStillSendsAPatrollingGuardToInvestigate()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            guard.OnNoiseHeard(new NoiseEvent(new Vector3(0f, 0f, 8f), 0.6f, NoiseType.VoiceCast));
            Assert.AreEqual(GuardAlertState.Investigating, guard.State);
        }

        // --- #195: the hue and cry keeps hunting ----------------------------------------------

        [Test]
        public void Test_AtTheHueAndCryASearchingGuardIsResentWhenThePlayerMoves()
        {
            AlarmFSMManager alarm = MakeAlarm(95f);
            CastleGuard guard = MakeGuard(Vector3.zero, alarm);
            guard.SetAlertState(GuardAlertState.Searching);
            Vector3 first = new Vector3(0f, 0f, 70f);          // beyond even the hue and cry's sight
            Transform player = MakeIntruder(first);

            Run(guard, 4.5f);
            Assert.That(Flat(guard.LastKnownIntruderPosition, first), Is.InRange(2.9f, 5.1f),
                "It is sent to a point 3 to 5 m from the player, not onto them.");

            Vector3 second = new Vector3(-70f, 0f, 0f);
            player.position = second;
            Run(guard, 4.5f);

            Assert.That(Flat(guard.LastKnownIntruderPosition, second), Is.InRange(2.9f, 5.1f),
                "Every few seconds the hue and cry points it at where the player is now.");
        }

        [Test]
        public void Test_AtTheHueAndCryAnInvestigatingGuardIsResentToo()
        {
            AlarmFSMManager alarm = MakeAlarm(95f);
            CastleGuard guard = MakeGuard(Vector3.zero, alarm);
            guard.AlertTo(new Vector3(0f, 0f, -70f));
            var player = new Vector3(70f, 0f, 0f);
            MakeIntruder(player);

            Run(guard, 4.5f);

            Assert.IsTrue(guard.InvestigationTarget.HasValue);
            Assert.That(Flat(guard.InvestigationTarget.Value, player), Is.InRange(2.9f, 5.1f));
        }

        [Test]
        public void Test_ACalmCastleDoesNotResendASearchingGuard()
        {
            CastleGuard guard = MakeGuard(Vector3.zero);
            guard.SetAlertState(GuardAlertState.Searching);
            MakeIntruder(new Vector3(0f, 0f, 70f));
            Vector3 was = guard.LastKnownIntruderPosition;

            Run(guard, 8f);

            Assert.AreEqual(was, guard.LastKnownIntruderPosition, "Only the hue and cry hunts the players down.");
        }

        // --- On a real NavMesh (play mode) ------------------------------------------------------

        private void BuildMesh(params Vector3[] centres)
        {
            var sources = new List<NavMeshBuildSource>();
            var bounds = new Bounds(Vector3.zero, Vector3.one);
            foreach (Vector3 c in centres)
            {
                sources.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(20f, 0.2f, 20f),
                    transform = Matrix4x4.TRS(c + Vector3.down * 0.1f, Quaternion.identity, Vector3.one),
                    area = 0
                });
                bounds.Encapsulate(new Bounds(c, new Vector3(22f, 4f, 22f)));
            }
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            _meshes.Add(NavMesh.AddNavMeshData(data));
        }

        private CastleGuard MakeAgentGuard(Vector3 position)
        {
            var go = Track(new GameObject("AgentGuard"));
            go.transform.position = position;
            go.AddComponent<BoxCollider>();
            var agent = go.AddComponent<NavMeshAgent>();
            var guard = go.AddComponent<CastleGuard>();
            guard.Configure(null);
            Assert.IsTrue(agent.isOnNavMesh, "Test premise: the agent stands on the runtime NavMesh.");
            return guard;
        }

        [UnityTest]
        public IEnumerator Test_ANoiseOffTheMeshIsSnappedSoTheGuardStillArrives()
        {
            Assume.That(Application.isPlaying, "Needs the navigation update, which only runs in play mode.");
            BuildMesh(Vector3.zero);                        // floor spans -10..10
            CastleGuard guard = MakeAgentGuard(new Vector3(-5f, 0f, 0f));
            var inWall = new Vector3(14f, 0f, 0f);         // four metres past the edge of the floor
            guard.OnNoiseHeard(new NoiseEvent(inWall, 0.8f, NoiseType.Explosion));

            float deadline = Time.time + 12f;
            while (Time.time < deadline && guard.State == GuardAlertState.Investigating)
                yield return null;

            Assert.AreEqual(GuardAlertState.Patrolling, guard.State,
                "It reached the nearest walkable point, looked around and went back to the route.");
            Assert.Greater(guard.transform.position.x, 6f, "It walked towards the noise.");
        }

        [UnityTest]
        public IEnumerator Test_AnUnreachableWaypointIsSkippedAndTheRouteGoesOn()
        {
            Assume.That(Application.isPlaying, "Needs the navigation update, which only runs in play mode.");
            BuildMesh(Vector3.zero, new Vector3(40f, 0f, 0f));   // two floors with no way between them
            CastleGuard guard = MakeAgentGuard(new Vector3(-5f, 0f, 0f));
            var island = Track(new GameObject("Island")).transform;
            var reachable = Track(new GameObject("Reachable")).transform;
            island.position = new Vector3(40f, 0f, 0f);
            reachable.position = new Vector3(6f, 0f, 6f);
            guard.Configure(null, new List<Transform> { island, reachable });

            float deadline = Time.time + 25f;
            float closest = float.MaxValue;
            while (Time.time < deadline && closest > 1.5f)
            {
                closest = Mathf.Min(closest, Flat(guard.transform.position, reachable.position));
                yield return null;
            }

            Assert.LessOrEqual(closest, 1.5f,
                "A waypoint on an island is given up after the watchdog's re-path and detour, and the next one is walked to.");
        }
    }
}
