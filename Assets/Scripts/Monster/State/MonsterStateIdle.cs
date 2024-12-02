using UnityEngine;

namespace Monster
{
    public class MonsterStateIdle: State
    {
        public override void EnterState()
        {
            Monster.agent.isStopped = true;
            // Debug.Log($"{Monster.CurrentState} EnterState 실행");
        }

        public override void ExitState()
        {
            // Debug.Log($"{Monster.CurrentState} ExitState 실행");
        }

        public override void UpdateState()
        {
            // Debug.Log($"{Monster.CurrentState} UpdateState 실행");
        }
    }
}