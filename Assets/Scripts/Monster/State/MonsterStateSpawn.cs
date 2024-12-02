using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateSpawn: State
    {
        public override void EnterState()
        {
            Monster.SendMonsterAnim(Monster.Spawn, ParameterType.ParameterTrigger);
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
            //Idle로의 변경은 Spawn Animation의 AnimationEvent로 한다.
        }
    }
}