using System;

namespace RogueAi.Inventory
{
    // doc-ref docs/systems/raid-scene-assembly.md ("Era reaches the raid")
    /// <summary>
    /// The facts every system building a raid needs to agree on: which seed, and which Age. Set by
    /// <c>RaidDirector.BuildCastle</c> on every peer before the castle, the loot and the garrison
    /// are built, and read by anything that has to pick era-appropriate content.
    ///
    /// Lives here, beside <see cref="HistoricalEra"/>, because this assembly depends only on PurrNet:
    /// the castle generator can reference it without a cycle (Raid already depends on Castle), which
    /// is how the era-specific rooms in docs/plans/era-castle-rooms.md (step 1) will read the Age.
    ///
    /// Callers that are handed the era explicitly (<c>GuardSpawner.SpawnFor</c>) should use that; this
    /// is for code that sits below the raid in the dependency graph and cannot be handed it.
    /// </summary>
    public readonly struct RaidContext : IEquatable<RaidContext>
    {
        /// <summary>The seed the raid's castle, loot and garrison are built from.</summary>
        public readonly int Seed;

        /// <summary>The Age the raid is set in.</summary>
        public readonly HistoricalEra Era;

        public RaidContext(int seed, HistoricalEra era)
        {
            Seed = seed;
            Era = era;
        }

        /// <summary>The raid being built or played on this peer. Only meaningful when <see cref="HasCurrent"/>.</summary>
        public static RaidContext Current { get; private set; }

        /// <summary>False in the Lair, before the first raid is built, and after <see cref="Clear"/>.</summary>
        public static bool HasCurrent { get; private set; }

        /// <summary>Raised on this peer whenever a raid publishes its context.</summary>
        public static event Action<RaidContext> Published;

        /// <summary>Publishes the context of the raid about to be built.</summary>
        public static void Publish(RaidContext context)
        {
            Current = context;
            HasCurrent = true;
            Published?.Invoke(context);
        }

        /// <summary>Forgets the current raid. Called on returning to the Lair, and by test teardown.</summary>
        public static void Clear()
        {
            Current = default;
            HasCurrent = false;
        }

        public bool Equals(RaidContext other) => Seed == other.Seed && Era == other.Era;
        public override bool Equals(object obj) => obj is RaidContext other && Equals(other);
        public override int GetHashCode() => unchecked(Seed * 397) ^ (int)Era;
        public override string ToString() => $"seed {Seed}, {Era}";
    }
}
