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

        /// <summary>
        /// How long after binding, and after the audio restarts, the saved values are checked against the
        /// mixer every frame and put back if the mixer has dropped them.
        /// </summary>
        public const float SettleSeconds = 2f;

        private const float ToleranceDb = 0.25f;
        private const int MaxWarnings = 5;

        private static AudioMixer s_mixer;
        private static float s_master = 1f;
        private static float s_music = 1f;
        private static float s_sfx = 1f;
        private static float s_castingDb;
        private static float s_keepPushingUntil;

        /// <summary>
        /// How long, after the mixer is bound or the audio engine restarts, the saved values are pushed
        /// again every frame. Measured 2026-09-30 (#181): a SetFloat made in the start-up frame is
        /// lost, the mixer sits at 0 dB until the next SetFloat; later pushes stick.
        /// </summary>
        public const float StartupPushSeconds = 1.5f;
        private static float s_settleLeft;
        private static bool s_watchingConfiguration;
        private static int s_warnings;

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
            s_warnings = 0;
            Push();
            KeepPushing();
            s_settleLeft = s_mixer != null ? SettleSeconds : 0f;

            // A device change or an audio restart can reset the mixer's exposed values; apply them again.
            if (!s_watchingConfiguration)
            {
                AudioSettings.OnAudioConfigurationChanged += HandleConfigurationChanged;
                s_watchingConfiguration = true;
            }
        }

        /// <summary>Pushes the current values now and again each frame for <see cref="StartupPushSeconds"/>; call after anything that may reset the mixer.</summary>
        public static void KeepPushing()
        {
            s_keepPushingUntil = Time.unscaledTime + StartupPushSeconds;
            Push();
        }

        /// <summary>Called every frame by the director: re-pushes while the start-up window is open.</summary>
        public static void TickStartup()
        {
            if (Time.unscaledTime < s_keepPushingUntil)
                Push();
        }

        /// <summary>Test seam: forgets the bound mixer and cached values, as a fresh process would.</summary>
        public static void ResetForFreshLaunch()
        {
            s_mixer = null;
            s_master = s_music = s_sfx = 1f;
            s_castingDb = 0f;
            s_keepPushingUntil = 0f;
            s_settleLeft = 0f;
            s_warnings = 0;
        }

        /// <summary>
        /// Called every frame by the director. For <see cref="SettleSeconds"/> after binding or an audio
        /// restart it reads the mixer back and, if a value differs from the saved one, logs it and sets it again.
        /// </summary>
        public static void Settle(float deltaTime)
        {
            if (s_mixer == null || s_settleLeft <= 0f)
                return;
            s_settleLeft -= deltaTime;
            VerifyMixer();
        }

        /// <summary>The audio was restarted or its device changed: the mixer may have lost the saved values.</summary>
        public static void HandleConfigurationChanged(bool deviceWasChanged)
        {
            if (s_mixer == null)
                return;
            Push();
            s_settleLeft = SettleSeconds;
        }

        public static void SetMaster(float linear, bool save = true)
        {
            if (save)
                Save(MasterKey, linear);
            s_master = linear;
            Push();
        }

        public static void SetMusic(float linear, bool save = true)
        {
            if (save)
                Save(MusicKey, linear);
            s_music = linear;
            Push();
        }

        public static void SetEffects(float linear, bool save = true)
        {
            if (save)
                Save(SfxKey, linear);
            s_sfx = linear;
            Push();
        }

        private static void Save(string key, float linear)
        {
            PlayerPrefs.SetFloat(key, linear);
            PlayerPrefs.Save();
        }

        private static void VerifyMixer()
        {
            if (Differs(MasterParameter, LinearToDb(s_master)) || Differs(MusicParameter, Combine(s_music, s_castingDb))
                || Differs(SfxParameter, Combine(s_sfx, s_castingDb)) || Differs(UiParameter, LinearToDb(s_sfx)))
            {
                if (s_warnings < MaxWarnings)
                {
                    s_warnings++;
                    Debug.LogWarning("[Audio] The mixer dropped the saved volumes; applying them again. " + DescribeMixer());
                }
                Push();
            }
        }

        private static bool Differs(string parameter, float expectedDb) =>
            !s_mixer.GetFloat(parameter, out float actual) || Mathf.Abs(actual - expectedDb) > ToleranceDb;

        private static string DescribeMixer()
        {
            s_mixer.GetFloat(MasterParameter, out float master);
            s_mixer.GetFloat(MusicParameter, out float music);
            s_mixer.GetFloat(SfxParameter, out float sfx);
            return $"Mixer master {master:0.0} dB (want {LinearToDb(s_master):0.0}), music {music:0.0} dB (want {Combine(s_music, s_castingDb):0.0}), " +
                   $"effects {sfx:0.0} dB (want {Combine(s_sfx, s_castingDb):0.0}).";
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
