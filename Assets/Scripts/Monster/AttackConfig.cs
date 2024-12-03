using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Monster
{
    public struct AttackConfig
    {
        // 공용 옵션
        public float DamageAmount;
        public float Distance; //몬스터의 앞쪽 얼마나 앞에 판정을 생성할지
        public Vector3 AttackPositionOffset; //Distance를 기준으로 추가적으로 세부적인 위치를 x, y, z 로 설정
        
        // 콜라이더 설정
        public IColliderConfig ColliderConfig;
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
    }

    public class CapsuleColliderConfig : IColliderConfig
    {
        public ColliderType ColliderType => ColliderType.Capsule;
        public float Height;
        public float Radius;
        public Vector3 Direction; // 보통 (0,1,0) Y축 방향
    }

    public enum ColliderType
    {
        Unknown,
        Sphere,
        Box,
        Capsule,
    }
}