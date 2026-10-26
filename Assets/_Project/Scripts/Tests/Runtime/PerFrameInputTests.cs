using Code.Scripts.EventSystems;
using System.Collections;
using NUnit.Framework;
using Plunderspell.Core;
using StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>Issue #391: input handlers must not stack on idle, and mouse look must not scale with frame time.</summary>
    public class PerFrameInputTests : InputTestFixture
    {
        private GameObject _bus;
        private GameObject _player;
        private GameState _stateBefore;

        public override void Setup()
        {
            base.Setup();
            DropSharedActions();
            GameServices.Initialize();
            _stateBefore = GameServices.GameState.CurrentState;
            GameServices.GameState.ChangeState(GameState.Playing);
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            if (_player != null) Object.DestroyImmediate(_player);
            if (_bus != null) Object.DestroyImmediate(_bus);
            if (GameServices.GameState != null) GameServices.GameState.ChangeState(_stateBefore);
            DropSharedActions();
            base.TearDown();
        }

        // The shared actions outlive InputTestFixture's reset of the Input System, so an earlier test's copy
        // would be deaf to this test's keyboard. GameInput builds a fresh one on next use.
        private static void DropSharedActions() =>
            typeof(Player.GameInput).GetMethod("Dispose",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);

        private PlayerStateMachine MakePlayer()
        {
            _player = new GameObject("Player");
            _player.AddComponent<Rigidbody>();
            var machine = _player.AddComponent<PlayerStateMachine>();
            var eye = new GameObject("Eye");
            eye.transform.SetParent(_player.transform, false);
            machine.CameraTransform = eye.transform;
            return machine;
        }

        [UnityTest]
        public IEnumerator Test_RepeatedIdleEventsLeaveOneJumpHandler()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            _bus = new GameObject("Bus");
            _bus.AddComponent<EventManager>();
            MakePlayer();
            _player.AddComponent<Player.PlayerInputController>();
            yield return null;

            int jumps = 0;
            EventManager.Instance.Subscribe(this, (PlayerJumpEvent e) => { if (e.enable) jumps++; });
            for (int i = 0; i < 3; i++)
                EventManager.Instance.Publish(new PlayerIdleEvent());

            Press(keyboard.spaceKey);
            yield return null;

            Assert.AreEqual(1, jumps, "Each idle event stacked another jump handler (#391).");
        }

        [UnityTest]
        public IEnumerator Test_LookTurnIgnoresFrameTime()
        {
            PlayerStateMachine machine = MakePlayer();
            yield return null;

            Time.captureDeltaTime = 0.01f;
            yield return null;
            machine.Look(new Vector2(3f, 0f));
            float fastYaw = machine.CameraTransform.eulerAngles.y;

            machine.FaceYaw(0f);
            Time.captureDeltaTime = 0.1f;
            yield return null;
            machine.Look(new Vector2(3f, 0f));

            Assert.AreEqual(fastYaw, machine.CameraTransform.eulerAngles.y, 0.001f,
                "A mouse delta is already per frame; scaling it by deltaTime made turn speed depend on frame rate (#391).");
        }
    }
}
