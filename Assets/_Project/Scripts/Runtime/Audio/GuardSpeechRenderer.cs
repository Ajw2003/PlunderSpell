using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Plunderspell.Audio.VoiceBank;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Plunderspell.Audio
{
    /// <summary>
    /// Re-voices a guard line with its disguise and keeps the result, so a guard pays for each line once.
    /// <see cref="TryGet"/> renders on a worker thread and never blocks a frame; call <see cref="Pump"/> once
    /// a frame to turn finished renders into clips. <see cref="Get"/> blocks and is for tests and tools. The
    /// cache holds the last few dozen clips.
    /// </summary>
    public sealed class GuardSpeechRenderer
    {
        public const int CacheSize = 48;
        private const float PeakLevel = 0.8f;
        private const float SlowRenderMilliseconds = 40f;

        private readonly Dictionary<string, LinkedListNode<Entry>> _cache = new Dictionary<string, LinkedListNode<Entry>>();
        private readonly LinkedList<Entry> _recent = new LinkedList<Entry>();
        private readonly HashSet<string> _rendering = new HashSet<string>();
        private readonly ConcurrentQueue<Finished> _finished = new ConcurrentQueue<Finished>();

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

        private readonly struct Finished
        {
            public readonly string Key;
            public readonly float[] Samples;
            public readonly int Rate;
            public readonly Exception Error;

            public Finished(string key, float[] samples, int rate, Exception error)
            {
                Key = key;
                Samples = samples;
                Rate = rate;
                Error = error;
            }
        }

        public int CachedCount => _cache.Count;

        /// <summary>Renders still running on worker threads.</summary>
        public int RenderingCount => _rendering.Count;

        /// <summary>
        /// The re-voiced clip if it is ready. Otherwise starts rendering it on a worker thread (once) and
        /// returns false; ask again after <see cref="Pump"/> has run on a later frame.
        /// </summary>
        public bool TryGet(AudioClip source, DisguiseProfile profile, string profileKey, out AudioClip clip)
        {
            string key = profileKey + "|" + source.name;
            if (TryCached(key, out clip))
                return true;
            if (_rendering.Contains(key))
                return false;

            float[] samples = ReadSamples(source);
            if (samples == null)
                return false;

            int rate = source.frequency;
            _rendering.Add(key);
            Task.Run(() =>
            {
                try
                {
                    _finished.Enqueue(new Finished(key, RenderSamples(samples, rate, profile), rate, null));
                }
                catch (Exception error)
                {
                    _finished.Enqueue(new Finished(key, null, rate, error));
                }
            });
            return false;
        }

        /// <summary>Turns renders that finished on worker threads into clips. Main thread, once a frame.</summary>
        public void Pump()
        {
            while (_finished.TryDequeue(out Finished done))
            {
                _rendering.Remove(done.Key);
                if (done.Error != null)
                {
                    Debug.LogError("[GuardSpeech] Rendering '" + done.Key + "' failed: " + done.Error);
                    continue;
                }
                Store(done.Key, MakeClip(done.Key, done.Samples, done.Rate));
            }
        }

        /// <summary>Blocks until the clip is rendered. For tests and tools, never for frame code.</summary>
        public AudioClip Get(AudioClip source, DisguiseProfile profile, string profileKey)
        {
            string key = profileKey + "|" + source.name;
            if (TryCached(key, out AudioClip hit))
                return hit;

            float[] samples = ReadSamples(source);
            if (samples == null)
                return null;

            var timer = Stopwatch.StartNew();
            AudioClip rendered = MakeClip(key, RenderSamples(samples, source.frequency, profile), source.frequency);
            timer.Stop();
            if (timer.Elapsed.TotalMilliseconds > SlowRenderMilliseconds)
                Debug.LogWarning("[GuardSpeech] Rendering '" + source.name + "' took " + timer.Elapsed.TotalMilliseconds.ToString("F0") + " ms on the main thread.");
            Store(key, rendered);
            return rendered;
        }

        private bool TryCached(string key, out AudioClip clip)
        {
            if (_cache.TryGetValue(key, out LinkedListNode<Entry> hit))
            {
                _recent.Remove(hit);
                _recent.AddFirst(hit);
                clip = hit.Value.Clip;
                return true;
            }
            clip = null;
            return false;
        }

        private void Store(string key, AudioClip clip)
        {
            _cache[key] = _recent.AddFirst(new Entry(key, clip));
            while (_cache.Count > CacheSize)
            {
                Entry oldest = _recent.Last.Value;
                _recent.RemoveLast();
                _cache.Remove(oldest.Key);
                DestroyClip(oldest.Clip);
            }
        }

        private static float[] ReadSamples(AudioClip source)
        {
            var samples = new float[source.samples * source.channels];
            if (samples.Length == 0 || !source.GetData(samples, 0))
            {
                Debug.LogWarning("[GuardSpeech] Cannot read samples of '" + source.name + "'; is it imported Decompress On Load?");
                return null;
            }
            return samples;
        }

        // Pure array work, safe on a worker thread.
        private static float[] RenderSamples(float[] samples, int rate, DisguiseProfile profile)
        {
            float[] output = VoiceDisguise.Render(samples, rate, profile);
            float peak = 0f;
            for (int i = 0; i < output.Length; i++)
                peak = Math.Max(peak, Math.Abs(output[i]));
            if (peak > 0f)
            {
                float gain = PeakLevel / peak;
                for (int i = 0; i < output.Length; i++)
                    output[i] *= gain;
            }
            return output;
        }

        private static AudioClip MakeClip(string key, float[] samples, int rate)
        {
            AudioClip clip = AudioClip.Create(key, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static void DestroyClip(AudioClip clip)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(clip);
            else
                UnityEngine.Object.DestroyImmediate(clip);
        }
    }
}
