using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The Ophan's arrival hum (#434's warning, approved 2026-10-27: "a loud monstrous hum"): a low drone built in code
    /// as a stand-in until real audio exists. Three low tones a little out of tune with each other beat against one
    /// another, swell in, hold, and fade. Built once and reused.
    /// </summary>
    public static class OphanHum
    {
        public const float Seconds = 5f;
        private const int SampleRate = 44100;
        private static readonly float[] Tones = { 41.2f, 43.7f, 82.4f, 123.5f };
        private static readonly float[] Levels = { 0.5f, 0.35f, 0.25f, 0.12f };

        private static AudioClip s_clip;

        public static AudioClip Clip => s_clip != null ? s_clip : s_clip = Build();

        /// <summary>Plays the hum on its own flat (not positional) source under <paramref name="parent"/>: it is meant
        /// to fill the whole sky, not come from one point.</summary>
        public static AudioSource Play(GameObject parent)
        {
            var source = parent.AddComponent<AudioSource>();
            source.clip = Clip;
            source.spatialBlend = 0f;
            source.volume = 1f;
            source.playOnAwake = false;
            source.Play();
            return source;
        }

        private static AudioClip Build()
        {
            int count = (int)(Seconds * SampleRate);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)SampleRate;
                float sum = 0f;
                for (int k = 0; k < Tones.Length; k++)
                    sum += Levels[k] * Mathf.Sin(2f * Mathf.PI * Tones[k] * t);
                float swell = Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((Seconds - t) / 1.5f);
                float throb = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 2.5f * t);
                // A soft clip makes it growl rather than whistle when it is loud.
                samples[i] = (float)System.Math.Tanh(1.8f * sum * swell * throb) * 0.9f;
            }
            AudioClip clip = AudioClip.Create("OphanHum", count, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
