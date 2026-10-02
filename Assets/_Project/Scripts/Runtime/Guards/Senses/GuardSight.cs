using System.Collections.Generic;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// What a guard can see: the cone, the range, the arrival grace and the line of sight. It looks only
    /// when <see cref="GuardSightThrottle"/> says so and keeps the answer until the next look, so a
    /// state asking "who do I see" every frame costs nothing. The line of sight ignores the target's
    /// own colliders, so a player's body never hides the player from a guard.
    /// </summary>
    public sealed class GuardSight
    {
        private const int HitBufferSize = 8;

        private readonly Transform _body;
        private readonly GuardTuning _tuning;
        private readonly GuardSightThrottle _throttle;
        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];
        private bool _closed;

        public GuardSight(Transform body, GuardTuning tuning, int guardId)
        {
            _body = body;
            _tuning = tuning;
            _throttle = new GuardSightThrottle(guardId);
        }

        /// <summary>The nearest intruder seen at the last look, or null.</summary>
        public Transform Visible { get; private set; }

        /// <summary>How many looks this guard has taken. For tests and the perf check.</summary>
        public int LookCount { get; private set; }

        /// <summary>Shuts the eyes for good (death, #213): nothing is seen from now on.</summary>
        public void Close()
        {
            _closed = true;
            Visible = null;
        }

        /// <summary>Test seam: the next <see cref="Update"/> looks at once.</summary>
        public void LookNext() => _throttle.LookNext();

        /// <summary>
        /// Advances the throttle and looks when it is this guard's turn. A guard that cannot see
        /// (incapacitated, or inside the calm arrival grace) sees nobody.
        /// </summary>
        public void Update(IReadOnlyList<Transform> intruders, AlarmState alarm, bool blind, float deltaTime)
        {
            if (_closed || blind || (alarm == AlarmState.Calm && GuardArrivalGrace.IsActive))
            {
                Visible = null;
                return;
            }

            if (_throttle.IsLookDue(deltaTime))
                Look(intruders);
        }

        private void Look(IReadOnlyList<Transform> intruders)
        {
            LookCount++;
            Vector3 eye = _body.position + Vector3.up * _tuning.EyeHeight;
            Transform best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < intruders.Count; i++)
            {
                Transform intruder = intruders[i];
                if (intruder == null)
                    continue;

                Vector3 aim = intruder.position + Vector3.up * _tuning.TargetAimHeight;
                float distance = Vector3.Distance(eye, aim);
                if (distance >= bestDistance || !IsInsideCone(eye, aim) || !HasLineOfSight(eye, aim, intruder))
                    continue;

                bestDistance = distance;
                best = intruder;
            }
            Visible = best;
        }

        // The cheap tests (range and angle) run before the raycast, so most intruders cost no cast.
        private bool IsInsideCone(Vector3 eye, Vector3 aim)
        {
            return GuardBrain.CanSee(eye, _body.forward, aim, _tuning.SightRange, _tuning.FieldOfView, true);
        }

        private bool HasLineOfSight(Vector3 eye, Vector3 aim, Transform target)
        {
            Vector3 toTarget = aim - eye;
            float distance = toTarget.magnitude;
            if (distance <= 0f)
                return true;

            int count = Physics.RaycastNonAlloc(eye, toTarget / distance, _hits, distance,
                _tuning.GeometryLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!_hits[i].collider.transform.IsChildOf(target))
                    return false;
            }
            return true;
        }
    }
}
