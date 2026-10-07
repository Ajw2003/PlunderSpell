using EventSystems;
using UnityEngine;

namespace Plunderspell.Spells
{
    // The spell system's payloads, published on EventManager (#301).

    /// <summary>A phrase was resolved on the caster's machine, cast or not.</summary>
    public readonly struct PhraseResolved : IEvent
    {
        public readonly SpellCastingSystem.PhraseReport Report;
        public PhraseResolved(SpellCastingSystem.PhraseReport report) { Report = report; }
    }

    /// <summary>A cast resolved; published on every peer.</summary>
    public readonly struct CastResolved : IEvent
    {
        public readonly SpellCastingSystem.CastReport Report;
        public CastResolved(SpellCastingSystem.CastReport report) { Report = report; }
    }

    /// <summary>Aurum Voco conjured a pile of gold worth <see cref="Worth"/> at <see cref="Where"/>.</summary>
    public readonly struct GoldConjured : IEvent
    {
        public readonly float Worth;
        public readonly Vector3 Where;
        public GoldConjured(float worth, Vector3 where) { Worth = worth; Where = where; }
    }

    /// <summary>A misfired Aurum Voco scattered a coin pile worth <see cref="Worth"/> at <see cref="Where"/>.</summary>
    public readonly struct GoldScattered : IEvent
    {
        public readonly float Worth;
        public readonly Vector3 Where;
        public GoldScattered(float worth, Vector3 where) { Worth = worth; Where = where; }
    }
}
