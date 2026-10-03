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
        private static GameObject _defaultStone;

        private float _cooldownLeft;
        private float _throwCooldownLeft;

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
        public void CoolDown(float deltaTime)
        {
            _cooldownLeft = Mathf.Max(0f, _cooldownLeft - deltaTime);
            _throwCooldownLeft = Mathf.Max(0f, _throwCooldownLeft - deltaTime);
        }

        /// <summary>True when the throw cooldown is over (#237). A melee guard throws only at a player it cannot reach.</summary>
        public bool IsThrowReady => _throwCooldownLeft <= 0f;

        /// <summary>
        /// Counts the cooldown down and fires at <paramref name="target"/> when it is ready, in sight range
        /// and no teammate is in the line. Returns true on a shot.
        /// </summary>
        public bool TryFire(Transform target, float deltaTime)
        {
            CoolDown(deltaTime);
            LastShotBlocked = false;
            GuardTuning tuning = _guard.Tuning;
            return IsRanged && Shoot(target, tuning.ProjectilePrefab, tuning.ProjectileSpeed, tuning.AttackDamage,
                ref _cooldownLeft, tuning.AttackCooldownSeconds);
        }

        /// <summary>
        /// A melee guard's stone (#237): the same shot path as <see cref="TryFire"/> (cooldown, line-of-fire
        /// check, signal, engaged event, projectile), with its own slower, weaker numbers and its own cooldown.
        /// Returns true on a throw.
        /// </summary>
        public bool TryThrow(Transform target, float deltaTime)
        {
            CoolDown(deltaTime);
            LastShotBlocked = false;
            GuardTuning tuning = _guard.Tuning;
            GameObject stone = tuning.ThrownPrefab != null ? tuning.ThrownPrefab : DefaultStone();
            return stone != null && Shoot(target, stone, tuning.ThrowSpeed, tuning.AttackDamage * tuning.ThrowDamageShare,
                ref _throwCooldownLeft, tuning.ThrowCooldownSeconds);
        }

        // The stone every melee guard throws unless its tuning names another. Loaded once for the whole game.
        private static GameObject DefaultStone()
        {
            if (_defaultStone == null)
                _defaultStone = Resources.Load<GameObject>("GuardStone");
            return _defaultStone;
        }

        private bool Shoot(Transform target, GameObject prefab, float speed, float damage, ref float cooldownLeft, float cooldownSeconds)
        {
            if (cooldownLeft > 0f)
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

            cooldownLeft = cooldownSeconds;
            _guard.AttackSignal.Signal(GuardAttackKind.Projectile);
            _guard.Link.Director?.Publish(new GuardEngaged(_guard, target));
            Launch(prefab, origin, toTarget.normalized, speed, damage);
            return true;
        }

        private void Launch(GameObject prefab, Vector3 origin, Vector3 direction, float speed, float damage)
        {
            GameObject shot = Object.Instantiate(prefab, origin, Quaternion.LookRotation(direction));
            if (shot.TryGetComponent(out NetworkedProjectile projectile))
            {
                projectile.Damage = Mathf.RoundToInt(damage);
                projectile.Instigator = _guard.gameObject;
            }

            // A shot must not hit the guard that fired it. Per shot, not per frame, so the array is fine.
            if (shot.TryGetComponent(out Collider shotCollider))
            {
                foreach (Collider own in _guard.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(shotCollider, own);
            }

            if (shot.TryGetComponent(out Rigidbody body))
                body.linearVelocity = direction * speed;
        }
    }
}
