namespace Plunderspell.Castle
{
    /// <summary>
    /// The castle's storeys (#247, docs/plans/multi-floor-castle.md). A module's root sits at the bottom of its
    /// 0.30 m floor slab. The keep's slab rests on the inner ward's wall tops (0.30 + 4.00); the crypt's 3.00 m
    /// rooms end at the underside of the ground slab.
    /// </summary>
    public static class CastleLevels
    {
        public const int Crypt = -1;
        public const int Ground = 0;
        public const int Keep = 1;
        public const int Lowest = Crypt;
        public const int Count = 3;

        public const float KeepRootY = 4.3f;
        public const float CryptRootY = -3.3f;

        /// <summary>World height of the root of a module standing on <paramref name="level"/>.</summary>
        public static float RootY(int level) => level == Keep ? KeepRootY : level == Crypt ? CryptRootY : 0f;

        /// <summary>0-based index of a level, for arrays sized <see cref="Count"/>.</summary>
        public static int Index(int level) => level - Lowest;
    }
}
