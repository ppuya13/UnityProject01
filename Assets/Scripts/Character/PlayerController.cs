using System.Collections;
using System.Collections.Generic;
using Game;
using Monster;
using RootMotion.FinalIK;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class PlayerController : SerializedMonoBehaviour
{
    public enum CharacterType
    {
        Unknown,
        MyPlayer,
        OtherPlayer,
        Dummy,
    }

    public string playerId;
    public string nickname;
    public CharacterType type;

    public float maxHp;
    public float currentHp;

    protected const float gravity = -9.81f; // 중력 값
    protected float CurrentSpeed; // 이동 속도
    protected float WalkSpeed = 2.0f; // 걷기 이동속도
    protected float RunSpeed = 3.0f; // 달리기 이동속도
    protected float SpeedChangeRate = 10f; //이동속도 변경 속도
    protected float JumpHeight = 2.0f; // 점프 높이 (추가적으로 점프를 적용할 경우)
    protected Vector3 Velocity;

    protected CharacterController CharacterController;
    protected Animator Animator;
    public LookAtIK lookAtIK;
    public bool disableKeyboard = false; //채팅할때 키보드 입력으로 움직이지 않도록 함.
    

    private AttackType hitAttack;
    private int hitIndex;
    private float hitInterval; //같은 속성의 공격을 더 이상 받지 않는 쿨타임
    private const float HitThreshold = 0.5f; //쿨타임이 임계점에 도달하면 같은 공격을 받을 수 있음
    private Dictionary<(AttackType, int), bool> hitDict = new(); //이미 맞은 공격들
    protected bool IsStun = false;
    protected bool IsRun = false;
    protected bool IsDie = false;
    protected Coroutine StunCoroutine;
    
    [HideInInspector]public readonly int Stun = Animator.StringToHash("Stun");
    [HideInInspector]public readonly int Horizontal = Animator.StringToHash("Horizontal");
    [HideInInspector]public readonly int Vertical = Animator.StringToHash("Vertical");
    [HideInInspector]public readonly int LR = Animator.StringToHash("LR");
    [HideInInspector]public readonly int FB = Animator.StringToHash("FB");
    [HideInInspector]public readonly int MotionIndex = Animator.StringToHash("MotionIndex");
    [HideInInspector]public readonly int FallDown = Animator.StringToHash("FallDown");
    [HideInInspector]public readonly int Damage = Animator.StringToHash("Damage"); //피격당했을때
    [HideInInspector]public readonly int StunEnd = Animator.StringToHash("StunEnd");
    [HideInInspector]public readonly int Die = Animator.StringToHash("Die");
    

    protected virtual void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
        Animator = GetComponent<Animator>();
        lookAtIK = GetComponent<LookAtIK>();
        InitializeCharacter();
    }

    protected virtual void Update()
    {
        // if (type == CharacterType.Dummy) return;
        Gravity();
        
        // Velocity를 기반으로 플레이어 이동
        CharacterController.Move(Velocity * Time.deltaTime);
        Velocity = Vector3.Lerp(Velocity, Vector3.zero, Time.deltaTime * 5f);
    }

    private void InitializeCharacter()
    {
        maxHp = 100;
        currentHp = maxHp;
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
            Velocity.y += gravity * Time.deltaTime; // 중력 적용
        }
    }
    
    //닿은 공격이 유효한지 체크
    public void HitCheck(AttackConfig config, AttackType attackType, int attackIdx, Transform monsterTransform)
    {
        if (attackIdx < 0) //0미만이면 다단히트라서 조건 계산 할 필요 없음 
        {
            TakeDamage(config, monsterTransform);
        }
        else if (attackIdx > 0)
        {
            if (hitDict.TryGetValue((attackType, attackIdx), out bool value))
            {
                //value값은 의미없고, 일단 true면 같은 공격에 맞았다는 뜻
                // Debug.Log($"이미 맞은 공격임(AttackType: {attackType}, index: {attackIdx})");
                return;
            }

            StartCoroutine(HitIntervalTimer(attackType, attackIdx));
            TakeDamage(config, monsterTransform);
        }
        else //0이면 사실 불릴 일이 없음
        {
            Debug.LogError("0인데 불렸음");
            return;
        }
    }

    IEnumerator HitIntervalTimer(AttackType attackType, int attackIdx)
    {
        hitDict.Add((attackType, attackIdx), true);
        yield return new WaitForSeconds(HitThreshold);
        hitDict.Remove((attackType, attackIdx));
    }

    //실제 데미지를 적용하고, 다른 효과들을 적용하는 메소드
    protected abstract void TakeDamage(AttackConfig config, Transform monsterTransform);

    //피격 애니메이션 재생
    protected (float lr, float fb, bool isBound, float motionIndex) SetAnimatorParameters(Vector3 attackDirection,
        AttackConfig config)
    {
        float lr = 0f;
        float fb = 0f;

        Vector3 localAttackDir = transform.InverseTransformDirection(attackDirection);

        bool isBound = config.knockBackType == KnockBackType.KnockbackBound;
        Animator.SetBool(FallDown, isBound);
        
        if (isBound)
        {
            // isBound가 true일 때: 앞과 뒤만 고려하여 FB 설정
            if (localAttackDir.z > 0.5f)
            {
                fb = 1f; // 앞에서 공격
            }
            else
            {
                fb = -1f; // 뒤에서 공격
            }
            // 좌우는 고려하지 않음
            lr = 0f;
        }
        else
        {
            if (IsDie) //넘어지는 공격이 아니고 사망했을 시 사망 애니메이션을 재생
            {
                Animator.SetTrigger(Die);
            }
            else
            {
                if (localAttackDir.z > 0.5f)
                {
                    fb = -1f; // 앞에서 공격
                }
                else if (localAttackDir.x < -0.5f)
                {
                    fb = 1f; // 뒤에서 공격
                }
                else
                {
                    fb = 0f; // 좌우 중립
                }

                if (localAttackDir.x > 0.5f)
                {
                    lr = -1f; // 오른쪽에서 공격
                }
                else if (localAttackDir.x < -0.5f)
                {
                    lr = 1f; // 왼쪽에서 공격
                }
                else
                {
                    lr = 0f; // 좌우 중립
                }
            }
        }
        
        Animator.SetFloat(LR, lr);
        Animator.SetFloat(FB, fb);

        float motionIndex = Random.value;
        Animator.SetFloat(MotionIndex, motionIndex);
        
        Animator.SetTrigger(Damage);
        
        return (lr, fb, isBound, motionIndex);
    }

    protected IEnumerator HandleStun(float stunDuration)
    {
        IsStun = true;
        yield return new WaitForSeconds(stunDuration);
        IsStun = false;
        if (!IsDie) Animator.SetTrigger(StunEnd); //죽지 않았을 경우에만 스턴이 풀림
    }

}