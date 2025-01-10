using System.Collections;
using System.Collections.Generic;
using Character;
using Game;
using Monster;
using RootMotion.FinalIK;
using Sirenix.OdinInspector;
using Sound;
using UI;
using UnityEngine;
using Random = UnityEngine.Random;

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

    private float currentHp;

    protected float CurrentHp
    {
        get => currentHp;
        set
        {
            if (Mathf.Approximately(currentHp, value)) return;
            currentHp = value;
            SetFill(value);
        }
    }

    public float yaw = 0f;

    protected const float gravity = -9.81f; // 중력 값
    protected float CurrentSpeed; // 이동 속도
    protected float WalkSpeed = 2.0f; // 걷기 이동속도
    protected float RunSpeed = 3.5f; // 달리기 이동속도
    protected float SpeedChangeRate = 10f; //이동속도 변경 속도
    protected float AttackMoveSpeed = 2.5f; //공격 중 전진 속도
    protected float DodgeSpeed = 8.5f; //회피 중 날아가는 속도
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
    protected bool IsStun = false; //공격의 스턴시간 (시간은 공격에서 설정되며 해당 시간동안 입력을 받지 않음)
    protected bool IsDown = false; //다운상태 (시간은 따로 설정되지 않으며 일어나는 애니메이션이 끝날 때 false됨)
    protected bool IsRun = false;
    protected bool IsDie = false;
    public bool isDodge = false; //회피 애니메이션 실행 중을 의미하는 상태
    public bool dodgeInvincible; //회피 애니메이션 안의 무적 상태를 의미함
    protected Coroutine StunCoroutine;
    protected Coroutine DodgeCoroutine;
    public float dodgeAnimLength = 0f;

    //회피 관련 변수
    protected Coroutine RotateStopCoroutine; //회피 중 원래 보던 방향으로 다시 회전하기 위한 코루틴
    protected Quaternion DodgeRotation;
    
    public PlayerAttackConfig[] attackConfigs;
    public Dictionary<PlayerAttackName, PlayerAttackConfig> AttackDict = new();
    public LayerMask targetLayer;
    public PlayerAttackConfig currentAttack;
    
    private CharacterSounds characterSounds = new();
    protected SoundType CurrentHitSound = SoundType.Unknown;
    public PlayerInfoPanel connectedPanel; //이 캐릭터와 연결된 패널(체력표시를 위함)

    [HideInInspector] public readonly int Stun = Animator.StringToHash("Stun");
    [HideInInspector] public readonly int Horizontal = Animator.StringToHash("Horizontal");
    [HideInInspector] public readonly int Vertical = Animator.StringToHash("Vertical");
    [HideInInspector] public readonly int Dodge = Animator.StringToHash("Dodge");
    [HideInInspector] public readonly int DodgeHorizontal = Animator.StringToHash("DodgeHorizontal");
    [HideInInspector] public readonly int DodgeVertical = Animator.StringToHash("DodgeVertical");
    [HideInInspector] public readonly int LR = Animator.StringToHash("LR");
    [HideInInspector] public readonly int FB = Animator.StringToHash("FB");
    [HideInInspector] public readonly int MotionIndex = Animator.StringToHash("MotionIndex");
    [HideInInspector] public readonly int FallDown = Animator.StringToHash("FallDown");
    [HideInInspector] public readonly int Down = Animator.StringToHash("Down");
    [HideInInspector] public readonly int Damage = Animator.StringToHash("Damage"); //피격당했을때
    [HideInInspector] public readonly int StunEnd = Animator.StringToHash("StunEnd");
    [HideInInspector] public readonly int Die = Animator.StringToHash("Die");

    protected virtual void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
        Animator = GetComponent<Animator>();
        lookAtIK = GetComponent<LookAtIK>();
        InitializeCharacter();
        characterSounds.InitializeClips();
    }

    protected virtual void Update()
    {
        // if (type == CharacterType.Dummy) return;
        Gravity();

        // Velocity를 기반으로 플레이어 이동
        CharacterController.Move(Velocity * Time.deltaTime);
        Velocity = Vector3.Lerp(Velocity, Vector3.zero, Time.deltaTime * 5f);
    }


    public void InitializeAttackConfigs()
    {
        foreach (PlayerAttackConfig config in attackConfigs)
        {
            if (config.attackName != PlayerAttackName.PlayerAttackUnknown)
            {
                if (!AttackDict.TryAdd(config.attackName, config))
                {
                    Debug.LogError($"PlayerAttackConfig {config.name}이 중복됨");
                }
            }
            else
            {
                Debug.LogError($"PlayerAttackConfig중 한 개 이상의 attackName이 설정되지 않음.");
            }
        }
    }

    private void InitializeCharacter()
    {
        maxHp = 100;
        CurrentHp = maxHp;
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

    //플레이어에게 닿은 공격이 유효한지 체크
    public void AttackValidation(AttackConfig config, AttackType attackType, int attackIdx, Transform monsterTransform, SoundType hitSound, Vector3 hitPoint)
    {
        if (dodgeInvincible) return; //회피무적상태면 리턴
        
        Debug.LogWarning("인빈시블아님");

        if (attackIdx < 0) //0미만이면 다단히트라서 조건 계산 할 필요 없음 
        {
            TakeDamage(config, monsterTransform);
        }
        else if (attackIdx > 0)
        {
            if (hitDict.TryGetValue((attackType, attackIdx), out bool value))
            {
                //value값은 의미없고, 일단 true면 같은 공격에 맞았다는 뜻
                Debug.Log($"이미 맞은 공격임(AttackType: {attackType}, index: {attackIdx})");
                return;
            }

            StartCoroutine(HitIntervalTimer(attackType, attackIdx));
            TakeDamage(config, monsterTransform);
            
            //랜덤한 피격 파티클 생성
            if (config.HitEffects is { Length: > 0 })
            {
                // 랜덤한 이펙트를 선택
                var effect = config.HitEffects[Random.Range(0, config.HitEffects.Length)];
                if (effect.ParticleEffect)
                {
                    // 이펙트 생성
                    GameObject particle = Instantiate(effect.ParticleEffect, hitPoint, effect.EffectRotation);
                    particle.transform.localScale = effect.EffectScale != Vector3.zero ? effect.EffectScale : Vector3.one;

                    // 일정 시간 후 제거
                    Destroy(particle, 2f);
                }
            }
            
            //피격 사운드 재생
            SoundManager.Instance.PlayRandomSound(characterSounds.GetSounds(hitSound), position: transform.position);
            
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
    protected (float lr, float fb, bool isBound, bool isDown, float motionIndex) SetAnimatorParameters(
        Vector3 attackDirection,
        AttackConfig config)
    {
        float lr = 0f;
        float fb = 0f;

        Vector3 localAttackDir = transform.InverseTransformDirection(attackDirection);

        bool isBound = config.knockBackType == KnockBackType.KnockbackBound;
        bool isDown = config.knockBackType == KnockBackType.KnockbackDown;
        Animator.SetBool(FallDown, isBound);
        Animator.SetBool(Down, isDown);

        if (isBound)
        {
            // isBound가 true일 때: 앞과 뒤만 고려하여 FB 설정
            if (localAttackDir.z > 0.5f)
            {
                fb = -1f; // 앞에서 공격
            }
            else
            {
                fb = 1f; // 뒤에서 공격
            }

            // 좌우는 고려하지 않음
            lr = 0f;
            IsDown = true;
        }
        else if (isDown)
        {
            IsDown = true;
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

        return (lr, fb, isBound, isDown, motionIndex);
    }

    protected IEnumerator HandleStun(float stunDuration)
    {
        IsStun = true;
        yield return new WaitForSeconds(stunDuration);
        IsStun = false;
        if (!IsDie) Animator.SetTrigger(StunEnd); //죽지 않았을 경우에만 스턴이 풀림
    }

    public void StandUp()
    {
        IsDown = false;
    }
    
    //가한 공격의 피격체크
    public abstract void HitCheck();

    //Animation Event에서 호출
    public void DodgeFlagOn()
    {
        isDodge = true;
    }

    //Animation Event에서 호출
    public void DodgeFlagOff()
    {
        isDodge = false;
        yaw = transform.eulerAngles.y;
    }

    public void InvincibleOn()
    {
        dodgeInvincible = true;
    }

    public void InvincibleOff()
    {
        dodgeInvincible = false;
    }
    
    
    //닷지 중에 피격될 경우
    public void DodgeStop()
    {
        isDodge = false;
        yaw = transform.eulerAngles.y;
        
        if (DodgeCoroutine != null)
        {
            StopCoroutine(DodgeCoroutine);
            DodgeCoroutine = null;
        }
    }

    public abstract void RotateStop();

    public void CreateAttackParticle(int idx)
    {
        if (!currentAttack)
        {
            Debug.LogError("currentAttack이 null임");
            return;
        }

        // 파티클 이펙트가 설정되어 있는지 확인합니다.
        if (currentAttack.EffectConfigs != null && currentAttack.EffectConfigs.Length > idx)
        {
            EffectConfig effect = currentAttack.EffectConfigs[idx];
            if (currentAttack.EffectConfigs[idx].ParticleEffect)
            {
                // // 공격 위치 계산 (HitCheck와 유사하게)
                // Vector3 attackPosition = transform.position + transform.forward * config.distance +
                //                          transform.TransformDirection(config.attackPositionOffset);

                // 이펙트의 위치, 회전, 크기를 설정합니다.
                Vector3 effectPosition = transform.position + transform.TransformDirection(effect.EffectPosition);
                Quaternion effectRotation = transform.rotation * effect.EffectRotation;
                Vector3 effectScale = effect.EffectScale != Vector3.zero ? effect.EffectScale : Vector3.one;

                // 파티클 이펙트를 인스턴스화합니다.
                GameObject particle = Instantiate(effect.ParticleEffect, effectPosition, effectRotation, transform);
                particle.transform.localScale = effectScale;

                // 이펙트가 일정 시간 후에 자동으로 파괴되도록 설정 (선택 사항)
                ParticleSystem ps = particle.GetComponent<ParticleSystem>();
                if (ps)
                {
                    Destroy(particle, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                {
                    // ParticleSystem이 없을 경우 기본적으로 5초 후 파괴
                    Destroy(particle, 5f);
                }
            }
            else
            {
                Debug.LogWarning($"AttackConfig에 파티클 이펙트가 설정되어 있지 않습니다: {currentAttack}");
            }
        }
        else
        {
            Debug.LogWarning("AttackConfig의 EffectConfigs가 null이거나 idx가 배열을 초과합니다.");
        }

        // 사운드 이펙트가 설정되어 있는지 확인하고 재생합니다.
        // if (currentAttack.SoundEffects is { Length: > 0 })
        // {
        //     // AudioSource가 존재하는지 확인하고 없으면 추가합니다.
        //     AudioSource audioSource = GetComponent<AudioSource>();
        //     if (!audioSource)
        //     {
        //         audioSource = gameObject.AddComponent<AudioSource>();
        //     }
        //
        //     // 사운드 클립을 랜덤으로 선택하여 재생합니다.
        //     AudioClip selectedClip = currentAttack.SoundEffects[Random.Range(0, currentAttack.SoundEffects.Length)];
        //     audioSource.PlayOneShot(selectedClip);
        // }
    }
    
    // public void PlayAttackSound(int idx)
    // {
    //     if (!currentAttack)
    //     {
    //         Debug.LogError("currentAttack이 null임");
    //         return;
    //     }
    //
    //     if (currentAttack.SoundEffects != null && currentAttack.SoundEffects.Length > idx)
    //     {
    //         // config.soundEffects[idx]
    //         // SoundManager.Instance.PlayRandomSound(currentAttack.SoundEffects[idx], position: transform.position);
    //         SoundManager.Instance.PlayRandomSound(currentAttack.SoundEffects[idx].Clips, position: transform.position);
    //     }
    //     else
    //     {
    //         Debug.LogWarning("AttackConfig의 soundEffects가 null이거나 idx가 배열을 초과합니다.");
    //     }
    // }
    
    public void PlayAttackSound(int index)
    {
        if (!currentAttack)
        {
            Debug.LogError("currentAttack이 null임");
            return;
        }
        
        if (currentAttack.SoundEffects != null && currentAttack.SoundEffects.Length > index)
        {
            SoundManager.Instance.PlayRandomSound(characterSounds.GetSounds(currentAttack.SoundEffects[index].Swing),
                position: transform.position);
            CurrentHitSound = currentAttack.SoundEffects[index].Hit;
        }
        else
        {
            Debug.LogWarning($"SoundEffects가 null이거나 index가 배열을 초과함: {currentAttack}");
        }
        
    }
    
    //애니메이션 이벤트로 호출됨
    public abstract void AttackStart(PlayerAttackName attackName);

    //anystate에서 시작하는 모든 clip에도 다 달아놔야 함. <- 귀찮아서 AnyStateBehaviour로 대체
    public abstract void AttackEnd();

    public abstract void AttackMoveStart();
    public abstract void AttackMoveStop();

    private void SetFill(float curHp)
    {
        if (!connectedPanel) return;
        connectedPanel.SetFill(curHp);
    }
}