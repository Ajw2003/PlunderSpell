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
        /// <summary>Seconds the book stays open by itself the first time a raid starts, so the keys are learnt.</summary>
        public const float FirstRaidSeconds = 8f;

        private const string SeenKey = "Hud.GrimoireSeen";

        private bool _watchUp;
        private bool _grimoireOpen;
        private float _autoOpenUntil = float.NegativeInfinity;

        private void OnEnable() =>
            EventManager.Instance?.Subscribe(this, (GameStateChanged e) => OnGameStateChanged(e.Current));

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.Playing)
            {
                _autoOpenUntil = float.NegativeInfinity; // leaving the raid, or pausing, ends the first-raid opening
                return;
            }
            if (PlayerPrefs.GetInt(SeenKey, 0) == 1)
                return;
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            _autoOpenUntil = Time.time + FirstRaidSeconds;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            bool playing = keyboard != null && GameServices.IsPlaying;
            SetWatch(playing && keyboard[Key.T].isPressed);

            // Reading takes both hands, so the book will not open while something is carried.
            bool wanted = playing && (keyboard[Key.Tab].isPressed || Time.time < _autoOpenUntil);
            bool carrying = ItemManager.Instance != null && ItemManager.Instance.CarriedItem != null;
            SetGrimoire(wanted && !carrying);
        }

        private void OnDisable()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            SetWatch(false);
            SetGrimoire(false);
        }

        private void SetGrimoire(bool open)
        {
            if (open == _grimoireOpen)
                return;
            _grimoireOpen = open;
            EventManager.Instance?.Publish(new GrimoireOpened(open));
        }

        private void SetWatch(bool up)
        {
            if (up == _watchUp)
                return;
            _watchUp = up;
            EventManager.Instance?.Publish(new WatchRaised(up));
        }
    }
}
