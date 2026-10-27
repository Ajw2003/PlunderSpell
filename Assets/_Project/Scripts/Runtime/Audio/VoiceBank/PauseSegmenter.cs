using System;
using System.Collections.Generic;

namespace Plunderspell.Audio.VoiceBank
{
    /// <summary>One cut-out stretch of speech, faded at both ends so it can be replayed without clicks.</summary>
    public readonly struct Chunk
    {
        public Chunk(float[] samples, float peakRms, float meanRms, float seconds)
        {
            Samples = samples;
            PeakRms = peakRms;
            MeanRms = meanRms;
            Seconds = seconds;
        }

        public float[] Samples { get; }
        public float PeakRms { get; }
        public float MeanRms { get; }
        public float Seconds { get; }
    }

    /// <summary>
    /// Cuts a 16 kHz mono mic stream into clips at pauses. It only looks at loudness, never at words.
    /// Plain C# so it can be unit-tested; Feed is called every frame and allocates only the emitted chunks.
    /// </summary>
    public sealed class PauseSegmenter
    {
        public const int SampleRate = 16000;

        private const int FrameSamples = SampleRate / 50;
        private const int PauseFrames = 6;
        private const int PreRollSamples = SampleRate * 30 / 1000;
        private const int TailSamples = SampleRate * 30 / 1000;
        private const int FadeSamples = SampleRate * 10 / 1000;
        private const int MinChunkSamples = SampleRate / 5;
        private const int MaxChunkSamples = SampleRate * 3 / 2;
        private const int CutSearchFrames = 30;

        private const float MinNoiseFloor = 0.002f;
        private const float MaxNoiseFloor = 0.05f;
        private const float MinSpeechRms = 0.01f;
        private const float SpeechOverFloor = 3f;
        private const float FloorFallRate = 0.3f;
        private const float FloorRiseRate = 0.02f;
        private const float FloorRiseRateDuringSpeech = 0.0005f;

        private readonly float[] _frame = new float[FrameSamples];
        private readonly float[] _preRoll = new float[PreRollSamples];
        private readonly List<float> _chunk = new List<float>(MaxChunkSamples + FrameSamples);
        private readonly List<float> _frameRms = new List<float>(MaxChunkSamples / FrameSamples + 2);

        private int _frameFill;
        private int _preRollFilled;
        private bool _inChunk;
        private int _silentFrames;
        private float _noiseFloor;
        private bool _floorSeeded;

        /// <summary>Raised from inside Feed or Flush whenever a chunk is finished.</summary>
        public event Action<Chunk> ChunkReady;

        public void Feed(float[] samples, int count)
        {
            if (samples == null) throw new ArgumentNullException(nameof(samples));
            if (count < 0 || count > samples.Length) throw new ArgumentOutOfRangeException(nameof(count));

            int read = 0;
            while (read < count)
            {
                int take = Math.Min(FrameSamples - _frameFill, count - read);
                Array.Copy(samples, read, _frame, _frameFill, take);
                _frameFill += take;
                read += take;
                if (_frameFill == FrameSamples)
                {
                    ProcessFrame();
                    _frameFill = 0;
                }
            }
        }

        /// <summary>Drops audio in progress without emitting it; the learned noise floor is kept.</summary>
        public void Reset()
        {
            _frameFill = 0;
            _preRollFilled = 0;
            _chunk.Clear();
            _frameRms.Clear();
            _inChunk = false;
            _silentFrames = 0;
        }

        /// <summary>Emits audio in progress as a chunk if it is long enough, then starts clean.</summary>
        public void Flush()
        {
            if (_inChunk)
            {
                int silentSamples = _silentFrames * FrameSamples;
                Emit(_chunk.Count - silentSamples + Math.Min(TailSamples, silentSamples), _frameRms.Count - _silentFrames);
            }
            Reset();
        }

