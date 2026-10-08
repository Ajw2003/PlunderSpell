using Plunderspell.Core;
using Code.Scripts.EventSystems;
using System.Collections;
using NUnit.Framework;
using Plunderspell.UI;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// Issue #131: the main menu and the Lair have no camera, and the Editor's Game view said "No
    /// cameras rendering" under a working menu. The backdrop camera renders exactly when nothing
    /// else does.
    /// </summary>
    public class BackdropCameraTests
    {
        private GameObject _backdrop;
        private GameObject _player;

        private readonly System.Collections.Generic.List<BackdropCamera> _stoodDown = new System.Collections.Generic.List<BackdropCamera>();

        // The menu owns a backdrop camera too. Two backdrops each count the other as a camera, so these tests stand
        // the menu's down while they run and give it back (and have it look again) afterwards.
        private void StandDownOthers(BackdropCamera mine)
        {
            foreach (BackdropCamera other in Object.FindObjectsByType<BackdropCamera>(FindObjectsSortMode.None))
            {
                if (other != mine && other.enabled)
                {
                    other.enabled = false;
                    _stoodDown.Add(other);
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (BackdropCamera other in _stoodDown)
            {
                if (other != null)
                {
                    other.enabled = true;
                    other.RecheckSoon();
                }
            }
            _stoodDown.Clear();
            if (_backdrop != null)
                Object.Destroy(_backdrop);
            if (_player != null)
                Object.Destroy(_player);
        }

        [UnityTest]
        public IEnumerator Test_TheBackdropRendersOnlyWhileNoOtherCameraDoes()
        {
            _backdrop = new GameObject("Backdrop");
            var backdrop = _backdrop.AddComponent<BackdropCamera>();
            StandDownOthers(backdrop);
            yield return null;

            bool othersBefore = Camera.allCamerasCount > (backdrop.IsRendering ? 1 : 0);
            Assert.AreEqual(!othersBefore, backdrop.IsRendering,
                "With no other camera the backdrop must render; with one, it must not.");

            _player = new GameObject("PlayerEye");
            var eye = _player.AddComponent<Camera>();
            // The backdrop no longer counts cameras every frame: it looks when the game changes mode or a scene loads.
            EventManager.Instance.Publish(new GameStateChanged(GameState.Lair, GameState.Playing));
            yield return null;
            yield return null;
            Assert.IsFalse(backdrop.IsRendering, "A player camera exists: the backdrop must step aside.");

            eye.enabled = false;
            EventManager.Instance.Publish(new GameStateChanged(GameState.Playing, GameState.GameOver));
            yield return null;
            yield return null;
            Assert.AreEqual(!othersBefore, backdrop.IsRendering,
                "With the player camera gone, the backdrop must come back.");
        }

        [UnityTest]
        public IEnumerator Test_TheBackdropDoesNotCountCamerasWithoutAnEvent()
        {
            _backdrop = new GameObject("Backdrop");
            var backdrop = _backdrop.AddComponent<BackdropCamera>();
            StandDownOthers(backdrop);
            yield return null;
            bool before = backdrop.IsRendering;

            _player = new GameObject("PlayerEye");
            _player.AddComponent<Camera>();
            yield return null;
            yield return null;

            Assert.AreEqual(before, backdrop.IsRendering, "Nothing announced a change, so nothing was looked at.");
            backdrop.enabled = false;
            Assert.AreEqual(0, EventManager.Instance.SubscriptionCount(backdrop), "A disabled backdrop leaves the bus.");
        }

        [UnityTest]
        public IEnumerator Test_TheMenuHasACameraRendering()
        {
            // UIBootstrapper creates the one UIRoot before any scene loads.
            yield return null;
            var uiRoot = Object.FindFirstObjectByType<UIRoot>();
            Assert.IsNotNull(uiRoot, "UIRoot was not bootstrapped.");
            Assert.IsNotNull(uiRoot.GetComponentInChildren<BackdropCamera>(), "UIRoot must own a backdrop camera.");
            Assert.Greater(Camera.allCamerasCount, 0, "The menu must never leave the screen with no camera.");
        }
    }
}
