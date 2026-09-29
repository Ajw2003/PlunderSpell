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

        /// <summary>How far Music and Effects drop while the push-to-cast key is held (docs/plans/audio.md 1.3).</summary>
        public const float CastingDipDb = -9f;

        private const float DipInSeconds = 0.12f;
        private const float DipOutSeconds = 0.3f;

        private static AudioMixer s_mixer;
        private static float s_master = 1f;
        private static float s_music = 1f;
        private static float s_sfx = 1f;
        private static float s_castingDb;

        /// <summary>The casting dip in force right now, 0 when not casting.</summary>
        public static float CastingDb => s_castingDb;

        /// <summary>Linear 0..1 to decibels: 0 is silent (-80), 1 is unity (0), 0.5 is about -6.</summary>
        public static float LinearToDb(float linear)
        {
            if (linear <= 0.0001f)
                return SilentDb;
            return Mathf.Max(SilentDb, 20f * Mathf.Log10(Mathf.Min(linear, 1f)));
        }

        /// <summary>A slider's decibels with an offset added, never below silence.</summary>
        public static float Combine(float linear, float offsetDb) => Mathf.Max(SilentDb, LinearToDb(linear) + offsetDb);

        /// <summary>Points the sliders at a mixer and applies the saved values. Called once by the director.</summary>
        public static void Bind(AudioMixer mixer)
        {
            s_mixer = mixer;
            s_castingDb = 0f;
            s_master = PlayerPrefs.GetFloat(MasterKey, 1f);
            s_music = PlayerPrefs.GetFloat(MusicKey, 1f);
            s_sfx = PlayerPrefs.GetFloat(SfxKey, 1f);
            Push();
        }

        public static void SetMaster(float linear, bool save = true)
        {
            if (save)
                PlayerPrefs.SetFloat(MasterKey, linear);
            s_master = linear;
            Push();
        }

        public static void SetMusic(float linear, bool save = true)
        {
            if (save)
                PlayerPrefs.SetFloat(MusicKey, linear);
            s_music = linear;
            Push();
        }

        public static void SetEffects(float linear, bool save = true)
        {
            if (save)
                PlayerPrefs.SetFloat(SfxKey, linear);
            s_sfx = linear;
            Push();
        }

        /// <summary>
        /// Moves the casting dip toward on or off. A snapshot cannot do this job: measured in Unity
        /// 6000.3, a snapshot transition overwrites an exposed parameter's value, so the dip would
        /// throw away the slider. See docs/4-systems/audio.md.
        /// </summary>
        public static void TickCasting(bool casting, float deltaTime)
        {
            float target = casting ? CastingDipDb : 0f;
            if (Mathf.Approximately(s_castingDb, target))
                return;
            float rate = Mathf.Abs(CastingDipDb) / (casting ? DipInSeconds : DipOutSeconds);
            s_castingDb = Mathf.MoveTowards(s_castingDb, target, rate * deltaTime);
            Push();
        }

        private static void Push()
        {
            // Without a mixer (a scene with no audio layer) the listener volume is the only master there is.
            if (s_mixer == null)
            {
                AudioListener.volume = s_master;
                return;
            }

            AudioListener.volume = 1f;
            s_mixer.SetFloat(MasterParameter, LinearToDb(s_master));
            s_mixer.SetFloat(MusicParameter, Combine(s_music, s_castingDb));
            s_mixer.SetFloat(SfxParameter, Combine(s_sfx, s_castingDb));
            s_mixer.SetFloat(UiParameter, LinearToDb(s_sfx));
        }
    }
}
