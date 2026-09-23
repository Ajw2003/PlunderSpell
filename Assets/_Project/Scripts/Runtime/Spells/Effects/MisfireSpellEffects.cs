using System.Collections.Generic;
using Interfaces;
using RogueAi.Acoustics;
using UnityEngine;

namespace RogueAi.Spells
{
    /// <summary>
    /// Base for the misfires. The rule that makes them punishing is here rather than repeated in
    /// each one: a misfire targets the CASTER, not the world. Every primary effect deliberately
    /// excludes the caster from its search; every misfire deliberately aims at them.
    /// </summary>
    public abstract class MisfireEffectBase : SpellEffectBase
    {
        /// <summary>
        /// Finds the caster's own <typeparamref name="T"/>. Falls back to the nearest one in a tight
        /// radius when the caster carries no such component, so a misfire still lands on somebody —
        /// a harmless misfire would defeat the point of the mechanic.
        /// </summary>
        protected static T SelfTarget<T>(in SpellEffectContext ctx) where T : class
        {
            Transform caster = ctx.CasterTransform;
            if (caster != null)
            {
                var own = caster.GetComponentInParent<T>() ?? caster.GetComponentInChildren<T>();
                if (own != null)
                    return own;
            }
            return SpellTargeting.FindNearest<T>(ctx.Origin, SpellTuning.MisfireRadius, ctx.TargetLayerMask);
        }
    }

    /// <summary>Misfired Ignis — the caster catches fire.</summary>
    public sealed class MisfireIgnisEffect : MisfireEffectBase
    {
        public override SpellId Id => SpellId.MisfireIgnis;

