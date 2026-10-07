using Plunderspell.Tests.EditMode;
using Code.Scripts.EventSystems;
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Alarm;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// The director's navigation service on a flat floor (#222): the sweep that stops a guard short of
    /// a player pinned against a wall (the #200 case), guards keeping apart, and no allocation per tick.
    /// </summary>
    public class GuardNavigationServiceTests
    {
        private const float Step = 0.02f;

        private readonly List<GameObject> _created = new List<GameObject>();
        private readonly List<Blocked> _blocked = new List<Blocked>();
        private readonly List<Arrived> _arrived = new List<Arrived>();
        private EnemyDirector _director;

        [SetUp]
        public void SetUp()
        {
            TestEventBus.Create();
            _director = Make("Director").AddComponent<EnemyDirector>();
            _director.Navigation.SetMap(new FlatNavigationMap());
            EventManager.Instance.Subscribe(this, (Blocked e) => _blocked.Add(e));
            EventManager.Instance.Subscribe(this, (Arrived e) => _arrived.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _created)
                Object.DestroyImmediate(go);
            _created.Clear();
            _blocked.Clear();
            _arrived.Clear();
            TestEventBus.Destroy();
        }

        [Test]
        public void GuardStopsShortOfAPlayerPinnedAgainstAWallAndDoesNotMoveThem()
        {
            GameObject wall = MakeBox("Wall", new Vector3(5.5f, 1f, 0f), new Vector3(1f, 3f, 6f));
            GameObject player = MakePlayer(new Vector3(4.55f, 0f, 0f));
            Physics.SyncTransforms();
            Vector3 playerBefore = player.transform.position;
            GameObject guard = MakeGuard(new Vector3(0f, 0f, 0f));

            EventManager.Instance.Publish(new MoveRequest(guard.transform, new Vector3(8f, 0f, 0f), 3f, MoveReason.Chase));
            RunTicks(400);

            float playerFace = playerBefore.x - 0.4f;
            Assert.That(guard.transform.position.x + GuardNavigationService.DefaultRadius, Is.LessThanOrEqualTo(playerFace + 0.001f),
                "the guard's capsule must stop before the player");
            Assert.That(guard.transform.position.x, Is.GreaterThan(playerFace - 1.2f), "it should have walked right up to the player");
            Assert.That(player.transform.position, Is.EqualTo(playerBefore), "the player must not be displaced");
            Assert.That(wall.transform.position.x - 0.5f, Is.GreaterThan(player.transform.position.x));
            Assert.That(_blocked.Count, Is.EqualTo(1));
            Assert.That(_blocked[0].Reason, Is.EqualTo(BlockedReason.Obstacle));
            Assert.That(_arrived.Count, Is.EqualTo(0));
        }

        [Test]
        public void GuardWalksToAnOpenDestinationAndArrives()
        {
            GameObject guard = MakeGuard(Vector3.zero);
            EventManager.Instance.Publish(new MoveRequest(guard.transform, new Vector3(6f, 0f, 3f), 3f, MoveReason.Patrol));
            RunTicks(400);

            Assert.That(_arrived.Count, Is.EqualTo(1));
            Assert.That(Vector3.Distance(guard.transform.position, new Vector3(6f, 0f, 3f)), Is.LessThan(0.4f));
            Assert.That(_blocked.Count, Is.EqualTo(0));
        }

        // Regression (2026-10-02 co-op run): a route that clipped an archway's edge stopped the guard dead
        // on the corner for good. It now slides past.
        [Test]
        public void AGuardThatClipsAWallCornerSlidesPastItAndArrives()
        {
            // The wall's near face is at z = 0.25, so a guard of radius 0.4 walking along z = 0 clips it by 0.15 m.
            MakeBox("Jamb", new Vector3(4f, 1f, 1.25f), new Vector3(1f, 3f, 2f));
            Physics.SyncTransforms();
            GameObject guard = MakeGuard(Vector3.zero);

            EventManager.Instance.Publish(new MoveRequest(guard.transform, new Vector3(8f, 0f, 0f), 3f, MoveReason.Patrol));
            RunTicks(400);

            Assert.That(_blocked.Count, Is.EqualTo(0), "clipping a corner must not block the walk");
            Assert.That(_arrived.Count, Is.EqualTo(1));
        }

        // Regression (2026-10-02 co-op run): guards share the Default layer with walls, so one guard's body
        // stopped another's sweep and a crowd round a player locked solid.
        [Test]
        public void AGuardWalksPastAnotherGuardStandingInItsWay()
        {
            GameObject standing = MakeGuard(new Vector3(4f, 0f, 0f));
            AddBody(standing);
            GameObject walker = MakeGuard(Vector3.zero);
            AddBody(walker);
            Physics.SyncTransforms();

            EventManager.Instance.Publish(new MoveRequest(walker.transform, new Vector3(8f, 0f, 0f), 3f, MoveReason.Chase));
            RunTicks(400);

            Assert.That(_blocked.Count, Is.EqualTo(0), "another guard's body must not block the sweep");
            Assert.That(_arrived.Count, Is.EqualTo(1));
        }

        [Test]
        public void TwentyGuardsSentToOneSpotKeepTheirDistance()
        {
            var guards = new GameObject[20];
            for (int i = 0; i < guards.Length; i++)
            {
                guards[i] = MakeGuard(new Vector3(i % 5 * 0.4f, 0f, i / 5 * 0.4f));
                EventManager.Instance.Publish(new MoveRequest(guards[i].transform, new Vector3(15f, 0f, 0f), 3f, MoveReason.Chase));
            }

            RunTicks(1500);

            float closest = float.MaxValue;
            for (int a = 0; a < guards.Length; a++)
            {
                for (int b = a + 1; b < guards.Length; b++)
                    closest = Mathf.Min(closest, Vector3.Distance(guards[a].transform.position, guards[b].transform.position));
            }
            Debug.Log($"[GuardNavigationTests] closest pair of 20 guards after 30 s: {closest:F3} m (spacing {_director.Navigation.Tuning.SeparationSpacing} m)");
            Assert.That(closest, Is.GreaterThanOrEqualTo(0.9f * _director.Navigation.Tuning.SeparationSpacing));
        }

        [Test]
        public void TicksAllocateNothing()
        {
            MakeBox("Wall", new Vector3(0f, 1f, 30f), new Vector3(60f, 3f, 1f));
            Physics.SyncTransforms();
            for (int i = 0; i < 20; i++)
            {
                GameObject guard = MakeGuard(new Vector3(i * 1.5f, 0f, 0f));
                EventManager.Instance.Publish(new MoveRequest(guard.transform, new Vector3(i * 1.5f, 0f, 40f), 2f, MoveReason.Patrol));
            }
            RunTicks(50); // warm-up: first sweeps and first JIT

            long before = GC.GetAllocatedBytesForCurrentThread();
            RunTicks(500);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            Debug.Log($"[GuardNavigationTests] allocated over 500 ticks of 20 moving guards, sweeping a wall: {allocated} B");
            Assert.That(allocated, Is.EqualTo(0L));
        }

        private void RunTicks(int count)
        {
            for (int i = 0; i < count; i++)
                _director.Navigation.Tick(Step);
        }

        private GameObject Make(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go;
        }

        private GameObject MakeGuard(Vector3 position)
        {
            GameObject guard = Make("Guard");
            guard.transform.position = position;
            _director.Navigation.Register(guard.transform);
            return guard;
        }

        private GameObject MakeBox(string name, Vector3 centre, Vector3 size)
        {
            GameObject box = Make(name);
            box.transform.position = centre;
            box.AddComponent<BoxCollider>().size = size;
            return box;
        }

        private GameObject MakePlayer(Vector3 feet)
        {
            GameObject player = Make("Player");
            player.transform.position = feet;
            AddBody(player);
            return player;
        }

        private static void AddBody(GameObject owner)
        {
            CapsuleCollider capsule = owner.AddComponent<CapsuleCollider>();
            capsule.radius = 0.4f;
            capsule.height = 1.8f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
        }
    }
}
