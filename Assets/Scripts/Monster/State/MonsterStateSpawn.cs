using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateSpawn: State
    {
        public override void EnterState()
        {
            Debug.Log($"{Monster.CurrentState} EnterState 실행");
        }

        public override void ExitState()
        {
            Debug.Log($"{Monster.CurrentState} ExitState 실행");
        }

        public override void UpdateState()
        {
            Debug.Log($"{Monster.CurrentState} UpdateState 실행");
            // Monster.CurrentState = MonsterState.MonsterStatusIdle;
            Monster.SendChangeState(MonsterState.MonsterStatusIdle);
        }
    }
}