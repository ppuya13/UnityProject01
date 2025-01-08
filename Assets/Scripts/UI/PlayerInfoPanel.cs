using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace UI
{
    public class PlayerInfoPanel: SerializedMonoBehaviour
    {
        public PlayerController connectedCharacter;
        public GameObject fill;
        public TextMeshProUGUI nameText;

        public void ConnectCharacter(PlayerController character)
        {
            connectedCharacter = character;
            character.connectedPanel = this;
            nameText.text = character.nickname;
        }

        public void SetFill(float currentHp)
        {
            // currentHp가 0이 되는 것을 방지
            float fillRatio = connectedCharacter.maxHp > 0 ? currentHp / connectedCharacter.maxHp : 0;

            // Fill의 스케일을 업데이트
            Vector3 scale = fill.transform.localScale;
            scale.x = Mathf.Clamp(fillRatio, 0, 1);
            fill.transform.localScale = scale;
        }
    }
}