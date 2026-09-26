using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.UI.Screens
{
    public class PauseMenuScreen : UIScreen
    {
        protected override void OnBuild()
        {
            UIFactory.CreateFullStretchPanel(transform, "Overlay", new Color(0f, 0f, 0f, 0.6f));
            var panel = UIFactory.CreatePanel(transform, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(480f, 380f), Vector2.zero, UITheme.PanelBackground);

            var title = UIFactory.CreateText(panel, "Title", "THE RAID GOES ON", UITheme.HeaderFontSize, UITheme.TextPrimary);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(420f, 60f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -24f);

            var buttonListGo = new GameObject("ButtonList", typeof(RectTransform));
            buttonListGo.transform.SetParent(panel, false);
            var buttonListRect = (RectTransform)buttonListGo.transform;
            buttonListRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonListRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonListRect.sizeDelta = new Vector2(360f, 220f);
            buttonListRect.anchoredPosition = new Vector2(0f, -30f);
            UIFactory.AddVerticalLayout(buttonListRect, 14f, new RectOffset(0, 0, 0, 0));

            UIFactory.CreateButton(buttonListRect, "ResumeButton", "Resume", OnResumeClicked, new Vector2(360f, 52f));
            UIFactory.CreateButton(buttonListRect, "SettingsButton", "Settings", OnSettingsClicked, new Vector2(360f, 52f));
            UIFactory.CreateButton(buttonListRect, "QuitButton", "Quit to Main Menu", OnQuitClicked, new Vector2(360f, 52f));
        }

        private void OnResumeClicked() => GameServices.GameState.ChangeState(GameState.Playing);

        private void OnSettingsClicked() => GameServices.GameState.ChangeState(GameState.Settings);

        private void OnQuitClicked() => GameServices.GameState.ChangeState(GameState.MainMenu);
    }
}
