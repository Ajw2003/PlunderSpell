using NUnit.Framework;
using Plunderspell.Core;
using Plunderspell.Voice;
using UnityEngine;

namespace Plunderspell.Tests
{
    /// <summary>
    /// The Settings microphone gain (#125): it turns a quiet microphone up before loudness is judged,
    /// clips rather than wraps, and stays inside its range.
    /// </summary>
    public class MicGainTests
    {
        private float _savedGain;
        private bool _hadGain;

        [SetUp]
        public void SaveGain()
        {
            _hadGain = PlayerPrefs.HasKey(AudioInputSettings.MicGainKey);
            _savedGain = PlayerPrefs.GetFloat(AudioInputSettings.MicGainKey, 1f);
        }

        [TearDown]
        public void RestoreGain()
        {
            if (_hadGain)
                PlayerPrefs.SetFloat(AudioInputSettings.MicGainKey, _savedGain);
            else
                PlayerPrefs.DeleteKey(AudioInputSettings.MicGainKey);
        }

        private static float[] Tone(float amplitude, int count = 1600)
        {
            var samples = new float[count];
            for (int i = 0; i < count; i++)
                samples[i] = amplitude * Mathf.Sin(i * 0.3f);
            return samples;
        }

        [Test]
        public void Test_GainTurnsAQuietMicrophoneUpToANormalCast()
        {
            float[] voice = Tone(0.1f); // RMS about 0.07: a whisper as recorded
            Assert.AreEqual(CastVolume.Whisper, VoiceUtility.ClassifyVolume(VoiceUtility.ComputeRms(voice, voice.Length)));

            VoiceUtility.ApplyGain(voice, voice.Length, 3f);

            Assert.AreEqual(CastVolume.Normal, VoiceUtility.ClassifyVolume(VoiceUtility.ComputeRms(voice, voice.Length)),
                "Three times the gain must lift that voice to a normal cast.");
        }

        [Test]
        public void Test_GainBelowOneQuietsAHotMicrophone()
        {
            float[] voice = Tone(0.7f); // a normal voice on a hot mic reads as a shout
            Assert.AreEqual(CastVolume.Shout, VoiceUtility.ClassifyVolume(VoiceUtility.ComputeRms(voice, voice.Length)));

            VoiceUtility.ApplyGain(voice, voice.Length, 0.5f);

            Assert.AreEqual(CastVolume.Normal, VoiceUtility.ClassifyVolume(VoiceUtility.ComputeRms(voice, voice.Length)));
        }

        [Test]
        public void Test_GainClipsAtFullScale()
        {
            float[] voice = Tone(0.9f);
            VoiceUtility.ApplyGain(voice, voice.Length, 4f);

            foreach (float s in voice)
                Assert.LessOrEqual(Mathf.Abs(s), 1f, "Amplified samples clip at full scale, as a preamp would.");
        }

        [Test]
        public void Test_TheSavedGainStaysInRange()
        {
            AudioInputSettings.MicGain = 100f;
            Assert.AreEqual(AudioInputSettings.MaxMicGain, AudioInputSettings.MicGain);

            AudioInputSettings.MicGain = 0f;
            Assert.AreEqual(AudioInputSettings.MinMicGain, AudioInputSettings.MicGain);

            PlayerPrefs.DeleteKey(AudioInputSettings.MicGainKey);
            Assert.AreEqual(1f, AudioInputSettings.MicGain, "No saved gain means the microphone as recorded.");
        }
    }
}
