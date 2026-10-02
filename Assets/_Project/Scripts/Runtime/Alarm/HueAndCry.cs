using System;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// The hue and cry: one investigate request at each player, so each guard in range can take the
    /// nearest. It is a request, not an order: guards decide what to do with it (#205).
    /// </summary>
    public sealed class HueAndCry
    {
        private readonly EnemyRegistry _registry;
        private readonly Action<InvestigateRequest> _publish;

        public HueAndCry(EnemyRegistry registry, Action<InvestigateRequest> publish)
        {
            _registry = registry;
            _publish = publish;
        }

        public void Raise()
        {
            for (int i = 0; i < _registry.Intruders.Count; i++)
            {
                if (_registry.Intruders[i] != null)
                    _publish(new InvestigateRequest(_registry.Intruders[i].position, InvestigateReason.HueAndCry));
            }
        }
    }
}
