using System;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// The hue and cry: one investigate request at each player, so every guard can take the nearest. It is
    /// a request, not an order: guards decide what to do with it (#205). While the alarm stays at Hue and
    /// Cry it is raised again every few seconds at the players' current positions (#239), because a guard
    /// that reaches a stale spot finds nothing and goes back to patrol.
    /// </summary>
    public sealed class HueAndCry
    {
        private readonly EnemyRegistry _registry;
        private readonly Action<InvestigateRequest> _publish;
        private readonly float _repeatSeconds;
        private float _secondsSinceRaise;

        public HueAndCry(EnemyRegistry registry, Action<InvestigateRequest> publish, float repeatSeconds)
        {
            _registry = registry;
            _publish = publish;
            _repeatSeconds = repeatSeconds;
        }

        public void Raise()
        {
            _secondsSinceRaise = 0f;
            for (int i = 0; i < _registry.Intruders.Count; i++)
            {
                if (_registry.Intruders[i] != null && !Interfaces.Downable.IsDown(_registry.Intruders[i]))
                    _publish(new InvestigateRequest(_registry.Intruders[i].position, InvestigateReason.HueAndCry));
            }
        }

        /// <summary>Counts time since the last raise and raises again when the repeat interval has passed. Call only while the alarm is at Hue and Cry.</summary>
        public void Repeat(float deltaTime)
        {
            _secondsSinceRaise += deltaTime;
            if (_secondsSinceRaise >= _repeatSeconds)
                Raise();
        }
    }
}
