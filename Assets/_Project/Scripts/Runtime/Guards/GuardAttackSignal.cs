namespace Plunderspell.Guards
{
    /// <summary>What a guard just did to someone. Carried by <see cref="GuardAttackSignal"/>.</summary>
    public enum GuardAttackKind
    {
        /// <summary>A swing or bite at arm's reach.</summary>
        Melee = 0,

        /// <summary>A shot or a throw: a projectile left the guard.</summary>
        Projectile = 1,
    }

    // doc-ref 51f2 docs/4-systems/net.md
    /// <summary>
    /// One replicated integer: the attack count in the high bits, the kind of the latest attack in
    /// the low <see cref="KindBits"/> bits. Pure, so the packing is tested without a network.
    /// </summary>
    public static class GuardAttackSignal
    {
        /// <summary>Bits reserved for the kind. Two, so up to four kinds fit without a format change.</summary>
        public const int KindBits = 2;

        private const int KindMask = (1 << KindBits) - 1;

        /// <summary>The signal before the guard has attacked at all.</summary>
        public const int None = 0;

        /// <summary>The signal after one more attack of <paramref name="kind"/>.</summary>
        public static int Next(int signal, GuardAttackKind kind)
        {
            // Wraps rather than throwing after about 500 million attacks; a client only ever
            // compares a value with the previous one, so a wrap still reads as a change.
            int count = unchecked(Count(signal) + 1) & (int.MaxValue >> KindBits);
            return (count << KindBits) | ((int)kind & KindMask);
        }

        /// <summary>How many attacks the signal has counted.</summary>
        public static int Count(int signal) => (int)((uint)signal >> KindBits);

        /// <summary>The kind of the most recent attack. Meaningless while <see cref="Count"/> is 0.</summary>
        public static GuardAttackKind Kind(int signal) => (GuardAttackKind)(signal & KindMask);
    }
}
