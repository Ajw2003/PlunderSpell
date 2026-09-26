using System;

namespace Plunderspell.Voice
{
    /// <summary>
    /// Loudness bucket a phrase was spoken at. Drives spell modifiers:
    /// Whisper reduces radius / bypasses acoustic detection, Shout amplifies
    /// the effect and emits extra acoustic noise.
    /// </summary>
    public enum CastVolume
    {
        Whisper = 0,
        Normal = 1,
        Shout = 2
    }

    /// <summary>
    /// The full payload produced when a voice provider recognises a phrase.
    /// Immutable value type so it can be handed across threads / events safely.
    /// </summary>
    public struct VoiceRecognitionResult
    {
        /// <summary>Raw text exactly as the recogniser returned it.</summary>
        public string RawText;

        /// <summary>Uppercased, punctuation-stripped, single-spaced text used for lexicon matching.</summary>
        public string NormalizedText;

        /// <summary>Recogniser confidence 0..1 (providers that don't expose it report 1f).</summary>
        public float Confidence;

        /// <summary>Root-mean-square amplitude of the captured audio buffer 0..1.</summary>
        public float RmsAmplitude;

        /// <summary>Loudness classification derived from <see cref="RmsAmplitude"/>.</summary>
        public CastVolume Volume;

        /// <summary>True when a number key produced the phrase rather than speech. A keyed cast is
        /// chanted for a moment before it fires, so it is never faster than saying the word (#116).</summary>
        public bool FromKeyboard;

        public VoiceRecognitionResult(string rawText, string normalizedText, float confidence,
            float rmsAmplitude, CastVolume volume, bool fromKeyboard = false)
        {
            RawText = rawText;
            NormalizedText = normalizedText;
            Confidence = confidence;
            RmsAmplitude = rmsAmplitude;
            Volume = volume;
            FromKeyboard = fromKeyboard;
        }

        public override string ToString() =>
            $"\"{NormalizedText}\" (conf={Confidence:0.00}, rms={RmsAmplitude:0.00}, vol={Volume})";
    }

    /// <summary>
    /// Abstraction over any speech-to-text source (Vosk on Windows, a keyboard mock
    /// in the editor / headless CI). Consumers (PushToCastController, SpellCastingSystem)
    /// only ever talk to this interface via <see cref="VoiceServiceLocator"/>.
    /// </summary>
    public interface IVoiceInputService
    {
        /// <summary>True between StartListening() and StopListening().</summary>
        bool IsListening { get; }

        /// <summary>Begin capturing audio / key input.</summary>
        void StartListening();

        /// <summary>Stop capturing and flush any pending recognition.</summary>
        void StopListening();

        /// <summary>Raised (on the main thread) whenever a phrase is recognised.</summary>
        event Action<VoiceRecognitionResult> OnPhraseRecognized;
    }

    /// <summary>
    /// Shared helpers for turning raw amplitude / text into normalised forms so every
    /// provider classifies volume and normalises text identically.
    /// </summary>
    public static class VoiceUtility
    {
        public const float WhisperThreshold = 0.1f;
        public const float ShoutThreshold = 0.4f;

        public static CastVolume ClassifyVolume(float rms)
        {
            if (rms < WhisperThreshold) return CastVolume.Whisper;
            if (rms > ShoutThreshold) return CastVolume.Shout;
            return CastVolume.Normal;
        }

        /// <summary>Uppercase, strip punctuation, collapse whitespace.</summary>
        public static string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var sb = new System.Text.StringBuilder(raw.Length);
            bool lastWasSpace = false;
            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToUpperInvariant(c));
                    lastWasSpace = false;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace && sb.Length > 0)
                    {
                        sb.Append(' ');
                        lastWasSpace = true;
                    }
                }
                // punctuation is dropped
            }
            return sb.ToString().Trim();
        }

        /// <summary>Compute RMS amplitude of a float PCM buffer in the range 0..1.</summary>
        public static float ComputeRms(float[] samples, int count)
        {
            if (samples == null || count <= 0) return 0f;
            double sum = 0.0;
            for (int i = 0; i < count; i++)
            {
                float s = samples[i];
                sum += (double)s * s;
            }
            return (float)System.Math.Sqrt(sum / count);
        }
    }
}
