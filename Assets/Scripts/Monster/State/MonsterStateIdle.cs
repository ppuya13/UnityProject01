using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateIdle: State
    {
        private float stateEnterTime;
        private const float TimeThreshold = 2.0f; //idle state가 일정 시간 이상 유지되면 readyToAction invoke
        
        public override void EnterState()
        {
            stateEnterTime = Time.time; // 상태 진입 시간을 기록
            if(SuperManager.Instance.isHost)
            {
                Monster.SendTarget(Monster.SelectRandomTarget());
                Monster.ReadyToAction += Monster.ChoicePattern;
            }
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
            if (Time.time - stateEnterTime > TimeThreshold && SuperManager.Instance.isHost)
            {
                // Debug.Log($"{TimeThreshold}초 대기로 readyToAction Invoke");
                // Idle 상태로 전환
                Monster.ReadyToAction.Invoke();
            }
        }
    }
}