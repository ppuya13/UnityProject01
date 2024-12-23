using System.Collections.Generic;
using Game;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Monster
{
    
    [CreateAssetMenu(fileName = "NewAttackConfig", menuName = "Monster/Attack Config", order = 1)]
    public class AttackConfig: SerializedScriptableObject
    {
        [Header("공격 데미지")]
        public float damageAmount; //공격 데미지

        [Header("공격 시 또는 공격 간 이동과 회전")]
        public float moveSpeed; //이동속도
        public float moveTime = 5.0f; //이동 정지는 Animation Event로 코루틴 정지를 통해 실행한다. moveTime은 깜빡하고 Animation Event에서 코루틴 정지 메소드를 할당하지 않았을 경우 무한히 이동하는 것을 방지하기 위해 사용함.
        [ValueDropdown("GetMoveDirectionOptions")]
        [Tooltip("이동 방향을 선택함. Vector3.zero로 설정 시 타겟을 향한다.")]
        public Vector3 moveDirection = Vector3.zero;
        public float rotateSpeed;
        public float rotateTime = 5.0f; //moveTime과 동일
        
        [Header("공격 판정")]
        // 공격 판정
        public float distance; //몬스터의 앞쪽 얼마나 앞에 판정을 생성할지 (positionOffset의 Z값과 동일)
        public Vector3 attackPositionOffset; //Distance를 기준으로 추가적으로 세부적인 위치를 x, y, z 로 설정
        public IColliderConfig ColliderConfig; //공격 판정에 사용될 콜라이더의 종류
        
        [Header("넉백/경직 설정")]
        //넉백 설정
        public KnockBackType knockBackType;
        public float knockBackPower; // 넉백으로 밀려나거나 날아가는 거리
        public float stunDuration; //경직에 걸리는 시간
        public ForceMode knockBackForceMode; // 넉백에 사용할 물리적 힘의 타입

        [Header("이펙트 설정")]
        public EffectConfig[] EffectConfigs;
        
        [Header("사운드 설정")]
        public AudioClip[] soundEffects; // 공격 시 재생할 소리 클립
        
        //이동 방향을 설정하는데 쓸 드롭다운 아이템
        private static IEnumerable<ValueDropdownItem<Vector3>> GetMoveDirectionOptions()
        {
            yield return new ValueDropdownItem<Vector3>("Zero", Vector3.zero);
            yield return new ValueDropdownItem<Vector3>("Up", Vector3.up);
            yield return new ValueDropdownItem<Vector3>("Down", Vector3.down);
            yield return new ValueDropdownItem<Vector3>("Left", Vector3.left);
            yield return new ValueDropdownItem<Vector3>("Right", Vector3.right);
            yield return new ValueDropdownItem<Vector3>("Forward", Vector3.forward);
            yield return new ValueDropdownItem<Vector3>("Back", Vector3.back);
        }
    }

    public struct EffectConfig
    {
        public GameObject ParticleEffect; //이미지 오브젝트
        public Vector3 EffectPosition; //이펙트 생성 위치 (기본: Distance에서 생성됨)
        public Quaternion EffectRotation; //이펙트의 회전값
        public Vector3 EffectScale; // 이펙트의 크기
    }

    public interface IColliderConfig
    {
        ColliderType ColliderType { get; }
    }

    public class SphereColliderConfig : IColliderConfig
    {
        public ColliderType ColliderType => ColliderType.Sphere;
        public float Radius;
    }

    public class BoxColliderConfig : IColliderConfig
    {
        public ColliderType ColliderType => ColliderType.Box;
        public Vector3 Size;
        public Vector3 Center;
        public Quaternion Rotation = Quaternion.identity;
    }

    public class CapsuleColliderConfig : IColliderConfig
    {
        public ColliderType ColliderType => ColliderType.Capsule;
        public float Height;
        public float Radius;
        public Vector3 Direction = Vector3.up; // 보통 (0,1,0) Y축 방향
    }

    public enum ColliderType
    {
        Unknown,
        Sphere,
        Box,
        Capsule,
    }
}