using System;

namespace Plunderspell.Voice
{
    /// <summary>One recognised word and where it sits in <see cref="ChatterReport.Samples"/>, in seconds.</summary>
    public readonly struct WordTiming
    {
        public readonly string Word;
        public readonly float Start;
        public readonly float End;
        public readonly float Confidence;

        public WordTiming(string word, float start, float end, float confidence)
        {
            Word = word;
            Start = start;
            End = end;
            Confidence = confidence;
        }
    }

    /// <summary>One utterance overheard between casts.</summary>
    public readonly struct ChatterReport
    {
        /// <summary>Sample rate of <see cref="Samples"/>.</summary>
        public const int SampleRate = 16000;

        public readonly string Transcript;
        public readonly float PeakRms;
        public readonly CastVolume Volume;

        /// <summary>Each word with its time in <see cref="Samples"/>; empty when the recogniser gave no timings.</summary>
        public readonly WordTiming[] Words;

        /// <summary>The recorded voice behind <see cref="Words"/>, mono, gain applied; empty when not kept.</summary>
        public readonly float[] Samples;

        public ChatterReport(string transcript, float peakRms, CastVolume volume)
            : this(transcript, peakRms, volume, Array.Empty<WordTiming>(), Array.Empty<float>())
        {
        }

        public ChatterReport(string transcript, float peakRms, CastVolume volume, WordTiming[] words, float[] samples)
        {
            Transcript = transcript;
            PeakRms = peakRms;
            Volume = volume;
            Words = words ?? Array.Empty<WordTiming>();
            Samples = samples ?? Array.Empty<float>();
        }
    }

    /// <summary>
    /// A voice service that can also write down ordinary talk between casts. Off until
    /// <see cref="ChatterEnabled"/> is set; audio stays on the machine, only the words leave it.
    /// </summary>
    public interface IChatterSource
    {
        bool ChatterEnabled { get; set; }
        event Action<ChatterReport> ChatterHeard;
    }

    /// <summary>Decides which recognised utterances are worth reporting.</summary>
    public static class ChatterFilter
    {
        /// <summary>Below this the utterance is background noise; Vosk's free-form mode invents short words from silence.</summary>
        public const float MinChatterRms = 0.02f;

        public static bool IsWorthReporting(string transcript, float peakRms) =>
            !string.IsNullOrWhiteSpace(transcript) && peakRms >= MinChatterRms;
    }
}
