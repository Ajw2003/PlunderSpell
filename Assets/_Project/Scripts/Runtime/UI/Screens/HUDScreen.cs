using Plunderspell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    public class HUDScreen : UIScreen
    {
        private Image _healthFill;
        private Text _healthText;
        private Image _manaFill;
        private Text _goldText;
        private GameObject _extractionGroup;
        private Image _extractionFill;
        private Text _extractionLabel;

        protected override void OnBuild()
        {
            var healthBar = UIFactory.CreateProgressBar(transform, "HealthBar", UITheme.Danger, new Vector2(280f, 26f), out _healthFill);
            // Bottom-left: the raid HUD owns the top-left corner (clock, alarm).
            SetAnchor(healthBar.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20f, 74f));
            _healthText = UIFactory.CreateText(healthBar.rectTransform, "HealthText", "", UITheme.SmallFontSize, UITheme.TextPrimary);
            _healthText.rectTransform.anchorMin = Vector2.zero;
            _healthText.rectTransform.anchorMax = Vector2.one;
            _healthText.rectTransform.offsetMin = Vector2.zero;
            _healthText.rectTransform.offsetMax = Vector2.zero;

            var manaBar = UIFactory.CreateProgressBar(transform, "ManaBar", UITheme.ManaColor, new Vector2(280f, 20f), out _manaFill);
            SetAnchor(manaBar.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20f, 48f));

            _goldText = UIFactory.CreateText(transform, "GoldText", "Gold: 0", UITheme.BodyFontSize, UITheme.Accent, TextAnchor.UpperRight);
            SetAnchor(_goldText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f));
            _goldText.rectTransform.sizeDelta = new Vector2(220f, 36f);

            var inventoryHint = UIFactory.CreateText(transform, "InventoryHint", "[TAB] Inventory   [ESC] Pause", UITheme.SmallFontSize, UITheme.TextSecondary, TextAnchor.LowerLeft);
            SetAnchor(inventoryHint.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20f, 12f));
            inventoryHint.rectTransform.sizeDelta = new Vector2(320f, 30f);

            _extractionGroup = new GameObject("ExtractionGroup", typeof(RectTransform));
            _extractionGroup.transform.SetParent(transform, false);
            var groupRect = (RectTransform)_extractionGroup.transform;
            groupRect.anchorMin = new Vector2(0.5f, 0f);
            groupRect.anchorMax = new Vector2(0.5f, 0f);
            groupRect.pivot = new Vector2(0.5f, 0f);
            groupRect.sizeDelta = new Vector2(420f, 70f);
            groupRect.anchoredPosition = new Vector2(0f, 40f);

            var extractionBar = UIFactory.CreateProgressBar(groupRect, "ExtractionBar", UITheme.Success, new Vector2(420f, 22f), out _extractionFill);
            extractionBar.rectTransform.anchoredPosition = Vector2.zero;

            _extractionLabel = UIFactory.CreateText(groupRect, "ExtractionLabel", "Extracting...", UITheme.SmallFontSize, UITheme.TextPrimary);
            _extractionLabel.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _extractionLabel.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _extractionLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            _extractionLabel.rectTransform.sizeDelta = new Vector2(420f, 30f);
            _extractionLabel.rectTransform.anchoredPosition = new Vector2(0f, 4f);

            _extractionGroup.SetActive(false);
        }

        protected override void OnShown()
        {
            RefreshStats();
            GameServices.PlayerStats.StatsChanged += RefreshStats;
            GameServices.Extraction.ExtractionStarted += OnExtractionStarted;
            GameServices.Extraction.ExtractionCancelled += OnExtractionEnded;
            GameServices.Extraction.ExtractionCompleted += OnExtractionEnded;
            GameServices.Extraction.ExtractionProgress += OnExtractionProgress;
        }

        private void OnDisable()
        {
            if (GameServices.PlayerStats != null)
            {
                GameServices.PlayerStats.StatsChanged -= RefreshStats;
            }

            if (GameServices.Extraction != null)
            {
                GameServices.Extraction.ExtractionStarted -= OnExtractionStarted;
                GameServices.Extraction.ExtractionCancelled -= OnExtractionEnded;
                GameServices.Extraction.ExtractionCompleted -= OnExtractionEnded;
                GameServices.Extraction.ExtractionProgress -= OnExtractionProgress;
            }
        }

        private void RefreshStats()
        {
            var stats = GameServices.PlayerStats;
            _healthFill.fillAmount = stats.MaxHealth == 0 ? 0f : (float)stats.Health / stats.MaxHealth;
            _healthText.text = $"{stats.Health} / {stats.MaxHealth}";
            _manaFill.fillAmount = stats.MaxMana == 0 ? 0f : (float)stats.Mana / stats.MaxMana;
            _goldText.text = $"Gold: {stats.Gold}";
        }

        private void OnExtractionStarted()
        {
            _extractionGroup.SetActive(true);
            _extractionFill.fillAmount = 0f;
        }

        private void OnExtractionEnded()
        {
            _extractionGroup.SetActive(false);
            _extractionFill.fillAmount = 0f;
        }

        private void OnExtractionProgress(float progress)
        {
            _extractionFill.fillAmount = progress;
            _extractionLabel.text =
                $"Extracting — {Mathf.Max(0f, GameServices.Extraction.RemainingSeconds):0.0}s  (stay on the pad)";
        }

        private static void SetAnchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
        }
    }
}
