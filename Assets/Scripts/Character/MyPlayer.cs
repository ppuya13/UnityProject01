using System;
using System.Collections;
using System.Collections.Generic;
using Character;
using Game;
using Monster;
using RootMotion.FinalIK;
using Sirenix.Utilities;
using UnityEngine;

public class MyPlayer : PlayerController
{
    public float axisSpeed = 5f; // 축 값이 변화하는 속도
    private float moveX = 0f;
    private float moveZ = 0f;

    private float yaw = 0f;

    public float mouseSensitivity = 120f; // 마우스 민감도
    public Camera characterCamera;
    public BodyTilt bodyTilt;

    private Vector3 lastPosition;
    private Vector3 lastRotation;
    private float lastSendTime;

    private bool inGame = false;
    private bool invincible = false;
    private bool isAttack = false;
    private bool attackMove = false; //공격 중에 앞키를 누르면 true가 됨. 공격 모션 중 이동 가능 상태일 때 true이면 앞으로 이동함.
    private const float MoveTime = 2.0f; // 애니메이션에 moveEnd를 달지 않았거나 호출되지 않았을 시 최대 이동 시간을 설정하기 위한 변수.
    private Coroutine attackMoveCoroutine;

    private static readonly int Attack = Animator.StringToHash("Attack");

    public PlayerAttackConfig[] attackConfigs;
    public Dictionary<PlayerAttackName, PlayerAttackConfig> AttackDict = new();

    protected override void Awake()
    {
        base.Awake();
        CurrentSpeed = WalkSpeed;

        lastPosition = transform.position;
        lastSendTime = Time.time;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        InitializeAttackConfigs();
    }

    protected override void Update()
    {
        base.Update();
        Invincible();
        SendPing();
        CursorControl();
        SendPosition();

        //이 아래로는 플레이어가 행동 가능한 상황일때만 실행됨
        if (IsStun || IsDie || disableKeyboard || IsDown || Cursor.visible) return;
        HandleAttackInput();
        HandleMouseInput();

        //공격중엔 실행되지 않음.
        if (!isAttack)
        {
            HandleMoveInput();
            Move();
            TiltSetting();
        }
        else
        {
            HandleAttackMoveInput();
        }
    }

    private void InitializeAttackConfigs()
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

