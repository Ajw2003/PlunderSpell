using System.Collections.Generic;
using Interfaces;
using Plunderspell.Acoustics;
using UnityEngine;

namespace Plunderspell.Spells
{
    /// <summary>
    /// Common plumbing for every built-in effect: the noise a cast makes, and the log line that
    /// makes a cast legible in a playtest.
    /// </summary>
    public abstract class SpellEffectBase : ISpellEffect
    {
        public abstract SpellId Id { get; }
        public abstract string Describe(in SpellEffectContext ctx);
        public abstract int Execute(in SpellEffectContext ctx);

        /// <summary>
        /// Emits the noise of speaking the words. Every cast does this, including a silent-looking
        /// one: the whole risk model is that casting is audible, and a spell that skipped it would
        /// be a free pass past the alarm.
        /// </summary>
        protected static int EmitCastNoise(in SpellEffectContext ctx)
        {
            return NoiseBroadcaster.Broadcast(
                ctx.Origin,
                SpellTuning.NoiseRadius(ctx.Volume),
                SpellTuning.NoiseStrength(ctx.Volume),
                NoiseType.VoiceCast,
                ~0,
                ctx.GeometryLayerMask);
        }

        /// <summary>Emits an extra, effect-specific noise (a thunderclap, a pile of coins landing).</summary>
        protected static int EmitEffectNoise(in SpellEffectContext ctx, float radius, float strength,
            NoiseType type)
        {
            return NoiseBroadcaster.Broadcast(ctx.Origin, radius * ctx.Power,
                Mathf.Clamp01(strength * ctx.Power), type, ~0, ctx.GeometryLayerMask);
        }
    }

    /// <summary>Ignis — sets the nearest burnable thing alight. Damage over time, not a single hit.</summary>
    public sealed class IgnisEffect : SpellEffectBase
    {
        public override SpellId Id => SpellId.Ignis;

        public override string Describe(in SpellEffectContext ctx) =>
            $"Ignis: fire, {SpellTuning.IgnisDamagePerSecond * ctx.Power:0} dps for {SpellTuning.IgnisBurnSeconds:0}s";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            var target = ctx.Aimed<IIgnitable>();
            if (target == null)
                return 0;

