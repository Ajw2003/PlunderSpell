namespace Plunderspell.Guards
{
    /// <summary>
    /// The states one guard can be in, made once so a guard moves between instances instead of
    /// allocating a state per change. Each behaviour issue (#207-#213) swaps its placeholder here.
    /// </summary>
    public sealed class GuardStateSet
    {
        public GuardStateSet(Guard guard)
        {
            Patrol = new PatrolState(guard);
            Incapacitated = new PlaceholderIncapacitatedState(guard);
            Dead = new PlaceholderDeadState(guard);
        }

        /// <summary>Walking about (#207).</summary>
        public GuardState Patrol { get; }

        /// <summary>Asleep, stunned or levitated (#211).</summary>
        public GuardState Incapacitated { get; }

        /// <summary>Down for good (#213).</summary>
        public GuardState Dead { get; }
    }
}
