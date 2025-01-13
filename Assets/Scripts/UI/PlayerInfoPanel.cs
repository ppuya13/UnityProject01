using Monster;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace UI
{
    public class InfoPanel: SerializedMonoBehaviour
    {
        public PlayerController connectedCharacter;
        public MonsterController connectedMonster;
        public GameObject fill;
        public TextMeshProUGUI nameText;

        public void ConnectCharacter(PlayerController character)
        {
            connectedCharacter = character;
            character.connectedPanel = this;
            nameText.text = character.nickname;
            SetFill(character.CurrentHp);
        }
        
        public void ConnectMonster(MonsterController character)
        {
            connectedMonster = character;
            character.connectedPanel = this;
            nameText.text = "Monster";
            SetFill(character.CurrentHp);
        }

        public void SetFill(float currentHp)
        {
            float fillRatio = default;
            if(connectedCharacter) fillRatio = connectedCharacter.maxHp > 0 ? currentHp / connectedCharacter.maxHp : 0;
            if(connectedMonster) fillRatio = connectedMonster.maxHp > 0 ? currentHp / connectedMonster.maxHp : 0;

            // Fill의 스케일을 업데이트
            Vector3 scale = fill.transform.localScale;
            scale.x = Mathf.Clamp(fillRatio, 0, 1);
            fill.transform.localScale = scale;
        }
    }
}