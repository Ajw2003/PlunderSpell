using PurrNet;
using RogueAi.Voice;
using UnityEngine;

namespace RogueAi.Spells
{
    /// <summary>
    /// Everything an effect needs to resolve itself: who cast, from where, facing where, how loud,
    /// and what actually resolved (which may be a misfire, not what the player intended).
    ///
    /// Deliberately a plain struct with no scene lookups of its own, so an effect can be executed in
    /// a test by handing it a context rather than by building a scene.
    /// </summary>
    public readonly struct SpellEffectContext
    {
        /// <summary>The spell that actually resolved — already misfire-substituted.</summary>
        public readonly SpellId Spell;

        /// <summary>Loudness the phrase was spoken at. Scales power and noise together.</summary>
        public readonly CastVolume Volume;

        /// <summary>World position the effect originates from (the caster's hands).</summary>
        public readonly Vector3 Origin;

        /// <summary>Direction the caster is facing, for directional spells.</summary>
        public readonly Vector3 Direction;

        /// <summary>The caster, when there is one. Null for tests and for environment-triggered casts.</summary>
        public readonly NetworkIdentity Caster;

        /// <summary>Layers the effect may affect. Defaults to everything.</summary>
        public readonly int TargetLayerMask;

        /// <summary>Layers treated as sound-blocking walls when the effect makes noise.</summary>
        public readonly int GeometryLayerMask;

        public SpellEffectContext(SpellId spell, CastVolume volume, Vector3 origin, Vector3 direction,
            NetworkIdentity caster = null, int targetLayerMask = ~0, int geometryLayerMask = 0)
        {
            Spell = spell;
            Volume = volume;
            Origin = origin;
            Direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector3.forward;
            Caster = caster;
            TargetLayerMask = targetLayerMask;
            GeometryLayerMask = geometryLayerMask;
        }

        /// <summary>Power multiplier for the volume this was spoken at.</summary>
        public float Power => SpellTuning.PowerMultiplier(Volume);

        /// <summary>Effect radius after the volume multiplier.</summary>
        public float Radius() => Radius(SpellTuning.DefaultEffectRadius);

        public float Radius(float baseRadius) => baseRadius * Power;

        /// <summary>True when what resolved is a misfire rather than the intended spell.</summary>
        public bool IsMisfire => SpellCatalogue.IsMisfire(Spell);

        /// <summary>The caster's transform, or null when there is no caster.</summary>
        public Transform CasterTransform => Caster != null ? Caster.transform : null;
    }
}
