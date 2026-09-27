using System.Collections.Generic;
using Interfaces;
using Plunderspell.Acoustics;
using UnityEngine;

namespace Plunderspell.Spells
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

    /// <summary>Misfired Velox — a full-force dash, in a direction nobody chose.</summary>
    public sealed class MisfireVeloxEffect : MisfireEffectBase, ICasterMovementSpell
    {
        public override SpellId Id => SpellId.MisfireVelox;

        public override string Describe(in SpellEffectContext ctx) => "Misfire: you dash the wrong way";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);
            return 1;
        }

        public bool CanMove(in SpellEffectContext ctx) => MovableCaster.Of(ctx) != null;

        public void MoveCaster(in SpellEffectContext ctx)
        {
            Vector2 flat = Random.insideUnitCircle;
            if (flat.sqrMagnitude < 0.01f)
                flat = Vector2.right;
            ISpellMovable body = MovableCaster.Of(ctx);
            if (body != null)
                body.SpellDash(new Vector3(flat.x, 0f, flat.y), SpellTuning.VeloxDashSpeed,
                    SpellTuning.VeloxDashSeconds);
        }
    }

    /// <summary>Misfired Saltus — a feeble hop, and the legs lock.</summary>
    public sealed class MisfireSaltusEffect : MisfireEffectBase, ICasterMovementSpell
    {
        public override SpellId Id => SpellId.MisfireSaltus;

        public override string Describe(in SpellEffectContext ctx) => "Misfire: a feeble hop, and you stumble";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);
            return 1;
        }

        // A misfire is never free, so it is paid for even in mid-air, where the hop does nothing.
        public bool CanMove(in SpellEffectContext ctx) => MovableCaster.Of(ctx) != null;

        public void MoveCaster(in SpellEffectContext ctx)
        {
            ISpellMovable body = MovableCaster.Of(ctx);
            if (body == null)
                return;
            body.SpellLaunch(SpellTuning.SaltusLaunchSpeed * SpellTuning.MisfireSaltusHop, 0f);
            body.Stagger(SpellTuning.MisfireSaltusStaggerSeconds);
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
