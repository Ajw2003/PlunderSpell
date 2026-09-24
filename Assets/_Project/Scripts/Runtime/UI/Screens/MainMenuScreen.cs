using Plunderspell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    public class MainMenuScreen : UIScreen
    {
        protected override void OnBuild()
        {
            UIFactory.CreateFullStretchPanel(transform, "Background", UITheme.Background);

            var title = UIFactory.CreateText(transform, "Title", "PLUNDERSPELL", UITheme.TitleFontSize, UITheme.Accent);
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.75f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.75f);
            title.rectTransform.sizeDelta = new Vector2(900f, 120f);

            var subtitle = UIFactory.CreateText(transform, "Subtitle", "A Rogue Extraction of Plunder & Spellcraft", UITheme.BodyFontSize, UITheme.TextSecondary);
            subtitle.rectTransform.anchorMin = new Vector2(0.5f, 0.68f);
            subtitle.rectTransform.anchorMax = new Vector2(0.5f, 0.68f);
            subtitle.rectTransform.sizeDelta = new Vector2(900f, 40f);

            var buttonListGo = new GameObject("ButtonList", typeof(RectTransform));
            buttonListGo.transform.SetParent(transform, false);
            var buttonListRect = (RectTransform)buttonListGo.transform;
            buttonListRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonListRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonListRect.sizeDelta = new Vector2(420f, 330f);
            buttonListRect.anchoredPosition = new Vector2(0f, -40f);
            UIFactory.AddVerticalLayout(buttonListRect, spacing: 16f, padding: new RectOffset(0, 0, 0, 0));

            UIFactory.CreateButton(buttonListRect, "PlayButton", "Play Solo", OnPlayClicked, new Vector2(420f, 56f));
            UIFactory.CreateButton(buttonListRect, "HostButton", "Host Co-op", OnHostClicked, new Vector2(420f, 56f));
            UIFactory.CreateButton(buttonListRect, "SettingsButton", "Settings", OnSettingsClicked, new Vector2(420f, 56f));
            UIFactory.CreateButton(buttonListRect, "QuitButton", "Quit", OnQuitClicked, new Vector2(420f, 56f));

            _status = UIFactory.CreateText(transform, "CoopStatus", "", UITheme.SmallFontSize, UITheme.TextSecondary);
            _status.rectTransform.anchorMin = new Vector2(0.5f, 0.12f);
            _status.rectTransform.anchorMax = new Vector2(0.5f, 0.12f);
            _status.rectTransform.sizeDelta = new Vector2(1100f, 30f);

            var version = UIFactory.CreateText(transform, "VersionLabel", "v0.1.0 - Prototype", UITheme.SmallFontSize, UITheme.TextSecondary, TextAnchor.LowerRight);
            version.rectTransform.anchorMin = new Vector2(1f, 0f);
            version.rectTransform.anchorMax = new Vector2(1f, 0f);
            version.rectTransform.pivot = new Vector2(1f, 0f);
            version.rectTransform.sizeDelta = new Vector2(300f, 30f);
            version.rectTransform.anchoredPosition = new Vector2(-20f, 20f);
        }

        private Text _status;

        protected override void OnShown()
        {
            if (GameServices.Coop != null)
            {
                GameServices.Coop.Changed -= RefreshStatus;
                GameServices.Coop.Changed += RefreshStatus;
            }
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (_status != null)
                _status.text = GameServices.Coop != null ? GameServices.Coop.Status : string.Empty;
        }

        private void OnPlayClicked()
        {
            GameServices.Coop?.PlaySolo();
            GameServices.GameState.ChangeState(GameState.Lair);
        }

        /// <summary>The session moves to the Lair itself once hosting has started: over Steam that
        /// waits for the lobby to be created.</summary>
        private void OnHostClicked() => GameServices.Coop?.HostCoop();

        private void OnSettingsClicked() => GameServices.GameState.ChangeState(GameState.Settings);

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
