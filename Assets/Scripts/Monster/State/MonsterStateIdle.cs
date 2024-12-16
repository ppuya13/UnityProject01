using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateIdle: State
    {
        public override void EnterState()
        {
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
        }
    }
}