using Code.Scripts.EventSystems;
using Plunderspell.Alarm;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Hold below (#237): a melee guard sees a player it cannot reach (see <see cref="GuardReachability"/>).
    /// It stays where the floor ends, within throwing range of the player, throws stones on their own
    /// cooldown, and calls other guards to the spot through the director. It does not walk away. When the
    /// player comes back within reach it returns to Chase, which hands over to Combat; when the player has
    /// been out of sight for a while it gives up like Chase does.
    ///
    /// A throw uses the ranged attack turn, so the director's limit on how many guards shoot one player at
    /// once covers stones as well as bolts. The turn is given back straight after each throw: there is no
    /// recovery window, because the throw cooldown is already longer.
    /// </summary>
    public sealed class HoldBelowState : GuardState
    {
        private const float SlotArrivalTolerance = 0.5f;

        private Transform _target;
        private Vector3 _lastSeenSpot;
        private float _secondsSinceSeen;
        private float _secondsToAsk;
        private float _secondsToRetarget;
        private int _sidestep;

        public HoldBelowState(Guard guard) : base(guard)
        {
        }

        // Still a fight as far as clients and the HUD care; a new replicated value is not worth it.
        public override GuardAlertState AlertState => GuardAlertState.Combat;

        /// <summary>The player being held below, for tests and the debug overlay.</summary>
        public Transform Target => _target;

        /// <summary>Names the player to hold below. Call before the guard enters this state.</summary>
        public void Hold(Transform player) => _target = player;

        public override void Enter()
        {
            _lastSeenSpot = _target.position;
            _secondsSinceSeen = 0f;
            _secondsToAsk = 0f;
            _secondsToRetarget = 0f;
            _sidestep = 0;
            Context.Navigator.Stop();
            Context.AttackTurn.Begin();
            EventManager.Instance?.Publish(new IntruderSpotted(Context, _target, _lastSeenSpot, false));
            EventManager.Instance?.Publish(new UnreachableIntruderReported(Context, _target));
        }

        public override void Exit()
        {
            Context.AttackTurn.End();
            Context.Navigator.Stop();
            Context.Reach.Reset();
            EventManager.Instance?.Publish(new IntruderLost(Context, _lastSeenSpot));
        }

        public override State<Guard> Tick(float deltaTime)
        {
            Context.RangedAttack.CoolDown(deltaTime);
            if (_target == null)
                return GiveUp();

            Transform seen = Context.Sight.Visible;
            if (seen == null)
            {
                _secondsSinceSeen += deltaTime;
                return _secondsSinceSeen >= Context.Tuning.UnreachableLoseSightSeconds ? GiveUp() : this;
            }

            _target = seen;
            _lastSeenSpot = seen.position;
            _secondsSinceSeen = 0f;
            if (!Context.Reach.IsUnreachable(seen, deltaTime))
            {
                Context.States.Chase.Follow(seen, true);
                return Context.States.Chase;
            }

            FaceToward(_target, deltaTime);
            TakePlace(deltaTime);
            Throw(deltaTime);
            return this;
        }

        private State<Guard> GiveUp()
        {
            Context.Leads.Offer(_lastSeenSpot, GuardLeads.SightingStrength);
            return Context.States.Investigate;
        }

        // A guard already within throwing range stays where it is; it moves only to get out of the way of
        // a teammate's line (sidestep) or to get close enough.
        private void TakePlace(float deltaTime)
        {
            _secondsToRetarget -= deltaTime;
            if (_secondsToRetarget > 0f)
                return;

            Vector3 here = Context.transform.position;
            Vector3 groundSpot = new Vector3(_target.position.x, here.y, _target.position.z);
            bool inRange = Vector3.Distance(here, groundSpot) <= Context.Tuning.ThrowStandOff + SlotArrivalTolerance;
            if (_sidestep == 0 && inRange)
                return;

            Vector3 slot = CombatRing.SlotAround(groundSpot, Context.GetInstanceID(), _sidestep, Context.Tuning.ThrowStandOff);
            Vector3? planned = Context.Navigator.Destination;
            bool standingThere = !planned.HasValue && Vector3.Distance(here, slot) <= SlotArrivalTolerance;
            bool alreadyGoing = planned.HasValue && Vector3.Distance(planned.Value, slot) <= Context.Tuning.ChaseRetargetDistance;
            if (standingThere || alreadyGoing)
                return;

            _secondsToRetarget = Context.Tuning.ChaseRetargetSeconds;
            Context.Navigator.MoveTo(slot, Context.Tuning.ChaseSpeed, MoveReason.Combat);
        }

        private void Throw(float deltaTime)
        {
            if (!Context.AttackTurn.HasTurn)
                AskForTurnWhenDue(deltaTime);
            if (!Context.AttackTurn.HasTurn)
                return;

            if (Context.RangedAttack.TryThrow(_target, 0f))
            {
                Context.AttackTurn.Release();
                return;
            }

            // A teammate is in the line: give the turn back and take another place round the player.
            if (Context.RangedAttack.LastShotBlocked)
            {
                Context.AttackTurn.Release();
                if (!Context.Navigator.IsMoving)
                {
                    _sidestep++;
                    _secondsToRetarget = 0f;
                }
            }
        }

        private void AskForTurnWhenDue(float deltaTime)
        {
            _secondsToAsk -= deltaTime;
            if (_secondsToAsk > 0f)
                return;

            _secondsToAsk = Context.Tuning.TurnRequestSeconds;
            if (Context.RangedAttack.IsThrowReady)
                Context.AttackTurn.Request(_target, true);
        }
    }
}
