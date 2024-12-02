using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateIdle: State
    {
        public override void EnterState()
        {
            if(SuperManager.Instance.IsHost)
                Monster.SendTarget(Monster.SelectRandomTarget());
            Monster.ReadyToAction += ChoicePattern;
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
        }

        public void ChoicePattern()
        {
            Debug.Log("패턴고를래");
            Monster.SendChangeState(MonsterState.MonsterStatusMove);
        }
    }
}