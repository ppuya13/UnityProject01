using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateMove: State
    {
        private float stateEnterTime;
        private const float TimeThresholdOrigin = 5.0f;
        private float timeThreshold; //이 시간동안 계속 걷고만 있으면 다시 idle상태로 전환
        private int previousCategory; // 이전 프레임에서의 거리 카테고리
        
        public override void EnterState()
        {
            stateEnterTime = Time.time; // 상태 진입 시간을 기록
            Monster.SendDestination(Monster.FindMoveDestination());
            timeThreshold = GetRandomTime();
            previousCategory = GetDistanceCategory(Vector3.Distance(Monster.transform.position, Monster.currentTarget.transform.position));
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
            // 타겟이 없으면 바로 Idle로 전환
            if (!Monster.currentTarget)
            {
                Monster.SendChangeState(MonsterState.MonsterStatusIdle);
                return;
            }

            if (SuperManager.Instance.isHost)
            {
                // 타겟과의 거리 계산
                float currentDistance = Vector3.Distance(Monster.transform.position, Monster.currentTarget.transform.position);
                int currentCategory = GetDistanceCategory(currentDistance);

                // 거리 카테고리가 이전과 달라지면 Idle 상태로 전환
                if (currentCategory != previousCategory)
                {
                    // Debug.Log($"카테고리 변경으로 idle로 전환(이전: {currentCategory}, 이후: {previousCategory})");
                    Monster.SendChangeState(MonsterState.MonsterStatusIdle);
                    return;
                }

                // 이전 카테고리 업데이트
                previousCategory = currentCategory;
            }
            
            // 5초가 경과했는지 확인
            if (Time.time - stateEnterTime > timeThreshold)
            {
                // 이동 중인지 확인
                if (Monster.moveStart)
                {
                    // 몬스터 이동 취소
                    if (Monster.agent.hasPath && Monster.agent.velocity.sqrMagnitude > 0f)
                    {
                        Monster.agent.ResetPath();
                    }
                    Monster.moveStart = false;

                    if (SuperManager.Instance.isHost)
                    {
                        // Idle 상태로 전환
                        Monster.SendChangeState(MonsterState.MonsterStatusIdle);
                    }
                }
                return;
            }
            
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

        private float GetRandomTime()
        {
            //timeThresholdOrigin * 0.8~1.2 사이의 랜덤한 값을 반환
            return Random.Range(TimeThresholdOrigin * 0.8f, TimeThresholdOrigin * 1.2f);
        } 
        
        private int GetDistanceCategory(float distance)
        {
            if (distance < 3.0f)
            {
                return 0; // 근접
            }
            else if (distance < 5.0f)
            {
                return 1; // 근거리
            }
            else if (distance < 10.0f)
            {
                return 2; // 중거리
            }
            else if (distance < 20.0f)
            {
                return 3; // 장거리
            }
            else
            {
                return 4; // 초장거리
            }
        }
        
        private bool IsAgentStopped()
        {
            return !Monster.agent.hasPath || Monster.agent.velocity.sqrMagnitude == 0f;
        }
    }
}