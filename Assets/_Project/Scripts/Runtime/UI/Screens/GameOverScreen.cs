using Plunderspell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    public class GameOverScreen : UIScreen
    {
        // The mockup's .go block: left-aligned, 180 in and 300 down, 1100 wide.
        private const float Left = 180f;
        private const float Top = 300f;
        private const float Width = 1100f;
        private const float Gap = 18f;
        private const float Line = Top + 24f + Gap;
        // The 150 px figure's digits stand 102 px above the baseline; the rest is air.
        private const float FigureHeight = 112f;

        private RawImage _victoryBackdrop;
        private RawImage _defeatBackdrop;
        private Text _eyebrow;
        private Text _titleText;
        private Text _leadText;
        private RectTransform _figureRow;
        private Text _figureText;
        private Text _summaryText;
        private RectTransform _returnRect;
        private Text _buttonLabel;
        private bool _isVictory;

        protected override void OnBuild()
        {
            _victoryBackdrop = UIFactory.CreateBackdrop(transform);
            // The death screen's ground carries one madder glow instead of the lair's warm ones.
            _defeatBackdrop = UIFactory.CreateBackdrop(transform, "BackdropDefeat", UITextures.BackdropDefeat);

            RectTransform eyebrow = UIFactory.CreateEyebrow(transform, "Eyebrow", string.Empty);
            UIFactory.PlaceTopLeft(eyebrow, Left, Top, Width, 24f);
            _eyebrow = eyebrow.GetComponentInChildren<Text>();

            _titleText = UIFactory.CreateText(transform, "Title", "You died", 150, UITheme.Danger, TextAnchor.MiddleLeft, UIFonts.DisplayHeavy);
            UIFactory.PlaceTopLeft(_titleText.rectTransform, Left, Line, Width, 135f);

            _leadText = UIFactory.CreateText(transform, "Lead", "Home with", 40, UITheme.Text, TextAnchor.MiddleLeft, UIFonts.BodyLight);
            UIFactory.PlaceTopLeft(_leadText.rectTransform, Left, Line, Width, 56f);

            _figureRow = UIFactory.CreateFigureRow(transform, "Figure", UIFonts.DisplayHeavy, 150, UITheme.Value, "COIN", 26, 18f, FigureHeight, out _figureText);
            UIFactory.PlaceTopLeft(_figureRow, Left, Line + 56f + Gap, Width, FigureHeight);

            _summaryText = UIFactory.CreateText(transform, "Summary", string.Empty, 34, UITheme.TextDim, TextAnchor.UpperLeft, UIFonts.BodyLight);
            UIFactory.PlaceTopLeft(_summaryText.rectTransform, Left, Line + 135f + Gap, 900f, 100f);

            var returnButton = UIFactory.CreateButton(transform, "ReturnButton", "Back to the Lair", OnReturnClicked, new Vector2(440f, 64f), ButtonKind.Primary);
            _buttonLabel = returnButton.GetComponentInChildren<Text>();
            _returnRect = returnButton.GetComponent<RectTransform>();
        }

        public void Configure(bool isVictory)
        {
            _isVictory = isVictory;

            _victoryBackdrop.gameObject.SetActive(isVictory);
            _defeatBackdrop.gameObject.SetActive(!isVictory);
            _titleText.gameObject.SetActive(!isVictory);
            _summaryText.gameObject.SetActive(!isVictory);
            _leadText.gameObject.SetActive(isVictory);
            _figureRow.gameObject.SetActive(isVictory);

            _eyebrow.text = UITheme.Tracked(isVictory ? "Extracted · through the portal" : "Fallen", UITheme.Label);
            _eyebrow.color = isVictory ? UITheme.Interactive : UITheme.Danger;

            float buttonTop;
            if (isVictory)
            {
                _figureText.text = GameServices.PlayerStats.Gold.ToString("N0");
                buttonTop = Line + 56f + Gap + FigureHeight + Gap + 40f;
            }
            else
            {
                _summaryText.text = "The castle keeps everything you didn't carry out.";
                buttonTop = Line + 135f + Gap + 48f + Gap + 40f;
            }

            UIFactory.PlaceTopLeft(_returnRect, Left, buttonTop, 440f, 64f);
            if (_buttonLabel != null)
                _buttonLabel.text = "Back to the Lair";
        }

        /// <summary>Both outcomes go back to the Lair: that is where the debt is paid and the next raid starts.</summary>
        private void OnReturnClicked() => GameServices.GameState.ChangeState(GameState.Lair);
    }
}
