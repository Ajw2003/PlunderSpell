using System;
using Plunderspell.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
            var panel = UIFactory.CreatePanel(transform, "Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(560f, 780f), Vector2.zero, UITheme.PanelBackground);

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
            listRect.sizeDelta = new Vector2(440f, 530f);
            listRect.anchoredPosition = new Vector2(0f, 10f);
            UIFactory.AddVerticalLayout(listRect, 30f, new RectOffset(0, 0, 0, 0));

            AddVolumeRow(listRect, "Master Volume", MasterVolumeKey, OnMasterVolumeChanged);
            AddVolumeRow(listRect, "Music Volume", MusicVolumeKey, OnMusicVolumeChanged);
            AddVolumeRow(listRect, "SFX Volume", SfxVolumeKey, OnSfxVolumeChanged);

            var micButton = UIFactory.CreateButton(listRect, "MicrophoneButton", string.Empty, CycleMicrophone, new Vector2(440f, 44f));
            _microphoneLabel = micButton.GetComponentInChildren<Text>();
            RefreshMicrophoneLabel();

            AddMicGainRow(listRect);

            var chatterButton = UIFactory.CreateButton(listRect, "ChatterButton", string.Empty, ToggleChatter, new Vector2(440f, 44f));
            _chatterLabel = chatterButton.GetComponentInChildren<Text>();
            RefreshChatterLabel();

            var graphicsButton = UIFactory.CreateButton(listRect, "GraphicsButton", string.Empty, CycleGraphics, new Vector2(440f, 44f));
            _graphicsLabel = graphicsButton.GetComponentInChildren<Text>();
            RefreshGraphicsLabel();

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

        /// <summary>
        /// The microphone gain (#125): how much a quiet microphone is turned up before the game hears
        /// it. The level meter while holding V shows the result.
        /// </summary>
        private void AddMicGainRow(Transform parent)
        {
            var rowGo = new GameObject("MicGainRow", typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowRect = (RectTransform)rowGo.transform;
            rowRect.sizeDelta = new Vector2(440f, 60f);

            _micGainLabel = UIFactory.CreateText(rowRect, "Label", string.Empty, UITheme.BodyFontSize, UITheme.TextPrimary, TextAnchor.UpperLeft);
            _micGainLabel.rectTransform.anchorMin = new Vector2(0f, 1f);
            _micGainLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
            _micGainLabel.rectTransform.pivot = new Vector2(0.5f, 1f);
            _micGainLabel.rectTransform.sizeDelta = new Vector2(0f, 24f);
            _micGainLabel.rectTransform.anchoredPosition = Vector2.zero;

            var slider = UIFactory.CreateSlider(rowRect, "Slider", AudioInputSettings.MinMicGain,
                AudioInputSettings.MaxMicGain, AudioInputSettings.MicGain, OnMicGainChanged, new Vector2(440f, 24f));
            slider.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -18f);
            RefreshMicGainLabel();
        }

        private void OnMicGainChanged(float value)
        {
            AudioInputSettings.MicGain = value;
            RefreshMicGainLabel();
        }

        private void RefreshMicGainLabel()
        {
            if (_micGainLabel != null)
                _micGainLabel.text = $"Microphone Gain: {AudioInputSettings.MicGain:0.00}x";
        }

        private Text _micGainLabel;

        private Text _chatterLabel;

        /// <summary>Opt-in: between casts the game also writes down what you say, and guards can overhear it.
        /// Runs on this machine; only the words go to the host in co-op. Use a headset so game sound is not "heard".</summary>
        private void ToggleChatter()
        {
            AudioInputSettings.GuardsHearChatter = !AudioInputSettings.GuardsHearChatter;
            RefreshChatterLabel();
        }

        private void RefreshChatterLabel()
        {
            if (_chatterLabel != null)
                _chatterLabel.text = $"Guards hear my voice: {(AudioInputSettings.GuardsHearChatter ? "On" : "Off")}";
        }

        private void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
        }

        private void OnMusicVolumeChanged(float value) => PlayerPrefs.SetFloat(MusicVolumeKey, value);

        private void OnSfxVolumeChanged(float value) => PlayerPrefs.SetFloat(SfxVolumeKey, value);

        private Text _microphoneLabel;

        private Text _graphicsLabel;

        protected override void OnShown()
        {
            RefreshMicrophoneLabel();
            RefreshChatterLabel();
            RefreshGraphicsLabel();
        }

        /// <summary>
        /// Steps through the quality levels (Low, Medium, High), applying each at once and saving it.
        /// Low is for a Steam Deck or a weak PC; the atmosphere picks it by itself on a Deck until the
        /// player chooses.
        /// </summary>
        private void CycleGraphics()
        {
            string[] levels = QualitySettings.names;
            int next = (QualitySettings.GetQualityLevel() + 1) % levels.Length;
            QualitySettings.SetQualityLevel(next, true);
            PlayerPrefs.SetString(GraphicsLevelSettings.QualityKey, levels[next]);
            PlayerPrefs.Save();
            RefreshGraphicsLabel();
        }

        private void RefreshGraphicsLabel()
        {
            if (_graphicsLabel == null)
                return;
            _graphicsLabel.text = $"Graphics: {QualitySettings.names[QualitySettings.GetQualityLevel()]}";
        }

        /// <summary>
        /// Steps through Automatic and every microphone Windows reports. Automatic skips virtual
        /// inputs (a VR streaming app's silent mic was the Windows default on the dev machine).
        /// </summary>
        private void CycleMicrophone()
        {
            string[] devices = Microphone.devices ?? Array.Empty<string>();
            string current = PlayerPrefs.GetString(AudioInputSettings.MicrophoneKey, string.Empty);
            int index = Array.IndexOf(devices, current); // -1 = Automatic
            index = index + 1 >= devices.Length ? -1 : index + 1;
            PlayerPrefs.SetString(AudioInputSettings.MicrophoneKey, index < 0 ? string.Empty : devices[index]);
            PlayerPrefs.Save();
            RefreshMicrophoneLabel();
        }

        private void RefreshMicrophoneLabel()
        {
            if (_microphoneLabel == null)
                return;
            string[] devices = Microphone.devices ?? Array.Empty<string>();
            string current = PlayerPrefs.GetString(AudioInputSettings.MicrophoneKey, string.Empty);
            _microphoneLabel.text = devices.Length == 0 ? "Microphone: none found (keys still cast)"
                : Array.IndexOf(devices, current) < 0 ? "Microphone: Automatic"
                : $"Microphone: {current}";
        }

        private void OnBackClicked() => GameServices.GameState.ChangeState(GameServices.GameState.PreviousState);
    }
}
