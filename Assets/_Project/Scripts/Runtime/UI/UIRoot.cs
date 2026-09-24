using Plunderspell.Core;
using Plunderspell.UI.Screens;
using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>Owns the Canvas and every top-level screen, showing exactly the ones that match the current GameState.</summary>
    [DisallowMultipleComponent]
    public class UIRoot : MonoBehaviour
    {
        private MainMenuScreen _mainMenu;
        private LairScreen _lair;
        private PauseMenuScreen _pauseMenu;
        private HUDScreen _hud;
        private SettingsScreen _settings;
        private GameOverScreen _gameOver;

        private void Awake()
        {
            var canvas = UIFactory.CreateRootCanvas("Canvas", transform);
            var root = canvas.transform;

            _mainMenu = BuildScreen<MainMenuScreen>(root, "MainMenuScreen");
            _lair = BuildScreen<LairScreen>(root, "LairScreen");
            _pauseMenu = BuildScreen<PauseMenuScreen>(root, "PauseMenuScreen");
            _hud = BuildScreen<HUDScreen>(root, "HUDScreen");
            _settings = BuildScreen<SettingsScreen>(root, "SettingsScreen");
            _gameOver = BuildScreen<GameOverScreen>(root, "GameOverScreen");

            GameServices.GameState.StateChanged += OnStateChanged;
        }

        private void Start()
        {
            ApplyState(GameServices.GameState.CurrentState);
        }

        private void OnDestroy()
        {
            if (GameServices.GameState != null)
            {
                GameServices.GameState.StateChanged -= OnStateChanged;
            }
        }

        private static T BuildScreen<T>(Transform parent, string name) where T : UIScreen
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var screen = go.AddComponent<T>();
            screen.Build();
            return screen;
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            ApplyState(next);
        }

        private void ApplyState(GameState state)
        {
            _mainMenu.SetVisible(state == GameState.MainMenu);
            _lair.SetVisible(state == GameState.Lair);
            _pauseMenu.SetVisible(state == GameState.Paused);
            _hud.SetVisible(state == GameState.Playing || state == GameState.Paused);
            _settings.SetVisible(state == GameState.Settings);
            _gameOver.SetVisible(state == GameState.GameOver || state == GameState.Victory);

            if (state == GameState.GameOver || state == GameState.Victory)
            {
                _gameOver.Configure(isVictory: state == GameState.Victory);
            }
        }
    }
}
