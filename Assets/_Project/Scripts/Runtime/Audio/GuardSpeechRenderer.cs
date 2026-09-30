using System.Collections.Generic;
using System.Diagnostics;
using Plunderspell.Audio.VoiceBank;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Plunderspell.Audio
{
    /// <summary>
    /// Re-voices a guard line with its disguise and keeps the result, so a guard pays for each line once.
    /// Runs on the main thread at first use; the cache holds the last few dozen clips.
    /// </summary>
    public sealed class GuardSpeechRenderer
    {
        public const int CacheSize = 48;
        private const float PeakLevel = 0.8f;
        private const float SlowRenderMilliseconds = 40f;

        private readonly Dictionary<string, LinkedListNode<Entry>> _cache = new Dictionary<string, LinkedListNode<Entry>>();
        private readonly LinkedList<Entry> _recent = new LinkedList<Entry>();

        private readonly struct Entry
        {
            public readonly string Key;
            public readonly AudioClip Clip;

            public Entry(string key, AudioClip clip)
            {
                Key = key;
                Clip = clip;
            }
        }

        public int CachedCount => _cache.Count;

        public AudioClip Get(AudioClip source, DisguiseProfile profile, string profileKey)
        {
            string key = profileKey + "|" + source.name;
            if (_cache.TryGetValue(key, out LinkedListNode<Entry> hit))
            {
                _recent.Remove(hit);
                _recent.AddFirst(hit);
                return hit.Value.Clip;
            }

            AudioClip rendered = Render(source, profile, key);
            if (rendered == null)
                return null;

            _cache[key] = _recent.AddFirst(new Entry(key, rendered));
            while (_cache.Count > CacheSize)
            {
                Entry oldest = _recent.Last.Value;
                _recent.RemoveLast();
                _cache.Remove(oldest.Key);
                DestroyClip(oldest.Clip);
            }
            return rendered;
        }

        private static AudioClip Render(AudioClip source, DisguiseProfile profile, string key)
        {
            var timer = Stopwatch.StartNew();
            var samples = new float[source.samples * source.channels];
            if (samples.Length == 0 || !source.GetData(samples, 0))
            {
                Debug.LogWarning("[GuardSpeech] Cannot read samples of '" + source.name + "'; is it imported Decompress On Load?");
                return null;
            }

            float[] output = VoiceDisguise.Render(samples, source.frequency, profile);
            float peak = 0f;
            for (int i = 0; i < output.Length; i++)
                peak = Mathf.Max(peak, Mathf.Abs(output[i]));
            if (peak > 0f)
            {
                float gain = PeakLevel / peak;
                for (int i = 0; i < output.Length; i++)
                    output[i] *= gain;
            }

            AudioClip clip = AudioClip.Create(key, output.Length, 1, source.frequency, false);
            clip.SetData(output, 0);

            timer.Stop();
            if (timer.Elapsed.TotalMilliseconds > SlowRenderMilliseconds)
                Debug.LogWarning("[GuardSpeech] Rendering '" + source.name + "' took " + timer.Elapsed.TotalMilliseconds.ToString("F0") + " ms.");
            return clip;
        }

        private static void DestroyClip(AudioClip clip)
        {
            if (Application.isPlaying)
                Object.Destroy(clip);
            else
                Object.DestroyImmediate(clip);
        }
    }
}
