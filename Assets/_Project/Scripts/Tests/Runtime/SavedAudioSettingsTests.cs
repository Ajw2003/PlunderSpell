using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Plunderspell.Audio;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>
    /// The volume sliders show the saved value, so the sound itself must already be at that value without
    /// the player touching a slider (#181). These read the mixer's real exposed values a few frames after
    /// start-up, and after Windows output routing restarts the audio.
    /// </summary>
    public class SavedAudioSettingsTests
    {
        private const float Master = 0.3f;
        private const float Music = 0.4f;
        private const float Effects = 0.5f;
        private const float ToleranceDb = 0.5f;

        private static readonly string[] Keys = { AudioLevels.MasterKey, AudioLevels.MusicKey, AudioLevels.SfxKey };

        private readonly float[] _savedValues = new float[3];
        private readonly bool[] _hadValues = new bool[3];
        private AudioMixer _mixer;

        [SetUp]
        public void SavePrefsAndFindMixer()
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                _hadValues[i] = PlayerPrefs.HasKey(Keys[i]);
                _savedValues[i] = PlayerPrefs.GetFloat(Keys[i], 1f);
            }

            SoundBank bank = null;
            SoundBank[] loaded = Resources.FindObjectsOfTypeAll<SoundBank>();
            if (loaded.Length > 0)
                bank = loaded[0];
#if UNITY_EDITOR
            // A headless test run may not have preloaded the bank; the asset file itself is enough for the mixer.
            if (bank == null)
                bank = UnityEditor.AssetDatabase.LoadAssetAtPath<SoundBank>("Assets/_Project/Audio/SoundBank.asset");
