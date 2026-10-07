using System;
using UnityEngine;

namespace Plunderspell.Core
{
    /// <summary>
    /// Where the player's microphone choices are saved. Shared by the Settings screen (which writes
    /// them) and the voice service (which reads them), neither of which can reference the other.
    /// </summary>
    public static class AudioInputSettings
    {
        /// <summary>PlayerPrefs key holding the chosen microphone's device name; empty means automatic.</summary>
        public const string MicrophoneKey = "Settings.Microphone";

        /// <summary>PlayerPrefs key holding the microphone gain (#125).</summary>
        public const string MicGainKey = "Settings.MicGain";

        /// <summary>PlayerPrefs key for "guards hear what I say between casts". Off unless switched on.</summary>
        public const string GuardsHearChatterKey = "Settings.GuardsHearChatter";

        public const float MinMicGain = 0.25f;
        public const float MaxMicGain = 4f;

        /// <summary>
        /// How much the microphone is amplified before speech recognition and loudness see it:
        /// 1 is as recorded. A quiet microphone needs more to reach a normal or shouted cast; a hot
        /// one needs less so a normal voice does not read as a shout.
        /// </summary>
        public static float MicGain
        {
            get => Mathf.Clamp(PlayerPrefs.GetFloat(MicGainKey, 1f), MinMicGain, MaxMicGain);
            set
            {
                PlayerPrefs.SetFloat(MicGainKey, Mathf.Clamp(value, MinMicGain, MaxMicGain));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Raised when <see cref="Microphone"/> actually changes value.</summary>
        public static event Action<string> MicrophoneChanged;

        /// <summary>The chosen microphone's device name; empty means automatic.</summary>
        public static string Microphone
        {
            get => PlayerPrefs.GetString(MicrophoneKey, string.Empty);
            set
            {
                value = value ?? string.Empty;
                if (value == Microphone)
                    return;
                PlayerPrefs.SetString(MicrophoneKey, value);
                PlayerPrefs.Save();
                MicrophoneChanged?.Invoke(value);
            }
        }

        /// <summary>Raised when <see cref="GuardsHearChatter"/> actually changes value.</summary>
        public static event Action<bool> GuardsHearChatterChanged;

        /// <summary>Opt-in: keep listening between casts and let guards overhear ordinary talk. Default off.</summary>
        public static bool GuardsHearChatter
        {
            get => PlayerPrefs.GetInt(GuardsHearChatterKey, 0) == 1;
            set
            {
                if (value == GuardsHearChatter)
                    return;
                PlayerPrefs.SetInt(GuardsHearChatterKey, value ? 1 : 0);
                PlayerPrefs.Save();
                GuardsHearChatterChanged?.Invoke(value);
            }
        }
    }
}
