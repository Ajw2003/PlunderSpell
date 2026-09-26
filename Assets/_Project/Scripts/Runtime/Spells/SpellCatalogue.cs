namespace Plunderspell.Spells
{
    /// <summary>
    /// Pure facts about <see cref="SpellId"/> values: whether an id is a misfire outcome, and the
    /// misfire each primary spell degrades into when the pronunciation is close but wrong.
    ///
    /// This is the fallback the <see cref="MisfireEngine"/> uses when a <see cref="SpellWord"/> asset
    /// leaves <c>misfireId</c> unset, so a half-authored lexicon still misfires instead of silently
    /// casting the real spell — the mechanic must never fail open.
    /// </summary>
    public static class SpellCatalogue
    {
        /// <summary>Misfire outcomes occupy ids 100 and above.</summary>
        public const int MisfireIdFloor = 100;

        /// <summary>True if the id is a misfire outcome rather than an intended spell.</summary>
        public static bool IsMisfire(SpellId id) => (int)id >= MisfireIdFloor;

        /// <summary>
        /// The misfire a primary spell degrades into. Spells with no authored misfire of their own
        /// fall back to <see cref="SpellId.None"/> — a fizzle, which is a failure, not a free cast.
        /// </summary>
        public static SpellId DefaultMisfireFor(SpellId spell)
        {
            switch (spell)
            {
                case SpellId.Ignis: return SpellId.MisfireIgnis;
                case SpellId.Frango: return SpellId.MisFireFrango;
                case SpellId.Levo: return SpellId.MisfireLevo;
                case SpellId.AurumVoco: return SpellId.MisfireAurumVoco;
                case SpellId.Tonitrus: return SpellId.MisfireTonitrus;
                case SpellId.Somnus: return SpellId.MisFireSomnus;
                case SpellId.CadaverSurge: return SpellId.MisFireCadaverSurge;
                case SpellId.Porta: return SpellId.MisfirePorta;
                default: return SpellId.None;
            }
        }

        /// <summary>The primary spell a misfire came from (inverse of <see cref="DefaultMisfireFor"/>).</summary>
        public static SpellId PrimaryFor(SpellId misfire)
        {
            switch (misfire)
            {
                case SpellId.MisfireIgnis: return SpellId.Ignis;
                case SpellId.MisFireFrango: return SpellId.Frango;
                case SpellId.MisfireLevo: return SpellId.Levo;
                case SpellId.MisfireAurumVoco: return SpellId.AurumVoco;
                case SpellId.MisfireTonitrus: return SpellId.Tonitrus;
                case SpellId.MisFireSomnus: return SpellId.Somnus;
                case SpellId.MisFireCadaverSurge: return SpellId.CadaverSurge;
                case SpellId.MisfirePorta: return SpellId.Porta;
                default: return SpellId.None;
            }
        }
    }
}
