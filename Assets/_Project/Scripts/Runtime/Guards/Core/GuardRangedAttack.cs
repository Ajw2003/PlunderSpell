using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// A ranged guard's shot: the cooldown, the attack signal, the engaged event and the projectile.
    /// Ported from the legacy <c>CastleGuard.TryAttack</c>/<c>FireAt</c> (docs/reference/guard-legacy/
    /// CastleGuard.cs.txt:1157-1215) so Chase (#209) can shoot on the move and Combat (#210) can reuse it.
    /// A guard is ranged when its tuning has a projectile prefab. The same <c>NetworkedProjectile</c> the
    /// player's spells use is fired, so there is one projectile in the game.
    ///
    /// Friendly fire (#210): before every shot <see cref="GuardLineOfFire"/> looks for a teammate in the
    /// way. A blocked shot is not fired and does not spend the cooldown; <see cref="LastShotBlocked"/> tells
    /// Combat to sidestep. Chase and Combat both shoot through <see cref="TryFire"/>, so both are covered.
    /// </summary>
    public sealed class GuardRangedAttack
    {
        private readonly Guard _guard;
        private readonly GuardLineOfFire _lineOfFire;
        private float _cooldownLeft;

        public GuardRangedAttack(Guard guard)
        {
            _guard = guard;
            _lineOfFire = new GuardLineOfFire(guard);
        }

        public bool IsRanged => _guard.Tuning.ProjectilePrefab != null;

        /// <summary>True when the cooldown is over, so the guard may ask for a turn to shoot.</summary>
        public bool IsReady => _cooldownLeft <= 0f;

        /// <summary>True when the latest <see cref="TryFire"/> held fire because a teammate was in the line.</summary>
        public bool LastShotBlocked { get; private set; }

        /// <summary>Counts the cooldown down on the guard's own step time, so a test can drive it.</summary>
        public void CoolDown(float deltaTime) => _cooldownLeft = Mathf.Max(0f, _cooldownLeft - deltaTime);

        /// <summary>
        /// Counts the cooldown down and fires at <paramref name="target"/> when it is ready, in sight range
        /// and no teammate is in the line. Returns true on a shot.
        /// </summary>
        public bool TryFire(Transform target, float deltaTime)
        {
            CoolDown(deltaTime);
            LastShotBlocked = false;
            if (!IsRanged || !IsReady)
                return false;

            GuardTuning tuning = _guard.Tuning;
            Vector3 origin = _guard.transform.position + Vector3.up * tuning.EyeHeight;
            Vector3 aim = target.position + Vector3.up * tuning.TargetAimHeight;
            Vector3 toTarget = aim - origin;
            if (toTarget.magnitude > tuning.SightRange)
                return false;

            LastShotBlocked = _lineOfFire.IsBlockedByTeammate(origin, aim, target);
            if (LastShotBlocked)
                return false;

            _cooldownLeft = tuning.AttackCooldownSeconds;
            _guard.AttackSignal.Signal(GuardAttackKind.Projectile);
            _guard.Link.Director?.Publish(new GuardEngaged(_guard, target));
            Launch(origin, toTarget.normalized);
            return true;
        }

        private void Launch(Vector3 origin, Vector3 direction)
        {
            GuardTuning tuning = _guard.Tuning;
            GameObject shot = Object.Instantiate(tuning.ProjectilePrefab, origin, Quaternion.LookRotation(direction));
            if (shot.TryGetComponent(out NetworkedProjectile projectile))
            {
                projectile.Damage = Mathf.RoundToInt(tuning.AttackDamage);
                projectile.Instigator = _guard.gameObject;
            }

            // A shot must not hit the guard that fired it. Per shot, not per frame, so the array is fine.
            if (shot.TryGetComponent(out Collider shotCollider))
            {
                foreach (Collider own in _guard.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(shotCollider, own);
            }

            if (shot.TryGetComponent(out Rigidbody body))
                body.linearVelocity = direction * tuning.ProjectileSpeed;
        }
    }
}
