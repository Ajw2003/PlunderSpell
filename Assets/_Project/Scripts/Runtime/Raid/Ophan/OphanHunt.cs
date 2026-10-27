namespace Plunderspell.Raid
{
    /// <summary>What the Ophan is doing. Sent to every machine, which spins its rings and shows its tells from it.</summary>
    public enum OphanState
    {
        Absent = 0,
        Arriving = 1,
        Watching = 2,
        Locking = 3,
        Draining = 4,
        Charging = 5,
        Leaving = 6,
    }

    /// <summary>What one step of the hunt asks the Ophan to do.</summary>
    public struct OphanStep
    {
        /// <summary>Slow-beam damage to deal its target this step.</summary>
        public float Drain;
        /// <summary>The charged beam fired this step.</summary>
        public bool ChargedFire;
        /// <summary>A beam reached a target carrying an artifact this step: it takes the piece.</summary>
        public bool Take;
        /// <summary>It has finished leaving and can be removed.</summary>
        public bool Gone;
    }

    /// <summary>
    /// The Ophan's rules as plain time and facts, with no scene: it arrives, locks on to its target in the open, drains
    /// them with the slow beam while nothing blocks it, charges after its target has hidden long enough, takes an
    /// artifact instead of a life, and leaves once paid. <see cref="OphanOfHours"/> feeds it the facts on the host and
    /// acts on each <see cref="OphanStep"/>. See docs/4-systems/fear.md, "The Ophan in the sky".
    /// </summary>
    public sealed class OphanHunt
    {
        private readonly OphanTuning _tuning;
        private float _stateTime;
        private float _coverTime;
        private float _cooldown;
        private float _drainOwed;
        private float _sinceDamageStep;

        public OphanHunt(OphanTuning tuning) => _tuning = tuning;

        public OphanState State { get; private set; } = OphanState.Absent;

        /// <summary>Seconds spent in the current state; the look reads it for the tells' progress.</summary>
        public float StateTime => _stateTime;

        public void Arrive() => Enter(OphanState.Arriving);

        /// <summary>Paid: it leaves at once (HuntSecondsAfterPaid is 0, #433).</summary>
        public void Leave()
        {
            if (State != OphanState.Absent && State != OphanState.Leaving)
                Enter(OphanState.Leaving);
        }

        /// <summary>
        /// Advances the hunt by <paramref name="dt"/> seconds. <paramref name="hasTarget"/>: a living player to hunt.
        /// <paramref name="inCover"/>: stone stands between its eye and that player. <paramref name="carriesArtifact"/>:
        /// that player holds a wanted piece.
        /// </summary>
        public OphanStep Tick(float dt, bool hasTarget, bool inCover, bool carriesArtifact)
        {
            var step = new OphanStep();
            _stateTime += dt;
            _cooldown -= dt;
            switch (State)
            {
                case OphanState.Arriving:
                    if (_stateTime >= _tuning.ArriveSeconds)
                        Enter(OphanState.Watching);
                    break;

                case OphanState.Watching:
                    if (hasTarget && !inCover)
                        Enter(OphanState.Locking);
                    break;

                case OphanState.Locking:
                    // Getting behind stone during the lock-on escapes it.
                    if (!hasTarget || inCover)
                        Enter(OphanState.Watching);
                    else if (_stateTime >= _tuning.LockSeconds)
                        Enter(OphanState.Draining);
                    break;

                case OphanState.Draining:
                    if (!hasTarget)
                    {
                        Enter(OphanState.Watching);
                        break;
                    }
                    if (inCover)
                    {
                        _coverTime += dt;
                        if (_coverTime >= _tuning.CoverSecondsBeforeCharge && _cooldown <= 0f)
                            Enter(OphanState.Charging);
                        break;
                    }
                    _coverTime = 0f;
                    if (carriesArtifact)
                    {
                        step.Take = true;
                        Enter(OphanState.Leaving);
                        break;
                    }
                    _drainOwed += _tuning.SlowBeamDamagePerSecond * dt;
                    _sinceDamageStep += dt;
                    if (_sinceDamageStep >= _tuning.DamageStepSeconds)
                    {
                        step.Drain = _drainOwed;
                        _drainOwed = 0f;
                        _sinceDamageStep = 0f;
                    }
                    break;

                case OphanState.Charging:
                    if (!hasTarget)
                    {
                        Enter(OphanState.Watching);
                        break;
                    }
                    if (_stateTime >= _tuning.ChargeSeconds)
                    {
                        // The charged beam goes through stone: hiding does not save you from it.
                        if (carriesArtifact)
                        {
                            step.Take = true;
                            Enter(OphanState.Leaving);
                        }
                        else
                        {
                            step.ChargedFire = true;
                            _cooldown = _tuning.ChargeCooldownSeconds;
                            Enter(OphanState.Draining);
                        }
                    }
                    break;

                case OphanState.Leaving:
                    if (_stateTime >= _tuning.LeaveSeconds)
                    {
                        step.Gone = true;
                        Enter(OphanState.Absent);
                    }
                    break;
            }
            return step;
        }

        private void Enter(OphanState state)
        {
            State = state;
            _stateTime = 0f;
            _coverTime = 0f;
            _drainOwed = 0f;
            _sinceDamageStep = 0f;
        }
    }
}
