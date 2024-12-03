using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class PlayerController : MonoBehaviour
{
    public enum CharacterType
    {
        Unknown,
        MyPlayer,
        OtherPlayer,
        Dummy,
    }

    public string playerId;
    public CharacterType type;

    protected const float gravity = -9.81f; // 중력 값
    protected float Speed = 5f; // 이동 속도
    protected float JumpHeight = 2.0f; // 점프 높이 (추가적으로 점프를 적용할 경우)
    protected Vector3 Velocity;

    protected CharacterController CharacterController;

    private AttackType hitAttack;
    private int hitIndex;
    private float hitInterval; //같은 속성의 공격을 더 이상 받지 않는 쿨타임
    private const float HitThreshold = 0.5f; //쿨타임이 임계점에 도달하면 같은 공격을 받을 수 있음
    private Dictionary<(AttackType, int), bool> hitDict = new(); //이미 맞은 공격들

    protected virtual void Awake()
    {
        CharacterController = GetComponent<CharacterController>();
    }

    protected virtual void Update()
    {
        if (type == CharacterType.Dummy) return;
        Gravity();
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


    public void TakeDamage(float damage, AttackType attackType, int attackIdx)
    {
        if (attackIdx < 0) //0미만이면 다단히트라서 조건 계산 할 필요 없음 
        {
            Debug.Log("");
        }
        else if (attackIdx > 0)
        {
            Debug.Log("");
            if (hitDict.TryGetValue((attackType, attackIdx), out bool value))
            {
                //value값은 의미없고, 일단 true면 같은 공격에 맞았다는 뜻
                Debug.Log($"이미 맞은 공격임(AttackType: {attackType}, index: {attackIdx})");
                return;
            }

            StartCoroutine(HitIntervalTimer(attackType, attackIdx));
            
            Debug.Log($"공격 히트(damage: {damage}, AttackType: {attackType}, index: {attackIdx})");
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
}