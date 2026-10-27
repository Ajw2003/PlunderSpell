using Code.Scripts.EventSystems;
using Interfaces;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// A melee guard's strike (#210): the cooldown, the attack signal, the engaged event and the damage.
    /// Ported from the legacy <c>CastleGuard.TryAttack</c> (docs/reference/guard-legacy/CastleGuard.cs.txt:
    /// 1157-1180): an instant hit at <c>MeleeReach</c> with the 1.4 s cooldown. The legacy swing has no
    /// windup, so none is added here. The signal is what an animator or a sound hooks; damage is applied on
    /// the server only, through the one <see cref="Damage"/> path.
    /// </summary>
    public sealed class GuardMeleeAttack
    {
        private readonly Guard _guard;
        private float _cooldownLeft;
        private Transform _pendingCatch;

        public GuardMeleeAttack(Guard guard)
        {
            _guard = guard;
        }

        public bool IsReady => _cooldownLeft <= 0f;

        /// <summary>Counts the cooldown down on the guard's own step time, so a test can drive it.</summary>
        public void CoolDown(float deltaTime) => _cooldownLeft = Mathf.Max(0f, _cooldownLeft - deltaTime);

        /// <summary>Strikes <paramref name="target"/> when it is within reach and the cooldown is over.
        /// Returns true on a strike.</summary>
        public bool TryStrike(Transform target)
        {
            if (!IsReady)
                return false;

            Vector3 origin = _guard.transform.position + Vector3.up * _guard.Tuning.EyeHeight;
            if (Vector3.Distance(_guard.transform.position, target.position) > _guard.Tuning.MeleeReach)
                return false;

            _cooldownLeft = _guard.Tuning.AttackCooldownSeconds;
            _guard.AttackSignal.Signal(GuardAttackKind.Melee);
            EventManager.Instance?.Publish(new GuardEngaged(_guard, target));
            // The Wick's catch (#417) lands on the AttackHit event of wick_catch, not at the strike.
            if (_guard.IsTurned)
                _pendingCatch = target;
            else
                Hit(target);
            return true;
        }

        /// <summary>The Wick's catch lands (the AttackHit event): hurts the player it reached, if still near.</summary>
        public void LandCatch()
        {
            Transform target = _pendingCatch;
            _pendingCatch = null;
            if (target != null && Vector3.Distance(_guard.transform.position, target.position) <= _guard.Tuning.MeleeReach * 1.5f)
                Hit(target);
        }

        private void Hit(Transform target)
        {
            Vector3 origin = _guard.transform.position + Vector3.up * _guard.Tuning.EyeHeight;
            if (target.TryGetComponent(out IHealth health))
            {
                Damage.Apply(health, _guard.Tuning.AttackDamage, _guard.gameObject, _guard.gameObject,
                    Damage.PointOn(target, origin), DamageKind.EnemyAttack);
            }
        }
    }
}
