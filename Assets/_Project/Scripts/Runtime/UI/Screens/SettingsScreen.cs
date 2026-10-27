using Plunderspell.Voice;
using Code.Scripts.EventSystems;
using System;
using System.Collections.Generic;
using Plunderspell.Audio;
using Plunderspell.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Plunderspell.UI.Screens
{
    public class SettingsScreen : UIScreen
    {
        private const string MasterVolumeKey = AudioLevels.MasterKey;
        private const string MusicVolumeKey = AudioLevels.MusicKey;
        private const string SfxVolumeKey = AudioLevels.SfxKey;

        // The mockup's .st-panel and .st-cols, in 1920x1080 canvas units.
        private const float PanelSide = 260f;
        private const float PanelVertical = 110f;
        private const float PadSide = 64f;
        private const float PadTop = 56f;
        private const float TitleHeight = 64f;
        private const float ColumnsTop = PadTop + TitleHeight + 30f;
        private const float ColumnHeight = 525f;
        private const float ColumnWidth = 422f;
        private const float RowLabelHeight = 37f;
        private const float RowGap = 12f;

        private Text _micGainValue;
        private Text _microphoneValue;
        private Text _outputValue;
        private UISegmentedControl _graphics;

        protected override void OnBuild()
        {
            UIFactory.CreateBackdrop(transform);

            RectTransform panel = UIFactory.CreateHairlinePanel(transform, "Panel", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            UIFactory.Stretch((RectTransform)panel.parent, PanelSide, PanelVertical, PanelSide, PanelVertical);

            var title = UIFactory.CreateText(panel, "Title", "Settings", UITheme.Heading, UITheme.Text, TextAnchor.MiddleLeft, UIFonts.Display);
            UIFactory.PlaceTopLeft(title.rectTransform, PadSide, PadTop, 700f, TitleHeight);

            var saved = UIFactory.CreateText(panel, "SavedNote", UITheme.Tracked("Saved as you change them", UITheme.Label), UITheme.Label, UITheme.TextFaint,
                TextAnchor.MiddleRight, UIFonts.Mono);
            UIFactory.PlaceTopRight(saved.rectTransform, PadSide, PadTop + TitleHeight - 24f, 600f, 24f);

            // The three columns share a LineSoft frame, so the 1px gaps between them read as hairlines.
            var frame = UIFactory.CreateImage(panel, "Columns", UITheme.LineSoft);
            UIFactory.PlaceTopStretch(frame.rectTransform, PadSide, PadSide, ColumnsTop, ColumnHeight + 2f);

            RectTransform sound = BuildColumn(frame.rectTransform, "SoundColumn", 0);
            RectTransform voice = BuildColumn(frame.rectTransform, "VoiceColumn", 1);
            RectTransform graphics = BuildColumn(frame.rectTransform, "GraphicsColumn", 2);

            UIFactory.CreateEyebrow(sound, "Eyebrow", "Sound");
            AddVolumeRow(sound, "Master", MasterVolumeKey, OnMasterVolumeChanged);
            AddVolumeRow(sound, "Music", MusicVolumeKey, OnMusicVolumeChanged);
            AddVolumeRow(sound, "Effects", SfxVolumeKey, OnSfxVolumeChanged);
            AddOutputRow(sound);

            // Voice is the lapis colour's own territory: the microphone and what it hears.
            UIFactory.CreateEyebrow(voice, "Eyebrow", "Voice", colour: UITheme.Voice);
            AddMicrophoneRow(voice);
            AddMicGainRow(voice);
            AddChatterRow(voice);
            AddMicLevelRow(voice);

            UIFactory.CreateEyebrow(graphics, "Eyebrow", "Graphics");
            AddQualityRow(graphics);
            AddNote(graphics, "Low suits a Steam Deck.");

            var backButton = UIFactory.CreateButton(panel, "BackButton", "‹ Back", OnBackClicked, new Vector2(200f, 64f), ButtonKind.Quiet);
            UIFactory.PlaceBottomLeft(backButton.GetComponent<RectTransform>(), PadSide, PadTop, 200f, 64f);
        }

        private static RectTransform BuildColumn(RectTransform frame, string name, int index)
        {
            var cell = UIFactory.CreateImage(frame, name, UITheme.Surface);
            // Inside the frame's 1px border, with a 1px gap between columns.
            UIFactory.PlaceTopLeft(cell.rectTransform, 1f + index * (ColumnWidth + 1f), 1f, ColumnWidth, ColumnHeight);
            UIFactory.AddVerticalLayout(cell.rectTransform, 26f, new RectOffset(30, 30, 28, 34), TextAnchor.UpperLeft);
            return cell.rectTransform;
        }

        /// <summary>A label on the left, its value on the right, and room below for the control.</summary>
        private static RectTransform AddRow(Transform parent, string name, string label, float controlHeight, out Text value)
        {
            var rowGo = new GameObject(name, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var row = (RectTransform)rowGo.transform;
            row.sizeDelta = new Vector2(0f, RowLabelHeight + RowGap + controlHeight);

            var labelText = UIFactory.CreateText(row, "Label", label, UITheme.Body, UITheme.Text, TextAnchor.MiddleLeft, UIFonts.Body);
            UIFactory.PlaceTopStretch(labelText.rectTransform, 0f, 0f, 0f, RowLabelHeight);

            value = UIFactory.CreateText(row, "Value", string.Empty, 17, UITheme.TextDim, TextAnchor.MiddleRight, UIFonts.Mono);
            UIFactory.PlaceTopStretch(value.rectTransform, 0f, 0f, 0f, RowLabelHeight);
            return row;
        }

        private static void PlaceControl(RectTransform control, float height) =>
            UIFactory.PlaceTopStretch(control, 0f, 0f, RowLabelHeight + RowGap, height);

        private static void AddNote(Transform parent, string text)
        {
            var note = UIFactory.CreateText(parent, "Note", text, 19, UITheme.TextDim, TextAnchor.UpperLeft, UIFonts.BodyItalic);
            note.rectTransform.sizeDelta = new Vector2(0f, 60f);
        }

        private void AddVolumeRow(Transform parent, string label, string prefsKey, UnityAction<float> onChanged)
        {
            RectTransform row = AddRow(parent, label + "Row", label, 28f, out Text value);

            float startValue = PlayerPrefs.GetFloat(prefsKey, 1f);
            value.text = Percent(startValue);
            var slider = UIFactory.CreateSlider(row, "Slider", 0f, 1f, startValue, v =>
            {
                value.text = Percent(v);
                onChanged(v);
            }, new Vector2(0f, 28f));
            PlaceControl(slider.GetComponent<RectTransform>(), 28f);
        }

        private static string Percent(float value) => $"{Mathf.RoundToInt(value * 100f)}%";

        private void AddMicrophoneRow(Transform parent)
        {
            RectTransform row = AddRow(parent, "MicrophoneRow", "Microphone", 54f, out Text value);
            value.gameObject.SetActive(false);

            RectTransform stepper = UIFactory.CreateStepper(row, "MicrophoneButton", () => CycleMicrophone(-1), () => CycleMicrophone(1),
                new Vector2(0f, 54f), out _microphoneValue);
            PlaceControl(stepper, 54f);
            RefreshMicrophoneLabel();
        }

        private void AddOutputRow(Transform parent)
        {
            RectTransform row = AddRow(parent, "OutputRow", "Output", 54f, out Text value);
            value.gameObject.SetActive(false);

            RectTransform stepper = UIFactory.CreateStepper(row, "OutputButton", () => CycleOutput(-1), () => CycleOutput(1),
                new Vector2(0f, 54f), out _outputValue);
            PlaceControl(stepper, 54f);
            // Windows device names run long ("Headphones (BlackShark V2 Pro PS 2.4)"): shrink to fit the box.
            _outputValue.horizontalOverflow = HorizontalWrapMode.Wrap;
            _outputValue.verticalOverflow = VerticalWrapMode.Truncate;
            _outputValue.resizeTextForBestFit = true;
            _outputValue.resizeTextMinSize = 12;
            _outputValue.resizeTextMaxSize = _outputValue.fontSize;
            RefreshOutputLabel();
        }

        /// <summary>
        /// The microphone gain (#125): how much a quiet microphone is turned up before the game hears
        /// it. The level meter while holding V shows the result.
        /// </summary>
        private void AddMicGainRow(Transform parent)
        {
            RectTransform row = AddRow(parent, "MicGainRow", "Gain", 28f, out _micGainValue);

            var slider = UIFactory.CreateSlider(row, "Slider", AudioInputSettings.MinMicGain,
                AudioInputSettings.MaxMicGain, AudioInputSettings.MicGain, OnMicGainChanged, new Vector2(0f, 28f));
            PlaceControl(slider.GetComponent<RectTransform>(), 28f);
            RefreshMicGainLabel();
        }

        /// <summary>Opt-in: between casts the game also writes down what you say, and guards can overhear it.
        /// Runs on this machine; only the words go to the host in co-op. Use a headset so game sound is not "heard".</summary>
        private void AddChatterRow(Transform parent)
        {
            RectTransform row = AddRow(parent, "ChatterRow", "Guards hear my voice", 52f, out Text value);
            value.gameObject.SetActive(false);

            _chatter = UIFactory.CreateSegmented(row, "ChatterButton", new[] { "Off", "On" },
                AudioInputSettings.GuardsHearChatter ? 1 : 0, index => AudioInputSettings.GuardsHearChatter = index == 1,
                new Vector2(0f, 52f));
            PlaceControl((RectTransform)_chatter.transform, 52f);
        }

        private UISegmentedControl _chatter;

        private void AddQualityRow(Transform parent)
        {
            RectTransform row = AddRow(parent, "QualityRow", "Quality", 52f, out Text value);
            value.gameObject.SetActive(false);

            _graphics = UIFactory.CreateSegmented(row, "GraphicsButton", QualitySettings.names, QualitySettings.GetQualityLevel(),
                SetGraphics, new Vector2(0f, 52f));
            PlaceControl((RectTransform)_graphics.transform, 52f);
        }

        // The meter's full scale: RMS 0.6 is a shout well past the shout mark.
        private const float MicMeterMax = 0.6f;

        private Image _micLevelFill;

        /// <summary>
        /// A live level meter with the whisper and shout marks, for testing and tuning the microphone and its
        /// gain here rather than in a raid. Fed by MicLevelChanged while this screen is open (#303).
        /// </summary>
        private void AddMicLevelRow(Transform parent)
        {
            RectTransform row = AddRow(parent, "MicLevelRow", "Level", 14f, out Text value);
            value.gameObject.SetActive(false);

            Image bar = UIFactory.CreateProgressBar(row, "MicLevelBar", UITheme.Voice, new Vector2(0f, 14f), out _micLevelFill);
            PlaceControl(bar.rectTransform, 14f);
            AddMeterMark(bar.rectTransform, "WhisperMark", VoiceUtility.WhisperThreshold / MicMeterMax);
            AddMeterMark(bar.rectTransform, "ShoutMark", VoiceUtility.ShoutThreshold / MicMeterMax);
            UIFactory.SetBarFill(_micLevelFill, 0f);
        }

        private static void AddMeterMark(RectTransform bar, string name, float at)
        {
            Image mark = UIFactory.CreateImage(bar, name, UITheme.TextDim);
            mark.rectTransform.anchorMin = new Vector2(at, 0f);
            mark.rectTransform.anchorMax = new Vector2(at, 1f);
            mark.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            mark.rectTransform.sizeDelta = new Vector2(2f, 0f);
            mark.rectTransform.anchoredPosition = Vector2.zero;
        }

        private static VoskVoiceInputService Speech =>
            (VoiceServiceLocator.Current as CombinedVoiceInputService)?.Speech;

        private void OnMicGainChanged(float value)
        {
            AudioInputSettings.MicGain = value;
            RefreshMicGainLabel();
        }

        private void RefreshMicGainLabel()
        {
            if (_micGainValue != null)
                _micGainValue.text = $"{AudioInputSettings.MicGain:0.00}×";
        }

        private void OnMasterVolumeChanged(float value) => AudioLevels.SetMaster(value);

        private void OnMusicVolumeChanged(float value) => AudioLevels.SetMusic(value);

        private void OnSfxVolumeChanged(float value) => AudioLevels.SetEffects(value);

        protected override void OnShown()
        {
            RefreshMicrophoneLabel();
            RefreshOutputLabel();
            if (_chatter != null)
                _chatter.SetSelected(AudioInputSettings.GuardsHearChatter ? 1 : 0);
            if (_graphics != null)
                _graphics.SetSelected(QualitySettings.GetQualityLevel());

            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            EventManager.Instance?.Subscribe(this, (MicLevelChanged e) => UIFactory.SetBarFill(_micLevelFill, e.Level / MicMeterMax));
            if (Speech != null)
                Speech.MeterEnabled = true;
        }

        // Closing, a change of game state and quitting all hide this screen: stop listening and let the microphone rest.
        private void OnDisable()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            if (Speech != null)
                Speech.MeterEnabled = false;
            if (_micLevelFill != null)
                UIFactory.SetBarFill(_micLevelFill, 0f);
        }

        /// <summary>
        /// Applies a quality level (Low, Medium, High) at once and saves it. Low is for a Steam Deck or
        /// a weak PC; the atmosphere picks it by itself on a Deck until the player chooses.
        /// </summary>
        private static void SetGraphics(int level)
        {
            string[] levels = QualitySettings.names;
            QualitySettings.SetQualityLevel(level, true);
            PlayerPrefs.SetString(GraphicsLevelSettings.QualityKey, levels[level]);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Steps through Automatic and every microphone Windows reports, forwards or back. Automatic
        /// skips virtual inputs (a VR streaming app's silent mic was the Windows default on the dev
        /// machine).
        /// </summary>
        private void CycleMicrophone(int step)
        {
            string[] devices = Microphone.devices ?? Array.Empty<string>();
            string current = PlayerPrefs.GetString(AudioInputSettings.MicrophoneKey, string.Empty);
            int index = Array.IndexOf(devices, current); // -1 = Automatic

            // Position 0 is Automatic, 1.. are the devices, wrapping in both directions.
            int count = devices.Length + 1;
            int position = ((index + 1 + step) % count + count) % count;
            index = position - 1;

            AudioInputSettings.Microphone = index < 0 ? string.Empty : devices[index];
            RefreshMicrophoneLabel();
        }

        private void RefreshMicrophoneLabel()
        {
            if (_microphoneValue == null)
                return;
            string[] devices = Microphone.devices ?? Array.Empty<string>();
            string current = PlayerPrefs.GetString(AudioInputSettings.MicrophoneKey, string.Empty);
            _microphoneValue.text = devices.Length == 0 ? "None found (keys still cast)"
                : Array.IndexOf(devices, current) < 0 ? "Automatic"
                : current;
        }

        /// <summary>
        /// Steps through the Windows default and every playback device, forwards or back, and switches
        /// the game's output at once. See <see cref="AudioOutputDevices"/>.
        /// </summary>
        private void CycleOutput(int step)
        {
            List<OutputDevice> devices = AudioOutputDevices.List();
            int index = devices.FindIndex(d => d.Id == AudioOutputDevices.ChosenId); // -1 = Windows default

            // Position 0 is the Windows default, 1.. are the devices, wrapping in both directions.
            int count = devices.Count + 1;
            int position = ((index + 1 + step) % count + count) % count;
            index = position - 1;

            AudioOutputDevices.Apply(index < 0 ? string.Empty : devices[index].Id);
            RefreshOutputLabel();
        }

        private void RefreshOutputLabel()
        {
            if (_outputValue == null)
                return;
            if (!AudioOutputDevices.IsSupported)
            {
                _outputValue.text = "System default";
                return;
            }
            List<OutputDevice> devices = AudioOutputDevices.List();
            OutputDevice chosen = devices.Find(d => d.Id == AudioOutputDevices.ChosenId);
            _outputValue.text = chosen.Id == null ? "Windows default" : chosen.Name;
        }

        private void OnBackClicked() => GameServices.GameState.ChangeState(GameServices.GameState.PreviousState);
    }
}
