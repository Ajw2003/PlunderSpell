using System;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// The director's event bus: plain C# events carrying readonly structs, so raising one allocates
    /// nothing. <c>Publish</c> raises the event, then does what the director owes that event: scoring
    /// the alarm, counting chasers, handing an attack turn out or taking it back. The director forwards
    /// its own events and Publish calls here, so callers still talk to the director.
    /// </summary>
    public sealed class EnemyDirectorBus
    {
        private readonly EnemyDirector _director;

        public EnemyDirectorBus(EnemyDirector director)
        {
            _director = director;
        }

        public event Action<NoiseReported> OnNoiseReported;
        public event Action<IntruderSpotted> OnIntruderSpotted;
        public event Action<IntruderLost> OnIntruderLost;
        public event Action<GuardEngaged> OnGuardEngaged;
        public event Action<AlarmChanged> OnAlarmChanged;
        public event Action<GuardDied> OnGuardDied;
        public event Action<InvestigateRequest> OnInvestigateRequest;
        public event Action<MoveRequest> OnMoveRequest;
        public event Action<PathReady> OnPathReady;
        public event Action<Arrived> OnArrived;
        public event Action<Blocked> OnBlocked;
        public event Action<AttackTurnRequested> OnAttackTurnRequested;
        public event Action<AttackTurnGranted> OnAttackTurnGranted;
        public event Action<AttackTurnDenied> OnAttackTurnDenied;
        public event Action<AttackTurnReleased> OnAttackTurnReleased;

        public void Publish(AttackTurnRequested e)
        {
            OnAttackTurnRequested?.Invoke(e);
            _director.AttackTurns.Handle(e);
        }

        public void Publish(AttackTurnGranted e) => OnAttackTurnGranted?.Invoke(e);

        public void Publish(AttackTurnDenied e) => OnAttackTurnDenied?.Invoke(e);

        public void Publish(AttackTurnReleased e)
        {
            OnAttackTurnReleased?.Invoke(e);
            _director.AttackTurns.Release(e.Guard);
        }

        public void Publish(IntruderSpotted e)
        {
            OnIntruderSpotted?.Invoke(e);
            if (!_director.IsAuthority)
                return;
            if (e.FirstSighting)
                _director.Alarm.ReportSighting();
            _director.Alarm.ReportChase(e.Guard != null ? e.Guard.GetInstanceID() : 0, true);
        }

        public void Publish(IntruderLost e)
        {
            OnIntruderLost?.Invoke(e);
            if (_director.IsAuthority)
                _director.Alarm.ReportChase(e.Guard != null ? e.Guard.GetInstanceID() : 0, false);
        }

        public void Publish(GuardEngaged e)
        {
            OnGuardEngaged?.Invoke(e);
            if (_director.IsAuthority)
                _director.Alarm.ReportAttack();
        }

        public void Publish(GuardDied e)
        {
            OnGuardDied?.Invoke(e);
            _director.ReleaseAttackTurnOf(e.Guard);
            if (_director.IsAuthority)
                _director.Alarm.ReportChase(e.Guard != null ? e.Guard.GetInstanceID() : 0, false);
        }

        public void Publish(NoiseReported e)
        {
            OnNoiseReported?.Invoke(e);
            if (_director.IsAuthority)
                _director.Alarm.ApplyNoise(e.Strength);
        }

        public void Publish(InvestigateRequest e) => OnInvestigateRequest?.Invoke(e);

        public void Publish(MoveRequest e) => OnMoveRequest?.Invoke(e);

        public void Publish(PathReady e) => OnPathReady?.Invoke(e);

        public void Publish(Arrived e) => OnArrived?.Invoke(e);

        public void Publish(Blocked e) => OnBlocked?.Invoke(e);

        public void Publish(AlarmChanged e) => OnAlarmChanged?.Invoke(e);
    }
}
