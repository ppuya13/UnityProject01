using Game;
using Monster;
using RootMotion.FinalIK;
using UnityEngine;

public class MyPlayer: PlayerController
{
    public float axisSpeed = 5f; // 축 값이 변화하는 속도
    private float moveX = 0f;
    private float moveZ = 0f;
    
    private float yaw = 0f;
    
    public float mouseSensitivity = 100f; // 마우스 민감도
    public Camera characterCamera;
    public BodyTilt bodyTilt;

    
    private Vector3 lastPosition;
    private Vector3 lastRotation;
    private float lastSendTime;

    private bool inGame = false;
    
    protected override void Awake()
    {
        base.Awake();
        CurrentSpeed = WalkSpeed;

        lastPosition = transform.position;
        lastSendTime = Time.time;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
    }

    protected override void Update()
    {
        base.Update();
        CursorControl();
        SendPosition();
        
        if (IsStun || IsDie || disableKeyboard) return; 
        HandleMouseInput();
        HandleMoveInput();
        Move();
        TiltSetting();
    }

    protected override void TakeDamage(AttackConfig config, Transform monsterTransform)
    {
        Debug.Log($"공격 히트(damage: {config.damageAmount})");
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
        (float lr, float fb, bool isBound, float motionIndex) = SetAnimatorParameters(attackDirection, config);

        
        // stunDuration 동안 대기 후 StunEnd 트리거 활성화
        if (StunCoroutine != null)
        {
            StopCoroutine(StunCoroutine);
            StunCoroutine = null;
        }
        StunCoroutine = StartCoroutine(HandleStun(config.stunDuration));
        
        TcpProtobufClient.Instance.SendPlayerTakeDamage(knockback, config.stunDuration, currentHp, IsDie, lr, fb, isBound, motionIndex);
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

        TcpProtobufClient.Instance.SendPlayerMovement(currentPosition, velocity, rotation, horizontal, vertical, isRunning);
        
        lastPosition = currentPosition;
        lastSendTime = Time.time;
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
    
    private void HandleMouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        yaw += mouseX;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
    
    private void Move()
    {
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        CharacterController.Move(move * (CurrentSpeed * Time.deltaTime) + Velocity * Time.deltaTime);
        
        if (Animator)
        {
            Animator.SetFloat(Horizontal, moveX);
            Animator.SetFloat(Vertical, moveZ);
        }
    }

    private void TiltSetting()
    {
        float move = Mathf.Max(Mathf.Abs(moveX), Mathf.Abs(moveZ));
        bodyTilt.weight = move >= 0.1 ? move : 0;
    }
}
