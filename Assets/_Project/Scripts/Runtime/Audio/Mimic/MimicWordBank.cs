using System;
using System.Collections.Generic;
using System.Text;
using Plunderspell.Voice;

namespace Plunderspell.Audio.Mimic
{
    /// <summary>
    /// Guard mimic prototype: every word a player said between casts, cut out of their recording.
    /// Word to at most <see cref="ClipsPerWord"/> clips (the newest replace the oldest), at most
    /// <see cref="MaxWords"/> words. Plain C#; lives for the play session.
    /// </summary>
    public sealed class MimicWordBank
    {
        public const int ClipsPerWord = 4;
        public const int MaxWords = 300;
        private const float Pad = 0.03f;
        private const float MinClip = 0.12f;
        private const float MaxClip = 1.2f;
        private const float MinConfidence = 0.5f;
        private const float FadeSeconds = 0.01f;

        private readonly Dictionary<string, List<float[]>> _clips = new Dictionary<string, List<float[]>>();

        public int WordCount => _clips.Count;
        public IEnumerable<string> Words => _clips.Keys;
        public bool Has(string word) => _clips.ContainsKey(word);

        /// <summary>Cuts each confidently heard word out of the report's audio and keeps it. Returns how many were added.</summary>
        public int Add(ChatterReport report)
        {
            int added = 0;
            foreach (WordTiming timing in report.Words)
            {
                if (timing.Confidence < MinConfidence)
                    continue;
                string word = Clean(timing.Word);
                if (word.Length == 0)
                    continue;
                float[] clip = Cut(report.Samples, timing.Start - Pad, timing.End + Pad, ChatterReport.SampleRate);
                if (clip == null)
                    continue;
                AddClip(word, clip);
                added++;
            }
            return added;
        }

        public void AddClip(string word, float[] clip)
        {
            if (!_clips.TryGetValue(word, out List<float[]> list))
            {
                if (_clips.Count >= MaxWords)
                    return;
                list = new List<float[]>();
                _clips[word] = list;
            }
            list.Add(clip);
            if (list.Count > ClipsPerWord)
                list.RemoveAt(0);
        }

        /// <summary>One of the word's clips at random, or null when the bank does not have it.</summary>
        public float[] Pick(string word, Random random)
        {
            if (!_clips.TryGetValue(word, out List<float[]> list) || list.Count == 0)
                return null;
            return list[random.Next(list.Count)];
        }

        /// <summary>Lower case letters and apostrophes only, so a word is safe in the model's grammar.</summary>
        public static string Clean(string word)
        {
            var sb = new StringBuilder(word?.Length ?? 0);
            if (word != null)
            {
                foreach (char c in word.ToLowerInvariant())
                {
                    if ((c >= 'a' && c <= 'z') || c == '\'')
                        sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>The samples between two times, faded in and out; null when too short, too long or out of range.</summary>
        public static float[] Cut(float[] samples, float start, float end, int rate)
        {
            if (samples == null)
                return null;
            int from = Math.Max(0, (int)(start * rate));
            int to = Math.Min(samples.Length, (int)(end * rate));
            int length = to - from;
            if (length < MinClip * rate || length > MaxClip * rate)
                return null;
            var clip = new float[length];
            Array.Copy(samples, from, clip, 0, length);
            int fade = Math.Min(length / 2, (int)(FadeSeconds * rate));
            for (int i = 0; i < fade; i++)
            {
                float gain = i / (float)fade;
                clip[i] *= gain;
                clip[length - 1 - i] *= gain;
            }
            return clip;
        }
    }
}
