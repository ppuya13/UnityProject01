using Sirenix.OdinInspector;
using UnityEngine;

namespace Character
{
    public class AnyStateBehaviour: SerializedStateMachineBehaviour
    {
        //anystate에서 넘어가는 모든 애니메이션에 달 것. 공격 상태를 해제하기 위함.
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            MyPlayer player = animator.GetComponent<MyPlayer>();
            if (!player) return;
            player.AttackEnd();
        }
    }
}