#endif
            Assert.IsNotNull(bank, "No SoundBank found; run Plunderspell > Audio > Rebuild SoundBank.");
            _mixer = bank.Mixer;
            Assert.IsNotNull(_mixer, "The SoundBank has no mixer.");
        }

        [TearDown]
        public void RestorePrefsAndMixer()
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                if (_hadValues[i])
                    PlayerPrefs.SetFloat(Keys[i], _savedValues[i]);
                else
                    PlayerPrefs.DeleteKey(Keys[i]);
            }

            AudioLevels.Bind(_mixer);
        }

        private void SaveTestVolumes()
        {
            PlayerPrefs.SetFloat(AudioLevels.MasterKey, Master);
            PlayerPrefs.SetFloat(AudioLevels.MusicKey, Music);
            PlayerPrefs.SetFloat(AudioLevels.SfxKey, Effects);
        }

        private float MixerDb(string parameter)
        {
            Assert.IsTrue(_mixer.GetFloat(parameter, out float value), "The mixer does not expose " + parameter);
            return value;
        }

        private void LogMixer(string when)
        {
            Debug.Log($"[SavedAudioSettings] {when}: master {MixerDb(AudioLevels.MasterParameter):0.00} dB, " +
                      $"music {MixerDb(AudioLevels.MusicParameter):0.00} dB, effects {MixerDb(AudioLevels.SfxParameter):0.00} dB, " +
                      $"listener {AudioListener.volume:0.00}");
        }

        private void AssertSavedVolumesApplied()
        {
            Assert.AreEqual(AudioLevels.LinearToDb(Master), MixerDb(AudioLevels.MasterParameter), ToleranceDb, "Master does not follow the saved value.");
            Assert.AreEqual(AudioLevels.LinearToDb(Music), MixerDb(AudioLevels.MusicParameter), ToleranceDb, "Music does not follow the saved value.");
            Assert.AreEqual(AudioLevels.LinearToDb(Effects), MixerDb(AudioLevels.SfxParameter), ToleranceDb, "Effects does not follow the saved value.");
            Assert.AreEqual(1f, AudioListener.volume, 0.001f, "With a mixer, the listener volume must stay at 1.");
        }

        private IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        [UnityTest]
        public IEnumerator Test_SavedVolumesReachTheMixerAfterStartUp()
        {
            SaveTestVolumes();
            AudioLevels.Bind(_mixer);
            LogMixer("frame 0");
            for (int frame = 1; frame <= 5; frame++)
            {
                yield return null;
                LogMixer("frame " + frame);
            }

            AssertSavedVolumesApplied();
        }

        private void KnockMixerOffTheSavedValues()
        {
            // What a mixer that resets itself when it first goes live (or after an audio restart) would do.
            _mixer.SetFloat(AudioLevels.MasterParameter, 0f);
            _mixer.SetFloat(AudioLevels.MusicParameter, 0f);
            _mixer.SetFloat(AudioLevels.SfxParameter, 0f);
            _mixer.SetFloat(AudioLevels.UiParameter, 0f);
            Assert.AreEqual(0f, MixerDb(AudioLevels.MasterParameter), 0.001f, "The test could not move the mixer off the saved values.");
        }

        [Test]
        public void Test_VolumesTheMixerDroppedArePutBackAndReported()
        {
            SaveTestVolumes();
            AudioLevels.Bind(_mixer);
            KnockMixerOffTheSavedValues();

            LogAssert.Expect(LogType.Warning, new Regex("dropped the saved volumes"));
            AudioLevels.Settle(0.016f);

            AssertSavedVolumesApplied();
        }

        [Test]
        public void Test_VolumesAreAppliedAgainWhenTheAudioConfigurationChanges()
        {
            SaveTestVolumes();
            AudioLevels.Bind(_mixer);
            KnockMixerOffTheSavedValues();

            AudioLevels.HandleConfigurationChanged(false);

            AssertSavedVolumesApplied();
        }

        [Test]
        public void Test_AfterTheSettleWindowTheMixerIsLeftAlone()
        {
            SaveTestVolumes();
            AudioLevels.Bind(_mixer);
            AudioLevels.Settle(AudioLevels.SettleSeconds + 0.1f);
            _mixer.SetFloat(AudioLevels.MasterParameter, -3f);

            AudioLevels.Settle(0.016f);

            Assert.AreEqual(-3f, MixerDb(AudioLevels.MasterParameter), 0.001f, "Settling must stop after the window so it never fights the sliders.");
        }

        [UnityTest]
        public IEnumerator Test_SavedVolumesHoldOnceASoundStartsPlaying()
        {
            SaveTestVolumes();
            AudioLevels.Bind(_mixer);
            LogMixer("bound, nothing playing");

            // The menu music is the first sound routed through the mixer; a mixer that is only built when
            // something plays through it can drop what was set before that.
            var go = new GameObject("SavedAudioSettingsTone");
            var source = go.AddComponent<AudioSource>();
            var clip = AudioClip.Create("tone", 4410, 1, 44100, false);
            var data = new float[4410];
            for (int i = 0; i < data.Length; i++)
                data[i] = 0.1f * Mathf.Sin(i * 0.1f);
            clip.SetData(data, 0);
            source.clip = clip;
            source.loop = true;
            source.outputAudioMixerGroup = _mixer.FindMatchingGroups("Master")[0];
            source.Play();
            try
            {
                for (int frame = 1; frame <= 10; frame++)
                {
                    yield return null;
                    LogMixer("playing +" + frame + (source.isPlaying ? " (source playing)" : " (source NOT playing)"));
                }

                AssertSavedVolumesApplied();
            }
            finally
            {
                Object.Destroy(go);
                Object.Destroy(clip);
            }
        }

        [UnityTest]
        public IEnumerator Test_SavedVolumesSurviveAnAudioRestart()
        {
            SaveTestVolumes();
            AudioLevels.Bind(_mixer);
            yield return Frames(2);
            LogMixer("before restart");

            // What AudioOutputDevices.RestartAudio does after routing to the saved device.
            AudioSettings.Reset(AudioSettings.GetConfiguration());
            LogMixer("restart, same frame");
            for (int frame = 1; frame <= 5; frame++)
            {
                yield return null;
                LogMixer("restart +" + frame);
            }

            AssertSavedVolumesApplied();
        }
    }
}
