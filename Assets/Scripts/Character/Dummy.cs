
using Game;
using Monster;
using UnityEngine;

public class Dummy: PlayerController
{
    private Vector3 initialPosition;
    private const float FixThreshold = 30;
    protected override void Awake()
    {
        base.Awake();
        initialPosition = transform.position;
    }

    protected override void Update()
    {
        base.Update();
        FixPosition();
    }

    protected override void TakeDamage(AttackConfig config, Transform monsterTransform)
    {
        
        // Debug.Log($"공격 히트(damage: {config.damageAmount})");
        //일단 넉백만 적용하도록 로직을 짤 것.
        Vector3 attackDirection = (transform.position - monsterTransform.position).normalized;
        
        Vector3 knockback = Vector3.zero;

        switch (config.knockBackType)
        {
            case KnockBackType.KnockbackNone:
                Debug.Log($"넉백 없음");
                // 넉백 없음
                break;
            case KnockBackType.KnockbackUp:
                // 위쪽으로 넉백
                knockback = Vector3.up * config.knockBackPower;
                // Debug.Log($"넉백 방향: Up, 넉백값: {knockback}");
                break;
            case KnockBackType.KnockbackPush:
                // 몬스터 방향으로 넉백 (밀려남)
                knockback = attackDirection.normalized * config.knockBackPower;
                // Debug.Log($"넉백 방향: Push, 넉백값: {knockback}");
                break;
            case KnockBackType.KnockbackPull:
                // 몬스터 반대 방향으로 넉백 (당겨옴)
                knockback = (-attackDirection).normalized * config.knockBackPower;
                // Debug.Log($"넉백 방향: Pull, 넉백값: {knockback}");
                break;
            case KnockBackType.KnockbackBound:
                // 몬스터 방향과 약간의 위쪽 방향으로 넉백 (날아감)
                knockback = (attackDirection.normalized + Vector3.up).normalized * config.knockBackPower;
                // Debug.Log($"넉백 방향: Bound, 넉백값: {knockback}");
                break;
            case KnockBackType.KnockbackDown:
                //이동하지 않음, 그냥 넘어지는 모션만 재생
                knockback = Vector3.zero;
                break;
            default:
                Debug.LogWarning($"알 수 없는 KnockBackType: {config.knockBackType}");
                break;
        }
        
        // 넉백 벡터를 현재 속도에 추가
        Velocity += knockback;
        
        // 체력 감소
        currentHp -= config.damageAmount;
        if (currentHp <= 0) IsDie = true; // 사망 여부 플래그 처리
        
        // 애니메이터 파라미터 설정
        (float lr, float fb, bool isBound, bool isDown, float motionIndex) = SetAnimatorParameters(attackDirection, config);

        
        // stunDuration 동안 대기 후 StunEnd 트리거 활성화
        if (StunCoroutine != null)
        {
            StopCoroutine(StunCoroutine);
            StunCoroutine = null;
        }
        StunCoroutine = StartCoroutine(HandleStun(config.stunDuration));
        
        //더미는 통신 메시지를 보내지 않는다.
        // TcpProtobufClient.Instance.SendPlayerTakeDamage(knockback, config.stunDuration, currentHp, IsDie, lr, fb, isBound, motionIndex);
    }

    public override void HitCheck()
    {
        throw new System.NotImplementedException();
    }

    public override void RotateStop()
    {
        throw new System.NotImplementedException();
    }

    //현재 위치가 initialPosition에서 threshold만큼 떨어지면 포지션을 initialPosition으로 변경
    private void FixPosition()
    {
        float distance = Vector3.Distance(transform.position, initialPosition);

        // 거리 비교
        if (distance > FixThreshold)
        {
            // 위치 재설정
            transform.position = initialPosition;

            // Velocity 초기화
            Velocity = Vector3.zero;
        }
    }
}
