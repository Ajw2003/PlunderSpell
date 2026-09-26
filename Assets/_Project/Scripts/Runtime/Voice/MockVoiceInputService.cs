using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RogueAi.Voice
{
    /// <summary>
    /// Keyboard-driven stand-in for the real speech recogniser. Used in the editor and
    /// headless CI so the whole casting pipeline can be exercised without a microphone
    /// or the native Vosk binary.
    ///
    /// While listening:
    ///   Alpha1..Alpha8            -> correct spell words
    ///   Shift + Alpha1..Alpha8    -> deliberate misfire near-matches
    ///   Ctrl  held  -> whisper (rms 0.05)
    ///   Shift held  -> shout   (rms 0.6)   [Shift also selects the misfire word]
    ///   otherwise   -> normal  (rms 0.15..0.35)
    /// </summary>
    public class MockVoiceInputService : MonoBehaviour, IVoiceInputService
    {
        public bool IsListening { get; private set; }
        public event Action<VoiceRecognitionResult> OnPhraseRecognized;

        /// <summary>Correct spell word per key.</summary>
        public readonly Dictionary<KeyCode, string> keybindMap = new Dictionary<KeyCode, string>
        {
            { KeyCode.Alpha1, "IGNIS" },
            { KeyCode.Alpha2, "FRANGO" },
            { KeyCode.Alpha3, "LEVO" },
            { KeyCode.Alpha4, "AURUM VOCO" },
            { KeyCode.Alpha5, "TONITRUS" },
            { KeyCode.Alpha6, "SOMNUS" },
            { KeyCode.Alpha7, "CADAVER SURGE" },
            { KeyCode.Alpha8, "PORTA" },
        };

        /// <summary>Near-match / misfire word per key (fired when Shift is held).</summary>
        public readonly Dictionary<KeyCode, string> misfireMap = new Dictionary<KeyCode, string>
        {
            { KeyCode.Alpha1, "AGNIS" },
            { KeyCode.Alpha2, "FRANCO" },
            { KeyCode.Alpha3, "LAVO" },
            { KeyCode.Alpha4, "AURUM BOCO" },
            { KeyCode.Alpha5, "TONITUS" },
            { KeyCode.Alpha6, "SONUS" },
            { KeyCode.Alpha7, "CADAVER SURJ" },
            { KeyCode.Alpha8, "PROTA" },
        };

        private static MockVoiceInputService _instance;

        /// <summary>Get (or lazily create a hidden driver object for) the mock service.</summary>
        public static MockVoiceInputService GetOrCreate()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject("~MockVoiceInputService");
            go.hideFlags = HideFlags.HideAndDontSave;
            if (Application.isPlaying)
                DontDestroyOnLoad(go);
            _instance = go.AddComponent<MockVoiceInputService>();
            return _instance;
        }

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
        }

        public void StartListening()
        {
            IsListening = true;
            Debug.Log("[MockVoice] StartListening");
        }

        public void StopListening()
        {
            IsListening = false;
            Debug.Log("[MockVoice] StopListening");
        }

        /// <summary>
        /// Which new-Input-System key stands in for each legacy <see cref="KeyCode"/> the maps are
        /// written in. The maps stay keyed by KeyCode because SimulateKeyPress is part of the test
        /// surface and several suites already call it that way.
        /// </summary>
        private static readonly Dictionary<KeyCode, Key> s_newInputKeys = new Dictionary<KeyCode, Key>
        {
            { KeyCode.Alpha1, Key.Digit1 },
            { KeyCode.Alpha2, Key.Digit2 },
            { KeyCode.Alpha3, Key.Digit3 },
            { KeyCode.Alpha4, Key.Digit4 },
            { KeyCode.Alpha5, Key.Digit5 },
            { KeyCode.Alpha6, Key.Digit6 },
            { KeyCode.Alpha7, Key.Digit7 },
            { KeyCode.Alpha8, Key.Digit8 },
        };

        private void Update()
        {
            if (!IsListening)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            bool ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;

            foreach (var kv in keybindMap)
            {
                if (!s_newInputKeys.TryGetValue(kv.Key, out Key key))
                    continue;

                if (keyboard[key].wasPressedThisFrame)
                {
                    string word = shift && misfireMap.TryGetValue(kv.Key, out var mis) ? mis : kv.Value;
                    EmitPhrase(word, ctrl, shift);
                }
            }
        }

        /// <summary>
        /// Public test seam: synthesise a phrase for a key press exactly as Update() would,
        /// so unit tests never depend on the Input system.
        /// </summary>
        public void SimulateKeyPress(KeyCode key, bool shift = false, bool ctrl = false)
        {
            if (!IsListening)
                return;
            string word = shift && misfireMap.TryGetValue(key, out var mis)
                ? mis
                : (keybindMap.TryGetValue(key, out var w) ? w : null);
            if (word == null)
                return;
            EmitPhrase(word, ctrl, shift);
        }

        private void EmitPhrase(string rawWord, bool ctrl, bool shift)
        {
            float rms;
            if (ctrl) rms = 0.05f;         // whisper
            else if (shift) rms = 0.6f;    // shout
            else rms = UnityEngine.Random.Range(0.15f, 0.35f); // normal

            var result = new VoiceRecognitionResult(
                rawText: rawWord,
                normalizedText: VoiceUtility.Normalize(rawWord),
                confidence: 1f,
                rmsAmplitude: rms,
                volume: VoiceUtility.ClassifyVolume(rms),
                fromKeyboard: true);

            Debug.Log($"[MockVoice] Emit {result}");
            OnPhraseRecognized?.Invoke(result);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
