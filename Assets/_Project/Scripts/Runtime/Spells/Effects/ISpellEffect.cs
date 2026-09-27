namespace Plunderspell.Spells
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

    /// <summary>
    /// A spell that moves its own caster (Velox, Saltus and their misfires). The movement runs on
    /// the caster's machine, where the body is simulated, before the cast goes to the server; the
    /// server's <see cref="ISpellEffect.Execute"/> only makes the noise. Moving it on the server
    /// would miss a remote player's body, and running both would move a host twice.
    /// </summary>
    public interface ICasterMovementSpell
    {
        /// <summary>False when the cast should fizzle without costing mana (Saltus in mid-air).</summary>
        bool CanMove(in SpellEffectContext ctx);

        /// <summary>Moves the caster. Called once, on the caster's machine.</summary>
        void MoveCaster(in SpellEffectContext ctx);
    }
}
