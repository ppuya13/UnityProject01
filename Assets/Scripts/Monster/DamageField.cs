using System;
using Game;
using UnityEngine;
using UnityEngine.Serialization;

namespace Monster
{
    public class DamageField: MonoBehaviour
    {
        //데미지필드의 기본 설정들
        public AttackType attackType;
        public int attackIdx;
        public AttackConfig config;
        public bool settingComplete = false;
        public Projectile Projectile;
        
        private float elapsedTime = 0f; // 투사체 지속 시간 추적

        private void Awake()
        {
        }

        public void SetDamageField(AttackType _attackType, int _attackIdx, AttackConfig _config, Projectile _projectile)
        {
            attackType = _attackType;
            attackIdx = _attackIdx;
            config = _config;
            Projectile = _projectile;
            settingComplete = true;
        }

        private void Update()
        {
            if (!settingComplete) return; //셋팅이 끝났을 때만 효과가 발동된다.
            if (config.RangeType is RangeAttack)
            {
                transform.position += Projectile.Direction.normalized * (Projectile.Speed * Time.deltaTime);
                elapsedTime += Time.deltaTime;

                // 투사체 지속 시간이 초과되면 제거
                if (elapsedTime > Projectile.Duration)
                {
                    Destroy(gameObject);
                }
            } 
            
        }

        private void OnTriggerEnter(Collider other)
        {
            var target = other.GetComponent<PlayerController>();
            if (target)
            {
                //일단 임시로 사운드이펙트는 첫번째껄 쓴다.
                target.AttackValidation(config, attackType, attackIdx, transform, config.SoundEffects[0].Hit);
            }
        }

        private void OnDestroy()
        {
            //투사체가 파괴될 때 이펙트라던가 하는것 추가
        }
    }
}