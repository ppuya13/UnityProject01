
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