            target.Ignite(SpellTuning.IgnisDamagePerSecond * ctx.Power, SpellTuning.IgnisBurnSeconds,
                ctx.CasterTransform != null ? ctx.CasterTransform.gameObject : null);
            return 1;
        }
    }

    /// <summary>Frango — a force blast at what you aim at: hurts, staggers and shoves a creature, or
    /// smashes a door open. See docs/6-decisions/Decisions.md, 2026-09-23, on why it no longer breaks loot.</summary>
    public sealed class FrangoEffect : SpellEffectBase
    {
        public override SpellId Id => SpellId.Frango;

        public override string Describe(in SpellEffectContext ctx) =>
            $"Frango: force blast, {SpellTuning.FrangoDamage * ctx.Power:0} damage and a {SpellTuning.FrangoKnockback * ctx.Power:0.0}m shove";

        /// <summary>
        /// Breaks what you aim at: a creature takes a hit, staggers and is shoved back; a door is
        /// smashed open, locked or not. It used to shatter every breakable within 4 m, and the only
        /// breakable things were the players' own valuables, so the spell could only cost you (#106).
        /// </summary>
        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            IHealth creature = ctx.Aimed<IHealth>();
            IHandOpenable door = ctx.Aimed<IHandOpenable>();
            Vector3 eye = ctx.Origin - ctx.Direction;

            // Whichever of the two is nearer the crosshair's line of fire wins.
            if (door != null && !door.IsOpen && door is Component dc &&
                (creature == null || !(creature is Component cc) ||
                 Vector3.Distance(eye, dc.transform.position) < Vector3.Distance(eye, cc.transform.position)))
            {
                door.ForceOpen();
                EmitEffectNoise(ctx, 10f, 0.6f, NoiseType.GlassBreak);
                return 1;
            }

            if (creature == null || creature.CurrentHealth <= 0f || !(creature is Component target))
                return 0;

            GameObject caster = ctx.CasterTransform != null ? ctx.CasterTransform.gameObject : null;
            Damage.Apply(creature, SpellTuning.FrangoDamage * ctx.Power, caster, caster,
                Damage.PointOn(target, eye), DamageKind.Spell);

            if (target.GetComponentInParent<IStunnable>() is IStunnable stunnable)
                stunnable.Stun(SpellTuning.FrangoStaggerSeconds);

            Shove(target.transform.root, ctx.Direction, SpellTuning.FrangoKnockback * ctx.Power);
            EmitEffectNoise(ctx, 10f, 0.6f, NoiseType.GlassBreak);
            return 1;
        }

        /// <summary>
        /// Pushes a body back along the blast. A guard is moved through its NavMeshAgent so it cannot
        /// be shoved through a wall; anything else physical gets an impulse.
        /// </summary>
        private static void Shove(Transform body, Vector3 direction, float metres)
        {
            Vector3 flat = new Vector3(direction.x, 0f, direction.z).normalized * metres;
            var agent = body.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(flat);
                return;
            }

            var rb = body.GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
                rb.AddForce(flat * 4f, ForceMode.VelocityChange);
        }
    }

    /// <summary>Levo — lifts the nearest levitatable object, which is how heavy loot clears a wall.</summary>
    public sealed class LevoEffect : SpellEffectBase
    {
        public override SpellId Id => SpellId.Levo;

        public override string Describe(in SpellEffectContext ctx) =>
            $"Levo: lift, impulse {SpellTuning.LevoImpulse * ctx.Power:0.0} for {SpellTuning.LevoSeconds:0}s";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            var target = ctx.Aimed<ILevitatable>();
            if (target == null)
                return 0;

            target.Levitate(Vector3.up * (SpellTuning.LevoImpulse * ctx.Power), SpellTuning.LevoSeconds,
                ctx.CasterTransform != null ? ctx.CasterTransform.gameObject : null);
            return 1;
        }
    }

    /// <summary>
    /// Aurum Voco — conjures coin out of nothing. The only spell that creates value, and it lands in
    /// a clattering heap, so it trades gold for alarm.
    /// </summary>
    public sealed class AurumVocoEffect : SpellEffectBase
    {
        public override SpellId Id => SpellId.AurumVoco;

        /// <summary>Raised when coin is conjured: (worth, world position). The loot layer spawns it.</summary>
        public static event System.Action<float, Vector3> GoldConjured;

        public override string Describe(in SpellEffectContext ctx) =>
            $"Aurum Voco: {SpellTuning.AurumVocoWorth * ctx.Power:0} coin";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            Vector3 where = ctx.Origin + ctx.Direction * 1.5f;
            GoldConjured?.Invoke(SpellTuning.AurumVocoWorth * ctx.Power, where);
            EmitEffectNoise(ctx, SpellTuning.AurumVocoNoiseRadius, SpellTuning.AurumVocoNoiseStrength,
                NoiseType.ItemDrop);
            return 1;
        }
    }

    /// <summary>
    /// Tonitrus — a thunderclap that stuns everything nearby. Powerful and catastrophically loud:
    /// cast it once and the castle knows where you are.
    /// </summary>
    public sealed class TonitrusEffect : SpellEffectBase
    {
        public override SpellId Id => SpellId.Tonitrus;

        public override string Describe(in SpellEffectContext ctx) =>
            $"Tonitrus: stun {SpellTuning.TonitrusStunSeconds * ctx.Power:0.0}s within {ctx.Radius():0.0}m";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            int stunned = 0;
            Transform caster = ctx.CasterTransform;
            // Bursts where you aim, not on your own feet.
            foreach (IStunnable target in SpellTargeting.FindAll<IStunnable>(
                         ctx.AimPoint, ctx.Radius(), ctx.TargetLayerMask))
            {
                if (caster != null && target is Component c && c.transform.IsChildOf(caster))
                    continue;
                target.Stun(SpellTuning.TonitrusStunSeconds * ctx.Power);
                stunned++;
            }

            EmitEffectNoise(ctx, SpellTuning.TonitrusNoiseRadius, SpellTuning.TonitrusNoiseStrength,
                NoiseType.Explosion);
            return stunned;
        }
    }

    /// <summary>
    /// Somnus — puts guards to sleep. The stealth answer to a patrol, but only if whispered: at
    /// shout volume the noise of casting it wakes more than it sleeps.
    /// </summary>
    public sealed class SomnusEffect : SpellEffectBase
    {
        public override SpellId Id => SpellId.Somnus;

        public override string Describe(in SpellEffectContext ctx) =>
            $"Somnus: sleep {SpellTuning.SomnusSleepSeconds * ctx.Power:0.0}s within {ctx.Radius():0.0}m";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            int slept = 0;
            Transform caster = ctx.CasterTransform;
            foreach (ISleepable target in SpellTargeting.FindAll<ISleepable>(
                         ctx.AimPoint, ctx.Radius(), ctx.TargetLayerMask))
            {
                if (caster != null && target is Component c && c.transform.IsChildOf(caster))
                    continue;
                if (target.IsAsleep)
                    continue;
                target.Sleep(SpellTuning.SomnusSleepSeconds * ctx.Power);
                slept++;
            }
            return slept;
        }
    }

    /// <summary>
    /// Cadaver Surge — raises the nearest corpse. Until the necromancy milestone lands this reports
    /// the corpse it would raise and makes the noise of doing it, rather than pretending to succeed.
    /// </summary>
    public sealed class CadaverSurgeEffect : SpellEffectBase
    {
        public override SpellId Id => SpellId.CadaverSurge;

        /// <summary>Raised with the position of the corpse a cast targeted. The enemy layer listens.</summary>
        public static event System.Action<Vector3> CorpseRaised;

        public override string Describe(in SpellEffectContext ctx) =>
            $"Cadaver Surge: raise a corpse within {ctx.Radius():0.0}m";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            var corpse = ctx.Aimed<IHealth>();
            if (corpse == null || corpse.CurrentHealth > 0f)
                return 0;

            CorpseRaised?.Invoke(corpse is Component c ? c.transform.position : ctx.Origin);
            return 1;
        }
    }

    /// <summary>Porta — opens the nearest door. The quiet way through a locked castle.</summary>
    public sealed class PortaEffect : SpellEffectBase
    {
        public override SpellId Id => SpellId.Porta;

        public override string Describe(in SpellEffectContext ctx) =>
            $"Porta: open a door within {ctx.Radius():0.0}m";

        public override int Execute(in SpellEffectContext ctx)
        {
            EmitCastNoise(ctx);

            var door = ctx.Aimed<IOpenable>();
            if (door == null || door.IsOpen)
                return 0;

            door.Open();
            return 1;
        }
    }
}
