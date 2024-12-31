using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;

namespace UI
{
    public class ChatMessage: SerializedMonoBehaviour
    {
        private Coroutine messageCoroutine;

        private const float ViewTime = 5.0f; //처음 채팅 메시지가 생성된 뒤 기다릴 시간
        private const float HideTime = 2.0f; //메시지가 사라질 때 까지 걸리는 시간
        
        public CanvasGroup canvasGroup;
        private bool isChatting = false;

        private float alpha = 1.0f;
        private float Alpha
        {
            get => alpha;
            set
            {
                alpha = value;
                if (!isChatting)
                {
                    canvasGroup.alpha = value;
                }
            }
        }
        
        private void Awake()
        {
            // 코루틴 시작
            messageCoroutine = StartCoroutine(MessageCoroutine());
            UIManager.Instance.ChattingActivated += ChattingActivated;
        }

        private void ChattingActivated(bool b)
        {
            isChatting = b;
            canvasGroup.alpha = b ? 1.0f : Alpha;
        }

        //채팅 메시지가 처음 등장하면 ViewTime동안 대기하고 hideTime에 걸쳐서 알파값을 0으로 만듦
        private IEnumerator MessageCoroutine()
        {
            // 1. ViewTime 동안 대기
            yield return new WaitForSeconds(ViewTime);

            // 2. HideTime 동안 알파값을 0으로 서서히 감소
            float elapsed = 0f;
            while (elapsed < HideTime)
            {
                elapsed += Time.deltaTime;
                Alpha = Mathf.Lerp(1f, 0f, elapsed / HideTime);
                yield return null;
            }

            // 3. 메시지 완전히 숨김
            Alpha = 0f;
        }
        
        private void OnDestroy()
        {
            UIManager.Instance.ChattingActivated -= ChattingActivated;
        }    
    }
}