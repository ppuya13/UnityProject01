using Sirenix.OdinInspector;
using UnityEngine;

namespace Character
{
    public class AnyStateBehaviour : SerializedStateMachineBehaviour
    {
        private static readonly int DodgeVertical = Animator.StringToHash("DodgeVertical");

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            MyPlayer player = animator.GetComponent<MyPlayer>();
            if (!player) return;
            player.AttackEnd();

            if (stateInfo.IsName("Dodge"))
            {
                // 블렌드 트리 입력값을 기반으로 현재 클립 매핑
                string currentClipName = GetDodgeClipName(animator);
                float clipLength = GetOriginalAnimationClipLength(animator, currentClipName);
                // Debug.Log($"Dodge Animation Length: {clipLength} seconds (Original Clip: {currentClipName})");
                player.dodgeAnimLength = clipLength * 0.8f;
            }
            else
            {
                if(player.dodgeInvincible) Debug.LogWarning("AnyStateBehaviour: OnStateEnter가 실행될 때 Invincible이 true임. 회피무적이 제대로 해제되지 않았음.");
                // player.DodgeFlagOff(); //DodgeStop에 포함됨
                player.DodgeStop();
            }
        }

        private string GetDodgeClipName(Animator animator)
        {
            // 블렌드 트리 입력값 가져오기
            float vertical = animator.GetFloat(DodgeVertical);

            // 입력값을 기반으로 클립 이름 매핑
            if (vertical < -0.1f) // 뒤로 이동 (Dodge_Back)
            {
                return "Dodge_Back";
            }
            else // 앞으로 이동 또는 기타 방향 (Dodge_Front)
            {
                return "Dodge_Front";
            }
        }

        private float GetOriginalAnimationClipLength(Animator animator, string clipName)
        {
            if (!animator.runtimeAnimatorController) return 0f;

            // Animator 컨트롤러의 모든 애니메이션 클립 탐색
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == clipName)
                {
                    return clip.length; // 원래 애니메이션 클립 길이 반환
                }
            }

            Debug.LogWarning($"애니메이션 클립 '{clipName}'을(를) 찾을 수 없습니다.");
            return 0f;
        }
    }
}