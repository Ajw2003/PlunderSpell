using Plunderspell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    public class PauseMenuScreen : UIScreen
    {
        // The mockup's .pa-panel: a 640-wide panel down the right edge, padded 120 top and 80 sides.
        private const float PanelWidth = 640f;
        private const float PadTop = 120f;
        private const float PadSide = 80f;
        private const float ContentWidth = PanelWidth - PadSide * 2f;

        protected override void OnBuild()
        {
            // The raid does not pause, so the world stays visible: a fade rather than a full cover.
            var fade = UIFactory.CreateBackdrop(transform, "Overlay", UITextures.FadeLeftToRight);
            fade.raycastTarget = true;

            Color surface = UITheme.Surface;
            surface.a = 0.96f;
            var panel = UIFactory.CreatePanel(transform, "Panel", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(PanelWidth, 0f), Vector2.zero, surface);
            panel.pivot = new Vector2(1f, 0.5f);

            var edge = UIFactory.CreateImage(panel, "Edge", UITheme.Line);
            edge.rectTransform.anchorMin = new Vector2(0f, 0f);
            edge.rectTransform.anchorMax = new Vector2(0f, 1f);
            edge.rectTransform.pivot = new Vector2(0f, 0.5f);
            edge.rectTransform.sizeDelta = new Vector2(1f, 0f);
            edge.rectTransform.anchoredPosition = Vector2.zero;

            RectTransform eyebrow = UIFactory.CreateEyebrow(panel, "Eyebrow", "Paused for you only");
            UIFactory.PlaceTopLeft(eyebrow, PadSide, PadTop, ContentWidth, 24f);

            var title = UIFactory.CreateText(panel, "Title", "The raid goes on", UITheme.Heading, UITheme.Text, TextAnchor.MiddleLeft, UIFonts.Display);
            UIFactory.PlaceTopLeft(title.rectTransform, PadSide, PadTop + 24f + 16f + 18f, ContentWidth, 64f);

            var sentence = UIFactory.CreateText(panel, "Sentence",
                "The castle does not wait. Your friends and the guards keep moving while this is open.",
                UITheme.Body, UITheme.TextDim, TextAnchor.UpperLeft, UIFonts.Body);
            float sentenceTop = PadTop + 24f + 16f + 18f + 64f + 10f + 16f;
            UIFactory.PlaceTopLeft(sentence.rectTransform, PadSide, sentenceTop, ContentWidth, 100f);

            var buttonListGo = new GameObject("ButtonList", typeof(RectTransform));
            buttonListGo.transform.SetParent(panel, false);
            var buttonListRect = (RectTransform)buttonListGo.transform;
            UIFactory.PlaceTopLeft(buttonListRect, PadSide, sentenceTop + 100f + 34f + 16f, ContentWidth, 64f * 3f + 16f * 2f);
            UIFactory.AddVerticalLayout(buttonListRect, 16f, new RectOffset(0, 0, 0, 0), TextAnchor.UpperLeft);

            var size = new Vector2(ContentWidth, 64f);
            UIFactory.CreateButton(buttonListRect, "ResumeButton", "Resume", OnResumeClicked, size, ButtonKind.Primary, "ESC");
            UIFactory.CreateButton(buttonListRect, "SettingsButton", "Settings", OnSettingsClicked, size);
            UIFactory.CreateButton(buttonListRect, "QuitButton", "Quit to Main Menu", OnQuitClicked, size, ButtonKind.Quiet);
        }

        private void OnResumeClicked() => GameServices.GameState.ChangeState(GameState.Playing);

        private void OnSettingsClicked() => GameServices.GameState.ChangeState(GameState.Settings);

        private void OnQuitClicked() => GameServices.GameState.ChangeState(GameState.MainMenu);
    }
}