    private void Invincible()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (!invincible)
            {
                invincible = true;
                UIManager.Instance.invincibleMode.SetActive(true);
                currentHp = maxHp;
            }
            else
            {
                invincible = false;
                UIManager.Instance.invincibleMode.SetActive(false);
            }
        }
    }

    private void SendPing()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            TcpProtobufClient.Instance.SendPing();
        }
    }

    protected override void TakeDamage(AttackConfig config, Transform monsterTransform)
    {
        // Debug.Log($"공격 히트(damage: {config.damageAmount})");
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
        if (!invincible)
        {
            currentHp -= config.damageAmount;
            if (currentHp <= 0) IsDie = true; // 사망 여부 플래그 처리
        }

        // 애니메이터 파라미터 설정
        (float lr, float fb, bool isBound, bool isDown, float motionIndex) =
            SetAnimatorParameters(attackDirection, config);


        // stunDuration 동안 대기 후 StunEnd 트리거 활성화
        if (StunCoroutine != null)
        {
            StopCoroutine(StunCoroutine);
            StunCoroutine = null;
        }

        StunCoroutine = StartCoroutine(HandleStun(config.stunDuration));

        TcpProtobufClient.Instance.SendPlayerTakeDamage(knockback, config.stunDuration, currentHp, IsDie, lr, fb,
            isBound, isDown, motionIndex);
    }

    //마우스를 없앴다 생겼다 함
    private void CursorControl()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.visible) return;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        if (Cursor.visible)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    //서버에 캐릭터의 현재 위치를 전송하는 메소드
    private void SendPosition()
    {
        Vector3 currentPosition = transform.position;
        Vector3 deltaPosition = currentPosition - lastPosition;
        float deltaTime = Time.time - lastSendTime;

        if (deltaTime <= 0)
        {
            deltaTime = 0.016f; // 기본값 (약 60 FPS)
        }

        Vector3 velocity = deltaPosition / deltaTime;
        Vector3 rotation = transform.rotation.eulerAngles;
        float horizontal = moveX;
        float vertical = moveZ;
        bool isRunning = IsRun;

        TcpProtobufClient.Instance.SendPlayerMovement(currentPosition, velocity, rotation, horizontal, vertical,
            isRunning);

        lastPosition = currentPosition;
        lastSendTime = Time.time;
    }

    private void HandleMouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        yaw += mouseX;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void HandleMoveInput()
    {
        float targetMoveX = Input.GetAxisRaw("Horizontal");
        float targetMoveZ = Input.GetAxisRaw("Vertical");

        Vector2 moveInput = new Vector2(targetMoveX, targetMoveZ);
        float moveMagnitude = moveInput.magnitude;

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            IsRun = !IsRun;
        }

        if (moveMagnitude <= 0.1f)
        {
            IsRun = false;
        }

        float finalMoveX = IsRun ? targetMoveX * 2 : targetMoveX;
        float finalMoveZ = IsRun ? targetMoveZ * 2 : targetMoveZ;

        moveX = Mathf.Lerp(moveX, finalMoveX, axisSpeed * Time.deltaTime);
        moveZ = Mathf.Lerp(moveZ, finalMoveZ, axisSpeed * Time.deltaTime);

        float targetSpeed = IsRun ? RunSpeed : WalkSpeed;
        CurrentSpeed = Mathf.Lerp(CurrentSpeed, targetSpeed, SpeedChangeRate * Time.deltaTime);
    }

    private void HandleAttackMoveInput()
    {
        float targetMoveZ = Input.GetAxisRaw("Vertical");
        float clamp = Mathf.Clamp01(targetMoveZ);
        if (clamp > 0)
        {
            attackMove = true;
        }
        else
        {
            attackMove = false;
        }
    }

    private void HandleAttackInput()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Animator.SetTrigger(Attack);
        }
    }

    private void Move()
    {
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        if (Animator)
        {
            Animator.SetFloat(Horizontal, moveX);
            Animator.SetFloat(Vertical, moveZ);
        }

        if (isAttack) return;
        CharacterController.Move(move * (CurrentSpeed * Time.deltaTime) + Velocity * Time.deltaTime);
    }

    private void StandUp()
    {
        IsDown = false;
    }

    private void TiltSetting()
    {
        float move = Mathf.Max(Mathf.Abs(moveX), Mathf.Abs(moveZ));
        bodyTilt.weight = move >= 0.1 ? move : 0;
    }

    //AttackBehaviour에서 호출(AttackBehaviour에서 직접 보내도 되지만, Player 스크립트에서 보내는 게 나중에 찾기도 편하니까)
    public void SendAttackState(int hash, int layer)
    {
        TcpProtobufClient.Instance.SendAttackState(hash, layer);
    }

    //애니메이션 이벤트로 호출됨
    public void AttackStart(PlayerAttackName attackName)
    {
        // Debug.Log("어택스타트");
        if (attackName == PlayerAttackName.PlayerAttackUnknown)
        {
            Debug.LogError($"현재 공격의 attackName이 설정되지 않음!! Animation Event에서 attackName을 설정해야 함.");
        }
        else
        {
            currentAttack = AttackDict[attackName];
        }

        moveX = 0;
        moveZ = 0;
        Animator.SetFloat(Horizontal, 0);
        Animator.SetFloat(Vertical, 0);
        isAttack = true;
    }

    //anystate에서 시작하는 모든 clip에도 다 달아놔야 함. <- 귀찮아서 AnyStateBehaviour로 대체
    public void AttackEnd()
    {
        // Debug.Log("어택엔드");
        isAttack = false;

        if (attackMoveCoroutine != null)
        {
            StopCoroutine(attackMoveCoroutine);
            attackMoveCoroutine = null;
        }

        currentAttack = null;
    }

    //각 공격 애니메이션의 이동 시간에 이벤트로 호출
    public void AttackMoveStart()
    {
        if (attackMoveCoroutine != null)
        {
            StopCoroutine(attackMoveCoroutine);
            attackMoveCoroutine = null;
        }

        attackMoveCoroutine = StartCoroutine(AttackMove());
    }

    public void AttackMoveStop()
    {
        if (attackMoveCoroutine != null)
        {
            StopCoroutine(attackMoveCoroutine);
            attackMoveCoroutine = null;
        }
    }

    IEnumerator AttackMove()
    {
        float elapsed = 0f;
        while (attackMove && elapsed < MoveTime)
        {
            Vector3 movement = transform.forward * (AttackMoveSpeed * Time.deltaTime);
            transform.position += movement;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"애니메이션에 MoveStop 이벤트가 설정되지 않음.");
    }

    public override void HitCheck()
    {
        if (!currentAttack)
        {
            Debug.LogError("currentAttack이 null임");
            return;
        }

        //데미지를 설정
        float distance = currentAttack.distance;
        Vector3 attackPositionOffset = currentAttack.attackPositionOffset;

        // 공격 위치 계산
        Vector3 attackPosition = transform.position + transform.forward * distance +
                                 transform.TransformDirection(attackPositionOffset);


        // 히트 판정 수행
        Collider[] hitColliders = Array.Empty<Collider>();

        switch (currentAttack.ColliderConfig.ColliderType)
        {
            case ColliderType.Sphere:
                if (currentAttack.ColliderConfig is SphereColliderConfig sphereConfig)
                {
                    hitColliders = Physics.OverlapSphere(attackPosition, sphereConfig.Radius, targetLayer);
                }
                else
                {
                    Debug.LogWarning("SphereCollider 설정이 올바르지 않습니다.");
                }

                break;
            case ColliderType.Box:
                if (currentAttack.ColliderConfig is BoxColliderConfig boxConfig)
                {
                    Vector3 boxCenterWorld = attackPosition + transform.TransformDirection(boxConfig.Center);
                    Quaternion worldRotation = transform.rotation * boxConfig.Rotation;
                    hitColliders = Physics.OverlapBox(boxCenterWorld, boxConfig.Size * 0.5f, worldRotation,
                        targetLayer);
                }
                else
                {
                    Debug.LogWarning("BoxCollider 설정이 올바르지 않습니다.");
                }

                break;

            case ColliderType.Capsule:
                if (currentAttack.ColliderConfig is CapsuleColliderConfig capsuleConfig)
                {
                    Vector3 point1 = attackPosition +
                                     capsuleConfig.Direction * (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                    Vector3 point2 = attackPosition -
                                     capsuleConfig.Direction * (capsuleConfig.Height / 2 - capsuleConfig.Radius);
                    hitColliders = Physics.OverlapCapsule(point1, point2, capsuleConfig.Radius, targetLayer);
                }
                else
                {
                    Debug.LogWarning("CapsuleCollider 설정이 올바르지 않습니다.");
                }

                break;

            default:
                Debug.LogWarning($"Unknown ColliderType: {currentAttack.ColliderConfig.ColliderType}.");
                break;
        }

        // Debug.Log($"공격, {hitColliders.Length}");

        // Debug.Log(
        //     $"HitCheck - AttackType: {currentAttack}, AttackIdx: {attackIdx}, Distance: {distance}, ColliderType: {config.ColliderConfig.ColliderType}, Hits: {hitColliders.Length}");
        foreach (var hitCollider in hitColliders)
        {
            MonsterController monster = hitCollider.GetComponent<MonsterController>();
            if (monster)
            {
                // 데미지 적용
                monster.AttackValidation(currentAttack, transform);
            }
        }
    }
}