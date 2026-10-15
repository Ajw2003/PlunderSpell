using Code.Scripts.EventSystems;
using Interfaces;
using Plunderspell.Acoustics;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The rules of a guard that turns into the Wick (#417); a no-op unless its tuning says
    /// <see cref="TrueFormTuning.CanTurn"/> (the Lantern Warden). It turns when it confirms sight of a
    /// player (<see cref="TurnOnSight"/>), is hit, or is woken by a missed Somnus; hearing alone only
    /// sends it to investigate. The turn is one way. In human form it is never killed
    /// (<see cref="LimitDamage"/>) and a hard blow to the head stuns it, then it turns when it gets up.
    /// Decided on the server only.
    /// </summary>
    public sealed class GuardTrueForm
    {
        private readonly Guard _guard;
        private bool _turnWhenUp;

        public GuardTrueForm(Guard guard)
        {
            _guard = guard;
            guard.Hearing.WokenByNoise += OnWokenByNoise;
            guard.Status.StatusChanged += OnStatusChanged;
            EventManager.Instance?.Subscribe<DamageDealt>(this, OnDamageDealt);
        }

        private TrueFormTuning Tuning => _guard.Tuning.TrueForm;

        public bool CanTurn => Tuning.CanTurn;

        public void Dispose()
        {
            _guard.Hearing.WokenByNoise -= OnWokenByNoise;
            _guard.Status.StatusChanged -= OnStatusChanged;
            EventManager.Instance?.Unsubscribe<DamageDealt>(this);
        }

        /// <summary>The most health a hit may take: a Lantern Warden always keeps one point, in either form.</summary>
        public float LimitDamage(float damage) => CanTurn ? Mathf.Min(damage, _guard.Health.Current - 1f) : damage;

        /// <summary>Called by Chase when it confirms a player in view. True when the guard is turning instead.</summary>
        public bool TurnOnSight(Transform seen) => Turn(seen, seen.position);

        private void OnWokenByNoise(NoiseEvent noise)
        {
            if (noise.Type == NoiseType.VoiceCast)
                Turn(null, noise.Origin);
        }

        private void OnDamageDealt(DamageDealt dealt)
        {
            DamageReport report = dealt.Report;
            if (report.Target != _guard || _guard.IsTurned)
                return;

            if (IsHardBlowToHead(report))
                Fall();
            else
                Turn(null, report.Instigator != null ? report.Instigator.transform.position : report.Point);
        }

        private bool IsHardBlowToHead(DamageReport report)
        {
            float heightOverFeet = report.Point.y - _guard.transform.position.y;
            return report.Amount >= Tuning.HardBlowDamage && heightOverFeet >= Tuning.HeadHeight;
        }

        // Knocked down and stunned; the hit is not forgotten, so it turns when the stun ends.
        private void Fall()
        {
            if (!_guard.IsAuthority || !CanTurn)
                return;
            _turnWhenUp = true;
            _guard.Status.Stun(Tuning.HeadStunSeconds);
        }

        private void OnStatusChanged(Plunderspell.Status.StatusEffectReceiver status)
        {
            if (_turnWhenUp && !status.IsIncapacitated)
            {
                _turnWhenUp = false;
                Turn(null, _guard.transform.position + _guard.transform.forward);
            }
        }

        private bool Turn(Transform target, Vector3 spot)
        {
            if (!CanTurn || _guard.IsTurned || _guard.IsDead || !_guard.IsAuthority)
                return false;

            _turnWhenUp = false;
            _guard.Status.ClearAll();
            _guard.States.Turning.Begin(target, spot);
            _guard.ChangeState(_guard.States.Turning);
            return true;
        }
    }
}
