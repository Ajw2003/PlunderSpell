using Plunderspell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    public class GameOverScreen : UIScreen
    {
        private Text _titleText;
        private Text _summaryText;

        protected override void OnBuild()
        {
            UIFactory.CreateFullStretchPanel(transform, "Overlay", new Color(0f, 0f, 0f, 0.85f));

            _titleText = UIFactory.CreateText(transform, "Title", "GAME OVER", UITheme.TitleFontSize, UITheme.Danger);
            _titleText.rectTransform.anchorMin = new Vector2(0.5f, 0.6f);
            _titleText.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
            _titleText.rectTransform.sizeDelta = new Vector2(900f, 100f);

            _summaryText = UIFactory.CreateText(transform, "Summary", string.Empty, UITheme.BodyFontSize, UITheme.TextSecondary);
            _summaryText.rectTransform.anchorMin = new Vector2(0.5f, 0.48f);
            _summaryText.rectTransform.anchorMax = new Vector2(0.5f, 0.48f);
            _summaryText.rectTransform.sizeDelta = new Vector2(700f, 60f);

            var returnButton = UIFactory.CreateButton(transform, "ReturnButton", "Return to Main Menu", OnReturnClicked, new Vector2(320f, 56f));
            var returnRect = returnButton.GetComponent<RectTransform>();
            returnRect.anchorMin = new Vector2(0.5f, 0.32f);
            returnRect.anchorMax = new Vector2(0.5f, 0.32f);
        }

        public void Configure(bool isVictory)
        {
            _titleText.text = isVictory ? "EXTRACTION SUCCESSFUL" : "GAME OVER";
            _titleText.color = isVictory ? UITheme.Success : UITheme.Danger;
            _summaryText.text = $"Gold Plundered: {GameServices.PlayerStats.Gold}";
        }

        private void OnReturnClicked() => GameServices.GameState.ChangeState(GameState.MainMenu);
    }
}
