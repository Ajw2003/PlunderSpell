using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Turns a standing guard in place (#483): <see cref="BeginSweep"/> looks 60 degrees left, then 60 degrees
    /// right, then back to the heading it started with; <see cref="BeginTurnToward"/> faces a point. A state
    /// calls <see cref="Tick"/> from its own Tick, so there is no Update of its own. Yaw only: the position stays
    /// with the navigation service.
    /// </summary>
    public sealed class GuardLookAround
    {
        /// <summary>How far each side of the starting heading the guard looks.</summary>
        public const float SweepDegrees = 60f;

        private readonly Guard _guard;
        private readonly float[] _headings = new float[3];
        private int _count;
        private int _next;

        public GuardLookAround(Guard guard)
        {
            _guard = guard;
        }

        /// <summary>True from a Begin call until the last heading is reached.</summary>
        public bool IsTurning => _next < _count;

        public void BeginSweep()
        {
            float start = _guard.transform.eulerAngles.y;
            _headings[0] = start - SweepDegrees;
            _headings[1] = start + SweepDegrees;
            _headings[2] = start;
            Start(3);
        }

        public void BeginTurnToward(Vector3 point)
        {
            Vector3 toPoint = point - _guard.transform.position;
            toPoint.y = 0f;
            if (toPoint.sqrMagnitude < 0.0001f)
            {
                Cancel();
                return;
            }
            _headings[0] = Quaternion.LookRotation(toPoint, Vector3.up).eulerAngles.y;
            Start(1);
        }

        public void Cancel() => Start(0);

        /// <summary>Turns toward the current heading; moves on to the next one when it is reached.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsTurning)
                return;

            Quaternion heading = Quaternion.Euler(0f, _headings[_next], 0f);
            Transform body = _guard.transform;
            body.rotation = Quaternion.RotateTowards(body.rotation, heading, _guard.Tuning.TurnDegreesPerSecond * deltaTime);
            if (Quaternion.Angle(body.rotation, heading) < 0.01f)
                _next++;
        }

        private void Start(int count)
        {
            _count = count;
            _next = 0;
        }
    }
}
