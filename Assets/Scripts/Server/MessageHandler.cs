using System;
using Game;
using Monster;
using UnityEngine;


//Game Manager를 대체한다.
//리팩토링 하는 김에 좀 더 명확한 이름으로 바꿈.
public class MessageHandler : DDSingletonManager<MessageHandler>
{
    private void Update()
    {
        if (!UnityMainThreadDispatcher.Instance)
        {
            Debug.LogError("[MessageHandler] UnityMainThreadDispatcher is null!");
            return;
        }

        if (UnityMainThreadDispatcher.Instance.ExecutionQueue == null)
        {
            Debug.LogError("[MessageHandler] ExecutionQueue is null!");
            return;
        }

        while (UnityMainThreadDispatcher.Instance.ExecutionQueue.Count > 0)
        {
            GameMessage msg = UnityMainThreadDispatcher.Instance.ExecutionQueue.Dequeue();
            // Debug.Log($"MessageHandler processing: {msg.PayloadCase}");

            switch (msg.PayloadCase)
            {
                case GameMessage.PayloadOneofCase.LoginResponse:
                    Debug.Log("로그인 리스폰스 수신");
                    HandleLoginResponse(msg.LoginResponse);
                    break;
                case GameMessage.PayloadOneofCase.LogoutResponse:
                    Debug.Log("로그아웃 리스폰스 수신");
                    break;
                case GameMessage.PayloadOneofCase.MonsterSpawn:
                    Debug.Log("몬스터 스폰 메시지 수신");
                    SpawnManager.Instance.SpawnMonster(msg.MonsterSpawn);
                    break;
                case GameMessage.PayloadOneofCase.MonsterAnim:
                    Debug.Log("몬스터 애니메이션 스테이트 변경 메시지 수신");
                    HandleMonsterAnim(msg.MonsterAnim);
                    break;
                case GameMessage.PayloadOneofCase.MonsterAction:
                    Debug.Log("몬스터 액션 메시지 수신");
                    HandleMonsterAction(msg.MonsterAction);
                    break;
                default:
                    Debug.LogError($"정의되지 않은 케이스 ({msg.PayloadCase})");
                    break;
            }
        }
    }

    private void HandleLoginResponse(LoginResponse msg)
    {
        if (msg.Success)
        {
            SuperManager.Instance.PlayerId = msg.PlayerId;
            SuperManager.Instance.PlayerNickname = msg.Username;

            SpawnManager.Instance.SpawnPlayer(msg.PlayerId);

            SpawnManager.Instance.ScanPlayer();
            if (SuperManager.Instance.IsHost)
            {
                TcpProtobufClient.Instance.SendMonsterSpawn();
            }
        }
        else
        {
            Debug.LogError(msg.ErrorMessage);
        }
    }


    #region 몬스터 관련

    #region 몬스터 애니메이션 스테이트

    private void HandleMonsterAnim(MonsterAnim msg)
    {
        if (SpawnManager.Instance.SpawnedMonsters.TryGetValue(msg.MonsterId, out MonsterController mc))
        {
            mc.SetParameter(msg);
        }
        else
        {
            Debug.LogError("MonsterController를 찾을 수 없음.");
        }
    }

    #endregion

    #region 몬스터 액션

    private void HandleMonsterAction(MonsterAction msg)
    {
        switch (msg.ActionType)
        {
            case ActionType.MonsterActionSetStatus:
            {
                if (SpawnManager.Instance.SpawnedMonsters.TryGetValue(msg.MonsterId, out MonsterController mc))
                {
                    mc.ChangeState(msg);
                }
                else
                {
                    Debug.LogError("MonsterController를 찾을 수 없음.");
                }
            }
                break;
            case ActionType.MonsterActionSetTarget:
            {
                if (SpawnManager.Instance.SpawnedMonsters.TryGetValue(msg.MonsterId, out MonsterController mc))
                {
                    mc.SetTarget(msg);
                }
                else
                {
                    Debug.LogError("MonsterController를 찾을 수 없음.");
                }
            }
                break;
            case ActionType.MonsterActionSetDestination:
            {
                if (SpawnManager.Instance.SpawnedMonsters.TryGetValue(msg.MonsterId, out MonsterController mc))
                {
                    mc.SetDestination(msg);
                }
                else
                {
                    Debug.LogError("MonsterController를 찾을 수 없음.");
                }
            }
                break;
            default:
                Debug.LogError($"정의되지 않은 액션 타입 ({msg.ActionType})");
                break;
        }
    }

    #endregion

    #endregion
}