        public override string Describe(in SpellEffectContext ctx) => "Misfire: you are on fire";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);
            var self = SelfTarget<IIgnitable>(ctx);
            if (self == null)
                return 0;
            self.Ignite(SpellTuning.MisfireSelfDamagePerSecond, SpellTuning.MisfireSelfBurnSeconds,
                ctx.CasterTransform != null ? ctx.CasterTransform.gameObject : null);
            return 1;
        }
    }

    /// <summary>
    /// Misfired Frango — shatters something the caster is carrying rather than the target. This is
    /// the one that costs a raid its payday, so it searches very close in.
    /// </summary>
    public sealed class MisfireFrangoEffect : MisfireEffectBase
    {
        public override SpellId Id => SpellId.MisFireFrango;

        public override string Describe(in SpellEffectContext ctx) => "Misfire: your own loot shatters";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            Transform caster = ctx.CasterTransform;
            IBreakable victim = null;

            if (caster != null)
            {
                foreach (IBreakable carried in caster.GetComponentsInChildren<IBreakable>())
                {
                    if (carried.IsBroken)
                        continue;
                    victim = carried;
                    break;
                }
            }

            if (victim == null)
            {
                foreach (IBreakable nearby in SpellTargeting.FindAll<IBreakable>(
                             ctx.Origin, SpellTuning.MisfireRadius, ctx.TargetLayerMask))
                {
                    if (nearby.IsBroken)
                        continue;
                    victim = nearby;
                    break;
                }
            }

            if (victim == null)
                return 0;

            victim.Break();
            EmitEffectNoise(ctx, 10f, 0.6f, NoiseType.GlassBreak);
            return 1;
        }
    }

    /// <summary>Misfired Levo — the caster floats off, uncontrolled.</summary>
    public sealed class MisfireLevoEffect : MisfireEffectBase
    {
        public override SpellId Id => SpellId.MisfireLevo;

        public override string Describe(in SpellEffectContext ctx) => "Misfire: you float away";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);
            var self = SelfTarget<ILevitatable>(ctx);
            if (self == null)
                return 0;
            self.Levitate(Vector3.up * SpellTuning.LevoImpulse, SpellTuning.LevoSeconds * 2f);
            return 1;
        }
    }

    /// <summary>Misfired Tonitrus — deafens and stuns the caster, and still wakes the castle.</summary>
    public sealed class MisfireTonitrusEffect : MisfireEffectBase
    {
        public override SpellId Id => SpellId.MisfireTonitrus;

        public override string Describe(in SpellEffectContext ctx) => "Misfire: deafening thunderclap on yourself";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            int affected = 0;
            var self = SelfTarget<IStunnable>(ctx);
            if (self != null)
            {
                self.Stun(SpellTuning.MisfireSelfStunSeconds);
                affected = 1;
            }

            // The clap happens whether or not it found someone to stun — the noise is the real cost.
            EmitEffectNoise(ctx, SpellTuning.TonitrusNoiseRadius, SpellTuning.TonitrusNoiseStrength,
                NoiseType.Explosion);
            return affected;
        }
    }

    /// <summary>Misfired Somnus — the caster falls asleep, in a castle full of guards.</summary>
    public sealed class MisfireSomnusEffect : MisfireEffectBase
    {
        public override SpellId Id => SpellId.MisFireSomnus;

        public override string Describe(in SpellEffectContext ctx) => "Misfire: you fall asleep";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);
            var self = SelfTarget<ISleepable>(ctx);
            if (self == null)
                return 0;
            self.Sleep(SpellTuning.MisfireSelfSleepSeconds);
            return 1;
        }
    }

    /// <summary>Misfired Cadaver Surge — the corpse detonates instead of rising.</summary>
    public sealed class MisfireCadaverSurgeEffect : MisfireEffectBase
    {
        public override SpellId Id => SpellId.MisFireCadaverSurge;

        /// <summary>Raised at the position of the corpse that exploded, with its damage radius.</summary>
        public static event System.Action<Vector3, float> CorpseExploded;

        public override string Describe(in SpellEffectContext ctx) => "Misfire: the corpse explodes";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            var corpse = SpellTargeting.FindNearest<IHealth>(ctx.Origin, ctx.Radius(),
                ctx.TargetLayerMask);
            Vector3 where = corpse is Component c ? c.transform.position : ctx.Origin;
            const float blastRadius = 5f;

            CorpseExploded?.Invoke(where, blastRadius);

            int hurt = 0;
            foreach (IHealth victim in SpellTargeting.FindAll<IHealth>(where, blastRadius, ctx.TargetLayerMask))
            {
                if (victim.CurrentHealth <= 0f)
                    continue;
                GameObject caster = ctx.CasterTransform != null ? ctx.CasterTransform.gameObject : null;
                Damage.Apply(victim, 25f, caster, caster, Damage.PointOn(victim as Component, where),
                    DamageKind.Spell);
                hurt++;
            }

            EmitEffectNoise(ctx, 15f, 1f, NoiseType.Explosion);
            return hurt;
        }
    }

    /// <summary>Misfired Aurum Voco — the coin arrives, scattered and extremely loud.</summary>
    public sealed class MisfireAurumVocoEffect : MisfireEffectBase
    {
        public override SpellId Id => SpellId.MisfireAurumVoco;

        /// <summary>Raised per scattered coin pile: (worth, world position).</summary>
        public static event System.Action<float, Vector3> GoldScattered;

        /// <summary>How many pieces the conjured pile breaks into when it misfires.</summary>
        public const int ScatterPieces = 4;

        public override string Describe(in SpellEffectContext ctx) => "Misfire: the gold scatters everywhere";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            float each = SpellTuning.AurumVocoWorth / ScatterPieces;
            for (int i = 0; i < ScatterPieces; i++)
            {
                float angle = i * (360f / ScatterPieces);
                Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * 2.5f;
                GoldScattered?.Invoke(each, ctx.Origin + offset);
            }

            EmitEffectNoise(ctx, SpellTuning.AurumVocoNoiseRadius * 2f, 0.9f, NoiseType.ItemDrop);
            return ScatterPieces;
        }
    }

    /// <summary>
    /// Misfired Porta — opens a door, but the wrong one: the farthest in range rather than the
    /// nearest. Opening a door across the hall is exactly how a guard walks in on you.
    /// </summary>
    public sealed class MisfirePortaEffect : MisfireEffectBase
    {
        public override SpellId Id => SpellId.MisfirePorta;

        public override string Describe(in SpellEffectContext ctx) => "Misfire: the wrong door opens";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            List<IOpenable> doors = SpellTargeting.FindAll<IOpenable>(
                ctx.Origin, ctx.Radius() * 2f, ctx.TargetLayerMask);

            for (int i = doors.Count - 1; i >= 0; i--)
            {
                if (doors[i].IsOpen)
                    continue;
                doors[i].Open();
                return 1;
            }
            return 0;
        }
    }
}
