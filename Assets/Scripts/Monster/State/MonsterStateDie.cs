using Game;
using UnityEngine;

namespace Monster
{
    public class MonsterStateDie: State
    {
        public override void EnterState()
        {
            Debug.Log("statusDie 진입");
        }

        public override void ExitState()
        {
        }

        public override void UpdateState()
        {
        }
    }
}