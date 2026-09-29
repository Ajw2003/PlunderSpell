using Plunderspell.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Plunderspell.UI
{
    public enum ButtonKind
    {
        /// <summary>The one action a screen wants you to take: verdigris fill.</summary>
        Primary,
        Secondary,
        /// <summary>Back, Quit: no fill and no border.</summary>
        Quiet,
    }

    /// <summary>Builds themed UGUI elements from code so every screen is constructed the same way.</summary>
    public static class UIFactory
    {
        private const float ButtonBorder = 1f;
        private const float ButtonLabelInset = 28f;
        private const float QuietLabelInset = 4f;
        private const float ButtonKeyInset = 20f;
        private const float FocusMarkerWidth = 6f;

        // Primary's hover in the mockup: verdigris, lifted.
        private static readonly Color PrimaryHover = new Color32(0x6D, 0xB2, 0x97, 255);

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

        // --- Placement ------------------------------------------------------------------------------
        // Offsets are in canvas units from the named edge or corner of the parent, so a screen can be
        // laid out straight from the mockup's CSS numbers.

        public static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static void PlaceTopLeft(RectTransform rect, float left, float top, float width, float height) =>
            Place(rect, new Vector2(0f, 1f), new Vector2(left, -top), new Vector2(width, height));

        public static void PlaceTopRight(RectTransform rect, float right, float top, float width, float height) =>
            Place(rect, new Vector2(1f, 1f), new Vector2(-right, -top), new Vector2(width, height));

        public static void PlaceBottomLeft(RectTransform rect, float left, float bottom, float width, float height) =>
            Place(rect, new Vector2(0f, 0f), new Vector2(left, bottom), new Vector2(width, height));

        public static void PlaceBottomRight(RectTransform rect, float right, float bottom, float width, float height) =>
            Place(rect, new Vector2(1f, 0f), new Vector2(-right, bottom), new Vector2(width, height));

        public static void PlaceBottomCentre(RectTransform rect, float bottom, float width, float height) =>
            Place(rect, new Vector2(0.5f, 0f), new Vector2(0f, bottom), new Vector2(width, height));

        /// <summary>Spans the parent's width, hanging a fixed height below the top edge.</summary>
        public static void PlaceTopStretch(RectTransform rect, float left, float right, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Spans the parent's width, standing a fixed height above the bottom edge.</summary>
        public static void PlaceBottomStretch(RectTransform rect, float left, float right, float bottom, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        private static void Place(RectTransform rect, Vector2 corner, Vector2 position, Vector2 size)
        {
            rect.anchorMin = corner;
            rect.anchorMax = corner;
            rect.pivot = corner;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // --- Panels and images ------------------------------------------------------------------------

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

        /// <summary>A plain coloured rectangle, for rules, borders and fills. Ignores the pointer unless asked.</summary>
        public static Image CreateImage(Transform parent, string name, Color color, bool blocksPointer = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = blocksPointer;
            return image;
        }

        /// <summary>The full-screen ground behind a menu: bone-black with candle glows (see <see cref="UITextures.Backdrop"/>).</summary>
        public static RawImage CreateBackdrop(Transform parent) => CreateBackdrop(parent, "Backdrop", UITextures.Backdrop);

        public static RawImage CreateBackdrop(Transform parent, string name, Texture texture)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, 0f, 0f, 0f, 0f);

            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            return image;
        }

        /// <summary>
        /// A panel: the surface at 94% opacity inside a 1px Line border. Returns the inner rect to put
        /// content in; its parent is the outer rect to place and size.
        /// </summary>
        public static RectTransform CreateHairlinePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            var outer = CreatePanel(parent, name, anchorMin, anchorMax, sizeDelta, anchoredPosition, UITheme.Line);
            Color surface = UITheme.Surface;
            surface.a = 0.94f;
            Image inner = CreateImage(outer, "Surface", surface);
            Stretch(inner.rectTransform, 1f, 1f, 1f, 1f);
            return inner.rectTransform;
        }

        // --- Text ---------------------------------------------------------------------------------------

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor alignment = TextAnchor.MiddleCenter) =>
            CreateText(parent, name, content, fontSize, color, alignment, UIFonts.Body);

        public static Text CreateText(Transform parent, string name, string content, int fontSize, Color color, TextAnchor alignment, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<Text>();
            text.text = content;
            text.font = font != null ? font : UIFonts.Body;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // Labels never take clicks: a button's label must not shadow the button itself.
            text.raycastTarget = false;

            return text;
        }

        /// <summary>
        /// A small mono label, upper-cased and letter-spaced, with a 1px rule filling the rest of the
        /// row. The label is the child named "Label".
        /// </summary>
        public static RectTransform CreateEyebrow(Transform parent, string name, string text, bool withRule = true, Color? colour = null, bool ruleFirst = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(0f, 24f);

            var row = go.GetComponent<HorizontalLayoutGroup>();
            row.spacing = 16f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            Text label = CreateText(rect, "Label", UITheme.Tracked(text, UITheme.Label), UITheme.Label, colour ?? UITheme.TextFaint, TextAnchor.MiddleLeft, UIFonts.Mono);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            if (withRule)
            {
                Image rule = CreateImage(rect, "Rule", UITheme.Line);
                var layout = rule.gameObject.AddComponent<LayoutElement>();
                layout.minWidth = 0f;
                layout.preferredWidth = 0f;
                layout.flexibleWidth = 1f;
                layout.preferredHeight = 1f;
                if (ruleFirst)
                    rule.transform.SetSiblingIndex(0);
            }

            return rect;
        }

        /// <summary>A key drawn in a 1px box, like [E] or [ESC]. Sized to its text.</summary>
        public static RectTransform CreateKeyBox(Transform parent, string name, string key, int fontSize, Color boxColour, Color textColour)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;

            Text label = CreateText(rect, "Key", key, fontSize, textColour, TextAnchor.MiddleCenter, UIFonts.Mono);
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(label.rectTransform, 0f, 0f, 0f, 0f);

            float width = label.preferredWidth + 14f;
            float height = fontSize + 8f;
            rect.sizeDelta = new Vector2(width, height);

            Image top = CreateImage(rect, "Top", boxColour);
            PlaceTopStretch(top.rectTransform, 0f, 0f, 0f, 1f);
            Image bottom = CreateImage(rect, "Bottom", boxColour);
            PlaceBottomStretch(bottom.rectTransform, 0f, 0f, 0f, 1f);
            Image left = CreateImage(rect, "Left", boxColour);
            PlaceTopLeft(left.rectTransform, 0f, 0f, 1f, height);
            Image right = CreateImage(rect, "Right", boxColour);
            PlaceTopRight(right.rectTransform, 0f, 0f, 1f, height);
            return rect;
        }

        /// <summary>
        /// A big figure with its small mono unit beside it ("1,050 COIN"), sharing one baseline that sits
        /// on the bottom edge of the returned rect. The figure Text comes back through
        /// <paramref name="figure"/>.
        /// </summary>
        /// <remarks>
        /// A Text draws its line box, not its glyphs, and Eczar's line box is 1.77 em with 0.63 em of it
        /// below the baseline. So the row inside is pushed down by the figure's descent, and the unit is
        /// top-aligned in a box tall enough to put its baseline on the same line.
        /// </remarks>
        public static RectTransform CreateFigureRow(Transform parent, string name, Font figureFont, int figureSize, Color figureColour,
            string unit, int unitSize, float gap, float height, out Text figure)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(0f, height);

            LineMetrics(figureFont, figureSize, out float figureAscent, out float figureLine);
            LineMetrics(UIFonts.Mono, unitSize, out float unitAscent, out _);
            float figureDescent = figureLine - figureAscent;

            var rowGo = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(rect, false);
            var rowRect = (RectTransform)rowGo.transform;
            rowRect.anchorMin = new Vector2(0f, 0f);
            rowRect.anchorMax = new Vector2(1f, 0f);
            rowRect.pivot = new Vector2(0f, 0f);
            rowRect.sizeDelta = new Vector2(0f, figureLine);
            rowRect.anchoredPosition = new Vector2(0f, -figureDescent);

            var row = rowGo.GetComponent<HorizontalLayoutGroup>();
            row.spacing = gap;
            row.childAlignment = TextAnchor.LowerLeft;
            row.childControlWidth = true;
            row.childControlHeight = false;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            figure = CreateText(rowRect, "Figure", string.Empty, figureSize, figureColour, TextAnchor.LowerLeft, figureFont);
            figure.horizontalOverflow = HorizontalWrapMode.Overflow;
            figure.rectTransform.sizeDelta = new Vector2(0f, figureLine);

            Text unitText = CreateText(rowRect, "Unit", UITheme.Tracked(unit, unitSize), unitSize, UITheme.TextFaint, TextAnchor.UpperLeft, UIFonts.Mono);
            unitText.horizontalOverflow = HorizontalWrapMode.Overflow;
            unitText.rectTransform.sizeDelta = new Vector2(0f, figureDescent + unitAscent);
            return rect;
        }

        /// <summary>The ascent and the line-box height of a font at a size, in canvas units.</summary>
        public static void LineMetrics(Font font, int size, out float ascent, out float line)
        {
            font.RequestCharactersInTexture("0Hg", size);
            float scale = font.fontSize > 0 ? size / (float)font.fontSize : 1f;
            ascent = font.ascent * scale;
            line = font.lineHeight * scale;
            if (ascent <= 0f || line <= 0f)
            {
                ascent = size * 0.9f;
                line = size * 1.25f;
            }
        }

        // --- Buttons ---------------------------------------------------------------------------------------

        /// <summary>A button that leaves a screen plays the page-turned-backwards sound instead of the click.</summary>
        private static bool IsBackLabel(string label) =>
            label != null && (label.StartsWith("Back", System.StringComparison.OrdinalIgnoreCase)
                || label.StartsWith("Close", System.StringComparison.OrdinalIgnoreCase)
                || label.StartsWith("Cancel", System.StringComparison.OrdinalIgnoreCase)
                || label.StartsWith("Resume", System.StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// A button: a border, a fill inside it, the label (always the first Text child, so callers can
        /// relabel with GetComponentInChildren), an optional key hint, and a marker shown on hover and
        /// selection.
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string label, UnityAction onClick, Vector2 sizeDelta,
            ButtonKind kind = ButtonKind.Secondary, string keyHint = null, int fontSize = 0)
        {
            bool primary = kind == ButtonKind.Primary;
            bool quiet = kind == ButtonKind.Quiet;

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(UIButtonFocus));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = sizeDelta;

            // The root is the border. A Quiet button has none, but still needs a Graphic to catch the pointer.
            var border = go.GetComponent<Image>();
            Color borderNormal = primary ? UITheme.Interactive : quiet ? Color.clear : UITheme.Line;
            border.color = borderNormal;

            Image fill = CreateImage(rect, "Fill", Color.white);
            Stretch(fill.rectTransform, ButtonBorder, ButtonBorder, ButtonBorder, ButtonBorder);

            Color fillNormal = primary ? UITheme.Interactive : quiet ? Color.clear : UITheme.Surface;
            Color fillHover = primary ? PrimaryHover : quiet ? Color.clear : UITheme.SurfaceHi;
            Color fillPressed = primary ? Color.Lerp(UITheme.Interactive, UITheme.BoneBlack, 0.25f)
                : quiet ? Color.clear : Color.Lerp(UITheme.SurfaceHi, UITheme.BoneBlack, 0.3f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = fill;
            ColorBlock colors = button.colors;
            colors.normalColor = fillNormal;
            colors.highlightedColor = fillHover;
            colors.selectedColor = fillHover;
            colors.pressedColor = fillPressed;
            colors.disabledColor = new Color(fillNormal.r, fillNormal.g, fillNormal.b, fillNormal.a * 0.4f);
            button.colors = colors;

            string clickSound = IsBackLabel(label) ? SoundNames.UiBack : SoundNames.UiClick;
            button.onClick.AddListener(() => AudioDirector.PlayUi(clickSound));
            if (onClick != null)
                button.onClick.AddListener(onClick);

            Color labelNormal = primary ? UITheme.BoneBlack : quiet ? UITheme.TextDim : UITheme.Text;
            Color labelFocused = quiet ? UITheme.Interactive : labelNormal;
            float inset = quiet ? QuietLabelInset : ButtonLabelInset;
            int size = fontSize > 0 ? fontSize : quiet ? UITheme.Body + 1 : UITheme.Button;

            Text labelText = CreateText(rect, "Label", label, size, labelNormal, TextAnchor.MiddleLeft, UIFonts.Display);
            Stretch(labelText.rectTransform, inset, 0f, ButtonKeyInset, 0f);

            if (!string.IsNullOrEmpty(keyHint))
            {
                Text key = CreateText(rect, "Key", UITheme.Tracked(keyHint, 14), 14, primary ? UITheme.InteractiveLo : UITheme.TextFaint,
                    TextAnchor.MiddleRight, UIFonts.Mono);
                Stretch(key.rectTransform, inset, 0f, ButtonKeyInset, 0f);
            }

            Image marker = CreateImage(rect, "FocusMarker", primary ? UITheme.Text : UITheme.Interactive);
            marker.rectTransform.anchorMin = new Vector2(0f, 0f);
            marker.rectTransform.anchorMax = new Vector2(0f, 1f);
            marker.rectTransform.pivot = new Vector2(0f, 0.5f);
            marker.rectTransform.sizeDelta = new Vector2(FocusMarkerWidth, 0f);
            marker.rectTransform.anchoredPosition = Vector2.zero;
            marker.gameObject.SetActive(false);

            // A Secondary border turns verdigris on focus; Primary already is, and Quiet has none.
            Image focusBorder = kind == ButtonKind.Secondary ? border : null;
            go.GetComponent<UIButtonFocus>().Setup(marker.gameObject, focusBorder, borderNormal, UITheme.Interactive,
                quiet ? labelText : null, labelNormal, labelFocused);

            return button;
        }

        // --- Bars and sliders -----------------------------------------------------------------------------

        /// <summary>
        /// A bar: a 1px Line frame, a dark inside, a flat fill, and optionally quarter ticks. The frame Image
        /// is returned; the fill comes back via <paramref name="fillImage"/> and is set with <see cref="SetBarFill"/>.
        /// </summary>
        public static Image CreateProgressBar(Transform parent, string name, Color fillColor, Vector2 sizeDelta, out Image fillImage, bool quarterTicks = false)
        {
            var frame = new GameObject(name, typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(parent, false);
            var frameRect = (RectTransform)frame.transform;
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            frameRect.sizeDelta = sizeDelta;
            var frameImage = frame.GetComponent<Image>();
            frameImage.color = UITheme.Line;
            frameImage.raycastTarget = false;

            Color inside = UITheme.BoneBlack;
            inside.a = 0.75f;
            Image inner = CreateImage(frameRect, "Inner", inside);
            Stretch(inner.rectTransform, 1f, 1f, 1f, 1f);

            fillImage = CreateImage(inner.rectTransform, "Fill", fillColor);
            SetBarFill(fillImage, 1f);

            if (quarterTicks)
            {
                for (int i = 1; i <= 3; i++)
                {
                    Image tick = CreateImage(inner.rectTransform, "Tick" + i * 25, UITheme.Line);
                    float at = i * 0.25f;
                    tick.rectTransform.anchorMin = new Vector2(at, 0f);
                    tick.rectTransform.anchorMax = new Vector2(at, 1f);
                    tick.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    tick.rectTransform.sizeDelta = new Vector2(1f, 0f);
                    tick.rectTransform.anchoredPosition = Vector2.zero;
                }
            }

            return frameImage;
        }

        /// <summary>
        /// Sets how full a bar built by <see cref="CreateProgressBar"/> is. Sized by anchor rather than
        /// Image.fillAmount, which does nothing on an Image with no sprite.
        /// </summary>
        public static void SetBarFill(Image fill, float amount)
        {
            RectTransform rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>A 2px Line track, verdigris fill and a 10x20 vellum handle, in a row 28 high for the pointer to grab.</summary>
        public static Slider CreateSlider(Transform parent, string name, float minValue, float maxValue, float value, UnityAction<float> onValueChanged, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rootRect = (RectTransform)go.transform;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = sizeDelta;

            // Invisible, but it makes the whole row grabbable rather than the 2px track.
            go.GetComponent<Image>().color = Color.clear;

            Image track = CreateImage(rootRect, "Background", UITheme.Line);
            SetTrackRect(track.rectTransform);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            SetTrackRect((RectTransform)fillArea.transform);

            Image fill = CreateImage(fillArea.transform, "Fill", UITheme.Interactive);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Stretch((RectTransform)handleArea.transform, 0f, 0f, 0f, 0f);

            // White here; the slider's colour block carries the real colour, as Selectable multiplies the two.
            Image handle = CreateImage(handleArea.transform, "Handle", Color.white, blocksPointer: true);
            var handleRect = handle.rectTransform;
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(10f, 20f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = minValue;
            slider.maxValue = maxValue;

            ColorBlock colors = slider.colors;
            colors.normalColor = UITheme.Text;
            colors.highlightedColor = UITheme.Text;
            colors.pressedColor = UITheme.Interactive;
            colors.selectedColor = UITheme.Interactive;
            colors.disabledColor = UITheme.TextFaint;
            slider.colors = colors;

            slider.value = value;

            if (onValueChanged != null)
                slider.onValueChanged.AddListener(onValueChanged);

            return slider;
        }

        private static void SetTrackRect(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, 2f);
            rect.anchoredPosition = Vector2.zero;
        }

        // --- Option controls ---------------------------------------------------------------------------------

        /// <summary>‹ value › in a 1px frame, for choices you step through. The value Text comes back via <paramref name="valueText"/>.</summary>
        public static RectTransform CreateStepper(Transform parent, string name, UnityAction onPrevious, UnityAction onNext, Vector2 sizeDelta, out Text valueText)
        {
            RectTransform inner = CreateHairlinePanel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), sizeDelta, Vector2.zero);
            var outer = (RectTransform)inner.parent;
            // The panel helper's surface is translucent; a stepper sits on a surface already.
            inner.GetComponent<Image>().color = UITheme.Surface;

            const float arrowWidth = 44f;
            Button previous = CreateGlyphButton(inner, "Previous", "‹", onPrevious);
            PlaceStretchLeft(previous.GetComponent<RectTransform>(), arrowWidth);
            Button next = CreateGlyphButton(inner, "Next", "›", onNext);
            PlaceStretchRight(next.GetComponent<RectTransform>(), arrowWidth);

            valueText = CreateText(inner, "Value", string.Empty, UITheme.Body - 2, UITheme.Text, TextAnchor.MiddleCenter, UIFonts.Body);
            Stretch(valueText.rectTransform, arrowWidth, 0f, arrowWidth, 0f);
            // A long device name shrinks to fit rather than wrapping onto a second line.
            valueText.resizeTextForBestFit = true;
            valueText.resizeTextMinSize = 14;
            valueText.resizeTextMaxSize = UITheme.Body - 2;
            return outer;
        }

        private static Button CreateGlyphButton(Transform parent, string name, string glyph, UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = Color.white;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = UITheme.SurfaceHi;
            colors.selectedColor = UITheme.SurfaceHi;
            colors.pressedColor = UITheme.SurfaceHi;
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            button.colors = colors;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            Text label = CreateText(go.transform, "Glyph", glyph, 22, UITheme.Interactive, TextAnchor.MiddleCenter, UIFonts.Mono);
            Stretch(label.rectTransform, 0f, 0f, 0f, 0f);
            return button;
        }

        private static void PlaceStretchLeft(RectTransform rect, float width)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width, 0f);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void PlaceStretchRight(RectTransform rect, float width)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(width, 0f);
            rect.anchoredPosition = Vector2.zero;
        }

        /// <summary>Equal segments in a 1px frame, one chosen. Use the returned control to change which.</summary>
        public static UISegmentedControl CreateSegmented(Transform parent, string name, string[] options, int selected, UnityAction<int> onSelect, Vector2 sizeDelta)
        {
            RectTransform inner = CreateHairlinePanel(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), sizeDelta, Vector2.zero);
            var outer = (RectTransform)inner.parent;
            inner.GetComponent<Image>().color = UITheme.Surface;

            var row = inner.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            var fills = new GameObject[options.Length];
            var underlines = new GameObject[options.Length];
            var labels = new Text[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                var cell = new GameObject("Segment" + i, typeof(RectTransform), typeof(Image), typeof(Button));
                cell.transform.SetParent(inner, false);
                var cellImage = cell.GetComponent<Image>();
                cellImage.color = Color.white;

                var button = cell.GetComponent<Button>();
                button.targetGraphic = cellImage;
                ColorBlock colors = button.colors;
                colors.normalColor = new Color(1f, 1f, 1f, 0f);
                colors.highlightedColor = UITheme.SurfaceHi;
                colors.selectedColor = UITheme.SurfaceHi;
                colors.pressedColor = UITheme.SurfaceHi;
                colors.disabledColor = new Color(1f, 1f, 1f, 0f);
                button.colors = colors;

                Image chosen = CreateImage(cell.transform, "Chosen", UITheme.InteractiveLo);
                Stretch(chosen.rectTransform, 0f, 0f, 0f, 0f);
                fills[i] = chosen.gameObject;

                Image underline = CreateImage(cell.transform, "Underline", UITheme.Interactive);
                PlaceBottomStretch(underline.rectTransform, 0f, 0f, 0f, 3f);
                underlines[i] = underline.gameObject;

                if (i > 0)
                {
                    Image divider = CreateImage(cell.transform, "Divider", UITheme.Line);
                    divider.rectTransform.anchorMin = new Vector2(0f, 0f);
                    divider.rectTransform.anchorMax = new Vector2(0f, 1f);
                    divider.rectTransform.pivot = new Vector2(0f, 0.5f);
                    divider.rectTransform.sizeDelta = new Vector2(1f, 0f);
                    divider.rectTransform.anchoredPosition = Vector2.zero;
                }

                labels[i] = CreateText(cell.transform, "Label", UITheme.Tracked(options[i], UITheme.Label), UITheme.Label, UITheme.TextDim, TextAnchor.MiddleCenter, UIFonts.Mono);
                Stretch(labels[i].rectTransform, 0f, 0f, 0f, 0f);
                labels[i].resizeTextForBestFit = true;
                labels[i].resizeTextMinSize = 10;
                labels[i].resizeTextMaxSize = UITheme.Label;
            }

            var control = outer.gameObject.AddComponent<UISegmentedControl>();
            control.Setup(fills, underlines, labels);
            control.SetSelected(selected);

            // Wired after the first SetSelected so building the control does not fire the callback.
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                inner.GetChild(i).GetComponent<Button>().onClick.AddListener(() =>
                {
                    control.SetSelected(index);
                    onSelect?.Invoke(index);
                });
            }

            return control;
        }

        // --- Layout groups ---------------------------------------------------------------------------------

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
