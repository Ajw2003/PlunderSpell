using Code.Scripts.EventSystems;
using Plunderspell.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Plunderspell.UI
{
    /// <summary>
    /// Reads the keys that raise HUD panels while held, and publishes a change when one goes down or up. Input has
    /// to be read each frame; everything that reacts to it listens for the event instead (#324). Only while playing:
    /// a panel never stays up through a menu.
    /// </summary>
    public sealed class HudHoldKeys : MonoBehaviour
    {
        private bool _watchUp;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            bool playing = keyboard != null && GameServices.IsPlaying;
            SetWatch(playing && keyboard[Key.T].isPressed);
        }

        private void OnDisable() => SetWatch(false);

        private void SetWatch(bool up)
        {
            if (up == _watchUp)
                return;
            _watchUp = up;
            EventManager.Instance?.Publish(new WatchRaised(up));
        }
    }
}
