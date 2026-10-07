using Code.Scripts.EventSystems;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// The director's listener: the director's ears on the shared <see cref="EventManager"/>. When a guard reports
    /// (noise, a sighting, a death, an attack-turn request) this does what the director owes that event:
    /// scoring the alarm, counting chasers, handing an attack turn out or taking it back.
    /// It is not a bus and holds no events of its own: <see cref="EventManager"/> is the only event bus in the game.
    /// </summary>
    public sealed class EnemyDirectorListener
    {
        private readonly EnemyDirector _director;

        public EnemyDirectorListener(EnemyDirector director)
        {
            _director = director;
        }

        /// <summary>Starts hearing the guards' reports on the shared bus. Safe to call twice.</summary>
        public void Listen()
        {
            EventManager bus = EventManager.Instance;
            if (bus == null)
                return;

            StopListening();
            bus.Subscribe(this, (AttackTurnRequested e) => OnAttackTurnRequested(e));
            bus.Subscribe(this, (AttackTurnReleased e) => OnAttackTurnReleased(e));
            bus.Subscribe(this, (IntruderSpotted e) => OnIntruderSpotted(e));
            bus.Subscribe(this, (IntruderLost e) => OnIntruderLost(e));
            bus.Subscribe(this, (GuardEngaged e) => OnGuardEngaged(e));
            bus.Subscribe(this, (GuardDied e) => OnGuardDied(e));
            bus.Subscribe(this, (NoiseReported e) => OnNoiseReported(e));
        }

        public void StopListening() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private void OnAttackTurnRequested(AttackTurnRequested e)
        {
            _director.AttackTurns.Handle(e);
        }

        private void OnAttackTurnReleased(AttackTurnReleased e)
        {
            _director.AttackTurns.Release(e.Guard);
        }

        private void OnIntruderSpotted(IntruderSpotted e)
        {
            if (!_director.IsAuthority)
                return;
            if (e.FirstSighting)
                _director.Alarm.ReportSighting(e.Guard != null ? e.Guard.GetInstanceID() : 0);
            _director.Alarm.ReportChase(e.Guard != null ? e.Guard.GetInstanceID() : 0, true);
        }

        private void OnIntruderLost(IntruderLost e)
        {
            if (_director.IsAuthority)
                _director.Alarm.ReportChase(e.Guard != null ? e.Guard.GetInstanceID() : 0, false);
        }

        private void OnGuardEngaged(GuardEngaged e)
        {
            if (_director.IsAuthority)
                _director.Alarm.ReportAttack();
        }

        private void OnGuardDied(GuardDied e)
        {
            _director.ReleaseAttackTurnOf(e.Guard);
            if (_director.IsAuthority)
                _director.Alarm.ReportChase(e.Guard != null ? e.Guard.GetInstanceID() : 0, false);
        }

        private void OnNoiseReported(NoiseReported e)
        {
            if (_director.IsAuthority)
                _director.Alarm.ReportHeardNoise(e.Guard != null ? e.Guard.GetInstanceID() : 0, e.Strength);
        }
    }
}
