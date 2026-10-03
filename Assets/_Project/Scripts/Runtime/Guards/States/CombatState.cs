using Plunderspell.Alarm;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Combat (#210): the guard is in reach of a player and fights in turns. It asks the director's
    /// mediator for a turn (see <see cref="GuardAttackTurn"/>). With a turn a melee guard walks in and
    /// strikes, a ranged guard shoots; the turn is given back when the strike or shot is done. Without one
    /// the guard holds a place on a ring around the player (<see cref="CombatRing"/>) and asks again.
    /// A ranged guard whose shot would hit a teammate does not fire: it takes a new place on the ring.
    ///
    /// Combat holds while the player is inside the reach plus <c>CombatMargin</c>, so one step back does not
    /// flicker the guard to Chase and back. Past that it returns to Chase if it still sees the player and to
    /// Investigate if it does not. Movement stays with the navigation service, whose capsule sweep stops the
    /// guard short of a player, so a player pinned against a wall is never pushed (#200).
    /// </summary>
    public sealed class CombatState : GuardState
    {
        private const float SlotArrivalTolerance = 0.5f;

        private Transform _target;
        private Vector3 _lastSeenSpot;
        private float _secondsSinceSeen;
        private float _secondsToAsk;
        private float _secondsToRetarget;
        private float _recoverySecondsLeft;
        private int _sidestep;

        public CombatState(Guard guard) : base(guard)
        {
        }

        public override GuardAlertState AlertState => GuardAlertState.Combat;

        /// <summary>The player being fought, for tests and the debug overlay.</summary>
        public Transform Target => _target;

        /// <summary>Names the player to fight. Call before the guard enters this state.</summary>
        public void Engage(Transform player) => _target = player;

        private bool IsRanged => Context.RangedAttack.IsRanged;

        private float Reach => IsRanged ? Context.Tuning.RangedEngageRange : Context.Tuning.MeleeReach;

        // Waiting melee guards stand just outside reach; waiting archers stand just inside their range.
        private float RingRadius => IsRanged ? Reach - Context.Tuning.CombatRingPadding : Reach + Context.Tuning.CombatRingPadding;

        public override void Enter()
        {
            _lastSeenSpot = _target.position;
            _secondsSinceSeen = 0f;
            _secondsToAsk = 0f;
            _secondsToRetarget = 0f;
            _sidestep = 0;
            _recoverySecondsLeft = 0f;
            Context.AttackTurn.Begin();
            // Still a chaser as far as the alarm counts, but not a new sighting: it would score again.
            Context.Link.Director?.Publish(new IntruderSpotted(Context, _target, _lastSeenSpot, false));
        }

        public override void Exit()
        {
            Context.AttackTurn.End();
            Context.Navigator.Stop();
            Context.Link.Director?.Publish(new IntruderLost(Context, _lastSeenSpot));
        }

        public override State<Guard> Tick(float deltaTime)
        {
            Context.MeleeAttack.CoolDown(deltaTime);
            Context.RangedAttack.CoolDown(deltaTime);
            if (_target == null)
                return GiveUp();

            NoteSight(deltaTime);
            State<Guard> leaving = ReasonToLeave();
            if (leaving != null)
                return leaving;

            // A player out of a melee guard's reach (too high for the strike) is not fought on the ring (#237).
            if (!IsRanged && _secondsSinceSeen <= 0f && Context.Reach.IsUnreachable(_target, deltaTime))
            {
                Context.States.HoldBelow.Hold(_target);
                return Context.States.HoldBelow;
            }

            _secondsToRetarget -= deltaTime;
            FaceToward(_target, deltaTime);
            Fight(deltaTime);
            return this;
        }

        private void NoteSight(float deltaTime)
        {
            Transform seen = Context.Sight.Visible;
            if (seen == null)
            {
                _secondsSinceSeen += deltaTime;
                return;
            }

            _target = seen;
            _lastSeenSpot = seen.position;
            _secondsSinceSeen = 0f;
        }

        // Sight is a look every 80 ms or so and can miss once, so a short grace keeps one missed look from
        // ending the fight.
        private State<Guard> ReasonToLeave()
        {
            if (_secondsSinceSeen >= Context.Tuning.ChaseLoseSightSeconds)
                return GiveUp();

            float distance = Vector3.Distance(Context.transform.position, _target.position);
            if (distance <= Reach + Context.Tuning.CombatMargin)
                return null;

            Context.States.Chase.Follow(_target, true);
            return Context.States.Chase;
        }

        private State<Guard> GiveUp()
        {
            Context.Leads.Offer(_lastSeenSpot, GuardLeads.SightingStrength);
            return Context.States.Investigate;
        }

        private void Fight(float deltaTime)
        {
            if (RecoverFromAttack(deltaTime))
                return;

            if (!Context.AttackTurn.HasTurn)
                AskForTurnWhenDue(deltaTime);

            if (!Context.AttackTurn.HasTurn)
                HoldPlaceOnRing();
            else if (IsRanged)
                ShootOrSidestep();
            else
                CloseInAndStrike();
        }

        // The turn is kept for a short recovery after the attack, so strikes by different guards are spread
        // out in time instead of landing in the same frame one after another.
        private bool RecoverFromAttack(float deltaTime)
        {
            if (_recoverySecondsLeft <= 0f)
                return false;

            _recoverySecondsLeft -= deltaTime;
            if (_recoverySecondsLeft <= 0f)
                Context.AttackTurn.Release();
            return true;
        }

        // Only a guard that could attack right now asks, and only every so often, so waiting guards do not
        // flood the director and a guard that just struck stays at the back of the queue until its cooldown ends.
        private void AskForTurnWhenDue(float deltaTime)
        {
            _secondsToAsk -= deltaTime;
            if (_secondsToAsk > 0f)
                return;

            _secondsToAsk = Context.Tuning.TurnRequestSeconds;
            bool ready = IsRanged ? Context.RangedAttack.IsReady : Context.MeleeAttack.IsReady;
            if (ready)
                Context.AttackTurn.Request(_target, IsRanged);
        }

        private void HoldPlaceOnRing()
        {
            int guardId = Context.GetInstanceID();
            MoveWhenDue(CombatRing.SlotAround(_target.position, guardId, _sidestep, RingRadius));
        }

        private void CloseInAndStrike()
        {
            if (Context.MeleeAttack.TryStrike(_target))
            {
                _recoverySecondsLeft = Context.Tuning.AttackRecoverySeconds;
                return;
            }
            MoveWhenDue(_target.position);
        }

        private void ShootOrSidestep()
        {
            if (Context.RangedAttack.TryFire(_target, 0f))
            {
                _recoverySecondsLeft = Context.Tuning.AttackRecoverySeconds;
                return;
            }

            // Wait for the move to the last place to finish before trying another, or it would spin.
            if (Context.RangedAttack.LastShotBlocked && !Context.Navigator.IsMoving)
            {
                _sidestep++;
                HoldPlaceOnRing();
            }
        }

        // Re-plans on a timer and only when the spot has really moved, as Chase does, so the route
        // service is not asked every frame.
        private void MoveWhenDue(Vector3 spot)
        {
            if (_secondsToRetarget > 0f)
                return;

            Vector3? planned = Context.Navigator.Destination;
            bool standingThere = !planned.HasValue
                && Vector3.Distance(Context.transform.position, spot) <= SlotArrivalTolerance;
            float moved = planned.HasValue ? Vector3.Distance(planned.Value, spot) : float.MaxValue;
            if (standingThere || moved <= Context.Tuning.ChaseRetargetDistance)
                return;

            _secondsToRetarget = Context.Tuning.ChaseRetargetSeconds;
            Context.Navigator.MoveTo(spot, Context.Tuning.ChaseSpeed, MoveReason.Combat);
        }
    }
}
