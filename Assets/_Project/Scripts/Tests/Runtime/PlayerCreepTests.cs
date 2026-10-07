using System.Reflection;
using NUnit.Framework;
using Plunderspell.Acoustics;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>The hold-to-creep pace (#238): slow enough that the footstep bands call it a crouch (1.5 m noise).</summary>
    public class PlayerCreepTests
    {
        private GameObject _player;

        [TearDown]
        public void TearDown()
        {
            if (_player != null)
                Object.DestroyImmediate(_player);
        }

        private float HorizontalSpeedWhenWalking(bool creeping)
        {
            _player = new GameObject("CreepPlayer");
            _player.AddComponent<CapsuleCollider>();
            _player.AddComponent<Rigidbody>();
            var machine = _player.AddComponent<PlayerStateMachine>();
            var eye = new GameObject("Eye");
            eye.transform.SetParent(_player.transform, false);
            machine.CameraTransform = eye.transform;
            machine.WalkSpeed = 5f;   // RaidPlayer.prefab
            machine.Creeping = creeping;
            machine.MovementDirection = Vector2.up;

            machine.WalkState.Update();
            Vector3 velocity = _player.GetComponent<Rigidbody>().linearVelocity;
            return new Vector2(velocity.x, velocity.z).magnitude;
        }

        [Test]
        public void HoldingCreepKeepsTheSpeedUnderTheCrouchBandAndTheFootstepIsTheQuietOne()
        {
            float speed = HorizontalSpeedWhenWalking(true);
            Assert.That(speed, Is.LessThan(FootstepNoiseEmitter.CrouchBelowSpeed));
            Assert.That(speed, Is.GreaterThan(0.5f), "creeping is slow, not standing still");
            Assert.That(FootstepNoiseEmitter.StanceForSpeed(speed), Is.EqualTo(MoveStance.Crouch));
            Assert.That(CrouchRadiusOfAFreshEmitter(), Is.EqualTo(1.5f));
        }

        [Test]
        public void WithoutCreepTheWalkIsTheLoudRunFootstep()
        {
            float speed = HorizontalSpeedWhenWalking(false);
            Assert.That(speed, Is.EqualTo(5f).Within(0.01f));
            Assert.That(FootstepNoiseEmitter.StanceForSpeed(speed), Is.EqualTo(MoveStance.Run));
        }

        // The crouch reach is the emitter's serialized default; read it so a change of that number shows here.
        private static float CrouchRadiusOfAFreshEmitter()
        {
            var holder = new GameObject("Emitter");
            try
            {
                var emitter = holder.AddComponent<FootstepNoiseEmitter>();
                return (float)typeof(FootstepNoiseEmitter).GetField("_crouchRadius", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(emitter);
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }
    }
}
