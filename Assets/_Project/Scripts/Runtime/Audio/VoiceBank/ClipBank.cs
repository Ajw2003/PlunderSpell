using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Plunderspell.Voice;

namespace Plunderspell.Audio.VoiceBank
{
    /// <summary>What a caller wants from the bank: a length range and which loudness classes are acceptable.</summary>
    public readonly struct ClipFilter
    {
        public ClipFilter(float minSeconds, float maxSeconds, bool allowWhisper, bool allowNormal, bool allowShout)
        {
            MinSeconds = minSeconds;
            MaxSeconds = maxSeconds;
            AllowWhisper = allowWhisper;
            AllowNormal = allowNormal;
            AllowShout = allowShout;
        }

        public float MinSeconds { get; }
        public float MaxSeconds { get; }
        public bool AllowWhisper { get; }
        public bool AllowNormal { get; }
        public bool AllowShout { get; }

        public static ClipFilter Any => new ClipFilter(0f, float.MaxValue, true, true, true);
    }

    /// <summary>
    /// Recorded clips kept on disk between sessions. Each clip is one WAV whose file name carries its tags
    /// (<c>utcTicks_loudness_alarm.wav</c>), so there is nothing else to keep in step. Plain C#.
    /// </summary>
    public sealed class ClipBank
    {
        private const int CacheSize = 8;
        private const int MaxAlarmLevel = 9;

        private static readonly Regex NamePattern = new Regex(@"^(\d+)_([wns])_([0-9])\.wav$", RegexOptions.CultureInvariant);

        private readonly string _directory;
        private readonly int _maxClips;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<string> _skipped = new List<string>();
        private readonly LinkedList<CachedClip> _cache = new LinkedList<CachedClip>();
        private long _lastTicks;

        public ClipBank(string directory, int maxClips = 300)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("A directory is required.", nameof(directory));
            if (maxClips < 1) throw new ArgumentOutOfRangeException(nameof(maxClips));

            _directory = directory;
            _maxClips = maxClips;
            Directory.CreateDirectory(_directory);
            IndexExistingFiles();
        }

        /// <summary>Wav files that did not match the naming pattern and were left alone.</summary>
        public IReadOnlyList<string> Skipped => _skipped;

        public int Count => _entries.Count;

        public void Add(Chunk chunk, int alarmLevel)
        {
            if (chunk.Samples == null || chunk.Samples.Length == 0)
                throw new ArgumentException("The chunk has no samples.", nameof(chunk));
            if (alarmLevel < 0 || alarmLevel > MaxAlarmLevel)
                throw new ArgumentOutOfRangeException(nameof(alarmLevel), "Alarm level must be 0-9.");

            long ticks = Math.Max(DateTime.UtcNow.Ticks, _lastTicks + 1);
            _lastTicks = ticks;
            char loudness = LoudnessLetter(VoiceUtility.ClassifyVolume(chunk.PeakRms));
            string path = Path.Combine(_directory, FileName(ticks, loudness, alarmLevel));

            try
            {
                File.WriteAllBytes(path, WavCodec.Encode(chunk.Samples, PauseSegmenter.SampleRate));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                throw new IOException($"ClipBank could not write clip '{path}': {e.Message}", e);
            }

            _entries.Add(new Entry(path, ticks, loudness, (float)chunk.Samples.Length / PauseSegmenter.SampleRate));
            EvictOldest();
        }