        private void ProcessFrame()
        {
            float rms = FrameRms();
            bool speech = rms > Math.Max(MinSpeechRms, _noiseFloor * SpeechOverFloor);
            UpdateNoiseFloor(rms, speech);

            if (!_inChunk)
            {
                if (speech)
                {
                    _inChunk = true;
                    _silentFrames = 0;
                    for (int i = PreRollSamples - _preRollFilled; i < PreRollSamples; i++)
                        _chunk.Add(_preRoll[i]);
                    AppendFrame(rms);
                }
            }
            else
            {
                AppendFrame(rms);
                _silentFrames = speech ? 0 : _silentFrames + 1;
                if (_silentFrames >= PauseFrames)
                {
                    Emit(_chunk.Count - _silentFrames * FrameSamples + TailSamples, _frameRms.Count - _silentFrames);
                    _chunk.Clear();
                    _frameRms.Clear();
                    _inChunk = false;
                    _silentFrames = 0;
                }
                else if (_chunk.Count >= MaxChunkSamples)
                {
                    CutAtQuietestFrame();
                }
            }

            RememberPreRoll();
        }

        private void AppendFrame(float rms)
        {
            for (int i = 0; i < FrameSamples; i++)
                _chunk.Add(_frame[i]);
            _frameRms.Add(rms);
        }

        // Cutting where the speech is quietest keeps a forced split from landing mid-syllable.
        private void CutAtQuietestFrame()
        {
            int window = Math.Min(CutSearchFrames, _frameRms.Count);
            int quietestFromEnd = 0;
            float quietest = float.MaxValue;
            for (int fromEnd = window - 1; fromEnd >= 0; fromEnd--)
            {
                float rms = _frameRms[_frameRms.Count - 1 - fromEnd];
                if (rms < quietest)
                {
                    quietest = rms;
                    quietestFromEnd = fromEnd;
                }
            }

            int keptFrames = _frameRms.Count - quietestFromEnd;
            int keptSamples = _chunk.Count - quietestFromEnd * FrameSamples;
            Emit(keptSamples, keptFrames);
            _chunk.RemoveRange(0, keptSamples);
            _frameRms.RemoveRange(0, keptFrames);
            _silentFrames = Math.Min(_silentFrames, _frameRms.Count);
        }

        private void Emit(int length, int bodyFrames)
        {
            if (length < MinChunkSamples || bodyFrames <= 0) return;

            var samples = new float[length];
            _chunk.CopyTo(0, samples, 0, length);

            int fade = Math.Min(FadeSamples, length / 2);
            for (int i = 0; i < fade; i++)
            {
                float gain = (float)i / fade;
                samples[i] *= gain;
                samples[length - 1 - i] *= gain;
            }

            float peak = 0f;
            float sum = 0f;
            for (int i = 0; i < bodyFrames; i++)
            {
                float rms = _frameRms[i];
                if (rms > peak) peak = rms;
                sum += rms;
            }

            ChunkReady?.Invoke(new Chunk(samples, peak, sum / bodyFrames, (float)length / SampleRate));
        }

        private float FrameRms()
        {
            float sum = 0f;
            for (int i = 0; i < FrameSamples; i++)
                sum += _frame[i] * _frame[i];
            return (float)Math.Sqrt(sum / FrameSamples);
        }

        private void UpdateNoiseFloor(float rms, bool speech)
        {
            if (!_floorSeeded)
            {
                _noiseFloor = Math.Min(MaxNoiseFloor, Math.Max(MinNoiseFloor, rms));
                _floorSeeded = true;
                return;
            }

            if (rms < _noiseFloor)
                _noiseFloor += (rms - _noiseFloor) * FloorFallRate;
            else
                _noiseFloor += (rms - _noiseFloor) * (speech ? FloorRiseRateDuringSpeech : FloorRiseRate);

            _noiseFloor = Math.Min(MaxNoiseFloor, Math.Max(MinNoiseFloor, _noiseFloor));
        }

        private void RememberPreRoll()
        {
            const int keep = PreRollSamples - FrameSamples;
            Array.Copy(_preRoll, FrameSamples, _preRoll, 0, keep);
            Array.Copy(_frame, 0, _preRoll, keep, FrameSamples);
            _preRollFilled = Math.Min(PreRollSamples, _preRollFilled + FrameSamples);
        }
    }
}
