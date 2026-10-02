using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// Who the director knows about: the guards that are alive and enabled, and the players (intruders)
    /// they look for. Lists only, no decisions. Guards are held as <see cref="Component"/> because the
    /// Alarm assembly sits below Guards, so callers cast to their guard type.
    /// </summary>
    public sealed class EnemyRegistry
    {
        /// <summary>Shared empty lists for callers that have no director, so they never allocate or null-check.</summary>
        public static readonly IReadOnlyList<Component> NoGuards = new List<Component>();
        public static readonly IReadOnlyList<Transform> NoIntruders = new List<Transform>();

        private readonly List<Component> _guards = new List<Component>();
        private readonly List<Transform> _intruders = new List<Transform>();

        public IReadOnlyList<Component> Guards => _guards;

        public IReadOnlyList<Transform> Intruders => _intruders;

        public void AddGuard(Component guard)
        {
            if (guard != null && !_guards.Contains(guard))
                _guards.Add(guard);
        }

        public void RemoveGuard(Component guard) => _guards.Remove(guard);

        public void AddIntruder(Transform intruder)
        {
            if (intruder != null && !_intruders.Contains(intruder))
                _intruders.Add(intruder);
        }

        public void RemoveIntruder(Transform intruder) => _intruders.Remove(intruder);

        public void ClearIntruders() => _intruders.Clear();

        public bool IsIntruder(Transform intruder) => _intruders.Contains(intruder);
    }
}
