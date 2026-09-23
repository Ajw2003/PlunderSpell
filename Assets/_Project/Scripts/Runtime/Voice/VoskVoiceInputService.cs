using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if !HEADLESS
using System.Threading.Tasks;
using Vosk;
#endif

namespace RogueAi.Voice
{
    [System.Serializable]
    public class VoiceRecognizerJson // JsonUtility target for Vosk's {"text":"..."} payload
    {
        public string text;
        public string partial;
    }

    // doc-ref 5ea6 docs/systems/voice.md
    /// <summary>
    /// Windows x64 speech provider backed by the offline Vosk recogniser. Reads the microphone on
    /// the main thread, and maps English spellings the model can hear back to the Latin lexicon
    /// words via <see cref="SetVocabulary"/>.
    /// </summary>
    public class VoskVoiceInputService : IVoiceInputService, IPhraseVocabularyTarget
    {
        public bool IsListening { get; private set; }
        public event Action<VoiceRecognitionResult> OnPhraseRecognized;

        // Model lives under StreamingAssets so it ships read-only with the player.
        public const string ModelRelativePath = "VoskModels/small-en-us";
        private const int SampleRate = 16000;
        private const int MicLoopSeconds = 2;
        private const string UnknownToken = "[unk]";

        /// <summary>The loudest moment of the last phrase (RMS, 0..1), for a loudness meter or calibration.</summary>
        public float LastPeakRms { get; private set; }

        /// <summary>What the recogniser literally heard last time, before the vocabulary mapped it.</summary>
        public string LastHeardText { get; private set; } = string.Empty;

        /// <summary>True when the model folder exists, i.e. real speech recognition is possible.</summary>
        public static bool IsModelInstalled =>
            Directory.Exists(Path.Combine(Application.streamingAssetsPath, ModelRelativePath));

        // heard phrase (lower case, as the recogniser spells it) -> canonical lexicon word.
        private readonly Dictionary<string, string> _vocabulary = new Dictionary<string, string>();

#if !HEADLESS
        private readonly Task<Model> _modelLoad;
        private VoskRecognizer _recognizer;
        private bool _recognizerStale = true;
        private AudioClip _micClip;
        private string _micDevice;
        private int _lastSamplePosition;
        private float[] _floatBuffer = new float[SampleRate];
        private short[] _shortBuffer = new short[SampleRate];
        private MainThreadPump _pump;

        public VoskVoiceInputService()
        {
            string modelPath = Path.Combine(Application.streamingAssetsPath, ModelRelativePath);
            if (!Directory.Exists(modelPath))
            {
                Debug.LogWarning(
                    $"[Vosk] Model not found at '{modelPath}'. Run " +
                    "'Plunderspell/Voice/Download Vosk Small Model' from the editor menu. " +
                    "Voice casting will stay silent.");
                return;
            }

            // ~300 ms of native loading: done once, off the main thread, so pressing the cast key
            // never hitches and never clips the start of what the player says.
            _modelLoad = Task.Run(() =>
            {
                Vosk.Vosk.SetLogLevel(-1); // silence native chatter
                return new Model(modelPath);
            });
        }
#endif

        /// <summary>
        /// Constrains recognition to these heard phrases and maps each back to its lexicon word.
        /// An empty map means free-form recognition.
        /// </summary>
        public void SetVocabulary(IReadOnlyDictionary<string, string> heardToCanonical)
        {
            _vocabulary.Clear();
            if (heardToCanonical != null)
            {
                foreach (var pair in heardToCanonical)
                {
                    if (!string.IsNullOrWhiteSpace(pair.Key))
                        _vocabulary[pair.Key.Trim().ToLowerInvariant()] = pair.Value;
                }
            }
#if !HEADLESS
            _recognizerStale = true;
#endif
        }

        public void StartListening()
        {
            if (IsListening)
                return;

#if HEADLESS
            Debug.LogWarning("[Vosk] Voice input disabled in HEADLESS build.");
#else
            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                // Directive: must not crash without a mic — log and stay silent.
                Debug.LogWarning("[Vosk] No microphone device found; voice casting will stay silent.");
                return;
            }

            if (!EnsureRecognizer())
                return;

            // null is Unity's "the system default microphone" — what the player actually set up in
            // Windows, rather than whichever device happens to enumerate first (often a virtual one).
            _micDevice = null;
            _micClip = Microphone.Start(_micDevice, true, MicLoopSeconds, SampleRate);
            if (_micClip == null)
            {
                Debug.LogWarning("[Vosk] Microphone.Start returned null; voice casting will stay silent.");
                return;
            }

            EnsurePump();
            _lastSamplePosition = 0;
            LastPeakRms = 0f;
            IsListening = true;
            Debug.Log($"[Vosk] Listening on the default microphone @ {SampleRate}Hz.");
#endif
        }

        public void StopListening()
        {
            if (!IsListening)
                return;

#if HEADLESS
            IsListening = false;
#else
            // Take whatever was said right up to the release before closing the mic.
            ReadMicrophone();
            IsListening = false;

            try
            {
                EmitFromJson(_recognizer.FinalResult());
                _recognizer.Reset();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Vosk] FinalResult failed: {e.Message}");
                _recognizerStale = true;
            }

            Microphone.End(_micDevice);
            _micClip = null;
            Debug.Log("[Vosk] Stopped listening.");
#endif
        }

