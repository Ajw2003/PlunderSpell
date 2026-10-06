using System.Collections.Generic;
using Interfaces;
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
        private const float SwitchDistanceRatio = 2f / 3f;

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
                Look(intruders, GuardBrain.SightRange(_tuning.SightRange * GuardBrain.BaseSightScale, alarm));
        }

        private void Look(IReadOnlyList<Transform> intruders, float range)
        {
            LookCount++;
            Vector3 eye = _body.position + Vector3.up * _tuning.EyeHeight;
            Transform best = null;
            float bestDistance = float.MaxValue;
            Transform kept = null;
            float keptDistance = 0f;

            for (int i = 0; i < intruders.Count; i++)
            {
                Transform intruder = intruders[i];
                if (intruder == null || Downable.IsDown(intruder))
                    continue;

                float distance = Vector3.Distance(eye, intruder.position + Vector3.up * _tuning.TargetAimHeight);
                bool isCurrent = intruder == Visible;
                if ((distance >= bestDistance && !isCurrent) || !SeesAnyAimPoint(eye, intruder, range))
                    continue;

                if (isCurrent)
                {
                    kept = intruder;
                    keptDistance = distance;
                }
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = intruder;
                }
            }
            // Stickiness (#270): keep the current target while it is seen, unless another is under 2/3 as far.
            Visible = kept != null && bestDistance >= keptDistance * SwitchDistanceRatio ? kept : best;
        }

        // A sighting needs one clear aim point, head first (the usual hit): a ledge edge or a lintel that hides
        // the head can leave the body in view, and the other way round (#238).
        private bool SeesAnyAimPoint(Vector3 eye, Transform intruder, float range)
        {
            float head = _tuning.TargetAimHeight;
            float feet = _tuning.TargetLowAimHeight;
            float middle = (head + feet) * 0.5f;
            return CanSeePoint(eye, intruder, head, range)
                || CanSeePoint(eye, intruder, middle, range)
                || CanSeePoint(eye, intruder, feet, range);
        }

        // The cheap tests (range and angle) run before the raycast, so most points cost no cast.
        private bool CanSeePoint(Vector3 eye, Transform intruder, float heightAbovePivot, float range)
        {
            Vector3 aim = intruder.position + Vector3.up * heightAbovePivot;
            return GuardBrain.CanSee(eye, _body.forward, aim, range, _tuning.FieldOfView, true)
                && HasLineOfSight(eye, aim, intruder);
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
