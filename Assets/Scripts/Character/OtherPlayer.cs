
using Game;
using Monster;
using UnityEngine;

public class OtherPlayer: PlayerController
{
    public Vector3 CurrentPosition { get; set; }
    public Vector3 TargetPosition { get; set; }
    public Vector3 Rotation { get; set; }
    public float InterpolationFactor { get; set; } = 0.1f; // 보간 속도
    
    private Vector3 lastPosition; // 이전 프레임의 위치

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

    protected override void TakeDamage(AttackConfig config, Transform monsterTransform)
    {
        // Debug.Log($"공격 히트(damage: {config.damageAmount})");
        // //일단 넉백만 적용하도록 로직을 짤 것.
        // Vector3 attackDirection = (transform.position - monsterTransform.position).normalized;
        //
        // Vector3 knockback = Vector3.zero;
        //
        // switch (config.knockBackType)
        // {
        //     case KnockBackType.KnockbackNone:
        //         Debug.Log($"넉백 없음");
        //         // 넉백 없음
        //         break;
        //     case KnockBackType.KnockbackUp:
        //         // 위쪽으로 넉백
        //         knockback = Vector3.up * config.knockBackPower;
        //         // Debug.Log($"넉백 방향: Up, 넉백값: {knockback}");
        //         break;
        //     case KnockBackType.KnockbackPush:
        //         // 몬스터 방향으로 넉백 (밀려남)
        //         knockback = attackDirection.normalized * config.knockBackPower;
        //         // Debug.Log($"넉백 방향: Push, 넉백값: {knockback}");
        //         break;
        //     case KnockBackType.KnockbackPull:
        //         // 몬스터 반대 방향으로 넉백 (당겨옴)
        //         knockback = (-attackDirection).normalized * config.knockBackPower;
        //         // Debug.Log($"넉백 방향: Pull, 넉백값: {knockback}");
        //         break;
        //     case KnockBackType.KnockbackBound:
        //         // 몬스터 방향과 약간의 위쪽 방향으로 넉백 (날아감)
        //         knockback = (attackDirection.normalized + Vector3.up).normalized * config.knockBackPower;
        //         // Debug.Log($"넉백 방향: Bound, 넉백값: {knockback}");
        //         break;
        //     case KnockBackType.KnockbackDown:
        //         //이동하지 않음, 그냥 넘어지는 모션만 재생
        //         knockback = Vector3.zero;
        //         break;
        //     default:
        //         Debug.LogWarning($"알 수 없는 KnockBackType: {config.knockBackType}");
        //         break;
        // }
        //
        // // 넉백 벡터를 현재 속도에 추가
        // Velocity += knockback;
        //
        // // 체력 감소
        // currentHp -= config.damageAmount;
        // if (currentHp <= 0) IsDie = true; // 사망 여부 플래그 처리
        //
        // // 애니메이터 파라미터 설정
        // (float lr, float fb, bool isBound, float motionIndex) = SetAnimatorParameters(attackDirection, config);
        //
        //
        // // stunDuration 동안 대기 후 StunEnd 트리거 활성화
        // if (StunCoroutine != null)
        // {
        //     StopCoroutine(StunCoroutine);
        //     StunCoroutine = null;
        // }
        // StunCoroutine = StartCoroutine(HandleStun(config.stunDuration));
        //
        // TcpProtobufClient.Instance.SendPlayerTakeDamage(knockback, config.stunDuration, currentHp, IsDie, lr, fb, isBound, motionIndex);
    }
    
    //OtherPlayer의 TakeDamage는 로컬이 아니라 서버에서 받아온 정보로 처리한다.
    public void OtherPlayerTakeDamage(PlayerInput msg)
    {
        Velocity += TcpProtobufClient.Instance.ConvertToVector3(msg.Knockback);
        currentHp = msg.CurrentHp;
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
    
}
