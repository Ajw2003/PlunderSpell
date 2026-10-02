using UnityEngine;

namespace Plunderspell.Alarm
{
    // Payloads for the attack turn mediator (#210). Readonly structs like the rest of the director's bus, so
    // raising one never allocates. A guard is carried as a Component because the Alarm assembly sits below Guards.

    /// <summary>A guard in combat asks for a turn to attack <see cref="Target"/>. Answered with
    /// <see cref="AttackTurnGranted"/> or <see cref="AttackTurnDenied"/>.</summary>
    public readonly struct AttackTurnRequested
    {
        public readonly Component Guard;
        public readonly Transform Target;
        /// <summary>True for a guard that shoots; ranged and melee guards have separate turn counts.</summary>
        public readonly bool Ranged;
        public AttackTurnRequested(Component guard, Transform target, bool ranged)
        {
            Guard = guard; Target = target; Ranged = ranged;
        }
    }

    /// <summary>The guard may attack now. The turn is held until released, the guard dies, or it times out.</summary>
    public readonly struct AttackTurnGranted
    {
        public readonly Component Guard;
        public readonly Transform Target;
        public AttackTurnGranted(Component guard, Transform target) { Guard = guard; Target = target; }
    }

    /// <summary>Too many guards are already attacking that player; the guard waits and asks again.</summary>
    public readonly struct AttackTurnDenied
    {
        public readonly Component Guard;
        public readonly Transform Target;
        public AttackTurnDenied(Component guard, Transform target) { Guard = guard; Target = target; }
    }

    /// <summary>The guard is done with its turn (the strike ended, or it left combat).</summary>
    public readonly struct AttackTurnReleased
    {
        public readonly Component Guard;
        public AttackTurnReleased(Component guard) { Guard = guard; }
    }
}
