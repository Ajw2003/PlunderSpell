using System;
using UnityEngine;

namespace Interfaces
{
    /// <summary>What kind of hurt a hit was, so feedback can tell a sword from a fire.</summary>
    public enum DamageKind
    {
        Impact,
        Melee,
        Projectile,
        Spell,
        Burn,
        EnemyAttack,
        Choke,
    }

    /// <summary>One hit that actually cost something health, as reported by <see cref="Damage.Dealt"/>.</summary>
    public readonly struct DamageReport
    {
        /// <summary>The component that lost health (the <see cref="IHealth"/> itself).</summary>
        public readonly Component Target;

        /// <summary>The object that physically did it: the thrown goblet, the bolt, the guard.</summary>
        public readonly GameObject Source;

        /// <summary>Who is responsible: the player who threw the goblet, the guard who fired the
        /// bolt. Null when nobody is (a vase knocked off a shelf by physics alone).</summary>
        public readonly GameObject Instigator;

        /// <summary>Health actually lost, which can be less than what was asked for.</summary>
        public readonly float Amount;

        public readonly Vector3 Point;
        public readonly DamageKind Kind;
        public readonly float HealthAfter;
        public readonly float MaxHealth;

        public DamageReport(Component target, GameObject source, GameObject instigator, float amount,
            Vector3 point, DamageKind kind, float healthAfter, float maxHealth)
        {
            Target = target;
            Source = source;
            Instigator = instigator;
            Amount = amount;
            Point = point;
            Kind = kind;
            HealthAfter = healthAfter;
            MaxHealth = maxHealth;
        }

        /// <summary>True when this hit took the target from alive to dead.</summary>
        public bool Killed => HealthAfter <= 0f && HealthAfter + Amount > 0f;

        /// <summary>True when the target hurt itself: its own misfire, its own thrown junk on the rebound.</summary>
        public bool SelfInflicted =>
            Instigator != null && Target != null && Instigator.transform.root == Target.transform.root;
    }

    /// <summary>
    /// The one way anything in the game loses health. Every weapon, spell, guard, fire and flying
    /// object calls <see cref="Apply"/>; it hurts the target and, if health really dropped, raises
    /// <see cref="Dealt"/> with who did it and where. Feedback (numbers, flashes, health bars) and
    /// any future kill feed or stats hang off that one event instead of off each weapon.
    /// </summary>
    public static class Damage
    {
        /// <summary>Raised after every hit that cost health. Listeners must not deal damage back
        /// synchronously.</summary>
        public static event Action<DamageReport> Dealt;

        /// <summary>
        /// Hurts <paramref name="target"/>. <paramref name="impactVelocity"/> routes to the
        /// impact overload of <see cref="IHealth.TakeDamage(float, float)"/>, which lets the target
        /// ignore soft bumps. Returns the health actually lost.
        /// </summary>
        public static float Apply(IHealth target, float amount, GameObject source, GameObject instigator,
            Vector3 point, DamageKind kind, float impactVelocity = -1f)
        {
            // IHealth is an interface, so Unity's destroyed-object check needs the concrete Object.
            var component = target as Component;
            if (target == null || component == null || amount <= 0f)
                return 0f;

            float before = target.CurrentHealth;
            if (before <= 0f)
                return 0f;

            if (impactVelocity >= 0f)
                target.TakeDamage(amount, impactVelocity);
            else
                target.TakeDamage(amount);

            // The component may have destroyed itself on death; read what we can.
            float after = component != null ? target.CurrentHealth : 0f;
            float lost = before - after;
            if (lost <= 0f)
                return 0f;

            Dealt?.Invoke(new DamageReport(component, source, instigator, lost, point, kind, after,
                component != null ? target.MaxHealth : before));
            return lost;
        }

        /// <summary>A sensible hit point on <paramref name="target"/> when the caller has no contact
        /// point: the nearest point of its collider to <paramref name="from"/>, else chest height.</summary>
        public static Vector3 PointOn(Component target, Vector3 from)
        {
            if (target == null)
                return from;

            var collider = target.GetComponentInChildren<Collider>();
            if (collider != null && collider.enabled && !(collider is MeshCollider mesh && !mesh.convex))
                return collider.ClosestPoint(from);

            return target.transform.position + Vector3.up * 1.2f;
        }
    }
}
