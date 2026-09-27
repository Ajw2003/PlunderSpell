using System;

namespace Plunderspell.Inventory
{
    // doc-ref 7655 docs/4-systems/raid-scene-assembly.md
    /// <summary>
    /// The seed and Age of the raid being built on this peer, published by
    /// <c>RaidDirector.BuildCastle</c>. For code below the raid in the dependency graph (the castle
    /// generator); anything handed the era explicitly should use that instead.
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