#if !HEADLESS
        /// <summary>
        /// Recognises a finished recording (16 kHz mono PCM) exactly as a held key would, without a
        /// microphone. The test seam for recorded fixtures; returns the lexicon word, or null.
        /// </summary>
        public string RecognizeSamples(short[] samples)
        {
            if (_modelLoad != null && !_modelLoad.IsCompleted)
                _modelLoad.Wait();
            if (!EnsureRecognizer())
                return null;

            string recognised = null;
            void Capture(VoiceRecognitionResult r) => recognised = r.NormalizedText;
            OnPhraseRecognized += Capture;
            try
            {
                _recognizer.AcceptWaveform(samples, samples.Length);
                EmitFromJson(_recognizer.FinalResult());
                _recognizer.Reset();
            }
            finally
            {
                OnPhraseRecognized -= Capture;
            }
            return recognised;
        }

        /// <summary>Makes sure a recogniser matching the current vocabulary exists. False if the
        /// model isn't installed, failed to load, or is still loading.</summary>
        private bool EnsureRecognizer()
        {
            if (_modelLoad == null)
            {
                Debug.LogWarning("[Vosk] No model installed; voice casting will stay silent.");
                return false;
            }

            if (!_modelLoad.IsCompleted)
            {
                Debug.LogWarning("[Vosk] The speech model is still loading; try again in a moment.");
                return false;
            }

            if (_modelLoad.IsFaulted)
            {
                Debug.LogWarning($"[Vosk] Failed to load model: {_modelLoad.Exception?.GetBaseException().Message}");
                return false;
            }

            if (_recognizer != null && !_recognizerStale)
                return true;

            try
            {
                _recognizer?.Dispose();
                _recognizer = _vocabulary.Count > 0
                    ? new VoskRecognizer(_modelLoad.Result, SampleRate, BuildGrammarJson())
                    : new VoskRecognizer(_modelLoad.Result, SampleRate);
                _recognizerStale = false;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Vosk] Failed to create recognizer: {e.Message}");
                _recognizer = null;
                return false;
            }
        }

        private string BuildGrammarJson()
        {
            var json = new System.Text.StringBuilder("[");
            foreach (string heard in _vocabulary.Keys)
                json.Append('"').Append(heard.Replace("\"", string.Empty)).Append("\",");
            json.Append('"').Append(UnknownToken).Append("\"]");
            return json.ToString();
        }

        /// <summary>Feeds every sample recorded since the last call to the recogniser. Main thread only.</summary>
        private void ReadMicrophone()
        {
            if (!IsListening || _micClip == null || _recognizer == null)
                return;

            int position = Microphone.GetPosition(_micDevice);
            int available = position - _lastSamplePosition;
            if (available < 0)
                available += _micClip.samples; // wrapped around the loop clip
            if (available <= 0)
                return;

            if (_floatBuffer.Length != available)
                _floatBuffer = new float[available];
            if (_shortBuffer.Length < available)
                _shortBuffer = new short[available];

            // GetData wraps around the end of a looping clip on its own.
            _micClip.GetData(_floatBuffer, _lastSamplePosition);
            _lastSamplePosition = position;

            LastPeakRms = Mathf.Max(LastPeakRms, VoiceUtility.ComputeRms(_floatBuffer, available));
            for (int i = 0; i < available; i++)
                _shortBuffer[i] = (short)Mathf.Clamp(_floatBuffer[i] * short.MaxValue, short.MinValue, short.MaxValue);

            try
            {
                // True means the recogniser found the end of an utterance mid-hold: emit it now.
                if (_recognizer.AcceptWaveform(_shortBuffer, available))
                    EmitFromJson(_recognizer.Result());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Vosk] AcceptWaveform failed: {e.Message}");
            }
        }

        /// <summary>Parse a Vosk JSON payload and, if it holds recognisable text, raise the event.</summary>
        private void EmitFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            string heard;
            try
            {
                heard = JsonUtility.FromJson<VoiceRecognizerJson>(json)?.text;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Vosk] Unreadable result '{json}': {e.Message}");
                return;
            }

            heard = heard?.Replace(UnknownToken, string.Empty).Trim();
            if (string.IsNullOrEmpty(heard))
            {
                Debug.Log("[Vosk] Heard nothing it recognises.");
                return;
            }

            LastHeardText = heard;
            string spoken = _vocabulary.TryGetValue(heard, out string canonical) ? canonical : heard;

            var result = new VoiceRecognitionResult(
                rawText: heard,
                normalizedText: VoiceUtility.Normalize(spoken),
                confidence: 1f,
                rmsAmplitude: LastPeakRms,
                volume: VoiceUtility.ClassifyVolume(LastPeakRms));

            Debug.Log($"[Vosk] Heard \"{heard}\" -> {result}");
            OnPhraseRecognized?.Invoke(result);
        }

        private void EnsurePump()
        {
            if (_pump != null)
                return;
            var go = new GameObject("~VoskVoicePump");
            go.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(go);
            _pump = go.AddComponent<MainThreadPump>();
            _pump.Init(this);
        }

        /// <summary>Hidden helper that reads the microphone on the Unity main thread each frame.</summary>
        private class MainThreadPump : MonoBehaviour
        {
            private VoskVoiceInputService _owner;
            public void Init(VoskVoiceInputService owner) => _owner = owner;
            private void Update() => _owner?.ReadMicrophone();
        }
#endif
    }
}
