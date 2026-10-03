using System;

namespace Plunderspell.Voice
{
    /// <summary>One utterance overheard between casts.</summary>
    public readonly struct ChatterReport
    {
        public readonly string Transcript;
        public readonly float PeakRms;
        public readonly CastVolume Volume;

        public ChatterReport(string transcript, float peakRms, CastVolume volume)
        {
            Transcript = transcript;
            PeakRms = peakRms;
            Volume = volume;
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
