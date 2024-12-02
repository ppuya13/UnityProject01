using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateMove: State
    {
        public override void EnterState()
        {
            Monster.agent.speed = 1f;
            Monster.SendDestination(Monster.FindMoveDestination());
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
            //이동 애니메이션 파라미터 업데이트
            Monster.UpdateMovementParameters();
            
            if (Monster.currentTarget)
            {
                Vector3 direction = Monster.currentTarget.transform.position - Monster.transform.position;
                direction.y = 0; // 수평 방향만 고려

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    Monster.transform.rotation = targetRotation;
                }
            }
            
            if (!Monster.agent.pathPending && Monster.agent.remainingDistance <= Monster.agent.stoppingDistance &&
                SuperManager.Instance.IsHost) 
            {
                if (!Monster.agent.hasPath || Monster.agent.velocity.sqrMagnitude == 0f)
                {
                    // Idle 상태로 전환
                    Monster.ChangeState(MonsterState.MonsterStatusIdle);
                }
            }
        }
    }
}