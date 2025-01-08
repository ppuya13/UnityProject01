
using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using Game;
using Monster;
using Sound;
using UnityEngine;

public class OtherPlayer: PlayerController
{
    public Vector3 CurrentPosition { get; set; }
    public Vector3 TargetPosition { get; set; }
    public Vector3 Rotation { get; set; }
    public float InterpolationFactor { get; set; } = 0.1f; // 보간 속도
    
    private Vector3 lastPosition; // 이전 프레임의 위치
    private Quaternion lastDodgeDirection = Quaternion.identity; // 마지막 회피 방향 저장

    protected override void Awake()
    {
        base.Awake();
        lastPosition = transform.position;
    }

    protected override void Update()
    {
        base.Update();
        InterpolatePosition();
        InterpolateRotation();
    }

    private void LateUpdate()
    {
        // 회피 중일 때는 회피 방향을 유지
        if (isDodge && lastDodgeDirection != Quaternion.identity)
        {
            transform.rotation = lastDodgeDirection;
        }
        else
        {
            // 회피가 끝난 후에는 서버로 받은 회전 값을 보간하여 적용
            Quaternion targetRotation = Quaternion.Euler(Rotation);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    protected override void TakeDamage(AttackConfig config, Transform monsterTransform)
    {
    }


    public override void HitCheck()
    {
        // if (!currentAttack)
        // {
        //     Debug.LogError("currentAttack이 null임");
        //     return;
        // }
        //
        // //데미지를 설정
        // float distance = currentAttack.distance;
        // Vector3 attackPositionOffset = currentAttack.attackPositionOffset;
        //
        // // 공격 위치 계산
        // Vector3 attackPosition = transform.position + transform.forward * distance +
        //                          transform.TransformDirection(attackPositionOffset);
        //
        //
        // // 히트 판정 수행
        // Collider[] hitColliders = Array.Empty<Collider>();
        //
        // switch (currentAttack.ColliderConfig.ColliderType)
        // {
        //     case ColliderType.Sphere:
        //         if (currentAttack.ColliderConfig is SphereColliderConfig sphereConfig)
        //         {
        //             hitColliders = Physics.OverlapSphere(attackPosition, sphereConfig.Radius, targetLayer);
        //         }
        //         else
        //         {
        //             Debug.LogWarning("SphereCollider 설정이 올바르지 않습니다.");
        //         }
        //
        //         break;
        //     case ColliderType.Box:
        //         if (currentAttack.ColliderConfig is BoxColliderConfig boxConfig)
        //         {
        //             Vector3 boxCenterWorld = attackPosition + transform.TransformDirection(boxConfig.Center);
        //             Quaternion worldRotation = transform.rotation * boxConfig.Rotation;
        //             hitColliders = Physics.OverlapBox(boxCenterWorld, boxConfig.Size * 0.5f, worldRotation,
        //                 targetLayer);
        //         }
        //         else
        //         {
        //             Debug.LogWarning("BoxCollider 설정이 올바르지 않습니다.");
        //         }
        //
        //         break;
        //
        //     case ColliderType.Capsule:
        //         if (currentAttack.ColliderConfig is CapsuleColliderConfig capsuleConfig)
        //         {
        //             Vector3 point1 = attackPosition +
        //                              capsuleConfig.Direction * (capsuleConfig.Height / 2 - capsuleConfig.Radius);
        //             Vector3 point2 = attackPosition -
        //                              capsuleConfig.Direction * (capsuleConfig.Height / 2 - capsuleConfig.Radius);
        //             hitColliders = Physics.OverlapCapsule(point1, point2, capsuleConfig.Radius, targetLayer);
        //         }
        //         else
        //         {
        //             Debug.LogWarning("CapsuleCollider 설정이 올바르지 않습니다.");
        //         }
        //
        //         break;
        //
        //     default:
        //         Debug.LogWarning($"Unknown ColliderType: {currentAttack.ColliderConfig.ColliderType}.");
        //         break;
        // }
        //
        // foreach (var hitCollider in hitColliders)
        // {
        //     MonsterController monster = hitCollider.GetComponent<MonsterController>();
        //     if (monster)
        //     {
        //         //OtherPlayer의 공격은 데미지를 적용하지 않고 이펙트만 적용한다.
        //         monster.OtherPlayerAttackValidation(currentAttack, transform);
        //     }
        // }
    }

    //OtherPlayer의 TakeDamage는 로컬이 아니라 서버에서 받아온 정보로 처리한다.
    public void OtherPlayerTakeDamage(PlayerInput msg)
    {
        Velocity += TcpProtobufClient.Instance.ConvertToVector3(msg.Knockback);
        CurrentHp = msg.CurrentHp;
        IsDie = msg.IsDie;
        OtherPlayerSetAnimatorParameters(msg.Params.Lr, msg.Params.Fb, msg.Params.IsBound, msg.Params.MotionIndex);
        
        if (StunCoroutine != null)
        {
            StopCoroutine(StunCoroutine);
            StunCoroutine = null;
        }
        StunCoroutine = StartCoroutine(HandleStun(msg.StunDuration));
    }

    private void OtherPlayerSetAnimatorParameters(float lr, float fb, bool isBound, float motionIndex)
    {
        Animator.SetBool(FallDown, isBound);
        
        if (!isBound)
        {
            if (IsDie) //날아가지 않는 공격에 맞아 죽었다면 Die모션 재생
            {
                Animator.SetTrigger(Die);
                return;
            }
        }
        
        Animator.SetFloat(LR, lr);
        Animator.SetFloat(FB, fb);
        Animator.SetFloat(MotionIndex, motionIndex);
        Animator.SetTrigger(Damage);
    }

    public void OtherPlayerDodge(float moveX, float moveY, bool isBack, float dodgeVertical)
    {
        // 수신된 회피 방향 계산
        Vector3 dodgeDirection = transform.TransformDirection(new Vector3(moveX, 0, moveY).normalized);
    
        // 회피 방향을 Quaternion으로 저장
        lastDodgeDirection = isBack 
            ? Quaternion.LookRotation(-dodgeDirection, Vector3.up) 
            : Quaternion.LookRotation(dodgeDirection, Vector3.up);
        
        // 애니메이터 파라미터 업데이트
        Animator.SetFloat(DodgeVertical, dodgeVertical);
        Animator.SetTrigger(Dodge);
        
        // 이동 처리 (회피 애니메이션 길이에 따라)
        DodgeCoroutine = StartCoroutine(DodgeAnimationMovement(dodgeDirection));
    }
    
    private IEnumerator DodgeAnimationMovement(Vector3 direction)
    {
        yield return null;
        float distance = DodgeSpeed * dodgeAnimLength;
        float elapsed = 0f;
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + direction * distance;

        while (elapsed < dodgeAnimLength)
        {
            // 이동 보간
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsed / dodgeAnimLength);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 이동 완료 후 위치 고정
        transform.position = targetPosition;
    }

    public override void RotateStop()
    {
        // 이미 RotateStop이 실행 중이면 기존 Coroutine 중지
        if (RotateStopCoroutine != null)
        {
            StopCoroutine(RotateStopCoroutine);
        }

        // 회전 복귀 Coroutine 시작
        RotateStopCoroutine = StartCoroutine(RotateBackToOriginal());
    }

    public override void AttackStart(PlayerAttackName attackName)
    {
    }

    public override void AttackEnd()
    {
        CurrentHitSound = SoundType.Unknown;
    }

    private IEnumerator RotateBackToOriginal()
    {
        Quaternion initialRotation = lastDodgeDirection; // 현재 lastDodgeDirection
        Quaternion targetRotation = Quaternion.Euler(Rotation); // 원래 상태 (기본 회전값)

        float duration = 0.2f; // 0.2초 동안 회전
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            // dodgeRotation 값을 점진적으로 변경
            lastDodgeDirection = Quaternion.Slerp(initialRotation, targetRotation, elapsedTime / duration);
            transform.rotation = lastDodgeDirection; // 적용된 회전을 유지
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        // 최종적으로 정확히 targetRotation으로 설정
        DodgeRotation = targetRotation;
        transform.rotation = DodgeRotation;

        // Coroutine 종료 처리
        RotateStopCoroutine = null;
    }

    public void UpdatePosition(Vector3 newPosition, float horizontal, float vertical, bool isRunning)
    {
        TargetPosition = newPosition;
        UpdateAnimationState(horizontal, vertical, isRunning);
    }
    
    public void UpdateVelocity(Vector3 newVelocity)
    {
        Velocity = newVelocity;
    }

    public void UpdateRotation(Vector3 newRotation)
    {
        Rotation = newRotation;
        transform.rotation = Quaternion.Euler(Rotation);
    }

    public void InterpolatePosition()
    {
        // 위치 보간
        CurrentPosition = Vector3.Lerp(CurrentPosition, TargetPosition, InterpolationFactor);

        // 특정 거리 이하일 경우 강제로 동기화
        if (Vector3.Distance(CurrentPosition, TargetPosition) < 0.01f)
        {
            CurrentPosition = TargetPosition;
        }

        // 실제 위치에 반영
        transform.position = CurrentPosition;
    }
    
    private void UpdateAnimationState(float horizontal, float vertical, bool isRunning)
    {
        if (Animator)
        {
            Animator.SetFloat(Horizontal, horizontal);
            Animator.SetFloat(Vertical, vertical);
        }
    }
    
    public void InterpolateRotation()
    {
        Quaternion currentRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(Rotation);
        transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, InterpolationFactor);
    }

    public void PlayAttackAnimation(int hash, int layer)
    {
        Animator.Play(hash, layer);
    }

    public void SetCurrentAttack(PlayerAttackName attackName)
    {
        if (attackName == PlayerAttackName.PlayerAttackUnknown)
        {
            currentAttack = null;
            return;
        }
        
        currentAttack = AttackDict[attackName];
    }
    
    public override void AttackMoveStart()
    {
    }

    public override void AttackMoveStop()
    {
    }
    
}
