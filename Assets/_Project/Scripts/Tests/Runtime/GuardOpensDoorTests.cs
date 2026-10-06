using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Alarm;
using Plunderspell.Castle;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>A guard mover that walks into a closed door opens it and goes through; a barred door too, guards having keys (#264/#263).</summary>
    public class GuardOpensDoorTests
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

        [Test]
        public void Test_AGuardWalkingIntoAClosedDoorOpensItAndGetsPast()
        {
            CastleDoor door = BuildDoor();
            float x = WalkInto(door);
            Assert.That(door.IsOpen, Is.True);
            Assert.That(x, Is.GreaterThan(1.5f), "the guard gets past the door");
        }

        [Test]
        public void Test_AGuardWalkingIntoABarredDoorOpensItAndGetsPast()
        {
            CastleDoor door = BuildDoor();
            door.Bar();
            float x = WalkInto(door);
            Assert.That(door.IsOpen, Is.True);
            Assert.That(x, Is.GreaterThan(1.5f), "the guard gets past the barred door");
        }

        // A door at x = 1 hinged at the origin: closed it is a slab across the path at z = 0.5, open it swings out of the way.
        private CastleDoor BuildDoor()
        {
            var root = new GameObject("Door");
            _spawned.Add(root);
            root.transform.position = new Vector3(1f, 0f, 0f);
            CastleDoor door = root.AddComponent<CastleDoor>();
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.transform.SetParent(root.transform, false);
            slab.transform.localPosition = new Vector3(0f, 1.5f, 0.5f);
            slab.transform.localScale = new Vector3(0.2f, 3f, 1f);
            Physics.SyncTransforms();
            return door;
        }

        private float WalkInto(CastleDoor door)
        {
            var guard = new GameObject("Guard");
            _spawned.Add(guard);
            guard.transform.position = new Vector3(0f, 0f, 0.5f);
            var mover = new GuardMover(guard.transform, 0.3f, 1.8f);
            var tuning = new GuardNavigationTuning();
            var stepper = new GuardMoverStepper(tuning, new GuardSweep(tuning, new HashSet<Transform>()));
            for (int i = 0; i < 10; i++)
                stepper.Move(mover, Vector3.right * 0.5f, null, 0.02f);
            return guard.transform.position.x;
        }
    }
}
