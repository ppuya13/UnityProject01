using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateMove: State
    {
        public override void EnterState()
        {
            Monster.SendDestination(Monster.FindMoveDestination());
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
            if (!Monster.agent.pathPending && Monster.agent.remainingDistance <= Monster.agent.stoppingDistance &&
                SuperManager.Instance.isHost && Monster.moveStart) 
            {
                if (!Monster.agent.hasPath || Monster.agent.velocity.sqrMagnitude == 0f)
                {
                    // Idle 상태로 전환
                    Monster.SendChangeState(MonsterState.MonsterStatusIdle);
                    Monster.moveStart = false;
                }
            }
        }
    }
}