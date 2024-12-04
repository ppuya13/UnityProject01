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
    
    protected override void Awake()
    {
        base.Awake();
        CurrentSpeed = WalkSpeed;
        
        // Cursor.visible = false;
        // Cursor.lockState = CursorLockMode.Locked;
    }

    protected override void Update()
    {
        base.Update();
        CursorControl();
        
        if (IsStun) return; 
        HandleMoveInput();
        HandleMouseInput();
        Move();
        TiltSetting();
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
