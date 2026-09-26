using Plunderspell.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Plunderspell.UI.Screens
{
    public class SettingsScreen : UIScreen
    {
        private const string MasterVolumeKey = "Settings.MasterVolume";
        private const string MusicVolumeKey = "Settings.MusicVolume";
        private const string SfxVolumeKey = "Settings.SfxVolume";

        protected override void OnBuild()
        {
            UIFactory.CreateFullStretchPanel(transform, "Overlay", new Color(0f, 0f, 0f, 0.7f));
            var panel = UIFactory.CreatePanel(transform, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 440f), Vector2.zero, UITheme.PanelBackground);

            var title = UIFactory.CreateText(panel, "Title", "SETTINGS", UITheme.HeaderFontSize, UITheme.TextPrimary);
            title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.sizeDelta = new Vector2(400f, 60f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -24f);

            var listGo = new GameObject("SliderList", typeof(RectTransform));
            listGo.transform.SetParent(panel, false);
            var listRect = (RectTransform)listGo.transform;
            listRect.anchorMin = new Vector2(0.5f, 0.5f);
            listRect.anchorMax = new Vector2(0.5f, 0.5f);
            listRect.sizeDelta = new Vector2(440f, 220f);
            listRect.anchoredPosition = new Vector2(0f, 10f);
            UIFactory.AddVerticalLayout(listRect, 30f, new RectOffset(0, 0, 0, 0));

            AddVolumeRow(listRect, "Master Volume", MasterVolumeKey, OnMasterVolumeChanged);
            AddVolumeRow(listRect, "Music Volume", MusicVolumeKey, OnMusicVolumeChanged);
            AddVolumeRow(listRect, "SFX Volume", SfxVolumeKey, OnSfxVolumeChanged);

            var backButton = UIFactory.CreateButton(panel, "BackButton", "Back", OnBackClicked, new Vector2(200f, 52f));
            var backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.5f, 0f);
            backRect.anchorMax = new Vector2(0.5f, 0f);
            backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 24f);
        }

        private void AddVolumeRow(Transform parent, string label, string prefsKey, UnityAction<float> onChanged)
        {
            var rowGo = new GameObject(label.Replace(" ", string.Empty) + "Row", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowRect = (RectTransform)rowGo.transform;
            rowRect.sizeDelta = new Vector2(440f, 60f);

            var labelText = UIFactory.CreateText(rowRect, "Label", label, UITheme.BodyFontSize, UITheme.TextPrimary, TextAnchor.UpperLeft);
            labelText.rectTransform.anchorMin = new Vector2(0f, 1f);
            labelText.rectTransform.anchorMax = new Vector2(1f, 1f);
            labelText.rectTransform.pivot = new Vector2(0.5f, 1f);
            labelText.rectTransform.sizeDelta = new Vector2(0f, 24f);
            labelText.rectTransform.anchoredPosition = Vector2.zero;

            float startValue = PlayerPrefs.GetFloat(prefsKey, 1f);
            var slider = UIFactory.CreateSlider(rowRect, "Slider", 0f, 1f, startValue, onChanged, new Vector2(440f, 24f));
            slider.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -18f);
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
        }

        private void OnMusicVolumeChanged(float value) => PlayerPrefs.SetFloat(MusicVolumeKey, value);

        private void OnSfxVolumeChanged(float value) => PlayerPrefs.SetFloat(SfxVolumeKey, value);

        private void OnBackClicked() => GameServices.GameState.ChangeState(GameServices.GameState.PreviousState);
    }
}
