using EventSystems;

namespace StateMachine
{
    /// <summary>The player this machine plays as changed; <see cref="Player"/> is null when there is none (#302).</summary>
    public readonly struct LocalPlayerChanged : IEvent
    {
        public readonly PlayerStateMachine Player;
        public LocalPlayerChanged(PlayerStateMachine player) { Player = player; }
    }
}
