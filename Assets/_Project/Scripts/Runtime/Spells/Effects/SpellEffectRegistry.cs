using System.Collections.Generic;
using UnityEngine;

namespace RogueAi.Spells
{
    /// <summary>
    /// Resolves a <see cref="SpellId"/> to the <see cref="ISpellEffect"/> that carries it out.
    ///
    /// Built-ins register themselves on first use. <see cref="Register"/> replaces an entry, which is
    /// how a test substitutes a spy for a real effect and how a future milestone can swap an effect
    /// without touching the casting system.
    /// </summary>
    public static class SpellEffectRegistry
    {
        private static readonly Dictionary<SpellId, ISpellEffect> _effects =
            new Dictionary<SpellId, ISpellEffect>();

        private static bool _builtInsInstalled;

        /// <summary>Every registered effect, built-ins included.</summary>
        public static IEnumerable<ISpellEffect> All
        {
            get
            {
                EnsureBuiltIns();
                return _effects.Values;
            }
        }

        /// <summary>Registers (or replaces) the effect for an id.</summary>
        public static void Register(ISpellEffect effect)
        {
            if (effect == null)
                return;
            EnsureBuiltIns();
            _effects[effect.Id] = effect;
        }

        /// <summary>Drops every registration, including built-ins. Test teardown uses this.</summary>
        public static void Reset()
        {
            _effects.Clear();
            _builtInsInstalled = false;
        }

        /// <summary>The effect for an id, or null when nothing handles it.</summary>
        public static ISpellEffect Find(SpellId id)
        {
            EnsureBuiltIns();
            return _effects.TryGetValue(id, out ISpellEffect effect) ? effect : null;
        }

        /// <summary>
        /// Runs the effect for <c>ctx.Spell</c>. Returns how many things it affected, or -1 when no
        /// effect is registered for that id — which is a different outcome from "affected nothing"
        /// and is reported as a warning rather than passing silently.
        /// </summary>
        public static int Execute(in SpellEffectContext ctx)
        {
            ISpellEffect effect = Find(ctx.Spell);
            if (effect == null)
            {
                if (ctx.Spell != SpellId.None)
                    Debug.LogWarning($"[SpellEffect] No effect registered for {ctx.Spell}; nothing happened.");
                return -1;
            }

            return effect.Execute(ctx);
        }

        private static void EnsureBuiltIns()
        {
            if (_builtInsInstalled)
                return;
            _builtInsInstalled = true;

            ISpellEffect[] builtIns =
            {
                new IgnisEffect(),
                new FrangoEffect(),
                new LevoEffect(),
                new AurumVocoEffect(),
                new TonitrusEffect(),
                new SomnusEffect(),
                new CadaverSurgeEffect(),
                new PortaEffect(),

                new MisfireIgnisEffect(),
                new MisfireFrangoEffect(),
                new MisfireLevoEffect(),
                new MisfireTonitrusEffect(),
                new MisfireSomnusEffect(),
                new MisfireCadaverSurgeEffect(),
                new MisfireAurumVocoEffect(),
                new MisfirePortaEffect(),
            };

            foreach (ISpellEffect effect in builtIns)
                _effects[effect.Id] = effect;
        }
    }
}
