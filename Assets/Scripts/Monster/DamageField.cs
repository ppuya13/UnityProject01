using Game;
using UnityEngine;

namespace Monster
{
    public class DamageField: MonoBehaviour
    {
        //공격의 기본 설정들
        public AttackType AttackType;
        public int AttackIdx;
        public AttackConfig Config;
        
        public void SetDamageField(AttackType attackType, int attackIdx, AttackConfig config)
        {
            AttackType = attackType;
            AttackIdx = attackIdx;
            Config = config;
        }
    }
}