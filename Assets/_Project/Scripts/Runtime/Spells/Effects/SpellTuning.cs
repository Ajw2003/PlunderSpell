using RogueAi.Voice;
using UnityEngine;

namespace RogueAi.Spells
{
    /// <summary>
    /// Every number that decides how a cast feels, in one pure static table.
    ///
    /// The central trade-off of Plunderspell lives here: <see cref="CastVolume"/> scales a spell's
    /// power AND the noise it makes, in the same direction. A whisper is weak and nearly silent; a
    /// shout is strong and wakes the castle. Nothing else in the game gives the player that dial, so
    /// it is deliberately steep — a shouted Tonitrus alone is over a third of the way to Roused.
    ///
    /// All of it is side-effect-free so the balance can be asserted in tests rather than eyeballed
    /// in play.
    /// </summary>
    public static class SpellTuning
    {
        /// <summary>Power/damage/radius multiplier for the volume a phrase was spoken at.</summary>
        public static float PowerMultiplier(CastVolume volume)
        {
            switch (volume)
            {
                case CastVolume.Whisper: return 0.5f;
                case CastVolume.Shout: return 1.75f;
                default: return 1.0f;
            }
        }

        /// <summary>Radius of the noise a cast makes, in metres. A whisper barely carries.</summary>
        public static float NoiseRadius(CastVolume volume)
        {
            switch (volume)
            {
                case CastVolume.Whisper: return 0.5f;
                case CastVolume.Shout: return 12.0f;
                default: return 5.0f;
            }
        }

        /// <summary>Loudness (0..1) of the noise a cast makes before wall attenuation.</summary>
        public static float NoiseStrength(CastVolume volume)
        {
            switch (volume)
            {
                case CastVolume.Whisper: return 0.1f;
                case CastVolume.Shout: return 1.0f;
                default: return 0.45f;
            }
        }

        /// <summary>Base effect radius in metres, before the volume multiplier.</summary>
        public const float DefaultEffectRadius = 6f;

        /// <summary>Ignis: damage per second while burning, and how long the fire lasts.</summary>
        public const float IgnisDamagePerSecond = 12f;
        public const float IgnisBurnSeconds = 4f;

        /// <summary>Tonitrus: stun duration and the extra noise a thunderclap adds on top of the cast.</summary>
        public const float TonitrusStunSeconds = 3f;
        public const float TonitrusNoiseRadius = 18f;
        public const float TonitrusNoiseStrength = 1f;

        /// <summary>Somnus: how long a guard sleeps. Whisper-cast it, or the noise wakes them anyway.</summary>
        public const float SomnusSleepSeconds = 8f;

        /// <summary>Levo: upward impulse applied to a levitated object, and how long it floats.</summary>
        public const float LevoImpulse = 6f;
        public const float LevoSeconds = 3f;

        /// <summary>AurumVoco: coin value conjured, and the racket a pile of coins makes on landing.</summary>
        public const float AurumVocoWorth = 40f;
        public const float AurumVocoNoiseRadius = 8f;
        public const float AurumVocoNoiseStrength = 0.5f;

        /// <summary>Misfire outcomes are punishing on purpose — that is the whole risk of speaking badly.</summary>
        public const float MisfireSelfBurnSeconds = 6f;
        public const float MisfireSelfDamagePerSecond = 8f;
        public const float MisfireSelfStunSeconds = 4f;
        public const float MisfireSelfSleepSeconds = 5f;

        /// <summary>Radius a misfire searches for its (wrong) victim — tighter than an intended cast.</summary>
        public const float MisfireRadius = 4f;
    }
}