        public bool TryPick(ClipFilter filter, Random random, out float[] samples, out int sampleRate)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));

            var candidates = new List<Entry>();
            foreach (Entry entry in _entries)
            {
                if (Matches(filter, entry))
                    candidates.Add(entry);
            }

            if (candidates.Count == 0)
            {
                samples = null;
                sampleRate = 0;
                return false;
            }

            Entry picked = candidates[random.Next(candidates.Count)];
            CachedClip clip = Load(picked.Path);
            samples = (float[])clip.Samples.Clone();
            sampleRate = clip.SampleRate;
            return true;
        }

        /// <summary>Deletes every clip file in the directory that follows the naming pattern; other files stay.</summary>
        public void Clear()
        {
            foreach (string path in Directory.GetFiles(_directory, "*.wav"))
            {
                if (NamePattern.IsMatch(Path.GetFileName(path)))
                    DeleteFile(path);
            }
            _entries.Clear();
            _cache.Clear();
        }

        private void IndexExistingFiles()
        {
            foreach (string path in Directory.GetFiles(_directory, "*.wav"))
            {
                Match match = NamePattern.Match(Path.GetFileName(path));
                if (!match.Success)
                {
                    _skipped.Add(path);
                    continue;
                }

                long ticks = long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                long payloadBytes = Math.Max(0, new FileInfo(path).Length - WavCodec.HeaderBytes);
                float seconds = (float)payloadBytes / 2 / PauseSegmenter.SampleRate;
                _entries.Add(new Entry(path, ticks, match.Groups[2].Value[0], seconds));
                _lastTicks = Math.Max(_lastTicks, ticks);
            }
            _entries.Sort((a, b) => a.Ticks.CompareTo(b.Ticks));
            EvictOldest();
        }

        private void EvictOldest()
        {
            _entries.Sort((a, b) => a.Ticks.CompareTo(b.Ticks));
            while (_entries.Count > _maxClips)
            {
                DeleteFile(_entries[0].Path);
                ForgetCached(_entries[0].Path);
                _entries.RemoveAt(0);
            }
        }

        private static void DeleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                throw new IOException($"ClipBank could not delete clip '{path}': {e.Message}", e);
            }
        }

        private CachedClip Load(string path)
        {
            for (LinkedListNode<CachedClip> node = _cache.First; node != null; node = node.Next)
            {
                if (node.Value.Path != path) continue;
                _cache.Remove(node);
                _cache.AddFirst(node);
                return node.Value;
            }

            float[] samples;
            int rate;
            try
            {
                samples = WavCodec.Decode(File.ReadAllBytes(path), out rate);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is InvalidDataException)
            {
                throw new IOException($"ClipBank could not read clip '{path}': {e.Message}", e);
            }

            var clip = new CachedClip(path, samples, rate);
            _cache.AddFirst(clip);
            if (_cache.Count > CacheSize)
                _cache.RemoveLast();
            return clip;
        }

        private void ForgetCached(string path)
        {
            for (LinkedListNode<CachedClip> node = _cache.First; node != null; node = node.Next)
            {
                if (node.Value.Path != path) continue;
                _cache.Remove(node);
                return;
            }
        }

        private static bool Matches(ClipFilter filter, Entry entry)
        {
            if (entry.Seconds < filter.MinSeconds || entry.Seconds > filter.MaxSeconds) return false;
            switch (entry.Loudness)
            {
                case 'w': return filter.AllowWhisper;
                case 'n': return filter.AllowNormal;
                default: return filter.AllowShout;
            }
        }

        private static char LoudnessLetter(CastVolume volume)
        {
            switch (volume)
            {
                case CastVolume.Whisper: return 'w';
                case CastVolume.Shout: return 's';
                default: return 'n';
            }
        }

        private static string FileName(long ticks, char loudness, int alarmLevel)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}_{1}_{2}.wav", ticks, loudness, alarmLevel);
        }

        private readonly struct Entry
        {
            public Entry(string path, long ticks, char loudness, float seconds)
            {
                Path = path;
                Ticks = ticks;
                Loudness = loudness;
                Seconds = seconds;
            }

            public string Path { get; }
            public long Ticks { get; }
            public char Loudness { get; }
            public float Seconds { get; }
        }

        private sealed class CachedClip
        {
            public CachedClip(string path, float[] samples, int sampleRate)
            {
                Path = path;
                Samples = samples;
                SampleRate = sampleRate;
            }

            public string Path { get; }
            public float[] Samples { get; }
            public int SampleRate { get; }
        }
    }
}
