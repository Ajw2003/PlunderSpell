using System.Collections;
using NUnit.Framework;
using Plunderspell.Raid;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// The raid scene had no audio listener until PurrNet spawned this machine's player. The
    /// fallback listens exactly while no camera does, so there is always one listener and never two.
    /// </summary>
    public class RaidListenerFallbackTests
    {
        private GameObject _fallback;
        private GameObject _player;

        [TearDown]
        public void TearDown()
        {
            if (_fallback != null)
                Object.Destroy(_fallback);
            if (_player != null)
                Object.Destroy(_player);
        }

        [UnityTest]
        public IEnumerator Test_TheFallbackListensOnlyWhileNoCameraDoes()
        {
            _fallback = new GameObject("Fallback");
            var fallback = _fallback.AddComponent<RaidListenerFallback>();
            yield return null;
            Assert.IsTrue(fallback.IsListening, "No player yet: the fallback must be the listener.");

            _player = new GameObject("PlayerCamera");
            _player.AddComponent<Camera>();
            var ears = _player.AddComponent<AudioListener>();
            yield return null;
            Assert.IsFalse(fallback.IsListening, "The player camera listens: the fallback must step aside.");

            ears.enabled = false;
            yield return null;
            Assert.IsTrue(fallback.IsListening, "The player's listener is off: the fallback must take over.");
        }
    }
}
