using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ChatInput : SerializedMonoBehaviour
    {
        public TMP_InputField inputField;
        public Button sendBtn;

        private void Awake()
        {
            UIManager.Instance.ChattingActivated += ChattingActivated;
        }

        private void ChattingActivated(bool b)
        {
            // Debug.Log($"채팅액티베이티드: {b}");
            inputField.interactable = b;
            sendBtn.interactable = b;
        }

        private void OnDestroy()
        {
            UIManager.Instance.ChattingActivated -= ChattingActivated;
        }
    }
}