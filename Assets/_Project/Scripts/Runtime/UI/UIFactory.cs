using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Plunderspell.UI
{
    /// <summary>Builds themed UGUI elements from code so every screen is constructed the same way.</summary>
    public static class UIFactory
    {
        public static Canvas CreateRootCanvas(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static RectTransform CreateFullStretchPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            go.GetComponent<Image>().color = color;
            return rect;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;

            go.GetComponent<Image>().color = color;
            return rect;
        }

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label, UnityAction onClick, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = sizeDelta;

            go.GetComponent<Image>().color = UITheme.Accent;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = UITheme.Accent;
            colors.highlightedColor = Color.Lerp(UITheme.Accent, Color.white, 0.2f);
            colors.pressedColor = UITheme.AccentDark;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            var labelText = CreateText(rect, "Label", label, UITheme.BodyFontSize, Color.black);
            var labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        public static Slider CreateSlider(Transform parent, string name, float minValue, float maxValue, float value, UnityAction<float> onValueChanged, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rootRect = (RectTransform)go.transform;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = sizeDelta;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(go.transform, false);
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = UITheme.PanelBackground;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillAreaRect, false);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().color = UITheme.Accent;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = Vector2.zero;
            handleAreaRect.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleAreaRect, false);
            var handleRect = (RectTransform)handle.transform;
            handleRect.sizeDelta = new Vector2(16f, 0f);
            handle.GetComponent<Image>().color = UITheme.TextPrimary;

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = value;

            if (onValueChanged != null)
            {
                slider.onValueChanged.AddListener(onValueChanged);
            }

            return slider;
        }

        /// <summary>Creates a background+fill progress bar. The background Image is returned; the fill Image comes back via <paramref name="fillImage"/>.</summary>
        public static Image CreateProgressBar(Transform parent, string name, Color fillColor, Vector2 sizeDelta, out Image fillImage)
        {
            var background = new GameObject(name, typeof(RectTransform), typeof(Image));
            background.transform.SetParent(parent, false);
            var backgroundRect = (RectTransform)background.transform;
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.pivot = new Vector2(0.5f, 0.5f);
            backgroundRect.sizeDelta = sizeDelta;
            var backgroundImage = background.GetComponent<Image>();
            backgroundImage.color = UITheme.PanelBackground;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(backgroundRect, false);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            fillImage = fill.GetComponent<Image>();
            fillImage.color = fillColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;

            return backgroundImage;
        }

        public static VerticalLayoutGroup AddVerticalLayout(RectTransform target, float spacing, RectOffset padding, TextAnchor childAlignment = TextAnchor.UpperCenter)
        {
            var layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = padding;
            layout.childAlignment = childAlignment;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static GridLayoutGroup AddGridLayout(RectTransform target, Vector2 cellSize, Vector2 spacing, RectOffset padding)
        {
            var layout = target.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = cellSize;
            layout.spacing = spacing;
            layout.padding = padding;
            return layout;
        }
    }
}
