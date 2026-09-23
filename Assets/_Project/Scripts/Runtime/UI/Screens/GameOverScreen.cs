using Plunderspell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    public class GameOverScreen : UIScreen
    {
        private Text _titleText;
        private Text _summaryText;
        private Text _buttonLabel;
        private bool _isVictory;

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

            var returnButton = UIFactory.CreateButton(transform, "ReturnButton", "Back to the Lair", OnReturnClicked, new Vector2(320f, 56f));
            _buttonLabel = returnButton.GetComponentInChildren<Text>();
            var returnRect = returnButton.GetComponent<RectTransform>();
            returnRect.anchorMin = new Vector2(0.5f, 0.32f);
            returnRect.anchorMax = new Vector2(0.5f, 0.32f);
        }

        public void Configure(bool isVictory)
        {
            _isVictory = isVictory;
            _titleText.text = isVictory ? "EXTRACTION SUCCESSFUL" : "YOU DIED";
            _titleText.color = isVictory ? UITheme.Success : UITheme.Danger;
            _summaryText.text = isVictory
                ? $"Gold Plundered: {GameServices.PlayerStats.Gold}"
                : "The castle keeps everything you didn't carry out.";
            if (_buttonLabel != null)
                _buttonLabel.text = "Back to the Lair";
        }

        /// <summary>Both outcomes go back to the Lair: that is where the debt is paid and the next raid starts.</summary>
        private void OnReturnClicked() => GameServices.GameState.ChangeState(GameState.Lair);
    }
}
