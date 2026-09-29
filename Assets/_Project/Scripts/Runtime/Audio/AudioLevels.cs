using UnityEngine;
using UnityEngine.Audio;

namespace Plunderspell.Audio
{
    /// <summary>The volume sliders in Settings, mapped onto the mixer's exposed parameters.</summary>
    public static class AudioLevels
    {
        public const string MasterKey = "Settings.MasterVolume";
        public const string MusicKey = "Settings.MusicVolume";
        public const string SfxKey = "Settings.SfxVolume";

        public const string MasterParameter = "MasterVolume";
        public const string MusicParameter = "MusicVolume";
        public const string SfxParameter = "SfxVolume";

        /// <summary>The UI group follows the Effects slider; it has its own parameter so the Casting snapshot cannot fight it.</summary>
        public const string UiParameter = "UiVolume";

        public const float SilentDb = -80f;

        private static AudioMixer s_mixer;

        /// <summary>Linear 0..1 to decibels: 0 is silent (-80), 1 is unity (0), 0.5 is about -6.</summary>
        public static float LinearToDb(float linear)
        {
            if (linear <= 0.0001f)
                return SilentDb;
            return Mathf.Max(SilentDb, 20f * Mathf.Log10(Mathf.Min(linear, 1f)));
        }

        /// <summary>Points the sliders at a mixer and applies the saved values. Called once by the director.</summary>
        public static void Bind(AudioMixer mixer)
        {
            s_mixer = mixer;
            SetMaster(PlayerPrefs.GetFloat(MasterKey, 1f), save: false);
            SetMusic(PlayerPrefs.GetFloat(MusicKey, 1f), save: false);
            SetEffects(PlayerPrefs.GetFloat(SfxKey, 1f), save: false);
        }

        public static void SetMaster(float linear, bool save = true)
        {
            if (save)
                PlayerPrefs.SetFloat(MasterKey, linear);

            // Without a mixer (a scene with no audio layer) the listener volume is the only master there is.
            if (s_mixer != null)
            {
                AudioListener.volume = 1f;
                s_mixer.SetFloat(MasterParameter, LinearToDb(linear));
            }
            else
            {
                AudioListener.volume = linear;
            }
        }

        public static void SetMusic(float linear, bool save = true)
        {
            if (save)
                PlayerPrefs.SetFloat(MusicKey, linear);
            if (s_mixer != null)
                s_mixer.SetFloat(MusicParameter, LinearToDb(linear));
        }

        public static void SetEffects(float linear, bool save = true)
        {
            if (save)
                PlayerPrefs.SetFloat(SfxKey, linear);
            if (s_mixer == null)
                return;
            float db = LinearToDb(linear);
            s_mixer.SetFloat(SfxParameter, db);
            s_mixer.SetFloat(UiParameter, db);
        }
    }
}
