using System;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Holds the one thing worth a look that a guard has been told about and not yet acted on: a noise it
    /// heard, a player it saw, or the director's hue and cry. It keeps only the strongest (latest on a tie),
    /// so a state asks one question ("is there a lead?") instead of subscribing to every source. Patrol
    /// and Investigate read it; this class makes no decisions about what to do.
    /// </summary>
    public sealed class GuardLeads
    {
        /// <summary>A noise is worth at most this much, so any sighting or hue and cry outranks it.</summary>
        public const float LoudestNoiseStrength = 1f;

        /// <summary>The director said a player is roughly here: better than a noise, worse than seeing one.</summary>
        public const float HueAndCryStrength = 2f;

        /// <summary>Another guard cried out that it saw a player: as good as the hue and cry, below seeing one.</summary>
        public const float CryStrength = HueAndCryStrength;

        /// <summary>The guard saw a player with its own eyes.</summary>
        public const float SightingStrength = 3f;

        private Vector3 _position;
        private float _strength;

        private readonly Func<System.Random> _random;

        public GuardLeads(GuardDirectorLink link, GuardHearing hearing, Func<System.Random> random)
        {
            _random = random;
            // The link applies its radius rule before it raises this; the hue and cry has none.
            link.InvestigateRequested += position => Offer(Roughly(position), HueAndCryStrength);
            // A cry leads to where the crier stood when it saw the intruder (the cry carries no spot of its own).
            hearing.CryHeard += origin => Offer(origin, CryStrength);
            hearing.NoiseNoticed +=(origin, strength) => Offer(origin, Mathf.Clamp01(strength) * LoudestNoiseStrength);
        }

        // The hue and cry knows only roughly where a player is, so a player who breaks away and hides
        // can still slip the hunt. Same 3 to 5 m offset the legacy guard used.
        private Vector3 Roughly(Vector3 position)
        {
            System.Random random = _random();
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float distance = Mathf.Lerp(GuardBrain.HuntOffsetMin, GuardBrain.HuntOffsetMax, (float)random.NextDouble());
            return position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
        }

        public bool HasLead { get; private set; }

        /// <summary>How strong the waiting lead is. Only meaningful while <see cref="HasLead"/>.</summary>
        public float Strength => _strength;

        /// <summary>Notes a lead. It replaces the waiting one only if it is at least as strong, so a newer lead of equal strength wins.</summary>
        public void Offer(Vector3 position, float strength)
        {
            if (HasLead && strength < _strength)
                return;

            HasLead = true;
            _position = position;
            _strength = strength;
        }

        /// <summary>Hands over the waiting lead and forgets it.</summary>
        public Vector3 Take()
        {
            HasLead = false;
            return _position;
        }

        public void Clear() => HasLead = false;
    }
}
