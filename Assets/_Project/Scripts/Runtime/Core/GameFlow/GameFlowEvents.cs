using EventSystems;

namespace Plunderspell.Core
{
    // Game-flow payloads published on EventManager (#300). Readonly structs, so publishing allocates nothing.

    /// <summary>The game moved to another screen or mode.</summary>
    public readonly struct GameStateChanged : IEvent
    {
        public readonly GameState Previous;
        public readonly GameState Current;
        public GameStateChanged(GameState previous, GameState current) { Previous = previous; Current = current; }
    }

    /// <summary>Health, mana or gold changed. Read the new numbers from <see cref="GameServices.PlayerStats"/>.</summary>
    public readonly struct PlayerStatsChanged : IEvent
    {
    }

    public readonly struct ExtractionStarted : IEvent
    {
    }

    public readonly struct ExtractionCancelled : IEvent
    {
    }

    public readonly struct ExtractionCompleted : IEvent
    {
    }

    /// <summary>Normalised 0..1 progress, published each tick while extracting.</summary>
    public readonly struct ExtractionProgress : IEvent
    {
        public readonly float Progress;
        public ExtractionProgress(float progress) { Progress = progress; }
    }

    /// <summary>The chosen microphone changed; empty means automatic.</summary>
    public readonly struct MicrophoneChanged : IEvent
    {
        public readonly string Device;
        public MicrophoneChanged(string device) { Device = device; }
    }

    /// <summary>The "guards hear my voice" setting changed.</summary>
    public readonly struct GuardsHearChatterChanged : IEvent
    {
        public readonly bool Enabled;
        public GuardsHearChatterChanged(bool enabled) { Enabled = enabled; }
    }

    /// <summary>The local player died. The raid treats it as a lost raid.</summary>
    public readonly struct LocalPlayerDied : IEvent
    {
    }

    /// <summary>The co-op session's players, lobby or connection changed. Read the new state from <see cref="GameServices.Coop"/>.</summary>
    public readonly struct CoopChanged : IEvent
    {
    }
}
