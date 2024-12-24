using Sirenix.OdinInspector;
using UnityEngine;

namespace Character
{
    public class AttackBehaviour : SerializedStateMachineBehaviour
    {
        //플레이어의 공격 애니메이션이 호출됐을 때 애니메이터에서 호출됨.
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            MyPlayer player = animator.GetComponent<MyPlayer>();
            if (!player) return;
            player.SendAttackState(stateInfo.fullPathHash, layerIndex);
        }
    }
}