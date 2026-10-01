using System.Collections;
using NUnit.Framework;
using Plunderspell.Audio;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plunderspell.Tests
{
    /// <summary>#181: a simulated fresh launch must leave the mixer carrying the saved volumes.</summary>
    public class SavedSettingsStartupTests
    {
        private float _master, _music, _sfx;

        [SetUp]
        public void Save()
        {
            _master = PlayerPrefs.GetFloat(AudioLevels.MasterKey, 1f);
            _music = PlayerPrefs.GetFloat(AudioLevels.MusicKey, 1f);
            _sfx = PlayerPrefs.GetFloat(AudioLevels.SfxKey, 1f);
        }

        [TearDown]
        public void Restore()
        {
            PlayerPrefs.SetFloat(AudioLevels.MasterKey, _master);
            PlayerPrefs.SetFloat(AudioLevels.MusicKey, _music);
            PlayerPrefs.SetFloat(AudioLevels.SfxKey, _sfx);
            var bank = Resources.FindObjectsOfTypeAll<SoundBank>();
            if (bank.Length > 0)
                AudioLevels.Bind(bank[0].Mixer);
        }

        [UnityTest]
        public IEnumerator FreshLaunchAppliesSavedVolumesToTheMixer()
        {
            SoundBank bank = Resources.FindObjectsOfTypeAll<SoundBank>()[0];
            PlayerPrefs.SetFloat(AudioLevels.MasterKey, 0.5f);
            PlayerPrefs.SetFloat(AudioLevels.MusicKey, 0.25f);
            PlayerPrefs.SetFloat(AudioLevels.SfxKey, 0.75f);

            // A fresh launch: the mixer's live values are wiped, then the start-up path runs.
            bank.Mixer.ClearFloat(AudioLevels.MasterParameter);
            bank.Mixer.ClearFloat(AudioLevels.MusicParameter);
            bank.Mixer.ClearFloat(AudioLevels.SfxParameter);
            bank.Mixer.ClearFloat(AudioLevels.UiParameter);
            AudioLevels.ResetForFreshLaunch();
            var go = new GameObject("FreshLaunchAudio");
            try
            {
                go.AddComponent<AudioDirector>().Initialize(bank, withSceneLayers: false);
                AudioOutputDevices.ApplySaved();

                // What the real launch does: the start-up frame's push is lost and the mixer sits at 0 dB.
                bank.Mixer.SetFloat(AudioLevels.MasterParameter, 0f);
                bank.Mixer.SetFloat(AudioLevels.MusicParameter, 0f);
                bank.Mixer.SetFloat(AudioLevels.SfxParameter, 0f);
                bank.Mixer.SetFloat(AudioLevels.UiParameter, 0f);
                yield return null;
                yield return null;

                AssertDb(bank, AudioLevels.MasterParameter, AudioLevels.LinearToDb(0.5f));
                AssertDb(bank, AudioLevels.MusicParameter, AudioLevels.LinearToDb(0.25f));
                AssertDb(bank, AudioLevels.SfxParameter, AudioLevels.LinearToDb(0.75f));
                AssertDb(bank, AudioLevels.UiParameter, AudioLevels.LinearToDb(0.75f));
            }
            finally
            {
                Object.Destroy(go);
            }
        }

        private static void AssertDb(SoundBank bank, string parameter, float expected)
        {
            Assert.IsTrue(bank.Mixer.GetFloat(parameter, out float db), parameter);
            Assert.AreEqual(expected, db, 0.05f, parameter + " in the live mixer");
        }
    }
}
