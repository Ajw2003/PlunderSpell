namespace RogueAi.Spells
{
    /// <summary>
    /// One spell's actual consequence in the world. Effects are stateless singletons registered in
    /// <see cref="SpellEffectRegistry"/> and resolved by <see cref="SpellId"/>, so adding a spell is
    /// adding one class and one registry line — no switch statement grows.
    ///
    /// An effect runs on the server (or on a lone host / in a test), never per-client: the
    /// <c>[ObserversRpc]</c> in <c>SpellCastingSystem</c> broadcasts presentation, while consequence
    /// stays authoritative.
    /// </summary>
    public interface ISpellEffect
    {
        /// <summary>The id this effect answers to.</summary>
        SpellId Id { get; }

        /// <summary>Short human-readable description, used in logs and the cast feed.</summary>
        string Describe(in SpellEffectContext ctx);

        /// <summary>
        /// Apply the effect. Returns the number of things it actually affected — 0 is a legitimate
        /// outcome (a spell cast at nothing) and is what makes effects assertable in tests.
        /// </summary>
        int Execute(in SpellEffectContext ctx);
    }
}
