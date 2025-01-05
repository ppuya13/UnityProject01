using System.Collections.Generic;
using Game;
using Monster;
using Sirenix.OdinInspector;
using Sound;
using UnityEngine;

namespace Character
{
    [CreateAssetMenu(fileName = "NewAttackConfig", menuName = "Character/Attack Config", order = 1)]
    public class PlayerAttackConfig : SerializedScriptableObject
    {
        [Header("이름")] public PlayerAttackName attackName; //공격의 이름, 같은 공격에 여러번 맞지 않게 하기 위해 사용.
        
        
        [Header("공격 데미지")] public float damageAmount; //공격 데미지
        
        [Header("공격 판정")]
        // 공격 판정
        public float distance; //
        public Vector3 attackPositionOffset; //Distance를 기준으로 추가적으로 세부적인 위치를 x, y, z 로 설정
        public IColliderConfig ColliderConfig; //공격 판정에 사용될 콜라이더의 종류
        
        [Header("이펙트 설정")] public EffectConfig[] EffectConfigs;

        // [Header("사운드 설정")] public AudioClip[][] SoundEffects; // 공격 시 재생할 소리 클립
        public SoundEffectGroup[] SoundEffects;
    }
}