using System;
using System.Threading.Tasks;

namespace StateMachine.States
{
    public class PlayerRespawnState : PlayerState
    {
        public PlayerRespawnState(PlayerStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            _stateMachine.dead = false;
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(_stateMachine.RespawnSpeed));
                Exit();
            });
        }
    }
}
