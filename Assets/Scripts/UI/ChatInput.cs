using System;
using Sirenix.OdinInspector;
using TMPro;
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
            inputField.interactable = b;
            sendBtn.interactable = b;
        }

        private void OnDestroy()
        {
            UIManager.Instance.ChattingActivated -= ChattingActivated;
        }
    }
}