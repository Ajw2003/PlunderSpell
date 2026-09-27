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

        [TearDown]
        public void TearDown()
        {
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
            yield return null;

            bool othersBefore = Camera.allCamerasCount > (backdrop.IsRendering ? 1 : 0);
            Assert.AreEqual(!othersBefore, backdrop.IsRendering,
                "With no other camera the backdrop must render; with one, it must not.");

            _player = new GameObject("PlayerEye");
            var eye = _player.AddComponent<Camera>();
            yield return null;
            Assert.IsFalse(backdrop.IsRendering, "A player camera exists: the backdrop must step aside.");

            eye.enabled = false;
            yield return null;
            Assert.AreEqual(!othersBefore, backdrop.IsRendering,
                "With the player camera gone, the backdrop must come back.");
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
