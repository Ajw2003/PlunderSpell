using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The wait before a guard notices someone at the side of its view (#484): 0.4 s of being in view there. A gap
    /// between sightings longer than <see cref="GapSeconds"/> (looks are 1/12 s apart) means the person left, and the
    /// wait starts again. Pure: time and a person in, yes or no out.
    /// </summary>
    public sealed class GuardSideNotice
    {
        public const float DelaySeconds = 0.4f;
        public const float GapSeconds = 0.25f;

        private readonly Dictionary<Transform, (float first, float last)> _inView = new Dictionary<Transform, (float first, float last)>();
        private float _now;

        public void Advance(float deltaTime) => _now += deltaTime;

        /// <summary>Records a sighting at the side and says whether the guard has now noticed the person.</summary>
        public bool Notices(Transform person)
        {
            if (!_inView.TryGetValue(person, out var seen) || _now - seen.last > GapSeconds)
                seen.first = _now;

            seen.last = _now;
            _inView[person] = seen;
            return _now - seen.first >= DelaySeconds;
        }
    }
}
