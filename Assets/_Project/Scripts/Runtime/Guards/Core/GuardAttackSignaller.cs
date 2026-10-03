using System;
using PurrNet;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Tells every peer that a guard attacked. The server resolves the attack and bumps a replicated
    /// integer (count and kind packed by <see cref="GuardAttackSignal"/>); each client replays the
    /// change as an <see cref="Attacked"/> event, which is what an animator or a sound hooks. The
    /// SyncVar itself is a field of the guard; this class only holds it.
    /// </summary>
    public sealed class GuardAttackSignaller
    {
        private readonly SyncVar<int> _signal;

        /// <summary>Raised on the server as the attack happens, and on a client when the signal arrives.</summary>
        public event Action<GuardAttackKind> Attacked;

        public GuardAttackSignaller(SyncVar<int> signal)
        {
            _signal = signal;
        }

        /// <summary>How many attacks this peer has heard about.</summary>
        public int Count => GuardAttackSignal.Count(_signal.value);

        /// <summary>The kind of the latest attack. Meaningless while <see cref="Count"/> is 0.</summary>
        public GuardAttackKind LastKind => GuardAttackSignal.Kind(_signal.value);

        /// <summary>Server side: records an attack and raises the event locally.</summary>
        public void Signal(GuardAttackKind kind)
        {
            _signal.value = GuardAttackSignal.Next(_signal.value, kind);
            Attacked?.Invoke(kind);
        }

        /// <summary>Starts replaying the replicated signal on a client.</summary>
        public void StartReplaying() => _signal.onChanged += OnReplicated;

        public void StopReplaying() => _signal.onChanged -= OnReplicated;

        private void OnReplicated(int signal)
        {
            if (GuardAttackSignal.Count(signal) > 0)
                Attacked?.Invoke(GuardAttackSignal.Kind(signal));
        }
    }
}
