using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Plunderspell.Raid
{
    /// <summary>
    /// A full-screen "Loading" card drawn over everything while a scene changes, so its long frames are not seen
    /// (aj, 2026-10-29, #549). Built in code the first time it is shown and kept for the session.
    /// </summary>
    public static class LoadingScreen
    {
        private static GameObject s_root;

        public static bool IsShowing => s_root != null && s_root.activeSelf;

        /// <summary>How opaque the card is, for fading it in and out.</summary>
        public static float Alpha
        {
            get => s_group != null ? s_group.alpha : 0f;
            set
            {
                if (s_root == null)
                    Build();
                s_group.alpha = value;
            }
        }

        private static CanvasGroup s_group;

        public static void Show()
        {
            if (s_root == null)
                Build();
            s_root.SetActive(true);
        }

        public static void Hide()
        {
            if (s_root != null)
                s_root.SetActive(false);
        }

        private static void Build()
        {
            s_root = new GameObject("LoadingScreen");
            Object.DontDestroyOnLoad(s_root);
            s_group = s_root.AddComponent<CanvasGroup>();
            var canvas = s_root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            s_root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var back = new GameObject("Back").AddComponent<Image>();
            back.transform.SetParent(s_root.transform, false);
            back.color = new Color(0.08f, 0.07f, 0.055f, 1f);
            Stretch(back.rectTransform);

            var label = new GameObject("Label").AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(s_root.transform, false);
            label.text = "Loading";
            label.fontSize = 48f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.86f, 0.82f, 0.73f, 1f);
            var font = Resources.Load<TMP_FontAsset>("UI/Fonts/Spectral-Regular SDF");
            if (font != null)
                label.font = font;
            Stretch(label.rectTransform);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
