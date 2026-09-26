using Plunderspell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    public class InventoryScreen : UIScreen
    {
        private RectTransform _grid;
        private Text _capacityText;

        protected override void OnBuild()
        {
            UIFactory.CreateFullStretchPanel(transform, "Overlay", new Color(0f, 0f, 0f, 0.7f));
            var panel = UIFactory.CreatePanel(transform, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(900f, 620f), Vector2.zero, UITheme.PanelBackground);

            var title = UIFactory.CreateText(panel, "Title", "INVENTORY", UITheme.HeaderFontSize, UITheme.TextPrimary, TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(0f, 1f);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.rectTransform.sizeDelta = new Vector2(400f, 50f);
            title.rectTransform.anchoredPosition = new Vector2(30f, -24f);

            _capacityText = UIFactory.CreateText(panel, "CapacityText", "0 / 0", UITheme.BodyFontSize, UITheme.TextSecondary, TextAnchor.MiddleRight);
            _capacityText.rectTransform.anchorMin = new Vector2(1f, 1f);
            _capacityText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _capacityText.rectTransform.pivot = new Vector2(1f, 1f);
            _capacityText.rectTransform.sizeDelta = new Vector2(200f, 50f);
            _capacityText.rectTransform.anchoredPosition = new Vector2(-30f, -24f);

            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(panel, false);
            _grid = (RectTransform)gridGo.transform;
            _grid.anchorMin = Vector2.zero;
            _grid.anchorMax = Vector2.one;
            _grid.offsetMin = new Vector2(30f, 90f);
            _grid.offsetMax = new Vector2(-30f, -90f);
            UIFactory.AddGridLayout(_grid, new Vector2(120f, 120f), new Vector2(12f, 12f), new RectOffset(0, 0, 0, 0));

            var buttonRowGo = new GameObject("ButtonRow", typeof(RectTransform));
            buttonRowGo.transform.SetParent(panel, false);
            var buttonRowRect = (RectTransform)buttonRowGo.transform;
            buttonRowRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRowRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRowRect.pivot = new Vector2(0.5f, 0f);
            buttonRowRect.sizeDelta = new Vector2(500f, 56f);
            buttonRowRect.anchoredPosition = new Vector2(0f, 20f);

            var horizontalLayout = buttonRowGo.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.spacing = 16f;
            horizontalLayout.childControlWidth = true;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childAlignment = TextAnchor.MiddleCenter;

            UIFactory.CreateButton(buttonRowRect, "ExtractButton", "Begin Extraction", OnExtractClicked, new Vector2(220f, 52f));
            UIFactory.CreateButton(buttonRowRect, "CloseButton", "Close", OnCloseClicked, new Vector2(160f, 52f));
        }

        protected override void OnShown()
        {
            RefreshGrid();
            GameServices.Inventory.InventoryChanged += RefreshGrid;
        }

        private void OnDisable()
        {
            if (GameServices.Inventory != null)
            {
                GameServices.Inventory.InventoryChanged -= RefreshGrid;
            }
        }

        private void RefreshGrid()
        {
            for (int i = _grid.childCount - 1; i >= 0; i--)
            {
                Destroy(_grid.GetChild(i).gameObject);
            }

            var inventory = GameServices.Inventory;
            foreach (var stack in inventory.Items)
            {
                var cell = UIFactory.CreatePanel(_grid, "ItemCell", Vector2.zero, Vector2.zero, new Vector2(120f, 120f), Vector2.zero, UITheme.Background);
                var label = UIFactory.CreateText(cell, "Label", $"{stack.Definition.ItemName}\nx{stack.Count}", UITheme.SmallFontSize, UITheme.TextPrimary);
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
            }

            _capacityText.text = $"{inventory.Items.Count} / {inventory.Capacity}";
        }

        private void OnExtractClicked()
        {
            GameServices.Extraction.StartExtraction();
            GameServices.GameState.ChangeState(GameState.Playing);
        }

        private void OnCloseClicked() => GameServices.GameState.ChangeState(GameState.Playing);
    }
}
