using UnityEngine;

public class MyPlayer: PlayerController
{
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();
        Move();
        Jump();
    }
    
    private void Move()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        CharacterController.Move(move * (Speed * Time.deltaTime) + Velocity * Time.deltaTime);
    }
    
    private void Jump()
    {
        if(!Input.GetKeyDown(KeyCode.Space) || !CharacterController.isGrounded) return;
        
        Velocity.y = Mathf.Sqrt(JumpHeight * -2f * gravity);
    }
}
