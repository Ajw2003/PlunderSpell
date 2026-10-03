using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Guards;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// A chasing guard must not walk its body into a player and shove them through a wall (#200).
    /// The fresh guard (#214) has no body, so it is tested against a player-shaped dynamic body. The
    /// legacy second test (a guard driven into the player with the stop-short bypassed) went with the
    /// legacy body: there is nothing left to bypass, and GuardCombatTests covers a pinned player.
    /// </summary>
    public class GuardShoveTests
    {
        private readonly List<Object> _spawned = new List<Object>();
        private readonly GuardCoreRig _rig = new GuardCoreRig();

        [SetUp]
        public void SetUp() => _rig.SetUp();

        [TearDown]
        public void TearDown()
        {
            _rig.TearDown();
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

        [UnityTest]
        public IEnumerator Test_AChasingGuardDoesNotShoveAPlayerThroughAWall()
        {
            Assume.That(Application.isPlaying, "Needs physics and the navigation update, which only run in play mode.");

            var floor = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(30f, 0.2f, 30f);

            // A thin wall at x = 8, the player's back against it.
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(8.15f, 1.5f, 0f);
            wall.transform.localScale = new Vector3(0.3f, 3f, 10f);
            const float wallFace = 8f;

            var player = Track(new GameObject("Player"));
            player.transform.position = new Vector3(7.55f, 0.05f, 0f);
            var playerCollider = player.AddComponent<CapsuleCollider>();
            playerCollider.radius = 0.4f;
            playerCollider.height = 1.8f;
            playerCollider.center = new Vector3(0f, 0.9f, 0f);
            var body = player.AddComponent<Rigidbody>();
            body.mass = 1f;                 // as the player prefab
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            _rig.Director.RegisterIntruder(player.transform);

            // The fresh guard has no body: the director's navigation service moves it with a capsule sweep, so
            // it never has to be told how to avoid shoving. It still gets a collider, as a prefab has.
            Guard guard = _rig.MakeGuard(new Vector3(2f, 0f, 0f));
            GameObject guardGo = guard.gameObject;
            guardGo.transform.rotation = Quaternion.LookRotation(Vector3.right);
            var guardCollider = guardGo.AddComponent<CapsuleCollider>();
            guardCollider.radius = 0.39f;
            guardCollider.height = 1.8f;
            guardCollider.center = new Vector3(0f, 0.9f, 0f);
            Physics.SyncTransforms();

            float closest = float.MaxValue;
            float furthestPlayerX = float.MinValue;
            float end = Time.time + 8f;
            while (Time.time < end)
            {
                yield return new WaitForFixedUpdate();
                Vector3 apart = guardGo.transform.position - player.transform.position;
                apart.y = 0f;
                closest = Mathf.Min(closest, apart.magnitude);
                furthestPlayerX = Mathf.Max(furthestPlayerX, player.transform.position.x);
            }

            Assert.That(guard.State, Is.EqualTo(GuardAlertState.Chasing).Or.EqualTo(GuardAlertState.Combat),
                "Test premise: the guard went for the player.");
            Assert.LessOrEqual(furthestPlayerX + playerCollider.radius, wallFace + 0.05f,
                "The player stays on the near side of the wall.");
            Assert.GreaterOrEqual(closest, guardCollider.radius + playerCollider.radius - 0.05f,
                "The guard's body never overlaps the player's.");
        }

    }
}
