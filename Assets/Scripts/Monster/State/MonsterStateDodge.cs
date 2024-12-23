using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateDodge: State
    {
        private float stateEnterTime;
        private const float TimeThreshold = 1.5f; //닷지 스테이트가 이 시간동안 유지되면 idle 상태로 전환
        
        public override void EnterState()
        {
            // Debug.Log("닷지실행");
            stateEnterTime = Time.time; // 상태 진입 시간을 기록
            Monster.agent.ResetPath();
            Monster.animator.SetTrigger(Monster.Dodge);
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
            if (Time.time - stateEnterTime > TimeThreshold && SuperManager.Instance.isHost)
            {
                // Debug.Log("1.5초 대기로 idle상태로 전환");
                // Idle 상태로 전환
                Monster.SendChangeState(MonsterState.MonsterStatusIdle);
            }
        }
    }
}