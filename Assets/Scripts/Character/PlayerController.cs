using System;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class PlayerController : MonoBehaviour
{
    protected CharacterController CharacterController;
    
    public enum CharacterType
    {
        Unknown,
        MyPlayer,
        OtherPlayer,
        Dummy,
    }

    public CharacterType type;
    protected const float gravity = -9.81f; // 중력 값
    protected float Speed = 5f;  // 이동 속도
    protected float JumpHeight = 2.0f;  // 점프 높이 (추가적으로 점프를 적용할 경우)
    
    protected Vector3 Velocity;

    protected virtual void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
    }

    protected virtual void Update()
    {
        if (type == CharacterType.Dummy) return;
        Gravity();
    }


    private void Gravity()
    {
        // 중력 적용
        if (CharacterController.isGrounded)
        {
            if (Velocity.y < 0)
            {
                Velocity.y = -2f;
            }
        }
        else
        {
            Velocity.y += gravity * Time.deltaTime;  // 중력 적용
        }
    }
}
