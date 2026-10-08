using Code.Scripts.EventSystems;
using Plunderspell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    public class HUDScreen : UIScreen
    {
        // The mockup's .rd-bl and .rd-vital: a 420-wide block in the bottom-left corner, labels in a
        // 100 column, the value in a 70 column, 14 between.
        private const float Left = 36f;
        private const float BlockWidth = 420f;
        private const float RowHeight = 29f;
        private const float LabelWidth = 100f;
        private const float ValueWidth = 70f;
        private const float ColumnGap = 14f;
        private const float HintHeight = 21f;
        private const float LowHealth = 0.25f;

        private Image _healthFill;
        private Text _healthText;
        private Image _manaFill;
        private Text _manaText;
        private GameObject _extractionGroup;
        private Image _extractionFill;
        private Text _extractionLabel;

        protected override void OnBuild()
        {
            // Bottom-left: the raid HUD owns the top-left corner (clock, alarm). Every spell spends
            // mana (SpellWord.ManaCost), so the pool sits under health.
            float manaBottom = 34f + HintHeight + 6f + 10f;
            float healthBottom = manaBottom + RowHeight + 10f;
            _healthText = BuildVital("Health", "HEALTH", 14f, UITheme.Danger, healthBottom, out _healthFill);
            _manaText = BuildVital("Mana", "MANA", 10f, UITheme.Voice, manaBottom, out _manaFill);

            // No gold counter: the raid HUD shows debt, banked and haul, and nothing adds to
            // PlayerStats.Gold. No Tab hint: the inventory it opened is gone (#132, #137).
            BuildMenuHint();

            _extractionGroup = new GameObject("ExtractionGroup", typeof(RectTransform));
            _extractionGroup.transform.SetParent(transform, false);
            var groupRect = (RectTransform)_extractionGroup.transform;
            // Above the caption and microphone meter the raid HUD draws in the bottom-centre.
            UIFactory.PlaceBottomCentre(groupRect, 120f, 420f, 44f);

            var extractionBar = UIFactory.CreateProgressBar(groupRect, "ExtractionBar", UITheme.Interactive, new Vector2(420f, 14f), out _extractionFill);
            UIFactory.PlaceBottomLeft(extractionBar.rectTransform, 0f, 0f, 420f, 14f);

            _extractionLabel = UIFactory.CreateText(groupRect, "ExtractionLabel", string.Empty, UITheme.Label - 1, UITheme.TextDim,
                TextAnchor.LowerCenter, UIFonts.Mono);
            UIFactory.PlaceTopLeft(_extractionLabel.rectTransform, 0f, 0f, 420f, 24f);

            _extractionGroup.SetActive(false);
        }

        /// <summary>One vital: a mono label, a bar with quarter ticks, and a right-aligned figure.</summary>
        private Text BuildVital(string name, string label, float barHeight, Color fill, float bottom, out Image fillImage)
        {
            var rowGo = new GameObject(name + "Row", typeof(RectTransform));
            rowGo.transform.SetParent(transform, false);
            var row = (RectTransform)rowGo.transform;
            UIFactory.PlaceBottomLeft(row, Left, bottom, BlockWidth, RowHeight);

            var labelText = UIFactory.CreateText(row, name + "Label", UITheme.Tracked(label, 14), 14, UITheme.TextFaint, TextAnchor.MiddleLeft, UIFonts.Mono);
            UIFactory.PlaceTopLeft(labelText.rectTransform, 0f, 0f, LabelWidth, RowHeight);

            float barWidth = BlockWidth - LabelWidth - ValueWidth - ColumnGap * 2f;
            var bar = UIFactory.CreateProgressBar(row, name + "Bar", fill, new Vector2(barWidth, barHeight), out fillImage, quarterTicks: true);
            UIFactory.PlaceTopLeft(bar.rectTransform, LabelWidth + ColumnGap, (RowHeight - barHeight) * 0.5f, barWidth, barHeight);

            var value = UIFactory.CreateText(row, name + "Text", string.Empty, 18, UITheme.Text, TextAnchor.MiddleRight, UIFonts.Mono);
            UIFactory.PlaceTopLeft(value.rectTransform, BlockWidth - ValueWidth, 0f, ValueWidth, RowHeight);
            return value;
        }

        private void BuildMenuHint()
        {
            var hintGo = new GameObject("MenuHint", typeof(RectTransform));
            hintGo.transform.SetParent(transform, false);
            var hint = (RectTransform)hintGo.transform;
            UIFactory.PlaceBottomLeft(hint, Left, 34f, BlockWidth, HintHeight);

            RectTransform key = UIFactory.CreateKeyBox(hint, "Key", "ESC", 13, UITheme.TextFaint, UITheme.Text);
            UIFactory.PlaceTopLeft(key, 0f, 0f, key.sizeDelta.x, HintHeight);

            var label = UIFactory.CreateText(hint, "Label", UITheme.Tracked("Menu", 13), 13, UITheme.TextDim, TextAnchor.MiddleLeft, UIFonts.Mono);
            UIFactory.PlaceTopLeft(label.rectTransform, key.sizeDelta.x + 6f, 0f, 200f, HintHeight);
        }

        protected override void OnShown()
        {
            RefreshStats();
            // Hidden, this screen hears no extraction events, so a countdown that ended meanwhile (death, the raid ending)
            // left the bar at its last reading, e.g. "LEAVING IN 1.0S". Read the countdown itself instead.
            _extractionGroup.SetActive(GameServices.Extraction != null && GameServices.Extraction.IsExtracting);
            EventManager bus = EventManager.Instance;
            if (bus == null)
                return;

            bus.UnsubscribeFromAllEvents(this);
            bus.Subscribe(this, (PlayerStatsChanged e) => RefreshStats());
            bus.Subscribe(this, (ExtractionStarted e) => OnExtractionStarted());
            bus.Subscribe(this, (ExtractionCancelled e) => OnExtractionEnded());
            bus.Subscribe(this, (ExtractionCompleted e) => OnExtractionEnded());
            bus.Subscribe(this, (ExtractionProgress e) => OnExtractionProgress(e.Progress));
        }

        private void OnDisable()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
        }

        private void RefreshStats()
        {
            var stats = GameServices.PlayerStats;
            float health = stats.MaxHealth == 0 ? 0f : (float)stats.Health / stats.MaxHealth;
            UIFactory.SetBarFill(_healthFill, health);
            _healthText.text = stats.Health.ToString();
            // The figure joins the bar in warning once health is low.
            _healthText.color = health < LowHealth ? UITheme.Danger : UITheme.Text;

            UIFactory.SetBarFill(_manaFill, stats.MaxMana == 0 ? 0f : (float)stats.Mana / stats.MaxMana);
            _manaText.text = stats.Mana.ToString();
        }

        private void OnExtractionStarted()
        {
            _extractionGroup.SetActive(true);
            UIFactory.SetBarFill(_extractionFill, 0f);
        }

        private void OnExtractionEnded()
        {
            _extractionGroup.SetActive(false);
            UIFactory.SetBarFill(_extractionFill, 0f);
        }

        private void OnExtractionProgress(float progress)
        {
            UIFactory.SetBarFill(_extractionFill, progress);
            _extractionLabel.text =
                $"LEAVING IN {Mathf.Max(0f, GameServices.Extraction.RemainingSeconds):0.0}S · STAY IN THE PORTAL";
        }
    }
}
