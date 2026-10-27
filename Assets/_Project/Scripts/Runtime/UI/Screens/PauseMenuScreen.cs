using Code.Scripts.EventSystems;
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
        private const float TitleHeight = 140f;

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

            // Two lines at 1.1 em: Eczar's own line box is 1.77 em, which spaces them like two paragraphs.
            _title = UIFactory.CreateText(panel, "Title", RaidTitle, UITheme.Heading, UITheme.Text, TextAnchor.MiddleLeft, UIFonts.Display);
            _title.lineSpacing = 0.62f;
            Text title = _title;
            float titleTop = PadTop + 24f + 16f + 18f;
            UIFactory.PlaceTopLeft(title.rectTransform, PadSide, titleTop, ContentWidth, TitleHeight);

            var sentence = UIFactory.CreateText(panel, "Sentence", RaidSentence,
                UITheme.Body, UITheme.TextDim, TextAnchor.UpperLeft, UIFonts.Body);
            _sentence = sentence;
            float sentenceTop = titleTop + TitleHeight + 16f;
            UIFactory.PlaceTopLeft(sentence.rectTransform, PadSide, sentenceTop, ContentWidth, 100f);

            var buttonListGo = new GameObject("ButtonList", typeof(RectTransform));
            buttonListGo.transform.SetParent(panel, false);
            var buttonListRect = (RectTransform)buttonListGo.transform;
            UIFactory.PlaceTopLeft(buttonListRect, PadSide, sentenceTop + 100f + 34f + 16f, ContentWidth, 64f * 4f + 16f * 3f);
            UIFactory.AddVerticalLayout(buttonListRect, 16f, new RectOffset(0, 0, 0, 0), TextAnchor.UpperLeft);

            var size = new Vector2(ContentWidth, 64f);
            UIFactory.CreateButton(buttonListRect, "ResumeButton", "Resume", OnResumeClicked, size, ButtonKind.Primary, "ESC");
            _invite = new PauseInviteSection(buttonListRect, size);
            UIFactory.CreateButton(buttonListRect, "SettingsButton", "Settings", OnSettingsClicked, size);
            UIFactory.CreateButton(buttonListRect, "QuitButton", "Quit to Main Menu", OnQuitClicked, size, ButtonKind.Quiet);
        }

        private const string RaidTitle = "The raid goes on";
        private const string RaidSentence = "The castle does not wait. Your friends and the guards keep moving while this is open.";
        private const string LairTitle = "The Lair waits";
        private const string LairSentence = "Nothing hunts you here. Invite a friend, change a setting, or go back to the room.";

        private Text _title;
        private Text _sentence;
        private PauseInviteSection _invite;

        /// <summary>The words follow where the pause came from, and the Invite button follows the session.</summary>
        protected override void OnShown()
        {
            bool inLair = GameServices.GameState.PausedFrom == GameState.LairRoom;
            _title.text = inLair ? LairTitle : RaidTitle;
            _sentence.text = inLair ? LairSentence : RaidSentence;

            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            EventManager.Instance?.Subscribe(this, (CoopChanged e) => _invite.Refresh());
            _invite.Refresh();
        }

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private void OnResumeClicked() => GameServices.GameState.ChangeState(GameServices.GameState.PausedFrom);

        private void OnSettingsClicked() => GameServices.GameState.ChangeState(GameState.Settings);

        private void OnQuitClicked()
        {
            GameServices.Coop?.Leave();
            GameServices.GameState.ChangeState(GameState.MainMenu);
        }
    }
}
